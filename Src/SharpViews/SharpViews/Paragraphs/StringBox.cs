using System;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Paragraphs
{
	/// <summary>
	/// This class is used for a run of text with the same writing system on a single line. String boxes are created and destroyed
	/// quite frequently as (for example) the paragraph layout width changes. Accordingly references to string boxes should not be
	/// held. To discourage this, for now StringBox is not a public class.
	///
	/// A StringBox wraps an ILgRenderSegment, which does much of the actual rendering etc.
	/// </summary>
	class StringBox : LeafBox
	{
		/// <summary>
		/// The Segment of text that is the whole point of the StringBox.
		/// </summary>
		public ILgSegment Segment { get; private set; }
		/// <summary>
		/// An offset into the VwTextSource of the paragraph where this segment begins.
		/// </summary>
		public int IchMin { get; internal set; }
		/// <summary>
		/// Make one.
		/// </summary>
		public StringBox(AssembledStyles styles, ILgSegment segment, int ichMin) : base(styles)
		{
			Segment = segment;
			IchMin = ichMin;
		}

		/// <summary>
		/// The length of the string box in rendered characters.
		/// </summary>
		internal int RenderLength
		{
			get
			{
				return Segment.get_Lim(IchMin);
			}
		}

		private int m_ascent;

		public override int Ascent
		{
			get
			{
				return m_ascent;
			}
		}

		public override void Layout(LayoutInfo transform)
		{
			Height = Segment.get_Height(IchMin, transform.VwGraphics);
			Width = Segment.get_Width(IchMin, transform.VwGraphics);
			m_ascent = Segment.get_Ascent(IchMin, transform.VwGraphics);
		}

		public string Text
		{
			get
			{
				if (Segment == null || !(Container is ParaBox) || ((ParaBox)Container).Source == null)
					return "";
				int length = Segment.get_Lim(IchMin);
				string renderText = ((ParaBox) Container).Source.RenderText;
				if (IchMin + length > renderText.Length)
					return length.ToString() + " chars from " + IchMin + " in " + renderText; // broken
				return renderText.Substring(IchMin, length);
			}
		}

		public override string ToString()
		{
			return "a text box (" + Text + ") with width " + Width + " at (" + Left + ", " + Right + ")";
		}

		public override void PaintForeground(IVwGraphics vg, PaintTransform ptrans)
		{
			if (Segment == null)
				return;
			int dxdWidth;
			PaintTransform segTrans = ptrans.PaintTransformOffsetBy(Left, Top);

			if (AnyColoredBackground)
				Segment.DrawTextNoBackground(IchMin, vg, segTrans.SourceRect, segTrans.DestRect, out dxdWidth);
			else
				Segment.DrawText(IchMin, vg, segTrans.SourceRect, segTrans.DestRect, out dxdWidth); // more efficient.
		}

		public override void PaintBackground(IVwGraphics vg, PaintTransform ptrans)
		{
			// Review JohnT: do we want to allow individual strings to paint borders etc? Should we call base?
			// base.PaintBackground(vg, ptrans);
			if (Segment == null)
				return;

			int dxdWidth;
			PaintTransform segTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			// Ideally, we'd just draw the background, but we don't have that capability currently.
			// The current implementation of DrawTextNoBackground does a good job of redrawing
			// the foreground text, even if it's already been painted.
			if (AnyColoredBackground)
				Segment.DrawText(IchMin, vg, segTrans.SourceRect, segTrans.DestRect, out dxdWidth);
			int dichLim = Segment.get_Lim(IchMin);
			int ichLim = IchMin + dichLim;
			int ichMinRun = IchMin;
			int ichLimRun;
			int dydOffset = Math.Max(1, segTrans.DpiY/96); // distance between double underline, also up and down for squiggle.
			for (; ichMinRun < ichLim; ichMinRun = ichLimRun)
			{
				int clrUnder;
				var unt = Paragraph.Source.GetUnderlineInfo(ichMinRun, out clrUnder, out ichLimRun);
				ichLimRun = Math.Min(ichLimRun, ichLim);
				Debug.Assert(ichLimRun > ichMinRun);
				if (unt == FwUnderlineType.kuntNone)
					continue;
				// Get info about where to draw underlines for this run
				//int ydApproxUnderline = rcSrcChild.MapYTo(psbox->Ascent(), rcDst);
				//// GetCharPlacement seems to be the really expensive part of underlining; don't do it
				//// if the underline is nowhere near the clip rectangle. Times 2 and times 3 are both one more multiple
				//// than typically needed.
				//if (ydApproxUnderline - dydOffset * 2 < ydTopClip - 1 || ydApproxUnderline + dydOffset * 3 > ydBottomClip + 1)
				//    continue;
				int[] lefts, rights, tops;
				int cxd;
				Segment.GetCharPlacement(IchMin, vg, ichMinRun,
					ichLimRun, segTrans.SourceRect, segTrans.DestRect, true, 0, out cxd,
					null, null, null);
				using (var rgxdLefts = MarshalEx.ArrayToNative<int>(cxd))
				using (var rgxdRights = MarshalEx.ArrayToNative<int>(cxd))
				using (var rgydTops = MarshalEx.ArrayToNative<int>(cxd))
				{
					Segment.GetCharPlacement(IchMin, vg, ichMinRun,
						ichLimRun, segTrans.SourceRect, segTrans.DestRect, true, cxd, out cxd,
						rgxdLefts, rgxdRights, rgydTops);
					lefts = MarshalEx.NativeToArray<int>(rgxdLefts, cxd);
					rights = MarshalEx.NativeToArray<int>(rgxdRights, cxd);
					tops = MarshalEx.NativeToArray<int>(rgydTops, cxd);
				}
				for (int ixd = 0; ixd < cxd; ixd++)
				{
					// top of underline 1 pixel below baseline
					int ydDrawAt = tops[ixd];
					// underline is drawn at most one offset above ydDrawAt and at most 2 offsets below.
					// Skip the work if it is clipped.
					//if (ydDrawAt - dydOffset < ydBottomClip + 1 && ydDrawAt + dydOffset * 2 > ydTopClip - 1)
					//{
					//int xLeft = max(rgxdLefts[ixd], xdLeftClip - 1);
					//int xRight = min(rgxdRights[ixd], xdRightClip + 1);
					int xLeft = lefts[ixd];
					int xRight = rights[ixd];
					DrawUnderline(vg, xLeft, xRight, ydDrawAt,
						segTrans.DpiX/96, dydOffset,
						clrUnder, unt, segTrans.XOffsetScroll);
					//}
				}
			}
		}

		private bool AnyColoredBackground
		{
			get
			{
				int ich = IchMin;
				int dichLim = Segment.get_Lim(IchMin);
				int ichLim = ich + dichLim;
				while (ich < ichLim)
				{
					LgCharRenderProps chrp;
					int ichMinRun, ichLimRun;
					Paragraph.Source.GetCharProps(ich, out chrp, out ichMinRun, out ichLimRun);
					Debug.Assert(ichLimRun >= ich);
					if ((int)chrp.clrBack != (int)FwTextColor.kclrTransparent)
					{
						return true;
					}
					ich = ichLimRun;
				}
				return false;
			}
		}

		internal void DrawIp(InsertionPoint ip, IVwGraphics vg, PaintTransform ptrans)
		{
			PaintTransform segTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			Segment.DrawInsertionPoint(IchMin, vg, segTrans.SourceRect, segTrans.DestRect,
				ip.RenderParaPosition, ip.AssociatePrevious, true, LgIPDrawMode.kdmNormal);

		}

		internal void DrawRange(int ichMin, int ichLim, IVwGraphics vg, PaintTransform ptrans, int topOfLine, int bottomOfLine)
		{
			PaintTransform segTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			// Todo JohnT: here or in client we need to handle ranges that don't start and end in this paragraph.
			// Todo JohnT: the last true appears to be what the old Views code normally passes, but there may be some case where
			// we should pass false.
			// Todo JohnT: passing the top and bottom of the string box as ydTop and ydBottom will not work when we have specified line spacing.
			Segment.DrawRange(IchMin, vg, segTrans.SourceRect, segTrans.DestRect,
				ichMin, ichLim, topOfLine, bottomOfLine, true, true);
		}
		/// <summary>
		///  Get the IP location, if in this segment; if not return a dummy rectangle and 'here' will be false.
		/// </summary>
		public Rectangle GetIpLocation(InsertionPoint ip, IVwGraphics vg, PaintTransform ptrans, out bool here)
		{
			PaintTransform segTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			Rect rectPrimary, rectSec;
			bool fPrimaryHere, fSecHere;
			Segment.PositionsOfIP(IchMin, vg, segTrans.SourceRect, segTrans.DestRect,
								  ip.RenderParaPosition, ip.AssociatePrevious, LgIPDrawMode.kdmNormal, out rectPrimary,
								  out rectSec,
								  out fPrimaryHere, out fSecHere);
			if (fPrimaryHere)
			{
				here = true;
				return new Rectangle(rectPrimary.left, rectPrimary.top, rectPrimary.right - rectPrimary.left,
									 rectPrimary.bottom - rectPrimary.top);
			}
			here = false;
			return new Rectangle();
		}

		/// <summary>
		/// Our immediate containing paragraph. (The container of a string box is always a paragraph.)
		/// </summary>
		ParaBox Paragraph { get { return (ParaBox) Container; }}

		internal override Selection MakeSelectionAt(Point where, IVwGraphics vg, PaintTransform ptrans)
		{
			var segTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			int ich;
			bool assocPrev;
			Segment.PointToChar(IchMin, vg, segTrans.SourceRect, segTrans.DestRect, where, out ich, out assocPrev);
			return Paragraph.SelectAt(ich, assocPrev);
		}

		/// <summary>
		/// Draw an underline from xdLeft to xdRight at ydTop, given the specified screen resolution,
		/// the desired colur and underline type, and (for aligning squiggles) the offset in the
		/// destination drawing rectangle.
		/// </summary>
		static void DrawUnderline(IVwGraphics pvg, int xdLeft, int xdRight, int ydTop,
			int dxScreenPix, int dyScreenPix, int clrUnder, FwUnderlineType unt, int xOffset)
		{
			int[] rgdx;
			pvg.ForeColor = clrUnder;
			int xStartPattern;
			switch (unt)
			{
				case FwUnderlineType.kuntSquiggle:
					{
						// BLOCK for var decls
						// ENHANCE JohnT: should we do some trick to make it look continuous
						// even if drawn in multiple chunks?
						// Note: going up as well as down from ydTop makes the squiggle
						// actually touch the bottom of typical letters. This is consistent
						// with Word; FrontPage puts the squiggle one pixel clear. If we want
						// the latter effect, just use ydTop + dyScreenPix.
						int dxdSeg = Math.Max(1, dxScreenPix*2);
						int xdStartFromTrueLeft = ((xdLeft - xOffset)/dxdSeg)*dxdSeg; // aligns it to multiple of dxdSeg
						int xdStart = xdStartFromTrueLeft + xOffset; // back in drawing coords
						int dydStart = -dyScreenPix; // toggle for up/down segs
						// Initial value is determined by whether xdStart is an odd or even multiple
						// of dxdSeg.
						if (xdStartFromTrueLeft%(dxdSeg*2) != 0)
							dydStart = -dydStart;
						while (xdStart < xdRight)
						{
							int xdEnd = xdStart + dxdSeg;
							pvg.DrawLine(xdStart, ydTop + dydStart, xdEnd, ydTop - dydStart);
							dydStart = -dydStart;
							xdStart = xdEnd;
						}
					}
					// This uses diagonal lines so don't break and draw a straight one, return
					return;
				case FwUnderlineType.kuntDotted:
					rgdx = new [] {dxScreenPix*2, dxScreenPix*2};
					break;
				case FwUnderlineType.kuntDashed:
					rgdx = new [] { dxScreenPix * 6, dxScreenPix * 3 };
					break;
				case FwUnderlineType.kuntStrikethrough:
					{
						int dydAscent = pvg.FontAscent;
						ydTop = ydTop - dydAscent/3;
						rgdx = new [] {int.MaxValue};
						break;
					}
				case FwUnderlineType.kuntDouble:
					xStartPattern = xdLeft;
					rgdx = new [] { int.MaxValue };
					pvg.DrawHorzLine(xdLeft, xdRight, ydTop + dyScreenPix * 2, dyScreenPix,
						1, rgdx, ref xStartPattern);
					// continue to draw the upper line as well, just like a normal underline.
					break;
				case FwUnderlineType.kuntSingle:
					// For (some) forwards compatibility, treat any unrecognized underline
					// type as single.
				default:
					rgdx = new [] { int.MaxValue };
					break;
			}
			xStartPattern = xdLeft;
			pvg.DrawHorzLine(xdLeft, xdRight, ydTop, dyScreenPix,
				rgdx.Length, rgdx, ref xStartPattern);
		}
	}
}
