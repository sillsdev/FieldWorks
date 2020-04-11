// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary />
	internal sealed class FeatureSystemInflectionFeatureListDlgLauncher : MsaInflectionFeatureListDlgLauncher
	{
		/// <summary>
		/// Handle launching of the LexEntryInflType features editor.
		/// </summary>
		protected override void HandleChooser()
		{
			VectorReferenceLauncher vrl = null;
			using (var dlg = new FeatureSystemInflectionFeatureListDlg())
			{
				var originalFs = m_obj as IFsFeatStruc;
				var mySlice = Slice;
				var owningFlid = mySlice.Flid;
				if (originalFs == null)
				{
					var leit = (ILexEntryInflType)mySlice.MyCmObject;
					dlg.SetDlgInfo(m_cache, PropertyTable, leit, owningFlid);
				}
				else
				{
					dlg.SetDlgInfo(m_cache, PropertyTable, originalFs, owningFlid);
				}
				const string ksPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='FeatureChooser']/";
				dlg.Text = StringTable.Table.GetStringWithXPath("InflectionFeatureTitle", ksPath);
				dlg.Prompt = StringTable.Table.GetStringWithXPath("InflectionFeaturesPrompt", ksPath);
				dlg.LinkText = StringTable.Table.GetStringWithXPath("InflectionFeaturesLink", ksPath);
				var result = dlg.ShowDialog(mySlice.FindForm());
				switch (result)
				{
					case DialogResult.OK:
						if (dlg.FS != null)
						{
							m_obj = dlg.FS;
						}
						m_msaInflectionFeatureListDlgLauncherView.Init(m_cache, dlg.FS);
						break;
					case DialogResult.Yes:
						LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(LanguageExplorerConstants.FeaturesAdvancedEditMachineName, m_cache.LanguageProject.MsFeatureSystemOA.Guid));
						break;
				}
			}
		}
	}
}