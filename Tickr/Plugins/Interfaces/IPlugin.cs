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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <summary>
///     Implementing this interface allows you to register your plugin in Tickr, in turn providing you a way to implement your own custom logic.
/// </summary>
[PublicAPI]
public interface IPlugin {
	/// <summary>
	///     Tickr will use this property as general plugin identifier for the user.
	/// </summary>
	/// <returns>String that will be used as the name of this plugin.</returns>
	[JsonInclude]
	public string Name { get; }

	/// <summary>
	///     Tickr will use this property as version indicator of your plugin to the user.
	///     You have a freedom in deciding what versioning you want to use, this is for identification purposes only.
	/// </summary>
	/// <returns>Version that will be shown to the user when plugin is loaded.</returns>
	[JsonInclude]
	public Version Version { get; }

	/// <summary>
	///     Tickr will call this method right after plugin initialization.
	/// </summary>
	public Task OnLoaded();
}
