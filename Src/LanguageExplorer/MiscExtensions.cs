// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer
{
	/// <summary>
	/// Extensions on various classes that have no other suitable extension file.
	/// </summary>
	internal static class MiscExtensions
	{
		/// <summary>
		/// Set configuration displayProperty from cmObjectCustomFieldFlid set elements' OwningList DisplayOption.
		///
		/// If cmObjectCustomFieldFlid refers to a set of elements (in cmObject), then examine the setting on the owning list of the
		/// elements to determine which property of each element to use when
		/// displaying each element in a slice, and record that information in configurationElement. This information is used
		/// in DetailControls.VectorReferenceVc.Display().
		/// Addresses LT-15705.
		/// </summary>
		internal static void SetConfigurationDisplayPropertyIfNeeded(this XElement me, ICmObject cmObject, int cmObjectCustomFieldFlid, ISilDataAccess mainCacheAccessor, ILcmServiceLocator lcmServiceLocator, IFwMetaDataCache metadataCache)
		{
			var fieldType = metadataCache.GetFieldType(cmObjectCustomFieldFlid);
			if (!(fieldType == (int)CellarPropertyType.ReferenceCollection || fieldType == (int)CellarPropertyType.OwningCollection || fieldType == (int)CellarPropertyType.ReferenceSequence
				  || fieldType == (int)CellarPropertyType.OwningSequence))
			{
				return;
			}
			var elementCount = mainCacheAccessor.get_VecSize(cmObject.Hvo, cmObjectCustomFieldFlid);
			if (elementCount == 0)
			{
				return;
			}
			if (!(lcmServiceLocator.GetObject(mainCacheAccessor.get_VecItem(cmObject.Hvo, cmObjectCustomFieldFlid, 0)) is ICmPossibility cmPossibility))
			{
				return;
			}
			var displayOption = cmPossibility.OwningList.DisplayOption;
			string propertyNameToGetAndShow = null;
			switch ((PossNameType)displayOption)
			{
				case PossNameType.kpntName:
					propertyNameToGetAndShow = "ShortNameTSS";
					break;
				case PossNameType.kpntNameAndAbbrev:
					propertyNameToGetAndShow = "AbbrAndNameTSS";
					break;
				case PossNameType.kpntAbbreviation:
					propertyNameToGetAndShow = "AbbrevHierarchyString";
					break;
				default:
					break;
			}
			if (string.IsNullOrWhiteSpace(propertyNameToGetAndShow))
			{
				return;
			}
			var displayPropertyAttribute = new XAttribute("displayProperty", propertyNameToGetAndShow);
			var deParamsElement = me.Element("deParams");
			if (deParamsElement == null)
			{
				me.Add(new XElement("deParams", displayPropertyAttribute));
				return;
			}
			if (deParamsElement.Attribute("displayProperty") == null)
			{
				deParamsElement.Add(displayPropertyAttribute);
				return;
			}
			deParamsElement.Attribute("displayProperty").SetValue(propertyNameToGetAndShow);
		}

		/// <summary>
		/// This method determines how much we should indent nodes produced from "part ref"
		/// elements embedded inside an "indent" element in another "part ref" element.
		/// Currently, by default we in fact do NOT add any indent, unless there is also
		/// an attribute indent="true".
		/// </summary>
		/// <returns>0 for no indent, 1 to indent.</returns>
		internal static int ExtraIndent(this XElement me)
		{
			return XmlUtils.GetOptionalBooleanAttributeValue(me, "indent", false) ? 1 : 0;
		}

		internal static ISlice GetSliceParent(this Control me)
		{
			while (true)
			{
				if (me?.Parent == null)
				{
					// 'me' is null, or Parent of 'me' is null.
					return null;
				}
				var myParent = me.Parent;
				if (myParent is ISlice parentAsISlice)
				{
					return parentAsISlice;
				}
				me = myParent;
			}
		}
	}
}