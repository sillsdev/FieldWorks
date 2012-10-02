using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		internal bool RightToLeft { get; set; }

		public int SegDatMaxLength
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Emulate what a real renderer will do, assuming character widths are as determined by CharWidth,
		/// and only space is a valid line break.
		/// Including width of white space:
		/// Basically, if the white space is known to end the line, we don't count its width.
		/// - If we stopped mid-line because of a hard break, ichLim{backtracking} != source length, etc.,
		/// we will try to put more on line, so must include space width.
		/// - If we stop at the very end of the whole paragraph, don't include it? But then how can IP be after
		/// final space?
		/// - If we know next stuff must go on another line leave out its width.
		/// </summary>
		public void FindBreakPoint(IVwGraphics vg, IVwTextSource source, IVwJustifier justifier,
			int ichMin, int ichLim, int ichLimBacktrack, bool fNeedFinalBreak, bool fStartLine, int dxMaxWidth,
			LgLineBreak lbPref, LgLineBreak lbMax, LgTrailingWsHandling twsh, bool fParaRightToLeft,
			out ILgSegment segRet, out int dichLimSeg, out int dxWidth, out LgEndSegmentType est, ILgSegment segPrev)
		{
			// Default return values in case we cannot make a segment.
			segRet = null;
			dichLimSeg = 0;
			dxWidth = 0;
			est = LgEndSegmentType.kestNothingFit;

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

			int ichLimWs = GetLimInSameWs(ichMin, source, ichLimBacktrack);

			FakeSegment result = null;
			// limit in input of characters we may include in segment.
			int limit = Math.Min(ichLimBacktrack, ichLimWs) - ichMin;
			int limWholeInput = ichLim - ichMin;
			int ichInput = 0;
			if (twsh == LgTrailingWsHandling.ktwshOnlyWs)
				result = MakeWhiteSpaceOnlySegment(input, width, ichInput, limit);
			else // mixed allowed or no-white-space
			{
				// Advance past what will fit, keeping track of the last good break position (white space).
				for (; ichInput < limit; ichInput++)
				{
					if (input[ichInput] == ' ')
					{
						lastSpace = ichInput;
						widthAtLastSpace = width;
					}
					var charWidth = CharWidth(input[ichInput]);
					if (width + charWidth > dxMaxWidth)
						break;
					width += charWidth;
				}
				Debug.Assert(width <= dxMaxWidth); // loop never allows it to exceed max
				if (ichInput < input.Length && input[ichInput] == ' ')
				{
					// good break exactly at limit
					lastSpace = ichInput;
					widthAtLastSpace = width;
				}

				if (ichInput == limit && ichLimBacktrack >= ichLimWs) // all the text in our WS fit; also handles special case of zero-char range
				{
					// everything we were allowed to include fit; if it wasn't absolutely everything caller needs to decide
					// about break.
					if (twsh == LgTrailingWsHandling.ktwshAll)
						result = new FakeSegment(input.Substring(0, limit), width, Ws,
							ichInput == limWholeInput ? LgEndSegmentType.kestNoMore : LgEndSegmentType.kestWsBreak);
					else
					{
						// must be no-trailing-white-space (all WS is handled above). strip off any trailing spaces.
						while (ichInput > 0 && input[ichInput - 1] == ' ')
						{
							ichInput--;
							width -= CharWidth(' ');
						}
						if (ichInput == limit) // no trailing ws removed (also handles empty run)
							result = new FakeSegment(input.Substring(0, limit), width, Ws,
								ichInput == limWholeInput ? LgEndSegmentType.kestNoMore : LgEndSegmentType.kestWsBreak); // all fit
						else if (ichInput > 0) // got some text, stripped some white space
							result = new FakeSegment(input.Substring(0, ichInput), width, Ws, LgEndSegmentType.kestMoreWhtsp);
						else
						{
							// we're not done with the whole input, but no non-white at start
							return;
						}
					}
				}
				else // some stuff we wanted to put on line didn't fit;
					if (lastSpace >= 0) // there's a good break we can use
					{
						int end = lastSpace;
						if (twsh == LgTrailingWsHandling.ktwshAll)
						{
							// include trailing spaces without increasing width; they 'disappear' into line break.
							while (end < limit && input[end] == ' ')
								end++;
						}
						result = new FakeSegment(input.Substring(0, end), widthAtLastSpace, Ws,
							LgEndSegmentType.kestMoreLines);
					}
					else if (lbMax == LgLineBreak.klbWordBreak)
					{
						// we needed a word break and didn't get one.
						return;
					}
					else
					{
						// remaining possibility is that we need to make some sort of segment with at least one character.
						// (if we don't have any i = limit = 0 and we take another branch).
						if (ichInput == 0)
						{
							width += CharWidth(input[ichInput]);
							ichInput++; // must have at least one character
						}
						result = new FakeSegment(input.Substring(ichInput), width, Ws, LgEndSegmentType.kestMoreLines);
					}
			}
			if (result == null)
				return;
			segRet = result;
			dichLimSeg = result.Length;
			dxWidth = result.Width;
			est = result.EndSegType;
			result.Height = SegmentHeight;
			result.Ascent = SegmentAscent;
			// DirectionDepth is usually 0 for LTR text in an LTR paragraph.
			// For RTL text, however, it is always 1.
			// And for UPSTREAM LTR text (in an RTL paragraph) it has to be 2.
			if (RightToLeft)
				result.DirectionDepth = 1;
			else if (fParaRightToLeft)
				result.DirectionDepth = 2;
		}

		private FakeSegment MakeWhiteSpaceOnlySegment(string input, int width, int ichInput, int limit)
		{
			FakeSegment result;
			for (; ichInput < limit && input[ichInput] == ' '; ichInput++)
				width += CharWidth(' ');
			if (ichInput == 0)
				return null;
			result = new FakeSegment(input.Substring(0, ichInput), width, Ws,
									 ichInput < limit ? LgEndSegmentType.kestMoreLines : LgEndSegmentType.kestNoMore);
			result.WeakDirection = true; // the only possibility, in this fake.
			return result;
		}

		private int GetLimInSameWs(int ichMin, IVwTextSource source, int ichLimBacktrack)
		{
			LgCharRenderProps chrpThis;
			int ichMinRun, ichLimWs;
			source.GetCharProps(ichMin, out chrpThis, out ichMinRun, out ichLimWs);
			while (ichLimWs < ichLimBacktrack)
			{
				int ichLimRunNext;
				LgCharRenderProps chrpNext;
				source.GetCharProps(ichMinRun, out chrpNext, out ichMinRun, out ichLimRunNext);
				if (chrpNext.ws != chrpThis.ws)
					break;
				ichLimWs = ichLimRunNext;
				ichMinRun = ichLimRunNext;
			}
			return ichLimWs;
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

		public int DirectionDepth { get; set; }
		public bool WeakDirection { get; set; }

		public bool get_DirectionDepth(int ichBase, out int depth)
		{
			depth = DirectionDepth;
			return WeakDirection;
		}

		public void SetDirectionDepth(int ichwBase, int nNewDepth)
		{
			DirectionDepth = nNewDepth;
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
				fSecHere = false;
				if (ich < ichBase || ich > ichBase + Length)
				{
					fPrimaryHere = false;
					return;
				}
				fPrimaryHere = true; // useful result when we don't care to prepare for this call.
				int width = 0;
				for (int i = 0; i < ich - ichBase; i++)
					width += FakeRenderEngine.CharWidth(Text[i]);
				rectPrimary = PaintTransform.ConvertToPaint(new Rect(width - 1, 0, width + 1, Height),
					rcSrc, rcDst);
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

		public bool RightToLeft(int ws)
		{
			return ((FakeRenderEngine)GetRenderer(ws, null)).RightToLeft;
		}

		internal void SetRenderer(int ws, IRenderEngine renderer)
		{
			m_renderers[ws] = renderer;
		}
	}
}
