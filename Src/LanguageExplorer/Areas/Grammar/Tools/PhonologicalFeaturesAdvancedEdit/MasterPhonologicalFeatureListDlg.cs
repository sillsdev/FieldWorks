// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.MGA;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Grammar.Tools.PhonologicalFeaturesAdvancedEdit
{
	/// <summary />
	internal class MasterPhonologicalFeatureListDlg : MasterListDlg
	{
		public MasterPhonologicalFeatureListDlg()
		{
		}

		public MasterPhonologicalFeatureListDlg(string className)
			: base(className, new PhonologicalFeaturesTreeView())
		{
		}

		protected override void DoExtraInit()
		{
			Text = LanguageExplorerControls.ksPhonologicalFeatureCatalogTitle;
			label1.Text = LanguageExplorerControls.ksPhonologicalFeatureCatalogPrompt;
			label2.Text = LanguageExplorerControls.ksPhonologicalFeatureCatalogTreeLabel;
			label3.Text = LanguageExplorerControls.ksPhonologicalFeatureCatalogDescriptionLabel;
			var phonologicalFeature = LanguageExplorerControls.ksPhonologicalFeature;
			linkLabel1.Text = string.Format(LanguageExplorerControls.ksLinkText, phonologicalFeature, phonologicalFeature);
			s_helpTopic = "khtpInsertPhonologicalFeature";
		}

		protected override void DoFinalAdjustment(TreeNode treeNode)
		{
				CheckMeIfAllDaughtersAreChecked(treeNode);
		}

		private static void CheckMeIfAllDaughtersAreChecked(TreeNode node)
		{
			if (node.Nodes.Count == 0)
			{
				return; // nothing to do
			}
			foreach (TreeNode daughterNode in node.Nodes)
			{
				CheckMeIfAllDaughtersAreChecked(daughterNode);
			}
			if (AllDaughtersAreChecked(node))
			{
				node.Checked = true;
				node.ImageIndex = (int)MGAImageKind.checkedBox;
			}
		}

		private static bool AllDaughtersAreChecked(TreeNode node)
		{
			return node.Nodes.Count != 0 && node.Nodes.Cast<TreeNode>().All(daughterNode => daughterNode.Checked);
		}

		protected override void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(LanguageExplorerControls.ksUndoInsertPhonologicalFeature, LanguageExplorerControls.ksRedoInsertPhonologicalFeature, m_cache.ActionHandlerAccessor, () =>
			{
				SelectedFeatDefn = m_cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
				m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.Add(SelectedFeatDefn);
				// create the two default feature values
				IFsSymFeatVal symFV;
				var closed = SelectedFeatDefn as IFsClosedFeature;
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
			});
			DialogResult = DialogResult.Yes;
			Close();
		}
	}
}