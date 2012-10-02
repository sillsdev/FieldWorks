using System.Collections.Generic;
using System.Xml;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// A layout merger is used to merge a current version layout with an override that is an older version.
	/// Merging takes place broadly by copying the current version into a new XmlNode in a specified document.
	/// Currently most elements are copied unmodified. However, part refs are treated specially:
	/// Each consecutive sequence of part nodes is adjusted in the following ways:
	///     - order is made to conform to the order in the override, for any elements found in the override.
	///     - any elements not found in the override are copied, in the current sequence starting from the previous
	///         element that IS matched, and visibility set to "never".
	///     - (elements not matched in the override are discarded, except that)
	///     - "$child" elements in the override are copied to the output
	///     - a specified set of attributes may also be overridden.
	/// </summary>
	public class LayoutMerger : IOldVersionMerger
	{
		private XmlDocument m_dest;
		private XmlNode m_current;
		private XmlNode m_wanted;
		private XmlElement m_output;
		private Dictionary<string, bool> m_wantedFound;
		private Set<XmlNode> m_insertedMissing; // missing nodes we already inserted.
		Set<string> m_safeAttrs = new Set<string>(
			new string[] { "before", "after", "sep", "ws", "style", "showLabels", "number", "numstyle", "numsingle", "visibility",
				"singlegraminfofirst", "showasindentedpara" });

		/// <summary>
		/// This is the main entry point.
		/// </summary>
		public XmlNode Merge(XmlNode current, XmlNode wanted, XmlDocument dest)
		{
			m_current = current;
			m_wanted = wanted;
			m_dest = dest;
			m_insertedMissing = new Set<XmlNode>();
			m_output = m_dest.CreateElement(current.Name);
			CopyAttributes(m_current, m_output);
			int startIndex = 0;
			BuildWantedFound();
			while (startIndex < m_current.ChildNodes.Count)
			{
				XmlNode currentChild = m_current.ChildNodes[startIndex];
				if (IsMergeableNode(currentChild))
				{
					int limIndex = startIndex + 1;
					while (limIndex < m_current.ChildNodes.Count && m_current.ChildNodes[limIndex].Name == currentChild.Name)
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

		private bool IsMergeableNode(XmlNode currentChild)
		{
			return currentChild.Name == "part";
		}

		// For most parts, the key is the ref attribute, but if that is $child, it's a custom field and
		// the label is the best way to distinguish.
		private string GetKey(XmlNode node)
		{
			string key = XmlUtils.GetOptionalAttributeValue(node, "ref", "");
			if (key == "$child")
				key = XmlUtils.GetOptionalAttributeValue(node, "label", "$child");
			return key;
		}

		private void BuildWantedFound()
		{
			m_wantedFound = new Dictionary<string, bool>();
			foreach (XmlNode child in m_wanted)
				if (IsMergeableNode(child))
					m_wantedFound[GetKey(child)] = false;
		}

		// Answer true if we want to insert a copy of an output node even though there isn't a match in the current
		// input range. Currently this requires both that it has a ref of $child and the key doesn't match ANY part node in the input.
		bool WantToCopyMissingItem(XmlNode node)
		{
			if(XmlUtils.GetOptionalAttributeValue(node, "ref", "") !="$child")
				return false;
			string key = GetKey(node);
			if (m_insertedMissing.Contains(node))
				return false; // don't insert twice!
			foreach (XmlNode child in m_current.ChildNodes)
				if (IsMergeableNode(child) && GetKey(child) == key)
					return false;
			return true;
		}

		private void CopyParts(int startIndex, int limIndex)
		{
			// Copy initial items not in wanted
			int firstWanted = CopyNodesNotInWanted(startIndex, limIndex);
			foreach (XmlNode wanted in m_wanted)
			{
				CopyStuffForWanted(wanted, firstWanted, limIndex);
			}
		}

		// Copy whatever we ought to for the output node wanted from the input range specified.
		// If we find a node with the same key in the range, copy it, and if the input range
		// contains following nodes not in output and this is the first copy of the output key, copy them too.
		// If we don't find an input node to copy, we might copy the output.
		private void CopyStuffForWanted(XmlNode wanted, int firstWanted, int limIndex)
		{
			string key = GetKey(wanted);
			for (int index = firstWanted; index < limIndex; index++)
			{
				XmlNode child = m_current.ChildNodes[index];
				if (GetKey(child) == key)
				{
					XmlNode copy = CopyToOutput(child);
					CopySafeAttrs(copy, wanted);
					if (!m_wantedFound[key])
					{
						m_wantedFound[key] = true; // copy new following nodes only once, not for duplicates.
						CopyNodesNotInWanted(index + 1, limIndex);
					}
					return;
				}
			}
			if (WantToCopyMissingItem(wanted))
			{
				CopyToOutput(wanted);
				m_insertedMissing.Add(wanted);
			}
		}

		private void CopySafeAttrs(XmlNode copy, XmlNode wanted)
		{
			// Ignore comments
			if (wanted.Attributes == null)
				return;
			foreach (XmlAttribute xa in wanted.Attributes)
				if (m_safeAttrs.Contains(xa.Name))
					XmlUtils.SetAttribute(copy, xa.Name, xa.Value);
		}

		/// <summary>
		/// Copy nodes from index to limIndex to output, stopping if a node is found
		/// that has a key in m_wantedFound. Return the index of the next (uncopied) node,
		/// either limIndex or the index of the node with a wanted key.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="limIndex"></param>
		/// <returns></returns>
		private int CopyNodesNotInWanted(int index, int limIndex)
		{
			while (index < limIndex && !m_wantedFound.ContainsKey(GetKey(m_current.ChildNodes[index])))
			{
				XmlNode copy = CopyToOutput(m_current.ChildNodes[index]);
				// Susanna said to preserve the visibility of nodes not in the old version.
				// If we want to hide them to make the new view more like the 'wanted' one, this is where to do it.
				//XmlUtils.SetAttribute(copy, "visibility", "never");
				index++;
			}
			return index;
		}

		private void CopyAttributes(XmlNode source, XmlNode dest)
		{
			foreach (XmlAttribute attr in source.Attributes)
			{
				XmlAttribute xa = m_dest.CreateAttribute(attr.Name);
				dest.Attributes.Append(xa);
				xa.Value = attr.Value;
			}
		}

	}
}
