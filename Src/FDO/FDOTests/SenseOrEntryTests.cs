// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.CoreImpl;
using SIL.CoreImpl.Text;
using SIL.CoreImpl.WritingSystems;
using SIL.FieldWorks.FDO.DomainImpl;
// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary/>
	public class SenseOrEntryTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary/>
		[Test]
		public void ISenseOrEntryHeadwordRef_IncludesSenseNumber()
		{
			var mainEntry = CreateInterestingLexEntry(Cache, "MainEntry", "MainSense");
			AddSenseToEntry(mainEntry, "SecondSense", EnsureWritingSystemSetup(Cache, "en", false), Cache);
			var secondSense = new SenseOrEntry(mainEntry.SensesOS[1]);
			CreateInterestingLexEntry(Cache, "MainEntry", "Nonsense"); // create a homograph

			// SUT
			Assert.AreEqual("MainEntry1 2", secondSense.HeadWordRef.BestVernacularAlternative.Text);
		}

		/// <summary>
		/// Creates an ILexEntry object, optionally with specified headword and gloss
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="headword">Optional: defaults to 'Citation'</param>
		/// <param name="gloss">Optional: defaults to 'gloss'</param>
		/// <returns></returns>
		internal static ILexEntry CreateInterestingLexEntry(FdoCache cache, string headword = "Citation", string gloss = "gloss")
		{
			var entryFactory = cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = entryFactory.Create();
			var wsEn = EnsureWritingSystemSetup(cache, "en", false);
			var wsFr = EnsureWritingSystemSetup(cache, "fr", true);
			AddHeadwordToEntry(entry, headword, wsFr, cache);
			entry.Comment.set_String(wsEn, TsStringUtils.MakeString("Comment", wsEn));
			AddSenseToEntry(entry, gloss, wsEn, cache);
			return entry;
		}

		private static void AddHeadwordToEntry(ILexEntry entry, string headword, int wsId, FdoCache cache)
		{
			// The headword field is special: it uses Citation if available, or LexemeForm if Citation isn't filled in
			entry.CitationForm.set_String(wsId, TsStringUtils.MakeString(headword, wsId));
		}

		private static void AddSenseToEntry(ILexEntry entry, string gloss, int wsId, FdoCache cache)
		{
			var senseFactory = cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			entry.SensesOS.Add(sense);
			if (!string.IsNullOrEmpty(gloss))
				sense.Gloss.set_String(wsId, TsStringUtils.MakeString(gloss, wsId));
		}

		private static int EnsureWritingSystemSetup(FdoCache cache, string wsStr, bool isVernacular)
		{
			var wsFact = cache.WritingSystemFactory;
			var result = wsFact.GetWsFromStr(wsStr);
			if (result < 1)
			{
				if (isVernacular)
				{
					cache.LangProject.AddToCurrentVernacularWritingSystems(cache.WritingSystemFactory.get_Engine(wsStr) as CoreWritingSystemDefinition);
				}
				else
				{
					cache.LangProject.AddToCurrentAnalysisWritingSystems(cache.WritingSystemFactory.get_Engine(wsStr) as CoreWritingSystemDefinition);
				}
			}
			return wsFact.GetWsFromStr(wsStr);
		}
	}
}
