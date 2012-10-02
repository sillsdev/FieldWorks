// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoActions.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	#region UndoSelectionAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for setting the selection
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoSelectionAction: UndoActionBase
	{
		private SelectionHelper m_selHelper;
		private bool m_fForUndo;
		private bool m_fStateUndone = false;

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
		}

		/// <summary>
		/// We want to make as sure as possible that this happens; there's no way to set up a
		/// Dispose for this class, but this may help make sure that when it's no longer useful
		/// we don't leave it in the Idle event list.
		/// </summary>
		public override void Commit()
		{
			Application.Idle -= new EventHandler(DoRestoreSelection);
			base.Commit();
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			return UndoRedo(true, fRefreshPending);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			if (fRefreshPending)
			{
				// make sure we are sync'd to the string in the database.
				// before we try to restore the selection.
				EnsureSyncText();
			}
			return UndoRedo(false, fRefreshPending);
		}

		private void EnsureSyncText()
		{
			if (m_selHelper != null && m_selHelper.Selection != null && m_selHelper.Selection.RootBox != null)
			{
				TextSelInfo tsi = new TextSelInfo(m_selHelper.Selection);
				m_selHelper.RootSite.RootBox.PropChanged(tsi.HvoAnchor, tsi.TagAnchor, 0, 0, 0);
				if (tsi.Hvo(false) != tsi.Hvo(true))
					m_selHelper.RootSite.RootBox.PropChanged(tsi.Hvo(true), tsi.Tag(true), 0, 0, 0);
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the selection if the desired action specified in <paramref name="fUndo"/>
		/// matches what we're set up to.
		/// </summary>
		/// <param name="fUndo">Desired action (<c>true</c> for Undo, <c>false</c> for Redo)
		/// <param name="fRefreshPending">If true, there will be a Refresh to restore the view.</param>
		/// </param>
		/// ------------------------------------------------------------------------------------
		private bool UndoRedo(bool fUndo, bool fRefreshPending)
		{
			Debug.Assert(m_fStateUndone == !fUndo);

			try
			{
				if (fUndo == m_fForUndo && m_selHelper != null)
				{
					if (fRefreshPending)
					{
					// We can't safely try to re-create our selection until after the Refresh.
						// Wait till the system is idle.
						Application.Idle += new EventHandler(DoRestoreSelection);

					}
					else
						RestoreSelection();
				}
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

		void DoRestoreSelection(object sender, EventArgs e)
		{
			Application.Idle -= new EventHandler(DoRestoreSelection);
			RestoreSelection();
		}

		private void RestoreSelection()
		{
			m_selHelper.RestoreSelectionAndScrollPos();
			// setting focus helps in the case where the user clicked in a different pane,
			// e.g. in the BT view
			if (m_selHelper.RootSite is Control)
				((Control)m_selHelper.RootSite).Focus();
		}
	}
	#endregion
}
