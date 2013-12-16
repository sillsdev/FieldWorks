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
	/// For Notebook File menu scripts.
	/// </summary>
	[TestFixture]
	public class aFileMenu
	{
		RunTest m_rt = new RunTest("NB");
		public aFileMenu() { }

		[Test]
		public void aNbfileimpstd() { m_rt.fromFile("aNbfileimpstd.xml"); }

		[Test]
		public void aNbfilesave() { m_rt.fromFile("aNbfilesave.xml"); }

		[Test]
		public void aNbPrintdlg() { m_rt.fromFile("aNbPrintdlg.xml"); }

		[Test]
		public void aPrintBrowse() { m_rt.fromFile("aPrintBrowse.xml"); }

		[Test]
		public void aPrintRecEd() { m_rt.fromFile("aPrintRecEd.xml"); }

	}
}
