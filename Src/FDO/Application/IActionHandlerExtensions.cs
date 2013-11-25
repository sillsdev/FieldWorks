// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IActionHandlerExtensions.cs
// Responsibility: FW Team

using System;
using System.ComponentModel;

namespace SIL.FieldWorks.FDO.Application
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple delegate for the DoingUndoOrRedo event
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public delegate void DoingUndoOrRedoDelegate(CancelEventArgs e);

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Additional interface supported by managed implementations of IActionHandler.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IActionHandlerExtensions
	{
		/// <summary>
		/// This task is done once when all the PropChanged notifications have been sent at the
		/// end of a unit of work. A new unit of work may be done (and completed!) within the task.
		/// (Note however that if it is a non-undoable task, as is likely, it should not modify
		/// the same objects as the main UOW, or Undo will be prevented.)
		/// The task will not occur again if the original UOW is undone or redone.
		/// These tasks are done BEFORE any DoAtEndOfPropChangedAlways tasks.
		/// This may only be called while a UOW is in progress.
		/// </summary>
		/// <remarks>An earlier version used a PropChangedEvent. But all clients currently
		/// want this notification only once. We tried to achieve this by having the event
		/// handlers remove themselves from the event. This is not reliable. If one or more
		/// of the tasks creates its own UOW, the end of THAT UOW will trigger whatever
		/// tasks are still outstanding. Then, even though the handlers have removed themselves
		/// from the current list of handlers, the original notification code continues to
		/// notify everything in the original list. Thus handlers get duplicate notifications,
		/// probably with unwanted results, for example, LT-14765.
		/// Also, the spec does not guarantee any particular order for notifying handlers of an
		/// event, and the order in which postponed tasks are done may be important.
		/// I therefore concluded that it was more appropriate to allow the client to register a
		/// task to be done once in order at the end of the PropChanged phase.</remarks>
		void DoAtEndOfPropChanged(Action task);
		/// <summary>
		/// This task is done when all the PropChanged notifications have been sent at the
		/// end of a unit of work. It is also done at the end of PropChanged when the UOW is undone or redone.
		/// It should not normally do new units of work, because that would interfere with the stack of
		/// units of work and possibly prevent an Undone task from being redone.
		/// This may only be called while a UOW is in progress.
		/// </summary>
		void DoAtEndOfPropChangedAlways (Action task);

		/// <summary>
		/// This event is raised before an undo or redo
		/// </summary>
		event DoingUndoOrRedoDelegate DoingUndoOrRedo;

		/// <summary>
		/// Combine the last two units of work. All the same actions are kept, in the same order, except
		/// where changed by the usual logic for merging multiple changes to the same property or suppressing
		/// changes to newly created/deleted objects. Uses the undo labels of the older UOW.
		/// </summary>
		void MergeLastTwoUnitsOfWork();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not an undo task has been started.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsUndoTaskActive
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether this is a legitimate time to start a new UOW.
		/// </summary>
		bool CanStartUow { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears all marks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ClearAllMarks();
	}
}
