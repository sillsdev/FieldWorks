// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportInterlinearAnalysesTests : MemoryOnlyBackendProviderReallyRestoredForEachTestTestBase
	{
		private MemoryStream m_Stream;

		private LinguaLinksImport.ImportInterlinearOptions CreateImportInterlinearOptions(string xml)
		{
			m_Stream = new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray()));
			return new LinguaLinksImport.ImportInterlinearOptions
				{
					AnalysesLevel = LinguaLinksImport.ImportAnalysesLevel.WordGloss,

					BirdData = m_Stream,
					Progress = new DummyProgressDlg(),
					AllottedProgress = 0
				};
		}

		public override void TestTearDown()
		{
			if (m_Stream != null)
				m_Stream.Dispose();
			m_Stream = null;
			base.TestTearDown();
		}

		[Test]
		public void ImportNewHumanApprovedByDefaultWordGloss()
		{
			var wsf = Cache.WritingSystemFactory;

			const string xml = "<document><interlinear-text>" +
				"<paragraphs><paragraph><phrases><phrase><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			LCModel.IText importedText = null;
			var options = CreateImportInterlinearOptions(xml);
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var para = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(para, Is.Not.Null);
				Assert.That(para.Analyses.Count(), Is.EqualTo(1));
				var wfiWord = para.Analyses.First().Wordform;
				int wsWordform = wsf.get_Engine("en").Handle;
				Assert.That(wfiWord.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));

				Assert.That(wfiWord.AnalysesOC.Count, Is.GreaterThan(0));

				var wfiAnalysis = wfiWord.AnalysesOC.First();
				// make sure we also created a morpheme form
				AssertMorphemeFormMatchesWordform(wfiWord, wfiAnalysis, wsWordform);
				// make sure we created a human approved opinion
				AssertHumanApprovedOpinion(wfiWord, wfiAnalysis);

				var at = new AnalysisTree(para.Analyses.First());
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				Assert.That(at.Gloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		private static void AssertMorphemeFormMatchesWordform(IWfiWordform wfiWord, IWfiAnalysis wfiAnalysis, int wsWordform)
		{
			var morphBundle = wfiAnalysis.MorphBundlesOS.FirstOrDefault();
			Assert.That(morphBundle, Is.Not.Null, "expected a morphbundle");
			Assert.That(morphBundle.Form.get_String(wsWordform).Text,
				Is.EqualTo(wfiWord.Form.get_String(wsWordform).Text));
		}

		private void AssertHumanApprovedOpinion(IWfiWordform wfiWord, IWfiAnalysis wfiAnalysis)
		{
			Assert.That(wfiWord.HumanApprovedAnalyses.Count(), Is.EqualTo(1));
			ICmAgent humanAgent = Cache.LangProject.DefaultUserAgent;
			Assert.That(wfiAnalysis.GetAgentOpinion(humanAgent), Is.EqualTo(Opinions.approves));
		}

		[Test]
		public void ImportNewHumanApprovedWordGloss()
		{
			var wsf = Cache.WritingSystemFactory;

			const string xml = "<document><interlinear-text>" +
				"<paragraphs><paragraph><phrases><phrase><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt' analysisStatus='humanApproved'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			LCModel.IText importedText = null;
			var options = CreateImportInterlinearOptions(xml);
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var para = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(para, Is.Not.Null);
				Assert.That(para.Analyses.Count(), Is.EqualTo(1));
				int wsWordform = wsf.get_Engine("en").Handle;
				Assert.That(para.Analyses.First().Wordform.Form.get_String(wsWordform).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				var at = new AnalysisTree(para.Analyses.First());
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				Assert.That(at.Gloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				// make sure we also created a morpheme form
				AssertMorphemeFormMatchesWordform(at.Wordform, at.WfiAnalysis, wsWordform);
				// make sure we created a human approved opinion
				AssertHumanApprovedOpinion(at.Wordform, at.WfiAnalysis);

				// assert that nothing else was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		/// <summary>
		/// NOTE: multiple ws alternatives per gloss isn't good practice (other than ipa), since typically there can be one to many or
		/// many to one meanings to words across languages. But, it currently supported in FLEx, so we should be able to handle it.
		/// </summary>
		[Test]
		public void ImportNewHumanApprovedWordGloss_WsAlternatives()
		{
			var wsf = Cache.WritingSystemFactory;

			const string xml = "<document><interlinear-text>" +
				"<paragraphs><paragraph><phrases><phrase><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt' analysisStatus='humanApproved'>absurdo</item>" +
						"<item type='gls' lang='fr' analysisStatus='humanApproved'>absierd</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			LCModel.IText importedText = null;
			var options = CreateImportInterlinearOptions(xml);
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var para = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(para, Is.Not.Null);
				Assert.That(para.Analyses.Count(), Is.EqualTo(1));
				int wsWordform = wsf.get_Engine("en").Handle;
				Assert.That(para.Analyses.First().Wordform.Form.get_String(wsWordform).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				var at = new AnalysisTree(para.Analyses.First());
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				Assert.That(at.Gloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));
				Assert.That(at.Gloss.Form.get_String(wsf.get_Engine("fr").Handle).Text, Is.EqualTo("absierd"));

				// make sure we also created a morpheme form
				AssertMorphemeFormMatchesWordform(at.Wordform, at.WfiAnalysis, wsWordform);
				// make sure we created a human approved opinion
				AssertHumanApprovedOpinion(at.Wordform, at.WfiAnalysis);

				// assert that nothing else was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void SkipNewGuessedWordGloss()
		{
			var wsf = Cache.WritingSystemFactory;
			const string xml = "<document><interlinear-text>" +
				"<paragraphs><paragraph><phrases><phrase><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt' analysisStatus='guessByHumanApproved'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			LCModel.IText importedText = null;
			var options = CreateImportInterlinearOptions(xml);
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var para = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(para, Is.Not.Null);
				Assert.That(para.Analyses.Count(), Is.EqualTo(1));
				Assert.That(para.Analyses.First().Wordform.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				var at = new AnalysisTree(para.Analyses.First());
				Assert.That(at.Gloss, Is.Null, "Analysis should not be WfiGloss");
				// assert that nothing else was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(0));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(0));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void ImportMorphemes_WhenAllMorphemesMatch_ExistingWifiAnalysisAreUsed()
		{
			// 1. Build pre-existing data with a known wordform and morphemes ("cat", "-s")
			var sl = Cache.ServiceLocator;
			LCModel.IText text;
			IStTxtPara para = null;
			IWfiWordform extantWordform = null;
			var segGuid = Guid.Empty;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache,
					new Guid("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"));
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);

				var segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				segGuid = segment.Guid;

				// Use the helper method to create a wordform with an analysis and two morph bundles and a gloss
				extantWordform = BuildWordformWithMorphemes();
				// Add the gloss analysis to the segment
				segment.AnalysesRS.Add(extantWordform.AnalysesOC.First().MeaningsOC.First());
			});

			// Get initial object counts for verification
			var initialWordformCount =
				Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count;
			var initialAnalysisCount =
				Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
			var initialGlossCount =
				Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count;
			var initialMorphBundleCount =
				Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count;

			// 2. Create XML for import where the morphemes match the existing ones
			var xml = "<document><interlinear-text guid='BBBBBBBB-AAAA-BBBB-BBBB-BBBBBBBBBBBB'>" +
					  "<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'><words>" +
					  "<word guid='" + extantWordform.Guid + "'>" +
					  "<item type='txt' lang='fr'>cats</item>" +
					  "<item type='gls' lang='en'>gato</item>" +
					  "<morphemes>" +
					  "<morph><item type='txt' lang='fr'>cat</item></morph>" +
					  "<morph><item type='txt' lang='fr'>-s</item></morph>" +
					  "</morphemes>" +
					  "</word>" +
					  "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			// 3. Perform the import
			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);

			// 4. Verify that no new objects were created
			var finalWordformCount =
				Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count;
			var finalAnalysisCount =
				Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
			var finalGlossCount =
				Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count;
			var finalMorphBundleCount =
				Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count;

			Assert.That(finalWordformCount, Is.EqualTo(initialWordformCount),
				"A new Wordform should not have been created.");
			Assert.That(finalAnalysisCount, Is.EqualTo(initialAnalysisCount),
				"A new Analysis should not have been created.");
			Assert.That(finalGlossCount, Is.EqualTo(initialGlossCount),
				"A new Gloss should not have been created.");
			Assert.That(finalMorphBundleCount, Is.EqualTo(initialMorphBundleCount),
				"New MorphBundles should not have been created.");

			// Verify the imported analysis is the same object
			var importedPara = importedText.ContentsOA.ParagraphsOS[0] as IStTxtPara;
			var importedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
			Assert.That(importedAnalysis, Is.SameAs(extantWordform.AnalysesOC.First().MeaningsOC.First()),
				"The imported analysis should be the same as the original.");
		}

		[Test]
		public void ImportNewText_PhraseWsUsedForMatching()
		{
			// 1. Build pre-existing data with a known wordform and morphemes ("cat", "-s")
			var sl = Cache.ServiceLocator;
			LCModel.IText text;
			IStTxtPara para = null;
			IWfiWordform extantWordform = null;
			var segGuid = Guid.Empty;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.AddToCurrentVernacularWritingSystems(new CoreWritingSystemDefinition("pt"));
				text = sl.GetInstance<ITextFactory>().Create(Cache,
					new Guid("CCCCCCCC-DDDD-CCCC-CCCC-CCCCCCCCCCCC"));
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);

				var segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				segGuid = segment.Guid;

				extantWordform = BuildWordformWithMorphemes("pt");
				segment.AnalysesRS.Add(extantWordform);
			});

			// Get initial object counts for verification
			var initialWordformCount =
				Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count;
			var initialAnalysisCount =
				Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
			var initialGlossCount =
				Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count;
			var initialMorphBundleCount =
				Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count;

			// 2. Create XML for import with a different second morpheme ("cat", "-ing")
			var xml = "<document><interlinear-text guid='CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC'>" +
					  "<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'>" +
					  "<item type='txt' lang='pt'>cats</item>" +
					  "<words>" +
					  "<word guid='" + extantWordform.Guid + "'>" +
					  "<item type='txt' lang='pt'>cats</item>" +
					  "<item type='gls' lang='en'>gato</item>" +
					  "<morphemes>" +
					  "<morph><item type='txt' lang='pt'>cat</item></morph>" +
					  "<morph><item type='txt' lang='pt'>-s</item></morph>" +
					  "</morphemes>" +
					  "</word>" +
					  "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			// 3. Perform the import
			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);

			// 4. Verify that no new objects were created
			var finalWordformCount =
				Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count;
			var finalAnalysisCount =
				Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
			var finalGlossCount =
				Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count;
			var finalMorphBundleCount =
				Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count;

			Assert.That(finalWordformCount, Is.EqualTo(initialWordformCount),
				"A new Wordform should not have been created.");
			Assert.That(finalAnalysisCount, Is.EqualTo(initialAnalysisCount),
				"A new Analysis should not have been created.");
			Assert.That(finalGlossCount, Is.EqualTo(initialGlossCount),
				"A new Gloss should not have been created.");
			Assert.That(finalMorphBundleCount, Is.EqualTo(initialMorphBundleCount),
				"New MorphBundles should not have been created.");

			// Verify the imported analysis is the same object
			var importedPara = importedText.ContentsOA.ParagraphsOS[0] as IStTxtPara;
			var importedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
			Assert.That(importedAnalysis, Is.SameAs(extantWordform.AnalysesOC.First().MeaningsOC.First()),
				"The imported analysis should be the same as the original.");
		}

		[Test]
		public void ImportMorphemes_WhenMorphemesDoNotMatch_WordFormGetsNewWfiAnalysis()
		{
			// 1. Build pre-existing data with a known wordform and morphemes ("cat", "-s")
			var sl = Cache.ServiceLocator;
			LCModel.IText text;
			IStTxtPara para = null;
			IWfiWordform extantWordform = null;
			var segGuid = Guid.Empty;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache,
					new Guid("CCCCCCCC-DDDD-CCCC-CCCC-CCCCCCCCCCCC"));
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);

				var segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				segGuid = segment.Guid;

				extantWordform = BuildWordformWithMorphemes();
				segment.AnalysesRS.Add(extantWordform);
			});

			// Get initial object counts for verification
			var initialWordformCount =
				Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count;
			var initialAnalysisCount =
				Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
			var initialMorphBundleCount =
				Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count;

			// 2. Create XML for import with a different second morpheme ("cat", "-ing")
			var xml = "<document><interlinear-text guid='CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC'>" +
					  "<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'><words>" +
					  "<word guid='" + extantWordform.Guid + "'>" +
					  "<item type='txt' lang='fr'>cats</item>" +
					  "<item type='gls' lang='en'>gato</item>" +
					  "<morphemes>" +
					  "<morph><item type='txt' lang='fr'>cat</item></morph>" +
					  "<morph><item type='txt' lang='fr'>-ing</item></morph>" +
					  "</morphemes>" +
					  "</word>" +
					  "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			// 3. Perform the import
			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);

			// 4. Verify that new objects were created due to the mismatch
			var finalWordformCount =
				Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count;
			var finalAnalysisCount =
				Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
			var finalMorphBundleCount =
				Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count;

			Assert.That(finalWordformCount, Is.EqualTo(initialWordformCount),
				"Wordform count should not change.");
			Assert.That(finalAnalysisCount, Is.EqualTo(initialAnalysisCount + 1),
				"A new Analysis should have been created.");
			Assert.That(finalMorphBundleCount, Is.EqualTo(initialMorphBundleCount + 2),
				"Two new MorphBundles should have been created.");

			// Verify the imported analysis and its contents
			var importedPara = importedText.ContentsOA.ParagraphsOS[0] as IStTxtPara;
			if(!(importedPara.SegmentsOS[0].AnalysesRS[0] is IWfiGloss importedAnalysis))
				Assert.Fail("Incorrect analysis type imported");
			else
			{
				Assert.That(importedAnalysis.Analysis.MorphBundlesOS.Count, Is.EqualTo(2),
					"The new analysis should have two morph bundles.");
				Assert.That(
					importedAnalysis.Analysis.MorphBundlesOS[0].Form.get_String(Cache.DefaultVernWs).Text,
					Is.EqualTo("cat"));
				Assert.That(
					importedAnalysis.Analysis.MorphBundlesOS[1].Form.get_String(Cache.DefaultVernWs).Text,
					Is.EqualTo("-ing"));
			}
		}

		[Test]
		public void ImportMorphemes_WhenMorphemesMatchButOutOfOrder_NewObjectsAreCreated()
		{
			// 1. Build pre-existing data with a known wordform and morphemes ("cat", "-s")
			var sl = Cache.ServiceLocator;
			LCModel.IText text;
			IStTxtPara para = null;
			IWfiWordform extantWordform = null;
			var segGuid = Guid.Empty;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache,
					new Guid("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD"));
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);

				var segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				segGuid = segment.Guid;

				extantWordform = BuildWordformWithMorphemes();
				segment.AnalysesRS.Add(extantWordform);
			});

			// Get initial object counts for verification
			var initialWordformCount =
				Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count;
			var initialAnalysisCount =
				Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
			var initialMorphBundleCount =
				Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count;

			// 2. Create XML for import where the morphemes are the same but the order is reversed
			var xml = "<document version='1'><interlinear-text guid='DDDDDDDD-EEEE-DDDD-DDDD-DDDDDDDDDDDD'>" +
					  "<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'><words>" +
					  "<word guid='" + extantWordform.Guid + "'>" +
					  "<item type='txt' lang='fr'>cats</item>" +
					  "<morphemes>" +
					  "<morph><item type='txt' lang='fr'>-s</item></morph>" +
					  "<morph><item type='txt' lang='fr'>cat</item></morph>" +
					  "</morphemes>" +
					  "</word>" +
					  "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			// 3. Perform the import
			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);

			// 4. Verify that new objects were created due to the order mismatch
			var finalWordformCount =
				Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count;
			var finalAnalysisCount =
				Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
			var finalMorphBundleCount =
				Cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().Count;

			Assert.That(finalWordformCount, Is.EqualTo(initialWordformCount),
				"Wordform count should not change.");
			Assert.That(finalAnalysisCount, Is.EqualTo(initialAnalysisCount + 1),
				"A new Analysis should have been created.");
			Assert.That(finalMorphBundleCount, Is.EqualTo(initialMorphBundleCount + 2),
				"Two new MorphBundles should have been created.");

			// Verify the imported analysis and its contents
			var importedPara = importedText.ContentsOA.ParagraphsOS[0] as IStTxtPara;
			if(!(importedPara.SegmentsOS[0].AnalysesRS[0] is IWfiAnalysis importedAnalysis))
				Assert.Fail("Incorrect analysis type imported");
			else
			{
				Assert.That(importedAnalysis.MorphBundlesOS.Count, Is.EqualTo(2),
					"The new analysis should have two morph bundles.");
				Assert.That(
					importedAnalysis.MorphBundlesOS[0].Form.get_String(Cache.DefaultVernWs).Text,
					Is.EqualTo("-s"));
				Assert.That(
					importedAnalysis.MorphBundlesOS[1].Form.get_String(Cache.DefaultVernWs).Text,
					Is.EqualTo("cat"));
			}
		}

		[Test]
		public void ImportNewUserConfirmedWordGlossToExistingWord()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			LCModel.IText text;
			IStTxtPara para = null;
			IWfiWordform extantWordForm = null;
			ITsString paraContents = null;
			Guid segGuid = new Guid();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache, new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"));
				//Cache.LangProject.TextsOC.Add(text);
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);
				paraContents = TsStringUtils.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeString("supercalifragilisticexpialidocious",
					wsf.get_Engine("en").Handle);
				segGuid = segment.Guid;
				extantWordForm = sl.GetInstance<IWfiWordformFactory>().Create(wform);
				segment.AnalysesRS.Add(extantWordForm);
			});

			// import an analysis with word gloss
			string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
				"<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(importedPara, Is.Not.Null);

				// make sure we've added the expected word gloss
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var importedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var importedWordForm = importedAnalysis.Wordform;
				var at = new AnalysisTree(importedAnalysis);
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				var importedGloss = at.Gloss;
				Assert.That(importedGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				Assert.That(importedPara.Guid.Equals(para.Guid));
				at = new AnalysisTree(para.Analyses.First());
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				var gloss = at.Gloss;
				Assert.That(gloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				// make sure nothing has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.That(importedPara.Contents.Text, Is.EqualTo(paraContents.Text), "Imported Para contents differ from original");
				Assert.That(paraContents.Equals(importedPara.Contents), Is.True, "Ws mismatch between imported and original paragraph");
				Assert.That(importedWordForm.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				// assert that nothing else was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		/// <summary>
		/// A helper method that builds a valid LCM object graph for a wordform with an analysis
		/// and morphemes, ensuring all objects have a proper owner. This method should be called
		/// from within a NonUndoableUnitOfWorkHelper.Do block.
		/// </summary>
		private IWfiWordform BuildWordformWithMorphemes(string vernacularWs = "fr")
		{
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			// Create the IWfiWordform object
			var wordform = sl.GetInstance<IWfiWordformFactory>().Create();
			wordform.Form.set_String(wsf.get_Engine(vernacularWs).Handle, "cats");

			// Establish the ownership chain for the wordform's internal objects first.
			var analysis = sl.GetInstance<IWfiAnalysisFactory>().Create();
			var gloss = sl.GetInstance<IWfiGlossFactory>().Create();
			wordform.AnalysesOC.Add(analysis);
			analysis.MeaningsOC.Add(gloss);
			gloss.Form.set_String(wsf.get_Engine("en").Handle, "gato");

				var stemMorphBundle = sl.GetInstance<IWfiMorphBundleFactory>().Create();
			analysis.MorphBundlesOS.Add(stemMorphBundle);

			var affixMorphBundle = sl.GetInstance<IWfiMorphBundleFactory>().Create();
			analysis.MorphBundlesOS.Add(affixMorphBundle);

			// Create the owning LexEntries for the allomorphs. This is a new, crucial step.
			// For this unit test, we'll create separate LexEntries to own the stem and the affix.
			var stemLexEntry = sl.GetInstance<ILexEntryFactory>().Create();
			var affixLexEntry = sl.GetInstance<ILexEntryFactory>().Create();

			// Create the allomorphs and establish their ownership via the LexEntries.
			// The LexEntry.LexemeFormOA property is an Owning Atom.
			var stemAllomorph = sl.GetInstance<IMoStemAllomorphFactory>().Create();
			stemLexEntry.LexemeFormOA = stemAllomorph;

			var affixAllomorph = sl.GetInstance<IMoAffixAllomorphFactory>().Create();
			affixLexEntry.LexemeFormOA = affixAllomorph;

			// Now that the allomorphs are valid and owned, we can assign them to the MorphRA properties.
			stemMorphBundle.MorphRA = stemAllomorph;
			affixMorphBundle.MorphRA = affixAllomorph;

			// Now, set the string properties for the objects.
			wordform.Form.set_String(wsf.get_Engine(vernacularWs).Handle, "cats");
			stemMorphBundle.Form.set_String(wsf.get_Engine(vernacularWs).Handle, "cat");
			affixMorphBundle.Form.set_String(wsf.get_Engine(vernacularWs).Handle, "-s");

			// Assume ILexSense exists and can be created or retrieved
			var lexSenseForStem = sl.GetInstance<ILexSenseFactory>().Create();
			stemLexEntry.SensesOS.Add(lexSenseForStem);
			stemMorphBundle.SenseRA = lexSenseForStem;

			var lexSenseForAffix = sl.GetInstance<ILexSenseFactory>().Create();
			affixLexEntry.SensesOS.Add(lexSenseForAffix);
			affixMorphBundle.SenseRA = lexSenseForAffix;

			return wordform;
		}
		[Test]
		public void ImportNewUserConfirmedWordGlossToExistingWordWithGuid()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			LCModel.IText text;
			IStTxtPara para = null;
			IWfiWordform word = null;
			ITsString paraContents = null;
			Guid segGuid = new Guid();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache, new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"));
				//Cache.LangProject.TextsOC.Add(text);
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);
				paraContents = TsStringUtils.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeString("supercalifragilisticexpialidocious",
					wsf.get_Engine("en").Handle);
				segGuid = segment.Guid;
				word = sl.GetInstance<IWfiWordformFactory>().Create(wform);
				segment.AnalysesRS.Add(word);
			});

			// import an analysis with word gloss
			string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
				"<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'><words>" +
					"<word guid='" + word.Guid + "'>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(importedPara, Is.Not.Null);

				// make sure we've added the expected word gloss
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var importedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var importedWord = importedAnalysis.Wordform;
				Assert.That(importedWord.Guid, Is.EqualTo(word.Guid));
				var at = new AnalysisTree(importedAnalysis);
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				var importedGloss = at.Gloss;
				Assert.That(importedGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				Assert.That(importedPara.Guid.Equals(para.Guid));
				at = new AnalysisTree(para.Analyses.First());
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				var gloss = at.Gloss;
				Assert.That(gloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				// make sure nothing has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.That(importedPara.Contents.Text, Is.EqualTo(paraContents.Text), "Imported Para contents differ from original");
				Assert.That(paraContents.Equals(importedPara.Contents), Is.True, "Ws mismatch between imported and original paragraph");
				Assert.That(importedWord.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));

				// assert that nothing else was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		[Test, Ignore("It appears that segments cannot be reused because the paragraphs are getting cleared during import?")]
		public void SkipUserConfirmedWordGlossToDifferentWordGloss()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			LCModel.IText text;

			IWfiWordform word = null;
			ITsString paraContents = null;
			var segGuid = new Guid();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache, new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"));
				//Cache.LangProject.TextsOC.Add(text);
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				IStTxtPara para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);
				para.Contents = TsStringUtils.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				paraContents = para.Contents;
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeString("supercalifragilisticexpialidocious",
					wsf.get_Engine("en").Handle);
				segGuid = segment.Guid;
				word = sl.GetInstance<IWfiWordformFactory>().Create(wform);
				var analysisTree = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(word);
				analysisTree.Gloss.Form.set_String(wsf.get_Engine("pt").Handle, "absirdo");
				segment.AnalysesRS.Add(analysisTree.Gloss);
			});

			// import an analysis with word gloss
			string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
				"<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(importedPara, Is.Not.Null);

				// make sure we've skipped the expected word gloss
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var skippedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var skippedWord = skippedAnalysis.Wordform;
				var at = new AnalysisTree(skippedAnalysis);
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				var skippedGloss = at.Gloss;
				Assert.That(skippedGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absirdo"));
				Assert.That(skippedWord.Guid, Is.EqualTo(segGuid));

				// make sure nothing else has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.That(importedPara.Contents.Text, Is.EqualTo(paraContents.Text), "Imported Para contents differ from original");
				Assert.That(paraContents.Equals(importedPara.Contents), Is.True, "Ws mismatch between imported and original paragraph");
				Assert.That(skippedWord.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				Assert.That(skippedWord.Guid, Is.EqualTo(word.Guid));
				// assert that nothing else was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void SkipConfirmedWordGlossToSameWordGloss()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			LCModel.IText text;

			IWfiWordform word = null;
			ITsString paraContents = null;
			var segGuid = new Guid();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache, new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"));
				//Cache.LangProject.TextsOC.Add(text);
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				IStTxtPara para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);
				paraContents = TsStringUtils.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeString("supercalifragilisticexpialidocious",
					wsf.get_Engine("en").Handle);
				segGuid = segment.Guid;
				word = sl.GetInstance<IWfiWordformFactory>().Create(wform);
				var analysisTree = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(word);
				analysisTree.Gloss.Form.set_String(wsf.get_Engine("pt").Handle, "absurdo");
				segment.AnalysesRS.Add(analysisTree.Gloss);
			});

			// import an analysis with word gloss
			string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
				"<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(importedPara, Is.Not.Null);

				// assert that nothing was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));

				// make sure existing word gloss didn't change
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var skippedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var skippedWord = skippedAnalysis.Wordform;
				var at = new AnalysisTree(skippedAnalysis);
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				var skippedGloss = at.Gloss;
				Assert.That(skippedGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));
				Assert.That(skippedWord.Guid, Is.EqualTo(word.Guid));

				// make sure nothing else has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.That(importedPara.Contents.Text, Is.EqualTo(paraContents.Text), "Imported Para contents differ from original");
				Assert.That(paraContents.Equals(importedPara.Contents), Is.True, "Ws mismatch between imported and original paragraph");
				Assert.That(skippedWord.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				Assert.That(skippedWord.Guid, Is.EqualTo(word.Guid));
			}
		}

		[Test]
		public void ImportNewUserConfirmedWordGlossSeparatedFromExistingWfiAnalysis()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			LCModel.IText text;

			IWfiWordform extandWordForm = null;
			ITsString paraContents = null;
			var segGuid = new Guid();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache, new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"));
				//Cache.LangProject.TextsOC.Add(text);
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				IStTxtPara para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);
				paraContents = TsStringUtils.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeString("supercalifragilisticexpialidocious",
					wsf.get_Engine("en").Handle);
				segGuid = segment.Guid;
				extandWordForm = sl.GetInstance<IWfiWordformFactory>().Create(wform);
				var extantAnalysis = sl.GetInstance<IWfiAnalysisFactory>().Create();
				extandWordForm.AnalysesOC.Add(extantAnalysis);
				segment.AnalysesRS.Add(extandWordForm);
			});

			// import an analysis with word gloss
			string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
				"<paragraphs><paragraph><phrases><phrase guid='" + segGuid + "'><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.That(imported, Is.Not.Null);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.That(importedPara, Is.Not.Null);

				// assert that new Analysis was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(2));

				// make sure imported word gloss is correct
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var importedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var importedWordForm = importedAnalysis.Wordform;
				var at = new AnalysisTree(importedAnalysis);
				Assert.That(at.Gloss, Is.Not.Null, "IAnalysis should be WfiGloss");
				var newGloss = at.Gloss;
				Assert.That(newGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				// make sure nothing else has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.That(importedPara.Contents.Text, Is.EqualTo(paraContents.Text), "Imported Para contents differ from original");
				Assert.That(paraContents.Equals(importedPara.Contents), Is.True, "Ws mismatch between imported and original paragraph");
				Assert.That(importedWordForm.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				// The wordform should be reused, but with a new analysis
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
			}
		}

		//[Test]
		//public void ImportNewUserConfirmedWordGlossMergeIntoAnalysisMatchingStemOfExistingAnalysis()
		//{
		// TODO
		//}

		[Test]
		public void ImportUnknownPhraseWholeSegmentNoVersion_MakesSeparateWords()
		{
			// import an analysis with word gloss
			string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
				"<paragraphs><paragraph><phrases><phrase><words>" +
				"<word>" +
					"<item type='txt' lang='en'>this is not a phrase</item>" +
				"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			var stText = importedText.ContentsOA;
			var para = (IStTxtPara)stText.ParagraphsOS[0];
			var seg = para.SegmentsOS[0];
			Assert.That(para.Contents.Text, Is.EqualTo("this is not a phrase"));
			// It's acceptable either that it hasn't been parsed at all (and will be when we look at it) and so
			// has no analyses, or that it's been parsed into five words. The other likely outcome is one phrase,
			// which is not acceptable for parsing Saymore output (LT-12621).
			Assert.That(seg.AnalysesRS.Count, Is.EqualTo(5).Or.EqualTo(0));
		}

		[Test]
		public void ImportKnownPhraseWholeSegmentNoVersion_MakesPhrase()
		{
			// import an analysis with word gloss
			string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
						 "<paragraphs><paragraph><phrases><phrase><words>" +
						 "<word>" +
							"<item type='txt' lang='en'>this is a phrase</item>" +
						 "</word>" +
						 "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
			{
				var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
				int wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
				wf.Form.set_String(wsEn, TsStringUtils.MakeString("this is a phrase", wsEn));
			});
			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			var stText = importedText.ContentsOA;
			var para = (IStTxtPara)stText.ParagraphsOS[0];
			var seg = para.SegmentsOS[0];
			Assert.That(seg.AnalysesRS.Count, Is.EqualTo(1));
		}

		[Test]
		public void ImportUnknownPhraseWholeSegmentVersion_MakesPhrase()
		{
			// import an analysis with word gloss
			string xml = "<document version=\"2\"><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
						 "<paragraphs><paragraph><phrases><phrase><words>" +
						 "<word>" +
							"<item type='txt' lang='en'>this is not a phrase</item>" +
						 "</word>" +
						 "</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			LCModel.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			var stText = importedText.ContentsOA;
			var para = (IStTxtPara)stText.ParagraphsOS[0];
			var seg = para.SegmentsOS[0];
			Assert.That(seg.AnalysesRS.Count, Is.EqualTo(1));
		}

		[Test]
		public void DeserializeWordsFragDocument()
		{
			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>glossedonce</item>
						<item type='gls' lang='en'>onlygloss</item>
					  </word>
					  <word>
						<item type='txt' lang='qaa-x-kal'>glossedtwice</item>
						<item type='gls' lang='en'>firstgloss</item>
						<item type='gls' lang='en'>secondgloss</item>
					  </word>
					  <word>
						<item type='txt' lang='qaa-x-kal'>support a phrase</item>
						<item type='gls' lang='en'>phrase gloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var wsQaa = Cache.WritingSystemFactory.GetWsFromStr("qaa-x-kal");
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsQaa), Throws.Nothing);
		}

		[Test]
		public void WordsFragDoc_OneWordAndOneGloss()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			CoreWritingSystemDefinition wsKal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsKal);

			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>glossedonce</item>
						<item type='gls' lang='en'>onlygloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);

			var wordsRepo = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			var wff1 = wordsRepo.GetMatchingWordform(wsKal.Handle, "glossedonce");
			Assert.That(wff1, Is.Not.Null);
			Assert.That(wff1.AnalysesOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC.ElementAt(0).Form.get_String(wsEn).Text, Is.EqualTo("onlygloss"));

			Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
		}

		[Test]
		public void WordsFragDoc_OneWordAndOneGloss_AvoidDuplication()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			CoreWritingSystemDefinition wsKal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsKal);

			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>glossedonce</item>
						<item type='gls' lang='en'>onlygloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);

			// First import
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);

			// Second Import
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);

			var wordsRepo = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			var wff1 = wordsRepo.GetMatchingWordform(wsKal.Handle, "glossedonce");
			Assert.That(wff1, Is.Not.Null);
			Assert.That(wff1.AnalysesOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC.ElementAt(0).Form.get_String(wsEn).Text, Is.EqualTo("onlygloss"));

			Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
		}

		[Test]
		public void WordsFragDoc_OneWordAndMultiGloss()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			CoreWritingSystemDefinition wsKal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsKal);

			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>glossedtwice</item>
						<item type='gls' lang='en'>firstgloss</item>
						<item type='gls' lang='en'>secondgloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);

			var wordsRepo = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			var wff1 = wordsRepo.GetMatchingWordform(wsKal.Handle, "glossedtwice");
			Assert.That(wff1, Is.Not.Null);
			Assert.That(wff1.AnalysesOC, Has.Count.EqualTo(2), "multiple word glosses (without specifying morphology) should create separate WfiAnalyses with separate glosses");
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC.ElementAt(0).Form.get_String(wsEn).Text, Is.EqualTo("firstgloss"));
			Assert.That(wff1.AnalysesOC.ElementAt(1).MeaningsOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(1).MeaningsOC.ElementAt(0).Form.get_String(wsEn).Text, Is.EqualTo("secondgloss"));

			Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(2));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(2));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
		}

		[Test]
		public void WordsFragDoc_OneWordAndMultiGloss_AvoidDuplication()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			CoreWritingSystemDefinition wsKal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsKal);

			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>glossedtwice</item>
						<item type='gls' lang='en'>firstgloss</item>
						<item type='gls' lang='en'>secondgloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			// First import
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);

			// Second import
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);

			var wordsRepo = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			var wff1 = wordsRepo.GetMatchingWordform(wsKal.Handle, "glossedtwice");
			Assert.That(wff1, Is.Not.Null);
			Assert.That(wff1.AnalysesOC, Has.Count.EqualTo(2), "multiple word glosses (without specifying morphology) should create separate WfiAnalyses with separate glosses");
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC.ElementAt(0).Form.get_String(wsEn).Text, Is.EqualTo("firstgloss"));
			Assert.That(wff1.AnalysesOC.ElementAt(1).MeaningsOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(1).MeaningsOC.ElementAt(0).Form.get_String(wsEn).Text, Is.EqualTo("secondgloss"));

			Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(2));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(2));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
		}

		[Test]
		public void WordsFragDoc_OneWordPhraseAndOneGloss()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			CoreWritingSystemDefinition wsKal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsKal);

			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>support a phrase</item>
						<item type='gls' lang='en'>phrase gloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);

			var wordsRepo = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			var wff1 = wordsRepo.GetMatchingWordform(wsKal.Handle, "support a phrase");
			Assert.That(wff1, Is.Not.Null);
			Assert.That(wff1.AnalysesOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC.ElementAt(0).Form.get_String(wsEn).Text, Is.EqualTo("phrase gloss"));

			Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
		}

		[Test]
		public void WordsFragDoc_OneWordPhraseAndOneGloss_AvoidDuplicates()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			CoreWritingSystemDefinition wsKal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsKal);

			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>support a phrase</item>
						<item type='gls' lang='en'>phrase gloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			// First Import
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);
			// Second Import
			Assert.That(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss, wsKal.Handle), Throws.Nothing);

			var wordsRepo = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			var wff1 = wordsRepo.GetMatchingWordform(wsKal.Handle, "support a phrase");
			Assert.That(wff1, Is.Not.Null);
			Assert.That(wff1.AnalysesOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC, Has.Count.EqualTo(1));
			Assert.That(wff1.AnalysesOC.ElementAt(0).MeaningsOC.ElementAt(0).Form.get_String(wsEn).Text, Is.EqualTo("phrase gloss"));

			Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
			Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
		}
	}
}
