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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tickr.Events;

[PublicAPI]
public interface ITickrEventBus {
	void Publish<TEvent>(TEvent tickrEvent) where TEvent : class, ITickrEvent;
	Task PublishAsync<TEvent>(TEvent tickrEvent, CancellationToken cancellationToken = default) where TEvent : class, ITickrEvent;
	IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, ITickrEvent;
	IDisposable SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> asyncHandler) where TEvent : class, ITickrEvent;
}

[PublicAPI]
public sealed class TickrEventBus : ITickrEventBus {
	public static readonly TickrEventBus Instance = new();

	private readonly ConcurrentDictionary<Type, List<Delegate>> Handlers = new();
	private readonly Lock SyncLock = new();

	public void Publish<TEvent>(TEvent tickrEvent) where TEvent : class, ITickrEvent {
		ArgumentNullException.ThrowIfNull(tickrEvent);

		if (!Handlers.TryGetValue(typeof(TEvent), out List<Delegate>? list)) {
			return;
		}

		Delegate[] snapshot;
		lock (SyncLock) {
			snapshot = list.ToArray();
		}

		foreach (Delegate handler in snapshot) {
			try {
				if (handler is Action<TEvent> syncAction) {
					syncAction(tickrEvent);
				} else if (handler is Func<TEvent, CancellationToken, Task> asyncFunc) {
					_ = asyncFunc(tickrEvent, CancellationToken.None);
				}
			} catch {
				// Handlers should not break event bus
			}
		}
	}

	public async Task PublishAsync<TEvent>(TEvent tickrEvent, CancellationToken cancellationToken = default) where TEvent : class, ITickrEvent {
		ArgumentNullException.ThrowIfNull(tickrEvent);

		if (!Handlers.TryGetValue(typeof(TEvent), out List<Delegate>? list)) {
			return;
		}

		Delegate[] snapshot;
		lock (SyncLock) {
			snapshot = list.ToArray();
		}

		foreach (Delegate handler in snapshot) {
			if (cancellationToken.IsCancellationRequested) {
				break;
			}

			try {
				if (handler is Action<TEvent> syncAction) {
					syncAction(tickrEvent);
				} else if (handler is Func<TEvent, CancellationToken, Task> asyncFunc) {
					await asyncFunc(tickrEvent, cancellationToken).ConfigureAwait(false);
				}
			} catch {
				// Handlers should not break event bus
			}
		}
	}

	public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, ITickrEvent {
		ArgumentNullException.ThrowIfNull(handler);

		List<Delegate> list = Handlers.GetOrAdd(typeof(TEvent), static _ => []);
		lock (SyncLock) {
			list.Add(handler);
		}

		return new Unsubscriber(() => {
			lock (SyncLock) {
				list.Remove(handler);
			}
		});
	}

	public IDisposable SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> asyncHandler) where TEvent : class, ITickrEvent {
		ArgumentNullException.ThrowIfNull(asyncHandler);

		List<Delegate> list = Handlers.GetOrAdd(typeof(TEvent), static _ => []);
		lock (SyncLock) {
			list.Add(asyncHandler);
		}

		return new Unsubscriber(() => {
			lock (SyncLock) {
				list.Remove(asyncHandler);
			}
		});
	}

	private sealed class Unsubscriber(Action unsubscribeAction) : IDisposable {
		private Action? UnsubscribeAction = unsubscribeAction;

		public void Dispose() {
			Action? action = Interlocked.Exchange(ref UnsubscribeAction, null);
			action?.Invoke();
		}
	}
}