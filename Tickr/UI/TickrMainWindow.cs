// ─────────────────────────────────────────────────────────────────────────────
//  Tickr — TickrMainWindow.cs  (Custom Titlebar + Borderless)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Tickr.Core;
using Tickr.IPC;

namespace Tickr.UI;

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
internal sealed partial class TickrMainWindow : Form {
	private readonly WebView2 WebView;
	private readonly NotifyIcon TrayIcon;

	// ── Win32 ────────────────────────────────────────────────────────────────
	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool ReleaseCapture();

	// PostMessage is non-blocking — crucial for drag to work from WebView2 JS events
	[LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	private const uint WM_NCLBUTTONDOWN = 0xA1;
	private const uint WM_NCHITTEST     = 0x0084;
	private const int  HTCAPTION        = 0x2;
	private const int  HTLEFT           = 10;
	private const int  HTRIGHT          = 11;
	private const int  HTTOP            = 12;
	private const int  HTTOPLEFT        = 13;
	private const int  HTTOPRIGHT       = 14;
	private const int  HTBOTTOM         = 15;
	private const int  HTBOTTOMLEFT     = 16;
	private const int  HTBOTTOMRIGHT    = 17;
	private const int  BORDER           = 5;

	internal TickrMainWindow() {
		Text            = SharedInfo.Tickr;
		Size            = new Size(1400, 900);
		MinimumSize     = new Size(1100, 700);
		StartPosition   = FormStartPosition.CenterScreen;
		BackColor       = Color.FromArgb(10, 10, 10);
		ShowIcon        = true;
		FormBorderStyle = FormBorderStyle.None;
		DoubleBuffered  = true;

		// Load icon directly from file (bypasses Windows icon cache)
		foreach (string p in new[] {
			Path.Combine(AppContext.BaseDirectory, "resources", "Tickr.ico"),
			Path.Combine(AppContext.BaseDirectory, "Tickr.ico")
		}) {
			if (File.Exists(p)) {
				try { Icon = new Icon(p); break; } catch { /* ignored */ }
			}
		}

		WebView = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.FromArgb(10, 10, 10) };
		Controls.Add(WebView);

		TrayIcon = new NotifyIcon { Text = SharedInfo.Tickr, Icon = Icon ?? SystemIcons.Application, Visible = true };
		ContextMenuStrip trayMenu = new();
		trayMenu.Items.Add("Open Tickr", null, (_, _) => { Show(); WindowState = FormWindowState.Normal; BringToFront(); });
		trayMenu.Items.Add("-");
		trayMenu.Items.Add("Exit", null, (_, _) => { TrayIcon.Visible = false; Application.Exit(); });
		TrayIcon.ContextMenuStrip = trayMenu;
		TrayIcon.DoubleClick += (_, _) => { Show(); WindowState = FormWindowState.Normal; BringToFront(); };
	}

	protected override async void OnShown(EventArgs e) {
		base.OnShown(e);

		// The launcher owns installation and progress UI. If a release appears while Tickr is
		// already running, hand control back to it automatically instead of asking via a button.
		UpdateChecker.CheckInBackground();

		await InitWebViewAsync().ConfigureAwait(true);
	}

	private async Task InitWebViewAsync() {
		try {
			// Use the single shared environment (one user data folder for the whole process)
			CoreWebView2Environment env = await TickrWebViewEnvironment.GetOrCreateAsync().ConfigureAwait(true);
			await WebView.EnsureCoreWebView2Async(env).ConfigureAwait(true);

			WebView.CoreWebView2.Settings.AreDevToolsEnabled            = Debugging.IsUserDebugging;
			WebView.CoreWebView2.Settings.IsStatusBarEnabled            = false;
			WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = Debugging.IsUserDebugging;
			WebView.CoreWebView2.WebMessageReceived                    += OnWebMessage;
			WebView.CoreWebView2.NavigationStarting                    += OnMainNavigationStarting;
			WebView.CoreWebView2.NewWindowRequested                    += OnNewWindowRequested;

			// Prefer loading the GUI from our own IPC server (same origin, no CORS hassle), fall back to file:// if IPC is not available
			string target = await ResolveHomePageAsync().ConfigureAwait(true);

			WebView.CoreWebView2.Navigate(target);
		} catch (Exception ex) {
			TickrApp.TickrLogger.LogGenericException(ex);
			MessageBox.Show("WebView2 Error: " + ex.Message, "Tickr", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private static async Task<string> ResolveHomePageAsync() {
		string indexPath = Path.Combine(AppContext.BaseDirectory, "www", "index.html");

		// IPC server is started asynchronously on a background thread, give it some time to come up
		for (byte i = 0; i < 20; i++) {
			if (TickrKestrel.IsRunning) {
				return TickrWebViewEnvironment.IpcHomeUrl;
			}

			await Task.Delay(500).ConfigureAwait(true);
		}

		return File.Exists(indexPath) ? new Uri(indexPath).AbsoluteUri : TickrWebViewEnvironment.IpcHomeUrl;
	}

	private void OnMainNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e) {
		if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri? uri) ||
			!((uri.Scheme == Uri.UriSchemeFile) || ((uri.Scheme == Uri.UriSchemeHttp) && uri.IsLoopback))) {
			e.Cancel = true;
		}
	}

