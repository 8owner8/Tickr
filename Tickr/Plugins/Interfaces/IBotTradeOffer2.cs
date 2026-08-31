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
using Tickr.Steam.Data;
using Tickr.Steam.Exchange;
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows your plugin to implement custom logic for accepting trades that Tickr isn't willing to handle itself.
/// </summary>
[PublicAPI]
public interface IBotTradeOffer2 : IPlugin {
	/// <summary>
	///     Tickr will call this method for unhandled (e.g. blacklisted, ignored and rejected) trade offers received by the bot.
	/// </summary>
	/// <param name="bot">Bot object related to this callback.</param>
	/// <param name="tradeOffer">Trade offer related to this callback.</param>
	/// <param name="tickrResult">Tickr result in regards to parsing this trade offer, can be useful for determining why it wasn't accepted as part of the core logic.</param>
	/// <returns>True if the trade offer should be accepted as part of this plugin, false otherwise.</returns>
	public Task<bool> OnBotTradeOffer(Bot bot, TradeOffer tradeOffer, ParseTradeResult.EResult tickrResult);
}
