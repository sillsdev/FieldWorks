// Copyright (c) 2014-2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.ObjectModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class HCParser : DisposableBase, IParser
	{
		private readonly LcmCache m_cache;
		// Out-of-process Server-GC worker proxy (RUSTIFY-fieldworks-worker-design.md) that now
		// does the bulk/interactive parsing an in-process m_morpher used to do directly. Tracing/
		// Try-a-Word (GetTraceMorpher/ParseToXml below) deliberately still runs in-process: its
		// FwXmlTraceManager touches LCM inline while tracing, which the worker has no access to -
		// see the design's §8 incremental-rollout note (bulk path first, interactive path already
		// ported here since it needed no such LCM-touching trace manager; Try-a-Word is left for a
		// follow-up).
		private readonly HCWorkerClient m_workerClient;
		// A dedicated morpher used only by the trace/Try-A-Word path. Tracing mutates the
		// morpher's LexEntrySelector, RuleSelector, and TraceManager.IsTracing; keeping a
		// separate morpher (with its own trace manager) ensures those mutations never corrupt
		// the bulk m_morpher, which may be parsing concurrently on several threads.
		private Morpher m_traceMorpher;
		private FwXmlTraceManager m_traceMorpherTraceManager;
		private Language m_language;
		private readonly FwXmlTraceManager m_traceManager;
		private readonly string m_outputDirectory;
		private ParserModelChangeListener m_changeListener;
		private bool m_forceUpdate;
		private bool m_guessRoots;
		private bool m_mergeAnalyses;
		private int m_delReapps;
		private int m_maxStemCount;

		// Diagnostic perf counters (accumulated across all threads) splitting bulk-parse time
		// into the lock-free morpher parse vs. the LCM-read mapping (GetMorphs under the read
		// lock). Near-zero overhead; used by the parser concurrency benchmark.
		public static long DiagMorpherParseTicks;
		public static long DiagGetMorphsTicks;

		// the public const strings are for GenerateHCConfigForFLExTrans and HCSynthByGlossLib
		internal const string CRuleID = "ID";
		// FormID/FormID2 are public so the out-of-process HCWorker (Src\LexText\HCWorker) can key
		// the same Allomorph.Properties bag when it projects a parsed Word down to MorphDto[] -
		// keeping the worker's id extraction and this class's GetMorphs consumption on one set of
		// key strings.
		public const string FormID = "ID";
		public const string FormID2 = "ID2";
		public const string InflTypeID = "InflTypeID";
		public const string MsaID = "ID";
		internal const string PRuleID = "ID";
		internal const string SlotID = "SlotID";
		internal const string TemplateID = "ID";

		internal const string IsNull = "IsNull";
		internal const string IsPrefix = "IsPrefix";
		public const string Env = "Env";
		public const string PrefixEnv = "PrefixEnv";
		public const string SuffixEnv = "SuffixEnv";

		public HCParser(LcmCache cache)
		{
			m_cache = cache;
			m_workerClient = new HCWorkerClient();
			m_traceManager = new FwXmlTraceManager(m_cache);
			m_outputDirectory = Path.GetTempPath();
			m_changeListener = new ParserModelChangeListener(m_cache);
			m_forceUpdate = true;
			m_guessRoots = true;
			m_mergeAnalyses = true;
		}

		#region IParser implementation
		public bool IsUpToDate()
		{
			return !m_changeListener.ModelChanged;
		}

		public void Update()
		{
			CheckDisposed();
			if (m_changeListener.Reset() || m_forceUpdate)
			{
				LoadParser();
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

			if (m_language == null)
				return null;

			WordAnalysisDto[] wordAnalyses;
			try
			{
				var morpherSw = Stopwatch.StartNew();
				// Round-trips to the worker process, OUTSIDE the LCM read lock below. The
				// worker's parse is the expensive, CPU-bound part and touches only its own copy
				// of the frozen HC grammar (not LCM), so keeping it off the read lock lets it run
				// without holding the read lock for its whole duration - same reasoning as the
				// in-process call this replaces, just across a process boundary now.
				wordAnalyses = m_workerClient.ParseWord(word, m_guessRoots);
				morpherSw.Stop();
				Interlocked.Add(ref DiagMorpherParseTicks, morpherSw.ElapsedTicks);
			}
			catch (Exception e)
			{
				return new ParseResult(ProcessParseException(e));
			}

			ParseResult result;
			var getMorphsSw = Stopwatch.StartNew();
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				var analyses = new List<ParseAnalysis>();
				foreach (WordAnalysisDto wordAnalysis in wordAnalyses)
				{
					List<MorphInfo> morphs;
					if (GetMorphs(wordAnalysis, out morphs))
					{
						analyses.Add(new ParseAnalysis(morphs.Select(mi =>
							new ParseMorph(mi.Form, mi.Msa, mi.InflType, mi.GuessedString))));
					}
				}
				result = new ParseResult(analyses);
			}
			getMorphsSw.Stop();
			Interlocked.Add(ref DiagGetMorphsTicks, getMorphsSw.ElapsedTicks);

			return result;
		}

		/// <summary>
		/// Bulk path (design §5): one WCF round trip for the whole batch of already-normalized
		/// word forms, instead of ParserWorker's old per-wordform Parallel.ForEach each calling
		/// the single-word ParseWord above. Returns null (rather than throwing) if the batch call
		/// itself fails even after HCWorkerClient's own retry-once, so ParserWorker can fall back
		/// to its per-wordform path for this run instead of losing it entirely (design §6).
		/// </summary>
		public IDictionary<string, ParseResult> ParseWordsBatch(string[] words)
		{
			CheckDisposed();

			if (m_language == null || words.Length == 0)
				return null;

			IDictionary<string, WordAnalysisDto[]> wordAnalysesByWord;
			try
			{
				wordAnalysesByWord = m_workerClient.ParseWordsBatch(words, m_guessRoots);
			}
			catch (Exception)
			{
				return null;
			}

			var results = new Dictionary<string, ParseResult>();
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				foreach (KeyValuePair<string, WordAnalysisDto[]> kvp in wordAnalysesByWord)
				{
					var analyses = new List<ParseAnalysis>();
					foreach (WordAnalysisDto wordAnalysis in kvp.Value)
					{
						List<MorphInfo> morphs;
						if (GetMorphs(wordAnalysis, out morphs))
						{
							analyses.Add(new ParseAnalysis(morphs.Select(mi =>
								new ParseMorph(mi.Form, mi.Msa, mi.InflType, mi.GuessedString))));
						}
					}
					results[kvp.Key] = new ParseResult(analyses);
				}
			}
			return results;
		}

		public XDocument TraceWordXml(string form, IEnumerable<int> selectTraceMorphs)
		{
			CheckDisposed();

			return ParseToXml(form, true, selectTraceMorphs);
		}

		public XDocument ParseWordXml(string form)
		{
			CheckDisposed();

			return ParseToXml(form, false, null);
		}
		#endregion

		protected override void DisposeManagedResources()
		{
			if (m_changeListener != null)
			{
				m_changeListener.Dispose();
				m_changeListener = null;
			}
			m_workerClient?.Dispose();
		}

		private void LoadParser()
		{
			m_language = null;
			// Force the trace morpher to be rebuilt over the freshly loaded language.
			m_traceMorpher = null;
			m_traceMorpherTraceManager = null;

			int delReapps = 0;
			// For Hermit Crab, the maximum number of roots/stems allowed is between one and ten.
			// The default is two in order to allow for compounding (which requires there be at least two roots/stems).
			int maxStemCount = 2;
			string loadErrorsFile = Path.Combine(m_outputDirectory, m_cache.ProjectId.Name + "HCLoadErrors.xml");
			using (XmlWriter writer = XmlWriter.Create(loadErrorsFile))
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				writer.WriteStartElement("LoadErrors");
				m_language = HCLoader.Load(m_cache, new XmlHCLoadErrorLogger(writer));
				writer.WriteEndElement();
				XElement parserParamsElem = XElement.Parse(m_cache.LanguageProject.MorphologicalDataOA.ParserParameters);
				XElement delReappsElem = parserParamsElem.Elements("HC").Elements("DelReapps").FirstOrDefault();
				XElement guessRootsElem = parserParamsElem.Elements("HC").Elements("GuessRoots").FirstOrDefault();
				XElement mergeAnalysesElem = parserParamsElem.Elements("HC").Elements("MergeAnalyses").FirstOrDefault();
				XElement maxRootsElem = parserParamsElem.Elements("HC").Elements("MaxRoots").FirstOrDefault();
				if (delReappsElem != null)
					delReapps = (int) delReappsElem;
				if (guessRootsElem != null)
					m_guessRoots = (bool) guessRootsElem;
				if (mergeAnalysesElem != null)
					m_mergeAnalyses = (bool) mergeAnalysesElem;
				if (maxRootsElem != null)
					maxStemCount = int.Parse(maxRootsElem.Value);
			}
			m_delReapps = delReapps;
			m_maxStemCount = maxStemCount;

			// Ship the freshly loaded grammar to the worker (design §4/§5 "Grammar change"): the
			// same HC.NET XML input format XmlLanguageLoader already reads, produced via
			// XmlLanguageWriter.Save on the Language HCLoader.Load just built - no new
			// serialization format, no changes to SIL.Machine.Morphology.HermitCrab itself.
			string grammarFile = Path.Combine(m_outputDirectory, m_cache.ProjectId.Name + "HCGrammar.xml");
			XmlLanguageWriter.Save(m_language, grammarFile);
			string grammarXml = File.ReadAllText(grammarFile);
			File.Delete(grammarFile);
			m_workerClient.UpdateGrammar(grammarXml, delReapps, maxStemCount, m_mergeAnalyses);
		}

		/// <summary>
		/// Lazily builds (and returns) the morpher used for tracing. It shares the frozen,
		/// read-only <see cref="m_language"/> with the bulk morpher but has its own mutable
		/// state and its own trace manager, so enabling tracing or setting morpheme selectors
		/// here cannot affect a bulk parse running on m_morpher.
		/// </summary>
		private Morpher GetTraceMorpher()
		{
			if (m_traceMorpher == null)
			{
				m_traceMorpherTraceManager = new FwXmlTraceManager(m_cache);
				m_traceMorpher = new Morpher(m_traceMorpherTraceManager, m_language)
				{
					DeletionReapplications = m_delReapps,
					MaxStemCount = m_maxStemCount,
					MergeEquivalentAnalyses = m_mergeAnalyses
				};
			}
			return m_traceMorpher;
		}

		private XDocument ParseToXml(string form, bool tracing, IEnumerable<int> selectTraceMorphs)
		{
			if (m_language == null)
				return null;

			// Use the dedicated trace morpher so that setting selectors / IsTracing here cannot
			// corrupt a bulk parse that may be running concurrently on m_morpher.
			Morpher traceMorpher = GetTraceMorpher();

			var doc = new XDocument();
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				if (selectTraceMorphs != null)
				{
					var selectTraceMorphsSet = new HashSet<int>(selectTraceMorphs);
					traceMorpher.LexEntrySelector = entry => selectTraceMorphsSet.Contains((int) entry.Properties[MsaID]);
					traceMorpher.RuleSelector = rule =>
					{
						// Need to check if the rule is a morpheme and if it has a non-null msa id.
						// If the rule comes from an irregularly inflected form, msa id will be null.
						if (rule is Morpheme mRule && mRule.Properties[MsaID] != null)
						{
							return selectTraceMorphsSet.Contains((int)mRule.Properties[MsaID]);
						}
						return true;
					};
				}
				else
				{
					traceMorpher.LexEntrySelector = entry => true;
					traceMorpher.RuleSelector = rule => true;
				}
				traceMorpher.TraceManager.IsTracing = tracing;
				var wordformElem = new XElement("Wordform", new XAttribute("form", form));
				try
				{
					object trace;
					foreach (Word wordAnalysis in traceMorpher.ParseWord(form, out trace, m_guessRoots))
					{
						List<MorphInfo> morphs;
						if (GetMorphs(wordAnalysis, out morphs))
							wordformElem.Add(new XElement("Analysis", morphs.Select(mi => CreateAllomorphElement("Morph", mi.Form, mi.Msa, mi.InflType, mi.IsCircumfix, mi.GuessedString))));
					}
					if (tracing)
						wordformElem.Add(new XElement("Trace", trace));
				}
				catch (Exception exc)
				{
					wordformElem.Add(new XElement("Error", ProcessParseException(exc)));
				}
				WriteDataIssues(wordformElem);
				doc.Add(wordformElem);
			}
			return doc;
		}

		/// <summary>
		/// Check integrity of phoneme-based natural classes (PhNCSegments)
		/// when there are phonological features
		/// </summary>
		public void WriteDataIssues(XElement elem)
		{
			if (!m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.Any())
				return; // no phonological features so nothing to check

			using (XmlWriter writer = elem.CreateWriter())
			{
				writer.WriteStartElement("DataIssues");
				foreach (IPhNCSegments natClass in m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.OfType<IPhNCSegments>())
				{
					HashSet<IFsSymFeatVal> feats = GetImpliedPhonologicalFeatures(natClass);
					var predictedPhonemes = new HashSet<IPhPhoneme>(m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.SelectMany(ps => ps.PhonemesOC).Where(p => GetFeatures(p) != null && feats.IsSubsetOf(GetFeatures(p))));
					if (!predictedPhonemes.SetEquals(natClass.SegmentsRC))
					{
						writer.WriteStartElement("NatClassPhonemeMismatch");
						writer.WriteElementString("ClassName", natClass.Name.BestAnalysisAlternative.Text);
						writer.WriteElementString("ClassAbbeviation", natClass.Abbreviation.BestAnalysisAlternative.Text);
						writer.WriteElementString("ImpliedPhonologicalFeatures", feats.Count == 0 ? "" : string.Format("[{0}]", string.Join(" ", feats.Select(v => string.Format("{0}:{1}", GetFeatureString(v), GetValueString(v))))));
						writer.WriteElementString("PredictedPhonemes", string.Join(" ", predictedPhonemes.Select(p => p.Name.BestVernacularAlternative.Text)));
						writer.WriteElementString("ActualPhonemes", string.Join(" ", natClass.SegmentsRC.Select(p => p.Name.BestVernacularAlternative.Text)));
						writer.WriteEndElement();
					}
				}
				foreach (IPhPhoneme phone in m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC)
				{
					foreach (IPhCode code in phone.CodesOS)
					{
						if (code != null && code.Representation != null)
						{
							var grapheme = code.Representation.BestVernacularAlternative.Text;
							// Check for empty graphemes/codes whcih can cause a crash; see https://jira.sil.org/browse/LT-21589
							if (String.IsNullOrEmpty(grapheme) || grapheme == "***")
							{
								writer.WriteStartElement("EmptyGrapheme");
								writer.WriteElementString("Phoneme", phone.Name.BestVernacularAnalysisAlternative.Text);
								writer.WriteEndElement();
							}
							else
							// Check for '[' and ']' which can cause a mysterious message in Try a Word
							if (grapheme.Contains("[") || grapheme.Contains("]"))
							{
								writer.WriteStartElement("NoBracketsAsGraphemes");
								writer.WriteElementString("Grapheme", grapheme);
								writer.WriteElementString("Phoneme", phone.Name.BestVernacularAnalysisAlternative.Text);
								writer.WriteElementString("Bracket", grapheme);
								writer.WriteEndElement();
							}
						}
					}
				}
				writer.WriteEndElement();
			}
		}

		private static HashSet<IFsSymFeatVal> GetImpliedPhonologicalFeatures(IPhNCSegments nc)
		{
			HashSet<IFsSymFeatVal> results = null;
			foreach (IPhPhoneme phoneme in nc.SegmentsRC.Where(p => p.FeaturesOA != null && !p.FeaturesOA.IsEmpty))
			{
				IEnumerable<IFsSymFeatVal> values = GetFeatures(phoneme);
				if (results == null)
					results = new HashSet<IFsSymFeatVal>(values);
				else
					results.IntersectWith(values);
			}
			return results ?? new HashSet<IFsSymFeatVal>();
		}

		private static IEnumerable<IFsSymFeatVal> GetFeatures(IPhPhoneme phoneme)
		{
			if (phoneme == null || phoneme.FeaturesOA == null)
				return null;
			return phoneme.FeaturesOA.FeatureSpecsOC.OfType<IFsClosedValue>().Select(cv => cv.ValueRA);
		}

		private static string GetFeatureString(IFsSymFeatVal value)
		{
			var feature = value.OwnerOfClass<IFsClosedFeature>();
			string str = feature.Abbreviation.BestAnalysisAlternative.Text;
			if (string.IsNullOrEmpty(str))
				str = feature.Name.BestAnalysisAlternative.Text;
			return str;
		}

		private static string GetValueString(IFsSymFeatVal value)
		{
			string str = value.Abbreviation.BestAnalysisAlternative.Text;
			if (string.IsNullOrEmpty(str))
				str = value.Name.BestAnalysisAlternative.Text;
			return str;
		}

		private bool GetMorphs(Word ws, out List<MorphInfo> result)
		{
			var morphs = new Dictionary<Morpheme, MorphInfo>();

			var aprCircumfixes = new List<int>();
			bool isSuffixPortionOfAprCircumfix = false;

			result = new List<MorphInfo>();
			foreach (Annotation<ShapeNode> morph in ws.Morphs)
			{
				Allomorph allomorph = ws.GetAllomorph(morph);
				var formID = (int?) allomorph.Properties[FormID] ?? 0;
				if (formID == 0)
					continue;

				isSuffixPortionOfAprCircumfix = false;
				var formID2 = (int?) allomorph.Properties[FormID2] ?? 0;
				if (formID2 == 0 && allomorph is AffixProcessAllomorph)
				{
					// Per the Leipzig glossing rules (https://www.eva.mpg.de/lingua/resources/glossing-rules.php),
					// circumfixes should appear both before and after the material they attach to.
					// HC does not have an overt marker for a circumfix when it is an affix processing rule (aka APR).
					// The following code determines when an APR is marked as a circumfix in FLEx and ensures the
					// two instances of it as a morph are included in the result at the correct places.
					// This is a fix for https://jira.sil.org/browse/LT-21447
					IMoForm circumForm;
					if (!m_cache.ServiceLocator.GetInstance<IMoFormRepository>().TryGetObject(formID, out circumForm))
					{
						result = null;
						return false;
					}
					if (circumForm.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphCircumfix)
					{
						if (aprCircumfixes.Contains(formID))
						{
							isSuffixPortionOfAprCircumfix = true;
						}
						else
						{
							// Remember this allomorph as an APR that is a circumfix
							aprCircumfixes.Add(formID);
						}
					}
				}


				string formStr = ws.Shape.GetNodes(morph.Range).ToString(ws.Stratum.CharacterDefinitionTable, false);
				int curFormID;
				MorphInfo morphInfo;
				if (!morphs.TryGetValue(allomorph.Morpheme, out morphInfo) || isSuffixPortionOfAprCircumfix)
				{
					curFormID = formID;
				}
				else if (formID2 > 0)
				{
					// circumfix
					curFormID = formID2;
				}
				else
				{
					continue;
				}

				IMoForm form;
				if (!m_cache.ServiceLocator.GetInstance<IMoFormRepository>().TryGetObject(curFormID, out form))
				{
					result = null;
					return false;
				}

				var msaID = (int) allomorph.Morpheme.Properties[MsaID];
				IMoMorphSynAnalysis msa;
				if (!m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().TryGetObject(msaID, out msa))
				{
					result = null;
					return false;
				}

				var inflTypeID = (int?) allomorph.Morpheme.Properties[InflTypeID] ?? 0;
				ILexEntryInflType inflType = null;
				if (inflTypeID > 0 && !m_cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(inflTypeID, out inflType))
				{
					result = null;
					return false;
				}

				morphInfo = new MorphInfo
					{
						Form = form,
						GuessedString = allomorph.Guessed ? formStr : null,
						Msa = msa,
						InflType = inflType,
						IsCircumfix = formID2 > 0
					};

				morphs[allomorph.Morpheme] = morphInfo;

				switch ((form.MorphTypeRA == null ? Guid.Empty : form.MorphTypeRA.Guid).ToString())
				{
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
						if (result.Count == 0)
							result.Add(morphInfo);
						else
							result.Insert(result.Count - 1, morphInfo);
						break;

					default:
						result.Add(morphInfo);
						break;
				}
			}
			return true;
		}

		/// <summary>
		/// The LCM-object-resolution half of GetMorphs(Word,...) above, ported to run over the
		/// worker's flat MorphDto[] instead of walking a live Word/Annotation&lt;ShapeNode&gt;/
		/// Allomorph/Morpheme graph (the worker has no LcmCache, so it can't do these repository
		/// lookups itself - see HCWorkerService.ToWordAnalysisDto and MorphDto's doc comment in
		/// IHCWorkerService.cs). Every circumfix/infix-placement decision below is identical to
		/// the Word-based version; only the source of FormId/FormId2/MsaId/InflTypeId/Guessed/
		/// FormStr and the "have we seen this morpheme already" key (MorphemeIndex instead of a
		/// Morpheme reference) changed.
		/// </summary>
		private bool GetMorphs(WordAnalysisDto wordAnalysis, out List<MorphInfo> result)
		{
			var morphs = new Dictionary<int, MorphInfo>();

			var aprCircumfixes = new List<int>();
			bool isSuffixPortionOfAprCircumfix = false;

			result = new List<MorphInfo>();
			foreach (MorphDto morphDto in wordAnalysis.Morphs)
			{
				// The worker already skips morphs with no FormId (HCWorkerService.
				// ToWordAnalysisDto mirrors this method's Word-based twin's `if (formID == 0)
				// continue;`), so every entry reaching here has one.
				int formID = morphDto.FormId;

				isSuffixPortionOfAprCircumfix = false;
				int formID2 = morphDto.FormId2;
				if (formID2 == 0 && morphDto.IsAffixProcessAllomorph)
				{
					// Per the Leipzig glossing rules (https://www.eva.mpg.de/lingua/resources/glossing-rules.php),
					// circumfixes should appear both before and after the material they attach to.
					// HC does not have an overt marker for a circumfix when it is an affix processing rule (aka APR).
					// The following code determines when an APR is marked as a circumfix in FLEx and ensures the
					// two instances of it as a morph are included in the result at the correct places.
					// This is a fix for https://jira.sil.org/browse/LT-21447
					IMoForm circumForm;
					if (!m_cache.ServiceLocator.GetInstance<IMoFormRepository>().TryGetObject(formID, out circumForm))
					{
						result = null;
						return false;
					}
					if (circumForm.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphCircumfix)
					{
						if (aprCircumfixes.Contains(formID))
						{
							isSuffixPortionOfAprCircumfix = true;
						}
						else
						{
							// Remember this allomorph as an APR that is a circumfix
							aprCircumfixes.Add(formID);
						}
					}
				}

				int curFormID;
				MorphInfo morphInfo;
				if (!morphs.TryGetValue(morphDto.MorphemeIndex, out morphInfo) || isSuffixPortionOfAprCircumfix)
				{
					curFormID = formID;
				}
				else if (formID2 > 0)
				{
					// circumfix
					curFormID = formID2;
				}
				else
				{
					continue;
				}

				IMoForm form;
				if (!m_cache.ServiceLocator.GetInstance<IMoFormRepository>().TryGetObject(curFormID, out form))
				{
					result = null;
					return false;
				}

				IMoMorphSynAnalysis msa;
				if (!m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().TryGetObject(morphDto.MsaId, out msa))
				{
					result = null;
					return false;
				}

				ILexEntryInflType inflType = null;
				if (morphDto.InflTypeId > 0 && !m_cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(morphDto.InflTypeId, out inflType))
				{
					result = null;
					return false;
				}

				morphInfo = new MorphInfo
					{
						Form = form,
						GuessedString = morphDto.Guessed ? morphDto.FormStr : null,
						Msa = msa,
						InflType = inflType,
						IsCircumfix = formID2 > 0
					};

				morphs[morphDto.MorphemeIndex] = morphInfo;

				switch ((form.MorphTypeRA == null ? Guid.Empty : form.MorphTypeRA.Guid).ToString())
				{
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
						if (result.Count == 0)
							result.Add(morphInfo);
						else
							result.Insert(result.Count - 1, morphInfo);
						break;

					default:
						result.Add(morphInfo);
						break;
				}
			}
			return true;
		}

		private static string GetMorphTypeString(Guid typeGuid)
		{
			switch (typeGuid.ToString())
			{
				case MoMorphTypeTags.kMorphBoundRoot:
					return "boundRoot";
				case MoMorphTypeTags.kMorphBoundStem:
					return "boundStem";
				case MoMorphTypeTags.kMorphCircumfix:
					return "circumfix";
				case MoMorphTypeTags.kMorphClitic:
					return "clitic";
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
					return "discontigPhrase";
				case MoMorphTypeTags.kMorphEnclitic:
					return "enclitic";
				case MoMorphTypeTags.kMorphInfix:
					return "infix";
				case MoMorphTypeTags.kMorphInfixingInterfix:
					return "infixIterfix";
				case MoMorphTypeTags.kMorphParticle:
					return "particle";
				case MoMorphTypeTags.kMorphPhrase:
					return "phrase";
				case MoMorphTypeTags.kMorphPrefix:
					return "prefix";
				case MoMorphTypeTags.kMorphPrefixingInterfix:
					return "prefixInterfix";
				case MoMorphTypeTags.kMorphProclitic:
					return "proclitic";
				case MoMorphTypeTags.kMorphRoot:
					return "root";
				case MoMorphTypeTags.kMorphSimulfix:
					return "simulfix";
				case MoMorphTypeTags.kMorphStem:
					return "stem";
				case MoMorphTypeTags.kMorphSuffix:
					return "suffix";
				case MoMorphTypeTags.kMorphSuffixingInterfix:
					return "suffixInterfix";
				case MoMorphTypeTags.kMorphSuprafix:
					return "suprafix";
			}
			return "unknown";
		}

		internal static XElement CreateAllomorphElement(string name, IMoForm form, IMoMorphSynAnalysis msa, ILexEntryInflType inflType, bool circumfix, string guessedString)
		{
			Guid morphTypeGuid = circumfix ? MoMorphTypeTags.kguidMorphCircumfix : (form.MorphTypeRA == null ? Guid.Empty : form.MorphTypeRA.Guid);
			var elem = new XElement(name, new XAttribute("id", form.Hvo), new XAttribute("type", GetMorphTypeString(morphTypeGuid)),
				new XElement("Form", circumfix ? form.OwnerOfClass<ILexEntry>().HeadWord.Text : guessedString ?? form.GetFormWithMarkers(form.Cache.DefaultVernWs)),
				new XElement("LongName", form.LongName));
			elem.Add(CreateMorphemeElement(msa, inflType));
			return elem;
		}

		internal static XElement CreateMorphemeElement(IMoMorphSynAnalysis msa, ILexEntryInflType inflType)
		{
			var msaElem = new XElement("Morpheme", new XAttribute("id", msa.Hvo));
			switch (msa.ClassID)
			{
				case MoStemMsaTags.kClassId:
					var stemMsa = (IMoStemMsa) msa;
					msaElem.Add(new XAttribute("type", "stem"));
					if (stemMsa.PartOfSpeechRA != null)
						msaElem.Add(new XElement("Category", stemMsa.PartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					if (stemMsa.FromPartsOfSpeechRC.Count > 0)
						msaElem.Add(new XElement("FromCategories", stemMsa.FromPartsOfSpeechRC.Select(pos => new XElement("Category", pos.Abbreviation.BestAnalysisAlternative.Text))));
					if (stemMsa.InflectionClassRA != null)
						msaElem.Add(new XElement("InflClass", stemMsa.InflectionClassRA.Abbreviation.BestAnalysisAlternative.Text));
					break;

				case MoDerivAffMsaTags.kClassId:
					var derivMsa = (IMoDerivAffMsa) msa;
					msaElem.Add(new XAttribute("type", "deriv"));
					if (derivMsa.FromPartOfSpeechRA != null)
						msaElem.Add(new XElement("FromCategory", derivMsa.FromPartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					if (derivMsa.ToPartOfSpeechRA != null)
						msaElem.Add(new XElement("ToCategory", derivMsa.ToPartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					if (derivMsa.ToInflectionClassRA != null)
						msaElem.Add(new XElement("ToInflClass", derivMsa.ToInflectionClassRA.Abbreviation.BestAnalysisAlternative.Text));
					break;

				case MoUnclassifiedAffixMsaTags.kClassId:
					var unclassMsa = (IMoUnclassifiedAffixMsa) msa;
					msaElem.Add(new XAttribute("type", "unclass"));
					if (unclassMsa.PartOfSpeechRA != null)
						msaElem.Add(new XElement("Category", unclassMsa.PartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					break;

				case MoInflAffMsaTags.kClassId:
					var inflMsa = (IMoInflAffMsa) msa;
					msaElem.Add(new XAttribute("type", "infl"));
					if (inflMsa.PartOfSpeechRA != null)
						msaElem.Add(new XElement("Category", inflMsa.PartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					if (inflMsa.SlotsRC.Count > 0)
					{
						IMoInflAffixSlot slot = inflMsa.SlotsRC.First();
						msaElem.Add(new XElement("Slot", new XAttribute("optional", slot.Optional), slot.Name.BestAnalysisAlternative.Text));
					}
					break;
			}

			msaElem.Add(new XElement("HeadWord", msa.OwnerOfClass<ILexEntry>().HeadWord.Text));

			var glossSB = new StringBuilder();
			if (inflType != null)
			{
				string prepend = inflType.GlossPrepend.BestAnalysisAlternative.Text;
				if (prepend != "***")
					glossSB.Append(prepend);
			}
			ILexSense sense = msa.OwnerOfClass<ILexEntry>().SenseWithMsa(msa);
			glossSB.Append(sense == null ? ParserCoreStrings.ksQuestions : sense.Gloss.BestAnalysisAlternative.Text);
			if (inflType != null)
			{
				string append = inflType.GlossAppend.BestAnalysisAlternative.Text;
				if (append != "***")
					glossSB.Append(append);
			}
			msaElem.Add(new XElement("Gloss", glossSB.ToString()));
			return msaElem;
		}

		private string ProcessParseException(Exception e)
		{
			var ise = e as InvalidShapeException;
			if (ise != null)
			{
				string phonemesFoundSoFar = ise.String.Substring(0, ise.Position);
				string rest = ise.String.Substring(ise.Position);
				if (Icu.Character.GetCharType(rest[0]) == Icu.Character.UCharCategory.NON_SPACING_MARK)
				{
					// the first character is a diacritic, combining type of character
					// insert a space so it does not show on top of a single quote in the message string
					rest = " " + rest;
				}
				return string.Format(ParserCoreStrings.ksHCInvalidWordform, ise.String, ise.Position + 1, rest, phonemesFoundSoFar);
			}

			return String.Format(ParserCoreStrings.ksHCDefaultErrorMsg, e.Message);
		}

		#region class MorphInfo
		class MorphInfo
		{
			public IMoForm Form { get; set; }
			public string GuessedString { get; set; }
			public IMoMorphSynAnalysis Msa { get; set; }
			public ILexEntryInflType InflType { get; set; }
			public bool IsCircumfix { get; set; }
		}
		#endregion

		class XmlHCLoadErrorLogger : IHCLoadErrorLogger
		{
			private readonly XmlWriter m_xmlWriter;

			public XmlHCLoadErrorLogger(XmlWriter xmlWriter)
			{
				m_xmlWriter = xmlWriter;
			}

			public void InvalidShape(string str, int errorPos, IMoMorphSynAnalysis msa)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-shape");
				m_xmlWriter.WriteElementString("Form", str);
				m_xmlWriter.WriteElementString("Position", errorPos.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteElementString("Hvo", msa.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}

			public void InvalidAffixProcess(IMoAffixProcess affixProcess, bool isInvalidLhs, IMoMorphSynAnalysis msa)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-affix-process");
				m_xmlWriter.WriteElementString("Form", affixProcess.Form.BestVernacularAlternative.Text);
				m_xmlWriter.WriteElementString("InvalidLhs", isInvalidLhs.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteElementString("Hvo", msa.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}

			public void InvalidPhoneme(IPhPhoneme phoneme)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-phoneme");
				m_xmlWriter.WriteElementString("Name", phoneme.ShortName);
				m_xmlWriter.WriteElementString("Hvo", phoneme.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}

			public void DuplicateGrapheme(IPhPhoneme phoneme)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "duplicate-grapheme");
				m_xmlWriter.WriteElementString("Name", phoneme.ShortName);
				m_xmlWriter.WriteElementString("Hvo", phoneme.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}

			public void InvalidEnvironment(IMoForm form, IPhEnvironment env, string reason, IMoMorphSynAnalysis msa)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-environment");
				m_xmlWriter.WriteElementString("Form", form.Form.VernacularDefaultWritingSystem.Text);
				m_xmlWriter.WriteElementString("Env", env.StringRepresentation.Text);
				m_xmlWriter.WriteElementString("Hvo", msa.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteElementString("Reason", reason);
				m_xmlWriter.WriteEndElement();
			}

			public void InvalidReduplicationForm(IMoForm form, string reason, IMoMorphSynAnalysis msa)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-redup-form");
				m_xmlWriter.WriteElementString("Form", form.Form.VernacularDefaultWritingSystem.Text);
				m_xmlWriter.WriteElementString("Hvo", msa.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteElementString("Reason", reason);
				m_xmlWriter.WriteEndElement();
			}
			public void InvalidRewriteRule(IPhRegularRule rule, string reason)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-rewrite-rule");
				m_xmlWriter.WriteElementString("Rule", rule.Name.BestAnalysisVernacularAlternative.Text);
				m_xmlWriter.WriteElementString("Reason", reason);
				m_xmlWriter.WriteEndElement();
			}

			public void InvalidStrata(string strata, string reason)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-strata");
				m_xmlWriter.WriteElementString("Reason", reason);
				m_xmlWriter.WriteEndElement();
			}

			public void OutOfScopeSlot(IMoInflAffixSlot slot, IMoInflAffixTemplate template, string reason)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "out-of-scope-slot");
				m_xmlWriter.WriteElementString("Reason", reason);
				m_xmlWriter.WriteEndElement();
			}
			public void UnmatchedReduplicationIndexedClass(IMoForm form, string reason, string pattern)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "unmatched-redup-indexed-class");
				m_xmlWriter.WriteElementString("Form", form.Form.VernacularDefaultWritingSystem.Text);
				m_xmlWriter.WriteElementString("Pattern", pattern);
				m_xmlWriter.WriteElementString("Reason", reason);
				m_xmlWriter.WriteElementString("Hvo", form.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}
		}
	}
}
