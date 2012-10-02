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
// File: MenuTesterTest.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.AcceptanceTests.Framework
{
	/// <summary>
	/// Summary description for MenuTesterTest.
	/// </summary>
	[TestFixture]
	public class MenuTesterTest: MenuTester
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MenuTesterTest"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public MenuTesterTest()
		{
			// Gets made by superclass in its FixtureSetup() method.
			//m_app = new AppInteract(@"DummyTestExe.exe");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the menu capitalization tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public override void MenuCapitalization()
		{
			int nErrors = CheckMenuCapitalization();
			Assert.AreEqual(2, nErrors);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pop-up menus do not have ellipses
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public override void MenuEllipses()
		{
			int nErrors = CheckMenuEllipses();
			Assert.AreEqual(1, nErrors);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test  the main frame menu items:
		///  * names must be a single word (not a phrase)
		///  * Hotkeys should be the first letter of the menu name, unless there is a conflict
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public override void MainFrameMenu()
		{
			int nErrors = CheckMainFrameMenu();
			Assert.AreEqual(6, nErrors);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pop-up menus do not have duplicate hotkeys
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public override void MenuHotkeys()
		{
			int nErrors = CheckMenuHotkeys();
			Assert.AreEqual(2, nErrors);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the menu name and shortcut uniqueness tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public override void MenuNameShortcutUniqueness()
		{
			int nErrors = CheckMenuNameShortCutUniqueness();
			Assert.AreEqual(2, nErrors);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the menu status bar menu text tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not finished implementing - low priority according to JohnW")]
		public override void StatusbarMenuTexts()
		{
			int nErrors = CheckStatusbarMenuTexts();
			Assert.AreEqual(6, nErrors);
		}

	}
}
