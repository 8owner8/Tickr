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
using Tickr.Core;

namespace Tickr.IPC.Requests;

[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
public sealed class BotInputRequest {
	[Description("Specifies the type of the input")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public TickrApp.EUserInputType Type { get; private init; }

	[Description($"Specifies the value for given input type (declared in {nameof(Type)})")]
	[JsonInclude]
	[JsonRequired]
	[Required]
	public string Value { get; private init; } = "";

	[JsonConstructor]
	private BotInputRequest() { }
}
