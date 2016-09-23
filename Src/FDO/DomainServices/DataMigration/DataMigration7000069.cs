// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 1) Fix the Restrictions field for both LexEntry and LexSense to be MultiString (AStr)
	///    instead of MultiUnicode (AUni).
	///
	/// 2) Add a MultiString (AUni) ReverseName field to LexEntryType, both Variant Type and
	///    Complex Form Type. The migration fills in the 'en' ws contents with Name.Value + " of".
	///    Swap Abbreviation and ReverseAbbr fields. By swapping the fields the "of" will be switched
	///    to the other reference.
	///
	/// 3) Add a MultiString UsageNote and MuliUnicode Exemplar field in LexSense, allowing for multiple ws
	///    attributes in their run. If user has created a Custom "UsageNote" or "Exemplar", then copy the data
	///    into the new field and remove the Custom field.
	///
	/// 4) Remove LexEntryRefs (Complex Forms and Variants) with no ComponentLexemes (they mean nothing)
	///
	/// 5) Add 4 fields to LexEtymology and make LexEntry->Etymology an owning sequence (instead of atomic).
	///    Added fields are: PrecComment, Language, Bibliography and Note.
	///    Remove Source and put its data in Language, other added fields are empty.
	///    Existing fields Form and Gloss are changed from MultiUnicode to MultiString.
	///
	/// 6) Add a new property to LexSense; owning sequence of LexExtendedNote.
	///    Add a new property to LexDb; owned atomic CmPossibilityList called ExtendedNoteTypes
	///    Add a new class LexExtendedNote with properties:
	///      - ExtendedNoteType: atomic reference to CmPossiblity owned by ExtendedNoteTypes list
	///      - Discussion: all analysis MultiString
	///      - Examples: owning sequence of LexExample
	///    Add the ExtendedNoteTypes CmPossibility list with 5 default entries
	///
	/// 7) Add an empty DialectLabels CmPossibility list to LexDb.
	///    Add reference sequence properties called DialectLabels to both LexEntry and LexSense.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000069 : IDataMigration
	{
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000068);

			UpdateRestrictions(repoDto);
			RemoveEmptyLexEntryRefs(repoDto);
			AddDefaultLexEntryRefType(repoDto);
			AddReverseNameAndSwapAbbreviationFields(repoDto);
			MigrateIntoNewMultistringField(repoDto, "Exemplar");
			MigrateIntoNewMultistringField(repoDto, "UsageNote");
			AugmentEtymologyCluster(repoDto);
			AddNewExtendedNoteCluster(repoDto);
			AddDialectLabelsList(repoDto);
			//VerifyExistenceOfMinimalPublicationType(repoDto); DM 7000041 does this already

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		/// <summary>
		/// Add LexDb.ExtendedNoteTypes possibility list to data
		/// </summary>
		/// <remarks>internal for testing</remarks>
		internal static void AddNewExtendedNoteCluster(IDomainObjectDTORepository repoDto)
		{
			const string extNoteListGuid = "ed6b2dcc-e82f-4631-b61a-6b630de332d0";

			var lexDbDTO = repoDto.AllInstancesSansSubclasses("LexDb").FirstOrDefault();
			if (lexDbDTO == null)
				return; // This must be a test that doesn't care about LexDb.
			var lexDbElt = XElement.Parse(lexDbDTO.Xml);
			if (lexDbElt.Element("ExtendedNoteTypes") != null)
				return; // probably a test involving NewLangProj which is still languishing at v7000062
			CreateNewLexDbProperty(lexDbElt, "ExtendedNoteTypes", extNoteListGuid);
			var lexDbGuid = lexDbElt.Attribute("guid").Value;

			// create ExtendedNoteTypes' possibility list
			var sb = new StringBuilder();
			sb.AppendFormat("<rt class=\"CmPossibilityList\" guid=\"{0}\" ownerguid=\"{1}\">", extNoteListGuid,
							lexDbGuid);
			sb.Append("<Abbreviation>");
			sb.Append("<AUni ws=\"en\">ExtNoteTyp</AUni>");
			sb.Append("</Abbreviation>");
			sb.Append("<DateCreated val=\"2016-06-27 18:48:18.679\" />");
			sb.Append("<DateModified val=\"2016-06-27 18:48:18.679\" />");
			sb.Append("<Depth val=\"1\" />");
			sb.Append("<IsSorted val=\"True\" />");
			sb.Append("<ItemClsid val=\"7\" />");
			sb.Append("<Name>");
			sb.Append("<AUni ws=\"en\">Extended Note Types</AUni>");
			sb.Append("</Name>");
			sb.Append("<Possibilities>");
			sb.Append("<objsur guid=\"2f06d436-b1e0-47ae-a42e-1f7b893c5fc2\" t=\"o\" />");
			sb.Append("<objsur guid=\"7ad06e7d-15d1-42b0-ae19-9c05b7c0b181\" t=\"o\" />");
			sb.Append("<objsur guid=\"30115b33-608a-4506-9f9c-2457cab4f4a8\" t=\"o\" />");
			sb.Append("<objsur guid=\"d3d28628-60c9-4917-8185-ba64c59f20c4\" t=\"o\" />");
			sb.Append("</Possibilities>");
			sb.Append("<PreventDuplicates val=\"True\" />");
			sb.Append("<WsSelector val=\"-3\" />");
			sb.Append("</rt>");
			var newCmPossibilityListElt = XElement.Parse(sb.ToString());
			var dtoCmPossibilityList = new DomainObjectDTO(extNoteListGuid, "CmPossibilityList", newCmPossibilityListElt.ToString());
			repoDto.Add(dtoCmPossibilityList);

			// Now add our 5 default possibilities
			CreatePossibility(repoDto, extNoteListGuid, "2f06d436-b1e0-47ae-a42e-1f7b893c5fc2", "Collocation", "Coll.");
			CreatePossibility(repoDto, extNoteListGuid, "7ad06e7d-15d1-42b0-ae19-9c05b7c0b181", "Cultural", "Cult.");
			CreatePossibility(repoDto, extNoteListGuid, "30115b33-608a-4506-9f9c-2457cab4f4a8", "Grammar", "Gram.");
			CreatePossibility(repoDto, extNoteListGuid, "d3d28628-60c9-4917-8185-ba64c59f20c4", "Inflectional", "Infl.");

			DataMigrationServices.UpdateDTO(repoDto, lexDbDTO, lexDbElt.ToString());
		}

		private static void CreatePossibility(IDomainObjectDTORepository repoDto, string listGuid, string possibilityGuid,
			string name, string abbr)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("<rt class=\"CmPossibility\" guid=\"{0}\" ownerguid=\"{1}\">", possibilityGuid, listGuid);
			sb.Append("<Abbreviation>");
			sb.AppendFormat("<AUni ws=\"en\">{0}</AUni>", abbr);
			sb.Append("</Abbreviation>");
			sb.Append("<DateCreated val=\"2016-06-27 18:48:18.679\" />");
			sb.Append("<DateModified val=\"2016-06-27 18:48:18.679\" />");
			sb.Append("<BackColor val=\"-1073741824\" />");
			sb.Append("<ForeColor val=\"-1073741824\" />");
			sb.Append("<IsProtected val=\"True\" />");
			sb.Append("<Name>");
			sb.AppendFormat("<AUni ws=\"en\">{0}</AUni>", name);
			sb.Append("</Name>");
			sb.Append("<UnderColor val=\"-1073741824\" />");
			sb.Append("</rt>");
			var newCmPossibilityElt = XElement.Parse(sb.ToString());
			var dtoCmPossibility = new DomainObjectDTO(possibilityGuid, "CmPossibility", newCmPossibilityElt.ToString());
			repoDto.Add(dtoCmPossibility);
		}

		private static void CreateNewLexDbProperty(XElement lexDbElt, string propName, string listGuid)
		{
			lexDbElt.Add(new XElement(propName,
							new XElement("objsur",
								new XAttribute("guid", listGuid),
								new XAttribute("t", "o"))));
		}

		/// <summary>
		/// Change LexEntry.Etymology to Owned Sequence, add several fields to LexEtymology
		/// Change some existing field signatures. Remove Source and put its data in LanguageNotes
		/// in a slightly different format (Unicode -> MultiString). Also add a new list Languages
		/// owned by LexDb. Languages list will be initially empty.
		/// </summary>
		/// <remarks>internal for testing</remarks>
		internal static void AugmentEtymologyCluster(IDomainObjectDTORepository repoDto)
		{
			const string languagesListGuid = "487c15b0-2ced-4417-8b77-9075f4a21e5f";
			var lexDbDTO = repoDto.AllInstancesSansSubclasses("LexDb").FirstOrDefault();
			if (lexDbDTO == null)
				return; // This must be a test that doesn't care about LexDb.
			var lexDbElt = XElement.Parse(lexDbDTO.Xml);
			CreateNewLexDbProperty(lexDbElt, "Languages", languagesListGuid);
			var lexDbGuid = lexDbElt.Attribute("guid").Value;

			// create Languages' possibility list
			var sb = new StringBuilder();
			sb.AppendFormat("<rt class=\"CmPossibilityList\" guid=\"{0}\" ownerguid=\"{1}\">", languagesListGuid,
							lexDbGuid);
			sb.Append("<Abbreviation>");
			sb.Append("<AUni ws=\"en\">Lgs</AUni>");
			sb.Append("<AUni ws=\"es\">Ids</AUni>");
			sb.Append("<AUni ws=\"fr\">Lgs</AUni>");
			sb.Append("</Abbreviation>");
			sb.Append("<DateCreated val=\"2016-07-25 18:48:18.679\" />");
			sb.Append("<DateModified val=\"2016-07-25 18:48:18.679\" />");
			sb.Append("<Depth val=\"1\" />");
			sb.Append("<IsSorted val=\"True\" />");
			sb.Append("<ItemClsid val=\"7\" />");
			sb.Append("<Name>");
			sb.Append("<AUni ws=\"en\">Languages</AUni>");
			sb.Append("<AUni ws=\"es\">Idiomas</AUni>");
			sb.Append("<AUni ws=\"fr\">Langues</AUni>");
			sb.Append("</Name>");
			sb.Append("<PreventDuplicates val=\"True\" />");
			sb.Append("<WsSelector val=\"-3\" />");
			sb.Append("</rt>");
			// We purposefully didn't add any Possibilities element.
			var newCmPossibilityListElt = XElement.Parse(sb.ToString());
			var dtoCmPossibilityList = new DomainObjectDTO(languagesListGuid, "CmPossibilityList", newCmPossibilityListElt.ToString());
			repoDto.Add(dtoCmPossibilityList);
			DataMigrationServices.UpdateDTO(repoDto, lexDbDTO, lexDbElt.ToString()); // update LexDb object

			// Augment existing Etymologies
			var etymologyDtos = repoDto.AllInstancesSansSubclasses("LexEtymology");
			if (!etymologyDtos.Any())
				return;
			var primaryAnalysisWs = ExtractPrimaryWsFromLangProj(repoDto, true);
			foreach (var etymologyDto in etymologyDtos)
			{
				ChangeMultiUnicodeElementToMultiString(repoDto, etymologyDto, "//Form");
				ChangeMultiUnicodeElementToMultiString(repoDto, etymologyDto, "//Gloss");
				var dataElt = XElement.Parse(etymologyDto.Xml);
				var sourceElt = dataElt.Element("Source");
				if (sourceElt == null)
					continue;
				sourceElt.Name = "LanguageNotes"; // sourceElt is now the languageNotesElt!
				var oldSourceData = sourceElt.Element("Uni").Value;
				var multiStrElt = BuildMultiStringElement(primaryAnalysisWs, oldSourceData);
				sourceElt.RemoveAll();
				sourceElt.Add(multiStrElt);
				DataMigrationServices.UpdateDTO(repoDto, etymologyDto, dataElt.ToString());
			}
		}

		private static string ExtractPrimaryWsFromLangProj(IDomainObjectDTORepository dtoRepos, bool analysis)
		{
			const string defaultWs = "en";
			var propName = analysis ? "CurAnalysisWss" : "CurVernWss";
			var langProj = dtoRepos.AllInstancesSansSubclasses("LangProject").FirstOrDefault();
			if (langProj == null) // We must be in a test! A real test that cares should have a LangProject element!
				return defaultWs;
			var propString = XElement.Parse(langProj.Xml).Element(propName).Element("Uni").Value;
			return propString.Split(' ')[0];
		}

		private static XElement BuildMultiStringElement(string primaryAnalysisWs, string oldSourceData)
		{
			var runElt = new XElement("Run");
			runElt.SetAttributeValue("ws", primaryAnalysisWs);
			runElt.SetValue(oldSourceData);
			var astrElt = new XElement("AStr");
			astrElt.SetAttributeValue("ws", primaryAnalysisWs);
			astrElt.Add(runElt);
			return astrElt;
		}

		/// <summary>We update every instance of a Restriction in a LexEntry or a LexSense to change from AUni to AStr.</summary>
		private static void UpdateRestrictions(IDomainObjectDTORepository repoDto)
		{
			var xpathToRestrictionsElt = "//Restrictions";
			foreach (var entryOrSenseDto in repoDto.AllInstancesSansSubclasses("LexEntry").Union(repoDto.AllInstancesSansSubclasses("LexSense")))
			{
				ChangeMultiUnicodeElementToMultiString(repoDto, entryOrSenseDto, xpathToRestrictionsElt);
			}
		}

		private static void ChangeMultiUnicodeElementToMultiString(IDomainObjectDTORepository repoDto, DomainObjectDTO dto,
			string xpathToMultiUnicodeElement)
		{
			const string auniXpath = "/AUni";
			var changed = false;
			var dataElt = XElement.Parse(dto.Xml);
			foreach (var elt in dataElt.XPathSelectElements(xpathToMultiUnicodeElement + auniXpath))
			{
				elt.Name = "AStr";
				var unicodeData = elt.Value;
				elt.Value = string.Empty;
				var wsAttr = elt.Attribute("ws");
				var runElt = new XElement("Run") {Value = unicodeData};
				runElt.SetAttributeValue("ws", wsAttr.Value);
				elt.Add(runElt);
				changed = true;
			}
			if (changed)
			{
				DataMigrationServices.UpdateDTO(repoDto, dto, dataElt.ToString());
			}
		}

		internal static void AddReverseNameAndSwapAbbreviationFields(IDomainObjectDTORepository repoDto)
		{
			// We DO want subclasses (e.g. LexEntryInflType)
			foreach (var dto in repoDto.AllInstancesWithSubclasses("LexEntryType"))
			{
				var data = XElement.Parse(dto.Xml);

				var nameElt = data.Element("Name");
				if (nameElt != null)
				{
					var nameAUniElt = nameElt.Elements("AUni").FirstOrDefault(elt => elt.Attribute("ws").Value == "en");
					if (nameAUniElt != null)
					{
						var revNameElt = new XElement("ReverseName");
						var auni = new XElement("AUni");
						auni.SetAttributeValue("ws", "en");
						auni.Value = nameAUniElt.Value + " of";
						revNameElt.Add(auni);
						data.Add(revNameElt);
					}
				}

				var abbrevElt = data.Element("Abbreviation");
				var revAbbrElt = data.Element("ReverseAbbr");
				if (abbrevElt != null)
					abbrevElt.Name = "ReverseAbbr";
				if (revAbbrElt != null)
					revAbbrElt.Name = "Abbreviation";

				DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
			}
		}

		internal static void RemoveEmptyLexEntryRefs(IDomainObjectDTORepository repoDto)
		{
			foreach (var dto in repoDto.AllInstancesWithSubclasses("LexEntryRef"))
			{
				XElement data = XElement.Parse(dto.Xml);

				var components = data.Element("ComponentLexemes");
				if (components == null || !components.HasElements)
				{
					DataMigrationServices.RemoveIncludingOwnedObjects(repoDto, dto, true);
				}
			}
		}

		internal static void AddDefaultLexEntryRefType(IDomainObjectDTORepository repoDto)
		{
			var cmPossListGuidForComplexFormType = "1ee09905-63dd-4c7a-a9bd-1d496743ccd6";
			var cmPossListGuidForVariantEntryType = "bb372467-5230-43ef-9cc7-4d40b053fb94";
			const string unspecComplexEntryTypeGuid = "fec038ed-6a8c-4fa5-bc96-a4f515a98c50";
			const string unspecVariantEntryTypeGuid = "3942addb-99fd-43e9-ab7d-99025ceb0d4e";
			foreach (var dto in repoDto.AllInstancesWithSubclasses("LexEntryRef"))
			{
				var data = XElement.Parse(dto.Xml);
				var refTypeElt = data.Element("RefType");
				var varientEntryTypeElt = data.Element("VariantEntryTypes");
				var complexEntryTypeElt = data.Element("ComplexEntryTypes");
				if (refTypeElt == null) continue;
				if (refTypeElt.FirstAttribute.Value == "0" && complexEntryTypeElt == null)
				{
					AddRefType(data, repoDto, dto, "ComplexEntryTypes", unspecComplexEntryTypeGuid);
				}
				else if (refTypeElt.FirstAttribute.Value == "1" && varientEntryTypeElt == null)
				{
					AddRefType(data, repoDto, dto, "VariantEntryTypes", unspecVariantEntryTypeGuid);
				}
			}

			foreach (var dto in repoDto.AllInstancesWithSubclasses("CmPossibilityList"))
			{
				var data = XElement.Parse(dto.Xml);
				var nameElt = data.Element("Name");
				if (nameElt != null && nameElt.Value.StartsWith("Variant Types"))
				{
					cmPossListGuidForVariantEntryType = dto.Guid;
					AddRefType(data, repoDto, dto, "Possibilities", unspecVariantEntryTypeGuid);
				}
				else if (nameElt != null && nameElt.Value.StartsWith("Complex Form Types"))
				{
					cmPossListGuidForComplexFormType = dto.Guid;
					AddRefType(data, repoDto, dto, "Possibilities", unspecComplexEntryTypeGuid);
				}
			}

			string variantXml = "<rt class=\"LexEntryType\" guid=\"" + unspecVariantEntryTypeGuid + "\"" +
			" ownerguid=\"" + cmPossListGuidForVariantEntryType + "\"><Abbreviation><AUni ws=\"en\">unspec. var. of</AUni></" +
			"Abbreviation><IsProtected val=\"true\" /><Name><AUni ws=\"en\">Unspecified Variant</AUni></" +
			"Name><ReverseAbbr><AUni ws=\"en\">unspec. var.</AUni></ReverseAbbr></rt>";
			var newDefaultVariantType = new DomainObjectDTO(unspecVariantEntryTypeGuid, "LexEntryType", variantXml);
			repoDto.Add(newDefaultVariantType);

			string complexXml = "<rt class=\"LexEntryType\" guid=\"" + unspecComplexEntryTypeGuid + "\"" +
						" ownerguid=\"" + cmPossListGuidForComplexFormType  + "\"><Abbreviation><AUni ws=\"en\">unspec. comp. form of</AUni>" +
						"</Abbreviation><Description><AStr ws=\"en\"><Run ws=\"en\"></Run></AStr></Description><IsProtected val=\"true\"" +
						" /><Name><AUni ws=\"en\">Unspecified Complex Form</AUni></Name>" +
						"<ReverseAbbr><AUni ws=\"en\">unspec. comp. form</AUni></ReverseAbbr></rt>";
			var newDefaultComplexType = new DomainObjectDTO(unspecComplexEntryTypeGuid, "LexEntryType", complexXml);
			repoDto.Add(newDefaultComplexType);
		}

		private static void AddRefType(XElement data, IDomainObjectDTORepository repoDto, DomainObjectDTO dto, string tagName, string guid)
		{
			var varElementTag = data.Element(tagName);
			if (varElementTag == null)
			{
				var varTypeReference = new XElement(tagName);
				var typeReference = new XElement("objsur");
				typeReference.SetAttributeValue("guid", guid);
				varTypeReference.Add(typeReference);
				data.Add(varTypeReference);
				DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
			}
			else
			{
				varElementTag = data.Element(tagName);
				var typeObject = new XElement("objsur");
				typeObject.SetAttributeValue("guid", guid);
				typeObject.SetAttributeValue("t", "o");
				if (varElementTag != null) varElementTag.Add(typeObject);
				DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
			}
		}

		/// <summary>
		/// If user created a Custom "Exemplar" or "UsageNote" of type MultiString or MultiUnicode,
		/// copy that data into the new built-in MultiString element and, if MultiString, remove the Custom Field.
		/// If a conflicting Custom Field cannot be migrated and removed, rename it to avoid conflict.
		/// </summary>
		internal static void MigrateIntoNewMultistringField(IDomainObjectDTORepository repoDto, string fieldName)
		{
			// This is the same algorithm used by FDOBackendProvider.PreLoadCustomFields to prevent naming conflicts with existing Custom Fields
			var nameSuffix = 0;
			var lexSenseClid = repoDto.MDC.GetClassId("LexSense");
			while (repoDto.MDC.FieldExists(lexSenseClid, fieldName + nameSuffix, false))
				++nameSuffix;
			var newFieldName = fieldName + nameSuffix;

			foreach (var dto in repoDto.AllInstancesSansSubclasses("LexSense"))
			{
				var data = XElement.Parse(dto.Xml);

				var customElt = data.Elements("Custom").FirstOrDefault(elt => elt.Attribute("name").Value == fieldName);
				if (customElt == null)
					continue;
				customElt.SetAttributeValue("name", newFieldName); // rename to the new custom Exemplar name

				var builtInElt = new XElement(fieldName);
				var isFieldBuiltIn = false;
				foreach (var multiStrElt in customElt.Elements("AStr"))
				{
					builtInElt.Add(multiStrElt);
					isFieldBuiltIn = true;
				}
				foreach (var multiStrElt in customElt.Elements("AUni"))
				{
					multiStrElt.Name = "AStr";
					var mutiStrData = multiStrElt.Value;
					multiStrElt.Value = string.Empty;
					var wsAttr = multiStrElt.Attribute("ws");
					var runElt = new XElement("Run") { Value = mutiStrData };
					runElt.SetAttributeValue("ws", wsAttr.Value);
					multiStrElt.Add(runElt);
					builtInElt.Add(multiStrElt);
					isFieldBuiltIn = true;
				}
				if (isFieldBuiltIn)
				{
					customElt.Remove();
					data.Add(builtInElt);
				}
				DataMigrationServices.UpdateDTO(repoDto, dto, data.ToString());
			}
		}

		/// <summary>
		/// Add a new list DialectLabels owned by LexDb. The list will be initially empty.
		/// The migration will also add a reference sequence property to LexEntry and
		/// LexSense, but since they will be empty too, there's no migration to happen here.
		/// </summary>
		/// <remarks>internal for testing</remarks>
		internal static void AddDialectLabelsList(IDomainObjectDTORepository repoDto)
		{
			const string dialectListGuid = "a3a8188b-ab00-4a43-b925-a1eed62287ba";
			var lexDbDTO = repoDto.AllInstancesSansSubclasses("LexDb").FirstOrDefault();
			if (lexDbDTO == null)
				return; // This must be a test that doesn't care about LexDb.
			var lexDbElt = XElement.Parse(lexDbDTO.Xml);
			CreateNewLexDbProperty(lexDbElt, "DialectLabels", dialectListGuid);
			var lexDbGuid = lexDbElt.Attribute("guid").Value;

			// create Languages' possibility list
			var sb = new StringBuilder();
			sb.AppendFormat("<rt class=\"CmPossibilityList\" guid=\"{0}\" ownerguid=\"{1}\">", dialectListGuid,
							lexDbGuid);
			sb.Append("<Abbreviation>");
			sb.Append("<AUni ws=\"en\">Dials</AUni>");
			sb.Append("</Abbreviation>");
			sb.Append("<DateCreated val=\"2016-08-09 18:48:18.679\" />");
			sb.Append("<DateModified val=\"2016-08-09 18:48:18.679\" />");
			sb.Append("<Depth val=\"1\" />");
			sb.Append("<IsSorted val=\"True\" />");
			sb.Append("<ItemClsid val=\"7\" />");
			sb.Append("<Name>");
			sb.Append("<AUni ws=\"en\">Dialect Labels</AUni>");
			sb.Append("</Name>");
			sb.Append("<PreventDuplicates val=\"True\" />");
			sb.Append("<WsSelector val=\"-6\" />");
			sb.Append("</rt>");
			// We purposefully didn't add any Possibilities element.
			var newCmPossibilityListElt = XElement.Parse(sb.ToString());
			var dtoCmPossibilityList = new DomainObjectDTO(dialectListGuid, "CmPossibilityList", newCmPossibilityListElt.ToString());
			repoDto.Add(dtoCmPossibilityList);
			DataMigrationServices.UpdateDTO(repoDto, lexDbDTO, lexDbElt.ToString()); // update LexDb object
		}
	}
}
