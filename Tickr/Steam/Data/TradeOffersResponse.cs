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

namespace Tickr.Steam.Data;

[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
public sealed class TradeOffersResponse {
	[JsonDisallowNull]
	[JsonInclude]
	[JsonPropertyName("descriptions")]
	public ImmutableHashSet<InventoryDescription> Descriptions { get; private init; } = [];

	[JsonDisallowNull]
	[JsonInclude]
	[JsonPropertyName("trade_offers_received")]
	public ImmutableHashSet<TradeOffer> TradeOffersReceived { get; private init; } = [];

	[JsonDisallowNull]
	[JsonInclude]
	[JsonPropertyName("trade_offers_sent")]
	public ImmutableHashSet<TradeOffer> TradeOffersSent { get; private init; } = [];

	[JsonConstructor]
	private TradeOffersResponse() { }
}
