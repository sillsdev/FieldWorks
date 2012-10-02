using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews.Paragraphs
{
	/// <summary>
	/// This class represents one line in a paragraph. It is not itself a box, but merely a way of organizing the boxes of the paragraph.
	/// </summary>
	class ParaLine
	{
		internal Box FirstBox { get; private set; }
		internal Box LastBox { get; private set; }
		internal int Top { get; set; }
		internal int Bottom { get { return Top + Height; } }

		internal void Add(Box box)
		{
			if (box == null)
				return;
			if (FirstBox == null)
			{
				FirstBox = box;
				FirstBox.Previous = null;
			}
			else
			{
				LastBox.Next = box;
				box.Previous = LastBox;
			}
			LastBox = box;
			box.Next = null;
		}

		internal void ArrangeBoxes(FwTextAlign align, int gapLeft, int gapRight, int firstLineIndent, int availWidth, int topDepth)
		{
			int position = 0;
			switch (align)
			{
					case FwTextAlign.ktalJustify: // Todo: implement justification; for now, just treat as left aligned.
				case FwTextAlign.ktalLeft:
					position = gapLeft + firstLineIndent;
					break;
				case FwTextAlign.ktalRight:
					// Enhance: adjust by firstLineIndent if direction is RTL.
					position = availWidth - gapRight - WidthOfAllBoxes;
					if (topDepth == 1)
						position -= firstLineIndent;
					break;
				case FwTextAlign.ktalCenter:
					position = gapLeft + firstLineIndent + (availWidth - gapRight - gapLeft - firstLineIndent - WidthOfAllBoxes)/2;
					break;
			}
			Height = 0;
			foreach (var box in OrderedBoxes(topDepth))
			{
				box.Top = Top; // todo: align baselines
				box.Left = position;
				position += box.Width;
				Height = Math.Max(box.Top + box.Height - Top, Height);
			}
			Width = LastBox.Left + LastBox.Width - gapLeft;
		}

		private int WidthOfAllBoxes
		{
			get { return (from box in Boxes select box.Width).Sum(); }
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

		internal void RemoveFrom(Box firstToRemove)
		{
			if (firstToRemove == FirstBox)
			{
				LastBox = FirstBox = null;
				return;
			}
			var prevBox = BoxBefore(firstToRemove);
			LastBox = prevBox;
			prevBox.Next = null;
		}

		internal Box BoxBefore(Box target)
		{
			Box prevBox = null;
			for (var box = FirstBox; box != target; box = box.Next)
				prevBox = box;
			return prevBox;
		}

		/// <summary>
		/// Adjust the direction of any weak-direction boxes to match the shallowest adjacent non-weak-direction box.
		/// The start and end of the paragraph count as having the depth specified by the topDepth argument.
		/// So do any non-string boxes.
		/// </summary>
		internal void SetWeakDirections(int topDepth)
		{
			// Figure out which boxes have weak directionality and set the direction depths of
			// all the boxes.
			var depthVals = new List<int>();
			var weakVals = new List<bool>();
			foreach (var box in Boxes)
			{
				var sbox = box as StringBox;
				if (sbox == null)
				{
					// Treat a non-string as something that is not weak and in the direction of the paragraph.
					depthVals.Add(topDepth);
					weakVals.Add(false);
				}
				else
				{
					int depth;
					weakVals.Add(sbox.Segment.get_DirectionDepth(sbox.IchMin, out depth));
					depthVals.Add(depth);
				}
			}
			int ibox = -1;

			foreach (var box in Boxes)
			{
				ibox++;
				if (!weakVals[ibox])
					continue;

				var sbox = box as StringBox;
				// Set the depth of the weak box to the shallowest of the adjacent boxes.
				int depthPrev = topDepth;
				for (int ibox2 = ibox; --ibox2 >= 0;)
				{
					if (!weakVals[ibox2])
					{
						depthPrev = depthVals[ibox2];
						break;
					}
				}
				int depthNext = topDepth;
				for (int ibox2 = ibox + 1; ibox2 < weakVals.Count; ++ibox2)
				{
					if (!weakVals[ibox2])
					{
						depthNext = depthVals[ibox2];
						break;
					}
				}
				sbox.Segment.SetDirectionDepth(sbox.IchMin, Math.Min(depthPrev, depthNext));
			}
		}

		int GetDirectionDepth(Box box, int topDepth)
		{
			StringBox sbox = box as StringBox;
			if (sbox != null)
			{
				int depth;
				sbox.Segment.get_DirectionDepth(sbox.IchMin, out depth);
				return depth;
			}
			return topDepth; // non-string boxes are always upstream top-level
		}

		/// <summary>
		/// Find in boxes any sequences of boxes whose direction depth is nDepth or more.
		/// Reverse the boxes in each such sequence.
		/// </summary>
		internal void ReverseUpstreamBoxes(int nDepth, List<Box> boxes, int topDepth)
		{
			for (int iboxMin = 0; iboxMin < boxes.Count; ++iboxMin)
			{
				int nDepthBox = GetDirectionDepth(boxes[iboxMin], topDepth);
				if (nDepthBox >= nDepth)
				{
					int iboxLim = iboxMin + 1; // limit of sequence to reverse
					// We got the first box of sequence to reverse!
					// Now find the index after the last to reverse.
					while (iboxLim < boxes.Count)
					{
						if (GetDirectionDepth(boxes[iboxLim], topDepth) < nDepth)
							break;
						iboxLim++;
					}

					// reverse the lists
					for (int ibox = 0; ibox < (iboxLim - iboxMin) / 2; ibox++)
					{
						var pboxTemp = boxes[iboxMin + ibox];
						boxes[iboxMin + ibox] = boxes[iboxLim - 1 - ibox];
						boxes[iboxLim - 1 - ibox] = pboxTemp;
					}

					// Advance start of search to box after this range.
					// Note that iboxLim is already checked, if there are that many.
					// Note that iboxLim is always at least one more than iboxMin, so we
					// do progress.
					iboxMin = iboxLim - 1;
				}
			}
		}

		/// <summary>
		/// Return the boxes in the order they should be laid out across the line.
		/// </summary>
		internal IEnumerable<Box> OrderedBoxes(int topDepth)
		{
			int maxDepth = Boxes.Select(box => GetDirectionDepth(box, topDepth)).Max();
			if (maxDepth == 0)
				return Boxes; // no bidi going on
			SetWeakDirections(topDepth);
			var boxes = new List<Box>(Boxes);
			for (int i = 1; i <= maxDepth; i++)
				ReverseUpstreamBoxes(i, boxes, topDepth);
			return boxes;
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
				for (var box = FirstBox; ; box = box.Next)
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
