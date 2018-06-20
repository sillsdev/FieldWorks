// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// This class is responsible for the testable business logic of the constituent chart.
	/// Enhance: some or all of this logic could possibly be moved to suitable methods of DsConstChart itself.
	/// Since nothing else wants it yet, it is easier to keep all the logic related to the chart with the
	/// UI, but carefully separated (i.e., into this class).
	/// </summary>
	public class ConstituentChartLogic
	{
		private IDsConstChart m_chart;
		private ChartLocation m_lastMoveCell; // row and column of last Move operation
		private IHelpTopicProvider m_helpTopicProvider;

		#region Factories and Repositories

		private readonly IStTextRepository m_textRepo;
		private readonly ISegmentRepository m_segRepo;
		private readonly IConstChartRowFactory m_rowFact;
		private readonly IConstChartRowRepository m_rowRepo;
		private readonly IConstituentChartCellPartRepository m_cellPartRepo;
		private readonly IConstChartWordGroupRepository m_wordGrpRepo;
		private readonly IConstChartWordGroupFactory m_wordGrpFact;
		private readonly IConstChartMovedTextMarkerRepository m_movedTextRepo;
		private readonly IConstChartMovedTextMarkerFactory m_movedTextFact;
		private readonly IConstChartClauseMarkerRepository m_clauseMkrRepo;
		private readonly IConstChartClauseMarkerFactory m_clauseMkrFact;
		private readonly IConstChartTagFactory m_chartTagFact;
		private readonly ICmPossibilityRepository m_possRepo;

		#endregion

		private ICmPossibility[] m_allMyColumns;
		private int[] m_currHighlightCells; // Keeps track of highlighted cells when dealing with ChartOrphan insertion.

		/// <summary>
		/// Number of columns we display in addition to the ones configured in the template.
		/// Currently these are the row number and Notes columns.
		/// </summary>
		public const int NumberOfExtraColumns = 2;
		/// <summary>
		/// The index of the first column that comes from the template. This many of the extra
		/// columns come first (currently the row number) while the rest come after the template
		/// columns.
		/// </summary>
		public const int indexOfFirstTemplateColumn = 1;

		public event RowModifiedEventHandler RowModifiedEvent;

		public event EventHandler Ribbon_Changed;

		public ConstituentChartLogic(LcmCache cache, IDsConstChart chart, int hvoStText)
			: this(cache)
		{
			StTextHvo = hvoStText;
			m_chart = chart;
		}

		/// <summary>
		/// Make one and set the other stuff later.
		/// </summary>
		public ConstituentChartLogic(LcmCache cache)
		{
			Cache = cache;
			// Setup Factories and Repositories
			var servLoc		= Cache.ServiceLocator;
			m_textRepo		= servLoc.GetInstance<IStTextRepository>();
			m_segRepo		= servLoc.GetInstance<ISegmentRepository>();
			m_rowRepo		= servLoc.GetInstance<IConstChartRowRepository>();
			m_rowFact		= servLoc.GetInstance<IConstChartRowFactory>();
			m_wordGrpRepo	= servLoc.GetInstance<IConstChartWordGroupRepository>();
			m_wordGrpFact	= servLoc.GetInstance<IConstChartWordGroupFactory>();
			m_movedTextRepo = servLoc.GetInstance<IConstChartMovedTextMarkerRepository>();
			m_movedTextFact = servLoc.GetInstance<IConstChartMovedTextMarkerFactory>();
			m_clauseMkrRepo = servLoc.GetInstance<IConstChartClauseMarkerRepository>();
			m_clauseMkrFact = servLoc.GetInstance<IConstChartClauseMarkerFactory>();
			m_chartTagFact	= servLoc.GetInstance<IConstChartTagFactory>();
			m_cellPartRepo	= servLoc.GetInstance<IConstituentChartCellPartRepository>();
			m_possRepo		= servLoc.GetInstance<ICmPossibilityRepository>();
		}

		public void Init(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
		}

		protected internal LcmCache Cache { get; protected set; }

		protected internal virtual bool ChartIsRtL
		{
			// virtual for testing
			get
			{
				if (Chart.BasedOnRA == null)
				{
					return false;
				}
				var defWs = Cache.ServiceLocator.WritingSystemManager.Get(Chart.BasedOnRA.MainWritingSystem);
				return defWs.RightToLeftScript;
			}
		}

		/// <summary>
		/// An array of 4 integers representing 4 indices; that of first row, first column, last row, and last column.
		/// </summary>
		internal int[] CurrHighlightCells
		{
			// Enhance GordonM: This really ought to use a pair of ChartLocation objects.
			get { return m_currHighlightCells ?? (m_currHighlightCells = new int[4] {-1, -1, -1, -1}); }
			set
			{
				var crowOld = 0;
				if (IsChOrphActive)
				{
					crowOld = 1;
					var irowOld = m_currHighlightCells[0];
					if (m_currHighlightCells[2] != irowOld)
					{
						crowOld++;
				}
				}
				m_currHighlightCells = value;
				if (StTextHvo != 0 && IsChOrphActive)
				{
					var crowNew = 1;
					var irowNew = m_currHighlightCells[0];
					if (m_currHighlightCells[2] != irowNew)
					{
						crowNew++;
				}
				}
			}
		}

		/// <summary>
		/// Returns true if CurrHighlightCells has been set (i.e. ChOrph input is set up and pending).
		/// </summary>
		internal bool IsChOrphActive => CurrHighlightCells[0] > -1;

		/// <summary>
		/// Repeat the most recent move (forward).
		/// </summary>
		public void RepeatLastMoveForward()
		{
			if (m_lastMoveCell != null && m_lastMoveCell.IsValidLocation)
			{
				MoveCellForward(m_lastMoveCell);
		}
		}

		/// <summary>
		/// Repeat the most recent move (back).
		/// </summary>
		public void RepeatLastMoveBack()
		{
			if (m_lastMoveCell != null && m_lastMoveCell.IsValidLocation)
			{
				MoveCellBack(m_lastMoveCell);
		}
		}

		public bool CanRepeatLastMove => m_lastMoveCell != null && m_lastMoveCell.IsValidLocation;

		public IDsConstChart Chart
		{
			get { return m_chart; }
			set
			{
				if (m_chart == null && value == null || (m_chart != null && m_chart.Equals(value)))
				{
					return; // no change.
				}
				m_chart = value;
				m_currHighlightCells = null; // otherwise we try to clear the old ones when ribbon changed event happens!
			}
		}

		public int StTextHvo { get; set; }

		/// <summary>
		/// Returns an array of all the columns(Hvos) for the template of the chart that this logic is initialized with.
		/// </summary>
		public ICmPossibility[] AllMyColumns
		{
			get
			{
				m_allMyColumns = AllColumns(m_chart.TemplateRA).ToArray();
				return m_allMyColumns;
			}
		}

		/// <summary>
		/// Returns an array of all the columns for the template of the chart that are the ends of column groups.
		/// </summary>
		public ISet<int> GroupEndIndices { get; set; }

		/// <summary>
		/// Return true if the specified column has automatic 'missing' markers.
		/// This both causes them to appear automatically in empty cells, and suppresses manually adding and
		/// removing them.
		///
		/// Enhance JohnT: this is an ugly way of determining which columns should do this,
		/// just barely adequate for V1. Don't know how we should decide it...possibly a subclass of
		/// CmPossibility for the column? Or maybe this technique would be OK with an external XML file
		/// to specify the column names that should have automatic missing markers?
		/// </summary>
		/// <param name="icol"></param>
		/// <returns></returns>
		internal bool ColumnHasAutoMissingMarkers(int icol)
		{
			var engLabel = AllMyColumns[icol].Name.get_String(Cache.WritingSystemFactory.GetWsFromStr("en")).Text;
			return (engLabel == "Subject" || engLabel == "Verb");
		}

		public IInterlinRibbon Ribbon { get; set; }

		/// <summary>
		/// Returns true if there is no more uncharted text.
		/// </summary>
		public bool IsChartComplete => (NextUnchartedInput(1).Length == 0);

		/// <summary>
		/// This routine raises the ribbon changed event. It should get run whenever there is a PropChanged
		/// on the Ribbon's OccurenceListId.
		/// </summary>
		internal void RaiseRibbonChgEvent()
		{
			Ribbon_Changed?.Invoke(this, new EventArgs()); // raise event that Ribbon has changed
		}

		/// <summary>
		/// Return the next wordforms that have not yet been added to the chart (up to maxContext of them).
		/// This overload assumes the current StText.
		/// </summary>
		public AnalysisOccurrence[] NextUnchartedInput(int maxContext)
		{
			return NextUnchartedInput(m_textRepo.GetObject(StTextHvo), maxContext);
		}

		/// <summary>
		/// Return the next wordforms that have not yet been added to the chart (up to maxContext of them).
		/// </summary>
		public AnalysisOccurrence[] NextUnchartedInput(IStText curText, int maxContext)
		{
			if (m_chart == null || curText.Hvo < 1)
			{
				return new AnalysisOccurrence[0];
			}
			// Try brute force first. If too slow, maybe we'll have to implement a chartingCache
			// that keeps track of all the Seg/Analysis index combinations and whether they're charted or not.
			var myParas = curText.ParagraphsOS;
			if (myParas == null || myParas.Count == 0)
			{
				return new AnalysisOccurrence[0];
			}

			var allSegments = new List<ISegment>();
			foreach (var myPara in myParas.Cast<IStTxtPara>().Where(myPara => myPara != null))
			{
				allSegments.AddRange(myPara.SegmentsOS);
			}

			// Get a list (in order) of all wordforms in this text.
			// But to save time, we'll just cache them as hvo and index tuples.
			var wordformRefsInThisText = new List<Tuple<int, int>>(); // T1:hvoSeg, T2: iAnalysis
			foreach (var seg in allSegments)
			{
				for (var i = 0; i < seg.AnalysesRS.Count; i++)
				{
					if (seg.AnalysesRS[i].HasWordform)
					{
						wordformRefsInThisText.Add(new Tuple<int, int>(seg.Hvo, i));
				}
			}
			}

			// Get a set of all AnalysisOccurrence objects currently in the chart.
			var chartedTargets = new HashSet<AnalysisOccurrence>();
			foreach (var cellPart in m_chart.RowsOS.SelectMany(row => row.CellsOS.OfType<IConstChartWordGroup>()))
			{
				chartedTargets.UnionWith(cellPart.GetOccurrences());
			}

			// Figure out which words are NOT charted
			foreach (var pointRef in chartedTargets.Select(point => new Tuple<int, int>(point.Segment.Hvo, point.Index))
				.Where(wordformRefsInThisText.Contains))
			{
				wordformRefsInThisText.Remove(pointRef);
			}
			var resultLength = Math.Min(wordformRefsInThisText.Count, maxContext);
			var result = new AnalysisOccurrence[resultLength];
			for (var i = 0; i < resultLength; i++)
			{
				result[i] = new AnalysisOccurrence(m_segRepo.GetObject(wordformRefsInThisText[i].Item1), wordformRefsInThisText[i].Item2);
			}
			return result;
		}

		#region Chart Orphan methods
		protected internal bool NextInputIsChOrph()
		{
			int dummy1, dummy2;
			return NextInputIsChOrph(out dummy1, out dummy2);
		}

		protected internal bool NextInputIsChOrph(out int iPara, out int offset)
		{
			iPara = -1;
			offset = -1;
			var nui = NextUnchartedInput(1);
			return nui.Length != 0 && IsChOrph(nui[0], out iPara, out offset);
		}

		protected bool IsChOrph(AnalysisOccurrence word)
		{
			int dummy1, dummy2;
			return IsChOrph(word, out dummy1, out dummy2);
		}

		/// <summary>
		/// Checks an AnalysisOccurrence (usually the next Ribbon item) to see if it is a Chart Orphan (ChOrph).
		/// Compares the paragraph (BeginObject) and offset (BeginOffset) with the last charted wordform
		/// to determine whether this one is an orphan from earlier in the chart.
		/// </summary>
		/// <param name="word">should be an IAnalysis with a wordform</param>
		/// <param name="iPara">index of StTxtPara for the wordform</param>
		/// <param name="offset">offset into paragraph for the wordform</param>
		/// <returns>true if this one belongs earlier in the chart, false if it could be added to the end of the chart</returns>
		protected bool IsChOrph(AnalysisOccurrence word, out int iPara, out int offset)
		{
			iPara = -1;
			offset = -1;
			if (word == null || !word.HasWordform)
			{
				throw new ArgumentOutOfRangeException(nameof(word), @"is either null or is punctuation!");
			}
			if (Chart == null || Chart.RowsOS.Count == 0)
			{
				return false; // No chart, therefore the whole text is ChOrphs! But that don't count.
			}

			// Get last charted wordform
			var lastWordform = GetLastChartedWordform();
			if (lastWordform == null)
			{
				return false; // This should mean the same as no charted text.
			}

			if (word.IsAfter(lastWordform))
			{
				return false; // the paragraph for which word is a part is not yet charted.
			}

			offset = word.GetMyBeginOffsetInPara();
			iPara = word.Paragraph.IndexInOwner;
			return true;
		}

		private AnalysisOccurrence GetLastChartedWordform()
		{
			var latestRow = LastRow; // shouldn't assume LastRow HAS a Wordform!
			while (true)
			{
				var lastWordGroup = FindLastWordGroup(CellPartsInRow(latestRow));
				if (lastWordGroup != null)
				{
					var temp = new AnalysisOccurrence(lastWordGroup.EndSegmentRA, lastWordGroup.EndAnalysisIndex + 1);
					return temp.PreviousWordform();
				}
				latestRow = PreviousRow(latestRow);
				if (latestRow == null)
				{
					return null;
			}
		}
		}

		/// <summary>
		/// Find the index of the paragraph in the text of which the occurrence is a part.
		/// </summary>
		protected int GetParagraphIndexForOccurrence(AnalysisOccurrence occurrence)
		{
			if (!occurrence.IsValid)
			{
				throw new ArgumentException("Invalid Analysis!");
			}
			return (occurrence.Segment.Owner as IStTxtPara).IndexInOwner;
		}

		/// <summary>
		/// Get cell location/reference for Preceding and Following WordGroup used for showing ChOrph insertion options.
		/// Returns null ChartLocation if call failed to find a WordGroup in the appropriate direction.
		/// </summary>
		/// <param name="iPara">ChOrph input: paragraph index</param>
		/// <param name="offset">ChOrph input: offset into paragraph</param>
		/// <param name="precCell">Result: ChartLocation of preceding WordGroup's cell</param>
		/// <param name="follCell">Result: ChartLocation of following WordGroup's cell</param>
		protected internal void GetWordGroupCellsBorderingChOrph(int iPara, int offset, out ChartLocation precCell, out ChartLocation follCell)
		{
			Debug.Assert(iPara >= 0, $"Bad ChOrph paragraph index = {iPara}!");
			Debug.Assert(offset >= 0, $"Bad ChOrph paragraph offset = {offset}!");
			Debug.Assert(m_chart != null && m_chart.RowsOS.Count > 0); // IsChOrph() should return false in these conditions

			// Set 'out' variables for Chart Cell references
			precCell = null;
			follCell = null;
			// Set temporary variables
			var icolPrec = -1;
			var icolFoll = -1;
			IConstChartRow rowPrec = null;
			IConstChartRow rowFoll = null;

			// Loop through rows of chart
			foreach (var row in m_chart.RowsOS)
			{
				// Find the first WordGroup in each row
				var wordGrp = FindFirstWordGroup(CellPartsInRow(row));
				if (wordGrp == null)
				{
					continue; // No wordgroups in this chart row, look in next one.
				}

				// Found first WordGroup in this row!
				// Compare its text-logical position with the input ChOrph's text-logical position.
				if (!WordGroupStartsBeforeChOrph(iPara, offset, wordGrp))
				{
					rowFoll = row;
					var hvoCellPartFollowing = wordGrp.Hvo;
					icolFoll = IndexOfColumnForCellPart(hvoCellPartFollowing);
					break;
				}
				rowPrec = row;
				var hvoCellPartPreceding = wordGrp.Hvo;
				icolPrec = IndexOfColumnForCellPart(hvoCellPartPreceding);
			}
			// We either hit the end of the chart, or we found the following WordGroup.
			// [Actually there IS a way to hit the end of the chart 'legitimately'.
			//   The chOrph belongs in the last row of the chart that contains wordforms,
			//   so there are only non-WordGroup rows after.]
			if (rowFoll == null)
			{
				// Hit the end of the chart without finding a wordform that belongs after the ChOrph.
				// Go back to 'rowPrec' (last row that found a wordform) and check for other wordforms in the row
				// after ChOrph (ought to exist by definition, although it COULD be the same Preceding WordGroup).
				rowFoll = rowPrec;
				var wordGrp = FindLastWordGroup(CellPartsInRow(rowFoll));
				icolFoll = IndexOfColumnForCellPart(wordGrp.Hvo);
				if (IsChOrphWithinWordGroup(iPara, offset, wordGrp))
				{
					icolPrec = icolFoll; // Fixes at least part of LT-8380
					// Set 'out' variables
					if (rowFoll != null)
					{
						follCell = new ChartLocation(rowFoll, icolFoll);
					}

					if (rowPrec != null)
					{
						precCell = new ChartLocation(rowPrec, icolPrec);
					}
					return;
				}
			}
			// See if we can narrow the search forward
			if (rowPrec == null)  // The first wordform found in the chart is already AFTER the ChOrph.
			{
				icolPrec = 0; // Set to first column, first row.
				rowPrec = m_chart.RowsOS[0];
			}
			else
			{
				icolPrec = NarrowSearchForward(iPara, offset, new ChartLocation(rowPrec, icolPrec));
				// See if we can narrow the search backward
				if (rowPrec.Hvo != rowFoll.Hvo)
				{
					var temp = CheckFollowingRowPosition(iPara, offset, new ChartLocation(rowFoll, icolFoll));
					icolFoll = temp.ColIndex;
					rowFoll = temp.Row;
				}

				if (rowPrec.Hvo == rowFoll.Hvo) // rowFoll might have been changed in CheckFollowingRowPosition()
				{
					icolFoll = NarrowSearchBackward(iPara, offset, new ChartLocation(rowPrec, icolPrec), icolFoll);
			}
			}
			// By the time we get here, we should be able to return the right answer.
			// Set 'out' variables
			if (rowFoll != null)
			{
				follCell = new ChartLocation(rowFoll, icolFoll);
			}

			if (rowPrec != null)
			{
				precCell = new ChartLocation(rowPrec, icolPrec);
		}
		}

		/// <summary>
		/// Takes a cell location and tries to narrow the search forward for the last column in the
		/// row that is still logically before the ChOrph. Returns the new column index.
		/// </summary>
		private int NarrowSearchForward(int iPara, int offset, ChartLocation cell)
		{
			Debug.Assert(cell != null && cell.IsValidLocation); // Shouldn't occur; tested in calling routine
			// Moving forward, can we narrow our search to another cell?
			// We know that the first wordform in this row is "before" our ChOrph's logical position
			// And we know that the right Preceding Cell is in this row
			var result = cell.ColIndex;
			for (var icolNew = cell.ColIndex + 1; icolNew < AllMyColumns.Length; icolNew++)
			{
				var wordGrpFound = FindFirstWordGroup(PartsInCell(new ChartLocation(cell.Row, icolNew)));
				if (wordGrpFound == null)
				{
					continue;
				}

				if (!WordGroupStartsBeforeChOrph(iPara, offset, wordGrpFound))
				{
					break;
				}
				result = icolNew; // Keep incrementing our 'ref' index until we get past our logical position
			}
			return result;
		}

		/// <summary>
		/// Moving backward, can we narrow our search to another cell?
		/// The first wordform in this row is "after" our ChOrph's logical position, and we might need
		/// to back up a row (or more) to see if there is a wordform in an earlier row "after" or not.
		/// This routine prepares the field for NarrowSearchBackward().
		/// </summary>
		/// <param name="iPara">ChOrph input: paragraph index</param>
		/// <param name="offset">ChOrph input: offset into paragraph</param>
		/// <param name="cell">ChartLocation of wordform's cell</param>
		/// <returns>ChartLocation moved up, if possible</returns>
		private ChartLocation CheckFollowingRowPosition(int iPara, int offset, ChartLocation cell)
		{
			var result = cell;
			IConstChartRow tempRow = null;
			var tempColIndex = 0;
			var rowNew = PreviousRow(cell.Row);
			while (rowNew != null)
			{
				var wordGrpFound = FindLastWordGroup(CellPartsInRow(rowNew));
				if (wordGrpFound == null)
				{
					// No wordform in this row, go up again!
					rowNew = PreviousRow(rowNew);
					continue;
				}
				if (WordGroupStartsBeforeChOrph(iPara, offset, wordGrpFound))
				{
					// Either we found a WordGroup that 'contains' our ChOrph's position
					// or we've gone too many rows back and our 'ref' vars were already set right.
					if (IsChOrphWithinWordGroup(iPara, offset, wordGrpFound))
					{
						result = new ChartLocation(rowNew, IndexOfColumnForCellPart(wordGrpFound.Hvo));
					}
					return result;
				}
				// Updating our 'temp' variables.
				tempRow = rowNew;
				tempColIndex = IndexOfColumnForCellPart(wordGrpFound.Hvo);
				break;
			}
			var temp = new ChartLocation(tempRow, tempColIndex);
			if (!temp.IsSameLocation(cell))
			{
				 result = temp;
			}
			// We are now ready for NarrowSearchBackward().
			return result;
		}

		/// <summary>
		/// Moving backward, can we narrow our search to another cell?
		/// The last wordform in this row is "after" our ChOrph's logical position, but the first isn't
		/// and the right Following Cell is in this row.
		/// </summary>
		private int NarrowSearchBackward(int iPara, int offset, ChartLocation precCell, int icolFoll)
		{
			Debug.Assert(precCell != null && precCell.IsValidLocation);

			var result = icolFoll;
			for (var icolNew = icolFoll - 1; icolNew >= Math.Max(0, precCell.ColIndex); icolNew--)
			{
				var wordGrpFound = FindLastWordGroup(PartsInCell(new ChartLocation(precCell.Row, icolNew)));
				if (wordGrpFound == null)
				{
					continue;
				}
				if (!WordGroupStartsBeforeChOrph(iPara, offset, wordGrpFound))
				{
					result = icolNew; // Keep decrementing our result index until we get past our logical position
					continue;
				}

				if (IsChOrphWithinWordGroup(iPara, offset, wordGrpFound))
				{
					result = icolNew;
				}
				break;
			}
			return result;
		}

		/// <summary>
		/// Returns 'true' if the first wordform referenced by the WordGroup is
		/// text-logically prior to the ChOrph at StTxtPara[iPara] and offset. The logical ChOrph position
		/// might be internal to the WordGroup, not necessarily after it.
		/// Returns 'false' if the logical ChOrph position is before this WordGroup.
		/// </summary>
		/// <param name="iPara">ChOrph paragraph index</param>
		/// <param name="offset">ChOrph offset into paragraph</param>
		/// <param name="wordGrp"></param>
		/// <returns></returns>
		private bool WordGroupStartsBeforeChOrph(int iPara, int offset, IConstChartWordGroup wordGrp)
		{
			// Assumes that wordGrp is a CCWordGroup
			Debug.Assert(wordGrp != null);
			var wordforms = wordGrp.GetOccurrences();
			Debug.Assert(wordforms.Count > 0);

			var word = wordforms[0];
			var imyPara = GetParagraphIndexForOccurrence(word);
			if (imyPara < iPara)
			{
				return true;
			}
			return imyPara == iPara && word.GetMyBeginOffsetInPara() < offset;
		}

		/// <summary>
		/// Returns 'true' if the logical ChOrph position is between the (multiple) wordforms
		/// of this WordGroup.
		/// [GJM (May 12'10): I don't believe this could still be a problem with our new model.]
		/// </summary>
		/// <param name="iPara">ChOrph paragraph index</param>
		/// <param name="offset">ChOrph offset into paragraph</param>
		/// <param name="wordGroup">Assumes wordGroup is from the same paragraph as the ChOrph.</param>
		/// <returns></returns>
		private bool IsChOrphWithinWordGroup(int iPara, int offset, IConstChartWordGroup wordGroup)
		{
			Debug.Assert(wordGroup != null);
			var words = wordGroup.GetOccurrences();
			Debug.Assert(words != null && words.Count > 0);

			var cAppliesTo = words.Count;
			if (cAppliesTo == 1)
			{
				return false;
			}

			// Test first wordform in WordGroup, is ChOrph offset less?
			var firstWord = words[0];
			if (firstWord != null && offset < firstWord.GetMyBeginOffsetInPara())
			{
				return false;
			}

			// Test last wordform in wordGroup, is ChOrph offset greater? Need to check iPara first!
			var lastWord = words[cAppliesTo - 1];
			if (GetParagraphIndexForOccurrence(lastWord) != iPara)
			{
				// The last wordform and the first in this WordGroup are in different paragraphs,
				// therefore our answer is TRUE
				return true;
			}
			return (offset < lastWord.GetMyBeginOffsetInPara());
		}

		/// <summary>
		/// Figure out which cells are possibilities for inserting the present ChOrph.
		/// Mark them to be highlighted. Return a boolean array corresponding to column possibilities.
		/// </summary>
		internal bool[] HighlightChOrphPossibles(ChartLocation precCell, ChartLocation follCell)
		{
			Debug.Assert(follCell != null && follCell.IsValidLocation, "No following row possibility.");

			int irowPrec, icolPrec;
			if (precCell == null || !precCell.IsValidLocation)
			{
				irowPrec = 0;
				icolPrec = 0;
			}
			else
			{
				irowPrec = precCell.Row.IndexInOwner;
				icolPrec = precCell.ColIndex;
			}
			var icolFoll = follCell.ColIndex;
			var irowFoll = follCell.Row.IndexInOwner;
			return HighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
		}

		/// <summary>
		/// This one makes it more easily testable.
		/// </summary>
		protected bool[] HighlightChOrphPossibles(int icolPrec, int irowPrec, int icolFoll, int irowFoll)
		{
			var ccols = AllMyColumns.Length;
			var goodCols = new bool[ccols]; // return array of flags to disable the right MoveHere buttons

			var icurrCol = icolPrec;
			var icurrRow = irowPrec;

			// Set begin cell for highlighting
			var currHighlightCells = new int[4];
			currHighlightCells[0] = icurrRow;
			currHighlightCells[1] = icurrCol;

			while (true) // Won't loop more than ccols + 1 times.
			{
				goodCols[icurrCol] = true;
				icurrCol++;
				if (icurrRow == irowFoll && icurrCol > icolFoll)
				{
					break;
				}
				if (icurrCol == ccols) // Went past last column, start over at beginning of next row
				{
					icurrCol = 0;
					icurrRow++;
				}
				if (icurrRow > irowPrec && icurrCol >= icolPrec) // Never want to highlight more than one complete row
				{
					icolFoll = icolPrec - 1;
					if (icolFoll < 0)
					{
						icolFoll = ccols - 1;
						icurrRow--;
					}
					break;
				}
			}
			// Set end cell for highlighting
			currHighlightCells[2] = icurrRow;
			currHighlightCells[3] = icolFoll;
			CurrHighlightCells = currHighlightCells;
			return goodCols;
		}

		/// <summary>
		/// CCLogic part of preparing for a ChOrph insertion. Calculates cells to highlight as possible entry
		/// points and limits the ribbon selection to the current ChOrph group.
		/// </summary>
		/// <param name="iPara">ChOrph's paragraph index</param>
		/// <param name="offset">ChOrph's offset within paragraph</param>
		/// <param name="rowPrec">first possible row in which to insert this ChOrph</param>
		/// <returns>an array of booleans corresponding to the columns in the chart</returns>
		protected internal bool[] PrepareForChOrphInsert(int iPara, int offset, out IConstChartRow rowPrec)
		{
			// Prepare 'out' vars for GetWordGroupCellsBorderingChOrph()
			ChartLocation precCell, follCell;

			GetWordGroupCellsBorderingChOrph(iPara, offset, out precCell, out follCell);
			// Set Ribbon to limit selection to this ChOrph group
			SetRibbonLimits(follCell);
			// highlight eligible insert spots
			rowPrec = precCell.Row; // Set 'out' variable
			return HighlightChOrphPossibles(precCell, follCell);
		}

		/// <summary>
		/// After processing a ChOrph, we need to reset the ribbon's selection limits.
		/// </summary>
		protected internal void ResetRibbonLimits()
		{
			Ribbon.EndSelLimitIndex = -1;
			Ribbon.SelLimOccurrence = null;
		}

		/// <summary>
		/// Set Ribbon index and wordform for maximum allowed selection to prevent user selecting past
		/// an active ChOrph awaiting repair. Uses GetWordGroupCellsBorderingChOrph() on successive Ribbon elements
		/// to determine how far the current ChOrph goes.
		/// </summary>
		/// <param name="follCell">ChartLocation limit for first Ribbon element (for comparison)</param>
		protected internal void SetRibbonLimits(ChartLocation follCell)
		{
			var nui = NextUnchartedInput(kMaxRibbonContext);
			var i = 1; // start at 1, we already know our first word is a ChOrph
			for (; i < nui.Length; i++)
			{
				int iPara, offset; // 'out' vars for IsChOrph() to be used later
				if (!IsChOrph(nui[i], out iPara, out offset))
				{
					break; // No longer in any ChOrph group
				}
				// Prepare more 'out' vars for GetWordGroupCellsBorderingChOrph
				// This time we don't care about the Preceding cell, only the Following cell; has it changed?
				ChartLocation dummy, newFollCell;
				GetWordGroupCellsBorderingChOrph(iPara, offset, out dummy, out newFollCell);
				if (!newFollCell.IsSameLocation(follCell))
				{
					break; // No longer in same ChOrph group
			}
			}
			i--;
			Ribbon.EndSelLimitIndex = i; // These emit PropChanged themselves now.
			Ribbon.SelLimOccurrence = nui[i];
			Ribbon.SelectFirstOccurence();
		}

		#endregion

		/// <summary>
		/// Return a ChartLocation containing the column index and Row of a Wordform,
		/// unless it is uncharted, in which case it returns null.
		/// If it is a ChOrph (not charted because of Baseline modification),
		/// then it will return the nearest charted location. This should make the green
		/// highlighting visible (FWR-3681).
		/// </summary>
		public ChartLocation FindChartLocOfWordform(AnalysisOccurrence point)
		{
			if (m_chart == null || Chart.RowsOS.Count < 1)
			{
				return null;
			}
			Debug.Assert(point != null);

			ChartLocation arrayLoc;
			if (!point.HasWordform || IsChOrph(point))
			{
				var iPara = point.Paragraph.IndexInOwner;
				var offset = point.GetMyBeginOffsetInPara();
				ChartLocation dummy;
				GetWordGroupCellsBorderingChOrph(iPara, offset, out arrayLoc, out dummy);
			}
			else
			{
				arrayLoc = FindChartLocOfWordformInternal(point);
			}

			return arrayLoc.IsValidLocation ? arrayLoc : null;
		}

		private ChartLocation FindChartLocOfWordformInternal(AnalysisOccurrence point)
		{
			// What we know:
			//    m_chart exists and has at least one Row

			var result = m_chart.RowsOS.SelectMany(row => m_wordGrpRepo.AllInstances(), (row, wordGrp) => new {row, wordGrp})
				.Where(@t => @t.row.CellsOS.Contains(@t.wordGrp) && @t.wordGrp.GetOccurrences().Contains(point))
				.Select(@t => new ChartLocation(@t.row, IndexOfColumnForCellPart(@t.wordGrp))).ToList();

			// Either return the valid result from LINQ or an invalid ChartLocation
			return result.Any() ? result.First() : new ChartLocation(null, -1);
		}

		/// <summary>
		/// Get the WfiWordform closest to the bookmark.
		/// </summary>
		internal AnalysisOccurrence FindWordformAtBookmark(InterAreaBookmark bookmark)
		{
			Debug.Assert(StTextHvo > 0, "No text!");
			Debug.Assert(bookmark != null && bookmark.IndexOfParagraph > -1, "Bad bookmark!");
			var txt = m_textRepo.GetObject(StTextHvo);
			if (bookmark.IndexOfParagraph >= txt.ParagraphsOS.Count)
			{
				Debug.Fail("Bad bookmark paragraph index!");
				return null;
			}
			var curPara = (IStTxtPara)txt.ParagraphsOS[bookmark.IndexOfParagraph];
			Debug.Assert(curPara != null, "What kind of paragraph is this?");
			var ibeg = bookmark.BeginCharOffset;
			var iend = bookmark.EndCharOffset;
			var curSeg = FindSegmentContainingParaOffset(curPara, ibeg);
			if (curSeg == null)
			{
				return null;
			}
			var segOffset = curSeg.BeginOffset;
			bool fExactMatch;
			return curSeg.FindWagform(ibeg - segOffset, iend - segOffset, out fExactMatch);
		}

		private static ISegment FindSegmentContainingParaOffset(IStTxtPara para, int offset)
		{
			// Enhance: Is this better or quicker?
			//var mySeg = para.SegmentsOS.Where(seg => seg.BeginOffset <= beginChOrphOffset).LastOrDefault();
			var icurSeg = 0;
			var csegs = para.SegmentsOS.Count;
			if (csegs == 0)
			{
				return null;
			}

			while (icurSeg < csegs && para.SegmentsOS[icurSeg].BeginOffset <= offset)
			{
				icurSeg++;
			}
			icurSeg--;

			return para.SegmentsOS[icurSeg];
		}

		/// <summary>
		/// Gets all the 'leaf' nodes in a chart template, and also the ends of column groupings.
		/// </summary>
		public List<ICmPossibility> AllColumns(ICmPossibility template)
		{
			var result = new List<ICmPossibility>();
			var groups = new HashSet<int>();
			if (template == null || template.SubPossibilitiesOS.Count == 0)
			{
				return result; // template itself can't be a column even if no children.
			}
			CollectColumns(result, template, groups, 0);
			GroupEndIndices = groups;
			return result;
		}

		/// <summary>
		/// Collect (in depth-first traversal) all the leaf columns in the template.
		/// Also (LT-8104) collect the set of column indices that are the ends of top-level column groupings.
		/// </summary>
		private static void CollectColumns(List<ICmPossibility> result, ICmPossibility template, HashSet<int> groups, int depth)
		{
			if (template.SubPossibilitiesOS.Count == 0)
			{
				// Note: do NOT do add to the list if it has children...we ONLY want leaves in the result.
				result.Add(template);
				// We now collect this column index in our GroupEndsIndices even if it's a group of one.
				if (depth == 1)
				{
					groups.Add(result.Count - 1);
				}
				return;
			}

			foreach (var child in template.SubPossibilitiesOS)
			{
				CollectColumns(result, child, groups, depth + 1);
			}

			// Collect this column index in our GroupEndsIndices if we're at the top-level.
			if (depth == 1)
			{
				groups.Add(result.Count - 1);
		}
		}

		#region actions for buttons

		internal void MoveToColumnInUOW(int icol)
		{
			IConstChartRow modifiedRow = null;
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveWordToColumn, LanguageExplorerResources.ksRedoMoveWordToColumn, Cache.ActionHandlerAccessor, () => MoveToColumn(icol, out modifiedRow));
			FireRowModifiedEvent(modifiedRow);
		}

		/// <summary>
		/// V1: move the selected wordforms from the interlinear ribbon to the indicated column of the chart.
		/// Select the first remaining item in the ribbon.
		/// Moving to the indicated column includes making an extra row if there is anything already in a later
		/// column of the current row.
		/// It gets more complicated in the presence of 'marker' CellParts, which indicate things like missing elements
		/// and moved text.
		/// There are three possible ways to insert the new words: append to an existing WordGroup, make a new one in the
		/// same row, or make a new one at the start of a new row. This routine does one or the other; it calls
		/// FindWhereToAddWords to decide which.
		/// V2: V1 iff ribbon is active. If main chart is active, move what is selected there. In either case,
		/// follow up by selecting first thing in ribbon and making it active. Moving within the main chart is
		/// possible only if it won't put things out of order; ensure this.
		/// </summary>
		/// <param name="icol"></param>
		/// <param name="modifiedRow"></param>
		/// <returns>Null if successful, otherwise, an error message.</returns>
		public string MoveToColumn(int icol, out IConstChartRow modifiedRow)
		{
			modifiedRow = null;
			var selectedWordforms = Ribbon.SelectedOccurrences;
			if (selectedWordforms == null || selectedWordforms.Length == 0)
			{
				return LanguageExplorerResources.ksNoWordformsMsg;
			}
			Cache.ActionHandlerAccessor.AddAction(new UpdateRibbonAction(this, false));
			int iPara, offset; // 'out' vars for IsChOrph()
			var rowFinal = LastRow;
			if (IsChOrph(selectedWordforms[0], out iPara, out offset))
			{
				// Define 'out' vars for GetWordGroupCellsBorderingChOrph()
				ChartLocation precCell, follCell;
				GetWordGroupCellsBorderingChOrph(iPara, offset, out precCell, out follCell);
				// Make sure GetValidRowColumnForChOrph() knows that precCell could be null
				rowFinal = GetValidRowColumnForChOrph(precCell, follCell, icol);
				if (rowFinal != null)
				{
					var finalCell = new ChartLocation(rowFinal, icol);
					MoveChOrphToColumn(iPara, offset, selectedWordforms, finalCell);
				}
				else
				{
					return LanguageExplorerResources.ksChooseDifferentColumn;
				}
			}
			else
			{
				int position; // where to insert if we need a new WordGroup.
				IConstChartWordGroup wordGrpToAppendTo;
				var addWhere = FindWhereToAddWords(icol, out position, out wordGrpToAppendTo);
				switch (addWhere)
				{
					case FindWhereToAddResult.kAppendToExisting:
						var endPoint = selectedWordforms[selectedWordforms.Length - 1];
						ExtendWordGroupForwardTo(wordGrpToAppendTo, endPoint);
						break;
					case FindWhereToAddResult.kInsertWordGrpInRow:
					case FindWhereToAddResult.kMakeNewRow:
						if (addWhere == FindWhereToAddResult.kMakeNewRow) // distinguishes from kInsertWordGrpInRow
							rowFinal = MakeNewRow();
						var finalCell = new ChartLocation(rowFinal, icol);
						MakeWordGroup(finalCell, position, selectedWordforms);
						break;
				}
			}

			// Remove wordforms from input and select next item.
			NoteActionChangesRibbonItems(); // Fires Ribbon_Changed event
			modifiedRow = rowFinal;
			// FWR-3278: Try doing this later (after the UOW finishes) so that it gets valid selections.
			//FireRowModifiedEvent(modifiedRow);
			return null;
		}

		public void FireRowModifiedEvent(IConstChartRow modifiedRow)
		{
			if (RowModifiedEvent != null)
			{
				if (modifiedRow == null)
				{
					return;
				}
				RowModifiedEvent(this, new RowModifiedEventArgs(modifiedRow));
			}
		}

		/// <summary>
		/// Extend the end point of a ConstChartWordGroup forward to a new contiguous point.
		/// Caller is responsible to see that this doesn't overlap another word group's 'territory'.
		/// </summary>
		private static void ExtendWordGroupForwardTo(IConstChartWordGroup wordGrpToExtend, AnalysisOccurrence endPoint)
		{
			wordGrpToExtend.EndSegmentRA = endPoint.Segment;
			wordGrpToExtend.EndAnalysisIndex = endPoint.Index;
		}

		/// <summary>
		/// Returns the proper row to insert a ChOrph in the specified column (icol). If the desired column
		/// is not a valid choice given the current chart configuration, returns null.
		/// </summary>
		private IConstChartRow GetValidRowColumnForChOrph(ChartLocation precCell, ChartLocation follCell, int icol)
		{
			Debug.Assert(m_chart != null && m_chart.RowsOS.Count > 0);

			// There may not BE a valid preceding cell. See below for reason.
			if (precCell == null || !precCell.IsValidLocation)
			{
				// Handle case where there is no charted wordform before the ChOrph.
				var firstRow = m_chart.RowsOS[0];
				if (follCell.HvoRow == firstRow.Hvo && icol > follCell.ColIndex)
				{
					firstRow = m_rowFact.Create();
					m_chart.RowsOS.Insert(0, firstRow);
					return firstRow;
				}
				return firstRow;
			}
			if (precCell.HvoRow == follCell.HvoRow)
			{
				if (icol < precCell.ColIndex || icol > follCell.ColIndex)
				{
					return null; // Not an acceptable column choice as things stand presently.
				}
				return precCell.Row;
			}
			if (icol < precCell.ColIndex)
			{
				var nextRow = NextRow(precCell.Row);
				if (nextRow == follCell.Row && icol > follCell.ColIndex)
				{
					return null;
				}
				return nextRow; // so if there's a blank row between Prec/Foll, it goes there.
			}
			return precCell.Row;
		}

		/// <summary>
		/// Insert the ChOrph into the column chosen by the user (validity already checked by other routines)
		/// </summary>
		/// <param name="iPara">ChOrph paragraph index</param>
		/// <param name="offset">ChOrph offset into Text para</param>
		/// <param name="selectedWords"></param>
		/// <param name="finalCell">chart row in which to insert ChOrph + user-chosen column</param>
		private void MoveChOrphToColumn(int iPara, int offset, AnalysisOccurrence[] selectedWords, ChartLocation finalCell)
		{
			// Possibilities:
			// 1. There exists a CCWordGroup in the right chart cell(row/column combo)
			//    Possibly extend the boundaries of our WordGroup to cover our selectedWords
			// 2. Create a new CCWordGroup at this location with our selectedWords
			int whereToInsert;
			IConstChartWordGroup existingWordGroup;
			var result = FindWhereToAddChOrph(finalCell, iPara, offset, out whereToInsert, out existingWordGroup);
			switch (result)
			{
				case FindWhereToAddResult.kInsertWordGrpInRow:
					// whereToInsert gives index in CCRow Cells sequence
					MakeWordGroup(finalCell, whereToInsert, selectedWords);
					break;
				case FindWhereToAddResult.kInsertChOrphInWordGrp: // fall through (shouldn't occur)
				case FindWhereToAddResult.kAppendToExisting:
					// whereToInsert gives index in existingWordGroup occurrences
					ExpandWordGroupToInclude(existingWordGroup, selectedWords, whereToInsert > 0);
					break;
			}
		}

		private static void ExpandWordGroupToInclude(IAnalysisReference existingWordGroup, IList<AnalysisOccurrence> selectedWordforms, bool fforward)
		{
			AnalysisOccurrence expandTo;
			if (fforward)
			{
				expandTo = selectedWordforms[selectedWordforms.Count - 1];
				while (existingWordGroup.EndRef() != expandTo)
				{
					existingWordGroup.GrowFromEnd(true);
			}
			}
			else
			{
				expandTo = selectedWordforms[0];
				while (existingWordGroup.BegRef() != expandTo)
				{
					existingWordGroup.GrowFromBeginning(true);
			}
		}
		}

		private void NoteActionChangesRibbonItems()
		{
			RaiseRibbonChgEvent();
			var undoAction = new UpdateRibbonAction(this, true);
			Cache.ActionHandlerAccessor.AddAction(undoAction);
			undoAction.UpdateUnchartedWordforms();
		}

		/// <summary>
		/// Force a new clause.
		/// </summary>
		/// <param name="ihvo"></param>
		/// <returns></returns>
		public string MoveToHereInNewClause(int ihvo)
		{
			var result = LanguageExplorerResources.ksNoWordformsMsg;
			IConstChartRow modifiedRow = null;
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveWordToColumn, LanguageExplorerResources.ksRedoMoveWordToColumn,
				Cache.ActionHandlerAccessor, () =>
			{
				MakeNewRow();
				result = MoveToColumn(ihvo, out modifiedRow);
			});
			FireRowModifiedEvent(modifiedRow);
			return result;
		}

		// Here's another couple of ways of describing the algorithm:
		// if (last non-marker is in a later column)
		//	(1)make a new row
		// else if (last non-marker is in the same column)
		//	(2)append to last non-marker
		// else if (there's a marker in the same colum)
		//	(3)make a new row
		// else
		//	(4) make a new WordGroup in the same row (before any markers in later columns)

		// 1. Find the decisive WordGroup: last non-marker or marker-in-the-same-column.
		// 2. Earlier column: append after decisive WordGroup.
		// 3. Later column, or marker-in-the-same-column: make a new row, append at start.
		// 4. Same column: append to existing WordGroup.
		/// <summary>
		/// Find where to append the new words. This can involve finding (and returning) a WordGroup
		/// that is already in the right row and column that we can just add to, or making a
		/// new WordGroup (possibly in a new row). Here is a summary of the options:
		/// 1. If the last WordGroup in the last row is in the right column, extend its coverage to the new words.
		/// 2. If there's an empty spot in the last row at the specified column, and no WordGroups
		///		in later columns, reference the words with a new WordGroup inserted into the row at the appropriate place.
		/// 3. Otherwise, reference the new words in a new WordGroup in a new row.
		/// Only used for appending words to the end of the chart.
		/// </summary>
		///<param name="icol">desired column index</param>
		///<param name="whereToInsert">index in Row's Cells sequence, if new WordGroup</param>
		///<param name="existingWordGrp"></param>
		///<returns>enum result used in switch</returns>
		internal FindWhereToAddResult FindWhereToAddWords(int icol, out int whereToInsert, out IConstChartWordGroup existingWordGrp)
		{
			existingWordGrp = null;
			whereToInsert = 0;
			var lastRow = LastRow;
			if (lastRow == null)
			{
				return FindWhereToAddResult.kMakeNewRow;
			}
			if (lastRow.CellsOS.Count == 0)
			{
				// Probably the only way this happens is when inserting a moved text marker.
				// If the marker collides, we add a row before calling MoveToColumn
				return FindWhereToAddResult.kInsertWordGrpInRow;
			}
			var allCols = new List<ICmPossibility>(AllMyColumns); // so we can use indexOf
			whereToInsert = lastRow.CellsOS.Count;
			var icellPart = lastRow.CellsOS.Count - 1;
			var cellPart1 = lastRow.CellsOS[icellPart];
			var wordGrp = cellPart1 as IConstChartWordGroup;
			// loop over markers (if any) at end of list till we find something that determines the outcome.
			// also skip list refs
			while (wordGrp == null) // cellPart1 is not a WordGroup, therefore a marker of some type
			{
				if (cellPart1.ColumnRA == AllMyColumns[icol])
				{
					if (IsMissingMarker(cellPart1))
					{
						// Missing Marker gets removed and then add here
						RemoveMissingMarker(new ChartLocation(lastRow, icol));
						whereToInsert = icellPart; // insert into current row here
						return FindWhereToAddResult.kInsertWordGrpInRow;
					}
					// marker in the same column...we'll have to start a new row. (case 3)
					whereToInsert = 0;
					return FindWhereToAddResult.kMakeNewRow;
				}
				if (allCols.IndexOf(cellPart1.ColumnRA) < icol)
				{
					// It's a marker BEFORE the column we want (case 4). If we get here there's nothing
					// in the row except possibly other markers after our target column. Since we didn't take
					// the exit above, there can't be anything in the target column, so can insert in same row.
					whereToInsert = icellPart + 1; // insert into current row AFTER CellPart
					return FindWhereToAddResult.kInsertWordGrpInRow;
				}
				if (icellPart == 0)
				{
					// No non-markers, perhaps the first thing the user did was insert a missing item marker?
					whereToInsert = 0; // insert BEFORE the current marker, since it is in a later column.
					return FindWhereToAddResult.kInsertWordGrpInRow;
				}
				// Current cellPart is a marker in a column after the one we want: look earlier to see whether
				// there is a conflict or something we can append to.
				icellPart--;
				cellPart1 = lastRow.CellsOS[icellPart];
				wordGrp = cellPart1 as IConstChartWordGroup;
			}

			// Got a wordGrp. If it's in the RIGHT colum, we just return it
			// (and whereToInsertWordGroup doesn't matter, because we won't make a new one). Case 2.
			if (wordGrp.ColumnRA == AllMyColumns[icol])
			{
				existingWordGrp = wordGrp;
				// whereToInsert doesn't matter
				return FindWhereToAddResult.kAppendToExisting;
			}

			// If the last non-marker is in a LATER column, we have to make a new row (case 1)
			if (allCols.IndexOf(wordGrp.ColumnRA) > icol)
			{
				whereToInsert = 0; // insert at start of new row.
				return FindWhereToAddResult.kMakeNewRow;
			}

			// wordGrp is in an EARLIER column; make a new WordGroup AFTER it (case 4)
			whereToInsert = icellPart + 1;
			return FindWhereToAddResult.kInsertWordGrpInRow;
		}

		/// <summary>
		/// Find where to insert the ChOrph wordforms. This can involve finding (and returning) a WordGroup
		/// that is already the right type in the right row and column that we can just add to, or making a
		/// new WordGroup. Here is a summary of the options:
		/// 1. If there is a WordGroup at the current ChartLocation, return it and
		///    figure out how to expand it to include the ChOrph (expand forward or backward).
		/// 2. If there is NOT a WordGroup in the current row at the specified column,
		///		insert the words into a new WordGroup inserted into the row at the appropriate place.
		/// Only used for inserting ChOrphs into their 'rightful place' in the chart.
		/// </summary>
		/// <param name="curCell">desired ChOrph cell</param>
		/// <param name="iChOrphPara">ChOrph paragraph index</param>
		/// <param name="beginChOrphOffset">ChOrph offset</param>
		/// <param name="whereToInsert">If we are creating a new WordGroup, this is the index in the row's CellsOS.
		/// If we are inserting into an existing WordGroup, this should tell us whether to append to the beginning
		/// or end of the WordGroup.</param>
		/// <param name="existingWordGroup"></param>
		/// <returns></returns>
		internal FindWhereToAddResult FindWhereToAddChOrph(ChartLocation curCell, int iChOrphPara, int beginChOrphOffset, out int whereToInsert, out IConstChartWordGroup existingWordGroup)
		{
			// Enhance GordonM: This is an awfully long method. I need to break it down somehow.
			existingWordGroup = null;
			whereToInsert = 0;
			var ccellPartsInRow = curCell.Row.CellsOS.Count;
			if (ccellPartsInRow == 0)
			{
				return FindWhereToAddResult.kInsertWordGrpInRow; // whereToInsert == 0
			}
			var cellPartInCellCollection = PartsInCell(curCell);
			existingWordGroup = FindFirstWordGroup(cellPartInCellCollection);
			if (existingWordGroup == null)
			{
				// Will need to insert a WordGroup. Figure out index into Row.CellsOS.
				whereToInsert = FindWhereToInsertInRow(curCell);
				return FindWhereToAddResult.kInsertWordGrpInRow;
			}

			// We've found at least one WordGroup
			int icurrWordGroup = cellPartInCellCollection.IndexOf(existingWordGroup);
			var nextWordGroup = existingWordGroup;
			var locationOfChOrph = FindChOrphLocation(iChOrphPara, beginChOrphOffset);
			while (true) // Loop through this and any other WordGroups in this cell until we find our spot
			{
				whereToInsert = 0;
				var wordList = nextWordGroup.GetOccurrences();
				if (wordList[0].IsAfter(locationOfChOrph))
				{
					if (nextWordGroup.Hvo == existingWordGroup.Hvo)
					{
						// This is the first WordGroup in this cell
						return  FindWhereToAddResult.kInsertChOrphInWordGrp; // will 'grow' WordGroup backwards
					}
					// will append to existing; whereToInsert will be wrong in this case!
					break;
				}
				whereToInsert = wordList.Count - 1;
				if (wordList[whereToInsert].IsAfter(locationOfChOrph))
				{
					return FindWhereToAddResult.kInsertChOrphInWordGrp; // i.e. Do nothing!
				}

				// If we fall through here, we either need to append to the WordGroup we've been looking at
				// or there are multiple WordGroups in this cell and we need to look further.
				existingWordGroup = nextWordGroup;
				int ifoundAt; // 'out' variable below
				nextWordGroup = FindNextWordGroup(cellPartInCellCollection, icurrWordGroup, out ifoundAt);
				if (nextWordGroup == null)
				{
					whereToInsert++; // will append to end of existingWordGroup
					break;
				}
				icurrWordGroup = ifoundAt;
			}
			return FindWhereToAddResult.kAppendToExisting;
		}

		private AnalysisOccurrence FindChOrphLocation(int iChOrphPara, int beginChOrphOffset)
		{
			Debug.Assert(m_chart != null && m_chart.BasedOnRA != null);
			var cpara = m_chart.BasedOnRA.ParagraphsOS.Count;
			if (iChOrphPara >= cpara)
			{
				throw new ArgumentOutOfRangeException(nameof(iChOrphPara));
			}
			var para = m_chart.BasedOnRA.ParagraphsOS[iChOrphPara] as IStTxtPara;
			if (para == null || para.SegmentsOS.Count == 0)
			{
				return null;
			}
			var mySeg = FindSegmentContainingParaOffset(para, beginChOrphOffset);
			bool fExactMatch;
			return mySeg.FindWagform(beginChOrphOffset, beginChOrphOffset, out fExactMatch);
		}

		private int FindWhereToInsertInRow(ChartLocation cellDesired)
		{
			var icolDesired = cellDesired.ColIndex;
			var row = cellDesired.Row;
			var cpartsInRow = row.CellsOS.Count;
			var iwhereToInsert = 0;
			for (; iwhereToInsert < cpartsInRow; iwhereToInsert++)
			{
				var currCellPart = row.CellsOS[iwhereToInsert];
				var icurrCol = IndexOfColumnForCellPart(currCellPart);
				if (icurrCol < icolDesired)
				{
					continue;
				}
				if (icurrCol > icolDesired)
				{
					break; // iwhereToInsert == 'the right spot' in sequence
				}

				// currCellPart is in the correct column
				if (currCellPart is IConstChartMovedTextMarker && ((IConstChartMovedTextMarker)currCellPart).Preposed)
				{
					continue; // So far, we assume only preposed markers go before wordforms in a cell
				}
				// This is a clause marker or tag and should come after any wordforms in this cell
				break;
			}
			return iwhereToInsert;
		}

		internal const int kMaxRibbonContext = 20;

		/// <summary>
		/// Insert another row.
		/// If inserting below, takes over any compDetails of the previous row.
		/// Line label is calculated based on the original row's label.
		/// Caller deals with UOW.
		/// </summary>
		/// <param name="originalRow">The row one clicks to insert another row above or below</param>
		/// <param name="insertAbove">True = insert above; False = insert below</param>
		public void InsertRow(IConstChartRow originalRow, bool insertAbove)
		{
			var index = originalRow.IndexInOwner;
			var newRow = m_rowFact.Create();
			if (insertAbove)
			{
				m_chart.RowsOS.Insert(index, newRow);
			}
			else
			{
			m_chart.RowsOS.Insert(index + 1, newRow);
				SetupCompDetailsForInsertRow(originalRow, newRow);
			}
			// foneSentOnly = true, because we are only adding a letter to the current sentence.
			RenumberRows(index, true);
		}

		/// <summary>
		/// Setups the boolean features for InsertRow().
		/// Inserting a new row will quite possibly remove special borders from the previous row.
		/// But it shouldn't affect dependent/song/speech clause features! [LT-9587]
		/// </summary>
		private static void SetupCompDetailsForInsertRow(IConstChartRow previousRow, IConstChartRow newRow)
		{
			SafelyToggleBottomBorder(previousRow);
			var fEndSent = previousRow.EndSentence;
			if (!fEndSent)
			{
				return; // don't set anything on new row
			}

			// delete prevRow Sentence feature, add newRow Sentence feature
			previousRow.EndSentence = false;
			newRow.EndSentence = true;
			var fEndPara = previousRow.EndParagraph;
			if (!fEndPara)
			{
				return;
			}
			// delete prevRow para feature, add newRow para feature
			previousRow.EndParagraph = false;
			newRow.EndParagraph = true;
		}

		/// <summary>
		/// Clears the chart from the given cell to the end of the chart.
		/// Creates a UOW.
		/// </summary>
		public void ClearChartFromHereOn(ChartLocation cell)
		{
			var rowIndex = cell.Row.IndexInOwner;
			var row = cell.Row;

			// If column index is zero we're deleting from the very start of the row.
			// This is done not mainly to avoid the call to FindIndexOfFirstCellPartInOrAfterColumn,
			// but for robustness: it allows us to delete successfully even if we somehow have
			// a broken cell part that isn't marked as being in any column.
			var icellPart = cell.ColIndex == 0 ? 0 : FindIndexOfFirstCellPartInOrAfterColumn(cell);
			var ifirstRowToDelete = rowIndex + 1;
			var crows = m_chart.RowsOS.Count;
			var crowsToDelete = crows - ifirstRowToDelete;
			var ccells = row.CellsOS.Count;
			var ccellsToDelete = ccells - icellPart;
			if (ccellsToDelete == 0 && crowsToDelete == 0)
			{
				return;
			}

			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoClearChart, LanguageExplorerResources.ksRedoClearChart,
										Cache.ActionHandlerAccessor, () =>
			{
				// Because of possible side effects of deleting cells and rows, we'll do it
				// one by one and be careful that there is still something to delete
				// each time!

				// Delete the redundant rows. Contents of owned sequence are deleted automatically.
				if (crowsToDelete > 0)
				{
					//m_chart.RowsOS.Replace(ifirstRowToDelete, crowsToDelete, new ICmObject[0]);
					for (var i = crowsToDelete + ifirstRowToDelete - 1; i >= ifirstRowToDelete; i--)
					{
						m_chart.RowsOS.RemoveAt(i);
						if (i < 0 || m_chart.Hvo == (int) SpecialHVOValues.kHvoObjectDeleted)
						{
							break;
						}
						var newCount = m_chart.RowsOS.Count;
						// defend against side effect deleting earlier object
						if (crows - 1 > newCount)
						{
							i--;
						}
						crows = newCount;
					}
				}

				// Delete the redundant stuff in the current row.
				// Have to recalculate some of the original information in case of side effects
				ccells = row.CellsOS.Count;
				icellPart = cell.ColIndex == 0 ? 0 : FindIndexOfFirstCellPartInOrAfterColumn(cell);
				ccellsToDelete = ccells - icellPart;

				if (ccellsToDelete > 0)
				{
					//row.CellsOS.Replace(icellPart, ccellsToDelete, new ICmObject[0]);
					for (var i = ccellsToDelete + icellPart - 1; i >= icellPart; i--)
					{
						row.CellsOS.RemoveAt(i);
						if (i < 0 || row.Hvo == (int) SpecialHVOValues.kHvoObjectDeleted)
						{
							break;
						}
						var newCount = row.CellsOS.Count;
						// defend against side effect deleting earlier object(s)
						if (ccells - 1 > newCount)
						{
							i--;
						}
						ccells = newCount;
					}
				}

				RepairRowsNoLongerReferedToByClauseMarkers();
				NoteActionChangesRibbonItems();
				RenumberRows(DecrementRowSafely(rowIndex), false);
			});
		}

		/// <summary>
		/// Find rows in this chart that have other than 'Normal' ClauseType. Check to see
		/// that there is still a ClauseMarker referencing that row. If not, reset DependentClauseGroup
		/// booleans and ClauseType.
		/// </summary>
		private void RepairRowsNoLongerReferedToByClauseMarkers()
		{
			var myAbnormalRows = m_chart.RowsOS.Where(row => row.ClauseType != ClauseTypes.Normal).ToList();
			if (myAbnormalRows.Count == 0)
			{
				return;
			}
			var clsMrkrTargets = new HashSet<IConstChartRow>();
			var myClsMrkrs = m_clauseMkrRepo.AllInstances().Where(mrkr =>
				mrkr.Owner != null &&
				mrkr.Owner.Owner != null &&
				mrkr.Owner.Owner.Hvo == m_chart.Hvo);
			foreach (var clsMrkr in myClsMrkrs)
			{
				clsMrkrTargets.UnionWith(clsMrkr.DependentClausesRS);
			}

			foreach (var row in myAbnormalRows.Where(row => !clsMrkrTargets.Contains(row)))
			{
				ResetDepClauseProps(row);
		}
		}

		private static void ResetDepClauseProps(IConstChartRow row)
		{
			row.StartDependentClauseGroup = false;
			row.EndDependentClauseGroup = false;
			row.ClauseType = ClauseTypes.Normal;
		}

		/// <summary>
		/// This creates a new row to append at the end of the chart's list of rows.
		/// This method assumes it is already inside of a valid UOW.
		/// </summary>
		/// <returns></returns>
		protected IConstChartRow MakeNewRow()
		{
			var rowLabel = CreateNewRowLabel();
			var newRow = GetNewRow(rowLabel);
			// Inserting a new row will quite possibly remove the end-of-document thick underline from
			// the previous line, so we need to force it to be redisplayed.
			var prevRow = Chart.RowsOS[DecrementRowSafely(LastRow.IndexInOwner)];
			if (newRow != prevRow)
			{
				SafelyToggleBottomBorder(prevRow);
			}
			return newRow;
		}

		private static void SafelyToggleBottomBorder(IConstChartRow prevRow)
		{
			// The sole purpose of this routine is to force the row to redraw itself
			// There ought to be a better way!
			if (prevRow.EndParagraph)
			{
				// more complicated that it would be otherwise
				// because EndParagraph assumes EndSentence.
				prevRow.EndParagraph = false;
				prevRow.EndParagraph = true;
			}
			else
			{
				// whichever way EndSentence is, toggle it the other way and back again
				prevRow.EndSentence = !prevRow.EndSentence;
				prevRow.EndSentence = !prevRow.EndSentence;
			}
		}

		private IConstChartRow GetNewRow(string rowLabel)
		{
			var newRow = m_rowFact.Create();
			m_chart.RowsOS.Add(newRow);
			newRow.Label = TsStringUtils.MakeString(rowLabel, WsLineNumber);
			return newRow;
		}

		/// <summary>
		/// Calculates a row label from the previous row's label.
		/// Note: This uses LastRowNumber to get the previous row's label
		/// so it needs to be called BEFORE Appending the new row to the
		/// chart list of rows and ONLY for a new row at the end of the chart.
		/// </summary>
		/// <returns>A string to be used in labeling the new line.</returns>
		private string CreateNewRowLabel()
		{
			int rowNumber;
			int clauseNumber;
			string rowLabel; // our result string
			var prevWasEOS = false; // was the last row marked End of Sentence?
			var prevRowLabel = LastRowNumber;
			if (LastRow != null)
			{
				prevWasEOS = LastRow.EndSentence;
			}
			else
			{
				rowLabel = "1";
				return rowLabel;
			}
			DecipherRowLabel(prevRowLabel, out rowNumber, out clauseNumber);
			if (clauseNumber == 1 && !prevWasEOS)
			{
				// If the previous one had no clause number, we should add an 'a' to it.
				// But only if the previous one wasn't the End of a Sentence.
				AddLetterToNumberOnlyLabel(LastRow.IndexInOwner, rowNumber);
			}
			rowLabel = CalculateMyRowNums(ref rowNumber, ref clauseNumber, prevWasEOS, true);
			return rowLabel;
		}

		/// <summary>
		/// If a row label is only a number, because it is currently the only clause in its sentence,
		/// and then we add a clause, that first row label needs to have an 'a' appended to it.
		/// </summary>
		/// <param name="rowIndex">Index of the row that needs modifying.</param>
		/// <param name="rowNumber">The number of this row for the label.</param>
		private void AddLetterToNumberOnlyLabel(int rowIndex, int rowNumber)
		{
			m_chart.RowsOS[rowIndex].Label = TsStringUtils.MakeString(Convert.ToString(rowNumber) + 'a', WsLineNumber);
		}

		/// <summary>
		/// Answer the writing system we want to use for line numbers.
		/// Currently this is locked to English, so as to avoid various weird behaviors
		/// when the first analysis or default user WS is changed (or is something that won't
		/// render "1a" sensibly.
		/// </summary>
		public int WsLineNumber => Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");

		/// <summary>
		/// Pulls the row number and clause letter(a=1) from a string
		/// </summary>
		private static void DecipherRowLabel(string rowLabel1, out int row, out int clause)
		{
			var rowLabel = rowLabel1 ?? string.Empty;
			var posFirstLetter = 0;

			for (var i = 1; i < rowLabel.Length; i++) // i=1 because never start with a letter
			{
				if (rowLabel[i] < 'a' || rowLabel[i] > 'z')
				{
					continue;
				}
				posFirstLetter = i;
				break;
			}
			if (posFirstLetter == 0)
			{
				// Haven't yet found a letter! So this is only a number, no subclauses.
				if (!int.TryParse(rowLabel, out row))
				{
					row = 1; // arbitrary, may arise e.g. from empty string following change of first analysis WS.
				}
				clause = 1;
			}
			else
			{
				row = Convert.ToInt32(rowLabel.Substring(0,posFirstLetter));
				if (posFirstLetter == (rowLabel.Length - 1))
				{
					// only one letter present; we assume no more than 2 letters
					// is it possible to have 53+ clauses in a Sentence?
					clause = Convert.ToInt32(rowLabel[posFirstLetter]) - Convert.ToInt32('a') + 1;
				}
				else
				{
					clause = Convert.ToInt32(rowLabel[posFirstLetter + 1]) - Convert.ToInt32('a') + 27;
			}
		}
		}

		/// <summary>
		/// Recalculates and installs sentence/clause numbers
		/// </summary>
		/// <param name="irow">The index of the modified row. (Actually, probably one up from the modified row.)</param>
		/// <param name="foneSentOnly">True if we only need to renumber this sentence (although it'll do 2 to be safe).</param>
		internal void RenumberRows(int irow, bool foneSentOnly)
		{
			bool fIsThisRowEOS;
			var fIsPrevRowEOS = true;
			var csentence = 0;
			var cclause = 1;
			if (irow > 0)
			{
				fIsPrevRowEOS = m_chart.RowsOS[irow - 1].EndSentence;
				// Get label from previous row
				DecipherRowLabel(m_chart.RowsOS[irow - 1].Label.Text, out csentence, out cclause);
			}

			// Iterate through all rows starting with the modified one
			// and calculate row numbering to install in the Row label.
			var foneSentFinished = false; // LT-8488 On foneSentOnly we need to do 2 sentences for odd case.
			while (irow < m_chart.RowsOS.Count)
			{
				// If this is the last row, assume EOS for numbering purposes,
				// otherwise look it up.
				fIsThisRowEOS = m_chart.RowsOS.Count == irow + 1 || m_chart.RowsOS[irow].EndSentence;
				// Calculate and deposit label (if changed)
				var rowLabel = CalculateMyRowNums(ref csentence, ref cclause, fIsPrevRowEOS, fIsThisRowEOS);
				if (m_chart.RowsOS[irow].Label.Text != rowLabel)
				{
					m_chart.RowsOS[irow].Label = TsStringUtils.MakeString(rowLabel, WsLineNumber);
				}

				if (fIsThisRowEOS && foneSentOnly && foneSentFinished)
				{
					break;
				}

				if (fIsThisRowEOS)
				{
					foneSentFinished = true;
				}
				fIsPrevRowEOS = fIsThisRowEOS;
				irow++;
			}
		}

		/// <summary>
		/// Figure out a new sentence number and clause letter number equivalent from context.
		/// </summary>
		/// <param name="prevSentNum">Number from previous line's label.</param>
		/// <param name="prevClauseNum">Number equivalent of letter in previous line's label.</param>
		/// <param name="fSentBrkBefore">Is there a Sentence break between me and previous line?</param>
		/// <param name="fSentBrkAfter">Is there a Sentence break between me and following line?</param>
		/// <returns>A string label for this line of a chart.</returns>
		private static string CalculateMyRowNums(ref int prevSentNum, ref int prevClauseNum, bool fSentBrkBefore, bool fSentBrkAfter)
		{
			if (fSentBrkBefore)
			{
				prevSentNum++;
				prevClauseNum = 1;
			}
			else prevClauseNum++;

			// Make the string
			var result = prevClauseNum > 26
				? Convert.ToString(prevSentNum) + "a" + Convert.ToChar(Convert.ToInt32('a') + prevClauseNum - 27)
				: Convert.ToString(prevSentNum) + Convert.ToChar(Convert.ToInt32('a') + prevClauseNum - 1);
			if (fSentBrkAfter && fSentBrkBefore)
			{
				// Strip 'a' off of string.
				result = result.Substring(0, result.Length - 1);
			}
			return result;
		}

		/// <summary>
		/// V1: move the selected wordforms from the interlinear ribbon to the indicated column of the chart,
		/// and also insert a moved text marker in iColMovedFrom (in the same row). Column moved from may
		/// come before or after icolActual.
		/// Insert actual text exactly like MoveToColumn.
		/// Append the marker if postposed, prepend if preposed, in the required destination cell.
		/// V2: V1 iff ribbon is active. Otherwise, make a moved text object out of the current selection in
		/// the current chart. (If it is already moved text, transfer the old marker.)
		/// Note: the moved text marker (as opposed to the moved text itself) is not an obstacle to putting
		/// more stuff on the same row.
		/// </summary>
		public string MakeMovedText(int icolActual, int icolMovedFrom)
		{
			var selectedWordforms = Ribbon.SelectedOccurrences;
			if (selectedWordforms == null || selectedWordforms.Length == 0)
			{
				return LanguageExplorerResources.ksNoWordformsMsg;
			}
			IConstChartRow rowModified = null;
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMakeMoved, LanguageExplorerResources.ksRedoMakeMoved,
										Cache.ActionHandlerAccessor, () =>
			{
				MoveToColumn(icolActual, out rowModified);
				MakeMovedFrom(icolActual, icolMovedFrom);
			});
			FireRowModifiedEvent(rowModified);
			return null;
		}

		internal IConstChartRow LastRow => m_chart != null && m_chart.RowsOS.Count > 0 ? m_chart.RowsOS[m_chart.RowsOS.Count - 1] : null;

		string LastRowNumber
		{
			get
			{
				if (m_chart == null || m_chart.RowsOS.Count <= 0)
				{
					return string.Empty;
				}
					var result = LastRow.Label.Text;
				return result ?? string.Empty;
			}
		}

		/// <summary>
		/// Answer true if actualCell contains a WordGroup and icolMovedFrom designates a column that contains
		/// a moved-text marker pointing at it (same row).
		/// </summary>
		protected bool IsMarkedAsMovedFrom(ChartLocation actualCell, int icolMovedFrom)
		{
			return PartsInCell(new ChartLocation(actualCell.Row, icolMovedFrom)).Any(mtm => IsMarkerOfMovedFrom(mtm, actualCell));
		}

		/// <summary>
		/// Answer true if cellPart is a ConstChartMovedTextMarker pointing to a wordgroup in the specified cell (same row).
		/// </summary>
		internal bool IsMarkerOfMovedFrom(IConstituentChartCellPart cellPart, ChartLocation cellInSameRow)
		{
			var mtm = cellPart as IConstChartMovedTextMarker;
			return mtm != null && PartsInCell(cellInSameRow).Contains(mtm.WordGroupRA);
		}

		/// <summary>
		/// Remove an existing moved text marker (that points at a WordGroup in actualCell).
		/// This version starts already knowing the marker.
		/// </summary>
		protected void RemoveMovedFrom(ChartLocation actualCell, IConstChartMovedTextMarker movedFromMarker)
		{
			new MakeMovedTextMethod(this, actualCell, movedFromMarker).RemoveMovedFrom();
		}

		/// <summary>
		/// Remove an existing moved text marker from movedFromCell (that points at a WordGroup in actualCell)
		/// </summary>
		protected void RemoveMovedFrom(ChartLocation actualCell, ChartLocation movedFromCell)
		{
			new MakeMovedTextMethod(this, actualCell, movedFromCell).RemoveMovedFrom();
		}

		/// <summary>
		/// This is the most generic form. Only need these for setting up the MovedTextMarker.
		/// Once set up, it knows if it's Preposed or not.
		/// </summary>
		internal bool IsPreposed(ChartLocation actualCell, ChartLocation movedFromCell)
		{
			return actualCell.HvoRow == movedFromCell.HvoRow ? IsPreposed(actualCell.ColIndex, movedFromCell.ColIndex) : IsPreposed(actualCell.Row, movedFromCell.Row);
		}

		/// <summary>
		/// Use this one if you have two columns in the same row to determine
		/// if the marker is Preposed or Postposed.
		/// </summary>
		internal static bool IsPreposed(int icolActual, int icolMovedFrom)
		{
			return icolActual < icolMovedFrom;
		}

		/// <summary>
		/// Use this one if you have a marker and its target on different rows to determine
		/// if the marker is Preposed or Postposed.
		/// </summary>
		internal bool IsPreposed(IConstChartRow rowActual, IConstChartRow rowMovedFrom)
		{
			return rowActual.IndexInOwner < rowMovedFrom.IndexInOwner;
		}

		/// <summary>
		/// Return true if cellPart is a MovedText marker and happens to mark Preposed text.
		/// </summary>
		internal bool IsPreposedMarker(IConstituentChartCellPart cellPart)
		{
			// If it's a ConstChartMovedTextMarker, ask it if it's Preposed!
			var mtm = cellPart as IConstChartMovedTextMarker;
			return mtm != null && mtm.Preposed; // much simpler!
		}

		/// <summary>
		/// Make a marker indicating that what is in icolActual of the current last row
		/// has been moved from the specified column. Assume there's a WordGroup in icolActual.
		/// </summary>
		private void MakeMovedFrom(int icolActual, int icolMovedFrom)
		{
			// Figure where to insert, and find the target.
			MakeMovedFrom(new ChartLocation(LastRow, icolActual), new ChartLocation(LastRow, icolMovedFrom));
		}

		/// <summary>
		/// Make a MovedText marker for moves (pre/postposed) involving two different rows.
		/// </summary>
		protected void MakeMovedFrom(ChartLocation actual, ChartLocation movedFrom)
		{
			new MakeMovedTextMethod(this, actual, movedFrom).MakeMovedFrom();
		}

		/// <summary>
		/// Make a MovedText marker for moves (pre/postposed) involving two different rows and only part
		/// of the source cell's text. Used by the 'Advanced' Pre/Postposed dialog. Most generic form.
		/// </summary>
		protected void MakeMovedFrom(ChartLocation actual, ChartLocation movedFrom, AnalysisOccurrence begPoint, AnalysisOccurrence endPoint)
		{
			// what if some part of the source cell is already marked as moved?
			// Enhance GordonM: Someday may need to handle case where list of wordforms is non-contiguous,
			// but not yet.
			new MakeMovedTextMethod(this, actual, movedFrom, begPoint, endPoint).MakeMovedFrom();
		}

		/// <summary>
		/// Make a marker indicating that something is missing in the specified column.
		/// </summary>
		internal void ToggleMissingMarker(ChartLocation cell, bool wasMarked)
		{
			if (wasMarked)
			{
				var cellParts = PartsInCell(cell);
				foreach (var part in cellParts)
				{
					var tag = part as IConstChartTag;
					if (tag == null)
					{
						continue;
					}
					if (tag.TagRA == null)
					{
						// This is the missing marker
						UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksRedoMarkMissing, LanguageExplorerResources.ksUndoMarkMissing,
								Cache.ActionHandlerAccessor, () => cell.Row.CellsOS.Remove(tag));
						break;
					}
				}
			}
			else
			{
				var icellPartInsertAt = FindIndexOfFirstCellPartInOrAfterColumn(cell);
				// Enhance JohnT: may want to make this configurable.
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMarkMissing, LanguageExplorerResources.ksRedoMarkMissing,
					Cache.ActionHandlerAccessor, () => MakeMissingMarker(cell, icellPartInsertAt));
			}
		}

		/// <summary>
		/// Find the index in the row's Cells property of the first CellPart in (or after)
		/// the specified column.
		/// </summary>
		private int FindIndexOfFirstCellPartInOrAfterColumn(ChartLocation targetCell)
		{
			var newTarget = new ChartLocation(targetCell.Row, targetCell.ColIndex - 1);
			return FindIndexOfCellPartInLaterColumn(newTarget);
		}

		/// <summary>
		/// Find the index of the first CellPart that is in a column with index > targetCell's column index.
		/// </summary>
		protected internal int FindIndexOfCellPartInLaterColumn(ChartLocation targetCell)
		{
			var icellPartInsertAt = targetCell.Row.CellsOS.Count; // insert at end unless we find something in a later column.
			var icellPart = 0;
			for (var icol = 0; icol < AllMyColumns.Length && icellPart < targetCell.Row.CellsOS.Count; )
			{
				var part = targetCell.Row.CellsOS[icellPart];
				var col = part.ColumnRA;
				if (col == AllMyColumns[icol])
				{
					// the current CellPart is in column icol. If icol > icolMovedFrom, we want to insert
					// the new CellPart before this one.
					if (icol > targetCell.ColIndex)
					{
						icellPartInsertAt = icellPart; // insert before this CellPart, the first one in a later column.
						break;
					}
					icellPart++;
				}
				else
				{
					// The current CellPart isn't in the current column; it must be in a later one.
					// Continue the main loop.
					icol++;
				}
			}
			return icellPartInsertAt;
		}

		/// <summary>
		/// Return a list of all the cell parts in the specified cell.
		/// (Doesn't test the parts for "WordGroup-ness".)
		/// </summary>
		protected internal List<IConstituentChartCellPart> PartsInCell(ChartLocation cell)
		{
			int dummy;
			return CellPartsInCell(cell, out dummy);
		}

		/// <summary>
		/// Return a list of all the cell parts in the specified cell(and the index in the row where the first occurs).
		/// (Doesn't test the parts for "WordGroup-ness".)
		/// If no occurences, returns empty list, but index should still be accurate.
		/// </summary>
		protected internal List<IConstituentChartCellPart> CellPartsInCell(ChartLocation cell, out int index)
		{
			var col = AllMyColumns[cell.ColIndex];
			var result = new List<IConstituentChartCellPart>();
			index = 0;
			foreach (var cellPart in cell.Row.CellsOS)
			{
				if (cellPart.ColumnRA == col)
				{
					result.Add(cellPart);
					continue;
				}
				if (IndexOfColumn(cellPart.ColumnRA) > cell.ColIndex)
					break;
				// Keep counting until we find one in the right column
				index++;
			}
			return result;
		}

		/// <summary>
		/// Return a list of all the CCWordGroups from the list of all CellParts supplied.
		/// If no occurences, returns null.
		/// </summary>
		/// <param name="partsInCell">A list of IConstituentChartCellParts typically from a chart cell.</param>
		/// <returns></returns>
		internal static List<IConstChartWordGroup> CollectCellWordGroups(List<IConstituentChartCellPart> partsInCell)
		{
			return partsInCell.OfType<IConstChartWordGroup>().ToList();
		}

		internal static List<IConstituentChartCellPart> CellPartsInRow(IConstChartRow row)
		{
			return row.CellsOS.ToList();
		}

		/// <summary>
		/// Make a clause marker in the specified cell of the specified row (which must be empty),
		/// specifying that depClauses is a sequence of adjacent dependent clauses that embed there.
		/// Currently no error checking! We're assuming it's only called for empty cells.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="depClauses"></param>
		/// <param name="depType">A localizable string; currently one of 'dependent', 'speech', or 'song'.</param>
		/// <returns></returns>
		public IConstChartClauseMarker MakeDependentClauseMarker(ChartLocation cell, IConstChartRow[] depClauses, ClauseTypes depType)
		{
			IConstChartClauseMarker newClauseMrkr = null;

			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMakeDepClause, LanguageExplorerResources.ksRedoMakeDepClause,
										Cache.ActionHandlerAccessor, () =>
			{
				// There's about to be something in the source cell.
				RemoveMissingMarker(cell);

				foreach (var rowDst in depClauses)
				{
					// clears any state it has from other, hopefully 'further out' markers
					// Enhance JohnT: possibly, we want to tag the marker itself so we have (partly redundant) information
					// about which clauses are dependent at which position. Then we could confidently restore, and perhaps
					// even figure out nesting automatically. For now (See LT-8100) we decided "most recent wins, though it
					// may leave an orphan marker".
					RemoveAllDepClauseMarkers(rowDst);
					rowDst.ClauseType = depType;
				}
				depClauses[0].StartDependentClauseGroup = true;
				depClauses[depClauses.Length - 1].EndDependentClauseGroup = true;
				newClauseMrkr = m_clauseMkrFact.Create(cell.Row, FindIndexOfCellPartInLaterColumn(cell), AllMyColumns[cell.ColIndex], depClauses);
			});
			return newClauseMrkr;
		}

		/// <summary>
		/// Find a CellPart in the specified column, or null if none.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="fWantWordGroup">If true, require result to be a WordGroup.</param>
		/// <returns></returns>
		internal IConstituentChartCellPart FindCellPartInColumn(ChartLocation cell, bool fWantWordGroup = false)
		{
			return cell.Row.CellsOS.Where(cellPart => cellPart.ColumnRA == AllMyColumns[cell.ColIndex]).FirstOrDefault(cellPart => !fWantWordGroup || cellPart is IConstChartWordGroup);
		}

		///<summary>
		/// Answer true if this row of the chart is a dependent clause.
		/// </summary>
		public bool IsDepClause(IConstChartRow row)
		{
			return row.ClauseType != ClauseTypes.Normal;
		}

		internal int IndexOfColumn(ICmPossibility col)
		{
			for (var i = 0; i < AllMyColumns.Length; i++)
			{
				if (AllMyColumns[i] == col)
				{
					return i;
				}
			}
			return -1;
		}

		internal int IndexOfColumnForCellPart(IConstituentChartCellPart part)
		{
			var col = part.ColumnRA;
			Debug.Assert(col != null);
			return IndexOfColumn(col);
		}

		internal int IndexOfColumnForCellPart(int hvoPart)
		{
			Debug.Assert(hvoPart > 0);
			var cellPart = m_cellPartRepo.GetObject(hvoPart);
			return IndexOfColumnForCellPart(cellPart);
		}

		/// <summary>
		/// Make a new constituent chart WordGroup in the specified cell (of the chart),
		/// inserting it at position icellPartInsertAt of the Cells sequence of the Row.
		/// Make it cover the specified targets. Caller deals with UOW.
		/// </summary>
		/// <param name="cell">The ChartLocation of the new WordGroup.</param>
		/// <param name="icellPartInsertAt"></param>
		/// <param name="targets">The new WordGroup references these Analyses.</param>
		/// <returns></returns>
		internal IConstChartWordGroup MakeWordGroup(ChartLocation cell, int icellPartInsertAt,
			AnalysisOccurrence[] targets)
		{
			Debug.Assert(targets.Length > 0, "Can't make a WordGroup with no words!");
			var begPoint = targets[0];
			var endPoint = targets[targets.Length - 1];
			return MakeWordGroup(cell, icellPartInsertAt, begPoint, endPoint);
		}

		/// <summary>
		/// Make a new constituent chart WordGroup in the specified cell (of the chart), inserting it at
		/// position icellPartInsertAt of the Cells sequence of the Row. Make it cover from the
		/// begPoint analysis to the endPoint analysis. Caller deals with UOW.
		/// </summary>
		/// <param name="cell">The ChartLocation of the new WordGroup.</param>
		/// <param name="icellPartInsertAt"></param>
		/// <param name="begPoint">The new WordGroup references Analyses starting here.</param>
		/// <param name="endPoint">The new WordGroup references Analyses ending here.</param>
		/// <returns></returns>
		internal IConstChartWordGroup MakeWordGroup(ChartLocation cell, int icellPartInsertAt,
			AnalysisOccurrence begPoint, AnalysisOccurrence endPoint)
		{
			Debug.Assert(begPoint.IsValid && endPoint.IsValid, "Can't make a WordGroup with invalid end points.");
			var colPoss = AllMyColumns[cell.ColIndex];
			return m_wordGrpFact.Create(cell.Row, icellPartInsertAt, colPoss, begPoint, endPoint);
		}

		/// <summary>
		/// Make a new constituent chart tag, pointing at the
		/// specified CmPossibility (hvoListItem), in the cell's column, inserting it at
		/// position icellPartInsertAt of the Cells sequence in the Row.
		/// </summary>
		private IConstChartTag MakeChartTag(ChartLocation cell, int hvoListItem, int icellPartInsertAt)
		{
			// N.B. The below note used to be true, but is no longer because LCM barfs if an
			// object that is required to be owned exits LCM without being owned by something!
			//
			// [It's best to change all its properties before we insert it into the row.
			// This avoids multiple display updates. Also, currently Redo loses changes
			// to properties of newly created objects, so if we set the ChartTag contents
			// after inserting the ChartTag, we actually don't see the recreated object.]
			var colPoss = AllMyColumns[cell.ColIndex];
			var listPoss = m_possRepo.GetObject(hvoListItem);
			return m_chartTagFact.Create(cell.Row, icellPartInsertAt, colPoss, listPoss);
		}

		/// <summary>
		/// Make a new user-added missing marker (special case of ChartTag), pointing at the
		/// specified CmPossibility (hvoListItem), in the cell's column, inserting it at
		/// position icellPartInsertAt of the Cells sequence of the Row.
		/// </summary>
		private IConstChartTag MakeMissingMarker(ChartLocation cell, int icellPartInsertAt)
		{
			var colPoss = AllMyColumns[cell.ColIndex];
			return m_chartTagFact.CreateMissingMarker(cell.Row, icellPartInsertAt, colPoss);
		}

		#endregion

		#region context menu

		/// <summary>
		/// Answer whether the specified column of the specified row is empty.
		/// </summary>
		/// <param name="cell"></param>
		/// <returns></returns>
		public bool IsCellEmpty(ChartLocation cell)
		{
			return FindCellPartInColumn(cell) == null;
		}

		public ContextMenuStrip MakeCellContextMenu(ChartLocation clickedCell)
		{
			var irow = clickedCell.Row.IndexInOwner;
			Debug.Assert(irow >= 0); // should be one of our rows!
			var menu = new ContextMenuStrip();

			// Menu items allowing wordforms to be marked as pre or postposed
			MakePreposedPostposedMenuItems(menu, clickedCell);

			// Menu items allowing dependent clause/speech clause to be made
			AddDependentClauseMenuItems(clickedCell, irow, menu);

			// Menu items for Moving cell contents.
			if (CellContainsWordforms(clickedCell))
			{
				MakeMoveItems(clickedCell, menu, itemMoveForward_Click, itemMoveBack_Click, LanguageExplorerResources.ksMoveMenuItem);
				MakeMoveItems(clickedCell, menu, itemMoveWordForward_Click, itemMoveWordBack_Click, LanguageExplorerResources.ksMoveWordMenuItem);
			}

			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(menu);

			// Menu items allowing the user to toggle whether the line ends a paragraph.
			var itemREP = new OneValMenuItem(LanguageExplorerResources.ksRowEndsParaMenuItem, clickedCell.HvoRow)
			{
				CheckState = clickedCell.Row.EndParagraph ? CheckState.Checked : CheckState.Unchecked
			};
			itemREP.Click += itemREP_Click;
			menu.Items.Add(itemREP);

			// Menu items allowing the user to toggle whether the line ends a sentence.
			var itemRES = new OneValMenuItem(LanguageExplorerResources.ksRowEndsSentMenuItem, clickedCell.Row.Hvo)
			{
				CheckState = clickedCell.Row.EndSentence ? CheckState.Checked : CheckState.Unchecked
			};
			itemRES.Click += itemRES_Click;
			menu.Items.Add(itemRES);

			// Menu items allowing the cell to be visually merged with an empty cell to the left or right.
			var cellPart = FindCellPartInColumn(clickedCell);
			if (cellPart != null)
			{
				// non-empty, may have merge left/right capability
				if (clickedCell.ColIndex > 0 && IsCellEmpty(new ChartLocation(clickedCell.Row, clickedCell.ColIndex - 1)))
				{
					var itemMergeBefore = new RowColMenuItem(LanguageExplorerResources.ksMergeBeforeMenuItem, clickedCell);
					itemMergeBefore.Click += itemMergeBefore_Click;
					if (cellPart.MergesBefore)
					{
						itemMergeBefore.Checked = true;
					}
					menu.Items.Add(itemMergeBefore);
				}
				if (clickedCell.ColIndex < AllMyColumns.Length - 1 && IsCellEmpty(new ChartLocation(clickedCell.Row, clickedCell.ColIndex + 1)))
				{
					var itemMergeAfter = new RowColMenuItem(LanguageExplorerResources.ksMergeAfterMenuItem, clickedCell);
					var cellPartLast = FindCellPartInColumn(clickedCell, false);
					if (cellPartLast.MergesAfter)
					{
						itemMergeAfter.Checked = true;
					}
					menu.Items.Add(itemMergeAfter);
					itemMergeAfter.Click += itemMergeAfter_Click;
				}
			}

			var itemNewRowAbove = new RowColMenuItem(LanguageExplorerResources.ksInsertRowMenuItemAbove, clickedCell);
			menu.Items.Add(itemNewRowAbove);
			itemNewRowAbove.Click += itemNewRowAbove_Click;

			var itemNewRowBelow = new RowColMenuItem(LanguageExplorerResources.ksInsertRowMenuItemBelow, clickedCell);
			menu.Items.Add(itemNewRowBelow);
			itemNewRowBelow.Click += itemNewRowBelow_Click;

			var itemCFH = new RowColMenuItem(LanguageExplorerResources.ksClearFromHereOnMenuItem, clickedCell);
			menu.Items.Add(itemCFH);
			itemCFH.Click += new EventHandler(itemCFH_Click);

			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(menu);

			// Menu items for inserting arbitrary markers from the ChartMarkers list.
			var chartMarkerList = Cache.LangProject.DiscourseDataOA.ChartMarkersOA;
			GeneratePlMenuItems(menu, chartMarkerList, ToggleMarker_Item_Click, clickedCell);

			var mms = MissingState(clickedCell);
			if (mms == MissingMarkerState.kmmsDoesNotApply)
			{
				return menu;
			}
			var itemMissingMarker = new RowColMenuItem(LanguageExplorerResources.ksMarkMissingItem, clickedCell);
			itemMissingMarker.Click += itemMissingMarker_Click;
				itemMissingMarker.Checked = (mms == MissingMarkerState.kmmsChecked);
				menu.Items.Add(itemMissingMarker);

			return menu;
		}

		private enum MissingMarkerState
		{
			kmmsDoesNotApply, // cell contains something else
			kmmsChecked, // missing marker present
			kmmsUnchecked // cell completely empty
		}

		private MissingMarkerState MissingState(ChartLocation cell)
		{
			// As per LT-8545, Possibility markers don't affect MissingMarkerState (they aren't 'content')
			if (ColumnHasAutoMissingMarkers(cell.ColIndex))
			{
				return MissingMarkerState.kmmsDoesNotApply;
			}
			var cellPart = FindCellPartInColumn(cell);
			if (cellPart == null)
			{
				return MissingMarkerState.kmmsUnchecked;
			}
			if (cellPart is IConstChartTag)
			{
				var chartTag = cellPart as IConstChartTag;
				return chartTag.TagRA == null ? MissingMarkerState.kmmsChecked : MissingMarkerState.kmmsUnchecked;
			}
			return MissingMarkerState.kmmsDoesNotApply; // other 'content'
		}

		/// <summary>
		/// Currently an ConstChartTag is identified as a missing marker
		/// by having a Tag property that doesn't point at anything.
		/// </summary>
		private static bool IsMissingMarker(IConstituentChartCellPart part)
		{
			if (!(part is IConstChartTag))
			{
				return false;
			}
			return ((IConstChartTag)part).TagRA == null;
		}

		/// <summary>
		/// If there's a missing marker in the specified cell get rid of it.
		/// </summary>
		internal void RemoveMissingMarker(ChartLocation cell)
		{
			var cellPart = FindCellPartInColumn(cell);
			if (!IsMissingMarker(cellPart))
			{
				return;
			}
			var row = cell.Row;
			if (row.CellsOS.Count > 1 || row.Notes != null)
			{
				// Easy case; removing a missing marker won't cause the row to go away
				row.CellsOS.Remove(cellPart);
				return;
			}
			// In this case the row will disappear before we can add anything to it!
			// Save its properties and recreate it after.
			var irow = row.IndexInOwner;
			var label = row.Label;
			var endPara = row.EndParagraph;
			var endSent = row.EndSentence;
			var clauseType = row.ClauseType;
			var startDepGrp = false;
			var endDepGrp = false;
			IConstChartClauseMarker[] markers = null;
			// Check also for ClauseType != Normal and if so modify the ClauseMarker
			if (clauseType != ClauseTypes.Normal)
			{
				startDepGrp = row.StartDependentClauseGroup;
				endDepGrp = row.EndDependentClauseGroup;
				markers = FindMyClauseMarkers(row);
			}
			row.CellsOS.Remove(cellPart);
			if (cell.HvoRow != (int) SpecialHVOValues.kHvoObjectDeleted)
			{
				return;
			}
			// Our row just disappeared on us due to deleting the last cell!
			// But we're obviously about to put something in here, so recreate the row.
			var newrow = m_rowFact.Create(Chart, irow, label);
			newrow.EndSentence = endSent;
			newrow.EndParagraph = endPara;
			if (clauseType == ClauseTypes.Normal)
			{
				return;
			}
			newrow.ClauseType = clauseType;
			newrow.StartDependentClauseGroup = startDepGrp;
			newrow.EndDependentClauseGroup = endDepGrp;
			foreach (var marker in markers)
			{
				marker.DependentClausesRS.Add(newrow);
		}
		}

		private IConstChartClauseMarker[] FindMyClauseMarkers(IConstChartRow row)
		{
			return m_clauseMkrRepo.AllInstances().Where(marker => marker.DependentClausesRS.Contains(row)).ToArray();
		}

		private void AddDependentClauseMenuItems(ChartLocation srcCell, int irow, ContextMenuStrip menu)
		{
			// See whether there is any existing dep clause marker (of any subtype) in the cell.
			foreach (var part in PartsInCell(srcCell))
			{
				if (!(part is IConstChartClauseMarker))
				{
					continue;
				}
				var itemR = new RowColMenuItem(LanguageExplorerResources.ksRemoveDependentMarkerMenuItem, srcCell);
				itemR.Click += itemR_Click;
				menu.Items.Add(itemR);
				return;
			}
			var itemMDC = MakeDepClauseItem(srcCell, irow, LanguageExplorerResources.ksMakeDepClauseMenuItem, ClauseTypes.Dependent);
			menu.Items.Add(itemMDC);

			var itemMSC = MakeDepClauseItem(srcCell, irow, LanguageExplorerResources.ksMakeSpeechClauseMenuItem, ClauseTypes.Speech);
			menu.Items.Add(itemMSC);

			var itemMSoC = MakeDepClauseItem(srcCell, irow, LanguageExplorerResources.ksMakeSongClauseMenuItem, ClauseTypes.Song);
			menu.Items.Add(itemMSoC);
		}

		private void itemR_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			RemoveDepClause(item.SrcCell);
		}

		/// <summary>
		/// Remove the (first) dependent clause info related to the specified cell.
		/// Enhance JohnT: there's a pathological case involving nested dependent clauses where
		/// it might not be right to clear everything like we are.
		/// </summary>
		/// <param name="cell"></param>
		protected void RemoveDepClause(ChartLocation cell)
		{
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoRemoveClauseMarker, LanguageExplorerResources.ksRedoRemoveClauseMarker,
										Cache.ActionHandlerAccessor, () =>
			{
				foreach (var clauseMarker in PartsInCell(cell).OfType<IConstChartClauseMarker>())
				{
					foreach (var depRow in clauseMarker.DependentClausesRS)
					{
						RemoveAllDepClauseMarkers(depRow);
					}
					cell.Row.CellsOS.Remove(clauseMarker);
					break;
				}
			});
		}

		private static void RemoveAllDepClauseMarkers(IConstChartRow depRow)
		{
			depRow.ClauseType = ClauseTypes.Normal;
			depRow.StartDependentClauseGroup = false;
			depRow.EndDependentClauseGroup = false;
		}

		private void itemCFH_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			var crowDel = m_chart.RowsOS.Count - item.SrcRow.IndexInOwner;
			if (MessageBox.Show(string.Format(LanguageExplorerResources.ksDelRowWarning, crowDel), LanguageExplorerResources.ksWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
			{
				ClearChartFromHereOn(item.SrcCell);
			}
		}

		private void itemNewRowAbove_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			InsertRowAboveInUOW(item.SrcRow);
		}

		void itemNewRowBelow_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			InsertRowBelowInUOW(item.SrcRow);
		}

		private void InsertRowAboveInUOW(IConstChartRow nextRow)
		{
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoInsertRow, LanguageExplorerResources.ksRedoInsertRow, Cache.ActionHandlerAccessor,
				() => InsertRow(nextRow, true));
		}

		private void InsertRowBelowInUOW(IConstChartRow prevRow)
		{
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoInsertRow, LanguageExplorerResources.ksRedoInsertRow, Cache.ActionHandlerAccessor,
				() => InsertRow(prevRow, false));
		}

		private bool CellContainsWordforms(ChartLocation cell)
		{
			return FindFirstWordGroup(PartsInCell(cell)) != null;
		}

		private void MakeMoveItems(ChartLocation srcCell, ContextMenuStrip menu, EventHandler forward, EventHandler backward, string mainLabel)
		{
			// If there's nothing in the cell we can't move it; and a missing marker doesn't count.
			var part = FindCellPartInColumn(srcCell);
			if (part == null || IsMissingMarker(part))
			{
				return;
			}

			var itemMove = new DisposableToolStripMenuItem(mainLabel);
			if (TryGetNextCell(srcCell))
			{
				var itemMoveForward = new RowColMenuItem(LanguageExplorerResources.ksForwardMenuItem, srcCell);
				itemMoveForward.Click += forward;
				itemMove.DropDownItems.Add(itemMoveForward);
			}
			if (TryGetPreviousCell(srcCell))
			{
				var itemMoveBack = new RowColMenuItem(LanguageExplorerResources.ksBackMenuItem, srcCell);
				itemMoveBack.Click += backward;
				itemMove.DropDownItems.Add(itemMoveBack);
			}
			menu.Items.Add(itemMove);
		}

		private void MakePreposedPostposedMenuItems(ContextMenuStrip menu, ChartLocation srcCell)
		{
			// Collect all WordGroups in the source cell
			var wordGroups = CollectCellWordGroups(PartsInCell(srcCell));
			if (wordGroups == null || wordGroups.Count == 0)
			{
				return; // can't do this without some real data in the cell.
			}

			var fMMDifferentRow = false; // true if we find a marker in a different row pointing to this cell (either direction)
			var fMMForward = false; // true if Preposed text has a marker in a different row
			var fMMBackward = false; // true if Postposed text has a marker in a different row
			foreach (var wordGrp in wordGroups)
			{
				// Check to see if wordGrp is 'movedText' and if so, find its target
				IConstChartMovedTextMarker movedMrkr;
				if (!IsMovedText(wordGrp, out movedMrkr))
				{
					continue;
				}
				var markerCell = new ChartLocation((IConstChartRow)movedMrkr.Owner, IndexOfColumn(movedMrkr.ColumnRA)); // Might eventually contain the location of the 'movedText' marker (MM=MovedMarker)
				if (srcCell.HvoRow == markerCell.HvoRow)
				{
					continue;
				}
				fMMDifferentRow = true;
				if (movedMrkr.Preposed)
				{
					fMMForward = true;
				}
				else
				{
					fMMBackward = true;
			}
			}
			int icol = srcCell.ColIndex;
			MakePreposedOrPostPosedMenuItem(menu, srcCell, 0, icol, LanguageExplorerResources.ksPostposeFromMenuItem, fMMDifferentRow && fMMBackward); // True if a MT Marker is already out there.
			MakePreposedOrPostPosedMenuItem(menu, srcCell, icol + 1, AllMyColumns.Length, LanguageExplorerResources.ksPreposeFromMenuItem, fMMDifferentRow && fMMForward);
		}

		/// <summary>
		/// Makes Pre/Postposed Menu items based on columns to the appropriate side of this cell. Also may
		/// make a menu item for Advanced... depending on boolean flags.
		/// (If the flag is true, the Advanced... menu item will be checked.)
		/// </summary>
		private void MakePreposedOrPostPosedMenuItem(ContextMenuStrip menu, ChartLocation srcCell, int icolStart, int icolLim, string text, bool fMarkerPresent)
		{
			// If no subitems, don't make the parent.
			// First check if another clause is possible?
			var fAnotherClausePossible = IsAnotherClausePossible(srcCell.Row, text == LanguageExplorerResources.ksPreposeFromMenuItem);
			if ((icolStart >= icolLim) && !fMarkerPresent && !fAnotherClausePossible)
			{
				return;
			}
			var itemMTSubmenu = new DisposableToolStripMenuItem(text);
			menu.Items.Add(itemMTSubmenu);
			for (var i = icolStart; i < icolLim; i++)
			{
				var itemCol = new TwoColumnMenuItem(GetColumnLabel(i), srcCell.ColIndex, i, srcCell.Row);
				itemCol.Click += itemCol_Click;
				itemCol.Checked = IsMarkedAsMovedFrom(srcCell, i);
				itemMTSubmenu.DropDownItems.Add(itemCol);
			}
			// We always need the "Advanced..." option if IsAnotherClausePossible is true, or there are available columns.
			var itemAdvanced = new RowColMenuItem(LanguageExplorerResources.ksAdvancedDlgMenuItem, srcCell);
			itemMTSubmenu.DropDownItems.Add(itemAdvanced);
			if (text == LanguageExplorerResources.ksPreposeFromMenuItem)
			{
				itemAdvanced.Click += itemAnotherPre_Click;
			}
			else
			{
				itemAdvanced.Click += itemAnotherPost_Click;
			}

			if (fMarkerPresent)
			{
				itemAdvanced.Checked = true;
		}
		}

		private bool IsAnotherClausePossible(IConstChartRow rowSrc, bool fPrepose)
		{
			return (CollectRowsInSentence(rowSrc, fPrepose).Count > 1); // If there's more than the source row, then it's possible!
		}

		const int rowLimiter = 5;

		/// <summary>
		/// Collects a list of rows in the same Sentence as the supplied ChartRow
		/// going in the specified direction. "Same Sentence" is defined by the rows' EndOfSentence
		/// Feature and limited (at this point) to 5 rows forward or backward.
		/// Current row is now included.
		/// </summary>
		private List<IConstChartRow> CollectRowsInSentence(IConstChartRow curRow, bool fForward)
		{
			// Better check first to see if we're going forward and curRow has EOS feature
			var result = new List<IConstChartRow> {curRow}; // include current row
			if (fForward && curRow.EndSentence)
			{
				return result;
			}
			var testIndex = curRow.IndexInOwner;
			var indexMax = Math.Min(m_chart.RowsOS.Count - 1, testIndex + rowLimiter);
			var indexMin = Math.Max(0, testIndex - rowLimiter);
			do
			{
				// if fForward, are there rows after rowSrc?
				// if not, are there rows before rowSrc?
				if (fForward)
				{
					testIndex++;
				}
				else
				{
					testIndex--;
				}
				if (testIndex < indexMin || testIndex > indexMax)
				{
					break; // We went too far!
				}
				var tempRow = m_chart.RowsOS[testIndex];
				if (tempRow.EndSentence)
				{
					if (fForward)
					{
						result.Add(tempRow); // going forward, include the EOS row
					}
					break;
				}
				result.Add(tempRow);
			} while (true);

			if (!fForward)
			{
				result.Reverse();
			}
			return result;
		}

		private void itemAnotherPre_Click(object sender, EventArgs e)
		{
			itemAdvanced_Click(sender, e, true);
		}

		private void itemAnotherPost_Click(object sender, EventArgs e)
		{
			itemAdvanced_Click(sender, e, false);
		}

		/// <summary>
		/// Handles clicks on Advanced... menu items under Prepose or Postpose
		/// in cell Context menu.
		/// </summary>
		/// <param name="sender">is a RowColMenuItem</param>
		/// <param name="e"></param>
		/// <param name="fPrepose">if True, this is a Prepose item, otherwise Postpose.</param>
		void itemAdvanced_Click(object sender, EventArgs e, bool fPrepose)
		{
			if (m_chart.RowsOS.Count < 1)
			{
				return;
			}

			var item = sender as RowColMenuItem;
			var srcCell = item.SrcCell;

			if (item.Checked)
			{
				// Go find the marker that points to this cell and remove it.
				var marker = FindMovedMarkerOtherRow(srcCell, fPrepose);
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksRedoMakeMoved, LanguageExplorerResources.ksUndoMakeMoved,
											Cache.ActionHandlerAccessor, () => RemoveMovedFrom(srcCell, marker));
				return;
			}

			// Collect rows and columns to display in dialog
			// First rows
			var eligibleRows = CollectEligibleRows(srcCell, fPrepose).ToArray();
			if (eligibleRows.Length == 0) return; // Shouldn't happen!


			// Enhance GordonM: We need to do something different if there are multiple WordGroups in the cell?!
			// I did 'something different' alright, but I'm not done. [Or am I?]
			// Maybe I need to make a temporary WordGroup that holds all the wordforms in the cell for dialog ribbon display purposes.

			// Load all WordGroups in this cell into the parameter object's AffectedWordGroups.
			var paramObject = new CChartSentenceElements(srcCell, eligibleRows, AllMyColumns)
			{
				AffectedWordGroups = CollectAllWordGroups(PartsInCell(srcCell))
			};
			var dlg = new AdvancedMTDialog(Cache, fPrepose, paramObject, m_helpTopicProvider);
			try
			{
			// Display dialog
			if (dlg.ShowDialog() != DialogResult.OK)
				{
				return;
				}
			var fromRow = dlg.SelectedRow.Row;
			var iColMovedFrom = IndexOfColumn(dlg.SelectedColumn.Column);

				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMakeMoved, LanguageExplorerResources.ksRedoMakeMoved,
				Cache.ActionHandlerAccessor, () =>
			{
				// LT-7668 If user chooses to make a movedText marker and one exists already for this cell
				// we need to remove the first one before adding this one.
				// Now we check all the WordGroups affected by our new MovedText item and remove any existing marker.
				foreach (var wordGrp in paramObject.AffectedWordGroups)
				{
					IConstChartMovedTextMarker marker;
					if (!IsMovedText(wordGrp, out marker))
							{
						continue;
							}
					RemoveMovedFrom(srcCell, FindChartLocOfCellPart(marker));
				}
				var movedFrom = new ChartLocation(fromRow, iColMovedFrom);
				var cwords = dlg.SelectedOccurrences.Length;
				MakeMovedFrom(srcCell, movedFrom, dlg.SelectedOccurrences[0], dlg.SelectedOccurrences[cwords - 1]);
			});
		}
			finally
			{
				dlg?.Dispose();
			}
		}

		private ChartLocation FindChartLocOfCellPart(IConstituentChartCellPart cellPart)
		{
			Debug.Assert(cellPart.IsValidObject, "Invalid object!");
			var row = (IConstChartRow)cellPart.Owner;
			var icol = IndexOfColumnForCellPart(cellPart);
			return new ChartLocation(row, icol);
		}

		/// <summary>
		/// Enhance JohnT/EricP: note that we are gradually developing mechanisms to prevent this happening
		/// (like AnalysisAdjuster). It may eventually be unnecessary as LCM causes objects
		/// to clean up after themselves more and more. Protected for testing.
		/// </summary>
		protected internal void CleanupInvalidChartCells()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var fReported = false;
				// Clobber any deleted words, etc.
				// GJM 2012.06.15 -- having too many problems with number of rows changing, so now we will check
				// row count each time through instead of maintaining a count that can get messed up.
				for (var irow = 0; irow < m_chart.RowsOS.Count; irow++) // not foreach here, as we may delete some as we go
				{
					var curRow = m_chart.RowsOS[irow];
					var citems = curRow.CellsOS.Count;
					// If there are already no items, it's presumably an empty row the user inserted manually
					// and plans to put something into, not a sign of a corrupted chart. So we don't want
					// to throw up the error message. Just skip it. See LT-7861.
					if (citems == 0)
					{
						// But if the empty row is the only one in the chart, we'll just delete it,
						// because we don't want the user to be able to have a chart for an empty text
						// consisting only of an empty row.
						// I don't trust 'crows' here because the chart contents could have been modified.
						// See LT-11436 -- GJM 2011.08.09
						if (m_chart.RowsOS.Count == 1)
						{
							m_chart.RowsOS[0].Delete();
						}
						continue;
					}
					// Under the new system, if a cellPart goes away and it's the last one, the row will go
					// automatically. We'll want to check, though, that we aren't loading things with null refs
					// such as MovedTextMarkers whose source WordGroup is gone or ClauseMarkers with one or more
					// missing clauses.
					// We also need to be careful. As we delete a word group, a side effect might be deleting
					// a moved text marker which points at it. Thus, a single iteration might remove more than
					// one thing from the list. It's also possible that the 'extra' one removed is earlier in
					// the list and thus we could skip one. The safest thing is to always restart the loop if
					// we delete one.
					for (var ipart = 0; ipart < citems; ipart++) // not foreach here, as we may delete some as we go
					{
						var curPart = curRow.CellsOS[ipart];
						if (curPart is IConstChartTag)
						{
							continue;
						}
						if (curPart is IConstChartClauseMarker)
						{
							if (!((IConstChartClauseMarker)curPart).HasValidRefs)
							{
								if (!ReportWarningAndUpdateCountsRemovingCellPart(curRow, curPart, ref fReported, ref ipart, ref citems))
								{
									irow--;
							}
							}
							continue;
						}
						if (curPart is IConstChartMovedTextMarker)
						{
							if (!((IConstChartMovedTextMarker)curPart).HasValidRef)
							{
								if (!ReportWarningAndUpdateCountsRemovingCellPart(curRow, curPart, ref fReported, ref ipart, ref citems))
								{
									irow--;
							}
							}
							continue;
						}
						// Do some further checking because it's a ConstChartWordGroup.
						var curWordGroup = curPart as IConstChartWordGroup;
						if (!curWordGroup.IsValidRef || !WordGroupTextMatchesChartText(curWordGroup))
						{
							if (!ReportWarningAndUpdateCountsRemovingCellPart(curRow, curPart, ref fReported, ref ipart, ref citems))
							{
								irow--;
							}
							continue; // Skip to next.
						}
						try
						{
							var occurrences = curWordGroup.GetOccurrences(); // Checks references for Wordforms
							if (occurrences.Count > 0)
							{
								continue;
						}
						}
						catch (NullReferenceException)
						{
							// This is a real problem, but not for the chart. There may be a WfiAnalysis or
							// WfiGloss with no owner!
						}
						// CCWordGroup is now empty, take it out of row!
						if (!ReportWarningAndUpdateCountsRemovingCellPart(curRow, curWordGroup, ref fReported, ref ipart, ref citems))
						{
							irow--;
						}
					} // cellPart loop
				} // row loop
				if (fReported)
				{
					RenumberRows(0, false); // We don't know where the change occurred. Better to be safe.
				}
			});
		}

		private bool WordGroupTextMatchesChartText(IConstChartWordGroup wg)
		{
			// Compares the hvo of our current chart's text to the hvo of the text
			// that this ConstChartWordGroup references. They'd better be the same!
			var wgText = wg.BeginSegmentRA.Paragraph.Owner as IStText;
			if (wgText == null || !wgText.IsValidObject)
			{
				return false;
			}
			return wgText.Hvo == Chart.BasedOnRA.Hvo; // But Chart IS set now!
		}

		/// <summary>
		/// Reports warning about chart messup (if it hasn't already been reported),
		/// deletes bad cell part, restarts cell loop. If this returns false, then
		/// the row index needs to be decremented (before the auto loop increment).
		/// </summary>
		private bool ReportWarningAndUpdateCountsRemovingCellPart(IConstChartRow row, IConstituentChartCellPart part, ref bool fReported, ref int index, ref int count)
		{
			//Debug.Assert(false, "About to delete cell part. Why!?");
			row.CellsOS.Remove(part);
			if (!fReported)
			{
				DisplayWarning();
				fReported = true;
			}
			// Restart the loop; we may have deleted additional items either before or after the one
			// we expect to.
			index = -1; // after auto-increment in for loop, starts over at beginning.
			// if we just removed the last cell in the row, the row will be deleted too!
			count = row.IsValidObject ? row.CellsOS.Count : 0;
			return row.IsValidObject; // if row gets deleted, returns false.
		}

		/// <summary>
		/// Overidden by test subclass to not display message.
		/// </summary>
		protected virtual void DisplayWarning()
		{
			MessageBox.Show(LanguageExplorerResources.ksTextEditWarning, LanguageExplorerResources.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		/// <summary>
		/// Returns a list of rows that should be eligible to choose from the Advanced... dialog row combo box.
		/// </summary>
		protected List<IConstChartRow> CollectEligibleRows(ChartLocation clickedCell, bool fPrepose)
		{
			// Collect all rows in the 'right' direction including the clicked one.
			var result = CollectRowsInSentence(clickedCell.Row, fPrepose);
			if (result.Count < 0)
			{
				return result;
			}
			// If we are marking Postposed from the first column, we don't want the last row to be eligible.
			// If we are marking Preposed from the last column, we don't want the first row to be eligible.
			if (fPrepose)
			{
				//int icolLast = IndexOfColumn(AllMyColumns[AllMyColumns.Length - 1].Hvo);
				var icolLast = AllMyColumns.Length - 1;
				if (clickedCell.ColIndex == icolLast)
				{
					result.RemoveAt(0);
			}
			}
			else
			{
				if (clickedCell.ColIndex == 0)
				{
					result.RemoveAt(result.Count - 1);
			}
			}
			return result;
		}

		/// <summary>
		/// Find and return the movedText marker that points to my cell from another row.
		/// Caller knows there should be one because of the 'movedText' feature on the text and no markers
		/// in the current row. Not limited to current sentence.
		/// </summary>
		private IConstChartMovedTextMarker FindMovedMarkerOtherRow(ChartLocation srcCell, bool fPrepose)
		{
			// The following LINQ gets only MovedTextMarkers from this chart
			//     that are pointing to WordGroups in srcCell's row
			//     and that match the fPrepose parameter (are pointing the right direction)
			var movedMkrList = m_movedTextRepo.AllInstances().Where(marker => marker.Owner.Owner.Hvo == Chart.Hvo &&
						  marker.Preposed == fPrepose &&
						  marker.WordGroupRA.Owner == srcCell.Row).ToList();
			return movedMkrList.FirstOrDefault(movedTextMarker => movedTextMarker.WordGroupRA.ColumnRA == AllMyColumns[srcCell.ColIndex]);
		}

		// Mark Moved Text from one cell to another(within a row).
		private void itemCol_Click(object sender, EventArgs e)
		{
			var item = sender as TwoColumnMenuItem;
			var srcCell = new ChartLocation(item.Row, item.Source);
			var dstCell = new ChartLocation(item.Row, item.Destination);
			if (item.Checked)
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksRedoMakeMoved, LanguageExplorerResources.ksUndoMakeMoved,
											Cache.ActionHandlerAccessor, () => RemoveMovedFrom(dstCell, srcCell));
			}
			else
			{
				var wordGrp = FindFirstWordGroup(PartsInCell(dstCell));
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMakeMoved, LanguageExplorerResources.ksRedoMakeMoved,
											Cache.ActionHandlerAccessor, () =>
				{
					IConstChartMovedTextMarker mtmarker;
					if (IsMovedText(wordGrp, out mtmarker))
					{
						var markerCell = new ChartLocation((IConstChartRow) mtmarker.Owner, IndexOfColumn(mtmarker.ColumnRA));
						RemoveMovedFrom(dstCell, markerCell);
					}
					MakeMovedFrom(dstCell, srcCell);
				});
			}
		}

		private void itemMoveForward_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			MoveCellForward(item.SrcCell);
		}

		private void itemMoveBack_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			MoveCellBack(item.SrcCell);
		}
		void itemMoveWordForward_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			MoveWordForward(item.SrcCell);
		}

		private void itemMoveWordBack_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			MoveWordBack(item.SrcCell);
		}

		private void itemMergeAfter_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			ToggleMergedCellFlag(item.SrcCell, true);
		}

		private void itemMergeBefore_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			ToggleMergedCellFlag(item.SrcCell, false);

		}

		private void GeneratePlMenuItems(ContextMenuStrip menu, ICmPossibilityList list, EventHandler clickHandler, ChartLocation cell)
		{
			var markerRefs = SelectListItemReferences(PartsInCell(cell));
			foreach (var poss in list.PossibilitiesOS)
			{
				menu.Items.Add(MakePlItem(clickHandler, cell, poss, markerRefs, LanguageExplorerResources.ksMarkMenuItemFormat));
			}
		}

		/// <summary>
		/// Make one item in a possibility-list menu. Includes making subitems if poss has subpossibilties.
		/// The leaves are the only really active choices. The item is checked if the specified cell
		/// already contains a pointer to this possibility.
		/// </summary>
		private ToolStripMenuItem MakePlItem(EventHandler clickHandler, ChartLocation cell, ICmPossibility poss, IEnumerable<IConstChartTag> markerRefs, string format)
		{
			var markerRefsAsList = markerRefs.ToList();
			var item = new RowColPossibilityMenuItem(cell, poss.Hvo);
			var label = poss.Name.BestAnalysisAlternative.Text ?? string.Empty;
			if (poss.SubPossibilitiesOS.Count == 0)
			{
				var abbr = poss.Abbreviation.AnalysisDefaultWritingSystem.Text;
				if (abbr != label && !string.IsNullOrEmpty(abbr))
				{
					label = label + " (" + abbr + ")";
				}
				item.Click += clickHandler; // can only select leaves.
				if (ListRefersToMarker(markerRefsAsList, poss.Hvo))
				{
					item.Checked = true;
			}
			}

			if (format != null)
			{
				label = string.Format(format, label);
			}
			item.Text = label;
			foreach (var poss2 in poss.SubPossibilitiesOS)
			{
				item.DropDownItems.Add(MakePlItem(clickHandler, cell, poss2, markerRefsAsList, null));
			}
			return item;
		}

		private static List<IConstChartTag> SelectListItemReferences(IEnumerable<IConstituentChartCellPart> input)
		{
			var result = new List<IConstChartTag>();
			result.AddRange(input.OfType<IConstChartTag>().Where(part => part.TagRA != null));
			return result;
		}

		private static bool ListRefersToMarker(IEnumerable<IConstChartTag> list, int hvoMarker)
		{
			return list.Any(chartTag => chartTag.TagRA.Hvo == hvoMarker);
		}

		private void ToggleMarker_Item_Click(object sender, EventArgs e)
		{
			var item = sender as RowColPossibilityMenuItem;

			AddOrRemoveMarker(item);
		}

		public void AddOrRemoveMarker(RowColPossibilityMenuItem item)
		{
			if (item.Checked)
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoRemoveMarker, LanguageExplorerResources.ksRedoRemoveMarker,
					Cache.ActionHandlerAccessor, () => RemoveListItemPart(item.SrcCell, item.m_hvoPoss));
			else
			{
				var icellPartInsertAt = FindIndexOfCellPartInLaterColumn(item.SrcCell);
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoAddMarker, LanguageExplorerResources.ksRedoAddMarker,
					Cache.ActionHandlerAccessor, () => MakeChartTag(item.SrcCell, item.m_hvoPoss, icellPartInsertAt));
			}
		}

		private void RemoveListItemPart(ChartLocation srcCell, int hvoMarker)
		{
			foreach (var part in PartsInCell(srcCell).Where(part => part is IConstChartTag && ((IConstChartTag)part).TagRA.Hvo == hvoMarker))
			{
				srcCell.Row.CellsOS.Remove(part);
				return;
			}
		}

		const int kdepClauseRowLimit = 5; // A limit to possible dependent clause menu rows.

		private ToolStripMenuItem MakeDepClauseItem(ChartLocation srcCell, int irowSrc, string mainLabel, ClauseTypes depType)
		{
			var itemMDC = new DisposableToolStripMenuItem(mainLabel);
			if (irowSrc > 0)
			{
				// put in just one 'previous clause' item.
				var item = new DepClauseMenuItem(LanguageExplorerResources.ksPreviousClauseMenuItem, srcCell, new [] { m_chart.RowsOS[irowSrc - 1] });
				item.Click += itemDC_Click;
				item.DepType = depType;
				itemMDC.DropDownItems.Add(item);
			}
			var depClauseRows = new List<IConstChartRow>(kdepClauseRowLimit);
			for (var irow = irowSrc + 1; irow < Math.Min(irowSrc + kdepClauseRowLimit, m_chart.RowsOS.Count); irow++)
			{
				string label;
				switch (irow - irowSrc)
				{
					case 1: label = LanguageExplorerResources.ksNextClauseMenuItem; break;
					case 2: label = LanguageExplorerResources.ksNextTwoClausesMenuItem; break;
					default: label = string.Format(LanguageExplorerResources.ksNextNClausesMenuItem, (irow - irowSrc)); break;
				}
				depClauseRows.Add(m_chart.RowsOS[irow]);
				var item = new DepClauseMenuItem(label, srcCell, depClauseRows.ToArray());
				item.Click += itemDC_Click;
				itemMDC.DropDownItems.Add(item);
				item.DepType = depType;
			}
			var itemOther = new DepClauseMenuItem(LanguageExplorerResources.ksOtherMenuItem, srcCell, null);
			itemMDC.DropDownItems.Add(itemOther);
			itemOther.Click += itemOther_Click;
			itemOther.DepType = depType;

			return itemMDC;
		}

		// Generates a dialog with each row except the one clicked (within reason)
		private void itemOther_Click(object sender, EventArgs e)
		{
			if (m_chart.RowsOS.Count == 1)
			{
				return;
			}
			var item = sender as DepClauseMenuItem;
			using (var dlg = new SelectClausesDialog())
			{
				var items = new List<RowMenuItem>();
				var iSrc = item.RowSource.IndexInOwner;
				var iSelect = -1;
				for (var irow = Math.Max(iSrc - 10, 0); irow < Math.Min(iSrc + 20, m_chart.RowsOS.Count); irow++)
				{
					if (irow == iSrc)
					{
						iSelect = items.Count;
						continue;
					}
					items.Add(new RowMenuItem(m_chart.RowsOS[irow]));
				}
				dlg.SetRows(items);
				if (iSelect >= items.Count)
				{
					iSelect = items.Count - 1;
				}
				dlg.SelectedRow = items[iSelect];
				if (dlg.ShowDialog() != DialogResult.OK)
				{
					return;
				}
					var outer = dlg.SelectedRow.Row;
				var index = outer.IndexInOwner;
				var start = iSrc + 1;
				var end = index;
					if (index < iSrc)
					{
						start = index;
						end = iSrc - 1;
					}
					var rows = new List<IConstChartRow>();
					for (var i = start; i <= end; i++)
				{
						rows.Add(m_chart.RowsOS[i]);
				}
					MakeDependentClauseMarker(item.SrcCell, rows.ToArray(), item.DepType);
				}
			}

		public static string EndParaFeatureName => "endPara";

		public static string EndSentFeatureName => "endSent";

		/// <summary>
		/// Invoked when the user chooses the "Row Ends Paragraph" menu item.
		/// Sender is a OneValMenuItem indicating the row.
		/// </summary>
		private void itemREP_Click(object sender, EventArgs e)
		{
			var item = sender as OneValMenuItem;
			bool fSentWasOn;
			var hvoRow = item.Source;
			var curRow = m_rowRepo.GetObject(hvoRow);
			var irow = curRow.IndexInOwner;

			if (curRow.EndParagraph)
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksRedoLastRowInPara, LanguageExplorerResources.ksUndoLastRowInPara,
					Cache.ActionHandlerAccessor, () => curRow.EndParagraph = false);
			}
			else
			{
				// Save EOS state for determining if we need to renumber rows
				fSentWasOn = curRow.EndSentence;

				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoLastRowInPara, LanguageExplorerResources.ksRedoLastRowInPara,
											Cache.ActionHandlerAccessor, () =>
				{
					// Set both EOP and EOS
					curRow.EndParagraph = true;
					curRow.EndSentence = true;

					// Turning on EOP only affects numbering if EOS was off before
					if (!fSentWasOn)
					{
						RenumberRows(irow, false);
					}
				});
			}
		}

		/// <summary>
		/// Invoked when the user chooses the "Row Ends Sentence" menu item.
		/// Sender is a OneValMenuItem indicating the row.
		/// </summary>
		private void itemRES_Click(object sender, EventArgs e)
		{
			var item = (OneValMenuItem)sender;
			var hvoRow = item.Source;
			var curRow = m_rowRepo.GetObject(hvoRow);
			var irow = curRow.IndexInOwner;

			if (curRow.EndSentence)
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksRedoLastRowInSent, LanguageExplorerResources.ksUndoLastRowInSent,
											Cache.ActionHandlerAccessor, () =>
				{
					// unchecking EOS, unchecks EOP too
					curRow.EndSentence = false;
					curRow.EndParagraph = false;
					// Now we need to renumber our row labels, unless we're on the last row.
					if (irow != m_chart.RowsOS.Count - 1)
					{
						RenumberRows(irow, false);
					}
				});
			}
			else
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoLastRowInSent, LanguageExplorerResources.ksRedoLastRowInSent,
											Cache.ActionHandlerAccessor, () =>
				{
					curRow.EndSentence = true;
					// Now we need to renumber our row labels, unless we're on the last row.
					if (irow != m_chart.RowsOS.Count - 1)
						RenumberRows(irow, false);
				});
		}
		}

		/// <summary>
		/// Invoked when the user clicks an item in add dependent/speech/song clause.
		/// </summary>
		private void itemDC_Click(object sender, EventArgs e)
		{
			var item = sender as DepClauseMenuItem;
			Debug.Assert(item != null);
			MakeDependentClauseMarker(item.SrcCell, item.DepClauses, item.DepType);
		}

		public const string mergeAfterTag = "mergeAfter";
		public const string mergeBeforeTag = "mergeBefore";

		/// <summary>
		/// Mark a cell as merging with cells to the left or to the right.
		/// Not allowed on empty cells.
		/// Can't have both left and right true; turn other off if new one turned on.
		/// </summary>
		public string ToggleMergedCellFlag(ChartLocation srcCell, bool following)
		{
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(LanguageExplorerResources.ksUndoMergeCells, LanguageExplorerResources.ksRedoMergeCells,
										Cache.ActionHandlerAccessor, () =>
			{
				var cellPart = FindCellPartInColumn(srcCell, false);
				if (following)
				{
					cellPart.MergesAfter = !cellPart.MergesAfter;
					// Make sure other direction is off
					if (cellPart.MergesAfter)
						cellPart.MergesBefore = false;
				}
				else
				{
					cellPart.MergesBefore = !cellPart.MergesBefore;
					// Make sure other direction is off
					if (cellPart.MergesBefore)
						cellPart.MergesAfter = false;
				}
			});
			return null;
		}

		public string GetColumnLabel(int icol)
		{
			return AllMyColumns[icol].Name.BestAnalysisAlternative.Text;
		}

		/// <summary>
		/// Creates the menu for a column's down arrow (context) button.
		/// </summary>
		public ContextMenuStrip InsertIntoChartContextMenu(int icol)
		{
			var menu = new ContextMenuStrip();

			var itemNewClause = new OneValMenuItem(LanguageExplorerResources.ksMoveHereInNewClause, icol);
			itemNewClause.Click += itemNewClause_Click;
			menu.Items.Add(itemNewClause);

			var itemMT = new DisposableToolStripMenuItem(LanguageExplorerResources.ksMovedFromMenuItem);
			for (var ihvo = 0; ihvo < AllMyColumns.Length; ihvo++)
			{
				if (ihvo == icol)
				{
					continue;
				}
				var item = new TwoColumnMenuItem(GetColumnLabel(ihvo), icol, ihvo);
				item.Click += InsertMovedText_Click;
				itemMT.DropDownItems.Add(item);
			}
			menu.Items.Add(itemMT);
			return menu;
		}

		private void itemMissingMarker_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			ToggleMissingMarker(item.SrcCell, item.Checked);
		}

		private void itemNewClause_Click(object sender, EventArgs e)
		{
			var item = sender as OneValMenuItem;
			MoveToHereInNewClause(item.Source);
		}

		private void InsertMovedText_Click(object sender, EventArgs e)
		{
			var item = sender as TwoColumnMenuItem;
			MakeMovedText(item.Destination, item.Source);

		}

		/// <summary>
		/// One of the four basic operations we use to implement moving things.
		/// This one moves cellParts from one column to another. Caller is responsible
		/// to ensure this is legitimate (i.e., will not make text out of order).
		/// (However, with appropriate safeguards it is OK with a PropChanged on the
		/// row's Cells sequence.
		/// </summary>
		public virtual void ChangeColumn(IConstituentChartCellPart[] partsToMove, ICmPossibility newCol, IConstChartRow row)
		{
			Debug.Assert(row.CellsOS.Contains(partsToMove[0]));
			ChangeColumnInternal(partsToMove, newCol);
		}

		/// <summary>
		/// Implementation of ChangeColumn, but not responsible for PropChanged messages.
		/// Can also be used as part of a move from end of one row to oppposite end of another.
		/// protected virtual only for override in testing.
		/// </summary>
		protected internal virtual void ChangeColumnInternal(IConstituentChartCellPart[] partsToMove, ICmPossibility newCol)
		{
			foreach (var part in partsToMove)
			{
				part.ColumnRA = newCol;
		}
		}

		/// <summary>
		/// One of the basic move operations. Moves CellPart objects from one row to another. Caller is responsible
		/// to ensure this is legitimate (i.e., will not make text out of order).
		/// </summary>
		public virtual void ChangeRow(IConstituentChartCellPart[] partsToMove, IConstChartRow rowSrc, IConstChartRow rowDst, int srcIndex, int dstIndex)
		{
			if (rowSrc == null || rowSrc.CellsOS.Count <= srcIndex)
			{
				throw new ArgumentOutOfRangeException(nameof(srcIndex));
			}

			if (rowDst == null || rowDst.CellsOS.Count <= dstIndex)
			{
				throw new ArgumentOutOfRangeException(nameof(dstIndex));
			}
			Debug.Assert(rowSrc.CellsOS[srcIndex] == partsToMove[0]);

			rowSrc.CellsOS.MoveTo(srcIndex, srcIndex + partsToMove.Length - 1, rowDst.CellsOS, dstIndex);
		}

		/// <summary>
		/// Basic move operation, moves all WordGroup Analyses into another. Caller is
		/// responsible for validity. Takes Source WordGroup and expands its coverage
		/// of Analyses to those of the Destination WordGroup.
		/// N.B. Leaves source to be deleted by the caller, since the destination now
		/// duplicates the source's coverage.
		/// </summary>
		public virtual void MoveAnalysesBetweenWordGroups(IConstChartWordGroup srcWordGrp, IConstChartWordGroup dstWordGrp)
		{
			if (srcWordGrp.IsAfter(dstWordGrp))
			{
				// dstWordGrp endPoint needs to change to accomodate up to srcWordGrp's endPoint
				dstWordGrp.EndSegmentRA = srcWordGrp.EndSegmentRA;
				dstWordGrp.EndAnalysisIndex = srcWordGrp.EndAnalysisIndex;
			}
			else
			{
				// dstWordGrp begPoint needs to change to accomodate up to srcWordGrp's begPoint
				dstWordGrp.BeginSegmentRA = srcWordGrp.BeginSegmentRA;
				dstWordGrp.BeginAnalysisIndex = srcWordGrp.BeginAnalysisIndex;
			}
		}

		/// <summary>
		/// Delete a specified group of CellParts (from the row and also from the database).
		/// Mainly made a method for simplified testing of callers.
		/// </summary>
		public virtual void DeleteCellParts(IConstChartRow row, int ihvo, int chvo)
		{
			for (var i = 0; i < chvo; i++)
			{
				row.CellsOS.RemoveAt(ihvo);
		}
		}

		/// <summary>
		/// Merge the contents of the source cell into the destination cell (at the start if forward is true,
		/// otherwise at the end).
		/// Caller deals with UOW.
		/// </summary>
		protected virtual void MergeCellContents(ChartLocation srcCell, ChartLocation dstCell, bool forward)
		{
			new MergeCellContentsMethod(this, srcCell, dstCell, forward).Run();
			m_lastMoveCell = dstCell;
		}

		/// <summary>
		/// Given a list of cellParts (typically in a single cell), find the last one (if any) that is actually
		/// a WordGroup. Since we are now going to have the possibility of more than one WordGroup per cell
		/// we are trying a (faster?) different way of doing this.
		/// </summary>
		protected internal static IConstChartWordGroup FindLastWordGroup(List<IConstituentChartCellPart> cellParts)
		{
			var temp = cellParts.GetRange(0, cellParts.Count); // Make a shallow copy to avoid side-effects.
			temp.Reverse(); // returns 'void', can't embed in line above or below.
			return FindFirstWordGroup(temp);
		}

		/// <summary>
		/// Given a list of cellParts (typically in a single cell), find the first one (if any) that is actually
		/// a WordGroup.
		/// </summary>
		protected internal static IConstChartWordGroup FindFirstWordGroup(List<IConstituentChartCellPart> cellParts)
		{
			return cellParts.OfType<IConstChartWordGroup>().FirstOrDefault();
		}

		/// <summary>
		/// Given a list of cellparts (typically in a single cell), collect a list of all the ones
		/// that are actually CCWordGroups.
		/// </summary>
		protected internal List<IConstChartWordGroup> CollectAllWordGroups(List<IConstituentChartCellPart> partsInCell)
		{
			return partsInCell.OfType<IConstChartWordGroup>().ToList();
		}

		/// <summary>
		/// Given a list of cell parts (typically in a single cell) and the index of the one we're on,
		/// find the next one in the list that is a WordGroup.
		/// Returns null if no WordGroup is found.
		/// </summary>
		/// <param name="partsInCell"></param>
		/// <param name="iStartPart">The index of the current WordGroup, a starting point.
		/// Index is relative to list, NOT to row Cells!</param>
		/// <param name="indexFoundAt">The index of the next WordGroup, if found. Value will be -1, if not found.
		/// Index is relative to list, NOT to row Cells!</param>
		/// <returns></returns>
		protected internal IConstChartWordGroup FindNextWordGroup(List<IConstituentChartCellPart> partsInCell, int iStartPart, out int indexFoundAt)
		{
			indexFoundAt = -1;
			for (var i = iStartPart + 1; i < partsInCell.Count; i++)
			{
				if (!(partsInCell[i] is IConstChartWordGroup))
				{
					continue;
				}
				indexFoundAt = i;
				return (IConstChartWordGroup)partsInCell[i];
			}
			return null;
		}

		/// <summary>
		/// Given a list of CellParts (typically in a single cell), find the next one
		/// in the list that is actually a WordGroup. This overload starts at the beginning
		/// of the list of CellParts. The out var returns the index within the list
		/// (NOT within the row).
		/// Returns null if no WordGroup is found.
		/// </summary>
		protected internal IConstChartWordGroup FindNextWordGroup(List<IConstituentChartCellPart> partsInCell, out int indexFoundAt)
		{
			return FindNextWordGroup(partsInCell, -1, out indexFoundAt);
		}

		/// <summary>
		/// Given a list of cell parts (typically in a single cell) and the index of the one we're on,
		/// find the previous WordGroup in the list (if any).
		/// </summary>
		/// <param name="partsInCell"></param>
		/// <param name="icurrPart">routine updates icurrPart to index of previous WordGroup</param>
		/// <returns></returns>
		protected internal IConstChartWordGroup FindPreviousWordGroup(List<IConstituentChartCellPart> partsInCell, ref int icurrPart)
		{
			for (var i = icurrPart - 1; i > -1; i--)
			{
				if (!(partsInCell[i] is IConstChartWordGroup)) continue;
				icurrPart = i;
				return partsInCell[i] as IConstChartWordGroup;
			}
			return null;
		}

		/// <summary>
		/// Move a cell's contents back (typically left). If the next cell is empty, just change columns.
		/// If the next cell is occupied, merge the contents. Wraps around in first column. Does nothing in
		/// very first cell.
		/// </summary>
		public void MoveCellBack(ChartLocation srcCell)
		{
			ChartLocation prevCell;
			if (srcCell.ColIndex == 0)
			{
				// Move to start of previous row.
				var prevRow = PreviousRow(srcCell.Row);
				// Enhance: If this is the first row, should we insert a row and then move the cell?
				if (prevRow == null)
				{
					return; // can't go back further.
				}
				prevCell = new ChartLocation(prevRow, AllMyColumns.Length - 1);
			}
			else
			{
				// Normal case, merging/moving to cell on same row.
				prevCell = new ChartLocation(srcCell.Row, srcCell.ColIndex - 1);
			}
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveCellBack, LanguageExplorerResources.ksRedoMoveCellBack,
										Cache.ActionHandlerAccessor, () => MergeCellContents(srcCell, prevCell, false));
		}

		private IConstChartRow PreviousRow(IConstChartRow row)
		{
			var index = row.IndexInOwner;
			return index == 0 ? null : Chart.RowsOS[index - 1];
		}

		private IConstChartRow NextRow(IConstChartRow row)
		{
			var index = row.IndexInOwner;
			var maxRows = Chart.RowsOS.Count;
			return index >= maxRows - 1 ? null : Chart.RowsOS[index + 1];
		}

		/// <summary>
		/// Move a cell's contents forward (typically right). If the next cell is empty, just change columns.
		/// If the next cell is occupied, merge the contents. Wraps around in last column. Does nothing in
		/// very last cell.
		/// </summary>
		public void MoveCellForward(ChartLocation srcCell)
		{
			ChartLocation dstCell;

			if (srcCell.ColIndex == AllMyColumns.Length - 1)
			{
				// Move to start of next row.
				var nextRow = NextRow(srcCell.Row);
				if (nextRow == null)
				{
					return; // can't move further.
				}
				dstCell = new ChartLocation(nextRow, 0);
			}
			else
			{
				// Normal case, merging/moving to cell on same row.
				dstCell = new ChartLocation(srcCell.Row, srcCell.ColIndex + 1);
			}
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveCellForward, LanguageExplorerResources.ksRedoMoveCellForward,
										Cache.ActionHandlerAccessor, () => MergeCellContents(srcCell, dstCell, true));
		}

		/// <summary>
		/// Decrement a row index without going negative.
		/// </summary>
		internal static int DecrementRowSafely(int irow)
		{
			return Math.Max(0, irow - 1);
		}

		/// <summary>
		/// Move the (first) word in a cell back (typically left). If the previous cell is empty, make a new
		/// WordGroup there; otherwise, move it into the WordGroup in the cell right. In the first column,
		/// moves to previous row. If there is only one word in the cell, merge everything into the destination.
		/// </summary>
		public void MoveWordBack(ChartLocation srcCell)
		{
			ChartLocation dstCell;
			var listOfPartsInSrc = PartsInCell(srcCell);
			int ipartInCell;
			// Start looking at the beginning of the cell
			var srcWordGroup = FindNextWordGroup(listOfPartsInSrc, out ipartInCell);
			if (srcWordGroup == null)
			{
				return;
			}

			if (!TryGetPreviousCell(srcCell, out dstCell))
			{
				return;
			}
			// If there's only one wordform in the WordGroup and no other WordGroups, just merge the cell contents.
			// N.B. If the first WordGroup is not the first cellPart in the cell, we can't use "1" below!!!!
			if (srcWordGroup.GetOccurrences().Count() == 1 && (listOfPartsInSrc.Count == ipartInCell + 1 || FindFirstWordGroup(listOfPartsInSrc.GetRange(ipartInCell + 1, listOfPartsInSrc.Count - 1)) == null))
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveWord, LanguageExplorerResources.ksRedoMoveWord,
											Cache.ActionHandlerAccessor, () => MergeCellContents(srcCell, dstCell, false));
				return;
			}

			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveWord, LanguageExplorerResources.ksRedoMoveWord,
										Cache.ActionHandlerAccessor, () =>
			{
				// If the destination contains a missing marker get rid of it! Don't try to 'merge' with it.
				RemoveMissingMarker(dstCell);

				bool fKeepOldWordGroup;
				var wordGrpTarget = FindLastWordGroup(PartsInCell(dstCell));
				var wordToMove = new[] {srcWordGroup.GetOccurrences()[0]};
				if (wordGrpTarget == null)
				{
					// Make a new WordGroup and move one word
					MakeWordGroup(dstCell, FindIndexOfCellPartInLaterColumn(dstCell), wordToMove);
					fKeepOldWordGroup = srcWordGroup.ShrinkFromBeginning(true);
				}
				else
				{
					fKeepOldWordGroup = srcWordGroup.ShrinkFromBeginning(true);
					wordGrpTarget.GrowFromEnd(true);
				}
				// Enhance GordonM: If we eventually allow a marker to show that words within a cell are reversed in order,
				// that marker may need deleting here.
				if (!fKeepOldWordGroup)
				{
					DeleteCellPart(srcWordGroup);
				}
			});
			m_lastMoveCell = dstCell;
		}

		/// <summary>
		/// Move the (last) word in a cell forward (typically right). If the next cell is empty, make a new
		/// WordGroup there; otherwise, move it into the WordGroup in the cell right. In the last column, it moves to
		/// the next row. If there is only one word in the cell, merge everything into the destination.
		/// </summary>
		public void MoveWordForward(ChartLocation srcCell)
		{
			ChartLocation dstCell;
			var listOfPartsInSrc = PartsInCell(srcCell);
			var ipartInCell = listOfPartsInSrc.Count; // start at end of cell and work backwards
			var srcWordGroup = FindPreviousWordGroup(listOfPartsInSrc, ref ipartInCell);
			if (srcWordGroup == null)
			{
				return;
			}

			if (!TryGetNextCell(srcCell, out dstCell))
			{
				return;
			}
			// If there's only one wordform in the WordGroup and no other WordGroups, just merge the cell contents.
			if (srcWordGroup.GetOccurrences().Count == 1 && (listOfPartsInSrc.Count == 1 || (ipartInCell > 0 && FindLastWordGroup(listOfPartsInSrc.GetRange(0, ipartInCell - 1)) == null)))
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveWord, LanguageExplorerResources.ksRedoMoveWord,
											Cache.ActionHandlerAccessor, () => MergeCellContents(srcCell, dstCell, true));
				return;
			}

			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveWord, LanguageExplorerResources.ksRedoMoveWord,
										Cache.ActionHandlerAccessor, () =>
			{
				// If the destination contains a missing marker get rid of it! Don't try to 'merge' with it.
				RemoveMissingMarker(dstCell);

				var fKeepOldWordGroup = true;
				var partsInDestCell = PartsInCell(dstCell);
				var wordGrpTarget = FindFirstWordGroup(partsInDestCell);
				var wordToMove = new AnalysisOccurrence(srcWordGroup.EndSegmentRA, srcWordGroup.EndAnalysisIndex);
				if (wordGrpTarget == null)
				{
					var iinsertAt = FindIndexOfFirstCellPartInOrAfterColumn(dstCell);
					// If there is a preposed marker in the same column at this index, increment index.
					if (partsInDestCell.Count > 0 && IsPreposedMarker(partsInDestCell[0]))
					{
						iinsertAt++;
					}
					// Make a new WordGroup and move one word
					MakeWordGroup(dstCell, iinsertAt, new [] { wordToMove });
					fKeepOldWordGroup = srcWordGroup.ShrinkFromEnd(true);
				}
				else
				{
					fKeepOldWordGroup = srcWordGroup.ShrinkFromEnd(true);
					wordGrpTarget.GrowFromBeginning(true);
				}
				// Enhance GordonM: If we eventually allow a marker to show that words within a cell are reversed in order,
				// that marker may need deleting here.
				if (!fKeepOldWordGroup)
				{
					DeleteCellPart(srcWordGroup);
				}
			});
			m_lastMoveCell = dstCell;
		}

		internal bool TryGetNextCell(ChartLocation srcCell)
		{
			ChartLocation dummy;
			return TryGetNextCell(srcCell, out dummy);
		}

		internal bool TryGetPreviousCell(ChartLocation srcCell)
		{
			ChartLocation dstCell;
			return TryGetPreviousCell(srcCell, out dstCell);
		}

		private bool TryGetNextCell(ChartLocation srcCell, out ChartLocation dstCell)
		{
			var icolDst = srcCell.ColIndex + 1;
			var rowDst = srcCell.Row;
			if (icolDst >= AllMyColumns.Length)
			{
				icolDst = 0;
				rowDst = NextRow(srcCell.Row);
			}
			dstCell = new ChartLocation(rowDst, icolDst);
			return rowDst != null;
		}

		private bool TryGetPreviousCell(ChartLocation srcCell, out ChartLocation dstCell)
		{
			var icolDst = srcCell.ColIndex - 1;
			var rowDst = srcCell.Row;
			if (icolDst < 0)
			{
				icolDst = m_allMyColumns.Length - 1;
				rowDst = PreviousRow(srcCell.Row);
			}
			dstCell = new ChartLocation(rowDst, icolDst);
			return rowDst != null;
		}

		private static void DeleteCellPart(IConstituentChartCellPart wordGrp)
		{
			var row = (IConstChartRow)wordGrp.Owner;
			// delete the old CellPart, which is now empty
			row.CellsOS.Remove(wordGrp);
		}

		#endregion context menu

		internal static ListView MakeHeaderGroups()
		{
			return new ListView();
		}

		internal void MakeMainHeaderCols(ListView view)
		{
			// This is actually a display method, not a true 'logic' method.
			// That's why we need to test for RTL script.
			view.SuspendLayout();
			view.Columns.Clear();

			if (ChartIsRtL)
			{
				MakeNotesColumnHeader(view);
				MakeTemplateColumnHeaders(view);
				MakeRowNumberColumnHeader(view);
			}
			else
			{
				MakeRowNumberColumnHeader(view);
				MakeTemplateColumnHeaders(view);
				MakeNotesColumnHeader(view);
			}

			view.ResumeLayout();
		}

		private static void MakeNotesColumnHeader(ListView view)
		{
			// Add one more column for notes.
			var ch = new ColumnHeader
			{
				Text = LanguageExplorerResources.ksNotesColumnHeader
			};
			view.Columns.Add(ch);
		}

		private void MakeTemplateColumnHeaders(ListView view)
		{
			foreach (var col in AllMyColumns)
			{
				var ch = new ColumnHeader
				{
					Text = col.Name.BestAnalysisAlternative.Text.Normalize()
				};
				if (ChartIsRtL)
				{
					view.Columns.Insert(1, ch); // should be safe because the Notes column will get added first.
				}
				else
				{
					view.Columns.Add(ch);
			}
		}
		}

		private static void MakeRowNumberColumnHeader(ListView view)
		{
			var ch = new ColumnHeader
			{
				Text = string.Empty
			};
			// otherwise default is 'column header'!
			view.Columns.Add(ch);
		}

		/// <summary>
		/// Given a set of column positions, return the one 'x' is in,
		/// that is, the largest index in positions such that x is less than positions[i+1].
		/// </summary>
		public int GetColumnFromPosition(int x, int[] positions)
		{
			for (var i = 1; i < positions.Length; i++)
			{
				if (positions[i] > x)
				{
					return i - 1;
				}
			}
			return positions.Length - 1;
		}


		/// <summary>
		/// Answer true if the HVO is a ConstChartClauseMarker (a placeholder for a dependent
		/// clause or speech or song). This is determined by checking its class, then that it
		/// references at least one ConstituentChartRow.
		/// </summary>
		internal bool IsClausePlaceholder(int hvo, out int hvoItem)
		{
			var part = m_cellPartRepo.GetObject(hvo);
			hvoItem = (int)SpecialHVOValues.kHvoUninitializedObject;
			var clauses = (part as IConstChartClauseMarker)?.DependentClausesRS;
			if (clauses?.Count > 0)
				{
					hvoItem = clauses[0].Hvo;
					return true;
				}
			return false;
		}

		internal bool IsClausePlaceholder(int hvo)
		{
			int dummy;
			return IsClausePlaceholder(hvo, out dummy);
		}

		/// <summary>
		/// Answer true if the HVO is a IConstChartWordGroup.
		/// </summary>
		internal bool IsWordGroup(int hvo)
		{
			return m_cellPartRepo.GetObject(hvo) is IConstChartWordGroup;
		}

		/// <summary>
		/// Gets bookmarkable wordform from the ribbon.
		/// </summary>
		/// <returns>hvo to bookmark or 0 to do nothing.</returns>
		internal AnalysisOccurrence GetUnchartedWordForBookmark()
		{
			if (m_chart == null || !m_chart.IsValidObject || m_chart.RowsOS.Count == 0)
			{
				return null; // No chart! Don't want to change any bookmark that might already be set.
			}

			// If we aren't done charting the text yet, we need to set the bookmark
			// to the first uncharted wordform in the ribbon
			var analysisArray = NextUnchartedInput(1);
			return analysisArray.Length > 0 ? analysisArray[0] : null;
		}

		/// <summary>
		/// Answer true if wordGrp is a moved text item, by looking at its back-refs.
		/// </summary>
		internal static bool IsMovedText(IConstituentChartCellPart wordGrp)
		{
			IConstChartMovedTextMarker dummy;
			return IsMovedText(wordGrp, out dummy);
		}

		/// <summary>
		/// Answer true if wordGrp is a moved text item, by looking at its back-refs.
		/// </summary>
		/// <param name="wordGrp"></param>
		/// <param name="result">out param: MovedText Marker that references wordGrp</param>
		/// <returns></returns>
		internal static bool IsMovedText(IConstituentChartCellPart wordGrp, out IConstChartMovedTextMarker result)
		{
			// Enhance Gordon: Is there a better way!?
			result = null;
			if (!(wordGrp is IConstChartWordGroup))
			{
				return false;
			}
			result = wordGrp.ReferringObjects.Where(target => target is IConstChartMovedTextMarker).Cast<IConstChartMovedTextMarker>().FirstOrDefault();
			return result != null;
		}

		/// <summary>
		/// Return a suitable style tag depending on whether hvo is the first or later
		/// moved text item in its row.
		/// </summary>
		internal string MovedTextTag(int hvoTarget)
		{
			return HasPreviousMovedItemOnLine(m_chart, hvoTarget) ? "movedText2" : "movedText";
		}

		internal static bool HasPreviousMovedItemOnLine(IDsConstChart chart, int hvoTarget)
		{
			foreach (var row in chart.RowsOS)
			{
				var cPrevMovedText = 0;
				foreach (var part in row.CellsOS)
				{
					if (part.Hvo == hvoTarget)
					{
						return cPrevMovedText != 0;
					}

					if (IsMovedText(part))
					{
						cPrevMovedText++;
				}
			}
			}
			return false; // desperation, never found it!
		}

		/// <summary>
		/// Return true if the cell(irow, icol) should be highlighted to indicate a valid ChOrph insertion point.
		/// </summary>
		internal bool IsHighlightedCell(int irow, int icol)
		{
			if (!IsChOrphActive)
			{
				return false;
			}
			var irowPrec = CurrHighlightCells[0];
			var irowFoll = CurrHighlightCells[2];
			if (irowPrec > irow || irow > irowFoll)
				{
				return false;
			}
			var icolPrec = CurrHighlightCells[1];
			var icolFoll = CurrHighlightCells[3];
					if (irowPrec == irowFoll)
			{
						return (icolPrec <= icol && icol <= icolFoll);
			}

					if (irow == irowPrec && icol >= icolPrec)
			{
						return true;
				}
			return irow == irowFoll && icol <= icolFoll;
		}

		/// <summary>
		/// Makes a MovedText marker in a cell pointing to a WordGroup object.
		/// </summary>
		internal IConstChartMovedTextMarker MakeMTMarker(ChartLocation cell, IConstChartWordGroup wordGrp, int iInsertMrkerHereInRow)
		{
			return MakeMTMarker(cell, wordGrp, iInsertMrkerHereInRow, IsPreposed(new ChartLocation((IConstChartRow)wordGrp.Owner, IndexOfColumnForCellPart(wordGrp)), cell));
		}

		/// <summary>
		/// Most complete version.
		/// </summary>
		internal IConstChartMovedTextMarker MakeMTMarker(ChartLocation cell, IConstChartWordGroup wordGrp, int iInsertMrkerHereInRow, bool fPreposed)
		{
			return m_movedTextFact.Create(cell.Row, iInsertMrkerHereInRow, AllMyColumns[cell.ColIndex], fPreposed, wordGrp);
		}

		public int LogicalColumnIndexFromDisplay(int icol)
		{
			if (ChartIsRtL && icol > -1)
			{
				icol = ConvertColumnIndexToFromRtL(icol, AllMyColumns.Length - 1);
			}
			return icol;
		}

		internal int ConvertColumnIndexToFromRtL(int icol, int imaxCol)
		{
			// RTL chart 'logical' column indices are reverse from 'display' column indices.
			var diff = ((float)imaxCol)/2 - icol;
			icol += (int)(diff * 2);
			return icol;
		}
	}
}
