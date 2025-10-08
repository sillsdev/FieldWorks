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
	/// InterlinearObjects provides a mapping between type and property names and XML names
	/// for objects associated with interlinear texts.  The standard mapping is used for export,
	/// and the inverted mapping is used for import.
	/// </summary>
	internal class InterlinearObjects
	{
		private Dictionary<string, string> m_typeMap;
		private Dictionary<string, string> m_xmlTypeMap;

		private readonly Dictionary<string, Dictionary<string, string>> m_propertyMaps;
		private readonly Dictionary<string, Dictionary<string, string>> m_xmlPropertyMaps;

		internal Dictionary<string, string> TypeMap
		{ get { return m_typeMap; } }

		internal Dictionary<string, string> XmlTypeMap
		{ get { return m_xmlTypeMap; } }

		internal InterlinearObjects()
		{
			m_typeMap = new Dictionary<string, string>
			{
				{ "CmAnthroItem", "AnthroItem" },
				{ "CmLocation", "Location" },
				{ "CmPerson", "Person" },
				{ "CmPossibility", "Possibility" },
				{ "RnGenericRec", "NotebookRecord" },
				{ "RnRoledPartic", "RoledParticipants" },
			};
			m_xmlTypeMap = new Dictionary<string, string>();
			foreach (string type in m_typeMap.Keys)
			{
				m_xmlTypeMap[m_typeMap[type]] = type;
			}

			m_propertyMaps = new Dictionary<string, Dictionary<string, string>>
			{
				["CmAnthroItem"] = new Dictionary<string, string>()
				{
					{ "Name", "name" },
					{ "Abbreviation", "abbreviation" },
					{ "Description", "description" },
					{ "ConfidenceRA", "confidence" },
					{ "ResearchersRC", "researcher" },
					{ "RestrictionsRC", "restriction" },
					{ "StatusRA", "status" },
				},
				["CmLocation"] = new Dictionary<string, string>()
				{
					{ "Name", "name" },
					{ "Abbreviation", "abbreviation" },
					{ "Description", "description" },
				},
				["CmPerson"] = new Dictionary<string, string>()
				{
					{ "Name", "name" },
					{ "Abbreviation", "abbreviation" },
					{ "Description", "description" },
					{ "ConfidenceRA", "confidence" },
					{ "PositionsRC", "position" },
					{ "RestrictionsRC", "restriction" },
					{ "StatusRA", "status" },
					{ "EducationRA", "education" },
					{ "Gender", "gender" },
					{ "IsResearcher", "is-researcher" },
					{ "PlacesOfResidenceRC", "place-of-residence" },
					{ "PlaceOfBirthRA", "place-of-birth" },
				},
				["CmPossibility"] = new Dictionary<string, string>()
				{
					{ "Name", "name" },
					{ "Abbreviation", "abbreviation" },
					{ "Description", "description" },
				},
				["RnGenericRec"] = new Dictionary<string, string>()
				{
					{ "ResearchersRC", "researcher" },
					{ "ParticipantsOC", "roled-participants" },
					{ "SourcesRC", "source" },
					{ "LocationsRC", "location" },
					{ "AnthroCodesRC", "anthro-code" },
				},
				["RnRoledPartic"] = new Dictionary<string, string>()
				{
					{ "ParticipantsRC", "participant" },
					{ "RoleRA", "role" },
				},
				["Text"] = new Dictionary<string, string>()
				{
					{ "GenresRC", "genre" },
				}
			};
			m_xmlPropertyMaps = new Dictionary<string, Dictionary<string, string>>();
		}

		internal Dictionary<string, string> GetPropertyMap(string type)
		{
			return m_propertyMaps[type];
		}

		internal Dictionary<string, string> GetXmlPropertyMap(string type)
		{
			if (m_xmlPropertyMaps.ContainsKey(type))
				return m_xmlPropertyMaps[type];

			Dictionary<string, string> propertyMap = GetPropertyMap(XmlTypeMap[type]);
			Dictionary<string, string> xmlPropertyMap = InvertMap(propertyMap);
			m_xmlPropertyMaps.Add(type, xmlPropertyMap);
			return xmlPropertyMap;
		}

		internal Dictionary<string, string> InvertMap(Dictionary<string, string> propertyMap)
		{
			Dictionary<string, string> invertedPropertyMap = new Dictionary<string, string>();
			foreach (string name in propertyMap.Keys)
			{
				invertedPropertyMap[propertyMap[name]] = name;
			}
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
