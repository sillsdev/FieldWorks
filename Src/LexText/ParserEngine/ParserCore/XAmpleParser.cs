using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

			string results = m_xample.ParseWord(word);
			results = results.Replace("DB_REF_HERE", "'0'");
			results = results.Replace("<...>", "[...]");
			var wordformElem = XElement.Parse(results);
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

			return m_xample.ParseWord(word);
		}

		public string TraceWordXml(string word, string selectTraceMorphs)
		{
			CheckDisposed();

			return m_xample.TraceWord(word, selectTraceMorphs);
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
