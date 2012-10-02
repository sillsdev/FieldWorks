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
// File: VectorTests.cs
// Responsibility: JohnH, RandyR
// Last reviewed:
//
// <remarks>
// Implements VectorTests.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Notebk;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region Vector tests that use real database - DON'T ADD TESTS HERE
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Vector tests that make use of the database
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class VectorTestsWithRealDb_DONTADDTESTSHERE : InDatabaseFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test transactions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("SmokeTest")]
		public void FDOCompetingTransactions()
		{
			CheckDisposed();

			FdoCache a = FdoCache.Create("TestlangProj");
			AddWord(a);

			UndoResult ures_a = 0;
			while (a.CanUndo)
			{
				a.Undo(out ures_a);
				if (ures_a == UndoResult.kuresFailed  || ures_a == UndoResult.kuresError)
					Assert.Fail("ures should not be == " + ures_a.ToString());
			}
			a.Dispose();


			FdoCache b = FdoCache.Create("TestlangProj");
			AddWord(b);

			UndoResult ures_b = 0;
			while (b.CanUndo)
			{
				b.Undo(out ures_b);
				if (ures_b == UndoResult.kuresFailed  || ures_b == UndoResult.kuresError)
					Assert.Fail("ures should not be == " + ures_b.ToString());
			}
			b.Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding a word
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// ------------------------------------------------------------------------------------
		protected void AddWord(FdoCache cache)
		{
			const string kWordForm = "FDOCompetingTransactions";
			ILangProject lp = cache.LangProject;
			IWfiWordform word = WfiWordform.FindOrCreateWordform(m_fdoCache, kWordForm, lp.DefaultVernacularWritingSystem);
			word.AnalysesOC.Add(new WfiAnalysis());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the form of wordform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFormOfWordform()
		{
			CheckDisposed();

			int hvo = m_fdoCache.LangProject.WordformInventoryOA.WordformsOC.HvoArray[0];//9749; //"ak"
			IWfiWordform w = WfiWordform.CreateFromDBObject(m_fdoCache, hvo);
			String s = w.Form.VernacularDefaultWritingSystem;
			Assert.IsNotNull(s);
			Assert.IsTrue(s.Length > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test adding analysis.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestFDOAnalysisAddRemove()
		{
			CheckDisposed();

			const string kWordForm = "ak";
			ILangProject lp = m_fdoCache.LangProject;
			IWfiWordform word = WfiWordform.FindOrCreateWordform(m_fdoCache, kWordForm, lp.DefaultVernacularWritingSystem);
			word.AnalysesOC.Add(new WfiAnalysis());

			int[] hvos = word.AnalysesOC.HvoArray;
			foreach(int hvo in hvos)
				word.AnalysesOC.Remove(hvo);
			Assert.AreEqual(0, word.AnalysesOC.Count, "Analyses removal failed.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the undo of move objects to own ord0. Jira issue is TE-4969
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This test appears to not restore the data to the pre-test state, as running it twice will fail the second time.")]
		public void TestUndoOfDeleteObjAfterMovingObjectsToOwnOrd0()
		{
			CheckDisposed();

			// This now no longer compiles. Either this test needs to be moved over into ScrFDOTests
			// or (better) it needs to be designed so that it not Scripture-dependent. If enough
			// pieces of FDO get moved to their own assemblies, eventually we may need a way to test
			// some general-purpose FDO stuff AFTER all the components have been built.\

			//IScrBook phm = (IScrBook)m_scr.ScriptureBooksOS[0];
			//IScrSection section1OfPhm = (IScrSection)phm.SectionsOS[0];
			//IStText contentS1 = section1OfPhm.ContentOA;
			//IStTxtPara para1 = (IStTxtPara)contentS1.ParagraphsOS[0];
			//IScrSection section2OfPhm = (IScrSection)phm.SectionsOS[1];
			//IStText contentS2 = section2OfPhm.ContentOA;
			//IStTxtPara para2 = (IStTxtPara)contentS2.ParagraphsOS[0];

			//// Step 1: Move a paragraph to the start of a new sequence, giving it an ownord of 0.
			//m_fdoCache.MoveOwningSequence(contentS1.Hvo, para1.OwningFlid, 0, 0, contentS2.Hvo,
			//    para1.OwningFlid, 0);
			//para1 = new StTxtPara(m_fdoCache, para1.Hvo, true, true); // Force reload
			//Assert.AreEqual(para1.Hvo, contentS2.ParagraphsOS.HvoArray[0]);
			//Assert.AreEqual(para2.Hvo, contentS2.ParagraphsOS.HvoArray[1]);
			//Assert.AreEqual(0, para1.OwnOrd);

			//// Step 2: Now delete the paragraph we just moved.
			//m_fdoCache.DeleteObject(para1.Hvo);
			//para2 = new StTxtPara(m_fdoCache, para2.Hvo, true, true); // Force reload
			//Assert.AreEqual(para2.Hvo, contentS2.ParagraphsOS.HvoArray[0]);
			//Assert.AreEqual(1, para2.OwnOrd);

			//// Undo and make sure we get back to our starting condition
			//Assert.IsTrue(m_fdoCache.CanUndo);
			//UndoResult ures;
			//m_fdoCache.Undo(out ures);
			//Assert.AreEqual(UndoResult.kuresRefresh, ures);
			//Assert.AreEqual(para1.Hvo, contentS1.ParagraphsOS.HvoArray[0]);
		}
	}
	#endregion

	#region Vector tests that use in-memory cache/database
	/// <summary>
	/// Implements various tests on vectors which will be performed by NUnit
	/// </summary>
	[TestFixture]
	public class VectorTests : InMemoryFdoTestBase
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


		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test the 'Count' vector method on a collection.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void VectorCount()
		{
			CheckDisposed();

			Cache.LangProject.LexDbOA.EntriesOC.Add(new LexEntry());
			ILexEntry entry = Cache.LangProject.LexDbOA.EntriesOC.Add(new LexEntry());
			int iS = entry.AlternateFormsOS.Count;
			//jdh NOv 2002 changed this	because who knows if this is empty not
			//iS = m_fdoCache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Count;

			Assert.AreEqual(0, iS, "Wrong count.");

			//cleanup
			Cache.LangProject.LexDbOA.EntriesOC.Remove(entry);

			// This should be greater than 0.
			iS = Cache.LangProject.LexDbOA.EntriesOC.Count;
			Assert.IsTrue(0 != iS, "Wrong count.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test removing things from a vector.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VectorRemove_OwningCollection()
		{
			CheckDisposed();

			// Remove an entry from owning collection.
			FdoOwningCollection<ILexEntry> oc = Cache.LangProject.LexDbOA.EntriesOC;
			oc.Add(new LexEntry());
			oc.Add(new LexEntry());
			oc.Add(new LexEntry());

			int[] vhvo = oc.HvoArray;
			oc.Remove(vhvo[1]);

			Assert.AreEqual(2, oc.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test removing things from a vector.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VectorRemove_ReferenceCollection()
		{
			CheckDisposed();

			ILexDb ldb = Cache.LangProject.LexDbOA;
			ILexEntry lme = ldb.EntriesOC.Add(new LexEntry());
			ILexSense ls = lme.SensesOS.Append(new LexSense());
			FdoOwningSequence<ICmPossibility> usageSeq = ldb.UsageTypesOA.PossibilitiesOS;
			usageSeq.Append(new CmPossibility());
			usageSeq.Append(new CmPossibility());
			usageSeq.Append(new CmPossibility());

			int[] usageTypes = new int[3];
			usageTypes[0] = usageSeq[0].Hvo;
			usageTypes[1] = usageSeq[1].Hvo;
			usageTypes[2] = usageSeq[2].Hvo;
			ls.UsageTypesRC.Add(usageTypes);

			ls.UsageTypesRC.Remove(usageSeq[1].Hvo);

			Assert.AreEqual(2, ls.UsageTypesRC.Count);
		}


		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test adding things to a owning collection.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void FdoOwningCollection_AddingNull()
		{
			CheckDisposed();

			FdoOwningCollection<IMoAdhocProhib> oc = Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC;
			// Should throw a null argument exception.
			IMoAdhocProhib p = null;
			oc.Add(p);
		}

		/// <summary>
		/// Test adding things to a owning collection.
		/// </summary>
		[Test]
		public void FdoOwningCollection_AddToOC()
		{
			CheckDisposed();

			FdoOwningCollection<IMoAdhocProhib> oc = Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC;
			MoAlloAdhocProhib acp = (MoAlloAdhocProhib)oc.Add(new MoAlloAdhocProhib());
			oc.Add(acp);	// Try adding it again.
			// This should work, but it won't actually be moved anywhere.
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test adding things to a owning collection.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(Exception))]
		public void FdoOwningCollection_AddToOC_WithUnownedId()
		{
			CheckDisposed();

			FdoOwningCollection<IMoAdhocProhib> oc = Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC;
			MoAlloAdhocProhib acp = new MoAlloAdhocProhib();

			// This should fail, since it has an unowned id in the array.
			int[] ahvo = new int[2];
			ahvo[0] = acp.Hvo;
			ahvo[1] = 1;	// Bad ID, since it isn't owned in source flid.
			oc.Add(ahvo);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test adding things to a owning collection.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void FdoOwningCollection_AddToOCFromOS()
		{
			CheckDisposed();

			// Now test to see if something can actually be moved.
			// We will use subrecords in a RN object for this.
			// The source flid is a sequence, while the destination is a collection.
			m_inMemoryCache.InitializeDataNotebook();
			IRnResearchNbk rn = Cache.LangProject.ResearchNotebookOA;
			IRnEvent ev = (IRnEvent)rn.RecordsOC.Add(new RnEvent());
			ev.SubRecordsOS.Append(new RnEvent());
			ev.SubRecordsOS.Append(new RnEvent());
			ev.SubRecordsOS.Append(new RnEvent());

			FdoOwningSequence<IRnGenericRec> os = ev.SubRecordsOS;
			int[] ahvoSrcBefore = (int[])os.HvoArray.Clone();
			FdoOwningCollection<IRnGenericRec> oc = Cache.LangProject.ResearchNotebookOA.RecordsOC;
			int[] ahvoDstBefore = (int[])oc.HvoArray.Clone();
			oc.Add(os.HvoArray);

			// Check data.
			Assert.AreEqual(0, os.HvoArray.Length);	// Not empty, so they didn't all get moved.
			Assert.AreEqual((ahvoSrcBefore.Length + ahvoDstBefore.Length), oc.HvoArray.Length); // Not the same size, so they didn't get moved.

			// Make sure everything in ahvoDstBefore is still in cv.HvoArray.
			// If not, then the test failed.
			foreach(int i in ahvoDstBefore)
			{
				bool fFailed = true;
				foreach(int j in oc.HvoArray)
				{
					if (i == j)
					{
						fFailed = false;
						break;
					}
				}
				Assert.IsFalse(fFailed);
			}
			// Make sure everything in ahvoSrcBefore made it into cv.HvoArray.
			// If not, then the test failed.
			foreach(int i in ahvoSrcBefore)
			{
				bool fFailed = true;
				foreach(int j in oc.HvoArray)
				{
					if (i == j)
					{
						fFailed = false;
						break;
					}
				}
				Assert.IsFalse(fFailed);
			}
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// This test is here to check on a collection with a special signature, that is, CmObject.
		/// the current generator must deal with this signature as a special case, because it is not
		/// part of any model.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void SignatureOfCmObject_VectorAccessor()//jdh 9Oct2002
		{
			CheckDisposed();

			IText itext = m_inMemoryCache.AddInterlinearTextToLangProj("My Interlinear Text");
			IStTxtPara para = m_inMemoryCache.AddParaToInterlinearTextContents(itext, "Once upon a time...");

			FdoOwningSequence<ICmObject> os = para.AnalyzedTextObjectsOS;
			//that's it.  This will fail if the code generator generated a bogus fully qualified signature.
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test adding things to a reference collection.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void FdoReferenceCollection()
		{
			CheckDisposed();

			ILexDb ldb = Cache.LangProject.LexDbOA;
			ldb.EntriesOC.Add(new LexEntry());
			ldb.EntriesOC.Add(new LexEntry());

			// Gather up all entries in the DB.
			int iLESize = Cache.GetVectorSize(ldb.Hvo,
				(int)LexDb.LexDbTags.kflidEntries);
			FdoOwningCollection<ILexEntry> ocvLexEntriesOC = ldb.EntriesOC;
			int[] ahvoLexEntriesOC = ocvLexEntriesOC.HvoArray;
			// Check sizes. They should be the same.
			Assert.AreEqual(iLESize, ahvoLexEntriesOC.Length, "Mis-matched number of entries.");
			FdoReferenceCollection<ILexEntry> rcLexEntriesRCBefore = ldb.LexicalFormIndexRC;
			int[] ahvoLexEntriesRCBefore = rcLexEntriesRCBefore.HvoArray;
			int iOldRCSize = ahvoLexEntriesRCBefore.Length;
			// Add all entries to reference collection.
			rcLexEntriesRCBefore.Add(ahvoLexEntriesOC);
			// Make sure they are there now.
			FdoReferenceCollection<ILexEntry> rcLexEntriesRCAfter = ldb.LexicalFormIndexRC;
			int[] ahvoLexEntriesRCAfter = rcLexEntriesRCAfter.HvoArray;
			Assert.AreEqual((ahvoLexEntriesOC.Length + iOldRCSize), ahvoLexEntriesRCAfter.Length, "Mis-matched number of entries in reference collection.");
			// Size of ahvoLexEntriesRCAfter is right, so quit.
			// Note: One could check the IDs, but it probably isn't needed,
			// as long as nobody else was messing with database at the same time.

			// Try adding a duplicate item to reference collection.
			// The size should be the same before as after.
			rcLexEntriesRCAfter.Add(ahvoLexEntriesOC[0]);
			Assert.AreEqual(ahvoLexEntriesRCAfter.Length, rcLexEntriesRCAfter.HvoArray.Length, "Mis-matched number of entries in reference collection.");
		}

		/// <summary>
		/// </summary>
		[Test]
		public void GetFirstItemOfSequence()
		{
			CheckDisposed();

			//first try with an empty sequence
			ILexEntry entry = Cache.LangProject.LexDbOA.EntriesOC.Add(new LexEntry());
			Assert.IsNull(entry.AlternateFormsOS.FirstItem);
			//cleanup
			Cache.LangProject.LexDbOA.EntriesOC.Remove(entry);

			ILgWritingSystem x = Cache.LangProject.CurAnalysisWssRS.FirstItem;
			Assert.AreEqual(Cache.DefaultAnalWs, x.Hvo);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void ContainsObject()
		{
			CheckDisposed();

			ILexEntry entry = Cache.LangProject.LexDbOA.EntriesOC.Add(new LexEntry());
			Assert.IsTrue(Cache.LangProject.LexDbOA.EntriesOC.Contains(entry.Hvo));
			Assert.IsTrue(Cache.LangProject.LexDbOA.EntriesOC.Contains(entry));

			// look for something that isn't there
			Assert.IsFalse(Cache.LangProject.LexDbOA.LexicalFormIndexRC.Contains(entry));

			//cleanup
			Cache.LangProject.LexDbOA.EntriesOC.Remove(entry);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void GetVectorSize_on_New_Sequence()
		{
			CheckDisposed();

			IText itext = m_inMemoryCache.AddInterlinearTextToLangProj("itName", false);
			IStText text = itext.ContentsOA = new StText();
			IStTxtPara newPara = (IStTxtPara)text.ParagraphsOS.Append(new StTxtPara());

			int chvoParas = Cache.GetVectorSize(text.Hvo,
				(int )SIL.FieldWorks.FDO.Cellar.StText.StTextTags.kflidParagraphs);

			Assert.AreEqual(1, chvoParas);
			Assert.AreEqual(text.ParagraphsOS.Count, chvoParas);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test the 'ReallyReallyAllPossibilities' method on a possibility list.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void GetAllPossibilities()
		{
			CheckDisposed();

			// We use the following hierarchy:
			// top - 1 -  1.1 - 1.1.1
			//     \ 2 \- 1.2
			//          \ 1.3
			// which are 7 CmPossibility objects
			ICmPossibility top = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Append(new CmPossibility());
			ICmPossibility one = top.SubPossibilitiesOS.Append(new CmPossibility());					// 1
			top.SubPossibilitiesOS.Append(new CmPossibility()); // 2
			ICmPossibility oneone = one.SubPossibilitiesOS.Append(new CmPossibility());				// 1.1
			one.SubPossibilitiesOS.Append(new CmPossibility()); // 1.2
			one.SubPossibilitiesOS.Append(new CmPossibility()); // 1.3
			oneone.SubPossibilitiesOS.Append(new CmPossibility()); // 1.1.1

			Set<ICmPossibility> al = Cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities;
			Assert.AreEqual(7, al.Count, "Wrong number of possibilites.");
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="LgWritingSystemCollection.GetWsFromIcuLocale"/> method.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void GetWsFromIcuLocale()
		{
			CheckDisposed();

			LgWritingSystemCollection analWritingSystems = Cache.LanguageEncodings;
			ILgWritingSystem lgws = analWritingSystems.Item(0);
			// TODO (DN-208) This needs to switch to just use the hvo.
			Assert.AreEqual(lgws.Hvo, analWritingSystems.GetWsFromIcuLocale(lgws.ICULocale));
			Assert.AreEqual(0, analWritingSystems.GetWsFromIcuLocale("not_THERE"));
		}
	}
	#endregion
}
