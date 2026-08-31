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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;

namespace Tickr.IPC.Responses;

public sealed class StatusCodeResponse {
	[Description("Value indicating whether the status is permanent. If yes, retrying the request with exactly the same payload doesn't make sense due to a permanent problem (e.g. Tickr misconfiguration)")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public bool Permanent { get; private init; }

	[Description("Status code transmitted in addition to the one in HTTP spec")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public HttpStatusCode StatusCode { get; private init; }

	internal StatusCodeResponse(HttpStatusCode statusCode, bool permanent) {
		StatusCode = statusCode;
		Permanent = permanent;
	}
}
