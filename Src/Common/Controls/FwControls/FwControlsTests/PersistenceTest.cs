// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PersistenceTest.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for Persistence
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PersistenceTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new instance of the <see cref="PersistenceTest"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PersistenceTest()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called before each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Initialize()
		{
			ClearTestRegistryEntries();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up called after each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Cleanup()
		{
			ClearTestRegistryEntries();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Persistence object does not interfere when form has
		/// * manual start position
		/// * no registry entries
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ManualStartPositionNoInterference()
		{
			DummyPersistedFormManual form = new DummyPersistedFormManual();
			form.SetDesktopBounds(100, 200, form.Width, form.Height);
			Rectangle rectOrig = form.DesktopBounds;
			form.Show();
			FormWindowState state = form.WindowState;
			Rectangle rcForm = form.DesktopBounds;
			form.Close();
			Assert.AreEqual(FormWindowState.Normal, state);
			Assert.AreEqual(rectOrig, rcForm);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Persistence object works when form has
		/// * manual start position
		/// * registry entries
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ManualStartPositionNormal()
		{
			DummyPersistedFormManual form = new DummyPersistedFormManual();
			form.SetDesktopBounds(40, 50, 110, 98);
			Rectangle rectOrig = form.DesktopBounds;
			form.Show();
			form.Close();
			form = new DummyPersistedFormManual();
			form.Show();

			FormWindowState state = form.WindowState;
			Rectangle rcForm = form.DesktopBounds;
			form.Close();

			Assert.AreEqual(FormWindowState.Normal, state);
			Assert.AreEqual(rectOrig, rcForm);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Persistence object works when form has
		/// * manual start position
		/// * registry entries
		/// * window state = maximized
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ManualStartPositionMaximized()
		{
			DummyPersistedFormManual form = new DummyPersistedFormManual();
			form.SetDesktopBounds(40, 50, 110, 98);
			Rectangle rectOrig = form.DesktopBounds;
			form.Show();
			form.WindowState = FormWindowState.Maximized;
			form.Close();
			form = new DummyPersistedFormManual();
			form.Show();
			FormWindowState state = form.WindowState;

			// Restore to normal state, original size
			form.WindowState = FormWindowState.Normal;
			Rectangle rcForm = form.DesktopBounds;
			form.Close();

			Assert.AreEqual(FormWindowState.Maximized, state);
			Assert.AreEqual(rectOrig, rcForm);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Persistence object works when form has
		/// * Windows default start position
		/// * registry entries
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void DefaultStartPositionNormal()
		{
			// Establish baseline - make registry entries
			DummyPersistedFormWinDef form = new DummyPersistedFormWinDef();
			form.Show();
			form.SetDesktopBounds(40, 50, 110, 98);
			Rectangle rectOrig = form.DesktopBounds;
			form.Close();

			// Verify measurements in new window with registry entries
			form = new DummyPersistedFormWinDef();
			form.Show();

			FormWindowState state = form.WindowState;
			Rectangle rcForm = form.DesktopBounds;
			form.Close();

			Assert.AreEqual(FormWindowState.Normal, state);
			Assert.AreEqual(rectOrig, rcForm);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Persistence object works when form has
		/// * Windows default start position
		/// * registry entries
		/// * window state = maximized
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void DefaultStartPositionMaximized()
		{
			// Establish baseline - make registry entries
			DummyPersistedFormWinDef form = new DummyPersistedFormWinDef();
			form.Show();
			form.SetDesktopBounds(48, 58, 118, 95);
			Rectangle rectOrig = form.DesktopBounds;
			form.WindowState = FormWindowState.Maximized;
			form.Close();

			// Verify measurements in new window with registry entries
			form = new DummyPersistedFormWinDef();
			form.Show();

			FormWindowState state = form.WindowState;

			// Restore to normal state, verify that we have original size
			form.WindowState = FormWindowState.Normal;
			Rectangle rcForm = form.DesktopBounds;
			form.Close();

			Assert.AreEqual(FormWindowState.Maximized, state);
			Assert.AreEqual(rectOrig, rcForm);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a window that is closed when minimized is persisted as normal.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MinimizedRestoresAsNormal()
		{
			// Establish Baseline - create registry entries
			DummyPersistedFormWinDef form = new DummyPersistedFormWinDef();
			form.Show();
			Rectangle rectOrig = form.DesktopBounds;
			form.WindowState = FormWindowState.Minimized;
			form.Close();

			// Test that Minimized Window Comes Back As Normal
			form = new DummyPersistedFormWinDef();
			form.Show();
			FormWindowState state = form.WindowState;
			Rectangle rcForm = form.DesktopBounds;
			form.Close();

			Assert.AreEqual(FormWindowState.Normal, state);
			Assert.AreEqual(rectOrig, rcForm);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that restored window has size/pos of last window closed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void LastWindowClosedIsPersisted()
		{
			// Open two forms with different sizes/positions
			DummyPersistedFormWinDef form1 = new DummyPersistedFormWinDef();
			form1.Show();
			form1.SetDesktopBounds(205, 106, 407, 308);

			DummyPersistedFormWinDef form2 = new DummyPersistedFormWinDef();
			form2.Show();
			form2.SetDesktopBounds(101, 52, 301, 401);
			Rectangle rectCompare = form2.DesktopBounds;
			form2.WindowState = FormWindowState.Maximized;
			form1.Close();
			form2.Close();

			// Test that restored window has size/pos of Last Window Closed
			DummyPersistedFormWinDef form = new DummyPersistedFormWinDef();
			form.Show();
			FormWindowState state = form.WindowState;
			form.WindowState = FormWindowState.Normal;
			Rectangle rcForm = form.DesktopBounds;
			form.Close();

			Assert.AreEqual(FormWindowState.Maximized, state);
			Assert.AreEqual(rectCompare, rcForm);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a window that is maximized keeps its normal desktop bounds saved in
		/// the persistence object
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MaximizedKeepsNormal()
		{
			// Create new window and change the size and location
			DummyPersistedFormWinDef form = new DummyPersistedFormWinDef();
			form.SetDesktopBounds(47, 31, 613, 317);
			form.Show();
			Rectangle rectOrig = form.DesktopBounds;
			// maximize the window
			form.WindowState = FormWindowState.Maximized;
			Rectangle rectNew = form.PersistenceObject.NormalStateDesktopBounds;
			form.Close();

			// Test that normal desktop bounds are still saved in the persistance object
			Assert.AreEqual(rectOrig, rectNew, "Maximized keeps normal");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up registry entries created by this test. The registry entry being cleared
		/// must match the default key path in DummyPersistedFormWinDef and
		/// DummyPersistedFormManual (Software\SIL\FwTest).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void ClearTestRegistryEntries()
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\SIL", true);
			if (key != null)
			{
				try
				{
					key.DeleteSubKey("FwTest");
				}
				catch
				{
				}
			}
		}
	}
}