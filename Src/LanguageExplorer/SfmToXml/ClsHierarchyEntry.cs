// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
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
	public class ClsHierarchyEntry
	{
		private HashSet<string> m_ancestors = new HashSet<string>();
		private HashSet<string> m_beginFields = new HashSet<string>();
		private HashSet<string> m_additionalFields = new HashSet<string>();
		private HashSet<string> m_multiFields = new HashSet<string>();
		private HashSet<string> m_uniqueFields = new HashSet<string>();

		public ClsHierarchyEntry()
		{
		}

		public ClsHierarchyEntry(string name)
		{
			Name = name;
		}

		public ClsHierarchyEntry(string name, string partof, string beginFields, string additionalFields, string multiFields, string uniqueFields)
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
				SplitString(uniqueFields,  m_uniqueFields);
			}
		}

		public bool UsesSFM(string sfm)
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

		public string ToXmlString()
		{
			var result = ToString(); // get the data portion of the string
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");

			// add langDef element
			result = $"<level {result}/>";
			return result;
		}


		public string Name { get; private set; }

		public string KEY => Name;

		public int AncestorCount => m_ancestors.Count;

		public IReadOnlyCollection<string> Ancestors => m_ancestors;

		public bool AddAncestor(string ancestor)
		{
			if (m_ancestors.Contains(ancestor))
			{
				return false;
			}
			m_ancestors.Add(ancestor);
			return true;
		}
		public void RemoveAncestor(string ancestor)
		{
			m_ancestors.Remove(ancestor);
		}

		public bool ContainsAncestor(string ancestor)
		{
			return m_ancestors.Contains(ancestor);
		}

		public IReadOnlyCollection<string> BeginFields => m_beginFields;

		public IReadOnlyCollection<string> AdditionalFields => m_additionalFields;

		public IReadOnlyCollection<string> MultiFields => m_multiFields;

		public IReadOnlyCollection<string> UniqueFields => m_uniqueFields;

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

		public bool ReadXmlNode(XmlNode Level)
		{
			foreach (XmlAttribute Attribute in Level.Attributes)
			{
				switch (Attribute.Name)
				{
					case "name":
						Name = Attribute.Value;
						break;
					case "partOf":
						SplitString(Attribute.Value, m_ancestors);
						break;
					case "beginFields":
						SplitString(Attribute.Value, m_beginFields);
						break;
					case "additionalFields":
						SplitString(Attribute.Value, m_additionalFields);
						break;
					case "multiFields":
						SplitString(Attribute.Value, m_multiFields);
						break;
					case "uniqueFields":
						SplitString(Attribute.Value, m_uniqueFields);
						break;
					default:
						Converter.Log.AddWarning(string.Format(SfmToXmlStrings.UnknownAttribute0InTheHierarchySection, Attribute.Name));
						break;
				}
			}
			var success = true;
			if (Name == null)
			{
				Converter.Log.AddError(SfmToXmlStrings.NameNotDefinedInAHierarchyLevel);
				success = false;
			}
			if (AncestorCount == 0)
			{
				Converter.Log.AddError(string.Format(SfmToXmlStrings.HierarchyLevel0PartOfAttributeNotDefined, Name));
				success = false;
			}
			if (m_beginFields.Count == 0)
			{
				Converter.Log.AddError(string.Format(SfmToXmlStrings.HierarchyLevel0LacksBeginWithAtLeast1SFM, Name));
				success = false;
			}

			return success;
		}

		public bool BeginFieldsContains(string sfm)
		{
			return m_beginFields.Contains(sfm);
		}

		public bool AddBeginField(string sfm)
		{
			if (m_beginFields.Contains(sfm))
			{
				return false;
			}
			m_beginFields.Add(sfm);
			return true;
		}

		public void RemoveBeginField(string sfm)
		{
			m_beginFields.Remove(sfm);
		}

		public bool MultiFieldsContains(string sfm)
		{
			return m_multiFields.Contains(sfm);
		}

		public bool AddMultiField(string sfm)
		{
			if (m_multiFields.Contains(sfm))
			{
				return false;
			}
			m_multiFields.Add(sfm);
			return true;
		}

		public void RemoveMultiField(string sfm)
		{
			m_multiFields.Remove(sfm);
		}

		public bool AdditionalFieldsContains(string sfm)
		{
			return m_additionalFields.Contains(sfm);
		}

		public bool AddAdditionalField(string sfm)
		{
			if (m_additionalFields.Contains(sfm))
			{
				return false;
			}
			m_additionalFields.Add(sfm);
			return true;
		}

		public void RemoveAdditionalField(string sfm)
		{
			m_additionalFields.Remove(sfm);
		}

		// Unique field processing
		public bool UniqueFieldsContains(string sfm)
		{
			return m_uniqueFields.Contains(sfm);
		}

		public bool AddUniqueField(string sfm)
		{
			if (m_uniqueFields.Contains(sfm))
			{
				return false;
			}
			m_uniqueFields.Add(sfm);
			return true;
		}

		public void RemoveUniqueField(string sfm)
		{
			m_uniqueFields.Remove(sfm);
		}

		// Create a way to put in test data
		protected void TestMethodAddAncestors(ICollection<string> names)
		{
			foreach (string name in names)
			{
				m_ancestors.Add(name);
			}
		}

		public static string FindRootFromHash(Hashtable hierarchy)
		{
			var rootString = string.Empty;
			// determine what the leaf and root nodes are
			var leaf = new Hashtable();
			var root = new Hashtable();
			foreach (DictionaryEntry dictionaryEntry in hierarchy)
			{
				var entry = dictionaryEntry.Value as ClsHierarchyEntry;
				leaf.Add(entry.Name, null);     // in C++ this would be a 'set' type
				foreach (string name in entry.Ancestors)
				{
					if (!root.ContainsKey(name))
					{
						root.Add(name, null);
					}
				}
			}
			// now walk through one of the lists and mark items that exist in both lists
			var myEnumerator = leaf.GetEnumerator();
			while (myEnumerator.MoveNext())
			{
				// see if it's in both lists
				if (root.ContainsKey(myEnumerator.Key))
				{
					root.Remove(myEnumerator.Key);
					leaf.Remove(myEnumerator.Key);
					myEnumerator = leaf.GetEnumerator();
				}
			}

			// if we have more or less than 1 root item, this is an error
			if (root.Count != 1)
			{
				if (root.Count != 0)
				{
					foreach (DictionaryEntry dictEntry in root)
					{
						rootString += $" '{dictEntry.Key}'";
					}
				}
			}
			else
			{
				myEnumerator = root.GetEnumerator();
				myEnumerator.MoveNext();    // get on the first element
				rootString = myEnumerator.Key.ToString();
			}
			return rootString;
		}
	}
}