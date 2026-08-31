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
using Tickr.Core;
using Tickr.Localization;
using JetBrains.Annotations;
using SteamKit2;
using SteamKit2.Internal;

namespace Tickr.Steam.Integration.Callbacks;

public sealed class UserNotificationsCallback : CallbackMsg {
	internal readonly Dictionary<EUserNotification, uint> Notifications;

	internal UserNotificationsCallback(JobID jobID, CMsgClientUserNotifications msg) {
		ArgumentNullException.ThrowIfNull(jobID);
		ArgumentNullException.ThrowIfNull(msg);

		JobID = jobID;

		// We might get null body here, and that means there are no notifications related to trading
		// TODO: Check if this workaround is still needed
		Notifications = new Dictionary<EUserNotification, uint> { { EUserNotification.Trading, 0 } };

		if (msg.notifications == null) {
			return;
		}

		foreach (CMsgClientUserNotifications.Notification notification in msg.notifications) {
			EUserNotification type = (EUserNotification) notification.user_notification_type;

			switch (type) {
				case EUserNotification.AccountAlerts:
				case EUserNotification.Chat:
				case EUserNotification.Comments:
				case EUserNotification.GameTurns:
				case EUserNotification.Gifts:
				case EUserNotification.HelpRequestReplies:
				case EUserNotification.Invites:
				case EUserNotification.Items:
				case EUserNotification.ModeratorMessages:
				case EUserNotification.Trading:
					break;
				default:
					TickrApp.TickrLogger.LogGenericError(Strings.FormatWarningUnknownValuePleaseReport(nameof(type), type));

					break;
			}

			Notifications[type] = notification.count;
		}
	}

	internal UserNotificationsCallback(JobID jobID, CMsgClientItemAnnouncements msg) {
		ArgumentNullException.ThrowIfNull(jobID);
		ArgumentNullException.ThrowIfNull(msg);

		JobID = jobID;
		Notifications = new Dictionary<EUserNotification, uint>(1) { { EUserNotification.Items, msg.count_new_items } };
	}

	internal UserNotificationsCallback(JobID jobID, CMsgClientCommentNotifications msg) {
		ArgumentNullException.ThrowIfNull(jobID);
		ArgumentNullException.ThrowIfNull(msg);

		JobID = jobID;
		Notifications = new Dictionary<EUserNotification, uint>(1) { { EUserNotification.Comments, msg.count_new_comments + msg.count_new_comments_owner + msg.count_new_comments_subscriptions } };
	}

	[PublicAPI]
	public enum EUserNotification : byte {
		Unknown,
		Trading,
		GameTurns,
		ModeratorMessages,
		Comments,
		Items,
		Invites,
		Unknown7, // Unknown type of notification, never seen in the wild
		Gifts,
		Chat,
		HelpRequestReplies,
		AccountAlerts
	}
}
