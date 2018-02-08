// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// This class stores the options data associated with a topics list destination.
	/// </summary>
	internal class TopicsListOptions
	{
		internal string m_wsId;
		internal CoreWritingSystemDefinition m_ws;
		internal bool m_fHaveMulti;
		internal string m_sDelimMulti;
		internal bool m_fHaveSub;
		internal string m_sDelimSub;
		internal bool m_fHaveBetween;
		internal string m_sMarkStart;
		internal string m_sMarkEnd;
		internal bool m_fHaveBefore;
		internal string m_sBefore;
		internal bool m_fIgnoreNewStuff;
		internal List<string> m_rgsMatch = new List<string>();
		internal List<string> m_rgsReplace = new List<string>();
		internal string m_sEmptyDefault;
		internal PossNameType m_pnt;
		// value looked up for m_sEmptyDefault.
		internal ICmPossibility m_default;
		// Parsed versions of the strings above, split into possibly multiple delimiters.
		internal string[] m_rgsDelimMulti;
		internal string[] m_rgsDelimSub;
		internal string[] m_rgsMarkStart;
		internal string[] m_rgsMarkEnd;
		internal string[] m_rgsBefore;
	}
}