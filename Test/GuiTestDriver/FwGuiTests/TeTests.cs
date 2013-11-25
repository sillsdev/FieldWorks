// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeTests.cs
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
	/// Summary description for Class1.
	/// </summary>
	[TestFixture]
	public class TeTests
	{
		RunTest m_rt = new RunTest("TE");

		public TeTests()
		{
		}

		[Test]
		public void SyncToParaText()
		{
			m_rt.fromFile("SyncToParaText.xml");
		}

		[Test]
		public void JumpToLE()
		{
			m_rt.fromFile("JumpToLE.xml");
		}

		[Test]
		public void JumpToHyperlink()
		{
			m_rt.fromFile("JumpToHyperlink.xml");
		}

		[Test]
		public void MainWinPreMenu()
		{
			m_rt.fromFile("MainWinPreMenu.xml");
		}

		[Test]
		public void MainWinPreWidgets()
		{
			m_rt.fromFile("MainWinPreWidgets.xml");
		}

		[Test]
		public void MainWinFunWidgets()
		{
			m_rt.fromFile("MainWinFunWidgets.xml");
		}

		[Test]
		public void MainMenPre()
		{
			m_rt.fromFile("MainMenPre.xml");
		}

		[Test]
		public void MainMenScPre()
		{
			m_rt.fromFile("MainMenScPre.xml");
		}

		[Test]
		public void MainMenFunDia()
		{
			m_rt.fromFile("MainMenFunDia.xml");
		}

		[Test]
		public void MainMenChkTests()
		{
			m_rt.fromFile("mainMenChkTests.xml");
		}

		[Test]
		public void tbStandardPre()
		{
			m_rt.fromFile("tbStandardPre.xml");
		}

		[Test]
		public void tbFormatPre()
		{
			m_rt.fromFile("tbFormatPre.xml");
		}

		[Test]
		public void tbInsertPre()
		{
			m_rt.fromFile("tbInsertPre.xml");
		}

		[Test]
		public void sbPre()
		{
			m_rt.fromFile("sbPre.xml");
		}

		[Test]
		public void tbStandardFun()
		{
			m_rt.fromFile("tbStandardFun.xml");
		}

		[Test]
		public void infoBarPre()
		{
			m_rt.fromFile("infoBarPre.xml");
		}

		[Test]
		public void stbPre()
		{
			m_rt.fromFile("stbPre.xml");
		}

		[Test]
		public void diaFmtStylePre()
		{
			m_rt.fromFile("diaFmtStylePre.xml");
		}

		[Test]
		public void InsertText()
		{
			m_rt.fromFile("InsertText.xml");
		}

		[Test]
		public void diaNewFwProj()
		{
			m_rt.fromFile("diaNewFwProj.xml");
		}

		[Test]
		public void ProjDel()
		{
			m_rt.fromFile("ProjDel.xml");
		}
	}
}
