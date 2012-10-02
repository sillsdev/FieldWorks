using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Utilities;

namespace SIL.FieldWorks.SharpViews.Selections
{
	public class RangeSelection : TextSelection
	{
		public InsertionPoint Anchor { get; private set; }
		public InsertionPoint DragEnd { get; private set; }
		public bool EndBeforeAnchor { get { return Anchor.AssociatePrevious; } }

		public RangeSelection(InsertionPoint anchor, InsertionPoint drag)
		{
			Anchor = anchor;
			DragEnd = drag;
			var anchorPara = Anchor.Para;
			var dragPara = DragEnd.Para;
			Debug.Assert(anchor != null && DragEnd != null && !anchor.SameLocation(DragEnd));
			bool endFirst;
			if (anchorPara == dragPara)
				endFirst = DragEnd.LogicalParaPosition < Anchor.LogicalParaPosition;
			else
			{
				Box anchorChild, dragChild;
				var commonContainer = anchorPara.CommonContainer(dragPara, out anchorChild, out dragChild);
				if (commonContainer == anchorPara)
				{
					throw new NotImplementedException(
						"selections extending from a paragraph to a descendant paragraph not implemented.");
				}
				if (commonContainer == dragPara)
				{
					throw new NotImplementedException(
						"selections extending from a paragraph to a descendant paragraph not implemented.");
				}
				// otherwise anchorChild and dragChild are different children of CommonContainer, just need their order
				endFirst = anchorChild.Follows(dragChild);
			}
			// Make sure the ends associate inwards.
			DragEnd = DragEnd.Associate(!endFirst);
			Anchor = Anchor.Associate(endFirst);
		}

		public override RootBox RootBox
		{
			get { return Anchor.RootBox; }
		}

		public override bool Contains(InsertionPoint ip)
		{
			if (ip.Para == Start.Para)
			{
				if (ip.LogicalParaPosition < Start.LogicalParaPosition)
					return false;
				if (ip.LogicalParaPosition == Start.LogicalParaPosition)
					return !ip.AssociatePrevious || ip.Para.Source.Length == 0;
			}
			if (ip.Para == End.Para)
			{
				if (ip.LogicalParaPosition > End.LogicalParaPosition)
					return false;
				if (ip.LogicalParaPosition == End.LogicalParaPosition)
					return ip.AssociatePrevious || ip.Para.Source.Length == 0;
			}
			// We now know it is neither before the start in the same paragraph, nor after the end in the
			// same paragraph. If it is in the same paragraph at all, it must be included.
			if (ip.Para == Start.Para || ip.Para == End.Para)
				return true;
			// does it belong to some intermediate paragraph?
			for (Box box = Start.Para; box != End.Para; box = box.NextInSelectionSequence(true) )
			{
				if (box == ip.Para)
					return true;
			}
			return false;
		}

		public override object DragDropData
		{
			get
			{
				if (Start.Para == End.Para)
				{
					var dataObj = new DataObject(DataFormats.StringFormat, Start.Para.Source.GetRenderText(Start.RenderParaPosition,
						End.LastRenderParaPosition - Start.RenderParaPosition));
					dataObj.SetData(DataFormats.Rtf, GetRtfData());
						//@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0 Times New Roman;}{\f1 Arial;}}{\colortbl;\red255\green0\blue0;\red0\green255\blue255;\red0\green255\blue0;}{\stylesheet{\*\cs1\f0\cf1\b\fs40 JohnStyle;}{\*\cs2\f1\cf2\highlight3\i\fs30 SharonStyle;}}\uc1\pard\sa200\sl276\slmult1\f0\fs22This is {\cs1\f0\cf1\b\fs40 John text} and this is {\cs2\f1\cf2\highlight3\i\fs30 Sharon text}"
					// Enhance: should return a third format, something like a serialized TsString, which another
					// SharpView can accept without loss of information.
					// Possibly also a fourth format suitable for old-style Viewws.
					return dataObj;
				}
				return null; // TODO!
			}
		}

		private string GetRtfData()
		{
			// Find all the (stylename, ws) pairs that exist in the data.
			var firstBox = Start.Para;
			var firstRunIndex = firstBox.Source.RunContaining(Start.RenderParaPosition);
			var firstRun = firstBox.Source.Runs[firstRunIndex];
			var firstStyle = firstRun.Styles;
			var lastBox = End.Para;
			if (lastBox == firstBox)
			{

				// Make an RtfStyle for each, except that where the style has no overrides
				// for the ws, we can use (and possibly reuse for multiple WSs) the base
				// RtfStyle with ws=0.
				// Collect all the font names used by the styles (and perhaps eventually
				// any font names specified literally in the selected text) and make a font
				// table.
				// Collect all the colors used by the styles (and eventually any color
				// specified explicitly) and make a color table.
				// Emit a standard prefix that begins the data)
				var fonts = Fonts;
				var colors = Colors;
				var styles = Styles(fonts, colors);
				return RtfPrefix
					   // Emit an appropriate represenation of the font, color, and style tables.
					   + RtfFontTable(fonts)
					   + RtfColorTable(colors)
					   + RtfStyleTable(styles)
					   // Insert a standard prefix before the actual data
					   + RtfDataPrefix
					   // Emit the data.
					   + RtfData(fonts, colors, styles)
					   // Emit the final closing brace to terminate the document.
					   + "}";
			}
			else
			{
				return ""; // Todo: handle multiple paragraph selections.
			}

		}

