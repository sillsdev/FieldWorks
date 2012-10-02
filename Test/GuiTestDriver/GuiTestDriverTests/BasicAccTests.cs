// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BasicAccTests.cs
// Responsibility: Testing
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Basic Acceptance Tests for the Test Driver.
	/// </summary>
	[TestFixture]
	public class BasicAccTests
	{
		RunTest m_rt = new RunTest("FW");

		public BasicAccTests()
		{
		}
		[Test]
		public void LaunchTest()
		{
			m_rt.fromFile("LaunchTest.xml");
		}
		[Test]
		public void CloseTest()
		{
			m_rt.fromFile("CloseTest.xml");
		}
		[Test]
		public void LaunchCloseTest()
		{
			m_rt.fromFile("LaunchCloseTest.xml");
		}

		[Test]
		public void ClickTest()
		{
			m_rt.fromFile("ClickTest.xml");
		}

		[Test]
		public void GlimpseTest()
		{
			m_rt.fromFile("GlimpseTest.xml");
		}

		[Test]
		public void IfTest()
		{
			m_rt.fromFile("IfTest.xml");
		}

		[Test]
		public void GlimpseExtraTest()
		{
			m_rt.fromFile("GlimpseExtraTest.xml");
		}

		[Test]
		public void SelectStringTest()
		{
			m_rt.fromFile("SelectStringTest.xml");
		}

		[Test]
		public void MatchStringTest()
		{
			m_rt.fromFile("MatchStringsTest.xml");
		}

		[Test]
		public void GlimpseSelectTest()
		{
			m_rt.fromFile("GlimpseSelectTest.xml");
		}

		// the last test script must use <on-application close="yes" .... >
		// otherwise at least one other later test (UtilitiesTest/getAhFromGpath)
		// will fail to launch its own process.

	}
}
