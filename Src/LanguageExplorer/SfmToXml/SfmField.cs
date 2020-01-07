// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This class represents one field (which may be one line or multiple lines) from a
	/// standard format file.
	/// </summary>
	public class SfmField
	{
		private string m_sData;
		private MultiToWideError m_mwError;
		private byte[] m_badBytes;

		public SfmField(string sMkr, byte[] rgbData, int lineNum)
		{
			Marker = sMkr;
			RawData = rgbData;
			var sData = Converter.MultiToWideWithERROR(rgbData, 0, rgbData.Length - 1, Encoding.UTF8, out m_mwError, out m_badBytes);
			m_sData = sData.Trim();
			LineNumber = lineNum;
		}

		public string Marker { get; }

		public string Data
		{
			get { return m_sData; }
			set
			{
				m_sData = value;
				m_mwError = MultiToWideError.None;
			}
		}

		public byte[] RawData { get; }

		public bool ErrorConvertingData => m_mwError != MultiToWideError.None;

		public int LineNumber { get; }
	}
}