// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — UpdateManager.cs  (GitHub Releases + SHA256 delta updates)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tickr.Launcher.Models;

namespace Tickr.Launcher;

/// <summary>
/// Progress of a file download batch: <paramref name="Current"/> of <paramref name="Total"/> files done, currently on <paramref name="FileName"/>.
/// </summary>
internal readonly record struct UpdateProgress(int Current, int Total, string FileName);

internal static class UpdateManager {
	internal const string GitHubRepo = "8owner8/Tickr";
	internal const string ManifestAssetName = "manifest.json";
	internal const string DataReleaseTagPrefix = "data-";
	internal const string LauncherExeName = "Tickr.exe";
	internal const string VersionFileName = "version.txt";
	internal const string TempDirectoryName = "temp";
	internal const string SelfRestartPendingFileName = "self_restart.pending";

	private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

	private const int MaxParallelOperations = 8;

	private static readonly HttpClient Http = new() {
		// Timeouts are enforced per-request via cancellation tokens - large file downloads must not hit the default 100 s cap
		Timeout = Timeout.InfiniteTimeSpan,
		// HTTP/2 multiplexing makes parallel downloads of many small files dramatically faster
		DefaultRequestVersion = HttpVersion.Version20,
		DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
		DefaultRequestHeaders = {
			{ "User-Agent", "Tickr-Launcher/1.0" }
		}
	};

	/// <summary>Resolves GitHub's public latest-release redirect. Returns null when there is nothing newer (or the redirect is malformed).</summary>
	/// <exception cref="HttpRequestException">Network/GitHub failure - caller decides the fallback.</exception>
	internal static async Task<UpdateInfo?> CheckForUpdateAsync(Version localVersion, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(localVersion);

		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(RequestTimeout);

		// Do not use api.github.com here: unauthenticated API calls are limited per public IP and can
		// prevent every installation behind that IP from updating. The regular releases/latest URL
		// redirects to /releases/tag/{tag} without consuming the API quota.
		using HttpResponseMessage response = await Http.GetAsync(
			new Uri($"https://github.com/{GitHubRepo}/releases/latest"),
			HttpCompletionOption.ResponseHeadersRead,
			timeout.Token
		).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		string? releaseTag = ExtractReleaseTag(response.RequestMessage?.RequestUri);

		if (string.IsNullOrEmpty(releaseTag)) {
			return null;
		}

		string remoteVersionText = releaseTag.TrimStart('v', 'V');

		if (!Version.TryParse(remoteVersionText, out Version? remoteVersion) || remoteVersion <= localVersion) {
			return null;
		}

		string launcherUrl = $"https://github.com/{GitHubRepo}/releases/download/{releaseTag}/{LauncherExeName}";
		Dictionary<string, string> assets = new(StringComparer.OrdinalIgnoreCase) { [LauncherExeName] = launcherUrl };

		return new UpdateInfo {
			Version = remoteVersionText,
			// The update payload (manifest + files) lives in a parallel prerelease "data-{tag}",
			// so the user-facing release stays clean: Tickr.exe + source code archives only.
			ManifestUrl = $"https://github.com/{GitHubRepo}/releases/download/{DataReleaseTagPrefix}{releaseTag}/{ManifestAssetName}",
			Assets = assets,
			HasSelfUpdate = true
		};
	}

	internal static string? ExtractReleaseTag(Uri? uri) {
		if (uri == null || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) {
			return null;
		}

		string prefix = $"/{GitHubRepo}/releases/tag/";

		if (!uri.AbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
			return null;
		}

		string tag = Uri.UnescapeDataString(uri.AbsolutePath[prefix.Length..]).Trim('/');

		return string.IsNullOrEmpty(tag) || tag.Contains('/') ? null : tag;
	}

	internal static async Task<Manifest> DownloadManifestAsync(string manifestUrl, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrEmpty(manifestUrl);

		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(RequestTimeout);

		string json = await Http.GetStringAsync(new Uri(manifestUrl), timeout.Token).ConfigureAwait(false);
		Manifest? manifest = JsonSerializer.Deserialize(json, LauncherJsonContext.Default.Manifest);

		if (manifest == null || string.IsNullOrEmpty(manifest.Version)) {
			throw new InvalidDataException("Downloaded manifest is empty or malformed");
		}

		return manifest;
	}

	/// <summary>Returns only the files that are missing locally or whose SHA256 differs from the manifest.</summary>
	internal static async Task<List<ManifestFile>> ComputeDeltaAsync(Manifest manifest, string baseDirectory, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(manifest);
		ArgumentException.ThrowIfNullOrEmpty(baseDirectory);

		ConcurrentBag<ManifestFile> delta = [];

		// Hashing is IO + CPU bound - parallelizing it cuts manifest verification time on big installs substantially
		await Parallel.ForEachAsync(
			manifest.Files,
			new ParallelOptions { MaxDegreeOfParallelism = MaxParallelOperations, CancellationToken = cancellationToken },
			async (file, ct) => {
				string localPath = ToLocalPath(baseDirectory, file.Path);

				if (!File.Exists(localPath)) {
					delta.Add(file);

					return;
				}

				string localHash = await ComputeSha256Async(localPath, ct).ConfigureAwait(false);

				if (!localHash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) {
					delta.Add(file);
				}
			}
		).ConfigureAwait(false);

		return [.. delta];
	}

