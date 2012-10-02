// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoActions.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	#region UndoSelectionAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for setting the selection
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoSelectionAction : UndoActionBase
	{
		private SelectionHelper m_selHelper;
		private bool m_fStateUndone = false;
		private readonly IVwRootSite m_rootSite;
		private readonly bool m_fForUndo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the <see cref="UndoSelectionAction"/> object.
		/// </summary>
		/// <param name="rootSite">The view that has the rootbox (and probably a selection)
		/// </param>
		/// <param name="fForUndo"><c>true</c> for Undo, <c>false</c> for Redo.</param>
		/// <param name="vwSel">The selection for the action</param>
		/// ------------------------------------------------------------------------------------
		public UndoSelectionAction(IVwRootSite rootSite, bool fForUndo, IVwSelection vwSel)
		{
			m_fForUndo = fForUndo;
			m_selHelper = SelectionHelper.Create(vwSel, rootSite);
			m_rootSite = rootSite;
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			return UndoRedo(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			return UndoRedo(false);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the selection if the desired action specified in <paramref name="fUndo"/>
		/// matches what we're set up to.
		/// </summary>
		/// <param name="fUndo">Desired action (<c>true</c> for Undo, <c>false</c> for Redo)</param>
		/// ------------------------------------------------------------------------------------
		private bool UndoRedo(bool fUndo)
		{
			Debug.Assert(m_fStateUndone == !fUndo);

			try
			{
				if (fUndo == m_fForUndo && m_selHelper != null)
					RestoreSelection();
			}
			finally
			{
				// If we somehow failed to make the desired selection, still note we're in the
				// other state, otherwise, we will get an Assert if ever done in the opposite
				// direction.
				m_fStateUndone = fUndo;
			}
			return true;
		}

		private void RestoreSelection()
		{
			m_selHelper.RestoreSelectionAndScrollPos();
			// Selecting the original rootsite handles the case where a command or subsequent user action caused
			// a different pane to be focused, e.g. in the BT view
			if (m_selHelper.RootSite is Control && ((Control)m_selHelper.RootSite).FindForm() == Form.ActiveForm)
			{
				Logger.WriteEvent("Selecting " + m_selHelper.RootSite);
				((Control)m_selHelper.RootSite).Select();
			}
			//else
			//{
			//    Logger.WriteEvent("Unable to set focus to the original rootsite. " +
			//        ((m_selHelper.RootSite is Control) ?
			//        "((Control)m_selHelper.RootSite).FindForm() == " + ((Control)m_selHelper.RootSite).FindForm() +
			//        "; Form.ActiveForm == " + Form.ActiveForm :
			//        "The selection helper's rootsite is a " + m_selHelper.RootSite + ", not a control"));
			//}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the saved selection to the selection that is currently in the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ResetSelection()
		{
			// If selHelper is not null, that means that the view probably just updated the
			// values of the existing selection instead of requesting a new selection at the
			// end of the UOW.
			// NOTE: This will not work correctly if we don't destroy the selection in
			// RequestSelectionAtEndOfUOW.
			if (m_selHelper != null)
				m_selHelper = SelectionHelper.Create(m_selHelper.Selection, m_selHelper.RootSite);
			else if (m_rootSite != null)
				m_selHelper = SelectionHelper.Create(m_rootSite);
		}
	}
	#endregion

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
		private SyncMsg m_syncMsg;
		private IApp m_app;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SyncUndoAction"/> class.
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="syncMsg">The sync message.</param>
		/// ------------------------------------------------------------------------------------
		public SyncUndoAction(IApp app, SyncMsg syncMsg)
		{
			m_app = app;
			m_syncMsg = syncMsg;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Do()
		{
			m_app.Synchronize(m_syncMsg);
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
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
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			Do();
			return true;
		}
		#endregion
	}

	#endregion
}
