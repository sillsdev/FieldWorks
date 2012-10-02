// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChkRefMatcherTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ChkRefMatcher class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ChkRefMatcherTests : TeKeyTermsInitTestBase
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method Transliterate.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Transliterate()
		{
			Assert.AreEqual("abba", ChkRefMatcher.Transliterate("\u03b1\u03b2\u03b2\u03b1"));
			Assert.AreEqual("moicheuw", ChkRefMatcher.Transliterate("\u03BC\u03BF\u03B9\u03C7\u03B5\u1F7B\u03C9"));
			Assert.AreEqual("parakljtos", ChkRefMatcher.Transliterate("\u03C0\u03B1\u03C1\u1F71\u03BA\u03BB\u03B7\u03C4\u03BF\u03C2"));
			Assert.AreEqual("charis", ChkRefMatcher.Transliterate("\u03C7\u1F71\u03C1\u03B9\u03C2"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to find a correspinding new ChkRef for an existing ChkRef with an
		/// assigned vernacular equivalent (TE-6808)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCorrespondingChkRef()
		{
			CheckDisposed();

			// First, create a possibility list with the old key terms and set some ChkRefs to
			// have renderings.
			ILangProject lp = m_fdoCache.LangProject;
			IWfiWordform abc = lp.WordformInventoryOA.AddRealWordform("abc", lp.DefaultVernacularWritingSystem);
			IWfiWordform def = lp.WordformInventoryOA.AddRealWordform("def", lp.DefaultVernacularWritingSystem);
			IWfiWordform ghi = lp.WordformInventoryOA.AddRealWordform("ghi", lp.DefaultVernacularWritingSystem);
			IWfiWordform jkl = lp.WordformInventoryOA.AddRealWordform("jkl", lp.DefaultVernacularWritingSystem);
			IWfiWordform mno = lp.WordformInventoryOA.AddRealWordform("mno", lp.DefaultVernacularWritingSystem);

			ICmPossibilityList oldKeyTermsList = new CmPossibilityList();
			lp.CheckListsOC.Add(oldKeyTermsList);
			ChkTerm term = new ChkTerm();
			oldKeyTermsList.PossibilitiesOS.Append(term);
			term.Name.SetAlternative("Adultery", m_wsEn);
			ChkTerm subsense = new ChkTerm();
			term.SubPossibilitiesOS.Append(subsense);
			subsense.Name.SetAlternative("The act of sexual unfaithfulness", m_wsEn);
			ChkRef chkrefMoicheuw040005027 = AddOccurrenceToOldStyleSense(subsense, 040005027, abc, "moicheuw");
			ChkRef chkrefMoicheuw040005028 = AddOccurrenceToOldStyleSense(subsense, 040005028, abc, "moicheuw");
			ChkRef chkrefMoichaomai040005032 = AddOccurrenceToOldStyleSense(subsense, 040005032, def, "moichaomai");
			ChkRef chkrefMoicheia040015019 = AddOccurrenceToOldStyleSense(subsense, 040015019, ghi, "moicheia");
			ChkRef chkrefMoichaomai040019009 = AddOccurrenceToOldStyleSense(subsense, 040019009, def, "moichaomai");

			subsense = new ChkTerm();
			term.SubPossibilitiesOS.Append(subsense);
			subsense.Name.SetAlternative("One who sexually violates marriage vows", m_wsEn);

			ChkTerm subsubsense = new ChkTerm();
			subsense.SubPossibilitiesOS.Append(subsubsense);
			subsubsense.Name.SetAlternative("Masculine offenders", m_wsEn);
			ChkRef chkrefMoichos042018011 = AddOccurrenceToOldStyleSense(subsubsense, 042018011, jkl, "moichos");
			ChkRef chkrefMoichos046006009 = AddOccurrenceToOldStyleSense(subsubsense, 046006009, jkl, "moichos");
			ChkRef chkrefMoichos058013004 = AddOccurrenceToOldStyleSense(subsubsense, 058013004, jkl, "moichos");

			subsubsense = new ChkTerm();
			subsense.SubPossibilitiesOS.Append(subsubsense);
			subsubsense.Name.SetAlternative("Feminine offenders", m_wsEn);
			ChkRef chkrefMoichalis045007003 = AddOccurrenceToOldStyleSense(subsubsense, 045007003, mno, "moichalis");
			ChkRef chkrefMoichalis061002014 = AddOccurrenceToOldStyleSense(subsubsense, 061002014, mno, "moichalis");

			// Next, load the new list of Biblicalk terms
			BiblicalTermsList terms = new BiblicalTermsList();
			terms.Version = Guid.NewGuid();
			terms.KeyTerms = new List<Term>();
			terms.KeyTerms.Add(new Term(3, "KT", "\u03b1\u03b2\u03b2\u03b1", "Greek",
				"abba; father", null, null, 4101403603, 4500801516, 4800400618));
			string sGrkMoichaomai = "\u03BC\u03BF\u03B9\u03C7\u1F71\u03BF\u03BC\u03B1\u03B9";
			terms.KeyTerms.Add(new Term(1139, "KT", "\u03BC\u03BF\u03B9\u03C7\u1F71\u03BF\u03BC\u03B1\u03B9-1",
				"Greek", "commit adultery", sGrkMoichaomai,
				"\u03BC\u03BF\u03B9\u03C7\u03B5\u1F77\u03B1, \u03BC\u03BF\u03B9\u03C7\u03B5\u1F7B\u03C9, \u03BC\u03BF\u03B9\u03C7\u03B1\u03BB\u1F77\u03C2, \u03BC\u03BF\u03B9\u03C7\u1F79\u03C2",
				04000503223, 04001900917, 04101001123, 04101001210));
			string sGrkMoicheia = "\u03BC\u03BF\u03B9\u03C7\u03B5\u1F77\u03B1";
			terms.KeyTerms.Add(new Term(1140, "KT", "\u03BC\u03BF\u03B9\u03C7\u1F71\u03BF\u03BC\u03B1\u03B9-2",
				"Greek", "adultery", sGrkMoicheia,
				"\u03BC\u03BF\u03B9\u03C7\u03B5\u1F77\u03B1, \u03BC\u03BF\u03B9\u03C7\u03B5\u1F7B\u03C9, \u03BC\u03BF\u03B9\u03C7\u03B1\u03BB\u1F77\u03C2, \u03BC\u03BF\u03B9\u03C7\u1F79\u03C2",
				04001501909, 04300800310));
			string sGrkMoicheuw = "\u03BC\u03BF\u03B9\u03C7\u03B5\u1F7B\u03C9";
			terms.KeyTerms.Add(new Term(1141, "KT", "\u03BC\u03BF\u03B9\u03C7\u1F71\u03BF\u03BC\u03B1\u03B9-3",
				"Greek", "commit adultery", sGrkMoicheuw,
				"\u03BC\u03BF\u03B9\u03C7\u03B5\u1F77\u03B1, \u03BC\u03BF\u03B9\u03C7\u03B5\u1F7B\u03C9, \u03BC\u03BF\u03B9\u03C7\u03B1\u03BB\u1F77\u03C2, \u03BC\u03BF\u03B9\u03C7\u1F79\u03C2",
				04000502705, 04000502708, 04000502815, 04001901812, 04101001907, 04201601810, 04201601817,
				04201802005, 04300800410, 04500202204, 04500202205, 04501300904, 05900201105, 05900201113,
				06600202208));
			string sGrkMoichalis = "\u03BC\u03BF\u03B9\u03C7\u03B1\u03BB\u1F77\u03C2";
			terms.KeyTerms.Add(new Term(1142, "KT", "\u03BC\u03BF\u03B9\u03C7\u1F71\u03BF\u03BC\u03B1\u03B9-4",
				"Greek", "adulterous; adulteress", sGrkMoichalis,
				"\u03BC\u03BF\u03B9\u03C7\u03B5\u1F77\u03B1, \u03BC\u03BF\u03B9\u03C7\u03B5\u1F7B\u03C9, \u03BC\u03BF\u03B9\u03C7\u03B1\u03BB\u1F77\u03C2, \u03BC\u03BF\u03B9\u03C7\u1F79\u03C2",
				04001203909, 04001600404, 04100803815, 04500700306, 04500700326, 05900400401, 06100201404));
			string sGrkMoichos = "\u03BC\u03BF\u03B9\u03C7\u1F79\u03C2";
			terms.KeyTerms.Add(new Term(1143, "KT", "\u03BC\u03BF\u03B9\u03C7\u1F71\u03BF\u03BC\u03B1\u03B9-5",
				"Greek", "adulterer", sGrkMoichos,
				"\u03BC\u03BF\u03B9\u03C7\u03B5\u1F77\u03B1, \u03BC\u03BF\u03B9\u03C7\u03B5\u1F7B\u03C9, \u03BC\u03BF\u03B9\u03C7\u03B1\u03BB\u1F77\u03C2, \u03BC\u03BF\u03B9\u03C7\u1F79\u03C2",
				04201801122, 04600600917));

			List<BiblicalTermsLocalization> localizations = new List<BiblicalTermsLocalization>(1);

			ICmPossibilityList newBiblicalTermsList = new CmPossibilityList();
			lp.CheckListsOC.Add(newBiblicalTermsList);
			DummyTeKeyTermsInit.CallLoadKeyTerms(newBiblicalTermsList, terms, localizations);

			ICmPossibility newKtList = null;
			foreach (ICmPossibility category in newBiblicalTermsList.PossibilitiesOS)
			{
				if (category.Abbreviation.GetAlternative(m_wsEn) == "KT")
					newKtList = category;
			}
			Assert.IsNotNull(newKtList);

			// Now check to make sure FindCorrespondingChkRefs works
			List<IChkRef> chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoichalis045007003);
			Assert.AreEqual(2, chkRefs.Count);
			Assert.AreEqual(sGrkMoichalis, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(045007003, chkRefs[0].Ref);
			Assert.AreEqual(6, chkRefs[0].Location);
			Assert.AreEqual(sGrkMoichalis, chkRefs[1].KeyWord.Text);
			Assert.AreEqual(045007003, chkRefs[1].Ref);
			Assert.AreEqual(26, chkRefs[1].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoichalis061002014);
			Assert.AreEqual(1, chkRefs.Count);
			Assert.AreEqual(sGrkMoichalis, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(061002014, chkRefs[0].Ref);
			Assert.AreEqual(4, chkRefs[0].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoichaomai040005032);
			Assert.AreEqual(1, chkRefs.Count);
			Assert.AreEqual(sGrkMoichaomai, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(040005032, chkRefs[0].Ref);
			Assert.AreEqual(23, chkRefs[0].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoichaomai040019009);
			Assert.AreEqual(1, chkRefs.Count);
			Assert.AreEqual(sGrkMoichaomai, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(040019009, chkRefs[0].Ref);
			Assert.AreEqual(17, chkRefs[0].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoicheia040015019);
			Assert.AreEqual(1, chkRefs.Count);
			Assert.AreEqual(sGrkMoicheia, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(040015019, chkRefs[0].Ref);
			Assert.AreEqual(9, chkRefs[0].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoicheuw040005027);
			Assert.AreEqual(2, chkRefs.Count);
			Assert.AreEqual(sGrkMoicheuw, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(040005027, chkRefs[0].Ref);
			Assert.AreEqual(5, chkRefs[0].Location);
			Assert.AreEqual(sGrkMoicheuw, chkRefs[1].KeyWord.Text);
			Assert.AreEqual(040005027, chkRefs[1].Ref);
			Assert.AreEqual(8, chkRefs[1].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoicheuw040005028);
			Assert.AreEqual(1, chkRefs.Count);
			Assert.AreEqual(sGrkMoicheuw, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(040005028, chkRefs[0].Ref);
			Assert.AreEqual(15, chkRefs[0].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoichos042018011);
			Assert.AreEqual(1, chkRefs.Count);
			Assert.AreEqual(sGrkMoichos, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(042018011, chkRefs[0].Ref);
			Assert.AreEqual(22, chkRefs[0].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoichos046006009);
			Assert.AreEqual(1, chkRefs.Count);
			Assert.AreEqual(sGrkMoichos, chkRefs[0].KeyWord.Text);
			Assert.AreEqual(046006009, chkRefs[0].Ref);
			Assert.AreEqual(17, chkRefs[0].Location);

			chkRefs = ChkRefMatcher.FindCorrespondingChkRefs(newKtList,
				chkrefMoichos058013004);
			Assert.AreEqual(0, chkRefs.Count, "We removed this reference from the new list to test the case where no match is found");
		}
	}
}
