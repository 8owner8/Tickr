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
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to change the default string comparers used across TickrApp.
/// </summary>
[PublicAPI]
public interface IBotsComparer : IPlugin {
	/// <summary>
	///     Tickr will use this property for determining the comparer for the bots.
	///     Unless you know what you're doing, you should not implement this property yourself and let Tickr decide.
	/// </summary>
	/// <returns>Comparer that will be used for the bots, as well as bot regexes.</returns>
	public StringComparer BotsComparer => StringComparer.Ordinal;
}
