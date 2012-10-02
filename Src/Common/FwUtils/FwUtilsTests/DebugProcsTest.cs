// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DebugProcsTest.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
#if DEBUG
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for DebugProcs
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DebugProcsTest
	{
		#region DummyDebugProcs
		internal class DummyDebugProcs : DebugProcs
		{
			internal bool m_fHandlerCalled = false;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Creates a new object
			/// </summary>
			/// <param name="fShowAssertMsgBox"><c>true</c> to show message box on asserts,
			/// otherwise false</param>
			/// ------------------------------------------------------------------------------------
			public DummyDebugProcs(bool fShowAssertMsgBox)
				: base(fShowAssertMsgBox)
			{
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Callback method that gets all debug output from unmanaged FieldWorks code.
			/// </summary>
			/// <param name="nReportType">Type of report</param>
			/// <param name="szMsg">Message</param>
			/// ------------------------------------------------------------------------------------
			public override void Report(CrtReportType nReportType, string szMsg)
			{
				CheckDisposed();
				m_fHandlerCalled = true;
			}
		}
		#endregion // DummyDebugProcs

		private string m_LogFileName = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the logfile name because we're producing some asserts here as part of our
		/// test, so we don't want them to appear in the output
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void Setup()
		{
			if (Debug.Listeners[0] is DefaultTraceListener)
			{
				m_LogFileName = ((DefaultTraceListener)Debug.Listeners[0]).LogFileName;
				((DefaultTraceListener)Debug.Listeners[0]).LogFileName = string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called after all tests are run
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void TearDown()
		{
			if (m_LogFileName != null && m_LogFileName != string.Empty)
				((DefaultTraceListener)Debug.Listeners[0]).LogFileName = m_LogFileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the message box isn't shown for asserts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DontShowMessageBoxForAsserts()
		{
			using (DebugProcs debugProcs = new DebugProcs(false))
			{
				ITsIncStrBldr bldr = TsIncStrBldrClass.Create();
				// asserts - this brings up a message box if ShowAssertMessageBox doesn't work
				bldr.SetIntPropValues(1, 0, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the report method gets the messages
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReportHook()
		{
			using (DummyDebugProcs debugProcs = new DummyDebugProcs(false))
			{
				ITsIncStrBldr bldr = TsIncStrBldrClass.Create();
				// next line asserts
				bldr.SetIntPropValues(1, 0, 0);

				Assert.IsTrue(debugProcs.m_fHandlerCalled);
			}
		}
	}
#endif
}
