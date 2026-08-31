// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — LauncherJsonContext.cs  (source-generated JSON, trim-safe)
// ─────────────────────────────────────────────────────────────────────────────

using System.Text.Json.Serialization;

namespace Tickr.Launcher.Models;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(GitHubRelease))]
internal sealed partial class LauncherJsonContext : JsonSerializerContext;
