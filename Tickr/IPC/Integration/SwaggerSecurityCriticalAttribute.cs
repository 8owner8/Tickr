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
using System.Text.Json.Nodes;
using JetBrains.Annotations;
using Microsoft.OpenApi;

namespace Tickr.IPC.Integration;

[PublicAPI]
public sealed class SwaggerSecurityCriticalAttribute : CustomSwaggerAttribute {
	private const string ExtensionName = "x-security-critical";

	public override void Apply(OpenApiSchema schema) {
		ArgumentNullException.ThrowIfNull(schema);

		JsonValue value = JsonValue.Create(true);

		if (schema.Items != null) {
			if (schema.Items is OpenApiSchema items) {
				items.AddExtension(ExtensionName, new JsonNodeExtension(value));
			} else if (schema.Items.Extensions != null) {
				schema.Items.Extensions[ExtensionName] = new JsonNodeExtension(value);
			} else {
				throw new InvalidOperationException(nameof(schema.Items));
			}

			return;
		}

		schema.AddExtension(ExtensionName, new JsonNodeExtension(value));
	}
}
