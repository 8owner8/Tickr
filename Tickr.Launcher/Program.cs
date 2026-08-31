// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — Program.cs  (entry point: update → launch → watch restart)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tickr.Launcher.Models;

namespace Tickr.Launcher;

internal static class Program {
	private const string TickrDllName = "Tickr.dll";
	private const string RestartPendingFileName = "restart.pending";
	private enum StartupUpdateResult : byte { NoUpdate, Applied, Failed }

	[STAThread]
	private static async Task<int> Main() {
		string baseDirectory = AppContext.BaseDirectory;

		ApplicationConfiguration.Initialize();

		// Leftovers from a previous launcher self-update - the new instance cleans them up
		try {
			string oldExe = Path.Combine(baseDirectory, UpdateManager.LauncherExeName + ".old");

			if (File.Exists(oldExe)) {
				File.Delete(oldExe);
				LauncherLog.Info("Removed stale Tickr.exe.old after self-update");
			}

			string selfRestartPending = Path.Combine(baseDirectory, UpdateManager.SelfRestartPendingFileName);

			if (File.Exists(selfRestartPending)) {
				File.Delete(selfRestartPending);
				LauncherLog.Info("Started after a self-update, removed self_restart.pending");
			}
		} catch (Exception e) {
			LauncherLog.Error("Failed to clean up self-update leftovers", e);
		}

		LauncherLog.Info($"Launcher started in {baseDirectory}");

		// The outer loop is entered at most twice in practice: initial run, and once more after restart.pending
		while (true) {
			Version localVersion = ReadLocalVersion(baseDirectory);
			LauncherLog.Info($"Local version: {localVersion}");

			bool appMissing = !File.Exists(Path.Combine(baseDirectory, TickrDllName));
			StartupUpdateResult updateResult = RunStartupUpdateWithUi(baseDirectory, localVersion);

			if (updateResult == StartupUpdateResult.Failed) {
				LauncherLog.Error("Mandatory update check or installation failed, refusing to launch an outdated app");
				MessageBox.Show("Tickr could not verify or install the required update. Check your internet connection and try again.", "Tickr update required", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return 1;
			}

			if (appMissing && updateResult == StartupUpdateResult.NoUpdate) {
				LauncherLog.Error("Tickr.dll is missing and no update is available, exiting");
				MessageBox.Show("Application files are missing and no downloadable update is available.", "Tickr", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return 1;
			}

			// The app refuses to start without its config directory. That directory ships empty in the
			// distribution (only a zero-byte .gitkeep placeholder, which can't be a release asset),
			// so the launcher materializes the runtime dirs itself before starting the app.
			foreach (string dir in new[] { "config", "logs", "plugins" }) {
				Directory.CreateDirectory(Path.Combine(baseDirectory, dir));
			}

			string dllPath = Path.Combine(baseDirectory, TickrDllName);

			if (!File.Exists(dllPath)) {
				LauncherLog.Error("Tickr.dll is still missing after the update cycle, exiting");
				MessageBox.Show("The downloaded update does not contain the main application file (Tickr.dll).", "Tickr", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return 1;
			}

			Process? child = StartApp(baseDirectory, dllPath);

			if (child == null) {
				return 1;
			}

			using RestartWatcher watcher = new(baseDirectory, RestartPendingFileName);
			watcher.Start();

			Task childExit = child.WaitForExitAsync();
			Task completed = await Task.WhenAny(childExit, watcher.RestartTask).ConfigureAwait(false);

			if (completed == watcher.RestartTask || watcher.ConsumePending()) {
				LauncherLog.Info("restart.pending received, restarting the app");

				if (!child.HasExited) {
					try {
						child.Kill();
						await child.WaitForExitAsync().ConfigureAwait(false);
					} catch (Exception e) {
						LauncherLog.Error("Failed to stop the running app", e);
					}
				}

				child.Dispose();

				continue;
			}

			// The app exited on its own - the launcher's job is done
			int exitCode = child.ExitCode;
			child.Dispose();

			if (exitCode != 0) {
				// The app crashed or failed to start (e.g. missing .NET runtime) - surface it instead of vanishing silently
				string childErrors = ConsumeChildErrors();
				LauncherLog.Error($"Tickr.dll exited with code {exitCode}");
				MessageBox.Show($"The application failed to start (exit code {exitCode}). Make sure the .NET 10 Runtime (Desktop + ASP.NET Core) is installed.{(string.IsNullOrEmpty(childErrors) ? "" : $"{Environment.NewLine}{Environment.NewLine}{childErrors}")}", "Tickr", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return 1;
			}

			return 0;
		}
	}

	private static readonly System.Collections.Concurrent.ConcurrentQueue<string> ChildErrorLines = new();
	private const int MaxRetainedChildErrorLines = 15;

	private static void OnChildErrorLine(string? line) {
		if (string.IsNullOrEmpty(line)) {
			return;
		}

		LauncherLog.Info($"[app] {line}");
		ChildErrorLines.Enqueue(line);

		while (ChildErrorLines.Count > MaxRetainedChildErrorLines && ChildErrorLines.TryDequeue(out _)) { }
	}

	private static string ConsumeChildErrors() {
		string result = string.Join(Environment.NewLine, ChildErrorLines);

		while (ChildErrorLines.TryDequeue(out _)) { }

		return result;
	}

	/// <summary>
	/// Shows the progress UI immediately, checks for a release, and installs it before the app can start.
	/// Any check/download/install failure blocks startup so an outdated version is never launched.
	/// </summary>
	private static StartupUpdateResult RunStartupUpdateWithUi(string baseDirectory, Version localVersion) {
		using LauncherWindow window = new();
		StartupUpdateResult result = StartupUpdateResult.Failed;

		window.Shown += async (_, _) => {
			string tempDirectory = Path.Combine(baseDirectory, UpdateManager.TempDirectoryName);

			try {
				window.SetStatus("Checking for required updates…");
				UpdateInfo? update = await UpdateManager.CheckForUpdateAsync(localVersion).ConfigureAwait(true);

				if (update == null) {
					LauncherLog.Info("No update available");
					window.SetStatus("Tickr is up to date");
					result = StartupUpdateResult.NoUpdate;

					return;
				}

				LauncherLog.Info($"Mandatory update available: {update.Version}");
				window.SetStatus("Downloading manifest…");
				Manifest manifest = await UpdateManager.DownloadManifestAsync(update.ManifestUrl).ConfigureAwait(true); // UI context: pumped by Application.Run below

				window.SetStatus("Checking files…");
				List<ManifestFile> delta = await UpdateManager.ComputeDeltaAsync(manifest, baseDirectory).ConfigureAwait(true);

				if (delta.Count == 0) {
					// Files already match - just stamp the version and move on
					LauncherLog.Info("All files already match the manifest, no download needed");
					await File.WriteAllTextAsync(Path.Combine(baseDirectory, UpdateManager.VersionFileName), manifest.Version).ConfigureAwait(true);
					result = StartupUpdateResult.Applied;

					return;
				}

				LauncherLog.Info($"Delta: {delta.Count} file(s) to download");

				bool downloaded = await UpdateManager.DownloadFilesAsync(delta, tempDirectory, update, window.CreateProgressReporter()).ConfigureAwait(true);

				if (!downloaded) {
					window.SetStatus("Required update download failed");
					await Task.Delay(3000).ConfigureAwait(true);

					return;
				}

				window.SetStatus("Installing update…");
				bool selfUpdatePending = UpdateManager.ApplyUpdate(manifest, baseDirectory, tempDirectory);

				if (selfUpdatePending) {
					window.SetStatus("Updating launcher…");
					UpdateManager.ApplySelfUpdate(baseDirectory, tempDirectory); // Never returns - the process exits and the new launcher takes over
				}

				UpdateManager.CleanupTemp(tempDirectory);
				LauncherLog.Info($"Update to {manifest.Version} applied");
				result = StartupUpdateResult.Applied;
			} catch (Exception e) {
				LauncherLog.Error("Update failed", e);
				UpdateManager.CleanupTemp(tempDirectory);
				window.SetStatus("Required update failed");
				await Task.Delay(3000).ConfigureAwait(true);
			} finally {
				window.Close();
			}
		};

		Application.Run(window);

		return result;
	}

	private static Process? StartApp(string baseDirectory, string dllPath) {
		try {
			Process child = Process.Start(
				new ProcessStartInfo {
					FileName = "dotnet",
					Arguments = $"\"{dllPath}\"",
					WorkingDirectory = baseDirectory,
					UseShellExecute = false,
					CreateNoWindow = true,
					// Capture stderr so a startup failure (e.g. missing runtime) is diagnosable from launcher.log
					RedirectStandardError = true,
					StandardErrorEncoding = System.Text.Encoding.UTF8
				}
			)!;

			child.ErrorDataReceived += (_, e) => OnChildErrorLine(e.Data);
			child.BeginErrorReadLine();

			return child;
		} catch (Exception e) {
			LauncherLog.Error("Failed to start Tickr.dll", e);
			MessageBox.Show("Failed to start the application (dotnet Tickr.dll). Make sure .NET 10 Runtime is installed.", "Tickr", MessageBoxButtons.OK, MessageBoxIcon.Error);

			return null;
		}
	}

	private static Version ReadLocalVersion(string baseDirectory) {
		try {
			string path = Path.Combine(baseDirectory, UpdateManager.VersionFileName);

			if (File.Exists(path)) {
				string text = File.ReadAllText(path).Trim().TrimStart('v', 'V');

				if (Version.TryParse(text, out Version? version)) {
					return version;
				}
			}
		} catch (Exception e) {
			LauncherLog.Error("Failed to read version.txt", e);
		}

		// First run or unreadable stamp - everything counts as an update
		return new Version(0, 0, 0);
	}
}
