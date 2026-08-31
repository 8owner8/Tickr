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

using System.Linq;
using System.Threading.Tasks;
using Tickr.Localization;
using Tickr.Steam;
using Tickr.Storage;

namespace Tickr.Core;

internal static class Events {
	internal static async Task OnBotShutdown() {
		bool shutdownIfPossible = TickrApp.GlobalConfig?.ShutdownIfPossible ?? GlobalConfig.DefaultShutdownIfPossible;

		if (!shutdownIfPossible || (Bot.Bots?.Values.Any(static bot => bot.KeepRunning) == true)) {
			return;
		}

		TickrApp.TickrLogger.LogGenericInfo(Strings.NoBotsAreRunning);

		// We give user extra 5 seconds for eventual config changes
		await Task.Delay(5000).ConfigureAwait(false);

		if (Bot.Bots?.Values.Any(static bot => bot.KeepRunning) == true) {
			return;
		}

		await Program.Exit().ConfigureAwait(false);
	}
}
