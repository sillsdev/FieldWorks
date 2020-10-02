// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.UtilityTools
{
	/// <summary>
	/// Interface for utilities.
	/// </summary>
	public interface IUtility
	{
		/// <summary>
		/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
		/// </summary>
		string WhatDescription { get; }

		/// <summary>
		/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
		/// </summary>
		string WhenDescription { get; }

		/// <summary>
		/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
		/// </summary>
		string RedoDescription { get; }

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		void Process();
	}
}
