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

using System.Net;
using Tickr.IPC.Controllers.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tickr.OfficialPlugins.SteamTokenDumper;

[Route("Api/SteamTokenDumperPlugin")]
public sealed class SteamTokenDumperController : TickrController {
	[HttpGet(nameof(GlobalConfigExtension))]
	[ProducesResponseType<GlobalConfigExtension>((int) HttpStatusCode.OK)]
	[Tags(nameof(GlobalConfigExtension))]
	public ActionResult<GlobalConfigExtension> Get() => Ok(new GlobalConfigExtension());
}
