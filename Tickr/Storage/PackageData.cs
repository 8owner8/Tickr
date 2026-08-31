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
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Tickr.Storage;

public sealed class PackageData {
	[JsonInclude]
	public ImmutableHashSet<uint>? AppIDs { get; private init; }

	[JsonInclude]
	[JsonRequired]
	public uint ChangeNumber { get; private init; }

	[JsonInclude]
	public ImmutableHashSet<string>? OnlyAllowRunInCountries { get; private init; }

	[JsonInclude]
	public ImmutableHashSet<string>? ProhibitRunInCountries { get; private init; }

	[JsonInclude]
	[JsonRequired]
	public DateTime ValidUntil { get; private init; }

	internal PackageData(uint changeNumber, DateTime validUntil, ImmutableHashSet<uint>? appIDs = null, ImmutableHashSet<string>? onlyAllowRunInCountries = null, ImmutableHashSet<string>? prohibitRunInCountries = null) {
		ArgumentOutOfRangeException.ThrowIfZero(changeNumber);
		ArgumentOutOfRangeException.ThrowIfEqual(validUntil, DateTime.MinValue);

		ChangeNumber = changeNumber;
		ValidUntil = validUntil;
		AppIDs = appIDs;
		OnlyAllowRunInCountries = onlyAllowRunInCountries;
		ProhibitRunInCountries = prohibitRunInCountries;
	}

	[JsonConstructor]
	private PackageData() { }

	[UsedImplicitly]
	public bool ShouldSerializeAppIDs() => AppIDs is { IsEmpty: false };

	[UsedImplicitly]
	public bool ShouldSerializeOnlyAllowRunInCountries() => OnlyAllowRunInCountries is { IsEmpty: false };

	[UsedImplicitly]
	public bool ShouldSerializeProhibitRunInCountries() => ProhibitRunInCountries is { IsEmpty: false };
}
