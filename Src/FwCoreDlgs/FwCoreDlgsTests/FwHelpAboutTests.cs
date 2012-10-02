// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010 SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
