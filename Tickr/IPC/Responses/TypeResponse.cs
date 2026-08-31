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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Tickr.IPC.Responses;

public sealed class TypeResponse {
	[Description($"A string-string map representing a decomposition of given type. The actual structure of this field depends on the type that was requested. You can determine that type based on {nameof(Properties)} metadata. For enums, keys are friendly names while values are underlying values of those names. For objects, keys are non-private fields and properties, while values are underlying types of those")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public ImmutableDictionary<string, string> Body { get; private init; }

	[Description("Metadata of given type")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public TypeProperties Properties { get; private init; }

	internal TypeResponse(IReadOnlyDictionary<string, string> body, TypeProperties properties) {
		ArgumentNullException.ThrowIfNull(body);
		ArgumentNullException.ThrowIfNull(properties);

		Body = body.ToImmutableDictionary(StringComparer.Ordinal);
		Properties = properties;
	}
}
