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

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to handle friend requests on the Steam platform.
/// </summary>
[PublicAPI]
public interface IBotFriendRequest : IPlugin {
	/// <summary>
	///     Tickr will call this method for unhandled (ignored and rejected) friend requests and Steam group invites received by the bot.
	/// </summary>
	/// <param name="bot">Bot object related to this callback.</param>
	/// <param name="steamID">64-bit Steam identificator of the user that sent a friend request, or a group that the bot has been invited to.</param>
	/// <returns>True if the request should be accepted as part of this plugin, false otherwise.</returns>
	public Task<bool> OnBotFriendRequest(Bot bot, ulong steamID);
}
