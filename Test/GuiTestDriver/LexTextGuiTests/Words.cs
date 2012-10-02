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
// File: Words.cs
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
	/// For Words scripts.
	/// </summary>
	[TestFixture]
	public class cWords
	{
		RunTest m_rt = new RunTest("LT");
		public cWords(){}

		[Test]
		public void wdAnaStat() { m_rt.fromFile("wdAnaStat.xml"); }
		[Test]
		public void wdaVisitViews(){m_rt.fromFile("wdaVisitViews.xml");}
		[Test]
		public void wdbAnalysisCols(){m_rt.fromFile("wdbAnalysisCols.xml");}
		[Test]
		public void wdcAnalysisChecks() { m_rt.fromFile("wdcAnalysisChecks.xml"); }
		[Test]
		public void wdConcDic() { m_rt.fromFile("wdConcDic.xml"); }
		[Test]
		public void wdInsDelGloss() { m_rt.fromFile("wdInsDelGloss.xml"); }
		[Test]
		public void wdShortcuts() { m_rt.fromFile("wdShortcuts.xml"); }
		[Test]
		public void wdSStat() { m_rt.fromFile("wdSStat.xml"); }
		[Test]
		public void wdwritSysDia() { m_rt.fromFile("wdwritSysDia.xml"); }

	}
}
