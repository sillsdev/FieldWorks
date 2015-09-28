// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using LanguageExplorer.Areas.Grammar;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

namespace LanguageExplorer.Areas.Lists.Tools.FeatureTypesAdvancedEdit
{
	/// <summary />
	internal sealed class FeatureSystemInflectionFeatureListDlgLauncher : MsaInflectionFeatureListDlgLauncher
	{
		/// <summary />
		public FeatureSystemInflectionFeatureListDlgLauncher()
			: base()
		{
		}

		/// <summary>
		/// Handle launching of the LexEntryInflType features editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			VectorReferenceLauncher vrl = null;
			using (FeatureSystemInflectionFeatureListDlg dlg = new FeatureSystemInflectionFeatureListDlg())
			{
				IFsFeatStruc originalFs = m_obj as IFsFeatStruc;

				Slice parentSlice = Slice;
				if (originalFs == null)
				{
					int owningFlid;
					ILexEntryInflType leit = parentSlice.Object as ILexEntryInflType;
					owningFlid = (parentSlice as FeatureSystemInflectionFeatureListDlgLauncherSlice).Flid;
					dlg.SetDlgInfo(m_cache, PropertyTable, leit, owningFlid);
				}
				else
				{
					dlg.SetDlgInfo(m_cache, PropertyTable, originalFs, (parentSlice as FeatureSystemInflectionFeatureListDlgLauncherSlice).Flid);
				}

				const string ksPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='FeatureChooser']/";
				dlg.Text = StringTable.Table.GetStringWithXPath("InflectionFeatureTitle", ksPath);
				dlg.Prompt = StringTable.Table.GetStringWithXPath("InflectionFeaturesPrompt", ksPath);
				dlg.LinkText = StringTable.Table.GetStringWithXPath("InflectionFeaturesLink", ksPath);
				DialogResult result = dlg.ShowDialog(parentSlice.FindForm());
				if (result == DialogResult.OK)
				{
					if (dlg.FS != null)
						m_obj = dlg.FS;
					m_msaInflectionFeatureListDlgLauncherView.Init(m_cache, dlg.FS);
				}
				else if (result == DialogResult.Yes)
				{
					var commands = new List<string>
					{
						"AboutToFollowLink",
						"FollowLink"
					};
					var parms = new List<object>
					{
						null,
						new FwLinkArgs("featuresAdvancedEdit", m_cache.LanguageProject.MsFeatureSystemOA.Guid)
					};
					Publisher.Publish(commands, parms);
				}
			}
		}
	}
}