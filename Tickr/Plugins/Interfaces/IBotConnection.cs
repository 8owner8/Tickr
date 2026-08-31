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

using System.Threading.Tasks;
using Tickr.Steam;
using JetBrains.Annotations;
using SteamKit2;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to monitor Bot's connection with the Steam network, in particular when it goes online and offline.
/// </summary>
[PublicAPI]
public interface IBotConnection : IPlugin {
	/// <summary>
	///     Tickr will call this method when bot gets disconnected from Steam network.
	/// </summary>
	/// <param name="bot">Bot object related to this callback.</param>
	/// <param name="reason">Reason for disconnection, or <see cref="EResult.OK" /> if the disconnection was initiated by Tickr (e.g. as a result of a command).</param>
	public Task OnBotDisconnected(Bot bot, EResult reason);

	/// <summary>
	///     Tickr will call this method when bot successfully connects to Steam network.
	/// </summary>
	/// <param name="bot">Bot object related to this callback.</param>
	public Task OnBotLoggedOn(Bot bot);
}
