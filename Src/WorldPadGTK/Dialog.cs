// Dialog.cs
// User: Jean-Marc Giffin at 4:38 P 07/05/2008

using System;

namespace SIL.FieldWorks.WorldPad
{
	public class Dialog
	{
		private IWorldPadDocController docController;
		private IWorldPadDocModel docModel;
		public const string DIALOGS = "glade/dialogs.glade";
		public const string CDIALOGS = "glade/converted-dialogs.glade";

		public Dialog(IWorldPadDocController docController, IWorldPadDocModel docModel)
		{
			Console.WriteLine(this.GetType() + " invoked");

			this.docModel = docModel;
			this.docController = docController;

			Glade.XML gxml = new Glade.XML(which(), "kridAfStyleDlg", null);
			gxml.Autoconnect(this);
		}

		protected string which() {
			return DIALOGS;
		}

		// **************************
		// Handlers for button events
		// **************************

		// This event follows a response event when the OK button is pressed

		private void on_kctidOk_clicked(object obj, EventArgs args)
		{
			Console.WriteLine(this.GetType() + ".on_kctidOk_clicked invoked");
			docController.HideAfStyleDlg();
		}

		// This event follows a response event when the Cancel button is pressed

		private void on_kctidCancel_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("AfStyleDlgController.on_kctidCancel_clicked invoked");

			//kridAfStyleDlg.Hide();
			docController.HideAfStyleDlg();
		}

		// This event follows a response event when the Apply button is pressed

		private void on_kctidClose_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("AfStyleDlgController.on_kctidClose_clicked invoked");
		}

		// This event follows a response event when the Help button is pressed

		private void on_kctidHelp_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("AfStyleDlgController.on_kctidHelp_clicked invoked");
		}

		public void Show(IWorldPadDocView parent)
		{
			Console.WriteLine("AfStyleDlgController.Show invoked");

			// Note: Needed for GTK+ to honour WindowPosition.CenterOnParent.
//			kridAfStyleDlg.TransientFor = parent.Window;

//			kridAfStyleDlg.Show();
		}

		public void Hide()
		{
			Console.WriteLine("AfStyleDlgController.Hide invoked");

//			kridAfStyleDlg.Hide();
		}
	}

}
