using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SIL.Collections;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Validation;
using SIL.HermitCrab;
using SIL.HermitCrab.MorphologicalRules;
using SIL.HermitCrab.PhonologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.FieldWorks.WordWorks.Parser
{
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_cache and m_loadErrorsWriter are references")]
	internal class HCLoader
	{
		public static Language Load(SpanFactory<ShapeNode> spanFactory, FdoCache cache, XmlWriter loadErrorsWriter)
		{
			var loader = new HCLoader(spanFactory, cache, loadErrorsWriter);
			loader.LoadLanguage();
			return loader.m_language;
		}

		private static readonly FeatureStruct Null = FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo("^0").Value;
		private static readonly FeatureStruct MorphBdry = FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo("+").Value;
		private static readonly FeatureStruct Any = FeatureStruct.New().Value;

		private readonly SpanFactory<ShapeNode> m_spanFactory;
		private readonly FdoCache m_cache;
		private readonly Dictionary<IMoForm, List<Allomorph>> m_allomorphs;
		private readonly Dictionary<IMoMorphSynAnalysis, List<Morpheme>> m_morphemes;
		private readonly Dictionary<IMoStemName, StemName> m_stemNames;
		private readonly Dictionary<ICmObject, MprFeature> m_mprFeatures;

		private Language m_language;
		private SymbolTable m_table;
		private Stratum m_morphophonemic;
		private Stratum m_clitic;
		private ComplexFeature m_headFeature;
		private SymbolicFeature m_posFeature;

		private readonly XmlWriter m_loadErrorsWriter;
		private readonly PhonEnvRecognizer m_envValidator;
		private readonly Dictionary<string, IPhNaturalClass> m_naturalClasses;

		private HCLoader(SpanFactory<ShapeNode> spanFactory, FdoCache cache, XmlWriter loadErrorsWriter)
		{
			m_spanFactory = spanFactory;
			m_cache = cache;
			m_loadErrorsWriter = loadErrorsWriter;
			m_allomorphs = new Dictionary<IMoForm, List<Allomorph>>();
			m_morphemes = new Dictionary<IMoMorphSynAnalysis, List<Morpheme>>();
			m_stemNames = new Dictionary<IMoStemName, StemName>();
			m_mprFeatures = new Dictionary<ICmObject, MprFeature>();

			m_envValidator = new PhonEnvRecognizer(
				m_cache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(),
				m_cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());

			m_naturalClasses = new Dictionary<string, IPhNaturalClass>();
			foreach (IPhNaturalClass nc in m_cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS)
				m_naturalClasses[nc.Abbreviation.BestAnalysisAlternative.Text] = nc;
		}

		private void LoadLanguage()
		{
			m_loadErrorsWriter.WriteStartElement("LoadErrors");
			m_language = new Language { Name = m_cache.ProjectId.Name };

			var inflClassesGroup = new MprFeatureGroup { Name = "inflClasses", MatchType = MprFeatureGroupMatchType.Any };
			var posSymbols = new List<FeatureSymbol>();
			foreach (IPartOfSpeech pos in m_cache.LanguageProject.AllPartsOfSpeech)
			{
				posSymbols.Add(new FeatureSymbol(pos.Guid.ToString()) {Description = pos.Name.BestAnalysisAlternative.Text});
				foreach (IMoInflClass inflClass in pos.AllInflectionClasses)
					LoadMprFeature(inflClass, inflClassesGroup);
			}

			var prodRestrictsGroup = new MprFeatureGroup { Name = "exceptionFeatures", MatchType = MprFeatureGroupMatchType.All };
			foreach (ICmPossibility prodRestrict in m_cache.LanguageProject.MorphologicalDataOA.ProdRestrictOA.ReallyReallyAllPossibilities)
				LoadMprFeature(prodRestrict, prodRestrictsGroup);

			var lexEntryInflTypesGroup = new MprFeatureGroup { Name = "lexEntryInflTypes", MatchType = MprFeatureGroupMatchType.All };
			foreach (ILexEntryInflType inflType in m_cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().AllInstances())
				LoadMprFeature(inflType, lexEntryInflTypesGroup);

			m_posFeature = new SymbolicFeature("pos", posSymbols) { Description = "POS" };
			m_language.SyntacticFeatureSystem.Add(m_posFeature);
			m_headFeature = new ComplexFeature("head") { Description = "Head" };
			m_language.SyntacticFeatureSystem.Add(m_headFeature);
			LoadFeatureSystem(m_cache.LanguageProject.MsFeatureSystemOA, m_language.SyntacticFeatureSystem);

			LoadFeatureSystem(m_cache.LanguageProject.PhFeatureSystemOA, m_language.PhoneticFeatureSystem);

			LoadSymbolTable(m_cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0], m_language.PhoneticFeatureSystem);

			foreach (IMoStemName stemName in m_cache.ServiceLocator.GetInstance<IMoStemNameRepository>().AllInstances())
			{
				var pos = stemName.OwnerOfClass<IPartOfSpeech>();
				var regions = new List<FeatureStruct>();
				foreach (IFsFeatStruc fs in stemName.RegionsOC.Where(fs => !fs.IsEmpty))
				{
					var hcFS = new FeatureStruct();
					hcFS.AddValue(m_headFeature, LoadFeatureStruct(fs, m_language.SyntacticFeatureSystem));
					hcFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(pos));
					hcFS.Freeze();
					regions.Add(hcFS);
				}
				var hcStemName = new StemName(regions) { Name = stemName.Name.BestAnalysisAlternative.Text };
				m_stemNames[stemName] = hcStemName;
			}

			m_morphophonemic = new Stratum(m_table) { Name = "Morphophonemic", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };
			m_language.Strata.Add(m_morphophonemic);

			m_clitic = new Stratum(m_table) { Name = "Clitic", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };
			m_language.Strata.Add(m_clitic);

			m_language.Strata.Add(new Stratum(m_table) { Name = "Surface" });

			XElement parserParamsElem = XElement.Parse(m_cache.LanguageProject.MorphologicalDataOA.ParserParameters);
			XElement hcElem = parserParamsElem.Element("HC");
			bool noDefaultCompounding = hcElem != null && ((bool?) hcElem.Element("NoDefaultCompounding") ?? false);

			if (m_cache.LanguageProject.MorphologicalDataOA.CompoundRulesOS.Count == 0 && !noDefaultCompounding)
			{
				m_morphophonemic.MorphologicalRules.AddRange(DefaultCompoundingRules());
			}
			else
			{
				foreach (IMoCompoundRule compoundRule in m_cache.LanguageProject.MorphologicalDataOA.CompoundRulesOS.Where(r => !r.Disabled))
				{
					switch (compoundRule.ClassID)
					{
						case MoEndoCompoundTags.kClassId:
							m_morphophonemic.MorphologicalRules.Add(LoadEndoCompoundingRule((IMoEndoCompound) compoundRule));
							break;

						case MoExoCompoundTags.kClassId:
							m_morphophonemic.MorphologicalRules.AddRange(LoadExoCompoundingRule((IMoExoCompound) compoundRule));
							break;
					}
				}
			}

			foreach (ILexEntry entry in m_cache.LanguageProject.LexDbOA.Entries)
			{
				var stemAllos = new List<IMoStemAllomorph>();
				var cliticStemAllos = new List<IMoStemAllomorph>();
				var affixAllos = new List<IMoForm>();
				var cliticAffixAllos = new List<IMoForm>();

				foreach (IMoForm form in entry.AlternateFormsOS.Concat(entry.LexemeFormOA))
				{
					if (form == null)
						continue;

					if (IsValidLexEntryForm(form))
					{
						if (IsCliticType(form.MorphTypeRA))
							cliticStemAllos.Add((IMoStemAllomorph) form);
						else
							stemAllos.Add((IMoStemAllomorph) form);
					}

					if (IsValidRuleForm(form))
					{
						if (IsCliticType(form.MorphTypeRA))
							cliticAffixAllos.Add(form);
						else
							affixAllos.Add(form);
					}
				}

				if (stemAllos.Count > 0)
					LoadLexEntries(m_morphophonemic, entry, stemAllos);
				if (cliticStemAllos.Count > 0)
					LoadLexEntries(m_clitic, entry, cliticStemAllos);
				if (affixAllos.Count > 0)
					LoadMorphologicalRules(m_morphophonemic, entry, affixAllos);
				if (cliticAffixAllos.Count > 0)
					LoadMorphologicalRules(m_clitic, entry, cliticAffixAllos);
			}

			foreach (IMoInflAffixTemplate template in m_cache.ServiceLocator.GetInstance<IMoInflAffixTemplateRepository>().AllInstances().Where(t => !t.Disabled))
			{
				IMoInflAffixSlot[] slots = template.SuffixSlotsRS.Concat(template.PrefixSlotsRS.Reverse()).Where(s => s.Affixes.Any(msa => m_morphemes.ContainsKey(msa))).ToArray();
				if (slots.Length > 0)
					m_morphophonemic.AffixTemplates.Add(LoadAffixTemplate(template, slots));
			}

			bool notOnClitics = hcElem == null || ((bool?) hcElem.Element("NotOnClitics") ?? true);
			foreach (IPhSegmentRule prule in m_cache.LanguageProject.PhonologicalDataOA.PhonRulesOS.Where(r => !r.Disabled).OrderBy(r => r.OrderNumber))
			{
				switch (prule.ClassID)
				{
					case PhRegularRuleTags.kClassId:
						var regRule = (IPhRegularRule) prule;
						if (regRule.StrucDescOS.Count > 0 || regRule.RightHandSidesOS.Any(rhs => rhs.StrucChangeOS.Count > 0))
						{
							RewriteRule hcRegRule = LoadRewriteRule(regRule);
							m_morphophonemic.PhonologicalRules.Add(hcRegRule);
							if (!notOnClitics)
								m_clitic.PhonologicalRules.Add(hcRegRule);
						}
						break;

					case PhMetathesisRuleTags.kClassId:
						var metaRule = (IPhMetathesisRule) prule;
						if (metaRule.LeftSwitchIndex != -1 && metaRule.RightSwitchIndex != -1)
						{
							MetathesisRule hcMetaRule = LoadMetathesisRule(metaRule);
							m_morphophonemic.PhonologicalRules.Add(hcMetaRule);
							if (!notOnClitics)
								m_clitic.PhonologicalRules.Add(hcMetaRule);
						}
						break;
				}
			}

			foreach (IMoAlloAdhocProhib alloAdhocProhib in m_cache.ServiceLocator.GetInstance<IMoAlloAdhocProhibRepository>().AllInstances()
				.Where(a => !a.Disabled && a.FirstAllomorphRA != null && a.RestOfAllosRS.Count > 0))
			{
				LoadAllomorphCoOccurrenceRules(alloAdhocProhib);
			}

			foreach (IMoMorphAdhocProhib morphAdhocProhib in m_cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibRepository>().AllInstances()
				.Where(a => !a.Disabled && a.FirstMorphemeRA != null && a.RestOfMorphsRS.Count > 0))
			{
				LoadMorphemeCoOccurrenceRules(morphAdhocProhib);
			}

			m_loadErrorsWriter.WriteEndElement();
		}

		private bool HasValidRuleForm(ILexEntry entry)
		{
			if (entry.IsCircumfix() && entry.LexemeFormOA is IMoAffixAllomorph)
			{
				bool hasPrefix = false, hasSuffix = false;
				foreach (IMoForm form in entry.AlternateFormsOS.Where(IsValidRuleForm))
				{
					if (form.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphPrefix)
						hasPrefix = true;
					else if (form.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphSuffix)
						hasSuffix = true;
					if (hasPrefix && hasSuffix)
						return true;
				}
				return false;
			}
			return entry.AllAllomorphs.Any(IsValidRuleForm);
		}

		private bool IsValidRuleForm(IMoForm form)
		{
			var affixProcess = form as IMoAffixProcess;
			if (affixProcess != null)
				return affixProcess.InputOS.Count > 1 || affixProcess.OutputOS.Count > 1;

			string formStr = form.Form.VernacularDefaultWritingSystem.Text;
			if (form.IsAbstract || string.IsNullOrEmpty(formStr))
				return false;

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
						return ((IMoAffixAllomorph) form).PhoneEnvRC.Any(env => m_envValidator.Recognize(env.StringRepresentation.Text));
					return true;

				case MoMorphTypeTags.kMorphInfix:
				case MoMorphTypeTags.kMorphInfixingInterfix:
					return ((IMoAffixAllomorph) form).PositionRS.Any(env => m_envValidator.Recognize(env.StringRepresentation.Text));
			}

			return false;
		}

		private void LoadMprFeature(ICmObject obj, MprFeatureGroup group)
		{
			var feat = new MprFeature { Name = obj.ShortName };
			group.MprFeatures.Add(feat);
			m_mprFeatures[obj] = feat;
		}

		private bool IsValidLexEntryForm(IMoForm form)
		{
			string formStr = form.Form.VernacularDefaultWritingSystem.Text;
			if (form.IsAbstract || string.IsNullOrEmpty(formStr))
				return false;

			return IsStemType(form.MorphTypeRA) || IsCliticType(form.MorphTypeRA);
		}

		private void WriteLoadError(Exception ex, int hvo)
		{
			m_loadErrorsWriter.WriteStartElement("LoadError");
			var ise = ex as InvalidShapeException;
			if (ise != null)
			{
				m_loadErrorsWriter.WriteAttributeString("type", "invalid-shape");
				m_loadErrorsWriter.WriteElementString("Form", ise.String);
				m_loadErrorsWriter.WriteElementString("Position", ise.Position.ToString(CultureInfo.InvariantCulture));
				m_loadErrorsWriter.WriteElementString("Hvo", hvo.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				m_loadErrorsWriter.WriteAttributeString("type", "unknown");
				m_loadErrorsWriter.WriteString(ex.Message);
			}
			m_loadErrorsWriter.WriteEndElement();
		}

		private static bool IsStemType(IMoMorphType type)
		{
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
			foreach (IMoStemMsa msa in entry.MorphoSyntaxAnalysesOC.OfType<IMoStemMsa>())
				AddEntry(stratum, LoadLexEntry(entry, msa, allos), msa);

			if (entry.AllSenses.Count == 0)
			{
				foreach (ILexEntryRef lexEntryRef in entry.EntryRefsOS)
				{
					foreach (ICmObject component in lexEntryRef.ComponentLexemesRS)
					{
						var mainEntry = component as ILexEntry;
						if (mainEntry != null)
						{
							foreach (IMoStemMsa msa in mainEntry.MorphoSyntaxAnalysesOC.OfType<IMoStemMsa>())
								AddEntry(stratum, LoadLexEntryOfVariant(lexEntryRef, msa, allos, mainEntry.SenseWithMsa(msa)), msa);
						}
						else
						{
							var mainSense = (ILexSense) component;
							var msa = (IMoStemMsa) mainSense.MorphoSyntaxAnalysisRA;
							AddEntry(stratum, LoadLexEntryOfVariant(lexEntryRef, msa, allos, mainSense), msa);
						}
					}
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

		private LexEntry LoadLexEntry(ILexEntry entry, IMoStemMsa msa, IList<IMoStemAllomorph> allos)
		{
			var hcEntry = new LexEntry();

			IMoInflClass inflClass = GetInflClass(msa);
			if (inflClass != null)
				hcEntry.MprFeatures.AddRange(LoadAllInflClasses(inflClass));

			foreach (ICmPossibility prodRestrict in msa.ProdRestrictRC)
				hcEntry.MprFeatures.Add(m_mprFeatures[prodRestrict]);

			hcEntry.Gloss = entry.SensesOS[0].Gloss.BestAnalysisAlternative.Text;

			var fs = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
				fs.AddValue(m_posFeature, m_posFeature.PossibleSymbols[msa.PartOfSpeechRA.Guid.ToString()]);
			if (msa.MsFeaturesOA != null && !msa.MsFeaturesOA.IsEmpty)
				fs.AddValue(m_headFeature, LoadFeatureStruct(msa.MsFeaturesOA, m_language.SyntacticFeatureSystem));
			fs.Freeze();
			hcEntry.SyntacticFeatureStruct = fs;

			hcEntry.Properties["ID"] = msa.Hvo;

			foreach (IMoStemAllomorph allo in allos)
			{
				try
				{
					RootAllomorph hcAllo = LoadRootAllomorph(allo);
					hcEntry.Allomorphs.Add(hcAllo);
					m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcAllo);
				}
				catch (InvalidShapeException ise)
				{
					WriteLoadError(ise, msa.Hvo);
				}
			}

			return hcEntry;
		}

		private LexEntry LoadLexEntryOfVariant(ILexEntryRef entryRef, IMoStemMsa msa, IList<IMoStemAllomorph> allos, ILexSense sense)
		{
			var hcEntry = new LexEntry();

			IMoInflClass inflClass = GetInflClass(msa);
			if (inflClass != null)
				hcEntry.MprFeatures.AddRange(LoadAllInflClasses(inflClass));

			foreach (ICmPossibility prodRestrict in msa.ProdRestrictRC)
				hcEntry.MprFeatures.Add(m_mprFeatures[prodRestrict]);

			ILexEntryInflType inflType = entryRef.VariantEntryTypesRS.OfType<ILexEntryInflType>().FirstOrDefault();
			// TODO: irregularly inflected forms should be handled by rule blocking in HC
			if (inflType != null)
				hcEntry.MprFeatures.Add(m_mprFeatures[inflType]);

			var glossSB = new StringBuilder();
			if (inflType != null)
			{
				string prepend = inflType.GlossPrepend.BestAnalysisAlternative.Text;
				if (prepend != "***")
					glossSB.Append(prepend);
			}
			glossSB.Append(sense.Gloss.BestAnalysisAlternative.Text);
			if (inflType != null)
			{
				string append = inflType.GlossAppend.BestAnalysisAlternative.Text;
				if (append != "***")
					glossSB.Append(append);
			}
			hcEntry.Gloss = glossSB.ToString();

			var fs = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
				fs.AddValue(m_posFeature, m_posFeature.PossibleSymbols[msa.PartOfSpeechRA.Guid.ToString()]);
			FeatureStruct headFS = null;
			if (msa.MsFeaturesOA != null && !msa.MsFeaturesOA.IsEmpty)
				headFS = LoadFeatureStruct(msa.MsFeaturesOA, m_language.SyntacticFeatureSystem);
			if (inflType != null)
			{
				if (inflType.SlotsRC.Count == 0 && inflType.InflFeatsOA != null && !inflType.InflFeatsOA.IsEmpty)
				{
					FeatureStruct inflFS = LoadFeatureStruct(inflType.InflFeatsOA, m_language.SyntacticFeatureSystem);
					if (headFS == null)
						headFS = inflFS;
					else
						headFS.Add(inflFS);
				}
			}
			if (headFS != null)
				fs.AddValue(m_headFeature, headFS);
			fs.Freeze();
			hcEntry.SyntacticFeatureStruct = fs;

			hcEntry.Properties["ID"] = msa.Hvo;
			hcEntry.Properties["LexEntryRefID"] = entryRef.Hvo;

			foreach (IMoStemAllomorph allo in allos)
			{
				try
				{
					RootAllomorph hcAllo = LoadRootAllomorph(allo);
					hcEntry.Allomorphs.Add(hcAllo);
					m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcAllo);
				}
				catch (InvalidShapeException ise)
				{
					WriteLoadError(ise, msa.Hvo);
				}
			}

			return hcEntry;
		}

		private RootAllomorph LoadRootAllomorph(IMoStemAllomorph allo)
		{
			Shape shape = m_table.Segment(FormatForm(allo.Form.VernacularDefaultWritingSystem.Text));
			var hcAllo = new RootAllomorph(shape);

			foreach (IPhEnvironment env in allo.PhoneEnvRC.Where(e => m_envValidator.Recognize(e.StringRepresentation.Text)))
			{
				Tuple<string, string> contexts = SplitEnvironment(env);
				hcAllo.RequiredEnvironments.Add(new AllomorphEnvironment(m_spanFactory, LoadEnvironmentPattern(contexts.Item1, true),
					LoadEnvironmentPattern(contexts.Item2, false)) {Name = env.StringRepresentation.Text});
			}

			if (allo.StemNameRA != null)
				hcAllo.StemName = m_stemNames[allo.StemNameRA];

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
				return;

			foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
			{
				AffixProcessRule mrule;

				switch (msa.ClassID)
				{
					case MoDerivAffMsaTags.kClassId:
						AddMorphologicalRule(stratum, LoadDerivAffixProcessRule(entry, (IMoDerivAffMsa) msa, allos), msa);
						break;

					case MoInflAffMsaTags.kClassId:
						var inflMsa = (IMoInflAffMsa) msa;
						AddMorphologicalRule(inflMsa.SlotsRC.Count == 0 ? stratum : null, LoadInflAffixProcessRule(entry, inflMsa, allos), msa);
						break;

					case MoUnclassifiedAffixMsaTags.kClassId:
						AddMorphologicalRule(stratum, LoadUnclassifiedAffixProcessRule(entry, (IMoUnclassifiedAffixMsa) msa, allos), msa);
						break;

					case MoStemMsaTags.kClassId:
						AddMorphologicalRule(stratum, LoadCliticAffixProcessRule(entry, (IMoStemMsa) msa, allos), msa);
						break;
				}
			}
		}

		private void AddMorphologicalRule(Stratum stratum, AffixProcessRule rule, IMoMorphSynAnalysis msa)
		{
			if (rule.Allomorphs.Count > 0)
			{
				if (stratum != null)
					stratum.MorphologicalRules.Add(rule);
				m_morphemes.GetValue(msa, () => new List<Morpheme>()).Add(rule);
			}
		}

		private AffixProcessRule LoadDerivAffixProcessRule(ILexEntry entry, IMoDerivAffMsa msa, IList<IMoForm> allos)
		{
			var mrule = new AffixProcessRule { Name = entry.ShortName };

			var requiredFS = new FeatureStruct();
			if (msa.FromPartOfSpeechRA != null)
				requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(msa.FromPartOfSpeechRA));
			if (msa.FromMsFeaturesOA != null && !msa.FromMsFeaturesOA.IsEmpty)
				requiredFS.AddValue(m_headFeature, LoadFeatureStruct(msa.FromMsFeaturesOA, m_language.SyntacticFeatureSystem));
			requiredFS.Freeze();
			mrule.RequiredSyntacticFeatureStruct = requiredFS;

			var outFS = new FeatureStruct();
			if (msa.ToPartOfSpeechRA != null)
				outFS.AddValue(m_posFeature, m_posFeature.PossibleSymbols[msa.ToPartOfSpeechRA.Guid.ToString()]);
			if (msa.ToMsFeaturesOA != null && !msa.ToMsFeaturesOA.IsEmpty)
				outFS.AddValue(m_headFeature, LoadFeatureStruct(msa.ToMsFeaturesOA, m_language.SyntacticFeatureSystem));
			outFS.Freeze();
			mrule.OutSyntacticFeatureStruct = outFS;

			var requiredMprFeatures = new List<MprFeature>();
			if (msa.FromInflectionClassRA != null)
				requiredMprFeatures.AddRange(LoadAllInflClasses(msa.FromInflectionClassRA));

			foreach (ICmPossibility prodRestrict in msa.FromProdRestrictRC)
				requiredMprFeatures.Add(m_mprFeatures[prodRestrict]);

			var outMprFeatures = new List<MprFeature>();
			if (msa.ToInflectionClassRA != null)
				outMprFeatures.Add(m_mprFeatures[msa.ToInflectionClassRA]);

			foreach (ICmPossibility prodRestrict in msa.ToProdRestrictRC)
				outMprFeatures.Add(m_mprFeatures[prodRestrict]);

			if (msa.FromStemNameRA != null)
				mrule.RequiredStemName = m_stemNames[msa.FromStemNameRA];

			mrule.Gloss = entry.SensesOS[0].Gloss.BestAnalysisAlternative.Text;

			mrule.Properties["ID"] = msa.Hvo;

			foreach (AffixProcessAllomorph hcAllo in LoadAffixProcessAllomorphs(msa, allos))
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
			var mrule = new AffixProcessRule { Name = entry.ShortName };

			var requiredFS = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
				requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(msa.PartOfSpeechRA));
			requiredFS.Freeze();
			mrule.RequiredSyntacticFeatureStruct = requiredFS;

			var outFS = new FeatureStruct();
			if (msa.InflFeatsOA != null && !msa.InflFeatsOA.IsEmpty)
				outFS.AddValue(m_headFeature, LoadFeatureStruct(msa.InflFeatsOA, m_language.SyntacticFeatureSystem));
			outFS.Freeze();
			mrule.OutSyntacticFeatureStruct = outFS;

			var requiredMprFeatures = new List<MprFeature>();
			foreach (ICmPossibility prodRestrict in msa.FromProdRestrictRC)
				requiredMprFeatures.Add(m_mprFeatures[prodRestrict]);

			mrule.Gloss = entry.SensesOS[0].Gloss.BestAnalysisAlternative.Text;

			mrule.Properties["ID"] = msa.Hvo;

			foreach (AffixProcessAllomorph hcAllo in LoadAffixProcessAllomorphs(msa, allos))
			{
				hcAllo.RequiredMprFeatures.AddRange(requiredMprFeatures);
				mrule.Allomorphs.Add(hcAllo);
			}

			return mrule;
		}

		private AffixProcessRule LoadUnclassifiedAffixProcessRule(ILexEntry entry, IMoUnclassifiedAffixMsa msa, IList<IMoForm> allos)
		{
			var mrule = new AffixProcessRule { Name = entry.ShortName };

			var requiredFS = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
				requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(msa.PartOfSpeechRA));
			requiredFS.Freeze();
			mrule.RequiredSyntacticFeatureStruct = requiredFS;

			mrule.Gloss = entry.SensesOS[0].Gloss.BestAnalysisAlternative.Text;

			mrule.Properties["ID"] = msa.Hvo;

			foreach (AffixProcessAllomorph hcAllo in LoadAffixProcessAllomorphs(msa, allos))
				mrule.Allomorphs.Add(hcAllo);

			return mrule;
		}

		private AffixProcessRule LoadCliticAffixProcessRule(ILexEntry entry, IMoStemMsa msa, IList<IMoForm> allos)
		{
			var mrule = new AffixProcessRule { Name = entry.ShortName };

			var requiredFS = new FeatureStruct();
			if (msa.FromPartsOfSpeechRC.Count > 0)
				requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(msa.FromPartsOfSpeechRC));
			requiredFS.Freeze();
			mrule.RequiredSyntacticFeatureStruct = requiredFS;

			mrule.Gloss = entry.SensesOS[0].Gloss.BestAnalysisAlternative.Text;

			mrule.Properties["ID"] = msa.Hvo;

			foreach (AffixProcessAllomorph hcAllo in LoadAffixProcessAllomorphs(msa, allos))
				mrule.Allomorphs.Add(hcAllo);

			return mrule;
		}

		private IEnumerable<AffixProcessAllomorph> LoadAffixProcessAllomorphs(IMoMorphSynAnalysis msa, IList<IMoForm> allos)
		{
			var entry = msa.OwnerOfClass<ILexEntry>();
			if (entry.IsCircumfix() && entry.LexemeFormOA is IMoAffixAllomorph)
			{
				foreach (IMoAffixAllomorph prefixAllo in allos.OfType<IMoAffixAllomorph>().Where(a => a.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphPrefix))
				{
					MprFeature[] requiredMprFeatures = LoadAllInflClasses(prefixAllo.InflectionClassesRC).ToArray();
					foreach (IMoAffixAllomorph suffixAllo in allos.OfType<IMoAffixAllomorph>().Where(a => a.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphSuffix))
					{
						foreach (IPhEnvironment prefixEnv in GetAffixAllomorphEnvironments(prefixAllo))
						{
							foreach (IPhEnvironment suffixEnv in GetAffixAllomorphEnvironments(suffixAllo))
							{
								AffixProcessAllomorph hcAllo = null;
								try
								{
									hcAllo = LoadCircumfixAffixProcessAllomorph(prefixAllo, prefixEnv, suffixAllo, suffixEnv);
									hcAllo.RequiredMprFeatures.AddRange(requiredMprFeatures);
									m_allomorphs.GetValue(entry.LexemeFormOA, () => new List<Allomorph>()).Add(hcAllo);
								}
								catch (InvalidShapeException ise)
								{
									WriteLoadError(ise, msa.Hvo);
								}
								if (hcAllo != null)
									yield return hcAllo;
							}
						}
					}
				}
			}
			else
			{
				foreach (IMoForm allo in allos)
				{
					switch (allo.ClassID)
					{
						case MoAffixProcessTags.kClassId:
							var affixProcess = (IMoAffixProcess) allo;
							AffixProcessAllomorph hcAffixProcessAllo = null;
							try
							{
								hcAffixProcessAllo = LoadAffixProcessAllomorph(affixProcess);
								hcAffixProcessAllo.RequiredMprFeatures.AddRange(LoadAllInflClasses(affixProcess.InflectionClassesRC));
								m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcAffixProcessAllo);
							}
							catch (InvalidShapeException ise)
							{
								WriteLoadError(ise, msa.Hvo);
							}
							if (hcAffixProcessAllo != null)
								yield return hcAffixProcessAllo;
							break;

						case MoAffixAllomorphTags.kClassId:
							var affixAllo = (IMoAffixAllomorph) allo;
							MprFeature[] requiredMprFeatures = LoadAllInflClasses(affixAllo.InflectionClassesRC).ToArray();
							foreach (IPhEnvironment env in GetAffixAllomorphEnvironments(affixAllo))
							{
								AffixProcessAllomorph hcAffixAllo = null;
								try
								{
									hcAffixAllo = LoadFormAffixProcessAllomorph(affixAllo, env);
									hcAffixAllo.RequiredMprFeatures.AddRange(requiredMprFeatures);
									var requiredFS = new FeatureStruct();
									if (affixAllo.MsEnvFeaturesOA != null && !affixAllo.MsEnvFeaturesOA.IsEmpty)
										requiredFS.AddValue(m_headFeature, LoadFeatureStruct(affixAllo.MsEnvFeaturesOA, m_language.SyntacticFeatureSystem));
									requiredFS.Freeze();
									hcAffixAllo.RequiredSyntacticFeatureStruct = requiredFS;
									m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcAffixAllo);
								}
								catch (InvalidShapeException ise)
								{
									WriteLoadError(ise, msa.Hvo);
								}
								if (hcAffixAllo != null)
									yield return hcAffixAllo;
							}
							break;

						case MoStemAllomorphTags.kClassId:
							AffixProcessAllomorph hcStemAllo = null;
							try
							{
								hcStemAllo = LoadFormAffixProcessAllomorph(allo, null);
								m_allomorphs.GetValue(allo, () => new List<Allomorph>()).Add(hcStemAllo);
							}
							catch (InvalidShapeException ise)
							{
								WriteLoadError(ise, msa.Hvo);
							}
							if (hcStemAllo != null)
								yield return hcStemAllo;
							break;
					}
				}
			}
		}

		private IEnumerable<IPhEnvironment> GetAffixAllomorphEnvironments(IMoAffixAllomorph allo)
		{
			bool hasBlankEnv = allo.PhoneEnvRC.Count == 0 && allo.PositionRS.Count == 0;
			foreach (IPhEnvironment env in allo.PhoneEnvRC.Concat(allo.PositionRS))
			{
				if (m_envValidator.Recognize(env.StringRepresentation.Text))
					yield return env;
				else
					hasBlankEnv = true;
			}

			if (hasBlankEnv)
				yield return null;
		}

		private AffixProcessAllomorph LoadCircumfixAffixProcessAllomorph(IMoAffixAllomorph prefixAllo, IPhEnvironment prefixEnv,
			IMoAffixAllomorph suffixAllo, IPhEnvironment suffixEnv)
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
					Tuple<string, string> prefixContexts = SplitEnvironment(prefixEnv);
					pattern.Children.AddRange(LoadPatternNodes(prefixContexts.Item2));

					if (!string.IsNullOrEmpty(prefixContexts.Item1))
						leftEnvPattern = LoadEnvironmentPattern(prefixContexts.Item1, true);
				}
				pattern.Children.AddRange(AnyStar());
				if (suffixEnv != null)
				{
					Tuple<string, string> suffixContexts = SplitEnvironment(suffixEnv);
					pattern.Children.AddRange(LoadPatternNodes(suffixContexts.Item1));
					pattern.Children.Add(SuffixNull());

					if (!string.IsNullOrEmpty(suffixContexts.Item2))
						rightEnvPattern = LoadEnvironmentPattern(suffixContexts.Item2, false);
				}
			}
			pattern.Freeze();
			hcAllo.Lhs.Add(pattern);

			hcAllo.Rhs.Add(new InsertShape(m_table, FormatForm(prefixAllo.Form.VernacularDefaultWritingSystem.Text) + "+"));
			hcAllo.Rhs.Add(new CopyFromInput("stem"));
			hcAllo.Rhs.Add(new InsertShape(m_table, "+" + FormatForm(suffixAllo.Form.VernacularDefaultWritingSystem.Text)));

			if (leftEnvPattern != null || rightEnvPattern != null)
			{
				string name;
				if (leftEnvPattern != null && rightEnvPattern == null)
					name = prefixEnv.StringRepresentation.Text;
				else if (leftEnvPattern == null)
					name = suffixEnv.StringRepresentation.Text;
				else
					name = string.Format("{0}, {1}", prefixEnv.StringRepresentation.Text, suffixEnv.StringRepresentation.Text);
				hcAllo.RequiredEnvironments.Add(new AllomorphEnvironment(m_spanFactory, leftEnvPattern, rightEnvPattern) {Name = name});
			}

			hcAllo.Properties["ID"] = prefixAllo.Hvo;
			hcAllo.Properties["ID2"] = suffixAllo.Hvo;
			if (prefixEnv != null)
				hcAllo.Properties["PrefixEnv"] = prefixEnv.StringRepresentation.Text;
			if (suffixEnv != null)
				hcAllo.Properties["SuffixEnv"] = suffixEnv.StringRepresentation.Text;
			return hcAllo;
		}

		private AffixProcessAllomorph LoadAffixProcessAllomorph(IMoAffixProcess allo)
		{
			var hcAllo = new AffixProcessAllomorph();
			foreach (IPhContextOrVar ctxtOrVar in allo.InputOS)
			{
				var var = ctxtOrVar as IPhVariable;
				if (var != null)
				{
					var pattern = new Pattern<Word, ShapeNode>(var.Hvo.ToString(CultureInfo.InvariantCulture), AnyStar());
					pattern.Freeze();
					hcAllo.Lhs.Add(pattern);
				}
				else
				{
					PatternNode<Word, ShapeNode> n;
					if (LoadPatternNode((IPhPhonContext) ctxtOrVar, out n))
					{
						var pattern = new Pattern<Word, ShapeNode>(ctxtOrVar.Hvo.ToString(CultureInfo.InvariantCulture), n);
						pattern.Freeze();
						hcAllo.Lhs.Add(pattern);
					}
				}
			}

			foreach (IMoRuleMapping mapping in allo.OutputOS)
			{
				switch (mapping.ClassID)
				{
					case MoInsertNCTags.kClassId:
						var insertNC = (IMoInsertNC) mapping;
						if (insertNC.ContentRA != null)
							hcAllo.Rhs.Add(new InsertShapeNode(LoadNaturalClassFeatureStruct(insertNC.ContentRA)));
						break;

					case MoCopyFromInputTags.kClassId:
						var copyFromInput = (IMoCopyFromInput) mapping;
						if (copyFromInput.ContentRA != null)
							hcAllo.Rhs.Add(new CopyFromInput(copyFromInput.ContentRA.Hvo.ToString(CultureInfo.InvariantCulture)));
						break;

					case MoInsertPhonesTags.kClassId:
						var insertPhones = (IMoInsertPhones) mapping;
						if (insertPhones.ContentRS.Count > 0)
							hcAllo.Rhs.Add(new InsertShape(m_table, string.Concat(insertPhones.ContentRS.Select(u => u.CodesOS[0].Representation.BestVernacularAlternative.Text))));
						break;

					case MoModifyFromInputTags.kClassId:
						var modifyFromInput = (IMoModifyFromInput) mapping;
						if (modifyFromInput.ContentRA != null && modifyFromInput.ModificationRA != null)
						{
							hcAllo.Rhs.Add(new ModifyFromInput(modifyFromInput.ContentRA.Hvo.ToString(CultureInfo.InvariantCulture),
								LoadNaturalClassFeatureStruct(modifyFromInput.ModificationRA)));
						}
						break;
				}
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

			hcAllo.Properties["ID"] = allo.Hvo;
			return hcAllo;
		}

		private AffixProcessAllomorph LoadFormAffixProcessAllomorph(IMoForm allo, IPhEnvironment env)
		{
			var hcAllo = new AffixProcessAllomorph();
			string form = allo.Form.VernacularDefaultWritingSystem.Text;
			Tuple<string, string> contexts = SplitEnvironment(env);
			if (form.Contains("["))
			{
				if (form.Contains("[...]"))
				{
					var stemPattern = new Pattern<Word, ShapeNode>("stem", AnyPlus());
					stemPattern.Freeze();
					hcAllo.Lhs.Add(stemPattern);

					hcAllo.Rhs.Add(new CopyFromInput("stem"));
					int beforePos = form.IndexOf('[');
					string beforeStr = form.Substring(0, beforePos);
					hcAllo.Rhs.Add(new InsertShape(m_table, "+" + beforeStr));
					hcAllo.Rhs.Add(new CopyFromInput("stem"));
					int afterPos = form.IndexOf(']');
					string afterStr = form.Substring(afterPos + 1);
					if (!string.IsNullOrEmpty(afterStr))
						hcAllo.Rhs.Add(new InsertShape(m_table, afterStr));

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
					string environment = "/_" + form;
					// A form containing a reduplication expression should look like an environment
					if (!m_envValidator.Recognize(environment))
						throw new InvalidReduplicationEnvironmentException(m_envValidator.ErrorMessage, form);

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
							hcAllo.Rhs.Add(new InsertShape(m_table, "+"));
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
							hcAllo.Rhs.Add(new InsertShape(m_table, "+"));
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
							leftInfixPattern.Children.Add(PrefixNull());
						else
							leftInfixPattern.Children.AddRange(AnyStar());
						leftInfixPattern.Children.AddRange(LoadPatternNodes(contexts.Item1));
						leftInfixPattern.Freeze();
						hcAllo.Lhs.Add(leftInfixPattern);

						var rightInfixPattern = new Pattern<Word, ShapeNode>("right");
						rightInfixPattern.Children.AddRange(LoadPatternNodes(contexts.Item2));
						if (contexts.Item2.EndsWith("#"))
							rightInfixPattern.Children.Add(SuffixNull());
						else
							rightInfixPattern.Children.AddRange(AnyStar());
						rightInfixPattern.Freeze();
						hcAllo.Lhs.Add(rightInfixPattern);

						hcAllo.Rhs.Add(new CopyFromInput("left"));
						hcAllo.Rhs.Add(new InsertShape(m_table, "+" + FormatForm(form) + "+"));
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
								suffixPattern.Children.Add(PrefixNull());
							else
								suffixPattern.Children.AddRange(AnyStar());

							suffixPattern.Children.AddRange(LoadPatternNodes(contexts.Item1));
							suffixPattern.Children.Add(SuffixNull());
						}
						suffixPattern.Freeze();
						hcAllo.Lhs.Add(suffixPattern);

						hcAllo.Rhs.Add(new CopyFromInput("stem"));
						hcAllo.Rhs.Add(new InsertShape(m_table, "+" + FormatForm(form)));

						if (!string.IsNullOrEmpty(contexts.Item2))
							hcAllo.RequiredEnvironments.Add(new AllomorphEnvironment(m_spanFactory, null, LoadEnvironmentPattern(contexts.Item2, false)) {Name = env.StringRepresentation.Text});
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
								prefixPattern.Children.Add(SuffixNull());
							else
								prefixPattern.Children.AddRange(AnyStar());
						}
						prefixPattern.Freeze();
						hcAllo.Lhs.Add(prefixPattern);

						hcAllo.Rhs.Add(new InsertShape(m_table, FormatForm(form) + "+"));
						hcAllo.Rhs.Add(new CopyFromInput("stem"));

						if (!string.IsNullOrEmpty(contexts.Item1))
							hcAllo.RequiredEnvironments.Add(new AllomorphEnvironment(m_spanFactory, LoadEnvironmentPattern(contexts.Item1, true), null) {Name = env.StringRepresentation.Text});
						break;
				}
			}

			hcAllo.Properties["ID"] = allo.Hvo;
			if (env != null)
				hcAllo.Properties["Env"] = env.StringRepresentation.Text;
			return hcAllo;
		}

		private IEnumerable<Pattern<Word, ShapeNode>> LoadReduplicationPatterns(string patternStr)
		{
			foreach (string token in TokenizeContext(patternStr))
			{
				if (token.StartsWith("["))
				{
					int caretPos = token.IndexOf('^');
					string ncAbbr = token.Substring(1, caretPos - 1).Trim();
					IPhNaturalClass nc = m_naturalClasses[ncAbbr];
					var pattern = new Pattern<Word, ShapeNode>(token.Substring(1, token.Length - 2).Trim(), new Constraint<Word, ShapeNode>(LoadNaturalClassFeatureStruct(nc)));
					pattern.Freeze();
					yield return pattern;
				}
			}
		}

		private IEnumerable<MorphologicalOutputAction> LoadReduplicationOutputActions(string patternStr)
		{
			foreach (string token in TokenizeContext(patternStr))
			{
				if (token.StartsWith("["))
				{
					yield return new CopyFromInput(token.Substring(1, token.Length - 2).Trim());
				}
				else
				{
					yield return new InsertShape(m_table, token.Trim());
				}
			}
		}

		private AffixTemplate LoadAffixTemplate(IMoInflAffixTemplate template, IList<IMoInflAffixSlot> slots)
		{
			var hcTemplate = new AffixTemplate { Name = template.Name.BestAnalysisAlternative.Text, IsFinal = template.Final };

			var requiredFS = new FeatureStruct();
			requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(template.OwnerOfClass<IPartOfSpeech>()));
			requiredFS.Freeze();
			hcTemplate.RequiredSyntacticFeatureStruct = requiredFS;
			hcTemplate.Properties["ID"] = template.Hvo;

			foreach (IMoInflAffixSlot slot in slots)
			{
				ILexEntryInflType type = slot.ReferringObjects.OfType<ILexEntryInflType>().FirstOrDefault();
				var hcSlot = new AffixTemplateSlot { Name = slot.Name.BestAnalysisAlternative.Text, Optional = slot.Optional };
				foreach (IMoInflAffMsa msa in slot.Affixes)
				{
					List<Morpheme> morphemes;
					if (m_morphemes.TryGetValue(msa, out morphemes))
					{
						AffixProcessRule mrule = morphemes.OfType<AffixProcessRule>().FirstOrDefault();
						if (mrule != null)
						{
							if (type != null)
							{
								// block slot from applying to irregularly inflected forms
								foreach (AffixProcessAllomorph allo in mrule.Allomorphs)
									allo.ExcludedMprFeatures.Add(m_mprFeatures[type]);
							}
							hcSlot.Rules.Add(mrule);
						}
					}
				}

				// add a null affix to the required slot so that irregularly inflected forms can parse correctly
				// TODO: this really should be handled using rule blocking in HC
				if (type != null && !slot.Optional)
					hcSlot.Rules.Add(LoadNullAffixProcessRule(type, template, slot));

				hcTemplate.Slots.Add(hcSlot);
			}

			return hcTemplate;
		}

		private AffixProcessRule LoadNullAffixProcessRule(ILexEntryInflType type, IMoInflAffixTemplate template, IMoInflAffixSlot slot)
		{
			var mrule = new AffixProcessRule { Name = "Null" };

			var outFS = new FeatureStruct();
			if (type.InflFeatsOA != null && !type.InflFeatsOA.IsEmpty)
				outFS.AddValue(m_headFeature, LoadFeatureStruct(type.InflFeatsOA, m_language.SyntacticFeatureSystem));
			outFS.Freeze();
			mrule.OutSyntacticFeatureStruct = outFS;

			var msubrule = new AffixProcessAllomorph();

			msubrule.RequiredMprFeatures.Add(m_mprFeatures[type]);

			var stemPattern = new Pattern<Word, ShapeNode>("stem");
			stemPattern.Children.AddRange(AnyPlus());
			stemPattern.Freeze();
			msubrule.Lhs.Add(stemPattern);

			bool isPrefix = template.PrefixSlotsRS.Contains(slot);

			if (isPrefix)
				msubrule.Rhs.Add(new InsertShape(m_table, "^0+"));
			msubrule.Rhs.Add(new CopyFromInput("stem"));
			if (!isPrefix)
				msubrule.Rhs.Add(new InsertShape(m_table, "+^0"));

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
			csubruleLeft.Rhs.Add(new InsertShape(m_table, "+"));
			csubruleLeft.Rhs.Add(new CopyFromInput("nonhead"));
			compLeft.Subrules.Add(csubruleLeft);

			yield return compLeft;

			var compRight = new CompoundingRule { Name = "Default Right Head Compounding" };
			var csubruleRight = new CompoundingSubrule();

			csubruleRight.HeadLhs.Add(headPattern);
			csubruleRight.NonHeadLhs.Add(nonheadPattern);

			csubruleRight.Rhs.Add(new CopyFromInput("nonhead"));
			csubruleRight.Rhs.Add(new InsertShape(m_table, "+"));
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
					headRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.RightMsaOA.PartOfSpeechRA));
				if (compoundRule.LeftMsaOA.PartOfSpeechRA != null)
					nonheadRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.LeftMsaOA.PartOfSpeechRA));
			}
			else
			{
				if (compoundRule.RightMsaOA.PartOfSpeechRA != null)
					nonheadRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.RightMsaOA.PartOfSpeechRA));
				if (compoundRule.LeftMsaOA.PartOfSpeechRA != null)
					headRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.LeftMsaOA.PartOfSpeechRA));
			}
			headRequiredFS.Freeze();
			nonheadRequiredFS.Freeze();

			var outFS = new FeatureStruct();
			if (compoundRule.OverridingMsaOA.PartOfSpeechRA != null)
				outFS.AddValue(m_posFeature, m_posFeature.PossibleSymbols[compoundRule.OverridingMsaOA.PartOfSpeechRA.Guid.ToString()]);
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
					Properties = {{"ID", compoundRule.Hvo}}
				};

			var subrule = new CompoundingSubrule();

			if (compoundRule.OverridingMsaOA.InflectionClassRA != null)
				subrule.OutMprFeatures.Add(m_mprFeatures[compoundRule.OverridingMsaOA.InflectionClassRA]);

			subrule.HeadLhs.Add(headPattern);
			subrule.NonHeadLhs.Add(nonheadPattern);

			subrule.Rhs.Add(new CopyFromInput(compoundRule.HeadLast ? "nonhead" : "head"));
			subrule.Rhs.Add(new InsertShape(m_table, "+"));
			subrule.Rhs.Add(new CopyFromInput(compoundRule.HeadLast ? "head" : "nonhead"));

			hcCompoundRule.Subrules.Add(subrule);
			return hcCompoundRule;
		}

		private IEnumerable<CompoundingRule> LoadExoCompoundingRule(IMoExoCompound compoundRule)
		{
			var rightRequiredFS = new FeatureStruct();
			if (compoundRule.RightMsaOA.PartOfSpeechRA != null)
				rightRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.RightMsaOA.PartOfSpeechRA));
			rightRequiredFS.Freeze();
			var leftRequiredFS = new FeatureStruct();
			if (compoundRule.LeftMsaOA.PartOfSpeechRA != null)
				leftRequiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(compoundRule.LeftMsaOA.PartOfSpeechRA));
			leftRequiredFS.Freeze();
			var outFS = new FeatureStruct();
			if (compoundRule.ToMsaOA.PartOfSpeechRA != null)
				outFS.AddValue(m_posFeature, m_posFeature.PossibleSymbols[compoundRule.ToMsaOA.PartOfSpeechRA.Guid.ToString()]);
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
					Properties = {{"ID", compoundRule.Hvo}}
				};

			var rightSubrule = new CompoundingSubrule();

			if (compoundRule.ToMsaOA.InflectionClassRA != null)
				rightSubrule.OutMprFeatures.Add(m_mprFeatures[compoundRule.ToMsaOA.InflectionClassRA]);

			rightSubrule.HeadLhs.Add(headPattern);
			rightSubrule.NonHeadLhs.Add(nonheadPattern);

			rightSubrule.Rhs.Add(new CopyFromInput("nonhead"));
			rightSubrule.Rhs.Add(new InsertShape(m_table, "+"));
			rightSubrule.Rhs.Add(new CopyFromInput("head"));

			hcRightCompoundRule.Subrules.Add(rightSubrule);

			yield return hcRightCompoundRule;

			var hcLeftCompoundRule = new CompoundingRule
				{
					Name = compoundRule.Name.BestAnalysisAlternative.Text,
					HeadRequiredSyntacticFeatureStruct = leftRequiredFS,
					NonHeadRequiredSyntacticFeatureStruct = rightRequiredFS,
					OutSyntacticFeatureStruct = outFS,
					Properties = {{"ID", compoundRule.Hvo}}
				};

			var leftSubrule = new CompoundingSubrule();

			if (compoundRule.ToMsaOA.InflectionClassRA != null)
				leftSubrule.OutMprFeatures.Add(m_mprFeatures[compoundRule.ToMsaOA.InflectionClassRA]);

			leftSubrule.HeadLhs.Add(headPattern);
			leftSubrule.NonHeadLhs.Add(nonheadPattern);

			leftSubrule.Rhs.Add(new CopyFromInput("head"));
			leftSubrule.Rhs.Add(new InsertShape(m_table, "+"));
			leftSubrule.Rhs.Add(new CopyFromInput("nonhead"));

			hcLeftCompoundRule.Subrules.Add(leftSubrule);

			yield return hcLeftCompoundRule;
		}

		private RewriteRule LoadRewriteRule(IPhRegularRule prule)
		{
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
				foreach (IPhSimpleContext ctxt in prule.StrucDescOS)
				{
					PatternNode<Word, ShapeNode> node;
					if (LoadPatternNode(ctxt, out node))
						lhsPattern.Children.Add(node);
				}
				lhsPattern.Freeze();
				hcPrule.Lhs = lhsPattern;
			}
			hcPrule.Properties["ID"] = prule.Hvo;

			foreach (IPhSegRuleRHS rhs in prule.RightHandSidesOS)
			{
				var psubrule = new RewriteSubrule();

				var requiredFS = new FeatureStruct();
				if (rhs.InputPOSesRC.Count > 0)
					requiredFS.AddValue(m_posFeature, LoadAllPartsOfSpeech(rhs.InputPOSesRC));
				requiredFS.Freeze();
				psubrule.RequiredSyntacticFeatureStruct = requiredFS;

				psubrule.RequiredMprFeatures.AddRange(rhs.ReqRuleFeatsRC.SelectMany(LoadMprFeatures));
				psubrule.ExcludedMprFeatures.AddRange(rhs.ExclRuleFeatsRC.SelectMany(LoadMprFeatures));

				if (rhs.StrucChangeOS.Count > 0)
				{
					var rhsPattern = new Pattern<Word, ShapeNode>();
					foreach (IPhSimpleContext ctxt in rhs.StrucChangeOS)
					{
						PatternNode<Word, ShapeNode> node;
						if (LoadPatternNode(ctxt, out node))
							rhsPattern.Children.Add(node);
					}
					rhsPattern.Freeze();
					psubrule.Rhs = rhsPattern;
				}

				if (rhs.LeftContextOA != null)
				{
					var leftPattern = new Pattern<Word, ShapeNode>();
					if (IsWordInitial(rhs.LeftContextOA.ToEnumerable()))
						leftPattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
					PatternNode<Word, ShapeNode> leftNode;
					if (LoadPatternNode(rhs.LeftContextOA, out leftNode))
						leftPattern.Children.Add(leftNode);
					leftPattern.Freeze();
					psubrule.LeftEnvironment = leftPattern;
				}

				if (rhs.RightContextOA != null)
				{
					var rightPattern = new Pattern<Word, ShapeNode>();
					PatternNode<Word, ShapeNode> rightNode;
					if (LoadPatternNode(rhs.RightContextOA, out rightNode))
						rightPattern.Children.Add(rightNode);
					if (IsWordFinal(rhs.RightContextOA.ToEnumerable()))
						rightPattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));
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

			var pattern = new Pattern<Word, ShapeNode>();
			if (IsWordInitial(prule.StrucDescOS))
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
			for (int i = 0; i < prule.StrucDescOS.Count; i++)
			{
				PatternNode<Word, ShapeNode> node;
				if (LoadPatternNode(prule.StrucDescOS[i], out node))
					pattern.Children.Add(new Group<Word, ShapeNode>(i.ToString(CultureInfo.InvariantCulture), node));
			}
			if (IsWordFinal(prule.StrucDescOS))
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));
			pattern.Freeze();
			hcPrule.Pattern = pattern;

			bool isMiddleWithLeftSwitch;
			int[] indices = prule.GetStrucChangeIndices(out isMiddleWithLeftSwitch);
			if (indices[PhMetathesisRuleTags.kidxLeftEnv] != -1)
				hcPrule.GroupOrder.AddRange(GetMetathesisGroupNames(prule, 0, indices[PhMetathesisRuleTags.kidxLeftEnv] + 1));
			hcPrule.GroupOrder.Add(indices[PhMetathesisRuleTags.kidxRightSwitch].ToString(CultureInfo.InvariantCulture));
			if (indices[PhMetathesisRuleTags.kidxMiddle] != -1)
				hcPrule.GroupOrder.Add(indices[PhMetathesisRuleTags.kidxMiddle].ToString(CultureInfo.InvariantCulture));
			hcPrule.GroupOrder.Add(indices[PhMetathesisRuleTags.kidxLeftSwitch].ToString(CultureInfo.InvariantCulture));
			if (indices[PhMetathesisRuleTags.kidxRightEnv] != -1)
				hcPrule.GroupOrder.AddRange(GetMetathesisGroupNames(prule, indices[PhMetathesisRuleTags.kidxRightEnv], prule.StrucDescOS.Count));

			hcPrule.Properties["ID"] = prule.Hvo;

			return hcPrule;
		}

		private IEnumerable<string> GetMetathesisGroupNames(IPhMetathesisRule prule, int start, int limit)
		{
			for (int i = start; i < limit; i++)
			{
				IPhSimpleContext ctxt = prule.StrucDescOS[i];
				if (!IsWordBoundary(ctxt))
					yield return i.ToString(CultureInfo.InvariantCulture);
			}
		}

		private void LoadAllomorphCoOccurrenceRules(IMoAlloAdhocProhib alloAdhocProhib)
		{
			List<Allomorph> firstAllos;
			if (m_allomorphs.TryGetValue(alloAdhocProhib.FirstAllomorphRA, out firstAllos))
			{
				var allOthers = new List<List<Allomorph>>();
				foreach (IMoForm form in alloAdhocProhib.RestOfAllosRS)
				{
					List<Allomorph> hcAllos;
					if (m_allomorphs.TryGetValue(form, out hcAllos))
						allOthers.Add(hcAllos);
					else
						return;
				}

				MorphCoOccurrenceAdjacency adjacency = GetAdjacency(alloAdhocProhib.Adjacency);
				foreach (Allomorph[] others in Permute(allOthers, 0))
				{
					foreach (Allomorph firstAllo in firstAllos)
						firstAllo.ExcludedAllomorphCoOccurrences.Add(new AllomorphCoOccurrenceRule(others, adjacency));
				}
			}
		}

		private IEnumerable<T[]> Permute<T>(List<List<T>> items, int index)
		{
			if (items.Count == 0)
				yield break;

			if (index == items.Count)
			{
				yield return new T[items.Count];
			}
			else
			{
				foreach (T item in items[index])
				{
					foreach (T[] result in Permute(items, index + 1))
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
				foreach (IMoMorphSynAnalysis msa in morphAdhocProhib.RestOfMorphsRS)
				{
					List<Morpheme> hcMorphemes;
					if (m_morphemes.TryGetValue(msa, out hcMorphemes))
						allOthers.Add(hcMorphemes);
					else
						return;
				}

				MorphCoOccurrenceAdjacency adjacency = GetAdjacency(morphAdhocProhib.Adjacency);
				foreach (Morpheme[] others in Permute(allOthers, 0))
				{
					foreach (Morpheme firstMorpheme in firstMorphemes)
						firstMorpheme.ExcludedMorphemeCoOccurrences.Add(new MorphemeCoOccurrenceRule(others, adjacency));
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
				return Tuple.Create("", "");
			string[] contexts = env.StringRepresentation.Text.Trim().Substring(1).Split('_');
			return Tuple.Create(contexts[0].Trim(), contexts[1].Trim());
		}

		private Pattern<Word, ShapeNode> LoadEnvironmentPattern(string patternStr, bool left)
		{
			if (string.IsNullOrEmpty(patternStr))
				return null;

			var pattern = new Pattern<Word, ShapeNode>();
			if (left && patternStr.StartsWith("#"))
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.LeftSideAnchor));
			pattern.Children.AddRange(LoadPatternNodes(patternStr));
			if (!left && patternStr.EndsWith("#"))
				pattern.Children.Add(new Constraint<Word, ShapeNode>(HCFeatureSystem.RightSideAnchor));
			pattern.Freeze();
			return pattern;
		}

		private static PatternNode<Word, ShapeNode> PrefixNull()
		{
			return new Quantifier<Word, ShapeNode>(0, -1,
				new Group<Word, ShapeNode>(
					new Constraint<Word, ShapeNode>(Null),
					new Constraint<Word, ShapeNode>(MorphBdry)));
		}

		private static PatternNode<Word, ShapeNode> SuffixNull()
		{
			return new Quantifier<Word, ShapeNode>(0, -1,
				new Group<Word, ShapeNode>(
					new Constraint<Word, ShapeNode>(MorphBdry),
					new Constraint<Word, ShapeNode>(Null)));
		}

		private static IEnumerable<PatternNode<Word, ShapeNode>> AnyPlus()
		{
			yield return PrefixNull();
			yield return new Quantifier<Word, ShapeNode>(1, -1, new Constraint<Word, ShapeNode>(Any));
			yield return SuffixNull();
		}

		private static IEnumerable<PatternNode<Word, ShapeNode>> AnyStar()
		{
			yield return PrefixNull();
			yield return new Quantifier<Word, ShapeNode>(0, -1, new Constraint<Word, ShapeNode>(Any));
			yield return SuffixNull();
		}

		private bool LoadPatternNode(IPhPhonContext ctxt, out PatternNode<Word, ShapeNode> node)
		{
			switch (ctxt.ClassID)
			{
				case PhSequenceContextTags.kClassId:
					var seqCtxt = (IPhSequenceContext) ctxt;
					var nodes = new List<PatternNode<Word, ShapeNode>>();
					foreach (IPhPhonContext member in seqCtxt.MembersRS)
					{
						PatternNode<Word, ShapeNode> n;
						if (LoadPatternNode(member, out n))
							nodes.Add(n);
					}
					if (nodes.Count > 0)
					{
						node = new Group<Word, ShapeNode>(nodes);
						return true;
					}
					break;

				case PhIterationContextTags.kClassId:
					var iterCtxt = (IPhIterationContext) ctxt;
					PatternNode<Word, ShapeNode> childNode;
					if (LoadPatternNode(iterCtxt.MemberRA, out childNode))
					{
						node = new Quantifier<Word, ShapeNode>(iterCtxt.Minimum, iterCtxt.Maximum, childNode);
						return true;
					}
					break;

				case PhSimpleContextBdryTags.kClassId:
					var bdryCtxt = (IPhSimpleContextBdry) ctxt;
					IPhBdryMarker bdry = bdryCtxt.FeatureStructureRA;
					if (bdry != null && bdry.Guid != LangProjectTags.kguidPhRuleWordBdry)
					{
						string[] strReps = bdry.CodesOS.Where(c => !string.IsNullOrEmpty(c.Representation.BestVernacularAlternative.Text))
							.Select(c => c.Representation.BestVernacularAlternative.Text).ToArray();
						if (strReps.Length > 0)
						{
							node = new Constraint<Word, ShapeNode>(FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo(strReps).Value);
							return true;
						}
					}
					break;

				case PhSimpleContextSegTags.kClassId:
					var segCtxt = (IPhSimpleContextSeg) ctxt;
					IPhPhoneme phoneme = segCtxt.FeatureStructureRA;
					if (phoneme != null)
					{
						FeatureStruct fs = null;
						foreach (IPhCode code in phoneme.CodesOS)
						{
							string strRep = code.Representation.VernacularDefaultWritingSystem.Text;
							if (!string.IsNullOrEmpty(strRep))
							{
								FeatureStruct segFS = m_table.GetSymbolFeatureStruct(strRep);
								if (fs == null)
									fs = segFS.DeepClone();
								else
									fs.Union(segFS);
							}
						}
						if (fs != null)
						{
							fs.Freeze();
							node = new Constraint<Word, ShapeNode>(fs);
							return true;
						}
					}
					break;

				case PhSimpleContextNCTags.kClassId:
					var ncCtxt = (IPhSimpleContextNC) ctxt;
					IPhNaturalClass nc = ncCtxt.FeatureStructureRA;
					if (nc != null)
					{
						node = new Constraint<Word, ShapeNode>(LoadNaturalClassFeatureStruct(nc, ncCtxt.PlusConstrRS, ncCtxt.MinusConstrRS));
						return true;
					}
					break;
			}

			node = null;
			return false;
		}

		private IEnumerable<PatternNode<Word, ShapeNode>> LoadPatternNodes(string patternStr)
		{
			foreach (string token in TokenizeContext(patternStr))
			{
				switch (token[0])
				{
					case '#':
						break;

					case '[':
						IPhNaturalClass nc = m_naturalClasses[token.Substring(1, token.Length - 2).Trim()];
						yield return new Constraint<Word, ShapeNode>(LoadNaturalClassFeatureStruct(nc));
						break;

					case '(':
						yield return new Quantifier<Word, ShapeNode>(0, 1, new Group<Word, ShapeNode>(LoadPatternNodes(token.Substring(1, token.Length - 2).Trim())));
						break;

					default:
						Shape shape = m_table.Segment(token.Trim());
						foreach (Constraint<Word, ShapeNode> cons in shape.Select(n => new Constraint<Word, ShapeNode>(n.Annotation.FeatureStruct)))
							yield return cons;
						break;
				}
			}
		}

		private IEnumerable<string> TokenizeContext(string contextStr)
		{
			int pos = 0;
			while (pos < contextStr.Length)
			{
				switch (contextStr[pos])
				{
					case '#':
						yield return "#";
						pos++;
						break;

					case '[':
						int endNCPos = contextStr.IndexOf(']', pos);
						yield return contextStr.Substring(pos, endNCPos - pos + 1);
						pos = endNCPos + 1;
						break;

					case '(':
						int endOptPos = contextStr.IndexOf(')', pos);
						yield return contextStr.Substring(pos, endOptPos - pos + 1);
						pos = endOptPos + 1;
						break;

					default:
						int endRepPos = contextStr.IndexOfAny(new[] { '#', '[', '(', ' ' }, pos);
						if (endRepPos == -1)
							endRepPos = contextStr.Length;
						yield return contextStr.Substring(pos, endRepPos - pos);
						pos = endRepPos;
						break;
				}
			}
		}

		private bool IsWordInitial(IEnumerable<IPhPhonContext> ctxts)
		{
			IPhPhonContext ctxt = ctxts.First();
			if (IsWordBoundary(ctxt))
				return true;

			var seqCtxt = ctxt as IPhSequenceContext;
			if (seqCtxt != null)
			{
				if (seqCtxt.MembersRS.Count > 0 && IsWordBoundary(seqCtxt.MembersRS[0]))
					return true;
			}
			return false;
		}

		private bool IsWordFinal(IEnumerable<IPhPhonContext> ctxts)
		{
			IPhPhonContext ctxt = ctxts.Last();
			if (IsWordBoundary(ctxt))
				return true;

			var seqCtxt = ctxt as IPhSequenceContext;
			if (seqCtxt != null)
			{
				if (seqCtxt.MembersRS.Count > 0 && IsWordBoundary(seqCtxt.MembersRS[seqCtxt.MembersRS.Count - 1]))
					return true;
			}
			return false;
		}

		private static bool IsWordBoundary(IPhPhonContext ctxt)
		{
			var bdryCtxt = ctxt as IPhSimpleContextBdry;
			if (bdryCtxt != null)
			{
				if (bdryCtxt.FeatureStructureRA.Guid == LangProjectTags.kguidPhRuleWordBdry)
					return true;
			}
			return false;
		}

		private FeatureStruct LoadNaturalClassFeatureStruct(IPhNaturalClass nc)
		{
			return LoadNaturalClassFeatureStruct(nc, Enumerable.Empty<IPhFeatureConstraint>(), Enumerable.Empty<IPhFeatureConstraint>());
		}

		private FeatureStruct LoadNaturalClassFeatureStruct(IPhNaturalClass nc, IEnumerable<IPhFeatureConstraint> plusConstraints, IEnumerable<IPhFeatureConstraint> minusConstraints)
		{
			FeatureStruct fs;
			var segNC = nc as IPhNCSegments;
			if (segNC != null)
			{
				fs = null;
				foreach (IPhCode code in segNC.SegmentsRC.SelectMany(p => p.CodesOS))
				{
					string strRep = code.Representation.VernacularDefaultWritingSystem.Text;
					if (!string.IsNullOrEmpty(strRep))
					{
						FeatureStruct segFS = m_table.GetSymbolFeatureStruct(strRep);
						if (fs == null)
							fs = segFS.DeepClone();
						else
							fs.Union(segFS);
					}
				}

				if (fs == null)
					fs = new FeatureStruct();
			}
			else
			{
				var featNC = (IPhNCFeatures) nc;
				fs = LoadFeatureStruct(featNC.FeaturesOA, m_language.PhoneticFeatureSystem);
				fs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
				AddVariables(fs, plusConstraints, true);
				AddVariables(fs, minusConstraints, false);
			}
			fs.Freeze();
			return fs;
		}

		private void AddVariables(FeatureStruct fs, IEnumerable<IPhFeatureConstraint> constraints, bool agree)
		{
			foreach (IPhFeatureConstraint constraint in constraints)
			{
				var feat = m_language.PhoneticFeatureSystem.GetFeature<SymbolicFeature>(constraint.FeatureRA.Guid.ToString());
				fs.AddValue(feat, new SymbolicFeatureValue(feat, constraint.Hvo.ToString(CultureInfo.InvariantCulture), agree));
			}
		}

		private FeatureStruct LoadFeatureStruct(IFsFeatStruc fs, FeatureSystem featSys)
		{
			var hcFS = new FeatureStruct();
			foreach (IFsFeatureSpecification value in fs.FeatureSpecsOC)
			{
				var closedValue = value as IFsClosedValue;
				if (closedValue != null)
				{
					var hcFeature = featSys.GetFeature<SymbolicFeature>(closedValue.FeatureRA.Guid.ToString());
					hcFS.AddValue(hcFeature, hcFeature.PossibleSymbols[closedValue.ValueRA.Guid.ToString()]);
				}
				else
				{
					var complexValue = (IFsComplexValue) value;
					var hcFeature = featSys.GetFeature<ComplexFeature>(complexValue.FeatureRA.Guid.ToString());
					hcFS.AddValue(hcFeature, LoadFeatureStruct((IFsFeatStruc) complexValue.ValueOA, featSys));
				}
			}
			return hcFS;
		}

		private static string FormatForm(string formStr)
		{
			switch (formStr)
			{
				case "*0":
				case "&0":
				case "Ø":
					return "^0";
			}

			return formStr.Replace(' ', '.');
		}

		private IEnumerable<FeatureSymbol> LoadAllPartsOfSpeech(IPartOfSpeech pos)
		{
			yield return m_posFeature.PossibleSymbols[pos.Guid.ToString()];
				foreach (FeatureSymbol symbol in LoadAllPartsOfSpeech(pos.SubPossibilitiesOS.Cast<IPartOfSpeech>()))
					yield return symbol;
		}

		private IEnumerable<FeatureSymbol> LoadAllPartsOfSpeech(IEnumerable<IPartOfSpeech> poss)
		{
			foreach (IPartOfSpeech pos in poss)
			{
				yield return m_posFeature.PossibleSymbols[pos.Guid.ToString()];
				foreach (FeatureSymbol symbol in LoadAllPartsOfSpeech(pos.SubPossibilitiesOS.Cast<IPartOfSpeech>()))
					yield return symbol;
			}
		}

		private IEnumerable<MprFeature> LoadAllInflClasses(IMoInflClass inflClass)
		{
			yield return m_mprFeatures[inflClass];
			foreach (MprFeature mprFeat in LoadAllInflClasses(inflClass.SubclassesOC))
				yield return mprFeat;
		}

		private IEnumerable<MprFeature> LoadAllInflClasses(IEnumerable<IMoInflClass> inflClasses)
		{
			foreach (IMoInflClass inflClass in inflClasses)
			{
				yield return m_mprFeatures[inflClass];
				foreach (MprFeature mprFeat in LoadAllInflClasses(inflClass.SubclassesOC))
					yield return mprFeat;
			}
		}

		private IEnumerable<MprFeature> LoadMprFeatures(IPhPhonRuleFeat ruleFeat)
		{
			switch (ruleFeat.ItemRA.ClassID)
			{
				case MoInflClassTags.kClassId:
					foreach (MprFeature mprFeat in LoadAllInflClasses((IMoInflClass) ruleFeat.ItemRA))
						yield return mprFeat;
					break;

				case CmPossibilityTags.kClassId:
					yield return m_mprFeatures[ruleFeat.ItemRA];
					break;
			}
		}

		private static IMoInflClass GetInflClass(IMoStemMsa msa)
		{
			if (msa.InflectionClassRA != null)
				return msa.InflectionClassRA;
			if (msa.PartOfSpeechRA != null)
				return GetDefaultInflClass(msa.PartOfSpeechRA);
			return null;
		}

		private static IMoInflClass GetDefaultInflClass(IPartOfSpeech pos)
		{
			if (pos.DefaultInflectionClassRA != null)
				return pos.DefaultInflectionClassRA;
			foreach (IPartOfSpeech child in pos.SubPossibilitiesOS.Cast<IPartOfSpeech>())
			{
				IMoInflClass defInflClass = GetDefaultInflClass(child);
				if (defInflClass != null)
					return defInflClass;
			}
			return null;
		}

		private static void LoadFeatureSystem(IFsFeatureSystem featSys, FeatureSystem hcFeatSys)
		{
			foreach (IFsFeatDefn feature in featSys.FeaturesOC)
			{
				var closedFeature = feature as IFsClosedFeature;
				if (closedFeature != null)
				{
					hcFeatSys.Add(new SymbolicFeature(closedFeature.Guid.ToString(),
						closedFeature.ValuesOC.Select(sfv => new FeatureSymbol(sfv.Guid.ToString()) { Description = sfv.Abbreviation.BestAnalysisAlternative.Text }))
					{ Description = feature.Abbreviation.BestAnalysisAlternative.Text });
				}
				else
				{
					hcFeatSys.Add(new ComplexFeature(feature.Guid.ToString()) { Description = feature.Abbreviation.BestAnalysisAlternative.Text });
				}
			}
			hcFeatSys.Freeze();
		}

		private void LoadSymbolTable(IPhPhonemeSet phonemeSet, FeatureSystem featSys)
		{
			m_table = new SymbolTable(m_spanFactory) { Name = phonemeSet.Name.BestAnalysisAlternative.Text };
			foreach (IPhPhoneme phoneme in phonemeSet.PhonemesOC)
			{
				FeatureStruct fs = null;
				if (featSys.Count > 0)
				{
					fs = LoadFeatureStruct(phoneme.FeaturesOA, m_language.PhoneticFeatureSystem);
					fs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
					fs.Freeze();
				}
				foreach (IPhCode code in phoneme.CodesOS)
				{
					string strRep = code.Representation.VernacularDefaultWritingSystem.Text;
					if (!string.IsNullOrEmpty(strRep))
						m_table.Add(strRep, fs ?? FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Feature(HCFeatureSystem.StrRep).EqualTo(strRep).Value);
				}
			}

			foreach (IPhBdryMarker bdry in phonemeSet.BoundaryMarkersOC.Where(bdry => bdry.Guid != LangProjectTags.kguidPhRuleWordBdry))
			{
				foreach (IPhCode code in bdry.CodesOS)
				{
					string strRep = code.Representation.BestVernacularAlternative.Text;
					if (!string.IsNullOrEmpty(strRep))
						m_table.Add(strRep, FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo(strRep).Value);
				}
			}
			m_table.Add("^0", Null);
			m_table.Add(".", FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo(".").Value);
		}
	}
}
