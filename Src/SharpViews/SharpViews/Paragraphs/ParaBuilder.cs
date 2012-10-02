using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.Paragraphs
{
	/// <summary>
	/// This class is responsible for laying out the components of a paragraph. It may do a complete layout, or redo the layout of a previously
	/// laid out paragraph that is being adjusted, typically because its contents have changed.
	///
	/// Its primary organization is a process of assembling lines by adding stuff to them until they are full or there is nothing more to add.
	///
	/// Initially, and after each line has been successfully added, its state may be thought of as follows:
	/// 1. We are trying to build a box sequence which will be stored in m_para.FirstBox. A chain of boxes built so far begins at m_firstBox
	/// (which may initially be null) and continues to m_lastBox. These boxes are organized into lines, stored in m_lines. They correspond
	/// to the text from the RenderRuns before m_renderRunIndex, plus the text from RenderRuns[m_renderRunIndex] == m_currentRenderRun, up to m_ichRendered.
	///
	/// 2. There may also be a group of lines at the end of the paragraph which we hope to be able to reuse (though they will currently be relative to
	/// another, old, TextSource). These lines are stored in m_oldLines, and we have deduced that they correspond to the text in our current source
	/// from m_ichStartReusing onwards. If, after completing a line, we find that m_ichRendered == m_ichStartReusing, then we have resynchronized
	/// and can reuse the old lines, after adjusting their source and source offset.
	///
	/// 3. Otherwise, we have to build a new line, starting with the material at m_ichRendered. Before doing this, if m_ichStartReusing > m_ichRendered,
	/// we must discard one (or more) lines of m_oldLines, since we are already past its position.
	///
	/// The process of constructing a line is basically to add boxes and text runs (which become string boxes) to the line until it is full. At that point,
	/// it may be determined that the current end of the line is not a valid place to break the line. This typically comes about because the last
	/// thing on the line is a text box which terminated at a writing system change rather than at a point where the RenderEngine said we could break.
	/// When this happens we must backtrack, replacing the last (string) box with a shortened version ending at a good break, or removing it altogether
	/// if it is not the first box on the line. (We must always put something on a line, or we get an infinite loop.)
	/// </summary>
	class ParaBuilder
	{
		private readonly ParaBox m_para; // that we are laying out
		private readonly LayoutInfo m_layoutInfo; // the parameters under which we are laying out
		private List<ParaLine> m_lines; // that we are assembling; Todo: what about partial layout?
		private readonly List<IRenderRun> m_renderRuns; // the content of the paragraph
		private int m_renderRunIndex; // index of the run we are in the process of adding or about to add.
		private int m_ichRendered; // index of the next character that needs to be added.
		private int m_ichLim; // end of the whole paragraph (in rendered characters)
		private int m_lastRenderRunIndex; // last render run in the whole paragraph.
		private IRenderRun m_currentRenderRun;
		private ParaLine m_currentLine;
		private int m_spaceLeftOnCurrentLine;
		// During relayout, these are the lines we might still reuse; their starting character indexes are not yet adjusted.
		private List<ParaLine> m_reuseableLines;
		private int m_gapTop;
		private int m_gapLeft;
		private int m_gapRight;
		private int m_surroundWidth;
		private int m_surroundHeight;
		private bool m_nextUpstreamSegWsOnly; // toggle for whether to try whitespace-only or no-ws for next upstream seg.

		public ParaBuilder(ParaBox para, LayoutInfo layoutInfo)
		{
			m_para = para;
			m_layoutInfo = layoutInfo;
			m_renderRuns = para.Source.RenderRuns;
			m_gapTop = m_para.GapTop(layoutInfo);
			m_gapLeft = m_para.GapLeading(layoutInfo); // Todo RTL.
			m_gapRight = m_para.GapTrailing(layoutInfo);
			m_surroundWidth = m_para.SurroundWidth(layoutInfo);
			m_surroundHeight = m_para.SurroundHeight(layoutInfo);
		}

		internal void FullLayout()
		{
			m_lines = new List<ParaLine>();
			m_renderRunIndex = 0;
			m_ichRendered = 0;
			m_lastRenderRunIndex = m_renderRuns.Count;
			if (m_renderRuns.Count != 0)
			{
				IRenderRun last = m_renderRuns[m_renderRuns.Count - 1];
				m_ichLim = last.RenderStart + last.RenderLength;
				while (!Finished)
				{
					BuildALine();
				}
			}
			else
			{
				m_ichLim = 0;
			}
			SetParaInfo();
		}

		private void SetParaInfo()
		{
			m_para.SetParaInfo(m_lines, ComputeWidth() + m_surroundWidth, ComputeHeight() + m_surroundHeight);
		}

		/// <summary>
		/// Compute the width of the box, not counting surrounding gaps, once its lines have been laid out.
		/// </summary>
		private int ComputeWidth()
		{
			if (m_lines.Count == 1)
				return m_lines[0].Width;
			if (m_lines.Count == 0)
				return 0;

			return Math.Max(m_layoutInfo.MaxWidth - m_surroundWidth, m_lines.Max(line => line.Width));
		}

		/// <summary>
		/// Compute the height of the box, not counting surrounding gaps, once its lines have been laid out.
		/// </summary>
		int ComputeHeight()
		{
			if (m_lines.Count == 0)
				return 0;
			ParaLine lastLine = m_lines[m_lines.Count - 1];
			var chrp = m_para.Style.Chrp;
			m_layoutInfo.VwGraphics.SetupGraphics(ref chrp);
			int bottomOfPara = TopOfNextLine(lastLine, m_layoutInfo.VwGraphics.FontAscent);
			return bottomOfPara - m_gapTop;
		}

		private bool Finished
		{
			get
			{
				return m_ichRendered == m_ichLim && m_renderRunIndex == m_lastRenderRunIndex;
			}
		}

		private void BuildALine()
		{
			m_currentLine = new ParaLine();
			m_spaceLeftOnCurrentLine = m_layoutInfo.MaxWidth - m_surroundWidth;

			if (m_lines.Count == 0)
			{
				m_currentLine.Top = m_gapTop;
				m_spaceLeftOnCurrentLine -= m_layoutInfo.MpToPixelsY(m_para.Style.FirstLineIndent);
			}
			m_lines.Add(m_currentLine);
			m_lineSegTypes.Clear();
			while (!Finished)
				if (!AddSomethingToLine())
					break;
			while (!FinalizeLine())
				if (!Backtrack())
					break;
			if (m_lines.Count > 1)
			{
				ParaLine previous = m_lines[m_lines.Count - 2];
				previous.LastBox.Next = m_currentLine.FirstBox;
				m_currentLine.Top = TopOfNextLine(previous, m_currentLine.Ascent);
			}
			m_currentLine.ArrangeBoxes(m_para.Style.ParaAlignment, m_gapLeft, m_gapRight,
									   m_lines.Count == 1 ? m_layoutInfo.MpToPixelsY(m_para.Style.FirstLineIndent) : 0,
									   m_layoutInfo.MaxWidth,
									   TopDepth);
		}

		/// <summary>
		/// Shorten the current line. Currently this is only called when the last thing on the line
		/// is a string box. If possible, replace its segment with a shorter one. If this is not possible,
		/// and it is not the first thing on the line, remove it altogether. If it IS the first thing on
		/// the line, do nothing. We'll live with the bad break, because there is nothing better we know
		/// how to do.
		/// Returns true if successful, which means FinalizeLine should be called again. False if
		/// unsuccessful, to prevent infinite loop.
		/// </summary>
		private bool Backtrack()
		{
			var lastBox = m_lines.Last().LastBox as StringBox;
			while (lastBox != null)
			{
				// We want to call FindLineBreak again with the same arguments we used to create the segment
				// of lastBox, except that ichLimBacktrack should prevent including the last character.
				// (We expect that is is NOT a white-space-only segment, since we were not able to break after it.)
				int ichMin = lastBox.IchMin;
				int ichLim = ichMin + lastBox.Segment.get_Lim(ichMin);
				//int dxWidthLast = lastBox.Segment.get_Width(ichMin, m_layoutInfo.VwGraphics);
				int ichLimBacktrack = ichLim - 1;
				// skip back one if we're splitting a surrogate.
				if (ichLimBacktrack > 0)
				{
					string charsAtLim = Fetch(ichLimBacktrack - 1, ichLimBacktrack + 1);
					if (Surrogates.IsTrailSurrogate(charsAtLim[1]) && Surrogates.IsLeadSurrogate(charsAtLim[0]))
						ichLimBacktrack--;
				}
				int runIndex = m_renderRunIndex; // what would have beem m_renderRunIndex when making lastBox's segment.
				while (runIndex > 0 && ichMin < m_renderRuns[runIndex].RenderStart)
					runIndex--;
				var renderer = m_layoutInfo.GetRenderer(m_renderRuns[runIndex].Ws);
				int ichRunLim = GetLimitOfRunWithSameRenderer(renderer, runIndex);
				ILgSegment seg;
				int dichLim, dxWidth;
				LgEndSegmentType est;
				bool mustGetSomeSeg = m_currentLine.FirstBox == lastBox;
				var twsh = LgTrailingWsHandling.ktwshAll;
				bool runRtl = m_layoutInfo.RendererFactory.RightToLeft(m_renderRuns[runIndex].Ws);
				bool paraRtl = m_para.Style.RightToLeft;
				if (runRtl != paraRtl)
					twsh = LgTrailingWsHandling.ktwshNoWs;
				var spaceLeftOnCurrentLine = m_spaceLeftOnCurrentLine + lastBox.Width;

				renderer.FindBreakPoint(m_layoutInfo.VwGraphics, m_para.Source, null, ichMin,
										ichRunLim, ichLimBacktrack, false, mustGetSomeSeg, spaceLeftOnCurrentLine,
										LgLineBreak.klbWordBreak,
										mustGetSomeSeg ? LgLineBreak.klbClipBreak : LgLineBreak.klbWordBreak, twsh,
										false, out seg, out dichLim, out dxWidth, out est, null);
				if (seg == null)
				{
					// can't make any shorter segment here.
					// Can we backtrack further by getting rid of this box altogether?
					Box boxBefore = m_lines.Last().BoxBefore(lastBox);
					if (boxBefore is StringBox)
					{
						lastBox = (StringBox)boxBefore;
						continue;
					}
					// Currently backtracking should not need to remove a non-string box, because we always allow
					// breaking after them. So if boxBefore is not a string box, we can break after it.
					m_currentLine.RemoveFrom(lastBox);
					m_ichRendered = ichMin;
					m_spaceLeftOnCurrentLine = spaceLeftOnCurrentLine;
					m_renderRunIndex = AdvanceRenderRunIndexToIch(m_ichRendered, m_renderRunIndex);
					return true;
				}

				m_currentLine.RemoveFrom(lastBox);
				AddBoxToLine(seg, ichMin, dichLim, est, spaceLeftOnCurrentLine);
				ILgSegment whiteSpaceSeg;
				if (twsh == LgTrailingWsHandling.ktwshNoWs)
				{
					// Add a white space segment if possible.
					renderer.FindBreakPoint(m_layoutInfo.VwGraphics, m_para.Source, null, m_ichRendered,
						ichRunLim, ichRunLim, false, true, m_spaceLeftOnCurrentLine, LgLineBreak.klbWordBreak,
											LgLineBreak.klbWordBreak, LgTrailingWsHandling.ktwshOnlyWs,
										false, out whiteSpaceSeg, out dichLim, out dxWidth, out est, null);
					if (seg != null)
						AddBoxToLine(whiteSpaceSeg, m_ichRendered, dichLim, est, m_spaceLeftOnCurrentLine);
				}
				m_renderRunIndex = AdvanceRenderRunIndexToIch(m_ichRendered, m_renderRunIndex);
				return true;
			}
			return false; // no better break available, use the line as it is.
		}

		private void AddBoxToLine(ILgSegment seg, int ichMin, int dichLim, LgEndSegmentType est, int spaceLeftOnCurrentLine)
		{
			var boxToAdd = new StringBox(m_para.Style, seg, ichMin);
			boxToAdd.Layout(m_layoutInfo);
			m_spaceLeftOnCurrentLine = spaceLeftOnCurrentLine - boxToAdd.Width;
			m_ichRendered = ichMin + dichLim;
			boxToAdd.Container = m_para;
			AddBoxToLine(boxToAdd, est);
		}

		/// <summary>
		/// This is called when we have put all we can on the current line. Sometimes we may have put too much!
		/// If so, return false, to indicate we can't finalize a line in this state, and trigger backtracking.
		/// </summary>
		private bool FinalizeLine()
		{
			Debug.Assert(m_lines.Count > 0);
			Debug.Assert(m_lines.Last().Boxes.Count() > 0);
			var lastBox = m_lines.Last().Boxes.Last() as StringBox;
			if (lastBox == null)
				return true; // for now it's always valid to break after a non-string box.
			var est = m_lineSegTypes.Last();
			// If we know that's a bad break, backtrack.
			if (est == LgEndSegmentType.kestBadBreak)
				return false;
			if (est != LgEndSegmentType.kestWsBreak)
				return true; // all other kinds of break we accept.
			// For a writing-system break, we must try to figure out whether we can break here.
			int ichMin = lastBox.IchMin;
			int length = lastBox.Segment.get_Lim(ichMin);
			int ichLast = ichMin + length - 1;
			// Enhance JohnT: MAYBE we should check for surrogate? But new surrogate pairs we can break after are unlikely.
			if (ichLast < 0)
				return false; // paranoia
			string lastChar = Fetch(ichLast, ichLast + 1);
			var cpe = LgIcuCharPropEngineClass.Create();
			byte lbp;
			using (var ptr = new ArrayPtr(1))
			{
				cpe.GetLineBreakProps(lastChar, 1, ptr);
				lbp = Marshal.ReadByte(ptr.IntPtr);
			}
			lbp &= 0x1f; // strip 'is it a space' high bit
			// If it's a space (or other character which provides a break opportunity after),
			// go ahead and break. Otherwise treat as bad break.
			if (lbp != (byte)LgLBP.klbpSP && lbp != (byte)LgLBP.klbpBA && lbp != (byte)LgLBP.klbpB2)
				return false; // can't break here, must backtrack
			return true; // stick with the break we have.
		}

		/// <summary>
		/// Get the specified range of (rendered) characters from the text source.
		/// </summary>
		string Fetch(int ichMin, int ichLim)
		{
			using (ArrayPtr ptr = new ArrayPtr(ichLim - ichMin))
			{
				m_para.Source.Fetch(ichMin, ichLim, ptr.IntPtr);
				return MarshalEx.NativeToString(ptr, ichLim - ichMin, true);
			}

		}

		int TopDepth
		{
			get { return m_para.Style.RightToLeft ? 1 : 0; }
		}

		int TopOfNextLine(ParaLine previous, int nextAscent)
		{
			int lineHeight = m_layoutInfo.MpToPixelsY(m_para.Style.LineHeight);
			// where the top has to be if we go by the LineHeight.
			int topNext = previous.Top + previous.Ascent + Math.Abs(lineHeight) - nextAscent;
			if (lineHeight >= 0)
				return Math.Max(topNext, previous.Bottom); // don't allow overlap unless doing exact layout.
			return topNext;
		}

		/// <summary>
		/// Return the writing system of the character at the specified offset in the source.
		/// </summary>
		static int WsInSource(int ich, IVwTextSource source)
		{
			LgCharRenderProps chrp;
			int ichMin, ichLim;
			source.GetCharProps(ich, out chrp, out ichMin, out ichLim);
			return chrp.ws;
		}

		/// <summary>
		/// Add something to the line. Return true if we should keep trying to add more. (That is, all of the current thing fit,
		/// and there is still room left; this routine is not responsible to determine whether there IS anything else to add.)
		/// If there is not yet anything in the line, this routine MUST add something; otherwise, it is allowed to fail,
		/// returning false without changing anything.
		/// </summary>
		private bool AddSomethingToLine()
		{
			m_currentRenderRun = m_renderRuns[m_renderRunIndex];
			Box boxToAdd = m_currentRenderRun.Box;
			if (boxToAdd != null)
			{
				// The current run works out to a single box; add it if it fits. Add it anyway if the line
				// is currently empty.
				boxToAdd.Layout(m_layoutInfo);
				if (m_currentLine.FirstBox != null && boxToAdd.Width > m_spaceLeftOnCurrentLine)
					return false;
				m_spaceLeftOnCurrentLine -= boxToAdd.Width;
				AddBoxToLine(boxToAdd, LgEndSegmentType.kestOkayBreak); // always OK to break after non-string.
				boxToAdd.Container = m_para;
				m_ichRendered = m_currentRenderRun.RenderLim;
				m_renderRunIndex++;
			}
			else
			{
				// current run is not a simple box. Make a text box out of part or all of it, or possibly also subsequent
				// runs that are not boxes and use the same renderer.
				IRenderEngine renderer = m_layoutInfo.GetRenderer(m_currentRenderRun.Ws);
				// If our text source doesn't yet know about the writing system factory, make sure it does.
				if (m_para.Source.GetWsFactory() == null)
					m_para.Source.SetWsFactory(renderer.WritingSystemFactory);
				int ichRunLim = GetLimitOfRunWithSameRenderer(renderer, m_renderRunIndex);
				ILgSegment seg;
				int dichLim, dxWidth;
				LgEndSegmentType est;
				bool mustGetSomeSeg = m_currentLine.FirstBox == null;
				var twsh = GetNextTwsh();

				renderer.FindBreakPoint(m_layoutInfo.VwGraphics, m_para.Source, null, m_ichRendered,
					ichRunLim, ichRunLim, false, mustGetSomeSeg, m_spaceLeftOnCurrentLine, (mustGetSomeSeg ? LgLineBreak.klbClipBreak : LgLineBreak.klbWordBreak),
										m_currentLine.FirstBox == null ? LgLineBreak.klbClipBreak : LgLineBreak.klbWordBreak, twsh,
										false, out seg, out dichLim, out dxWidth, out est, null);
				if (est == LgEndSegmentType.kestNothingFit && twsh != LgTrailingWsHandling.ktwshAll)
				{
					// Nothing of the one we were trying for, try for the other.
					twsh = GetNextTwsh();
					renderer.FindBreakPoint(m_layoutInfo.VwGraphics, m_para.Source, null, m_ichRendered,
						ichRunLim, ichRunLim, false, mustGetSomeSeg, m_spaceLeftOnCurrentLine, (mustGetSomeSeg ? LgLineBreak.klbClipBreak : LgLineBreak.klbWordBreak),
											m_currentLine.FirstBox == null ? LgLineBreak.klbClipBreak : LgLineBreak.klbWordBreak, twsh,
										false, out seg, out dichLim, out dxWidth, out est, null);
				}
				switch (est)
				{
					case LgEndSegmentType.kestNoMore:
					case LgEndSegmentType.kestOkayBreak:
					case LgEndSegmentType.kestMoreLines:
					case LgEndSegmentType.kestWsBreak:
					case LgEndSegmentType.kestMoreWhtsp:
						boxToAdd = new StringBox(m_para.Style, seg, m_ichRendered);
						boxToAdd.Layout(m_layoutInfo);
						m_spaceLeftOnCurrentLine -= boxToAdd.Width;
						m_ichRendered += dichLim;
						boxToAdd.Container = m_para;
						// If we get NoMore, we should also check, because it might really be a ws break at the end
						// of the run.
						if (est == LgEndSegmentType.kestNoMore && ichRunLim < m_para.Source.Length && ichRunLim > 0
							&& WsInSource(ichRunLim - 1, m_para.Source) != WsInSource(ichRunLim, m_para.Source))
						{
							// The renderer failed to detect it because not told to look further in the source,
							// but there is in fact a writing system break.
							// However, if the next character is a box, we want to treat it as a definitely good break.
							if (m_para.Source.IsThereABoxAt(ichRunLim))
								est = LgEndSegmentType.kestOkayBreak;
							else
								est = LgEndSegmentType.kestWsBreak;
						}
						AddBoxToLine(boxToAdd, est);
						m_renderRunIndex = AdvanceRenderRunIndexToIch(m_ichRendered, m_renderRunIndex);
						// We want to return true if more could be put on this line.
						// Of the cases that take this branch, NoMore means no more input at all, so we can't add any more;
						// MoreLines means this line is full. So for both of those we return false. OkayBreak allows us to try
						// to put more segments on this line, as does ws break, and moreWhtsp.
						return est == LgEndSegmentType.kestOkayBreak || est == LgEndSegmentType.kestWsBreak || est == LgEndSegmentType.kestMoreWhtsp;
					case LgEndSegmentType.kestNothingFit:
						Debug.Assert(m_currentLine.FirstBox != null, "Making segment must not return kestNothingFit if line contains nothing");
						return false;
					//case LgEndSegmentType.kestHardBreak:
					//    if()
					//    return true;
					default:
						Debug.Assert(false);
						break;
				}
			}
			return true;
		}

		private int GetLimitOfRunWithSameRenderer(IRenderEngine renderer, int startIndex)
		{
			var startRun = m_renderRuns[startIndex];
			int ichRunLim = startRun.RenderStart + startRun.RenderLength;
			int lastRunIndex = startIndex;
			while (lastRunIndex < m_renderRuns.Count - 1)
			{
				var anotherRun = m_renderRuns[lastRunIndex + 1];
				if (anotherRun.Box != null)
					break; // got to a box, can't merge.
				if (m_layoutInfo.GetRenderer(anotherRun.Ws) != renderer)
					break; // treat separately, uses another rendering engine.
				ichRunLim = anotherRun.RenderLim;
				lastRunIndex++;
			}
			return ichRunLim;
		}

		int AdvanceRenderRunIndexToIch(int ich, int startIndex)
		{
			int index = startIndex;
			while (index < m_renderRuns.Count && ich >= m_renderRuns[index].RenderLim)
				index++;
			return index;
		}

		private List<LgEndSegmentType> m_lineSegTypes = new List<LgEndSegmentType>();

		private void AddBoxToLine(Box boxToAdd, LgEndSegmentType est)
		{
			m_currentLine.Add(boxToAdd);
			m_lineSegTypes.Add(est);
		}

		private LgTrailingWsHandling GetNextTwsh()
		{
			var twsh = LgTrailingWsHandling.ktwshAll;
			bool runRtl = m_layoutInfo.RendererFactory.RightToLeft(m_currentRenderRun.Ws);
			bool paraRtl = m_para.Style.RightToLeft;
			if (runRtl != paraRtl)
			{
				twsh = m_nextUpstreamSegWsOnly ? LgTrailingWsHandling.ktwshOnlyWs : LgTrailingWsHandling.ktwshNoWs;
				m_nextUpstreamSegWsOnly = !m_nextUpstreamSegWsOnly;
			}
			return twsh;
		}

		/// <summary>
		/// Redo layout. Should produce the same segments as FullLayout, but assume that segments for text up to
		/// details.StartChange may be reused (if not affected by changing line breaks), and segments after
		/// details.StartChange+details.DeleteCount may be re-used if a line break works out (and after adjusting
		/// their begin offset).
		/// </summary>
		/// <param name="details"></param>
		internal void Relayout(SourceChangeDetails details, LayoutCallbacks lcb)
		{
			m_reuseableLines = m_para.Lines;
			m_lines = new List<ParaLine>();
			m_renderRunIndex = 0;
			m_ichRendered = 0;
			IRenderRun last = m_renderRuns[m_renderRuns.Count - 1];
			m_ichLim = last.RenderStart + last.RenderLength;
			m_lastRenderRunIndex = m_renderRuns.Count;
			Rectangle invalidateRect = m_para.InvalidateRect;
			int delta = details.InsertCount - details.DeleteCount;
			int oldHeight = m_para.Height;
			int oldWidth = m_para.Width;
			// Make use of details.StartChange to reuse some lines at start.
			if (m_reuseableLines.Count > 0)
			{
				// As long as we have two complete lines before the change, we can certainly reuse the first of them.
				while (m_reuseableLines.Count > 2 && details.StartChange > m_reuseableLines[2].IchMin)
				{
					m_lines.Add(m_reuseableLines[0]);
					m_reuseableLines.RemoveAt(0);
				}
				// If we still have one complete line before the change, we can reuse it provided there is white
				// space after the end of the line and before the change.
				if (m_reuseableLines.Count > 1)
				{
					int startNextLine = m_reuseableLines[1].IchMin;
					if (details.StartChange > startNextLine)
					{
						bool fGotWhite = false;
						string line1Text = m_reuseableLines[1].CheckedText;
						int lim = details.StartChange - startNextLine;
						var cpe = LgIcuCharPropEngineClass.Create();
						for (int ich = 0; ich < lim; ich++)
						{
							// Enhance JohnT: possibly we need to consider surrogates here?
							// Worst case is we don't reuse a line we could have, since a surrogate won't
							// be considered white.
							if (cpe.get_IsSeparator(Convert.ToInt32(line1Text[ich])))
							{
								fGotWhite = true;
								break;
							}
						}
						if (fGotWhite)
						{
							m_lines.Add(m_reuseableLines[0]);
							m_reuseableLines.RemoveAt(0);
						}
					}
				}
				m_ichRendered = m_reuseableLines[0].IchMin;
				int topOfFirstDiscardedLine = m_reuseableLines[0].Top;
				// We don't need to invalidate the lines we're keeping.
				invalidateRect = new Rectangle(invalidateRect.Left, invalidateRect.Top + topOfFirstDiscardedLine,
											   invalidateRect.Width, invalidateRect.Height - topOfFirstDiscardedLine);
			}

			// Figure out which run we need to continue from, to correspond to the start of the first line
			// we need to rebuild.
			while (m_renderRunIndex < m_renderRuns.Count && m_renderRuns[m_renderRunIndex].RenderLim <= m_ichRendered)
				m_renderRunIndex++;

			while (!Finished)
			{
				// Todo: I think we need to adjust available width if this is the first line.
				BuildALine();
				// Drop any initial reusable lines we now determine to be unuseable after all.
				// If we've used characters beyond the start of this potentially reusable line, we can't reuse it.
				// Also, we don't reuse empty lines. Typically an empty line is left over from a previously empty
				// paragraph, and we no longer need the empty segment, even though it doesn't have any of the same
				// characters (since it has none) as the segment that has replaced it.
				while (m_reuseableLines.Count > 0 && (m_ichRendered > m_reuseableLines[0].IchMin + delta || m_reuseableLines[0].Length == 0))
				{
					m_reuseableLines.RemoveAt(0);
				}
				if (m_reuseableLines.Count > 0)
				{
					// See if we can resync.
					var nextLine = m_reuseableLines[0];
					if (m_ichRendered == nextLine.IchMin + delta)
					{
						// reuse it.
						int top = m_gapTop;
						if (m_lines.Count > 0)
						{
							ParaLine previous = m_lines.Last();
							previous.LastBox.Next = nextLine.FirstBox;
							top = TopOfNextLine(previous, nextLine.Ascent);
						}

						m_lines.AddRange(m_reuseableLines);
						if (top != nextLine.Top)
						{
							ParaLine previous = null;
							foreach (var line in m_reuseableLines)
							{
								if (previous != null) // first time top has already been computed
									top = TopOfNextLine(previous, line.Ascent);
								line.Top = top; // BEFORE ArrangeBoxes, since it gets copied to the individual boxes
								m_currentLine.ArrangeBoxes(m_para.Style.ParaAlignment, m_gapLeft, m_gapRight, 0, m_layoutInfo.MaxWidth, TopDepth);
								previous = line;
							}
						}
						else
						{
							// reusable lines have not moved, we don't need to invalidate them.
							invalidateRect.Height -= (m_reuseableLines.Last().Bottom - top);
						}
						for (Box box = nextLine.FirstBox; box != null; box = box.Next)
						{
							if (box is StringBox)
								(box as StringBox).IchMin += delta;
						}

						break;
					}
				}

			}
			SetParaInfo();
			// if the paragraph got larger, we need to invalidate the extra area.
			// (But, don't reduce it if it got smaller; we want to invalidate all the old stuff as well as all the new.)
			if (m_para.Height > oldHeight)
				invalidateRect.Height += m_para.Height - oldHeight;
			if (m_para.Width > oldWidth)
				invalidateRect.Width += m_para.Width - oldWidth;
			lcb.InvalidateInRoot(invalidateRect);
		}
	}
}
