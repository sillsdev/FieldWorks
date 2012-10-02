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
