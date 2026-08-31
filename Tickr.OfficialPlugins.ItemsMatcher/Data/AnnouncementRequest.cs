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
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Tickr.Steam.Data;
using Tickr.Steam.Storage;
using SteamKit2;

namespace Tickr.OfficialPlugins.ItemsMatcher.Data;

internal sealed class AnnouncementRequest : AnnouncementRequestBase {
	[JsonInclude]
	[JsonRequired]
	private ImmutableList<AssetForListing> Inventory { get; init; }

	internal AnnouncementRequest(Guid guid, ulong steamID, IReadOnlyList<AssetForListing> inventory, string inventoryChecksum, IReadOnlyCollection<EAssetType> matchableTypes, uint totalInventoryCount, bool matchEverything, byte maxTradeHoldDuration, string tradeToken, string? nickname = null, string? avatarHash = null) : base(guid, steamID, inventoryChecksum, matchableTypes, totalInventoryCount, matchEverything, maxTradeHoldDuration, tradeToken, nickname, avatarHash) {
		ArgumentOutOfRangeException.ThrowIfEqual(guid, Guid.Empty);

		if ((steamID == 0) || !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		ArgumentNullException.ThrowIfNull(inventory);
		ArgumentException.ThrowIfNullOrEmpty(inventoryChecksum);

		if ((matchableTypes == null) || (matchableTypes.Count == 0)) {
			throw new ArgumentNullException(nameof(matchableTypes));
		}

		ArgumentOutOfRangeException.ThrowIfZero(totalInventoryCount);
		ArgumentException.ThrowIfNullOrEmpty(tradeToken);

		if (tradeToken.Length != BotConfig.SteamTradeTokenLength) {
			throw new ArgumentOutOfRangeException(nameof(tradeToken));
		}

		Inventory = [.. inventory];
	}
}
