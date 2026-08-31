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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tickr.Web;

internal static class WebBrowserUtilities {
	internal static async Task<StreamContent> CreateCompressedHttpContent(HttpContent content, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(content);

		// We're going to create compressed stream and copy original content to it
		MemoryStream compressionOutput = new();

		BrotliStream compressionInput = new(compressionOutput, CompressionLevel.SmallestSize, true);

		await using (compressionInput.ConfigureAwait(false)) {
			await content.CopyToAsync(compressionInput, cancellationToken).ConfigureAwait(false);
		}

		// Reset the position back to 0, so HttpClient can read it again
		compressionOutput.Position = 0;

		StreamContent result = new(compressionOutput);

		foreach ((string key, IEnumerable<string> values) in content.Headers) {
			result.Headers.Add(key, values);
		}

		// Inform the server that we're sending compressed data
		result.Headers.ContentEncoding.Add("br");

		return result;
	}
}
