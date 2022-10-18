// Copyright (c) 2014-2022 SIL International
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
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;
using SIL.ObjectModel;

namespace SIL.FieldWorks.WordWorks.Parser.HermitCrab
{
	public sealed class HCParser : DisposableBase, IParser
	{
		private readonly LcmCache m_cache;
		private Morpher m_morpher;
		private Language m_language;
		private readonly ITraceManager m_traceManager;
		private readonly string m_outputDirectory;
		private ParserModelChangeListener m_changeListener;
		private bool m_forceUpdate;

		internal const string CRuleID = "ID";
		internal const string FormID = "ID";
		internal const string FormID2 = "ID2";
		internal const string InflTypeID = "InflTypeID";
		internal const string MsaID = "ID";
		internal const string PRuleID = "ID";
		internal const string SlotID = "SlotID";
		internal const string TemplateID = "ID";

		internal const string IsNull = "IsNull";
		internal const string IsPrefix = "IsPrefix";
		internal const string Env = "Env";
		internal const string PrefixEnv = "PrefixEnv";
		internal const string SuffixEnv = "SuffixEnv";

		public HCParser(LcmCache cache)
		{
			m_cache = cache;
			m_traceManager = new FwXmlTraceManager(m_cache.ServiceLocator);
			m_outputDirectory = Path.GetTempPath();
			m_changeListener = new ParserModelChangeListener(m_cache);
			m_forceUpdate = true;
		}

		#region IParser implementation
		public bool IsUpToDate()
		{
			return !m_changeListener.ModelChanged;
		}

		public void Update()
		{
			if (m_changeListener.Reset() || m_forceUpdate)
			{
				LoadParser();
				m_forceUpdate = false;
			}
		}

		public void Reset()
		{
			m_forceUpdate = true;
		}

		public ParseResult ParseWord(string word)
		{
			if (m_morpher == null)
			{
				return null;
			}
			IEnumerable<Word> wordAnalyses;
			try
			{
				wordAnalyses = m_morpher.ParseWord(word);
			}
			catch (Exception e)
			{
				return new ParseResult(ProcessParseException(e));
			}

			ParseResult result;
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				var analyses = new List<ParseAnalysis>();
				foreach (var wordAnalysis in wordAnalyses)
				{
					if (GetMorphs(wordAnalysis, out var morphs))
					{
						analyses.Add(new ParseAnalysis(morphs.Select(mi => new ParseMorph(mi.Form, mi.Msa, mi.InflType))));
					}
				}
				result = new ParseResult(analyses);
			}

			return result;
		}

		public XDocument TraceWordXml(string form, IEnumerable<int> selectTraceMorphs)
		{
			return ParseToXml(form, true, selectTraceMorphs);
		}

		public XDocument ParseWordXml(string form)
		{
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
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}

