// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
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
			var lrt = (ILexRefType)m_obj.Owner;
			var type = (LexRefTypeTags.MappingTypes)lrt.MappingType;
			BaseGoDlg dlg = null;
			string sTitle = string.Empty;
			try
			{
				switch (type)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
						dlg = new LinkEntryOrSenseDlg();
						(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
						sTitle = String.Format(LexEdStrings.ksIdentifyXSense,
							lrt.Name.BestAnalysisAlternative.Text);
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryCollection:
						dlg = new EntryGoDlg();
						sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntry,
							lrt.Name.BestAnalysisAlternative.Text);
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
						dlg = new LinkEntryOrSenseDlg();
						sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntryOrSense,
							lrt.Name.BestAnalysisAlternative.Text);
						break;
				}
				Debug.Assert(dlg != null);
				var wp = new WindowParams { m_title = sTitle, m_btnText = LexEdStrings.ks_Add };
				dlg.SetDlgInfo(m_cache, wp, m_mediator);
				dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
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
			AddItem(obj, LexEdStrings.ksUndoAddRef, LexEdStrings.ksRedoAddRef);
		}

		protected override VectorReferenceView CreateVectorReverenceView()
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
	}
}
