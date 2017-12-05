// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Works
{
	public interface IRecordClerk : IFlexComponent, IRecordListUpdater, IAnalysisOccurrenceFromHvo, IBulkPropChanged, IVwNotifyChange, IDisposable
	{
		event FilterChangeHandler FilterChangedByClerk;
		event RecordNavigationInfoEventHandler RecordChanged;
		event SelectObjectEventHandler SelectedObjectChanged;
		event EventHandler SorterChangedByClerk;

		void ActivateUI(bool updateStatusBar = true);
		bool AreCustomFieldsAProblem(int[] clsids);
		bool AreSortersCompatible(RecordSorter first, RecordSorter second);
		RecordBarHandler BarHandler { get; }
		void BecomeInactive();
		LcmCache Cache { get; }
		bool CanMoveTo(Navigation navigateTo);
		// int CurrentIndex { get; }
		int CurrentIndex { get; set;  }
		ICmObject CurrentObject { get; }
		int CurrentObjectHvo { get; }
		bool Editable { get; set; }
		// RecordFilter Filter { get; }
		RecordFilter Filter { get; set; }
		bool HasEmptyList { get; }
		string Id { get; }
		bool IsActiveInGui { get; }
		bool IsControllingTheRecordTreeBar { get; set; }
		bool IsDefaultSort { get; set; }
		bool IsDisposed { get; }
		void JumpToIndex(int index, bool suppressFocusChange = false);
		void JumpToRecord(int jumpToHvo, bool suppressFocusChange = false);
		void JumpToRecord(Guid jumpToGuid, bool suppressFocusChange = false);
		int ListItemsClass { get; }
		bool ListLoadingSuppressed { get; set; }
		bool ListLoadingSuppressedNoSideEffects { get; set; }
		bool ListModificationInProgress { get; set; }
		int ListSize { get; }
		void MoveToIndex(Navigation navigateTo);
		bool OnAdjustFilterSelection(object argument);
		void OnChangeFilter(FilterChangeEventArgs args);
		void OnChangeFilterClearAll(object commandObject);
		void OnChangeListItemsClass(int listItemsClass, int newTargetFlid, bool force);
		void OnChangeSorter();
		bool OnDeleteRecord(object commandObject);
		bool OnExport(object argument);
		bool OnInsertItemInVector(object argument);
		void OnItemDataModified(object argument);
		bool OnJumpToRecord(object argument);
		bool OnLast { get; }
		void OnPropertyChanged(string name);
		bool OnRefresh(object argument);
		void OnSorterChanged(RecordSorter sorter, string sortName, bool isDefaultSort);
		ICmObject OwningObject { get; set; }
		int OwningFlid { get; }
		IRecordClerk ParentClerk { get; }
		string PersistedIndexProperty { get; }
		void PersistListOn(string pathname);
		IProgress ProgressReporter { get; set; }
		// IRecordList RecordList { get; }
		IRecordList MyRecordList { get; }
		void ReloadFilterProvider();
		void ReloadIfNeeded();
		void RemoveInvalidItems();
		void RemoveItemsFor(int hvoToRemove);
		// bool RequestedLoadWhileSuppressed { get; }
		bool RequestedLoadWhileSuppressed { get; set; }
		void ResetFilterToDefault();
		bool RestoreListFrom(string pathname);
		void SaveOnChangeRecord();
		void SelectedRecordChanged(bool suppressFocusChange, bool fSkipRecordNavigation = false);
		bool ShouldNotModifyList { get; }
		bool SkipShowRecord { get; set; }
		// RecordSorter Sorter { get; }
		RecordSorter Sorter { get; set; }
		ISortItemProvider SortItemProvider { get; }
		string SortName { get; }
		bool SuppressSaveOnChangeRecord { get; set; }
		bool SuspendLoadingRecordUntilOnJumpToRecord { get; set; }
		bool SuspendLoadListUntilOnChangeFilter { get; set; }
		bool TryClerkProvidingRootObject(out IRecordClerk clerkProvidingRootObject);
		bool UpdateFiltersAndSortersIfNeeded();
		ListUpdateHelper UpdateHelper { get; set; }
		void UpdateOwningObjectIfNeeded();
		void UpdateRecordTreeBarIfNeeded();
		void UpdateStatusBarRecordNumber(string noRecordsText);
		void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e);
		int VirtualFlid { get; }
		ISilDataAccessManaged VirtualListPublisher { get; }
	}
}