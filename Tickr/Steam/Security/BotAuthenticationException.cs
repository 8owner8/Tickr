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
using System.ComponentModel;
using SteamKit2;

namespace Tickr.Steam.Security;

#pragma warning disable CA1032 // This type is internal and we don't require additional constructors
internal sealed class BotAuthenticationException : Exception {
	internal readonly EResult Result;

	internal BotAuthenticationException(EResult result) {
		if (!Enum.IsDefined(result)) {
			throw new InvalidEnumArgumentException(nameof(result), (int) result, typeof(EResult));
		}

		Result = result;
	}
}
#pragma warning restore CA1032 // This type is internal and we don't require additional constructors
