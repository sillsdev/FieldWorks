// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Areas
{
	public abstract class PatternVcBase : FwBaseVc
	{
		// variant frags
		public const int kfragEmpty = 0;
		public const int kfragLeftBracketUpHook = 1;
		public const int kfragLeftBracketExt = 2;
		public const int kfragLeftBracketLowHook = 3;
		public const int kfragRightBracketUpHook = 4;
		public const int kfragRightBracketExt = 5;
		public const int kfragRightBracketLowHook = 6;
		public const int kfragLeftBracket = 7;
		public const int kfragRightBracket = 8;
		public const int kfragLeftParenUpHook = 9;
		public const int kfragLeftParenExt = 10;
		public const int kfragLeftParenLowHook = 11;
		public const int kfragRightParenUpHook = 12;
		public const int kfragRightParenExt = 13;
		public const int kfragRightParenLowHook = 14;
		public const int kfragLeftParen = 15;
		public const int kfragRightParen = 16;
		public const int kfragQuestions = 17;
		public const int kfragZeroWidthSpace = 18;
		// fake flids
		public const int ktagLeftBoundary = -100;
		public const int ktagRightBoundary = -101;
		public const int ktagLeftNonBoundary = -102;
		public const int ktagRightNonBoundary = -103;
		public const int ktagInnerNonBoundary = -104;
		// spacing between contexts
		protected const int PileMargin = 2000;
		protected IPropertyTable m_propertyTable;
		protected ITsTextProps m_bracketProps;
		protected ITsTextProps m_pileProps;
		protected ITsString m_empty;
		protected ITsString m_leftBracketUpHook;
		protected ITsString m_leftBracketExt;
		protected ITsString m_leftBracketLowHook;
		protected ITsString m_rightBracketUpHook;
		protected ITsString m_rightBracketExt;
		protected ITsString m_rightBracketLowHook;
		protected ITsString m_leftBracket;
		protected ITsString m_rightBracket;
		protected ITsString m_leftParenUpHook;
		protected ITsString m_leftParenExt;
		protected ITsString m_leftParenLowHook;
		protected ITsString m_rightParenUpHook;
		protected ITsString m_rightParenExt;
		protected ITsString m_rightParenLowHook;
		protected ITsString m_leftParen;
		protected ITsString m_rightParen;
		protected ITsString m_questions;
		protected ITsString m_zwSpace;

		protected PatternVcBase(LcmCache cache, IPropertyTable propertyTable)
		{
			Cache = cache;
			m_propertyTable = propertyTable;
			var userWs = m_cache.DefaultUserWs;
			var maxFontSize = Cache.ServiceLocator.WritingSystems.AllWritingSystems.Select(ws => GetFontHeight(ws.Handle)).Max();
			var tpb = TsStringUtils.MakePropsBldr();
			// specify the writing system, so that font info for a specific WS in the normal style does not override these props
			tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
			// use Charis SIL because it supports the special characters that are needed for
			// multiline brackets
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Charis SIL");
			// make the size of the brackets large enough so that they display properly
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, maxFontSize);
			m_bracketProps = tpb.GetTextProps();
			tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
			tpb.SetIntPropValues((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
			m_pileProps = tpb.GetTextProps();
			m_empty = TsStringUtils.EmptyString(userWs);
			m_leftBracketUpHook = TsStringUtils.MakeString("\u23a1", userWs);
			m_leftBracketExt = TsStringUtils.MakeString("\u23a2", userWs);
			m_leftBracketLowHook = TsStringUtils.MakeString("\u23a3", userWs);
			m_rightBracketUpHook = TsStringUtils.MakeString("\u23a4", userWs);
			m_rightBracketExt = TsStringUtils.MakeString("\u23a5", userWs);
			m_rightBracketLowHook = TsStringUtils.MakeString("\u23a6", userWs);
			m_leftBracket = TsStringUtils.MakeString("[", userWs);
			m_rightBracket = TsStringUtils.MakeString("]", userWs);
			m_leftParenUpHook = TsStringUtils.MakeString("\u239b", userWs);
			m_leftParenExt = TsStringUtils.MakeString("\u239c", userWs);
			m_leftParenLowHook = TsStringUtils.MakeString("\u239d", userWs);
			m_rightParenUpHook = TsStringUtils.MakeString("\u239e", userWs);
			m_rightParenExt = TsStringUtils.MakeString("\u239f", userWs);
			m_rightParenLowHook = TsStringUtils.MakeString("\u23a0", userWs);
			m_leftParen = TsStringUtils.MakeString("(", userWs);
			m_rightParen = TsStringUtils.MakeString(")", userWs);
			m_questions = TsStringUtils.MakeString("???", userWs);
			m_zwSpace = TsStringUtils.MakeString("\u200b", userWs);
		}

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			// we use display variant to display literal strings that are editable
			ITsString tss = null;
			switch (frag)
			{
				case kfragEmpty:
					tss = m_empty;
					break;
				case kfragLeftBracketUpHook:
					tss = m_leftBracketUpHook;
					break;
				case kfragLeftBracketExt:
					tss = m_leftBracketExt;
					break;
				case kfragLeftBracketLowHook:
					tss = m_leftBracketLowHook;
					break;
				case kfragRightBracketUpHook:
					tss = m_rightBracketUpHook;
					break;
				case kfragRightBracketExt:
					tss = m_rightBracketExt;
					break;
				case kfragRightBracketLowHook:
					tss = m_rightBracketLowHook;
					break;
				case kfragLeftBracket:
					tss = m_leftBracket;
					break;
				case kfragRightBracket:
					tss = m_rightBracket;
					break;
				case kfragLeftParenUpHook:
					tss = m_leftParenUpHook;
					break;
				case kfragLeftParenExt:
					tss = m_leftParenExt;
					break;
				case kfragLeftParenLowHook:
					tss = m_leftParenLowHook;
					break;
				case kfragRightParenUpHook:
					tss = m_rightParenUpHook;
					break;
				case kfragRightParenExt:
					tss = m_rightParenExt;
					break;
				case kfragRightParenLowHook:
					tss = m_rightParenLowHook;
					break;
				case kfragLeftParen:
					tss = m_leftParen;
					break;
				case kfragRightParen:
					tss = m_rightParen;
					break;
				case kfragQuestions:
					tss = m_questions;
					break;
				case kfragZeroWidthSpace:
					tss = m_zwSpace;
					break;
			}
			return tss;
		}

		protected void AddExtraLines(int numLines, IVwEnv vwenv)
		{
			AddExtraLines(numLines, ktagLeftNonBoundary, vwenv);
		}

		protected void AddExtraLines(int numLines, int tag, IVwEnv vwenv)
		{
			for (var i = 0; i < numLines; i++)
			{
				vwenv.Props = m_bracketProps;
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
				vwenv.OpenParagraph();
				vwenv.AddProp(tag, this, kfragEmpty);
				vwenv.CloseParagraph();
			}
		}

		protected void OpenSingleLinePile(IVwEnv vwenv, int maxNumLines)
		{
			OpenSingleLinePile(vwenv, maxNumLines, true);
		}

		protected void OpenSingleLinePile(IVwEnv vwenv, int maxNumLines, bool addBoundary)
		{
			vwenv.Props = m_pileProps;
			vwenv.OpenInnerPile();
			AddExtraLines(maxNumLines - 1, vwenv);
			vwenv.OpenParagraph();
			if (addBoundary)
			{
				vwenv.Props = m_bracketProps;
				vwenv.AddProp(ktagLeftBoundary, this, kfragZeroWidthSpace);
			}
		}

		protected void CloseSingleLinePile(IVwEnv vwenv)
		{
			CloseSingleLinePile(vwenv, true);
		}

		protected void CloseSingleLinePile(IVwEnv vwenv, bool addBoundary)
		{
			if (addBoundary)
			{
				vwenv.Props = m_bracketProps;
				vwenv.AddProp(ktagRightBoundary, this, kfragZeroWidthSpace);
			}
			vwenv.CloseParagraph();
			vwenv.CloseInnerPile();
		}

		/// <summary>
		/// Gets the font height of the specified writing system for the normal style.
		/// </summary>
		protected int GetFontHeight(int ws)
		{
			return FontHeightAdjuster.GetFontHeightForStyle("Normal", FwUtils.StyleSheetFromPropertyTable(m_propertyTable), ws, m_cache.LanguageWritingSystemFactoryAccessor);
		}
	}
}