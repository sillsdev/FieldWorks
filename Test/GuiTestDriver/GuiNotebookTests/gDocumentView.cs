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
	/// For Notebook Views and Filter test scripts.
	/// </summary>
	[TestFixture]
	public class gDocumentView
	{
		RunTest m_rt = new RunTest("NB");
		public gDocumentView() { }

		[Test]
		public void gaSortByRecV() { m_rt.fromFile("gaSortByRecV.xml"); }

		[Test]
		public void gbFilterByBrowseV() { m_rt.fromFile("gbFilterByBrowseV.xml"); }

		[Test]
		public void gcPrintDoc() { m_rt.fromFile("gcPrintDoc.xml"); }

	}
}
