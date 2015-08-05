using System.Collections.Generic;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Widgets;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Handles a TreeCombo control (Widgets assembly) for use in selecting inflection features.
	/// </summary>
	public class InflectionFeaturePopupTreeManager : PopupTreeManager
	{
		private const int kEmpty = 0;
		private const int kLine = -1;
		private const int kMore = -2;

		/// <summary>
		/// Constructor.
		/// </summary>
		public InflectionFeaturePopupTreeManager(TreeCombo treeCombo, FdoCache cache, bool useAbbr, Mediator mediator, IPropertyTable propertyTable, Form parent, int wsDisplay)
			: base(treeCombo, cache, mediator, propertyTable, cache.LanguageProject.PartsOfSpeechOA, wsDisplay, useAbbr, parent)
		{
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			int tagNamePOS = UseAbbr ?
				CmPossibilityTags.kflidAbbreviation :
				CmPossibilityTags.kflidName;

			List<HvoTreeNode> relevantPartsOfSpeech = new List<HvoTreeNode>();
			InflectionClassPopupTreeManager.GatherPartsOfSpeech(Cache, List.Hvo,
				CmPossibilityListTags.kflidPossibilities,
				CmPossibilityTags.kflidSubPossibilities,
				PartOfSpeechTags.kflidInflectableFeats,
				tagNamePOS, WritingSystem,
				relevantPartsOfSpeech);
			relevantPartsOfSpeech.Sort();
			TreeNode match = null;
			foreach(HvoTreeNode item in relevantPartsOfSpeech)
			{
				popupTree.Nodes.Add(item);
				var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(item.Hvo);
				foreach(IFsFeatStruc fs in pos.ReferenceFormsOC)
				{
					// Note: beware of using fs.ShortName. That can be
					// absolutely EMPTY (if the user has turned off the 'Show Abbreviation as its label'
					// field for both the feature category and value).
					// ChooserName shows the short name if it is non-empty, otherwise the long name.
					HvoTreeNode node = new HvoTreeNode(fs.ChooserNameTS, fs.Hvo);
					item.Nodes.Add(node);
					if (fs.Hvo == hvoTarget)
						match = node;
				}
				item.Nodes.Add(new HvoTreeNode(
					Cache.TsStrFactory.MakeString(LexTextControls.ksChooseInflFeats, Cache.WritingSystemFactory.UserWs),
					kMore));
			}
			return match;
		}

		protected override void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			HvoTreeNode selectedNode = e.Node as HvoTreeNode;
			PopupTree pt = GetPopupTree();

			switch (selectedNode.Hvo)
			{
				case kMore:
					// Only launch the dialog by a mouse click (or simulated mouse click).
					if (e.Action != TreeViewAction.ByMouse)
						break;
					// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
					// This will effectively revert the list selection to a previous confirmed state.
					// Whatever happens below, we don't want to actually leave the "More..." node selected!
					// This is at least required if the user selects "Cancel" from the dialog below.
					pt.Hide();
					using (MsaInflectionFeatureListDlg dlg = new MsaInflectionFeatureListDlg())
					{
						HvoTreeNode parentNode = selectedNode.Parent as HvoTreeNode;
						int hvoPos = parentNode.Hvo;
						var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoPos);
						dlg.SetDlgInfo(Cache, m_mediator, m_propertyTable, pos);
						switch (dlg.ShowDialog(ParentForm))
						{
							case DialogResult.OK:
							{
								int hvoFs = 0;
								if (dlg.FS != null)
									hvoFs = dlg.FS.Hvo;
								LoadPopupTree(hvoFs);
								// In the course of loading the popup tree, we will have selected the hvoFs item, and triggered an AfterSelect.
								// But, it will have had an Unknown action, and thus will not trigger some effects we want.
								// That one will work like arrowing over items: they are 'selected', but the system will not
								// behave as if the user actually chose this item.
								// But, we want clicking OK in the dialog to produce the same result as clicking an item in the list.
								// So, we need to trigger an AfterSelect with our own event args, which (since we're acting on it)
								// must have a ByMouse TreeViewAction.
								base.m_treeCombo_AfterSelect(sender, e);
								// everything should be setup with new node selected, so return.
								return;
							}
							case DialogResult.Yes:
							{
								// go to m_highestPOS in editor
								m_mediator.PostMessage("FollowLink", new FwLinkArgs("posEdit", dlg.HighestPOS.Guid));
								if (ParentForm != null && ParentForm.Modal)
								{
									// Close the dlg that opened the popup tree,
									// since its hotlink was used to close it,
									// and a new item has been created.
									ParentForm.DialogResult = DialogResult.Cancel;
									ParentForm.Close();
								}
								break;
							}
							default:
								// NOTE: If the user has selected "Cancel", then don't change
								// our m_lastConfirmedNode to the "More..." node. Keep it
								// the value set by popupTree_PopupTreeClosed() when we
								// called pt.Hide() above. (cf. comments in LT-2522)
								break;
						}
					}
					break;
				default:
					break;
			}
			// FWR-3432 - If we get here and we still haven't got a valid Hvo, don't continue
			// on to the base method. It'll crash.
			if (selectedNode.Hvo == kMore)
				return;
			base.m_treeCombo_AfterSelect(sender, e);
		}
	}
}
