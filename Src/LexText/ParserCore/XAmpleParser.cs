using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
		private readonly M3ToXAmpleTransformer m_transformer;
		private readonly string m_database;

		public XAmpleParser(FdoCache cache, string dataDir)
		{
			m_cache = cache;
			m_xample = new XAmpleWrapper();
			m_xample.Init();
			m_dataDir = dataDir;
			m_retriever = new M3ParserModelRetriever(m_cache);
			m_database = ParserHelper.ConvertNameToUseAnsiCharacters(m_cache.ProjectId.Name);
			m_transformer = new M3ToXAmpleTransformer(dataDir, m_database);
		}

		public bool IsUpToDate()
		{
			return !m_retriever.Updated;
		}

		public void Update()
		{
			CheckDisposed();

			XDocument model, template;
			if (!m_retriever.RetrieveModel(out model, out template))
				return;

			// PrepareTemplatesForXAmpleFiles adds orderclass elements to MoInflAffixSlot elements
			m_transformer.PrepareTemplatesForXAmpleFiles(model, template);

			m_transformer.MakeAmpleFiles(model);

			int maxAnalCount = 20;
			XElement maxAnalCountElem = model.Elements("M3Dump").Elements("ParserParameters").Elements("XAmple").Elements("MaxAnalysesToReturn").FirstOrDefault();
			if (maxAnalCountElem != null)
			{
				maxAnalCount = (int) maxAnalCountElem;
				if (maxAnalCount < 1)
					maxAnalCount = -1;
			}

			m_xample.SetParameter("MaxAnalysesToReturn", maxAnalCount.ToString(CultureInfo.InvariantCulture));

			m_xample.LoadFiles(Path.Combine(m_dataDir, "Configuration", "Grammar"), Path.GetTempPath(), m_database);
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

		public XDocument ParseWordXml(string word)
		{
			CheckDisposed();

			var sb = new StringBuilder(m_xample.ParseWord(word));
			sb.Replace("DB_REF_HERE", "'0'");
			sb.Replace("<...>", "[...]");
			while (sb[sb.Length - 1] == '\x0')
				sb.Remove(sb.Length - 1, 1);

			XDocument doc = XDocument.Parse(sb.ToString());
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				foreach (XElement morphElem in doc.Descendants("Morph"))
				{
					var type = (string) morphElem.Attribute("type");
					var props = (string) morphElem.Element("props");

					XElement formElem = morphElem.Element("MoForm");
					Debug.Assert(formElem != null);
					var formID = (string) formElem.Attribute("DbRef");
					var wordType = (string) formElem.Attribute("wordType");

					XElement msaElem = morphElem.Element("MSI");
					Debug.Assert(msaElem != null);
					var msaID = (string) msaElem.Attribute("DbRef");

					using (XmlWriter writer = morphElem.CreateWriter())
						writer.WriteMorphInfoElements(m_cache, formID, msaID, wordType, props);

					using (XmlWriter writer = msaElem.CreateWriter())
						writer.WriteMsaElement(m_cache, formID, msaID, type, wordType);
				}
			}

			return doc;
		}

		public XDocument TraceWordXml(string word, string selectTraceMorphs)
		{
			CheckDisposed();

			var sb = new StringBuilder(m_xample.TraceWord(word, selectTraceMorphs));
			sb.Remove(0, 47);
			sb.Replace("&rsqb;", "]");
			while (sb[sb.Length - 1] == '\x0')
				sb.Remove(sb.Length - 1, 1);

			XDocument doc = XDocument.Parse(sb.ToString());
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				foreach (XElement morphElem in doc.Descendants("morph"))
				{
					var formID = (string) morphElem.Attribute("alloid");
					var msaID = (string) morphElem.Attribute("morphname");
					var type = (string) morphElem.Attribute("type");
					var props = (string) morphElem.Element("props");
					var wordType = (string) morphElem.Attribute("wordType");

					using (XmlWriter writer = morphElem.CreateWriter())
					{
						writer.WriteMorphInfoElements(m_cache, formID, msaID, wordType, props);
						writer.WriteMsaElement(m_cache, formID, msaID, type, wordType);
						writer.WriteInflClassesElement(m_cache, formID);
					}
				}
				ConvertFailures(doc, GetStrRep);
			}

			return doc;
		}

		internal static void ConvertFailures(XDocument doc, Func<int, int, string> strRepSelector)
		{
			int wordGrammarFailureCount = 1;
			foreach (XElement failureElem in doc.Descendants("failure"))
			{
				var test = (string) failureElem.Attribute("test");

				if ((test.StartsWith("SEC_ST") || test.StartsWith("InfixEnvironment")) && test.Contains("["))
				{
					int i = test.IndexOf('/');
					string[] sa = test.Substring(i).Split('[', ']'); // split into hunks using brackets
					var sb = new StringBuilder();
					foreach (string str in sa)  // for each hunk
					{
						if (str.IndexOfAny(Digits) >= 0)
						{
							// assume it is an hvo
							sb.Append("[");
							string sHvo = str;
							int hvo = Convert.ToInt32(sHvo);
							sb.Append(strRepSelector(PhNaturalClassTags.kClassId, hvo));
							sb.Append("]");
						}
						else
						{
							sb.Append(str);
						}
					}
					failureElem.SetAttributeValue("test", test.Substring(0, i) + sb);
				}
				else if ((test.StartsWith("ANCC_FT") || test.StartsWith("MCC_FT")) && !test.Contains("ExcpFeat") && !test.Contains("StemName")
					&& !test.Contains("IrregInflForm"))
				{
					int index = test.IndexOf(":", StringComparison.Ordinal);
					string testName = test.Substring(0, index);

					int iStartingPos = test.IndexOf("::", StringComparison.Ordinal) + 2; // skip to the double colon portion
					string[] sa = test.Substring(iStartingPos).Split(' ');

					var sb = new StringBuilder();
					sb.Append(testName);
					sb.Append(":");
					foreach (string str in sa)
					{
						sb.Append(" ");
						if (str.IndexOfAny(Digits) >= 0)
						{
							int hvo = Convert.ToInt32(str);
							sb.Append(strRepSelector(testName == "ANCC_FT" ? MoFormTags.kClassId : MoMorphSynAnalysisTags.kClassId, hvo));
						}
						else
						{
							sb.Append(str);
						}
					}
					failureElem.SetAttributeValue("test", sb.ToString());
				}
				else if (test.StartsWith("PC-PATR"))
				{
					failureElem.Add(new XAttribute("id", wordGrammarFailureCount));
					wordGrammarFailureCount++;
				}
			}
		}

		private string GetStrRep(int classID, int hvo)
		{
			ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			switch (classID)
			{
				case MoFormTags.kClassId:
					var form = obj as IMoForm;
					if (form != null)
						return form.ShortName;
					throw new ApplicationException(ParserCoreStrings.ksUnknownAllomorph);

				case MoMorphSynAnalysisTags.kClassId:
					var msa = obj as IMoMorphSynAnalysis;
					if (msa != null)
						return msa.LongName;
					throw new ApplicationException(ParserCoreStrings.ksUnknownMorpheme);

				case PhNaturalClassTags.kClassId:
					var nc = obj as IPhNCSegments;
					if (nc != null)
						return nc.Name.BestAnalysisAlternative.Text;
					throw new ApplicationException(ParserCoreStrings.ksUnknownNaturalClass);
			}
			return null;
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
