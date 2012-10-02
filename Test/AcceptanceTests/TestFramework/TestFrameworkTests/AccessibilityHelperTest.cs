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
// File: AccessibilityHelperTest.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.Utils;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.AcceptanceTests.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the <see cref="AccessibilityHelper"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AccessibilityHelperTest : BaseTest
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
		[SetUp]
		public void Init()
		{
			m_proc = Process.Start(@"DummyTestExe.exe");
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
		[TearDown]
		public void EndTest()
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
			Assert.AreEqual("dummyTestForm", m_ah.Name);
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
			AccessibilityHelper child = m_ah.FindChild("informationBar", AccessibleRole.None);
			Assert.IsNotNull(child);
			Assert.AreEqual("informationBar", child.Name);
			Assert.AreEqual(AccessibleRole.Window, child.Role);

			child = m_ah.FindChild(null, AccessibleRole.Client);
			Assert.IsNotNull(child);
			Assert.AreEqual("dummyTestForm", child.Name);
			Assert.AreEqual(AccessibleRole.Client, child.Role);

			child = m_ah.FindChild("informationBar", AccessibleRole.Window);
			Assert.IsNotNull(child);
			Assert.AreEqual("informationBar", child.Name);
			Assert.AreEqual(AccessibleRole.Window, child.Role);

			child = m_ah.FindChild("draftView", AccessibleRole.Client);
			Assert.IsNotNull(child);
			Assert.AreEqual("draftView", child.Name);
			Assert.AreEqual(AccessibleRole.Client, child.Role);

			child = m_ah.FindChild("NotExistantChild", AccessibleRole.None);
			Assert.IsNull(child);

			child = m_ah.FindChild(null, AccessibleRole.None);
			Assert.IsNull(child);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test Value method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Value()
		{
			AccessibilityHelper child = m_ah.FindChild("informationBar", AccessibleRole.None);
			Assert.AreEqual("Information Bar Text", child.Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="AccessibilityHelper.Navigate"/>  method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Parent_Navigate()
		{
			AccessibilityHelper child = m_ah.FindChild("informationBar", AccessibleRole.None);
			AccessibilityHelper parent = child.Parent;
			Assert.AreEqual("dummyTestForm", parent.Name);
			AccessibilityHelper draftView = child.Navigate(AccessibleNavigation.Down);
			Assert.IsNotNull(draftView);
			Assert.AreEqual("draftView", draftView.Name);

			AccessibilityHelper navChild = m_ah.Navigate(AccessibleNavigation.FirstChild);
			Assert.AreEqual("System", navChild.Name);
			navChild = navChild.Navigate(AccessibleNavigation.Next);
			Assert.IsNull(navChild.Name);
			navChild = navChild.Navigate(AccessibleNavigation.Next);
			Assert.AreEqual("Application", navChild.Name);
			navChild = navChild.Navigate(AccessibleNavigation.Next);
			Assert.AreEqual("dummyTestForm", navChild.Name);
			navChild = navChild.Navigate(AccessibleNavigation.Next);
			Assert.IsNull(navChild);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="AccessibilityHelper.FindNthChild"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNthChild()
		{
			Process proc = m_proc;
			AccessibilityHelper ah = m_ah;

			// start second instance
			Init();

			// now look for the second instance
			int nWhich = 1;
			AccessibilityHelper first = m_ah.Parent.FindNthChild("dummyTestForm",
				AccessibleRole.None, ref nWhich, 0);
			nWhich = 2;
			AccessibilityHelper second = m_ah.Parent.FindNthChild("dummyTestForm",
				AccessibleRole.None, ref nWhich, 0);
			nWhich = 3;
			AccessibilityHelper none = m_ah.Parent.FindNthChild("dummyTestForm",
				AccessibleRole.None, ref nWhich, 0);

			int hwnd1 = m_proc.MainWindowHandle.ToInt32();
			int hwnd2 = proc.MainWindowHandle.ToInt32();
			int foundHwnd1 = first.HWnd;
			int foundHwnd2 = second.HWnd;

			// Close the second instance
			EndTest();

			m_proc = proc;
			m_ah = ah;

			// now test that we got the right window. Do this after closing the second
			// instance, because if it fails the rest of this method isn't executed
			Assert.AreEqual(hwnd1, foundHwnd1);
			Assert.AreEqual(hwnd2, foundHwnd2);
			Assert.IsTrue(hwnd1 != foundHwnd2);
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
			AccessibilityHelper child = m_ah.FindChild("informationBar", AccessibleRole.None);
			Rect rect;
			Win32.GetWindowRect((IntPtr)child.HWnd, out rect);
			Point pt = new Point(rect.left, rect.top);
			AccessibilityHelper test = new AccessibilityHelper(pt);
			Assert.AreEqual(child.HWnd, test.HWnd);
		}
	}
}
