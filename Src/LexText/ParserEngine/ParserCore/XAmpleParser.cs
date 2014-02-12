using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using XAmpleManagedWrapper;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class XAmpleParser : FwDisposableBase, IParser
	{
		private static readonly char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		private XAmpleWrapper m_xample;
		private readonly string m_dataDir;
		private readonly FdoCache m_cache;
		private M3ParserModelRetriever m_retriever;

		public XAmpleParser(FdoCache cache, string dataDir)
		{
			m_cache = cache;
			m_xample = new XAmpleWrapper();
			m_xample.Init();
			m_dataDir = dataDir;
			m_retriever = new M3ParserModelRetriever(m_cache);
		}

		public bool IsUpToDate()
		{
			return m_retriever.Loaded;
		}

		public void Update()
		{
			CheckDisposed();

			if (!m_retriever.RetrieveModel())
				return;

			XmlDocument fxtResult = m_retriever.ModelDom;
			XmlDocument gafawsFxtResult = m_retriever.TemplateDom;
			string projectName = ParserHelper.ConvertNameToUseAnsiCharacters(m_cache.ProjectId.Name);

			var transformer = new M3ToXAmpleTransformer(projectName, m_dataDir);
			// PrepareTemplatesForXAmpleFiles adds orderclass elements to MoInflAffixSlot elements
			transformer.PrepareTemplatesForXAmpleFiles(ref fxtResult, gafawsFxtResult);

			transformer.MakeAmpleFiles(fxtResult);

			int maxAnalCount = 20;
			XmlNode maxAnalCountNode = fxtResult.SelectSingleNode("/M3Dump/ParserParameters/XAmple/MaxAnalysesToReturn");
			if (maxAnalCountNode != null)
			{
				maxAnalCount = Convert.ToInt16(maxAnalCountNode.FirstChild.Value);
				if (maxAnalCount < 1)
					maxAnalCount = -1;
			}

			m_xample.SetParameter("MaxAnalysesToReturn", maxAnalCount.ToString(CultureInfo.InvariantCulture));

			string tempPath = Path.GetTempPath();
			m_xample.LoadFiles(m_dataDir + @"/Configuration/Grammar", tempPath, projectName);
		}

		public void Reset()
		{
			CheckDisposed();

			m_retriever.Reset();
		}

		public ParseResult ParseWord(string word)
		{
			CheckDisposed();

			var results = new StringBuilder(m_xample.ParseWord(word));
			results = results.Replace("DB_REF_HERE", "'0'");
			results = results.Replace("<...>", "[...]");
			var wordformElem = XElement.Parse(results.ToString());
			string errorMessage = null;
			var exceptionElem = wordformElem.Element("Exception");
			if (exceptionElem != null)
			{
				var totalAnalysesValue = (string) exceptionElem.Attribute("totalAnalyses");
				switch ((string) exceptionElem.Attribute("code"))
				{
					case "ReachedMaxAnalyses":
						errorMessage = String.Format(ParserCoreStrings.ksReachedMaxAnalysesAllowed,
							totalAnalysesValue);
						break;
					case "ReachedMaxBufferSize":
						errorMessage = String.Format(ParserCoreStrings.ksReachedMaxInternalBufferSize,
							totalAnalysesValue);
						break;
				}
			}
			else
			{
				errorMessage = (string) wordformElem.Element("Error");
			}

			ParseResult result;
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				var analyses = new List<ParseAnalysis>();
				foreach (XElement analysisElem in wordformElem.Descendants("WfiAnalysis"))
				{
					var morphs = new List<ParseMorph>();
					bool skip = false;
					foreach (XElement morphElem in analysisElem.Descendants("Morph"))
					{
						ParseMorph morph;
						if (!ParserHelper.TryCreateParseMorph(m_cache, morphElem, out morph))
						{
							skip = true;
							break;
						}
						if (morph != null)
							morphs.Add(morph);
					}

					if (!skip && morphs.Count > 0)
						analyses.Add(new ParseAnalysis(morphs));
				}
				result = new ParseResult(analyses, errorMessage);
			}

			return result;
		}

		public string ParseWordXml(string word)
		{
			CheckDisposed();

			var sb = new StringBuilder(m_xample.ParseWord(word));
			sb.Replace("<...>", "[...]");
			sb.Replace("DB_REF_HERE", "'0'");
			while (sb[sb.Length - 1] == '\x0')
				sb.Remove(sb.Length - 1, 1);

			var doc = new XmlDocument();
			doc.LoadXml(sb.ToString());
			ParserXmlGenerator.ConvertMorphs(doc, "//Morph", false, m_cache);

			XmlNodeList nl = doc.SelectNodes("//Morph/MSI");
			if (nl != null)
			{
				foreach (XmlNode node in nl)
					ParserXmlGenerator.CreateMsaXmlElement(node, doc, node, "@DbRef", m_cache);
			}

			return doc.OuterXml;
		}

		public string TraceWordXml(string word, string selectTraceMorphs)
		{
			CheckDisposed();

			var sb = new StringBuilder(m_xample.TraceWord(word, selectTraceMorphs));
			sb.Remove(0, 47);
			sb.Replace("&rsqb;", "]");
			while (sb[sb.Length - 1] == '\x0')
				sb.Remove(sb.Length - 1, 1);

			var doc = new XmlDocument();
			doc.LoadXml(sb.ToString());
			ParserXmlGenerator.ConvertMorphs(doc, "//morph", true, m_cache);
			ConvertFailures(doc);

			XmlNodeList nl = doc.SelectNodes("//morph[@morphname]");
			if (nl != null)
			{
				foreach (XmlNode node in nl)
					ParserXmlGenerator.CreateMsaXmlElement(node, doc, node, "@morphname", m_cache);
			}

			return doc.OuterXml;
		}

		private void ConvertFailures(XmlDocument doc)
		{
			ConvertNaturalClasses(doc, "SEC_ST", GetNCRepresentation);
			ConvertNaturalClasses(doc, "InfixEnvironment", GetNCRepresentation);
			ConvertAdHocFailures(doc, "ANCC_FT", GetFormRepresentation);
			ConvertAdHocFailures(doc, "MCC_FT", GetMsaRepresentation);
			ConvertWordGrammarFailures(doc);
		}

		private string GetFormRepresentation(int hvo)
		{
			ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			var form = obj as IMoForm;
			if (form != null)
				return form.ShortName;

			throw new ApplicationException(ParserCoreStrings.ksUnknownAllomorph);
		}

		private string GetMsaRepresentation(int hvo)
		{
			ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			var msa = obj as IMoMorphSynAnalysis;
			if (msa != null)
				return msa.LongName;

			throw new ApplicationException(ParserCoreStrings.ksUnknownMorpheme);
		}

		private string GetNCRepresentation(int hvo)
		{
			ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			var nc = obj as IPhNCSegments;
			if (nc != null)
				return nc.Name.BestAnalysisAlternative.Text;

			throw new ApplicationException(ParserCoreStrings.ksUnknownNaturalClass);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		internal static void ConvertNaturalClasses(XmlDocument doc, string sTestName, Func<int, string> repSelector)
		{
			var sbXPath = new StringBuilder();
			sbXPath.Append("//failure[contains(@test,'");
			sbXPath.Append(sTestName);
			sbXPath.Append("') and contains(@test,'[')]");
			XmlNodeList nl = doc.SelectNodes(sbXPath.ToString());
			if (nl != null)
			{
				int testingCount = 0;

				foreach (XmlNode node in nl)
				{
					XmlNode test = node.Attributes.GetNamedItem("test");
					string s = test.InnerText;
					int i = test.InnerText.IndexOf('/');
					string s0 = s.Substring(i);
					string[] sa = s0.Split('[', ']'); // split into hunks using brackets
					var sb = new StringBuilder();
					foreach (string str in sa)  // for each hunk
					{
						if (str.IndexOfAny(Digits) >= 0)
						{
							// assume it is an hvo
							sb.Append("[");
							string sHvo = str;
							int hvo = Convert.ToInt32(sHvo);
							sb.Append(repSelector(hvo));
							sb.Append("]");
						}
						else
						{
							sb.Append(str);
						}
					}
					test.InnerText = s.Substring(0, i) + sb;
				}
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		internal static void ConvertAdHocFailures(XmlDocument doc, string testName, Func<int, string> repSelector)
		{
			string sXPath = "//failure[contains(@test,'" + testName +
				"') and not(contains(@test,'ExcpFeat')) and not(contains(@test,'StemName')) and not(contains(@test,'IrregInflForm'))]";
			XmlNodeList nl = doc.SelectNodes(sXPath);
			if (nl != null)
			{
				int testingCount = 0;
				foreach (XmlNode node in nl)
				{
					XmlNode test = node.Attributes.GetNamedItem("test");
					string s = test.InnerText;
					int iStartingPos = s.IndexOf("::", StringComparison.Ordinal) + 2; // skip to the double colon portion
					string[] sa = s.Substring(iStartingPos).Split(' ');

					var sb = new StringBuilder();
					sb.Append(testName);
					sb.Append(":");
					foreach (string str in sa)
					{
						sb.Append(" ");
						if (str.IndexOfAny(Digits) >= 0)
						{
							string sHvo = str;
							int hvo = Convert.ToInt32(sHvo);
							sb.Append(repSelector(hvo));
						}
						else
						{
							sb.Append(str);
						}
					}
					test.InnerText = sb.ToString();
				}
			}
		}


		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void ConvertWordGrammarFailures(XmlDocument doc)
		{
			const string sXPath = "//failure[contains(@test,'PC-PATR')]";
			XmlNodeList nl = doc.SelectNodes(sXPath);
			if (nl != null)
			{
				int iCount = 1;
				foreach (XmlNode node in nl)
				{
					ParserXmlGenerator.CreateXmlAttribute(doc, "id", iCount.ToString(CultureInfo.InvariantCulture), node);
					iCount++;
				}
			}
		}

		protected override void DisposeManagedResources()
		{
			if (m_xample != null)
			{
				m_xample.Dispose();
				m_xample = null;
			}

			if (m_retriever != null)
			{
				m_retriever.Dispose();
				m_retriever = null;
			}
		}
	}
}
