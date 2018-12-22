// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary>
	/// This class stores the options data associated with a string destination.
	/// </summary>
	internal sealed class StringOptions
	{
		internal string m_wsId;
		internal CoreWritingSystemDefinition m_ws;
	}
}