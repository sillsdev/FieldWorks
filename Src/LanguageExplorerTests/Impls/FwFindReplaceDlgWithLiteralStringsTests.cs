// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using FieldWorks.TestUtilities;
using NUnit.Framework;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.Impls
{
	/// <summary>
	/// Tests for Find/Replace dialog when view includes literal strings (such as labels in the
	/// TE Notes view) that are added by the view constructor.
	/// </summary>
	[TestFixture]
	public class FwFindReplaceDlgWithLiteralStringsTests : FwFindReplaceDlgBaseTests
	{
		/// <summary>
		/// Create the dialog
		/// </summary>
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_vwRootsite.MyDisplayType = DisplayType.kNormal | DisplayType.kLiteralStringLabels;
		}

		#region Find tests
		/// <summary>
		/// Test an initial search when finding a next match when the selection is in a literal
		/// string added by the VC.
		/// </summary>
		[Test]
		public void FindFromLiteralString()
		{
			// There is a string added by the VC at the beginning of our view.
			m_vwRootsite.RootBox.MakeSimpleSel(true, false, false, true);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false, null, null, null);
			m_dlg.FindText = TsStringUtils.MakeString("Blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 0, 4);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
		}

		/// <summary>
		/// Test when finding all matches when the selection is in a literal string added by
		/// the VC. After wrapping and getting back around to starting location, we should
		/// inform the user.
		/// </summary>
		[Test]
		public void FindFromLiteralString_StopWhenPassedLimit()
		{
			// There is a string added by the VC at the beginning of our view.
			m_vwRootsite.RootBox.MakeSimpleSel(true, false, false, true);
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
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 0, 17);
		}
		#endregion
	}
}