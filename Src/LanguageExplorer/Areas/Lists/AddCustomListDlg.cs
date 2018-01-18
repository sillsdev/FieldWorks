// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lists
{
	public class AddCustomListDlg : CustomListDlg
	{
		private ICmPossibilityList _newList;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:AddCustomListDlg"/> class.
		/// </summary>
		public AddCustomListDlg(IPropertyTable propertyTable, IPublisher publisher, LcmCache cache)
			: base(propertyTable, publisher, cache)
		{
			s_helpTopic = "khtpNewCustomList";
			_newList = null;
		}

		/// <summary>
		/// Get the new list, if it was created, or null.
		/// </summary>
		public ICmPossibilityList NewList => _newList;

		protected override void DoOKAction()
		{
			if (IsListNameEmpty)
			{
				MessageBox.Show(ListResources.ksProvideValidListName, ListResources.ksNoListName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				_newList = null;
				return;
			}
			CreateList();
		}

		protected override void InitializeDialogFields()
		{
			base.InitializeDialogFields();
			// OK button is enabled, but DoOKAction needs to test whether
			// the List Name contains anything or not.
			EnableOKButton(true);
		}

		/// <summary>
		/// Creates the new Custom list.
		/// </summary>
		private void CreateList()
		{
			if (m_publisher == null || Cache == null)
			{
				throw new ArgumentException("Don't call this without a publisher and a cache.");
			}
			if (IsListNameEmpty)
			{
				// shouldn't ever get here because OK btn isn't enabled until name has a non-empty value
				throw new ArgumentException("Please provide a valid list name.");
			}

			// This checks that we aren't creating a list with the same name as another list
			// but it doesn't always look like it because the name in the list and on FLEx (in Lists area)
			// aren't necessarily the same (e.g. Text Chart Markers is actually Chart Markers in the file).
			// This will likely get taken care of by a data migration to change internal list names.
			if (IsListNameDuplicated)
			{
				MessageBox.Show(ListResources.ksChooseAnotherListName, ListResources.ksDuplicateName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			UndoableUnitOfWorkHelper.Do(ListResources.ksUndoCreateList, ListResources.ksRedoCreateList, Cache.ActionHandlerAccessor, () =>
			{
				var ws = Cache.DefaultUserWs; // get default ws
				var listName = m_lmscListName.Value(ws);
				_newList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(listName.Text, ws);
				SetAllMultiAlternatives(_newList.Name, m_lmscListName);

				// Set various properties of CmPossibilityList
				_newList.DisplayOption = (int)DisplayBy;
				_newList.PreventDuplicates = !AllowDuplicate;
				_newList.IsSorted = SortByName;
				var wss = SelectedWs;
				_newList.WsSelector = wss;
				_newList.IsVernacular = wss == WritingSystemServices.kwsVerns || wss == WritingSystemServices.kwsVernAnals;
				_newList.Depth = SupportsHierarchy ? 127 : 1;
				SetAllMultiAlternatives(_newList.Description, m_lmscDescription);
			});
		}
	}
}