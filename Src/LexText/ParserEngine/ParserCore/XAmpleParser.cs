using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using XAmpleManagedWrapper;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class XAmpleParser : FwDisposableBase, IParser
	{
		private XAmpleWrapper m_xample;
		private readonly string m_appInstallDir;
		private readonly FdoCache m_cache;
		private M3ParserModelRetriever m_retriever;

		public XAmpleParser(FdoCache cache, string appInstallDir)
		{
			m_cache = cache;
			m_xample = new XAmpleWrapper();
			m_xample.Init(appInstallDir);
			m_appInstallDir = appInstallDir;
			m_retriever = new M3ParserModelRetriever(m_cache);
		}

		public void Initialize()
		{
			CheckDisposed();

			if (!m_retriever.RetrieveModel())
				return;

			XmlDocument fxtResult = m_retriever.ModelDom;
			XmlDocument gafawsFxtResult = m_retriever.TemplateDom;
			string projectName = ConvertNameToUseAnsiCharacters(m_cache.ProjectId.Name);

			var transformer = new M3ToXAmpleTransformer(projectName, m_appInstallDir);
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
			m_xample.LoadFiles(m_appInstallDir + @"/Language Explorer/Configuration/Grammar", tempPath, projectName);
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
						if (!TryCreateParseMorph(morphElem, out morph))
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

		/// <summary>
		/// Creates a single ParseMorph object
		/// Handles special cases where the MoForm hvo and/or MSI hvos are
		/// not actual MoForm or MSA objects.
		/// </summary>
		/// <param name="morphElem">A Morph element returned by one of the automated parsers</param>
		/// <param name="morph">a new ParseMorph object or null if the morpheme should be skipped</param>
		/// <returns></returns>
		private bool TryCreateParseMorph(XElement morphElem, out ParseMorph morph)
		{
			// Normally, the hvo for MoForm is a MoForm and the hvo for MSI is an MSA
			// There are four exceptions, though, when an irregularly inflected form is involved:
			// 1. <MoForm DbRef="x"... and x is an hvo for a LexEntryInflType.
			//       This is one of the null allomorphs we create when building the
			//       input for the parser in order to still get the Word Grammar to have something in any
			//       required slots in affix templates.  The parser filer can ignore these.
			// 2. <MSI DbRef="y"... and y is an hvo for a LexEntryInflType.
			//       This is one of the null allomorphs we create when building the
			//       input for the parser in order to still get the Word Grammar to have something in any
			//       required slots in affix templates.  The parser filer can ignore these.
			// 3. <MSI DbRef="y"... and y is an hvo for a LexEntry.
			//       The LexEntry is an irregularly inflected form for the first set of LexEntryRefs.
			// 4. <MSI DbRef="y"... and y is an hvo for a LexEntry followed by a period and an index digit.
			//       The LexEntry is an irregularly inflected form and the (non-zero) index indicates
			//       which set of LexEntryRefs it is for.
			XElement formElement = morphElem.Element("MoForm");
			Debug.Assert(formElement != null);
			var hvoForm = (int) formElement.Attribute("DbRef");
			ICmObject objForm;
			if (!m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(hvoForm, out objForm))
			{
				morph = null;
				return false;
			}
			var form = objForm as IMoForm;
			if (form == null)
			{
				morph = null;
				return true;
			}

			// Irregulary inflected forms can have a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
			XElement msiElement = morphElem.Element("MSI");
			Debug.Assert(msiElement != null);
			var msaHvoStr = (string) msiElement.Attribute("DbRef");
			string[] msaHvoParts = msaHvoStr.Split('.');
			ICmObject objMsa;
			if (!m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(int.Parse(msaHvoParts[0]), out objMsa))
			{
				morph = null;
				return false;
			}
			var msa = objMsa as IMoMorphSynAnalysis;
			if (msa != null)
			{
				morph = new ParseMorph(form, msa);
				return true;
			}

			var msaAsLexEntry = objMsa as ILexEntry;
			if (msaAsLexEntry != null)
			{
				// is an irregularly inflected form
				// get the MoStemMsa of its variant
				if (msaAsLexEntry.EntryRefsOS.Count > 0)
				{
					int index = msaHvoParts.Length == 2 ? int.Parse(msaHvoParts[1]) : 0;
					ILexEntryRef lexEntryRef = msaAsLexEntry.EntryRefsOS[index];
					ILexSense sense = MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
					var inflType = (ILexEntryInflType) lexEntryRef.VariantEntryTypesRS[0];
					morph = new ParseMorph(form, sense.MorphoSyntaxAnalysisRA, inflType);
					return true;
				}
			}

			// if it is anything else, we ignore it
			morph = null;
			return true;
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

		/// <summary>
		/// Convert any characters in the name which are higher than 0x00FF to hex.
		/// Neither XAmple nor PC-PATR can read a file name containing letters above 0x00FF.
		/// </summary>
		/// <param name="originalName">The original name to be converted</param>
		/// <returns>Converted name</returns>
		private static string ConvertNameToUseAnsiCharacters(string originalName)
		{
			var sb = new StringBuilder();
			char[] letters = originalName.ToCharArray();
			foreach (var letter in letters)
			{
				int value = Convert.ToInt32(letter);
				if (value > 255)
				{
					string hex = value.ToString("X4");
					sb.Append(hex);
				}
				else
				{
					sb.Append(letter);
				}
			}
			return sb.ToString();
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
