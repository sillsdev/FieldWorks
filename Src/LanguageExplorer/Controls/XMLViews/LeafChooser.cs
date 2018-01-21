// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// A LeafChooser means we are trying to choose items at the leaves of a hierarchy.
	/// The prototypical case is choosing Inflection classes. So there is a tree of CmPossibilities,
	/// (actually PartOfSpeechs) any of which may have leaves in the InflectionClasses property.
	/// We want to display only the possibilities that have inflection classes (either themselves
	/// or some descendant), plus the inflection classes themselves.
	/// </summary>
	public class LeafChooser : ReallySimpleListChooser
	{
		private readonly int m_leafFlid;
		/// <summary>
		/// Initializes a new instance of the <see cref="LeafChooser"/> class.
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		/// <param name="leafFlid">The leaf flid.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		public LeafChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, LcmCache cache,
			IEnumerable<ICmObject> chosenObjs, int leafFlid, IHelpTopicProvider helpTopicProvider)
			: base (persistProvider, fieldName, cache, chosenObjs, helpTopicProvider)
		{
			m_leafFlid = leafFlid;

			// Normally done by the base class constructor, but requires m_leafFlid to be set, so
			// we made a special constructor to finesse things.
			FinishConstructor(labels);
		}

		/// <summary>
		/// Creates the label node.
		/// </summary>
		protected override LabelNode CreateLabelNode(ObjectLabel nol, bool displayUsage)
		{
			return new LeafLabelNode(nol, m_stylesheet, displayUsage, m_leafFlid);
		}
		/// <summary>
		/// In this class we want only those nodes that have interesting leaves somewhere.
		/// Unfortunately this method is duplicated on LeafLabelNode. I can't see a clean way to
		/// avoid this.
		/// </summary>
		public override bool WantNodeForLabel(ObjectLabel label)
		{
			CheckDisposed();

			if (!base.WantNodeForLabel(label)) // currently does nothing, but just in case...
			{
				return false;
			}

			if (HasLeaves(label))
			{
				return true;
			}
			foreach (var labelSub in label.SubItems)
			{
				if (WantNodeForLabel(labelSub))
				{
					return true;
				}
			}
			return false;
		}
		private bool HasLeaves(ObjectLabel label)
		{
			return label.Cache.DomainDataByFlid.get_VecSize(label.Object.Hvo, m_leafFlid) > 0;
		}

		/// <summary />
		protected class LeafLabelNode : LabelNode
		{
			private readonly int m_leafFlid;

			/// <summary>
			/// Initializes a new instance of the <see cref="LeafLabelNode"/> class.
			/// </summary>
			/// <param name="label">The label.</param>
			/// <param name="stylesheet">The stylesheet.</param>
			/// <param name="displayUsage"><c>true</c> if usage statistics will be displayed; otherwise, <c>false</c>.</param>
			/// <param name="leafFlid">The leaf flid.</param>
			public LeafLabelNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage, int leafFlid)
				: base(label, stylesheet, displayUsage)
			{
				m_leafFlid = leafFlid;
			}

			/// <summary>
			/// Adds the secondary nodes.
			/// </summary>
			public override void AddSecondaryNodes(LabelNode node, TreeNodeCollection nodes, IEnumerable<ICmObject> chosenObjs)
			{
				AddSecondaryNodesAndLookForSelected(node, nodes, null, null, null, chosenObjs);
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
			public override LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node, TreeNodeCollection nodes,
				LabelNode nodeRepresentingCurrentChoice, ICmObject objToSelect, Stack<ICmObject> ownershipStack, IEnumerable<ICmObject> chosenObjs)
			{
				LabelNode result = nodeRepresentingCurrentChoice; // result unless we match hvoToSelect
				var label = (ObjectLabel) Tag;
				var sda = label.Cache.GetManagedSilDataAccess();
				var objs = from hvo in sda.VecProp(label.Object.Hvo, m_leafFlid)
					select label.Cache.ServiceLocator.GetObject((int) hvo);
				var secLabels = ObjectLabel.CreateObjectLabels(label.Cache, objs,
					"ShortNameTSS", "analysis vernacular"); // Enhance JohnT: may want to make these configurable one day...
				foreach (ObjectLabel secLabel in secLabels)
				{
					// Perversely, we do NOT want a LeafLabelNode for the leaves, because their HVOS are the leaf type,
					// and therefore objects that do NOT possess the leaf property!
					var secNode = new LabelNode(secLabel, m_stylesheet, true);
					if (chosenObjs != null)
					{
						secNode.Checked = chosenObjs.Contains(secLabel.Object);
					}
					node.Nodes.Add(secNode);
					if (secLabel.Object == objToSelect)
					{
						result = secNode;
					}
				}
				return result;
			}

			/// <summary>
			/// In this class we want only those nodes that have interesting leaves somewhere.
			/// Unfortunately this method is duplicated on LeafChooser. I can't see a clean way to
			/// avoid this.
			/// </summary>
			public override bool WantNodeForLabel(ObjectLabel label)
			{
				if (!base.WantNodeForLabel(label)) // currently does nothing, but just in case...
				{
					return false;
				}

				if (HasLeaves(label))
				{
					return true;
				}
				foreach (var labelSub in label.SubItems)
				{
					if (WantNodeForLabel(labelSub))
					{
						return true;
					}
				}
				return false;
			}

			private bool HasLeaves(ObjectLabel label)
			{
				return label.Cache.DomainDataByFlid.get_VecSize(label.Object.Hvo, m_leafFlid) > 0;
			}
		}
	}
}