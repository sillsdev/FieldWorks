// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Alpha.cs
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
