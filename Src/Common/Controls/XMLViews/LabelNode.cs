using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Controls
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// This override to TreeNode handles the displaying of an ObjectLabel in a custom way
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class LabelNode : TreeNode
	{
		/// <summary></summary>
		protected IVwStylesheet m_stylesheet;

		private bool m_displayUsage;
		private bool m_fEnabled;
		private Color m_enabledColor;

		/// <summary>
		/// Return the basic string representing the label for this node.
		/// </summary>
		protected virtual string BasicNodeString { get { return Label.AsTss.Text; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="LabelNode"/> class.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="displayUsage"><c>true</c> if usage statistics will be displayed; otherwise, <c>false</c>.</param>
		public LabelNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage)
		{
			Tag = label;
			m_stylesheet = stylesheet;
			m_displayUsage = displayUsage;
			m_fEnabled = true;
			m_enabledColor = ForeColor;
			ITsString tssDisplay = label.AsTss;
			int wsVern;
			if (HasVernacularText(tssDisplay, label.Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(ws => ws.Handle),
								  out wsVern))
			{
				NodeFont = GetVernacularFont(label.Cache.WritingSystemFactory, wsVern, stylesheet);
			}
			SetNodeText();
			if (label.HaveSubItems)
				// this is a hack to make the node expandable before we have filled in any
				// actual children
				Nodes.Add(new TreeNode("should not see this"));
		}

		/// <summary>
		/// Gets or sets a value indicating whether to display the usage statistics.
		/// </summary>
		/// <value><c>true</c> if usage statistics will be displayed; otherwise, <c>false</c>.</value>
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
			ObjectLabel label = Label;
			string text = BasicNodeString;
			if (m_displayUsage)
			{
				// Don't count the reference from an overlay, since we have no way to tell
				// how many times that overlay has been used.  See FWR-1050.
				int count = 0;
				// I think only label.Object is likely to be null, but let's prevent crashes thoroughly.
				if (label != null && label.Object != null && label.Object.ReferringObjects != null)
				{
					count = label.Object.ReferringObjects.Count;
					foreach (ICmObject x in label.Object.ReferringObjects)
					{
						if (x is ICmOverlay)
							--count;
					}
				}
				if (count > 0)
					text += " (" + count + ")";
			}
			Text = text;
		}

		private static bool HasVernacularText(ITsString tss, IEnumerable<int> vernWses, out int wsVern)
		{
			wsVern = 0;
			int crun = tss.RunCount;
			for (int irun = 0; irun < crun; irun++)
			{
				ITsTextProps ttp = tss.get_Properties(irun);
				int nvar;
				int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
				if (vernWses.Any(vernWS => ws == vernWS))
				{
					wsVern = ws;
					return true;
				}
			}
			return false;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Resets the vernacular font.
		/// </summary>
		/// <param name="wsf">The WSF.</param>
		/// <param name="wsVern">The ws vern.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// --------------------------------------------------------------------------------
		public void ResetVernacularFont(ILgWritingSystemFactory wsf, int wsVern, IVwStylesheet stylesheet)
		{
			NodeFont = GetVernacularFont(wsf, wsVern, stylesheet);
		}

		private static Font GetVernacularFont(ILgWritingSystemFactory wsf, int wsVern, IVwStylesheet stylesheet)
		{
			if (stylesheet == null)
			{
				ILgWritingSystem wsEngine = wsf.get_EngineOrNull(wsVern);
				string fontName = wsEngine.DefaultFontName;
				return new Font(fontName, (float)10.0);
			}
			else
			{
				return FontHeightAdjuster.GetFontForNormalStyle(wsVern, stylesheet, wsf);
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the label.
		/// </summary>
		/// <value>The label.</value>
		/// --------------------------------------------------------------------------------
		public ObjectLabel Label
		{
			get
			{
				return (ObjectLabel) Tag;
			}
		}
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
					return;
				foreach (LabelNode node in Nodes)
					node.AddChildren(true, chosenObjs);
				return;
			}
			Nodes.Clear(); // get rid of the dummy.

			AddSecondaryNodes(this, Nodes, chosenObjs);
			foreach (ObjectLabel label in ((ObjectLabel)Tag).SubItems)
			{
				if (!WantNodeForLabel(label))
					continue;
				LabelNode node = Create(label, m_stylesheet, m_displayUsage);
				if (chosenObjs != null)
					node.Checked = chosenObjs.Contains(label.Object);
				Nodes.Add(node);
				AddSecondaryNodes(node, node.Nodes, chosenObjs);
				if (recursively)
				{
					node.AddChildren(true, chosenObjs);
				}
			}
		}
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Wants the node for label.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public virtual bool WantNodeForLabel(ObjectLabel label)
		{
			return true; // by default want all nodes.
		}
		/// <summary>
		/// Adds the secondary nodes.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="nodes">The nodes.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		public virtual void AddSecondaryNodes(LabelNode node, TreeNodeCollection nodes, IEnumerable<ICmObject> chosenObjs)
		{
			// default is to do nothing
		}

		/// <summary>
		/// Creates the specified LabelNode from the ObjectLabel.
		/// </summary>
		/// <param name="nol"></param>
		/// <param name="stylesheet"></param>
		/// <param name="displayUsage"><c>true</c> if usage statistics will be displayed; otherwise, <c>false</c>.</param>
		/// <returns></returns>
		protected virtual LabelNode Create(ObjectLabel nol, IVwStylesheet stylesheet, bool displayUsage)
		{
			return new LabelNode(nol, stylesheet, displayUsage);
		}

		/// <summary>
		/// Add the children nodes of a particular node in the tree.
		/// While adding nodes if a match is found for objToSelect then nodeRepresentingCurrentChoice assigned to it
		/// and is returned. Otherwise if no node in the tree matches objToSelect this method returns null.
		/// </summary>
		public virtual LabelNode AddChildrenAndLookForSelected (ICmObject objToSelect,
			Stack<ICmObject> ownershipStack, IEnumerable<ICmObject> chosenObjs)
		{
			LabelNode nodeRepresentingCurrentChoice = null;
			// JohnT: if this.Nodes[0] is not a LabelNode, it is a dummy node we added so that
			// its parent LOOKS like something we can expand. That is the usual case for a node
			// we can expand. Therefore finding one of those, or finding more or less than one
			// node, is evidence that we haven't previously computed the real children of this,
			// and should do so.
			bool fExpanded = Nodes.Count != 1 || (Nodes[0] as LabelNode) != null;
			if (!fExpanded)
			{
				Nodes.Clear();
				nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(this,
					Nodes, nodeRepresentingCurrentChoice, objToSelect, ownershipStack, chosenObjs);
				foreach (ObjectLabel label in ((ObjectLabel) Tag).SubItems)
				{
					if (!WantNodeForLabel(label))
						continue;
					LabelNode node = Create(label, m_stylesheet, m_displayUsage);
					if (chosenObjs != null)
						node.Checked = chosenObjs.Contains(label.Object);
					Nodes.Add(node);
					nodeRepresentingCurrentChoice = CheckForSelection(label, objToSelect,
						node, nodeRepresentingCurrentChoice);
					nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(
						node, node.Nodes, nodeRepresentingCurrentChoice, objToSelect,
						ownershipStack, chosenObjs);
				}
			}
			else
			{
				// Even if we don't have to create children for this, we need to search the
				// children for matches, and perhaps expand some of them.
				foreach (LabelNode node in Nodes)
				{
					nodeRepresentingCurrentChoice = CheckForSelection(node.Label,
						objToSelect, node, nodeRepresentingCurrentChoice);
				}
			}
			if (nodeRepresentingCurrentChoice == null)
			{
				foreach (LabelNode node in Nodes)
				{
					if (ownershipStack.Contains(node.Label.Object))
					{
						nodeRepresentingCurrentChoice =	node.AddChildrenAndLookForSelected(
							objToSelect, ownershipStack, chosenObjs);
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
		{
			// default is to do nothing
			return nodeRepresentingCurrentChoice;
		}
		/// <summary>
		/// Checks for selection.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <param name="objToSelect">The obj to select.</param>
		/// <param name="node">The node.</param>
		/// <param name="nodeRepresentingCurrentChoice">The node representing current choice.</param>
		/// <returns></returns>
		protected virtual LabelNode CheckForSelection(ObjectLabel label, ICmObject objToSelect,
			LabelNode node, LabelNode nodeRepresentingCurrentChoice)
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