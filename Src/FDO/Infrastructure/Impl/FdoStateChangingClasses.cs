// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoStateChangingClasses.cs
// Responsibility: Randy Regnier

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	#region Interfaces
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An interface for Undo actions that need something done in a first pass
	/// before all other actions are Undone, such as object deletions.
	/// For example, if the change removes two objects with the same owner, the sequence of actions
	/// will initially be
	/// 1. Remove object A from owner
	/// 2. Delete object A
	/// 3. Remove object B from owner
	/// 4. Delete object B
	/// Action 3 gets merged into action 1, which thus becomes "Remove objects A and B from owner".
	/// Undoing this before we re-create object B will fail, as object B remains invalid.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal interface IFirstPassUndo
	{
		void FirstPassUndo();
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An interface for Undo actions that need something done in a first pass
	/// before all other actions are Redone, such as object creations.
	/// For example, if the change adds two objects with the same owner, the sequence of actions
	/// will initially be
	/// 1. Create object A
	/// 2. Add object A to owner
	/// 3. Create object B
	/// 4. Add object B to owner
	/// Action 4 gets merged into action 2, which thus becomes "Add objects A and B to owner".
	/// Redoing this before we re-create object B will fail, as object B remains invalid.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal interface IFirstPassRedo
	{
		void FirstPassRedo();
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for undo actions which modify a property of an FDO object.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal interface IFdoPropertyChanged : IUndoAction
	{
		/// <summary>
		/// This may be overridden by subclasses that need to know whether the current state is undone
		/// (fForUndo true) or done/Redone (fForUndo false)
		/// </summary>
		ChangeInformation GetChangeInfo(bool fForUndo);

		/// <summary>
		/// Gets a value indicating whether the changed object is (now) deleted or uninitialized.
		/// </summary>
		bool ObjectIsInvalid { get; }
	}
	#endregion

	#region FdoStateChangeBase class
	/// <summary>
	/// Base class for all CmObject IUndoAction classes.
	/// There should be one subclass for each data type in CmObject,
	/// and one for adding a new CmObject, and another for deleting a CmObject.
	/// </summary>
	[ComVisible(true)]
	internal abstract class FdoStateChangeBase : IUndoAction
	{
		/// <summary>
		/// The changed object.
		/// </summary>
		protected ICmObject m_changedObject;
		/// <summary>
		/// The relevant state of the object, before the change.
		/// May be null. Used with non-networked machines.
		/// </summary>
		protected string m_beforeStateXML;
		/// <summary>
		/// The relevant state of the object, after the change.
		/// May be null. Used with non-networked machines.
		/// </summary>
		protected string m_afterStateXML;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="changedObject"></param>
		internal FdoStateChangeBase(ICmObject changedObject)
		{
			m_changedObject = changedObject;
		}

		/// <summary>
		/// Get the changed Object.
		/// </summary>
		[ComVisible(false)]
		public ICmObject Object
		{
			get { return m_changedObject; }
		}

		/// <summary>
		/// Return true if the given guid is a legitimate thing to refer to after Undoing or redoing this change.
		/// That is, it is either in the set (and so will be reinstated by the change) or is already a valid object GUID.
		/// </summary>
		internal bool IsValidReferenceTarget(ICmObjectRepository objRepo, Guid guid, HashSet<Guid> reinstatedObjects)
		{
			if (reinstatedObjects.Contains(guid))
				return true;
			return objRepo.IsValidObjectId(guid);
		}

		/// <summary>
		/// Return true if the XML expresses references to objects that neither exist nor
		/// will exist after the objects represented by reinstatedObjects are re-created.
		/// </summary>
		internal bool XmlContainsProblemReference(ICmObjectRepository objRepo, string xml, HashSet<Guid> reinstatedObjects)
		{
			var match1 = "objsur ";
			var match2 = "t=\"";
			var match3 = "guid=\"";
			var len1 = match1.Length;
			var len2 = match2.Length;
			var len3 = match3.Length;
			for (int ich = 0; ; ) // terminates by Return when no more matches
			{
				ich = xml.IndexOf(match1, ich);
				if (ich < 0)
					return false;
				ich += len1;
				int ichType = xml.IndexOf(match2, ich) + len2;
				if (xml[ichType] != 'r')
				{
					ich = ichType;
					continue;
				}
				// Note that this might be before or after the type flag.
				ich = xml.IndexOf(match3, ich) + len3;
				int ichEnd = xml.IndexOf('"', ich);
				var guid = new Guid(xml.Substring(ich, ichEnd - ich));
				if (!IsValidReferenceTarget(objRepo, guid, reinstatedObjects))
					return true;
				ich = ichEnd;
			}
			// unreachable
		}

		/// <summary>
		/// Do whatever should be done to add this action to the UOW. Sometimes this means modifying
		/// an earlier action that specifies a change to the same property rather than actually adding this one.
		/// This default implementation is suitable for changes, and explores whether the change
		/// can be combined with an existing one.
		/// Actions that create or delete should override.
		/// </summary>
		/// <param name="uow"></param>
		internal abstract void AddToUnitOfWork(FdoUnitOfWork uow);

		#region IUndoAction implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Irreversibly commits an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Commit()
		{ /* Do nothing. */ }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True for most actions, which make changes to data; false for actions that represent
		/// updates to the user interface, like replacing the selection.
		/// </summary>
		/// <returns>this implementation always returns false</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDataChange
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True for most actions, which are redoable; false for actions that aren't, like
		/// Scripture import.
		/// </summary>
		/// <returns>This implementation always returns true</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsRedoable
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns><c>true</c> if successful; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public abstract bool Redo();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets whether this undo action should notify the world that the action has been undone
		/// or redone. For ISqlUndoAction, this supresses the PropChanged notifications.
		/// </summary>
		/// <remarks>This implementation does nothing</remarks>
		/// ------------------------------------------------------------------------------------
		public bool SuppressNotification
		{
			set { /* Do nothing */ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns><c>true</c> if successful; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public abstract bool Undo();

		#endregion IUndoAction implementation

		/// <summary>
		/// Return true if Undoing this change would create an invalid reference, in that the state after the
		/// change would include a reference to an object that neither exists now nor is one of the objects
		/// that the Undo will reinstate.
		/// </summary>
		internal virtual bool CreatesProblemReferenceOnUndo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			return false;
		}
		/// <summary>
		/// Return true if Redoing this change would create an invalid reference, in that the state after the
		/// change would include a reference to an object that neither exists now nor is one of the objects
		/// that the Redo will reinstate.
		/// </summary>
		internal virtual bool CreatesProblemReferenceOnRedo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			return false;
		}

		/// <summary>
		/// Override if undoing this change affects reference properties. Answer how many references
		/// from objects NOT in newObjects to objects that ARE in newObjects will be deleted by undoing the change.
		/// </summary>
		internal virtual int CountDeletedRefsToOnUndo(HashSet<Guid> newObjects)
		{
			return 0;
		}

		/// <summary>
		/// Override if redoing this change affects reference properties. Answer how many references
		/// from objects NOT in newObjects to objects that ARE in newObjects will be deleted by redoing the change.
		/// </summary>
		internal virtual int CountDeletedRefsToOnRedo(HashSet<Guid> newObjects)
		{
			return 0;
		}
	}
	#endregion

	#region FdoStateChangeObjectCreation class
	/// <summary>
	/// Undo Action for newly created CmObjects.
	/// </summary>
	internal class FdoStateChangeObjectCreation : FdoStateChangeBase, IFirstPassRedo
	{
		private readonly int m_previousHvo;
		private readonly FdoCache m_cache;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="newObject"></param>
		internal FdoStateChangeObjectCreation(ICmObject newObject)
			: base(newObject)
		{
			m_cache = newObject.Cache;
			m_previousHvo = newObject.Hvo;
			m_beforeStateXML = null;
			// 'm_afterStateXML' handled in the second big step.
			// The call is to SetAfterXML().
		}

		/// <summary>
		/// Set the 'after' XML.
		/// </summary>
		internal void SetAfterXML()
		{
			var asInternal = (ICmObjectInternal)m_changedObject;
			if (m_changedObject.Hvo > 0) // May have also been deleted, so skip the reset.
				m_afterStateXML = asInternal.ToXmlString();
		}

		/// <summary>
		/// Used when faking one from a foreign surrogate.
		/// </summary>
		/// <param name="xml"></param>
		internal void SetAfterXml(string xml)
		{
			m_afterStateXML = xml;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main body of the Redo is done in FirstPassRedo.
		/// The remaining bit, which is done with the other Redo tasks once all objects are
		/// re-created, is to restore incoming references on things this object refers to.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).RestoreIncomingRefsOnOutgoingRefs();
			return true;
		}

		internal override bool CreatesProblemReferenceOnRedo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			return XmlContainsProblemReference(objRepo, m_afterStateXML, reinstatedObjects);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			// "Un-create".
			((ICmObjectInternal)m_changedObject).ClearIncomingRefsOnOutgoingRefs();
			var identityMap = m_cache.ServiceLocator.GetInstance<IdentityMap>();
			identityMap.UnregisterObject(m_changedObject);
			ObjectRestoreService.ResetBasics(
				identityMap,
				(ICmObjectInternal)m_changedObject,
				null,
				(int)SpecialHVOValues.kHvoUninitializedObject);
			return true;
		}

		internal override void AddToUnitOfWork(FdoUnitOfWork uow)
		{
			// Note: it is tempting to discard ones for new objects whose owner is also new,
			// but we should not do this because we need to unregister the object if its creation
			// is undone, and re-register if the creation is redone.
			uow.AddVerifiedAction(this);
			uow.RegisterObjectAsCreated(Object);
		}

		public void FirstPassRedo()
		{
			// The main work of the Restore is done here.
			var identityMap = m_cache.ServiceLocator.GetInstance<IdentityMap>();
			ObjectRestoreService.ResetFull(
				identityMap,
				(ICmObjectInternal)m_changedObject,
				m_cache,
				m_previousHvo,
				m_afterStateXML);
			identityMap.ReregisterObject(m_changedObject);
		}
	}

	#endregion

	#region FdoStateChangeObjectDeletion class
	/// <summary>
	/// Undo Action for deleted CmObjects.
	/// </summary>
	internal class FdoStateChangeObjectDeletion : FdoStateChangeBase, IFirstPassUndo
	{
		private readonly int m_previousHvo;
		private readonly FdoCache m_cache;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="deletedObject"></param>
		/// <param name="beforeStateXML">Old before State XML (in case it gets undeleted).</param>
		internal FdoStateChangeObjectDeletion(ICmObject deletedObject, string beforeStateXML)
			: base(deletedObject)
		{
			m_previousHvo = deletedObject.Hvo; // Still good at this point.
			m_cache = deletedObject.Cache; // Still good at this point.
			m_beforeStateXML = beforeStateXML;
			// 'm_afterStateXML' is meaningless at this point and cannot be made useful.
			m_afterStateXML = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			// "Delete" again.
			((ICmObjectInternal)m_changedObject).ClearIncomingRefsOnOutgoingRefs();
			var identityMap = m_cache.ServiceLocator.GetInstance<IdentityMap>();
			identityMap.UnregisterObject(m_changedObject);
			ObjectRestoreService.ResetBasics(
				identityMap,
				(ICmObjectInternal)m_changedObject,
				null,
				(int)SpecialHVOValues.kHvoObjectDeleted);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The bulk of Undo is implemented in FirstPassUndo; during the main pass we can safely
		/// restore references.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			// Restore
			((ICmObjectInternal)m_changedObject).RestoreIncomingRefsOnOutgoingRefs();

			return true;
		}

		internal override bool CreatesProblemReferenceOnUndo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			return XmlContainsProblemReference(objRepo, m_beforeStateXML, reinstatedObjects);
		}

		internal override void AddToUnitOfWork(FdoUnitOfWork uow)
		{
			uow.RegisterObjectAsDeleted(Object);
			uow.AddVerifiedAction(this);
		}

		/// <summary>
		/// In the first pass of Undo we undo all the object deletions, restoring the objects so we can
		/// do things like restoring references to them.
		/// </summary>
		public void FirstPassUndo()
		{
			var identityMap = m_cache.ServiceLocator.GetInstance<IdentityMap>();
			ObjectRestoreService.ResetFull(
				identityMap,
				(ICmObjectInternal)m_changedObject,
				m_cache,
				m_previousHvo,
				m_beforeStateXML);
			identityMap.ReregisterObject(m_changedObject);
		}
	}
	#endregion

	#region ObjectRestoreService class
	/// <summary>
	/// This class manages the steps used for Undo and Redo of full object creation and deletion.
	/// </summary>
	internal static class ObjectRestoreService
	{
		/// <summary>
		/// Reset basics for Undo operation of object creation and Redo operation of object deletion.
		/// </summary>
		/// <param name="identityMap"></param>
		/// <param name="obj"></param>
		/// <param name="cache">Null is fine</param>
		/// <param name="newHvo">What to change the HVO to; may be original (if Restoring) or  kHvoUninitializedObject,
		/// if changing to deleted state.</param>
		internal static void ResetBasics(IdentityMap identityMap, ICmObjectInternal obj, FdoCache cache, int newHvo)
		{
			if (identityMap == null) throw new ArgumentNullException("identityMap");
			if (obj == null) throw new ArgumentNullException("obj");
			// 1. Set hvo of obj to restoredHvo
			// 2. Set the Cache of the obj to 'cache', which may be null.
			obj.ResetForUndoRedo(cache, newHvo);

			// Since we do not persist at the end of each UOW,
			// we need not fret about fixing the persisted state.
		}

		/// <summary>
		/// Reset the given 'obj' to whatever is in the xml string.
		/// </summary>
		/// <param name="identityMap"></param>
		/// <param name="obj"></param>
		/// <param name="cache"></param>
		/// <param name="restoredHvo"></param>
		/// <param name="xml"></param>
		internal static void ResetFull(IdentityMap identityMap, ICmObjectInternal obj, FdoCache cache, int restoredHvo, string xml)
		{
			if (identityMap == null) throw new ArgumentNullException("identityMap");
			if (obj == null) throw new ArgumentNullException("obj");
			if (cache == null) throw new ArgumentNullException("cache");
			if (string.IsNullOrEmpty(xml)) throw new ArgumentNullException("xml");

			// 1. Process the xml
			obj.LoadFromDataStore(cache, XElement.Parse(xml), cache.ServiceLocator.GetInstance<LoadingServices>());
			// 2. Reset the cache and the hvo.
			ResetBasics(identityMap, obj, cache, restoredHvo);
		}
	}
	#endregion

	#region FdoPropertyChangedBase class
	/// <summary>
	/// Base class for all CmObject property (of any kind) Undo Actions.
	/// </summary>
	internal abstract class FdoPropertyChangedBase : FdoStateChangeBase, IFdoPropertyChanged
	{
		/// <summary>
		/// The modified flid.
		/// </summary>
		protected int m_modifiedFlid;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		internal FdoPropertyChangedBase(ICmObject modifiedObject, int modifiedFlid)
			: base(modifiedObject)
		{
			m_modifiedFlid = modifiedFlid;
		}

		/// <summary>
		/// Get the Flid that was changed.
		/// </summary>
		internal int ModifiedFlid
		{
			get { return m_modifiedFlid; }
		}

		/// <summary>
		/// This is used in comparing two property changes to see if they affect the 'same' property.
		/// MultiString property changes override to return the actual WS affected; all others use this constant value.
		/// </summary>
		internal virtual int WS
		{
			get { return 0; }
		}

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected abstract ChangeInformation ChangeInfo
		{ get; }

		#region IFdoPropertyChanged members
		/// <summary>
		/// This may be overridden by subclasses that need to know whether the current state is undone
		/// (fForUndo true) or done/Redone (fForUndo false)
		/// </summary>
		public virtual ChangeInformation GetChangeInfo(bool fForUndo)
		{
			return ChangeInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the changed object is (now) deleted or uninitialized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ObjectIsInvalid
		{
			get { return Object.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted ||
						Object.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject; }
		}
		#endregion

		/// <summary>
		/// Update the new state of the object's flid.
		/// There have been more than one such change within one bundle of changes,
		/// but we only need to remember the last one.
		/// </summary>
		/// <param name="newChange"></param>
		internal abstract void UpdateNewState(FdoPropertyChangedBase newChange);

		/// <summary>
		/// Do whatever should be done to add this action to the UOW. Sometimes this means modifying
		/// an earlier action that specifies a change to the same property rather than actually adding this one.
		/// This default implementation is suitable for changes, and explores whether the change
		/// can be combined with an existing one.
		/// Actions that create or delete should override.
		/// </summary>
		internal override void AddToUnitOfWork(FdoUnitOfWork uow)
		{
			if (uow.IsNew(Object))
			{
				// If it's a new object, we don't care about any subsequent modifications, and can ignore
				// this action and not add it.
				return;
			}
			if (uow.IsModified(Object))
			{
				if (uow.IsFieldForWhichCustomFieldDefnChanged(this))
					return; // the custom field itself changed, don't keep records of changes to the instance.
				// Check to see if we already have modified the flid (and, if relevant, WS alternative).
				// If we have, then update the new value on the extant action,
				// and do not add this action.
				FdoPropertyChangedBase earlierModAction;
				if (uow.TryGetPropertyChange(this, out earlierModAction))
				{
					earlierModAction.UpdateNewState(this);
					return;
				}
			}
			else
			{
				// No previous knowledge that this object is modified, record that it is.
				// (but only if it's a real change that needs to be written to the file).
				if(IsDataChange)
					uow.RegisterObjectAsModified(Object);
				else
				{
					// If it's not a data change, the parent object may not be modified,
					// e.g., when deleting a LexEntry and recording a virtual prop change on LexDb.
					// In such a case we still want to suppress multiple change records for the same property.
					FdoPropertyChangedBase earlierModAction;
					if (uow.TryGetPropertyChange(this, out earlierModAction))
					{
						earlierModAction.UpdateNewState(this);
						return;
					}
				}
			}
			// If we haven't returned by now, we don't have a prior action we can modify, so add this one to the UOW.
			uow.NotePropertyChange(this);
		}
	}
	#endregion

	#region FdoVirtualReferenceCollectionChangedAction class

	/// <summary>
	/// Class to support providing ChangeInfo for a changing virtual property.
	/// Enahnce JohnT: With some advantage we could extract from this a base class for real reference collections.
	/// However the Undo/Redo would be more complex. This is enough for now.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class FdoVirtualReferenceCollectionChangedAction<T> : FdoPropertyChangedBase
	{
		private Func<IEnumerable<T>> m_reader;
		private HashSet<T> m_added;
		private HashSet<T> m_removed;
		public FdoVirtualReferenceCollectionChangedAction(ICmObject modifiedObject, int modifiedFlid,
			Func<IEnumerable<T>> reader, IEnumerable<T> added, IEnumerable<T> removed)
			: base(modifiedObject, modifiedFlid)
		{
			m_reader = reader;
			m_added = new HashSet<T>(added);
			m_removed = new HashSet<T>(removed);
		}

		/// <summary>
		/// No real changes, this is for a virtual.
		/// </summary>
		public override bool Undo()
		{
			return true;
		}

		/// <summary>
		/// No real changes, this is for a virtual.
		/// </summary>
		public override bool Redo()
		{
			return true;
		}

		/// <summary>
		/// This change is NOT a true data change, which requires the object to be written.
		/// </summary>
		public override bool IsDataChange
		{
			get { return false; }
		}

		/// <summary>
		/// Override this so that (a) our unimplemented ChangeInfo property won't be called, and
		/// (b) to generate the required ChangeInfo from our added and removed objects.
		/// </summary>
		public override ChangeInformation GetChangeInfo(bool fForUndo)
		{
			var result = new List<T>(m_reader());
			// Special cases: adding or deleting just one object. We want these to go out as a single item change if possible.
			// If we are redoing an insertion or undoing a deletion, the current value contains the item so we can
			// be precise about where it was inserted.
			if (m_added.Count == 1 && m_removed.Count == 0 && !fForUndo)
			{
				var inserted = m_added.ToArray()[0];
				int index = result.IndexOf(inserted);
				return new ChangeInformation(Object, ModifiedFlid, index, 1, 0);
			}
			if (m_added.Count == 0 && m_removed.Count == 1 && fForUndo)
			{
				var removed = m_removed.ToArray()[0];
				int index = result.IndexOf(removed);
				return new ChangeInformation(Object, ModifiedFlid, index, 1, 0);
			}
			// If we're undoing, the change is to insert the removed objects and delete the added ones.
			// We depend on the fact that all (real) changes have been made by the time we collect change info.
			// Thus, if we're undoing, result is the 'Undone' value. That is, it includes the removed
			// but not the added objects. We want to make a propchanged where exactly those current objects
			// claim to have been (all) inserted, while the number deleted is the number when the change is
			// done, that is, the current result minus those removed plus those added.
			// If this is for Redo, the current value is the 'Redone' one. In that case the length of the
			// other value is found by removing the added objects and adding the removed ones!
			// The current value includes the added ones; we pretend the change will remove all of these.
			// It's safest to regard the whole property as changed, however.
			if (fForUndo)
				return new ChangeInformation(Object, ModifiedFlid, 0, result.Count, result.Count - m_removed.Count + m_added.Count);
			else
				return new ChangeInformation(Object, ModifiedFlid, 0, result.Count, result.Count + m_removed.Count - m_added.Count);
		}

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Update the new state of the object's flid.
		/// There have been more than one such change within one bundle of changes,
		/// but we only need to remember the net effect.
		/// </summary>
		/// <param name="newChange"></param>
		internal override void UpdateNewState(FdoPropertyChangedBase newChange)
		{
			var update = (FdoVirtualReferenceCollectionChangedAction<T>)newChange;
			// Something is a real addtion if the new change says it was added, unless the old change
			// removed it. If the old change removed it, adding it back will just remove it from the removed set.
			var reallyAdded = update.m_added.Except(m_removed);
			// Something is a real removal if the new change says it was removed, unless the old change
			// added it. If the old change added it, removing it again will just remove it from the added set.
			var reallyRemoved = update.m_removed.Except(m_added);
			// Anything previously added and subsequently removed is no longer added.
			m_added.ExceptWith(update.m_removed);
			// Anything previously removed but now added back is no longer removed.
			m_removed.ExceptWith(update.m_added);
			// Finally put in any real additions and removals
			m_added.UnionWith(reallyAdded);
			m_removed.UnionWith(reallyRemoved);
		}
	}
	#endregion

	#region FdoPropertyChangedAction class
	/// <summary>
	/// Generic class for all CmObject property (of any kind) Undo Actions.
	/// </summary>
	/// <remarks>
	/// There still have to be subclasses, so the Undo/Redo methods can get the values as the right object.
	/// </remarks>
	internal abstract class FdoPropertyChangedAction<T> : FdoPropertyChangedBase
	{
		/// <summary>The original value.</summary>
		protected T m_originalValue;
		/// <summary>The new value.</summary>
		protected T m_newValue;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoPropertyChangedAction(ICmObject modifiedObject, int modifiedFlid, T originalValue, T newValue)
			: base(modifiedObject, modifiedFlid)
		{
			m_originalValue = originalValue;
			m_newValue = newValue;
		}

		/// <summary>
		/// Update the new state of the object's flid.
		/// There have been more than one such change within one bundle of changes,
		/// but we only need to remember the last one.
		/// </summary>
		/// <param name="newChange"></param>
		internal override void UpdateNewState(FdoPropertyChangedBase newChange)
		{
			m_newValue = ((FdoPropertyChangedAction<T>) newChange).m_newValue;
		}
	}
	#endregion

	#region FdoOwnerChanged class
	/// <summary>
	/// Supports Undo/Redo for modified owner when change is the result of moving object from
	/// one vector to another.
	/// </summary>
	internal class FdoOwnerChanged : FdoStateChangeBase
	{
		private Guid m_originalOwner;
		private int m_originalFlid;
		private Guid m_newOwner;
		private int m_newFlid;

		internal FdoOwnerChanged(ICmObject modifiedObject, Guid originalOwner, int originalFlid, Guid newOwner, int newFlid) : base(modifiedObject)
		{
			m_originalOwner = originalOwner;
			m_originalFlid = originalFlid;
			m_newOwner = newOwner;
			m_newFlid = newFlid;
		}

		internal override void AddToUnitOfWork(FdoUnitOfWork uow)
		{
			if (!uow.IsNew(Object) && !uow.IsModified(Object))
			{
				// No previous knowledge that this object is modified, record that it is.
				uow.RegisterObjectAsModified(Object);
			}
			if (uow.IsModified(Object))
				uow.AddVerifiedAction(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			var owner = Object.Cache.ServiceLocator.ObjectRepository.GetObject(m_newOwner);
			((ICmObjectInternal)Object).SetOwnerForUndoRedo(owner, m_newFlid, -1);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			var owner = Object.Cache.ServiceLocator.ObjectRepository.GetObject(m_originalOwner);
			((ICmObjectInternal)Object).SetOwnerForUndoRedo(owner, m_originalFlid, -1);
			return true;
		}
	}
	#endregion

	#region FdoVectorPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified vector properties.
	/// This can be a sequence or collection property, whether owning or reference.
	/// </summary>
	internal class FdoVectorPropertyChanged : FdoPropertyChangedAction<Guid[]>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoVectorPropertyChanged(ICmObject modifiedObject, int modifiedFlid, Guid[] originalValue, Guid[] newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		bool IsReferenceProp
		{
			get
			{
				var fieldType = m_changedObject.Services.MetaDataCache.GetFieldType(m_modifiedFlid);
				return fieldType == (int)CellarPropertyType.ReferenceSequence ||
					   fieldType == (int)CellarPropertyType.ReferenceCollection;
			}
		}

		internal Guid[] OriginalGuids { get { return m_originalValue; }  set { m_originalValue = value;}}
		internal Guid[] NewGuids { get { return m_newValue; } set { m_newValue = value;} }

		/// <summary>
		/// Override if undoing this change affects reference properties. Answer how many references
		/// from objects NOT in newObjects to objects that ARE in newObjects will be deleted by undoing the change.
		/// </summary>
		internal override int CountDeletedRefsToOnUndo(HashSet<Guid> newObjects)
		{
			// doesn't count if source is in set, deleted or property is not reference.
			if (newObjects.Contains(m_changedObject.Guid) || m_changedObject.Cache == null || !IsReferenceProp)
				return 0;
			// How many more things in the set do we refer to now than we will if undone?
			return m_newValue.Where(guid => newObjects.Contains(guid) && !m_originalValue.Contains(guid)).Count();
		}

		/// <summary>
		/// Override if redoing this change affects reference properties. Answer how many references
		/// from objects NOT in newObjects to objects that ARE in newObjects will be deleted by redoing the change.
		/// </summary>
		internal override int CountDeletedRefsToOnRedo(HashSet<Guid> newObjects)
		{
			// doesn't count if source is in set or property is not reference.
			if (newObjects.Contains(m_changedObject.Guid) || !IsReferenceProp)
				return 0;
			// How many more things in the set do we refer to now than we will if redone?
			return m_originalValue.Where(guid => newObjects.Contains(guid) && !m_newValue.Contains(guid)).Count();
		}

		internal override bool CreatesProblemReferenceOnUndo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			foreach (var guid in m_originalValue)
			{
				if (!IsValidReferenceTarget(objRepo, guid, reinstatedObjects))
					return true;
			}
			return false;
		}

		internal override bool CreatesProblemReferenceOnRedo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			foreach (var guid in m_newValue)
			{
				if (!IsValidReferenceTarget(objRepo, guid, reinstatedObjects))
					return true;
			}
			return false;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				// 1. null->new (all added)
				return ComputeChanges(m_originalValue);
			}
		}

		internal ChangeInformation ComputeChanges(Guid[] originalValue)
		{
			if (originalValue.Length == 0 && m_newValue.Length > 0)
				return new ChangeInformation(m_changedObject, m_modifiedFlid, 0, m_newValue.Length, 0);
			// 2. old->null (all removed)
			if (originalValue.Length > 0 && m_newValue.Length == 0)
				return new ChangeInformation(m_changedObject, m_modifiedFlid, 0, 0, originalValue.Length);
			// 3. old->new (some added or removed)
			int ivMin = 0;
			if (originalValue.Length > m_newValue.Length)
			{
				// This becomes the lim in m_newValue of the range that changed. If nothing is
				// different in the new value it will stay the length of the new value.
				var ivLimNewChange = m_newValue.Length;
				int deltaNew = originalValue.Length - m_newValue.Length;
				// Loop through new vals, since it is shorter
				for (var i = 0; i < m_newValue.Length; ++i)
				{
					if (originalValue[i] == m_newValue[i])
					{
						++ivMin;
					}
					else
					{
						// Different at ivMin.
						for (;
							ivLimNewChange > i && originalValue[ivLimNewChange + deltaNew - 1] == m_newValue[ivLimNewChange - 1];
							ivLimNewChange--)
						{
						}
						break;
					}
				}
				return new ChangeInformation(m_changedObject, m_modifiedFlid,
					ivMin, ivLimNewChange - ivMin,
					ivLimNewChange - ivMin + deltaNew);
			}

			// This becomes the lim in originalValue of the range that changed. If nothing is
			// different in the original it will stay the length of the original.
			var ivLimChange = originalValue.Length;
			int delta = m_newValue.Length - originalValue.Length;
			//originalValue.Length == m_newValue.Length || originalValue.Length < m_newValue.Length)
			// Loop through original vals, since it is the same length, or shorter
			for (var i = 0; i < originalValue.Length; ++i)
			{
				if (originalValue[i] == m_newValue[i])
				{
					++ivMin;
				}
				else
				{
					// Different at ivMin.
					for (;
						ivLimChange > i && originalValue[ivLimChange - 1] == m_newValue[ivLimChange + delta - 1];
						ivLimChange--)
					{
					}
					break;
				}
			}
			return new ChangeInformation(m_changedObject, m_modifiedFlid,
				ivMin, ivLimChange - ivMin + delta, ivLimChange - ivMin);
		}

		#endregion
	}
	#endregion

	#region FdoVectorVirtualChanged class
	/// <summary>
	/// This one is used for virtual property changes. These don't require Undoing or Redoing, and aren't considered data changes,
	/// but a PropChanged is needed.
	///
	/// Usually when we create an instance of this class, we cannot reliably determine the old value, since it would have to be computed
	/// based on the state of other properties that have already changed. Thus, usually an empty collection is passed as the old value.
	/// This works for 'do' and 'redo' since our display code for vectors ignores the old value if it can determine that the 'number added'
	/// to the collection is in fact the whole new value. For Undo, we have the correct 'old' value (which is the 'new' value computed at the
	/// time the change was made), and can compute the value we are changing TO (the 'undo' value) at the time of the Undo, since at that time
	/// it has become the current value.
	/// </summary>
	internal class FdoVectorVirtualChanged : FdoVectorPropertyChanged
	{
		internal FdoVectorVirtualChanged(ICmObject modifiedObject, int modifiedFlid, Guid[] originalValue, Guid[] newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No-op.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No-op.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			return true;
		}

		/// <summary>
		/// For this count we're only interested in real properties.
		/// </summary>
		internal override int CountDeletedRefsToOnRedo(HashSet<Guid> newObjects)
		{
			return 0;
		}

		/// <summary>
		/// For this count we're only interested in real properties.
		/// </summary>
		internal override int CountDeletedRefsToOnUndo(HashSet<Guid> newObjects)
		{
			return 0;
		}

		/// <summary>
		/// This change is NOT a true data change, which requires the object to be written.
		/// </summary>
		public override bool IsDataChange
		{
			get { return false; }
		}

		/// <summary>
		/// Changes to virtuals are never a problem.
		/// </summary>
		internal override bool CreatesProblemReferenceOnRedo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			return false;
		}

		/// <summary>
		/// Changes to virtuals are never a problem.
		/// </summary>
		internal override bool CreatesProblemReferenceOnUndo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			return false;
		}

		/// <summary>
		/// We override this to verify that at least the current value of the property is
		/// correct.
		/// When we originally create a virtual Undo vector, we compute the value of the property, and pass it as the new value.
		/// This should be correct! OTOH, we generally don't have any reliable way to compute the old value. Thus, the ChangeInfo we generate for Redo
		/// (or for the original Do) has a correct new value and doubtful old value. The ChangeInfo we generate for Undo has a correct old value but a
		/// doubtful NEW value! That is a problem...but when we are Undoing, we've already undone the data changes, so we can compute the CURRENT value
		/// as a correct new value.
		/// </summary>
		/// <param name="fForUndo"></param>
		/// <returns></returns>
		public override ChangeInformation GetChangeInfo(bool fForUndo)
		{
			if (!fForUndo)
				return base.ChangeInfo; // can't do better than original Do code (see method comment).
			var currentHvos = ((ISilDataAccessManaged)m_changedObject.Cache.DomainDataByFlid).VecProp(m_changedObject.Hvo, m_modifiedFlid);
			var repo = m_changedObject.Services.ObjectRepository;
			var trueOrginalValue = (from hvo in currentHvos select repo.GetObject(hvo).Guid).ToArray();
			return ComputeChanges(trueOrginalValue);
		}
	}
	#endregion

	#region FdoGuidPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified Guids.
	/// This can be a regular Guid property, or for an atomic owning/reference property.
	/// </summary>
	internal class FdoGuidPropertyChanged : FdoPropertyChangedAction<Guid>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoGuidPropertyChanged(ICmObject modifiedObject, int modifiedFlid, Guid originalValue, Guid newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			// This can be a regular Guid property, or an atomic owning/ref property,
			// so use the right method to Undo it.
			var propType = (CellarPropertyType)m_changedObject.Cache.DomainDataByFlid.MetaDataCache.GetFieldType(ModifiedFlid);
			if (propType == CellarPropertyType.Guid)
				((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			else
			{
				((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid,
					(m_originalValue == Guid.Empty) ? null : m_changedObject.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_originalValue),
					false);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			// This can be a regular Guid property, or an atomic owning/ref property,
			// so use the right method to Undo it.
			var propType = (CellarPropertyType)m_changedObject.Cache.DomainDataByFlid.MetaDataCache.GetFieldType(ModifiedFlid);
			if (propType == CellarPropertyType.Guid)
				((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			else
			{
				((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid,
					(m_newValue == Guid.Empty) ? null : m_changedObject.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_newValue),
					false);
			}
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				var flidType =
					(CellarPropertyType)Object.Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>().GetFieldType(ModifiedFlid);
				if (flidType == CellarPropertyType.Guid)
					return new ChangeInformation(Object, ModifiedFlid, 0, 0, 0);

				// Atomic Object property (Owning and Reference are treated the same.)
				int cvIns;
				int cvDel;
				if ((m_originalValue == Guid.Empty) && (m_newValue != Guid.Empty))
				{
					// Inserted
					cvIns = 1;
					cvDel = 0;
				}
				else if ((m_originalValue != Guid.Empty) && (m_newValue == Guid.Empty))
				{
					// Deleted
					cvIns = 0;
					cvDel = 1;
				}
				else
				{
					// Replaced.
					cvIns = 1;
					cvDel = 1;
				}
				return new ChangeInformation(Object, ModifiedFlid, 0, cvIns, cvDel);
			}
		}

		#endregion
	}
	#endregion

	#region FdoAtomicRefPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified Refernce atomic properties.
	/// </summary>
	internal class FdoAtomicRefPropertyChanged : FdoGuidPropertyChanged
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoAtomicRefPropertyChanged(ICmObject modifiedObject, int modifiedFlid, Guid originalValue, Guid newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		/// Override if undoing this change affects reference properties. Answer how many references
		/// from objects NOT in newObjects to objects that ARE in newObjects will be deleted by undoing the change.
		/// </summary>
		internal override int CountDeletedRefsToOnUndo(HashSet<Guid> newObjects)
		{
			if (newObjects.Contains(m_changedObject.Guid))
				return 0; // any refernce IS from an object in newObjects, it doesn't count
			if (newObjects.Contains(m_newValue) && !newObjects.Contains(m_originalValue))
				return 1;
			return 0;
		}
		/// <summary>
		/// Override if redoing this change affects reference properties. Answer how many references
		/// from objects NOT in newObjects to objects that ARE in newObjects will be deleted by redoing the change.
		/// </summary>
		internal override int CountDeletedRefsToOnRedo(HashSet<Guid> newObjects)
		{
			if (newObjects.Contains(m_changedObject.Guid))
				return 0; // any refernce IS from an object in newObjects, it doesn't count
			if (newObjects.Contains(m_originalValue) && !newObjects.Contains(m_newValue))
				return 1;
			return 0;
		}


		/// <summary>
		/// Return true if Undoing this change would create an invalid reference, in that the state after the
		/// change would include a reference to an object that neither exists now nor is one of the objects
		/// that the Undo will reinstate.
		/// </summary>
		internal override bool CreatesProblemReferenceOnUndo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			if (m_originalValue == Guid.Empty)
				return false;
			return !IsValidReferenceTarget(objRepo, m_originalValue, reinstatedObjects);
		}

		/// <summary>
		/// Return true if Redoing this change would create an invalid reference, in that the state after the
		/// change would include a reference to an object that neither exists now nor is one of the objects
		/// that the Redo will reinstate.
		/// </summary>
		internal override bool CreatesProblemReferenceOnRedo(ICmObjectRepository objRepo, HashSet<Guid> reinstatedObjects)
		{
			if (m_newValue == Guid.Empty)
				return false;
			return !IsValidReferenceTarget(objRepo, m_newValue, reinstatedObjects);
		}
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Undo()
		{
			var originalValue = (m_originalValue == Guid.Empty)
									? null
									: m_changedObject.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(
										m_originalValue);
			var newValue = (m_newValue == Guid.Empty)
									? null
									: m_changedObject.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(
										m_newValue);
			((ICmObjectInternal) m_changedObject).SetProperty(ModifiedFlid, originalValue, false);
			// Setting it back to the original value, that gains an incoming reference, while the 'new' one loses one.
			if (originalValue is ICmObjectInternal)
				((ICmObjectInternal) originalValue).AddIncomingRef((IReferenceSource) m_changedObject);
			if (newValue is ICmObjectInternal)
				((ICmObjectInternal)newValue).RemoveIncomingRef((IReferenceSource)m_changedObject);

			return true;
		}
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Redo()
		{
			var originalValue = (m_originalValue == Guid.Empty)
									? null
									: m_changedObject.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(
										m_originalValue);
			var newValue = (m_newValue == Guid.Empty)
									? null
									: m_changedObject.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(
										m_newValue);
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, newValue, false);
			// Setting it to the new value, that gains an incoming reference, while the original one loses one.
			if (originalValue is ICmObjectInternal)
				((ICmObjectInternal)originalValue).RemoveIncomingRef((IReferenceSource)m_changedObject);
			if (newValue is ICmObjectInternal)
				((ICmObjectInternal)newValue).AddIncomingRef((IReferenceSource)m_changedObject);

			return true;
		}
	}
	#endregion

	#region FdoTsStringPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified ITsString strings.
	/// </summary>
	internal class FdoTsStringPropertyChanged : FdoPropertyChangedAction<ITsString>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoTsStringPropertyChanged(ICmObject modifiedObject, int modifiedFlid, ITsString originalValue, ITsString newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				if (m_originalValue == null && m_newValue != null)
					return new ChangeInformation(m_changedObject, m_modifiedFlid, 0, m_newValue.Length, 0);

				if (m_originalValue != null && m_newValue != null)
					return new ChangeInformation(m_changedObject, m_modifiedFlid, 0, m_newValue.Length,
												 m_originalValue.Length);

				// Old value was not null, but new value is null
				return new ChangeInformation(m_changedObject, m_modifiedFlid, 0, 0, m_originalValue.Length);
			}
		}

		#endregion
	}
	#endregion

	#region FdoTsStringVirtualChanged class
	/// <summary>
	/// This one is used for virtual property changes. These don't require Undoing or Redoing, and aren't considered data changes,
	/// but a PropChanged is needed.
	/// </summary>
	internal class FdoTsStringVirtualChanged : FdoTsStringPropertyChanged
	{
		internal FdoTsStringVirtualChanged(ICmObject modifiedObject, int modifiedFlid, ITsString originalValue, ITsString newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		public override bool Undo()
		{
			return true;
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		public override bool Redo()
		{
			return true;
		}

		/// <summary>
		/// This change is NOT a true data change, which requires the object to be written.
		/// </summary>
		public override bool IsDataChange
		{
			get { return false; }
		}
	}
	#endregion

	#region FdoMultiTsStringPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified multi-ITsString properties.
	/// </summary>
	internal class FdoMultiTsStringPropertyChanged : FdoPropertyChangedAction<ITsString>
	{
		private readonly int m_ws;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="ws"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoMultiTsStringPropertyChanged(ICmObject modifiedObject, int modifiedFlid, int ws, ITsString originalValue, ITsString newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
			m_ws = ws;
		}

		/// <summary>
		/// Get the WS HVO.
		/// </summary>
		internal override int WS
		{
			get { return m_ws; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			((IMultiAccessorInternal)((ICmObjectInternal)m_changedObject).GetITsMultiStringProperty(ModifiedFlid)).SetAltQuietly(m_ws, m_originalValue);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			((IMultiAccessorInternal)((ICmObjectInternal)m_changedObject).GetITsMultiStringProperty(ModifiedFlid)).SetAltQuietly(m_ws, m_newValue);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get { return new ChangeInformation(m_changedObject, ModifiedFlid, m_ws, 0, 0); }
		}

		#endregion
	}
	#endregion

	#region FdoMultiTsStringVirtualChanged class
	/// <summary>
	/// This one is used for virtual property changes. These don't require Undoing or Redoing, and aren't considered data changes,
	/// but a PropChanged is needed.
	/// </summary>
	internal class FdoMultiTsStringVirtualChanged : FdoMultiTsStringPropertyChanged
	{
		internal FdoMultiTsStringVirtualChanged(ICmObject modifiedObject, int modifiedFlid, int ws, ITsString originalValue, ITsString newValue)
			: base(modifiedObject, modifiedFlid, ws, originalValue, newValue)
		{
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		public override bool Undo()
		{
			return true;
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		public override bool Redo()
		{
			return true;
		}

		/// <summary>
		/// This change is NOT a true data change, which requires the object to be written.
		/// </summary>
		public override bool IsDataChange
		{
			get { return false; }
		}
	}
	#endregion

	#region FdoTsTextPropsPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified ITsTextProps properties.
	/// </summary>
	internal class FdoTsTextPropsPropertyChanged : FdoPropertyChangedAction<ITsTextProps>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoTsTextPropsPropertyChanged(ICmObject modifiedObject, int modifiedFlid, ITsTextProps originalValue, ITsTextProps newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get { return new ChangeInformation(m_changedObject, ModifiedFlid, 0, 0, 0); }
		}

		#endregion
	}
	#endregion

	#region FdoStringPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified string (Unicode) properties.
	/// </summary>
	internal class FdoStringPropertyChanged : FdoPropertyChangedAction<string>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoStringPropertyChanged(ICmObject modifiedObject, int modifiedFlid, string originalValue, string newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				if (m_originalValue == null && m_newValue != null)
					return new ChangeInformation(m_changedObject, ModifiedFlid, 0, m_newValue.Length, 0);

				if (m_originalValue != null && m_newValue != null)
					return new ChangeInformation(m_changedObject, ModifiedFlid, 0, m_newValue.Length,
												 m_originalValue.Length);

				// Must have been set to something and reset to null in one UOW.
				if (m_originalValue == null && m_newValue == null)
					return new ChangeInformation(m_changedObject, ModifiedFlid, 0, 0, 0);

				// Old value was not null, but new value is null
				return new ChangeInformation(m_changedObject, ModifiedFlid, 0, 0, m_originalValue.Length);
			}
		}

		#endregion
	}
	#endregion

	#region FdoStringVirtualChanged class
	/// <summary>
	/// This one is used for virtual property changes. These don't require Undoing or Redoing, and aren't considered data changes,
	/// but a PropChanged is needed.
	/// </summary>
	internal class FdoStringVirtualChanged : FdoStringPropertyChanged
	{
		internal FdoStringVirtualChanged(ICmObject modifiedObject, int modifiedFlid, string originalValue, string newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		public override bool Undo()
		{
			return true;
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		public override bool Redo()
		{
			return true;
		}

		/// <summary>
		/// This change is NOT a true data change, which requires the object to be written.
		/// </summary>
		public override bool IsDataChange
		{
			get { return false; }
		}
	}
	#endregion

	#region FdoBooleanPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified boolean properties.
	/// </summary>
	internal class FdoBooleanPropertyChanged : FdoPropertyChangedAction<bool>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoBooleanPropertyChanged(ICmObject modifiedObject, int modifiedFlid, bool originalValue, bool newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				return new ChangeInformation(m_changedObject, ModifiedFlid,
											 0, 0, 0);
			}
		}

		#endregion
	}
	#endregion

	#region FdoBooleanVirtualChanged class
	/// <summary>
	/// This one is used for virtual property changes. These don't require Undoing or Redoing, and aren't considered data changes,
	/// but a PropChanged is needed.
	/// </summary>
	internal class FdoBooleanVirtualChanged : FdoBooleanPropertyChanged
	{
		internal FdoBooleanVirtualChanged(ICmObject modifiedObject, int modifiedFlid, bool originalValue, bool newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		public override bool Undo()
		{
			return true;
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		public override bool Redo()
		{
			return true;
		}

		/// <summary>
		/// This change is NOT a true data change, which requires the object to be written.
		/// </summary>
		public override bool IsDataChange
		{
			get { return false; }
		}
	}
	#endregion

	#region FdoDateTimePropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified DateTime properties.
	/// </summary>
	internal class FdoDateTimePropertyChanged : FdoPropertyChangedAction<DateTime>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoDateTimePropertyChanged(ICmObject modifiedObject, int modifiedFlid, DateTime originalValue, DateTime newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		internal DateTime OldTime
		{
			get { return m_originalValue; }
			set
			{
				m_originalValue = value;
			}
		}
		internal DateTime NewTime { get { return m_newValue; }  set { m_newValue = value;}}

		internal override void AddToUnitOfWork(FdoUnitOfWork uow)
		{
			base.AddToUnitOfWork(uow);
			if (m_changedObject.Cache.MetaDataCache.GetFieldName(m_modifiedFlid) == "DateModified")
			{
				uow.AddDateModifiedObject(m_changedObject);
			}
		}
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				return new ChangeInformation(m_changedObject, ModifiedFlid, 0, 0, 0);
			}
		}

		#endregion
	}
	#endregion

	#region FdoIntegerPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified GenDate and integer properties.
	/// </summary>
	internal class FdoIntegerPropertyChanged : FdoPropertyChangedAction<int>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoIntegerPropertyChanged(ICmObject modifiedObject, int modifiedFlid, int originalValue, int newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				return new ChangeInformation(m_changedObject, ModifiedFlid, 0, 0, 0);
			}
		}

		#endregion
	}
	#endregion

	#region FdoIntegerVirtualChanged class
	/// <summary>
	/// This one is used for virtual property changes. These don't require Undoing or Redoing, and aren't considered data changes,
	/// but a PropChanged is needed.
	/// </summary>
	internal class FdoIntegerVirtualChanged : FdoIntegerPropertyChanged
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoIntegerVirtualChanged(ICmObject modifiedObject, int modifiedFlid, int originalValue, int newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		/// <returns></returns>
		public override bool Undo()
		{
			return true;
		}

		/// <summary>
		/// Nothing should be done.
		/// </summary>
		/// <returns></returns>
		public override bool Redo()
		{
			return true;
		}

		/// <summary>
		/// This change is NOT a true data change, which requires the object to be written.
		/// </summary>
		public override bool IsDataChange
		{
			get { return false; }
		}
	}
	#endregion

	#region FdoBinaryPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified Binary properties.
	/// </summary>
	internal class FdoBinaryPropertyChanged : FdoPropertyChangedAction<byte[]>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoBinaryPropertyChanged(ICmObject modifiedObject, int modifiedFlid, byte[] originalValue, byte[] newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				return new ChangeInformation(m_changedObject, m_modifiedFlid,
											 0, 0, 0);
			}
		}

		#endregion
	}
	#endregion

	#region FdoCustomPropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified custom properties.
	/// </summary>
	internal class FdoCustomPropertyChanged : FdoPropertyChangedAction<object>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoCustomPropertyChanged(ICmObject modifiedObject, int modifiedFlid, object originalValue, object newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				return new ChangeInformation(m_changedObject, m_modifiedFlid,
											 0, 0, 0);
			}
		}

		#endregion
	}
	#endregion

	#region FdoGenDatePropertyChanged class
	/// <summary>
	/// Supports Undo/Redo for modified GenDate properties.
	/// </summary>
	internal class FdoGenDatePropertyChanged : FdoPropertyChangedAction<GenDate>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifiedObject"></param>
		/// <param name="modifiedFlid"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		internal FdoGenDatePropertyChanged(ICmObject modifiedObject, int modifiedFlid, GenDate originalValue, GenDate newValue)
			: base(modifiedObject, modifiedFlid, originalValue, newValue)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Undo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_originalValue, false);
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override bool Redo()
		{
			((ICmObjectInternal)m_changedObject).SetProperty(ModifiedFlid, m_newValue, false);
			return true;
		}

		#region Overrides of FdoPropertyChangedBase

		/// <summary>
		/// Get the ChangeInformation used to do PropChanges.
		/// </summary>
		protected override ChangeInformation ChangeInfo
		{
			get
			{
				return new ChangeInformation(m_changedObject, m_modifiedFlid, 0, 0, 0);
			}
		}

		#endregion
	}
	#endregion

	#region FdoStateChangeCustomFieldDefnModified class
	/// <summary>
	/// This state change is used to indicate that an object has been modified
	/// because it contains data for a custom field that was modified (that is, the definition of the custom field changed).
	/// </summary>
	internal class FdoStateChangeCustomFieldDefnModified : FdoStateChangeBase
	{
		public FdoStateChangeCustomFieldDefnModified(ICmObject changedObject, int modifiedFlid)
			: base(changedObject)
		{
			ModifiedFlid = modifiedFlid;
		}

		public int ModifiedFlid
		{
			get; private set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if successful; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			return true;
		}

		internal override void AddToUnitOfWork(FdoUnitOfWork uow)
		{
			if (uow.IsModified(Object))
			{
				// Check to see if we already have modified the flid.
				// If we have, remove any prior property change actions for this flid
				// It is tempting to use uow.TryGetPropertyChange(), but if it is a multistring, there could be
				// more than one change for the flid, and we want to remove them all. This is rare enough not
				// to worry about performance.
				for (int i = uow.Changes.Count - 1; i >= 0; i--)
				{
					var ua = uow.Changes[i] as FdoPropertyChangedBase;
					if (ua != null && ua.Object == Object && ua.ModifiedFlid == ModifiedFlid)
						uow.Changes.RemoveAt(i);
				}
			}
			else
			{
				uow.RegisterObjectAsModified(Object);
			}
			uow.RecordCustomFieldDefnChanging(this);
		}
	}
	#endregion
}
