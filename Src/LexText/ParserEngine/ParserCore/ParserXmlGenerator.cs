using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public static class ParserXmlGenerator
	{
		public static void CreateMsaXmlElement(XmlNode node, XmlDocument doc, XmlNode morphNode, string sHvo, FdoCache fdoCache)
		{
			// morphname contains the hvo of the msa
			XmlNode attr = node.SelectSingleNode(sHvo);
			if (attr != null)
			{
				string sObjHvo = attr.Value;
				// Irregulary inflected forms can have a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
				var indexOfPeriod = IndexOfPeriodInMsaHvo(ref sObjHvo);
				ICmObject obj = fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(Convert.ToInt32(sObjHvo));
				switch (obj.GetType().Name)
				{
					default:
						throw new ApplicationException(String.Format("Invalid MSA type: {0}.", obj.GetType().Name));
					case "MoStemMsa":
						var stemMsa = obj as IMoStemMsa;
						CreateStemMsaXmlElement(doc, morphNode, stemMsa, fdoCache);
						break;
					case "MoInflAffMsa":
						var inflMsa = obj as IMoInflAffMsa;
						CreateInflectionClasses(doc, morphNode, fdoCache);
						CreateInflMsaXmlElement(doc, morphNode, inflMsa, fdoCache);
						break;
					case "MoDerivAffMsa":
						var derivMsa = obj as IMoDerivAffMsa;
						CreateDerivMsaXmlElement(doc, morphNode, derivMsa);
						break;
					case "MoUnclassifiedAffixMsa":
						var unclassMsa = obj as IMoUnclassifiedAffixMsa;
						CreateUnclassifedMsaXmlElement(doc, morphNode, unclassMsa);
						break;
					case "LexEntry":
						// is an irregularly inflected form
						// get the MoStemMsa of its variant
						var entry = obj as ILexEntry;
						Debug.Assert(entry != null);
						if (entry.EntryRefsOS.Count > 0)
						{
							var index = IndexOfLexEntryRef(attr.Value, indexOfPeriod);
							var lexEntryRef = entry.EntryRefsOS[index];
							var sense = FDO.DomainServices.MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
							stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
							CreateStemMsaXmlElement(doc, morphNode, stemMsa, fdoCache);
						}
						break;
					case "LexEntryInflType":
						// This is one of the null allomorphs we create when building the
						// input for the parser in order to still get the Word Grammar to have something in any
						// required slots in affix templates.
						CreateInflMsaForLexEntryInflType(doc, morphNode, obj as ILexEntryInflType, fdoCache);
						break;
				}
			}
		}
		public static void CreateMsaXmlElement(XmlWriter writer, string sObjHvo, string sAlloId, string slotHvo, FdoCache fdoCache)
		{
			// Irregulary inflected forms can have a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
			var indexOfPeriod = IndexOfPeriodInMsaHvo(ref sObjHvo);
			ICmObject obj = fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(Convert.ToInt32(sObjHvo));
			switch (obj.GetType().Name)
			{
				default:
					throw new ApplicationException(String.Format("Invalid MSA type: {0}.", obj.GetType().Name));
				case "MoStemMsa":
					var stemMsa = obj as IMoStemMsa;
					CreateStemMsaXmlElement(writer, stemMsa, fdoCache);
					break;
				case "MoInflAffMsa":
					var inflMsa = obj as IMoInflAffMsa;
					CreateInflectionClasses(writer, sAlloId, fdoCache);
					CreateInflMsaXmlElement(writer, inflMsa, slotHvo, fdoCache);
					break;
				case "MoDerivAffMsa":
					var derivMsa = obj as IMoDerivAffMsa;
					CreateDerivMsaXmlElement(writer, derivMsa);
					break;
				case "MoUnclassifiedAffixMsa":
					var unclassMsa = obj as IMoUnclassifiedAffixMsa;
					CreateUnclassifedMsaXmlElement(writer, unclassMsa);
					break;
				case "LexEntry":
					// is an irregularly inflected form
					// get the MoStemMsa of its variant
					var entry = obj as ILexEntry;
					Debug.Assert(entry != null);
					if (entry.EntryRefsOS.Count > 0)
					{
						var index = IndexOfLexEntryRef(sObjHvo, indexOfPeriod);
						var lexEntryRef = entry.EntryRefsOS[index];
						var sense = FDO.DomainServices.MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
						stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
						CreateStemMsaXmlElement(writer, stemMsa, fdoCache);
					}
					break;
				case "LexEntryInflType":
					// This is one of the null allomorphs we create when building the
					// input for the parser in order to still get the Word Grammar to have something in any
					// required slots in affix templates.
					CreateInflMsaForLexEntryInflType(writer, slotHvo, obj as ILexEntryInflType, fdoCache);
					break;
			}
		}

		private static int IndexOfLexEntryRef(string sHvo, int indexOfPeriod)
		{
			int index = 0;
			if (indexOfPeriod >= 0)
			{
				string sIndex = sHvo.Substring(indexOfPeriod + 1);
				index = Convert.ToInt32(sIndex);
			}
			return index;
		}

		private static int IndexOfPeriodInMsaHvo(ref string sObjHvo)
		{
			// Irregulary inflected forms can a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
			int indexOfPeriod = sObjHvo.IndexOf('.');
			if (indexOfPeriod >= 0)
			{
				sObjHvo = sObjHvo.Substring(0, indexOfPeriod);
			}
			return indexOfPeriod;
		}

		/// <summary>
		/// Determine if a PartOfSpeech requires inflection.
		/// If it or any of its parent POSes have a template, it requires inflection.
		/// If it is null we default to not requiring inflection.
		/// </summary>
		/// <param name="pos">the Part of Speech</param>
		/// <returns>true if it does, false otherwise</returns>
		private static bool RequiresInflection(IPartOfSpeech pos)
		{
			return pos != null && pos.RequiresInflection;
		}

		private static void CreateStemMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoStemMsa stemMsa, FdoCache fdoCache)
		{
			XmlNode stemMsaNode = CreateXmlElement(doc, "stemMsa", morphNode);
			CreatePOSXmlAttribute(doc, stemMsaNode, stemMsa.PartOfSpeechRA, "cat");
			IMoInflClass inflClass = stemMsa.InflectionClassRA;
			if (inflClass == null)
			{ // use default inflection class of the POS or
				// the first ancestor POS that has a non-zero default inflection class
				int inflClassHvo = 0;
				IPartOfSpeech pos = stemMsa.PartOfSpeechRA;
				while (pos != null && inflClassHvo == 0)
				{
					if (pos.DefaultInflectionClassRA != null)
						inflClassHvo = pos.DefaultInflectionClassRA.Hvo;
					else
					{
						int clsid = fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(pos.Owner.Hvo).ClassID;
						pos = clsid == PartOfSpeechTags.kClassId ? fdoCache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(pos.Owner.Hvo) : null;
					}
				}
				if (inflClassHvo != 0)
					inflClass = fdoCache.ServiceLocator.GetInstance<IMoInflClassRepository>().GetObject(inflClassHvo);
			}
			CreateInflectionClassXmlAttribute(doc, stemMsaNode, inflClass, "inflClass");
			CreateRequiresInflectionXmlAttribute(doc,
				stemMsa.PartOfSpeechRA,
				stemMsaNode);
			CreateFeatureStructureNodes(doc, stemMsaNode, stemMsa.MsFeaturesOA, stemMsa.Hvo);
			CreateProductivityRestrictionNodes(doc, stemMsaNode, stemMsa.ProdRestrictRC, "productivityRestriction");
			CreateFromPOSNodes(doc, stemMsaNode, stemMsa.FromPartsOfSpeechRC, "fromPartsOfSpeech");
		}

		private static void CreateStemMsaXmlElement(XmlWriter writer, IMoStemMsa stemMsa, FdoCache fdoCache)
		{
			writer.WriteStartElement("stemMsa");
			CreatePOSXmlAttribute(writer, stemMsa.PartOfSpeechRA, "cat");
			IMoInflClass inflClass = stemMsa.InflectionClassRA;
			if (inflClass == null)
			{ // use default inflection class of the POS or
				// the first ancestor POS that has a non-zero default inflection class
				int inflClassHvo = 0;
				IPartOfSpeech pos = stemMsa.PartOfSpeechRA;
				while (pos != null && inflClassHvo == 0)
				{
					if (pos.DefaultInflectionClassRA != null)
						inflClassHvo = pos.DefaultInflectionClassRA.Hvo;
					else
					{
						int clsid = fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(pos.Owner.Hvo).ClassID;
						pos = clsid == PartOfSpeechTags.kClassId ? fdoCache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(pos.Owner.Hvo) : null;
					}
				}
				if (inflClassHvo != 0)
					inflClass = fdoCache.ServiceLocator.GetInstance<IMoInflClassRepository>().GetObject(inflClassHvo);
			}
			CreateInflectionClassXmlAttribute(writer, inflClass, "inflClass");
			CreateRequiresInflectionXmlAttribute(writer, stemMsa.PartOfSpeechRA);
			CreateFeatureStructureNodes(writer, stemMsa.MsFeaturesOA, stemMsa.Hvo);
			CreateProductivityRestrictionNodes(writer, stemMsa.ProdRestrictRC, "productivityRestriction");
			CreateFromPOSNodes(writer, stemMsa.FromPartsOfSpeechRC, "fromPartsOfSpeech");
			writer.WriteEndElement(); //stemMsa
		}

		private static void CreateInflectionClasses(XmlDocument doc, XmlNode morphNode, FdoCache fdoCache)
		{
			string sAlloId = XmlUtils.GetOptionalAttributeValue(morphNode, "alloid");
			if (sAlloId == null)
				return;
			int hvoAllomorph = Convert.ToInt32(sAlloId);
			// use IMoForm instead of IMoAffixForm or IMoAffixAllomorph because it could be an IMoStemAllomorph
			IMoForm form = fdoCache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(hvoAllomorph);
			if (form == null)
				return;
			if (!(form is IMoAffixForm))
				return;
			foreach (IMoInflClass ic in ((IMoAffixForm)form).InflectionClassesRC)
			{
				XmlNode icNode = CreateXmlElement(doc, "inflectionClass", morphNode);
				CreateXmlAttribute(doc, "id", ic.Hvo.ToString(CultureInfo.InvariantCulture), icNode);
				CreateXmlAttribute(doc, "abbr", ic.Abbreviation.BestAnalysisAlternative.Text, icNode);
			}
		}

		private static void CreateInflectionClasses(XmlWriter writer, string sAlloId, FdoCache fdoCache)
		{
			if (sAlloId == null)
				return;
			int hvoAllomorph = Convert.ToInt32(sAlloId);
			// use IMoForm instead of IMoAffixForm or IMoAffixAllomorph because it could be an IMoStemAllomorph
			IMoForm form = fdoCache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(hvoAllomorph);
			if (form == null)
				return;
			if (!(form is IMoAffixForm))
				return;
			foreach (IMoInflClass ic in ((IMoAffixForm)form).InflectionClassesRC)
			{
				writer.WriteStartElement("inflectionClass");
				writer.WriteAttributeString("id", ic.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString("abbr", ic.Abbreviation.BestAnalysisAlternative.Text);
				writer.WriteEndElement(); //inflectionClass
			}
		}

		private static void CreateInflMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoInflAffMsa inflMsa, FdoCache fdoCache)
		{
			XmlNode inflMsaNode = CreateXmlElement(doc, "inflMsa", morphNode);
			CreatePOSXmlAttribute(doc, inflMsaNode, inflMsa.PartOfSpeechRA, "cat");
			// handle any slot
			HandleSlotInfoForInflectionalMsa(inflMsa, doc, inflMsaNode, morphNode, fdoCache);
			CreateFeatureStructureNodes(doc, inflMsaNode, inflMsa.InflFeatsOA, inflMsa.Hvo);
			CreateProductivityRestrictionNodes(doc, inflMsaNode, inflMsa.FromProdRestrictRC, "fromProductivityRestriction");
		}

		private static void CreateInflMsaXmlElement(XmlWriter writer, IMoInflAffMsa inflMsa, string slotHvo, FdoCache fdoCache)
		{
			writer.WriteStartElement("inflMsa");
			CreatePOSXmlAttribute(writer, inflMsa.PartOfSpeechRA, "cat");
			// handle any slot
			HandleSlotInfoForInflectionalMsa(writer, slotHvo, fdoCache);
			CreateFeatureStructureNodes(writer, inflMsa.InflFeatsOA, inflMsa.Hvo);
			CreateProductivityRestrictionNodes(writer, inflMsa.FromProdRestrictRC, "fromProductivityRestriction");
			writer.WriteEndElement(); //inflMsa
		}

		private static void CreateDerivMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoDerivAffMsa derivMsa)
		{
			XmlNode derivMsaNode = CreateXmlElement(doc, "derivMsa", morphNode);
			CreatePOSXmlAttribute(doc, derivMsaNode, derivMsa.FromPartOfSpeechRA, "fromCat");
			CreatePOSXmlAttribute(doc, derivMsaNode, derivMsa.ToPartOfSpeechRA, "toCat");
			CreateInflectionClassXmlAttribute(doc, derivMsaNode, derivMsa.FromInflectionClassRA, "fromInflClass");
			CreateInflectionClassXmlAttribute(doc, derivMsaNode, derivMsa.ToInflectionClassRA, "toInflClass");
			CreateRequiresInflectionXmlAttribute(doc, derivMsa.ToPartOfSpeechRA, derivMsaNode);
			CreateFeatureStructureNodes(doc, derivMsaNode, derivMsa.FromMsFeaturesOA, derivMsa.Hvo, "fromFS");
			CreateFeatureStructureNodes(doc, derivMsaNode, derivMsa.ToMsFeaturesOA, derivMsa.Hvo, "toFS");
			CreateProductivityRestrictionNodes(doc, derivMsaNode, derivMsa.FromProdRestrictRC, "fromProductivityRestriction");
			CreateProductivityRestrictionNodes(doc, derivMsaNode, derivMsa.ToProdRestrictRC, "toProductivityRestriction");
		}

		private static void CreateDerivMsaXmlElement(XmlWriter writer, IMoDerivAffMsa derivMsa)
		{
			writer.WriteStartElement("derivMsa");
			CreatePOSXmlAttribute(writer, derivMsa.FromPartOfSpeechRA, "fromCat");
			CreatePOSXmlAttribute(writer, derivMsa.ToPartOfSpeechRA, "toCat");
			CreateInflectionClassXmlAttribute(writer, derivMsa.FromInflectionClassRA, "fromInflClass");
			CreateInflectionClassXmlAttribute(writer, derivMsa.ToInflectionClassRA, "toInflClass");
			CreateRequiresInflectionXmlAttribute(writer, derivMsa.ToPartOfSpeechRA);
			CreateFeatureStructureNodes(writer, derivMsa.FromMsFeaturesOA, derivMsa.Hvo, "fromFS");
			CreateFeatureStructureNodes(writer, derivMsa.ToMsFeaturesOA, derivMsa.Hvo, "toFS");
			CreateProductivityRestrictionNodes(writer, derivMsa.FromProdRestrictRC, "fromProductivityRestriction");
			CreateProductivityRestrictionNodes(writer, derivMsa.ToProdRestrictRC, "toProductivityRestriction");
			writer.WriteEndElement(); //derivMsa
		}

		private static void CreateUnclassifedMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoUnclassifiedAffixMsa unclassMsa)
		{
			XmlNode unclassMsaNode = CreateXmlElement(doc, "unclassMsa", morphNode);
			CreatePOSXmlAttribute(doc, unclassMsaNode, unclassMsa.PartOfSpeechRA, "fromCat");
		}

		private static void CreateUnclassifedMsaXmlElement(XmlWriter writer, IMoUnclassifiedAffixMsa unclassMsa)
		{
			writer.WriteStartElement("unclassMsa");
			CreatePOSXmlAttribute(writer, unclassMsa.PartOfSpeechRA, "fromCat");
			writer.WriteEndElement(); //unclassMsa
		}

		private static void CreateInflMsaForLexEntryInflType(XmlDocument doc, XmlNode node, ILexEntryInflType lexEntryInflType, FdoCache fdoCache)
		{
			/*var slots = lexEntryInflType.SlotsRC;
			IMoInflAffixSlot firstSlot = null;
			foreach (var slot in slots)
			{
				if (firstSlot == null)
					firstSlot = slot;
			}*/
			IMoInflAffixSlot slot;
			var slotId = node.SelectSingleNode("MoForm/@wordType");
			if (slotId != null)
				slot = fdoCache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(Convert.ToInt32(slotId.InnerText));
			else
			{
				var slots = lexEntryInflType.SlotsRC;
				IMoInflAffixSlot firstSlot = slots.FirstOrDefault();
				slot = firstSlot;
			}
			if (slot != null)
			{
				XmlNode nullInflMsaNode = CreateXmlElement(doc, "inflMsa", node);
				CreatePOSXmlAttribute(doc, nullInflMsaNode, slot.Owner as IPartOfSpeech, "cat");
				CreateXmlAttribute(doc, "slot", slot.Hvo.ToString(CultureInfo.InvariantCulture), nullInflMsaNode);
				CreateXmlAttribute(doc, "slotAbbr", slot.Name.BestAnalysisAlternative.Text, nullInflMsaNode);
				CreateXmlAttribute(doc, "slotOptional", "false", nullInflMsaNode);
				CreateFeatureStructureNodes(doc, nullInflMsaNode, lexEntryInflType.InflFeatsOA, lexEntryInflType.Hvo);
			}
		}

		private static void CreateInflMsaForLexEntryInflType(XmlWriter writer, string slotHvo, ILexEntryInflType lexEntryInflType, FdoCache fdoCache)
		{
			IMoInflAffixSlot slot;
			//var slotId = node.SelectSingleNode("MoForm/@wordType");
			if (slotHvo != null)
				slot = fdoCache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(Convert.ToInt32(slotHvo));
			else
			{
				var slots = lexEntryInflType.SlotsRC;
				IMoInflAffixSlot firstSlot = slots.FirstOrDefault();
				slot = firstSlot;
			}
			if (slot != null)
			{
				writer.WriteStartElement("inflMsa");
				CreatePOSXmlAttribute(writer, slot.Owner as IPartOfSpeech, "cat");
				writer.WriteAttributeString("slot", slot.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString("slotAbbr", slot.Name.BestAnalysisAlternative.Text);
				writer.WriteAttributeString("slotOptional", "false");
				CreateFeatureStructureNodes(writer, lexEntryInflType.InflFeatsOA, lexEntryInflType.Hvo);
				writer.WriteEndElement(); //inflMsa
			}
		}

		private static void CreateInflectionClassXmlAttribute(XmlDocument doc, XmlNode msaNode, IMoInflClass inflClass, string sInflClass)
		{
			if (inflClass != null)
			{
				CreateXmlAttribute(doc, sInflClass, inflClass.Hvo.ToString(CultureInfo.InvariantCulture), msaNode);
				string sInflClassAbbr = inflClass.Hvo > 0 ? inflClass.Abbreviation.BestAnalysisAlternative.Text : "";
				CreateXmlAttribute(doc, sInflClass + "Abbr", sInflClassAbbr, msaNode);
			}
			else
				CreateXmlAttribute(doc, sInflClass, "0", msaNode);
		}

		private static void CreateInflectionClassXmlAttribute(XmlWriter writer, IMoInflClass inflClass, string sInflClass)
		{
			if (inflClass != null)
			{
				writer.WriteAttributeString(sInflClass, inflClass.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString(sInflClass + "Abbr", inflClass.Hvo > 0 ? inflClass.Abbreviation.BestAnalysisAlternative.Text : "");
			}
			else
				writer.WriteAttributeString(sInflClass, "0");
		}

		private static void CreateRequiresInflectionXmlAttribute(XmlDocument doc, IPartOfSpeech pos, XmlNode msaNode)
		{
			string sPlusMinus = RequiresInflection(pos) ? "+" : "-";
			CreateXmlAttribute(doc, "requiresInfl", sPlusMinus, msaNode);
		}

		private static void CreateRequiresInflectionXmlAttribute(XmlWriter writer, IPartOfSpeech pos)
		{
			writer.WriteAttributeString("requiresInfl", RequiresInflection(pos) ? "+" : "-");
		}

		private static void CreateFeatureStructureNodes(XmlDocument doc, XmlNode msaNode, IFsFeatStruc fs, int id, string sFsName = "fs")
		{
			if (fs == null)
				return;
			XmlNode fsNode = CreateXmlElement(doc, sFsName, msaNode);
			CreateXmlAttribute(doc, "id", id.ToString(CultureInfo.InvariantCulture), fsNode);
			foreach (IFsFeatureSpecification spec in fs.FeatureSpecsOC)
			{
				XmlNode feature = CreateXmlElement(doc, "feature", fsNode);
				XmlNode name = CreateXmlElement(doc, "name", feature);
				name.InnerText = spec.FeatureRA.Abbreviation.BestAnalysisAlternative.Text;
				XmlNode fvalue = CreateXmlElement(doc, "value", feature);
				var cv = spec as IFsClosedValue;
				if (cv != null)
					fvalue.InnerText = cv.ValueRA.Abbreviation.BestAnalysisAlternative.Text;
				else
				{
					var complex = spec as IFsComplexValue;
					if (complex == null)
						continue; // skip this one since we're not dealing with it yet
					var nestedFs = complex.ValueOA as IFsFeatStruc;
					if (nestedFs != null)
						CreateFeatureStructureNodes(doc, fvalue, nestedFs, 0);
				}
			}
		}

		private static void CreateFeatureStructureNodes(XmlWriter writer, IFsFeatStruc fs, int id, string sFsName = "fs")
		{
			if (fs == null)
				return;
			writer.WriteStartElement(sFsName);
			writer.WriteAttributeString("id", id.ToString(CultureInfo.InvariantCulture));
			foreach (IFsFeatureSpecification spec in fs.FeatureSpecsOC)
			{
				writer.WriteStartElement("feature");
				writer.WriteElementString("name", spec.FeatureRA.Abbreviation.BestAnalysisAlternative.Text);
				writer.WriteStartElement("value");
				var cv = spec as IFsClosedValue;
				if (cv != null)
					writer.WriteString(cv.ValueRA.Abbreviation.BestAnalysisAlternative.Text);
				else
				{
					var complex = spec as IFsComplexValue;
					if (complex == null)
						continue; // skip this one since we're not dealing with it yet
					var nestedFs = complex.ValueOA as IFsFeatStruc;
					if (nestedFs != null)
						CreateFeatureStructureNodes(writer, nestedFs, 0);
				}
				writer.WriteEndElement(); //value
				writer.WriteEndElement(); //feature
			}
			writer.WriteEndElement(); //sFsName
		}

		private static void CreateProductivityRestrictionNodes(XmlDocument doc, XmlNode msaNode, IFdoReferenceCollection<ICmPossibility> prodRests, string sElementName)
		{
			if (prodRests == null || prodRests.Count < 1)
				return;
			foreach (ICmPossibility pr in prodRests)
			{
				XmlNode prNode = CreateXmlElement(doc, sElementName, msaNode);
				CreateXmlAttribute(doc, "id", pr.Hvo.ToString(CultureInfo.InvariantCulture), prNode);
				XmlNode prName = CreateXmlElement(doc, "name", prNode);
				prName.InnerText = pr.Name.BestAnalysisAlternative.Text;
			}
		}

		private static void CreateProductivityRestrictionNodes(XmlWriter writer, IFdoReferenceCollection<ICmPossibility> prodRests, string sElementName)
		{
			if (prodRests == null || prodRests.Count < 1)
				return;
			foreach (ICmPossibility pr in prodRests)
			{
				writer.WriteStartElement(sElementName);
				writer.WriteAttributeString("id", pr.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteElementString("name", pr.Name.BestAnalysisAlternative.Text);
				writer.WriteEndElement(); //sElementName
			}
		}

		private static void CreateFromPOSNodes(XmlDocument doc, XmlNode msaNode, IFdoReferenceCollection<IPartOfSpeech> fromPoSes, string sElementName)
		{
			if (fromPoSes == null || fromPoSes.Count < 1)
				return;
			foreach (IPartOfSpeech pos in fromPoSes)
			{
				XmlNode posNode = CreateXmlElement(doc, sElementName, msaNode);
				CreateXmlAttribute(doc, "fromCat", pos.Hvo.ToString(CultureInfo.InvariantCulture), posNode);
				CreateXmlAttribute(doc, "fromCatAbbr", pos.Abbreviation.BestAnalysisAlternative.Text, posNode);
			}
		}

		private static void CreateFromPOSNodes(XmlWriter writer, IFdoReferenceCollection<IPartOfSpeech> fromPoSes, string sElementName)
		{
			if (fromPoSes == null || fromPoSes.Count < 1)
				return;
			foreach (IPartOfSpeech pos in fromPoSes)
			{
				writer.WriteStartElement(sElementName);
				writer.WriteAttributeString("fromCat", pos.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString("fromCatAbbr", pos.Abbreviation.BestAnalysisAlternative.Text);
				writer.WriteEndElement(); //sElementName
			}
		}

		private static void HandleSlotInfoForInflectionalMsa(IMoInflAffMsa inflMsa, XmlDocument doc, XmlNode inflMsaNode, XmlNode morphNode, FdoCache fdoCache)
		{
			int slotHvo = 0;
			int iCount = inflMsa.SlotsRC.Count;
			if (iCount > 0)
			{
				if (iCount > 1)
				{ // have a circumfix; assume only two slots and assume that the first is prefix and second is suffix
					// TODO: ideally would figure out if the slots are prefix or suffix slots and then align the
					// o and 1 indices to the appropriate slot.  Will just do this for now (hab 2005.08.04).
					XmlNode attrType = morphNode.SelectSingleNode("@type");
					if (attrType != null && attrType.InnerText != "sfx")
						slotHvo = inflMsa.SlotsRC.ToHvoArray()[0];
					else
						slotHvo = inflMsa.SlotsRC.ToHvoArray()[1];
				}
				else
					slotHvo = inflMsa.SlotsRC.ToHvoArray()[0];
			}
			CreateXmlAttribute(doc, "slot", slotHvo.ToString(CultureInfo.InvariantCulture), inflMsaNode);
			string sSlotOptional = "false";
			string sSlotAbbr = "??";
			if (slotHvo > 0)
			{
				var slot = fdoCache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(slotHvo);
				if (slot != null)
				{
					sSlotAbbr = slot.Name.BestAnalysisAlternative.Text;
					if (slot.Optional)
						sSlotOptional = "true";
				}
			}
			CreateXmlAttribute(doc, "slotAbbr", sSlotAbbr, inflMsaNode);
			CreateXmlAttribute(doc, "slotOptional", sSlotOptional, inflMsaNode);
		}

		private static void HandleSlotInfoForInflectionalMsa(XmlWriter writer, string slotHvo, FdoCache fdoCache)
		{
			writer.WriteAttributeString("slot", slotHvo.ToString(CultureInfo.InvariantCulture));
			string sSlotOptional = "false";
			string sSlotAbbr = "??";
			if (Convert.ToInt32(slotHvo) > 0)
			{
				var slot = fdoCache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(Convert.ToInt32(slotHvo));
				if (slot != null)
				{
					sSlotAbbr = slot.Name.BestAnalysisAlternative.Text;
					if (slot.Optional)
						sSlotOptional = "true";
				}
			}
			writer.WriteAttributeString("slotAbbr", sSlotAbbr);
			writer.WriteAttributeString("slotOptional", sSlotOptional);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public static void ConvertMorphs(XmlDocument doc, string sNodeListToFind, bool fIdsInAttribute, FdoCache fdoCache)
		{
			XmlNodeList nl = doc.SelectNodes(sNodeListToFind);
			if (nl != null)
			{
				foreach (XmlNode node in nl)
				{
					Debug.Assert(node.Attributes != null);
					XmlNode alloid = fIdsInAttribute ? node.Attributes.GetNamedItem("alloid") : node.SelectSingleNode("MoForm/@DbRef");
					Debug.Assert(alloid != null);
					int hvo = Convert.ToInt32(alloid.InnerText);
					var obj = fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					var form = obj as IMoForm;
					if (form == null)
					{
						// This is one of the null allomorphs we create when building the
						// input for the parser in order to still get the Word Grammar to have something in any
						// required slots in affix templates.
						var lexEntryInflType = obj as ILexEntryInflType;
						if (lexEntryInflType != null)
						{
							ConvertLexEntryInflType(doc, node, lexEntryInflType, fdoCache);
							continue;
						}
					}
					string sLongName;
					string sForm;
					string sGloss;
					string sCitationForm;
					if (form != null)
					{
						sLongName = form.LongName;
						int iFirstSpace = sLongName.IndexOf(" (", StringComparison.Ordinal);
						int iLastSpace = sLongName.LastIndexOf("):", StringComparison.Ordinal) + 2;
						sForm = sLongName.Substring(0, iFirstSpace);
						XmlNode msaid = fIdsInAttribute ? node.Attributes.GetNamedItem("morphname") : node.SelectSingleNode("MSI/@DbRef");
						Debug.Assert(msaid != null);
						string sMsaHvo = msaid.InnerText;
						var indexOfPeriod = IndexOfPeriodInMsaHvo(ref sMsaHvo);
						int hvoMsa = Convert.ToInt32(sMsaHvo);
						var msaObj = fdoCache.ServiceLocator.GetObject(hvoMsa);
						if (msaObj.ClassID == LexEntryTags.kClassId)
						{
							var entry = msaObj as ILexEntry;
							Debug.Assert(entry != null);
							if (entry.EntryRefsOS.Count > 0)
							{
								var index = IndexOfLexEntryRef(msaid.Value, indexOfPeriod); // the value of the int after the period
								var lexEntryRef = entry.EntryRefsOS[index];
								ITsIncStrBldr sbGlossPrepend;
								ITsIncStrBldr sbGlossAppend;
								var sense = FDO.DomainServices.MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
								var glossWs = fdoCache.ServiceLocator.WritingSystemManager.Get(fdoCache.DefaultAnalWs);
								FDO.DomainServices.MorphServices.JoinGlossAffixesOfInflVariantTypes(lexEntryRef.VariantEntryTypesRS,
																									glossWs,
																									out sbGlossPrepend, out sbGlossAppend);
								ITsIncStrBldr sbGloss = sbGlossPrepend;
								sbGloss.Append(sense.Gloss.BestAnalysisAlternative.Text);
								sbGloss.Append(sbGlossAppend.Text);
								sGloss = sbGloss.Text;
							}
							else
							{
								sGloss = ParserCoreStrings.ksUnknownGloss;
							}

						}
						else
						{
							var msa = msaObj as IMoMorphSynAnalysis;
							sGloss = msa != null ? msa.GetGlossOfFirstSense() : sLongName.Substring(iFirstSpace, iLastSpace - iFirstSpace).Trim();
						}
						sCitationForm = sLongName.Substring(iLastSpace).Trim();
						sLongName = String.Format(ParserCoreStrings.ksX_Y_Z, sForm, sGloss, sCitationForm);
					}
					else
					{
						sForm = ParserCoreStrings.ksUnknownMorpheme; // in case the user continues...
						sGloss = ParserCoreStrings.ksUnknownGloss;
						sCitationForm = ParserCoreStrings.ksUnknownCitationForm;
						sLongName = String.Format(ParserCoreStrings.ksX_Y_Z, sForm, sGloss, sCitationForm);
						throw new ApplicationException(sLongName);
					}
					XmlNode tempNode = CreateXmlElement(doc, "shortName", node);
					tempNode.InnerXml = CreateEntities(sLongName);
					tempNode = CreateXmlElement(doc, "alloform", node);
					tempNode.InnerXml = CreateEntities(sForm);
					switch (form.ClassID)
					{
						case MoStemAllomorphTags.kClassId:
							ConvertStemName(doc, node, form);
							break;
						case MoAffixAllomorphTags.kClassId:
							ConvertAffixAlloFeats(doc, node, form, fdoCache);
							ConvertStemNameAffix(doc, node, fdoCache);
							break;

					}
					tempNode = CreateXmlElement(doc, "gloss", node);
					tempNode.InnerXml = CreateEntities(sGloss);
					tempNode = CreateXmlElement(doc, "citationForm", node);
					tempNode.InnerXml = CreateEntities(sCitationForm);
				}
			}
		}

		private static void ConvertLexEntryInflType(XmlDocument doc, XmlNode node, ILexEntryInflType lexEntryInflType, FdoCache fdoCache)
		{
			var lexEntryInflTypeNode = CreateXmlElement(doc, "lexEntryInflType", node);
			var nullTempNode = CreateXmlElement(doc, "alloform", node);
			nullTempNode.InnerText = "0";
			string sNullGloss = null;
			var sbGloss = new StringBuilder();
			if (string.IsNullOrEmpty(lexEntryInflType.GlossPrepend.BestAnalysisAlternative.Text))
			{
				sbGloss.Append(lexEntryInflType.GlossPrepend.BestAnalysisAlternative.Text);
				sbGloss.Append("...");
			}
			sbGloss.Append(lexEntryInflType.GlossAppend.BestAnalysisAlternative.Text);
			nullTempNode = CreateXmlElement(doc, "gloss", node);
			nullTempNode.InnerXml = CreateEntities(sbGloss.ToString());
			var sMsg = string.Format(ParserCoreStrings.ksIrregularlyInflectedFormNullAffix, lexEntryInflType.ShortName);
			nullTempNode = CreateXmlElement(doc, "citationForm", node);
			nullTempNode.InnerXml = CreateEntities(sMsg);
			CreateInflMsaForLexEntryInflType(doc, node, lexEntryInflType, fdoCache);
		}

		private static void ConvertStemName(XmlDocument doc, XmlNode node, IMoForm form)
		{
			var sallo = form as IMoStemAllomorph;
			Debug.Assert(sallo != null);
			IMoStemName sn = sallo.StemNameRA;
			if (sn != null)
			{
				XmlNode tempNode = CreateXmlElement(doc, "stemName", node);
				CreateXmlAttribute(doc, "id", sn.Hvo.ToString(CultureInfo.InvariantCulture), tempNode);
				tempNode.InnerXml = CreateEntities(sn.Name.BestAnalysisAlternative.Text);
			}
			else
			{   // There's no overt stem name on this allomorph, but there might be overt stem names
				// on other allomorphs in this lexical entry.  This allomorph, then, cannot bear any
				// of the features of these other stem names.  If so, there will be a property named
				// NotStemNameddd or NotStemNamedddNotStemNamedddd, etc.
				CreateNotStemNameElement(doc, node);
			}
		}

//		private static void ConvertStemName(XmlWriter writer, IMoForm form)
//		{
//			var sallo = form as IMoStemAllomorph;
//			Debug.Assert(sallo != null);
//			IMoStemName sn = sallo.StemNameRA;
//			if (sn != null)
//			{
//				writer.WriteStartElement("stemName");
//				writer.WriteAttributeString("id", sn.Hvo.ToString(CultureInfo.InvariantCulture));
//				writer.WriteString(sn.Name.BestAnalysisAlternative.Text);
//				writer.WriteEndElement(); //stemName
//			}
//			else
//			{   // There's no overt stem name on this allomorph, but there might be overt stem names
//				// on other allomorphs in this lexical entry.  This allomorph, then, cannot bear any
//				// of the features of these other stem names.  If so, there will be a property named
//				// NotStemNameddd or NotStemNamedddNotStemNamedddd, etc.
//				CreateNotStemNameElement(writer);
//			}
//		}

		private static void ConvertStemNameAffix(XmlDocument doc, XmlNode node, FdoCache fdoCache)
		{
			XmlNode props = node.SelectSingleNode("props");
			if (props != null)
			{
				string sProps = props.InnerText.Trim();
				string[] saProps = sProps.Split(' ');
				foreach (string sProp in saProps)
				{
					int i = sProp.IndexOf("StemNameAffix", StringComparison.Ordinal);
					if (i > -1)
					{
						string sId = (sProp.Substring(i + 13)).Trim();
						var sn = fdoCache.ServiceLocator.GetInstance<IMoStemNameRepository>().GetObject(Convert.ToInt32(sId));
						if (sn != null)
						{
							XmlNode tempNode = CreateXmlElement(doc, "stemNameAffix", node);
							CreateXmlAttribute(doc, "id", sId, tempNode);
							tempNode.InnerXml = CreateEntities(sn.Name.BestAnalysisAlternative.Text);
						}
					}
				}
			}
		}

		private static void CreateNotStemNameElement(XmlDocument doc, XmlNode node)
		{
			XmlNode props = node.SelectSingleNode("props");
			if (props != null)
			{
				int i = props.InnerText.IndexOf("NotStemName", StringComparison.Ordinal);
				if (i > -1)
				{
					string s = props.InnerText.Substring(i);
					int iSpace = s.IndexOf(" ", StringComparison.Ordinal);
					string sNotStemName = iSpace > -1 ? s.Substring(0, iSpace - 1) : s;
					XmlNode tempNode = CreateXmlElement(doc, "stemName", node);
					CreateXmlAttribute(doc, "id", sNotStemName, tempNode);
				}
			}
		}

		private static void CreateNotStemNameElement(XmlWriter writer, string propsText)
		{
			if (propsText != null)
			{
				int i = propsText.IndexOf("NotStemName", StringComparison.Ordinal);
				if (i > -1)
				{
					string s = propsText.Substring(i);
					int iSpace = s.IndexOf(" ", StringComparison.Ordinal);
					string sNotStemName = iSpace > -1 ? s.Substring(0, iSpace - 1) : s;
					writer.WriteStartElement("stemName");
					writer.WriteAttributeString("id", sNotStemName);
					writer.WriteEndElement(); //stemName
				}
			}
		}

		private static void ConvertAffixAlloFeats(XmlDocument doc, XmlNode node, IMoForm form, FdoCache fdoCache)
		{
			var sallo = form as IMoAffixAllomorph;
			if (sallo == null)
				return;  // the form could be an IMoAffixProcess in which case there are no MsEnvFeatures.
			IFsFeatStruc fsFeatStruc = sallo.MsEnvFeaturesOA;
			if (fsFeatStruc != null && !fsFeatStruc.IsEmpty)
			{
				XmlNode tempNode = CreateXmlElement(doc, "affixAlloFeats", node);
				CreateFeatureStructureNodes(doc, tempNode, fsFeatStruc, fsFeatStruc.Hvo);
			}
			else
			{   // There's no overt stem name on this allomorph, but there might be overt stem names
				// on other allomorphs in this lexical entry.  This allomorph, then, cannot bear any
				// of the features of these other stem names.  If so, there will be a property named
				// NotStemNameddd or NotStemNamedddNotStemNamedddd, etc.
				CreateNotAffixAlloFeatsElement(doc, node, fdoCache);
			}
		}

		private static void ConvertAffixAlloFeats(XmlWriter writer, IMoForm form, string propsText, FdoCache fdoCache)
		{
			var sallo = form as IMoAffixAllomorph;
			if (sallo == null)
				return;  // the form could be an IMoAffixProcess in which case there are no MsEnvFeatures.
			IFsFeatStruc fsFeatStruc = sallo.MsEnvFeaturesOA;
			if (fsFeatStruc != null && !fsFeatStruc.IsEmpty)
			{
				writer.WriteStartElement("affixAlloFeats");
				CreateFeatureStructureNodes(writer, fsFeatStruc, fsFeatStruc.Hvo);
				writer.WriteEndElement(); //affixAlloFeats
			}
			else
			{   // There's no overt stem name on this allomorph, but there might be overt stem names
				// on other allomorphs in this lexical entry.  This allomorph, then, cannot bear any
				// of the features of these other stem names.  If so, there will be a property named
				// NotStemNameddd or NotStemNamedddNotStemNamedddd, etc.
				CreateNotAffixAlloFeatsElement(writer, propsText, fdoCache);
			}
		}

		private static void CreateNotAffixAlloFeatsElement(XmlDocument doc, XmlNode node, FdoCache fdoCache)
		{
			XmlNode props = node.SelectSingleNode("props");
			if (props != null)
			{
				int i = props.InnerText.IndexOf("MSEnvFSNot", StringComparison.Ordinal);
				if (i > -1)
				{
					XmlNode affixAlloFeatsNode = CreateXmlElement(doc, "affixAlloFeats", node);
					XmlNode notNode = CreateXmlElement(doc, "not", affixAlloFeatsNode);
					string s = props.InnerText.Substring(i);
					int j = s.IndexOf(' ');
					if (j > 0)
						s = props.InnerText.Substring(i, j + 1);
					int iNot = s.IndexOf("Not", StringComparison.Ordinal) + 3;
					while (iNot > 3)
					{
						int iNextNot = s.IndexOf("Not", iNot, StringComparison.Ordinal);
						string sFsHvo;
						if (iNextNot > -1)
						{
							// there are more
							sFsHvo = s.Substring(iNot, iNextNot - iNot);
							CreateFeatureStructureFromHvoString(doc, sFsHvo, notNode, fdoCache);
							iNot = iNextNot + 3;
						}
						else
						{
							// is the last one
							sFsHvo = s.Substring(iNot);
							CreateFeatureStructureFromHvoString(doc, sFsHvo, notNode, fdoCache);
							iNot = 0;
						}
					}
				}
			}
		}

		private static void CreateNotAffixAlloFeatsElement(XmlWriter writer, string propsText, FdoCache fdoCache)
		{
			if (propsText != null)
			{
				int i = propsText.IndexOf("MSEnvFSNot", StringComparison.Ordinal);
				if (i > -1)
				{
					writer.WriteStartElement("affixAlloFeats");
					writer.WriteStartElement("not");
					string s = propsText.Substring(i);
					int j = s.IndexOf(' ');
					if (j > 0)
						s = propsText.Substring(i, j + 1);
					int iNot = s.IndexOf("Not", StringComparison.Ordinal) + 3;
					while (iNot > 3)
					{
						int iNextNot = s.IndexOf("Not", iNot, StringComparison.Ordinal);
						string sFsHvo;
						if (iNextNot > -1)
						{
							// there are more
							sFsHvo = s.Substring(iNot, iNextNot - iNot);
							CreateFeatureStructureFromHvoString(writer, sFsHvo, fdoCache);
							iNot = iNextNot + 3;
						}
						else
						{
							// is the last one
							sFsHvo = s.Substring(iNot);
							CreateFeatureStructureFromHvoString(writer, sFsHvo, fdoCache);
							iNot = 0;
						}
					}
					writer.WriteEndElement(); //not
					writer.WriteEndElement(); //affixAlloFeats
				}
			}
		}

		private static void CreateFeatureStructureFromHvoString(XmlDocument doc, string sFsHvo, XmlNode parentNode, FdoCache fdoCache)
		{
			int fsHvo = Convert.ToInt32(sFsHvo);
			var fsFeatStruc = (IFsFeatStruc)fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(fsHvo);
			if (fsFeatStruc != null)
			{
				CreateFeatureStructureNodes(doc, parentNode, fsFeatStruc, fsHvo);
			}
		}

		private static void CreateFeatureStructureFromHvoString(XmlWriter writer, string sFsHvo, FdoCache fdoCache)
		{
			int fsHvo = Convert.ToInt32(sFsHvo);
			var fsFeatStruc = (IFsFeatStruc)fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(fsHvo);
			if (fsFeatStruc != null)
				CreateFeatureStructureNodes(writer, fsFeatStruc, fsHvo);
		}

		public static string CreateEntities(string sInput)
		{
			if (sInput == null)
				return "";
			string sResult1 = sInput.Replace("&", "&amp;"); // N.B. Must be ordered first!
			string sResult2 = sResult1.Replace("<", "&lt;");
			return sResult2;
		}

		private static void CreatePOSXmlAttribute(XmlDocument doc, XmlNode msaNode, IPartOfSpeech pos, string sCat)
		{
			if (pos != null)
			{
				CreateXmlAttribute(doc, sCat, pos.Hvo.ToString(CultureInfo.InvariantCulture), msaNode);
				string sPosAbbr = pos.Hvo > 0 ? pos.Abbreviation.BestAnalysisAlternative.Text : "??";
				CreateXmlAttribute(doc, sCat + "Abbr", sPosAbbr, msaNode);
			}
			else
				CreateXmlAttribute(doc, sCat, "0", msaNode);
		}

		private static void CreatePOSXmlAttribute(XmlWriter writer, IPartOfSpeech pos, string sCat)
		{
			if (pos != null)
			{
				writer.WriteAttributeString(sCat, pos.Hvo.ToString(CultureInfo.InvariantCulture));
				string sPosAbbr = pos.Hvo > 0 ? pos.Abbreviation.BestAnalysisAlternative.Text : "??";
				writer.WriteAttributeString(sCat + "Abbr", sPosAbbr);
			}
			else
				writer.WriteAttributeString(sCat, "0");
		}

		/// <summary>
		/// Create an xml element and add it to the tree
		/// </summary>
		/// <param name="doc">Xml document containing the element</param>
		/// <param name="sElementName">name of the element to create</param>
		/// <param name="parentNode">owner of the newly created element</param>
		/// <returns>newly created element node</returns>
		public static XmlNode CreateXmlElement(XmlDocument doc, string sElementName, XmlNode parentNode)
		{
			XmlNode node = doc.CreateNode(XmlNodeType.Element, sElementName, null);
			parentNode.AppendChild(node);
			return node;
		}

		public static void CreateXmlAttribute(XmlDocument doc, string sAttrName, string sAttrValue, XmlNode elementNode)
		{
			XmlNode attr = doc.CreateNode(XmlNodeType.Attribute, sAttrName, null);
			attr.Value = sAttrValue;
			if (elementNode.Attributes != null)
				elementNode.Attributes.SetNamedItem(attr);
		}
	}
}
