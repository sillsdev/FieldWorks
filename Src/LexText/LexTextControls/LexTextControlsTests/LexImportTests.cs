// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.LexText.Controls;
using Sfm2Xml;

namespace LexTextControlsTests
{
	/// <summary>
	/// Tests the behavior of LexImport (and hence some aspects of FDO.XmlImportData).
	/// </summary>
	[TestFixture]
	public class LexImportTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private string allNumbered_OutOfOrder =
@"\lx aha
\hm 2
\de two
\lx aha
\hm 1
\de one
\lx aha
\hm 3
\de three
\lx bahaa
\mn aha3
\lx bahaaa
\mn aha1
\lx bahaaaa
\mn aha2";
		[Test]
		public void ImportHomographs_AllNumbered_OutOfOrder()
		{
			DoImport(allNumbered_OutOfOrder, MakeDefaultFields(), 6);

			VerifyHomographNumber("one", 1);
			VerifyHomographNumber("two", 2);
			VerifyHomographNumber("three", 3);
			VerifyHomographXRef("bahaa", 3);
			VerifyHomographXRef("bahaaa", 1);
			VerifyHomographXRef("bahaaaa", 2);
		}

		private string someNumbered_OutOfOrder_Subentry =
@"\lx baha
\hm 3
\de threeB
\lx bahh
\se baha
\de oneB
\lx ha
\se baha
\de twoB ";
		[Test]
		public void ImportHomographs_SomeNumbered_OutOfOrder_Subentry()
		{
			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			DoImport(someNumbered_OutOfOrder_Subentry, MakeDefaultFields(), 5);

			VerifyHomographNumber("oneB", 1);
			VerifyHomographNumber("twoB", 2);
			VerifyHomographNumber("threeB", 3);
		}

		private string noneNumberedTwoSubentries =
@"\lx bahaC
\de oneC
\lx bahhC
\se bahaC
\de twoC
\lx haC
\se bahaC
\de threeC ";
		[Test]
		public void ImportHomographs_NoneNumberedTwoSubentries()
		{
			DoImport(noneNumberedTwoSubentries, MakeDefaultFields(), 5);

			VerifyHomographNumber("oneC", 1);
			VerifyHomographNumber("twoC", 2);
			VerifyHomographNumber("threeC", 3);
		}

		private string refsToSubentry =
@"\lx zahu
\se zahuwa
\de oneD
\lx huwa
\se zahuwa
\de twoD
\lx zahuwa
\hm 3
\de threeD
\lx zahuwa
\hm 4
\de four
\lx zahuwua
\mn zahuwa1
\de one ";
		[Test]
		public void ImportHomographs_RefsToSubentry()
		{
			var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			DoImport(refsToSubentry, MakeDefaultFields(), 7);

			VerifyHomographNumber("oneD", 1);
			VerifyHomographNumber("twoD", 2);
			VerifyHomographNumber("threeD", 3);
			VerifyHomographXRef("zahuwua", 1);
		}

		private string refsToHomograph1OrBlankBothWork =
@"\lx aba
\de test a

\lx aba
\hm 2
\de test a2

\lx baba
\hm 1
\de test b

\lx baba
\hm 2
\de test b2

\lx testA
\mn aba

\lx testB
\mn aba1

\lx testC
\mn baba

\lx testD
\mn baba1";
		[Test]
		public void ImportHomographs_RefsToHomograph1OrBlankBothWork()
		{
			DoImport(refsToHomograph1OrBlankBothWork, MakeDefaultFields(), 8);

			VerifyHomographNumber("test a", 1);
			VerifyHomographNumber("test a2", 2);
			VerifyHomographNumber("test b", 1);
			VerifyHomographNumber("test b2", 2);
			VerifyHomographXRef("testA", 1);
			VerifyHomographXRef("testB", 1);
			VerifyHomographXRef("testC", 1);
			VerifyHomographXRef("testD", 1);
		}

