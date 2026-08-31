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
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tickr.Helpers.SerializableFile;

namespace Tickr.Tests;

#pragma warning disable CA1812 // False positive, the class is used during MSTest
[TestClass]
internal sealed class SerializableFile : TestContextBase {
	[UsedImplicitly]
	public SerializableFile(TestContext testContext) : base(testContext) => ArgumentNullException.ThrowIfNull(testContext);

	[TestMethod]
	internal async Task StaticWriteTests() {
		string expectedFileContent = $"Tickr-test-{Guid.NewGuid()}";

		string tempFileName = Path.GetTempFileName();

		try {
			Assert.IsTrue(File.Exists(tempFileName));
		} finally {
			File.Delete(tempFileName);
		}

		Assert.IsFalse(File.Exists(tempFileName));

		try {
			await Write(tempFileName, expectedFileContent).ConfigureAwait(false);

			Assert.IsTrue(File.Exists(tempFileName));
			Assert.AreEqual(expectedFileContent, await File.ReadAllTextAsync(tempFileName, CancellationToken).ConfigureAwait(false));

			expectedFileContent += "-2";

			await Write(tempFileName, expectedFileContent).ConfigureAwait(false);

			Assert.IsTrue(File.Exists(tempFileName));
			Assert.AreEqual(expectedFileContent, await File.ReadAllTextAsync(tempFileName, CancellationToken).ConfigureAwait(false));
		} finally {
			File.Delete(tempFileName);
		}
	}
}
#pragma warning restore CA1812 // False positive, the class is used during MSTest
