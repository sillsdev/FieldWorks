// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexReferenceSequenceLauncher.
	/// TODO: how similar is this class to LexReferenceTreeBranchesLauncher and
	/// LexReferenceCollectionLauncher? Could they all inherit from the same class?
	/// </summary>
	internal class LexReferenceSequenceLauncher : VectorReferenceLauncher
	{
		/// <summary />
		protected ICmObject m_displayParent = null;

		/// <summary />
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
			ILexRefType lrt = (ILexRefType)m_obj.Owner;
			int type = lrt.MappingType;
			BaseGoDlg dlg = null;
			try
			{
				switch ((LexRefTypeTags.MappingTypes)type)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseSequence:
						dlg = new LinkEntryOrSenseDlg();
						(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
						break;
					case LexRefTypeTags.MappingTypes.kmtEntrySequence:
						dlg = new EntryGoDlg();
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
						dlg = new LinkEntryOrSenseDlg();
						break;
				}
				Debug.Assert(dlg != null);
				var wp = new WindowParams
				{
					m_title = String.Format(LanguageExplorerResources.ksIdentifyXEntry,
					lrt.Name.BestAnalysisAlternative.Text),
					m_btnText = LanguageExplorerResources.ks_Add
				};
				dlg.SetDlgInfo(m_cache, wp, PropertyTable, Publisher);
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

		/// <summary />
		public override void AddItem(ICmObject obj)
		{
			AddItem(obj, LanguageExplorerResources.ksUndoAddRef, LanguageExplorerResources.ksRedoAddRef);
		}

		/// <summary />
		protected override VectorReferenceView CreateVectorReferenceView()
		{
			LexReferenceSequenceView lrcv = new LexReferenceSequenceView();
			if (m_displayParent != null)
				lrcv.DisplayParent = m_displayParent;
			return lrcv;
		}

		/// <summary />
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
	}
}
