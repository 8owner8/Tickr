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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tickr.IPC.Responses;
using Tickr.Localization;
using Tickr.Web;
using Tickr.Web.GitHub;
using Tickr.Web.GitHub.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tickr.IPC.Controllers.Api;

[Route("Api/WWW/GitHub")]
public sealed class GitHubController : TickrController {
	[EndpointDescription("This is internal API being utilizied by our Tickr-ui IPC frontend. You should not depend on existence of any /Api/WWW endpoints as they can disappear and change anytime")]
	[EndpointSummary("Fetches the most recent GitHub release of Tickr project")]
	[HttpGet("Release")]
	[ProducesResponseType<GenericResponse<GitHubReleaseResponse>>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.ServiceUnavailable)]
	public async Task<ActionResult<GenericResponse>> GitHubReleaseGet() {
		CancellationToken cancellationToken = HttpContext.RequestAborted;

		ReleaseResponse? releaseResponse = await GitHubService.GetLatestRelease(SharedInfo.GithubRepo, false, cancellationToken).ConfigureAwait(false);

		return releaseResponse != null ? Ok(new GenericResponse<GitHubReleaseResponse>(new GitHubReleaseResponse(releaseResponse))) : StatusCode((int) HttpStatusCode.ServiceUnavailable, new GenericResponse(false, Strings.FormatErrorRequestFailedTooManyTimes(WebBrowser.MaxTries)));
	}

	[EndpointDescription("This is internal API being utilizied by our Tickr-ui IPC frontend. You should not depend on existence of any /Api/WWW endpoints as they can disappear and change anytime")]
	[EndpointSummary("Fetches specific GitHub release of Tickr project. Use \"latest\" for latest stable release")]
	[HttpGet("Release/{version:required}")]
	[ProducesResponseType<GenericResponse<GitHubReleaseResponse>>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.BadRequest)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.ServiceUnavailable)]
	public async Task<ActionResult<GenericResponse>> GitHubReleaseGet(string version) {
		ArgumentException.ThrowIfNullOrEmpty(version);

		CancellationToken cancellationToken = HttpContext.RequestAborted;

		ReleaseResponse? releaseResponse;

		switch (version.ToUpperInvariant()) {
			case "LATEST":
				releaseResponse = await GitHubService.GetLatestRelease(SharedInfo.GithubRepo, cancellationToken: cancellationToken).ConfigureAwait(false);

				break;
			default:
				if (!Version.TryParse(version, out Version? parsedVersion)) {
					return BadRequest(new GenericResponse(false, Strings.FormatErrorIsInvalid(nameof(version))));
				}

				releaseResponse = await GitHubService.GetRelease(SharedInfo.GithubRepo, parsedVersion.ToString(4), cancellationToken).ConfigureAwait(false);

				break;
		}

		return releaseResponse != null ? Ok(new GenericResponse<GitHubReleaseResponse>(new GitHubReleaseResponse(releaseResponse))) : StatusCode((int) HttpStatusCode.ServiceUnavailable, new GenericResponse(false, Strings.FormatErrorRequestFailedTooManyTimes(WebBrowser.MaxTries)));
	}

	[EndpointDescription("This is internal API being utilizied by our Tickr-ui IPC frontend. You should not depend on existence of any /Api/WWW endpoints as they can disappear and change anytime")]
	[EndpointSummary("Fetches history of specific GitHub page from Tickr project")]
	[HttpGet("Wiki/History/{page:required}")]
	[ProducesResponseType<GenericResponse<string>>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.BadRequest)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.ServiceUnavailable)]
	public async Task<ActionResult<GenericResponse>> GitHubWikiHistoryGet(string page) {
		ArgumentException.ThrowIfNullOrEmpty(page);

		CancellationToken cancellationToken = HttpContext.RequestAborted;

		Dictionary<string, DateTime>? revisions = await GitHubService.GetWikiHistory(page, cancellationToken).ConfigureAwait(false);

		return revisions != null ? revisions.Count > 0 ? Ok(new GenericResponse<ImmutableDictionary<string, DateTime>>(revisions.ToImmutableDictionary(revisions.Comparer))) : BadRequest(new GenericResponse(false, Strings.FormatErrorIsInvalid(nameof(page)))) : StatusCode((int) HttpStatusCode.ServiceUnavailable, new GenericResponse(false, Strings.FormatErrorRequestFailedTooManyTimes(WebBrowser.MaxTries)));
	}

	[EndpointDescription("This is internal API being utilizied by our Tickr-ui IPC frontend. You should not depend on existence of any /Api/WWW endpoints as they can disappear and change anytime. Specifying revision is optional - when not specified, will fetch latest available. If specified revision is invalid, GitHub will automatically fetch the latest revision as well")]
	[EndpointSummary("Fetches specific GitHub page of Tickr project")]
	[HttpGet("Wiki/Page/{page:required}")]
	[ProducesResponseType<GenericResponse<string>>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.BadRequest)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.ServiceUnavailable)]
	public async Task<ActionResult<GenericResponse>> GitHubWikiPageGet(string page, [FromQuery] string? revision = null) {
		ArgumentException.ThrowIfNullOrEmpty(page);

		CancellationToken cancellationToken = HttpContext.RequestAborted;

		string? html = await GitHubService.GetWikiPage(page, revision, cancellationToken).ConfigureAwait(false);

		return html switch {
			null => StatusCode((int) HttpStatusCode.ServiceUnavailable, new GenericResponse(false, Strings.FormatErrorRequestFailedTooManyTimes(WebBrowser.MaxTries))),
			"" => BadRequest(new GenericResponse(false, Strings.FormatErrorIsInvalid(nameof(page)))),
			_ => Ok(new GenericResponse<string>(html))
		};
	}
}
