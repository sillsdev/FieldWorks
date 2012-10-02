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
using System.Diagnostics;
using System.Runtime.InteropServices;

using NUnit.Framework;
using NMock;
using NMock.Constraints;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.FwUtils;

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
		/// <param name="hvo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool CallInsertUserPrompt(IVwEnv vwEnv, int hvo)
		{
			CheckDisposed();

			return InsertParaContentsUserPrompt(vwEnv, hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// provide access to the "InsertTranslationUserPrompt" method
		/// </summary>
		/// <param name="vwEnv"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool CallInsertBackTranslationUserPrompt(IVwEnv vwEnv, int hvo)
		{
			CheckDisposed();

			return InsertTranslationUserPrompt(vwEnv, hvo);
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
		public void CallMakeDispPropOverrides(StTxtPara para, int paraMinHighlight, int paraLimHighlight,
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
	public class TeStVcTests : BaseTest
	{
		#region Data Members
		private ScrInMemoryFdoCache m_inMemoryCache;
		private DynamicMock m_vwenvMock;
		private ITsString m_emptyTsString;
		private IScrBook m_book;
		private bool m_oldPromptSetting;

		private string typeInt = "System.Int32";
		#endregion

		#region Setup and Teardown
		#region IDisposable override
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)

			{
				// Dispose managed resources here.
				if (m_inMemoryCache != null)
					m_inMemoryCache.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			if (m_emptyTsString != null)

			{
				Marshal.ReleaseComObject(m_emptyTsString);
				m_emptyTsString = null;
			}
			m_vwenvMock = null;
			m_inMemoryCache = null;
			m_book = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			CheckDisposed();

			if (m_emptyTsString != null)
				Marshal.ReleaseComObject(m_emptyTsString);
			// Create an empty TsString
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, string.Empty, null);
			m_emptyTsString = bldr.GetString();

			// Set up the FDO mock and populate it with some values
			m_inMemoryCache = ScrInMemoryFdoCache.Create();
			m_inMemoryCache.InitializeLangProject();
			m_inMemoryCache.InitializeScripture();
			m_book = m_inMemoryCache.AddBookToMockedScripture(57, "Philemon");

			// Set up IVwEnv object
			m_vwenvMock = new DynamicMock(typeof(IVwEnv));
			m_vwenvMock.SetupResult("DataAccess", m_inMemoryCache.CacheAccessor);

			// save settings
			m_oldPromptSetting = Options.ShowEmptyParagraphPromptsSetting;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			CheckDisposed();

			Options.ShowEmptyParagraphPromptsSetting = m_oldPromptSetting;
			if (m_emptyTsString != null)
				Marshal.ReleaseComObject(m_emptyTsString);
			m_inMemoryCache.Dispose();
			m_inMemoryCache = null;
			m_book = null;
			m_vwenvMock = null;
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
			m_vwenvMock.Expect("NoteDependency", new int[] { hvo },
				new int[] { (int)flid }, 1);
			m_vwenvMock.Expect("AddProp", new object[] { SimpleRootSite.kTagUserPrompt, vc, frag},
				new string[] { typeInt, typeof(IVwViewConstructor).FullName, typeInt });
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
				(int)SIL.FieldWorks.Common.Utils.ColorUtil.ConvertColorToBGR(Color.LightGray));
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_inMemoryCache.Cache.DefaultUserWs);
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
			IStText text = m_inMemoryCache.AddTitleToMockedBook(m_book.Hvo, "Title text");

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultVernWs);
			Assert.AreEqual(0, stVc.PropOverrides.Count);
			stVc.CallMakeDispPropOverrides((StTxtPara)text.ParagraphsOS[0], -1, 11,
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
			IStText text = m_inMemoryCache.AddTitleToMockedBook(m_book.Hvo, "Title text");

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultVernWs);
			Assert.AreEqual(0, stVc.PropOverrides.Count);
			stVc.CallMakeDispPropOverrides((StTxtPara)text.ParagraphsOS[0], 11, 11,
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
			IStText text = m_inMemoryCache.AddTitleToMockedBook(m_book.Hvo, "Title text");

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultVernWs);
			Assert.AreEqual(0, stVc.PropOverrides.Count);
			stVc.CallMakeDispPropOverrides((StTxtPara)text.ParagraphsOS[0], 8, 2,
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
			CheckDisposed();

			// Set up title with an empty paragraph
			StText title = m_inMemoryCache.AddTitleToMockedBook(m_book.Hvo, m_emptyTsString,
				StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle));
			Options.ShowEmptyParagraphPromptsSetting = true;

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultVernWs);

			CreateExpectedUserPrompt(stVc, title.ParagraphsOS.FirstItem.Hvo,
				(int)ScrBook.ScrBookTags.kflidTitle, (int)StTxtPara.StTxtParaTags.kflidContents);

			m_vwenvMock.ExpectAndReturn("CurrentObject", title.ParagraphsOS.FirstItem.Hvo);
			IVwEnv vwEnv = (IVwEnv)m_vwenvMock.MockInstance;
			bool fTextAdded = stVc.CallInsertUserPrompt(vwEnv,
				title.ParagraphsOS.FirstItem.Hvo);

			Assert.IsTrue(fTextAdded, "User prompt not added");
			ITsString text = stVc.DisplayVariant(vwEnv, SimpleRootSite.kTagUserPrompt, null,
				(int)ScrBook.ScrBookTags.kflidTitle);

			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(
				ExpectedUserPrompt("Type book title for Philemon here", m_inMemoryCache.Cache.DefaultVernWs),
				text, out difference);
			Assert.IsTrue(fEqual, difference);
			m_vwenvMock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt gets added to an empty section head paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptForSectionHeadWithEmptyPara()
		{
			CheckDisposed();

			// Set up section head with an empty paragraph
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_inMemoryCache.AddSectionHeadParaToSection(section.Hvo, "",
				 ScrStyleNames.SectionHead);
			section.AdjustReferences();
			Options.ShowEmptyParagraphPromptsSetting = true;

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultVernWs);

			CreateExpectedUserPrompt(stVc, para.Hvo,
				(int)ScrSection.ScrSectionTags.kflidHeading, (int)StTxtPara.StTxtParaTags.kflidContents);

			IVwEnv vwEnv = (IVwEnv)m_vwenvMock.MockInstance;
			bool fTextAdded = stVc.CallInsertUserPrompt(vwEnv, para.Hvo);

			Assert.IsTrue(fTextAdded, "User prompt not added");
			ITsString text = stVc.DisplayVariant(vwEnv, SimpleRootSite.kTagUserPrompt, null,
				(int)ScrSection.ScrSectionTags.kflidHeading);

			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(
				ExpectedUserPrompt("Type section head here", m_inMemoryCache.Cache.DefaultVernWs),
				text, out difference);
			Assert.IsTrue(fEqual, difference);
			m_vwenvMock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt is replaced properly when the user types over it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptChangeWSWhenTyping()
		{
			CheckDisposed();

			// Set up section head with an empty paragraph
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_inMemoryCache.AddSectionHeadParaToSection(section.Hvo, "",
				ScrStyleNames.SectionHead);
			section.AdjustReferences();

			Options.ShowEmptyParagraphPromptsSetting = true;
			DynamicMock rootb = new DynamicMock(typeof(IVwRootBox));
			rootb.SetupResult("IsCompositionInProgress", false);
			DynamicMock vwsel = new DynamicMock(typeof(IVwSelection));
			IVwRootBox mockRootbox = (IVwRootBox)rootb.MockInstance;
			vwsel.SetupResult("RootBox", mockRootbox);
			vwsel.SetupResult("CLevels", 4, typeof(bool));
			vwsel.Ignore("AllTextSelInfo");
#if DEBUG
			vwsel.SetupResult("IsValid", true);
#endif

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultAnalWs, mockRootbox);
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tssVal;
			tssVal = tsf.MakeString("TEST", m_inMemoryCache.Cache.DefaultVernWs);

			// Now simulate the user typing over the user prompt
			stVc.UpdateProp((IVwSelection)vwsel.MockInstance, para.Hvo, SimpleRootSite.kTagUserPrompt, 0, tssVal);

			// verify that the text is in the paragraph and that there is no longer a user prompt.
			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_inMemoryCache.Cache.DefaultAnalWs);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "TEST", ttpBldr.GetTextProps());
			string diff;
			Assert.IsTrue(TsStringHelper.TsStringsAreEqual(bldr.GetString(),
				para.Contents.UnderlyingTsString, out diff), diff);

			m_vwenvMock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt is replaced properly when the user types over it. We also
		/// check that the writing system gets set correctly, but the character formatting
		/// remains (TE-3429).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptChangeWSWhenPasting()
		{
			CheckDisposed();

			// Set up section head with an empty paragraph
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_inMemoryCache.AddSectionHeadParaToSection(section.Hvo, "",
				ScrStyleNames.SectionHead);
			section.AdjustReferences();

			Options.ShowEmptyParagraphPromptsSetting = true;
			DynamicMock rootb = new DynamicMock(typeof(IVwRootBox));
			rootb.SetupResult("IsCompositionInProgress", false);
			DynamicMock vwsel = new DynamicMock(typeof(IVwSelection));
			IVwRootBox mockRootbox = (IVwRootBox)rootb.MockInstance;
			vwsel.SetupResult("RootBox", mockRootbox);
			vwsel.SetupResult("CLevels", 4, typeof(bool));
			vwsel.Ignore("AllTextSelInfo");
#if DEBUG
			vwsel.SetupResult("IsValid", true);
#endif

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache,
				m_inMemoryCache.Cache.DefaultAnalWs, mockRootbox);

			// set up the text to paste - will be TE2ST with vernacular WS
			int ws = m_inMemoryCache.Cache.DefaultVernWs;
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps ttp = propFact.MakeProps(null, ws, 0);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.ReplaceRgch(0, 0, "TEST", 4, ttp);
			ttp = propFact.MakeProps(ScrStyleNames.VerseNumber, ws, 0);
			bldr.ReplaceRgch(2, 2, "2", 1, ttp);
			ITsString tssVal = bldr.GetString();

			// Now simulate the user pasting over the user prompt
			stVc.UpdateProp((IVwSelection)vwsel.MockInstance, para.Hvo,
				SimpleRootSite.kTagUserPrompt, 0, tssVal);

			// verify that the text is in the paragraph, that there is no longer a user
			// prompt, and that the ws changed but the character formatting survives.
			bldr = tssVal.GetBldr();
			bldr.SetIntPropValues(0, 5, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_inMemoryCache.Cache.DefaultAnalWs);
			AssertEx.AreTsStringsEqual(bldr.GetString(), para.Contents.UnderlyingTsString);

			m_vwenvMock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt gets added to an empty section head paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptForSectionHeadWithEmptyPara_NoOption()
		{
			CheckDisposed();

			// Set up section head with an empty paragraph
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_inMemoryCache.AddSectionHeadParaToSection(section.Hvo, "",
				ScrStyleNames.SectionHead);
			section.AdjustReferences();

			Options.ShowEmptyParagraphPromptsSetting = false;

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultVernWs);

			IVwEnv vwEnv = (IVwEnv)m_vwenvMock.MockInstance;
			bool fTextAdded = stVc.CallInsertUserPrompt(vwEnv, para.Hvo);

			Assert.IsFalse(fTextAdded, "User prompt added");
			m_vwenvMock.Verify();
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
			CheckDisposed();

			// Set up empty content paragraph
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			section.AdjustReferences();

			m_vwenvMock.ExpectNoCall("NoteDependency", new string[] { typeof(int[]).FullName,
																		typeof(int[]).FullName, typeInt });
			m_vwenvMock.ExpectNoCall("AddProp", new string[] { typeInt,
																 typeof(IVwViewConstructor).FullName, typeInt });

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultVernWs);
			bool fTextAdded = stVc.CallInsertUserPrompt((IVwEnv)m_vwenvMock.MockInstance,
				para.Hvo);

			Assert.IsFalse(fTextAdded, "User prompt was added to empty content para");
			m_vwenvMock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user prompt gets added to an empty back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UserPromptForBackTranslation()
		{
			CheckDisposed();

			// Set up a section and paragraph with text
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			section.AdjustReferences();

			m_inMemoryCache.AddRunToMockedPara(para, "Some paragraph text.", null);

			// Add an empty translation to the paragraph
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, string.Empty, null);

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultAnalWs);

			CreateExpectedUserPrompt(stVc, trans.Hvo,
				(int)CmTranslation.CmTranslationTags.kflidTranslation,
				(int)CmTranslation.CmTranslationTags.kflidTranslation);

			// verify that the prompt gets added
			IVwEnv vwEnv = (IVwEnv)m_vwenvMock.MockInstance;
			bool fTextAdded = stVc.CallInsertBackTranslationUserPrompt(vwEnv, trans.Hvo);
			Assert.IsTrue(fTextAdded, "User prompt not added");

			// verify the contents of the prompt
			ITsString text = stVc.DisplayVariant(vwEnv, SimpleRootSite.kTagUserPrompt, null,
				(int)CmTranslation.CmTranslationTags.kflidTranslation);
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(
				ExpectedUserPrompt("Type back translation here", m_inMemoryCache.Cache.DefaultAnalWs),
				text, out difference);
			Assert.IsTrue(fEqual, difference);

			// verify the mock - is this useful?
			m_vwenvMock.Verify();
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
			CheckDisposed();

			// Set up a section and paragraph with text
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			section.AdjustReferences();

			m_inMemoryCache.AddRunToMockedPara(para, "Some paragraph text.", null);

			// Add a translation to the paragraph. The mock for the AltString provides text by default.
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, "this is text in the BT", null);

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultAnalWs);

			m_vwenvMock.ExpectNoCall("NoteDependency", new string[] { typeof(int[]).FullName,
				typeof(int[]).FullName, typeInt });
			m_vwenvMock.ExpectNoCall("AddProp", new string[] { typeInt,
				 typeof(IVwViewConstructor).FullName, typeInt });

			// verify that the prompt does not get added
			IVwEnv vwEnv = (IVwEnv)m_vwenvMock.MockInstance;
			bool fTextAdded = stVc.CallInsertBackTranslationUserPrompt(vwEnv, trans.Hvo);
			Assert.IsFalse(fTextAdded, "User prompt added when it should not have been");

			m_vwenvMock.Verify();
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
			CheckDisposed();

			// Set up a section and empty paragraph
			IScrSection section = m_inMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_inMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			section.AdjustReferences();

			// Add an empty translation to the paragraph
			int wsBT = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsBT);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBT, string.Empty, null);

			DummyTeStVc stVc = new DummyTeStVc(m_inMemoryCache.Cache, m_inMemoryCache.Cache.DefaultAnalWs);

			m_vwenvMock.ExpectNoCall("NoteDependency", new string[] { typeof(int[]).FullName,
				typeof(int[]).FullName, typeInt });
			m_vwenvMock.ExpectNoCall("AddProp", new string[] { typeInt,
				typeof(IVwViewConstructor).FullName, typeInt });

			// verify that the prompt does not get added
			IVwEnv vwEnv = (IVwEnv)m_vwenvMock.MockInstance;
			bool fTextAdded = stVc.CallInsertBackTranslationUserPrompt(vwEnv, trans.Hvo);
			Assert.IsFalse(fTextAdded, "User prompt was added and should not have been");

			// verify the mock
			m_vwenvMock.Verify();
		}
		#endregion
	}
}
