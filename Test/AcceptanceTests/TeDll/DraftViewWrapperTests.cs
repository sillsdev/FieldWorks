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
// File: DraftViewWrapperTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.Controls.SplitGridView;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Acceptance tests for DraftViewWrapper
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DraftViewWrapperTests : BaseTest
	{
		private DraftViewWrapper m_dummyWrapper;
		private Form m_mainWindow;

		#region Setup and Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view wrapper
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();
			m_mainWindow = new Form();
			m_mainWindow.Size = new Size(300, 300);
			RegistryKey regKey =
				Registry.CurrentUser.OpenSubKey(@"Software\SIL\FieldWorks\Translation Editor");
			// we just add a dummy control so that we have one to get the size of the panes
			FixedControlCreateInfo dummyCreateInfo = new FixedControlCreateInfo(new Control());
			m_dummyWrapper = new DraftViewWrapper("DummyDraftView", null, null, null, regKey,
				dummyCreateInfo, dummyCreateInfo, dummyCreateInfo, dummyCreateInfo,
				dummyCreateInfo, dummyCreateInfo);
			m_mainWindow.Controls.Add(m_dummyWrapper);
			m_dummyWrapper.Visible = true;
			m_mainWindow.Show();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();
			m_mainWindow.Dispose();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if minimizing the window messes up the splitter window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestMinimize()
		{
			// Display the top and bottom row
			m_dummyWrapper.GetRow(0).Visible = true;
			m_dummyWrapper.GetRow(1).Visible = true;
			Application.DoEvents();

			Size saveDraftSizeA = m_dummyWrapper.GetControl(0, 1).Size;
			Size saveDraftSizeB = m_dummyWrapper.GetControl(1, 1).Size;
			m_mainWindow.WindowState = FormWindowState.Minimized;
			Application.DoEvents();
			m_mainWindow.WindowState = FormWindowState.Normal;
			Application.DoEvents();
			Assert.AreEqual(saveDraftSizeA, m_dummyWrapper.GetControl(0, 1).Size);
			Assert.AreEqual(saveDraftSizeB, m_dummyWrapper.GetControl(1, 1).Size);
		}
	}
}
