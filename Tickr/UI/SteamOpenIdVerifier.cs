using System;
using System.Collections.Generic;

namespace Tickr.UI;

internal static class SteamOpenIdVerifier {
	private const string IdentifierSelect = "http://specs.openid.net/auth/2.0/identifier_select";
	private const string Namespace = "http://specs.openid.net/auth/2.0";
	private const string SteamIdentityPrefix = "https://steamcommunity.com/openid/id/";
	private static readonly Uri SteamLoginUri = new("https://steamcommunity.com/openid/login");

	internal static Uri BuildLoginUri(Uri returnUri, Uri realm) {
		ArgumentNullException.ThrowIfNull(returnUri);
		ArgumentNullException.ThrowIfNull(realm);

		string query = string.Join('&', new[] {
			Pair("openid.ns", Namespace),
			Pair("openid.mode", "checkid_setup"),
			Pair("openid.return_to", returnUri.AbsoluteUri),
			Pair("openid.realm", realm.AbsoluteUri),
			Pair("openid.identity", IdentifierSelect),
			Pair("openid.claimed_id", IdentifierSelect)
		});

		return new UriBuilder(SteamLoginUri) { Query = query }.Uri;
	}

	internal static bool TryValidateCallback(Uri callbackUri, Uri expectedReturnUri, out ulong steamID, out IReadOnlyList<KeyValuePair<string, string>> verificationParameters) {
		ArgumentNullException.ThrowIfNull(callbackUri);
		ArgumentNullException.ThrowIfNull(expectedReturnUri);

		steamID = 0;
		verificationParameters = [];

		Dictionary<string, string> parameters = ParseQuery(callbackUri.Query);
		if (!parameters.TryGetValue("openid.mode", out string? mode) || !string.Equals(mode, "id_res", StringComparison.Ordinal) ||
			!parameters.TryGetValue("openid.op_endpoint", out string? endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? endpointUri) || (endpointUri != SteamLoginUri) ||
			!parameters.TryGetValue("openid.return_to", out string? returnTo) || !string.Equals(returnTo, expectedReturnUri.AbsoluteUri, StringComparison.Ordinal) ||
			!parameters.TryGetValue("openid.claimed_id", out string? claimedID) || !claimedID.StartsWith(SteamIdentityPrefix, StringComparison.Ordinal) ||
			!parameters.TryGetValue("openid.identity", out string? identity) || !string.Equals(identity, claimedID, StringComparison.Ordinal) ||
			!ulong.TryParse(claimedID.AsSpan(SteamIdentityPrefix.Length), out steamID)) {
			steamID = 0;
			return false;
		}

		parameters["openid.mode"] = "check_authentication";
		verificationParameters = [.. parameters];
		return true;
	}

	private static string Pair(string name, string value) => $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";

	private static Dictionary<string, string> ParseQuery(string query) {
		Dictionary<string, string> result = new(StringComparer.Ordinal);

		foreach (string item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
			int separator = item.IndexOf('=', StringComparison.Ordinal);
			string name = separator >= 0 ? item[..separator] : item;
			string value = separator >= 0 ? item[(separator + 1)..] : string.Empty;
			result[Uri.UnescapeDataString(name.Replace('+', ' '))] = Uri.UnescapeDataString(value.Replace('+', ' '));
		}

		return result;
	}
}
