// Copyright (c) 2013-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.LCModel.Utils;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks
{
	/// <summary/>
	[TestFixture]
	public class WelcomeToFieldWorksDlgTests
	{
		/// <summary/>
		[Test]
		public void CanCreateWelcomeToFieldWorksDlg()
		{
			using (var dlg = new WelcomeToFieldWorksDlg((IHelpTopicProvider)DynamicLoader.CreateNonPublicObject(FwDirectoryFinder.LanguageExplorerDll,
						"LanguageExplorer.HelpTopics.FlexHelpTopicProvider"), null, false))
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
			using (var dlg = new WelcomeToFieldWorksDlg((IHelpTopicProvider)DynamicLoader.CreateNonPublicObject(FwDirectoryFinder.LanguageExplorerDll,
						"LanguageExplorer.HelpTopics.FlexHelpTopicProvider"), null, false))
			{
				var receiveButton = ReflectionHelper.GetField(dlg, "receiveButton") as Button;
				if (FLExBridgeHelper.IsFlexBridgeInstalled())
					Assert.That(receiveButton.Enabled, Is.True);
				else
					Assert.That(receiveButton.Enabled, Is.False);
			}
		}
	}
}
