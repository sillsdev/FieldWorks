using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.WordWorks.Parser
{
	internal static class ParserXmlWriterExtensions
	{
		public static void WriteMsaElement(this XmlWriter writer, FdoCache cache, string formID, string msaID, string type, string wordType)
		{
			// Irregulary inflected forms can have a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
			Tuple<int, int> msaTuple = ParserHelper.ProcessMsaHvo(msaID);
			ICmObject obj = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(msaTuple.Item1);
			switch (obj.GetType().Name)
			{
				default:
					throw new ApplicationException(String.Format("Invalid MSA type: {0}.", obj.GetType().Name));
				case "MoStemMsa":
					WriteStemMsaXmlElement(writer, (IMoStemMsa) obj, Enumerable.Empty<ILexEntryRef>());
					break;
				case "MoInflAffMsa":
					WriteInflectionClasses(writer, cache, int.Parse(formID, CultureInfo.InvariantCulture));
					WriteInflMsaXmlElement(writer, (IMoInflAffMsa) obj, type);
					break;
				case "MoDerivAffMsa":
					WriteDerivMsaXmlElement(writer, (IMoDerivAffMsa) obj);
					break;
				case "MoUnclassifiedAffixMsa":
					WriteUnclassifedMsaXmlElement(writer, (IMoUnclassifiedAffixMsa) obj);
					break;
				case "LexEntry":
					// is an irregularly inflected form
					// get the MoStemMsa of its variant
					var entry = (ILexEntry) obj;
					if (entry.EntryRefsOS.Count > 0)
					{
						ILexEntryRef lexEntryRef = entry.EntryRefsOS[msaTuple.Item2];
						ILexSense sense = MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
						WriteStemMsaXmlElement(writer, (IMoStemMsa) sense.MorphoSyntaxAnalysisRA, entry.VariantEntryRefs);
					}
					break;
				case "LexEntryInflType":
					// This is one of the null allomorphs we create when building the
					// input for the parser in order to still get the Word Grammar to have something in any
					// required slots in affix templates.
					WriteInflMsaForLexEntryInflType(writer, wordType, (ILexEntryInflType) obj);
					break;
			}
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

		private static void WriteStemMsaXmlElement(XmlWriter writer, IMoStemMsa stemMsa, IEnumerable<ILexEntryRef> variantEntryRefs)
		{
			writer.WriteStartElement("stemMsa");
			WritePosXmlAttribute(writer, stemMsa.PartOfSpeechRA, "cat");
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
						int clsid = stemMsa.Services.GetInstance<ICmObjectRepository>().GetObject(pos.Owner.Hvo).ClassID;
						pos = clsid == PartOfSpeechTags.kClassId ? stemMsa.Services.GetInstance<IPartOfSpeechRepository>().GetObject(pos.Owner.Hvo) : null;
					}
				}
				if (inflClassHvo != 0)
					inflClass = stemMsa.Services.GetInstance<IMoInflClassRepository>().GetObject(inflClassHvo);
			}
			WriteInflectionClassXmlAttribute(writer, inflClass, "inflClass");
			WriteRequiresInflectionXmlAttribute(writer, stemMsa.PartOfSpeechRA);
			WriteFeatureStructureNodes(writer, stemMsa.MsFeaturesOA, stemMsa.Hvo);
			WriteProductivityRestrictionNodes(writer, stemMsa.ProdRestrictRC, "productivityRestriction");
			WriteFromPosNodes(writer, stemMsa.FromPartsOfSpeechRC, "fromPartsOfSpeech");
			foreach (ILexEntryRef entryRef in variantEntryRefs)
			{
				foreach (ILexEntryType lexEntryType in entryRef.VariantEntryTypesRS)
				{
					var inflEntryType = lexEntryType as ILexEntryInflType;
					if (inflEntryType != null)
						WriteFeatureStructureNodes(writer, inflEntryType.InflFeatsOA, stemMsa.Hvo);
				}
			}
			writer.WriteEndElement(); //stemMsa
		}

		private static void WriteInflectionClasses(XmlWriter writer, FdoCache fdoCache, int allomorphHvo)
		{
			if (allomorphHvo <= 0)
				return;
			// use IMoForm instead of IMoAffixForm or IMoAffixAllomorph because it could be an IMoStemAllomorph
			IMoForm form = fdoCache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(allomorphHvo);
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

		private static void WriteInflMsaXmlElement(XmlWriter writer, IMoInflAffMsa inflMsa, string type)
		{
			writer.WriteStartElement("inflMsa");
			WritePosXmlAttribute(writer, inflMsa.PartOfSpeechRA, "cat");
			// handle any slot
			HandleSlotInfoForInflectionalMsa(writer, inflMsa, type);
			WriteFeatureStructureNodes(writer, inflMsa.InflFeatsOA, inflMsa.Hvo);
			WriteProductivityRestrictionNodes(writer, inflMsa.FromProdRestrictRC, "fromProductivityRestriction");
			writer.WriteEndElement(); //inflMsa
		}

		private static void WriteDerivMsaXmlElement(XmlWriter writer, IMoDerivAffMsa derivMsa)
		{
			writer.WriteStartElement("derivMsa");
			WritePosXmlAttribute(writer, derivMsa.FromPartOfSpeechRA, "fromCat");
			WritePosXmlAttribute(writer, derivMsa.ToPartOfSpeechRA, "toCat");
			WriteInflectionClassXmlAttribute(writer, derivMsa.FromInflectionClassRA, "fromInflClass");
			WriteInflectionClassXmlAttribute(writer, derivMsa.ToInflectionClassRA, "toInflClass");
			WriteRequiresInflectionXmlAttribute(writer, derivMsa.ToPartOfSpeechRA);
			WriteFeatureStructureNodes(writer, derivMsa.FromMsFeaturesOA, derivMsa.Hvo, "fromFS");
			WriteFeatureStructureNodes(writer, derivMsa.ToMsFeaturesOA, derivMsa.Hvo, "toFS");
			WriteProductivityRestrictionNodes(writer, derivMsa.FromProdRestrictRC, "fromProductivityRestriction");
			WriteProductivityRestrictionNodes(writer, derivMsa.ToProdRestrictRC, "toProductivityRestriction");
			writer.WriteEndElement(); //derivMsa
		}

		private static void WriteUnclassifedMsaXmlElement(XmlWriter writer, IMoUnclassifiedAffixMsa unclassMsa)
		{
			writer.WriteStartElement("unclassMsa");
			WritePosXmlAttribute(writer, unclassMsa.PartOfSpeechRA, "fromCat");
			writer.WriteEndElement(); //unclassMsa
		}

		private static void WriteInflMsaForLexEntryInflType(XmlWriter writer, string wordType, ILexEntryInflType lexEntryInflType)
		{
			IMoInflAffixSlot slot;
			if (wordType != null)
			{
				slot = lexEntryInflType.Services.GetInstance<IMoInflAffixSlotRepository>().GetObject(Convert.ToInt32(wordType));
			}
			else
			{
				var slots = lexEntryInflType.SlotsRC;
				IMoInflAffixSlot firstSlot = slots.FirstOrDefault();
				slot = firstSlot;
			}

			if (slot != null)
			{
				writer.WriteStartElement("inflMsa");
				WritePosXmlAttribute(writer, slot.Owner as IPartOfSpeech, "cat");
				writer.WriteAttributeString("slot", slot.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString("slotAbbr", slot.Name.BestAnalysisAlternative.Text);
				writer.WriteAttributeString("slotOptional", "false");
				WriteFeatureStructureNodes(writer, lexEntryInflType.InflFeatsOA, lexEntryInflType.Hvo);
				writer.WriteEndElement(); //inflMsa
			}
		}

		private static void WriteInflectionClassXmlAttribute(XmlWriter writer, IMoInflClass inflClass, string sInflClass)
		{
			if (inflClass != null)
			{
				writer.WriteAttributeString(sInflClass, inflClass.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString(sInflClass + "Abbr", inflClass.Hvo > 0 ? inflClass.Abbreviation.BestAnalysisAlternative.Text : "");
			}
			else
				writer.WriteAttributeString(sInflClass, "0");
		}

		private static void WriteRequiresInflectionXmlAttribute(XmlWriter writer, IPartOfSpeech pos)
		{
			writer.WriteAttributeString("requiresInfl", RequiresInflection(pos) ? "+" : "-");
		}

		private static void WriteFeatureStructureNodes(XmlWriter writer, IFsFeatStruc fs, int id, string sFsName = "fs")
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
						WriteFeatureStructureNodes(writer, nestedFs, 0);
				}
				writer.WriteEndElement(); //value
				writer.WriteEndElement(); //feature
			}
			writer.WriteEndElement(); //sFsName
		}

		private static void WriteProductivityRestrictionNodes(XmlWriter writer, IFdoReferenceCollection<ICmPossibility> prodRests, string sElementName)
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

		private static void WriteFromPosNodes(XmlWriter writer, IFdoReferenceCollection<IPartOfSpeech> fromPoSes, string sElementName)
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

		private static void HandleSlotInfoForInflectionalMsa(XmlWriter writer, IMoInflAffMsa inflMsa, string type)
		{
			int slotHvo = 0;
			int iCount = inflMsa.SlotsRC.Count;
			if (iCount > 0)
			{
				if (iCount > 1)
				{ // have a circumfix; assume only two slots and assume that the first is prefix and second is suffix
					// TODO: ideally would figure out if the slots are prefix or suffix slots and then align the
					// o and 1 indices to the appropriate slot.  Will just do this for now (hab 2005.08.04).
					if (type != null && type != "sfx")
						slotHvo = inflMsa.SlotsRC.ToHvoArray()[0];
					else
						slotHvo = inflMsa.SlotsRC.ToHvoArray()[1];
				}
				else
					slotHvo = inflMsa.SlotsRC.ToHvoArray()[0];
			}
			writer.WriteAttributeString("slot", slotHvo.ToString(CultureInfo.InvariantCulture));
			string sSlotOptional = "false";
			string sSlotAbbr = "??";
			if (Convert.ToInt32(slotHvo) > 0)
			{
				var slot = inflMsa.Services.GetInstance<IMoInflAffixSlotRepository>().GetObject(Convert.ToInt32(slotHvo));
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

		public static void WriteMorphInfoElements(this XmlWriter writer, FdoCache cache, string formID, string msaID, string wordType, string props)
		{
			ICmObject obj = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(int.Parse(formID, CultureInfo.InvariantCulture));
			var form = obj as IMoForm;
			if (form == null)
			{
				// This is one of the null allomorphs we create when building the
				// input for the parser in order to still get the Word Grammar to have something in any
				// required slots in affix templates.
				var lexEntryInflType = obj as ILexEntryInflType;
				if (lexEntryInflType != null)
				{
					WriteLexEntryInflTypeElement(writer, wordType, lexEntryInflType);
					return;
				}
			}
			string shortName;
			string alloform;
			string gloss;
			string citationForm;
			if (form != null)
			{
				shortName = form.LongName;
				int iFirstSpace = shortName.IndexOf(" (", StringComparison.Ordinal);
				int iLastSpace = shortName.LastIndexOf("):", StringComparison.Ordinal) + 2;
				alloform = shortName.Substring(0, iFirstSpace);
				Tuple<int, int> msaTuple = ParserHelper.ProcessMsaHvo(msaID);
				ICmObject msaObj = cache.ServiceLocator.GetObject(msaTuple.Item1);
				if (msaObj.ClassID == LexEntryTags.kClassId)
				{
					var entry = msaObj as ILexEntry;
					Debug.Assert(entry != null);
					if (entry.EntryRefsOS.Count > 0)
					{
						ILexEntryRef lexEntryRef = entry.EntryRefsOS[msaTuple.Item2];
						ITsIncStrBldr sbGlossPrepend;
						ITsIncStrBldr sbGlossAppend;
						ILexSense sense = MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
						IWritingSystem glossWs = cache.ServiceLocator.WritingSystemManager.Get(cache.DefaultAnalWs);
						MorphServices.JoinGlossAffixesOfInflVariantTypes(lexEntryRef.VariantEntryTypesRS,
							glossWs, out sbGlossPrepend, out sbGlossAppend);
						ITsIncStrBldr sbGloss = sbGlossPrepend;
						sbGloss.Append(sense.Gloss.BestAnalysisAlternative.Text);
						sbGloss.Append(sbGlossAppend.Text);
						gloss = sbGloss.Text;
					}
					else
					{
						gloss = ParserCoreStrings.ksUnknownGloss;
					}

				}
				else
				{
					var msa = msaObj as IMoMorphSynAnalysis;
					gloss = msa != null ? msa.GetGlossOfFirstSense() : shortName.Substring(iFirstSpace, iLastSpace - iFirstSpace).Trim();
				}
				citationForm = shortName.Substring(iLastSpace).Trim();
				shortName = String.Format(ParserCoreStrings.ksX_Y_Z, alloform, gloss, citationForm);
			}
			else
			{
				alloform = ParserCoreStrings.ksUnknownMorpheme; // in case the user continues...
				gloss = ParserCoreStrings.ksUnknownGloss;
				citationForm = ParserCoreStrings.ksUnknownCitationForm;
				shortName = String.Format(ParserCoreStrings.ksX_Y_Z, alloform, gloss, citationForm);
				throw new ApplicationException(shortName);
			}
			writer.WriteElementString("shortName", shortName);
			writer.WriteElementString("alloform", alloform);
			switch (form.ClassID)
			{
				case MoStemAllomorphTags.kClassId:
					WriteStemNameElement(writer, form, props);
					break;
				case MoAffixAllomorphTags.kClassId:
					WriteAffixAlloFeatsElement(writer, form, props);
					WriteStemNameAffixElement(writer, cache, props);
					break;

			}
			writer.WriteElementString("gloss", gloss);
			writer.WriteElementString("citationForm", citationForm);
		}

		private static void WriteLexEntryInflTypeElement(XmlWriter writer, string wordType, ILexEntryInflType lexEntryInflType)
		{
			writer.WriteStartElement("lexEntryInflType");
			writer.WriteEndElement();
			writer.WriteElementString("alloform", "0");
			string sNullGloss = null;
			var sbGloss = new StringBuilder();
			if (string.IsNullOrEmpty(lexEntryInflType.GlossPrepend.BestAnalysisAlternative.Text))
			{
				sbGloss.Append(lexEntryInflType.GlossPrepend.BestAnalysisAlternative.Text);
				sbGloss.Append("...");
			}
			sbGloss.Append(lexEntryInflType.GlossAppend.BestAnalysisAlternative.Text);
			writer.WriteElementString("gloss", sbGloss.ToString());
			var sMsg = string.Format(ParserCoreStrings.ksIrregularlyInflectedFormNullAffix, lexEntryInflType.ShortName);
			writer.WriteElementString("citationForm", sMsg);
			WriteInflMsaForLexEntryInflType(writer, wordType, lexEntryInflType);
		}

		private static void WriteStemNameElement(XmlWriter writer, IMoForm form, string props)
		{
			var sallo = form as IMoStemAllomorph;
			Debug.Assert(sallo != null);
			IMoStemName sn = sallo.StemNameRA;
			if (sn != null)
			{
				writer.WriteStartElement("stemName");
				writer.WriteAttributeString("id", sn.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteString(sn.Name.BestAnalysisAlternative.Text);
				writer.WriteEndElement(); //stemName
			}
			else
			{   // There's no overt stem name on this allomorph, but there might be overt stem names
				// on other allomorphs in this lexical entry.  This allomorph, then, cannot bear any
				// of the features of these other stem names.  If so, there will be a property named
				// NotStemNameddd or NotStemNamedddNotStemNamedddd, etc.
				WriteNotStemNameElement(writer, props);
			}
		}

		private static void WriteStemNameAffixElement(XmlWriter writer, FdoCache cache, string props)
		{
			if (string.IsNullOrEmpty(props))
				return;

			string[] propsArray = props.Trim().Split(' ');
			foreach (string prop in propsArray)
			{
				int i = prop.IndexOf("StemNameAffix", StringComparison.Ordinal);
				if (i > -1)
				{
					string id = (prop.Substring(i + 13)).Trim();
					IMoStemName sn = cache.ServiceLocator.GetInstance<IMoStemNameRepository>().GetObject(Convert.ToInt32(id));
					if (sn != null)
					{
						writer.WriteStartElement("stemNameAffix");
						writer.WriteAttributeString("id", id);
						writer.WriteString(sn.Name.BestAnalysisAlternative.Text);
						writer.WriteEndElement();
					}
				}
			}
		}

		private static void WriteNotStemNameElement(XmlWriter writer, string propsText)
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

		private static void WriteAffixAlloFeatsElement(XmlWriter writer, IMoForm form, string propsText)
		{
			var sallo = form as IMoAffixAllomorph;
			if (sallo == null)
				return;  // the form could be an IMoAffixProcess in which case there are no MsEnvFeatures.
			IFsFeatStruc fsFeatStruc = sallo.MsEnvFeaturesOA;
			if (fsFeatStruc != null && !fsFeatStruc.IsEmpty)
			{
				writer.WriteStartElement("affixAlloFeats");
				WriteFeatureStructureNodes(writer, fsFeatStruc, fsFeatStruc.Hvo);
				writer.WriteEndElement(); //affixAlloFeats
			}
			else
			{   // There's no overt stem name on this allomorph, but there might be overt stem names
				// on other allomorphs in this lexical entry.  This allomorph, then, cannot bear any
				// of the features of these other stem names.  If so, there will be a property named
				// NotStemNameddd or NotStemNamedddNotStemNamedddd, etc.
				WriteNotAffixAlloFeatsElement(writer, form.Cache, propsText);
			}
		}

		private static void WriteNotAffixAlloFeatsElement(XmlWriter writer, FdoCache cache, string propsText)
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
							WriteFeatureStructureFromHvoString(writer, cache, sFsHvo);
							iNot = iNextNot + 3;
						}
						else
						{
							// is the last one
							sFsHvo = s.Substring(iNot);
							WriteFeatureStructureFromHvoString(writer, cache, sFsHvo);
							iNot = 0;
						}
					}
					writer.WriteEndElement(); //not
					writer.WriteEndElement(); //affixAlloFeats
				}
			}
		}

		private static void WriteFeatureStructureFromHvoString(XmlWriter writer, FdoCache cache, string sFsHvo)
		{
			int fsHvo = int.Parse(sFsHvo, CultureInfo.InvariantCulture);
			var fsFeatStruc = (IFsFeatStruc) cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(fsHvo);
			if (fsFeatStruc != null)
				WriteFeatureStructureNodes(writer, fsFeatStruc, fsHvo);
		}

		private static void WritePosXmlAttribute(XmlWriter writer, IPartOfSpeech pos, string sCat)
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

		public static void WriteInflClassesElement(this XmlWriter writer, FdoCache cache, string formID)
		{
			int hvo = int.Parse(formID, CultureInfo.InvariantCulture);
			ICmObject obj = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			var form = obj as IMoAffixForm;  // only for affix forms
			if (form != null)
			{
				if (form.InflectionClassesRC.Count > 0)
				{
					writer.WriteStartElement("inflClasses");
					WriteInflectionClassesAndSubclassesXmlElement(writer, form.InflectionClassesRC);
					writer.WriteEndElement();
				}
			}
		}

		private static void WriteInflectionClassesAndSubclassesXmlElement(XmlWriter writer, IEnumerable<IMoInflClass> inflectionClasses)
		{
			foreach (IMoInflClass ic in inflectionClasses)
			{
				writer.WriteStartElement("inflClass");
				writer.WriteAttributeString("hvo", ic.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString("abbr", ic.Abbreviation.BestAnalysisAlternative.Text);
				writer.WriteEndElement();
				WriteInflectionClassesAndSubclassesXmlElement(writer, ic.SubclassesOC);
			}
		}
	}
}
