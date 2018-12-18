// Copyright (c) 2010-2019 SIL International
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
		/// <summary />
		public AddCustomListDlg(IPropertyTable propertyTable, IPublisher publisher, LcmCache cache)
			: base(propertyTable, publisher, cache)
		{
			s_helpTopic = "khtpNewCustomList";
			NewList = null;
		}

		/// <summary>
		/// Get the new list, if it was created, or null.
		/// </summary>
		public ICmPossibilityList NewList { get; private set; }

		protected override void InitializeDialogFields()
		{
			base.InitializeDialogFields();
			// OK button is enabled, but DoOKAction needs to test whether
			// the List Name contains anything or not.
			EnableOKButton(true);
		}

		protected override void DoOKAction()
		{
			if (IsListNameEmpty)
			{
				MessageBox.Show(ListResources.ksProvideValidListName, ListResources.ksNoListName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				NewList = null;
				return;
			}
			if (m_publisher == null || Cache == null)
			{
				throw new ArgumentException("Don't call this without a publisher and a cache.");
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
				NewList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(listName.Text, ws);
				SetAllMultiAlternatives(NewList.Name, m_lmscListName);
				// Set various properties of CmPossibilityList
				NewList.DisplayOption = (int)DisplayBy;
				NewList.PreventDuplicates = !AllowDuplicate;
				NewList.IsSorted = SortByName;
				var wss = SelectedWs;
				NewList.WsSelector = wss;
				NewList.IsVernacular = wss == WritingSystemServices.kwsVerns || wss == WritingSystemServices.kwsVernAnals;
				NewList.Depth = SupportsHierarchy ? 127 : 1;
				SetAllMultiAlternatives(NewList.Description, m_lmscDescription);
			});
		}
	}
}