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
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tickr.Core;
using Tickr.Helpers;
using Tickr.Helpers.Json;
using Tickr.OfficialPlugins.SteamTokenDumper.Localization;
using Tickr.Web.Responses;
using JetBrains.Annotations;
using SteamKit2;

namespace Tickr.OfficialPlugins.SteamTokenDumper;

internal sealed class GlobalCache : SerializableFile {
	internal static readonly TickrCacheable<FrozenSet<uint>> KnownDepotIDs = new(ResolveKnownDepotIDs, TimeSpan.FromDays(7));

	private static string SharedFilePath => Path.Combine(Tickr.SharedInfo.ConfigDirectory, $"{nameof(SteamTokenDumper)}.cache");

	[JsonInclude]
	internal uint LastChangeNumber { get; private set; }

	[JsonDisallowNull]
	[JsonInclude]
	private ConcurrentDictionary<uint, uint> AppChangeNumbers { get; init; } = new();

	[JsonDisallowNull]
	[JsonInclude]
	private ConcurrentDictionary<uint, ulong> AppTokens { get; init; } = new();

	[JsonDisallowNull]
	[JsonInclude]
	private ConcurrentDictionary<uint, string> DepotKeys { get; init; } = new();

	[JsonDisallowNull]
	[JsonInclude]
	private ConcurrentDictionary<uint, ulong> SubmittedApps { get; init; } = new();

	[JsonDisallowNull]
	[JsonInclude]
	private ConcurrentDictionary<uint, string> SubmittedDepots { get; init; } = new();

	[JsonDisallowNull]
	[JsonInclude]
	private ConcurrentDictionary<uint, ulong> SubmittedPackages { get; init; } = new();

	[JsonConstructor]
	internal GlobalCache() => FilePath = SharedFilePath;

	[UsedImplicitly]
	public bool ShouldSerializeAppChangeNumbers() => !AppChangeNumbers.IsEmpty;

	[UsedImplicitly]
	public bool ShouldSerializeAppTokens() => !AppTokens.IsEmpty;

	[UsedImplicitly]
	public bool ShouldSerializeDepotKeys() => !DepotKeys.IsEmpty;

	[UsedImplicitly]
	public bool ShouldSerializeLastChangeNumber() => LastChangeNumber > 0;

	[UsedImplicitly]
	public bool ShouldSerializeSubmittedApps() => !SubmittedApps.IsEmpty;

	[UsedImplicitly]
	public bool ShouldSerializeSubmittedDepots() => !SubmittedDepots.IsEmpty;

	[UsedImplicitly]
	public bool ShouldSerializeSubmittedPackages() => !SubmittedPackages.IsEmpty;

	protected override Task Save() => Save(this);

	internal ulong GetAppToken(uint appID) => AppTokens[appID];

	internal Dictionary<uint, ulong> GetAppTokensForSubmission() => AppTokens.Where(appToken => (SteamTokenDumperPlugin.Config?.SecretAppIDs.Contains(appToken.Key) != true) && (appToken.Value > 0) && (!SubmittedApps.TryGetValue(appToken.Key, out ulong token) || (appToken.Value != token))).ToDictionary(static appToken => appToken.Key, static appToken => appToken.Value);
	internal Dictionary<uint, string> GetDepotKeysForSubmission() => DepotKeys.Where(depotKey => (SteamTokenDumperPlugin.Config?.SecretDepotIDs.Contains(depotKey.Key) != true) && !string.IsNullOrEmpty(depotKey.Value) && (!SubmittedDepots.TryGetValue(depotKey.Key, out string? key) || (depotKey.Value != key))).ToDictionary(static depotKey => depotKey.Key, static depotKey => depotKey.Value);

	internal Dictionary<uint, ulong> GetPackageTokensForSubmission() {
		if (TickrApp.GlobalDatabase == null) {
			throw new InvalidOperationException(nameof(TickrApp.GlobalDatabase));
		}

		return TickrApp.GlobalDatabase.PackageAccessTokensReadOnly.Where(packageToken => (SteamTokenDumperPlugin.Config?.SecretPackageIDs.Contains(packageToken.Key) != true) && (packageToken.Value > 0) && (!SubmittedPackages.TryGetValue(packageToken.Key, out ulong token) || (packageToken.Value != token))).ToDictionary(static packageToken => packageToken.Key, static packageToken => packageToken.Value);
	}

