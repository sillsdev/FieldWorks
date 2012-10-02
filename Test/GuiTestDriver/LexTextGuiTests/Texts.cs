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
// File: Texts.cs
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
	/// For Texts scripts.
	/// </summary>
	[TestFixture]
	public class bTexts
	{
		RunTest m_rt = new RunTest("LT");
		public bTexts(){}

		[Test]
		public void txaVisitViews(){m_rt.fromFile("txaVisitViews.xml");}
		[Test]
		public void txbEdit() { m_rt.fromFile("txEdit.xml"); }
		[Test]
		public void txcSortLxGm() { m_rt.fromFile("txSortLxGm.xml"); }
		[Test]
		public void txdShortcuts() { m_rt.fromFile("txtShortcuts.xml"); }
		[Test]
		public void txeDiscourse() { m_rt.fromFile("txDiscourse.xml"); }
		[Test]
		public void txfConfigIL() { m_rt.fromFile("txfConfigIL.xml"); }
	}
}
