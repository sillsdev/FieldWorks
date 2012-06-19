using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_stream gets disposed in TestTearDown()")]
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
			FDO.IText importedText = null;
			var options = CreateImportInterlinearOptions(xml);
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var para = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(para);
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
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
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
			Assert.NotNull(morphBundle, "expected a morphbundle");
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
			FDO.IText importedText = null;
			var options = CreateImportInterlinearOptions(xml);
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var para = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(para);
				Assert.That(para.Analyses.Count(), Is.EqualTo(1));
				int wsWordform = wsf.get_Engine("en").Handle;
				Assert.That(para.Analyses.First().Wordform.Form.get_String(wsWordform).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				var at = new AnalysisTree(para.Analyses.First());
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
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
			FDO.IText importedText = null;
			var options = CreateImportInterlinearOptions(xml);
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var para = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(para);
				Assert.That(para.Analyses.Count(), Is.EqualTo(1));
				int wsWordform = wsf.get_Engine("en").Handle;
				Assert.That(para.Analyses.First().Wordform.Form.get_String(wsWordform).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				var at = new AnalysisTree(para.Analyses.First());
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
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
			FDO.IText importedText = null;
			var options = CreateImportInterlinearOptions(xml);
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var para = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(para);
				Assert.That(para.Analyses.Count(), Is.EqualTo(1));
				Assert.That(para.Analyses.First().Wordform.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				var at = new AnalysisTree(para.Analyses.First());
				Assert.IsNull(at.Gloss, "Analysis should not be WfiGloss");
				// assert that nothing else was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(0));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(0));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void ImportNewUserConfirmedWordGlossToExistingWord()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			FDO.IText text;

			IWfiWordform word = null;
			ITsString paraContents = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache, new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"));
				//Cache.LangProject.TextsOC.Add(text);
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				IStTxtPara para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);
				para.Contents = Cache.TsStrFactory.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				paraContents = para.Contents;
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeTss("supercalifragilisticexpialidocious",
					wsf.get_Engine("en").Handle);
				word = sl.GetInstance<IWfiWordformFactory>().Create(wform);
				segment.AnalysesRS.Add(word);
			});

			// import an analysis with word gloss
			const string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
				"<paragraphs><paragraph><phrases><phrase><words>" +
					"<word>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			FDO.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(importedPara);

				// make sure we've added the expected word gloss
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var importedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var importedWord = importedAnalysis.Wordform;
				var at = new AnalysisTree(importedAnalysis);
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
				var importedGloss = at.Gloss;
				Assert.That(importedGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				/* NOTE: currently paragraphs are getting recreated, so we can't depend upon that ownership tree persisting after the import
				 *
				Assert.That(importedPara.Guid, Is.SameAs(para.Guid));
				var at = new AnalysisTree(para.Analyses.First());
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
				var gloss = at.Gloss;
				Assert.That(gloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));
				 */

				// make sure nothing has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.AreEqual(paraContents.Text, importedPara.Contents.Text, "Imported Para contents differ from original");
				Assert.IsTrue(paraContents.Equals(importedPara.Contents), "Ws mismatch between imported and original paragraph");
				Assert.That(importedWord.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				Assert.That(importedWord.Guid, Is.EqualTo(word.Guid));
				// assert that nothing else was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void ImportNewUserConfirmedWordGlossToExistingWordWithGuid()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			FDO.IText text;

			IWfiWordform word = null;
			ITsString paraContents = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				text = sl.GetInstance<ITextFactory>().Create(Cache, new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"));
				//Cache.LangProject.TextsOC.Add(text);
				var sttext = sl.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = sttext;
				IStTxtPara para = sl.GetInstance<IStTxtParaFactory>().Create();
				sttext.ParagraphsOS.Add(para);
				para.Contents = Cache.TsStrFactory.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				paraContents = para.Contents;
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeTss("supercalifragilisticexpialidocious",
					wsf.get_Engine("en").Handle);
				word = sl.GetInstance<IWfiWordformFactory>().Create(wform);
				segment.AnalysesRS.Add(word);
			});

			// import an analysis with word gloss
			string xml = "<document><interlinear-text guid='AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'>" +
				"<paragraphs><paragraph><phrases><phrase><words>" +
					"<word guid='" + word.Guid + "'>" +
						"<item type='txt' lang='en'>supercalifragilisticexpialidocious</item>" +
						"<item type='gls' lang='pt'>absurdo</item>" +
					"</word>" +
				"</words></phrase></phrases></paragraph></paragraphs></interlinear-text></document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			FDO.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(importedPara);

				// make sure we've added the expected word gloss
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var importedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var importedWord = importedAnalysis.Wordform;
				Assert.That(importedWord.Guid, Is.EqualTo(word.Guid));
				var at = new AnalysisTree(importedAnalysis);
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
				var importedGloss = at.Gloss;
				Assert.That(importedGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));

				/* NOTE: currently paragraphs are getting recreated, so we can't depend upon that ownership tree persisting after the import
				 *
				Assert.That(importedPara.Guid, Is.SameAs(para.Guid));
				var at = new AnalysisTree(para.Analyses.First());
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
				var gloss = at.Gloss;
				Assert.That(gloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));
				 */

				// make sure nothing has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.AreEqual(paraContents.Text, importedPara.Contents.Text, "Imported Para contents differ from original");
				Assert.IsTrue(paraContents.Equals(importedPara.Contents), "Ws mismatch between imported and original paragraph");
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

			FDO.IText text;

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
				para.Contents = Cache.TsStrFactory.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				paraContents = para.Contents;
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeTss("supercalifragilisticexpialidocious",
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
			FDO.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(importedPara);

				// make sure we've skipped the expected word gloss
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var skippedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var skippedWord = skippedAnalysis.Wordform;
				var at = new AnalysisTree(skippedAnalysis);
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
				var skippedGloss = at.Gloss;
				Assert.That(skippedGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absirdo"));
				Assert.That(skippedWord.Guid, Is.EqualTo(segGuid));

				// make sure nothing else has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.AreEqual(paraContents.Text, importedPara.Contents.Text, "Imported Para contents differ from original");
				Assert.IsTrue(paraContents.Equals(importedPara.Contents), "Ws mismatch between imported and original paragraph");
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

			FDO.IText text;

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
				para.Contents = Cache.TsStrFactory.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				paraContents = para.Contents;
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeTss("supercalifragilisticexpialidocious",
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
			FDO.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(importedPara);

				// assert that nothing was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));

				// make sure existing word gloss didn't change
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var skippedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var skippedWord = skippedAnalysis.Wordform;
				var at = new AnalysisTree(skippedAnalysis);
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
				var skippedGloss = at.Gloss;
				Assert.That(skippedGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));
				Assert.That(skippedWord.Guid, Is.EqualTo(word.Guid));

				// make sure nothing else has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.AreEqual(paraContents.Text, importedPara.Contents.Text, "Imported Para contents differ from original");
				Assert.IsTrue(paraContents.Equals(importedPara.Contents), "Ws mismatch between imported and original paragraph");
				Assert.That(skippedWord.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				Assert.That(skippedWord.Guid, Is.EqualTo(word.Guid));
			}
		}

		[Test]
		public void ImportNewUserConfirmedWordGlossSeparatedFromToExistingWfiAnalysis()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;

			FDO.IText text;

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
				para.Contents = Cache.TsStrFactory.MakeString("supercalifragilisticexpialidocious", wsf.get_Engine("en").Handle);
				paraContents = para.Contents;
				ISegment segment = sl.GetInstance<ISegmentFactory>().Create();
				para.SegmentsOS.Add(segment);
				ITsString wform = TsStringUtils.MakeTss("supercalifragilisticexpialidocious",
					wsf.get_Engine("en").Handle);
				segGuid = segment.Guid;
				word = sl.GetInstance<IWfiWordformFactory>().Create(wform);
				var newWfiAnalysis = sl.GetInstance<IWfiAnalysisFactory>().Create();
				word.AnalysesOC.Add(newWfiAnalysis);
				segment.AnalysesRS.Add(word);
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
			FDO.IText importedText = null;
			li.ImportInterlinear(options, ref importedText);
			using (var firstEntry = Cache.LanguageProject.Texts.GetEnumerator())
			{
				firstEntry.MoveNext();
				var imported = firstEntry.Current;
				Assert.IsNotNull(imported);
				var importedPara = imported.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				Assert.IsNotNull(importedPara);

				// assert that new Analysis was created
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count, Is.EqualTo(2));

				// make sure imported word gloss is correct
				Assert.That(importedPara.SegmentsOS[0].AnalysesRS.Count, Is.EqualTo(1));
				var importedAnalysis = importedPara.SegmentsOS[0].AnalysesRS[0];
				var skippedWord = importedAnalysis.Wordform;
				var at = new AnalysisTree(importedAnalysis);
				Assert.IsNotNull(at.Gloss, "IAnalysis should be WfiGloss");
				var newGloss = at.Gloss;
				Assert.That(newGloss.Form.get_String(wsf.get_Engine("pt").Handle).Text, Is.EqualTo("absurdo"));
				Assert.That(skippedWord.Guid, Is.EqualTo(word.Guid));

				// make sure nothing else has changed:
				Assert.That(Cache.LanguageProject.Texts.Count, Is.EqualTo(1));
				Assert.That(imported.ContentsOA.ParagraphsOS.Count, Is.EqualTo(1));
				Assert.AreEqual(paraContents.Text, importedPara.Contents.Text, "Imported Para contents differ from original");
				Assert.IsTrue(paraContents.Equals(importedPara.Contents), "Ws mismatch between imported and original paragraph");
				Assert.That(skippedWord.Form.get_String(wsf.get_Engine("en").Handle).Text,
					Is.EqualTo("supercalifragilisticexpialidocious"));
				Assert.That(skippedWord.Guid, Is.EqualTo(word.Guid));

				// make sure nothing else changed
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().Count, Is.EqualTo(1));
				Assert.That(Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().Count, Is.EqualTo(1));
			}
		}

		[Test, Ignore]
		public void ImportNewUserConfirmedWordGlossMergeIntoAnalysisMatchingStemOfExistingAnalysis()
		{

		}

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
			FDO.IText importedText = null;
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
				wf.Form.set_String(wsEn, Cache.TsStrFactory.MakeString("this is a phrase", wsEn));
			});
			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			var options = CreateImportInterlinearOptions(xml);
			FDO.IText importedText = null;
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
			FDO.IText importedText = null;
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
			Assert.DoesNotThrow(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss));
		}

		[Test]
		public void WordsFragDoc_OneWordAndOneGloss()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			IWritingSystem wsKal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsKal);

			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>glossedonce</item>
						<item type='gls' lang='en'>onlygloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			Assert.DoesNotThrow(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss));

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
			IWritingSystem wsKal;
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
			Assert.DoesNotThrow(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss));

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
			IWritingSystem wsKal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsKal);

			const string xml =
				@"<document>
					  <word>
						<item type='txt' lang='qaa-x-kal'>support a phrase</item>
						<item type='gls' lang='en'>phrase gloss</item>
					  </word>
				</document>";

			var li = new BIRDFormatImportTests.LLIMergeExtension(Cache, null, null);
			Assert.DoesNotThrow(() => li.ImportWordsFrag(
				() => new MemoryStream(Encoding.ASCII.GetBytes(xml.ToCharArray())),
				LinguaLinksImport.ImportAnalysesLevel.WordGloss));

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
