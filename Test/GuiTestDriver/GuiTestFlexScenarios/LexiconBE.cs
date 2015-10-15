// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Threading;
using NUnit.Framework;

namespace GuiTestDriver
{
	[TestFixture]
	public class LexiconBE
	{
		RunTest m_rt = new RunTest("FS");

		public LexiconBE()
		{
		}

		[Test]
		public void lxBePart1()
		{
			m_rt.fromFile("lxBePart1");
		}

		[Test]
		public void lxBePart2()
		{
			m_rt.fromFile("lxBePart2");
		}

		[Test]
		public void lxBePart3()
		{
			m_rt.fromFile("lxBePart3");
		}

		[Test]
		public void lxBePart4()
		{
			m_rt.fromFile("lxBePart4");
		}

		[Test]
		public void lxBePart5()
		{
			m_rt.fromFile("lxBePart5");
		}

		[Test]
		public void lxBePart6()
		{
			m_rt.fromFile("lxBePart6");
		}

		[Test]
		public void lxBePart7()
		{
			m_rt.fromFile("lxBePart7");
		}

		[Test]
		public void lxBePart8()
		{
			m_rt.fromFile("lxBePart8");
		}

		[Test]
		public void lxBePart9()
		{
			m_rt.fromFile("lxBePart9");
		}


	}
}
