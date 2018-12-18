// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lists
{
	public class ConfigureListDlg : CustomListDlg
	{
		#region MemberData

		/// <summary>
		/// We are editing the properties of a list, this is it.
		/// </summary>
		private ICmPossibilityList m_curList;

		/// <summary>
		/// 'true' if we have made changes to an existing list's properties,
		/// 'false' if no changes have been made.
		/// </summary>
		private bool m_fnameChanged;
		private bool m_fhierarchyChanged;
		private bool m_fsortChanged;
		private bool m_fduplicateChanged;
		private bool m_fwsChanged;
		private bool m_fdisplayByChanged;
		private bool m_fdescriptionChanged;

		#endregion

		/// <summary />
		public ConfigureListDlg(IPropertyTable propertyTable, IPublisher publisher, LcmCache cache, ICmPossibilityList possList)
			: base(propertyTable, publisher, cache)
		{
			m_curList = possList;
			Text = ListResources.ksConfigureList;
			s_helpTopic = "khtpConfigureList";
		}

		protected override void InitializeDialogFields()
		{
			//m_curList is set in constructor
			if (m_curList.IsClosed)
			{
				DisableChanges();
			}
			LoadAllMultiAlternatives(m_curList.Name, m_lmscListName);
			LoadAllMultiAlternatives(m_curList.Description, m_lmscDescription);
			DisplayBy = (PossNameType)m_curList.DisplayOption;
			SortByName = m_curList.IsSorted;
			AllowDuplicate = !m_curList.PreventDuplicates;
			SupportsHierarchy = m_curList.Depth > 1;
			SetWritingSystemCombo();
			ResetAllFlags();
			// We want the OK button enabled, even though it won't do anything unless changes are made.
			EnableOKButton(true);
		}

		private void ResetAllFlags()
		{
			m_fnameChanged = false;
			m_fhierarchyChanged = false;
			m_fsortChanged = false;
			m_fduplicateChanged = false;
			m_fwsChanged = false;
			m_fdisplayByChanged = false;
			m_fdescriptionChanged = false;
		}

		private void SetWritingSystemCombo()
		{
			var curWs = m_curList.WsSelector;
			switch (curWs)
			{
				case WritingSystemServices.kwsAnal:
				case WritingSystemServices.kwsAnals:
				case WritingSystemServices.kwsLim:
					SelectedWs = WritingSystemServices.kwsAnals;
					break;
				case WritingSystemServices.kwsVern:
				case WritingSystemServices.kwsVerns:
					SelectedWs = WritingSystemServices.kwsVerns;
					break;
				default:
					SelectedWs = curWs;
					break;
			}
		}

		protected override void m_chkBoxHierarchy_CheckedChanged(object sender, EventArgs e)
		{
			m_fhierarchyChanged = SupportsHierarchy != (m_curList.Depth > 1);
		}

		protected override void m_chkBoxSortBy_CheckedChanged(object sender, EventArgs e)
		{
			m_fsortChanged = SortByName != m_curList.IsSorted;
		}

		protected override void m_chkBoxDuplicate_CheckedChanged(object sender, EventArgs e)
		{
			m_fduplicateChanged = AllowDuplicate == m_curList.PreventDuplicates;
		}

		protected override void m_wsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_fwsChanged = SelectedWs != m_curList.WsSelector;
		}

		protected override void m_displayByCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_fdisplayByChanged = ((int)DisplayBy) != m_curList.DisplayOption;
		}

		private bool WereChangesMade()
		{
			return m_fnameChanged | m_fhierarchyChanged | m_fsortChanged | m_fduplicateChanged | m_fwsChanged | m_fdisplayByChanged | m_fdescriptionChanged;
		}

		protected override void DoOKAction()
		{
			// LabeledMultiStringControls don't seem to have a valid TextChanged Event, so we have to simulate one.
			m_fnameChanged = HasMsContentChanged(m_curList.Name, m_lmscListName);
			m_fdescriptionChanged = HasMsContentChanged(m_curList.Description, m_lmscDescription);
			if (!WereChangesMade())
			{
				return; // Nothing to do!
			}
			if (m_fnameChanged && IsListNameDuplicated)
			{
				MessageBox.Show(ListResources.ksChooseAnotherListName, ListResources.ksDuplicateName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			UndoableUnitOfWorkHelper.Do(ListResources.ksUndoConfigureList, ListResources.ksRedoConfigureList, Cache.ActionHandlerAccessor, () =>
			{
				m_curList.WsSelector = SelectedWs;
				m_curList.IsVernacular = SelectedWs == WritingSystemServices.kwsVerns || SelectedWs == WritingSystemServices.kwsVernAnals;
				SetAllMultiAlternatives(m_curList.Name, m_lmscListName);
				SetAllMultiAlternatives(m_curList.Description, m_lmscDescription);
				m_curList.DisplayOption = (int)DisplayBy;
				m_curList.IsSorted = SortByName;
				m_curList.PreventDuplicates = !AllowDuplicate;
				m_curList.Depth = SupportsHierarchy ? 127 : 1;
			});
			ResetAllFlags();
		}

		/// <summary>
		/// Searches existing CmPossibilityLists for a Name alternative that matches one in the
		/// dialog's listName MultiStringControl, but isn't in our 'being edited' list.
		/// </summary>
		protected override bool IsListNameDuplicated
		{
			get
			{
				var repo = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
				var cws = m_lmscListName.NumberOfWritingSystems;
				for (var i = 0; i < cws; i++)
				{
					var curWs = m_lmscListName.Ws(i);
					var emptyStr = TsStringUtils.EmptyString(curWs).Text;
					var lmscName = m_lmscListName.Value(curWs).Text;
					if (lmscName != emptyStr && repo.AllInstances().Any(list => list != m_curList && list.Name.get_String(curWs).Text == lmscName))
					{
						return true;
					}
				}
				return false;
			}
		}

		private static bool HasMsContentChanged(IMultiAccessorBase oldStrings, LabeledMultiStringControl msControl)
		{
			var cws = msControl.NumberOfWritingSystems;
			for (var i = 0; i < cws; i++)
			{
				var curWs = msControl.Ws(i);
				if (oldStrings.get_String(curWs).Text != msControl.Value(curWs).Text)
				{
					return true;
				}
			}
			return false;
		}
	}
}