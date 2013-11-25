// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Lexicon.cs
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
	/// For Lexicon scripts.
	/// </summary>
	[TestFixture]
	public class aLexicon
	{
		RunTest m_rt = new RunTest("LT");
		public aLexicon(){}

		[Test]
		public void lxaaVisitViews(){m_rt.fromFile("lxaaVisitViews.xml");}
		[Test]
		public void lxabDataNav(){m_rt.fromFile("lxabDataNav.xml");}
		[Test]
		public void lxacShowAllFields(){m_rt.fromFile("lxacShowAllFields.xml");}
		[Test]
		public void lxBEentries() { m_rt.fromFile("lxBEentries.xml"); }
		[Test]
		public void lxBkCopy() { m_rt.fromFile("lxBkCopy.xml"); }
		[Test]
		public void lxCatEntry() { m_rt.fromFile("lxCatEntry.xml"); }
		[Test]
		public void lxChangeStat() { m_rt.fromFile("lxChangeStat.xml"); }
		[Test]
		public void lxCkCopy() { m_rt.fromFile("lxCkCopy.xml"); }
	}
}
