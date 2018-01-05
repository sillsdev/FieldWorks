// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Interface which the DictionaryConfigManager exposes to its Caller (XmlDocConfigureDlg).
	/// </summary>
	internal interface IDictConfigManager
	{
		/// <summary>
		/// If there has been no change in change in current view, this will be null.
		/// If a new configuration should now be the current one (a copy was made or
		/// the current one was deleted), then this will return the unique code of the
		/// new current view.
		/// </summary>
		string FinalConfigurationView { get; }

		/// <summary>
		/// If copies of older configuration views have been made, this property will
		/// provide a list of the new views to create.
		/// Items(Tuples) in the list are of the format:
		///		(newUniqueCode, codeOfViewCopiedFrom, newDisplayName)
		/// </summary>
		IEnumerable<Tuple<string, string, string>> NewConfigurationViews { get; }

		/// <summary>
		/// If existing configuration views have been deleted, this property will
		/// provide a list of the unique codes to delete.
		/// N.B.: Make sure Caller processes copying views first, in case some of
		/// the copies are based on views that are to be deleted!
		/// </summary>
		IEnumerable<string> ConfigurationViewsToDelete { get; }

		/// <summary>
		/// If older configuration views have been renamed, this property will
		/// provide a list of the codes with their new display names.
		/// Items(Tuples) in the list are of the format:
		///		(uniqueCode, newDisplayName)
		/// </summary>
		IEnumerable<Tuple<string, string>> RenamedExistingViews { get; }
	}
}