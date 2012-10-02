using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Paragraphs
{
	/// <summary>
	/// This class represents one line in a paragraph. It is not itself a box, but merely a way of organizing the boxes of the paragraph.
	/// </summary>
	class ParaLine
	{
		internal Box FirstBox { get; set; }
		internal Box LastBox { get; set; }
		internal int Top { get; set; }
		internal int Bottom {get { return Top + Height;}}

		internal void Add(Box box)
		{
			if (FirstBox == null)
				FirstBox = box;
			else
				LastBox.Next = box;
			LastBox = box;
			box.Next = null;
		}

		// Enhance JohnT: handle out-of-order and RTL, boxes.
		internal void ArrangeBoxes(int gapLeft)
		{
			int position = gapLeft;
			Height = 0;
			for (Box box = FirstBox; box != null; box = box.Next)
			{
				box.Top = Top; // todo: align baselines
				box.Left = position;
				position += box.Width;
				Height = Math.Max(box.Top + box.Height - this.Top, Height);
			}
			Width = LastBox.Left + LastBox.Width - gapLeft;
		}

		internal int Height { get; private set; }

		internal IEnumerable<Box> Boxes
		{
			get
			{
				for (Box box = FirstBox; box != null; box = box.Next)
				{
					yield return box;
					if (box == LastBox)
						break;
				}
			}
		}

		internal int Ascent
		{
			get
			{
				// After we've been laid out, we could compute this from the first box's ascent and top
				// and our own top; but this is often used before we've been laid out (in fact as part of the process).
				// Optimize: would it be worth caching in a local variable?
				return Boxes.Max(box => box.Ascent);
			}
		}

		// Get the width of the line (not counting paragraph surroundWidth).
		internal int Width { get; private set; }

		// The character offset in rendered characters of the first box in the line.
		internal int IchMin
		{
			get
			{
				int nonStringBoxes = 0;
				for (Box box = FirstBox; box != null; box = box.Next)
				{
					if (box is StringBox)
						return (box as StringBox).IchMin + nonStringBoxes;
					nonStringBoxes++;
				}
				return (FirstBox.Container as ParaBox).Source.Length - nonStringBoxes;
			}
		}
		// The length in rendered characters of the line.
		internal int Length
		{
			get
			{
				int result = 0;
				for(var box = FirstBox; ; box = box.Next)
				{
					var stringBox = box as StringBox;
					if (stringBox == null)
						result++;
					else
						result += stringBox.RenderLength;
					if (box == LastBox)
						break;
				}
				return result;
			}
		}

		private ParaBox Para
		{
			get { return FirstBox.Container as ParaBox; }
		}

		internal string Text
		{
			get { return Para.Source.GetRenderText(IchMin, Length); }
		}

		/// <summary>
		/// Like Text, but truncated if the source is now too short.
		/// </summary>
		internal string CheckedText
		{
			get
			{
				int len = Para.Source.Length;
				if (IchMin >= len)
					return "";
				return Para.Source.GetRenderText(IchMin, Math.Min(Length, len - IchMin));
			}
		}
	}
}
