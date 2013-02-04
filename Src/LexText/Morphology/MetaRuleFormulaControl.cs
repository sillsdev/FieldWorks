using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
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

			m_insertionControl.AddOption(new InsertOption(RuleInsertType.Phoneme), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.NaturalClass), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.Features), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.WordBoundary), DisplayOption);
			m_insertionControl.AddOption(new InsertOption(RuleInsertType.MorphemeBoundary), DisplayOption);
			m_insertionControl.NoOptionsMessage = DisplayNoOptsMsg;
		}

		private bool DisplayOption(object option)
		{
			RuleInsertType type = ((InsertOption) option).Type;
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			if (cellId == -1 || cellId == -2)
				return false;

			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftSwitch:
					switch (type)
					{
						case RuleInsertType.WordBoundary:
							return false;

						case RuleInsertType.MorphemeBoundary:
							if (Rule.LeftSwitchIndex == -1 || Rule.MiddleIndex != -1 || sel.IsRange)
								return false;
							return GetInsertionIndex(Rule.StrucDescOS.Cast<ICmObject>().ToArray(), sel) == Rule.LeftSwitchIndex + 1;

						default:
							if (Rule.LeftSwitchIndex == -1)
							{
								return true;
							}
							if (sel.IsRange)
							{
								var beginObj = GetItem(sel, SelectionHelper.SelLimitType.Top);
								var endObj = GetItem(sel, SelectionHelper.SelLimitType.Bottom);

								IPhSimpleContext lastCtxt;
								if (Rule.MiddleIndex != -1 && Rule.IsMiddleWithLeftSwitch)
									lastCtxt = Rule.StrucDescOS[Rule.MiddleLimit - 1];
								else
									lastCtxt = Rule.StrucDescOS[Rule.LeftSwitchLimit - 1];

								return beginObj == Rule.StrucDescOS[Rule.LeftSwitchIndex] && endObj == lastCtxt;
							}
							return false;
					}

				case PhMetathesisRuleTags.kidxRightSwitch:
					switch (type)
					{
						case RuleInsertType.WordBoundary:
							return false;

						case RuleInsertType.MorphemeBoundary:
							if (Rule.RightSwitchIndex == -1 || Rule.MiddleIndex != -1 || sel.IsRange)
								return false;
							return GetInsertionIndex(Rule.StrucDescOS.Cast<ICmObject>().ToArray(), sel) == Rule.RightSwitchIndex;

						default:
							if (Rule.RightSwitchIndex == -1)
							{
								return true;
							}
							if (sel.IsRange)
							{
								var beginObj = GetItem(sel, SelectionHelper.SelLimitType.Top);
								var endObj = GetItem(sel, SelectionHelper.SelLimitType.Bottom);

								IPhSimpleContext firstCtxt;
								if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
									firstCtxt = Rule.StrucDescOS[Rule.MiddleIndex];
								else
									firstCtxt = Rule.StrucDescOS[Rule.RightSwitchIndex];

								return beginObj == firstCtxt && endObj == Rule.StrucDescOS[Rule.RightSwitchLimit - 1];
							}
							return false;
					}

				case PhMetathesisRuleTags.kidxLeftEnv:
					ICmObject[] leftCtxts = Rule.StrucDescOS.Cast<ICmObject>().ToArray();
					IPhSimpleContext first = null;
					if (Rule.StrucDescOS.Count > 0)
						first = Rule.StrucDescOS[0];

					if (type == RuleInsertType.WordBoundary)
					{
						// only display the word boundary option if we are at the beginning of the left context and
						// there is no word boundary already inserted
						if (sel.IsRange)
							return GetIndicesToRemove(leftCtxts, sel)[0] == 0;
						return GetInsertionIndex(leftCtxts, sel) == 0 && !IsWordBoundary(first);
					}
					// we cannot insert anything to the left of a word boundary in the left context
					return sel.IsRange || GetInsertionIndex(leftCtxts, sel) != 0 || !IsWordBoundary(first);

				case PhMetathesisRuleTags.kidxRightEnv:
					ICmObject[] rightCtxts = Rule.StrucDescOS.Cast<ICmObject>().ToArray();
					IPhSimpleContext last = null;
					if (Rule.StrucDescOS.Count > 0)
						last = Rule.StrucDescOS[Rule.StrucDescOS.Count - 1];
					if (type == RuleInsertType.WordBoundary)
					{
						// only display the word boundary option if we are at the end of the right context and
						// there is no word boundary already inserted
						if (sel.IsRange)
						{
							int[] indices = GetIndicesToRemove(rightCtxts, sel);
							return indices[indices.Length - 1] == rightCtxts.Length - 1;
						}
						return GetInsertionIndex(rightCtxts, sel) == rightCtxts.Length && !IsWordBoundary(last);
					}
					// we cannot insert anything to the right of a word boundary in the right context
					return sel.IsRange || GetInsertionIndex(rightCtxts, sel) != rightCtxts.Length || !IsWordBoundary(last);
			}

			return false;
		}

		private string DisplayNoOptsMsg()
		{
			SelectionHelper sel = SelectionHelper.Create(m_view);
			int cellId = GetCell(sel);
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftSwitch:
				case PhMetathesisRuleTags.kidxRightSwitch:
					return MEStrings.ksMetaRuleNoOptsMsg;

				case PhMetathesisRuleTags.kidxLeftEnv:
				case PhMetathesisRuleTags.kidxRightEnv:
					return MEStrings.ksRuleWordBdryNoOptsMsg;
			}
			return null;
		}

		protected override string FeatureChooserHelpTopic
		{
			get { return "khtpChoose-Grammar-PhonFeats-MetaRuleFormulaControl"; }
		}

		protected override string RuleName
		{
			get { return Rule.Name.BestAnalysisAlternative.Text; }
		}

		protected override string ContextMenuID
		{
			get { return "mnuPhMetathesisRule"; }
		}

		protected override int GetNextCell(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					return PhMetathesisRuleTags.kidxLeftSwitch;
				case PhMetathesisRuleTags.kidxLeftSwitch:
					return PhMetathesisRuleTags.kidxRightSwitch;
				case PhMetathesisRuleTags.kidxRightSwitch:
					return PhMetathesisRuleTags.kidxRightEnv;
				case PhMetathesisRuleTags.kidxRightEnv:
					return -1;
			}
			return -1;
		}

		protected override int GetPrevCell(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					return -1;
				case PhMetathesisRuleTags.kidxLeftSwitch:
					return PhMetathesisRuleTags.kidxLeftEnv;
				case PhMetathesisRuleTags.kidxRightSwitch:
					return PhMetathesisRuleTags.kidxLeftSwitch;
				case PhMetathesisRuleTags.kidxRightEnv:
					return PhMetathesisRuleTags.kidxRightSwitch;
			}
			return -1;
		}

		protected override int GetCell(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			int tag = sel.GetTextPropId(limit);
			switch (tag)
			{
				case MetaRuleFormulaVc.ktagLeftEnv:
					return PhMetathesisRuleTags.kidxLeftEnv;
				case MetaRuleFormulaVc.ktagLeftSwitch:
					return PhMetathesisRuleTags.kidxLeftSwitch;
				case MetaRuleFormulaVc.ktagRightSwitch:
					return PhMetathesisRuleTags.kidxRightSwitch;
				case MetaRuleFormulaVc.ktagRightEnv:
					return PhMetathesisRuleTags.kidxRightEnv;
			}

			var obj = GetItem(sel, limit);
			if (obj == null)
				return -1;

			return Rule.GetStrucChangeIndex(obj as IPhSimpleContext);
		}

		protected override ICmObject GetItem(SelectionHelper sel, SelectionHelper.SelLimitType limit)
		{
			if (Rule.StrucDescOS.Count == 0 || sel.GetNumberOfLevels(limit) == 0)
				return null;

			var levels = sel.GetLevelInfo(limit);
			return m_cache.ServiceLocator.GetObject(levels[levels.Length - 1].hvo);
		}

		protected override int GetItemCellIndex(int cellId, ICmObject obj)
		{
			if (obj == null)
				return -1;
			return ConvertToCellIndex(cellId, obj.IndexInOwner);
		}

		int ConvertToCellIndex(int cellId, int index)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					if (Rule.LeftEnvIndex == -1)
						index = -1;
					break;
				case PhMetathesisRuleTags.kidxLeftSwitch:
					if (Rule.LeftSwitchIndex == -1)
						index = -1;
					else
						index -= Rule.LeftSwitchIndex;
					break;
				case PhMetathesisRuleTags.kidxRightSwitch:
					if (Rule.RightSwitchIndex == -1)
						index = -1;
					else if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
						index -= Rule.MiddleIndex;
					else
						index -= Rule.RightSwitchIndex;
					break;
				case PhMetathesisRuleTags.kidxRightEnv:
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
					case PhMetathesisRuleTags.kidxLeftSwitch:
						cellIndex += Rule.LeftSwitchIndex;
						break;
					case PhMetathesisRuleTags.kidxRightSwitch:
						if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
							cellIndex += Rule.MiddleIndex;
						else
							cellIndex += Rule.RightSwitchIndex;
						break;
					case PhMetathesisRuleTags.kidxRightEnv:
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
				case PhMetathesisRuleTags.kidxLeftEnv:
					if (Rule.LeftEnvIndex == -1)
						return 0;
					return Rule.LeftEnvLimit;

				case PhMetathesisRuleTags.kidxLeftSwitch:
					if (Rule.LeftSwitchIndex == -1)
					{
						return 0;
					}
					int leftMidCount = 0;
					if (Rule.MiddleIndex != -1 && Rule.IsMiddleWithLeftSwitch)
					{
						leftMidCount = Rule.MiddleLimit - Rule.MiddleIndex;
					}
					return (Rule.LeftSwitchLimit - Rule.LeftSwitchIndex) + leftMidCount;

				case PhMetathesisRuleTags.kidxRightSwitch:
					if (Rule.RightSwitchIndex == -1)
					{
						return 0;
					}
					int rightMidCount = 0;
					if (Rule.MiddleIndex != -1 && !Rule.IsMiddleWithLeftSwitch)
					{
						rightMidCount = Rule.MiddleLimit - Rule.MiddleIndex;
					}
					return (Rule.RightSwitchLimit - Rule.RightSwitchIndex) + rightMidCount;

				case PhMetathesisRuleTags.kidxRightEnv:
					if (Rule.RightEnvIndex == -1)
						return 0;
					return Rule.RightEnvLimit - Rule.RightEnvIndex;
			}
			return 0;
		}

		protected override int GetFlid(int cellId)
		{
			switch (cellId)
			{
				case PhMetathesisRuleTags.kidxLeftEnv:
					return MetaRuleFormulaVc.ktagLeftEnv;
				case PhMetathesisRuleTags.kidxLeftSwitch:
					return MetaRuleFormulaVc.ktagLeftSwitch;
				case PhMetathesisRuleTags.kidxRightSwitch:
					return MetaRuleFormulaVc.ktagRightSwitch;
				case PhMetathesisRuleTags.kidxRightEnv:
					return MetaRuleFormulaVc.ktagRightEnv;
			}
			return -1;
		}

		protected override int InsertPhoneme(IPhPhoneme phoneme, SelectionHelper sel, out int cellIndex)
		{
			var segCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			var cellId = InsertContext(segCtxt, sel, out cellIndex);
			segCtxt.FeatureStructureRA = phoneme;
			return cellId;
		}

		protected override int InsertBdry(IPhBdryMarker bdry, SelectionHelper sel, out int cellIndex)
		{
			var bdryCtxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
			var cellId = InsertContext(bdryCtxt, sel, out cellIndex);
			bdryCtxt.FeatureStructureRA = bdry;
			return cellId;
		}

		protected override int InsertNC(IPhNaturalClass nc, SelectionHelper sel, out int cellIndex, out IPhSimpleContextNC ctxt)
		{
			ctxt = m_cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			var cellId = InsertContext(ctxt, sel, out cellIndex);
			ctxt.FeatureStructureRA = nc;
			return cellId;
		}

		int InsertContext(IPhSimpleContext ctxt, SelectionHelper sel, out int cellIndex)
		{
			int cellId = GetCell(sel);
			int index = InsertContextInto(ctxt, sel, Rule.StrucDescOS);
			Rule.UpdateStrucChange(cellId, index, true);
			cellIndex = ConvertToCellIndex(cellId, index);
			return cellId;
		}

		protected override int GetInsertionIndex(ICmObject[] objs, SelectionHelper sel)
		{
			if (sel.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) == 0)
			{
				int cellId = GetCell(sel, SelectionHelper.SelLimitType.Top);
				switch (cellId)
				{
					case PhMetathesisRuleTags.kidxLeftEnv:
						return 0;

					case PhMetathesisRuleTags.kidxLeftSwitch:
						if (Rule.MiddleIndex != -1)
							return Rule.MiddleIndex;
						if (Rule.RightSwitchIndex != -1)
							return Rule.RightSwitchIndex;
						if (Rule.RightEnvIndex != -1)
							return Rule.RightEnvIndex;
						break;

					case PhMetathesisRuleTags.kidxRightSwitch:
						if (Rule.RightEnvIndex != -1)
							return Rule.RightEnvIndex;
						break;
				}
				return objs.Length;
			}
			return base.GetInsertionIndex(objs, sel);
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
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, MiscUtils.StandardSansSerif);
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_colHeaderProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, MiscUtils.StandardSansSerif);
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Gray));
			tpb.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_rowHeaderProps = tpb.GetTextProps();

			var tsf = m_cache.TsStrFactory;
			var userWs = m_cache.DefaultUserWs;
			m_inputStr = tsf.MakeString(MEStrings.ksMetaRuleInput, userWs);
			m_resultStr = tsf.MakeString(MEStrings.ksMetaRuleResult, userWs);
			m_leftEnvStr = tsf.MakeString(MEStrings.ksMetaRuleLeftEnv, userWs);
			m_rightEnvStr = tsf.MakeString(MEStrings.ksMetaRuleRightEnv, userWs);
			m_switchStr = tsf.MakeString(MEStrings.ksMetaRuleSwitch, userWs);
		}

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
			switch (frag)
			{
				case kfragRule:
					m_rule = m_cache.ServiceLocator.GetInstance<IPhMetathesisRuleRepository>().GetObject(hvo);
					if (m_rule.Disabled)
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, "Disabled Text");
					}

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
