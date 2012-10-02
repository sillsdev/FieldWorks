// PrintDialogFactory.cs
// User: Jean-Marc Giffin at 11:54 A 22/07/2008

using System;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// Factory that returns the appropriate PrintDialog, based on the operating system
	/// and the libraries used.
	/// </summary>
	public class PrintDialogFactory
	{
		/// <summary>
		/// Get a PrintDialog.
		/// </summary>
		/// <param name="type">
		/// A <see cref="PrintDialogType"/> telling us which kind of IPrintDialog to get.
		/// </param>
		/// <returns>
		/// A <see cref="IPrintDialog"/> The appropriate IPrintDialog.
		/// </returns>
		public static IPrintDialog GetPrintDialog(PrintDialogType type)
		{
			// MarkS: GnomePrintDialog works, but we don't want to depend on Gnome.
			//if (type == PrintDialogType.Gnome)
			//	return new GnomePrintDialog();
			return new GtkPrintUnixDialog();
		}

		/// <summary>
		/// The types of PrintDialogs available.
		/// </summary>
		public enum PrintDialogType
		{
			//Gnome,
			Gtk
		}
	}
}
