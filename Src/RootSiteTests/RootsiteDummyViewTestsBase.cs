// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using FieldWorks.TestUtilities;
using LanguageExplorer.TestUtilities;
using RootSite.TestUtilities;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Base class for tests that use <see cref="DummyBasicView"/>
	/// </summary>
	public class RootsiteDummyViewTestsBase : ScrInMemoryLcmTestBase
	{
		/// <summary>The draft form</summary>
		protected DummyBasicView m_basicView;
		/// <summary />
		protected FlexComponentParameters _flexComponentParameters;

		/// <summary>
		/// Set up data that is constant for all tests in fixture
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				var book = AddBookToMockedScripture(1, "GEN");
				var title = AddTitleToMockedBook(book, "This is the title");
				AddFootnote(book, (IStTxtPara)title.ParagraphsOS[0], 0);
			});
		}

		/// <summary>
		/// Create a new basic view
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			var styleSheet = new LcmStyleSheet();

			styleSheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);

			Debug.Assert(m_basicView == null, "m_basicView is not null.");
			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();

			m_basicView = new DummyBasicView { Cache = Cache, Visible = false, StyleSheet = styleSheet };
			m_basicView.InitializeFlexComponent(_flexComponentParameters);
		}

		/// <summary>
		/// Close the draft view
		/// </summary>
		public override void TestTearDown()
		{
			m_basicView.Dispose();
			m_basicView = null;
			TestSetupServices.DisposeTrash(_flexComponentParameters);
			_flexComponentParameters = null;

			base.TestTearDown();
		}

		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		protected void ShowForm()
		{
			m_basicView.MyDisplayType = DisplayType.kBookTitle;

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = 307 - 25;
			m_basicView.MakeRoot(m_scr.ScriptureBooksOS[0].Hvo, ScrBookTags.kflidTitle);
			m_basicView.CallLayout();
		}
	}
}