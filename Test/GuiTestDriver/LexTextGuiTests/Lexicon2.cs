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
// File: Lexicon2.cs
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
	/// For half of the Lexicon scripts.
	/// </summary>
	[TestFixture]
	public class aLexicon2
	{
		RunTest m_rt = new RunTest("LT");
		public aLexicon2() { }

		[Test]
		public void lxdBulkEdit() { m_rt.fromFile("lxdBulkEdit.xml"); }
		[Test]
		public void lxDictLex() { m_rt.fromFile("lxDictLex.xml"); }
		[Test]
		public void lxeBulkReplace() { m_rt.fromFile("lxeBulkReplace.xml"); }
		[Test]
		public void lxfaRegExp() { m_rt.fromFile("lxfaRegExp.xml"); }
		[Test]
		public void lxfREBulkReplace() { m_rt.fromFile("lxfREBulkReplace.xml"); }
		[Test]
		public void lxgCombExp() { m_rt.fromFile("lxgCombExp.xml"); }
		[Test]
		public void lxShortcuts() { m_rt.fromFile("lxShortcuts.xml"); }
		[Test]
		public void lxuLT_2882() { m_rt.fromFile("lxuLT_2882.xml"); }
		[Test]
		public void lxzFindExample() { m_rt.fromFile("lxzFindExample.xml"); }

	}
}