		private string sfmDataWithVariantsAndMainEntryLinks =
@"\lx axle
\va aa
\ps v
\ge axle
\se ab

\lx aa
\mn axle
\ps v
\ge aa

\lx ab
\mn axle
\ps v
\ge ab";
		[Test]
		public void ImportVariantsAndMainEntryRefs_DoesNotDuplicateEntries()
		{
			DoImport(sfmDataWithVariantsAndMainEntryLinks, MakeDefaultFields(), 3);
		}

		private string sfmDataWithMinorBeforeMainEntryLinks =
@"\lx ab
\mn a
\ps n

\lx a
\va ab
\ps v";
		[Test]
		public void ImportMinorBeforeMain_DoesNotDuplicateEntries()
		{
			DoImport(sfmDataWithMinorBeforeMainEntryLinks, MakeDefaultFields(), 2);
		}

		private string sfmDataWithBlankPosFollowingRealPos =
@"\lx a
\ps n
\sn
\de thing one
\ps
\sn
\de non-thing one ";
		[Test]
		public void ImportBlankPsAfterNonBlank_DoesNotDropBlankPosAndDupPrevious()
		{
			DoImport(sfmDataWithBlankPosFollowingRealPos, MakeDefaultFields(), 1);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().First();
			Assert.AreEqual(2, entry.SensesOS.Count(), "Import should have resulted in two senses");
			Assert.AreEqual(entry.SensesOS[0].MorphoSyntaxAnalysisRA.PosFieldName, "n");
			Assert.AreNotEqual(entry.SensesOS[1].MorphoSyntaxAnalysisRA.PosFieldName, "n");
		}

		/// <summary>
		/// This messy process simulates what the real import wizard does to import a string like input,
		/// with the given field mappings, and verifies that it produces the expected number of new lexEntries.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="sfmInfo"></param>
		/// <param name="expectedCreations"></param>
		private void DoImport(string input, List<FieldHierarchyInfo> sfmInfo, int expectedCreations)
		{
			var tempLocation = Path.GetTempFileName();
			File.Delete(tempLocation);
			var tempDir = Path.ChangeExtension(tempLocation, "") + Path.DirectorySeparatorChar;
			Directory.CreateDirectory(tempDir);
			try
			{
				string sTransformDir = Path.Combine(FwDirectoryFinder.CodeDirectory,
					String.Format("Language Explorer{0}Import{0}", Path.DirectorySeparatorChar));
				var sut = new LexImport(Cache, tempDir, sTransformDir);

				string databaseFileName = Path.GetTempFileName();
				File.WriteAllText(databaseFileName, input, Encoding.UTF8);

				var entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
				// JohnT: I don't know why this is needed, the base class is supposed to Undo anything we create.
				// But somehow things are getting left over from one test and messing up another.
				foreach (var entry in entryRepo.AllInstances())
					entry.Delete();
				Assert.That(entryRepo.AllInstances().Count(), Is.EqualTo(0));

				// Create the map file that controls the import

				var uiLangsNew = new Hashtable();
				var infoEn = new LanguageInfoUI("English", "English", "", "en");
				var vernId = Cache.LangProject.DefaultVernacularWritingSystem.Id;
				var infoV = new LanguageInfoUI("Vernacular", "Vern", "", vernId);
				uiLangsNew[infoEn.Key] = infoEn;
				uiLangsNew["Vernacular"] = infoV;

				var lexFields = new LexImportFields();
				lexFields.ReadLexImportFields(sTransformDir + "ImportFields.xml");
				var customFields = new LexImportFields();

				var ifMarker = new List<ClsInFieldMarker>();
				string mapFile = Path.GetTempFileName();

				STATICS.NewMapFileBuilder(uiLangsNew, lexFields, customFields, sfmInfo, ifMarker, mapFile);

				var phase1Output = Path.Combine(tempDir, LexImport.s_sPhase1FileName);

				var converter = new LexImportWizard.FlexConverter(Cache);
				converter.AddPossibleAutoField("Entry", "eires"); // found these by running an example.
				converter.AddPossibleAutoField("Sense", "sires");
				converter.AddPossibleAutoField("Subentry", "seires");
				converter.AddPossibleAutoField("Variant", "veires");
				converter.Convert(databaseFileName, mapFile, phase1Output);

				m_actionHandler.EndUndoTask(); // import starts its own.

				sut.Import(new DummyProgressDlg(), new object[]
				{
					true, // run to completion
					5, // last step to execute => all of them
					0, // phase already completed
					phase1Output,
					3, // lex entries in file
					false, // don't want to display import report
					"", // phase 1 html report, only used in generating messages, I think.
					LexImport.s_sPhase1FileName // required always
				});
				Assert.That(entryRepo.AllInstances().Count(), Is.EqualTo(expectedCreations), "wrong number of entries created");

			}
			finally
			{
				Directory.Delete(tempDir, true);
			}
		}

