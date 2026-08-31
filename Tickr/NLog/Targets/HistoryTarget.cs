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
using System.Collections.Generic;
using Tickr.Collections;
using Tickr.Core;
using JetBrains.Annotations;
using NLog;
using NLog.Targets;

namespace Tickr.NLog.Targets;

[Target("History")]
internal sealed class HistoryTarget : TargetWithLayout {
	private const byte DefaultMaxCount = 20;

	internal IEnumerable<string> ArchivedMessages => HistoryQueue;

	private readonly FixedSizeConcurrentQueue<string> HistoryQueue = [with(DefaultMaxCount)];

	// This is NLog config property, it must have public get() and set() capabilities
	[UsedImplicitly]
	public byte MaxCount {
		get => HistoryQueue.MaxCount;

		set {
			if (value == 0) {
				TickrApp.TickrLogger.LogNullError(value);

				return;
			}

			HistoryQueue.MaxCount = value;
		}
	}

	// This parameter-less constructor is intentionally public, as NLog uses it for creating targets
	// It must stay like this as we want to have our targets defined in our NLog.config
	[UsedImplicitly]
	public HistoryTarget() { }

	internal HistoryTarget(string name) : this() => Name = name;

	protected override void Write(LogEventInfo logEvent) {
		ArgumentNullException.ThrowIfNull(logEvent);

		base.Write(logEvent);

		string message = Layout.Render(logEvent);

		HistoryQueue.Enqueue(message);
		NewHistoryEntry?.Invoke(this, new NewHistoryEntryArgs(message));
	}

	internal event EventHandler<NewHistoryEntryArgs>? NewHistoryEntry;

	internal sealed class NewHistoryEntryArgs : EventArgs {
		internal readonly string Message;

		internal NewHistoryEntryArgs(string message) {
			ArgumentNullException.ThrowIfNull(message);

			Message = message;
		}
	}
}
