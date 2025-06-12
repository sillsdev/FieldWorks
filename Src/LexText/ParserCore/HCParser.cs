// Copyright (c) 2014-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
		private Morpher m_morpher;
		private Language m_language;
		private readonly FwXmlTraceManager m_traceManager;
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
			m_traceManager = new FwXmlTraceManager(m_cache);
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

			if (m_morpher == null)
				return null;

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
				foreach (Word wordAnalysis in wordAnalyses)
				{
					List<MorphInfo> morphs;
					if (GetMorphs(wordAnalysis, out morphs))
					{
						analyses.Add(new ParseAnalysis(morphs.Select(mi =>
							new ParseMorph(mi.Form, mi.Msa, mi.InflType))));
					}
				}
				result = new ParseResult(analyses);
			}

			return result;
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
		}

		private void LoadParser()
		{
			m_morpher = null;

			int delReapps = 0;
			string loadErrorsFile = Path.Combine(m_outputDirectory, m_cache.ProjectId.Name + "HCLoadErrors.xml");
			using (XmlWriter writer = XmlWriter.Create(loadErrorsFile))
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				writer.WriteStartElement("LoadErrors");
				m_language = HCLoader.Load(m_cache, new XmlHCLoadErrorLogger(writer));
				writer.WriteEndElement();
				XElement parserParamsElem = XElement.Parse(m_cache.LanguageProject.MorphologicalDataOA.ParserParameters);
				XElement delReappsElem = parserParamsElem.Elements("ParserParameters").Elements("HC").Elements("DelReapps").FirstOrDefault();
				if (delReappsElem != null)
					delReapps = (int) delReappsElem;
			}
			m_morpher = new Morpher(m_traceManager, m_language) { DeletionReapplications = delReapps };
		}

		private XDocument ParseToXml(string form, bool tracing, IEnumerable<int> selectTraceMorphs)
		{
			if (m_morpher == null)
				return null;

			var doc = new XDocument();
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				if (selectTraceMorphs != null)
				{
					var selectTraceMorphsSet = new HashSet<int>(selectTraceMorphs);
					m_morpher.LexEntrySelector = entry => selectTraceMorphsSet.Contains((int) entry.Properties[MsaID]);
					m_morpher.RuleSelector = rule =>
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
					m_morpher.LexEntrySelector = entry => true;
					m_morpher.RuleSelector = rule => true;
				}
				m_morpher.TraceManager.IsTracing = tracing;
				var wordformElem = new XElement("Wordform", new XAttribute("form", form));
				try
				{
					object trace;
					foreach (Word wordAnalysis in m_morpher.ParseWord(form, out trace))
					{
						List<MorphInfo> morphs;
						if (GetMorphs(wordAnalysis, out morphs))
							wordformElem.Add(new XElement("Analysis", morphs.Select(mi => CreateAllomorphElement("Morph", mi.Form, mi.Msa, mi.InflType, mi.IsCircumfix))));
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
					morphInfo.String += formStr;
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
						String = formStr,
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
			Guid morphTypeGuid = circumfix ? MoMorphTypeTags.kguidMorphCircumfix : (form.MorphTypeRA == null ? Guid.Empty : form.MorphTypeRA.Guid);
			var elem = new XElement(name, new XAttribute("id", form.Hvo), new XAttribute("type", GetMorphTypeString(morphTypeGuid)),
				new XElement("Form", circumfix ? form.OwnerOfClass<ILexEntry>().HeadWord.Text : form.GetFormWithMarkers(form.Cache.DefaultVernWs)),
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
			public string String { get; set; }
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
		}
	}
}
