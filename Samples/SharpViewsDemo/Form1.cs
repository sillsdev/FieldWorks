// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Selections;

namespace SharpViewsDemo
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void whichView_SelectedIndexChanged(object sender, EventArgs e)
		{
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
			}
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
		//    //var root = new RootBox(styles);
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
			RootBox root = new RootBox(styles);
			int ws = 1; // arbitrary with default renderer factory.
			AddSimpleTextPara(styles, ws, root);
			theSharpView.Root = root;
			root.SelectAtEnd().Install();
			theSharpView.Focus();
		}

		private void InitDoubleString()
		{
			AssembledStyles styles = new AssembledStyles();
			RootBox root = new RootBox(styles);
			int ws = 1; // arbitrary with default renderer factory.
			AddSimpleTextPara(styles, ws, root);
			AddSimpleTextPara(styles, ws, root);
			theSharpView.Root = root;
			root.SelectAtEnd().Install();
			theSharpView.Focus();
		}

		private void InitMultiPara()
		{
			AssembledStyles styles = new AssembledStyles();
			RootBox root = new RootBox(styles);
			var owner = new ParagraphOwnerDemo();
			owner.InsertParagraph(0, new ParagraphDemo());
			int ws = 1; // arbitrary with default renderer factory.
			root.Builder.Show(Display.Of(() => owner.Paragraphs).Using(
				(builder, para) => builder.AddString(()=>para.Contents, ws))
				.EditParagraphsUsing(new ParagraphOpsDemo(owner)));
			theSharpView.Root = root;
			root.SelectAtEnd().Install();
			theSharpView.Focus();
		}

		private void InitLongText()
		{
			AssembledStyles styles = new AssembledStyles();
			RootBox root = new RootBox(styles);
			var owner = new ParagraphOwnerDemo();
			var words =
				"This is a bit of text from which we can extract substrings of increasing length to populate various paragraphs in different ways".Split(' ');
			var sb = new StringBuilder();
			for (int i = 0; i < 20; i++)
			{
				var para = new ParagraphDemo();
				if (i < words.Length)
					sb.Append(words[i]);
				para.Contents = sb.ToString();
				if (i < words.Length)
					sb.Append(" ");
				owner.InsertParagraph(0, para);
			}
			int ws = 1; // arbitrary with default renderer factory.
			root.Builder.Show(Display.Of(() => owner.Paragraphs).Using(
				(builder, para) => builder.AddString(() => para.Contents, ws))
				.EditParagraphsUsing(new ParagraphOpsDemo(owner)));
			theSharpView.Root = root;
			root.SelectAtEnd().Install();
			theSharpView.Focus();

		}

		private void InitStyledText()
		{
			AssembledStyles styles = new AssembledStyles();
			RootBox root = new RootBox(styles);
			root.RendererFactory = theSharpView.RendererFactory;
			var obj0 = new ParagraphDemo() { Contents = "plain " };
			var obj1 = new ParagraphDemo() { Contents = "bold " };
			var obj2 = new ParagraphDemo() { Contents = "italic " };
			var obj3 = new ParagraphDemo() { Contents = "bold italic " };
			var obj4 = new ParagraphDemo() { Contents = "red on yellow" };
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
					.Borders(1.Points(), 2.Points(), 3.Points(),4.Points(), Color.Green)
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
			root.SelectAtEnd().Install();
			theSharpView.Focus();

		}

		// Add to the root a text paragraph which reflects the SimpleText property.
		private void AddSimpleTextPara(AssembledStyles styles, int ws, RootBox root)
		{
			var items = new List<ClientRun>();
			var run = new StringClientRun("This is the day that the Lord has made. We will rejoice and be glad in it",
										  styles.WithWs(ws));
			items.Add(run);
			TextSource source = new TextSource(items);
			ParaBox para = new ParaBox(styles, source);
			var hookup = new StringHookup(this, () => this.SimpleText,
										  hook => SimpleTextChanged += hook.StringPropChanged,
										  hook => SimpleTextChanged -= hook.StringPropChanged, para);
			hookup.Writer = newVal => SimpleText = newVal;
			run.Hookup = hookup;
			root.AddBox(para);
		}

		private void InitSeveralBoxes()
		{
			AssembledStyles styles = new AssembledStyles();
			RootBox root = new RootBox(styles);
			var items = new List<ClientRun>();
			items.Add(new BlockBox(styles, Color.Red, 72000, 36000));
			items.Add(new BlockBox(styles, Color.Blue, 36000, 18000));
			items.Add(new BlockBox(styles, Color.Orange, 18000, 36000));
			items.Add(new BlockBox(styles, Color.Green, 72000, 18000));
			items.Add(new BlockBox(styles, Color.Yellow, 72000, 36000));
			TextSource source = new TextSource(items);
			ParaBox para = new ParaBox(styles, source);
			root.AddBox(para);
			theSharpView.Root = root;
		}

		private void InitRedBox()
		{
			AssembledStyles styles = new AssembledStyles();
			RootBox root = new RootBox(styles);
			BlockBox block = new BlockBox(styles, Color.Red, 72000, 36000);
			root.AddBox(block);
			theSharpView.Root = root;
		}
	}
}
