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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tickr.Steam.Integration.SteamChatMessage;

namespace Tickr.Tests;

#pragma warning disable CA1812 // False positive, the class is used during MSTest
[TestClass]
internal sealed class SteamChatMessage : TestContextBase {
	[UsedImplicitly]
	public SteamChatMessage(TestContext testContext) : base(testContext) => ArgumentNullException.ThrowIfNull(testContext);

	[TestMethod]
	internal async Task CanSplitEvenWithStupidlyLongPrefix() {
		string prefix = new('x', MaxMessagePrefixBytes);

		const string emoji = "😎";
		const string message = $"{emoji}{emoji}{emoji}{emoji}";

		List<string> output = await GetMessageParts(message, prefix, true).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(4, output);

		Assert.AreEqual($"{prefix}{emoji}{ContinuationCharacter}", output[0]);
		Assert.AreEqual($"{prefix}{ContinuationCharacter}{emoji}{ContinuationCharacter}", output[1]);
		Assert.AreEqual($"{prefix}{ContinuationCharacter}{emoji}{ContinuationCharacter}", output[2]);
		Assert.AreEqual($"{prefix}{ContinuationCharacter}{emoji}", output[3]);
	}

	[TestMethod]
	internal void ContinuationCharacterSizeIsProperlyCalculated() => Assert.AreEqual(ContinuationCharacterBytes, Encoding.UTF8.GetByteCount(ContinuationCharacter.ToString()));

	[TestMethod]
	internal async Task DoesntSkipEmptyNewlines() {
		string message = $"asdf{Environment.NewLine}{Environment.NewLine}asdf";

		List<string> output = await GetMessageParts(message).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(1, output);
		Assert.AreEqual(message, output.First());
	}

	[DataRow(false)]
	[DataRow(true)]
	[TestMethod]
	internal async Task DoesntSplitInTheMiddleOfMultiByteChar(bool isAccountLimited) {
		int maxMessageBytes = isAccountLimited ? MaxMessageBytesForLimitedAccounts : MaxMessageBytesForUnlimitedAccounts;
		int longLineLength = maxMessageBytes - ReservedContinuationMessageBytes;

		const string emoji = "😎";

		string longSequence = new('a', longLineLength - 1);
		string message = $"{longSequence}{emoji}";

		List<string> output = await GetMessageParts(message, isAccountLimited: isAccountLimited).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(2, output);

		Assert.AreEqual($"{longSequence}{ContinuationCharacter}", output[0]);
		Assert.AreEqual($"{ContinuationCharacter}{emoji}", output[1]);
	}

	[TestMethod]
	internal async Task DoesntSplitJustBecauseOfLastEscapableCharacter() {
		const string message = "abcdef[";
		const string escapedMessage = @"abcdef\[";

		List<string> output = await GetMessageParts(message).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(1, output);
		Assert.AreEqual(escapedMessage, output.First());
	}

