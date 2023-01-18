// Copyright (c) 2003-2020 SIL International
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
	internal class LeafChooser : ReallySimpleListChooser
	{
		private readonly int m_leafFlid;

		/// <summary />
		public LeafChooser(IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels, string fieldName, LcmCache cache, IEnumerable<ICmObject> chosenObjs, int leafFlid, IHelpTopicProvider helpTopicProvider)
			: base(persistProvider, fieldName, cache, chosenObjs, helpTopicProvider)
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
		/// Unfortunately this method is duplicated on LeafChooser. I can't see a clean way to
		/// avoid this.
		/// </summary>
		public override bool WantNodeForLabel(ObjectLabel label)
		{
			return base.WantNodeForLabel(label) && (HasLeaves(label) || label.SubItems.Any(labelSub => WantNodeForLabel(labelSub)));
		}
		private bool HasLeaves(ObjectLabel label)
		{
			return label.Cache.DomainDataByFlid.get_VecSize(label.Object.Hvo, m_leafFlid) > 0;
		}

		/// <summary />
		private sealed class LeafLabelNode : LabelNode
		{
			private readonly int m_leafFlid;

			/// <summary />
			public LeafLabelNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage, int leafFlid)
				: base(label, stylesheet, displayUsage, false)
			{
				m_leafFlid = leafFlid;

				bool haveSubItems = false;
				// Inflection Classes
				if (m_leafFlid == PartOfSpeechTags.kflidInflectionClasses &&
					label is CmPossibilityLabel cmPoss &&
					cmPoss.Possibility is IPartOfSpeech partOfSpeech)
					haveSubItems = partOfSpeech.InflectionClassesOC.Count > 0;
				// Other Classes
				else
					haveSubItems = label.HaveSubItems;

				if (haveSubItems)
				{
					// this is a hack to make the node expandable before we have filled in any
					// actual children
					Nodes.Add(new TreeNode("should not see this"));
				}
			}

			/// <summary>
			/// Adds the secondary nodes.
			/// </summary>
			public override void AddSecondaryNodes(LabelNode node, IEnumerable<ICmObject> chosenObjs)
			{
				AddSecondaryNodesAndLookForSelected(node, null, null, chosenObjs);
			}

			/// <summary>
			/// Add secondary nodes to tree at nodes (and check any that occur in rghvoChosen),
			/// and return the one whose hvo is hvoToSelect, or nodeRepresentingCurrentChoice
			/// if none match.
			/// </summary>
			public override LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node,
				LabelNode nodeRepresentingCurrentChoice, ICmObject objToSelect, IEnumerable<ICmObject> chosenObjs)
			{
				var result = nodeRepresentingCurrentChoice; // result unless we match hvoToSelect
				var label = (ObjectLabel)Tag;
				var sda = label.Cache.GetManagedSilDataAccess();
				var objs = sda.VecProp(label.Object.Hvo, m_leafFlid).Select(hvo => label.Cache.ServiceLocator.GetObject(hvo));
				var secLabels = ObjectLabel.CreateObjectLabels(label.Cache, objs, "ShortNameTSS", "analysis vernacular"); // Enhance JohnT: may want to make these configurable one day...
				foreach (var secLabel in secLabels)
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
		}
	}
}