// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText
{
	public class FeatureSystemInflectionFeatureListDlg : MsaInflectionFeatureListDlg
	{
		public FeatureSystemInflectionFeatureListDlg()
			: base()
		{
		}

		protected override void EnableLink()
		{
			linkLabel1.Enabled = true;
		}

		protected override void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// code in the launcher handles the jump
			DialogResult = DialogResult.Yes;
			Close();
		}

		/// <summary>
		/// Get/Set prompt text
		/// </summary>
		public override string Prompt
		{
			get
			{
				return labelPrompt.Text;
			}
			set
			{
				labelPrompt.Text = value;
			}
		}

		/// <summary>
		/// Get/Set link text
		/// </summary>
		public override string LinkText
		{
			get
			{
				return linkLabel1.Text;
			}
			set
			{
				linkLabel1.Text = value;
			}
		}
		/// <summary>
		/// Load the tree items if the starting point is a feature structure.
		/// </summary>
		protected override void LoadInflFeats(IFsFeatStruc fs)
		{
			PopulateTreeFromFeatureSystem();
			m_tvMsaFeatureList.PopulateTreeFromFeatureStructure(fs);
			FinishLoading();
		}

		/// <summary>
		/// Load the tree items if the starting point is an owning MSA and flid.
		/// </summary>
		protected override void LoadInflFeats(ICmObject cobj, int owningFlid)
		{
			PopulateTreeFromFeatureSystem();
			FinishLoading();
		}

		/// <summary>
		/// Get the top level complex features
		/// Also get top level closed features which are not used by any complex feature
		/// (to tell, we have to look at the types)
		/// </summary>
		private void PopulateTreeFromFeatureSystem()
		{
			var featureSystem = m_cache.LangProject.MsFeatureSystemOA;
			var topLevelComplexFeatureDefinitions = featureSystem.FeaturesOC.Where(fd => fd.ClassID == FsComplexFeatureTags.kClassId);
			m_tvMsaFeatureList.PopulateTreeFromInflectableFeats(topLevelComplexFeatureDefinitions);
			var topLevelClosedFeatureDefinitions = featureSystem.FeaturesOC.Where(fd => fd.ClassID == FsClosedFeatureTags.kClassId);
			foreach (var closedFeatureDefinition in topLevelClosedFeatureDefinitions)
			{
				var typeUsedByComplexFormForThisClosedFeature = topLevelComplexFeatureDefinitions.Cast<IFsComplexFeature>()
					.Select(cx => cx.TypeRA).Where(t => t.FeaturesRS.Contains(closedFeatureDefinition));
				if (!typeUsedByComplexFormForThisClosedFeature.Any())
				{
					m_tvMsaFeatureList.PopulateTreeFromInflectableFeat(closedFeatureDefinition);
				}
			}
		}

		public FeatureStructureTreeView TreeView => m_tvMsaFeatureList;
	}
}