// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.Extensions;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;
using SIL.Machine.FeatureModel;
using FS = System.Collections.Generic.Dictionary<string, object>;
using System.Diagnostics;

namespace SIL.FieldWorks.WordWorks.Parser
{
	[TestFixture]
	public class HCLoaderTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string PrefixNull = "([StrRep:{\"&0\", \"*0\", \"^0\", \"∅\"}, Type:boundary][StrRep:\"+\", Type:boundary])*";
		private const string SuffixNull = "([StrRep:\"+\", Type:boundary][StrRep:{\"&0\", \"*0\", \"^0\", \"∅\"}, Type:boundary])*";
		private const string AnyPlus = PrefixNull + "ANY+" + SuffixNull;
		private const string AnyStar = PrefixNull + "ANY*" + SuffixNull;
		private const string VowelFS = "[cons:-, Type:segment, voc:+]";
		private const string ConsFS = "[cons:+, Type:segment, voc:-]";
		private const string RightAnchorFS = "[AnchorType:RightSide, Type:anchor]";
		private const string LeftAnchorFS = "[AnchorType:LeftSide, Type:anchor]";

		private enum LoadErrorType
		{
			InvalidShape,
			InvalidAffixProcess,
			InvalidPhoneme,
			DuplicateGrapheme,
			InvalidEnvironment,
			InvalidRedupForm,
			InvalidRewriteRule
		}

		private class TestHCLoadErrorLogger : IHCLoadErrorLogger
		{
			private readonly IList<Tuple<LoadErrorType, ICmObject>> m_loadErrors;

			public TestHCLoadErrorLogger(IList<Tuple<LoadErrorType, ICmObject>> loadErrors)
			{
				m_loadErrors = loadErrors;
			}

			public void InvalidShape(string str, int errorPos, IMoMorphSynAnalysis msa)
			{
				m_loadErrors.Add(Tuple.Create(LoadErrorType.InvalidShape, (ICmObject) msa));
			}

			public void InvalidAffixProcess(IMoAffixProcess affixProcess, bool isInvalidLhs, IMoMorphSynAnalysis msa)
			{
				m_loadErrors.Add(Tuple.Create(LoadErrorType.InvalidAffixProcess, (ICmObject) msa));
			}

			public void InvalidPhoneme(IPhPhoneme phoneme)
			{
				m_loadErrors.Add(Tuple.Create(LoadErrorType.InvalidPhoneme, (ICmObject) phoneme));
			}

			public void DuplicateGrapheme(IPhPhoneme phoneme)
			{
				m_loadErrors.Add(Tuple.Create(LoadErrorType.DuplicateGrapheme, (ICmObject) phoneme));
			}

			public void InvalidEnvironment(IMoForm form, IPhEnvironment env, string reason, IMoMorphSynAnalysis msa)
			{
				m_loadErrors.Add(Tuple.Create(LoadErrorType.InvalidEnvironment, (ICmObject) msa));
			}

			public void InvalidReduplicationForm(IMoForm form, string reason, IMoMorphSynAnalysis msa)
			{
				m_loadErrors.Add(Tuple.Create(LoadErrorType.InvalidRedupForm, (ICmObject) msa));
			}

			public void InvalidRewriteRule(IPhRegularRule rule, string reason)
			{
				m_loadErrors.Add(Tuple.Create(LoadErrorType.InvalidRedupForm, (ICmObject) rule));
			}
		}

