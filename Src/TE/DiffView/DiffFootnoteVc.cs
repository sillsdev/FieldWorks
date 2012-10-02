// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DiffFootnoteVc.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	#region DiffFootnoteVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class displays the diff footnote view.
	/// </summary>
	///  ----------------------------------------------------------------------------------------
	internal class DiffFootnoteVc: FootnoteVc
	{
		#region Member variables
		private DifferenceList m_Differences;
		private bool m_fRev;
		private bool m_fNeedHighlight;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DiffFootnoteVc class
		/// </summary>
		/// <param name="differences">List of differences</param>
		/// <param name="fRev"><c>true</c> for revision, <c>false</c> for current version</param>
		/// <param name="cache">The database cache</param>
		/// ------------------------------------------------------------------------------------
		public DiffFootnoteVc(DifferenceList differences, bool fRev, FdoCache cache) :
			base(0, LayoutViewTarget.targetDraft, cache.DefaultVernWs)
		{
			base.Cache = cache;
			m_Differences = differences;
			m_fRev = fRev;
			m_fNeedHighlight = true; // assume we start needing the highlight

			// Start the view as editable.  Non-editable mode is now handled in the
			// DiffFootnoteView by eating the keyboard input.  This allows movement of the IP
			// inside of a read-only DiffFootnoteView.
			Editable = true;
		}

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

			m_Differences = null;
			m_DispPropOverrides = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the view needs highlighting
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
		/// This override provides special difference highlighting for a footnote paragraph.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch(frag)
			{
				case (int)StTextFrags.kfrFootnotePara:
				{
					// Even if we don't display BT yet we want to create the translations, so
					// it will be included in the undo action sequence.
					// Otherwise we're having problems with Undo after the Diff dialog
					// is closed (TE-4896).
					GetTranslationForPara(hvo);

					m_DispPropOverrides.Clear();

					// If the given footnote paragraph has differences to be highlighted,
					//  add appropriate properties.

					// Note: When the diff type is ParagraphMissingInCurrent, we don't scan the new
					// para for text diffs - we will just copy all of it including its footnotes.
					// So there may be no sub-diffs created for the footnotes present. Thus we won't
					// be highlighting those footnotes.
					if (m_fNeedHighlight)
						AddOverridesToHighlightFootnoteDiff(hvo);

					// the base Display will do the actual displaying of the FootnotePara frag
					base.Display(vwenv, hvo, frag);
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
		/// Adds the overrides to highlight a footnote diff.
		/// </summary>
		/// <param name="hvo">The hvo of the footnote paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void AddOverridesToHighlightFootnoteDiff(int hvo)
		{
			Difference diff = FindSubDiffForFootnote(hvo);
			if (diff == null)
				return;

			StTxtPara para = new StTxtPara(Cache, hvo);

			// Get the footnote which contains the paragraph.
			int ownerHvo = para.OwnerHVO;
			Debug.Assert(m_cache.GetClassOfObject(ownerHvo) == StFootnote.kClassId);
			if (m_cache.GetClassOfObject(ownerHvo) != StFootnote.kClassId)
				return; //don't override the props for this para; continue on gracefully
			ScrFootnote footnote = new ScrFootnote(Cache, ownerHvo);

			// Only add offset to first paragraph in footnote (should only be one para)
			int offset = 0;
			if (footnote.ParagraphsOS[0].Hvo == hvo)
			{
				int refLength = footnote.GetReference(m_wsDefault).Length;
				int markerLength = footnote.FootnoteMarker.Length;
				//add one for the space in between (added in StVc)
				offset = refLength + markerLength + 1;
			}

			const uint knNinch = 0x80000000;


			// Now add appropriate properties.
			// Need to add override properties for each run in the
			// range to be highlighted.
			int ichOverrideMin = diff.GetIchMin(m_fRev);
			ITsString tss = para.Contents.UnderlyingTsString;
			TsRunInfo runInfo;
			int ichOverrideLim;
			int prevLim = 0;
			do
			{
				tss.FetchRunInfoAt(ichOverrideMin, out runInfo);
				ichOverrideLim = Math.Min(diff.GetIchLim(m_fRev), runInfo.ichLim);
				// Prevent infinite loop in case of bad data in difference
				if (ichOverrideLim == prevLim)
					break;
				prevLim = ichOverrideLim;
				DispPropOverride prop = new DispPropOverride();
				prop.chrp.clrBack = DiffViewVc.kHighlightColor;
				prop.chrp.clrFore = knNinch;
				prop.chrp.clrUnder = knNinch;
				prop.chrp.dympOffset = -1;
				prop.chrp.ssv = -1;
				prop.chrp.unt = -1;
				prop.chrp.ttvBold = -1;
				prop.chrp.ttvItalic = -1;
				prop.chrp.dympHeight = -1;
				prop.chrp.szFaceName = null;
				prop.chrp.szFontVar = null;
				prop.ichMin = ichOverrideMin + offset;
				prop.ichLim = ichOverrideLim + offset;
				m_DispPropOverrides.Add(prop);
				ichOverrideMin = ichOverrideLim;
			}
			while (ichOverrideLim < diff.GetIchLim(m_fRev));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the diff for footnote.
		/// </summary>
		/// <param name="hvoFootnotePara">The hvo of the footnote para.</param>
		/// <returns>The sub-difference</returns>
		/// ------------------------------------------------------------------------------------
		private Difference FindSubDiffForFootnote(int hvoFootnotePara)
		{
			if (m_Differences.CurrentDifference == null ||
				m_Differences.CurrentDifference.SubDiffsForORCs == null)
			{
				return null;
			}
			foreach (Difference diff in m_Differences.CurrentDifference.SubDiffsForORCs)
			{
				// There could be multiple footnote subdiffs, so we need to look for the one that
				// corresponds to the footnote para we're interested in.
				// Also note: Subdiffs are also used for multiple added paragraphs and sections,
				// and no longer just for footnotes. In these cases, the para hvos in a subdiff
				// may well not be a footnote para at all, or they may be be zero. If we
				// encounter these, keep looking.

				// if this subdiff does not match the given FootnotePara, keep looking
				if (diff.GetHvo(m_fRev) == hvoFootnotePara)
					return diff;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the appropriate "OpenPara" method.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvoPara">the StTxtPara for which we want a paragraph</param>
		/// ------------------------------------------------------------------------------------
		protected override void OpenPara(IVwEnv vwenv, int hvoPara)
		{
			vwenv.OpenOverridePara(m_DispPropOverrides.Count, m_DispPropOverrides.ToArray());
		}
		#endregion
	}
	#endregion
}
