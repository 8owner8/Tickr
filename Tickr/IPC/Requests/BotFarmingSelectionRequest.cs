using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Tickr.IPC.Requests;

public sealed class BotFarmingSelectionRequest {
	[JsonInclude]
	[JsonRequired]
	[Required]
	[MinLength(1)]
	public HashSet<uint> AppIDs { get; private init; } = [];
}
