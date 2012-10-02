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
	/// For Notebook Window and Help menus and Toolbar scripts.
	/// </summary>
	[TestFixture]
	public class eWindowMenu
	{
		RunTest m_rt = new RunTest("NB");
		public eWindowMenu() { }

		[Test]
		public void eNbfwhelp() { m_rt.fromFile("eNbfwhelp.xml"); }

		[Test]
		public void eNbtbstd() { m_rt.fromFile("eNbtbstd.xml"); }

		[Test]
		public void eNbwindowm() { m_rt.fromFile("eNbwindowm.xml"); }

	}
}
