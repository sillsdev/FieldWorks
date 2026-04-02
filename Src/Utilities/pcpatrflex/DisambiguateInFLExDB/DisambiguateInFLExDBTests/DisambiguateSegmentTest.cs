// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.DisambiguateInFLExDB;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.WritingSystems;
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
	class DisambiguateSegmentTests : DisambiguateTests
	{
		List<Guid> MorphBundleGuidsWeWantToGetMarriedAndBeHappy { get; set; }

		public override void FixtureSetup()
		{
			//IcuInit();
			TestDirInit();
			base.FixtureSetup();
			MorphBundleGuidsWeWantToGetMarriedAndBeHappy = new List<Guid>()
			{
				new Guid("e2e4949d-9af0-4142-9d4f-f2d9afdcb646"),
				new Guid("b3e8623e-5679-4261-acd5-d62ed71d1d2b"),
				new Guid("2f63f58f-112b-46ab-b329-3dc85ffda392"),
				new Guid("8ac47da9-e3dd-4645-9ef6-507b4f9dc802"),
				new Guid("1ea23f59-f6d9-406d-89f6-792318a04efe"),
				new Guid("479aca02-ca6a-4c2a-862a-d980fbcc9a37"),
				new Guid("04f021dc-a0dd-44fc-8b0a-9e6741743dd8"),
				new Guid("07fbf262-bbe7-415b-af3f-8317a2cb4521")
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
		public void DisambiguateSegmentTest()
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
			var paragraph = (IStTxtPara)text.ParagraphsOS.ElementAt(3);
			var segment = paragraph.SegmentsOS.First();
			var segmentDisam = new SegmentDisambiguation(
				segment,
				MorphBundleGuidsWeWantToGetMarriedAndBeHappy
			);
			var defaultAgent = MyCache.LanguageProject.DefaultUserAgent;

			// Before disambiguation
			Assert.AreEqual(9, segment.AnalysesRS.Count);
			var analysis = segment.AnalysesRS.ElementAt(0); // we
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // want
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // to (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(3); // get (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(4); // married
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(5); // and
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(6); // be (ambiguous)
			Assert.AreEqual(WfiWordformTags.kClassId, analysis.ClassID);
			Assert.IsNull(analysis.Analysis);
			analysis = segment.AnalysesRS.ElementAt(7); // happy
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));

			segmentDisam.Disambiguate(MyCache);
			//After disambiguation
			Assert.AreEqual(9, segment.AnalysesRS.Count);
			analysis = segment.AnalysesRS.ElementAt(0); // we
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(1); // want
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(2); // to (ambiguous)
			Assert.AreEqual(WfiAnalysisTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(3); // get (ambiguous)
			Assert.AreEqual(WfiAnalysisTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(4); // married
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(5); // and
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(6); // be (ambiguous)
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
			analysis = segment.AnalysesRS.ElementAt(7); // happy
			Assert.AreEqual(WfiGlossTags.kClassId, analysis.ClassID);
			Assert.AreEqual(Opinions.approves, analysis.Analysis.GetAgentOpinion(defaultAgent));
		}

		/// <summary>
		/// Test disambiguating segment in a text
		/// </summary>
		[Test]
		public void EnsureMorphBundleHasSenseTest()
		{
			//MyCache = Loader.CreateCache();
			Assert.IsNotNull(MyCache);
			Assert.AreEqual(ProjId.UiName, MyCache.ProjectId.UiName);
			Guid wfiMorphBundleGuidToUse = new Guid("01e77f47-a055-4053-8640-18d27f672085");
			var bundle =
				MyCache.ServiceLocator.ObjectRepository.GetObject(wfiMorphBundleGuidToUse)
				as IWfiMorphBundle;
			Assert.IsNotNull(bundle);
			Assert.IsNull(bundle.SenseRA);
			NonUndoableUnitOfWorkHelper.Do(
				MyCache.ActionHandlerAccessor,
				() =>
				{
					var segmentDisam = new SegmentDisambiguation(null, null);
					segmentDisam.EnsureMorphBundleHasSense(bundle);
					Assert.IsNotNull(bundle.SenseRA);
					Assert.AreEqual(
						"run-PST-MOT",
						bundle.SenseRA.Gloss.AnalysisDefaultWritingSystem.Text
					);
				}
			);
		}
	}
}
