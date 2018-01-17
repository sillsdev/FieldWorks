// Copyright (c) ????-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas
{
	internal class MetaRuleFormulaVc : RuleFormulaVcBase
	{
		public const int kfragRule = 200;

		public const int ktagLeftEnv = -300;
		public const int ktagRightEnv = -301;
		public const int ktagLeftSwitch = -302;
		public const int ktagRightSwitch = -303;

		private readonly ITsTextProps m_inputCtxtProps;
		private readonly ITsTextProps m_resultCtxtProps;
		private readonly ITsTextProps m_colHeaderProps;
		private readonly ITsTextProps m_rowHeaderProps;

		private readonly ITsString m_inputStr;
		private readonly ITsString m_resultStr;
		private readonly ITsString m_leftEnvStr;
		private readonly ITsString m_rightEnvStr;
		private readonly ITsString m_switchStr;

		private IPhMetathesisRule m_rule;

		public MetaRuleFormulaVc(LcmCache cache, IPropertyTable propertyTable)
			: base(cache, propertyTable)
		{
			var tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			m_inputCtxtProps = tpb.GetTextProps();

			tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
			m_resultCtxtProps = tpb.GetTextProps();

			tpb = TsStringUtils.MakePropsBldr();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, MiscUtils.StandardSansSerif);
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_colHeaderProps = tpb.GetTextProps();

			tpb = TsStringUtils.MakePropsBldr();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, MiscUtils.StandardSansSerif);
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_rowHeaderProps = tpb.GetTextProps();

			var userWs = m_cache.DefaultUserWs;
			m_inputStr = TsStringUtils.MakeString(AreaResources.ksMetaRuleInput, userWs);
			m_resultStr = TsStringUtils.MakeString(AreaResources.ksMetaRuleResult, userWs);
			m_leftEnvStr = TsStringUtils.MakeString(AreaResources.ksMetaRuleLeftEnv, userWs);
			m_rightEnvStr = TsStringUtils.MakeString(AreaResources.ksMetaRuleRightEnv, userWs);
			m_switchStr = TsStringUtils.MakeString(AreaResources.ksMetaRuleSwitch, userWs);
		}

		protected override int GetMaxNumLines()
		{
			return GetNumLines(m_rule.StrucDescOS);
		}

		protected override int GetVarIndex(IPhFeatureConstraint var)
		{
			return -1;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragRule:
					m_rule = m_cache.ServiceLocator.GetInstance<IPhMetathesisRuleRepository>().GetObject(hvo);
					if (m_rule.Disabled)
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, "Disabled Text");
					}

					var maxNumLines = GetMaxNumLines();

					VwLength tableLen;
					tableLen.nVal = 10000;
					tableLen.unit = VwUnit.kunPercent100;
					vwenv.OpenTable(5, tableLen, 0, VwAlignment.kvaCenter, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 4000, false);

					VwLength ctxtLen;
					ctxtLen.nVal = 1;
					ctxtLen.unit = VwUnit.kunRelative;

					int resultx, inputx, dmpy;
					vwenv.get_StringWidth(m_resultStr, m_colHeaderProps, out resultx, out dmpy);
					vwenv.get_StringWidth(m_inputStr, m_colHeaderProps, out inputx, out dmpy);
					VwLength headerLen;
					headerLen.nVal = Math.Max(resultx, inputx) + 8000;
					headerLen.unit = VwUnit.kunPoint1000;

					vwenv.MakeColumns(1, headerLen);
					vwenv.MakeColumns(4, ctxtLen);

					vwenv.OpenTableBody();

					vwenv.OpenTableRow();

					vwenv.OpenTableCell(1, 1);
					vwenv.CloseTableCell();

					// left context header cell
					vwenv.Props = m_colHeaderProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_leftEnvStr);
					vwenv.CloseTableCell();

					// switch header cell
					vwenv.Props = m_colHeaderProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.OpenTableCell(1, 2);
					vwenv.AddString(m_switchStr);
					vwenv.CloseTableCell();

					// right context header cell
					vwenv.Props = m_colHeaderProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_rightEnvStr);
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();

					vwenv.OpenTableRow();

					// input header cell
					vwenv.Props = m_rowHeaderProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_inputStr);
					vwenv.CloseTableCell();

					// input left context cell
					vwenv.Props = m_inputCtxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.LeftEnvIndex == -1)
					{
						OpenSingleLinePile(vwenv, maxNumLines, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagLeftEnv, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
					}
					else
					{
						for (var i = 0; i < m_rule.LeftEnvLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// input left switch cell
					vwenv.Props = m_inputCtxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.LeftSwitchIndex == -1)
					{
						OpenSingleLinePile(vwenv, maxNumLines, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagLeftSwitch, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
					}
					else
					{
						for (var i = m_rule.LeftSwitchIndex; i < m_rule.LeftSwitchLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}

						if (m_rule.MiddleIndex != -1 && m_rule.IsMiddleWithLeftSwitch)
						{
							for (var i = m_rule.MiddleIndex; i < m_rule.MiddleLimit; i++)
							{
								vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
							}
						}
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// input right switch cell
					vwenv.Props = m_inputCtxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.RightSwitchIndex == -1)
					{
						OpenSingleLinePile(vwenv, maxNumLines, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagRightSwitch, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
					}
					else
					{
						if (m_rule.MiddleIndex != -1 && !m_rule.IsMiddleWithLeftSwitch)
						{
							for (var i = m_rule.MiddleIndex; i < m_rule.MiddleLimit; i++)
							{
								vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
							}
						}

						for (var i = m_rule.RightSwitchIndex; i < m_rule.RightSwitchLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// input right context cell
					vwenv.Props = m_inputCtxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.RightEnvIndex == -1)
					{
						OpenSingleLinePile(vwenv, maxNumLines, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagRightEnv, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
					}
					else
					{
						for (var i = m_rule.RightEnvIndex; i < m_rule.RightEnvLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();

					vwenv.OpenTableRow();

					// input result header cell
					vwenv.Props = m_rowHeaderProps;
					vwenv.OpenTableCell(1, 1);
					vwenv.AddString(m_resultStr);
					vwenv.CloseTableCell();

					// result left context cell
					vwenv.Props = m_resultCtxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.LeftEnvIndex != -1)
					{
						for (var i = 0; i < m_rule.LeftEnvLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// result right switch cell
					vwenv.Props = m_resultCtxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.RightSwitchIndex != -1)
					{
						for (var i = m_rule.RightSwitchIndex; i < m_rule.RightSwitchLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					if (m_rule.MiddleIndex != -1 && m_rule.IsMiddleWithLeftSwitch)
					{
						for (var i = m_rule.MiddleIndex; i < m_rule.MiddleLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// result left switch cell
					vwenv.Props = m_resultCtxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();

					if (m_rule.MiddleIndex != -1 && !m_rule.IsMiddleWithLeftSwitch)
					{
						for (var i = m_rule.MiddleIndex; i < m_rule.MiddleLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					if (m_rule.LeftSwitchIndex != -1)
					{
						for (var i = m_rule.LeftSwitchIndex; i < m_rule.LeftSwitchLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					// result right context cell
					vwenv.Props = m_resultCtxtProps;
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (m_rule.RightEnvIndex != -1)
					{
						for (var i = m_rule.RightEnvIndex; i < m_rule.RightEnvLimit; i++)
						{
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();

					vwenv.CloseTableBody();

					vwenv.CloseTable();
					break;

				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
	}
}