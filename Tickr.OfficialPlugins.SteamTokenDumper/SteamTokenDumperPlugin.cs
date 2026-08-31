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
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tickr.Core;
using Tickr.Helpers;
using Tickr.Helpers.Json;
using Tickr.OfficialPlugins.SteamTokenDumper.Data;
using Tickr.OfficialPlugins.SteamTokenDumper.Localization;
using Tickr.Plugins;
using Tickr.Plugins.Interfaces;
using Tickr.Steam;
using Tickr.Steam.Interaction;
using Tickr.Storage;
using Tickr.Web;
using Tickr.Web.Responses;
using SteamKit2;

namespace Tickr.OfficialPlugins.SteamTokenDumper;

[Export(typeof(IPlugin))]
internal sealed class SteamTokenDumperPlugin : OfficialPlugin, ITickr, IBot, IBotCommand2, IBotSteamClient, ISteamPICSChanges {
	private const ushort DepotsRateLimitingDelay = 500;

	internal static SteamTokenDumperConfig? Config { get; private set; }

	private static readonly ConcurrentDictionary<Bot, IDisposable> BotSubscriptions = new();
	private static readonly ConcurrentDictionary<Bot, (SemaphoreSlim RefreshSemaphore, Timer RefreshTimer)> BotSynchronizations = new();
	private static readonly SemaphoreSlim SubmissionSemaphore = new(1, 1);
	private static readonly Timer SubmissionTimer = new(OnSubmissionTimer);

	private static GlobalCache? GlobalCache;
	private static DateTimeOffset LastUploadAt = DateTimeOffset.MinValue;

	[JsonInclude]
	public override string Name => nameof(SteamTokenDumperPlugin);

	[JsonInclude]
	public override Version Version => typeof(SteamTokenDumperPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task<uint> GetPreferredChangeNumberToStartFrom() => Task.FromResult(GlobalCache?.LastChangeNumber ?? 0);

	public async Task OnTickrInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
		if (!SharedInfo.HasValidToken) {
			TickrApp.TickrLogger.LogGenericError(Strings.FormatPluginDisabledMissingBuildToken(nameof(SteamTokenDumperPlugin)));

			return;
		}

		bool isEnabled = false;
		SteamTokenDumperConfig? config = null;

		if (additionalConfigProperties != null) {
			foreach ((string configProperty, JsonElement configValue) in additionalConfigProperties) {
				try {
					switch (configProperty) {
						case nameof(GlobalConfigExtension.SteamTokenDumperPlugin):
							config = configValue.ToJsonObject<SteamTokenDumperConfig>();

							break;
						case nameof(GlobalConfigExtension.SteamTokenDumperPluginEnabled) when configValue.ValueKind == JsonValueKind.False:
							isEnabled = false;

							break;
						case nameof(GlobalConfigExtension.SteamTokenDumperPluginEnabled) when configValue.ValueKind == JsonValueKind.True:
							isEnabled = true;

							break;
					}
				} catch (Exception e) {
					TickrApp.TickrLogger.LogGenericException(e);
					TickrApp.TickrLogger.LogGenericWarning(Strings.FormatPluginDisabledInConfig(nameof(SteamTokenDumperPlugin)));

					return;
				}
			}
		}

		if (GlobalCache == null) {
			GlobalCache? globalCache = await GlobalCache.Load().ConfigureAwait(false);

			if (globalCache == null) {
				TickrApp.TickrLogger.LogGenericError(Strings.FormatFileCouldNotBeLoadedFreshInit(nameof(GlobalCache)));

				GlobalCache = new GlobalCache();
			} else {
				GlobalCache = globalCache;
			}
		}

		if (!isEnabled && (config == null)) {
			TickrApp.TickrLogger.LogGenericInfo(Strings.FormatPluginDisabledInConfig(nameof(SteamTokenDumperPlugin)));

			return;
		}

		config ??= new SteamTokenDumperConfig();

		if (isEnabled) {
			config.Enabled = true;
		}

		if (!config.Enabled) {
			TickrApp.TickrLogger.LogGenericInfo(Strings.FormatPluginDisabledInConfig(nameof(SteamTokenDumperPlugin)));
		}

		if (!config.SecretAppIDs.IsEmpty) {
			TickrApp.TickrLogger.LogGenericInfo(Strings.FormatPluginSecretListInitialized(nameof(config.SecretAppIDs), string.Join(", ", config.SecretAppIDs)));
		}

		if (!config.SecretPackageIDs.IsEmpty) {
			TickrApp.TickrLogger.LogGenericInfo(Strings.FormatPluginSecretListInitialized(nameof(config.SecretPackageIDs), string.Join(", ", config.SecretPackageIDs)));
		}

		if (!config.SecretDepotIDs.IsEmpty) {
			TickrApp.TickrLogger.LogGenericInfo(Strings.FormatPluginSecretListInitialized(nameof(config.SecretDepotIDs), string.Join(", ", config.SecretDepotIDs)));
		}

		Config = config;

		if (!config.Enabled) {
			return;
		}

#pragma warning disable CA5394 // This call isn't used in a security-sensitive manner
		TimeSpan startIn = TimeSpan.FromMinutes(Random.Shared.Next(SharedInfo.MinimumMinutesBeforeFirstUpload, SharedInfo.MaximumMinutesBeforeFirstUpload));
#pragma warning restore CA5394 // This call isn't used in a security-sensitive manner

		// ReSharper disable once SuspiciousLockOverSynchronizationPrimitive - this is not a mistake, we need extra synchronization, and we can re-use the semaphore object for that
		lock (SubmissionSemaphore) {
			SubmissionTimer.Change(startIn, TimeSpan.FromHours(SharedInfo.HoursBetweenUploads));
		}

		TickrApp.TickrLogger.LogGenericInfo(Strings.FormatPluginInitializedAndEnabled(nameof(SteamTokenDumperPlugin), startIn.ToHumanReadable()));
	}

