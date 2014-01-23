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
	/// For Notebook Format and Tools menu scripts.
	/// </summary>
	[TestFixture]
	public class dFormatMenu
	{
		RunTest m_rt = new RunTest("NB");
		public dFormatMenu() { }

		[Test]
		public void dNbfmtastyle() { m_rt.fromFile("dNbfmtastyle.xml"); }

		[Test]
		public void dNbfmtstyle() { m_rt.fromFile("dNbfmtstyle.xml"); }

		[Test]
		public void dNbfmtsuws() { m_rt.fromFile("dNbfmtsuws.xml"); }

		[Test]
		public void dNbfmtwritsy() { m_rt.fromFile("dNbfmtwritsy.xml"); }
		[Test]
		public void dNbtlsconfigure() { m_rt.fromFile("dNbtlsconfigure.xml"); }
		[Test]
		public void dNbtlsspl() { m_rt.fromFile("dNbtlsspl.xml"); }
	}
}
