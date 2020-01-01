// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer
{
	internal interface IRecordList : IAnalysisOccurrenceFromHvo, IBulkPropChanged, IDisposable, IFlexComponent, IRecordListUpdater, ISortItemProvider, IVwNotifyChange
	{
		event FilterChangeHandler FilterChangedByList;
		event RecordNavigationInfoEventHandler RecordChanged;
		event RecordNavigationInfoEventHandler OwningObjectChanged;
		event SelectObjectEventHandler SelectedObjectChanged;
		event EventHandler SorterChangedByList;

		void ActivateUI(bool updateStatusBar = true);
		void BecomeInactive();
		bool CanChangeFilterClearAll { get; }
		bool CanDelete { get; }
		IReadOnlyDictionary<Navigation, bool> CanMoveToOptions { get; }
		int CurrentIndex { get; set; }
		ICmObject CurrentObject { get; }
		int CurrentObjectHvo { get; }
		bool Editable { get; set; }
		RecordFilter Filter { get; set; }
		string FontName { get; }
		bool HasEmptyList { get; }
		string Id { get; }
		bool IsActiveInGui { get; }
		bool IsControllingTheRecordTreeBar { get; set; }
		bool IsDefaultSort { get; set; }
		bool IsSortingByHeadword { get; }
		bool IsSubservientRecordList { get; }
		void JumpToIndex(int index, bool suppressFocusChange = false);
		void JumpToRecord(int jumpToHvo, bool suppressFocusChange = false);
		bool ListLoadingSuppressed { get; set; }
		bool ListLoadingSuppressedNoSideEffects { get; set; }
		bool ListModificationInProgress { get; set; }
		int ListSize { get; }
		void MoveToIndex(Navigation navigateTo);
		ITreeBarHandler MyTreeBarHandler { get; }
		void OnChangeFilterClearAll();
		void OnChangeFilter(FilterChangeEventArgs args);
		void OnChangeListItemsClass(int listItemsClass, int newTargetFlid, bool force);
		bool DeleteRecord(string uowBaseText, StatusBarProgressPanel panel);
		bool OnFirst { get; }
		bool OnInsertItemInVector(object argument);
		void OnItemDataModified(object argument);
		bool OnJumpToRecord(object argument);
		bool OnLast { get; }
		bool OnRefresh(object argument);
		void OnSorterChanged(RecordSorter sorter, string sortName, bool isDefaultSort);
		ICmObject OwningObject { get; set; }
		int OwningFlid { get; }
		IRecordList ParentList { get; }
		string PersistedIndexProperty { get; }
		void PersistListOn(string pathname);
		string PropertyName { get; }
		void ReloadFilterProvider();
		void ReloadIfNeeded();
		bool RequestedLoadWhileSuppressed { get; set; }
		void ResetFilterToDefault();
		bool RestoreListFrom(string pathname);
		void SaveOnChangeRecord();
		void SelectedRecordChanged(bool suppressFocusChange, bool fSkipRecordNavigation = false);
		bool ShouldNotModifyList { get; }
		bool ShouldNotHandleDeletionMessage { get; }
		bool SkipShowRecord { get; set; }
		List<IManyOnePathSortItem> SortedObjects { get; set; }
		RecordSorter Sorter { get; }
		string SortName { get; set; }
		bool SuppressSaveOnChangeRecord { get; set; }
		bool SuspendLoadingRecordUntilOnJumpToRecord { get; set; }
		int TypeSize { get; }
		bool UpdateFiltersAndSortersIfNeeded();
		bool UpdatingList { get; set; }
		void UpdateOwningObject(bool updateOwningObjectOnlyIfChanged = false);
		void UpdateRecordTreeBar();
		void UpdateRecordTreeBarIfNeeded();
		void UpdateStatusBarRecordNumber(string noRecordsText);
		void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e);
		int VirtualFlid { get; }
		ISilDataAccessManaged VirtualListPublisher { get; }
	}
}