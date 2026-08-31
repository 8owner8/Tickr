# ─────────────────────────────────────────────────────────────────────────────
#  Tickr — generate-manifest.ps1
#
#  Builds manifest.json for the auto-update system: every file of the published
#  app with its relative path, SHA256 hash and size. The launcher's UpdateManager
#  compares these hashes against the local installation to compute the delta.
#
#  DownloadUrl is filled with the predictable GitHub Release asset URL:
#  https://github.com/<repo>/releases/download/<tag>/<asset>
#  Asset names are flat (GitHub Releases have no folders), so relative paths are
#  encoded: "www/style.css" -> "www__style.css". Tickr.Launcher/UpdateManager.cs
#  (EncodeAssetName) must stay in sync with this encoding.
#
#  Usage:
#    pwsh scripts/generate-manifest.ps1 -Version "v1.3.0" -InputDir "out/result" `
#        -OutputFile "manifest.json" -LauncherExe "launcher_out/Tickr.exe"
# ─────────────────────────────────────────────────────────────────────────────
[CmdletBinding()]
param(
	# Release version, with or without the leading "v" (e.g. "v1.3.0" or "1.3.0")
	[Parameter(Mandatory = $true)]
	[string] $Version,

	# Directory with the published app (dotnet publish output)
	[Parameter(Mandatory = $true)]
	[string] $InputDir,

	# Where to write the manifest
	[Parameter(Mandatory = $true)]
	[string] $OutputFile,

	# GitHub repo in owner/name form - must match UpdateManager.GitHubRepo
	[string] $Repo = "8owner8/Tickr",

	# Optional path to the built launcher Tickr.exe, included in the manifest for self-updates
	[string] $LauncherExe = "",

	# Release tag the payload files are downloaded from. The payload lives in a parallel prerelease
	# "data-{tag}" so the user-facing release stays clean - CI passes "data-{tag}" here.
	# The launcher exe itself is downloaded from the MAIN release (version tag).
	[string] $DownloadTag = ""
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Files that exist only on the user's machine at runtime - never part of a release
$excludedNames = @('restart.pending', 'self_restart.pending', 'launcher.log', 'version.txt', 'update-check.txt')

$tag = $Version.TrimStart('v', 'V')
$versionStamp = "v$tag"

function Get-AssetName([string] $RelativePath) {
	# Flat asset name: path separators become double underscores
	return $RelativePath.Replace('/', '__')
}

if ([string]::IsNullOrEmpty($DownloadTag)) {
	$DownloadTag = $versionStamp
}

function Get-DownloadUrl([string] $RelativePath, [string] $Tag) {
	$asset = Get-AssetName $RelativePath
	$encodedAsset = [uri]::EscapeDataString($asset)
	return "https://github.com/$Repo/releases/download/$Tag/$encodedAsset"
}

function New-ManifestEntry([System.IO.FileInfo] $File, [string] $RelativePath, [string] $Tag) {
	$hash = (Get-FileHash -Algorithm SHA256 -Path $File.FullName).Hash.ToLowerInvariant()
	return [ordered]@{
		path        = $RelativePath
		sha256      = $hash
		size        = $File.Length
		downloadUrl = Get-DownloadUrl $RelativePath $Tag
	}
}

$resolvedInput = (Resolve-Path $InputDir).Path
$files = [System.Collections.Generic.List[object]]::new()

foreach ($item in (Get-ChildItem -Path $resolvedInput -Recurse -File)) {
	$relative = $item.FullName.Substring($resolvedInput.Length).TrimStart('\', '/') -replace '\\', '/'

	if ($excludedNames -contains $item.Name) { continue }
	if ($item.Length -eq 0) { continue } # Zero-byte files (e.g. .gitkeep placeholders) - GitHub rejects them as release assets, and the app creates these dirs on demand anyway
	if ($relative -like '*.old') { continue }
	if ($relative -eq 'manifest.json') { continue }
	if ($relative -eq 'Tickr.exe') { continue } # Never take it from the app output - the launcher enters the manifest only via -LauncherExe (an apphost Tickr.exe here would be a stale duplicate)
	if ($relative.StartsWith('temp/')) { continue }

	$files.Add((New-ManifestEntry -File $item -RelativePath $relative -Tag $DownloadTag))
}

# The launcher itself is published separately - append it so it can self-update
if (-not [string]::IsNullOrEmpty($LauncherExe)) {
	$launcherItem = Get-Item $LauncherExe
	$files.Add((New-ManifestEntry -File $launcherItem -RelativePath 'Tickr.exe' -Tag $versionStamp))
}

$manifest = [ordered]@{
	version = $tag
	files   = $files
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $OutputFile -Encoding utf8

Write-Host "Manifest written to $OutputFile : $($files.Count) files, version $tag"
