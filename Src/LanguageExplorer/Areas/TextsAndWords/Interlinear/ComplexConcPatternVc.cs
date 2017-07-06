// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public class ComplexConcPatternVc : PatternVcBase
	{
		// normal frags
		public const int kfragPattern = 100;
		public const int kfragNode = 101;

		// variant frags
		public const int kfragFeatureLine = 103;
		public const int kfragNodeMax = 104;
		public const int kfragNodeMin = 105;
		public const int kfragOR = 106;
		public const int kfragHash = 107;

		// fake flids
		public const int ktagType = -200;
		public const int ktagForm = -201;
		public const int ktagGloss = -202;
		public const int ktagCategory = -203;
		public const int ktagEntry = -204;
		public const int ktagTag = -205;
		public const int ktagInfl = -206;

		private readonly ITsString m_infinity;
		private readonly ITsString m_or;
		private readonly ITsString m_hash;

		private IDictionary<IFsFeatDefn, object> m_curInflFeatures;

		public ComplexConcPatternVc(LcmCache cache, IPropertyTable propertyTable)
			: base(cache, propertyTable)
		{
			int userWs = m_cache.DefaultUserWs;
			m_infinity = TsStringUtils.MakeString("\u221e", userWs);
			m_or = TsStringUtils.MakeString("OR", userWs);
			m_hash = TsStringUtils.MakeString("#", userWs);
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
						OpenSingleLinePile(vwenv, GetMaxNumLines(vwenv), false);
						vwenv.Props = m_bracketProps;
						vwenv.AddProp(ComplexConcPatternSda.ktagChildren, this, kfragEmpty);
						CloseSingleLinePile(vwenv, false);
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
					int maxNumLines = GetMaxNumLines(vwenv);
					if (node is ComplexConcOrNode)
					{
						OpenSingleLinePile(vwenv, maxNumLines);
						vwenv.AddProp(ktagInnerNonBoundary, this, kfragOR);
						CloseSingleLinePile(vwenv, false);
					}
					else if (node is ComplexConcWordBdryNode)
					{
						OpenSingleLinePile(vwenv, maxNumLines);
						vwenv.AddProp(ktagInnerNonBoundary, this, kfragHash);
						CloseSingleLinePile(vwenv);
					}
					else if (node is ComplexConcGroupNode)
					{
						int numLines = GetNumLines(node);
						bool hasMinMax = node.Maximum != 1 || node.Minimum != 1;
						if (numLines == 1)
						{
							OpenSingleLinePile(vwenv, maxNumLines, false);
							// use normal parentheses for a single line group
							vwenv.AddProp(ktagLeftBoundary, this, kfragLeftParen);

							vwenv.AddObjVecItems(ComplexConcPatternSda.ktagChildren, this, kfragNode);

							vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightParen);
							if (hasMinMax)
								DisplayMinMax(numLines, vwenv);
							CloseSingleLinePile(vwenv, false);
						}
						else
						{
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
							OpenSingleLinePile(vwenv, maxNumLines, false);
							// use normal brackets for a single line constraint
							vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracket);

							DisplayFeatures(vwenv, node);

							vwenv.AddProp(hasMinMax ? ktagInnerNonBoundary : ktagRightBoundary, this, kfragRightBracket);
							if (hasMinMax)
								DisplayMinMax(numLines, vwenv);
							CloseSingleLinePile(vwenv, false);
						}
						else
						{
							// left bracket pile
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
					tss = node1.Maximum == -1 ? m_infinity : TsStringUtils.MakeString(node1.Maximum.ToString(CultureInfo.InvariantCulture), m_cache.DefaultUserWs);
					break;

				case kfragNodeMin:
					var node2 = ((ComplexConcPatternSda) vwenv.DataAccess).Nodes[vwenv.CurrentObject()];
					tss = TsStringUtils.MakeString(node2.Minimum.ToString(CultureInfo.InvariantCulture), m_cache.DefaultUserWs);
					break;

				case kfragOR:
					tss = m_or;
					break;

				case kfragHash:
					tss = m_hash;
					break;

				default:
					tss = base.DisplayVariant(vwenv, tag, frag);
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
			ITsIncStrBldr featLine = TsStringUtils.MakeIncStrBldr();
			featLine.AppendTsString(name);
			featLine.Append(": ");
			if (value != null)
			{
				if (negated)
					featLine.AppendTsString(TsStringUtils.MakeString("!", m_cache.DefaultUserWs));
				featLine.AppendTsString(value);
			}
			return featLine.GetString();
		}

		public ITsString CreateFeatureLine(string name, ITsString value, bool negated)
		{
			return CreateFeatureLine(TsStringUtils.MakeString(name, m_cache.DefaultUserWs), value, negated);
		}

		private ITsString CreateFeatureLine(string name, string value, int ws)
		{
			return CreateFeatureLine(name, TsStringUtils.MakeString(value, ws), false);
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
	}
}
