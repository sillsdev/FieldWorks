// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CustomPropertyFixer.cs
// Responsibility: GordonM

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
				var ownerClassAttr = field.Attribute("class");
				if (nameAttr == null || string.IsNullOrEmpty(nameAttr.Value))
					continue;
				// Using the name of the attribute followed by the class will allow the same name to be used on different types i.e. LexEntry and LexSense
				var nameClassKey = nameAttr.Value + @"_" + (ownerClassAttr != null ? ownerClassAttr.Value : "");
				m_customFieldNames.Add(nameClassKey, field);
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
					String.Format(Strings.ksRemovingUndefinedCustomProperty, kvp.Key.Substring(0, kvp.Key.LastIndexOf('_')), className, guid));
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
				XAttribute ownerClassAttr = null;
				string classKey = null;
				if(customProp.Parent != null)
				{
					ownerClassAttr = customProp.Parent.Attribute("class");
				}
				// CustomFields added to Allomorphs get a class of MoForm in the CustomField Element
				// however the class on the rt element that owns the Custom property can be either MoStemAllomorph or MoAffixForm
				// Setting the classKey here to "MoForm" in those cases will prevent these properties from being mistaken as dangling
				if(ownerClassAttr != null && !string.IsNullOrEmpty(ownerClassAttr.Value) && ownerClassAttr.Value.StartsWith("Mo"))
				{
					classKey = "MoForm";
				}
				var nameClassKey = nameKey + "_" + (classKey ?? (ownerClassAttr != null ? ownerClassAttr.Value : ""));
				customProps.Add(nameClassKey, customProp);
			}
			return customProps;
		}
	}
}
