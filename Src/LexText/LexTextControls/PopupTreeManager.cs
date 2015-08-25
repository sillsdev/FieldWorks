using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Handles a TreeCombo control (Widgets assembly). Subclass must at least implement
	/// MakeMenuItems.
	/// </summary>
	public abstract class PopupTreeManager : IFWDisposable
	{
		private const int kEmpty = 0;
		private const int kLine = -1;
		private const int kMore = -2;

		#region Data members

		private bool m_isTreeLoaded = false;
		private TreeCombo m_treeCombo;
		private PopupTree m_popupTree;
		private FdoCache m_cache;
		private bool m_useAbbr;
		private Form m_parent;
		protected IPropertyTable m_propertyTable;
		protected IPublisher m_publisher;
		// It's conceivable to have a subclass that isn't based on a possibility list,
		// but we haven't needed it yet, and it would be easy to just pass null if
		// we need to make one. So it's handy to have it in the common code.
		private ICmPossibilityList m_list;
		private HvoTreeNode m_lastConfirmedNode = null;
		/// <summary>
		///  "<Not Sure>" node, or sometimes 'Any' node.
		/// </summary>
		protected HvoTreeNode m_kEmptyNode = null;
		private int m_ws;

		#endregion Data members

		#region Events

		public event TreeViewEventHandler AfterSelect;
		public event TreeViewCancelEventHandler BeforeSelect;

		#endregion Events

		/// <summary>
		/// Constructor.
		/// </summary>
		public PopupTreeManager(TreeCombo treeCombo, FdoCache cache, IPropertyTable propertyTable, IPublisher publisher, ICmPossibilityList list, int ws, bool useAbbr, Form parent)
		{
			m_treeCombo = treeCombo;
			Init(cache, propertyTable, publisher, list, ws, useAbbr, parent);
			m_treeCombo.BeforeSelect += m_treeCombo_BeforeSelect;
			m_treeCombo.AfterSelect += m_treeCombo_AfterSelect;
			m_treeCombo.Tree.PopupTreeClosed += popupTree_PopupTreeClosed;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public PopupTreeManager(PopupTree popupTree, FdoCache cache, IPropertyTable propertyTable, IPublisher publisher, ICmPossibilityList list, int ws, bool useAbbr, Form parent)
		{
			m_popupTree = popupTree;
			Init(cache, propertyTable, publisher, list, ws, useAbbr, parent);
			popupTree.BeforeSelect += m_treeCombo_BeforeSelect;
			popupTree.AfterSelect += m_treeCombo_AfterSelect;
			popupTree.PopupTreeClosed += popupTree_PopupTreeClosed;
		}

		private void Init(FdoCache cache, IPropertyTable propertyTable, IPublisher publisher, ICmPossibilityList list, int ws, bool useAbbr, Form parent)
		{
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			if (parent == null)
			{
				if (m_propertyTable != null)
				{
					IApp app = m_propertyTable.GetValue<IApp>("App");
					if (app != null)
					{
						parent = app.ActiveMainWindow;
					}
					if (parent == null)
					{
						parent = m_propertyTable.GetValue<Form>("window");
					}
				}
				if (parent == null)
					parent = Form.ActiveForm; // desperate for something...
			}
			m_cache = cache;
			m_useAbbr = useAbbr;
			m_parent = parent;
			m_list = list;
			m_ws = ws;

		}

		/// <summary>
		/// Set the label on the empty node.
		/// </summary>
		/// <param name="label"></param>
		public void SetEmptyLabel(string label)
		{
			CheckDisposed();

			if (m_kEmptyNode != null)
				m_kEmptyNode.Tss = Cache.TsStrFactory.MakeString(label, Cache.WritingSystemFactory.UserWs);
			if (m_treeCombo != null && m_treeCombo.SelectedNode == m_kEmptyNode)
				m_treeCombo.Tss = m_kEmptyNode.Tss;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		protected bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~PopupTreeManager()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_treeCombo != null && !m_treeCombo.IsDisposed)
				{
					m_treeCombo.BeforeSelect -= m_treeCombo_BeforeSelect;
					m_treeCombo.AfterSelect -= m_treeCombo_AfterSelect;
					if (m_treeCombo.Tree != null)
						m_treeCombo.Tree.PopupTreeClosed -= new TreeViewEventHandler(popupTree_PopupTreeClosed);
					// We only manage m_treeCombo, so it gets disposed elsewhere.
				}
				if (m_popupTree != null)
				{
					m_popupTree.BeforeSelect -= m_treeCombo_BeforeSelect;
					m_popupTree.AfterSelect -= m_treeCombo_AfterSelect;
					m_popupTree.PopupTreeClosed -= popupTree_PopupTreeClosed;
					// We only manage m_popupTree, so it gets disposed elsewhere.
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_lastConfirmedNode = null;
			m_kEmptyNode = null;
			m_treeCombo = null;
			m_popupTree = null;
			m_parent = null;
			m_cache = null;
			m_list = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Get the popup tree we are managing. This may be a stand-alone popup tree,
		/// or one that is part of the implementation of a tree combo box.
		/// </summary>
		protected PopupTree GetPopupTree()
		{
			if (m_popupTree != null)
				return m_popupTree;
			else if (m_treeCombo != null)	// See LT-9043.  This is the only conceivable fix.
				return m_treeCombo.Tree;
			else
				return null;
		}

		/// <summary>
		/// Provide subclasses access to the tree combo in case they need it.
		/// </summary>
		protected TreeCombo TreeCombo
		{
			get { return m_treeCombo; }
		}

		/// <summary>
		/// Get value to determine if the tree has been loaded via the LoadPopupTree method.
		/// </summary>
		public bool IsTreeLoaded
		{
			get { return m_isTreeLoaded; }
		}

		/// <summary>
		/// The possibility list we are (typically) choosing from
		/// </summary>
		protected ICmPossibilityList List
		{
			get { return m_list; }
		}

		/// <summary>
		/// True to display abbreviations, false to display names.
		/// </summary>
		protected bool UseAbbr
		{
			get { return m_useAbbr; }
		}

		/// <summary>
		/// The data cache relevant for this list etc.
		/// </summary>
		protected FdoCache Cache
		{
			get { return m_cache; }
		}

		/// <summary>
		/// The writing system in which to display item names.
		/// </summary>
		protected int WritingSystem
		{
			get { return m_ws; }
		}

		/// <summary>
		/// Typically the parent indicated to the constructor; see Init() for other possibilities.
		/// </summary>
		protected Form ParentForm
		{
			get { return m_parent; }
		}

		/// <summary>
		/// Get a string made up og hyphens to separate the POSes from the two final lines,
		/// which are not POSes.
		/// </summary>
		private string TimberLine
		{
			get
			{
				string line = "-";
				if (GetPopupTree() != null)
				{
					Font fnt = GetPopupTree().Font;
					using (Graphics g = GetPopupTree().CreateGraphics())
					{
						int width = GetPopupTree().Width - 18;
						SizeF sz = g.MeasureString(line + "-", fnt);
						while (sz.Width < width)
						{
							line += "-";
							sz = g.MeasureString(line + "-", fnt);
						}
					}
					fnt = null; // Don't destroy it, since it isn't ours.
				}
				return line;
			}
		}

		/// <summary>
		/// We need to prevent recursive calls to LoadPopupTree() during its execution.
		/// If one occurs, we delay it until the end of the function.  (See LT-7574.)
		/// </summary>
		bool m_fLoadingPopupTree = false;
		bool m_fNeedReload = false;
		int m_hvoPendingTarget = -1;

		/// <summary>
		/// Load the PopupTree with HvoTreeNode objects, which represent all POSes in the list.
		/// Reloads the PopupTree even if it has been loaded before, since the list may now have new ones in it.
		/// </summary>
		/// <param name="hvoSpecifiedTarget"></param>
		public void LoadPopupTree(int hvoSpecifiedTarget)
		{
			CheckDisposed();

			if (m_fLoadingPopupTree)
			{
				if (m_hvoPendingTarget != hvoSpecifiedTarget)
				{
					m_fNeedReload = true;
					m_hvoPendingTarget = hvoSpecifiedTarget;
				}
				return;
			}
			try
			{
				m_fLoadingPopupTree = true;
				m_isTreeLoaded = false;		// can't be loaded if it's loading! See LT-9191.
				PopupTree popupTree = GetPopupTree();
				if (popupTree != null)
				{
					// If no new target is given, but we already have one selected, try and reuse it.
					int hvoTarget = hvoSpecifiedTarget;
					if (hvoSpecifiedTarget == 0 && popupTree.SelectedNode != null && popupTree.SelectedNode is HvoTreeNode)
						hvoTarget = (popupTree.SelectedNode as HvoTreeNode).Hvo;
					popupTree.BeginUpdate();

					// On Mono Clear() generates AfterSelect events. So disable the handlers
					// to ensure the same behaviour as .NET.
					popupTree.EnableAfterAndBeforeSelectHandling(false);
					popupTree.Nodes.Clear();
					popupTree.EnableAfterAndBeforeSelectHandling(true);

					TreeNode match = MakeMenuItems(popupTree, hvoTarget);

					SelectChosenItem(match, popupTree);
					popupTree.EndUpdate();
					m_isTreeLoaded = true;
				}
			}
			finally
			{
				m_fLoadingPopupTree = false;
			}
			if (m_fNeedReload)
			{
				// Okay, we need a reload, so do it.  But not if it's a recursion too far...
				if (m_hvoPendingTarget != hvoSpecifiedTarget)
					LoadPopupTree(m_hvoPendingTarget);
				m_fNeedReload = false;
				m_hvoPendingTarget = -1;
			}
		}

		/// <summary>
		/// Make all the menu items. May well call things like AddTimberline, AddNotSureItem, AddMoreItem.
		/// </summary>
		/// <param name="popupTree"></param>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		protected abstract TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget);

		/// <summary>
		///
		/// </summary>
		/// <param name="popupTree"></param>
		/// <param name="hvoTarget"></param>
		/// <returns>the node matching hvoTarget, if found</returns>
		protected TreeNode AddPossibilityListItems(PopupTree popupTree, int hvoTarget)
		{
			int tagName = UseAbbr ?
						 CmPossibilityTags.kflidAbbreviation :
						 CmPossibilityTags.kflidName;
			TreeNode match;
			try
			{
				// detect whether this possibility list is (to be) sorted.
				popupTree.Sorted = List.IsSorted;
				match = AddNodes(popupTree.Nodes, List.Hvo,
					CmPossibilityListTags.kflidPossibilities, hvoTarget, tagName);
			}
			finally
			{
				// now allow subsequent items to get added, but not necessarily sorted.
				popupTree.Sorted = false;
			}
			return match;
		}

		/// <summary>
		/// override to add additional nodes to popupTree (e.g. AddNotSureItem, AddTimberline)
		/// </summary>
		protected virtual TreeNode AppendAdditionalItems(PopupTree popupTree, int hvoTarget)
		{
			return null; // subclasses override.
		}

		/// <summary>
		/// Add to the nodes parameter an HvoTreeNode for each hvo in property flid of object hvo.
		/// Give it to children (by calling recursively) for each of its subpossibilities.
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flid"></param>
		/// <param name="hvoTarget"></param>
		/// <param name="tagName"></param>
		/// <returns>If one of the nodes has an Hvo matching hvoTarget, return it, otherwise null.</returns>
		protected TreeNode AddNodes(TreeNodeCollection nodes, int hvoOwner,
			int flid, int hvoTarget, int tagName)
		{
			return AddNodes(nodes, hvoOwner,
				flid, CmPossibilityTags.kflidSubPossibilities, hvoTarget, tagName);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flid"></param>
		/// <param name="flidSub">specify a subitems,
		/// other than CmPossibilityTags.kflidSubPossibilities</param>
		/// <param name="hvoTarget"></param>
		/// <param name="tagName"></param>
		/// <returns></returns>
		protected TreeNode AddNodes(TreeNodeCollection nodes, int hvoOwner,
			int flid, int flidSub, int hvoTarget, int tagName)
		{
			TreeNode result = null;
			int chvo = Cache.MainCacheAccessor.get_VecSize(hvoOwner, flid);
			for (int i = 0; i < chvo; i++)
			{
				int hvoChild = Cache.MainCacheAccessor.get_VecItem(hvoOwner, flid, i);
				ITsString tssLabel = WritingSystemServices.GetMagicStringAlt(Cache,
					WritingSystemServices.kwsFirstAnalOrVern, hvoChild, tagName);
				if (tssLabel == null)
					tssLabel = TsStringUtils.MakeTss(LexTextControls.ksStars, Cache.WritingSystemFactory.UserWs);
				HvoTreeNode node = new HvoTreeNode(tssLabel, hvoChild);
				nodes.Add(node);
				TreeNode temp = AddNodes(node.Nodes, hvoChild,
					flidSub, flidSub, hvoTarget, tagName);
				if (hvoChild == hvoTarget)
					result = node;
				else if (temp != null)
					result = temp;
			}
			return result;
		}

		protected static ITsString GetTssLabel(FdoCache cache, int hvoItem, int flidName, int wsName)
		{
			var multiProp = (IMultiStringAccessor)cache.DomainDataByFlid.get_MultiStringProp(hvoItem, flidName);
			int wsActual;
			ITsString tssLabel = multiProp.GetAlternativeOrBestTss(wsName, out wsActual);
			return tssLabel;
		}

		/// <summary>
		/// Select the specified menu item in the tree.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="popupTree"></param>
		protected void SelectChosenItem(TreeNode item, PopupTree popupTree)
		{
			CheckDisposed();

			if (item != null)
			{
				// We do NOT want to simulate a mouse click because that will cause the
				// text box in the combo to be focused. We may be updating this from a PropChanged
				// that should not set focus.
				popupTree.SelectByAction = TreeViewAction.Unknown;
				popupTree.SelectedNode = item;
				if (m_treeCombo != null)
					m_treeCombo.SetComboText(item);
			}
		}

		/// <summary>
		/// Add a 'More...' item to the tree. Subclass is responsible to implement.
		/// </summary>
		/// <param name="popupTree"></param>
		protected void AddMoreItem(PopupTree popupTree)
		{
			popupTree.Nodes.Add(new HvoTreeNode(Cache.TsStrFactory.MakeString(LexTextControls.ksMore_, Cache.WritingSystemFactory.UserWs), kMore));
		}

		/// <summary>
		/// Add a 'not sure' item to the menu. If the current target is zero, it will be selected.
		/// It is saved as m_kEmptyNode. Also returns the new node.
		/// </summary>
		/// <param name="popupTree"></param>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		protected TreeNode AddNotSureItem(PopupTree popupTree)
		{
			HvoTreeNode empty = new HvoTreeNode(Cache.TsStrFactory.MakeString(LexTextControls.ks_NotSure_, Cache.WritingSystemFactory.UserWs), kEmpty);
			popupTree.Nodes.Add(empty);
			m_kEmptyNode = empty;
			return empty;
		}

		/// <summary>
		/// Add the --- line to the popup (use if adding any 'extra' items).
		/// </summary>
		/// <param name="popupTree"></param>
		protected void AddTimberLine(PopupTree popupTree)
		{
			popupTree.Nodes.Add(new HvoTreeNode(Cache.TsStrFactory.MakeString(TimberLine, Cache.WritingSystemFactory.UserWs), kLine));
		}

		/// <summary>
		/// Make sure the combo text has correct value
		/// </summary>
		protected void SetComboTextToLastConfirmedSelection()
		{
			if (m_lastConfirmedNode != null)
				m_treeCombo.Tss = m_lastConfirmedNode.Tss;
		}

		/// <summary>
		/// Sometimes we need to revert to the previous selection, so save it just in case.
		/// (See FWR-3082.)
		/// </summary>
		protected TreeNode m_selPrior;

		private void m_treeCombo_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			HvoTreeNode newSelNode = (HvoTreeNode)e.Node;
			PopupTree pt = GetPopupTree();
			if (pt == null)
				return;		// can't do anything without the tree!  See LT-9031.

			m_selPrior = (m_treeCombo == null) ? pt.SelectedNode : m_treeCombo.SelectedNode;
			switch (newSelNode.Hvo)
			{
				case kLine:
					e.Cancel = true;
					// It may be selected by keyboard action, which needs more care.
					if (e.Action == TreeViewAction.ByKeyboard)
					{
						// Select the next/previous one, rather than the line.
						HvoTreeNode oldSelNode = pt.SelectedNode as HvoTreeNode;
						if (oldSelNode != null)
						{
							if (oldSelNode == newSelNode.NextNode && newSelNode.PrevNode != null)
							//if (oldSelNode.Hvo == kEmpty && newSelNode.PrevNode != null)
							{
								// Up Arrow probably used.
								// Select node above the line in the tree view.
								pt.SelectedNode = newSelNode.PrevNode;
							}
							else if (newSelNode.NextNode != null)
							{
								// Down Arrow probably used.
								// Select <empty> POS.
								pt.SelectedNode = newSelNode.NextNode;
							}
							else
							{
								Debug.Assert(false); // multiple adjacent empty (separator line) nodes, or nothing else??
							}
						}
					}
					break;
					/* Nothing special to do, since it is handled as a normal POS.
					case kEmpty:
						PopupTree.OldSelectedNode = PopupTree.SelectedNode;
						//m_treeCombo.Tree.SelectedNode = null;
						break;
					*/
			}

			if (BeforeSelect != null)
				BeforeSelect(this, e);
		}

		/// <summary>
		/// Override this to handle any special items like 'More...'.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			HvoTreeNode selectedNode = e.Node as HvoTreeNode;
			if (selectedNode == null)
				return; // User moved highlight off any item and hit enter; ignore.
			PopupTree pt = GetPopupTree();

			if (e.Action == TreeViewAction.ByMouse)
			{
				// bookmark this confirmed selection - required before PopupTree.Hide()
				// (cf. comments in LT-2522)
				if (pt != null)
					m_lastConfirmedNode = (HvoTreeNode)pt.SelectedNode;
			}

			// Pass on the event to owners/clients and close the PopupTree, only if it has been confirmed
			// "ByMouse" (or simulated mouse event). This will ensure that the user can use the keyboard
			// to move to items in the list, without actually affecting their previous selection until they
			// CONFIRM it "ByMouse" which can happen by clicking or by pressing ENTER. This allows the user
			// to ESCape there movement and revert to previously confirmed selection. (cf. comments in LT-2522)
			if (e.Action == TreeViewAction.ByMouse)
			{
				// If we are managing a treeCombo, then go ahead and set its Text in case the owner
				// does not want to do so in the AfterSelect below.
				if (m_treeCombo != null)
				{
					if (m_treeCombo.Text != m_lastConfirmedNode.Text)
						m_treeCombo.Tss = m_lastConfirmedNode.Tss;
				}
				if (AfterSelect != null)
					AfterSelect(this, new TreeViewEventArgs(m_lastConfirmedNode, e.Action));
				// Close the PopupTree for confirmed selections.
				if (GetPopupTree() != null)
					GetPopupTree().Hide();	// This should trigger popupTree_PopupTreeClosed() below.
			}
		}

		// The basic purpose of PopupTreeClosed event is to allow us to reset the list selection
		// to the last confirmed node, whenever the PopupTree becomes hidden (especially for ESCape or other
		// non-selecting hides).
		// Otherwise, the next time you open the dropdown list, it will be highlighting the
		// previously unconfirmed item. This is unexpected behavior especially for comboboxes where the
		// user would always expect to see the item that is in the textbox initially highlighted,
		// not something they may have "ESCaped" from. (cf. comments in LT-2522).
		//
		// So, when our PopupTree closes, reset the list selection to our last confirmed selection.
		// If it has already been set, then do nothing.
		private void popupTree_PopupTreeClosed(object sender, TreeViewEventArgs e)
		{
			if(GetPopupTree() != null && GetPopupTree().SelectedNode != m_lastConfirmedNode)
			{
				if (m_lastConfirmedNode != null)
					GetPopupTree().SelectedNode = (TreeNode)m_lastConfirmedNode;
				else if (m_kEmptyNode != null)
					GetPopupTree().SelectedNode = (TreeNode)m_kEmptyNode;
			}
		}
	}


	/// <summary>
	/// This class implements PopupTreeManager for a possibility list.
	/// </summary>
	public class PossibilityListPopupTreeManager : PopupTreeManager
	{
		public PossibilityListPopupTreeManager(TreeCombo treeCombo, FdoCache cache,
			IPropertyTable propertyTable, IPublisher publisher, ICmPossibilityList list, int ws, bool useAbbr, Form parent)
			: base(treeCombo, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			TreeNode match1 = AddPossibilityListItems(popupTree, hvoTarget);
			TreeNode match2 = AppendAdditionalItems(popupTree, hvoTarget);
			return match1 ?? match2;
		}
	}
}
