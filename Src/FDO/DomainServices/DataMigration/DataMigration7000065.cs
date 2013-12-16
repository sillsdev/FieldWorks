// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000065.cs
// Responsibility: gordonm

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000064 to 7000065.
	///
	/// Add a valid Name to any Reversal Index's PartOfSpeech possibility list in older projects.
	/// Make sure that ItemClsid is set to 5049 'PartOfSpeech'.
	/// Make sure that IsSorted is set to 'true'.
	/// </summary>
	/// <remarks>
	/// N/A
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000065 : IDataMigration
	{
		private const int PosClsId = PartOfSpeechTags.kClassId;
		private const string Name = "Name";
		private const string Auni = "AUni";
		private const string SortProp = "IsSorted";
		private const string ClsProp = "ItemClsid";
		private const string Truth = "True";
		private const string DepthProp = "Depth";

		#region IDataMigration Members

		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000064);

			var revIndexList = domainObjectDtoRepository.AllInstancesSansSubclasses("ReversalIndex");
			foreach (var revIndexDto in revIndexList)
			{
				var dirty = false;
				// grab ws name(s) from each Reversal Index
				var wsCodeNameDict = new Dictionary<string, string>();
				GetWsNamesFromReversalIndex(revIndexDto, wsCodeNameDict);

				// Find PartsOfSpeech possibility list
				string possListGuid;
				if (!GetGuidForPosListSafely(revIndexDto, out possListGuid))
					continue; // Can't find the PartsOfSpeech list without a guid!
				var posListDto = domainObjectDtoRepository.GetDTO(possListGuid);
				var listElt = XElement.Parse(posListDto.Xml);

				// Check for the right Depth, ItemClsid and IsSorted values
				var depthElt = listElt.Element(DepthProp);
				if (depthElt != null)
					depthElt.Remove(); // Don't want the old one, it was wrong!
				// Replace with a new Property
				var sbDepth = new StringBuilder();
				sbDepth.Append(OpenCloseTagWithValAttr(DepthProp, "127"));
				depthElt = XElement.Parse(sbDepth.ToString());
				listElt.Add(depthElt);
				var clsIdElt = listElt.Element(ClsProp);
				if (clsIdElt == null) // Don't replace an existing value
				{
					// Create the new property
					var sb = new StringBuilder();
					sb.Append(OpenCloseTagWithValAttr(ClsProp, Convert.ToString(PosClsId)));
					clsIdElt = XElement.Parse(sb.ToString());
					listElt.Add(clsIdElt);
					dirty = true;
				}
				var sortedElt = listElt.Element(SortProp);
				if (sortedElt == null)
				{
					var sb = new StringBuilder();
					sb.Append(OpenCloseTagWithValAttr(SortProp, Truth));
					sortedElt = XElement.Parse(sb.ToString());
					listElt.Add(sortedElt);
					dirty = true;
				}
				if (sortedElt.Attribute("val").Value != Truth)
				{
					sortedElt.SetAttributeValue("val", Truth);
					dirty = true;
				}

				// If Name exists skip it, otherwise put a valid name in for each ws that RI Name had.
				var nameElt = listElt.Element(Name);
				if (nameElt == null || nameElt.Element(Auni) == null)
				{
					if (nameElt != null)
						nameElt.Remove(); // Just in case we end up with an empty name element
					var sb = new StringBuilder();
					sb.Append(OpenTag(Name));
					foreach (var kvp in wsCodeNameDict)
					{
						var wsCode = kvp.Key;
						var wsName = kvp.Value;
						sb.AppendFormat(OpenTagWithWsAttr(Auni, wsCode));
						sb.AppendFormat(Strings.ksReversalIndexPOSListName, wsName);
						sb.Append(EndTag(Auni));
					}
					sb.Append(EndTag(Name));
					nameElt = XElement.Parse(sb.ToString());
					listElt.Add(nameElt);
					dirty = true;
				}
				if (dirty)
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, posListDto, listElt.ToString());
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		private static void GetWsNamesFromReversalIndex(DomainObjectDTO revIndexDto, Dictionary<string, string> wsCodeNameDict )
		{
			var wsElt = XElement.Parse(revIndexDto.Xml).Element(Name);
			var existingNameAUniElts = wsElt.Elements(Auni);
			foreach (var aUniElt in existingNameAUniElts)
			{
				var wsCode = aUniElt.Attribute("ws").Value;
				var wsUiStr = aUniElt.Value;
				wsCodeNameDict.Add(wsCode, wsUiStr);
			}
		}

		private static bool GetGuidForPosListSafely(DomainObjectDTO revIndexDto, out string possListGuid)
		{
			possListGuid = string.Empty;
			var posElt = XElement.Parse(revIndexDto.Xml).Element("PartsOfSpeech");
			if (posElt == null)
				return false;

			var objsurElt = posElt.Element("objsur");
			if (objsurElt == null)
				return false;

			possListGuid = GetGuid(objsurElt);
			return true;
		}

		private static string GetGuid(XElement rootElement)
		{
			return rootElement.Attribute("guid").Value.ToLowerInvariant();
		}

		private static string OpenCloseTagWithValAttr(string tagName, string value)
		{
			return string.Format("<{0} val=\"{1}\" />", tagName, value);
		}

		private static string OpenTagWithWsAttr(string tagName, string wsValue)
		{
			return string.Format("<{0} ws=\"{1}\" >", tagName, wsValue);
		}

		private static string OpenTag(string tagName)
		{
			return "<" + tagName + ">";
		}

		private static string EndTag(string tagName)
		{
			return "</" + tagName + ">";
		}

		#endregion
	}
}
