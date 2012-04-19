using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Base class common to all parser trace processing
	/// </summary>
	public abstract class ParserTrace : ParserTraceBase
	{
		protected enum TransformKind
		{
			kcptParse = 0,
			kcptTrace = 1,
			kcptWordGrammarDebugger = 2,
		}
		/// <summary>
		/// Temp File names
		/// </summary>
		protected string m_sParse;
		protected string m_sTrace;

		protected WordGrammarDebugger m_wordGrammarDebugger;
		/// <summary>
		/// Transform file names
		/// </summary>
		protected string m_sFormatParse;
		protected string m_sFormatTrace;

		protected const string m_ksWordGrammarDebugger = "WordGrammarDebugger";
		/// Testing variables
		protected string[] m_saTesting = { "A", "AB", "ABC", "ABCD", "ABCDE", "ABCDEF" };
		protected int m_iTestingCount = 0;
		protected char[] m_space = { ' ' };

		protected char[] m_digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		/// <summary>
		/// For testing
		/// </summary>
		public ParserTrace()
			: base()
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		protected ParserTrace(Mediator mediator)
			: base(mediator)
		{
		}

		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		/// <param name="result">XML string of the XAmple trace output</param>
		/// <returns>URL of the resulting HTML page</returns>
		public abstract string CreateResultPage(string result);


		protected abstract string ConvertHvosToStrings(string sAdjusted, bool fIsTrace);

		/// <summary>
		/// Initialize what is needed to perform the word grammar debugging and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the node to use</param>
		/// <param name="sForm">the wordform being tried</param>
		/// <returns>temporary html file showing the results of the first step</returns>
		public abstract string SetUpWordGrammarDebuggerPage(string sNodeId, string sForm, string sLastURL);

		/// <summary>
		/// Perform another step in the word grammar debugging process and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the selected node to use</param>
		/// <returns>temporary html file showing the results of the next step</returns>
		public string PerformAnotherWordGrammarDebuggerStepPage(string sNodeId, string sForm, string sLastURL)
		{
			return m_wordGrammarDebugger.PerformAnotherWordGrammarDebuggerStepPage(sNodeId, sForm, sLastURL);
		}

		public string PopWordGrammarStack()
		{
			return m_wordGrammarDebugger.PopWordGrammarStack();
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



		protected string TransformToHtml(string sInputFile, TransformKind kind)
		{
			string sOutput = null;
			var args = new List<XmlUtils.XSLParameter>();

			switch (kind)
			{
				case TransformKind.kcptParse:
					sOutput = TransformToHtml(sInputFile, m_sParse, m_sFormatParse, args);
					break;
				case TransformKind.kcptTrace:
					string sIconPath = CreateIconPath();
					args.Add(new XmlUtils.XSLParameter("prmIconPath", sIconPath));
					sOutput = TransformToHtml(sInputFile, m_sTrace, m_sFormatTrace, args);
					break;
			}
			return sOutput;
		}


		private string CreateIconPath()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("file:///");
			sb.Append(TransformPath.Replace(@"\", "/"));
			sb.Append("/");
			return sb.ToString();
		}

		protected void ConvertMorphs(XmlDocument doc, string sNodeListToFind, bool fIdsInAttribute)
		{
			XmlNodeList nl = doc.SelectNodes(sNodeListToFind);
			if (nl != null)
			{
				foreach (XmlNode node in nl)
				{
					XmlNode alloid;
					if (fIdsInAttribute)
						alloid = node.Attributes.GetNamedItem("alloid");
					else
						alloid = node.SelectSingleNode("MoForm/@DbRef");
					int hvo = Convert.ToInt32(alloid.InnerText);
					IMoForm form = m_cache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(hvo);
					string sLongName;
					string sForm;
					string sGloss;
					string sCitationForm;
					if (form != null)
					{
						sLongName = form.LongName;
						int iFirstSpace = sLongName.IndexOf(" (");
						int iLastSpace = sLongName.LastIndexOf("):") + 2;
						sForm = sLongName.Substring(0, iFirstSpace);
						XmlNode msaid;
						if (fIdsInAttribute)
							msaid = node.Attributes.GetNamedItem("morphname");
						else
							msaid = node.SelectSingleNode("MSI/@DbRef");
						int hvoMsa = Convert.ToInt32(msaid.InnerText);
						IMoMorphSynAnalysis msa = m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().GetObject(hvoMsa);
						if (msa != null)
						{
							sGloss = msa.GetGlossOfFirstSense();
						}
						else
						{
							sGloss = sLongName.Substring(iFirstSpace, iLastSpace - iFirstSpace).Trim();
						}
						sCitationForm = sLongName.Substring(iLastSpace).Trim();
						sLongName = String.Format(ParserUIStrings.ksX_Y_Z, sForm, sGloss, sCitationForm);
					}
					else
					{
						sForm = ParserUIStrings.ksUnknownMorpheme; // in case the user continues...
						sGloss = ParserUIStrings.ksUnknownGloss;
						sCitationForm = ParserUIStrings.ksUnknownCitationForm;
						sLongName = String.Format(ParserUIStrings.ksX_Y_Z, sForm, sGloss, sCitationForm);
						throw new ApplicationException(sLongName);
					}
					XmlNode tempNode = CreateXmlElement(doc, "shortName", node);
					tempNode.InnerXml = CreateEntities(sLongName);
					tempNode = CreateXmlElement(doc, "alloform", node);
					tempNode.InnerXml = CreateEntities(sForm);
					switch (form.ClassID)
					{
						case MoStemAllomorphTags.kClassId:
							ConvertStemName(doc, node, form, tempNode);
							break;
						case MoAffixAllomorphTags.kClassId:
							ConvertAffixAlloFeats(doc, node, form, tempNode);
							ConvertStemNameAffix(doc, node, tempNode);
							break;

					}
					tempNode = CreateXmlElement(doc, "gloss", node);
					tempNode.InnerXml = CreateEntities(sGloss);
					tempNode = CreateXmlElement(doc, "citationForm", node);
					tempNode.InnerXml = CreateEntities(sCitationForm);
				}
			}
		}
		protected void CreateNotAffixAlloFeatsElement(XmlDocument doc, XmlNode node, XmlNode tempNode)
		{
			XmlNode props = node.SelectSingleNode("props");
			if (props != null)
			{
				int i = props.InnerText.IndexOf("MSEnvFSNot");
				if (i > -1)
				{
					XmlNode affixAlloFeatsNode = CreateXmlElement(doc, "affixAlloFeats", node);
					XmlNode notNode = CreateXmlElement(doc, "not", affixAlloFeatsNode);
					string s = props.InnerText.Substring(i);
					int j = s.IndexOf(' ');
					if (j > 0)
						s = props.InnerText.Substring(i, j + 1);
					int iNot = s.IndexOf("Not") + 3;
					while (iNot > 3)
					{
						int iNextNot = s.IndexOf("Not", iNot);
						string sFsHvo;
						if (iNextNot > -1)
						{
							// there are more
							sFsHvo = s.Substring(iNot, iNextNot - iNot);
							CreateFeatureStructureFromHvoString(doc, sFsHvo, notNode);
							iNot = iNextNot + 3;
						}
						else
						{
							// is the last one
							sFsHvo = s.Substring(iNot);
							CreateFeatureStructureFromHvoString(doc, sFsHvo, notNode);
							iNot = 0;
						}
					}
				}
			}
		}

		private void CreateFeatureStructureFromHvoString(XmlDocument doc, string sFSHvo, XmlNode parentNode)
		{
			int fsHvo = Convert.ToInt32(sFSHvo);
			var fsFeatStruc = (IFsFeatStruc)m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(fsHvo);
			if (fsFeatStruc != null)
			{
				CreateFeatureStructureNodes(doc, parentNode, fsFeatStruc, fsHvo);
			}
		}

		protected void ConvertStemNameAffix(XmlDocument doc, XmlNode node, XmlNode tempNode)
		{
			XmlNode props = node.SelectSingleNode("props");
			if (props != null)
			{
				string sProps = props.InnerText.Trim();
				string[] saProps = sProps.Split(' ');
				foreach (string sProp in saProps)
				{
					int i = sProp.IndexOf("StemNameAffix");
					if (i > -1)
					{
						string sId = (sProp.Substring(i + 13)).Trim();
						var sn = (IMoStemName)m_cache.ServiceLocator.GetInstance<IMoStemNameRepository>().GetObject(Convert.ToInt32(sId));
						if (sn != null)
						{
							tempNode = CreateXmlElement(doc, "stemNameAffix", node);
							CreateXmlAttribute(doc, "id", sId, tempNode);
							tempNode.InnerXml = CreateEntities(sn.Name.BestAnalysisAlternative.Text);
						}
					}
				}
			}
		}
		protected void ConvertStemName(XmlDocument doc, XmlNode node, IMoForm form, XmlNode tempNode)
		{
			IMoStemAllomorph sallo = form as IMoStemAllomorph;
			IMoStemName sn = sallo.StemNameRA;
			if (sn != null)
			{
				tempNode = CreateXmlElement(doc, "stemName", node);
				CreateXmlAttribute(doc, "id", sn.Hvo.ToString(), tempNode);
				tempNode.InnerXml = CreateEntities(sn.Name.BestAnalysisAlternative.Text);
			}
			else
			{   // There's no overt stem name on this allomorph, but there might be overt stem names
				// on other allomorphs in this lexical entry.  This allomorph, then, cannot bear any
				// of the features of these other stem names.  If so, there will be a property named
				// NotStemNameddd or NotStemNamedddNotStemNamedddd, etc.
				tempNode = CreateNotStemNameElement(doc, node, tempNode);
			}
		}
		protected XmlNode CreateNotStemNameElement(XmlDocument doc, XmlNode node, XmlNode tempNode)
		{
			XmlNode props = node.SelectSingleNode("props");
			if (props != null)
			{
				int i = props.InnerText.IndexOf("NotStemName");
				if (i > -1)
				{
					string sNotStemName;
					string s = props.InnerText.Substring(i);
					int iSpace = s.IndexOf(" ");
					if (iSpace > -1)
						sNotStemName = s.Substring(0, iSpace - 1);
					else
						sNotStemName = s;
					tempNode = CreateXmlElement(doc, "stemName", node);
					CreateXmlAttribute(doc, "id", sNotStemName, tempNode);
				}
			}
			return tempNode;
		}

		protected string CreateEntities(string sInput)
		{
			if (sInput == null)
				return "";
			string sResult1 = sInput.Replace("&", "&amp;"); // N.B. Must be ordered first!
			string sResult2 = sResult1.Replace("<", "&lt;");
			return sResult2;
		}
		protected void ConvertAffixAlloFeats(XmlDocument doc, XmlNode node, IMoForm form, XmlNode tempNode)
		{
			IMoAffixAllomorph sallo = form as IMoAffixAllomorph;
			IFsFeatStruc fsFeatStruc = sallo.MsEnvFeaturesOA;
			if (fsFeatStruc != null && !fsFeatStruc.IsEmpty)
			{
				tempNode = CreateXmlElement(doc, "affixAlloFeats", node);
				CreateFeatureStructureNodes(doc, tempNode, fsFeatStruc, fsFeatStruc.Hvo);
			}
			else
			{   // There's no overt stem name on this allomorph, but there might be overt stem names
				// on other allomorphs in this lexical entry.  This allomorph, then, cannot bear any
				// of the features of these other stem names.  If so, there will be a property named
				// NotStemNameddd or NotStemNamedddNotStemNamedddd, etc.
				CreateNotAffixAlloFeatsElement(doc, node, tempNode);
			}
		}
		protected void AddMsaNodes(bool fIsTrace)
		{
			string sXPath;
			string sAttrXPath;
			if (fIsTrace)
			{
				sXPath = "//morph[@morphname]";
				sAttrXPath = "@morphname";
			}
			else
			{
				sXPath = "//Morph/MSI";
				sAttrXPath = "@DbRef";
			}
			XmlNodeList nl = m_parseResult.SelectNodes(sXPath);
			if (nl != null)
			{
				foreach (XmlNode node in nl)
				{
					CreateMsaXmlElement(node, m_parseResult, node, sAttrXPath);
				}
			}
		}
	}
}
