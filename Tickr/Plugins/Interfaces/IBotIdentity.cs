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
///     Implementing this interface allows you to discover core properties of your bot's identity, upon logging in to the Steam network.
/// </summary>
[PublicAPI]
public interface IBotIdentity : IPlugin {
	/// <summary>
	///     Tickr will call this method when bot receives its own identity information.
	/// </summary>
	/// <param name="bot">Bot object related to this callback.</param>
	/// <param name="data">Full data received by Tickr in the callback that relates to this bot (Steam) account.</param>
	/// <param name="nickname">Parsed nickname set for this bot (Steam) account.</param>
	/// <param name="avatarHash">Parsed hash of the avatar of this bot (Steam) account, as hex.</param>
	public Task OnSelfPersonaState(Bot bot, SteamFriends.PersonaStateCallback data, string? nickname, string? avatarHash);
}
