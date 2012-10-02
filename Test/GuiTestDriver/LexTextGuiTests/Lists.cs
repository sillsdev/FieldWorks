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
// File: Lists.cs
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
