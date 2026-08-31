// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — LauncherWindow.cs  (borderless update progress window)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Tickr.Launcher;

[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "StatusLabel and ProgressBarControl are owned by the Controls collection, which the base Form disposes")]
internal sealed class LauncherWindow : Form {
	private static readonly Color BackgroundColor = Color.FromArgb(0x05, 0x05, 0x0F);
	private static readonly Color ForegroundColor = Color.FromArgb(0xF0, 0xF0, 0xF5);
	private static readonly Color AccentColor = Color.FromArgb(0xC9, 0xFF, 0x47);
	private static readonly Color TrackColor = Color.FromArgb(0x1C, 0x1C, 0x2E);
	private static readonly Color MutedColor = Color.FromArgb(0x8A, 0x8A, 0x99);

	private readonly Label StatusLabel;
	private readonly AccentProgressBar ProgressBarControl;

	internal LauncherWindow() {
		Text = "Tickr";
		ClientSize = new Size(400, 110);
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.None;
		BackColor = BackgroundColor;
		ForeColor = ForegroundColor;
		ShowIcon = false;
		ShowInTaskbar = false;
		TopMost = true;
		DoubleBuffered = true;
		MaximizeBox = false;
		MinimizeBox = false;
		ControlBox = false;

		PictureBox logo = new() {
			Location = new Point(16, 14),
			Size = new Size(32, 32),
			SizeMode = PictureBoxSizeMode.Zoom
		};

		Image? logoImage = LoadLogoImage();

		if (logoImage != null) {
			logo.Image = logoImage;
		} else {
			// Accent square as a minimal fallback when the logo is unavailable
			logo.Paint += static (_, e) => {
				using SolidBrush brush = new(AccentColor);
				e.Graphics.FillRectangle(brush, 0, 0, 32, 32);
			};
		}

		Label titleLabel = new() {
			Text = "Tickr",
			Location = new Point(58, 16),
			AutoSize = true,
			Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = ForegroundColor,
			BackColor = Color.Transparent
		};

		StatusLabel = new Label {
			Text = "Checking for updates…",
			Location = new Point(16, 56),
			Size = new Size(368, 18),
			Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
			ForeColor = MutedColor,
			BackColor = Color.Transparent
		};

		ProgressBarControl = new AccentProgressBar {
			Location = new Point(16, 82),
			Size = new Size(368, 8)
		};

		Controls.Add(logo);
		Controls.Add(titleLabel);
		Controls.Add(StatusLabel);
		Controls.Add(ProgressBarControl);
	}

	internal IProgress<UpdateProgress> CreateProgressReporter() => new Progress<UpdateProgress>(OnProgress);

	internal void SetStatus(string status) {
		if (InvokeRequired) {
			BeginInvoke(() => SetStatus(status));

			return;
		}

		StatusLabel.Text = status;
	}

	protected override void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);

		// Thin accent frame so the borderless window reads as a deliberate surface, not a rendering glitch
		using Pen pen = new(Color.FromArgb(40, AccentColor));
		e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
	}

	private void OnProgress(UpdateProgress progress) {
		if (progress.Total <= 0) {
			return;
		}

		if (progress.Current < progress.Total) {
			StatusLabel.Text = $"Downloaded {progress.Current}/{progress.Total}: {progress.FileName}";
		} else {
			StatusLabel.Text = $"Downloaded {progress.Total}/{progress.Total}";
		}

		ProgressBarControl.Ratio = (double) progress.Current / progress.Total;
	}

	private static Image? LoadLogoImage() {
		// Embedded resource works even on the very first run, before anything is downloaded
		try {
			Assembly assembly = Assembly.GetExecutingAssembly();

			using Stream? stream = assembly.GetManifestResourceStream("tickr-logo.jpg");

			if (stream != null) {
				return Image.FromStream(stream);
			}
		} catch {
			// Fall through to the file-based lookup
		}

		foreach (string candidate in new[] { "tickr-logo.jpg", Path.Combine("resources", "Tickr.jpg"), "Tickr.jpg" }) {
			string path = Path.Combine(AppContext.BaseDirectory, candidate);

			if (File.Exists(path)) {
				try {
					return Image.FromFile(path);
				} catch {
					// Try the next candidate
				}
			}
		}

		return null;
	}

	private sealed class AccentProgressBar : Control {
		private double ratio;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal double Ratio {
			get => ratio;
			set {
				ratio = Math.Clamp(value, 0.0, 1.0);
				Invalidate();
			}
		}

		internal AccentProgressBar() {
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
			BackColor = BackgroundColor;
		}

		protected override void OnPaint(PaintEventArgs e) {
			Rectangle track = new(0, 0, Width - 1, Height - 1);

			using (SolidBrush trackBrush = new(TrackColor)) {
				e.Graphics.FillRectangle(trackBrush, track);
			}

			if (ratio > 0) {
				// Classic WinForms ProgressBar "Blocks" style: filled area is rendered as segmented blocks
				const int blockWidth = 8;
				const int gap = 2;

				int fillWidth = (int) Math.Round(track.Width * ratio);

				using SolidBrush fillBrush = new(AccentColor);

				for (int x = 0; x + blockWidth <= fillWidth; x += blockWidth + gap) {
					e.Graphics.FillRectangle(fillBrush, x, 0, blockWidth, track.Height);
				}
			}
		}
	}
}
