// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// This is a temporary record list that can be used in a guicontrol where the parent control knows
	/// when the list contents have changed, and to what.
	///
	/// The 'owner' is the now defunct class "WordformInventory" and the property is its old owning "Wordforms" collection.
	///
	/// This list is only used by the "WordformsBrowseView" guicontrol, which is in turn only used by the "WordformGoDlg"
	/// </summary>
	internal sealed class MatchingItemsRecordList : TemporaryRecordList
	{
		private IEnumerable<int> m_objs;

		internal MatchingItemsRecordList(ISilDataAccessManaged decorator, StatusBar statusBar, ILangProject languageProject)
			: base("matchingWords", statusBar, decorator, false, new VectorPropertyParameterObject(languageProject, "AllWordforms", decorator.MetaDataCache.GetFieldId2(languageProject.ClassID, "AllWordforms", false)))
		{
		}

		#region Overrides of IRecordList

		protected override void InitLoad(bool loadList)
		{
			ComputeInsertableClasses();
			CurrentIndex = -1;
			m_hvoCurrent = 0;
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
		}

		/// <summary>
		/// We never want to filter matching items displayed in the dialog.  See LT-6422.
		/// </summary>
		public override RecordFilter Filter
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		#endregion Overrides of IRecordList

		#region Overrides of RecordList

		protected override IEnumerable<int> GetObjectSet()
		{
			return m_objs ?? new int[0];
		}

		protected override bool TryRestoreFilter()
		{
			return false;
		}

		protected override bool TryRestoreSorter()
		{
			return false;
		}

		#endregion Overrides of RecordList

		/// <summary>
		/// This reloads the list using the supplied set of hvos.
		/// </summary>
		/// <param name="objs"></param>
		public void UpdateList(IEnumerable<int> objs)
		{
			m_objs = objs;
			ReloadList();
		}
	}
}