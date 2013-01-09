// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CustomPropertyFixer.cs
// Responsibility: GordonM
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.FixData
{
	/// <summary>
	/// If two users are doing Send/Receive and one deletes a Custom Field (or Custom List!) and
	/// the other edits the same custom property, Chorus will keep the edit, but we have no way to recreate
	/// the custom field, so delete the (now) undefined custom property.
	/// </summary>
	internal class CustomPropertyFixer : RtFixer
	{
		// I used a Dictionary in case we want to someday compare the name of the rt element
		// the custom property is found in with the class attribute in this XElement.
		// They will probably be the same, although the containing rt element could be a subclass.
		readonly Dictionary<string, XElement> m_customFieldNames = new Dictionary<string, XElement>();

		internal override void InspectAdditionalFieldsElement(XElement additionalFieldsElem)
		{
			// LT-13936 - This fixer needs to check to make sure that any rt element
			// Custom entries are for one of these, otherwise they'll get deleted.
			var customFields = additionalFieldsElem.Descendants("CustomField");
			foreach (var field in customFields)
			{
				var nameAttr = field.Attribute("name");
				if (nameAttr == null || string.IsNullOrEmpty(nameAttr.Value))
					continue;
				m_customFieldNames.Add(nameAttr.Value, field);
			}
		}

		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger logger)
		{
			var guid = new Guid(rt.Attribute("guid").Value);
			var xaClass = rt.Attribute("class");
			var className = xaClass == null ? "<unknown>" : xaClass.Value;
			var customProperties = GetAllCustomProperties(rt);
			foreach (var kvp in customProperties)
			{
				if (!string.IsNullOrEmpty(kvp.Key) && m_customFieldNames.ContainsKey(kvp.Key))
					continue;
				logger(guid.ToString(), DateTime.Now.ToShortDateString(),
					String.Format(Strings.ksRemovingUndefinedCustomProperty, kvp.Key, className, guid));
				kvp.Value.Remove();
			}
			return true;
		}

		private Dictionary<string, XElement> GetAllCustomProperties(XElement rt)
		{
			var customProps = new Dictionary<string, XElement>();
			foreach (var customProp in rt.Descendants("Custom"))
			{
				var nameAttr = customProp.Attribute("name");
				var nameKey = (nameAttr == null) ? "" : nameAttr.Value;
				customProps.Add(nameKey, customProp);
			}
			return customProps;
		}
	}
}
