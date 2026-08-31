// Copyright 2015-2026 Tickr
// Contact: support@TickrApp.dev
// |
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// |
// http://www.apache.org/licenses/LICENSE-2.0
// |
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Tickr.Web.GitHub.Data;

namespace Tickr.IPC.Responses;

public sealed class GitHubReleaseResponse {
	[Description("Changelog of the release rendered in HTML")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public string ChangelogHTML { get; private init; }

	[Description("Date of the release")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public DateTime ReleasedAt { get; private init; }

	[Description("Boolean value that specifies whether the build is stable or not (pre-release)")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public bool Stable { get; private init; }

	[Description("Version of the release")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public string Version { get; private init; }

	internal GitHubReleaseResponse(ReleaseResponse releaseResponse) {
		ArgumentNullException.ThrowIfNull(releaseResponse);

		ChangelogHTML = releaseResponse.ChangelogHTML ?? "";
		ReleasedAt = releaseResponse.PublishedAt;
		Stable = !releaseResponse.IsPreRelease;
		Version = releaseResponse.Tag;
	}
}
