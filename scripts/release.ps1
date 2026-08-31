# ─────────────────────────────────────────────────────────────────────────────
#  Tickr — release.ps1  (one-command release: commit → tag → push)
#
#  Usage (from the repo root):
#    pwsh scripts/release.ps1 -Version 1.0.4
#    pwsh scripts/release.ps1 -Version 1.0.4 -Message "Fix app.js"
#
#  What it does:
#    1. Commits current changes with the given message (or uses HEAD if clean)
#    2. Pushes main, then creates and pushes tag v<version>
#    3. The release workflow on GitHub builds and publishes both releases;
#       users' launchers then download only the changed files (SHA256 delta)
#
#  Directory.Build.props version metadata is deliberately NOT bumped here.
#  Changing it rewrites satellite assemblies and .deps.json files even when their
#  contents/dependencies are unchanged. The real release version comes from the
#  git tag, manifest.json and version.txt written by the launcher.
#
#  Note: for content-only changes (www, images, html) the CI build still runs -
#  it must, to produce a consistent manifest - but users only download the delta.
# ─────────────────────────────────────────────────────────────────────────────
[CmdletBinding()]
param(
	# Release version, with or without the leading "v" (e.g. "1.0.4" or "v1.0.4")
	[Parameter(Mandatory = $true)]
	[string] $Version,

	# Commit message; defaults to the version itself
	[string] $Message = ""
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$tag = $Version.TrimStart('v', 'V')
$parsedVersion = $null
if (-not [System.Version]::TryParse($tag, [ref] $parsedVersion)) {
	throw "Invalid version '$Version' (expected e.g. 1.0.6 or v1.0.6)"
}

if ([string]::IsNullOrEmpty($Message)) {
	$Message = "v$tag"
}

# Must run from the repository root (where the solution lives)
if (-not (Test-Path 'Tickr.slnx')) {
	throw "Run this script from the repository root (the folder containing Tickr.slnx)"
}

# 1. Commit pending work. A clean tree is valid: it creates a release from the current HEAD.
$hasChanges = -not [string]::IsNullOrEmpty((git status --porcelain | Out-String).Trim())
if ($hasChanges) {
	git add -A
	git commit -m $Message
	if ($LASTEXITCODE -ne 0) { throw 'Failed to commit release changes' }
	Write-Host "Committed: $Message"
} else {
	Write-Host 'Working tree is clean - releasing the current HEAD.'
}

# 2. Push main and the tag - the tag triggers the release workflow
git push origin main
if ($LASTEXITCODE -ne 0) { throw 'Failed to push main' }
git tag "v$tag"
if ($LASTEXITCODE -ne 0) { throw "Failed to create tag v$tag (does it already exist?)" }
git push origin "v$tag"
if ($LASTEXITCODE -ne 0) { throw "Failed to push tag v$tag" }

Write-Host ''
Write-Host "Released v$tag - the workflow is building it now:"
Write-Host "https://github.com/8owner8/Tickr/actions"
