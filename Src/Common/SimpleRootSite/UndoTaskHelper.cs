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
// File: UndoTaskHelper.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.Utils;

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
	public class UndoTaskHelper : IFWDisposable
	{
		private IVwRootSite m_vwRootSite;
		private bool m_fEndUndoTask = true;
		private bool m_fCommit;
		private ISilDataAccess m_dataAccess;
		private int m_nDepth;
		/// <summary>
		/// Use the ActionHandlerAccessor property instead of this variable
		/// </summary>
		private IActionHandler m_actionHandler;

		#region Constructors/Init
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="dataAccess">The ISilDataAccess to start the undo task on</param>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(ISilDataAccess dataAccess, IVwRootSite rootSite, string stid)
			: this(dataAccess, rootSite, stid, true)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// -----------------------------------------------------------------------------------
		public UndoTaskHelper(IVwRootSite rootSite, string stid)
			: this(rootSite, stid, true)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="dataAccess">The ISilDataAccess to start the undo task on</param>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// <param name="fCommit"><c>true</c> to call commit (and thus end all outstanding
		/// transactions). If an <see cref="UndoTaskHelper"/> object is created as part of an
		/// inner task, you probably want to set this to <c>false</c>. Default is <c>true</c>.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(ISilDataAccess dataAccess, IVwRootSite rootSite, string stid,
			bool fCommit)
		{
			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels(stid, out stUndo, out stRedo);
			Init(dataAccess, rootSite, stUndo, stRedo, fCommit);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view (required)</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// <param name="fCommit"><c>true</c> to call commit (and thus end all outstanding
		/// transactions). If an <see cref="UndoTaskHelper"/> object is created as part of an
		/// inner task, you probably want to set this to <c>false</c>. Default is <c>true</c>.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(IVwRootSite rootSite, string stid, bool fCommit)
		{
			Debug.Assert(rootSite != null);
			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels(stid, out stUndo, out stRedo);
			Init(rootSite.RootBox.DataAccess, rootSite, stUndo, stRedo, fCommit);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="dataAccess">The ISilDataAccess to start the undo task on</param>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stUndo">Undo label</param>
		/// <param name="stRedo">Redo label</param>
		/// <param name="fCommit"><c>true</c> to call commit (and thus end all outstanding
		/// transactions). If an <see cref="UndoTaskHelper"/> object is created as part of an
		/// inner task, you probably want to set this to <c>false</c>. Default is <c>true</c>.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(ISilDataAccess dataAccess, IVwRootSite rootSite, string stUndo,
			string stRedo, bool fCommit)
		{
			Init(dataAccess, rootSite, stUndo, stRedo, fCommit);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view (required)</param>
		/// <param name="stUndo">Undo label</param>
		/// <param name="stRedo">Redo label</param>
		/// <param name="fCommit"><c>true</c> to call commit (and thus end all outstanding
		/// transactions). If an <see cref="UndoTaskHelper"/> object is created as part of an
		/// inner task, you probably want to set this to <c>false</c>. Default is <c>true</c>.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public UndoTaskHelper(IVwRootSite rootSite, string stUndo, string stRedo, bool fCommit)
		{
			Debug.Assert(rootSite != null);
			Init(rootSite.RootBox.DataAccess, rootSite, stUndo, stRedo, fCommit);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization
		/// </summary>
		/// <param name="dataAccess">The ISilDataAccess to start the undo task on</param>
		/// <param name="rootSite">The view (can be <c>null</c>)</param>
		/// <param name="stUndo">Undo label</param>
		/// <param name="stRedo">Redo label</param>
		/// <param name="fCommit"><c>true</c> to call commit (and thus end all outstanding
		/// transactions). If an <see cref="UndoTaskHelper"/> object is created as part of an
		/// inner task, you probably want to set this to <c>false</c>. Default is <c>true</c>.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void Init(ISilDataAccess dataAccess, IVwRootSite rootSite, string stUndo,
			string stRedo, bool fCommit)
		{
			Debug.Assert(dataAccess != null);
			m_vwRootSite = rootSite;
			m_fCommit = fCommit;

			m_dataAccess = dataAccess;
			if (ActionHandlerAccessor != null)
			{
				m_nDepth = ActionHandlerAccessor.CurrentDepth;
				m_dataAccess.BeginUndoTask(stUndo, stRedo);
			}

			// Record an action that will handle replacing the selection on undo.
			SetupUndoSelection(true);
		}
		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~UndoTaskHelper()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (EndUndoTask)
					EndTheUndoTask();
				else
					Rollback();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			// If our action handler is a COM object and it we received it through
			// a COM object we have to release it. If it is a COM object but we
			// received it from a managed object then the managed object will/should
			// take care to release it.
			if (Marshal.IsComObject(m_dataAccess))
			{
				if (m_actionHandler != null && Marshal.IsComObject(m_actionHandler))
					Marshal.ReleaseComObject(m_actionHandler);
			}

			m_actionHandler = null;
			m_vwRootSite = null;
			m_dataAccess = null;
			m_isDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(ToString(), "This object is being used after it has been disposed: this is an Error.");
		}

		#endregion IDisposable & Co. implementation

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property manages the ActionHandler. Since it is a COM object we want to
		/// limit the number of references to the object. Using this property will guarantee that
		/// we only get one reference to the COM object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IActionHandler ActionHandlerAccessor
		{
			get
			{
				if (m_actionHandler == null)
					m_actionHandler = m_dataAccess.GetActionHandler();

				return m_actionHandler;
			}
			set
			{
				m_actionHandler = value;
				m_dataAccess.SetActionHandler(value);
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the undo task and call commit on root site
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void EndTheUndoTask()
		{
			//	Record an action that will handle replacing the selection on redo.
			SetupUndoSelection(false);

			if (ActionHandlerAccessor != null)
				m_dataAccess.EndUndoTask();

			if (m_fCommit && m_vwRootSite != null)
			{
				IVwRootBox rootBox = m_vwRootSite.RootBox;
				if (rootBox != null)
				{
					IVwSelection vwsel = rootBox.Selection;
					if (vwsel != null)
						vwsel.Commit();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rollback to the save point
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void Rollback()
		{
			using (new WaitCursor(m_vwRootSite as Control))
			{
				if (m_dataAccess != null)
					m_dataAccess.Rollback();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag whether to end the undo task in Dispose. If <c>false</c> and
		/// a save point was set it does a rollback instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool EndUndoTask
		{
			get { CheckDisposed(); return m_fEndUndoTask; }
			set { CheckDisposed(); m_fEndUndoTask = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set up an undo-action to replace the selection
		/// </summary>
		/// <param name="fForUndo"><c>true</c> to setup action for Undo, <c>false</c> for
		/// Redo.</param>
		/// <remarks>We want to create a UndoSelectionAction only if we are the outermost
		/// UndoTask.</remarks>
		/// -----------------------------------------------------------------------------------
		protected void SetupUndoSelection(bool fForUndo)
		{
			if (ActionHandlerAccessor == null || m_vwRootSite == null ||
				((System.Windows.Forms.Control)(this.m_vwRootSite)).IsDisposed ||	// has already been disposed ... tough to find...
				!(m_vwRootSite is IRootSite) || m_nDepth > 0 || m_vwRootSite.RootBox == null)
			{
				return;
			}

			ActionHandlerAccessor.AddAction(
				new UndoSelectionAction(m_vwRootSite, fForUndo,
				m_vwRootSite.RootBox.Selection));
		}
	}
}
