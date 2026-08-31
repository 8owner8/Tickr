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
using System.Collections.Generic;

namespace Tickr.Collections;

internal sealed class ConcurrentEnumerator<T> : IEnumerator<T> {
	public T Current => Enumerator.Current;

	private readonly IEnumerator<T> Enumerator;
	private readonly IDisposable LockObject;

	object? IEnumerator.Current => Current;

	internal ConcurrentEnumerator(IReadOnlyCollection<T> collection, IDisposable lockObject) {
		ArgumentNullException.ThrowIfNull(collection);
		ArgumentNullException.ThrowIfNull(lockObject);

		Enumerator = collection.GetEnumerator();
		LockObject = lockObject;
	}

	public void Dispose() {
		Enumerator.Dispose();
		LockObject.Dispose();
	}

	public bool MoveNext() => Enumerator.MoveNext();
	public void Reset() => Enumerator.Reset();
}