	[DataRow(false)]
	[DataRow(true)]
	[TestMethod]
	internal async Task DoesntSplitOnBackslashNotUsedForEscaping(bool isAccountLimited) {
		int maxMessageBytes = isAccountLimited ? MaxMessageBytesForLimitedAccounts : MaxMessageBytesForUnlimitedAccounts;
		int longLineLength = maxMessageBytes - ReservedContinuationMessageBytes;

		string longLine = new('a', longLineLength - 2);
		string message = $@"{longLine}\";

		List<string> output = await GetMessageParts(message, isAccountLimited: isAccountLimited).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(1, output);
		Assert.AreEqual($@"{message}\", output.First());
	}

	[DataRow(false)]
	[DataRow(true)]
	[TestMethod]
	internal async Task DoesntSplitOnEscapeCharacter(bool isAccountLimited) {
		int maxMessageBytes = isAccountLimited ? MaxMessageBytesForLimitedAccounts : MaxMessageBytesForUnlimitedAccounts;
		int longLineLength = maxMessageBytes - ReservedContinuationMessageBytes;

		string longLine = new('a', longLineLength - 1);
		string message = $"{longLine}[";

		List<string> output = await GetMessageParts(message, isAccountLimited: isAccountLimited).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(2, output);

		Assert.AreEqual($"{longLine}{ContinuationCharacter}", output[0]);
		Assert.AreEqual($@"{ContinuationCharacter}\[", output[1]);
	}

	[TestMethod]
	internal async Task NoNeedForAnySplittingWithNewlines() {
		string message = $"abcdef{Environment.NewLine}ghijkl{Environment.NewLine}mnopqr";

		List<string> output = await GetMessageParts(message).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(1, output);
		Assert.AreEqual(message, output.First());
	}

	[TestMethod]
	internal async Task NoNeedForAnySplittingWithoutNewlines() {
		const string message = "abcdef";

		List<string> output = await GetMessageParts(message).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(1, output);
		Assert.AreEqual(message, output.First());
	}

	[TestMethod]
	internal void ParagraphCharacterSizeIsLessOrEqualToContinuationCharacterSize() => Assert.IsGreaterThanOrEqualTo(Encoding.UTF8.GetByteCount(ParagraphCharacter.ToString()), ContinuationCharacterBytes);

	[TestMethod]
	internal async Task ProperlyEscapesCharacters() {
		const string message = @"[b]bold[/b] \n";
		const string escapedMessage = @"\[b]bold\[/b] \\n";

		List<string> output = await GetMessageParts(message).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(1, output);
		Assert.AreEqual(escapedMessage, output.First());
	}

	[TestMethod]
	internal async Task ProperlyEscapesSteamMessagePrefix() {
		const string prefix = "/pre []";
		const string escapedPrefix = @"/pre \[]";

		const string message = "asdf";

		List<string> output = await GetMessageParts(message, prefix).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(1, output);
		Assert.AreEqual($"{escapedPrefix}{message}", output.First());
	}

	[DataRow(false)]
	[DataRow(true)]
	[TestMethod]
	internal async Task ProperlySplitsLongSingleLine(bool isAccountLimited) {
		int maxMessageBytes = isAccountLimited ? MaxMessageBytesForLimitedAccounts : MaxMessageBytesForUnlimitedAccounts;
		int longLineLength = maxMessageBytes - ReservedContinuationMessageBytes;

		string longLine = new('a', longLineLength);
		string message = $"{longLine}{longLine}{longLine}{longLine}";

		List<string> output = await GetMessageParts(message, isAccountLimited: isAccountLimited).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(4, output);

		Assert.AreEqual($"{longLine}{ContinuationCharacter}", output[0]);
		Assert.AreEqual($"{ContinuationCharacter}{longLine}{ContinuationCharacter}", output[1]);
		Assert.AreEqual($"{ContinuationCharacter}{longLine}{ContinuationCharacter}", output[2]);
		Assert.AreEqual($"{ContinuationCharacter}{longLine}", output[3]);
	}

	[TestMethod]
	internal void ReservedSizeForEscapingIsProperlyCalculated() => Assert.AreEqual(ReservedEscapeMessageBytes, Encoding.UTF8.GetByteCount(@"\") + 4); // Maximum amount of bytes per single UTF-8 character is 4, not 6 as from Encoding.UTF8.GetMaxByteCount(1)

	[TestMethod]
	internal async Task RyzhehvostInitialTestForSplitting() {
		const string prefix = "/me ";

		const string message = """
								<XLimited5> Уже имеет: app/1493800 | Aircraft Carrier Survival: Prolouge
								<XLimited5> Уже имеет: app/349520 | Armillo
								<XLimited5> Уже имеет: app/346330 | BrainBread 2
								<XLimited5> Уже имеет: app/1086690 | C-War 2
								<XLimited5> Уже имеет: app/730 | Counter-Strike: Global Offensive
								<XLimited5> Уже имеет: app/838380 | DEAD OR ALIVE 6
								<XLimited5> Уже имеет: app/582890 | Estranged: The Departure
								<XLimited5> Уже имеет: app/331470 | Everlasting Summer
								<XLimited5> Уже имеет: app/1078000 | Gamecraft
								<XLimited5> Уже имеет: app/266310 | GameGuru
								<XLimited5> Уже имеет: app/275390 | Guacamelee! Super Turbo Championship Edition
								<XLimited5> Уже имеет: app/627690 | Idle Champions of the Forgotten Realms
								<XLimited5> Уже имеет: app/1048540 | Kao the Kangaroo: Round 2
								<XLimited5> Уже имеет: app/370910 | Kathy Rain
								<XLimited5> Уже имеет: app/343710 | KHOLAT
								<XLimited5> Уже имеет: app/253900 | Knights and Merchants
								<XLimited5> Уже имеет: app/224260 | No More Room in Hell
								<XLimited5> Уже имеет: app/343360 | Particula
								<XLimited5> Уже имеет: app/237870 | Planet Explorers
								<XLimited5> Уже имеет: app/684680 | Polygoneer
								<XLimited5> Уже имеет: app/1089130 | Quake II RTX
								<XLimited5> Уже имеет: app/755790 | Ring of Elysium
								<XLimited5> Уже имеет: app/1258080 | Shop Titans
								<XLimited5> Уже имеет: app/759530 | Struckd - 3D Game Creator
								<XLimited5> Уже имеет: app/269710 | Tumblestone
								<XLimited5> Уже имеет: app/304930 | Unturned
								<XLimited5> Уже имеет: app/1019250 | WWII TCG - World War 2: The Card Game

								<Tickr> 1/1 ботов уже имеют игру app/1493800 | Aircraft Carrier Survival: Prolouge.
								<Tickr> 1/1 ботов уже имеют игру app/349520 | Armillo.
								<Tickr> 1/1 ботов уже имеют игру app/346330 | BrainBread 2.
								<Tickr> 1/1 ботов уже имеют игру app/1086690 | C-War 2.
								<Tickr> 1/1 ботов уже имеют игру app/730 | Counter-Strike: Global Offensive.
								<Tickr> 1/1 ботов уже имеют игру app/838380 | DEAD OR ALIVE 6.
								<Tickr> 1/1 ботов уже имеют игру app/582890 | Estranged: The Departure.
								<Tickr> 1/1 ботов уже имеют игру app/331470 | Everlasting Summer.
								<Tickr> 1/1 ботов уже имеют игру app/1078000 | Gamecraft.
								<Tickr> 1/1 ботов уже имеют игру app/266310 | GameGuru.
								<Tickr> 1/1 ботов уже имеют игру app/275390 | Guacamelee! Super Turbo Championship Edition.
								<Tickr> 1/1 ботов уже имеют игру app/627690 | Idle Champions of the Forgotten Realms.
								<Tickr> 1/1 ботов уже имеют игру app/1048540 | Kao the Kangaroo: Round 2.
								<Tickr> 1/1 ботов уже имеют игру app/370910 | Kathy Rain.
								<Tickr> 1/1 ботов уже имеют игру app/343710 | KHOLAT.
								<Tickr> 1/1 ботов уже имеют игру app/253900 | Knights and Merchants.
								<Tickr> 1/1 ботов уже имеют игру app/224260 | No More Room in Hell.
								<Tickr> 1/1 ботов уже имеют игру app/343360 | Particula.
								<Tickr> 1/1 ботов уже имеют игру app/237870 | Planet Explorers.
								<Tickr> 1/1 ботов уже имеют игру app/684680 | Polygoneer.
								<Tickr> 1/1 ботов уже имеют игру app/1089130 | Quake II RTX.
								<Tickr> 1/1 ботов уже имеют игру app/755790 | Ring of Elysium.
								<Tickr> 1/1 ботов уже имеют игру app/1258080 | Shop Titans.
								<Tickr> 1/1 ботов уже имеют игру app/759530 | Struckd - 3D Game Creator.
								<Tickr> 1/1 ботов уже имеют игру app/269710 | Tumblestone.
								<Tickr> 1/1 ботов уже имеют игру app/304930 | Unturned.
								""";

		List<string> output = await GetMessageParts(message, prefix).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(2, output);

		foreach (string messagePart in output) {
			if ((messagePart.Length <= prefix.Length) || !messagePart.StartsWith(prefix, StringComparison.Ordinal)) {
				Assert.Fail();

				return;
			}

			string[] lines = messagePart.Split(SharedInfo.NewLineIndicators, StringSplitOptions.None);

			int bytes = lines.Where(static line => line.Length > 0).Sum(Encoding.UTF8.GetByteCount) + ((lines.Length - 1) * NewlineWeight);

			if (bytes > MaxMessageBytesForUnlimitedAccounts) {
				Assert.Fail();

				return;
			}
		}
	}

	[DataRow(false)]
	[DataRow(true)]
	[TestMethod]
	internal async Task SplitsOnNewlinesWithParagraphCharacter(bool isAccountLimited) {
		int maxMessageBytes = isAccountLimited ? MaxMessageBytesForLimitedAccounts : MaxMessageBytesForUnlimitedAccounts;

		StringBuilder newlinePartBuilder = new();

		for (ushort bytes = 0; bytes < maxMessageBytes - ReservedContinuationMessageBytes - NewlineWeight;) {
			if (newlinePartBuilder.Length > 0) {
				bytes += NewlineWeight;
				newlinePartBuilder.Append(Environment.NewLine);
			}

			bytes++;
			newlinePartBuilder.Append('a');
		}

		string newlinePart = newlinePartBuilder.ToString();
		string message = $"{newlinePart}{Environment.NewLine}{newlinePart}{Environment.NewLine}{newlinePart}{Environment.NewLine}{newlinePart}";

		List<string> output = await GetMessageParts(message, isAccountLimited: isAccountLimited).ToListAsync(CancellationToken).ConfigureAwait(false);

		Assert.HasCount(4, output);

		Assert.AreEqual($"{newlinePart}{ParagraphCharacter}", output[0]);
		Assert.AreEqual($"{newlinePart}{ParagraphCharacter}", output[1]);
		Assert.AreEqual($"{newlinePart}{ParagraphCharacter}", output[2]);
		Assert.AreEqual(newlinePart, output[3]);
	}

	[TestMethod]
	internal async Task ThrowsOnTooLongNewlinesPrefix() {
		string prefix = new('\n', (MaxMessagePrefixBytes / NewlineWeight) + 1);

		const string message = "asdf";

		await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => await GetMessageParts(message, prefix).ToListAsync(CancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
	}

	[TestMethod]
	internal async Task ThrowsOnTooLongPrefix() {
		string prefix = new('x', MaxMessagePrefixBytes + 1);

		const string message = "asdf";

		await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => await GetMessageParts(message, prefix).ToListAsync(CancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
	}
}
#pragma warning restore CA1812 // False positive, the class is used during MSTest
