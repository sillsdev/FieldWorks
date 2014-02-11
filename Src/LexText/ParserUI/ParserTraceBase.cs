using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.WordWorks.Parser;
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

		protected ParserTraceBase()
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
		private void CreateInflectionClassesAndSubclassesXmlElement(XmlDocument doc,
			IEnumerable<IMoInflClass> inflectionClasses,
			XmlNode inflClasses)
		{
			foreach (IMoInflClass ic in inflectionClasses)
			{
				XmlNode inflClass = CreateXmlElement(doc, "inflClass", inflClasses);
				CreateXmlAttribute(doc, "hvo", ic.Hvo.ToString(CultureInfo.InvariantCulture), inflClass);
				CreateXmlAttribute(doc, "abbr", ic.Abbreviation.BestAnalysisAlternative.Text, inflClass);
				CreateInflectionClassesAndSubclassesXmlElement(doc, ic.SubclassesOC, inflClasses);
			}
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
			XmlNode attr = node.SelectSingleNode("@alloid");
			if (attr != null && node.Attributes != null)
			{
				XmlNode alloid = node.Attributes.GetNamedItem("alloid");
				int hvo = Convert.ToInt32(alloid.InnerText);
				ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				var form = obj as IMoAffixForm;  // only for affix forms
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
			ParserXMLGenerator.CreateMsaXmlElement(node, doc, morphNode, "@morphname", m_cache);
		}
		public string CreateTempFile(string sPrefix, string sExtension)
		{
			string sTempFileName = Path.Combine(Path.GetTempPath(), m_sDataBaseName + sPrefix) + "." + sExtension;
			using (StreamWriter sw = File.CreateText(sTempFileName))
				sw.Close();
			return sTempFileName;
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

			string sRtl = defVernWs.RightToLeftScript ? "Y" : "N";
			args.Add(new XmlUtils.XSLParameter("prmVernacularRTL", sRtl));
		}

		protected virtual void AddParserSpecificArguments(List<XmlUtils.XSLParameter> args)
		{
			// default is to do nothing
		}

		protected static XmlNode CreateXmlElement(XmlDocument doc, string sElementName, XmlNode parentNode)
		{
			return ParserXMLGenerator.CreateXmlElement(doc, sElementName, parentNode);
		}

		protected static void CreateXmlAttribute(XmlDocument doc, string sAttrName, string sAttrValue, XmlNode elementNode)
		{
			ParserXMLGenerator.CreateXmlAttribute(doc, sAttrName, sAttrValue, elementNode);
		}
	}
}
