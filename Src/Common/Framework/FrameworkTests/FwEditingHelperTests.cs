// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwEditingHelperTests.cs
// Responsibility: FW Team

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using SIL.FieldWorks.Common.RootSites;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwEditingHelperTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Data members
		private Mock<IEditingCallbacks> m_callbacksMock;
		private Mock<IVwRootSite> m_rootsiteMock;
		private Mock<IVwRootBox> m_rootboxMock;
		private Mock<IVwGraphics> m_vgMock;
		private IEditingCallbacks m_callbacks;
		private IVwRootSite m_rootsite;
		private IVwRootBox m_rootbox;
		private IVwGraphics m_vg;
		private ITsTextProps m_ttpHyperlink, m_ttpNormal;
		#endregion

		private enum IchPosition
		{
			StartOfString,
			StartOfHyperlink,
			EndOfHyperlink,
			EndOfString,
			EarlyInHyperlink,
			LateInHyperlink,
			InTextBeforeHyperlink,
			InTextAfterHyperlink,
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_callbacksMock = new Mock<IEditingCallbacks>();
			m_rootsiteMock = new Mock<IVwRootSite>();
			m_rootboxMock = new Mock<IVwRootBox>();
			m_vgMock = new Mock<IVwGraphics>();

			m_callbacks = m_callbacksMock.Object;
			m_rootsite = m_rootsiteMock.Object;
			m_rootbox = m_rootboxMock.Object;
			m_vg = m_vgMock.Object;

			m_callbacksMock.Setup(x => x.EditedRootBox).Returns(m_rootbox);
			m_rootboxMock.Setup(rbox => rbox.Site).Returns(m_rootsite);
			m_rootboxMock.SetupProperty(rbox => rbox.DataAccess, new Mock<ISilDataAccess>().Object);
			// For GetGraphics with out parameters, we use Callback
			IVwGraphics outVg = m_vg;
			Rect outRcSrcRoot = new Rect();
			Rect outRcDstRoot = new Rect();
			m_rootsiteMock.Setup(site => site.GetGraphics(
				It.IsAny<IVwRootBox>(),
				out outVg,
				out outRcSrcRoot,
				out outRcDstRoot));

			ITsPropsBldr ttpBldr = TsStringUtils.MakePropsBldr();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, -1, 911);
			m_ttpNormal = ttpBldr.GetTextProps();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Hyperlink");
			char chOdt = Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName);
			string sRef = chOdt.ToString() + "http://www.google.com";
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptObjData, sRef);
			m_ttpHyperlink = ttpBldr.GetTextProps();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of a hyperlink followed by additional text.
		/// Simulates typing a character when the selection covers the whole paragraph.
		/// Expectation is that the typed character will not have the hyperlink style or link
		/// property containing the URL.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void OverTypingHyperlink_LinkPluSFollowingText_WholeParagraphSelected()
		{
			var selection = MakeMockSelection();
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkFollowedByPlainText(selHelper, IchPosition.StartOfString,
				IchPosition.EndOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(0));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of a hyperlink followed by additional text.
		/// Simulates typing a character when the selection covers only the hyperlink.
		/// Expectation is that the typed character will have the hyperlink style and the link
		/// property containing the URL.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void OverTypingHyperlink_LinkButNotFollowingText()
		{
			var selection = MakeMockSelection();
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkFollowedByPlainText(selHelper, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

	//			selection.AssertWasNotCalled(sel => sel.SetTypingProps(null));
				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(0));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of nothing but a hyperlink. Simulates
		/// typing a character when the selection is at the end of the paragraph.
		/// Expectation is that the typed character will not have the hyperlink style or the link
		/// property containing the URL.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TypingAfterHyperlink()
		{
			var selection = MakeMockSelection(false);
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.EndOfString, IchPosition.EndOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(0));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of a hyperlink followed by additional text.
		/// Simulates typing a character when the selection is at the end of the hyperlink.
		/// Expectation is that the typed character will have the properties of the following
		/// text.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TypingAfterHyperlink_WithFollowingPlainText()
		{
			var selection = MakeMockSelection(false);
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkFollowedByPlainText(selHelper, IchPosition.EndOfHyperlink,
				IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(0));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of a hyperlink followed by additional
		/// italicized text. Simulates typing a character when the selection is at the end of the
		/// hyperlink.
		/// Expectation is that the typed character will have the properties of the following
		/// text.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TypingAfterHyperlink_WithFollowingItalicsText()
		{
			var selection = MakeMockSelection(false);
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			ITsPropsBldr bldr = m_ttpNormal.GetBldr();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Italics");
			SimulateHyperlinkFollowedByText(selHelper, bldr.GetTextProps(),
				IchPosition.EndOfHyperlink, IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(1));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle), Is.EqualTo("Italics"));
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of nothing but a hyperlink. Simulates
		/// typing a character when the selection is at the start of the paragraph.
		/// Expectation is that the typed character will not have the hyperlink style or the link
		/// property containing the URL.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TypingBeforeHyperlink()
		{
			var selection = MakeMockSelection(false);
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.StartOfString, IchPosition.StartOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(0));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of italicized text followed by a hyperlink.
		/// Simulates typing a character when the selection is at the start of the hyperlink.
		/// Expectation is that the typed character will have the properties of the preceding
		/// text.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TypingBeforeHyperlink_WithPrecedingItalicsText()
		{
			var selection = MakeMockSelection(false);
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			ITsPropsBldr bldr = m_ttpNormal.GetBldr();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Italics");
			SimulateTextFollowedByHyperlink(selHelper, bldr.GetTextProps(),
				IchPosition.StartOfHyperlink, IchPosition.StartOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(1));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle), Is.EqualTo("Italics"));
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of nothing but a hyperlink. Simulates
		/// pressing Backspace when the selection covers entire hyperlink (i.e. the whole paragraph).
		/// Expectation is that the hyperlink-related properties will be cleared.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void BackspaceHyperlink_EntireLink_WholeParagraph()
		{
			var selection = MakeMockSelection();
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.StartOfString,
				IchPosition.EndOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs((char)VwSpecialChars.kscBackspace), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(0));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of nothing but a hyperlink. Simulates
		/// pressing Delete when the selection covers entire hyperlink (i.e. the whole paragraph).
		/// Expectation is that the hyperlink-related properties will be cleared.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeletingHyperlink_EntireLink_WholeParagraph()
		{
			var selection = MakeMockSelection();
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.StartOfString,
				IchPosition.EndOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(0));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of a hyperlink followed by additional text.
		/// Simulates pressing Delete when the selection covers entire hyperlink, but not the
		/// rest of the text in the paragraph.
		/// Expectation is that the hyperlink-related properties will be cleared.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeletingHyperlink_LinkButNotFollowingText()
		{
			var selection = MakeMockSelection();
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkFollowedByPlainText(selHelper, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(0));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of plain text followed by a hyperlink.
		/// Simulates pressing Delete when the selection covers entire hyperlink, but not the
		/// rest of the text in the paragraph.
		/// Expectation is that the hyperlink-related properties will be cleared.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeletingHyperlink_LinkButNotPrecedingText()
		{
			var selection = MakeMockSelection();
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulatePlainTextFollowedByHyperlink(selHelper, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.That(ttpSentToSetTypingProps.StrPropCount, Is.EqualTo(0));
				Assert.That(ttpSentToSetTypingProps.IntPropCount, Is.EqualTo(1));
				int nVar;
				Assert.That(ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar), Is.EqualTo(911));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests scenario where a paragraph consists of nothing but a hyperlink. Simulates
		/// pressing Delete when the selection covers part of the hyperlink.
		/// Expectation is that the hyperlink-related properties will be kept so that any
		/// subsequent typing will retain the hyperlink properties.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeletingMiddleOfHyperlink()
		{
			var selection = MakeMockSelection();
			var selHelper = SelectionHelper.s_mockedSelectionHelper =
				new Mock<SelectionHelper>().Object;
			selHelper.Setup(selH => selH.Selection).Returns(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.EarlyInHyperlink,
				IchPosition.LateInHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

	//			selection.AssertWasNotCalled(sel => sel.SetTypingProps(null));
				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.That(argsSentToSetTypingProps.Count, Is.EqualTo(0));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding a hyperlink to a stringbuilder using the AddHyperlink method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddHyperlink()
		{
			ITsStrBldr strBldr = TsStringUtils.MakeStrBldr();

			LcmStyleSheet mockStylesheet = new Mock<LcmStyleSheet>().Object;
			IStStyle mockHyperlinkStyle = new Mock<IStStyle>().Object;
			mockHyperlinkStyle.Name = StyleServices.Hyperlink;
			mockHyperlinkStyle.Setup(x => x.InUse).Returns(true);
			mockStylesheet.Stub(x => x.FindStyle(StyleServices.Hyperlink)).Return(mockHyperlinkStyle);

			Assert.That(FwEditingHelper.AddHyperlink(strBldr, Cache.DefaultAnalWs, "Click Here",
				"www.google.com", mockStylesheet), Is.True);
			Assert.That(strBldr.RunCount, Is.EqualTo(1));
			Assert.That(strBldr.get_RunText(0), Is.EqualTo("Click Here"));
			ITsTextProps props = strBldr.get_Properties(0);
			LcmTestHelper.VerifyHyperlinkPropsAreCorrect(props, Cache.DefaultAnalWs, "www.google.com");
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a mock IVwSelection representing a range and sets up some basic properties
		/// needed for all the tests in this fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IVwSelection MakeMockSelection()
		{
			return MakeMockSelection(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a mock IVwSelection and sets up some basic properties needed for all the
		/// tests in this fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IVwSelection MakeMockSelection(bool fRange)
		{
			var selection = new Mock<IVwSelection>();
			selection.Setup(sel => sel.IsRange).Returns(fRange);
			selection.Setup(sel => sel.IsValid).Returns(true);
			selection.Setup(sel => sel.IsEditable).Returns(true);
			m_rootbox.Setup(rbox => rbox.Selection).Returns(selection);
			return selection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string with a hyperlink followed
		/// by some plain text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulateHyperlinkFollowedByPlainText(SelectionHelper selHelper,
			IchPosition start, IchPosition end)
		{
			SimulateHyperlinkFollowedByText(selHelper, m_ttpNormal, start, end);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string with a hyperlink followed
		/// by some non-hyperlink text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulateHyperlinkFollowedByText(SelectionHelper selHelper,
			ITsTextProps ttpFollowingText, IchPosition start, IchPosition end)
		{
			ITsStrBldr bldr = TsStringUtils.MakeStrBldr();
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			bldr.Replace(bldr.Length, bldr.Length, "some more text", ttpFollowingText);
			selHelper.Stub(selH => selH.GetTss(It.IsAny<SelectionHelper.SelLimitType>()))
				.Return(bldr.GetString());

			selHelper.Stub(selH => selH.GetSelProps(Arg<SelectionHelper.SelLimitType>.Is.Equal(
				SelectionHelper.SelLimitType.Top))).Return(m_ttpHyperlink);

			int ichStart = 0;
			int ichEnd = 0;
			switch (start)
			{
				case IchPosition.EndOfHyperlink: ichStart = "Google".Length; break;
			}
			switch (end)
			{
				case IchPosition.EndOfString:
					selHelper.Stub(selH => selH.GetSelProps(Arg<SelectionHelper.SelLimitType>.Is.Equal(
						SelectionHelper.SelLimitType.Bottom))).Return(ttpFollowingText);
					ichEnd = bldr.Length;
					break;
				case IchPosition.EndOfHyperlink:
					selHelper.Stub(selH => selH.GetSelProps(Arg<SelectionHelper.SelLimitType>.Is.Equal(
						SelectionHelper.SelLimitType.Bottom))).Return(m_ttpHyperlink);
					ichEnd = "Google".Length;
					break;
			}
			selHelper.Stub(selH => selH.GetIch(SelectionHelper.SelLimitType.Top)).Return(ichStart);
			selHelper.Stub(selH => selH.GetIch(SelectionHelper.SelLimitType.Bottom)).Return(ichEnd);
			selHelper.Setup(selH => selH.IsRange).Returns(ichStart != ichEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string with some plain text followed
		/// by a hyperlink.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulatePlainTextFollowedByHyperlink(SelectionHelper selHelper,
			IchPosition start, IchPosition end)
		{
			SimulateTextFollowedByHyperlink(selHelper, m_ttpNormal, start, end);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string with some plain text followed
		/// by a hyperlink.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulateTextFollowedByHyperlink(SelectionHelper selHelper,
			ITsTextProps ttpPrecedingText, IchPosition start, IchPosition end)
		{
			ITsStrBldr bldr = TsStringUtils.MakeStrBldr();
			bldr.Replace(bldr.Length, bldr.Length, "some plain text", ttpPrecedingText);
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			selHelper.Stub(selH => selH.GetTss(It.IsAny<SelectionHelper.SelLimitType>()))
				.Return(bldr.GetString());

			int ichStart = 0;
			int ichEnd = bldr.Length;
			switch (start)
			{
				case IchPosition.StartOfString:
					selHelper.Stub(selH => selH.GetSelProps(Arg<SelectionHelper.SelLimitType>.Is.Equal(
						SelectionHelper.SelLimitType.Top))).Return(ttpPrecedingText);
					break;
				case IchPosition.StartOfHyperlink:
					selHelper.Stub(selH => selH.GetSelProps(Arg<SelectionHelper.SelLimitType>.Is.Equal(
						SelectionHelper.SelLimitType.Top))).Return(m_ttpHyperlink);
					ichStart = "some plain text".Length;
					break;
			}
			switch (end)
			{
				case IchPosition.StartOfHyperlink: ichEnd = "some plain text".Length; break;
			}
			selHelper.Stub(selH => selH.GetSelProps(Arg<SelectionHelper.SelLimitType>.Is.Equal(
				SelectionHelper.SelLimitType.Bottom))).Return(m_ttpHyperlink);
			selHelper.Stub(selH => selH.GetIch(SelectionHelper.SelLimitType.Top)).Return(ichStart);
			selHelper.Stub(selH => selH.GetIch(SelectionHelper.SelLimitType.Bottom)).Return(ichEnd);
			selHelper.Setup(selH => selH.IsRange).Returns(ichStart != ichEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string consisting of only a hyperlink.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulateHyperlinkOnly(SelectionHelper selHelper,
			IchPosition start, IchPosition end)
		{
			ITsStrBldr bldr = TsStringUtils.MakeStrBldr();
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			selHelper.Stub(selH => selH.GetTss(It.IsAny<SelectionHelper.SelLimitType>()))
				.Return(bldr.GetString());

			selHelper.Stub(selH => selH.GetSelProps(It.IsAny<SelectionHelper.SelLimitType>()))
				.Return(m_ttpHyperlink);

			int ichStart = 0;
			int ichEnd = 0;
			switch (start)
			{
				case IchPosition.EarlyInHyperlink: ichStart = 2; break;
				case IchPosition.EndOfString:
				case IchPosition.EndOfHyperlink: ichStart = bldr.Length; break;
			}
			switch (end)
			{
				case IchPosition.LateInHyperlink: ichEnd = 4; break;
				case IchPosition.EndOfString:
				case IchPosition.EndOfHyperlink: ichEnd = bldr.Length; break;
			}
			selHelper.Stub(selH => selH.GetIch(SelectionHelper.SelLimitType.Top)).Return(ichStart);
			selHelper.Stub(selH => selH.GetIch(SelectionHelper.SelLimitType.Bottom)).Return(ichEnd);
			selHelper.Setup(selH => selH.IsRange).Returns(ichStart != ichEnd);
		}
		#endregion
	}
}
