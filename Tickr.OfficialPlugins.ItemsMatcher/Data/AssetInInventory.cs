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
using Tickr.Steam.Data;

namespace Tickr.OfficialPlugins.ItemsMatcher.Data;

internal class AssetInInventory : AssetForMatching {
	[JsonInclude]
	[JsonPropertyName("d")]
	[JsonRequired]
	internal ulong AssetID { get; private init; }

	[JsonConstructor]
	protected AssetInInventory() { }

	internal AssetInInventory(Asset asset) : base(asset) {
		ArgumentNullException.ThrowIfNull(asset);

		AssetID = asset.AssetID;
	}

	internal Asset ToAsset() => new(Asset.SteamAppID, Asset.SteamCommunityContextID, ClassID, Amount, new InventoryDescription(Asset.SteamAppID, ClassID, tradable: Tradable, realAppID: RealAppID, type: Type, rarity: Rarity), AssetID);
}
