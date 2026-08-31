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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Tickr.IPC.Responses;

public sealed class BotAddLicenseResponse {
	[Description("A collection (set) of apps (appIDs) to ask license for")]
	[JsonInclude]
	public ImmutableDictionary<uint, AddLicenseResult>? Apps { get; private init; }

	[Description("A collection (set) of packages (subIDs) to ask license for")]
	[JsonInclude]
	public ImmutableDictionary<uint, AddLicenseResult>? Packages { get; private init; }

	internal BotAddLicenseResponse(IReadOnlyDictionary<uint, AddLicenseResult>? apps, IReadOnlyDictionary<uint, AddLicenseResult>? packages) {
		Apps = apps?.ToImmutableDictionary();
		Packages = packages?.ToImmutableDictionary();
	}
}
