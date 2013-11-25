// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TestStateTest.cs
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
	/// Tests the TestState class.
	/// </summary>
	[TestFixture]
	public class TestStateTest
	{
		public TestStateTest()
		{
		}

		private TestState m_ts;

		/// <summary>
		/// Create a test state for each test below
		/// </summary>
		[TestFixtureSetUp]
		public void Init()
		{
			m_ts = TestState.getOnly("LT");
			m_ts.Script = "TestStateTest.xml";
		}

		/// <summary>
		/// Try to create a test state object.
		/// </summary>
		[Test]
		public void TestStateExists()
		{
			Assert.IsNotNull(m_ts, "TestState not created.");
		}
		[Test]
		public void AddInstructionToTestState()
		{
			Instruction ins = AddNamedInstruction("Name");
			Assert.AreEqual(ins,m_ts.Instruction("Name"),"TestState failed to add and recall a named instruction.");
		}
		[Test]
		public void AddInstructionsToTestState()
		{
			int count;
			for (count = 1; count == 10; count++)
			{
				Instruction ins = AddNamedInstruction(Convert.ToString(count));
				Assert.AreEqual(count,m_ts.Instruction(Convert.ToString(count)),"TestState failed to add and recall the instruction named "+count+".");
			}
		}
		public Instruction AddNamedInstruction(string name)
		{
			NoOp ins = new NoOp();
			Assert.IsNotNull(ins,"Instruction "+name+" not created.");
			m_ts.AddNamedInstruction(name,ins);
			return ins;
		}
	}
}
