// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;

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

		/// <summary>
		/// Initializes a new instance of the <see cref="LabelNode"/> class.
		/// </summary>
		public LabelNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage)
		{
			Tag = label;
			m_stylesheet = stylesheet;
			m_displayUsage = displayUsage;
			m_fEnabled = true;
			m_enabledColor = ForeColor;
			var tssDisplay = label.AsTss;
			int wsVern;
			if (HasVernacularText(tssDisplay, label.Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(ws => ws.Handle), out wsVern))
			{
				NodeFont = GetVernacularFont(label.Cache.WritingSystemFactory, wsVern, stylesheet);
			}
			SetNodeText();
			if (label.HaveSubItems)
			{
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
			get
			{
				return m_displayUsage;
			}

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
					text += " (" + count + ")";
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
				int nvar;
				var ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
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

			AddSecondaryNodes(this, Nodes, chosenObjs);
			foreach (var label in ((ObjectLabel)Tag).SubItems)
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
				AddSecondaryNodes(node, node.Nodes, chosenObjs);
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
		public virtual void AddSecondaryNodes(LabelNode node, TreeNodeCollection nodes, IEnumerable<ICmObject> chosenObjs)
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
		public virtual LabelNode AddChildrenAndLookForSelected (ICmObject objToSelect, Stack<ICmObject> ownershipStack, IEnumerable<ICmObject> chosenObjs)
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
				nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(this, Nodes, nodeRepresentingCurrentChoice, objToSelect, ownershipStack, chosenObjs);
				foreach (var label in ((ObjectLabel) Tag).SubItems)
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
					nodeRepresentingCurrentChoice = CheckForSelection(label, objToSelect, node, nodeRepresentingCurrentChoice);
					nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(node, node.Nodes, nodeRepresentingCurrentChoice, objToSelect, ownershipStack, chosenObjs);
				}
			}
			else
			{
				// Even if we don't have to create children for this, we need to search the
				// children for matches, and perhaps expand some of them.
				foreach (LabelNode node in Nodes)
				{
					nodeRepresentingCurrentChoice = CheckForSelection(node.Label, objToSelect, node, nodeRepresentingCurrentChoice);
				}
			}
			if (nodeRepresentingCurrentChoice == null)
			{
				foreach (LabelNode node in Nodes)
				{
					if (ownershipStack.Contains(node.Label.Object))
					{
						nodeRepresentingCurrentChoice =	node.AddChildrenAndLookForSelected(objToSelect, ownershipStack, chosenObjs);
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
		/// Add secondary nodes to tree at nodes (and check any that occur in rghvoChosen),
		/// and return the one whose hvo is hvoToSelect, or nodeRepresentingCurrentChoice
		/// if none match.
		/// </summary>
		public virtual LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node,
			TreeNodeCollection nodes, LabelNode nodeRepresentingCurrentChoice,
			ICmObject objToSelect, Stack<ICmObject> ownershipStack, IEnumerable<ICmObject> chosenObjs)
		{
			// default is to do nothing
			return nodeRepresentingCurrentChoice;
		}

		/// <summary>
		/// Checks for selection.
		/// </summary>
		protected virtual LabelNode CheckForSelection(ObjectLabel label, ICmObject objToSelect, LabelNode node, LabelNode nodeRepresentingCurrentChoice)
		{
			if (label.Object == objToSelect)		//make it look selected
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
			get { return m_fEnabled; }
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