// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — LauncherLog.cs  (tiny append-only logger → launcher.log)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Globalization;
using System.IO;

namespace Tickr.Launcher;

internal static class LauncherLog {
	private static readonly object WriteLock = new();
	private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "launcher.log");

	internal static void Info(string message) => Write("INFO", message);

	internal static void Error(string message, Exception? exception = null) {
		Write("ERROR", message);

		if (exception != null) {
			Write("ERROR", exception.ToString());
		}
	}

	private static void Write(string level, string message) {
		try {
			lock (WriteLock) {
				File.AppendAllText(LogPath, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} [{level}] {message}{Environment.NewLine}");
			}
		} catch {
			// Logging must never take the launcher down
		}
	}
}
