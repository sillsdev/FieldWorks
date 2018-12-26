// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Handles a TreeCombo control for use with PartOfSpeech objects.
	/// </summary>
	public class POSPopupTreeManager : PopupTreeManager
	{
		private const int kEmpty = 0;
		private const int kLine = -1;
		private const int kMore = -2;

		/// <summary>
		/// Tries to find the tool to jump to, based on the owner of the POS list.
		/// </summary>
		private string JumpToToolNamed => List.OwningFlid == LangProjectTags.kflidPartsOfSpeech ? AreaServices.PosEditMachineName : AreaServices.ReversalToolReversalIndexPOSMachineName;

		/// <summary />
		public POSPopupTreeManager(TreeCombo treeCombo, LcmCache cache, ICmPossibilityList list, int ws, bool useAbbr, IPropertyTable propertyTable, IPublisher publisher, Form parent)
			: base(treeCombo, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
		}

		/// <summary />
		public POSPopupTreeManager(PopupTree popupTree, LcmCache cache, ICmPossibilityList list, int ws, bool useAbbr, IPropertyTable propertyTable, IPublisher publisher, Form parent)
			: base(popupTree, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			var tagName = UseAbbr ? CmPossibilityTags.kflidAbbreviation : CmPossibilityTags.kflidName;
			popupTree.Sorted = true;
			TreeNode match = null;
			if (List != null)
			{
				match = AddNodes(popupTree.Nodes, List.Hvo, CmPossibilityListTags.kflidPossibilities, hvoTarget, tagName);
			}
			popupTree.Sorted = false;
			// Add two special nodes used to:
			//	1. Set the value to 'empty', or
			//	2. Launch the new Grammatical Category Catalog dlg.
			AddTimberLine(popupTree);
			var empty = NotSureIsAny ? AddAnyItem(popupTree) : AddNotSureItem(popupTree);
			if (hvoTarget == 0)
			{
				match = empty;
			}
			AddMoreItem(popupTree);
			return match;
		}

		/// <summary>
		/// Set this (before MakeMenuItems is called) to have an 'Any' menu item instead of 'Not sure'.
		/// </summary>
		public bool NotSureIsAny { get; set; }

		/// <summary>
		/// Add an 'Any' item to the menu. If the current target is zero, it will be selected.
		/// It is saved as m_kEmptyNode. Also returns the new node.
		/// </summary>
		protected TreeNode AddAnyItem(PopupTree popupTree)
		{
			var empty = new HvoTreeNode(TsStringUtils.MakeString(LanguageExplorerControls.ksAny, Cache.WritingSystemFactory.UserWs), kEmpty);
			popupTree.Nodes.Add(empty);
			m_kEmptyNode = empty;
			return empty;
		}

		protected override void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			var selectedNode = e.Node as HvoTreeNode;
			if (selectedNode != null && selectedNode.Hvo == kMore && e.Action == TreeViewAction.ByMouse)
			{
				if (TreeCombo != null)
				{
					TreeCombo.SelectedNode = m_selPrior;
				}
				else
				{
					GetPopupTree().SelectedNode = m_selPrior;
				}
				// If we wait to hide it until after we show the dialog, hiding it activates the disabled main
				// window which owns it, with weird results. Since we're going to launch another window,
				// we don't want to activate the parent at all.
				GetPopupTree().HideForm(false);
				using (var dlg = new MasterCategoryListDlg())
				{
					dlg.SetDlginfo(List, m_propertyTable, false, null);
					switch (dlg.ShowDialog(ParentForm))
					{
						case DialogResult.OK:
							LoadPopupTree(dlg.SelectedPOS.Hvo);
							// everything should be setup with new node selected, but now we need to trigger
							// any side effects, as if we had selected that item by mouse. So go ahead and
							// call the base method to do this. (LT-14062)
							break;
						case DialogResult.Yes:
							{
								// Post a message so that we jump to Grammar(area)/Categories tool.
								// Do this before we close any parent dialog in case
								// the parent wants to check to see if such a Jump is pending (via "AboutToFollowLink").
								// NOTE: We use PostMessage here, rather than SendMessage which
								// disposes of the PopupTree before we and/or our parents might
								// be finished using it (cf. LT-2563).
								LinkHandler.PublishFollowLinkMessage(m_publisher, new FwLinkArgs(JumpToToolNamed, dlg.SelectedPOS.Guid));
								if (ParentForm != null && ParentForm.Modal)
								{
									// Close the dlg that opened the master POS dlg,
									// since its hotlink was used to close it,
									// and a new POS has been created.
									ParentForm.DialogResult = DialogResult.Cancel;
									ParentForm.Close();
								}
								break;
							}
						default:
							{
								return;
							}
					}
				}
			}
			base.m_treeCombo_AfterSelect(sender, e);
		}
	}
}