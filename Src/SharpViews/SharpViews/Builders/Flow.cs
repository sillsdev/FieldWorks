using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// A base class for Paragraph, Div, Run, and other builder constructs that implement flow objects
	/// in the fluent language.
	/// A typical fluent sequence is Builder.Show(Flow.Containing(Display.Of(...)
	/// which needs to result in adding the appropriate kind of box for the Flow
	/// subclass to the view, and embedding in it the results of passing the "..." to the builder's AddObjSeq
	/// or some similar method.
	/// </summary>
	public abstract class Flow
	{
		private AssembledStyles.PropSetter m_propSetter;
		/// <summary>
		/// Stores a delegate that can create the appropriate kind of box.
		/// May return null (for a run), in which case, stuff built by AddContent is added to the current
		/// box of the builder.
		/// </summary>
		internal Func<AssembledStyles, GroupBox> BoxMaker { get; set; }
		/// <summary>
		/// Add your content to the builder.
		/// </summary>
		/// <param name="builder"></param>
		public abstract void AddContent(ViewBuilder builder);
		/// <summary>
		/// Add to the builder the box that wraps your content.
		/// </summary>
		public GroupBox MakeBox(AssembledStyles baseStyles)
		{
			var derivedStyles = baseStyles.WithProperties(m_propSetter);
			if (BoxMaker == null)
				return new RunBox(derivedStyles);
			return BoxMaker(derivedStyles);
		}

		internal AssembledStyles ChildStyles(AssembledStyles inheritedStyles)
		{
			return inheritedStyles.WithProperties(m_propSetter);
		}

		public void Show(ViewBuilder builder)
		{
			builder.AddGroupBox(MakeBox(builder.NestedBoxStyles), AddContent);
		}

		private void AddSetter(AssembledStyles.PropSetter setter)
		{
			if (m_propSetter == null)
				m_propSetter = setter;
			else
				m_propSetter.AppendProp(setter);
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow will be Bold.
		/// </summary>
		public Flow Bold
		{
			get
			{
				AddSetter(AssembledStyles.FontWeightSetter((int) VwFontWeight.kvfwBold));
				return this;
			}
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow will be Italic.
		/// (Strictly, italic will be inverted: it will be italic iff the context is not.)
		/// </summary>
		public Flow Italic
		{
			get
			{
				AddSetter(AssembledStyles.FontItalicSetter(FwTextToggleVal.kttvInvert));
				return this;
			}
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified font size in
		/// millipoints.
		/// </summary>
		public Flow FontSize(int mp)
		{
			AddSetter(AssembledStyles.FontSizeSetter(mp));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified baseline offset in
		/// millipoints (positive to raise).
		/// </summary>
		public Flow BaselineOffset(int mp)
		{
			AddSetter(AssembledStyles.BaselineOffsetSetter(mp));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified line height in
		/// millipoints (negative works for exact, but consider using ExactLineHeight for that).
		/// </summary>
		public Flow LineHeight(int mp)
		{
			AddSetter(AssembledStyles.LineHeightSetter(mp));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have exactly the specified line height in
		/// millipoints.
		/// </summary>
		public Flow ExactLineHeight(int mp)
		{
			AddSetter(AssembledStyles.LineHeightSetter(-mp));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified font face name.
		/// Use this rarely, as which font is desirable is generally writing-system specific.
		/// </summary>
		public Flow FaceName(string name)
		{
			AddSetter(AssembledStyles.FaceNameSetter(name));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified style.
		/// </summary>
		public Flow Style(string name)
		{
			AddSetter(AssembledStyles.StyleSetter(name));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified foreground color.
		/// </summary>
		public Flow ForeColor(Color color)
		{
			AddSetter(AssembledStyles.ForeColorSetter(color));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified background color.
		/// </summary>
		public Flow BackColor(Color color)
		{
			AddSetter(AssembledStyles.BackColorSetter(color));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified underline type.
		/// </summary>
		public Flow Underline(FwUnderlineType unt)
		{
			AddSetter(AssembledStyles.UnderlineSetter(unt));
			return this;
		}

		/// <summary>
		/// Fluent language: return this, but the contents of the flow have the specified underline type and color.
		/// </summary>
		public Flow Underline(FwUnderlineType unt, Color color)
		{
			AddSetter(AssembledStyles.UnderlineSetter(unt));
			AddSetter(AssembledStyles.UnderlineColorSetter(color));
			return this;
		}

		public Flow Margins(int mpLeading, int mpTop, int mpTrailing, int mpBottom)
		{
			AddSetter(AssembledStyles.MarginsSetter(
				new Thickness(mpLeading/1000.0, mpTop/1000.0, mpTrailing/1000.0, mpBottom/1000.0)));
			return this;
		}
		public Flow Pads(int mpLeading, int mpTop, int mpTrailing, int mpBottom)
		{
			AddSetter(AssembledStyles.PadsSetter(
				new Thickness(mpLeading / 1000.0, mpTop / 1000.0, mpTrailing / 1000.0, mpBottom / 1000.0)));
			return this;
		}
		public Flow Borders(int mpLeading, int mpTop, int mpTrailing, int mpBottom, Color borderColor)
		{
			AddSetter(AssembledStyles.BordersSetter(
				new Thickness(mpLeading / 1000.0, mpTop / 1000.0, mpTrailing / 1000.0, mpBottom / 1000.0)));
			AddSetter(AssembledStyles.BorderColorSetter(borderColor));
			return this;
		}
		public Flow Border(int mpBorder)
		{
			AddSetter(AssembledStyles.BordersSetter(new Thickness(mpBorder/1000.0)));
			return this;
		}
		public Flow Border(int mpBorder, Color borderColor)
		{
			AddSetter(AssembledStyles.BordersSetter(new Thickness(mpBorder / 1000.0)));
			AddSetter(AssembledStyles.BorderColorSetter(borderColor));
			return this;
		}
	}

	/// <summary>
	/// Class representing a flow of objects of type T.
	/// </summary>
	public abstract class Flow<T> : Flow where T: class
	{
		internal IParagraphOperations<T> m_paragraphOps;
		public Flow<T> EditParagraphsUsing(IParagraphOperations<T> paragraphOps)
		{
			m_paragraphOps = paragraphOps;
			return this;
		}

		/// <summary>
		/// Subclasses should call this AFTER adding some content, typically calling some overload
		/// of AddObjSeq.
		/// </summary>
		/// <param name="builder"></param>
		public override void AddContent(ViewBuilder builder)
		{
			HandleParagraphOps(builder);
		}

		/// <summary>
		/// Subclasses which make an object sequence may call this to see whether paragraph ops are available.
		/// </summary>
		/// <param name="builder"></param>
		protected void HandleParagraphOps(ViewBuilder builder)
		{
			if (m_paragraphOps != null)
				builder.EditParagraphsUsing(m_paragraphOps);
		}
	}

	/// <summary>
	/// This specifically stores an expression which should be passed to AddObjSeq on the view builder.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class AddObjSeqFlow<T> : Flow<T> where T: class
	{
		internal Expression<Func<IEnumerable<T>>> FetchItems { get; set; }
		internal ItemBuilder<T> ItemBuilder { get; set; }

		public override void AddContent(ViewBuilder builder)
		{
			builder.AddObjSeq(FetchItems, ItemBuilder.m_displayOneItem);
			base.AddContent(builder);
		}
	}
	/// <summary>
	/// This specifically stores an expression which should be passed to LazyAddObjSeq on the view builder.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class LazyAddObjSeqFlow<T> : Flow<T> where T: class
	{
		internal Expression<Func<IEnumerable<T>>> FetchItems { get; set; }
		internal ItemBuilder<T> ItemBuilder { get; set; }

		public override void AddContent(ViewBuilder builder)
		{
			builder.AddLazyObjSeq(FetchItems, ItemBuilder.m_displayOneItem);
			base.AddContent(builder);
		}
	}

	internal class LiteralFlow : Flow
	{
		string Content { get; set; }
		int Ws { get; set; }
		public LiteralFlow(string content)
		{
			Content = content;
		}
		public LiteralFlow(string content, int ws) : this(content)
		{
			Ws = ws;
		}
		public override void AddContent(ViewBuilder builder)
		{
			builder.AddString(Content, Ws);
		}
	}

	public class StringExpressionFlow : Flow
	{
		private Expression<Func<string>> m_fetchString;
		private int m_ws;
		private string m_substituteString;
		private int m_substituteWs;
		internal StringExpressionFlow(Expression<Func<string>> fetchString, int ws)
		{
			m_fetchString = fetchString;
			m_ws = ws;
		}
		public override void AddContent(ViewBuilder builder)
		{
			if (m_substituteString != null)
				builder.AddString(m_fetchString, m_ws, m_substituteString, m_substituteWs);
			else
				builder.AddString(m_fetchString, m_ws);
		}

		/// <summary>
		/// Fluent language construct causing the specified substitute in the specified WS to be displayed when the string
		/// normally displayed by the StringExpression is empty.
		/// </summary>
		public Flow WhenEmpty(string substitute, int ws)
		{
			m_substituteString = substitute;
			m_substituteWs = ws;
			return this;
		}

		/// <summary>
		/// Fluent language construct causing the specified substitute (in the same WS as the main string)
		/// to be displayed when the string normally displayed by the StringExpression is empty.
		/// </summary>
		public Flow WhenEmpty(string substitute)
		{
			return WhenEmpty(substitute, m_ws);
		}
	}

	/// <summary>
	/// A sequence flow combines the contents of multiple flows.
	/// </summary>
	internal class SequenceFlow : Flow
	{
		private Flow[] Children { get; set;}

		public SequenceFlow(Flow[] children)
		{
			Children = children;
		}

		public override void AddContent(ViewBuilder builder)
		{
			if (Children == null)
				return;
			// We need to Show() each flow, not just Add its content, because each flow may
			// wish to create a box, or at least set styles.
			foreach (var flow in Children)
				flow.Show(builder);
		}
	}

	internal class BlockFlow : Flow
	{
		private Color m_color;
		private int m_mpWidth;
		private int m_mpHeight;
		public BlockFlow(Color color, int mpWidth, int mpHeight)
		{
			m_color = color;
			m_mpWidth = mpWidth;
			m_mpHeight = mpHeight;
		}
		public override void AddContent(ViewBuilder builder)
		{
			builder.AddBlock(m_color, m_mpWidth, m_mpHeight);
		}
	}
}
