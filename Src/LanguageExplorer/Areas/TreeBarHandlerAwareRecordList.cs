// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas
{
	internal sealed class TreeBarHandlerAwareRecordList : RecordList
	{
		private static IRecordList s_lastRecordListToLoadTreeBar;

		private RecordBarHandler _recordBarHandler;
		/// <summary>
		/// Constructor for a list that is owned or not.
		/// </summary>
		internal TreeBarHandlerAwareRecordList(string id, StatusBar statusBar, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion, ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid, ICmObject owner, string propertyName, RecordBarHandler recordBarHandler)
			: base(id, statusBar, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion, decorator, usingAnalysisWs, flid, owner, propertyName)
		{
			Guard.AgainstNull(recordBarHandler, nameof(recordBarHandler));

			_recordBarHandler = recordBarHandler;
			IsControllingTheRecordTreeBar = true;
		}

		#region Overrides of RecordList

		public override bool IsControllingTheRecordTreeBar
		{
			get { return true; }
			set { base.IsControllingTheRecordTreeBar = true; }
		}

		public override RecordBarHandler BarHandler => _recordBarHandler;

		public override void BecomeInactive()
		{
			_recordBarHandler.ReleaseRecordBar();
			base.BecomeInactive();
		}

		public override void UpdateRecordTreeBarIfNeeded()
		{
			_recordBarHandler.PopulateRecordBarIfNeeded(this);
		}

		protected override void ActivateRecordBar()
		{
			if (s_lastRecordListToLoadTreeBar == this)
			{
				return;
			}
			s_lastRecordListToLoadTreeBar = this;
			_recordBarHandler.PopulateRecordBar(this);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_recordBarHandler.Dispose();
			}
			_recordBarHandler = null;

			base.Dispose(disposing);
		}

		protected override void UpdateStatusBarForRecordBar()
		{
			UpdateStatusBarRecordNumber(StringTable.Table.GetString("No Records", "Misc"));
		}

		protected override void UpdateSelectionForRecordBar()
		{
			_recordBarHandler.UpdateSelection(CurrentObject);
			OnSelectedObjectChanged(new SelectObjectEventArgs(CurrentObject));
		}

		/// <summary>
		/// Update the contents of the tree bar and anything else that should change when,
		/// for example, the filter or sort order changes.
		/// </summary>
		protected override void OnListChanged(object src, ListChangedEventArgs arguments)
		{
			if (arguments.Actions == ListChangedEventArgs.ListChangedActions.UpdateListItemName)
			{
				// ******************************************************************************
				// In the case where there are no other items and the Current object isn't valid,
				// then just don't do anything.  LT-5849.
				// A more robust solution would be to have in our design a way to produce
				// a 'defered' prop changed so that the current actions can finish before
				// others are notified of the change (which is often incomplete at that time).
				// The stack for this issue showed the RecordList and RecordClerk being
				// re-entered while they were deleting an object in a previous stack frame.
				// This is not the only case where this has been noted, but a solution has
				// not yet been thought of.
				// In the meantime, this fixed the crash .. <sigh> but doesn't help at all
				// for the other cases where this can happen.
				// ******************************************************************************
				if (_recordBarHandler is TreeBarHandler && CurrentObject != null && (CurrentObject.Cache != null || SortedObjects.Count != 1))
				{
					// all we need to do is replace the currently selected item in the tree.
					var hvoItem = arguments.ItemHvo;
					ICmObject obj = null;
					if (hvoItem != 0)
					{
						Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(hvoItem, out obj);
					}
					if (obj == null)
					{
						obj = CurrentObject;
					}
					_recordBarHandler.ReloadItem(obj);
				}
			}
			else
			{
				_recordBarHandler.PopulateRecordBar(this);
			}

			base.OnListChanged(src, arguments);
		}

		#endregion
	}
}