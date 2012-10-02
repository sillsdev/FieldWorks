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
// File: Class1.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
