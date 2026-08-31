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
using JetBrains.Annotations;

namespace Tickr.Steam.Data;

public sealed class Tag {
	[JsonInclude]
	[JsonPropertyName("color")]
	[PublicAPI]
	public string? Color { get; private init; }

	[JsonInclude]
	[JsonPropertyName("category")]
	[JsonRequired]
	[PublicAPI]
	public string Identifier { get; private init; } = "";

	[JsonInclude]
	[JsonPropertyName("localized_category_name")]
	[PublicAPI]
	public string? LocalizedIdentifier { get; private init; }

	[JsonInclude]
	[JsonPropertyName("localized_tag_name")]
	[PublicAPI]
	public string? LocalizedValue { get; private init; }

	[JsonInclude]
	[JsonPropertyName("internal_name")]
	[JsonRequired]
	[PublicAPI]
	public string Value { get; private init; } = "";

	internal Tag(string identifier, string value, string? localizedIdentifier = null, string? localizedValue = null, string? color = null) {
		ArgumentException.ThrowIfNullOrEmpty(identifier);
		ArgumentNullException.ThrowIfNull(value);

		Identifier = identifier;
		Value = value;
		LocalizedIdentifier = localizedIdentifier;
		LocalizedValue = localizedValue;
		Color = color;
	}

	[JsonConstructor]
	private Tag() { }
}
