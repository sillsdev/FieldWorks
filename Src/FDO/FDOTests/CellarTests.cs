// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CellarTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
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
		public static readonly string ksFS1 = string.Format("<item id=\"fgender\" type=\"feature\"><abbrev ws=\"en\">Gen</abbrev><term ws=\"en\">gender</term>" +
	"<def ws=\"en\">Grammatical gender is a noun class system, composed of two or three classes,{0}whose nouns that have human male and female referents tend to be in separate classes.{0}Other nouns that are classified in the same way in the language may not be classed by{0}any correlation with natural sex distinctions.</def>" +
	"<citation>Hartmann and Stork 1972:93</citation><citation>Foley and Van Valin 1984:325</citation><citation>Mish et al. 1990:510</citation>" +
	"<citation>Crystal 1985:133</citation><citation>Dixon, R. 1968:105</citation><citation>Quirk, et al. 1985:314</citation>" +
	"<item id=\"vMasc\" type=\"value\"><abbrev ws=\"en\">Masc</abbrev><term ws=\"en\">masculine gender</term><def ws=\"en\">Masculine gender is a grammatical gender that - marks nouns having human or animal male referents, and - often marks nouns having referents that do not have distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:730</citation><fs id=\"vMascFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Masc\" /></f></fs></item><item id=\"vFem\" type=\"value\"><abbrev ws=\"en\">Fem</abbrev><term ws=\"en\">feminine gender</term><def ws=\"en\">Feminine gender is a grammatical gender that - marks nouns that have human or animal female referents, and - often marks nouns that have referents that do not carry distinctions of sex.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:456</citation><fs id=\"vFemFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Fem\" /></f></fs></item><item id=\"vNeut\" type=\"value\"><abbrev ws=\"en\">Neut</abbrev><term ws=\"en\">neuter gender</term><def ws=\"en\">Neuter gender is a grammatical gender that - includes those nouns having referents which do not have distinctions of sex, and - often includes some which do have a natural sex distinction.</def><citation>Hartmann and Stork 1972:93</citation><citation>Mish et al. 1990:795</citation><fs id=\"vNeutFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"Neut\" /></f></fs></item><item id=\"vUnknownfgender\" type=\"value\"><abbrev ws=\"en\">?</abbrev><term ws=\"en\">unknown gender</term><fs id=\"vUnknownfgenderFS\" type=\"Agr\"><f name=\"Gen\"><sym value=\"?\" /></f></fs></item></item>",
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
}
