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
using System.ComponentModel;
using System.Threading.Tasks;
using Tickr.Core;
using Tickr.Localization;
using Tickr.Steam.Security;
using Tickr.Storage;
using SteamKit2;
using SteamKit2.Authentication;

namespace Tickr.Steam.Integration;

internal sealed class BotCredentialsProvider : IAuthenticator {
	private const byte MaxLoginFailures = 3;

	private readonly Bot Bot;

	internal byte LoginFailures { get; private set; }

	internal BotCredentialsProvider(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		Bot = bot;
	}

	public Task<bool> AcceptDeviceConfirmationAsync() {
		if (Program.Service || (TickrApp.GlobalConfig?.Headless ?? GlobalConfig.DefaultHeadless)) {
			// In headless/service mode, we always fallback to the code instead, as user can't confirm future popup from the next login procedure, and we never wait for current one
			return Task.FromResult(false);
		}

		if (Bot.HasMobileAuthenticator || Bot.HasLoginCodeReady) {
			// We don't want device confirmation under any circumstance, we can provide the code on our own
			return Task.FromResult(false);
		}

		// SteamKit polls the login session after this returns true. Requiring an extra console/API
		// answer here meant that approving the notification on the phone alone could never finish
		// the login. Desktop users now only need to approve the request in the Steam mobile app.
		return Task.FromResult(true);
	}

	public async Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect) => await ProvideInput(TickrApp.EUserInputType.TwoFactorAuthentication, previousCodeWasIncorrect).ConfigureAwait(false);

	public async Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect) => await ProvideInput(TickrApp.EUserInputType.SteamGuard, previousCodeWasIncorrect).ConfigureAwait(false);

	private async Task<string> ProvideInput(TickrApp.EUserInputType inputType, bool previousCodeWasIncorrect) {
		if (!Enum.IsDefined(inputType)) {
			throw new InvalidEnumArgumentException(nameof(inputType), (int) inputType, typeof(TickrApp.EUserInputType));
		}

		EResult reason = inputType == TickrApp.EUserInputType.TwoFactorAuthentication ? EResult.TwoFactorCodeMismatch : EResult.InvalidLoginAuthCode;

		if (previousCodeWasIncorrect) {
			Bot.TickrLogger.LogGenericWarning(Strings.FormatBotUnableToLogin(reason, reason));

			if (++LoginFailures >= MaxLoginFailures) {
				throw new BotAuthenticationException(reason);
			}
		}

		string? input = await Bot.RequestInput(inputType, previousCodeWasIncorrect).ConfigureAwait(false);

		if (string.IsNullOrEmpty(input)) {
			Bot.TickrLogger.LogGenericWarning(Strings.FormatErrorIsEmpty(nameof(input)));

			LoginFailures = MaxLoginFailures;

			throw new BotAuthenticationException(reason);
		}

		return input;
	}
}
