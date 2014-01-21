using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public abstract class ParserTraceBase
	{
		/// <summary>
		/// xCore Mediator.
		/// </summary>
		protected Mediator m_mediator;

		protected FdoCache m_cache;
		protected string m_sDataBaseName = "";
		/// <summary>
		/// The parse result xml document
		/// </summary>
		protected XmlDocument m_parseResult;
		/// <summary>
		/// the latest word grammar debugging step xml document
		/// </summary>
		protected string m_sWordGrammarDebuggerXmlFile;

		public ParserTraceBase()
		{}
				/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		protected ParserTraceBase(Mediator mediator)
		{
			m_mediator = mediator;
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_sDataBaseName = m_cache.ProjectId.Name;
		}
		protected void CopyXmlAttribute(XmlDocument doc, XmlNode node, string sAttrName, XmlNode morphNode)
		{
			XmlNode attr = node.SelectSingleNode("@" + sAttrName);
			if (attr != null && sAttrName != null)
				CreateXmlAttribute(doc, sAttrName, attr.InnerText, morphNode);
		}

		protected void CreateDerivMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoDerivAffMsa derivMsa)
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
		protected void CreateFeatureStructureNodes(XmlDocument doc, XmlNode msaNode, IFsFeatStruc fs, int id)
		{
			CreateFeatureStructureNodes(doc, msaNode, fs, id, "fs");
		}
		protected void CreateFeatureStructureNodes(XmlDocument doc, XmlNode msaNode, IFsFeatStruc fs, int id, string sFSName)
		{
			if (fs == null)
				return;
			XmlNode fsNode = CreateXmlElement(doc, sFSName, msaNode);
			CreateXmlAttribute(doc, "id", id.ToString(), fsNode);
			foreach (IFsFeatureSpecification spec in fs.FeatureSpecsOC)
			{
				XmlNode feature = CreateXmlElement(doc, "feature", fsNode);
				XmlNode name = CreateXmlElement(doc, "name", feature);
				name.InnerText = spec.FeatureRA.Abbreviation.BestAnalysisAlternative.Text;
				XmlNode fvalue = CreateXmlElement(doc, "value", feature);
				IFsClosedValue cv = spec as IFsClosedValue;
				if (cv != null)
					fvalue.InnerText = cv.ValueRA.Abbreviation.BestAnalysisAlternative.Text;
				else
				{
					IFsComplexValue complex = spec as IFsComplexValue;
					if (complex == null)
						continue; // skip this one since we're not dealing with it yet
					IFsFeatStruc nestedFs = complex.ValueOA as IFsFeatStruc;
					if (nestedFs != null)
						CreateFeatureStructureNodes(doc, fvalue, nestedFs, 0, "fs");
				}
			}
		}
		protected void CreateFromPOSNodes(XmlDocument doc, XmlNode msaNode, IFdoReferenceCollection<IPartOfSpeech> fromPOSes, string sElementName)
		{
			if (fromPOSes == null || fromPOSes.Count < 1)
				return;
			foreach (IPartOfSpeech pos in fromPOSes)
			{
				XmlNode posNode = CreateXmlElement(doc, sElementName, msaNode);
				CreateXmlAttribute(doc, "fromCat", pos.Hvo.ToString(), posNode);
				CreateXmlAttribute(doc, "fromCatAbbr", pos.Abbreviation.BestAnalysisAlternative.Text, posNode);
			}
		}
		protected void CreateInflectionClasses(XmlDocument doc, XmlNode morphNode)
		{
			string sAlloId = XmlUtils.GetOptionalAttributeValue(morphNode, "alloid");
			if (sAlloId == null)
				return;
			int hvoAllomorph = Convert.ToInt32(sAlloId);
			// use IMoForm instead of IMoAffixForm or IMoAffixAllomorph because it could be an IMoStemAllomorph
			IMoForm form = m_cache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(hvoAllomorph);
			if (form == null)
				return;
			if (!(form is IMoAffixForm))
				return;
			foreach (IMoInflClass ic in ((IMoAffixForm)form).InflectionClassesRC)
			{
				XmlNode icNode = CreateXmlElement(doc, "inflectionClass", morphNode);
				CreateXmlAttribute(doc, "id", ic.Hvo.ToString(), icNode);
				CreateXmlAttribute(doc, "abbr", ic.Abbreviation.BestAnalysisAlternative.Text, icNode);
			}
		}
		private void CreateInflectionClassesAndSubclassesXmlElement(XmlDocument doc,
	System.Collections.Generic.IEnumerable<IMoInflClass> inflectionClasses,
	XmlNode inflClasses)
		{
			foreach (IMoInflClass ic in inflectionClasses)
			{
				XmlNode inflClass = CreateXmlElement(doc, "inflClass", inflClasses);
				CreateXmlAttribute(doc, "hvo", ic.Hvo.ToString(), inflClass);
				CreateXmlAttribute(doc, "abbr", ic.Abbreviation.BestAnalysisAlternative.Text, inflClass);
				CreateInflectionClassesAndSubclassesXmlElement(doc, ic.SubclassesOC, inflClasses);
			}
		}
		protected void CreateInflectionClassXmlAttribute(XmlDocument doc, XmlNode msaNode, IMoInflClass inflClass, string sInflClass)
		{
			if (inflClass != null)
			{
				CreateXmlAttribute(doc, sInflClass, inflClass.Hvo.ToString(), msaNode);
				string sInflClassAbbr;
				if (inflClass.Hvo > 0)
					sInflClassAbbr = inflClass.Abbreviation.BestAnalysisAlternative.Text;
				else
					sInflClassAbbr = "";
				CreateXmlAttribute(doc, sInflClass + "Abbr", sInflClassAbbr, msaNode);
			}
			else
				CreateXmlAttribute(doc, sInflClass, "0", msaNode);
		}

		protected void CreateInflMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoInflAffMsa inflMsa)
		{
			XmlNode inflMsaNode = CreateXmlElement(doc, "inflMsa", morphNode);
			CreatePOSXmlAttribute(doc, inflMsaNode, inflMsa.PartOfSpeechRA, "cat");
			// handle any slot
			HandleSlotInfoForInflectionalMsa(inflMsa, doc, inflMsaNode, morphNode);
			CreateFeatureStructureNodes(doc, inflMsaNode, inflMsa.InflFeatsOA, inflMsa.Hvo);
			CreateProductivityRestrictionNodes(doc, inflMsaNode, inflMsa.FromProdRestrictRC, "fromProductivityRestriction");
		}
		protected virtual void CreateMorphAffixAlloFeatsXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode affixAlloFeatsNode = node.SelectSingleNode("affixAlloFeats");
			if (affixAlloFeatsNode != null)
			{
				morphNode.InnerXml += affixAlloFeatsNode.OuterXml;
			}
		}
		protected void CreateMorphAlloformXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode formNode = node.SelectSingleNode("alloform");
			if (formNode != null)
				morphNode.InnerXml += "<alloform>" + formNode.InnerXml + "</alloform>";
		}
		protected void CreateMorphCitationFormXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode citationFormNode = node.SelectSingleNode("citationForm");
			if (citationFormNode != null)
				morphNode.InnerXml += "<citationForm>" + citationFormNode.InnerXml + "</citationForm>";
		}

		protected void CreateMorphGlossXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode glossNode = node.SelectSingleNode("gloss");
			if (glossNode != null)
				morphNode.InnerXml += "<gloss>" + glossNode.InnerXml + "</gloss>";
		}
		protected void CreateMorphInflectionClassesXmlElement(XmlDocument doc, XmlNode node, XmlNode morphNode)
		{
			XmlNode attr;
			attr = node.SelectSingleNode("@alloid");
			if (attr != null)
			{
				XmlNode alloid = node.Attributes.GetNamedItem("alloid");
				int hvo = Convert.ToInt32(alloid.InnerText);
				ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				IMoAffixForm form = obj as IMoAffixForm;  // only for affix forms
				if (form != null)
				{
					if (form.InflectionClassesRC.Count > 0)
					{
						XmlNode inflClasses = CreateXmlElement(doc, "inflClasses", morphNode);
						IFdoReferenceCollection<IMoInflClass> inflectionClasses = form.InflectionClassesRC;
						CreateInflectionClassesAndSubclassesXmlElement(doc, inflectionClasses, inflClasses);
					}
				}
			}
		}


		protected abstract void CreateMorphNodes(XmlDocument doc, XmlNode seqNode, string sNodeId);

		protected virtual void CreateMorphShortNameXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode formNode = node.SelectSingleNode("shortName");
			if (formNode != null)
				morphNode.InnerXml = "<shortName>" + formNode.InnerXml + "</shortName>";
		}
		protected void CreateMorphStemNameXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode stemNameNode = node.SelectSingleNode("stemName");
			if (stemNameNode != null)
			{
				XmlNode idNode = stemNameNode.SelectSingleNode("@id");
				if (idNode != null)
					morphNode.InnerXml += "<stemName" + " id=\"" + idNode.Value + "\">" + stemNameNode.InnerXml + "</stemName>";
			}
		}

		protected void CreateMorphWordTypeXmlAttribute(XmlNode node, XmlDocument doc, XmlNode morphNode)
		{
			XmlNode attr = node.SelectSingleNode("@type");
			if (attr != null)
				CreateXmlAttribute(doc, "type", attr.Value, morphNode);
			attr = node.SelectSingleNode("@wordType");
			if (attr != null)
				CreateXmlAttribute(doc, "wordType", attr.Value, morphNode);
		}
		protected void CreateMorphXmlElement(XmlDocument doc, XmlNode seqNode, XmlNode node)
		{
			XmlNode morphNode = CreateXmlElement(doc, "morph", seqNode);
			CopyXmlAttribute(doc, node, "alloid", morphNode);
			CopyXmlAttribute(doc, node, "morphname", morphNode);
			CreateMorphWordTypeXmlAttribute(node, doc, morphNode);
			CreateMorphShortNameXmlElement(node, morphNode);
			CreateMorphAlloformXmlElement(node, morphNode);
			CreateMorphStemNameXmlElement(node, morphNode);
			CreateMorphAffixAlloFeatsXmlElement(node, morphNode);
			CreateMorphGlossXmlElement(node, morphNode);
			CreateMorphCitationFormXmlElement(node, morphNode);
			CreateMorphInflectionClassesXmlElement(doc, node, morphNode);
			CreateMsaXmlElement(node, doc, morphNode, "@morphname");
		}
		protected void CreateMsaXmlElement(XmlNode node, XmlDocument doc, XmlNode morphNode, string sHvo)
		{
			XmlNode attr;
			// morphname contains the hvo of the msa
			attr = node.SelectSingleNode(sHvo);
			if (attr != null)
			{
				string sObjHvo = attr.Value;
				// Irregulary inflected forms can have a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
				var indexOfPeriod = IndexOfPeriodInMsaHvo(ref sObjHvo);
				ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(Convert.ToInt32(sObjHvo));
				switch (obj.GetType().Name)
				{
					default:
						throw new ApplicationException(String.Format("Invalid MSA type: {0}.", obj.GetType().Name));
					case "MoStemMsa":
						IMoStemMsa stemMsa = obj as IMoStemMsa;
						CreateStemMsaXmlElement(doc, morphNode, stemMsa);
						break;
					case "MoInflAffMsa":
						IMoInflAffMsa inflMsa = obj as IMoInflAffMsa;
						CreateInflectionClasses(doc, morphNode);
						CreateInflMsaXmlElement(doc, morphNode, inflMsa);
						break;
					case "MoDerivAffMsa":
						IMoDerivAffMsa derivMsa = obj as IMoDerivAffMsa;
						CreateDerivMsaXmlElement(doc, morphNode, derivMsa);
						break;
					case "MoUnclassifiedAffixMsa":
						IMoUnclassifiedAffixMsa unclassMsa = obj as IMoUnclassifiedAffixMsa;
						CreateUnclassifedMsaXmlElement(doc, morphNode, unclassMsa);
						break;
					case "LexEntry":
						// is an irregularly inflected form
						// get the MoStemMsa of its variant
						var entry = obj as ILexEntry;
						if (entry.EntryRefsOS.Count > 0)
						{
							var index = IndexOfLexEntryRef(attr.Value, indexOfPeriod);
							var lexEntryRef = entry.EntryRefsOS[index];
							var sense = FDO.DomainServices.MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
							stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
							CreateStemMsaXmlElement(doc, morphNode, stemMsa);
						}
						break;
					case "LexEntryInflType":
						// This is one of the null allomorphs we create when building the
						// input for the parser in order to still get the Word Grammar to have something in any
						// required slots in affix templates.
						CreateInflMsaForLexEntryInflType(doc, morphNode, obj as ILexEntryInflType);
						break;
				}
			}
		}

		protected static int IndexOfLexEntryRef(string sHvo, int indexOfPeriod)
		{
			int index = 0;
			if (indexOfPeriod >= 0)
			{
				string sIndex = sHvo.Substring(indexOfPeriod+1);
				index = Convert.ToInt32(sIndex);
			}
			return index;
		}

		protected static int IndexOfPeriodInMsaHvo(ref string sObjHvo)
		{
			// Irregulary inflected forms can a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
			int indexOfPeriod = sObjHvo.IndexOf('.');
			if (indexOfPeriod >= 0)
			{
				sObjHvo = sObjHvo.Substring(0, indexOfPeriod);
			}
			return indexOfPeriod;
		}

		protected void CreateInflMsaForLexEntryInflType(XmlDocument doc, XmlNode node, ILexEntryInflType lexEntryInflType)
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
				slot = m_cache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(Convert.ToInt32(slotId.InnerText));
			else
			{
				var slots = lexEntryInflType.SlotsRC;
				IMoInflAffixSlot firstSlot = null;
				foreach (var slot1 in slots)  // there's got to be a better way to do this...
				{
					firstSlot = slot1;
					break;
				}
				slot = firstSlot;
			}
			XmlNode nullInflMsaNode;
			nullInflMsaNode = CreateXmlElement(doc, "inflMsa", node);
			CreatePOSXmlAttribute(doc, nullInflMsaNode, slot.Owner as IPartOfSpeech, "cat");
			CreateXmlAttribute(doc, "slot", slot.Hvo.ToString(), nullInflMsaNode);
			CreateXmlAttribute(doc, "slotAbbr", slot.Name.BestAnalysisAlternative.Text, nullInflMsaNode);
			CreateXmlAttribute(doc, "slotOptional", "false", nullInflMsaNode);
			CreateFeatureStructureNodes(doc, nullInflMsaNode, lexEntryInflType.InflFeatsOA, lexEntryInflType.Hvo);
		}

		protected void CreatePOSXmlAttribute(XmlDocument doc, XmlNode msaNode, IPartOfSpeech pos, string sCat)
		{
			if (pos != null)
			{
				CreateXmlAttribute(doc, sCat, pos.Hvo.ToString(), msaNode);
				string sPosAbbr;
				if (pos.Hvo > 0)
					sPosAbbr = pos.Abbreviation.BestAnalysisAlternative.Text;
				else
					sPosAbbr = "??";
				CreateXmlAttribute(doc, sCat + "Abbr", sPosAbbr, msaNode);
			}
			else
				CreateXmlAttribute(doc, sCat, "0", msaNode);
		}

		protected void CreateProductivityRestrictionNodes(XmlDocument doc, XmlNode msaNode, IFdoReferenceCollection<ICmPossibility> prodRests, string sElementName)
		{
			if (prodRests == null || prodRests.Count < 1)
				return;
			foreach (ICmPossibility pr in prodRests)
			{
				XmlNode prNode = CreateXmlElement(doc, sElementName, msaNode);
				CreateXmlAttribute(doc, "id", pr.Hvo.ToString(), prNode);
				XmlNode prName = CreateXmlElement(doc, "name", prNode);
				prName.InnerText = pr.Name.BestAnalysisAlternative.Text;
			}
		}
		protected void CreateRequiresInflectionXmlAttribute(XmlDocument doc, IPartOfSpeech pos, XmlNode msaNode)
		{
			string sPlusMinus;
			if (RequiresInflection(pos))
				sPlusMinus = "+";
			else
				sPlusMinus = "-";
			CreateXmlAttribute(doc, "requiresInfl", sPlusMinus, msaNode);
		}
		public string CreateTempFile(string sPrefix, string sExtension)
		{
			string sTempFileName = Path.Combine(Path.GetTempPath(), m_sDataBaseName + sPrefix) + "." + sExtension;
			using (StreamWriter sw = File.CreateText(sTempFileName))
				sw.Close();
			return sTempFileName;
		}
		protected static void CreateXmlAttribute(XmlDocument doc, string sAttrName, string sAttrValue, XmlNode elementNode)
		{
			XmlNode attr = doc.CreateNode(XmlNodeType.Attribute, sAttrName, null);
			attr.Value = sAttrValue;
			elementNode.Attributes.SetNamedItem(attr);
		}
		/// <summary>
		/// Create an xml element and add it to the tree
		/// </summary>
		/// <param name="doc">Xml document containing the element</param>
		/// <param name="sElementName">name of the element to create</param>
		/// <param name="parentNode">owner of the newly created element</param>
		/// <returns>newly created element node</returns>
		protected XmlNode CreateXmlElement(XmlDocument doc, string sElementName, XmlNode parentNode)
		{
			XmlNode node = doc.CreateNode(XmlNodeType.Element, sElementName, null);
			parentNode.AppendChild(node);
			return node;
		}
		protected void HandleSlotInfoForInflectionalMsa(IMoInflAffMsa inflMsa, XmlDocument doc, XmlNode inflMsaNode, XmlNode morphNode)
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
			CreateXmlAttribute(doc, "slot", slotHvo.ToString(), inflMsaNode);
			string sSlotOptional = "false";
			string sSlotAbbr = "??";
			if (slotHvo > 0)
			{
				var slot = m_cache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(slotHvo);
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
		protected void CreateStemMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoStemMsa stemMsa)
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
						int clsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(pos.Owner.Hvo).ClassID;
						if (clsid == PartOfSpeechTags.kClassId)
							pos = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(pos.Owner.Hvo);
						else
							pos = null;
					}
				}
				if (inflClassHvo != 0)
					inflClass = m_cache.ServiceLocator.GetInstance<IMoInflClassRepository>().GetObject(inflClassHvo);
			}
			CreateInflectionClassXmlAttribute(doc, stemMsaNode, inflClass, "inflClass");
			CreateRequiresInflectionXmlAttribute(doc,
				stemMsa.PartOfSpeechRA,
				stemMsaNode);
			CreateFeatureStructureNodes(doc, stemMsaNode, stemMsa.MsFeaturesOA, stemMsa.Hvo);
			CreateProductivityRestrictionNodes(doc, stemMsaNode, stemMsa.ProdRestrictRC, "productivityRestriction");
			CreateFromPOSNodes(doc, stemMsaNode, stemMsa.FromPartsOfSpeechRC, "fromPartsOfSpeech");
		}
		protected void CreateUnclassifedMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoUnclassifiedAffixMsa unclassMsa)
		{
			XmlNode unclassMsaNode = CreateXmlElement(doc, "unclassMsa", morphNode);
			CreatePOSXmlAttribute(doc, unclassMsaNode, unclassMsa.PartOfSpeechRA, "fromCat");
		}
		/// <summary>
		/// Determine if a PartOfSpeech requires inflection.
		/// If it or any of its parent POSes have a template, it requires inflection.
		/// If it is null we default to not requiring inflection.
		/// </summary>
		/// <param name="pos">the Part of Speech</param>
		/// <returns>true if it does, false otherwise</returns>
		protected bool RequiresInflection(IPartOfSpeech pos)
		{
			return pos != null && pos.RequiresInflection;
		}
		/// <summary>
		/// Path to transforms
		/// </summary>
		protected string TransformPath
		{
			get { return FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer/Configuration/Words/Analyses/TraceParse"); }
		}
		protected string TransformToHtml(string sInputFile, string sTempFileBase, string sTransformFile, List<XmlUtils.XSLParameter> args)
		{
			string sOutput = CreateTempFile(sTempFileBase, "htm");
			string sTransform = Path.Combine(TransformPath, sTransformFile);
			SetWritingSystemBasedArguments(args);
			AddParserSpecificArguments(args);
			XmlUtils.TransformFileToFile(sTransform, args.ToArray(), sInputFile, sOutput);
			return sOutput;
		}

		private void SetWritingSystemBasedArguments(List<XmlUtils.XSLParameter> args)
		{
			ILgWritingSystemFactory wsf = m_cache.WritingSystemFactory;
			IWritingSystemContainer wsContainer = m_cache.ServiceLocator.WritingSystems;
			IWritingSystem defAnalWs = wsContainer.DefaultAnalysisWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defAnalWs.Handle, m_mediator, wsf))
			{
				args.Add(new XmlUtils.XSLParameter("prmAnalysisFont", myFont.FontFamily.Name));
				args.Add(new XmlUtils.XSLParameter("prmAnalysisFontSize", myFont.Size + "pt"));
			}

			IWritingSystem defVernWs = wsContainer.DefaultVernacularWritingSystem;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(defVernWs.Handle, m_mediator, wsf))
			{
				args.Add(new XmlUtils.XSLParameter("prmVernacularFont", myFont.FontFamily.Name));
				args.Add(new XmlUtils.XSLParameter("prmVernacularFontSize", myFont.Size + "pt"));
			}

			string sRTL = defVernWs.RightToLeftScript ? "Y" : "N";
			args.Add(new XmlUtils.XSLParameter("prmVernacularRTL", sRTL));
		}

		protected virtual void AddParserSpecificArguments(List<XmlUtils.XSLParameter> args)
		{
			// default is to do nothing
		}
	}
}
