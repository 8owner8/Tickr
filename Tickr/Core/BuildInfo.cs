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

namespace Tickr.Core;

internal static class BuildInfo {
#if TICKR_VARIANT_DOCKER
	internal static bool CanUpdate => false;
	internal static string Variant => "docker";
#elif TICKR_VARIANT_GENERIC
	internal static bool CanUpdate => true;
	internal static string Variant => "generic";
#elif TICKR_VARIANT_LINUX_ARM
	internal static bool CanUpdate => true;
	internal static string Variant => "linux-arm";
#elif TICKR_VARIANT_LINUX_ARM64
	internal static bool CanUpdate => true;
	internal static string Variant => "linux-arm64";
#elif TICKR_VARIANT_LINUX_X64
	internal static bool CanUpdate => true;
	internal static string Variant => "linux-x64";
#elif TICKR_VARIANT_OSX_ARM64
	internal static bool CanUpdate => true;
	internal static string Variant => "osx-arm64";
#elif TICKR_VARIANT_OSX_X64
	internal static bool CanUpdate => true;
	internal static string Variant => "osx-x64";
#elif TICKR_VARIANT_WIN_ARM64
	internal static bool CanUpdate => true;
	internal static string Variant => "win-arm64";
#elif TICKR_VARIANT_WIN_X64
	internal static bool CanUpdate => true;
	internal static string Variant => "win-x64";
#else
	internal static bool CanUpdate => false;
	internal static string Variant => SourceVariant;
#endif

#if TICKR_RUNTIME_TRIMMED
	internal static bool IsRuntimeTrimmed => true;
#else
	internal static bool IsRuntimeTrimmed => false;
#endif

	private const string SourceVariant = "source";

	internal static bool IsCustomBuild => Variant == SourceVariant;
}
