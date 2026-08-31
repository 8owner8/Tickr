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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Tickr.IPC.Responses;
using Tickr.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tickr.IPC.Controllers.Api;

[Route("Api/Structure")]
public sealed class StructureController : TickrController {
	[EndpointDescription("Structure is defined as a representation of given object in its default state")]
	[EndpointSummary("Fetches structure of given type")]
	[HttpGet("{structure:required}")]
	[ProducesResponseType<GenericResponse<object>>((int) HttpStatusCode.OK)]
	[ProducesResponseType<GenericResponse>((int) HttpStatusCode.BadRequest)]
	[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2072", Justification = "We don't care about trimmed assemblies, as we need it to work only with the known (used) ones")]
	public ActionResult<GenericResponse> StructureGet(string structure) {
		ArgumentException.ThrowIfNullOrEmpty(structure);

		Type? targetType = WebUtilities.ParseType(structure);

		if (targetType == null) {
			return BadRequest(new GenericResponse(false, Strings.FormatErrorIsInvalid(structure)));
		}

		object? obj;

		try {
			obj = Activator.CreateInstance(targetType, true);
		} catch (Exception e) {
			return BadRequest(new GenericResponse(false, $"{Strings.FormatErrorParsingObject(nameof(targetType))}{Environment.NewLine}{e}"));
		}

		return Ok(new GenericResponse<object>(obj));
	}
}
