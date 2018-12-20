// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal sealed class LexReferenceCollectionLauncher : VectorReferenceLauncher
	{
		/// <summary />
		private ICmObject m_displayParent;

		/// <summary />
		public LexReferenceCollectionLauncher()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Wrapper for HandleChooser() to make it available to the slice.
		/// </summary>
		internal void LaunchChooser()
		{
			HandleChooser();
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries.
		/// </summary>
		protected override void HandleChooser()
		{
			var lrt = (ILexRefType)m_obj.Owner;
			var type = (LexRefTypeTags.MappingTypes)lrt.MappingType;
			BaseGoDlg dlg = null;
			var sTitle = string.Empty;
			try
			{
				switch (type)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
						dlg = new LinkEntryOrSenseDlg();
						((LinkEntryOrSenseDlg)dlg).SelectSensesOnly = true;
						sTitle = string.Format(LanguageExplorerResources.ksIdentifyXSense, lrt.Name.BestAnalysisAlternative.Text);
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryCollection:
						dlg = new EntryGoDlg();
						sTitle = string.Format(LanguageExplorerResources.ksIdentifyXLexEntry, lrt.Name.BestAnalysisAlternative.Text);
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
						dlg = new LinkEntryOrSenseDlg();
						sTitle = string.Format(LanguageExplorerResources.ksIdentifyXLexEntryOrSense, lrt.Name.BestAnalysisAlternative.Text);
						break;
				}
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				var wp = new WindowParams { m_title = sTitle, m_btnText = LanguageExplorerResources.ks_Add };
				dlg.SetDlgInfo(m_cache, wp);
				dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.SelectedObject != null)
				{
					if (!((ILexReference)m_obj).TargetsRS.Contains(dlg.SelectedObject))
					{
						AddItem(dlg.SelectedObject);
					}
				}
			}
			finally
			{
				dlg?.Dispose();
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
			var lrcv = new LexReferenceCollectionView();
			if (m_displayParent != null)
			{
				lrcv.DisplayParent = m_displayParent;
			}
			return lrcv;
		}

		/// <summary />
		public ICmObject DisplayParent
		{
			set
			{
				m_displayParent = value;
				if (m_vectorRefView != null)
				{
					((LexReferenceCollectionView)m_vectorRefView).DisplayParent = value;
				}
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
	}
}