// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// An interface that a Form can implement in order to configure the Find and Replace dialog.
	/// </summary>
	public interface IFindAndReplaceContext
	{
		/// <summary>
		/// The ID to pass to the help topic provider to get help for the Find tab.
		/// </summary>
		string FindTabHelpId { get; }
	}
}