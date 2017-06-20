// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml.Linq;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Services that override XML configurations
	/// </summary>
	internal static class OverrideServices
	{
		/// <summary>
		/// Override 'visibility' attribute in 'column' child elements of <paramref name="sourceElement"/> with new values in <paramref name="overridesContainingElement"/>.
		/// </summary>
		internal static void OverrideVisibiltyAttributes(XElement sourceElement, XElement overridesContainingElement)
		{
			const string layout = "layout";
			const string visibility = "visibility";
			foreach (var overrideElement in overridesContainingElement.Elements())
			{
				var layoutKey = overrideElement.Attribute(layout).Value;
				var matchingSourceElement = sourceElement.Elements("column").FirstOrDefault(column => column.Attribute(layout).Value == layoutKey);
				if (matchingSourceElement == null)
					continue;
				var overrideVisibilityAttr = overrideElement.Attribute(visibility);
				var sourceVisbilityAttribute = matchingSourceElement.Attribute(visibility);
				if (sourceVisbilityAttribute == null)
				{
					matchingSourceElement.Add(overrideVisibilityAttr);
				}
				else
				{
					sourceVisbilityAttribute.Value = overrideVisibilityAttr.Value;
				}
			}
		}

#if RANDYTODO
		// TODO: revise this code to use XElements, and add it in a new, general, override method, and have all tools use it that do overrides of any type.
				// this is a group of overrides, so alter matched nodes accordingly.
				// treat the first three element parts (element and first attribute) as a node query key,
				// and subsequent attributes as subsitutions.
				foreach (XmlNode overrideNode in overridesNode.ChildNodes)
				{
					// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
					// is marked with [MonoTODO] and might not work as expected in 4.0.
					if (overrideNode.GetType() == typeof(XmlComment))
						continue;
					string elementKey = overrideNode.Name;
					string firstAttributeKey = overrideNode.Attributes[0].Name;
					string firstAttributeValue = overrideNode.Attributes[0].Value;
					string xPathToModifyElement = String.Format(".//{0}[@{1}='{2}']", elementKey, firstAttributeKey, firstAttributeValue);
					XmlNode elementToModify = parentNode.SelectSingleNode(xPathToModifyElement);
					if (elementToModify != null && elementToModify != overrideNode)
					{
						if (overrideNode.ChildNodes.Count > 0)
						{
							// replace the elementToModify with this overrideNode.
							XmlNode parentToModify = elementToModify.ParentNode;
							parentToModify.ReplaceChild(overrideNode.Clone(), elementToModify);
						}
						else
						{
							// just modify existing attributes or add new ones.
							foreach (XmlAttribute xaOverride in overrideNode.Attributes)
							{
								// the keyAttribute will be identical, so it won't change.
								XmlAttribute xaToModify = elementToModify.Attributes[xaOverride.Name];
								// if the attribute exists on the node we're modifying, alter it
								// otherwise add the new attribute.
								if (xaToModify != null)
									xaToModify.Value = xaOverride.Value;
								else
									elementToModify.Attributes.Append(xaOverride.Clone() as XmlAttribute);
							}
						}
					}
				}
#endif
	}
}
