// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Grammar.cs
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
	/// For Grammar scripts.
	/// </summary>
	[TestFixture]
	public class dGrammar
	{
		RunTest m_rt = new RunTest("LT");
		public dGrammar(){}

		[Test]
		public void gmaVisitViews(){m_rt.fromFile("gmaVisitViews.xml");}
		[Test]
		public void gmCatEd() { m_rt.fromFile("gmCatEd.xml"); }
		[Test]
		public void gmdAddDelNatClass(){m_rt.fromFile("gmdAddDelNatClass.xml");}
		[Test]
		public void gmEFinsDel() { m_rt.fromFile("gmEFinsDel.xml"); }
		[Test]
		public void gmFeaInsDel() { m_rt.fromFile("gmFeaInsDel.xml"); }
		[Test]
		public void gmhCategories(){m_rt.fromFile("gmhCategories.xml");}
		[Test]
		public void gmmCatBrowseFilter(){m_rt.fromFile("gmmCatBrowseFilter.xml");}
		[Test]
		public void gmPhonemes() { m_rt.fromFile("gmPhonemes.xml"); }
		[Test]
		public void gmrLT_2693() { m_rt.fromFile("gmrLT_2693.xml"); }

	}
}
