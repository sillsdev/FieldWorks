// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Lexicon2.cs
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
	public class aLexicon2
	{
		RunTest m_rt = new RunTest("LT");
		public aLexicon2() { }

		[Test]
		public void lxClassDict() { m_rt.fromFile("lxClassDict.xml"); }
		[Test]
		public void lxConfDict() { m_rt.fromFile("lxConfDict.xml"); }
		[Test]
		public void lxdBulkEdit() { m_rt.fromFile("lxdBulkEdit.xml"); }
		[Test]
		public void lxdBulkRepSetup() { m_rt.fromFile("lxdBulkRepSetup.xml"); }
		[Test]
		public void lxDictionaryFind() { m_rt.fromFile("lxDictionaryFind.xml"); }
		[Test]
		public void lxDictLex() { m_rt.fromFile("lxDictLex.xml"); }
		[Test]
		public void lxeBulkReplace() { m_rt.fromFile("lxeBulkReplace.xml"); }
		[Test]
		public void lxfaRegExp() { m_rt.fromFile("lxfaRegExp.xml"); }

	}
}
