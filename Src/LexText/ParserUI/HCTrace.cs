using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class HCTrace : ParserTrace
	{
		/// <summary>
		/// Temp File names
		/// </summary>
		const string m_ksHCParse = "HCParse";
		const string m_ksHCTrace = "HCTrace";

		/// <summary>
		/// For testing
		/// </summary>
		public HCTrace()
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public HCTrace(Mediator mediator)
			: base(mediator)
		{
			m_sParse = m_ksHCParse;
			m_sTrace = m_ksHCTrace;
			m_sFormatParse = "FormatXAmpleParse.xsl"; // the XAmple one works fine with Hermit Crab parse output
			m_sFormatTrace = "FormatHCTrace.xsl";

		}
		public override string CreateResultPage(string result)
		{
			bool fIsTrace = result.Contains("<Trace>");

			m_parseResult = new XmlDocument();
			m_parseResult.LoadXml(ConvertHvosToStrings(result, fIsTrace));

			AddMsaNodes(false);

			string sInput = CreateTempFile(m_sTrace, "xml");
			m_parseResult.Save(sInput);
			XPathDocument xpath = new XPathDocument(sInput);

			TransformKind kind = (fIsTrace ? TransformKind.kcptTrace : TransformKind.kcptParse);
			string sOutput = TransformToHtml(xpath, kind);
			return sOutput;
		}

		protected override string ConvertHvosToStrings(string sAdjusted, bool fIsTrace)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(sAdjusted);
			if (fIsTrace)
			{
				ConvertMorphs(doc, "//RuleAllomorph/Morph | //RootAllomorph/Morph | //Morphs/Morph", false);
				//ConvertMorphs(doc, "//WordGrammarAttempt/Morphs", true);
			}
			else
			{
				ConvertMorphs(doc, "//Morphs/Morph", false);
			}
			return doc.InnerXml;
		}

		protected override void CreateMorphNodes(XmlDocument doc, XmlNode seqNode, string sNodeId)
		{
			string s = "//WordGrammarAttempt[Id=\"" + sNodeId + "\"]";
			XmlNode node = m_parseResult.SelectSingleNode(s);
			if (node != null)
			{
				XmlNodeList morphs = node.SelectNodes("Morphs");
				foreach (XmlNode morph in morphs)
				{
					CreateMorphXmlElement(doc, seqNode, morph);
				}

			}
		}

		private void CreateMorphXmlElement(XmlDocument doc, XmlNode seqNode, XmlNode node)
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

	}
}
