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
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Tickr.Helpers.Json;

namespace Tickr.Steam.Data;

[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
public sealed class InventoryAppData {
	[JsonInclude]
	[JsonPropertyName("appid")]
	[JsonRequired]
	public uint AppID { get; private init; }

	[JsonInclude]
	[JsonPropertyName("asset_count")]
	[JsonRequired]
	public uint AssetsCount { get; private init; }

	[JsonInclude]
	[JsonPropertyName("rgContexts")]
	[JsonRequired]
	public ImmutableDictionary<ulong, InventoryContextData> Contexts { get; private init; } = ImmutableDictionary<ulong, InventoryContextData>.Empty;

	[JsonInclude]
	[JsonPropertyName("icon")]
	[JsonRequired]
	public Uri Icon { get; private init; } = null!;

	[JsonInclude]
	[JsonPropertyName("inventory_logo")]
	public Uri? InventoryLogo { get; private init; }

	[JsonInclude]
	[JsonPropertyName("link")]
	[JsonRequired]
	public Uri Link { get; private init; } = null!;

	// This seems to be rendered as number always, but who knows, better treat it like other brain damages in this response
	[JsonConverter(typeof(BooleanNormalizationConverter))]
	[JsonInclude]
	[JsonPropertyName("load_failed")]
	[JsonRequired]
	public bool LoadFailed { get; private init; }

	[JsonInclude]
	[JsonPropertyName("name")]
	[JsonRequired]
	public string Name { get; private init; } = "";

	// Steam renders this sometimes as a number, sometimes as a boolean, because fuck you, that's why
	[JsonConverter(typeof(BooleanNormalizationConverter))]
	[JsonInclude]
	[JsonPropertyName("owner_only")]
	[JsonRequired]
	public bool OwnerOnly { get; private init; }

	// Steam renders this sometimes as a string, sometimes as a boolean, because fuck you, that's why
	[JsonConverter(typeof(BooleanNormalizationConverter))]
	[JsonInclude]
	[JsonPropertyName("store_vetted")]
	[JsonRequired]
	public bool StoreVetted { get; private init; }

	[JsonInclude]
	[JsonPropertyName("trade_permissions")]
	[JsonRequired]
	public string TradePermissions { get; private init; } = "";

	[JsonConstructor]
	private InventoryAppData() { }
}
