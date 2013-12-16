// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Lexicon.cs
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
