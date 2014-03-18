// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Serialization;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class allows configuring an element of dictionary data
	/// </summary>
	[XmlType (AnonymousType = false, TypeName = @"ConfigurationItem")]
	public class ConfigurableDictionaryNode
	{
		/// <summary>
		/// Additional information directing this configuration to a sub field, or property of a field, in the FieldWorks model
		/// </summary>
		[XmlAttribute(AttributeName = "subField")]
		public string SubField { get; set; }

		/// <summary>
		/// Information about the field in the FieldWorks model that this node is configuring
		/// </summary>
		[XmlAttribute(AttributeName = "field")]
		public string FieldDescription { get; set; }

		/// <summary>
		/// The style to apply to the data configured by this node
		/// </summary>
		[XmlAttribute(AttributeName = "style")]
		public string Style { get; set; }

		/// <summary>
		/// The label to display for this node
		/// </summary>
		[XmlAttribute(AttributeName = "name")]
		public string Label { get; set; }

		/// <summary>
		/// A suffix to distinguish between duplicate nodes
		/// </summary>
		[XmlAttribute(AttributeName = "nameSuffix")]
		public string LabelSuffix { get; set; }

		/// <summary>
		/// Combination of Label and LabelSuffix, if set.
		/// </summary>
		[XmlIgnore]
		public string DisplayLabel
		{
			get
			{
				if (LabelSuffix == null)
					return Label;
				return string.Format("{0} ({1})", Label, LabelSuffix);
			}
		}

		/// <summary>
		/// String to apply before the content configured by this node
		/// </summary>
		[XmlAttribute(AttributeName = "before")]
		public string Before { get; set; }

		/// <summary>
		/// String to apply after the content configured by this node
		/// </summary>
		[XmlAttribute(AttributeName = "after")]
		public string After { get; set; }

		/// <summary>
		/// String to apply between content items configured by this node (only applicable to lists)
		/// </summary>
		[XmlAttribute(AttributeName = "between")]
		public string Between { get; set; }

		/// <summary>
		/// Parent of this node, or null.
		/// </summary>
		[XmlIgnore]
		public ConfigurableDictionaryNode Parent { get; internal set; }

		/// <summary>
		/// Reference to a shared configuration node or null.
		/// </summary>
		[XmlElement("ReferenceItem")]
		public string ReferenceItem { get; set; }

		/// <summary>
		/// Ordered list of nodes contained by this configurable node
		/// </summary>
		[XmlElement(ElementName = "ConfigurationItem")]
		public List<ConfigurableDictionaryNode> Children { get; set; }

		/// <summary>
		/// Type specific configuration options for this configurable node;
		/// </summary>
		[XmlElement("WritingSystemOptions", typeof(DictionaryNodeWritingSystemOptions))]
		[XmlElement("ListTypeOptions", typeof(DictionaryNodeListOptions))]
		[XmlElement("ComplexFormOptions", typeof(DictionaryNodeComplexFormOptions))]
		[XmlElement("SenseOptions", typeof(DictionaryNodeSenseOptions))]
		public DictionaryNodeOptions DictionaryNodeOptions { get; set; }

		/// <summary>
		/// Whether this element of dictionary data is to shown as part of the dictionary.
		/// </summary>
		[XmlAttribute(AttributeName = "isEnabled")]
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Whether this element of dictionary data was duplicated from another element of dictionary data.
		/// </summary>
		[XmlAttribute(AttributeName = "isDuplicate")]
		public bool IsDuplicate { get; set; }

		/// <summary>
		/// Whether this element of dictionary data represents a custom field.
		/// </summary>
		[XmlAttribute(AttributeName = "isCustomField")]
		public bool IsCustomField { get; set; }

		/// <summary>
		/// Clone this node. Point to the same Parent object. Deep-clone Children and DictionaryNodeOptions.
		/// </summary>
		internal ConfigurableDictionaryNode DeepCloneUnderSameParent()
		{
			var clone = new ConfigurableDictionaryNode();

			// Copy everything over at first, importantly handling strings, bools, and Parent.
			var properties = typeof (ConfigurableDictionaryNode).GetProperties();
			foreach (var property in properties)
			{
				// Skip read-only properties (eg DisplayLabel)
				if (!property.CanWrite)
					continue;
				var originalValue = property.GetValue(this, null);
				property.SetValue(clone, originalValue, null);
			}

			// Deep-clone Children
			if (Children != null)
			{
				var clonedChildren = new List<ConfigurableDictionaryNode>();
				foreach (var child in Children)
				{
					var clonedChild = child.DeepCloneUnderSameParent();
					// Cloned children should point to their newly-cloned parent
					clonedChild.Parent = clone;
					clonedChildren.Add(clonedChild);
				}
				clone.Children = clonedChildren;
			}

			// TODO: Deep-clone DictionaryNodeOptions

			return clone;
		}

		public override int GetHashCode()
		{
			return Parent == null ? DisplayLabel.GetHashCode() : DisplayLabel.GetHashCode() ^ Parent.GetHashCode();
		}

		public override bool Equals(object other)
		{
			var otherNode = other as ConfigurableDictionaryNode;
			// The rules for our tree prevent two same-named nodes under a parent
			return otherNode != null && CheckParents(this, otherNode);
		}

		/// <summary>
		/// A match is two nodes with the same label and suffix in the same hierarchy (all ancestors have same labels & suffixes)
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		private static bool CheckParents(ConfigurableDictionaryNode first, ConfigurableDictionaryNode second)
		{
			if(first == null && second == null)
			{
				return true;
			}
			if((first.Parent == null && second.Parent != null) || (second.Parent == null && first.Parent != null))
			{
				return false;
			}
			return first.Label == second.Label && first.LabelSuffix == second.LabelSuffix && CheckParents(first.Parent, second.Parent);
		}

		/// <summary>
		/// Duplicate this node and its Children, adding the result to the Parent's list of children.
		/// </summary>
		public ConfigurableDictionaryNode DuplicateAmongSiblings()
		{
			return DuplicateAmongSiblings(Parent.Children);
		}

		/// <summary>
		/// Duplicate this node and its Children, adding the result among the list of siblings.
		/// </summary>
		public ConfigurableDictionaryNode DuplicateAmongSiblings(List<ConfigurableDictionaryNode> siblings)
		{
			var duplicate = DeepCloneUnderSameParent();
			duplicate.IsDuplicate = true;

			// Provide a suffix to distinguish among similar dictionary items.
			int suffix = 1;
			while (siblings.Exists(sibling => sibling.Label == this.Label && sibling.LabelSuffix == suffix.ToString()))
			{
				suffix++;
			}
			duplicate.LabelSuffix = suffix.ToString();

			var locationOfThisNode = siblings.IndexOf(this);
			siblings.Insert(locationOfThisNode + 1, duplicate);
			return duplicate;
		}

		/// <summary>
		/// Disassociate this node from its current Parent.
		/// </summary>
		public void UnlinkFromParent()
		{
			if (Parent == null)
				return;

			Parent.Children.Remove(this);
			Parent = null;
		}

		/// <summary>
		/// Change suffix. Must be unique among sibling dictionary items with the same label. It's okay to request to change to the current suffix.
		/// </summary>
		public bool ChangeSuffix(string newSuffix)
		{
			return ChangeSuffix(newSuffix, Parent.Children);
		}

		public bool ChangeSuffix(string newSuffix, List<ConfigurableDictionaryNode> siblings)
		{
			if (siblings.Exists(sibling => sibling != this && sibling.Label == this.Label && sibling.LabelSuffix == newSuffix))
				return false;

			LabelSuffix = newSuffix;
			return true;
		}
	}
}
