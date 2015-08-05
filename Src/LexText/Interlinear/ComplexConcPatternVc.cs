using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_mediator is a reference")]
	public class ComplexConcPatternVc : FwBaseVc
	{
		public const int kfragPattern = 0;
		public const int kfragNode = 1;

		// variant frags
		public const int kfragEmpty = 2;
		public const int kfragFeatureLine = 3;
		public const int kfragNodeMax = 4;
		public const int kfragNodeMin = 5;
		public const int kfragLeftBracketUpHook = 6;
		public const int kfragLeftBracketExt = 7;
		public const int kfragLeftBracketLowHook = 8;
		public const int kfragRightBracketUpHook = 9;
		public const int kfragRightBracketExt = 10;
		public const int kfragRightBracketLowHook = 11;
		public const int kfragLeftBracket = 12;
		public const int kfragRightBracket = 13;
		public const int kfragLeftParen = 14;
		public const int kfragRightParen = 15;
		public const int kfragZeroWidthSpace = 16;
		public const int kfragLeftParenUpHook = 17;
		public const int kfragLeftParenExt = 18;
		public const int kfragLeftParenLowHook = 19;
		public const int kfragRightParenUpHook = 20;
		public const int kfragRightParenExt = 21;
		public const int kfragRightParenLowHook = 22;
		public const int kfragOR = 23;
		public const int kfragHash = 24;

		// fake flids
		public const int ktagType = -100;
		public const int ktagForm = -101;
		public const int ktagGloss = -102;
		public const int ktagCategory = -103;
		public const int ktagEntry = -104;
		public const int ktagTag = -105;
		public const int ktagInfl = -106;
		public const int ktagLeftBoundary = -107;
		public const int ktagRightBoundary = -108;
		public const int ktagLeftNonBoundary = -109;
		public const int ktagRightNonBoundary = -110;
		public const int ktagInnerNonBoundary = -111;

		// spacing between contexts
		private const int PileMargin = 1000;

		private readonly IPropertyTable m_propertyTable;

		private readonly ITsTextProps m_bracketProps;
		private readonly ITsTextProps m_pileProps;

		private readonly ITsString m_empty;
		private readonly ITsString m_infinity;
		private readonly ITsString m_leftBracketUpHook;
		private readonly ITsString m_leftBracketExt;
		private readonly ITsString m_leftBracketLowHook;
		private readonly ITsString m_rightBracketUpHook;
		private readonly ITsString m_rightBracketExt;
		private readonly ITsString m_rightBracketLowHook;
		private readonly ITsString m_leftBracket;
		private readonly ITsString m_rightBracket;
		private readonly ITsString m_leftParen;
		private readonly ITsString m_rightParen;
		private readonly ITsString m_zwSpace;
		private readonly ITsString m_leftParenUpHook;
		private readonly ITsString m_leftParenExt;
		private readonly ITsString m_leftParenLowHook;
		private readonly ITsString m_rightParenUpHook;
		private readonly ITsString m_rightParenExt;
		private readonly ITsString m_rightParenLowHook;
		private readonly ITsString m_or;
		private readonly ITsString m_hash;

		private IDictionary<IFsFeatDefn, object> m_curInflFeatures;

		public ComplexConcPatternVc(FdoCache cache, IPropertyTable propertyTable)
		{
			Cache = cache;
			m_propertyTable = propertyTable;

			// use Charis SIL because it supports the special characters that are needed for
			// multiline brackets
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int) FwTextPropType.ktptFontFamily, "Charis SIL");
			m_bracketProps = tpb.GetTextProps();

			tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int) FwTextPropType.ktptMarginLeading, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
			tpb.SetIntPropValues((int) FwTextPropType.ktptMarginTrailing, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
			m_pileProps = tpb.GetTextProps();

			var tsf = m_cache.TsStrFactory;
			var userWs = m_cache.DefaultUserWs;
			m_empty = tsf.MakeString("", userWs);
			m_infinity = tsf.MakeString("\u221e", userWs);
			m_leftBracketUpHook = tsf.MakeString("\u23a1", userWs);
			m_leftBracketExt = tsf.MakeString("\u23a2", userWs);
			m_leftBracketLowHook = tsf.MakeString("\u23a3", userWs);
			m_rightBracketUpHook = tsf.MakeString("\u23a4", userWs);
			m_rightBracketExt = tsf.MakeString("\u23a5", userWs);
			m_rightBracketLowHook = tsf.MakeString("\u23a6", userWs);
			m_leftBracket = tsf.MakeString("[", userWs);
			m_rightBracket = tsf.MakeString("]", userWs);
			m_leftParen = tsf.MakeString("(", userWs);
			m_rightParen = tsf.MakeString(")", userWs);
			m_zwSpace = tsf.MakeString("\u200b", userWs);
			m_leftParenUpHook = tsf.MakeString("\u239b", userWs);
			m_leftParenExt = tsf.MakeString("\u239c", userWs);
			m_leftParenLowHook = tsf.MakeString("\u239d", userWs);
			m_rightParenUpHook = tsf.MakeString("\u239e", userWs);
			m_rightParenExt = tsf.MakeString("\u239f", userWs);
			m_rightParenLowHook = tsf.MakeString("\u23a0", userWs);
			m_or = tsf.MakeString("OR", userWs);
			m_hash = tsf.MakeString("#", userWs);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragPattern:
					VwLength tableLen;
					tableLen.nVal = 10000;
					tableLen.unit = VwUnit.kunPercent100;
					vwenv.OpenTable(1, tableLen, 0, VwAlignment.kvaCenter, VwFramePosition.kvfpVoid, VwRule.kvrlNone, 0, 0, false);
					VwLength patternLen;
					patternLen.nVal = 1;
					patternLen.unit = VwUnit.kunRelative;
					vwenv.MakeColumns(1, patternLen);
					vwenv.OpenTableBody();
					vwenv.OpenTableRow();

					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
					vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint, 2000);
					vwenv.OpenTableCell(1, 1);
					vwenv.OpenParagraph();
					if (((ComplexConcPatternSda) vwenv.DataAccess).Root.IsLeaf)
					{
						OpenSingleLinePile(vwenv);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ComplexConcPatternSda.ktagChildren, this, kfragEmpty);
						CloseSingleLinePile(vwenv);
					}
					else
					{
						vwenv.AddObjVecItems(ComplexConcPatternSda.ktagChildren, this, kfragNode);
					}
					vwenv.CloseParagraph();
					vwenv.CloseTableCell();

					vwenv.CloseTableRow();
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					break;

				case kfragNode:
					ComplexConcPatternNode node = ((ComplexConcPatternSda) vwenv.DataAccess).Nodes[hvo];
					if (node is ComplexConcOrNode)
					{
						OpenSingleLinePile(vwenv);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagLeftBoundary, this, kfragZeroWidthSpace);
						vwenv.AddProp(ktagInnerNonBoundary, this, kfragOR);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagRightBoundary, this, kfragZeroWidthSpace);
						CloseSingleLinePile(vwenv);
					}
					else if (node is ComplexConcWordBdryNode)
					{
						OpenSingleLinePile(vwenv);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagLeftBoundary, this, kfragZeroWidthSpace);
						vwenv.AddProp(ktagInnerNonBoundary, this, kfragHash);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ktagRightBoundary, this, kfragZeroWidthSpace);
						CloseSingleLinePile(vwenv);
					}
					else if (node is ComplexConcGroupNode)
					{
						int numLines = GetNumLines(node);
						bool hasMinMax = node.Maximum != 1 || node.Minimum != 1;
						if (numLines == 1)
						{
							OpenSingleLinePile(vwenv);
							// use normal parentheses for a single line group
							vwenv.AddProp(ktagLeftBoundary, this, kfragLeftParen);

							vwenv.AddObjVecItems(ComplexConcPatternSda.ktagChildren, this, kfragNode);

							vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightParen);
							if (hasMinMax)
								DisplayMinMax(numLines, vwenv);
							CloseSingleLinePile(vwenv);
						}
						else
						{
							int maxNumLines = GetMaxNumLines(vwenv);
							vwenv.Props = m_bracketProps;
							vwenv.set_IntProperty((int) FwTextPropType.ktptMarginLeading, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
							vwenv.OpenInnerPile();
							AddExtraLines(maxNumLines - numLines, ktagLeftNonBoundary, vwenv);
							vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftParenUpHook);
							for (int i = 1; i < numLines - 1; i++)
								vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftParenExt);
							vwenv.AddProp(ktagLeftBoundary, this, kfragLeftParenLowHook);
							vwenv.CloseInnerPile();

							vwenv.AddObjVecItems(ComplexConcPatternSda.ktagChildren, this, kfragNode);

							vwenv.Props = m_bracketProps;
							vwenv.set_IntProperty((int) FwTextPropType.ktptMarginTrailing, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
							vwenv.OpenInnerPile();
							AddExtraLines(maxNumLines - numLines, hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, vwenv);
							vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, this, kfragRightParenUpHook);
							for (int i = 1; i < numLines - 1; i++)
								vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, this, kfragRightParenExt);
							vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightParenLowHook);
							vwenv.CloseInnerPile();
							if (hasMinMax)
								DisplayMinMax(numLines, vwenv);
						}
					}
					else
					{
						bool hasMinMax = node.Maximum != 1 || node.Minimum != 1;
						int numLines = GetNumLines(node);
						if (numLines == 1)
						{
							OpenSingleLinePile(vwenv);
							// use normal brackets for a single line constraint
							vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracket);

							DisplayFeatures(vwenv, node);

							vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightBracket);
							if (hasMinMax)
								DisplayMinMax(numLines, vwenv);
							CloseSingleLinePile(vwenv);
						}
						else
						{
							// left bracket pile
							int maxNumLines = GetMaxNumLines(vwenv);

							vwenv.Props = m_bracketProps;
							vwenv.set_IntProperty((int) FwTextPropType.ktptMarginLeading, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
							vwenv.OpenInnerPile();
							AddExtraLines(maxNumLines - numLines, ktagLeftNonBoundary, vwenv);
							vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketUpHook);
							for (int i = 1; i < numLines - 1; i++)
								vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketExt);
							vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracketLowHook);
							vwenv.CloseInnerPile();

							// feature pile
							vwenv.set_IntProperty((int) FwTextPropType.ktptAlign, (int) FwTextPropVar.ktpvEnum, (int) FwTextAlign.ktalLeft);
							vwenv.OpenInnerPile();
							AddExtraLines(maxNumLines - numLines, ktagInnerNonBoundary, vwenv);
							DisplayFeatures(vwenv, node);
							vwenv.CloseInnerPile();

							// right bracket pile
							vwenv.Props = m_bracketProps;
							vwenv.set_IntProperty((int) FwTextPropType.ktptMarginTrailing, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
							vwenv.OpenInnerPile();
							AddExtraLines(maxNumLines - numLines, hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, vwenv);
							vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, this, kfragRightBracketUpHook);
							for (int i = 1; i < numLines - 1; i++)
								vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightNonBoundary, this, kfragRightBracketExt);
							vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightBracketLowHook);
							vwenv.CloseInnerPile();
							if (hasMinMax)
								DisplayMinMax(numLines, vwenv);
						}
					}
					break;
			}
		}

		private void DisplayMinMax(int numLines, IVwEnv vwenv)
		{
			int superOffset = 0;
			if (numLines == 1)
			{
				// if the inner context is a single line, then make the min value a subscript and the max value a superscript.
				// I tried to use the Views subscript and superscript properties, but they added extra space so that it would
				// have the same line height of a normal character, which is not what I wanted, so I compute the size myself
				int fontHeight = GetFontHeight(m_cache.DefaultUserWs);
				int superSubHeight = (fontHeight * 2) / 3;
				vwenv.set_IntProperty((int) FwTextPropType.ktptFontSize, (int) FwTextPropVar.ktpvMilliPoint, superSubHeight);
				vwenv.set_IntProperty((int) FwTextPropType.ktptLineHeight, (int) FwTextPropVar.ktpvMilliPoint, -superSubHeight);
				superOffset = superSubHeight / 2;
			}
			else
			{
				vwenv.set_IntProperty((int) FwTextPropType.ktptMarginTrailing, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
			}
			vwenv.OpenInnerPile();
			if (numLines == 1)
				vwenv.set_IntProperty((int) FwTextPropType.ktptOffset, (int) FwTextPropVar.ktpvMilliPoint, superOffset);
			vwenv.OpenParagraph();
			vwenv.AddProp(ktagRightNonBoundary, this, kfragNodeMax);
			vwenv.CloseParagraph();
			AddExtraLines(numLines - 2, ktagRightNonBoundary, vwenv);
			vwenv.set_IntProperty((int) FwTextPropType.ktptOffset, (int) FwTextPropVar.ktpvMilliPoint, 0);
			vwenv.OpenParagraph();
			vwenv.AddProp(ktagRightBoundary, this, kfragNodeMin);
			vwenv.CloseParagraph();
			vwenv.CloseInnerPile();
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

				case kfragFeatureLine:
					ComplexConcPatternNode node = ((ComplexConcPatternSda) vwenv.DataAccess).Nodes[vwenv.CurrentObject()];
					switch (tag)
					{
						case ktagType:
							string typeStr = null;
							if (node is ComplexConcMorphNode)
								typeStr = ITextStrings.ksComplexConcMorph;
							else if (node is ComplexConcWordNode)
								typeStr = ITextStrings.ksComplexConcWord;
							else if (node is ComplexConcTagNode)
								typeStr = ITextStrings.ksComplexConcTag;
							tss = CreateFeatureLine(ITextStrings.ksComplexConcType, typeStr, m_cache.DefaultUserWs);
							break;

						case ktagForm:
							ITsString form = null;
							var formMorphNode = node as ComplexConcMorphNode;
							if (formMorphNode != null)
							{
								form = formMorphNode.Form;
							}
							else
							{
								var formWordNode = node as ComplexConcWordNode;
								if (formWordNode != null)
									form = formWordNode.Form;
							}
							Debug.Assert(form != null);
							tss = CreateFeatureLine(ITextStrings.ksComplexConcForm, form, false);
							break;

						case ktagEntry:
							ITsString entry = null;
							var entryMorphNode = node as ComplexConcMorphNode;
							if (entryMorphNode != null)
								entry = entryMorphNode.Entry;
							Debug.Assert(entry != null);
							tss = CreateFeatureLine(ITextStrings.ksComplexConcEntry, entry, false);
							break;

						case ktagGloss:
							ITsString gloss = null;
							var glossMorphNode = node as ComplexConcMorphNode;
							if (glossMorphNode != null)
							{
								gloss = glossMorphNode.Gloss;
							}
							else
							{
								var glossWordNode = node as ComplexConcWordNode;
								if (glossWordNode != null)
									gloss = glossWordNode.Gloss;
							}
							Debug.Assert(gloss != null);
							tss = CreateFeatureLine(ITextStrings.ksComplexConcGloss, gloss, false);
							break;

						case ktagCategory:
							IPartOfSpeech category = null;
							bool catNegated = false;
							var catMorphNode = node as ComplexConcMorphNode;
							if (catMorphNode != null)
							{
								category = catMorphNode.Category;
								catNegated = catMorphNode.NegateCategory;
							}
							else
							{
								var catWordNode = node as ComplexConcWordNode;
								if (catWordNode != null)
								{
									category = catWordNode.Category;
									catNegated = catWordNode.NegateCategory;
								}
							}
							Debug.Assert(category != null);
							tss = CreateFeatureLine(ITextStrings.ksComplexConcCategory, category.Abbreviation.BestAnalysisAlternative, catNegated);
							break;

						case ktagTag:
							ICmPossibility tagPoss = null;
							var tagNode = node as ComplexConcTagNode;
							if (tagNode != null)
								tagPoss = tagNode.Tag;
							Debug.Assert(tagPoss != null);
							tss = CreateFeatureLine(ITextStrings.ksComplexConcTag, tagPoss.Abbreviation.BestAnalysisAlternative, false);
							break;

						case ktagInfl:
							tss = CreateFeatureLine(ITextStrings.ksComplexConcInflFeatures, null, false);
							break;

						default:
							IFsFeatDefn feature = m_curInflFeatures.Keys.Single(f => f.Hvo == tag);
							if (feature is IFsComplexFeature)
							{
								tss = CreateFeatureLine(feature.Abbreviation.BestAnalysisAlternative, null, false);
							}
							else if (feature is IFsClosedFeature)
							{
								var value = (ClosedFeatureValue) m_curInflFeatures[feature];
								tss = CreateFeatureLine(feature.Abbreviation.BestAnalysisAlternative, value.Symbol.Abbreviation.BestAnalysisAlternative, value.Negate);
							}
							break;
					}
					break;

				case kfragNodeMax:
					// if the max value is -1, it indicates that it is infinite
					ComplexConcPatternNode node1 = ((ComplexConcPatternSda) vwenv.DataAccess).Nodes[vwenv.CurrentObject()];
					tss = node1.Maximum == -1 ? m_infinity : m_cache.TsStrFactory.MakeString(node1.Maximum.ToString(CultureInfo.InvariantCulture), m_cache.DefaultUserWs);
					break;

				case kfragNodeMin:
					ComplexConcPatternNode node2 = ((ComplexConcPatternSda) vwenv.DataAccess).Nodes[vwenv.CurrentObject()];
					tss = m_cache.TsStrFactory.MakeString(node2.Minimum.ToString(CultureInfo.InvariantCulture), m_cache.DefaultUserWs);
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

				case kfragLeftParen:
					tss = m_leftParen;
					break;

				case kfragRightParen:
					tss = m_rightParen;
					break;

				case kfragZeroWidthSpace:
					tss = m_zwSpace;
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

				case kfragOR:
					tss = m_or;
					break;

				case kfragHash:
					tss = m_hash;
					break;
			}
			return tss;
		}

		private void DisplayFeatures(IVwEnv vwenv, ComplexConcPatternNode node)
		{
			vwenv.AddProp(ktagType, this, kfragFeatureLine);
			var morphNode = node as ComplexConcMorphNode;
			if (morphNode != null)
			{
				if (morphNode.Form != null)
					vwenv.AddProp(ktagForm, this, kfragFeatureLine);
				if (morphNode.Entry != null)
					vwenv.AddProp(ktagEntry, this, kfragFeatureLine);
				if (morphNode.Category != null)
					vwenv.AddProp(ktagCategory, this, kfragFeatureLine);
				if (morphNode.Gloss != null)
					vwenv.AddProp(ktagGloss, this, kfragFeatureLine);
				if (morphNode.InflFeatures.Count > 0)
				{
					vwenv.OpenParagraph();
					vwenv.AddProp(ktagInfl, this, kfragFeatureLine);
					DisplayInflFeatures(vwenv, morphNode.InflFeatures);
					vwenv.CloseParagraph();
				}
			}
			else
			{
				var wordNode = node as ComplexConcWordNode;
				if (wordNode != null)
				{
					if (wordNode.Form != null)
						vwenv.AddProp(ktagForm, this, kfragFeatureLine);
					if (wordNode.Category != null)
						vwenv.AddProp(ktagCategory, this, kfragFeatureLine);
					if (wordNode.Gloss != null)
						vwenv.AddProp(ktagGloss, this, kfragFeatureLine);
					if (wordNode.InflFeatures.Count > 0)
					{
						vwenv.OpenParagraph();
						vwenv.AddProp(ktagInfl, this, kfragFeatureLine);
						DisplayInflFeatures(vwenv, wordNode.InflFeatures);
						vwenv.CloseParagraph();
					}
				}
				else
				{
					var tagNode = node as ComplexConcTagNode;
					if (tagNode != null)
					{
						if (tagNode.Tag != null)
							vwenv.AddProp(ktagTag, this, kfragFeatureLine);
					}
				}
			}
		}

		private void DisplayInflFeatureLines(IVwEnv vwenv, IDictionary<IFsFeatDefn, object> inflFeatures, bool openPara)
		{
			IDictionary<IFsFeatDefn, object> lastInflFeatures = m_curInflFeatures;
			m_curInflFeatures = inflFeatures;
			foreach (KeyValuePair<IFsFeatDefn, object> kvp in inflFeatures)
			{
				if (kvp.Key is IFsComplexFeature)
				{
					if (openPara)
						vwenv.OpenParagraph();
					vwenv.AddProp(kvp.Key.Hvo, this, kfragFeatureLine);
					DisplayInflFeatures(vwenv, (IDictionary<IFsFeatDefn, object>) kvp.Value);
					if (openPara)
						vwenv.CloseParagraph();
				}
				else
				{
					vwenv.AddProp(kvp.Key.Hvo, this, kfragFeatureLine);
				}
			}
			m_curInflFeatures = lastInflFeatures;
		}

		private void DisplayInflFeatures(IVwEnv vwenv, IDictionary<IFsFeatDefn, object> inflFeatures)
		{
			int numLines = GetNumLines(inflFeatures);
			if (numLines == 1)
			{
				// use normal brackets for a single line constraint
				vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);

				DisplayInflFeatureLines(vwenv, inflFeatures, false);

				vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);
			}
			else
			{
				// left bracket pile
				int maxNumLines = GetMaxNumLines(vwenv);

				vwenv.Props = m_bracketProps;
				vwenv.set_IntProperty((int) FwTextPropType.ktptMarginLeading, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
				vwenv.OpenInnerPile();
				vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketUpHook);
				for (int i = 1; i < numLines - 1; i++)
					vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketExt);
				vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracketLowHook);
				vwenv.CloseInnerPile();

				// feature pile
				vwenv.set_IntProperty((int) FwTextPropType.ktptAlign, (int) FwTextPropVar.ktpvEnum, (int) FwTextAlign.ktalLeft);
				vwenv.OpenInnerPile();
				DisplayInflFeatureLines(vwenv, inflFeatures, true);
				vwenv.CloseInnerPile();

				// right bracket pile
				vwenv.Props = m_bracketProps;
				vwenv.set_IntProperty((int) FwTextPropType.ktptMarginTrailing, (int) FwTextPropVar.ktpvMilliPoint, PileMargin);
				vwenv.OpenInnerPile();
				vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracketUpHook);
				for (int i = 1; i < numLines - 1; i++)
					vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracketExt);
				vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracketLowHook);
				vwenv.CloseInnerPile();
			}
		}

		public ITsString CreateFeatureLine(ITsString name, ITsString value, bool negated)
		{
			ITsIncStrBldr featLine = TsIncStrBldrClass.Create();
			featLine.AppendTsString(name);
			featLine.Append(": ");
			if (value != null)
			{
				if (negated)
					featLine.AppendTsString(m_tsf.MakeString("!", m_cache.DefaultUserWs));
				featLine.AppendTsString(value);
			}
			return featLine.GetString();
		}

		public ITsString CreateFeatureLine(string name, ITsString value, bool negated)
		{
			return CreateFeatureLine(m_cache.TsStrFactory.MakeString(name, m_cache.DefaultUserWs), value, negated);
		}

		private ITsString CreateFeatureLine(string name, string value, int ws)
		{
			return CreateFeatureLine(name, m_cache.TsStrFactory.MakeString(value, ws), false);
		}

		private int GetMaxNumLines(IVwEnv vwenv)
		{
			ComplexConcPatternNode root = ((ComplexConcPatternSda) vwenv.DataAccess).Root;
			return GetNumLines(root);
		}

		private int GetNumLines(ComplexConcPatternNode node)
		{
			var morphNode = node as ComplexConcMorphNode;
			if (morphNode != null)
			{
				int numLines = 1;
				if (morphNode.Form != null)
					numLines++;
				if (morphNode.Entry != null)
					numLines++;
				if (morphNode.Gloss != null)
					numLines++;
				if (morphNode.Category != null)
					numLines++;
				numLines += GetNumLines(morphNode.InflFeatures);
				return numLines;
			}

			var wordNode = node as ComplexConcWordNode;
			if (wordNode != null)
			{
				int numLines = 1;
				if (wordNode.Form != null)
					numLines++;
				if (wordNode.Gloss != null)
					numLines++;
				if (wordNode.Category != null)
					numLines++;
				numLines += GetNumLines(wordNode.InflFeatures);
				return numLines;
			}

			var tagNode = node as ComplexConcTagNode;
			if (tagNode != null)
			{
				int numLines = 1;
				if (tagNode.Tag != null)
					numLines++;
				return numLines;
			}

			if (!node.IsLeaf)
				return node.Children.Max(n => GetNumLines(n));

			return 1;
		}

		private int GetNumLines(IDictionary<IFsFeatDefn, object> inflFeatures)
		{
			int num = 0;
			foreach (KeyValuePair<IFsFeatDefn, object> kvp in inflFeatures)
			{
				if (kvp.Key is IFsComplexFeature)
					num += GetNumLines((IDictionary<IFsFeatDefn, object>) kvp.Value);
				else
					num++;
			}
			return num;
		}

		/// <summary>
		/// Gets the font height of the specified writing system for the normal style.
		/// </summary>
		/// <param name="ws">The ws.</param>
		/// <returns></returns>
		private int GetFontHeight(int ws)
		{
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			return FontHeightAdjuster.GetFontHeightForStyle("Normal", stylesheet,
				ws, m_cache.LanguageWritingSystemFactoryAccessor);
		}

		private void AddExtraLines(int numLines, int tag, IVwEnv vwenv)
		{
			for (int i = 0; i < numLines; i++)
			{
				vwenv.Props = m_bracketProps;
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
				vwenv.OpenParagraph();
				vwenv.AddProp(tag, this, kfragEmpty);
				vwenv.CloseParagraph();
			}
		}

		private void OpenSingleLinePile(IVwEnv vwenv)
		{
			vwenv.Props = m_pileProps;
			vwenv.OpenInnerPile();
			AddExtraLines(GetMaxNumLines(vwenv) - 1, ktagLeftNonBoundary, vwenv);
			vwenv.OpenParagraph();
		}

		private void CloseSingleLinePile(IVwEnv vwenv)
		{
			vwenv.CloseParagraph();
			vwenv.CloseInnerPile();
		}
	}
}
