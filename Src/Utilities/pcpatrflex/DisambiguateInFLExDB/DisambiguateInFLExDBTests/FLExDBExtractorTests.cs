// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.WritingSystems;
using System.Reflection;
using System.IO;
using SIL.DisambiguateInFLExDB;
using SIL.LCModel.DomainServices;

namespace SIL.DisambiguateInFLExDBTests
{
	[TestFixture]
	class FLExDBExtractorTests : DisambiguateTests
	{
		String Lexicon { get; set; }

		public override void FixtureSetup()
		{
			//IcuInit();
			TestDirInit();
			TestFile = Path.Combine(TestDataDir, "PCPATRTestingMultiMorphemic.fwdata");
			SavedTestFile = Path.Combine(TestDataDir, "PCPATRTestingMultiMorphemicB4.fwdata");

			base.FixtureSetup();

			using (
				var streamReader = new StreamReader(
					Path.Combine(TestDataDir, "Lexicon.lex"),
					Encoding.UTF8
				)
			)
			{
				Lexicon = streamReader.ReadToEnd().Replace("\r", "");
			}
		}

		/// <summary></summary>
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
		}

		/// <summary>
		/// Test extracting of lexicon.
		/// </summary>
		[Test]
		public void ExtractLexiconTest()
		{
			//MyCache = Loader.CreateCache();
			Assert.IsNotNull(MyCache);
			Assert.AreEqual(ProjId.UiName, MyCache.ProjectId.UiName);
			Assert.AreEqual(26, MyCache.LangProject.AllPartsOfSpeech.Count);
			Assert.AreEqual(335, MyCache.LangProject.LexDbOA.Entries.Count());
			var extractor = new FLExDBExtractor(MyCache);
			String lexicon = extractor.ExtractPcPatrLexicon();
			//Console.Write(lexicon);
			Assert.AreEqual(Lexicon, lexicon);
		}

