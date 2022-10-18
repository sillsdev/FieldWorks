<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/LabelNode.cs
// Copyright (c) 2006-2020 SIL International
||||||| f013144d5:Src/Common/Controls/XMLViews/LabelNode.cs
﻿// Copyright (c) 2015 SIL International
=======
// Copyright (c) 2015 SIL International
>>>>>>> develop:Src/Common/Controls/XMLViews/LabelNode.cs
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This override to TreeNode handles the displaying of an ObjectLabel in a custom way
	/// </summary>
	public class LabelNode : TreeNode
	{
		/// <summary />
		protected IVwStylesheet m_stylesheet;
		private bool m_displayUsage;
		private bool m_fEnabled;
		private Color m_enabledColor;

		/// <summary>
		/// Return the basic string representing the label for this node.
		/// </summary>
		protected virtual string BasicNodeString => Label.AsTss.Text;

<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/LabelNode.cs
		/// <summary />
		public LabelNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage)
||||||| f013144d5:Src/Common/Controls/XMLViews/LabelNode.cs
		/// <summary>
		/// Initializes a new instance of the <see cref="LabelNode"/> class.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="displayUsage"><c>true</c> if usage statistics will be displayed; otherwise, <c>false</c>.</param>
		public LabelNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage)
=======
		/// <summary>
		/// Initializes a new instance of the <see cref="LabelNode"/> class.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="displayUsage"><c>true</c> if usage statistics will be displayed; otherwise, <c>false</c>.</param>
		/// <param name="initialize"><c>true</c> This constructor should do the initialization.
		///                          <c>false</c> Do not do the initialization, let the caller do it.</param>
		public LabelNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage, bool initialize = true)
