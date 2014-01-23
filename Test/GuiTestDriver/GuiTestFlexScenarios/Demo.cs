// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Demo.cs
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
	/// Summary description for Demo.
	/// </summary>
	[TestFixture]
	public class Demo
	{
		RunTest m_rt = new RunTest("FS");

		public Demo()
		{
		}

		[Test]
		public void DemoA()
		{
			m_rt.fromFile("Demo");
		}

		[Test]
		public void DemoPartD()
		{
			m_rt.fromFile("DemoD");
		}

		[Test]
		public void DemoPartF()
		{
			m_rt.fromFile("DemoF");
		}

		[Test]
		public void DemoPartG()
		{
			m_rt.fromFile("DemoG");
		}

		[Test]
		public void DemoPartH()
		{
			m_rt.fromFile("DemoH");
		}

		[Test]
		public void DemoPartI()
		{
			m_rt.fromFile("DemoI");
		}

		[Test]
		public void DemoPartJ()
		{
			m_rt.fromFile("DemoJ");
		}

		[Test]
		public void DemoPartK()
		{
			m_rt.fromFile("DemoK");
		}

	}
}
