// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.Utils;
using Timer = System.Timers.Timer;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// IActionHandler implementation for stateful FDO.
	///
	/// This class also serves as a Mediator between two sets of clients.
	///
	/// One set of clients is between CmObjects and application classes.
	/// This Mediator class notifies registered application classes of property changes
	/// via the 'PropChanged' method of the IVwNotifyChange interface.
	/// In this implementation, the FDO ISILDataAccess implementation farms out this
	/// notification function to this class.
	/// IVwNotifyChange implementations are not to use this notificatin mechanism
	/// to make additional data changes. That is done using the second set of Mediator clients.
	///
	/// The second set of Mediator clients is between CmObjects.
	/// This Mediator function allows for 'side effects' to be made, when a property changes.
	/// That is, instances of CmObjects register themselves with this class to get notifications
	/// of properties on other CmObjects.
	///
	/// With this twofold Mediator mechanism,
	/// the IVwNotifyChange system can stay with its purpose of refeshing UI display,
	/// while the second system can take care of side effect data changes.
	/// </summary>
	/// <remarks>
	/// There are five steps of work in three major steps that get managed by this class:
	///
	/// 1.A Basic Undo/Redo processing.
	///		In this stage, some undoable element is processed.
	///		This will typically be done using special FDO IUndoAction subclasses that record
	///		the 'before' and 'after' states of a property on a CmObject.
	/// 1.B For each action in Step 1.A, notification of the change will be sent to registered clients,
	///		so they can handle any side effects of the primary change.
	///		It is expected that those side effects will precipitate additional Undo/Redo actions (Step 1.A),
	///		which may generate more while those are being handled.
	///
	/// After all handling of Steps 1.A and 1.B has been finished for one or more changes,
	/// a call to the 'EndUndoTask' method is expected.
	/// That method call will start Steps 2.A and 2.B.
	///
	/// 2.A The XML string that is persisted for each new/modified CmObject will be generated.
	///		Technically, this can be deferred until commit time, but doing it here may help spread out
	///		the "True Cost of Operations" a bit so the end-user perceives FW apps to be faster.
	///		If it proves a drag on overall performance, this step can be shifted into the Commit call.
	///	2.B PropChanged notifications will take place in this step.
	///		Calls to the ISILDataAccess 'PropChanged' method are not supported at any time.
	///
	///		For cases where a new CmObject is created,
	///		there is no need for broadcasting PropChanges when its properties are also set.
	///		There is probably no need to create and store IUndoActions for these either.
	///		The reason for this is nothing can be displaying the new object,
	///		so there is no reason to bother the views code with the 'noise'.
	///
	/// 3. Session logging (not to be implemented yet [as of 16 June 2009]):
	///		This class will maintain an XML file that logs changes made to a language project
	///		during a session (run of the application program).
	///		The log file will be updated on each Commit call. That is, it will be created on the first commit call,
	///		and subsequent calls will append the new set of changes.
	///
	///		This log file will be suitable for use to update remote user machines.
	///		The log could also be any other BEP, in which case it could serve as the place to do auto-saves.
	/// </remarks>
	internal sealed class UnitOfWorkService : ISilDataAccessHelperInternal, IUnitOfWorkService, IWorkerThreadReadHandler, IUndoStackManager, IDisposable
	{
		/// <summary>
		/// Keep track of FdoMediator business transaction state (Finite State Machine).
		/// </summary>
		[ComVisible(false)]
		internal enum FdoBusinessTransactionState
		{
			ReadyForBeginTask,
			ProcessingDataChanges,
			BroadcastingPropChanges
		}
		public event EventHandler<SaveEventArgs> OnSave;

		private DateTime m_lastSave = DateTime.Now;
		private readonly Timer m_saveTimer = new Timer();

		private bool m_fInSaveInternal;

		/// <summary>
		/// Non-null if a Save has failed due to conflicting changes (client-server only).
		/// No further Saves can be attempted until Refresh allows these changes to be reconciled.
		/// </summary>
		private IReconcileChanges m_pendingReconciliation;

		private readonly IDataStorer m_dataStorer;
		private readonly IdentityMap m_identityMap;
		private readonly IFdoUI m_ui;
		internal ICmObjectRepositoryInternal ObjectRepository
		{
			get;
			private set;
		}
		private readonly HashSet<IVwNotifyChange> m_changeWatchers = new HashSet<IVwNotifyChange>();
		private FdoBusinessTransactionState m_currentProcessingState;
		internal FdoBusinessTransactionState CurrentProcessingState {
			get { return m_currentProcessingState; }
			set
			{
				// This lock is needed for LinguaLinks import to prevent deletions from happening while saving file LT-11727
				lock (this)
				{
					m_currentProcessingState = value;
				}
				// Any time we return to the quiescent state, make the active stack match the current one.
				// This way the active one does not change during a UOW; it is allowed to complete on the old one.
				if (m_currentProcessingState == FdoBusinessTransactionState.ReadyForBeginTask)
					m_activeUndoStack = m_currentUndoStack;
			}
		}
		internal bool UndoOrRedoInProgress { get; set;}
		internal bool SuppressSelections { get; set; }

		/// <summary>
		/// The CURRENT undo stack is the one associated with the active window and can be changed any time
		/// a window is activated.
		/// </summary>
		private UndoStack m_currentUndoStack;
		/// <summary>
		/// The ACTIVE Undo stack is the one that has a UOW in progress. It switches to the current one
		/// at the end of a UOW.
		/// </summary>
		private UndoStack m_activeUndoStack;

		private readonly List<UndoStack> m_undoStacks = new List<UndoStack>();
		internal UndoStack NonUndoableStack { get; private set; }

		/// <summary>
		/// The FDO lock. This lock is used to synchronize access to the entire FDO. The UI thread is the
		/// only thread that is allowed to start UOWs and thus perform writes to FDO objects.
		/// </summary>
		internal readonly ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();

		/// <summary>
		/// Constructor.
		/// </summary>
		internal UnitOfWorkService(IDataStorer dataStorer, IdentityMap identityMap, ICmObjectRepositoryInternal objectRepository, IFdoUI ui)
		{
			if (dataStorer == null) throw new ArgumentNullException("dataStorer");
			if (identityMap == null) throw new ArgumentNullException("identityMap");
			if (objectRepository == null) throw new ArgumentNullException("objectRepository");
			if (ui == null) throw new ArgumentNullException("ui");

			m_dataStorer = dataStorer;
			m_identityMap = identityMap;
			ObjectRepository = objectRepository;
			m_ui = ui;
			CurrentProcessingState = FdoBusinessTransactionState.ReadyForBeginTask;
			NonUndoableStack = (UndoStack)CreateUndoStack();
			// Make a separate stack as the initial default. This should be mainly used in tests.
			// It serves to keep undoable UOWs separate from non-undoable ones, the only things ever put on the NonUndoableStack.
			m_currentUndoStack = m_activeUndoStack = (UndoStack)CreateUndoStack();

			m_saveTimer.SynchronizingObject = ui.SynchronizeInvoke;
			m_saveTimer.Interval = 1000;
			m_saveTimer.Elapsed += SaveOnIdle;
			m_saveTimer.Start();
		}

		/// <summary>
		/// Stops the timer that periodically saves changes.
		/// </summary>
		public void StopSaveTimer()
		{
			m_saveTimer.Stop();
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~UnitOfWorkService()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		private void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_lock.Dispose();
				m_saveTimer.Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		void SaveOnIdle(object sender, ElapsedEventArgs e)
		{
			lock (this)
			{
				// Check if we are already in SaveInternal.
				if (m_fInSaveInternal)
					return;
				// Don't save if we're in the middle of something and not in the right state to Save!
				if (UndoOrRedoInProgress || CurrentProcessingState != FdoBusinessTransactionState.ReadyForBeginTask)
					return; // don't start another, if for example the conflict dialog is open.

				if (((CmObjectRepository)ObjectRepository).IsDisposed)
					return; // hopefully only significant during testing, we don't want autosaves if the cache is already disposed.
				if (m_pendingReconciliation != null)
					return; // don't auto-save until the user Refreshes.
				// Don't auto-save if an undo stack contains a mark since the stack might throw away or collapse
				// all of the items since the mark. Saving at this time would cause problems since a collapse
				// would reset the has-stuff-to-save variable even though some of the data might be saved by
				// this save. (FWR-2991)
				if (m_undoStacks.Any(stack => stack.TopMarkHandle != 0))
					return;
				// Don't autosave less than 10s from the last save.
				if (DateTime.Now - m_lastSave < TimeSpan.FromSeconds(10.0))
					return;
				// Nor if it's been less than 2s since the user did something. We don't want to interrupt continuous activity.
				if (DateTime.Now - m_ui.LastActivityTime < TimeSpan.FromSeconds(2.0))
					return;

				SaveInternal();
			}
		}

		internal List<UndoStack> UndoStacks { get { return m_undoStacks; } }

		/// <summary>
		/// The currently active one (typically set by activating a main window).
		/// </summary>
		public IActionHandler CurrentUndoStack { get { return m_currentUndoStack; } }

		public IActionHandler ActiveUndoStack { get { return m_activeUndoStack; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether any of the call stacks have unsaved changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasUnsavedChanges
		{
			get { return m_undoStacks.Any(stack => stack.HasUnsavedChanges); }
		}

		/// <summary>
		/// Save all changes to disk.
		/// </summary>
		public void Save()
		{
			lock (this)
				SaveInternal();
		}

		private void SaveInternal()
		{
			// don't allow reentrant calls.
			if (m_fInSaveInternal)
				return;

			m_fInSaveInternal = true;
			try
			{
				// m_currentProcessingState *must* be FdoBusinessTransactionState.ReadyForBeginTask.
				m_activeUndoStack.CheckReadyForCommit("Commit at wrong place.");
				m_lastSave = DateTime.Now;

				// Gather up the objects from the UOW bundles and send them to the BEP.
				// We need to reverse the Stack order.
				var newbies = new HashSet<ICmObjectId>();
				var dirtballs = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
				var goners = new HashSet<ICmObjectId>();

				GatherChanges(newbies, dirtballs, goners);
				bool fWaitForCommitLock = false;
				if (newbies.Count != 0 || dirtballs.Count != 0 || goners.Count != 0)
				{
					fWaitForCommitLock = true;
					// raise the OnSave event: something nontrivial is being saved.
					bool undoable = false;
					foreach (var stack in m_undoStacks)
					{
						if (stack.CanUndo())
							undoable = true;
					}
					RaiseSave(undoable);
				}

				var repo = (ICmObjectRepository)ObjectRepository;
				var realNewbies = new HashSet<ICmObjectOrSurrogate>(
						(from id in newbies
						 select (ICmObjectOrSurrogate)repo.GetObject(id)).Where(x => x != null));

				if (m_dataStorer is IClientServerDataManager)
				{
					if (m_pendingReconciliation != null)
					{
						GetUserInputOnConflictingSave();
						return; // Don't try to save the changes we just reverted!
					}
					List<ICmObjectSurrogate> foreignNewbies;
					List<ICmObjectSurrogate> foreignDirtballs;
					List<ICmObjectId> foreignGoners;
					var csm = (IClientServerDataManager)m_dataStorer;
					while (csm.GetUnseenForeignChanges(out foreignNewbies, out foreignDirtballs, out foreignGoners, fWaitForCommitLock))
					{
						var reconciler = Reconciler(foreignNewbies, foreignDirtballs, foreignGoners);
						if (reconciler.OkToReconcileChanges())
						{
							reconciler.ReconcileForeignChanges();
							// And continue looping, in case there are by now MORE foreign changes!
						}
						else
						{
							m_pendingReconciliation = reconciler;
							GetUserInputOnConflictingSave();
							return;
						}
					}
				}

				// let the BEP determine if a commit should occur or not
				if (!m_dataStorer.Commit(realNewbies, dirtballs, goners))
				{
					// TODO: What happens if BEP was not able to commit?
					throw new InvalidOperationException("Could not save the data for some reason.");
				}
				foreach (var stack in m_undoStacks)
					stack.RecordSaved();
			}
			finally
			{
				m_fInSaveInternal = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets user input on conflicting save
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetUserInputOnConflictingSave()
		{
			if (m_ui.ConflictingSave())
			{
				RevertToSavedState();
			}
		}

		private void RaiseSave(bool undoable)
		{
			if (OnSave != null)
				OnSave(this, new SaveEventArgs {UndoableChanges = undoable, Cache = ((CmObjectRepository)ObjectRepository).Cache});
		}

		/// <summary>
		/// Call this as part of a global Refresh. If it's possible for the backend to get into a
		/// state where it needs to copy changes from the backing store to the in-memory objects,
		/// do so. This might involve undoing some changes, or changing the state of which changes
		/// are considered Saved or Undoable.
		/// </summary>
		public void Refresh()
		{
			if (m_pendingReconciliation != null)
				RevertToSavedState();
		}

		private void RevertToSavedState()
		{
			// Undo the most recent unsaved change from any stack until none remain.
			var updatedStacks = new List<UndoStack>();
			for (; ;)
			{
				FdoUnitOfWork mostRecentUnsavedChange = null;
				UndoStack stackWithMostRecentChange = null;
				foreach (var stack in m_undoStacks)
				{
					if (stack == NonUndoableStack)
						continue; // if there's a change here that conflicts we are dead.
					foreach (var item in stack.UnsavedUnitsOfWork)
					{
						if (mostRecentUnsavedChange == null || item.Sequence > mostRecentUnsavedChange.Sequence)
						{
							mostRecentUnsavedChange = item;
							stackWithMostRecentChange = stack;
						}
					}
				}
				if (mostRecentUnsavedChange == null)
					break;
				stackWithMostRecentChange.RevertUnsavedUnitOfWork();
				if (!updatedStacks.Contains(stackWithMostRecentChange))
					updatedStacks.Add(stackWithMostRecentChange);
			}
			// items on redo stack will not be consistent with new reconciled state, so clear all the redo info
			foreach (var stack in updatedStacks)
				stack.ClearRedoStack();
			if (m_pendingReconciliation != null)
			{
				m_pendingReconciliation.ReconcileForeignChanges();
				m_pendingReconciliation = null;
			}
		}

		/// <summary>
		/// A hook for testing.
		/// </summary>
		internal IReconcileChanges Reconciler( List<ICmObjectSurrogate> foreignNewbies,
			List<ICmObjectSurrogate> foreignDirtballs, List<ICmObjectId> foreignGoners)
		{
			return new ChangeReconciler(this, foreignNewbies, foreignDirtballs, foreignGoners);
		}

		public void GatherChanges(HashSet<ICmObjectId> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			foreach (FdoUnitOfWork currentBundle in UnsavedUnitsOfWork)
				currentBundle.GatherChanges(newbies, dirtballs, goners);
			var transients = new List<ICmObjectId>();
			foreach (var id in goners)
			{
				// If something is deleted we don't need to record modifications to it.
				// (Sequence is not important: if it is deleted there can't be subsequent modifications.)
				var idSurrogateWrapper = new IdSurrogateWrapper(id);
				dirtballs.Remove(idSurrogateWrapper);
				// If an object is both created AND deleted in the same set of changes, we can forget all about it.
				// This is true both normally and if we undid deleting it, then went on to undo creating it.
				// (note that we already removed it from dirtballs, since it is in goners).
				if (newbies.Contains(id))
					transients.Add(id);
			}
			foreach (var id in transients)
			{
				newbies.Remove(id);
				goners.Remove(id);
			}
			// We also don't need to record modifications to new objects.
			foreach (var id in newbies)
			{
				var idSurrogateWrapper = new IdSurrogateWrapper(id);
				dirtballs.Remove(idSurrogateWrapper);
			}
		}

		/// <summary>
		/// Get the unsaved UOWs from all the undo stacks, in the order they occurred.
		/// </summary>
		public List<FdoUnitOfWork> UnsavedUnitsOfWork
		{
			get
			{
				var result = new List<FdoUnitOfWork>();
				foreach (var stack in m_undoStacks)
					result.AddRange(stack.UnsavedUnitsOfWork);
				return result;
			}
		}

		/// <summary>
		/// Delegate impl
		/// </summary>
		/// <param name="subscriber"></param>
		/// <returns></returns>
		internal bool SubscriberCanReceivePropChangeCallDelegate(IVwNotifyChange subscriber)
		{
			return m_changeWatchers.Contains(subscriber);
		}

		#region IVwNotifyChange handling (provides this Mediator service for ISILDataAccess)

		/// <summary>
		/// Request notification when properties change. The ${IVwNotifyChange#PropChanged}
		/// method will be called when the property changes (provided the client making the
		/// change properly calls ${#PropChanged}.
		///</summary>
		/// <param name='nchng'> </param>
		[ComVisible(false)]
		void ISilDataAccessHelperInternal.AddNotification(IVwNotifyChange nchng)
		{
			m_changeWatchers.Add(nchng);
		}

		/// <summary> Request removal from the list of objects to notify when properties change. </summary>
		/// <param name='nchng'> </param>
		[ComVisible(false)]
		void ISilDataAccessHelperInternal.RemoveNotification(IVwNotifyChange nchng)
		{
			m_changeWatchers.Remove(nchng);
		}

		#endregion IVwNotifyChange handling (provides this Mediator service for ISILDataAccess)

		#region Other methods

		/// <summary>
		/// Broadcast the change(s) to the listeners.
		/// </summary>
		internal void SendPropChangedNotifications(IEnumerable<ChangeInformation> changesEnum)
		{
			// This is very likely to modify the UI, so if the UOW is not being done on the UI thread, we need
			// to do at least this part on that thread. One known case is a task being done in the background
			// thread of ProgressDialogWithTask.
			m_ui.SynchronizeInvoke.Invoke(() =>
			{
				var subscribers = m_changeWatchers.ToArray();
				var changes = changesEnum.ToList();
				foreach (var sub in subscribers)
				{
					if (sub is IBulkPropChanged)
						((IBulkPropChanged)sub).BeginBroadcastingChanges(changes.Count);
				}
				foreach (ChangeInformation change in changes)
				{
					change.BroadcastChanges(SubscriberCanReceivePropChangeCallDelegate, subscribers);
				}
				foreach (var sub in subscribers)
				{
					if (sub is IBulkPropChanged)
						((IBulkPropChanged)sub).EndBroadcastingChanges();
				}
			});
		}

		/// <summary>
		/// Do common stuff for adding an action.
		/// </summary>
		/// <param name="stateChanged"></param>
		private void RegisterCommon(IUndoAction stateChanged)
		{
			if (CurrentProcessingState != FdoBusinessTransactionState.ProcessingDataChanges)
				throw new InvalidOperationException("Not in the right state to register a change.");

			m_activeUndoStack.AddActionInternal(stateChanged);
		}

		/// <summary>
		/// Return true if the object is newly created this UOW. Virtual PropChange information
		/// is typically not needed in this case.
		/// </summary>
		public bool IsNew(ICmObject obj)
		{
			return m_activeUndoStack != null && m_activeUndoStack.IsNew(obj);
		}

		/// <summary>
		/// Add a newly created object to the Undo/Redo system.
		/// </summary>
		/// <param name="newby"></param>
		void IUnitOfWorkService.RegisterObjectAsCreated(ICmObject newby)
		{
			// TODO: Factory should add it to the map, eventually(?).
			// TODO: Maybe the factory would add it to the map, and call this method to add it to the UOW.
			// TODO: But, we can't use factories, until there is no other way to create stuff,
			// TODO: since it would be so easy to not get it registered.
			m_identityMap.RegisterObjectAsCreated(newby);
			RegisterCommon(new FdoStateChangeObjectCreation(newby));

			ObjectRepository.RegisterObjectAsCreated(newby);

			// At this point, we assume the new object is not fully initialized
			// and the it will have some of its properties modified.
			// Therefore, we should not fire any PropChanged calls
			// when those properties change. We really only need one PropChanged call
			// on the owning property of 'newby'.
			// (TODO: What needs to be done for unowned objects?)
		}

		/// <summary>
		/// Add a newly deleted object to the Undo/Redo system.
		/// </summary>
		/// <param name="goner"></param>
		/// <param name="xmlStateBeforeDeletion">Old before State XML (in case it gets undeleted).</param>
		void IUnitOfWorkService.RegisterObjectAsDeleted(ICmObject goner, string xmlStateBeforeDeletion)
		{
			RegisterCommon(new FdoStateChangeObjectDeletion(goner, xmlStateBeforeDeletion));
			// TODO: Repository should do this. eventually(?).
			// TODO: The Repository would then call this to get it into the UOW.
			// TODO: But, we can't use factories, until there is no other way to create stuff,
			// TODO: since it would be so easy to not get it registered.
			m_identityMap.UnregisterObject(goner);
		}

		/// <summary>
		/// Register an object as having its ownership changed.  This won't cause either an Undo or Redo, or
		/// a PropChanged, but will cause the modified XML to be written out.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="ownerBeforeChange">Owner prior to this change</param>
		/// <param name="owningFlidBeforeChange">Owning field id prior to this change</param>
		/// <param name="owningFlid">Owning field id after change</param>
		void IUnitOfWorkService.RegisterObjectOwnershipChange(ICmObject dirtball, Guid ownerBeforeChange,
			int owningFlidBeforeChange, int owningFlid)
		{
			// Only register Ownership change if both original and new owner are valid.
			if (ownerBeforeChange != Guid.Empty && dirtball.Owner != null)
				RegisterCommon(new FdoOwnerChanged(dirtball, ownerBeforeChange, owningFlidBeforeChange,
					dirtball.Owner.Guid, owningFlid));
			else
				m_activeUndoStack.m_currentBundle.RegisterObjectAsModified(dirtball);
		}

		/// <summary>
		///
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, Guid[] originalValue, Guid[] newValue)
		{
			RegisterCommon(new FdoVectorPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, Guid[] originalValue, Guid[] newValue)
		{
			RegisterCommon(new FdoVectorVirtualChanged(dirtball, modifiedFlid, originalValue, newValue));
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
		void IUnitOfWorkService.RegisterVirtualCollectionAsModified<T>(ICmObject dirtball, int modifiedFlid, Func<IEnumerable<T>> reader,
			IEnumerable<T> added, IEnumerable<T> removed)
		{
			RegisterCommon(new FdoVirtualReferenceCollectionChangedAction<T>(dirtball, modifiedFlid, reader, added, removed));
		}

		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, IEnumerable<ICmObject> newValue)
		{
			int flid = dirtball.Cache.MetaDataCacheAccessor.GetFieldId2(dirtball.ClassID, virtualPropName, true);
			Guid[] newGuids = (from obj in newValue select obj.Guid).ToArray();
			((IUnitOfWorkService)this).RegisterVirtualAsModified(dirtball, flid, new Guid[0], newGuids);
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, Guid originalValue, Guid newValue)
		{
			RegisterCommon(new FdoGuidPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having a modified atomic reference.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModifiedRef(ICmObject dirtball, int modifiedFlid, Guid originalValue, Guid newValue)
		{
			RegisterCommon(new FdoAtomicRefPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, ITsString originalValue, ITsString newValue)
		{
			RegisterCommon(new FdoTsStringPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, ITsString originalValue, ITsString newValue)
		{
			RegisterCommon(new FdoTsStringVirtualChanged(dirtball, modifiedFlid, originalValue, newValue));
		}
		/// <summary>
		/// Register an object as having experienced a modification of a TsString virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, ITsString newValue)
		{
			int flid = dirtball.Cache.MetaDataCacheAccessor.GetFieldId2(dirtball.ClassID, virtualPropName, true);
			((IUnitOfWorkService)this).RegisterVirtualAsModified(dirtball, flid, null, newValue);
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="ws">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, int ws, ITsString originalValue, ITsString newValue)
		{
			RegisterCommon(new FdoMultiTsStringPropertyChanged(dirtball, modifiedFlid, ws, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having experienced a modification of a multistring virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, int ws, ITsString originalValue, ITsString newValue)
		{
			RegisterCommon(new FdoMultiTsStringVirtualChanged(dirtball, modifiedFlid, ws, originalValue, newValue));
		}

		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, int ws)
		{
			int flid = dirtball.Cache.MetaDataCacheAccessor.GetFieldId2(dirtball.ClassID, virtualPropName, true);
			((IUnitOfWorkService)this).RegisterVirtualAsModified(dirtball, flid, ws, null, null);
		}
		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, ITsTextProps originalValue, ITsTextProps newValue)
		{
			RegisterCommon(new FdoTsTextPropsPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, string originalValue, string newValue)
		{
			RegisterCommon(new FdoStringPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, string originalValue, string newValue)
		{
			RegisterCommon(new FdoStringVirtualChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, bool originalValue, bool newValue)
		{
			RegisterCommon(new FdoBooleanPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Simply register the object as being modified (e.g., we need to write it out because WS tags have changed).
		/// </summary>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball)
		{
			if (CurrentProcessingState != FdoBusinessTransactionState.ProcessingDataChanges)
				throw new InvalidOperationException("Not in the right state to register a change.");

			m_activeUndoStack.m_currentBundle.RegisterObjectAsModified(dirtball);
		}

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, bool originalValue, bool newValue)
		{
			RegisterCommon(new FdoBooleanVirtualChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, DateTime originalValue, DateTime newValue)
		{
			RegisterCommon(new FdoDateTimePropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, byte[] originalValue, byte[] newValue)
		{
			RegisterCommon(new FdoBinaryPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, int originalValue, int newValue)
		{
			RegisterCommon(new FdoIntegerPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, int originalValue, int newValue)
		{
			RegisterCommon(new FdoIntegerVirtualChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, object originalValue, object newValue)
		{
			RegisterCommon(new FdoCustomPropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void IUnitOfWorkService.RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, GenDate originalValue, GenDate newValue)
		{
			RegisterCommon(new FdoGenDatePropertyChanged(dirtball, modifiedFlid, originalValue, newValue));
		}

		/// <summary>
		/// Registers an object as containing data for a custom field that was modified or deleted.
		/// </summary>
		/// <param name="dirtball">The modified object.</param>
		/// <param name="modifiedFlid">The modified flid.</param>
		void IUnitOfWorkService.RegisterCustomFieldAsModified(ICmObject dirtball, int modifiedFlid)
		{
			RegisterCommon(new FdoStateChangeCustomFieldDefnModified(dirtball, modifiedFlid));
		}

		#endregion Other methods

		#region Implementation of IWorkerThreadReadHandler

		/// <summary>
		/// Begins a read task on a worker thread.
		/// </summary>
		public void BeginReadTask()
		{
			m_lock.EnterReadLock();
		}

		/// <summary>
		/// Ends a read task on a worker thread.
		/// </summary>
		public void EndReadTask()
		{
			m_lock.ExitReadLock();
		}

		#endregion

		#region Implementation of IUndoStackManager

		/// <summary>
		/// Make one (and remember it!). Don't make it current.
		/// </summary>
		/// <returns></returns>
		public IActionHandler CreateUndoStack()
		{
			var result = new UndoStack(this, m_ui);
			m_undoStacks.Add(result);
			return result;
		}

		/// <summary>
		/// Make it current (but not necessarily active).
		/// </summary>
		/// <param name="stack"></param>
		public void SetCurrentStack(IActionHandler stack)
		{
			if (stack == m_currentUndoStack)
				return;
			m_currentUndoStack = (UndoStack)stack;

			if (CurrentProcessingState == FdoBusinessTransactionState.ReadyForBeginTask)
			{
				m_activeUndoStack = m_currentUndoStack;
			}
		}

		/// <summary>
		/// Get rid of one no longer to be used.
		/// </summary>
		/// <param name="stack"></param>
		public void DisposeStack(IActionHandler stack)
		{
			if (stack == CurrentUndoStack)
				throw new ArgumentException("Must set a different undo stack before disposing");
			m_undoStacks.Remove((UndoStack)stack);
		}

		#endregion

		internal bool HasConflictingUndoChanges(FdoUnitOfWork itemToUndo)
		{
			foreach (var stack in m_undoStacks)
				if (stack.HasConflictingUndoChanges(itemToUndo))
					return true;
			return false;
		}

		internal bool HasConflictingRedoChanges(FdoUnitOfWork itemToUndo)
		{
			foreach (var stack in m_undoStacks)
				if (stack.HasConflictingRedoChanges(itemToUndo))
					return true;
			return false;
		}

		/// <summary>
		/// Expose this capability to the ChangeReconciler; not a normal part of this class's function.
		/// </summary>
		internal ICmObject GetObject(ICmObjectId id)
		{
			return m_identityMap.GetObject(id);
		}

		/// <summary>
		/// Expose this capability to the ChangeReconciler; not a normal part of this class's function.
		/// </summary>
		internal ICmObject GetObject(Guid id)
		{
			return m_identityMap.GetObject(id);
		}

		/// <summary>
		/// Expose this capability to the ChangeReconciler; not a normal part of this class's function.
		/// </summary>
		internal bool HasObject(Guid id)
		{
			return m_identityMap.HasObject(id);
		}
	}
}

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	///
	/// </summary>
	/// <param name="subscriber"></param>
	internal delegate bool SubscriberCanReceivePropChangeCallDelegate(IVwNotifyChange subscriber);

	/// <summary>
	/// Class the consolidates changes that need to be propagated out to IVwNotifyChange listeners.
	/// </summary>
	internal sealed class ChangeInformation
	{
		private readonly IPropertyChangeNotifier m_notifier;
		private readonly int m_hvo;
		private readonly int m_tag;
		private readonly int m_ivMin;
		private readonly int m_cvIns;
		private readonly int m_cvDel;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name='changedObject'>The object that changed </param>
		/// <param name='tag'>The property that changed </param>
		/// <param name='ivMin'>
		/// For vectors, the starting index where the change occurred.
		/// For MultiStrings, the writing system where the change occurred.
		/// </param>
		/// <param name='cvIns'>
		/// For vectors, the number of items inserted.
		/// For atomic objects, 1 if an item was added.
		/// Otherwise (including basic properties), 0.
		/// </param>
		/// <param name='cvDel'>
		/// For vectors, the number of items deleted.
		/// For atomic objects, 1 if an item was deleted.
		/// Otherwise (including basic properties), 0.
		/// </param>
		/// ------------------------------------------------------------------------------------
		internal ChangeInformation(ICmObject changedObject, int tag, int ivMin, int cvIns, int cvDel) :
			this (changedObject.Hvo, tag, ivMin, cvIns, cvDel, changedObject as IPropertyChangeNotifier)
		{
		}

		private ChangeInformation(int hvo, int tag, int ivMin, int cvIns, int cvDel, IPropertyChangeNotifier notifier)
		{
			m_hvo = hvo;
			m_tag = tag;
			m_ivMin = ivMin;
			m_cvIns = cvIns;
			m_cvDel = cvDel;
			m_notifier = notifier;
		}

		internal void BroadcastChanges(SubscriberCanReceivePropChangeCallDelegate subscriberChecker, IVwNotifyChange[] subscribers)
		{
			if (subscriberChecker == null) throw new ArgumentNullException("subscriberChecker");
			if (subscribers == null) throw new ArgumentNullException("subscribers");

			// Unfortunately, it is possible for the set of subscribers to change in the
			// middle of this loop (subscribers could be added or removed in a PropChanged
			// call), so we use a copy of the original set (the input array) and loop over the array copy.
			//
			// PropChanged should not be called on any subscribers that are added or
			// removed (thus the callback to the delegate) during this loop.
			foreach (var subscriber in subscribers)
			{
				if (subscriberChecker(subscriber))
					subscriber.PropChanged(m_hvo, m_tag, m_ivMin, m_cvIns, m_cvDel);
			}
			if (m_notifier != null)
				m_notifier.NotifyOfChangedProperty(m_tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the change for undo (i.e., with the inserted and deleted counts swapped)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ChangeInformation ChangeForUndo
		{
			get { return new ChangeInformation(m_hvo, m_tag, m_ivMin, m_cvDel, m_cvIns, m_notifier); }
		}
	}
}
