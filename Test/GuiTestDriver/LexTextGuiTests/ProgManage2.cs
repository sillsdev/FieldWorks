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
// File: ProgManage2.cs
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