	internal static async Task<GlobalCache?> Load() {
		if (!File.Exists(SharedFilePath)) {
			return new GlobalCache();
		}

		TickrApp.TickrLogger.LogGenericInfo(Strings.LoadingGlobalCache);

		GlobalCache? globalCache;

		try {
			string json = await File.ReadAllTextAsync(SharedFilePath).ConfigureAwait(false);

			if (string.IsNullOrEmpty(json)) {
				TickrApp.TickrLogger.LogGenericError(Tickr.Localization.Strings.FormatErrorIsEmpty(nameof(json)));

				return null;
			}

			globalCache = json.ToJsonObject<GlobalCache>();
		} catch (Exception e) {
			TickrApp.TickrLogger.LogGenericException(e);

			return null;
		}

		if (globalCache == null) {
			TickrApp.TickrLogger.LogNullError(globalCache);

			return null;
		}

		TickrApp.TickrLogger.LogGenericInfo(Strings.ValidatingGlobalCacheIntegrity);

		if (globalCache.DepotKeys.Values.Any(static depotKey => !IsValidDepotKey(depotKey))) {
			TickrApp.TickrLogger.LogGenericError(Strings.GlobalCacheIntegrityValidationFailed);

			return null;
		}

		return globalCache;
	}

	internal void OnPICSChanges(uint currentChangeNumber, IReadOnlyCollection<KeyValuePair<uint, SteamApps.PICSChangesCallback.PICSChangeData>> appChanges) {
		ArgumentOutOfRangeException.ThrowIfZero(currentChangeNumber);
		ArgumentNullException.ThrowIfNull(appChanges);

		if (currentChangeNumber <= LastChangeNumber) {
			return;
		}

		LastChangeNumber = currentChangeNumber;

		foreach ((uint appID, SteamApps.PICSChangesCallback.PICSChangeData appData) in appChanges) {
			if (!AppChangeNumbers.TryGetValue(appID, out uint previousChangeNumber) || (previousChangeNumber >= appData.ChangeNumber)) {
				continue;
			}

			AppChangeNumbers.TryRemove(appID, out _);
		}

		Utilities.InBackground(Save);
	}

	internal void OnPICSChangesRestart(uint currentChangeNumber) {
		ArgumentOutOfRangeException.ThrowIfZero(currentChangeNumber);

		if (currentChangeNumber <= LastChangeNumber) {
			return;
		}

		LastChangeNumber = currentChangeNumber;

		Reset();
	}

	internal void Reset(bool clear = false) {
		AppChangeNumbers.Clear();

		if (clear) {
			AppTokens.Clear();
			DepotKeys.Clear();
		}

		Utilities.InBackground(Save);
	}

	internal bool ShouldRefreshAppInfo(uint appID) => !AppChangeNumbers.ContainsKey(appID);
	internal bool ShouldRefreshDepotKey(uint depotID) => !DepotKeys.ContainsKey(depotID);

	internal void UpdateAppChangeNumbers(IReadOnlyCollection<KeyValuePair<uint, uint>> appChangeNumbers) {
		ArgumentNullException.ThrowIfNull(appChangeNumbers);

		bool save = false;

		foreach ((uint appID, uint changeNumber) in appChangeNumbers) {
			if (AppChangeNumbers.TryGetValue(appID, out uint previousChangeNumber) && (previousChangeNumber >= changeNumber)) {
				continue;
			}

			AppChangeNumbers[appID] = changeNumber;
			save = true;
		}

		if (save) {
			Utilities.InBackground(Save);
		}
	}

	internal void UpdateAppTokens(IReadOnlyCollection<KeyValuePair<uint, ulong>> appTokens, IReadOnlyCollection<uint> publicAppIDs) {
		ArgumentNullException.ThrowIfNull(appTokens);
		ArgumentNullException.ThrowIfNull(publicAppIDs);

		bool save = false;

		foreach ((uint appID, ulong appToken) in appTokens) {
			if (AppTokens.TryGetValue(appID, out ulong previousAppToken) && (previousAppToken == appToken)) {
				continue;
			}

			AppTokens[appID] = appToken;
			save = true;
		}

		foreach (uint appID in publicAppIDs) {
			if (AppTokens.TryGetValue(appID, out ulong previousAppToken) && (previousAppToken == 0)) {
				continue;
			}

			AppTokens[appID] = 0;
			save = true;
		}

		if (save) {
			Utilities.InBackground(Save);
		}
	}

