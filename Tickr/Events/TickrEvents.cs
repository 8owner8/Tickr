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

namespace Tickr.Events;

[PublicAPI]
public interface ITickrEvent {
	DateTime Timestamp { get; }
}

[PublicAPI]
public abstract record TickrEventBase : ITickrEvent {
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

[PublicAPI]
public sealed record BotStateChangedEvent(string BotName, bool IsConnected, bool IsLoggingIn, string? StatusMessage = null) : TickrEventBase;

[PublicAPI]
public sealed record FarmingProgressEvent(string BotName, bool NowFarming, bool Paused, uint GamesRemaining, TimeSpan TimeRemaining, string? CurrentGame = null) : TickrEventBase;

[PublicAPI]
public sealed record CardDroppedEvent(string BotName, uint AppID, string CardName) : TickrEventBase;

[PublicAPI]
public sealed record TwoFactorPromptEvent(string BotName, string ConfirmationType) : TickrEventBase;

[PublicAPI]
public sealed record TradeOfferReceivedEvent(string BotName, ulong TradeOfferID, ulong PartnerSteamID) : TickrEventBase;

[PublicAPI]
public sealed record LogEntryEmittedEvent(string Level, string Message) : TickrEventBase;