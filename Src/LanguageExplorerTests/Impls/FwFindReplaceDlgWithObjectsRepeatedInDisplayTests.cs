// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Runtime.InteropServices;
using FieldWorks.TestUtilities;
using NUnit.Framework;
using RootSite.TestUtilities;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.Impls
{
	/// <summary>
	/// Tests for Find/Replace dialog when view includes literal strings (such as labels in the
	/// TE Notes view) that are added by the view constructor.
	/// </summary>
	[TestFixture]
	public class FwFindReplaceDlgWithObjectsRepeatedInDisplayTests : ScrInMemoryLcmTestBase
	{
		#region Data members
		private const string m_kTitleText = "Blah, blah, blah!";
		private DummyFwFindReplaceDlg m_dlg;
		private DummyBasicView m_vwRootsite;
		private IVwPattern m_vwPattern;
		private IVwStylesheet m_Stylesheet;
		private IStText m_text;
		#endregion

		#region setup & teardown
		/// <summary>
		/// Create the dialog
		/// </summary>
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_vwPattern = VwPatternClass.Create();
			m_Stylesheet = new TestFwStylesheet();
			m_vwRootsite = new DummyBasicView
			{
				StyleSheet = m_Stylesheet,
				Cache = Cache,
				MyDisplayType = DisplayType.kNormal | DisplayType.kDuplicateParagraphs
			};
			m_vwRootsite.MakeRoot(m_text.Hvo, ScrBookTags.kflidTitle, 3);
			m_dlg = new DummyFwFindReplaceDlg();
		}

		/// <summary>
		/// Creates the test data.
		/// </summary>
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_text = AddTitleToMockedBook(AddBookToMockedScripture(1, "Genesis"), m_kTitleText, Cache.ServiceLocator.WritingSystemManager.UserWs);
		}

		/// <summary>
		/// Dispose of the dialog
		/// </summary>
		[TearDown]
		public override void TestTearDown()
		{
			base.TestTearDown();
			if (m_dlg != null)
			{
				if (m_dlg.IsHandleCreated)
				{
					m_dlg.Close();
				}
				m_dlg.Dispose();
				m_dlg = null;
			}
			if (m_vwRootsite != null)
			{
				m_vwRootsite.Dispose();
				m_vwRootsite = null;
			}
			if (m_vwPattern != null)
			{
				if (Marshal.IsComObject(m_vwPattern))
				{
					Marshal.ReleaseComObject(m_vwPattern);
				}
				m_vwPattern = null;
			}
			if (m_Stylesheet != null)
			{
				if (Marshal.IsComObject(m_Stylesheet))
				{
					Marshal.ReleaseComObject(m_Stylesheet);
				}
				m_Stylesheet = null;
			}
			m_text = null;
		}
		#endregion

		#region Find tests
		/// <summary>
		/// Test when finding matches in a view that has objects displayed more than once
		/// (like twice).
		/// </summary>
		[Test]
		public void FindNextWithDuplicateParagraphs()
		{
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false, null, null, null);
			m_dlg.FindText = TsStringUtils.MakeString("Blah, blah, blah!", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 1, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(1, 0, 0, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(1, 0, 1, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(1, 0, 2, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
		}
		#endregion
	}
}