// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CopyObjectTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for CopyObject FDO domain service. All of the following tests test the method
	/// CloneFDOObjects in FDO.DomainServices.CopyObject (which is a static object).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CopyObjectTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Member Variables

		private IStTxtPara m_txtPara;
		private IStText m_stTxt;
		private IText m_txt;
		private IFdoServiceLocator m_servLoc;
		private ILgWritingSystemFactory m_wsf;
		private int m_ws_en;
		private int m_ws_fr;

		#endregion

		#region Fixture Setup Methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_servLoc = Cache.ServiceLocator;
			m_wsf = Cache.WritingSystemFactory;
			m_ws_en = m_wsf.GetWsFromStr("en");
			m_ws_fr = m_wsf.GetWsFromStr("fr");

			CreateTestText();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a text for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateTestText()
		{
			m_txt = m_servLoc.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(m_txt);
			m_stTxt = m_servLoc.GetInstance<IStTextFactory>().Create();
			m_txt.ContentsOA = m_stTxt;
			m_txtPara = m_txt.ContentsOA.AddNewTextPara(null);

			// 0         1         2         3         4
			// 0123456789012345678901234567890123456789012
			// This is a test string for CopyObject tests.

			int hvoVernWs = Cache.DefaultVernWs;
			m_txtPara.Contents = TsStringUtils.MakeTss("This is a test string for CopyObject tests.", hvoVernWs);
		}

		#endregion

		#region Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up an IDsConstChart for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IDsConstChart SetupTestConstChart()
		{
			var m_DsConstChartFactory = m_servLoc.GetInstance<IDsConstChartFactory>();
			var rowFact = m_servLoc.GetInstance<IConstChartRowFactory>();
			var tssFact = Cache.TsStrFactory;
			// easiest thing to put in a test chart are tags
			var tagFact = m_servLoc.GetInstance<IConstChartTagFactory>();
			var template = Cache.LangProject.GetDefaultChartTemplate();
			var possTags = Cache.LangProject.GetDefaultChartMarkers();
			var srcChart = m_DsConstChartFactory.Create(
				Cache.LangProject.DiscourseDataOA,
				m_stTxt,
				template);
			var row1 = rowFact.Create(srcChart, 0, tssFact.MakeString("1a", m_ws_en));
			var row2 = rowFact.Create(srcChart, 1, tssFact.MakeString("1b", m_ws_en));
			var row3 = rowFact.Create(srcChart, 2, tssFact.MakeString("1c", m_ws_en));
			tagFact.Create(row1, 0, template.SubPossibilitiesOS[0], possTags.SubPossibilitiesOS[0]);
			tagFact.Create(row1, 1, template.SubPossibilitiesOS[2], possTags.SubPossibilitiesOS[0]);
			tagFact.Create(row2, 0, template.SubPossibilitiesOS[1], possTags.SubPossibilitiesOS[0]);
			// Don't put any analyses in at this point, because we have trouble copying them!
			// And when we get analyses to copy, we won't need them here!
			return srcChart;
		}

		private static void VerifyWordform(IWfiWordform oldform, IWfiWordform newform)
		{
			// A couple of obvious checks
			Assert.IsNotNull(newform, "Copy failed!");
			Assert.AreNotEqual(oldform, newform, "Copy should be a different object.");

			Assert.AreEqual(oldform.Form.StringCount, newform.Form.StringCount);
			foreach (int ws in oldform.Form.AvailableWritingSystemIds)
				Assert.AreEqual(oldform.Form.get_String(ws), newform.Form.get_String(ws));
		}

		#endregion

		// ----------------------- Start Unit Tests here -----------------------------------------
		#region Unit Tests

		/// <summary>
		/// Test CloneFDOObjects for (owned) IStTxtPara
		/// </summary>
		[Test]
		public void CopyParagraph()
		{
			// Setup test
			// just record some info; source object already exists.
			IStTxtPara srcObj = m_stTxt[0];

			// SUT
			var newPara = CopyObject<IStTxtPara>.CloneFdoObject(srcObj, x => m_stTxt.ParagraphsOS.Add(x));
			var srcAsTxtPara = srcObj as IStTxtPara;

			// Verify results
			// A couple of obvious checks
			Assert.IsNotNull(newPara, "Copy failed!");
			Assert.AreNotEqual(srcObj.Hvo, newPara.Hvo, "Copy shouldn't have same Hvo as original!");

			// Check ClassID, Owner, OwningFlid
			Assert.AreEqual(srcObj.ClassID, newPara.ClassID, "Copy has different ClassID.");
			Assert.AreEqual(srcObj.OwningFlid, newPara.OwningFlid, "Copy has different OwningFlid.");
			Assert.IsNotNull(newPara.Owner, "Copy's owner is null.");
			Assert.AreEqual(srcObj.Owner.Hvo, newPara.Owner.Hvo, "Copy's owner is different.");

			// Check ParseIsCurrent and AnalyzedTextObjects
			Assert.AreEqual(srcAsTxtPara.ParseIsCurrent, newPara.ParseIsCurrent,
							"Copy has different ParseIsCurrent flag.");
			Assert.IsNotNull(newPara.AnalyzedTextObjectsOS);

			// Check Contents
			Assert.IsNotNull(newPara.Contents,"Copy is missing Contents.");
			var expectedText = srcAsTxtPara.Contents.Text;
			var actualText = newPara.Contents.Text;
			Assert.AreEqual(expectedText, actualText, "Copy has different Contents.");
		}

		/// <summary>
		/// Test CloneFDOObjects for owned DsConstChart
		/// This tests RS/RA properties
		/// </summary>
		[Test]
		public void CopyConstChart()
		{
			// Setup test
			var srcObj = SetupTestConstChart();

			// SUT
			var newChart = CopyObject<IDsConstChart>.CloneFdoObject(srcObj,
				x => Cache.LangProject.DiscourseDataOA.ChartsOC.Add(x));

			// Verify
			Assert.AreNotEqual(srcObj.Hvo, newChart.Hvo, "Shouldn't end up with same Hvo!");
			Assert.AreEqual(srcObj.ClassID, newChart.ClassID, "Should be of the same Class.");

			Assert.AreEqual(srcObj.BasedOnRA.Hvo, newChart.BasedOnRA.Hvo, "Copy is not 'BasedOn' our test IStText.");
			Assert.IsNotNull(newChart.RowsOS, "Copy has no rows!");
			Assert.AreEqual(srcObj.RowsOS.Count, newChart.RowsOS.Count, "Copy has different number of Rows.");

			for (var i = 0; i < srcObj.RowsOS.Count; i++) // Should be 3 rows (0 to 2)
			{
				Assert.IsNotNull(newChart.RowsOS[i].CellsOS, String.Format("Copy has no CellParts in Row[{0}].", i));
				Assert.AreEqual(srcObj.RowsOS[i].CellsOS.Count, newChart.RowsOS[i].CellsOS.Count,
					String.Format("Copy has different number of CellParts in Row[{0}].", i));
			}
		}

		/// <summary>
		/// Test CloneFDOObjects for unrelated collection of FDO Objects
		/// </summary>
		[Test]
		public void CopySeveralObjects_unrelated()
		{
			// Setup test
			var source = new List<ICmObject>(); // IEnumerable collection to feed to CopyObject

			ICmPossibilityList possList = m_servLoc.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LanguageProject.TimeOfDayOA = possList;

			possList.ListVersion = new Guid("5f145304-64cd-47ef-2001-d147ddda0492");
			possList.IsSorted = true;

			source.Add(possList);

			// Make a Wordform Object
			var wordform = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			wordform.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("rubbish", Cache.DefaultVernWs);
			source.Add(wordform);

			// SUT
			var newCollection = CopyObject<ICmObject>.CloneFdoObjects(source,
				x => Cache.LanguageProject.ThesaurusRA = (ICmPossibilityList)x);
			var newList = new List<ICmObject>(newCollection);

			// Verify results
			Assert.AreEqual(2, newList.Count, "Copied the wrong number of objects!");
			var newPoss = (ICmPossibilityList)newList[0];
			var newWordform = (IWfiWordform)newList[1];

			VerifyWordform(wordform, newWordform);

			Assert.AreNotEqual(possList, newPoss);
			Assert.AreEqual(possList.ListVersion, newPoss.ListVersion);
			Assert.AreEqual(possList.IsSorted, newPoss.IsSorted);
		}

		/// <summary>
		/// Test CloneFDOObjects for inter-related collection of FDO Objects
		/// Tests handling of references within and without the copyMap
		/// </summary>
		[Test]
		public void CopySeveralObjects_interrelated()
		{
			// Test Setup
			var source = new List<ICmObject>(); // IEnumerable collection to feed to CopyObject

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			source.Add(entry);

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);

			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			var statusList = Cache.LanguageProject.StatusOA;
			if (statusList == null)
			{
				statusList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				Cache.LanguageProject.StatusOA = statusList;
			}
			if (statusList.PossibilitiesOS.Count == 0)
			{
				var status = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				statusList.PossibilitiesOS.Add(status);
			}
			sense.StatusRA = statusList.PossibilitiesOS[0];

			// SUT
			var copyColl = CopyObject<ICmObject>.CloneFdoObjects(source,
				CopyObject<ICmObject>.kAddToSourceOwner);
			var newList = new List<ICmObject>();
			foreach (ICmObject obj in copyColl)
				newList.Add(obj);

			// Verify results
			Assert.AreEqual(1, newList.Count, "Wrong number of copied objects.");
			// Verify new TextSegment
			var newEntry = newList[0] as ILexEntry;
			Assert.IsNotNull(newEntry, "Root entry failed to copy.");
			Assert.AreNotEqual(entry, newEntry, "Copy of entry should not be the same object.");

			Assert.AreEqual(1, newEntry.SensesOS.Count, "We should have same number of senses on the copy.");
			var newSense = newEntry.SensesOS[0];
			Assert.AreNotEqual(sense, newSense, "Copy of sense should not be the same object.");

			var newMsa = newEntry.MorphoSyntaxAnalysesOC.ToArray()[0];
			Assert.AreNotEqual(msa, newMsa, "Copy of Msa should not be the same object.");

			Assert.AreEqual(newMsa, newSense.MorphoSyntaxAnalysisRA,"The copy of the sense should point to the copy of the msa.");
			Assert.AreEqual(statusList.PossibilitiesOS[0], newSense.StatusRA, "The copy of the sense should point to the uncopied status.");
		}

		#endregion
	}
}
