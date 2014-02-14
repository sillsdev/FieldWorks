// Copyright (c) 2014-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using PatrParserWrapper;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Validation;
using SIL.HermitCrab;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class HCParser : FwDisposableBase, IParser
	{
		private readonly FdoCache m_cache;
		private PatrParser m_patr;
		private readonly Loader m_loader;
		private M3ParserModelRetriever m_retriever;
		private readonly string m_dataDir;
		private readonly string m_outputDirectory;
		// m_projectName here is only used to create temporary files for the parser to load.
		// We convert the name to use strictly ANSI characters so that the patr parsers (which is
		// a legacy C program) can read the file names.
		private readonly string m_projectName;

		public HCParser(FdoCache cache, string dataDir)
		{
			m_cache = cache;
			m_dataDir = dataDir;

			m_retriever = new M3ParserModelRetriever(m_cache);
			m_patr = new PatrParser
			{
				CommentChar = '|',
				CodePage = Encoding.UTF8.CodePage
			};
			m_loader = new XmlLoader
			{
				XmlResolver = new XmlFwResolver(dataDir),
				QuitOnError = false
			};

			m_outputDirectory = Path.GetTempPath();
			m_projectName = ParserHelper.ConvertNameToUseAnsiCharacters(cache.ProjectId.Name);
		}

		#region IParser implementation
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
			LoadParser(ref fxtResult);
		}

		public void Reset()
		{
			CheckDisposed();

			m_retriever.Reset();
		}

		public ParseResult ParseWord(string word)
		{
			CheckDisposed();

			if (!m_loader.IsLoaded)
				return null;

			ICollection<WordSynthesis> synthesisRecs;
			try
			{
				synthesisRecs = m_loader.CurrentMorpher.MorphAndLookupWord(word);
			}
			catch (MorphException me)
			{
				return new ParseResult(ProcessMorphException(me));
			}

			ParseResult result;
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				IEnumerable<PatrResult> patrResults = ProcessPatr(synthesisRecs, Path.Combine(m_outputDirectory, m_projectName + "patrlex.txt"), false);

				var analyses = new List<ParseAnalysis>();
				foreach (PatrResult patrResult in patrResults)
				{
					var morphs = new List<ParseMorph>();
					bool skip = false;
					foreach (PcPatrMorph pcPatrMorph in patrResult.Morphs)
					{
						ParseMorph morph;
						if (!ParserHelper.TryCreateParseMorph(m_cache, pcPatrMorph.formId, pcPatrMorph.msaId, out morph))
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
				result = new ParseResult(analyses);
			}

			return result;
		}

		public XDocument TraceWordXml(string form, string selectTraceMorphs)
		{
			CheckDisposed();

			return TraceWordXml(form, GetArrayFromString(selectTraceMorphs));
		}

		public XDocument ParseWordXml(string form)
		{
			CheckDisposed();

			return ParseToXml(form, false, null);
		}
		#endregion

		public XDocument TraceWordXml(string form, string[] selectTraceMorphs)
		{
			CheckDisposed();

			return ParseToXml(form, true, selectTraceMorphs);
		}

		protected override void DisposeManagedResources()
		{
			if (m_patr != null)
			{
				m_patr.Dispose();
				m_patr = null;
			}

			if (m_retriever != null)
			{
				m_retriever.Dispose();
				m_retriever = null;
			}
		}

		#region Load
		private void LoadParser(ref XmlDocument model)
		{
			string hcPath = HcInputPath;
			File.Delete(hcPath); // In case we don't produce one successfully, don't keep an old one.
			// Check for errors that will prevent the transformations working.
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				foreach (var affix in m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphRepository>().AllInstances())
				{
					string form = affix.Form.VernacularDefaultWritingSystem.Text;
					if (String.IsNullOrEmpty(form) || !form.Contains("["))
						continue;
					string environment = "/_" + form;
					// A form containing a reduplication expression should look like an environment
					var validator = new PhonEnvRecognizer(
						m_cache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(),
						m_cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());
					if (!validator.Recognize(environment))
					{
						m_loader.Reset(); // make sure nothing thinks it is in a useful state
						throw new InvalidReduplicationEnvironmentException(validator.ErrorMessage, form);
					}
				}
			}

			var transformer = new M3ToHCTransformer(m_projectName, m_dataDir);
			transformer.MakeHCFiles(ref model);

			m_patr.LoadGrammarFile(HcGrammarPath);

			LoadHCInfo(hcPath);

			XmlNode delReappsNode = model.SelectSingleNode("/M3Dump/ParserParameters/HC/DelReapps");
			if (delReappsNode != null)
				m_loader.CurrentMorpher.DelReapplications = Convert.ToInt32(delReappsNode.InnerText);
		}

		private void LoadHCInfo(string hcPath)
		{
			string loadErrorsFile = Path.Combine(m_outputDirectory, m_projectName + "HCLoadErrors.xml");
			using (XmlWriter writer = new XmlTextWriter(loadErrorsFile, null))
			{
				var loadOutput = new XmlOutput(writer);
				writer.WriteStartElement("LoadErrors");
				m_loader.Output = loadOutput;
				m_loader.Load(hcPath);
				writer.WriteEndElement();
				loadOutput.Close();
			}
		}

		private string HcInputPath
		{
			get { return Path.Combine(m_outputDirectory, m_projectName + "HCInput.xml"); }
		}

		private string HcGrammarPath
		{
			get { return Path.Combine(m_outputDirectory, m_projectName + "gram.txt"); }
		}
		#endregion

		private string[] GetArrayFromString(string selectTraceMorphs)
		{
			string[] selectTraceMorphIds = null;
			if (!String.IsNullOrEmpty(selectTraceMorphs))
			{
				selectTraceMorphIds = selectTraceMorphs.TrimEnd().Split(' ');
				int i = 0;
				using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
				{
					foreach (string sId in selectTraceMorphIds)
					{
						IMoMorphSynAnalysis msa = m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().GetObject(int.Parse(sId));
						if (msa.ClassID == MoStemMsaTags.kClassId)
							selectTraceMorphIds.SetValue("lex" + sId, i);
						else
							selectTraceMorphIds.SetValue("mrule" + sId, i);
						i++;
					}
				}
			}
			return selectTraceMorphIds;
		}

		private XDocument ParseToXml(string form, bool trace, string[] selectTraceMorphs)
		{
			if (!m_loader.IsLoaded)
				return null;

			var doc = new XDocument();
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				using (XmlWriter writer = doc.CreateWriter())
				{
					writer.WriteStartElement("Wordform");
					writer.WriteAttributeString("DbRef", Convert.ToString(0));
					writer.WriteAttributeString("Form", form);
					MorphAndLookupWord(writer, m_loader.CurrentMorpher, form, true, selectTraceMorphs, Path.Combine(m_outputDirectory, m_projectName + "patrlex.txt"), trace);
					WriteDataIssues(writer);
					writer.WriteEndElement();
				}
			}
			return doc;
		}

		/// <summary>
		/// Check integrity of phoneme-based natural classes (PhNCSegments)
		/// when there are phonological features
		/// </summary>
		public void WriteDataIssues(XmlWriter writer)
		{
			if (!m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.Any())
				return; // no phonological features so nothing to check

			writer.WriteStartElement("DataIssues");
			foreach (IPhNCSegments natClass in m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.OfType<IPhNCSegments>())
			{
				IFsFeatStruc fs = natClass.GetImpliedPhonologicalFeatures();
				var predictedPhonemes = new HashSet<IPhPhoneme>(natClass.GetPredictedPhonemes(fs));
				if (!predictedPhonemes.SetEquals(natClass.SegmentsRC))
				{
					writer.WriteStartElement("NatClassPhonemeMismatch");
					writer.WriteStartElement("ClassName");
					writer.WriteString(natClass.Name.BestAnalysisAlternative.Text);
					writer.WriteEndElement();
					writer.WriteStartElement("ClassAbbeviation");
					writer.WriteString(natClass.Abbreviation.BestAnalysisAlternative.Text);
					writer.WriteEndElement();
					writer.WriteStartElement("ImpliedPhonologicalFeatures");
					writer.WriteString(fs.LongName);
					writer.WriteEndElement();
					writer.WriteStartElement("PredictedPhonemes");
					writer.WriteString(string.Join(" ", predictedPhonemes.Select(p => p.Name.BestVernacularAlternative.Text)));
					writer.WriteEndElement();
					writer.WriteStartElement("ActualPhonemes");
					writer.WriteString(string.Join(" ", natClass.SegmentsRC.Select(p => p.Name.BestVernacularAlternative.Text)));
					writer.WriteEndElement();
					writer.WriteEndElement();
					writer.WriteEndElement();
				}
			}
			writer.WriteEndElement();
		}

		private IEnumerable<PatrResult> ProcessPatr(IEnumerable<WordSynthesis> synthesisRecs, string patrlexPath, bool trace)
		{
			IList<PatrResult> patrResults = new List<PatrResult>();
			bool passedPatr = false;
			foreach (WordSynthesis ws in synthesisRecs)
			{
				WordGrammarTrace wordGrammarTrace = null;
				List<PcPatrMorph> morphs = GetMorphs(ws);
				if (trace)
					wordGrammarTrace = new WordGrammarTrace(((uint)ws.GetHashCode()).ToString(CultureInfo.InvariantCulture), morphs, m_cache);
				if (morphs.Count == 1)
				{
					PcPatrMorph morph = morphs[0];
					string formid = morph.formId;
					IMoForm form = m_cache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(Int32.Parse(formid));
					var morphtype = form.MorphTypeRA;
					if (morphtype.IsBoundType)
					{
						if (wordGrammarTrace != null)
							wordGrammarTrace.Success = false; // this is not really true; what other options are there?
						continue;
					}
				}
				WritePcPatrLexiconFile(patrlexPath, morphs);
				m_patr.LoadLexiconFile(patrlexPath, 0);
				string sentence = BuildPcPatrInputSentence(morphs);
				try
				{
					if (m_patr.ParseString(sentence) != null)
					{
						passedPatr = true;
						if (wordGrammarTrace != null)
							wordGrammarTrace.Success = true;
					}
					else if (wordGrammarTrace != null)
					{
						wordGrammarTrace.Success = false;
					}
				}
				catch (Exception)
				{
				}
				patrResults.Add(new PatrResult { Morphs = morphs, WordGrammarTrace = wordGrammarTrace, PassedPatr = passedPatr });
			}
			return patrResults;
		}

		private List<PcPatrMorph> GetMorphs(WordSynthesis ws)
		{
			var ppMorphs = new Dictionary<string, PcPatrMorph>();
			var result = new List<PcPatrMorph>();
			foreach (Morph morph in ws.Morphs)
			{
				string[] formIds = morph.Allomorph.GetProperty("FormID").Split(' ');
				string[] wordTypes = morph.Allomorph.GetProperty("WordCategory").Split(' ');
				string form = ws.Stratum.CharacterDefinitionTable.ToString(morph.Shape, ModeType.SYNTHESIS, false);
				PcPatrMorph ppMorph;
				if (!ppMorphs.TryGetValue(morph.Allomorph.Morpheme.ID, out ppMorph))
				{
					ppMorph = new PcPatrMorph
					{
						formIndex = 0,
						formId = formIds[0],
						form = form,
						wordType = wordTypes[0]
					};
				}
				else if (formIds.Length == 1)
				{
					ppMorph.form += form;
					continue;
				}
				else
				{
					PcPatrMorph oldMorph = ppMorph;
					int wordTypeIndex = WordTypeIndex(oldMorph.formIndex + 1, wordTypes.Count());
					ppMorph = new PcPatrMorph
					{
						formIndex = oldMorph.formIndex + 1,
						formId = formIds[oldMorph.formIndex + 1],
						form = form,
						wordType = wordTypes[wordTypeIndex]
					};
				}

				ppMorph.msaId = morph.Allomorph.GetProperty("MsaID");
				ppMorph.featureDescriptors = morph.Allomorph.GetProperty("FeatureDescriptors");
				ppMorph.gloss = morph.Allomorph.Morpheme.Gloss.Description;
				ppMorphs[morph.Allomorph.Morpheme.ID] = ppMorph;

				string morphType = morph.Allomorph.GetProperty("MorphType");
				switch (morphType)
				{
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
						if (result.Count == 0)
							result.Add(ppMorph);
						else
							result.Insert(result.Count - 1, ppMorph);
						break;

					default:
						result.Add(ppMorph);
						break;
				}
			}
			return result;
		}

		private void WritePcPatrLexiconFile(string path, IEnumerable<PcPatrMorph> morphs)
		{
			using (var writer = new StreamWriter(path))
			{
				foreach (PcPatrMorph morph in morphs)
				{
					writer.WriteLine("\\w {0}", morph.Form);
					writer.WriteLine("\\c {0}", morph.wordType);
					writer.WriteLine("\\g {0}", morph.gloss);
					if (!String.IsNullOrEmpty(morph.featureDescriptors))
					{
						string lastFeatDesc = "";
						string combinedCfpFeatDescs = "";
						string[] featDescs = morph.featureDescriptors.Split(' ');
						if (featDescs.Any())
							writer.Write("\\f");
						foreach (string featDesc in featDescs)
						{
							if (featDesc.StartsWith("CFP"))
							{
								combinedCfpFeatDescs += featDesc;
								lastFeatDesc = featDesc;
								continue;
							}
							if (lastFeatDesc.StartsWith("CFP"))
								writer.Write(" {0}", combinedCfpFeatDescs);
							writer.Write(" {0}", featDesc);
						}
						if (lastFeatDesc.StartsWith("CFP"))
							writer.Write(" {0}", combinedCfpFeatDescs);
					}
					writer.WriteLine();
					writer.WriteLine();
				}
				writer.Close();
			}
		}

		private string BuildPcPatrInputSentence(IEnumerable<PcPatrMorph> morphs)
		{
			var sentence = new StringBuilder();
			bool firstItem = true;
			foreach (PcPatrMorph morph in morphs)
			{
				if (!firstItem)
					sentence.Append(" ");
				sentence.Append(morph.Form);
				firstItem = false;
			}
			return sentence.ToString();
		}

		private static int WordTypeIndex(int expectedindex, int wordTypesCount)
		{
			int wordTypeIndex = expectedindex;
			if (wordTypeIndex + 1 > wordTypesCount)
			{
				// something is wrong (perhaps a missing suffix slot for a circumfix)
				// we'll use the same as last time, hoping for a parse failure
				wordTypeIndex = wordTypeIndex - 1;
			}
			return wordTypeIndex;
		}

		private string ProcessMorphException(MorphException me)
		{
			string errorMessage;
			switch (me.ErrorType)
			{
				case MorphException.MorphErrorType.INVALID_SHAPE:
					var shape = (string)me.Data["shape"];
					var position = (int)me.Data["position"];
					var phonemesFoundSoFar = (string)me.Data["phonemesFoundSoFar"];
					string rest = shape.Substring(position);
					string restToUse = rest;
					LgGeneralCharCategory cc = m_cache.ServiceLocator.UnicodeCharProps.get_GeneralCategory(rest[0]);
					if (cc == LgGeneralCharCategory.kccMn)
					{
						// the first character is a diacritic, combining type of character
						// insert a space so it does not show on top of a single quote in the message string
						restToUse = " " + rest;
					}
					errorMessage = String.Format(ParserCoreStrings.ksHCInvalidWordform, shape, position + 1, restToUse, phonemesFoundSoFar);
					break;

				case MorphException.MorphErrorType.UNINSTANTIATED_FEATURE:
					var featId = me.Data["feature"] as string;
					var feat = me.Morpher.PhoneticFeatureSystem.GetFeature(featId);
					errorMessage = String.Format(ParserCoreStrings.ksHCUninstFeature, feat.Description);
					break;

				default:
					errorMessage = String.Format(ParserCoreStrings.ksHCDefaultErrorMsg, me.Message);
					break;
			}
			return errorMessage;
		}

		#region Write Xml
		private void MorphAndLookupWord(XmlWriter writer, Morpher morpher, string word, bool printTraceInputs, string[] selectTraceMorphs, string patrlexPath, bool doTrace)
		{
			try
			{
				ICollection<WordGrammarTrace> wordGrammarTraces = null;
				if (doTrace)
					wordGrammarTraces = new HashSet<WordGrammarTrace>();
				var traceManager = new FwXmlTraceManager(m_cache) { WriteInputs = printTraceInputs, TraceAll = doTrace };
				ICollection<WordSynthesis> synthesisRecs = morpher.MorphAndLookupWord(word, traceManager, selectTraceMorphs);

				IEnumerable<PatrResult> patrResults = ProcessPatr(synthesisRecs, patrlexPath, doTrace);
				foreach (PatrResult patrResult in patrResults)
				{
					if (patrResult.PassedPatr)
						BuildXmlOutput(writer, patrResult.Morphs);
					if (wordGrammarTraces != null)
						wordGrammarTraces.Add(patrResult.WordGrammarTrace);
				}

				if (doTrace)
				{
					WriteTrace(writer, traceManager);
					ConvertWordGrammarTraceToXml(writer, wordGrammarTraces);
				}
				traceManager.Reset();
			}
			catch (MorphException exc)
			{
				Write(writer, exc);
			}
		}

		private void BuildXmlOutput(XmlWriter writer, IEnumerable<PcPatrMorph> morphs)
		{
			writer.WriteStartElement("WfiAnalysis");
			writer.WriteStartElement("Morphs");

			foreach (PcPatrMorph morph in morphs)
			{
				writer.WriteStartElement("Morph");

				writer.WriteStartElement("MoForm");
				writer.WriteAttributeString("DbRef", morph.formId);
				writer.WriteAttributeString("Label", morph.form);
				writer.WriteAttributeString("wordType", morph.wordType);
				writer.WriteEndElement();

				writer.WriteStartElement("MSI");
				writer.WriteAttributeString("DbRef", morph.msaId);
				writer.WriteMsaElement(m_cache, morph.formId, morph.msaId, null, morph.wordType);
				writer.WriteEndElement();

				writer.WriteMorphInfoElements(m_cache, morph.formId, morph.msaId, morph.wordType, null);

				writer.WriteEndElement();
			}

			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		private void ConvertWordGrammarTraceToXml(XmlWriter writer, IEnumerable<WordGrammarTrace> wordGrammarTraces)
		{
			writer.WriteStartElement("WordGrammarTrace");
			foreach (WordGrammarTrace trace in wordGrammarTraces)
				trace.ToXml(writer);
			writer.WriteEndElement();
		}

		private void WriteTrace(XmlWriter writer, XmlTraceManager traceManager)
		{
			writer.WriteStartElement("Trace");
			foreach (XElement trace in traceManager.WordAnalysisTraces)
				trace.WriteTo(writer);
			writer.WriteEndElement();
		}

		private void Write(XmlWriter writer, MorphException me)
		{
			writer.WriteStartElement("Error");
			writer.WriteString(ProcessMorphException(me));
			writer.WriteEndElement();
		}
		#endregion

		#region class PatrResult
		private class PatrResult
		{
			public List<PcPatrMorph> Morphs { get; set; }
			public WordGrammarTrace WordGrammarTrace { get; set; }
			public bool PassedPatr { get; set; }
		}
		#endregion

		#region class WordGrammarTrace
		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification = "m_cache is a reference and disposed in the parent class")]
		private class WordGrammarTrace
		{
			private readonly IEnumerable<PcPatrMorph> m_morphs;
			private bool m_fSuccess;
			private readonly string m_id;
			private readonly FdoCache m_cache;

			public WordGrammarTrace(string id, IEnumerable<PcPatrMorph> morphs, FdoCache cache)
			{
				m_id = id;
				m_morphs = morphs;
				m_cache = cache;
			}

			/// <summary>
			/// Get/set success status of word grammar attempt
			/// </summary>
			public bool Success
			{
				set { m_fSuccess = value; }

			}

			/// <summary>
			/// Report trace information as XML
			/// </summary>
			/// <param name="writer"></param>
			public void ToXml(XmlWriter writer)
			{
				writer.WriteStartElement("WordGrammarAttempt");
				writer.WriteAttributeString("success", m_fSuccess ? "true" : "false");
				writer.WriteStartElement("Id");
				writer.WriteValue(m_id);
				writer.WriteEndElement();

				string type = "pfx"; // try to guess morph type based on word type
				foreach (PcPatrMorph morph in m_morphs)
				{
					writer.WriteStartElement("morph");
					string sWordType = morph.wordType;
					writer.WriteAttributeString("wordType", sWordType);
					if (type == "pfx" && sWordType == "root")
						type = "root";
					else if (type == "root" && sWordType != "root")
						type = "sfx";
					writer.WriteAttributeString("type", type);
					writer.WriteAttributeString("alloid", morph.formId);
					writer.WriteAttributeString("morphname", morph.msaId);

					writer.WriteMorphInfoElements(m_cache, morph.formId, morph.msaId, morph.wordType, morph.featureDescriptors);
					writer.WriteMsaElement(m_cache, morph.formId, morph.msaId, type, morph.wordType);
					writer.WriteInflClassesElement(m_cache, morph.formId);
					writer.WriteEndElement(); // morph
				}
				writer.WriteEndElement();
			}
		}
		#endregion

		#region class FwXmlTraceManager
		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification = "m_cache is a reference and disposed in the parent class")]
		private class FwXmlTraceManager : XmlTraceManager
		{
			private readonly FdoCache m_cache;

			public FwXmlTraceManager(FdoCache fdoCache)
			{
				m_cache = fdoCache;
			}

			public override void ReportSuccess(WordSynthesis output)
			{
				if (TraceSuccess)
				{
					XElement wsElem = Write("Result", output);
					wsElem.Add(new XAttribute("id", ((uint)output.GetHashCode()).ToString(CultureInfo.InvariantCulture)));
					((XElement)output.CurrentTraceObject).Add(new XElement("ReportSuccessTrace", wsElem));
				}
			}

			public override void MorphologicalRuleNotUnapplied(MorphologicalRule rule, WordAnalysis input)
			{
			}

			public override void MorphologicalRuleNotApplied(MorphologicalRule rule, WordSynthesis input)
			{
			}

			protected override XElement Write(string name, Allomorph allomorph)
			{
				XElement elem = Write(name, (HCObject)allomorph);

				string formIdsStr = allomorph.GetProperty("FormID");
				string msaId = allomorph.GetProperty("MsaID");
				if (!String.IsNullOrEmpty(formIdsStr) || !String.IsNullOrEmpty(msaId))
				{
					var morphElem = new XElement("Morph");
					string firstFormId = null;
					string firstWordType = null;
					string featDesc = allomorph.GetProperty("FeatureDescriptors");
					if (!String.IsNullOrEmpty(formIdsStr))
					{
						string[] formIds = formIdsStr.Split(' ');
						string[] wordTypes = allomorph.GetProperty("WordCategory").Split(' ');
						for (int i = 0; i < formIds.Length; i++)
						{
							int wordTypeIndex = WordTypeIndex(i, wordTypes.Length);
							string wordType = wordTypes[wordTypeIndex];
							morphElem.Add(new XElement("MoForm", new XAttribute("DbRef", formIds[i]), new XAttribute("wordType", wordType)));
							morphElem.Add(string.IsNullOrEmpty(featDesc) ? new XElement("props") : new XElement("props", featDesc));
							if (i == 0)
							{
								firstFormId = formIds[i];
								firstWordType = wordType;
							}
						}
					}

					if (!String.IsNullOrEmpty(msaId))
					{
						var msiElement = new XElement("MSI", new XAttribute("DbRef", msaId));
						using (XmlWriter writer = msiElement.CreateWriter())
							writer.WriteMsaElement(m_cache, firstFormId, msaId, null, firstWordType);
						morphElem.Add(msiElement);
					}

					using (XmlWriter writer = morphElem.CreateWriter())
						writer.WriteMorphInfoElements(m_cache, firstFormId, msaId, firstWordType, featDesc);
					elem.Add(morphElem);
				}

				return elem;
			}
		}
		#endregion

		#region class PcPatrMorph
		class PcPatrMorph
		{
			public string formId;
			public string form;
			public string wordType;
			public string msaId;
			public string featureDescriptors;
			public string gloss;
			public int formIndex;

			public string Form
			{
				get
				{ return String.IsNullOrEmpty(form) ? "0" : form; }
			}
		}
		#endregion

		#region class XmlFwResolver
		class XmlFwResolver : XmlUrlResolver
		{
			readonly Uri m_baseUri;

			public XmlFwResolver(string dataDir)
			{
				m_baseUri = new Uri(dataDir + Path.DirectorySeparatorChar);
			}

			public override Uri ResolveUri(Uri baseUri, string relativeUri)
			{
				return base.ResolveUri(m_baseUri, relativeUri);
			}
		}
		#endregion
	}
}
