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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tickr.Collections;
using Tickr.Core;
using Tickr.Helpers;
using Tickr.Helpers.Json;
using Tickr.Localization;
using Tickr.OfficialPlugins.ItemsMatcher.Data;
using JetBrains.Annotations;

namespace Tickr.OfficialPlugins.ItemsMatcher;

internal sealed class BotCache : SerializableFile {
	[JsonDisallowNull]
	[JsonInclude]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	internal ConcurrentList<AssetForListing> LastAnnouncedAssetsForListing { get; private init; } = [];

	[JsonInclude]
	[JsonPropertyName("BackingLastAnnouncedTradeToken")]
	internal string? LastAnnouncedTradeToken {
		get;

		set {
			if (field == value) {
				return;
			}

			field = value;
			Utilities.InBackground(Save);
		}
	}

	[JsonInclude]
	[JsonPropertyName("BackingLastInventoryChecksumBeforeDeduplication")]
	internal string? LastInventoryChecksumBeforeDeduplication {
		get;

		set {
			if (field == value) {
				return;
			}

			field = value;
			Utilities.InBackground(Save);
		}
	}

	[JsonInclude]
	[JsonPropertyName("BackingLastRequestAt")]
	internal DateTime? LastRequestAt {
		get;

		set {
			if (field == value) {
				return;
			}

			field = value;
			Utilities.InBackground(Save);
		}
	}

	private BotCache(string filePath) : this() {
		ArgumentException.ThrowIfNullOrEmpty(filePath);

		FilePath = filePath;
	}

	[JsonConstructor]
	private BotCache() => LastAnnouncedAssetsForListing.OnModified += OnObjectModified;

	[UsedImplicitly]
	public bool ShouldSerializeLastAnnouncedAssetsForListing() => LastAnnouncedAssetsForListing.Count > 0;

	[UsedImplicitly]
	public bool ShouldSerializeLastAnnouncedTradeToken() => !string.IsNullOrEmpty(LastAnnouncedTradeToken);

	[UsedImplicitly]
	public bool ShouldSerializeLastInventoryChecksumBeforeDeduplication() => !string.IsNullOrEmpty(LastInventoryChecksumBeforeDeduplication);

	[UsedImplicitly]
	public bool ShouldSerializeLastRequestAt() => LastRequestAt.HasValue;

	protected override void Dispose(bool disposing) {
		if (disposing) {
			// Events we registered
			LastAnnouncedAssetsForListing.OnModified -= OnObjectModified;
		}

		// Base dispose
		base.Dispose(disposing);
	}

	protected override Task Save() => Save(this);

	internal static async Task<BotCache> CreateOrLoad(string filePath) {
		ArgumentException.ThrowIfNullOrEmpty(filePath);

		if (!File.Exists(filePath)) {
			return new BotCache(filePath);
		}

		BotCache? botCache;

		try {
			string json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

			if (string.IsNullOrEmpty(json)) {
				TickrApp.TickrLogger.LogGenericError(Strings.FormatErrorIsEmpty(nameof(json)));

				return new BotCache(filePath);
			}

			botCache = json.ToJsonObject<BotCache>();
		} catch (Exception e) {
			TickrApp.TickrLogger.LogGenericException(e);

			return new BotCache(filePath);
		}

		if (botCache == null) {
			TickrApp.TickrLogger.LogNullError(botCache);

			return new BotCache(filePath);
		}

		botCache.FilePath = filePath;

		return botCache;
	}

	private async void OnObjectModified(object? sender, EventArgs e) {
		if (string.IsNullOrEmpty(FilePath)) {
			return;
		}

		await Save().ConfigureAwait(false);
	}
}
