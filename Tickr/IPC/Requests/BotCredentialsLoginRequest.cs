using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Tickr.IPC.Requests;

[PublicAPI]
public sealed class BotCredentialsLoginRequest {
	[Required]
	public string SteamLogin { get; init; } = string.Empty;

	[Required]
	public string SteamPassword { get; init; } = string.Empty;
}
