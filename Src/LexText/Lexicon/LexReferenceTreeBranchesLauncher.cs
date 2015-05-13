using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;
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
			ILexRefType lrt = (ILexRefType)m_obj.Owner;
			int type = lrt.MappingType;
			BaseGoDlg dlg = null;
			try
			{
				switch ((LexRefTypeTags.MappingTypes)type)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
						dlg = new LinkEntryOrSenseDlg();
						(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
						dlg = new EntryGoDlg();
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
						dlg = new LinkEntryOrSenseDlg();
						break;
				}
				Debug.Assert(dlg != null);
				var wp = new WindowParams { m_title = String.Format(LexEdStrings.ksIdentifyXEntry, lrt.Name.BestAnalysisAlternative.Text), m_btnText = LexEdStrings.ks_Add };
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

		protected override VectorReferenceView CreateVectorReverenceView()
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
	}
}
