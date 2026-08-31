// ─────────────────────────────────────────────────────────────────────────────
//  Tickr — UpdateToastWindow.cs  (borderless "update available" toast)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Windows.Forms;
using Tickr.Core;

namespace Tickr.UI;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Update toast strings are hardcoded English by design, matching the launcher UI")]
internal sealed class UpdateToastWindow : Form {
	private static readonly Color BackgroundColor = Color.FromArgb(0x05, 0x05, 0x0F);
	private static readonly Color ForegroundColor = Color.FromArgb(0xF0, 0xF0, 0xF5);
	private static readonly Color AccentColor = Color.FromArgb(0xC9, 0xFF, 0x47);
	private static readonly Color MutedColor = Color.FromArgb(0x8A, 0x8A, 0x99);

	private readonly Font TitleFont = new("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
	private readonly Font BodyFont = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	private readonly System.Windows.Forms.Timer AutoCloseTimer;

	private UpdateToastWindow(Version version, Form owner) {
		ArgumentNullException.ThrowIfNull(version);
		ArgumentNullException.ThrowIfNull(owner);

		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.Manual;
		ShowInTaskbar = false;
		TopMost = true;
		BackColor = BackgroundColor;
		ForeColor = ForegroundColor;
		ClientSize = new Size(380, 104);

		Rectangle workingArea = Screen.FromControl(owner).WorkingArea;
		Location = new Point(workingArea.Right - Width - 16, workingArea.Bottom - Height - 16);

		Panel accentBar = new() {
			Location = new Point(0, 0),
			Size = new Size(4, 104),
			BackColor = AccentColor
		};

		Label titleLabel = new() {
			Text = $"Update available: v{version}",
			Location = new Point(20, 12),
			Size = new Size(320, 20),
			Font = TitleFont,
			ForeColor = ForegroundColor,
			BackColor = Color.Transparent
		};

		Label bodyLabel = new() {
			Text = "Restart the app to install it.",
			Location = new Point(20, 34),
			Size = new Size(320, 18),
			Font = BodyFont,
			ForeColor = MutedColor,
			BackColor = Color.Transparent
		};

		Button updateButton = new() {
			Text = "Update now",
			Location = new Point(20, 60),
			Size = new Size(150, 30),
			FlatStyle = FlatStyle.Flat,
			BackColor = AccentColor,
			ForeColor = BackgroundColor,
			Font = BodyFont,
			Cursor = Cursors.Hand
		};

		updateButton.FlatAppearance.BorderSize = 0;
		updateButton.Click += OnUpdateClicked;

		Label closeLabel = new() {
			Text = "✕",
			Location = new Point(352, 8),
			Size = new Size(20, 20),
			Font = BodyFont,
			ForeColor = MutedColor,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleCenter,
			Cursor = Cursors.Hand
		};

		closeLabel.Click += (_, _) => Close();

		AutoCloseTimer = new System.Windows.Forms.Timer { Interval = 30000 };
		AutoCloseTimer.Tick += (_, _) => Close();

		Controls.Add(accentBar);
		Controls.Add(titleLabel);
		Controls.Add(bodyLabel);
		Controls.Add(updateButton);
		Controls.Add(closeLabel);
	}

	protected override bool ShowWithoutActivation => true;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "A shown non-modal Form is disposed by Close(), which the analyzer cannot see")]
	internal static void ShowToast(TickrMainWindow owner, Version version) {
		ArgumentNullException.ThrowIfNull(owner);
		ArgumentNullException.ThrowIfNull(version);

		if (owner.IsDisposed) {
			return;
		}

		UpdateToastWindow toast = new(version, owner);
		toast.Show(owner);
		toast.AutoCloseTimer.Start();
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			AutoCloseTimer.Dispose();
			TitleFont.Dispose();
			BodyFont.Dispose();
		}

		base.Dispose(disposing);
	}

	protected override void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);

		using Pen borderPen = new(Color.FromArgb(40, AccentColor));
		e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
	}

	private async void OnUpdateClicked(object? sender, EventArgs e) {
		// Hand over to the launcher: sentinel file first, graceful shutdown second
		AutoCloseTimer.Stop();
		await UpdateChecker.RequestUpdateRestart().ConfigureAwait(true);
	}
}
