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
