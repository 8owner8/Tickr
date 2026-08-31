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
using System.Collections.Generic;
using System.ComponentModel;
using Tickr.Steam.Data;
using JetBrains.Annotations;

namespace Tickr.Steam.Exchange;

public sealed class ParseTradeResult {
	[PublicAPI]
	public IReadOnlyCollection<Asset>? ItemsToGive { get; }

	[PublicAPI]
	public IReadOnlyCollection<Asset>? ItemsToReceive { get; }

	[PublicAPI]
	public EResult Result { get; }

	[PublicAPI]
	public ulong TradeOfferID { get; }

	[PublicAPI]
	public bool Confirmed { get; internal set; }

	internal ParseTradeResult(ulong tradeOfferID, EResult result, bool requiresMobileConfirmation, IReadOnlyCollection<Asset>? itemsToGive = null, IReadOnlyCollection<Asset>? itemsToReceive = null) {
		ArgumentOutOfRangeException.ThrowIfZero(tradeOfferID);

		if ((result == EResult.Unknown) || !Enum.IsDefined(result)) {
			throw new InvalidEnumArgumentException(nameof(result), (int) result, typeof(EResult));
		}

		TradeOfferID = tradeOfferID;
		Result = result;
		Confirmed = !requiresMobileConfirmation;
		ItemsToGive = itemsToGive;
		ItemsToReceive = itemsToReceive;
	}

	public enum EResult : byte {
		Unknown,
		Accepted,
		Blacklisted,
		Ignored,
		Rejected,
		TryAgain,
		RetryAfterOthers
	}
}
