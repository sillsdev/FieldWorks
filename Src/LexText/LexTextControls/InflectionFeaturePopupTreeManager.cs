using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
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

		#region Data members
		private Mediator m_mediator;

		#endregion Data members

		#region Events

		#endregion Events

		/// <summary>
		/// Constructor.
		/// </summary>
		public InflectionFeaturePopupTreeManager(TreeCombo treeCombo, FdoCache cache,  bool useAbbr, Mediator mediator, Form parent, int wsDisplay)
			: base(treeCombo, cache, cache.LangProject.PartsOfSpeechOA, wsDisplay, useAbbr, parent)
		{
			m_mediator = mediator;
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			int tagNamePOS = UseAbbr ?
				(int)CmPossibility.CmPossibilityTags.kflidAbbreviation :
				(int)CmPossibility.CmPossibilityTags.kflidName;

			List<HvoTreeNode> relevantPartsOfSpeech = new List<HvoTreeNode>();
			InflectionClassPopupTreeManager.GatherPartsOfSpeech(Cache, List.Hvo,
				(int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities,
				(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities,
				(int)PartOfSpeech.PartOfSpeechTags.kflidInflectableFeats,
				tagNamePOS, WritingSystem,
				relevantPartsOfSpeech);
			relevantPartsOfSpeech.Sort();
			TreeNode match = null;
			foreach(HvoTreeNode item in relevantPartsOfSpeech)
			{
				popupTree.Nodes.Add(item);
				IPartOfSpeech pos = (IPartOfSpeech)PartOfSpeech.CreateFromDBObject(Cache, item.Hvo, false);
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
				item.Nodes.Add(new HvoTreeNode(Cache.MakeUserTss(LexTextControls.ksChooseInflFeats), kMore));
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
						IPartOfSpeech pos = (IPartOfSpeech)PartOfSpeech.CreateFromDBObject(Cache, hvoPos, false);
						dlg.SetDlgInfo(Cache, m_mediator, pos);
						switch (dlg.ShowDialog(ParentForm))
						{
							case DialogResult.OK:
							{
								int hvoFs = 0;
								if (dlg.FS != null)
									hvoFs = dlg.FS.Hvo;
								LoadPopupTree(hvoFs);
								// everything should be setup with new node selected, so return.
								return;
							}
							case DialogResult.Yes:
							{
								// go to m_highestPOS in editor
								// Also, is there some way to know the application name and tool name without hard coding them?
								FdoUi.FwLink linkJump = new SIL.FieldWorks.FdoUi.FwLink("Language Explorer", "posEdit",
									dlg.HighestPOS.Guid, Cache.ServerName, Cache.DatabaseName);
								m_mediator.PostMessage("FollowLink", linkJump);
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

			base.m_treeCombo_AfterSelect(sender, e);
		}
	}
}
