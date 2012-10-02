// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2008' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoChangesUnitOfWork.cs
// Responsibility: Randy Regnier
// Last reviewed: never
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This class manages CmObjects that are new, modified, or deleted in one sequence of related changes.
	///
	/// This class implements most of a "Unit of Work" pattern that keeps track of
	/// a related group of changes to CmObjects.
	///
	/// The series of individual changes is bounded on one end by
	/// a call to the 'BeginUndoTask' method of FDO's ISILDataAccess implementation,
	/// or more directly via FDO's IActionHandler implementation.
	/// The series is terminated by a call to the corresponding "EndUndoTask".
	///
	/// This class will also produce an XML log of changes suitable for use
	/// in updating a non-networked computer. The XML will go into a larger
	/// 'Session Log' of changes.
	///
	/// This class will diverge from the prototypical 'Unit of Work'
	/// in that it won't directly update the data store. It does gather the relevant data
	/// for that persisting opertation, however.
	/// </summary>
	internal abstract class FdoUnitOfWork
	{
		private static int s_sequenceNumber;
		protected readonly List<IUndoAction> m_changes = new List<IUndoAction>();
		protected readonly UnitOfWorkService m_uowService;
		private readonly HashSet<Guid> m_newGuids = new HashSet<Guid>();
		// Sets used for Unit of Work support.
		private readonly HashSet<ICmObjectId> m_newObjects = new HashSet<ICmObjectId>();
		private readonly HashSet<ICmObjectOrSurrogate> m_dirtyObjects = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
		private HashSet<ICmObject> m_DateModifiedObjects;
		private readonly HashSet<ICmObjectId> m_deletedObjects = new HashSet<ICmObjectId>();
		internal int Sequence { get; set; }
		/// <summary>
		/// This dictionary supports rapidly finding an existing property change undo action for a given property.
		/// For the purposes of this dictionary, two FdoPropertyChanges (considered as keys) are equal if they modify
		/// the same property of the same object (and, if relevant, the same writing system alternative). The value
		/// is the same as the key, an existing change action for that property.
		/// </summary>
		private readonly Dictionary<FdoPropertyChangedBase, FdoPropertyChangedBase> m_propertyChanges =
			new Dictionary<FdoPropertyChangedBase, FdoPropertyChangedBase>(new ComparePropertyChanges());

		/// <summary>
		/// This (usually never-created) list records any instances of the special action we use when a custom field
		/// is being added, removed, or modified (that is, when the definition of the field is being changed for all
		/// objects, not when the value is changed for an instance).
		/// </summary>
		private List<FdoStateChangeCustomFieldDefnModified> m_customFieldChanges;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FdoUnitOfWork"/> class.
		/// </summary>
		/// <param name="uowService">The unit-of-work service.</param>
		/// ------------------------------------------------------------------------------------
		internal FdoUnitOfWork(UnitOfWorkService uowService)
		{
			Sequence = s_sequenceNumber++;
			m_uowService = uowService;
		}

		/// <summary>
		/// Get all the dirty objects.
		/// </summary>
		internal IEnumerable<ICmObjectInternal> DirtyObjects
		{
			get
			{
				foreach (var item in m_dirtyObjects)
					if (!m_deletedObjects.Contains(item.Id))
						yield return (ICmObjectInternal) item.Object;
			}
		}
		internal HashSet<ICmObjectId> NewObjects { get { return m_newObjects; } }

		/// <summary>
		/// Answer a new UOW which is the opposite of this one in terms of objects deleted, inserted, and changed.
		/// Note that it does NOT have the inverse set of changes! This can only be reliably done if the object
		/// is in the Undone state, so that the objects it deletes are undeleted and so exist currently.
		/// </summary>
		internal FdoUnitOfWork InverseObjectChanges
		{
			get
			{
				var result = new FdoNonUndoableUnitOfWork(m_uowService);
				foreach (var id in m_newObjects)
					result.m_deletedObjects.Add(id);
				foreach (var obj in m_dirtyObjects)
					result.m_dirtyObjects.Add(obj);
				foreach (var id in m_deletedObjects)
					result.m_newObjects.Add(id);
				return result;
			}
		}

		/// <summary>
		/// Two units of work have conflicting changes if they create, modify, or delete any of the same objects.
		/// In addition, since this is considered the change which must not be harmed by undoing (or redoing)
		/// </summary>
		internal bool AffectsSameObjects(FdoUnitOfWork other)
		{
			var itsChangedObjects = new HashSet<ICmObjectId>(other.m_deletedObjects);
			foreach(var item in other.m_dirtyObjects)
				itsChangedObjects.Add(item.Id);
			foreach(var item in other.m_newObjects)
				itsChangedObjects.Add(item);
			foreach (var item in m_dirtyObjects)
			{
				if (itsChangedObjects.Contains(item.Id))
					return true;
			}
			foreach (var item in m_newObjects)
			{
				if (itsChangedObjects.Contains(item))
					return true;
			}
			foreach (var item in m_deletedObjects)
			{
				if (itsChangedObjects.Contains(item))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Store one object that has had its DateModified property changed explicitly.
		/// </summary>
		internal void AddDateModifiedObject(ICmObject obj)
		{
			if (m_DateModifiedObjects == null)
				m_DateModifiedObjects = new HashSet<ICmObject>();
			m_DateModifiedObjects.Add(obj);
		}

		/// <summary>
		/// Check whether the given object has had its DateModified property changed explicitly.
		/// </summary>
		internal bool IsDateModifiedExplicitly(ICmObject obj)
		{
			if (m_DateModifiedObjects == null)
				return false;
			else
				return m_DateModifiedObjects.Contains(obj);
		}

		/// <summary>
		/// A change is affected by undoing another unit of work if it affects any of the same objects,
		/// or if it adds a reference to an item which the other change created (and which would therefore
		/// be deleted by undoing it).
		/// </summary>
		internal bool IsAffectedByUndoing(FdoUnitOfWork other)
		{
			if (AffectsSameObjects(other))
				return true;
			//// We have a problem if the other change creates an object, and this one creates a reference
			//// to that object. If we Undo the other change first, the reference we created will be dangling.
			//if (other.m_newObjects.Count != 0)
			//{
			//    var guids = new HashSet<Guid>(from obj in other.m_newObjects select obj.Id.Guid);
			//    foreach (var item in m_changes)
			//    {
			//        var change = item as FdoStateChangeBase;
			//        if (change != null && change.RefersToAfterChange(guids))
			//            return true;
			//    }
			//}
			//// We have a problem if this change deletes an object, and the other one deleted a
			//// reference to it. Undoing the other one first will re-create the reference without
			//// re-creating a target for it.
			//if (m_deletedObjects.Count == 0)
			//    return false;
			//var deletedGuids = new HashSet<Guid>(from obj in m_deletedObjects select obj.Guid);
			//foreach (var item in other.m_changes)
			//{
			//    var change = item as FdoStateChangeBase;
			//    if (change != null && change.RefersToBeforeChange(deletedGuids))
			//        return true;
			//}

			return false;
		}


		/// <summary>
		/// A change is affected by redoing another unit of work if it affects any of the same objects,
		/// or if it removes a reference to an item which the other change deletes (and so redoing the
		/// other change first would delete the target without first removing the reference).
		/// </summary>
		internal bool IsAffectedByRedoing(FdoUnitOfWork other)
		{
			if (AffectsSameObjects(other))
				return true;

			//// We have a problem if the other change creates a reference to an object, and this change
			//// creates the object. If we redo the other change first, the reference will exist without a target.
			//if (m_newObjects.Count != 0)
			//{
			//    var guids = new HashSet<Guid>(from obj in m_newObjects select obj.Id.Guid);
			//    foreach (var item in other.m_changes)
			//    {
			//        var change = item as FdoStateChangeBase;
			//        if (change != null && change.RefersToAfterChange(guids))
			//            return true;
			//    }
			//}
			//// We have a problem if this change deletes a reference to an object, and the other change
			//// deletes the object. If we redo the other change first, the object will be deleted, but not
			//// the reference to it.
			//if (other.m_deletedObjects.Count == 0)
			//    return false;
			//var deletedGuids = new HashSet<Guid>(from obj in other.m_deletedObjects select obj.Guid);
			//foreach (var item in m_changes)
			//{
			//    var change = item as FdoStateChangeBase;
			//    if (change != null && change.RefersToBeforeChange(deletedGuids))
			//        return true;
			//}

			return false;
		}

		class ComparePropertyChanges : IEqualityComparer<FdoPropertyChangedBase>
		{
			public bool Equals(FdoPropertyChangedBase x, FdoPropertyChangedBase y)
			{
				return x.Object == y.Object && x.ModifiedFlid == y.ModifiedFlid && x.WS == y.WS;

			}

			public int GetHashCode(FdoPropertyChangedBase obj)
			{
				return obj.Object.GetHashCode() ^ obj.ModifiedFlid ^ obj.WS;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo the action(s) in this object.
		/// </summary>
		/// <returns>
		/// Cumulative result of undoing all the actions. If we succeeded, then this
		/// will be kuresSuccess.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal abstract UndoResult Undo();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redo the action(s) in this object. This is overridden to do nothing in
		/// non-undoable unit of work, but may be called even from there in one special case.
		/// </summary>
		/// <returns>
		/// Cumulative result of redoing all the actions. If we succeeded, then this will
		/// be kuresSuccess.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redo the action(s) in this object.
		/// </summary>
		internal virtual UndoResult Redo()
		{
			// Because of the complexities of our undo/redo system, we need to do the redo
			// in multiple passes:
			// 1) Restores any created objects.
			// 2) Redo any actions that are data change actions.
			// 3) Fire any PropChanges.
			// 4) Redo any actions that are not data change actions (e.g., selection change actions)

			m_uowService.SuppressSelections = true;
			UndoResult result = UndoResult.kuresSuccess;
			try
			{
				// Do the first pass (Restores any created objects)
				foreach (IUndoAction undoAction in m_changes)
				{
					if (undoAction is IFirstPassRedo)
						((IFirstPassRedo)undoAction).FirstPassRedo();
				}

				// Do the second pass (Redo any actions that are data change actions)
				foreach (IUndoAction undoAction in m_changes)
				{
					if (undoAction.IsDataChange && !undoAction.Redo())
					{
						// TODO: Undo any changes that have been redone
						return UndoResult.kuresFailed;
					}
				}

				try
				{
					// Do the third pass (Fire any PropChanges)
					m_uowService.SendPropChangedNotifications(GetPropChangeInformation(false));
				}
				catch (Exception e)
				{
					Logger.WriteEvent("Exception during PropChanges in Redo");
					Logger.WriteError(e);
					result = UndoResult.kuresRefresh;
				}
			}
			finally
			{
				m_uowService.SuppressSelections = false;
			}

			// Do the fourth pass (Redo any actions that are not data change actions)
			foreach (IUndoAction undoAction in m_changes)
			{
				if (!undoAction.IsDataChange && !undoAction.Redo())
				{
					// TODO: Undo any changes that have been redone
					return UndoResult.kuresFailed;
				}
			}

			return result;
		}

		/// <summary>
		/// Rollback the action(s) in this object.
		/// </summary>
		internal void Rollback()
		{
			UndoRollbackCommon();
		}

		internal void NotePropertyChange(FdoPropertyChangedBase undoAction)
		{
			m_propertyChanges[undoAction] = undoAction;
			AddVerifiedAction(undoAction);
		}

		internal bool TryGetPropertyChange(FdoPropertyChangedBase keyAction, out FdoPropertyChangedBase result)
		{
			return m_propertyChanges.TryGetValue(keyAction, out result);
		}

		internal void RecordCustomFieldDefnChanging(FdoStateChangeCustomFieldDefnModified action)
		{
			if (m_customFieldChanges == null)
				m_customFieldChanges = new List<FdoStateChangeCustomFieldDefnModified>();
			m_customFieldChanges.Add(action);
			AddVerifiedAction(action);
		}

		/// <summary>
		/// Return true if we have already recorded a custom field definition change for this object and flid.
		/// </summary>
		/// <returns></returns>
		internal bool IsFieldForWhichCustomFieldDefnChanged(FdoPropertyChangedBase action)
		{
			if (m_customFieldChanges == null)
				return false;
			return
				m_customFieldChanges.Any(cfc => cfc.Object == action.Object && cfc.ModifiedFlid == action.ModifiedFlid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo the action(s) in this object.
		/// </summary>
		/// <returns>
		/// Cumulative result of undoing all the actions. If we succeeded, then this will
		/// be kuresSuccess.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected UndoResult UndoRollbackCommon()
		{
			// Because of the complexities of our undo/redo system, we need to do the undo
			// in multiple passes:
			// 1) Restores any deleted objects.
			// 2) Undo any actions that are data change actions.
			// 3) Fire any PropChanges.
			// 4) Undo any actions that are not data change actions (e.g., selection change actions)

			m_uowService.SuppressSelections = true;
			UndoResult result = UndoResult.kuresSuccess;

			try
			{
				// Do the first pass (Restores any deleted objects)
				for (int i = m_changes.Count - 1; i >= 0; i--)
				{
					IUndoAction undoAction = m_changes[i];
					if (undoAction is IFirstPassUndo)
						((IFirstPassUndo)undoAction).FirstPassUndo();
				}

				// Do the second pass (Undo any actions that are data change actions)
				for (int i = m_changes.Count - 1; i >= 0; i--)
				{
					if (m_changes[i].IsDataChange && !m_changes[i].Undo())
					{
						// TODO: Redo any changes that have been undone
						return UndoResult.kuresFailed;
					}
				}

				// Do the third pass (Fire any PropChanges)
				// swap cvIns and cvDel fields for each change notification, because we are doing the
				// reverse of the original change
				IEnumerable<ChangeInformation> changes = from change in GetPropChangeInformation(true).Reverse()
					select change.ChangeForUndo;
				// Fire PropChanged calls so display is updated.
				try
				{
					m_uowService.SendPropChangedNotifications(changes);
				}
				catch (Exception e)
				{
					Logger.WriteEvent("Exception during PropChanges in Undo");
					Logger.WriteError(e);
					result = UndoResult.kuresRefresh;
				}
			}
			finally
			{
				m_uowService.SuppressSelections = false;
			}

			// Do the fourth pass (Undo any actions that are not data change actions)
			for (int i = m_changes.Count - 1; i >= 0; i--)
			{
				if (!m_changes[i].IsDataChange && !m_changes[i].Undo())
				{
					// TODO: Redo any changes that have been undone
					return UndoResult.kuresFailed;
				}
			}

			return result;
		}

		/// <summary>
		/// Get the Undo Text for this set of changes.
		/// </summary>
		internal abstract string UndoText
		{
			get;
		}

		/// <summary>
		/// Get the Redo Text for this set of changes.
		/// </summary>
		internal abstract string RedoText
		{
			get;
		}

		/// <summary>
		/// Checks to see if the given object is new.
		/// </summary>
		/// <param name="obj">The object to check</param>
		/// <returns>True, if it is new. Otherwise, it returns false.</returns>
		internal bool IsNew(ICmObject obj)
		{
			return m_newObjects.Contains(obj.Id);
		}

		/// <summary>
		/// Checks to see if the given obejct is modified, but not new and not deleted.
		/// </summary>
		/// <param name="obj">The object to check</param>
		/// <returns>True, if it is simply modified (not new and not deleted). Otherwise, it returns false.</returns>
		internal bool IsModified(ICmObject obj)
		{
			return m_dirtyObjects.Contains((ICmObjectOrSurrogate)obj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether this unit of work has any actual data changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasDataChange
		{
			get { return m_changes.Exists(x => x.IsDataChange); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the actions from another unit of work (merge) into this UOW at the beginning
		/// of this change list. This method handles the deleted/created objects correctly.
		/// </summary>
		/// <param name="uow">The unit of work.</param>
		/// ------------------------------------------------------------------------------------
		internal void InsertActionsFrom(FdoUnitOfWork uow)
		{
			m_changes.InsertRange(0, uow.m_changes);
			foreach (Guid guid in uow.m_newGuids)
				m_newGuids.Add(guid);
			m_newObjects.UnionWith(uow.m_newObjects);
			m_dirtyObjects.UnionWith(uow.m_dirtyObjects);
			m_deletedObjects.UnionWith(uow.m_deletedObjects);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all of the changes that are in this UOW.
		/// WARNING!: This is not a copy of the internal list, so any changes made to the
		/// returned list will be reflected in this UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal List<IUndoAction> Changes {get { return m_changes;}}

		/// <summary>
		/// Add some non-FdoStateChangeBase based IUndoAction.
		/// </summary>
		/// <param name="undoAction"></param>
		internal void AddAction(IUndoAction undoAction)
		{
			if (!(undoAction is FdoStateChangeBase))
			{
				AddVerifiedAction(undoAction);
				return;
			}

			((FdoStateChangeBase)undoAction).AddToUnitOfWork(this);
		}

		/// <summary>
		/// This should ONLY be used by implementations of FdoStateChangeBase.AddToUnitOfWork().
		/// </summary>
		/// <param name="undoAction"></param>
		internal void AddVerifiedAction(IUndoAction undoAction)
		{
			m_changes.Add(undoAction);
		}

		/// <summary>
		/// Register a CmObject as new.
		/// </summary>
		/// <param name="newby"></param>
		internal void RegisterObjectAsCreated(ICmObject newby)
		{
			m_newGuids.Add(newby.Guid);
			m_newObjects.Add(newby.Id);
		}

		/// <summary>
		/// Register a CmObject as modified.
		/// </summary>
		/// <param name="dirtball"></param>
		internal void RegisterObjectAsModified(ICmObject dirtball)
		{
			m_dirtyObjects.Add((ICmObjectOrSurrogate)dirtball);
		}

		/// <summary>
		/// Register a CmObject as deleted.
		/// </summary>
		/// <param name="goner"></param>
		internal void RegisterObjectAsDeleted(ICmObject goner)
		{
			// If it is also created in this UOW, reset the 'after' xml.
			// If the user undoes the change the xml is needed.
			if (m_newGuids.Contains(goner.Guid))
			{
				// Get the creation action handler and reset its xml.
				foreach (var change in m_changes)
				{
					if (!(change is FdoStateChangeObjectCreation)) continue;
					var newby = (FdoStateChangeObjectCreation) change;
					if (newby.Object != goner) continue;

					newby.SetAfterXML();
					break;
				}
			}
			m_deletedObjects.Add(goner.Id);
		}

		/// <summary>
		/// Add the changes which this UOW makes into the three collectors passed in.
		/// Note that this does NOT attempt to eliminate overlap (e.g., objects created in another UOW and modified in this).
		/// </summary>
		internal void GatherChanges(HashSet<ICmObjectId> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			newbies.UnionWith(m_newObjects);
			dirtballs.UnionWith(m_dirtyObjects);
			goners.UnionWith(m_deletedObjects);
		}

		/// <summary>
		/// Overridden in Undoable UOW, this records the state of newly created objects as of the end of the UOW
		/// as an XML string.
		/// </summary>
		internal virtual void SetAfterXml()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of objects that contain property change information (modifications of
		/// some pre-existing object) needed for issuing PropChanged calls. Property changes for
		/// deleted objects will not be included.
		/// </summary>
		/// <param name="fForUndo"><c>true</c> for undo; <c>false</c> for initial change or
		/// subsequent redo</param>
		/// ------------------------------------------------------------------------------------
		internal IEnumerable<ChangeInformation> GetPropChangeInformation(bool fForUndo)
		{
			foreach (IUndoAction change in m_changes)
			{
				IFdoPropertyChanged propChange = change as IFdoPropertyChanged;
				if (propChange == null || propChange.ObjectIsInvalid)
					continue;

				yield return propChange.GetChangeInfo(fForUndo);
			}
		}

		/// <summary>
		/// Determine whether this UOW can safely Undo.
		/// Higher-level methods are responsible for determining whether this UOW conflicts with
		/// those in other stacks. This method is mainly focused on whether referential integrity will be broken.
		/// </summary>
		internal bool CanUndo(ICmObjectRepository objRepo)
		{
			var deletedGuids = new HashSet<Guid>(from obj in m_deletedObjects select obj.Guid);
			if (m_changes.OfType<FdoStateChangeBase>().Any(change => change.CreatesProblemReferenceOnUndo(objRepo, deletedGuids)))
				return false;

			// Now verify that none of the new objects which this action creates will leave a dangling
			// reference if deleted.
			if (m_newObjects.Count == 0)
				return true;
			// If any of the new objects is has already been deleted, we don't have to worry about leaving refs to it!
			var newObjects = new HashSet<ICmObject>(from id in m_newObjects where objRepo.IsValidObjectId(id.Guid)
				select objRepo.GetObject(id));
			int externalIncomingRefs = newObjects.Sum(obj => ((ICmObjectInternal)obj).IncomingRefsNotFrom(newObjects));
			if (externalIncomingRefs == 0)
				return true;
			// OK, there are problem references AFTER the change...will undoing it fix them?
			var newGuids = new HashSet<Guid>(from id in m_newObjects select id.Guid);
			foreach (var item in m_changes)
			{
				var change = item as FdoStateChangeBase;
				if (change != null)
					externalIncomingRefs -= change.CountDeletedRefsToOnUndo(newGuids);
			}
			return externalIncomingRefs == 0;
		}

		/// <summary>
		/// Determine whether this UOW can safely Redo.
		/// Higher-level methods are responsible for determining whether this UOW conflicts with
		/// those in other stacks. This method is mainly focused on whether referential integrity will be broken.
		/// </summary>
		/// <returns></returns>
		internal bool CanRedo(ICmObjectRepository objRepo)
		{
			var newGuids = new HashSet<Guid>(from id in m_newObjects select id.Guid);
			foreach (var item in m_changes)
			{
				if (!item.IsRedoable)
					return false;
				var change = item as FdoStateChangeBase;
				if (change == null)
					continue; // assume other changes are redoable if they think they are
				if (change.CreatesProblemReferenceOnRedo(objRepo, newGuids))
					return false;
			}
			// Now verify that none of the objects which this action deletes will leave a dangling
			// reference if deleted (except references which the action already deletes itself).
			// (We don't need to check objects created by this UOW; they won't get re-created by undoing it.)
			if (m_deletedObjects.Count == 0)
				return true;
			var delObjects = new HashSet<ICmObject>(
				from obj in m_deletedObjects where !newGuids.Contains(obj.Guid) select objRepo.GetObject(obj));
			int externalIncomingRefs = delObjects.Sum(obj => ((ICmObjectInternal)obj).IncomingRefsNotFrom(delObjects));
			if (externalIncomingRefs == 0)
				return true;
			// OK, there are potential problem references after we redo the change...will redoing it fix them?
			var oldGuids = new HashSet<Guid>(from obj in m_deletedObjects select obj.Guid);
			foreach (var item in m_changes)
			{
				var change = item as FdoStateChangeBase;
				if (change != null)
					externalIncomingRefs -= change.CountDeletedRefsToOnRedo(oldGuids);
			}
			return externalIncomingRefs == 0;
		}

		internal void ResetSequenceNumber()
		{
			Sequence = s_sequenceNumber++;
		}
	}

	/// <summary>
	/// This class will handle collections of changes that cannot be undone/redone.
	/// </summary>
	internal sealed class FdoNonUndoableUnitOfWork : FdoUnitOfWork
	{
		public FdoNonUndoableUnitOfWork(UnitOfWorkService uowService) : base(uowService)
		{
		}

		internal override UndoResult Undo()
		{
			return UndoResult.kuresSuccess;
		}

		internal override UndoResult Redo()
		{
			return UndoResult.kuresSuccess;
		}

		/// <summary>
		/// In reconciling changes from other clients, we make a phony UOW and simulate redoing it,
		/// even though (since the changes are already saved elsewhere) it isn't undoable.
		/// This requires bypassing the override of Redo (though that should never be called on one of these
		/// anyway).
		/// </summary>
		internal void SimulateRedo()
		{
			base.Redo();
		}

		/// <summary>
		/// Get the Undo Text for this set of changes.
		/// </summary>
		internal override string UndoText
		{
			get { return Strings.ksNotUndoable; }
		}

		/// <summary>
		/// Get the Redo Text for this set of changes.
		/// </summary>
		internal override string RedoText
		{
			get { return Strings.ksNotRedoable; }
		}
	}

	/// <summary>
	/// This class handles the various IUndoActions,
	/// along with the core Unit of Work stuff with the main CmObjects.
	/// </summary>
	internal sealed class FdoUndoableUnitOfWork : FdoUnitOfWork
	{
		private readonly string m_undoText;
		private readonly string m_redoText;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="uowService">The unit-of-work service.</param>
		/// <param name="undoText">The undo text.</param>
		/// <param name="redoText">The redo text.</param>
		/// ------------------------------------------------------------------------------------
		internal FdoUndoableUnitOfWork(UnitOfWorkService uowService, string undoText, string redoText) : base(uowService)
		{
			if (String.IsNullOrEmpty(undoText))
				throw new ArgumentException("Invalid Undo Text.", "undoText");
			if (String.IsNullOrEmpty(redoText))
				throw new ArgumentException("Invalid Redo Text.", "redoText");

			m_undoText = undoText;
			m_redoText = redoText;
		}

		/// <summary>
		/// Overridden in Undoable UOW, this records the state of newly created objects as of the end of the UOW
		/// as an XML string.
		/// </summary>
		internal override void SetAfterXml()
		{
			base.SetAfterXml();

			// Reset them from the end of the list. (JohnT: tell me why not what!!)
			for (var i = m_changes.Count - 1; i >= 0; i--)
			{
				var action = m_changes[i];
				if (action is FdoStateChangeObjectCreation)
					((FdoStateChangeObjectCreation)action).SetAfterXML();
			}
		}

		/// <summary>
		/// Undo the action(s) in this object.
		/// </summary>
		internal override UndoResult Undo()
		{
			return UndoRollbackCommon();
		}

		/// <summary>
		/// Get the Undo Text for this set of changes.
		/// </summary>
		internal override string UndoText
		{
			get { return m_undoText; }
		}

		/// <summary>
		/// Get the Redo Text for this set of changes.
		/// </summary>
		internal override string RedoText
		{
			get { return m_redoText; }
		}
	}
}
