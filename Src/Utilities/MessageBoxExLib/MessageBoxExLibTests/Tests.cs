// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class MessageBoxTests
	{
		[OneTimeTearDown]
		public void FixtureTearDown()
		{
			MessageBoxExManager.DisposeAllMessageBoxes();
		}

		[Test]
		public void ShowReturnsSavedResponseWithoutShowingDialog()
		{
			string name = "SavedResponseTest";
			using (MessageBoxEx msgBox = MessageBoxExManager.CreateMessageBox(name))
			{
				msgBox.Caption = "Test Caption";
				msgBox.Text = "Test message";
				msgBox.AddButtons(MessageBoxButtons.YesNo);
				
				// Set a saved response directly via the manager
				var savedResponse = "No gracias";
				MessageBoxExManager.SavedResponses[name] = savedResponse;
				
				// Enable using saved responses
				msgBox.UseSavedResponse = true;
				
				// Show should return the saved response without showing the dialog
				string result = msgBox.Show();
				
				Assert.That(result, Is.EqualTo(savedResponse), "Show should return the saved response");
			}
			
			// Clean up the saved response
			MessageBoxExManager.ResetSavedResponse("SavedResponseTest");
		}

		public void DoNothing()
		{
		}
	}
}
