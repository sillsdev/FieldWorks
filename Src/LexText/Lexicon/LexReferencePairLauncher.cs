// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.LCModel;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
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
			bool useLinkDlg = false, sensesOnly = false, allowSenses = false;
			switch (type)
			{
				case LexRefTypeTags.MappingTypes.kmtSensePair:
				case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair: // Sense pair with different Forward/Reverse names
					useLinkDlg = true; sensesOnly = true;
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryPair:
				case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair: // Entry pair with different Forward/Reverse names
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense pair with different Forward/Reverse
					useLinkDlg = true; allowSenses = true;
					break;
			}
			//on creating Pair Lexical Relation have an Add button and Add in the title bar; otherwise Replacing
			string sTitle, btnText;
			if (Target == null)
			{
				sTitle = String.Format(LexEdStrings.ksIdentifyXEntry, lrt.Name.BestAnalysisAlternative.Text);
				btnText = LexEdStrings.ks_Add;
			}
			else
			{
				sTitle = String.Format(LexEdStrings.ksReplaceXEntry);
				btnText = LexEdStrings.ks_Replace;
			}

			// New-UI gate (mirrors the EntrySequence gate): the LinkEntryOrSense relations use the Avalonia
			// Choose-Lexical-Entry-or-Sense dialog in New mode; the entry-only entry pairs keep their EntryGoDlg.
			var uiMode = m_propertyTable.GetStringProperty("UIMode", null);
			if (useLinkDlg && UIModeGates.ShouldUseAvaloniaUI(uiMode))
			{
				ShowAvaloniaLinkDialog(allowSenses, sensesOnly, sTitle, btnText);
				return;
			}

			BaseGoDlg dlg = null;
			try
			{
				dlg = useLinkDlg ? (BaseGoDlg)new LinkEntryOrSenseDlg() : new EntryGoDlg();
				if (useLinkDlg)
					((LinkEntryOrSenseDlg)dlg).SelectSensesOnly = sensesOnly;
				Debug.Assert(dlg != null);
				var wp = new WindowParams { m_title = sTitle, m_btnText = btnText };

				dlg.SetDlgInfo(m_cache, wp, m_mediator, m_propertyTable);
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

		// NoInlining keeps the Avalonia assembly load out of the gated caller's JIT (Legacy loader isolation).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ShowAvaloniaLinkDialog(bool allowSenses, bool sensesOnly, string sTitle, string btnText)
		{
			var helpProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider", null);
			var chosen = LcmLinkEntryOrSenseDialogLauncher.Show(m_cache, m_mediator, m_propertyTable, null,
				FindForm(), "khtpChooseLexicalRelationAdd", helpProvider, allowSenses: allowSenses,
				sensesOnly: sensesOnly, title: sTitle, okButtonText: btnText);
			if (chosen != null)
			{
				AddItem(chosen);
				// it is possible that the previous update has caused the data tree to refresh
				if (!IsDisposed)
					m_atomicRefView.RootBox.Reconstruct(); // view is somehow too complex for auto-update.
			}
		}
	}
}
