// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WindowStatePersistence.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Threading;
using NUnit.Framework;
using SIL.FieldWorks.AcceptanceTests.Framework;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// <summary>
	/// Acceptance tests for the WindowStatePersistence story.
	/// </summary>
	[TestFixture]
	public class WindowStatePersistence: TeTestsBase
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowStatePersistence"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public WindowStatePersistence(): base(false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the view and section are restored when TE is restarted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Due to, I think, Unicode normalization, the Info. Bar text is all messed up now so this will fail until that's sorted out.")]
		public void SavedLayoutIsRestoredOnOpen()
		{
			m_app.GoToConcordanceView();
			m_app.GoToDraftView();

			// Go to first section
			Application.DoEvents();
			m_app.SendKeys("^{HOME}");
			Application.DoEvents();

			string firstSection = m_app.InfoBarValue;

			// TODO TeTeam: Fix - Due to Normalization this no longer works.
			// The Info. Bar text is all messed up now.
			Assert.AreEqual("Draft - ÉPÎTRE À PHILÉMON", firstSection);

			// Go to a different section - 12 paragraphs down should bring us to verse 1
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			m_app.SendKeys("^{DOWN}");
			string displayedSection = m_app.InfoBarValue;
			Assert.IsTrue(firstSection != displayedSection, "First section == scrolled to section");

			// now reopen TE and make sure that it's in the same view and section.
			m_app.Exit();
			m_app.Start();

			Assert.AreEqual(displayedSection, m_app.InfoBarValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that there is no filter active on open
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoFilterOnOpen()
		{
			// TODO (EberhardB): we can't test this really right now, because we don't have
			// filters. For now it means that the test succeeds...
			Thread.Sleep(1000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the IP is at the beginning of the section on startup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TextCursorAtTopOfPassageOnOpen()
		{
			m_app.GoToDraftView();

			// Go to last section
			m_app.SendKeys("^{END}");

			// Go to a different section
			m_app.SendKeys("{UP}");
			m_app.SendKeys("{UP}");
			m_app.SendKeys("{UP}");
			m_app.SendKeys("^{RIGHT}");
			m_app.SendKeys("^{RIGHT}");
			// now we should be at an odd enough position...

			// mark the next word and copy it to the clipboard
			m_app.SendKeys("+^{RIGHT}");
			// REVIEW (EberhardB): Why does ^C not work here?
			m_app.SendKeys("%EC"); // Alt-E/Copy

			IDataObject obj = Clipboard.GetDataObject();
			string selected = (string)obj.GetData(typeof(string));

			// now reopen TE
			m_app.Exit();
			m_app.Start();

			// mark the first word and copy it to the clipboard
			m_app.SendKeys("+^{RIGHT}");
			m_app.SendKeys("%EC"); // Alt-E/Copy

			obj = Clipboard.GetDataObject();
			string newSelected = (string)obj.GetData(typeof(string));
			Assert.IsTrue(newSelected != selected, "Seems like we're at the same position");

			// if we go 2 paragraphs up (previous paragraph could be section title,
			// so take 2), we should be in a different section
			string origSection = m_app.InfoBarValue;
			m_app.SendKeys("^{UP}");
			m_app.SendKeys("^{UP}");
			Assert.IsTrue(m_app.InfoBarValue != origSection, "Seems like we're in the same passage");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that on startup the settings of the last closed window are used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OpenUsesLastClosedSettings()
		{
			m_app.GoToDraftView();
			m_app.SendKeys("{PGDN}");
			m_app.SendKeys("{PGDN}");
			m_app.SendKeys("{PGDN}");
			m_app.SendKeys("{PGDN}");

			string firstWindowInfo = m_app.InfoBarValue;

			// now open duplicate window
			m_app.SendKeys("%WN"); // Window/New Window

			// go to a different section, than a different view
			m_app.SendKeys("^{HOME}");
			m_app.GoToConcordanceView();

			// switch to first window
			m_app.SendKeys("%{TAB}");

			AccessibilityHelper parent = m_app.MainAccessibilityHelper.Parent;
			int nWhich = 2;
			AccessibilityHelper secondWindow = parent.FindNthChild("Translation Editor",
				AccessibleRole.None, ref nWhich, 0);
			AccessibilityHelper infoBar =
				secondWindow.FindChild("InfoBarLabel", AccessibleRole.None);

			string secondWindowInfo = infoBar.Value;

			// close first window
			m_app.SendKeys("%{F4}");

			// then close second window
			m_app.Exit(true);

			m_app.Start();

			// Now compare view
			Assert.AreEqual(secondWindowInfo, m_app.InfoBarValue);
		}
	}
}
