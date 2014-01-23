// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProgManage2.cs
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
	public class ProgManage2
	{
		RunTest m_rt = new RunTest("LT");

		public ProgManage2() { }

		[Test]
		public void pmgLT_2602() { m_rt.fromFile("pmgLT_2602.xml"); }
		[Test]
		public void pmgProp() { m_rt.fromFile("pmgProp.xml"); }
		[Test]
		public void pmgShortcuts() { m_rt.fromFile("pmgShortcuts.xml"); }
		[Test]
		public void pmgStartTE() { m_rt.fromFile("pmgStartTE.xml"); }
		[Test]
		public void pmImport() { m_rt.fromFile("pmImport"); }
		[Test]
		public void pmMoveProj() { m_rt.fromFile("pmMoveProj"); }
		[Test]
		public void pmpDelProject() { m_rt.fromFile("pmpDelProject.xml"); }
		[Test]
		public void pmrRestoreBackup() { m_rt.fromFile("pmrRestoreBackup.xml"); }
	}
}
