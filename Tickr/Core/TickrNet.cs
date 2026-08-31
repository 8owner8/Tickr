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
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Tickr.Helpers;
using Tickr.IPC.Responses;
using Tickr.Steam;
using Tickr.Steam.Integration;
using Tickr.Web;
using Tickr.Web.Responses;
using SteamKit2;

namespace Tickr.Core;

internal static class TickrNet {
	internal static Uri URL => new("https://TickrApp.Tickr.dev");

	private static readonly TickrCacheable<IReadOnlyCollection<ulong>> CachedBadBots = new(ResolveCachedBadBots, TimeSpan.FromDays(1));

	internal static async Task<string?> FetchBuildChecksum(Version version, string variant, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(version);
		ArgumentException.ThrowIfNullOrEmpty(variant);

		if (TickrApp.WebBrowser == null) {
			throw new InvalidOperationException(nameof(TickrApp.WebBrowser));
		}

		Uri request = new(URL, $"/Api/Checksum/{version}/{variant}");

		ObjectResponse<GenericResponse<string>>? response;

		try {
			response = await TickrApp.WebBrowser.UrlGetToJsonObject<GenericResponse<string>>(request, cancellationToken: cancellationToken).ConfigureAwait(false);
		} catch (OperationCanceledException e) {
			TickrApp.TickrLogger.LogGenericDebuggingException(e);

			return null;
		}

		if (response?.Content == null) {
			return null;
		}

		return response.Content.Result ?? "";
	}

	internal static async Task<bool?> IsBadBot(ulong steamID, CancellationToken cancellationToken = default) {
		if ((steamID == 0) || !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		(_, IReadOnlyCollection<ulong>? badBots) = await CachedBadBots.GetValue(ECacheFallback.FailedNow, cancellationToken).ConfigureAwait(false);

		return badBots?.Contains(steamID);
	}

	internal static async Task<HttpStatusCode?> SignInWithSteam(Bot bot, WebBrowser webBrowser, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentNullException.ThrowIfNull(webBrowser);

		if (!bot.IsConnectedAndLoggedOn) {
			return null;
		}

		// We expect data or redirection to Steam OpenID
		Uri authenticateRequest = new(URL, $"/Api/Steam/Authenticate?steamID={bot.SteamID}");

		ObjectResponse<GenericResponse<ulong>>? authenticateResponse = await webBrowser.UrlGetToJsonObject<GenericResponse<ulong>>(authenticateRequest, requestOptions: WebBrowser.ERequestOptions.ReturnRedirections | WebBrowser.ERequestOptions.ReturnClientErrors | WebBrowser.ERequestOptions.AllowInvalidBodyOnErrors, cancellationToken: cancellationToken).ConfigureAwait(false);

		if (authenticateResponse == null) {
			return null;
		}

		if (authenticateResponse.StatusCode.IsClientErrorCode()) {
			return authenticateResponse.StatusCode;
		}

		if (authenticateResponse.StatusCode.IsSuccessCode()) {
			return authenticateResponse.Content?.Result == bot.SteamID ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
		}

		// We've got a redirection, initiate OpenID procedure by following it
		using HtmlDocumentResponse? challengeResponse = await bot.TickrWebHandler.UrlGetToHtmlDocumentWithSession(authenticateResponse.FinalUri, cancellationToken: cancellationToken).ConfigureAwait(false);

		if (challengeResponse?.Content == null) {
			return null;
		}

		IElement? paramsNode = challengeResponse.Content.QuerySelector("input[name='openidparams'][value]");

		if (paramsNode == null) {
			TickrApp.TickrLogger.LogNullError(paramsNode);

			return null;
		}

		string? paramsValue = paramsNode.GetAttribute("value");

		if (string.IsNullOrEmpty(paramsValue)) {
			TickrApp.TickrLogger.LogNullError(paramsValue);

			return null;
		}

		IElement? nonceNode = challengeResponse.Content.QuerySelector("input[name='nonce'][value]");

		if (nonceNode == null) {
			TickrApp.TickrLogger.LogNullError(nonceNode);

			return null;
		}

		string? nonceValue = nonceNode.GetAttribute("value");

		if (string.IsNullOrEmpty(nonceValue)) {
			TickrApp.TickrLogger.LogNullError(nonceValue);

			return null;
		}

		Uri loginRequest = new(TickrWebHandler.SteamCommunityURL, "/openid/login");

		using StringContent actionContent = new("steam_openid_login");
		using StringContent modeContent = new("checkid_setup");
		using StringContent paramsContent = new(paramsValue);
		using StringContent nonceContent = new(nonceValue);

		using MultipartFormDataContent data = new();

		data.Add(actionContent, "action");
		data.Add(modeContent, "openid.mode");
		data.Add(paramsContent, "openidparams");
		data.Add(nonceContent, "nonce");

		// Accept OpenID request presented and follow redirection back to the data we initially expected
		BasicResponse? loginResponse = await bot.TickrWebHandler.WebBrowser.UrlPost(loginRequest, data: data, requestOptions: WebBrowser.ERequestOptions.ReturnRedirections, cancellationToken: cancellationToken).ConfigureAwait(false);

		if (loginResponse == null) {
			return null;
		}

		// We've got a final redirection, follow it and complete login procedure
		authenticateResponse = await webBrowser.UrlGetToJsonObject<GenericResponse<ulong>>(loginResponse.FinalUri, requestOptions: WebBrowser.ERequestOptions.ReturnClientErrors | WebBrowser.ERequestOptions.AllowInvalidBodyOnErrors, cancellationToken: cancellationToken).ConfigureAwait(false);

		if (authenticateResponse == null) {
			return null;
		}

		if (authenticateResponse.StatusCode.IsClientErrorCode()) {
			return authenticateResponse.StatusCode;
		}

		return authenticateResponse.Content?.Result == bot.SteamID ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
	}

	private static async Task<(bool Success, IReadOnlyCollection<ulong>? Result)> ResolveCachedBadBots(CancellationToken cancellationToken = default) {
		if (TickrApp.GlobalDatabase == null) {
			throw new InvalidOperationException(nameof(TickrApp.GlobalDatabase));
		}

		if (TickrApp.WebBrowser == null) {
			throw new InvalidOperationException(nameof(TickrApp.WebBrowser));
		}

		Uri request = new(URL, "/Api/BadBots");

		ObjectResponse<GenericResponse<ImmutableHashSet<ulong>>>? response;

		try {
			response = await TickrApp.WebBrowser.UrlGetToJsonObject<GenericResponse<ImmutableHashSet<ulong>>>(request, cancellationToken: cancellationToken).ConfigureAwait(false);
		} catch (OperationCanceledException e) {
			TickrApp.TickrLogger.LogGenericDebuggingException(e);

			return (false, TickrApp.GlobalDatabase.CachedBadBots);
		}

		if (response?.Content?.Result == null) {
			return (false, TickrApp.GlobalDatabase.CachedBadBots);
		}

		TickrApp.GlobalDatabase.CachedBadBots.ReplaceIfNeededWith(response.Content.Result);

		return (true, response.Content.Result);
	}
}
