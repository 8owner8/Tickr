// Copyright 2015-2026 Tickr
// Contact: support@TickrApp.dev
// |
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// |
// http://www.apache.org/licenses/LICENSE-2.0
// |
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tickr.Launcher;
using Tickr.Launcher.Models;

#pragma warning disable CA1812 // False positive, the class is used during MSTest
[TestClass]
internal sealed class LauncherUpdateManager {
	[TestMethod]
	internal void ApplyUpdateMovesFilesAndSkipsLauncher() {
		string baseDir = CreateTempDirectory();
		string tempDir = CreateTempDirectory();

		try {
			WriteFile(tempDir, "Tickr.dll", "new app");
			WriteFile(tempDir, "www/style.css", "new style");
			WriteFile(tempDir, "Tickr.exe", "new launcher");
			WriteFile(baseDir, "Tickr.dll", "old app");

			Manifest manifest = new() {
				Version = "2.0.0",
				Files = [
					MakeManifestFile("Tickr.dll", "new app"),
					MakeManifestFile("www/style.css", "new style"),
					MakeManifestFile("Tickr.exe", "new launcher")
				]
			};

			bool selfUpdatePending = UpdateManager.ApplyUpdate(manifest, baseDir, tempDir);

			Assert.IsTrue(selfUpdatePending, "Tickr.exe in the delta must be reported as a pending self-update");
			Assert.AreEqual("new app", File.ReadAllText(Path.Combine(baseDir, "Tickr.dll")), "Updated file must replace the old one");
			Assert.AreEqual("new style", File.ReadAllText(Path.Combine(baseDir, "www", "style.css")), "Subdirectories must be created on apply");
			Assert.IsFalse(File.Exists(Path.Combine(baseDir, "Tickr.exe")), "Tickr.exe must not be moved by ApplyUpdate - that is ApplySelfUpdate's job");
			Assert.AreEqual("2.0.0", File.ReadAllText(Path.Combine(baseDir, UpdateManager.VersionFileName)), "version.txt must be stamped with the new version");
		} finally {
			DeleteDirectory(baseDir);
			DeleteDirectory(tempDir);
		}
	}

	[TestMethod]
	internal async Task ComputeDeltaIncludesMissingFile() {
		string baseDir = CreateTempDirectory();

		try {
			WriteFile(baseDir, "a.dll", "aaa");

			Manifest manifest = new() {
				Version = "2.0.0",
				Files = [MakeManifestFile("a.dll", "aaa"), MakeManifestFile("b.dll", "bbb")]
			};

			List<ManifestFile> delta = await UpdateManager.ComputeDeltaAsync(manifest, baseDir).ConfigureAwait(false);

			Assert.AreEqual(1, delta.Count);
			Assert.AreEqual("b.dll", delta[0].Path, "The missing file must be part of the delta");
		} finally {
			DeleteDirectory(baseDir);
		}
	}

	[TestMethod]
	internal async Task ComputeDeltaIsEmptyWhenAllFilesMatch() {
		string baseDir = CreateTempDirectory();

		try {
			WriteFile(baseDir, "a.dll", "aaa");
			WriteFile(baseDir, "www/style.css", "body { }");

			Manifest manifest = new() {
				Version = "2.0.0",
				Files = [MakeManifestFile("a.dll", "aaa"), MakeManifestFile("www/style.css", "body { }")]
			};

			List<ManifestFile> delta = await UpdateManager.ComputeDeltaAsync(manifest, baseDir).ConfigureAwait(false);

			Assert.AreEqual(0, delta.Count);
		} finally {
			DeleteDirectory(baseDir);
		}
	}

	[TestMethod]
	internal async Task ComputeDeltaReturnsOnlyModifiedFile() {
		string baseDir = CreateTempDirectory();

		try {
			WriteFile(baseDir, "a.dll", "aaa");
			WriteFile(baseDir, "b.dll", "MODIFIED");
			WriteFile(baseDir, "c.dll", "ccc");

			Manifest manifest = new() {
				Version = "2.0.0",
				Files = [MakeManifestFile("a.dll", "aaa"), MakeManifestFile("b.dll", "bbb"), MakeManifestFile("c.dll", "ccc")]
			};

			List<ManifestFile> delta = await UpdateManager.ComputeDeltaAsync(manifest, baseDir).ConfigureAwait(false);

			Assert.AreEqual(1, delta.Count, "Only the modified file may appear in the delta");
			Assert.AreEqual("b.dll", delta[0].Path);
		} finally {
			DeleteDirectory(baseDir);
		}
	}

