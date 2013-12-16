// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Words.cs
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
