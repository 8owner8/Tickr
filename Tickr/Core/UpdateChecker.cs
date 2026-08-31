// ─────────────────────────────────────────────────────────────────────────────
//  Tickr — UpdateChecker.cs  (lightweight "new version available" notifier)
// ─────────────────────────────────────────────────────────────────────────────
//  The main app never downloads update files itself - that is the launcher's
//  job. This only checks the latest GitHub release (at most once per 4 hours),
//  shows a toast, and creates restart.pending when the user accepts. The
//  launcher picks the sentinel up, applies the update and restarts the app.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tickr.Core;

internal static class UpdateChecker {
	private const string LastCheckFileName = "update-check.txt";
	private const string RestartPendingFileName = "restart.pending";
	private static readonly TimeSpan MinimumCheckInterval = TimeSpan.FromHours(4);

	internal static void CheckInBackground() {
		Utilities.InBackground(CheckForUpdates);
	}

	/// <summary>Creates the restart.pending sentinel for the launcher and shuts the app down gracefully.</summary>
	internal static async Task RequestUpdateRestart() {
		string pendingPath = Path.Combine(SharedInfo.HomeDirectory, RestartPendingFileName);

		try {
			await File.WriteAllTextAsync(pendingPath, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)).ConfigureAwait(false);
		} catch (Exception e) {
			TickrApp.TickrLogger.LogGenericException(e);

			return;
		}

		await Program.Exit().ConfigureAwait(false);
	}

	private static async Task CheckForUpdates() {
		try {
			string stampPath = Path.Combine(SharedInfo.HomeDirectory, SharedInfo.ConfigDirectory, LastCheckFileName);

			if (File.Exists(stampPath)) {
				string stampText = await File.ReadAllTextAsync(stampPath).ConfigureAwait(false);

				if (DateTime.TryParse(stampText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime lastCheck) && (DateTime.UtcNow - lastCheck < MinimumCheckInterval)) {
					return;
				}
			}

			using HttpClient client = new();
			client.DefaultRequestHeaders.UserAgent.ParseAdd($"{SharedInfo.AssemblyName}/{SharedInfo.Version}");
			client.Timeout = TimeSpan.FromSeconds(15);

			using HttpResponseMessage response = await client.GetAsync(new Uri($"https://github.com/{SharedInfo.GithubRepo}/releases/latest"), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();

			// Stamp only after a successful response - when offline we want to retry on the next launch
			try {
				Directory.CreateDirectory(Path.GetDirectoryName(stampPath)!);
				await File.WriteAllTextAsync(stampPath, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)).ConfigureAwait(false);
			} catch (Exception e) {
				TickrApp.TickrLogger.LogGenericDebuggingException(e);
			}

			Uri? releaseUri = response.RequestMessage?.RequestUri;
			string marker = $"/{SharedInfo.GithubRepo}/releases/tag/";

			if (releaseUri == null || !releaseUri.AbsolutePath.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			string versionText = Uri.UnescapeDataString(releaseUri.AbsolutePath[marker.Length..]).Trim('/').TrimStart('v', 'V');
			Version localVersion = ReadInstalledVersion();

			if (!Version.TryParse(versionText, out Version? remoteVersion) || (remoteVersion <= localVersion)) {
				return;
			}

			TickrApp.TickrLogger.LogGenericInfo($"Mandatory update available: v{localVersion} -> v{remoteVersion}; restarting through launcher");
			await RequestUpdateRestart().ConfigureAwait(false);
		} catch (Exception e) {
			// No network, GitHub down, rate limit - all normal, stay quiet
			TickrApp.TickrLogger.LogGenericDebuggingException(e);
		}
	}

	private static Version ReadInstalledVersion() {
		try {
			string path = Path.Combine(SharedInfo.HomeDirectory, "version.txt");

			if (File.Exists(path) && Version.TryParse(File.ReadAllText(path).Trim().TrimStart('v', 'V'), out Version? version)) {
				return version;
			}
		} catch (Exception e) {
			TickrApp.TickrLogger.LogGenericDebuggingException(e);
		}

		return SharedInfo.Version;
	}
}
