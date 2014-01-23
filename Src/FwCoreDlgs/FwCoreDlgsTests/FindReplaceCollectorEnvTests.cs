// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FindReplaceCollectorEnvTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System.Collections.Generic;
using System.Runtime.InteropServices; // needed for Marshal
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FindReplaceCollectorEnvTests: ScrInMemoryFdoTestBase
	{
		#region Dummy View Constructor
		///  ----------------------------------------------------------------------------------------
		/// <summary>
		/// Possible scripture fragments
		/// </summary>
		///  ----------------------------------------------------------------------------------------
		public enum ScrFrags : int
		{
			/// <summary>Scripture</summary>
			kfrScripture = 111,
			/// <summary>A book</summary>
			kfrBook,
			/// <summary>A section</summary>
			kfrSection,
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// View constructor used to "display" scripture. This is similar to DraftViewVc to
		/// which we don't have access here in our tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DummyScriptureVc : StVc
		{
			private List<int> m_hvosReadOnly = new List<int>();

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:DummyScriptureVc"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DummyScriptureVc() : this(null)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:DummyScriptureVc"/> class.
			/// </summary>
			/// <param name="hvosReadOnly">list of read-only HVOs.</param>
			/// --------------------------------------------------------------------------------
			public DummyScriptureVc(params int[] hvosReadOnly)
			{
				m_hvosReadOnly = new List<int>(hvosReadOnly);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This is the main interesting method of displaying objects and fragments of them.
			/// A Scripture is displayed by displaying its Books;
			/// and a Book is displayed by displaying its Title and Sections;
			/// and a Section is displayed by displaying its Heading and Content;
			/// which are displayed by using the standard view constructor for StText.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="hvo"></param>
			/// <param name="frag"></param>
			/// ------------------------------------------------------------------------------------
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case (int)ScrFrags.kfrScripture:
						vwenv.AddLazyVecItems(ScriptureTags.kflidScriptureBooks,
							this, (int)ScrFrags.kfrBook);
						break;
					case (int)ScrFrags.kfrBook:
						vwenv.OpenDiv();
						vwenv.AddObjProp(ScrBookTags.kflidTitle, this,
							(int)StTextFrags.kfrText);
						vwenv.AddLazyVecItems(ScrBookTags.kflidSections, this,
							(int)ScrFrags.kfrSection);
						vwenv.CloseDiv();
						break;
					case (int)ScrFrags.kfrSection:
						vwenv.OpenDiv();
						vwenv.AddObjProp(ScrSectionTags.kflidHeading, this,
							(int)StTextFrags.kfrText);
						vwenv.AddObjProp(ScrSectionTags.kflidContent, this,
							(int)StTextFrags.kfrText);
						vwenv.CloseDiv();
						break;
					case (int)StTextFrags.kfrPara:
						if (m_hvosReadOnly.Contains(hvo))
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
								(int)FwTextPropVar.ktpvEnum,
								(int)TptEditable.ktptNotEditable);
						}
						base.Display(vwenv, hvo, frag);
						break;
					default:
						base.Display(vwenv, hvo, frag);
						break;
				}
			}
		}
		#endregion // Dummy View Constructor

		#region Data members
		private IScrSection m_section;
		private IStTxtPara m_para1;
		private IStTxtPara m_para2;
		private IStTxtPara m_para3;
		private StVc m_vc;
		private IVwPattern m_pattern;
		private ITsStrFactory m_strFactory;

		// Member that simulates our current "selection"
		private FindCollectorEnv.LocationInfo m_sel;
		#endregion

		#region Setup/Teardown/Initialize
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
			m_strFactory = TsStrFactoryClass.Create();

			m_pattern.Pattern = m_strFactory.MakeString("a", Cache.DefaultVernWs);
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
			if (Marshal.IsComObject(m_strFactory))
				Marshal.ReleaseComObject(m_strFactory);
			m_strFactory = null;


			base.TestTearDown();
		}
		#endregion

		#region ReplaceAll tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests replacing all
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceAll()
		{
			m_pattern.ReplaceWith = m_strFactory.MakeString("b", Cache.DefaultVernWs);

			ReplaceAllCollectorEnv collectorEnv = new ReplaceAllCollectorEnv(m_vc,
				Cache.MainCacheAccessor, m_para1.Owner.Hvo, (int)StTextFrags.kfrText,
				m_pattern, null);
			int nReplaces = collectorEnv.ReplaceAll();

			Assert.AreEqual(8, nReplaces);
			Assert.AreEqual("This is some text so thbt we cbn test the find functionblity.",
				m_para1.Contents.Text);
			Assert.AreEqual("Some more text so thbt we cbn test the find bnd replbce functionblity.",
				m_para2.Contents.Text);
			Assert.AreEqual("This purugruph doesn't contuin the first letter of the ulphubet.",
				m_para3.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests replacing all with a more complex replace string with two runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceAllWithMultipleRuns()
		{
			ITsStrBldr stringBuilder = TsStrBldrClass.Create();
			stringBuilder.Replace(0, 0, "blaBLA", null);
			stringBuilder.SetIntPropValues(0, 6, (int)FwTextPropType.ktptWs, 0, Cache.DefaultVernWs);
			stringBuilder.SetStrPropValue(0, 3, (int)FwTextPropType.ktptNamedStyle, "CStyle3");
			m_pattern.ReplaceWith = stringBuilder.GetString();

			ReplaceAllCollectorEnv collectorEnv = new ReplaceAllCollectorEnv(m_vc,
				Cache.MainCacheAccessor, m_para1.Owner.Hvo, (int)StTextFrags.kfrText,
				m_pattern, null);
			int nReplaces = collectorEnv.ReplaceAll();

			Assert.AreEqual(8, nReplaces);
			Assert.AreEqual("This is some text so thblaBLAt we cblaBLAn test the find functionblaBLAlity.",
				m_para1.Contents.Text);
			Assert.AreEqual("Some more text so thblaBLAt we cblaBLAn test the find blaBLAnd replblaBLAce functionblaBLAlity.",
				m_para2.Contents.Text);
			Assert.AreEqual("This purugruph doesn't contuin the first letter of the ulphubet.",
				m_para3.Contents.Text);
			ITsString para1Contents = m_para1.Contents;
			Assert.AreEqual(7, para1Contents.RunCount);
			Assert.AreEqual("bla", para1Contents.get_RunText(1));
			Assert.AreEqual("CStyle3", para1Contents.get_Properties(1).GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests replacing all when text contains read-only substrings. Replace should not
		/// replace those.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceAllWithReadOnly()
		{
			m_vc = new DummyScriptureVc(m_para1.Hvo);
			m_vc.Cache = Cache;
			m_pattern.ReplaceWith = m_strFactory.MakeString("b", Cache.DefaultVernWs);

			ReplaceAllCollectorEnv collectorEnv = new ReplaceAllCollectorEnv(m_vc,
				Cache.MainCacheAccessor, m_para1.Owner.Hvo, (int)StTextFrags.kfrText,
				m_pattern, null);
			int nReplaces = collectorEnv.ReplaceAll();

			Assert.AreEqual(5, nReplaces);
			Assert.AreEqual("This is some text so that we can test the find functionality.",
				m_para1.Contents.Text);
			Assert.AreEqual("Some more text so thbt we cbn test the find bnd replbce functionblity.",
				m_para2.Contents.Text);
			Assert.AreEqual("This purugruph doesn't contuin the first letter of the ulphubet.",
				m_para3.Contents.Text);
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
			FindCollectorEnv collectorEnv = new FindCollectorEnv(m_vc,
				Cache.MainCacheAccessor, m_para1.Owner.Hvo, (int)StTextFrags.kfrText,
				m_pattern, null);

			// Start at the top
			SelLevInfo[] levInfo = new SelLevInfo[1];
			levInfo[0].hvo = m_para1.Hvo;
			levInfo[0].tag = StTextTags.kflidParagraphs;
			m_sel = new FindCollectorEnv.LocationInfo(levInfo, StTxtParaTags.kflidContents, 0);

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding text starting in the middle of the "view"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Find_FromMiddle()
		{
			FindCollectorEnv collectorEnv = new FindCollectorEnv(m_vc,
				Cache.DomainDataByFlid, m_para1.Owner.Hvo, (int)StTextFrags.kfrText,
				m_pattern, null);

			// Start in the middle
			SelLevInfo[] levInfo = new SelLevInfo[1];
			levInfo[0].hvo = m_para2.Hvo;
			levInfo[0].tag = StTextTags.kflidParagraphs;
			m_sel = new FindCollectorEnv.LocationInfo(levInfo, StTxtParaTags.kflidContents, 5);

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
			FindCollectorEnv.LocationInfo foundLocation = collectorEnv.FindNext(m_sel);
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
