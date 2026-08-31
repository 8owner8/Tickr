// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — Manifest.cs  (update manifest: full file list + hashes)
// ─────────────────────────────────────────────────────────────────────────────

namespace Tickr.Launcher.Models;

internal sealed class Manifest {
	public string Version { get; set; } = "";
	public ManifestFile[] Files { get; set; } = [];
}
