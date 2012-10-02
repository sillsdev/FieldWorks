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
// File: RevEntrySensesCollectionReferenceView.cs
// Responsibility: Randyr Regnier
// Last reviewed:
//
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Validation;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.Common.Controls
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

		/// <summary>
		/// override this to allow deleting an item IF the key is Delete.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			try
			{
				if (e.KeyCode == Keys.Delete)
				{
					m_fOkToDeleteItem = true;
					RemoveReversalEntryFromSense();
				}
				base.OnKeyDown (e);
			}
			finally
			{
				m_fOkToDeleteItem = false;
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			try
			{
				if (e.KeyChar == (char)Keys.Back)
				{
					m_fOkToDeleteItem = true;
					RemoveReversalEntryFromSense();
				}
				base.OnKeyPress (e);
			}
			finally
			{
				m_fOkToDeleteItem = false;
			}
		}

		public override void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
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
				base.SelectionChanged(rootb, vwselNew);

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
			ILexSense sense = LexSense.CreateFromDBObject(m_fdoCache, m_selectedSenseHvo);
			FdoReferenceCollection<IReversalIndexEntry> col = sense.ReversalEntriesRC;
			int oldCount = col.Count;
			m_fdoCache.BeginUndoTask(SIL.FieldWorks.XWorks.LexEd.LexEdStrings.ksUndoDeleteRevFromSense,
				SIL.FieldWorks.XWorks.LexEd.LexEdStrings.ksRedoDeleteRevFromSense);
			// Remove does a PropChanged on the main sense ReversalEntries property.
			sense.ReversalEntriesRC.Remove(m_rootObj.Hvo);
			// Update the ReferringSenses property, and do PropChanged on it.
			ReversalIndexEntry.ResetReferringSenses(m_fdoCache, m_rootObj.Hvo);
			m_fdoCache.EndUndoTask();
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
