// GnomePrintDialog.cs
// User: Jean-Marc Giffin at 11:55 A 22/07/2008

using System;
using Gnome;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// Implementation of IPrintDialog using the Gnome package.
	/// </summary>
	public class GnomePrintDialog : IPrintDialog
	{
		private PrintDialog pd_;
		[Glade.Widget] private Gtk.Window window2;

		/// <summary>
		/// Construct a new GnomePrintDialog.
		/// </summary>
		public GnomePrintDialog()
		{
			pd_ = new PrintDialog(new PrintJob(), "Print");
			Glade.XML gxml = new Glade.XML("/home/giffinj/tmp/Local/glade/window2.glade", "window2", "Local");
			gxml.Autoconnect(this);
		}

		/// <summary>
		/// Show the GnomePrintDialog.
		/// </summary>
		/// <returns>
		/// A <see cref="PrintResponse"/> which indicates which button was pressed:
		/// "Print", "Cancel", or "Print Preview"
		/// </returns>
		public PrintResponse Show()
		{
			int response = pd_.Run();
			if (response == (int)Gnome.PrintButtons.Print)
				return PrintResponse.Print;
			else if (response == (int)Gnome.PrintButtons.Preview)
				return PrintResponse.Preview;
			else
				return PrintResponse.Cancel;
		}

		/// <summary>
		/// Prints the document.
		/// TODO: Implement
		/// </summary>
		public void Print()
		{
			Console.WriteLine("Now Printing...");
			Close();
		}

		/// <summary>
		/// Close the dialog.
		/// </summary>
		public void Close()
		{
			pd_.Hide();
		}

		/// <summary>
		/// Show a preview of the page to be printed. This must close the dialog, because the
		/// indication that we want a preview is a ResponseType, meaning that this dialog is
		/// no longer good after any selection has been pressed.
		/// </summary>
		public void ShowPreview()
		{
			window2.Show();
			Close();
		}
	}
}
