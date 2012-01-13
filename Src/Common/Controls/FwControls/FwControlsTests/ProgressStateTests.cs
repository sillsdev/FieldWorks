// Copyright (c) 2011, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2011-02-18 ProgressStateTests.cs

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary/>
	[TestFixture]
	public class ProgressStateTests : BaseTest
	{
		/// <summary/>
		[Test]
		public void ProgressState_changesAndResetsCursor()
		{
			Assert.That(Application.UseWaitCursor == false, "Not using wait cursor");
			Cursor.Current = Cursors.Default;
			using (var statusBar = new StatusBar())
			{
				using (var panel = new StatusBarProgressPanel(statusBar))
				{
					using (new ProgressState(panel))
					{
						Assert.That(Application.UseWaitCursor, "Changed to using wait cursor");
					}
					Assert.That(Application.UseWaitCursor == false, "Back to not using wait cursor");
				}
			}
		}
	}
}
