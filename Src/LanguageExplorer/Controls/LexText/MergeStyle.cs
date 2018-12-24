// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.LexText
{
	internal enum MergeStyle
	{
		/// <summary>When there's a conflict, keep the existing data.</summary>
		MsKeepOld = 1,
		/// <summary>When there's a conflict, keep the data in the LIFT file.</summary>
		MsKeepNew = 2,
		/// <summary>When there's a conflict, keep both the existing data and the data in the LIFT file.</summary>
		MsKeepBoth = 3,
		/// <summary>Throw away any existing entries/senses/... that are not in the LIFT file.</summary>
		MsKeepOnlyNew = 4
	}
}