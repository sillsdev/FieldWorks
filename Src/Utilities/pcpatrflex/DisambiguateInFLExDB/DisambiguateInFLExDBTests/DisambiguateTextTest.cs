// Copyright (c) 2018 SIL International
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
	class DisambiguateTextTests : DisambiguateTests
	{
		string[] TextPart3;

		public override void FixtureSetup()
		{
			//IcuInit();
			TestDirInit();
			TestFile = "PCPATRTesting4Text.fwdata";
			SavedTestFile = "PCPATRTesting4TextB4.fwdata";

			base.FixtureSetup();
			TextPart3 = new string[]
			{
				"",
				"e2e4949d-9af0-4142-9d4f-f2d9afdcb646\n750f0e3f-ddab-495b-b91d-da1eb4eea68d\n1ea23f59-f6d9-406d-89f6-792318a04efe\n",
				"",
				"",
				"e2e4949d-9af0-4142-9d4f-f2d9afdcb646\nb3e8623e-5679-4261-acd5-d62ed71d1d2b\n9be2d38f-bc3a-4e96-acb5-64d2b3e53d95\n04f021dc-a0dd-44fc-8b0a-9e6741743dd8\n1ea23f59-f6d9-406d-89f6-792318a04efe\nb3854054-5a37-4072-8c3d-35896dc0286b\n479aca02-ca6a-4c2a-862a-d980fbcc9a37\n07fbf262-bbe7-415b-af3f-8317a2cb4521\n",
				"",
				"7841d0ff-57f0-4a2c-a689-6d109efca66e\n728ba8cd-c8c2-4911-81bf-645622c0a3c8\n4a1d23f6-c387-4956-b594-de9fa7ba22ed\ndb63e48e-690e-4bd6-9717-8c6486aa14e4\nac219243-83d7-4b12-8f3f-f759001a8e03\n10dfbbc4-a5ac-4027-a11c-1e5dccf87e07\n9be2d38f-bc3a-4e96-acb5-64d2b3e53d95\nbabe0bf3-206c-48b0-9c5d-faf71d1c87f7\nbbc1751c-a2ea-4b4c-82d7-d4b1d5f80a42\n",
				"",
				"",
				"",
				"",
				"",
				"",
				"",
				"",
				"",
				""
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
		public void DisambiguateTextTest()
		{
			//MyCache = Loader.CreateCache();
			Assert.IsNotNull(MyCache);
			Assert.AreEqual(ProjId.UiName, MyCache.ProjectId.UiName);
			Assert.AreEqual(26, MyCache.LangProject.AllPartsOfSpeech.Count);
			Assert.AreEqual(323, MyCache.LangProject.LexDbOA.Entries.Count());
			Assert.AreEqual(4, MyCache.LangProject.InterlinearTexts.Count);

			var text = MyCache.LangProject.InterlinearTexts
				.Where(t => t.Title.BestAnalysisAlternative.Text == "Part 4")
				.First();
			var itext = text.Owner as IText;
			string AndFile = Path.Combine(TestDataDir, "Text.and");
			var textDisam = new TextDisambiguation(itext, TextPart3, AndFile);
			var defaultAgent = MyCache.LanguageProject.DefaultUserAgent;

			// Before disambiguation
			Assert.AreEqual(18, text.ParagraphsOS.Count);
			var para = text.ParagraphsOS.ElementAtOrDefault(1) as IStTxtPara;
			Assert.NotNull(para);
			var segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			var analysis = segment.AnalysesRS.ElementAt(0); // we
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // got
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(2); // married
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // punctuation
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			para = text.ParagraphsOS.ElementAtOrDefault(2) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			analysis = segment.AnalysesRS.ElementAt(0); // are (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(1); // they
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // married
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // punctuation
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			para = text.ParagraphsOS.ElementAtOrDefault(4) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			analysis = segment.AnalysesRS.ElementAt(0); // we
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // want
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // to (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(3); // be (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(4); // married
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(5); // and
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(6); // healthy
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(7); // and
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(8); // happy
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(9); // punctuation
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			para = text.ParagraphsOS.ElementAtOrDefault(16) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			analysis = segment.AnalysesRS.ElementAt(0); // we
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // got (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(2); // to (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(3); // go
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(4); // to (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(5); // the
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(6); // party
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(7); // punctuation
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			textDisam.Disambiguate(MyCache);

			//After disambiguation
			para = text.ParagraphsOS.ElementAtOrDefault(1) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			analysis = segment.AnalysesRS.ElementAt(0); // we
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // got
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // married
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // punctuation
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			para = text.ParagraphsOS.ElementAtOrDefault(2) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			analysis = segment.AnalysesRS.ElementAt(0); // are
			Assert.AreEqual(WfiAnalysisTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // they
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // married
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // punctuation
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			para = text.ParagraphsOS.ElementAtOrDefault(4) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			analysis = segment.AnalysesRS.ElementAt(0); // we
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // want
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // to
			Assert.AreEqual(WfiAnalysisTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // be
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(4); // married
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(5); // and
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(6); // healthy
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(7); // and
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(8); // happy
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(9); // punctuation
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);

			para = text.ParagraphsOS.ElementAtOrDefault(16) as IStTxtPara;
			Assert.NotNull(para);
			segment = para.SegmentsOS.FirstOrDefault();
			Assert.NotNull(segment);
			analysis = segment.AnalysesRS.ElementAt(0); // we
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // got
			Assert.AreEqual(WfiAnalysisTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // to
			Assert.AreEqual(WfiAnalysisTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // go
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(4); // to
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(5); // the
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(6); // party
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(7); // punctuation
			Assert.AreEqual(PunctuationFormTags.kClassId, analysis.ClassID);
		}
	}
}
