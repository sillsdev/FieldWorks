// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using LanguageExplorer.MGA;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Summary description for MasterPhonologicalFeatureListDlg.
	/// </summary>
	internal class MasterPhonologicalFeatureListDlg : MasterListDlg
	{
		public MasterPhonologicalFeatureListDlg()
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
			var sPhonoFeat = LexTextControls.ksPhonologicalFeature;
			linkLabel1.Text = string.Format(LexTextControls.ksLinkText, sPhonoFeat, sPhonoFeat);
			s_helpTopic = "khtpInsertPhonologicalFeature";
		}

		protected override void DoFinalAdjustment(TreeNode treeNode)
		{
				CheckMeIfAllDaughtersAreChecked(treeNode);
		}
		private void CheckMeIfAllDaughtersAreChecked(TreeNode node)
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
		private bool AllDaughtersAreChecked(TreeNode node)
		{
			if (node.Nodes.Count == 0)
			{
				return false;  // no daughters, so they are not checked
			}
			foreach (TreeNode daughterNode in node.Nodes)
			{
				if (!daughterNode.Checked)
				{
					return false;
				}
			}
			return true;
		}
		protected override void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoInsertPhonologicalFeature, LexTextControls.ksRedoInsertPhonologicalFeature,
				m_cache.ActionHandlerAccessor, () =>
			{
				SelectedFeatDefn = m_cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
				m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.Add(SelectedFeatDefn);

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
