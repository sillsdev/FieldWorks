// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TreeBarHandler.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
//	This class is responsible for populating the XCore tree bar with the records
//	that are given to it by the RecordClerk.
// </remarks>

// --------------------------------------------------------------------------------------------
using System;
using XCore;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.XWorks
{

	/// <summary>
	/// Responsible for populating the XCore tree bar with the records.
	/// </summary>
	public abstract class RecordBarHandler : IFWDisposable
	{
		protected XCore.Mediator m_mediator;
		protected FdoCache m_cache; // initialized with mediator.
		protected bool m_expand;
		protected bool m_hierarchical;
		protected bool m_includeAbbr;
		protected string m_bestWS;
		// This gets set when we skipped populating the tree bar because it wasn't visible.
		protected bool m_fOutOfDate = false;

		static public RecordBarHandler Create(XCore.Mediator mediator, XmlNode toolConfiguration)
		{
			RecordBarHandler handler = null;
			XmlNode node = toolConfiguration.SelectSingleNode("treeBarHandler");
			//if (node != null)
			//{
			//    handler = (TreeBarHandler)DynamicLoader.CreateObject(node);
			//    handler.Init(mediator, node);
			//}
			if (node == null)
				handler = new RecordBarListHandler();
			else
				handler = (TreeBarHandler)DynamicLoader.CreateObject(node);
			handler.Init(mediator, node);
			return handler;
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
		private bool m_isDisposed = false;

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
		~RecordBarHandler()
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;
			m_cache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		internal virtual void  Init(XCore.Mediator mediator, XmlNode node)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");

			if (node != null)
			{
				m_hierarchical = XmlUtils.GetBooleanAttributeValue(node, "hierarchical");
				m_expand = XmlUtils.GetBooleanAttributeValue(node, "expand");
				m_includeAbbr = XmlUtils.GetBooleanAttributeValue(node, "includeAbbr");
				m_bestWS = XmlUtils.GetOptionalAttributeValue(node, "ws", null);
			}
		}

		public void PopulateRecordBarIfNeeded(RecordList list)
		{
			CheckDisposed();

			if (m_fOutOfDate)
			{
				PopulateRecordBar(list);
			}
		}
		public abstract void PopulateRecordBar(RecordList list);
		public abstract void UpdateSelection(ICmObject currentObject);
		public abstract void ReloadItem(ICmObject currentObject);
		public abstract void ReleaseRecordBar();

		protected bool IsShowing
		{
			get
			{
				return m_mediator.PropertyTable.GetBoolProperty("ShowRecordList", false);
			}
		}

		protected virtual bool ShouldAddNode(ICmObject obj)
		{
			return true;
		}
	}

	/// <summary>
	/// makes a hierarchical tree of possibility items, *even if the record list is flattened*
	/// </summary>
	public class PossibilityTreeBarHandler : TreeBarHandler
	{
		//must have a constructor with no parameters, to use with the dynamic loader.
		public PossibilityTreeBarHandler()
		{
		}

		protected override string GetDisplayPropertyName
		{
			get
			{
				// NOTE: For labels with abbreviations using "LongName" rather than "AbbrAndNameTSS"
				// seems to load quicker for Semantic Domains and AnthroCodes.
				if (m_includeAbbr)
					return "LongName";
				else
					return base.GetDisplayPropertyName;
			}
		}

		/// <summary>
		/// add any subitems to the tree. Note! This assumes that the list has been preloaded
		/// (e.g., using PreLoadList), so it bypasses normal load operations for speed purposes.
		/// Withoug preloading, it took almost 19,000 queries to start FW showing semantic domain
		/// list. With preloading it reduced the number to 200 queries.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="parentsCollection"></param>
		protected override void AddSubNodes(ICmObject obj, TreeNodeCollection parentsCollection)
		{
			var pss = obj as ICmPossibility;
			foreach (var subPss in pss.SubPossibilitiesOS)
			{
				AddTreeNode(subPss, parentsCollection);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> this is overridden because we actually need to avoid adding items from the top-level if
		/// they are not top-level possibilities. They will show up under their respective parents.in other words,
		/// if the list we are given has been flattened, we need to un-flatten it.</remarks>
		protected override bool ShouldAddNode(ICmObject obj)
		{
			var possibility = (ICmPossibility)obj;
			//don't show it if it is a child of another possibility.
			return possibility.OwningFlid != CmPossibilityTags.kflidSubPossibilities;
		}

		protected override ContextMenuStrip CreateTreebarContextMenuStrip()
		{
			ContextMenuStrip menu = base.CreateTreebarContextMenuStrip();
			if (RecordList.OwningObject is ICmPossibilityList
				&& !(RecordList.OwningObject as ICmPossibilityList).IsSorted)
			{
				// Move up and move down items make sense
				menu.Items.Add(new ToolStripMenuItem(xWorksStrings.MoveUp));
				menu.Items.Add(new ToolStripMenuItem(xWorksStrings.MoveDown));
			}
			return menu;
		}

		protected override void tree_moveUp()
		{
			MoveItem(-1);
		}
		protected override void tree_moveDown()
		{
			MoveItem(1);
		}

		/// <summary>
		/// Move the clicked item the specified distance (currently +/- 1) in its owning list.
		/// </summary>
		/// <param name="distance"></param>
		void MoveItem(int distance)
		{
			int hvoMove = ClickObject;
			var column = m_possRepo.GetObject(hvoMove);
			if (column.CheckAndReportProtectedChartColumn())
				return;
			var owner = column.Owner;
			if (owner == null) // probably not possible
				return;
			int hvoOwner = owner.Hvo;
			int ownFlid = column.OwningFlid;
			int oldIndex = m_cache.DomainDataByFlid.GetObjIndex(hvoOwner, (int)ownFlid, column.Hvo);
			int newIndex = oldIndex + distance;
			if (newIndex < 0)
				return;
			int cobj = m_cache.DomainDataByFlid.get_VecSize(hvoOwner, (int)ownFlid);
			if (newIndex >= cobj)
				return;
			// Without this, we insert it before the next object, which is the one it's already before,
			// so it doesn't move.
			if (distance > 0)
				newIndex++;
			UndoableUnitOfWorkHelper.Do(xWorksStrings.UndoMoveItem, xWorksStrings.RedoMoveItem,
				m_cache.ActionHandlerAccessor,
				() =>
					{
						m_cache.DomainDataByFlid.MoveOwnSeq(hvoOwner, (int) ownFlid, oldIndex, oldIndex, hvoOwner,
														  (int) ownFlid, newIndex);
					});
		}
	}

	public class TreeBarHandler : RecordBarHandler
	{
		protected Dictionary<int, TreeNode> m_hvoToTreeNodeTable = new Dictionary<int, TreeNode>();

		private TreeNode m_dragHiliteNode; // node that currently has background set to show drag destination
		private TreeNode m_clickNode; // node the user mouse-downed on

		protected ICmObjectRepository m_objRepo;
		protected ICmPossibilityRepository m_possRepo;

		TreeView m_tree;
		int m_typeSize;		// font size for the tree's fonts.
		// map from writing system to font.
		Dictionary<int, Font> m_dictFonts = new Dictionary<int, Font>();

		//must have a constructor with no parameters, to use with the dynamic loader.
		protected TreeBarHandler()
		{
		}

		internal override void Init(Mediator mediator, XmlNode node)
		{
			base.Init(mediator, node);
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
					return 0;
				return (int)m_clickNode.Tag;
			}
		}

		#region IDisposable override

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_hvoToTreeNodeTable != null)
					m_hvoToTreeNodeTable.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_hvoToTreeNodeTable = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		private RecordList m_list;

		/// <summary>
		/// Check whether the given hvo is represented by a TreeNode.
		/// </summary>
		internal bool IsHvoATreeNode(int hvo)
		{
			TreeNode node;
			return m_hvoToTreeNodeTable.TryGetValue(hvo, out node);
		}

		public override void ReloadItem(ICmObject currentObject)
		{
			CheckDisposed();

			if (currentObject == null || m_hvoToTreeNodeTable.Count == 0)
				return;
			if (this.IsShowing)
			{
				m_fOutOfDate = false;
			}
			else
			{
				m_fOutOfDate = true;
				return;
			}

			TreeNode node = m_hvoToTreeNodeTable[currentObject.Hvo];
			if (node == null)
				return;
			Font font;
			var text = GetTreeNodeLabel(currentObject, out font);
// ReSharper disable RedundantCheckBeforeAssignment
			if (text != node.Text)
				node.Text = text;
			if (font != node.NodeFont)
				node.NodeFont = font;
// ReSharper restore RedundantCheckBeforeAssignment
		}

		/// <summary>
		/// Makes the record list available to subclasses.
		/// </summary>
		protected RecordList RecordList
		{
			get { return m_list; }
		}

		public override void PopulateRecordBar(RecordList list)
		{
			CheckDisposed();

			if (this.IsShowing)
			{
				m_fOutOfDate = false;
			}
			else
			{
				m_fOutOfDate = true;
				return;
			}

			m_list = list;

			XWindow window = (XWindow)m_mediator.PropertyTable.GetValue("window");
			using (new WaitCursor(window))
			{
				window.TreeBarControl.IsFlatList = false;
				TreeView tree = (TreeView)window.TreeStyleRecordList;
				Set<int> expandedItems = new Set<int>();
				if (m_tree != null && !m_expand)
					GetExpandedItems(m_tree.Nodes, expandedItems);
				m_tree = tree;

				// Removing the handlers first seems to be necessary because multiple tree handlers are
				// working with one treeview. Only this active one should have handlers connected.
				// If we fail to do this, switching to a different list causes drag and drop to stop working.
				ReleaseRecordBar();

				tree.NodeMouseClick += new TreeNodeMouseClickEventHandler(tree_NodeMouseClick);
				tree.MouseDown += new MouseEventHandler(tree_MouseDown);
				tree.MouseMove += new MouseEventHandler(tree_MouseMove);
				tree.DragDrop += new DragEventHandler(tree_DragDrop);
				tree.DragOver += new DragEventHandler(tree_DragOver);
				tree.GiveFeedback += new GiveFeedbackEventHandler(tree_GiveFeedback);
				tree.ContextMenuStrip = CreateTreebarContextMenuStrip();
				tree.ContextMenuStrip.MouseClick += new MouseEventHandler(tree_MouseClicked);
				tree.AllowDrop = true;
				tree.BeginUpdate();
				window.ClearRecordBarList();	//don't want to directly clear the nodes, because that causes an event to be fired as every single note is removed!
				m_hvoToTreeNodeTable.Clear();

				// type size must be set before AddTreeNodes is called
				m_typeSize = list.TypeSize;
				AddTreeNodes(list.SortedObjects, tree);

				tree.Font = new System.Drawing.Font(list.FontName, m_typeSize);
				tree.ShowRootLines = m_hierarchical;

				if (m_expand)
					tree.ExpandAll();
				else
				{
					tree.CollapseAll();
					ExpandItems(tree.Nodes, expandedItems);
				}
				// Set the selection after expanding/collapsing the tree.  This allows the true
				// selection to be visible even when the tree is collapsed but the selection is
				// an internal node.  (See LT-4508.)
				UpdateSelection(list.CurrentObject);
				tree.EndUpdate();

				EnsureSelectedNodeVisible(tree);
			}
		}

		/// <summary>
		/// For all the nodes that are expanded, if their tag is an int, add it to the set.
		/// </summary>
		/// <param name="treeNodeCollection"></param>
		/// <param name="expandedItems"></param>
		private void GetExpandedItems(TreeNodeCollection treeNodeCollection, Set<int> expandedItems)
		{
			foreach (TreeNode node in treeNodeCollection)
			{
				if (node.IsExpanded)
				{
					if (node.Tag is int)
						expandedItems.Add((int)node.Tag);
					GetExpandedItems(node.Nodes, expandedItems);
				}
			}
		}

		/// <summary>
		/// If any of the nodes in treeNodeCollection has a tag that is an int that is in the set,
		/// expand it, and recursively check its children.
		/// </summary>
		/// <param name="treeNodeCollection"></param>
		/// <param name="expandedItems"></param>
		private void ExpandItems(TreeNodeCollection treeNodeCollection, Set<int> expandedItems)
		{
			foreach (TreeNode node in treeNodeCollection)
			{
				if (node.Tag is int && expandedItems.Contains((int)node.Tag))
				{
					node.Expand();
					ExpandItems(node.Nodes, expandedItems);
				}
			}
		}

		protected virtual ContextMenuStrip CreateTreebarContextMenuStrip()
		{
			ToolStripMenuItem promoteMenuItem = new ToolStripMenuItem(xWorksStrings.Promote);
			ContextMenuStrip contStrip = new ContextMenuStrip();
			contStrip.Items.Add(promoteMenuItem);
			return contStrip;
		}

		public override void ReleaseRecordBar()
		{
			CheckDisposed();

			if (m_tree != null)
			{
				m_tree.NodeMouseClick -= new TreeNodeMouseClickEventHandler(tree_NodeMouseClick);
				m_tree.MouseDown -= new MouseEventHandler(tree_MouseDown);
				m_tree.MouseMove -= new MouseEventHandler(tree_MouseMove);
				m_tree.DragDrop -= new DragEventHandler(tree_DragDrop);
				m_tree.DragOver -= new DragEventHandler(tree_DragOver);
				m_tree.GiveFeedback -= new GiveFeedbackEventHandler(tree_GiveFeedback);
			}
		}

		void tree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			m_clickNode = e.Node;
		}

		void tree_GiveFeedback(object sender, GiveFeedbackEventArgs e)
		{
		}

		private void ClearDragHilite()
		{
			if (m_dragHiliteNode != null)
				m_dragHiliteNode.BackColor = Color.FromKnownColor(KnownColor.Window);
		}

		void tree_Promote()
		{
			if (m_clickNode == null)	// LT-5652: don't promote anything
				return;

			TreeNode source = m_clickNode;
			// destination for promote is two levels up, or null to move all the way to the top.
			TreeNode destNode = source.Parent;
			if (destNode != null)
				destNode = destNode.Parent;
			MoveItem(m_tree, destNode, source);
		}

		void tree_MouseClicked(object sender, MouseEventArgs e)
		{
			// LT-5664  This event handler was set up to ensure the user does not
			// accidentally select the Promote command with a right mouse click.
			if (e.Button == MouseButtons.Left)
			{
				ToolStripItem item = m_tree.ContextMenuStrip.GetItemAt(e.X, e.Y);
				if (item == null)
					return;

				string ItemSelected = item.Text;
				if (ItemSelected.Equals(xWorksStrings.Promote))
					tree_Promote();
				else if (ItemSelected.Equals(xWorksStrings.MoveDown))
					tree_moveDown();
				else if (ItemSelected.Equals(xWorksStrings.MoveUp))
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

		void tree_DragOver(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(typeof(LocalDragItem)))
			{
				e.Effect = DragDropEffects.None; // not my sort of data at all, can't drop
				return;
			}
			// An inactive handler is unfortunately also being notified. Don't express any
			// opinion at all about drag effects.
			LocalDragItem item = (LocalDragItem)e.Data.GetData(typeof(LocalDragItem));
			if (item.Handler != this)
				return;

			TreeNode destNode;
			if (OkToDrop(sender, e, out destNode))
				e.Effect = DragDropEffects.Move;
			else
				e.Effect = DragDropEffects.None;
			if (destNode != m_dragHiliteNode)
			{
				ClearDragHilite();
				m_dragHiliteNode = destNode;
				if (m_dragHiliteNode != null)
					m_dragHiliteNode.BackColor = Color.Gray;
			}
		}

		void tree_MouseMove(object sender, MouseEventArgs e)
		{
			if ((e.Button & MouseButtons.Left) != MouseButtons.Left)
				return;
			TreeView tree = sender as TreeView;
			if (tree == null)
				return;
			// The location here is always different than the one in tree_MouseDown.
			// Sometimes, the difference is great enough to choose an adjacent item!
			// See LT-10295.  So we'll use the location stored in tree_MouseDown.
			// (The sample code in MSDN uses the item/location information in MouseDown
			// establish the item to drag in MouseMove.)
			TreeNode selItem = tree.GetNodeAt(m_mouseDownLocation);
			if (selItem == null)
				return;
			LocalDragItem item = new LocalDragItem(this, selItem);
			tree.DoDragDrop(item, DragDropEffects.Move);
			ClearDragHilite();
		}

		/// <summary>
		/// Currently we only know how to move our own items.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void tree_DragDrop(object sender, DragEventArgs e)
		{
			TreeNode destNode;
			if (!OkToDrop(sender, e, out destNode))
				return;
			// Notification also gets sent to inactive handlers, which should ignore it.
			LocalDragItem item = (LocalDragItem)e.Data.GetData(typeof(LocalDragItem));
			if (item.Handler != this)
				return;
			if (e.Effect != DragDropEffects.Move)
				return;
			MoveItem(sender, destNode, item.SourceNode);
		}

		private void MoveItem(object sender, TreeNode destNode, TreeNode sourceItem)
		{
			var hvoMove = (int)sourceItem.Tag;
			ICmObject dest;
			var hvoDest = 0;
			int flidDest;
			var cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			var move = cache.ServiceLocator.GetObject(hvoMove);
			var moveLabel = sourceItem.Text;
			TreeNodeCollection newSiblings;
			var tree = sender as TreeView;
			if (destNode == null)
			{
				for (dest = move.Owner; dest != null; dest = dest.Owner)
				{
					if (!(dest is ICmPossibilityList))
						continue;
					hvoDest = dest.Hvo;
					break;
				}
				if (dest == null)
					return;
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
				return;

			var hvoOldOwner = move.Owner.Hvo;
			if (hvoOldOwner == hvoDest)
				return; // nothing to do.

			var flidSrc = move.OwningFlid;
			var srcIndex = cache.DomainDataByFlid.GetObjIndex(hvoOldOwner, flidSrc, hvoMove);
			var ihvoDest = 0;
			for (; ihvoDest < newSiblings.Count; ihvoDest++)
			{
				if (newSiblings[ihvoDest].Text.CompareTo(moveLabel) > 0) // Enhance JohnT: use ICU comparison...
					break;
			}
			using (new RecordClerk.ListUpdateHelper(m_list, tree.TopLevelControl))
			{
				UndoableUnitOfWorkHelper.Do(xWorksStrings.UndoMoveItem, xWorksStrings.RedoMoveItem,
					cache.ActionHandlerAccessor, () =>
						cache.DomainDataByFlid.MoveOwnSeq(hvoOldOwner, flidSrc, srcIndex, srcIndex,
														 hvoDest, flidDest, ihvoDest));
				// Note: use MoveOwningSequence off FdoCache,
				// so we get propchanges that can be picked up by SyncWatcher (CLE-76)
				// (Hopefully the propchanges won't cause too much intermediant flicker,
				// before ListUpdateHelper calls ReloadList())
				//cache.DomainDataByFlid.MoveOwnSeq(hvoOldOwner, flidSrc, srcIndex, srcIndex,
				//    hvoDest, flidDest, ihvoDest);
				//move.MoveSideEffects(hvoOldOwner);
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
		/// <param name="hvoMove"></param>
		/// <param name="hvoDest"></param>
		/// <returns>true if a problem was reported and the move should be cancelled.</returns>
		private bool CheckAndReportForbiddenMove(int hvoMove, int hvoDest)
		{
			var cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			var movingPossItem = m_possRepo.GetObject(hvoMove);
			if (movingPossItem != null)
			{
				var rootPoss = movingPossItem.MainPossibility;
				int hvoRootItem = rootPoss.Hvo;
				int hvoPossList = rootPoss.OwningList.Hvo;

				// If we get here hvoPossList is a possibility list and hvoRootItem is a top level item in that list
				// and movingPossItem is, or is a subpossibility of, that top level item.

				// 1. Check to see if hvoRootItem is a chart template containing our target.
				// If so, hvoPossList is owned in the chart templates property.
				if (m_objRepo.GetObject(hvoPossList).OwningFlid == DsDiscourseDataTags.kflidConstChartTempl)
					return CheckAndReportBadDiscourseTemplateMove(cache, movingPossItem, hvoRootItem, hvoPossList, hvoDest);

				// 2. Check to see if hvoRootItem is a TextMarkup TagList containing our target (i.e. a Tag type).
				// If so, hvoPossList is owned in the text markup tags property.
				if (m_objRepo.GetObject(hvoPossList).OwningFlid == LangProjectTags.kflidTextMarkupTags)
					return CheckAndReportBadTagListMove(cache, movingPossItem, hvoRootItem, hvoPossList, hvoDest);
			}
			return false; // not detecting problems with moving other kinds of things.
		}

		/// <summary>
		/// Checks for and reports any disallowed tag list moves.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="movingTagItem">The proposed tag item to move.</param>
		/// <param name="hvoSubListRoot">The hvo of the top-level Tag Type Possibility containing the moving item.</param>
		/// <param name="hvoMainTagList">The hvo of the main PossiblityList grouping all TextMarkup Tags.</param>
		/// <param name="hvoDest">The hvo of the destination tag item.</param>
		/// <returns>true if we found and reported a bad move.</returns>
		private bool CheckAndReportBadTagListMove(FdoCache cache, ICmPossibility movingTagItem, int hvoSubListRoot,
			int hvoMainTagList, int hvoDest)
		{
			// Check if movingTagItem is a top-level Tag Type.
			if (movingTagItem.Hvo == hvoSubListRoot)
			{
				if (hvoDest == hvoMainTagList) // top-level Tag Type can move to main list (probably already there)
					return false;

				// The moving item is a top-level Tag Type, it cannot be demoted.
				MessageBox.Show(m_tree, xWorksStrings.ksCantDemoteTagList,
								xWorksStrings.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
				MessageBox.Show(m_tree, xWorksStrings.ksCantPromoteTag,
								xWorksStrings.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			// This would give us hierarchy too deep (at least for now)
			MessageBox.Show(m_tree, xWorksStrings.ksTagListTooDeep,
							xWorksStrings.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return true;
				}

		/// <summary>
		/// Checks for and reports any disallowed discourse template moves.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="movingColumn">The proposed possibility item (template column) to move.</param>
		/// <param name="hvoTemplate">The hvo of the affected Chart Template (only 'default' exists so far).</param>
		/// <param name="hvoTemplateList">The hvo of the Template List.</param>
		/// <param name="hvoDest">The hvo of the destination item.</param>
		/// <returns>true means we found and reported a bad move.</returns>
		private bool CheckAndReportBadDiscourseTemplateMove(FdoCache cache, ICmPossibility movingColumn, int hvoTemplate,
			int hvoTemplateList, int hvoDest)
		{
			// First, check whether we're allowed to manipulate this column at all. This is the same check as
				// whether we're allowed to delete it.
			if (movingColumn.CheckAndReportProtectedChartColumn())
					return true;
				// Other things being equal, we now need to make sure we aren't messing up the chart levels
				// Unless something is badly wrong, the destination is either the root template,
				// a column group one level down from the root template, a column two levels down,
				// or the base list.
			if (hvoDest == hvoTemplateList)
				{
					MessageBox.Show(m_tree, xWorksStrings.ksCantPromoteGroupToTemplate,
								xWorksStrings.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return true;
				}
				// if the destination IS the root, that's fine...anything can move there.
			if (hvoDest == hvoTemplate)
					return false;
				// It's OK to move a leaf to a group (one level down from the root, as long as
				// the destination 'group' isn't a column that's in use.
			bool moveColumnIsLeaf = movingColumn.SubPossibilitiesOS.Count == 0;
			if (m_objRepo.GetObject(hvoDest).Owner.Hvo == hvoTemplate && moveColumnIsLeaf)
				{
					var dest = m_possRepo.GetObject(hvoDest);
					// If it isn't already a group, we can only turn it into one if it's empty
					if (dest.SubPossibilitiesOS.Count == 0)
						return dest.CheckAndReportProtectedChartColumn();
					// If it's already a group it should be fine as a destination.
					return false;
				}
				// Anything else represents an attempt to make the tree too deep, e.g., moving a
				// column into child column, or a group into another group.
				MessageBox.Show(m_tree, xWorksStrings.ksTemplateTooDeep,
							xWorksStrings.ksProhibitedMovement, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}

		private bool OkToDrop(object sender, DragEventArgs e, out TreeNode destNode)
		{
			destNode = null;
			// Don't allow drop in non-hierarchical lists.
			if (m_list.OwningObject is ICmPossibilityList && (m_list.OwningObject as ICmPossibilityList).Depth < 2)
				return false;
			TreeView tree = sender as TreeView;
			if (tree == null)
				return false;
			if (!e.Data.GetDataPresent(typeof(LocalDragItem)))
				return false;
			destNode = tree.GetNodeAt(tree.PointToClient(new Point(e.X, e.Y)));
			LocalDragItem item = (LocalDragItem)e.Data.GetData(typeof(LocalDragItem));
			if (item.SourceNode == destNode)
				return false;
			int hvoMove = (int)item.SourceNode.Tag;
			int hvoDest = 0;
			if (destNode != null)
				hvoDest = (int)destNode.Tag;
			if (hvoDest <= 0)
				return false;
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
			TreeView tree = sender as TreeView;
			if (e.Button != MouseButtons.Left)
			{
				TreeNode node = tree.GetNodeAt(e.X, e.Y);
				if (node != null)
					tree.SelectedNode = node;
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
			CheckDisposed();

			if (tree.SelectedNode == null)
				return;
			TreeNode node = tree.SelectedNode;
			m_clickNode = node;		// use the current selection just incase the
									// user clicks off the list.  LT-5652
			string label = node.Text;
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
			foreach(IManyOnePathSortItem item in sortedObjects)
			{
				var hvo = item.RootObjectHvo;
				if(hvo < 0)//was deleted
					continue;
				var obj = item.RootObjectUsing(m_cache);
				if (!ShouldAddNode(obj))
					continue;
				AddTreeNode(obj, tree.Nodes);
			}
		}

		protected void AddToTreeNodeTable(int keyHvo, TreeNode node)
		{
			// Don't add it to the Dictionary again.
			// Checking if it is there first fixes LT-2693.
			// The only side effect is that the object will be displayed multiple times,
			// but when the object is forced to be selected, it will select the one in the Dictionary,
			// not any of the others in the tree.
			if (!m_hvoToTreeNodeTable.ContainsKey(keyHvo))
				m_hvoToTreeNodeTable.Add(keyHvo, node);
		}

		protected virtual string GetDisplayPropertyName
		{
			get { return "ShortNameTSS"; }
		}

		protected virtual string GetTreeNodeLabel(ICmObject obj, out Font font)
		{
			string displayPropertyName = GetDisplayPropertyName;
			ObjectLabel label = ObjectLabel.CreateObjectLabel(obj.Cache, obj, displayPropertyName, m_bestWS);
			int ws = TsStringUtils.GetWsAtOffset(label.AsTss, 0);
			if (!m_dictFonts.TryGetValue(ws, out font))
			{
				string sFont = m_cache.ServiceLocator.WritingSystemManager.Get(ws).DefaultFontName;
				font = new Font(sFont, m_typeSize);
				m_dictFonts.Add(ws, font);
			}
			return TsStringUtils.NormalizeToNFC(label.DisplayName);
		}

		protected virtual TreeNode AddTreeNode(ICmObject obj, TreeNodeCollection parentsCollection)
		{
			Font font;
			TreeNode node = new TreeNode( GetTreeNodeLabel(obj, out font) );
			node.Tag = obj.Hvo; //note that we could store the whole object instead.
			node.NodeFont = font;
			parentsCollection.Add(node);
			AddToTreeNodeTable(obj.Hvo, node);
			AddSubNodes(obj, node.Nodes);
			return node;
		}

		/// <summary>
		/// the default implementation does not add any sub nodes
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="parentsCollection"></param>
		protected virtual void AddSubNodes(ICmObject obj, TreeNodeCollection parentsCollection)
		{

		}

		public override void UpdateSelection(ICmObject currentObject)
		{
			CheckDisposed();

			XWindow window = (XWindow)m_mediator.PropertyTable.GetValue("window");
			TreeView tree = (TreeView)window.TreeStyleRecordList;
			if (currentObject == null)
			{
				tree.SelectedNode = null;
				m_clickNode = null; // otherwise we can try to promote a deleted one etc.
				return;
			}

			TreeNode node = null;
			if (m_hvoToTreeNodeTable.ContainsKey(currentObject.Hvo))
				node = m_hvoToTreeNodeTable[currentObject.Hvo];
			//Debug.Assert(node != null);
			// node.EnsureVisible() throws an exception if tree != node.TreeView, and this can
			// happen somehow.  (see LT-986)
			if (node != null && node.TreeView == tree && (tree.SelectedNode != node))
			{
				tree.SelectedNode = node;
				EnsureSelectedNodeVisible(tree);
			}
		}
	}

	/// <summary>
	/// Used to represent a drag from one place in the tree view to another. This is the only kind of drag
	/// currently supported.
	/// </summary>
	class LocalDragItem
	{
		TreeBarHandler m_handler; // handler dragging from
		TreeNode m_sourceTreeNode; // tree node being dragged

		public LocalDragItem(TreeBarHandler handler, TreeNode sourceTreeNode)
		{
			m_handler = handler;
			m_sourceTreeNode = sourceTreeNode;
		}
		public TreeBarHandler Handler
		{
			get {return m_handler; }
		}

		public TreeNode SourceNode
		{
			get { return m_sourceTreeNode; }
		}
	}
}
