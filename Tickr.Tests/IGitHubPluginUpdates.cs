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
using Tickr.Core;
using Tickr.Localization;
using Tickr.NLog;
using Tickr.Storage;
using Tickr.Web;
using Tickr.Web.GitHub;
using Tickr.Web.GitHub.Data;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tickr.Tests;

#pragma warning disable CA1812 // False positive, the class is used during MSTest
[TestClass]
internal sealed class IGitHubPluginUpdates : TestContextBase {
	private const string PluginName = "Tickr.OfficialPlugins.Monitoring";
	private const string Repository = "8owner8/Tickr";

	[UsedImplicitly]
	public IGitHubPluginUpdates(TestContext testContext) : base(testContext) => ArgumentNullException.ThrowIfNull(testContext);

	// Targets upstream ASF's plugin distribution model (per-plugin repos with -V*.zip variant assets).
	// Tickr bundles official plugins inside the data-v* prerelease instead, so this scenario does not apply.
	[TestCategory("Manual")]
	[TestMethod]
	[Ignore("Upstream ASF plugin-update scenario; Tickr distributes plugins bundled in the data release")]
	internal async Task DoesNotOfferPointlessUpdatesWhenMultipleAssetsAreFound() {
		using WebBrowser webBrowser = new(new TickrLogger("Test"));

		typeof(TickrApp).GetProperty(nameof(TickrApp.WebBrowser))?.SetValue(null, webBrowser);

		ReleaseResponse? response = await GitHubService.GetLatestRelease(Repository, cancellationToken: CancellationToken).ConfigureAwait(false);

		if (response == null) {
			Assert.Inconclusive(Strings.FormatWarningFailedWithError(nameof(response)));
		}

		Version version = Version.Parse(response.Tag.TrimStart('v', 'V'));

		Plugins.Interfaces.IGitHubPluginUpdates plugin = new TestGitHubPluginUpdates(version);

		Uri? releaseURL = await plugin.GetTargetReleaseURL(version, BuildInfo.Variant, true, GlobalConfig.EUpdateChannel.Stable, false).ConfigureAwait(false);

		Assert.IsNull(releaseURL);

		Uri? forcedReleaseURL = await plugin.GetTargetReleaseURL(version, BuildInfo.Variant, true, GlobalConfig.EUpdateChannel.Stable, true).ConfigureAwait(false);

		Assert.IsNotNull(forcedReleaseURL);
	}

	private sealed class TestGitHubPluginUpdates : Plugins.Interfaces.IGitHubPluginUpdates {
		public string Name => PluginName;
		public string RepositoryName => Repository;
		public Version Version { get; }

		internal TestGitHubPluginUpdates(Version version) {
			ArgumentNullException.ThrowIfNull(version);

			Version = version;
		}

		public Task OnLoaded() => Task.CompletedTask;
	}
}
#pragma warning restore CA1812 // False positive, the class is used during MSTest
