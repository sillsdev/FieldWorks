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