>>>>>>> develop:Src/Common/Controls/XMLViews/LabelNode.cs
		{
			Tag = label;
			m_stylesheet = stylesheet;
			m_displayUsage = displayUsage;
			m_fEnabled = true;
			m_enabledColor = ForeColor;
			var tssDisplay = label.AsTss;
			if (HasVernacularText(tssDisplay, label.Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(ws => ws.Handle), out var wsVern))
			{
				NodeFont = GetVernacularFont(label.Cache.WritingSystemFactory, wsVern, stylesheet);
			}
			SetNodeText();
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/LabelNode.cs
			if (label.HaveSubItems)
			{
||||||| f013144d5:Src/Common/Controls/XMLViews/LabelNode.cs
			if (label.HaveSubItems)
=======
			if (initialize && label.HaveSubItems)
>>>>>>> develop:Src/Common/Controls/XMLViews/LabelNode.cs
				// this is a hack to make the node expandable before we have filled in any
				// actual children
				Nodes.Add(new TreeNode("should not see this"));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to display the usage statistics.
		/// </summary>
		public bool DisplayUsage
		{
			get => m_displayUsage;
			set
			{
				m_displayUsage = value;
				SetNodeText();
			}
		}

		private void SetNodeText()
		{
			var text = BasicNodeString;
			if (m_displayUsage)
			{
				var count = CountUsages();
				if (count > 0)
				{
					text += $" ({count})";
				}
			}
			Text = text;
		}

		/// <summary>
		/// Count how many references this item has. Virtual so a subclass can use a different
		/// algorithm (c.f. SemanticDomainsChooser's DomainNode).
		/// </summary>
		protected virtual int CountUsages()
		{
			// Don't count the reference from an overlay, since we have no way to tell
			// how many times that overlay has been used.  See FWR-1050.
			var label = Label;
			var count = 0;
			// I think only label.Object is likely to be null, but let's prevent crashes thoroughly.
			if (label?.Object?.ReferringObjects != null)
			{
				count = label.Object.ReferringObjects.Count;
				foreach (var x in label.Object.ReferringObjects)
				{
					if (x is ICmOverlay)
					{
						--count;
					}
				}
			}
			return count;
		}

		private static bool HasVernacularText(ITsString tss, IEnumerable<int> vernWses, out int wsVern)
		{
			wsVern = 0;
			var crun = tss.RunCount;
			for (var irun = 0; irun < crun; irun++)
			{
				var ttp = tss.get_Properties(irun);
				var ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out _);
				if (vernWses.Any(vernWS => ws == vernWS))
				{
					wsVern = ws;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Resets the vernacular font.
		/// </summary>
		public void ResetVernacularFont(ILgWritingSystemFactory wsf, int wsVern, IVwStylesheet stylesheet)
		{
			NodeFont = GetVernacularFont(wsf, wsVern, stylesheet);
		}

		private static Font GetVernacularFont(ILgWritingSystemFactory wsf, int wsVern, IVwStylesheet stylesheet)
		{
			if (stylesheet == null)
			{
				var wsEngine = wsf.get_EngineOrNull(wsVern);
				var fontName = wsEngine.DefaultFontName;
				return new Font(fontName, (float)10.0);
			}
			return FontHeightAdjuster.GetFontForNormalStyle(wsVern, stylesheet, wsf);
		}

		/// <summary>
		/// Gets the label.
		/// </summary>
		public ObjectLabel Label => (ObjectLabel)Tag;

		/// <summary>
		/// Add the children nodes of a particular node in the tree. Do this recursively if
		/// that parameter is set to true. Also set the node as Checked if is it in the set
		/// or chosenObjs.
		/// </summary>
		public void AddChildren(bool recursively, IEnumerable<ICmObject> chosenObjs)
		{
			// JohnT: if we redo this every time we open, we discard the old nodes AND THEIR VALUES.
			// Thus, collapsing and reopening a tree clears its members! But we do need to check
			// that we have a label node, we put a dummy one in to show that it can be expanded.
			if (Nodes.Count > 0 && Nodes[0] is LabelNode)
			{
				// This already has its nodes, but what about its children if recursive?
				if (!recursively)
				{
					return;
				}
				foreach (LabelNode node in Nodes)
				{
					node.AddChildren(true, chosenObjs);
				}
				return;
			}
			Nodes.Clear(); // get rid of the dummy.
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/LabelNode.cs
			AddSecondaryNodes(this, Nodes, chosenObjs);
			foreach (var label in ((ObjectLabel)Tag).SubItems)
||||||| f013144d5:Src/Common/Controls/XMLViews/LabelNode.cs

			AddSecondaryNodes(this, Nodes, chosenObjs);
			foreach (ObjectLabel label in ((ObjectLabel)Tag).SubItems)
=======

			AddSecondaryNodes(this, chosenObjs);
			foreach (ObjectLabel label in ((ObjectLabel)Tag).SubItems)
>>>>>>> develop:Src/Common/Controls/XMLViews/LabelNode.cs
			{
				if (!WantNodeForLabel(label))
				{
					continue;
				}
				var node = Create(label, m_stylesheet, m_displayUsage);
				if (chosenObjs != null)
				{
					node.Checked = chosenObjs.Contains(label.Object);
				}
				Nodes.Add(node);
				AddSecondaryNodes(node, chosenObjs);
				if (recursively)
				{
					node.AddChildren(true, chosenObjs);
				}
			}
		}

		/// <summary>
		/// Wants the node for label.
		/// </summary>
		public virtual bool WantNodeForLabel(ObjectLabel label)
		{
			return true; // by default want all nodes.
		}

		/// <summary>
		/// Adds the secondary nodes.
		/// </summary>
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/LabelNode.cs
		public virtual void AddSecondaryNodes(LabelNode node, TreeNodeCollection nodes, IEnumerable<ICmObject> chosenObjs)
||||||| f013144d5:Src/Common/Controls/XMLViews/LabelNode.cs
		/// <param name="node">The node.</param>
		/// <param name="nodes">The nodes.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		public virtual void AddSecondaryNodes(LabelNode node, TreeNodeCollection nodes, IEnumerable<ICmObject> chosenObjs)
=======
		/// <param name="node">The node.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		public virtual void AddSecondaryNodes(LabelNode node, IEnumerable<ICmObject> chosenObjs)
>>>>>>> develop:Src/Common/Controls/XMLViews/LabelNode.cs
		{
			// default is to do nothing
		}

		/// <summary>
		/// Creates the specified LabelNode from the ObjectLabel.
		/// </summary>
		protected virtual LabelNode Create(ObjectLabel nol, IVwStylesheet stylesheet, bool displayUsage)
		{
			return new LabelNode(nol, stylesheet, displayUsage);
		}

		/// <summary>
		/// Add the children nodes of a particular node in the tree.
		/// While adding nodes if a match is found for objToSelect then nodeRepresentingCurrentChoice assigned to it
		/// and is returned. Otherwise if no node in the tree matches objToSelect this method returns null.
		/// </summary>
		public virtual LabelNode AddChildrenAndLookForSelected(ICmObject objToSelect, Stack<ICmObject> ownershipStack, IEnumerable<ICmObject> chosenObjs)
		{
			LabelNode nodeRepresentingCurrentChoice = null;
			// JohnT: if this.Nodes[0] is not a LabelNode, it is a dummy node we added so that
			// its parent LOOKS like something we can expand. That is the usual case for a node
			// we can expand. Therefore finding one of those, or finding more or less than one
			// node, is evidence that we haven't previously computed the real children of this,
			// and should do so.
			var fExpanded = Nodes.Count != 1 || Nodes[0] as LabelNode != null;
			if (!fExpanded)
			{
				Nodes.Clear();
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/LabelNode.cs
				nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(this, Nodes, nodeRepresentingCurrentChoice, objToSelect, ownershipStack, chosenObjs);
				foreach (var label in ((ObjectLabel)Tag).SubItems)
||||||| f013144d5:Src/Common/Controls/XMLViews/LabelNode.cs
				nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(this,
					Nodes, nodeRepresentingCurrentChoice, objToSelect, ownershipStack, chosenObjs);
				foreach (ObjectLabel label in ((ObjectLabel) Tag).SubItems)
=======
				nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(this,
					nodeRepresentingCurrentChoice, objToSelect, chosenObjs);
				foreach (ObjectLabel label in ((ObjectLabel) Tag).SubItems)
>>>>>>> develop:Src/Common/Controls/XMLViews/LabelNode.cs
				{
					if (!WantNodeForLabel(label))
					{
						continue;
					}
					var node = Create(label, m_stylesheet, m_displayUsage);
					if (chosenObjs != null)
					{
						node.Checked = chosenObjs.Contains(label.Object);
					}
					Nodes.Add(node);
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/LabelNode.cs
					nodeRepresentingCurrentChoice = CheckForSelection(label, objToSelect, node, nodeRepresentingCurrentChoice);
					nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(node, node.Nodes, nodeRepresentingCurrentChoice, objToSelect, ownershipStack, chosenObjs);
||||||| f013144d5:Src/Common/Controls/XMLViews/LabelNode.cs
					nodeRepresentingCurrentChoice = CheckForSelection(label, objToSelect,
						node, nodeRepresentingCurrentChoice);
					nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(
						node, node.Nodes, nodeRepresentingCurrentChoice, objToSelect,
						ownershipStack, chosenObjs);
=======
					nodeRepresentingCurrentChoice = CheckForSelection(label, objToSelect,
						node, nodeRepresentingCurrentChoice);
					nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(
						node, nodeRepresentingCurrentChoice, objToSelect,
						chosenObjs);
>>>>>>> develop:Src/Common/Controls/XMLViews/LabelNode.cs
				}
			}
			else
			{
				// Even if we don't have to create children for this, we need to search the
				// children for matches, and perhaps expand some of them.
				nodeRepresentingCurrentChoice = Nodes.Cast<LabelNode>().Aggregate(nodeRepresentingCurrentChoice, (current, node) => CheckForSelection(node.Label, objToSelect, node, current));
			}
			if (nodeRepresentingCurrentChoice == null)
			{
				foreach (LabelNode node in Nodes)
				{
					if (ownershipStack.Contains(node.Label.Object))
					{
						nodeRepresentingCurrentChoice = node.AddChildrenAndLookForSelected(objToSelect, ownershipStack, chosenObjs);
						return nodeRepresentingCurrentChoice;
					}
				}
			}
			else
			{
				Expand();
				nodeRepresentingCurrentChoice.EnsureVisible();
			}
			return nodeRepresentingCurrentChoice;
		}

		/// <summary>
		/// Add secondary nodes to tree (and check any that occur in rghvoChosen),
		/// and return the one whose hvo is hvoToSelect, or nodeRepresentingCurrentChoice
		/// if none match.
		/// </summary>
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/LabelNode.cs
		public virtual LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node, TreeNodeCollection nodes, LabelNode nodeRepresentingCurrentChoice, ICmObject objToSelect,
			Stack<ICmObject> ownershipStack, IEnumerable<ICmObject> chosenObjs)
||||||| f013144d5:Src/Common/Controls/XMLViews/LabelNode.cs
		/// <param name="node">node to be added</param>
		/// <param name="nodes">where to add it</param>
		/// <param name="nodeRepresentingCurrentChoice">The node representing current choice.</param>
		/// <param name="objToSelect">The obj to select.</param>
		/// <param name="ownershipStack">The ownership stack.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		/// <returns></returns>
		public virtual LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node,
			TreeNodeCollection nodes, LabelNode nodeRepresentingCurrentChoice,
			ICmObject objToSelect, Stack<ICmObject> ownershipStack, IEnumerable<ICmObject> chosenObjs)
=======
		/// <param name="node">node to be added</param>
		/// <param name="nodeRepresentingCurrentChoice">The node representing current choice.</param>
		/// <param name="objToSelect">The obj to select.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		/// <returns></returns>
		public virtual LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node,
			LabelNode nodeRepresentingCurrentChoice,
			ICmObject objToSelect, IEnumerable<ICmObject> chosenObjs)
>>>>>>> develop:Src/Common/Controls/XMLViews/LabelNode.cs
		{
			// default is to do nothing
			return nodeRepresentingCurrentChoice;
		}

		/// <summary>
		/// Checks for selection.
		/// </summary>
		protected virtual LabelNode CheckForSelection(ObjectLabel label, ICmObject objToSelect, LabelNode node, LabelNode nodeRepresentingCurrentChoice)
		{
			if (label.Object == objToSelect)        //make it look selected
			{
				nodeRepresentingCurrentChoice = node;
			}
			return nodeRepresentingCurrentChoice;
		}

		/// <summary>
		/// Get or set the enabled state of the label node
		/// </summary>
		public bool Enabled
		{
			get => m_fEnabled;
			set
			{
				if (m_fEnabled && !value)
				{
					m_enabledColor = ForeColor;
					ForeColor = SystemColors.GrayText;
				}
				else if (!m_fEnabled && value)
				{
					ForeColor = m_enabledColor;
				}
				m_fEnabled = value;
			}
		}
	}
}