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
using Tickr.Plugins.Interfaces;

namespace Tickr.Plugins;

internal abstract class OfficialPlugin : IPlugin {
	public abstract string Name { get; }
	public abstract Version Version { get; }
	public abstract Task OnLoaded();

	// Official plugin assemblies use the same deliberately frozen assembly metadata as the core.
	// The product release version comes from version.txt and must not be used for this compatibility check.
	internal bool HasSameVersion() => Version == SharedInfo.AssemblyVersion;
}
