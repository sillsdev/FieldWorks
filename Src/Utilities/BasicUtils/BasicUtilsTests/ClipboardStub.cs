// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ClipboardStub.cs
// Responsibility: EberhardB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Windows.Forms;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stub for tests that access the clipboard
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ClipboardStub: IClipboard
	{
		private IDataObject m_DataObject = new DataObject();

		#region IClipboard Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether there is data on the Clipboard in the UnicodeText format.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool ContainsText()
		{
			return m_DataObject.GetDataPresent(DataFormats.UnicodeText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the data that is currently on the system Clipboard.
		/// </summary>
		/// <returns>
		/// An IDataObject that represents the data currently on the Clipboard, or
		/// <c>null</c> if there is no data on the Clipboard.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IDataObject GetDataObject()
		{
			return m_DataObject;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves text data from the Clipboard in the UnicodeText format.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetText()
		{
			return (string)m_DataObject.GetData(DataFormats.UnicodeText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Places data on the system Clipboard and specifies whether the data should remain on
		/// the Clipboard after the application exits.
		/// </summary>
		/// <param name="data">The data to place on the Clipboard.</param>
		/// <param name="copy"><c>true</c> if you want data to remain on the Clipboard after
		/// this application exits; otherwise, <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public void SetDataObject(object data, bool copy)
		{
			if (data is IDataObject)
				m_DataObject = (IDataObject) data;
			else
				m_DataObject = new DataObject(data);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Places data on the system Clipboard and specifies whether the data should remain on
		/// the Clipboard after the application exits.
		/// </summary>
		/// <param name="data">The data to place on the Clipboard.</param>
		/// <param name="copy"><c>true</c> if you want data to remain on the Clipboard after
		/// this application exits; otherwise, <c>false</c>.</param>
		/// <param name="retries"># of times to retry</param>
		/// <param name="msDelay"># of milliseconds to delay between retries</param>
		/// ------------------------------------------------------------------------------------
		public void SetDataObject(object data, bool copy, int retries, int msDelay)
		{
			if (data is IDataObject)
				m_DataObject = (IDataObject) data;
			else
				m_DataObject = new DataObject(data);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Places nonpersistent data on the system Clipboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetDataObject(object data)
		{
			SetDataObject(data, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds text data to the Clipboard in the format indicated by the specified
		/// TextDataFormat value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetText(string text, TextDataFormat format)
		{
			((DataObject)m_DataObject).SetText(text, format);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds text data to the Clipboard in the UnicodeText format.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetText(string text)
		{
			SetText(text, TextDataFormat.UnicodeText);
		}

		#endregion
	}
}