	private static void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e) {
		e.Handled = true;

		if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps) ||
			!(uri.Host.EndsWith(".steampowered.com", StringComparison.OrdinalIgnoreCase) || string.Equals(uri.Host, "steampowered.com", StringComparison.OrdinalIgnoreCase) ||
			  uri.Host.EndsWith(".steamcommunity.com", StringComparison.OrdinalIgnoreCase) || string.Equals(uri.Host, "steamcommunity.com", StringComparison.OrdinalIgnoreCase))) {
			return;
		}

		Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
	}

	// ── JS → C# bridge ───────────────────────────────────────────────────────
	private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
		string raw = e.TryGetWebMessageAsString();

		switch (raw) {
			case "titlebar:minimize":
				WindowState = FormWindowState.Minimized;
				return;

			case "titlebar:maximize":
				WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
				return;

			case "titlebar:close":
				TrayIcon.Visible = false;
				Application.Exit();
				return;

			case "titlebar:drag":
				if (WindowState == FormWindowState.Maximized) return;
				ReleaseCapture();
				PostMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
				return;

			default:
				if (raw.StartsWith("titlebar:resize:", StringComparison.Ordinal)) {
					StartResize(raw["titlebar:resize:".Length..]);
					return;
				}

				break;
		}
	}

	// ── Borderless resize via WM_NCLBUTTONDOWN (triggered from JS edge zones) ─
	private void StartResize(string direction) {
		if (WindowState == FormWindowState.Maximized) {
			return;
		}

		int hitTest = direction switch {
			"left"        => HTLEFT,
			"right"       => HTRIGHT,
			"top"         => HTTOP,
			"bottom"      => HTBOTTOM,
			"topleft"     => HTTOPLEFT,
			"topright"    => HTTOPRIGHT,
			"bottomleft"  => HTBOTTOMLEFT,
			"bottomright" => HTBOTTOMRIGHT,
			_ => 0
		};

		if (hitTest == 0) {
			return;
		}

		ReleaseCapture();
		PostMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)hitTest, IntPtr.Zero);
	}

	// ── Borderless resize via WM_NCHITTEST ───────────────────────────────────
	protected override void WndProc(ref Message m) {
		if (m.Msg == (int)WM_NCHITTEST && WindowState != FormWindowState.Maximized) {
			Point cur = PointToClient(Cursor.Position);
			bool left   = cur.X <= BORDER;
			bool right  = cur.X >= ClientSize.Width  - BORDER;
			bool top    = cur.Y <= BORDER;
			bool bottom = cur.Y >= ClientSize.Height - BORDER;

			if      (top && left)    m.Result = (IntPtr)HTTOPLEFT;
			else if (top && right)   m.Result = (IntPtr)HTTOPRIGHT;
			else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
			else if (bottom && right)m.Result = (IntPtr)HTBOTTOMRIGHT;
			else if (left)           m.Result = (IntPtr)HTLEFT;
			else if (right)          m.Result = (IntPtr)HTRIGHT;
			else if (top)            m.Result = (IntPtr)HTTOP;
			else if (bottom)         m.Result = (IntPtr)HTBOTTOM;
			else base.WndProc(ref m);
			return;
		}
		base.WndProc(ref m);
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			TrayIcon.Visible = false;
			TrayIcon.Dispose();
			if (WebView.CoreWebView2 != null) {
				WebView.CoreWebView2.WebMessageReceived -= OnWebMessage;
				WebView.CoreWebView2.NavigationStarting -= OnMainNavigationStarting;
				WebView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
			}

			WebView.Dispose();
		}
		base.Dispose(disposing);
	}
}
