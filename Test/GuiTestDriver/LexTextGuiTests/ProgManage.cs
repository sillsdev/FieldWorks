// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeTests.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.IO;			// for Path
using System.Threading;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	[TestFixture]
	public class ProgManage
	{
		RunTest m_rt = new RunTest("LT");

		public ProgManage(){}

		[Test]
		public void paaSounds(){m_rt.fromFile("paaSounds");}
		[Test]
		public void pabHyperlink(){m_rt.fromFile("pabHyperlink.xml");}
		[Test]
		public void pmeVernacularWsSwap(){m_rt.fromFile("pmeVernacularWsSwap.xml");}
		[Test]
		public void pmgAddDelStyle() { m_rt.fromFile("pmgAddDelStyle.xml"); }
		[Test]
		public void pmgEditFindUndo() { m_rt.fromFile("pmgEditFindUndo.xml"); }
		[Test]
		public void pmgExtLink2Dn(){m_rt.fromFile("pmgExtLink2Dn.xml");}
		[Test]
		public void pmgHelp() { m_rt.fromFile("pmgHelp.xml"); }
		[Test]
		public void pmgHelp2() { m_rt.fromFile("pmgHelp2.xml"); }

	}
}
