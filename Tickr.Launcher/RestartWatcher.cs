// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — RestartWatcher.cs  (watches for restart.pending from Tickr)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Threading.Tasks;

namespace Tickr.Launcher;

/// <summary>
/// Watches the app directory for the restart.pending sentinel file created by the main app
/// when the user accepts an update. Completes <see cref="RestartTask"/> once that happens.
/// </summary>
internal sealed class RestartWatcher : IDisposable {
	private readonly FileSystemWatcher Watcher;
	private readonly string PendingFilePath;
	private readonly TaskCompletionSource RestartSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

	internal RestartWatcher(string directory, string fileName) {
		ArgumentException.ThrowIfNullOrEmpty(directory);
		ArgumentException.ThrowIfNullOrEmpty(fileName);

		PendingFilePath = Path.Combine(directory, fileName);

		Watcher = new FileSystemWatcher(directory, fileName) {
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
			IncludeSubdirectories = false
		};

		Watcher.Created += OnPendingFileAppeared;
		Watcher.Renamed += OnPendingFileAppeared;
	}

	/// <summary>Completes when restart.pending appears. Never faults, never cancels - the caller races it against the child process exit.</summary>
	internal Task RestartTask => RestartSignal.Task;

	internal void Start() {
		// A stale sentinel from a crashed run must not trigger an instant restart
		ConsumePending();
		Watcher.EnableRaisingEvents = true;
	}

	/// <summary>Deletes restart.pending if it exists. Returns true when a sentinel was consumed - used to close the race between the watcher event and the child process exiting.</summary>
	internal bool ConsumePending() {
		try {
			if (File.Exists(PendingFilePath)) {
				File.Delete(PendingFilePath);

				return true;
			}
		} catch (Exception e) {
			LauncherLog.Error("Failed to consume restart.pending", e);
		}

		return false;
	}

	public void Dispose() {
		Watcher.EnableRaisingEvents = false;
		Watcher.Created -= OnPendingFileAppeared;
		Watcher.Renamed -= OnPendingFileAppeared;
		Watcher.Dispose();
	}

	private void OnPendingFileAppeared(object sender, FileSystemEventArgs e) => RestartSignal.TrySetResult();
}
