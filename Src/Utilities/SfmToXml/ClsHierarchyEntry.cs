using System;
using System.Collections;

namespace Sfm2Xml
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
		private string m_Name;

		// using Hashtables for improved performance when looking for individual entries
		private Hashtable m_Ancestors;
		private Hashtable m_BeginFields;
		private Hashtable m_AdditionalFields;
		private Hashtable m_MultiFields;
		private Hashtable m_UniqueFields;

		public ClsHierarchyEntry()
		{
			Init();
		}
		public ClsHierarchyEntry(string name)
		{
			m_Name = name;
			Init();
		}

		public ClsHierarchyEntry(string name, string partof, string beginFields,
			string additionalFields, string multiFields, string uniqueFields)
		{
			Init();
			m_Name = name;
			if (partof != null)
				SplitString(partof, ref m_Ancestors);
			if (beginFields != null)
				SplitString(beginFields, ref m_BeginFields);
			if (additionalFields != null)
				SplitString(additionalFields, ref m_AdditionalFields);
			if (multiFields != null)
				SplitString(multiFields, ref m_MultiFields);
			if (uniqueFields != null)
				SplitString(uniqueFields, ref m_UniqueFields);
		}

		private void Init()
		{
			m_Ancestors = new Hashtable();
			m_BeginFields = new Hashtable();
			m_AdditionalFields = new Hashtable();
			m_MultiFields = new Hashtable();
			m_UniqueFields = new Hashtable();
		}

		public bool UsesSFM(string sfm)
		{
			if (m_BeginFields.ContainsKey(sfm))
				return true;
			if (m_AdditionalFields.ContainsKey(sfm))
				return true;
			if (m_MultiFields.ContainsKey(sfm))
				return true;
			if (m_UniqueFields.ContainsKey(sfm))
				return true;
			return false;
		}


		public override string ToString()
		{
			string levelData = "name=\"" + this.Name + "\"";
			if (m_Ancestors.Count > 0)
			{
				levelData += " partOf=\"";
				foreach (string name in Ancestors)
				{
					levelData += name + " ";
				}
				levelData = levelData.Remove(levelData.Length-1, 1) + "\"";
			}

			if (m_BeginFields.Count > 0)
			{
				levelData += " beginFields=\"";
				foreach (string name in BeginFields)
				{
					levelData += name + " ";
				}
				levelData = levelData.Remove(levelData.Length-1, 1) + "\"";
			}

			if (m_AdditionalFields.Count > 0)
			{
				levelData += " additionalFields=\"";
				foreach (string name in AdditionalFields)
				{
					levelData += name + " ";
				}
				levelData = levelData.Remove(levelData.Length-1, 1) + "\"";
			}

			if (m_MultiFields.Count > 0)
			{
				levelData += " multiFields=\"";
				foreach (string name in MultiFields)
				{
					levelData += name + " ";
				}
				levelData = levelData.Remove(levelData.Length-1, 1) + "\"";
			}

			if (m_UniqueFields.Count > 0)
			{
				levelData += " uniqueFields=\"";
				foreach (string name in UniqueFields)
				{
					levelData += name + " ";
				}
				levelData = levelData.Remove(levelData.Length-1, 1) + "\"";
			}

			return levelData;
		}

		public string ToXmlString()
		{
			string result = ToString();	// get the data portion of the string
			result = result.Replace("&", "&amp;");
			result = result.Replace("<", "&lt;");
			result = result.Replace(">", "&gt;");

			// add langDef element
			result = "<level " + result + "/>";
			return result;
		}


		public string Name
		{
			get { return m_Name; }
		}

		public string KEY
		{
			get { return Name; }
		}


		public int AncestorCount
		{
			get	{ return m_Ancestors.Count;	}
		}
		public ICollection Ancestors
		{
			get { return m_Ancestors.Keys; }
		}
		public bool AddAncestor(string ancestor)
		{
			if (m_Ancestors.ContainsKey(ancestor))
				return false;
			m_Ancestors.Add(ancestor, null);
			return true;
		}
		public void RemoveAncestor(string ancestor)
		{
			m_Ancestors.Remove(ancestor);
		}
		public bool ContainsAncestor(string ancestor)
		{
			return m_Ancestors.ContainsKey(ancestor);
		}

		public ICollection BeginFields
		{
			get { return m_BeginFields.Keys; }
		}

		public ICollection AdditionalFields
		{
			get { return m_AdditionalFields.Keys; }
		}

		public ICollection MultiFields
		{
			get { return m_MultiFields.Keys; }
		}

		public ICollection UniqueFields
		{
			get { return m_UniqueFields.Keys; }
		}

		private void SplitString(string xyz, ref Hashtable list)
		{
			char [] delim = new char [] {' ', '\n', (char)0x0D, (char)0x0A };
			string [] values = xyz.Split(delim);
			foreach (string item in values)
			{
				// Make sure we're not dealing with adjacent delimiters or repeated substrings:
				if (item.Length > 0 && !list.ContainsKey(item))
					list.Add(item, null);
			}
		}

		public bool ReadXmlNode(System.Xml.XmlNode Level)
		{
			foreach(System.Xml.XmlAttribute Attribute in Level.Attributes)
			{
				switch (Attribute.Name)
				{
					case "name":
						m_Name = Attribute.Value;
						break;
					case "partOf":
						SplitString(Attribute.Value, ref m_Ancestors);
						break;
					case "beginFields":
						SplitString(Attribute.Value, ref m_BeginFields);
						break;
					case "additionalFields":
						SplitString(Attribute.Value, ref m_AdditionalFields);
						break;
					case "multiFields":
						SplitString(Attribute.Value, ref m_MultiFields);
						break;
					case "uniqueFields":
						SplitString(Attribute.Value, ref m_UniqueFields);
						break;
					default:
						Converter.Log.AddWarning(String.Format(Sfm2XmlStrings.UnknownAttribute0InTheHierarchySection, Attribute.Name));
						break;
				}
			}
			bool Success = true;
			if (m_Name == null)
			{
				Converter.Log.AddError(Sfm2XmlStrings.NameNotDefinedInAHierarchyLevel);
				Success = false;
			}
			if (AncestorCount == 0)
			{
				Converter.Log.AddError(String.Format(Sfm2XmlStrings.HierarchyLevel0PartOfAttributeNotDefined, m_Name));
				Success = false;
			}
			if (m_BeginFields.Count == 0)
			{
				Converter.Log.AddError(String.Format(Sfm2XmlStrings.HierarchyLevel0LacksBeginWithAtLeast1SFM, m_Name));
				Success = false;
			}

			return Success;
		}

		public bool BeginFieldsContains(string sfm)
		{
			return m_BeginFields.ContainsKey(sfm);
		}
		public bool AddBeginField(string sfm)
		{
			if (m_BeginFields.ContainsKey(sfm))
				return false;
			m_BeginFields.Add(sfm, null);
			return true;
		}
		public void RemoveBeginField(string sfm)
		{
			m_BeginFields.Remove(sfm);
		}

		public bool MultiFieldsContains(string sfm)
		{
			return m_MultiFields.ContainsKey(sfm);
		}
		public bool AddMultiField(string sfm)
		{
			if (m_MultiFields.ContainsKey(sfm))
				return false;
			m_MultiFields.Add(sfm, null);
			return true;
		}
		public void RemoveMultiField(string sfm)
		{
			m_MultiFields.Remove(sfm);
		}

		public bool AdditionalFieldsContains(string sfm)
		{
			return m_AdditionalFields.ContainsKey(sfm);
		}
		public bool AddAdditionalField(string sfm)
		{
			if (m_AdditionalFields.ContainsKey(sfm))
				return false;
			m_AdditionalFields.Add(sfm, null);
			return true;
		}
		public void RemoveAdditionalField(string sfm)
		{
			m_AdditionalFields.Remove(sfm);
		}

		// Unique field processing
		public bool UniqueFieldsContains(string sfm)
		{
			return m_UniqueFields.ContainsKey(sfm);
		}
		public bool AddUniqueField(string sfm)
		{
			if (m_UniqueFields.ContainsKey(sfm))
				return false;
			m_UniqueFields.Add(sfm, null);
			return true;
		}
		public void RemoveUniqueField(string sfm)
		{
			m_UniqueFields.Remove(sfm);
		}

		// Create a way to put in test data
		protected void TestMethodAddAncestors(ICollection names)
		{
			foreach(string name in names)
			{
				m_Ancestors.Add(name, null);
			}
		}

		public static string FindRootFromHash(Hashtable hierarchy)
		{
			string rootString = "";
			// determine what the leaf and root nodes are
			Hashtable leaf = new Hashtable();
			Hashtable root = new Hashtable();
			foreach(DictionaryEntry dictEentry in hierarchy)
			{
				ClsHierarchyEntry entry = dictEentry.Value as ClsHierarchyEntry;
				leaf.Add(entry.Name, null);		// in C++ this would be a 'set' type
				foreach(string name in entry.Ancestors)
				{
					if (!root.ContainsKey(name))
						root.Add(name, null);
				}
			}
			// now walk through one of the lists and mark items that exist in both lists
			IDictionaryEnumerator myEnumerator = leaf.GetEnumerator();
			while ( myEnumerator.MoveNext() )
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
					foreach (DictionaryEntry dictEntry in root)
						rootString += " '" + dictEntry.Key + "'";
			}
			else
			{
				myEnumerator = root.GetEnumerator();
				myEnumerator.MoveNext();	// get on the first element
				rootString = myEnumerator.Key.ToString();
			}
			return rootString;
		}
	}
}
