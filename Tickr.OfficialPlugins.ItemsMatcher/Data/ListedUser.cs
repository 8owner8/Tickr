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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Tickr.Steam.Data;

namespace Tickr.OfficialPlugins.ItemsMatcher.Data;

#pragma warning disable CA1812 // False positive, the class is used during json deserialization
[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
internal sealed class ListedUser {
	[JsonInclude]
	[JsonRequired]
	internal ImmutableHashSet<AssetInInventory> Assets { get; private init; } = [];

	[JsonInclude]
	[JsonRequired]
	internal ImmutableHashSet<EAssetType> MatchableTypes { get; private init; } = [];

	[JsonInclude]
	[JsonRequired]
	internal bool MatchEverything { get; private init; }

	[JsonInclude]
	[JsonRequired]
	internal byte MaxTradeHoldDuration { get; private init; }

	[JsonInclude]
	internal string? Nickname { get; private init; }

	[JsonInclude]
	[JsonRequired]
	internal ulong SteamID { get; private init; }

	[JsonInclude]
	[JsonRequired]
	internal uint TotalGamesCount { get; private init; }

	[JsonInclude]
	[JsonRequired]
	internal uint TotalInventoryCount { get; private init; }

	[JsonInclude]
	[JsonRequired]
	internal string TradeToken { get; private init; } = "";

	[JsonConstructor]
	private ListedUser() { }
}
#pragma warning restore CA1812 // False positive, the class is used during json deserialization
