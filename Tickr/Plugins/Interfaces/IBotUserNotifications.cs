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

using System.Collections.Generic;
using System.Threading.Tasks;
using Tickr.Steam;
using Tickr.Steam.Integration.Callbacks;
using JetBrains.Annotations;

namespace Tickr.Plugins.Interfaces;

/// <inheritdoc />
/// <summary>
///     Implementing this interface allows you to receive Steam notifications transmitted over the network.
/// </summary>
[PublicAPI]
public interface IBotUserNotifications : IPlugin {
	/// <summary>
	///     Tickr will call this method when number of notifications for one or more notification types changes.
	/// </summary>
	/// <param name="bot">Bot object related to this callback.</param>
	/// <param name="newNotifications">Collection containing those notification types that are new (that is, when new count > previous count of that notification type).</param>
	public Task OnBotUserNotifications(Bot bot, IReadOnlyCollection<UserNotificationsCallback.EUserNotification> newNotifications);
}
