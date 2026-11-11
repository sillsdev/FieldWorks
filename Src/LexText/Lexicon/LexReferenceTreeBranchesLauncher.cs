// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceTreeBranchesLauncher.
	/// </summary>
	public class LexReferenceTreeBranchesLauncher : VectorReferenceLauncher
	{
		public LexReferenceTreeBranchesLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
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
			// This launcher sits on the whole's row, so the whole is the shown object.
			var relation = (ILexReference)m_obj;
			var chooser = LexicalRelationEditing.Chooser(relation, relation.TargetsRS.Count > 0 ? relation.TargetsRS[0] : null);

			// New-UI gate (mirrors the EntrySequence gate): the LinkEntryOrSense relations use the Avalonia
			// Choose-Lexical-Entry-or-Sense dialog in New mode; the entry-only kmtEntryTree keeps its EntryGoDlg.
			var uiMode = m_propertyTable.GetStringProperty("UIMode", null);
			if (chooser.UseLinkDialog && UIModeGates.ShouldUseAvaloniaUI(uiMode))
			{
				ShowAvaloniaLinkDialog(chooser.AllowSenses, chooser.SensesOnly, chooser.Title);
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
					if (!(m_obj as ILexReference).TargetsRS.Contains(dlg.SelectedObject))
						AddItem(dlg.SelectedObject);
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
			AddItem(obj, LexicalRelationEditing.UndoAddReference, LexicalRelationEditing.RedoAddReference);
		}

		protected override VectorReferenceView CreateVectorReferenceView()
		{
			return new LexReferenceTreeBranchesView();
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceTreeBranchesLauncher";
		}
		#endregion

		// NoInlining keeps the Avalonia assembly load out of the gated caller's JIT (Legacy loader isolation).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ShowAvaloniaLinkDialog(bool allowSenses, bool sensesOnly, string sTitle)
		{
			var helpProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider", null);
			var chosen = LcmLinkEntryOrSenseDialogLauncher.Show(m_cache, m_mediator, m_propertyTable, null,
				FindForm(), "khtpChooseLexicalRelationAdd", helpProvider, allowSenses: allowSenses,
				sensesOnly: sensesOnly, title: sTitle, okButtonText: LexicalRelationEditing.AddButtonText);
			if (chosen != null && !(m_obj as ILexReference).TargetsRS.Contains(chosen))
				AddItem(chosen);
		}
	}
}
