// WhoopeeDlgController.cs
// User: Jean-Marc Giffin at 11:17 A 08/05/2008

using System;
using System.IO;
using Gtk;
using Glade;

namespace SIL.FieldWorks.WorldPad
{
	public class WhoopeeDlgController : DialogController
	{
		public WhoopeeDlgController(IDialogModel model) : base("WhoopeeDlg", model)
		{

		}

		protected override void Commit() { }

		// This event follows a response event when the Default button is pressed

		private void on_default_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("DEFAULT!!!");
		}

		// This event follows a response event when the Help button is pressed

		private void on_help_clicked(object obj, EventArgs args)
		{
			Console.WriteLine("HELP!!!");
		}
	}
}
