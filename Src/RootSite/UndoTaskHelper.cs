// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Encapsulates BeginUndoTask/EndUndoTask and Commit methods.
	/// </summary>
	/// <example>
	/// Typical usage is
	/// <code>
	/// using(new UndoTaskHelper(vwrootSite, "kidExample", true))
	///	{
	///		DoStuff();
	///	}
	/// </code>
	/// </example>
	public class UndoTaskHelper : UnitOfWorkHelper
	{
		private readonly IVwRootSite m_vwRootSite;
		private UndoSelectionAction m_redoSelectionAction;

		#region Constructors/Init

		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view (required)</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		public UndoTaskHelper(IVwRootSite rootSite, string stid)
			: this(rootSite.RootBox.DataAccess.GetActionHandler(), rootSite, stid)
		{
		}

		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="actionHandler">The IActionHandler to start the undo task on</param>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		public UndoTaskHelper(IActionHandler actionHandler, IVwRootSite rootSite, string stid)
			: base(actionHandler)
		{
			m_vwRootSite = rootSite;

			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels(stid, out stUndo, out stRedo);
			Init(stUndo, stRedo);
		}

		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view (required)</param>
		/// <param name="stUndo">Undo label</param>
		/// <param name="stRedo">Redo label</param>
		public UndoTaskHelper(IVwRootSite rootSite, string stUndo, string stRedo)
			: this(rootSite.RootBox.DataAccess.GetActionHandler(), rootSite, stUndo, stRedo)
		{
		}

		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="actionHandler">The IActionHandler to start the undo task on</param>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stUndo">Undo label</param>
		/// <param name="stRedo">Redo label</param>
		public UndoTaskHelper(IActionHandler actionHandler, IVwRootSite rootSite, string stUndo, string stRedo) : base(actionHandler)
		{
			m_vwRootSite = rootSite;
			Init(stUndo, stRedo);
		}

		/// <summary>
		/// Initializes this UndoTaskHelper.
		/// </summary>
		private void Init(string stUndo, string stRedo)
		{
			m_actionHandler.BeginUndoTask(stUndo, stRedo);

			// Record an action that will handle replacing the selection on undo.
			SetupUndoSelection(true);
		}
		#endregion

		#region Overrides of UnitOfWorkHelper

		/// <summary>
		/// Ends the undo task when not rolling back the changes.
		/// </summary>
		protected override void EndUndoTask()
		{
			//	Record an action that will handle replacing the selection on redo.
			SetupUndoSelection(false);
			m_actionHandler.EndUndoTask();

			// Make sure the selection for the redo is updated to the new selection after the
			// UOW was finished.
			m_redoSelectionAction?.ResetSelection();
		}
		#endregion

		#region Private helper methods

		/// <summary>
		/// Set up an undo-action to replace the selection
		/// </summary>
		/// <param name="fForUndo"><c>true</c> to setup action for Undo, <c>false</c> for
		/// Redo.</param>
		/// <remarks>We want to create a UndoSelectionAction only if we are the outermost
		/// UndoTask.</remarks>
		private void SetupUndoSelection(bool fForUndo)
		{
			if (m_vwRootSite == null || ((Control)m_vwRootSite).IsDisposed || m_vwRootSite.RootBox == null)
			{
				return;
			}
			var selectionAction = new UndoSelectionAction(m_vwRootSite, fForUndo, m_vwRootSite.RootBox.Selection);
			m_actionHandler.AddAction(selectionAction);
			if (!fForUndo)
			{
				m_redoSelectionAction = selectionAction;
			}
		}
		#endregion

		/// <summary>
		/// Undo action for setting the selection
		/// </summary>
		private sealed class UndoSelectionAction : UndoActionBase
		{
			private SelectionHelper m_selHelper;
			private bool m_fStateUndone;
			private readonly IVwRootSite m_rootSite;
			private readonly bool m_fForUndo;

			/// <summary>
			/// Creates a new instance of the <see cref="UndoSelectionAction"/> object.
			/// </summary>
			/// <param name="rootSite">The view that has the rootbox (and probably a selection)
			/// </param>
			/// <param name="fForUndo"><c>true</c> for Undo, <c>false</c> for Redo.</param>
			/// <param name="vwSel">The selection for the action</param>
			public UndoSelectionAction(IVwRootSite rootSite, bool fForUndo, IVwSelection vwSel)
			{
				m_fForUndo = fForUndo;
				m_selHelper = SelectionHelper.Create(vwSel, rootSite);
				m_rootSite = rootSite;
			}

			#region Overrides of UndoActionBase

			/// <summary>
			/// Reverses (or "un-does") an action.
			/// </summary>
			public override bool Undo()
			{
				return UndoRedo(true);
			}

			/// <summary>
			/// Re-applies (or "re-does") an action.
			/// </summary>
			public override bool Redo()
			{
				return UndoRedo(false);
			}

			#endregion

			/// <summary>
			/// Resets the selection if the desired action specified in <paramref name="fUndo"/>
			/// matches what we're set up to.
			/// </summary>
			/// <param name="fUndo">Desired action (<c>true</c> for Undo, <c>false</c> for Redo)</param>
			private bool UndoRedo(bool fUndo)
			{
				Debug.Assert(m_fStateUndone == !fUndo);

				try
				{
					if (fUndo == m_fForUndo && m_selHelper != null)
					{
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
			}

			/// <summary>
			/// Updates the saved selection to the selection that is currently in the view.
			/// </summary>
			internal void ResetSelection()
			{
				// If selHelper is not null, that means that the view probably just updated the
				// values of the existing selection instead of requesting a new selection at the
				// end of the UOW.
				// NOTE: This will not work correctly if we don't destroy the selection in
				// RequestSelectionAtEndOfUOW.
				if (m_selHelper != null)
				{
					m_selHelper = SelectionHelper.Create(m_selHelper.Selection, m_selHelper.RootSite);
				}
				else if (m_rootSite != null)
				{
					m_selHelper = SelectionHelper.Create(m_rootSite);
				}
			}
		}
	}
}