// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Stub for tests that access the clipboard
	/// </summary>
	public class ClipboardStub : IClipboard
	{
		private IDataObject m_DataObject = new DataObject();

		#region IClipboard Members

		/// <inheritdoc />
		public bool ContainsText()
		{
			return m_DataObject.GetDataPresent(DataFormats.UnicodeText);
		}

		/// <inheritdoc />
		public IDataObject GetDataObject()
		{
			return m_DataObject;
		}

		/// <inheritdoc />
		public string GetText()
		{
			return (string)m_DataObject.GetData(DataFormats.UnicodeText);
		}

		/// <inheritdoc />
		public void SetDataObject(object data, bool copy)
		{
			m_DataObject = data is IDataObject ? (IDataObject)data : new DataObject(data);
		}

		/// <inheritdoc />
		public void SetDataObject(object data, bool copy, int retries, int msDelay)
		{
			m_DataObject = data is IDataObject ? (IDataObject)data : new DataObject(data);
		}

		/// <inheritdoc />
		public void SetDataObject(object data)
		{
			SetDataObject(data, false);
		}

		/// <inheritdoc />
		public void SetText(string text, TextDataFormat format)
		{
			((DataObject)m_DataObject).SetText(text, format);
		}

		/// <inheritdoc />
		public void SetText(string text)
		{
			SetText(text, TextDataFormat.UnicodeText);
		}

		#endregion
	}
}