	public Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		ArgumentNullException.ThrowIfNull(bot);

		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		if ((args == null) || (args.Length == 0)) {
			throw new ArgumentNullException(nameof(args));
		}

		switch (args.Length) {
			case 1:
				switch (args[0].ToUpperInvariant()) {
					case "STD":
						return Task.FromResult(ResponseRefreshManually(access, bot));
				}

				break;
			default:
				switch (args[0].ToUpperInvariant()) {
					case "STD":
						return Task.FromResult(ResponseRefreshManually(access, Utilities.GetArgsAsText(args, 1, ","), steamID));
				}

				break;
		}

		return Task.FromResult<string?>(null);
	}

	public async Task OnBotDestroy(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		if (BotSubscriptions.TryRemove(bot, out IDisposable? subscription)) {
			subscription.Dispose();
		}

		if (BotSynchronizations.TryRemove(bot, out (SemaphoreSlim RefreshSemaphore, Timer RefreshTimer) synchronization)) {
			// Ensure the semaphore is empty, otherwise we're risking disposed exceptions
			await synchronization.RefreshSemaphore.WaitAsync().ConfigureAwait(false);

			synchronization.RefreshSemaphore.Dispose();

			await synchronization.RefreshTimer.DisposeAsync().ConfigureAwait(false);
		}
	}

	public async Task OnBotInit(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		if (GlobalCache == null) {
			// We can't operate like this anyway, skip initialization of synchronization structures
			return;
		}

		SemaphoreSlim refreshSemaphore = new(1, 1);
		Timer refreshTimer = new(OnBotRefreshTimer, bot, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

		if (!BotSynchronizations.TryAdd(bot, (refreshSemaphore, refreshTimer))) {
			refreshSemaphore.Dispose();

			await refreshTimer.DisposeAsync().ConfigureAwait(false);
		}
	}

	public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentNullException.ThrowIfNull(callbackManager);

		if (BotSubscriptions.TryRemove(bot, out IDisposable? subscription)) {
			subscription.Dispose();
		}

		if (Config is not { Enabled: true }) {
			return Task.CompletedTask;
		}

		subscription = callbackManager.Subscribe<SteamApps.LicenseListCallback>(callback => OnLicenseList(bot, callback));

		if (!BotSubscriptions.TryAdd(bot, subscription)) {
			subscription.Dispose();
		}

		return Task.CompletedTask;
	}

	public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot) => Task.FromResult<IReadOnlyCollection<ClientMsgHandler>?>(null);

	public override Task OnLoaded() {
		Utilities.WarnAboutIncompleteTranslation(Strings.ResourceManager);

		return Task.CompletedTask;
	}

	public Task OnPICSChanges(uint currentChangeNumber, IReadOnlyDictionary<uint, SteamApps.PICSChangesCallback.PICSChangeData> appChanges, IReadOnlyDictionary<uint, SteamApps.PICSChangesCallback.PICSChangeData> packageChanges) {
		ArgumentOutOfRangeException.ThrowIfZero(currentChangeNumber);
		ArgumentNullException.ThrowIfNull(appChanges);
		ArgumentNullException.ThrowIfNull(packageChanges);

		GlobalCache?.OnPICSChanges(currentChangeNumber, appChanges);

		return Task.CompletedTask;
	}

	public Task OnPICSChangesRestart(uint currentChangeNumber) {
		ArgumentOutOfRangeException.ThrowIfZero(currentChangeNumber);

		GlobalCache?.OnPICSChangesRestart(currentChangeNumber);

		return Task.CompletedTask;
	}

	private static async void OnBotRefreshTimer(object? state) {
		if (state is not Bot bot) {
			throw new InvalidOperationException(nameof(state));
		}

		await Refresh(bot).ConfigureAwait(false);
	}

	private static async void OnLicenseList(Bot bot, SteamApps.LicenseListCallback callback) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentNullException.ThrowIfNull(callback);

		if (Config is not { Enabled: true }) {
			return;
		}

		// Schedule a refresh in a while from now
		if (!BotSynchronizations.TryGetValue(bot, out (SemaphoreSlim RefreshSemaphore, Timer RefreshTimer) synchronization)) {
			return;
		}

		if (!await synchronization.RefreshSemaphore.WaitAsync(0).ConfigureAwait(false)) {
			// Another refresh is in progress, skip the refresh for now
			return;
		}

		try {
			synchronization.RefreshTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromHours(SharedInfo.MaximumHoursBetweenRefresh));
		} finally {
			synchronization.RefreshSemaphore.Release();
		}
	}

	private static async void OnSubmissionTimer(object? state = null) => await SubmitData().ConfigureAwait(false);

	private static async Task Refresh(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		if (GlobalCache == null) {
			throw new InvalidOperationException(nameof(GlobalCache));
		}

		if (TickrApp.GlobalDatabase == null) {
			throw new InvalidOperationException(nameof(TickrApp.GlobalDatabase));
		}

		if (!BotSynchronizations.TryGetValue(bot, out (SemaphoreSlim RefreshSemaphore, Timer RefreshTimer) synchronization)) {
			throw new InvalidOperationException(nameof(synchronization));
		}

		if (!await synchronization.RefreshSemaphore.WaitAsync(0).ConfigureAwait(false)) {
			return;
		}

		SemaphoreSlim depotsRateLimitingSemaphore = new(1, 1);

		try {
			if (!bot.IsConnectedAndLoggedOn) {
				return;
			}

			HashSet<uint> packageIDs = [.. bot.OwnedPackages.Where(static package => (Config?.SecretPackageIDs.Contains(package.Key) != true) && ((package.Value.PaymentMethod != EPaymentMethod.AutoGrant) || (Config?.SkipAutoGrantPackages == false))).Select(static package => package.Key)];

			HashSet<uint> appIDsToRefresh = [];

			foreach (uint packageID in packageIDs.Where(static packageID => Config?.SecretPackageIDs.Contains(packageID) != true)) {
				if (!TickrApp.GlobalDatabase.PackagesDataReadOnly.TryGetValue(packageID, out PackageData? packageData) || (packageData.AppIDs == null)) {
					// Tickr might not have the package info for us at the moment, we'll retry later
					continue;
				}

				appIDsToRefresh.UnionWith(packageData.AppIDs.Where(static appID => (Config?.SecretAppIDs.Contains(appID) != true) && GlobalCache.ShouldRefreshAppInfo(appID)));
			}

			if (appIDsToRefresh.Count == 0) {
				bot.TickrLogger.LogGenericDebug(Strings.BotNoAppsToRefresh);

				return;
			}

			bot.TickrLogger.LogGenericInfo(Strings.FormatBotRetrievingTotalAppAccessTokens(appIDsToRefresh.Count));

			foreach (uint[] appIDsThisRound in appIDsToRefresh.Chunk(Bot.EntriesPerSinglePICSRequest)) {
				if (!bot.IsConnectedAndLoggedOn) {
					return;
				}

				bot.TickrLogger.LogGenericInfo(Strings.FormatBotRetrievingAppAccessTokens(appIDsThisRound.Length));

				SteamApps.PICSTokensCallback response;

				try {
					response = await bot.SteamApps.PICSGetAccessTokens(appIDsThisRound, []).ToLongRunningTask().ConfigureAwait(false);
				} catch (Exception e) {
					bot.TickrLogger.LogGenericWarningException(e);

					continue;
				}

				bot.TickrLogger.LogGenericInfo(Strings.FormatBotFinishedRetrievingAppAccessTokens(appIDsThisRound.Length));

				GlobalCache.UpdateAppTokens(response.AppTokens, response.AppTokensDenied);
			}

			bot.TickrLogger.LogGenericInfo(Strings.FormatBotFinishedRetrievingTotalAppAccessTokens(appIDsToRefresh.Count));
			bot.TickrLogger.LogGenericInfo(Strings.FormatBotRetrievingTotalDepots(appIDsToRefresh.Count));

			(_, FrozenSet<uint>? knownDepotIDs) = await GlobalCache.KnownDepotIDs.GetValue(ECacheFallback.SuccessPreviously).ConfigureAwait(false);

			foreach (uint[] appIDsThisRound in appIDsToRefresh.Chunk(Bot.EntriesPerSinglePICSRequest)) {
				if (!bot.IsConnectedAndLoggedOn) {
					return;
				}

				bot.TickrLogger.LogGenericInfo(Strings.FormatBotRetrievingAppInfos(appIDsThisRound.Length));

				AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet response;

				try {
					response = await bot.SteamApps.PICSGetProductInfo(appIDsThisRound.Select(static appID => new SteamApps.PICSRequest(appID, GlobalCache.GetAppToken(appID))), []).ToLongRunningTask().ConfigureAwait(false);
				} catch (Exception e) {
					bot.TickrLogger.LogGenericWarningException(e);

					continue;
				}

				if (response.Results == null) {
					bot.TickrLogger.LogGenericWarning(Tickr.Localization.Strings.FormatWarningFailedWithError(nameof(response.Results)));

					continue;
				}

				bot.TickrLogger.LogGenericInfo(Strings.FormatBotFinishedRetrievingAppInfos(appIDsThisRound.Length));

				Dictionary<uint, uint> appChangeNumbers = new();

				uint depotKeysSuccessful = 0;
				uint depotKeysTotal = 0;

				foreach (SteamApps.PICSProductInfoCallback.PICSProductInfo app in response.Results.SelectMany(static result => result.Apps.Values)) {
					appChangeNumbers[app.ID] = app.ChangeNumber;

					bool shouldFetchMainKey = false;

					foreach (KeyValue depot in app.KeyValues["depots"].Children) {
						if (!uint.TryParse(depot.Name, out uint depotID) || (knownDepotIDs?.Contains(depotID) == true) || (Config?.SecretDepotIDs.Contains(depotID) == true) || !GlobalCache.ShouldRefreshDepotKey(depotID)) {
							continue;
						}

						depotKeysTotal++;

						await depotsRateLimitingSemaphore.WaitAsync().ConfigureAwait(false);

						try {
							SteamApps.DepotKeyCallback depotResponse = await bot.SteamApps.GetDepotDecryptionKey(depotID, app.ID).ToLongRunningTask().ConfigureAwait(false);

							depotKeysSuccessful++;

							if (depotResponse.Result != EResult.OK) {
								continue;
							}

							shouldFetchMainKey = true;

							GlobalCache.UpdateDepotKey(depotResponse);
						} catch (Exception e) {
							// We can still try other depots
							bot.TickrLogger.LogGenericWarningException(e);
						} finally {
							Utilities.InBackground(async () => {
									await Task.Delay(DepotsRateLimitingDelay).ConfigureAwait(false);

									// ReSharper disable once AccessToDisposedClosure - we're waiting for the semaphore to be free before disposing it
									depotsRateLimitingSemaphore.Release();
								}
							);
						}
					}

					// Consider fetching main appID key only if we've actually considered some new depots for resolving
					if (shouldFetchMainKey && (knownDepotIDs?.Contains(app.ID) != true) && GlobalCache.ShouldRefreshDepotKey(app.ID)) {
						await depotsRateLimitingSemaphore.WaitAsync().ConfigureAwait(false);

						try {
							SteamApps.DepotKeyCallback depotResponse = await bot.SteamApps.GetDepotDecryptionKey(app.ID, app.ID).ToLongRunningTask().ConfigureAwait(false);

							// Increment total in combination with successful, we allow this one to fail on us
							depotKeysTotal++;
							depotKeysSuccessful++;

							GlobalCache.UpdateDepotKey(depotResponse);
						} catch (Exception e) {
							// We can still try other depots
							bot.TickrLogger.LogGenericWarningException(e);
						} finally {
							Utilities.InBackground(async () => {
									await Task.Delay(DepotsRateLimitingDelay).ConfigureAwait(false);

									// ReSharper disable once AccessToDisposedClosure - we're waiting for the semaphore to be free before disposing it
									depotsRateLimitingSemaphore.Release();
								}
							);
						}
					}
				}

				if (depotKeysTotal > 0) {
					bot.TickrLogger.LogGenericInfo(Strings.FormatBotFinishedRetrievingDepotKeys(depotKeysSuccessful, depotKeysTotal));
				}

				if (depotKeysSuccessful < depotKeysTotal) {
					// We're not going to record app change numbers, as we didn't fetch all the depot keys we wanted
					continue;
				}

				GlobalCache.UpdateAppChangeNumbers(appChangeNumbers);
			}

			bot.TickrLogger.LogGenericInfo(Strings.FormatBotFinishedRetrievingTotalDepots(appIDsToRefresh.Count));
		} finally {
			if (Config?.Enabled == true) {
				TimeSpan timeSpan = TimeSpan.FromHours(SharedInfo.MaximumHoursBetweenRefresh);

				synchronization.RefreshTimer.Change(timeSpan, timeSpan);
			}

			await depotsRateLimitingSemaphore.WaitAsync().ConfigureAwait(false);

			synchronization.RefreshSemaphore.Release();

			depotsRateLimitingSemaphore.Dispose();
		}
	}

	private static string? ResponseRefreshManually(EAccess access, Bot bot) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentNullException.ThrowIfNull(bot);

		if (access < EAccess.Master) {
			return access > EAccess.None ? bot.Commands.FormatBotResponse(Tickr.Localization.Strings.ErrorAccessDenied) : null;
		}

		if (GlobalCache == null) {
			return bot.Commands.FormatBotResponse(Tickr.Localization.Strings.FormatWarningFailedWithError(nameof(GlobalCache)));
		}

		Utilities.InBackground(async () => {
				await Refresh(bot).ConfigureAwait(false);
				await SubmitData().ConfigureAwait(false);
			}
		);

		return bot.Commands.FormatBotResponse(Tickr.Localization.Strings.Done);
	}

	private static string? ResponseRefreshManually(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Owner ? Commands.FormatStaticResponse(Tickr.Localization.Strings.FormatBotNotFound(botNames)) : null;
		}

		if (bots.RemoveWhere(bot => Commands.GetProxyAccess(bot, access, steamID) < EAccess.Master) > 0) {
			if (bots.Count == 0) {
				return access >= EAccess.Owner ? Commands.FormatStaticResponse(Tickr.Localization.Strings.FormatBotNotFound(botNames)) : null;
			}
		}

		if (GlobalCache == null) {
			return Commands.FormatStaticResponse(Tickr.Localization.Strings.FormatWarningFailedWithError(nameof(GlobalCache)));
		}

		Utilities.InBackground(async () => {
				await Utilities.InParallel(bots.Select(static bot => Refresh(bot))).ConfigureAwait(false);

				await SubmitData().ConfigureAwait(false);
			}
		);

		return Commands.FormatStaticResponse(Tickr.Localization.Strings.Done);
	}

	private static async Task SubmitData(CancellationToken cancellationToken = default) {
		if (Bot.Bots == null) {
			throw new InvalidOperationException(nameof(Bot.Bots));
		}

		if (GlobalCache == null) {
			throw new InvalidOperationException(nameof(GlobalCache));
		}

		if (TickrApp.WebBrowser == null) {
			throw new InvalidOperationException(nameof(TickrApp.WebBrowser));
		}

		if (LastUploadAt + TimeSpan.FromMinutes(SharedInfo.MinimumMinutesBetweenUploads) > DateTimeOffset.UtcNow) {
			return;
		}

		if (!await SubmissionSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false)) {
			return;
		}

		try {
			Dictionary<uint, ulong> appTokens = GlobalCache.GetAppTokensForSubmission();
			Dictionary<uint, ulong> packageTokens = GlobalCache.GetPackageTokensForSubmission();
			Dictionary<uint, string> depotKeys = GlobalCache.GetDepotKeysForSubmission();

			if ((appTokens.Count == 0) && (packageTokens.Count == 0) && (depotKeys.Count == 0)) {
				TickrApp.TickrLogger.LogGenericInfo(Strings.SubmissionNoNewData);

				return;
			}

			ulong contributorSteamID = TickrApp.GlobalConfig is { SteamOwnerID: > 0 } && new SteamID(TickrApp.GlobalConfig.SteamOwnerID).IsIndividualAccount ? TickrApp.GlobalConfig.SteamOwnerID : Bot.Bots.Values.Where(static bot => bot.SteamID > 0).MaxBy(static bot => bot.OwnedPackages.Count)?.SteamID ?? 0;

			if (contributorSteamID == 0) {
				TickrApp.TickrLogger.LogGenericError(Strings.FormatSubmissionNoContributorSet(nameof(TickrApp.GlobalConfig.SteamOwnerID)));

				return;
			}

			Uri request = new($"{SharedInfo.ServerURL}/submit");
			SubmitRequest data = new(contributorSteamID, appTokens, packageTokens, depotKeys);

			TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionInProgress(appTokens.Count, packageTokens.Count, depotKeys.Count));

			ObjectResponse<SubmitResponse>? response = await TickrApp.WebBrowser.UrlPostToJsonObject<SubmitResponse, SubmitRequest>(request, data: data, requestOptions: WebBrowser.ERequestOptions.ReturnClientErrors | WebBrowser.ERequestOptions.AllowInvalidBodyOnErrors, cancellationToken: cancellationToken).ConfigureAwait(false);

			if (response == null) {
				TickrApp.TickrLogger.LogGenericWarning(Tickr.Localization.Strings.WarningFailed);

				return;
			}

			// We've communicated with the server and didn't timeout, regardless of the success, this was the last upload attempt
			LastUploadAt = DateTimeOffset.UtcNow;

			if (response.StatusCode.IsClientErrorCode()) {
				TickrApp.TickrLogger.LogGenericWarning(Tickr.Localization.Strings.FormatWarningFailedWithError(response.StatusCode));

				switch (response.StatusCode) {
					case HttpStatusCode.Forbidden when Config?.Enabled == true:
						// SteamDB told us to stop submitting data for now
						// ReSharper disable once SuspiciousLockOverSynchronizationPrimitive - this is not a mistake, we need extra synchronization, and we can re-use the semaphore object for that
						lock (SubmissionSemaphore) {
							SubmissionTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
						}

						break;
					case HttpStatusCode.Conflict:
						// SteamDB told us to reset our cache
						GlobalCache.Reset(true);

						break;
					case HttpStatusCode.TooManyRequests when Config?.Enabled == true:
						// SteamDB told us to try again later
#pragma warning disable CA5394 // This call isn't used in a security-sensitive manner
						TimeSpan startIn = TimeSpan.FromMinutes(Random.Shared.Next(SharedInfo.MinimumMinutesBeforeFirstUpload, SharedInfo.MaximumMinutesBeforeFirstUpload));
#pragma warning restore CA5394 // This call isn't used in a security-sensitive manner

						// ReSharper disable once SuspiciousLockOverSynchronizationPrimitive - this is not a mistake, we need extra synchronization, and we can re-use the semaphore object for that
						lock (SubmissionSemaphore) {
							SubmissionTimer.Change(startIn, TimeSpan.FromHours(SharedInfo.HoursBetweenUploads));
						}

						TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionFailedTooManyRequests(startIn.ToHumanReadable()));

						break;
				}

				return;
			}

			if (response.Content is not { Success: true }) {
				TickrApp.TickrLogger.LogGenericError(Tickr.Localization.Strings.WarningFailed);

				return;
			}

			if (response.Content.Data == null) {
				TickrApp.TickrLogger.LogGenericError(Tickr.Localization.Strings.FormatErrorIsInvalid(nameof(response.Content.Data)));

				return;
			}

			TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionSuccessful(response.Content.Data.NewApps.Count, response.Content.Data.VerifiedApps.Count, response.Content.Data.NewPackages.Count, response.Content.Data.VerifiedPackages.Count, response.Content.Data.NewDepots.Count, response.Content.Data.VerifiedDepots.Count));

			GlobalCache.UpdateSubmittedData(appTokens, packageTokens, depotKeys);

			if (!response.Content.Data.NewApps.IsEmpty) {
				TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionSuccessfulNewApps(string.Join(", ", response.Content.Data.NewApps)));
			}

			if (!response.Content.Data.VerifiedApps.IsEmpty) {
				TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionSuccessfulVerifiedApps(string.Join(", ", response.Content.Data.VerifiedApps)));
			}

			if (!response.Content.Data.NewPackages.IsEmpty) {
				TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionSuccessfulNewPackages(string.Join(", ", response.Content.Data.NewPackages)));
			}

			if (!response.Content.Data.VerifiedPackages.IsEmpty) {
				TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionSuccessfulVerifiedPackages(string.Join(", ", response.Content.Data.VerifiedPackages)));
			}

			if (!response.Content.Data.NewDepots.IsEmpty) {
				TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionSuccessfulNewDepots(string.Join(", ", response.Content.Data.NewDepots)));
			}

			if (!response.Content.Data.VerifiedDepots.IsEmpty) {
				TickrApp.TickrLogger.LogGenericInfo(Strings.FormatSubmissionSuccessfulVerifiedDepots(string.Join(", ", response.Content.Data.VerifiedDepots)));
			}
		} finally {
			SubmissionSemaphore.Release();
		}
	}
}
