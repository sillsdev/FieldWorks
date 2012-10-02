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
			ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, m_obj.OwnerHVO);
			int type = lrt.MappingType;
			BaseGoDlg dlg = null;
			switch ((LexRefType.MappingTypes)type)
			{
				case LexRefType.MappingTypes.kmtSenseTree:
					dlg = new LinkEntryOrSenseDlg();
					(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
					break;
				case LexRefType.MappingTypes.kmtEntryTree:
					dlg = new GoDlg();
					break;
				case LexRefType.MappingTypes.kmtEntryOrSenseTree:
					dlg = new LinkEntryOrSenseDlg();
					break;
			}
			Debug.Assert(dlg != null);
			WindowParams wp = new WindowParams();
			wp.m_title = String.Format(LexEdStrings.ksIdentifyXEntry,
				lrt.Name.AnalysisDefaultWritingSystem);
			wp.m_label = LexEdStrings.ksFind_;
			wp.m_btnText = LexEdStrings.ks_Add; //for parts of a whole always have an Add button
			dlg.SetDlgInfo(m_cache, wp, m_mediator);
			dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				AddItem(dlg.SelectedID);
			dlg.Dispose();
		}

		protected override VectorReferenceView CreateVectorReverenceView()
		{
			return new LexReferenceTreeBranchesView();
		}

		public override void AddItem(int hvoNew)
		{
			CheckDisposed();

			LexReference lr = m_obj as LexReference;
			List<int> senses = new List<int>();
			for (int i = 0; i < lr.TargetsRS.Count; i++)
			{
				int hvo = lr.TargetsRS.HvoArray[i];
				// Don't duplicate entries.
				if (hvo == hvoNew)
					return;
				senses.Add(hvo);
			}
			senses.Add(hvoNew);
			SetItems(senses);
			Debug.Assert(senses.Count == lr.TargetsRS.Count && senses[0] == lr.TargetsRS[0].Hvo);
			lr.UpdateTargetTimestamps();
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
