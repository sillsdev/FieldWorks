// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class RibbonVc : InterlinVc
	{
		readonly InterlinRibbon m_ribbon;

		public RibbonVc(InterlinRibbon ribbon)
			: base(ribbon.Cache)
		{
			m_ribbon = ribbon;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case InterlinRibbon.kfragRibbonWordforms:
					if (hvo == 0)
					{
						return;
					}
					if (m_ribbon.IsRightToLeft)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
					}
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, 10000);
					vwenv.OpenDiv();
					vwenv.OpenParagraph();
					AddLabelPile(vwenv, m_cache, true, ShowMorphBundles);
					vwenv.AddObjVecItems(m_ribbon.OccurenceListId, this, InterlinVc.kfragBundle);
					vwenv.CloseParagraph();
					vwenv.CloseDiv();
					break;
				case kfragBundle:
					// Review: will this lead to multiple spurious blue lines?
					var realHvo = (m_ribbon.Decorator as InterlinRibbonDecorator).OccurrenceFromHvo(hvo).Analysis.Hvo;
					if (m_ribbon.SelLimOccurrence != null && m_ribbon.SelLimOccurrence.Analysis.Hvo == realHvo)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 5000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Blue));
					}
					base.Display(vwenv, hvo, frag);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		/// <summary>
		/// In this case, the 'hvo' is a dummy for the cached AnalysisOccurrence.
		/// </summary>
		protected override void AddWordBundleInternal(int hvo, IVwEnv vwenv)
		{
			SetupAndOpenInnerPile(vwenv);
			var frag = (m_ribbon.Decorator as InterlinRibbonDecorator).OccurrenceFromHvo(hvo) as LocatedAnalysisOccurrence;
			DisplayAnalysisAndCloseInnerPile(vwenv, frag, false);
		}

		protected override void GetSegmentLevelTags(LcmCache cache)
		{
			// do nothing (we don't need tags above bundle level).
		}
	}
}