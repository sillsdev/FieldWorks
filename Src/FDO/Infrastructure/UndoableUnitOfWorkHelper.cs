// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UndoableUnitOfWorkHelper.cs
// Responsibility: FW Team

using System;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// ----------------------------------------------------------------------------------------
	///<summary>
	/// Class that starts and ends an undoable/redoable Unit Of Work.
	///</summary>
	/// <remarks>
	/// The client should set 'RollBack' to false, as the last instruction in a 'using' structure.
	/// That will allow the change(s) to be saved.
	/// If an exception is thrown, during the course of the chenge(s),
	/// then there is an automatic rollback, if 'RollBack' is not set to false at the end.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public sealed class UndoableUnitOfWorkHelper : UnitOfWorkHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UndoableUnitOfWorkHelper(IActionHandler actionHandler, string undoText, string redoText) :
			base(actionHandler)
		{
			m_actionHandler.BeginUndoTask(undoText, redoText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deprecated Constructor, for tests only. Prefer to pass the Undo and Redo text separately.
		/// </summary>
		/// <param name="actionHandler"></param>
		/// <param name="message">
		/// The part of the 'Undo some change' and 'Redo some change'
		/// that comes after 'Undo ' and 'Redo '.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public UndoableUnitOfWorkHelper(IActionHandler actionHandler, string message) :
			this(actionHandler, "Undo " + message, "Redo " + message)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the specified task, making it an undoable task in the specified action handler, with
		/// the specified labels. The task will automatically be begun and ended if all goes well, and rolled
		/// back if an exception is thrown; the exception will then be rethrown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Do(string undoText, string redoText, IActionHandler actionHandler, Action task)
		{
			using (var undoHelper = new UndoableUnitOfWorkHelper(actionHandler, undoText, redoText))
			{
				task();
				undoHelper.RollBack = false; // task ran successfully, don't roll back.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the specified task, making it an undoable task in the action handler
		/// associated with the input ICmObject, with the specified labels. The task will
		/// automatically be begun and ended if all goes well, and rolled back if an exception
		/// is thrown. (The exception will then be rethrown.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Do(string undoText, string redoText, ICmObject obj, Action task)
		{
			Do(undoText, redoText, obj.Cache.ServiceLocator.GetInstance<IActionHandler>(), task);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the specified task, making it an undoable task in the action handler if
		/// necessary (using the specified labels). If an undoable action has already been
		/// started, then this task will be done as part of the existing UOW. If an undo task is
		/// created and the specified task completes normally, the UOW will be added to the undo
		/// stack; it will be rolled back if an exception is thrown. (The exception will then be
		/// rethrown.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void DoUsingNewOrCurrentUOW(string undoText, string redoText,
			IActionHandler actionHandler, Action task)
		{
			if (actionHandler.CurrentDepth > 0)
				task();
			else
				Do(undoText, redoText, actionHandler, task);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the undo task when not rolling back the changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void EndUndoTask()
		{
			m_actionHandler.EndUndoTask();
		}
	}
}
