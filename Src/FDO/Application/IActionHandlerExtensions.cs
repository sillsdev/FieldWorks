// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IActionHandlerExtensions.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;

namespace SIL.FieldWorks.FDO.Application
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple delegate for the PropChangedCompleted event
	/// </summary>
	/// <param name="sender">The undo stack that fired the event</param>
	/// <param name="fromUndoRedo">True if the event was fired from an undo (or rollback) or
	/// redo, false otherwise.</param>
	/// ----------------------------------------------------------------------------------------
	public delegate void PropChangedCompletedDelegate(object sender, bool fromUndoRedo);

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
		/// This event is raised when all the PropChanged notifications have been sent at the
		/// end of a unit of work.
		/// </summary>
		event PropChangedCompletedDelegate PropChangedCompleted;

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
