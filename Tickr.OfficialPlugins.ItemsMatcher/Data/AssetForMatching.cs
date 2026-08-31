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

internal class AssetForMatching {
	[JsonInclude]
	[JsonPropertyName("a")]
	[JsonRequired]
	internal uint Amount { get; set; }

	[JsonInclude]
	[JsonPropertyName("c")]
	[JsonRequired]
	internal ulong ClassID { get; private init; }

	[JsonInclude]
	[JsonPropertyName("r")]
	[JsonRequired]
	internal EAssetRarity Rarity { get; private init; }

	[JsonInclude]
	[JsonPropertyName("e")]
	[JsonRequired]
	internal uint RealAppID { get; private init; }

	[JsonInclude]
	[JsonPropertyName("t")]
	[JsonRequired]
	internal bool Tradable { get; private init; }

	[JsonInclude]
	[JsonPropertyName("p")]
	[JsonRequired]
	internal EAssetType Type { get; private init; }

	[JsonConstructor]
	protected AssetForMatching() { }

	internal AssetForMatching(Asset asset) {
		ArgumentNullException.ThrowIfNull(asset);

		Amount = asset.Amount;

		ClassID = asset.ClassID;
		Tradable = asset.Tradable;

		RealAppID = asset.RealAppID;
		Type = asset.Type;
		Rarity = asset.Rarity;
	}
}
