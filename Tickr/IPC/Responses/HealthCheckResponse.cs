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
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tickr.IPC.Responses;

public sealed class HealthCheckResponse {
	[Description($"{nameof(Status)} written as text")]
	[JsonInclude]
	public string StatusText => Status.ToString();

	[Description("Health status of the application")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public HealthStatus Status { get; private init; }

	internal HealthCheckResponse(HealthReport report) {
		ArgumentNullException.ThrowIfNull(report);

		Status = report.Status;
	}
}
