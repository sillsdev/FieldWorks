// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoImportObjectAction.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for import. Cannot Redo. Undo deletes the object.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoImportObjectAction: UndoRefreshAction
	{
		#region Data members
		/// <summary>The id of the object that this action is responsible for removing if an Undo
		/// occurs</summary>
		protected int m_hvoAddedObject;
		/// <summary>The cache</summary>
		protected FdoCache m_cache;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:UndoImportObjectAction"/> class.
		/// </summary>
		/// <remarks>This is internal because we only want UndoImportManager to create these
		/// </remarks>
		/// <param name="obj">The thing we should delete if we Undo.</param>
		/// ------------------------------------------------------------------------------------
		public UndoImportObjectAction(ICmObject obj)
		{
			m_hvoAddedObject = obj.Hvo;
			m_cache = obj.Cache;
		}
		#endregion

		#region Overrides of UndoRefreshAction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns always <c>true</c>
		/// </summary>
		/// <returns>always <c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>false</c> because this can't be redone.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsRedoable()
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") a Scripture import.
		/// </summary>
		/// <param name="fRefreshPending">Set to <c>true</c> if app will call refresh after all
		/// Undo actions are finished. This means the UndoImportAction need not call PropChanged.
		/// (However, this implementation will anyway.)
		/// </param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			if (m_cache.IsValidObject(m_hvoAddedObject))
				m_cache.DeleteObject(m_hvoAddedObject);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redoing an import isn't valid, so this method throws an exception.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			throw new NotImplementedException("Import cannot be redone.");
		}
		#endregion
	}
}
