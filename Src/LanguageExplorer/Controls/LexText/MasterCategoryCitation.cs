// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.LexText
{
	internal class MasterCategoryCitation
	{
		private string m_ws;
		private string m_citation;

		public string WS
		{
			get { return m_ws; }
		}

		public string Citation
		{
			get { return m_citation; }
		}

		public MasterCategoryCitation(string ws, string citation)
		{
			m_ws = ws;
			m_citation = citation;
		}

		public void ResetDescription(RichTextBox rtbDescription)
		{
			rtbDescription.AppendText(String.Format(LexTextControls.ksBullettedItem,
				m_citation, System.Environment.NewLine));
		}
	}
}