using System;
using System.Collections.Generic;
using System.Linq;
using Tickr.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tickr.Tests;

#pragma warning disable CA1812
[TestClass]
internal sealed class SteamOpenIdVerifierTests {
	private static readonly Uri ExpectedReturnUri = new("http://127.0.0.1:1242/auth/openid/callback?state=0123456789ABCDEF");

	[TestMethod]
	internal void BuildLoginUriUsesOfficialSteamEndpointAndLocalReturnUri() {
		Uri uri = SteamOpenIdVerifier.BuildLoginUri(ExpectedReturnUri, new Uri("http://127.0.0.1:1242/"));

		Assert.AreEqual(Uri.UriSchemeHttps, uri.Scheme);
		Assert.AreEqual("steamcommunity.com", uri.Host);
		Assert.AreEqual("/openid/login", uri.AbsolutePath);
		StringAssert.Contains(uri.Query, Uri.EscapeDataString(ExpectedReturnUri.AbsoluteUri), StringComparison.Ordinal);
	}

	[TestMethod]
	internal void ValidCallbackIsPreparedForDirectSteamVerification() {
		Uri callback = BuildCallback("https://steamcommunity.com/openid/id/76561198000000000", ExpectedReturnUri.AbsoluteUri);

		bool valid = SteamOpenIdVerifier.TryValidateCallback(callback, ExpectedReturnUri, out ulong steamID, out IReadOnlyList<KeyValuePair<string, string>> parameters);

		Assert.IsTrue(valid);
		Assert.AreEqual(76561198000000000UL, steamID);
		Assert.AreEqual("check_authentication", parameters.Single(static parameter => parameter.Key == "openid.mode").Value);
	}

	[TestMethod]
	internal void CallbackWithTamperedReturnUriIsRejected() {
		Uri callback = BuildCallback("https://steamcommunity.com/openid/id/76561198000000000", "http://attacker.invalid/callback");

		Assert.IsFalse(SteamOpenIdVerifier.TryValidateCallback(callback, ExpectedReturnUri, out _, out _));
	}

	[TestMethod]
	internal void CallbackWithNonSteamIdentityIsRejected() {
		Uri callback = BuildCallback("https://attacker.invalid/openid/id/76561198000000000", ExpectedReturnUri.AbsoluteUri);

		Assert.IsFalse(SteamOpenIdVerifier.TryValidateCallback(callback, ExpectedReturnUri, out _, out _));
	}

	private static Uri BuildCallback(string claimedID, string returnTo) {
		Dictionary<string, string> values = new(StringComparer.Ordinal) {
			["state"] = "0123456789ABCDEF",
			["openid.ns"] = "http://specs.openid.net/auth/2.0",
			["openid.mode"] = "id_res",
			["openid.op_endpoint"] = "https://steamcommunity.com/openid/login",
			["openid.return_to"] = returnTo,
			["openid.claimed_id"] = claimedID,
			["openid.identity"] = claimedID,
			["openid.response_nonce"] = "2026-08-29T12:00:00Znonce",
			["openid.assoc_handle"] = "1234567890",
			["openid.signed"] = "signed,op_endpoint,claimed_id,identity,return_to,response_nonce,assoc_handle",
			["openid.sig"] = "signature"
		};

		string query = string.Join('&', values.Select(static pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
		return new Uri($"http://127.0.0.1:1242/auth/openid/callback?{query}");
	}
}
