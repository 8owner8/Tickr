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
using System.Net;
using System.Threading.Tasks;
using Tickr.IPC.Responses;
using Tickr.Localization;
using Tickr.Steam;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tickr.IPC.Controllers.Api;

[Route("/Api/Bot/{botName:required}/QrLogin")]
public sealed class QrLoginController : TickrController {
	[EndpointSummary("Initiates QR login for given bot")]
	[HttpPost]
	[ProducesResponseType<GenericResponse<QrLoginResponse>>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.BadRequest)]
	public async Task<ActionResult<GenericResponse>> Post(string botName) {
		ArgumentException.ThrowIfNullOrEmpty(botName);

		Bot? bot = await ResolveBot(botName).ConfigureAwait(false);

		if (bot == null) {
			return BadRequest(new GenericResponse(false, Strings.FormatBotNotFound(botName)));
		}

		if (!await bot.InitQrLogin().ConfigureAwait(false)) {
			return StatusCode((int) HttpStatusCode.ServiceUnavailable, new GenericResponse(false, Strings.WarningFailed));
		}

		return Ok(new GenericResponse<QrLoginResponse>(new QrLoginResponse(bot)));
	}

	[EndpointSummary("Fetches QR login status of given bot")]
	[HttpGet]
	[ProducesResponseType<GenericResponse<QrLoginResponse>>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.BadRequest)]
	public async Task<ActionResult<GenericResponse>> Get(string botName) {
		ArgumentException.ThrowIfNullOrEmpty(botName);

		Bot? bot = await ResolveBot(botName).ConfigureAwait(false);

		if (bot == null) {
			return BadRequest(new GenericResponse(false, Strings.FormatBotNotFound(botName)));
		}

		return Ok(new GenericResponse<QrLoginResponse>(new QrLoginResponse(bot)));
	}

	[EndpointSummary("Cancels QR login of given bot")]
	[HttpDelete]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.BadRequest)]
	public async Task<ActionResult<GenericResponse>> Delete(string botName) {
		ArgumentException.ThrowIfNullOrEmpty(botName);

		Bot? bot = await ResolveBot(botName).ConfigureAwait(false);

		if (bot == null) {
			return BadRequest(new GenericResponse(false, Strings.FormatBotNotFound(botName)));
		}

		await bot.CancelQrLogin().ConfigureAwait(false);

		return Ok(new GenericResponse(true));
	}

	// A bot might be registered asynchronously (config watcher) right after its config file was created, allow a short grace period for that
	private static async Task<Bot?> ResolveBot(string botName) {
		for (byte i = 0; i < 10; i++) {
			if (Bot.Bots?.TryGetValue(botName, out Bot? bot) == true) {
				return bot;
			}

			await Task.Delay(500).ConfigureAwait(false);
		}

		return null;
	}
}
