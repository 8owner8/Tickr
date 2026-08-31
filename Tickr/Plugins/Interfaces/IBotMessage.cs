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
using Tickr.Storage;
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to react to each message received on the Steam platform.
/// </summary>
/// <remarks>This is used for Steam messages exclusively, that are not Tickr commands. If you want to grab the commands, check <see cref="IBotCommand2" /> interface instead.</remarks>
[PublicAPI]
public interface IBotMessage : IPlugin {
	/// <summary>
	///     Tickr will call this method for messages that are not commands, so ones that do not start from <see cref="GlobalConfig.CommandPrefix" />.
	/// </summary>
	/// <param name="bot">Bot object related to this callback.</param>
	/// <param name="steamID">64-bit long unsigned integer of steamID executing the command.</param>
	/// <param name="message">Message in its raw format.</param>
	/// <returns>Response to the message, or null/empty (as the task value) for silence.</returns>
	public Task<string?> OnBotMessage(Bot bot, ulong steamID, string message);
}