		/// <summary>
		/// Make the set of field info objects needed to parse the sample inputs here.
		/// </summary>
		/// <returns></returns>
		private static List<FieldHierarchyInfo> MakeDefaultFields()
		{
			var sfmInfo = new List<FieldHierarchyInfo>();
			sfmInfo.Add(new FieldHierarchyInfo("lx", "lex", "Vernacular", true, "Entry"));
			sfmInfo.Add(new FieldHierarchyInfo("hm", "hom", "English", false, "Entry"));
			sfmInfo.Add(new FieldHierarchyInfo("de", "def", "English", true, "Sense"));
			sfmInfo.Add(new FieldHierarchyInfo("ge", "glos", "English", true, "Sense"));
			sfmInfo.Add(new FieldHierarchyInfo("ps", "pos", "English", true, "Sense"));
			sfmInfo.Add(new FieldHierarchyInfo("mn", "meref", "Vernacular", false, "Entry"));
			var variantInfo = new FieldHierarchyInfo("va", "var", "Vernacular", true, "Variant");
			sfmInfo.Add(variantInfo);
			variantInfo.RefFunc = "fr";
			variantInfo.RefFuncWS = "fr";
			var subentryInfo = new FieldHierarchyInfo("se", "sub", "Vernacular", true, "SubEntry");
			sfmInfo.Add(subentryInfo);
			subentryInfo.RefFuncWS = "Derivative";
			subentryInfo.RefFunc = "en";
			return sfmInfo;
		}

		/// <summary>
		/// Verify that the entry whose lexeme form is lf has a cross-ref (LexEntryRef with first component) with the
		/// expected homograph number.
		/// </summary>
		/// <param name="lf"></param>
		/// <param name="hn"></param>
		void VerifyHomographXRef(string lf, int hn)
		{
			var morphRepo = Cache.ServiceLocator.GetInstance<IMoFormRepository>();
			//var morphs = (from m in morphRepo.AllInstances() select m.Form.VernacularDefaultWritingSystem.Text).ToArray();
			var morph =
				(from m in morphRepo.AllInstances() where m.Form.VernacularDefaultWritingSystem.Text == lf select m)
					.FirstOrDefault();
			Assert.That(morph, Is.Not.Null);
			var entry = (ILexEntry)morph.Owner;
			var entryRef = entry.EntryRefsOS.FirstOrDefault();
			Assert.That(entryRef, Is.Not.Null);
			var component = (ILexEntry)entryRef.ComponentLexemesRS.FirstOrDefault();
			Assert.That(component.HomographNumber, Is.EqualTo(hn));
		}

		// The entry with the specified sense definition should have the specified homograph number.
		private void VerifyHomographNumber(string defn, int hn)
		{
			var senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			//var senses = (from s in senseRepo.AllInstances() select s.Definition.AnalysisDefaultWritingSystem.Text).ToArray();
			var sense =
				(from s in senseRepo.AllInstances() where s.Definition.AnalysisDefaultWritingSystem.Text == defn select s)
					.FirstOrDefault();
			Assert.That(sense, Is.Not.Null);
			var entry = sense.Entry;
			Assert.That(entry.HomographNumber, Is.EqualTo(hn));
		}
	}
}
