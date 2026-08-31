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
using System.Text.Json.Serialization;

namespace Tickr.OfficialPlugins.SteamTokenDumper.Data;

#pragma warning disable CA1812 // False positive, the class is used during json deserialization
internal sealed class SubmitResponseData {
	[JsonInclude]
	[JsonPropertyName("new_apps")]
	[JsonRequired]
	internal ImmutableHashSet<uint> NewApps { get; private init; } = [];

	[JsonInclude]
	[JsonPropertyName("new_depots")]
	[JsonRequired]
	internal ImmutableHashSet<uint> NewDepots { get; private init; } = [];

	[JsonInclude]
	[JsonPropertyName("new_subs")]
	[JsonRequired]
	internal ImmutableHashSet<uint> NewPackages { get; private init; } = [];

	[JsonInclude]
	[JsonPropertyName("verified_apps")]
	[JsonRequired]
	internal ImmutableHashSet<uint> VerifiedApps { get; private init; } = [];

	[JsonInclude]
	[JsonPropertyName("verified_depots")]
	[JsonRequired]
	internal ImmutableHashSet<uint> VerifiedDepots { get; private init; } = [];

	[JsonInclude]
	[JsonPropertyName("verified_subs")]
	[JsonRequired]
	internal ImmutableHashSet<uint> VerifiedPackages { get; private init; } = [];
}
#pragma warning restore CA1812 // False positive, the class is used during json deserialization
