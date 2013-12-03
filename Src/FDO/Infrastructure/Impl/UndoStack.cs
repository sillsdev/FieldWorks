// --------------------------------------------------------------------------------------------
// Copyright (C) 2008 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: FdoActionHandler.cs
// Responsibility: Randy Regnier
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.Utils;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainImpl;

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
	///		Calls to the ISILDataAccess 'PropChanged' method before the 'EndUndoTask' method is called
	///		probably are errors of logic of some sort. They can either trigger an exception immediately (my preference),
	///		or they can be queued up for processing here.
	///
	///		PropChanged calls that are received during the execution of Step 2.B,
	///		are probably errors in logic, as well, or are probably intended to trigger
	///		other data changes. In this system, usage of the PropChanged notification system as a way to trigger
	///		side effects is illegal and will throw an exception.
	///
	///		For cases where a new CmObject is created,
	///		there is no need for broadcasting PropChanges when its properties are also set.
	///		There is probably no need to create and store IUndoAcitons for these either.
	///		The reason for this is nothing can be displaying the new object,
	///		so there is no reason to bother the views code with the 'noise'.
	///
	/// 3. Session logging: This class will maintain an XML file that logs changes made to a language project
	///		during a session (run of the application program).
	///		The log file will be updated on each Commit call. That is, it will be created on the first commit call,
	///		and subsequent calls will append the new set of changes.
	///
	///		This log file will be suitable for use to update remote user machines.
	/// </remarks>
	[ComVisible(true)]
	internal sealed class UndoStack : IActionHandler, IActionHandlerExtensions
	{
		#region Data members
		private readonly Stack<FdoUnitOfWork> m_undoBundles;
		private readonly Stack<FdoUnitOfWork> m_redoBundles;
		private readonly Dictionary<int, int> m_markIndexes = new Dictionary<int, int>();
		private int m_currentMarkHandle;
		private bool m_createMarkIfNeeded;
		internal FdoUnitOfWork m_currentBundle;
		private readonly UnitOfWorkService m_uowService;
		private readonly IFdoUserAction m_userAction;
		// Positive values count unsaved bundles in m_undoBundles.
		// Negative values count unsaved bundles in m_redoBundles, that is, things Undone since the last Save.
		private int m_countUnsavedBundles;

		private List<Action> m_actionsToDoAtEndOfPropChanged = new List<Action>();
		#endregion

		#region Constructor
		public UndoStack(UnitOfWorkService uowService, IFdoUserAction userAction)
		{
			m_uowService = uowService;
			m_userAction = userAction;
			m_undoBundles = new Stack<FdoUnitOfWork>();
			m_redoBundles = new Stack<FdoUnitOfWork>();
			m_currentBundle = null;

		}
		#endregion

		#region Internal/private methods
		internal IEnumerable<FdoUnitOfWork> RedoableUnitsOfWork
		{
			get { return m_redoBundles; }
		}

		internal IEnumerable<FdoUnitOfWork> UnsavedUnitsOfWork
		{
			get
			{
				if (m_countUnsavedBundles > 0)
					return m_undoBundles.Take(m_countUnsavedBundles);
				return from uow in m_redoBundles.Take(-m_countUnsavedBundles) select uow.InverseObjectChanges;
			}
		}

		/// <summary>
		/// Record that the database has been saved in the current state.
		/// </summary>
		internal void RecordSaved()
		{
			m_countUnsavedBundles = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this stack has unsaved changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool HasUnsavedChanges
		{
			get { return m_countUnsavedBundles != 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Either Undo or Redo, to get one UOW closer to having no unsaved work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void RevertUnsavedUnitOfWork()
		{
			if (m_countUnsavedBundles > 0)
				Undo();
			else
				Redo();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears all marks and bundles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Clear()
		{
			m_currentBundle = null;
			ClearAllMarks();
			m_undoBundles.Clear();
			m_redoBundles.Clear();
		}
		#endregion

		#region IActionHandler implementation

		/// <summary>
		/// Begin a sequence of actions that will be treated as one task for the purposes
		/// of undo and redo. If there is already such a task in process, this sequence will be
		/// included (nested) in that one, and the descriptive strings will be ignored.
		///</summary>
		/// <param name='bstrUndo'>Short description of an action. This is intended to appear on the
		/// "undo" menu item (e.g. "Typing" or "Clear") </param>
		/// <param name='bstrRedo'>Short description of an action. This is intended to appear on the
		/// "redo" menu item (e.g. "Typing" or "Clear"). Usually, this is the same as &lt;i&gt;bstrUndo&lt;/i&gt; </param>
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			if (this != m_uowService.ActiveUndoStack)
			{
				m_uowService.ActiveUndoStack.BeginUndoTask(bstrUndo, bstrRedo);
				return;
			}
			CheckNotProcessingDataChanges("Nested tasks are not supported.");
			CheckNotBroadcastingPropChanges("Can't start new task, while broadcasting PropChanges.");
			CheckNotInUndoRedo();

			m_uowService.CurrentProcessingState = UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges;
			m_uowService.m_lock.EnterWriteLock();
			m_currentBundle = new FdoUndoableUnitOfWork(m_uowService, bstrUndo, bstrRedo);
		}

		private void CheckNotInUndoRedo()
		{
			if (m_uowService.UndoOrRedoInProgress)
				throw new InvalidOperationException("Cannot start a UOW while an Undo or Redo is in progress");
		}

		private void CheckNotProcessingDataChanges(string message)
		{
			if (m_uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges)
			{
				Rollback(0);
				throw new InvalidOperationException(message);
			}
		}

		private void CheckNotBroadcastingPropChanges(string message)
		{
			if (m_uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.BroadcastingPropChanges)
			{
				// Can't roll back here because we're not in the right state. Shouldn't have to, since presumably the UOW
				// was completed successfully. This exception is most likely caused by a side-effect of prop-changed handling
				// incorrectly trying to make a data change or mess with the marks.
				throw new InvalidOperationException(message);
			}
		}

		private void CheckNotReadyForBeginTask(string message)
		{
			if (m_uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.ReadyForBeginTask)
			{
				// Can't roll back here because we're not in the right state. Shouldn't have to, since presumably the previous
				// UOW (if any) was completed successfully.
				throw new InvalidOperationException(message);
			}
		}

		internal void CheckReadyForCommit(string message)
		{
			if (m_uowService.CurrentProcessingState != UnitOfWorkService.FdoBusinessTransactionState.ReadyForBeginTask)
			{
				Rollback(0);
				throw new InvalidOperationException(message);
			}
		}
		internal UnitOfWorkService UowService { get { return m_uowService; } }

		/// <summary>
		/// Begin a sequence of non-undoable actions.
		///</summary>
		public void BeginNonUndoableTask()
		{
			if (this != m_uowService.ActiveUndoStack)
			{
				m_uowService.ActiveUndoStack.BeginNonUndoableTask();
				return;
			}
			CheckNotProcessingDataChanges("Nested tasks are not supported.");
			CheckNotBroadcastingPropChanges("Can't start new task, while broadcasting PropChanges.");
			CheckNotInUndoRedo();
			m_uowService.CurrentProcessingState = UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges;
			m_uowService.m_lock.EnterWriteLock();
			m_currentBundle = new FdoNonUndoableUnitOfWork(m_uowService);
		}

		/// <summary>
		/// End the current task sequence.
		///</summary>
		public void EndNonUndoableTask()
		{
			if (this != m_uowService.ActiveUndoStack)
				m_uowService.ActiveUndoStack.EndNonUndoableTask();
			else
				EndUndoTaskCommon(false);
		}

		/// <summary>
		/// End the current task sequence.
		///</summary>
		public void EndUndoTask()
		{
			if (this != m_uowService.ActiveUndoStack)
				m_uowService.ActiveUndoStack.EndUndoTask();
			else
				EndUndoTaskCommon(true);
		}

		private void EndUndoTaskCommon(bool updateDateModified)
		{
			if (m_uowService.CurrentProcessingState != UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges)
				throw new InvalidOperationException("Cannot end task that has not been started.");

			if (updateDateModified)
			{
				// A generic side effect of all changes is to update DateModified.
				// Collect the objects we want to record the modify time on. Don't do each as found:
				// Updating them will add new dirtballs, which will mess up the DirtyObjects iterator.
				// Also, we don't need the overhead of updating an object repeatedly if it has several changes.
				var collector = new HashSet<ICmObjectInternal>();
				foreach (var item in m_currentBundle.DirtyObjects)
				{
					item.CollectDateModifiedObject(collector);
				}
				// Don't update the modify time on new objects, it should be near enough, and Undo will fail
				// trying to restore the modify time on the deleted object.
				var newObjects = m_currentBundle.NewObjects;
				foreach (var dmObj in collector)
				{
					if (!newObjects.Contains(dmObj.Id) && !m_currentBundle.IsDateModifiedExplicitly(dmObj))
						dmObj.UpdateDateModified();
				}
				// Update the project DateModified, but only once every 2 minutes at most.
				if (m_currentBundle.DirtyObjects.Count() > 0)
				{
					LangProject proj = m_currentBundle.DirtyObjects.ElementAt(0).Cache.LangProject as LangProject;
					TimeSpan span = new TimeSpan(DateTime.Now.Ticks - proj.DateModified.Ticks);
					if (span.Minutes >= 2 || m_currentBundle.DirtyObjects.Contains(proj))
						proj.UpdateDateModifiedInternal();
				}
			}

			m_uowService.m_lock.ExitWriteLock();

			m_uowService.CurrentProcessingState = UnitOfWorkService.FdoBusinessTransactionState.BroadcastingPropChanges;

			if (m_currentBundle.HasDataChange)
			{
				// Can't redo these now.
				// If m_currentBundle can't be finished well,
				// then it can all be rolled back
				ClearRedoStack();
				PushUowOnUndoStack(m_currentBundle);
				m_currentBundle.SetAfterXml();

				m_uowService.SuppressSelections = true;

				try
				{
					// Handle Step 2.B (PropChanged calls) here.
					// Do them here because we may not commit yet.
					// 2.B can be moved after the optional save, but then rethink the states.
					m_uowService.SendPropChangedNotifications(m_currentBundle.GetPropChangeInformation(false));
				}
				finally
				{
					m_uowService.SuppressSelections = false;
				}
			}

			var oldCurrentBundle = m_currentBundle;
			m_currentBundle = null;

			m_uowService.CurrentProcessingState = UnitOfWorkService.FdoBusinessTransactionState.ReadyForBeginTask;

			// Do this after we are back in a safe state to do a new UOW, if necessary.
			DoTasksForEndOfPropChanged(oldCurrentBundle, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the prop changed completed event.
		/// </summary>
		/// <param name="uow">The unit of work we are doing, undoing, or redoing.</param>
		/// <param name="fromUndoRedo">True if the method was called for an undo or
		/// redo, false for the original action.</param>
		/// ------------------------------------------------------------------------------------
		private void DoTasksForEndOfPropChanged(FdoUnitOfWork uow, bool fromUndoRedo)
		{
			m_userAction.SynchronizeInvoke.Invoke(() =>
				{
					if (!fromUndoRedo)
					{
						// These notifications must be sent only once.
						// In particular they must not be sent again if one of the tasks in the list makes a new UOW.
						// Since this method will execute at the end of such a task, we must make sure that there is
						// a fresh m_actionsToDoAtEndOfPropChanged for any such UOW so it will not see our list.
						var temp = m_actionsToDoAtEndOfPropChanged;
						m_actionsToDoAtEndOfPropChanged = new List<Action>();
						foreach (var task in temp)
							task();
					}
					foreach (var task in uow.ActionsToDoAtEndOfPropChanged)
						task();
				});
		}

		/// <summary>
		/// Continue the previous sequence. This is intended to be called from a place like
		/// OnIdle that performs "cleanup" operations that are really part of the previous
		/// sequence.
		///</summary>
		public void ContinueUndoTask()
		{
			throw new NotSupportedException("'ContinueUndoTask' is not supported.");
		}

		/// <summary>
		/// End the current sequence, and any outer ones that are in progress. This is intended
		/// to be used as a cleanup function to get everything back in sync.
		///</summary>
		public void EndOuterUndoTask()
		{
			throw new NotSupportedException("'EndOuterUndoTask' is not supported.");
		}

		/// <summary>
		/// Break the current undo task into two at the current point. Subsequent actions will
		/// be part of the new task which will be assigned the given labels.
		///</summary>
		/// <param name='bstrUndo'> </param>
		/// <param name='bstrRedo'> </param>
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			if (this != m_uowService.ActiveUndoStack)
			{
				m_uowService.ActiveUndoStack.BreakUndoTask(bstrUndo, bstrRedo);
				return;
			}
			EndUndoTask();
			BeginUndoTask(bstrUndo, bstrRedo);
		}

		/// <summary>
		/// Sets a flag which makes the ActionHandler create a mark if there is no mark and
		/// another action is added to the stack.
		///</summary>
		/// <param name='fCreateMark'> </param>
		public void CreateMarkIfNeeded(bool fCreateMark)
		{
			m_createMarkIfNeeded = fCreateMark;
		}

		/// <summary>
		/// Begins an action sequence. An action sequence consists of one or more UndoAction's
		/// that constitute a single task (at least, from the user's perspective).
		/// Calling this method requires that an UndoAction be supplied to "seed" the action
		/// sequence.
		///</summary>
		/// <param name='bstrUndo'>Short description of an action. This is intended to appear on the
		/// "undo" menu item (e.g. "Undo Typing") </param>
		/// <param name='bstrRedo'>Short description of an action. This is intended to appear on the
		/// "redo" menu item (e.g. "Redo Typing"). Usually, this is the same as &lt;i&gt;bstrUndo&lt;/i&gt; </param>
		/// <param name='uact'>Pointer to an IUndoAction interface. This is the first action of an
		/// action sequence. </param>
		public void StartSeq(string bstrUndo, string bstrRedo, IUndoAction uact)
		{
			throw new NotSupportedException("'StartSeq' is not supported.");
		}

		/// <summary>
		/// Adds an UndoAction to the current action sequence. An action sequence
		/// <b>MUST</b> already be started before an additional UndoAction can be added.
		///</summary>
		/// <param name='uact'>Pointer to an UndoAction interface. This is NEVER the
		/// first action of an action sequence. </param>
		public void AddAction(IUndoAction uact)
		{
			if (this != m_uowService.ActiveUndoStack)
			{
				m_uowService.ActiveUndoStack.AddAction(uact);
				return;
			}
			CheckNotReadyForBeginTask("'BeginUndoTask' must be called first.");
			CheckNotBroadcastingPropChanges("Can't add new actions while broadcasting PropChanges.");

			if (uact is FdoStateChangeBase)
				throw new ArgumentException("Can't feed that kind of IUndoAction in to the system from outside.");

			AddActionInternal(uact);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an UndoAction to the current action sequence.
		/// </summary>
		/// <param name="uact">The UndoAction to add.</param>
		/// ------------------------------------------------------------------------------------
		internal void AddActionInternal(IUndoAction uact)
		{
			if (m_createMarkIfNeeded && m_markIndexes.Count == 0)
				Mark();

			// I don't need any of these for normal FDO data changes.
			// All CmObject changes are handled by other means.
			// But, we'll store whatever shows up, and hope it knows what it's doing.
			m_currentBundle.AddAction(uact);
		}

		/// <summary>
		/// Return true if there is a current UOW and the object was created during it.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		internal bool IsNew(ICmObject obj)
		{
			return m_currentBundle != null && m_currentBundle.IsNew(obj);
		}

		/// <summary>
		/// Returns the "undo" description of the current action sequence, as was given in
		/// the ${IActionHandler#StartSeq} call.
		///</summary>
		/// <returns>"Undo" description of the current action sequence.</returns>
		public string GetUndoText()
		{
			return (m_undoBundles.Count > 0) ? m_undoBundles.Peek().UndoText : Strings.ksNothingToUndo;
		}

		/// <summary>
		/// Returns the "undo" description of the specified action sequence, as was given in
		/// the ${IActionHandler#StartSeq} call.
		///</summary>
		/// <param name='iAct'>Zerobased index of the undo action </param>
		/// <returns>"Undo" description of the current action sequence.</returns>
		public string GetUndoTextN(int iAct)
		{
			return m_undoBundles.ToArray()[m_undoBundles.Count - iAct - 1].UndoText;
		}

		/// <summary>
		/// Returns the "redo" description of the action sequence that would be redone,
		/// as was given in the ${IActionHandler#StartSeq} call. This is usually the same
		/// as the "undo" description.
		///</summary>
		/// <returns>"Redo" description of the current action sequence.</returns>
		public string GetRedoText()
		{
			return (m_redoBundles.Count > 0) ? m_redoBundles.Peek().RedoText : Strings.ksNothingToRedo;
		}

		/// <summary>
		/// Returns the "redo" description of the specified action sequence, as was given in
		/// the ${IActionHandler#StartSeq} call. This is usually the same as the "undo"
		/// description.
		///</summary>
		/// <param name='iAct'>Zerobased index of the redo action </param>
		/// <returns>"Redo" description of the current action sequence.</returns>
		public string GetRedoTextN(int iAct)
		{
			return m_redoBundles.ToArray()[iAct].RedoText;
		}

		/// <summary> Indicates if there is an action sequence on the stack that can be undone. </summary>
		public bool CanUndo()
		{
			return m_undoBundles.Count > 0 && !m_uowService.HasConflictingUndoChanges(m_undoBundles.Peek())
				&& m_undoBundles.Peek().CanUndo((ICmObjectRepository)m_uowService.ObjectRepository);
		}

		/// <summary>
		/// This undo stack has a change that conflicts with itemToUndo if it has an undoable item with a later
		/// sequence number that is affected by undoing itemToUndo.
		/// </summary>
		internal bool HasConflictingUndoChanges(FdoUnitOfWork itemToUndo)
		{
			// Stack enumeration is in reverse order, so we get the most recent changes first.
			foreach (var other in m_undoBundles)
			{
				// If it's equal we're comparing with ourself, which should not be considered a conflict!
				if (other.Sequence <= itemToUndo.Sequence)
					return false; // this and therefore all remaining changes are earlier than the one we are testing
				if (other.IsAffectedByUndoing(itemToUndo))
					return true;
			}
			return false;
		}

		/// <summary> Indicates if there is an action sequence on the stack that can be redone. </summary>
		public bool CanRedo()
		{
			return m_redoBundles.Count > 0 && !m_uowService.HasConflictingRedoChanges(m_redoBundles.Peek())
				&& m_redoBundles.Peek().CanRedo((ICmObjectRepository)m_uowService.ObjectRepository);
		}

		/// <summary>
		/// This undo stack has a change that conflicts with itemToUndo if it has a redoable item with an earlier
		/// sequence number that is affected by redoing itemToRedo.
		/// </summary>
		internal bool HasConflictingRedoChanges(FdoUnitOfWork itemToRedo)
		{
			// Stack enumeration is in reverse order, so we get the first change we might redo first.
			foreach (var other in m_redoBundles)
			{
				// If it's equal we're comparing with ourself, which should not be considered a conflict!
				if (other.Sequence >= itemToRedo.Sequence)
					return false; // this and therefore all remaining changes are later than the one we are testing
				if (other.IsAffectedByRedoing(itemToRedo))
					return true;
			}
			return false;
		}
		/// <summary>
		/// Undoes an action sequence. This can involve the reversal of several
		/// UndoAction's.
		///</summary>
		/// <returns></returns>
		public UndoResult Undo()
		{
			if (!CanUndo())
				throw new InvalidOperationException("Can't undo");

			if (DoingUndoOrRedo != null)
			{
				var arg = new CancelEventArgs();
				DoingUndoOrRedo(arg);
				if (arg.Cancel)
					return UndoResult.kuresError;
			}

			m_uowService.UndoOrRedoInProgress = true;
			m_uowService.ObjectRepository.ClearCachesOnUndoRedo();
			UndoResult res;
			var undoBundle = m_undoBundles.Peek();
			try
			{
				m_uowService.m_lock.EnterWriteLock();
				try
				{
					// Undo it all.
					res = undoBundle.Undo();
				}
				finally
				{
					m_uowService.m_lock.ExitWriteLock();
				}

				// Move it to the redo stack.
				m_redoBundles.Push(m_undoBundles.Pop());
				m_countUnsavedBundles--; // This is where it can go negative, if we're undoing a saved change

				ValidateExistingMarksAfterUndo();
			}
			finally
			{
				m_uowService.UndoOrRedoInProgress = false;
			}
			DoTasksForEndOfPropChanged(undoBundle, true);

			return res;

		}

		/// <summary>
		/// Redoes an action sequence. This can involve the reapplication of
		/// several UndoAction's.
		///</summary>
		/// <returns></returns>
		public UndoResult Redo()
		{
			if (!CanRedo())
				throw new InvalidOperationException("Can't redo");

			if (DoingUndoOrRedo != null)
			{
				var arg = new CancelEventArgs();
				DoingUndoOrRedo(arg);
				if (arg.Cancel)
					return UndoResult.kuresError;
			}

			m_uowService.UndoOrRedoInProgress = true;
			m_uowService.ObjectRepository.ClearCachesOnUndoRedo();
			UndoResult res;
			var redoBundle = m_redoBundles.Peek();
			try
			{
				m_uowService.m_lock.EnterWriteLock();
				try
				{
					// Redo it all.
					res = redoBundle.Redo();
				}
				finally
				{
					m_uowService.m_lock.ExitWriteLock();
				}

				// Move it back to the undo stack.
				m_undoBundles.Push(m_redoBundles.Pop());
				m_countUnsavedBundles++;
			}
			finally
			{
				m_uowService.UndoOrRedoInProgress = false;
			}

			DoTasksForEndOfPropChanged(redoBundle, true);

			return res;
		}

		/// <summary>
		/// Rollback the current UOW.
		///</summary>
		/// <param name='nDepth'>[Not used.]</param>
		/// <exception cref="InvalidOperationException">
		/// Thrown if not in the right state to do
		/// a rollback (in the data change phase of the UOW).
		/// </exception>
		public void Rollback(int nDepth)
		{
			if (this != m_uowService.ActiveUndoStack)
			{
				m_uowService.ActiveUndoStack.Rollback(nDepth);
				return;
			}
			if (m_uowService.CurrentProcessingState != UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges)
				throw new InvalidOperationException("Rollback not supported in the current state.");

			Debug.Assert(m_uowService.m_lock.IsWriteLockHeld, "Trying Rollback without write lock!");
			if (m_uowService.m_lock.IsWriteLockHeld)
				m_uowService.m_lock.ExitWriteLock();
			else
				Logger.WriteEvent("Trying to rollback without write lock!");

			m_currentBundle.Rollback();
			m_currentBundle = null;
			m_actionsToDoAtEndOfPropChanged.Clear(); // don't do them on some subsequent task
			m_uowService.CurrentProcessingState = UnitOfWorkService.FdoBusinessTransactionState.ReadyForBeginTask;
		}

		/// <summary>
		/// Gets the current depth of the nested BeginUndoTask() calls.
		///</summary>
		/// <returns>1 if in undo task, 0 otherwise</returns>
		public int CurrentDepth
		{
			get { return m_uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges ? 1 : 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Irreversably commits all actions that have been executed so far (or at least since
		/// the last Commit) and clears the action stack. (Currently, both undo and
		/// redo actions are cleared).
		/// Use uowService.Save() to persist stuff without clearing the stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Commit()
		{
			if (this != m_uowService.ActiveUndoStack)
			{
				m_uowService.ActiveUndoStack.Commit();
				return;
			}
			// m_currentProcessingState *must* be FdoBusinessTransactionState.ReadyForBeginTask.
			CheckReadyForCommit("Commit at wrong place.");
			m_uowService.Save();
			Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes all actions that are on the (internal) action stack, thereby releasing
		/// references and resources. This should be called just before the application is to
		/// end so that there aren't any circular reference problems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Close()
		{
			Clear();
			m_countUnsavedBundles = 0; // probably not needed, but keep consistent.
			m_uowService.CurrentProcessingState = UnitOfWorkService.FdoBusinessTransactionState.ReadyForBeginTask;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a mark and returns a handle that can be used later to discard all Undo items
		/// back to the mark. Handle will never be zero.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Mark()
		{
			m_markIndexes.Add(++m_currentMarkHandle, m_undoBundles.Count);
			return m_currentMarkHandle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears all marks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearAllMarks()
		{
			m_markIndexes.Clear();
		}

		FdoUnitOfWork PopUndoStack()
		{
			if (m_countUnsavedBundles > 0)
				m_countUnsavedBundles--;
			return m_undoBundles.Pop();
		}

		/// <summary>
		/// Collapses all Undo tasks back to a specified mark and creates a single Undo task for
		/// them all. Also discards the mark.
		///</summary>
		/// <param name='hMark'>The mark handle </param>
		/// <param name='bstrUndo'>Short description of an action. This is intended to appear on the
		/// "undo" menu item (e.g. "Undo Typing") for the task created by collapsing all tasks
		/// following the mark </param>
		/// <param name='bstrRedo'>Short description of an action. This is intended to appear on the
		/// "redo" menu item (e.g. "Redo Typing") for the task created by collapsing all tasks
		/// following the mark. Usually, this is the same as &lt;i&gt;bstrUndo&lt;/i&gt; </param>
		/// <returns><c>true</c> if actions were collapsed; <c>false</c> otherwise</returns>
		public bool CollapseToMark(int hMark, string bstrUndo, string bstrRedo)
		{
			CheckNotProcessingDataChanges("Can't collapse to mark in middle of undo task.");
			CheckNotBroadcastingPropChanges("Can't collapse to mark while broadcasting PropChanges.");
			int markIndex;
			if (hMark <= 0 || !m_markIndexes.TryGetValue(hMark, out markIndex))
				throw new ArgumentException("Invalid mark handle");

			bool fActionsCollapsed = false;
			if (m_undoBundles.Count > markIndex)
			{
				FdoUndoableUnitOfWork newUow = new FdoUndoableUnitOfWork(m_uowService, bstrUndo, bstrRedo);

				while (m_undoBundles.Count > markIndex)
					newUow.InsertActionsFrom(PopUndoStack());

				Debug.Assert(m_undoBundles.Count == markIndex);
				ClearRedoStack();
				PushUowOnUndoStack(newUow);
				fActionsCollapsed = true;
			}
			ValidateExistingMarks(hMark);
			return fActionsCollapsed;
		}

		internal void ClearRedoStack()
		{
			// If some of the things we've undone represent unsaved changes (undone after the most recent Save),
			// transfer them to the non-undoable stack so they will get saved next time.
			if (m_countUnsavedBundles < 0)
			{
				var nonUndoableStack = m_uowService.NonUndoableStack;
				foreach (var uow in m_redoBundles.Take(-m_countUnsavedBundles))
				{
					nonUndoableStack.PushUowOnUndoStack(uow.InverseObjectChanges);
				}
				m_countUnsavedBundles = 0;
				// If we have a current UOW, it's actually later than the phony ones we pushed on the non-undoable
				// stack. In fact, it's later than ANY existing change. We want to reflect this, so that we can
				// still Undo it, even if it conflicts with one of the Undone changes. Re-assigning it the next
				// available sequence number achieves this.
				if (m_currentBundle != null)
					m_currentBundle.ResetSequenceNumber();
			}
			m_redoBundles.Clear();
		}

		internal void AddForeignBundleToUndoStack(FdoNonUndoableUnitOfWork newUow)
		{
			Debug.Assert(m_countUnsavedBundles >= 0); // we should be the non-undoable stack, so no undone unsaved bundles
			// We want to insert the foreign one (which HAS been saved, by the other client) further down the
			// stack than any of our own unsaved bundles.
			var unsavedBundles = new Stack<FdoUnitOfWork>();
			for (int i = 0; i < m_countUnsavedBundles; i++)
				unsavedBundles.Push(m_undoBundles.Pop());
			m_undoBundles.Push(newUow);
			for (int i = 0; i < m_countUnsavedBundles; i++)
				m_undoBundles.Push(unsavedBundles.Pop());
		}


		private void PushUowOnUndoStack(FdoUnitOfWork newUow)
		{
			m_undoBundles.Push(newUow);
			Debug.Assert(m_countUnsavedBundles >= 0);
			m_countUnsavedBundles++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Discard all Undo items back to the specified mark (or the most recent mark, if any,
		/// if handle is zero).
		/// </summary>
		/// <param name="hMark">Handle of the mark</param>
		/// ------------------------------------------------------------------------------------
		public void DiscardToMark(int hMark)
		{
			CheckNotProcessingDataChanges("Nested tasks are not supported.");
			CheckNotBroadcastingPropChanges("Can't start new task, while broadcasting PropChanges.");
			int markIndex;
			if (hMark <= 0 || !m_markIndexes.TryGetValue(hMark, out markIndex))
				throw new ArgumentException("Invalid mark handle");

			while (m_undoBundles.Count > markIndex)
			{
				if (PopUndoStack().HasDataChange)
					throw new InvalidOperationException("Cannot discard an undo action which changed the data. The action should be undone instead.");
			}

			m_redoBundles.Clear();
			ValidateExistingMarks(hMark);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the existing marks by cleaning up any that are larger than the specified mark
		/// (inclusive)
		/// </summary>
		/// <param name="hMark">The mark handle.</param>
		/// ------------------------------------------------------------------------------------
		private void ValidateExistingMarks(int hMark)
		{
			for(int i = hMark; i <= m_currentMarkHandle; i++)
				m_markIndexes.Remove(i);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the existing marks by cleaning up any that are outside the current undo
		/// position
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ValidateExistingMarksAfterUndo()
		{
			foreach (KeyValuePair<int, int> item in m_markIndexes.ToArray())
			{
				if (item.Value > m_undoBundles.Count)
					m_markIndexes.Remove(item.Key);
			}
		}

		/// <summary>
		/// Get the handle to the top mark. If there are no marks on the undo stack, returns 0.
		///</summary>
		/// <returns>A System.Int32 </returns>
		public int TopMarkHandle
		{
			get
			{
				if (m_markIndexes.ContainsValue(m_currentMarkHandle))
					return m_currentMarkHandle;
				int maxHandle = 0;
				foreach (int handle in m_markIndexes.Keys)
					if (handle > maxHandle)
						maxHandle = handle;
				return maxHandle;
			}
		}

		/// <summary>
		/// Return true if there is anything undoable after the top mark (and if there is at
		/// least one mark).
		///</summary>
		/// <param name='fUndo'> </param>
		/// <returns></returns>
		public bool get_TasksSinceMark(bool fUndo)
		{
			int topMark = TopMarkHandle;
			if (topMark == 0)
				return false;
			return (fUndo) ? (m_markIndexes[topMark] < m_undoBundles.Count) :
				(m_redoBundles.Count > 0);
		}

		/// <summary>
		/// Return the number of outstanding Undoable actions. This may be more than the number of
		/// times the user could issue the Undo command, as that depends on individual items
		/// being grouped into sequences. This does not count items that could be redone.
		///</summary>
		public int UndoableActionCount
		{
			get
			{
				int actionCount = m_undoBundles.Sum(uow => uow.Changes.Count);
				actionCount += (m_currentBundle != null ? m_currentBundle.Changes.Count : 0);
				return actionCount;
			}
		}

		/// <summary>
		/// Returns the number of Undoable sequences. This is the number of times the user
		/// could issue the Undo command.
		///</summary>
		public int UndoableSequenceCount
		{
			get { return m_undoBundles.Count; }
		}

		/// <summary>
		/// Returns the number of Redoable sequences. This is the number of times the user
		/// could issue the Redo command.
		///</summary>
		public int RedoableSequenceCount
		{
			get { return m_redoBundles.Count; }
		}

		/// <summary>
		/// This will return the current UndoGrouper for the AH if one exists, otherwise returns null.
		/// This will set the UndoGrouper for this AH.
		///</summary>
		/// <returns>A IUndoGrouper </returns>
		public IUndoGrouper UndoGrouper
		{
			get { throw new NotSupportedException("'UndoGrouper getter' is not supported."); }
			set { throw new NotSupportedException("'UndoGrouper setter' is not supported."); }
		}

		/// <summary>
		/// Tells whether an Undo or Redo operation is in progress. During such operations,
		/// actions should not be added to the sequence, and some other side effect tasks
		/// may be suppressed. For example, we don't update modify times when a data field
		/// is modified by Undo/Redo; it is assumed that there is an action in the sequence
		/// to set the modify time to the appropriate value.
		/// </summary>
		/// <returns>A System.Boolean </returns>
		public bool IsUndoOrRedoInProgress
		{
			get { return m_uowService.UndoOrRedoInProgress; }
		}

		/// <summary>
		/// Tells whether making selections should be suppressed during a unit of work or an
		/// Undo/Redo operation.
		/// </summary>
		/// <value></value>
		/// <returns>A System.Boolean </returns>
		public bool SuppressSelections
		{
			get { return m_uowService.SuppressSelections; }
		}
		#endregion IActionHandler implementation

		#region IActionHandlerExtensions Members

		public void DoAtEndOfPropChanged(Action task)
		{
			m_actionsToDoAtEndOfPropChanged.Add(task);
		}

		public void DoAtEndOfPropChangedAlways(Action task)
		{
			m_currentBundle.ActionsToDoAtEndOfPropChanged.Add(task);
		}

		public event DoingUndoOrRedoDelegate DoingUndoOrRedo;

		public void MergeLastTwoUnitsOfWork()
		{
			if (m_uowService.CurrentProcessingState != UnitOfWorkService.FdoBusinessTransactionState.ReadyForBeginTask)
				throw new InvalidOperationException("Can't merge units of work while one is in progress");
			if (m_undoBundles.Count < 2)
				throw new InvalidOperationException("Can't merge units of work unless there are two to merge");
			if (m_redoBundles.Count > 0 || m_countUnsavedBundles < 2)
				throw new InvalidOperationException("Can only merge two newly-created units of work");
			var lastBundle = m_undoBundles.Pop();
			m_countUnsavedBundles--;
			var mergeInto = m_undoBundles.Peek();
			foreach(var change in lastBundle.Changes)
				mergeInto.AddAction(change);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not an undo task has been started.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsUndoTaskActive
		{
			get
			{
				return (m_uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.ProcessingDataChanges);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this is a legitimate time to start a new UOW.
		/// </summary>
		public bool CanStartUow
		{
			get
			{
				return m_uowService.CurrentProcessingState == UnitOfWorkService.FdoBusinessTransactionState.ReadyForBeginTask
					   && !m_uowService.UndoOrRedoInProgress;
			}
		}
		#endregion
	}
}
