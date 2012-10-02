using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This class is responsible to compare the (unsaved) changes in the current UOW with a set of changes made
	/// (and saved) by another client, to determine whether it is safe to save the current changes (they don't
	/// conflict with the ones saved elsewhere), and if so, to adjust the state of the UOW manager so that
	/// the changes can proceed.
	///
	/// This change usually consists of inserting a non-undoable unit of work into
	/// the non-undoable stack just before the first unsaved change, one which makes the same changes as have
	/// in fact been made (and can't be undone) in the remote system.
	///
	/// In a few cases, where we handle changes made to the same property of the same object, the reconciler
	/// also adjusts the 'before' state of unsaved UOWs so that undoing them will restore the state saved
	/// elsewhere rather than the state that actually existed before the change was made.
	///
	/// For example, if the other client inserted a lexical entry, and we did too, then the non-undoable change
	/// will insert it, and our change to the LexDb will be modified to indiate a 'before' state that includes
	/// that entry.
	/// </summary>
	class ChangeReconciler : IReconcileChanges
	{
		private UnitOfWorkService UowService;
		private List<ICmObjectSurrogate> m_foreignNewbies;
		private List<ICmObjectSurrogate> m_foreignDirtballs;
		private List<ICmObjectId> m_foreignGoners;

		public ChangeReconciler(UnitOfWorkService uowService, List<ICmObjectSurrogate> foreignNewbies,
			List<ICmObjectSurrogate> foreignDirtballs, List<ICmObjectId> foreignGoners)
		{
			UowService = uowService;
			m_foreignNewbies = foreignNewbies;
			m_foreignDirtballs = foreignDirtballs;
			m_foreignGoners = foreignGoners;
		}
		/// <summary>
		/// Verifies that the changes indicated by the three lists made on some other client can be
		/// safely reconciled with any unsaved changes in this client.
		/// </summary>
		public bool OkToReconcileChanges()
		{
			var newbies = new HashSet<ICmObjectId>();
			var dirtballs = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			var goners = new HashSet<ICmObjectId>();

			UowService.GatherChanges(newbies, dirtballs, goners);
			// First check for any of the same objects modified.
			var foreignDirtballIds = new HashSet<ICmObjectId>(from obj in m_foreignDirtballs select obj.Id);
			var ourDirtballIds = new HashSet<ICmObjectId>(from obj in dirtballs select obj.Id);
			var commonDirtballs = new HashSet<ICmObjectId>(ourDirtballIds.Intersect(foreignDirtballIds));
			if (commonDirtballs.FirstOrDefault() != null && !OkToReconcileCommonDirtballs(commonDirtballs))
				return false; // todo: allow certain conflicts in owning collections.
			// Have we deleted anything they modified?
			if (goners.Intersect(foreignDirtballIds).FirstOrDefault() != null)
				return false;
			// Or they deleted something we modified?
			if (ourDirtballIds.Intersect(m_foreignGoners).FirstOrDefault() != null)
				return false;
			// Did we add a reference to something they deleted?
			if (m_foreignGoners.Count > 0)
			{
				var collector = new List<ICmObject>();
				foreach (var objOrSurrogate in dirtballs)
				{
					objOrSurrogate.Object.AllReferencedObjects(collector);
				}
				foreach (var id in newbies)
				{
					UowService.GetObject(id).AllReferencedObjects(collector);
				}
				foreach (var obj in collector)
				{
					if (m_foreignGoners.Contains(obj.Id))
						return false; // other client deleted an object we still reference, probably added a reference.
				}
			}
			// Did they add a reference to something we deleted?
			if (goners.Count > 0)
			{
				var gonerGuids = new HashSet<Guid>(from id in goners select id.Guid);
				foreach (var surrogate in m_foreignNewbies.Concat(m_foreignDirtballs))
				{
					var xml = surrogate.XML;
					// Scan all the objsur elements.
					for (int ich = 0; ; )
					{
						var sobjsur = "<objsur";
						int ichSurr = xml.IndexOf(sobjsur, ich);
						if (ichSurr < 0)
						{
							break;
						}
						var sguid = "guid=\"";
						int ichGuid = xml.IndexOf(sguid, ichSurr);
						if (ichGuid < 0)
						{
							Debug.Fail("objsur without guid!");
							return false; // don't try to reconcile THESE changes!
						}
						ichGuid += sguid.Length;
						int ichEndGuid = xml.IndexOf("\"", ichGuid);
						if (gonerGuids.Contains(new Guid(xml.Substring(ichGuid, ichEndGuid - ichGuid))))
						{
							// See if it is a reference surrogate. If it is owning, we should see some other conflict,
							// unless the ownership is in a collection we will reconcile.
							int ichType = xml.IndexOf("t=\"");
							if (ichType >= 0 && xml[ichType + "t=\"".Length] == 'r')
								return false; // they still reference something we killed.
						}
						ich = ichEndGuid;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Answer true if the changes reflected in m_foreignDirtballs for the specified object IDs
		/// can be automatically reconciled with the ones in the unsaved UOW.
		/// </summary>
		/// <param name="commonDirtballs"></param>
		/// <returns></returns>
		private bool OkToReconcileCommonDirtballs(HashSet<ICmObjectId> commonDirtballs)
		{
			foreach (var dirtball in m_foreignDirtballs)
			{
				if (!commonDirtballs.Contains(dirtball.Id))
					continue;
				if (!OkToReconcileDirtball(dirtball))
					return false;
			}
			return true;
		}

		private bool OkToReconcileDirtball(ICmObjectSurrogate dirtball)
		{
			bool foundBadChange = false;
			MakeChangeInfoFor(dirtball, action =>
			{
				if (action is FdoDateTimePropertyChanged)
				{
					var dtChange = (FdoDateTimePropertyChanged) action;
					var flid = dtChange.ModifiedFlid;
					var mdc = dtChange.Object.Services.MetaDataCache;
					if (mdc.GetFieldName(flid) == "DateModified")
					{
						// this change is not a problem, keep looking.
						// (This return does not exit this method, just the delegate.)
						return false;
					}
				}
				else if (action is FdoVectorPropertyChanged)
				{
					var flid = ((FdoVectorPropertyChanged) action).ModifiedFlid;
					var obj = action.Object;
					// collections we know how to reconcile.
					if ((CellarPropertyType)obj.Services.MetaDataCache.GetFieldType(flid) == CellarPropertyType.OwningCollection)
						return false; // keep checking, this one is not a problem
				}
				foundBadChange = true;
				// (This return does not exit this method, just the delegate.)
				return true; // stop processing changes, we found a decisive problem.
			});
			return !foundBadChange;
		}
		/// <summary>
		/// Given the changes indicated by the three lists made on some other client (which should have been
		/// verified using OkToReconcileChanges), make a non-undoable UOW which can be 'redone' to make the
		/// appropriate consistent changes to our system.
		/// This UOW is not exactly equivalent to the changes made in the other system. In most cases it is,
		/// but for collection and DateModified properties, we reconcile cases where both clients have made
		/// changes. The value we want in the UOW is the end result of combining our changes with the foreign
		/// ones, so that Redo will update the display to what it should be NOW. Likewise, to generate the
		/// right change, the old value in the created UOW is the current value (after our unsaved changes),
		/// not the value that obtained before the other user made changes. Also, we are not trying to update
		/// the UOW's list of objects created and deleted to be consistent with our reconciliation of
		/// owning collections.
		/// This might, in some pathological case, prevent an Undo of some pre-Save change that should be
		/// allowed. We can fix that if necessary, but are already well ahead of 6.0.
		/// </summary>
		public void ReconcileForeignChanges()
		{
			var uow = new FdoNonUndoableUnitOfWork(UowService);
			MakeBundlePredateUnsavedBundles(uow);
			UowService.NonUndoableStack.AddForeignBundleToUndoStack(uow);
			var fakeService = new UowServiceSimulator();
			foreach (var newby in m_foreignNewbies)
			{
				var xml = newby.XML; // may be destroyed by getting the Object.
				var objectCreation = new FdoStateChangeObjectCreation(newby.Object);
				objectCreation.SetAfterXml(xml);
				uow.AddAction(objectCreation);
				((CmObject)newby.Object).RegisterVirtualsModifiedForObjectCreation(fakeService);
			}
			foreach (var dirtball in m_foreignDirtballs)
			{
				// An object should already exist, since we supposedly checked we haven't deleted anything the other guy modified.
				MakeChangeInfoFor(dirtball,
					action =>
						{
							if (action is FdoDateTimePropertyChanged)
							{
								var dtChange = (FdoDateTimePropertyChanged) action;
								var flid = dtChange.ModifiedFlid;
								var mdc = dtChange.Object.Services.MetaDataCache;
								if (mdc.GetFieldName(flid) == "DateModified")
								{
									// We have a date modified change. We allow these even when the current unsaved changes
									// also modify this date. But if that is the case we need to do some reconciling.
									var newForeignDate = dtChange.NewTime;
									var currentObjInternal = (ICmObjectInternal) UowService.GetObject(dirtball.Id);
									var currentDate = currentObjInternal.GetTimeProperty(flid);
									// See if we have a conflict to reconcile. We do if any of our unsaved changes
									// modify this same date time.
									// (otherwise, this UOW has NOT modified the date time, so we just let the
									// other client value be used.)
									// they changed it, then we changed it.
									// Our date wins.
									// However if our change is undone, we should revert to their date, not the
									// original.
									foreach (var undoStack in UowService.UndoStacks)
									{
										foreach (var uow1 in undoStack.UnsavedUnitsOfWork)
										{
											foreach (var change in uow1.Changes.ToArray())
											{
												var dateChange = change as FdoDateTimePropertyChanged;
												if (dateChange != null && dateChange.ModifiedFlid == flid && dateChange.Object == dtChange.Object)
												{
													// Both changed it! Reconcile. We are going to pretend the foreign change happened
													// before ours (since it got saved first). In case our change gets undone, it should
													// not set the modify time back earlier than the foreign change.
													if (dateChange.OldTime < newForeignDate)
														dateChange.OldTime = newForeignDate;
													if (dateChange.NewTime < newForeignDate)
													{
														// Our change is going to be processed as after theirs, but it tries to set
														// an earlier time (presumably it really happened earlier, but didn't
														// get saved until later). Just discard it. "Redoing" their change will
														// update the current time to match theirs.
														uow1.Changes.Remove(change);
													}
													else
													{
														// Our change is to a later time than theirs. We want to keep our time.
														// So we don't need to do any PropChanged on this property; we can
														// discard it from the fake UOW. To do this we just do NOT add it to
														// our fake UOW.
														return false; // continue processing other changes.
													}
												}
											}
										}
									}
								}
							}
							else if(action is FdoVectorPropertyChanged)
							{
								ReconcileCollectionProperty((FdoVectorPropertyChanged) action);
							}
							uow.AddAction(action);
							return false; // continue processing changes.
						});
			}
			foreach (var goner in m_foreignGoners)
			{
				if (!UowService.HasObject(goner.Guid))
					continue; // improbably, we don't have it; maybe both created and deleted elsewhere?
				var existingObj = UowService.GetObject(goner.Guid);
				uow.AddAction(new FdoStateChangeObjectDeletion(existingObj, ((ICmObjectInternal)existingObj).ToXmlString()));
				((CmObject)existingObj).RegisterVirtualsModifiedForObjectDeletion(fakeService);
			}
			uow.SimulateRedo();
			// Now all the real properties are in their expected state. So we can figure correct final values for
			// all the modified virtuals. We fake a change indicating ALL the items are new; we don't have an
			// accurate old count, but recipients of PropChanged should not rely on the old count when told that
			// all items are inserted. (Passing the new count as the number deleted is therefore somewhat arbitrary.)
			if (fakeService.ModifiedVirtuals.Count > 0)
			{
				var changes = new List<ChangeInformation>();
				foreach (var pair in fakeService.ModifiedVirtuals)
				{
					int chvo = pair.Item1.Cache.DomainDataByFlid.get_VecSize(pair.Item1.Hvo, pair.Item2);
					var change = new ChangeInformation(pair.Item1, pair.Item2, 0, chvo, chvo);
					changes.Add(change);
				}
				UowService.SendPropChangedNotifications(changes);
			}
		}

		/// <summary>
		/// This class simulates a UnitOfWorkService for the purpose of allowing RegisterVirtualsModifiedForObjectDeletion
		/// and RegisterVirtualsModifiedForObjectCreation to add actions to the current UOW. For this purpose most of the
		/// methods do not need to be implemented.
		/// </summary>
		class UowServiceSimulator : IUnitOfWorkService
		{
			public HashSet<Tuple<ICmObject, int>> ModifiedVirtuals = new HashSet<Tuple<ICmObject, int>>();

			/// <summary>
			/// Return true if the object is newly created this UOW. Virtual PropChange information
			/// is typically not needed in this case.
			/// </summary>
			public bool IsNew(ICmObject obj)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Add a newly created object to the Undo/Redo system.
			/// </summary>
			/// <param name="newby"></param>
			public void RegisterObjectAsCreated(ICmObject newby)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Add a newly deleted object to the Undo/Redo system.
			/// </summary>
			/// <param name="goner"></param>
			/// <param name="xmlStateBeforeDeletion">Old before State XML (in case it gets undeleted).</param>
			public void RegisterObjectAsDeleted(ICmObject goner, string xmlStateBeforeDeletion)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having its ownership changed.  This won't cause either an Undo or Redo, or
			/// a PropChanged, but will cause the modified XML to be written out.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="ownerBeforeChange">Owner prior to change</param>
			/// <param name="owningFlidBeforeChange">Owning field id prior to change</param>
			/// <param name="owningFlid">Owning field id after change</param>
			public void RegisterObjectOwnershipChange(ICmObject dirtball, Guid ownerBeforeChange, int owningFlidBeforeChange, int owningFlid)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, Guid[] originalValue, Guid[] newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having a modified atomic reference.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModifiedRef(ICmObject dirtball, int modifiedFlid, Guid originalValue, Guid newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
			/// but no actual data change to be saved.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, Guid[] originalValue, Guid[] newValue)
			{
				// This one line of code (and the one below) is currently the whole purpose of this class!
				ModifiedVirtuals.Add(new Tuple<ICmObject, int>(dirtball, modifiedFlid));
			}

			/// <summary>
			/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
			/// but no actual data change to be saved.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="reader">Function to read the current value</param>
			/// <param name="added">items added</param>
			/// <param name="removed">items removed</param>
			public void RegisterVirtualCollectionAsModified<T>(ICmObject dirtball, int modifiedFlid, Func<IEnumerable<T>> reader, IEnumerable<T> added, IEnumerable<T> removed)
			{
				// This one line of code (and the one above) is currently the whole purpose of this class!
				ModifiedVirtuals.Add(new Tuple<ICmObject, int>(dirtball, modifiedFlid));
			}

			/// <summary>
			/// Often a more convenient way to register a virtual as modified.
			/// </summary>
			public void RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, IEnumerable<ICmObject> newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, Guid originalValue, Guid newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, ITsString originalValue, ITsString newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
			/// but no actual data change to be saved.
			/// </summary>
			public void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, ITsString originalValue, ITsString newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having experienced a modification of a string virtual property. This causes a PropChanged to be sent,
			/// but no actual data change to be saved.
			/// </summary>
			public void RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, ITsString newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="ws">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, int ws, ITsString originalValue, ITsString newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having experienced a modification of a multistring virtual property. This causes a PropChanged to be sent,
			/// but no actual data change to be saved.
			/// </summary>
			public void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, int ws, ITsString originalValue, ITsString newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having experienced a modification of a multistring alternative. This causes a PropChanged to be sent,
			/// but no actual data change to be saved.
			/// </summary>
			public void RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, int ws)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, ITsTextProps originalValue, ITsTextProps newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, string originalValue, string newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
			/// but no actual data change to be saved.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, string originalValue, string newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, bool originalValue, bool newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
			/// but no actual data change to be saved.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, bool originalValue, bool newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, DateTime originalValue, DateTime newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, byte[] originalValue, byte[] newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, int originalValue, int newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, int originalValue, int newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, object originalValue, object newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Register an object as having been modified.
			/// </summary>
			/// <param name="dirtball">Modified object</param>
			/// <param name="modifiedFlid">Modified flid</param>
			/// <param name="originalValue">Original value</param>
			/// <param name="newValue">New value</param>
			public void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, GenDate originalValue, GenDate newValue)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Registers an object as containing data for a custom field that was modified or deleted.
			/// </summary>
			/// <param name="dirtball">The modified object.</param>
			/// <param name="modifiedFlid">The modified flid.</param>
			public void RegisterCustomFieldAsModified(ICmObject dirtball, int modifiedFlid)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Get the unsaved UOWs from all the undo stacks, in the order they occurred.
			/// </summary>
			public List<FdoUnitOfWork> UnsavedUnitsOfWork
			{
				get { throw new NotImplementedException(); }
			}

			/// <summary>
			/// Gather the lists of modified etc. objects that must be saved. (This is put in the interface just for testing.)
			/// </summary>
			public void GatherChanges(HashSet<ICmObjectId> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// This new (non-undoable) bundle needs to appear to predate any unsaved changes, and any (later) changes
		/// we might still redo, but not any others.
		/// This allows us to Undo our own changes, which have been adjusted if need be to appear subsequent
		/// to the argument bundle (though not to undo further, if conflicts occur).
		/// </summary>
		/// <param name="uow"></param>
		private void MakeBundlePredateUnsavedBundles(FdoNonUndoableUnitOfWork uow)
		{
			int minSequence = int.MaxValue;
			foreach (var undoStack in UowService.UndoStacks)
			{
				foreach (var uow1 in undoStack.UnsavedUnitsOfWork.Concat(undoStack.RedoableUnitsOfWork))
				{
					minSequence = Math.Min(uow1.Sequence, minSequence);
				}
			}
			if (minSequence > uow.Sequence)
				return;
			var delta = uow.Sequence - minSequence + 1;
			foreach (var undoStack in UowService.UndoStacks)
			{
				foreach (var uow1 in undoStack.UnsavedUnitsOfWork.Concat(undoStack.RedoableUnitsOfWork))
				{
					uow1.Sequence += delta;
				}
			}
		}

		/// <summary>
		/// The other UOW has changed a collection. If we have too, do something to reconcile things.
		/// </summary>
		private void ReconcileCollectionProperty(FdoVectorPropertyChanged foreignChange)
		{
			int flid = foreignChange.ModifiedFlid;
			var obj = foreignChange.Object;
			foreach (var undoStack in UowService.UndoStacks)
			{
				foreach (var uow1 in undoStack.UnsavedUnitsOfWork)
				{
					foreach (var change in uow1.Changes.ToArray())
					{
						var vecChange = change as FdoVectorPropertyChanged;
						if (vecChange != null && vecChange.ModifiedFlid == flid && vecChange.Object == obj)
						{
							// We don't know what they really changed, because the foreignChange reflects
							// the difference between the CURRENT value of the field and what it was changed
							// to on the other system. If we let it, this change would typically delete anything we
							// added, and restore anything we deleted. However, we do have true information on what WE
							// changed.
							var currentGuids = new HashSet<Guid>(vecChange.NewGuids);
							var oldGuids = new HashSet<Guid>(vecChange.OriginalGuids);
							// These express the change we made here.
							var localAddedGuids = new HashSet<Guid>(currentGuids.Except(oldGuids));
							var localDeletedGuids = new HashSet<Guid>(oldGuids.Except(currentGuids));

							// We don't want to create any duplicates, so in case we somehow both added something
							// (perhaps in a ref collection), remove from our add list anything already in the output.
							localAddedGuids.ExceptWith(foreignChange.NewGuids);

							// We filter from the foreign value anything we deleted, to prevent the reconciliation
							// from putting it back. And, we add to it anything we added, to prevent the reconciliation
							// from deleting it.
							var newForeignGuids =
								((from guid in foreignChange.NewGuids where !localDeletedGuids.Contains(guid) select guid)
									.Concat(localAddedGuids)).ToArray();
							// We'll use that below to update foreignChange.NewGuids, but leave it alone till we've finished using it.

							// Now we want to fix the input and output stages of vecChange. The things the other guy
							// added should be added to both lists, and the things the other guy deleted should be
							// deleted from both lists.

							var foreignGuids = new HashSet<Guid>(foreignChange.NewGuids);
							// These express the change from our end result to the value saved elsewhere.
							var foreignAddedGuids = new HashSet<Guid>(foreignGuids.Except(currentGuids));
							var foreignDeletedGuids = new HashSet<Guid>(currentGuids.Except(foreignGuids));

							// A more accurate idea of what they deleted is obtained by removing from their set
							// the ones we added. Presumably they are missing from the foreign set because we
							// added them, not because they deleted them. This presumes that the value we started
							// from is a common starting point.
							foreignDeletedGuids.ExceptWith(localAddedGuids);
							// Likewise, ones we deleted are probably in the foreign added set by mistake.
							foreignAddedGuids.ExceptWith(localDeletedGuids);
							var foreignExceptLocalNew = new HashSet<Guid>(foreignAddedGuids.Except(currentGuids));
							var foreignExceptLocalOld = new HashSet<Guid>(foreignAddedGuids.Except(oldGuids));

							var correctedNewGuids =
								((from guid in vecChange.NewGuids where !foreignDeletedGuids.Contains(guid) select guid)
									.Concat(foreignExceptLocalNew)).ToArray();
							var correctedOldGuids =
								((from guid in vecChange.OriginalGuids where !foreignDeletedGuids.Contains(guid) select guid)
									.Concat(foreignExceptLocalOld)).ToArray();

							foreignChange.NewGuids = newForeignGuids;
							vecChange.NewGuids = correctedNewGuids;
							vecChange.OriginalGuids = correctedOldGuids;
						}
					}
				}
			}
		}

		/// <summary>
		/// Given a foreign dirtball, which we know has a corresponding object in our own DB, make change records
		/// which will convert our object to match the dirtball. Each change record is passed to the processChangeRecord
		/// func, and if it returns true, we stop.
		/// </summary>
		private void MakeChangeInfoFor(ICmObjectSurrogate dirtball, Func<FdoStateChangeBase, bool> processChangeRecord)
		{
			var currentObj = UowService.GetObject(dirtball.Id);
			var currentInternal = (ICmObjectInternal)currentObj;
			var rtElement = XElement.Parse(dirtball.XML);
			var mdc = (IFwMetaDataCacheManaged)currentObj.Cache.MetaDataCache;
			// First see if it changed owners.
			var ownerAttr = rtElement.Attribute("ownerguid");
			var foreignOwnerGuid = ownerAttr == null ? Guid.Empty : new Guid(ownerAttr.Value);
			var currentOwnerGuid = currentObj.Owner == null ? Guid.Empty : currentObj.Owner.Guid;
			if (foreignOwnerGuid != currentOwnerGuid)
				if (processChangeRecord(new FdoOwnerChanged(currentObj, currentOwnerGuid, currentObj.OwningFlid, foreignOwnerGuid, 0)))
					return;
			foreach (var flid in mdc.GetFields(currentObj.ClassID, true, (int)CellarPropertyTypeFilter.All))
			{
				if (mdc.get_IsVirtual(flid))
					continue;
				var name = mdc.GetFieldName(flid);
				XElement element;
				if (mdc.IsCustom(flid))
				{
					element =
						(from elt in rtElement.Elements("Custom")
						 where AttrValue(elt, "name") == name
						 select elt).FirstOrDefault();
				}
				else
				{
					element = rtElement.Element(name);
				}
				var propertyType = (CellarPropertyType)mdc.GetFieldType(flid);
				switch (propertyType)
				{
					case CellarPropertyType.OwningCollection:
					case CellarPropertyType.ReferenceCollection:
						{
							var currentItems = (from obj in currentInternal.GetVectorProperty(flid) select obj.Guid).ToArray();
							var currentSet = new HashSet<Guid>(currentItems);
							var foreignItems = TargetsInForignElement(element);
							var foreignSet = new HashSet<Guid>(foreignItems);
							if (currentSet.SetEquals(foreignSet))
								continue;
							if (processChangeRecord(new FdoVectorPropertyChanged(currentObj, flid, currentItems, foreignItems)))
								return;
						}
						break;
					case CellarPropertyType.OwningSequence:
					case CellarPropertyType.ReferenceSequence:
						{
							var currentItems = (from obj in currentInternal.GetVectorProperty(flid) select obj.Guid).ToArray();
							var foreignItems = TargetsInForignElement(element);
							if (ArrayUtils.AreEqual(currentItems, foreignItems))
								continue;
							if (processChangeRecord(new FdoVectorPropertyChanged(currentObj, flid, currentItems, foreignItems)))
								return;
						}
						break;
					case CellarPropertyType.OwningAtomic:
					case CellarPropertyType.ReferenceAtomic:
						{
							int hvo = currentInternal.GetObjectProperty(flid); // wish this just returned the object!
							var currentItem = hvo == 0 ? Guid.Empty : currentObj.Services.GetObject(hvo).Guid;
							var foreignItem = Guid.Empty;
							if (element != null)
							{
								var foreignItems = TargetsInForignElement(element);
								if (foreignItems.Length == 1)
									foreignItem = foreignItems[0];
							}
							if (currentItem != foreignItem)
							{
								if (propertyType == CellarPropertyType.OwningAtomic)
									processChangeRecord(new FdoGuidPropertyChanged(currentObj, flid, currentItem, foreignItem));
								else
									if (processChangeRecord(new FdoAtomicRefPropertyChanged(currentObj, flid, currentItem, foreignItem)))
										return;
							}
						}
						break;

					case CellarPropertyType.Time:
						if (element == null || element.Attribute("val") == null)
							continue; // what time would we change it to?
						var originalTime = currentInternal.GetTimeProperty(flid);
						var newTime = ReadWriteServices.LoadDateTime(element);
						// We only record time in the file to an accuracy of a millisecond, so ignore smaller differences.
						if (IsTimeRoughlyEqual(originalTime, newTime))
							continue;
						if (processChangeRecord(new FdoDateTimePropertyChanged(currentObj, flid, originalTime, newTime)))
							return;
						break;
					case CellarPropertyType.MultiString:
					case CellarPropertyType.MultiBigString:
						{
							if (element == null)
								continue;
							var wsf = currentObj.Services.WritingSystemFactory;
							foreach (var aStrNode in element.Elements("AStr"))
							{
								ITsString tss;
								int ws = MultiAccessor.ReadAstrElementOfMultiString(aStrNode, wsf, out tss);
								if (ws == 0)
									continue;
								var multiString = currentInternal.GetITsMultiStringProperty(flid);
								var oldValue = multiString.get_String(ws);
								if ((tss == null && oldValue != null && oldValue.Length != 0) || !tss.Equals(oldValue))
								{
									if (processChangeRecord(new FdoMultiTsStringPropertyChanged(currentObj, flid, ws, oldValue, tss)))
										return;
								}
							}
						}
						break;
					case CellarPropertyType.MultiUnicode:
					case CellarPropertyType.MultiBigUnicode:
						{
							if (element == null)
								continue;
							var wsf = currentObj.Services.WritingSystemFactory;
							var tsf = currentObj.Cache.TsStrFactory;
							foreach (var aStrNode in element.Elements("AUni"))
							{
								ITsString tss;
								int ws = MultiUnicodeAccessor.ReadMultiUnicodeAlternative(aStrNode, wsf, tsf, out tss);
								if (ws == 0)
									continue;
								var multiString = currentInternal.GetITsMultiStringProperty(flid);
								var oldValue = multiString.get_String(ws);
								if ((tss == null && !IsNullOrEmptyTss(oldValue)) || (tss != null && !tss.Equals(oldValue)))
								{
									if (processChangeRecord(new FdoMultiTsStringPropertyChanged(currentObj, flid, ws, oldValue, tss)))
										return;
								}
							}
						}
						break;
					case CellarPropertyType.String:
					case CellarPropertyType.BigString:
						{
							ITsString foreign = null;
							if (element != null)
								foreign = TsStringSerializer.DeserializeTsStringFromXml((XElement)element.FirstNode,
									currentObj.Services.WritingSystemFactory);
							var current = currentInternal.GetITsStringProperty(flid);
							if ((current == null && !IsNullOrEmptyTss(foreign)) || (current != null && !current.Equals(foreign)))
								if (processChangeRecord(new FdoTsStringPropertyChanged(currentObj, flid, current, foreign)))
									return;
						}
						break;
					case CellarPropertyType.Boolean:
						{
							var foreign = element == null ? false : ReadWriteServices.LoadBoolean(element);
							var current = currentInternal.GetBoolProperty(flid);
							if (current != foreign)
								if (processChangeRecord(new FdoBooleanPropertyChanged(currentObj, flid, current, foreign)))
									return;
						}
						break;
					case CellarPropertyType.GenDate:
						{
							var foreign = element == null ? new GenDate() : ReadWriteServices.LoadGenDate(element);
							var current = currentInternal.GetGenDateProperty(flid);
							if (current != foreign)
								if (processChangeRecord(new FdoGenDatePropertyChanged(currentObj, flid, current, foreign)))
									return;
						}
						break;
					case CellarPropertyType.Guid:
						{
							if (flid == CmObjectTags.kflidGuid)
								continue; // this one for some reason don't show up as virtual but can't be set normally.
							var foreign = element == null ? Guid.Empty : ReadWriteServices.LoadGuid(element);
							var current = currentInternal.GetGuidProperty(flid);
							if (current != foreign)
								if(processChangeRecord(new FdoGuidPropertyChanged(currentObj, flid, current, foreign)))
									return;
						}
						break;
					case CellarPropertyType.Integer:
						{
							switch (flid)
							{
								case CmObjectTags.kflidHvo:
								case CmObjectTags.kflidOwnFlid:
								case CmObjectTags.kflidClass:
								case CmObjectTags.kflidOwnOrd:
									continue; // these ones for some reason don't show up as virtual but can't be set normally.
							}
							var foreign = element == null ? 0 : ReadWriteServices.LoadInteger(element);
							var current = currentInternal.GetIntegerValue(flid);
							if (current != foreign)
								if (processChangeRecord(new FdoIntegerPropertyChanged(currentObj, flid, current, foreign)))
									return;
						}
						break;
					case CellarPropertyType.Binary:
						{
							// These are tricky. Most of the few that exist are used for tsTextProps, but a handful
							// (possibly obsolete) hold byte arrays. There's no easy MDC way to tell which are which.
							// Eventually hopefully we either get rid of the non-tsTextProps ones, or have a distinct
							// enumeration member in CellarPropertyType. In the meantime, to make it work, we do this:
							var propertyName = mdc.GetFieldName(flid);
							string sig = (from attr in currentObj.GetType().GetProperty(propertyName).GetCustomAttributes(false)
											 where attr is ModelPropertyAttribute select ((ModelPropertyAttribute)attr).Signature)
											 .FirstOrDefault();
							if (sig == "ITsTextProps")
							{
								ITsTextProps foreign = null;
								if (element != null)
									foreign = ReadWriteServices.LoadTextPropBinary(element, currentObj.Services.WritingSystemFactory);
								var current = currentInternal.GetITsTextPropsProperty(flid);
								if (current != foreign)
									if (processChangeRecord(new FdoTsTextPropsPropertyChanged(currentObj, flid, current, foreign)))
										return;
							}
							else
							{
								var foreign = new byte[0];
								if (element != null)
									foreign = ReadWriteServices.LoadByteArray(element);
								var current = currentInternal.GetBinaryProperty(flid);
								if (!EqualByteArrays(foreign, current))
									if (processChangeRecord(new FdoBinaryPropertyChanged(currentObj, flid, current, foreign)))
										return;
							}
						}
						break;
					case CellarPropertyType.Unicode:
						{
							var foreign = element == null ? "" : ReadWriteServices.LoadUnicodeString(element);
							var current = currentInternal.GetStringProperty(flid);
							if (string.IsNullOrEmpty(foreign) && string.IsNullOrEmpty(current))
								continue;
							if (current != foreign)
								if (processChangeRecord(new FdoStringPropertyChanged(currentObj, flid, current, foreign)))
									return;
						}
						break;
				}
			}
		}

		private bool EqualByteArrays(byte[] foreign, byte[] current)
		{
			if (foreign == current)
				return true; // among other possibilities, handles both null.
			if (foreign == null)
				return current.Length == 0;
			if (current == null)
				return foreign.Length == 0;
			if (foreign.Length != current.Length)
				return false;
			for (int i = 0; i < current.Length; i++)
			{
				if (foreign[i] != current[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// We're looking for times that come out the same in our XML file.
		/// First we check that they are quite close, a way of answering false for anything that might
		/// differ by seconds or more.
		/// Then we check that they come up with the exact same millisecond value.
		/// </summary>
		private bool IsTimeRoughlyEqual(DateTime originalTime, DateTime newTime)
		{
			return Math.Abs((originalTime - newTime).TotalMilliseconds) < 3 && originalTime.Millisecond == newTime.Millisecond;
		}

		/// <summary>
		/// Return the guids in the list of objsur elements in the string.
		/// </summary>
		Guid[] TargetsInForignElement(XElement source)
		{
			if (source == null)
				return new Guid[0];
			var results = new List<Guid>();
			foreach (var element in source.Elements())
			{
				if (element.Name != "objsur")
					continue;
				results.Add(new Guid(element.Attribute("guid").Value));
			}
			return results.ToArray();
		}
		string AttrValue(XElement elt, string name)
		{
			var attr = elt.Attribute(name);
			if (attr == null)
				return "";
			return attr.Value;
		}
		bool IsNullOrEmptyTss(ITsString tss)
		{
			return tss == null || tss.Length == 0;
		}
	}
}
