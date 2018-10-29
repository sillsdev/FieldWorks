// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary />
	[TestFixture]
	public class ProgressDlgTests
	{
		private ThreadHelper m_threadHelper;
		private DummyProgressDlg m_dlg;
		private Timer m_timer;

		/// <summary>
		/// Set up the test
		/// </summary>
		[SetUp]
		public void Setup()
		{
			m_threadHelper = new ThreadHelper();
			m_dlg = new DummyProgressDlg(m_threadHelper) { Maximum = 10 };
		}

		/// <summary>
		/// Tear down this instance.
		/// </summary>
		[TearDown]
		public void Teardown()
		{
			if (m_timer != null)
			{
				m_timer.Dispose();
				m_timer = null;
			}

			if (m_dlg != null)
			{
				m_dlg.Dispose();
				m_dlg = null;
			}

			if (m_threadHelper != null)
			{
				m_threadHelper.Dispose();
				m_threadHelper = null;
			}

		}

		/// <summary>
		/// The task to do in the background. We just increment the progress and sleep in
		/// between.
		/// </summary>
		private static object BackgroundTask(IThreadedProgress progressDlg, object[] parameters)
		{
			var i = 0;
			for (; i < 10; i++)
			{
				if (progressDlg.Canceled)
				{
					break;
				}
				progressDlg.Step(1);
				Thread.Sleep(1000);
			}

			return i;
		}

		/// <summary>
		/// Test the progress dialog and verifies the cancel button doesn't show when there
		/// isn't a Cancel delegate specified. Also verifies the count is correct at the end,
		/// although that's not an incredibly exciting test.
		/// </summary>
		[Test]
		[Category("DesktopRequired")]
		public void TestWithoutCancel()
		{
			var nProgress = (int)m_dlg.RunTask(false, BackgroundTask);
			Assert.AreEqual(10, nProgress);
		}
	}
}
