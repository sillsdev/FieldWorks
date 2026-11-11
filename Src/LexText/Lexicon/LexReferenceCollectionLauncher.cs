// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceCollectionLauncher.
	/// </summary>
	public class LexReferenceCollectionLauncher : VectorReferenceLauncher
	{
		protected ICmObject m_displayParent;

		public LexReferenceCollectionLauncher()
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

			// New-UI gate (mirrors the EntrySequence gate): in New mode the LinkEntryOrSense relations use the
			// Avalonia Choose-Lexical-Entry-or-Sense dialog (sense / entry-or-sense per mapping type); the
			// entry-only kmtEntryCollection keeps its EntryGoDlg (out of this slice's scope). Legacy keeps WinForms.
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
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.SelectedObject != null)
				{
					if (!((ILexReference)m_obj).TargetsRS.Contains(dlg.SelectedObject))
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
			var lrcv = new LexReferenceCollectionView();
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
					((LexReferenceCollectionView)m_vectorRefView).DisplayParent = value;
			}
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceCollectionLauncher";
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
			if (chosen != null && !((ILexReference)m_obj).TargetsRS.Contains(chosen))
				AddItem(chosen);
		}
	}
}
