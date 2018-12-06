// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SIL.Collections;
using SIL.HermitCrab;
using SIL.HermitCrab.MorphologicalRules;
using SIL.HermitCrab.PhonologicalRules;
using SIL.LCModel;
using SIL.LCModel.Core.Phonology;
using SIL.LCModel.Core.WritingSystems;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class HCLoader
	{
		public static Language Load(SpanFactory<ShapeNode> spanFactory, LcmCache cache, IHCLoadErrorLogger logger)
		{
			var loader = new HCLoader(spanFactory, cache, logger);
			loader.LoadLanguage();
			return loader.m_language;
		}

		private static readonly string[] VariableNames =
		{
			"α", "β", "γ", "δ", "ε", "ζ", "η", "θ", "ι", "κ", "λ", "μ", "ν", "ξ",
			"ο", "π", "ρ", "σ", "τ", "υ", "φ", "χ", "ψ", "ω"
		};

		private readonly SpanFactory<ShapeNode> m_spanFactory;
		private readonly LcmCache m_cache;
		private readonly Dictionary<IMoForm, List<Allomorph>> m_allomorphs;
		private readonly Dictionary<IMoMorphSynAnalysis, List<Morpheme>> m_morphemes;
		private readonly Dictionary<IMoStemName, StemName> m_stemNames;
		private readonly Dictionary<ICmObject, MprFeature> m_mprFeatures;

		private Language m_language;
		private CharacterDefinitionTable m_table;
		private Stratum m_morphophonemic;
		private Stratum m_clitic;
		private ComplexFeature m_headFeature;
		private SymbolicFeature m_posFeature;

		private readonly IHCLoadErrorLogger m_logger;
		private readonly PhonEnvRecognizer m_envValidator;
		private readonly Dictionary<string, IPhNaturalClass> m_naturalClassLookup;
		private readonly Dictionary<IPhNaturalClass, NaturalClass> m_naturalClasses;
		private readonly Dictionary<IPhTerminalUnit, CharacterDefinition> m_charDefs;

		private readonly bool m_noDefaultCompounding;
		private readonly bool m_notOnClitics;
		private readonly bool m_acceptUnspecifiedGraphemes;

		private SimpleContext m_any;
		private CharacterDefinition m_null;
		private CharacterDefinition m_morphBdry;

		private HCLoader(SpanFactory<ShapeNode> spanFactory, LcmCache cache, IHCLoadErrorLogger logger)
		{
			m_spanFactory = spanFactory;
			m_cache = cache;
			m_logger = logger;
			m_allomorphs = new Dictionary<IMoForm, List<Allomorph>>();
			m_morphemes = new Dictionary<IMoMorphSynAnalysis, List<Morpheme>>();
			m_stemNames = new Dictionary<IMoStemName, StemName>();
			m_mprFeatures = new Dictionary<ICmObject, MprFeature>();

			m_envValidator = new PhonEnvRecognizer(
				m_cache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(),
				m_cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());

			m_naturalClassLookup = new Dictionary<string, IPhNaturalClass>();
			foreach (var nc in m_cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS)
			{
				m_naturalClassLookup[nc.Abbreviation.BestAnalysisAlternative.Text] = nc;
			}
			var parserParamsElem = XElement.Parse(m_cache.LanguageProject.MorphologicalDataOA.ParserParameters);
			var hcElem = parserParamsElem.Element("HC");
			m_noDefaultCompounding = hcElem != null && ((bool?)hcElem.Element("NoDefaultCompounding") ?? false);
			m_notOnClitics = hcElem == null || ((bool?)hcElem.Element("NotOnClitics") ?? true);
			m_acceptUnspecifiedGraphemes = hcElem != null && ((bool?)hcElem.Element("AcceptUnspecifiedGraphemes") ?? false);

			m_naturalClasses = new Dictionary<IPhNaturalClass, NaturalClass>();
			m_charDefs = new Dictionary<IPhTerminalUnit, CharacterDefinition>();
		}

		private void LoadLanguage()
		{
			m_language = new Language { Name = m_cache.ProjectId.Name };

			var inflClassesGroup = new MprFeatureGroup { Name = "inflClasses", MatchType = MprFeatureGroupMatchType.Any };
			var posSymbols = new List<FeatureSymbol>();
			foreach (var pos in m_cache.LanguageProject.AllPartsOfSpeech)
			{
				posSymbols.Add(new FeatureSymbol("pos" + pos.Hvo) { Description = pos.Abbreviation.BestAnalysisAlternative.Text });
				foreach (var inflClass in pos.InflectionClassesOC)
				{
					LoadInflClassMprFeature(inflClass, inflClassesGroup);
				}
			}
			if (inflClassesGroup.MprFeatures.Count > 0)
			{
				m_language.MprFeatureGroups.Add(inflClassesGroup);
			}
			var prodRestrictsGroup = new MprFeatureGroup { Name = "exceptionFeatures", MatchType = MprFeatureGroupMatchType.All };
			foreach (var prodRestrict in m_cache.LanguageProject.MorphologicalDataOA.ProdRestrictOA.ReallyReallyAllPossibilities)
			{
				LoadMprFeature(prodRestrict, prodRestrictsGroup);
			}
			if (prodRestrictsGroup.MprFeatures.Count > 0)
			{
				m_language.MprFeatureGroups.Add(prodRestrictsGroup);
			}
			var lexEntryInflTypesGroup = new MprFeatureGroup { Name = "lexEntryInflTypes", MatchType = MprFeatureGroupMatchType.All };
			foreach (var inflType in m_cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().AllInstances())
			{
				LoadMprFeature(inflType, lexEntryInflTypesGroup);
			}
			if (lexEntryInflTypesGroup.MprFeatures.Count > 0)
			{
				m_language.MprFeatureGroups.Add(lexEntryInflTypesGroup);
			}
			m_posFeature = m_language.SyntacticFeatureSystem.AddPartsOfSpeech(posSymbols);
			m_headFeature = m_language.SyntacticFeatureSystem.AddHeadFeature();
			LoadFeatureSystem(m_cache.LanguageProject.MsFeatureSystemOA, m_language.SyntacticFeatureSystem);

			LoadFeatureSystem(m_cache.LanguageProject.PhFeatureSystemOA, m_language.PhonologicalFeatureSystem);

			var anyNC = new NaturalClass(FeatureStruct.New().Value) { Name = "Any" };
			m_language.NaturalClasses.Add(anyNC);
			m_any = new SimpleContext(anyNC, Enumerable.Empty<SymbolicFeatureValue>());

			LoadCharacterDefinitionTable(m_cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0]);

			foreach (var stemName in m_cache.ServiceLocator.GetInstance<IMoStemNameRepository>().AllInstances())
			{
				var pos = stemName.OwnerOfClass<IPartOfSpeech>();
				var regions = new List<FeatureStruct>();
				foreach (var fs in stemName.RegionsOC.Where(fs => !fs.IsEmpty))
				{
					var hcFS = new FeatureStruct();
					hcFS.AddValue(m_headFeature, LoadFeatureStruct(fs, m_language.SyntacticFeatureSystem));
					hcFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(pos));
					hcFS.Freeze();
					regions.Add(hcFS);
				}
				if (regions.Count > 0)
				{
					var hcStemName = new StemName(regions) { Name = stemName.Name.BestAnalysisAlternative.Text };
					m_stemNames[stemName] = hcStemName;
					m_language.StemNames.Add(hcStemName);
				}
			}

			m_morphophonemic = new Stratum(m_table) { Name = "Morphophonemic", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };
			m_language.Strata.Add(m_morphophonemic);

			m_clitic = new Stratum(m_table) { Name = "Clitic", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };
			m_language.Strata.Add(m_clitic);

			m_language.Strata.Add(new Stratum(m_table) { Name = "Surface" });

			if (m_cache.LanguageProject.MorphologicalDataOA.CompoundRulesOS.Count == 0 && !m_noDefaultCompounding)
			{
				m_morphophonemic.MorphologicalRules.AddRange(DefaultCompoundingRules());
			}
			else
			{
				foreach (var compoundRule in m_cache.LanguageProject.MorphologicalDataOA.CompoundRulesOS.Where(r => !r.Disabled))
				{
					switch (compoundRule.ClassID)
					{
						case MoEndoCompoundTags.kClassId:
							m_morphophonemic.MorphologicalRules.Add(LoadEndoCompoundingRule((IMoEndoCompound)compoundRule));
							break;

						case MoExoCompoundTags.kClassId:
							m_morphophonemic.MorphologicalRules.AddRange(LoadExoCompoundingRule((IMoExoCompound)compoundRule));
							break;
					}
				}
			}

			foreach (var entry in m_cache.LanguageProject.LexDbOA.Entries)
			{
				var stemAllos = new List<IMoStemAllomorph>();
				var cliticStemAllos = new List<IMoStemAllomorph>();
				var affixAllos = new List<IMoForm>();
				var cliticAffixAllos = new List<IMoForm>();

				foreach (var form in entry.AlternateFormsOS.Concat(entry.LexemeFormOA))
				{
					if (form == null)
					{
						continue;
					}
					if (IsValidLexEntryForm(form))
					{
						if (IsCliticType(form.MorphTypeRA))
						{
							cliticStemAllos.Add((IMoStemAllomorph)form);
						}
						else
						{
							stemAllos.Add((IMoStemAllomorph)form);
						}
					}
					if (IsValidRuleForm(form))
					{
						if (IsCliticType(form.MorphTypeRA))
						{
							cliticAffixAllos.Add(form);
						}
						else
						{
							affixAllos.Add(form);
						}
					}
				}
				if (stemAllos.Count > 0)
				{
					LoadLexEntries(m_morphophonemic, entry, stemAllos);
				}
				if (cliticStemAllos.Count > 0)
				{
					LoadLexEntries(m_clitic, entry, cliticStemAllos);
				}
				if (affixAllos.Count > 0)
				{
					LoadMorphologicalRules(m_morphophonemic, entry, affixAllos);
				}
				if (cliticAffixAllos.Count > 0)
				{
					LoadMorphologicalRules(m_clitic, entry, cliticAffixAllos);
				}
			}

			foreach (var template in m_cache.ServiceLocator.GetInstance<IMoInflAffixTemplateRepository>().AllInstances().Where(t => !t.Disabled))
			{
				var slots = template.SuffixSlotsRS.Concat(template.PrefixSlotsRS.Reverse()).Where(s => s.Affixes.Any(msa => m_morphemes.ContainsKey(msa))).ToArray();
				if (slots.Length > 0)
				{
					m_morphophonemic.AffixTemplates.Add(LoadAffixTemplate(template, slots));
				}
			}

			foreach (var prule in m_cache.LanguageProject.PhonologicalDataOA.PhonRulesOS.Where(r => !r.Disabled).OrderBy(r => r.OrderNumber))
			{
				switch (prule.ClassID)
				{
					case PhRegularRuleTags.kClassId:
						var regRule = (IPhRegularRule)prule;
						if (regRule.StrucDescOS.Count > 0 || regRule.RightHandSidesOS.Any(rhs => rhs.StrucChangeOS.Count > 0))
						{
							var hcRegRule = LoadRewriteRule(regRule);
							m_morphophonemic.PhonologicalRules.Add(hcRegRule);
							if (!m_notOnClitics)
							{
								m_clitic.PhonologicalRules.Add(hcRegRule);
							}
							m_language.PhonologicalRules.Add(hcRegRule);
						}
						break;

					case PhMetathesisRuleTags.kClassId:
						var metaRule = (IPhMetathesisRule)prule;
						if (metaRule.LeftSwitchIndex != -1 && metaRule.RightSwitchIndex != -1)
						{
							var hcMetaRule = LoadMetathesisRule(metaRule);
							m_morphophonemic.PhonologicalRules.Add(hcMetaRule);
							if (!m_notOnClitics)
							{
								m_clitic.PhonologicalRules.Add(hcMetaRule);
							}
							m_language.PhonologicalRules.Add(hcMetaRule);
						}
						break;
				}
			}

			m_language.NaturalClasses.AddRange(m_naturalClasses.Values.Where(nc => nc != null));

			foreach (var alloAdhocProhib in m_cache.ServiceLocator.GetInstance<IMoAlloAdhocProhibRepository>().AllInstances()
				.Where(a => !a.Disabled && a.FirstAllomorphRA != null && a.RestOfAllosRS.Count > 0))
			{
				LoadAllomorphCoOccurrenceRules(alloAdhocProhib);
			}
			foreach (var morphAdhocProhib in m_cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibRepository>().AllInstances()
				.Where(a => !a.Disabled && a.FirstMorphemeRA != null && a.RestOfMorphsRS.Count > 0))
			{
				LoadMorphemeCoOccurrenceRules(morphAdhocProhib);
			}
		}

		private void LoadInflClassMprFeature(IMoInflClass inflClass, MprFeatureGroup inflClassesGroup)
		{
			LoadMprFeature(inflClass, inflClassesGroup);
			foreach (var subclass in inflClass.SubclassesOC)
			{
				LoadInflClassMprFeature(subclass, inflClassesGroup);
			}
		}

		private bool HasValidRuleForm(ILexEntry entry)
		{
			if (entry.IsCircumfix() && entry.LexemeFormOA is IMoAffixAllomorph)
			{
				bool hasPrefix = false, hasSuffix = false;
				foreach (var form in entry.AlternateFormsOS.Where(IsValidRuleForm))
				{
					if (form.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphPrefix)
					{
						hasPrefix = true;
					}
					else if (form.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphSuffix)
					{
						hasSuffix = true;
					}
					if (hasPrefix && hasSuffix)
					{
						return true;
					}
				}
				return false;
			}
			return entry.AllAllomorphs.Any(IsValidRuleForm);
		}

		private bool IsValidRuleForm(IMoForm form)
		{
			var affixProcess = form as IMoAffixProcess;
			if (affixProcess != null)
			{
				return affixProcess.InputOS.Count > 1 || affixProcess.OutputOS.Count > 1;
			}
			var formStr = form.Form.VernacularDefaultWritingSystem.Text;
			if (form.IsAbstract || string.IsNullOrEmpty(formStr))
			{
				return false;
			}
			if (form.MorphTypeRA != null)
			{
				switch (form.MorphTypeRA.Guid.ToString())
				{
					case MoMorphTypeTags.kMorphProclitic:
					case MoMorphTypeTags.kMorphEnclitic:
						return true;

					case MoMorphTypeTags.kMorphPrefix:
					case MoMorphTypeTags.kMorphPrefixingInterfix:
					case MoMorphTypeTags.kMorphSuffix:
					case MoMorphTypeTags.kMorphSuffixingInterfix:
						if (formStr.Contains("[") && !formStr.Contains("[...]"))
						{
							return ((IMoAffixAllomorph)form).PhoneEnvRC.Any(env => IsValidEnvironment(env.StringRepresentation.Text));
						}
						return true;

					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
						return ((IMoAffixAllomorph)form).PositionRS.Any(env => IsValidEnvironment(env.StringRepresentation.Text));
				}
			}

			return false;
		}

		private void LoadMprFeature(ICmObject obj, MprFeatureGroup group)
		{
			var feat = new MprFeature { Name = obj.ShortName };
			group.MprFeatures.Add(feat);
			m_mprFeatures[obj] = feat;
			m_language.MprFeatures.Add(feat);
		}

		private static bool IsValidLexEntryForm(IMoForm form)
		{
			if (!(form is IMoStemAllomorph))
			{
				return false;
			}
			var formStr = form.Form.VernacularDefaultWritingSystem.Text;
			if (form.IsAbstract || string.IsNullOrEmpty(formStr))
			{
				return false;
			}
			return IsStemType(form.MorphTypeRA) || IsCliticType(form.MorphTypeRA);
		}

		private static bool IsStemType(IMoMorphType type)
		{
			if (type == null)
			{
				return false;
			}
			switch (type.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphRoot:
				case MoMorphTypeTags.kMorphStem:
				case MoMorphTypeTags.kMorphBoundRoot:
				case MoMorphTypeTags.kMorphBoundStem:
				case MoMorphTypeTags.kMorphPhrase:
					return true;
			}

			return false;
		}

		private static bool IsCliticType(IMoMorphType type)
		{
			if (type == null)
			{
				return false;
			}
			switch (type.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphClitic:
				case MoMorphTypeTags.kMorphEnclitic:
				case MoMorphTypeTags.kMorphProclitic:
				case MoMorphTypeTags.kMorphParticle:
					return true;
			}

			return false;
		}

		private void LoadLexEntries(Stratum stratum, ILexEntry entry, IList<IMoStemAllomorph> allos)
		{
			if (entry.SensesOS.Count == 0)
			{
				foreach (var lexEntryRef in entry.EntryRefsOS)
				{
					foreach (var inflType in GetInflTypes(lexEntryRef))
					{
						foreach (var component in lexEntryRef.ComponentLexemesRS)
						{
							var mainEntry = component as ILexEntry;
							if (mainEntry != null)
							{
								foreach (var msa in mainEntry.MorphoSyntaxAnalysesOC.OfType<IMoStemMsa>())
								{
									LoadLexEntryOfVariant(stratum, inflType, msa, allos);
								}
							}
							else
							{
								var sense = (ILexSense)component;
								LoadLexEntryOfVariant(stratum, inflType, (IMoStemMsa)sense.MorphoSyntaxAnalysisRA, allos);
							}
						}
					}
				}
			}
			foreach (var msa in entry.MorphoSyntaxAnalysesOC.OfType<IMoStemMsa>())
			{
				LoadLexEntry(stratum, msa, allos);
			}
		}

		private static IEnumerable<ILexEntryInflType> GetInflTypes(ILexEntryRef lexEntryRef)
		{
			if (lexEntryRef.VariantEntryTypesRS.Count == 0)
			{
				yield return null;
				yield break;
			}

			var normalTypeFound = false;
			foreach (var type in lexEntryRef.VariantEntryTypesRS)
			{
				var inflType = type as ILexEntryInflType;
				if (inflType != null)
				{
					yield return inflType;
				}
				else if (!normalTypeFound)
				{
					yield return null;
					normalTypeFound = true;
				}
			}
		}

		private void AddEntry(Stratum stratum, LexEntry hcEntry, IMoMorphSynAnalysis msa)
		{
			if (hcEntry.Allomorphs.Count > 0)
			{
				stratum.Entries.Add(hcEntry);
				m_morphemes.GetValue(msa, () => new List<Morpheme>()).Add(hcEntry);
			}
		}

		private void LoadLexEntry(Stratum stratum, IMoStemMsa msa, IList<IMoStemAllomorph> allos)
		{
			var hcEntry = new LexEntry();
			var inflClass = GetInflClass(msa);
			if (inflClass != null)
			{
				hcEntry.MprFeatures.Add(m_mprFeatures[inflClass]);
			}
			foreach (var prodRestrict in msa.ProdRestrictRC)
			{
				hcEntry.MprFeatures.Add(m_mprFeatures[prodRestrict]);
			}
			hcEntry.Gloss = GetGloss(msa);
			var fs = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
			{
				fs.AddValue(m_posFeature, m_posFeature.PossibleSymbols["pos" + msa.PartOfSpeechRA.Hvo]);
			}
			else
			{
				hcEntry.IsPartial = true;
			}
			if (msa.MsFeaturesOA != null && !msa.MsFeaturesOA.IsEmpty)
			{
				fs.AddValue(m_headFeature, LoadFeatureStruct(msa.MsFeaturesOA, m_language.SyntacticFeatureSystem));
			}
			fs.Freeze();
			hcEntry.SyntacticFeatureStruct = fs;
			hcEntry.Properties["ID"] = msa.Hvo;
			foreach (var allo in allos)
			{
				try
				{
					var hcAllo = LoadRootAllomorph(allo, msa);
					hcEntry.Allomorphs.Add(hcAllo);
					m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcAllo);
				}
				catch (InvalidShapeException ise)
				{
					m_logger.InvalidShape(ise.String, ise.Position, msa);
				}
			}

			AddEntry(stratum, hcEntry, msa);
		}

		private void LoadLexEntryOfVariant(Stratum stratum, ILexEntryInflType inflType, IMoStemMsa msa, IList<IMoStemAllomorph> allos)
		{
			var hcEntry = new LexEntry();
			var inflClass = GetInflClass(msa);
			if (inflClass != null)
			{
				hcEntry.MprFeatures.Add(m_mprFeatures[inflClass]);
			}
			foreach (var prodRestrict in msa.ProdRestrictRC)
			{
				hcEntry.MprFeatures.Add(m_mprFeatures[prodRestrict]);
			}
			// TODO: irregularly inflected forms should be handled by rule blocking in HC
			if (inflType != null)
			{
				hcEntry.MprFeatures.Add(m_mprFeatures[inflType]);
			}
			var glossSB = new StringBuilder();
			if (inflType != null)
			{
				var prepend = inflType.GlossPrepend.BestAnalysisAlternative.Text;
				if (prepend != "***")
				{
					glossSB.Append(prepend);
				}
			}
			glossSB.Append(GetGloss(msa));
			if (inflType != null)
			{
				var append = inflType.GlossAppend.BestAnalysisAlternative.Text;
				if (append != "***")
				{
					glossSB.Append(append);
				}
			}
			hcEntry.Gloss = glossSB.ToString();
			var fs = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
			{
				fs.AddValue(m_posFeature, m_posFeature.PossibleSymbols["pos" + msa.PartOfSpeechRA.Hvo]);
			}
			else
			{
				hcEntry.IsPartial = true;
			}
			FeatureStruct headFS = null;
			if (msa.MsFeaturesOA != null && !msa.MsFeaturesOA.IsEmpty)
			{
				headFS = LoadFeatureStruct(msa.MsFeaturesOA, m_language.SyntacticFeatureSystem);
			}
			if (inflType != null)
			{
				if (inflType.SlotsRC.Count == 0 && inflType.InflFeatsOA != null && !inflType.InflFeatsOA.IsEmpty)
				{
					var inflFS = LoadFeatureStruct(inflType.InflFeatsOA, m_language.SyntacticFeatureSystem);
					if (headFS == null)
					{
						headFS = inflFS;
					}
					else
					{
						headFS.Add(inflFS);
					}
				}
			}
			if (headFS != null)
			{
				fs.AddValue(m_headFeature, headFS);
			}
			fs.Freeze();
			hcEntry.SyntacticFeatureStruct = fs;

			hcEntry.Properties["ID"] = msa.Hvo;
			if (inflType != null)
			{
				hcEntry.Properties["InflTypeID"] = inflType.Hvo;
			}
			foreach (var allo in allos)
			{
				try
				{
					var hcAllo = LoadRootAllomorph(allo, msa);
					hcEntry.Allomorphs.Add(hcAllo);
					m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcAllo);
				}
				catch (InvalidShapeException ise)
				{
					m_logger.InvalidShape(ise.String, ise.Position, msa);
				}
			}

			AddEntry(stratum, hcEntry, msa);
		}

		private RootAllomorph LoadRootAllomorph(IMoStemAllomorph allo, IMoMorphSynAnalysis msa)
		{
			var form = FormatForm(allo.Form.VernacularDefaultWritingSystem.Text);
			var shape = Segment(form);
			var hcAllo = new RootAllomorph(new Segments(m_table, form, shape));
			foreach (var env in allo.PhoneEnvRC)
			{
				string error;
				if (IsValidEnvironment(env.StringRepresentation.Text, out error))
				{
					var contexts = SplitEnvironment(env);
					hcAllo.Environments.Add(new AllomorphEnvironment(m_spanFactory, ConstraintType.Require, LoadEnvironmentPattern(contexts.Item1, true),
						LoadEnvironmentPattern(contexts.Item2, false))
					{ Name = env.StringRepresentation.Text });
				}
				else
				{
					m_logger.InvalidEnvironment(allo, env, error, msa);
				}
			}

			StemName hcStemName;
			if (allo.StemNameRA != null && m_stemNames.TryGetValue(allo.StemNameRA, out hcStemName))
			{
				hcAllo.StemName = hcStemName;
			}
			switch (allo.MorphTypeRA.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphBoundRoot:
				case MoMorphTypeTags.kMorphBoundStem:
					hcAllo.IsBound = true;
					break;
			}

			hcAllo.Properties["ID"] = allo.Hvo;
			return hcAllo;
		}

		private void LoadMorphologicalRules(Stratum stratum, ILexEntry entry, IList<IMoForm> allos)
		{
			if (!HasValidRuleForm(entry))
			{
				return;
			}
			if (entry.SensesOS.Count == 0)
			{
				foreach (var lexEntryRef in entry.EntryRefsOS)
				{
					foreach (var component in lexEntryRef.ComponentLexemesRS)
					{
						var mainEntry = component as ILexEntry;
						if (mainEntry != null)
						{
							foreach (var msa in mainEntry.MorphoSyntaxAnalysesOC)
							{
								LoadMorphologicalRule(stratum, entry, allos, msa);
							}
						}
						else
						{
							var sense = (ILexSense)component;
							LoadMorphologicalRule(stratum, entry, allos, sense.MorphoSyntaxAnalysisRA);
						}
					}
				}
			}
			foreach (var msa in entry.MorphoSyntaxAnalysesOC)
			{
				LoadMorphologicalRule(stratum, entry, allos, msa);
			}
		}

		private void LoadMorphologicalRule(Stratum stratum, ILexEntry entry, IList<IMoForm> allos, IMoMorphSynAnalysis msa)
		{
			AffixProcessRule mrule = null;
			var s = stratum;
			switch (msa.ClassID)
			{
				case MoDerivAffMsaTags.kClassId:
					mrule = LoadDerivAffixProcessRule(entry, (IMoDerivAffMsa)msa, allos);
					break;

				case MoInflAffMsaTags.kClassId:
					var inflMsa = (IMoInflAffMsa)msa;
					if (inflMsa.SlotsRC.Count > 0)
					{
						s = null;
					}
					mrule = LoadInflAffixProcessRule(entry, inflMsa, allos);
					break;

				case MoUnclassifiedAffixMsaTags.kClassId:
					mrule = LoadUnclassifiedAffixProcessRule(entry, (IMoUnclassifiedAffixMsa)msa, allos);
					break;

				case MoStemMsaTags.kClassId:
					mrule = LoadCliticAffixProcessRule(entry, (IMoStemMsa)msa, allos);
					break;
			}

			if (mrule != null)
			{
				mrule.Gloss = GetGloss(msa);
				AddMorphologicalRule(s, mrule, msa);
			}
		}

		private static string GetGloss(IMoMorphSynAnalysis msa)
		{
			var sense = msa.OwnerOfClass<ILexEntry>().SenseWithMsa(msa);
			return sense?.Gloss.BestAnalysisAlternative.Text;
		}

		private void AddMorphologicalRule(Stratum stratum, AffixProcessRule rule, IMoMorphSynAnalysis msa)
		{
			if (rule.Allomorphs.Count > 0)
			{
				stratum?.MorphologicalRules.Add(rule);
				m_morphemes.GetValue(msa, () => new List<Morpheme>()).Add(rule);
			}
		}

		private AffixProcessRule LoadDerivAffixProcessRule(ILexEntry entry, IMoDerivAffMsa msa, IList<IMoForm> allos)
		{
			var mrule = new AffixProcessRule { Name = entry.ShortName };
			var requiredFS = new FeatureStruct();
			if (msa.FromPartOfSpeechRA != null)
			{
				requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(msa.FromPartOfSpeechRA));
			}
			if (msa.FromMsFeaturesOA != null && !msa.FromMsFeaturesOA.IsEmpty)
			{
				requiredFS.AddValue(m_headFeature, LoadFeatureStruct(msa.FromMsFeaturesOA, m_language.SyntacticFeatureSystem));
			}
			requiredFS.Freeze();
			mrule.RequiredSyntacticFeatureStruct = requiredFS;

			var outFS = new FeatureStruct();
			if (msa.ToPartOfSpeechRA != null)
			{
				outFS.AddValue(m_posFeature, m_posFeature.PossibleSymbols["pos" + msa.ToPartOfSpeechRA.Hvo]);
			}
			if (msa.ToMsFeaturesOA != null && !msa.ToMsFeaturesOA.IsEmpty)
			{
				outFS.AddValue(m_headFeature, LoadFeatureStruct(msa.ToMsFeaturesOA, m_language.SyntacticFeatureSystem));
			}
			outFS.Freeze();
			mrule.OutSyntacticFeatureStruct = outFS;

			var requiredMprFeatures = new List<MprFeature>();
			if (msa.FromInflectionClassRA != null)
			{
				requiredMprFeatures.AddRange(LoadAllInflClasses(msa.FromInflectionClassRA));
			}
			requiredMprFeatures.AddRange(msa.FromProdRestrictRC.Select(prodRestrict => m_mprFeatures[prodRestrict]));
			var outMprFeatures = new List<MprFeature>();
			if (msa.ToInflectionClassRA != null)
			{
				outMprFeatures.Add(m_mprFeatures[msa.ToInflectionClassRA]);
			}
			outMprFeatures.AddRange(msa.ToProdRestrictRC.Select(prodRestrict => m_mprFeatures[prodRestrict]));

			StemName hcStemName;
			if (msa.FromStemNameRA != null && m_stemNames.TryGetValue(msa.FromStemNameRA, out hcStemName))
			{
				mrule.RequiredStemName = hcStemName;
			}
			mrule.Properties["ID"] = msa.Hvo;

			foreach (var hcAllo in LoadAffixProcessAllomorphs(msa, allos))
			{
				hcAllo.RequiredMprFeatures.AddRange(requiredMprFeatures);
				hcAllo.OutMprFeatures.AddRange(outMprFeatures);
				mrule.Allomorphs.Add(hcAllo);
			}

			return mrule;
		}

		private AffixProcessRule LoadInflAffixProcessRule(ILexEntry entry, IMoInflAffMsa msa, IList<IMoForm> allos)
		{
			// TODO: use realizational affix process rules
			var mrule = new AffixProcessRule
			{
				Name = entry.ShortName,
				IsPartial = msa.SlotsRC.Count == 0
			};

			var requiredFS = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
			{
				requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(msa.PartOfSpeechRA));
			}
			if (msa.InflFeatsOA != null && !msa.InflFeatsOA.IsEmpty)
			{
				requiredFS.AddValue(m_headFeature, LoadFeatureStruct(msa.InflFeatsOA, m_language.SyntacticFeatureSystem));
			}
			requiredFS.Freeze();
			mrule.RequiredSyntacticFeatureStruct = requiredFS;
			var requiredMprFeatures = msa.FromProdRestrictRC.Select(prodRestrict => m_mprFeatures[prodRestrict]).ToList();
			mrule.Properties["ID"] = msa.Hvo;

			foreach (var hcAllo in LoadAffixProcessAllomorphs(msa, allos))
			{
				hcAllo.RequiredMprFeatures.AddRange(requiredMprFeatures);
				mrule.Allomorphs.Add(hcAllo);
			}

			return mrule;
		}

		private AffixProcessRule LoadUnclassifiedAffixProcessRule(ILexEntry entry, IMoUnclassifiedAffixMsa msa, IList<IMoForm> allos)
		{
			var mrule = new AffixProcessRule
			{
				Name = entry.ShortName,
				IsPartial = true
			};

			var requiredFS = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
			{
				requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(msa.PartOfSpeechRA));
			}
			requiredFS.Freeze();
			mrule.RequiredSyntacticFeatureStruct = requiredFS;

			mrule.Properties["ID"] = msa.Hvo;

			foreach (var hcAllo in LoadAffixProcessAllomorphs(msa, allos))
			{
				mrule.Allomorphs.Add(hcAllo);
			}

			return mrule;
		}

		private AffixProcessRule LoadCliticAffixProcessRule(ILexEntry entry, IMoStemMsa msa, IList<IMoForm> allos)
		{
			var mrule = new AffixProcessRule { Name = entry.ShortName };

			var requiredFS = new FeatureStruct();
			if (msa.FromPartsOfSpeechRC.Count > 0)
			{
				requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(msa.FromPartsOfSpeechRC));
			}
			requiredFS.Freeze();
			mrule.RequiredSyntacticFeatureStruct = requiredFS;

			mrule.Properties["ID"] = msa.Hvo;

			foreach (var hcAllo in LoadAffixProcessAllomorphs(msa, allos))
			{
				mrule.Allomorphs.Add(hcAllo);
			}

			return mrule;
		}

		private IEnumerable<AffixProcessAllomorph> LoadAffixProcessAllomorphs(IMoMorphSynAnalysis msa, IList<IMoForm> allos)
		{
			var entry = msa.OwnerOfClass<ILexEntry>();
			if (entry.IsCircumfix() && entry.LexemeFormOA is IMoAffixAllomorph)
			{
				foreach (var prefixAllo in allos.OfType<IMoAffixAllomorph>().Where(a => a.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphPrefix))
				{
					MprFeature[] requiredMprFeatures = null;
					if (msa is IMoInflAffMsa)
					{
						requiredMprFeatures = LoadAllInflClasses(prefixAllo.InflectionClassesRC).ToArray();
					}
					foreach (var suffixAllo in allos.OfType<IMoAffixAllomorph>().Where(a => a.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphSuffix))
					{
						foreach (var prefixEnv in GetAffixAllomorphEnvironments(prefixAllo, msa))
						{
							foreach (var suffixEnv in GetAffixAllomorphEnvironments(suffixAllo, msa))
							{
								AffixProcessAllomorph hcAllo = null;
								try
								{
									hcAllo = LoadCircumfixAffixProcessAllomorph(prefixAllo, prefixEnv, suffixAllo, suffixEnv);
									if (requiredMprFeatures != null)
									{
										hcAllo.RequiredMprFeatures.AddRange(requiredMprFeatures);
									}
									m_allomorphs.GetValue(entry.LexemeFormOA, () => new List<Allomorph>()).Add(hcAllo);
								}
								catch (InvalidShapeException ise)
								{
									m_logger.InvalidShape(ise.String, ise.Position, msa);
								}
								if (hcAllo != null)
								{
									yield return hcAllo;
								}
							}
						}
					}
				}
			}
			else
			{
				foreach (var allo in allos)
				{
					switch (allo.ClassID)
					{
						case MoAffixProcessTags.kClassId:
							var affixProcess = (IMoAffixProcess)allo;
							AffixProcessAllomorph hcAffixProcessAllo = null;
							try
							{
								hcAffixProcessAllo = LoadAffixProcessAllomorph(affixProcess);
								if (msa is IMoInflAffMsa)
								{
									hcAffixProcessAllo.RequiredMprFeatures.AddRange(LoadAllInflClasses(affixProcess.InflectionClassesRC));
								}
								m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcAffixProcessAllo);
							}
							catch (InvalidShapeException ise)
							{
								m_logger.InvalidShape(ise.String, ise.Position, msa);
							}
							catch (InvalidAffixProcessException iape)
							{
								m_logger.InvalidAffixProcess(affixProcess, iape.IsInvalidLhs, msa);
							}
							if (hcAffixProcessAllo != null)
							{
								yield return hcAffixProcessAllo;
							}
							break;

						case MoAffixAllomorphTags.kClassId:
							var affixAllo = (IMoAffixAllomorph)allo;
							MprFeature[] requiredMprFeatures = null;
							if (msa is IMoInflAffMsa)
							{
								requiredMprFeatures = LoadAllInflClasses(affixAllo.InflectionClassesRC).ToArray();
							}
							foreach (var env in GetAffixAllomorphEnvironments(affixAllo, msa))
							{
								AffixProcessAllomorph hcAffixAllo = null;
								try
								{
									hcAffixAllo = LoadFormAffixProcessAllomorph(affixAllo, env);
									if (requiredMprFeatures != null)
									{
										hcAffixAllo.RequiredMprFeatures.AddRange(requiredMprFeatures);
									}
									var requiredFS = new FeatureStruct();
									if (affixAllo.MsEnvFeaturesOA != null && !affixAllo.MsEnvFeaturesOA.IsEmpty)
									{
										requiredFS.AddValue(m_headFeature, LoadFeatureStruct(affixAllo.MsEnvFeaturesOA, m_language.SyntacticFeatureSystem));
									}
									requiredFS.Freeze();
									hcAffixAllo.RequiredSyntacticFeatureStruct = requiredFS;
									m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcAffixAllo);
								}
								catch (InvalidShapeException ise)
								{
									m_logger.InvalidShape(ise.String, ise.Position, msa);
								}
								catch (InvalidReduplicationFormException iee)
								{
									m_logger.InvalidReduplicationForm(affixAllo, iee.Message, msa);
								}
								if (hcAffixAllo != null)
									yield return hcAffixAllo;
							}
							break;

						case MoStemAllomorphTags.kClassId:
							var stemAllo = (IMoStemAllomorph)allo;
							foreach (var env in GetStemAllomorphEnvironments(stemAllo, msa))
							{
								AffixProcessAllomorph hcStemAllo = null;
								try
								{
									hcStemAllo = LoadFormAffixProcessAllomorph(allo, env);
									m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcStemAllo);
								}
								catch (InvalidShapeException ise)
								{
									m_logger.InvalidShape(ise.String, ise.Position, msa);
								}
								if (hcStemAllo != null)
								{
									yield return hcStemAllo;
								}
							}
							break;
					}
				}
			}
		}

		private IEnumerable<IPhEnvironment> GetAffixAllomorphEnvironments(IMoAffixAllomorph allo, IMoMorphSynAnalysis msa)
		{
			return GetValidEnvironments(allo.PhoneEnvRC.Concat(allo.PositionRS), allo, msa);
		}

		private IEnumerable<IPhEnvironment> GetStemAllomorphEnvironments(IMoStemAllomorph allo, IMoMorphSynAnalysis msa)
		{
			return GetValidEnvironments(allo.PhoneEnvRC, allo, msa);
		}

		private IEnumerable<IPhEnvironment> GetValidEnvironments(IEnumerable<IPhEnvironment> envs, IMoForm allo, IMoMorphSynAnalysis msa)
		{
			var envArray = envs.ToArray();
			var hasBlankEnv = envArray.Length == 0;
			foreach (var env in envArray)
			{
				string error;
				if (IsValidEnvironment(env.StringRepresentation.Text, out error))
				{
					yield return env;
				}
				else
				{
					m_logger.InvalidEnvironment(allo, env, error, msa);
					hasBlankEnv = true;
				}
			}
			if (hasBlankEnv)
			{
				yield return null;
			}
		}

		private bool IsValidEnvironment(string env)
		{
			string error;
			return IsValidEnvironment(env, out error);
		}

		private bool IsValidEnvironment(string env, out string error)
		{
			if (m_envValidator.Recognize(env))
			{
				var start = 0;
				do
				{
					start = env.IndexOf("[", start, StringComparison.Ordinal);
					if (start != -1)
					{
						var end = env.IndexOf("]", start + 1, StringComparison.Ordinal);
						var ncAbbr = env.Substring(start + 1, end - start - 1).Trim();
						var caretIndex = ncAbbr.IndexOf("^", StringComparison.Ordinal);
						if (caretIndex != -1)
						{
							ncAbbr = ncAbbr.Substring(0, caretIndex).Trim();
						}
						NaturalClass hcNaturalClass;
						if (!TryLoadNaturalClass(m_naturalClassLookup[ncAbbr], out hcNaturalClass))
						{
							error = $"The natural class \"{ncAbbr}\" contains an invalid phoneme.";
							return false;
						}
						start = end + 1;
					}
				} while (start != -1);
				error = null;
				return true;
			}

			error = null;
			try
			{
				var errorElem = XElement.Parse(m_envValidator.ErrorMessage);
				var status = (string)errorElem.Attribute("status");
				var pos = (int)errorElem.Attribute("pos") + 1;
				switch (status)
				{
					case "class":
						error = $"Unrecognized natural class at position {pos}.";
						break;
					case "segment":
						error = $"Unrecognized phoneme at position {pos}.";
						break;
					case "missingClosingParen":
						error = $"Missing closing parenthesis at position {pos}.";
						break;
					case "missingOpeningParen":
						error = $"Missing opening parenthesis at position {pos}.";
						break;
					case "missingClosingSquareBracket":
						error = $"Missing closing bracket at position {pos}.";
						break;
					case "missingOpeningSquareBracket":
						error = $"Missing opening bracket at position {pos}.";
						break;
					case "syntax":
						error = $"Invalid format at position {pos}.";
						break;
				}
			}
			catch (XmlException)
			{
			}

			if (error == null)
			{
				error = "Invalid format.";
			}
			return false;
		}

		private AffixProcessAllomorph LoadCircumfixAffixProcessAllomorph(IMoAffixAllomorph prefixAllo, IPhEnvironment prefixEnv, IMoAffixAllomorph suffixAllo, IPhEnvironment suffixEnv)
		{
			var hcAllo = new AffixProcessAllomorph();
			Pattern<Word, ShapeNode> leftEnvPattern = null, rightEnvPattern = null;
			var pattern = new Pattern<Word, ShapeNode>("stem");
			if (prefixEnv == null && suffixEnv == null)
			{
				pattern.Children.AddRange(AnyPlus());
			}
			else
			{
				if (prefixEnv != null)
				{
					pattern.Children.Add(PrefixNull());
					var prefixContexts = SplitEnvironment(prefixEnv);
					pattern.Children.AddRange(LoadPatternNodes(prefixContexts.Item2));
					if (!string.IsNullOrEmpty(prefixContexts.Item1))
					{
						leftEnvPattern = LoadEnvironmentPattern(prefixContexts.Item1, true);
					}
				}
				pattern.Children.AddRange(AnyStar());
				if (suffixEnv != null)
				{
					var suffixContexts = SplitEnvironment(suffixEnv);
					pattern.Children.AddRange(LoadPatternNodes(suffixContexts.Item1));
					pattern.Children.Add(SuffixNull());
					if (!string.IsNullOrEmpty(suffixContexts.Item2))
					{
						rightEnvPattern = LoadEnvironmentPattern(suffixContexts.Item2, false);
					}
				}
			}
			pattern.Freeze();
			hcAllo.Lhs.Add(pattern);

			hcAllo.Rhs.Add(new InsertSegments(Segments(prefixAllo.Form.VernacularDefaultWritingSystem.Text.Trim() + "+")));
			hcAllo.Rhs.Add(new CopyFromInput("stem"));
			hcAllo.Rhs.Add(new InsertSegments(Segments("+" + suffixAllo.Form.VernacularDefaultWritingSystem.Text.Trim())));

			if (leftEnvPattern != null || rightEnvPattern != null)
			{
				string name;
				if (leftEnvPattern != null && rightEnvPattern == null)
				{
					name = prefixEnv.StringRepresentation.Text;
				}
				else if (leftEnvPattern == null)
				{
					name = suffixEnv.StringRepresentation.Text;
				}
				else
				{
					name = $"{prefixEnv.StringRepresentation.Text}, {suffixEnv.StringRepresentation.Text}";
				}
				hcAllo.Environments.Add(new AllomorphEnvironment(m_spanFactory, ConstraintType.Require, leftEnvPattern, rightEnvPattern) { Name = name });
			}

			hcAllo.Properties["ID"] = prefixAllo.Hvo;
			hcAllo.Properties["ID2"] = suffixAllo.Hvo;
			if (prefixEnv != null)
			{
				hcAllo.Properties["PrefixEnv"] = prefixEnv.StringRepresentation.Text;
			}
			if (suffixEnv != null)
			{
				hcAllo.Properties["SuffixEnv"] = suffixEnv.StringRepresentation.Text;
			}
			return hcAllo;
		}

		private AffixProcessAllomorph LoadAffixProcessAllomorph(IMoAffixProcess allo)
		{
			var hcAllo = new AffixProcessAllomorph();
			var i = 1;
			foreach (var ctxtOrVar in allo.InputOS)
			{
				var var = ctxtOrVar as IPhVariable;
				if (var != null)
				{
					var pattern = new Pattern<Word, ShapeNode>(i.ToString(CultureInfo.InvariantCulture), AnyStar());
					pattern.Freeze();
					hcAllo.Lhs.Add(pattern);
				}
				else
				{
					PatternNode<Word, ShapeNode> n;
					if (LoadPatternNode((IPhPhonContext)ctxtOrVar, out n))
					{
						var pattern = new Pattern<Word, ShapeNode>(i.ToString(CultureInfo.InvariantCulture), n);
						pattern.Freeze();
						hcAllo.Lhs.Add(pattern);
					}
					else
					{
						throw new InvalidAffixProcessException(allo, true);
					}
				}
				i++;
			}

			foreach (var mapping in allo.OutputOS)
			{
				switch (mapping.ClassID)
				{
					case MoInsertNCTags.kClassId:
						var insertNC = (IMoInsertNC)mapping;
						if (insertNC.ContentRA != null)
						{
							SimpleContext ctxt;
							if (!TryLoadSimpleContext(insertNC.ContentRA, out ctxt))
							{
								throw new InvalidAffixProcessException(allo, false);
							}
							hcAllo.Rhs.Add(new InsertSimpleContext(ctxt));
						}
						break;

					case MoCopyFromInputTags.kClassId:
						var copyFromInput = (IMoCopyFromInput)mapping;
						if (copyFromInput.ContentRA != null)
						{
							string partName = (copyFromInput.ContentRA.IndexInOwner + 1).ToString(CultureInfo.InvariantCulture);
							hcAllo.Rhs.Add(new CopyFromInput(partName));
						}
						break;

					case MoInsertPhonesTags.kClassId:
						var insertPhones = (IMoInsertPhones)mapping;
						if (insertPhones.ContentRS.Count > 0)
						{
							var sb = new StringBuilder();
							foreach (var termUnit in insertPhones.ContentRS)
							{
								var code = termUnit.CodesOS[0];
								var strRep = termUnit.ClassID == PhBdryMarkerTags.kClassId ? code.Representation.BestVernacularAlternative.Text
									: code.Representation.VernacularDefaultWritingSystem.Text;
								strRep = strRep.Trim();
								if (string.IsNullOrEmpty(strRep))
								{
									throw new InvalidAffixProcessException(allo, false);
								}
								sb.Append(strRep);
							}
							hcAllo.Rhs.Add(new InsertSegments(Segments(sb.ToString())));
						}
						break;

					case MoModifyFromInputTags.kClassId:
						var modifyFromInput = (IMoModifyFromInput)mapping;
						if (modifyFromInput.ContentRA != null && modifyFromInput.ModificationRA != null)
						{
							SimpleContext ctxt;
							if (!TryLoadSimpleContext(modifyFromInput.ModificationRA, out ctxt))
							{
								throw new InvalidAffixProcessException(allo, false);
							}
							var partName = (modifyFromInput.ContentRA.IndexInOwner + 1).ToString(CultureInfo.InvariantCulture);
							hcAllo.Rhs.Add(new ModifyFromInput(partName, ctxt));
						}
						break;
				}
			}

			if (allo.MorphTypeRA != null)
			{
				switch (allo.MorphTypeRA.Guid.ToString())
				{
					case MoMorphTypeTags.kMorphPrefix:
						hcAllo.ReduplicationHint = ReduplicationHint.Prefix;
						break;

					case MoMorphTypeTags.kMorphSuffix:
						hcAllo.ReduplicationHint = ReduplicationHint.Suffix;
						break;
				}
			}

			hcAllo.Properties["ID"] = allo.Hvo;
			return hcAllo;
		}

		private AffixProcessAllomorph LoadFormAffixProcessAllomorph(IMoForm allo, IPhEnvironment env)
		{
			var hcAllo = new AffixProcessAllomorph();
			var form = allo.Form.VernacularDefaultWritingSystem.Text.Trim();
			var contexts = SplitEnvironment(env);
			if (form.Contains("["))
			{
				if (form.Contains("[...]"))
				{
					var stemPattern = new Pattern<Word, ShapeNode>("stem", AnyPlus());
					stemPattern.Freeze();
					hcAllo.Lhs.Add(stemPattern);

					hcAllo.Rhs.Add(new CopyFromInput("stem"));
					var beforePos = form.IndexOf('[');
					var beforeStr = form.Substring(0, beforePos).Trim();
					hcAllo.Rhs.Add(new InsertSegments(Segments("+" + beforeStr)));
					hcAllo.Rhs.Add(new CopyFromInput("stem"));
					var afterPos = form.IndexOf(']');
					var afterStr = form.Substring(afterPos + 1).Trim();
					if (!string.IsNullOrEmpty(afterStr))
					{
						hcAllo.Rhs.Add(new InsertSegments(Segments(afterStr)));
					}
					switch (allo.MorphTypeRA.Guid.ToString())
					{
						case MoMorphTypeTags.kMorphPrefix:
							hcAllo.ReduplicationHint = ReduplicationHint.Prefix;
							break;

						case MoMorphTypeTags.kMorphSuffix:
							hcAllo.ReduplicationHint = ReduplicationHint.Suffix;
							break;
					}
				}
				else
				{
					var environment = $"/_{form}";
					string error;
					// A form containing a reduplication expression should look like an environment
					if (!IsValidEnvironment(environment, out error))
					{
						throw new InvalidReduplicationFormException(error);
					}
					var stemPattern = new Pattern<Word, ShapeNode>("stem", AnyStar());
					stemPattern.Freeze();
					switch (allo.MorphTypeRA.Guid.ToString())
					{
						case MoMorphTypeTags.kMorphSuffix:
						case MoMorphTypeTags.kMorphSuffixingInterfix:
						case MoMorphTypeTags.kMorphEnclitic:
							hcAllo.Lhs.Add(stemPattern);
							hcAllo.Lhs.AddRange(LoadReduplicationPatterns(contexts.Item1));
							var suffixNull = new Pattern<Word, ShapeNode>("suffixNull", SuffixNull());
							suffixNull.Freeze();
							hcAllo.Lhs.Add(suffixNull);

							hcAllo.Rhs.Add(new CopyFromInput("stem"));
							hcAllo.Rhs.AddRange(LoadReduplicationOutputActions(contexts.Item1));
							hcAllo.Rhs.Add(new CopyFromInput("suffixNull"));
							hcAllo.Rhs.Add(new InsertSegments(Segments("+")));
							hcAllo.Rhs.AddRange(LoadReduplicationOutputActions(form));
							break;

						case MoMorphTypeTags.kMorphPrefix:
						case MoMorphTypeTags.kMorphPrefixingInterfix:
						case MoMorphTypeTags.kMorphProclitic:
							var prefixNull = new Pattern<Word, ShapeNode>("prefixNull", PrefixNull());
							prefixNull.Freeze();
							hcAllo.Lhs.Add(prefixNull);
							hcAllo.Lhs.AddRange(LoadReduplicationPatterns(contexts.Item2));
							hcAllo.Lhs.Add(stemPattern);

							hcAllo.Rhs.AddRange(LoadReduplicationOutputActions(form));
							hcAllo.Rhs.Add(new InsertSegments(Segments("+")));
							hcAllo.Rhs.Add(new CopyFromInput("prefixNull"));
							hcAllo.Rhs.AddRange(LoadReduplicationOutputActions(contexts.Item2));
							hcAllo.Rhs.Add(new CopyFromInput("stem"));
							break;
					}
				}
			}
			else
			{
				switch (allo.MorphTypeRA.Guid.ToString())
				{
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
						var leftInfixPattern = new Pattern<Word, ShapeNode>("left");
						if (contexts.Item1.StartsWith("#"))
						{
							leftInfixPattern.Children.Add(PrefixNull());
						}
						else
						{
							leftInfixPattern.Children.AddRange(AnyStar());
						}
						leftInfixPattern.Children.AddRange(LoadPatternNodes(contexts.Item1));
						leftInfixPattern.Freeze();
						hcAllo.Lhs.Add(leftInfixPattern);

						var rightInfixPattern = new Pattern<Word, ShapeNode>("right");
						rightInfixPattern.Children.AddRange(LoadPatternNodes(contexts.Item2));
						if (contexts.Item2.EndsWith("#"))
						{
							rightInfixPattern.Children.Add(SuffixNull());
						}
						else
						{
							rightInfixPattern.Children.AddRange(AnyStar());
						}
						rightInfixPattern.Freeze();
						hcAllo.Lhs.Add(rightInfixPattern);

						hcAllo.Rhs.Add(new CopyFromInput("left"));
						hcAllo.Rhs.Add(new InsertSegments(Segments("+" + form + "+")));
						hcAllo.Rhs.Add(new CopyFromInput("right"));
						break;

					case MoMorphTypeTags.kMorphSuffix:
					case MoMorphTypeTags.kMorphSuffixingInterfix:
					case MoMorphTypeTags.kMorphEnclitic:
						var suffixPattern = new Pattern<Word, ShapeNode>("stem");
						if (string.IsNullOrEmpty(contexts.Item1))
						{
							suffixPattern.Children.AddRange(AnyPlus());
						}
						else
						{
							if (contexts.Item1.StartsWith("#"))
							{
								suffixPattern.Children.Add(PrefixNull());
							}
							else
							{
								suffixPattern.Children.AddRange(AnyStar());
							}
							suffixPattern.Children.AddRange(LoadPatternNodes(contexts.Item1));
							suffixPattern.Children.Add(SuffixNull());
						}
						suffixPattern.Freeze();
						hcAllo.Lhs.Add(suffixPattern);

						hcAllo.Rhs.Add(new CopyFromInput("stem"));
						hcAllo.Rhs.Add(new InsertSegments(Segments("+" + form)));

						if (!string.IsNullOrEmpty(contexts.Item2))
						{
							hcAllo.Environments.Add(new AllomorphEnvironment(m_spanFactory, ConstraintType.Require, null, LoadEnvironmentPattern(contexts.Item2, false)) { Name = env.StringRepresentation.Text });
						}
						break;

					case MoMorphTypeTags.kMorphPrefix:
					case MoMorphTypeTags.kMorphPrefixingInterfix:
					case MoMorphTypeTags.kMorphProclitic:
						var prefixPattern = new Pattern<Word, ShapeNode>("stem");
						if (string.IsNullOrEmpty(contexts.Item2))
						{
							prefixPattern.Children.AddRange(AnyPlus());
						}
						else
						{
							prefixPattern.Children.Add(PrefixNull());
							prefixPattern.Children.AddRange(LoadPatternNodes(contexts.Item2));
							if (contexts.Item2.EndsWith("#"))
							{
								prefixPattern.Children.Add(SuffixNull());
							}
							else
							{
								prefixPattern.Children.AddRange(AnyStar());
							}
						}
						prefixPattern.Freeze();
						hcAllo.Lhs.Add(prefixPattern);

						hcAllo.Rhs.Add(new InsertSegments(Segments(form + "+")));
						hcAllo.Rhs.Add(new CopyFromInput("stem"));

						if (!string.IsNullOrEmpty(contexts.Item1))
						{
							hcAllo.Environments.Add(new AllomorphEnvironment(m_spanFactory, ConstraintType.Require, LoadEnvironmentPattern(contexts.Item1, true), null) { Name = env.StringRepresentation.Text });
						}
						break;
				}
			}

			hcAllo.Properties["ID"] = allo.Hvo;
			if (env != null)
			{
				hcAllo.Properties["Env"] = env.StringRepresentation.Text;
			}
			return hcAllo;
		}

		private IEnumerable<Pattern<Word, ShapeNode>> LoadReduplicationPatterns(string patternStr)
		{
			foreach (var token in TokenizeContext(patternStr))
			{
				if (token.StartsWith("["))
				{
					var caretPos = token.IndexOf('^');
					var ncAbbr = token.Substring(1, caretPos - 1).Trim();
					var naturalClass = m_naturalClassLookup[ncAbbr];
					SimpleContext ctxt;
					TryLoadSimpleContext(naturalClass, out ctxt);
					var pattern = new Pattern<Word, ShapeNode>(token.Substring(1, token.Length - 2).Trim(), new Constraint<Word, ShapeNode>(ctxt.FeatureStruct) { Tag = ctxt });
					pattern.Freeze();
					yield return pattern;
				}
			}
		}

		private IEnumerable<MorphologicalOutputAction> LoadReduplicationOutputActions(string patternStr)
		{
			foreach (var token in TokenizeContext(patternStr))
			{
				if (token.StartsWith("["))
				{
					yield return new CopyFromInput(token.Substring(1, token.Length - 2).Trim());
				}
				else
				{
					yield return new InsertSegments(Segments(token.Trim()));
				}
			}
		}

		private AffixTemplate LoadAffixTemplate(IMoInflAffixTemplate template, IList<IMoInflAffixSlot> slots)
		{
			var hcTemplate = new AffixTemplate
			{
				Name = template.Name.BestAnalysisAlternative.Text,
				IsFinal = template.Final
			};

			var requiredFS = new FeatureStruct();
			requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(template.OwnerOfClass<IPartOfSpeech>()));
			requiredFS.Freeze();
			hcTemplate.RequiredSyntacticFeatureStruct = requiredFS;
			hcTemplate.Properties["ID"] = template.Hvo;

			foreach (var slot in slots)
			{
				var type = slot.ReferringObjects.OfType<ILexEntryInflType>().FirstOrDefault();
				var rules = new List<MorphemicMorphologicalRule>();
				foreach (var msa in slot.Affixes)
				{
					List<Morpheme> morphemes;
					if (m_morphemes.TryGetValue(msa, out morphemes))
					{
						foreach (var mrule in morphemes.OfType<AffixProcessRule>())
						{
							if (type != null)
							{
								// block slot from applying to irregularly inflected forms
								foreach (var allo in mrule.Allomorphs)
								{
									allo.ExcludedMprFeatures.Add(m_mprFeatures[type]);
								}
							}
							rules.Add(mrule);
						}
					}
				}

				// add a null affix to the required slot so that irregularly inflected forms can parse correctly
				// TODO: this really should be handled using rule blocking in HC
				if (type != null && !slot.Optional)
				{
					rules.Add(LoadNullAffixProcessRule(type, template, slot));
				}
				hcTemplate.Slots.Add(new AffixTemplateSlot(rules) { Name = slot.Name.BestAnalysisAlternative.Text, Optional = slot.Optional });
			}

			return hcTemplate;
		}

		private AffixProcessRule LoadNullAffixProcessRule(ILexEntryInflType type, IMoInflAffixTemplate template, IMoInflAffixSlot slot)
		{
			var mrule = new AffixProcessRule { Name = "Null" };
			var outFS = new FeatureStruct();
			if (type.InflFeatsOA != null && !type.InflFeatsOA.IsEmpty)
			{
				outFS.AddValue(m_headFeature, LoadFeatureStruct(type.InflFeatsOA, m_language.SyntacticFeatureSystem));
			}
			outFS.Freeze();
			mrule.OutSyntacticFeatureStruct = outFS;

			var msubrule = new AffixProcessAllomorph();

			msubrule.RequiredMprFeatures.Add(m_mprFeatures[type]);

			var stemPattern = new Pattern<Word, ShapeNode>("stem");
			stemPattern.Children.AddRange(AnyPlus());
			stemPattern.Freeze();
			msubrule.Lhs.Add(stemPattern);

			var isPrefix = template.PrefixSlotsRS.Contains(slot);

			if (isPrefix)
			{
				msubrule.Rhs.Add(new InsertSegments(Segments("^0+")));
			}
			msubrule.Rhs.Add(new CopyFromInput("stem"));
			if (!isPrefix)
			{
				msubrule.Rhs.Add(new InsertSegments(Segments("+^0")));
			}
			mrule.Allomorphs.Add(msubrule);

			mrule.Properties["InflTypeID"] = type.Hvo;
			mrule.Properties["SlotID"] = slot.Hvo;
			msubrule.Properties["IsNull"] = true;
			msubrule.Properties["IsPrefix"] = isPrefix;

			return mrule;
		}

		private IEnumerable<CompoundingRule> DefaultCompoundingRules()
		{
			var headPattern = new Pattern<Word, ShapeNode>("head", AnyPlus());
			headPattern.Freeze();
			var nonheadPattern = new Pattern<Word, ShapeNode>("nonhead", AnyPlus());
			nonheadPattern.Freeze();

			var compLeft = new CompoundingRule { Name = "Default Left Head Compounding" };
			var csubruleLeft = new CompoundingSubrule();

			csubruleLeft.HeadLhs.Add(headPattern);
			csubruleLeft.NonHeadLhs.Add(nonheadPattern);

			csubruleLeft.Rhs.Add(new CopyFromInput("head"));
			csubruleLeft.Rhs.Add(new InsertSegments(Segments("+")));
			csubruleLeft.Rhs.Add(new CopyFromInput("nonhead"));
			compLeft.Subrules.Add(csubruleLeft);

			yield return compLeft;

			var compRight = new CompoundingRule { Name = "Default Right Head Compounding" };
			var csubruleRight = new CompoundingSubrule();

			csubruleRight.HeadLhs.Add(headPattern);
			csubruleRight.NonHeadLhs.Add(nonheadPattern);

			csubruleRight.Rhs.Add(new CopyFromInput("nonhead"));
			csubruleRight.Rhs.Add(new InsertSegments(Segments("+")));
			csubruleRight.Rhs.Add(new CopyFromInput("head"));
			compRight.Subrules.Add(csubruleRight);

			yield return compRight;
		}

		private CompoundingRule LoadEndoCompoundingRule(IMoEndoCompound compoundRule)
		{
			var headRequiredFS = new FeatureStruct();
			var nonheadRequiredFS = new FeatureStruct();
			if (compoundRule.HeadLast)
			{
				if (compoundRule.RightMsaOA.PartOfSpeechRA != null)
				{
					headRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.RightMsaOA.PartOfSpeechRA));
				}
				if (compoundRule.LeftMsaOA.PartOfSpeechRA != null)
				{
					nonheadRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.LeftMsaOA.PartOfSpeechRA));
				}
			}
			else
			{
				if (compoundRule.RightMsaOA.PartOfSpeechRA != null)
				{
					nonheadRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.RightMsaOA.PartOfSpeechRA));
				}
				if (compoundRule.LeftMsaOA.PartOfSpeechRA != null)
				{
					headRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.LeftMsaOA.PartOfSpeechRA));
				}
			}
			headRequiredFS.Freeze();
			nonheadRequiredFS.Freeze();

			var outFS = new FeatureStruct();
			if (compoundRule.OverridingMsaOA.PartOfSpeechRA != null)
			{
				outFS.AddValue(m_posFeature, m_posFeature.PossibleSymbols["pos" + compoundRule.OverridingMsaOA.PartOfSpeechRA.Hvo]);
			}
			outFS.Freeze();

			var headPattern = new Pattern<Word, ShapeNode>("head", AnyPlus());
			headPattern.Freeze();
			var nonheadPattern = new Pattern<Word, ShapeNode>("nonhead", AnyPlus());
			nonheadPattern.Freeze();

			var hcCompoundRule = new CompoundingRule
			{
				Name = compoundRule.Name.BestAnalysisAlternative.Text,
				HeadRequiredSyntacticFeatureStruct = headRequiredFS,
				NonHeadRequiredSyntacticFeatureStruct = nonheadRequiredFS,
				OutSyntacticFeatureStruct = outFS,
				Properties = { { "ID", compoundRule.Hvo } }
			};

			var subrule = new CompoundingSubrule();

			if (compoundRule.OverridingMsaOA.InflectionClassRA != null)
			{
				subrule.OutMprFeatures.Add(m_mprFeatures[compoundRule.OverridingMsaOA.InflectionClassRA]);
			}
			subrule.HeadLhs.Add(headPattern);
			subrule.NonHeadLhs.Add(nonheadPattern);

			subrule.Rhs.Add(new CopyFromInput(compoundRule.HeadLast ? "nonhead" : "head"));
			subrule.Rhs.Add(new InsertSegments(Segments("+")));
			subrule.Rhs.Add(new CopyFromInput(compoundRule.HeadLast ? "head" : "nonhead"));

			hcCompoundRule.Subrules.Add(subrule);
			return hcCompoundRule;
		}

		private IEnumerable<CompoundingRule> LoadExoCompoundingRule(IMoExoCompound compoundRule)
		{
			var rightRequiredFS = new FeatureStruct();
			if (compoundRule.RightMsaOA.PartOfSpeechRA != null)
			{
				rightRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.RightMsaOA.PartOfSpeechRA));
			}
			rightRequiredFS.Freeze();
			var leftRequiredFS = new FeatureStruct();
			if (compoundRule.LeftMsaOA.PartOfSpeechRA != null)
			{
				leftRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.LeftMsaOA.PartOfSpeechRA));
			}
			leftRequiredFS.Freeze();
			var outFS = new FeatureStruct();
			if (compoundRule.ToMsaOA.PartOfSpeechRA != null)
			{
				outFS.AddValue(m_posFeature, m_posFeature.PossibleSymbols["pos" + compoundRule.ToMsaOA.PartOfSpeechRA.Hvo]);
			}
			outFS.Freeze();

			var headPattern = new Pattern<Word, ShapeNode>("head", AnyPlus());
			headPattern.Freeze();
			var nonheadPattern = new Pattern<Word, ShapeNode>("nonhead", AnyPlus());
			nonheadPattern.Freeze();

			var hcRightCompoundRule = new CompoundingRule
			{
				Name = compoundRule.Name.BestAnalysisAlternative.Text,
				HeadRequiredSyntacticFeatureStruct = rightRequiredFS,
				NonHeadRequiredSyntacticFeatureStruct = leftRequiredFS,
				OutSyntacticFeatureStruct = outFS,
				Properties = { { "ID", compoundRule.Hvo } }
			};

			var rightSubrule = new CompoundingSubrule();

			if (compoundRule.ToMsaOA.InflectionClassRA != null)
			{
				rightSubrule.OutMprFeatures.Add(m_mprFeatures[compoundRule.ToMsaOA.InflectionClassRA]);
			}
			rightSubrule.HeadLhs.Add(headPattern);
			rightSubrule.NonHeadLhs.Add(nonheadPattern);

			rightSubrule.Rhs.Add(new CopyFromInput("nonhead"));
			rightSubrule.Rhs.Add(new InsertSegments(Segments("+")));
			rightSubrule.Rhs.Add(new CopyFromInput("head"));

			hcRightCompoundRule.Subrules.Add(rightSubrule);

			yield return hcRightCompoundRule;

			var hcLeftCompoundRule = new CompoundingRule
			{
				Name = compoundRule.Name.BestAnalysisAlternative.Text,
				HeadRequiredSyntacticFeatureStruct = leftRequiredFS,
				NonHeadRequiredSyntacticFeatureStruct = rightRequiredFS,
				OutSyntacticFeatureStruct = outFS,
				Properties = { { "ID", compoundRule.Hvo } }
			};

			var leftSubrule = new CompoundingSubrule();

			if (compoundRule.ToMsaOA.InflectionClassRA != null)
			{
				leftSubrule.OutMprFeatures.Add(m_mprFeatures[compoundRule.ToMsaOA.InflectionClassRA]);
			}
			leftSubrule.HeadLhs.Add(headPattern);
			leftSubrule.NonHeadLhs.Add(nonheadPattern);

			leftSubrule.Rhs.Add(new CopyFromInput("head"));
			leftSubrule.Rhs.Add(new InsertSegments(Segments("+")));
			leftSubrule.Rhs.Add(new CopyFromInput("nonhead"));

			hcLeftCompoundRule.Subrules.Add(leftSubrule);

			yield return hcLeftCompoundRule;
		}

		private RewriteRule LoadRewriteRule(IPhRegularRule prule)
		{
			var variables = new Dictionary<IPhFeatureConstraint, string>();
			var i = 0;
			foreach (var var in prule.FeatureConstraints)
			{
				variables[var] = VariableNames[i];
				i++;
			}

			var hcPrule = new RewriteRule { Name = prule.Name.BestAnalysisAlternative.Text };

			switch (prule.Direction)
			{
				case 0:
					hcPrule.Direction = Direction.LeftToRight;
					hcPrule.ApplicationMode = RewriteApplicationMode.Iterative;
					break;

				case 1:
					hcPrule.Direction = Direction.RightToLeft;
					hcPrule.ApplicationMode = RewriteApplicationMode.Iterative;
					break;

				case 2:
					hcPrule.Direction = Direction.LeftToRight;
					hcPrule.ApplicationMode = RewriteApplicationMode.Simultaneous;
					break;
			}

			if (prule.StrucDescOS.Count > 0)
			{
				var lhsPattern = new Pattern<Word, ShapeNode>();
				foreach (var ctxt in prule.StrucDescOS)
				{
					PatternNode<Word, ShapeNode> node;
					if (LoadPatternNode(ctxt, variables, out node))
					{
						lhsPattern.Children.Add(node);
					}
				}
				lhsPattern.Freeze();
				hcPrule.Lhs = lhsPattern;
			}
			hcPrule.Properties["ID"] = prule.Hvo;

			foreach (var rhs in prule.RightHandSidesOS)
			{
				var psubrule = new RewriteSubrule();

				var requiredFS = new FeatureStruct();
				if (rhs.InputPOSesRC.Count > 0)
				{
					requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(rhs.InputPOSesRC));
				}
				requiredFS.Freeze();
				psubrule.RequiredSyntacticFeatureStruct = requiredFS;

				psubrule.RequiredMprFeatures.AddRange(rhs.ReqRuleFeatsRC.SelectMany(LoadMprFeatures));
				psubrule.ExcludedMprFeatures.AddRange(rhs.ExclRuleFeatsRC.SelectMany(LoadMprFeatures));

				if (rhs.StrucChangeOS.Count > 0)
				{
					var rhsPattern = new Pattern<Word, ShapeNode>();
					foreach (var ctxt in rhs.StrucChangeOS)
					{
						PatternNode<Word, ShapeNode> node;
						if (LoadPatternNode(ctxt, variables, out node))
						{
							rhsPattern.Children.Add(node);
						}
					}
					rhsPattern.Freeze();
					psubrule.Rhs = rhsPattern;
				}

				if (rhs.LeftContextOA != null)
				{
					var leftPattern = new Pattern<Word, ShapeNode>();
					if (IsWordInitial(rhs.LeftContextOA.ToEnumerable()))
					{
						leftPattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
					}
					PatternNode<Word, ShapeNode> leftNode;
					if (LoadPatternNode(rhs.LeftContextOA, variables, out leftNode))
					{
						leftPattern.Children.Add(leftNode);
					}
					leftPattern.Freeze();
					psubrule.LeftEnvironment = leftPattern;
				}

				if (rhs.RightContextOA != null)
				{
					var rightPattern = new Pattern<Word, ShapeNode>();
					PatternNode<Word, ShapeNode> rightNode;
					if (LoadPatternNode(rhs.RightContextOA, variables, out rightNode))
					{
						rightPattern.Children.Add(rightNode);
					}
					if (IsWordFinal(rhs.RightContextOA.ToEnumerable()))
					{
						rightPattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));
					}
					rightPattern.Freeze();
					psubrule.RightEnvironment = rightPattern;
				}

				hcPrule.Subrules.Add(psubrule);
			}

			return hcPrule;
		}

		private MetathesisRule LoadMetathesisRule(IPhMetathesisRule prule)
		{
			var hcPrule = new MetathesisRule { Name = prule.Name.BestAnalysisAlternative.Text };

			switch (prule.Direction)
			{
				case 0:
				case 2:
					hcPrule.Direction = Direction.LeftToRight;
					break;

				case 1:
					hcPrule.Direction = Direction.RightToLeft;
					break;
			}

			bool isMiddleWithLeftSwitch;
			var indices = prule.GetStrucChangeIndices(out isMiddleWithLeftSwitch);

			hcPrule.LeftSwitchName = "r";
			hcPrule.RightSwitchName = "l";

			var pattern = new Pattern<Word, ShapeNode>();
			if (IsWordInitial(prule.StrucDescOS))
			{
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
			}
			for (var i = 0; i < prule.StrucDescOS.Count; i++)
			{
				PatternNode<Word, ShapeNode> node;
				if (LoadPatternNode(prule.StrucDescOS[i], out node))
				{
					string name = null;
					if (i == indices[PhMetathesisRuleTags.kidxRightSwitch])
					{
						name = "r";
					}
					else if (i == indices[PhMetathesisRuleTags.kidxLeftSwitch])
					{
						name = "l";
					}
					pattern.Children.Add(new Group<Word, ShapeNode>(name, node));
				}
			}
			if (IsWordFinal(prule.StrucDescOS))
			{
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));
			}
			pattern.Freeze();
			hcPrule.Pattern = pattern;

			hcPrule.Properties["ID"] = prule.Hvo;

			return hcPrule;
		}

		private void LoadAllomorphCoOccurrenceRules(IMoAlloAdhocProhib alloAdhocProhib)
		{
			List<Allomorph> firstAllos;
			if (m_allomorphs.TryGetValue(alloAdhocProhib.FirstAllomorphRA, out firstAllos))
			{
				var allOthers = new List<List<Allomorph>>();
				foreach (var form in alloAdhocProhib.RestOfAllosRS)
				{
					List<Allomorph> hcAllos;
					if (m_allomorphs.TryGetValue(form, out hcAllos))
					{
						allOthers.Add(hcAllos);
					}
					else
					{
						return;
					}
				}

				var adjacency = GetAdjacency(alloAdhocProhib.Adjacency);
				foreach (var others in Permute(allOthers, 0))
				{
					foreach (var firstAllo in firstAllos)
					{
						var rule = new AllomorphCoOccurrenceRule(ConstraintType.Exclude, others, adjacency);
						firstAllo.AllomorphCoOccurrenceRules.Add(rule);
						m_language.AllomorphCoOccurrenceRules.Add(rule);
					}
				}
			}
		}

		private static IEnumerable<T[]> Permute<T>(List<List<T>> items, int index)
		{
			if (items.Count == 0)
			{
				yield break;
			}
			if (index == items.Count)
			{
				yield return new T[items.Count];
			}
			else
			{
				foreach (var item in items[index])
				{
					foreach (var result in Permute(items, index + 1))
					{
						result[index] = item;
						yield return result;
					}
				}
			}
		}

		private void LoadMorphemeCoOccurrenceRules(IMoMorphAdhocProhib morphAdhocProhib)
		{
			List<Morpheme> firstMorphemes;
			if (m_morphemes.TryGetValue(morphAdhocProhib.FirstMorphemeRA, out firstMorphemes))
			{
				var allOthers = new List<List<Morpheme>>();
				foreach (var msa in morphAdhocProhib.RestOfMorphsRS)
				{
					List<Morpheme> hcMorphemes;
					if (m_morphemes.TryGetValue(msa, out hcMorphemes))
					{
						allOthers.Add(hcMorphemes);
					}
					else
					{
						return;
					}
				}

				var adjacency = GetAdjacency(morphAdhocProhib.Adjacency);
				foreach (var others in Permute(allOthers, 0))
				{
					foreach (var firstMorpheme in firstMorphemes)
					{
						var rule = new MorphemeCoOccurrenceRule(ConstraintType.Exclude, others, adjacency);
						firstMorpheme.MorphemeCoOccurrenceRules.Add(rule);
						m_language.MorphemeCoOccurrenceRules.Add(rule);
					}
				}
			}
		}

		private static MorphCoOccurrenceAdjacency GetAdjacency(int adj)
		{
			switch (adj)
			{
				case 0:
					return MorphCoOccurrenceAdjacency.Anywhere;
				case 1:
					return MorphCoOccurrenceAdjacency.SomewhereToLeft;
				case 2:
					return MorphCoOccurrenceAdjacency.SomewhereToRight;
				case 3:
					return MorphCoOccurrenceAdjacency.AdjacentToLeft;
				case 4:
					return MorphCoOccurrenceAdjacency.AdjacentToRight;
			}

			throw new InvalidEnumArgumentException();
		}

		private Tuple<string, string> SplitEnvironment(IPhEnvironment env)
		{
			if (env == null)
			{
				return Tuple.Create("", "");
			}
			var contexts = env.StringRepresentation.Text.Trim().Substring(1).Split('_');
			return Tuple.Create(contexts[0].Trim(), contexts[1].Trim());
		}

		private Pattern<Word, ShapeNode> LoadEnvironmentPattern(string patternStr, bool left)
		{
			if (string.IsNullOrEmpty(patternStr))
			{
				return null;
			}
			var pattern = new Pattern<Word, ShapeNode>();
			if (left && patternStr.StartsWith("#"))
			{
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
			}
			pattern.Children.AddRange(LoadPatternNodes(patternStr));
			if (!left && patternStr.EndsWith("#"))
			{
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));
			}
			pattern.Freeze();
			return pattern;
		}

		private PatternNode<Word, ShapeNode> PrefixNull()
		{
			return new Quantifier<Word, ShapeNode>(0, -1,
				new Group<Word, ShapeNode>(
					new Constraint<Word, ShapeNode>(m_null.FeatureStruct) { Tag = m_null },
					new Constraint<Word, ShapeNode>(m_morphBdry.FeatureStruct) { Tag = m_morphBdry }));
		}

		private PatternNode<Word, ShapeNode> SuffixNull()
		{
			return new Quantifier<Word, ShapeNode>(0, -1,
				new Group<Word, ShapeNode>(
					new Constraint<Word, ShapeNode>(m_morphBdry.FeatureStruct) { Tag = m_morphBdry },
					new Constraint<Word, ShapeNode>(m_null.FeatureStruct) { Tag = m_null }));
		}

		private IEnumerable<PatternNode<Word, ShapeNode>> AnyPlus()
		{
			yield return PrefixNull();
			yield return new Quantifier<Word, ShapeNode>(1, -1, new Constraint<Word, ShapeNode>(m_any.FeatureStruct) { Tag = m_any });
			yield return SuffixNull();
		}

		private IEnumerable<PatternNode<Word, ShapeNode>> AnyStar()
		{
			yield return PrefixNull();
			yield return new Quantifier<Word, ShapeNode>(0, -1, new Constraint<Word, ShapeNode>(m_any.FeatureStruct) { Tag = m_any });
			yield return SuffixNull();
		}

		private bool LoadPatternNode(IPhPhonContext ctxt, out PatternNode<Word, ShapeNode> node)
		{
			return LoadPatternNode(ctxt, null, out node);
		}

		private bool LoadPatternNode(IPhPhonContext ctxt, Dictionary<IPhFeatureConstraint, string> variables, out PatternNode<Word, ShapeNode> node)
		{
			switch (ctxt.ClassID)
			{
				case PhSequenceContextTags.kClassId:
					var seqCtxt = (IPhSequenceContext)ctxt;
					var nodes = new List<PatternNode<Word, ShapeNode>>();
					foreach (var member in seqCtxt.MembersRS)
					{
						PatternNode<Word, ShapeNode> n;
						if (LoadPatternNode(member, variables, out n))
						{
							nodes.Add(n);
						}
					}
					if (nodes.Count > 0)
					{
						node = nodes.Count == 1 ? nodes.First() : new Group<Word, ShapeNode>(nodes);
						return true;
					}
					break;

				case PhIterationContextTags.kClassId:
					var iterCtxt = (IPhIterationContext)ctxt;
					PatternNode<Word, ShapeNode> childNode;
					if (LoadPatternNode(iterCtxt.MemberRA, variables, out childNode))
					{
						node = new Quantifier<Word, ShapeNode>(iterCtxt.Minimum, iterCtxt.Maximum, childNode);
						return true;
					}
					break;

				case PhSimpleContextBdryTags.kClassId:
					var bdryCtxt = (IPhSimpleContextBdry)ctxt;
					var bdry = bdryCtxt.FeatureStructureRA;
					if (bdry != null && bdry.Guid != LangProjectTags.kguidPhRuleWordBdry)
					{
						CharacterDefinition cd;
						if (m_charDefs.TryGetValue(bdry, out cd))
						{
							node = new Constraint<Word, ShapeNode>(cd.FeatureStruct) { Tag = cd };
							return true;
						}
					}
					break;

				case PhSimpleContextSegTags.kClassId:
					var segCtxt = (IPhSimpleContextSeg)ctxt;
					var phoneme = segCtxt.FeatureStructureRA;
					if (phoneme != null)
					{
						CharacterDefinition cd;
						if (m_charDefs.TryGetValue(phoneme, out cd))
						{
							node = new Constraint<Word, ShapeNode>(cd.FeatureStruct) { Tag = cd };
							return true;
						}
					}
					break;

				case PhSimpleContextNCTags.kClassId:
					var ncCtxt = (IPhSimpleContextNC)ctxt;
					SimpleContext hcCtxt;
					if (TryLoadSimpleContext(ncCtxt, variables, out hcCtxt))
					{
						node = new Constraint<Word, ShapeNode>(hcCtxt.FeatureStruct) { Tag = hcCtxt };
						return true;
					}
					break;
			}

			node = null;
			return false;
		}

		private IEnumerable<PatternNode<Word, ShapeNode>> LoadPatternNodes(string patternStr)
		{
			foreach (var token in TokenizeContext(patternStr))
			{
				switch (token[0])
				{
					case '#':
						break;

					case '[':
						var nc = m_naturalClassLookup[token.Substring(1, token.Length - 2).Trim()];
						SimpleContext ctxt;
						TryLoadSimpleContext(nc, out ctxt);
						yield return new Constraint<Word, ShapeNode>(ctxt.FeatureStruct) { Tag = ctxt };
						break;

					case '(':
						yield return new Quantifier<Word, ShapeNode>(0, 1, new Group<Word, ShapeNode>(LoadPatternNodes(token.Substring(1, token.Length - 2).Trim())));
						break;

					default:
						var representation = token.Trim();
						var segments = Segments(representation);
						yield return new Group<Word, ShapeNode>(segments.Shape.Select(n => new Constraint<Word, ShapeNode>(n.Annotation.FeatureStruct))) { Tag = segments };
						break;
				}
			}
		}

		private static IEnumerable<string> TokenizeContext(string contextStr)
		{
			var pos = 0;
			while (pos < contextStr.Length)
			{
				switch (contextStr[pos])
				{
					case '#':
						yield return "#";
						pos++;
						break;

					case '[':
						var endNCPos = contextStr.IndexOf(']', pos);
						yield return contextStr.Substring(pos, endNCPos - pos + 1);
						pos = endNCPos + 1;
						break;

					case '(':
						var endOptPos = contextStr.IndexOf(')', pos);
						yield return contextStr.Substring(pos, endOptPos - pos + 1);
						pos = endOptPos + 1;
						break;

					case ' ':
						pos++;
						break;

					default:
						var endRepPos = contextStr.IndexOfAny(new[] { '#', '[', '(', ' ' }, pos);
						if (endRepPos == -1)
						{
							endRepPos = contextStr.Length;
						}
						yield return contextStr.Substring(pos, endRepPos - pos);
						pos = endRepPos;
						break;
				}
			}
		}

		private static bool IsWordInitial(IEnumerable<IPhPhonContext> ctxts)
		{
			var ctxt = ctxts.First();
			if (IsWordBoundary(ctxt))
			{
				return true;
			}
			var seqCtxt = ctxt as IPhSequenceContext;
			if (seqCtxt != null)
			{
				if (seqCtxt.MembersRS.Count > 0 && IsWordBoundary(seqCtxt.MembersRS[0]))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsWordFinal(IEnumerable<IPhPhonContext> ctxts)
		{
			var ctxt = ctxts.Last();
			if (IsWordBoundary(ctxt))
			{
				return true;
			}
			var seqCtxt = ctxt as IPhSequenceContext;
			if (seqCtxt != null)
			{
				if (seqCtxt.MembersRS.Count > 0 && IsWordBoundary(seqCtxt.MembersRS[seqCtxt.MembersRS.Count - 1]))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsWordBoundary(IPhPhonContext ctxt)
		{
			var bdryCtxt = ctxt as IPhSimpleContextBdry;
			if (bdryCtxt != null)
			{
				if (bdryCtxt.FeatureStructureRA.Guid == LangProjectTags.kguidPhRuleWordBdry)
				{
					return true;
				}
			}
			return false;
		}

		private static FeatureStruct LoadFeatureStruct(IFsFeatStruc fs, FeatureSystem featSys)
		{
			var hcFS = new FeatureStruct();
			if (fs != null)
			{
				foreach (var value in fs.FeatureSpecsOC)
				{
					var closedValue = value as IFsClosedValue;
					if (closedValue != null)
					{
						var hcFeature = featSys.GetFeature<SymbolicFeature>("feat" + closedValue.FeatureRA.Hvo);
						// TODO: should we display something to the user if a FS has an invalid value?
						FeatureSymbol symbol;
						if (hcFeature.PossibleSymbols.TryGetValue("sym" + closedValue.ValueRA.Hvo, out symbol))
						{
							hcFS.AddValue(hcFeature, symbol);
						}
					}
					else
					{
						var complexValue = (IFsComplexValue)value;
						var hcFeature = featSys.GetFeature<ComplexFeature>("feat" + complexValue.FeatureRA.Hvo);
						hcFS.AddValue(hcFeature, LoadFeatureStruct((IFsFeatStruc)complexValue.ValueOA, featSys));
					}
				}
			}
			return hcFS;
		}

		private Shape Segment(string str)
		{
			Shape shape;
			if (m_acceptUnspecifiedGraphemes)
			{
				int[] baseCharPositions = null;
				do
				{
					var errorPos = m_table.TrySegment(str, out shape);
					if (errorPos != -1)
					{
						shape = null;
						// add whole text element (base character + diacritics) to character definition table
						var symbolStr = StringInfo.GetNextTextElement(str, errorPos);
						if (CharUnicodeInfo.GetUnicodeCategory(symbolStr[0]) == UnicodeCategory.NonSpacingMark)
						{
							// we hit an unknown diacritic, so add previous base character along with diacritic to symbol table
							if (baseCharPositions == null)
							{
								baseCharPositions = StringInfo.ParseCombiningCharacters(str);
							}
							symbolStr = StringInfo.GetNextTextElement(str, baseCharPositions.Last(pos => pos < errorPos));
						}
						m_table.AddSegment(symbolStr);
					}
				} while (shape == null);
			}
			else
			{
				shape = m_table.Segment(str);
			}
			return shape;
		}

		private static string FormatForm(string formStr)
		{
			return formStr.Trim().Replace(' ', '.');
		}

		private IEnumerable<FeatureSymbol> LoadAllPartsOfSpeech(IPartOfSpeech pos)
		{
			return LoadAllPartsOfSpeech(pos.ToEnumerable());
		}

		private IEnumerable<FeatureSymbol> LoadAllPartsOfSpeech(IEnumerable<IPartOfSpeech> poss)
		{
			foreach (var pos in poss)
			{
				yield return m_posFeature.PossibleSymbols["pos" + pos.Hvo];
				foreach (var symbol in LoadAllPartsOfSpeech(pos.SubPossibilitiesOS.Cast<IPartOfSpeech>()))
				{
					yield return symbol;
				}
			}
		}

		private IEnumerable<MprFeature> LoadAllInflClasses(IMoInflClass inflClass)
		{
			yield return m_mprFeatures[inflClass];
			foreach (var mprFeat in LoadAllInflClasses(inflClass.SubclassesOC))
			{
				yield return mprFeat;
			}
		}

		private IEnumerable<MprFeature> LoadAllInflClasses(IEnumerable<IMoInflClass> inflClasses)
		{
			foreach (var inflClass in inflClasses)
			{
				yield return m_mprFeatures[inflClass];
				foreach (var mprFeat in LoadAllInflClasses(inflClass.SubclassesOC))
				{
					yield return mprFeat;
				}
			}
		}

		private IEnumerable<MprFeature> LoadMprFeatures(IPhPhonRuleFeat ruleFeat)
		{
			switch (ruleFeat.ItemRA.ClassID)
			{
				case MoInflClassTags.kClassId:
					foreach (var mprFeat in LoadAllInflClasses((IMoInflClass)ruleFeat.ItemRA))
					{
						yield return mprFeat;
					}
					break;

				case CmPossibilityTags.kClassId:
					yield return m_mprFeatures[ruleFeat.ItemRA];
					break;
			}
		}

		private static IMoInflClass GetInflClass(IMoStemMsa msa)
		{
			return msa.InflectionClassRA ?? (msa.PartOfSpeechRA != null ? GetDefaultInflClass(msa.PartOfSpeechRA) : null);
		}

		private static IMoInflClass GetDefaultInflClass(IPartOfSpeech pos)
		{
			return pos.DefaultInflectionClassRA ?? pos.SubPossibilitiesOS.Cast<IPartOfSpeech>().Select(child => GetDefaultInflClass(child)).FirstOrDefault(defInflClass => defInflClass != null);
		}

		private static void LoadFeatureSystem(IFsFeatureSystem featSys, FeatureSystem hcFeatSys)
		{
			foreach (var feature in featSys.FeaturesOC)
			{
				var closedFeature = feature as IFsClosedFeature;
				if (closedFeature != null)
				{
					hcFeatSys.Add(new SymbolicFeature("feat" + closedFeature.Hvo,
						closedFeature.ValuesOC.Select(sfv => new FeatureSymbol("sym" + sfv.Hvo) { Description = sfv.Abbreviation.BestAnalysisAlternative.Text }))
					{ Description = feature.Abbreviation.BestAnalysisAlternative.Text });
				}
				else
				{
					hcFeatSys.Add(new ComplexFeature("feat" + feature.Hvo) { Description = feature.Abbreviation.BestAnalysisAlternative.Text });
				}
			}
			hcFeatSys.Freeze();
		}

		private void LoadCharacterDefinitionTable(IPhPhonemeSet phonemeSet)
		{
			m_table = new CharacterDefinitionTable(m_spanFactory) { Name = phonemeSet.Name.BestAnalysisAlternative.Text };
			foreach (var phoneme in phonemeSet.PhonemesOC)
			{
				FeatureStruct fs = null;
				if (phoneme.FeaturesOA != null && phoneme.FeaturesOA.FeatureSpecsOC.Count > 0)
				{
					fs = LoadFeatureStruct(phoneme.FeaturesOA, m_language.PhonologicalFeatureSystem);
				}
				var reps = phoneme.CodesOS.Where(c => !string.IsNullOrEmpty(c.Representation.VernacularDefaultWritingSystem.Text))
					.Select(c => c.Representation.VernacularDefaultWritingSystem.Text).ToArray();
				if (reps.Length == 0)
				{
					// did not find a grapheme for this phoneme
					m_logger.InvalidPhoneme(phoneme);
				}
				else if (reps.Any(r => m_table.Contains(r)))
				{
					// another phoneme has defined the same grapheme
					m_logger.DuplicateGrapheme(phoneme);
				}
				else
				{
					var cd = m_table.AddSegment(reps, fs);
					m_charDefs[phoneme] = cd;
				}
			}

			foreach (var bdry in phonemeSet.BoundaryMarkersOC.Where(bdry => bdry.Guid != LangProjectTags.kguidPhRuleWordBdry))
			{
				var reps = bdry.CodesOS.Where(c => !string.IsNullOrEmpty(c.Representation.BestVernacularAlternative.Text))
					.Select(c => c.Representation.BestVernacularAlternative.Text).ToArray();
				if (reps.Length > 0)
				{
					var cd = m_table.AddBoundary(reps);
					m_charDefs[bdry] = cd;
				}
			}

			m_null = m_table.AddBoundary(new[] { "^0", "*0", "&0", "Ø", "∅" });
			m_table.AddBoundary(".");
			m_morphBdry = m_table["+"];

			if (m_acceptUnspecifiedGraphemes)
			{
				// load valid characters from the default vernacular writing system into symbol table
				var ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
				var validChars = ValidCharacters.Load(ws);
				foreach (var wordFormingChar in validChars.WordFormingCharacters)
				{
					if (!m_table.Contains(wordFormingChar))
					{
						m_table.AddSegment(wordFormingChar);
					}
				}
				foreach (var otherChar in validChars.OtherCharacters)
				{
					if (!m_table.Contains(otherChar))
					{
						m_table.AddBoundary(otherChar);
					}
				}
			}
			m_language.CharacterDefinitionTables.Add(m_table);
		}

		private bool TryLoadSimpleContext(IPhSimpleContextNC ctxt, Dictionary<IPhFeatureConstraint, string> variables, out SimpleContext hcCtxt)
		{
			if (ctxt.FeatureStructureRA == null)
			{
				hcCtxt = null;
				return false;
			}

			NaturalClass hcNaturalClass;
			if (!TryLoadNaturalClass(ctxt.FeatureStructureRA, out hcNaturalClass))
			{
				hcCtxt = null;
				return false;
			}

			hcCtxt = new SimpleContext(hcNaturalClass, GetVariables(variables, ctxt.PlusConstrRS, true)
				.Concat(GetVariables(variables, ctxt.MinusConstrRS, false)));
			return true;
		}

		private IEnumerable<SymbolicFeatureValue> GetVariables(Dictionary<IPhFeatureConstraint, string> variables, IEnumerable<IPhFeatureConstraint> constraints, bool agree)
		{
			return constraints
				.Select(constraint => new { constraint, varName = variables[constraint] })
				.Select(@t => new
				{
					@t, feat = m_language.PhonologicalFeatureSystem.GetFeature<SymbolicFeature>("feat" + @t.constraint.FeatureRA.Hvo)
				})
				.Select(@t => new SymbolicFeatureValue(@t.feat, @t.@t.varName, agree));
		}

		private bool TryLoadSimpleContext(IPhNaturalClass naturalClass, out SimpleContext ctxt)
		{
			NaturalClass hcNaturalClass;
			if (!TryLoadNaturalClass(naturalClass, out hcNaturalClass))
			{
				ctxt = null;
				return false;
			}

			ctxt = new SimpleContext(hcNaturalClass, Enumerable.Empty<SymbolicFeatureValue>());
			return true;
		}

		private bool TryLoadNaturalClass(IPhNaturalClass naturalClass, out NaturalClass hcNaturalClass)
		{
			if (naturalClass == null)
			{
				hcNaturalClass = null;
				return false;
			}

			if (m_naturalClasses.TryGetValue(naturalClass, out hcNaturalClass))
			{
				return hcNaturalClass != null;
			}

			var segNC = naturalClass as IPhNCSegments;
			if (segNC != null)
			{
				var segments = new List<CharacterDefinition>();
				foreach (var phoneme in segNC.SegmentsRC)
				{
					CharacterDefinition cd;
					if (m_charDefs.TryGetValue(phoneme, out cd))
					{
						segments.Add(cd);
					}
					else
					{
						m_naturalClasses[naturalClass] = null;
						return false;
					}
				}

				hcNaturalClass = new SegmentNaturalClass(segments);
			}
			else
			{
				var featNC = (IPhNCFeatures)naturalClass;
				var fs = LoadFeatureStruct(featNC.FeaturesOA, m_language.PhonologicalFeatureSystem);
				hcNaturalClass = new NaturalClass(fs);
			}
			hcNaturalClass.Name = naturalClass.Abbreviation.BestAnalysisAlternative.Text;

			m_naturalClasses[naturalClass] = hcNaturalClass;
			return true;
		}

		private Segments Segments(string representation)
		{
			representation = FormatForm(representation);
			return new Segments(m_table, representation, Segment(representation));
		}
	}
}