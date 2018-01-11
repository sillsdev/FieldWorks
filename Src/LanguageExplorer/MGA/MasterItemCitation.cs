// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer.MGA
{
	internal sealed class MasterItemCitation
	{
		private readonly string m_ws;
		private readonly string m_citation;

		internal string WS => m_ws;

		internal string Citation => m_citation;

		internal MasterItemCitation(string ws, string citation)
		{
			m_ws = ws;
			m_citation = citation;
		}

		internal void ResetDescription(RichTextBox rtbDescription)
		{
			rtbDescription.AppendText(string.Format(MGAStrings.ksBullettedItem, m_citation, Environment.NewLine));
		}
	}
}