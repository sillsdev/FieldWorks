using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	enum UnexpectedSegments
	{
		DontFit,
		MakeOneCharSeg,
	}
	class MockRenderEngine : IRenderEngine
	{
		public UnexpectedSegments OtherSegPolicy = UnexpectedSegments.DontFit;

		public void InitRenderer(IVwGraphics _vg, string bstrData)
		{
			throw new System.NotImplementedException();
		}



		public void FontIsValid()
		{
			throw new System.NotImplementedException();
		}

		public void FindBreakPoint(IVwGraphics vg, IVwTextSource _ts, IVwJustifier _vjus, int ichMin, int ichLim,
			int ichLimBacktrack, bool fNeedFinalBreak, bool fStartLine, int dxMaxWidth, LgLineBreak lbPref,
			LgLineBreak lbMax, LgTrailingWsHandling twsh, bool fParaRightToLeft,
			out ILgSegment segRet, out int dichLimSeg, out int dxWidth, out LgEndSegmentType est, ILgSegment _segPrev)
		{

			MockSegment seg;
			var key = new MockSegKey(ichMin, ichLim);
			if (lbPref != LgLineBreak.klbClipBreak && m_failOnPartialLine.Contains(key))
			{
				// fail.
				segRet = null;
				dichLimSeg = 0;
				dxWidth = 0;
				est = LgEndSegmentType.kestNothingFit;
				return;
			}
			if (m_potentialSegs.TryGetValue(key, out seg))
			{
				if (seg.Width < dxMaxWidth) // otherwise we meant it for the next line.
				{
					segRet = seg;
					dichLimSeg = seg.get_Lim(ichMin);
					dxWidth = seg.get_Width(ichMin, vg);
					est = seg.EndSegType;
					return;
				}
			}
			switch(OtherSegPolicy)
			{
				case UnexpectedSegments.DontFit:
				default: // to make compiler happy
					Assert.AreNotEqual(LgLineBreak.klbClipBreak, lbMax,
									   "FindBreakPoint called with unexpected arguments.");
					// If we aren't pre-prepared for the requested break, assume nothing fits with these arguments.
					segRet = null;
					dichLimSeg = 0;
					dxWidth = 0;
					est = LgEndSegmentType.kestNothingFit;
					break;
				case UnexpectedSegments.MakeOneCharSeg:
					// Make a very narrow segment that will fit and allow more stuff. This will usually give a test failure if not intentional.
					seg = new MockSegment(1, 2, 1, LgEndSegmentType.kestOkayBreak);
					// If we aren't pre-prepared for the requested break, assume nothing fits with these arguments.
					segRet = seg;
					dichLimSeg = 1;
					dxWidth = 2;
					est = LgEndSegmentType.kestOkayBreak;
					break;
			}
		}

		public int SegDatMaxLength
		{
			get { throw new System.NotImplementedException(); }
		}

		public int ScriptDirection
		{
			get { throw new System.NotImplementedException(); }
		}

		public Guid ClassId
		{
			get { throw new System.NotImplementedException(); }
		}

		public ILgWritingSystemFactory WritingSystemFactory { get; set; }

		class MockSegKey
		{
			public  MockSegKey(int min, int lim)
			{
				IchMin = min;
				IchLim = lim;
			}

			private int IchMin
			{
				get;
				set;
			}

			private int IchLim { get; set; }

			public override bool Equals(object obj)
			{
				var other = obj as MockSegKey;
				return other != null && other.IchLim == IchLim && other.IchMin == IchMin;
			}

			public override int GetHashCode()
			{
				return IchMin + IchLim;
			}
		}

		internal void Reset()
		{
			m_potentialSegsInOrder.Clear();
			m_potentialSegs.Clear();
			m_failOnPartialLine.Clear();
		}

		// If the caller asks to find a break and passes the specified args, return a segment with the specified properties.
		internal void AddMockSeg(int ichMin, int ichLim, int len, int width, int ws, LgEndSegmentType est)
		{
			var seg = new MockSegment(len, width, ws, est);
			m_potentialSegs[new MockSegKey(ichMin, ichLim)] = seg;
			m_potentialSegsInOrder.Add(seg);
		}

		internal void FailOnPartialLine(int ichMin, int ichLim)
		{
			m_failOnPartialLine.Add(new MockSegKey(ichMin, ichLim));
		}

		Dictionary<MockSegKey, MockSegment> m_potentialSegs = new Dictionary<MockSegKey, MockSegment>();
		HashSet<MockSegKey> m_failOnPartialLine = new HashSet<MockSegKey>();
		internal List<MockSegment> m_potentialSegsInOrder = new List<MockSegment>();
	}
}
