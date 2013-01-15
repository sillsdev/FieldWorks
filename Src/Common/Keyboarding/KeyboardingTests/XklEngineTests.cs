// Copyright (c) 2013, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2013-01-04 XklEngineTests.cs

#if __MonoCS__
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.Keyboarding.Linux;
using SIL.FieldWorks.Test.TestUtils;
using X11.XKlavier;

namespace SIL.FieldWorks.Common.Keyboarding
{
	[TestFixture]
	[Platform(Include="Linux", Reason="Linux specific tests")]
	[SetUICulture("en-US")]
	public class XklEngineTests: BaseTest
	{
		/// <summary>
		/// Can be created and closed. Doesn't crash.
		/// </summary>
		[Test]
		public void Basic()
		{
			var engine = new XklEngine();
			engine.Close();
		}

		/// <summary/>
		[Test]
		public void UseAfterClose_NotCrash()
		{
			var engine = new XklEngine();
			engine.Close();
			engine.SetGroup(0);
		}

		/// <summary/>
		[Test]
		public void MultipleEngines_ClosedInReverseOrder_NotCrash()
		{
			var engine1 = new XklEngine();
			var engine2 = new XklEngine();
			engine2.Close();
			engine1.Close();
		}

		/// <summary/>
		[Test]
		public void MultipleEngines_ClosedInOpenOrder_NotCrash()
		{
			var engine1 = new XklEngine();
			var engine2 = new XklEngine();
			engine1.Close();
			engine2.Close();
		}

		/// <summary/>
		[Test]
		public void GetDisplayConnection()
		{
			var displayConnection = XklEngine.GetDisplayConnection();
			Assert.That(displayConnection, Is.Not.EqualTo(IntPtr.Zero), "Expected display connection");
		}
	}
}
#endif
