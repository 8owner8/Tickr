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
using System.Threading.Tasks;
using Tickr.Steam;
using Tickr.Steam.Exchange;
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to receive information about all processed trades, in particular if you want to fire some logic based on trade offers being handled.
/// </summary>
[PublicAPI]
public interface IBotTradeOfferResults : IPlugin {
	/// <summary>
	///     Tickr will call this method for notifying you about the result of each received trade offer being handled. The method is executed for each batch that can contain 1 or more trade offers.
	/// </summary>
	/// <param name="bot">Bot object related to this callback.</param>
	/// <param name="tradeResults">Trade results related to this callback.</param>
	public Task OnBotTradeOfferResults(Bot bot, IReadOnlyCollection<ParseTradeResult> tradeResults);
}
