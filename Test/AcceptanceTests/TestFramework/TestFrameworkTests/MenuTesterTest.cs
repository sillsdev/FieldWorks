// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MenuTesterTest.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>

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
