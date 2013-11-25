// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: aaSetup.cs
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
	/// For Texts scripts.
	/// </summary>
	[TestFixture]
	public class aaSetup
	{
		RunTest m_rt = new RunTest("LT");
		public aaSetup() { }

		[Test]
		public void RestoreKalaba() { m_rt.fromFile("RestoreKalaba.xml"); }
	}
}
