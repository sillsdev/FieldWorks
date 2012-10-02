using System;
using NUnit.Framework;
using NUnit.Extensions.Forms;

using System.Windows.Forms;
using System.Drawing;
namespace Utils.MessageBoxExLib
{
	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class MessageBoxTests : NUnitFormTest
	{
		public MessageBoxTests()
		{
		}
		public override bool UseHidden
		{
			get
			{return true;}
		}

		//http://www.dotnetguru2.org/tbarrere/?p=196&more=1&c=1&tb=1&pb=1

		[Test]
		public void TimeoutOfNewBox()
		{
			string name=System.IO.Path.GetTempPath()/*just a hack to get a unique name*/;
			MessageBoxEx msgBox = MessageBoxExManager.CreateMessageBox(name);
			msgBox.Caption = "Question";
			msgBox.Text = "Blah blah blah?";

			msgBox.AddButtons(MessageBoxButtons.YesNo);

			msgBox.Timeout = 10;
			msgBox.TimeoutResult = TimeoutResult.Timeout;

			ExpectModal(name, "DoNothing",true);//the nunitforms framework freaks out if we show a dialog with out warning it first
			Assert.AreEqual("Timeout",msgBox.Show());
		}
		[Test]
		public void RememberOkBox()
		{
			string name="X";
			MessageBoxEx msgBox = MessageBoxExManager.CreateMessageBox(name);
			msgBox.Caption = name;
			msgBox.Text = "Blah blah blah?";

			msgBox.AddButtons(MessageBoxButtons.YesNo);

			msgBox.SaveResponseText = "Don't ask me again";
			msgBox.UseSavedResponse = false;
			msgBox.AllowSaveResponse  = true;

			//click the yes button when the dialog comes up
			ExpectModal(name, "ConfirmModalByYesAndRemember",true);

			Assert.AreEqual("Yes", 	msgBox.Show());

			ExpectModal(name, "DoNothing",false /*don't expect it, because it should use our saved response*/);
			msgBox.UseSavedResponse = true;
			Assert.AreEqual("Yes", 	msgBox.Show());

		}

		public void DoNothing()
		{
		}

		public void ConfirmModalByYes()
		{
			ButtonTester t = new ButtonTester("Yes");
			t.Click();
		}
		public void ConfirmModalByYesAndRemember()
		{
			new CheckBoxTester("chbSaveResponse").Check(true);
			new ButtonTester("Yes").Click();
		}
	}
}
