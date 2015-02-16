using System.Collections.Generic;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Xml;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// A layout merger is used to merge a current version layout (newMaster) with an override that is an older version.
	/// Merging takes place broadly by copying the newMaster version into a new XmlNode in a specified document.
	/// Currently most elements are copied unmodified. However, part refs are treated specially:
	/// Each consecutive sequence of part nodes is adjusted in the following ways:
	///     - order is made to conform to the order in the override, for any elements found in the override.
	///     - any elements not found in the override are copied, in the current sequence starting from the previous
	///         element that IS matched.
	///     - (elements not matched in the override are discarded, except that)
	///     - "$child" elements in the override are copied to the output
	///     - a specified set of attributes may also be overridden.
	///     - also changes have been made (17 June 2013) to handle copied views and duplicated nodes. The latter,
	///          especially, modify the way nodes are matched so that nodes which have a 'dup' attribute are
	///          still copied over, in a way that upgrades them to match the newMaster as much as possible while
	///          still preserving the user's configuration changes.
	/// </summary>
	public class LayoutMerger : IOldVersionMerger
	{
		private XmlDocument m_dest;
		private XmlNode m_newMaster;
		private XmlNode m_oldConfigured;
		private XmlElement m_output;
		/// <summary>
		/// If present, this is the stuff after (and including) the # in the 'oldConfigured' argument to Merge,
		/// assumed to be common to all relevant parts of 'oldConfigured' (that have a 'param' attribute)
		/// and to be duplicated to all relevant parts of the output.
		/// </summary>
		private string m_layoutParamAttrSuffix;
		/// <summary>
		/// Here we store stuff added to a 'param' attribute in an 'oldConfigured' part ref (not the main
		/// layout node) because that node is a duplicate, so that when the newMaster version is copied over
		/// we can add it back into the new 'param' attribute. The key is the 'ref' attribute's value + the
		/// 'dup' attribute's value (e.g. key='LexReferencesConfig1', value='%01').
		/// </summary>
		private Dictionary<string, string> m_partLevelParamAttrSuffix;
		/// <summary>
		/// Similar to the above, except in this case we store the stuff added to the 'label' attribute to
		/// mark a duplicate. In the above example we would have key='LexReferencesConfig1', value=' (1)'.
		/// </summary>
		private Dictionary<string, string> m_labelAttrSuffix;
		/// <summary>
		/// A set of keys of child elements in 'oldConfigured', that we have already copied.
		/// The significance is that if there are new children in the patterns (ones that don't match at all),
		/// we include them in the output, immediately following the previous node that DOES match.
		/// But we only want to do that ONCE, not for every duplicate of the preceding match node.
		/// </summary>
		private Dictionary<string, bool> m_oldPartsFound;
		private Set<XmlNode> m_insertedMissing; // missing nodes we already inserted.
		Set<string> m_safeAttrs = new Set<string>(
			new string[] { "before", "after", "sep", "ws", "style", "showLabels", "number", "numstyle", "numsingle", "visibility",
				"singlegraminfofirst", "showasindentedpara", "reltypeseq", "dup" });

		private const string NameAttr = "name";
		private const string LabelAttr = "label";
		private const string ParamAttr = "param";
		private const string RefAttr = "ref";
		private const string ChildStr = "$child";
		private const string DupAttr = "dup";
		private const string PartNode = "part";

		/// <summary>
		/// This is the main entry point.
		/// </summary>
		public XmlNode Merge(XmlNode newMaster, XmlNode oldConfigured, XmlDocument dest, string oldLayoutSuffix)
		{
			// As a result of LT-14650, certain bits of logic regarding copied views and duplicated nodes
			// were brought inside of the Merge.
			m_newMaster = newMaster;
			m_oldConfigured = oldConfigured;
			m_dest = dest;
			m_insertedMissing = new Set<XmlNode>();
			m_output = m_dest.CreateElement(newMaster.Name);
			m_layoutParamAttrSuffix = oldLayoutSuffix;
			CopyAttributes(m_newMaster, m_output);
			int startIndex = 0;
			BuildOldConfiguredPartsDicts();
			while (startIndex < m_newMaster.ChildNodes.Count)
			{
				XmlNode currentChild = m_newMaster.ChildNodes[startIndex];
				if (IsMergeableNode(currentChild))
				{
					int limIndex = startIndex + 1;
					while (limIndex < m_newMaster.ChildNodes.Count && m_newMaster.ChildNodes[limIndex].Name == currentChild.Name)
						limIndex++;
					CopyParts(startIndex, limIndex);
					startIndex = limIndex;
				}
				else
				{
					CopyToOutput(currentChild);
					startIndex++;
				}
			}
			return m_output;
		}

		private XmlNode CopyToOutput(XmlNode source)
		{
			XmlNode newNode = m_dest.ImportNode(source, true);
			m_output.AppendChild(newNode);
			return newNode;
		}

		private void FixUpPartRefParamAttrForDupNode(XmlNode partRefNode, string dupKey)
		{
			if (string.IsNullOrEmpty(partRefNode.GetOptionalStringAttribute(ParamAttr, string.Empty)))
				return; // nothing to do

			var xaParam = partRefNode.Attributes[ParamAttr];
			string suffix;
			if (m_partLevelParamAttrSuffix.TryGetValue(dupKey, out suffix))
			{
				xaParam.Value = xaParam.Value + suffix;
			}
		}

		private void FixUpPartRefLabelAttrForDupNode(XmlNode partRefNode, string dupKey)
		{
			if (string.IsNullOrEmpty(partRefNode.GetOptionalStringAttribute(LabelAttr, string.Empty)))
				return; // nothing to do

			var xaLabel = partRefNode.Attributes[LabelAttr];
			string suffix;
			if (m_labelAttrSuffix.TryGetValue(dupKey, out suffix))
			{
				xaLabel.Value = xaLabel.Value + suffix;
			}
		}

		private bool IsMergeableNode(XmlNode currentChild)
		{
			return currentChild.Name == PartNode;
		}

		// For most parts, the key is the ref attribute, but if that is $child, it's a custom field and
		// the label is the best way to distinguish. This key is used to match elements between
		// newMaster and oldConfigured nodes.
		private string GetKey(XmlNode node)
		{
			var key = Utils.XmlUtils.GetOptionalAttributeValue(node, RefAttr, string.Empty);
			if (key == ChildStr)
				key = Utils.XmlUtils.GetOptionalAttributeValue(node, LabelAttr, ChildStr);
			return key;
		}

		// For most parts, the key is the ref attribute, but if that is $child, it's a custom field and
		// the label is the best way to distinguish. On the other hand, we need to be able to distinguish
		// parts that have been duplicated too, so we have a second sort of key that also uses the dup attribute.
		// This allows us to compare different oldConfigured nodes with the relevant newMaster node to see
		// if this one is a duplicate node.
		private string GetKeyWithDup(XmlNode node)
		{
			var key = Utils.XmlUtils.GetOptionalAttributeValue(node, RefAttr, string.Empty);
			if (key == ChildStr)
				key = Utils.XmlUtils.GetOptionalAttributeValue(node, LabelAttr, ChildStr);
			var dup = Utils.XmlUtils.GetOptionalAttributeValue(node, DupAttr, string.Empty);
			return key + dup;
		}

		private void BuildOldConfiguredPartsDicts()
		{
			m_partLevelParamAttrSuffix = new Dictionary<string, string>();
			m_labelAttrSuffix = new Dictionary<string, string>();
			m_oldPartsFound = new Dictionary<string, bool>();
			foreach (XmlNode child in m_oldConfigured)
			{
				if (!IsMergeableNode(child))
					continue;
				var baseKey = GetKey(child);
				m_oldPartsFound[baseKey] = false;
				var dupKey = GetKeyWithDup(child);
				if (dupKey == baseKey)
					continue;
				m_labelAttrSuffix.Add(dupKey, LayoutKeyUtils.GetPossibleLabelSuffix(child));
				m_partLevelParamAttrSuffix.Add(dupKey, LayoutKeyUtils.GetPossibleParamSuffix(child));
			}
		}

		// Answer true if we want to insert a copy of an output node even though there isn't a match in the current
		// input range. Currently this requires both that it has a ref of $child and the key doesn't match ANY part node in the input.
		bool WantToCopyMissingItem(XmlNode node)
		{
			if (Utils.XmlUtils.GetOptionalAttributeValue(node, RefAttr, string.Empty) != ChildStr)
				return false;
			string key = GetKey(node);
			if (m_insertedMissing.Contains(node))
				return false; // don't insert twice!
			foreach (XmlNode child in m_newMaster.ChildNodes)
				if (IsMergeableNode(child) && GetKey(child) == key)
					return false;
			return true;
		}

		private void CopyParts(int startIndex, int limIndex)
		{
			// Copy initial items not in oldConfigured
			int indexOfFirstNodeWanted = CopyNodesNotInOldConfigured(startIndex, limIndex);
			foreach (XmlNode oldNode in m_oldConfigured)
			{
				CopyStuffWantedForNewNode(oldNode, indexOfFirstNodeWanted, limIndex);
			}
		}

		// Copy whatever we ought to for the output node wanted from the input range specified.
		// If we find a node with the same key in the range, copy it, and if the input range
		// contains following nodes not in output and this is the first copy of the output key, copy them too.
		// If we don't find an input node to copy, we might copy the output.
		private void CopyStuffWantedForNewNode(XmlNode oldNode, int indexOfFirstNodeWanted, int limIndex)
		{
			string key = GetKey(oldNode);
			for (int index = indexOfFirstNodeWanted; index < limIndex; index++)
			{
				XmlNode child = m_newMaster.ChildNodes[index];
				if (GetKey(child) == key)
				{
					XmlNode copy = CopyToOutput(child);
					CopySafeAttrs(copy, oldNode);
					var dupKey = GetKeyWithDup(oldNode);
					if (dupKey != key)
					{
						// This duplicate may have suffixes to attach
						ReattachDupSuffixes(copy, dupKey);
					}
					else
					{
						CheckForAndReattachLayoutParamSuffix(copy);
					}
					if (!m_oldPartsFound[key])
					{
						m_oldPartsFound[key] = true; // copy new following nodes only once, not for duplicates.
						CopyNodesNotInOldConfigured(index + 1, limIndex);
					}
					return;
				}
			}
			if (WantToCopyMissingItem(oldNode))
			{
				CopyToOutput(oldNode);
				m_insertedMissing.Add(oldNode);
			}
		}

		private void CheckForAndReattachLayoutParamSuffix(XmlNode workingNode)
		{
			if (string.IsNullOrEmpty(m_layoutParamAttrSuffix) ||
				string.IsNullOrEmpty(workingNode.GetOptionalStringAttribute(ParamAttr, string.Empty)))
				return; // nothing to do

			var xaParam = workingNode.Attributes[ParamAttr];
			xaParam.Value = xaParam.Value + m_layoutParamAttrSuffix;
		}

		private void ReattachDupSuffixes(XmlNode copy, string dupKey)
		{
			FixUpPartRefParamAttrForDupNode(copy, dupKey);
			FixUpPartRefLabelAttrForDupNode(copy, dupKey);
		}

		private void CopySafeAttrs(XmlNode copy, XmlNode oldConfiguredPartRef)
		{
			// Ignore comments
			if (oldConfiguredPartRef.Attributes == null)
				return;
			foreach (XmlAttribute xa in oldConfiguredPartRef.Attributes)
				if (m_safeAttrs.Contains(xa.Name))
					Utils.XmlUtils.SetAttribute(copy, xa.Name, xa.Value);
		}

		/// <summary>
		/// Copy nodes from index to limIndex to output, stopping if a node is found
		/// that has a key in m_oldPartsFound. Return the index of the next (uncopied) node,
		/// either limIndex or the index of the node with a key we still want.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="limIndex"></param>
		/// <returns></returns>
		private int CopyNodesNotInOldConfigured(int index, int limIndex)
		{
			while (index < limIndex && !m_oldPartsFound.ContainsKey(GetKey(m_newMaster.ChildNodes[index])))
			{
				XmlNode copy = CopyToOutput(m_newMaster.ChildNodes[index]);
				// Susanna said to preserve the visibility of nodes not in the old version.
				// If we want to hide them to make the new view more like the 'oldConfigured' one, this is where to do it.
				//XmlUtils.SetAttribute(copy, "visibility", "never");
				CheckForAndReattachLayoutParamSuffix(copy);
				index++;
			}
			return index;
		}

		private void CopyAttributes(XmlNode source, XmlNode dest)
		{
			// Copy all layout attributes from the standard pattern to the output
			foreach (XmlAttribute attr in source.Attributes)
			{
				var xa = m_dest.CreateAttribute(attr.Name);
				dest.Attributes.Append(xa);
				xa.Value = attr.Value;
				if (attr.Name == NameAttr && !string.IsNullOrEmpty(m_layoutParamAttrSuffix))
				{
					// This suffix also attaches to the 'name' attribute of the layout itself
					xa.Value = attr.Value + m_layoutParamAttrSuffix;
				}
			}
		}

	}
}
