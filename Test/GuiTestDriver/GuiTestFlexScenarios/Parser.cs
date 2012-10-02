using System;
using System.Threading;
using NUnit.Framework;

namespace GuiTestDriver
{
	[TestFixture]
	public class Parser
	{
		RunTest m_rt = new RunTest("FS");

		public Parser()
		{
		}

		[Test]
		public void cipPart1()
		{
			m_rt.fromFile("cipPart1");
		}

		[Test]
		public void cipPart2()
		{
			m_rt.fromFile("cipPart2");
		}

		[Test]
		public void cipPart3()
		{
			m_rt.fromFile("cipPart3");
		}

		[Test]
		public void cipPart4()
		{
			m_rt.fromFile("cipPart4");
		}

		[Test]
		public void cipPart5()
		{
			m_rt.fromFile("cipPart5");
		}

		[Test]
		public void cipPart6()
		{
			m_rt.fromFile("cipPart6");
		}

		[Test]
		public void cipPart7()
		{
			m_rt.fromFile("cipPart7");
		}

	}
}
