// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeStVcTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;

using NUnit.Framework;
using Rhino.Mocks;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	#region DummyTeStVc class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy TeStVc class that provides access to the methods we want to test
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyTeStVc: TeStVc
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyTeStVc(FdoCache cache, int defaultWs) : this(cache, defaultWs, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyTeStVc(FdoCache cache, int defaultWs, IVwRootBox rootb) :
			base(TeStVc.LayoutViewTarget.targetDraft, -1)
		{
			Cache = cache;
			DefaultWs = defaultWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// provides access to the "m_DispPropOverrides" field
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<DispPropOverride> PropOverrides
		{
			get { return m_DispPropOverrides; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to the "InsertParaContentsUserPrompt" method.
		/// </summary>
		/// <param name="vwEnv"></param>
		/// <param name="para"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool CallInsertUserPrompt(IVwEnv vwEnv, IStTxtPara para)
		{
			return InsertParaContentsUserPrompt(vwEnv, para.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// provide access to the "InsertTranslationUserPrompt" method
		/// </summary>
		/// <param name="vwEnv"></param>
		/// <param name="trans"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool CallInsertBackTranslationUserPrompt(IVwEnv vwEnv, ICmTranslation trans)
		{
			return InsertTranslationUserPrompt(vwEnv, trans);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// privides access to the "MakeDispPropOverrides" method
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="paraMinHighlight">The para min highlight.</param>
		/// <param name="paraLimHighlight">The para lim highlight.</param>
		/// <param name="initializer">The initializer.</param>
		/// ------------------------------------------------------------------------------------
		public void CallMakeDispPropOverrides(IScrTxtPara para, int paraMinHighlight, int paraLimHighlight,
			DispPropInitializer initializer)
		{
			MakeDispPropOverrides(para, paraMinHighlight, paraLimHighlight, initializer);
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for TeStVc
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeStVcTests : ScrInMemoryFdoTestBase
	{
		#region Data Members
		private IVwEnv m_vwenvMock;
		private IScrBook m_book;
		private bool m_oldPromptSetting;

		private string typeInt = "System.Int32";
		#endregion

		#region Setup and Teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override of TestSetup to clear book filters between tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			// Set up the FDO and populate it with some values
			m_book = AddBookToMockedScripture(57, "Philemon");

			// Set up IVwEnv object
			m_vwenvMock = MockRepository.GenerateMock<IVwEnv>();
			m_vwenvMock.Stub(x => x.DataAccess).Return(Cache.DomainDataByFlid);

			// save settings
			m_oldPromptSetting = Options.ShowEmptyParagraphPromptsSetting;
		}

		/// <summary>
		///
		/// </summary>
		public override void TestTearDown()
		{
			Options.ShowEmptyParagraphPromptsSetting = m_oldPromptSetting;
			m_book = null;
			m_vwenvMock = null;
			base.TestTearDown();
		}


		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the text we expect to be added to the view in place of the empty paragraph
		/// </summary>
		/// <param name="vc">view constructor to use</param>
		/// <param name="hvo">hvo of the object to create a prompt for</param>
		/// <param name="frag">fragment id indicating the type of user prompt</param>
		/// <param name="flid">field id for the view system note dependency</param>
		/// ------------------------------------------------------------------------------------
		private void CreateExpectedUserPrompt(TeStVc vc, int hvo, int frag, int flid)
		{
			// Set expectations
			m_vwenvMock.Expect(x => x.NoteDependency(new int[] { hvo }, new int[] { flid }, 1));
			m_vwenvMock.Expect(x => x.AddProp(SimpleRootSite.kTagUserPrompt, vc, frag));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the expected user prompt string
		/// </summary>
		/// <param name="text">text for the prompt</param>
		/// <param name="ws">writing system to use for the ZWS character</param>
		/// ------------------------------------------------------------------------------------
		private ITsString ExpectedUserPrompt(string text, int ws)
		{
			// Set up the text we expect to be added to the paragraph
			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.LightGray));
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.WritingSystemFactory.UserWs);
			ttpBldr.SetIntPropValues(SimpleRootSite.ktptUserPrompt,
				(int)FwTextPropVar.ktpvDefault, 1);
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptSpellCheck,
				(int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, text, ttpBldr.GetTextProps());

			ITsPropsBldr ttpBldr2 = TsPropsBldrClass.Create();
			ttpBldr2.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, ws);
			ttpBldr2.SetIntPropValues(SimpleRootSite.ktptUserPrompt,
				(int)FwTextPropVar.ktpvDefault, 1);
			strBldr.Replace(0, 0, "\u200B", ttpBldr2.GetTextProps());

			return strBldr.GetString();
		}
		#endregion

		#region MakeDispPropOverrides tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MakeDispPropOverrides method with invalid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MakeDispPropOverrides_invalidValues_BeyondParaBounds()
		{
			IStText text = AddTitleToMockedBook(m_book, "Title text");

			DummyTeStVc stVc = new DummyTeStVc(Cache, Cache.DefaultVernWs);
				Assert.AreEqual(0, stVc.PropOverrides.Count);
				stVc.CallMakeDispPropOverrides((IScrTxtPara)text[0], -1, 11,
						 delegate(ref DispPropOverride prop)
						 {
							 // Nothing to do
						 });
				Assert.AreEqual(1, stVc.PropOverrides.Count);
				Assert.AreEqual(0, stVc.PropOverrides[0].ichMin);
				Assert.AreEqual(10, stVc.PropOverrides[0].ichLim);
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MakeDispPropOverrides method with invalid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MakeDispPropOverrides_invalidValues_BeyondParaBounds2()
		{
			IStText text = AddTitleToMockedBook(m_book, "Title text");

			DummyTeStVc stVc = new DummyTeStVc(Cache, Cache.DefaultVernWs);
				Assert.AreEqual(0, stVc.PropOverrides.Count);
				stVc.CallMakeDispPropOverrides((IScrTxtPara)text[0], 11, 11,
					delegate(ref DispPropOverride prop)
					{
					// Nothing to do
					});
				Assert.AreEqual(1, stVc.PropOverrides.Count);
				Assert.AreEqual(10, stVc.PropOverrides[0].ichMin);
				Assert.AreEqual(10, stVc.PropOverrides[0].ichLim);
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MakeDispPropOverrides method with invalid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void MakeDispPropOverrides_invalidValues_ReversedMinLim()
		{
			IStText text = AddTitleToMockedBook(m_book, "Title text");

			DummyTeStVc stVc = new DummyTeStVc(Cache, Cache.DefaultVernWs);
				Assert.AreEqual(0, stVc.PropOverrides.Count);
				stVc.CallMakeDispPropOverrides((IScrTxtPara)text[0], 8, 2,
					delegate(ref DispPropOverride prop)
					{
					// Nothing to do
				});
			}
		#endregion

		#region User prompt tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt gets added to an empty Book Title paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptForBookTitleWithEmptyPara()
		{
			// Set up title with an empty paragraph
			IStText title = AddTitleToMockedBook(m_book, string.Empty, Cache.DefaultVernWs);
			Options.ShowEmptyParagraphPromptsSetting = true;

			int defVernWs = Cache.DefaultVernWs;
			DummyTeStVc stVc = new DummyTeStVc(Cache, defVernWs);
				CreateExpectedUserPrompt(stVc, title.ParagraphsOS[0].Hvo,
					ScrBookTags.kflidTitle, StTxtParaTags.kflidContents);

				m_vwenvMock.Stub(x => x.CurrentObject()).Return(title.ParagraphsOS[0].Hvo);
				bool fTextAdded = stVc.CallInsertUserPrompt(m_vwenvMock, title[0]);

				Assert.IsTrue(fTextAdded, "User prompt not added");
				ITsString text = stVc.DisplayVariant(m_vwenvMock, SimpleRootSite.kTagUserPrompt,
					ScrBookTags.kflidTitle);

				string difference;
				bool fEqual = TsStringHelper.TsStringsAreEqual(
					ExpectedUserPrompt("Type book title for Philemon here", defVernWs),
					text, out difference);
				Assert.IsTrue(fEqual, difference);
				m_vwenvMock.VerifyAllExpectations();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt gets added to an empty section head paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptForSectionHeadWithEmptyPara()
		{
			// Set up section head with an empty paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddSectionHeadParaToSection(section, "",
				 ScrStyleNames.SectionHead);
			Options.ShowEmptyParagraphPromptsSetting = true;
			int defVernWs = Cache.DefaultVernWs;
			DummyTeStVc stVc = new DummyTeStVc(Cache, defVernWs);
				CreateExpectedUserPrompt(stVc, para.Hvo,
					ScrSectionTags.kflidHeading, StTxtParaTags.kflidContents);

				bool fTextAdded = stVc.CallInsertUserPrompt(m_vwenvMock, para);

				Assert.IsTrue(fTextAdded, "User prompt not added");
				ITsString text = stVc.DisplayVariant(m_vwenvMock, SimpleRootSite.kTagUserPrompt,
					ScrSectionTags.kflidHeading);

				string difference;
				bool fEqual = TsStringHelper.TsStringsAreEqual(
					ExpectedUserPrompt("Type section head here", defVernWs),
					text, out difference);
				Assert.IsTrue(fEqual, difference);
				m_vwenvMock.VerifyAllExpectations();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt in the non-segmented back translation is replaced
		/// properly when the user types over it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateUserPrompt_NormalBT_Typing()
		{
			// Set up section head with an empty paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddSectionHeadParaToSection(section, "",
				ScrStyleNames.SectionHead);

			int defAnalWs = Cache.DefaultAnalWs;
			IVwRootBox rootb;
			IVwSelection vwsel;
			IVwRootSite rootsite;
			SetUpResultsForUpdateUserPromptTests(5, "b", out rootb, out vwsel, out rootsite);

			DummyTeStVc stVc = new DummyTeStVc(Cache, defAnalWs, rootb);
				ITsIncStrBldr strBdlr = TsIncStrBldrClass.Create();
				strBdlr.SetIntPropValues(SimpleRootSite.ktptUserPrompt, (int)FwTextPropVar.ktpvDefault, 1);
				strBdlr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultUserWs);
				strBdlr.Append("b");
				ITsString tssTyped = strBdlr.GetString();
				ITsString tssExpected = StringUtils.MakeTss("b", defAnalWs);

				// Now simulate the user typing over the user prompt
				ICmTranslation bt = para.GetOrCreateBT();
				stVc.UpdateProp(vwsel, bt.Hvo, SimpleRootSite.kTagUserPrompt, CmTranslationTags.kflidTranslation, tssTyped);

				// verify that the text is in the paragraph and that there is no longer a user prompt.
				string diff;
				Assert.IsTrue(TsStringHelper.TsStringsAreEqual(tssExpected, bt.Translation.get_String(defAnalWs), out diff), diff);

				m_vwenvMock.VerifyAllExpectations();
				VerifyArgsSentToRequestSelectionAtEndOfUow(rootsite, rootb, defAnalWs, 5, CmTranslationTags.kflidTranslation, "b");
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt is replaced properly when the user types over it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateUserPrompt_Vern_Typing()
		{
			// Set up section head with an empty paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddSectionHeadParaToSection(section, "",
				ScrStyleNames.SectionHead);

			IVwRootBox rootb;
			IVwSelection vwsel;
			IVwRootSite rootsite;
			SetUpResultsForUpdateUserPromptTests(4, "t", out rootb, out vwsel, out rootsite);

			int defVernWs = Cache.DefaultVernWs;

			DummyTeStVc stVc = new DummyTeStVc(Cache, defVernWs, rootb);
				ITsIncStrBldr strBdlr = TsIncStrBldrClass.Create();
				strBdlr.SetIntPropValues(SimpleRootSite.ktptUserPrompt, (int)FwTextPropVar.ktpvDefault, 1);
				strBdlr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultUserWs);
				strBdlr.Append("t");
				ITsString tssTyped = strBdlr.GetString();
				ITsString tssExpected = StringUtils.MakeTss("t", defVernWs);

				// Now simulate the user typing over the user prompt
				stVc.UpdateProp(vwsel, para.Hvo, SimpleRootSite.kTagUserPrompt, 0, tssTyped);

				// verify that the text is in the paragraph and that there is no longer a user prompt.
				string diff;
				Assert.IsTrue(TsStringHelper.TsStringsAreEqual(tssExpected, para.Contents, out diff), diff);

				m_vwenvMock.VerifyAllExpectations();
				VerifyArgsSentToRequestSelectionAtEndOfUow(rootsite, rootb, 0, 4, StTxtParaTags.kflidContents, "t");
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt is replaced properly when the user replaces it by
		/// pasting in the back translation. We check that the writing system gets set
		/// correctly, but the character formatting remains (TE-3429).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateUserPrompt_NormalBT_Pasting()
		{
			// Set up section head with an empty paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddSectionHeadParaToSection(section, "", ScrStyleNames.SectionHead);

			int defAnalWs = Cache.DefaultAnalWs;
			IVwRootBox rootb;
			IVwSelection vwsel;
			IVwRootSite rootsite;
			SetUpResultsForUpdateUserPromptTests(5,	"TE2ST", out rootb, out vwsel, out rootsite);

			DummyTeStVc stVc = new DummyTeStVc(Cache, defAnalWs, rootb);
				// set up the text to paste - will be TE2ST with vernacular WS
				ITsPropsFactory propFact = TsPropsFactoryClass.Create();
				ITsTextProps ttp = propFact.MakeProps(null, defAnalWs, 0);

				ITsStrBldr bldr = TsStrBldrClass.Create();
				bldr.ReplaceRgch(0, 0, "TEST", 4, ttp);
				ttp = propFact.MakeProps(ScrStyleNames.VerseNumber, defAnalWs, 0);
				bldr.ReplaceRgch(2, 2, "2", 1, ttp);
				ITsString tssPasted = bldr.GetString();

				// Now simulate the user pasting over the user prompt
				ICmTranslation bt = para.GetOrCreateBT();
				stVc.UpdateProp(vwsel, bt.Hvo, SimpleRootSite.kTagUserPrompt, CmTranslationTags.kflidTranslation, tssPasted);

				// Verify that the text is in the paragraph and that the character formatting survives.
				AssertEx.AreTsStringsEqual(tssPasted, bt.Translation.get_String(defAnalWs));

				m_vwenvMock.VerifyAllExpectations();
				VerifyArgsSentToRequestSelectionAtEndOfUow(rootsite, rootb, defAnalWs, 5, CmTranslationTags.kflidTranslation, "TE2ST");
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt gets added to an empty section head paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptForSectionHeadWithEmptyPara_NoOption()
		{
			// Set up section head with an empty paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddSectionHeadParaToSection(section, "", ScrStyleNames.SectionHead);

			Options.ShowEmptyParagraphPromptsSetting = false;

			DummyTeStVc stVc = new DummyTeStVc(Cache, Cache.DefaultVernWs);
				bool fTextAdded = stVc.CallInsertUserPrompt(m_vwenvMock, para);

				Assert.IsFalse(fTextAdded, "User prompt added");
				m_vwenvMock.VerifyAllExpectations();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that no user prompt are added to an empty paragraph that is not a book title
		/// or a section head paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoUserPromptForContentPara()
		{
			// Set up empty content paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);

			DummyTeStVc stVc = new DummyTeStVc(Cache, Cache.DefaultVernWs);
				bool fTextAdded = stVc.CallInsertUserPrompt(m_vwenvMock, para);

				Assert.IsFalse(fTextAdded, "User prompt was added to empty content para");

				m_vwenvMock.AssertWasNotCalled(x => x.NoteDependency(Arg<int[]>.Is.Anything, Arg<int[]>.Is.Anything, Arg<int>.Is.Anything));
				m_vwenvMock.AssertWasNotCalled(x => x.AddProp(Arg<int>.Is.Anything, Arg<IVwViewConstructor>.Is.Anything, Arg<int>.Is.Anything));
				m_vwenvMock.VerifyAllExpectations();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt gets added to an empty back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptForBackTranslation()
		{
			// Set up a section and paragraph with text
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			AddRunToMockedPara(para, "Some paragraph text.", null);

			// Add an empty translation to the paragraph
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(para, wsBT);
			AddRunToMockedTrans(trans, wsBT, string.Empty, null);

			DummyTeStVc stVc = new DummyTeStVc(Cache, wsBT);
				CreateExpectedUserPrompt(stVc, trans.Hvo, CmTranslationTags.kflidTranslation,
					CmTranslationTags.kflidTranslation);

				// verify that the prompt gets added
				bool fTextAdded = stVc.CallInsertBackTranslationUserPrompt(m_vwenvMock, trans);
				Assert.IsTrue(fTextAdded, "User prompt not added");

				// verify the contents of the prompt
				ITsString text = stVc.DisplayVariant(m_vwenvMock, SimpleRootSite.kTagUserPrompt,
					CmTranslationTags.kflidTranslation);
				string difference;
				bool fEqual = TsStringHelper.TsStringsAreEqual(
					ExpectedUserPrompt("Type back translation here", wsBT),
					text, out difference);
				Assert.IsTrue(fEqual, difference);

				// verify the mock - is this useful?
				m_vwenvMock.VerifyAllExpectations();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt does NOT get added to a back translation which
		/// already has text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoUserPromptForBTWithText()
		{
			// Set up a section and paragraph with text
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			AddRunToMockedPara(para, "Some paragraph text.", null);

			// Add a translation to the paragraph. The mock for the AltString provides text by default.
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(para, wsBT);
			AddRunToMockedTrans(trans, wsBT, "this is text in the BT", null);

			DummyTeStVc stVc = new DummyTeStVc(Cache, wsBT);
				// verify that the prompt does not get added
				bool fTextAdded = stVc.CallInsertBackTranslationUserPrompt(m_vwenvMock, trans);
				Assert.IsFalse(fTextAdded, "User prompt added when it should not have been");

				m_vwenvMock.AssertWasNotCalled(x => x.NoteDependency(Arg<int[]>.Is.Anything, Arg<int[]>.Is.Anything, Arg<int>.Is.Anything));
				m_vwenvMock.AssertWasNotCalled(x => x.AddProp(Arg<int>.Is.Anything, Arg<IVwViewConstructor>.Is.Anything, Arg<int>.Is.Anything));
				m_vwenvMock.VerifyAllExpectations();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt does not get added to an empty back translation when the
		/// owning paragraph is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoUserPromptForBTWithEmptyPara()
		{
			// Set up a section and empty paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			// Add an empty translation to the paragraph
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(para, wsBT);
			AddRunToMockedTrans(trans, wsBT, string.Empty, null);

			DummyTeStVc stVc = new DummyTeStVc(Cache, wsBT);
				// verify that the prompt does not get added
				bool fTextAdded = stVc.CallInsertBackTranslationUserPrompt(m_vwenvMock, trans);
				Assert.IsFalse(fTextAdded, "User prompt was added and should not have been");

				m_vwenvMock.AssertWasNotCalled(x => x.NoteDependency(Arg<int[]>.Is.Anything, Arg<int[]>.Is.Anything, Arg<int>.Is.Anything));
				m_vwenvMock.AssertWasNotCalled(x => x.AddProp(Arg<int>.Is.Anything, Arg<IVwViewConstructor>.Is.Anything, Arg<int>.Is.Anything));
				m_vwenvMock.VerifyAllExpectations();
			}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up results for user prompt change tests (mostly creates and sets up mock
		/// objects to stub out methods).
		/// </summary>
		/// <param name="cLevels">The count of selection levels.</param>
		/// <param name="text">The text that was typed or pasted to replace the prompt.</param>
		/// <param name="rootb">The (mocked) root box.</param>
		/// <param name="vwsel">The (mocked) vw selection.</param>
		/// <param name="rootsite">The (mocked) rootsite.</param>
		/// ------------------------------------------------------------------------------------
		private static void SetUpResultsForUpdateUserPromptTests(int cLevels, string text,
			out IVwRootBox rootb, out IVwSelection vwsel, out IVwRootSite rootsite)
		{
			Options.ShowEmptyParagraphPromptsSetting = true;
			IVwRootBox rootbox = rootb = MockRepository.GenerateMock<IVwRootBox>();
			rootb.Stub(x => x.IsCompositionInProgress).Return(false);
			rootsite = MockRepository.GenerateMock<IVwRootSite>();
			rootb.Stub(x => x.Site).Return(rootsite);
			vwsel = MockRepository.GenerateMock<IVwSelection>();
			vwsel.Stub(x => x.RootBox).Return(rootb);
			// The number of levels CLevels reports includes one for the string property, so we add that here (production code subtracts it)
			vwsel.Stub(x => x.CLevels(Arg<bool>.Is.Anything)).Return(cLevels + 1);
			vwsel.Stub(x => x.AllTextSelInfo(out Arg<int>.Out(0).Dummy, Arg<int>.Is.Equal(cLevels),
				Arg<ArrayPtr>.Is.Anything, out Arg<int>.Out(0).Dummy, out Arg<int>.Out(0).Dummy,
				out Arg<int>.Out(0).Dummy, out Arg<int>.Out(text.Length).Dummy, out Arg<int>.Out(0).Dummy,
				out Arg<bool>.Out(false).Dummy, out Arg<int>.Out(0).Dummy,
				out Arg<ITsTextProps>.Out(null).Dummy));
#if DEBUG
			vwsel.Stub(x => x.IsValid).Return(true);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the args sent to the RequestSelectionAtEndOfUow method.
		/// </summary>
		/// <param name="rootsite">The (mocked) rootsite on which RequestSelectionAtEndOfUow
		/// should have been called.</param>
		/// <param name="rootb">The (mocked) rootbox.</param>
		/// <param name="wsAlt">The ws of the back translation (or 0 if vernacular).</param>
		/// <param name="cLevels">The expected number of selection levels.</param>
		/// <param name="tagTextProp">The expected text prop flid.</param>
		/// <param name="text">The text that was typed or pasted to replace the prompt.</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyArgsSentToRequestSelectionAtEndOfUow(IVwRootSite rootsite,
			IVwRootBox rootb, int wsAlt, int cLevels, int tagTextProp, string text)
		{
			IList<object[]> argsSentToRequestSelectionAtEndOfUow =
				rootsite.GetArgumentsForCallsMadeOn(rs => rs.RequestSelectionAtEndOfUow(Arg<IVwRootBox>.Is.Anything,
				Arg<int>.Is.Anything, Arg<int>.Is.Anything, Arg<SelLevInfo[]>.Is.Anything,
				Arg<int>.Is.Anything, Arg<int>.Is.Anything, Arg<int>.Is.Anything, Arg<int>.Is.Anything,
				Arg<bool>.Is.Anything, Arg<ITsTextProps>.Is.Anything));
			Assert.AreEqual(1, argsSentToRequestSelectionAtEndOfUow.Count);
			Assert.AreEqual(10, argsSentToRequestSelectionAtEndOfUow[0].Length);
			Assert.AreEqual(rootb, argsSentToRequestSelectionAtEndOfUow[0][0]);
			Assert.AreEqual(0, argsSentToRequestSelectionAtEndOfUow[0][1]);
			Assert.AreEqual(cLevels, argsSentToRequestSelectionAtEndOfUow[0][2]);
			SelLevInfo[] levInfo = (SelLevInfo[])argsSentToRequestSelectionAtEndOfUow[0][3];
			Assert.AreEqual(cLevels, levInfo.Length);
			Assert.AreEqual(tagTextProp, argsSentToRequestSelectionAtEndOfUow[0][4]);
			Assert.AreEqual(0, argsSentToRequestSelectionAtEndOfUow[0][5]);
			Assert.AreEqual(text.Length, argsSentToRequestSelectionAtEndOfUow[0][6]);
			Assert.AreEqual(wsAlt, argsSentToRequestSelectionAtEndOfUow[0][7]);
			Assert.AreEqual(true, argsSentToRequestSelectionAtEndOfUow[0][8]);
			ITsTextProps props = (ITsTextProps)argsSentToRequestSelectionAtEndOfUow[0][9];
			Assert.AreEqual(1, props.IntPropCount);
			Assert.AreEqual(0, props.StrPropCount);
		}
		#endregion
	}
}
