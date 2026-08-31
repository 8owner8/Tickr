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
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Tickr.Helpers;

namespace Tickr.IPC.Requests;

[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
public sealed class TickrHashRequest {
	[Description("Hashing method used for hashing this string")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public TickrCryptoHelper.EHashingMethod HashingMethod { get; private init; }

	[Description($"String to hash with provided {nameof(HashingMethod)}")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public string StringToHash { get; private init; } = "";

	[JsonConstructor]
	private TickrHashRequest() { }
}
