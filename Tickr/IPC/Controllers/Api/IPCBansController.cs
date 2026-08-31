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
using System.Linq;
using System.Net;
using Tickr.IPC.Integration;
using Tickr.IPC.Responses;
using Tickr.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tickr.IPC.Controllers.Api;

[Route("Api/IPC/Bans")]
public sealed class IPCBansController : TickrController {
	[EndpointSummary("Clears the list of all IP addresses currently blocked by Tickr's IPC module")]
	[HttpDelete]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.OK)]
	public ActionResult<GenericResponse> Delete() {
		ApiAuthenticationMiddleware.ClearFailedAuthorizations();

		return Ok(new GenericResponse(true));
	}

	[EndpointSummary("Removes an IP address from the list of addresses currently blocked by Tickr's IPC module")]
	[HttpDelete("{ipAddress:required}")]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.BadRequest)]
	public ActionResult<GenericResponse> DeleteSpecific(string ipAddress) {
		ArgumentException.ThrowIfNullOrEmpty(ipAddress);

		if (!IPAddress.TryParse(ipAddress, out IPAddress? remoteAddress)) {
			return BadRequest(new GenericResponse(false, Strings.FormatErrorIsInvalid(nameof(ipAddress))));
		}

		bool result = ApiAuthenticationMiddleware.UnbanIP(remoteAddress);

		if (!result) {
			return BadRequest(new GenericResponse(false, Strings.FormatErrorIPNotBanned(ipAddress)));
		}

		return Ok(new GenericResponse(true));
	}

	[EndpointSummary("Gets all IP addresses currently blocked by Tickr's IPC module")]
	[HttpGet]
	[ProducesResponseType<GenericResponse<IReadOnlySet<string>>>((int) HttpStatusCode.OK)]
	public ActionResult<GenericResponse<IReadOnlySet<string>>> Get() => Ok(new GenericResponse<IReadOnlySet<string>>(ApiAuthenticationMiddleware.GetCurrentlyBannedIPs().Select(static ip => ip.ToString()).ToHashSet(StringComparer.Ordinal)));
}
