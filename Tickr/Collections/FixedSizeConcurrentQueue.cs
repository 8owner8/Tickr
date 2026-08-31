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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Tickr.Collections;

internal sealed class FixedSizeConcurrentQueue<T> : IEnumerable<T> where T : notnull {
	private readonly ConcurrentQueue<T> BackingQueue = new();

	internal byte MaxCount {
		get;

		set {
			ArgumentOutOfRangeException.ThrowIfZero(value);

			field = value;

			Resize();
		}
	}

	internal FixedSizeConcurrentQueue(byte maxCount) {
		ArgumentOutOfRangeException.ThrowIfZero(maxCount);

		MaxCount = maxCount;
	}

	[MustDisposeResource]
	public IEnumerator<T> GetEnumerator() => BackingQueue.GetEnumerator();

	[MustDisposeResource]
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	internal void Enqueue(T obj) {
		ArgumentNullException.ThrowIfNull(obj);

		BackingQueue.Enqueue(obj);

		Resize();
	}

	private void Resize() {
		if (BackingQueue.Count <= MaxCount) {
			return;
		}

		lock (BackingQueue) {
			while ((BackingQueue.Count > MaxCount) && BackingQueue.TryDequeue(out _)) { }
		}
	}
}
