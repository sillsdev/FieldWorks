// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SIL.HermitCrab;
using SIL.HermitCrab.MorphologicalRules;
using SIL.HermitCrab.PhonologicalRules;
using SIL.LCModel;
using SIL.Machine.FeatureModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	internal class FwXmlTraceManager : ITraceManager
	{
		private readonly LcmCache m_cache;

		public FwXmlTraceManager(LcmCache lcmCache)
		{
			m_cache = lcmCache;
		}

		public bool IsTracing { get; set; }

		public void AnalyzeWord(Language lang, Word input)
		{
			input.CurrentTrace = new XElement("WordAnalysisTrace", new XElement("InputWord", input.Shape.ToString(lang.SurfaceStratum.CharacterDefinitionTable, true)));
		}

		public void BeginUnapplyStratum(Stratum stratum, Word input)
		{
		}

		public void EndUnapplyStratum(Stratum stratum, Word output)
		{
		}

		public void PhonologicalRuleUnapplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
		{
			((XElement)output.CurrentTrace).Add(new XElement("PhonologicalRuleAnalysisTrace",
				CreateHCRuleElement("PhonologicalRule", rule),
				CreateWordElement("Input", input, true),
				CreateWordElement("Output", output, true)));
		}

		public void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, int subruleIndex, Word input)
		{
			((XElement)input.CurrentTrace).Add(new XElement("PhonologicalRuleAnalysisTrace",
				CreateHCRuleElement("PhonologicalRule", rule),
				CreateWordElement("Input", input, true),
				CreateWordElement("Output", input, true)));
		}

		public void BeginUnapplyTemplate(AffixTemplate template, Word input)
		{
			((XElement)input.CurrentTrace).Add(new XElement("TemplateAnalysisTraceIn",
				CreateHCRuleElement("AffixTemplate", template),
				CreateWordElement("Input", input, true)));
		}

		public void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied)
		{
			((XElement)output.CurrentTrace).Add(new XElement("TemplateAnalysisTraceOut",
				CreateHCRuleElement("AffixTemplate", template),
				CreateWordElement("Output", unapplied ? output : null, true)));
		}

		public void MorphologicalRuleUnapplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
		{
			var trace = new XElement("MorphologicalRuleAnalysisTrace", CreateMorphologicalRuleElement(rule));
			var aprule = rule as AffixProcessRule;
			if (aprule != null)
			{
				trace.Add(CreateAllomorphElement(aprule.Allomorphs[subruleIndex]));
			}
			trace.Add(CreateWordElement("Output", output, true));
			((XElement)output.CurrentTrace).Add(trace);
			output.CurrentTrace = trace;
		}

		public void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, int subruleIndex, Word input)
		{
		}

		public void LexicalLookup(Stratum stratum, Word input)
		{
			var trace = new XElement("LexLookupTrace",
				new XElement("Stratum", stratum.Name),
				new XElement("Shape", input.Shape.ToRegexString(stratum.CharacterDefinitionTable, true)));
			((XElement)input.CurrentTrace).Add(trace);
		}

		public void SynthesizeWord(Language lang, Word input)
		{
			var trace = new XElement("WordSynthesisTrace",
				CreateAllomorphElement(input.RootAllomorph),
				new XElement("MorphologicalRules", input.MorphologicalRules.Select(CreateMorphologicalRuleElement)));
			var curTrace = (XElement)input.CurrentTrace;
			var lookupTrace = (XElement)curTrace.LastNode;
			lookupTrace.Add(trace);
			input.CurrentTrace = trace;
		}

		public void BeginApplyStratum(Stratum stratum, Word input)
		{
		}

		public void NonFinalTemplateAppliedLast(Stratum stratum, Word word)
		{
			((XElement)word.CurrentTrace).Add(new XElement("FailureReason", new XAttribute("type", "nonFinalTemplate")));
		}

		public void ApplicableTemplatesNotApplied(Stratum stratum, Word word)
		{
			((XElement)word.CurrentTrace).Add(new XElement("FailureReason", new XAttribute("type", "noTemplatesApplied")));
		}

		public void EndApplyStratum(Stratum stratum, Word output)
		{
		}

		public void PhonologicalRuleApplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output)
		{
			((XElement)output.CurrentTrace).Add(new XElement("PhonologicalRuleSynthesisTrace",
				CreateHCRuleElement("PhonologicalRule", rule),
				CreateWordElement("Input", input, false),
				CreateWordElement("Output", output, false)));
		}

		public void PhonologicalRuleNotApplied(IPhonologicalRule rule, int subruleIndex, Word input, FailureReason reason, object failureObj)
		{
			var pruleTrace = new XElement("PhonologicalRuleSynthesisTrace",
				CreateHCRuleElement("PhonologicalRule", rule),
				CreateWordElement("Input", input, false),
				CreateWordElement("Output", input, false));

			var rewriteRule = rule as RewriteRule;
			if (rewriteRule != null)
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

		private static XElement CreateMprFeaturesFailureElement(bool required, MprFeatureGroup group, MprFeatureSet feats, Word input)
		{
			return new XElement("FailureReason", new XAttribute("type", "mprFeatures"),
				new XElement("MatchType", required ? "required" : "excluded"),
				new XElement("Group", group),
				new XElement("MprFeatures", input.MprFeatures.Where(mf => mf.Group == group).Select(f => new XElement("MprFeature", f))),
				new XElement("ConstrainingMprFeatrues", feats.Where(mf => mf.Group == group).Select(f => new XElement("MprFeature", f))));
		}

		public void BeginApplyTemplate(AffixTemplate template, Word input)
		{
			((XElement)input.CurrentTrace).Add(new XElement("TemplateSynthesisTraceIn",
				CreateHCRuleElement("AffixTemplate", template),
				CreateWordElement("Input", input, false)));
		}

		public void EndApplyTemplate(AffixTemplate template, Word output, bool applied)
		{
			((XElement)output.CurrentTrace).Add(new XElement("TemplateSynthesisTraceOut",
				CreateHCRuleElement("AffixTemplate", template),
				CreateWordElement("Output", applied ? output : null, false)));
		}

		public void MorphologicalRuleApplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output)
		{
			var trace = new XElement("MorphologicalRuleSynthesisTrace", CreateMorphologicalRuleElement(rule));
			var aprule = rule as AffixProcessRule;
			if (aprule != null)
			{
				trace.Add(CreateAllomorphElement(aprule.Allomorphs[subruleIndex]));
			}
			trace.Add(CreateWordElement("Output", output, false));
			((XElement)output.CurrentTrace).Add(trace);
			output.CurrentTrace = trace;
		}

		public void MorphologicalRuleNotApplied(IMorphologicalRule rule, int subruleIndex, Word input, FailureReason reason, object failureObj)
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
					trace.Add(new XElement("FailureReason", new XAttribute("type", "fromStemName"),
						new XElement("StemName", failureObj)));
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

		public void ParseBlocked(IHCRule rule, Word output)
		{
		}

		public void ParseSuccessful(Language lang, Word output)
		{
			((XElement)output.CurrentTrace).Add(new XElement("ParseCompleteTrace", new XAttribute("success", true),
				CreateWordElement("Result", output, false)));
		}

		public void ParseFailed(Language lang, Word word, FailureReason reason, Allomorph allomorph, object failureObj)
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
			IMoMorphSynAnalysis msa;
			if (msaID == 0 || !m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().TryGetObject(msaID, out msa))
			{
				return null;
			}
			var inflTypeID = (int?)morpheme.Properties["InflTypeID"] ?? 0;
			ILexEntryInflType inflType = null;
			if (inflTypeID != 0 && !m_cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(inflTypeID, out inflType))
			{
				return null;
			}
			return HCParser.CreateMorphemeElement(msa, inflType);
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
			var morpheme = rule as Morpheme;
			if (morpheme != null)
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
				IMoInflAffixSlot slot;
				if (!m_cache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().TryGetObject(slotID, out slot))
				{
					return null;
				}
				var nullInflTypeID = (int)allomorph.Morpheme.Properties["InflTypeID"];
				ILexEntryInflType nullInflType;
				if (!m_cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(nullInflTypeID, out nullInflType))
				{
					return null;
				}
				var isPrefix = (bool)allomorph.Properties["IsPrefix"];
				return new XElement("Allomorph", new XAttribute("id", 0), new XAttribute("type", isPrefix ? MoMorphTypeTags.kMorphPrefix : MoMorphTypeTags.kMorphSuffix),
					new XElement("Form", "^0"),
					new XElement("Morpheme", new XAttribute("id", 0), new XAttribute("type", "infl"),
						new XElement("HeadWord", string.Format("Automatically generated null affix for the {0} irregularly inflected form", nullInflType.Name.BestAnalysisAlternative.Text)),
						new XElement("Gloss", (nullInflType.GlossPrepend.BestAnalysisAlternative.Text == "***" ? "" : nullInflType.GlossPrepend.BestAnalysisAlternative.Text)
							+ (nullInflType.GlossAppend.BestAnalysisAlternative.Text == "***" ? "" : nullInflType.GlossAppend.BestAnalysisAlternative.Text)),
						new XElement("Category", slot.OwnerOfClass<IPartOfSpeech>().Abbreviation.BestAnalysisAlternative.Text),
						new XElement("Slot", new XAttribute("optional", slot.Optional), slot.Name.BestAnalysisAlternative.Text)));
			}

			var formID = (int?)allomorph.Properties["ID"] ?? 0;
			IMoForm form;
			if (formID == 0 || !m_cache.ServiceLocator.GetInstance<IMoFormRepository>().TryGetObject(formID, out form))
			{
				return null;
			}
			var formID2 = (int?)allomorph.Properties["ID2"] ?? 0;
			var msaID = (int)allomorph.Morpheme.Properties["ID"];
			IMoMorphSynAnalysis msa;
			if (!m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().TryGetObject(msaID, out msa))
			{
				return null;
			}
			var inflTypeID = (int?)allomorph.Morpheme.Properties["InflTypeID"] ?? 0;
			ILexEntryInflType inflType = null;
			if (inflTypeID != 0 && !m_cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(inflTypeID, out inflType))
			{
				return null;
			}
			return HCParser.CreateAllomorphElement("Allomorph", form, msa, inflType, formID2 != 0);
		}
	}
}