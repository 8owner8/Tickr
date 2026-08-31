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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Tickr.Storage;

namespace Tickr.IPC.Requests;

[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
public sealed class PluginUpdateRequest {
	[Description($"Target update channel. Not required, will default to {nameof(GlobalConfig.UpdateChannel)} if not provided")]
	[JsonInclude]
	public GlobalConfig.EUpdateChannel? Channel { get; private init; }

	[Description($"Forced update. This allows Tickr to potentially downgrade to previous version available on selected {nameof(Channel)}, which isn't permitted normally")]
	[JsonInclude]
	public bool Forced { get; private init; }

	[Description($"Target plugins. Not required, will default to plugin update configuration in {nameof(GlobalConfig)} if not provided")]
	[JsonInclude]
	public ImmutableHashSet<string>? Plugins { get; private init; }

	[JsonConstructor]
	private PluginUpdateRequest() { }
}
