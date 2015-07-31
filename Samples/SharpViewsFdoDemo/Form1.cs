// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.SharpViews;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;
using TextSource = SIL.FieldWorks.SharpViews.TextSource;

namespace SharpViewsDemo
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			AllowDrop = true;
		}

		private void whichView_SelectedIndexChanged(object sender, EventArgs e)
		{
			wsSelector1.Visible = false;
			wsSelector2.Visible = false;
			styleChooser.Visible = false;
			switch (whichView.SelectedItem as string)
			{
				case "Red Box":
					InitRedBox();
					break;
				case "Several boxes":
					InitSeveralBoxes();
					break;
				case "Simple Text Para":
					InitSimpleTextPara();
					break;
				case "Echo Para":
					InitDoubleString();
					break;
				case "MultiPara":
					InitMultiPara();
					break;
				case "Styled text":
					InitStyledText();
					break;
				case "Long text":
					InitLongText();
					break;
				case "Text with prompts":
					InitTextWithPrompts();
					break;
				case "Multilingual Strings":
					InitMultiLingualStrings();
					break;
				case "Stylesheet Chooser":
					InitStyleSheetChooser();
					break;
				case "Proportional Row Boxes":
					InitProportionalRowBoxes();
					break;
				case "Fixed Row Boxes":
					InitFixedRowBoxes();
					break;
			}
		}
		private int m_Ws1;
		private int m_Ws2;
		private int Ws1
		{
			get { return m_Ws1; }
			set
			{
				m_Ws1 = value;
				StartupMultiLingualStrings();
			}
		}
		private int Ws2
		{
			get { return m_Ws2; }
			set
			{
				m_Ws2 = value;
				StartupMultiLingualStrings();
			}
		}

		IViewMultiString multiLingString;
		private MultiLingualStringOwner m_mlsOwner;

		private void InitMultiLingualStrings()
		{
			m_mlsOwner = new MultiLingualStringOwner();
			multiLingString = m_mlsOwner.MyMultiString;
			m_Ws2 = 2; // don't call StartupMultiLingualStrings
			m_Ws1 = 1;
			ITsString ts1 =
				TsStrFactoryClass.Create().MakeString("This is the day that the Lord has made. We will rejoice and be glad in it", 1);
			var bldr1 = ts1.GetBldr();
			bldr1.SetStrPropValue(0, ts1.Text.Length, (int)FwTextPropType.ktptNamedStyle, "Normal");
			ts1 = bldr1.GetString();
			ITsString ts2 =
				TsStrFactoryClass.Create().MakeString("This is the day that the Lord has made.", 2);
			var bldr2 = ts2.GetBldr();
			bldr2.SetStrPropValue(0, ts2.Text.Length, (int)FwTextPropType.ktptNamedStyle, "Normal");
			ts2 = bldr2.GetString();
			ITsString ts3 =
				TsStrFactoryClass.Create().MakeString("This is the day", 3);
			var bldr3 = ts3.GetBldr();
			bldr3.SetStrPropValue(0, ts3.Text.Length, (int)FwTextPropType.ktptNamedStyle, "Normal");
			ts3 = bldr3.GetString();
			ITsString ts4 =
				TsStrFactoryClass.Create().MakeString("This", 4);
			var bldr4 = ts4.GetBldr();
			bldr4.SetStrPropValue(0, ts4.Text.Length, (int)FwTextPropType.ktptNamedStyle, "Normal");
			ts4 = bldr4.GetString();
			multiLingString.set_String(1, ts1);
			multiLingString.set_String(2, ts2);
			multiLingString.set_String(3, ts3);
			multiLingString.set_String(4, ts4);
			wsSelector1.Visible = true;
			wsSelector2.Visible = true;
			styleChooser.Visible = true;
			wsSelector1.SelectedIndexChanged += wsSelector1_SelectedIndexChanged;
			wsSelector2.SelectedIndexChanged += wsSelector2_SelectedIndexChanged;
			StartupMultiLingualStrings();
		}

		void StartupMultiLingualStrings()
		{
			var stylesheet = SetupStyles();
			var styles = new AssembledStyles(stylesheet);
			RootBox root = new RootBoxFdo(styles);
			var owner = new ParagraphOwnerDemo();
			var paraDemo = new ParagraphDemo {ParaStyle = "Left"};
			paraDemo.MlsContents = m_mlsOwner.MyMultiString;
			owner.InsertParagraph(0, paraDemo);
			root.Builder.Show(Display.Of(() => owner.Paragraphs).Using(
				(builder, para) =>
				builder.Show(Paragraph.Containing(Display.Of(() => para.MlsContents, Ws2)).Style(para.ParaStyle))),
							  Display.Of(() => owner.Paragraphs).Using(
								(builder, para) =>
								builder.Show(Paragraph.Containing(Display.Of(() => para.MlsContents, Ws1)).Style(para.ParaStyle))))
				.EditParagraphsUsing(new ParagraphOpsDemo(owner));
			wsSelector1.SelectedItem = wsSelector1.Items[Ws1 - 1];
			wsSelector2.SelectedItem = wsSelector2.Items[Ws2 - 1];
			theSharpView.Root = root;
			root.SelectAtStart().Install();
			theSharpView.Focus();
		}

		private void InitTextWithPrompts()
		{
			var styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			root.RendererFactory = theSharpView.RendererFactory;
			var obj0 = new ParagraphDemo { Contents = "one" };
			var obj1 = new ParagraphDemo { Contents = "" };
			var obj2 = new ParagraphDemo { Contents = "three" };
			int ws = 1; // arbitrary with default renderer factory.
			root.Builder.Show(
				Paragraph.Containing(
					Display.Of(() => obj0.Contents, ws).WhenEmpty("prompt", ws),
					Display.Of(" "),
					Display.Of(() => obj1.Contents, ws).WhenEmpty("prompt", ws),
					Display.Of(" "),
					Display.Of(() => obj2.Contents, ws).WhenEmpty("prompt", ws)
					)
				);
			theSharpView.Root = root;
			root.SelectAtStart().Install();
			theSharpView.Focus();
		}

		//// This should eventually become another option in the switch in whichView_SelectedIndexChanged.
		//private void InitHtmlEdit()
		//{
		//    HtmlRootBox root = new HtmlRootBox();
		//    root.Html =
		//         @"<p lang='en'>This is the day that the <span class='NameOfGod'>LORD</span> has made. In German, Lord is <span lang='ge'>Der Herr</span>.";
		//    HtmlStylesheet styles = new HtmlStylesheet();
		//    // Material with class NameOfGod is bold, red
		//    styles.Add("NameOfGod", "color: red; font-weight: bold");
		//    // Default material in german is italic in a different font
		//    styles.Add("", "ge", "italic; font-family: Storybook");
		//    root.Stylesheet = styles;
		//    theSharpView.Root = root;

		//    // Later, some button should be able to show that root.Html has changed, consistent with some editing.

		//    // This might be some of the internal code that initializes HtmlRootBox.
		//    //var styles = new AssembledStyles();
		//    //var root = new RootBoxFdo(styles);
		//    //var items = new List<ClientRun>();
		//    //var source = new TextSource(items);
		//    //var para = new ParaBox(styles, source);
		//}


		private string m_simpleText;
		string SimpleText
		{
			get { return m_simpleText; }
			set
			{
				m_simpleText = value;
				if (SimpleTextChanged != null)
					SimpleTextChanged(this, new EventArgs());
			}
		}

		private event EventHandler SimpleTextChanged;

		private void InitSimpleTextPara()
		{
			AssembledStyles styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			int ws = 1; // arbitrary with default renderer factory.
			AddSimpleTextPara(styles, ws, root);
			theSharpView.Root = root;
			root.SelectAtStart().Install();
			theSharpView.Focus();
		}

		private void InitDoubleString()
		{
			var styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			int ws = 1; // arbitrary with default renderer factory.
			AddSimpleTextPara(styles, ws, root);
			AddSimpleTextPara(styles, ws, root);
			theSharpView.Root = root;
			root.SelectAtStart().Install();
			theSharpView.Focus();
		}

		private void InitMultiPara()
		{
			var styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			var owner = new ParagraphOwnerDemo();
			owner.InsertParagraph(0, new ParagraphDemo());
			int ws = 1; // arbitrary with default renderer factory.
			root.Builder.Show(Display.Of(() => owner.Paragraphs).Using(
				(builder, para) => builder.AddString(() => para.Contents, ws))
				.EditParagraphsUsing(new ParagraphOpsDemo(owner)));
			theSharpView.Root = root;
			root.SelectAtStart().Install();
			theSharpView.Focus();
		}

		private void InitLongText()
		{
			var stylesheet = SetupStyles();
			var styles = new AssembledStyles(stylesheet);
			RootBox root = new RootBoxFdo(styles.WithLineHeight(12500));
			var owner = new ParagraphOwnerDemo();
			var words =
				"This is a bit of text from which we can extract substrings of increasing length to populate various paragraphs in different ways"
					.Split(' ');
			string sb = "";
			int ws = 1; // arbitrary with default renderer factory.
			for (int i = 0; i < 20; i++)
			{
				if (i < words.Length)
					sb += (words[i]);
				if (i < words.Length)
					sb += (" ");
				var para = ApplyTsStringStyle(sb, "Normal", ws);
				owner.InsertParagraph(0, para);
			}

			root.Builder.Show(Div.Containing(Div.Containing(Display.Of(() => owner.Paragraphs).Using(
				(builder, para) => builder.Show(Paragraph.Containing(Display.Of(() => para.TsContents)).Style(para.ParaStyle)))
								.EditParagraphsUsing(new ParagraphOpsDemo(owner))).Border(1500).Pads(3000, 3000, 3000, 3000)).Border(2500).Pads(1000, 1000, 1000, 1000));

			styleChooser.Visible = true;
			theSharpView.Root = root;
			root.SelectAtStart().Install();
			theSharpView.Focus();
		}

		private DemoStyleSheet SetupStyles()
		{
			var stylesheet = new DemoStyleSheet();

			var nonCharStyled = new DemoStyle { DefaultCharacterStyleInfo = new DemoCharStyleInfo(), Name = "No Character Style" };

			var nonParaStyled = new DemoStyle { ParagraphStyleInfo = new DemoParaStyleInfo(), Name = "No Paragraph Style" };

			var normal = new DemoStyle
							{
								DefaultCharacterStyleInfo = new DemoCharStyleInfo { FontSize = new DemoStyleProp<int> { Value = 10000 } },
								Name = "Normal"
							};

			var bold = new DemoStyle
						{
							DefaultCharacterStyleInfo = new DemoCharStyleInfo { Bold = new DemoStyleProp<bool> { Value = true } },
							Name = "Bold"
						};

			var italic = new DemoStyle
							{
								DefaultCharacterStyleInfo = new DemoCharStyleInfo { Italic = new DemoStyleProp<bool> { Value = true } },
								Name = "Italic"
							};

			var boldItalic = new DemoStyle
								{
									DefaultCharacterStyleInfo =
										new DemoCharStyleInfo { Bold = new DemoStyleProp<bool> { Value = true }, Italic = new DemoStyleProp<bool> { Value = true } },
									Name = "Bold Italic"
								};

			var redOnYellow = new DemoStyle
								{
									DefaultCharacterStyleInfo =
										new DemoCharStyleInfo
											{
												FontColor = new DemoStyleProp<Color> {Value = Color.Red},
												BackColor = new DemoStyleProp<Color> {Value = Color.Yellow}
											},
									Name = "Red on Yellow"
								};

			var left = new DemoStyle
						{
							ParagraphStyleInfo =
								new DemoParaStyleInfo { Alignment = new DemoStyleProp<FwTextAlign> { Value = FwTextAlign.ktalLeft } },
							Name = "Left"
						};

			var right = new DemoStyle
							{
								ParagraphStyleInfo =
									new DemoParaStyleInfo
										{
											Alignment = new DemoStyleProp<FwTextAlign> {Value = FwTextAlign.ktalRight}
										},
								Name = "Right"
							};

			var center = new DemoStyle
							{
								ParagraphStyleInfo =
									new DemoParaStyleInfo { Alignment = new DemoStyleProp<FwTextAlign> { Value = FwTextAlign.ktalCenter } },
								Name = "Center"
							};

			var justify = new DemoStyle
							{
								ParagraphStyleInfo =
									new DemoParaStyleInfo { Alignment = new DemoStyleProp<FwTextAlign> { Value = FwTextAlign.ktalJustify } },
								Name = "Justify"
							};

			var lineHeight = new DemoStyle
								{
									ParagraphStyleInfo =
										new DemoParaStyleInfo { LineHeight = new DemoStyleProp<LineHeightInfo> { Value = new LineHeightInfo(20000, false) } },
									Name = "LineHeight"
								};

			var border = new DemoStyle
							{
								ParagraphStyleInfo =
									new DemoParaStyleInfo
										{
											BorderBottom = new DemoStyleProp<int> {Value = 5},
											BorderLeading = new DemoStyleProp<int> {Value = 5},
											BorderTop = new DemoStyleProp<int> {Value = 5},
											BorderTrailing = new DemoStyleProp<int> {Value = 5},
											BorderColor = new DemoStyleProp<Color> {Value = Color.Red}
										},
								Name = "Border"
							};

			var margin = new DemoStyle
							{
								ParagraphStyleInfo =
									new DemoParaStyleInfo
										{
											MarginBottom = new DemoStyleProp<int> {Value = 10},
											MarginLeading = new DemoStyleProp<int> {Value = 10},
											MarginTop = new DemoStyleProp<int> {Value = 10},
											MarginTrailing = new DemoStyleProp<int> {Value = 10},
											BorderBottom = new DemoStyleProp<int> {Value = 1},
											BorderLeading = new DemoStyleProp<int> {Value = 1},
											BorderTop = new DemoStyleProp<int> {Value = 1},
											BorderTrailing = new DemoStyleProp<int> {Value = 1},
											BorderColor = new DemoStyleProp<Color> {Value = Color.Black}
										},
								Name = "Margin"
							};

			var padding = new DemoStyle
							{
								ParagraphStyleInfo =
									new DemoParaStyleInfo
										{
											PadBottom = new DemoStyleProp<int> {Value = 10},
											PadLeading = new DemoStyleProp<int> {Value = 10},
											PadTop = new DemoStyleProp<int> {Value = 10},
											PadTrailing = new DemoStyleProp<int> {Value = 10},
											BorderBottom = new DemoStyleProp<int> {Value = 1},
											BorderLeading = new DemoStyleProp<int> {Value = 1},
											BorderTop = new DemoStyleProp<int> {Value = 1},
											BorderTrailing = new DemoStyleProp<int> {Value = 1},
											BorderColor = new DemoStyleProp<Color> {Value = Color.Black}
										},
								Name = "Padding"
							};

			var firstLineIndent = new DemoStyle
									{
										ParagraphStyleInfo =
											new DemoParaStyleInfo { FirstLineIndent = new DemoStyleProp<int> { Value = -10000 } },
										Name = "FirstLineIndent"
									};

			stylesheet.SetStyle("No Character Style", nonCharStyled);
			stylesheet.SetStyle("No Paragraph Style", nonParaStyled);
			stylesheet.SetStyle("Normal", normal);
			stylesheet.SetStyle("Bold", bold);
			stylesheet.SetStyle("Italic", italic);
			stylesheet.SetStyle("Bold Italic", boldItalic);
			stylesheet.SetStyle("Red on Yellow", redOnYellow);
			stylesheet.SetStyle("Left", left);
			stylesheet.SetStyle("Right", right);
			stylesheet.SetStyle("Center", center);
			stylesheet.SetStyle("Justify", justify);
			stylesheet.SetStyle("Line Height", lineHeight);
			stylesheet.SetStyle("Border", border);
			stylesheet.SetStyle("Margin", margin);
			stylesheet.SetStyle("Padding", padding);
			stylesheet.SetStyle("FirstLineIndent", firstLineIndent);

			styleChooser.Items.Clear();
			styleChooser.Items.Add("No Character Style");
			styleChooser.Items.Add("No Paragraph Style");
			styleChooser.Items.Add("Normal");
			styleChooser.Items.Add("Bold");
			styleChooser.Items.Add("Italic");
			styleChooser.Items.Add("Bold Italic");
			styleChooser.Items.Add("Red on Yellow");
			styleChooser.Items.Add("Left");
			styleChooser.Items.Add("Right");
			styleChooser.Items.Add("Center");
			styleChooser.Items.Add("Justify");
			styleChooser.Items.Add("Line Height");
			styleChooser.Items.Add("Border");
			styleChooser.Items.Add("Margin");
			styleChooser.Items.Add("Padding");
			styleChooser.Items.Add("FirstLineIndent");

			return stylesheet;
		}

		private void InitStyleSheetChooser()
		{
			var stylesheet = SetupStyles();

			int ws = 1; // arbitrary with default renderer factory.
			var styles = new AssembledStyles(stylesheet);
			RootBox root = new RootBoxFdo(styles);
			root.RendererFactory = theSharpView.RendererFactory;

			var obj0 = ApplyTsStringStyle("plain, ", "Normal", ws);
			var obj1 = ApplyTsStringStyle("bold, ", "Bold", ws);
			var obj2 = ApplyTsStringStyle("italic, ", "Italic", ws);
			var obj3 = ApplyTsStringStyle("bold italic, ", "Bold Italic", ws);
			var obj4 = ApplyTsStringStyle("red on yellow", "Red on Yellow", ws);

			root.Builder.Show(Paragraph.Containing(Display.Of(() => obj0.TsContents),
												   Display.Of(() => obj1.TsContents),
												   Display.Of(() => obj2.TsContents),
												   Display.Of(() => obj3.TsContents),
												   Display.Of(() => obj4.TsContents)));
			styleChooser.Visible = true;

			theSharpView.Root = root;
			root.SelectAtStart().Install();
			theSharpView.Focus();
		}

		private static ParagraphDemo ApplyTsStringStyle(string contents, string style, int ws)
		{
			var tsString = TsStrFactoryClass.Create().MakeString(contents, ws);
			var bldr = tsString.GetBldr();
			bldr.SetStrPropValue(0, tsString.Text.Length, (int)FwTextPropType.ktptNamedStyle, style);
			var para = new ParagraphDemo {TsContents = bldr.GetString(), ParaStyle = "Left"};
			return para;
		}

		private void InitStyledText()
		{
			var styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			root.RendererFactory = theSharpView.RendererFactory;
			var obj0 = new ParagraphDemo { Contents = "plain " };
			var obj1 = new ParagraphDemo { Contents = "bold " };
			var obj2 = new ParagraphDemo { Contents = "italic " };
			var obj3 = new ParagraphDemo { Contents = "bold italic " };
			var obj4 = new ParagraphDemo { Contents = "red on yellow" };
			int ws = 1; // arbitrary with default renderer factory.
			root.Builder.Show(
				Paragraph.Containing(
					Display.Of(() => obj0.Contents, ws).FaceName("Times New Roman"),
					Display.Of(() => obj1.Contents, ws).FaceName("Times New Roman").Bold,
					Display.Of(() => obj2.Contents, ws).FaceName("Times New Roman").Italic,
					Display.Of(() => obj3.Contents, ws).FaceName("Times New Roman").Bold.Italic,
					Display.Of(() => obj4.Contents, ws).ForeColor(Color.Red).BackColor(Color.Yellow)
					).Border(1.Points(), Color.Red).Pads(2.Points(), 3.Points(), 2.Points(), 3.Points())
				);
			root.Builder.Show(
				Paragraph.Containing(
					Display.Of("plain"),
					Display.Of("underOnYellow").Underline(FwUnderlineType.kuntSingle).BackColor(Color.Yellow).FaceName("Times New Roman")
					).Margins(3.Points(), 2.Points(), 5.Points(), 2.Points())
					.Borders(1.Points(), 2.Points(), 3.Points(), 4.Points(), Color.Green)
					.BackColor(Color.Pink).Pads(2.Points(), 2.Points(), 2.Points(), 2.Points()),
				Paragraph.Containing(
					Display.Of("doubleRedOnPink").Underline(FwUnderlineType.kuntDouble, Color.Red).BackColor(Color.Pink),
					Display.Of("dotted").Underline(FwUnderlineType.kuntDotted),
					Display.Of("dottedOnYellow").Underline(FwUnderlineType.kuntDotted).BackColor(Color.Yellow)
					),
				Paragraph.Containing(
					Display.Of("dashed").Underline(FwUnderlineType.kuntDashed),
					Display.Of("dashedRed").Underline(FwUnderlineType.kuntDashed).ForeColor(Color.Red),
					Display.Of("squiggle").Underline(FwUnderlineType.kuntSquiggle, Color.Red)
					)
				);
			theSharpView.Root = root;
			root.SelectAtStart().Install();
			theSharpView.Focus();
		}

		// Add to the root a text paragraph which reflects the SimpleText property.
		private void AddSimpleTextPara(AssembledStyles styles, int ws, RootBox root)
		{
			var items = new List<IClientRun>();
			var run = new StringClientRun("This is the day that the Lord has made. We will rejoice and be glad in it",
										  styles.WithWs(ws));
			items.Add(run);
			var source = new TextSource(items);
			var para = new ParaBox(styles, source);
			var hookup = new StringHookup(this, () => this.SimpleText,
										  hook => SimpleTextChanged += hook.StringPropChanged,
										  hook => SimpleTextChanged -= hook.StringPropChanged, para);
			hookup.Writer = newVal => SimpleText = newVal;
			run.Hookup = hookup;
			root.AddBox(para);
		}

		private void InitProportionalRowBoxes()
		{
			var stylesheet = SetupStyles();
			var styles = new AssembledStyles(stylesheet);
			RootBox root = new RootBoxFdo(styles.WithFirstLineIndent(10000));

			var item1 = ApplyTsStringStyle("This begins row one", "Normal", 1);
			var item2 = ApplyTsStringStyle("This is Box 2", "Normal", 1);
			var item3 = ApplyTsStringStyle("This is Box 3", "Normal", 1);
			var item4 = ApplyTsStringStyle("This ends row one", "Normal", 1);

			root.Builder.Show(
				Row.WithWidths(new ProportionalColumnWidths(4, 2, 2, 2)).Containing(Cell.Containing(
					Paragraph.Containing(Display.Of(() => item1.TsContents)).Style(item1.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.Pink).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item2.TsContents)).Style(item2.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.Orange).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item3.TsContents)).Style(item3.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.LightGray).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item4.TsContents)).Style(item4.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.FloralWhite).Border(1000)).Margins(5000, 5000, 5000, 5000).Borders(1000, 1000, 1000, 1000, Color.Black));
			root.Builder.Show(
				Row.WithWidths(new ProportionalColumnWidths(4, 2, 2, 2)).WithWrap.Containing(Cell.Containing(
					Paragraph.Containing(Display.Of(() => item1.TsContents)).Style(item1.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.Pink).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item2.TsContents)).Style(item2.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.Orange).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item3.TsContents)).Style(item3.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.LightGray).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item4.TsContents)).Style(item4.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.FloralWhite).Border(1000)).Margins(5000, 5000, 5000, 5000).Borders(1000, 1000, 1000, 1000, Color.Black));

			styleChooser.Visible = true;
			theSharpView.Root = root;
			theSharpView.Focus();
		}

		private void InitFixedRowBoxes()
		{
			var stylesheet = SetupStyles();
			var styles = new AssembledStyles(stylesheet);
			RootBox root = new RootBoxFdo(styles.WithFirstLineIndent(10000));

			var item1 = ApplyTsStringStyle("This begins row one", "Normal", 1);
			var item2 = ApplyTsStringStyle("This is Box 2", "Normal", 1);
			var item3 = ApplyTsStringStyle("This is Box 3", "Normal", 1);
			var item4 = ApplyTsStringStyle("This ends row one", "Normal", 1);

			root.Builder.Show(
				Row.WithWidths(new FixedColumnWidths(300, 60, 60, 100)).Containing(
					Cell.Containing(Paragraph.Containing(Display.Of(() => item1.TsContents)).Style(item1.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.Pink).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item2.TsContents)).Style(item2.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.Orange).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item3.TsContents)).Style(item3.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.LightGray).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item4.TsContents)).Style(item4.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.FloralWhite).Border(1000)).Margins(5000, 5000, 5000, 5000).Borders(1000, 1000, 1000, 1000, Color.Black));
			root.Builder.Show(
				Row.WithWidths(new FixedColumnWidths(300, 60, 60, 100)).WithWrap.Containing(
					Cell.Containing(Paragraph.Containing(Display.Of(() => item1.TsContents)).Style(item1.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.Pink).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item2.TsContents)).Style(item2.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.Orange).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item3.TsContents)).Style(item3.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.LightGray).Border(1000),
					Cell.Containing(Paragraph.Containing(Display.Of(() => item4.TsContents)).Style(item4.ParaStyle)).Pads(2000, 2000, 2000, 2000).Margins(2000, 2000, 2000, 2000)
						.BackColor(Color.FloralWhite).Border(1000)).Margins(5000, 5000, 5000, 5000).Borders(1000, 1000, 1000, 1000, Color.Black));

			styleChooser.Visible = true;
			theSharpView.Root = root;
			theSharpView.Focus();
		}

		private void InitSeveralBoxes()
		{
			var styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			var items = new List<IClientRun>();
			items.Add(new BlockBox(styles, Color.Red, 72000, 36000));
			items.Add(new BlockBox(styles, Color.Blue, 36000, 18000));
			items.Add(new BlockBox(styles, Color.Orange, 18000, 36000));
			items.Add(new BlockBox(styles, Color.Green, 72000, 18000));
			items.Add(new ImageBox(styles.WithBackColor(Color.Pink).WithBorderColor(Color.Blue)
				.WithBorders(new Thickness(2.0)).WithPads(new Thickness(4.0)), new Icon(SystemIcons.Shield, 40, 40).ToBitmap()));
			items.Add(new BlockBox(styles, Color.Yellow, 72000, 36000));
			var source = new TextSource(items);
			var para = new ParaBox(styles, source);
			root.AddBox(para);
			theSharpView.Root = root;
		}

		private void InitRedBox()
		{
			var styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			var block = new BlockBox(styles, Color.Red, 72000, 36000);
			root.AddBox(block);
			theSharpView.Root = root;
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			theSharpView.OnDelete();
			theSharpView.Focus();
		}

		private void wsSelector1_SelectedIndexChanged(object sender, EventArgs e)
		{
			Ws1 = int.Parse(wsSelector1.SelectedItem as String);
			theSharpView.Focus();
		}

		private void wsSelector2_SelectedIndexChanged(object sender, EventArgs e)
		{
			Ws2 = int.Parse(wsSelector2.SelectedItem as String);
			theSharpView.Focus();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			theSharpView.OnEditCut(sender);
			theSharpView.Focus();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			theSharpView.OnEditCopy(sender);
			theSharpView.Focus();
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			theSharpView.OnEditPaste(sender);
			theSharpView.Focus();
		}

		private void editToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (theSharpView.Root != null)
			{
				deleteToolStripMenuItem.Enabled = theSharpView.Root.CanDelete();

				cutToolStripMenuItem.Enabled = theSharpView.Root.CanCut();

				copyToolStripMenuItem.Enabled = theSharpView.Root.CanCopy();

				pasteToolStripMenuItem.Enabled = theSharpView.Root.CanPaste();
			}
			else
			{
				deleteToolStripMenuItem.Enabled = false;

				cutToolStripMenuItem.Enabled = false;

				copyToolStripMenuItem.Enabled = false;

				pasteToolStripMenuItem.Enabled = false;
			}
			theSharpView.Focus();
		}

		private void styleChooser_SelectedIndexChanged(object sender, EventArgs e)
		{
			var style = styleChooser.SelectedItem;
			if(style != null)
				theSharpView.OnApplyStyle(style.ToString());
			theSharpView.Focus();
		}

		private void showPromptsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			theSharpView.Focus();
		}
	}
}
