// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Runtime.InteropServices;
using LanguageExplorer.Impls;
// needed for Marshal
using NUnit.Framework;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.WritingSystems;

namespace LanguageExplorerTests.Impls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FindCollectorEnvTests: ScrInMemoryLcmTestBase
	{
		#region Data members
		private IScrSection m_section;
		private IStTxtPara m_para1;
		private IStTxtPara m_para2;
		private IStTxtPara m_para3;
		private StVc m_vc;
		private IVwPattern m_pattern;

		// Member that simulates our current "selection"
		private CollectorEnv.LocationInfo m_sel;
		#endregion

		#region Setup/Teardown/Initialize
		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}
			base.FixtureSetup();
		}

		public override void FixtureTeardown()
		{
			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the up a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_vc = new StVc();
			m_vc.Cache = Cache;

			m_pattern = VwPatternClass.Create();

			m_pattern.Pattern = TsStringUtils.MakeString("a", Cache.DefaultVernWs);
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;

			IScrBook genesis = AddBookWithTwoSections(1, "Genesis");
			m_section = genesis.SectionsOS[0];
			// Add paragraphs (because we use an StVc in the test we add them all to the same section)
			m_para1 = AddParaToMockedSectionContent(m_section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(m_para1,
				"This is some text so that we can test the find functionality.", null);
			m_para2 = AddParaToMockedSectionContent(m_section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(m_para2,
				"Some more text so that we can test the find and replace functionality.", null);
			m_para3 = AddParaToMockedSectionContent(
				m_section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(m_para3,
				"This purugruph doesn't contuin the first letter of the ulphubet.", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_vc = null;
			m_section = null;
			m_para1 = null;
			m_para2 = null;
			m_para3 = null;
			if (Marshal.IsComObject(m_pattern))
				Marshal.ReleaseComObject(m_pattern);
			m_pattern = null;

			base.TestTearDown();
		}
		#endregion

		#region Find tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding text starting at the top of the "view"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Find_FromTop()
		{
			using (var collectorEnv = new FindCollectorEnv(m_vc,
				Cache.MainCacheAccessor, m_para1.Owner.Hvo, (int) StTextFrags.kfrText,
				m_pattern, null))
			{

				// Start at the top
				SelLevInfo[] levInfo = new SelLevInfo[1];
				levInfo[0].hvo = m_para1.Hvo;
				levInfo[0].tag = StTextTags.kflidParagraphs;
				m_sel = new CollectorEnv.LocationInfo(levInfo, StTxtParaTags.kflidContents, 0);

				VerifyFindNext(collectorEnv, m_para1.Hvo, 23, 24);
				VerifyFindNext(collectorEnv, m_para1.Hvo, 30, 31);
				VerifyFindNext(collectorEnv, m_para1.Hvo, 55, 56);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 20, 21);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 27, 28);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 44, 45);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 52, 53);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 64, 65);
				Assert.IsNull(collectorEnv.FindNext(m_sel));

				// Make sure nothing got replaced by accident.
				Assert.AreEqual("This is some text so that we can test the find functionality.",
					m_para1.Contents.Text);
				Assert.AreEqual("Some more text so that we can test the find and replace functionality.",
					m_para2.Contents.Text);
				Assert.AreEqual("This purugruph doesn't contuin the first letter of the ulphubet.",
					m_para3.Contents.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding text starting in the middle of the "view"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Find_FromMiddle()
		{
			using (var collectorEnv = new FindCollectorEnv(m_vc,
				Cache.DomainDataByFlid, m_para1.Owner.Hvo, (int) StTextFrags.kfrText,
				m_pattern, null))
			{

				// Start in the middle
				SelLevInfo[] levInfo = new SelLevInfo[1];
				levInfo[0].hvo = m_para2.Hvo;
				levInfo[0].tag = StTextTags.kflidParagraphs;
				m_sel = new CollectorEnv.LocationInfo(levInfo, StTxtParaTags.kflidContents, 5);

				VerifyFindNext(collectorEnv, m_para2.Hvo, 20, 21);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 27, 28);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 44, 45);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 52, 53);
				VerifyFindNext(collectorEnv, m_para2.Hvo, 64, 65);
				Assert.IsNull(collectorEnv.FindNext(m_sel));

				// Make sure nothing got replaced by accident.
				Assert.AreEqual("This is some text so that we can test the find functionality.",
					m_para1.Contents.Text);
				Assert.AreEqual("Some more text so that we can test the find and replace functionality.",
					m_para2.Contents.Text);
				Assert.AreEqual("This purugruph doesn't contuin the first letter of the ulphubet.",
					m_para3.Contents.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the find next.
		/// </summary>
		/// <param name="collectorEnv">The collector env.</param>
		/// <param name="hvoExpected">The hvo expected.</param>
		/// <param name="ichMinExpected">The ich min expected.</param>
		/// <param name="ichLimExpected">The ich lim expected.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyFindNext(FindCollectorEnv collectorEnv, int hvoExpected,
			int ichMinExpected, int ichLimExpected)
		{
			CollectorEnv.LocationInfo foundLocation = collectorEnv.FindNext(m_sel);
			Assert.IsNotNull(foundLocation);
			Assert.AreEqual(1, foundLocation.m_location.Length);
			Assert.AreEqual(hvoExpected, foundLocation.TopLevelHvo);
			Assert.AreEqual(StTextTags.kflidParagraphs, foundLocation.m_location[0].tag);
			Assert.AreEqual(StTxtParaTags.kflidContents, foundLocation.m_tag);
			Assert.AreEqual(ichMinExpected, foundLocation.m_ichMin);
			Assert.AreEqual(ichLimExpected, foundLocation.m_ichLim);
			m_sel = foundLocation;
		}
		#endregion
	}
}
