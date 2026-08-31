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
using System.Threading;
using System.Threading.Tasks;
using Tickr.Core;
using Tickr.IPC.Controllers.Api;
using Tickr.IPC.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Tickr.CustomPlugins.ExamplePlugin;

// This is an example class which shows you how you can extend Tickr's API with your own custom API routes and controllers
// You're free to decide whether you want to integrate with existing Tickr concepts (such as TickrController/GenericResponse), or roll out your own
// All API controllers will be discovered during our Kestrel initialization using attributes mapping, you're also getting usual Tickr goodies such as swagger documentation out of the box
[Route("/Api/Cat")]
public sealed class CatController : TickrController {
	/// <summary>
	///     Fetches URL of a random cat picture.
	/// </summary>
	[HttpGet]
	[ProducesResponseType<GenericResponse<Uri>>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.ServiceUnavailable)]
	public async Task<ActionResult<GenericResponse>> CatGet() {
		if (TickrApp.WebBrowser == null) {
			throw new InvalidOperationException(nameof(TickrApp.WebBrowser));
		}

		CancellationToken cancellationToken = HttpContext.RequestAborted;

		Uri? url = await CatAPI.GetRandomCatURL(TickrApp.WebBrowser, cancellationToken).ConfigureAwait(false);

		return url != null ? Ok(new GenericResponse<Uri>(url)) : StatusCode((int) HttpStatusCode.ServiceUnavailable, new GenericResponse(false));
	}
}
