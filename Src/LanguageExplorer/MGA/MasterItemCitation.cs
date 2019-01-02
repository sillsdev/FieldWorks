// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer.MGA
{
	internal sealed class MasterItemCitation
	{
		internal string WS { get; }

		internal string Citation { get; }

		internal MasterItemCitation(string ws, string citation)
		{
			WS = ws;
			Citation = citation;
		}

		internal void ResetDescription(RichTextBox rtbDescription)
		{
			rtbDescription.AppendText(string.Format(MGAStrings.ksBullettedItem, Citation, Environment.NewLine));
		}
	}
}