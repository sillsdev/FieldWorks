// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwAppTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

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
			m_mainWnd.Expect("PreSynchronize", new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			// This should call (Pre)Synchronize
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);

			m_mainWnd.Verify();
		}
		#endregion

	#region Tests for Suppress/ResumeSynchronize methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests Suppress synchronize method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SuppressSynchronize()
		{
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			// This should call nothing
			m_app.SuppressSynchronize(Cache);
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);

			m_mainWnd.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests Resume method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResumeSynchronize()
		{
			m_mainWnd.Expect("PreSynchronize", new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			m_app.SuppressSynchronize(Cache);
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);
			// This should call (Pre)Synchronize
			m_app.ResumeSynchronize(Cache);

			m_mainWnd.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that suppress/resume synchronize stores identical messages only once,
		/// and also that the message goes to all main windows,
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiMessageSynchronize_IdenticalMessages()
		{
			m_mainWnd.Expect("PreSynchronize", new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			DynamicMock otherMainWnd = new DynamicMock(typeof(IFwMainWnd));
			otherMainWnd.SetupResult("Cache", Cache);
			otherMainWnd.Expect("PreSynchronize", new IsAnything());
			otherMainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)otherMainWnd.MockInstance);

			m_app.SuppressSynchronize(Cache);
			// we expect that the identical message will be discarded
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);

			// This should call (Pre)Synchronize only once on each window
			m_app.ResumeSynchronize(Cache);

			m_mainWnd.Verify();
			otherMainWnd.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that suppress/resume synchronize stores message with different cache
		/// multiple times
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiMessageSynchronize_DifferentCache()
		{
			m_mainWnd.Expect("PreSynchronize", new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			FdoCache differentCache = FdoCache.CreateCache(FDOBackendProviderType.kMemoryOnly, BackendBulkLoadDomain.All, null);
			try
			{
				DynamicMock otherMainWnd = new DynamicMock(typeof(IFwMainWnd));
				otherMainWnd.SetupResult("Cache", differentCache);
				otherMainWnd.Expect("PreSynchronize", new IsAnything());
				otherMainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
				m_app.MainWindows.Add((IFwMainWnd)otherMainWnd.MockInstance);

				m_app.SuppressSynchronize(Cache);
				m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);
				m_app.Synchronize(SyncMsg.ksyncUndoRedo, differentCache);

				// This should call (Pre)Synchronize once for each main window
				m_app.ResumeSynchronize(Cache);

				m_mainWnd.Verify();
				otherMainWnd.Verify();
			}
			finally
			{
				differentCache.Dispose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that suppress/resume synchronize stores different messages
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiMessageSynchronize_DifferentMessages()
		{
			m_mainWnd.ExpectAndReturn(2, "PreSynchronize", true, new IsAnything());
			m_mainWnd.ExpectAndReturn(2, "Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			m_app.SuppressSynchronize(Cache);
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);
			m_app.Synchronize(SyncMsg.ksyncDelPss, Cache);

			// This should call (Pre)Synchronize twice
			m_app.ResumeSynchronize(Cache);

			m_mainWnd.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that suppress/resume calls synchronize in expected order
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResumeCallsMessagesInExpectedOrder()
		{
			m_mainWnd.ExpectAndReturn(3, "PreSynchronize", true, new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, SyncMsg.ksyncSimpleEdit);
			m_mainWnd.ExpectAndReturn("Synchronize", true, SyncMsg.ksyncScriptureNewBook);
			// Even though UndoRedo is the first synch message to be sent, it should be processed last.
			m_mainWnd.ExpectAndReturn("Synchronize", true, SyncMsg.ksyncUndoRedo);
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			m_app.SuppressSynchronize(Cache);
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);
			m_app.Synchronize(SyncMsg.ksyncSimpleEdit, Cache);
			m_app.Synchronize(SyncMsg.ksyncScriptureNewBook, Cache);
			m_app.Synchronize(SyncMsg.ksyncUndoRedo, Cache);

			// This should call (Pre)Synchronize three times
			m_app.ResumeSynchronize(Cache);

			m_mainWnd.Verify();
		}
		#endregion
	}
#endif
}
