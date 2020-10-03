// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	///  This class implements the data structure for the "hierarchy" mapping element.
	///  This element contains any number of "level" elements which define a hierarchy level.
	///  Attributes of "level" are:
	///  - name : (R) the name of this type
	///  - partOf : (R) this is a list of "levels" that this one can belong to.
	///  - beginFields : (R) this is a list of "field"-"sfm" values that defines which fields
	/// 	 can be used to start a new "level".
	///  - additionalFields : (O) this is a list of "field"-"sfm" values that are allowed for
	/// 	 this type.
	///  - multiFields : (O) this is a list of "field"-"sfm" values that can have multiple
	/// 	 entries at this current level.
	///  - uniqueFields: (O) this is a list of "field"-"sfm" values that can only be used one
	///		 time in each entry.  It is used to override allowing multiple begin fields that are
	///		 different to be combined in a given field/object.
	/// </summary>
	internal sealed class ClsHierarchyEntry
	{
		private HashSet<string> m_ancestors = new HashSet<string>();
		private HashSet<string> m_beginFields = new HashSet<string>();
		private HashSet<string> m_additionalFields = new HashSet<string>();
		private HashSet<string> m_multiFields = new HashSet<string>();
		private HashSet<string> m_uniqueFields = new HashSet<string>();

		internal ClsHierarchyEntry()
		{
		}

		internal ClsHierarchyEntry(string name)
		{
			Name = name;
		}

		internal ClsHierarchyEntry(string name, string partof, string beginFields, string additionalFields, string multiFields, string uniqueFields)
			: this(name)
		{
			if (partof != null)
			{
				SplitString(partof, m_ancestors);
			}
			if (beginFields != null)
			{
				SplitString(beginFields, m_beginFields);
			}
			if (additionalFields != null)
			{
				SplitString(additionalFields, m_additionalFields);
			}
			if (multiFields != null)
			{
				SplitString(multiFields, m_multiFields);
			}
			if (uniqueFields != null)
			{
				SplitString(uniqueFields, m_uniqueFields);
			}
		}

		internal bool UsesSFM(string sfm)
		{
			return m_beginFields.Contains(sfm) || m_additionalFields.Contains(sfm) || m_multiFields.Contains(sfm) || m_uniqueFields.Contains(sfm);
		}


		public override string ToString()
		{
			var levelData = $"name=\"{Name}\"";
			if (m_ancestors.Count > 0)
			{
				levelData += " partOf=\"";
				levelData = Ancestors.Aggregate(levelData, (current, name) => current + name + " ");
				levelData = levelData.Remove(levelData.Length - 1, 1) + "\"";
			}
			if (m_beginFields.Count > 0)
			{
				levelData += " beginFields=\"";
				levelData = BeginFields.Aggregate(levelData, (current, name) => current + name + " ");
				levelData = levelData.Remove(levelData.Length - 1, 1) + "\"";
			}
			if (m_additionalFields.Count > 0)
			{
				levelData += " additionalFields=\"";
				levelData = AdditionalFields.Aggregate(levelData, (current, name) => current + name + " ");
				levelData = levelData.Remove(levelData.Length - 1, 1) + "\"";
			}
			if (m_multiFields.Count > 0)
			{
				levelData += " multiFields=\"";
				levelData = MultiFields.Aggregate(levelData, (current, name) => current + name + " ");
				levelData = levelData.Remove(levelData.Length - 1, 1) + "\"";
			}
			if (m_uniqueFields.Count > 0)
			{
				levelData += " uniqueFields=\"";
				levelData = UniqueFields.Aggregate(levelData, (current, name) => current + name + " ");
				levelData = levelData.Remove(levelData.Length - 1, 1) + "\"";
			}
			return levelData;
		}

		internal string ToXmlString()
		{
			var result = ToString(); // get the data portion of the string
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");
			// add langDef element
			result = $"<level {result}/>";
			return result;
		}

		internal string Name { get; private set; }

		internal string KEY => Name;

		internal int AncestorCount => m_ancestors.Count;

		internal IReadOnlyCollection<string> Ancestors => m_ancestors;

		internal bool AddAncestor(string ancestor)
		{
			if (m_ancestors.Contains(ancestor))
			{
				return false;
			}
			m_ancestors.Add(ancestor);
			return true;
		}

		internal void RemoveAncestor(string ancestor)
		{
			m_ancestors.Remove(ancestor);
		}

		internal bool ContainsAncestor(string ancestor)
		{
			return m_ancestors.Contains(ancestor);
		}

		internal IReadOnlyCollection<string> BeginFields => m_beginFields;

		internal IReadOnlyCollection<string> AdditionalFields => m_additionalFields;

		internal IReadOnlyCollection<string> MultiFields => m_multiFields;

		internal IReadOnlyCollection<string> UniqueFields => m_uniqueFields;

		private static void SplitString(string xyz, ISet<string> list)
		{
			var delim = new[] { ' ', '\n', (char)0x0D, (char)0x0A };
			var values = xyz.Split(delim);
			foreach (var item in values)
			{
				// Make sure we're not dealing with adjacent delimiters or repeated substrings:
				if (item.Length > 0 && !list.Contains(item))
				{
					list.Add(item);
				}
			}
		}

		internal bool ReadXmlNode(XmlNode level)
		{
			foreach (XmlAttribute xmlAttribute in level.Attributes)
			{
				switch (xmlAttribute.Name)
				{
					case "name":
						Name = xmlAttribute.Value;
						break;
					case "partOf":
						SplitString(xmlAttribute.Value, m_ancestors);
						break;
					case "beginFields":
						SplitString(xmlAttribute.Value, m_beginFields);
						break;
					case "additionalFields":
						SplitString(xmlAttribute.Value, m_additionalFields);
						break;
					case "multiFields":
						SplitString(xmlAttribute.Value, m_multiFields);
						break;
					case "uniqueFields":
						SplitString(xmlAttribute.Value, m_uniqueFields);
						break;
					default:
						SfmToXmlServices.Log.AddWarning(string.Format(SfmToXmlStrings.UnknownAttribute0InTheHierarchySection, xmlAttribute.Name));
						break;
				}
			}
			var success = true;
			if (Name == null)
			{
				SfmToXmlServices.Log.AddError(SfmToXmlStrings.NameNotDefinedInAHierarchyLevel);
				success = false;
			}
			if (AncestorCount == 0)
			{
				SfmToXmlServices.Log.AddError(string.Format(SfmToXmlStrings.HierarchyLevel0PartOfAttributeNotDefined, Name));
				success = false;
			}
			if (m_beginFields.Count == 0)
			{
				SfmToXmlServices.Log.AddError(string.Format(SfmToXmlStrings.HierarchyLevel0LacksBeginWithAtLeast1SFM, Name));
				success = false;
			}
			return success;
		}

		internal bool BeginFieldsContains(string sfm)
		{
			return m_beginFields.Contains(sfm);
		}

		internal bool AddBeginField(string sfm)
		{
			if (m_beginFields.Contains(sfm))
			{
				return false;
			}
			m_beginFields.Add(sfm);
			return true;
		}

		internal bool MultiFieldsContains(string sfm)
		{
			return m_multiFields.Contains(sfm);
		}

		internal bool AddMultiField(string sfm)
		{
			if (m_multiFields.Contains(sfm))
			{
				return false;
			}
			m_multiFields.Add(sfm);
			return true;
		}

		internal bool AdditionalFieldsContains(string sfm)
		{
			return m_additionalFields.Contains(sfm);
		}

		internal bool AddAdditionalField(string sfm)
		{
			if (m_additionalFields.Contains(sfm))
			{
				return false;
			}
			m_additionalFields.Add(sfm);
			return true;
		}

		// Unique field processing
		internal bool UniqueFieldsContains(string sfm)
		{
			return m_uniqueFields.Contains(sfm);
		}

		internal bool AddUniqueField(string sfm)
		{
			if (m_uniqueFields.Contains(sfm))
			{
				return false;
			}
			m_uniqueFields.Add(sfm);
			return true;
		}
	}
}