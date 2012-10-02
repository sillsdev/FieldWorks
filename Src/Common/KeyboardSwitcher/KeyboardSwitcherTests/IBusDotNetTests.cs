// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using NUnit.Framework;
using IBusDotNet;

namespace KeyboardSwitcherTests
{
	/// <summary>
	/// Tests for ibusdotnet. These tests really belong to ibusdotnet, but they are here so that
	/// we run them regularly in order to identify problems with updated ibus libraries (which
	/// might change the API)
	/// </summary>
	[TestFixture]
	public class IBusDotNetTests // can't derive from BaseTest because of circular dependency
	{
		private IBusConnection Connection;

		/// <summary>
		/// Close connection to ibus
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			if (Connection != null)
				Connection.Dispose();
			Connection = null;
		}

		/// <summary>
		/// Tests that we can get the ibus engine. This will fail if the ibus API changed.
		/// </summary>
		[Test]
		public void CanGetEngineDesc()
		{
			Connection = IBusConnectionFactory.Create();
			if (Connection == null)
			{
				Assert.Ignore("Can't run this test without ibus running.");
				return;
			}

			var ibusWrapper = new IBusDotNet.InputBusWrapper(Connection);
			object[] engines = ibusWrapper.InputBus.ListActiveEngines();
			if (engines.Length == 0)
			{
				Assert.Ignore("Can't run this test without any ibus keyboards installed.");
				return;
			}

			Assert.IsNotNull(IBusEngineDesc.GetEngineDesc(engines[0]));
		}
	}
}
#endif
