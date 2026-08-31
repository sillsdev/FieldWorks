// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Covers Upload to Webonary dialog behavior that is independent of a project and a
	/// controller.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class UploadToWebonaryDlgTests
	{
		[Test]
		public void SubmitIsDisabledUntilSiteNameUserNameAndPasswordAreAllPresent()
		{
			using (var dlg = new UploadToWebonaryDlg())
			{
				var submit = GetControl<Button>(dlg, "publishButton");
				var siteName = GetControl<TextBox>(dlg, "webonarySiteNameTextbox");
				var userName = GetControl<TextBox>(dlg, "webonaryUsernameTextbox");
				var password = GetControl<TextBox>(dlg, "webonaryPasswordTextbox");

				Assert.That(submit.Enabled, Is.False, "Submit should be disabled while every field is empty.");

				siteName.Text = "site";
				Assert.That(submit.Enabled, Is.False, "Submit should stay disabled without a user name and a password.");

				userName.Text = "user";
				Assert.That(submit.Enabled, Is.False, "Submit should stay disabled without a password.");

				password.Text = "password";
				Assert.That(submit.Enabled, Is.True, "Submit should be enabled once all three fields are filled in.");

				userName.Text = "   ";
				Assert.That(submit.Enabled, Is.True,
					"A credential is opaque, so whitespace in one still counts as filled in.");

				userName.Text = "user";
				siteName.Text = "   ";
				Assert.That(submit.Enabled, Is.False, "Whitespace should not satisfy the site name.");

				siteName.Text = string.Empty;
				Assert.That(submit.Enabled, Is.False, "Clearing the site name should disable Submit again.");
			}
		}

		private static T GetControl<T>(UploadToWebonaryDlg dlg, string fieldName) where T : Control
		{
			var field = typeof(UploadToWebonaryDlg).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, fieldName + " is missing from the dialog.");
			return (T)field.GetValue(dlg);
		}
	}
}
