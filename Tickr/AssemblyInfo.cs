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
using System.Runtime.CompilerServices;
#if TICKR_SIGNED_BUILD
using Tickr;
#endif

[assembly: CLSCompliant(false)]

#if TICKR_SIGNED_BUILD
[assembly: InternalsVisibleTo($"Tickr.Tests, PublicKey={SharedInfo.PublicKey}")]
[assembly: InternalsVisibleTo($"Tickr.CustomPlugins.SignInWithSteam, PublicKey={SharedInfo.PublicKey}")]
[assembly: InternalsVisibleTo($"Tickr.OfficialPlugins.ItemsMatcher, PublicKey={SharedInfo.PublicKey}")]
[assembly: InternalsVisibleTo($"Tickr.OfficialPlugins.MobileAuthenticator, PublicKey={SharedInfo.PublicKey}")]
[assembly: InternalsVisibleTo($"Tickr.OfficialPlugins.Monitoring, PublicKey={SharedInfo.PublicKey}")]
[assembly: InternalsVisibleTo($"Tickr.OfficialPlugins.SteamTokenDumper, PublicKey={SharedInfo.PublicKey}")]
#else
[assembly: InternalsVisibleTo("Tickr.Tests")]
[assembly: InternalsVisibleTo("Tickr.CustomPlugins.SignInWithSteam")]
[assembly: InternalsVisibleTo("Tickr.OfficialPlugins.ItemsMatcher")]
[assembly: InternalsVisibleTo("Tickr.OfficialPlugins.MobileAuthenticator")]
[assembly: InternalsVisibleTo("Tickr.OfficialPlugins.Monitoring")]
[assembly: InternalsVisibleTo("Tickr.OfficialPlugins.SteamTokenDumper")]
#endif