	internal void UpdateDepotKey(SteamApps.DepotKeyCallback depotKeyResult) {
		ArgumentNullException.ThrowIfNull(depotKeyResult);

		if (depotKeyResult.Result != EResult.OK) {
			return;
		}

		string depotKey = Convert.ToHexString(depotKeyResult.DepotKey);

		if (!IsValidDepotKey(depotKey)) {
			TickrApp.TickrLogger.LogGenericWarning(Tickr.Localization.Strings.FormatErrorIsInvalid(nameof(depotKey)));

			return;
		}

		if (DepotKeys.TryGetValue(depotKeyResult.DepotID, out string? previousDepotKey) && (previousDepotKey == depotKey)) {
			return;
		}

		DepotKeys[depotKeyResult.DepotID] = depotKey;

		Utilities.InBackground(Save);
	}

	internal void UpdateSubmittedData(IReadOnlyDictionary<uint, ulong> apps, IReadOnlyDictionary<uint, ulong> packages, IReadOnlyDictionary<uint, string> depots) {
		ArgumentNullException.ThrowIfNull(apps);
		ArgumentNullException.ThrowIfNull(packages);
		ArgumentNullException.ThrowIfNull(depots);

		bool save = false;

		foreach ((uint appID, ulong token) in apps) {
			if (SubmittedApps.TryGetValue(appID, out ulong previousToken) && (previousToken == token)) {
				continue;
			}

			SubmittedApps[appID] = token;
			save = true;
		}

		foreach ((uint packageID, ulong token) in packages) {
			if (SubmittedPackages.TryGetValue(packageID, out ulong previousToken) && (previousToken == token)) {
				continue;
			}

			SubmittedPackages[packageID] = token;
			save = true;
		}

		foreach ((uint depotID, string key) in depots) {
			if (SubmittedDepots.TryGetValue(depotID, out string? previousKey) && (previousKey == key)) {
				continue;
			}

			SubmittedDepots[depotID] = key;
			save = true;
		}

		if (save) {
			Utilities.InBackground(Save);
		}
	}

	private static bool IsValidDepotKey(string depotKey) {
		ArgumentException.ThrowIfNullOrEmpty(depotKey);

		return (depotKey.Length == 64) && Utilities.IsValidHexadecimalText(depotKey);
	}

	private static async Task<(bool Success, FrozenSet<uint>? Result)> ResolveKnownDepotIDs(CancellationToken cancellationToken = default) {
		if (TickrApp.WebBrowser == null) {
			throw new InvalidOperationException(nameof(TickrApp.WebBrowser));
		}

		Uri request = new($"{SharedInfo.ServerURL}/knowndepots.csv");

		StreamResponse? response = await TickrApp.WebBrowser.UrlGetToStream(request, cancellationToken: cancellationToken).ConfigureAwait(false);

		if (response == null) {
			return (false, null);
		}

		HashSet<uint> result;

		await using (response.ConfigureAwait(false)) {
			if (response.Content == null) {
				return (false, null);
			}

			try {
				using StreamReader reader = new(response.Content);

				string? countText = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

				if (string.IsNullOrEmpty(countText) || !int.TryParse(countText, out int count) || (count <= 0)) {
					TickrApp.TickrLogger.LogNullError(countText);

					return (false, null);
				}

				result = [with(count)];

				while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { Length: > 0 } line) {
					if (!uint.TryParse(line, out uint depotID) || (depotID == 0)) {
						TickrApp.TickrLogger.LogNullError(depotID);

						continue;
					}

					result.Add(depotID);
				}
			} catch (Exception e) {
				TickrApp.TickrLogger.LogGenericWarningException(e);

				return (false, null);
			}
		}

		return (result.Count > 0, result.ToFrozenSet());
	}
}
