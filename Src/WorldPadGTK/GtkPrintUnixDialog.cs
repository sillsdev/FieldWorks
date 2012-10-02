// GtkPrintUnixDialog.cs
// User: Jean-Marc Giffin at 3:10 P 22/07/2008

using System;
using Gtk;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// Implementation of IPrintDialog using the Gtk package.
	/// </summary>
	public class GtkPrintUnixDialog : IPrintDialog
	{
		private PrintUnixDialog pud_;

		/// <summary>
		/// Construct a new GnomePrintDialog.
		/// </summary>
		public GtkPrintUnixDialog()
		{
			pud_ = new PrintUnixDialog("Print", null);
		}

		/// <summary>
		/// The GTK Print Dialog does not support preview-showing, so this can be left blank.
		/// </summary>
		public void ShowPreview() { }

		/// <summary>
		/// Prints the document.
		/// TODO: Implement
		/// </summary>
		public void Print()
		{
			Console.WriteLine("Time to print!");
		}

		/// <summary>
		/// Close the dialog.
		/// </summary>
		public void Close()
		{
			pud_.Hide();
		}

		/// <summary>
		/// Show the GtkPrintDialog.
		/// </summary>
		/// <returns>
		/// A <see cref="PrintResponse"/> which indicates which button was pressed:
		/// "Print" or "Cancel"
		/// </returns>
		public PrintResponse Show()
		{
			ResponseType response = (ResponseType)pud_.Run();
			if (response == ResponseType.Ok)
				return PrintResponse.Print;
			return PrintResponse.Cancel;
		}
	}
}
