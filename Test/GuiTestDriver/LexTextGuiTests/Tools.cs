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
	public class fTools
	{
		RunTest m_rt = new RunTest("LT");
		public fTools() { }

		[Test]
		public void tlsApplyStyles() { m_rt.fromFile("tlsApplyStyles.xml"); }
		[Test]
		public void tlsBarStyles() { m_rt.fromFile("tlsBarStyles.xml"); }
		[Test]
		public void tlsCustomFields() { m_rt.fromFile("tlsCustomFields.xml"); }
		[Test]
		public void tlsFilterForItems() { m_rt.fromFile("tlsFilterForItems.xml"); }
		[Test]
		public void tlsOptionsDiag() { m_rt.fromFile("tlsOptionsDiag.xml"); }
		[Test]
		public void tlszDelete() { m_rt.fromFile("tlsDelete.xml"); }
	}
}
