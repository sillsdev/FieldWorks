using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000065 to 7000066.
	///
	/// Redux of DM64, but using a real DM. This will make sure that all missing
	/// xml elements for basic data properties (including basic custom properties)
	/// are included in the xml.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000066 : IDataMigration
	{
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000065);

			// Cache all basic data properties for each class in the mdc.
			var mdc = domainObjectDtoRepository.MDC;
			var cachedBasicProperties = CacheBasicProperties(mdc);
			foreach (var kvp in cachedBasicProperties)
			{
				var className = kvp.Key;
				if (mdc.GetAbstract(mdc.GetClassId(className)))
					continue; // Won't find any of those as dtos.

				var basicProps = kvp.Value;
				foreach (var dto in domainObjectDtoRepository.AllInstancesSansSubclasses(className))
				{
					var rootElementChanged = false;
					var rootElement = XElement.Parse(dto.Xml);
					foreach (var basicPropertyInfo in basicProps)
					{
						if (basicPropertyInfo.m_isCustom)
						{
							var customPropElement = rootElement.Elements("Custom").FirstOrDefault(element => element.Attribute("name").Value == basicPropertyInfo.m_propertyName);
							if (customPropElement == null)
							{
								CreateCustomProperty(rootElement, basicPropertyInfo);
								rootElementChanged = true;
							}
						}
						else
						{
							var basicPropertyElement = rootElement.Element(basicPropertyInfo.m_propertyName);
							if (basicPropertyElement == null && !SkipTheseBasicPropertyNames.Contains(basicPropertyInfo.m_propertyName) && !basicPropertyInfo.m_isVirtual)
							{
								CreateBasicProperty(rootElement, basicPropertyInfo);
								rootElementChanged = true;
							}
						}
					}
					if (!rootElementChanged)
						continue;

					dto.Xml = rootElement.ToString();
					domainObjectDtoRepository.Update(dto);
				}
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		private static HashSet<string> SkipTheseBasicPropertyNames
		{
			get { return new HashSet<string> { "ClassID", "Guid", "OwningFlid", "OwnOrd" }; }
		}

		private static void CreateCustomProperty(XElement rootElement, PropertyInfo basicCustomPropertyInfo)
		{
			rootElement.Add(new XElement("Custom",
				new XAttribute("name", basicCustomPropertyInfo.m_propertyName),
				CreateValAttribute(basicCustomPropertyInfo.m_propertyType)));
		}

		private static void CreateBasicProperty(XElement rootElement, PropertyInfo basicPropertyInfo)
		{
			rootElement.Add(new XElement(basicPropertyInfo.m_propertyName,
				CreateValAttribute(basicPropertyInfo.m_propertyType)));
		}

		private static XAttribute CreateValAttribute(CellarPropertyType propertyType)
		{
			return new XAttribute("val", GetDefaultValueForPropertyType(propertyType));
		}

		private static string GetDefaultValueForPropertyType(CellarPropertyType propertyType)
		{
			switch (propertyType)
			{
				case CellarPropertyType.Boolean:
					return "False";
				case CellarPropertyType.GenDate:
					var genDate = new GenDate();
					return string.Format("{0}{1:0000}{2:00}{3:00}{4}",
						genDate.IsAD ? "" : "-",
						genDate.Year,
						genDate.Month,
						genDate.Day,
						(int)genDate.Precision);
				case CellarPropertyType.Guid:
					return Guid.Empty.ToString();
				case CellarPropertyType.Float:
					throw new NotSupportedException("The 'Float' data type is not supported in the FW data model yet (as of 23 March 2013).");
				case CellarPropertyType.Integer:
					return "0";
				case CellarPropertyType.Time:
					var datetime = new DateTime();
					datetime = datetime.ToUniversalTime(); // Store it as UTC.
					return String.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}",
						datetime.Year,
						datetime.Month,
						datetime.Day,
						datetime.Hour,
						datetime.Minute,
						datetime.Second,
						datetime.Millisecond);
				case CellarPropertyType.Numeric:
					throw new NotSupportedException("The 'Numeric' data type is not supported in the FW data model yet (as of 23 March 2013).");
				default:
					throw new InvalidOperationException("The given 'propertyType' is not a basic data type.");
			}
		}

		private static Dictionary<string, List<PropertyInfo>> CacheBasicProperties(IFwMetaDataCacheManaged mdc)
		{
			var cachedBasicProperties = new Dictionary<string, List<PropertyInfo>>();
			foreach (var classId in mdc.GetClassIds())
			{
				var className = mdc.GetClassName(classId);
				List<PropertyInfo> basicProps;
				if (!cachedBasicProperties.TryGetValue(className, out basicProps))
				{
					basicProps = new List<PropertyInfo>();
					cachedBasicProperties.Add(className, basicProps);
				}
				basicProps.AddRange(mdc.GetFields(classId, className != "CmObject", (int)CellarPropertyTypeFilter.AllBasic).Select(propId => new PropertyInfo
					{
						m_propertyName = mdc.GetFieldName(propId),
						m_propertyType = (CellarPropertyType)mdc.GetFieldType(propId),
						m_isCustom = mdc.IsCustom(propId),
						m_isVirtual = mdc.get_IsVirtual(propId)
					}));
				if (basicProps.Count == 0)
					cachedBasicProperties.Remove(className);
			}
			return cachedBasicProperties;
		}

		private class PropertyInfo
		{
			internal string m_propertyName;
			internal CellarPropertyType m_propertyType;
			internal bool m_isCustom;
			internal bool m_isVirtual;
		}
	}
}