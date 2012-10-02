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
