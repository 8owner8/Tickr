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
using System.Composition;
using System.Runtime;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tickr.Core;
using Tickr.Plugins.Interfaces;
using JetBrains.Annotations;

namespace Tickr.CustomPlugins.PeriodicGC;

[Export(typeof(IPlugin))]
[UsedImplicitly]
internal sealed class PeriodicGCPlugin : IPlugin {
	private const byte GCPeriod = 60; // In seconds

	private static readonly Lock Lock = new();
	private static readonly Timer PeriodicGCTimer = new(PerformGC);

	[JsonInclude]
	public string Name => nameof(PeriodicGCPlugin);

	[JsonInclude]
	public Version Version => typeof(PeriodicGCPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public Task OnLoaded() {
		TimeSpan timeSpan = TimeSpan.FromSeconds(GCPeriod);

		TickrApp.TickrLogger.LogGenericWarning($"Periodic GC will occur every {timeSpan.ToHumanReadable()}. Please keep in mind that this plugin should be used for debugging tests only.");

		lock (Lock) {
			PeriodicGCTimer.Change(timeSpan, timeSpan);
		}

		return Task.CompletedTask;
	}

	private static void PerformGC(object? state = null) {
		TickrApp.TickrLogger.LogGenericWarning($"Performing GC, current memory: {GC.GetTotalMemory(false) / 1024} KB.");

		lock (Lock) {
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
		}

		TickrApp.TickrLogger.LogGenericWarning($"GC finished, current memory: {GC.GetTotalMemory(false) / 1024} KB.");
	}
}
