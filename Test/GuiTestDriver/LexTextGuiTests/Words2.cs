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
	public class cWords2
	{
		RunTest m_rt = new RunTest("LT");
		public cWords2() { }

		[Test]
		public void wdInsertScripture() { m_rt.fromFile("wdInsertScripture.xml"); }
		[Test]
		public void wdzAssignAnalysis() { m_rt.fromFile("wdzAssignAnalysis.xml"); }
		[Test]
		public void wdzSpelling() { m_rt.fromFile("wdzSpelling.xml"); }

	}
}
