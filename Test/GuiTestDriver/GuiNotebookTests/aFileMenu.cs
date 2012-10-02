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
