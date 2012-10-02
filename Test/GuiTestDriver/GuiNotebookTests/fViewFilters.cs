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
