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
// File: DiffTestHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class DiffTestHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given difference
		/// for any types that deal with changes in a paragraph.
		/// </summary>
		/// <remarks>char styles and subDiffs are not verified here; test code should just check
		/// those directly if relevant</remarks>
		/// <param name="diff">the given Difference.</param>
		/// <param name="start">The verse ref start.</param>
		/// <param name="end">The verse ref end.</param>
		/// <param name="type">Type of the diff.</param>
		/// <param name="paraCurr">The Current paragraph.</param>
		/// <param name="ichMinCurr">The ich min in paraCurr.</param>
		/// <param name="ichLimCurr">The ich lim in paraCurr.</param>
		/// <param name="paraRev">The Revision paragraph.</param>
		/// <param name="ichMinRev">The ich min in paraRev.</param>
		/// <param name="ichLimRev">The ich lim in paraRev.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyParaDiff(Difference diff,
			BCVRef start, BCVRef end, DifferenceType type,
			IScrTxtPara paraCurr, int ichMinCurr, int ichLimCurr,
			IScrTxtPara paraRev, int ichMinRev, int ichLimRev)
		{
			// verify the basics
			Assert.AreEqual(start, diff.RefStart);
			Assert.AreEqual(end, diff.RefEnd);
			Assert.AreEqual(type, diff.DiffType);

			// the Current para stuff
			Assert.AreEqual(paraCurr, diff.ParaCurr);
			Assert.AreEqual(ichMinCurr, diff.IchMinCurr);
			Assert.AreEqual(ichLimCurr, diff.IchLimCurr);

			// the Revision para stuff
			Assert.AreEqual(paraRev, diff.ParaRev);
			Assert.AreEqual(ichMinRev, diff.IchMinRev);
			Assert.AreEqual(ichLimRev, diff.IchLimRev);

			// section stuff should be null
			Assert.IsNull(diff.SectionsRev);
			Assert.IsNull(diff.SectionsCurr);
		}

		/// <summary>overload for same end ref</summary>
		public static void VerifyParaDiff(Difference diff,
			BCVRef startAndEnd, DifferenceType type,
			IScrTxtPara paraCurr, int ichMinCurr, int ichLimCurr,
			IScrTxtPara paraRev, int ichMinRev, int ichLimRev)
		{
			VerifyParaDiff(diff, startAndEnd, startAndEnd, type,
				paraCurr, ichMinCurr, ichLimCurr,
				paraRev, ichMinRev, ichLimRev);
		}

		/// <summary>overload for same end ref, same ichCurr</summary>
		public static void VerifyParaDiff(Difference diff,
			BCVRef startAndEnd, DifferenceType type,
			IScrTxtPara paraCurr, int ichCurr,
			IScrTxtPara paraRev, int ichMinRev, int ichLimRev)
		{
			VerifyParaDiff(diff, startAndEnd, startAndEnd, type,
				paraCurr, ichCurr, ichCurr,
				paraRev, ichMinRev, ichLimRev);
		}

		/// <summary>overload for same end ref, same ichRev</summary>
		public static void VerifyParaDiff(Difference diff,
			BCVRef startAndEnd, DifferenceType type,
			IScrTxtPara paraCurr, int ichMinCurr, int ichLimCurr,
			IScrTxtPara paraRev, int ichRev)
		{
			VerifyParaDiff(diff, startAndEnd, startAndEnd, type,
				paraCurr, ichMinCurr, ichLimCurr,
				paraRev, ichRev, ichRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given base difference
		/// that deals with changes in paragraph structure.
		/// </summary>
		/// <param name="diff">The diff.</param>
		/// <param name="startAndEnd">The starting and ending reference.</param>
		/// <param name="type">The difference type.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyParaStructDiff(Difference diff,
			BCVRef startAndEnd, DifferenceType type)
		{
			VerifyParaStructDiff(diff, startAndEnd, startAndEnd, type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given base difference
		/// that deals with changes in paragraph structure.
		/// </summary>
		/// <remarks>char styles and subDiffs are not verified here; test code should just check
		/// those directly if relevant</remarks>
		/// <param name="diff">the given Difference.</param>
		/// <param name="start">The verse ref start.</param>
		/// <param name="end">The verse ref end.</param>
		/// <param name="type">Type of the diff.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyParaStructDiff(Difference diff,
			BCVRef start, BCVRef end, DifferenceType type)
		{
			// verify the basics
			Assert.AreEqual(start, diff.RefStart);
			Assert.AreEqual(end, diff.RefEnd);
			Assert.AreEqual(type, diff.DiffType);

			// Subdifferences must exist.
			Assert.IsNotNull(diff.SubDiffsForParas, "Subdifferences should have been created.");
			Assert.Greater(diff.SubDiffsForParas.Count, 0, "Subdifferences should have been created.");
			Difference firstSubdiff = diff.SubDiffsForParas[0];

			// the Current para stuff should be the same as the start of the first subdiff
			Assert.AreEqual(firstSubdiff.ParaCurr, diff.ParaCurr);
			Assert.AreEqual(firstSubdiff.IchMinCurr, diff.IchMinCurr);
			Assert.AreEqual(firstSubdiff.IchMinCurr, diff.IchLimCurr);

			// the Revision para stuff should be the same as the start of the first subdiff also
			Assert.AreEqual(firstSubdiff.ParaRev, diff.ParaRev);
			Assert.AreEqual(firstSubdiff.IchMinRev, diff.IchMinRev);
			Assert.AreEqual(firstSubdiff.IchMinRev, diff.IchLimRev);

			// section stuff should be null
			Assert.IsNull(diff.SectionsRev);
			Assert.IsNull(diff.SectionsCurr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given difference
		/// for a one-sided subDiff representing a footnote in the Current.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void VerifySubDiffFootnoteCurr(Difference rootDiff, int iSubDiff,
			IScrFootnote footnoteCurr)
		{
			Difference subDiff = rootDiff.SubDiffsForORCs[iSubDiff];
			// verify the basics
			Assert.AreEqual(0, subDiff.RefStart);
			Assert.AreEqual(0, subDiff.RefEnd);
			Assert.AreEqual(DifferenceType.NoDifference, subDiff.DiffType);

			// the Current para stuff
			Assert.AreEqual(((IScrTxtPara)footnoteCurr.ParagraphsOS[0]), subDiff.ParaCurr);
			Assert.AreEqual(0, subDiff.IchMinCurr);
			Assert.AreEqual(((IScrTxtPara)footnoteCurr.ParagraphsOS[0]).Contents.Length, subDiff.IchLimCurr);

			// the Revision para stuff
			Assert.AreEqual(null, subDiff.ParaRev);
			Assert.AreEqual(0, subDiff.IchMinRev);
			Assert.AreEqual(0, subDiff.IchLimRev);

			// style names should be null
			Assert.IsNull(subDiff.StyleNameCurr);
			Assert.IsNull(subDiff.StyleNameRev);

			// section stuff should be null
			Assert.IsNull(subDiff.SectionsRev);
			Assert.IsNull(subDiff.SectionsCurr);

			// subDiffs may not have subDiffs, so far
			Assert.IsNull(subDiff.SubDiffsForORCs);

			//check the root difference for consistency with this subDiff
			Assert.IsTrue(rootDiff.DiffType == DifferenceType.TextDifference ||
				rootDiff.DiffType == DifferenceType.FootnoteAddedToCurrent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given difference
		/// for a one-sided subDiff representing a footnote in the Revision.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void VerifySubDiffFootnoteRev(Difference rootDiff, int iSubDiff,
			IScrFootnote footnoteRev)
		{
			Difference subDiff = rootDiff.SubDiffsForORCs[iSubDiff];
			// verify the basics
			Assert.AreEqual(0, subDiff.RefStart);
			Assert.AreEqual(0, subDiff.RefEnd);
			Assert.AreEqual(DifferenceType.NoDifference, subDiff.DiffType);

			// the Current para stuff
			Assert.AreEqual(null, subDiff.ParaCurr);
			Assert.AreEqual(0, subDiff.IchMinCurr);
			Assert.AreEqual(0, subDiff.IchLimCurr);

			// the Revision para stuff
			Assert.AreEqual(((IScrTxtPara)footnoteRev.ParagraphsOS[0]), subDiff.ParaRev);
			Assert.AreEqual(0, subDiff.IchMinRev);
			Assert.AreEqual(((IScrTxtPara)footnoteRev.ParagraphsOS[0]).Contents.Length, subDiff.IchLimRev);

			// style names should be null
			Assert.IsNull(subDiff.StyleNameCurr);
			Assert.IsNull(subDiff.StyleNameRev);

			// section stuff should be null
			Assert.IsNull(subDiff.SectionsRev);
			Assert.IsNull(subDiff.SectionsCurr);

			// subDiffs may not have subDiffs, so far
			Assert.IsNull(subDiff.SubDiffsForORCs);

			//check the root difference for consistency with this subDiff
			Assert.IsTrue(rootDiff.DiffType == DifferenceType.TextDifference ||
				rootDiff.DiffType == DifferenceType.FootnoteMissingInCurrent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given sub-difference
		/// for a two-sided subDiff representing a text comparison.
		/// </summary>
		/// <remarks>char styles are not verified here; test code should just check
		/// those directly if relevant</remarks>
		/// ------------------------------------------------------------------------------------
		//TODO: this is used only for VerseMoved subdiffs. rename it as VerifySubDiffVerseMoved.
		// provide logic appropriate for VerseMoved, and don't rely on VerifySubDiffTextCompared
		// Maybe just revert to the 2006 logic when VerseMoved was implemented.
		public static void VerifySubDiffTextCompared(Difference rootDiff, int iSubDiff,
			BCVRef start, BCVRef end, DifferenceType subDiffType,
			IScrTxtPara paraCurr, int ichMinCurr, int ichLimCurr,
			IScrTxtPara paraRev, int ichMinRev, int ichLimRev)
		{
			Difference subDiff = rootDiff.SubDiffsForParas[iSubDiff];
			// verify the Scripture references
			Assert.AreEqual(start, subDiff.RefStart);
			Assert.AreEqual(end, subDiff.RefEnd);

			// verify everything else
			VerifySubDiffTextCompared(rootDiff, iSubDiff, subDiffType, paraCurr, ichMinCurr, ichLimCurr,
				paraRev, ichMinRev, ichLimRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given sub-difference
		/// for a two-sided subDiff representing a text comparison. This overload does not
		/// check for the starting and ending references for sub-diffs that are created without
		/// that information.
		/// </summary>
		/// <param name="rootDiff">The root difference.</param>
		/// <param name="iSubDiff">The sub difference to verify.</param>
		/// <param name="subDiffType">Type of the sub difference.</param>
		/// <param name="paraCurr">The current paragraph.</param>
		/// <param name="ichMinCurr">The beginning character offset of the difference in the
		/// current.</param>
		/// <param name="ichLimCurr">The ending character offset of the difference in the
		/// current.</param>
		/// <param name="paraRev">The revision paragraph.</param>
		/// <param name="ichMinRev">The beginning character offset of the difference in the
		/// revision.</param>
		/// <param name="ichLimRev">The ending character offset of the difference in the
		/// current.</param>
		/// <remarks>char styles are not verified here; test code should just check
		/// those directly if relevant</remarks>
		/// ------------------------------------------------------------------------------------
		//TODO: use an iSubDiff parameter instead of the subDiff itself;
		// use the following method as a model to verify the root diff type
		// make a separate method for footnote subdiffs
		public static void VerifySubDiffTextCompared(Difference rootDiff, int iSubDiff,
			DifferenceType subDiffType,
			IScrTxtPara paraCurr, int ichMinCurr, int ichLimCurr,
			IScrTxtPara paraRev, int ichMinRev, int ichLimRev)
		{
			Difference subDiff = rootDiff.SubDiffsForParas[iSubDiff];
			// the Current para stuff
			Assert.AreEqual(paraCurr, subDiff.ParaCurr);
			Assert.AreEqual(ichMinCurr, subDiff.IchMinCurr);
			Assert.AreEqual(ichLimCurr, subDiff.IchLimCurr);

			// the Revision para stuff
			Assert.AreEqual(paraRev, subDiff.ParaRev);
			Assert.AreEqual(ichMinRev, subDiff.IchMinRev);
			Assert.AreEqual(ichLimRev, subDiff.IchLimRev);

			// section stuff should be null
			Assert.IsNull(subDiff.SectionsRev);
			Assert.IsNull(subDiff.SectionsCurr);

			// subDiffs may not have subDiffs, so far
			Assert.IsNull(subDiff.SubDiffsForORCs);
			Assert.IsNull(subDiff.SubDiffsForParas);

			Assert.AreEqual(subDiffType, subDiff.DiffType);

			if ((rootDiff.DiffType & DifferenceType.ParagraphSplitInCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphMergedInCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphStructureChange) != 0)
			{
				// check the subDiff for consistency with the root diff.
				Assert.IsTrue((subDiff.DiffType & DifferenceType.TextDifference) != 0 ||
					(subDiff.DiffType & DifferenceType.FootnoteAddedToCurrent) != 0 ||
					(subDiff.DiffType & DifferenceType.FootnoteMissingInCurrent) != 0 ||
					(subDiff.DiffType & DifferenceType.FootnoteDifference) != 0 ||
					(subDiff.DiffType & DifferenceType.MultipleCharStyleDifferences) != 0 ||
					(subDiff.DiffType & DifferenceType.CharStyleDifference) != 0 ||
					(subDiff.DiffType & DifferenceType.PictureAddedToCurrent) != 0 ||
					(subDiff.DiffType & DifferenceType.PictureMissingInCurrent) != 0 ||
					(subDiff.DiffType & DifferenceType.PictureDifference) != 0 ||
					subDiff.DiffType == DifferenceType.ParagraphStyleDifference ||
					subDiff.DiffType == DifferenceType.NoDifference, // (structure change only)
					subDiff.DiffType +
					" is not a consistent subtype with split or merged paragraph differences.");
			}
			else
			{
				Assert.IsNotNull(paraCurr, "The current paragraph cannot be null except for para split/merge root diff");
				Assert.IsNotNull(paraRev, "The revision paragraph cannot be null except for para split/merge root diff");

				//check the root difference for consistency with this subDiff
				if (subDiff.DiffType == DifferenceType.VerseMoved)
				// ||
				// subDiff.DiffType == DifferenceType.ParagraphMoved)
				{
					// this subDiff verse or paragraph was moved into an added section
					Assert.IsTrue(rootDiff.DiffType == DifferenceType.SectionAddedToCurrent ||
						rootDiff.DiffType == DifferenceType.SectionMissingInCurrent,
						"inconsistent type of root difference");
				}
				else if (subDiff.DiffType == DifferenceType.TextDifference)
				{
					// this subDiff text difference is within a footnote
					Assert.AreEqual(DifferenceType.FootnoteDifference, rootDiff.DiffType);
				}
				else
					Assert.Fail("unexpected type of sub-diff");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given sub-difference
		/// for a two-sided subDiff representing a text comparison. This overload does not
		/// check for the starting and ending references for sub-diffs that are created without
		/// that information.
		/// </summary>
		/// <param name="rootDiff">The root difference.</param>
		/// <param name="iSubDiff">The sub difference to verify.</param>
		/// <param name="subDiffType">Type of the sub difference.</param>
		/// <param name="footnoteCurr">The footnote curr.</param>
		/// <param name="ichMinCurr">The beginning character offset of the difference in the
		/// current.</param>
		/// <param name="ichLimCurr">The ending character offset of the difference in the
		/// current.</param>
		/// <param name="footnoteRev">The footnote rev.</param>
		/// <param name="ichMinRev">The beginning character offset of the difference in the
		/// revision.</param>
		/// <param name="ichLimRev">The ending character offset of the difference in the
		/// current.</param>
		/// <remarks>char styles are not verified here; test code should just check
		/// those directly if relevant</remarks>
		/// ------------------------------------------------------------------------------------
		public static void VerifySubDiffFootnote(Difference rootDiff, int iSubDiff,
			DifferenceType subDiffType,
			IScrFootnote footnoteCurr, int ichMinCurr, int ichLimCurr,
			IScrFootnote footnoteRev, int ichMinRev, int ichLimRev)
		{
			Difference subDiff = rootDiff.SubDiffsForORCs[iSubDiff];
			// the Current para stuff
			Assert.AreEqual((footnoteCurr != null) ? footnoteCurr.ParagraphsOS[0] : null, subDiff.ParaCurr);
			Assert.AreEqual(ichMinCurr, subDiff.IchMinCurr);
			Assert.AreEqual(ichLimCurr, subDiff.IchLimCurr);

			// the Revision para stuff
			Assert.AreEqual((footnoteRev != null) ? footnoteRev.ParagraphsOS[0] : null, subDiff.ParaRev);
			Assert.AreEqual(ichMinRev, subDiff.IchMinRev);
			Assert.AreEqual(ichLimRev, subDiff.IchLimRev);

			// section stuff should be null
			Assert.IsNull(subDiff.SectionsRev);
			Assert.IsNull(subDiff.SectionsCurr);

			// subDiffs may not have subDiffs, so far
			Assert.IsNull(subDiff.SubDiffsForORCs);
			Assert.IsNull(subDiff.SubDiffsForParas);

			Assert.AreEqual(subDiffType, subDiff.DiffType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given difference and checks that the 0th subdiff
		/// provides only reference points (i.e. IPs).
		/// Intended only for rootdiff types: ParagraphStructureChange, ParagraphSplitInCurrent,
		/// ParagraphMergedInCurrent.
		/// </summary>
		/// <param name="rootDiff">The root diff.</param>
		/// <param name="paraCurr">The para curr.</param>
		/// <param name="ichCurr">The ich curr.</param>
		/// <param name="paraRev">The para rev.</param>
		/// <param name="ichRev">The ich rev.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifySubDiffParaReferencePoints(Difference rootDiff,
			IScrTxtPara paraCurr, int ichCurr, IScrTxtPara paraRev, int ichRev)
		{
			Assert.IsTrue((rootDiff.DiffType & DifferenceType.ParagraphStructureChange) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphSplitInCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphMergedInCurrent) != 0);

			Difference subDiff = rootDiff.SubDiffsForParas[0];
			Assert.AreEqual(DifferenceType.NoDifference, subDiff.DiffType);

			Assert.AreEqual(paraCurr, subDiff.ParaCurr);
			Assert.AreEqual(ichCurr, subDiff.IchMinCurr);
			Assert.AreEqual(ichCurr, subDiff.IchLimCurr);

			Assert.AreEqual(paraRev, subDiff.ParaRev);
			Assert.AreEqual(ichRev, subDiff.IchMinRev);
			Assert.AreEqual(ichRev, subDiff.IchLimRev);

			Assert.IsNull(subDiff.SectionsRev);
			Assert.IsNull(subDiff.SectionsRev);
			Assert.IsNull(subDiff.StyleNameCurr);
			Assert.IsNull(subDiff.StyleNameRev);
			Assert.IsNull(subDiff.SubDiffsForORCs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given difference and given SubDiffForParas
		/// for subdiff types: missing/added paragraphs.
		/// </summary>
		/// <param name="rootDiff">The root diff.</param>
		/// <param name="iSubDiff">The para subdiff index.</param>
		/// <param name="subDiffType">diffType of the subdiff.</param>
		/// <param name="paraAdded">The para added.</param>
		/// <param name="ichLim">The ichlim for the paraAdded. Often this may be the end of the para,
		/// or it may indicate only the first portion (ScrVerse) of the final paragraph.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifySubDiffParaAdded(Difference rootDiff, int iSubDiff,
			DifferenceType subDiffType, IScrTxtPara paraAdded, int ichLim)
		{
			Assert.IsTrue((rootDiff.DiffType & DifferenceType.ParagraphStructureChange) != 0);
			// a ParaAdded/Missing subDiff must not be at index 0 (paragraph reference points must be in that subdiff
			Assert.LessOrEqual(1, iSubDiff);

			Difference subDiff = rootDiff.SubDiffsForParas[iSubDiff];
			Assert.AreEqual(subDiffType, subDiff.DiffType);

			switch (subDiffType)
			{
				case DifferenceType.ParagraphAddedToCurrent:
					Assert.AreEqual(paraAdded, subDiff.ParaCurr);
					Assert.AreEqual(0, subDiff.IchMinCurr);
					Assert.AreEqual(ichLim, subDiff.IchLimCurr); //subDiff may be only first portion of the final paragraph

					Assert.AreEqual(null, subDiff.ParaRev);
					Assert.AreEqual(0, subDiff.IchMinRev);
					Assert.AreEqual(0, subDiff.IchLimRev);
					break;

				case DifferenceType.ParagraphMissingInCurrent:
					Assert.AreEqual(null, subDiff.ParaCurr);
					Assert.AreEqual(0, subDiff.IchMinCurr);
					Assert.AreEqual(0, subDiff.IchLimCurr);

					Assert.AreEqual(paraAdded, subDiff.ParaRev);
					Assert.AreEqual(0, subDiff.IchMinRev);
					Assert.AreEqual(ichLim, subDiff.IchLimRev); //subDiff may be only first portion of the final paragraph
					break;

				default:
					Assert.Fail("Invalid subDiff type for a Paragraph Added/Missing subDiff");
					break;
			}

			Assert.IsNull(subDiff.SectionsRev);
			Assert.IsNull(subDiff.SectionsRev);
			Assert.IsNull(subDiff.StyleNameCurr);
			Assert.IsNull(subDiff.StyleNameRev);
			Assert.IsNull(subDiff.SubDiffsForORCs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given difference
		/// for types: missing/added empty paragraphs.
		/// </summary>
		/// <param name="diff">the given Difference</param>
		/// <param name="startAndEnd">The starting and ending verse ref start.</param>
		/// <param name="type">Type of the diff.</param>
		/// <param name="paraAdded">The paragraph added.</param>
		/// <param name="paraDest">the destination paragraph</param>
		/// <param name="ichDest">The character index in the destination paragraph,
		/// where the added items could be inserted in the other book.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyStanzaBreakAddedDiff(Difference diff,
			BCVRef startAndEnd, DifferenceType type,
			IScrTxtPara paraAdded, /*string strAddedParaStyle,*/ IScrTxtPara paraDest, int ichDest)
		{
			Assert.IsTrue(diff.DiffType == DifferenceType.StanzaBreakAddedToCurrent ||
				diff.DiffType == DifferenceType.StanzaBreakMissingInCurrent);
			//string addedParaStyle = (diff.DiffType == DifferenceType.StanzaBreakAddedToCurrent) ?
			//    diff.StyleNameCurr : diff.StyleNameRev;
			//Assert.AreEqual(strAddedParaStyle, addedParaStyle);

			VerifyParaAddedDiff(diff, startAndEnd, startAndEnd, type, paraAdded, paraDest, ichDest);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given difference
		/// for types: missing/added paragraphs.
		/// </summary>
		/// <param name="diff">the given Difference</param>
		/// <param name="start">The verse ref start.</param>
		/// <param name="end">The verse ref end.</param>
		/// <param name="type">Type of the diff.</param>
		/// <param name="paraAdded">The paragraph added.</param>
		/// <param name="paraDest">the destination paragraph</param>
		/// <param name="ichDest">The character index in the destination paragraph,
		/// where the added items could be inserted in the other book.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyParaAddedDiff(Difference diff,
			BCVRef start, BCVRef end, DifferenceType type,
			IScrTxtPara paraAdded, IScrTxtPara paraDest, int ichDest)
		{
			Assert.AreEqual(start, diff.RefStart);
			Assert.AreEqual(end, diff.RefEnd);
			Assert.AreEqual(type, diff.DiffType);
			switch (type)
			{
				case DifferenceType.ParagraphAddedToCurrent:
					Assert.IsNull(diff.SectionsRev);

					Assert.AreEqual(paraAdded, diff.ParaCurr);
					Assert.AreEqual(0, diff.IchMinCurr);
					Assert.AreEqual(paraAdded.Contents.Length, diff.IchLimCurr);

					Assert.AreEqual(paraDest, diff.ParaRev);
					Assert.AreEqual(ichDest, diff.IchMinRev);
					Assert.AreEqual(ichDest, diff.IchLimRev);

					Assert.IsNull(diff.StyleNameCurr);
					Assert.IsNull(diff.StyleNameRev);
					break;

				case DifferenceType.ParagraphMissingInCurrent:
					Assert.IsNull(diff.SectionsRev);

					Assert.AreEqual(paraDest, diff.ParaCurr);
					Assert.AreEqual(ichDest, diff.IchMinCurr);
					Assert.AreEqual(ichDest, diff.IchLimCurr);

					Assert.AreEqual(paraAdded, diff.ParaRev);
					Assert.AreEqual(0, diff.IchMinRev);
					Assert.AreEqual(paraAdded.Contents.Length, diff.IchLimRev);

					Assert.IsNull(diff.StyleNameCurr);
					Assert.IsNull(diff.StyleNameRev);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for book merger tests-
		/// Verifies the contents of the given difference
		/// of type Missing/Added Section/SectionHead.
		/// </summary>
		/// <remarks>subDiffs are not verified here; test code should check those directly
		/// if relevant</remarks>
		/// <param name="diff">the given Difference</param>
		/// <param name="start">The verse ref start.</param>
		/// <param name="end">The verse ref end.</param>
		/// <param name="type">Type of the diff.</param>
		/// <param name="sectionsAdded">Sections that were added (this can be single one or an
		/// array of them; the code will be smart enough to figure out which and act accordingly)</param>
		/// <param name="paraDest">The destination paragraph</param>
		/// <param name="ichDest">The character index in the destination paragraph,
		/// where the added items could be inserted in the other book.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifySectionDiff(Difference diff,
			BCVRef start, BCVRef end, DifferenceType type,
			object sectionsAdded, IScrTxtPara paraDest, int ichDest)
		{
			Assert.AreEqual(start, diff.RefStart);
			Assert.AreEqual(end, diff.RefEnd);
			Assert.AreEqual(type, diff.DiffType);
			switch (type)
			{
				case DifferenceType.SectionAddedToCurrent:
				case DifferenceType.SectionHeadAddedToCurrent:
					if (sectionsAdded is IScrSection)
					{
						Assert.AreEqual(1, diff.SectionsCurr.Count());
						Assert.AreEqual(sectionsAdded, diff.SectionsCurr.First());
					}
					else if (sectionsAdded is IScrSection[])
						Assert.IsTrue(ArrayUtils.AreEqual((IScrSection[])sectionsAdded, diff.SectionsCurr));
					else
						Assert.Fail("Invalid parameter type");

					Assert.IsNull(diff.SectionsRev);

					Assert.AreEqual(null, diff.ParaCurr);
					Assert.AreEqual(0, diff.IchMinCurr);
					Assert.AreEqual(0, diff.IchLimCurr);

					Assert.AreEqual(paraDest, diff.ParaRev);
					Assert.AreEqual(ichDest, diff.IchMinRev);
					Assert.AreEqual(ichDest, diff.IchLimRev);

					Assert.IsNull(diff.StyleNameCurr);
					Assert.IsNull(diff.StyleNameRev);
					break;

				case DifferenceType.SectionMissingInCurrent:
				case DifferenceType.SectionHeadMissingInCurrent:
					if (sectionsAdded is IScrSection)
					{
						Assert.AreEqual(1, diff.SectionsRev.Count());
						Assert.AreEqual(sectionsAdded, diff.SectionsRev.First());
					}
					else if (sectionsAdded is IScrSection[])
						Assert.IsTrue(ArrayUtils.AreEqual((IScrSection[])sectionsAdded, diff.SectionsRev));
					else
						Assert.Fail("Invalid parameter type");

					Assert.IsNull(diff.SectionsCurr);

					Assert.AreEqual(paraDest, diff.ParaCurr);
					Assert.AreEqual(ichDest, diff.IchMinCurr);
					Assert.AreEqual(ichDest, diff.IchLimCurr);

					Assert.AreEqual(null, diff.ParaRev);
					Assert.AreEqual(0, diff.IchMinRev);
					Assert.AreEqual(0, diff.IchLimRev);

					Assert.IsNull(diff.StyleNameCurr);
					Assert.IsNull(diff.StyleNameRev);
					break;
				default:
					Assert.Fail("test called wrong verify method or something");
					break;
			}
		}

		/// -----------------------------------------------------------------------------------
		///<summary>
		/// Verify the specified ScrVerse
		///</summary>
		///<param name="verse">specified ScrVerse</param>
		///<param name="verseText">expected text within the ScrVerse</param>
		///<param name="styleName">expected stylename for the ScrVerse paragraph</param>
		///<param name="startRef">expected starting reference</param>
		///<param name="endRef">expected ending reference</param>
		/// -----------------------------------------------------------------------------------
		public static void VerifyScrVerse(ScrVerse verse, string verseText, string styleName,
					BCVRef startRef, BCVRef endRef)
		{
			IScrTxtPara versePara = verse.Para;
			if (string.IsNullOrEmpty(verseText))
				Assert.IsTrue(verse.Text == null || string.IsNullOrEmpty(verse.Text.Text));
			else
				Assert.AreEqual(verseText, verse.Text.Text);
			Assert.AreEqual(styleName, versePara.StyleName);
			Assert.AreEqual(startRef, verse.StartRef);
			Assert.AreEqual(endRef, verse.EndRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method for verifying a ScrVerse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void VerifyScrVerse(ScrVerse scrVerse, IScrTxtPara para, int startRef, int endRef,
			string verseText, int iVerseStart, bool fIsChapter, bool fIsHeading, int iSection)
		{
			Assert.AreEqual(para, scrVerse.Para);
			Assert.AreEqual(startRef, scrVerse.StartRef);
			Assert.AreEqual(endRef, scrVerse.EndRef);
			Assert.AreEqual(verseText, scrVerse.Text.Text);
			Assert.AreEqual(iVerseStart, scrVerse.VerseStartIndex);
			Assert.AreEqual(fIsChapter, scrVerse.ChapterNumberRun);
			// check the ParaNodeMap too
			Assert.AreEqual(ScrBookTags.kflidSections, scrVerse.ParaNodeMap.BookFlid);
			Assert.AreEqual(iSection, scrVerse.ParaNodeMap.SectionIndex);
			Assert.AreEqual(fIsHeading ? ScrSectionTags.kflidHeading :
				ScrSectionTags.kflidContent, scrVerse.ParaNodeMap.SectionFlid);
			Assert.AreEqual(0, scrVerse.ParaNodeMap.ParaIndex);
			ParaNodeMap map = new ParaNodeMap(para);
			Assert.IsTrue(map.Equals(scrVerse.ParaNodeMap));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates dummy paras.
		/// </summary>
		/// <returns>An array of paras</returns>
		/// ------------------------------------------------------------------------------------
		public static IScrTxtPara[] CreateDummyParas(int count, FdoCache cache)
		{
			IText text = cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//cache.LangProject.TextsOC.Add(text);
			IStText stText = cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;

			IScrTxtPara[] paras = new IScrTxtPara[count];
			for (int i = 0; i < paras.Length; i++)
			{
				paras[i] = cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
					stText, ScrStyleNames.NormalParagraph);
			}

			return paras;
		}

	}
}
