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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Tickr.Collections;
using Tickr.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tickr.Tests;

#pragma warning disable CA1812 // False positive, the class is used during MSTest
[TestClass]
internal sealed class ConcurrentTests {
	[TestMethod]
	internal void ConcurrentDictionarySupportsWritingDuringLinq() {
		ConcurrentDictionary<ushort, bool> collection = [];

		for (byte i = 0; i < 10; i++) {
			collection.TryAdd(i, true);
		}

		Utilities.InBackground(
			() => {
				for (ushort i = 10; i < ushort.MaxValue; i++) {
					collection.TryAdd(i, true);
				}
			}, true
		);

		foreach (KeyValuePair<ushort, bool> _ in collection.AsLinqThreadSafeEnumerable().OrderBy(static entry => entry.Key)) { }
	}

	[TestMethod]
	internal void ConcurrentHashSetSupportsWritingDuringLinq() {
		ConcurrentHashSet<ushort> collection = [];

		for (byte i = 0; i < 10; i++) {
			collection.Add(i);
		}

		Utilities.InBackground(
			() => {
				for (ushort i = 10; i < ushort.MaxValue; i++) {
					collection.Add(i);
				}
			}, true
		);

		foreach (ushort _ in collection.AsLinqThreadSafeEnumerable().OrderBy(static entry => entry)) { }
	}

	[TestMethod]
	internal void ConcurrentListSupportsWritingDuringLinq() {
		ConcurrentList<ushort> collection = [];

		for (byte i = 0; i < 10; i++) {
			collection.Add(i);
		}

		Utilities.InBackground(
			() => {
				for (ushort i = 10; i < ushort.MaxValue; i++) {
					collection.Add(i);
				}
			}, true
		);

		foreach (ushort _ in collection.AsLinqThreadSafeEnumerable().OrderBy(static entry => entry)) { }
	}
}
#pragma warning restore CA1812 // False positive, the class is used during MSTest
