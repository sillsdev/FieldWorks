// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface for clipboard methods. This helps in providing a system independent, reliable
	/// implementation for unit tests.
	/// </summary>
	public interface IClipboard
	{
		/// <summary>
		/// Indicates whether there is data on the Clipboard in the Text or UnicodeText format,
		/// depending on the operating system.
		/// </summary>
		bool ContainsText();

		/// <summary>
		/// Retrieves text data from the Clipboard in the Text or UnicodeText format, depending
		/// on the operating system.
		/// </summary>
		string GetText();

		/// <summary>
		/// Retrieves the data that is currently on the system Clipboard.
		/// </summary>
		/// <returns>An IDataObject that represents the data currently on the Clipboard, or
		/// <c>null</c> if there is no data on the Clipboard.</returns>
		IDataObject GetDataObject();

		/// <summary>
		/// Adds text data to the Clipboard in the Text or UnicodeText format, depending on the
		/// operating system.
		/// </summary>
		void SetText(string text);

		/// <summary>
		/// Adds text data to the Clipboard in the format indicated by the specified
		/// TextDataFormat value.
		/// </summary>
		void SetText(string text, TextDataFormat format);

		/// <summary>
		/// Places nonpersistent data on the system Clipboard.
		/// </summary>
		void SetDataObject(object data);

		/// <summary>
		/// Places data on the system Clipboard and specifies whether the data should remain on
		/// the Clipboard after the application exits.
		/// </summary>
		/// <param name="data">The data to place on the Clipboard.</param>
		/// <param name="copy"><c>true</c> if you want data to remain on the Clipboard after
		/// this application exits; otherwise, <c>false</c>.</param>
		void SetDataObject(object data, bool copy);

		/// <summary>
		/// Places data on the system Clipboard and specifies whether the data should remain on
		/// the Clipboard after the application exits.
		/// </summary>
		/// <param name="data">The data to place on the Clipboard.</param>
		/// <param name="copy"><c>true</c> if you want data to remain on the Clipboard after
		/// this application exits; otherwise, <c>false</c>.</param>
		/// <param name="retries"># of times to retry</param>
		/// <param name="msDelay"># of milliseconds to delay between retries</param>
		void SetDataObject(object data, bool copy, int retries, int msDelay);
	}
}