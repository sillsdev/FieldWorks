// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UndoTaskHelper.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
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
	/// ----------------------------------------------------------------------------------------
	public class UndoTaskHelper : UnitOfWorkHelper
	{
		private readonly IVwRootSite m_vwRootSite;
		private UndoSelectionAction m_redoSelectionAction;

		#region Constructors/Init
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view (required)</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(IVwRootSite rootSite, string stid) :
			this(rootSite.RootBox.DataAccess.GetActionHandler(), rootSite, stid)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="actionHandler">The IActionHandler to start the undo task on</param>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(IActionHandler actionHandler, IVwRootSite rootSite, string stid) :
			base(actionHandler)
		{
			m_vwRootSite = rootSite;

			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels(stid, out stUndo, out stRedo);
			Init(stUndo, stRedo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view (required)</param>
		/// <param name="stUndo">Undo label</param>
		/// <param name="stRedo">Redo label</param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(IVwRootSite rootSite, string stUndo, string stRedo) :
			this(rootSite.RootBox.DataAccess.GetActionHandler(), rootSite, stUndo, stRedo)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="actionHandler">The IActionHandler to start the undo task on</param>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stUndo">Undo label</param>
		/// <param name="stRedo">Redo label</param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(IActionHandler actionHandler, IVwRootSite rootSite, string stUndo,
			string stRedo) : base(actionHandler)
		{
			m_vwRootSite = rootSite;
			Init(stUndo, stRedo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this UndoTaskHelper.
		/// </summary>
		/// <param name="stUndo">Undo label</param>
		/// <param name="stRedo">Redo label</param>
		/// ------------------------------------------------------------------------------------
		private void Init(string stUndo, string stRedo)
		{
			m_actionHandler.BeginUndoTask(stUndo, stRedo);

			// Record an action that will handle replacing the selection on undo.
			SetupUndoSelection(true);
		}
		#endregion

		#region Overrides of UnitOfWorkHelper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the undo task when not rolling back the changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void EndUndoTask()
		{
			//	Record an action that will handle replacing the selection on redo.
			SetupUndoSelection(false);
			m_actionHandler.EndUndoTask();

			// Make sure the selection for the redo is updated to the new selection after the
			// UOW was finished.
			if (m_redoSelectionAction != null)
				m_redoSelectionAction.ResetSelection();
		}
		#endregion

		#region Private helper methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set up an undo-action to replace the selection
		/// </summary>
		/// <param name="fForUndo"><c>true</c> to setup action for Undo, <c>false</c> for
		/// Redo.</param>
		/// <remarks>We want to create a UndoSelectionAction only if we are the outermost
		/// UndoTask.</remarks>
		/// -----------------------------------------------------------------------------------
		private void SetupUndoSelection(bool fForUndo)
		{
			if (m_vwRootSite == null ||
				((Control)m_vwRootSite).IsDisposed ||	// has already been disposed ... tough to find...
				m_vwRootSite.RootBox == null)
			{
				return;
			}

			UndoSelectionAction selectionAction = new UndoSelectionAction(m_vwRootSite,
				fForUndo, m_vwRootSite.RootBox.Selection);
			m_actionHandler.AddAction(selectionAction);
			if (!fForUndo)
				m_redoSelectionAction = selectionAction;
		}
		#endregion
	}
}
