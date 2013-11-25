// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ChangeParagraphStyleTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ChangeCharacterStyleTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ChangeCharacterStyleTests : DraftViewTestBase
	{
		#region Tests (TE-5872)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When applying character formatting, if the selection includes whitespace at either
		/// end AND the selection includes characters that are not whitespace, then alter the
		/// selection so that it does not have whitespace at either end before applying the
		/// character formatting. (It is almost never right to format adjacent whitespace with
		/// the character style and this contributes to the problem of orphaned formatting).
		/// The selection that is shown after the operation should be the altered (reduced)
		/// selection.
		/// (From TE-5872)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-5872: This issue is being re-evaluated. We may handle orphaned character styles with Scripture checks")]
		public void ApplyStyle_CharStyleNotAppliedToBorderingWhiteSpace()
		{
			m_draftView.RefreshDisplay();

			// Select a word in the book intro with white space around it: " text. "
			m_draftView.SetInsertionPoint(0, 0, 0, 5, false);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 0, 0, 12, true);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection rangeSel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			ITsString tssSelected;
			rangeSel.GetSelectionString(out tssSelected, "*");
			Assert.AreEqual(" text. ", tssSelected.Text);

			// Apply a character style to the selected text.
			m_draftView.ApplyStyle("Emphasis");

			// The selection should be updated to omit bordering white space in the original selection.
			m_draftView.RootBox.Selection.GetSelectionString(out tssSelected, "*");
			Assert.AreEqual("text.", tssSelected.Text);

			// Confirm that the character styles are applied correctly: Emphasis style applied only to "text."
			IScrSection introSection = m_exodus.SectionsOS[0];
			ITsString paraContents = introSection.ContentOA[0].Contents;
			Assert.AreEqual(3, paraContents.RunCount);
			Assert.AreEqual(7, paraContents.get_MinOfRun(1));
			Assert.AreEqual(11, paraContents.get_LimOfRun(1));
			ITsTextProps ttp = paraContents.get_Properties(0);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle),
				"First run should be 'Default Paragraph Characters'");
			ttp = paraContents.get_Properties(1);
			Assert.AreEqual("Emphasis", ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			ttp = paraContents.get_Properties(2);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle),
				"Third run should be 'Default Paragraph Characters'");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When character formatting is removed, extend the removal selection to include any
		/// adjacent whitespace before applying the removal. The selection that is shown after
		/// the operation should be the original selection, not the extended selection.
		/// (From TE-5872)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-5872: This issue is being re-evaluated. We may handle orphaned character styles with Scripture checks")]
		public void ClearStyle_CharStyleRemovedFromAdjacentWhiteSpace()
		{
			m_draftView.RefreshDisplay();

			// *** Test setup ***
			// Select a word in the book intro with white space around it: " text. "
			m_draftView.SetInsertionPoint(0, 0, 0, 5, false);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 0, 0, 12, true);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection rangeSel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			ITsString tssSelected;
			rangeSel.GetSelectionString(out tssSelected, "*");
			Assert.AreEqual(" text. ", tssSelected.Text);

			// Build an array of string props with the style name of the only run.
			ITsTextProps[] props = new ITsTextProps[1];
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			// The named style is the default, so not property values are added.
			props[0] = bldr.GetTextProps();

			// Apply a character style to the selected text (including the surrounding white space)
			m_draftView.EditingHelper.RemoveCharFormatting(rangeSel, ref props, "Emphasis");

			// so that we have three runs.
			IScrSection introSection = m_exodus.SectionsOS[0];
			ITsString paraContents = introSection.ContentOA[0].Contents;
			Assert.AreEqual(3, paraContents.RunCount);

			// *** Test ***
			// Select just the word "text." without the surrounding white space.
			m_draftView.SetInsertionPoint(0, 0, 0, 6, false);
			sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 0, 0, 11, true);
			sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			rangeSel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			rangeSel.GetSelectionString(out tssSelected, "*");
			Assert.AreEqual("text.", tssSelected.Text);

			// Remove character style formatting from the selected text.
			m_draftView.EditingHelper.RemoveCharFormatting(rangeSel, ref props, string.Empty);

			// We expect that there should now only be one run in this paragraph with the style
			// "Default Paragraph Characters".
			paraContents = introSection.ContentOA[0].Contents;
			Assert.AreEqual(1, paraContents.RunCount,
				"Style for boundary white space characters not properly removed");
			ITsTextProps ttp = paraContents.get_Properties(0);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle),
				"Character style of run should be 'Default Paragraph Characters'");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When any whitespace character is entered preceding a run that has character
		/// formatting, if it is after a Default Paragraph Character run within the same
		/// paragraph, add it to that run rather than to the run with the character formatting.
		/// (From TE-5872)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-5872: This issue is being re-evaluated. We may handle orphaned character styles with Scripture checks")]
		public void InsertChars_WhiteSpaceOnBorderOfStyleUsesDefaultStyle()
		{
			m_draftView.RefreshDisplay();

			// *** Test setup ***
			// Insert a word with a character style.
			IStTxtPara para = m_exodus.SectionsOS[0].ContentOA[0];
			ITsStrBldr tssBldr = para.Contents.GetBldr();
			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasis");
			tssBldr.ReplaceRgch(15, 15, "really", 6, ttpBldr.GetTextProps());
			para.Contents = tssBldr.GetString();
			Assert.AreEqual(3, para.Contents.RunCount);

			// Set the IP immediately before this inserted text.
			m_draftView.SetInsertionPoint(0, 0, 0, 15, true);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);

			// *** Test ***
			// Type two spaces.
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Space));
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Space));

			// We expect that the spaces will be in "Default Paragraph Characters"
			// not in a character style
			ITsString tss = para.Contents;
			// the word "really" is in the Emphasis style but the inserted white space is not.
			Assert.AreEqual("really", tss.get_RunText(1));
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual("Emphasis",
				ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			ttp = tss.get_Properties(0);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle),
				"Style of the first run should be 'Default Paragraph Characters'");
			Assert.AreEqual("Intro text. We   ", tss.get_RunText(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a new paragraph break is created, remove character formatting from any number
		/// of white space characters that are adjactent to the break.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-5872: This issue is being re-evaluated. We may handle orphaned character styles with Scripture checks")]
		public void ParagraphBreak_RemoveCharStyleFromAdjacentWhiteSpace()
		{
			m_draftView.RefreshDisplay();

			// *** Test setup ***
			// Insert text with a character style.
			IStTxtPara para1 = m_exodus.SectionsOS[0].ContentOA[0];
			ITsStrBldr tssBldr = para1.Contents.GetBldr();
			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasis");
			tssBldr.ReplaceRgch(15, 15, "really,  really", 6, ttpBldr.GetTextProps());
			int paraCount = m_exodus.SectionsOS[0].ContentOA.ParagraphsOS.Count;

			// Set the IP in the white space within the inserted text.
			m_draftView.SetInsertionPoint(0, 0, 0, 23, true);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);

			// Insert paragraph break
			m_draftView.OnKeyPress(new KeyPressEventArgs('\r'));

			Assert.AreEqual(paraCount + 1, m_exodus.SectionsOS[0].ContentOA.ParagraphsOS.Count,
				"Should have one more paragraph in the intro section");

			// We expect that the last run of the first paragraph and first run of the new
			// paragraph will be a space character with no character style.
			ITsString tss1 = para1.Contents;
			Assert.AreEqual(3, tss1.RunCount, "First intro paragraph should have three runs");
			Assert.AreEqual("really,", tss1.get_RunText(1));
			ITsTextProps ttp = tss1.get_Properties(1);
			Assert.AreEqual("Emphasis", ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			ttp = tss1.get_Properties(2);
			Assert.AreEqual(" ", tss1.get_RunText(2));
			Assert.AreEqual(null, ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));

			ITsString tss2 = m_exodus.SectionsOS[0].ContentOA[1].Contents;
			Assert.AreEqual(3, tss2.RunCount);
			Assert.AreEqual(" ", tss2.get_RunText(0));
			ttp = tss1.get_Properties(0);
			Assert.AreEqual(null, ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			ttp = tss1.get_Properties(1);
			Assert.AreEqual("Emphasis", ttp.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			Assert.AreEqual("really", tss2.get_RunText(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// After any deletion, remove character formatting from any number of white space
		/// characters that are adjactent to the new IP within the same paragraph. Be sure that
		/// deletions that are part of Cut or Paste operations are included in this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-5872: This issue is being re-evaluated. We may handle orphaned character styles with Scripture checks")]
		public void DeleteChars_RemoveCharStyleAdjacentToIP()
		{
			//TODO: This will probably require three different tests, if implemented.
			Assert.Fail();
		}
		#endregion
	}
}