	[TestMethod]
	internal async Task DownloadFilesRollsBackOnHashMismatch() {
		string tempDir = CreateTempDirectory();

		// The server deliberately serves content that does NOT match the manifest hash
		using (StartFileServer("tampered content", out string url)) {
			try {
				ManifestFile file = MakeManifestFile("a.dll", "expected content");
				file.DownloadUrl = url;

				UpdateInfo update = new() { Version = "2.0.0", ManifestUrl = url, Assets = new Dictionary<string, string>() };

				bool result = await UpdateManager.DownloadFilesAsync([file], tempDir, update).ConfigureAwait(false);

				Assert.IsFalse(result, "A SHA256 mismatch must fail the download");
				Assert.IsFalse(Directory.Exists(tempDir), "The temp directory must be wiped after a failed download");
			} finally {
				DeleteDirectory(tempDir);
			}
		}
	}

	[TestMethod]
	internal async Task DownloadFilesSucceedsWithCorrectHash() {
		string tempDir = CreateTempDirectory();

		using (StartFileServer("expected content", out string url)) {
			try {
				ManifestFile file = MakeManifestFile("sub/a.dll", "expected content");
				file.DownloadUrl = url;

				UpdateInfo update = new() { Version = "2.0.0", ManifestUrl = url, Assets = new Dictionary<string, string>() };

				bool result = await UpdateManager.DownloadFilesAsync([file], tempDir, update).ConfigureAwait(false);

				Assert.IsTrue(result, "Matching SHA256 must accept the download");
				Assert.AreEqual("expected content", await File.ReadAllTextAsync(Path.Combine(tempDir, "sub", "a.dll")).ConfigureAwait(false), "The file must land in temp with its subdirectory");
			} finally {
				DeleteDirectory(tempDir);
			}
		}
	}

	[TestMethod]
	internal void GitHubReleaseJsonDeserializesTagAndAssets() {
		// Regression: DTO properties must be public - System.Text.Json ignores internal ones and produces an empty object
		const string json = """
		{
		  "tag_name": "v1.0.0",
		  "assets": [
		    { "name": "manifest.json", "browser_download_url": "https://github.com/8owner8/Tickr/releases/download/v1.0.0/manifest.json" },
		    { "name": "Tickr.exe", "browser_download_url": "https://github.com/8owner8/Tickr/releases/download/v1.0.0/Tickr.exe" }
		  ]
		}
		""";

		GitHubRelease? release = JsonSerializer.Deserialize(json, LauncherJsonContext.Default.GitHubRelease);

		Assert.IsNotNull(release);
		Assert.AreEqual("v1.0.0", release.TagName);
		Assert.AreEqual(2, release.Assets.Length);
		Assert.AreEqual("Tickr.exe", release.Assets[1].Name);
		Assert.IsTrue(release.Assets[1].DownloadUrl.EndsWith("/Tickr.exe", StringComparison.Ordinal));
	}

	[TestMethod]
	internal void ManifestJsonDeserializesFiles() {
		const string json = """
		{
		  "version": "1.0.0",
		  "files": [
		    { "path": "Tickr.dll", "sha256": "abc123", "size": 12345, "downloadUrl": "https://example.com/Tickr.dll" },
		    { "path": "www/style.css", "sha256": "def456", "size": 100, "downloadUrl": "https://example.com/www__style.css" }
		  ]
		}
		""";

		Manifest? manifest = JsonSerializer.Deserialize(json, LauncherJsonContext.Default.Manifest);

		Assert.IsNotNull(manifest);
		Assert.AreEqual("1.0.0", manifest.Version);
		Assert.AreEqual(2, manifest.Files.Length);
		Assert.AreEqual("www/style.css", manifest.Files[1].Path);
		Assert.AreEqual("def456", manifest.Files[1].Sha256);
		Assert.AreEqual(100, manifest.Files[1].Size);
		Assert.AreEqual("https://example.com/www__style.css", manifest.Files[1].DownloadUrl);
	}

	[TestMethod]
	internal async Task DownloadFilesHandlesManyFilesInParallel() {
		string tempDir = CreateTempDirectory();

		using (StartFileServer("expected content", out string url)) {
			try {
				// 20 files through a single-file server - exercises the parallel download path
				List<ManifestFile> files = [];

				for (int i = 0; i < 20; i++) {
					ManifestFile file = MakeManifestFile($"dir{i % 5}/file{i}.dll", "expected content");
					file.DownloadUrl = url;
					files.Add(file);
				}

				UpdateInfo update = new() { Version = "2.0.0", ManifestUrl = url, Assets = new Dictionary<string, string>() };

				bool result = await UpdateManager.DownloadFilesAsync(files, tempDir, update).ConfigureAwait(false);

				Assert.IsTrue(result, "All parallel downloads must succeed");

				foreach (ManifestFile file in files) {
					string localPath = Path.Combine(tempDir, file.Path.Replace('/', Path.DirectorySeparatorChar));
					Assert.IsTrue(File.Exists(localPath), $"Missing downloaded file: {file.Path}");
				}
			} finally {
				DeleteDirectory(tempDir);
			}
		}
	}

