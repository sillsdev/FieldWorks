using System.Windows.Forms;
using NUnit.Extensions.Forms;
using NUnit.Framework;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	///
	/// </summary>
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "TODO-Linux: depends on nunitforms which is not cross platform")]
	public class MessageBoxTests
	{
		private NUnitFormTest m_FormTest;

		[SetUp]
		public void Setup()
		{
			m_FormTest = new NUnitFormTest();
			m_FormTest.SetUp();
		}

		[TearDown]
		public void Teardown()
		{
			m_FormTest.TearDown();
		}

		[TestFixtureTearDown]
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

				m_FormTest.ExpectModal(name, DoNothing, true);//the nunitforms framework freaks out if we show a dialog with out warning it first
				Assert.AreEqual("Timeout",msgBox.Show());
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
				m_FormTest.ExpectModal(name, ConfirmModalByYesAndRemember, true);

				Assert.AreEqual("Yes", msgBox.Show());

				m_FormTest.ExpectModal(name, DoNothing, false /*don't expect it, because it should use our saved response*/);
				msgBox.UseSavedResponse = true;
				Assert.AreEqual("Yes", msgBox.Show());
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
