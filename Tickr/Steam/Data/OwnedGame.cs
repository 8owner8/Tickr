// ----------------------------------------------------------------------------------------------
// Copyright 2015-2026 Tickr
// Licensed under the Apache License, Version 2.0
// ----------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Tickr.Steam.Data;

public sealed class OwnedGame {
	[JsonInclude]
	[JsonRequired]
	[Required]
	public uint AppID { get; private init; }

	[JsonInclude]
	public bool HasCommunityVisibleStats { get; private init; }

	[JsonInclude]
	public string? IconHash { get; private init; }

	[JsonInclude]
	public DateTimeOffset? LastPlayedAt { get; private init; }

	[JsonInclude]
	[JsonRequired]
	[Required]
	public string Name { get; private init; }

	[Description("Total playtime reported by Steam, in minutes")]
	[JsonInclude]
	public uint PlaytimeMinutes { get; private init; }

	[Description("Playtime in the last two weeks reported by Steam, in minutes")]
	[JsonInclude]
	public uint RecentPlaytimeMinutes { get; private init; }

	internal OwnedGame(uint appID, string? name, int playtimeMinutes, int recentPlaytimeMinutes, uint lastPlayedAt, string? iconHash, bool hasCommunityVisibleStats) {
		ArgumentOutOfRangeException.ThrowIfZero(appID);

		AppID = appID;
		Name = string.IsNullOrWhiteSpace(name) ? $"App {appID}" : name;
		PlaytimeMinutes = (uint) Math.Max(playtimeMinutes, 0);
		RecentPlaytimeMinutes = (uint) Math.Max(recentPlaytimeMinutes, 0);
		LastPlayedAt = lastPlayedAt > 0 ? DateTimeOffset.FromUnixTimeSeconds(lastPlayedAt) : null;
		IconHash = !string.IsNullOrWhiteSpace(iconHash) ? iconHash : null;
		HasCommunityVisibleStats = hasCommunityVisibleStats;
	}

	[JsonConstructor]
	private OwnedGame() => Name = string.Empty;
}
