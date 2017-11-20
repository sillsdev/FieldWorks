// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Works
{
	public interface IRecordList : IFlexComponent, IVwNotifyChange, ISortItemProvider, IDisposable
	{
		LcmCache Cache { get; }
		int CurrentIndex { get; set; }
		ICmObject CurrentObject { get; }
		int CurrentObjectHvo { get; }
		bool DeletingObject { get; }
		RecordFilter Filter { get; set; }
		int FirstItemIndex { get; }
		int Flid { get; }
		string FontName { get; }
		bool IsEmpty { get; }
		int LastItemIndex { get; }
		bool ListModificationInProgress { get; set; }
		int NextItemIndex { get; }
		bool OnFirst { get; }
		bool OnLast { get; }
		ICmObject OwningObject { get; set; }
		int PrevItemIndex { get; }
		IProgress ProgressReporter { get; set; }
		string PropertyName { get; }
		bool ShouldNotModifyList { get; }
		ArrayList SortedObjects { get; set; }
		RecordSorter Sorter { get; set; }
		int TypeSize { get; }
		int VirtualFlid { get; }
		ISilDataAccessManaged VirtualListPublisher { get; }

		event EventHandler AboutToReload;
		event EventHandler DoneReload;
		event ListChangedEventHandler ListChanged;

		bool CanInsertClass(string className);
		void ChangeOwningObjectId(int hvo);
		void ChangeSorter(RecordSorter sorter);
		bool CreateAndInsert(string className);
		AndFilter CreateNewAndFilter(params RecordFilter[] filters);
		void DeleteCurrentObject();
		void DeleteCurrentObject(ProgressState state, ICmObject thingToDelete);
		TObj DoCreateAndInsert<TObj>(ICreateAndInsert<TObj> createAndInsertMethodObj) where TObj : ICmObject;
		void ForceReloadList();
		int IndexOfChildOf(int hvoTarget);
		int IndexOfParentOf(int hvoTarget);
		void InitLoad(bool loadList);
		void OnChangeFilter(FilterChangeEventArgs args);
		void ReloadList();
		ICmObject RootObjectAt(int index);
		void TransferOwnership(IDisposable obj);

		IRecordClerk Clerk { get; set; }
		void PersistOn(string pathname);
		bool RestoreFrom(string pathname);
		string PropertyTableId(string sorterOrFilter);
		void ReplaceListItem(int hvoReplaced);
		bool RequestedLoadWhileSuppressed { get; set; }
		bool ListLoadingSuppressed { get; set; }
		void SetSuppressingLoadList(bool value);
		bool NeedToReloadList();
		void RemoveUnwantedSortItems(List<int> hvosToRemove);
		bool CurrentObjectIsValid { get; }
		bool IsDisposed { get; }
		bool IsVirtualPublisherCreated { get; }
		bool IsCurrentObjectValid();
		void ReloadList(int ivMin, int cvIns, int cvDel);
		void ReloadList(int newListItemsClass, int newTargetFlid, bool force);
		bool UpdatingList { get; set; }
		bool EnableSendPropChanged { get; set; }
		ICmObject CreateNewObject(int hvoOwner, IList<ClassAndPropInfo> cpiPath);
	}
}