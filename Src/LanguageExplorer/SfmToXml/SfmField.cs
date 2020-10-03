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
	internal sealed class SfmField
	{
		private string m_sData;
		private MultiToWideError m_mwError;

		internal SfmField(string sMkr, byte[] rgbData, int lineNum)
		{
			Marker = sMkr;
			RawData = rgbData;
			var sData = SfmToXmlServices.MultiToWideWithERROR(rgbData, 0, rgbData.Length - 1, Encoding.UTF8, out m_mwError, out _);
			m_sData = sData.Trim();
			LineNumber = lineNum;
		}

		internal string Marker { get; }

		internal string Data
		{
			get => m_sData;
			set
			{
				m_sData = value;
				m_mwError = MultiToWideError.None;
			}
		}

		internal byte[] RawData { get; }

		internal bool ErrorConvertingData => m_mwError != MultiToWideError.None;

		internal int LineNumber { get; }
	}
}