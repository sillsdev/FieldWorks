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
// File: ExcludeCheckingErrorsFilterTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExcludeCheckingErrorsFilterTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_genesis;
		private ExcludeCheckingErrorsFilter m_filter;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_filter = new ExcludeCheckingErrorsFilter(Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_genesis = AddBookWithTwoSections(1, "Genesis");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the filter excludes Scripture Checking errors that are not ignored.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ExcludeUnignoredErrors()
		{
			Assert.AreEqual(string.Empty, m_filter.FilterName);
			IScrScriptureNote note = AddAnnotation(
				(IStTxtPara)m_genesis.SectionsOS[0].ContentOA.ParagraphsOS[0], 01001001,
				NoteType.CheckingError);
			note.ResolutionStatus = NoteStatus.Open;
			Assert.IsFalse(m_filter.MatchesCriteria(note.Hvo));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the filter excludes Scripture Checking errors that are ignored but not
		/// commented.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ExcludeUncommentedErrors()
		{
			IScrScriptureNote note1 = AddAnnotation(
				(IStTxtPara)m_genesis.SectionsOS[0].ContentOA.ParagraphsOS[0], 01001001,
				NoteType.CheckingError);
			note1.ResolutionStatus = NoteStatus.Open;
			IScrScriptureNote note2 = AddAnnotation(
				(IStTxtPara)m_genesis.SectionsOS[0].ContentOA.ParagraphsOS[0], 01001002,
				NoteType.CheckingError);
			note2.ResolutionStatus = NoteStatus.Closed; // The same as an ignored error annotation

			Assert.IsFalse(m_filter.MatchesCriteria(note1.Hvo));
			Assert.IsFalse(m_filter.MatchesCriteria(note2.Hvo));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the filter includes commented Scripture Checking errors.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IncludeCommentedErrors()
		{
			IScrScriptureNote note1 = AddAnnotation(
				(IStTxtPara)m_genesis.SectionsOS[0].ContentOA.ParagraphsOS[0], 01001001,
				NoteType.CheckingError);
			note1.ResolutionStatus = NoteStatus.Open;
			IStTxtPara para1 = AddParaToMockedText(note1.ResolutionOA, ScrStyleNames.NormalParagraph);
			para1.Contents = Cache.TsStrFactory.MakeString("This is my comment.",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			IScrScriptureNote note2 = AddAnnotation(m_genesis.SectionsOS[0].ContentOA.ParagraphsOS[0], 01001002,
				NoteType.CheckingError);
			note2.ResolutionStatus = NoteStatus.Closed;
			IStTxtPara para2 = AddParaToMockedText(note2.ResolutionOA, ScrStyleNames.NormalParagraph);
			para2.Contents = Cache.TsStrFactory.MakeString("This is my comment.",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			Assert.IsTrue(m_filter.MatchesCriteria(note1.Hvo));
			Assert.IsTrue(m_filter.MatchesCriteria(note2.Hvo));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the filter includes translator and consultant notes.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IncludeNormalNotes()
		{
			IScrScriptureNote note1 = AddAnnotation(
				(IStTxtPara)m_genesis.SectionsOS[0].ContentOA.ParagraphsOS[0], 01001001,
				NoteType.Consultant);
			IScrScriptureNote note2 = AddAnnotation(
				(IStTxtPara)m_genesis.SectionsOS[0].ContentOA.ParagraphsOS[0], 01001002,
				NoteType.Translator);

			Assert.IsTrue(m_filter.MatchesCriteria(note1.Hvo));
			Assert.IsTrue(m_filter.MatchesCriteria(note2.Hvo));
		}
	}
}
