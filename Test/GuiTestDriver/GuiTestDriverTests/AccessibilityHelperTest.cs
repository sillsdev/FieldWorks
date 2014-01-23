// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AccessibilityHelperTest.cs
// Responsibility: Testing
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;
using System.Drawing;
//using SIL.FieldWorks.Common.COMInterfaces;

namespace GuiTestDriver
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the <see cref="AccessibilityHelper"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AccessibilityHelperTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AccessibilityHelperTest"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelperTest()
		{
		}

		private Process m_proc;
		private AccessibilityHelper m_ah;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start dummy form
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void Init()
		{
			string tePath = SIL.FieldWorks.Common.Utils.DirectoryFinder.GetFWCodeFile(@"\..\Output\Debug\TE.exe");
			m_proc = Process.Start(tePath);
			m_proc.WaitForInputIdle();
			while (Process.GetProcessById(m_proc.Id).MainWindowHandle == IntPtr.Zero)
				Thread.Sleep(100);
			m_proc.WaitForInputIdle();
			Win32.SetForegroundWindow(m_proc.MainWindowHandle);
			m_ah = new AccessibilityHelper(m_proc.MainWindowHandle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close down dummy form
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void EndTest()
		{
			CloseWindow();
			// Thread.Sleep(200); // when a test fails TE stays up - find a way to force it closed
			// CloseWindow();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test creation of AccessibilityHelper object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ObjectCreation()
		{
			Assert.IsNotNull(m_ah);
			Assert.AreEqual(m_proc.MainWindowHandle.ToInt32(), m_ah.HWnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test name, role, states, and keyboard shortcut of AccessibilityHelper object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NameRoleStatesShortcutDefAction()
		{
			Assert.AreEqual("Kalaba (TestLangProj) - Translation Editor", m_ah.Name);
			Assert.AreEqual(AccessibleRole.Window, m_ah.Role);
			Assert.AreEqual(AccessibleStates.Sizeable|AccessibleStates.Moveable|
				AccessibleStates.Focusable, m_ah.States);
			Assert.IsNull(m_ah.Shortcut, "non-null keyboard shortcut");
			Assert.IsNull(m_ah.DefaultAction, "non-null default action");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test AccessiblityEnumerator class and ChildCount method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AccessiblityEnumeratorChildCount()
		{
			int count = 0;
			foreach (AccessibilityHelper child in m_ah)
			{
				count++;
				Assert.IsNotNull(child);
			}
			Assert.AreEqual(7, count);
			Assert.AreEqual(7, m_ah.ChildCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test FindChild method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindChild()
		{
			// Not enough accessibility to get to the menu reliably - too many NAMELESS
			/*AccessibilityHelper child = m_ah.FindChild("~MainMenu~", AccessibleRole.Window);
			Assert.IsNotNull(child,"null child");
			Assert.AreEqual("~MainMenu~", child.Name);
			Assert.AreEqual(AccessibleRole.Window, child.Role);

			child = child.FindChild("mnuFormat", AccessibleRole.MenuItem);
			Assert.IsNotNull(child,"null child");
			Assert.AreEqual("mnuFormat", child.Name);
			Assert.AreEqual(AccessibleRole.MenuItem, child.Role);*/

			AccessibilityHelper child = m_ah.FindChild(null, AccessibleRole.Client);
			Assert.IsNotNull(child,"null child");
			Assert.AreEqual("Kalaba (TestLangProj) - Translation Editor", child.Name);
			Assert.AreEqual(AccessibleRole.Client, child.Role);

			child = m_ah.FindChild("System", AccessibleRole.MenuBar);
			Assert.IsNotNull(child,"null child");
			Assert.AreEqual("System", child.Name);
			Assert.AreEqual(AccessibleRole.MenuBar, child.Role);

			child = m_ah.FindChild("NotExistantChild", AccessibleRole.None);
			Assert.IsNull(child,"non-null child");

			child = m_ah.FindChild(null, AccessibleRole.None);
			Assert.IsNull(child,"non-null child");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test Value method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Value()
		{
			AccessibilityHelper child = m_ah.FindChild(null, AccessibleRole.ScrollBar);
			Assert.AreEqual("0", child.Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="AccessibilityHelper.Navigate"/>  method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Parent_Navigate()
		{
			AccessibilityHelper child = m_ah.FindChild("System", AccessibleRole.MenuBar);
			AccessibilityHelper parent = child.Parent;
			Assert.AreEqual("Kalaba (TestLangProj) - Translation Editor", parent.Name);
			AccessibilityHelper window = child.Navigate(AccessibleNavigation.Down);
			Assert.IsNotNull(window);
			Assert.AreEqual("Kalaba (TestLangProj) - Translation Editor",window.Name);

			AccessibilityHelper navChild = m_ah.Navigate(AccessibleNavigation.FirstChild);
			Assert.AreEqual("System", navChild.Name);
			navChild = navChild.Navigate(AccessibleNavigation.Next);
			Assert.IsNull(navChild.Name);
			navChild = navChild.Navigate(AccessibleNavigation.Next);
			Assert.AreEqual("Kalaba (TestLangProj) - Translation Editor", navChild.Name);
			navChild = navChild.Navigate(AccessibleNavigation.Next);
			Assert.AreEqual("Kalaba (TestLangProj) - Translation Editor", navChild.Name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="AccessibilityHelper.FindNthChild"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		//[Ignore ("TE doesn't have enough windows to test this yet.")]
		[Test]
		public void FindNthChild()
		{
			AccessibilityHelper ah = m_ah;

			// now look for the second instance
			int nWhich = 3;
			AccessibilityHelper first = m_ah.Parent.FindNthChild("Paragraph",
				AccessibleRole.Text, nWhich, 10);
			nWhich = 4;
			AccessibilityHelper second = m_ah.Parent.FindNthChild("Paragraph",
				AccessibleRole.Text, nWhich, 10);
			nWhich = 300;
			AccessibilityHelper none = m_ah.Parent.FindNthChild("Paragraph",
				AccessibleRole.Text, nWhich, 10);

			m_ah = ah;

			Assert.IsTrue(first != second);
			Assert.IsNull(none);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="AccessibilityHelper"/> constructor that is based on a call to
		/// the AccessibleObjectFromPoint API function.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AccessibleObjectFromPoint()
		{
			AccessibilityHelper child = m_ah.FindChild("statusBarFw", AccessibleRole.Window);
			Rect rect;
			Win32.GetWindowRect((IntPtr)child.HWnd, out rect);
			Point pt = new Point(rect.left, rect.top);
			AccessibilityHelper test = new AccessibilityHelper(pt);
			Assert.AreEqual(child.HWnd, test.HWnd);
		}

		private void CloseWindow()
		{
			try
			{
				m_proc.WaitForInputIdle();
				Win32.SetForegroundWindow(m_proc.MainWindowHandle);
				SendKeys.SendWait("%{F4}");
				m_proc.WaitForInputIdle();
				m_proc.WaitForExit();
			}
			catch
			{
			}
		}

	}
}
