// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoActionBase.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The default base class for C# undo actions
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	public abstract class UndoActionBase : IUndoAction
	{
		#region IUndoAction Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Irreversibly commits an action.
		/// </summary>
		/// <remarks>This implementation does nothing</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void Commit()
		{
			// Default is to do nothing
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True for most actions, which make changes to data; false for actions that represent
		/// updates to the user interface, like replacing the selection.
		/// </summary>
		/// <returns>this implementation always returns false</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDataChange
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True for most actions, which are redoable; false for actions that aren't, like
		/// Scripture import.
		/// </summary>
		/// <returns>This implementation always returns true</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsRedoable
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public abstract bool Redo();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets whether this undo action should notify the world that the action has been undone
		/// or redone. For ISqlUndoAction, this supresses the PropChanged notifications.
		/// </summary>
		/// <remarks>This implementation does nothing</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool SuppressNotification
		{
			set { /* Default is to do nothing */ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action. Sets pfSuccess to true if successful. If not successful
		/// because the database state has changed unexpectedly, sets pfSuccess to false but still
		/// returns S_OK. More catastrophic errors may produce error result codes.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public abstract bool Undo();
		#endregion
	}
}
