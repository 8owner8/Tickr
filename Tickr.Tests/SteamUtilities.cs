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

using Tickr.Steam.Interaction;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tickr.Steam.Integration.SteamUtilities;

namespace Tickr.Tests;

#pragma warning disable CA1812 // False positive, the class is used during MSTest
[TestClass]
internal sealed class SteamUtilities {
	[DataRow("730", EGameIdentifier.Application, EGameIdentifier.Application, 730U)]
	[DataRow("730", EGameIdentifier.Package, EGameIdentifier.Package, 730U)]
	[DataRow("app/730", EGameIdentifier.Package, EGameIdentifier.Application, 730U)]
	[DataRow("a/730", EGameIdentifier.Package, EGameIdentifier.Application, 730U)]
	[DataRow("sub/123", EGameIdentifier.Application, EGameIdentifier.Package, 123U)]
	[DataRow("s/123", EGameIdentifier.Application, EGameIdentifier.Package, 123U)]
	[DataRow("https://store.steampowered.com/app/730", EGameIdentifier.Package, EGameIdentifier.Application, 730U)]
	[DataRow("https://store.steampowered.com/sub/123", EGameIdentifier.Application, EGameIdentifier.Package, 123U)]
	[DataRow("https://store.steampowered.com/app/730/SomeGameName/", EGameIdentifier.Package, EGameIdentifier.Application, 730U)]
	[DataRow("steam://launch/730/Dialog", EGameIdentifier.Package, EGameIdentifier.Application, 730U)]
	[DataRow("steam://launch/730/", EGameIdentifier.Package, EGameIdentifier.Application, 730U)]
	[TestMethod]
	internal void TryParseGameIdentifierReturnsExpectedId(string input, EGameIdentifier defaultType, EGameIdentifier expectedType, uint expectedId) {
		bool result = TryParseGameIdentifier(input, defaultType, out EGameIdentifier? type, out uint id);

		Assert.IsTrue(result);
		Assert.AreEqual(expectedType, type);
		Assert.AreEqual(expectedId, id);
	}

	[DataRow("0", EGameIdentifier.Application)]
	[DataRow("abc", EGameIdentifier.Application)]
	[DataRow("app/0", EGameIdentifier.Package)]
	[DataRow("sub/abc", EGameIdentifier.Application)]
	[DataRow("unknown/123", EGameIdentifier.Application)]
	[DataRow("https://store.steampowered.com/bundle/123", EGameIdentifier.Application)]
	[DataRow("https://example.com/app/730", EGameIdentifier.Package)]
	[DataRow("regex/pattern", EGameIdentifier.Application)]
	[DataRow("name/Half-Life", EGameIdentifier.Application)]
	[DataRow("steam://launch/Half-Life/", EGameIdentifier.Package)]
	[TestMethod]
	internal void TryParseGameIdentifierReturnsFalseForInvalidInput(string input, EGameIdentifier defaultType) {
		bool result = TryParseGameIdentifier(input, defaultType, out EGameIdentifier? type, out uint id);

		Assert.IsFalse(result);
		Assert.IsNull(type);
		Assert.AreEqual(0U, id);
	}

	[DataRow("regex/pattern", EGameIdentifier.Application, EGameIdentifier.Regex, "pattern")]
	[DataRow("r/test.*", EGameIdentifier.Application, EGameIdentifier.Regex, "test.*")]
	[DataRow("name/Half-Life", EGameIdentifier.Application, EGameIdentifier.Name, "Half-Life")]
	[DataRow("n/Portal", EGameIdentifier.Application, EGameIdentifier.Name, "Portal")]
	[DataRow("http:CS2", EGameIdentifier.Name, EGameIdentifier.Name, "http:CS2")]
	[DataRow("steam:CS2", EGameIdentifier.Name, EGameIdentifier.Name, "steam:CS2")]
	[TestMethod]
	internal void TryParseGameIdentifierStringReturnsExpectedValue(string input, EGameIdentifier defaultType, EGameIdentifier expectedType, string expectedValue) {
		bool result = TryParseGameIdentifier(input, defaultType, out EGameIdentifier? type, out string? value);

		Assert.IsTrue(result);
		Assert.AreEqual(expectedType, type);
		Assert.AreEqual(expectedValue, value);
	}
}
#pragma warning restore CA1812 // False positive, the class is used during MSTest
