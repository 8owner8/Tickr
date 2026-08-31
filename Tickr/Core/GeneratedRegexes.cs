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

using System.Text.RegularExpressions;

namespace Tickr.Core;

internal static partial class GeneratedRegexes {
	private const RegexOptions DefaultOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

	[GeneratedRegex(@"^[0-9A-Z]{4,7}-[0-9A-Z]{4,7}-[0-9A-Z]{4,7}(?:(?:-[0-9A-Z]{4,7})?(?:-[0-9A-Z]{4,7}))?$", DefaultOptions)]
	internal static partial Regex CdKey();

	[GeneratedRegex(@"[0-9\.,]+", DefaultOptions)]
	internal static partial Regex Decimal();

	[GeneratedRegex(@"\d+", DefaultOptions)]
	internal static partial Regex Digits();

	[GeneratedRegex(@"EResult (?<EResult>\d+)$", DefaultOptions)]
	internal static partial Regex InventoryEResult();

	[GeneratedRegex(@"[^\u0000-\u007F]+", DefaultOptions)]
	internal static partial Regex NonAscii();
}
