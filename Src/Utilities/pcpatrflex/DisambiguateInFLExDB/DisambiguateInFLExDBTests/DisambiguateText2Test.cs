// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.DisambiguateInFLExDB;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDBTests
{
	[TestFixture]
	class DisambiguateText2Tests : DisambiguateTests
	{
		string[] Text2Testing;

		public override void FixtureSetup()
		{
			//IcuInit();
			TestDirInit();
			TestFile = Path.Combine(TestDataDir, "PCPATRTestingMultiSegmentInitPunc.fwdata");
			SavedTestFile = Path.Combine(TestDataDir, "PCPATRTestingMultiSegmentInitPuncB4.fwdata");

			base.FixtureSetup();
			Text2Testing = new string[]
			{
				"\n\nfd888928-590d-44b4-8b53-496d8cd35f83\n\na8b32926-8a2a-42bd-a233-80d5a1162652\n\n\n",
				"\n26f38286-c43f-488e-a23c-d31f094e5067\n99339961-85c4-4520-93df-3c5ff681c47f\n\n",
				"39029f5c-c055-4756-a1a3-6af4fa7718a8\n08dd96ac-5431-47c8-9776-50fdb9328466\n54c867f9-c8b7-4bfd-ab05-4f9b70863fbb\n1808aceb-e3f3-4709-ab0f-2bd2780e70c9\ne7b1a7de-8b4f-41fc-bb42-afa9d447903b\n",
				"\n410b4af8-77dc-4acb-930d-f5299d849e7a\n\n",
			};
		}

		/// <summary></summary>
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
		}

		/// <summary>
		/// Test disambiguating segment in a text
		/// </summary>
		[Test]
		public void DisambiguateText2Test()
		{
			//MyCache = Loader.CreateCache();
			Assert.IsNotNull(MyCache);
			Assert.AreEqual(ProjId.UiName, MyCache.ProjectId.UiName);
			Assert.AreEqual(30, MyCache.LangProject.AllPartsOfSpeech.Count);
			Assert.AreEqual(10912, MyCache.LangProject.LexDbOA.Entries.Count());
			Assert.AreEqual(119, MyCache.LangProject.InterlinearTexts.Count);

			var text = MyCache.LangProject.InterlinearTexts
				.Where(t => t.Title.BestAnalysisAlternative.Text == "PCPATR 3 Ron's testing")
				.First();
			var itext = text.Owner as IText;
			String AndFile = Path.Combine(TestDataDir, "Text2b.and");
			var textDisam = new TextDisambiguation(itext, Text2Testing, AndFile);
			var defaultAgent = MyCache.LanguageProject.DefaultUserAgent;

			// Before disambiguation
			Assert.AreEqual(2, text.ParagraphsOS.Count);
			var para = text.ParagraphsOS.ElementAtOrDefault(0) as IStTxtPara;
			Assert.NotNull(para);
			Assert.AreEqual(3, para.SegmentsOS.Count);

			var segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			Assert.AreEqual(7, segment.AnalysesRS.Count);
			var analysis = segment.AnalysesRS.ElementAt(0); // "
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(1); // '
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(2); // سلام
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.AreEqual("سلام", analysis.ShortName);
			analysis = segment.AnalysesRS.ElementAt(3); // ،
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(4); // گفتم
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.AreEqual("گفتم", analysis.ShortName);
			analysis = segment.AnalysesRS.ElementAt(5); // '"
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(6); // ..
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			segment = para.SegmentsOS.ElementAtOrDefault(1);
			Assert.AreEqual(4, segment.AnalysesRS.Count);
			analysis = segment.AnalysesRS.ElementAt(0); // :.
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(1); // چطور
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.AreEqual("چطور", analysis.ShortName);
			analysis = segment.AnalysesRS.ElementAt(2); // هستید
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.AreEqual("هستید", analysis.ShortName);
			analysis = segment.AnalysesRS.ElementAt(3); // ؟
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			// there is one more segment but no need to test for it

			para = text.ParagraphsOS.ElementAtOrDefault(1) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			Assert.AreEqual(3, segment.AnalysesRS.Count);
			analysis = segment.AnalysesRS.ElementAt(0); // ،
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(1); // باش
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.AreEqual("باش", analysis.ShortName);
			analysis = segment.AnalysesRS.ElementAt(2); // !
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			textDisam.Disambiguate(MyCache);

			//After disambiguation
			para = text.ParagraphsOS.ElementAtOrDefault(0) as IStTxtPara;
			Assert.NotNull(para);
			Assert.AreEqual(3, para.SegmentsOS.Count);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);

			Assert.AreEqual(7, segment.AnalysesRS.Count);
			analysis = segment.AnalysesRS.ElementAt(0); // "
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(1); // '
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(2); // سلام
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual("hello", analysis.ShortName);
			Assert.NotNull(analysis.Analysis);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // ،
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(4); // گفتم
			Assert.AreEqual(WfiAnalysisTags.kClassId, analysis.ClassID);
			Assert.AreEqual("e96cb86b-eb82-42de-bb2d-8662bd736c48", analysis.Guid.ToString());
			analysis = segment.AnalysesRS.ElementAt(5); // '"
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(6); // ..
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			segment = para.SegmentsOS.ElementAtOrDefault(1);
			Assert.AreEqual(4, segment.AnalysesRS.Count);
			analysis = segment.AnalysesRS.ElementAt(0); // :.
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(1); // چطور
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual("how", analysis.ShortName);
			Assert.NotNull(analysis.Analysis);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // هستید
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual("you are", analysis.ShortName);
			Assert.NotNull(analysis.Analysis);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // ؟
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			para = text.ParagraphsOS.ElementAtOrDefault(1) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			Assert.AreEqual(3, segment.AnalysesRS.Count);
			analysis = segment.AnalysesRS.ElementAt(0); // ،
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
			analysis = segment.AnalysesRS.ElementAt(1); // باش
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual("be!", analysis.ShortName);
			Assert.NotNull(analysis.Analysis);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // !
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
		}
	}
}
