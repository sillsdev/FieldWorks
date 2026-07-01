// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceUnidirectionalLauncher.
	/// </summary>
	public class LexReferenceUnidirectionalLauncher : VectorReferenceLauncher
	{
		public LexReferenceUnidirectionalLauncher()
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
			ILexRefType lrt = (ILexRefType)m_obj.Owner;
			int type = lrt.MappingType;
			bool useLinkDlg = false, sensesOnly = false, allowSenses = false;
			switch ((LexRefTypeTags.MappingTypes)type)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					useLinkDlg = true; sensesOnly = true;
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional:
					useLinkDlg = true; allowSenses = true;
					break;
			}
			var sTitle = String.Format(LexEdStrings.ksIdentifyXEntry, lrt.Name.BestAnalysisAlternative.Text);

			// New-UI gate (mirrors the EntrySequence gate): the LinkEntryOrSense relations use the Avalonia
			// Choose-Lexical-Entry-or-Sense dialog in New mode; the entry-only kmtEntryUnidirectional keeps EntryGoDlg.
			var uiMode = m_propertyTable.GetStringProperty("UIMode", null);
			if (useLinkDlg && AvaloniaOptionsDialogLauncher.ShouldUseAvaloniaOptionsDialog(uiMode))
			{
				var helpProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider", null);
				var chosen = LcmLinkEntryOrSenseDialogLauncher.Show(m_cache, m_mediator, m_propertyTable, null,
					FindForm(), "khtpChooseLexicalRelationAdd", helpProvider, allowSenses: allowSenses,
					sensesOnly: sensesOnly, title: sTitle, okButtonText: LexEdStrings.ks_Add);
				if (chosen != null && !(m_obj as ILexReference).TargetsRS.Contains(chosen))
					AddItem(chosen);
				return;
			}

			BaseGoDlg dlg = null;
			try
			{
				dlg = useLinkDlg ? (BaseGoDlg)new LinkEntryOrSenseDlg() : new EntryGoDlg();
				if (useLinkDlg)
					((LinkEntryOrSenseDlg)dlg).SelectSensesOnly = sensesOnly;
				Debug.Assert(dlg != null);
				var wp = new WindowParams { m_title = sTitle, m_btnText = LexEdStrings.ks_Add };
				dlg.SetDlgInfo(m_cache, wp, m_mediator, m_propertyTable);
				dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
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
			AddItem(obj, LexEdStrings.ksUndoAddRef, LexEdStrings.ksRedoAddRef);
		}

		protected override VectorReferenceView CreateVectorReferenceView()
		{
			return new LexReferenceUnidirectionalView();
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceUnidirectionalLauncher";
		}
		#endregion
	}
}
