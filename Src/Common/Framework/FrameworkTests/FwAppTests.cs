// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwAppTests.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Utils;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.Framework
{
#if WANTTESTPORT // (Common) FWR-251 Tests need to be updated for new synchronization approach
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the FwApp class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwAppTests : MemoryOnlyBackendProviderTestBase
	{
		private DynamicMock m_mainWnd;
		private FwApp m_app;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_mainWnd = new DynamicMock(typeof(IFwMainWnd));
			m_mainWnd.SetupResult("Cache", Cache);
			m_app = new DummyFwApp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets values so tests are independent of each other
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			if (m_app != null)
			{
				m_app.Dispose(); // Ensure cache disposed and WSF shutdown.
				m_app = null;
			}
			m_mainWnd = null;

			base.TestTearDown();
		}

	#region Test for Synchronize
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that (Pre)Synchronize gets called
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Synchronize()
		{
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			// This should call (Pre)Synchronize
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);

			m_mainWnd.Verify();
		}
		#endregion
	}
#endif
}