		private void LoadParser()
		{
			m_morpher = null;
			var delReapps = 0;
			var loadErrorsFile = Path.Combine(m_outputDirectory, m_cache.ProjectId.Name + "HCLoadErrors.xml");
			using (var writer = XmlWriter.Create(loadErrorsFile))
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				writer.WriteStartElement("LoadErrors");
				m_language = HCLoader.Load(m_cache, new XmlHCLoadErrorLogger(writer));
				writer.WriteEndElement();
				var parserParamsElem = XElement.Parse(m_cache.LanguageProject.MorphologicalDataOA.ParserParameters);
				var delReappsElem = parserParamsElem.Elements("ParserParameters").Elements("HC").Elements("DelReapps").FirstOrDefault();
				if (delReappsElem != null)
				{
					delReapps = (int)delReappsElem;
				}
			}
			m_morpher = new Morpher(m_traceManager, m_language) { DeletionReapplications = delReapps };
		}

		private XDocument ParseToXml(string form, bool tracing, IEnumerable<int> selectTraceMorphs)
		{
			if (m_morpher == null)
			{
				return null;
			}
			var doc = new XDocument();
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				if (selectTraceMorphs != null)
				{
					var selectTraceMorphsSet = new HashSet<int>(selectTraceMorphs);
					m_morpher.LexEntrySelector = entry => selectTraceMorphsSet.Contains((int)entry.Properties[MsaID]);
					m_morpher.RuleSelector = rule => !(rule is Morpheme morphRule) || selectTraceMorphsSet.Contains((int)morphRule.Properties[MsaID]);
				}
				else
				{
					m_morpher.LexEntrySelector = entry => true;
					m_morpher.RuleSelector = rule => true;
				}
				m_morpher.TraceManager.IsTracing = tracing;
				var wordformElem = new XElement("Wordform", new XAttribute("form", form));
				try
				{
					object trace;
					foreach (var wordAnalysis in m_morpher.ParseWord(form, out trace))
					{
						if (GetMorphs(wordAnalysis, out var morphs))
						{
							wordformElem.Add(new XElement("Analysis", morphs.Select(mi => CreateAllomorphElement("Morph", mi.Form, mi.Msa, mi.InflType, mi.IsCircumfix))));
						}
					}
					if (tracing)
					{
						wordformElem.Add(new XElement("Trace", trace));
					}
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
			{
				return; // no phonological features so nothing to check
			}
			using (var writer = elem.CreateWriter())
			{
				writer.WriteStartElement("DataIssues");
				foreach (var natClass in m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.OfType<IPhNCSegments>())
				{
					var feats = GetImpliedPhonologicalFeatures(natClass);
					var predictedPhonemes = new HashSet<IPhPhoneme>(m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.SelectMany(ps => ps.PhonemesOC).Where(p => GetFeatures(p) != null && feats.IsSubsetOf(GetFeatures(p))));
					if (!predictedPhonemes.SetEquals(natClass.SegmentsRC))
					{
						writer.WriteStartElement("NatClassPhonemeMismatch");
						writer.WriteElementString("ClassName", natClass.Name.BestAnalysisAlternative.Text);
						writer.WriteElementString("ClassAbbeviation", natClass.Abbreviation.BestAnalysisAlternative.Text);
						writer.WriteElementString("ImpliedPhonologicalFeatures", feats.Count == 0 ? "" : $"[{string.Join(" ", feats.Select(v => $"{GetFeatureString(v)}:{GetValueString(v)}"))}]");
						writer.WriteElementString("PredictedPhonemes", string.Join(" ", predictedPhonemes.Select(p => p.Name.BestVernacularAlternative.Text)));
						writer.WriteElementString("ActualPhonemes", string.Join(" ", natClass.SegmentsRC.Select(p => p.Name.BestVernacularAlternative.Text)));
						writer.WriteEndElement();
					}
				}
				writer.WriteEndElement();
			}
		}

		private static HashSet<IFsSymFeatVal> GetImpliedPhonologicalFeatures(IPhNCSegments nc)
		{
			HashSet<IFsSymFeatVal> results = null;
			foreach (var phoneme in nc.SegmentsRC.Where(p => p.FeaturesOA != null && !p.FeaturesOA.IsEmpty))
			{
				var values = GetFeatures(phoneme);
				if (results == null)
				{
					results = new HashSet<IFsSymFeatVal>(values);
				}
				else
				{
					results.IntersectWith(values);
				}
			}
			return results ?? new HashSet<IFsSymFeatVal>();
		}

		private static IEnumerable<IFsSymFeatVal> GetFeatures(IPhPhoneme phoneme)
		{
			return phoneme?.FeaturesOA?.FeatureSpecsOC.OfType<IFsClosedValue>().Select(cv => cv.ValueRA);
		}

		private static string GetFeatureString(IFsSymFeatVal value)
		{
			var feature = value.OwnerOfClass<IFsClosedFeature>();
			var str = feature.Abbreviation.BestAnalysisAlternative.Text;
			if (string.IsNullOrEmpty(str))
			{
				str = feature.Name.BestAnalysisAlternative.Text;
			}
			return str;
		}

		private static string GetValueString(IFsSymFeatVal value)
		{
			var str = value.Abbreviation.BestAnalysisAlternative.Text;
			if (string.IsNullOrEmpty(str))
			{
				str = value.Name.BestAnalysisAlternative.Text;
			}
			return str;
		}

		private bool GetMorphs(Word ws, out List<MorphInfo> result)
		{
			var morphs = new Dictionary<Morpheme, MorphInfo>();
			result = new List<MorphInfo>();
			foreach (var morph in ws.Morphs)
			{
				var allomorph = ws.GetAllomorph(morph);
				var formID = (int?)allomorph.Properties[FormID] ?? 0;
				if (formID == 0)
				{
					continue;
				}
				var formID2 = (int?)allomorph.Properties[FormID2] ?? 0;
				var formStr = ws.Shape.GetNodes(morph.Range).ToString(ws.Stratum.CharacterDefinitionTable, false);
				int curFormID;
				if (!morphs.TryGetValue(allomorph.Morpheme, out var morphInfo))
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
					morphInfo.String += formStr;
					continue;
				}

				if (!m_cache.ServiceLocator.GetInstance<IMoFormRepository>().TryGetObject(curFormID, out var form))
				{
					result = null;
					return false;
				}

				var msaID = (int)allomorph.Morpheme.Properties[MsaID];
				if (!m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().TryGetObject(msaID, out var msa))
				{
					result = null;
					return false;
				}

				var inflTypeID = (int?)allomorph.Morpheme.Properties[InflTypeID] ?? 0;
				ILexEntryInflType inflType = null;
				if (inflTypeID > 0 && !m_cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(inflTypeID, out inflType))
				{
					result = null;
					return false;
				}

				morphInfo = new MorphInfo
				{
					Form = form,
					String = formStr,
					Msa = msa,
					InflType = inflType,
					IsCircumfix = formID2 > 0
				};

				morphs[allomorph.Morpheme] = morphInfo;

				switch ((form.MorphTypeRA?.Guid ?? Guid.Empty).ToString())
				{
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
						if (result.Count == 0)
						{
							result.Add(morphInfo);
						}
						else
						{
							result.Insert(result.Count - 1, morphInfo);
						}
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

		internal static XElement CreateAllomorphElement(string name, IMoForm form, IMoMorphSynAnalysis msa, ILexEntryInflType inflType, bool circumfix)
		{
			var morphTypeGuid = circumfix ? MoMorphTypeTags.kguidMorphCircumfix : form.MorphTypeRA?.Guid ?? Guid.Empty;
			var elem = new XElement(name, new XAttribute("id", form.Hvo), new XAttribute("type", GetMorphTypeString(morphTypeGuid)),
				new XElement("Form", circumfix ? form.OwnerOfClass<ILexEntry>().HeadWord.Text : form.GetFormWithMarkers(form.Cache.DefaultVernWs)),
				new XElement("LongName", form.LongName));
			elem.Add(CreateMorphemeElement(msa, inflType));
			return elem;
		}

		internal static XElement CreateMorphemeElement(IMoMorphSynAnalysis msa, ILexEntryInflType inflType)
		{
			var msaElem = new XElement("Morpheme", new XAttribute("id", msa.Hvo));
			switch (msa)
			{
				case IMoStemMsa stemMsa:
					msaElem.Add(new XAttribute("type", "stem"));
					if (stemMsa.PartOfSpeechRA != null)
					{
						msaElem.Add(new XElement("Category", stemMsa.PartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					}
					if (stemMsa.FromPartsOfSpeechRC.Count > 0)
					{
						msaElem.Add(new XElement("FromCategories", stemMsa.FromPartsOfSpeechRC.Select(pos => new XElement("Category", pos.Abbreviation.BestAnalysisAlternative.Text))));
					}
					if (stemMsa.InflectionClassRA != null)
					{
						msaElem.Add(new XElement("InflClass", stemMsa.InflectionClassRA.Abbreviation.BestAnalysisAlternative.Text));
					}
					break;
				case IMoDerivAffMsa derivMsa:
					msaElem.Add(new XAttribute("type", "deriv"));
					if (derivMsa.FromPartOfSpeechRA != null)
					{
						msaElem.Add(new XElement("FromCategory", derivMsa.FromPartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					}
					if (derivMsa.ToPartOfSpeechRA != null)
					{
						msaElem.Add(new XElement("ToCategory", derivMsa.ToPartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					}
					if (derivMsa.ToInflectionClassRA != null)
					{
						msaElem.Add(new XElement("ToInflClass", derivMsa.ToInflectionClassRA.Abbreviation.BestAnalysisAlternative.Text));
					}
					break;
				case IMoUnclassifiedAffixMsa unclassMsa:
					msaElem.Add(new XAttribute("type", "unclass"));
					if (unclassMsa.PartOfSpeechRA != null)
					{
						msaElem.Add(new XElement("Category", unclassMsa.PartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					}
					break;
				case IMoInflAffMsa inflMsa:
					msaElem.Add(new XAttribute("type", "infl"));
					if (inflMsa.PartOfSpeechRA != null)
					{
						msaElem.Add(new XElement("Category", inflMsa.PartOfSpeechRA.Abbreviation.BestAnalysisAlternative.Text));
					}
					if (inflMsa.SlotsRC.Count > 0)
					{
						var slot = inflMsa.SlotsRC.First();
						msaElem.Add(new XElement("Slot", new XAttribute("optional", slot.Optional), slot.Name.BestAnalysisAlternative.Text));
					}
					break;
			}

			msaElem.Add(new XElement("HeadWord", msa.OwnerOfClass<ILexEntry>().HeadWord.Text));

			var glossSB = new StringBuilder();
			if (inflType != null)
			{
				var prepend = inflType.GlossPrepend.BestAnalysisAlternative.Text;
				if (prepend != "***")
				{
					glossSB.Append(prepend);
				}
			}
			var sense = msa.OwnerOfClass<ILexEntry>().SenseWithMsa(msa);
			glossSB.Append(sense == null ? FwUtilsStrings.ksThreeQuestionMarks : sense.Gloss.BestAnalysisAlternative.Text);
			if (inflType != null)
			{
				var append = inflType.GlossAppend.BestAnalysisAlternative.Text;
				if (append != "***")
				{
					glossSB.Append(append);
				}
			}
			msaElem.Add(new XElement("Gloss", glossSB.ToString()));
			return msaElem;
		}

		private static string ProcessParseException(Exception e)
		{
			if (e is InvalidShapeException ise)
			{
				var phonemesFoundSoFar = ise.String.Substring(0, ise.Position);
				var rest = ise.String.Substring(ise.Position);
				if (Icu.Character.GetCharType(rest[0]) == Icu.Character.UCharCategory.NON_SPACING_MARK)
				{
					// the first character is a diacritic, combining type of character
					// insert a space so it does not show on top of a single quote in the message string
					rest = $" {rest}";
				}
				return string.Format(ParserCoreStrings.ksHCInvalidWordform, ise.String, ise.Position + 1, rest, phonemesFoundSoFar);
			}

			return string.Format(ParserCoreStrings.ksHCDefaultErrorMsg, e.Message);
		}

		private sealed class MorphInfo
		{
			internal IMoForm Form { get; set; }
			internal string String { get; set; }
			internal IMoMorphSynAnalysis Msa { get; set; }
			internal ILexEntryInflType InflType { get; set; }
			internal bool IsCircumfix { get; set; }
		}

		private sealed class XmlHCLoadErrorLogger : IHCLoadErrorLogger
		{
			private readonly XmlWriter m_xmlWriter;

			internal XmlHCLoadErrorLogger(XmlWriter xmlWriter)
			{
				m_xmlWriter = xmlWriter;
			}

			void IHCLoadErrorLogger.InvalidShape(string str, int errorPos, IMoMorphSynAnalysis msa)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-shape");
				m_xmlWriter.WriteElementString("Form", str);
				m_xmlWriter.WriteElementString("Position", errorPos.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteElementString("Hvo", msa.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}

			void IHCLoadErrorLogger.InvalidAffixProcess(IMoAffixProcess affixProcess, bool isInvalidLhs, IMoMorphSynAnalysis msa)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-affix-process");
				m_xmlWriter.WriteElementString("Form", affixProcess.Form.BestVernacularAlternative.Text);
				m_xmlWriter.WriteElementString("InvalidLhs", isInvalidLhs.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteElementString("Hvo", msa.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}

			void IHCLoadErrorLogger.InvalidPhoneme(IPhPhoneme phoneme)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-phoneme");
				m_xmlWriter.WriteElementString("Name", phoneme.ShortName);
				m_xmlWriter.WriteElementString("Hvo", phoneme.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}

			void IHCLoadErrorLogger.DuplicateGrapheme(IPhPhoneme phoneme)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "duplicate-grapheme");
				m_xmlWriter.WriteElementString("Name", phoneme.ShortName);
				m_xmlWriter.WriteElementString("Hvo", phoneme.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteEndElement();
			}

			void IHCLoadErrorLogger.InvalidEnvironment(IMoForm form, IPhEnvironment env, string reason, IMoMorphSynAnalysis msa)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-environment");
				m_xmlWriter.WriteElementString("Form", form.Form.VernacularDefaultWritingSystem.Text);
				m_xmlWriter.WriteElementString("Env", env.StringRepresentation.Text);
				m_xmlWriter.WriteElementString("Hvo", msa.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteElementString("Reason", reason);
				m_xmlWriter.WriteEndElement();
			}

			void IHCLoadErrorLogger.InvalidReduplicationForm(IMoForm form, string reason, IMoMorphSynAnalysis msa)
			{
				m_xmlWriter.WriteStartElement("LoadError");
				m_xmlWriter.WriteAttributeString("type", "invalid-redup-form");
				m_xmlWriter.WriteElementString("Form", form.Form.VernacularDefaultWritingSystem.Text);
				m_xmlWriter.WriteElementString("Hvo", msa.Hvo.ToString(CultureInfo.InvariantCulture));
				m_xmlWriter.WriteElementString("Reason", reason);
				m_xmlWriter.WriteEndElement();
			}
		}

		private sealed class FwXmlTraceManager : ITraceManager
		{
			private readonly ILcmServiceLocator _serviceLocator;

			internal FwXmlTraceManager(ILcmServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			#region ITraceManager implementation
			bool ITraceManager.IsTracing { get; set; }

			void ITraceManager.AnalyzeWord(Language lang, Word input)
			{
				input.CurrentTrace = new XElement("WordAnalysisTrace", new XElement("InputWord", input.Shape.ToString(lang.SurfaceStratum.CharacterDefinitionTable, true)));
			}

			void ITraceManager.BeginUnapplyStratum(Stratum stratum, Word input)
			{
			}

			void ITraceManager.EndUnapplyStratum(Stratum stratum, Word output)
			{
			}

			void ITraceManager.PhonologicalRuleUnapplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
			{
				((XElement)output.CurrentTrace).Add(new XElement("PhonologicalRuleAnalysisTrace",
					CreateHCRuleElement("PhonologicalRule", rule),
					CreateWordElement("Input", input, true),
					CreateWordElement("Output", output, true)));
			}

			void ITraceManager.PhonologicalRuleNotUnapplied(IPhonologicalRule rule, int subruleIndex, Word input)
			{
				((XElement)input.CurrentTrace).Add(new XElement("PhonologicalRuleAnalysisTrace",
					CreateHCRuleElement("PhonologicalRule", rule),
					CreateWordElement("Input", input, true),
					CreateWordElement("Output", input, true)));
			}

			void ITraceManager.BeginUnapplyTemplate(AffixTemplate template, Word input)
			{
				((XElement)input.CurrentTrace).Add(new XElement("TemplateAnalysisTraceIn",
					CreateHCRuleElement("AffixTemplate", template),
					CreateWordElement("Input", input, true)));
			}

			void ITraceManager.EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied)
			{
				((XElement)output.CurrentTrace).Add(new XElement("TemplateAnalysisTraceOut",
					CreateHCRuleElement("AffixTemplate", template),
					CreateWordElement("Output", unapplied ? output : null, true)));
			}

			void ITraceManager.MorphologicalRuleUnapplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
			{
				var trace = new XElement("MorphologicalRuleAnalysisTrace", CreateMorphologicalRuleElement(rule));
				if (rule is AffixProcessRule aprule)
				{
					trace.Add(CreateAllomorphElement(aprule.Allomorphs[subruleIndex]));
				}
				trace.Add(CreateWordElement("Output", output, true));
				((XElement)output.CurrentTrace).Add(trace);
				output.CurrentTrace = trace;
			}

			void ITraceManager.MorphologicalRuleNotUnapplied(IMorphologicalRule rule, int subruleIndex, Word input)
			{
			}

			void ITraceManager.LexicalLookup(Stratum stratum, Word input)
			{
				var trace = new XElement("LexLookupTrace",
					new XElement("Stratum", stratum.Name),
					new XElement("Shape", input.Shape.ToRegexString(stratum.CharacterDefinitionTable, true)));
				((XElement)input.CurrentTrace).Add(trace);
			}

			void ITraceManager.SynthesizeWord(Language lang, Word input)
			{
				var trace = new XElement("WordSynthesisTrace",
					CreateAllomorphElement(input.RootAllomorph),
					new XElement("MorphologicalRules", input.Stratum.MorphologicalRules.Select(CreateMorphologicalRuleElement)));
				var curTrace = (XElement)input.CurrentTrace;
				var lookupTrace = (XElement)curTrace.LastNode;
				lookupTrace.Add(trace);
				input.CurrentTrace = trace;
			}

			void ITraceManager.BeginApplyStratum(Stratum stratum, Word input)
			{
			}

			void ITraceManager.NonFinalTemplateAppliedLast(Stratum stratum, Word word)
			{
				((XElement)word.CurrentTrace).Add(new XElement("FailureReason", new XAttribute("type", "nonFinalTemplate")));
			}

			void ITraceManager.ApplicableTemplatesNotApplied(Stratum stratum, Word word)
			{
				((XElement)word.CurrentTrace).Add(new XElement("FailureReason", new XAttribute("type", "noTemplatesApplied")));
			}

			void ITraceManager.EndApplyStratum(Stratum stratum, Word output)
			{
			}

			void ITraceManager.PhonologicalRuleApplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
			{
				((XElement)output.CurrentTrace).Add(new XElement("PhonologicalRuleSynthesisTrace",
					CreateHCRuleElement("PhonologicalRule", rule),
					CreateWordElement("Input", input, false),
					CreateWordElement("Output", output, false)));
			}

			void ITraceManager.PhonologicalRuleNotApplied(IPhonologicalRule rule, int subruleIndex, Word input, FailureReason reason, object failureObj)
			{
				var pruleTrace = new XElement("PhonologicalRuleSynthesisTrace",
					CreateHCRuleElement("PhonologicalRule", rule),
					CreateWordElement("Input", input, false),
					CreateWordElement("Output", input, false));

				if (rule is RewriteRule rewriteRule)
				{
					var sr = rewriteRule.Subrules[subruleIndex];
					switch (reason)
					{
						case FailureReason.RequiredSyntacticFeatureStruct:
							pruleTrace.Add(new XElement("FailureReason", new XAttribute("type", "category"),
								new XElement("Category", input.SyntacticFeatureStruct.PartsOfSpeech().FirstOrDefault()),
								new XElement("RequiredCategories", sr.RequiredSyntacticFeatureStruct.PartsOfSpeech()
									.Select(pos => new XElement("Category", pos)))));
							break;

						case FailureReason.RequiredMprFeatures:
							pruleTrace.Add(CreateMprFeaturesFailureElement(true, (MprFeatureGroup)failureObj, sr.RequiredMprFeatures, input));
							break;

						case FailureReason.ExcludedMprFeatures:
							pruleTrace.Add(CreateMprFeaturesFailureElement(false, (MprFeatureGroup)failureObj, sr.ExcludedMprFeatures, input));
							break;
					}
				}

				((XElement)input.CurrentTrace).Add(pruleTrace);
			}

			void ITraceManager.BeginApplyTemplate(AffixTemplate template, Word input)
			{
				((XElement)input.CurrentTrace).Add(new XElement("TemplateSynthesisTraceIn",
					CreateHCRuleElement("AffixTemplate", template),
					CreateWordElement("Input", input, false)));
			}

			void ITraceManager.EndApplyTemplate(AffixTemplate template, Word output, bool applied)
			{
				((XElement)output.CurrentTrace).Add(new XElement("TemplateSynthesisTraceOut",
					CreateHCRuleElement("AffixTemplate", template),
					CreateWordElement("Output", applied ? output : null, false)));
			}

			void ITraceManager.MorphologicalRuleApplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
			{
				var trace = new XElement("MorphologicalRuleSynthesisTrace", CreateMorphologicalRuleElement(rule));
				if (rule is AffixProcessRule aprule)
				{
					trace.Add(CreateAllomorphElement(aprule.Allomorphs[subruleIndex]));
				}
				trace.Add(CreateWordElement("Output", output, false));
				((XElement)output.CurrentTrace).Add(trace);
				output.CurrentTrace = trace;
			}

			void ITraceManager.MorphologicalRuleNotApplied(IMorphologicalRule rule, int subruleIndex, Word input, FailureReason reason, object failureObj)
			{
				var trace = new XElement("MorphologicalRuleSynthesisTrace", CreateMorphologicalRuleElement(rule));
				var aprule = rule as AffixProcessRule;
				if (aprule != null)
				{
					trace.Add(CreateAllomorphElement(subruleIndex == -1 ? aprule.Allomorphs.Last() : aprule.Allomorphs[subruleIndex]));
				}
				trace.Add(new XElement("Output", "*None*"));
				switch (reason)
				{
					case FailureReason.RequiredSyntacticFeatureStruct:
						Debug.Assert(aprule != null);
						var requiredFS = (FeatureStruct)failureObj;
						var requiredPos = requiredFS.PartsOfSpeech().ToArray();
						var inputPos = input.SyntacticFeatureStruct.PartsOfSpeech().ToArray();
						if (requiredPos.Intersect(inputPos).Any())
						{
							trace.Add(new XElement("FailureReason", new XAttribute("type", "inflFeats"),
								CreateInflFeaturesElement("InflFeatures", input.SyntacticFeatureStruct),
								CreateInflFeaturesElement("RequiredInflFeatures", requiredFS)));
						}
						else
						{
							trace.Add(new XElement("FailureReason", new XAttribute("type", "pos"),
								new XElement("Pos", string.Join(", ", inputPos.Select(s => s.Description))),
								new XElement("RequiredPos", string.Join(", ", requiredPos.Select(s => s.Description)))));
						}
						break;

					case FailureReason.RequiredStemName:
						trace.Add(new XElement("FailureReason", new XAttribute("type", "fromStemName"), new XElement("StemName", failureObj)));
						break;

					case FailureReason.RequiredMprFeatures:
						Debug.Assert(aprule != null);
						var group = (MprFeatureGroup)failureObj;
						trace.Add(group.Name == "lexEntryInflTypes" ? new XElement("FailureReason", new XAttribute("type", "requiredInflType"))
							: CreateMprFeaturesFailureElement(true, group, aprule.Allomorphs[subruleIndex].RequiredMprFeatures, input));
						break;

					case FailureReason.ExcludedMprFeatures:
						trace.Add(new XElement("FailureReason", new XAttribute("type", "excludedInflType")));
						break;

					case FailureReason.Pattern:
						Debug.Assert(aprule != null);
						var env = (string)aprule.Allomorphs[subruleIndex].Properties["Env"];
						var prefixEnv = (string)aprule.Allomorphs[subruleIndex].Properties["PrefixEnv"];
						var suffixEnv = (string)aprule.Allomorphs[subruleIndex].Properties["SuffixEnv"];
						if (env != null || prefixEnv != null || suffixEnv != null)
						{
							var reasonElem = new XElement("FailureReason", new XAttribute("type", "environment"));
							if (env != null)
							{
								reasonElem.Add(new XElement("Environment", env));
							}
							if (prefixEnv != null)
							{
								reasonElem.Add(new XElement("Environment", env));
							}
							if (suffixEnv != null)
							{
								reasonElem.Add(new XElement("Environment", env));
							}
							trace.Add(reasonElem);
						}
						else
						{
							trace.Add(new XElement("FailureReason", new XAttribute("type", "affixProcess")));
						}
						break;

					case FailureReason.MaxApplicationCount:
						trace.Add(new XElement("FailureReason", new XAttribute("type", "maxAppCount")));
						break;

					case FailureReason.NonPartialRuleProhibitedAfterFinalTemplate:
						trace.Add(new XElement("FailureReason", new XAttribute("type", "nonPartialRuleAfterFinalTemplate")));
						break;

					case FailureReason.NonPartialRuleRequiredAfterNonFinalTemplate:
						trace.Add(new XElement("FailureReason", new XAttribute("type", "partialRuleAfterNonFinalTemplate")));
						break;

					default:
						return;
				}
				((XElement)input.CurrentTrace).Add(trace);
			}

			void ITraceManager.Blocked(IHCRule rule, Word output)
			{
			}

			void ITraceManager.Successful(Language lang, Word output)
			{
				((XElement)output.CurrentTrace).Add(new XElement("ParseCompleteTrace", new XAttribute("success", true),
					CreateWordElement("Result", output, false)));
			}

			void ITraceManager.Failed(Language lang, Word word, FailureReason reason, Allomorph allomorph, object failureObj)
			{
				XElement trace;
				switch (reason)
				{
					case FailureReason.AllomorphCoOccurrenceRules:
						var alloRule = (AllomorphCoOccurrenceRule)failureObj;
						trace = CreateParseCompleteElement(word,
							new XElement("FailureReason", new XAttribute("type", "adhocProhibitionRule"),
								new XElement("RuleType", "Allomorph"),
								CreateAllomorphElement(allomorph),
								new XElement("Others", alloRule.Others.Select(CreateAllomorphElement)),
								new XElement("Adjacency", alloRule.Adjacency)));
						break;

					case FailureReason.MorphemeCoOccurrenceRules:
						var morphemeRule = (MorphemeCoOccurrenceRule)failureObj;
						trace = CreateParseCompleteElement(word,
							new XElement("FailureReason", new XAttribute("type", "adhocProhibitionRule"),
								new XElement("RuleType", "Morpheme"),
								CreateMorphemeElement(allomorph.Morpheme),
								new XElement("Others", morphemeRule.Others.Select(CreateMorphemeElement)),
								new XElement("Adjacency", morphemeRule.Adjacency)));
						break;

					case FailureReason.Environments:
						trace = CreateParseCompleteElement(word,
							new XElement("FailureReason", new XAttribute("type", "environment"),
								CreateAllomorphElement(allomorph),
								new XElement("Environment", failureObj)));
						break;

					case FailureReason.SurfaceFormMismatch:
						trace = CreateParseCompleteElement(word, new XElement("FailureReason", new XAttribute("type", "formMismatch")));
						break;

					case FailureReason.RequiredSyntacticFeatureStruct:
						trace = CreateParseCompleteElement(word,
							new XElement("FailureReason", new XAttribute("type", "affixInflFeats"),
								CreateAllomorphElement(allomorph),
								CreateInflFeaturesElement("InflFeatures", word.SyntacticFeatureStruct),
								CreateInflFeaturesElement("RequiredInflFeatures", (FeatureStruct)failureObj)));
						break;

					case FailureReason.RequiredStemName:
						trace = CreateParseCompleteElement(word,
							new XElement("FailureReason", new XAttribute("type", "requiredStemName"),
								CreateAllomorphElement(allomorph),
								new XElement("StemName", failureObj)));
						break;

					case FailureReason.ExcludedStemName:
						trace = CreateParseCompleteElement(word,
							new XElement("FailureReason", new XAttribute("type", "excludedStemName"),
								CreateAllomorphElement(allomorph),
								new XElement("StemName", failureObj)));
						break;

					case FailureReason.BoundRoot:
						trace = CreateParseCompleteElement(word, new XElement("FailureReason", new XAttribute("type", "boundStem")));
						break;

					case FailureReason.DisjunctiveAllomorph:
						trace = CreateParseCompleteElement(word,
							new XElement("FailureReason", new XAttribute("type", "disjunctiveAllomorph"),
								CreateWordElement("Word", (Word)failureObj, false)));
						break;

					case FailureReason.PartialParse:
						trace = CreateParseCompleteElement(word, new XElement("FailureReason", new XAttribute("type", "partialParse")));
						break;

					default:
						return;
				}
				((XElement)word.CurrentTrace).Add(trace);
			}
			#endregion

			private static XElement CreateMprFeaturesFailureElement(bool required, MprFeatureGroup group, MprFeatureSet feats, Word input)
			{
				return new XElement("FailureReason", new XAttribute("type", "mprFeatures"),
					new XElement("MatchType", required ? "required" : "excluded"),
					new XElement("Group", group),
					new XElement("MprFeatures", input.MprFeatures.Where(mf => mf.Group == group).Select(f => new XElement("MprFeature", f))),
					new XElement("ConstrainingMprFeatrues", feats.Where(mf => mf.Group == group).Select(f => new XElement("MprFeature", f))));
			}

			private static XElement CreateParseCompleteElement(Word word, XElement reasonElem)
			{
				return new XElement("ParseCompleteTrace", new XAttribute("success", false),
					CreateWordElement("Result", word, false),
					reasonElem);
			}

			private static XElement CreateInflFeaturesElement(string name, FeatureStruct fs)
			{
				return new XElement(name, fs.Head().ToString().Replace(",", ""));
			}

			private static XElement CreateWordElement(string name, Word word, bool analysis)
			{
				var wordStr = word == null ? "*None*"
					: analysis ? word.Shape.ToRegexString(word.Stratum.CharacterDefinitionTable, true)
						: word.Shape.ToString(word.Stratum.CharacterDefinitionTable, true);
				return new XElement(name, wordStr);
			}

			private XElement CreateMorphemeElement(Morpheme morpheme)
			{
				var msaID = (int?)morpheme.Properties["ID"] ?? 0;
				if (msaID == 0 || !_serviceLocator.GetInstance<IMoMorphSynAnalysisRepository>().TryGetObject(msaID, out var msa))
				{
					return null;
				}
				var inflTypeID = (int?)morpheme.Properties["InflTypeID"] ?? 0;
				ILexEntryInflType inflType = null;
				return inflTypeID != 0 && !_serviceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(inflTypeID, out inflType)
					? null : HCParser.CreateMorphemeElement(msa, inflType);
			}

			private static XElement CreateMorphologicalRuleElement(IMorphologicalRule rule)
			{
				var elem = CreateHCRuleElement("MorphologicalRule", rule);
				elem.Add(new XAttribute("type", rule is AffixProcessRule ? "affix" : "compound"));
				return elem;
			}

			private static XElement CreateHCRuleElement(string name, IHCRule rule)
			{
				var id = 0;
				if (rule is Morpheme morpheme)
				{
					id = (int?)morpheme.Properties["ID"] ?? 0;
				}
				return new XElement(name, new XAttribute("id", id), rule.Name);
			}

			private XElement CreateAllomorphElement(Allomorph allomorph)
			{
				var isNull = (bool?)allomorph.Properties["IsNull"] ?? false;
				if (isNull)
				{
					var slotID = (int)allomorph.Morpheme.Properties["SlotID"];
					if (!_serviceLocator.GetInstance<IMoInflAffixSlotRepository>().TryGetObject(slotID, out var slot))
					{
						return null;
					}
					var nullInflTypeID = (int)allomorph.Morpheme.Properties["InflTypeID"];
					if (!_serviceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(nullInflTypeID, out var nullInflType))
					{
						return null;
					}
					var isPrefix = (bool)allomorph.Properties["IsPrefix"];
					return new XElement("Allomorph", new XAttribute("id", 0), new XAttribute("type", isPrefix ? MoMorphTypeTags.kMorphPrefix : MoMorphTypeTags.kMorphSuffix),
						new XElement("Form", "^0"),
						new XElement("Morpheme", new XAttribute("id", 0), new XAttribute("type", "infl"),
							new XElement("HeadWord", $"Automatically generated null affix for the {nullInflType.Name.BestAnalysisAlternative.Text} irregularly inflected form"),
							new XElement("Gloss", (nullInflType.GlossPrepend.BestAnalysisAlternative.Text == "***" ? "" : nullInflType.GlossPrepend.BestAnalysisAlternative.Text)
								+ (nullInflType.GlossAppend.BestAnalysisAlternative.Text == "***" ? "" : nullInflType.GlossAppend.BestAnalysisAlternative.Text)),
							new XElement("Category", slot.OwnerOfClass<IPartOfSpeech>().Abbreviation.BestAnalysisAlternative.Text),
							new XElement("Slot", new XAttribute("optional", slot.Optional), slot.Name.BestAnalysisAlternative.Text)));
				}

				var formID = (int?)allomorph.Properties["ID"] ?? 0;
				if (formID == 0 || !_serviceLocator.GetInstance<IMoFormRepository>().TryGetObject(formID, out var form))
				{
					return null;
				}
				var formID2 = (int?)allomorph.Properties["ID2"] ?? 0;
				var msaID = (int)allomorph.Morpheme.Properties["ID"];
				if (!_serviceLocator.GetInstance<IMoMorphSynAnalysisRepository>().TryGetObject(msaID, out var msa))
				{
					return null;
				}
				var inflTypeID = (int?)allomorph.Morpheme.Properties["InflTypeID"] ?? 0;
				ILexEntryInflType inflType = null;
				if (inflTypeID != 0 && !_serviceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(inflTypeID, out inflType))
				{
					return null;
				}
				return HCParser.CreateAllomorphElement("Allomorph", form, msa, inflType, formID2 != 0);
			}

			object ITraceManager.GenerateWords(Language lang)
			{
				throw new NotImplementedException();
			}
		}
	}
}