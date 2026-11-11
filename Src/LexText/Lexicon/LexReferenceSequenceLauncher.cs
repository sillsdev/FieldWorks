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
	/// Summary description for LexReferenceSequenceLauncher.
	/// TODO: how similar is this class to LexReferenceTreeBranchesLauncher and
	/// LexReferenceCollectionLauncher? Could they all inherit from the same class?
	/// </summary>
	public class LexReferenceSequenceLauncher : VectorReferenceLauncher
	{
		protected ICmObject m_displayParent = null;

		public LexReferenceSequenceLauncher()
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
			var chooser = LexicalRelationEditing.Chooser((ILexReference)m_obj, m_displayParent);

			// New-UI gate (mirrors the EntrySequence gate): the LinkEntryOrSense relations use the Avalonia
			// Choose-Lexical-Entry-or-Sense dialog in New mode; the entry-only kmtEntrySequence keeps its EntryGoDlg.
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
			LexReferenceSequenceView lrcv = new LexReferenceSequenceView();
			if (m_displayParent != null)
				lrcv.DisplayParent = m_displayParent;
			return lrcv;
		}

		public ICmObject DisplayParent
		{
			set
			{
				CheckDisposed();

				m_displayParent = value;
				if (m_vectorRefView != null)
					(m_vectorRefView as LexReferenceSequenceView).DisplayParent = value;
			}
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceSequenceLauncher";
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