		[Test]
		public void IsAttachedCliticTest()
		{
			//MyCache = Loader.CreateCache();
			var extractor = new FLExDBExtractor(MyCache);
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphBoundRoot, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphBoundStem, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphCircumfix, 1));
			Assert.IsFalse(
				extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphDiscontiguousPhrase, 1)
			);
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphInfix, 1));
			Assert.IsFalse(
				extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphInfixingInterfix, 1)
			);
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphParticle, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphPhrase, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphPrefix, 1));
			Assert.IsFalse(
				extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphPrefixingInterfix, 1)
			);
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphRoot, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphSimulfix, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphStem, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphSuffix, 1));
			Assert.IsFalse(
				extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphSuffixingInterfix, 1)
			);
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphSuprafix, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphClitic, 1));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphClitic, 2));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphEnclitic, 1));
			Assert.IsTrue(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphEnclitic, 2));
			Assert.IsFalse(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphProclitic, 1));
			Assert.IsTrue(extractor.IsAttachedClitic(MoMorphTypeTags.kguidMorphProclitic, 2));
		}

		[Test]
		public void GetOrComputeWordCategoryTest()
		{
			//MyCache = Loader.CreateCache();
			var extractor = new FLExDBExtractor(MyCache);
			var wordCat = extractor.GetOrComputeWordCategory(null);
			Assert.AreEqual("", wordCat);

			var text = MyCache.LangProject.InterlinearTexts
				.Where(t => t.Title.BestAnalysisAlternative.Text == "Multi-morphemic")
				.First();
			var paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(1);
			var segment = paragraph.SegmentsOS.First();
			var analysis = segment.AnalysesRS.ElementAtOrDefault(3);
			var wordform = analysis.Wordform; // trees
			Assert.AreEqual(2, wordform.AnalysesOC.Count);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(0));
			Assert.AreEqual("n", wordCat);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(1));
			Assert.AreEqual("n", wordCat);
			analysis = segment.AnalysesRS.ElementAtOrDefault(7);
			wordform = analysis.Wordform; // booksi
			Assert.AreEqual(1, wordform.AnalysesOC.Count);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(0));
			Assert.AreEqual("n", wordCat);

			paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(2);
			segment = paragraph.SegmentsOS.First();
			analysis = segment.AnalysesRS.ElementAtOrDefault(0);
			wordform = analysis.Wordform; // the
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(0));
			Assert.AreEqual("art", wordCat);
			analysis = segment.AnalysesRS.ElementAtOrDefault(1);
			wordform = analysis.Wordform; // preturntables
			Assert.AreEqual(2, wordform.AnalysesOC.Count);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(0));
			Assert.AreEqual("n", wordCat);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(1));
			Assert.AreEqual("n", wordCat);
			analysis = segment.AnalysesRS.ElementAtOrDefault(2);
			wordform = analysis.Wordform; // are
			Assert.AreEqual(5, wordform.AnalysesOC.Count);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(0));
			Assert.AreEqual("v", wordCat);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(1));
			Assert.AreEqual("v", wordCat);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(2));
			Assert.AreEqual("v", wordCat);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(3));
			Assert.AreEqual("v", wordCat);
			wordCat = extractor.GetOrComputeWordCategory(wordform.AnalysesOC.ElementAtOrDefault(4));
			Assert.AreEqual("aux", wordCat);
		}

		/// <summary>
		/// Test extracting of text segments in ANA format.
		/// </summary>
		[Test]
		public void ExtractTextSegmentAsANATest()
		{
			//MyCache = Loader.CreateCache();
			Assert.IsNotNull(MyCache);
			Assert.AreEqual(ProjId.UiName, MyCache.ProjectId.UiName);
			Assert.AreEqual(26, MyCache.LangProject.AllPartsOfSpeech.Count);
			Assert.AreEqual(335, MyCache.LangProject.LexDbOA.Entries.Count());
			Assert.AreEqual(7, MyCache.LangProject.InterlinearTexts.Count);
			var extractor = new FLExDBExtractor(MyCache);
			var text = MyCache.LangProject.InterlinearTexts
				.Where(t => t.Title.BestAnalysisAlternative.Text == "Part 4")
				.First();
			var paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(3);
			var segment = paragraph.SegmentsOS.First();
			String segmentAsANA = extractor.ExtractTextSegmentAsANA(segment);
			String expectedANA = ExpectedSegmentAsANA("WeWantToGetMarriedAndBeHappy.ana");
			Assert.AreEqual(expectedANA, segmentAsANA);
			paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(7);
			segment = paragraph.SegmentsOS.First();
			segmentAsANA = extractor.ExtractTextSegmentAsANA(segment);
			expectedANA = ExpectedSegmentAsANA("ItIsHardToPickUpTheDullBrokenGlass.ana");
			Assert.AreEqual(expectedANA, segmentAsANA);

			text = MyCache.LangProject.InterlinearTexts
				.Where(t => t.Title.BestAnalysisAlternative.Text == "Multi-morphemic")
				.First();
			paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(0);
			segment = paragraph.SegmentsOS.First();
			segmentAsANA = extractor.ExtractTextSegmentAsANA(segment);
			expectedANA = ExpectedSegmentAsANA("ISeeTwoTrees.ana");
			//Console.WriteLine("ana='" + segmentAsANA + "'");
			Assert.AreEqual(expectedANA, segmentAsANA);
			paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(1);
			segment = paragraph.SegmentsOS.First();
			segmentAsANA = extractor.ExtractTextSegmentAsANA(segment);
			expectedANA = ExpectedSegmentAsANA("ISeeTheTreesColor.ana");
			//Console.WriteLine("ana='" + segmentAsANA + "'");
			Assert.AreEqual(expectedANA, segmentAsANA);
			paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(2);
			segment = paragraph.SegmentsOS.First();
			segmentAsANA = extractor.ExtractTextSegmentAsANA(segment);
			expectedANA = ExpectedSegmentAsANA("ThePreturntablesAreBetterThanTheProturntables.ana");
			//Console.WriteLine("ana='" + segmentAsANA + "'");
			Assert.AreEqual(expectedANA, segmentAsANA);
			paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(3);
			segment = paragraph.SegmentsOS.First();
			segmentAsANA = extractor.ExtractTextSegmentAsANA(segment);
			expectedANA = ExpectedSegmentAsANA("SiPro.ana");
			//Console.WriteLine("ana='" + segmentAsANA + "'");
			Assert.AreEqual(expectedANA, segmentAsANA);

			text = MyCache.LangProject.InterlinearTexts
				.Where(t => t.Title.BestAnalysisAlternative.Text == "Testing for dual-sense bug")
				.First();
			paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(0);
			segment = paragraph.SegmentsOS.First();
			segmentAsANA = extractor.ExtractTextSegmentAsANA(segment);
			expectedANA = ExpectedSegmentAsANA("BahBook.ana");
			//Console.WriteLine("ana='" + segmentAsANA + "'");
			Assert.AreEqual(expectedANA, segmentAsANA);
		}

		private String ExpectedSegmentAsANA(String segmentFileName)
		{
			String result;
			using (
				var streamReader = new StreamReader(
					Path.Combine(TestDataDir, segmentFileName),
					Encoding.UTF8
				)
			)
			{
				result = streamReader.ReadToEnd().Replace("\r", "");
			}
			return result;
		}
	}
}
