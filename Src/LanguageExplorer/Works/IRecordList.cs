// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Works
{
	public interface IRecordList : IFlexComponent, ISortItemProvider, IVwNotifyChange, IDisposable
	{
		event EventHandler AboutToReload;
		event EventHandler DoneReload;
		event ListChangedEventHandler ListChanged;

		LcmCache Cache { get; }
		bool CanInsertClass(string className);
		void ChangeOwningObjectId(int hvo);
		void ChangeSorter(RecordSorter sorter);
		IRecordClerk Clerk { get; set; }
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
		bool EnableSendPropChanged { get; set; }
		RecordFilter Filter { get; set; }
		int FirstItemIndex { get; }
		int Flid { get; }
		string FontName { get; }
		void ForceReloadList();
		int IndexOfChildOf(int hvoTarget);
		int IndexOfParentOf(int hvoTarget);
		void InitLoad(bool loadList);
		bool IsCurrentObjectValid();
		bool IsDisposed { get; }
		bool IsEmpty { get; }
		bool IsVirtualPublisherCreated { get; }
		int LastItemIndex { get; }
		bool ListLoadingSuppressed { get; set; }
		bool ListModificationInProgress { get; set; }
		bool NeedToReloadList();
		int NextItemIndex { get; }
		void OnChangeFilter(FilterChangeEventArgs args);
		bool OnFirst { get; }
		bool OnLast { get; }
		ICmObject OwningObject { get; set; }
		void PersistOn(string pathname);
		int PrevItemIndex { get; }
		IProgress ProgressReporter { get; set; }
		string PropertyName { get; }
		string PropertyTableId(string sorterOrFilter);
		void ReloadList();
		void ReloadList(int ivMin, int cvIns, int cvDel);
		void ReloadList(int newListItemsClass, int newTargetFlid, bool force);
		void RemoveUnwantedSortItems(List<int> hvosToRemove = null);
		void ReplaceListItem(int hvoReplaced, ListChangedEventArgs.ListChangedActions listChangeAction = ListChangedEventArgs.ListChangedActions.Normal);
		bool RequestedLoadWhileSuppressed { get; set; }
		bool RestoreFrom(string pathname);
		ICmObject RootObjectAt(int index);
		void SetSuppressingLoadList(bool value);
		bool ShouldNotModifyList { get; }
		ArrayList SortedObjects { get; set; }
		RecordSorter Sorter { get; set; }
		void TransferOwnership(IDisposable obj);
		int TypeSize { get; }
		bool UpdatingList { get; set; }
		int VirtualFlid { get; }
		ISilDataAccessManaged VirtualListPublisher { get; }
	}
}