// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for utilities.
	/// </summary>
	internal interface IUtility
	{
		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		string Label { get; }

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		void OnSelection();

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		void Process();
	}
}
