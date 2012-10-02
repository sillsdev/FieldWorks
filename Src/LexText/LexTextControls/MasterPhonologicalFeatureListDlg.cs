// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MasterPhonologicalFeatureListDlg.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.LexText.Controls.MGA;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MasterPhonologicalFeatureListDlg.
	/// </summary>
	public class MasterPhonologicalFeatureListDlg : MasterListDlg
	{
		// HOW do this??  private LexText.Controls.MGA.PhonologicalFeaturesTreeView m_tvMasterList;


		public MasterPhonologicalFeatureListDlg() : base()
		{
		}
		public MasterPhonologicalFeatureListDlg(string className) : base(className, new PhonologicalFeaturesTreeView())
		{
		}

		protected override void DoExtraInit()
		{
			Text = LexTextControls.ksPhonologicalFeatureCatalogTitle;
			label1.Text = LexTextControls.ksPhonologicalFeatureCatalogPrompt;
			label2.Text = LexTextControls.ksPhonologicalFeatureCatalogTreeLabel;
			label3.Text = LexTextControls.ksPhonologicalFeatureCatalogDescriptionLabel;
			string sPhonoFeat = LexTextControls.ksPhonologicalFeature;
			linkLabel1.Text = String.Format(LexTextControls.ksLinkText, sPhonoFeat, sPhonoFeat);
			s_helpTopic = "khtpInsertPhonologicalFeature";
		}

		protected override void DoFinalAdjustment(TreeNode treeNode)
		{
				CheckMeIfAllDaughtersAreChecked(treeNode);
		}
		private void CheckMeIfAllDaughtersAreChecked(TreeNode node)
		{
			if (node.Nodes.Count == 0)
				return; // nothing to do
			foreach (TreeNode daughterNode in node.Nodes)
			{
				CheckMeIfAllDaughtersAreChecked(daughterNode);
			}
			if (AllDaughtersAreChecked(node))
			{
				node.Checked = true;
				node.ImageIndex = (int) GlossListTreeView.ImageKind.checkedBox;
			}

		}
		private bool AllDaughtersAreChecked(TreeNode node)
		{
			if (node.Nodes.Count == 0)
				return false;  // no daughters, so they are not checked
			foreach (TreeNode daughterNode in node.Nodes)
			{
				if (!daughterNode.Checked)
					return false;
			}
			return true;
		}
		protected override void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			m_cache.BeginUndoTask(LexTextControls.ksUndoInsertPhonologicalFeature,
				LexTextControls.ksRedoInsertPhonologicalFeature);

			int flid;
			int featureHvo;
			int fsysHvo = CreatePhonologicalfeature(out flid, out featureHvo);

			// create the two default feature values
			IFsSymFeatVal symFV = null;
			FsClosedFeature closed = m_selFeatDefn as FsClosedFeature;
			if (closed != null)
			{
				symFV = closed.ValuesOC.Add(new FsSymFeatVal());
				symFV.SimpleInit("+", "positive");
				symFV = closed.ValuesOC.Add(new FsSymFeatVal());
				symFV.SimpleInit("-", "negative");
			}
			// end create
			m_cache.EndUndoTask();

			ForceRecordClerkToReload(fsysHvo, flid, featureHvo);

			DialogResult = DialogResult.Yes;
			Close();
		}

		private int CreatePhonologicalfeature(out int flid, out int featureHvo)
		{
			int fsysHvo = m_cache.LangProject.PhFeatureSystemOAHvo;
			flid = (int)FsFeatureSystem.FsFeatureSystemTags.kflidFeatures;
			featureHvo = m_cache.CreateObject(FsClosedFeature.kClassId, fsysHvo, flid, 0);
			m_selFeatDefn = (FsFeatDefn)CmObject.CreateFromDBObject(m_cache, featureHvo, true);
			return fsysHvo;
		}

	}
}
