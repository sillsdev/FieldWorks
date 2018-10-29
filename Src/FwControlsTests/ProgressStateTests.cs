// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary />
	[TestFixture]
	public class ProgressStateTests
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