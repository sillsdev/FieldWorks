// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// THIS NEEDS TO BE REFACTORED!!
//
// File: XAmpleTrace.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implementation of:
//		XAmpleTrace - Deal with results of an XAmple trace
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Xml;

using SIL.FieldWorks.FDO;
using XCore;
using System.Xml.XPath;


namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for XAmpleTrace.
	/// </summary>
	public class XAmpleTrace : ParserTrace
	{

		/// <summary>
		/// Temp File names
		/// </summary>
		const string m_ksXAmpleParse = "XAmpleParse";
		const string m_ksXAmpleTrace = "XAmpleTrace";

		/// <summary>
		/// For testing
		/// </summary>
		public XAmpleTrace()
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public XAmpleTrace(Mediator mediator) : base(mediator)
		{
			m_sParse = m_ksXAmpleParse;
			m_sTrace = m_ksXAmpleTrace;
			m_sFormatParse = "FormatXAmpleParse.xsl";
			m_sFormatTrace = "FormatXAmpleTrace.xsl";

		}
		/// <summary>
		/// Initialize what is needed to perform the word grammar debugging and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the node to use</param>
		/// <param name="sForm">the wordform being tried</param>
		/// <returns>temporary html file showing the results of the first step</returns>
		public override string SetUpWordGrammarDebuggerPage(string sNodeId, string sForm, string sLastURL)
		{
			m_wordGrammarDebugger = new XAmpleWordGrammarDebugger(m_mediator, m_parseResult);
			return m_wordGrammarDebugger.SetUpWordGrammarDebuggerPage(sNodeId, sForm, sLastURL);
		}

		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		/// <param name="result">XML string of the XAmple trace output</param>
		/// <returns>URL of the resulting HTML page</returns>
		public override string CreateResultPage(string result)
		{
			string sAdjusted;
			bool fIsTrace;
			AdjustResultContent(result, out sAdjusted, out fIsTrace);

			m_parseResult = new XmlDocument();
			m_parseResult.LoadXml(ConvertHvosToStrings(sAdjusted, fIsTrace));

			AddMsaNodes(fIsTrace);

			string sInput = CreateTempFile(m_ksXAmpleTrace, "xml");
			m_parseResult.Save(sInput);
			XPathDocument xpath = new XPathDocument(sInput);

			TransformKind kind = (fIsTrace ? TransformKind.kcptTrace : TransformKind.kcptParse);
			string sOutput = TransformToHtml(xpath, kind);
			return sOutput;
		}

		protected override void CreateMorphNodes(XmlDocument doc, XmlNode seqNode, string sNodeId)
		{
			string s = "//failure[@id=\"" + sNodeId + "\"]/ancestor::parseNode[morph][1]/morph";
			XmlNode node = m_parseResult.SelectSingleNode(s);
			if (node != null)
			{
				CreateMorphNode(doc, seqNode, node);
			}
		}
		private void CreateMorphNode(XmlDocument doc, XmlNode seqNode, XmlNode node)
		{
			// get the <morph> element closest up the chain to node
			XmlNode morph = node.SelectSingleNode("../ancestor::parseNode[morph][1]/morph");
			if (morph != null)
			{
				CreateMorphNode(doc, seqNode, morph);
			}
			CreateMorphXmlElement(doc, seqNode, node);
		}


#if false // CS 169
		private void CreateMorphGenericXmlElement(XmlNode node, XmlNode morphNode, string sName)
		{
			XmlNode nameNode = node.SelectSingleNode(sName);
			if (nameNode != null)
				morphNode.InnerXml = "<" + sName +">" + nameNode.InnerXml + "</" + sName + ">";
		}
#endif
		protected override string ConvertHvosToStrings(string sAdjusted, bool fIsTrace)
		{
			// When we  switched to VS 2005, the result from XAmple had a final trailing null character.
			// I'm not sure why (Andy).  Remove it.
			string sNoFinalNull = RemoveAnyFinalNull(sAdjusted);
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(sNoFinalNull);
			if (fIsTrace)
			{
				ConvertMorphs(doc, "//morph", true);
				ConvertFailures(doc);
			}
			else
				ConvertMorphs(doc, "//Morph", false);
			return doc.InnerXml;
		}

		private void AdjustResultContent(string sXml, out string sAdjusted, out bool fIsTrace)
		{


			if (sXml.Contains("<AmpleTrace"))
			{
				string sSkipDoctype = sXml.Substring(47);
				sAdjusted = AdjustEntities(sSkipDoctype);
				fIsTrace = true;
			}
			else
			{
				// N.B. following is actually XAmple-specific
				string sFullRedup = sXml.Replace("<...>", "[...]");
				sAdjusted = sFullRedup.Replace("DbRef=DB_REF_HERE", "DbRef=\"DB_REF_HERE\"");
				fIsTrace = false;
			}
		}

		private string RemoveAnyFinalNull(string sInput)
		{
			string sResult;
			int i = sInput.LastIndexOf('\x0');
			if (i >= 0)
				sResult = sInput.Substring(0, i);
			else
				sResult = sInput;
			return sResult;
		}
		private string AdjustEntities(string sInput)
		{
			string sResult = sInput.Replace("&rsqb;", "]"); // Created by XAmple
			return sResult;
		}


		private void ConvertFailures(XmlDocument doc)
		{
			ConvertSECFailures(doc, false);
			ConvertInfixEnvironmentFailures(doc, false);
			ConvertANCCFailures(doc, false);
			ConvertMCCFailures(doc, false);
			ConvertWordGrammarFailures(doc, false);
		}

		public void ConvertSECFailures(XmlDocument doc, bool fTesting)
		{
			const string ksSEC_ST = "SEC_ST";
			ConvertNaturalClasses(ksSEC_ST, doc, fTesting);
			ConvertEntities(ksSEC_ST, doc);
		}

		private void ConvertEntities(string sTestName, XmlDocument doc)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("//failure[contains(@test,'");
			sb.Append(sTestName);
			sb.Append("') and contains(@test,'&')]");
			XmlNodeList nl = doc.SelectNodes(sb.ToString());
			if (nl != null)
			{
				foreach (XmlNode node in nl)
				{
					XmlNode test = node.Attributes.GetNamedItem("test");
					string s = test.InnerText;
					test.InnerText = CreateEntities(s);
				}
			}
		}

		private void ConvertNaturalClasses(string sTestName, XmlDocument doc, bool fTesting)
		{
			StringBuilder sbXPath = new StringBuilder();
			sbXPath.Append("//failure[contains(@test,'");
			sbXPath.Append(sTestName);
			sbXPath.Append("') and contains(@test,'[')]");
			XmlNodeList nl = doc.SelectNodes(sbXPath.ToString());
			if (nl != null)
			{
				m_iTestingCount = 0;

				foreach (XmlNode node in nl)
				{
					XmlNode test = node.Attributes.GetNamedItem("test");
					string s = test.InnerText;
					int i = test.InnerText.IndexOf('/');
					string s0 = s.Substring(i);
					char[] brackets = {'[', ']'};
					string[] sa = s0.Split(brackets); // split into hunks using brackets
					StringBuilder sb = new StringBuilder();
					foreach (string str in sa)  // for each hunk
					{
						if (str.IndexOfAny(m_digits) >= 0)
						{  // assume it is an hvo
							sb.Append("[");
							if (fTesting)
							{
								if (m_iTestingCount > 5)
									m_iTestingCount = 0;
								sb.Append(m_saTesting[m_iTestingCount++]);
							}
							else
							{
								string sHvo = str;
								int hvo = Convert.ToInt32(sHvo);
								ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
								IPhNCSegments nc = obj as IPhNCSegments;
								string sName;
								if (nc != null)
									sName = nc.Name.BestAnalysisAlternative.Text;
								else
								{
									sName = ParserUIStrings.ksUnknownNaturalClass; // in case the user continues...
									throw new ApplicationException(sName);
								}
								sb.Append(sName);
							}
							sb.Append("]");
						}
						else
							sb.Append(str);
					}
					test.InnerText = s.Substring(0,i) + sb.ToString();
				}
			}
		}
		public void ConvertInfixEnvironmentFailures(XmlDocument doc, bool fTesting)
		{
			const string ksInfixEnvironment = "InfixEnvironment";
			ConvertNaturalClasses(ksInfixEnvironment, doc, fTesting);
			ConvertEntities(ksInfixEnvironment, doc);
		}

		public void ConvertMCCFailures(XmlDocument doc, bool fTesting)
		{
			MCCTraceTest mcc = new MCCTraceTest();
			ConvertAdHocFailures(doc, fTesting, mcc);
		}

		public void ConvertANCCFailures(XmlDocument doc, bool fTesting)
		{
			ANCCTraceTest ancc = new ANCCTraceTest();
			ConvertAdHocFailures(doc, fTesting, ancc);
		}

		private void ConvertAdHocFailures(XmlDocument doc, bool fTesting, AdhocTraceTest tt)
		{
			string sXPath = "//failure[contains(@test,'" + tt.Name + "') and not(contains(@test,'ExcpFeat')) and not(contains(@test,'StemName'))]";
			XmlNodeList nl = doc.SelectNodes(sXPath);
			if (nl != null)
			{
				m_iTestingCount = 0;
				foreach (XmlNode node in nl)
				{
					XmlNode test = node.Attributes.GetNamedItem("test");
					string s = test.InnerText;
					int iStartingPos = s.IndexOf("::") + 2; // skip to the double colon portion
					string[] sa = s.Substring(iStartingPos).Split(m_space);

					StringBuilder sb = new StringBuilder();
					sb.Append(tt.Name);
					sb.Append(":");
					foreach (string str in sa)
					{
						sb.Append(" ");
						if (str.IndexOfAny(m_digits) >= 0)
						{
							if (fTesting)
							{
								if (m_iTestingCount > 5)
									m_iTestingCount = 0;
								sb.Append(m_saTesting[m_iTestingCount++]);
							}
							else
							{
								string sHvo = str;
								int hvo = Convert.ToInt32(sHvo);
								sb.Append(tt.GetHvoRepresentation(m_cache, hvo));
								// get msa PhNCSegments nc = m_cache.ServiceLocator.GetInstance<IPhNCSegmentsRepository>().GetObject(hvo);
								// string name = nc.Name.BestAnalysisAlternative.Text;
							}
						}
						else
							sb.Append(str);
					}
					test.InnerText = sb.ToString();
				}
			}
		}

		private void ConvertWordGrammarFailures(XmlDocument doc, bool fTesting)
		{
			string sXPath = "//failure[contains(@test,'PC-PATR')]";
			XmlNodeList nl = doc.SelectNodes(sXPath);
			if (nl != null)
			{
				int iCount = 1;
				foreach (XmlNode node in nl)
				{
					CreateXmlAttribute(doc, "id", iCount.ToString(), node);
					iCount++;
				}
			}
		}


	}
	public abstract class AdhocTraceTest
	{
		protected string m_Name;

		public AdhocTraceTest()
		{
		}

		public virtual string GetHvoRepresentation(FdoCache cache, int hvo)
		{
			return "";
		}

		public string Name
		{
			get
			{
				return m_Name;
			}
		}
	}
	public class ANCCTraceTest : AdhocTraceTest
	{
		public ANCCTraceTest()
		{
			m_Name = "ANCC_FT";
		}

		public override string GetHvoRepresentation(FdoCache cache, int hvo)
		{
			ICmObject obj = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			IMoForm form = obj as IMoForm;
			string sResult;
			if (form != null)
				sResult = form.ShortName;
			else
			{
				sResult = ParserUIStrings.ksUnknownAllomorph; // in case the user continues...
				throw new ApplicationException(sResult);
			}
			return sResult;
		}
	}
	public class MCCTraceTest : AdhocTraceTest
	{
		public MCCTraceTest()
		{
			m_Name = "MCC_FT";
		}

		public override string GetHvoRepresentation(FdoCache cache, int hvo)
		{
			var obj = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			IMoMorphSynAnalysis msa = obj as IMoMorphSynAnalysis;
			string sResult;
			if (msa != null)
				sResult = msa.LongName;
			else
			{
				sResult = ParserUIStrings.ksUnknownMorpheme; // in case the user continues...
				throw new ApplicationException(sResult);
			}
			return sResult;
		}
	}
	public class WordGrammarStepPair
	{
		protected string m_sXmlFile;
		protected string m_sHtmlFile;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sXmlFile">Xml file</param>
		/// <param name="sHtmlFile">Html file</param>
		public WordGrammarStepPair(string sXmlFile, string sHtmlFile)
		{
			m_sXmlFile = sXmlFile;
			m_sHtmlFile = sHtmlFile;
		}
		/// <summary>
		/// Gete/set XmlFile
		/// </summary>
		public string XmlFile
		{
			get
			{
				return m_sXmlFile;
			}
			set
			{
				m_sXmlFile = value;
			}
		}
		/// <summary>
		/// Gete/set HtmlFile
		/// </summary>
		public string HtmlFile
		{
			get
			{
				return m_sHtmlFile;
			}
			set
			{
				m_sHtmlFile = value;
			}
		}

	}

}
///
/// Note for Andy
///
#if Later
using mshtml;

IHTMLDocument2 doc;
object boxDoc = m_browser.Document;
doc = (IHTMLDocument2)boxDoc;
string sHtml = doc.body.innerHTML;
#endif
