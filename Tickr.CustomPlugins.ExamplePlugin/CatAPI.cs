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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tickr.Web;
using Tickr.Web.Responses;

namespace Tickr.CustomPlugins.ExamplePlugin;

// This is example class that shows how you can call third-party services within your plugin
// You've always wanted from your Tickr to post cats, right? Now is your chance!
// P.S. The code is almost 1:1 copy, you can thank me later
internal static class CatAPI {
	private const string URL = "https://api.thecatapi.com";

	internal static async Task<Uri?> GetRandomCatURL(WebBrowser webBrowser, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(webBrowser);

		Uri request = new($"{URL}/v1/images/search");

		ObjectResponse<ImmutableList<MeowResponse>>? response = await webBrowser.UrlGetToJsonObject<ImmutableList<MeowResponse>>(request, cancellationToken: cancellationToken).ConfigureAwait(false);

		return response?.Content?.FirstOrDefault()?.URL;
	}
}
