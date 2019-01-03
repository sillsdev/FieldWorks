// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using FS = System.Collections.Generic.Dictionary<SIL.LCModel.IFsFeatDefn, object>;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	[TestFixture]
	public class ComplexConcPatternModelTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IPartOfSpeech m_noun;
		private IPartOfSpeech m_verb;
		private IPartOfSpeech m_adj;
		private IText m_text;
		private ICmPossibility m_np;
		private readonly IEqualityComparer<IParaFragment> m_fragmentComparer = new ParaFragmentEqualityComparer();
		private IFsFeatStrucType m_inflType;

		#region Overrides of LcmTestBase

		protected override void CreateTestData()
		{
			m_noun = MakePartOfSpeech("noun");
			m_verb = MakePartOfSpeech("verb");
			m_adj = MakePartOfSpeech("adj");

			var msFeatSys = Cache.LanguageProject.MsFeatureSystemOA;
			m_inflType = AddFSType(msFeatSys, "infl", AddComplexFeature(msFeatSys, "nounAgr", AddClosedFeature(msFeatSys, "num", "sg", "pl")), AddClosedFeature(msFeatSys, "tense", "pres"));
			m_np = Cache.LangProject.GetDefaultTextTagList().ReallyReallyAllPossibilities.Single(poss => poss.Abbreviation.BestAnalysisAlternative.Text == "Noun Phrase");
			var ni = MakeEntry("ni-", m_verb, "1SgSubj");
			var him = MakeEntry("him-", m_verb, "3SgObj");
			var bili = MakeEntry("bili", m_verb, "to see");
			var ra = MakeEntry("-ra", m_verb, "Pres", new FS { { GetFeature("tense"), GetValue("pres") } });
			var pus = MakeEntry("pus", m_adj, "green");
			var yalo = MakeEntry("yalo", m_noun, "mat");
			var la = MakeEntry("-la", m_noun, "1SgPoss", new FS { { GetFeature("nounAgr"), new FS { { GetFeature("num"), GetValue("sg") } } } });
			MakeWordform("nihimbilira", "I see", m_verb, ni, him, bili, ra);
			MakeWordform("pus", "green", m_adj, pus);
			MakeWordform("yalola", "my mat", m_noun, yalo, la);
			MakeWordform("ban", "test", MakePartOfSpeech("pos"));
			m_text = MakeText("nihimbilira pus, yalola ban.");
			var para = (IStTxtPara)m_text.ContentsOA.ParagraphsOS.First();
			MakeTag(m_text, m_np, para.SegmentsOS.First(), 1, para.SegmentsOS.First(), 3);
		}
		#endregion

		private IFsClosedFeature AddClosedFeature(IFsFeatureSystem featSys, string name, params string[] values)
		{
			var feat = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			featSys.FeaturesOC.Add(feat);
			feat.Name.SetAnalysisDefaultWritingSystem(name);
			feat.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			foreach (var value in values)
			{
				var symbol = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
				feat.ValuesOC.Add(symbol);
				symbol.Name.SetAnalysisDefaultWritingSystem(value);
				symbol.Abbreviation.SetAnalysisDefaultWritingSystem(value);
			}
			return feat;
		}

		private IFsFeatStrucType AddFSType(IFsFeatureSystem featSys, string name, params IFsFeatDefn[] features)
		{
			var type = Cache.ServiceLocator.GetInstance<IFsFeatStrucTypeFactory>().Create();
			featSys.TypesOC.Add(type);
			type.Name.SetAnalysisDefaultWritingSystem(name);
			type.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			foreach (var fd in features)
			{
				type.FeaturesRS.Add(fd);
			}
			return type;
		}

		private IFsComplexFeature AddComplexFeature(IFsFeatureSystem featSys, string name, params IFsFeatDefn[] features)
		{
			var type = AddFSType(featSys, name, features);
			var feat = Cache.ServiceLocator.GetInstance<IFsComplexFeatureFactory>().Create();
			featSys.FeaturesOC.Add(feat);
			feat.Name.SetAnalysisDefaultWritingSystem(name);
			feat.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			feat.TypeRA = type;
			return feat;
		}

		private void CreateFeatStruc(IFsFeatStrucType type, IFsFeatStruc fs, FS featVals)
		{
			fs.TypeRA = type;
			foreach (var featVal in featVals)
			{
				var closedFeat = featVal.Key as IFsClosedFeature;
				if (closedFeat != null)
				{
					var sym = (IFsSymFeatVal)featVal.Value;
					var cv = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
					fs.FeatureSpecsOC.Add(cv);
					cv.FeatureRA = closedFeat;
					cv.ValueRA = sym;
				}
				else
				{
					var complexFeat = (IFsComplexFeature)featVal.Key;
					var cv = Cache.ServiceLocator.GetInstance<IFsComplexValueFactory>().Create();
					fs.FeatureSpecsOC.Add(cv);
					var childFS = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
					cv.FeatureRA = complexFeat;
					cv.ValueOA = childFS;
					CreateFeatStruc(complexFeat.TypeRA, childFS, (FS)featVal.Value);
				}
			}
		}

		private IFsFeatDefn GetFeature(string id)
		{
			return Cache.LanguageProject.MsFeatureSystemOA.FeaturesOC.First(f => f.Abbreviation.AnalysisDefaultWritingSystem.Text == id);
		}

		private IFsSymFeatVal GetValue(string id)
		{
			return Cache.LanguageProject.MsFeatureSystemOA.FeaturesOC.OfType<IFsClosedFeature>().SelectMany(f => f.ValuesOC).First(sym => sym.Abbreviation.AnalysisDefaultWritingSystem.Text == id);
		}

		private ITextTag MakeTag(IText text, ICmPossibility tag, ISegment beginSeg, int begin, ISegment endSeg, int end)
		{
			var ttag = Cache.ServiceLocator.GetInstance<ITextTagFactory>().Create();
			text.ContentsOA.TagsOC.Add(ttag);
			ttag.TagRA = tag;
			ttag.BeginSegmentRA = beginSeg;
			ttag.BeginAnalysisIndex = begin;
			ttag.EndSegmentRA = endSeg;
			ttag.EndAnalysisIndex = end;
			return ttag;
		}

		private IText MakeText(string contents)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = TsStringUtils.MakeString(contents, Cache.DefaultVernWs);
			using (var pp = new ParagraphParser(Cache))
			{
				pp.Parse(para);
			}
			var seg = para.SegmentsOS.First();
			for (var i = 0; i < seg.AnalysesRS.Count; i++)
			{
				var analysis = seg.AnalysesRS[i];
				var wordform = analysis as IWfiWordform;
				if (wordform != null)
				{
					seg.AnalysesRS[i] = wordform.AnalysesOC.First().MeaningsOC.First();
				}
			}
			return text;
		}

		private IPartOfSpeech MakePartOfSpeech(string name)
		{
			var partOfSpeech = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(partOfSpeech);
			partOfSpeech.Name.AnalysisDefaultWritingSystem = MakeAnalysisString(name);
			partOfSpeech.Abbreviation.AnalysisDefaultWritingSystem = partOfSpeech.Name.AnalysisDefaultWritingSystem;
			return partOfSpeech;
		}

		private Guid GetSlotType(string form)
		{
			var slotType = MoMorphTypeTags.kguidMorphStem;
			if (form.StartsWith("-"))
			{
				slotType = MoMorphTypeTags.kguidMorphSuffix;
			}
			else if (form.EndsWith("-"))
			{
				slotType = MoMorphTypeTags.kguidMorphPrefix;
			}
			return slotType;
		}

		private ILexEntry MakeEntry(string lf, IPartOfSpeech pos, string gloss, FS inflFS = null)
		{
			// The entry itself.
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			// Lexeme Form and MSA.
			var slotType = GetSlotType(lf);
			IMoForm form;
			var msa = GetMsaAndMoForm(entry, slotType, pos, inflFS, out form);
			entry.LexemeFormOA = form;
			var trimmed = lf.Trim('-');
			form.Form.VernacularDefaultWritingSystem = MakeVernString(trimmed);
			form.Form.AnalysisDefaultWritingSystem = MakeAnalysisString(trimmed);
			form.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(slotType);
			// Bare bones of Sense
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = MakeAnalysisString(gloss);
			sense.MorphoSyntaxAnalysisRA = msa;
			return entry;
		}

		private IMoMorphSynAnalysis GetMsaAndMoForm(ILexEntry entry, Guid slotType, IPartOfSpeech pos, FS inflFS, out IMoForm form)
		{
			var fs = inflFS == null ? null : Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			IMoMorphSynAnalysis msa;
			if (slotType == MoMorphTypeTags.kguidMorphStem)
			{
				form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				var stemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				msa = stemMsa;
				entry.MorphoSyntaxAnalysesOC.Add(msa);
				stemMsa.PartOfSpeechRA = pos;
				if (inflFS != null)
				{
					stemMsa.MsFeaturesOA = fs;
				}
			}
			else
			{
				form = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				var affixMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
				msa = affixMsa;
				entry.MorphoSyntaxAnalysesOC.Add(msa);
				affixMsa.PartOfSpeechRA = pos;
				if (inflFS != null)
				{
					affixMsa.InflFeatsOA = fs;
				}
			}
			if (inflFS != null)
			{
				CreateFeatStruc(m_inflType, fs, inflFS);
			}
			return msa;
		}

		private void MakeWordform(string form, string gloss, IPartOfSpeech pos, params ILexEntry[] morphs)
		{
			var result = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			result.Form.VernacularDefaultWritingSystem = MakeVernString(form);
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			result.AnalysesOC.Add(wa);
			wa.CategoryRA = pos;
			foreach (var morph in morphs)
			{
				var bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
				wa.MorphBundlesOS.Add(bundle);
				bundle.MorphRA = morph.LexemeFormOA;
				bundle.MsaRA = morph.MorphoSyntaxAnalysesOC.First();
				bundle.SenseRA = morph.SensesOS.First();
			}
			var wg = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wg);
			wg.Form.AnalysisDefaultWritingSystem = MakeAnalysisString(gloss);
		}

		private ITsString MakeVernString(string content)
		{
			return TsStringUtils.MakeString(content, Cache.DefaultVernWs);
		}
		private ITsString MakeAnalysisString(string content)
		{
			return TsStringUtils.MakeString(content, Cache.DefaultAnalWs);
		}

		[Test]
		public void SimpleSearch()
		{
			var para = (IStTxtPara)m_text.ContentsOA.ParagraphsOS.First();
			var seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcWordNode { Category = m_verb });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode { InflFeatures = { { GetFeature("nounAgr"), new FS { { GetFeature("num"), new ClosedFeatureValue(GetValue("sg"), false) } } } } });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode { InflFeatures = { { GetFeature("tense"), new ClosedFeatureValue(GetValue("pres"), true) } } });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 12, 15, null), new ParaFragment(seg, 17, 23, null), new ParaFragment(seg, 24, 27, null) }).Using(m_fragmentComparer));
			model.Root.Children.Clear();

			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_noun });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcTagNode { Tag = m_np });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 12, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 12, 15, null), new ParaFragment(seg, 17, 23, null), new ParaFragment(seg, 24, 27, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Form = MakeVernString("him") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Entry = MakeVernString("ra") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Gloss = MakeAnalysisString("1SgPoss") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_adj, NegateCategory = true });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_adj, NegateCategory = true, Form = MakeVernString("him"), Entry = MakeVernString("him"), Gloss = MakeAnalysisString("3SgObj") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_adj, NegateCategory = true, Form = MakeVernString("him"), Entry = MakeVernString("hin"), Gloss = MakeAnalysisString("3SgObj") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode());
			model.Root.Children.Add(new ComplexConcTagNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 23, null) }).Using(m_fragmentComparer));
		}

		[Test]
		public void WordBoundarySearch()
		{
			var para = (IStTxtPara)m_text.ContentsOA.ParagraphsOS.First();
			var seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcMorphNode { Form = MakeVernString("bili") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Form = MakeVernString("bili") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Form = MakeVernString("bili") });
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_noun });
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_noun });
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_noun });
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_adj });
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_noun });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_adj });
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_noun });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 12, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_adj });
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_noun });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Minimum = 0, Maximum = -1 });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] {new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 12, 15, null), new ParaFragment(seg, 17, 23, null)}).Using(m_fragmentComparer));
		}

		[Test]
		public void QuantifierSearch()
		{
			var para = (IStTxtPara)m_text.ContentsOA.ParagraphsOS.First();
			var seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcWordNode { Minimum = 0, Maximum = -1 });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 27, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_noun });
			model.Root.Children.Add(new ComplexConcWordNode { Minimum = 0, Maximum = -1 });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 17, 27, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcMorphNode { Gloss = MakeAnalysisString("1SgSubj") });
			model.Root.Children.Add(new ComplexConcMorphNode { Minimum = 1, Maximum = -1 });
			model.Root.Children.Add(new ComplexConcMorphNode { Form = MakeVernString("ra") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_verb });
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_adj, Minimum = 0, Maximum = 1 });
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_noun });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_verb, Minimum = 1, Maximum = 3 });
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.Empty);

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_verb, Minimum = 1, Maximum = 4 });
			model.Root.Children.Add(new ComplexConcWordBdryNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));
		}

		[Test]
		public void AlternationSearch()
		{
			var para = (IStTxtPara)m_text.ContentsOA.ParagraphsOS.First();
			var seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcWordNode { Category = m_noun });
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_verb });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_noun });
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_verb });
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcGroupNode
			{
				Children =
				{
					new ComplexConcWordNode {Category = m_verb},
					new ComplexConcOrNode(),
					new ComplexConcWordNode {Form = MakeVernString("yalola")}
				}
			});
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Gloss = MakeAnalysisString("green") });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 12, 15, null), new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcTagNode { Tag = m_np });
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcMorphNode { Gloss = MakeAnalysisString("green") });
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_noun });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 12, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_noun });
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcOrNode());
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_verb });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null), new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));
		}

		[Test]
		public void GroupSearch()
		{
			var para = (IStTxtPara)m_text.ContentsOA.ParagraphsOS.First();
			var seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcGroupNode
			{
				Minimum = 1,
				Maximum = 2,
				Children =
				{
					new ComplexConcWordNode {Category = m_verb},
					new ComplexConcOrNode(),
					new ComplexConcWordNode {Category = m_adj}
				}
			});
			model.Root.Children.Add(new ComplexConcWordNode { Category = m_noun });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 23, null) }).Using(m_fragmentComparer));

			model.Root.Children.Clear();
			model.Root.Children.Add(new ComplexConcGroupNode
			{
				Minimum = 1,
				Maximum = -1,
				Children =
				{
					new ComplexConcWordNode {Category = m_verb},
					new ComplexConcGroupNode {Minimum = 0, Maximum = -1, Children =
						{
							new ComplexConcWordNode {Category = m_adj},
							new ComplexConcOrNode(),
							new ComplexConcWordNode {Category = m_noun}
						}},
				}
			});
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 23, null) }).Using(m_fragmentComparer));
		}

		[Test]
		public void ParagraphWithInvalidParse()
		{
			var para = (IStTxtPara)m_text.ContentsOA.ParagraphsOS.First();
			var seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcMorphNode { Category = m_noun });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));

			// cause analyses and baseline to get out-of-sync
			seg.AnalysesRS.RemoveAt(0);
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 17, 23, null) }).Using(m_fragmentComparer));
		}

		[Test]
		public void InvalidTags()
		{
			var para = (IStTxtPara)m_text.ContentsOA.ParagraphsOS.First();
			var seg = para.SegmentsOS.First();

			var model = new ComplexConcPatternModel(Cache);

			model.Root.Children.Add(new ComplexConcWordNode { Category = m_verb });
			model.Compile();
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));

			// create a tag that occurs after the segment
			var ttag = MakeTag(m_text, m_np, seg, 6, seg, 6);
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));

			ttag.Delete();
			// create a tag where the begin index is greater than the end index
			MakeTag(m_text, m_np, seg, 5, seg, 4);
			Assert.That(model.Search(m_text.ContentsOA), Is.EquivalentTo(new IParaFragment[] { new ParaFragment(seg, 0, 11, null) }).Using(m_fragmentComparer));
		}

		private sealed class ParaFragmentEqualityComparer : IEqualityComparer<IParaFragment>
		{
			public bool Equals(IParaFragment x, IParaFragment y)
			{
				return x.Paragraph == y.Paragraph && x.GetMyBeginOffsetInPara() == y.GetMyBeginOffsetInPara() && x.GetMyEndOffsetInPara() == y.GetMyEndOffsetInPara();
			}

			public int GetHashCode(IParaFragment obj)
			{
				var code = 23;
				code = code * 31 + obj.Paragraph.GetHashCode();
				code = code * 31 + obj.GetMyBeginOffsetInPara();
				code = code * 31 + obj.GetMyEndOffsetInPara();
				return code;
			}
		}
	}
}