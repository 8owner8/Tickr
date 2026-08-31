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
using System.Net;
using System.Threading.Tasks;
using Tickr.IPC.Requests;
using Tickr.IPC.Responses;
using Tickr.Localization;
using Tickr.Plugins;
using Tickr.Plugins.Interfaces;
using Tickr.Steam.Interaction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tickr.IPC.Controllers.Api;

[Route("Api/Plugins")]
public sealed class PluginsController : TickrController {
	[EndpointSummary("Gets active plugins loaded into the process")]
	[HttpGet]
	[ProducesResponseType<GenericResponse<IReadOnlyCollection<IPlugin>>>((int) HttpStatusCode.OK)]
	public ActionResult<GenericResponse<IReadOnlyCollection<IPlugin>>> PluginsGet([FromQuery] bool official = true, [FromQuery] bool custom = true) {
		HashSet<IPlugin> result = [];

		foreach (IPlugin plugin in PluginsCore.ActivePlugins) {
			if (plugin is OfficialPlugin) {
				if (official) {
					result.Add(plugin);
				}
			} else {
				if (custom) {
					result.Add(plugin);
				}
			}
		}

		return Ok(new GenericResponse<IReadOnlyCollection<IPlugin>>(result));
	}

	[EndpointSummary("Makes Tickr update selected plugins")]
	[HttpPost("Update")]
	[ProducesResponseType<GenericResponse<string>>((int) HttpStatusCode.OK)]
	public async Task<ActionResult<GenericResponse<string>>> UpdatePost([FromBody] PluginUpdateRequest request) {
		ArgumentNullException.ThrowIfNull(request);

		if (request.Channel.HasValue && !Enum.IsDefined(request.Channel.Value)) {
			return BadRequest(new GenericResponse(false, Strings.FormatErrorIsInvalid(nameof(request.Channel))));
		}

		(bool success, string? message) = await Actions.UpdatePlugins(request.Channel, request.Plugins, request.Forced).ConfigureAwait(false);

		if (string.IsNullOrEmpty(message)) {
			message = success ? Strings.Success : Strings.WarningFailed;
		}

		return Ok(new GenericResponse<string>(success, message));
	}
}
