using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// InterlinearRecords provides a mapping between type and property names and XML names
	/// for records associated with interlinear texts.  The standard mapping is used for export,
	/// and the inverted mapping is used for import.
	/// </summary>
	internal class InterlinearRecords
	{
		private Dictionary<string, string> m_typeMap;
		private Dictionary<string, string> m_invertedTypeMap;

		private readonly Dictionary<string, Dictionary<string, string>> m_propertyMaps;
		private readonly Dictionary<string, Dictionary<string, string>> m_invertedPropertyMaps;

		internal Dictionary<string, string> TypeMap
		{ get { return m_typeMap; } }

		internal Dictionary<string, string> InvertedTypeMap
		{ get { return m_invertedTypeMap; } }

		internal InterlinearRecords()
		{
			m_typeMap = new Dictionary<string, string>
			{
				{ "CmPossibility", "Possibility" },
				{ "RnGenericRec", "Record" }
			};
			m_invertedTypeMap = new Dictionary<string, string>();
			foreach (string type in m_typeMap.Keys)
			{
				m_invertedTypeMap[m_typeMap[type]] = type;
			}

			m_propertyMaps = new Dictionary<string, Dictionary<string, string>>();
			m_propertyMaps["CmPossibility"] = new Dictionary<string, string>()
			{
				{ "Name", "name" },
				{ "Abbreviation", "abbreviation" },
				{ "Description", "description" },
				{ "StatusRA", "status" },
				{ "DiscussionOA", "discussion" },
				{ "ConfidenceRA", "confidence" },
				{ "ResearchersRC", "researcher" },
				{ "RestrictionsRC", "restriction" },
			};
			m_invertedPropertyMaps = new Dictionary<string, Dictionary<string, string>>();
		}

		internal Dictionary<string, string> GetPropertyMap(string type)
		{
			return m_propertyMaps[type];
		}

		internal Dictionary<string, string> GetInvertedPropertyMap(string type)
		{
			if (m_invertedPropertyMaps.ContainsKey(type))
				return m_invertedPropertyMaps[type];

			Dictionary<string, string> propertyMap = GetPropertyMap(InvertedTypeMap[type]);
			Dictionary<string, string> invertedPropertyMap = new Dictionary<string, string>();
			foreach(string name in propertyMap.Keys)
			{
				invertedPropertyMap[propertyMap[name]] = name;
			}
			m_invertedPropertyMaps.Add(type, propertyMap);
			return invertedPropertyMap;
		}

		internal string HyphenCase(string name)
		{
			string newName = "";
			for (int i = 0; i < name.Length; i++)
			{
				newName += (i > 0 && char.IsUpper(name[i])) ? ("-" + char.ToLower(name[i])) : name[i].ToString();
			}
			return newName;
		}

	}
}
