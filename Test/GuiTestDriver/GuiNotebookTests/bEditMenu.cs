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
	/// For Notebook Edit menu scripts.
	/// </summary>
	[TestFixture]
	public class bEditMenu
	{
		RunTest m_rt = new RunTest("NB");
		public bEditMenu() { }

		[Test]
		public void bNbedtccp() { m_rt.fromFile("bNbedtccp.xml"); }

		[Test]
		public void bNbedtcph() { m_rt.fromFile("bNbedtcph.xml"); }

		[Test]
		public void bNbedtdel() { m_rt.fromFile("bNbedtdel.xml"); }

		[Test]
		public void bNbedtfind() { m_rt.fromFile("bNbedtfind.xml"); }

		[Test]
		public void bNbedtLeRec() { m_rt.fromFile("bNbedtLeRec.xml"); }

		[Test]
		public void bNbedtselall() { m_rt.fromFile("bNbedtselall.xml"); }

		[Test]
		public void bNbedtundoredo() { m_rt.fromFile("bNbedtundoredo.xml"); }
	}
}
