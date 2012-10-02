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
// File: Lexicon.cs
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
		[Test]
		public void lxClassDict() { m_rt.fromFile("lxClassDict.xml"); }
		[Test]
		public void lxConfDict() { m_rt.fromFile("lxConfDict.xml"); }
	}
}
