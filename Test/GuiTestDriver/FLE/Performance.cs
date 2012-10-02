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
//using System;
//using System.Threading;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// For Performance scripts.
	/// </summary>
	[TestFixture]
	public class Performance
	{
		RunTest m_rt = new RunTest("FLE");
		public Performance() { }

		[Test]
		public void aaThaiOpen() { m_rt.fromFile("aaThaiOpen.xml"); }
		[Test]
		public void abThaiReopen() { m_rt.fromFile("abThaiReopen.xml"); }
		[Test]
		public void acThaiSort() { m_rt.fromFile("acThaiSort.xml"); }
		[Test]
		public void adThaiViews() { m_rt.fromFile("adThaiViews.xml"); }
		[Test]
		public void baTagaOpen() { m_rt.fromFile("baTagaOpen.xml"); }
		[Test]
		public void bbTagaWords() { m_rt.fromFile("bbTagaWords.xml"); }
		[Test]
		public void caMutsunOpen() { m_rt.fromFile("caMutsunOpen.xml"); }
		[Test]
		public void cbMutsun() { m_rt.fromFile("cbMutsun.xml"); }
		[Test]
		public void ccMutsun() { m_rt.fromFile("ccMutsun.xml"); }
		[Test]
		public void daEngBibleOpen() { m_rt.fromFile("daEngBibleOpen.xml"); }
		[Test]
		public void dbEngBible() { m_rt.fromFile("dbEngBible.xml"); }
	}
}
