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
		private Mock<IEditingCallbacks> m_callbacksMock = new Mock<IEditingCallbacks>();
		private Mock<IVwRootSite> m_rootsiteMock = new Mock<IVwRootSite>();
		private Mock<IVwRootBox> m_rootboxMock = new Mock<IVwRootBox>();
		private Mock<IVwGraphics> m_vgMock = new Mock<IVwGraphics>();

		private IEditingCallbacks m_callbacks => m_callbacksMock.Object;
		private IVwRootSite m_rootsite => m_rootsiteMock.Object;
		private IVwRootBox m_rootbox => m_rootboxMock.Object;
		private IVwGraphics m_vg => m_vgMock.Object;

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

			m_callbacksMock.Setup(x => x.EditedRootBox).Returns(m_rootbox);
			m_rootboxMock.Setup(rbox => rbox.Site).Returns(m_rootsite);
			m_rootboxMock.Object.DataAccess = new Mock<ISilDataAccess>().Object;

			// Setup GetGraphics with out parameters
			IVwGraphics vgOut = m_vg;
			Rect rect1 = new Rect();
			Rect rect2 = new Rect();
			m_rootsiteMock.Setup(site => site.GetGraphics(m_rootbox, out vgOut, out rect1, out rect2));

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
			var selectionMock = MakeMockSelectionMock();
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkFollowedByPlainText(selHelperMock, IchPosition.StartOfString,
				IchPosition.EndOfString);

			SelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				// Verify SetTypingProps was called exactly once
				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock();
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkFollowedByPlainText(selHelperMock, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				// Verify SetTypingProps was not called
				Assert.That(capturedProps.Count, Is.EqualTo(0));
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
			var selectionMock = MakeMockSelectionMock(false);
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkOnly(selHelperMock, IchPosition.EndOfString, IchPosition.EndOfString);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock(false);
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkFollowedByPlainText(selHelperMock, IchPosition.EndOfHyperlink,
				IchPosition.EndOfHyperlink);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock(false);
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			ITsPropsBldr bldr = m_ttpNormal.GetBldr();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Italics");
			SimulateHyperlinkFollowedByText(selHelperMock, bldr.GetTextProps(),
				IchPosition.EndOfHyperlink, IchPosition.EndOfHyperlink);

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock(false);
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkOnly(selHelperMock, IchPosition.StartOfString, IchPosition.StartOfString);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock(false);
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			ITsPropsBldr bldr = m_ttpNormal.GetBldr();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Italics");
			SimulateTextFollowedByHyperlink(selHelperMock, bldr.GetTextProps(),
				IchPosition.StartOfHyperlink, IchPosition.StartOfHyperlink);

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock();
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkOnly(selHelperMock, IchPosition.StartOfString,
				IchPosition.EndOfString);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs((char)VwSpecialChars.kscBackspace), Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock();
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkOnly(selHelperMock, IchPosition.StartOfString,
				IchPosition.EndOfString);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock();
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkFollowedByPlainText(selHelperMock, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock();
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulatePlainTextFollowedByHyperlink(selHelperMock, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				Assert.That(capturedProps.Count, Is.EqualTo(1));
				ITsTextProps ttpSentToSetTypingProps = capturedProps[0];
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
			var selectionMock = MakeMockSelectionMock();
			var selHelperMock = new Mock<SelectionHelper>();
			selHelperMock.Setup(selH => selH.Selection).Returns(selectionMock.Object);

			SimulateHyperlinkOnly(selHelperMock, IchPosition.EarlyInHyperlink,
				IchPosition.LateInHyperlink);

			// Setup callbackSelectionHelper.s_mockedSelectionHelper = selHelperMock.Object;

			// Setup callback to capture arguments passed to SetTypingProps
			var capturedProps = new List<ITsTextProps>();
			selectionMock.Setup(sel => sel.SetTypingProps(It.IsAny<ITsTextProps>()))
				.Callback<ITsTextProps>(ttp => capturedProps.Add(ttp));

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				// Verify SetTypingProps was not called
				Assert.That(capturedProps.Count, Is.EqualTo(0));
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

			var mockStylesheetMock = new Mock<LcmStyleSheet>();
			var mockHyperlinkStyleMock = new Mock<IStStyle>();
			mockHyperlinkStyleMock.Setup(x => x.Name).Returns(StyleServices.Hyperlink);
			mockHyperlinkStyleMock.Setup(x => x.InUse).Returns(true);
			mockStylesheetMock.Setup(x => x.FindStyle(StyleServices.Hyperlink)).Returns(mockHyperlinkStyleMock.Object);

			Assert.That(FwEditingHelper.AddHyperlink(strBldr, Cache.DefaultAnalWs, "Click Here",
				"www.google.com", mockStylesheetMock.Object), Is.True);
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
		/// <summary>
		/// Generates a mock IVwSelection and sets up some basic properties needed for all the
		/// tests in this fixture. Returns the mock object so tests can verify calls.
		/// </summary>
		private Mock<IVwSelection> MakeMockSelectionMock()
		{
			return MakeMockSelectionMock(true);
		}

		private Mock<IVwSelection> MakeMockSelectionMock(bool fRange)
		{
			var selectionMock = new Mock<IVwSelection>();
			selectionMock.Setup(sel => sel.IsRange).Returns(fRange);
			selectionMock.Setup(sel => sel.IsValid).Returns(true);
			selectionMock.Setup(sel => sel.IsEditable).Returns(true);
			m_rootboxMock.Setup(rbox => rbox.Selection).Returns(selectionMock.Object);
			return selectionMock;
		}

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
			return MakeMockSelectionMock(fRange).Object;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string with a hyperlink followed
		/// by some plain text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulateHyperlinkFollowedByPlainText(Mock<SelectionHelper> selHelperMock,
			IchPosition start, IchPosition end)
		{
			SimulateHyperlinkFollowedByText(selHelperMock, m_ttpNormal, start, end);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string with a hyperlink followed
		/// by some non-hyperlink text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulateHyperlinkFollowedByText(Mock<SelectionHelper> selHelperMock,
			ITsTextProps ttpFollowingText, IchPosition start, IchPosition end)
		{
			ITsStrBldr bldr = TsStringUtils.MakeStrBldr();
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			bldr.Replace(bldr.Length, bldr.Length, "some more text", ttpFollowingText);
			selHelperMock.Setup(selH => selH.GetTss(It.IsAny<SelectionHelper.SelLimitType>()))
				.Returns(bldr.GetString());

			selHelperMock.Setup(selH => selH.GetSelProps(SelectionHelper.SelLimitType.Top))
				.Returns(m_ttpHyperlink);

			int ichStart = 0;
			int ichEnd = 0;
			switch (start)
			{
				case IchPosition.EndOfHyperlink: ichStart = "Google".Length; break;
			}
			switch (end)
			{
				case IchPosition.EndOfString:
					selHelperMock.Setup(selH => selH.GetSelProps(SelectionHelper.SelLimitType.Bottom))
						.Returns(ttpFollowingText);
					ichEnd = bldr.Length;
					break;
				case IchPosition.EndOfHyperlink:
					selHelperMock.Setup(selH => selH.GetSelProps(SelectionHelper.SelLimitType.Bottom))
						.Returns(m_ttpHyperlink);
					ichEnd = "Google".Length;
					break;
			}
			selHelperMock.Setup(selH => selH.GetIch(SelectionHelper.SelLimitType.Top)).Returns(ichStart);
			selHelperMock.Setup(selH => selH.GetIch(SelectionHelper.SelLimitType.Bottom)).Returns(ichEnd);
			selHelperMock.Setup(selH => selH.IsRange).Returns(ichStart != ichEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string with some plain text followed
		/// by a hyperlink.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulatePlainTextFollowedByHyperlink(Mock<SelectionHelper> selHelperMock,
			IchPosition start, IchPosition end)
		{
			SimulateTextFollowedByHyperlink(selHelperMock, m_ttpNormal, start, end);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string with some plain text followed
		/// by a hyperlink.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulateTextFollowedByHyperlink(Mock<SelectionHelper> selHelperMock,
			ITsTextProps ttpPrecedingText, IchPosition start, IchPosition end)
		{
			ITsStrBldr bldr = TsStringUtils.MakeStrBldr();
			bldr.Replace(bldr.Length, bldr.Length, "some plain text", ttpPrecedingText);
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			selHelperMock.Setup(selH => selH.GetTss(It.IsAny<SelectionHelper.SelLimitType>()))
				.Returns(bldr.GetString());

			int ichStart = 0;
			int ichEnd = bldr.Length;
			switch (start)
			{
				case IchPosition.StartOfString:
					selHelperMock.Setup(selH => selH.GetSelProps(SelectionHelper.SelLimitType.Top))
						.Returns(ttpPrecedingText);
					break;
				case IchPosition.StartOfHyperlink:
					selHelperMock.Setup(selH => selH.GetSelProps(SelectionHelper.SelLimitType.Top))
						.Returns(m_ttpHyperlink);
					ichStart = "some plain text".Length;
					break;
			}
			switch (end)
			{
				case IchPosition.StartOfHyperlink: ichEnd = "some plain text".Length; break;
			}
			selHelperMock.Setup(selH => selH.GetSelProps(SelectionHelper.SelLimitType.Bottom))
				.Returns(m_ttpHyperlink);
			selHelperMock.Setup(selH => selH.GetIch(SelectionHelper.SelLimitType.Top)).Returns(ichStart);
			selHelperMock.Setup(selH => selH.GetIch(SelectionHelper.SelLimitType.Bottom)).Returns(ichEnd);
			selHelperMock.Setup(selH => selH.IsRange).Returns(ichStart != ichEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a TsString to be returned from the selection helper so that it will appear
		/// to the editing helper as though we're editing a string consisting of only a hyperlink.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimulateHyperlinkOnly(Mock<SelectionHelper> selHelperMock,
			IchPosition start, IchPosition end)
		{
			ITsStrBldr bldr = TsStringUtils.MakeStrBldr();
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			selHelperMock.Setup(selH => selH.GetTss(It.IsAny<SelectionHelper.SelLimitType>()))
				.Returns(bldr.GetString());

			selHelperMock.Setup(selH => selH.GetSelProps(It.IsAny<SelectionHelper.SelLimitType>()))
				.Returns(m_ttpHyperlink);

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
			selHelperMock.Setup(selH => selH.GetIch(SelectionHelper.SelLimitType.Top)).Returns(ichStart);
			selHelperMock.Setup(selH => selH.GetIch(SelectionHelper.SelLimitType.Bottom)).Returns(ichEnd);
			selHelperMock.Setup(selH => selH.IsRange).Returns(ichStart != ichEnd);
		}
		#endregion
	}
}
