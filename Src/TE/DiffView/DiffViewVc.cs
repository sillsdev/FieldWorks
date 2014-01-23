// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DiffViewVc.cs
// Responsibility: TeTeam
//
// <remarks>
// Implements the view constructor for a diff view
// </remarks>
// --------------------------------------------------------------------------------------------

using System.Drawing;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class displays the diff view.
	/// </summary>
	///  ----------------------------------------------------------------------------------------
	internal class DiffViewVc: DraftViewVc
	{
		#region Member variables
		private DifferenceList m_Differences;
		private bool m_fRev;
		private bool m_fNeedHighlight;
		internal static readonly uint kHighlightColor = (uint)ColorTranslator.ToWin32(Color.Yellow);
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DiffViewVc class
		/// </summary>
		/// <param name="differences">List of differences</param>
		/// <param name="fRev"><c>true</c> for revision, <c>false</c> for current version</param>
		/// <param name="cache">The database cache</param>
		/// ------------------------------------------------------------------------------------
		public DiffViewVc(DifferenceList differences, bool fRev, FdoCache cache) :
			base(TeStVc.LayoutViewTarget.targetDraft, 0, null, false)
		{
			m_Differences = differences;
			m_fRev = fRev;
			m_fNeedHighlight = true; // assume we start needing the highlight

			// Start the view as editable.  Non-editable mode is now handled in the DiffView by
			// eating the keyboard input.  This allows movement of the IP inside of a read-only
			// DiffView.
			Editable = true;

			Cache = cache;

			// Because this is a diff view, we want to show trailing white space
			m_fShowTailingSpace = true;
		}
		#endregion

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_DispPropOverrides != null)
					m_DispPropOverrides.Clear(); // Should these be disposed, as well?
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_Differences = null;
			m_DispPropOverrides = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the view needs highlighting.
		/// A diff pane in Edit mode does not use highlighting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NeedHighlight
		{
			get
			{
				CheckDisposed();
				return m_fNeedHighlight;
			}
			set
			{
				CheckDisposed();
				m_fNeedHighlight = value;
			}
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// A Scripture is displayed by displaying its Books;
		/// and a Book is displayed by displaying its Title and Sections;
		/// and a Section is diplayed by displaying its Heading and Content;
		/// which are displayed by using the standard view constructor for StText.
		///
		/// This override provides special difference highlighting for a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch(frag)
			{
				case (int)StTextFrags.kfrPara:
				{
					// The hvo may refer to m_Differences.CurrentDifference, or to a subdiff,
					//  so find the correct one.
					IScrTxtPara para = Cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().GetObject(hvo);
					Difference diff = FindDiff(para);

					m_DispPropOverrides.Clear();

					// If the diff represents added sections, and this paragraph belongs to it,
					// we must highlight the whole para
					bool fDisplayMissingParaPlaceholderAfter = false;
					if (diff != null)
					{
						bool highlightWholePara = diff.IncludesWholePara(para, m_fRev);

						// If the given paragraph has differences to be highlighted,
						//  add appropriate properties
						if ((diff.GetPara(m_fRev) == para || highlightWholePara) && m_fNeedHighlight)
						{
							// Need to add override properties for each run in the
							// range to be highlighted.
							// Determine the range of the paragraph that we want to highlight.
							int paraMinHighlight = highlightWholePara ? 0 : diff.GetIchMin(m_fRev);
							int paraLimHighlight = highlightWholePara ?
								para.Contents.Length : diff.GetIchLim(m_fRev);
							if (paraMinHighlight == paraLimHighlight &&
								IsParagraphAdditionOrDeletion(diff.DiffType))
							{
								if (paraMinHighlight == 0)
									InsertMissingContentPara(vwenv);
								else
									fDisplayMissingParaPlaceholderAfter = true;
							}

							MakeDispPropOverrides(para, paraMinHighlight, paraLimHighlight,
								delegate(ref DispPropOverride prop)
								{
									prop.chrp.clrBack = kHighlightColor;
								});
						}
					}

					// the base Display will do the actual displaying of the Para frag
					base.Display(vwenv, hvo, frag);
					if (fDisplayMissingParaPlaceholderAfter)
						InsertMissingContentPara(vwenv);
					break;
				}

				default:
					// handle all other frags
					base.Display(vwenv, hvo, frag);
					break;
			}
		}



		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified type of difference is one which, if "Use this
		/// Version" (i.e., revert to old) is clicked, would result in deletion of a significant
		/// amount of data (a whole verse or more).
		/// </summary>
		/// <param name="diffType">Type of the diff.</param>
		/// ------------------------------------------------------------------------------------
		internal static bool IsParagraphAdditionOrDeletion(DifferenceType diffType)
		{
			return ((diffType & DifferenceType.ParagraphAddedToCurrent) != 0 ||
				(diffType & DifferenceType.ParagraphMissingInCurrent) != 0 ||
				(diffType & DifferenceType.SectionHeadAddedToCurrent) != 0 ||
				(diffType & DifferenceType.SectionHeadMissingInCurrent) != 0 ||
				(diffType & DifferenceType.SectionAddedToCurrent) != 0 ||
				(diffType & DifferenceType.SectionMissingInCurrent) != 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the missing content paragraph.
		/// </summary>
		/// <param name="vwenv">The view environment.</param>
		/// ------------------------------------------------------------------------------------
		private void InsertMissingContentPara(IVwEnv vwenv)
		{
			vwenv.OpenParagraph();
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault, (int)kHighlightColor);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.AddString(TsStringUtils.MakeTss(
				TeDiffViewResources.kstidContentMissing, Cache.DefaultUserWs));
			vwenv.CloseParagraph();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert end of paragraph or section marks, if needed. Highlight the markers if
		/// they are part of a diff range in a paragraph split, merge or structure change.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="paraHvo"></param>
		/// ------------------------------------------------------------------------------------
		protected override void InsertEndOfParaMarks(IVwEnv vwenv, int paraHvo)
		{
			IStTxtPara para = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraHvo);
			Difference rootDiff = m_Differences.CurrentDifference;
			bool fParaNeedsBoundaryHighlight = NeedsBoundaryHighlight(rootDiff, para);
			if (m_target == LayoutViewTarget.targetDraft && fParaNeedsBoundaryHighlight)
			{
				// Set up for an end mark.

				// If this is the last paragraph of a section then insert an
				// end of section mark, otherwise insert a paragraph mark.
				VwBoundaryMark boundaryMark;
				IFdoOwningSequence<IStPara> paraArray = ((IStText)para.Owner).ParagraphsOS;
				if (para == paraArray[paraArray.Count - 1])
					boundaryMark = VwBoundaryMark.endofSectionHighlighted; // "§"
				else
					boundaryMark = VwBoundaryMark.endOfParagraphHighlighted; // "¶"
				vwenv.SetParagraphMark(boundaryMark);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the appropriate "OpenPara" method.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="paraHvo">the HVO of the paragraph</param>
		/// ------------------------------------------------------------------------------------
		protected override void OpenPara(IVwEnv vwenv, int paraHvo)
		{
			vwenv.OpenOverridePara(m_DispPropOverrides.Count, m_DispPropOverrides.ToArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't show separators in Diff view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InsertBookSeparator(int hvoScrBook, IVwEnv vwenv)
		{
			// Do nothing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Never display a user prompt in diff view.
		/// </summary>
		/// <param name="vwenv">view environment</param>
		/// <param name="paraHvo">the HVO of the paragraph to be displayed</param>
		/// <returns>false</returns>
		/// -----------------------------------------------------------------------------------
		protected override bool InsertParaContentsUserPrompt(IVwEnv vwenv, int paraHvo)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override base and do nothing (e.g., if user clicks a footnote caller in diff view,
		/// we don't do anything).
		/// </summary>
		/// <param name="strData"></param>
		/// <param name="sda"></param>
		/// ------------------------------------------------------------------------------------
		public override void DoHotLinkAction(string strData, ISilDataAccess sda)
		{
			CheckDisposed();

		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the current paragraph boundary marker needs to be highlighted.
		/// </summary>
		/// <param name="rootDiff">The root difference.</param>
		/// <param name="para">The current paragraph.</param>
		/// <returns><c>true</c> if the paragraph needs its boundary marker highlighted;
		/// <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool NeedsBoundaryHighlight(Difference rootDiff, IStTxtPara para)
		{
			// If our difference involves a paragraph split or merge and our paragraph is not
			// in a last subdifference . . .
			if (rootDiff != null &&
				((rootDiff.DiffType & DifferenceType.ParagraphMergedInCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphSplitInCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.ParagraphStructureChange) != 0 ||
				(rootDiff.DiffType & DifferenceType.StanzaBreakAddedToCurrent) != 0 ||
				(rootDiff.DiffType & DifferenceType.StanzaBreakMissingInCurrent) != 0) &&
				ParaBreakNeedsHighlight(rootDiff, para))
			{
				// this paragraph needs to have the boundary character highlighted.
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified paragraph hvo is in the root diff and needs to be
		/// highlighted (i.e. it is not the last paragraph in the diff).
		/// </summary>
		/// <param name="rootDiff">The root diff.</param>
		/// <param name="para">The current paragraph.</param>
		/// <returns><c>true</c> if paragraph hvo is referenced in a last subdifference;
		/// <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool ParaBreakNeedsHighlight(Difference rootDiff, IStTxtPara para)
		{
			Debug.Assert(rootDiff != null);
			if (rootDiff.HasParaSubDiffs)
			{
				for (int iSubDiff = 0; iSubDiff < rootDiff.SubDiffsForParas.Count; iSubDiff++)
				{
					// If we found the current paragraph in the subdiffs . . .
					if (rootDiff.SubDiffsForParas[iSubDiff].GetPara(m_fRev) == para)
					{
						// now determine if it is the last one. We don't highlight the paragraph
						// in the last subdifference because the difference does not span the para break.
						// We also don't highlight the paragraph break if it is the only paragraph
						// in the Revision or Current (the last subdifference would have a
						// paragraph hvo of 0 if it was the only paragraph).
						return (iSubDiff < rootDiff.SubDiffsForParas.Count - 1 &&
							rootDiff.SubDiffsForParas[rootDiff.SubDiffsForParas.Count - 1].GetPara(m_fRev) != null);
					}
				}
			}
			else
			{
				//Check for StanzaBreakAdded/Missing in root diff
				if ((rootDiff.DiffType & DifferenceType.StanzaBreakAddedToCurrent) != 0 ||
					(rootDiff.DiffType & DifferenceType.StanzaBreakMissingInCurrent) != 0)
				{
					return rootDiff.GetPara(m_fRev) == para;
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the diff that the view constructor needs at this time.
		/// Normally it needs the CurrentDifference, but for differences that contain
		/// SubDiffs for Paragraphs, the subdiff may be the one that matches the hvo being
		/// displayed. In that case, return the subDiff instead of the CurrentDifference.
		/// </summary>
		/// <param name="para">The para that Display is attempting to draw.</param>
		/// <returns>the difference that the view constructor needs at this time</returns>
		/// ------------------------------------------------------------------------------------
		private Difference FindDiff(IStTxtPara para)
		{
			Difference diff = m_Differences.CurrentDifference;
			if (diff == null)
				return null;
			if ((diff.DiffType & DifferenceType.ParagraphMergedInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.ParagraphSplitInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.ParagraphStructureChange) != 0)
			{
				Debug.Assert(diff.HasParaSubDiffs);
				// The main diff does not reference the hvo, so we need to search for a reference
				// to the hvo in the subdifferences for paragraphs.
				foreach (Difference subDiff in diff.SubDiffsForParas)
				{
					if (subDiff.DiffType != DifferenceType.NoDifference && para == subDiff.GetPara(m_fRev))
						return subDiff;
				}
			}

			// return the CurrentDifference
			return diff;
		}

		#endregion
	}
}