		private string RtfData(Dictionary<string, int> fonts, Dictionary<Color, int> colors, Dictionary<Tuple<string, int>, StyleData> styles)
		{
			var builder = new StringBuilder();
			var firstBox = Start.Para;
			var firstRunIndex = firstBox.Source.RunContaining(Start.RenderParaPosition);
			var firstRun = firstBox.Source.Runs[firstRunIndex];
			var lastBox = End.Para;
			int endPosition = End.RenderParaPosition;
			if (endPosition > 0)
				endPosition--; // Refers to last character actually IN the selection.
			var lastRunIndex = lastBox.Source.RunContaining(endPosition);
			var lastRun = lastBox.Source.Runs[lastRunIndex];

			RunsDo(aRun =>
				{
					builder.Append("{");
					var style = aRun.Styles;
					var styleName = style.StyleName;
					if (!string.IsNullOrEmpty(styleName))
					{
						var key = new Tuple<string, int>(styleName, style.Ws);
						builder.Append(styles[key].DataFormatting);
						builder.Append(" ");
					}
					var renderText = aRun.RenderText;
					if (aRun == lastRun)
					{
						// truncate the bit beyond the selection
						renderText = renderText.Substring(0, aRun.RenderLength - (aRun.RenderLim - End.RenderParaPosition));
					}
					if (aRun == firstRun)
					{
						// truncate the bit before the selection
						renderText = renderText.Substring(Start.RenderParaPosition - aRun.RenderStart);
					}
					builder.Append(RtfStyle.ConvertString(renderText));
					builder.Append("}");
				});
			return builder.ToString();
		}

		private string RtfStyleTable(Dictionary<Tuple<string, int>, StyleData> styles)
		{
			var builder = new StringBuilder(@"{\stylesheet{");
			foreach (var kvp in styles.OrderBy(pair => pair.Value.Id))
				builder.Append(kvp.Value.StyleTableFormatting).Append(";");
			builder.Append("}");
			return builder.ToString();

		}

		private string RtfColorTable(Dictionary<Color, int> colors)
		{
			var builder = new StringBuilder(@"{\colortbl;");
			foreach (var kvp in colors.OrderBy(pair => pair.Value))
				builder.AppendFormat(@"\red{0}\green{1}\blue{2};", kvp.Key.R, kvp.Key.G, kvp.Key.B);
			builder.Append("}");
			return builder.ToString();
		}

		/// <summary>
		/// Given a dictionary such as Fonts returns, produce the fonts table needed for RTF.
		/// </summary>
		/// <param name="fonts"></param>
		/// <returns></returns>
		string RtfFontTable(Dictionary<string, int> fonts)
		{
			var builder = new StringBuilder(@"{\fonttbl");
			foreach (var kvp in fonts.OrderBy(pair => pair.Value))
				builder.AppendFormat(@"{{\f{0} {1};}}", kvp.Value, kvp.Key);
			builder.Append("}");
			return builder.ToString();
		}

		/// <summary>
		/// Get a dictionary which maps each font used in the selection to a number.
		/// </summary>
		Dictionary<string, int> Fonts
		{
			get
			{
				var result = new Dictionary<string, int>();
				int id = 0; // start numbering fonts in font table from 0.
				RunsDo(aRun => GetFontsForAssembledStyle(aRun.Styles, result, ref id));
				return result;
			}
		}

		// Return a dictionary mapping color to an ID that will be used for that color in a color table
		// (which we build from the result)
		Dictionary<Color, int> Colors
		{
			get
			{
				var result = new Dictionary<Color, int>(new ColorComparer());
				int id = 1; // start numbering colors in color table from 1; 0 is the initial (empty) color.
				RunsDo(aRun =>
					{
						AddColor(aRun.Styles.ForeColor, ref id, result);
						AddColor(aRun.Styles.BackColor, ref id, result);
						AddColor(aRun.Styles.UnderlineColor, ref id, result);
					});
				return result;
			}
		}

		class ColorComparer : IEqualityComparer<Color>
		{
			public bool Equals(Color x, Color y)
			{
				return x.ToArgb() == y.ToArgb();
			}

