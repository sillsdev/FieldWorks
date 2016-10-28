using System;
using System.Drawing;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	class AffixRuleFormulaVc : RuleFormulaVcBase
	{
		public const int kfragRule = 200;
		public const int kfragInput = 201;
		public const int kfragRuleMapping = 202;

		public const int kfragSpace = 203;

		public const int ktagLeftEmpty = -300;
		public const int ktagRightEmpty = -301;
		public const int ktagIndex = -302;

		private IMoAffixProcess m_rule;

		private readonly ITsTextProps m_headerProps;
		private readonly ITsTextProps m_arrowProps;
		private readonly ITsTextProps m_ctxtProps;
		private readonly ITsTextProps m_indexProps;
		private readonly ITsTextProps m_resultProps;

		private readonly ITsString m_inputStr;
		private readonly ITsString m_indexStr;
		private readonly ITsString m_resultStr;
		private readonly ITsString m_doubleArrow;
		private readonly ITsString m_space;

		public AffixRuleFormulaVc(FdoCache cache, PropertyTable propertyTable)
			: base(cache, propertyTable)
		{
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, MiscUtils.StandardSansSerif);
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_headerProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 24000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Charis SIL");
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_arrowProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			m_ctxtProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			m_indexProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			m_resultProps = tpb.GetTextProps();

			var tsf = m_cache.TsStrFactory;
			var userWs = m_cache.DefaultUserWs;
			m_inputStr = tsf.MakeString(MEStrings.ksAffixRuleInput, userWs);
			m_indexStr = tsf.MakeString(MEStrings.ksAffixRuleIndex, userWs);
			m_resultStr = tsf.MakeString(MEStrings.ksAffixRuleResult, userWs);
			m_doubleArrow = tsf.MakeString("\u21d2", userWs);
			m_space = tsf.MakeString(" ", userWs);
		}

		protected override int GetMaxNumLines()
		{
			int maxNumLines = 1;
			foreach (IPhContextOrVar ctxtOrVar in m_rule.InputOS)
			{
				int numLines = GetNumLines(ctxtOrVar);
				if (numLines > maxNumLines)
					maxNumLines = numLines;
			}

			return maxNumLines;
		}

		private int GetOutputMaxNumLines()
		{
			int maxNumLines = 1;
			foreach (IMoModifyFromInput modify in m_rule.OutputOS.OfType<IMoModifyFromInput>())
			{
				IPhNCFeatures nc = modify.ModificationRA;
				if (nc != null && nc.FeaturesOA != null)
				{
					int numLines = nc.FeaturesOA.FeatureSpecsOC.Count;
					if (numLines > maxNumLines)
						maxNumLines = numLines;
				}
			}
			return maxNumLines;
		}

		protected override int GetVarIndex(IPhFeatureConstraint var)
		{
			return -1;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			ITsStrFactory tsf = m_cache.TsStrFactory;
			int userWs = m_cache.DefaultUserWs;
			switch (frag)
			{
				case kfragRule:
					m_rule = m_cache.ServiceLocator.GetInstance<IMoAffixProcessRepository>().GetObject(hvo);

					int maxNumLines = GetMaxNumLines();

					VwLength tableLen;
					tableLen.nVal = 10000;
					tableLen.unit = VwUnit.kunPercent100;
					vwenv.OpenTable(3, tableLen, 0, VwAlignment.kvaLeft, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 0, false);

					VwLength inputLen;
					inputLen.nVal = 0;
					inputLen.unit = VwUnit.kunPoint1000;

					int indexWidth = GetStrWidth(m_indexStr, m_headerProps, vwenv);
					int inputWidth = GetStrWidth(m_inputStr, m_headerProps, vwenv);
					VwLength headerLen;
					headerLen.nVal = Math.Max(indexWidth, inputWidth) + 8000;
					headerLen.unit = VwUnit.kunPoint1000;
					inputLen.nVal += headerLen.nVal;

					VwLength leftEmptyLen;
					leftEmptyLen.nVal = 8000 + (PileMargin * 2) + 2000;
					leftEmptyLen.unit = VwUnit.kunPoint1000;
					inputLen.nVal += leftEmptyLen.nVal;

					var ctxtLens = new VwLength[m_rule.InputOS.Count];
					vwenv.NoteDependency(new[] {m_rule.Hvo}, new[] {MoAffixProcessTags.kflidInput}, 1 );
					for (int i = 0; i < m_rule.InputOS.Count; i++)
					{
						int idxWidth = GetStrWidth(tsf.MakeString(Convert.ToString(i + 1), userWs), m_indexProps, vwenv);
						int ctxtWidth = GetWidth(m_rule.InputOS[i], vwenv);
						ctxtLens[i].nVal = Math.Max(idxWidth, ctxtWidth) + 8000 + 1000;
						ctxtLens[i].unit = VwUnit.kunPoint1000;
						inputLen.nVal += ctxtLens[i].nVal;
					}

					VwLength rightEmptyLen;
					rightEmptyLen.nVal = 8000 + (PileMargin * 2) + 1000;
					rightEmptyLen.unit = VwUnit.kunPoint1000;
					inputLen.nVal += rightEmptyLen.nVal;

					vwenv.MakeColumns(1, inputLen);

					VwLength arrowLen;
					arrowLen.nVal = GetStrWidth(m_doubleArrow, m_arrowProps, vwenv) + 8000;
					arrowLen.unit = VwUnit.kunPoint1000;
					vwenv.MakeColumns(1, arrowLen);

					VwLength outputLen;
					outputLen.nVal = 1;
					outputLen.unit = VwUnit.kunRelative;
					vwenv.MakeColumns(1, outputLen);

					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					// input table cell
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenTable(m_rule.InputOS.Count + 3, tableLen, 0, VwAlignment.kvaCenter, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 4000, false);
					vwenv.MakeColumns(1, headerLen);
					vwenv.MakeColumns(1, leftEmptyLen);
					foreach (VwLength ctxtLen in ctxtLens)
						vwenv.MakeColumns(1, ctxtLen);
					vwenv.MakeColumns(1, rightEmptyLen);

					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					// input header cell
					vwenv.Props = m_headerProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_inputStr);
					vwenv.CloseTableCell();

					// input left empty cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					OpenSingleLinePile(vwenv, maxNumLines, false);
					vwenv.Props = m_bracketProps;
					vwenv.AddProp(ktagLeftEmpty, this, kfragEmpty);
					CloseSingleLinePile(vwenv, false);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// input context cells
					vwenv.AddObjVec(MoAffixProcessTags.kflidInput, this, kfragInput);

					// input right empty cell
					vwenv.Props = m_ctxtProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					OpenSingleLinePile(vwenv, maxNumLines, false);
					vwenv.Props = m_bracketProps;
					vwenv.AddProp(ktagRightEmpty, this, kfragEmpty);
					CloseSingleLinePile(vwenv, false);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.OpenTableRow();

					// index header cell
					vwenv.Props = m_headerProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_indexStr);
					vwenv.CloseTableCell();

					// index left empty cell
					vwenv.Props = m_indexProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.CloseTableCell();

					// index cells
					for (int i = 0; i < m_rule.InputOS.Count; i++)
					{
						vwenv.Props = m_indexProps;
						vwenv.OpenTableCell(1, 1);
						vwenv.AddString(tsf.MakeString(Convert.ToString(i + 1), userWs));
						vwenv.CloseTableCell();
					}

					// index right empty cell
					vwenv.Props = m_indexProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					vwenv.CloseTableCell();

					// double arrow cell
					vwenv.Props = m_arrowProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_doubleArrow);
					vwenv.CloseTableCell();

					// result table cell
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenTable(1, tableLen, 0, VwAlignment.kvaLeft, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 4000, false);
					vwenv.MakeColumns(1, outputLen);

					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					// result header cell
					vwenv.Props = m_headerProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_resultStr);
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.OpenTableRow();

					// result cell
					vwenv.Props = m_resultProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.OutputOS.Count == 0)
						vwenv.AddProp(MoAffixProcessTags.kflidOutput, this, kfragEmpty);
					else
						vwenv.AddObjVecItems(MoAffixProcessTags.kflidOutput, this, kfragRuleMapping);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();

					vwenv.CloseTableCell();
					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					break;

				case kfragRuleMapping:
					var mapping = m_cache.ServiceLocator.GetInstance<IMoRuleMappingRepository>().GetObject(hvo);
					switch (mapping.ClassID)
					{
						case MoCopyFromInputTags.kClassId:
							var copy = (IMoCopyFromInput) mapping;
							OpenSingleLinePile(vwenv, GetOutputMaxNumLines());
							if (copy.ContentRA == null)
								vwenv.AddProp(ktagIndex, this, 0);
							else
								vwenv.AddProp(ktagIndex, this, copy.ContentRA.IndexInOwner + 1);
							CloseSingleLinePile(vwenv);
							break;

						case MoInsertPhonesTags.kClassId:
							OpenSingleLinePile(vwenv, GetOutputMaxNumLines());
							vwenv.AddObjVecItems(MoInsertPhonesTags.kflidContent, this, kfragTerminalUnit);
							CloseSingleLinePile(vwenv);
							break;

						case MoModifyFromInputTags.kClassId:
							var modify = (IMoModifyFromInput) mapping;
							int outputMaxNumLines = GetOutputMaxNumLines();
							int numLines = modify.ModificationRA.FeaturesOA.FeatureSpecsOC.Count;

							// index pile
							vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
							vwenv.OpenInnerPile();
							AddExtraLines(outputMaxNumLines - 1, vwenv);
							vwenv.OpenParagraph();
							vwenv.Props = m_bracketProps;
							vwenv.AddProp(ktagLeftBoundary, this, kfragZeroWidthSpace);
							if (modify.ContentRA == null)
								vwenv.AddProp(ktagIndex, this, 0);
							else
								vwenv.AddProp(ktagIndex, this, modify.ContentRA.IndexInOwner + 1);
							vwenv.CloseParagraph();
							vwenv.CloseInnerPile();

							// left bracket pile
							// right align brackets in left bracket pile, since the index could have a greater width, then the bracket
							if (numLines == 1)
							{
								vwenv.OpenInnerPile();
								AddExtraLines(outputMaxNumLines - 1, vwenv);
								vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
								vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracket);
								vwenv.CloseInnerPile();
							}
							else
							{
								vwenv.OpenInnerPile();
								vwenv.Props = m_bracketProps;
								vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
								vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketUpHook);
								for (int i = 1; i < numLines - 1; i++)
								{
									vwenv.Props = m_bracketProps;
									vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
									vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketExt);
								}
								vwenv.Props = m_bracketProps;
								vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
								vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketLowHook);
								vwenv.CloseInnerPile();
							}

							// feature pile
							vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
							vwenv.OpenInnerPile();
							AddExtraLines(outputMaxNumLines - numLines, vwenv);
							if (numLines == 0)
								vwenv.AddProp(MoModifyFromInputTags.kflidModification, this, kfragQuestions);
							else
								vwenv.AddObjProp(MoModifyFromInputTags.kflidModification, this, kfragFeatNC);
							vwenv.CloseInnerPile();

							// right bracket pile
							if (numLines == 1)
							{
								vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
								AddExtraLines(outputMaxNumLines - 1, vwenv);
								vwenv.OpenInnerPile();
								vwenv.AddProp(ktagRightBoundary, this, kfragRightBracket);
								vwenv.CloseInnerPile();
							}
							else
							{
								vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
								vwenv.OpenInnerPile();
								AddExtraLines(outputMaxNumLines - numLines, vwenv);
								vwenv.Props = m_bracketProps;
								vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketUpHook);
								for (int i = 1; i < numLines - 1; i++)
								{
									vwenv.Props = m_bracketProps;
									vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketExt);
								}
								vwenv.Props = m_bracketProps;
								vwenv.AddProp(ktagRightBoundary, this, kfragRightBracketLowHook);
								vwenv.CloseInnerPile();
							}
							break;
					}
					break;

				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			if (tag == ktagIndex)
			{
				// pass the index in the frag argument
				return m_cache.TsStrFactory.MakeString(Convert.ToString(frag), m_cache.DefaultUserWs);
			}
			switch (frag)
			{
				case kfragSpace:
					return m_space;

				default:
					return base.DisplayVariant(vwenv, tag, frag);
			}
		}

		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			switch (frag)
			{
				case kfragInput:
					// input context cell
					foreach (var ctxt in m_rule.InputOS)
					{
						vwenv.Props = m_ctxtProps;
						vwenv.OpenTableCell(1, 1);
						vwenv.OpenParagraph();
						vwenv.AddObj(ctxt.Hvo, this, kfragContext);
						vwenv.CloseParagraph();
						vwenv.CloseTableCell();
					}
					break;

				default:
					base.DisplayVec(vwenv, hvo, tag, frag);
					break;
			}
		}
	}
}