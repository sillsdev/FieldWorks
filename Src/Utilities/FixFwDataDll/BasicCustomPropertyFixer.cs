// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.FixData
{
	/// <summary>
	/// This fixer is responsible to ensure that all custom properties that have value (or basic) types
	/// have an actual value in the file. Although FLEx ensures this when writing the file, a merge
	/// could potentially merge a new custom field definition from one user with a new instance
	/// that was created without the custom field from the other user.
	/// </summary>
	internal class BasicCustomPropertyFixer : RtFixer
	{
		// Maps class name to list of required (value type) custom fields
		private readonly Dictionary<string, List<string>> m_customFields = new Dictionary<string, List<string>>();

		internal override void InspectAdditionalFieldsElement(XElement additionalFieldsElem)
		{
			var customFields = additionalFieldsElem.Descendants("CustomField");
			foreach (var field in customFields)
			{
				var classAttr = field.Attribute("class");
				if (classAttr == null || string.IsNullOrEmpty(classAttr.Value))
					continue; // bizarre, but we can't fix it.
				var nameAttr = field.Attribute("name");
				if (nameAttr == null || string.IsNullOrEmpty(nameAttr.Value))
					continue;
				var fieldAttr = field.Attribute("type");
				if (fieldAttr == null || string.IsNullOrEmpty(fieldAttr.Value))
					continue;
				switch (fieldAttr.Value)
				{
					case "Integer":
						break;
					case "GenDate":
						break;
						// Eventually, we may need to handle all the types for which FdoIFwMetaDataCache.IsValueType returns true,
						// currently Boolean, GenDate, Guid, Integer, Float, Numeric, and Time.
						// Some of these would need other default values, such as 'false' (or False?).
						// However, currently Integer and GenDate are the only supported kinds of custom basic property,
						// and zero is always a suitable default.
					default:
						continue;
				}
				List<string> fields;
				if (!m_customFields.TryGetValue(classAttr.Value, out fields))
				{
					fields = new List<string>();
					m_customFields.Add(classAttr.Value, fields); // needed even for MoForm, so the TryGetValue above works.
					// It would be much nicer here to use a metadatacache and find all the concrete subclasses
					// of classAttr.Value and (independently) add the field to their (independent) lists.
					// But we don't have access to an MDC here, and in practice only one of the four classes
					// that can currently have custom fields has subclasses, and none of them can have
					// custom fields that the others do not. So this kludge works.
					if (classAttr.Value == "MoForm")
					{
						m_customFields.Add("MoAffixAllomorph", fields);
						m_customFields.Add("MoAffixProcess", fields);
						m_customFields.Add("MoStemAllomorph", fields);
					}
				}
				fields.Add(nameAttr.Value);
			}
		}

		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger logger)
		{
			var classAttr = rt.Attribute("class");
			if (classAttr == null)
				return true; // bizarre, but leave it alone
			List<string> requiredFields;
			if (!m_customFields.TryGetValue(classAttr.Value, out requiredFields))
				return true; // has no custom fields we care about
			var missingFields = new HashSet<string>(requiredFields);
			foreach (var child in rt.Elements())
			{
				if (child.Name == "Custom")
				{
					var nameAttr = child.Attribute("name");
					if (nameAttr != null)
						missingFields.Remove(nameAttr.Value); // not missing, we don't need to add it.
				}
			}
			foreach (var fieldName in missingFields)
			{
				rt.Add(new XElement("Custom",
					new XAttribute("name", fieldName),
					new XAttribute("val", "0")));
				var guid = rt.Attribute("guid").Value;
				// This is such an insignificant fix from the user's point of view that we might prefer not
				// even to report it. But don't remove the logging without adding another mechanism for
				// the system to know that a problem has been fixed...this controls the important behavior
				// of re-splitting the file before we commit.
				logger(guid, DateTime.Now.ToShortDateString(),
					String.Format(Strings.ksAddingMissingDefaultForValueType, guid));
			}
			return true;
		}
	}
}
