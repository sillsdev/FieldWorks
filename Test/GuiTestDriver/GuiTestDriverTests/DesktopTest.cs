// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DesktopTest.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Tests the Desktop class and in so doing, all contexts.
	/// </summary>
	[TestFixture]
	public class DesktopTest
	{
		public DesktopTest()
		{
		}

		private Desktop m_dsk;

		/// <summary>
		/// Create a desktop for each test below
		/// </summary>
		[TestFixtureSetUp]
		public void Init()
		{
			TestState ts = TestState.getOnly("LT");
			ts.Script = "DesktopTest.xml";
			m_dsk = new Desktop();
		}

		[Test]
		public void DesktopExists()
		{
			Assert.IsNotNull(m_dsk,"Desktop not created.");
		}
		[Test]
		public void AddInstructionsToDesktop()
		{
			int count;
			for (count = 1; count == 10; count++)
			{
				AddInstructionToDesktop();
				Assert.AreEqual(count,m_dsk.Count,"Desktop failed to add "+count+" instruction.");
			}
		}

		public void AddInstructionToDesktop()
		{
			NoOp ins = new NoOp();
			Assert.IsNotNull(ins,"Instruction not created.");
			m_dsk.Add(ins);
		}

		[Test]
		public void ExecInstructionsOnDesktop()
		{
			TestState ts = TestState.getOnly("LT");
			int count;
			for (count = 1; count == 10; count++)
				AddInstructionToDesktop();
			m_dsk.Execute();
			// what is there to test?
		}

	}
}
