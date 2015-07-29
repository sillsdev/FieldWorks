// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SharpViewsDemo
{
	class ParagraphOpsDemo : ParagraphOperations<ParagraphDemo>
	{
		private ParagraphOwnerDemo m_owner;
		public ParagraphOpsDemo(ParagraphOwnerDemo owner)
		{
			m_owner = owner;
			List = owner.Paragraphs;
		}

		// We override the default insert function because we need to call a method that
		// raises the event as well as inserting the item.
		public override ParagraphDemo MakeListItem(int index, bool ipAtStartOfPara)
		{
			var paragraphDemo = new ParagraphDemo();
			paragraphDemo.ParaStyle = ipAtStartOfPara
										? m_owner.Paragraphs[index].ParaStyle
										: paragraphDemo.ParaStyle = m_owner.Paragraphs[index - 1].ParaStyle;
			m_owner.InsertParagraph(index, paragraphDemo);
			return paragraphDemo;
		}

		public override IStyle CreateStyle(List<string> styleProps, string name, Dictionary<int, Color> colorTable)
		{
			IStyle style;
			DemoStyleProp<FwTextAlign> alignment = null;
			DemoStyleProp<int> firstLineIndent = null;
			DemoStyleProp<int> marginLeading = null;
			DemoStyleProp<int> marginTrailing = null;
			DemoStyleProp<int> marginTop = null;
			DemoStyleProp<int> marginBottom = null;
			DemoStyleProp<LineHeightInfo> lineSpacing = null;
			DemoStyleProp<int> borderLeading = null;
			DemoStyleProp<int> borderTrailing = null;
			DemoStyleProp<int> borderTop = null;
			DemoStyleProp<int> borderBottom = null;
			DemoStyleProp<Color> borderColor = null;
			DemoStyleProp<bool> bold = null;
			DemoStyleProp<bool> italic = null;
			DemoStyleProp<string> fontName = null;
			DemoStyleProp<int> fontSize = null;
			DemoStyleProp<Color> fontColor = null;
			DemoStyleProp<Color> underlineColor = null;
			DemoStyleProp<FwSuperscriptVal> superSub = null;
			DemoStyleProp<Color> highlight = null;
			string defaultStyleName = name;
			string additiveStyleName = "";

			foreach (string prop in styleProps)
			{
				int placeHolder;
				if (prop.StartsWith("b"))
				{
					bold = new DemoStyleProp<bool> {Value = true};
					defaultStyleName += "+Bold";
				}
				else if (prop.StartsWith("i"))
				{
					italic = new DemoStyleProp<bool> {Value = true};
					defaultStyleName += "+Italic";
				}
				else if (prop.StartsWith("fs"))
				{
					if (int.TryParse(prop.Substring(2), out placeHolder))
					{
						fontSize = new DemoStyleProp<int> { Value = placeHolder };
						defaultStyleName += "+" + fontSize.Value + "pt";
					}
				}
				else if (prop.StartsWith("f"))
				{
					fontName = new DemoStyleProp<string> {Value = prop.Substring(1)};
					defaultStyleName += "+" + fontName.Value;
				}
				else if (prop.StartsWith("cf"))
				{
					fontColor = new DemoStyleProp<Color> {Value = colorTable[int.Parse(prop.Substring(2))]};
					defaultStyleName += "+" + fontColor.Value + "Font";
				}
				else if (prop.StartsWith("ulc"))
				{
					underlineColor = new DemoStyleProp<Color> {Value = colorTable[int.Parse(prop.Substring(2))]};
					defaultStyleName += "+" + underlineColor.Value + "Underline";
				}
				else if (prop.StartsWith("nosupersub"))
				{
					superSub = new DemoStyleProp<FwSuperscriptVal> {Value = FwSuperscriptVal.kssvOff};
					defaultStyleName += "+NoSuperSub";
				}
				else if (prop.StartsWith("super"))
				{
					superSub = new DemoStyleProp<FwSuperscriptVal> {Value = FwSuperscriptVal.kssvSuper};
					defaultStyleName += "+Super";
				}
				else if (prop.StartsWith("sub"))
				{
					superSub = new DemoStyleProp<FwSuperscriptVal> {Value = FwSuperscriptVal.kssvSub};
					defaultStyleName += "+Sub";
				}
				else if (prop.StartsWith("highlight"))
				{
					highlight = new DemoStyleProp<Color> {Value = colorTable[int.Parse(prop.Substring(9, prop.Length - 9))]};
					defaultStyleName += "+" + highlight.Value + "Highlight";
				}
				else if (prop == "ql")
				{
					alignment = new DemoStyleProp<FwTextAlign> {Value = FwTextAlign.ktalLeft};
					defaultStyleName += "+AlignLeft";
				}
				else if (prop == "qr")
				{
					alignment = new DemoStyleProp<FwTextAlign> {Value = FwTextAlign.ktalRight};
					defaultStyleName += "+AlignRight";
				}
				else if (prop == "qc")
				{
					alignment = new DemoStyleProp<FwTextAlign> {Value = FwTextAlign.ktalCenter};
					defaultStyleName += "+AlignCenter";
				}
				else if (prop == "qj")
				{
					alignment = new DemoStyleProp<FwTextAlign> {Value = FwTextAlign.ktalJustify};
					defaultStyleName += "+AlignJustify";
				}
				else if (prop.StartsWith("fi"))
				{
					if (int.TryParse(prop.Substring(2), out placeHolder))
					{
						firstLineIndent = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + firstLineIndent.Value + "FirstLineIndent";
					}
				}
				else if (prop.StartsWith("lin"))
				{
					if (int.TryParse(prop.Substring(3), out placeHolder))
					{
						marginLeading = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + marginLeading.Value + "MargLead";
					}
				}
				else if (prop.StartsWith("rin"))
				{
					if (int.TryParse(prop.Substring(3), out placeHolder))
					{
						marginTrailing = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + marginTrailing + "MargTrail";
					}
				}
				else if (prop.StartsWith("sa"))
				{
					if (int.TryParse(prop.Substring(2), out placeHolder))
					{
						marginTop = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + marginTop + "MargTop";
					}
				}
				else if (prop.StartsWith("sb"))
				{
					if (int.TryParse(prop.Substring(2), out placeHolder))
					{
						marginBottom = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + marginBottom + "MargBot";
					}
				}
				else if (prop.StartsWith("sl"))
				{
					if (int.TryParse(prop.Substring(2), out placeHolder))
					{
						lineSpacing = new DemoStyleProp<LineHeightInfo> {Value = new LineHeightInfo(placeHolder*50, false)};
						defaultStyleName += "+" + lineSpacing.Value + "LineSpace";
					}
					else if (prop.Substring(0, 6) == "slmult")
						if (int.TryParse(prop.Substring(6, prop.Length - 6), out placeHolder))
						{
							placeHolder *= 50;
							if (placeHolder == 10000 || placeHolder == 15000 || placeHolder == 20000)
							{
								lineSpacing = new DemoStyleProp<LineHeightInfo> {Value = new LineHeightInfo(placeHolder, true)};
								defaultStyleName += "+" + lineSpacing.Value + "LineSpace";
							}
						}
				}
				else if (prop.StartsWith("brdrlbrdrsbrdrw"))
				{
					if (int.TryParse(prop.Substring(15), out placeHolder))
					{
						borderLeading = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + borderLeading.Value + "BordLead";
					}
					else if (prop.Contains("brsp"))
					{
						int index = prop.IndexOf("brsp");
						if (int.TryParse(prop.Substring(15, index - 15), out placeHolder))
						{
							borderLeading = new DemoStyleProp<int> {Value = placeHolder*50};
							defaultStyleName += "+" + borderLeading.Value + "BordLead";
						}
						char c = '0';
						index += 3;
						while (c < 40 && c > 29)
						{
							index++;
							c = prop[index];
						}
						borderColor = new DemoStyleProp<Color> {Value = colorTable[int.Parse(prop.Substring(2))]};
						defaultStyleName += "+" + borderColor.Value + "Border";
					}
				}
				else if (prop.StartsWith("brdrrbrdrsbrdrw"))
				{
					if (int.TryParse(prop.Substring(15), out placeHolder))
					{
						borderTrailing = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + borderTrailing.Value + "BordTrail";
					}
					else if (prop.Contains("brsp"))
					{
						int index = prop.IndexOf("brsp");
						if (int.TryParse(prop.Substring(15, index - 15), out placeHolder))
						{
							borderTrailing = new DemoStyleProp<int> {Value = placeHolder*50};
							defaultStyleName += "+" + borderTrailing.Value + "BordTrail";
						}
						char c = '0';
						index += 3;
						while (c < 40 && c > 29)
						{
							index++;
							c = prop[index];
						}
						borderColor = new DemoStyleProp<Color> {Value = colorTable[int.Parse(prop.Substring(2))]};
						defaultStyleName += "+" + borderColor.Value + "Border";
					}
				}
				else if (prop.StartsWith("brdrtbrdrsbrdrw"))
				{
					if (int.TryParse(prop.Substring(15), out placeHolder))
					{
						borderTop = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + borderTop.Value + "BordTop";
					}
					else if (prop.Contains("brsp"))
					{
						int index = prop.IndexOf("brsp");
						if (int.TryParse(prop.Substring(15, index - 15), out placeHolder))
						{
							borderTop = new DemoStyleProp<int> {Value = placeHolder*50};
							defaultStyleName += "+" + borderTop.Value + "BordTop";
						}
						char c = '0';
						index += 3;
						while (c < 40 && c > 29)
						{
							index++;
							c = prop[index];
						}
						borderColor = new DemoStyleProp<Color> {Value = colorTable[int.Parse(prop.Substring(2))]};
						defaultStyleName += "+" + borderColor.Value + "Border";
					}
				}
				else if (prop.StartsWith("brdrbbrdrsbrdrw"))
				{
					if (int.TryParse(prop.Substring(15), out placeHolder))
					{
						borderBottom = new DemoStyleProp<int> {Value = placeHolder*50};
						defaultStyleName += "+" + borderBottom.Value + "BordBot";
					}
					else if (prop.Contains("brsp"))
					{
						int index = prop.IndexOf("brsp");
						if (int.TryParse(prop.Substring(15, index - 15), out placeHolder))
						{
							borderBottom = new DemoStyleProp<int> {Value = placeHolder*50};
							defaultStyleName += "+" + borderBottom.Value + "BordBot";
						}
						char c = '0';
						index += 3;
						while (c < 58 && c > 47)
						{
							index++;
							c = prop[index];
						}
						borderColor = new DemoStyleProp<Color> {Value = colorTable[int.Parse(prop.Substring(2))]};
						defaultStyleName += "+" + borderColor.Value + "Border";
					}
				}
				else if(prop.StartsWith("additive"))
				{
					additiveStyleName = prop.Substring(9);
				}
			}
			if (name.StartsWith("\\s") || name.StartsWith("\\pard"))
				style = new DemoStyle
							{
								ParagraphStyleInfo =
									new DemoParaStyleInfo
										{
											Alignment = alignment,
											FirstLineIndent = firstLineIndent,
											MarginLeading = marginLeading,
											MarginTrailing = marginTrailing,
											MarginTop = marginTop,
											MarginBottom = marginBottom,
											LineHeight = lineSpacing,
											BorderLeading = borderLeading,
											BorderTrailing = borderTrailing,
											BorderTop = borderTop,
											BorderBottom = borderBottom,
											BorderColor = borderColor
										},
								DefaultCharacterStyleInfo = new DemoCharStyleInfo
																{
																	BackColor = highlight,
																	Bold = bold,
																	FontColor = fontColor,
																	FontName = fontName,
																	FontSize = fontSize,
																	Italic = italic,
																	SuperSub = superSub,
																	UnderlineColor = underlineColor
																},
								Name = additiveStyleName == "" ? defaultStyleName : additiveStyleName
							};
			else if (name.StartsWith("cs"))
				style = new DemoStyle
							{
								DefaultCharacterStyleInfo =
									new DemoCharStyleInfo
										{
											BackColor = highlight,
											Bold = bold,
											FontColor = fontColor,
											FontName = fontName,
											FontSize = fontSize,
											Italic = italic,
											SuperSub = superSub,
											UnderlineColor = underlineColor
										},
								Name = additiveStyleName == "" ? defaultStyleName : additiveStyleName
							};
			else
				style = null;
			return style;
		}

		public override void SetString(ParagraphDemo destination, string val)
		{
			destination.Contents = val;
			destination.TsContents = TsStrFactoryClass.Create().MakeString(val, 1);
		}

		public override void SetString(ParagraphDemo destination, ITsString val)
		{
			destination.TsContents = val;
			destination.Contents = val.Text;
		}

		public override void AddStyle(IStyle style, IStylesheet stylesheet)
		{
			((DemoStyleSheet) stylesheet).SetStyle(style.Name, style);
		}

		public override void ApplyParagraphStyle(int index, int numBoxes, string style)
		{
			if (style != null)
			{
				int i = index;
				while (i < index + numBoxes)
				{
					m_owner.Paragraphs[i].ParaStyle = style;
					i++;
				}
				m_owner.Paragraphs.SimulateChange(index, numBoxes);
			}
		}
	}
}
