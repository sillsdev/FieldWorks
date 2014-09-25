// Copyright (c) 2014-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.HermitCrab;
using SIL.Machine.Annotations;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class HCParser : FwDisposableBase, IParser
	{
		private readonly FdoCache m_cache;
		private Morpher m_morpher;
		private Language m_language;
		private readonly SpanFactory<ShapeNode> m_spanFactory;
		private readonly FwXmlTraceManager m_traceManager;
		private readonly string m_outputDirectory;
		private ParserModelChangeListener m_changeListener;
		private bool m_forceUpdate;

		public HCParser(FdoCache cache)
		{
			m_cache = cache;
			m_spanFactory = new ShapeSpanFactory();
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
							new ParseMorph(mi.Form, mi.Msa, mi.LexEntryRef != null ? mi.LexEntryRef.VariantEntryTypesRS[0] as ILexEntryInflType : null))));
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
				m_language = HCLoader.Load(m_spanFactory, m_cache, writer);
				XElement parserParamsElem = XElement.Parse(m_cache.LanguageProject.MorphologicalDataOA.ParserParameters);
				XElement delReappsElem = parserParamsElem.Elements("ParserParameters").Elements("HC").Elements("DelReapps").FirstOrDefault();
				if (delReappsElem != null)
					delReapps = (int) delReappsElem;
			}
			m_morpher = new Morpher(m_spanFactory, m_traceManager, m_language) { DeletionReapplications = delReapps };
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
					m_morpher.LexEntrySelector = entry => selectTraceMorphsSet.Contains((int) entry.Properties["MsaID"]);
					m_morpher.RuleSelector = rule =>
						{
							var mrule = rule as Morpheme;
							if (mrule != null)
								return selectTraceMorphsSet.Contains((int) mrule.Properties["MsaID"]);
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
							wordformElem.Add(new XElement("Analysis", morphs.Select(mi => CreateAllomorphElement("Morph", mi.Form, mi.Msa, mi.IsCircumfix))));
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
					var predictedPhonemes = new HashSet<IPhPhoneme>(m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.SelectMany(ps => ps.PhonemesOC).Where(p => feats.IsSubsetOf(GetFeatures(p))));
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
			result = new List<MorphInfo>();
			foreach (Annotation<ShapeNode> morph in ws.Morphs)
			{
				Allomorph allomorph = ws.GetAllomorph(morph);
				var formID = (int?) allomorph.Properties["ID"] ?? 0;
				if (formID == 0)
					continue;
				var formID2 = (int?) allomorph.Properties["ID2"] ?? 0;
				string formStr = ws.Shape.GetNodes(morph.Span).ToString(ws.Stratum.SymbolTable, false);
				int curFormID;
				MorphInfo morphInfo;
				if (!morphs.TryGetValue(allomorph.Morpheme, out morphInfo))
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

				var msaID = (int) allomorph.Morpheme.Properties["ID"];
				IMoMorphSynAnalysis msa;
				if (!m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().TryGetObject(msaID, out msa))
				{
					result = null;
					return false;
				}

				var lexEntryRefID = (int?) allomorph.Morpheme.Properties["LexEntryRefID"] ?? 0;
				ILexEntryRef lexEntryRef = null;
				if (lexEntryRefID > 0 && !m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().TryGetObject(lexEntryRefID, out lexEntryRef))
				{
					result = null;
					return false;
				}

				morphInfo = new MorphInfo
					{
						Form = form,
						String = formStr,
						Msa = msa,
						LexEntryRef = lexEntryRef,
						IsCircumfix = formID2 > 0
					};

				morphs[allomorph.Morpheme] = morphInfo;

				switch (form.MorphTypeRA.Guid.ToString())
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

		internal static XElement CreateAllomorphElement(string name, IMoForm form, IMoMorphSynAnalysis msa, bool circumfix)
		{
			var elem = new XElement(name, new XAttribute("id", form.Hvo), new XAttribute("type", GetMorphTypeString(circumfix ? MoMorphTypeTags.kguidMorphCircumfix : form.MorphTypeRA.Guid)),
				new XElement("Form", circumfix ? form.OwnerOfClass<ILexEntry>().HeadWord.Text : form.GetFormWithMarkers(form.Cache.DefaultVernWs)),
				new XElement("LongName", form.LongName));
			elem.Add(CreateMorphemeElement(msa));
			return elem;
		}

		internal static XElement CreateMorphemeElement(IMoMorphSynAnalysis msa)
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
			msaElem.Add(new XElement("Gloss", msa.GetGlossOfFirstSense()));
			return msaElem;
		}

		private string ProcessParseException(Exception e)
		{
			var ise = e as InvalidShapeException;
			if (ise != null)
			{
				string phonemesFoundSoFar = ise.String.Substring(0, ise.Position);
				string rest = ise.String.Substring(ise.Position);
				LgGeneralCharCategory cc = m_cache.ServiceLocator.UnicodeCharProps.get_GeneralCategory(rest[0]);
				if (cc == LgGeneralCharCategory.kccMn)
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
			public ILexEntryRef LexEntryRef { get; set; }
			public bool IsCircumfix { get; set; }
		}
		#endregion
	}
}
