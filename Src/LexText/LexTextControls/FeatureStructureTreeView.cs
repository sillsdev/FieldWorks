// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for FeatureStructureTreeView.
	/// </summary>
	public class FeatureStructureTreeView : TreeView, IFWDisposable
	{
		public enum ImageKind
		{
			complex = 0,
			feature,
			radio,
			radioSelected
		}
//		private FeatureTreeNode m_lastSelectedTreeNode = null;
		private System.Windows.Forms.ImageList imageList1;
		private System.ComponentModel.IContainer components;

		public FeatureStructureTreeView(System.ComponentModel.IContainer container)
		{
			///
			/// Required for Windows.Forms Class Composition Designer support
			///
			container.Add(this);
			Init();
		}

		public FeatureStructureTreeView()
		{
			Init();
		}

		private void Init()
		{
			InitializeComponent();

			MouseUp += new MouseEventHandler(OnMouseUp);
			KeyUp += new KeyEventHandler(OnKeyUp);
		}

		public void PopulateTreeFromInflectableFeats(IEnumerable<IFsFeatDefn> defns)
		{
			CheckDisposed();

			foreach (var defn in defns)
			{
				AddNode(defn, null);
			}
		}

		public void PopulateTreeFromInflectableFeat(IFsFeatDefn defn)
		{
			CheckDisposed();

			AddNode(defn, null);
		}

		public void PopulateTreeFromFeatureStructure(IFsFeatStruc fs)
		{
			CheckDisposed();

			AddNode(fs, null);
		}

		public new void Sort()
		{
			CheckDisposed();

			if (Nodes.Count > 0)
				Sort(Nodes);
		}
		public void Sort(TreeNodeCollection col)
		{
			CheckDisposed();

			if (col.Count == 0)
				return;
			List<FeatureTreeNode> list = new List<FeatureTreeNode>(col.Count);
			foreach (FeatureTreeNode childNode in col)
			{
				list.Add(childNode);
			}
			list.Sort();

			BeginUpdate();
			col.Clear();
			foreach (FeatureTreeNode childNode in list)
			{
				col.Add(childNode);
				if (childNode.Nodes.Count > 0)
				{
					if (childNode.Nodes[0].Nodes.Count > 0)
						Sort(childNode.Nodes); // sort all but terminal nodes
					else
					{ // append "none of the above" node to terminal nodes
						FeatureTreeNode noneOfTheAboveNode = new FeatureTreeNode(
							// REVIEW: SHOULD THIS STRING BE LOCALIZED?
							LexTextControls.ksNoneOfTheAbove,
							(int)ImageKind.radio, (int)ImageKind.radio, 0,
							FeatureTreeNodeInfo.NodeKind.Other);
						InsertNode(noneOfTheAboveNode, childNode);
					}
				}
			}
			EndUpdate();
		}

		private void AddNode(IFsFeatStruc fs, FeatureTreeNode parentNode)
		{
			foreach (var spec in fs.FeatureSpecsOC)
			{
				AddNode(spec, parentNode);
			}
		}

		private void AddNode(IFsFeatDefn defn, FeatureTreeNode parentNode)
		{
			var closed = defn as IFsClosedFeature;
			if (closed != null)
			{
				if (!AlreadyInTree(closed.Hvo, parentNode))
				{ // avoid duplicates
					FeatureTreeNode newNode = new FeatureTreeNode(closed.Name.AnalysisDefaultWritingSystem.Text,
																	  (int)ImageKind.feature, (int)ImageKind.feature,
																	  closed.Hvo, FeatureTreeNodeInfo.NodeKind.Closed);
					InsertNode(newNode, parentNode);

					foreach (var val in closed.ValuesSorted)
					{
						AddNode(val, newNode);
					}
				}
			}
			var complex = defn as IFsComplexFeature;
			if (complex != null)
			{
				if (!AlreadyInTree(complex.Hvo, parentNode))
				{ // avoid infinite loop if a complex feature's type is the same as other features.
					FeatureTreeNode newNode = new FeatureTreeNode(complex.Name.BestAnalysisAlternative.Text,
						(int)ImageKind.complex, (int)ImageKind.complex, complex.Hvo, FeatureTreeNodeInfo.NodeKind.Complex);
					InsertNode(newNode, parentNode);
					var type = complex.TypeRA;
					foreach (var defn2 in type.FeaturesRS)
						AddNode(defn2, newNode);
				}
			}
		}

		private void AddNode(IFsSymFeatVal val, FeatureTreeNode parentNode)
		{
			FeatureTreeNode newNode = new FeatureTreeNode(val.Name.BestAnalysisAlternative.Text,
				(int)ImageKind.radio, (int)ImageKind.radio, val.Hvo, FeatureTreeNodeInfo.NodeKind.SymFeatValue);
			InsertNode(newNode, parentNode);
		}

		private void AddNode(IFsFeatureSpecification spec, FeatureTreeNode parentNode)
		{
			var defn = spec.FeatureRA;
			TreeNodeCollection col;
			if (parentNode == null)
				col = Nodes;
			else
				col = parentNode.Nodes;
			var closed = spec as IFsClosedValue;
			if (closed != null)
			{
				foreach (FeatureTreeNode node in col)
				{
					if (defn.Hvo == node.Hvo)
					{ // already there (which is to be expected); see if its value is, too
						AddNodeFromFS(closed.ValueRA, node);
						return;
					}
				}
				// did not find the node, so add it and its value (not to be expected, but we'd better deal with it)
				FeatureTreeNode newNode = new FeatureTreeNode(defn.Name.AnalysisDefaultWritingSystem.Text,
					(int)ImageKind.feature, (int)ImageKind.feature, defn.Hvo, FeatureTreeNodeInfo.NodeKind.Closed);
				InsertNode(newNode, parentNode);
				var val = closed.ValueRA;
				if (val != null)
				{
					FeatureTreeNode newValueNode = new FeatureTreeNode(val.Name.AnalysisDefaultWritingSystem.Text,
						(int)ImageKind.radioSelected, (int)ImageKind.radioSelected, val.Hvo, FeatureTreeNodeInfo.NodeKind.SymFeatValue);
					newValueNode.Chosen = true;
					InsertNode(newValueNode, newNode);
				}
			}
			var complex = spec as IFsComplexValue;
			if (complex != null)
			{
				foreach (FeatureTreeNode node in col)
				{
					if (defn.Hvo == node.Hvo)
					{ // already there (which is to be expected); see if its value is, too
						AddNode((IFsFeatStruc)complex.ValueOA, node);
						return;
					}
				}
				// did not find the node, so add it and its value (not to be expected, but we'd better deal with it)
				FeatureTreeNode newNode = new FeatureTreeNode(defn.Name.AnalysisDefaultWritingSystem.Text,
					(int)ImageKind.complex, (int)ImageKind.complex, defn.Hvo, FeatureTreeNodeInfo.NodeKind.Complex);
				InsertNode(newNode, parentNode);
				AddNode((IFsFeatStruc)complex.ValueOA, newNode);
			}
		}
		private void AddNodeFromFS(IFsSymFeatVal val, FeatureTreeNode parentNode)
		{
			TreeNodeCollection col;
			if (parentNode == null)
				col = Nodes;
			else
				col = parentNode.Nodes;
			if (val == null)
				return; // can't select it!
			int hvoVal = val.Hvo;
			foreach (FeatureTreeNode node in col)
			{
				if (hvoVal == node.Hvo)
				{ // already there (which is to be expected); mark it as selected
					node.ImageIndex = (int)ImageKind.radioSelected;
					node.SelectedImageIndex = (int)ImageKind.radioSelected;
					node.Chosen = true;
					return;
				}
			}
			// did not find the node, so add it (not to be expected, but we'd better deal with it)
			FeatureTreeNode newNode = new FeatureTreeNode(val.Name.AnalysisDefaultWritingSystem.Text,
				(int)ImageKind.radio, (int)ImageKind.radio, val.Hvo, FeatureTreeNodeInfo.NodeKind.SymFeatValue);
			InsertNode(newNode, parentNode);
			newNode.Chosen = true;
		}
		private void InsertNode(FeatureTreeNode newNode, FeatureTreeNode parentNode)
		{
			if (parentNode == null)
				Nodes.Add(newNode);
			else
				parentNode.Nodes.Add(newNode);
		}
		private bool AlreadyInTree(int iTag, FeatureTreeNode node)
		{
			if (node == null)
			{ // at the top level
				foreach (FeatureTreeNode treeNode in Nodes)
				{
					if (iTag == treeNode.Hvo)
						return true;
				}
			}
			while (node != null)
			{
				if (iTag == node.Hvo)
					return true;
				node = (FeatureTreeNode)node.Parent;
			}
			return false;
		}
		private void OnMouseUp(object obj, MouseEventArgs mea)
		{
			if (mea.Button == MouseButtons.Left)
			{
				TreeView tv = (TreeView) obj;
				FeatureTreeNode tn = (FeatureTreeNode)tv.GetNodeAt(mea.X, mea.Y);
				if (tn != null)
				{
					Rectangle rec = tn.Bounds;
					rec.X += -18;       // include the image bitmap (16 pixels plus 2 pixels between the image and the text)
					rec.Width += 18;
					if (rec.Contains(mea.X, mea.Y))
					{
						HandleCheckBoxNodes(tv, tn);
						//int i = tn.ImageIndex;
					}
				}
			}
		}
		private void OnKeyUp(object obj, KeyEventArgs kea)
		{
			TreeView tv = (TreeView) obj;
			FeatureTreeNode tn = (FeatureTreeNode)tv.SelectedNode;
			if (kea.KeyCode == Keys.Space && tn != null)
			{
				HandleCheckBoxNodes(tv, tn);
			}
		}
		private bool IsTerminalNode(TreeNode tn)
		{
			return (tn.Nodes.Count == 0);
		}
//		private void UndoLastSelectedNode()
//		{
//			if (m_lastSelectedTreeNode != null)
//			{
//				if (IsTerminalNode(m_lastSelectedTreeNode))
//				{
//					m_lastSelectedTreeNode.Chosen = false;
//					m_lastSelectedTreeNode.ImageIndex = m_lastSelectedTreeNode.SelectedImageIndex = (int)ImageKind.radio;
//				}
//			}
//		}
		private void HandleCheckBoxNodes(TreeView tv, FeatureTreeNode tn)
		{
			//UndoLastSelectedNode();
			if (IsTerminalNode(tn))
			{
				tn.Chosen = true;
				tn.ImageIndex = tn.SelectedImageIndex = (int)ImageKind.radioSelected;
				if (tn.Parent != null)
				{
					FeatureTreeNode sibling = (FeatureTreeNode)tn.Parent.FirstNode;
					while (sibling != null)
					{
						if (IsTerminalNode(sibling) && sibling != tn)
						{
							sibling.Chosen = false;
							sibling.ImageIndex = sibling.SelectedImageIndex = (int)ImageKind.radio;
						}
						sibling = (FeatureTreeNode)sibling.NextNode;
					}
				}
				tv.Invalidate();
			}
//			m_lastSelectedTreeNode = tn;
		}

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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FeatureStructureTreeView));
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// imageList1
			//
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList1.Images.SetKeyName(0, "");
			this.imageList1.Images.SetKeyName(1, "");
			this.imageList1.Images.SetKeyName(2, "");
			this.imageList1.Images.SetKeyName(3, "");
			//
			// FeatureStructureTreeView
			//
			resources.ApplyResources(this, "$this");
			this.ImageList = this.imageList1;
			this.ResumeLayout(false);

		}
		#endregion
	}
	internal class FeatureTreeNode : TreeNode, IComparable
	{
		protected bool m_fChosen;
		public FeatureTreeNode(string sName, int i, int iSel, int iHvo, FeatureTreeNodeInfo.NodeKind eKind) : base(sName, i, iSel)
		{
			FeatureTreeNodeInfo info = new FeatureTreeNodeInfo(iHvo, eKind);
			Tag = info;
		}
		public int CompareTo(object obj)
		{
			TreeNode node = obj as TreeNode;
			if (node == null)
				return 0; // not sure what else to do...
			return Text.CompareTo(node.Text);
		}
		/// <summary>
		/// Gets/sets whether the node has been chosen by the user
		/// </summary>
		/// <remarks>For some reason, using the Checked property of TreeNode did not work.
		/// I could set Checked to true when loading a feature structure, but when the dialog closed,
		/// the value would always be false.</remarks>
		public bool Chosen
		{
			get
			{
				return m_fChosen;
			}
			set
			{
				m_fChosen = value;
			}
		}
		/// <summary>
		/// Hvo associated with the node
		/// </summary>
		public int Hvo
		{
			get
			{
				FeatureTreeNodeInfo info = Tag as FeatureTreeNodeInfo;
				if (info == null)
					return 0;
				else
					return info.iHvo;
			}
		}
		/// <summary>
		/// Type of node
		/// </summary>
		public FeatureTreeNodeInfo.NodeKind Kind
		{
			get
			{
				FeatureTreeNodeInfo info = Tag as FeatureTreeNodeInfo;
				if (info == null)
					return FeatureTreeNodeInfo.NodeKind.Other;
				else
					return info.eKind;
			}
		}


	}
	internal class FeatureTreeNodeInfo
	{
		public enum NodeKind
		{
			Complex = 0,
			Closed,
			SymFeatValue,
			Other
		}
		public NodeKind eKind;
		public int iHvo;
		public FeatureTreeNodeInfo(int hvo, NodeKind kind)
		{
			iHvo = hvo;
			eKind = kind;
		}
	}
}
