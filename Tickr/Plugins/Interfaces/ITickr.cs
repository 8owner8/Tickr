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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to easily register your own data structures upon Tickr initialization. This is especially useful if you want to register those based on the Tickr's global config.
/// </summary>
/// <remarks>If your logic doesn't require Tickr settings, you can consider using core <see cref="IPlugin.OnLoaded" /> method instead. Implementing this interface might still make sense, since it happens later during Tickr initialization and provides more available data structures.</remarks>
[PublicAPI]
public interface ITickr : IPlugin {
	/// <summary>
	///     Tickr will call this method right after global config initialization.
	/// </summary>
	/// <param name="additionalConfigProperties">Extra config properties made out of <see cref="JsonExtensionDataAttribute" />. Can be null if no extra properties are found.</param>
	public Task OnTickrInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null);
}
