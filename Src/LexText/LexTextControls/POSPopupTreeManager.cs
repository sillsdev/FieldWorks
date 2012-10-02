using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Handles a TreeCombo control (Widgets assembly) for use with PartOfSpeech objects.
	/// </summary>
	public class POSPopupTreeManager : PopupTreeManager
	{
		private const int kEmpty = 0;
		private const int kLine = -1;
		private const int kMore = -2;

		#region Data members

		private bool m_fNotSureIsAny;
		private Mediator m_mediator;

		#endregion Data members

		#region Events

		#endregion Events

		/// <summary>
		/// Tries to find the tool to jump to, based on the owner of the POS list.
		/// </summary>
		private string JumpToToolNamed
		{
			get
			{
				if (List.OwningFlid == (int)LangProject.LangProjectTags.kflidPartsOfSpeech)
					return "posEdit";
				else
					return "reversalToolReversalIndexPOS";
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public POSPopupTreeManager(TreeCombo treeCombo, FdoCache cache, ICmPossibilityList list, int ws, bool useAbbr, Mediator mediator, Form parent)
			:base (treeCombo, cache, list, ws, useAbbr, parent)
		{
			m_mediator = mediator;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public POSPopupTreeManager(PopupTree popupTree, FdoCache cache, ICmPossibilityList list, int ws, bool useAbbr, Mediator mediator, Form parent)
			: base(popupTree,  cache, list, ws, useAbbr, parent)
		{
			m_mediator = mediator;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				// Do NOT dispose of the mediator, which does not 'belong' to us!
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable & Co. implementation

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			int tagName = UseAbbr ?
				(int)CmPossibility.CmPossibilityTags.kflidAbbreviation :
				(int)CmPossibility.CmPossibilityTags.kflidName;
			popupTree.Sorted = true;
			TreeNode match = AddNodes(popupTree.Nodes, List.Hvo,
				(int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities, hvoTarget, tagName);
			popupTree.Sorted = false;
			// Add two special nodes used to:
			//	1. Set the value to 'empty', or
			//	2. Launch the new Grammatical Category Catalog dlg.
			AddTimberLine(popupTree);
			TreeNode empty = m_fNotSureIsAny ? AddAnyItem(popupTree, hvoTarget) : AddNotSureItem(popupTree, hvoTarget);
			if (hvoTarget == 0)
				match = empty;
			AddMoreItem(popupTree);
			return match;
		}

		/// <summary>
		/// Set this (before MakeMenuItems is called) to have an 'Any' menu item instead of 'Not sure'.
		/// </summary>
		public bool NotSureIsAny
		{
			get
			{
				CheckDisposed();
				return m_fNotSureIsAny;
			}
			set
			{
				CheckDisposed();
				m_fNotSureIsAny = value;
			}
		}

		/// <summary>
		/// Add an 'Any' item to the menu. If the current target is zero, it will be selected.
		/// It is saved as m_kEmptyNode. Also returns the new node.
		/// </summary>
		/// <param name="popupTree"></param>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		protected TreeNode AddAnyItem(PopupTree popupTree, int hvoTarget)
		{
			HvoTreeNode empty = new HvoTreeNode(Cache.MakeUserTss(LexTextControls.ksAny), kEmpty);
			popupTree.Nodes.Add(empty);
			m_kEmptyNode = empty;
			return empty;
		}

		protected override void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			HvoTreeNode selectedNode = e.Node as HvoTreeNode;

			if (selectedNode != null && selectedNode.Hvo == kMore && e.Action == TreeViewAction.ByMouse)
			{
				// Only launch the dialog by a mouse click (or simulated mouse click).
				PopupTree pt = GetPopupTree();
				// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
				// This will effectively revert the list selection to a previous confirmed state.
				// Whatever happens below, we don't want to actually leave the "More..." node selected!
				// This is at least required if the user selects "Cancel" from the dialog below.
				pt.Hide();
				using (MasterCategoryListDlg dlg = new MasterCategoryListDlg())
				{
					dlg.SetDlginfo(List, m_mediator, false, null);
					switch (dlg.ShowDialog(ParentForm))
					{
						case DialogResult.OK:
						{
							LoadPopupTree(dlg.SelectedPOS.Hvo);
							// everything should be setup with new node selected, so return.
							return;
						}
						case DialogResult.Yes:
						{
							// Post a message so that we jump to Grammar(area)/Categories tool.
							// Do this before we close any parent dialog in case
							// the parent wants to check to see if such a Jump is pending.
							// NOTE: We use PostMessage here, rather than SendMessage which
							// disposes of the PopupTree before we and/or our parents might
							// be finished using it (cf. LT-2563).
							m_mediator.PostMessage("FollowLink",
								SIL.FieldWorks.FdoUi.FwLink.Create(JumpToToolNamed,
								Cache.GetGuidFromId(dlg.SelectedPOS.Hvo),
								Cache.ServerName,
								Cache.DatabaseName));
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
							// NOTE: If the user has selected "Cancel", then don't change
							// our m_lastConfirmedNode to the "More..." node. Keep it
							// the value set by popupTree_PopupTreeClosed() when we
							// called pt.Hide() above. (cf. comments in LT-2522)
							break;
					}
				}
			}

			base.m_treeCombo_AfterSelect(sender, e);
		}
	}
}
