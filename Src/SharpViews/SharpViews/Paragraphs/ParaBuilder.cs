using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;

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
		private readonly List<RenderRun> m_renderRuns; // the content of the paragraph
		private int m_renderRunIndex; // index of the run we are in the process of adding or about to add.
		private int m_ichRendered; // index of the next character that needs to be added.
		private int m_ichLim; // end of the whole paragraph (in rendered characters)
		private int m_lastRenderRunIndex; // last render run in the whole paragraph.
		private RenderRun m_currentRenderRun;
		private ParaLine m_currentLine;
		private int m_spaceLeftOnCurrentLine;
		// During relayout, these are the lines we might still reuse; their starting character indexes are not yet adjusted.
		private List<ParaLine> m_reuseableLines;
		private int m_gapTop;
		private int m_gapLeft;
		private int m_surroundWidth;
		private int m_surroundHeight;

		public ParaBuilder(ParaBox para, LayoutInfo layoutInfo)
		{
			m_para = para;
			m_layoutInfo = layoutInfo;
			m_renderRuns = para.Source.RenderRuns;
			m_gapTop = m_para.GapTop(layoutInfo);
			m_gapLeft = m_para.GapLeading(layoutInfo); // Todo RTL.
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
				RenderRun last = m_renderRuns[m_renderRuns.Count - 1];
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
			return lastLine.Top + lastLine.Height - m_gapTop;
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
			m_spaceLeftOnCurrentLine = m_layoutInfo.MaxWidth - m_surroundWidth; // todo: adjust by first line indent.
			if (m_lines.Count == 0)
				m_currentLine.Top = m_gapTop;
			m_lines.Add(m_currentLine);
			while (!Finished)
				if (!AddSomethingToLine())
					break;
			if (m_lines.Count > 1)
			{
				ParaLine previous = m_lines[m_lines.Count - 2];
				previous.LastBox.Next = m_currentLine.FirstBox;
				m_currentLine.Top = TopOfNextLine(previous, m_currentLine);
			}

			m_currentLine.ArrangeBoxes(m_gapLeft);
		}

		int TopOfNextLine(ParaLine previous, ParaLine next)
		{
			int lineHeight = m_layoutInfo.MpToPixelsY(m_para.Style.LineHeight);
			// where the top has to be if we go by the LineHeight.
			int topNext = previous.Top + previous.Ascent + Math.Abs(lineHeight) - next.Ascent;
			if (lineHeight >= 0)
				return Math.Max(topNext, previous.Bottom); // don't allow overlap unless doing exact layout.
			return topNext;
		}

		/// <summary>
		/// Return the writing system of the character at the specified offset in the source.
		/// </summary>
		int WsInSource(int ich, IVwTextSource source)
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
		/// <returns></returns>
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
				m_currentLine.Add(boxToAdd);
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
				int ichRunLim = m_currentRenderRun.RenderStart + m_currentRenderRun.RenderLength;
				int lastRunIndex = m_renderRunIndex;
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
				ILgSegment seg;
				int dichLim, dxWidth;
				LgEndSegmentType est;
				bool mustGetSomeSeg = m_currentLine.FirstBox == null;
				renderer.FindBreakPoint(m_layoutInfo.VwGraphics, m_para.Source, null, m_ichRendered,
					ichRunLim, ichRunLim, false, mustGetSomeSeg, m_spaceLeftOnCurrentLine, (mustGetSomeSeg ? LgLineBreak.klbClipBreak : LgLineBreak.klbWordBreak),
										m_currentLine.FirstBox == null ? LgLineBreak.klbClipBreak : LgLineBreak.klbWordBreak, LgTrailingWsHandling.ktwshAll,
										false, out seg, out dichLim, out dxWidth, out est, null);
				switch (est)
				{
					case LgEndSegmentType.kestNoMore:
					case LgEndSegmentType.kestOkayBreak:
					case LgEndSegmentType.kestMoreLines:
					case LgEndSegmentType.kestWsBreak:
						boxToAdd = new StringBox(m_para.Style, seg, m_ichRendered);
						boxToAdd.Layout(m_layoutInfo);
						m_spaceLeftOnCurrentLine -= boxToAdd.Width;
						m_ichRendered += dichLim;
						boxToAdd.Container = m_para;
						m_currentLine.Add(boxToAdd);
						while (m_renderRunIndex < m_renderRuns.Count && m_ichRendered >= m_renderRuns[m_renderRunIndex].RenderLim)
							m_renderRunIndex++; // done all of that run, move on.
						// We want to return true if more could be put on this line.
						// Of the cases that take this branch, NoMore means no more input at all, so we can't add any more;
						// MoreLines means this line is full. So for both of those we return false. OkayBreak allows us to try
						// to put more segments on this line, as does ws break.
						// If we get NoMore, we should also check, because it might really be a ws break at the end
						// of the run.
						if (est == LgEndSegmentType.kestNoMore && ichRunLim < m_para.Source.Length && ichRunLim > 0
							&& WsInSource(ichRunLim -1, m_para.Source) != WsInSource(ichRunLim, m_para.Source))
						{
							// The renderer failed to detect it because not told to look further in the source,
							// but there is in fact a writing system break.
							est = LgEndSegmentType.kestWsBreak;
						}
						return est == LgEndSegmentType.kestOkayBreak || est == LgEndSegmentType.kestWsBreak;
					case LgEndSegmentType.kestNothingFit:
						Debug.Assert(m_currentLine.FirstBox != null, "Making segment must not return kestNothingFit if line contains nothing");
						return false;
					default:
						Debug.Assert(false);
						break;
				}
			}
			return true;
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
			RenderRun last = m_renderRuns[m_renderRuns.Count - 1];
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
							top = TopOfNextLine(previous, nextLine);
						}

						m_lines.AddRange(m_reuseableLines);
						if (top != nextLine.Top)
						{
							ParaLine previous = null;
							foreach (var line in m_reuseableLines)
							{
								if (previous != null) // first time top has already been computed
									top = TopOfNextLine(previous, line);
								line.Top = top; // BEFORE ArrangeBoxes, since it gets copied to the individual boxes
								line.ArrangeBoxes(m_gapLeft);
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