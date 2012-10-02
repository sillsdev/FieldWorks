// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LingTests.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Validation;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;


namespace SIL.FieldWorks.FDO.FDOTests
{
	#region LingTests

	#region LingTests with real cache - DON'T ADD TESTS HERE!
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests that require a real database because they test methods that directly make use
	/// of SQL commands or stored procedures.
	/// </summary>
	/// <remarks>Do not add tests to this fixture unless there is no other way to write this
	/// test without using the real database. Running a test with the real database takes
	/// times longer than doing it with the in-memory cache.</remarks>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LingTestsWithDb_DONT_ADD_TESTS_HERE : InDatabaseFdoTestBase
	{
		/// <summary>
		/// Test adding wordform with special characters
		/// </summary>
		[Test]
		public void AddWordformSpecialCharacter()
		{
			CheckDisposed();

			System.Random generator = new Random();
			ILangProject lp = m_fdoCache.LangProject;
			char kSpecialCharacter = '\x014b';		// ŋ
			// Make a wordform which has a random number in it in order to reduce the chance to the word is already loaded.
			string kWordForm = "aaa" + generator.Next().ToString() + kSpecialCharacter;
			IWfiWordform word = WfiWordform.FindOrCreateWordform(m_fdoCache, kWordForm, lp.DefaultVernacularWritingSystem);
			Assert.IsTrue(word.Hvo != 0, "Adding word failed, gave hvo = 0");


			int checkIndex = kWordForm.Length - 1;
			Assert.AreEqual(kSpecialCharacter, word.Form.VernacularDefaultWritingSystem[checkIndex],
				"Special character was not handled correctly.");
		}

		/// <summary>
		/// Test the ParseCount method.
		/// </summary>
		[Test]
		public void CheckAgentCounts()
		{
			CheckDisposed();

			int hvoWf = m_fdoCache.LangProject.WordformInventoryOA.WordformsOC.HvoArray[0];
			IWfiWordform wf = WfiWordform.CreateFromDBObject(m_fdoCache, hvoWf);
			int cOldUser = wf.UserCount;
			int cOldParse = wf.ParserCount;
			WfiAnalysis wa1 = new WfiAnalysis();
			WfiAnalysis wa2 = new WfiAnalysis();
			wf.AnalysesOC.Add(wa1);
			wf.AnalysesOC.Add(wa2);
			ICmAgent ca = m_fdoCache.LangProject.DefaultParserAgent;
			ca.SetEvaluation(wa1.Hvo, 1, "");
			ca = m_fdoCache.LangProject.DefaultUserAgent;
			ca.SetEvaluation(wa2.Hvo, 1, "");
			int cNewUser = wf.UserCount;
			int cNewParse = wf.ParserCount;
			Assert.IsTrue(cOldUser + 1 == cNewUser, "UserCount wrong: Expected " +
				cNewUser + " but got " + (cOldUser + 1));
			Assert.IsTrue(cOldParse + 1 == cNewParse, "ParserCount wrong: Expected " +
				cNewParse + " but got " + (cOldParse + 1));
		}

		/// <summary>
		/// Test the NumberOfEntries methods.
		/// </summary>
		[Test]
		public void CheckNumberOfEntries()
		{
			CheckDisposed();

			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(new LexEntry());
			m_fdoCache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(new PartOfSpeech());
			PartOfSpeech pos = (Ling.PartOfSpeech)m_fdoCache.LangProject.PartsOfSpeechOA.PossibilitiesOS.FirstItem;
			int hvoLme = m_fdoCache.LangProject.LexDbOA.EntriesOC.HvoArray[0];
			ILexEntry lme = LexEntry.CreateFromDBObject(m_fdoCache, hvoLme);
			int cRef1 = pos.NumberOfLexEntries;
			MoStemMsa msm = new MoStemMsa();
			lme.MorphoSyntaxAnalysesOC.Add(msm);
			msm.PartOfSpeechRA = pos;
			int cRef2 = pos.NumberOfLexEntries;
			Assert.IsTrue(cRef1 + 1 == cRef2, "NumberOfLexEntries for MoStemMsa was " +
				cRef2 + " but should have been" + (cRef1 + 1));

			MoMorphType mmt = (Ling.MoMorphType)m_fdoCache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.FirstItem;
			cRef1 = mmt.NumberOfLexEntries;
			MoStemAllomorph msa = new MoStemAllomorph();
			lme.AlternateFormsOS.Append(msa);
			msa.MorphTypeRA = mmt;
			cRef2 = mmt.NumberOfLexEntries;
			Assert.IsTrue(cRef1 + 1 == cRef2, "NumberOfLexEntries for MoMorphType was " +
				cRef2 + " but should have been" + (cRef1 + 1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the static method which retreives the set of abbreviations for PhNaturalClasses
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNaturalClassAbbreviations()
		{
			CheckDisposed();

			string[] sa = PhNaturalClass.ClassAbbreviations(Cache);
			Assert.IsTrue(sa.Length == 3, "Expect three abbreviations in the set of natural classes");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the static method which retreives the set of abbreviations for PhNaturalClasses
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNaturalClassNames()
		{
			CheckDisposed();

			string[] sa = PhNaturalClass.ClassNames(Cache);
			Assert.IsTrue(sa.Length == 3, "Expect three abbreviations in the set of natural classes");
		}
		/// <summary>
		/// Check to see whether homograph collection works.
		/// This needs a real database because it uses SQL to get a base list of entries
		/// to compare.
		/// </summary>
		[Test]
		public void HomographCollectionWorks()
		{
			CheckDisposed();

			LexEntry lme = new LexEntry();
			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(lme);
			string sLexForm = "unitTestLexemeForm";
			lme.LexemeFormOA = new MoStemAllomorph();
			lme.LexemeFormOA.Form.VernacularDefaultWritingSystem = sLexForm;

			// Make sure it has 2 other homographs.
			LexEntry lme2 = new LexEntry();
			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(lme2);
			lme2.LexemeFormOA = new MoStemAllomorph();
			lme2.LexemeFormOA.Form.VernacularDefaultWritingSystem = sLexForm;
			LexEntry lme3 = new LexEntry();
			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(lme3);
			lme3.LexemeFormOA = new MoStemAllomorph();
			lme3.LexemeFormOA.Form.VernacularDefaultWritingSystem = sLexForm;

			string homographForm = lme.HomographForm;
			Assert.AreEqual(sLexForm, homographForm, "Lexeme form and homograph form are not the same.");

			// These two tests check lexeme forms of the two/three entries.
			// This version of the CollectHomographs method will not include the lme entry.
			List<ILexEntry> rgHomographs = lme.CollectHomographs(homographForm, lme.Hvo);
			Assert.AreEqual(rgHomographs.Count, 2, "Wrong homograph count.");

			// This version of the CollectHomographs method will not include the lme entry.
			rgHomographs = lme.CollectHomographs();
			Assert.AreEqual(rgHomographs.Count, 2, "Wrong homograph count.");

			// This version of the CollectHomographs method will include the lme entry.
			rgHomographs = lme.CollectHomographs(homographForm, 0);
			Assert.AreEqual(rgHomographs.Count, 3, "Wrong homograph count.");

			// Now set the citation form to something different than sLexForm.
			string sCitationForm = "unitTestCitationForm";
			lme.CitationForm.VernacularDefaultWritingSystem = sCitationForm;
			homographForm = lme.HomographForm;
			Assert.AreEqual(sCitationForm, homographForm, "Citation form and homograph form are not the same.");

			// This version of the CollectHomographs method will include the lme entry.
			rgHomographs = lme.CollectHomographs(homographForm, 0);
			Assert.AreEqual(rgHomographs.Count, 1, "Wrong homograph count.");

			// This version of the CollectHomographs method will include the lme entry.
			rgHomographs = lme2.CollectHomographs(sLexForm, 0);
			Assert.AreEqual(rgHomographs.Count, 2, "Wrong homograph count.");
		}

		/// <summary>
		/// Check to see whether homograph validation works.
		/// </summary>
		[Test]
		public void HomographValidationWorks()
		{
			CheckDisposed();

			LexEntry lme = new LexEntry();
			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(lme);
			string sLexForm = "unitTestLexemeForm";
			lme.LexemeFormOA = new MoStemAllomorph();
			lme.LexemeFormOA.Form.VernacularDefaultWritingSystem = sLexForm;

			// Make sure it has 2 homographs.
			LexEntry lme2 = new LexEntry();
			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(lme2);
			lme2.LexemeFormOA = new MoStemAllomorph();
			lme2.LexemeFormOA.Form.VernacularDefaultWritingSystem = sLexForm;
			LexEntry lme3 = new LexEntry();
			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(lme3);
			lme3.LexemeFormOA = new MoStemAllomorph();
			lme3.LexemeFormOA.Form.VernacularDefaultWritingSystem = sLexForm;

			string homographForm = lme.HomographForm;
			Assert.AreEqual(sLexForm, homographForm, "lexeme form and homograph form are not the same.");

			// This version of the CollectHomographs will not include the lme entry.
			List<ILexEntry> rgHomographs = lme.CollectHomographs();
			Assert.AreEqual(rgHomographs.Count, 2, "Wrong homograph count.");

			// Reset all the homograph numbers to zero.
			foreach (ILexEntry le in rgHomographs)
			{
				le.HomographNumber = 0;
			}

			// Restore valid homograph numbers by calling ValidateHomographNumbers.
			bool fOk = LexEntry.ValidateExistingHomographs(rgHomographs);
			Assert.IsFalse(fOk, "Validation had to renumber homographs");
			int n = 1;
			foreach (ILexEntry le in rgHomographs)
			{
				Assert.AreEqual(n++, le.HomographNumber, "Wrong homograph number found.");
			}

			// If we get here without asserting, the renumbering worked okay.
			fOk = LexEntry.ValidateExistingHomographs(rgHomographs);
			Assert.IsTrue(fOk, "Validation should not have to renumber this time.");

			// Reset all the homograph numbers by multiplying each by 2.
			foreach (ILexEntry le in rgHomographs)
			{
				le.HomographNumber *= 2;
			}

			// Restore valid homograph numbers by calling ValidateHomographNumbers.
			fOk = LexEntry.ValidateExistingHomographs(rgHomographs);
			Assert.IsFalse(fOk, "Validation had to renumber homographs");
			n = 1;
			foreach (ILexEntry le in rgHomographs)
			{
				Assert.AreEqual(n++, le.HomographNumber, "Wrong homograph number found.");
			}
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for LingTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LingTests : InMemoryFdoTestBase
	{
		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_inMemoryCache.InitializeLexDb();
		}
		#endregion

		#region Tests
		/// <summary>
		/// </summary>
		[Test]
		public void CheckEndoCentricCompound()
		{
			CheckDisposed();

			MoEndoCompound cmp = (MoEndoCompound)Cache.LangProject.MorphologicalDataOA.CompoundRulesOS.Append(new MoEndoCompound());
			Assert.IsNotNull(cmp.LeftMsaOA);
			Assert.IsNotNull(cmp.RightMsaOA);
		}

		/// <summary>
		/// </summary>
		//[Test]
		public void CheckEnvironmentMessages()
		{
			CheckDisposed();

			string[] saSegments = { "a", "ai", "b", "c", "d", "e", "f", "fl", "fr",
									"í",  // single combined Unicode acute i
									"H"
								};
			string[] saNaturalClasses = { "V", "Vowels", "C", "+son", "+lab, +vd", "+ant, -cor, -vd" };

			PhonEnvRecognizer rec = new PhonEnvRecognizer(saSegments, saNaturalClasses);
			string strRep;
			strRep = "/ _ q";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ q': The phoneme which begins 'q' was not found in the set of representations for any Phoneme.");
			strRep = "/ _ aqa";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ aqa': The phoneme which begins 'qa' was not found in the set of representations for any Phoneme.");
			strRep = "/ _ [COP]";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ [COP]': The abbreviation for the class 'COP' was not found in the set of Natural Classes.");
			strRep = "/ [C][V] _ [V][COP]";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ [C][V] _ [V][COP]': The abbreviation for the class 'COP' was not found in the set of Natural Classes.");
			strRep = "/ [C _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ [C _': There is a missing closing square bracket ']' somewhere around here: '_'.");
			strRep = "/ C] _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ C] _': There is a missing opening square bracket '[' somewhere around here: 'C] _'.");
			strRep = "/ (a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ (a _': There is a missing closing parenthesis ')' somewhere around here: '_'.");
			strRep = "/ a) _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ a) _': There is a missing opening parenthesis '(' somewhere around here: 'a) _'.");
			strRep = " a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string ' a _': There is some kind of error somewhere around here: ' _'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ a ";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ a ': There is some kind of error somewhere around here: ' a '.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ _ a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ a _': There is some kind of error somewhere around here: '_'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ b # a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ b # a _': There is some kind of error somewhere around here: ' a _'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ _ b # a";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ b # a': There is some kind of error somewhere around here: 'a'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ ## a _";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ ## a _': There is some kind of error somewhere around here: ' a _'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
			strRep = "/ _ a ##";
			CheckEnvironmentMessage(rec, strRep, "There is a problem with this environment string '/ _ a ##': There is some kind of error somewhere around here: '#'.  It may be a missing underscore _, two or more underscores, a missing slash /, something beyond a word boundary symbol #, or two or more word boundary symbols at the same edge.");
		}

		private static void CheckEnvironmentMessage(PhonEnvRecognizer rec, string strRep, string sExpected)
		{
			if (rec.Recognize(strRep))
				Assert.Fail("Environment '" + strRep + "' should fail, but did not");
			else
			{
				int pos;
				string sMessage;
				PhEnvironment.CreateErrorMessageFromXml(strRep, rec.ErrorMessage, out pos, out sMessage);
				Assert.AreEqual(sExpected, sMessage);
			}
		}
		/// <summary>
		/// </summary>
		[Test]
		public void CheckExoCentricCompound()
		{
			CheckDisposed();

			MoExoCompound cmp = (MoExoCompound)Cache.LangProject.MorphologicalDataOA.CompoundRulesOS.Append(new MoExoCompound());
			Assert.IsNotNull(cmp.LeftMsaOA);
			Assert.IsNotNull(cmp.RightMsaOA);
			Assert.IsNotNull(cmp.ToMsaOA);
		}

		/// <summary>
		/// Check the merging MSAs, when two entries are merged.
		/// </summary>
		[Test]
		public void MergeEntryMSAs()
		{
			CheckDisposed();

			ILexDb ldb = Cache.LangProject.LexDbOA;
			FdoOwningSequence<ICmPossibility> posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());
			FdoOwningCollection<ILexEntry> entriesCol = ldb.EntriesOC;
			ILexEntry lmeKeeper = entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = entriesCol.Add(new LexEntry());

			try
			{
				// Set up stem MSAs.
				MoStemMsa stemKeeper = (MoStemMsa)lmeKeeper.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
				stemKeeper.PartOfSpeechRA = pos;
				MoStemMsa stemToss = (MoStemMsa)lmeSrc.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
				stemToss.PartOfSpeechRA = pos;
				MoStemMsa stemKeep = (MoStemMsa)lmeSrc.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
				stemKeep.PartOfSpeechRA = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());

				// Set up deriv affix MSAs.
				MoDerivAffMsa daKeeper = (MoDerivAffMsa)lmeKeeper.MorphoSyntaxAnalysesOC.Add(new MoDerivAffMsa());
				daKeeper.FromPartOfSpeechRA = pos;
				daKeeper.ToPartOfSpeechRA = pos;
				MoDerivAffMsa daToss = (MoDerivAffMsa)lmeSrc.MorphoSyntaxAnalysesOC.Add(new MoDerivAffMsa());
				daToss.FromPartOfSpeechRA = pos;
				daToss.ToPartOfSpeechRA = pos;

				// Set up inflectional affix MSAs.
				MoInflAffMsa iaKeeper = (MoInflAffMsa)lmeKeeper.MorphoSyntaxAnalysesOC.Add(new MoInflAffMsa());
				iaKeeper.PartOfSpeechRA = pos;
				MoInflAffMsa iaToss = (MoInflAffMsa)lmeSrc.MorphoSyntaxAnalysesOC.Add(new MoInflAffMsa());
				iaToss.PartOfSpeechRA = pos;

				// Merge entries.
				lmeKeeper.MergeObject(lmeSrc);
				Assert.AreEqual(4, lmeKeeper.MorphoSyntaxAnalysesOC.Count);
			}
			finally
			{
				entriesCol.Remove(lmeKeeper);
			}
		}

