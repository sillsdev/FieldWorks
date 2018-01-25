// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// This struct stores the options data associated with a string destination.
	/// </summary>
	internal class StringOptions
	{
#if RANDYTODO
		// TODO: Are both really needed?
		// TODO: NB: The class comment suggests this is a struct, not a class.
#endif
		internal string m_wsId;
		internal CoreWritingSystemDefinition m_ws;
	}
}