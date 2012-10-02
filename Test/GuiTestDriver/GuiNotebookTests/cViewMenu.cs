// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2003' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Lists.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Threading;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// For Notebook View and Data and Insert menu scripts.
	/// </summary>
	[TestFixture]
	public class cViewMenu
	{
		RunTest m_rt = new RunTest("NB");
		public cViewMenu() { }

		[Test]
		public void cNbdatarrow() { m_rt.fromFile("cNbdatarrow.xml"); }

		[Test]
		public void cNbinsextl() { m_rt.fromFile("cNbinsextl.xml"); }

		[Test]
		public void cNbinsspe() { m_rt.fromFile("cNbinsspe.xml"); }

		[Test]
		public void cNbInsSub() { m_rt.fromFile("cNbInsSub.xml"); }
		[Test]
		public void cNbvwrnshf() { m_rt.fromFile("cNbvwrnshf.xml"); }
	}
}