		/// <summary>
		/// Check the merging allomorphs, when two entries are merged.
		/// </summary>
		[Test]
		public void MergeEntryAllomorphs()
		{
			CheckDisposed();

			ILexDb ldb = Cache.LangProject.LexDbOA;
			MoMorphTypeCollection mtCol = new MoMorphTypeCollection(Cache);
			string form = "cat";
			int clsid;
			IMoMorphType stemMT = MoMorphType.FindMorphType(Cache, mtCol, ref form, out clsid);
			form = "s-";
			IMoMorphType pfxMT = MoMorphType.FindMorphType(Cache, mtCol, ref form, out clsid);
			FdoOwningCollection<ILexEntry> entriesCol = ldb.EntriesOC;
			ILexEntry lmeKeeper = entriesCol.Add(new LexEntry());
			ILexEntry lmeSrc = entriesCol.Add(new LexEntry());

			try
			{
				// Set up stem allomorphs.
				MoStemAllomorph stemKeeper = (MoStemAllomorph)lmeKeeper.AlternateFormsOS.Append(new MoStemAllomorph());
				stemKeeper.MorphTypeRA = stemMT;
				stemKeeper.Form.VernacularDefaultWritingSystem = "cat";
				MoStemAllomorph stemToss = (MoStemAllomorph)lmeSrc.AlternateFormsOS.Append(new MoStemAllomorph());
				stemToss.MorphTypeRA = stemMT;
				stemToss.Form.VernacularDefaultWritingSystem = "cat";
				MoStemAllomorph stemKeep = (MoStemAllomorph)lmeSrc.AlternateFormsOS.Append(new MoStemAllomorph());
				stemKeep.MorphTypeRA = stemMT;
				stemKeep.Form.VernacularDefaultWritingSystem = "meow";

				// Set up affix allomorphs.
				MoAffixAllomorph daKeeper = (MoAffixAllomorph)lmeKeeper.AlternateFormsOS.Append(new MoAffixAllomorph());
				daKeeper.MorphTypeRA = pfxMT;
				daKeeper.Form.VernacularDefaultWritingSystem = "s";
				MoAffixAllomorph daToss = (MoAffixAllomorph)lmeSrc.AlternateFormsOS.Append(new MoAffixAllomorph());
				daToss.MorphTypeRA = pfxMT;
				daToss.Form.VernacularDefaultWritingSystem = "s";

				// Merge entries.
				lmeKeeper.MergeObject(lmeSrc);
				Assert.AreEqual(3, lmeKeeper.AlternateFormsOS.Count);
			}
			finally
			{
				entriesCol.Remove(lmeKeeper);
			}
		}

