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
using System.Threading;
using System.Threading.Tasks;
using Tickr.Storage;
using JetBrains.Annotations;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tickr.IPC.OpenApi;

[UsedImplicitly]
internal sealed class OperationTransformer : IOpenApiOperationTransformer {
	public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken) {
		ArgumentNullException.ThrowIfNull(operation);
		ArgumentNullException.ThrowIfNull(context);

		if (context.Description.RelativePath?.StartsWith("Api", StringComparison.OrdinalIgnoreCase) == true) {
			operation.Security ??= new List<OpenApiSecurityRequirement>(1);

			operation.Security.Add(
				new OpenApiSecurityRequirement {
					{
						new OpenApiSecuritySchemeReference(nameof(GlobalConfig.IPCPassword), context.Document),
						[]
					}
				}
			);
		}

		return Task.CompletedTask;
	}
}
