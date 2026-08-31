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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Tickr.IPC;

internal static class WebUtilities {
	internal const string BotNamesParameterDescription = "Plural argument accepting one or many bots, acts the same as [Bots] parameter in the commands - https://github.com/8owner8/Tickr/wiki/Commands#bots-argument";

	internal static string? GetUnifiedName(this Type type) {
		ArgumentNullException.ThrowIfNull(type);

		return type.GenericTypeArguments.Length == 0 ? type.FullName : $"{type.Namespace}.{type.Name}{string.Join(null, type.GenericTypeArguments.Select(static innerType => $"[{innerType.GetUnifiedName()}]"))}";
	}

	[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2057:TypeGetType", Justification = "We don't care about trimmed assemblies, as we need it to work only with the known (used) ones")]
	internal static Type? ParseType(string typeText) {
		ArgumentException.ThrowIfNullOrEmpty(typeText);

		Type? targetType = Type.GetType(typeText);

		if (targetType != null) {
			return targetType;
		}

		// We can try one more time by trying to smartly guess the assembly name from the namespace, this will work for custom libraries like SteamKit2
		int index = typeText.IndexOf('.', StringComparison.Ordinal);

		if ((index <= 0) || (index >= typeText.Length - 1)) {
			return null;
		}

		return Type.GetType($"{typeText},{typeText[..index]}");
	}
}
