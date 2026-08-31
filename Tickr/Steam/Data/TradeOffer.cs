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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using Tickr.Helpers.Json;
using JetBrains.Annotations;
using SteamKit2;

namespace Tickr.Steam.Data;

// REF: https://developer.valvesoftware.com/wiki/Steam_Web_API/IEconService#CEcon_TradeOffer
[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
public sealed class TradeOffer {
	[PublicAPI]
	public IReadOnlyCollection<Asset> ItemsToGiveReadOnly => ItemsToGive;

	[PublicAPI]
	public IReadOnlyCollection<Asset> ItemsToReceiveReadOnly => ItemsToReceive;

	[PublicAPI]
	public ulong OtherSteamID64 { get; private set; }

	[JsonInclude]
	[JsonPropertyName("trade_offer_state")]
	[PublicAPI]
	public ETradeOfferState State { get; private set; }

	[JsonInclude]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
	[JsonPropertyName("tradeofferid")]
	[PublicAPI]
	public ulong TradeOfferID { get; private set; }

	[JsonDisallowNull]
	[JsonInclude]
	[JsonPropertyName("items_to_give")]
	internal HashSet<Asset> ItemsToGive { get; private init; } = [];

	[JsonDisallowNull]
	[JsonInclude]
	[JsonPropertyName("items_to_receive")]
	internal HashSet<Asset> ItemsToReceive { get; private init; } = [];

	[JsonInclude]
	[JsonPropertyName("accountid_other")]
	[JsonRequired]
	private uint OtherSteamID3 { init => OtherSteamID64 = new SteamID(value, EUniverse.Public, EAccountType.Individual); }

	[JsonConstructor]
	private TradeOffer() { }

	[PublicAPI]
	public bool IsValidSteamItemsRequest(IReadOnlyCollection<EAssetType> acceptedTypes) {
		if ((acceptedTypes == null) || (acceptedTypes.Count == 0)) {
			throw new ArgumentNullException(nameof(acceptedTypes));
		}

		return ItemsToGive.All(item => item is { AppID: Asset.SteamAppID, ContextID: Asset.SteamCommunityContextID, AssetID: > 0, Amount: > 0, ClassID: > 0, RealAppID: > 0 and not Asset.SteamAppID, Type: > EAssetType.Unknown, Rarity: > EAssetRarity.Unknown } && acceptedTypes.Contains(item.Type));
	}
}
