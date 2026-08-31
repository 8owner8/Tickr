// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — UpdateInfo.cs  (result of a "is there a new release?" check)
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;

namespace Tickr.Launcher.Models;

internal sealed class UpdateInfo {
	/// <summary>Version published on GitHub, without the leading "v" (e.g. "1.3.0").</summary>
	internal required string Version { get; init; }

	/// <summary>Download URL of manifest.json attached to that release.</summary>
	internal required string ManifestUrl { get; init; }

	/// <summary>All release assets: asset name → download URL. Fallback for files with an empty DownloadUrl in the manifest.</summary>
	internal required IReadOnlyDictionary<string, string> Assets { get; init; }

	/// <summary>True when the release ships a new Tickr.exe (launcher self-update).</summary>
	internal bool HasSelfUpdate { get; init; }
}
