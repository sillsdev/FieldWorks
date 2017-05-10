using System;
using System.Drawing;
using SIL.CoreImpl.Text;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	class RegRuleFormulaVc : RuleFormulaVcBase
	{
		public const int kfragRHS = 200;
		public const int kfragRule = 201;

		private readonly ITsTextProps m_ctxtProps;
		private readonly ITsTextProps m_charProps;

		private readonly ITsString m_arrow;
		private readonly ITsString m_slash;
		private readonly ITsString m_underscore;

		private IPhSegRuleRHS m_rhs;

		public RegRuleFormulaVc(FdoCache cache, PropertyTable propertyTable)
			: base(cache, propertyTable)
		{
			ITsPropsBldr tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			m_ctxtProps = tpb.GetTextProps();

			tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 20000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, 2000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, MiscUtils.StandardSansSerif);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_charProps = tpb.GetTextProps();

			int userWs = m_cache.DefaultUserWs;
			m_arrow = TsStringUtils.MakeString("\u2192", userWs);
			m_slash = TsStringUtils.MakeString("/", userWs);
			m_underscore = TsStringUtils.MakeString("__", userWs);
		}

		protected override int GetMaxNumLines()
		{
			int numLines = GetNumLines(m_rhs.OwningRule.StrucDescOS);
			int maxNumLines = numLines;
			numLines = GetNumLines(m_rhs.StrucChangeOS);
			if (numLines > maxNumLines)
				maxNumLines = numLines;
			numLines = GetNumLines(m_rhs.LeftContextOA);
			if (numLines > maxNumLines)
				maxNumLines = numLines;
			numLines = GetNumLines(m_rhs.RightContextOA);
			if (numLines > maxNumLines)
				maxNumLines = numLines;
			return maxNumLines;
		}

		protected override int GetVarIndex(IPhFeatureConstraint var)
		{
			int i = 0;
			foreach (var curVar in m_rhs.OwningRule.FeatureConstraints)
			{
				if (var == curVar)
					return i;
				i++;
			}
			return -1;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragRHS:
					m_rhs = m_cache.ServiceLocator.GetInstance<IPhSegRuleRHSRepository>().GetObject(hvo);
					var rule = m_rhs.OwningRule;
					if (rule.Disabled)
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, "Disabled Text");
					}

					int arrowWidth, slashWidth, underscoreWidth, charHeight;
					vwenv.get_StringWidth(m_arrow, m_charProps, out arrowWidth, out charHeight);
					int maxCharHeight = charHeight;
					vwenv.get_StringWidth(m_slash, m_charProps, out slashWidth, out charHeight);
					maxCharHeight = Math.Max(charHeight, maxCharHeight);
					vwenv.get_StringWidth(m_underscore, m_charProps, out underscoreWidth, out charHeight);
					maxCharHeight = Math.Max(charHeight, maxCharHeight);

					int dmpx, spaceHeight;
					vwenv.get_StringWidth(m_zwSpace, m_bracketProps, out dmpx, out spaceHeight);

					int maxNumLines = GetMaxNumLines();
					int maxCtxtHeight = maxNumLines * spaceHeight;

					int maxHeight = Math.Max(maxCharHeight, maxCtxtHeight);
					int charOffset = maxHeight;
					int ctxtPadding = maxHeight - maxCtxtHeight;

					VwLength tableLen;
					tableLen.nVal = 10000;
					tableLen.unit = VwUnit.kunPercent100;
					vwenv.OpenTable(7, tableLen, 0, VwAlignment.kvaCenter, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 0, false);

					VwLength ctxtLen;
					ctxtLen.nVal = 1;
					ctxtLen.unit = VwUnit.kunRelative;
					VwLength charLen;
					charLen.unit = VwUnit.kunPoint1000;
					vwenv.MakeColumns(1, ctxtLen);

					charLen.nVal = arrowWidth + 4000;
					vwenv.MakeColumns(1, charLen);

					vwenv.MakeColumns(1, ctxtLen);

					charLen.nVal = slashWidth + 4000;
					vwenv.MakeColumns(1, charLen);

					vwenv.MakeColumns(1, ctxtLen);

					charLen.nVal = underscoreWidth + 4000;
					vwenv.MakeColumns(1, charLen);

					vwenv.MakeColumns(1, ctxtLen);

					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					// LHS cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, ctxtPadding);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					vwenv.AddObjProp(m_cache.MetaDataCacheAccessor.GetFieldId2(PhSegRuleRHSTags.kClassId, "OwningRule", false), this, kfragRule);
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// arrow cell
					vwenv.Props = m_charProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, -charOffset);
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_arrow);
					vwenv.CloseTableCell();

					// RHS cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, ctxtPadding);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rhs.StrucChangeOS.Count > 0)
					{
						vwenv.AddObjVecItems(PhSegRuleRHSTags.kflidStrucChange, this, kfragContext);
					}
					else
					{
						vwenv.NoteDependency(new[] {hvo}, new[] {PhSegRuleRHSTags.kflidStrucChange}, 1);
						OpenSingleLinePile(vwenv, maxNumLines, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(PhSegRuleRHSTags.kflidStrucChange, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// slash cell
					vwenv.Props = m_charProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, -charOffset);
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_slash);
					vwenv.CloseTableCell();

					// left context cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, ctxtPadding);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rhs.LeftContextOA != null)
					{
						vwenv.AddObjProp(PhSegRuleRHSTags.kflidLeftContext, this, kfragContext);
					}
					else
					{
						vwenv.NoteDependency(new[] { hvo }, new[] { PhSegRuleRHSTags.kflidLeftContext }, 1);
						OpenSingleLinePile(vwenv, maxNumLines, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(PhSegRuleRHSTags.kflidLeftContext, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// underscore cell
					vwenv.Props = m_charProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, -charOffset);
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_underscore);
					vwenv.CloseTableCell();

					// right context cell
					vwenv.Props = m_ctxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, ctxtPadding);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rhs.RightContextOA != null)
					{
						vwenv.AddObjProp(PhSegRuleRHSTags.kflidRightContext, this, kfragContext);
					}
					else
					{
						vwenv.NoteDependency(new[] { hvo }, new[] { PhSegRuleRHSTags.kflidRightContext }, 1);
						OpenSingleLinePile(vwenv, maxNumLines, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(PhSegRuleRHSTags.kflidRightContext, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					break;

				case kfragRule:
					if (m_rhs.OwningRule.StrucDescOS.Count > 0)
					{
						vwenv.AddObjVecItems(PhSegmentRuleTags.kflidStrucDesc, this, kfragContext);
					}
					else
					{
						OpenSingleLinePile(vwenv, GetMaxNumLines(), false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(PhSegmentRuleTags.kflidStrucDesc, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
					}
					break;

				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
	}
}