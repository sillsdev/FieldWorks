// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwHelpAboutTests.cs
// Authorship History: MarkS
// ---------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Unit tests for FwCoreDlgs FwHelpAbout
	/// </summary>
	[TestFixture]
	public class FwHelpAboutTests: BaseTest
	{
		/// <summary></summary>
		[Test]
		public void Show()
		{
			using (var window = new FwHelpAbout())
			{
				window.Show();
				Application.DoEvents();
			}
		}
	}
}
