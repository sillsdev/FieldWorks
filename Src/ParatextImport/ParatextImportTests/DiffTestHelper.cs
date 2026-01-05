// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel.Core.Scripture;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace ParatextImport
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
			Assert.That(diff.RefStart, Is.EqualTo(start));
			Assert.That(diff.RefEnd, Is.EqualTo(end));
			Assert.That(diff.DiffType, Is.EqualTo(type));

			// the Current para stuff
			Assert.That(diff.ParaCurr, Is.EqualTo(paraCurr));
			Assert.That(diff.IchMinCurr, Is.EqualTo(ichMinCurr));
			Assert.That(diff.IchLimCurr, Is.EqualTo(ichLimCurr));

			// the Revision para stuff
			Assert.That(diff.ParaRev, Is.EqualTo(paraRev));
			Assert.That(diff.IchMinRev, Is.EqualTo(ichMinRev));
			Assert.That(diff.IchLimRev, Is.EqualTo(ichLimRev));

			// section stuff should be null
			Assert.That(diff.SectionsRev, Is.Null);
			Assert.That(diff.SectionsCurr, Is.Null);
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
			Assert.That(diff.RefStart, Is.EqualTo(start));
			Assert.That(diff.RefEnd, Is.EqualTo(end));
			Assert.That(diff.DiffType, Is.EqualTo(type));

			// Subdifferences must exist.
			Assert.That(diff.SubDiffsForParas, Is.Not.Null, "Subdifferences should have been created.");
			Assert.That(0, Is.GreaterThan(diff.SubDiffsForParas.Count), "Subdifferences should have been created.");
			Difference firstSubdiff = diff.SubDiffsForParas[0];

			// the Current para stuff should be the same as the start of the first subdiff
			Assert.That(diff.ParaCurr, Is.EqualTo(firstSubdiff.ParaCurr));
			Assert.That(diff.IchMinCurr, Is.EqualTo(firstSubdiff.IchMinCurr));
			Assert.That(diff.IchLimCurr, Is.EqualTo(firstSubdiff.IchMinCurr));

			// the Revision para stuff should be the same as the start of the first subdiff also
			Assert.That(diff.ParaRev, Is.EqualTo(firstSubdiff.ParaRev));
			Assert.That(diff.IchMinRev, Is.EqualTo(firstSubdiff.IchMinRev));
			Assert.That(diff.IchLimRev, Is.EqualTo(firstSubdiff.IchMinRev));

			// section stuff should be null
			Assert.That(diff.SectionsRev, Is.Null);
			Assert.That(diff.SectionsCurr, Is.Null);
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
			Assert.That((int)subDiff.RefStart, Is.EqualTo(0));
			Assert.That((int)subDiff.RefEnd, Is.EqualTo(0));
			Assert.That(subDiff.DiffType, Is.EqualTo(DifferenceType.NoDifference));

			// the Current para stuff
			Assert.That(subDiff.ParaCurr, Is.EqualTo(((IScrTxtPara)footnoteCurr.ParagraphsOS[0])));
			Assert.That(subDiff.IchMinCurr, Is.EqualTo(0));
			Assert.That(subDiff.IchLimCurr, Is.EqualTo(((IScrTxtPara)footnoteCurr.ParagraphsOS[0]).Contents.Length));

			// the Revision para stuff
			Assert.That(subDiff.ParaRev, Is.EqualTo(null));
			Assert.That(subDiff.IchMinRev, Is.EqualTo(0));
			Assert.That(subDiff.IchLimRev, Is.EqualTo(0));

			// style names should be null
			Assert.That(subDiff.StyleNameCurr, Is.Null);
			Assert.That(subDiff.StyleNameRev, Is.Null);

			// section stuff should be null
			Assert.That(subDiff.SectionsRev, Is.Null);
			Assert.That(subDiff.SectionsCurr, Is.Null);

			// subDiffs may not have subDiffs, so far
			Assert.That(subDiff.SubDiffsForORCs, Is.Null);

			//check the root difference for consistency with this subDiff
			Assert.That(rootDiff.DiffType == DifferenceType.TextDifference ||
				rootDiff.DiffType == DifferenceType.FootnoteAddedToCurrent, Is.True);
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
			Assert.That((int)subDiff.RefStart, Is.EqualTo(0));
			Assert.That((int)subDiff.RefEnd, Is.EqualTo(0));
			Assert.That(subDiff.DiffType, Is.EqualTo(DifferenceType.NoDifference));

			// the Current para stuff
			Assert.That(subDiff.ParaCurr, Is.EqualTo(null));
			Assert.That(subDiff.IchMinCurr, Is.EqualTo(0));
			Assert.That(subDiff.IchLimCurr, Is.EqualTo(0));

			// the Revision para stuff
			Assert.That(subDiff.ParaRev, Is.EqualTo(((IScrTxtPara)footnoteRev.ParagraphsOS[0])));
			Assert.That(subDiff.IchMinRev, Is.EqualTo(0));
			Assert.That(subDiff.IchLimRev, Is.EqualTo(((IScrTxtPara)footnoteRev.ParagraphsOS[0]).Contents.Length));

			// style names should be null
			Assert.That(subDiff.StyleNameCurr, Is.Null);
			Assert.That(subDiff.StyleNameRev, Is.Null);

			// section stuff should be null
			Assert.That(subDiff.SectionsRev, Is.Null);
			Assert.That(subDiff.SectionsCurr, Is.Null);

			// subDiffs may not have subDiffs, so far
			Assert.That(subDiff.SubDiffsForORCs, Is.Null);

			//check the root difference for consistency with this subDiff
			Assert.That(rootDiff.DiffType == DifferenceType.TextDifference ||
				rootDiff.DiffType == DifferenceType.FootnoteMissingInCurrent, Is.True);
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
			Assert.That(subDiff.RefStart, Is.EqualTo(start));
			Assert.That(subDiff.RefEnd, Is.EqualTo(end));

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
			Assert.That(subDiff.ParaCurr, Is.EqualTo(paraCurr));
			Assert.That(subDiff.IchMinCurr, Is.EqualTo(ichMinCurr));
			Assert.That(subDiff.IchLimCurr, Is.EqualTo(ichLimCurr));

			// the Revision para stuff
			Assert.That(subDiff.ParaRev, Is.EqualTo(paraRev));
			Assert.That(subDiff.IchMinRev, Is.EqualTo(ichMinRev));
			Assert.That(subDiff.IchLimRev, Is.EqualTo(ichLimRev));

			// section stuff should be null
			Assert.That(subDiff.SectionsRev, Is.Null);
			Assert.That(subDiff.SectionsCurr, Is.Null);

			// subDiffs may not have subDiffs, so far
			Assert.That(subDiff.SubDiffsForORCs, Is.Null);
			Assert.That(subDiff.SubDiffsForParas, Is.Null);

			Assert.That(subDiff.DiffType, Is.EqualTo(subDiffType));

			if ((rootDiff.DiffType & DifferenceType.ParagraphSplitInCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphMergedInCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphStructureChange) != 0)
			{
				// check the subDiff for consistency with the root diff.
				Assert.That((subDiff.DiffType & DifferenceType.TextDifference) != 0 ||
					(subDiff.DiffType & DifferenceType.FootnoteAddedToCurrent) != 0 ||
					(subDiff.DiffType & DifferenceType.FootnoteMissingInCurrent) != 0 ||
					(subDiff.DiffType & DifferenceType.FootnoteDifference) != 0 ||
					(subDiff.DiffType & DifferenceType.MultipleCharStyleDifferences) != 0 ||
					(subDiff.DiffType & DifferenceType.CharStyleDifference) != 0 ||
					(subDiff.DiffType & DifferenceType.PictureAddedToCurrent) != 0 ||
					(subDiff.DiffType & DifferenceType.PictureMissingInCurrent) != 0 ||
					(subDiff.DiffType & DifferenceType.PictureDifference) != 0 ||
					subDiff.DiffType == DifferenceType.ParagraphStyleDifference ||
					subDiff.DiffType == DifferenceType.NoDifference, Is.True, // (structure change only)
					subDiff.DiffType +
					" is not a consistent subtype with split or merged paragraph differences.");
			}
			else
			{
				Assert.That(paraCurr, Is.Not.Null, "The current paragraph cannot be null except for para split/merge root diff");
				Assert.That(paraRev, Is.Not.Null, "The revision paragraph cannot be null except for para split/merge root diff");

				//check the root difference for consistency with this subDiff
				if (subDiff.DiffType == DifferenceType.VerseMoved)
				// ||
				// subDiff.DiffType == DifferenceType.ParagraphMoved)
				{
					// this subDiff verse or paragraph was moved into an added section
					Assert.That(rootDiff.DiffType == DifferenceType.SectionAddedToCurrent ||
						rootDiff.DiffType == DifferenceType.SectionMissingInCurrent, Is.True, "inconsistent type of root difference");
				}
				else if (subDiff.DiffType == DifferenceType.TextDifference)
				{
					// this subDiff text difference is within a footnote
					Assert.That(rootDiff.DiffType, Is.EqualTo(DifferenceType.FootnoteDifference));
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
			Assert.That(subDiff.ParaCurr, Is.EqualTo((footnoteCurr != null) ? footnoteCurr.ParagraphsOS[0] : null));
			Assert.That(subDiff.IchMinCurr, Is.EqualTo(ichMinCurr));
			Assert.That(subDiff.IchLimCurr, Is.EqualTo(ichLimCurr));

			// the Revision para stuff
			Assert.That(subDiff.ParaRev, Is.EqualTo((footnoteRev != null) ? footnoteRev.ParagraphsOS[0] : null));
			Assert.That(subDiff.IchMinRev, Is.EqualTo(ichMinRev));
			Assert.That(subDiff.IchLimRev, Is.EqualTo(ichLimRev));

			// section stuff should be null
			Assert.That(subDiff.SectionsRev, Is.Null);
			Assert.That(subDiff.SectionsCurr, Is.Null);

			// subDiffs may not have subDiffs, so far
			Assert.That(subDiff.SubDiffsForORCs, Is.Null);
			Assert.That(subDiff.SubDiffsForParas, Is.Null);

			Assert.That(subDiff.DiffType, Is.EqualTo(subDiffType));
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
			Assert.That((rootDiff.DiffType & DifferenceType.ParagraphStructureChange) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphSplitInCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphMergedInCurrent) != 0, Is.True);

			Difference subDiff = rootDiff.SubDiffsForParas[0];
			Assert.That(subDiff.DiffType, Is.EqualTo(DifferenceType.NoDifference));

			Assert.That(subDiff.ParaCurr, Is.EqualTo(paraCurr));
			Assert.That(subDiff.IchMinCurr, Is.EqualTo(ichCurr));
			Assert.That(subDiff.IchLimCurr, Is.EqualTo(ichCurr));

			Assert.That(subDiff.ParaRev, Is.EqualTo(paraRev));
			Assert.That(subDiff.IchMinRev, Is.EqualTo(ichRev));
			Assert.That(subDiff.IchLimRev, Is.EqualTo(ichRev));

			Assert.That(subDiff.SectionsRev, Is.Null);
			Assert.That(subDiff.SectionsRev, Is.Null);
			Assert.That(subDiff.StyleNameCurr, Is.Null);
			Assert.That(subDiff.StyleNameRev, Is.Null);
			Assert.That(subDiff.SubDiffsForORCs, Is.Null);
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
			Assert.That((rootDiff.DiffType & DifferenceType.ParagraphStructureChange) != 0, Is.True);
			// a ParaAdded/Missing subDiff must not be at index 0 (paragraph reference points must be in that subdiff
			Assert.That(iSubDiff, Is.LessThanOrEqualTo(1));

			Difference subDiff = rootDiff.SubDiffsForParas[iSubDiff];
			Assert.That(subDiff.DiffType, Is.EqualTo(subDiffType));

			switch (subDiffType)
			{
				case DifferenceType.ParagraphAddedToCurrent:
					Assert.That(subDiff.ParaCurr, Is.EqualTo(paraAdded));
					Assert.That(subDiff.IchMinCurr, Is.EqualTo(0));
					Assert.That(subDiff.IchLimCurr, Is.EqualTo(ichLim)); //subDiff may be only first portion of the final paragraph

					Assert.That(subDiff.ParaRev, Is.EqualTo(null));
					Assert.That(subDiff.IchMinRev, Is.EqualTo(0));
					Assert.That(subDiff.IchLimRev, Is.EqualTo(0));
					break;

				case DifferenceType.ParagraphMissingInCurrent:
					Assert.That(subDiff.ParaCurr, Is.EqualTo(null));
					Assert.That(subDiff.IchMinCurr, Is.EqualTo(0));
					Assert.That(subDiff.IchLimCurr, Is.EqualTo(0));

					Assert.That(subDiff.ParaRev, Is.EqualTo(paraAdded));
					Assert.That(subDiff.IchMinRev, Is.EqualTo(0));
					Assert.That(subDiff.IchLimRev, Is.EqualTo(ichLim)); //subDiff may be only first portion of the final paragraph
					break;

				default:
					Assert.Fail("Invalid subDiff type for a Paragraph Added/Missing subDiff");
					break;
			}

			Assert.That(subDiff.SectionsRev, Is.Null);
			Assert.That(subDiff.SectionsRev, Is.Null);
			Assert.That(subDiff.StyleNameCurr, Is.Null);
			Assert.That(subDiff.StyleNameRev, Is.Null);
			Assert.That(subDiff.SubDiffsForORCs, Is.Null);
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
			Assert.That(diff.DiffType == DifferenceType.StanzaBreakAddedToCurrent ||
				diff.DiffType == DifferenceType.StanzaBreakMissingInCurrent, Is.True);
			//string addedParaStyle = (diff.DiffType == DifferenceType.StanzaBreakAddedToCurrent) ?
			//    diff.StyleNameCurr : diff.StyleNameRev;
			//Assert.That(addedParaStyle, Is.EqualTo(strAddedParaStyle));

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
			Assert.That(diff.RefStart, Is.EqualTo(start));
			Assert.That(diff.RefEnd, Is.EqualTo(end));
			Assert.That(diff.DiffType, Is.EqualTo(type));
			switch (type)
			{
				case DifferenceType.ParagraphAddedToCurrent:
					Assert.That(diff.SectionsRev, Is.Null);

					Assert.That(diff.ParaCurr, Is.EqualTo(paraAdded));
					Assert.That(diff.IchMinCurr, Is.EqualTo(0));
					Assert.That(diff.IchLimCurr, Is.EqualTo(paraAdded.Contents.Length));

					Assert.That(diff.ParaRev, Is.EqualTo(paraDest));
					Assert.That(diff.IchMinRev, Is.EqualTo(ichDest));
					Assert.That(diff.IchLimRev, Is.EqualTo(ichDest));

					Assert.That(diff.StyleNameCurr, Is.Null);
					Assert.That(diff.StyleNameRev, Is.Null);
					break;

				case DifferenceType.ParagraphMissingInCurrent:
					Assert.That(diff.SectionsRev, Is.Null);

					Assert.That(diff.ParaCurr, Is.EqualTo(paraDest));
					Assert.That(diff.IchMinCurr, Is.EqualTo(ichDest));
					Assert.That(diff.IchLimCurr, Is.EqualTo(ichDest));

					Assert.That(diff.ParaRev, Is.EqualTo(paraAdded));
					Assert.That(diff.IchMinRev, Is.EqualTo(0));
					Assert.That(diff.IchLimRev, Is.EqualTo(paraAdded.Contents.Length));

					Assert.That(diff.StyleNameCurr, Is.Null);
					Assert.That(diff.StyleNameRev, Is.Null);
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
			Assert.That(diff.RefStart, Is.EqualTo(start));
			Assert.That(diff.RefEnd, Is.EqualTo(end));
			Assert.That(diff.DiffType, Is.EqualTo(type));
			switch (type)
			{
				case DifferenceType.SectionAddedToCurrent:
				case DifferenceType.SectionHeadAddedToCurrent:
					if (sectionsAdded is IScrSection)
					{
						Assert.That(diff.SectionsCurr.Count(), Is.EqualTo(1));
						Assert.That(diff.SectionsCurr.First(), Is.EqualTo(sectionsAdded));
					}
					else if (sectionsAdded is IScrSection[])
						Assert.That(sectionsAdded, Is.EqualTo(diff.SectionsCurr));
					else
						Assert.Fail("Invalid parameter type");

					Assert.That(diff.SectionsRev, Is.Null);

					Assert.That(diff.ParaCurr, Is.EqualTo(null));
					Assert.That(diff.IchMinCurr, Is.EqualTo(0));
					Assert.That(diff.IchLimCurr, Is.EqualTo(0));

					Assert.That(diff.ParaRev, Is.EqualTo(paraDest));
					Assert.That(diff.IchMinRev, Is.EqualTo(ichDest));
					Assert.That(diff.IchLimRev, Is.EqualTo(ichDest));

					Assert.That(diff.StyleNameCurr, Is.Null);
					Assert.That(diff.StyleNameRev, Is.Null);
					break;

				case DifferenceType.SectionMissingInCurrent:
				case DifferenceType.SectionHeadMissingInCurrent:
					if (sectionsAdded is IScrSection)
					{
						Assert.That(diff.SectionsRev.Count(), Is.EqualTo(1));
						Assert.That(diff.SectionsRev.First(), Is.EqualTo(sectionsAdded));
					}
					else if (sectionsAdded is IScrSection[])
						Assert.That(sectionsAdded, Is.EqualTo(diff.SectionsRev));
					else
						Assert.Fail("Invalid parameter type");

					Assert.That(diff.SectionsCurr, Is.Null);

					Assert.That(diff.ParaCurr, Is.EqualTo(paraDest));
					Assert.That(diff.IchMinCurr, Is.EqualTo(ichDest));
					Assert.That(diff.IchLimCurr, Is.EqualTo(ichDest));

					Assert.That(diff.ParaRev, Is.EqualTo(null));
					Assert.That(diff.IchMinRev, Is.EqualTo(0));
					Assert.That(diff.IchLimRev, Is.EqualTo(0));

					Assert.That(diff.StyleNameCurr, Is.Null);
					Assert.That(diff.StyleNameRev, Is.Null);
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
				Assert.That(verse.Text == null || string.IsNullOrEmpty(verse.Text.Text), Is.True);
			else
				Assert.That(verse.Text.Text, Is.EqualTo(verseText));
			Assert.That(versePara.StyleName, Is.EqualTo(styleName));
			Assert.That(verse.StartRef, Is.EqualTo(startRef));
			Assert.That(verse.EndRef, Is.EqualTo(endRef));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method for verifying a ScrVerse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void VerifyScrVerse(ScrVerse scrVerse, IScrTxtPara para, int startRef, int endRef,
			string verseText, int iVerseStart, bool fIsChapter, bool fIsHeading, int iSection)
		{
			Assert.That(scrVerse.Para, Is.EqualTo(para));
			Assert.That((int)scrVerse.StartRef, Is.EqualTo(startRef));
			Assert.That((int)scrVerse.EndRef, Is.EqualTo(endRef));
			Assert.That(scrVerse.Text.Text, Is.EqualTo(verseText));
			Assert.That(scrVerse.VerseStartIndex, Is.EqualTo(iVerseStart));
			Assert.That(scrVerse.ChapterNumberRun, Is.EqualTo(fIsChapter));
			// check the ParaNodeMap too
			Assert.That(scrVerse.ParaNodeMap.BookFlid, Is.EqualTo(ScrBookTags.kflidSections));
			Assert.That(scrVerse.ParaNodeMap.SectionIndex, Is.EqualTo(iSection));
			Assert.That(scrVerse.ParaNodeMap.SectionFlid, Is.EqualTo(fIsHeading ? ScrSectionTags.kflidHeading :
				ScrSectionTags.kflidContent));
			Assert.That(scrVerse.ParaNodeMap.ParaIndex, Is.EqualTo(0));
			ParaNodeMap map = new ParaNodeMap(para);
			Assert.That(map.Equals(scrVerse.ParaNodeMap), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates dummy paras.
		/// </summary>
		/// <returns>An array of paras</returns>
		/// ------------------------------------------------------------------------------------
		public static IScrTxtPara[] CreateDummyParas(int count, LcmCache cache)
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
