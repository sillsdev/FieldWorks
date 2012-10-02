using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceCollectionLauncher.
	/// </summary>
	public class LexReferenceCollectionLauncher : VectorReferenceLauncher
	{
		protected int m_hvoDisplayParent = 0;

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
			ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, m_obj.OwnerHVO);
			LexRefType.MappingTypes type = (LexRefType.MappingTypes)lrt.MappingType;
			BaseGoDlg dlg = null;
			string sTitle="";
			switch (type)
			{
				case LexRefType.MappingTypes.kmtSenseCollection:
					dlg = new LinkEntryOrSenseDlg();
					(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
					sTitle = String.Format(LexEdStrings.ksIdentifyXSense,
						lrt.Name.BestAnalysisAlternative.Text);
					break;
				case LexRefType.MappingTypes.kmtEntryCollection:
					dlg = new GoDlg();
					sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntry,
						lrt.Name.BestAnalysisAlternative.Text);
					break;
				case LexRefType.MappingTypes.kmtEntryOrSenseCollection:
					dlg = new LinkEntryOrSenseDlg();
					sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntryOrSense,
						lrt.Name.BestAnalysisAlternative.Text);
					break;
			}
			Debug.Assert(dlg != null);
			WindowParams wp = new WindowParams();
			wp.m_title = sTitle;
			wp.m_label = LexEdStrings.ksFind_;
			wp.m_btnText = LexEdStrings.ks_Add; //for collection relation of items always have an Add button
			dlg.SetDlgInfo(m_cache, wp, m_mediator);
			dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				AddItem(dlg.SelectedID);
			dlg.Dispose();
		}

		public override void AddItem(int hvoNew)
		{
			CheckDisposed();

			ILexReference lr = m_obj as ILexReference;
			List<int> senses = new List<int>();
			foreach (int hvo in lr.TargetsRS.HvoArray)
			{
				// Don't duplicate entries for simple collections.
				if (hvo == hvoNew)
					return;
				senses.Add(hvo);
			}
			senses.Add(hvoNew);
			SetItems(senses);
			Debug.Assert(senses.Count == lr.TargetsRS.Count && senses[0] == lr.TargetsRS[0].Hvo);
			(lr as LexReference).UpdateTargetTimestamps();
		}

		protected override VectorReferenceView CreateVectorReverenceView()
		{
			LexReferenceCollectionView lrcv = new LexReferenceCollectionView();
			if (m_hvoDisplayParent != 0)
				lrcv.DisplayParentHvo = m_hvoDisplayParent;
			return lrcv;
		}

		public int DisplayParentHvo
		{
			set
			{
				CheckDisposed();

				m_hvoDisplayParent = value;
				if (m_vectorRefView != null)
					(m_vectorRefView as LexReferenceCollectionView).DisplayParentHvo = value;
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
