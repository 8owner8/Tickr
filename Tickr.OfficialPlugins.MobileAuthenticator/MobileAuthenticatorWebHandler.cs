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
using System.Net.Http;
using System.Threading.Tasks;
using Tickr.Core;
using Tickr.Localization;
using Tickr.Steam;
using Tickr.Steam.Integration;
using Tickr.Web;
using SteamKit2;
using SteamKit2.Internal;

namespace Tickr.OfficialPlugins.MobileAuthenticator;

internal static class MobileAuthenticatorWebHandler {
	private const string TwoFactorService = "ITwoFactorService";

	internal static async Task<CTwoFactor_AddAuthenticator_Response?> AddAuthenticator(Bot bot, string deviceID) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentException.ThrowIfNullOrEmpty(deviceID);

		if (!bot.IsConnectedAndLoggedOn) {
			return null;
		}

		string? accessToken = bot.AccessToken;

		if (string.IsNullOrEmpty(accessToken)) {
			return null;
		}

		const string endpoint = "AddAuthenticator";
		HttpMethod method = HttpMethod.Post;

		CTwoFactor_AddAuthenticator_Request request = new() {
			authenticator_time = Utilities.GetUnixTime(),
			authenticator_type = 1,
			device_identifier = deviceID,
			steamid = bot.SteamID
		};

		Dictionary<string, object?> arguments = new(1, StringComparer.Ordinal) {
			{ "access_token", accessToken }
		};

		using WebAPI.AsyncInterface twoFactorService = bot.SteamConfiguration.GetAsyncWebAPIInterface(TwoFactorService);

		twoFactorService.Timeout = bot.TickrWebHandler.WebBrowser.Timeout;

		WebAPI.WebAPIResponse<CTwoFactor_AddAuthenticator_Response>? response = null;

		for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
			if ((i > 0) && (TickrWebHandler.WebLimiterDelay > 0)) {
				await Task.Delay(TickrWebHandler.WebLimiterDelay).ConfigureAwait(false);
			}

			if (Debugging.IsUserDebugging) {
				bot.TickrLogger.LogGenericDebug($"{method} {bot.SteamConfiguration.WebAPIBaseAddress}{TwoFactorService}/{endpoint}");
			}

			try {
				response = await TickrWebHandler.WebLimitRequest(
					bot.SteamConfiguration.WebAPIBaseAddress,

					// ReSharper disable once AccessToDisposedClosure
					async () => await twoFactorService.CallProtobufAsync<CTwoFactor_AddAuthenticator_Response, CTwoFactor_AddAuthenticator_Request>(method, endpoint, request, extraArgs: arguments).ConfigureAwait(false)
				).ConfigureAwait(false);
			} catch (TaskCanceledException e) {
				bot.TickrLogger.LogGenericDebuggingException(e);
			} catch (Exception e) {
				bot.TickrLogger.LogGenericWarningException(e);
			}
		}

		if (response == null) {
			bot.TickrLogger.LogGenericWarning(Strings.FormatErrorRequestFailedTooManyTimes(WebBrowser.MaxTries));

			return null;
		}

		return response.Body;
	}

	internal static async Task<CTwoFactor_FinalizeAddAuthenticator_Response?> FinalizeAuthenticator(Bot bot, string activationCode, string authenticatorCode, ulong authenticatorTime) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentException.ThrowIfNullOrEmpty(activationCode);
		ArgumentException.ThrowIfNullOrEmpty(authenticatorCode);
		ArgumentOutOfRangeException.ThrowIfZero(authenticatorTime);

		if (!bot.IsConnectedAndLoggedOn) {
			return null;
		}

		string? accessToken = bot.AccessToken;

		if (string.IsNullOrEmpty(accessToken)) {
			return null;
		}

		const string endpoint = "FinalizeAddAuthenticator";
		HttpMethod method = HttpMethod.Post;

		CTwoFactor_FinalizeAddAuthenticator_Request request = new() {
			activation_code = activationCode,
			authenticator_code = authenticatorCode,
			authenticator_time = authenticatorTime,
			steamid = bot.SteamID
		};

		Dictionary<string, object?> arguments = new(1, StringComparer.Ordinal) {
			{ "access_token", accessToken }
		};

		using WebAPI.AsyncInterface twoFactorService = bot.SteamConfiguration.GetAsyncWebAPIInterface(TwoFactorService);

		twoFactorService.Timeout = bot.TickrWebHandler.WebBrowser.Timeout;

		WebAPI.WebAPIResponse<CTwoFactor_FinalizeAddAuthenticator_Response>? response = null;

		for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
			if ((i > 0) && (TickrWebHandler.WebLimiterDelay > 0)) {
				await Task.Delay(TickrWebHandler.WebLimiterDelay).ConfigureAwait(false);
			}

			if (Debugging.IsUserDebugging) {
				bot.TickrLogger.LogGenericDebug($"{method} {bot.SteamConfiguration.WebAPIBaseAddress}{TwoFactorService}/{endpoint}");
			}

			try {
				response = await TickrWebHandler.WebLimitRequest(
					bot.SteamConfiguration.WebAPIBaseAddress,

					// ReSharper disable once AccessToDisposedClosure
					async () => await twoFactorService.CallProtobufAsync<CTwoFactor_FinalizeAddAuthenticator_Response, CTwoFactor_FinalizeAddAuthenticator_Request>(method, endpoint, request, extraArgs: arguments).ConfigureAwait(false)
				).ConfigureAwait(false);
			} catch (TaskCanceledException e) {
				bot.TickrLogger.LogGenericDebuggingException(e);
			} catch (Exception e) {
				bot.TickrLogger.LogGenericWarningException(e);
			}
		}

		if (response == null) {
			bot.TickrLogger.LogGenericWarning(Strings.FormatErrorRequestFailedTooManyTimes(WebBrowser.MaxTries));

			return null;
		}

		return response.Body;
	}
}
