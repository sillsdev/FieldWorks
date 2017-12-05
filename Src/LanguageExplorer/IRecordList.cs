// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Works;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer
{
	public interface IRecordList : IAnalysisOccurrenceFromHvo, IBulkPropChanged, IDisposable, IFlexComponent, IRecordListUpdater, ISortItemProvider, IVwNotifyChange
	{
		event EventHandler AboutToReload;
		event EventHandler DoneReload;
		event FilterChangeHandler FilterChangedByList;
		event ListChangedEventHandler ListChanged;
		event RecordNavigationInfoEventHandler RecordChanged;
		event SelectObjectEventHandler SelectedObjectChanged;
		event EventHandler SorterChangedByList;

		void ActivateUI(bool updateStatusBar = true);
		bool AreCustomFieldsAProblem(int[] clsids);
		RecordBarHandler BarHandler { get; }
		void BecomeInactive();
		LcmCache Cache { get; }
		bool CanInsertClass(string className);
		bool CanMoveTo(Navigation navigateTo);
		void ChangeOwningObjectId(int hvo);
		void ChangeSorter(RecordSorter sorter);
		bool CreateAndInsert(string className);
		AndFilter CreateNewAndFilter(params RecordFilter[] filters);
		ICmObject CreateNewObject(int hvoOwner, IList<ClassAndPropInfo> cpiPath);
		int CurrentIndex { get; set; }
		ICmObject CurrentObject { get; }
		int CurrentObjectHvo { get; }
		bool CurrentObjectIsValid { get; }
		void DeleteCurrentObject(ICmObject thingToDelete);
		bool DeletingObject { get; }
		TObj DoCreateAndInsert<TObj>(ICreateAndInsert<TObj> createAndInsertMethodObj) where TObj : ICmObject;
		bool Editable { get; set; }
		bool EnableSendPropChanged { get; set; }
		RecordFilter Filter { get; set; }
		int FirstItemIndex { get; }
		int Flid { get; }
		string FontName { get; }
		void ForceReloadList();
		bool HasEmptyList { get; }
		string Id { get; }
		int IndexOfChildOf(int hvoTarget);
		int IndexOfParentOf(int hvoTarget);
		void InitLoad(bool loadList);
		bool IsActiveInGui { get; }
		bool IsControllingTheRecordTreeBar { get; set; }
		bool IsCurrentObjectValid();
		bool IsDefaultSort { get; set; }
		//bool IsDisposed { get; }
		bool IsEmpty { get; }
		bool IsVirtualPublisherCreated { get; }
		void JumpToIndex(int index, bool suppressFocusChange = false);
		void JumpToRecord(int jumpToHvo, bool suppressFocusChange = false);
		void JumpToRecord(Guid jumpToGuid, bool suppressFocusChange = false);
		int LastItemIndex { get; }
		bool ListLoadingSuppressed { get; set; }
		bool ListLoadingSuppressedNoSideEffects { get; set; }
		bool ListModificationInProgress { get; set; }
		int ListSize { get; }
		void MoveToIndex(Navigation navigateTo);
		bool NeedToReloadList();
		int NextItemIndex { get; }
		bool OnAdjustFilterSelection(object argument);
		void OnChangeFilter(FilterChangeEventArgs args);
		void OnChangeFilterClearAll(object commandObject);
		void OnChangeListItemsClass(int listItemsClass, int newTargetFlid, bool force);
		void OnChangeSorter();
		bool OnDeleteRecord(object commandObject);
		bool OnExport(object argument);
		bool OnFirst { get; }
		bool OnInsertItemInVector(object argument);
		void OnItemDataModified(object argument);
		bool OnJumpToRecord(object argument);
		bool OnLast { get; }
		void OnPropertyChanged(string name);
		bool OnRefresh(object argument);
		void OnSorterChanged(RecordSorter sorter, string sortName, bool isDefaultSort);
		ICmObject OwningObject { get; set; }
		int OwningFlid { get; }
		IRecordList ParentList { get; }
		string PersistedIndexProperty { get; }
		void PersistListOn(string pathname);
		void PersistOn(string pathname);
		int PrevItemIndex { get; }
		IProgress ProgressReporter { get; set; }
		string PropertyName { get; }
		string PropertyTableId(string sorterOrFilter);
		void ReloadFilterProvider();
		void ReloadIfNeeded();
		void ReloadList();
		void ReloadList(int ivMin, int cvIns, int cvDel);
		void ReloadList(int newListItemsClass, int newTargetFlid, bool force);
		void RemoveInvalidItems();
		void RemoveUnwantedSortItems(List<int> hvosToRemove = null);
		void ReplaceListItem(int hvoReplaced, ListChangedEventArgs.ListChangedActions listChangeAction = ListChangedEventArgs.ListChangedActions.Normal);
		bool RequestedLoadWhileSuppressed { get; set; }
		void ResetFilterToDefault();
		bool RestoreListFrom(string pathname);
		ICmObject RootObjectAt(int index);
		void SaveOnChangeRecord();
		void SelectedRecordChanged(bool suppressFocusChange, bool fSkipRecordNavigation = false);
		void SetSuppressingLoadList(bool value);
		bool ShouldNotModifyList { get; }
		bool SkipShowRecord { get; set; }
		ArrayList SortedObjects { get; set; }
		RecordSorter Sorter { get; }
		string SortName { get; }
		bool SuppressSaveOnChangeRecord { get; set; }
		bool SuspendLoadingRecordUntilOnJumpToRecord { get; set; }
		bool SuspendLoadListUntilOnChangeFilter { get; set; }
		void TransferOwnership(IDisposable obj);
		bool TryListProvidingRootObject(out IRecordList recordListProvidingRootObject);
		int TypeSize { get; }
		bool UpdateFiltersAndSortersIfNeeded();
		ListUpdateHelper UpdateHelper { get; set; }
		bool UpdatingList { get; set; }
		void UpdateOwningObjectIfNeeded();
		void UpdateRecordTreeBarIfNeeded();
		void UpdateStatusBarRecordNumber(string noRecordsText);
		void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e);
		int VirtualFlid { get; }
		ISilDataAccessManaged VirtualListPublisher { get; }
	}
}