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
// File: ScriptDev.cs
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
	/// For developing new scripts.
	/// </summary>
	[TestFixture]
	public class ScriptDev
	{
		RunTest m_rt = new RunTest("LT");

		public ScriptDev()
		{
		}

		[Test]
		public void NewTest1()
		{
			m_rt.fromFile("NewTest1");
		}

		[Test]
		public void NewTest2()
		{
			m_rt.fromFile("NewTest2");
		}

		[Test]
		public void NewTest3()
		{
			m_rt.fromFile("NewTest3");
		}

		[Test]
		public void NewTest4()
		{
			m_rt.fromFile("NewTest4");
		}

		[Test]
		public void NewTest5()
		{
			m_rt.fromFile("NewTest5");
		}

		[Test]
		public void NewTest6()
		{
			m_rt.fromFile("NewTest6");
		}

		[Test]
		public void NewTest7()
		{
			m_rt.fromFile("NewTest7");
		}

		[Test]
		public void NewTest8()
		{
			m_rt.fromFile("NewTest8");
		}

		[Test]
		public void NewTest9()
		{
			m_rt.fromFile("NewTest9");
		}

		[Test]
		public void NewTest10()
		{
			m_rt.fromFile("NewTest10");
		}

	}
}
