// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Unit tests for FwCoreDlgs FwHelpAbout
	/// </summary>
	[TestFixture]
	public class FwHelpAboutTests
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
