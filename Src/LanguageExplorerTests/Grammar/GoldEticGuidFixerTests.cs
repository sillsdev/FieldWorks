// Copyright (c) 2014-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using LanguageExplorer.Areas.Grammar.Tools.PosEdit;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;

namespace LanguageExplorerTests.Grammar
{
	public class GoldEticGuidFixerTests : BaseTest
	{
		protected FdoCache Cache { get; set; }

		[SetUp]
		public void TestSetup()
		{
			Cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
																			 "en", "fr", "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories, new FdoSettings());
			var loader = new XmlList();
			loader.ImportList(Cache.LangProject, "PartsOfSpeech", Path.Combine(FwDirectoryFinder.TemplateDirectory, "POS.xml"),
									new DummyProgressDlg());
			// This allows tests to do any kind of data changes without worrying about starting a UOW.
			Cache.ActionHandlerAccessor.BeginUndoTask("Undo doing stuff", "Redo doing stuff");
		}

		/// <summary>
		/// Teardown method: destroy the memory-only mock cache.
		/// </summary>
		[TearDown]
		public void DestroyMockCache()
		{
			Cache.ActionHandlerAccessor.EndUndoTask();
			Cache.Dispose();
			Cache = null;
		}

		[Test]
		public void ReplacePOSGuidsWithGoldEticGuids_WrongPosGuidChangedToMatchStandard()
		{
			// This test will grab the first part of speach from the project and delete it
			// it then sets up a new item which which matches the id of that first part of speech and
			// makes sure that the guid for the new one is swapped back to the standard.
			// Note: This test assumes our POS.xml template has correct guids
			var posList = Cache.LangProject.PartsOfSpeechOA;
			var firstDefaultPos = (IPartOfSpeech)posList.PossibilitiesOS[0];
			posList.PossibilitiesOS.Remove(firstDefaultPos);
			var goldGuid = firstDefaultPos.Guid;
			Assert.Throws<KeyNotFoundException>(()=>Cache.ServiceLocator.ObjectRepository.GetObject(goldGuid));
			var myNewPos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posList.PossibilitiesOS.Insert(0, myNewPos);
			myNewPos.Name.CopyAlternatives(firstDefaultPos.Name);
			myNewPos.CatalogSourceId = firstDefaultPos.CatalogSourceId;
			var nonStandardGuid = myNewPos.Guid;
			// SUT
			Assert.That(GoldEticGuidFixer.ReplacePOSGuidsWithGoldEticGuids(Cache), Is.True);
			Assert.Throws<KeyNotFoundException>(() => Cache.ServiceLocator.ObjectRepository.GetObject(nonStandardGuid));
			Assert.NotNull(Cache.ServiceLocator.ObjectRepository.GetObject(goldGuid));
		}

		[Test]
		public void ReplacePOSGuidsWithGoldEticGuids_WrongPosGuidInWrongPlaceGuidChangedToMatchStandard()
		{
			// Note: This test assumes our POS.xml template has correct guids
			var posList = Cache.LangProject.PartsOfSpeechOA;
			var firstDefaultPos = (IPartOfSpeech)posList.PossibilitiesOS[0];
			posList.PossibilitiesOS.Remove(firstDefaultPos);
			var secondDefaultPos = (IPartOfSpeech)posList.PossibilitiesOS[0];
			var goldGuid = firstDefaultPos.Guid;
			Assert.Throws<KeyNotFoundException>(() => Cache.ServiceLocator.ObjectRepository.GetObject(goldGuid));
			var myNewPos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			// Insert the item with a wrong guid in an unexpected location
			secondDefaultPos.SubPossibilitiesOS.Insert(0, myNewPos);
			myNewPos.Name.CopyAlternatives(firstDefaultPos.Name);
			myNewPos.CatalogSourceId = firstDefaultPos.CatalogSourceId;
			var nonStandardGuid = myNewPos.Guid;
			// SUT
			Assert.That(GoldEticGuidFixer.ReplacePOSGuidsWithGoldEticGuids(Cache), Is.True);
			Assert.Throws<KeyNotFoundException>(() => Cache.ServiceLocator.ObjectRepository.GetObject(nonStandardGuid));
			Assert.NotNull(Cache.ServiceLocator.ObjectRepository.GetObject(goldGuid));
		}

		[Test]
		public void ReplacePOSGuidsWithGoldEticGuids_EntriesUsingChangingPosAreNotNegativelyAffected()
		{
			var posList = Cache.LangProject.PartsOfSpeechOA;
			var firstDefaultPos = (IPartOfSpeech)posList.PossibilitiesOS[0];
			var originalText = firstDefaultPos.Name.BestVernacularAnalysisAlternative.Text;
			posList.PossibilitiesOS.Remove(firstDefaultPos);
			var servLoc = Cache.ServiceLocator;
			var myNewPos = servLoc.GetInstance<IPartOfSpeechFactory>().Create();
			posList.PossibilitiesOS.Insert(0, myNewPos);
			myNewPos.Name.CopyAlternatives(firstDefaultPos.Name);
			myNewPos.CatalogSourceId = firstDefaultPos.CatalogSourceId;
			var entry = servLoc.GetInstance<ILexEntryFactory>().Create();
			var sense = servLoc.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			var msa = servLoc.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;
			msa.PartOfSpeechRA = myNewPos;
			// SUT
			Assert.That(GoldEticGuidFixer.ReplacePOSGuidsWithGoldEticGuids(Cache), Is.True);
			Assert.NotNull(msa.PartOfSpeechRA);
			Assert.AreEqual(originalText, msa.PartOfSpeechRA.Name.BestVernacularAnalysisAlternative.Text);
		}

		[Test]
		public void ReplacePOSGuidsWithGoldEticGuids_CustomPosItemsAreUnaffected()
		{
			var posList = Cache.LangProject.PartsOfSpeechOA;
			var myNewPos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			posList.PossibilitiesOS.Add(myNewPos);
			myNewPos.Name.set_String(wsEn, Cache.TsStrFactory.MakeString("Mine", wsEn));
			var myNewPosGuid = myNewPos.Guid;
			// SUT
			Assert.That(GoldEticGuidFixer.ReplacePOSGuidsWithGoldEticGuids(Cache), Is.False);
			Assert.AreEqual(myNewPos, Cache.ServiceLocator.GetObject(myNewPosGuid),
								"Guid should not have been replaced");
		}

		[Test]
		public void ReplacePOSGuidsWithGoldEticGuids_NoChangesReturnsFalse()
		{
			Assert.That(GoldEticGuidFixer.ReplacePOSGuidsWithGoldEticGuids(Cache), Is.False);
		}
	}
}
