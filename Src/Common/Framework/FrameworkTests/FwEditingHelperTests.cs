// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwEditingHelperTests.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;
using Rhino.Mocks;
using SIL.FieldWorks.Common.RootSites;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

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
		private IEditingCallbacks m_callbacks = MockRepository.GenerateStub<IEditingCallbacks>();
		private IVwRootSite m_rootsite = MockRepository.GenerateStub<IVwRootSite>();
		private IVwRootBox m_rootbox = MockRepository.GenerateStub<IVwRootBox>();
		private IVwGraphics m_vg = MockRepository.GenerateStub<IVwGraphics>();
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

			m_callbacks.Stub(x => x.EditedRootBox).Return(m_rootbox);
			m_rootbox.Stub(rbox => rbox.Site).Return(m_rootsite);
			m_rootbox.DataAccess = MockRepository.GenerateMock<ISilDataAccess>();
			m_rootsite.Stub(site => site.GetGraphics(Arg<IVwRootBox>.Is.Equal(m_rootbox),
				out Arg<IVwGraphics>.Out(m_vg).Dummy,
				out Arg<Rect>.Out(new Rect()).Dummy,
				out Arg<Rect>.Out(new Rect()).Dummy));

			ITsPropsBldr ttpBldr = (ITsPropsBldr)TsPropsBldrClass.Create();
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkFollowedByPlainText(selHelper, IchPosition.StartOfString,
				IchPosition.EndOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(0, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkFollowedByPlainText(selHelper, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

	//			selection.AssertWasNotCalled(sel => sel.SetTypingProps(null));
				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(0, argsSentToSetTypingProps.Count);
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.EndOfString, IchPosition.EndOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(0, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkFollowedByPlainText(selHelper, IchPosition.EndOfHyperlink,
				IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(0, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			ITsPropsBldr bldr = m_ttpNormal.GetBldr();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Italics");
			SimulateHyperlinkFollowedByText(selHelper, bldr.GetTextProps(),
				IchPosition.EndOfHyperlink, IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(1, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual("Italics", ttpSentToSetTypingProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.StartOfString, IchPosition.StartOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(0, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			ITsPropsBldr bldr = m_ttpNormal.GetBldr();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Italics");
			SimulateTextFollowedByHyperlink(selHelper, bldr.GetTextProps(),
				IchPosition.StartOfHyperlink, IchPosition.StartOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs('b'), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(1, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual("Italics", ttpSentToSetTypingProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.StartOfString,
				IchPosition.EndOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.OnKeyPress(new KeyPressEventArgs((char)VwSpecialChars.kscBackspace), Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(0, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.StartOfString,
				IchPosition.EndOfString);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(0, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkFollowedByPlainText(selHelper, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(0, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulatePlainTextFollowedByHyperlink(selHelper, IchPosition.StartOfHyperlink,
				IchPosition.EndOfHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(1, argsSentToSetTypingProps.Count);
				ITsTextProps ttpSentToSetTypingProps = (ITsTextProps)argsSentToSetTypingProps[0][0];
				Assert.AreEqual(0, ttpSentToSetTypingProps.StrPropCount);
				Assert.AreEqual(1, ttpSentToSetTypingProps.IntPropCount);
				int nVar;
				Assert.AreEqual(911, ttpSentToSetTypingProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
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
				MockRepository.GenerateStub<SelectionHelper>();
			selHelper.Stub(selH => selH.Selection).Return(selection);

			SimulateHyperlinkOnly(selHelper, IchPosition.EarlyInHyperlink,
				IchPosition.LateInHyperlink);

			using (FwEditingHelper editingHelper = new FwEditingHelper(Cache, m_callbacks))
			{
				editingHelper.HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);

	//			selection.AssertWasNotCalled(sel => sel.SetTypingProps(null));
				IList<object[]> argsSentToSetTypingProps =
					selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
				Assert.AreEqual(0, argsSentToSetTypingProps.Count);
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();

			FwStyleSheet mockStylesheet = MockRepository.GenerateStub<FwStyleSheet>();
			IStStyle mockHyperlinkStyle = MockRepository.GenerateStub<IStStyle>();
			mockHyperlinkStyle.Name = StyleServices.Hyperlink;
			mockHyperlinkStyle.Stub(x => x.InUse).Return(true);
			mockStylesheet.Stub(x => x.FindStyle(StyleServices.Hyperlink)).Return(mockHyperlinkStyle);

			Assert.IsTrue(FwEditingHelper.AddHyperlink(strBldr, Cache.DefaultAnalWs, "Click Here",
				"www.google.com", mockStylesheet));
			Assert.AreEqual(1, strBldr.RunCount);
			Assert.AreEqual("Click Here", strBldr.get_RunText(0));
			ITsTextProps props = strBldr.get_Properties(0);
			FdoTestHelper.VerifyHyperlinkPropsAreCorrect(props, Cache.DefaultAnalWs, "www.google.com");
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
			var selection = MockRepository.GenerateMock<IVwSelection>();
			selection.Stub(sel => sel.IsRange).Return(fRange);
			selection.Stub(sel => sel.IsValid).Return(true);
			selection.Stub(sel => sel.IsEditable).Return(true);
			m_rootbox.Stub(rbox => rbox.Selection).Return(selection);
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
			ITsStrBldr bldr = (ITsStrBldr)TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			bldr.Replace(bldr.Length, bldr.Length, "some more text", ttpFollowingText);
			selHelper.Stub(selH => selH.GetTss(Arg<SelectionHelper.SelLimitType>.Is.Anything))
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
			selHelper.Stub(selH => selH.IsRange).Return(ichStart != ichEnd);
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
			ITsStrBldr bldr = (ITsStrBldr)TsStrBldrClass.Create();
			bldr.Replace(bldr.Length, bldr.Length, "some plain text", ttpPrecedingText);
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			selHelper.Stub(selH => selH.GetTss(Arg<SelectionHelper.SelLimitType>.Is.Anything))
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
			selHelper.Stub(selH => selH.IsRange).Return(ichStart != ichEnd);
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
			ITsStrBldr bldr = (ITsStrBldr)TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Google", m_ttpHyperlink);
			selHelper.Stub(selH => selH.GetTss(Arg<SelectionHelper.SelLimitType>.Is.Anything))
				.Return(bldr.GetString());

			selHelper.Stub(selH => selH.GetSelProps(Arg<SelectionHelper.SelLimitType>.Is.Anything))
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
			selHelper.Stub(selH => selH.IsRange).Return(ichStart != ichEnd);
		}
		#endregion
	}
}
