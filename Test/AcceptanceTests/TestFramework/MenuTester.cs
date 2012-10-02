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
// File: MenuTester.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.Utils;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.AcceptanceTests.Framework
{
	/// <summary>
	/// Tests of the Menu Framework Standard story (menu)
	/// </summary>
	[TestFixture]
	public class MenuTester : BaseTest
	{
		/// <summary> The application </summary>
		protected AppInteract m_app;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MenuTester"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public MenuTester()
		{
		}

		#region Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_app = TestApp;
			m_app.Start();
			ActivateAllMenuItems(m_app.MainAccessibilityHelper);
		}

		/// <summary>
		/// AppInteract to use for tests.
		/// </summary>
		protected virtual AppInteract TestApp
		{
			get { return new AppInteract(@"DummyTestExe.exe"); }
		}

		///// <summary>
		///// Correct way to deal with FixtureTearDown for class that derive from BaseTest.
		///// </summary>
		///// <param name="disposing"></param>
		//protected override void Dispose(bool disposing)
		//{
		//    if (IsDisposed)
		//        return;

		//    if (disposing)
		//    {
		//        if (m_app != null)
		//        {
		//            m_app.Exit();
		//        }
		//    }
		//    m_app = null;
		//    m_StatusBar = null;

		//    base.Dispose(disposing);
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loop through all menu items to make sure that they are loaded (some are created
		/// only when they show, but afterwards they still exist).
		/// </summary>
		/// <param name="parent"><see cref="AccessibilityHelper"/> of the main window</param>
		/// ------------------------------------------------------------------------------------
		public void ActivateAllMenuItems(AccessibilityHelper parent)
		{
			foreach (AccessibilityHelper child in parent)
			{
				if (child.Role == AccessibleRole.MenuBar && child.Name != "System")
				{
					m_app.SendKeys("%");

					foreach (AccessibilityHelper menu in child)
					{
						Debug.WriteLine("----------------------------------------");
						Debug.WriteLine(string.Format("Activating main frame menu {0} (Role: {1})",
							menu.Name, menu.Role));
						if (menu.IsRealAccessibleObject)
						{
							m_app.SendKeys("{DOWN}");
							Debug.WriteLine("{DOWN}");
							ActivateThisMenu(menu);
						}

						Debug.WriteLine("{RIGHT}");
						m_app.SendKeys("{RIGHT}");
					}
					Debug.WriteLine("{ESC}");
					m_app.SendKeys("{ESC}");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loop through all menu items (and sub-menus) of one menu.
		/// </summary>
		/// <param name="menuParent">The main frame menu <see cref="AccessibilityHelper"/>, e.g.
		/// for File.</param>
		/// ------------------------------------------------------------------------------------
		protected void ActivateThisMenu(AccessibilityHelper menuParent)
		{
			// Because we're really opening the menu, we have one additional layer, that is
			// usually not visible (if you inspect it with AccExplorer, e.g): the window.
			// This consists also only of 1 child
			foreach (AccessibilityHelper menuWindow in menuParent)
			{
				// There should be only 1 popup menu for the menuParent...
				foreach (AccessibilityHelper menuPopup in menuWindow)
				{
					Debug.WriteLine(string.Format("Menu popup {0} (#children: {1}, role: {2})",
						menuPopup.Name, menuPopup.ChildCount, menuPopup.Role));

					foreach (AccessibilityHelper menuItem in menuPopup)
					{
						if (menuItem.IsRealAccessibleObject)
						{
							Debug.WriteLine(string.Format("Sub Menu {0} (role: {1}",
								menuItem.Name, menuItem.Role));
							Debug.WriteLine("{RIGHT}");
							m_app.SendKeys("{RIGHT}");
							ActivateThisMenu(menuItem);
							Debug.WriteLine("{DOWN}");
							m_app.SendKeys("{DOWN}");
						}
						else if (menuItem.Role == AccessibleRole.Separator)
							Debug.WriteLine("Separator -------");
						else
						{
							Debug.WriteLine(string.Format("Menu item {0}", menuItem.Name));
							Debug.WriteLine("{DOWN}");
							m_app.SendKeys("{DOWN}");
						}
					}
				}
			}
			Debug.WriteLine("{ESC}");
			m_app.SendKeys("{ESC}");
		}
		#endregion

		#region Test accessibility of menu items
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that each menu item has an accessible name. If it hasn't this shows that
		/// the menu item is probably owner-drawn (all the time) or has the wrong type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyMenuItemNames()
		{
			int nErrors = 0;
			AccessibilityHelper prevChild = null;
			foreach (AccessibilityHelper child in m_app.MainAccessibilityHelper)
			{
				if (child.Role == AccessibleRole.MenuBar)
				{
					nErrors += VerifyMenuItemNames(child, m_app.MainAccessibilityHelper, prevChild);
					prevChild = child;
				}
			}

			Assert.AreEqual(0, nErrors, string.Format("{0} menu names couldn't be read. " +
				"The other tests will fail. See 'Standard Out' for details.", nErrors));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that each menu item has an accessible name.
		/// </summary>
		/// <param name="menu">Menu to test</param>
		/// <param name="parent">The parent menu item</param>
		/// <param name="prevMenu">The menu item before this</param>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		private int VerifyMenuItemNames(AccessibilityHelper menu, AccessibilityHelper parent,
			AccessibilityHelper prevMenu)
		{
			int nErrors = 0;
			if (menu.Role != AccessibleRole.Separator &&
				(menu.Name == null || menu.Name.Length <= 0))
			{
				Console.WriteLine(
					"Can't get menu name. Parent is '{0}', previous menu item is '{1}'. {2}",
					parent != null ? parent.Name : "<null>",
					prevMenu != null ? prevMenu.Name : "<null>",
					menu.IsRealAccessibleObject && menu.ChildCount > 0 ?
						"Not testing subitems." : "");
				nErrors++;
			}
			else if (menu.IsRealAccessibleObject)
			{
				AccessibilityHelper prevChild = null;
				foreach(AccessibilityHelper child in menu)
				{
					nErrors += VerifyMenuItemNames(child, menu, prevChild);
					prevChild = child;
				}
			}
			return nErrors;
		}
		#endregion

		#region Main frame menu tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test  the main frame menu items:
		///  * names must be a single word (not a phrase)
		///  * Hotkeys should be the first letter of the menu name, unless there is a conflict
		/// </summary>
		/// <remarks>
		/// Override this method to test the test (your test will want to have nErrors > 0).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void MainFrameMenu()
		{
			int nErrors = CheckMainFrameMenu();
			Assert.AreEqual(0, nErrors, string.Format(
				"{0} main frame menu errors occured. See 'Standard Out' for details.", nErrors));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the main frame menu items:
		///  * names must be a single word (not a phrase)
		///  * Hotkeys should be the first letter of the menu name, unless there is a conflict
		///  * last menu item must be 'Help'
		/// </summary>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		public int CheckMainFrameMenu()
		{
			Console.WriteLine("Testing main frame menu...");
			int nErrors = 0;
			foreach (AccessibilityHelper child in m_app.MainAccessibilityHelper)
			{
				if (child.Role == AccessibleRole.MenuBar && child.Name != "System")
				{
					Hashtable htPotentialHotkeyConflicts = new Hashtable(12);
					foreach(AccessibilityHelper c in child)
					{
						if (!htPotentialHotkeyConflicts.Contains(c.Name.ToLower()[0]) &&
							c.Name.ToLower()[0] == c.Shortcut.ToLower()[4])
						{
							htPotentialHotkeyConflicts[c.Name.ToLower()[0]] = c.Name;
						}
					}
					string sLastMenu = string.Empty;
					foreach(AccessibilityHelper c in child)
					{
						if (c.Name.IndexOf(" ") >= 0)
						{
							Console.WriteLine("\tThe main menu '{0}' is not a single word",
								c.Name);
							nErrors++;
						}
						if (c.Name.ToLower()[0] != c.Shortcut.ToLower()[4] &&
							!htPotentialHotkeyConflicts.Contains(c.Name.ToLower()[0]))
						{
							Console.WriteLine(
								"\tMain menu '{0}' should have hotkey '{1}' instead of '{2}'",
								c.Name, c.Name[0], c.Shortcut[4]);
							nErrors++;
						}
						sLastMenu = c.Name;
					}
					if (sLastMenu != "Help")
					{
						Console.WriteLine(
							"\tLast item on Main menu should be 'Help' instead of '{0}'",
							sLastMenu);
						nErrors++;
					}
				}
			}
			return nErrors;
		}
		#endregion

		#region Menu Capitalization and "Book Title" convention
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the menu capitalization and "Book Title" convention
		/// </summary>
		/// <remarks>
		/// Override this method to test the test (your test will want to have nErrors > 0).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void MenuCapitalization()
		{
			int nErrors = CheckMenuCapitalization();
			Assert.AreEqual(0, nErrors, string.Format(
				"{0} capitalization errors occured. See 'Console.Out' for details.", nErrors));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the menu capitalization and "Book Title" convention
		/// </summary>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		public int CheckMenuCapitalization()
		{
			Console.WriteLine("Testing capitalization and 'Book Title' convention...");
			int nErrors = 0;
			foreach (AccessibilityHelper child in m_app.MainAccessibilityHelper)
			{
				if (child.Role == AccessibleRole.MenuBar)
				{
					nErrors += CheckMenuCapitalization(child);
				}
			}
			return nErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the menu capitalization and "Book Title" convention
		/// </summary>
		/// <param name="menu">Menu to test</param>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		private int CheckMenuCapitalization(AccessibilityHelper menu)
		{
			int nErrors = 0;
			if (menu.Role != AccessibleRole.Separator && menu.Role != AccessibleRole.MenuPopup
				&& menu.Name != null && menu.Name.Length > 0)
			{
				if (!Char.IsUpper(menu.Name[0]) && !Char.IsDigit(menu.Name[0]))
				{
					Console.WriteLine("\t'{0}' is not capitalized nor digit", menu.Name);
					nErrors++;
				}
				String[] menuNameParts = menu.Name.Split(new char[] { ' ' });
				for(int i = 1; i < menuNameParts.Length; i++)
				{
					if (Char.IsLower(menuNameParts[i][0]))
					{
						switch(menuNameParts[i])
						{
							case "the": //these are okay
							case "or":
							case "and":
							case "a":
							case "of":
							case "in":
								break;

							default: // any other lower case is not
								Console.WriteLine(
									"\t'{0}' does not follow 'Book Title' convention", menu.Name);
								nErrors++;
								break;
						}

					}
				}
			}
			if (menu.IsRealAccessibleObject)
			{
				foreach(AccessibilityHelper child in menu)
				{
					nErrors += CheckMenuCapitalization(child);
				}
			}
			return nErrors;
		}
		#endregion

		#region Menu Hotkeys
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that menu items within pop-up menus have unique hotkeys
		/// </summary>
		/// <remarks>
		/// Override this method to test the test (your test will want to have nErrors > 0).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void MenuHotkeys()
		{
			int nErrors = CheckMenuHotkeys();
			Assert.AreEqual(0, nErrors, string.Format(
				"{0} menu hotkey errors occured. See 'Standard Out' for details.", nErrors));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For each menu bar in the application, check all the sub menus for duplicate hotkeys.
		/// </summary>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		public int CheckMenuHotkeys()
		{
			Console.WriteLine("Testing for unique menu hotkeys...");
			int nErrors = 0;
			foreach (AccessibilityHelper child in m_app.MainAccessibilityHelper)
			{
				if (child.Role == AccessibleRole.MenuBar && child.Name != "System")
				{
					nErrors += CheckMenuHotkeys(child);
				}
			}
			return nErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recurse the menus to make sure no menus have duplicate hotkeys in the menu items
		/// </summary>
		/// <param name="menu">Menu to test</param>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		private int CheckMenuHotkeys(AccessibilityHelper menu)
		{
			int nErrors = 0;

			ArrayList rgMenuItems = new ArrayList(20);
			foreach(AccessibilityHelper child in menu)
			{
				rgMenuItems.Add(child);
				if (child.IsRealAccessibleObject)
				{
					nErrors += CheckMenuHotkeys(child);
				}
			}

			for (int id1 = 0; id1 < rgMenuItems.Count; id1++)
			{
				AccessibilityHelper child1 = (AccessibilityHelper)rgMenuItems[id1];
				if (child1.Role != AccessibleRole.Separator)
				{
					for (int id2 = id1+1; id2 < rgMenuItems.Count; id2++)
					{
						AccessibilityHelper child2 = (AccessibilityHelper)rgMenuItems[id2];
						if (child2.Role != AccessibleRole.Separator &&
							child1.Shortcut == child2.Shortcut &&
							child1.Shortcut != null &&
							child1.Shortcut.Length != 0)
						{
							Console.WriteLine("\tThe hotkey '{0}' is used by '{1}' and by '{2}'",
								child1.Shortcut, child1.Name, child2.Name);
							nErrors++;
						}
					}
				}
			}
			return nErrors;
		}
		#endregion

		#region Menu Ellipses
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pop-up menus do not have ellipses
		/// </summary>
		/// <remarks>
		/// Override this method to test the test (your test will want to have nErrors > 0).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void MenuEllipses()
		{
			int nErrors = CheckMenuEllipses();
			Assert.AreEqual(0, nErrors, string.Format(
				"{0} menu ellipses errors occured. See 'Standard Out' for details.", nErrors));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For each menu bar in the application, check all the sub menus.
		/// </summary>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		public int CheckMenuEllipses()
		{
			Console.WriteLine("Testing for menu ellipses...");
			int nErrors = 0;
			foreach (AccessibilityHelper child in m_app.MainAccessibilityHelper)
			{
				if (child.Role == AccessibleRole.MenuBar && child.Name != "System")
				{
					nErrors += CheckMenuEllipses(child);
				}
			}
			return nErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recurse the menus to make sure no menus with sub menus contain trailing ellipses
		/// in the menu text.
		/// </summary>
		/// <param name="menu">Menu to test</param>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		private int CheckMenuEllipses(AccessibilityHelper menu)
		{
			int nErrors = 0;

			if (menu.Role == AccessibleRole.MenuPopup)
			{
				if (menu.Name.Trim().EndsWith("..."))
				{
					Console.WriteLine("\tSub menu '{0}' has ellipses in name", menu.Name);
					nErrors++;
				}
			}

			if (menu.IsRealAccessibleObject)
			{
				foreach(AccessibilityHelper child in menu)
				{
					nErrors += CheckMenuEllipses(child);
				}
			}
			return nErrors;
		}
		#endregion

		#region Shortcuts and menu name uniqueness
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the menu name and shortcut uniqueness
		/// </summary>
		/// <remarks>
		/// Override this method to test the test (your test will want to have nErrors > 0).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void MenuNameShortcutUniqueness()
		{
			int nErrors = CheckMenuNameShortCutUniqueness();
			Assert.AreEqual(0, nErrors);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the menu shortcuts
		/// </summary>
		/// <returns>Number of errors</returns>
		/// ------------------------------------------------------------------------------------
		public int CheckMenuNameShortCutUniqueness()
		{
			Console.WriteLine("Testing uniqueness of shortcuts...");
			Hashtable shortCuts = new Hashtable();
			Hashtable menuNames = new Hashtable();
			int nErrors = 0;
			foreach (AccessibilityHelper child in m_app.MainAccessibilityHelper)
			{
				if (child.Role == AccessibleRole.MenuBar)
				{
					nErrors += CheckMenuNameShortCutUniqueness(child, shortCuts, menuNames);
				}
			}
			return nErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the menu shortcuts for a particular menu
		/// </summary>
		/// <param name="menu">The menu to test</param>
		/// <param name="shortCuts">All short cuts found so far</param>
		/// <param name="menuNames">All menu names found so far</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int CheckMenuNameShortCutUniqueness(AccessibilityHelper menu,
			Hashtable shortCuts, Hashtable menuNames)
		{
			int nErrors = 0;
			if (menu.Role == AccessibleRole.MenuItem)
			{
				string[] menuNameParts = menu.Name.Split(new char[] { '\t' });
				if (menuNameParts.Length > 1)
				{
					string shortCut = menuNameParts[menuNameParts.Length - 1];
					if (!shortCuts.Contains(shortCut))
					{
						shortCuts.Add(shortCut, menu.Name);
					}
					else
					{
						Console.WriteLine("\tThe shortcut '{0}' is used by '{1}' and by '{2}'",
							shortCut, shortCuts[shortCut], menu.Name);
						nErrors++;
					}
				}

				string name = menuNameParts[0].TrimEnd(new char[] { '.', ' ' });
				if (!menuNames.Contains(name))
				{
					menuNames.Add(name, menu.Parent.Name);
				}
				else
				{
					Console.WriteLine("\tThe menu name '{0}' is used by '{1}' and by '{2}'",
						name, menu.Parent.Name, menuNames[name]);
					nErrors++;
				}
			}

			if (menu.IsRealAccessibleObject)
			{
				foreach(AccessibilityHelper child in menu)
				{
					nErrors += CheckMenuNameShortCutUniqueness(child, shortCuts, menuNames);
				}
			}
			return nErrors;
		}
		#endregion

		#region Status bar texts
		/// <summary>The status bar of the application</summary>
		protected AccessibilityHelper m_StatusBar;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first pane of the status bar
		/// </summary>
		/// <remarks>The accessible object for the first pane changes when the text in it
		/// changes</remarks>
		/// ------------------------------------------------------------------------------------
		private AccessibilityHelper StatusBarPane
		{
			get
			{
				AccessibilityHelper firstPane = null;
				if (m_StatusBar == null)
				{
					m_StatusBar = m_app.MainAccessibilityHelper.FindChild(null,
						AccessibleRole.StatusBar);
				}
				if (m_StatusBar != null)
				{
					// We're interested in the first pane (i.e. child) of the status bar
					foreach (AccessibilityHelper child in m_StatusBar)
					{
						firstPane = child;
						break;
					}
				}
				return firstPane;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that all menu items display a help text in the status bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not finished implementing - low priority according to JohnW")]
		public virtual void StatusbarMenuTexts()
		{
			int nErrors = CheckStatusbarMenuTexts();
			Assert.AreEqual(0, nErrors);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loop through all menu items and check that they display a help text in the status
		/// bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CheckStatusbarMenuTexts()
		{
			int nErrors = 0;
			foreach (AccessibilityHelper child in m_app.MainAccessibilityHelper)
			{
				if (child.Role == AccessibleRole.MenuBar && child.Name != "System")
				{
					m_app.SendKeys("%");

					foreach (AccessibilityHelper menu in child)
					{
						Debug.WriteLine("----------------------------------------");
						Debug.WriteLine(string.Format("Activating main frame menu {0} (Role: {1})",
							menu.Name, menu.Role));
						nErrors += CheckMenuHelp(menu.Name);
						if (menu.IsRealAccessibleObject)
						{
							m_app.SendKeys("{DOWN}");
							Debug.WriteLine("{DOWN}");
							nErrors += CheckStatusbarMenuTexts(menu);
						}

						Debug.WriteLine("{RIGHT}");
						m_app.SendKeys("{RIGHT}");
					}
					Debug.WriteLine("{ESC}");
					m_app.SendKeys("{ESC}");
				}
			}
			return nErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loop through all menu items (and sub-menus) of one menu and check help text in the
		/// status bar.
		/// </summary>
		/// <param name="menuParent">The main frame menu <see cref="AccessibilityHelper"/>, e.g.
		/// for File.</param>
		/// ------------------------------------------------------------------------------------
		protected int CheckStatusbarMenuTexts(AccessibilityHelper menuParent)
		{
			int nErrors = 0;
			// Because we're really opening the menu, we have one additional layer, that is
			// usually not visible (if you inspect it with AccExplorer, e.g): the window.
			// This consists also only of 1 child
			foreach (AccessibilityHelper menuWindow in menuParent)
			{
				// There should be only 1 popup menu for the menuParent...
				foreach (AccessibilityHelper menuPopup in menuWindow)
				{
					Debug.WriteLine(string.Format("Menu popup {0} (#children: {1}, role: {2})",
						menuPopup.Name, menuPopup.ChildCount, menuPopup.Role));

					foreach (AccessibilityHelper menuItem in menuPopup)
					{
						if (menuItem.Role == AccessibleRole.Separator)
							Debug.WriteLine("Separator -------");
						else
						{
							nErrors += CheckMenuHelp(menuItem.Name);
							if (menuItem.IsRealAccessibleObject)
							{
								Debug.WriteLine(string.Format("Sub Menu {0} (role: {1})",
									menuItem.Name, menuItem.Role));
								Debug.WriteLine("{RIGHT}");
								m_app.SendKeys("{RIGHT}");
								nErrors += CheckStatusbarMenuTexts(menuItem);
							}
							else
							{
								Debug.WriteLine(string.Format("Menu item {0}", menuItem.Name));
							}
							Debug.WriteLine("{DOWN}");
							m_app.SendKeys("{DOWN}");
						}
					}
				}
			}
			Debug.WriteLine("{ESC}");
			m_app.SendKeys("{ESC}");
			return nErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a menu item has help.
		/// </summary>
		/// <param name="sMenuName">The name of the menu being checked (for reporting purposes)
		/// </param>
		/// <returns>1 if item does not have help; 0 otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected int CheckMenuHelp(string sMenuName)
		{
			// StatusBarPane.Name contains the displayed value
			Debug.WriteLine(string.Format("Check menu help for {0}: {1}", sMenuName, StatusBarPane.Name));
			return 0;
		}
		#endregion
	}
}
