// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.LCModel;
using SIL.FieldWorks.Common.DetailRules;
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

				LexicalRelationEditing.ReplacePairPartner((ILexReference)m_obj, m_displayParent, value);
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
			//on creating Pair Lexical Relation have an Add button and Add in the title bar; otherwise Replacing
			var chooser = LexicalRelationEditing.Chooser((ILexReference)m_obj, m_displayParent);

			// New-UI gate (mirrors the EntrySequence gate): the LinkEntryOrSense relations use the Avalonia
			// Choose-Lexical-Entry-or-Sense dialog in New mode; the entry-only entry pairs keep their EntryGoDlg.
			var uiMode = m_propertyTable.GetStringProperty("UIMode", null);
			if (chooser.UseLinkDialog && UIModeGates.ShouldUseAvaloniaUI(uiMode))
			{
				ShowAvaloniaLinkDialog(chooser.AllowSenses, chooser.SensesOnly, chooser.Title, chooser.ButtonText);
				return;
			}

			BaseGoDlg dlg = null;
			try
			{
				dlg = chooser.UseLinkDialog ? (BaseGoDlg)new LinkEntryOrSenseDlg() : new EntryGoDlg();
				if (chooser.UseLinkDialog)
					((LinkEntryOrSenseDlg)dlg).SelectSensesOnly = chooser.SensesOnly;
				Debug.Assert(dlg != null);
				var wp = new WindowParams { m_title = chooser.Title, m_btnText = chooser.ButtonText };

				dlg.SetDlgInfo(m_cache, wp, m_mediator, m_propertyTable);
				dlg.SetHelpTopic(LexicalRelationEditing.ChooserHelpTopic);
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
				undoStr = LexicalRelationEditing.UndoAddReference;
				redoStr = LexicalRelationEditing.RedoAddReference;
			}
			else
			{
				undoStr = LexicalRelationEditing.UndoReplaceReference;
				redoStr = LexicalRelationEditing.RedoReplaceReference;
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
