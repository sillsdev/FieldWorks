// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using LanguageExplorer.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	internal abstract class TreeBarHandler : ITreeBarHandler
	{
		protected IPropertyTable m_propertyTable;
		protected LcmCache m_cache;
		protected bool m_expand;
		protected bool m_hierarchical;
		protected bool m_includeAbbr;
		protected string m_bestWS;

		// This gets set when we skipped populating the tree bar because it wasn't visible.
		protected bool m_fOutOfDate;
		protected Dictionary<int, TreeNode> m_hvoToTreeNodeTable = new Dictionary<int, TreeNode>();
		private TreeNode m_dragHiliteNode; // node that currently has background set to show drag destination
		private TreeNode m_clickNode; // node the user mouse-downed on
		protected ICmObjectRepository m_objRepo;
		protected ICmPossibilityRepository m_possRepo;
		TreeView m_tree;
		int m_typeSize;	// font size for the tree's fonts.
						// map from writing system to font.
		readonly Dictionary<int, Font> m_wsToFontTable = new Dictionary<int, Font>();

		/// <summary />
		protected TreeBarHandler(IPropertyTable propertyTable, bool expand, bool hierarchical, bool includeAbbr, string bestWS)
		{
			Guard.AgainstNull(propertyTable, nameof(propertyTable));

			m_propertyTable = propertyTable;
			m_cache = m_propertyTable.GetValue<LcmCache>("cache");
			m_expand = expand;
			m_hierarchical = hierarchical;
			m_includeAbbr = includeAbbr;
			m_bestWS = bestWS;
			m_objRepo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			m_possRepo = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
		}

		/// <summary>
		/// Get the HVO of the item the user clicked on (or zero if nothing has been clicked).
		/// </summary>
		protected int ClickObject
		{
			get
			{
				if (m_clickNode == null)
				{
					return 0;
				}
				return (int)m_clickNode.Tag;
			}
		}

		#region IRecordBarHandler implementation

		/// <summary>
		/// Check whether the given hvo is represented by a TreeNode.
		/// </summary>
		public bool IsItemInTree(int hvo)
		{
			return m_hvoToTreeNodeTable.ContainsKey(hvo);
		}

		public void PopulateRecordBarIfNeeded(IRecordList list)
		{
			if (m_fOutOfDate)
			{
				PopulateRecordBar(list);
			}
		}

		public virtual void PopulateRecordBar(IRecordList list)
		{
			PopulateRecordBar(list, true);
		}

		public virtual void UpdateSelection(ICmObject currentObject)
		{
			var window = m_propertyTable.GetValue<IFwMainWnd>("window");
			var tree = window.TreeStyleRecordList;
			if (currentObject == null)
			{
				if (tree != null)
				{
					tree.SelectedNode = null;
				}
				m_clickNode = null; // otherwise we can try to promote a deleted one etc.
				return;
			}

			TreeNode node = null;
			if (m_hvoToTreeNodeTable.ContainsKey(currentObject.Hvo))
			{
				node = m_hvoToTreeNodeTable[currentObject.Hvo];
			}
			//Debug.Assert(node != null);
			// node.EnsureVisible() throws an exception if tree != node.TreeView, and this can
			// happen somehow.  (see LT-986)
			if (node != null && tree != null && node.TreeView == tree && (tree.SelectedNode != node))
			{
				tree.SelectedNode = node;
				EnsureSelectedNodeVisible(tree);
			}
		}

		public virtual void ReloadItem(ICmObject currentObject)
		{
			if (currentObject == null || m_hvoToTreeNodeTable.Count == 0)
			{
				return;
			}
			m_fOutOfDate = false;

			var node = m_hvoToTreeNodeTable[currentObject.Hvo];
			if (node == null)
			{
				return;
			}
			Font font;
			var text = GetTreeNodeLabel(currentObject, out font);
			// ReSharper disable RedundantCheckBeforeAssignment
			if (text != node.Text)
			{
				node.Text = text;
			}
			if (font != node.NodeFont)
			{
				node.NodeFont = font;
			}
			// ReSharper restore RedundantCheckBeforeAssignment
		}

		public virtual void ReleaseRecordBar()
		{
			if (m_tree == null)
			{
				return;
			}
			m_tree.NodeMouseClick -= tree_NodeMouseClick;
			m_tree.MouseDown -= tree_MouseDown;
			m_tree.MouseMove -= tree_MouseMove;
			m_tree.DragDrop -= tree_DragDrop;
			m_tree.DragOver -= tree_DragOver;
			m_tree.GiveFeedback -= tree_GiveFeedback;
	}

		#endregion IRecordBarHandler implementation

		#region IDisposable implementation

		protected bool _isDisposed;

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~TreeBarHandler()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary />
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			// No need to run it more than once.
			if (_isDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				m_hvoToTreeNodeTable?.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_hvoToTreeNodeTable = null;
			m_cache = null;

			_isDisposed = true;
		}

		#endregion IDisposable implementation

		/// <summary>
		/// Makes the record list available to subclasses.
		/// </summary>
		protected IRecordList MyRecordList { get; private set; }

		protected virtual void PopulateRecordBar(IRecordList recordList, bool editable)
		{
			if (MyRecordList == recordList)
			{
				return; // Been here. Done that.
			}
			m_fOutOfDate = false;

			MyRecordList = recordList;

			var window = m_propertyTable.GetValue<IFwMainWnd>("window");
			var recordBarControl = window.RecordBarControl;
			if (recordBarControl == null)
			{
				return;
			}
			using (new WaitCursor((Form)window))
			{
				var tree = window.TreeStyleRecordList;
				var expandedItems = new HashSet<int>();
				if (m_tree != null && !m_expand)
				{
					GetExpandedItems(m_tree.Nodes, expandedItems);
				}
				m_tree = tree;

				// Removing the handlers first seems to be necessary because multiple tree handlers are
				// working with one treeview. Only this active one should have handlers connected.
				// If we fail to do this, switching to a different list causes drag and drop to stop working.
				ReleaseRecordBar();

				tree.NodeMouseClick += tree_NodeMouseClick;
				if (editable)
				{
					tree.MouseDown += tree_MouseDown;
					tree.MouseMove += tree_MouseMove;
					tree.DragDrop += tree_DragDrop;
					tree.DragOver += tree_DragOver;
					tree.GiveFeedback += tree_GiveFeedback; // REVIEW (Hasso) 2015.02: this handler currently does nothing.  Needed?
					tree.ContextMenuStrip = CreateTreebarContextMenuStrip();
					tree.ContextMenuStrip.MouseClick += tree_MouseClicked;
				}
				else
				{
					tree.ContextMenuStrip = new ContextMenuStrip();
				}
				tree.AllowDrop = editable;
				tree.BeginUpdate();
				recordBarControl.Clear();
				m_hvoToTreeNodeTable.Clear();

				// type size must be set before AddTreeNodes is called
				m_typeSize = recordList.TypeSize;
				AddTreeNodes(recordList.SortedObjects, tree);

				tree.Font = new Font(recordList.FontName, m_typeSize);
				tree.ShowRootLines = m_hierarchical;

				if (m_expand)
				{
					tree.ExpandAll();
				}
				else
				{
					tree.CollapseAll();
					ExpandItems(tree.Nodes, expandedItems);
				}

				// Set the selection after expanding/collapsing the tree.  This allows the true
				// selection to be visible even when the tree is collapsed but the selection is
				// an internal node.  (See LT-4508.)
				UpdateSelection(recordList.CurrentObject);
				tree.EndUpdate();
			}
		}

		/// <summary>
		/// For all the nodes that are expanded, if their tag is an int, add it to the set.
		/// </summary>
		private static void GetExpandedItems(TreeNodeCollection treeNodeCollection, HashSet<int> expandedItems)
		{
			foreach (TreeNode node in treeNodeCollection)
			{
				if (!node.IsExpanded)
				{
					continue;
				}
				if (node.Tag is int)
				{
					expandedItems.Add((int)node.Tag);
				}
				GetExpandedItems(node.Nodes, expandedItems);
			}
		}

		/// <summary>
		/// If any of the nodes in treeNodeCollection has a tag that is an int that is in the set,
		/// expand it, and recursively check its children.
		/// </summary>
		private static void ExpandItems(TreeNodeCollection treeNodeCollection, HashSet<int> expandedItems)
		{
			foreach (TreeNode node in treeNodeCollection)
			{
				if (!(node.Tag is int) || !expandedItems.Contains((int)node.Tag))
				{
					continue;
				}
				node.Expand();
				ExpandItems(node.Nodes, expandedItems);
			}
		}

		protected virtual ContextMenuStrip CreateTreebarContextMenuStrip()
		{
			var promoteMenuItem = new DisposableToolStripMenuItem(AreaResources.Promote);
			var contStrip = new ContextMenuStrip();
			contStrip.Items.Add(promoteMenuItem);
			return contStrip;
		}

		private void tree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			m_clickNode = e.Node;
		}

		void tree_GiveFeedback(object sender, GiveFeedbackEventArgs e)
		{
		}

		private void ClearDragHilite()
		{
			if (m_dragHiliteNode != null)
			{
				m_dragHiliteNode.BackColor = Color.FromKnownColor(KnownColor.Window);
			}
		}

		private void tree_Promote()
		{
			if (m_clickNode == null) // LT-5652: don't promote anything
			{
				return;
			}

			var source = m_clickNode;
			// destination for promote is two levels up, or null to move all the way to the top.
			var destNode = source.Parent;
			destNode = destNode?.Parent;
			MoveItem(m_tree, destNode, source);
		}

		private void tree_MouseClicked(object sender, MouseEventArgs e)
		{
			// LT-5664  This event handler was set up to ensure the user does not
			// accidentally select the Promote command with a right mouse click.
			if (e.Button != MouseButtons.Left)
			{
				return;
			}
			var item = m_tree.ContextMenuStrip.GetItemAt(e.X, e.Y);
			if (item == null)
			{
				return;
			}

			var itemSelected = item.Text;
			if (itemSelected.Equals(AreaResources.Promote))
			{
				tree_Promote();
			}
			else if (itemSelected.Equals(LanguageExplorerResources.MoveDown))
			{
				tree_moveDown();
			}
			else if (itemSelected.Equals(LanguageExplorerResources.MoveUp))
			{
				tree_moveUp();
			}
		}

		/// <summary>
		/// let subclass handle this
		/// </summary>
		protected virtual void tree_moveUp()
		{
		}

		/// <summary>
		/// let subclass handle this
		/// </summary>
		protected virtual void tree_moveDown()
		{
		}

		private void tree_DragOver(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(typeof(LocalDragItem)))
			{
				e.Effect = DragDropEffects.None; // not my sort of data at all, can't drop
				return;
			}
			// An inactive handler is unfortunately also being notified. Don't express any
			// opinion at all about drag effects.
			var item = (LocalDragItem)e.Data.GetData(typeof(LocalDragItem));
			if (item.TreeBarHandler != this)
			{
				return;
			}

			TreeNode destNode;
			e.Effect = OkToDrop(sender, e, out destNode) ? DragDropEffects.Move : DragDropEffects.None;
			if (destNode == m_dragHiliteNode)
			{
				return;
			}
			ClearDragHilite();
			m_dragHiliteNode = destNode;
			if (m_dragHiliteNode != null)
			{
				m_dragHiliteNode.BackColor = Color.Gray;
			}
		}

		private void tree_MouseMove(object sender, MouseEventArgs e)
		{
			if ((e.Button & MouseButtons.Left) != MouseButtons.Left)
			{
				return;
			}
			var tree = sender as TreeView;
			// The location here is always different than the one in tree_MouseDown.
			// Sometimes, the difference is great enough to choose an adjacent item!
			// See LT-10295.  So we'll use the location stored in tree_MouseDown.
			// (The sample code in MSDN uses the item/location information in MouseDown
			// establish the item to drag in MouseMove.)
			var selItem = tree?.GetNodeAt(m_mouseDownLocation);
			if (selItem == null)
			{
				return;
			}
			var item = new LocalDragItem(this, selItem);
			tree.DoDragDrop(item, DragDropEffects.Move);
			ClearDragHilite();
		}

		/// <summary>
		/// Currently we only know how to move our own items.
		/// </summary>
		private void tree_DragDrop(object sender, DragEventArgs e)
		{
			TreeNode destNode;
			if (!OkToDrop(sender, e, out destNode))
			{
				return;
			}
			// Notification also gets sent to inactive handlers, which should ignore it.
			var item = (LocalDragItem)e.Data.GetData(typeof(LocalDragItem));
			if (item.TreeBarHandler != this)
			{
				return;
			}

			if (e.Effect != DragDropEffects.Move)
			{
				return;
			}
			MoveItem(sender, destNode, item.SourceNode);
		}

		private void MoveItem(object sender, TreeNode destNode, TreeNode sourceItem)
		{
			var hvoMove = (int)sourceItem.Tag;
			var hvoDest = 0;
			int flidDest;
			var cache = m_propertyTable.GetValue<LcmCache>("cache");
			var move = cache.ServiceLocator.GetObject(hvoMove);
			var moveLabel = sourceItem.Text;
			TreeNodeCollection newSiblings;
			var tree = (TreeView)sender;
			if (destNode == null)
			{
				ICmObject dest;
				for (dest = move.Owner; dest != null; dest = dest.Owner)
				{
					if (!(dest is ICmPossibilityList))
					{
						continue;
					}
					hvoDest = dest.Hvo;
					break;
				}

				if (dest == null)
				{
					return;
				}
				flidDest = CmPossibilityListTags.kflidPossibilities;
				newSiblings = tree.Nodes;
			}
			else
			{
				hvoDest = (int)destNode.Tag;
				flidDest = CmPossibilityTags.kflidSubPossibilities;
				newSiblings = destNode.Nodes;
			}

			if (CheckAndReportForbiddenMove(hvoMove, hvoDest))
			{
				return;
			}

			var hvoOldOwner = move.Owner.Hvo;
			if (hvoOldOwner == hvoDest)
			{
				return; // nothing to do.
			}

			var flidSrc = move.OwningFlid;
			var srcIndex = cache.DomainDataByFlid.GetObjIndex(hvoOldOwner, flidSrc, hvoMove);
			var ihvoDest = 0;
			for (; ihvoDest < newSiblings.Count; ihvoDest++)
			{
				if (newSiblings[ihvoDest].Text.CompareTo(moveLabel) > 0) // Enhance JohnT: use ICU comparison...
				{
					break;
				}
			}
			using (new WaitCursor(tree.TopLevelControl))
			using (new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = MyRecordList }))
			{
				UndoableUnitOfWorkHelper.Do(AreaResources.UndoMoveItem, AreaResources.RedoMoveItem, cache.ActionHandlerAccessor, () =>
						cache.DomainDataByFlid.MoveOwnSeq(hvoOldOwner, flidSrc, srcIndex, srcIndex, hvoDest, flidDest, ihvoDest));
			}
		}

		/// <summary>
		/// Check for and deal with moves that should not be allowed.
		/// Previously the only disallowed move was one that reorganized a chart template that is in use.
		/// This is not the optimum place to 'know' about this, but I (JohnT) am not sure where is.
		/// Now we are disallowing certain moves within the TextMarkup Tags list too, so I (GordonM) have
		/// refactored a bit.
		/// Review: Should these be pulled out to the PossibilityTreeBarHandler subclass?
		/// </summary>
		private bool CheckAndReportForbiddenMove(int hvoMove, int hvoDest)
		{
			var movingPossItem = m_possRepo.GetObject(hvoMove);
			if (movingPossItem == null)
			{
				return false; // not detecting problems with moving other kinds of things.
			}
			var rootPoss = movingPossItem.MainPossibility;
			var hvoRootItem = rootPoss.Hvo;
			var hvoPossList = rootPoss.OwningList.Hvo;

			// If we get here hvoPossList is a possibility list and hvoRootItem is a top level item in that list
			// and movingPossItem is, or is a subpossibility of, that top level item.
			switch (m_objRepo.GetObject(hvoPossList).OwningFlid)
			{
				case DsDiscourseDataTags.kflidConstChartTempl:
					// hvoPossList is owned in the chart templates property.
					return CheckAndReportBadDiscourseTemplateMove(movingPossItem, hvoRootItem, hvoPossList, hvoDest);
				case LangProjectTags.kflidTextMarkupTags:
					// hvoPossList is owned in the text markup tags property.
					return CheckAndReportBadTagListMove(movingPossItem, hvoRootItem, hvoPossList, hvoDest);
			}
			return false; // not detecting problems with moving other kinds of things.
		}

		/// <summary>
		/// Checks for and reports any disallowed tag list moves.
		/// </summary>
		/// <param name="movingTagItem">The proposed tag item to move.</param>
		/// <param name="hvoSubListRoot">The hvo of the top-level Tag Type Possibility containing the moving item.</param>
		/// <param name="hvoMainTagList">The hvo of the main PossiblityList grouping all TextMarkup Tags.</param>
		/// <param name="hvoDest">The hvo of the destination tag item.</param>
		/// <returns>true if we found and reported a bad move.</returns>
		private bool CheckAndReportBadTagListMove(ICmPossibility movingTagItem, int hvoSubListRoot, int hvoMainTagList, int hvoDest)
		{
			// Check if movingTagItem is a top-level Tag Type.
			if (movingTagItem.Hvo == hvoSubListRoot)
			{
				if (hvoDest == hvoMainTagList) // top-level Tag Type can move to main list (probably already there)
				{
					return false;
				}

				// The moving item is a top-level Tag Type, it cannot be demoted.
				MessageBox.Show(m_tree, AreaResources.ksCantDemoteTagList, AreaResources.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			// Unless something is badly wrong, the destination is either the tag type root,
			// a tag one level down from the root, or the base list.
			if (m_objRepo.GetObject(hvoDest).Owner.Hvo == hvoMainTagList)
			{
				// Destination is Tag Type root, not a problem
				return false;
			}
			if (hvoDest == hvoMainTagList)
			{
				// Can't promote tag to Tag Type root
				MessageBox.Show(m_tree, AreaResources.ksCantPromoteTag, AreaResources.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			// This would give us hierarchy too deep (at least for now)
			MessageBox.Show(m_tree, AreaResources.ksTagListTooDeep, AreaResources.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return true;
		}

		/// <summary>
		/// Checks for and reports any disallowed discourse template moves.
		/// </summary>
		/// <param name="movingColumn">The proposed possibility item (template column) to move.</param>
		/// <param name="hvoTemplate">The hvo of the affected Chart Template (only 'default' exists so far).</param>
		/// <param name="hvoTemplateList">The hvo of the Template List.</param>
		/// <param name="hvoDest">The hvo of the destination item.</param>
		/// <returns>true means we found and reported a bad move.</returns>
		private bool CheckAndReportBadDiscourseTemplateMove(ICmPossibility movingColumn, int hvoTemplate, int hvoTemplateList, int hvoDest)
		{
			using (var movingColumnUI = CmPossibilityUi.MakeLcmModelUiObject(movingColumn))
			{
				// NB: Doesn't need to call 'InitializeFlexComponent', since the code doesn't access the three objects the init call sets.
				// First, check whether we're allowed to manipulate this column at all. This is the same check as
				// whether we're allowed to delete it.
				if (movingColumnUI.CheckAndReportProtectedChartColumn())
				{
					return true;
			}
			}
			// Other things being equal, we now need to make sure we aren't messing up the chart levels
			// Unless something is badly wrong, the destination is either the root template,
			// a column group one level down from the root template, a column two levels down,
			// or the base list.
			if (hvoDest == hvoTemplateList)
			{
				MessageBox.Show(m_tree, AreaResources.ksCantPromoteGroupToTemplate, AreaResources.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			// if the destination IS the root, that's fine...anything can move there.
			if (hvoDest == hvoTemplate)
			{
				return false;
			}
			// It's OK to move a leaf to a group (one level down from the root, as long as
			// the destination 'group' isn't a column that's in use.
			var moveColumnIsLeaf = movingColumn.SubPossibilitiesOS.Count == 0;
			if (m_objRepo.GetObject(hvoDest).Owner.Hvo == hvoTemplate && moveColumnIsLeaf)
			{
				var dest = m_possRepo.GetObject(hvoDest);
				using (var destUI = CmPossibilityUi.MakeLcmModelUiObject(dest))
				{
					// NB: Doesn't need to call 'InitializeFlexComponent', since the code doesn't access the three objects the init call sets.
					// If it isn't already a group, we can only turn it into one if it's empty
					if (dest.SubPossibilitiesOS.Count == 0)
					{
						return destUI.CheckAndReportProtectedChartColumn();
				}
				}
				// If it's already a group it should be fine as a destination.
				return false;
			}
			// Anything else represents an attempt to make the tree too deep, e.g., moving a
			// column into child column, or a group into another group.
			MessageBox.Show(m_tree, AreaResources.ksTemplateTooDeep, AreaResources.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return true;
		}

		private bool OkToDrop(object sender, DragEventArgs e, out TreeNode destNode)
		{
			destNode = null;
			// Don't allow drop in non-hierarchical lists.
			if (MyRecordList.OwningObject is ICmPossibilityList && (MyRecordList.OwningObject as ICmPossibilityList).Depth < 2)
			{
				return false;
			}
			var tree = sender as TreeView;
			if (tree == null)
			{
				return false;
			}
			if (!e.Data.GetDataPresent(typeof(LocalDragItem)))
			{
				return false;
			}
			destNode = tree.GetNodeAt(tree.PointToClient(new Point(e.X, e.Y)));
			var item = (LocalDragItem)e.Data.GetData(typeof(LocalDragItem));
			if (item.SourceNode == destNode)
			{
				return false;
			}
			var hvoMove = (int)item.SourceNode.Tag;
			var hvoDest = 0;
			if (destNode != null)
			{
				hvoDest = (int)destNode.Tag;
			}
			if (hvoDest <= 0)
			{
				return false;
			}
			// It must not be that hvoMove owns hvoDest
			var destObj = m_objRepo.GetObject(hvoDest);
			var moveObj = m_objRepo.GetObject(hvoMove);
			return !destObj.IsOwnedBy(moveObj);
		}

		/// <summary>
		/// Save the mouse down location, because it's a better choice than the mouse move
		/// location for selecting a drag item.  See LT-10295.
		/// </summary>
		Point m_mouseDownLocation;

		void tree_MouseDown(object sender, MouseEventArgs e)
		{
			var tree = (TreeView)sender;
			if (e.Button != MouseButtons.Left)
			{
				var node = tree.GetNodeAt(e.X, e.Y);
				if (node != null)
				{
					tree.SelectedNode = node;
				}
			}
			m_mouseDownLocation = e.Location;
		}

		/// <summary>
		/// Ensure that the selected node is visible VERTICALLY.
		/// TreeNode.EnsureVisible attempts to make as much as possible of the label of the node
		/// visible. If the label is wider than the window, it will scroll horizontally and hide
		/// the plus/minus icons, which is very confusing to the user.
		/// The only way I (JohnT) have found to suppress this is to temporarily give the node a narrow
		/// label while calling EnsureVisible.
		/// </summary>
		/// <param name="tree"></param>
		public void EnsureSelectedNodeVisible(TreeView tree)
		{
			if (tree.SelectedNode == null)
			{
				return;
			}
			var node = tree.SelectedNode;
			m_clickNode = node; // use the current selection just incase the
								// user clicks off the list.  LT-5652
			var label = node.Text;
			try
			{
				node.Text = "a"; // Ugh! Hope the user never sees this! But otherwise it may scroll horizontally.
				node.EnsureVisible();
			}
			finally
			{
				// whatever else happens, make SURE to set it back.
				node.Text = label;
			}
		}

		protected virtual void AddTreeNodes(ArrayList sortedObjects, TreeView tree)
		{
			foreach (IManyOnePathSortItem item in sortedObjects)
			{
				var hvo = item.RootObjectHvo;
				if (hvo < 0) //was deleted
				{
					continue;
				}
				var obj = item.RootObjectUsing(m_cache);
				if (!ShouldAddNode(obj))
				{
					continue;
				}
				AddTreeNode(obj, tree.Nodes);
			}
		}

		protected virtual bool ShouldAddNode(ICmObject obj)
		{
			return true;
		}

		protected void AddToTreeNodeTable(int keyHvo, TreeNode node)
		{
			// Don't add it to the Dictionary again.
			// Checking if it is there first fixes LT-2693.
			// The only side effect is that the object will be displayed multiple times,
			// but when the object is forced to be selected, it will select the one in the Dictionary,
			// not any of the others in the tree.
			if (!m_hvoToTreeNodeTable.ContainsKey(keyHvo))
			{
				m_hvoToTreeNodeTable.Add(keyHvo, node);
		}
		}

		protected virtual string GetDisplayPropertyName => "ShortNameTSS";

		protected virtual string GetTreeNodeLabel(ICmObject obj, out Font font)
		{
			var displayPropertyName = GetDisplayPropertyName;
			var label = ObjectLabel.CreateObjectLabel(obj.Cache, obj, displayPropertyName, m_bestWS);
			// Get the ws of the name, not the abbreviation, if we can.  See FWNX-1059.
			// The string " - " is inserted by ObjectLabel.AsTss after the abbreviation
			// and before the name for semantic domains and anthropology codes.  When
			// localized, these lists are likely not to have localized the abbreviation.
			var tss = label.AsTss;
			var ws = tss.get_WritingSystem(tss.RunCount - 1);
			if (!m_wsToFontTable.TryGetValue(ws, out font))
			{
				var sFont = m_cache.ServiceLocator.WritingSystemManager.Get(ws).DefaultFontName;
				font = new Font(sFont, m_typeSize);
				m_wsToFontTable.Add(ws, font);
			}
			return TsStringUtils.NormalizeToNFC(label.DisplayName);
		}

		protected virtual TreeNode AddTreeNode(ICmObject obj, TreeNodeCollection parentsCollection)
		{
			Font font;
			var node = new TreeNode(GetTreeNodeLabel(obj, out font)) { Tag = obj.Hvo, NodeFont = font };
			parentsCollection.Add(node);
			AddToTreeNodeTable(obj.Hvo, node);
			AddSubNodes(obj, node.Nodes);
			return node;
		}

		/// <summary>
		/// the default implementation does not add any sub nodes
		/// </summary>
		protected virtual void AddSubNodes(ICmObject obj, TreeNodeCollection parentsCollection)
		{
		}
	}
}