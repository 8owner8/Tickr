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
using System.Text.Json.Serialization;

namespace Tickr.IPC.Responses;

public sealed class TypeProperties {
	[Description("Base type of given type, if available. This can be used for determining how the body of the response should be interpreted")]
	[JsonInclude]
	public string? BaseType { get; private init; }

	[Description($"Custom attributes of given type, if available. This can be used for determining main enum type if {nameof(BaseType)} is {nameof(Enum)}")]
	[JsonInclude]
	public ImmutableHashSet<string>? CustomAttributes { get; private init; }

	[Description($"Underlying type of given type, if available. This can be used for determining underlying enum type if {nameof(BaseType)} is {nameof(Enum)}")]
	[JsonInclude]
	public string? UnderlyingType { get; private init; }

	internal TypeProperties(string? baseType = null, IEnumerable<string>? customAttributes = null, string? underlyingType = null) {
		BaseType = baseType;
		CustomAttributes = customAttributes?.ToImmutableHashSet(StringComparer.Ordinal);
		UnderlyingType = underlyingType;
	}
}
