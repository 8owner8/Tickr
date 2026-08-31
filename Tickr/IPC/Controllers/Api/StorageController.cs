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
using System.Text.Json;
using Tickr.Core;
using Tickr.IPC.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tickr.IPC.Controllers.Api;

[Route("Api/Storage/{key:required}")]
public sealed class StorageController : TickrController {
	[EndpointSummary("Deletes entry under specified key from Tickr's persistent KeyValue JSON storage")]
	[HttpDelete]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.OK)]
	public ActionResult<GenericResponse> StorageDelete(string key) {
		ArgumentException.ThrowIfNullOrEmpty(key);

		if (TickrApp.GlobalDatabase == null) {
			throw new InvalidOperationException(nameof(TickrApp.GlobalDatabase));
		}

		TickrApp.GlobalDatabase.DeleteFromJsonStorage(key);

		return Ok(new GenericResponse(true));
	}

	[EndpointSummary("Loads entry under specified key from Tickr's persistent KeyValue JSON storage")]
	[HttpGet]
	[ProducesResponseType<GenericResponse<JsonElement?>>((int) HttpStatusCode.OK)]
	public ActionResult<GenericResponse> StorageGet(string key) {
		ArgumentException.ThrowIfNullOrEmpty(key);

		if (TickrApp.GlobalDatabase == null) {
			throw new InvalidOperationException(nameof(TickrApp.GlobalDatabase));
		}

		JsonElement value = TickrApp.GlobalDatabase.LoadFromJsonStorage(key);

		return Ok(new GenericResponse<JsonElement?>(true, value.ValueKind != JsonValueKind.Undefined ? value : null));
	}

	[EndpointSummary("Saves entry under specified key in Tickr's persistent KeyValue JSON storage")]
	[HttpPost]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.OK)]
	public ActionResult<GenericResponse> StoragePost(string key, [FromBody] JsonElement value) {
		ArgumentException.ThrowIfNullOrEmpty(key);

		if (value.ValueKind == JsonValueKind.Undefined) {
			throw new ArgumentOutOfRangeException(nameof(value));
		}

		if (TickrApp.GlobalDatabase == null) {
			throw new InvalidOperationException(nameof(TickrApp.GlobalDatabase));
		}

		if (value.ValueKind == JsonValueKind.Null) {
			TickrApp.GlobalDatabase.DeleteFromJsonStorage(key);
		} else {
			TickrApp.GlobalDatabase.SaveToJsonStorage(key, value);
		}

		return Ok(new GenericResponse(true));
	}
}
