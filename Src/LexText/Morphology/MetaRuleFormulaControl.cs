using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class represents a rule formula control for a metathesis rule. It consists of
	/// four editable cells: left context, right context, left switch, and right switch. The
	/// data for each cell is contained within the <c>StrucDesc</c> and <c>StrucChange</c> fields
	/// in <c>PhMetathesisRule</c> class. <c>StrucDesc</c> is an owning sequence of phonological
	/// contexts. The <c>StrucChange</c> field is a string field that contains five indices. Each
	/// index is used to represent what contexts in <c>StrucDesc</c> are associated with each of
	/// the four cells.
	/// </summary>
	public class MetaRuleFormulaControl : RuleFormulaControl
	{
		public MetaRuleFormulaControl(XmlNode configurationNode)
			: base(configurationNode)
		{
			m_menuId = "mnuPhMetathesisRule";
		}

		public override bool SliceIsCurrent
		{
			set
			{
				CheckDisposed();
				if (value)
					m_view.Select();
			}
		}

		IPhMetathesisRule Rule
		{
			get
			{
				return m_obj as IPhMetathesisRule;
			}
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider,
			XCore.Mediator mediator, string displayNameProperty, string displayWs)
		{
			CheckDisposed();
			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, displayNameProperty, displayWs);

			m_view.Init(mediator, obj, this, new MetaRuleFormulaVc(cache, mediator), MetaRuleFormulaVc.kfragRule);

			m_insertionControl.Initialize(cache, mediator, persistProvider, Rule.Name.BestAnalysisAlternative.Text);
			m_insertionControl.AddOption(RuleInsertType.PHONEME, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.NATURAL_CLASS, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.FEATURES, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.WORD_BOUNDARY, DisplayOption);
			m_insertionControl.AddOption(RuleInsertType.MORPHEME_BOUNDARY, DisplayOption);
			m_insertionControl.NoOptionsMessage = DisplayNoOptsMsg;
		}

		bool DisplayOption(RuleInsertType type)
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
				return false;

			switch (cellId)
			{
				case PhMetathesisRule.kidxLeftSwitch:
					switch (type)
					{
						case RuleInsertType.WORD_BOUNDARY:
							return false;

						case RuleInsertType.MORPHEME_BOUNDARY:
							if (Rule.LeftSwitchIndex == -1 || Rule.MiddleIndex != -1 || sel.IsRange)
								return false;
							else
								return GetInsertionIndex(Rule.StrucDescOS.HvoArray, sel) == Rule.LeftSwitchIndex + 1;

						default:
							if (Rule.LeftSwitchIndex == -1)
							{
								return true;
							}
							else if (sel.IsRange)
							{
								int beginHvo = GetItemHvo(sel, SelectionHelper.SelLimitType.Top);
								int endHvo = GetItemHvo(sel, SelectionHelper.SelLimitType.Bottom);

								int lastHvo = 0;
								if (Rule.MiddleIndex != -1 && Rule.IsMiddleWithLeftSwitch)
									lastHvo = Rule.StrucDescOS[Rule.MiddleLimit - 1].Hvo;
								else
									lastHvo = Rule.StrucDescOS[Rule.LeftSwitchLimit - 1].Hvo;

								return beginHvo == Rule.StrucDescOS[Rule.LeftSwitchIndex].Hvo && endHvo == lastHvo;
							}
							return false;
					}

				case PhMetathesisRule.kidxRightSwitch:
					switch (type)
					{
						case RuleInsertType.WORD_BOUNDARY:
							return false;

						case RuleInsertType.MORPHEME_BOUNDARY:
							if (Rule.RightSwitchIndex == -1 || Rule.MiddleIndex != -1 || sel.IsRange)
								return false;
							else
								return GetInsertionIndex(Rule.StrucDescOS.HvoArray, sel) == Rule.RightSwitchIndex;

						default:
							if (Rule.RightSwitchIndex == -1)
							{
								return true;
							}
							else if (sel.IsRange)
							{
								int beginHvo = GetItemHvo(sel, SelectionHelper.SelLimitType.Top);
								int endHvo = GetItemHvo(sel, SelectionHelper.SelLimitType.Bottom);

								int firstHvo = 0;
								if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
									firstHvo = Rule.StrucDescOS[Rule.MiddleIndex].Hvo;
								else
									firstHvo = Rule.StrucDescOS[Rule.RightSwitchIndex].Hvo;

								return beginHvo == firstHvo && endHvo == Rule.StrucDescOS[Rule.RightSwitchLimit - 1].Hvo;
							}
							return false;
					}

				case PhMetathesisRule.kidxLeftEnv:
					int[] leftHvos = Rule.StrucDescOS.HvoArray;
					IPhSimpleContext first = null;
					if (Rule.StrucDescOS.Count > 0)
						first = Rule.StrucDescOS[0];

					if (type == RuleInsertType.WORD_BOUNDARY)
					{
						// only display the word boundary option if we are at the beginning of the left context and
						// there is no word boundary already inserted
						if (sel.IsRange)
							return GetIndicesToRemove(leftHvos, sel)[0] == 0;
						else
							return GetInsertionIndex(leftHvos, sel) == 0 && !IsWordBoundary(first);
					}
					else
					{
						// we cannot insert anything to the left of a word boundary in the left context
						return sel.IsRange || GetInsertionIndex(leftHvos, sel) != 0 || !IsWordBoundary(first);
					}

				case PhMetathesisRule.kidxRightEnv:
					int[] rightHvos = Rule.StrucDescOS.HvoArray;
					IPhSimpleContext last = null;
					if (Rule.StrucDescOS.Count > 0)
						last = Rule.StrucDescOS[Rule.StrucDescOS.Count - 1];
					if (type == RuleInsertType.WORD_BOUNDARY)
					{
						// only display the word boundary option if we are at the end of the right context and
						// there is no word boundary already inserted
						if (sel.IsRange)
						{
							int[] indices = GetIndicesToRemove(rightHvos, sel);
							return indices[indices.Length - 1] == rightHvos.Length - 1;
						}
						else
						{
							return GetInsertionIndex(rightHvos, sel) == rightHvos.Length && !IsWordBoundary(last);
						}
					}
					else
					{
						// we cannot insert anything to the right of a word boundary in the right context
						return sel.IsRange || GetInsertionIndex(rightHvos, sel) != rightHvos.Length || !IsWordBoundary(last);
					}
			}

			return false;
		}

		string DisplayNoOptsMsg()
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			switch (cellId)
			{
				case PhMetathesisRule.kidxLeftSwitch:
				case PhMetathesisRule.kidxRightSwitch:
					return MEStrings.ksMetaRuleNoOptsMsg;

				case PhMetathesisRule.kidxLeftEnv:
				case PhMetathesisRule.kidxRightEnv:
					return MEStrings.ksRuleWordBdryNoOptsMsg;
			}
			return null;
		}

		protected override int GetNextCell(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRule.kidxLeftEnv:
					return PhMetathesisRule.kidxLeftSwitch;
				case PhMetathesisRule.kidxLeftSwitch:
					return PhMetathesisRule.kidxRightSwitch;
				case PhMetathesisRule.kidxRightSwitch:
					return PhMetathesisRule.kidxRightEnv;
				case PhMetathesisRule.kidxRightEnv:
					return -1;
			}
			return -1;
		}

		protected override int GetPrevCell(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRule.kidxLeftEnv:
					return -1;
				case PhMetathesisRule.kidxLeftSwitch:
					return PhMetathesisRule.kidxLeftEnv;
				case PhMetathesisRule.kidxRightSwitch:
					return PhMetathesisRule.kidxLeftSwitch;
				case PhMetathesisRule.kidxRightEnv:
					return PhMetathesisRule.kidxRightSwitch;
			}
			return -1;
		}

		protected override int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			int tag = sel.GetTextPropId(limit);
			switch (tag)
			{
				case MetaRuleFormulaVc.ktagLeftEnv:
					return PhMetathesisRule.kidxLeftEnv;
				case MetaRuleFormulaVc.ktagLeftSwitch:
					return PhMetathesisRule.kidxLeftSwitch;
				case MetaRuleFormulaVc.ktagRightSwitch:
					return PhMetathesisRule.kidxRightSwitch;
				case MetaRuleFormulaVc.ktagRightEnv:
					return PhMetathesisRule.kidxRightEnv;
			}

			int hvo = GetItemHvo(sel, limit);
			if (hvo == 0)
				return -1;

			return Rule.GetStrucChangeIndex(hvo);
		}

		protected override int GetItemHvo(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (Rule.StrucDescOS.Count == 0 || sel.GetNumberOfLevels(limit) == 0)
				return 0;

			SelLevInfo[] levels = sel.GetLevelInfo(limit);
			return levels[levels.Length - 1].hvo;
		}

		protected override int GetItemCellIndex(int cellId, int hvo)
		{
			if (hvo == 0)
				return -1;

			int index = m_cache.GetObjIndex(Rule.Hvo, (int)PhMetathesisRule.PhSegmentRuleTags.kflidStrucDesc, hvo);
			return ConvertToCellIndex(cellId, index);
		}

		int ConvertToCellIndex(int cellId, int index)
		{
			switch (cellId)
			{
				case PhMetathesisRule.kidxLeftEnv:
					if (Rule.LeftEnvIndex == -1)
						index = -1;
					break;
				case PhMetathesisRule.kidxLeftSwitch:
					if (Rule.LeftSwitchIndex == -1)
						index = -1;
					else
						index -= Rule.LeftSwitchIndex;
					break;
				case PhMetathesisRule.kidxRightSwitch:
					if (Rule.RightSwitchIndex == -1)
						index = -1;
					else if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
						index -= Rule.MiddleIndex;
					else
						index -= Rule.RightSwitchIndex;
					break;
				case PhMetathesisRule.kidxRightEnv:
					if (Rule.RightEnvIndex == -1)
						index = -1;
					else
						index -= Rule.RightEnvIndex;
					break;
			}
			return index;
		}

		protected override SelLevInfo[] GetLevelInfo(int cellId, int cellIndex)
		{
			SelLevInfo[] levels = null;
			if (cellIndex > -1)
			{
				switch (cellId)
				{
					case PhMetathesisRule.kidxLeftSwitch:
						cellIndex += Rule.LeftSwitchIndex;
						break;
					case PhMetathesisRule.kidxRightSwitch:
						if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
							cellIndex += Rule.MiddleIndex;
						else
							cellIndex += Rule.RightSwitchIndex;
						break;
					case PhMetathesisRule.kidxRightEnv:
						cellIndex += Rule.RightEnvIndex;
						break;
				}

				levels = new SelLevInfo[1];
				levels[0].cpropPrevious = cellIndex;
				levels[0].tag = -1;
			}
			return levels;
		}

		protected override int GetCellCount(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRule.kidxLeftEnv:
					if (Rule.LeftEnvIndex == -1)
						return 0;
					else
						return Rule.LeftEnvLimit;

				case PhMetathesisRule.kidxLeftSwitch:
					if (Rule.LeftSwitchIndex == -1)
					{
						return 0;
					}
					else
					{
						int middleCount = 0;
						if (Rule.MiddleIndex != -1 && Rule.IsMiddleWithLeftSwitch)
						{
							middleCount = Rule.MiddleLimit - Rule.MiddleIndex;
						}
						return (Rule.LeftSwitchLimit - Rule.LeftSwitchIndex) + middleCount;
					}

				case PhMetathesisRule.kidxRightSwitch:
					if (Rule.RightSwitchIndex == -1)
					{
						return 0;
					}
					else
					{
						int middleCount = 0;
						if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
						{
							middleCount = Rule.MiddleLimit - Rule.MiddleIndex;
						}
						return (Rule.RightSwitchLimit - Rule.RightSwitchIndex) + middleCount;
					}

				case PhMetathesisRule.kidxRightEnv:
					if (Rule.RightEnvIndex == -1)
						return 0;
					else
						return Rule.RightEnvLimit - Rule.RightEnvIndex;
			}
			return 0;
		}

		protected override int GetFlid(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRule.kidxLeftEnv:
					return MetaRuleFormulaVc.ktagLeftEnv;
				case PhMetathesisRule.kidxLeftSwitch:
					return MetaRuleFormulaVc.ktagLeftSwitch;
				case PhMetathesisRule.kidxRightSwitch:
					return MetaRuleFormulaVc.ktagRightSwitch;
				case PhMetathesisRule.kidxRightEnv:
					return MetaRuleFormulaVc.ktagRightEnv;
			}
			return -1;
		}

		protected override int InsertPhoneme(int hvo, SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(new PhSimpleContextSeg(), (int)PhSimpleContextSeg.PhSimpleContextSegTags.kflidFeatureStructure,
				hvo, sel, out cellIndex);
		}

		protected override int InsertBdry(int hvo, SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(new PhSimpleContextBdry(), (int)PhSimpleContextBdry.PhSimpleContextBdryTags.kflidFeatureStructure,
				hvo, sel, out cellIndex);
		}

		protected override int InsertNC(int hvo, SelectionHelper sel, out int cellIndex)
		{
			return InsertContext(new PhSimpleContextNC(), (int)PhSimpleContextNC.PhSimpleContextNCTags.kflidFeatureStructure,
				hvo, sel, out cellIndex);
		}

		int InsertContext(IPhSimpleContext ctxt, int fsFlid, int fsHvo, SelectionHelper sel, out int cellIndex)
		{
			int cellId = GetCell(sel);
			int index = InsertContextInto(ctxt, fsFlid, fsHvo, sel, Rule.StrucDescOS);
			Rule.UpdateStrucChange(cellId, index, true);
			cellIndex = ConvertToCellIndex(cellId, index);
			return cellId;
		}

		protected override int GetInsertionIndex(int[] hvos, SelectionHelper sel)
		{
			if (sel.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) == 0)
			{
				int cellId = GetCell(sel, SelectionHelper.SelLimitType.Top);
				switch (cellId)
				{
					case PhMetathesisRule.kidxLeftEnv:
						return 0;

					case PhMetathesisRule.kidxLeftSwitch:
						if (Rule.MiddleIndex != -1)
							return Rule.MiddleIndex;
						else if (Rule.RightSwitchIndex != -1)
							return Rule.RightSwitchIndex;
						else if (Rule.RightEnvIndex != -1)
							return Rule.RightEnvIndex;
						break;

					case PhMetathesisRule.kidxRightSwitch:
						if (Rule.RightEnvIndex != -1)
							return Rule.RightEnvIndex;
						break;
				}
				return hvos.Length;
			}
			else
			{
				return base.GetInsertionIndex(hvos, sel);
			}
		}

		protected override int RemoveItems(SelectionHelper sel, bool forward, out int cellIndex)
		{
			cellIndex = -1;
			int cellId = GetCell(sel);
			int index;
			// PhMetathesisRule.UpdateStrucChange does not need to be called here, because it is called in
			// PhSimpleContext.DeleteObjectSideEffects
			bool reconstruct = RemoveContextsFrom(forward, sel, Rule.StrucDescOS, true, out index);
			if (reconstruct)
				cellIndex = ConvertToCellIndex(cellId, index);
			return reconstruct ? cellId : -1;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (m_view != null)
			{
				int w = Width;
				m_view.Width = w > 0 ? w : 0;
			}
		}
	}

	class MetaRuleFormulaVc : RuleFormulaVc
	{
		public const int kfragRule = 100;

		public const int ktagLeftEnv = -200;
		public const int ktagRightEnv = -201;
		public const int ktagLeftSwitch = -202;
		public const int ktagRightSwitch = -203;

		ITsTextProps m_inputCtxtProps;
		ITsTextProps m_resultCtxtProps;
		ITsTextProps m_colHeaderProps;
		ITsTextProps m_rowHeaderProps;

		ITsString m_inputStr;
		ITsString m_resultStr;
		ITsString m_leftEnvStr;
		ITsString m_rightEnvStr;
		ITsString m_switchStr;

		IPhMetathesisRule m_rule = null;

		public MetaRuleFormulaVc(FdoCache cache, XCore.Mediator mediator)
			: base(cache, mediator)
		{
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			m_inputCtxtProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			m_resultCtxtProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_colHeaderProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_rowHeaderProps = tpb.GetTextProps();

			m_inputStr = m_cache.MakeUserTss(MEStrings.ksMetaRuleInput);
			m_resultStr = m_cache.MakeUserTss(MEStrings.ksMetaRuleResult);
			m_leftEnvStr = m_cache.MakeUserTss(MEStrings.ksMetaRuleLeftEnv);
			m_rightEnvStr = m_cache.MakeUserTss(MEStrings.ksMetaRuleRightEnv);
			m_switchStr = m_cache.MakeUserTss(MEStrings.ksMetaRuleSwitch);
		}

		#region IDisposable override
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_rule = null;

			if (m_inputCtxtProps != null)
			{
				Marshal.ReleaseComObject(m_inputCtxtProps);
				m_inputCtxtProps = null;
			}
			if (m_resultCtxtProps != null)
			{
				Marshal.ReleaseComObject(m_resultCtxtProps);
				m_resultCtxtProps = null;
			}
			if (m_colHeaderProps != null)
			{
				Marshal.ReleaseComObject(m_colHeaderProps);
				m_colHeaderProps = null;
			}
			if (m_rowHeaderProps != null)
			{
				Marshal.ReleaseComObject(m_rowHeaderProps);
				m_rowHeaderProps = null;
			}
			if (m_inputStr != null)
			{
				Marshal.ReleaseComObject(m_inputStr);
				m_inputStr = null;
			}
			if (m_resultStr != null)
			{
				Marshal.ReleaseComObject(m_resultStr);
				m_resultStr = null;
			}
			if (m_leftEnvStr != null)
			{
				Marshal.ReleaseComObject(m_leftEnvStr);
				m_leftEnvStr = null;
			}
			if (m_rightEnvStr != null)
			{
				Marshal.ReleaseComObject(m_rightEnvStr);
				m_rightEnvStr = null;
			}
			if (m_switchStr != null)
			{
				Marshal.ReleaseComObject(m_switchStr);
				m_switchStr = null;
			}

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		protected override int MaxNumLines
		{
			get
			{
				return GetNumLines(m_rule.StrucDescOS);
			}
		}

		protected override int GetVarIndex(IPhFeatureConstraint var)
		{
			return -1;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch (frag)
			{
				case kfragRule:
					m_rule = new PhMetathesisRule(m_cache, hvo);

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
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagLeftEnv, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					else
					{
						for (int i = 0; i < m_rule.LeftEnvLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
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
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagLeftSwitch, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					else
					{
						for (int i = m_rule.LeftSwitchIndex; i < m_rule.LeftSwitchLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);

						if (m_rule.MiddleIndex != -1 && m_rule.IsMiddleWithLeftSwitch)
						{
							for (int i = m_rule.MiddleIndex; i < m_rule.MiddleLimit; i++)
								vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
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
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagRightSwitch, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					else
					{
						if (m_rule.MiddleIndex != -1 && !m_rule.IsMiddleWithLeftSwitch)
						{
							for (int i = m_rule.MiddleIndex; i < m_rule.MiddleLimit; i++)
								vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
						}

						for (int i = m_rule.RightSwitchIndex; i < m_rule.RightSwitchLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
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
						OpenContextPile(vwenv, false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagRightEnv, this, kfragEmpty);
						CloseContextPile(vwenv, false);
					}
					else
					{
						for (int i = m_rule.RightEnvIndex; i < m_rule.RightEnvLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
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
						for (int i = 0; i < m_rule.LeftEnvLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
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
						for (int i = m_rule.RightSwitchIndex; i < m_rule.RightSwitchLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
					}
					if (m_rule.MiddleIndex != -1 && m_rule.IsMiddleWithLeftSwitch)
					{
						for (int i = m_rule.MiddleIndex; i < m_rule.MiddleLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
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
						for (int i = m_rule.MiddleIndex; i < m_rule.MiddleLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
					}
					if (m_rule.LeftSwitchIndex != -1)
					{
						for (int i = m_rule.LeftSwitchIndex; i < m_rule.LeftSwitchLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
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
						for (int i = m_rule.RightEnvIndex; i < m_rule.RightEnvLimit; i++)
							vwenv.AddObj(m_rule.StrucDescOS[i].Hvo, this, kfragContext);
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
