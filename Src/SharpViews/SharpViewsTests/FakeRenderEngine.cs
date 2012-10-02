using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Fake render engine allows a variety of tests based on assuming certain characters will behave in certain ways.
	/// CharWidth specifies how wide a character will be considered to be. Currently only space counts as a break opportunity.
	/// </summary>
	class FakeRenderEngine : IRenderEngine
	{
		public FakeRenderEngine()
		{
			SegmentHeight = 10; // a default
			SegmentAscent = 7; // default
		}
		public void InitRenderer(IVwGraphics vg, string bstrData)
		{

		}

		public int Ws { get; set; }

		public int SegmentHeight { get; set; }

		public int SegmentAscent { get; set; }

		public void FontIsValid()
		{
		}

		public int SegDatMaxLength
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Emulate what a real renderer will do, assuming character widths are as determined by CharWidth,
		/// and only space is a valid line break.
		/// todo: needs enhancing for various more elaborate options, particularly bidi, hard line breaks,
		/// WS changes,...
		/// </summary>
		public void FindBreakPoint(IVwGraphics vg, IVwTextSource source, IVwJustifier justifier,
			int ichMin, int ichLim, int ichLimBacktrack, bool fNeedFinalBreak, bool fStartLine, int dxMaxWidth,
			LgLineBreak lbPref, LgLineBreak lbMax, LgTrailingWsHandling twsh, bool fParaRightToLeft,
			out ILgSegment segRet, out int dichLimSeg, out int dxWidth, out LgEndSegmentType est, ILgSegment segPrev)
		{
			int width = 0;
			int lastSpace = -1;
			string input;
			int cchRead = ichLim - ichMin;
			if (fNeedFinalBreak)
				cchRead++;
			using (ArrayPtr ptr = new ArrayPtr((cchRead) * 2 + 2))
			{
				source.Fetch(ichMin, ichMin + cchRead, ptr.IntPtr);
				input = MarshalEx.NativeToString(ptr, cchRead, true);
			}
			int widthAtLastSpace = 0;

			int limit = ichLimBacktrack - ichMin;
			int i;
			for (i = 0; i < limit; i++)
			{
				if (input[i] == ' ')
				{
					lastSpace = i;
					widthAtLastSpace = width;
				}
				var charWidth = CharWidth(input[i]);
				if (width + charWidth > dxMaxWidth)
					break;
				width += charWidth;
			}
			if (i < input.Length && input[i] == ' ' && width < dxMaxWidth)
			{
				// good break exactly at limit
				lastSpace = i;
				widthAtLastSpace = width;
			}
			FakeSegment result = null;
			if (i == input.Length && width < dxMaxWidth) // also handles special case of zero-char range
				result = new FakeSegment(input, width, Ws, LgEndSegmentType.kestNoMore); // all fit
			else if (lastSpace >= 0)
			{
				// include trailing spaces without increasing width. Todo: depends on twsh?
				int end = lastSpace;
				while (end < limit - 1 && input[end] == ' ')
					end++;
				result = new FakeSegment(input.Substring(0, end), widthAtLastSpace, Ws,
										 LgEndSegmentType.kestMoreLines);
			}
			else if (lbMax == LgLineBreak.klbWordBreak)
			{
				// we needed a word break and didn't get one.
				segRet = null;
				dichLimSeg = 0;
				dxWidth = 0;
				est = LgEndSegmentType.kestNothingFit;
				return;
			}
			else
			{
				// remaining possibility is that we need to make some sort of segment.
				if (i == 0)
				{
					width += CharWidth(input[i]);
					i++; // must have at least one character
				}
				result = new FakeSegment(input, width, Ws, LgEndSegmentType.kestMoreLines);
			}
			segRet = result;
			dichLimSeg = result.Length;
			dxWidth = result.Width;
			est = result.EndSegType;
			result.Height = SegmentHeight;
			result.Ascent = SegmentAscent;
		}

		/// <summary>
		/// This is the basis for faking rendering: various widths for characters.
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		internal static int CharWidth(char ch)
		{
			switch(ch)
			{
				case 'i':
					return 2;
				case '.':
				case ',':
					return 1;
				case 'l':
					return 3;
				case 'm':
					return 5;
				case 'w':
					return 6;
				case '^':
				case '~':
					return 0; // treat these as diacritics.
				default:
					return 4;
			}
		}

		public static int SimulatedWidth(string text)
		{
			return (from ch in text select CharWidth(ch)).Sum();
		}

		public int ScriptDirection
		{
			get { throw new NotImplementedException(); }
		}

		public Guid ClassId
		{
			get { throw new NotImplementedException(); }
		}

		public ILgWritingSystemFactory WritingSystemFactory { get; set;}
	}

	class FakeSegment : ILgSegment
	{
		public FakeSegment(string text, int width, int ws, LgEndSegmentType est)
		{
			Text = text;
			Length = text.Length;
			Width = width;
			Ws = ws;
			EndSegType = est;
			NextCharPlacementResults = new List<CharPlacementResults>();
			PrevCharPlacementArgs = new List<CharPlacementArgs>();
		}

		internal string Text { get; private set; }
		// Length the segment should claim to be (in chars)
		internal int Length
		{
			get;
			private set;
		}

		// Width the segment should claim to be (in pixels)
		internal int Width
		{
			get;
			set;
		}

		// The writing system to claim to be
		internal int Ws
		{ get; private set; }

		// Height in pixels to claim to be.
		internal int Height { get; set; }

		// Ascent to claim to have.
		internal int Ascent { get; set; }

		internal LgEndSegmentType EndSegType { get; private set; }

		public List<object> DrawActions { get; set; }

		internal class DrawTextAction
		{
			public FakeSegment Segment;
			public int IchBase;
			public IVwGraphics Vg;
			public Rect RcSrc;
			public Rect RcDst;
		}

		public void DrawText(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			if (DrawActions == null)
				DrawActions = new List<object>();
			DrawActions.Add(new DrawTextAction() { Segment = this, IchBase = ichBase, Vg = _vg, RcSrc = rcSrc, RcDst = rcDst });
			dxdWidth = Width;
		}

		public void Recompute(int ichBase, IVwGraphics _vg)
		{
		}

		public int get_Width(int ichBase, IVwGraphics _vg)
		{
			return Width;
		}

		public int get_RightOverhang(int ichBase, IVwGraphics _vg)
		{
			return 0;
		}

		public int get_LeftOverhang(int ichBase, IVwGraphics _vg)
		{
			return 0;
		}

		public int get_Height(int ichBase, IVwGraphics _vg)
		{
			return Height;
		}

		public int get_Ascent(int ichBase, IVwGraphics _vg)
		{
			return Ascent;
		}

		public void Extent(int ichBase, IVwGraphics _vg, out int width, out int height)
		{
			width = Width;
			height = Height;
		}

		public Rect BoundingRect(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public void GetActualWidth(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			throw new NotImplementedException();
		}

		public int get_AscentOverhang(int ichBase, IVwGraphics _vg)
		{
			return 0;
		}

		public int get_DescentOverhang(int ichBase, IVwGraphics _vg)
		{
			return 0;
		}

		public bool get_RightToLeft(int ichBase)
		{
			return false;
		}

		public bool get_DirectionDepth(int ichBase, out int _nDepth)
		{
			throw new NotImplementedException();
		}

		public void SetDirectionDepth(int ichwBase, int nNewDepth)
		{
			throw new NotImplementedException();
		}

		public int get_WritingSystem(int ichBase)
		{
			return Ws;
		}

		public int get_Lim(int ichBase)
		{
			return Length;
		}

		public int get_LimInterest(int ichBase)
		{
			throw new NotImplementedException();
		}

		public void set_EndLine(int ichBase, IVwGraphics _vg, bool fNewVal)
		{
		}

		public void set_StartLine(int ichBase, IVwGraphics _vg, bool fNewVal)
		{
		}

		public LgLineBreak get_StartBreakWeight(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public LgLineBreak get_EndBreakWeight(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		private int m_stretch;
		public int get_Stretch(int ichBase)
		{
			return m_stretch;
		}

		public void set_Stretch(int ichBase, int xs)
		{
			m_stretch = xs;
		}

		/// <summary>
		/// An insertion point is valid unless between a base character and diacritic.
		/// For simulation purposes the diacritics are '^' and '~".
		/// </summary>
		public LgIpValidResult IsValidInsertionPoint(int ichBase, IVwGraphics _vg, int ich)
		{
			int offset = ich - ichBase;
			if (offset >= 0 && offset < Text.Length && (Text[offset] == '~' || Text[offset] == '^'))
				return LgIpValidResult.kipvrBad;
			return LgIpValidResult.kipvrOK;
		}

		public bool DoBoundariesCoincide(int ichBase, IVwGraphics _vg, bool fBoundaryEnd, bool fBoundaryRight)
		{
			return true;
		}

		public MockSegment.DrawInsertionPointArgs LastDrawIpCall { get; private set; }

		public void DrawInsertionPoint(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ich, bool fAssocPrev, bool fOn, LgIPDrawMode dm)
		{
			LastDrawIpCall = new MockSegment.DrawInsertionPointArgs
			{
				IchBase = ichBase,
				Graphics = _vg,
				RcSrc = rcSrc,
				RcDst = rcDst,
				Ich = ich,
				AssocPrev = fAssocPrev,
				On = fOn,
				DrawMode = dm
			};
		}

		public MockSegment.PositionsOfIpArgs LastPosIpCall { get; private set; }
		public MockSegment.PositionsOfIpResults NextPosIpResult { get; set; }

		public void PositionsOfIP(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ich, bool fAssocPrev, LgIPDrawMode dm,
			out Rect rectPrimary, out Rect rectSecondary, out bool fPrimaryHere, out bool fSecHere)
		{
			LastPosIpCall = new MockSegment.PositionsOfIpArgs();
			LastPosIpCall.IchBase = ichBase;
			LastPosIpCall.Graphics = _vg;
			LastPosIpCall.RcSrc = rcSrc;
			LastPosIpCall.RcDst = rcDst;
			LastPosIpCall.Ich = ich;
			LastPosIpCall.AssocPrev = fAssocPrev;
			LastPosIpCall.DrawMode = dm;
			if (NextPosIpResult != null)
			{
				rectPrimary = NextPosIpResult.RectPrimary;
				rectSecondary = new Rect(); // NextPosIpResult.RectSecondary;
				fPrimaryHere = NextPosIpResult.PrimaryHere;
				fSecHere = false; // NextPosIpResult.SecHere;
			}
			else
			{
				rectPrimary = new Rect(0, 0, 0, 0);
				rectSecondary = new Rect(0, 0, 0, 0);
				fPrimaryHere = true; // useful result when we don't care to prepare for this call.
				fSecHere = false;
			}
		}

		public MockSegment.DrawRangeArgs LastDrawRangeCall
		{
			get;
			set;
		}

		public Rect DrawRange(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ichMin, int ichLim, int ydTop, int ydBottom, bool bOn, bool fIsLastLineOfSelection)
		{
			LastDrawRangeCall = new MockSegment.DrawRangeArgs
			{
				IchBase = ichBase,
				Graphics = _vg,
				RcSrc = rcSrc,
				RcDst = rcDst,
				IchMin = ichMin,
				IchLim = ichLim,
				YdTop = ydTop,
				YdBottom = ydBottom,
				On = bOn,
				IsLastLineOfSelection = fIsLastLineOfSelection
			};
			// Enhance JohnT: figure sensible value for left and right if testing requires it.
			return new Rect(0, ydTop, 0, ydBottom);
		}

		public int LeftPositionOfRangeResult { set; get; }
		public int RightPositionOfRangeResult { set; get; }
		public MockSegment.DrawRangeArgs LastPositionOfRangeArgs;

		public bool PositionOfRange(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ichMin, int ichLim,
			int ydTop, int ydBottom, bool fIsLastLineOfSelection, out Rect rsBounds)
		{
			LastPositionOfRangeArgs = new MockSegment.DrawRangeArgs
			{
				IchBase = ichBase,
				Graphics = _vg,
				RcSrc = rcSrc,
				RcDst = rcDst,
				IchMin = ichMin,
				IchLim = ichLim,
				YdTop = ydTop,
				YdBottom = ydBottom,
				On = true,		// meaningless here, but it is always passed true to Draw, so some verification stuff checks that.
				IsLastLineOfSelection = fIsLastLineOfSelection
			};
			rsBounds = new Rect(0, 0, 0, 0); // default
			if (ichLim <= ichBase || ichMin >= ichBase + Length)
				return false;
			rsBounds = new Rect(LeftPositionOfRangeResult, ydTop, RightPositionOfRangeResult, ydBottom);
			return true;
		}

		public class PointToCharArgs
		{
			public int IchBase;
			public IVwGraphics Vg;
			public Rect RcSrc;
			public Rect RcDst;
			public Point ClickPosition;
		}

		public PointToCharArgs LastPointToCharArgs { get; private set; }

		public void PointToChar(int ichBase, IVwGraphics vg, Rect rcSrc, Rect rcDst, Point tdClickPosition,
			out int ichOut, out bool fAssocPrev)
		{
			LastPointToCharArgs = new PointToCharArgs()
				{IchBase = ichBase, Vg = vg, RcSrc = rcSrc, RcDst = rcDst, ClickPosition = tdClickPosition};
			int xpos = tdClickPosition.X + rcSrc.left - rcDst.left; // Enhance: not sure this is right for rcDst, and ignores zoom
			int ich = 0;
			fAssocPrev = false;
			if (xpos < 0)
			{
				ichOut = ich + ichBase;
				return;
			}
			int lastCharWidth = 0;
			while (xpos >= 0 & ich < Length)
			{
				lastCharWidth = FakeRenderEngine.CharWidth(Text[ich]);
				xpos -= lastCharWidth;
				ich++;
			}
			if (xpos >= 0 && ich > 0)
			{
				fAssocPrev = true; // click to right of last char
				{
					ichOut = ich + ichBase;
					return;
				}
			}
			// We clicked somewhere in the last character, of width lastCharWidth.
			// If we clicked in the first half of it, treat as a click before it.
			// -xpos is the distance we clicked left of its right edge.
			if ((-xpos) < lastCharWidth / 2)
				fAssocPrev = true; // click on right half, return current index and assoc prev
			else if (ich > 0)
				ich--; // click on left half, return previous index and assoc prev false.
			ichOut = ich + ichBase;
		}

		public void ArrowKeyPosition(int ichBase, IVwGraphics _vg, ref int _ich, ref bool _fAssocPrev, bool fRight, bool fMovingIn, out bool _fResult)
		{
			throw new NotImplementedException();
		}

		public void ExtendSelectionPosition(int ichBase, IVwGraphics _vg, ref int _ich, bool fAssocPrevMatch, bool fAssocPrevNeeded, int ichAnchor, bool fRight, bool fMovingIn, out bool _fRet)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This is an exact conversion of how the old C++ code in segments implements the transformations
		/// signified by rcSrc and rcDst.
		/// </summary>
		int MapXTo(int x, Rect rcSrc, Rect rcDst)
		{

			int dxs = rcSrc.right - rcSrc.left;
			int dxd = rcDst.right - rcDst.left;

			if (dxs == dxd)
				return x + rcDst.left - rcSrc.left;

			return rcDst.left + MulDiv(x - rcSrc.left, dxd, dxs);
		}


		int MulDiv(int n1, int n2, int denom)
		{
			return (int)(((long)n1 * (long)n2) / (long)denom);
		}

		int MapYTo(int y, Rect rcSrc, Rect rcDst)
		{
			int dys = rcSrc.bottom - rcSrc.top;
			int dyd = rcDst.bottom - rcDst.top;

			if (dys == dyd)
				return y + rcDst.top - rcSrc.top;

			return rcDst.top + MulDiv(y - rcSrc.top, dyd, dys);
		}

		public class CharPlacementResults
		{
			public int[] Lefts;
			public int[] Rights;
			public int[] Tops;
		}

		public class CharPlacementArgs
		{
			public int IchBase;
			public IVwGraphics Vg;
			public int IchMin;
			public int IchLim;
			public Rect RcSrc;
			public Rect RcDst;
			public bool SkipSpace;
			public int CxdMax;
		}

		/// <summary>
		/// Use this to prespecify what results should next be returned by GetCharPlacement.
		/// </summary>
		public List<CharPlacementResults> NextCharPlacementResults { get; private set; }
		public List<CharPlacementArgs> PrevCharPlacementArgs { get; private set; }

		public void GetCharPlacement(int ichBase, IVwGraphics vg, int ichMin, int ichLim, Rect rcSrc, Rect rcDst,
			bool fSkipSpace, int cxdMax, out int cxd,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler),
				SizeParamIndex = 1)] ArrayPtr/*int[]*/ rgxdLefts,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler),
				SizeParamIndex = 1)] ArrayPtr/*int[]*/ rgxdRights,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler),
				SizeParamIndex = 1)] ArrayPtr/*int[]*/ rgydUnderTops)
		{
			if (cxdMax == 0)
			{
				// don't count this call; it's a preliminary query to get the length.
				if (NextCharPlacementResults.Count == 0)
					cxd = 1;
				else
				{
					cxd = NextCharPlacementResults[0].Lefts.Length;
				}
				return;
			}
			PrevCharPlacementArgs.Add(new CharPlacementArgs()
										{
											IchBase = ichBase,
											Vg = vg,
											IchMin = ichMin,
											IchLim = ichLim,
											RcSrc = rcSrc,
											RcDst = rcDst,
											SkipSpace = fSkipSpace,
											CxdMax = cxdMax
										});
			var lefts = new int[1];
			var rights = new int[1];
			var tops = new int[1];
			if (NextCharPlacementResults.Count == 0)
			{
				// This is a plausible algorithm which is not currently used and hasn't been tried.
				cxd = 1;
				if (cxdMax < 1)
					return;

				int cchMin = ichMin - ichBase;
				if (cchMin <= 0 || cchMin >= Length)
				{
					cxd = 0;
					return;
				}
				int cch = ichLim - ichMin;
				if (ichLim > ichBase + Length)
					cch = Length - (ichMin - ichBase);

				int left = FakeRenderEngine.SimulatedWidth(Text.Substring(cchMin));
				int right = left + FakeRenderEngine.SimulatedWidth(Text.Substring(cchMin, cch));
				lefts[0] = MapXTo(left, rcSrc, rcDst);
				rights[0] = MapXTo(right, rcSrc, rcDst);
				tops[0] = MapYTo(Ascent + 1, rcSrc, rcDst);
			}
			else
			{
				var nextResult = NextCharPlacementResults[0];
				NextCharPlacementResults.RemoveAt(0);
				cxd = nextResult.Lefts.Length;
				if (cxdMax == 0)
					return;
				lefts = nextResult.Lefts;
				rights = nextResult.Rights;
				tops = nextResult.Tops;
			}

			MarshalEx.ArrayToNative(rgxdLefts, cxdMax, lefts);
			MarshalEx.ArrayToNative(rgxdRights, cxdMax, rights);
			MarshalEx.ArrayToNative(rgydUnderTops, cxdMax, tops);
		}

		internal class DrawTextNoBackgroundAction : DrawTextAction
		{

		}
		public void DrawTextNoBackground(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			if (DrawActions == null)
				DrawActions = new List<object>();
			DrawActions.Add(new DrawTextNoBackgroundAction() { Segment = this, IchBase = ichBase, Vg = _vg, RcSrc = rcSrc, RcDst = rcDst });
			dxdWidth = Width;
		}
	}

	public class FakeRendererFactory : IRendererFactory
	{
		Dictionary<int, IRenderEngine> m_renderers = new Dictionary<int, IRenderEngine>();
		public IRenderEngine GetRenderer(int ws, IVwGraphics vg)
		{
			IRenderEngine result;
			if (m_renderers.TryGetValue(ws, out result))
				return result;
			result = new FakeRenderEngine();
			m_renderers[ws] = result;
			return result;
		}

		public int UserWs
		{
			get { return 1; }
		}

		internal void SetRenderer(int ws, IRenderEngine renderer)
		{
			m_renderers[ws] = renderer;
		}
	}
}
