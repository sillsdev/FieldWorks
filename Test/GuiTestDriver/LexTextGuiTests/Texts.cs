// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Texts.cs
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
		public void txbTryStyles() { m_rt.fromFile("txTryStyles.xml"); }
		[Test]
		public void txcSortLxGm() { m_rt.fromFile("txSortLxGm.xml"); }
		[Test]
		public void txdShortcuts() { m_rt.fromFile("txtShortcuts.xml"); }
		[Test]
		public void txeDiscourse() { m_rt.fromFile("txDiscourse.xml"); }
		[Test]
		public void txfConfigIL() { m_rt.fromFile("txfConfigIL.xml"); }
		[Test]
		public void txGlossTab() { m_rt.fromFile("txGlossTab.xml"); }
	}
}