	/// <summary>Downloads every delta file into <paramref name="tempDirectory"/>, verifying SHA256 after each one. On any failure the temp directory is wiped and false is returned.</summary>
	internal static async Task<bool> DownloadFilesAsync(IReadOnlyList<ManifestFile> delta, string tempDirectory, UpdateInfo update, IProgress<UpdateProgress>? progress = null, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(delta);
		ArgumentException.ThrowIfNullOrEmpty(tempDirectory);
		ArgumentNullException.ThrowIfNull(update);

		try {
			if (Directory.Exists(tempDirectory)) {
				Directory.Delete(tempDirectory, true);
			}

			Directory.CreateDirectory(tempDirectory);

			// Downloads run in parallel - per-file latency dominates on small files, so this is an order
			// of magnitude faster than sequential. A file that fails verification aborts the whole batch.
			int completed = 0;

			await Parallel.ForEachAsync(
				delta,
				new ParallelOptions { MaxDegreeOfParallelism = MaxParallelOperations, CancellationToken = cancellationToken },
				async (file, ct) => {
					string url = ResolveDownloadUrl(file, update.Assets);
					string tempPath = ToLocalPath(tempDirectory, file.Path);

					Directory.CreateDirectory(Path.GetDirectoryName(tempPath) ?? tempDirectory);

					string partPath = tempPath + ".part";

					await DownloadFileAsync(url, partPath, ct).ConfigureAwait(false);

					string downloadedHash = await ComputeSha256Async(partPath, ct).ConfigureAwait(false);

					if (!downloadedHash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) {
						throw new InvalidDataException($"SHA256 mismatch for {file.Path}: expected {file.Sha256}, got {downloadedHash}");
					}

					File.Move(partPath, tempPath, true);

					int done = Interlocked.Increment(ref completed);
					progress?.Report(new UpdateProgress(done, delta.Count, file.Path));
				}
			).ConfigureAwait(false);

			progress?.Report(new UpdateProgress(delta.Count, delta.Count, ""));

			return true;
		} catch (Exception e) when (e is not OperationCanceledException) {
			LauncherLog.Error("File download failed, rolling back", e);
			CleanupTemp(tempDirectory);

			return false;
		}
	}

	/// <summary>Moves downloaded files from temp into the app directory (everything except Tickr.exe) and stamps version.txt. Returns true if a launcher self-update is pending in temp.</summary>
	internal static bool ApplyUpdate(Manifest manifest, string baseDirectory, string tempDirectory) {
		ArgumentNullException.ThrowIfNull(manifest);
		ArgumentException.ThrowIfNullOrEmpty(baseDirectory);
		ArgumentException.ThrowIfNullOrEmpty(tempDirectory);

		bool selfUpdatePending = false;

		foreach (ManifestFile file in manifest.Files) {
			string tempPath = ToLocalPath(tempDirectory, file.Path);

			if (!File.Exists(tempPath)) {
				// Not part of the delta - unchanged locally
				continue;
			}

			if (file.Path.Equals(LauncherExeName, StringComparison.OrdinalIgnoreCase)) {
				selfUpdatePending = true;

				continue;
			}

			string destinationPath = ToLocalPath(baseDirectory, file.Path);

			Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? baseDirectory);
			File.Move(tempPath, destinationPath, true);
		}

		File.WriteAllText(Path.Combine(baseDirectory, VersionFileName), manifest.Version);

		return selfUpdatePending;
	}

	/// <summary>
	/// Replaces the running Tickr.exe with the one downloaded into temp, then hands over to the new process.
	/// Windows allows renaming a running executable, which is the trick that makes self-update possible.
	/// This method never returns - the process exits on success.
	/// </summary>
	internal static void ApplySelfUpdate(string baseDirectory, string tempDirectory) {
		ArgumentException.ThrowIfNullOrEmpty(baseDirectory);
		ArgumentException.ThrowIfNullOrEmpty(tempDirectory);

		string exePath = Path.Combine(baseDirectory, LauncherExeName);
		string oldPath = exePath + ".old";
		string newPath = ToLocalPath(tempDirectory, LauncherExeName);

		File.Delete(oldPath);
		File.Move(exePath, oldPath);

		try {
			File.Move(newPath, exePath);
		} catch {
			// Roll back so the user is not left without a runnable launcher
			File.Move(oldPath, exePath);

			throw;
		}

		CleanupTemp(tempDirectory);

		// Marker for the new instance (and for diagnostics) that this start follows a self-update - the new launcher deletes it on startup
		File.WriteAllText(Path.Combine(baseDirectory, SelfRestartPendingFileName), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));

		LauncherLog.Info("Launcher self-update applied, restarting into the new version");

		Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true, WorkingDirectory = baseDirectory });
		Environment.Exit(0);
	}

	internal static void CleanupTemp(string tempDirectory) {
		try {
			if (Directory.Exists(tempDirectory)) {
				Directory.Delete(tempDirectory, true);
			}
		} catch (Exception e) {
			LauncherLog.Error($"Failed to clean up {tempDirectory}", e);
		}
	}

	/// <summary>Maps a relative manifest path to the flat asset name used in the GitHub release: "www/style.css" → "www__style.css". Must stay in sync with scripts/generate-manifest.ps1.</summary>
	internal static string EncodeAssetName(string relativePath) => relativePath.Replace("/", "__").Replace('\\', '_');

	private static string ResolveDownloadUrl(ManifestFile file, IReadOnlyDictionary<string, string> assets) {
		if (!string.IsNullOrEmpty(file.DownloadUrl)) {
			return file.DownloadUrl;
		}

		if (assets.TryGetValue(EncodeAssetName(file.Path), out string? url)) {
			return url;
		}

		throw new InvalidDataException($"No download URL available for {file.Path}");
	}

	private static string ToLocalPath(string baseDirectory, string relativePath) => Path.Combine(baseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

	private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken) {
		using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
		byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);

		return Convert.ToHexStringLower(hash);
	}

	private static async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken) {
		using HttpResponseMessage response = await Http.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		using FileStream fileStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
		await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
	}
}