		/// <summary>
		/// Used with exporting to get, e.g. ("-is, -iz")
		/// </summary>
		[Test]
		public void MoForm_NamesWithMarkers()
		{
			FdoReferenceSequence<ILgWritingSystem> systems = Cache.LangProject.CurVernWssRS;
			ILexDb ld = Cache.LangProject.LexDbOA;
		   ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);
			IMoForm m = MoForm.MakeMorph(Cache, le, StringUtils.MakeTss("-is", Cache.DefaultVernWs));
			Assert.AreEqual("is", m.Form.GetAlternative(systems[0].Hvo));
			m.Form.SetAlternative("iz", systems[1].Hvo);
			Dictionary<string, string> markers = ((MoForm)m).NamesWithMarkers;
			Assert.AreEqual("-is", markers[systems[0].Abbreviation]);
			Assert.AreEqual("-iz", markers[systems[1].Abbreviation]);
			Assert.AreEqual(2, markers.Count);
		}

		/// <summary>
		/// Smoke Test various methods which just load specific groups of objects more efficiently.
		/// These are used from FieldWorks XML template files.
		/// </summary>
		[Test]
		public void LoadSpecificGroup()
		{
			CheckDisposed();

			FdoObjectSet<IPartOfSpeech> poses;
			poses = Cache.LangProject.AllPartsOfSpeech;
			FdoObjectSet<IMoForm> forms;
			forms = Cache.LangProject.LexDbOA.AllAllomorphs;
			FdoObjectSet<IMoMorphSynAnalysis> msas;
			msas = Cache.LangProject.LexDbOA.AllMSAs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which test that a new PhPhoneme comes with a PhCode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePhPhonemeWithPhCode()
		{
			CheckDisposed();

			IPhPhoneme phone = new PhPhoneme();
			IPhPhonemeSet ps = Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Append(new PhPhonemeSet());
			ps.PhonemesOC.Add(phone);
			Assert.IsTrue((phone.CodesOS != null) && (phone.CodesOS.Count > 0), "PhPhoneme should have at least one PhCode");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which test that a new PhPhoneme comes with a PhCode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BasicIPASymbolSetsDescription()
		{
			CheckDisposed();

			// initialize local variables
			bool fJustChangedDescription = false;
			XmlDocument myIPAMapperDocument = InitBasicIPAXMlMapperDocument();
			const string ksPEnglishDescription = "Voiceless bilabial plosive";
			const string ksPSpanishDescription = "Plosivo bilabial sordo";

			// create a new phoneme
			IPhPhoneme phone = new PhPhoneme();
			IPhPhonemeSet ps = Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Append(new PhPhonemeSet());
			ps.PhonemesOC.Add(phone);


			//Put an unknown value in BasicIPASymbol
			phone.BasicIPASymbol.Text = "Nonexistent";
			// description is blank, so we should get the value from the mapper file
			fJustChangedDescription = phone.SetDescriptionBasedOnIPA(myIPAMapperDocument, fJustChangedDescription);
			CheckPhonemeDescriptionNewValues(phone, null, null, "description is blank, but there is nothing in the file for this symbol");
			Assert.IsFalse(fJustChangedDescription, "We did not change the description");

			//Put a known value in BasicIPASymbol
			phone.BasicIPASymbol.Text = "q";
			// description is blank, so we should get the value from the mapper file
			fJustChangedDescription = phone.SetDescriptionBasedOnIPA(myIPAMapperDocument, fJustChangedDescription);
			CheckPhonemeDescriptionNewValues(phone, "Voiceless uvular plosive", "Plosivo uvular sordo", "description is blank, so we should get the value from the mapper file");
			Assert.IsTrue(fJustChangedDescription, "We did just change the description");

			//Change value of symbol
			phone.BasicIPASymbol.Text = "p";
			// we have just changed it, so we should get the new value from the mapper file
			fJustChangedDescription = phone.SetDescriptionBasedOnIPA(myIPAMapperDocument, fJustChangedDescription);
			CheckPhonemeDescriptionNewValues(phone, ksPEnglishDescription, ksPSpanishDescription,"basic ipa symbol changed while still 'editting', so we should get the value from the mapper file");
			Assert.IsTrue(fJustChangedDescription, "We did just change the description");

			phone.BasicIPASymbol.Text = "q";
			// description is already set, so should not change.
			fJustChangedDescription = phone.SetDescriptionBasedOnIPA(myIPAMapperDocument, false);
			CheckPhonemeDescriptionNewValues(phone, ksPEnglishDescription, ksPSpanishDescription, "descriptions should not change");
			Assert.IsFalse(fJustChangedDescription, "We did not just change the description");

			// Set basicIPASymbol to blank
			phone.BasicIPASymbol.Text = "";
			// now the description should become blank
			fJustChangedDescription = phone.SetDescriptionBasedOnIPA(myIPAMapperDocument, true);
			CheckPhonemeDescriptionNewValues(phone, null, null, "description should be blank now");
			Assert.IsTrue(fJustChangedDescription, "We did just change the description");
		}

		private void CheckPhonemeDescriptionNewValues(IPhPhoneme phone, string sEnglishDescription, string sSpanishDescription, string sMessage)
		{
			bool fFoundEnglish = false;
			bool fFoundSpanish = false;
			foreach (ILgWritingSystem writingSystem in Cache.LangProject.AnalysisWssRC)
			{
				if (writingSystem.ICULocale == "en")
				{
					fFoundEnglish = true;
					Assert.AreEqual(sEnglishDescription, phone.Description.GetAlternative(writingSystem.Hvo).Text, sMessage);
				}
				else if (writingSystem.ICULocale == "es")
				{
					fFoundSpanish = true;
					Assert.AreEqual(sSpanishDescription, phone.Description.GetAlternative(writingSystem.Hvo).Text, sMessage);
				}
			}
			Assert.IsTrue(fFoundEnglish, "English description changed");
			Assert.IsTrue(fFoundSpanish, "Spanish description changed");
		}
#if NotUsingInMemoryTests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which test that a new PhPhoneme comes with a PhCode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BasicIPASymbolSetsFeatures()
		{
			CheckDisposed();

			const string ksConsonantal = "fPAConsonantal";
			const string ksConsonantalPositive = "vPAConsonantalPositive";
			const string ksConsonantalNegative = "vPAConsonantalNegative";
			const string ksSonorant = "fPASonorant";
			const string ksSonorantPositive = "vPASonorantPositive";
			const string ksSonorantNegative = "vPASonorantNegative";
			const string ksSyllabic = "fPASyllabic";
			const string ksSyllabicPositive = "vPASyllabicPositive";
			const string ksSyllabicNegative = "vPASyllabicNegative";

			// initialize local variables
			bool fJustChangedFeatures = false;
			XmlDocument myIPAMapperDocument = InitBasicIPAXMlMapperDocument();

			// create a new phoneme
			IPhPhoneme phone = new PhPhoneme();
			IPhPhonemeSet ps = Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Append(new PhPhonemeSet());
			ps.PhonemesOC.Add(phone);

			//Put a known value in BasicIPASymbol
			phone.BasicIPASymbol.Text = "b";
			// no phonological features in system, so nothing should be added to phone.FeaturesOA
			fJustChangedFeatures = phone.SetFeaturesBasedOnIPA(myIPAMapperDocument, false);
			Assert.IsNull(phone.FeaturesOA, "no features in system, so adding doesn't do anything");
			Assert.IsFalse(fJustChangedFeatures, "We did not change any features");

			// Add some features to feature system
			IFsFeatureSystem featSystem = new FsFeatureSystem();
			Cache.LangProject.PhFeatureSystemOA = featSystem;
			XmlDocument phonFeatList = new XmlDocument();
			string sXmlFile = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\MGA\GlossLists\PhonFeatsEticGlossList.xml");
			phonFeatList.Load(sXmlFile);
			CreateNewPhonFeature(featSystem, phonFeatList, ksConsonantal);
			CreateNewPhonFeature(featSystem, phonFeatList, ksSonorant);
			CreateNewPhonFeature(featSystem, phonFeatList, ksSyllabic);

			// now we should get some features
			fJustChangedFeatures = phone.SetFeaturesBasedOnIPA(myIPAMapperDocument, false);
			Assert.IsNotNull(phone.FeaturesOA, "have something now");
			Assert.AreEqual(3, phone.FeaturesOA.FeatureSpecsOC.Count, "expect three features to be added");
			CheckFeatureValues(phone, ksConsonantal, ksConsonantalPositive, ksSonorant, ksSonorantNegative, ksSyllabic, ksSyllabicNegative);
			Assert.IsTrue(fJustChangedFeatures, "We just did change features");


			//Change value of symbol
			phone.BasicIPASymbol.Text = "a";
			// we have just changed it, so we should get the new value from the mapper file
			fJustChangedFeatures = phone.SetFeaturesBasedOnIPA(myIPAMapperDocument, true);
			Assert.IsNotNull(phone.FeaturesOA, "have something now");
			Assert.AreEqual(3, phone.FeaturesOA.FeatureSpecsOC.Count, "expect three features to be added");
			CheckFeatureValues(phone, ksConsonantal, ksConsonantalNegative, ksSonorant, ksSonorantPositive, ksSyllabic, ksSyllabicPositive);
			Assert.IsTrue(fJustChangedFeatures, "We did just change the description");

			// Set basicIPASymbol to blank
			phone.BasicIPASymbol.Text = "";
			// now the description should become blank
			fJustChangedFeatures = phone.SetFeaturesBasedOnIPA(myIPAMapperDocument, true);
			Assert.IsNull(phone.FeaturesOA, "no features in system, so adding doesn't do anything");
			Assert.IsTrue(fJustChangedFeatures, "We did just change the description");
		}

		private void CheckFeatureValues(IPhPhoneme phone, string ksConsonantal, string ksConsonantalValue, string ksSonorant, string ksSonorantValue, string ksSyllabic, string ksSyllabicValue)
		{
			foreach (IFsClosedValue spec in phone.FeaturesOA.FeatureSpecsOC)
			{
				Assert.IsNotNull(spec);
				if (spec.FeatureRA.CatalogSourceId == ksConsonantal)
					Assert.AreEqual(ksConsonantalValue, spec.ValueRA.CatalogSourceId);
				else if (spec.FeatureRA.CatalogSourceId == ksSonorant)
					Assert.AreEqual(ksSonorantValue, spec.ValueRA.CatalogSourceId);
				else if (spec.FeatureRA.CatalogSourceId == ksSyllabic)
					Assert.AreEqual(ksSyllabicValue, spec.ValueRA.CatalogSourceId);
				else
				{
					Assert.Fail("Did not one of the expected features in the feature structure!  Found: [" + spec.FeatureRA.CatalogSourceId + ":" + spec.ValueRA.CatalogSourceId + "]");
				}
			}
		}

		private void CreateNewPhonFeature(IFsFeatureSystem featSystem, XmlDocument phonFeatList, string sFeatureId)
		{
			XmlNode item = phonFeatList.SelectSingleNode("//item[@id='" + sFeatureId + "']");

			// Since phonological features in the chooser only have features and no values,
			// we need to create the positive and negative value nodes
			string sName = XmlUtils.GetManditoryAttributeValue(item, "id");
			const string sTemplate =
				"<item id='v{0}Positive' type='value'><abbrev ws='en'>+</abbrev><term ws='en'>positive</term>" +
				"<fs id='v{0}PositiveFS' type='Phon'><f name='{0}'><sym value='+'/></f></fs></item>" +
				"<item id='v{0}Negative' type='value'><abbrev ws='en'>-</abbrev><term ws='en'>negative</term>" +
				"<fs id='v{0}NegativeFS' type='Phon'><f name='{0}'><sym value='-'/></f></fs></item>";
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(sTemplate, sName.Substring(1));
			item.InnerXml += sb.ToString();
			// have to use a ndw document or, for some odd reason, it keeps on using an old value and not the new one...
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(item.OuterXml);
			// add positive value; note that the FsFeatDefn will be the same for both
			XmlNode valueNode = doc.SelectSingleNode("//item[contains(@id,'Positive')]");
			FsFeatureSystem.AddFeatureAsXml(Cache, featSystem, valueNode);
			// add negative value
			valueNode = doc.SelectSingleNode("//item[contains(@id,'Negative')]");
			FsFeatureSystem.AddFeatureAsXml(Cache, featSystem, valueNode);
		}
#endif
		private XmlDocument InitBasicIPAXMlMapperDocument()
		{
			XmlDocument myIPAMapperDocument;
			myIPAMapperDocument = new XmlDocument();
			string sIPAMapper = Path.Combine(DirectoryFinder.TemplateDirectory, PhPhoneme.ksBasicIPAInfoFile);
			myIPAMapperDocument.Load(sIPAMapper);
			return myIPAMapperDocument;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test that ShortName for optional slots have parentheses and non-optional do not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InflAffixSlotShortName()
		{
			CheckDisposed();

			MoInflAffixSlot slot = new MoInflAffixSlot();
			PartOfSpeech pos = (PartOfSpeech)Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(new PartOfSpeech());
			pos.AffixSlotsOC.Add(slot);
			slot.Optional = false;
			string sName = "TestSlot";
			slot.Name.AnalysisDefaultWritingSystem = sName;
			Assert.AreEqual(sName, slot.ShortName);
			slot.Optional = true;
			Assert.AreEqual("(" + sName + ")", slot.ShortName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which produces a citation form with markers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CitationFormWithAffixType()
		{
			CheckDisposed();

			LexEntry entry = new LexEntry ();
			Cache.LangProject.LexDbOA.EntriesOC.Add(entry);
			string sULForm = "abc";
			entry.CitationForm.VernacularDefaultWritingSystem = sULForm;
			MoAffixAllomorph allomorph = new MoAffixAllomorph();
			entry.LexemeFormOA = allomorph;
			Set<ICmPossibility> morphTypes = Cache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities;
			foreach (IMoMorphType mmt in morphTypes)
			{
				allomorph.MorphTypeRAHvo = mmt.Hvo;
				switch (MoMorphType.FindMorphTypeIndex(Cache, mmt))
				{
					case MoMorphType.kmtBoundRoot:
						Assert.AreEqual("*" + sULForm, entry.CitationFormWithAffixType, "Expected * prefix for bound root with CF");
						break;

					case MoMorphType.kmtBoundStem:
						Assert.AreEqual("*" + sULForm, entry.CitationFormWithAffixType, "Expected * prefix for bound stem with CF");
						break;

					case MoMorphType.kmtCircumfix:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for circumfix");
						break;

					case MoMorphType.kmtClitic:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for clitic");
						break;

					case MoMorphType.kmtEnclitic:
						Assert.AreEqual("=" + sULForm, entry.CitationFormWithAffixType, "Expected = prefix for enclitic");
						break;

					case MoMorphType.kmtInfix:
						Assert.AreEqual("-" + sULForm + "-", entry.CitationFormWithAffixType, "Expected - prefix and - postfix for infix");
						break;

					case MoMorphType.kmtInfixingInterfix:
						Assert.AreEqual("-" + sULForm + "-", entry.CitationFormWithAffixType, "Expected - prefix and - postfix for infixing interfix");
						break;

					case MoMorphType.kmtMixed:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for Mixed");
						break;

					case MoMorphType.kmtParticle:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for particle");
						break;

					case MoMorphType.kmtPrefix:
						Assert.AreEqual(sULForm + "-", entry.CitationFormWithAffixType, "Expected - postfix for prefix");
						break;

					case MoMorphType.kmtPrefixingInterfix:
						Assert.AreEqual(sULForm + "-", entry.CitationFormWithAffixType, "Expected - postfix for prefixing interfix");
						break;

					case MoMorphType.kmtProclitic:
						Assert.AreEqual(sULForm + "=", entry.CitationFormWithAffixType, "Expected = postfix for proclitic");
						break;

					case MoMorphType.kmtRoot:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for root");
						break;

					case MoMorphType.kmtSimulfix:
						Assert.AreEqual("=" + sULForm + "=", entry.CitationFormWithAffixType, "Expected = prefix and = postfix for simulfix");
						break;

					case MoMorphType.kmtStem:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for stem");
						break;

					case MoMorphType.kmtSuffix:
						Assert.AreEqual("-" + sULForm, entry.CitationFormWithAffixType, "Expected - prefix for suffix");
						break;

					case MoMorphType.kmtSuffixingInterfix:
						Assert.AreEqual("-" + sULForm, entry.CitationFormWithAffixType, "Expected - prefix for suffixing interfix");
						break;

					case MoMorphType.kmtSuprafix:
						Assert.AreEqual("~" + sULForm + "~", entry.CitationFormWithAffixType, "Expected ~ prefix and ~ postfix for suprafix");
						break;

					case MoMorphType.kmtPhrase:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for phrase");
						break;

					case MoMorphType.kmtDiscontiguousPhrase:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for discontiguous phrase");
						break;

					case MoMorphType.kmtUnknown:
						Assert.AreEqual(sULForm, entry.CitationFormWithAffixType, "Expected no prefix or postfix for Unknown");
						break;
				}
			}
		}

		/// <summary>
		/// Test POS Requires Inflection.
		/// </summary>
		/// <remarks>It seems to me (EberhardB) that this basically tests the data stored
		/// in TestLangProj rather then the code. The test didn't verify that there were
		/// any of the expected POS in the database, so it green-bared even with an empty
		/// database (which is the case now with InMemoryFdoTestBase).</remarks>
		[Test]
		[Ignore("Needs refactoring - test tests data in the database rather then code")]
		public void POSRequiresInflection()
		{
			CheckDisposed();

			// need to setup expected test data
			int[] hvos = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.HvoArray;
			Assert.IsTrue(hvos.Length > 0);
			foreach (int hvo in hvos)
			{
				IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(Cache, hvo);
				switch (pos.Name.AnalysisDefaultWritingSystem)
				{
					case "adjunct":
						Assert.IsFalse(pos.RequiresInflection(), "adjunct does not require inflection");
						break;
					case "marker":
						VerifyRequiresInflectionOnSubCategory(pos, "aspectual", false);
						break;
					case "noun":
						Assert.IsTrue(pos.RequiresInflection(), "noun requires inflection");
						VerifyRequiresInflectionOnSubCategory(pos, "common noun", true);
						break;
					case "relator":
						VerifyRequiresInflectionOnSubSubCategory(pos, "adposition", "preposition", false);
						break;
				}
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which tells us whether the inflection classes field is relevant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InflectionClassIsRelevant()
		{
			CheckDisposed();

			LexEntry entry = new LexEntry ();
			Cache.LangProject.LexDbOA.EntriesOC.Add(entry);
			MoAffixAllomorph allomorph = new MoAffixAllomorph();
			entry.AlternateFormsOS.Append(allomorph);
			Assert.IsFalse(allomorph.IsFieldRelevant((int)MoAffixForm.MoAffixFormTags.kflidInflectionClasses),
				"InflectionClass should not be relevant until an inflectional affix MSA with a category has been added.");
			MoInflAffMsa orange = new MoInflAffMsa();
			entry.MorphoSyntaxAnalysesOC.Add(orange);
			Assert.IsFalse(allomorph.IsFieldRelevant((int)MoAffixForm.MoAffixFormTags.kflidInflectionClasses),
				"InflectionClass should not be relevant until an inflectional affix MSA with a category has been added.");
			PartOfSpeech pos = new PartOfSpeech();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(pos);
			orange.PartOfSpeechRA = pos;
			Assert.IsTrue(allomorph.IsFieldRelevant((int)MoAffixForm.MoAffixFormTags.kflidInflectionClasses),
				"InflectionClass should now be relevant since an inflectional affix MSA with a category has been added.");

		}

				/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which tells us whether the inflection classes field is relevant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CliticVsStemMsaWithoutCategory()
		{
			CheckDisposed();

			LexEntry entry = new LexEntry();
			Cache.LangProject.LexDbOA.EntriesOC.Add(entry);
			MoStemAllomorph allomorph = new MoStemAllomorph();
			entry.LexemeFormOA = allomorph;
			MoStemMsa msa = new MoStemMsa();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			SetMorphType(allomorph, MoMorphType.kguidMorphClitic);
			Assert.AreEqual("Clitic of unknown category", msa.LongNameTs.Text);
			MoStemAllomorph allo2 = new MoStemAllomorph();
			entry.AlternateFormsOS.Append(allo2);
			SetMorphType(allomorph, MoMorphType.kguidMorphStem);
			Assert.AreEqual("Stem/root of unknown category; takes any affix", msa.LongNameTs.Text);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which tells us whether the inflection classes field is relevant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FromPartsOfSpeechIsRelevant()
		{
			CheckDisposed();

			LexEntry entry = new LexEntry();
			Cache.LangProject.LexDbOA.EntriesOC.Add(entry);
			MoStemAllomorph allomorph = new MoStemAllomorph();
			entry.AlternateFormsOS.Append(allomorph);
			MoStemMsa msa = new MoStemMsa();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			SetMorphType(allomorph, MoMorphType.kguidMorphStem);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphProclitic);
			Assert.IsTrue(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should now be relevant since the entry has a proclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphEnclitic);
			Assert.IsTrue(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should now be relevant since the entry has an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphClitic);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphBoundRoot);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphBoundStem);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphCircumfix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphDiscontiguousPhrase);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphInfix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphInfixingInterfix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphParticle);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphPhrase);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphPrefix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphPrefixingInterfix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphRoot);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphSimulfix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphSuffix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphSuffixingInterfix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");
			SetMorphType(allomorph, MoMorphType.kguidMorphSuprafix);
			Assert.IsFalse(msa.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidFromPartsOfSpeech),
				"FromPartsOfSpeech should not be relevant until the entry contains a proclitic or an enclitic.");

		}

		private void SetMorphType(MoStemAllomorph allomorph, string sType)
		{
			foreach (MoMorphType mt in Cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				if (mt.Guid.ToString() == sType)
				{
					allomorph.MorphTypeRA = mt;
					break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test the method which tells us whether inflection class is relevant for compound rules
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InflectionClassInCompoundStemMsasIsRelevant()
		{
			CheckDisposed();

			MoExoCompound compound = new MoExoCompound();
			Cache.LangProject.MorphologicalDataOA.CompoundRulesOS.Append(compound);
			MoStemMsa msaLeft = new MoStemMsa();
			MoStemMsa msaRight = new MoStemMsa();
			MoStemMsa msaTo = new MoStemMsa();
			compound.LeftMsaOA = msaLeft;
			compound.RightMsaOA = msaRight;
			compound.ToMsaOA = msaTo;
			Assert.IsFalse(msaLeft.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidInflectionClass),
				"Inflection Class should not be relevant for LeftMsa.");
			Assert.IsFalse(msaRight.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidInflectionClass),
				"Inflection Class should not be relevant for RightMsa.");
			Assert.IsFalse(msaTo.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidInflectionClass),
				"Inflection Class should not be relevant for ToMsa if it does not have a category.");
			PartOfSpeech pos = new PartOfSpeech();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(pos);
			msaTo.PartOfSpeechRA = pos;
			Assert.IsTrue(msaTo.IsFieldRelevant((int)MoStemMsa.MoStemMsaTags.kflidInflectionClass),
				"Inflection Class should be relevant for ToMsa when it has a category.");
		}


		#endregion Tests

		#region broken tests

		/// <summary>
		/// </summary>
		[Test]
		[Ignore("Broken until recursive reference Targets is reimplemented (JH).")]
		public void RefTargCandOnMoStemMsa()
		{
			CheckDisposed();

			MoStemMsa msa = GetFirstMoStemMsa();
			Set<int> hvos = msa.ReferenceTargetCandidates((int)MoStemMsa.MoStemMsaTags.kflidPartOfSpeech);
			ObjectLabelCollection items = new ObjectLabelCollection(Cache, hvos);
			Assert.AreEqual(items.Count, Cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities.Count, "Wrong count");
		}

		#endregion broken tests

		#region Misc methods

		/// <summary>
		/// </summary>
		protected MoStemMsa GetFirstMoStemMsa()
		{
			int[] hvos = GetHvosForFirstNObjectsOfClass("MoStemMsa", 1);
			return (MoStemMsa)CmObject.CreateFromDBObject(Cache, hvos[0]);

		}

		private void VerifyRequiresInflectionOnSubSubCategory(IPartOfSpeech pos, string sPOSName, string sPOSSubCatName, bool fExpectTrue)
		{
			int[] hvos = pos.SubPossibilitiesOS.HvoArray;
			Assert.IsTrue(hvos.Length > 0);
			foreach (int hvo in hvos)
			{
				pos = PartOfSpeech.CreateFromDBObject(Cache, hvo);
				if (pos.Name.AnalysisDefaultWritingSystem == sPOSName)
					VerifyRequiresInflectionOnSubCategory(pos, sPOSSubCatName, fExpectTrue);
			}
		}
		private void VerifyRequiresInflectionOnSubCategory(IPartOfSpeech pos, string sPOSName, bool fExpectTrue)
		{
			int[] hvos = pos.SubPossibilitiesOS.HvoArray;
			Assert.IsTrue(hvos.Length > 0);
			foreach (int hvo in hvos)
			{
				pos = PartOfSpeech.CreateFromDBObject(Cache, hvo);
				if (pos.Name.AnalysisDefaultWritingSystem == sPOSName)
				{
					if (fExpectTrue)
						Assert.IsTrue(pos.RequiresInflection(), sPOSName + " requires inflection");
					else
						Assert.IsFalse(pos.RequiresInflection(), sPOSName + " does not require inflection");
				}
			}
		}

		/// <summary>
		/// </summary>
		[Test]
		public void LexemeFormStemAllomorphIsRelevant()
		{
			LexEntry stemEntry = new LexEntry ();
			Cache.LangProject.LexDbOA.EntriesOC.Add(stemEntry);
			MoStemAllomorph stemAllomorph = new MoStemAllomorph();
			stemEntry.LexemeFormOA = stemAllomorph;
			Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant when there is no morph type.");
			foreach (IMoMorphType mmt in Cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				stemAllomorph.MorphTypeRA = mmt;
				switch (mmt.Guid.ToString())
				{
					case MoMorphType.kguidMorphRoot:
						Assert.IsTrue(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should be relevant for a root morph type.");
						break;
					case MoMorphType.kguidMorphBoundRoot:
						Assert.IsTrue(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should be relevant for a bound root morph type.");
						break;
					case MoMorphType.kguidMorphStem:
						Assert.IsTrue(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should be relevant for a stem morph type.");
						break;
					case MoMorphType.kguidMorphBoundStem:
						Assert.IsTrue(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should be relevant for a bound stem morph type.");
						break;
					case MoMorphType.kguidMorphCircumfix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a circumfix morph type.");
						break;
					case MoMorphType.kguidMorphDiscontiguousPhrase:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a discontiguous phrasemorph type.");
						break;
					case MoMorphType.kguidMorphClitic:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a clitic morph type.");
						break;
					case MoMorphType.kguidMorphEnclitic:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for an enclitic morph type.");
						break;
					case MoMorphType.kguidMorphInfix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for an infix morph type.");
						break;
					case MoMorphType.kguidMorphInfixingInterfix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for an infixing interfix morph type.");
						break;
					case MoMorphType.kguidMorphParticle:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a particle morph type.");
						break;
					case MoMorphType.kguidMorphPhrase:
						Assert.IsTrue(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should be relevant for a phrase morph type.");
						break;
					case MoMorphType.kguidMorphPrefix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a prefix morph type.");
						break;
					case MoMorphType.kguidMorphPrefixingInterfix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a prefixing interfix morph type.");
						break;
					case MoMorphType.kguidMorphProclitic:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a proclitic morph type.");
						break;
					case MoMorphType.kguidMorphSimulfix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a simulfix morph type.");
						break;
					case MoMorphType.kguidMorphSuffix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a suffix morph type.");
						break;
					case MoMorphType.kguidMorphSuffixingInterfix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a suffixing interfix morph type.");
						break;
					case MoMorphType.kguidMorphSuprafix:
						Assert.IsFalse(stemAllomorph.IsFieldRelevant((int)MoStemAllomorph.MoStemAllomorphTags.kflidStemName),
							"Stem allomorph stem name should not be relevant for a suprafix morph type.");
						break;
				}
			}
		}

		private void DoTestLexemeFormAffixAllomorphIsRelevant()
		{
			LexEntry affixEntry = new LexEntry ();
			Cache.LangProject.LexDbOA.EntriesOC.Add(affixEntry);
			MoAffixAllomorph affixAllomorph = new MoAffixAllomorph();
			affixEntry.LexemeFormOA = affixAllomorph;
			Assert.IsFalse(affixAllomorph.IsFieldRelevant((int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPosition),
				"Affix allomorph position should not be relevant in an underlying form.");
			Assert.IsFalse(affixAllomorph.IsFieldRelevant((int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPhoneEnv),
				"Affix allomorph environment should not be relevant in an underlying form.");
			MoInflAffMsa iamsa = new MoInflAffMsa();
			affixEntry.MorphoSyntaxAnalysesOC.Add(iamsa);
			Assert.IsFalse(affixAllomorph.IsFieldRelevant((int)MoAffixAllomorph.MoAffixFormTags.kflidInflectionClasses),
				"Affix inflection class(es) should not be relevant in an underlying form.");
		}

		#endregion Misc methods
	}

	#endregion LingTests

	#region MsaCleanupTests

	/// <summary>
	/// Test the various ways that Msas need to be cleaned up when deleting referencing objects.
	/// </summary>
	[TestFixture]
	public class MsaCleanupTests: InMemoryFdoTestBase
	{
		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_inMemoryCache.InitializeLexDb();
		}
		#endregion
		/// <summary>
		/// Check that deleting a LexSense also deletes any MSA that was referred to only by that
		/// LexSense.
		/// </summary>
		[Test]
		public void DeleteLexSense()
		{
			CheckDisposed();

			ILexDb ld = Cache.LangProject.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);
			Assert.AreEqual(1, le.SensesOS.Count);
			LexSense ls = le.SensesOS[0] as LexSense;

			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			int hvoMsa = le.MorphoSyntaxAnalysesOC.HvoArray[0];

			Assert.AreEqual(hvoMsa, ls.MorphoSyntaxAnalysisRAHvo);

			ls.DeleteUnderlyingObject();
			Assert.AreEqual(0, le.SensesOS.Count);
			Assert.AreEqual(0, le.MorphoSyntaxAnalysesOC.Count);
		}

		/// <summary>
		/// Check that changing an MSA on the LexSense also deletes any MSA that was referred to only by that
		/// LexSense.
		/// </summary>
		[Test]
		public void ChangeMsaOnLexSense()
		{
			CheckDisposed();

			ILexDb ld = Cache.LangProject.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);
			Assert.AreEqual(1, le.SensesOS.Count);
			LexSense ls = le.SensesOS[0] as LexSense;

			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			int hvoMsa = le.MorphoSyntaxAnalysesOC.HvoArray[0];
			Assert.AreEqual(hvoMsa, ls.MorphoSyntaxAnalysisRAHvo);

			// Add new MSA to LexEntry
			MoMorphSynAnalysis msa = new MoStemMsa();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			Assert.AreEqual(2, le.MorphoSyntaxAnalysesOC.Count);
			// Change the LexSense MSA
			ls.MorphoSyntaxAnalysisRA = msa;
			Assert.AreEqual(msa.Hvo, ls.MorphoSyntaxAnalysisRAHvo);
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			Assert.AreEqual(msa.Hvo, le.MorphoSyntaxAnalysesOC.HvoArray[0]);
		}

		/// <summary>
		/// Check that deleting a WfiMorphBundle also deletes any MSA that was referred to only
		/// by that WfiMorphBundle.
		/// </summary>
		[Test]
		public void DeleteWfiMorphBundle()
		{
			CheckDisposed();

			ILexDb ld = Cache.LangProject.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);
			Assert.AreEqual(1, le.SensesOS.Count);
			LexSense ls = le.SensesOS[0] as LexSense;
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			int hvoMsa = le.MorphoSyntaxAnalysesOC.HvoArray[0];
			Assert.AreEqual(hvoMsa, ls.MorphoSyntaxAnalysisRAHvo);

			// Setup WfiMorphBundle
			IWfiWordform wf = Cache.LangProject.WordformInventoryOA.WordformsOC.Add(new WfiWordform());
			IWfiAnalysis anal = wf.AnalysesOC.Add(new WfiAnalysis());
			IWfiMorphBundle wmb = anal.MorphBundlesOS.Append(new WfiMorphBundle());

			MoStemAllomorph bearNForm = (MoStemAllomorph)le.AlternateFormsOS.Append(new MoStemAllomorph());
			bearNForm.Form.VernacularDefaultWritingSystem = "bearNTEST";
			wmb.MorphRA = bearNForm;
			wmb.MsaRA = ls.MorphoSyntaxAnalysisRA;

			// Delete our LexSense
			ls.DeleteUnderlyingObject();
			Assert.AreEqual(0, le.SensesOS.Count);
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);

			// Delete our WfiMorphBundle
			wmb.DeleteUnderlyingObject();
			Assert.AreEqual(0, le.MorphoSyntaxAnalysesOC.Count);
		}

		/// <summary>
		/// Check that changing an MSA on the WfiMorphBundle also deletes any MSA that was referred to only by that
		/// WfiMorphBundle.
		/// </summary>
		[Test]
		public void ChangeMsaOnWfiMorphBundle()
		{
			CheckDisposed();

			ILexDb ld = Cache.LangProject.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);
			Assert.AreEqual(1, le.SensesOS.Count);
			LexSense ls = le.SensesOS[0] as LexSense;
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			int hvoMsa = le.MorphoSyntaxAnalysesOC.HvoArray[0];
			Assert.AreEqual(hvoMsa, ls.MorphoSyntaxAnalysisRAHvo);

			// Setup WfiMorphBundle
			IWfiWordform wf = Cache.LangProject.WordformInventoryOA.WordformsOC.Add(new WfiWordform());
			IWfiAnalysis anal = wf.AnalysesOC.Add(new WfiAnalysis());
			IWfiMorphBundle wmb = anal.MorphBundlesOS.Append(new WfiMorphBundle());

			MoStemAllomorph bearNForm = (MoStemAllomorph)le.AlternateFormsOS.Append(new MoStemAllomorph());
			bearNForm.Form.VernacularDefaultWritingSystem = "bearNTEST";
			wmb.MorphRA = bearNForm;
			wmb.MsaRA = ls.MorphoSyntaxAnalysisRA;

			// Add new MSA to LexEntry
			MoMorphSynAnalysis msa = new MoStemMsa();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			Assert.AreEqual(2, le.MorphoSyntaxAnalysesOC.Count);

			// Change LexSense MSA to new MSA
			ls.MorphoSyntaxAnalysisRA = msa;
			Assert.AreEqual(msa.Hvo, ls.MorphoSyntaxAnalysisRAHvo);
			Assert.AreEqual(2, le.MorphoSyntaxAnalysesOC.Count);

			// Change Msa on WfiMorphBundle
			wmb.MsaRA = ls.MorphoSyntaxAnalysisRA;
			Assert.AreEqual(msa.Hvo, wmb.MsaRA.Hvo);
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
		}

		/// <summary>
		/// Check that deleting a MoMorphAdhocProhib also deletes any MSA that was
		/// referred to only by that object.
		/// </summary>
		[Test]
		public void DeleteMoMorphAdhocProhib()
		{
			CheckDisposed();

			ILexDb ld = Cache.LangProject.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);
			Assert.AreEqual(1, le.SensesOS.Count);
			LexSense ls = le.SensesOS[0] as LexSense;
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			int hvoMsa = le.MorphoSyntaxAnalysesOC.HvoArray[0];
			Assert.AreEqual(hvoMsa, ls.MorphoSyntaxAnalysisRAHvo);
			IMoMorphSynAnalysis baseMsa = ls.MorphoSyntaxAnalysisRA;

			MoMorphAdhocProhib mmac = (MoMorphAdhocProhib)
				Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Add(new MoMorphAdhocProhib());
			// Set FirstMorpheme
			mmac.FirstMorphemeRA = baseMsa;
			Assert.AreEqual(baseMsa.Hvo, mmac.FirstMorphemeRAHvo);

			// Build Morphemes (Reference Sequence) for our mmac
			int i = 0;
			for (; i < 2; i++)
			{
				// Add new MSA to our LexEntry
				MoMorphSynAnalysis msa = new MoStemMsa();
				le.MorphoSyntaxAnalysesOC.Add(msa);
				Assert.AreEqual(i + 2, le.MorphoSyntaxAnalysesOC.Count);
				// Add new MSA to Components
				mmac.MorphemesRS.Append(msa.Hvo);
				Assert.AreEqual(i + 1, mmac.MorphemesRS.Count);
				Assert.AreEqual(msa.Hvo, mmac.MorphemesRS[i].Hvo);
			}

			// Build RestOfMorphs (Reference Sequence) for our mmac
			for (int j = 0; j < 2; j++)
			{
				// Add new MSA to our LexEntry
				MoMorphSynAnalysis msa = new MoStemMsa();
				le.MorphoSyntaxAnalysesOC.Add(msa);
				Assert.AreEqual(j + (i + 2), le.MorphoSyntaxAnalysesOC.Count);
				// Add new MSA to Components
				mmac.RestOfMorphsRS.Append(msa.Hvo);
				Assert.AreEqual(j + 1, mmac.RestOfMorphsRS.Count);
				Assert.AreEqual(msa.Hvo, mmac.RestOfMorphsRS[j].Hvo);
			}

			// Delete our LexSense so that when we delete the msa, mmac can delete its FirstMorpheme.
			ls.DeleteUnderlyingObject();
			Assert.AreEqual(0, le.SensesOS.Count);
			Assert.AreEqual(1 + 2 + 2, le.MorphoSyntaxAnalysesOC.Count);
			// Delete our MoMorphAdhocProhib (mmac)
			mmac.DeleteUnderlyingObject();
			Assert.AreEqual(0, le.MorphoSyntaxAnalysesOC.Count);
		}

		/// <summary>
		/// Check that deleting a MoMorphSynAnalysis also deletes any MSA that was
		/// referred to only by that object.
		/// </summary>
		[Test]
		public void DeleteMoMorphSynAnalysis()
		{
			CheckDisposed();

			ILexDb ld = Cache.LangProject.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);
			Assert.AreEqual(1, le.SensesOS.Count);
			LexSense ls = le.SensesOS[0] as LexSense;
			Assert.AreEqual(1, le.MorphoSyntaxAnalysesOC.Count);
			int hvoMsa = le.MorphoSyntaxAnalysesOC.HvoArray[0];
			Assert.AreEqual(hvoMsa, ls.MorphoSyntaxAnalysisRAHvo);
			IMoMorphSynAnalysis baseMsa = ls.MorphoSyntaxAnalysisRA;

			// Build Components (Reference Sequence) for our baseMSA
			for (int i = 1; i < 4; i++)
			{
				// Add new MSA to our LexEntry
				MoMorphSynAnalysis msa = new MoStemMsa();
				le.MorphoSyntaxAnalysesOC.Add(msa);
				Assert.AreEqual(i + 1, le.MorphoSyntaxAnalysesOC.Count);
				// Add new MSA to Components
				baseMsa.ComponentsRS.Append(msa.Hvo);
				Assert.AreEqual(i, baseMsa.ComponentsRS.Count);
				Assert.AreEqual(msa.Hvo, baseMsa.ComponentsRS[i - 1].Hvo);
			}

			// Delete baseMsa by deleting the only reference to it (LexSense)
			ls.DeleteUnderlyingObject();
			Assert.AreEqual(0, le.SensesOS.Count);
			Assert.AreEqual(0, le.MorphoSyntaxAnalysesOC.Count);
		}
	}
	#endregion MsaCleanupTests

	#region EqualMsaTests

	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class EqualMsaTests : InDatabaseFdoTestBase
	{
		#region Data members
		const string m_ksFS1 = "<item id=\"fgender\" type=\"feature\"><abbrev ws=\"en\">Gen</abbrev><term ws=\"en\">gender</term>" +
			"<def ws=\"en\">Grammatical gender is a noun class system, composed of two or three classes,\r\nwhose nouns that have human male and female referents tend to be in separate classes.\r\nOther nouns that are classified in the same way in the language may not be classed by\r\nany correlation with natural sex distinctions.</def>" +
			"<citation>Hartmann and Stork 1972:93</citation><citation>Foley and Van Valin 1984:325</citation><citation>Mish et al. 1990:510</citation>" +
			"<citation>Crystal 1985:133</citation><citation>Dixon, R. 1968:105</citation><citation>Quirk, et al. 1985:314</citation>" +
			"<item id=\"vMasc\" type=\"value\"><abbrev ws=\"en\">Masc</abbrev><term ws=\"en\">masculine gender</term><def ws=\"en\">Masculine gender is a grammatical gender that - marks nouns having human or animal male referents, and - often marks nouns having referents that do not have distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:730</citation><fs id=\"vMascFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Masc\" /></f></fs></item><item id=\"vFem\" type=\"value\"><abbrev ws=\"en\">Fem</abbrev><term ws=\"en\">feminine gender</term><def ws=\"en\">Feminine gender is a grammatical gender that - marks nouns that have human or animal female referents, and - often marks nouns that have referents that do not carry distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:456</citation><fs id=\"vFemFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Fem\" /></f></fs></item><item id=\"vNeut\" type=\"value\"><abbrev ws=\"en\">Neut</abbrev><term ws=\"en\">neuter gender</term><def ws=\"en\">Neuter gender is a grammatical gender that - includes those nouns having referents which do not have distinctions of sex, and - often includes some which do have a natural sex distinction.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:795</citation><fs id=\"vNeutFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Neut\" /></f></fs></item><item id=\"vUnknownfgender\" type=\"value\"><abbrev ws=\"en\">?</abbrev><term ws=\"en\">unknown gender</term><fs id=\"vUnknownfgenderFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"?\" /></f></fs></item></item>";
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateTestData()
		{
			ILangProject lp = Cache.LangProject;
			lp.MorphologicalDataOA.ProdRestrictOA = new CmPossibilityList();
			lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Append(new CmPossibility());
			lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Append(new CmPossibility());
			lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Append(new CmPossibility());
			lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Append(new CmPossibility());
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ld"></param>
		/// <param name="cf"></param>
		/// <param name="defn"></param>
		/// <param name="hvoDomain"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ILexEntry MakeLexEntry(ILexDb ld, string cf, string defn, int hvoDomain)
		{
			ILexEntry le = ld.EntriesOC.Add(new LexEntry());
			le.CitationForm.VernacularDefaultWritingSystem = cf;
			ILexSense ls = le.SensesOS.Append(new LexSense());
			ls.Definition.AnalysisDefaultWritingSystem.Text = defn;
			if (hvoDomain != 0)
				ls.SemanticDomainsRC.Add(hvoDomain);
			MoMorphSynAnalysis msa = new MoStemMsa();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			ls.MorphoSyntaxAnalysisRA = msa;
			return le;
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MoStemMsa_EqualMsa()
		{
			CheckDisposed();

			CreateTestData();

			ILangProject lp = Cache.LangProject;
			ILexDb ld = lp.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);

			MoStemMsa stemMsa1 = new MoStemMsa();
			MoStemMsa stemMsa2 = new MoStemMsa();
			MoDerivAffMsa derivAffixMsa = new MoDerivAffMsa();
			MoInflAffMsa inflAffixMsa = new MoInflAffMsa();
			MoUnclassifiedAffixMsa unclassifiedAffixMsa = new MoUnclassifiedAffixMsa();

			le.MorphoSyntaxAnalysesOC.Add(stemMsa1);
			le.MorphoSyntaxAnalysesOC.Add(stemMsa2);
			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(inflAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa);

			DummyGenericMSA dummyMsa1 = DummyGenericMSA.Create(stemMsa1);
			DummyGenericMSA dummyMsa2 = DummyGenericMSA.Create(stemMsa2);
			DummyGenericMSA dummyMsa3 = DummyGenericMSA.Create(derivAffixMsa);
			DummyGenericMSA dummyMsa4 = DummyGenericMSA.Create(inflAffixMsa);
			DummyGenericMSA dummyMsa5 = DummyGenericMSA.Create(unclassifiedAffixMsa);

			// Verify we fail comparing on different MSA types.
			Assert.IsFalse(stemMsa1.EqualsMsa(derivAffixMsa));
			Assert.IsFalse(stemMsa1.EqualsMsa(inflAffixMsa));
			Assert.IsFalse(stemMsa1.EqualsMsa(unclassifiedAffixMsa));
			Assert.IsFalse(stemMsa1.EqualsMsa(dummyMsa3));
			Assert.IsFalse(stemMsa1.EqualsMsa(dummyMsa4));
			Assert.IsFalse(stemMsa1.EqualsMsa(dummyMsa5));

			// Verify that stemMsa1 equals itself.
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa1), stemMsa1.ToString() + " - should equal itself.");

			// Verify that stemMsa1 equals stemMsa2
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), stemMsa1.ToString() + " - should equal - " + stemMsa2.ToString());
			Assert.IsTrue(stemMsa1.EqualsMsa(dummyMsa2), "stemMsa1 should equal dummyMsa2");

			// compare with on different PartOfSpeech
			FdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos1 = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());
			IPartOfSpeech pos2 = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());

			stemMsa1.PartOfSpeechRA = pos1;
			stemMsa2.PartOfSpeechRA = pos2;
			dummyMsa2.MainPOS = pos2.Hvo;
			Assert.IsTrue(stemMsa1.PartOfSpeechRA != stemMsa2.PartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to different POS");
			Assert.IsFalse(stemMsa1.EqualsMsa(dummyMsa2), "stemMsa1 should not equal dummyMsa2");

			// reset POS
			stemMsa1.PartOfSpeechRAHvo = stemMsa2.PartOfSpeechRAHvo;
			dummyMsa2.MainPOS = stemMsa1.PartOfSpeechRAHvo;
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 & stemMsa2 should be equal with matching POS");
			Assert.IsTrue(stemMsa1.EqualsMsa(dummyMsa2), "stemMsa1 should equal dummyMsa2");

			// compare on different InflectionClass
			pos1.InflectionClassesOC.Add(new MoInflClass());
			pos2.InflectionClassesOC.Add(new MoInflClass());
			stemMsa1.InflectionClassRAHvo = pos1.InflectionClassesOC.HvoArray[0];
			stemMsa2.InflectionClassRAHvo = pos2.InflectionClassesOC.HvoArray[0];
			Assert.IsTrue(stemMsa1.InflectionClassRA != stemMsa2.InflectionClassRA, "inflection classes should not be equal.");
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to different inflection classes.");

			// reset InflectionClass
			stemMsa1.InflectionClassRA = stemMsa2.InflectionClassRA;
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 & stemMsa2 should be equal with matching inflection classes");

			// compare different Productivity Restrictions
			ICmPossibility pr1 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[0];
			ICmPossibility pr2 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[1];

			stemMsa1.ProdRestrictRC.Add(pr1.Hvo);
			stemMsa2.ProdRestrictRC.Add(pr2.Hvo);
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to productivity restrictions differences.");

			stemMsa1.ProdRestrictRC.Add(pr2.Hvo);
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to productivity restrictions count difference.");

			// Match Productivity Restrictions
			stemMsa2.ProdRestrictRC.Add(pr1.Hvo);
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 & stemMsa2 should be equal with matching productivity restrictions");

			// compare different MsFeatures
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(m_ksFS1);
			XmlNode itemNeut = doc.SelectSingleNode("/item/item[3]");
			XmlNode itemFem = doc.SelectSingleNode("/item/item[2]");
			stemMsa1.MsFeaturesOA = new FsFeatStruc();
			stemMsa2.MsFeaturesOA = new FsFeatStruc();

			stemMsa1.MsFeaturesOA.AddFeatureFromXml(Cache, itemNeut);
			stemMsa2.MsFeaturesOA.AddFeatureFromXml(Cache, itemFem);

			Assert.IsFalse(stemMsa1.MsFeaturesOA.IsEquivalent(stemMsa2.MsFeaturesOA), "MsFeaturesOA should not be equal.");
			Assert.IsFalse(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 should not be equal to stemMsa2 due to different MsFeaturesOA.");

			// match feature structures
			stemMsa1.MsFeaturesOA.AddFeatureFromXml(Cache, itemFem);
			Assert.IsTrue(stemMsa1.EqualsMsa(stemMsa2), "stemMsa1 & stemMsa2 should be equal with matching feature structure");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MoInflAffMsa_EqualMsa()
		{
			CheckDisposed();

			CreateTestData();

			ILangProject lp = Cache.LangProject;
			ILexDb ld = lp.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);

			MoInflAffMsa infAffixMsa1 = new MoInflAffMsa();
			MoInflAffMsa infAffixMsa2 = new MoInflAffMsa();
			MoStemMsa stemMsa = new MoStemMsa();
			MoDerivAffMsa derivAffixMsa = new MoDerivAffMsa();
			MoUnclassifiedAffixMsa unclassifiedAffixMsa = new MoUnclassifiedAffixMsa();

			le.MorphoSyntaxAnalysesOC.Add(infAffixMsa1);
			le.MorphoSyntaxAnalysesOC.Add(infAffixMsa2);
			le.MorphoSyntaxAnalysesOC.Add(stemMsa);
			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa);

			DummyGenericMSA dummyMsa1 = DummyGenericMSA.Create(infAffixMsa1);
			DummyGenericMSA dummyMsa2 = DummyGenericMSA.Create(infAffixMsa2);
			DummyGenericMSA dummyMsa3 = DummyGenericMSA.Create(stemMsa);
			DummyGenericMSA dummyMsa4 = DummyGenericMSA.Create(derivAffixMsa);
			DummyGenericMSA dummyMsa5 = DummyGenericMSA.Create(unclassifiedAffixMsa);

			// Verify we fail comparing on different MSA types.
			Assert.IsFalse(infAffixMsa1.EqualsMsa(stemMsa));
			Assert.IsFalse(infAffixMsa1.EqualsMsa(derivAffixMsa));
			Assert.IsFalse(infAffixMsa1.EqualsMsa(unclassifiedAffixMsa));
			Assert.IsFalse(infAffixMsa1.EqualsMsa(dummyMsa3));
			Assert.IsFalse(infAffixMsa1.EqualsMsa(dummyMsa4));
			Assert.IsFalse(infAffixMsa1.EqualsMsa(dummyMsa5));

			// Verify that infAffixMsa1 equals itself.
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa1), infAffixMsa1.ToString() + " - should equal itself.");

			// Verify that infAffixMsa1 equals infAffixMsa2
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), infAffixMsa1.ToString() + " - should equal - " + infAffixMsa2.ToString());
			Assert.IsTrue(infAffixMsa1.EqualsMsa(dummyMsa2), "infAffixMsa1 should equal dummyMsa2");

			// compare with on different PartOfSpeech
			FdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos1 = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());
			IPartOfSpeech pos2 = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());

			infAffixMsa1.PartOfSpeechRA = pos1;
			infAffixMsa2.PartOfSpeechRA = pos2;
			dummyMsa2.MainPOS = pos2.Hvo;
			Assert.IsTrue(infAffixMsa1.PartOfSpeechRA != infAffixMsa2.PartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to different POS");
			Assert.IsFalse(infAffixMsa1.EqualsMsa(dummyMsa2), "infAffixMsa1 should not equal dummyMsa2");

			// reset POS
			infAffixMsa1.PartOfSpeechRAHvo = infAffixMsa2.PartOfSpeechRAHvo;
			dummyMsa2.MainPOS = infAffixMsa1.PartOfSpeechRAHvo;
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 & infAffixMsa2 should be equal with matching POS");
			Assert.IsTrue(infAffixMsa1.EqualsMsa(dummyMsa2), "infAffixMsa1 should equal dummyMsa2");

			// skip AffixCategory

			// compare different Slots
			pos1.AffixSlotsOC.Add(new MoInflAffixSlot());
			pos1.AffixSlotsOC.Add(new MoInflAffixSlot());

			infAffixMsa1.SlotsRC.Add(pos1.AffixSlotsOC.HvoArray[0]);
			infAffixMsa2.SlotsRC.Add(pos1.AffixSlotsOC.HvoArray[1]);
			dummyMsa2.Slot = pos1.AffixSlotsOC.HvoArray[1];
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to affix slots differences.");
			Assert.IsFalse(infAffixMsa1.EqualsMsa(dummyMsa2), "infAffixMsa1 should not equal dummyMsa2");

			infAffixMsa1.SlotsRC.Add(pos1.AffixSlotsOC.HvoArray[1]);
			Assert.IsTrue(infAffixMsa1.SlotsRC.Count != infAffixMsa2.SlotsRC.Count);
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to affix slots Count differences.");
			Assert.IsFalse(infAffixMsa1.EqualsMsa(dummyMsa2), "infAffixMsa1 should not equal dummyMsa2");

			infAffixMsa2.SlotsRC.Add(pos1.AffixSlotsOC.HvoArray[0]);
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should equal infAffixMsa2 due to affix slots matching.");

			// compare different FromProdRestrict
			ICmPossibility pr1 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[0];
			ICmPossibility pr2 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[1];

			infAffixMsa1.FromProdRestrictRC.Add(pr1.Hvo);
			infAffixMsa2.FromProdRestrictRC.Add(pr2.Hvo);
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to productivity restrictions differences.");

			infAffixMsa1.FromProdRestrictRC.Add(pr2.Hvo);
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to productivity restrictions count difference.");

			// Match Productivity Restrictions
			infAffixMsa2.FromProdRestrictRC.Add(pr1.Hvo);
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 & infAffixMsa2 should be equal with matching productivity restrictions");

			// compare different InflFeats
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(m_ksFS1);
			XmlNode itemNeut = doc.SelectSingleNode("/item/item[3]");
			XmlNode itemFem = doc.SelectSingleNode("/item/item[2]");
			infAffixMsa1.InflFeatsOA = new FsFeatStruc();
			infAffixMsa2.InflFeatsOA = new FsFeatStruc();
			infAffixMsa1.InflFeatsOA.AddFeatureFromXml(Cache, itemNeut);
			infAffixMsa2.InflFeatsOA.AddFeatureFromXml(Cache, itemFem);

			Assert.IsFalse(infAffixMsa1.InflFeatsOA.IsEquivalent(infAffixMsa2.InflFeatsOA), "InflFeatsOA should not be equal.");
			Assert.IsFalse(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 should not be equal to infAffixMsa2 due to different InflFeatsOA.");

			// match feature structures
			infAffixMsa1.InflFeatsOA.AddFeatureFromXml(Cache, itemFem);
			Assert.IsTrue(infAffixMsa1.EqualsMsa(infAffixMsa2), "infAffixMsa1 & infAffixMsa2 should be equal with matching feature structure");
		}

		/// <summary>
		/// TODO: Add to tests when we start using MoDerivStepMsa.
		/// </summary>
		[Test]
		[Ignore("Add this test after we start using MoDerivStepMsa.")]
		public void MoDerivStepMsa_EqualMsa()
		{
			CheckDisposed();

			// Basically the same properties tested in MoStemMsa_EqualMsa()?
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MoDerivAffMsa_EqualMsa()
		{
			CheckDisposed();

			CreateTestData();

			ILangProject lp = Cache.LangProject;
			ILexDb ld = lp.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);

			MoDerivAffMsa derivAffixMsa1 = new MoDerivAffMsa();
			MoDerivAffMsa derivAffixMsa2 = new MoDerivAffMsa();
			MoStemMsa stemMsa = new MoStemMsa();
			MoInflAffMsa inflAffixMsa = new MoInflAffMsa();
			MoUnclassifiedAffixMsa unclassifiedAffixMsa = new MoUnclassifiedAffixMsa();

			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa1);
			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa2);
			le.MorphoSyntaxAnalysesOC.Add(stemMsa);
			le.MorphoSyntaxAnalysesOC.Add(inflAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa);

			DummyGenericMSA dummyMsa1 = DummyGenericMSA.Create(derivAffixMsa1);
			DummyGenericMSA dummyMsa2 = DummyGenericMSA.Create(derivAffixMsa2);
			DummyGenericMSA dummyMsa3 = DummyGenericMSA.Create(stemMsa);
			DummyGenericMSA dummyMsa4 = DummyGenericMSA.Create(inflAffixMsa);
			DummyGenericMSA dummyMsa5 = DummyGenericMSA.Create(unclassifiedAffixMsa);

			// Verify we fail comparing on different MSA types.
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(stemMsa));
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(inflAffixMsa));
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(unclassifiedAffixMsa));
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(dummyMsa3));
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(dummyMsa4));
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(dummyMsa5));

			// Verify that derivAffixMsa1 equals itself.
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa1), derivAffixMsa1.ToString() + " - should equal itself.");

			// Verify that derivAffixMsa1 equals derivAffixMsa2
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), derivAffixMsa1.ToString() + " - should equal - " + derivAffixMsa2.ToString());
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(dummyMsa2), "derivAffixMsa1 should equal dummyMsa2");

			// compare with on different FromPartOfSpeech
			FdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos1 = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());
			IPartOfSpeech pos2 = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());
			derivAffixMsa1.FromPartOfSpeechRA = pos1;
			derivAffixMsa2.FromPartOfSpeechRA = pos2;
			dummyMsa2.MainPOS = pos2.Hvo;
			Assert.IsTrue(derivAffixMsa1.FromPartOfSpeechRA != derivAffixMsa2.FromPartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different FromPartOfSpeech");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(dummyMsa2), "derivAffixMsa1 should not equal dummyMsa2");

			// reset POS
			derivAffixMsa1.FromPartOfSpeechRAHvo = derivAffixMsa2.FromPartOfSpeechRAHvo;
			dummyMsa2.MainPOS = derivAffixMsa1.FromPartOfSpeechRAHvo;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching POS");
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(dummyMsa2), "derivAffixMsa1 should equal dummyMsa2");

			// compare with on different ToPartOfSpeech
			PartOfSpeech pos3 = (PartOfSpeech)posSeq.Append(new PartOfSpeech());
			PartOfSpeech pos4 = (PartOfSpeech)posSeq.Append(new PartOfSpeech());
			derivAffixMsa1.ToPartOfSpeechRA = pos3;
			derivAffixMsa2.ToPartOfSpeechRA = pos4;
			dummyMsa2.SecondaryPOS = pos4.Hvo;
			Assert.IsTrue(derivAffixMsa1.ToPartOfSpeechRA != derivAffixMsa2.ToPartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different ToPartOfSpeech");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(dummyMsa2), "derivAffixMsa1 should not equal dummyMsa2");

			// reset POS
			derivAffixMsa1.ToPartOfSpeechRAHvo = derivAffixMsa2.ToPartOfSpeechRAHvo;
			dummyMsa2.SecondaryPOS = derivAffixMsa1.ToPartOfSpeechRAHvo;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching POS");
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(dummyMsa2), "derivAffixMsa1 should equal dummyMsa2");

			// compare on different FromInflectionClass
			pos1.InflectionClassesOC.Add(new MoInflClass());
			pos2.InflectionClassesOC.Add(new MoInflClass());
			derivAffixMsa1.FromInflectionClassRAHvo = pos1.InflectionClassesOC.HvoArray[0];
			derivAffixMsa2.FromInflectionClassRAHvo = pos2.InflectionClassesOC.HvoArray[0];
			Assert.IsTrue(derivAffixMsa1.FromInflectionClassRA != derivAffixMsa2.FromInflectionClassRA, "inflection classes should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different inflection classes.");

			// reset InflectionClass
			derivAffixMsa1.FromInflectionClassRA = derivAffixMsa2.FromInflectionClassRA;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching inflection classes");

			// compare on different FromStemName
			pos1.StemNamesOC.Add(new MoStemName());
			pos2.StemNamesOC.Add(new MoStemName());
			derivAffixMsa1.FromStemNameRAHvo = pos1.StemNamesOC.HvoArray[0];
			derivAffixMsa2.FromStemNameRAHvo = pos2.StemNamesOC.HvoArray[0];
			Assert.IsTrue(derivAffixMsa1.FromStemNameRA != derivAffixMsa2.FromStemNameRA, "stem names should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different stem names.");

			// reset StemName
			derivAffixMsa1.FromStemNameRA = derivAffixMsa2.FromStemNameRA;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching stem names");

			// compare on different ToInflectionClass
			pos3.InflectionClassesOC.Add(new MoInflClass());
			pos4.InflectionClassesOC.Add(new MoInflClass());
			derivAffixMsa1.ToInflectionClassRAHvo = pos3.InflectionClassesOC.HvoArray[0];
			derivAffixMsa2.ToInflectionClassRAHvo = pos4.InflectionClassesOC.HvoArray[0];
			Assert.IsTrue(derivAffixMsa1.ToInflectionClassRA != derivAffixMsa2.ToInflectionClassRA, "inflection classes should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different inflection classes.");

			// reset InflectionClass
			derivAffixMsa1.ToInflectionClassRA = derivAffixMsa2.ToInflectionClassRA;
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching inflection classes");

			// compare different FromProdRestrict
			ICmPossibility pr1 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[0];
			ICmPossibility pr2 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[1];

			derivAffixMsa1.FromProdRestrictRC.Add(pr1.Hvo);
			derivAffixMsa2.FromProdRestrictRC.Add(pr2.Hvo);
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to productivity restrictions differences.");

			derivAffixMsa1.FromProdRestrictRC.Add(pr2.Hvo);
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to productivity restrictions count difference.");

			// Match Productivity Restrictions
			derivAffixMsa2.FromProdRestrictRC.Add(pr1.Hvo);
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching productivity restrictions");

			// compare different ToProdRestrict
			CmPossibility pr3 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[2] as CmPossibility;
			CmPossibility pr4 = lp.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS[3] as CmPossibility;

			derivAffixMsa1.ToProdRestrictRC.Add(pr3.Hvo);
			derivAffixMsa2.ToProdRestrictRC.Add(pr4.Hvo);
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to productivity restrictions differences.");

			derivAffixMsa1.ToProdRestrictRC.Add(pr4.Hvo);
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to productivity restrictions count difference.");

			// Match Productivity Restrictions
			derivAffixMsa2.ToProdRestrictRC.Add(pr3.Hvo);
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching productivity restrictions");

			// compare different FromMsFeatures
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(m_ksFS1);
			XmlNode itemNeut = doc.SelectSingleNode("/item/item[3]");
			XmlNode itemFem = doc.SelectSingleNode("/item/item[2]");
			derivAffixMsa1.FromMsFeaturesOA = new FsFeatStruc();
			derivAffixMsa2.FromMsFeaturesOA = new FsFeatStruc();
			derivAffixMsa1.FromMsFeaturesOA.AddFeatureFromXml(Cache, itemNeut);
			derivAffixMsa2.FromMsFeaturesOA.AddFeatureFromXml(Cache, itemFem);

			Assert.IsFalse(derivAffixMsa1.FromMsFeaturesOA.IsEquivalent(derivAffixMsa2.FromMsFeaturesOA), "FromMsFeaturesOA should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different FromMsFeaturesOA.");

			// match feature structures
			derivAffixMsa1.FromMsFeaturesOA.AddFeatureFromXml(Cache, itemFem);
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching feature structure");

			// compare different ToMsFeatures
			derivAffixMsa1.ToMsFeaturesOA = new FsFeatStruc();
			derivAffixMsa2.ToMsFeaturesOA = new FsFeatStruc();
			derivAffixMsa1.ToMsFeaturesOA.AddFeatureFromXml(Cache, itemFem);
			derivAffixMsa2.ToMsFeaturesOA.AddFeatureFromXml(Cache, itemNeut);

			Assert.IsFalse(derivAffixMsa1.ToMsFeaturesOA.IsEquivalent(derivAffixMsa2.ToMsFeaturesOA), "ToMsFeaturesOA should not be equal.");
			Assert.IsFalse(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 should not be equal to derivAffixMsa2 due to different ToMsFeaturesOA.");

			// match feature structures
			derivAffixMsa1.ToMsFeaturesOA.AddFeatureFromXml(Cache, itemNeut);
			Assert.IsTrue(derivAffixMsa1.EqualsMsa(derivAffixMsa2), "derivAffixMsa1 & derivAffixMsa2 should be equal with matching feature structure");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MoUnclassifiedAffixMsa_EqualMsa()
		{
			CheckDisposed();

			ILangProject lp = Cache.LangProject;
			ILexDb ld = lp.LexDbOA;
			ILexEntry le = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", 0);

			MoUnclassifiedAffixMsa unclassifiedAffixMsa1 = new MoUnclassifiedAffixMsa();
			MoUnclassifiedAffixMsa unclassifiedAffixMsa2 = new MoUnclassifiedAffixMsa();
			MoStemMsa stemMsa = new MoStemMsa();
			MoInflAffMsa inflAffixMsa = new MoInflAffMsa();
			MoDerivAffMsa derivAffixMsa = new MoDerivAffMsa();

			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa1);
			le.MorphoSyntaxAnalysesOC.Add(unclassifiedAffixMsa2);
			le.MorphoSyntaxAnalysesOC.Add(stemMsa);
			le.MorphoSyntaxAnalysesOC.Add(inflAffixMsa);
			le.MorphoSyntaxAnalysesOC.Add(derivAffixMsa);

			DummyGenericMSA dummyMsa1 = DummyGenericMSA.Create(unclassifiedAffixMsa1);
			DummyGenericMSA dummyMsa2 = DummyGenericMSA.Create(unclassifiedAffixMsa2);
			DummyGenericMSA dummyMsa3 = DummyGenericMSA.Create(stemMsa);
			DummyGenericMSA dummyMsa4 = DummyGenericMSA.Create(inflAffixMsa);
			DummyGenericMSA dummyMsa5 = DummyGenericMSA.Create(derivAffixMsa);

			// Verify we fail comparing on different MSA types.
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(stemMsa));
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(inflAffixMsa));
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(derivAffixMsa));
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(dummyMsa3));
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(dummyMsa4));
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(dummyMsa5));

			// Verify that unclassifiedAffixMsa1 equals itself.
			Assert.IsTrue(unclassifiedAffixMsa1.EqualsMsa(unclassifiedAffixMsa1), unclassifiedAffixMsa1.ToString() + " - should equal itself.");

			// Verify that unclassifiedAffixMsa1 equals unclassifiedAffixMsa2
			Assert.IsTrue(unclassifiedAffixMsa1.EqualsMsa(unclassifiedAffixMsa2), unclassifiedAffixMsa1.ToString() + " - should equal - " + unclassifiedAffixMsa2.ToString());
			Assert.IsTrue(unclassifiedAffixMsa1.EqualsMsa(dummyMsa2), "unclassifiedAffixMsa1 should equal dummyMsa2");

			// compare with on different PartOfSpeech
			FdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos1 = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());
			IPartOfSpeech pos2 = (IPartOfSpeech)posSeq.Append(new PartOfSpeech());

			unclassifiedAffixMsa1.PartOfSpeechRA = pos1;
			unclassifiedAffixMsa2.PartOfSpeechRA = pos2;
			dummyMsa2.MainPOS = pos2.Hvo;
			Assert.IsTrue(unclassifiedAffixMsa1.PartOfSpeechRA != unclassifiedAffixMsa2.PartOfSpeechRA, "msa POSes should not be equal.");
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(unclassifiedAffixMsa2), "unclassifiedAffixMsa1 should not be equal to unclassifiedAffixMsa2 due to different POS");
			Assert.IsFalse(unclassifiedAffixMsa1.EqualsMsa(dummyMsa2), "unclassifiedAffixMsa1 should not equal dummyMsa2");

			// reset POS
			unclassifiedAffixMsa1.PartOfSpeechRAHvo = unclassifiedAffixMsa2.PartOfSpeechRAHvo;
			dummyMsa2.MainPOS = unclassifiedAffixMsa1.PartOfSpeechRAHvo;
			Assert.IsTrue(unclassifiedAffixMsa1.EqualsMsa(unclassifiedAffixMsa2), "unclassifiedAffixMsa1 & unclassifiedAffixMsa2 should be equal with matching POS");
			Assert.IsTrue(unclassifiedAffixMsa1.EqualsMsa(dummyMsa2), "unclassifiedAffixMsa1 should equal dummyMsa2");
		}

	}


	#endregion EqualMsaTests

	#region LexSenseTests

	/// <summary>
	/// Test the LexSense class.
	/// </summary>
	[TestFixture]
	public class LexSenseTests : InMemoryFdoTestBase
	{
		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_inMemoryCache.InitializeLexDb();
		}
		#endregion

		/// <summary>
		/// Check the method for merging RDE senses. (RDEMergeSense)
		/// </summary>
		[Test]
		public void RdeMerge()
		{
			CheckDisposed();

			// Create a LexEntry LE1 ("xyzTest1" defined as "xyzDefn1.1" in D1).
			// Attempt to merge it and verify that it survives.
			ILexDb ld = Cache.LangProject.LexDbOA;
			FdoOwningSequence<ICmPossibility> seq = Cache.LangProject.SemanticDomainListOA.PossibilitiesOS;
			seq.Append(new CmSemanticDomain());
			seq.Append(new CmSemanticDomain());
			seq.Append(new CmSemanticDomain());
			seq.Append(new CmSemanticDomain());
			seq.Append(new CmSemanticDomain());

			int hvoDom1 = Cache.LangProject.SemanticDomainListOA.PossibilitiesOS[0].Hvo;
			ILexEntry le1 = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.1", hvoDom1);
			Set<int> newItems = new Set<int>();
			int hvoSense1 = le1.SensesOS.FirstItem.Hvo;
			newItems.Add(hvoSense1);
			int fakeFlid = FdoCache.DummyFlid; // get an id for the 'list of senses' property (which isn't in the model)

			LexSense.RDEMergeSense(hvoDom1, fakeFlid, null, Cache, hvoSense1, newItems);

			Assert.IsTrue(Cache.IsRealObject(le1.Hvo, LexEntry.kClassId));
			Assert.AreEqual(hvoDom1, le1.SensesOS[0].SemanticDomainsRC.HvoArray[0]);

			// Create LexEntries LE2("xyzTest1/xyzDefn1.2/D2") and LE3"xyzTest3/xyzDefn3.1/D2".
			// Attempt to merge them both.
			// Verify that LE3 survives.
			// Verify that old LE1 survives and now has two senses; new sense has xyzDefn1.2.
			// Verify that LE2 is deleted and LE3 survives.
			int hvoDom2 = Cache.LangProject.SemanticDomainListOA.PossibilitiesOS[1].Hvo;
			ILexEntry le2 = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.2", hvoDom2);
			Set<int> newItems2 = new Set<int>();
			int hvoSense2 = le2.SensesOS.FirstItem.Hvo;
			newItems2.Add(hvoSense2);

			ILexEntry le3 = MakeLexEntry(ld, "xyzTest3", "xyzDefn3.1", hvoDom2);
			int hvoSense3 = le3.SensesOS.FirstItem.Hvo;
			newItems2.Add(hvoSense3);


			LexSense.RDEMergeSense(hvoDom2, fakeFlid, null, Cache, hvoSense2, newItems2);
			LexSense.RDEMergeSense(hvoDom2, fakeFlid, null, Cache, hvoSense3, newItems2);

			Assert.IsTrue(Cache.IsRealObject(le3.Hvo, LexEntry.kClassId));
			Assert.IsFalse(Cache.IsRealObject(le2.Hvo, LexEntry.kClassId));
			Assert.IsTrue(Cache.IsRealObject(le1.Hvo, LexEntry.kClassId));
			Assert.AreEqual(2, le1.SensesOS.Count, "sense added to entry by merge");
			Assert.AreEqual("xyzDefn1.2", le1.SensesOS[1].Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(hvoDom2, le1.SensesOS[1].SemanticDomainsRC.HvoArray[0]);
			Assert.AreEqual(hvoDom2, le3.SensesOS[0].SemanticDomainsRC.HvoArray[0]);

			// Create two more entries LE4("xyzTest1/xyzDefn1.2/D3" and LE5 ("xyzTest1/xyzDefn1.3/D3").
			// Verify that the second sense of LE1 gains a domain;
			// It also gains exactly one new sense;
			// And LE4 and LE5 are both deleted.
			int hvoDom3 = Cache.LangProject.SemanticDomainListOA.PossibilitiesOS[2].Hvo;
			ILexEntry le4 = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.2", hvoDom3);
			Set<int> newItems3 = new Set<int>();
			int hvoSense4 = le4.SensesOS.FirstItem.Hvo;
			newItems3.Add(hvoSense4);

			ILexEntry le5 = MakeLexEntry(ld, "xyzTest1", "xyzDefn1.3", hvoDom3);
			int hvoSense5 = le5.SensesOS.FirstItem.Hvo;
			newItems3.Add(hvoSense5);


			LexSense.RDEMergeSense(hvoDom3, fakeFlid, null, Cache, hvoSense4, newItems3);
			LexSense.RDEMergeSense(hvoDom3, fakeFlid, null, Cache, hvoSense5, newItems3);

			Assert.IsTrue(Cache.IsRealObject(le3.Hvo, LexEntry.kClassId));
			Assert.IsFalse(Cache.IsRealObject(le4.Hvo, LexEntry.kClassId));
			Assert.IsFalse(Cache.IsRealObject(le5.Hvo, LexEntry.kClassId));
			Assert.IsTrue(Cache.IsRealObject(le1.Hvo, LexEntry.kClassId));
			Assert.AreEqual(3, le1.SensesOS.Count, "one sense added to entry by merge");
			Assert.AreEqual("xyzDefn1.3", le1.SensesOS[2].Definition.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(hvoDom3, le1.SensesOS[2].SemanticDomainsRC.HvoArray[0]);
			int[] sense2Domains = le1.SensesOS[1].SemanticDomainsRC.HvoArray;
			Assert.AreEqual(2, sense2Domains.Length, "got 2 semantic domains on sense 2");
			int minDom = Math.Min(hvoDom2, hvoDom3);  // smaller of expected domains.
			int maxDom = Math.Max(hvoDom2, hvoDom3);
			int minActual = Math.Min(sense2Domains[0], sense2Domains[1]);
			int maxActual = Math.Max(sense2Domains[0], sense2Domains[1]);
			Assert.AreEqual(minDom, minActual, "expected domains on merged sense");
			Assert.AreEqual(maxDom, maxActual, "expected domains on merged sense");

			// Try adding four senses, three for the same CF, but which doesn't pre-exist.
			// Also, the three are exact duplicates.
			int hvoDom4 = Cache.LangProject.SemanticDomainListOA.PossibilitiesOS[4].Hvo;
			ILexEntry le6 = MakeLexEntry(ld, "xyzTest6", "xyzDefn6.1", hvoDom4);
			Set<int> newItems4 = new Set<int>();
			int hvoSense6 = le6.SensesOS.FirstItem.Hvo;
			newItems4.Add(hvoSense6);

			ILexEntry le7 = MakeLexEntry(ld, "xyzTest6", "xyzDefn6.1", hvoDom4);
			int hvoSense7 = le7.SensesOS.FirstItem.Hvo;
			newItems4.Add(hvoSense7);

			ILexEntry le8 = MakeLexEntry(ld, "xyzTest6", "xyzDefn6.1", hvoDom4);
			int hvoSense8 = le8.SensesOS.FirstItem.Hvo;
			newItems4.Add(hvoSense8);


			LexSense.RDEMergeSense(hvoDom4, fakeFlid, null, Cache, hvoSense6, newItems4);
			LexSense.RDEMergeSense(hvoDom4, fakeFlid, null, Cache, hvoSense7, newItems4);
			LexSense.RDEMergeSense(hvoDom4, fakeFlid, null, Cache, hvoSense8, newItems4);

			Assert.IsTrue(Cache.IsRealObject(le6.Hvo, LexEntry.kClassId));
			Assert.IsFalse(Cache.IsRealObject(le7.Hvo, LexEntry.kClassId));
			Assert.IsFalse(Cache.IsRealObject(le8.Hvo, LexEntry.kClassId));
			Assert.AreEqual(1, le6.SensesOS.Count, "one sense survives merge");
		}
	}

	#endregion LexSenseTests
}
