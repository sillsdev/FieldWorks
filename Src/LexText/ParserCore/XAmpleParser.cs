// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
using SIL.FieldWorks.FDO.DomainServices;
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
		private ParserModelChangeListener m_changeListener;
		private readonly M3ToXAmpleTransformer m_transformer;
		private readonly string m_database;
		private bool m_forceUpdate;

		public XAmpleParser(FdoCache cache, string dataDir)
		{
			m_cache = cache;
			m_xample = new XAmpleWrapper();
			m_xample.Init();
			m_dataDir = dataDir;
			m_changeListener = new ParserModelChangeListener(m_cache);
			m_database = ConvertNameToUseAnsiCharacters(m_cache.ProjectId.Name);
			m_transformer = new M3ToXAmpleTransformer(m_database);
			m_forceUpdate = true;
		}

		/// <summary>
		/// Convert any characters in the name which are higher than 0x00FF to hex.
		/// Neither XAmple nor PC-PATR can read a file name containing letters above 0x00FF.
		/// </summary>
		/// <param name="originalName">The original name to be converted</param>
		/// <returns>Converted name</returns>
		internal static string ConvertNameToUseAnsiCharacters(string originalName)
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

		public bool IsUpToDate()
		{
			return !m_changeListener.ModelChanged;
		}

		public void Update()
		{
			CheckDisposed();

			if (m_changeListener.Reset() || m_forceUpdate)
			{
				XDocument model, template;
				// According to the fxt template files, GAFAWS is NFC, all others are NFD.
				using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
				{
					ILangProject lp = m_cache.LanguageProject;
					// 1. Export lexicon and/or grammar.
					model = M3ModelExportServices.ExportGrammarAndLexicon(lp);

					// 2. Export GAFAWS data.
					template = M3ModelExportServices.ExportGafaws(lp.PartsOfSpeechOA.PossibilitiesOS);
				}

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
				m_forceUpdate = false;
			}
		}

		public void Reset()
		{
			CheckDisposed();

			m_forceUpdate = true;
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
						if (!TryCreateParseMorph(m_cache, morphElem, out morph))
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

		private static bool TryCreateParseMorph(FdoCache cache, XElement morphElem, out ParseMorph morph)
		{
			XElement formElement = morphElem.Element("MoForm");
			Debug.Assert(formElement != null);
			var formHvo = (string) formElement.Attribute("DbRef");

			XElement msiElement = morphElem.Element("MSI");
			Debug.Assert(msiElement != null);
			var msaHvo = (string) msiElement.Attribute("DbRef");

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
			//       The LexEntry is a variant form for the first set of LexEntryRefs.
			// 4. <MSI DbRef="y"... and y is an hvo for a LexEntry followed by a period and an index digit.
			//       The LexEntry is a variant form and the (non-zero) index indicates
			//       which set of LexEntryRefs it is for.
			ICmObject objForm;
			if (!cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(int.Parse(formHvo), out objForm))
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
			Tuple<int, int> msaTuple = ProcessMsaHvo(msaHvo);
			ICmObject objMsa;
			if (!cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(msaTuple.Item1, out objMsa))
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
					ILexEntryRef lexEntryRef = msaAsLexEntry.EntryRefsOS[msaTuple.Item2];
					ILexSense sense = MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
					var inflType = lexEntryRef.VariantEntryTypesRS[0] as ILexEntryInflType;
					morph = new ParseMorph(form, sense.MorphoSyntaxAnalysisRA, inflType);
					return true;
				}
			}

			// if it is anything else, we ignore it
			morph = null;
			return true;
		}

		private static Tuple<int, int> ProcessMsaHvo(string msaHvo)
		{
			string[] msaHvoParts = msaHvo.Split('.');
			return Tuple.Create(int.Parse(msaHvoParts[0]), msaHvoParts.Length == 2 ? int.Parse(msaHvoParts[1]) : 0);
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

		public XDocument TraceWordXml(string word, IEnumerable<int> selectTraceMorphs)
		{
			CheckDisposed();

			var sb = new StringBuilder(m_xample.TraceWord(word, selectTraceMorphs == null ? null : string.Join(" ", selectTraceMorphs)));
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

			if (m_changeListener != null)
			{
				m_changeListener.Dispose();
				m_changeListener = null;
			}
		}
	}
}