			public int GetHashCode(Color obj)
			{
				return obj.ToArgb();
			}
		}

		private void AddColor(Color color, ref int id, Dictionary<Color, int> colors)
		{
			int colorId;
			if (colors.TryGetValue(color, out colorId))
				return;
			colors[color] = id++;
		}

		class StyleData
		{
			// 1. The integer ID we will use for this style
			public int Id;
			// 2. The name we will use to describe this style; this is its style name plus
			// the ID of the writing system if it has overrides for the WS.
			public string Name;
			// 3. The string that represents it in the style table (formatting plus the name plus possibly next/based on)
			public string StyleTableFormatting;
			// 4. The string that represents it in the data (formatting plus possibly \highlightN)
			public string DataFormatting;
		}

		/// <summary>
		/// Return a dictionary keyed by (style name, ws) and returning a tuple containing
		/// </summary>
		Dictionary<Tuple<string, int>, StyleData> Styles(Dictionary<string, int> fonts, Dictionary<Color, int> colors)
		{
				var result = new Dictionary<Tuple<string, int>, StyleData>();
				int id = 1; // start numbering styles in style table from 1; 0 seems to be reserved.
				RunsDo(aRun => AddStyleData(aRun.Styles, ref id, fonts, colors, result));
				return result;
		}

		void AddStyleData(AssembledStyles styles, ref int id, Dictionary<string, int> fonts,
			Dictionary<Color, int> colors, Dictionary<Tuple<string, int>, StyleData> styleTable)
		{
			var ss = styles.Stylesheet;
			if (ss == null)
				return; // Can't do anything useful with styles.
			var styleName = styles.StyleName;
			if (string.IsNullOrEmpty(styleName))
				return; // no style info.
			var ws = styles.Ws;
			var key = new Tuple<string, int>(styleName, ws);
			if (styleTable.ContainsKey(key))
				return; // already know all we need.
			var data = new StyleData() { Name = styleName, Id = id++ };
			styleTable[key] = data;
			var style = ss.Style(styleName);
			if (style == null)
			{
				data.StyleTableFormatting = ""; // Todo: some minimal definition with at least the name
				data.DataFormatting = "";
			}
			// Todo: if style has any overrides for ws, make a distinct style name.
			var rtfStyle = new RtfStyle(style, ws) {StyleNumber = data.Id, Fonts = fonts, Colors = colors};
			data.StyleTableFormatting = rtfStyle.ToString(styleName, true);
			data.DataFormatting = rtfStyle.ToString(styleName, false);
		}

		private void RunsDo(Action<MapRun> what)
		{
			var firstBox = Start.Para;
			var firstRunIndex = firstBox.Source.RunContaining(Start.RenderParaPosition);
			var firstRun = firstBox.Source.Runs[firstRunIndex];
			var firstStyle = firstRun.Styles;
			var lastBox = End.Para;
			int endPosition = End.RenderParaPosition;
			if (endPosition > 0)
				endPosition--; // Refers to last character actually IN the selection.
			var lastRunIndex = lastBox.Source.RunContaining(endPosition);
			// Todo: loop over all the runs.
			what(firstRun);
			if (lastBox == firstBox && lastRunIndex == firstRunIndex)
				return; // There's only the one run.
			var lastRun = lastBox.Source.Runs[lastRunIndex];
			var box = firstBox;
			for (var index = firstRunIndex + 1; ; index++)
			{
				if (index >= firstBox.Source.Runs.Length)
				{
					break; // todo: advance to next paragraph box
				}
				var run = box.Source.Runs[index];
				what(run);
				if (run == lastRun)
					break;
			}
		}

		void GetFontsForAssembledStyle(AssembledStyles styles, Dictionary<string, int> fonts, ref int nextId)
		{
			int ws = styles.Ws;
			string fontName = styles.FaceName;
			if (ws != 0)
			{
				var chrp = styles.Chrp;
				var wsFactory = Anchor.Para.Source.GetWsFactory();
				if (wsFactory != null) // paranoia
				{
					var writingSystem = wsFactory.get_EngineOrNull(ws);
					if (writingSystem != null)
					{
						writingSystem.InterpretChrp(ref chrp);
						fontName = AssembledStyles.FaceNameFromChrp(chrp);
					}
				}
			}
			int id;
			if (fonts.TryGetValue(fontName, out id))
				return;
			fonts[fontName] = nextId++;
		}

