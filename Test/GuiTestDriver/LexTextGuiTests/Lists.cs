// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Lists.cs
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
	/// For Lists scripts.
	/// </summary>
	[TestFixture]
	public class eLists
	{
		RunTest m_rt = new RunTest("LT");
		public eLists(){}

		[Test]
		public void liaVisitViews(){m_rt.fromFile("liaVisitViews.xml");}
		[Test]
		public void liFTinsDel() { m_rt.fromFile("liFTinsDel.xml"); }
		[Test]
		public void liLink2Edit() { m_rt.fromFile("liLink2Edit.xml"); }
		[Test]
		public void limAddDelItem(){m_rt.fromFile("limAddDelItem.xml");}
		[Test]
		public void liqEditLists() { m_rt.fromFile("liqEditLists.xml"); }
		[Test]
		public void liTransExpImp() { m_rt.fromFile("liTransExpImp.xml"); }

	}
}