		private readonly List<Tuple<LoadErrorType, ICmObject>> m_loadErrors = new List<Tuple<LoadErrorType, ICmObject>>();
		private Language m_lang;
		private IPartOfSpeech m_noun;
		private IPartOfSpeech m_verb;
		private IPartOfSpeech m_adj;
		private IPartOfSpeech m_particle;
		private IFsFeatStrucType m_inflType;
		private IPhNCFeatures m_vowel;
		private IPhNCFeatures m_cons;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
		}

		protected override void CreateTestData()
		{
			base.CreateTestData();

			Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem);

			m_noun = AddPartOfSpeech("N");
			m_verb = AddPartOfSpeech("V");
			m_adj = AddPartOfSpeech("A");
			m_particle = AddPartOfSpeech("P");

			Cache.LanguageProject.MorphologicalDataOA.ParserParameters = "<ParserParameters><ActiveParser>HC</ActiveParser><HC><NoDefaultCompounding>true</NoDefaultCompounding><AcceptUnspecifiedGraphemes>false</AcceptUnspecifiedGraphemes></HC></ParserParameters>";

			IFsFeatureSystem phFeatSys = Cache.LanguageProject.PhFeatureSystemOA;
			AddClosedFeature(phFeatSys, "voc", "+", "-");
			AddClosedFeature(phFeatSys, "cons", "+", "-");
			AddClosedFeature(phFeatSys, "high", "+", "-");
			AddClosedFeature(phFeatSys, "low", "+", "-");
			AddClosedFeature(phFeatSys, "back", "+", "-");
			AddClosedFeature(phFeatSys, "round", "+", "-");
			AddClosedFeature(phFeatSys, "vd", "+", "-");
			AddClosedFeature(phFeatSys, "asp", "+", "-");
			AddClosedFeature(phFeatSys, "del_rel", "+", "-");
			AddClosedFeature(phFeatSys, "strident", "+", "-");
			AddClosedFeature(phFeatSys, "cont", "+", "-");
			AddClosedFeature(phFeatSys, "nasal", "+", "-");
			AddClosedFeature(phFeatSys, "poa", "bilabial", "labiodental", "alveolar", "velar");

			Cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());

			AddPhoneme("a", new FS {{"cons", "-"}, {"voc", "+"}, {"high", "-"}, {"low", "+"}, {"back", "+"}, {"round", "-"}, {"vd", "+"}});
			AddPhoneme("i", new FS {{"cons", "-"}, {"voc", "+"}, {"high", "+"}, {"low", "-"}, {"back", "-"}, {"round", "-"}, {"vd", "+"}});
			AddPhoneme("u", new FS {{"cons", "-"}, {"voc", "+"}, {"high", "+"}, {"low", "-"}, {"back", "+"}, {"round", "+"}, {"vd", "+"}});
			AddPhoneme("o", new FS {{"cons", "-"}, {"voc", "+"}, {"high", "-"}, {"low", "-"}, {"back", "+"}, {"round", "+"}, {"vd", "+"}});
			AddPhoneme("y", new FS {{"cons", "-"}, {"voc", "+"}, {"high", "+"}, {"low", "-"}, {"back", "-"}, {"round", "+"}, {"vd", "+"}});
			AddPhoneme("ɯ", new FS {{"cons", "-"}, {"voc", "+"}, {"high", "+"}, {"low", "-"}, {"back", "+"}, {"round", "-"}, {"vd", "+"}});

			AddPhoneme("p", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "bilabial"}, {"vd", "-"}, {"asp", "-"}, {"strident", "-"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("t", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "alveolar"}, {"vd", "-"}, {"asp", "-"}, {"del_rel", "-"}, {"strident", "-"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("k", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "velar"}, {"vd", "-"}, {"asp", "-"}, {"strident", "-"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("ts", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "alveolar"}, {"vd", "-"}, {"asp", "-"}, {"del_rel", "+"}, {"strident", "+"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("pʰ", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "bilabial"}, {"vd", "-"}, {"asp", "+"}, {"strident", "-"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("tʰ", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "alveolar"}, {"vd", "-"}, {"asp", "+"}, {"del_rel", "-"}, {"strident", "-"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("kʰ", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "velar"}, {"vd", "-"}, {"asp", "+"}, {"strident", "-"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("tsʰ", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "alveolar"}, {"vd", "-"}, {"asp", "+"}, {"del_rel", "+"}, {"strident", "+"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("b", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "bilabial"}, {"vd", "+"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("d", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "alveolar"}, {"vd", "+"}, {"strident", "-"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("g", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "velar"}, {"vd", "+"}, {"cont", "-"}, {"nasal", "-"}});
			AddPhoneme("m", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "bilabial"}, {"vd", "+"}, {"cont", "-"}, {"nasal", "+"}});
			AddPhoneme("n", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "alveolar"}, {"vd", "+"}, {"strident", "-"}, {"cont", "-"}, {"nasal", "+"}});
			AddPhoneme("ŋ", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "velar"}, {"vd", "+"}, {"cont", "-"}, {"nasal", "+"}});
			AddPhoneme("s", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "alveolar"}, {"vd", "-"}, {"asp", "-"}, {"del_rel", "-"}, {"strident", "+"}, {"cont", "+"}});
			AddPhoneme("z", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "alveolar"}, {"vd", "+"}, {"asp", "-"}, {"del_rel", "-"}, {"strident", "+"}, {"cont", "+"}});
			AddPhoneme("f", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "labiodental"}, {"vd", "-"}, {"asp", "-"}, {"strident", "+"}, {"cont", "+"}});
			AddPhoneme("v", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "labiodental"}, {"vd", "+"}, {"asp", "-"}, {"strident", "+"}, {"cont", "+"}});

			AddBdry(LangProjectTags.kguidPhRuleMorphBdry, "+");
			AddBdry(LangProjectTags.kguidPhRuleWordBdry, "#");

			m_vowel = AddNaturalClass("V", new FS {{"cons", "-"}, {"voc", "+"}});
			m_cons = AddNaturalClass("C", new FS {{"cons", "+"}, {"voc", "-"}});

			IFsFeatureSystem msFeatSys = Cache.LanguageProject.MsFeatureSystemOA;
			m_inflType = AddFSType(msFeatSys, "infl",
				AddComplexFeature(msFeatSys, "nounAgr", AddClosedFeature(msFeatSys, "num", "sg", "pl")),
				AddClosedFeature(msFeatSys, "tense", "pres", "past"));
		}

		private IPhEnvironment AddEnvironment(string strRep)
		{
			IPhEnvironment env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.EnvironmentsOS.Add(env);
			env.StringRepresentation = TsStringUtils.MakeString(strRep, Cache.DefaultVernWs);
			return env;
		}

		private IPhNCFeatures AddNaturalClass(string name, FS featVals)
		{
			IPhNCFeatures nc = Cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS.Add(nc);
			nc.Name.SetAnalysisDefaultWritingSystem(name);
			nc.Abbreviation.SetAnalysisDefaultWritingSystem(name);

			nc.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.PhFeatureSystemOA, null, nc.FeaturesOA, featVals);
			return nc;
		}

		private IPartOfSpeech AddPartOfSpeech(string name)
		{
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			pos.Name.SetAnalysisDefaultWritingSystem(name);
			pos.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			return pos;
		}

		private IFsClosedFeature AddClosedFeature(IFsFeatureSystem featSys, string name, params string[] values)
		{
			IFsClosedFeature feat = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			featSys.FeaturesOC.Add(feat);
			feat.Name.SetAnalysisDefaultWritingSystem(name);
			feat.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			foreach (string value in values)
			{
				IFsSymFeatVal symbol = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
				feat.ValuesOC.Add(symbol);
				symbol.Name.SetAnalysisDefaultWritingSystem(value);
				symbol.Abbreviation.SetAnalysisDefaultWritingSystem(value);
			}
			return feat;
		}

		private IFsFeatStrucType AddFSType(IFsFeatureSystem featSys, string name, params IFsFeatDefn[] features)
		{
			IFsFeatStrucType type = Cache.ServiceLocator.GetInstance<IFsFeatStrucTypeFactory>().Create();
			featSys.TypesOC.Add(type);
			type.Name.SetAnalysisDefaultWritingSystem(name);
			type.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			type.FeaturesRS.AddRange(features);
			return type;
		}

		private IFsComplexFeature AddComplexFeature(IFsFeatureSystem featSys, string name, params IFsFeatDefn[] features)
		{
			IFsFeatStrucType type = AddFSType(featSys, name, features);

			IFsComplexFeature feat = Cache.ServiceLocator.GetInstance<IFsComplexFeatureFactory>().Create();
			featSys.FeaturesOC.Add(feat);
			feat.Name.SetAnalysisDefaultWritingSystem(name);
			feat.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			feat.TypeRA = type;
			return feat;
		}

		private void AddPhoneme(string strRep, FS featVals, string grapheme = null)
		{
			IPhPhoneme phoneme = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(phoneme);
			phoneme.Name.SetVernacularDefaultWritingSystem(strRep);
			IPhCode code = phoneme.CodesOS[0];
			code.Representation.SetVernacularDefaultWritingSystem(grapheme ?? strRep);

			phoneme.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.PhFeatureSystemOA, null, phoneme.FeaturesOA, featVals);
		}

		private void CreateFeatStruc(IFsFeatureSystem featSys, IFsFeatStrucType type, IFsFeatStruc fs, FS featVals)
		{
			if (featVals.Count == 0)
				return;

			fs.TypeRA = type;
			foreach (KeyValuePair<string, object> featVal in featVals)
			{
				IFsFeatDefn fd = featSys.FeaturesOC.First(f => f.Abbreviation.AnalysisDefaultWritingSystem.Text == featVal.Key);

				var closedFeat = fd as IFsClosedFeature;
				if (closedFeat != null)
				{
					IFsSymFeatVal sym = closedFeat.ValuesOC.First(v => v.Abbreviation.AnalysisDefaultWritingSystem.Text == (string) featVal.Value);
					IFsClosedValue cv = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
					fs.FeatureSpecsOC.Add(cv);
					cv.FeatureRA = fd;
					cv.ValueRA = sym;
				}
				else
				{
					var complexFeat = (IFsComplexFeature) fd;
					IFsComplexValue cv = Cache.ServiceLocator.GetInstance<IFsComplexValueFactory>().Create();
					fs.FeatureSpecsOC.Add(cv);

					IFsFeatStruc childFS = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
					cv.FeatureRA = fd;
					cv.ValueOA = childFS;
					CreateFeatStruc(featSys, complexFeat.TypeRA, childFS, (FS) featVal.Value);
				}
			}
		}

		private void AddBdry(Guid guid, string strRep)
		{
			IPhBdryMarker bdry = Cache.ServiceLocator.GetInstance<IPhBdryMarkerFactory>().Create(guid, Cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0]);
			ITsString tss = TsStringUtils.MakeString(strRep, Cache.DefaultAnalWs);
			bdry.Name.set_String(Cache.DefaultAnalWs, tss);
			IPhCode code = Cache.ServiceLocator.GetInstance<IPhCodeFactory>().Create();
			bdry.CodesOS.Add(code);
			code.Representation.set_String(Cache.DefaultAnalWs, tss);
		}

		private ILexEntry AddEntry(Guid morphType, string lexemeForm, string gloss, SandboxGenericMSA msa)
		{
			return Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(morphType),
				TsStringUtils.MakeString(lexemeForm, Cache.DefaultVernWs), gloss, msa);
		}

		private IMoInflClass AddInflectionClass(IPartOfSpeech pos, string name)
		{
			var inflClass = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos.InflectionClassesOC.Add(inflClass);
			inflClass.Name.SetAnalysisDefaultWritingSystem(name);
			inflClass.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			return inflClass;
		}

		private void AddInflectiononSubclass(IMoInflClass parent, string name)
		{
			var inflClass = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			parent.SubclassesOC.Add(inflClass);
			inflClass.Name.SetAnalysisDefaultWritingSystem(name);
			inflClass.Abbreviation.SetAnalysisDefaultWritingSystem(name);
		}

		private ICmPossibility AddExceptionFeature(string name)
		{
			var excFeat = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			Cache.LanguageProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Add(excFeat);
			excFeat.Name.SetAnalysisDefaultWritingSystem(name);
			excFeat.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			return excFeat;
		}

		private IMoStemName AddStemName(IPartOfSpeech pos, string name, FS featVals)
		{
			var stemName = Cache.ServiceLocator.GetInstance<IMoStemNameFactory>().Create();
			pos.StemNamesOC.Add(stemName);
			stemName.Name.SetAnalysisDefaultWritingSystem(name);
			stemName.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			IFsFeatStruc region = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			stemName.RegionsOC.Add(region);
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, region, featVals);
			return stemName;
		}

		private IMoInflAffixSlot AddSlot(IMoInflAffixTemplate template, string name, bool prefix, bool optional)
		{
			IMoInflAffixSlot slot = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			template.OwnerOfClass<IPartOfSpeech>().AffixSlotsOC.Add(slot);
			slot.Name.SetAnalysisDefaultWritingSystem(name);
			slot.Optional = optional;
			if (prefix)
				template.PrefixSlotsRS.Add(slot);
			else
				template.SuffixSlotsRS.Add(slot);
			return slot;
		}

		private IPhFeatureConstraint AddFeatureConstraint(string feature)
		{
			IPhFeatureConstraint constr = Cache.ServiceLocator.GetInstance<IPhFeatureConstraintFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.FeatConstraintsOS.Add(constr);
			constr.FeatureRA = Cache.LanguageProject.PhFeatureSystemOA.FeaturesOC.First(f => f.Name.BestAnalysisAlternative.Text == feature);
			return constr;
		}

		private IPhSimpleContextSeg AddSegContext(string strRep)
		{
			IPhSimpleContextSeg segCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.ContextsOS.Add(segCtxt);
			segCtxt.FeatureStructureRA = GetPhoneme(strRep);
			return segCtxt;
		}

		private IPhSimpleContextBdry AddBdryContext(Guid guid)
		{
			IPhSimpleContextBdry bdryCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.ContextsOS.Add(bdryCtxt);
			bdryCtxt.FeatureStructureRA = GetBdry(guid);
			return bdryCtxt;
		}

		private IPhSimpleContextNC AddNCContext(IPhNaturalClass nc)
		{
			IPhSimpleContextNC ncCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.ContextsOS.Add(ncCtxt);
			ncCtxt.FeatureStructureRA = nc;
			return ncCtxt;
		}

		private IPhPhonRuleFeat AddPhonRuleFeature(ICmObject obj)
		{
			IPhPhonRuleFeat ruleFeat = Cache.ServiceLocator.GetInstance<IPhPhonRuleFeatFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonRuleFeatsOA.PossibilitiesOS.Add(ruleFeat);
			ruleFeat.ItemRA = obj;
			return ruleFeat;
		}

		private IPhPhoneme GetPhoneme(string strRep)
		{
			return Cache.ServiceLocator.GetInstance<IPhPhonemeRepository>().AllInstances().First(p => p.Name.BestVernacularAlternative.Text == strRep);
		}

		private IPhBdryMarker GetBdry(Guid guid)
		{
			return Cache.ServiceLocator.GetInstance<IPhBdryMarkerRepository>().GetObject(guid);
		}

		private void LoadLanguage()
		{
			m_loadErrors.Clear();
			m_lang = HCLoader.Load(Cache, new TestHCLoadErrorLogger(m_loadErrors));
		}

		[Test]
		public void PhonologicalFeatures()
		{
			LoadLanguage();
			Assert.That(m_lang.PhonologicalFeatureSystem.Count, Is.EqualTo(13));
			var voc = (SymbolicFeature) m_lang.PhonologicalFeatureSystem.First(f => f.Description == "voc");
			Assert.That(voc.PossibleSymbols.Select(s => s.Description), Is.EquivalentTo(new[] {"+", "-"}));
			var poa = (SymbolicFeature) m_lang.PhonologicalFeatureSystem.First(f => f.Description == "poa");
			Assert.That(poa.PossibleSymbols.Select(s => s.Description), Is.EquivalentTo(new[] {"bilabial", "labiodental", "alveolar", "velar"}));
		}

		[Test]
		public void PhonemesAndBoundaries()
		{
			LoadLanguage();
			Assert.That(m_lang.SurfaceStratum.CharacterDefinitionTable["a"].FeatureStruct.ToString(), Is.EqualTo("[back:+, cons:-, high:-, low:+, round:-, Type:segment, vd:+, voc:+]"));
			Assert.That(m_lang.SurfaceStratum.CharacterDefinitionTable["pʰ"].FeatureStruct.ToString(), Is.EqualTo("[asp:+, cons:+, cont:-, nasal:-, poa:bilabial, strident:-, Type:segment, vd:-, voc:-]"));
			Assert.That(m_lang.SurfaceStratum.CharacterDefinitionTable["+"].FeatureStruct.ToString(), Is.EqualTo("[StrRep:\"+\", Type:boundary]"));
		}

		[Test]
		public void Suffix()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			var allo = (IMoAffixAllomorph) entry.LexemeFormOA;
			allo.PhoneEnvRC.Add(AddEnvironment("/ [V] _ #"));
			allo.PhoneEnvRC.Add(AddEnvironment("/ #[C] _ [C]"));
			allo.MsEnvFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, allo.MsEnvFeaturesOA, new FS {{"tense", "pres"}});
			IMoInflClass inflClass = AddInflectionClass(m_verb, "inflClass");
			AddInflectiononSubclass(inflClass, "inflSubclass");
			allo.InflectionClassesRC.Add(inflClass);
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(2));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyStar + VowelFS + SuffixNull}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<stem>", "+ɯd"}));
			Assert.That(hcAllo.Environments.Select(e => e.ToEnvString()), Is.EquivalentTo(new[] {"/ _ " + RightAnchorFS}));
			Assert.That(hcAllo.RequiredMprFeatures, Is.Empty);
			Assert.That(hcAllo.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[tense:pres]]"));

			hcAllo = rule.Allomorphs[1];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {PrefixNull + ConsFS + SuffixNull}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<stem>", "+ɯd"}));
			Assert.That(hcAllo.Environments.Select(e => e.ToEnvString()), Is.EquivalentTo(new[] {"/ _ " + ConsFS}));
			Assert.That(hcAllo.RequiredMprFeatures, Is.Empty);
			Assert.That(hcAllo.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[tense:pres]]"));
		}

		[Test]
		public void AbstractForm()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			var allo = (IMoAffixAllomorph) entry.LexemeFormOA;
			allo.IsAbstract = true;
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));
		}

		[Test]
		public void InvalidShape()
		{
			AddEntry(MoMorphTypeTags.kguidMorphSuffix, "hello", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));
			Assert.That(m_loadErrors.Count, Is.EqualTo(1));
			Assert.That(m_loadErrors[0].Item1, Is.EqualTo(LoadErrorType.InvalidShape));
		}

		[Test]
		public void Infix()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphInfix, "a", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			var allo = (IMoAffixAllomorph) entry.LexemeFormOA;
			allo.PositionRS.Clear();
			allo.PositionRS.Add(AddEnvironment("/ #[V] _ [V]"));
			allo.PositionRS.Add(AddEnvironment("/ [C] _ [C]"));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(2));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {PrefixNull + VowelFS, VowelFS + AnyStar}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<left>", "+a+", "<right>"}));
			Assert.That(hcAllo.Environments, Is.Empty);

			hcAllo = rule.Allomorphs[1];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyStar + ConsFS, ConsFS + AnyStar}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<left>", "+a+", "<right>"}));
			Assert.That(hcAllo.Environments, Is.Empty);
		}

		[Test]
		public void InvalidInfixEnvironment()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphInfix, "a", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			var allo = (IMoAffixAllomorph) entry.LexemeFormOA;
			allo.PositionRS.Clear();
			allo.PositionRS.Add(AddEnvironment("/ #[V] _ [A]"));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));
		}

		[Test]
		public void EnvironmentWithSpaces()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphStem, "a", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem});
			var allo = (IMoStemAllomorph) entry.LexemeFormOA;
			allo.PhoneEnvRC.Clear();
			allo.PhoneEnvRC.Add(AddEnvironment("/ _ [C] ([V]) #"));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(1));
			LexEntry hcEntry = m_lang.Strata[0].Entries.First();

			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));

			RootAllomorph hcAllo = hcEntry.PrimaryAllomorph;
			Assert.That(hcAllo.Environments.Select(e => e.ToEnvString()), Is.EquivalentTo(new[]
				{
					"/ _ [cons:+, Type:segment, voc:-]([cons:-, Type:segment, voc:+])?[AnchorType:RightSide, Type:anchor]"
				}));
		}

		[Test]
		public void FullReduplication()
		{
			AddEntry(MoMorphTypeTags.kguidMorphPrefix, "[...]", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(1));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyPlus}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<stem>", "+", "<stem>"}));
			Assert.That(hcAllo.ReduplicationHint, Is.EqualTo(ReduplicationHint.Prefix));
		}

		[Test]
		public void PartialReduplication()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphPrefix, "[C^1][V^1]d", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			var allo = (IMoAffixAllomorph) entry.LexemeFormOA;
			allo.PhoneEnvRC.Add(AddEnvironment("/ _ [C^1][V^1]"));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];
			Assert.That(rule.Gloss, Is.EqualTo("gloss"));
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(1));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {PrefixNull, ConsFS, VowelFS, AnyStar}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] { "<C_x005E_1>", "<V_x005E_1>", "d", "+", "<prefixNull>", "<C_x005E_1>", "<V_x005E_1>", "<stem>"}));
		}

		[Test]
		public void InvalidPartialReduplicationEnvironment()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphPrefix, "[C^1][V^1]d", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			var allo = (IMoAffixAllomorph) entry.LexemeFormOA;
			allo.PhoneEnvRC.Add(AddEnvironment("_ [C^1][V^1]"));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));
		}

		[Test]
		public void Circumfix()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphCircumfix, "d- -t", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			var prefix = (IMoAffixAllomorph) entry.AlternateFormsOS[0];
			prefix.PhoneEnvRC.Add(AddEnvironment("/ #[C] _ [C]"));
			var suffix = (IMoAffixAllomorph) entry.AlternateFormsOS[1];
			suffix.PhoneEnvRC.Add(AddEnvironment("/ [V] _ [V]"));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(1));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {PrefixNull + ConsFS + AnyStar + VowelFS + SuffixNull}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"d+", "<stem>", "+t"}));
			Assert.That(hcAllo.Environments.Select(e => e.ToEnvString()), Is.EquivalentTo(new[] {"/ " + LeftAnchorFS + ConsFS + " _ " + VowelFS}));
		}

		[Test]
		public void UnclassifedAffix()
		{
			AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified, MainPOS = m_verb});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];

			Assert.That(rule.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(rule.Gloss, Is.EqualTo("gloss"));
			Assert.That(rule.IsPartial, Is.True);
		}

		[Test]
		public void AffixNoMorphTypeSet()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA { MsaType = MsaType.kUnclassified, MainPOS = m_verb });
			entry.LexemeFormOA.MorphTypeRA = null;
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));
		}

		[Test]
		public void DerivationalAffix()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kDeriv, MainPOS = m_verb, SecondaryPOS = m_noun});
			var msa = (IMoDerivAffMsa) entry.MorphoSyntaxAnalysesOC.First();
			msa.FromInflectionClassRA = AddInflectionClass(m_verb, "verbClass");
			msa.ToInflectionClassRA = AddInflectionClass(m_noun, "nounClass");
			msa.FromProdRestrictRC.Add(AddExceptionFeature("fromExceptFeat"));
			msa.ToProdRestrictRC.Add(AddExceptionFeature("toExceptFeat"));
			var fsFactory = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>();
			msa.FromMsFeaturesOA = fsFactory.Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, msa.FromMsFeaturesOA, new FS {{"tense", "pres"}});
			msa.ToMsFeaturesOA = fsFactory.Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, msa.ToMsFeaturesOA, new FS {{"nounAgr", new FS {{"num", "pl"}}}});
			msa.FromStemNameRA = AddStemName(m_verb, "stemName", new FS {{"tense", "pres"}});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];

			Assert.That(rule.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[tense:pres], POS:V]"));
			Assert.That(rule.Gloss, Is.EqualTo("gloss"));
			Assert.That(rule.OutSyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[nounAgr:[num:pl]], POS:N]"));
			Assert.That(rule.RequiredStemName.ToString(), Is.EqualTo("stemName"));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.RequiredMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"verbClass", "fromExceptFeat"}));
			Assert.That(hcAllo.OutMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"nounClass", "toExceptFeat"}));
		}

		[Test]
		public void InflectionalAffixWithNoSlot()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kInfl, MainPOS = m_verb});
			var msa = (IMoInflAffMsa) entry.MorphoSyntaxAnalysesOC.First();
			msa.FromProdRestrictRC.Add(AddExceptionFeature("fromExceptFeat"));
			msa.InflFeatsOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, msa.InflFeatsOA, new FS {{"tense", "past"}});
			var allo = (IMoAffixAllomorph) entry.LexemeFormOA;
			IMoInflClass inflClass = AddInflectionClass(m_verb, "inflClass");
			AddInflectiononSubclass(inflClass, "inflSubclass");
			allo.InflectionClassesRC.Add(inflClass);
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];

			Assert.That(rule.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[tense:past], POS:V]"));
			Assert.That(rule.OutSyntacticFeatureStruct.ToString(), Is.EqualTo("ANY"));
			Assert.That(rule.Gloss, Is.EqualTo("gloss"));
			Assert.That(rule.Allomorphs[0].RequiredMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"fromExceptFeat", "inflClass", "inflSubclass"}));
			Assert.That(rule.IsPartial, Is.True);
		}

		[Test]
		public void AffixTemplate()
		{
			IMoInflAffixTemplate template = Cache.ServiceLocator.GetInstance<IMoInflAffixTemplateFactory>().Create();
			m_verb.AffixTemplatesOS.Add(template);
			template.Name.SetAnalysisDefaultWritingSystem("verbTemplate");
			template.Final = false;
			IMoInflAffixSlot prefixSlot1 = AddSlot(template, "prefixSlot1", true, true);
			AddEntry(MoMorphTypeTags.kguidMorphPrefix, "d", "gloss1", new SandboxGenericMSA {MsaType = MsaType.kInfl, MainPOS = m_verb, Slot = prefixSlot1});
			IMoInflAffixSlot prefixSlot2 = AddSlot(template, "prefixSlot2", true, false);
			AddEntry(MoMorphTypeTags.kguidMorphPrefix, "s", "gloss2", new SandboxGenericMSA {MsaType = MsaType.kInfl, MainPOS = m_verb, Slot = prefixSlot2});
			IMoInflAffixSlot suffixSlot = AddSlot(template, "suffixSlot", false, false);
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "t", "gloss3", new SandboxGenericMSA {MsaType = MsaType.kInfl, MainPOS = m_verb, Slot = suffixSlot});
			ILexEntryType type = Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypDialectalVar);
			entry.CreateVariantEntryAndBackRef(type, TsStringUtils.MakeString("ɯt", Cache.DefaultVernWs));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));

			Assert.That(m_lang.Strata[0].AffixTemplates.Count, Is.EqualTo(1));
			var hcTemplate = m_lang.Strata[0].AffixTemplates.First();
			Assert.That(hcTemplate.IsFinal, Is.False);
			Assert.That(hcTemplate.Slots.Count, Is.EqualTo(3));

			AffixTemplateSlot hcSlot = hcTemplate.Slots[0];
			Assert.That(hcSlot.Optional, Is.False);
			Assert.That(hcSlot.Rules.Count, Is.EqualTo(2));
			Assert.That(hcSlot.Rules.First().ToString(), Is.EqualTo("-t"));
			Assert.That(hcSlot.Rules.Last().ToString(), Is.EqualTo("-ɯt"));

			hcSlot = hcTemplate.Slots[1];
			Assert.That(hcSlot.Optional, Is.False);
			Assert.That(hcSlot.Rules.Count, Is.EqualTo(1));
			Assert.That(hcSlot.Rules.First().ToString(), Is.EqualTo("s-"));

			hcSlot = hcTemplate.Slots[2];
			Assert.That(hcSlot.Optional, Is.True);
			Assert.That(hcSlot.Rules.Count, Is.EqualTo(1));
			Assert.That(hcSlot.Rules.First().ToString(), Is.EqualTo("d-"));
		}

		[Test]
		public void InvalidSlot()
		{
			IMoInflAffixTemplate template = Cache.ServiceLocator.GetInstance<IMoInflAffixTemplateFactory>().Create();
			m_verb.AffixTemplatesOS.Add(template);
			template.Name.SetAnalysisDefaultWritingSystem("verbTemplate");
			template.Final = false;
			IMoInflAffixSlot prefixSlot1 = AddSlot(template, "prefixSlot1", true, true);
			AddEntry(MoMorphTypeTags.kguidMorphPrefix, "h", "gloss1", new SandboxGenericMSA {MsaType = MsaType.kInfl, MainPOS = m_verb, Slot = prefixSlot1});
			IMoInflAffixSlot prefixSlot2 = AddSlot(template, "prefixSlot2", true, false);
			AddEntry(MoMorphTypeTags.kguidMorphPrefix, "s", "gloss2", new SandboxGenericMSA {MsaType = MsaType.kInfl, MainPOS = m_verb, Slot = prefixSlot2});
			IMoInflAffixSlot suffixSlot = AddSlot(template, "suffixSlot", false, false);
			AddEntry(MoMorphTypeTags.kguidMorphSuffix, "t", "gloss3", new SandboxGenericMSA {MsaType = MsaType.kInfl, MainPOS = m_verb, Slot = suffixSlot});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));

			Assert.That(m_lang.Strata[0].AffixTemplates.Count, Is.EqualTo(1));
			var hcTemplate = m_lang.Strata[0].AffixTemplates.First();
			Assert.That(hcTemplate.IsFinal, Is.False);
			Assert.That(hcTemplate.Slots.Count, Is.EqualTo(2));
		}

		[Test]
		public void Stem()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphBoundStem, "sag", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem, MainPOS = m_verb});
			IMoInflClass inflClass = AddInflectionClass(m_verb, "inflClass");
			AddInflectiononSubclass(inflClass, "inflSubclass");
			var msa = (IMoStemMsa) entry.MorphoSyntaxAnalysesOC.First();
			msa.InflectionClassRA = inflClass;
			var allo = (IMoStemAllomorph) entry.LexemeFormOA;
			allo.PhoneEnvRC.Add(AddEnvironment("/ [V] _ [V]#"));
			allo.PhoneEnvRC.Add(AddEnvironment("/ [C] _ [C]"));
			allo.StemNameRA = AddStemName(m_verb, "stemName", new FS {{"tense", "pres"}});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(1));
			LexEntry hcEntry = m_lang.Strata[0].Entries.First();

			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss"));
			Assert.That(hcEntry.SyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(hcEntry.MprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"inflClass"}));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));

			RootAllomorph hcAllo = hcEntry.PrimaryAllomorph;
			Assert.That(hcAllo.Segments.ToString(), Is.EqualTo("sag"));
			Assert.That(hcAllo.StemName.ToString(), Is.EqualTo("stemName"));
			Assert.That(hcAllo.IsBound, Is.True);
			Assert.That(hcAllo.Environments.Select(e => e.ToEnvString()), Is.EquivalentTo(new[]
			{
				"/ " + VowelFS + " _ " + VowelFS + RightAnchorFS,
				"/ " + ConsFS + " _ " + ConsFS
			}));
		}

		[Test]
		public void StemNoMorphTypeSet()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphBoundStem, "sag", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem, MainPOS = m_verb});
			entry.LexemeFormOA.MorphTypeRA = null;
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(0));
		}

		[Test]
		public void PartialStem()
		{
			AddEntry(MoMorphTypeTags.kguidMorphBoundStem, "sag", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem, MainPOS = null});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(1));
			LexEntry hcEntry = m_lang.Strata[0].Entries.First();
			Assert.That(hcEntry.SyntacticFeatureStruct.ToString(), Is.EqualTo("ANY"));
			Assert.That(hcEntry.IsPartial, Is.True);
		}

		[Test]
		public void Enclitic()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphEnclitic, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem, MainPOS = m_particle});
			var msa = (IMoStemMsa) entry.MorphoSyntaxAnalysesOC.First();
			msa.FromPartsOfSpeechRC.Add(m_noun);
			var allo = (IMoStemAllomorph) entry.LexemeFormOA;
			allo.PhoneEnvRC.Add(AddEnvironment("/ [V] _ #"));
			allo.PhoneEnvRC.Add(AddEnvironment("/ #[C] _ [C]"));
			LoadLanguage();

			Assert.That(m_lang.Strata[1].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[1].MorphologicalRules[0];
			Assert.That(rule.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:N]"));
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(2));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyStar + VowelFS + SuffixNull}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<stem>", "+ɯd"}));
			Assert.That(hcAllo.Environments.Select(e => e.ToEnvString()), Is.EquivalentTo(new[] {"/ _ " + RightAnchorFS}));

			hcAllo = rule.Allomorphs[1];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {PrefixNull + ConsFS + SuffixNull}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<stem>", "+ɯd"}));
			Assert.That(hcAllo.Environments.Select(e => e.ToEnvString()), Is.EquivalentTo(new[] {"/ _ " + ConsFS}));
		}

		[Test]
		public void EncliticAffixAllomorph()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			entry.LexemeFormOA.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphEnclitic);
			LoadLanguage();

			Assert.That(m_lang.Strata[1].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[1].MorphologicalRules[0];
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(1));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyPlus}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<stem>", "+ɯd"}));
		}

		[Test]
		public void Clitic()
		{
			AddEntry(MoMorphTypeTags.kguidMorphClitic, "sag", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem});
			LoadLanguage();

			Assert.That(m_lang.Strata[1].Entries.Count, Is.EqualTo(1));
			LexEntry hcEntry = m_lang.Strata[1].Entries.First();

			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));

			RootAllomorph hcAllo = hcEntry.PrimaryAllomorph;
			Assert.That(hcAllo.Segments.ToString(), Is.EqualTo("sag"));
		}

		[Test]
		public void Particle()
		{
			AddEntry(MoMorphTypeTags.kguidMorphParticle, "sag", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem});
			LoadLanguage();

			Assert.That(m_lang.Strata[1].Entries.Count, Is.EqualTo(1));
			LexEntry hcEntry = m_lang.Strata[1].Entries.First();

			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));

			RootAllomorph hcAllo = hcEntry.PrimaryAllomorph;
			Assert.That(hcAllo.Segments.ToString(), Is.EqualTo("sag"));
		}

		[Test]
		public void EndoCompoundRule()
		{
			IMoEndoCompound compoundRule = Cache.ServiceLocator.GetInstance<IMoEndoCompoundFactory>().Create();
			Cache.LanguageProject.MorphologicalDataOA.CompoundRulesOS.Add(compoundRule);
			compoundRule.Name.SetAnalysisDefaultWritingSystem("compound");
			compoundRule.LeftMsaOA.PartOfSpeechRA = m_verb;
			compoundRule.RightMsaOA.PartOfSpeechRA = m_noun;
			compoundRule.OverridingMsaOA.PartOfSpeechRA = m_adj;
			compoundRule.HeadLast = true;
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var hcCompoundRule = (CompoundingRule) m_lang.Strata[0].MorphologicalRules[0];

			Assert.That(hcCompoundRule.HeadRequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:N]"));
			Assert.That(hcCompoundRule.NonHeadRequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(hcCompoundRule.OutSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:A]"));

			Assert.That(hcCompoundRule.Subrules.Count, Is.EqualTo(1));
			CompoundingSubrule subrule = hcCompoundRule.Subrules[0];
			Assert.That(subrule.HeadLhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyPlus}));
			Assert.That(subrule.NonHeadLhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyPlus}));
			Assert.That(subrule.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<nonhead>", "+", "<head>"}));
		}

		[Test]
		public void ExoCompoundRule()
		{
			IMoExoCompound compoundRule = Cache.ServiceLocator.GetInstance<IMoExoCompoundFactory>().Create();
			Cache.LanguageProject.MorphologicalDataOA.CompoundRulesOS.Add(compoundRule);
			compoundRule.Name.SetAnalysisDefaultWritingSystem("compound");
			compoundRule.LeftMsaOA.PartOfSpeechRA = m_verb;
			compoundRule.RightMsaOA.PartOfSpeechRA = m_noun;
			compoundRule.ToMsaOA.PartOfSpeechRA = m_adj;
			compoundRule.ToMsaOA.InflectionClassRA = AddInflectionClass(m_adj, "inflClass");
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(2));
			var hcCompoundRule = (CompoundingRule) m_lang.Strata[0].MorphologicalRules[0];

			Assert.That(hcCompoundRule.HeadRequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:N]"));
			Assert.That(hcCompoundRule.NonHeadRequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(hcCompoundRule.OutSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:A]"));

			Assert.That(hcCompoundRule.Subrules.Count, Is.EqualTo(1));
			CompoundingSubrule subrule = hcCompoundRule.Subrules[0];
			Assert.That(subrule.HeadLhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyPlus}));
			Assert.That(subrule.NonHeadLhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyPlus}));
			Assert.That(subrule.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<nonhead>", "+", "<head>"}));
			Assert.That(subrule.OutMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"inflClass"}));

			hcCompoundRule = (CompoundingRule) m_lang.Strata[0].MorphologicalRules[1];

			Assert.That(hcCompoundRule.HeadRequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(hcCompoundRule.NonHeadRequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:N]"));
			Assert.That(hcCompoundRule.OutSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:A]"));

			Assert.That(hcCompoundRule.Subrules.Count, Is.EqualTo(1));
			subrule = hcCompoundRule.Subrules[0];
			Assert.That(subrule.HeadLhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyPlus}));
			Assert.That(subrule.NonHeadLhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyPlus}));
			Assert.That(subrule.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<head>", "+", "<nonhead>"}));
			Assert.That(subrule.OutMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"inflClass"}));
		}

		[Test]
		public void AdhocAllomorphProhibitionRule()
		{
			ILexEntry stemEntry = AddEntry(MoMorphTypeTags.kguidMorphStem, "sag", "gloss1", new SandboxGenericMSA {MsaType = MsaType.kStem});
			ILexEntry suffixEntry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss2", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			IMoAlloAdhocProhib adhocRule = Cache.ServiceLocator.GetInstance<IMoAlloAdhocProhibFactory>().Create();
			Cache.LanguageProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Add(adhocRule);
			adhocRule.FirstAllomorphRA = stemEntry.LexemeFormOA;
			adhocRule.RestOfAllosRS.Add(suffixEntry.LexemeFormOA);
			adhocRule.Adjacency = 2;
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(1));
			LexEntry hcStemEntry = m_lang.Strata[0].Entries.First();
			Assert.That(hcStemEntry.PrimaryAllomorph.AllomorphCoOccurrenceRules.Count, Is.EqualTo(1));
			AllomorphCoOccurrenceRule coOccurRule = hcStemEntry.PrimaryAllomorph.AllomorphCoOccurrenceRules.First();
			Assert.That(coOccurRule.Adjacency, Is.EqualTo(MorphCoOccurrenceAdjacency.SomewhereToRight));
			Assert.That(coOccurRule.Key.Morpheme.ToString(), Is.EqualTo("sag"));
			Assert.That(coOccurRule.Others.Select(a => a.Morpheme.ToString()), Is.EqualTo(new[] {"-ɯd"}));
		}

		[Test]
		public void AdhocMorphemeProhibitionRule()
		{
			ILexEntry stemEntry = AddEntry(MoMorphTypeTags.kguidMorphStem, "sag", "gloss1", new SandboxGenericMSA {MsaType = MsaType.kStem});
			ILexEntry suffixEntry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss2", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			IMoMorphAdhocProhib adhocRule = Cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibFactory>().Create();
			Cache.LanguageProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Add(adhocRule);
			adhocRule.FirstMorphemeRA = stemEntry.MorphoSyntaxAnalysesOC.First();
			adhocRule.RestOfMorphsRS.Add(suffixEntry.MorphoSyntaxAnalysesOC.First());
			adhocRule.Adjacency = 4;
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(1));
			LexEntry hcStemEntry = m_lang.Strata[0].Entries.First();
			Assert.That(hcStemEntry.MorphemeCoOccurrenceRules.Count, Is.EqualTo(1));
			MorphemeCoOccurrenceRule coOccurRule = hcStemEntry.MorphemeCoOccurrenceRules.First();
			Assert.That(coOccurRule.Adjacency, Is.EqualTo(MorphCoOccurrenceAdjacency.AdjacentToRight));
			Assert.That(coOccurRule.Key.ToString(), Is.EqualTo("sag"));
			Assert.That(coOccurRule.Others.Select(a => a.ToString()), Is.EqualTo(new[] {"-ɯd"}));
		}

		[Test]
		public void PhonologicalRule()
		{
			IPhRegularRule prule = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonRulesOS.Add(prule);
			prule.Name.SetAnalysisDefaultWritingSystem("prule");
			prule.Direction = 2;
			IPhSimpleContextNC ncCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			prule.StrucDescOS.Add(ncCtxt);
			ncCtxt.FeatureStructureRA = m_vowel;

			IPhSegRuleRHS rhs = prule.RightHandSidesOS[0];
			rhs.InputPOSesRC.Add(m_verb);
			rhs.ReqRuleFeatsRC.Add(AddPhonRuleFeature(AddInflectionClass(m_verb, "inflClass")));
			rhs.ExclRuleFeatsRC.Add(AddPhonRuleFeature(AddExceptionFeature("exceptFeat")));
			ncCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			rhs.StrucChangeOS.Add(ncCtxt);
			ncCtxt.FeatureStructureRA = AddNaturalClass("output", new FS {{"cont", "+"}});
			IPhFeatureConstraint constr = AddFeatureConstraint("vd");
			ncCtxt.PlusConstrRS.Add(constr);

			IPhSequenceContext seqCtxt = Cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
			rhs.LeftContextOA = seqCtxt;
			seqCtxt.MembersRS.Add(AddBdryContext(LangProjectTags.kguidPhRuleWordBdry));
			seqCtxt.MembersRS.Add(AddSegContext("t"));

			ncCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			rhs.RightContextOA = ncCtxt;
			ncCtxt.FeatureStructureRA = AddNaturalClass("right", new FS {{"poa", "alveolar"}});
			ncCtxt.MinusConstrRS.Add(constr);
			LoadLanguage();

			Assert.That(m_lang.Strata[0].PhonologicalRules.Count, Is.EqualTo(1));
			var hcPrule = (RewriteRule) m_lang.Strata[0].PhonologicalRules[0];

			Assert.That(hcPrule.Direction, Is.EqualTo(Machine.DataStructures.Direction.LeftToRight));
			Assert.That(hcPrule.ApplicationMode, Is.EqualTo(RewriteApplicationMode.Simultaneous));
			Assert.That(hcPrule.Lhs.ToString(), Is.EqualTo(VowelFS));

			Assert.That(hcPrule.Subrules.Count, Is.EqualTo(1));
			RewriteSubrule subrule = hcPrule.Subrules[0];

			Assert.That(subrule.LeftEnvironment.ToString(), Is.EqualTo(LeftAnchorFS + m_lang.Strata[0].CharacterDefinitionTable["t"].FeatureStruct));
			Assert.That(subrule.Rhs.ToString(), Is.EqualTo("[cont:+, Type:segment, vd:+α]"));
			Assert.That(subrule.RightEnvironment.ToString(), Is.EqualTo("[poa:alveolar, Type:segment, vd:-α]"));
			Assert.That(subrule.RequiredMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"inflClass"}));
			Assert.That(subrule.ExcludedMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"exceptFeat"}));
			Assert.That(subrule.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
		}

		[Test]
		public void MetathesisRule()
		{
			IPhMetathesisRule prule = Cache.ServiceLocator.GetInstance<IPhMetathesisRuleFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonRulesOS.Add(prule);
			prule.Name.SetAnalysisDefaultWritingSystem("prule");
			prule.Direction = 1;

			IPhSimpleContextNC ncCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			prule.StrucDescOS.Add(ncCtxt);
			ncCtxt.FeatureStructureRA = m_vowel;
			prule.UpdateStrucChange(PhMetathesisRuleTags.kidxLeftEnv, 0, true);

			IPhSimpleContextSeg segCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			prule.StrucDescOS.Add(segCtxt);
			segCtxt.FeatureStructureRA = GetPhoneme("a");
			prule.UpdateStrucChange(PhMetathesisRuleTags.kidxLeftSwitch, 1, true);

			segCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			prule.StrucDescOS.Add(segCtxt);
			segCtxt.FeatureStructureRA = GetPhoneme("t");
			prule.UpdateStrucChange(PhMetathesisRuleTags.kidxRightSwitch, 2, true);

			ncCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			prule.StrucDescOS.Add(ncCtxt);
			ncCtxt.FeatureStructureRA = m_cons;
			prule.UpdateStrucChange(PhMetathesisRuleTags.kidxRightEnv, 3, true);
			LoadLanguage();

			Assert.That(m_lang.Strata[0].PhonologicalRules.Count, Is.EqualTo(1));
			var hcPrule = (MetathesisRule) m_lang.Strata[0].PhonologicalRules[0];

			Assert.That(hcPrule.Direction, Is.EqualTo(Machine.DataStructures.Direction.RightToLeft));
			Assert.That(hcPrule.Pattern.ToString(), Is.EqualTo(string.Format("({0})({1})({2})({3})", VowelFS,
				m_lang.Strata[0].CharacterDefinitionTable["a"].FeatureStruct, m_lang.Strata[0].CharacterDefinitionTable["t"].FeatureStruct, ConsFS)));
			Assert.That(hcPrule.LeftSwitchName, Is.EqualTo("r"));
			Assert.That(hcPrule.RightSwitchName, Is.EqualTo("l"));

			// Hermit Crab uses the group names as unique dictionary keys in AnalysisMetathesisRuleSpec() constructor.
			// Group names of all pattern children must be non-null.
			foreach (var child in hcPrule.Pattern.Children)
			{
				Assert.IsNotNull(((Group<Word,ShapeNode>) child).Name);
			}
			// Group names of all children must be unique.
			var namesList = hcPrule.Pattern.Children.Select(child => new{str = ((Group<Word, ShapeNode>)child).Name});
			var uniqueNames = namesList.Distinct();
			Assert.That(uniqueNames.Count() == namesList.Count());
		}

		[Test]
		public void AffixProcessRule()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			entry.ReplaceMoForm(entry.LexemeFormOA, Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create());
			var allo = (IMoAffixProcess) entry.LexemeFormOA;
			allo.InputOS.Clear();

			IPhIterationContext input1 = Cache.ServiceLocator.GetInstance<IPhIterationContextFactory>().Create();
			allo.InputOS.Add(input1);
			input1.Minimum = 2;
			input1.Maximum = -1;
			IPhSequenceContext seqCtxt = Cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.ContextsOS.Add(seqCtxt);
			seqCtxt.MembersRS.Add(AddNCContext(m_cons));
			seqCtxt.MembersRS.Add(AddNCContext(m_cons));
			input1.MemberRA = seqCtxt;

			IPhSimpleContextNC input2 = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			allo.InputOS.Add(input2);
			input2.FeatureStructureRA = AddNaturalClass("roundVowels", new FS {{"voc", "+"}, {"round", "+"}});

			IPhVariable input3 = Cache.ServiceLocator.GetInstance<IPhVariableFactory>().Create();
			allo.InputOS.Add(input3);

			IMoCopyFromInput output1 = Cache.ServiceLocator.GetInstance<IMoCopyFromInputFactory>().Create();
			allo.OutputOS.Add(output1);
			output1.ContentRA = input1;

			IMoInsertPhones output2 = Cache.ServiceLocator.GetInstance<IMoInsertPhonesFactory>().Create();
			allo.OutputOS.Add(output2);
			output2.ContentRS.Add(GetBdry(LangProjectTags.kguidPhRuleMorphBdry));
			output2.ContentRS.Add(GetPhoneme("a"));
			output2.ContentRS.Add(GetBdry(LangProjectTags.kguidPhRuleMorphBdry));

			IMoModifyFromInput output3 = Cache.ServiceLocator.GetInstance<IMoModifyFromInputFactory>().Create();
			allo.OutputOS.Add(output3);
			output3.ContentRA = input2;
			output3.ModificationRA = AddNaturalClass("unroundedVowels", new FS {{"voc", "+"}, {"round", "-"}});

			IMoInsertNC output4 = Cache.ServiceLocator.GetInstance<IMoInsertNCFactory>().Create();
			allo.OutputOS.Add(output4);
			output4.ContentRA = AddNaturalClass("ŋ", new FS {{"cons", "+"}, {"voc", "-"}, {"poa", "velar"}, {"vd", "+"}, {"cont", "-"}, {"nasal", "+"}});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(1));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {"(" + ConsFS + ConsFS + ")[2,]", "[round:+, Type:segment, voc:+]", AnyStar}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[]
			{
				"<1>",
				"+a+",
				"<2> -> [round:-, Type:segment, voc:+]",
				"[cons:+, cont:-, nasal:+, poa:velar, Type:segment, vd:+, voc:-]"
			}));
		}

		[Test]
		public void AffixProcessRuleNoMorphTypeSet()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA { MsaType = MsaType.kUnclassified });
			entry.LexemeFormOA.MorphTypeRA = null;
			entry.ReplaceMoForm(entry.LexemeFormOA, Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create());
			var allo = (IMoAffixProcess)entry.LexemeFormOA;
			allo.InputOS.Clear();

			IPhVariable input1 = Cache.ServiceLocator.GetInstance<IPhVariableFactory>().Create();
			allo.InputOS.Add(input1);

			IMoCopyFromInput output1 = Cache.ServiceLocator.GetInstance<IMoCopyFromInputFactory>().Create();
			allo.OutputOS.Add(output1);
			output1.ContentRA = input1;

			IMoInsertPhones output2 = Cache.ServiceLocator.GetInstance<IMoInsertPhonesFactory>().Create();
			allo.OutputOS.Add(output2);
			output2.ContentRS.Add(GetBdry(LangProjectTags.kguidPhRuleMorphBdry));
			output2.ContentRS.Add(GetPhoneme("a"));

			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule)m_lang.Strata[0].MorphologicalRules[0];
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(1));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] { AnyStar }));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[]
			{
				"<1>",
				"+a"
			}));
		}

		[Test]
		public void InvalidAffixProcessRule()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			entry.ReplaceMoForm(entry.LexemeFormOA, Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create());
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));
		}

		[Test]
		public void IrregularInflectionVariantWithNonemptyAffixTemplate()
		{
			IFsFeatureSystem msFeatSys = Cache.LanguageProject.MsFeatureSystemOA;

			AddClosedFeature(msFeatSys, "case", "acc", "nom", "voc");

			// create an affix template
			IMoInflAffixTemplate template = Cache.ServiceLocator
				.GetInstance<IMoInflAffixTemplateFactory>().Create();
			m_noun.AffixTemplatesOS.Add(template);
			template.Name.SetAnalysisDefaultWritingSystem("nounTemplate");
			template.Final = false;

			// add a slot and a suffix entry to the affix template
			IMoInflAffixSlot caseNumSlot = AddSlot(template, "case/number", true, false);
			AddEntry(MoMorphTypeTags.kguidMorphSuffix, "d", "gloss1",
				new SandboxGenericMSA
				{ MsaType = MsaType.kInfl, MainPOS = m_noun, Slot = caseNumSlot });

			// create a bound stem entry
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphBoundStem, "sag", "gloss",
				new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = m_noun });

			// define custom variant type 1 and add slot
			ILexEntryInflType type1 = Cache.ServiceLocator
				.GetInstance<ILexEntryInflTypeRepository>()
				.GetObject(LexEntryTypeTags.kguidLexTypIrregInflectionVar);
			type1.GlossAppend.SetAnalysisDefaultWritingSystem(".acc");
			type1.SlotsRC.Add(caseNumSlot);

			// add inflection feature to variant type 1
			type1.InflFeatsOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, type1.InflFeatsOA, new FS { { "nounAgr", new FS { { "case", "acc" } } } });

			// create variant1
			ILexEntry variantEntry = AddEntry(MoMorphTypeTags.kguidMorphStem, "sau", "gloss",
				new SandboxGenericMSA { MsaType = MsaType.kInfl, MainPOS = m_noun });
			variantEntry.MakeVariantOf(entry, type1);

			// create a new ILexEntryInflType for custom variant type 2
			ILexEntryInflType type2 = Cache.ServiceLocator.GetInstance<ILexEntryInflTypeFactory>()
				.Create();
			//  add new type to the project collection
			Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(type2);

			type2.GlossAppend.SetAnalysisDefaultWritingSystem(".nom");
			type2.SlotsRC.Add(caseNumSlot);

			// adding inflection feature to variant type 2
			type2.InflFeatsOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, type2.InflFeatsOA, new FS { { "nounAgr", new FS { { "case", "nom" } } } });

			// creating variant 2
			ILexEntry variantEntry2 = AddEntry(MoMorphTypeTags.kguidMorphStem, "sal", "gloss",
				new SandboxGenericMSA { MsaType = MsaType.kInfl, MainPOS = m_noun });
			variantEntry2.MakeVariantOf(entry, type2);

			LoadLanguage();
			Assert.That(m_lang.Strata[0].AffixTemplates.Count, Is.EqualTo(1));
			var hcTemplate = m_lang.Strata[0].AffixTemplates.First();

			Assert.That(hcTemplate.Slots.Count, Is.EqualTo(1));
			AffixTemplateSlot hcSlot = hcTemplate.Slots[0];

			Assert.That(hcSlot.Optional, Is.False);

			// Should have an affix process rule for the suffix entry.
			// Should have created two null affix process rules
			// --one for each of the two irregularly inflected variant types.
			Assert.That(hcSlot.Rules.Count, Is.EqualTo(3));
		}

		[Test]
		public void VariantStem()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphStem, "sag", "gloss", new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = m_verb });
			ILexEntryInflType type = Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPluralVar);
			type.GlossAppend.SetAnalysisDefaultWritingSystem(".pl");
			type.InflFeatsOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, type.InflFeatsOA, new FS { { "nounAgr", new FS { { "num", "pl" } } } });
			ILexEntryRef entryRef = entry.CreateVariantEntryAndBackRef(type, TsStringUtils.MakeString("sau", Cache.DefaultVernWs));
			entryRef.VariantEntryTypesRS.Add(Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypFreeVar));

			ILexEntryInflType type3 = Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypIrregInflectionVar);
			type3.GlossAppend.SetAnalysisDefaultWritingSystem(".sg");
			type3.InflFeatsOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, type3.InflFeatsOA, new FS { { "nounAgr", new FS { { "num", "sg" } } } });
			ILexEntryRef entryRef3 = entry.CreateVariantEntryAndBackRef(type3, TsStringUtils.MakeString("sai", Cache.DefaultVernWs));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(4));
			LexEntry hcEntry = m_lang.Strata[0].Entries.ElementAt(0);
			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));
			Assert.That(hcEntry.PrimaryAllomorph.Segments.ToString(), Is.EqualTo("sag"));

			hcEntry = m_lang.Strata[0].Entries.ElementAt(1);
			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss.pl"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));
			Assert.That(hcEntry.PrimaryAllomorph.Segments.ToString(), Is.EqualTo("sau"));
			Assert.That(hcEntry.SyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[nounAgr:[num:pl]], POS:V]"));
			Assert.That(hcEntry.MprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] { "Plural Variant" }));

			hcEntry = m_lang.Strata[0].Entries.ElementAt(2);
			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));
			Assert.That(hcEntry.PrimaryAllomorph.Segments.ToString(), Is.EqualTo("sau"));
			Assert.That(hcEntry.SyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(hcEntry.MprFeatures, Is.Empty);

			hcEntry = m_lang.Strata[0].Entries.ElementAt(3);
			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss.sg"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));
			Assert.That(hcEntry.PrimaryAllomorph.Segments.ToString(), Is.EqualTo("sai"));
			Assert.That(hcEntry.SyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[nounAgr:[num:sg]], POS:V]"));
			Assert.That(hcEntry.MprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] { "Irregular Inflectional Variant" }));
		}

		[Test]
		public void VariantAffix()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified, MainPOS = m_verb});
			ILexEntryType type = Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypFreeVar);
			entry.CreateVariantEntryAndBackRef(type, TsStringUtils.MakeString("ɯt", Cache.DefaultVernWs));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(2));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];

			Assert.That(rule.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(rule.Gloss, Is.EqualTo("gloss"));

			rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[1];

			Assert.That(rule.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(rule.Gloss, Is.EqualTo("gloss"));
		}

		[Test]
		public void AcceptUnspecifiedGraphemes()
		{
			AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ed", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified, MainPOS = m_verb});
			AddEntry(MoMorphTypeTags.kguidMorphBoundStem, "sȧg", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem, MainPOS = m_verb});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].CharacterDefinitionTable.Contains("e"), Is.False);
			Assert.That(m_lang.Strata[0].CharacterDefinitionTable.Contains("ȧ"), Is.False);

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(0));
			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));

			Cache.LanguageProject.MorphologicalDataOA.ParserParameters = "<ParserParameters><ActiveParser>HC</ActiveParser><HC><NoDefaultCompounding>true</NoDefaultCompounding><AcceptUnspecifiedGraphemes>true</AcceptUnspecifiedGraphemes></HC></ParserParameters>";
			LoadLanguage();

			CharacterDefinition cd;
			Assert.That(m_lang.Strata[0].CharacterDefinitionTable.TryGetValue("e", out cd), Is.True);
			Assert.That(cd.FeatureStruct.ValueEquals(FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Feature(HCFeatureSystem.StrRep).EqualTo("e").Value), Is.True);
			Assert.That(m_lang.Strata[0].CharacterDefinitionTable.TryGetValue("ȧ", out cd), Is.True);
			Assert.That(cd.FeatureStruct.ValueEquals(FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Feature(HCFeatureSystem.StrRep).EqualTo("ȧ").Value), Is.True);

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(1));
			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
		}

		[Test]
		public void EmptyStemName()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphBoundStem, "sag", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem, MainPOS = m_verb});
			var allo = (IMoStemAllomorph) entry.LexemeFormOA;
			allo.StemNameRA = AddStemName(m_verb, "stemName", new FS());
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(1));
			LexEntry hcEntry = m_lang.Strata[0].Entries.First();

			RootAllomorph hcAllo = hcEntry.PrimaryAllomorph;
			Assert.That(hcAllo.StemName, Is.Null);
		}

		[Test]
		public void DuplicateGraphemes()
		{
			AddPhoneme("N", new FS {{"nasal", "+"}}, "n");
			LoadLanguage();

			Assert.That(m_lang.Strata[0].CharacterDefinitionTable.Count, Is.EqualTo(27));
			Assert.That(m_loadErrors.Count, Is.EqualTo(1));
			Assert.That(m_loadErrors[0].Item1, Is.EqualTo(LoadErrorType.DuplicateGrapheme));
		}
	}
}
