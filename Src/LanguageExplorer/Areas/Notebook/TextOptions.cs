// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary>
	/// This class stores the options data associated with a structured text destination.
	/// </summary>
	internal class TextOptions
	{
		internal string m_sStyle;
		internal bool m_fStartParaNewLine;
		internal bool m_fStartParaBlankLine;
		internal bool m_fStartParaIndented;
		internal bool m_fStartParaShortLine;
		internal int m_cchShortLim;
		internal string m_wsId;
		internal CoreWritingSystemDefinition m_ws;
	}
}