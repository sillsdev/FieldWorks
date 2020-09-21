// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Main class for displaying the VectorReferenceSlice.
	/// </summary>
	public class RevEntrySensesCollectionReferenceView : VectorReferenceView
	{
		#region Constants and data members

		private int m_selectedSenseHvo = 0;
		private bool m_handlingSelectionChanged = false;
		private System.ComponentModel.IContainer components = null;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		public RevEntrySensesCollectionReferenceView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
		}

		#endregion // Construction, initialization, and disposal

		#region other overrides and related methods

		protected override void Delete()
		{
			RemoveReversalEntryFromSense();
			base.Delete();
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			if (m_handlingSelectionChanged)
				return;

			m_handlingSelectionChanged = true;
			try
			{
				m_selectedSenseHvo = 0;
				if (vwselNew == null)
					return;
				base.HandleSelectionChange(rootb, vwselNew);

				// Get the Id of the selected snes, and store it.
				int cvsli = vwselNew.CLevels(false);
				// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
				cvsli--;
				if (cvsli == 0)
				{
					// No objects in selection: don't allow a selection.
					m_rootb.DestroySelection();
					// Enhance: invoke launcher's selection dialog.
					return;
				}
				ITsString tss;
				int ichAnchor;
				int ichEnd;
				bool fAssocPrev;
				int hvoObj;
				int hvoObjEnd;
				int tag;
				int ws;
				vwselNew.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj,
					out tag, out ws);
				vwselNew.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd,
					out tag, out ws);
				if (hvoObj != hvoObjEnd)
					return;
				m_selectedSenseHvo = hvoObj;
			}
			finally
			{
				m_handlingSelectionChanged = false;
			}
		}

		private void RemoveReversalEntryFromSense()
		{
			if (m_selectedSenseHvo == 0)
				return;		// must be selecting multiple objects!  (See LT-5724.)
			int h1 = m_rootb.Height;
			ILexSense sense = (ILexSense)m_cache.ServiceLocator.GetObject(m_selectedSenseHvo);
			var col = sense.ReferringReversalIndexEntries;
			using (UndoableUnitOfWorkHelper helper = new UndoableUnitOfWorkHelper(
				m_cache.ActionHandlerAccessor,
				SIL.FieldWorks.XWorks.LexEd.LexEdStrings.ksUndoDeleteRevFromSense,
				SIL.FieldWorks.XWorks.LexEd.LexEdStrings.ksRedoDeleteRevFromSense))
			{
				((IReversalIndexEntry)m_rootObj).SensesRS.Remove(sense);
				helper.RollBack = false;
			}
			CheckViewSizeChanged(h1, m_rootb.Height);
		}

		#endregion other overrides and related methods

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// RevEntrySensesCollectionReferenceView
			//
			this.Name = "RevEntrySensesCollectionReferenceView";
			this.Size = new System.Drawing.Size(232, 40);

		}
		#endregion
	}
}
