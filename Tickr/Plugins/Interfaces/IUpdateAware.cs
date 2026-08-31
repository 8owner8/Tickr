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
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to be aware of Tickr updates and execute appropriate logic that you need to happen before/after such update happens.
/// </summary>
[PublicAPI]
public interface IUpdateAware : IPlugin {
	/// <summary>
	///     Tickr will call this method after update to a particular Tickr version has been finished, just before restart of the process.
	/// </summary>
	/// <param name="currentVersion">The current (old) version of Tickr program.</param>
	/// <param name="newVersion">The target (new) version of Tickr program.</param>
	public Task OnUpdateFinished(Version currentVersion, Version newVersion);

	/// <summary>
	///     Tickr will call this method before proceeding with an update to a particular Tickr version.
	/// </summary>
	/// <param name="currentVersion">The current (old) version of Tickr program.</param>
	/// <param name="newVersion">The target (new) version of Tickr program.</param>
	public Task OnUpdateProceeding(Version currentVersion, Version newVersion);
}
