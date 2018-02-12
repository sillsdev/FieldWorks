// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.LexText
{
	internal class MasterCategoryCitation
	{
		public string WS { get; }

		public string Citation { get; }

		public MasterCategoryCitation(string ws, string citation)
		{
			WS = ws;
			Citation = citation;
		}

		public void ResetDescription(RichTextBox rtbDescription)
		{
			rtbDescription.AppendText(string.Format(LexTextControls.ksBullettedItem, Citation, Environment.NewLine));
		}
	}
}