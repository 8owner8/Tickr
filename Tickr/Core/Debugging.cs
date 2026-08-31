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
using Tickr.Storage;
using SteamKit2;

namespace Tickr.Core;

internal static class Debugging {
#if DEBUG
	internal static bool IsDebugBuild => true;
#else
	internal static bool IsDebugBuild => false;
#endif

	internal static bool IsDebugConfigured => TickrApp.GlobalConfig?.Debug ?? GlobalConfig.DefaultDebug;

	internal static bool IsUserDebugging => IsDebugBuild || IsDebugConfigured;

	internal sealed class DebugListener : IDebugListener {
		public void WriteLine(string category, string msg) {
			if (string.IsNullOrEmpty(category) && string.IsNullOrEmpty(msg)) {
				throw new InvalidOperationException($"{nameof(category)} && {nameof(msg)}");
			}

			TickrApp.TickrLogger.LogGenericDebug($"{category} | {msg}");
		}
	}
}
