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
using System.Linq;
using System.Threading;
using Tickr.Core;
using Tickr.Localization;
using Tickr.Plugins;
using Tickr.Web;
using SteamKit2;

namespace Tickr.Steam.Integration;

internal static class SteamPICSChanges {
	private const byte RefreshTimerInMinutes = 5;

	internal static uint LastChangeNumber { get; private set; }
	internal static bool LiveUpdate { get; private set; }

	private static readonly SemaphoreSlim RefreshSemaphore = new(1, 1);
	private static readonly Timer RefreshTimer = new(RefreshChanges);

	private static bool TimerAlreadySet;

	internal static void Init(uint changeNumberToStartFrom) => LastChangeNumber = changeNumberToStartFrom;

	internal static void OnBotLoggedOn() {
		if (TimerAlreadySet) {
			return;
		}

		// ReSharper disable once SuspiciousLockOverSynchronizationPrimitive - this is not a mistake, we need extra synchronization, and we can re-use the semaphore object for that
		lock (RefreshSemaphore) {
			if (TimerAlreadySet) {
				return;
			}

			TimerAlreadySet = true;
			RefreshTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(RefreshTimerInMinutes));
		}
	}

	private static async void RefreshChanges(object? state = null) {
		if (!await RefreshSemaphore.WaitAsync(0).ConfigureAwait(false)) {
			return;
		}

		try {
			Bot? refreshBot = null;
			SteamApps.PICSChangesCallback? picsChanges = null;

			for (byte i = 0; (i < WebBrowser.MaxTries) && (picsChanges == null); i++) {
				refreshBot = Bot.Bots?.Values.FirstOrDefault(static bot => bot.IsConnectedAndLoggedOn);

				if (refreshBot == null) {
					LiveUpdate = false;

					return;
				}

				try {
					picsChanges = await refreshBot.SteamApps.PICSGetChangesSince(LastChangeNumber, true, true).ToLongRunningTask().ConfigureAwait(false);
				} catch (Exception e) {
					refreshBot.TickrLogger.LogGenericWarningException(e);
				}
			}

			if ((refreshBot == null) || (picsChanges == null)) {
				LiveUpdate = false;
				TickrApp.TickrLogger.LogGenericWarning(Strings.WarningFailed);

				return;
			}

			if (picsChanges.CurrentChangeNumber == picsChanges.LastChangeNumber) {
				LiveUpdate = true;

				return;
			}

			LastChangeNumber = picsChanges.CurrentChangeNumber;

			if (picsChanges.RequiresFullAppUpdate || picsChanges.RequiresFullPackageUpdate) {
				if (TickrApp.GlobalDatabase != null) {
					await TickrApp.GlobalDatabase.OnPICSChangesRestart(picsChanges.CurrentChangeNumber).ConfigureAwait(false);
				}

				LiveUpdate = true;

				await PluginsCore.OnPICSChangesRestart(picsChanges.CurrentChangeNumber).ConfigureAwait(false);

				return;
			}

			LiveUpdate = true;

			if (TickrApp.GlobalDatabase != null) {
				TickrApp.GlobalDatabase.LastChangeNumber = picsChanges.CurrentChangeNumber;

				if (picsChanges.PackageChanges.Count > 0) {
					await TickrApp.GlobalDatabase.RefreshPackages(refreshBot, picsChanges.PackageChanges.ToDictionary(static package => package.Key, static package => package.Value.ChangeNumber)).ConfigureAwait(false);
				}
			}

			await PluginsCore.OnPICSChanges(picsChanges.CurrentChangeNumber, picsChanges.AppChanges, picsChanges.PackageChanges).ConfigureAwait(false);
		} finally {
			RefreshSemaphore.Release();
		}
	}
}
