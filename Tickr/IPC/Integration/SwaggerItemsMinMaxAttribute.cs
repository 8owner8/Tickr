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
using System.Globalization;
using JetBrains.Annotations;
using Microsoft.OpenApi;

namespace Tickr.IPC.Integration;

[PublicAPI]
public sealed class SwaggerItemsMinMaxAttribute : CustomSwaggerAttribute {
	public uint MaximumUint {
		get => BackingMaximum.HasValue ? decimal.ToUInt32(BackingMaximum.Value) : 0;
		set => BackingMaximum = value;
	}

	public uint MinimumUint {
		get => BackingMinimum.HasValue ? decimal.ToUInt32(BackingMinimum.Value) : 0;
		set => BackingMinimum = value;
	}

	private decimal? BackingMaximum;
	private decimal? BackingMinimum;

	public override void Apply(OpenApiSchema schema) {
		ArgumentNullException.ThrowIfNull(schema);

		if (schema.Items is not OpenApiSchema items) {
			throw new InvalidOperationException(nameof(schema.Items));
		}

		if (BackingMinimum.HasValue) {
			items.Minimum = BackingMinimum.Value.ToString(CultureInfo.InvariantCulture);
		}

		if (BackingMaximum.HasValue) {
			items.Maximum = BackingMaximum.Value.ToString(CultureInfo.InvariantCulture);
		}
	}
}
