// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScriptDev.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Threading;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// For developing new scripts.
	/// </summary>
	[TestFixture]
	public class ScriptDev
	{
		RunTest m_rt = new RunTest("TE");

		public ScriptDev()
		{
		}

		[Test]
		public void NewTest1()
		{
			m_rt.fromFile("NewTest1");
		}

		[Test]
		public void NewTest2()
		{
			m_rt.fromFile("NewTest2");
		}

		[Test]
		public void NewTest3()
		{
			m_rt.fromFile("NewTest3");
		}

		[Test]
		public void NewTest4()
		{
			m_rt.fromFile("NewTest4");
		}

		[Test]
		public void NewTest5()
		{
			m_rt.fromFile("NewTest5");
		}

		[Test]
		public void NewTest6()
		{
			m_rt.fromFile("NewTest6");
		}

		[Test]
		public void NewTest7()
		{
			m_rt.fromFile("NewTest7");
		}

		[Test]
		public void NewTest8()
		{
			m_rt.fromFile("NewTest8");
		}

		[Test]
		public void NewTest9()
		{
			m_rt.fromFile("NewTest9");
		}

		[Test]
		public void NewTest10()
		{
			m_rt.fromFile("NewTest10");
		}

	}
}
