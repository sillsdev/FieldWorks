// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PersistenceTest.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for Persistence
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PersistenceTest : BaseTest
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
			using (DummyPersistedFormManual form = new DummyPersistedFormManual())
			{
				form.SetDesktopBounds(100, 200, form.Width, form.Height);
				Rectangle rectOrig = form.DesktopBounds;
				form.Show();
				FormWindowState state = form.WindowState;
				Rectangle rcForm = form.DesktopBounds;
				float dpi;
				using (var graphics = form.CreateGraphics())
				{
					dpi = graphics.DpiX;
				}
				form.Close();
				Assert.AreEqual(FormWindowState.Normal, state);
				Assert.AreEqual(rectOrig.Location, rcForm.Location);
				// At any other DPI, DotNet resizes the window for us!
				if (dpi == 96)
					Assert.AreEqual(rectOrig, rcForm);
			}
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
			Rectangle rectOrig;
			using (var form = new DummyPersistedFormManual())
			{
				form.Show();
				form.SetDesktopBounds(40, 50, 110, 98);
				rectOrig = form.DesktopBounds;
				form.Close();
			}
			using (var form = new DummyPersistedFormManual())
			{
				form.Show();

				FormWindowState state = form.WindowState;
				Rectangle rcForm = form.DesktopBounds;
				form.Close();

				Assert.AreEqual(FormWindowState.Normal, state);
				Assert.AreEqual(rectOrig, rcForm);
			}
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
			Rectangle rectOrig;
			using (var form = new DummyPersistedFormManual())
			{
				form.Show();
				form.SetDesktopBounds(40, 50, 110, 98);
				rectOrig = form.DesktopBounds;
				form.WindowState = FormWindowState.Maximized;
				form.Close();
			}
			using (var form = new DummyPersistedFormManual())
			{
				form.Show();
#if !__MonoCS__
				FormWindowState state = form.WindowState;
#endif

				// Restore to normal state, original size
				form.WindowState = FormWindowState.Normal;
				Rectangle rcForm = form.DesktopBounds;
				form.Close();

#if !__MonoCS__
				Assert.AreEqual(FormWindowState.Maximized, state);
#else
				// TODO-Linux: probably fails because of this bug https://bugzilla.novell.com/show_bug.cgi?id=495562 renable this when this has been fixed
#endif
				Assert.AreEqual(rectOrig, rcForm);
			}
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
			Rectangle rectOrig;
			using (var form = new DummyPersistedFormWinDef())
			{
				form.Show();
				form.SetDesktopBounds(40, 50, 110, 98);
				rectOrig = form.DesktopBounds;
				form.Close();
			}

			// Verify measurements in new window with registry entries
			using (var form = new DummyPersistedFormWinDef())
			{
				form.Show();

				FormWindowState state = form.WindowState;
				Rectangle rcForm = form.DesktopBounds;
				form.Close();

				Assert.AreEqual(FormWindowState.Normal, state);
				Assert.AreEqual(rectOrig, rcForm);
			}
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
			Rectangle rectOrig;
			using (var form = new DummyPersistedFormWinDef())
			{
				form.Show();
				form.SetDesktopBounds(48, 58, 118, 95);
				rectOrig = form.DesktopBounds;
				form.WindowState = FormWindowState.Maximized;
				form.Close();
			}

			// Verify measurements in new window with registry entries
			using (var form = new DummyPersistedFormWinDef())
			{
				form.Show();

#if !__MonoCS__
				FormWindowState state = form.WindowState;
#endif

				// Restore to normal state, verify that we have original size
				form.WindowState = FormWindowState.Normal;
				Rectangle rcForm = form.DesktopBounds;
				form.Close();

#if !__MonoCS__
				Assert.AreEqual(FormWindowState.Maximized, state);
#else
				// TODO-Linux: proberbly fails because of this bug https://bugzilla.novell.com/show_bug.cgi?id=495562 renable this when this has been fixed
#endif
				Assert.AreEqual(rectOrig, rcForm);
			}
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
			Rectangle rectOrig;
			using (var form = new DummyPersistedFormWinDef())
			{
				form.Show();
				rectOrig = form.DesktopBounds;
				form.WindowState = FormWindowState.Minimized;
				form.Close();
			}

			// Test that Minimized Window Comes Back As Normal
			using (var form = new DummyPersistedFormWinDef())
			{
				form.Show();
				FormWindowState state = form.WindowState;
				Rectangle rcForm = form.DesktopBounds;
				form.Close();

				Assert.AreEqual(FormWindowState.Normal, state);
				Assert.AreEqual(rectOrig, rcForm);
			}
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
			Rectangle rectCompare;
			using (var form1 = new DummyPersistedFormWinDef())
			{
				form1.Show();
				form1.SetDesktopBounds(205, 106, 407, 308);

				using (var form2 = new DummyPersistedFormWinDef())
				{
					form2.Show();
					form2.SetDesktopBounds(101, 52, 301, 401);
					rectCompare = form2.DesktopBounds;
					form2.WindowState = FormWindowState.Maximized;
					form1.Close();
					form2.Close();
				}
			}

			// Test that restored window has size/pos of Last Window Closed
			using (var form = new DummyPersistedFormWinDef())
			{
				form.Show();
#if !__MonoCS__
				FormWindowState state = form.WindowState;
#endif
				form.WindowState = FormWindowState.Normal;
				Rectangle rcForm = form.DesktopBounds;
				form.Close();
#if !__MonoCS__
				Assert.AreEqual(FormWindowState.Maximized, state);
#else
				// TODO-Linux: proberbly fails because of this bug https://bugzilla.novell.com/show_bug.cgi?id=495562 renable this when this has been fixed
#endif
				Assert.AreEqual(rectCompare, rcForm);
			}
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
			using (var form = new DummyPersistedFormWinDef())
			{
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
			try
			{
				RegistryHelper.CompanyKey.DeleteSubKey("FwTest");
			}
			catch
			{
			}
		}
	}
}
