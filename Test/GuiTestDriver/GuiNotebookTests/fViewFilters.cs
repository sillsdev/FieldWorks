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
	public class fViewFilters
	{
		RunTest m_rt = new RunTest("NB");
		public fViewFilters() { }

		[Test]
		public void fNbbrowseview() { m_rt.fromFile("fNbbrowseview.xml"); }

		[Test]
		public void fNbdeview() { m_rt.fromFile("fNbdeview.xml"); }

		[Test]
		public void fNbdocview() { m_rt.fromFile("fNbdocview.xml"); }

		[Test]
		public void fNbfiltuse() { m_rt.fromFile("fNbfiltuse.xml"); }

		[Test]
		public void fNbvwfilters() { m_rt.fromFile("fNbvwfilters.xml"); }
	}
}
