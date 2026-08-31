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
using Tickr.Helpers.Json;
using Tickr.Steam.Integration;
using SteamKit2;

namespace Tickr.Steam.Data;

[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
internal sealed class InventoryResponse : OptionalResultResponse {
	internal EResult? ErrorCode {
		get {
			if (field.HasValue) {
				return field;
			}

			if (string.IsNullOrEmpty(ErrorText)) {
				return null;
			}

			return field = SteamUtilities.InterpretError(ErrorText);
		}
	}

	[JsonDisallowNull]
	[JsonInclude]
	[JsonPropertyName("assets")]
	internal ImmutableList<Asset> Assets { get; private init; } = [];

	[JsonDisallowNull]
	[JsonInclude]
	[JsonPropertyName("descriptions")]
	internal ImmutableHashSet<InventoryDescription> Descriptions { get; private init; } = [];

	[JsonInclude]
	[JsonPropertyName("error")]
	internal string? ErrorText { get; private init; }

	[JsonInclude]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
	[JsonPropertyName("last_assetid")]
	internal ulong LastAssetID { get; private init; }

	[JsonConverter(typeof(BooleanNormalizationConverter))]
	[JsonInclude]
	[JsonPropertyName("more_items")]
	internal bool MoreItems { get; private init; }

	[JsonInclude]
	[JsonPropertyName("total_inventory_count")]
	internal uint TotalInventoryCount { get; private init; }

	[JsonConstructor]
	private InventoryResponse() { }
}
