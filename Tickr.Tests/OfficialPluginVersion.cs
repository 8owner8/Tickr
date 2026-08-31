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
using System.Threading.Tasks;
using Tickr.Plugins;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tickr.Tests;

#pragma warning disable CA1812 // False positive, the class is used during MSTest
[TestClass]
internal sealed class OfficialPluginVersion : TestContextBase {
	[UsedImplicitly]
	public OfficialPluginVersion(TestContext testContext) : base(testContext) => ArgumentNullException.ThrowIfNull(testContext);

	[TestMethod]
	internal void UsesFrozenAssemblyVersionForCompatibility() {
		Assert.IsTrue(new TestOfficialPlugin(SharedInfo.AssemblyVersion).HasSameVersion());
		Assert.IsFalse(new TestOfficialPlugin(new Version(SharedInfo.AssemblyVersion.Major + 1, 0)).HasSameVersion());
	}

	private sealed class TestOfficialPlugin(Version version) : OfficialPlugin {
		public override string Name => nameof(TestOfficialPlugin);
		public override Version Version { get; } = version;
		public override Task OnLoaded() => Task.CompletedTask;
	}
}
#pragma warning restore CA1812 // False positive, the class is used during MSTest
