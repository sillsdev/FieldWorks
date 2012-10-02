// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Alpha.cs
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
	/// Summary description for Demo.
	/// </summary>
	[TestFixture]
	public class Alpha
	{
		RunTest m_rt = new RunTest("LT");

		public Alpha()
		{
		}

		[Test]
		public void AfterInstall()
		{
			m_rt.fromFile("FlexStartUpTest");
		}

	}
}
