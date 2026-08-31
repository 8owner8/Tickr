// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — GitHubRelease.cs  (DTOs for the GitHub Releases API)
// ─────────────────────────────────────────────────────────────────────────────

using System.Text.Json.Serialization;

namespace Tickr.Launcher.Models;

internal sealed class GitHubRelease {
	[JsonPropertyName("tag_name")]
	public string TagName { get; set; } = "";

	[JsonPropertyName("assets")]
	public GitHubAsset[] Assets { get; set; } = [];
}

internal sealed class GitHubAsset {
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("browser_download_url")]
	public string DownloadUrl { get; set; } = "";
}
