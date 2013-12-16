// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Lists.cs
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
