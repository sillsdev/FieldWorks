using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.FDO.Application;
using XCore;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// This class is responsible for the testable business logic of the constituent chart.
	/// Enhance: some or all of this logic could possibly be moved to suitable methods of DsConstChart itself.
	/// Since nothing else wants it yet, it is easier to keep all the logic related to the chart with the
	/// UI, but carefully separated (i.e., into this class).
	/// </summary>
	public class ConstituentChartLogic
	{
		protected int m_hvoStText;
		protected IInterlinRibbon m_ribbon;
		private IDsConstChart m_chart;
		private ChartLocation m_lastMoveCell; // row and column of last Move operation
		private IHelpTopicProvider m_helpTopicProvider;

		// Various Factories and Repositories

		#region Factories and Repositories

		private readonly IStTextRepository m_textRepo;
		private readonly ISegmentRepository m_segRepo;
		private readonly IAnalysisRepository m_analysisRepo;
		private readonly IConstChartRowFactory m_rowFact;
		private readonly IConstChartRowRepository m_rowRepo;
		private readonly IConstituentChartCellPartRepository m_cellPartRepo;
		private readonly IConstChartWordGroupRepository m_wordGrpRepo;
		private readonly IConstChartWordGroupFactory m_wordGrpFact;
		private readonly IConstChartMovedTextMarkerRepository m_movedTextRepo;
		private readonly IConstChartMovedTextMarkerFactory m_movedTextFact;
		private readonly IConstChartClauseMarkerRepository m_clauseMkrRepo;
		private readonly IConstChartClauseMarkerFactory m_clauseMkrFact;
		private readonly IConstChartTagRepository m_chartTagRepo;
		private readonly IConstChartTagFactory m_chartTagFact;
		private readonly ICmPossibilityRepository m_possRepo;
		private readonly ITsStrFactory m_tssFact;

		#endregion

		private ICmPossibility[] m_allMyColumns;
		private Set<int> m_indexGroupEnds; // indices of ends of column Groups (for LT-8104; setting apart Nucleus)
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

		public ConstituentChartLogic(FdoCache cache, IDsConstChart chart, int hvoStText)
			: this(cache)
		{
			m_hvoStText = hvoStText;
			m_chart = chart;
		}

		/// <summary>
		/// Make one and set the other stuff later.
		/// </summary>
		/// <param name="cache"></param>
		public ConstituentChartLogic(FdoCache cache)
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
			m_chartTagRepo	= servLoc.GetInstance<IConstChartTagRepository>();
			m_chartTagFact	= servLoc.GetInstance<IConstChartTagFactory>();
			m_analysisRepo	= servLoc.GetInstance<IAnalysisRepository>();
			m_cellPartRepo	= servLoc.GetInstance<IConstituentChartCellPartRepository>();
			m_possRepo		= servLoc.GetInstance<ICmPossibilityRepository>();
			m_tssFact		= servLoc.GetInstance<ITsStrFactory>();
		}

		public void Init(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
		}

		#region kflid Constants
		const int kflidAbbreviation		= CmPossibilityTags.kflidAbbreviation;
		const int kflidRows				= DsConstChartTags.kflidRows;

		const int kflidParagraphs		= StTextTags.kflidParagraphs;
		#endregion

		protected internal FdoCache Cache { get; protected set; }

		/// <summary>
		/// An array of 4 integers representing 4 indices; that of first row, first column, last row, and last column.
		/// </summary>
		internal int[] CurrHighlightCells
		{
			// Enhance GordonM: This really ought to use a pair of ChartLocation objects.
			get
			{
				if (m_currHighlightCells == null)
				{
					m_currHighlightCells = new int[4] { -1, -1, -1, -1 };
				}
				return m_currHighlightCells;
			}
			set
			{
				var irowOld = 0;
				var crowOld = 0;
				if (IsChOrphActive)
				{
					crowOld = 1;
					irowOld = m_currHighlightCells[0];
					if (m_currHighlightCells[2] != irowOld)
						crowOld++;
				}
				m_currHighlightCells = value;
				if (m_hvoStText != 0 && IsChOrphActive)
				{
					int crowNew = 1;
					int irowNew = m_currHighlightCells[0];
					if (m_currHighlightCells[2] != irowNew)
						crowNew++;
					//Cache.PropChanged(m_chart.Hvo, kflidRows, irowNew, crowNew, crowNew);
				}
				if (m_hvoStText != 0 && crowOld > 0) // Some danger of repeating myself, but probably worth it.
				{
					//Cache.PropChanged(m_chart.Hvo, kflidRows, irowOld, crowOld, crowOld);
				}
			}
		}

		/// <summary>
		/// Returns true if CurrHighlightCells has been set (i.e. ChOrph input is set up and pending).
		/// </summary>
		/// <returns></returns>
		internal bool IsChOrphActive
		{
			get { return CurrHighlightCells[0] > -1; }
		}

		/// <summary>
		/// Repeat the most recent move (forward).
		/// </summary>
		public void RepeatLastMoveForward()
		{
			if (m_lastMoveCell != null && m_lastMoveCell.IsValidLocation)
				MoveCellForward(m_lastMoveCell);
		}

		/// <summary>
		/// Repeat the most recent move (back).
		/// </summary>
		public void RepeatLastMoveBack()
		{
			if (m_lastMoveCell != null && m_lastMoveCell.IsValidLocation)
				MoveCellBack(m_lastMoveCell);
		}

		public bool CanRepeatLastMove
		{
			get { return m_lastMoveCell != null && m_lastMoveCell.IsValidLocation; }
		}

		public IDsConstChart Chart
		{
			get { return m_chart; }
			set {
				if (m_chart == null && value == null || (m_chart != null && m_chart.Equals(value)))
					return; // no change.
				m_chart = value;
				m_currHighlightCells = null; // otherwise we try to clear the old ones when ribbon changed event happens!
			}
		}

		public int StTextHvo
		{
			get { return m_hvoStText; }
			set { m_hvoStText = value; }
		}

		/// <summary>
		/// Returns an array of all the columns(Hvos) for the template of the chart that this logic is initialized with.
		/// </summary>
		public ICmPossibility[] AllMyColumns
		{
			get
			{
				if (m_allMyColumns == null)
					m_allMyColumns = AllColumns(m_chart.TemplateRA).ToArray();
				return m_allMyColumns;
			}
		}

		/// <summary>
		/// Returns an array of all the columns for the template of the chart that are the ends of column groups.
		/// </summary>
		public Set<int> GroupEndIndices
		{
			get
			{
				return m_indexGroupEnds;
			}
			set
			{
				m_indexGroupEnds = value;
			}
		}

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
			string engLabel = AllMyColumns[icol].Name.get_String(Cache.WritingSystemFactory.GetWsFromStr("en")).Text;
			return (engLabel == "Subject" || engLabel == "Verb");
		}

		public IInterlinRibbon Ribbon
		{
			get { return m_ribbon; }
			set { m_ribbon = value; }
		}

		/// <summary>
		/// Returns true if there is no more uncharted text.
		/// </summary>
		public bool IsChartComplete
		{
			get
			{
				var result = NextUnchartedInput(1);
				return (result.Length == 0);
			}
		}

		/// <summary>
		/// This routine raises the ribbon changed event. It should get run whenever there is a PropChanged
		/// on the Ribbon's OccurenceListId.
		/// </summary>
		internal void RaiseRibbonChgEvent()
		{
			if (Ribbon_Changed != null)
				Ribbon_Changed(this, new EventArgs()); // raise event that Ribbon has changed
		}

		/// <summary>
		/// Return the next wordforms that have not yet been added to the chart (up to maxContext of them).
		/// This overload assumes the current StText.
		/// </summary>
		/// <param name="maxContext"></param>
		/// <returns></returns>
		public AnalysisOccurrence[] NextUnchartedInput(int maxContext)
		{
			var txt = m_textRepo.GetObject(m_hvoStText);
			return NextUnchartedInput(txt, maxContext);
		}

		/// <summary>
		/// Return the next wordforms that have not yet been added to the chart (up to maxContext of them).
		/// </summary>
		/// <param name="curText"></param>
		/// <param name="maxContext"></param>
		/// <returns></returns>
		public AnalysisOccurrence[] NextUnchartedInput(IStText curText, int maxContext)
		{
			if (m_chart == null || curText.Hvo < 1)
				return new AnalysisOccurrence[0];
			// Try brute force first. If too slow, maybe we'll have to implement a chartingCache
			// that keeps track of all the Seg/Analysis index combinations and whether they're charted or not.
			var myParas = curText.ParagraphsOS;
			if (myParas == null || myParas.Count == 0)
				return new AnalysisOccurrence[0];

			var allSegments = new List<ISegment>();
			foreach (var myPara in myParas.Cast<IStTxtPara>().Where(myPara => myPara != null))
				allSegments.AddRange(myPara.SegmentsOS);

			// Get a list (in order) of all wordforms in this text.
			// But to save time, we'll just cache them as hvo and index tuples.
			var wordformRefsInThisText = new List<Tuple<int, int>>(); // T1:hvoSeg, T2: iAnalysis
			foreach (var seg in allSegments)
			{
				for (var i = 0; i < seg.AnalysesRS.Count; i++)
				{
					if (seg.AnalysesRS[i].HasWordform)
						wordformRefsInThisText.Add(new Tuple<int, int>(seg.Hvo, i));
				}
			}

			// Get a set of all AnalysisOccurrence objects currently in the chart.
			var chartedTargets = new Set<AnalysisOccurrence>();
			foreach (var cellPart in m_chart.RowsOS.SelectMany(
				row => row.CellsOS.OfType<IConstChartWordGroup>()))
			{
				chartedTargets.AddRange((cellPart).GetOccurrences());
			}

			// Figure out which words are NOT charted
			foreach (var pointRef in chartedTargets.Select(point => new Tuple<int, int>(
							point.Segment.Hvo, point.Index)).Where(wordformRefsInThisText.Contains))
				wordformRefsInThisText.Remove(pointRef);
			var resultLength = Math.Min(wordformRefsInThisText.Count, maxContext);
			var result = new AnalysisOccurrence[resultLength];
			for (var i = 0; i < resultLength; i++)
				result[i] = new AnalysisOccurrence(
					m_segRepo.GetObject(wordformRefsInThisText[i].Item1),
					wordformRefsInThisText[i].Item2);
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
			if (nui.Length == 0)
				return false;
			return IsChOrph(nui[0], out iPara, out offset);
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
				throw new ArgumentOutOfRangeException("word", "is either null or is punctuation!");
			if (Chart == null || Chart.RowsOS.Count == 0)
				return false; // No chart, therefore the whole text is ChOrphs! But that don't count.

			// Get last charted wordform
			var lastWordform = GetLastChartedWordform();
			if (lastWordform == null)
				return false; // This should mean the same as no charted text.

			if (word.IsAfter(lastWordform))
				return false; // the paragraph for which word is a part is not yet charted.

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
					return null;
			}
		}

		/// <summary>
		/// Find the index of the paragraph in the text of which the occurrence is a part.
		/// </summary>
		/// <param name="occurrence"></param>
		/// <returns></returns>
		protected int GetParagraphIndexForOccurrence(AnalysisOccurrence occurrence)
		{
			if (!occurrence.IsValid)
				throw new ArgumentException("Invalid Analysis!");
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
		protected internal void GetWordGroupCellsBorderingChOrph(int iPara, int offset,
			out ChartLocation precCell, out ChartLocation follCell)
		{
			Debug.Assert(iPara >= 0, String.Format("Bad ChOrph paragraph index = {0}!", iPara));
			Debug.Assert(offset >= 0, String.Format("Bad ChOrph paragraph offset = {0}!", offset));
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
					continue; // No wordgroups in this chart row, look in next one.

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
						follCell = new ChartLocation(rowFoll, icolFoll);
					if (rowPrec != null)
						precCell = new ChartLocation(rowPrec, icolPrec);
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
					ChartLocation temp = CheckFollowingRowPosition(iPara, offset, new ChartLocation(rowFoll, icolFoll));
					icolFoll = temp.ColIndex;
					rowFoll = temp.Row;
				}
				if (rowPrec.Hvo == rowFoll.Hvo) // rowFoll might have been changed in CheckFollowingRowPosition()
					icolFoll = NarrowSearchBackward(iPara, offset, new ChartLocation(rowPrec, icolPrec), icolFoll);
			}
			// By the time we get here, we should be able to return the right answer.
			// Set 'out' variables
			if (rowFoll != null)
				follCell = new ChartLocation(rowFoll, icolFoll);
			if (rowPrec != null)
				precCell = new ChartLocation(rowPrec, icolPrec);
		}

		/// <summary>
		/// Takes a cell location and tries to narrow the search forward for the last column in the
		/// row that is still logically before the ChOrph. Returns the new column index.
		/// </summary>
		/// <param name="iPara"></param>
		/// <param name="offset"></param>
		/// <param name="cell"></param>
		/// <returns></returns>
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
					continue;
				if (!WordGroupStartsBeforeChOrph(iPara, offset, wordGrpFound))
					break;
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
						result = new ChartLocation(rowNew, IndexOfColumnForCellPart(wordGrpFound.Hvo));
					return result;
				}
				// Updating our 'temp' variables.
				tempRow = rowNew;
				tempColIndex = IndexOfColumnForCellPart(wordGrpFound.Hvo);
				break;
			}
			var temp = new ChartLocation(tempRow, tempColIndex);
			if (!temp.IsSameLocation(cell))
				 result = temp;
			// We are now ready for NarrowSearchBackward().
			return result;
		}

		/// <summary>
		/// Moving backward, can we narrow our search to another cell?
		/// The last wordform in this row is "after" our ChOrph's logical position, but the first isn't
		/// and the right Following Cell is in this row.
		/// </summary>
		/// <param name="iPara"></param>
		/// <param name="offset"></param>
		/// <param name="precCell"></param>
		private int NarrowSearchBackward(int iPara, int offset, ChartLocation precCell, int icolFoll)
		{
			Debug.Assert(precCell != null && precCell.IsValidLocation);

			int result = icolFoll;
			for (int icolNew = icolFoll - 1; icolNew >= Math.Max(0, precCell.ColIndex); icolNew--)
			{
				var wordGrpFound = FindLastWordGroup(PartsInCell(new ChartLocation(precCell.Row, icolNew)));
				if (wordGrpFound == null)
					continue;
				if (!WordGroupStartsBeforeChOrph(iPara, offset, wordGrpFound))
				{
					result = icolNew; // Keep decrementing our result index until we get past our logical position
					continue;
				}
				if (IsChOrphWithinWordGroup(iPara, offset, wordGrpFound))
					result = icolNew;
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
				return true;
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
				return false;

			// Test first wordform in WordGroup, is ChOrph offset less?
			var firstWord = words[0];
			if (firstWord != null && offset < firstWord.GetMyBeginOffsetInPara())
				return false;

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
		/// <param name="precCell"></param>
		/// <param name="follCell"></param>
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
		/// <param name="icolPrec"></param>
		/// <param name="irowPrec"></param>
		/// <param name="icolFoll"></param>
		/// <param name="irowFoll"></param>
		/// <returns></returns>
		protected bool[] HighlightChOrphPossibles(int icolPrec, int irowPrec, int icolFoll, int irowFoll)
		{
			int ccols = AllMyColumns.Length;
			bool[] goodCols = new bool[ccols]; // return array of flags to disable the right MoveHere buttons

			int icurrCol = icolPrec;
			int icurrRow = irowPrec;

			// Set begin cell for highlighting
			int[] currHighlightCells = new int[4];
			currHighlightCells[0] = icurrRow;
			currHighlightCells[1] = icurrCol;

			while (true) // Won't loop more than ccols + 1 times.
			{
				goodCols[icurrCol] = true;
				icurrCol++;
				if (icurrRow == irowFoll && icurrCol > icolFoll)
					break;
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
			m_ribbon.EndSelLimitIndex = -1;
			m_ribbon.SelLimOccurrence = null;
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
					break; // No longer in any ChOrph group
				// Prepare more 'out' vars for GetWordGroupCellsBorderingChOrph
				// This time we don't care about the Preceding cell, only the Following cell; has it changed?
				ChartLocation dummy, newFollCell;
				GetWordGroupCellsBorderingChOrph(iPara, offset, out dummy, out newFollCell);
				if (!newFollCell.IsSameLocation(follCell))
					break; // No longer in same ChOrph group
			}
			i--;
			m_ribbon.EndSelLimitIndex = i; // These emit PropChanged themselves now.
			m_ribbon.SelLimOccurrence = nui[i];
			m_ribbon.SelectFirstOccurence();
		}

		#endregion

		/// <summary>
		/// Return a ChartLocation containing the column index and Row of a Wordform,
		/// unless it is uncharted, in which case it returns null.
		/// If it is a ChOrph (not charted because of Baseline modification),
		/// then it will return the nearest charted location. This should make the green
		/// highlighting visible (FWR-3681).
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public ChartLocation FindChartLocOfWordform(AnalysisOccurrence point)
		{
			if (m_chart == null || Chart.RowsOS.Count < 1)
				return null;
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
				arrayLoc = FindChartLocOfWordformInternal(point);

			return arrayLoc.IsValidLocation ? arrayLoc : null;
		}

		private ChartLocation FindChartLocOfWordformInternal(AnalysisOccurrence point)
		{
			// What we know:
			//    m_chart exists and has at least one Row

			var result = from row in m_chart.RowsOS
						 from wordGrp in m_wordGrpRepo.AllInstances()
						 where row.CellsOS.Contains(wordGrp) &&
							wordGrp.GetOccurrences().Contains(point)
						 select new ChartLocation(row, IndexOfColumnForCellPart(wordGrp));

			// Either return the valid result from LINQ or an invalid ChartLocation
			return result.Count() > 0 ? result.First() : new ChartLocation(null, -1);
		}

		/// <summary>
		/// Get the WfiWordform closest to the bookmark.
		/// </summary>
		/// <param name="bookmark"></param>
		/// <returns></returns>
		internal AnalysisOccurrence FindWordformAtBookmark(InterAreaBookmark bookmark)
		{
			Debug.Assert(m_hvoStText > 0, "No text!");
			Debug.Assert(bookmark != null && bookmark.IndexOfParagraph > -1, "Bad bookmark!");
			var txt = m_textRepo.GetObject(m_hvoStText);
			var curPara = (IStTxtPara)txt.ParagraphsOS[bookmark.IndexOfParagraph];
			Debug.Assert(curPara != null, "What kind of paragraph is this?");
			var ibeg = bookmark.BeginCharOffset;
			var iend = bookmark.EndCharOffset;
			var curSeg = FindSegmentContainingParaOffset(curPara, ibeg);
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
				return null;
			while (icurSeg < csegs && para.SegmentsOS[icurSeg].BeginOffset <= offset)
				icurSeg++;
			icurSeg--;

			return para.SegmentsOS[icurSeg];
		}

		/// <summary>
		/// Gets all the 'leaf' nodes in a chart template, and also the ends of column groupings.
		/// </summary>
		/// <param name="template"></param>
		/// <returns>List of int (hvos?)</returns>
		public List<ICmPossibility> AllColumns(ICmPossibility template)
		{
			var result = new List<ICmPossibility>();
			var groups = new Set<int>();
			if (template == null || template.SubPossibilitiesOS.Count == 0)
				return result; // template itself can't be a column even if no children.
			CollectColumns(result, template, groups, 0);
			m_indexGroupEnds = groups;
			return result;
		}

		/// <summary>
		/// Collect (in depth-first traversal) all the leaf columns in the template.
		/// Also (LT-8104) collect the set of column indices that are the ends of top-level column groupings.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="template"></param>
		/// <param name="groups"></param>
		/// <param name="depth"></param>
		private void CollectColumns(List<ICmPossibility> result, ICmPossibility template, Set<int> groups, int depth)
		{
			if (template.SubPossibilitiesOS.Count == 0)
			{
				// Note: do NOT do add to the list if it has children...we ONLY want leaves in the result.
				result.Add(template);
				// We now collect this column index in our GroupEndsIndices even if it's a group of one.
				if (depth == 1)
					groups.Add(result.Count - 1);
				return;
			}
			foreach (var child in template.SubPossibilitiesOS)
				CollectColumns(result, child, groups, depth + 1);

			// Collect this column index in our GroupEndsIndices if we're at the top-level.
			if (depth == 1)
				groups.Add(result.Count - 1);
		}

		#region actions for buttons

		internal void MoveToColumnInUOW(int icol)
		{
			IConstChartRow modifiedRow = null;
			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMoveWordToColumn, DiscourseStrings.ksRedoMoveWordToColumn,
				Cache.ActionHandlerAccessor, () => MoveToColumn(icol, out modifiedRow));
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
			var selectedWordforms = m_ribbon.SelectedOccurrences;
			if (selectedWordforms == null || selectedWordforms.Length == 0)
				return DiscourseStrings.ksNoWordformsMsg;
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
				else return DiscourseStrings.ksChooseDifferentColumn;
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
			// LT-7620 row numbers in ConstChart also says to ignore baseline paragraph breaks. [GordonM]
			//int[] nextWordforms = m_ribbon.SelectedOccurrences;
			//if (nextWordforms.Length > 0 &&
			//    GetPara(nextAnnotations[0]) != GetPara(selectedAnnotations[0]))
			//{
			//    SetFeature(m_cache.MainCacheAccessor, LastRow.Hvo, EndParaFeatureName, true);
			//}
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
					return;
				RowModifiedEvent(this, new RowModifiedEventArgs(modifiedRow));
			}
		}

		/// <summary>
		/// Extend the end point of a ConstChartWordGroup forward to a new contiguous point.
		/// Caller is responsible to see that this doesn't overlap another word group's 'territory'.
		/// </summary>
		/// <param name="wordGrpToExtend"></param>
		/// <param name="endPoint"></param>
		private static void ExtendWordGroupForwardTo(IConstChartWordGroup wordGrpToExtend, AnalysisOccurrence endPoint)
		{
			wordGrpToExtend.EndSegmentRA = endPoint.Segment;
			wordGrpToExtend.EndAnalysisIndex = endPoint.Index;
		}

		/// <summary>
		/// Returns the proper row to insert a ChOrph in the specified column (icol). If the desired column
		/// is not a valid choice given the current chart configuration, returns null.
		/// </summary>
		/// <param name="precCell"></param>
		/// <param name="follCell"></param>
		/// <param name="icol">Desired column index to insert ChOrph into (user column choice).</param>
		/// <returns></returns>
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
					return null; // Not an acceptable column choice as things stand presently.
				return precCell.Row;
			}
			if (icol < precCell.ColIndex)
			{
				var nextRow = NextRow(precCell.Row);
				if (nextRow == follCell.Row && icol > follCell.ColIndex)
					return null;
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
			var result = FindWhereToAddChOrph(finalCell,
				iPara, offset, out whereToInsert, out existingWordGroup);
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

		private static void ExpandWordGroupToInclude(IAnalysisReference existingWordGroup,
			IList<AnalysisOccurrence> selectedWordforms, bool fforward)
		{
			AnalysisOccurrence expandTo;
			if (fforward)
			{
				expandTo = selectedWordforms[selectedWordforms.Count - 1];
				while (existingWordGroup.EndRef() != expandTo)
					existingWordGroup.GrowFromEnd(true);
			}
			else
			{
				expandTo = selectedWordforms[0];
				while (existingWordGroup.BegRef() != expandTo)
					existingWordGroup.GrowFromBeginning(true);
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
			var result = DiscourseStrings.ksNoWordformsMsg;
			IConstChartRow modifiedRow = null;
			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMoveWordToColumn, DiscourseStrings.ksRedoMoveWordToColumn,
				Cache.ActionHandlerAccessor, () =>
			{
				MakeNewRow();
				result = MoveToColumn(ihvo, out modifiedRow);
			});
			FireRowModifiedEvent(modifiedRow);
			return result;
		}

		/// <summary>
		/// Indicates how to interpret the other results of FindWhereToAddChOrph and FindWhereToAddWords,
		/// namely, 'whereToInsert' and 'existingWordGroup'. A (different) set of 3 of these results apply to each of
		/// the 2 'FindWhereToAddX' methods. 'FWTAWords' uses the first 3. 'FWTAChOrph' uses all but kMakeNewRow.
		/// </summary>
		public enum FindWhereToAddResult
		{
			kAppendToExisting,		// append (word or ChOrph) as last occurrence(s) of 'existingWordGroup'
									// (ignore 'whereToInsert')

			kInsertWordGrpInRow,	// 'whereToInsert' specifies the index in the row's CellsOS of the new WordGroup to be
									// created (from Words or ChOrphs) in the (at this time, anyway) previously empty cell
									// (ignore 'existingWordGroup')

			kMakeNewRow,			// Make a new WordGroup in a new row. (ignore both 'whereToInsert and 'existingWordGroup')
									// (Not used for FindWhereToAddChOrph)

			kInsertChOrphInWordGrp	// Insert ChOrph word(s) into 'existingWordGroup'; whereToInsert is now index in WordGroup's
									// list of occurrences (Not used for FindWhereToAddWords)
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
		public FindWhereToAddResult FindWhereToAddWords(int icol, out int whereToInsert,
			out IConstChartWordGroup existingWordGrp)
		{
			existingWordGrp = null;
			whereToInsert = 0;
			var lastRow = LastRow;
			if (lastRow == null)
				return FindWhereToAddResult.kMakeNewRow;
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
		public FindWhereToAddResult FindWhereToAddChOrph(ChartLocation curCell, int iChOrphPara, int beginChOrphOffset,
			out int whereToInsert, out IConstChartWordGroup existingWordGroup)
		{
			// Enhance GordonM: This is an awfully long method. I need to break it down somehow.
			existingWordGroup = null;
			whereToInsert = 0;
			int ccellPartsInRow = curCell.Row.CellsOS.Count;
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
					return FindWhereToAddResult.kInsertChOrphInWordGrp; // i.e. Do nothing!

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
				throw new ArgumentOutOfRangeException("iChOrphPara");
			var para = m_chart.BasedOnRA.ParagraphsOS[iChOrphPara] as IStTxtPara;
			if (para == null || para.SegmentsOS.Count == 0)
				return null;
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
					continue;
				if (icurrCol > icolDesired)
					break; // iwhereToInsert == 'the right spot' in sequence

				// currCellPart is in the correct column
				if (currCellPart is IConstChartMovedTextMarker && ((IConstChartMovedTextMarker)currCellPart).Preposed)
					continue; // So far, we assume only preposed markers go before wordforms in a cell
				// This is a clause marker or tag and should come after any wordforms in this cell
				break;
			}
			return iwhereToInsert;
		}

		internal const int kMaxRibbonContext = 20;

		/// <summary>
		/// Insert another row below the argument row.
		/// Takes over any compDetails of the previous row.
		/// Line label is calculated based on the previous row's label.
		/// Caller deals with UOW.
		/// </summary>
		/// <param name="previousRow"></param>
		public void InsertRow(IConstChartRow previousRow)
		{
			var index = previousRow.IndexInOwner;

			var newRow = m_rowFact.Create();
			m_chart.RowsOS.Insert(index + 1, newRow);
			SetupCompDetailsForInsertRow(previousRow, newRow);
			// It's easiest to just renumber starting with the row above the inserted one.
			// foneSentOnly = true, because we are only adding a letter to the current sentence.
			RenumberRows(index, true);
		}

		/// <summary>
		/// Setups the boolean features for InsertRow().
		/// Inserting a new row will quite possibly remove special borders from the previous row.
		/// But it shouldn't affect dependent/song/speech clause features! [LT-9587]
		/// </summary>
		/// <param name="previousRow"></param>
		/// <param name="newRow"></param>
		private static void SetupCompDetailsForInsertRow(IConstChartRow previousRow, IConstChartRow newRow)
		{
			SafelyToggleBottomBorder(previousRow);
			var fEndSent = previousRow.EndSentence;
			if (!fEndSent)
				return; // don't set anything on new row

			// delete prevRow Sentence feature, add newRow Sentence feature
			previousRow.EndSentence = false;
			newRow.EndSentence = true;
			var fEndPara = previousRow.EndParagraph;
			if (!fEndPara)
				return;
			// delete prevRow para feature, add newRow para feature
			previousRow.EndParagraph = false;
			newRow.EndParagraph = true;
		}

		/// <summary>
		/// Clears the chart from the given cell to the end of the chart.
		/// Creates a UOW.
		/// </summary>
		/// <param name="cell"></param>
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
				return;

			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoClearChart, DiscourseStrings.ksRedoClearChart,
										Cache.ActionHandlerAccessor, () =>
			{
				// Because of possible side effects of deleting cells and rows, we'll do it
				// one by one and be careful that there is still something to delete
				// each time!

				// Delete the redundant rows. Contents of owned sequence are deleted automatically.
				if (crowsToDelete > 0)
				{
					//m_chart.RowsOS.Replace(ifirstRowToDelete, crowsToDelete, new ICmObject[0]);
					for (int i = crowsToDelete + ifirstRowToDelete - 1; i >= ifirstRowToDelete; i--)
					{
						m_chart.RowsOS.RemoveAt(i);
						if (i < 0 || m_chart.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
							break;
						var newCount = m_chart.RowsOS.Count;
						// defend against side effect deleting earlier object
						if (crows - 1 > newCount)
							i--;
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
					for (int i = ccellsToDelete + icellPart - 1; i >= icellPart; i--)
					{
						row.CellsOS.RemoveAt(i);
						if (i < 0 || row.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
							break;
						var newCount = row.CellsOS.Count;
						// defend against side effect deleting earlier object(s)
						if (ccells - 1 > newCount)
							i--;
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
				return;
			var clsMrkrTargets = new Set<IConstChartRow>();
			var myClsMrkrs = m_clauseMkrRepo.AllInstances().Where(mrkr => mrkr.Owner.Owner.Hvo == m_chart.Hvo);
			foreach (var clsMrkr in myClsMrkrs)
				clsMrkrTargets.AddRange(clsMrkr.DependentClausesRS);
			foreach (var row in myAbnormalRows.Where(row => !clsMrkrTargets.Contains(row)))
				ResetDepClauseProps(row);
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
				SafelyToggleBottomBorder(prevRow);
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
			newRow.Label = m_tssFact.MakeString(rowLabel, WsLineNumber);
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
			bool prevWasEOS = false; // was the last row marked End of Sentence?

			string prevRowLabel = LastRowNumber;
			if (LastRow != null)
				prevWasEOS = LastRow.EndSentence;
			else
			{
				rowLabel = "1";
				return rowLabel;
			}
			DecipherRowLabel(prevRowLabel, out rowNumber, out clauseNumber);
			if (clauseNumber == 1 && !prevWasEOS)
				// If the previous one had no clause number, we should add an 'a' to it.
				// But only if the previous one wasn't the End of a Sentence.
				AddLetterToNumberOnlyLabel(LastRow.IndexInOwner, rowNumber);
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
			m_chart.RowsOS[rowIndex].Label =
				m_tssFact.MakeString(Convert.ToString(rowNumber) + 'a', WsLineNumber);
		}

		/// <summary>
		/// Answer the writing system we want to use for line numbers.
		/// Currently this is locked to English, so as to avoid various weird behaviors
		/// when the first analysis or default user WS is changed (or is something that won't
		/// render "1a" sensibly.
		/// </summary>
		public int WsLineNumber
		{
			get
			{
				// Note: if you change this, also fix RowMenuItem.ToString().
				return Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");
			}
		}

		/// <summary>
		/// Pulls the row number and clause letter(a=1) from a string
		/// </summary>
		/// <param name="rowLabel1"></param>
		/// <param name="row"></param>
		/// <param name="clause"></param>
		private static void DecipherRowLabel(string rowLabel1, out int row, out int clause)
		{
			var rowLabel = rowLabel1 == null ? "" : rowLabel1;
			var posFirstLetter = 0;

			for (var i = 1; i < rowLabel.Length; i++) // i=1 because never start with a letter
			{
				if (rowLabel[i] < 'a' || rowLabel[i] > 'z')
					continue;
				posFirstLetter = i;
				break;
			}
			if (posFirstLetter == 0)
			{
				// Haven't yet found a letter! So this is only a number, no subclauses.
				try
				{
					row = Convert.ToInt32(rowLabel);
				}
				catch (Exception)
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
					clause = Convert.ToInt32(rowLabel[posFirstLetter + 1]) - Convert.ToInt32('a') + 27;
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
			bool foneSentFinished = false; // LT-8488 On foneSentOnly we need to do 2 sentences for odd case.
			while (irow < m_chart.RowsOS.Count)
			{
				// If this is the last row, assume EOS for numbering purposes,
				// otherwise look it up.
				if (m_chart.RowsOS.Count == irow + 1)
					fIsThisRowEOS = true;
				else
					fIsThisRowEOS = m_chart.RowsOS[irow].EndSentence;
				// Calculate and deposit label (if changed)
				var rowLabel = CalculateMyRowNums(ref csentence, ref cclause, fIsPrevRowEOS, fIsThisRowEOS);
				if (m_chart.RowsOS[irow].Label.Text != rowLabel)
				{
					m_chart.RowsOS[irow].Label = m_tssFact.MakeString(rowLabel, WsLineNumber);
				}
				if (fIsThisRowEOS && foneSentOnly && foneSentFinished)
					break;
				if (fIsThisRowEOS)
					foneSentFinished = true;
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
		private static string CalculateMyRowNums(ref int prevSentNum, ref int prevClauseNum,
			bool fSentBrkBefore, bool fSentBrkAfter)
		{
			if (fSentBrkBefore)
			{
				prevSentNum++;
				prevClauseNum = 1;
			}
			else prevClauseNum++;

			// Make the string
			string result;
			if (prevClauseNum > 26)
				result = Convert.ToString(prevSentNum) + "a" + Convert.ToChar(Convert.ToInt32('a')
																			  + prevClauseNum - 27);
			else
				result = Convert.ToString(prevSentNum) + Convert.ToChar(Convert.ToInt32('a')
																		+ prevClauseNum - 1);
			if (fSentBrkAfter && fSentBrkBefore)
				// Strip 'a' off of string.
				result = result.Substring(0, result.Length - 1);
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
		/// <param name="icolActual"></param>
		/// <param name="icolMovedFrom"></param>
		/// <returns>Null if successful, otherwise, an error message.</returns>
		public string MakeMovedText(int icolActual, int icolMovedFrom)
		{
			var selectedWordforms = m_ribbon.SelectedOccurrences;
			if (selectedWordforms == null || selectedWordforms.Length == 0)
				return DiscourseStrings.ksNoWordformsMsg;
			IConstChartRow rowModified = null;
			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMakeMoved, DiscourseStrings.ksRedoMakeMoved,
										Cache.ActionHandlerAccessor, () =>
			{
				MoveToColumn(icolActual, out rowModified);
				MakeMovedFrom(icolActual, icolMovedFrom);
			});
			FireRowModifiedEvent(rowModified);
			return null;
		}

		internal IConstChartRow LastRow
		{
			get
			{
				if (m_chart != null && m_chart.RowsOS.Count > 0)
					return m_chart.RowsOS[m_chart.RowsOS.Count - 1];
				return null;
			}
		}

		string LastRowNumber
		{
			get
			{
				if (m_chart != null && m_chart.RowsOS.Count > 0)
				{
					var result = LastRow.Label.Text;
					if (result != null)
						return result;
				}
				return "";
			}
		}

		/// <summary>
		/// Answer true if actualCell contains a WordGroup and icolMovedFrom designates a column that contains
		/// a moved-text marker pointing at it (same row).
		/// </summary>
		/// <param name="actualCell"></param>
		/// <param name="icolMovedFrom"></param>
		/// <returns></returns>
		protected bool IsMarkedAsMovedFrom(ChartLocation actualCell, int icolMovedFrom)
		{
			return PartsInCell(new ChartLocation(actualCell.Row, icolMovedFrom)).Any(
				mtm => IsMarkerOfMovedFrom(mtm, actualCell));
		}

		/// <summary>
		/// Answer true if cellPart is a ConstChartMovedTextMarker pointing to a wordgroup in the specified cell (same row).
		/// </summary>
		/// <param name="cellPart"></param>
		/// <param name="cellInSameRow"></param>
		/// <returns></returns>
		internal bool IsMarkerOfMovedFrom(IConstituentChartCellPart cellPart, ChartLocation cellInSameRow)
		{
			var mtm = cellPart as IConstChartMovedTextMarker;
			return mtm != null && PartsInCell(cellInSameRow).Contains(mtm.WordGroupRA);
		}

		/// <summary>
		/// Remove an existing moved text marker (that points at a WordGroup in actualCell).
		/// This version starts already knowing the marker.
		/// </summary>
		/// <param name="actualCell"></param>
		/// <param name="movedFromMarker"></param>
		protected void RemoveMovedFrom(ChartLocation actualCell, IConstChartMovedTextMarker movedFromMarker)
		{
			new MakeMovedTextMethod(this, actualCell, movedFromMarker).RemoveMovedFrom();
		}

		/// <summary>
		/// Remove an existing moved text marker from movedFromCell (that points at a WordGroup in actualCell)
		/// </summary>
		/// <param name="actualCell"></param>
		/// <param name="movedFromCell"></param>
		protected void RemoveMovedFrom(ChartLocation actualCell, ChartLocation movedFromCell)
		{
			new MakeMovedTextMethod(this, actualCell, movedFromCell).RemoveMovedFrom();
		}

		/// <summary>
		/// This is the most generic form. Only need these for setting up the MovedTextMarker.
		/// Once set up, it knows if it's Preposed or not.
		/// </summary>
		/// <returns></returns>
		internal bool IsPreposed(ChartLocation actualCell, ChartLocation movedFromCell)
		{
			if (actualCell.HvoRow == movedFromCell.HvoRow)
				return IsPreposed(actualCell.ColIndex, movedFromCell.ColIndex);
			return IsPreposed(actualCell.Row, movedFromCell.Row);
		}

		/// <summary>
		/// Use this one if you have two columns in the same row to determine
		/// if the marker is Preposed or Postposed.
		/// </summary>
		/// <param name="icolActual"></param>
		/// <param name="icolMovedFrom"></param>
		/// <returns></returns>
		internal static bool IsPreposed(int icolActual, int icolMovedFrom)
		{
			return icolActual < icolMovedFrom;
		}

		/// <summary>
		/// Use this one if you have a marker and its target on different rows to determine
		/// if the marker is Preposed or Postposed.
		/// </summary>
		/// <param name="rowActual"></param>
		/// <param name="rowMovedFrom"></param>
		/// <returns></returns>
		internal bool IsPreposed(IConstChartRow rowActual, IConstChartRow rowMovedFrom)
		{
			return rowActual.IndexInOwner < rowMovedFrom.IndexInOwner;
		}

		/// <summary>
		/// Return true if cellPart is a MovedText marker and happens to mark Preposed text.
		/// </summary>
		/// <param name="cellPart"></param>
		/// <returns></returns>
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
		/// <param name="icolActual"></param>
		/// <param name="icolMovedFrom"></param>
		private void MakeMovedFrom(int icolActual, int icolMovedFrom)
		{
			// Figure where to insert, and find the target.
			MakeMovedFrom(new ChartLocation(LastRow, icolActual), new ChartLocation(LastRow, icolMovedFrom));
		}

		/// <summary>
		/// Make a MovedText marker for moves (pre/postposed) involving two different rows.
		/// </summary>
		/// <param name="actual"></param>
		/// <param name="movedFrom"></param>
		protected void MakeMovedFrom(ChartLocation actual, ChartLocation movedFrom)
		{
			new MakeMovedTextMethod(this, actual, movedFrom).MakeMovedFrom();
		}

		/// <summary>
		/// Make a MovedText marker for moves (pre/postposed) involving two different rows and only part
		/// of the source cell's text. Used by the 'Advanced' Pre/Postposed dialog. Most generic form.
		/// </summary>
		/// <param name="actual"></param>
		/// <param name="movedFrom"></param>
		/// <param name="begPoint"></param>
		/// <param name="endPoint"></param>
		protected void MakeMovedFrom(ChartLocation actual, ChartLocation movedFrom, AnalysisOccurrence begPoint,
			AnalysisOccurrence endPoint)
		{
			// what if some part of the source cell is already marked as moved?
			// Enhance GordonM: Someday may need to handle case where list of wordforms is non-contiguous,
			// but not yet.
			new MakeMovedTextMethod(this, actual, movedFrom, begPoint, endPoint).MakeMovedFrom();
		}

		/// <summary>
		/// Make a marker indicating that something is missing in the specified column.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="wasMarked"></param>
		internal void ToggleMissingMarker(ChartLocation cell, bool wasMarked)
		{
			if (wasMarked)
			{
				var cellParts = PartsInCell(cell);
				foreach (var part in cellParts)
				{
					var tag = part as IConstChartTag;
					if (tag == null)
						continue;
					if (tag.TagRA == null)
						// This is the missing marker
					{
						UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksRedoMarkMissing, DiscourseStrings.ksUndoMarkMissing,
								Cache.ActionHandlerAccessor, () => cell.Row.CellsOS.Remove(tag));
						break;
					}
				}
			}
			else
			{
				var icellPartInsertAt = FindIndexOfFirstCellPartInOrAfterColumn(cell);
				// Enhance JohnT: may want to make this configurable.
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMarkMissing, DiscourseStrings.ksRedoMarkMissing,
					Cache.ActionHandlerAccessor, () => MakeMissingMarker(cell, icellPartInsertAt));
			}
		}

		/// <summary>
		/// Find the index in the row's Cells property of the first CellPart in (or after)
		/// the specified column.
		/// </summary>
		/// <param name="targetCell"></param>
		/// <returns></returns>
		int FindIndexOfFirstCellPartInOrAfterColumn(ChartLocation targetCell)
		{
			var newTarget = new ChartLocation(targetCell.Row, targetCell.ColIndex - 1);
			return FindIndexOfCellPartInLaterColumn(newTarget);
		}

		/// <summary>
		/// Find the index of the first CellPart that is in a column with index > targetCell's column index.
		/// </summary>
		/// <param name="targetCell"></param>
		/// <returns></returns>
		protected internal int FindIndexOfCellPartInLaterColumn(ChartLocation targetCell)
		{
			int icellPartInsertAt = targetCell.Row.CellsOS.Count; // insert at end unless we find something in a later column.
			int icellPart = 0;
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
		/// <param name="cell"></param>
		/// <returns></returns>
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
		/// <param name="cell"></param>
		/// <param name="index">index in row of first item in list</param>
		/// <returns></returns>
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
		public IConstChartClauseMarker MakeDependentClauseMarker(ChartLocation cell, IConstChartRow[] depClauses,
			ClauseTypes depType)
		{
			IConstChartClauseMarker newClauseMrkr = null;

			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMakeDepClause, DiscourseStrings.ksRedoMakeDepClause,
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
				newClauseMrkr = m_clauseMkrFact.Create(cell.Row, FindIndexOfCellPartInLaterColumn(cell),
					AllMyColumns[cell.ColIndex], depClauses);
			});
			return newClauseMrkr;
		}

		/// <summary>
		/// Find the (first) CellPart in the specified column, or null if none.
		/// </summary>
		/// <param name="cell"></param>
		/// <returns></returns>
		private IConstituentChartCellPart FindCellPartInColumn(ChartLocation cell)
		{
			return FindCellPartInColumn(cell, false);
		}

		/// <summary>
		/// Find a CellPart in the specified column, or null if none.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="fWantWordGroup">If true, require result to be a WordGroup.</param>
		/// <returns></returns>
		internal IConstituentChartCellPart FindCellPartInColumn(ChartLocation cell, bool fWantWordGroup)
		{
			return cell.Row.CellsOS.Where(cellPart => cellPart.ColumnRA == AllMyColumns[cell.ColIndex]).
				FirstOrDefault(cellPart => !fWantWordGroup || cellPart is IConstChartWordGroup);
		}

		///<summary>
		/// Answer true if this row of the chart is a dependent clause.
		/// </summary>
		/// <param name="row"></param>
		public bool IsDepClause(IConstChartRow row)
		{
			return row.ClauseType != ClauseTypes.Normal;
		}

		internal int IndexOfColumn(ICmPossibility col)
		{
			for (var i = 0; i < AllMyColumns.Length; i++)
				if (AllMyColumns[i] == col)
					return i;
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
		/// <param name="cell"></param>
		/// <param name="hvoListItem"></param>
		/// <param name="icellPartInsertAt"></param>
		/// <returns></returns>
		private IConstChartTag MakeChartTag(ChartLocation cell, int hvoListItem, int icellPartInsertAt)
		{
			// N.B. The below note used to be true, but is no longer because FDO barfs if an
			// object that is required to be owned exits FDO without being owned by something!
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
		/// <param name="cell"></param>
		/// <param name="icellPartInsertAt"></param>
		/// <returns></returns>
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
				MakeMoveItems(clickedCell, menu, new EventHandler(itemMoveForward_Click),
					new EventHandler(itemMoveBack_Click), DiscourseStrings.ksMoveMenuItem);
				MakeMoveItems(clickedCell, menu, new EventHandler(itemMoveWordForward_Click),
					new EventHandler(itemMoveWordBack_Click), DiscourseStrings.ksMoveWordMenuItem);
			}

			menu.Items.Add(new ToolStripSeparator());

			// Menu items allowing the user to toggle whether the line ends a paragraph.
			var itemREP = new OneValMenuItem(DiscourseStrings.ksRowEndsParaMenuItem, clickedCell.HvoRow);
			itemREP.CheckState = clickedCell.Row.EndParagraph ? CheckState.Checked : CheckState.Unchecked;
			itemREP.Click += new EventHandler(itemREP_Click);
			menu.Items.Add(itemREP);

			// Menu items allowing the user to toggle whether the line ends a sentence.
			var itemRES = new OneValMenuItem(DiscourseStrings.ksRowEndsSentMenuItem, clickedCell.Row.Hvo);
			itemRES.CheckState = clickedCell.Row.EndSentence ? CheckState.Checked : CheckState.Unchecked;
			itemRES.Click += new EventHandler(itemRES_Click);
			menu.Items.Add(itemRES);

			// Menu items allowing the cell to be visually merged with an empty cell to the left or right.
			var cellPart = FindCellPartInColumn(clickedCell);
			if (cellPart != null)
			{
				// non-empty, may have merge left/right capability
				if (clickedCell.ColIndex > 0 &&
					IsCellEmpty(new ChartLocation(clickedCell.Row, clickedCell.ColIndex - 1)))
				{
					var itemMergeBefore = new RowColMenuItem(DiscourseStrings.ksMergeBeforeMenuItem,
						clickedCell);
					itemMergeBefore.Click += new EventHandler(itemMergeBefore_Click);
					if (cellPart.MergesBefore)
						itemMergeBefore.Checked = true;
					menu.Items.Add(itemMergeBefore);
				}
				if (clickedCell.ColIndex < AllMyColumns.Length - 1
					&& IsCellEmpty(new ChartLocation(clickedCell.Row, clickedCell.ColIndex + 1)))
				{
					var itemMergeAfter = new RowColMenuItem(DiscourseStrings.ksMergeAfterMenuItem,
						clickedCell);
					var cellPartLast = FindCellPartInColumn(clickedCell, false);
					if (cellPartLast.MergesAfter)
						itemMergeAfter.Checked = true;
					menu.Items.Add(itemMergeAfter);
					itemMergeAfter.Click += new EventHandler(itemMergeAfter_Click);
				}
			}

			var itemNewRow = new RowColMenuItem(DiscourseStrings.ksInsertRowMenuItem, clickedCell);
			menu.Items.Add(itemNewRow);
			itemNewRow.Click += new EventHandler(itemNewRow_Click);

			var itemCFH = new RowColMenuItem(DiscourseStrings.ksClearFromHereOnMenuItem, clickedCell);
			menu.Items.Add(itemCFH);
			itemCFH.Click += new EventHandler(itemCFH_Click);

			menu.Items.Add(new ToolStripSeparator());

			// Menu items for inserting arbitrary markers from the ChartMarkers list.
			var chartMarkerList = Cache.LangProject.DiscourseDataOA.ChartMarkersOA;
			GeneratePlMenuItems(menu, chartMarkerList, new EventHandler(ToggleMarker_Item_Click), clickedCell);

			var mms = MissingState(clickedCell);
			if (mms != MissingMarkerState.kmmsDoesNotApply)
			{
				var itemMissingMarker = new RowColMenuItem(DiscourseStrings.ksMarkMissingItem, clickedCell);
				itemMissingMarker.Click += new EventHandler(itemMissingMarker_Click);
				itemMissingMarker.Checked = (mms == MissingMarkerState.kmmsChecked);
				menu.Items.Add(itemMissingMarker);
			}

			return menu;
		}

		enum MissingMarkerState
		{
			kmmsDoesNotApply, // cell contains something else
			kmmsChecked, // missing marker present
			kmmsUnchecked // cell completely empty
		}

		private MissingMarkerState MissingState(ChartLocation cell)
		{
			// As per LT-8545, Possibility markers don't affect MissingMarkerState (they aren't 'content')
			if (ColumnHasAutoMissingMarkers(cell.ColIndex))
				return MissingMarkerState.kmmsDoesNotApply;
			var cellPart = FindCellPartInColumn(cell);
			if (cellPart == null)
				return MissingMarkerState.kmmsUnchecked;
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
		/// <param name="part"></param>
		/// <returns></returns>
		private static bool IsMissingMarker(IConstituentChartCellPart part)
		{
			if (part == null || !(part is IConstChartTag))
				return false;
			return (part as IConstChartTag).TagRA == null;
		}

		/// <summary>
		/// If there's a missing marker in the specified cell get rid of it.
		/// </summary>
		/// <param name="cell"></param>
		internal void RemoveMissingMarker(ChartLocation cell)
		{
			var cellPart = FindCellPartInColumn(cell);
			if (!IsMissingMarker(cellPart))
				return;
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
				return;
			// Our row just disappeared on us due to deleting the last cell!
			// But we're obviously about to put something in here, so recreate the row.
			var newrow = m_rowFact.Create(Chart, irow, label);
			newrow.EndSentence = endSent;
			newrow.EndParagraph = endPara;
			if (clauseType == ClauseTypes.Normal)
				return;
			newrow.ClauseType = clauseType;
			newrow.StartDependentClauseGroup = startDepGrp;
			newrow.EndDependentClauseGroup = endDepGrp;
			foreach (var marker in markers)
				marker.DependentClausesRS.Add(newrow);
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
					continue;
				var itemR = new RowColMenuItem(DiscourseStrings.ksRemoveDependentMarkerMenuItem, srcCell);
				itemR.Click += new EventHandler(itemR_Click);
				menu.Items.Add(itemR);
				return;
			}
			var itemMDC = MakeDepClauseItem(srcCell, irow, DiscourseStrings.ksMakeDepClauseMenuItem,
				ClauseTypes.Dependent);
			menu.Items.Add(itemMDC);

			var itemMSC = MakeDepClauseItem(srcCell, irow, DiscourseStrings.ksMakeSpeechClauseMenuItem,
				ClauseTypes.Speech);
			menu.Items.Add(itemMSC);

			var itemMSoC = MakeDepClauseItem(srcCell, irow, DiscourseStrings.ksMakeSongClauseMenuItem,
				ClauseTypes.Song);
			menu.Items.Add(itemMSoC);
		}

		void itemR_Click(object sender, EventArgs e)
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
			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoRemoveClauseMarker, DiscourseStrings.ksRedoRemoveClauseMarker,
										Cache.ActionHandlerAccessor, () =>
			{
				foreach (var clauseMarker in PartsInCell(cell).OfType<IConstChartClauseMarker>())
				{
					foreach (var depRow in clauseMarker.DependentClausesRS)
						RemoveAllDepClauseMarkers(depRow);
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

		void itemCFH_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			var crowDel = m_chart.RowsOS.Count - item.SrcRow.IndexInOwner;
			if (MessageBox.Show(String.Format(DiscourseStrings.ksDelRowWarning, crowDel),
				DiscourseStrings.ksWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
			{
				ClearChartFromHereOn(item.SrcCell);
			}
		}

		void itemNewRow_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			InsertRowInUOW(item.SrcRow);
		}

		private void InsertRowInUOW(IConstChartRow prevRow)
		{
			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoInsertRow, DiscourseStrings.ksRedoInsertRow,
							Cache.ActionHandlerAccessor, () => InsertRow(prevRow));
		}

		private bool CellContainsWordforms(ChartLocation cell)
		{
			return FindFirstWordGroup(PartsInCell(cell)) != null;
		}

		private void MakeMoveItems(ChartLocation srcCell, ContextMenuStrip menu,
			EventHandler forward, EventHandler backward, string mainLabel)
		{
			// If there's nothing in the cell we can't move it; and a missing marker doesn't count.
			var part = FindCellPartInColumn(srcCell);
			if (part == null || IsMissingMarker(part))
				return;

			var itemMove = new ToolStripMenuItem(mainLabel);
			if (TryGetNextCell(srcCell))
			{
				var itemMoveForward = new RowColMenuItem(DiscourseStrings.ksForwardMenuItem, srcCell);
				itemMoveForward.Click += forward;
				itemMove.DropDownItems.Add(itemMoveForward);
			}
			if (TryGetPreviousCell(srcCell))
			{
				var itemMoveBack = new RowColMenuItem(DiscourseStrings.ksBackMenuItem, srcCell);
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
				return; // can't do this without some real data in the cell.

			ChartLocation markerCell; // Might eventually contain the location of the 'movedText' marker (MM=MovedMarker)
			var fMMDifferentRow = false; // true if we find a marker in a different row pointing to this cell (either direction)
			var fMMForward = false; // true if Preposed text has a marker in a different row
			var fMMBackward = false; // true if Postposed text has a marker in a different row
			foreach (var wordGrp in wordGroups)
			{
				// Check to see if wordGrp is 'movedText' and if so, find its target
				IConstChartMovedTextMarker movedMrkr;
				if (!IsMovedText(wordGrp, out movedMrkr))
					continue;
				markerCell = new ChartLocation((IConstChartRow)movedMrkr.Owner, IndexOfColumn(movedMrkr.ColumnRA));
				if (srcCell.HvoRow == markerCell.HvoRow)
					continue;
				fMMDifferentRow = true;
				if (movedMrkr.Preposed)
					fMMForward = true;
				else
					fMMBackward = true;
			}
			int icol = srcCell.ColIndex;
			MakePreposedOrPostPosedMenuItem(menu, srcCell, 0, icol,
				DiscourseStrings.ksPostposeFromMenuItem,
				fMMDifferentRow && fMMBackward); // True if a MT Marker is already out there.
			MakePreposedOrPostPosedMenuItem(menu, srcCell, icol + 1, AllMyColumns.Length,
				DiscourseStrings.ksPreposeFromMenuItem,
				fMMDifferentRow && fMMForward);
		}

		/// <summary>
		/// Makes Pre/Postposed Menu items based on columns to the appropriate side of this cell. Also may
		/// make a menu item for Advanced... depending on boolean flags.
		/// (If the flag is true, the Advanced... menu item will be checked.)
		/// </summary>
		/// <param name="menu"></param>
		/// <param name="srcCell"></param>
		/// <param name="icolStart"></param>
		/// <param name="icolLim"></param>
		/// <param name="text"></param>
		/// <param name="fMarkerPresent">'true' if there is already a 'movedText' marker out there in another row in
		/// the appropriate direction.</param>
		private void MakePreposedOrPostPosedMenuItem(ContextMenuStrip menu, ChartLocation srcCell,
			int icolStart, int icolLim, string text, bool fMarkerPresent)
		{
			// If no subitems, don't make the parent.
			// First check if another clause is possible?
			bool fAnotherClausePossible = IsAnotherClausePossible(srcCell.Row, text == DiscourseStrings.ksPreposeFromMenuItem);
			if ((icolStart >= icolLim) && !fMarkerPresent && !fAnotherClausePossible)
				return;
			var itemMTSubmenu = new ToolStripMenuItem(text);
			menu.Items.Add(itemMTSubmenu);
			for (int i = icolStart; i < icolLim; i++)
			{
				var itemCol = new TwoColumnMenuItem(GetColumnLabel(i), srcCell.ColIndex, i, srcCell.Row);
				itemCol.Click += new EventHandler(itemCol_Click);
				itemCol.Checked = IsMarkedAsMovedFrom(srcCell, i);
				itemMTSubmenu.DropDownItems.Add(itemCol);
			}
			// We always need the "Advanced..." option if IsAnotherClausePossible is true, or there are available columns.
			RowColMenuItem itemAdvanced = new RowColMenuItem(DiscourseStrings.ksAdvancedDlgMenuItem, srcCell);
			itemMTSubmenu.DropDownItems.Add(itemAdvanced);
			if (text == DiscourseStrings.ksPreposeFromMenuItem)
				itemAdvanced.Click += new EventHandler(itemAnotherPre_Click);
			else
				itemAdvanced.Click += new EventHandler(itemAnotherPost_Click);
			if (fMarkerPresent)
				itemAdvanced.Checked = true;
		}

		private bool IsAnotherClausePossible(IConstChartRow rowSrc, bool fPrepose)
		{
			var eligibleRows = CollectRowsInSentence(rowSrc, fPrepose);
			return (eligibleRows.Count > 1); // If there's more than the source row, then it's possible!
		}

		const int rowLimiter = 5;

		/// <summary>
		/// Collects a list of rows in the same Sentence as the supplied ChartRow
		/// going in the specified direction. "Same Sentence" is defined by the rows' EndOfSentence
		/// Feature and limited (at this point) to 5 rows forward or backward.
		/// Current row is now included.
		/// </summary>
		/// <param name="curRow"></param>
		/// <param name="fForward">true is forward, false is backward.</param>
		/// <returns></returns>
		private List<IConstChartRow> CollectRowsInSentence(IConstChartRow curRow, bool fForward)
		{
			// Better check first to see if we're going forward and curRow has EOS feature
			var result = new List<IConstChartRow> {curRow}; // include current row
			if (fForward && curRow.EndSentence)
				return result;
			var testIndex = curRow.IndexInOwner;
			var indexMax = Math.Min(m_chart.RowsOS.Count - 1, testIndex + rowLimiter);
			var indexMin = Math.Max(0, testIndex - rowLimiter);
			do
			{
				// if fForward, are there rows after rowSrc?
				// if not, are there rows before rowSrc?
				if (fForward)
					testIndex++;
				else
					testIndex--;
				if (testIndex < indexMin || testIndex > indexMax)
					break; // We went too far!
				var tempRow = m_chart.RowsOS[testIndex];
				if (tempRow.EndSentence)
				{
					if (fForward)
						result.Add(tempRow); // going forward, include the EOS row
					break;
				}
				result.Add(tempRow);
			} while (true);
			if (!fForward)
				result.Reverse();
			return result;
		}

		void itemAnotherPre_Click(object sender, EventArgs e)
		{
			itemAdvanced_Click(sender, e, true);
		}

		void itemAnotherPost_Click(object sender, EventArgs e)
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
				return;

			var item = sender as RowColMenuItem;
			var srcCell = item.SrcCell;

			if (item.Checked)
			{
				// Go find the marker that points to this cell and remove it.
				var marker = FindMovedMarkerOtherRow(srcCell, fPrepose);
				var markerCell = new ChartLocation((IConstChartRow)marker.Owner, IndexOfColumnForCellPart(marker));
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksRedoMakeMoved, DiscourseStrings.ksUndoMakeMoved,
											Cache.ActionHandlerAccessor, () => RemoveMovedFrom(srcCell, marker));
				return;
			}

			// Collect rows and columns to display in dialog
			// First rows
			var eligibleRows = CollectEligibleRows(srcCell, fPrepose).ToArray();
			if (eligibleRows.Length == 0) return; // Shouldn't happen!

			var paramObject = new CChartSentenceElements(srcCell, eligibleRows, AllMyColumns);

			// Enhance GordonM: We need to do something different if there are multiple WordGroups in the cell?!
			// I did 'something different' alright, but I'm not done. [Or am I?]
			// Maybe I need to make a temporary WordGroup that holds all the wordforms in the cell for dialog ribbon display purposes.

			// Load all WordGroups in this cell into the parameter object's AffectedWordGroups.
			paramObject.AffectedWordGroups = CollectAllWordGroups(PartsInCell(srcCell));
			var dlg = new AdvancedMTDialog(Cache, fPrepose, paramObject, m_helpTopicProvider);

			// Display dialog
			if (dlg.ShowDialog() != DialogResult.OK)
				return;
			var fromRow = dlg.SelectedRow.Row;
			var iColMovedFrom = IndexOfColumn(dlg.SelectedColumn.Column);

			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMakeMoved, DiscourseStrings.ksRedoMakeMoved,
				Cache.ActionHandlerAccessor, () =>
			{
				// LT-7668 If user chooses to make a movedText marker and one exists already for this cell
				// we need to remove the first one before adding this one.
				// Now we check all the WordGroups affected by our new MovedText item and remove any existing marker.
				foreach (var wordGrp in paramObject.AffectedWordGroups)
				{
					IConstChartMovedTextMarker marker;
					if (!IsMovedText(wordGrp, out marker))
						continue;
					RemoveMovedFrom(srcCell, FindChartLocOfCellPart(marker));
				}
				var movedFrom = new ChartLocation(fromRow, iColMovedFrom);
				var cwords = dlg.SelectedOccurrences.Length;
				MakeMovedFrom(srcCell, movedFrom, dlg.SelectedOccurrences[0], dlg.SelectedOccurrences[cwords - 1]);
			});
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
		/// (like AnalysisAdjuster). It may eventually be unnecessary as FDO causes objects
		/// to clean up after themselves more and more. Protected for testing.
		/// </summary>
		protected internal void CleanupInvalidChartCells()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var fReported = false;
				// Clobber any deleted words, etc.
				var crows = m_chart.RowsOS.Count;
				for (var irow = 0; irow < crows; irow++) // not foreach here, as we may delete some as we go
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
							m_chart.RowsOS[0].Delete();
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
							continue;
						if (curPart is IConstChartClauseMarker)
						{
							if (!((IConstChartClauseMarker)curPart).HasValidRefs)
								ReportWarningAndUpdateCountsRemovingCellPart(curRow, curPart, ref fReported, ref ipart, ref citems);
							continue;
						}
						if (curPart is IConstChartMovedTextMarker)
						{
							if (!((IConstChartMovedTextMarker)curPart).HasValidRef)
								ReportWarningAndUpdateCountsRemovingCellPart(curRow, curPart, ref fReported, ref ipart, ref citems);
							continue;
						}
						// Do some further checking because it's a ConstChartWordGroup.
						var curWordGroup = curPart as IConstChartWordGroup;
						if (!curWordGroup.IsValidRef)
						{
							// This is an invalid cell part. We need to delete this cell part.
							ReportWarningAndUpdateCountsRemovingCellPart(curRow, curPart, ref fReported, ref ipart, ref citems);
							continue; // Skip to next.
						}
						var occurrences = curWordGroup.GetOccurrences(); // Checks references for Wordforms
						if (occurrences.Count > 0)
							continue;
						// CCWordGroup is now empty, take it out of row!
						ReportWarningAndUpdateCountsRemovingCellPart(curRow, curWordGroup, ref fReported, ref ipart, ref citems);
					} // cellPart loop
					if (curRow.Hvo > 0)
						continue;
					// row is now empty and has disappeared automatically, update counts
					irow--;
					crows--;
				} // row loop
			});
		}

		private static void ReportWarningAndUpdateCountsRemovingCellPart(IConstChartRow row,
			IConstituentChartCellPart part, ref bool fReported, ref int index, ref int count)
		{
			//Debug.Assert(false, "About to delete cell part. Why!?");
			row.CellsOS.Remove(part);
			if (!fReported)
			{
				MessageBox.Show(DiscourseStrings.ksTextEditWarning, DiscourseStrings.ksWarning,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				fReported = true;
			}
			// Restart the loop; we may have deleted additional items either before or after the one
			// we expect to.
			index = -1; // after auto-increment in for loop, starts over at beginning.
			// if we just removed the last cell in the row, the row will be deleted too!
			count = row.IsValidObject ? row.CellsOS.Count : 0;
		}

		/// <summary>
		/// Returns a list of rows that should be eligible to choose from the Advanced... dialog row combo box.
		/// </summary>
		/// <param name="clickedCell"></param>
		/// <param name="fPrepose"></param>
		/// <returns></returns>
		protected List<IConstChartRow> CollectEligibleRows(ChartLocation clickedCell, bool fPrepose)
		{
			// Collect all rows in the 'right' direction including the clicked one.
			var result = CollectRowsInSentence(clickedCell.Row, fPrepose);
			if (result.Count < 0)
				return result;
			// If we are marking Postposed from the first column, we don't want the last row to be eligible.
			// If we are marking Preposed from the last column, we don't want the first row to be eligible.
			if (fPrepose)
			{
				//int icolLast = IndexOfColumn(AllMyColumns[AllMyColumns.Length - 1].Hvo);
				var icolLast = AllMyColumns.Length - 1;
				if (clickedCell.ColIndex == icolLast)
					result.RemoveAt(0);
			}
			else
			{
				if (clickedCell.ColIndex == 0)
					result.RemoveAt(result.Count - 1);
			}
			return result;
		}

		/// <summary>
		/// Find and return the movedText marker that points to my cell from another row.
		/// Caller knows there should be one because of the 'movedText' feature on the text and no markers
		/// in the current row. Not limited to current sentence.
		/// </summary>
		/// <param name="srcCell"></param>
		/// <param name="fPrepose"></param>
		/// <returns>the movedText marker's ChartLocation</returns>
		private IConstChartMovedTextMarker FindMovedMarkerOtherRow(ChartLocation srcCell, bool fPrepose)
		{
			// The following LINQ gets only MovedTextMarkers from this chart
			//     that are pointing to WordGroups in srcCell's row
			//     and that match the fPrepose parameter (are pointing the right direction)
			var movedMkrList = m_movedTextRepo.AllInstances().Where(
				marker => marker.Owner.Owner.Hvo == Chart.Hvo &&
						  marker.Preposed == fPrepose &&
						  marker.WordGroupRA.Owner == srcCell.Row).ToList();
			var column = AllMyColumns[srcCell.ColIndex];
			return movedMkrList.FirstOrDefault(
				movedTextMarker => movedTextMarker.WordGroupRA.ColumnRA == column);
		}

		// Mark Moved Text from one cell to another(within a row).
		void itemCol_Click(object sender, EventArgs e)
		{
			var item = sender as TwoColumnMenuItem;
			var srcCell = new ChartLocation(item.Row, item.Source);
			var dstCell = new ChartLocation(item.Row, item.Destination);
			if (item.Checked)
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksRedoMakeMoved, DiscourseStrings.ksUndoMakeMoved,
											Cache.ActionHandlerAccessor, () => RemoveMovedFrom(dstCell, srcCell));
			else
			{
				var wordGrp = FindFirstWordGroup(PartsInCell(dstCell));

				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMakeMoved, DiscourseStrings.ksRedoMakeMoved,
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

		void itemMoveForward_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			MoveCellForward(item.SrcCell);
		}
		void itemMoveBack_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			MoveCellBack(item.SrcCell);
		}
		void itemMoveWordForward_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			MoveWordForward(item.SrcCell);
		}
		void itemMoveWordBack_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			MoveWordBack(item.SrcCell);
		}

		void itemMergeAfter_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			ToggleMergedCellFlag(item.SrcCell, true);
		}

		void itemMergeBefore_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			ToggleMergedCellFlag(item.SrcCell, false);

		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem gets added to menu.Items collection and disposed there.")]
		private void GeneratePlMenuItems(ContextMenuStrip menu, ICmPossibilityList list,
			EventHandler clickHandler, ChartLocation cell)
		{
			var markerRefs = SelectListItemReferences(PartsInCell(cell));
			foreach (var poss in list.PossibilitiesOS)
			{
				menu.Items.Add(MakePlItem(clickHandler, cell, poss, markerRefs, DiscourseStrings.ksMarkMenuItemFormat));
			}
		}

		/// <summary>
		/// Make one item in a possibility-list menu. Includes making subitems if poss has subpossibilties.
		/// The leaves are the only really active choices. The item is checked if the specified cell
		/// already contains a pointer to this possibility.
		/// </summary>
		/// <param name="clickHandler"></param>
		/// <param name="cell"></param>
		/// <param name="poss"></param>
		/// <param name="markerRefs"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		private ToolStripMenuItem MakePlItem(EventHandler clickHandler, ChartLocation cell,
			ICmPossibility poss, IEnumerable<IConstChartTag> markerRefs, string format)
		{
			var item = new RowColPossibilityMenuItem(cell, poss.Hvo);
			var label = poss.Name.BestAnalysisAlternative.Text;
			if (label == null)
				label = "";
			if (poss.SubPossibilitiesOS.Count == 0)
			{
				string abbr = poss.Abbreviation.AnalysisDefaultWritingSystem.Text;
				if (abbr != label && !String.IsNullOrEmpty(abbr))
					label = label + " (" + abbr + ")";
				item.Click += clickHandler; // can only select leaves.
				if (ListRefersToMarker(markerRefs, poss.Hvo))
					item.Checked = true;
			}
			if (format != null)
				label = String.Format(format, label);
			item.Text = label;
			foreach (var poss2 in poss.SubPossibilitiesOS)
				item.DropDownItems.Add(MakePlItem(clickHandler, cell, poss2, markerRefs, null));
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

		void ToggleMarker_Item_Click(object sender, EventArgs e)
		{
			var item = sender as RowColPossibilityMenuItem;

			AddOrRemoveMarker(item);
		}

		public void AddOrRemoveMarker(RowColPossibilityMenuItem item)
		{
			if (item.Checked)
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoRemoveMarker, DiscourseStrings.ksRedoRemoveMarker,
					Cache.ActionHandlerAccessor, () => RemoveListItemPart(item.SrcCell, item.m_hvoPoss));
			else
			{
				var icellPartInsertAt = FindIndexOfCellPartInLaterColumn(item.SrcCell);
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoAddMarker, DiscourseStrings.ksRedoAddMarker,
					Cache.ActionHandlerAccessor, () => MakeChartTag(item.SrcCell, item.m_hvoPoss, icellPartInsertAt));
			}
		}

		private void RemoveListItemPart(ChartLocation srcCell, int hvoMarker)
		{
			foreach (var part in
				PartsInCell(srcCell).Where(part => part is IConstChartTag &&
					(part as IConstChartTag).TagRA.Hvo == hvoMarker))
			{
				srcCell.Row.CellsOS.Remove(part);
				return;
			}
		}

		const int kdepClauseRowLimit = 5; // A limit to possible dependent clause menu rows.

		private ToolStripMenuItem MakeDepClauseItem(ChartLocation srcCell, int irowSrc,
			string mainLabel, ClauseTypes depType)
		{
			var itemMDC = new ToolStripMenuItem(mainLabel);
			if (irowSrc > 0)
			{
				// put in just one 'previous clause' item.
				var item = new DepClauseMenuItem(DiscourseStrings.ksPreviousClauseMenuItem,
					srcCell, new [] { m_chart.RowsOS[irowSrc - 1] });
				item.Click += new EventHandler(itemDC_Click);
				item.DepType = depType;
				itemMDC.DropDownItems.Add(item);
			}
			var depClauseRows = new List<IConstChartRow>(kdepClauseRowLimit);
			for (var irow = irowSrc + 1; irow < Math.Min(irowSrc + kdepClauseRowLimit, m_chart.RowsOS.Count); irow++)
			{
				string label;
				switch (irow - irowSrc)
				{
					case 1: label = DiscourseStrings.ksNextClauseMenuItem; break;
					case 2: label = DiscourseStrings.ksNextTwoClausesMenuItem; break;
					default: label = String.Format(DiscourseStrings.ksNextNClausesMenuItem, (irow - irowSrc)); break;
				}
				depClauseRows.Add(m_chart.RowsOS[irow]);
				var item = new DepClauseMenuItem(label, srcCell, depClauseRows.ToArray());
				item.Click += new EventHandler(itemDC_Click);
				itemMDC.DropDownItems.Add(item);
				item.DepType = depType;
			}
			var itemOther = new DepClauseMenuItem(DiscourseStrings.ksOtherMenuItem, srcCell, null);
			itemMDC.DropDownItems.Add(itemOther);
			itemOther.Click += new EventHandler(itemOther_Click);
			itemOther.DepType = depType;

			return itemMDC;
		}

		// Generates a dialog with each row except the one clicked (within reason)
		void itemOther_Click(object sender, EventArgs e)
		{
			if (m_chart.RowsOS.Count == 1)
				return;
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
					iSelect = items.Count - 1;
				dlg.SelectedRow = items[iSelect];
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					var outer = dlg.SelectedRow.Row;
					int index = outer.IndexInOwner;
					int start = iSrc + 1;
					int end = index;
					if (index < iSrc)
					{
						start = index;
						end = iSrc - 1;
					}
					var rows = new List<IConstChartRow>();
					for (var i = start; i <= end; i++)
						rows.Add(m_chart.RowsOS[i]);
					MakeDependentClauseMarker(item.SrcCell, rows.ToArray(), item.DepType);
				}
			}
		}

		static public string EndParaFeatureName
		{
			get { return "endPara"; }
		}

		static public string EndSentFeatureName
		{
			get { return "endSent"; }
		}

		/// <summary>
		/// Invoked when the user chooses the "Row Ends Paragraph" menu item.
		/// Sender is a OneValMenuItem indicating the row.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void itemREP_Click(object sender, EventArgs e)
		{
			var item = sender as OneValMenuItem;
			bool fSentWasOn;
			var hvoRow = item.Source;
			var curRow = m_rowRepo.GetObject(hvoRow);
			var irow = curRow.IndexInOwner;

			if (curRow.EndParagraph)
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksRedoLastRowInPara, DiscourseStrings.ksUndoLastRowInPara,
					Cache.ActionHandlerAccessor, () => curRow.EndParagraph = false);
			else
			{
				// Save EOS state for determining if we need to renumber rows
				fSentWasOn = curRow.EndSentence;

				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoLastRowInPara, DiscourseStrings.ksRedoLastRowInPara,
											Cache.ActionHandlerAccessor, () =>
				{
					// Set both EOP and EOS
					curRow.EndParagraph = true;
					curRow.EndSentence = true;

					// Turning on EOP only affects numbering if EOS was off before
					if (!fSentWasOn)
						RenumberRows(irow, false);
				});
			}
		}

		/// <summary>
		/// Invoked when the user chooses the "Row Ends Sentence" menu item.
		/// Sender is a OneValMenuItem indicating the row.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void itemRES_Click(object sender, EventArgs e)
		{
			OneValMenuItem item = sender as OneValMenuItem;
			int hvoRow = item.Source;
			var curRow = m_rowRepo.GetObject(hvoRow);
			var irow = curRow.IndexInOwner;

			if (curRow.EndSentence)
			{
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksRedoLastRowInSent, DiscourseStrings.ksUndoLastRowInSent,
											Cache.ActionHandlerAccessor, () =>
				{
					// unchecking EOS, unchecks EOP too
					curRow.EndSentence = false;
					curRow.EndParagraph = false;
					// Now we need to renumber our row labels, unless we're on the last row.
					if (irow != m_chart.RowsOS.Count - 1)
						RenumberRows(irow, false);
				});
			}
			else
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoLastRowInSent, DiscourseStrings.ksRedoLastRowInSent,
											Cache.ActionHandlerAccessor, () =>
				{
					curRow.EndSentence = true;
					// Now we need to renumber our row labels, unless we're on the last row.
					if (irow != m_chart.RowsOS.Count - 1)
						RenumberRows(irow, false);
				});
		}

		/// <summary>
		/// Invoked when the user clicks an item in add dependent/speech/song clause.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void itemDC_Click(object sender, EventArgs e)
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
		/// <param name="srcCell"></param>
		/// <param name="following"></param>
		/// <returns></returns>
		public string ToggleMergedCellFlag(ChartLocation srcCell, bool following)
		{
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(DiscourseStrings.ksUndoMergeCells, DiscourseStrings.ksRedoMergeCells,
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
		/// <param name="icol">The icol.</param>
		/// <returns></returns>
		public ContextMenuStrip MakeContextMenu(int icol)
		{
			var menu = new ContextMenuStrip();

			var itemNewClause = new OneValMenuItem(DiscourseStrings.ksMoveHereInNewClause, icol);
			itemNewClause.Click += new EventHandler(itemNewClause_Click);
			menu.Items.Add(itemNewClause);

			var itemMT = new ToolStripMenuItem(DiscourseStrings.ksMovedFromMenuItem);
			for (int ihvo = 0; ihvo < AllMyColumns.Length; ihvo++)
			{
				if (ihvo == icol)
					continue;
				var item = new TwoColumnMenuItem(GetColumnLabel(ihvo), icol, ihvo);
				item.Click += new EventHandler(InsertMovedText_Click);
				itemMT.DropDownItems.Add(item);
			}
			menu.Items.Add(itemMT);
			return menu;
		}

		void itemMissingMarker_Click(object sender, EventArgs e)
		{
			var item = sender as RowColMenuItem;
			ToggleMissingMarker(item.SrcCell, item.Checked);
		}

		void itemNewClause_Click(object sender, EventArgs e)
		{
			var item = sender as OneValMenuItem;
			MoveToHereInNewClause(item.Source);
		}

		void InsertMovedText_Click(object sender, EventArgs e)
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
		/// <param name="partsToMove"></param>
		/// <param name="newCol"></param>
		/// <param name="row"></param>
		public virtual void ChangeColumn(IConstituentChartCellPart[] partsToMove, ICmPossibility newCol,
			IConstChartRow row)
		{
			Debug.Assert(row.CellsOS.Contains(partsToMove[0]));
			ChangeColumnInternal(partsToMove, newCol);
		}

		/// <summary>
		/// Implementation of ChangeColumn, but not responsible for PropChanged messages.
		/// Can also be used as part of a move from end of one row to oppposite end of another.
		/// protected virtual only for override in testing.
		/// </summary>
		/// <param name="partsToMove"></param>
		/// <param name="newCol"></param>
		protected internal virtual void ChangeColumnInternal(IConstituentChartCellPart[] partsToMove, ICmPossibility newCol)
		{
			foreach (var part in partsToMove)
				part.ColumnRA = newCol;
		}

		/// <summary>
		/// One of the basic move operations. Moves CellPart objects from one row to another. Caller is responsible
		/// to ensure this is legitimate (i.e., will not make text out of order).
		/// </summary>
		/// <param name="partsToMove"></param>
		/// <param name="rowSrc"></param>
		/// <param name="rowDst"></param>
		/// <param name="srcIndex">AppliesTo index</param>
		/// <param name="dstIndex">AppliesTo index</param>
		public virtual void ChangeRow(IConstituentChartCellPart[] partsToMove, IConstChartRow rowSrc, IConstChartRow rowDst,
			int srcIndex, int dstIndex)
		{
			if (rowSrc == null || rowSrc.CellsOS.Count <= srcIndex)
				throw new ArgumentOutOfRangeException("srcIndex");
			if (rowDst == null || rowDst.CellsOS.Count <= dstIndex)
				throw new ArgumentOutOfRangeException("dstIndex");
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
		/// <param name="srcWordGrp"></param>
		/// <param name="dstWordGrp"></param>
		public virtual void MoveAnalysesBetweenWordGroups(IConstChartWordGroup srcWordGrp,
			IConstChartWordGroup dstWordGrp)
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
		/// <param name="row"></param>
		/// <param name="ihvo">in AppliesTo</param>
		/// <param name="chvo"></param>
		public virtual void DeleteCellParts(IConstChartRow row, int ihvo, int chvo)
		{
			for (var i = 0; i < chvo; i++)
				row.CellsOS.RemoveAt(ihvo);
		}

		/// <summary>
		/// Merge the contents of the source cell into the destination cell (at the start if forward is true,
		/// otherwise at the end).
		/// Caller deals with UOW.
		/// </summary>
		/// <param name="srcCell">Source cell</param>
		/// <param name="dstCell">Destination cell</param>
		/// <param name="forward">true if merging in forward direction (assuming LtoR, this is right).</param>
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
		/// <param name="cellParts"></param>
		/// <returns></returns>
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
		/// <param name="cellParts"></param>
		/// <returns></returns>
		protected internal static IConstChartWordGroup FindFirstWordGroup(List<IConstituentChartCellPart> cellParts)
		{
			return cellParts.OfType<IConstChartWordGroup>().FirstOrDefault();
		}

		/// <summary>
		/// Given a list of cellparts (typically in a single cell), collect a list of all the ones
		/// that are actually CCWordGroups.
		/// </summary>
		/// <param name="partsInCell"></param>
		/// <returns></returns>
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
		protected internal IConstChartWordGroup FindNextWordGroup(List<IConstituentChartCellPart> partsInCell, int iStartPart,
			out int indexFoundAt)
		{
			indexFoundAt = -1;
			for (var i = iStartPart + 1; i < partsInCell.Count; i++)
			{
				if (!(partsInCell[i] is IConstChartWordGroup))
					continue;
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
		/// <param name="partsInCell"></param>
		/// <param name="indexFoundAt"></param>
		/// <returns></returns>
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
		/// <param name="srcCell"></param>
		public void MoveCellBack(ChartLocation srcCell)
		{
			ChartLocation prevCell;
			if (srcCell.ColIndex == 0)
			{
				// Move to start of previous row.
				var prevRow = PreviousRow(srcCell.Row);
				// Enhance: If this is the first row, should we insert a row and then move the cell?
				if (prevRow == null)
					return; // can't go back further.
				prevCell = new ChartLocation(prevRow, AllMyColumns.Length - 1);
			}
			else
			{
				// Normal case, merging/moving to cell on same row.
				prevCell = new ChartLocation(srcCell.Row, srcCell.ColIndex - 1);
			}
			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMoveCellBack, DiscourseStrings.ksRedoMoveCellBack,
										Cache.ActionHandlerAccessor, () => MergeCellContents(srcCell, prevCell, false));
		}

		IConstChartRow PreviousRow(IConstChartRow row)
		{
			var index = row.IndexInOwner;
			return index == 0 ? null : Chart.RowsOS[index - 1];
		}

		IConstChartRow NextRow(IConstChartRow row)
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
		/// <param name="srcCell"></param>
		public void MoveCellForward(ChartLocation srcCell)
		{
			ChartLocation dstCell;

			if (srcCell.ColIndex == AllMyColumns.Length - 1)
			{
				// Move to start of next row.
				var nextRow = NextRow(srcCell.Row);
				if (nextRow == null)
					return; // can't move further.
				dstCell = new ChartLocation(nextRow, 0);
			}
			else
			{
				// Normal case, merging/moving to cell on same row.
				dstCell = new ChartLocation(srcCell.Row, srcCell.ColIndex + 1);
			}
			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMoveCellForward, DiscourseStrings.ksRedoMoveCellForward,
										Cache.ActionHandlerAccessor, () => MergeCellContents(srcCell, dstCell, true));
		}

		/// <summary>
		/// Decrement a row index without going negative.
		/// </summary>
		/// <param name="irow"></param>
		/// <returns></returns>
		internal static int DecrementRowSafely(int irow)
		{
			return Math.Max(0, irow - 1);
		}

		/// <summary>
		/// Move the (first) word in a cell back (typically left). If the previous cell is empty, make a new
		/// WordGroup there; otherwise, move it into the WordGroup in the cell right. In the first column,
		/// moves to previous row. If there is only one word in the cell, merge everything into the destination.
		/// </summary>
		/// <param name="srcCell"></param>
		public void MoveWordBack(ChartLocation srcCell)
		{
			ChartLocation dstCell;
			var listOfPartsInSrc = PartsInCell(srcCell);
			int ipartInCell;
			// Start looking at the beginning of the cell
			var srcWordGroup = FindNextWordGroup(listOfPartsInSrc, out ipartInCell);
			if (srcWordGroup == null)
				return;
			if (!TryGetPreviousCell(srcCell, out dstCell))
				return;
			// If there's only one wordform in the WordGroup and no other WordGroups, just merge the cell contents.
			// N.B. If the first WordGroup is not the first cellPart in the cell, we can't use "1" below!!!!
			if (srcWordGroup.GetOccurrences().Count() == 1 &&
				(listOfPartsInSrc.Count == ipartInCell + 1 || FindFirstWordGroup(listOfPartsInSrc.GetRange(ipartInCell + 1, listOfPartsInSrc.Count - 1)) == null))
			{
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMoveWord, DiscourseStrings.ksRedoMoveWord,
											Cache.ActionHandlerAccessor, () => MergeCellContents(srcCell, dstCell, false));
				return;
			}

			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMoveWord, DiscourseStrings.ksRedoMoveWord,
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
				if(!fKeepOldWordGroup)
					DeleteCellPart(srcWordGroup);
			});
			m_lastMoveCell = dstCell;
		}

		/// <summary>
		/// Move the (last) word in a cell forward (typically right). If the next cell is empty, make a new
		/// WordGroup there; otherwise, move it into the WordGroup in the cell right. In the last column, it moves to
		/// the next row. If there is only one word in the cell, merge everything into the destination.
		/// </summary>
		/// <param name="srcCell"></param>
		public void MoveWordForward(ChartLocation srcCell)
		{
			ChartLocation dstCell;
			var listOfPartsInSrc = PartsInCell(srcCell);
			var ipartInCell = listOfPartsInSrc.Count; // start at end of cell and work backwards
			var srcWordGroup = FindPreviousWordGroup(listOfPartsInSrc, ref ipartInCell);
			if (srcWordGroup == null)
				return;
			if(!TryGetNextCell(srcCell, out dstCell))
				return;
			// If there's only one wordform in the WordGroup and no other WordGroups, just merge the cell contents.
			if (srcWordGroup.GetOccurrences().Count == 1 &&
				(listOfPartsInSrc.Count == 1 || (ipartInCell > 0 && FindLastWordGroup(listOfPartsInSrc.GetRange(0, ipartInCell - 1)) == null)))
			{
				UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMoveWord, DiscourseStrings.ksRedoMoveWord,
											Cache.ActionHandlerAccessor, () => MergeCellContents(srcCell, dstCell, true));
				return;
			}

			UndoableUnitOfWorkHelper.Do(DiscourseStrings.ksUndoMoveWord, DiscourseStrings.ksRedoMoveWord,
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
						iinsertAt++;
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
					DeleteCellPart(srcWordGroup);
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

		#region for test only
		static public string FTO_MovedTextMenuText
		{
			get { return DiscourseStrings.ksMovedFromMenuItem; }
		}
		static public string FTO_InsertAsClauseMenuText
		{
			get { return DiscourseStrings.ksMoveHereInNewClause; }
		}
		static public string FTO_MovedTextBefore
		{
			get { return DiscourseStrings.ksMovedTextBefore; }
		}
		static public string FTO_MovedTextAfter
		{
			get { return DiscourseStrings.ksMovedTextAfter; }
		}
		static public string FTO_InsertMissingMenuText
		{
			get { return DiscourseStrings.ksMarkMissingItem; }
		}
		static public string FTO_MakeDepClauseMenuText
		{
			get { return DiscourseStrings.ksMakeDepClauseMenuItem; }
		}
		static public string FTO_MakeSpeechClauseMenuItem
		{
			get { return DiscourseStrings.ksMakeSpeechClauseMenuItem; }
		}
		static public string FTO_MakeSongClauseMenuItem
		{
			get { return DiscourseStrings.ksMakeSongClauseMenuItem; }
		}
		static public string FTO_PreviousClauseMenuItem
		{
			get { return DiscourseStrings.ksPreviousClauseMenuItem; }
		}
		static public string FTO_NextClauseMenuItem
		{
			get { return DiscourseStrings.ksNextClauseMenuItem; }
		}
		static public string FTO_NextTwoClausesMenuItem
		{
			get { return DiscourseStrings.ksNextTwoClausesMenuItem; }
		}
		static public string FTO_NextNClausesMenuItem
		{
			get { return DiscourseStrings.ksNextNClausesMenuItem; }
		}
		static public string FTO_RowEndsParaMenuItem
		{
			get { return DiscourseStrings.ksRowEndsParaMenuItem; }
		}
		static public string FTO_RowEndsSentMenuItem
		{
			get { return DiscourseStrings.ksRowEndsSentMenuItem; }
		}
		static public string FTO_MergeAfterMenuItem
		{
			get { return DiscourseStrings.ksMergeAfterMenuItem; }
		}
		static public string FTO_MergeBeforeMenuItem
		{
			get { return DiscourseStrings.ksMergeBeforeMenuItem; }
		}
		static public string FTO_UndoMoveCellForward
		{
			get { return DiscourseStrings.ksUndoMoveCellForward; }
		}
		static public string FTO_RedoMoveCellForward
		{
			get { return DiscourseStrings.ksRedoMoveCellForward; }
		}
		static public string FTO_MoveMenuItem
		{
			get { return DiscourseStrings.ksMoveMenuItem; }
		}
		static public string FTO_ForwardMenuItem
		{
			get { return DiscourseStrings.ksForwardMenuItem; }
		}
		static public string FTO_BackMenuItem
		{
			get { return DiscourseStrings.ksBackMenuItem; }
		}
		static public string FTO_UndoMoveCellBack
		{
			get { return DiscourseStrings.ksUndoMoveCellBack; }
		}
		static public string FTO_RedoMoveCellBack
		{
			get { return DiscourseStrings.ksRedoMoveCellBack; }
		}

		static public string FTO_PreposeFromMenuItem
		{
			get { return DiscourseStrings.ksPreposeFromMenuItem; }
		}
		static public string FTO_PostposeFromMenuItem
		{
			get { return DiscourseStrings.ksPostposeFromMenuItem; }
		}
		static public string FTO_AnotherClause
		{
			get { return DiscourseStrings.ksAdvancedDlgMenuItem; }
		}
		static public string FTO_UndoPreposeFrom
		{
			get { return DiscourseStrings.ksUndoPreposeFrom; }
		}
		static public string FTO_RedoPreposeFrom
		{
			get { return DiscourseStrings.ksRedoPreposeFrom; }
		}
		static public string FTO_UndoPostposeFrom
		{
			get { return DiscourseStrings.ksUndoPostposeFrom; }
		}
		static public string FTO_RedoPostposeFrom
		{
			get { return DiscourseStrings.ksRedoPostposeFrom; }
		}
		static public string FTO_UndoMoveWord
		{
			get { return DiscourseStrings.ksUndoMoveWord; }
		}
		static public string FTO_RedoMoveWord
		{
			get { return DiscourseStrings.ksRedoMoveWord; }
		}
		static public string FTO_MoveWordMenuItem
		{
			get { return DiscourseStrings.ksMoveWordMenuItem; }
		}
		static public string FTO_InsertRowMenuItem
		{
			get { return DiscourseStrings.ksInsertRowMenuItem; }
		}
		static public string FTO_UndoInsertRow
		{
			get { return DiscourseStrings.ksUndoInsertRow; }
		}
		static public string FTO_RedoInsertRow
		{
			get { return DiscourseStrings.ksRedoInsertRow; }
		}
		static public string FTO_UndoAddMarker
		{
			get { return DiscourseStrings.ksUndoAddMarker; }
		}
		static public string FTO_RedoAddMarker
		{
			get { return DiscourseStrings.ksRedoAddMarker; }
		}
		static public string FTO_ClearFromHereOnMenuItem
		{
			get { return DiscourseStrings.ksClearFromHereOnMenuItem; }
		}
		static public string FTO_UndoClearChart
		{
			get { return DiscourseStrings.ksUndoClearChart; }
		}
		static public string FTO_RedoClearChart
		{
			get { return DiscourseStrings.ksRedoClearChart; }
		}
		static public string FTO_OtherMenuItem
		{
			get { return DiscourseStrings.ksOtherMenuItem; }
		}
		static public string FTO_RedoRemoveClauseMarker
		{
			get { return DiscourseStrings.ksRedoRemoveClauseMarker; }
		}
		static public string FTO_UndoRemoveClauseMarker
		{
			get { return DiscourseStrings.ksUndoRemoveClauseMarker; }
		}
		static public string FTO_UndoMakeNewRow
		{
			get { return DiscourseStrings.ksUndoMakeNewRow; }
		}
		static public string FTO_RedoMakeNewRow
		{
			get { return DiscourseStrings.ksRedoMakeNewRow; }
		}
		#endregion for test only

		internal static ListView MakeHeaderGroups()
		{
			ListView result = new ListView();

			return result;
		}

		internal void MakeMainHeaderCols(ListView view)
		{
			view.SuspendLayout();
			view.Columns.Clear();
			var ch = new ColumnHeader();
			ch.Text = ""; // otherwise default is 'column header'!
			view.Columns.Add(ch); // for row numbers column.
			foreach (var col in AllMyColumns)
			{
				ch = new ColumnHeader();

				// ensure NFC -- See LT-8815.
				//ch.Text = m_possRepo.GetObject(col.Hvo).Name.BestAnalysisAlternative.Text.Normalize();
				ch.Text = col.Name.BestAnalysisAlternative.Text.Normalize();
				view.Columns.Add(ch);
			}

			// Add one more column for notes.
			ch = new ColumnHeader();
			ch.Text = DiscourseStrings.ksNotesColumnHeader;
			view.Columns.Add(ch);

			view.ResumeLayout();
		}

		/// <summary>
		/// Given a set of column positions, return the one 'x' is in,
		/// that is, the largest index in positions such that x is less than positions[i+1].
		/// </summary>
		/// <param name="x"></param>
		/// <param name="positions"></param>
		/// <returns></returns>
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
		/// <param name="hvo"></param>
		/// <param name="hvoItem">the first clause</param>
		/// <returns></returns>
		internal bool IsClausePlaceholder(int hvo, out int hvoItem)
		{
			var part = m_cellPartRepo.GetObject(hvo);
			hvoItem = (int)SpecialHVOValues.kHvoUninitializedObject;
			if (part is IConstChartClauseMarker)
			{
				var clauses = (part as IConstChartClauseMarker).DependentClausesRS;
				if (clauses.Count > 0)
				{
					hvoItem = clauses[0].Hvo;
					return true;
				}
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
		/// <param name="hvo"></param>
		/// <returns></returns>
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
				return null; // No chart! Don't want to change any bookmark that might already be set.

			// If we aren't done charting the text yet, we need to set the bookmark
			// to the first uncharted wordform in the ribbon
			var analysisArray = NextUnchartedInput(1);
			if (analysisArray.Length > 0)
				return analysisArray[0]; // return hvo of wordform to bookmark
			// Do nothing for now, since the chart has been fully charted.
			// Enhance GordonM: Can we figure out what part of the chart is selected and save that?
			return null;
		}

		/// <summary>
		/// Answer true if wordGrp is a moved text item, by looking at its back-refs.
		/// </summary>
		/// <param name="wordGrp"></param>
		/// <returns></returns>
		static internal bool IsMovedText(IConstituentChartCellPart wordGrp)
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
		static internal bool IsMovedText(IConstituentChartCellPart wordGrp, out IConstChartMovedTextMarker result)
		{
			// Enhance Gordon: Is there a better way!?
			result = null;
			if (!(wordGrp is IConstChartWordGroup))
				return false;
			result = wordGrp.ReferringObjects.Where(
				target => target is IConstChartMovedTextMarker).Cast<IConstChartMovedTextMarker>().FirstOrDefault();
			return result != null;
		}

		/// <summary>
		/// Return a suitable style tag depending on whether hvo is the first or later
		/// moved text item in its row.
		/// </summary>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		internal string MovedTextTag(int hvoTarget)
		{
			if (HasPreviousMovedItemOnLine(m_chart, hvoTarget))
				return "movedText2";
			return "movedText";
		}

		static internal bool HasPreviousMovedItemOnLine(IDsConstChart chart, int hvoTarget)
		{
			foreach (var row in chart.RowsOS)
			{
				var cPrevMovedText = 0;
				foreach (var part in row.CellsOS)
				{
					if (part.Hvo == hvoTarget)
						return cPrevMovedText == 0 ? false : true;
					if (IsMovedText(part))
						cPrevMovedText++;
				}
			}
			return false; // desperation, never found it!
		}

		/// <summary>
		/// Return true if the cell(irow, icol) should be highlighted to indicate a valid ChOrph insertion point.
		/// </summary>
		/// <param name="irow"></param>
		/// <param name="icol"></param>
		/// <returns></returns>
		internal bool IsHighlightedCell(int irow, int icol)
		{
			if (IsChOrphActive)
			{
				int irowPrec = CurrHighlightCells[0];
				int irowFoll = CurrHighlightCells[2];
				if (irowPrec <= irow && irow <= irowFoll)
				{
					int icolPrec = CurrHighlightCells[1];
					int icolFoll = CurrHighlightCells[3];
					if (irowPrec == irowFoll)
						return (icolPrec <= icol && icol <= icolFoll);
					if (irow == irowPrec && icol >= icolPrec)
						return true;
					if (irow == irowFoll && icol <= icolFoll)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Makes a MovedText marker in a cell pointing to a WordGroup object.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="wordGrp"></param>
		/// <param name="iInsertMrkerHereInRow"></param>
		/// <returns></returns>
		internal IConstChartMovedTextMarker MakeMTMarker(ChartLocation cell, IConstChartWordGroup wordGrp,
			int iInsertMrkerHereInRow)
		{
			var fPreposed = IsPreposed(new ChartLocation((IConstChartRow) wordGrp.Owner,
				IndexOfColumnForCellPart(wordGrp)), cell);
			return MakeMTMarker(cell, wordGrp, iInsertMrkerHereInRow, fPreposed);
		}

		/// <summary>
		/// Most complete version.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="wordGrp"></param>
		/// <param name="iInsertMrkerHereInRow"></param>
		/// <param name="fPreposed"></param>
		/// <returns></returns>
		internal IConstChartMovedTextMarker MakeMTMarker(ChartLocation cell, IConstChartWordGroup wordGrp,
			int iInsertMrkerHereInRow, bool fPreposed)
		{
			return m_movedTextFact.Create(cell.Row, iInsertMrkerHereInRow, AllMyColumns[cell.ColIndex],
				fPreposed, wordGrp);
		}
	}

	/// <summary>
	/// This is used for a slice to ask the data tree to display a context menu.
	/// </summary>
	public delegate void RowModifiedEventHandler (object sender, RowModifiedEventArgs e);

	public class RowModifiedEventArgs : EventArgs
	{
		readonly IConstChartRow m_row;
		public RowModifiedEventArgs(IConstChartRow row)
		{
			m_row = row;
		}

		public IConstChartRow Row
		{
			get { return m_row; }
		}
	}

	class DepClauseMenuItem : ToolStripMenuItem
	{
		readonly ChartLocation m_srcCell;
		readonly IConstChartRow[] m_depClauses;
		ClauseTypes m_depType;

		public DepClauseMenuItem(string label, ChartLocation srcCell, IConstChartRow[] depClauses)
			: base(label)
		{
			m_depClauses = depClauses;
			m_srcCell = srcCell;
		}

		public IConstChartRow RowSource
		{
			get { return m_srcCell.Row; }
		}

		public int HvoRow
		{
			get { return m_srcCell.HvoRow; }
		}

		public IConstChartRow[] DepClauses
		{
			get { return m_depClauses; }
		}

		public int Column
		{
			get { return m_srcCell.ColIndex; }
		}

		public ChartLocation SrcCell
		{
			get { return m_srcCell; }
		}

		public ClauseTypes DepType
		{
			get { return m_depType; }
			set { m_depType = value; }
		}
	}

	internal class RowColMenuItem : ToolStripMenuItem
	{
		readonly ChartLocation m_srcCell;

		/// <summary>
		/// Creates a ToolStripMenuItem for a chart cell carrying the row and column information.
		/// Usually represents a cell that the user clicked.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="cloc">The chart cell location</param>
		public RowColMenuItem(string label, ChartLocation cloc)
			: base(label)
		{
			m_srcCell = cloc;
		}

		/// <summary>
		/// The source (other) column index.
		/// </summary>
		public int SrcColIndex
		{
			get { return m_srcCell.ColIndex; }
		}

		/// <summary>
		/// The ChartRow object.
		/// </summary>
		public IConstChartRow SrcRow
		{
			get { return m_srcCell.Row; }
		}

		/// <summary>
		/// The cell that was clicked.
		/// </summary>
		public ChartLocation SrcCell
		{
			get { return m_srcCell; }
		}
	}

	class OneValMenuItem : ToolStripMenuItem
	{
		int m_colSrc;
		public OneValMenuItem(string label, int colSrc)
			: base(label)
		{
			m_colSrc = colSrc;
		}

		/// <summary>
		/// The source (other) column.
		/// </summary>
		public int Source
		{
			get { return m_colSrc; }
		}
	}

	class TwoColumnMenuItem : ToolStripMenuItem
	{
		int m_colDst;
		int m_colSrc;
		IConstChartRow m_row;

		/// <summary>
		/// Make one that doesn't care about row.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="colDst"></param>
		/// <param name="colSrc"></param>
		public TwoColumnMenuItem(string label, int colDst, int colSrc)
			: this(label, colDst, colSrc, null)
		{
		}

		/// <summary>
		/// Make one that knows about row.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="colDst"></param>
		/// <param name="colSrc"></param>
		/// <param name="row"></param>
		public TwoColumnMenuItem(string label, int colDst, int colSrc, IConstChartRow row)
			:base(label)
		{
			m_colDst = colDst;
			m_colSrc = colSrc;
			m_row = row;
		}

		/// <summary>
		/// The source (other) column.
		/// </summary>
		public int Source
		{
			get { return m_colSrc; }
		}

		/// <summary>
		/// The Destination column (where the action will occur; where the menu appears).
		/// </summary>
		public int Destination
		{
			get { return m_colDst; }
		}

		/// <summary>
		/// The row in which everything takes place.
		/// </summary>
		public IConstChartRow Row
		{
			get { return m_row; }
		}
	}

	#region MergeCellContentsMethod

	/// <summary>
	/// A Method Object used to merge the contents of chart cells
	/// </summary>
	class MergeCellContentsMethod
	{
		readonly ConstituentChartLogic m_logic;
		private readonly IConstChartMovedTextMarkerRepository m_mtmRepo;
		readonly ChartLocation m_srcCell;
		readonly ChartLocation m_dstCell;
		readonly bool m_forward;
		List<IConstituentChartCellPart> m_cellPartsSrc;
		List<IConstituentChartCellPart> m_cellPartsDest;
		List<IConstChartWordGroup> m_wordGroupsSrc;
		List<IConstChartWordGroup> m_wordGroupsDest;
		IConstChartWordGroup m_wordGroupToMerge;
		IConstChartWordGroup m_wordGroupToMergeWith;

		public MergeCellContentsMethod(ConstituentChartLogic logic, ChartLocation srcCell, ChartLocation dstCell, bool forward)
		{
			m_logic = logic;
			m_srcCell = srcCell;
			m_dstCell = dstCell;
			m_forward = forward;
			m_mtmRepo = Cache.ServiceLocator.GetInstance<IConstChartMovedTextMarkerRepository>();
		}

		FdoCache Cache
		{
			get { return m_logic.Cache; }
		}

		IConstChartRow SrcRow
		{
			get { return m_srcCell.Row; }
		}

		int HvoSrcRow
		{
			get { return m_srcCell.HvoRow; }
		}

		int SrcColIndex
		{
			get { return m_srcCell.ColIndex; }
		}

		IConstChartRow DstRow
		{
			get { return m_dstCell.Row; }
		}

		int HvoDstRow
		{
			get { return m_dstCell.HvoRow; }
		}

		int DstColIndex
		{
			get { return m_dstCell.ColIndex; }
		}

		/// <summary>
		/// Remove the word group from m_wordGroupsSrc (and its Row).
		/// </summary>
		/// <param name="part"></param>
		/// <param name="indexDest">keeps track of Destination insertion point</param>
		int RemoveSourceWordGroup(IConstChartWordGroup part, int indexDest)
		{
			var irow = SrcRow.IndexInOwner;
			m_cellPartsSrc.Remove(part);
			m_wordGroupsSrc.Remove(part);
			SrcRow.CellsOS.Remove(part);
			if (SrcRow.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
			{
				RenumberRowsFromDeletedRow(irow);
				return 0;
			}
			if ((HvoDstRow == HvoSrcRow) && m_forward)
				indexDest--; // To compensate for lost item(WordGroup) in row.Cells
			return indexDest;
		}

		private void RenumberRowsFromDeletedRow(int irow)
		{
			m_logic.RenumberRows(ConstituentChartLogic.DecrementRowSafely(irow), false);
		}

		/// <summary>
		/// Remove the MovedTextMarker from wordGroupsToMergeWith (and its Row).
		/// </summary>
		/// <param name="mtm"></param>
		/// <param name="indexDest">keeps track of Destination insertion point</param>
		void RemoveRedundantMTMarker(IConstChartMovedTextMarker mtm, ref int indexDest)
		{
			m_cellPartsDest.Remove(mtm);
			DstRow.CellsOS.Remove(mtm);
			if ((HvoDstRow == HvoSrcRow) && m_forward)
				indexDest--; // To compensate for lost item(WordGroup) in row.Cells; is a preposed marker
		}

		internal void Run()
		{
			// Get markers and WordGroups from source and verify that source cell has some data.
			m_cellPartsSrc = m_logic.PartsInCell(m_srcCell);
			m_wordGroupsSrc = ConstituentChartLogic.CollectCellWordGroups(m_cellPartsSrc);
			if (m_wordGroupsSrc.Count == 0)
				return; // no words to move!!

			// If the destination contains a missing marker get rid of it! Don't try to 'merge' with it.
			m_logic.RemoveMissingMarker(m_dstCell);

			// Get markers and WordGroups from destination
			int indexDest; // This will keep track of where we are in rowDst.AppliesTo
			m_cellPartsDest = m_logic.CellPartsInCell(m_dstCell, out indexDest);
			// indexDest is now the index in the destination row's AppliesTo of the first WordGroup in the destination cell.
			m_wordGroupsDest = ConstituentChartLogic.CollectCellWordGroups(m_cellPartsDest);

			// Here is where we need to check to see if the destination cell contains any movedText markers
			// for text in the source cell. If it does, the marker goes away, the movedText feature goes away,
			// and that MAY leave us ready to do a PreMerge in the source cell prior to merging the cells.
			if (CheckForRedundantMTMarkers(ref indexDest))
				indexDest = TryPreMerge(indexDest);

			// The above check for MTMarkers may cause this 'if' to be true, but we may still need to delete objects
			if (m_cellPartsDest.Count == 0)
			{
				// The destination is completely empty. Just move the m_wordGroupsSrc.
				if (HvoSrcRow == HvoDstRow)
				{
					// This is where we worry about reordering SrcRow.Cells if other wordforms exist between
					// the m_wordGroupsSrc and the destination (since non-data stuff doesn't move).
					if (m_wordGroupsSrc.Count != m_cellPartsSrc.Count && m_forward)
					{
						MoveWordGroupsToEndOfCell(indexDest); // in preparation to moving data to next cell
					}
					m_logic.ChangeColumn(m_wordGroupsSrc.ToArray(), m_logic.AllMyColumns[DstColIndex],
							SrcRow);
				}
				else
				{
					if (SrcColIndex != DstColIndex)
						m_logic.ChangeColumnInternal(m_wordGroupsSrc.ToArray(), m_logic.AllMyColumns[DstColIndex]);

					// Enhance GordonM: If we ever allow markers between WordGroups, this [& ChangeRow()] will need to change.
					MoveCellPartsToDestRow(indexDest);
				}
				return;
			}

			if (m_logic.IsPreposedMarker(m_cellPartsDest[0]))
				indexDest++; // Insertion point should be after preposed marker

			// Set up possible coalescence of WordGroups.
			PrepareForMerge();

			// At this point 'm_wordGroupToMerge' and 'm_wordGroupToMergeWith' are the only WordGroups that might actually
			// coalesce. Neither is guaranteed to be non-null (either one could be movedText, which is not mergeable).
			// If either is null, there will be no coalescence.
			// But there may well be other word groups in 'm_wordGroupsSrc' that will need to move.

			if (m_wordGroupToMerge != null)
			{
				if (m_wordGroupToMergeWith != null)
				{
					// Merge the word groups and delete the empty one.
					indexDest = MergeTwoWordGroups(indexDest);
				}
				else
				{
					// Move the one(s) we have. This is accomplished by just leaving it in
					// the list (now m_wordGroupsSrc).
					// Enhance JohnT: Possibly we may eventually have different rules about
					// where in the destination cell to put it?
				}
			}

			if (m_wordGroupsSrc.Count == 0)
				return;

			// Change column of any surviving items in m_wordGroupsSrc.
			if (SrcColIndex != DstColIndex)
				m_logic.ChangeColumnInternal(m_wordGroupsSrc.ToArray(), m_logic.AllMyColumns[DstColIndex]);

			// If we're on the same row and there aren't any intervening markers, we're done.
			if (SrcRow.Hvo == DstRow.Hvo &&
				(indexDest == m_wordGroupsSrc[0].IndexInOwner + m_wordGroupsSrc.Count))
				return;

			indexDest = FindWhereToInsert(indexDest); // Needs an accurate 'm_wordGroupsDest'
			MoveCellPartsToDestRow(indexDest);

			// Review: what should we do about dependent clause markers pointing at the destination row?
		}

		/// <summary>
		/// Merge the word groups and delete the empty one, maintaining an accurate index
		/// in the destination.
		/// </summary>
		/// <param name="indexDest"></param>
		/// <returns></returns>
		private int MergeTwoWordGroups(int indexDest)
		{
			m_logic.MoveAnalysesBetweenWordGroups(m_wordGroupToMerge, m_wordGroupToMergeWith);

			// remove WordGroup from source lists and delete, keeping accurate destination index
			return RemoveSourceWordGroup(m_wordGroupToMerge, indexDest);
		}

		/// <summary>
		/// Moves the WordGroups to the end of the source cell in preparation to move them to the next cell.
		/// Solves confusion in row.Cells
		/// Situation: moving forward in same row, nothing in destination cell.
		/// </summary>
		private void MoveWordGroupsToEndOfCell(int iDest)
		{
			Debug.Assert(iDest > 0, "Bad destination index.");
			var istart = m_wordGroupsSrc[0].IndexInOwner;
			// Does MoveTo work when the src and dest sequences are the same? Yes.
			SrcRow.CellsOS.MoveTo(istart, istart + m_wordGroupsSrc.Count - 1, SrcRow.CellsOS, iDest);
		}

		private int TryPreMerge(int indexDest)
		{
			// Look through source WordGroups to see if any can be merged now that we've removed
			// a movedText feature prior to moving to a neighboring cell.
			var flastWasNotMT = false;
			for (var iSrc = 0; iSrc < m_wordGroupsSrc.Count; iSrc++)
			{
				var currWordGrp = m_wordGroupsSrc[iSrc];
				if (ConstituentChartLogic.IsMovedText(currWordGrp))
				{
					flastWasNotMT = false;
					continue;
				}
				if (flastWasNotMT)
				{
					// Merge this WordGroup with the last one and delete this one
					var destWordGrp = m_wordGroupsSrc[iSrc - 1];
					m_logic.MoveAnalysesBetweenWordGroups(currWordGrp, destWordGrp);
					// mark WordGroup for delete and remove from source lists, keeping accurate destination index
					indexDest = RemoveSourceWordGroup(currWordGrp, indexDest);
					iSrc--;
				}
				flastWasNotMT = true;
			}
			return indexDest;
		}

		private bool CheckForRedundantMTMarkers(ref int indexDest)
		{
			var found = false;
			for (var iDes = 0; iDes < m_cellPartsDest.Count; )
			{
				// Not foreach because we might delete from m_cellPartsDest
				var part = m_cellPartsDest[iDes];

				// The source cell part is still in its old row and column, so we can detect this directly.
				if (IsMarkerForCell(part, m_srcCell))
				{
					var part1 = part as IConstChartMovedTextMarker;
					// Turn off feature in source
					//TurnOffMovedTextFeatureFromMarker(part1, SrcRow);
					// Take out movedText marker, keep accurate destination index
					RemoveRedundantMTMarker(part1, ref indexDest);
					found = true;
					continue;
				}
				iDes++; // only if we do NOT clobber it.
			}
			return found;
		}

		/// <summary>
		/// Moves all WordGroups in m_wordGroupsSrc from SrcRow to DstRow.
		/// </summary>
		/// <param name="indexDest">Index in rowDst.Cells where insertion should occur.</param>
		private void MoveCellPartsToDestRow(int indexDest)
		{
			var irow = SrcRow.IndexInOwner;
			var istart = m_wordGroupsSrc[0].IndexInOwner;
			var iend = m_wordGroupsSrc[m_wordGroupsSrc.Count - 1].IndexInOwner;
			SrcRow.CellsOS.MoveTo(istart, iend, DstRow.CellsOS, indexDest);
			if (SrcRow.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				RenumberRowsFromDeletedRow(irow);
		}

		private void PrepareForMerge()
		{
			// Destination cell has something in it, but it might not be a WordGroup!
			if (m_wordGroupsDest.Count == 0)
				m_wordGroupToMergeWith = null;
			else
			{
				if (m_forward)
					m_wordGroupToMergeWith = m_wordGroupsDest[0];
				else
					m_wordGroupToMergeWith = m_wordGroupsDest[m_wordGroupsDest.Count - 1];

				if (ConstituentChartLogic.IsMovedText(m_wordGroupToMergeWith))
					// Can't merge with movedText, must append/insert merging WordGroup instead.
					m_wordGroupToMergeWith = null;
			}

			if (m_forward)
				m_wordGroupToMerge = m_wordGroupsSrc[m_wordGroupsSrc.Count - 1]; // Has to be something here.
			else
				m_wordGroupToMerge = m_wordGroupsSrc[0];

			if (ConstituentChartLogic.IsMovedText(m_wordGroupToMerge))
				// Can't merge with movedText, must append/insert merging WordGroup instead.
				m_wordGroupToMerge = null;
		}

		/// <summary>
		/// Return true if srcPart is a moved text marker with its destination in the specified cell.
		/// </summary>
		/// <param name="srcPart"></param>
		/// <param name="cell"></param>
		/// <returns></returns>
		private bool IsMarkerForCell(IConstituentChartCellPart srcPart, ChartLocation cell)
		{
			// typically it is null if the thing we're testing isn't the right kind of CellPart.
			if (srcPart == null || !(srcPart is IConstChartMovedTextMarker))
				return false;
			var target = (srcPart as IConstChartMovedTextMarker).WordGroupRA;
			if (target == null || target.ColumnRA != m_logic.AllMyColumns[cell.ColIndex])
				return false;
			return cell.Row.CellsOS.Contains(target);
		}

		/// <summary>
		/// If we find a moved text marker pointing at 'target', delete it.
		/// </summary>
		/// <param name="target"></param>
		private void CleanUpMovedTextMarkerFor(IConstChartWordGroup target)
		{
			IConstChartRow row;
			var marker = FindMovedTextMarkerFor(target, out row);
			if (marker == null)
				return;
			row.CellsOS.Remove(marker);
			//DeleteRowIfEmpty(row);
		}

		private IConstChartMovedTextMarker FindMovedTextMarkerFor(IConstChartWordGroup target, out IConstChartRow rowTarget)
		{
			// If we find a moved text marker pointing at 'target', return it and (through the 'out' var) its row.
			// Enhance JohnT: it MIGHT be faster (in long texts) to use a back ref. Or we might limit the
			// search to nearby rows...
			var myMTM = m_mtmRepo.AllInstances().Where(mtm => mtm.WordGroupRA == target).FirstOrDefault();
			if (myMTM != null)
			{
				rowTarget = myMTM.Owner as IConstChartRow;
				return myMTM;
			}
			rowTarget = null;
			return null;
		}

		/// <summary>
		/// Returns index of where in destination row's Cells sequence we want to insert
		/// remaining source WordGroups. Uses m_wordGroupsDest list. And m_forward and DstRow
		/// and m_cellPartsDest and indexDest.
		/// </summary>
		/// <param name="indexDest">Enters as the index in row.Cells of the beginning of the destination cell.</param>
		/// <returns></returns>
		private int FindWhereToInsert(int indexDest)
		{
			Debug.Assert(0 <= indexDest && indexDest <= DstRow.CellsOS.Count);
			if (m_cellPartsDest.Count == 0)
				return indexDest; // If indexDest == Cells.Count, we should take this branch.

			// Enhance GordonM: If we ever allow other markers before the first WordGroup or
			// we allow markers between WordGroups, this will need to change.
			if (m_forward)
				return indexDest;
			return indexDest + m_wordGroupsDest.Count;
		}
	}

	#endregion // MergeCellContentsMethod

	#region MakeMovedTextMethod

	/// <summary>
	/// Actually this class is used both to Make and Remove MovedText Markers/Features.
	/// </summary>
	class MakeMovedTextMethod
	{
		readonly ConstituentChartLogic m_logic;
		readonly ChartLocation m_movedTextCell;
		readonly ChartLocation m_markerCell;
		readonly bool m_fPrepose;
		readonly IAnalysis[] m_wordformsToMark;
		private IConstChartMovedTextMarker m_existingMarker; // only used for RemoveMovedFrom()

		public MakeMovedTextMethod(ConstituentChartLogic logic, ChartLocation movedTextCell,
			ChartLocation markerCell, AnalysisOccurrence begPoint, AnalysisOccurrence endPoint)
			: this(logic, movedTextCell, markerCell)
		{
			if (!begPoint.IsValid || !endPoint.IsValid)
				throw new ArgumentException("Bad begin or end point.");
			m_wordformsToMark = begPoint.GetAdvancingWordformsInclusiveOf(endPoint).ToArray();
		}

		public MakeMovedTextMethod(ConstituentChartLogic logic, ChartLocation movedTextCell,
			IConstChartMovedTextMarker marker)
		{
			m_logic = logic;
			m_movedTextCell = movedTextCell;
			m_existingMarker = marker;
			m_markerCell = new ChartLocation((IConstChartRow)marker.Owner, m_logic.IndexOfColumnForCellPart(marker));
			m_fPrepose = marker.Preposed;
		}

		public MakeMovedTextMethod(ConstituentChartLogic logic, ChartLocation movedTextCell,
			ChartLocation markerCell)
		{
			m_logic = logic;
			m_movedTextCell = movedTextCell;
			m_markerCell = markerCell;
			m_fPrepose = m_logic.IsPreposed(movedTextCell, markerCell);
		}

		#region PropertiesAndConstants

		FdoCache Cache
		{
			get { return m_logic.Cache; }
		}

		IConstChartRow MTRow
		{
			get { return m_movedTextCell.Row; }
		}

		IConstChartRow MarkerRow
		{
			get { return m_markerCell.Row; }
		}

		int MarkerColIndex
		{
			get { return m_markerCell.ColIndex; }
		}

		#endregion

		#region Method Object methods

		/// <summary>
		/// One of two main entry points for this method object. The other is RemoveMovedFrom().
		/// Perhaps this should be deprecated? Does it properly handle multiple WordGroups in a cell?
		/// </summary>
		/// <returns></returns>
		internal IConstChartMovedTextMarker MakeMovedFrom()
		{
			// Making a MovedFrom marker creates a Feature on the Actual row that needs to be updated.
			// Removed ExtraPropChangedInserter that caused a crash on Undo in certain contexts. (LT-9442)
			// Not real sure why everything still works!
			// Get rid of any empty marker in the cell where we will insert the marker.
			m_logic.RemoveMissingMarker(m_markerCell);

			if (m_wordformsToMark == null || m_wordformsToMark.Length == 0)
				return MarkEntireCell();
			return MarkPartialCell();
		}

		/// <summary>
		/// This handles the situation where we mark only the user-selected words as moved.
		/// m_hvosToMark member array contains the words to mark as moved
		///		(for now they should be contiguous)
		/// Deals with four cases:
		///		Case 1: the new WordGroup is at the beginning of the old, create a new one to hold the remainder
		///		Case 2: the new WordGroup is embedded within the old, create 2 new ones, first for the movedText
		///			and second for the remainder
		///		Case 3: the new WordGroup is at the end of the old, create a new one for the movedText
		///		Case 4: the new WordGroup IS the old WordGroup, just set the feature and marker
		///			(actually this last case should be handled by the dialog; it'll return no hvos)
		/// </summary>
		/// <returns></returns>
		private IConstChartMovedTextMarker MarkPartialCell()
		{
			// find the first WordGroup in this cell
			var cellContents = m_logic.PartsInCell(m_movedTextCell);
			var movedTextWordGrp = ConstituentChartLogic.FindFirstWordGroup(cellContents);
			Debug.Assert(movedTextWordGrp != null);

			// Does this WordGroup contain all our hvos and are they "in order" (no skips or unorderliness)
			int ilastHvo;
			var ifirstHvo = CheckWordGroupContainsAllOccurrenceHvos(movedTextWordGrp, out ilastHvo);
			if (ifirstHvo > -1)
			{
				var indexOfWordGrpInRow = movedTextWordGrp.IndexInOwner;
				// At this point we need to deal with our four cases:
				if (ifirstHvo == 0)
				{
					if (ilastHvo < movedTextWordGrp.GetOccurrences().Count - 1)
					{
						// Case 1: Add new WordGroup, put remainder of wordforms in it
						PullOutRemainderWordformsToNewWordGroup(movedTextWordGrp, indexOfWordGrpInRow, ilastHvo + 1);
					}
					// Case 4: just drops through here, sets Marker
				}
				else
				{
					if (ilastHvo < movedTextWordGrp.GetOccurrences().Count - 1)
					{
						// Case 2: Take MT wordforms and remainder wordforms out of original,
						// add new WordGroups for MT and for remainder

						// For now, just take out remainder wordforms and add a WordGroup for them.
						// The rest will be done when we drop through to Case 3
						PullOutRemainderWordformsToNewWordGroup(movedTextWordGrp, indexOfWordGrpInRow, ilastHvo + 1);
					}
					// Case 3: Take MT wordforms out of original, add a new WordGroup for the MT
					movedTextWordGrp = PullOutRemainderWordformsToNewWordGroup(movedTextWordGrp, indexOfWordGrpInRow, ifirstHvo);
				}
			}
			else return null; // Bail out because of a problem in our array of hvos (misordered or non-contiguous)

			// Now figure out where to insert the MTmarker
			var imarkerInsertAt = FindWhereToInsertMTMarker();

			// Insert the Marker
			return m_logic.MakeMTMarker(m_markerCell, movedTextWordGrp, imarkerInsertAt, m_fPrepose);
		}

		/// <summary>
		/// Pulls trailing wordforms out of a WordGroup and creates a new one for them.
		/// Then we return the new WordGroup.
		/// </summary>
		/// <param name="srcMovedText">The original WordGroup from which we will remove the moved
		/// 	text wordforms.</param>
		/// <param name="indexInRow"></param>
		/// <param name="ifirstWord"></param>
		/// <returns>The new WordGroup that will now contain the moved text wordforms.</returns>
		private IConstChartWordGroup PullOutRemainderWordformsToNewWordGroup(
			IConstChartWordGroup srcMovedText, int indexInRow, int ifirstWord)
		{
			var srcWordforms = srcMovedText.GetOccurrences();
			if (ifirstWord >= srcWordforms.Count)
				throw new ArgumentOutOfRangeException("ifirstWord");

			var oldSrcEnd = srcWordforms[srcWordforms.Count - 1]; // EndPoint of source becomes endPoint of new WordGroup
			var dstBegin = srcWordforms[ifirstWord];
			var newSrcEnd = dstBegin.PreviousWordform();
			// Move source EndPoint up to Analysis previous to first destination Analysis
			srcMovedText.EndSegmentRA = newSrcEnd.Segment;
			srcMovedText.EndAnalysisIndex = newSrcEnd.Index;

			return m_logic.MakeWordGroup(m_movedTextCell, indexInRow + 1, dstBegin, oldSrcEnd);
		}

		/// <summary>
		/// Checks our array of hvos to see that all are referenced by this WordGroup and that
		/// they are in order. For now they must be contigous too. Returns the index of the
		/// first hvo within Begin/EndAnalysisIndex for this WordGroup or -1 if check fails.
		/// The out variable returns the index of the last hvo to mark within this WordGroup.
		/// </summary>
		/// <param name="movedTextWordGrp"></param>
		/// <param name="ilastHvo"></param>
		/// <returns>ifirstHvo</returns>
		private int CheckWordGroupContainsAllOccurrenceHvos(IAnalysisReference movedTextWordGrp, out int ilastHvo)
		{
			Debug.Assert(m_wordformsToMark.Length > 0, "No words to mark!");
			Debug.Assert(movedTextWordGrp != null && movedTextWordGrp.IsValidRef, "No wordforms in this WordGroup!");
			var movedTextWordformList = movedTextWordGrp.GetOccurrences();
			Debug.Assert(movedTextWordformList != null, "No wordforms in this WordGroup!");
			var cwordforms = movedTextWordformList.Count;
			Debug.Assert(cwordforms > 0, "No wordforms in this WordGroup!");
			var ifirstHvo = -1;
			ilastHvo = -1;
			var ihvosToMark = 0;
			var chvosToMark = m_wordformsToMark.Length;
			for (var iwordform = 0; iwordform < cwordforms; iwordform++)
			{
				// TODO: Check this! Does it work?!
				if (movedTextWordformList[iwordform].Analysis == m_wordformsToMark[ihvosToMark])
				{
					// Found a live one!
					if (ihvosToMark == 0)
						ifirstHvo = iwordform; // found the first one!
					ilastHvo = iwordform;
					ihvosToMark++;

					// If we've found them all, get out before we have an array subscript error!
					if (ihvosToMark == chvosToMark)
						return ifirstHvo;
				}
				else
				{
					if (ifirstHvo > -1 && ihvosToMark < chvosToMark)
					{
						// We had found some, but we aren't finding anymore
						// and we haven't found all of them yet! Error!
						return -1;
					}
				}
			}
			// The only way to really hit the end of the 'for' loop is by failing to finish finding
			// all of the hvosToMark array, so this is an error too.
			return -1;
		}

		private IConstChartMovedTextMarker MarkEntireCell()
		{
			// This handles the case where we mark the entire cell (CCWordGroup) as moved.
			// Review: What happens if there is more than one WordGroup in the cell already?
			// At this point, only the first will get marked as moved.
			var wordGroupMovedText = (IConstChartWordGroup)m_logic.FindCellPartInColumn(m_movedTextCell, true);
			Debug.Assert(wordGroupMovedText != null);

			// Now figure out where to insert the MTmarker
			var icellPartInsertAt = FindWhereToInsertMTMarker();

			return m_logic.MakeMTMarker(m_markerCell, wordGroupMovedText, icellPartInsertAt);
		}

		/// <summary>
		/// Insert BEFORE anything already in column, if Preposed; otherwise, after.
		/// </summary>
		/// <returns></returns>
		private int FindWhereToInsertMTMarker()
		{
			var markerCell = new ChartLocation(MarkerRow, (m_fPrepose ? MarkerColIndex - 1: MarkerColIndex));
			return m_logic.FindIndexOfCellPartInLaterColumn(markerCell);
		}

		internal void RemoveMovedFrom()
		{
			if (m_existingMarker == null)
				FindMarkerAndDelete();
			else
				m_markerCell.Row.CellsOS.Remove(m_existingMarker);

			// Handle cases where after removing, there are multiple WordGroups that can be merged.
			// If the removed movedFeature was part of the cell,
			// there could easily be 3 WordGroups to merge after removing.
			CollapseRedundantWordGroups();
		}

		private void FindMarkerAndDelete()
		{
			foreach (var part in m_logic.PartsInCell(m_markerCell).Where(
				cellPart => m_logic.IsMarkerOfMovedFrom(cellPart, m_movedTextCell)))
			{
				// Remove Marker from Row
				MarkerRow.CellsOS.Remove(part);
			}
		}

		private void CollapseRedundantWordGroups()
		{
			// Enhance GordonM: May need to do something different here if we can put markers between WordGroups.

			// Get ALL the cellParts
			var cellPartList = m_logic.PartsInCell(m_movedTextCell);

			for (var icellPart = 0; icellPart < cellPartList.Count - 1; icellPart++)
			{
				var currCellPart = cellPartList[icellPart];
				if (!(currCellPart is IConstChartWordGroup) || ConstituentChartLogic.IsMovedText(currCellPart))
					continue;
				while (icellPart < cellPartList.Count - 1) // Allows swallowing multiple WordGroups if conditions are right.
				{
					var nextCellPart = cellPartList[icellPart + 1];
					if (!(nextCellPart is IConstChartWordGroup))
						break;
					if (ConstituentChartLogic.IsMovedText(nextCellPart))
					{
						icellPart++; // Skip current AND next, since next is MovedText
						continue;
					}
					// Conditions are right! Swallow the next WordGroup into the current one!
					SwallowRedundantWordGroup(cellPartList, icellPart);
				}
			}
		}

		private void SwallowRedundantWordGroup(List<IConstituentChartCellPart> cellPartList, int icellPart)
		{
			// Move all analyses from cellPartList[icellPart+1] to end of cellPartList[icellPart].
			var srcWordGrp = cellPartList[icellPart+1] as IConstChartWordGroup;
			var dstWordGrp = cellPartList[icellPart] as IConstChartWordGroup;
			m_logic.MoveAnalysesBetweenWordGroups(srcWordGrp, dstWordGrp);

			// Clean up
			cellPartList.Remove(srcWordGrp); // remove swallowed WordGroup from list
			MTRow.CellsOS.Remove(srcWordGrp); // remove swallowed WordGroup from row (& therefore delete it)
		}

		#endregion
	}

	#endregion // MakeMovedTextMethod

	// used for user-defined markers
	public class RowColPossibilityMenuItem : ToolStripMenuItem
	{
		private readonly ChartLocation m_srcCell;
		internal int m_hvoPoss;
		public RowColPossibilityMenuItem(ChartLocation cloc, int hvoPoss)
		{
			m_srcCell = cloc;
			m_hvoPoss = hvoPoss;
		}

		public ChartLocation SrcCell
		{
			get { return m_srcCell; }
		}

	}

	/// <summary>
	/// This interface defines the functionality that the logic needs from the ribbon.
	/// Test code implements mock ribbons.
	/// </summary>
	public interface IInterlinRibbon
	{
		AnalysisOccurrence[] SelectedOccurrences { get; }
		void CacheRibbonItems(List<AnalysisOccurrence> wordForms);
		void MakeInitialSelection();
		void SelectFirstOccurence();
		int OccurenceListId { get; }
		int EndSelLimitIndex { get; set; }
		AnalysisOccurrence SelLimOccurrence { get; set;}
		ISilDataAccessManaged Decorator { get; }
	}

	/// <summary>
	/// Update the ribbon during Undo or Redo of annotating something.
	/// Note that we create TWO of these, one that is the first action in the group, and one that is the last.
	/// The first is for Undo, and updates the ribbon to the appropriate state for when the action is undone.
	/// (It needs to be first so it will be the last thing undone.)
	/// The last is for Redo, and updates the ribbon after the task is redone (needs to be last so it is the
	/// last thing redone).
	/// </summary>
	public class UpdateRibbonAction : IUndoAction
	{
		readonly ConstituentChartLogic m_logic;
		readonly bool m_fForRedo;

		public UpdateRibbonAction(ConstituentChartLogic logic, bool fForRedo)
		{
			m_logic = logic;
			m_fForRedo = fForRedo;
		}

		public void UpdateUnchartedWordforms()
		{
			var unchartedWordforms = m_logic.NextUnchartedInput(ConstituentChartLogic.kMaxRibbonContext);
			m_logic.Ribbon.CacheRibbonItems(unchartedWordforms.ToList()); // now handles PropChanged???

			m_logic.Ribbon.SelectFirstOccurence();
		}

		#region IUndoAction Members

		public void Commit()
		{
		}

		public bool IsDataChange
		{
			get { return false; } // no real data changes as a result of this.
		}

		public bool IsRedoable
		{
			get { return true; }
		}

		public bool Redo()
		{
			if (m_fForRedo)
				UpdateUnchartedWordforms();
			return true;
		}

		public bool SuppressNotification
		{
			set {  }
		}

		public bool Undo()
		{
			if (!m_fForRedo)
				UpdateUnchartedWordforms();
			return true;
		}

		#endregion
	}

	internal class OccurrenceSorter : IComparer<AnalysisOccurrence>
	{

		#region IComparer<AnalysisOccurrence> Members

		/// <summary>
		/// Compares two AnalysisOccurrences to determine which comes first in a text.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>Positive integer if y occurs prior to x in text.
		/// Negative integer if x occurs prior to y in the text.</returns>
		public int Compare(AnalysisOccurrence x, AnalysisOccurrence y)
		{
			if (x.Paragraph.Hvo == y.Paragraph.Hvo)
			{
				if (x.Segment.Hvo == y.Segment.Hvo)
				{
					return x.Index - y.Index;
				}
				return x.Segment.IndexInOwner - y.Segment.IndexInOwner;
			}
			return x.Paragraph.IndexInOwner - y.Paragraph.IndexInOwner;
		}

		#endregion

	}

	// Item used in row combo box.
	internal class RowMenuItem
	{
		readonly IConstChartRow m_row;
		internal RowMenuItem(IConstChartRow row)
		{
			m_row = row;
		}

		// Return the ChartRow's row label (1a, 1b, etc.) as a string
		public override string ToString()
		{
			return m_row.Label.Text;
		}

		public IConstChartRow Row
		{
			get { return m_row; }
		}
	}

	// Item used in column combo box.
	internal class ColumnMenuItem
	{
		ICmPossibility m_column;

		internal ColumnMenuItem(ICmPossibility column)
		{
			m_column = column;
		}

		public override string ToString()
		{
			return m_column.Name.BestAnalysisAlternative.Text;
		}

		public ICmPossibility Column
		{
			get { return m_column; }
		}
	}
}
