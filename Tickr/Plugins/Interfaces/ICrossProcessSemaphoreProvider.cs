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

using System.Threading.Tasks;
using Tickr.Helpers;
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to provide custom cross-process semaphore, which is used for synchronizing multiple Tickr instances with their limiters.
///     Custom cross-process semaphore might be useful if you wanted to extend cross-process semaphore offered by Tickr, e.g. by utilizing remote-oriented tools like redis and allowing Tickr instances over several different machines to synchronize with each other.
/// </summary>
[PublicAPI]
public interface ICrossProcessSemaphoreProvider : IPlugin {
	/// <summary>
	///     Tickr will call this method when initializing instance of <see cref="ICrossProcessSemaphore" /> for its internal limiters.
	/// </summary>
	/// <param name="resourceName">Unique resource name provided by Tickr for identification purposes.</param>
	/// <returns>Concrete implementation of <see cref="ICrossProcessSemaphore" /> providing required functionality. It's allowed to return null if you want to use Tickr's default implementation for specified resource instead.</returns>
	public Task<ICrossProcessSemaphore?> GetCrossProcessSemaphore(string resourceName);
}
