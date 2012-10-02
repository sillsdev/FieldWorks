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
// File: Grammar.cs
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
