// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Interface which the DictionaryConfigMgrDlg or (for test) the DictionaryConfigViewerStub
	/// exposes to the DictionaryConfigManager.
	/// </summary>
	internal interface IDictConfigViewer
	{
		IDictConfigPresenter Presenter { get; }

		/// <summary>
		/// Tuples of strings are (uniqueCode, dispName) pairs to be displayed.
		/// </summary>
		/// <param name="listItems"></param>
		/// <param name="selectedItem">The code for the item that should be selected
		/// in the dialog ListView.</param>
		void SetListViewItems(IEnumerable<Tuple<string, string>> listItems, string selectedItem);

		/// <summary>
		/// The unique code for the item currently selected in the dialog listView.
		/// </summary>
		string CurrentSelectedCode { get; }
	}
}
