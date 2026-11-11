// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks.LexEd;
using XCore;

namespace LexEdDllTests
{
	[TestFixture]
	public class MotifListenerTests
	{
		[Test]
		public void CommandFollowsMotifBeingInstalledAndRemovedWhileFieldWorksIsOpen()
		{
			var installed = false;
			var resolver = new MotifExecutableResolver(() => null, () => @"C:\Motif\motif.exe",
				path => installed && path == @"C:\Motif\motif.exe");
			var runner = new MotifApplyRunner(resolver, info =>
			{
				Assert.Fail("Showing the command must not start Motif.");
				return null;
			});

			using (var listener = new MotifListener(runner))
			{
				foreach (var isInstalled in new[] { false, true, false })
				{
					installed = isInstalled;
					var display = new UIItemDisplayProperties(null, "Apply Motif changes", false, null, false);

					Assert.That(listener.OnDisplayApplyMotifChanges(null, ref display), Is.True);
					Assert.That(display.Visible, Is.True);
					Assert.That(display.Enabled, Is.EqualTo(isInstalled));
				}
			}
		}
	}
}
