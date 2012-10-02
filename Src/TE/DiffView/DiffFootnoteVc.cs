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
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;

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
			base(LayoutViewTarget.targetDraft, 0)
		{
			Cache = cache;
			DefaultWs = cache.DefaultVernWs;
			m_Differences = differences;
			m_fRev = fRev;
			m_fNeedHighlight = true; // assume we start needing the highlight

			// Start the view as editable.  Non-editable mode is now handled in the
			// DiffFootnoteView by eating the keyboard input.  This allows movement of the IP
			// inside of a read-only DiffFootnoteView.
			Editable = true;
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the view needs highlighting
		/// A diff pane in Edit mode does not use highlighting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NeedHighlight
		{
			get { return m_fNeedHighlight; }
			set { m_fNeedHighlight = value; }
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
			switch(frag)
			{
				case (int)StTextFrags.kfrFootnotePara:
				{
					IScrTxtPara para = Cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().GetObject(hvo);
					// Even if we don't display BT yet we want to create the translations, so
					// it will be included in the undo action sequence.
					// Otherwise we're having problems with Undo after the Diff dialog
					// is closed (TE-4896).
					GetTranslationForPara(para.Hvo);

					m_DispPropOverrides.Clear();

					// If the given footnote paragraph has differences to be highlighted,
					//  add appropriate properties.

					// Note: When the diff type is ParagraphMissingInCurrent, we don't scan the new
					// para for text diffs - we will just copy all of it including its footnotes.
					// So there may be no sub-diffs created for the footnotes present. Thus we won't
					// be highlighting those footnotes.
					if (m_fNeedHighlight)
						AddOverridesToHighlightFootnoteDiff(para);

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
		/// <param name="para">The footnote paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void AddOverridesToHighlightFootnoteDiff(IScrTxtPara para)
		{
			Difference diff = FindSubDiffForFootnote(para);
			if (diff == null)
				return;

			if (!(para.Owner is IScrFootnote))
			{
				Debug.Fail("Non-footnote paragraph being displayed in footnote VC!");
				return; //continue on gracefully
			}

			MakeDispPropOverrides(para, diff.GetIchMin(m_fRev), diff.GetIchLim(m_fRev),
				delegate(ref DispPropOverride prop)
				{
					prop.chrp.clrBack = DiffViewVc.kHighlightColor;
				});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the diff for footnote.
		/// </summary>
		/// <param name="footnotePara">The footnote para.</param>
		/// <returns>The sub-difference</returns>
		/// ------------------------------------------------------------------------------------
		private Difference FindSubDiffForFootnote(IScrTxtPara footnotePara)
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
				if (diff.GetPara(m_fRev) == footnotePara)
					return diff;
			}
			return null;
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
		#endregion
	}
	#endregion
}
