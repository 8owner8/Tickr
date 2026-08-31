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
using System.Collections.Generic;
using System.Threading.Tasks;
using Tickr.Web.GitHub.Data;

namespace Tickr.Plugins.Interfaces;

internal interface IOfficialGitHubPluginUpdates : IGitHubPluginUpdates {
	Task<ReleaseAsset?> IGitHubPluginUpdates.GetTargetReleaseAsset(Version tickrVersion, string TickrVariant, Version newPluginVersion, IReadOnlyCollection<ReleaseAsset> releaseAssets) {
		ArgumentNullException.ThrowIfNull(tickrVersion);
		ArgumentException.ThrowIfNullOrEmpty(TickrVariant);
		ArgumentNullException.ThrowIfNull(newPluginVersion);

		if ((releaseAssets == null) || (releaseAssets.Count == 0)) {
			throw new ArgumentNullException(nameof(releaseAssets));
		}

		// For official plugins, the Tickr version must match the plugin version
		// Refuse to find the match if that's not the case, otherwise fallback to default implementation
		return Task.FromResult(tickrVersion == newPluginVersion ? FindPossibleMatch(tickrVersion, newPluginVersion, releaseAssets) : null);
	}
}
