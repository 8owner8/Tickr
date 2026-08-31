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

using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to provide your own additional services and middlewares for IPC endpoints - Use with caution!
/// </summary>
[PublicAPI]
public interface IWebServiceProvider : IPlugin {
	/// <summary>
	///     Tickr will call this method during configuration of the IPC endpoints.
	/// </summary>
	/// <param name="app">Application builder related to this callback.</param>
	public void OnConfiguringEndpoints(IApplicationBuilder app);

	/// <summary>
	///     Tickr will call this method during configuration of the IPC services.
	/// </summary>
	/// <param name="services">Service collection related to this callback.</param>
	public void OnConfiguringServices(IServiceCollection services);
}
