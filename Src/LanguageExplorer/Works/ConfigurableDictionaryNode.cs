// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.FwUtils;
using System.Text.RegularExpressions;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// This class allows configuring an element of dictionary data
	/// </summary>
	[XmlType (AnonymousType = false, TypeName = @"ConfigurationItem")]
	public class ConfigurableDictionaryNode
	{
		public override string ToString()
		{
			return DisplayLabel ?? FieldDescription + (SubField == null ? "" : "_" + SubField);
		}

		/// <summary>
		/// The string table is used to localize strings plucked from the XML.  (Other external
		/// processing makes the localizations available.)
		/// </summary>
		[XmlIgnore]
		public StringTable StringTable { get; set; }

		/// <summary>
		/// The non-editable portion of the label to display for this node
		/// </summary>
		[XmlAttribute(AttributeName = "name")]
		public string Label { get; set; }

		/// <summary>
		/// A suffix to distinguish between duplicate nodes
		/// </summary>
		[XmlAttribute(AttributeName = "nameSuffix")]
		public string LabelSuffix { get; set; }

		/// <summary>
		/// Combination of Label and LabelSuffix, if set.  This is localized if at all possible.
		/// </summary>
		[XmlIgnore]
		public string DisplayLabel
		{
			get
			{
				string localizedLabel;
				if (StringTable == null)
				{
					localizedLabel = LabelSuffix == null ? Label : $"{Label} ({LabelSuffix})";
				}
				else
				{
					localizedLabel = LabelSuffix == null ? StringTable.LocalizeAttributeValue(Label) : $"{StringTable.LocalizeAttributeValue(Label)} ({StringTable.LocalizeAttributeValue(LabelSuffix)})";
				}
				if (DictionaryNodeOptions is DictionaryNodeGroupingOptions)
				{
					return $"[{localizedLabel}]";
				}
				return localizedLabel;
			}
		}

		/// <summary>
		/// Whether this element of dictionary data is shown as part of the dictionary.
		/// </summary>
		[XmlAttribute(AttributeName = "isEnabled")]
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Whether this element of dictionary data was duplicated from another element of dictionary data.
		/// </summary>
		[XmlAttribute(AttributeName = "isDuplicate")]
		public bool IsDuplicate { get; set; } // REVIEW (Hasso) 2014.04: could we use get { return !string.IsNullOrEmpty(NameSuffix); }?

		/// <summary>ShouldSerialize[Attribute] is a magic method to prevent serializing the default value</summary>
		public bool ShouldSerializeIsDuplicate() { return IsDuplicate; }

		/// <summary>
		/// Whether this element of dictionary data represents a custom field.
		/// </summary>
		[XmlAttribute(AttributeName = "isCustomField")]
		public bool IsCustomField { get; set; }

		/// <summary>ShouldSerialize[Attribute] is a magic method to prevent serializing the default value</summary>
		public bool ShouldSerializeIsCustomField() { return IsCustomField; }

		/// <summary>
		/// Should we hide custom fields which would show as children of this node.
		/// </summary>
		[XmlAttribute(AttributeName = "hideCustomFields")]
		public bool HideCustomFields { get; set; }

		/// <summary>ShouldSerialize[Attribute] is a magic method to prevent serializing the default value</summary>
		public bool ShouldSerializeHideCustomFields() { return HideCustomFields; }

		/// <summary>
		/// The style to apply to the data configured by this node
		/// </summary>
		[XmlAttribute(AttributeName = "style")]
		public string Style { get; set; }

		/// <summary>
		/// Whether the node's style selection should use character or paragraph styles. Allows specifying special cases like Minor Entry - Components (LT-15834).
		/// </summary>
		[XmlAttribute(AttributeName="styleType")]
		public StyleTypes StyleType { get; set; }

		/// <summary>
		/// ShouldSerialize[Attribute] is a magic method to prevent serialization of the default value.
		/// XMLSerializer looks for this method to determine whether to serialize each Element and Attribute.
		/// </summary>
		public bool ShouldSerializeStyleType()
		{
			return StyleType != StyleTypes.Default;
		}

		/// <summary>
		/// String to apply before the content configured by this node
		/// </summary>
		[XmlAttribute(AttributeName = "before")]
		public string Before { get; set; }

		/// <summary>
		/// String to apply between content items configured by this node (only applicable to lists)
		/// </summary>
		[XmlAttribute(AttributeName = "between")]
		public string Between { get; set; }

		/// <summary>
		/// String to apply after the content configured by this node
		/// </summary>
		[XmlAttribute(AttributeName = "after")]
		public string After { get; set; }

		/// <summary>
		/// Information about the field in the FieldWorks model that this node is configuring
		/// </summary>
		[XmlAttribute(AttributeName = "field")]
		public string FieldDescription { get; set; }

		/// <summary>
		/// Additional information directing this configuration to a sub field, or property of a field, in the FieldWorks model
		/// </summary>
		[XmlAttribute(AttributeName = "subField")]
		public string SubField { get; set; }

		/// <summary>
		/// Normally the FieldDescription in a ConfigurationNode will be directly used as the class name for
		/// the css and xhtml generated at that node. This field is used to provide alternative class names either to match
		/// historical exports, or for other strong reasons which should be documented where the override is defined.
		/// </summary>
		[XmlAttribute(AttributeName = "cssClassNameOverride")]
		public string CSSClassNameOverride { get; set; }

		/// <summary>
		/// Type specific configuration options for this configurable node;
		/// </summary>
		[XmlElement("WritingSystemOptions", typeof(DictionaryNodeWritingSystemOptions))]
		[XmlElement("WritingSystemAndParaOptions", typeof(DictionaryNodeWritingSystemAndParaOptions))]
		[XmlElement("ReferringSenseOptions", typeof(DictionaryNodeReferringSenseOptions))]
		[XmlElement("ListTypeOptions", typeof(DictionaryNodeListOptions))]
		[XmlElement("ComplexFormOptions", typeof(DictionaryNodeListAndParaOptions))]
		[XmlElement("SenseOptions", typeof(DictionaryNodeSenseOptions))]
		[XmlElement("PictureOptions", typeof(DictionaryNodePictureOptions))]
		[XmlElement("GroupingOptions", typeof(DictionaryNodeGroupingOptions))]
		public DictionaryNodeOptions DictionaryNodeOptions { get; set; }

		/// <summary>
		/// Ordered list of nodes contained by this configurable node
		/// </summary>
		[XmlElement(ElementName = "ConfigurationItem")]
		public List<ConfigurableDictionaryNode> Children { get; set; }

		/// <summary>
		/// Parent of this node, or null if this is a top-level node.
		/// </summary>
		[XmlIgnore]
		public ConfigurableDictionaryNode Parent { get; internal set; }

		/// <summary>
		/// Reference to (Label of) a shared configuration node in SharedItems or null.
		/// </summary>
		[XmlElement("ReferenceItem")]
		public string ReferenceItem { get; set; }

		/// <summary>
		/// The actual node denoted by ReferenceItem; null if none
		/// </summary>
		internal ConfigurableDictionaryNode ReferencedNode { get; set; }

		/// <summary>
		/// Children of this node, if any; otherwise, children of the ReferenceItem, if any
		/// </summary>
		[XmlIgnore]
		public List<ConfigurableDictionaryNode> ReferencedOrDirectChildren => ReferencedNode == null ? Children : ReferencedNode.Children;

		/// <summary>If node is a HeadWord node.</summary>
		internal bool IsHeadWord => CSSClassNameOverride == "headword" || CSSClassNameOverride == "mainheadword";

		/// <summary>If node is a Main Entry node.</summary>
		internal bool IsMainEntry
		{
			get
			{
				switch (CSSClassNameOverride)
				{
					case "entry":
					case "mainentrycomplex":
					case "reversalindexentry":
						return true;
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Whether this node is the master parent of a SharedItem
		/// </summary>
		internal bool IsMasterParent => ReferencedNode != null && ReferenceEquals(this, ReferencedNode.Parent);

		/// <summary>
		/// True when this node is a parent of a SharedItem, but not the Master Parent
		/// </summary>
		internal bool IsSubordinateParent => ReferencedNode != null && !ReferenceEquals(this, ReferencedNode.Parent);

		/// <summary>
		/// Whether this is a SharedItem.
		/// </summary>
		internal bool IsSharedItem => Parent != null && Parent.ReferencedNode != null;

		/// <summary>
		/// Whether this has a SharedItem anywhere in its ancestry
		/// </summary>
		internal bool IsSharedItemOrDescendant { get { ConfigurableDictionaryNode dummy; return TryGetMasterParent(out dummy); } }

		/// <summary>
		/// Finds the nearest Master Parent in this node's ancestry if this is a SharedItem or descendent;
		/// returns false (out null) if this is a direct descendent of a Part.
		/// </summary>
		internal bool TryGetMasterParent(out ConfigurableDictionaryNode masterParent)
		{
			for (masterParent = Parent; masterParent != null; masterParent = masterParent.Parent)
			{
				if (masterParent.ReferencedNode != null)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Clone this node. Point to the same Parent object. Deep-clone Children and DictionaryNodeOptions.
		/// </summary>
		/// <remarks>
		/// Grouping node children are cloned only in recursive calls.
		/// Referenced children are cloned only if this is NOT a recursive call.
		/// </remarks>
		internal ConfigurableDictionaryNode DeepCloneUnderSameParent()
		{
			return DeepCloneUnderParent(Parent);
		}

		/// <summary>
		/// Clone this node, point to the given Parent. Deep-clone Children and DictionaryNodeOptions
		/// </summary>
		/// <remarks>
		/// Grouping node children are cloned only if this is a recursive call.
		/// Referenced children are cloned only if this is NOT a recursive call.
		/// </remarks>
		internal ConfigurableDictionaryNode DeepCloneUnderParent(ConfigurableDictionaryNode parent, bool isRecursiveCall = false)
		{
			var clone = new ConfigurableDictionaryNode();

			// Copy everything over at first, importantly handling strings, bools.
			var properties = typeof(ConfigurableDictionaryNode).GetProperties();
			foreach (var property in properties)
			{
				// Skip Parent and read-only properties (eg DisplayLabel)
				if (!property.CanWrite || property.Name == "Parent")
				{
					continue;
				}
				var originalValue = property.GetValue(this, null);
				property.SetValue(clone, originalValue, null);
			}
			clone.ReferencedNode = ReferencedNode; // GetProperties() doesn't return internal properties; copy here
			clone.Parent = parent;

			// Deep-clone Children
			if (Children != null && Children.Any())
			{
				if (isRecursiveCall || !(DictionaryNodeOptions is DictionaryNodeGroupingOptions))
				{
					// Cloned children should point to their newly-cloned parent
					clone.Children = Children.Select(child => child.DeepCloneUnderParent(clone, true)).ToList();
				}
				else
				{
					// Cloning children of a group creates problems because the children can be moved out of the group.
					// Also the only expected use of cloning a group is to get a new group to group different children.
					clone.Children = null;
				}
			}
			else if (!isRecursiveCall && ReferencedNode != null && ReferencedNode.Children != null)
			{
				// Allow users to configure copies of Shared nodes (e.g. Subentries) separately
				clone.ReferencedNode = null;
				clone.ReferenceItem = null;
				clone.Children = ReferencedNode.Children.Select(child => child.DeepCloneUnderParent(clone, true)).ToList();
			}

			// Deep-clone DictionaryNodeOptions
			if (DictionaryNodeOptions != null)
				clone.DictionaryNodeOptions = DictionaryNodeOptions.DeepClone();

			return clone;
		}

		public override int GetHashCode()
		{
			return Parent == null ? DisplayLabel.GetHashCode() : DisplayLabel.GetHashCode() ^ Parent.GetHashCode();
		}

		public override bool Equals(object other)
		{
			var otherNode = other as ConfigurableDictionaryNode;
			if (otherNode == null || Label != otherNode.Label || LabelSuffix != otherNode.LabelSuffix ||
			    FieldDescription != otherNode.FieldDescription)
			{
				return false;
			}
			// The rules for our tree prevent two same-named nodes under a parent
			return CheckParents(this, otherNode);
		}

		/// <summary>
		/// A match is two nodes with the same label and suffix in the same hierarchy (all ancestors have same labels & suffixes)
		/// </summary>
		private static bool CheckParents(ConfigurableDictionaryNode first, ConfigurableDictionaryNode second)
		{
			if(first == null && second == null)
			{
				return true;
			}
			if((first == null ^ second == null) || (first.Parent == null ^ second.Parent == null)) // ^ is XOR
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
			var suffix = 1;
			var labelSuffix = string.IsNullOrEmpty(duplicate.LabelSuffix) ? "" : duplicate.LabelSuffix;
			var copy = Regex.Match(labelSuffix, @"\d+$");
			if (copy.Success)
			{
				suffix = Convert.ToInt32(copy.Value);
				labelSuffix = labelSuffix.Remove(labelSuffix.LastIndexOf(copy.Value));
			}
			duplicate.LabelSuffix = string.Concat(labelSuffix, suffix);

			// Check that no siblings have a matching suffix, and that no children of grouping siblings have a matching suffix
			while (duplicate.NodeWithSameDisplayLabelExists(duplicate.LabelSuffix, siblings))
			{
				suffix++;
				duplicate.LabelSuffix = string.Concat(labelSuffix, suffix);
			}
			//duplicate.LabelSuffix += suffix;

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
			{
				return;
			}

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
			if (NodeWithSameDisplayLabelExists(newSuffix, siblings))
			{
				return false;
			}

			LabelSuffix = newSuffix;
			return true;
		}

		private bool NodeWithSameDisplayLabelExists(string newSuffix, List<ConfigurableDictionaryNode> siblings)
		{
			return GatherReallyReallyAllSiblings(siblings).Exists(node => !ReferenceEquals(node, this) && node.Label == Label && node.LabelSuffix == newSuffix);
		}

		/// <summary>sibling nodes and all related children of grouping nodes together for comparison (null for top-level nodes)</summary>
		[XmlIgnore]
		public List<ConfigurableDictionaryNode> ReallyReallyAllSiblings => Parent == null ? null : GatherReallyReallyAllSiblings(Parent.Children);

		private List<ConfigurableDictionaryNode> GatherReallyReallyAllSiblings(List<ConfigurableDictionaryNode> siblings)
		{
			if (Parent?.DictionaryNodeOptions is DictionaryNodeGroupingOptions)
			{
				siblings = Parent.IsSharedItem ? Parent.Parent.Parent.ReferencedOrDirectChildren : Parent.Parent.ReferencedOrDirectChildren;
			}
			var reallyReallyAllSiblings = new List<ConfigurableDictionaryNode>(siblings);
			foreach (var sibling in siblings.Where(sibling => sibling.DictionaryNodeOptions is DictionaryNodeGroupingOptions))
			{
				if (sibling.ReferencedOrDirectChildren != null)
				{
					reallyReallyAllSiblings.AddRange(sibling.ReferencedOrDirectChildren);
				}
			}
			return reallyReallyAllSiblings;
		}
	}
}
