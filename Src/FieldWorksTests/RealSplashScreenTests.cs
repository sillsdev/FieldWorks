// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Test FwCoreDlgs RealSplashScreen
	/// </summary>
	[TestFixture]
	public class RealSplashScreenTests
	{
		/// <summary>
		/// Basic test of RealSplashScreen
		/// </summary>
		[Test]
		public void Basic()
		{
			using (var window = new RealSplashScreen(false))
			{
				Assert.NotNull(window);
			}
		}

		/// <summary>
		/// Test running RealSplashScreen on main thread
		/// </summary>
		[Test]
		[Platform(Include = "Linux")]
		public void RunOnMainThread()
		{
			using (var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset))
			using (var window = new RealSplashScreen(false))
			{
				window.WaitHandle = waitHandle;
				window.CreateControl();
				window.Message = string.Empty;
				window.Show();
				Application.DoEvents();
			}
		}
	}
}
