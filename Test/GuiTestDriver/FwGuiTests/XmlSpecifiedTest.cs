// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Class1.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	[TestFixture]
	public class XmlSpecifiedClass
	{
		RunTest m_rt = new RunTest("TE");

		public XmlSpecifiedClass()
		{
		}

		[Test]
		public void ChangeTextTest()
		{
			m_rt.fromFile("ChangeText.xml");
		}

		[Test]
		public void ContentNavTest()
		{
			m_rt.fromFile("ContentNav.xml");
		}
	}
}
