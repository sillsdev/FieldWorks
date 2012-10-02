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
// File: TeTests.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
