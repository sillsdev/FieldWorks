// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoActions.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.RootSites
{
	#region Class SyncUndoAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handle Undo and Redo (as well as Do) for a sync message. Basically it just means that
	/// we have to do a sync again.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SyncUndoAction : UndoActionBase
	{
		#region Data members
		private FdoCache m_fdoCache;
		private SyncInfo m_syncInfo;
		private IApp m_app;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SyncUndoAction"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="app">The application.</param>
		/// <param name="syncInfo">The sync info.</param>
		/// ------------------------------------------------------------------------------------
		public SyncUndoAction(FdoCache cache, IApp app, SyncInfo syncInfo)
		{
			m_fdoCache = cache;
			m_app = app;
			m_syncInfo = syncInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Do()
		{
			m_app.Synchronize(m_syncInfo, m_fdoCache);
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// <param name="fRefreshPending">Set to true if app will call refresh after all Undo
		/// actions are finished. This means the UndoAction doesn't have to call PropChanged.
		/// </param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			Do();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "undoes") an action. Sets pfSuccess to true if successful. If not successful
		/// because the database state has changed unexpectedly, sets pfSuccess to false but still
		/// returns S_OK. More catastrophic errors may produce error result codes.
		/// </summary>
		/// <param name="fRefreshPending">Set to true if app will call refresh after all Undo actions are
		/// finished. This means the UndoAction doesn't have to call PropChanged.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			Do();
			return true;
		}
		#endregion
	}

	#endregion
}
