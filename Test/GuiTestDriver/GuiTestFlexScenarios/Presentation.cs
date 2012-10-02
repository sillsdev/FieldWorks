using System;
using System.Threading;
using NUnit.Framework;

// this needs a comment

namespace GuiTestDriver
{
	[TestFixture]
	public class Presentation
	{
		RunTest m_rt = new RunTest("FS");

		public Presentation()
		{
		}

		[Test]
		public void abPres1()
		{
			m_rt.fromFile("abPres1");
		}

		[Test]
		public void abPres2()
		{
			m_rt.fromFile("abPres2");
		}

		[Test]
		public void abPres3()
		{
			m_rt.fromFile("abPres3");
		}

		[Test]
		public void abPres4()
		{
			m_rt.fromFile("abPres4");
		}

		[Test]
		public void abPres5()
		{
			m_rt.fromFile("abPres5");
		}


	}
}
