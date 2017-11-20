// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Works
{
	public interface IRecordClerk : IFlexComponent, IRecordListUpdater, IAnalysisOccurrenceFromHvo, IBulkPropChanged, IVwNotifyChange, IDisposable
	{
		LcmCache Cache { get; }
		int CurrentIndex { get; }
		ICmObject CurrentObject { get; }
		int CurrentObjectHvo { get; }
		bool Editable { get; set; }
		RecordFilter Filter { get; }
		bool HasEmptyList { get; }
		string Id { get; }
		bool IsControllingTheRecordTreeBar { get; set; }
		bool IsDefaultSort { get; set; }
		bool IsDisposed { get; }
		int ListItemsClass { get; }
		bool ListLoadingSuppressed { get; set; }
		bool ListModificationInProgress { get; set; }
		int ListSize { get; }
		bool OnLast { get; }
		ICmObject OwningObject { get; set; }
		IProgress ProgressReporter { get; set; }
		bool ShouldNotModifyList { get; }
		RecordSorter Sorter { get; }
		ISortItemProvider SortItemProvider { get; }
		string SortName { get; }
		bool SuppressSaveOnChangeRecord { get; set; }
		bool SuspendLoadingRecordUntilOnJumpToRecord { get; set; }
		int VirtualFlid { get; }
		ISilDataAccessManaged VirtualListPublisher { get; }
		event FilterChangeHandler FilterChangedByClerk;
		event RecordNavigationInfoEventHandler RecordChanged;
		event SelectObjectEventHandler SelectedObjectChanged;
		event EventHandler SorterChangedByClerk;
		void ActivateUI(bool useRecordTreeBar, bool updateStatusBar = true);
		bool AreSortersCompatible(RecordSorter first, RecordSorter second);
		void BecomeInactive();
		void CheckDisposed();
		void JumpToIndex(int index);
		void JumpToIndex(int index, bool suppressFocusChange);
		void JumpToRecord(int jumpToHvo);
		void JumpToRecord(Guid jumpToGuid);
		void JumpToRecord(int jumpToHvo, bool suppressFocusChange);
		void JumpToRecord(Guid jumpToGuid, bool suppressFocusChange);
		bool OnAdjustFilterSelection(object argument);
		void OnChangeFilter(FilterChangeEventArgs args);
		void OnChangeFilterClearAll(object commandObject);
		void OnChangeSorter();
		bool OnDeleteRecord(object commandObject);
		bool OnExport(object argument);
		bool OnInsertItemInVector(object argument);
		void OnItemDataModified(object argument);
		bool OnJumpToRecord(object argument);
		void OnPropertyChanged(string name);
		bool OnRefresh(object argument);
		bool AreCustomFieldsAProblem(int[] clsids);
		void OnSorterChanged(RecordSorter sorter, string sortName, bool isDefaultSort);
		void ReloadFilterProvider();
		void ReloadIfNeeded();
		void RemoveInvalidItems();
		void RemoveItemsFor(int hvoToRemove);
		void SaveOnChangeRecord();
		void SelectedRecordChanged(bool suppressFocusChange);
		void SelectedRecordChanged(bool fSkipRecordNavigation, bool suppressFocusChange);
		void UpdateOwningObjectIfNeeded();
		void UpdateRecordTreeBarIfNeeded();
		void UpdateStatusBarRecordNumber(string noRecordsText);
		void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e);


		bool CanMoveTo(Navigation navigateTo);
		void MoveToIndex(Navigation navigateTo);
		IRecordList RecordList { get; }
		bool SkipShowRecord { get; set; }
		IRecordClerk ParentClerk { get; }
		bool RequestedLoadWhileSuppressed { get; }
		RecordClerk.ListUpdateHelper UpdateHelper { get; set; }
		bool ListLoadingSuppressedNoSideEffects { get; set; }
		bool TryClerkProvidingRootObject(out IRecordClerk clerkProvidingRootObject);
		bool IsActiveInGui { get; }
		bool SuspendLoadListUntilOnChangeFilter { get; set; }
		string PersistedIndexProperty { get; }
		void UpdateList(bool fRefreshRecord, bool forceSort);
		bool SetCurrentFromRelatedClerk();
		void PersistListOn(string pathname);
		bool RestoreListFrom(string pathname);
		void OnChangeListItemsClass(int listItemsClass, int newTargetFlid, bool force);
		bool UpdateFiltersAndSortersIfNeeded();
		void ResetFilterToDefault();
		int OwningFlid { get; }
		RecordBarHandler BarHandler { get; }
		void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e, IVwSelection sel);
	}
}