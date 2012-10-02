// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewTestBase.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------

using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that need a draft view (inside a form) showing the Exodus test
	/// data.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public abstract class DraftViewTestBase : TeTestBase
	{
		#region Member variables
		/// <summary>The form that holds the DraftView</summary>
		protected DummyDraftViewForm m_draftForm;
		/// <summary>The DraftView</summary>
		protected DummyDraftView m_draftView;
		/// <summary>A ScrBook containing the Exodus test data</summary>
		protected IScrBook m_exodus;

		private bool m_saveSegmentedBT;
		private bool m_saveShowPrompts;
		#endregion

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the ScrReference for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().RegisterViewTypeId<TeParaCounter>((int)TeViewGroup.Scripture);

			FileUtils.Manager.SetFileAdapter(new MockFileOS());
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			FileUtils.Manager.Reset();
			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the dummy DraftView
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			// Save value of user prompt setting - restored in Cleanup.
			m_saveShowPrompts = Options.ShowEmptyParagraphPromptsSetting;
			Options.ShowEmptyParagraphPromptsSetting = false;
			m_saveSegmentedBT = Options.UseInterlinearBackTranslation;
			Options.UseInterlinearBackTranslation = CreateSegmentedBt;

			Debug.Assert(m_draftForm == null, "m_draftForm is not null.");
			//if (m_draftForm != null)
			//	m_draftForm.Dispose();
			m_draftForm = new DummyDraftViewForm(Cache);
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache, CreateBtDraftView);

			m_draftView = m_draftForm.DraftView;
			m_draftView.Width = 300;
			m_draftView.Height = 290;
			m_draftView.CallOnLayout();

			// Set the selection at the start of the view
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up the dummy DraftView
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_draftView = null;
			m_draftForm.Close();
			m_draftForm = null;

			base.TestTearDown();

			// Restore prompt setting
			Options.ShowEmptyParagraphPromptsSetting = m_saveShowPrompts;
			Options.UseInterlinearBackTranslation = m_saveSegmentedBT;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the Exodus test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			if (CreateTheExodusData)
			{
				m_exodus = CreateExodusData();
				m_exodus.BookIdRA.BookName.SetUserWritingSystem("Exodus");
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to create the draft view showing back translation
		/// data or not. (Default is false)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool CreateBtDraftView
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to simulate a segmented (interlinear) back
		/// translation. (Default is false)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool CreateSegmentedBt
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to create the Exodus test data or not. (Default is true)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool CreateTheExodusData
		{
			get { return true; }
		}
		#endregion

		#region Protected helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the requested selection in the first (0th) m_exodus.
		/// </summary>
		/// <param name="iSection">The expected section index (in book 0).</param>
		/// <param name="flidSectionText">ScrSectionTags.kflidHeading or
		/// ScrSectionTags.kflidContent</param>
		/// <param name="iPara">The expected paragraph index.</param>
		/// <param name="ichAnchor">The character offset.</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyRequestedSelection(int iSection, int flidSectionText, int iPara,
			int ichAnchor)
		{
			VerifyRequestedSelection(0, iSection, flidSectionText, iPara, ichAnchor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the requested selection.
		/// </summary>
		/// <param name="iBook">The expected book index.</param>
		/// <param name="iSection">The expected section index.</param>
		/// <param name="flidSectionText">ScrSectionTags.kflidHeading or
		/// ScrSectionTags.kflidContent</param>
		/// <param name="iPara">The expected paragraph index.</param>
		/// <param name="ichAnchor">The character offset.</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyRequestedSelection(int iBook, int iSection, int flidSectionText,
			int iPara, int ichAnchor)
		{
			SelectionHelper helper = m_draftView.RequestedSelectionAtEndOfUow;
			Assert.IsNotNull(helper);
			Assert.IsFalse(helper.IsRange, "Selection should not be a range.");
			Assert.AreEqual(4, helper.NumberOfLevels, "Unexpected number of levels");
			Assert.AreEqual(iBook, helper.LevelInfo[3].ihvo, "Unexpected book"); // Book
			Assert.AreEqual(ScrBookTags.kflidSections, helper.LevelInfo[2].tag, "Unexpected tag for book");
			Assert.AreEqual(iSection, helper.LevelInfo[2].ihvo, "Unexpected section");
			Assert.AreEqual(flidSectionText, helper.LevelInfo[1].tag, "Unexpected tag for section");
			Assert.AreEqual(StTextTags.kflidParagraphs, helper.LevelInfo[0].tag, "Unexpected tag for para");
			Assert.AreEqual(iPara, helper.LevelInfo[0].ihvo, "Unexpected para index");
			Assert.AreEqual(StTxtParaTags.kflidContents, helper.TextPropId);
			Assert.AreEqual(ichAnchor, helper.IchAnchor, "Unexpected character index");
			m_draftView.RequestedSelectionAtEndOfUow = null; // Needs to be clear for the next time
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the requested back translation (non-segmented) selection.
		/// </summary>
		/// <param name="iBook">The expected book index.</param>
		/// <param name="iSection">The expected section index.</param>
		/// <param name="flidSectionText">ScrSectionTags.kflidHeading or
		/// ScrSectionTags.kflidContent</param>
		/// <param name="iPara">The expected paragraph index.</param>
		/// <param name="ichAnchor">The character offset.</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyRequestedBTSelection(int iBook, int iSection, int flidSectionText,
			int iPara, int ichAnchor)
		{
			SelectionHelper helper = m_draftView.RequestedSelectionAtEndOfUow;
			Assert.IsNotNull(helper);
			Assert.IsFalse(helper.IsRange, "Selection should not be a range.");
			Assert.AreEqual(5, helper.NumberOfLevels, "Unexpected number of levels");
			Assert.AreEqual(iBook, helper.LevelInfo[4].ihvo, "Unexpected book"); // Book
			Assert.AreEqual(ScrBookTags.kflidSections, helper.LevelInfo[3].tag, "Unexpected tag for book");
			Assert.AreEqual(iSection, helper.LevelInfo[3].ihvo, "Unexpected section");
			Assert.AreEqual(flidSectionText, helper.LevelInfo[2].tag, "Unexpected tag for section");
			Assert.AreEqual(StTextTags.kflidParagraphs, helper.LevelInfo[1].tag, "Unexpected tag for para");
			Assert.AreEqual(iPara, helper.LevelInfo[1].ihvo, "Unexpected para index");
			Assert.AreEqual(-1, helper.LevelInfo[0].tag, "Unexpected tag for translations");
			Assert.AreEqual(CmTranslationTags.kflidTranslation, helper.TextPropId);
			Assert.AreEqual(ichAnchor, helper.IchAnchor, "Unexpected character index");
			m_draftView.RequestedSelectionAtEndOfUow = null; // Needs to be clear for the next time
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the requested selection is at the start of the first book's title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void VerifyRequestedSelectionAtStartOfFirstBookTitle()
		{
			VerifyRequestedSelectionInBookTitle(0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the requested selection is at the start of the first book's title.
		/// </summary>
		/// <param name="iPara">The expected paragraph index.</param>
		/// <param name="ichAnchor">The character offset.</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyRequestedSelectionInBookTitle(int iPara, int ichAnchor)
		{
			VerifyRequestedSelectionInBookTitle(0, iPara, ichAnchor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the requested selection is at the start of the first book's title.
		/// </summary>
		/// <param name="iBook">The expected book index.</param>
		/// <param name="iPara">The expected paragraph index.</param>
		/// <param name="ichAnchor">The character offset.</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyRequestedSelectionInBookTitle(int iBook, int iPara, int ichAnchor)
		{
			SelectionHelper helper = m_draftView.RequestedSelectionAtEndOfUow;
			Assert.IsNotNull(helper);
			Assert.IsFalse(helper.IsRange);
			Assert.AreEqual(3, helper.NumberOfLevels);
			Assert.AreEqual(iBook, helper.LevelInfo[2].ihvo); // Book
			Assert.AreEqual(ScrBookTags.kflidTitle, helper.LevelInfo[1].tag);
			Assert.AreEqual(StTextTags.kflidParagraphs, helper.LevelInfo[0].tag);
			Assert.AreEqual(iPara, helper.LevelInfo[0].ihvo);
			Assert.AreEqual(ichAnchor, helper.IchAnchor);
			m_draftView.RequestedSelectionAtEndOfUow = null; // Needs to be clear for the next time
		}
		#endregion
	}
}
