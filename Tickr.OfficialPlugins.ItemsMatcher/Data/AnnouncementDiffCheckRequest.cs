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
using System.Text.Json.Serialization;
using SteamKit2;

namespace Tickr.OfficialPlugins.ItemsMatcher.Data;

internal sealed class AnnouncementDiffCheckRequest {
	[JsonInclude]
	[JsonRequired]
	private Guid Guid { get; init; }

	[JsonInclude]
	[JsonRequired]
	private string PreviousInventoryChecksum { get; init; }

	[JsonInclude]
	[JsonRequired]
	private ulong SteamID { get; init; }

	internal AnnouncementDiffCheckRequest(Guid guid, ulong steamID, string previousInventoryChecksum) {
		ArgumentOutOfRangeException.ThrowIfEqual(guid, Guid.Empty);

		if ((steamID == 0) || !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		ArgumentException.ThrowIfNullOrEmpty(previousInventoryChecksum);

		Guid = guid;
		SteamID = steamID;
		PreviousInventoryChecksum = previousInventoryChecksum;
	}
}
