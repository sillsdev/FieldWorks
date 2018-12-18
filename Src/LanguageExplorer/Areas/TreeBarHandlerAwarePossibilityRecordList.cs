// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Class that knows how to manage an instance of ICmPossibilityList AND the related PossibilityTreeBarHandler.
	/// </summary>
	internal sealed class TreeBarHandlerAwarePossibilityRecordList : PossibilityRecordList
	{
		private static IRecordList s_lastRecordListToLoadTreeBar;
		private PossibilityTreeBarHandler _possibilityTreeBarHandler;

		/// <summary />
		internal TreeBarHandlerAwarePossibilityRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, ICmPossibilityList ownedPossibilityList, PossibilityTreeBarHandler possibilityTreeBarHandler, RecordFilterParameterObject recordFilterParameterObject = null)
			: base(id, statusBar, decorator, ownedPossibilityList, recordFilterParameterObject)
		{
			Guard.AgainstNull(possibilityTreeBarHandler, nameof(possibilityTreeBarHandler));

			_possibilityTreeBarHandler = possibilityTreeBarHandler;
			IsControllingTheRecordTreeBar = true;
		}

		#region Overrides of RecordList

		public override bool IsControllingTheRecordTreeBar
		{
			get { return true; }
			set { base.IsControllingTheRecordTreeBar = true; }
		}

		public override ITreeBarHandler MyTreeBarHandler => _possibilityTreeBarHandler;

		public override void BecomeInactive()
		{
			_possibilityTreeBarHandler.ReleaseRecordBar();
			base.BecomeInactive();
		}

		public override void UpdateRecordTreeBar()
		{
			_possibilityTreeBarHandler.ForcePopulateRecordBar(this);
		}

		public override void UpdateRecordTreeBarIfNeeded()
		{
			_possibilityTreeBarHandler.PopulateRecordBarIfNeeded(this);
		}

		protected override void ActivateRecordBar()
		{
			if (s_lastRecordListToLoadTreeBar == this)
			{
				return;
			}
			s_lastRecordListToLoadTreeBar = this;
			_possibilityTreeBarHandler.ForcePopulateRecordBar(this);
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_possibilityTreeBarHandler.Dispose();
			}
			_possibilityTreeBarHandler = null;

			base.Dispose(disposing);
		}

		protected override void UpdateStatusBarForRecordBar()
		{
			UpdateStatusBarRecordNumber(StringTable.Table.GetString("No Records", "Misc"));
		}

		protected override void UpdateSelectionForRecordBar()
		{
			_possibilityTreeBarHandler.UpdateSelection(CurrentObject);
			OnSelectedObjectChanged(new SelectObjectEventArgs(CurrentObject));
		}

		/// <summary>
		/// Update the contents of the tree bar and anything else that should change when,
		/// for example, the filter or sort order changes.
		/// </summary>
		protected override void OnListChanged(int hvo = 0, ListChangedActions actions = ListChangedActions.Normal)
		{
			if (actions == ListChangedActions.UpdateListItemName)
			{
				// In the case where there are no other items and the Current object isn't valid,
				// then just don't do anything.  LT-5849.
				// A more robust solution would be to have in our design a way to produce
				// a 'defered' prop changed so that the current actions can finish before
				// others are notified of the change (which is often incomplete at that time).
				// The stack for this issue showed the RecordList being
				// re-entered while they were deleting an object in a previous stack frame.
				// This is not the only case where this has been noted, but a solution has
				// not yet been thought of.
				// In the meantime, this fixed the crash .. <sigh> but doesn't help at all
				// for the other cases where this can happen.
				if (CurrentObject != null && (CurrentObject.Cache != null || SortedObjects.Count != 1))
				{
					// all we need to do is replace the currently selected item in the tree.
					ICmObject obj = null;
					if (hvo != 0)
					{
						m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(hvo, out obj);
					}
					if (obj == null)
					{
						obj = CurrentObject;
					}
					_possibilityTreeBarHandler.ReloadItem(obj);
				}
			}
			else
			{
				_possibilityTreeBarHandler.PopulateRecordBar(this);
			}

			base.OnListChanged(hvo, actions);
		}

		#endregion
	}
}