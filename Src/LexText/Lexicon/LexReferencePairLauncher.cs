using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferencePairLauncher.
	/// </summary>
	public class LexReferencePairLauncher : AtomicReferenceLauncher
	{
		protected ICmObject m_displayParent;

		public LexReferencePairLauncher()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override ICmObject Target
		{
			get
			{
				var lr = m_obj as ILexReference;
				if (lr.TargetsRS.Count < 2)
					return null;
				var target = lr.TargetsRS[0];
				if (target == m_displayParent)
					target = lr.TargetsRS[1];
				return target;
			}
			set
			{
				Debug.Assert(value != null);

				int index = 0;
				var lr = m_obj as ILexReference;
				var item = lr.TargetsRS[0];
				if (item == m_displayParent)
					index = 1;
#if WANTPORTMULTI
				(lr as ILexReference).UpdateTargetTimestamps();
				(co as ICmObject).UpdateTimestampForVirtualChange();
#endif
				// LT-13729: Remove old and then Insert new might cause the deletion of the lr, then the insert fails.
				lr.TargetsRS.Replace(index, (index < lr.TargetsRS.Count) ? 1 : 0, new List<ICmObject>() { value });
			}
		}

		/// <summary>
		/// Wrapper for HandleChooser() to make it available to the slice.
		/// </summary>
		internal void LaunchChooser()
		{
			CheckDisposed();

			HandleChooser();
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries.
		/// </summary>
		protected override void HandleChooser()
		{
			ILexRefType lrt = (ILexRefType)m_obj.Owner;
			LexRefTypeTags.MappingTypes type = (LexRefTypeTags.MappingTypes)lrt.MappingType;
			BaseGoDlg dlg = null;
			try
			{
				switch (type)
				{
					case LexRefTypeTags.MappingTypes.kmtSensePair:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair: // Sense pair with different Forward/Reverse names
						dlg = new LinkEntryOrSenseDlg();
						(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryPair:
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair: // Entry pair with different Forward/Reverse names
						dlg = new EntryGoDlg();
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense pair with different Forward/Reverse
						dlg = new LinkEntryOrSenseDlg();
						(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = false;
						break;
				}
				Debug.Assert(dlg != null);
				var wp = new WindowParams();
				//on creating Pair Lexical Relation have an Add button and Add in the title bar
				if (Target == null)
				{
					wp.m_title = String.Format(LexEdStrings.ksIdentifyXEntry, lrt.Name.BestAnalysisAlternative.Text);
					wp.m_btnText = LexEdStrings.ks_Add;
				}
				else //Otherwise we are Replacing the item
				{
					wp.m_title = String.Format(LexEdStrings.ksReplaceXEntry);
					wp.m_btnText = LexEdStrings.ks_Replace;
				}

				dlg.SetDlgInfo(m_cache, wp, PropertyTable, Publisher);
				dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				{
					if (dlg.SelectedObject != null)
					{
						AddItem(dlg.SelectedObject);
						// it is possible that the previous update has caused the data tree to refresh
						if (!IsDisposed)
							m_atomicRefView.RootBox.Reconstruct(); // view is somehow too complex for auto-update.
					}
				}
			}
			finally
			{
				if (dlg != null)
					dlg.Dispose();
			}
		}

		public override void AddItem(ICmObject obj)
		{
			string undoStr, redoStr;
			if (Target == null)
			{
				undoStr = LexEdStrings.ksUndoAddRef;
				redoStr = LexEdStrings.ksRedoAddRef;
			}
			else
			{
				undoStr = LexEdStrings.ksUndoReplaceRef;
				redoStr = LexEdStrings.ksRedoReplaceRef;
			}
			AddItem(obj, undoStr, redoStr);
		}

		#region Component Designer generated code
		/// <summary>
		/// Everything except the Name is taken care of by the Superclass.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferencePairLauncher";
		}
		#endregion

		protected override AtomicReferenceView CreateAtomicReferenceView()
		{
			LexReferencePairView pv = new LexReferencePairView();
			if (m_displayParent != null)
				pv.DisplayParent = m_displayParent;
			return pv;
		}

		public ICmObject DisplayParent
		{
			set
			{
				CheckDisposed();

				m_displayParent = value;
				if (m_atomicRefView != null)
					(m_atomicRefView as LexReferencePairView).DisplayParent = value;
			}
		}
	}
}
