// Copyright (c) 2013, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2013-02-20 WelcomeToFieldWorksDlgTests.cs

using System.Windows.Forms;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.XWorks.LexText;
using SIL.Utils;
using NUnit.Framework;

namespace SIL.FieldWorks
{
	/// <summary/>
	[TestFixture]
	public class WelcomeToFieldWorksDlgTests : BaseTest
	{
		/// <summary/>
		[Test]
		public void Basic()
		{
			using (var dlg = new WelcomeToFieldWorksDlg(new FlexHelpTopicProvider(), null, null, false))
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
			using (var dlg = new WelcomeToFieldWorksDlg(new FlexHelpTopicProvider(), null, null, false))
			{
				var receiveButton = ReflectionHelper.GetField(dlg, "receiveButton") as Button;
				if (SIL.FieldWorks.Common.FwUtils.FLExBridgeHelper.IsFlexBridgeInstalled())
					Assert.That(receiveButton.Enabled, Is.True);
				else
					Assert.That(receiveButton.Enabled, Is.False);
			}
		}
	}
}
