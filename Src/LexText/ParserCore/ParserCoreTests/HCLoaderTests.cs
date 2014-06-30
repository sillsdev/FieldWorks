using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.Collections;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.HermitCrab;
using SIL.HermitCrab.MorphologicalRules;
using SIL.HermitCrab.PhonologicalRules;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using FS = System.Collections.Generic.Dictionary<string, object>;

namespace SIL.FieldWorks.WordWorks.Parser
{
	[TestFixture]
	public class HCLoaderTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string PrefixNull = "([StrRep:\"^0\", Type:boundary][StrRep:\"+\", Type:boundary])*";
		private const string SuffixNull = "([StrRep:\"+\", Type:boundary][StrRep:\"^0\", Type:boundary])*";
		private const string AnyPlus = PrefixNull + "ANY+" + SuffixNull;
		private const string AnyStar = PrefixNull + "ANY*" + SuffixNull;
		private const string VowelFS = "[cons:-, Type:segment, voc:+]";
		private const string ConsFS = "[cons:+, Type:segment, voc:-]";
		private const string RightAnchorFS = "[AnchorType:RightSide, Type:anchor]";
		private const string LeftAnchorFS = "[AnchorType:LeftSide, Type:anchor]";

		private SpanFactory<ShapeNode> m_spanFactory;
		private string m_dataIssues;
		private Language m_lang;
		private IPartOfSpeech m_noun;
		private IPartOfSpeech m_verb;
		private IPartOfSpeech m_adj;
		private IFsFeatStrucType m_inflType;
		private IPhNCFeatures m_vowel;
		private IPhNCFeatures m_cons;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_spanFactory = new ShapeSpanFactory();
		}

		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_noun = AddPartOfSpeech("N");
			m_verb = AddPartOfSpeech("V");
			m_adj = AddPartOfSpeech("A");

			Cache.LanguageProject.MorphologicalDataOA.ParserParameters = "<ParserParameters><ActiveParser>HC</ActiveParser><HC><NoDefaultCompounding>true</NoDefaultCompounding></HC></ParserParameters>";

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
			env.StringRepresentation = Cache.TsStrFactory.MakeString(strRep, Cache.DefaultVernWs);
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

		private void AddPhoneme(string strRep, FS featVals)
		{
			IPhPhoneme phoneme = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(phoneme);
			phoneme.Name.SetVernacularDefaultWritingSystem(strRep);
			IPhCode code = phoneme.CodesOS[0];
			code.Representation.SetVernacularDefaultWritingSystem(strRep);

			phoneme.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.PhFeatureSystemOA, null, phoneme.FeaturesOA, featVals);
		}

		private void CreateFeatStruc(IFsFeatureSystem featSys, IFsFeatStrucType type, IFsFeatStruc fs, FS featVals)
		{
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
			bdry.Name.SetVernacularDefaultWritingSystem(strRep);
			IPhCode code = Cache.ServiceLocator.GetInstance<IPhCodeFactory>().Create();
			bdry.CodesOS.Add(code);
			code.Representation.SetVernacularDefaultWritingSystem(strRep);
		}

		private ILexEntry AddEntry(Guid morphType, string lexemeForm, string gloss, SandboxGenericMSA msa)
		{
			return Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(morphType),
				Cache.TsStrFactory.MakeString(lexemeForm, Cache.DefaultVernWs), gloss, msa);
		}

		private IMoInflClass AddInflectionClass(IPartOfSpeech pos, string name)
		{
			var inflClass = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos.InflectionClassesOC.Add(inflClass);
			inflClass.Name.SetAnalysisDefaultWritingSystem(name);
			inflClass.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			return inflClass;
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
			var dataIssues = new StringBuilder();
			using (XmlWriter writer = XmlWriter.Create(dataIssues))
			{
				m_lang = HCLoader.Load(m_spanFactory, Cache, writer);
			}
			m_dataIssues = dataIssues.ToString();
		}

		[Test]
		public void PhonologicalFeatures()
		{
			LoadLanguage();
			Assert.That(m_lang.PhoneticFeatureSystem.Count, Is.EqualTo(13));
			var voc = (SymbolicFeature) m_lang.PhoneticFeatureSystem.First(f => f.Description == "voc");
			Assert.That(voc.PossibleSymbols.Select(s => s.Description), Is.EquivalentTo(new[] {"+", "-"}));
			var poa = (SymbolicFeature) m_lang.PhoneticFeatureSystem.First(f => f.Description == "poa");
			Assert.That(poa.PossibleSymbols.Select(s => s.Description), Is.EquivalentTo(new[] {"bilabial", "labiodental", "alveolar", "velar"}));
		}

		[Test]
		public void PhonemesAndBoundaries()
		{
			LoadLanguage();
			Assert.That(m_lang.SurfaceStratum.SymbolTable.GetSymbolFeatureStruct("a").ToString(), Is.EqualTo("[back:+, cons:-, high:-, low:+, round:-, Type:segment, vd:+, voc:+]"));
			Assert.That(m_lang.SurfaceStratum.SymbolTable.GetSymbolFeatureStruct("pʰ").ToString(), Is.EqualTo("[asp:+, cons:+, cont:-, nasal:-, poa:bilabial, strident:-, Type:segment, vd:-, voc:-]"));
			Assert.That(m_lang.SurfaceStratum.SymbolTable.GetSymbolFeatureStruct("+").ToString(), Is.EqualTo("[StrRep:\"+\", Type:boundary]"));
		}

		[Test]
		public void Suffix()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphSuffix, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kUnclassified});
			var allo = (IMoAffixAllomorph) entry.LexemeFormOA;
			allo.PhoneEnvRC.Add(AddEnvironment("/ [V] _ [V]#"));
			allo.PhoneEnvRC.Add(AddEnvironment("/ #[C] _ [C]"));
			allo.MsEnvFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, allo.MsEnvFeaturesOA, new FS {{"tense", "pres"}});
			allo.InflectionClassesRC.Add(AddInflectionClass(m_verb, "inflClass"));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];
			Assert.That(rule.Allomorphs.Count, Is.EqualTo(2));

			AffixProcessAllomorph hcAllo = rule.Allomorphs[0];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyStar + VowelFS + SuffixNull}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<stem>", "+ɯd"}));
			Assert.That(hcAllo.RequiredEnvironments.Select(e => e.ToString()), Is.EquivalentTo(new[] {"/ _ " + VowelFS + RightAnchorFS}));
			Assert.That(hcAllo.RequiredMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"inflClass"}));
			Assert.That(hcAllo.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[tense:pres]]"));

			hcAllo = rule.Allomorphs[1];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {PrefixNull + ConsFS + SuffixNull}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<stem>", "+ɯd"}));
			Assert.That(hcAllo.RequiredEnvironments.Select(e => e.ToString()), Is.EquivalentTo(new[] {"/ _ " + ConsFS}));
			Assert.That(hcAllo.RequiredMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"inflClass"}));
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
			XElement dataIssuesElem = XElement.Parse(m_dataIssues);
			Assert.That(dataIssuesElem.Elements("LoadError").Count(e => (string) e.Attribute("type") == "invalid-shape"), Is.EqualTo(1));
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
			Assert.That(hcAllo.RequiredEnvironments, Is.Empty);

			hcAllo = rule.Allomorphs[1];
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {AnyStar + ConsFS, ConsFS + AnyStar}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<left>", "+a+", "<right>"}));
			Assert.That(hcAllo.RequiredEnvironments, Is.Empty);
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
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[] {"<C^1>", "<V^1>", "d", "+", "<prefixNull>", "<C^1>", "<V^1>", "<stem>"}));
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
			Assert.That(hcAllo.RequiredEnvironments.Select(e => e.ToString()), Is.EquivalentTo(new[] {"/ " + LeftAnchorFS + ConsFS + " _ " + VowelFS}));
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
			msa.FromStemNameRA = AddStemName(m_verb, "stemName", new FS());
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
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(1));
			var rule = (AffixProcessRule) m_lang.Strata[0].MorphologicalRules[0];

			Assert.That(rule.RequiredSyntacticFeatureStruct.ToString(), Is.EqualTo("[POS:V]"));
			Assert.That(rule.OutSyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[tense:past]]"));
			Assert.That(rule.Gloss, Is.EqualTo("gloss"));
			Assert.That(rule.Allomorphs[0].RequiredMprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"fromExceptFeat"}));
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
			AddEntry(MoMorphTypeTags.kguidMorphSuffix, "t", "gloss3", new SandboxGenericMSA {MsaType = MsaType.kInfl, MainPOS = m_verb, Slot = suffixSlot});
			LoadLanguage();

			Assert.That(m_lang.Strata[0].MorphologicalRules.Count, Is.EqualTo(0));

			Assert.That(m_lang.Strata[0].AffixTemplates.Count, Is.EqualTo(1));
			var hcTemplate = m_lang.Strata[0].AffixTemplates.First();
			Assert.That(hcTemplate.IsFinal, Is.False);
			Assert.That(hcTemplate.Slots.Count, Is.EqualTo(3));

			AffixTemplateSlot hcSlot = hcTemplate.Slots[0];
			Assert.That(hcSlot.Optional, Is.False);
			Assert.That(hcSlot.Rules.Count, Is.EqualTo(1));
			Assert.That(hcSlot.Rules.First().ToString(), Is.EqualTo("-t"));

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
			var allo = (IMoStemAllomorph) entry.LexemeFormOA;
			allo.PhoneEnvRC.Add(AddEnvironment("/ [V] _ [V]#"));
			allo.PhoneEnvRC.Add(AddEnvironment("/ [C] _ [C]"));
			allo.StemNameRA = AddStemName(m_verb, "stemName", new FS());
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(1));
			LexEntry hcEntry = m_lang.Strata[0].Entries.First();

			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));

			RootAllomorph hcAllo = hcEntry.PrimaryAllomorph;
			Assert.That(hcAllo.Shape.ToString(m_lang.Strata[0].SymbolTable, false), Is.EqualTo("sag"));
			Assert.That(hcAllo.StemName.ToString(), Is.EqualTo("stemName"));
			Assert.That(hcAllo.IsBound, Is.True);
			Assert.That(hcAllo.RequiredEnvironments.Select(e => e.ToEnvString()), Is.EquivalentTo(new[]
				{
					"/ [cons:-, Type:segment, voc:+] _ [cons:-, Type:segment, voc:+][AnchorType:RightSide, Type:anchor]",
					"/ [cons:+, Type:segment, voc:-] _ [cons:+, Type:segment, voc:-]"
				}));
		}

		[Test]
		public void Enclitic()
		{
			AddEntry(MoMorphTypeTags.kguidMorphEnclitic, "ɯd", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem});
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
			Assert.That(hcAllo.Shape.ToString(m_lang.Strata[0].SymbolTable, false), Is.EqualTo("sag"));
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
			Assert.That(hcAllo.Shape.ToString(m_lang.Strata[0].SymbolTable, false), Is.EqualTo("sag"));
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
			Assert.That(hcStemEntry.PrimaryAllomorph.ExcludedAllomorphCoOccurrences.Count, Is.EqualTo(1));
			AllomorphCoOccurrenceRule coOccurRule = hcStemEntry.PrimaryAllomorph.ExcludedAllomorphCoOccurrences.First();
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
			Assert.That(hcStemEntry.ExcludedMorphemeCoOccurrences.Count, Is.EqualTo(1));
			MorphemeCoOccurrenceRule coOccurRule = hcStemEntry.ExcludedMorphemeCoOccurrences.First();
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
			IPhSimpleContextSeg segCtxt = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			prule.StrucDescOS.Add(segCtxt);
			segCtxt.FeatureStructureRA = GetPhoneme("a");
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

			Assert.That(hcPrule.Direction, Is.EqualTo(Direction.LeftToRight));
			Assert.That(hcPrule.ApplicationMode, Is.EqualTo(RewriteApplicationMode.Simultaneous));
			Assert.That(hcPrule.Lhs.ToString(), Is.EqualTo(m_lang.Strata[0].SymbolTable.GetSymbolFeatureStruct("a") + VowelFS));

			Assert.That(hcPrule.Subrules.Count, Is.EqualTo(1));
			RewriteSubrule subrule = hcPrule.Subrules[0];

			Assert.That(subrule.LeftEnvironment.ToString(), Is.EqualTo(LeftAnchorFS + "(" + m_lang.Strata[0].SymbolTable.GetSymbolFeatureStruct("t") + ")"));
			Assert.That(subrule.Rhs.ToString(), Is.EqualTo(string.Format("[cont:+, Type:segment, vd:+{0}]", constr.Hvo)));
			Assert.That(subrule.RightEnvironment.ToString(), Is.EqualTo(string.Format("[poa:alveolar, Type:segment, vd:-{0}]", constr.Hvo)));
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

			Assert.That(hcPrule.Direction, Is.EqualTo(Direction.RightToLeft));
			Assert.That(hcPrule.Pattern.ToString(), Is.EqualTo(string.Format("({0})({1})({2})({3})", VowelFS,
				m_lang.Strata[0].SymbolTable.GetSymbolFeatureStruct("a"), m_lang.Strata[0].SymbolTable.GetSymbolFeatureStruct("t"), ConsFS)));
			Assert.That(hcPrule.GroupOrder, Is.EqualTo(new[] {"0", "2", "1", "3"}));
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
			input1.MemberRA = AddNCContext(m_cons);

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
			Assert.That(hcAllo.Lhs.Select(p => p.ToString()), Is.EqualTo(new[] {ConsFS + "[2,]", "[round:+, Type:segment, voc:+]", AnyStar}));
			Assert.That(hcAllo.Rhs.Select(a => a.ToString()), Is.EqualTo(new[]
				{
					string.Format("<{0}>", input1.Hvo),
					"+a+",
					string.Format("<{0}> -> [round:-, Type:segment, voc:+]", input2.Hvo),
					"[cons:+, cont:-, nasal:+, poa:velar, Type:segment, vd:+, voc:-]"
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
		public void Variant()
		{
			ILexEntry entry = AddEntry(MoMorphTypeTags.kguidMorphStem, "sag", "gloss", new SandboxGenericMSA {MsaType = MsaType.kStem, MainPOS = m_verb});
			ILexEntryInflType type = Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPluralVar);
			type.GlossAppend.SetAnalysisDefaultWritingSystem(".pl");
			type.InflFeatsOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			CreateFeatStruc(Cache.LanguageProject.MsFeatureSystemOA, m_inflType, type.InflFeatsOA, new FS {{"nounAgr", new FS {{"num", "pl"}}}});
			entry.CreateVariantEntryAndBackRef(type, Cache.TsStrFactory.MakeString("sau", Cache.DefaultVernWs));
			LoadLanguage();

			Assert.That(m_lang.Strata[0].Entries.Count, Is.EqualTo(2));
			LexEntry hcEntry = m_lang.Strata[0].Entries.First();
			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));
			Assert.That(hcEntry.PrimaryAllomorph.Shape.ToString(m_lang.Strata[0].SymbolTable, false), Is.EqualTo("sag"));

			hcEntry = m_lang.Strata[0].Entries.Last();
			Assert.That(hcEntry.Gloss, Is.EqualTo("gloss.pl"));
			Assert.That(hcEntry.Allomorphs.Count, Is.EqualTo(1));
			Assert.That(hcEntry.PrimaryAllomorph.Shape.ToString(m_lang.Strata[0].SymbolTable, false), Is.EqualTo("sau"));
			Assert.That(hcEntry.SyntacticFeatureStruct.ToString(), Is.EqualTo("[Head:[nounAgr:[num:pl]], POS:V]"));
			Assert.That(hcEntry.MprFeatures.Select(mf => mf.ToString()), Is.EquivalentTo(new[] {"Plural Variant"}));
		}
	}
}
