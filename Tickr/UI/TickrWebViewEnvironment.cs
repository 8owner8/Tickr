// ─────────────────────────────────────────────────────────────────────────────
//  Tickr — TickrWebViewEnvironment.cs
//  Single shared WebView2 environment for all windows (main GUI, auth dialogs)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Tickr.UI;

internal static class TickrWebViewEnvironment {
	internal const string IpcHomeUrl = "http://127.0.0.1:1242/";

	private static readonly SemaphoreSlim Semaphore = new(1, 1);

	private static CoreWebView2Environment? SharedEnvironment;

	internal static async Task<CoreWebView2Environment> GetOrCreateAsync() {
		if (SharedEnvironment != null) {
			return SharedEnvironment;
		}

		await Semaphore.WaitAsync().ConfigureAwait(true);

		try {
			// A single stable user data folder shared by every window in this process.
			// Multiple environments with separate folders (especially in %TEMP%) are known to deadlock/fail silently, leaving the window black.
			string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tickr", "WebView2");
			Directory.CreateDirectory(userDataFolder);

			SharedEnvironment = await CoreWebView2Environment.CreateAsync(null, userDataFolder).ConfigureAwait(true);

			return SharedEnvironment;
		} finally {
			Semaphore.Release();
		}
	}
}
