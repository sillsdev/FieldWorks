// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
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
		private XElement m_newMaster;
		private XElement m_oldConfigured;
		private XElement m_output;
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
		private HashSet<XElement> m_insertedMissing; // missing nodes we already inserted.
		private readonly HashSet<string> m_safeAttrs = new HashSet<string>
		{
			"before", "after", "sep", "ws", "style", "showLabels", "number", "numstyle", "numsingle", "visibility",
			"singlegraminfofirst", "showasindentedpara", "reltypeseq", "dup", "entrytypeseq", "flowType"
		};
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
		public XElement Merge(XElement newMaster, XElement oldConfigured, XDocument dest, string oldLayoutSuffix)
		{
			// As a result of LT-14650, certain bits of logic regarding copied views and duplicated nodes
			// were brought inside of the Merge.
			m_newMaster = newMaster;
			m_oldConfigured = oldConfigured;
			m_insertedMissing = new HashSet<XElement>();
			m_output = new XElement(newMaster.Name);
			m_layoutParamAttrSuffix = oldLayoutSuffix;
			CopyAttributes(m_newMaster, m_output);
			var startIndex = 0;
			BuildOldConfiguredPartsDicts();
			var newMasterChildElements = m_newMaster.Elements().ToList();
			var oldConfiguredElements = m_oldConfigured.Elements().ToList();
			while (startIndex < newMasterChildElements.Count)
			{
				var currentChild = newMasterChildElements[startIndex];
				if (IsMergeableNode(currentChild))
				{
					var limIndex = startIndex + 1;
					while (limIndex < newMasterChildElements.Count && newMasterChildElements[limIndex].Name == currentChild.Name)
					{
						limIndex++;
					}
					CopyParts(startIndex, limIndex);
					startIndex = limIndex;
				}
				else
				{
					var copy = CopyToOutput(currentChild);
					// We need to merge sublayout nodes as well if at all possible.
					if (currentChild.Name == "sublayout" && m_oldConfigured.Elements().Count() > startIndex)
					{
						var currentOldChild = oldConfiguredElements[startIndex];
						if (currentOldChild.Name == currentChild.Name && currentOldChild.Attributes().Any())
						{
							foreach (var xa in currentOldChild.Attributes())
							{
								if (m_safeAttrs.Contains(xa.Name.LocalName) || xa.Name == "name" || xa.Name == "group")
								{
									XmlUtils.SetAttribute(copy, xa.Name.LocalName, xa.Value);
								}
							}
						}
					}
					startIndex++;
				}
			}
			return m_output;
		}

		private XElement CopyToOutput(XElement source)
		{
			var newNode = source.Clone();
			m_output.Add(newNode);
			return newNode;
		}

		private void FixUpPartRefParamAttrForDupNode(XElement partRefNode, string dupKey)
		{
			if (string.IsNullOrEmpty(XmlUtils.GetOptionalAttributeValue(partRefNode, ParamAttr, string.Empty)))
			{
				return; // nothing to do
			}
			var xaParam = partRefNode.Attribute(ParamAttr);
			string suffix;
			if (m_partLevelParamAttrSuffix.TryGetValue(dupKey, out suffix))
			{
				xaParam.Value = xaParam.Value + suffix;
			}
		}

		private void FixUpPartRefLabelAttrForDupNode(XElement partRefNode, string dupKey)
		{
			if (string.IsNullOrEmpty(XmlUtils.GetOptionalAttributeValue(partRefNode, LabelAttr, string.Empty)))
			{
				return; // nothing to do
			}
			var xaLabel = partRefNode.Attribute(LabelAttr);
			string suffix;
			if (m_labelAttrSuffix.TryGetValue(dupKey, out suffix))
			{
				xaLabel.Value = xaLabel.Value + suffix;
			}
		}

		private static bool IsMergeableNode(XElement currentChild)
		{
			return currentChild.Name == PartNode;
		}

		// For most parts, the key is the ref attribute, but if that is $child, it's a custom field and
		// the label is the best way to distinguish. This key is used to match elements between
		// newMaster and oldConfigured nodes.
		private static string GetKey(XElement node)
		{
			var key = XmlUtils.GetOptionalAttributeValue(node, RefAttr, string.Empty);
			if (key == ChildStr)
			{
				key = XmlUtils.GetOptionalAttributeValue(node, LabelAttr, ChildStr);
			}
			return key;
		}

		/// <summary>
		/// For most parts, the key is the ref attribute, but if that is $child, it's a custom field and
		/// the label is the best way to distinguish. On the other hand, we need to be able to distinguish
		/// parts that have been duplicated too, so we have a second sort of key that also uses the dup attribute.
		/// This allows us to compare different oldConfigured nodes with the relevant newMaster node to see
		/// if this one is a duplicate node.
		/// </summary>
		private string GetKeyWithDup(XElement node, bool isInitializing)
		{
			var key = XmlUtils.GetOptionalAttributeValue(node, RefAttr, string.Empty);
			if (key == ChildStr)
			{
				key = XmlUtils.GetOptionalAttributeValue(node, LabelAttr, ChildStr);
			}
			var dup = XmlUtils.GetOptionalAttributeValue(node, DupAttr, string.Empty);
			if (!isInitializing || !m_labelAttrSuffix.ContainsKey(key + dup))
			{
				return key + dup;
			}
			if (!dup.Contains("."))
			{
				return key + dup;
			}
			//numIncr value are getting from the label attribute text which are between the paranthesis
			var labelKey = XmlUtils.GetOptionalAttributeValue(node, LabelAttr, ChildStr);
			var numIncr = Regex.Match(labelKey, @"\(([^)]*)\)").Groups[1].Value;
			dup = string.Join(".", dup + "-" + numIncr);
			//Updating dup value in node attribute
			if (node.Attributes().Any())
			{
				node.Attribute("dup").SetValue(dup);
			}
			return key + dup;
		}

		private void BuildOldConfiguredPartsDicts()
		{
			m_partLevelParamAttrSuffix = new Dictionary<string, string>();
			m_labelAttrSuffix = new Dictionary<string, string>();
			m_oldPartsFound = new Dictionary<string, bool>();
			foreach (var child in m_oldConfigured.Elements())
			{
				if (!IsMergeableNode(child))
				{
					continue;
				}
				var baseKey = GetKey(child);
				m_oldPartsFound[baseKey] = false;
				var dupKey = GetKeyWithDup(child, true);
				if (dupKey == baseKey)
				{
					continue;
				}
				// Due to an old bug some configurations have bad data with indistinguishable duplicate nodes. Just drop the extra ones.
				if (!m_labelAttrSuffix.ContainsKey(dupKey))
				{
					m_labelAttrSuffix.Add(dupKey, LayoutKeyUtils.GetPossibleLabelSuffix(child));
					m_partLevelParamAttrSuffix.Add(dupKey, LayoutKeyUtils.GetPossibleParamSuffix(child));
				}
			}
		}

		// Answer true if we want to insert a copy of an output node even though there isn't a match in the current
		// input range. Currently this requires both that it has a ref of $child and the key doesn't match ANY part node in the input.
		private bool WantToCopyMissingItem(XElement node)
		{
			if (XmlUtils.GetOptionalAttributeValue(node, RefAttr, string.Empty) != ChildStr)
			{
				return false;
			}
			var key = GetKey(node);
			if (m_insertedMissing.Contains(node))
			{
				return false; // don't insert twice!
			}
			foreach (var child in m_newMaster.Elements())
			{
				if (IsMergeableNode(child) && GetKey(child) == key)
				{
					return false;
				}
			}
			return true;
		}

		private void CopyParts(int startIndex, int limIndex)
		{
			// Copy initial items not in oldConfigured
			var indexOfFirstNodeWanted = CopyNodesNotInOldConfigured(startIndex, limIndex);
			foreach (var oldNode in m_oldConfigured.Elements())
			{
				CopyStuffWantedForNewNode(oldNode, indexOfFirstNodeWanted, limIndex);
			}
		}

		// Copy whatever we ought to for the output node wanted from the input range specified.
		// If we find a node with the same key in the range, copy it, and if the input range
		// contains following nodes not in output and this is the first copy of the output key, copy them too.
		// If we don't find an input node to copy, we might copy the output.
		private void CopyStuffWantedForNewNode(XElement oldNode, int indexOfFirstNodeWanted, int limIndex)
		{
			var key = GetKey(oldNode);
			var newMasterChildElements = m_newMaster.Elements().ToList();
			for (var index = indexOfFirstNodeWanted; index < limIndex; index++)
			{
				var child = newMasterChildElements[index];
				if (GetKey(child) != key)
				{
					continue;
				}
				var copy = CopyToOutput(child);
				CopySafeAttrs(copy, oldNode);
				var dupKey = GetKeyWithDup(oldNode, false);
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
			if (WantToCopyMissingItem(oldNode))
			{
				CopyToOutput(oldNode);
				m_insertedMissing.Add(oldNode);
			}
		}

		private void CheckForAndReattachLayoutParamSuffix(XElement workingNode)
		{
			if (string.IsNullOrEmpty(m_layoutParamAttrSuffix) || string.IsNullOrEmpty(XmlUtils.GetOptionalAttributeValue(workingNode, ParamAttr, string.Empty)))
			{
				return; // nothing to do
			}
			var xaParam = workingNode.Attribute(ParamAttr);
			xaParam.Value = xaParam.Value + m_layoutParamAttrSuffix;
		}

		private void ReattachDupSuffixes(XElement copy, string dupKey)
		{
			FixUpPartRefParamAttrForDupNode(copy, dupKey);
			FixUpPartRefLabelAttrForDupNode(copy, dupKey);
		}

		private void CopySafeAttrs(XElement copy, XElement oldConfiguredPartRef)
		{
			// Ignore comments
			if (!oldConfiguredPartRef.HasAttributes)
			{
				return;
			}
			foreach (var xa in oldConfiguredPartRef.Attributes())
			{
				if (m_safeAttrs.Contains(xa.Name.LocalName))
				{
					XmlUtils.SetAttribute(copy, xa.Name.LocalName, xa.Value);
				}
				else if (NeedsAsParaParamSet(copy, xa))
				{
					XmlUtils.SetAttribute(copy, ParamAttr, xa.Value.Substring(0, xa.Value.IndexOf("_AsPara", StringComparison.Ordinal) + "_AsPara".Length)); // truncate after _AsPara
				}
			}
		}

		private static bool NeedsAsParaParamSet(XElement copy, XAttribute xa)
		{
			return xa.Name == ParamAttr && xa.Value.Contains("_AsPara") && copy.Attributes().Any() && copy.Attribute(ParamAttr) != null && !copy.Attribute(ParamAttr).Value.Contains("_AsPara");
		}

		/// <summary>
		/// Copy nodes from index to limIndex to output, stopping if a node is found
		/// that has a key in m_oldPartsFound. Return the index of the next (uncopied) node,
		/// either limIndex or the index of the node with a key we still want.
		/// </summary>
		private int CopyNodesNotInOldConfigured(int index, int limIndex)
		{
			var newMasterChildElements = m_newMaster.Elements().ToList();
			while (index < limIndex && !m_oldPartsFound.ContainsKey(GetKey(newMasterChildElements[index])))
			{
				var copy = CopyToOutput(newMasterChildElements[index]);
				// Susanna said to preserve the visibility of nodes not in the old version.
				// If we want to hide them to make the new view more like the 'oldConfigured' one, this is where to do it.
				//XmlUtils.SetAttribute(copy, "visibility", "never");
				CheckForAndReattachLayoutParamSuffix(copy);
				index++;
			}
			return index;
		}

		private void CopyAttributes(XElement source, XElement dest)
		{
			// Copy all layout attributes from the standard pattern to the output
			foreach (var attr in source.Attributes())
			{
				var xa = new XAttribute(attr.Name, attr.Value);
				dest.Add(xa);
				if (attr.Name == NameAttr && !string.IsNullOrEmpty(m_layoutParamAttrSuffix))
				{
					// This suffix also attaches to the 'name' attribute of the layout itself
					xa.Value = attr.Value + m_layoutParamAttrSuffix;
				}
			}
		}
	}
}