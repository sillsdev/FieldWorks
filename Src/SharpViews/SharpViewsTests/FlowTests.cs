using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Paragraphs;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class FlowTests
	{
		[Test]
		public void FormattingFlows()
		{
			//var parentBox = new ParaBox(new AssembledStyles());
			var flow = new AddObjSeqFlow<MockData1>();
			var baseStyles = new AssembledStyles();
			Assert.That(flow.ChildStyles(baseStyles), Is.EqualTo(baseStyles));
			var dummy = flow.Bold;
			Assert.That(flow.ChildStyles(baseStyles).FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.Italic;
			Assert.That(flow.ChildStyles(baseStyles).FontItalic, Is.True);

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.ForeColor(Color.Red);
			Assert.That(flow.ChildStyles(baseStyles).ForeColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.BackColor(Color.Yellow);
			Assert.That(flow.ChildStyles(baseStyles).BackColor.ToArgb(), Is.EqualTo(Color.Yellow.ToArgb()));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.Underline(FwUnderlineType.kuntDashed);
			Assert.That(flow.ChildStyles(baseStyles).Underline, Is.EqualTo(FwUnderlineType.kuntDashed));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.Underline(FwUnderlineType.kuntDotted, Color.Green);
			Assert.That(flow.ChildStyles(baseStyles).UnderlineColor.ToArgb(), Is.EqualTo(Color.Green.ToArgb()));
			Assert.That(flow.ChildStyles(baseStyles).Underline, Is.EqualTo(FwUnderlineType.kuntDotted));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.FontSize(14000);
			Assert.That(flow.ChildStyles(baseStyles).FontSize, Is.EqualTo(14000));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.FontSize(13.Points());
			Assert.That(flow.ChildStyles(baseStyles).FontSize, Is.EqualTo(13000));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.BaselineOffset(2.1.Points());
			Assert.That(flow.ChildStyles(baseStyles).BaselineOffset, Is.EqualTo(2100));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.LineHeight(10.Points());
			Assert.That(flow.ChildStyles(baseStyles).LineHeight, Is.EqualTo(10000));
			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.ExactLineHeight(12.Points());
			Assert.That(flow.ChildStyles(baseStyles).LineHeight, Is.EqualTo(-12000));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.FaceName("Arial");
			Assert.That(flow.ChildStyles(baseStyles).FaceName, Is.EqualTo("Arial"));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.Margins(2.Points(), 3.Points(), 4.Points(), 5.Points());
			Assert.That(flow.ChildStyles(baseStyles).Margins.Leading, Is.EqualTo(2.0));
			Assert.That(flow.ChildStyles(baseStyles).Margins.Top, Is.EqualTo(3.0));
			Assert.That(flow.ChildStyles(baseStyles).Margins.Trailing, Is.EqualTo(4.0));
			Assert.That(flow.ChildStyles(baseStyles).Margins.Bottom, Is.EqualTo(5.0));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.Pads(2.Points(), 3.Points(), 4.Points(), 5.Points());
			Assert.That(flow.ChildStyles(baseStyles).Pads.Leading, Is.EqualTo(2.0));
			Assert.That(flow.ChildStyles(baseStyles).Pads.Top, Is.EqualTo(3.0));
			Assert.That(flow.ChildStyles(baseStyles).Pads.Trailing, Is.EqualTo(4.0));
			Assert.That(flow.ChildStyles(baseStyles).Pads.Bottom, Is.EqualTo(5.0));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.Borders(2.Points(), 3.Points(), 4.Points(), 5.Points(), Color.Red);
			Assert.That(flow.ChildStyles(baseStyles).Borders.Leading, Is.EqualTo(2.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Top, Is.EqualTo(3.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Trailing, Is.EqualTo(4.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Bottom, Is.EqualTo(5.0));
			Assert.That(flow.ChildStyles(baseStyles).BorderColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.Border(2.Points());
			Assert.That(flow.ChildStyles(baseStyles).Borders.Leading, Is.EqualTo(2.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Top, Is.EqualTo(2.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Trailing, Is.EqualTo(2.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Bottom, Is.EqualTo(2.0));

			flow = new AddObjSeqFlow<MockData1>();
			dummy = flow.Border(3.Points(), Color.Yellow);
			Assert.That(flow.ChildStyles(baseStyles).Borders.Leading, Is.EqualTo(3.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Top, Is.EqualTo(3.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Trailing, Is.EqualTo(3.0));
			Assert.That(flow.ChildStyles(baseStyles).Borders.Bottom, Is.EqualTo(3.0));
			Assert.That(flow.ChildStyles(baseStyles).BorderColor.ToArgb(), Is.EqualTo(Color.Yellow.ToArgb()));
		}

		[Test]
		public void UsingFlowFormatting()
		{
			Flow flow = new AddObjSeqFlow<MockData1>();
			var baseStyles = new AssembledStyles();
			var dummy = flow.Bold;
			var runBox = flow.MakeBox(baseStyles);
			Assert.That(runBox, Is.TypeOf(typeof(RunBox)));
			Assert.That(runBox.Style.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));
			// This is more like how it would really be used, except using Display.Of to make
			// the inner Flow, and wrapped in builder.Display.
			flow = Paragraph.Containing(new AddObjSeqFlow<MockData1>()).Bold.Italic.ForeColor(Color.Red);
			var paraBox = flow.MakeBox(baseStyles);
			Assert.That(paraBox, Is.TypeOf(typeof(ParaBox)));
			Assert.That(paraBox.Style.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));
			Assert.That(paraBox.Style.FontItalic, Is.True);
			Assert.That(paraBox.Style.ForeColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
		}
	}

}
