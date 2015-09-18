// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: MarkS 2013-02-20 WelcomeToFieldWorksDlgTests.cs
using System.Windows.Forms;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks
{
#if RANDYTODO
	// TODO: Reference to LexTextDll.dll removed, so this is called before LexTextDll is built, so fails.
	// TODO: Restore it, or something like it, at some point.
	/// <summary/>
	[TestFixture]
	public class WelcomeToFieldWorksDlgTests : BaseTest
	{
		/// <summary/>
		[Test]
		public void Basic()
		{
			using (var dlg = new WelcomeToFieldWorksDlg((IHelpTopicProvider)DynamicLoader.CreateObject(FwDirectoryFinder.FlexDll,
						"SIL.FieldWorks.XWorks.LexText.FlexHelpTopicProvider"), null, false))
			{
				Assert.That(dlg, Is.Not.Null);
			}
		}

		/// <summary>
		/// Receive button should be enabled/disabled based on FlexBridge availability.
		/// </summary>
		[Test]
		public void ReceiveButtonIsDisabled()
		{
			using (var dlg = new WelcomeToFieldWorksDlg((IHelpTopicProvider)DynamicLoader.CreateObject(FwDirectoryFinder.FlexDll,
						"SIL.FieldWorks.XWorks.LexText.FlexHelpTopicProvider"), null, false))
			{
				var receiveButton = ReflectionHelper.GetField(dlg, "receiveButton") as Button;
				if (FLExBridgeHelper.IsFlexBridgeInstalled())
					Assert.That(receiveButton.Enabled, Is.True);
				else
					Assert.That(receiveButton.Enabled, Is.False);
			}
		}
	}
#endif
}
