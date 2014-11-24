// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MasterPhonologicalFeatureListDlg.cs
// Responsibility:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls.MGA;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MasterPhonologicalFeatureListDlg.
	/// </summary>
	public class MasterPhonologicalFeatureListDlg : MasterListDlg
	{
		public MasterPhonologicalFeatureListDlg()
		{
		}
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "PhonologicalFeaturesTreeView gets disposed in base class")]
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
		protected override void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoInsertPhonologicalFeature, LexTextControls.ksRedoInsertPhonologicalFeature,
				m_cache.ActionHandlerAccessor, () =>
			{
				m_selFeatDefn = m_cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
				m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.Add(m_selFeatDefn);

				// create the two default feature values
				IFsSymFeatVal symFV;
				var closed = m_selFeatDefn as IFsClosedFeature;
				if (closed != null)
				{
					var symFeatFactory = m_cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>();
					symFV = symFeatFactory.Create();
					closed.ValuesOC.Add(symFV);
					symFV.SimpleInit("+", "positive");
					symFV = symFeatFactory.Create();
					closed.ValuesOC.Add(symFV);
					symFV.SimpleInit("-", "negative");
				}
				// end create
			});

			DialogResult = DialogResult.Yes;
			Close();
		}
	}
}
