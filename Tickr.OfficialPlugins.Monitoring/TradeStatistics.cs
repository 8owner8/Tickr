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
using System.Threading;
using Tickr.Steam.Exchange;

namespace Tickr.OfficialPlugins.Monitoring;

internal sealed class TradeStatistics {
	private readonly Lock Lock = new();

	internal int AcceptedOffers { get; private set; }
	internal int BlacklistedOffers { get; private set; }
	internal int ConfirmedOffers { get; private set; }
	internal int IgnoredOffers { get; private set; }
	internal int ItemsGiven { get; private set; }
	internal int ItemsReceived { get; private set; }
	internal int RejectedOffers { get; private set; }

	internal void Include(ParseTradeResult result) {
		ArgumentNullException.ThrowIfNull(result);

		lock (Lock) {
			switch (result.Result) {
				case ParseTradeResult.EResult.Accepted when result.Confirmed:
					ConfirmedOffers++;

					ItemsGiven += result.ItemsToGive?.Count ?? 0;
					ItemsReceived += result.ItemsToReceive?.Count ?? 0;

					goto case ParseTradeResult.EResult.Accepted;
				case ParseTradeResult.EResult.Accepted:
					AcceptedOffers++;

					break;
				case ParseTradeResult.EResult.Rejected:
					RejectedOffers++;

					break;
				case ParseTradeResult.EResult.Blacklisted:
					BlacklistedOffers++;

					break;
				case ParseTradeResult.EResult.Ignored:
					IgnoredOffers++;

					break;
			}
		}
	}
}
