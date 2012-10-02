// IPrintDialog.cs
// User: Jean-Marc Giffin at 11:55 A 22/07/2008

using System;

namespace SIL.FieldWorks.WorldPad
{
	public interface IPrintDialog
	{
		/// <summary>
		/// Show the IPrintDialog.
		/// </summary>
		/// <returns>
		/// A <see cref="PrintResponse"/> which indicates which button was pressed:
		/// "Print", "Cancel", or "Print Preview"
		/// </returns>
		PrintResponse Show();

		/// <summary>
		/// Close the PrintDialog.
		/// </summary>
		void Close();

		/// <summary>
		/// Prints the document.
		/// </summary>
		void Print();

		/// <summary>
		/// Show a preview of the document. This only needs to be implemented if the dialog
		/// supports this function.
		/// </summary>
		void ShowPreview();
	}

	/// <summary>
	/// The possible return types for a PrintDialog: Print, Cancel, and Preview.
	/// </summary>
	public enum PrintResponse
	{
		Print,
		Cancel,
		Preview
	}
}
