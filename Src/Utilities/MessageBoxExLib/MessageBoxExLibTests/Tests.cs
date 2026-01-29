// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Extensions.Forms;
using NUnit.Framework;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	/// Tests for MessageBoxEx using NUnitForms framework.
	/// Inherits from NUnitFormTest to access protected ExpectModal method.
	/// </summary>
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "TODO-Linux: depends on nunitforms which is not cross platform")]
	public class MessageBoxTests : NUnitFormTest
	{
		[OneTimeTearDown]
		public void FixtureTearDown()
		{
			MessageBoxExManager.DisposeAllMessageBoxes();
		}

		[Test]
		public void TimeoutOfNewBox()
		{
			string name=System.IO.Path.GetTempPath()/*just a hack to get a unique name*/;
			using (MessageBoxEx msgBox = MessageBoxExManager.CreateMessageBox(name))
			{
				msgBox.Caption = "Question";
				msgBox.Text = "Blah blah blah?";

				msgBox.AddButtons(MessageBoxButtons.YesNo);

				msgBox.Timeout = 10;
				msgBox.TimeoutResult = TimeoutResult.Timeout;

				ExpectModal(name, DoNothing, true);//the nunitforms framework freaks out if we show a dialog with out warning it first
				Assert.That(msgBox.Show(), Is.EqualTo("Timeout"));
			}
		}

		[Test]
		public void RememberOkBox()
		{
			string name = "X";
			using (MessageBoxEx msgBox = MessageBoxExManager.CreateMessageBox(name))
			{
				msgBox.Caption = name;
				msgBox.Text = "Blah blah blah?";

				msgBox.AddButtons(MessageBoxButtons.YesNo);

				msgBox.SaveResponseText = "Don't ask me again";
				msgBox.UseSavedResponse = false;
				msgBox.AllowSaveResponse  = true;

				//click the yes button when the dialog comes up
				ExpectModal(name, ConfirmModalByYesAndRemember, true);

				Assert.That(msgBox.Show(), Is.EqualTo("Yes"));

				ExpectModal(name, DoNothing, false /*don't expect it, because it should use our saved response*/);
				msgBox.UseSavedResponse = true;
				Assert.That(msgBox.Show(), Is.EqualTo("Yes"));
			}
		}

		public void DoNothing()
		{
		}

		public void ConfirmModalByYes()
		{
			var t = new ButtonTester("Yes");
			t.Click();
		}
		public void ConfirmModalByYesAndRemember()
		{
			new CheckBoxTester("chbSaveResponse").Check(true);
			new ButtonTester("Yes").Click();
		}
	}
}
