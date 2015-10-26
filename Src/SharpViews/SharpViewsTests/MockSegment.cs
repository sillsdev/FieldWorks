// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	class MockSegment : ILgSegment
	{
		public MockSegment(int len, int width, int ws, LgEndSegmentType est)
		{
			Length = len;
			Width = width;
			Ws = ws;
			EndSegType = est;
			Height = 10; // unless overridden
			Ascent = 7;  // unless overridden
		}
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

		internal class SegDrawTextArgs
		{
			internal int ichBase;
			internal IVwGraphics vg;
			internal Rect rcSrc;
			internal Rect rcDst;
		}

		internal SegDrawTextArgs LastDrawTextCall { get; private set; }

		public void DrawText(int ichBase, IVwGraphics vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			dxdWidth = Width;
			var args = new SegDrawTextArgs();
			args.ichBase = ichBase;
			args.vg = vg;
			args.rcSrc = rcSrc;
			args.rcDst = rcDst;
			LastDrawTextCall = args;
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

		public void Extent(int ichBase, IVwGraphics vg, out int x, out int y)
		{
			x = get_Width(ichBase, vg);
			y = get_Height(ichBase, vg);
		}

		public Rect BoundingRect(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst)
		{
			throw new System.NotImplementedException();
		}

		public void GetActualWidth(int ichBase, IVwGraphics vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			dxdWidth = get_Width(ichBase, vg);
		}

		public int get_AscentOverhang(int ichBase, IVwGraphics _vg)
		{
			return 2;
		}

		public int get_DescentOverhang(int ichBase, IVwGraphics _vg)
		{
			return 1;
		}

		public bool get_RightToLeft(int ichBase)
		{
			return false;
		}

		public bool get_DirectionDepth(int ichBase, out int _nDepth)
		{
			_nDepth = 0;
			return true;
		}

		public void SetDirectionDepth(int ichwBase, int nNewDepth)
		{
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
			throw new System.NotImplementedException();
		}

		public void set_EndLine(int ichBase, IVwGraphics _vg, bool fNewVal)
		{
		}

		public void set_StartLine(int ichBase, IVwGraphics _vg, bool fNewVal)
		{
		}

		public LgLineBreak get_StartBreakWeight(int ichBase, IVwGraphics _vg)
		{
			throw new System.NotImplementedException();
		}

		public LgLineBreak get_EndBreakWeight(int ichBase, IVwGraphics _vg)
		{
			throw new System.NotImplementedException();
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

		public LgIpValidResult IsValidInsertionPoint(int ichBase, IVwGraphics _vg, int ich)
		{
			return LgIpValidResult.kipvrOK;
		}

		public bool DoBoundariesCoincide(int ichBase, IVwGraphics _vg, bool fBoundaryEnd, bool fBoundaryRight)
		{
			return true;
		}

		public class DrawInsertionPointArgs
		{
			public int IchBase;
			public IVwGraphics Graphics;
			public Rect RcSrc;
			public Rect RcDst;
			public int Ich;
			public bool AssocPrev;
			public bool On;
			public LgIPDrawMode DrawMode;
		}

		public class PositionsOfIpArgs
		{
			public int IchBase;
			public IVwGraphics Graphics;
			public Rect RcSrc;
			public Rect RcDst;
			public int Ich;
			public bool AssocPrev;
			public LgIPDrawMode DrawMode;
		}

		public class PositionsOfIpResults
		{
			public Rect RectPrimary;
			//public Rect RectSecondary;
			public bool PrimaryHere;
			//public bool SecHere;
		}

		public DrawInsertionPointArgs LastDrawIpCall { get; private set; }

		public void DrawInsertionPoint(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ich, bool fAssocPrev, bool fOn, LgIPDrawMode dm)
		{
			LastDrawIpCall = new DrawInsertionPointArgs
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

		public PositionsOfIpArgs LastPosIpCall { get; private set; }
		public PositionsOfIpResults NextPosIpResult { get; set; }

		public void PositionsOfIP(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ich, bool fAssocPrev, LgIPDrawMode dm,
			out Rect rectPrimary, out Rect rectSecondary, out bool fPrimaryHere, out bool fSecHere)
		{
			LastPosIpCall = new PositionsOfIpArgs();
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
				rectPrimary = new Rect(0,0,0,0);
				rectSecondary = new Rect(0,0,0,0);
				fPrimaryHere = true; // useful result when we don't care to prepare for this call.
				fSecHere = false;
			}
		}

		public class DrawRangeArgs
		{
			public int IchBase;
			public IVwGraphics Graphics;
			public Rect RcSrc;
			public Rect RcDst;
			public int IchMin;
			public int IchLim;
			public int YdTop;
			public int YdBottom;
			public bool On;
			public bool IsLastLineOfSelection;
		}

		public DrawRangeArgs LastDrawRangeCall
		{
			get; private set;
		}

		/// <summary>
		/// These two allow us to control where the segment will consider to be the left and right of any selection it is told to draw.
		/// </summary>
		public int DrawRangeLeft;
		public int DrawRangeRight;

		public Rect DrawRange(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ichMin, int ichLim, int ydTop, int ydBottom, bool bOn, bool fIsLastLineOfSelection)
		{
			LastDrawRangeCall = new DrawRangeArgs
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
			return new Rect(DrawRangeLeft, ydTop, DrawRangeRight, ydBottom);
		}

		public DrawRangeArgs LastPositionOfRangeCall { get; set; }

		public class PositionOfRangeResult
		{
			public Rect RsBounds;
			public bool DisplayedInSegment;
		}

		public PositionOfRangeResult NextPositionOfRange;

		public bool PositionOfRange(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ichMin, int ichLim, int ydTop, int ydBottom, bool fIsLastLineOfSelection, out Rect rsBounds)
		{
			LastDrawRangeCall = new DrawRangeArgs
			{
				IchBase = ichBase,
				Graphics = _vg,
				RcSrc = rcSrc,
				RcDst = rcDst,
				IchMin = ichMin,
				IchLim = ichLim,
				YdTop = ydTop,
				YdBottom = ydBottom,
				IsLastLineOfSelection = fIsLastLineOfSelection
			};
			if (NextPositionOfRange == null)
			{
				rsBounds = new Rect(-1, -1, -1, -1);
				return false;
			}
			else
			{
				rsBounds = NextPositionOfRange.RsBounds;
				return NextPositionOfRange.DisplayedInSegment;
			}
		}

		private int m_pointToCharOffset;
		private bool m_pointToCharAssocPrev;
		internal void OnPointToCharReturn(int offset, bool assocPrev)
		{
			m_pointToCharOffset = offset;
			m_pointToCharAssocPrev = assocPrev;
		}

		public int PointToCharIchBase;
		public IVwGraphics PointToCharVg;
		public Rect PointToCharRcSrc;
		public Rect PointToCharRcDst;
		public Point PointToCharClickPosition;

		public void PointToChar(int ichBase, IVwGraphics vg, Rect rcSrc, Rect rcDst, Point tdClickPosition, out int ich, out bool assocPrev)
		{
			PointToCharIchBase = ichBase;
			PointToCharVg = vg;
			PointToCharRcSrc = rcSrc;
			PointToCharRcDst = rcDst;
			PointToCharClickPosition = tdClickPosition;
			ich = m_pointToCharOffset;
			assocPrev = m_pointToCharAssocPrev;
		}

		public void ArrowKeyPosition(int ichBase, IVwGraphics _vg, ref int _ich, ref bool _fAssocPrev, bool fRight, bool fMovingIn, out bool _fResult)
		{
			throw new System.NotImplementedException();
		}

		public void ExtendSelectionPosition(int ichBase, IVwGraphics _vg, ref int _ich, bool fAssocPrevMatch, bool fAssocPrevNeeded, int ichAnchor, bool fRight, bool fMovingIn, out bool _fRet)
		{
			throw new System.NotImplementedException();
		}

		public void GetCharPlacement(int ichBase, IVwGraphics _vg, int ichMin, int ichLim, Rect rcSrc, Rect rcDst, bool fSkipSpace, int cxdMax, out int _cxd, ArrayPtr _rgxdLefts, ArrayPtr _rgxdRights, ArrayPtr _rgydUnderTops)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// This method is used for something like a font testing program.
		/// Review (SharonC): should it be in a separate interface?
		/// HRESULT GetGlyphsAndPositions(
		///  [in] int ichBase,
		///  [in] IVwGraphics pvg,
		///  [in] RECT rcSrc,  as for DrawText
		///  [in] RECT rcDst,
		///  [in] int cchMax,
		///  [out] int pcchRet,
		///  [out, size_is(cchMax)] OLECHAR prgchGlyphs,
		///  [out, size_is(cchMax)] int prgxd,
		///  [out, size_is(cchMax)] int prgyd);
		/// This method is intended for debugging only. Eventually we can get rid of it, but
		/// it seems like it is useful to keep around for a while.
		/// HRESULT GetCharData(
		///  [in] int ichBase,
		///  [in] int cchMax,
		///  [out, size_is(cchMax)] OLECHAR prgch,
		///  [out] int pcchRet);
		/// Should do exactly the same as DrawText would do if all background
		/// colors were transparent. That is, no background is ever painted.
		///</summary>
		/// <param name='ichBase'> </param>
		/// <param name='_vg'> </param>
		/// <param name='rcSrc'> </param>
		/// <param name='rcDst'> </param>
		/// <param name='dxdWidth'> </param>
		public void DrawTextNoBackground(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			// for purposes of this mock, the two can be considered equivalent.
			DrawText(ichBase, _vg, rcSrc, rcDst, out dxdWidth);
		}

		internal LgEndSegmentType EndSegType { get; private set; }
	}
}
