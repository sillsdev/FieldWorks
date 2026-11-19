// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The word around a point, pinned to the cases of the Views text engine's rule: letters
	/// and numbers join, anything else ends the word, and so does a change of writing system,
	/// of the spelling-check setting, or of what two styles set of them.
	/// </summary>
	[TestFixture]
	public class TextWordBoundariesTests : MemoryOnlyBackendProviderTestBase
	{
		private const string StyleA = "Style A";
		private const string StyleB = "Style B";

		[TestCase(6, 4, 9, TestName = "Inside a word, the whole word")]
		[TestCase(4, 4, 9, TestName = "At a word's start, that word")]
		[TestCase(3, 0, 3, TestName = "At a word's end, that word")]
		[TestCase(13, 10, 13, TestName = "At the end of the string, the last word")]
		[TestCase(0, 0, 3, TestName = "At the start of the string, the first word")]
		public void FindsTheWordAroundThePoint(int ich, int expectedMin, int expectedLim)
			=> AssertWord(Plain("the quick fox"), ich, expectedMin, expectedLim);

		[Test]
		public void BetweenTwoSpaces_NoWord() => AssertWord(Plain("the  fox"), 4, 4, 4);

		[Test]
		public void NumbersJoinTheWord() => AssertWord(Plain("abc123 def"), 1, 0, 6);

		[Test]
		public void PunctuationEndsTheWord()
		{
			AssertWord(Plain("abc,def"), 1, 0, 3);
			AssertWord(Plain("abc,def"), 5, 4, 7);
		}

		[Test]
		public void AWritingSystemChangeEndsTheWord()
		{
			var tss = Runs(("abc", Props(Cache.DefaultAnalWs)), ("def", Props(Cache.DefaultVernWs)));
			AssertWord(tss, 1, 0, 3);
			AssertWord(tss, 3, 3, 6, "at the boundary the word is the one after the point");
		}

		[Test]
		public void ASpellingCheckChangeEndsTheWord()
		{
			var tss = Runs(("abc", Props(Cache.DefaultVernWs)), ("def", Props(Cache.DefaultVernWs, doNotCheck: true)));
			AssertWord(tss, 1, 0, 3);
		}

		[Test]
		public void TwoStyles_EndTheWordOnlyWhenWhatTheySetDiffers()
		{
			var tss = Runs(("abc", Props(Cache.DefaultVernWs, StyleA)), ("def", Props(Cache.DefaultVernWs, StyleB)));

			AssertWord(tss, 1, 0, 3, "style A turns spelling checks off and B does not",
				name => name == StyleA ? Props(0, doNotCheck: true) : TsStringUtils.MakeProps(null, 0));
			AssertWord(tss, 1, 0, 6, "the styles set the same", name => TsStringUtils.MakeProps(null, 0));
			AssertWord(tss, 1, 0, 6, "without a stylesheet styles never end a word", null);
		}

		[Test]
		public void AnEmptyString_HasNoWord()
		{
			AssertWord(Plain(string.Empty), 0, 0, 0);
			TextWordBoundaries.Find(null, 3, Cache.WritingSystemFactory, null, out var ichMin, out var ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(0));
		}

		private void AssertWord(ITsString tss, int ich, int expectedMin, int expectedLim, string because = null,
			Func<string, ITsTextProps> styleProperties = null)
		{
			TextWordBoundaries.Find(tss, ich, Cache.WritingSystemFactory, styleProperties, out var ichMin, out var ichLim);
			Assert.That(new[] { ichMin, ichLim }, Is.EqualTo(new[] { expectedMin, expectedLim }), because);
		}

		private ITsString Plain(string text) => TsStringUtils.MakeString(text, Cache.DefaultVernWs);

		private static ITsString Runs(params (string Text, ITsTextProps Props)[] runs)
		{
			var builder = TsStringUtils.MakeStrBldr();
			foreach (var run in runs)
				builder.Replace(builder.Length, builder.Length, run.Text, run.Props);
			return builder.GetString();
		}

		private static ITsTextProps Props(int ws, string style = null, bool doNotCheck = false)
		{
			var builder = TsStringUtils.MakePropsBldr();
			if (ws != 0)
				builder.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);
			if (style != null)
				builder.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, style);
			if (doNotCheck)
			{
				builder.SetIntPropValues((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
					(int)SpellingModes.ksmDoNotCheck);
			}
			return builder.GetTextProps();
		}
	}
}
