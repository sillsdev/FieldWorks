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
// File: Lexicon3.cs
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
