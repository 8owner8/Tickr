// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — ManifestFile.cs  (single file entry in the update manifest)
// ─────────────────────────────────────────────────────────────────────────────

namespace Tickr.Launcher.Models;

internal sealed class ManifestFile {
	/// <summary>Relative path inside the app directory, forward slashes (e.g. "www/style.css").</summary>
	public string Path { get; set; } = "";

	/// <summary>SHA256 of the file content, lowercase hex.</summary>
	public string Sha256 { get; set; } = "";

	/// <summary>Size in bytes.</summary>
	public long Size { get; set; }

	/// <summary>Direct download URL (GitHub Release asset).</summary>
	public string DownloadUrl { get; set; } = "";
}
