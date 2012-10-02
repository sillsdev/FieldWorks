using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Base class common to all parser trace processing
	/// </summary>
	public abstract class ParserTrace
	{
		protected enum TransformKind
		{
			kcptParse = 0,
			kcptTrace = 1,
			kcptWordGrammarDebugger = 2,
		}
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
		/// <summary>
		/// Word Grammar step stack
		/// </summary>
		protected Stack<WordGrammarStepPair> m_XmlHtmlStack;
		/// <summary>
		/// Temp File names
		/// </summary>
		protected string m_sParse;
		protected string m_sTrace;

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
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		protected ParserTrace(Mediator mediator)
		{
			m_mediator = mediator;
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_sDataBaseName = m_cache.DatabaseName;

			m_XmlHtmlStack = new Stack<WordGrammarStepPair>();
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
		public string SetUpWordGrammarDebuggerPage(string sNodeId, string sForm, string sLastURL)
		{
			m_XmlHtmlStack.Push(new WordGrammarStepPair(null, sLastURL));
			string sInitialAnalysisXml = CreateAnalysisXml(sNodeId, sForm);
			string sHtmlPage = CreateWordDebuggerPage(sInitialAnalysisXml);
			return sHtmlPage;
		}

		/// <summary>
		/// Perform another step in the word grammar debugging process and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the selected node to use</param>
		/// <returns>temporary html file showing the results of the next step</returns>
		public string PerformAnotherWordGrammarDebuggerStepPage(string sNodeId, string sForm, string sLastURL)
		{
			m_XmlHtmlStack.Push(new WordGrammarStepPair(m_sWordGrammarDebuggerXmlFile, sLastURL));
			string sNextXml = CreateSelectedWordGrammarXml(sNodeId, sForm);
			string sHtmlPage = CreateWordDebuggerPage(sNextXml);
			return sHtmlPage;
		}
		public string PopWordGrammarStack()
		{
			WordGrammarStepPair wgsp;
			if (m_XmlHtmlStack.Count > 0)
			{
				wgsp = m_XmlHtmlStack.Pop(); // get the previous one
				m_sWordGrammarDebuggerXmlFile = wgsp.XmlFile;
				return wgsp.HtmlFile;
			}
			return "unknown";
		}

		private string CreateWordDebuggerPage(string sXmlFile)
		{
			// apply word grammar step transform file
			XPathDocument xpath = new XPathDocument(sXmlFile);
			string sXmlOutput = TransformToXml(xpath);
			m_sWordGrammarDebuggerXmlFile = sXmlOutput;
			// format the result
			xpath = new XPathDocument(sXmlOutput);
			string sOutput = TransformToHtml(xpath, TransformKind.kcptWordGrammarDebugger);
			return sOutput;
		}

		private string CreateAnalysisXml(string sNodeId, string sForm)
		{
			string sResult;
			if (m_parseResult != null)
			{
				XmlDocument doc = new XmlDocument();
				XmlNode wordNode = CreateXmlElement(doc, "word", doc);
				XmlNode formNode = CreateXmlElement(doc, "form", wordNode);
				formNode.InnerXml = sForm;
				XmlNode seqNode = CreateXmlElement(doc, "seq", wordNode);

				// following for debugging as needed
				sResult = CreateTempFile("ParseResult", "xml");
				m_parseResult.Save(sResult);
				CreateMorphNodes(doc, seqNode, sNodeId);

				sResult = CreateTempFile(CreateWordGrammarDebuggerFileName(), "xml");
				doc.Save(sResult);
			}
			else
				sResult = "error!";
			return sResult;
		}

		protected abstract void CreateMorphNodes(XmlDocument doc, XmlNode seqNode, string sNodeId);

		protected void CreateMorphWordTypeXmlAttribute(XmlNode node, XmlDocument doc, XmlNode morphNode)
		{
			XmlNode attr = node.SelectSingleNode("@type");
			if (attr != null)
				CreateXmlAttribute(doc, "type", attr.Value, morphNode);
			attr = node.SelectSingleNode("@wordType");
			if (attr != null)
				CreateXmlAttribute(doc, "wordType", attr.Value, morphNode);
		}
		protected void CreateMorphShortNameXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode formNode = node.SelectSingleNode("shortName");
			if (formNode != null)
				morphNode.InnerXml = "<shortName>" + formNode.InnerXml + "</shortName>";
		}
		protected void CreateMorphAlloformXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode formNode = node.SelectSingleNode("alloform");
			if (formNode != null)
				morphNode.InnerXml += "<alloform>" + formNode.InnerXml + "</alloform>";
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
		protected void CreateMorphAffixAlloFeatsXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode affixAlloFeatsNode = node.SelectSingleNode("affixAlloFeats");
			if (affixAlloFeatsNode != null)
			{
				morphNode.InnerXml += affixAlloFeatsNode.OuterXml;
			}
		}
		protected void CreateMorphGlossXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode glossNode = node.SelectSingleNode("gloss");
			if (glossNode != null)
				morphNode.InnerXml += "<gloss>" + glossNode.InnerXml + "</gloss>";
		}
		protected void CreateMorphCitationFormXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode citationFormNode = node.SelectSingleNode("citationForm");
			if (citationFormNode != null)
				morphNode.InnerXml += "<citationForm>" + citationFormNode.InnerXml + "</citationForm>";
		}
		protected void CreateMorphInflectionClassesXmlElement(XmlDocument doc, XmlNode node, XmlNode morphNode)
		{
			XmlNode attr;
			attr = node.SelectSingleNode("@alloid");
			if (attr != null)
			{
				XmlNode alloid = node.Attributes.GetNamedItem("alloid");
				int hvo = Convert.ToInt32(alloid.InnerText);
				ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvo);
				IMoAffixForm form = obj as IMoAffixForm;  // only for affix forms
				if (form != null)
				{
					if (form.InflectionClassesRC.Count > 0)
					{
						XmlNode inflClasses = CreateXmlElement(doc, "inflClasses", morphNode);
						FdoReferenceCollection<IMoInflClass> inflectionClasses = form.InflectionClassesRC;
						CreateInflectionClassesAndSubclassesXmlElement(doc, inflectionClasses, inflClasses);
					}
				}
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



		private string CreateSelectedWordGrammarXml(string sNodeId, string sForm)
		{
			string sResult;
			if (m_sWordGrammarDebuggerXmlFile != null)
			{
				XmlDocument lastDoc = new XmlDocument();
				lastDoc.Load(m_sWordGrammarDebuggerXmlFile);
				XmlDocument doc = new XmlDocument();
				XmlNode wordNode = CreateXmlElement(doc, "word", doc);
				XmlNode formNode = CreateXmlElement(doc, "form", wordNode);
				formNode.InnerXml = sForm;
				// Find the sNode'th seq node
				string sSelect = "//seq[position()='" + sNodeId + "']";
				XmlNode selectedSeqNode = lastDoc.SelectSingleNode(sSelect);
				// create the "result so far node"
				XmlNode resultSoFarNode = CreateXmlElement(doc, "resultSoFar", wordNode);
				resultSoFarNode.InnerXml = selectedSeqNode.InnerXml;
				// create the seq node
				XmlNode seqNode = CreateXmlElement(doc, "seq", wordNode);
				seqNode.InnerXml = selectedSeqNode.InnerXml;
				// save result
				sResult = CreateTempFile("SelectedWordGrammarXml", "xml");
				doc.Save(sResult);
			}
			else
				sResult = "error!";
			return sResult;
		}
		protected string TransformToHtml(XPathDocument doc, TransformKind kind)
		{
			string sOutput = null;
			XslCompiledTransform transformer = new XslCompiledTransform();
			XsltArgumentList args = new XsltArgumentList();
			switch (kind)
			{
				case TransformKind.kcptParse:
					sOutput = CreateTempFile(m_sParse, "htm");
					transformer.Load(Path.Combine(TransformPath, m_sFormatParse));
					break;
				case TransformKind.kcptTrace:
					sOutput = CreateTempFile(m_sTrace, "htm");
					transformer.Load(Path.Combine(TransformPath, m_sFormatTrace));
					string sIconPath = CreateIconPath();
					args.AddParam("prmIconPath", "", sIconPath);
					break;
				case TransformKind.kcptWordGrammarDebugger:
					string sDepthLevel = m_XmlHtmlStack.Count.ToString();
					sOutput = CreateTempFile(CreateWordGrammarDebuggerFileName(), "htm");
					transformer.Load(Path.Combine(TransformPath, "FormatXAmpleWordGrammarDebuggerResult.xsl"));
					break;
			}
			TextWriter writer = File.CreateText(sOutput);
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			Font myFont = FontHeightAdjuster.GetFontForNormalStyle(m_cache.LangProject.DefaultAnalysisWritingSystem, m_mediator, wsf);
			args.AddParam("prmAnalysisFont", "", myFont.FontFamily.Name);
			args.AddParam("prmAnalysisFontSize", "", myFont.Size.ToString() + "pt");
			int vernWs = m_cache.LangProject.DefaultVernacularWritingSystem;
			myFont = FontHeightAdjuster.GetFontForNormalStyle(vernWs, m_mediator, wsf);
			args.AddParam("prmVernacularFont", "", myFont.FontFamily.Name);
			args.AddParam("prmVernacularFontSize", "", myFont.Size.ToString() + "pt");
			string sRTL = "N";
			IWritingSystem wsObj = wsf.get_EngineOrNull(vernWs);
			if (wsObj != null && wsObj.RightToLeft)
				sRTL = "Y";
			args.AddParam("prmVernacularRTL", "", sRTL);
			transformer.Transform(doc, args, writer);
			writer.Close();
			return sOutput;
		}
		private string TransformToXml(XPathDocument doc)
		{
			string sOutput = CreateTempFile(CreateWordGrammarDebuggerFileName(), "xml");
			TextWriter writer = File.CreateText(sOutput);
			XslCompiledTransform transformer = new XslCompiledTransform();
			XsltArgumentList args = new XsltArgumentList();
			string sName = m_sDataBaseName + "XAmpleWordGrammarDebugger" + ".xsl";
			transformer.Load(Path.Combine(Path.GetDirectoryName(sOutput), sName));
			transformer.Transform(doc, args, writer);
			writer.Close();
			return sOutput;
		}

		public string CreateTempFile(string sPrefix, string sExtension)
		{
			string sTempFileName = Path.Combine(System.IO.Path.GetTempPath(), m_sDataBaseName + sPrefix) + "." + sExtension;
			StreamWriter sw = File.CreateText(sTempFileName);
			sw.Close();
			return sTempFileName;
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

		protected static void CreateXmlAttribute(XmlDocument doc, string sAttrName, string sAttrValue, XmlNode elementNode)
		{
			XmlNode attr = doc.CreateNode(XmlNodeType.Attribute, sAttrName, null);
			attr.Value = sAttrValue;
			elementNode.Attributes.SetNamedItem(attr);
		}
		protected void CopyXmlAttribute(XmlDocument doc, XmlNode node, string sAttrName, XmlNode morphNode)
		{
			XmlNode attr = node.SelectSingleNode("@" + sAttrName);
			if (attr != null && sAttrName != null)
				CreateXmlAttribute(doc, sAttrName, attr.InnerText, morphNode);
		}

		private string CreateWordGrammarDebuggerFileName()
		{
			string sDepthLevel = m_XmlHtmlStack.Count.ToString();
			return m_ksWordGrammarDebugger + sDepthLevel;
		}
		private string CreateIconPath()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("file:///");
			sb.Append(TransformPath.Replace(@"\", "/"));
			sb.Append("/");
			return sb.ToString();
		}

		/// <summary>
		/// Path to transforms
		/// </summary>
		private string TransformPath
		{
			get { return DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer\Configuration\Words\Analyses\TraceParse"); }
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
					IMoForm form = MoForm.CreateFromDBObject(m_cache, hvo);
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
						IMoMorphSynAnalysis msa = MoMorphSynAnalysis.CreateFromDBObject(m_cache, hvoMsa);
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
						case MoStemAllomorph.kclsidMoStemAllomorph:
							ConvertStemName(doc, node, form, tempNode);
							break;
						case MoAffixAllomorph.kclsidMoAffixAllomorph:
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
			IFsFeatStruc fsFeatStruc = (IFsFeatStruc)CmObject.CreateFromDBObject(m_cache, fsHvo);
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
						MoStemName sn = (MoStemName)MoStemName.CreateFromDBObject(m_cache, Convert.ToInt32(sId));
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
			MoStemAllomorph sallo = form as MoStemAllomorph;
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
		protected void ConvertAffixAlloFeats(XmlDocument doc, XmlNode node, IMoForm form, XmlNode tempNode)
		{
			MoAffixAllomorph sallo = form as MoAffixAllomorph;
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

		protected void CreateMsaXmlElement(XmlNode node, XmlDocument doc, XmlNode morphNode, string sHvo)
		{
			XmlNode attr;
			// morphname contains the hvo of the msa
			attr = node.SelectSingleNode(sHvo);
			if (attr != null)
			{
				ICmObject obj = CmObject.CreateFromDBObject(m_cache, Convert.ToInt32(attr.Value));
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
				}
			}
		}
		protected void CreateUnclassifedMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoUnclassifiedAffixMsa unclassMsa)
		{
			XmlNode unclassMsaNode = CreateXmlElement(doc, "unclassMsa", morphNode);
			CreatePOSXmlAttribute(doc, unclassMsaNode, unclassMsa.PartOfSpeechRA, "fromCat");
		}
		protected void CreateDerivMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoDerivAffMsa derivMsa)
		{
			XmlNode derivMsaNode = CreateXmlElement(doc, "derivMsa", morphNode);
			CreatePOSXmlAttribute(doc, derivMsaNode, derivMsa.FromPartOfSpeechRA, "fromCat");
			CreatePOSXmlAttribute(doc, derivMsaNode, derivMsa.ToPartOfSpeechRA, "toCat");
			CreateInflectionClassXmlAttribute(doc, derivMsaNode, derivMsa.FromInflectionClassRA, "fromInflClass");
			CreateInflectionClassXmlAttribute(doc, derivMsaNode, derivMsa.ToInflectionClassRA, "toInflClass");
			CreateRequiresInflectionXmlAttribute(doc, derivMsa.ToPartOfSpeechRAHvo, derivMsaNode);
			CreateFeatureStructureNodes(doc, derivMsaNode, derivMsa.FromMsFeaturesOA, derivMsa.Hvo, "fromFS");
			CreateFeatureStructureNodes(doc, derivMsaNode, derivMsa.ToMsFeaturesOA, derivMsa.Hvo, "toFS");
			CreateProductivityRestrictionNodes(doc, derivMsaNode, derivMsa.FromProdRestrictRC, "fromProductivityRestriction");
			CreateProductivityRestrictionNodes(doc, derivMsaNode, derivMsa.ToProdRestrictRC, "toProductivityRestriction");
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
					if (pos.DefaultInflectionClassRAHvo != 0)
						inflClassHvo = pos.DefaultInflectionClassRAHvo;
					else
					{
						int clsid = m_cache.GetClassOfObject(pos.OwnerHVO);
						if (clsid == PartOfSpeech.kClassId)
							pos = PartOfSpeech.CreateFromDBObject(m_cache, pos.OwnerHVO);
						else
							pos = null;
					}
				}
				if (inflClassHvo != 0)
					inflClass = MoInflClass.CreateFromDBObject(m_cache, inflClassHvo);
			}
			CreateInflectionClassXmlAttribute(doc, stemMsaNode, inflClass, "inflClass");
			CreateRequiresInflectionXmlAttribute(doc, stemMsa.PartOfSpeechRAHvo, stemMsaNode);
			CreateFeatureStructureNodes(doc, stemMsaNode, stemMsa.MsFeaturesOA, stemMsa.Hvo);
			CreateProductivityRestrictionNodes(doc, stemMsaNode, stemMsa.ProdRestrictRC, "productivityRestriction");
			CreateFromPOSNodes(doc, stemMsaNode, stemMsa.FromPartsOfSpeechRC, "fromPartsOfSpeech");
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

		protected void CreateInflMsaXmlElement(XmlDocument doc, XmlNode morphNode, IMoInflAffMsa inflMsa)
		{
			XmlNode inflMsaNode = CreateXmlElement(doc, "inflMsa", morphNode);
			CreatePOSXmlAttribute(doc, inflMsaNode, inflMsa.PartOfSpeechRA, "cat");
			// handle any slot
			HandleSlotInfoForInflectionalMsa(inflMsa, doc, inflMsaNode, morphNode);
			CreateFeatureStructureNodes(doc, inflMsaNode, inflMsa.InflFeatsOA, inflMsa.Hvo);
			CreateProductivityRestrictionNodes(doc, inflMsaNode, inflMsa.FromProdRestrictRC, "fromProductivityRestriction");
		}

		protected void CreateInflectionClasses(XmlDocument doc, XmlNode morphNode)
		{
			string sAlloId = XmlUtils.GetOptionalAttributeValue(morphNode, "alloid");
			if (sAlloId == null)
				return;
			int hvoAllomorph = Convert.ToInt32(sAlloId);
			IMoAffixAllomorph allo = MoAffixAllomorph.CreateFromDBObject(m_cache, hvoAllomorph);
			if (allo == null)
				return;
			foreach (IMoInflClass ic in allo.InflectionClassesRC)
			{
				XmlNode icNode = CreateXmlElement(doc, "inflectionClass", morphNode);
				CreateXmlAttribute(doc, "id", ic.Hvo.ToString(), icNode);
				CreateXmlAttribute(doc, "abbr", ic.Abbreviation.BestAnalysisAlternative.Text, icNode);
			}
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
						slotHvo = inflMsa.SlotsRC.HvoArray[0];
					else
						slotHvo = inflMsa.SlotsRC.HvoArray[1];
				}
				else
					slotHvo = inflMsa.SlotsRC.HvoArray[0];
			}
			CreateXmlAttribute(doc, "slot", slotHvo.ToString(), inflMsaNode);
			string sSlotOptional = "false";
			string sSlotAbbr = "??";
			if (slotHvo > 0)
			{
				MoInflAffixSlot slot = (MoInflAffixSlot)CmObject.CreateFromDBObject(this.m_cache, slotHvo);
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

		protected void CreateFromPOSNodes(XmlDocument doc, XmlNode msaNode, FdoReferenceCollection<IPartOfSpeech> fromPOSes, string sElementName)
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
		protected void CreateProductivityRestrictionNodes(XmlDocument doc, XmlNode msaNode, FdoReferenceCollection<ICmPossibility> prodRests, string sElementName)
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
		protected void CreateRequiresInflectionXmlAttribute(XmlDocument doc, int posHvo, XmlNode msaNode)
		{
			string sPlusMinus;
			if (RequiresInflection(posHvo))
				sPlusMinus = "+";
			else
				sPlusMinus = "-";
			CreateXmlAttribute(doc, "requiresInfl", sPlusMinus, msaNode);
		}

		/// <summary>
		/// Determine if a PartOfSpeech requires inflection.
		/// If it or any of its parent POSes have a template, it requires inflection.
		/// </summary>
		/// <param name="posHvo">hvo of the Part of Speech</param>
		/// <returns>true if it does, false otherwise</returns>
		protected bool RequiresInflection(int posHvo)
		{
			bool fResult = false;  // be pessimistic
			if (posHvo > 0)
			{
				IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(m_cache, posHvo);
				fResult = pos.RequiresInflection();
			}
			return fResult;
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


	}
}
