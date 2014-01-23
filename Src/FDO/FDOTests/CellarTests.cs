// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CellarTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using System.Xml;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests.CellarTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the non-generated parts of the CmAgent class can go here.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CmAgentTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Make sure a null target parameter is not acceptable.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullTargetTest()
		{
			Cache.LanguageProject.DefaultParserAgent.SetEvaluation(null, Opinions.noopinion);
		}

		/// <summary>
		/// Make sure a target that is not a WfiAnalysis or a WfiWordfor is not acceptable.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TargetClassNotGoodTest()
		{
			Cache.LanguageProject.DefaultParserAgent.SetEvaluation(
				Cache.LanguageProject,
				Opinions.noopinion);
		}

		/// <summary>
		/// Reset an extant Evaluation.
		/// </summary>
		[Test]
		public void ResetEvaluation()
		{
			// Set up data for test.
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			var parserAgent = Cache.LanguageProject.DefaultParserAgent;
			wa.SetAgentOpinion(parserAgent, Opinions.disapproves);

			// test
			parserAgent.SetEvaluation(wa, Opinions.approves);
			Assert.AreEqual(Opinions.approves, wa.GetAgentOpinion(parserAgent), "Evaluation not changed.");
		}

		/// <summary>
		/// Make a new Evaluation.
		/// </summary>
		[Test]
		public void MakeEvaluation()
		{
			// Set up data for test.
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			var parserAgent = Cache.LanguageProject.DefaultParserAgent;
			Assert.AreEqual(Opinions.noopinion, wa.GetAgentOpinion(parserAgent), "Wrong initial opinion in new analysis.");

			parserAgent.SetEvaluation(wa, Opinions.approves);
			Assert.AreEqual(Opinions.approves, wa.GetAgentOpinion(parserAgent), "Evaluation not set.");

			parserAgent.SetEvaluation(wa, Opinions.disapproves);
			Cache.ActionHandlerAccessor.BreakUndoTask("undo", "redo"); // force PropChanges
			Assert.AreEqual(Opinions.disapproves, wa.GetAgentOpinion(parserAgent), "Evaluation not set.");
			Assert.AreEqual(1, wa.EvaluationsRC.Count, "added new evaluation but did not remove old one");

			parserAgent.SetEvaluation(wa, Opinions.noopinion);
			Assert.AreEqual(Opinions.noopinion, wa.GetAgentOpinion(parserAgent), "Evaluation not cleared.");
			Assert.AreEqual(0, wa.EvaluationsRC.Count, "somehow got to noopinion but still has an evaluation");

			var humanAgent = Cache.LangProject.DefaultUserAgent;
			var watcherHumanApproved = new PropChangedVerifier(Cache, WfiWordformTags.kClassId, "HumanApprovedAnalyses");
			var watcherHumanNoOp = new PropChangedVerifier(Cache, WfiWordformTags.kClassId, "HumanNoOpinionParses");
			humanAgent.SetEvaluation(wa, Opinions.approves);
			Cache.ActionHandlerAccessor.BreakUndoTask("undo", "redo"); // force PropChanges
			Assert.AreEqual(Opinions.approves, wa.GetAgentOpinion(humanAgent), "Evaluation not set.");
			// Check that we got notifications for the two virtual properties affected.
			// Currently we will get one for the other, too, but that's not something we want to clamp.
			Assert.That(watcherHumanApproved.Hvo, Is.EqualTo(wa.Owner.Hvo));
			Assert.That(watcherHumanNoOp.Hvo, Is.EqualTo(wa.Owner.Hvo));

			var watcherHumanDisApproved = new PropChangedVerifier(Cache, WfiWordformTags.kClassId, "HumanDisapprovedParses");
			watcherHumanApproved.Reset();
			watcherHumanNoOp.Reset();
			humanAgent.SetEvaluation(wa, Opinions.disapproves);
			Cache.ActionHandlerAccessor.BreakUndoTask("undo", "redo"); // force PropChanges
			Assert.AreEqual(Opinions.disapproves, wa.GetAgentOpinion(humanAgent), "Evaluation not set.");
			Assert.AreEqual(1, wa.EvaluationsRC.Count, "added new evaluation but did not remove old one");
			Assert.That(watcherHumanApproved.Hvo, Is.EqualTo(wa.Owner.Hvo));
			Assert.That(watcherHumanDisApproved.Hvo, Is.EqualTo(wa.Owner.Hvo));

			watcherHumanApproved.Reset();
			watcherHumanNoOp.Reset();
			watcherHumanDisApproved.Reset();
			humanAgent.SetEvaluation(wa, Opinions.noopinion);
			Cache.ActionHandlerAccessor.BreakUndoTask("undo", "redo"); // force PropChanges
			Assert.AreEqual(Opinions.noopinion, wa.GetAgentOpinion(humanAgent), "Evaluation not cleared.");
			Assert.AreEqual(0, wa.EvaluationsRC.Count, "somehow got to noopinion but still has an evaluation");
			Assert.That(watcherHumanDisApproved.Hvo, Is.EqualTo(wa.Owner.Hvo));
			Assert.That(watcherHumanNoOp.Hvo, Is.EqualTo(wa.Owner.Hvo));
		}

		/// <summary>
		/// The 'ScaleFactor' property of ICmPicture has a range between 1  and 100, inclusive.
		/// </summary>
		[Test]
		public void SideEffectsForCmPictureTests()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LanguageProject;

			var entry = servLoc.GetInstance<ILexEntryFactory>().Create();
			var sense = servLoc.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			var pic = servLoc.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(pic);

			// Make sure the partial method 'ValidateScaleFactor' does its job.
			pic.ScaleFactor = 200;
			Assert.AreEqual(100, pic.ScaleFactor, "Wrong high-adjusted scale factor (from 200).");
			pic.ScaleFactor = -50;
			Assert.AreEqual(100, pic.ScaleFactor, "Wrong low-adjusted scale factor (from -50).");
			pic.ScaleFactor = 50;
			Assert.AreEqual(50, pic.ScaleFactor, "Wrong un-adjusted scale factor (from 50).");
			pic.ScaleFactor = 0;
			Assert.AreEqual(100, pic.ScaleFactor, "Wrong low-adjusted scale factor (from 0).");
		}
	}

	/// <summary>
	/// Class watches for a particular PropChanged and records it.
	/// </summary>
	class PropChangedVerifier : IVwNotifyChange
	{
		public PropChangedVerifier(FdoCache cache, int clsid, string propName)
		{
			InterestingFlid = cache.MetaDataCache.GetFieldId2(clsid, propName, false);
			cache.DomainDataByFlid.AddNotification(this);
		}
		public int InterestingFlid;
		public int Hvo;
		public int IvMin;
		public int CvIns;
		public int CvDel;
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == InterestingFlid)
			{
				Hvo = hvo;
				IvMin = ivMin;
				CvIns = cvIns;
				CvDel = cvDel;
			}
		}
		/// <summary>
		/// Prepare for another test.
		/// </summary>
		public void Reset()
		{
			Hvo = 0;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the various factory methods on CmTranslationFactory.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CmTranslationFactoryTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ICmTranslationFactory m_factory;
		private ICmPossibility m_backTranslationType;

		/// <summary>
		/// Override to set up this fixture.
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			var servLoc = Cache.ServiceLocator;
			m_factory = servLoc.GetInstance<ICmTranslationFactory>();
			m_backTranslationType = servLoc.GetInstance<ICmPossibilityRepository>().GetObject(
					CmPossibilityTags.kguidTranBackTranslation);
		}

		/// <summary>
		/// Make sure the smart Create method chokes on a null owner.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Ensure_smart_method_throws_on_null_owner_paragraph()
		{
			const IStTxtPara para = null;
			m_factory.Create(para, m_backTranslationType);
		}

		/// <summary>
		/// Make sure the smart Create method chokes on a null owner.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Ensure_smart_method_throws_on_null_owner_example_sentence()
		{
			const ILexExampleSentence sentence = null;
			m_factory.Create(sentence, m_backTranslationType);
		}

		/// <summary>
		/// Make sure the smart Create method chokes on a null translation type.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Ensure_smart_method_throws_on_null_translation_type_paragraph()
		{
			m_factory.Create(
				Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create(),
				null);
		}

		/// <summary>
		/// Make sure the smart Create method chokes on a null translation type.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Ensure_smart_method_throws_on_null_translation_type_example_sentence()
		{
			m_factory.Create(
				Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create(),
				null);
		}

		/// <summary>
		/// Make sure the two new methods work.
		/// </summary>
		[Test]
		public void Ensure_smart_methods_work()
		{
			var servLoc = Cache.ServiceLocator;
			var backTransType = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(
				CmPossibilityTags.kguidTranBackTranslation);
			var lp = Cache.LanguageProject;

			// Test it with IStTxtPara.
			var text = servLoc.GetInstance<ITextFactory>().Create();
			//lp.TextsOC.Add(text);
			var txt = servLoc.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = txt;
			var para = servLoc.GetInstance<IStTxtParaFactory>().Create();
			txt.ParagraphsOS.Add(para);
			var trans = servLoc.GetInstance<ICmTranslationFactory>().Create(para, backTransType);
			Assert.AreSame(trans.Owner, para, "Wrong owner.");
			Assert.AreSame(backTransType, trans.TypeRA, "Wrong trans type.");

			// Test it with ILexExampleSentence.
			var entry = servLoc.GetInstance<ILexEntryFactory>().Create();
			var sense = servLoc.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			var exSentence = servLoc.GetInstance<ILexExampleSentenceFactory>().Create();
			sense.ExamplesOS.Add(exSentence);
			trans = servLoc.GetInstance<ICmTranslationFactory>().Create(exSentence, backTransType);
			Assert.AreSame(trans.Owner, exSentence, "Wrong owner.");
			Assert.AreSame(backTransType, trans.TypeRA, "Wrong trans type.");
		}
	}

	#region CellarTests with real database - DON'T ADD TO THIS!
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests cellar overrides with real database
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MoreCellarTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary/>
		public static readonly string ksFS1 = string.Format("<item id=\"fgender\" guid=\"4D8387EE-8BE1-424E-99A8-A7EC48B93909\" type=\"feature\"><abbrev ws=\"en\">Gen</abbrev><term ws=\"en\">gender</term>" +
	"<def ws=\"en\">Grammatical gender is a noun class system, composed of two or three classes,{0}whose nouns that have human male and female referents tend to be in separate classes.{0}Other nouns that are classified in the same way in the language may not be classed by{0}any correlation with natural sex distinctions.</def>" +
	"<citation>Hartmann and Stork 1972:93</citation><citation>Foley and Van Valin 1984:325</citation><citation>Mish et al. 1990:510</citation>" +
	"<citation>Crystal 1985:133</citation><citation>Dixon, R. 1968:105</citation><citation>Quirk, et al. 1985:314</citation>" +
	"<item id=\"vMasc\" guid=\"04828CA7-CACA-4DCA-8284-FF526E2C2F29\" type=\"value\"><abbrev ws=\"en\">Masc</abbrev><term ws=\"en\">masculine gender</term><def ws=\"en\">Masculine gender is a grammatical gender that - marks nouns having human or animal male referents, and - often marks nouns having referents that do not have distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:730</citation><fs id=\"vMascFS\" typeguid=\"25003F1F-690C-4169-8046-7126AC1BBE27\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Masc\" /></f></fs></item><item id=\"vFem\" guid=\"B18097DD-C55B-4517-A778-52BE2141D573\" type=\"value\"><abbrev ws=\"en\">Fem</abbrev><term ws=\"en\">feminine gender</term><def ws=\"en\">Feminine gender is a grammatical gender that - marks nouns that have human or animal female referents, and - often marks nouns that have referents that do not carry distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:456</citation><fs id=\"vFemFS\" typeguid=\"25003F1F-690C-4169-8046-7126AC1BBE27\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Fem\" /></f></fs></item><item id=\"vNeut\" guid=\"A3A920DF-6332-4174-A3EF-C5F90F27E6C5\" type=\"value\"><abbrev ws=\"en\">Neut</abbrev><term ws=\"en\">neuter gender</term><def ws=\"en\">Neuter gender is a grammatical gender that - includes those nouns having referents which do not have distinctions of sex, and - often includes some which do have a natural sex distinction.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:795</citation><fs id=\"vNeutFS\" typeguid=\"25003F1F-690C-4169-8046-7126AC1BBE27\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Neut\" /></f></fs></item><item id=\"vUnknownfgender\" guid=\"C163CD0B-F629-4887-A91C-1B412C6572D8\" type=\"value\"><abbrev ws=\"en\">?</abbrev><term ws=\"en\">unknown gender</term><fs id=\"vUnknownfgenderFS\" typeguid=\"25003F1F-690C-4169-8046-7126AC1BBE27\" type=\"Agr\"><f name=\"Gen\"><sym value=\"?\" /></f></fs></item></item>",
			Environment.NewLine);

		/// <summary>
		/// Ensure that we have a PartOfSpeech object to use in the tests.
		/// </summary>
		protected override void CreateTestData()
		{
			base.CreateTestData();

			if (Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Count == 0)
			{
				IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			}
		}

		/// <summary>
		/// Verifies that this property is sorted.
		/// </summary>
		[Test]
		public void CmSemanticDomainReferringSensesAreSorted()
		{
			ICmSemanticDomainFactory factSemDom = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			var semDom = factSemDom.Create() as CmSemanticDomain;
			Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(semDom);
			ILexEntry lezzz = MakeLexEntry("zzz", "zzzDefn", semDom);
			ILexEntry lerrr = MakeLexEntry("rrr", "rrrDefn", semDom);
			ILexEntry leabc = MakeLexEntry("abc", "abcDefn", semDom);
			ILexEntry leaa = MakeLexEntry("aa", "aaDefn", semDom);
			ILexEntry lemnop = MakeLexEntry("mnop", "gloss", semDom);
			ILexEntry lemnop2 = MakeLexEntry("mnop", "agloss", semDom);

			var list = semDom.ReferringSenses.ToArray();
			Assert.That(list[0], Is.EqualTo(leaa.SensesOS[0]));
			Assert.That(list[1], Is.EqualTo(leabc.SensesOS[0]));
			Assert.That(list[2], Is.EqualTo(lemnop.SensesOS[0]));
			Assert.That(list[3], Is.EqualTo(lemnop2.SensesOS[0]));
			Assert.That(list[4], Is.EqualTo(lerrr.SensesOS[0]));
			Assert.That(list[5], Is.EqualTo(lezzz.SensesOS[0]));
		}

		private ILexEntry MakeLexEntry(string cf, string defn, ICmSemanticDomain domain)
		{
			var servLoc = Cache.ServiceLocator;
			var le = servLoc.GetInstance<ILexEntryFactory>().Create();

			var ws = Cache.DefaultVernWs;
			le.CitationForm.set_String(ws, Cache.TsStrFactory.MakeString(cf, ws));
			var ls = servLoc.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(ls);
			ws = Cache.DefaultAnalWs;
			ls.Definition.set_String(ws, Cache.TsStrFactory.MakeString(defn, ws));
			ls.SemanticDomainsRC.Add(domain);
			var msa = servLoc.GetInstance<IMoStemMsaFactory>().Create();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			ls.MorphoSyntaxAnalysisRA = msa;
			return le;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding closed features to feature system and to a feature structure
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddClosedFeaturesToFeatureSystemAndThenToAFeatureStructure()
		{
			ILangProject lp = Cache.LangProject;

			// Set up the xml fs description
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(ksFS1);
			XmlNode itemNeut = doc.SelectSingleNode("/item/item[3]");

			// Add the feature for first time
			IFsFeatureSystem msfs = lp.MsFeatureSystemOA;
			msfs.AddFeatureFromXml(itemNeut);
			Assert.AreEqual(1, msfs.TypesOC.Count, "should have one type");
			Assert.AreEqual(1, msfs.FeaturesOC.Count, "should have one feature");
			foreach (IFsFeatStrucType type in msfs.TypesOC)
			{
				Assert.AreEqual("Agr", type.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have Agr type");
				Assert.AreEqual(1, type.FeaturesRS.Count, "Expect to have one feature in the type");
				IFsClosedFeature closed = type.FeaturesRS[0] as IFsClosedFeature;
				Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem.Text, "Expect name of gender");
			}
			foreach (IFsClosedFeature closed in msfs.FeaturesOC)
			{
				Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem.Text, "Expect to have gender feature");
				foreach (IFsSymFeatVal value in closed.ValuesOC)
				{
					Assert.AreEqual("neuter gender", value.Name.AnalysisDefaultWritingSystem.Text, "Expect neuter value");
				}
			}
			// Now add a feature that differs only in value
			XmlNode itemFem = doc.SelectSingleNode("/item/item[2]");
			msfs.AddFeatureFromXml(itemFem);
			Assert.AreEqual(1, msfs.TypesOC.Count, "should have one type");
			Assert.AreEqual(1, msfs.FeaturesOC.Count, "should have one feature");
			foreach (IFsFeatStrucType type in msfs.TypesOC)
			{
				Assert.AreEqual("Agr", type.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have Agr type");
			}
			foreach (IFsClosedFeature closed in msfs.FeaturesOC)
			{
				Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem.Text, "Expect to have gender feature");
				Assert.AreEqual(2, closed.ValuesOC.Count, "should have two values");
				foreach (IFsSymFeatVal cv in closed.ValuesOC)
				{
					if (cv.Name.AnalysisDefaultWritingSystem.Text != "neuter gender" &&
						cv.Name.AnalysisDefaultWritingSystem.Text != "feminine gender")
						Assert.Fail("Unexpected value found: {0}", cv.Name.AnalysisDefaultWritingSystem.Text);
				}
				var sortedValues = closed.ValuesSorted;
				Assert.AreEqual(2,sortedValues.Count());
				Assert.AreEqual("feminine gender", sortedValues.First().Name.AnalysisDefaultWritingSystem.Text);
				Assert.AreEqual("neuter gender", sortedValues.Last().Name.AnalysisDefaultWritingSystem.Text);
			}

			// now add to feature structure
			IPartOfSpeech pos = lp.PartsOfSpeechOA.PossibilitiesOS[0] as IPartOfSpeech;
			pos.DefaultFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			IFsFeatStruc featStruct = pos.DefaultFeaturesOA;

			// Add the first feature
			featStruct.AddFeatureFromXml(itemNeut, msfs);
			Assert.AreEqual("Agr", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect type Agr");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsClosedValue cv in featStruct.FeatureSpecsOC)
			{
				Assert.AreEqual("Gen", cv.FeatureRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have Gen feature name");
				Assert.AreEqual("Neut", cv.ValueRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have Neut feature value");
			}
			// Keep a copy of this feature structure for testing merge method later
			pos.InherFeatValOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			IFsFeatStruc featStrucGenNeut = pos.InherFeatValOA;
			featStrucGenNeut.AddFeatureFromXml(itemNeut, msfs);
			// Now add a feature that differs only in value; it should override the old one
			featStruct.AddFeatureFromXml(itemFem, msfs);
			Assert.AreEqual("Agr", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect type Agr");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsClosedValue cv in featStruct.FeatureSpecsOC)
			{
				if (cv.FeatureRA.Name.AnalysisDefaultWritingSystem.Text != "gender" ||
					  cv.ValueRA.Name.AnalysisDefaultWritingSystem.Text != "feminine gender")
					Assert.Fail("Unexpected value found: {0}:{1}", cv.FeatureRA.Name.AnalysisDefaultWritingSystem.Text,
							cv.ValueRA.Name.AnalysisDefaultWritingSystem.Text);
			}
			// Update inflectable features on pos
			pos.AddInflectableFeatsFromXml(itemNeut);
			Assert.AreEqual(1, pos.InflectableFeatsRC.Count, "should have 1 inflectable feature in pos");
			foreach (IFsClosedFeature closed in pos.InflectableFeatsRC)
			{
				Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem.Text, "expect to find gender in pos inflectable features");
			}
			// Check for correct ShortName string in closed
			Assert.AreEqual("Fem", featStruct.ShortName, "Incorrect ShortName for closed");
			// Check for correct LongName string in complex
			Assert.AreEqual("[Gen:Fem]", featStruct.LongName, "Incorrect LongName for closed");
			// Now merge in a feature structure
			featStruct.PriorityUnion(featStrucGenNeut);
			Assert.AreEqual("Agr", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect type Agr");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsClosedValue cv in featStruct.FeatureSpecsOC)
			{
				Assert.AreEqual("Gen", cv.FeatureRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have Gen feature name");
				Assert.AreEqual("Neut", cv.ValueRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have Neut feature value");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding complex features to feature system and to a feature structure
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddComplexFeaturesToFeatureSystemAndThenToAFeatureStructure()
		{
			ILangProject lp = Cache.LangProject;
			Assert.IsNotNull(lp.MsFeatureSystemOA, "Expect a feature system to be present");

			// Set up the xml fs description
			XmlDocument doc = new XmlDocument();
			string sFileDir = Path.Combine(DirectoryFinder.FwSourceDirectory, @"FDO/FDOTests/TestData");
			string sFile = Path.Combine(sFileDir, "FeatureSystem2.xml");

			doc.Load(sFile);
			XmlNode itemNeut = doc.SelectSingleNode("//item[@id='vNeut']");

			// Add the feature for first time
			IFsFeatureSystem msfs = lp.MsFeatureSystemOA;
			msfs.AddFeatureFromXml(itemNeut);
			Assert.AreEqual(1, msfs.TypesOC.Count, "should have two types");
			Assert.AreEqual(2, msfs.FeaturesOC.Count, "should have two features");
			foreach (IFsFeatStrucType type in msfs.TypesOC)
			{
				string sName = type.Name.AnalysisDefaultWritingSystem.Text;
				if (sName != "Subject agreement")
					Assert.Fail("Unexpected fs type found: {0}", sName);
				Assert.AreEqual(1, type.FeaturesRS.Count, "Expect to have one feature in the type");
				IFsFeatDefn defn = type.FeaturesRS[0];
				Assert.IsNotNull(defn, "first feature in type {0} is not null", sName);
				IFsComplexFeature complex = defn as IFsComplexFeature;
				if (complex != null)
					Assert.AreEqual("subject agreement", complex.Name.AnalysisDefaultWritingSystem.Text, "Expect name of subject agreement");
				IFsClosedFeature closed = defn as IFsClosedFeature;
				if (closed != null)
				{
					Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem.Text, "Expect to have gender feature");
					foreach (IFsSymFeatVal value in closed.ValuesOC)
					{
						Assert.AreEqual("neuter gender", value.Name.AnalysisDefaultWritingSystem.Text, "Expect neuter value");
					}
				}
			}
			foreach (IFsFeatDefn defn in msfs.FeaturesOC)
			{
				IFsComplexFeature complex = defn as IFsComplexFeature;
				if (complex != null)
				{
					Assert.AreEqual("subject agreement", complex.Name.AnalysisDefaultWritingSystem.Text, "Expect to have subject agreement feature");
				}
				IFsClosedFeature closed = defn as IFsClosedFeature;
				if (closed != null)
				{
					Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem.Text, "Expect to have gender feature");
					foreach (IFsSymFeatVal value in closed.ValuesOC)
					{
						Assert.AreEqual("neuter gender", value.Name.AnalysisDefaultWritingSystem.Text, "Expect neuter value");
					}
				}
			}
			// Now add a feature that differs only in value
			XmlNode itemFem = doc.SelectSingleNode("//item[@id='vFem']");
			msfs.AddFeatureFromXml(itemFem);
			Assert.AreEqual(1, msfs.TypesOC.Count, "should have two types");
			Assert.AreEqual(2, msfs.FeaturesOC.Count, "should have two features");
			foreach (IFsFeatStrucType type in msfs.TypesOC)
			{
				string sName = type.Name.AnalysisDefaultWritingSystem.Text;
				if (sName != "Subject agreement")
					Assert.Fail("Unexpected fs type found: {0}", sName);
			}
			foreach (IFsFeatDefn defn in msfs.FeaturesOC)
			{
				IFsComplexFeature complex = defn as IFsComplexFeature;
				if (complex != null)
				{
					Assert.AreEqual("subject agreement", complex.Name.AnalysisDefaultWritingSystem.Text, "Expect to have subject agreement feature");
				}
				IFsClosedFeature closed = defn as IFsClosedFeature;
				if (closed != null)
				{
					Assert.AreEqual("gender", closed.Name.AnalysisDefaultWritingSystem.Text, "Expect to have gender feature");
					Assert.AreEqual(2, closed.ValuesOC.Count, "should have two values");
					foreach (IFsSymFeatVal cv in closed.ValuesOC)
					{
						if (cv.Name.AnalysisDefaultWritingSystem.Text != "neuter gender" &&
							cv.Name.AnalysisDefaultWritingSystem.Text != "feminine gender")
							Assert.Fail("Unexpected value found: {0}", cv.Name.AnalysisDefaultWritingSystem.Text);
					}
				}
			}

			// now add to feature structure
			IPartOfSpeech pos = lp.PartsOfSpeechOA.PossibilitiesOS[0] as IPartOfSpeech;
			Assert.IsNotNull(pos, "Need one non-null pos");

			pos.DefaultFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			IFsFeatStruc featStruct = pos.DefaultFeaturesOA;

			// Add the first feature
			featStruct.AddFeatureFromXml(itemNeut, msfs);
			Assert.AreEqual("sbj", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect type sbj");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsFeatureSpecification fspec in featStruct.FeatureSpecsOC)
			{
				IFsComplexValue complex = fspec as IFsComplexValue;
				Assert.IsNotNull(complex, "Should have non-null complex feature value");
				IFsFeatStruc nestedFs = (IFsFeatStruc)complex.ValueOA;
				Assert.IsNotNull(nestedFs, "Should have non-null nested fs");
				foreach (IFsClosedValue cv in nestedFs.FeatureSpecsOC)
				{
					Assert.AreEqual("gen", cv.FeatureRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have gen feature name");
					Assert.AreEqual("n", cv.ValueRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have 'n' feature value");
				}
			}
			// Now add a feature that differs only in value; it should override the old one
			featStruct.AddFeatureFromXml(itemFem, msfs);
			Assert.AreEqual("sbj", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect type sbj");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec");
			foreach (IFsFeatureSpecification fspec in featStruct.FeatureSpecsOC)
			{
				IFsComplexValue complex = fspec as IFsComplexValue;
				Assert.IsNotNull(complex, "Should have non-null complex feature value");
				IFsFeatStruc nestedFs = (IFsFeatStruc)complex.ValueOA;
				Assert.IsNotNull(nestedFs, "Should have non-null nested fs");
				foreach (IFsClosedValue cv in nestedFs.FeatureSpecsOC)
				{
					if (cv.FeatureRA.Name.AnalysisDefaultWritingSystem.Text != "gender" &&
						cv.ValueRA.Name.AnalysisDefaultWritingSystem.Text != "feminine gender")
						Assert.Fail("Unexpected value found: {0}:{1}", cv.FeatureRA.Name.AnalysisDefaultWritingSystem.Text,
							cv.ValueRA.Name.AnalysisDefaultWritingSystem.Text);
				}
			}
			// Now add another feature
			XmlNode item1st = doc.SelectSingleNode("//item[@id='v1']");
			featStruct.AddFeatureFromXml(item1st, msfs);
			Assert.AreEqual("sbj", featStruct.TypeRA.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect type sbj");
			Assert.AreEqual(1, featStruct.FeatureSpecsOC.Count, "should have one feature spec at top feature structure");
			foreach (IFsFeatureSpecification fspec in featStruct.FeatureSpecsOC)
			{
				IFsComplexValue complex = fspec as IFsComplexValue;
				Assert.IsNotNull(complex, "Should have non-null complex feature value");
				IFsFeatStruc nestedFs = (IFsFeatStruc)complex.ValueOA;
				Assert.IsNotNull(nestedFs, "Should have non-null nested fs");
				Assert.AreEqual(2, nestedFs.FeatureSpecsOC.Count, "should have two feature specs in nested feature structure");
				foreach (IFsClosedValue cv in nestedFs.FeatureSpecsOC)
				{
					if (!(((cv.FeatureRA.Name.AnalysisDefaultWritingSystem.Text == "gender") &&
							(cv.ValueRA.Name.AnalysisDefaultWritingSystem.Text == "feminine gender")) ||
						  ((cv.FeatureRA.Name.AnalysisDefaultWritingSystem.Text == "person") &&
							(cv.ValueRA.Name.AnalysisDefaultWritingSystem.Text == "first person"))))
						Assert.Fail("Unexpected value found: {0}:{1}", cv.FeatureRA.Name.AnalysisDefaultWritingSystem.Text,
							cv.ValueRA.Name.AnalysisDefaultWritingSystem.Text);
				}
			}
			// Update inflectable features on pos
			pos.AddInflectableFeatsFromXml(itemNeut);
			Assert.AreEqual(1, pos.InflectableFeatsRC.Count, "should have 1 inflectable feature in pos");
			foreach (IFsFeatDefn defn in pos.InflectableFeatsRC)
			{
				IFsComplexFeature complex = defn as IFsComplexFeature;
				if (complex != null)
					Assert.AreEqual("subject agreement", complex.Name.AnalysisDefaultWritingSystem.Text, "expect to find subject agreement in pos inflectable features");
			}
			// Check for correct ShortName string in complex
			Assert.AreEqual("f 1", featStruct.ShortName, "Incorrect ShortName for complex");
			// Check for correct LongName string in complex
			Assert.AreEqual("[sbj:[gen:f pers:1]]", featStruct.LongName, "Incorrect LongName for complex");
			// Now add a closed feature not at the same level
			sFile = Path.Combine(sFileDir, "FeatureSystem3.xml");
			doc = null;
			doc = new XmlDocument();
			doc.Load(sFile);
			XmlNode itemAorist = doc.SelectSingleNode("//item[@id='xAor']");
			msfs.AddFeatureFromXml(itemAorist);
			pos.AddInflectableFeatsFromXml(itemAorist);
			featStruct.AddFeatureFromXml(itemAorist, msfs);
			// Check for correct LongName
			Assert.AreEqual("[sbj:[gen:f pers:1] asp:aor]", featStruct.LongName, "Incorrect LongName for complex and closed");
			// Now add the features in the featurs struct in a different order
			pos.DefaultFeaturesOA = null;
			pos.DefaultFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			featStruct = pos.DefaultFeaturesOA;
			featStruct.AddFeatureFromXml(itemAorist, msfs);
			featStruct.AddFeatureFromXml(item1st, msfs);
			featStruct.AddFeatureFromXml(itemFem, msfs);
			// check for correct short name
			Assert.AreEqual("aor 1 f", featStruct.ShortName, "Incorrect ShortName for complex");
			// Check for correct LongName
			Assert.AreEqual("[asp:aor sbj:[pers:1 gen:f]]", featStruct.LongName, "Incorrect LongName for complex and closed");
			// Now create another feature structure with different values and merge it into the first feature structure
			pos.InherFeatValOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			IFsFeatStruc featStruct2 = pos.InherFeatValOA;
			featStruct2.AddFeatureFromXml(itemNeut, msfs);
			sFile = Path.Combine(sFileDir, "FeatureSystem2.xml");
			doc = null;
			doc = new XmlDocument();
			doc.Load(sFile);
			XmlNode itemSg = doc.SelectSingleNode("//item[@id='vSg']");
			msfs.AddFeatureFromXml(itemSg);
			featStruct2.AddFeatureFromXml(itemSg, msfs);
			featStruct.PriorityUnion(featStruct2);
			// Check for correct LongName
			Assert.AreEqual("[asp:aor sbj:[pers:1 gen:n num:sg]]", featStruct.LongName, "Incorrect LongName for merged feature struture");
			Assert.AreEqual("[asp:aor sbj:[gen:n num:sg pers:1]]", featStruct.LongNameSorted, "Incorrect LongNameSorted for merged feature struture");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding closed features to feature system and to a feature structure
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FeatureStructBaseAnnotation()
		{
			ILangProject lp = Cache.LangProject;

			var natClass = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(natClass);
			// initial invocation should create a new annotation
			var anno = CmBaseAnnotation.GetOrCreateFeatureStructBaseAnnotation(Cache, natClass);
			Assert.NotNull(anno, "Expect annotation to be found or created; should not be null");
			Assert.NotNull(anno.FeaturesOA, "Expect annotation to have a feature structure");
			Assert.AreEqual(natClass, anno.BeginObjectRA, "Expect the annotation object to be the natural class");

			// second invocation should find first annotation
			var anno2 = CmBaseAnnotation.GetOrCreateFeatureStructBaseAnnotation(Cache, natClass);
			Assert.AreEqual(anno, anno2, "Expect second invocation to find the first annotation");
		}

		/// <summary>
		/// Tests of CmAgent.SetAgentOpinion.
		/// Note that this does not verify doing and undoing.
		/// It may be superceded by the tests in CmAgentTests.
		/// </summary>
		[Test]
		public void SetAgentOpinion()
		{
			ICmAgent agent = Cache.LangProject.DefaultComputerAgent;
			IWfiWordform wf = WfiWordformServices.FindOrCreateWordform(Cache,
				TsStringUtils.MakeTss("xxxyyyzzz12234", Cache.DefaultVernWs));
			IWfiAnalysis wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			Assert.AreEqual(Opinions.noopinion, wa.GetAgentOpinion(agent));

			wa.SetAgentOpinion(agent, Opinions.approves);
			Assert.AreEqual(Opinions.approves, wa.GetAgentOpinion(agent));

			wa.SetAgentOpinion(agent, Opinions.disapproves);
			Assert.AreEqual(Opinions.disapproves, wa.GetAgentOpinion(agent));

			wa.SetAgentOpinion(agent, Opinions.noopinion);
			Assert.AreEqual(Opinions.noopinion, wa.GetAgentOpinion(agent));
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the non-generated parts of the CmAgent class can go here.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PhonologyTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		///
		/// </summary>
		public static readonly string ksPhFS1 =
			string.Format("<item id=\"gPAMajorClassFeature\" posid=\"Adjective\" guid=\"f673a43d-ba35-44f1-a4d0-308a292c4b97\" status=\"visible\" type=\"group\"><abbrev ws=\"en\">mcf</abbrev><term ws=\"en\">major class features</term><def ws=\"en\">“The features that represent the major classes of sounds.”</def><citation>[http://en.wikipedia.org/wiki/Distinctive_feature] Date accessed: 12-Feb-2009</citation>" +
				"<item id=\"fPAConsonantal\" guid=\"b4ddf8e5-1ff8-43fc-9723-04f1ee0471fc\" type=\"feature\"><abbrev ws=\"en\">cons</abbrev><term ws=\"en\">consonantal</term><def ws=\"en\">“Consonantal segments are produced with an audible constriction in the vocal tract, like plosives, affricates, fricatives, nasals, laterals and [r]. Vowels, glides and laryngeal segments are not consonantal.”</def><citation>[http://en.wikipedia.org/wiki/Distinctive_feature] Date accessed: 12-Feb-2009</citation>" +
				"<item id='vPAConsonantalPositive' guid=\"ec5800b4-52a8-4859-a976-f3005c53bd5f\" type='value'><abbrev ws='en'>+</abbrev><term ws='en'>positive</term><fs id='vPAConsonantalPositiveFS' type='Phon' typeguid=\"0ea53dd6-79f5-4fac-a672-f2f7026d8d15\"><f name='fPAConsonantal'><sym value='+'/></f></fs></item>" +
				"<item id='vPAConsonantalNegative' guid=\"81c50b82-83ff-4f73-8e27-6ff9217b810a\" type='value'><abbrev ws='en'>-</abbrev><term ws='en'>negative</term><fs id='vPAConsonantalNegativeFS' type='Phon' typeguid=\"0ea53dd6-79f5-4fac-a672-f2f7026d8d15\"><f name='fPAConsonantal'><sym value='-'/></f></fs></item></item>" +
				"<item id=\"fPASonorant\" guid=\"7df7b583-dd42-424d-9730-ab7bcda314e7\" type=\"feature\"><abbrev ws=\"en\">son</abbrev><term ws=\"en\">sonorant</term><def ws=\"en\">“This feature describes the type of oral constriction that can occur in the vocal tract. [+son] designates the vowels and sonorant consonants, which are produced without the imbalance of air pressure in the vocal tract that might cause turbulence. [-son] alternatively describes the obstruents, articulated with a noticeable turbulence caused by an imbalance of air pressure in the vocal tract.”</def><citation>[http://en.wikipedia.org/wiki/Distinctive_feature] Date accessed: 12-Feb-2009</citation>" +
				"<item id='vPASonorantPositive' guid=\"d190d8a1-f058-4a9c-b16e-f16b525b041c\" type='value'><abbrev ws='en'>+</abbrev><term ws='en'>positive</term><fs id='vPASonorantPositiveFS' type='Phon' typeguid=\"0ea53dd6-79f5-4fac-a672-f2f7026d8d15\"><f name='fPASonorant'><sym value='+'/></f></fs></item>" +
				"<item id='vPASonorantNegative' guid=\"ff4a2434-54e9-4e3d-bf11-cadfedef1765\" type='value'><abbrev ws='en'>-</abbrev><term ws='en'>negative</term><fs id='vPASonorantNegativeFS' type='Phon' typeguid=\"0ea53dd6-79f5-4fac-a672-f2f7026d8d15\"><f name='fPASonorant'><sym value='-'/></f></fs></item></item>" +
				"<item id=\"fPASyllabic\" guid=\"0acbdb9b-28bc-41c2-9706-5873bb3b12e5\" type=\"feature\"><abbrev ws=\"en\">syl</abbrev><term ws=\"en\">syllabic</term><def ws=\"en\">“Syllabic segments may function as the nucleus of a syllable, while their counterparts, the [-syl] segments, may not.”</def><citation>[http://en.wikipedia.org/wiki/Distinctive_feature] Date accessed: 12-Feb-2009</citation>" +
				"<item id='vPASyllabicPositive' guid=\"31929bd3-e2f8-4ea7-beed-527404d34e74\" type='value'><abbrev ws='en'>+</abbrev><term ws='en'>positive</term><fs id='vPASyllabicPositiveFS' type='Phon' typeguid=\"0ea53dd6-79f5-4fac-a672-f2f7026d8d15\"><f name='fPASyllabic'><sym value='+'/></f></fs></item>" +
				"<item id='vPASyllabicNegative' guid=\"73a064b8-21f0-479a-b5d2-142f30297ffa\" type='value'><abbrev ws='en'>-</abbrev><term ws='en'>negative</term><fs id='vPASyllabicNegativeFS' type='Phon' typeguid=\"0ea53dd6-79f5-4fac-a672-f2f7026d8d15\"><f name='fPASyllabic'><sym value='-'/></f></fs></item></item></item>",
				Environment.NewLine);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding closed features to feature system and to a feature structure
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PhonologicalFeatures()
		{
			ILangProject lp = Cache.LangProject;

			// ==================================
			// set up phonological feature system
			// ==================================
			// Set up the xml fs description
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(ksPhFS1);
			// get [consonantal:positive]
			XmlNode itemValue = doc.SelectSingleNode("/item/item[1]/item[1]");

			// Add the feature for first time
			IFsFeatureSystem phfs = lp.PhFeatureSystemOA;
			phfs.AddFeatureFromXml(itemValue);
			// get [consonantal:negative]
			itemValue = doc.SelectSingleNode("/item/item[1]/item[2]");
			phfs.AddFeatureFromXml(itemValue);
			Assert.AreEqual(1, phfs.TypesOC.Count, "should have one type");
			Assert.AreEqual(1, phfs.FeaturesOC.Count, "should have one feature");
			foreach (IFsFeatStrucType type in phfs.TypesOC)
			{
				Assert.AreEqual("Phon", type.Abbreviation.AnalysisDefaultWritingSystem.Text, "Expect to have Phon type");
				Assert.AreEqual(1, type.FeaturesRS.Count, "Expect to have one feature in the type");
				IFsClosedFeature closed = type.FeaturesRS[0] as IFsClosedFeature;
				Assert.AreEqual("consonantal", closed.Name.AnalysisDefaultWritingSystem.Text, "Expect name of consonantal");
			}
			foreach (IFsClosedFeature closed in phfs.FeaturesOC)
			{
				Assert.AreEqual("consonantal", closed.Name.AnalysisDefaultWritingSystem.Text, "Expect to have consonantal feature");
				Assert.AreEqual(2, closed.ValuesOC.Count, "Expect consonantal to have two values");
				var value = closed.ValuesOC.First();
				Assert.AreEqual("positive", value.Name.AnalysisDefaultWritingSystem.Text, "Expect positive first value");
				value = closed.ValuesOC.Last();
				Assert.AreEqual("negative", value.Name.AnalysisDefaultWritingSystem.Text, "Expect negative last value");
			}
			// add sonorant feature
			itemValue = doc.SelectSingleNode("/item/item[2]/item[1]");
			phfs.AddFeatureFromXml(itemValue);
			itemValue = doc.SelectSingleNode("/item/item[2]/item[2]");
			phfs.AddFeatureFromXml(itemValue);
			// add syllabic feature
			itemValue = doc.SelectSingleNode("/item/item[3]/item[1]");
			phfs.AddFeatureFromXml(itemValue);
			itemValue = doc.SelectSingleNode("/item/item[3]/item[2]");
			phfs.AddFeatureFromXml(itemValue);
			Assert.AreEqual(3, phfs.FeaturesOC.Count, "should have three features");
			IFsClosedFeature closedf = phfs.FeaturesOC.First() as IFsClosedFeature;
			CheckFeatureAndItsValues("consonantal", closedf);
			closedf = phfs.FeaturesOC.ElementAt(1) as IFsClosedFeature;
			CheckFeatureAndItsValues("sonorant", closedf);
			closedf = phfs.FeaturesOC.Last() as IFsClosedFeature;
			CheckFeatureAndItsValues("syllabic", closedf);

			// ===============
			// set up phonemes
			// ===============
			var phonData = lp.PhonologicalDataOA;

			var phonemeset = Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create();
			phonData.PhonemeSetsOS.Add(phonemeset);
			var phonemeM = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			phonemeset.PhonemesOC.Add(phonemeM);
			phonemeM.Name.set_String(Cache.DefaultUserWs, "m");
			phonemeM.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			var fsM = phonemeM.FeaturesOA;
			var closedValue = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			fsM.FeatureSpecsOC.Add(closedValue);
			var feat = phfs.FeaturesOC.First() as IFsClosedFeature;
			closedValue.FeatureRA = feat;
			closedValue.ValueRA = feat.ValuesOC.First();
			closedValue = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			fsM.FeatureSpecsOC.Add(closedValue);
			feat = phfs.FeaturesOC.ElementAt(1) as IFsClosedFeature;
			closedValue.FeatureRA = feat;
			closedValue.ValueRA = feat.ValuesOC.First();
			Assert.AreEqual("[cons:+ son:+]", fsM.LongName, "Expect phoneme m to have [cons:+ son:+] features");
			var phonemeP = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			phonemeset.PhonemesOC.Add(phonemeP);
			phonemeP.Name.set_String(Cache.DefaultUserWs, "p");
			phonemeP.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			var fsP = phonemeP.FeaturesOA;
			closedValue = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			fsP.FeatureSpecsOC.Add(closedValue);
			feat = phfs.FeaturesOC.First() as IFsClosedFeature;
			closedValue.FeatureRA = feat;
			closedValue.ValueRA = feat.ValuesOC.First();
			closedValue = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			fsP.FeatureSpecsOC.Add(closedValue);
			feat = phfs.FeaturesOC.ElementAt(1) as IFsClosedFeature;
			closedValue.FeatureRA = feat;
			closedValue.ValueRA = feat.ValuesOC.Last();
			Assert.AreEqual("[cons:+ son:-]", fsP.LongName, "Expect phoneme p to have [cons:+ son:-] features");

			var phonemeB = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			phonemeset.PhonemesOC.Add(phonemeB);
			phonemeB.Name.set_String(Cache.DefaultUserWs, "b");
			phonemeB.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			var fsB = phonemeB.FeaturesOA;
			closedValue = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			fsB.FeatureSpecsOC.Add(closedValue);
			feat = phfs.FeaturesOC.First() as IFsClosedFeature;
			closedValue.FeatureRA = feat;
			closedValue.ValueRA = feat.ValuesOC.First();
			closedValue = Cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
			fsB.FeatureSpecsOC.Add(closedValue);
			feat = phfs.FeaturesOC.ElementAt(1) as IFsClosedFeature;
			closedValue.FeatureRA = feat;
			closedValue.ValueRA = feat.ValuesOC.Last();
			Assert.AreEqual("[cons:+ son:-]", fsB.LongName, "Expect phoneme b to have [cons:+ son:-] features");

			// ====================
			// set up natural class
			// ====================
			var natClass = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
			phonData.NaturalClassesOS.Add(natClass);
			natClass.SegmentsRC.Add(phonemeM);
			natClass.SegmentsRC.Add(phonemeP);
			natClass.SegmentsRC.Add(phonemeB);

			var anno = CmBaseAnnotation.GetOrCreateFeatureStructBaseAnnotation(Cache, natClass);
			var fs = natClass.SetIntersectionOfPhonemeFeatures(anno.FeaturesOA);
			Assert.AreEqual(1, fs.FeatureSpecsOC.Count, "Expect one feature after intersection");
			Assert.AreEqual("[cons:+]", fs.LongName, "Expect [cons:+]");

			// ==================================
			// Test phoneme feature compatibility
			// ==================================
			Assert.True(phonemeM.FeaturesAreCompatible(null), "Expect true if the feature structure is null");
			Assert.True(phonemeM.FeaturesAreCompatible(fs), "Expect true because m is cons:+");
			Assert.False(phonemeM.FeaturesAreCompatible(fsB), "Expect false because m is son:+ while b is son:-");
		}

		private static void CheckFeatureAndItsValues(string sFeatureName, IFsClosedFeature closedf)
		{
			Assert.AreEqual(sFeatureName, closedf.Name.AnalysisDefaultWritingSystem.Text,
				"Expect to have " + sFeatureName + " feature");
			Assert.AreEqual(2, closedf.ValuesOC.Count, "Expect consonantal to have two values");
			var value = closedf.ValuesOC.First();
			Assert.AreEqual("positive", value.Name.AnalysisDefaultWritingSystem.Text, "Expect positive first value");
			value = closedf.ValuesOC.Last();
			Assert.AreEqual("negative", value.Name.AnalysisDefaultWritingSystem.Text, "Expect negative last value");
		}

	}
}
