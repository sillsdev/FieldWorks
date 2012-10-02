// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010 SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RealSplashScreenTests.cs
// Authorship History: MarkS
// ---------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Test FwCoreDlgs RealSplashScreen
	/// </summary>
	[TestFixture]
	public class RealSplashScreenTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		/// <summary>
		/// Basic test of RealSplashScreen
		/// </summary>
		[Test]
		public void Basic()
		{
			using (var window = new RealSplashScreen(false))
				Assert.NotNull(window);
		}

#if __MonoCS__
		/// <summary>
		/// Test running RealSplashScreen on main thread
		/// </summary>
		[Test]
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
#endif
	}
}
