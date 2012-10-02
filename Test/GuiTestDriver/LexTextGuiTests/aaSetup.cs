// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2003' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: aaSetup.cs
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
