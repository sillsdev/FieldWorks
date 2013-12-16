// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Lexicon3.cs
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
	public class aLexicon3
	{
		RunTest m_rt = new RunTest("LT");
		public aLexicon3() { }

		[Test]
		public void lxEditCat() { m_rt.fromFile("lxEditCat.xml"); }
		[Test]
		public void lxfREBulkReplace() { m_rt.fromFile("lxfREBulkReplace.xml"); }
		[Test]
		public void lxgCombExp() { m_rt.fromFile("lxgCombExp.xml"); }
		[Test]
		public void lxMergeEntry() { m_rt.fromFile("lxMergeEntry.xml"); }
		[Test]
		public void lxShortcuts() { m_rt.fromFile("lxShortcuts.xml"); }
		[Test]
		public void lxuLT_2882() { m_rt.fromFile("lxuLT_2882.xml"); }

	}
}