	[TestMethod]
	internal void EncodeAssetNameMatchesManifestGeneratorConvention() {
		// scripts/generate-manifest.ps1 encodes "www/style.css" as the flat asset name "www__style.css" - both sides must agree
		Assert.AreEqual("www__style.css", UpdateManager.EncodeAssetName("www/style.css"));
		Assert.AreEqual("Tickr.dll", UpdateManager.EncodeAssetName("Tickr.dll"));
	}

	[TestMethod]
	internal void ExtractReleaseTagAcceptsOnlyTickrGitHubReleaseUrls() {
		Assert.AreEqual("v1.2.3", UpdateManager.ExtractReleaseTag(new Uri("https://github.com/8owner8/Tickr/releases/tag/v1.2.3")));
		Assert.IsNull(UpdateManager.ExtractReleaseTag(new Uri("https://github.com/8owner8/Tickr/releases/latest")));
		Assert.IsNull(UpdateManager.ExtractReleaseTag(new Uri("https://example.com/8owner8/Tickr/releases/tag/v1.2.3")));
		Assert.IsNull(UpdateManager.ExtractReleaseTag(new Uri("https://github.com/other/repo/releases/tag/v1.2.3")));
	}

	private static string CreateTempDirectory() {
		string path = Path.Combine(Path.GetTempPath(), $"TickrTests_{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);

		return path;
	}

	private static void DeleteDirectory(string path) {
		try {
			if (Directory.Exists(path)) {
				Directory.Delete(path, true);
			}
		} catch (IOException) {
			// Best effort - the OS temp cleaner will get it eventually
		} catch (UnauthorizedAccessException) {
			// Best effort - the OS temp cleaner will get it eventually
		}
	}

	private static ManifestFile MakeManifestFile(string path, string content) {
		byte[] bytes = Encoding.UTF8.GetBytes(content);

		return new ManifestFile { Path = path, Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), Size = bytes.LongLength };
	}

	/// <summary>
	/// Minimal HTTP/1.1 file server on a raw TcpListener. HttpListener cannot be used here:
	/// it requires administrator rights (urlacl reservation) even for loopback.
	/// Disposing the listener unblocks the accept loop (SocketException) and shuts the server down.
	/// </summary>
	private static TcpListener StartFileServer(string content, out string url) {
		byte[] body = Encoding.UTF8.GetBytes(content);

		TcpListener listener = new(IPAddress.Loopback, 0);
		listener.Start();

		int port = ((IPEndPoint) listener.LocalEndpoint).Port;
		url = $"http://127.0.0.1:{port}/file";

		_ = Task.Run(
			async () => {
				try {
					while (true) {
						TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
						_ = RespondAsync(client, body);
					}
				} catch (ObjectDisposedException) {
					// Server is shutting down
				} catch (SocketException) {
					// Server is shutting down
				}
			}
		);

		return listener;
	}

	private static async Task RespondAsync(TcpClient client, byte[] body) {
		try {
			using (client) {
				NetworkStream stream = client.GetStream();
				byte[] buffer = new byte[4096];
				StringBuilder request = new();

				// Consume the request headers - the content does not matter, every GET gets the same file
				while (!request.ToString().Contains("\r\n\r\n", StringComparison.Ordinal)) {
					int read = await stream.ReadAsync(buffer).ConfigureAwait(false);

					if (read == 0) {
						return;
					}

					request.Append(Encoding.ASCII.GetString(buffer, 0, read));
				}

				string header = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {body.LongLength}\r\nConnection: close\r\n\r\n";
				await stream.WriteAsync(Encoding.ASCII.GetBytes(header)).ConfigureAwait(false);
				await stream.WriteAsync(body).ConfigureAwait(false);
			}
		} catch (IOException) {
			// Client went away mid-response - irrelevant for the test
		} catch (SocketException) {
			// Client went away mid-response - irrelevant for the test
		}
	}

	private static void WriteFile(string baseDir, string relativePath, string content) {
		string path = Path.Combine(baseDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? baseDir);
		File.WriteAllText(path, content);
	}
}