		/// <summary>
		/// This is the prefix that is at the very start of every RTF document we output (at least for DragDrop)
		/// \rtf1 says this is an RTF version 1 document. (I think all RTF is 'version 1', later additions
		/// are backwards compatible. We're using about 1.5 actually, I think.)
		/// \ansi\ansicpg1252 says default characters are from that code page. Other characters are
		/// represented with Unicode escapes.
		/// \deff0 says to use the first font in the font table by default.
		/// \deflang1033 says the default languge is English. Unfortunately only a few languages can
		/// be specified in RTF; I don't think we're even fully using that capability yet.
		/// The final opening brace must be matched by a closing one at the end of the whole document.
		/// </summary>
		internal const string RtfPrefix = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{";

		/// <summary>
		/// This is the prefix which is inserted after the style table before the actual data.
		/// uc0 says we don't emit any placeholder for the benefit of RTF processors so old they
		/// don't understand the unicode escapes.
		/// I'm not sure about all the others...must have copied from somewhere.
		/// \f0 says to use the first font in the table (unless a run specifies otherwise)
		/// \fs22 establishes 11 points as the default size.
		/// </summary>
		internal const string RtfDataPrefix = @"\uc0\pard\sa200\sl276\slmult1\f0\fs22";

		/// <summary>
		/// Paint the range seleection.
		/// Todo JohnT: handle multi-paragraph selections.
		/// </summary>
		internal override void Draw(IVwGraphics vg, PaintTransform ptrans)
		{
			var stopAt = End.Para;
			for (Box box = Start.Para; box != null; box = box.NextInSelectionSequence(true))
			{
				var para = box as ParaBox;
				if (para == null)
					continue;
				para.DrawRange(this, vg, para.Container.ChildTransformFromRootTransform(ptrans));
				if (para == stopAt)
					break; // after we've drawn it in the last paragraph
			}
		}

		Rectangle Sum(Rectangle first, Rectangle second)
		{
			if (second.Width == 0 || second.Height == 0)
				return first;
			if (first.Width == 0 || first.Height == 0)
				return second;
			int left = Math.Min(first.Left, second.Left);
			int top = Math.Min(first.Top, second.Top);
			int right = Math.Max(first.Right, second.Right);
			int bottom = Math.Max(first.Bottom, second.Bottom);
			return new Rectangle(left, top, right - left, bottom - top);
		}

		public override Rectangle GetSelectionLocation(IVwGraphics vg, PaintTransform ptrans)
		{
			var rectStart = Start.Para.GetRangeLocation(this, vg, Start.Para.Container.ChildTransformFromRootTransform(ptrans));
			if (Start.Para == End.Para)
				return rectStart;
			var rectEnd = End.Para.GetRangeLocation(this, vg, End.Para.Container.ChildTransformFromRootTransform(ptrans));
			var result = Sum(rectStart, rectEnd);
			// Boxes in between are just added in their entirety.

			var stopAt = End.Para;
			bool first = true;
			for (Box box = Start.Para.Next; box != null && box != End.Para; box = box.NextInSelectionSequence(true))
			{
				// We don't want to automatically include the whole of a containing box if the selection ends somewhere inside it.
				// For example, the selection may extend into a following division.
				if (box.Contains(stopAt))
					continue;
				var boxTrans = box.Container.ChildTransformFromRootTransform(ptrans);
				result = Sum(result, ptrans.ToPaint(box.Bounds));
			}
			return result;
		}

		public override Selection MoveByKey(KeyEventArgs args)
		{
			if (args.Shift)
			{
				var newEnd = DragEnd.MoveByKey(new KeyEventArgs(args.KeyCode & ~Keys.ShiftKey)) as InsertionPoint;
				if (newEnd == null)
					return null;
				if (Anchor.SameLocation(newEnd))
					return Anchor;
				return new RangeSelection(Anchor, newEnd);
			}
			switch (args.KeyCode)
			{
				case Keys.Left:
					return Start;
				case Keys.Right:
					return End;
			}
			return base.MoveByKey(args);
		}

		/// <summary>
		/// The end of the selection which comes first in the document.
		/// </summary>
		public InsertionPoint Start
		{
			get
			{
				if (EndBeforeAnchor)
					return DragEnd;
				return Anchor;
			}
		}
		/// <summary>
		/// The end of the selection which comes lastt in the document.
		/// </summary>
		public InsertionPoint End
		{
			get
			{
				if (EndBeforeAnchor)
					return Anchor;
				return DragEnd;
			}
		}

		public override bool CanDelete()
		{
			if (Anchor.Hookup != DragEnd.Hookup)
				return false;  // Enhance JohnT: eventually we may handle some more cases.
			return Anchor.Hookup.CanDelete(Start, End);
		}

		public override void Delete()
		{
			if (!CanDelete())
				return;
			Invalidate(); // while all old state still applies figure out old thing to invalidate.
			// Enhance JohnT: eventually need to handle cases where the end-points are not in the
			// same hookup (e.g., paragraph level editing).
			Anchor.Hookup.Delete(Start, End);
			RootBox.Selection = Start;
		}
	}
}
