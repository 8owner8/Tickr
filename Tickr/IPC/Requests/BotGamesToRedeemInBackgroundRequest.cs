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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Tickr.IPC.Requests;

[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
public sealed class BotGamesToRedeemInBackgroundRequest {
	[Description("A string-string map that maps cd-key to redeem (key) to its name (value). Key in the map must be a valid and unique Steam cd-key. Value in the map must be a non-null and non-empty name of the key (e.g. game's name, but can be anything)")]
	[JsonInclude]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	[JsonRequired]
	[Required]
	public OrderedDictionary<string, string> GamesToRedeemInBackground { get; private init; } = new(StringComparer.OrdinalIgnoreCase);

	[JsonConstructor]
	private BotGamesToRedeemInBackgroundRequest() { }
}
