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
using SteamKit2;
using SteamKit2.Internal;

namespace Tickr.Steam.Integration.Callbacks;

internal sealed class SharedLibraryLockStatusCallback : CallbackMsg {
	internal readonly ulong LibraryLockedBySteamID;

	internal SharedLibraryLockStatusCallback(JobID jobID, CMsgClientSharedLibraryLockStatus msg) {
		ArgumentNullException.ThrowIfNull(jobID);
		ArgumentNullException.ThrowIfNull(msg);

		JobID = jobID;

		if (msg.own_library_locked_by == 0) {
			return;
		}

		LibraryLockedBySteamID = new SteamID(msg.own_library_locked_by, EUniverse.Public, EAccountType.Individual);
	}
}
