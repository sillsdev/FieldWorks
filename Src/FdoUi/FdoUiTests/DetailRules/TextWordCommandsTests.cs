// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// When Find in Dictionary and Add to Dictionary apply to a word of text, and the form
	/// Add to Dictionary offers.
	/// </summary>
	[TestFixture]
	public class TextWordCommandsTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void WritingSystemOf_ASpanInOneWritingSystem_IsThatOne_ElseNone()
		{
			var tss = MixedString();

			Assert.That(TextWordCommands.WritingSystemOf(tss, 0, 4), Is.EqualTo(Cache.DefaultVernWs));
			Assert.That(TextWordCommands.WritingSystemOf(tss, 5, 9), Is.EqualTo(Cache.DefaultAnalWs));
			Assert.That(TextWordCommands.WritingSystemOf(tss, 2, 7), Is.EqualTo(0), "the span mixes two");
			Assert.That(TextWordCommands.WritingSystemOf(tss, 3, 3), Is.EqualTo(0), "an empty span");
		}

		[Test]
		public void CanLookUp_AnyWordThatIsNotEmpty()
		{
			Assert.That(TextWordCommands.CanLookUp(2, 5), Is.True);
			Assert.That(TextWordCommands.CanLookUp(5, 5), Is.False);
		}

		[Test]
		public void CanAddToLexicon_OnlyAWordInTheDefaultVernacular()
		{
			var tss = MixedString();

			Assert.That(TextWordCommands.CanAddToLexicon(Cache, tss, 0, 4, 0), Is.True);
			Assert.That(TextWordCommands.CanAddToLexicon(Cache, tss, 5, 9, 0), Is.False, "an analysis word");
			Assert.That(TextWordCommands.CanAddToLexicon(Cache, tss, 4, 4, 0), Is.False, "no word");
			Assert.That(TextWordCommands.CanAddToLexicon(Cache, tss, 5, 9, Cache.DefaultVernWs), Is.True,
				"the writing system the selection reports wins");
		}

		[Test]
		public void FormOf_CutsTheWordFromTheString_WithItsWritingSystem()
		{
			var form = TextWordCommands.FormOf(MixedString(), 1, 3);

			Assert.That(form.Text, Is.EqualTo("ou"));
			Assert.That(form.get_WritingSystem(0), Is.EqualTo(Cache.DefaultVernWs));
		}

		// "nous" in the vernacular, then " word" in the analysis writing system.
		private ITsString MixedString()
		{
			var builder = TsStringUtils.MakeStrBldr();
			builder.Replace(0, 0, "nous", TsStringUtils.MakeProps(null, Cache.DefaultVernWs));
			builder.Replace(4, 4, " word", TsStringUtils.MakeProps(null, Cache.DefaultAnalWs));
			return builder.GetString();
		}
	}
}
