// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class InterlinDocForAnalysisVc : InterlinVc
	{
		public InterlinDocForAnalysisVc(LcmCache cache)
			: base(cache)
		{
			FocusBoxSize = new Size(100000, 50000); // If FocusBoxAnnotation is set, this gives the size of box to make. (millipoints)
		}

		AnalysisOccurrence m_focusBoxOccurrence;
		/// <summary>
		/// Set the annotation that is displayed as a fix-size box on top of which the SandBox is overlayed.
		/// Client must also do PropChanged to produce visual effect.
		/// Size is in millipoints!
		/// </summary>
		/// <remarks>This can become invalid if the user deletes some text.  See FWR-3003.</remarks>
		internal AnalysisOccurrence FocusBoxOccurrence
		{
			get
			{
				if (m_focusBoxOccurrence != null && m_focusBoxOccurrence.IsValid)
				{
					return m_focusBoxOccurrence;
				}
				m_focusBoxOccurrence = null;
				return null;
			}
			set { m_focusBoxOccurrence = value; }
		}

		/// <summary>
		/// Set the size of the space reserved for the Sandbox. Client must also do a Propchanged to trigger
		/// visual effect.
		/// </summary>
		internal Size FocusBoxSize
		{
			get;
			set;
		}

		protected override void AddWordBundleInternal(int hvo, IVwEnv vwenv)
		{
			// Determine whether it is the focus box occurrence.
			if (FocusBoxOccurrence != null)
			{
				int hvoSeg, tag, ihvo;
				vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoSeg, out tag, out ihvo);
				if (hvoSeg == FocusBoxOccurrence.Segment.Hvo && ihvo == FocusBoxOccurrence.Index)
				{
					// Leave room for the Sandbox instead of displaying the internlinear data.
					// The first argument makes it invisible in case a little bit of it shows around
					// the sandbox.
					// The last argument puts the 'Baseline' of the sandbox (which aligns with the base of the
					// first line of text) an appropriate distance from the top of the Sandbox. This aligns it's
					// top line of text properly.
					// Enhance JohnT: 90% of font height is not always exactly right, but it's the closest
					// I can get without a new API to get the exact ascent of the font.
					var wsSeg = TsStringUtils.GetWsAtOffset(FocusBoxOccurrence.Segment.BaselineText, 0);
					var dympBaseline = FontHeightAdjuster.GetFontHeightForStyle("Normal", StyleSheet, wsSeg, m_cache.LanguageWritingSystemFactoryAccessor) * 9 / 10;
					var transparent = 0xC0000000; // FwTextColor.kclrTransparent won't convert to uint
					vwenv.AddSimpleRect((int)transparent, FocusBoxSize.Width, FocusBoxSize.Height, - (FocusBoxSize.Height - dympBaseline));
					return;
				}
			}
			base.AddWordBundleInternal(hvo, vwenv);
		}

		/// <summary>
		/// The only property we update is a user prompt. We need to switch things back to normal if
		/// anything was typed there, otherwise, the string has the wrong properties, and with all of it
		/// selected, we keep typing over things.
		/// </summary>
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		{
			if (tag != SimpleRootSite.kTagUserPrompt)
			{
				return tssVal;
			}

			// wait until an IME composition is completed before switching the user prompt to a comment
			// field, otherwise setting the comment will terminate the composition (LT-9929)
			if (RootSite.RootBox.IsCompositionInProgress)
			{
				return tssVal;
			}

			if (tssVal.Length == 0)
			{
				// User typed something (return?) which didn't actually put any text over the prompt.
				// No good replacing it because we'll just get the prompt string back and won't be
				// able to make our new selection.
				return tssVal;
			}

			// Get information about current selection
			var helper = SelectionHelper.Create(vwsel, RootSite);
			var seg = (ISegment)m_coRepository.GetObject(hvo);
			var bldr = tssVal.GetBldr();
			// Clear special prompt properties
			bldr.SetIntPropValues(0, bldr.Length, SimpleRootSite.ktptUserPrompt, -1, -1);
			bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptSpellCheck, -1, -1);

			// Add the text the user just typed to the translatin - this destroys the selection
			// because we replace the user prompt. We use the frag to note the WS of interest.
			RootSite.RootBox.DataAccess.SetMultiStringAlt(seg.Hvo, ActiveFreeformFlid, frag, bldr.GetString());

			// arrange to restore the selection (in the new property) at the end of the UOW (when the
			// required property will have been re-established by various PropChanged calls).
			RootSite.RequestSelectionAtEndOfUow(RootSite.RootBox, 0, helper.LevelInfo.Length, helper.LevelInfo, ActiveFreeformFlid,
				m_cpropActiveFreeform, helper.IchAnchor, helper.Ws, helper.AssocPrev,
				helper.GetSelProps(SelectionHelper.SelLimitType.Anchor));
			SetActiveFreeform(0, 0, 0, 0); // AFTER request selection, since it clears ActiveFreeformFlid.
			return tssVal;
		}
	}
}