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

public sealed class GamesToRedeemInBackgroundResponse {
	[Description("Keys that were redeemed and not used during the process, if available")]
	[JsonInclude]
	public ImmutableDictionary<string, string>? UnusedKeys { get; private init; }

	[Description("Keys that were redeemed and used during the process, if available")]
	[JsonInclude]
	public ImmutableDictionary<string, string>? UsedKeys { get; private init; }

	internal GamesToRedeemInBackgroundResponse(IReadOnlyDictionary<string, string>? unusedKeys = null, IReadOnlyDictionary<string, string>? usedKeys = null) {
		UnusedKeys = unusedKeys?.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
		UsedKeys = usedKeys?.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
	}
}
