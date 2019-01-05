// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageExplorer.DictionaryConfiguration;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	/// <summary>
	/// Test stub used to replace the DictionaryConfigMgrDlg in testing.
	/// </summary>
	internal sealed class DictionaryConfigViewerStub : IDictConfigViewer
	{
		private List<Tuple<string, string>> m_listItems;

		public DictionaryConfigViewerStub()
		{
			TestPresenter = new DictionaryConfigTestPresenter(this);
			m_listItems = new List<Tuple<string, string>>();
		}

		internal DictionaryConfigTestPresenter TestPresenter { get; }

		public string SelectedItem { get; private set; }

		#region Implementation of IDictConfigViewer

		public IDictConfigPresenter Presenter => TestPresenter;

		/// <summary>
		/// Tuples of strings are (uniqueCode, dispName) pairs to be displayed.
		/// </summary>
		/// <param name="listItems"></param>
		/// <param name="selectedItem">uniqueCode of item that should be selected.</param>
		public void SetListViewItems(IEnumerable<Tuple<string, string>> listItems, string selectedItem)
		{
			if (m_listItems == null)
			{
				m_listItems = new List<Tuple<string, string>>();
			}
			m_listItems.Clear();
			foreach (var listItem in listItems)
			{
				m_listItems.Add(listItem);
			}
			Debug.Assert(m_listItems.FirstOrDefault(tpl => tpl.Item1 == selectedItem) != null, "Selected item does not exist in list.");
			SelectedItem = selectedItem;
		}

		/// <summary>
		/// The unique code for the item currently selected in the dialog listView.
		/// </summary>
		public string CurrentSelectedCode => SelectedItem;

		#endregion
	}
}