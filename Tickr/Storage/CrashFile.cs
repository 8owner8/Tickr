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
using Tickr.Core;
using Tickr.Helpers;
using Tickr.Helpers.Json;
using Tickr.Localization;
using JetBrains.Annotations;

namespace Tickr.Storage;

internal sealed class CrashFile : SerializableFile {
	[JsonInclude]
	[JsonPropertyName("BackingLastStartup")]
	internal DateTime LastStartup {
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
	[JsonPropertyName("BackingStartupCount")]
	internal byte StartupCount {
		get;

		set {
			if (field == value) {
				return;
			}

			field = value;
			Utilities.InBackground(Save);
		}
	}

	private CrashFile(string filePath) : this() {
		ArgumentException.ThrowIfNullOrEmpty(filePath);

		FilePath = filePath;
	}

	[JsonConstructor]
	private CrashFile() { }

	[UsedImplicitly]
	public bool ShouldSerializeLastStartup() => LastStartup > DateTime.MinValue;

	[UsedImplicitly]
	public bool ShouldSerializeStartupCount() => StartupCount > 0;

	protected override Task Save() => Save(this);

	internal static async Task<CrashFile> CreateOrLoad(string filePath) {
		ArgumentException.ThrowIfNullOrEmpty(filePath);

		if (!File.Exists(filePath)) {
			return new CrashFile(filePath);
		}

		CrashFile? crashFile;

		try {
			string json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

			if (string.IsNullOrEmpty(json)) {
				TickrApp.TickrLogger.LogGenericError(Strings.FormatErrorIsEmpty(nameof(json)));

				return new CrashFile(filePath);
			}

			crashFile = json.ToJsonObject<CrashFile>();
		} catch (Exception e) {
			TickrApp.TickrLogger.LogGenericException(e);

			return new CrashFile(filePath);
		}

		if (crashFile == null) {
			TickrApp.TickrLogger.LogNullError(crashFile);

			return new CrashFile(filePath);
		}

		crashFile.FilePath = filePath;

		return crashFile;
	}
}
