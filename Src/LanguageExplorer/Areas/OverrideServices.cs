// Copyright (c) 2016 SIL International
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
	}
}
