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
using System.Threading.Tasks;
using JetBrains.Annotations;
using Tickr.Events;
using Tickr.Steam;

namespace Tickr.Core.Services;

[PublicAPI]
public interface IBotManager {
	IReadOnlyDictionary<string, Bot>? Bots { get; }
	ITickrEventBus Events { get; }

	Bot? GetBot(string botName);
	Task<bool> StartBot(string botName);
	Task<bool> StopBot(string botName);
	Task<bool> PauseFarming(string botName, bool permanent = true);
	Task<bool> ResumeFarming(string botName);
	Task<string?> ExecuteCommand(string command, ulong steamID = 0);
}

[PublicAPI]
public sealed class BotManager(ITickrEventBus? eventBus = null) : IBotManager {
	public static readonly BotManager Instance = new();

	public IReadOnlyDictionary<string, Bot>? Bots => Bot.BotsReadOnly;
	public ITickrEventBus Events { get; } = eventBus ?? TickrEventBus.Instance;

	public Bot? GetBot(string botName) {
		ArgumentException.ThrowIfNullOrEmpty(botName);

		if (Bot.BotsReadOnly == null) {
			return null;
		}

		return Bot.BotsReadOnly.TryGetValue(botName, out Bot? bot) ? bot : null;
	}

	public async Task<bool> StartBot(string botName) {
		ArgumentException.ThrowIfNullOrEmpty(botName);

		Bot? bot = GetBot(botName);
		if (bot == null) {
			return false;
		}

		await bot.Start().ConfigureAwait(false);
		return true;
	}

	public async Task<bool> StopBot(string botName) {
		ArgumentException.ThrowIfNullOrEmpty(botName);

		Bot? bot = GetBot(botName);
		if (bot == null) {
			return false;
		}

		await bot.Stop().ConfigureAwait(false);
		return true;
	}

	public async Task<bool> PauseFarming(string botName, bool permanent = true) {
		ArgumentException.ThrowIfNullOrEmpty(botName);

		Bot? bot = GetBot(botName);
		if (bot == null) {
			return false;
		}

		await bot.CardsFarmer.Pause(permanent).ConfigureAwait(false);
		return true;
	}

	public async Task<bool> ResumeFarming(string botName) {
		ArgumentException.ThrowIfNullOrEmpty(botName);

		Bot? bot = GetBot(botName);
		if (bot == null) {
			return false;
		}

		await bot.CardsFarmer.Resume(true).ConfigureAwait(false);
		return true;
	}

	public async Task<string?> ExecuteCommand(string command, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(command);

		if (Bot.BotsReadOnly == null || Bot.BotsReadOnly.Count == 0) {
			return null;
		}

		Bot? targetBot = null;
		foreach (Bot bot in Bot.BotsReadOnly.Values) {
			targetBot = bot;
			break;
		}

		if (targetBot == null) {
			return null;
		}

		EAccess access = steamID != 0 ? targetBot.GetAccess(steamID) : EAccess.Owner;
		return await targetBot.Commands.Response(access, command, steamID).ConfigureAwait(false);
	}
}