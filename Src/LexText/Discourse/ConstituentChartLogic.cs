using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.IText;

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
		protected FdoCache m_cache;
		protected int m_hvoStText;
		protected IInterlinRibbon m_ribbon;
		DsConstChart m_chart;
		ChartLocation m_lastMoveCell; // row and column of last Move operation

		int[] m_allMyColumns;
		Set<int> m_indexGroupEnds; // indices of ends of column Groups (for LT-8104; setting apart Nucleus)
		int[] m_currHighlightCells; // Keeps track of highlighted cells when dealing with ChartOrphan insertion.

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
		public const int IndexOfFirstTemplateColumn = 1;

		public event RowModifiedEventHandler RowModifiedEvent;

		public event EventHandler Ribbon_Changed;

		public ConstituentChartLogic(FdoCache cache, DsConstChart chart, int hvoStText)
			:this(cache)
		{
			m_hvoStText = hvoStText;
			m_chart = chart;
		}

		#region kflid Constants
		const int kflidAbbreviation		= (int)CmPossibility.CmPossibilityTags.kflidAbbreviation;

		const int kflidAnnotationType	= (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType;
		const int kflidCompDetails		= (int)CmAnnotation.CmAnnotationTags.kflidCompDetails;
		const int kflidComment			= (int)CmAnnotation.CmAnnotationTags.kflidComment;
		const int kflidInstanceOf		= (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf;

		const int kflidBeginObject		= (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject;
		const int kflidBeginOffset		= (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset;

		const int kflidAppliesTo		= (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo;
		const int kflidRows				= (int)DsConstChart.DsConstChartTags.kflidRows;

		const int kflidParagraphs		= (int)StText.StTextTags.kflidParagraphs;
		#endregion

		internal FdoCache Cache
		{
			get { return m_cache; }
		}

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
				int irowOld = 0;
				int crowOld = 0;
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
					Cache.PropChanged(m_chart.Hvo, kflidRows, irowNew, crowNew, crowNew);
				}
				if (m_hvoStText != 0 && crowOld > 0) // Some danger of repeating myself, but probably worth it.
					Cache.PropChanged(m_chart.Hvo, kflidRows, irowOld, crowOld, crowOld);
			}
		}

		/// <summary>
		/// Returns true if CurrHighlightCells has been set (i.e. ChOrph input is set up and pending).
		/// </summary>
		/// <returns></returns>
		internal bool IsChOrphActive
		{
			get
			{
				return !(CurrHighlightCells[0] == -1);
			}
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

		/// <summary>
		/// Make one and set the other stuff later.
		/// </summary>
		/// <param name="cache"></param>
		public ConstituentChartLogic(FdoCache cache)
		{
			m_cache = cache;
		}

		public DsConstChart Chart
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
		public int[] AllMyColumns
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
			string engLabel = m_cache.GetMultiStringAlt(AllMyColumns[icol], (int)CmPossibility.CmPossibilityTags.kflidName,
				m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en")).Text;
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
				int[] result = NextUnchartedInput(1);
				return (result.Length == 0);
			}
		}

		/// <summary>
		/// This routine raises the ribbon changed event. It should get run whenever there is a PropChanged
		/// on the Ribbon's AnnotationListId.
		/// </summary>
		internal void RaiseRibbonChgEvent()
		{
			if (Ribbon_Changed != null)
				Ribbon_Changed(this, new EventArgs()); // raise event that Ribbon has changed
		}

		/// <summary>
		/// Return the next wfics that have not yet been added to the chart (up to maxContext of them).
		/// </summary>
		/// <param name="maxContext"></param>
		/// <returns></returns>
		public int[] NextUnchartedInput(int maxContext)
		{
			if (m_cache.DatabaseAccessor == null)
			{
				ICmAnnotationDefn WficType = CmAnnotationDefn.Twfic(m_cache);
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				// Ouch! Test code in production! But maybe will be useful for real in-memory version one day?
				Set<ICmBaseAnnotation> possibleTargets = new Set<ICmBaseAnnotation>();
				foreach (CmAnnotation ca in m_cache.LangProject.AnnotationsOC)
				{
					CmBaseAnnotation cba = ca as CmBaseAnnotation;
					if (cba != null && cba.BeginObjectRA != null && cba.BeginObjectRA.OwnerHVO == m_hvoStText
						&& ca.AnnotationTypeRAHvo == WficType.Hvo)
					{
						// might be interesting!
						possibleTargets.Add(cba);
					}
				}
				ICmAnnotationDefn CcaType = CmAnnotationDefn.ConstituentChartAnnotation(m_cache);
				foreach (CmAnnotation ca in m_cache.LangProject.AnnotationsOC)
				{
					if (ca is CmIndirectAnnotation)
					{
						foreach (CmObject obj in (ca as CmIndirectAnnotation).AppliesToRS)
						{
							if (ca.AnnotationTypeRAHvo != CcaType.Hvo)
								continue;
							if (!(obj is CmBaseAnnotation))
								continue;
							if (possibleTargets.Contains(obj as CmBaseAnnotation))
								possibleTargets.Remove(obj as CmBaseAnnotation);
						}
					}
				}
				List<ICmBaseAnnotation> targets = new List<ICmBaseAnnotation>(possibleTargets);
				targets.Sort(new WficSorter());
				int resultLength = Math.Min(targets.Count, maxContext);
				int[] result = new int[resultLength];
				for (int i = 0; i < resultLength; i++)
					result[i] = targets[i].Hvo;
				return result;
			}
			if (m_chart == null)
				return new int[0];
			else
				return DiscourseDbOps.NextUnusedInput(m_cache, m_hvoStText, maxContext, m_chart.Hvo);
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
			int[] nui = NextUnchartedInput(1);
			if (nui.Length == 0)
				return false;
			return IsChOrph(nui[0], out iPara, out offset);
		}

		protected bool IsChOrph(ICmBaseAnnotation cba)
		{
			return IsChOrph(cba.Hvo);
		}

		protected bool IsChOrph(int hvoCba)
		{
			int dummy1, dummy2;
			return IsChOrph(hvoCba, out dummy1, out dummy2);
		}

		/// <summary>
		/// Checks a base annotation (usually the next Ribbon item) to see if it a Chart Orphan (ChOrph).
		/// Compares the paragraph (BeginObject) and offset (BeginOffset) with the last charted Wfic
		/// to determine whether this one is an orphan from earlier in the chart.
		/// </summary>
		/// <param name="hvoCba">should be a Wfic</param>
		/// <param name="iPara">index of StTxtPara for the annotation</param>
		/// <param name="offset">offset into paragraph for the annotation</param>
		/// <returns>true if this one belongs earlier in the chart, false if it could be added to the end of the chart</returns>
		protected bool IsChOrph(int hvoCba, out int iPara, out int offset)
		{
			// Enhance GordonM: Can we use John's nifty WficSorter class?
			iPara = -1;
			offset = -1;
			if (hvoCba == 0)
			{
				Debug.Assert(false, "Invalid hvo.");
				return false;
			}
			if (Chart == null || Chart.RowsRS.Count == 0)
				return false; // No chart, therefore the whole text is ChOrphs! But that don't count.
			if (Cache == null || Cache.GetIntProperty(hvoCba, kflidAnnotationType) != CmAnnotationDefn.Twfic(Cache).Hvo)
			{
				Debug.Assert(false, "No cache or hvo is not a Twfic");
				return false;
			}

			// Get last charted Twfic
			ICmBaseAnnotation lastWfic = GetLastChartedWfic();
			if (lastWfic == null)
				return false; // This should mean the same as no charted text.

			// Compare paragraph index of this Annotation's paragraph with last charted Wfic's paragraph index
			iPara = GetParagraphIndexForWfic(hvoCba);
			//iPara = RawTextPane.GetParagraphIndexForAnnotation(Cache, hvoCba);
			int iLastChartedPara = GetParagraphIndexForWfic(lastWfic.Hvo);
			//int iLastChartedPara = RawTextPane.GetParagraphIndexForAnnotation(Cache, lastWfic.Hvo);
			if (iPara > iLastChartedPara)
				return false; // the paragraph for which hvoCba is a part is not yet charted.

			// This is here because 'offset' is an 'out' var and I don't want to return 'true' w/an invalid offset.
			offset = GetBeginOffset(hvoCba);
			if (iPara < iLastChartedPara)
				return true; // hvoCba represents a ChOrph from a previous paragraph of the text

			// We are in the same paragraph of text as the last charted piece.
			// Compare BeginOffset with last charted Twfic's BeginOffset
			return (offset < GetBeginOffset(lastWfic.Hvo));
		}

		private ICmBaseAnnotation GetLastChartedWfic()
		{
			ICmBaseAnnotation wfic = null;
			ICmIndirectAnnotation latestRow = LastRow; // shouldn't assume LastRow HAS a Wfic!
			while (true)
			{
				ICmIndirectAnnotation lastWficCca = FindLastCcaWithWfics(CcasInRow(latestRow));
				if (lastWficCca != null)
				{
					wfic = lastWficCca.AppliesToRS[lastWficCca.AppliesToRS.Count - 1] as ICmBaseAnnotation;
					return wfic;
				}
				latestRow = PreviousRow(latestRow);
				if (latestRow == null)
					return null;
			}
		}

		protected int GetBeginOffset(int hvoCba)
		{
			Debug.Assert(hvoCba != 0);
			return Cache.GetIntProperty(hvoCba, kflidBeginOffset);
		}

		/// <summary>
		/// Find the index of the paragraph in the text of which hvoWfic is a part.
		/// Cannibalized from RawTextPane.
		/// </summary>
		/// <param name="hvoWfic"></param>
		/// <returns></returns>
		protected int GetParagraphIndexForWfic(int hvoWfic)
		{
			// Enhance 5.2.1: Once we are allowed to modify IText.dll, maybe we should refactor
			// to use RawTextPane versions.
			int hvoPara = Cache.MainCacheAccessor.get_ObjectProp(hvoWfic, kflidBeginObject);
			return GetParagraphIndexForPara(hvoPara);
		}

		private int GetParagraphIndexForPara(int paraHvo)
		{
			// Enhance 5.2.1: Once we are allowed to modify IText.dll, maybe we should refactor
			// to use RawTextPane versions.
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int cpara = sda.get_VecSize(StTextHvo, kflidParagraphs);
			for (int ihvoPara = 0; ihvoPara < cpara; ihvoPara++)
			{
				if (sda.get_VecItem(StTextHvo, kflidParagraphs, ihvoPara) == paraHvo)
					return ihvoPara;
			}
			return -1;
		}

		/// <summary>
		/// Get cell location/reference for Preceding and Following Wfic used for showing ChOrph insertion options.
		/// Returns null ChartLocation if call failed to find a cell containing a Wfic in the appropriate direction.
		/// </summary>
		/// <param name="iPara">ChOrph input: paragraph index</param>
		/// <param name="offset">ChOrph input: offset into paragraph</param>
		/// <param name="precCell">Result: ChartLocation of preceding Wfic's cell</param>
		/// <param name="follCell">Result: ChartLocation of following Wfic's cell</param>
		protected internal void GetWficCellsBorderingChOrph(int iPara, int offset,
			out ChartLocation precCell, out ChartLocation follCell)
		{
			Debug.Assert(iPara >= 0);
			Debug.Assert(offset >= 0);
			Debug.Assert(m_chart != null && m_chart.RowsRS.Count > 0); // IsChOrph() should return false in these conditions

			// Set 'out' variables for Chart Cell references
			precCell = null;
			follCell = null;

			// Set temporary variables
			int icolPrec = -1;
			int icolFoll = -1;
			ICmIndirectAnnotation rowPrec = null;
			ICmIndirectAnnotation rowFoll = null;

			int hvoCcaPreceding = 0;
			int hvoCcaFollowing = 0;
			// Loop through rows of chart
			foreach (ICmIndirectAnnotation row in m_chart.RowsRS)
			{
				// Find the first wfic in each row
				ICmIndirectAnnotation cca = FindFirstCcaWithWfics(CcasInRow(row));
				if (cca == null)
					continue; // No wfics in this chart row, look in next one.

				// Found first Wfic in this row!
				// Compare its text-logical position with the input ChOrph's text-logical position.
				if (!CcaStartsBeforeChOrph(iPara, offset, cca))
				{
					rowFoll = row;
					hvoCcaFollowing = cca.Hvo;
					icolFoll = IndexOfColumnForCca(hvoCcaFollowing);
					break;
				}
				rowPrec = row;
				hvoCcaPreceding = cca.Hvo;
				icolPrec = IndexOfColumnForCca(hvoCcaPreceding);
			}
			// We either hit the end of the chart, or we found the following Wfic Cca.
			// [Actually there IS a way to hit the end of the chart 'legitimately'.
			//   The chOrph belongs in the last row of the chart that contains wfics (so there are only non-wfic rows after).]
			if (rowFoll == null)
			{
				// Hit the end of the chart without finding a wfic that belongs after the ChOrph.
				// Go back to 'rowPrec' (last row that found a wfic) and check for other wfics in the row
				// after ChOrph (ought to exist by definition, although it COULD be the same Preceding CCA).
				rowFoll = rowPrec;
				ICmIndirectAnnotation cca = FindLastCcaWithWfics(CcasInRow(rowFoll));
				icolFoll = IndexOfColumnForCca(cca.Hvo);
				if (IsChOrphWithinCCA(iPara, offset, cca))
				{
					icolPrec = icolFoll; // Fixes at least part of LT-8380
					// Set 'out' variables
					if (rowFoll != null)
						follCell = new ChartLocation(icolFoll, rowFoll);
					if (rowPrec != null)
						precCell = new ChartLocation(icolPrec, rowPrec);
					return;
				}
			}
			// See if we can narrow the search forward
			if (rowPrec == null)  // The first Wfic found in the chart is already AFTER the ChOrph.
			{
				icolPrec = 0; // Set to first column, first row.
				rowPrec = m_chart.RowsRS[0];
			}
			else
			{
				icolPrec = NarrowSearchForward(iPara, offset, new ChartLocation(icolPrec, rowPrec));
				// See if we can narrow the search backward
				if (rowPrec.Hvo != rowFoll.Hvo)
				{
					ChartLocation temp = CheckFollowingRowPosition(iPara, offset, new ChartLocation(icolFoll, rowFoll));
					icolFoll = temp.ColIndex;
					rowFoll = temp.RowAnn;
				}
				if (rowPrec.Hvo == rowFoll.Hvo) // rowFoll might have been changed in CheckFollowingRowPosition()
					icolFoll = NarrowSearchBackward(iPara, offset, new ChartLocation(icolPrec, rowPrec), icolFoll);
			}
			// By the time we get here, we should be able to return the right answer.
			// Set 'out' variables
			if (rowFoll != null)
				follCell = new ChartLocation(icolFoll, rowFoll);
			if (rowPrec != null)
				precCell = new ChartLocation(icolPrec, rowPrec);
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
			// We know that the first Wfic in this row is "before" our ChOrph's logical position
			// And we know that the right Preceding Cell is in this row
			int result = cell.ColIndex;
			ICmIndirectAnnotation ccaFound;
			for (int icolNew = cell.ColIndex + 1; icolNew < AllMyColumns.Length; icolNew++)
			{
				ccaFound = FindFirstCcaWithWfics(CcasInCell(new ChartLocation(icolNew, cell.RowAnn)));
				if (ccaFound == null)
					continue;
				if (!CcaStartsBeforeChOrph(iPara, offset, ccaFound))
					break;
				result = icolNew; // Keep incrementing our 'ref' index until we get past our logical position
			}
			return result;
		}

		/// <summary>
		/// Moving backward, can we narrow our search to another cell?
		/// The first Wfic in this row is "after" our ChOrph's logical position, and we might need
		/// to back up a row (or more) to see if there is a Wfic in an earlier row "after" or not.
		/// This routine prepares the field for NarrowSearchBackward().
		/// </summary>
		/// <param name="iPara">ChOrph input: paragraph index</param>
		/// <param name="offset">ChOrph input: offset into paragraph</param>
		/// <param name="cell">ChartLocation of Wfic's cell</param>
		/// <returns>ChartLocation moved up, if possible</returns>
		private ChartLocation CheckFollowingRowPosition(int iPara, int offset, ChartLocation cell)
		{
			ChartLocation result = cell;
			ICmIndirectAnnotation ccaFound;
			ICmIndirectAnnotation tempRow = null;
			int tempColIndex = 0;
			ICmIndirectAnnotation rowNew = PreviousRow(cell.RowAnn);
			int icolNew = cell.ColIndex;
			while (rowNew != null)
			{
				ccaFound = FindLastCcaWithWfics(CcasInRow(rowNew));
				if (ccaFound == null)
				{
					// No Wfics in this row, go up again!
					rowNew = PreviousRow(rowNew);
					continue;
				}
				if (CcaStartsBeforeChOrph(iPara, offset, ccaFound))
				{
					// Either we found a CCA that 'contains' our ChOrph's position
					// or we've gone too many rows back and our 'ref' vars were already set right.
					if (IsChOrphWithinCCA(iPara, offset, ccaFound))
						result = new ChartLocation(IndexOfColumnForCca(ccaFound.Hvo), rowNew);
					return result;
				}
				// Updating our 'temp' variables.
				tempRow = rowNew;
				tempColIndex = IndexOfColumnForCca(ccaFound.Hvo);
				break;
			}
			ChartLocation temp = new ChartLocation(tempColIndex, tempRow);
			if (!temp.IsSameLocation(cell))
				 result = temp;
			// We are now ready for NarrowSearchBackward().
			return result;
		}

		/// <summary>
		/// Moving backward, can we narrow our search to another cell?
		/// The last Wfic in this row is "after" our ChOrph's logical position, but the first isn't
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
				ICmIndirectAnnotation ccaFound = FindLastCcaWithWfics(CcasInCell(new ChartLocation(icolNew, precCell.RowAnn)));
				if (ccaFound == null)
					continue;
				if (!CcaStartsBeforeChOrph(iPara, offset, ccaFound))
				{
					result = icolNew; // Keep decrementing our result index until we get past our logical position
					continue;
				}
				if (IsChOrphWithinCCA(iPara, offset, ccaFound))
					result = icolNew;
				break;
			}
			return result;
		}

		/// <summary>
		/// Assumes the CCA is a Wfic group. Returns 'true' if the first Wfic referenced by the CCA is
		/// text-logically prior to the ChOrph at StTxtPara[iPara] and offset. The logical ChOrph position
		/// might be internal to the CCA, not necessarily after it.
		/// Returns 'false' if the logical ChOrph position is before this CCA.
		/// </summary>
		/// <param name="iPara">ChOrph paragraph index</param>
		/// <param name="offset">ChOrph offset into paragraph</param>
		/// <param name="cca">Assumes cca is a wfic group.</param>
		/// <returns></returns>
		private bool CcaStartsBeforeChOrph(int iPara, int offset, ICmIndirectAnnotation cca)
		{
			// Assumes that cca is a wfic group
			Debug.Assert(cca != null && cca.AppliesToRS.Count > 0);

			int hvoCba = (cca.AppliesToRS[0] as ICmBaseAnnotation).Hvo;
			//int imyPara = RawTextPane.GetParagraphIndexForAnnotation(Cache, hvoCba);
			int imyPara = GetParagraphIndexForWfic(hvoCba);
			if (imyPara < iPara)
				return true;
			if (imyPara == iPara && GetBeginOffset(hvoCba) < offset)
				return true;
			return false;
		}

		/// <summary>
		/// Returns 'true' if the logical ChOrph position is between the (multiple) Wfics of this CCA.
		/// </summary>
		/// <param name="iPara">ChOrph paragraph index</param>
		/// <param name="offset">ChOrph offset into paragraph</param>
		/// <param name="cca">Assumes cca is a wfic group from the same paragraph as the ChOrph.</param>
		/// <returns></returns>
		private bool IsChOrphWithinCCA(int iPara, int offset, ICmIndirectAnnotation cca)
		{
			// Assumes that cca is a wfic group from the same paragraph as the ChOrph.
			Debug.Assert(cca != null && cca.AppliesToRS.Count > 0);

			int cAppliesTo = cca.AppliesToRS.Count;
			if (cAppliesTo == 1)
				return false;

			// Test first Wfic in CCA, is ChOrph offset less?
			ICmBaseAnnotation wfic = cca.AppliesToRS[0] as ICmBaseAnnotation;
			if (offset < GetBeginOffset(wfic.Hvo))
				return false;

			// Test last Wfic in CCA, is ChOrph offset greater? Need to check iPara first!
			wfic = cca.AppliesToRS[cAppliesTo - 1] as ICmBaseAnnotation;
			if (GetParagraphIndexForWfic(wfic.Hvo) != iPara)
			{
				// The last wfic and the first in this CCA are in different paragraphs,
				// therefore our answer is TRUE
				return true;
			}
			return (offset < GetBeginOffset(wfic.Hvo));
		}

		/// <summary>
		/// Figure out which cells are possibilities for inserting the present ChOrph.
		/// Mark them to be highlighted. Return a boolean array corresponding to column possibilities.
		/// </summary>
		/// <param name="icolPrec"></param>
		/// <param name="rowPrec"></param>
		/// <param name="icolFoll"></param>
		/// <param name="rowFoll"></param>
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
				irowPrec = IndexOfRow(precCell.HvoRow);
				icolPrec = precCell.ColIndex;
			}
			int icolFoll = follCell.ColIndex;
			int irowFoll = IndexOfRow(follCell.HvoRow);
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
		protected internal bool[] PrepareForChOrphInsert(int iPara, int offset, out ICmIndirectAnnotation rowPrec)
		{
			// Prepare 'out' vars for GetWficCellsBorderingChOrph()
			ChartLocation precCell, follCell;

			GetWficCellsBorderingChOrph(iPara, offset, out precCell, out follCell);
			// Set Ribbon to limit selection to this ChOrph group
			SetRibbonLimits(follCell);
			// highlight eligible insert spots
			rowPrec = precCell.RowAnn; // Set 'out' variable
			return HighlightChOrphPossibles(precCell, follCell);
		}

		/// <summary>
		/// After processing a ChOrph, we need to reset the ribbon's selection limits.
		/// </summary>
		protected internal void ResetRibbonLimits()
		{
			m_ribbon.EndSelLimitIndex = -1;
			m_ribbon.SelLimAnn = 0;
		}

		/// <summary>
		/// Set Ribbon index and annotation for maximum allowed selection to prevent user selecting past
		/// an active ChOrph awaiting repair. Uses GetWficCellsBorderingChOrph() on successive Ribbon elements
		/// to determine how far the current ChOrph goes.
		/// </summary>
		/// <param name="follCell">ChartLocation limit for first Ribbon element (for comparison)</param>
		protected internal void SetRibbonLimits(ChartLocation follCell)
		{
			int[] nui = NextUnchartedInput(kMaxRibbonContext);
			int i = 1; // start at 1, we already know our first word is a ChOrph
			for (; i < nui.Length; i++)
			{
				int iPara, offset; // 'out' vars for IsChOrph() to be used later
				if (!IsChOrph(nui[i], out iPara, out offset))
					break; // No longer in any ChOrph group
				// Prepare more 'out' vars for GetWficCellsBorderingChOrph
				// This time we don't care about the Preceding cell, only the Following cell; has it changed?
				ChartLocation dummy, newFollCell;
				GetWficCellsBorderingChOrph(iPara, offset, out dummy, out newFollCell);
				if (!newFollCell.IsSameLocation(follCell))
					break; // No longer in same ChOrph group
			}
			i--;
			m_ribbon.EndSelLimitIndex = i;
			m_ribbon.SelLimAnn = nui[i];
			Cache.PropChanged(m_hvoStText, m_ribbon.AnnotationListId, 0, nui.Length, nui.Length);
			m_ribbon.SelectFirstAnnotation();
		}

		#endregion

		/// <summary>
		/// Return a ChartLocation containing the column index and Row Annotation of a Wfic,
		/// unless it is uncharted, in which case it returns null.
		/// </summary>
		/// <param name="hvoWfic"></param>
		/// <returns></returns>
		public ChartLocation FindChartLocOfWfic(int hvoWfic)
		{
			if (m_chart == null || Chart.RowsRS.Count < 1)
				return null;
			Debug.Assert(hvoWfic != 0);
			int[] arrayLoc = DiscourseDbOps.FindChartLocOfWfic(Cache, m_chart.Hvo, hvoWfic);
			if (arrayLoc.Length == 0)
				return null;
			return new ChartLocation(Math.Max(0, IndexOfColumnForCca(arrayLoc[1])),
				CmIndirectAnnotation.CreateFromDBObject(Cache, arrayLoc[0]));
		}

		/// <summary>
		/// Get the annotation (wfic) closest to the bookmark (uses an IText.RawTextPane method).
		/// </summary>
		/// <param name="bookmark"></param>
		/// <returns></returns>
		internal int FindAnnAtBookmark(InterAreaBookmark bookmark)
		{
			return RawTextPane.AnnotationHvo(Cache, new StText(Cache, StTextHvo), bookmark, true);
		}

		/// <summary>
		/// Gets all the 'leaf' nodes in a chart template, and also the ends of column groupings.
		/// </summary>
		/// <param name="template"></param>
		/// <returns>List of int (hvos?)</returns>
		public List<int> AllColumns(ICmPossibility template)
		{
			List<int> result = new List<int>();
			Set<int> groups = new Set<int>();
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
		private void CollectColumns(List<int> result, ICmPossibility template, Set<int> groups, int depth)
		{
			if (template.SubPossibilitiesOS.Count == 0)
			{
				// Note: do NOT do add to the list if it has children...we ONLY want leaves in the result.
				result.Add(template.Hvo);
				// We now collect this column index in our GroupEndsIndices even if it's a group of one.
				if (depth == 1)
					groups.Add(result.Count - 1);
				return;
			}
			foreach (ICmPossibility child in template.SubPossibilitiesOS)
				CollectColumns(result, child, groups, depth + 1);

			// Collect this column index in our GroupEndsIndices if we're at the top-level.
			if (depth == 1)
				groups.Add(result.Count - 1);
		}

		/// <summary>
		/// Given the hvo of a CmAnnotation, retrieve its CompDetails. If it is empty, return false.
		/// Otherise, expect an XML element, a 'ccinfo'. If it has the specified attribute with value
		/// 'true', return true, otherwise false.
		/// </summary>
		/// <param name="hvoAnn"></param>
		/// <returns></returns>
		public static bool GetFeature(ISilDataAccess sda, int hvoAnn, string name)
		{
			string xml = sda.get_UnicodeProp(hvoAnn, kflidCompDetails);
			if (String.IsNullOrEmpty(xml))
				return false;
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			XmlNode root = doc.DocumentElement;
			XmlAttribute attr = root.Attributes[name];
			return (attr != null && attr.Value == "true");
		}

		public static void SetFeature(ISilDataAccess sda, int hvoAnn, string name, bool value)
		{
			string xml = sda.get_UnicodeProp(hvoAnn, kflidCompDetails);
			string output;
			if (String.IsNullOrEmpty(xml))
			{
				if (!value)
					return;
				output = "<ccinfo " + name + "=\"true\"/>";
			}
			else
			{
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xml);
				XmlNode root = doc.DocumentElement;
				XmlAttribute attr = root.Attributes[name];
				if (attr == null)
				{
					if (!value)
						return;
					attr = doc.CreateAttribute(name);
					root.Attributes.Append(attr);
				}
				attr.Value = value ? "true" : "false";
				output = root.OuterXml;
			}
			sda.set_UnicodeProp(hvoAnn, kflidCompDetails, output);
		}

		#region actions for buttons

		/// <summary>
		/// V1: move the selected annotations from the interlinear ribbon to the indicated column of the chart.
		/// Select the first remaining item in the ribbon.
		/// Moving to the indicated column includes making an extra row if there is anything already in a later
		/// column of the current row.
		/// It gets more complicated in the presence of 'marker' CCAs, which indicate things like missing elements
		/// and moved text.
		/// There are three possible ways to insert the new words: append to an existing CCA, make a new one in the
		/// same row, or make a new one at the start of a new row. This routine does one or the other; it calls
		/// FindWhereToAddWords to decide which.
		/// V2: V1 iff ribbon is active. If main chart is active, move what is selected there. In either case,
		/// follow up by selecting first thing in ribbon and making it active. Moving within the main chart is
		/// possible only if it won't put things out of order; ensure this.
		/// </summary>
		/// <param name="icol"></param>
		/// <returns>Null if successful, otherwise, an error message.</returns>
		public string MoveToColumn(int icol)
		{
			int[] selectedAnnotations = m_ribbon.SelectedAnnotations;
			if (selectedAnnotations == null || selectedAnnotations.Length == 0)
				return DiscourseStrings.ksNoAnnotationsMsg;
			using (new UndoRedoTaskHelper(m_cache, DiscourseStrings.ksUndoMoveAnnotation, DiscourseStrings.ksRedoMoveAnnotation))
			{
				m_cache.ActionHandlerAccessor.AddAction(new UpdateRibbonAction(this, false));
				int iPara, offset; // 'out' vars for IsChOrph()
				ICmIndirectAnnotation rowFinal = LastRow;
				if (IsChOrph(selectedAnnotations[0], out iPara, out offset))
				{
					// Define 'out' vars for GetWficCellsBorderingChOrph()
					ChartLocation precCell, follCell;
					GetWficCellsBorderingChOrph(iPara, offset, out precCell, out follCell);
					// Make sure GetValidRowColumnForChOrph() knows that precCell could be null
					rowFinal = GetValidRowColumnForChOrph(precCell, follCell, icol);
					if (rowFinal != null)
					{
						ChartLocation finalCell = new ChartLocation(icol, rowFinal);
						MoveChOrphToColumn(iPara, offset, selectedAnnotations, finalCell);
					}
					else return DiscourseStrings.ksChooseDifferentColumn;
				}
				else
				{
					int position; // where to insert if we need a new CCA.
					ICmIndirectAnnotation ccaToAppendTo;
					FindWhereToAddResult addWhere = FindWhereToAddWords(icol, out position, out ccaToAppendTo);
					switch (addWhere)
					{
						case FindWhereToAddResult.kAppendToExisting:
							foreach (int item in selectedAnnotations)
								ccaToAppendTo.AppliesToRS.Append(item);
							break;
						case FindWhereToAddResult.kInsertCcaInSameRow:
						case FindWhereToAddResult.kMakeNewRow:
							if (addWhere == FindWhereToAddResult.kMakeNewRow) // distinguishes from kInsertCcaInSameRow
								rowFinal = MakeNewRow();
							ChartLocation finalCell = new ChartLocation(icol, rowFinal);
							MakeCca(finalCell, selectedAnnotations, position, null);
							break;
					}
				}

				// Remove annotations from input and select next item.
				NoteActionChangesRibbonItems(); // Fires Ribbon_Changed event
				// LT-7620 row numbers in ConstChart also says to ignore baseline paragraph breaks. [GordonM]
				//int[] nextAnnotations = m_ribbon.SelectedAnnotations;
				//if (nextAnnotations.Length > 0 &&
				//    GetPara(nextAnnotations[0]) != GetPara(selectedAnnotations[0]))
				//{
				//    SetFeature(m_cache.MainCacheAccessor, LastRow.Hvo, EndParaFeatureName, true);
				//}
				if (RowModifiedEvent != null)
				{
					RowModifiedEvent(this, new RowModifiedEventArgs(rowFinal));
				}
				return null;
			}
		}

		/// <summary>
		/// Returns the proper row to insert a ChOrph in the specified column (icol). If the desired column
		/// is not a valid choice given the current chart configuration, returns null.
		/// </summary>
		/// <param name="precCell"></param>
		/// <param name="follCell"></param>
		/// <param name="icol">Desired column index to insert ChOrph into (user column choice).</param>
		/// <returns></returns>
		private ICmIndirectAnnotation GetValidRowColumnForChOrph(ChartLocation precCell, ChartLocation follCell, int icol)
		{
			Debug.Assert(m_chart != null && m_chart.RowsRS.Count > 0);

			// There may not BE a valid preceding cell. See below for reason.
			if (precCell == null || !precCell.IsValidLocation)
			{
				// Handle case where there is no charted Wfic before the ChOrph.
				// Enhance GordonM: For the case that returns null below,
				//    insert a blank row at the top and return it.
				ICmIndirectAnnotation firstRow = m_chart.RowsRS[0];
				if (follCell.HvoRow == firstRow.Hvo && icol > follCell.ColIndex)
					return null; // Currently there's no way to insert a top row.
				return firstRow;
			}
			if (precCell.HvoRow == follCell.HvoRow)
			{
				if (icol < precCell.ColIndex || icol > follCell.ColIndex)
					return null; // Not an acceptable column choice as things stand presently.
				return precCell.RowAnn;
			}
			else
			{
				if (icol < precCell.ColIndex)
				{
					ICmIndirectAnnotation nextRow = NextRow(precCell.RowAnn);
					if (nextRow.Hvo == follCell.HvoRow && icol > follCell.ColIndex)
						return null;
					return nextRow; // so if there's a blank row between Prec/Foll, it goes there.
				}
				return precCell.RowAnn;
			}
		}

		/// <summary>
		/// Insert the ChOrph into the column chosen by the user (validity already checked by other routines)
		/// </summary>
		/// <param name="iPara">ChOrph paragraph index</param>
		/// <param name="offset">ChOrph offset into Text para</param>
		/// <param name="selectedAnnotations">Wfics to insert</param>
		/// <param name="icol">user-chosen column</param>
		/// <param name="rowFinal">chart row in which to insert ChOrph</param>
		private void MoveChOrphToColumn(int iPara, int offset, int[] selectedAnnotations, ChartLocation finalCell)
		{
			// Possibilities:
			// 1. There exists a Wfic CCA in the right chart cell(row/column combo)
			//    Search for the correct index in CCA AppliesTo to insert our selected CBA(Wfic)s
			// 2. Create a new CCA at this location with our selectedAnnotations
			int whereToInsert;
			ICmIndirectAnnotation existingCca;
			FindWhereToAddResult result = FindWhereToAddChOrph(finalCell,
				iPara, offset, out whereToInsert, out existingCca);
			switch (result)
			{
				case FindWhereToAddResult.kInsertCcaInSameRow:
					// whereToInsert gives index in CCR appliesTo
					MakeCca(finalCell, selectedAnnotations, whereToInsert, null);
					break;
				case FindWhereToAddResult.kInsertChOrphInCCA:
					// whereToInsert gives index in existingCca appliesTo
					foreach (int item in selectedAnnotations)
						existingCca.AppliesToRS.InsertAt(item, whereToInsert++);
					break;
				case FindWhereToAddResult.kAppendToExisting:
					foreach (int item in selectedAnnotations)
						existingCca.AppliesToRS.Append(item);
					break;
			}
		}

		private void NoteActionChangesRibbonItems()
		{
			UpdateRibbonAction undoAction = new UpdateRibbonAction(this, true);
			m_cache.ActionHandlerAccessor.AddAction(undoAction);
			undoAction.UpdateUnchartedAnnotations();
		}

		private int GetPara(int hvoAnn)
		{
			return m_cache.GetObjProperty(hvoAnn, kflidBeginObject);
		}

		/// <summary>
		/// Force a new clause.
		/// </summary>
		/// <param name="ihvo"></param>
		/// <returns></returns>
		public string MoveToHereInNewClause(int ihvo)
		{
			using (new UndoRedoTaskHelper(m_cache, DiscourseStrings.ksUndoMoveAnnotation, DiscourseStrings.ksRedoMoveAnnotation))
			{
				MakeNewRow();
				return MoveToColumn(ihvo);
			}
		}

		/// <summary>
		/// Indicates how to interpret the other results of FindWhereToAddChOrph and FindWhereToAddWords,
		/// namely, 'whereToInsert' and 'existingCca'. A (different) set of 3 of these results apply to each of
		/// the 2 'FindWhereToAddX' methods. 'FWTAWords' uses the first 3. 'FWTAChOrph' uses all but kMakeNewRow.
		/// </summary>
		public enum FindWhereToAddResult
		{
			kAppendToExisting,		// append (word or ChOrph) as last wfics of 'existingCca' (ignore 'whereToInsert')
			kInsertCcaInSameRow,	// 'whereToInsert' specifies the index in the row's AppliesTo of the new CCA to be
									// created (from Words or ChOrphs) in the (at this time, anyway) previously empty cell
									// (ignore 'existingCca')
			kMakeNewRow,			// Make a new CCA in a new row. (ignore both 'whereToInsert and 'existingCca')
									// (Not used for ChOrphs)
			kInsertChOrphInCCA		// Insert ChOrph Wfic into 'existingCca'; whereToInsert is now index in CCA's appliesTo
		}

		// Here's another couple of ways of describing the algorithm:
		// if (last non-marker is in a later column)
		//	(1)make a new row
		// else if (last non-marker is in the same column)
		//	(2)append to last non-marker
		// else if (there's a marker in the same colum)
		//	(3)make a new row
		// else
		//	(4) make a new cca in the same row (before any markers in later columns)

		// 1. Find the decisive cca: last non-marker or marker-in-the-same-column.
		// 2. Earlier column: append after decisive cca.
		// 3. Later column, or marker-in-the-same-column: make a new row, append at start.
		// 4. Same column: append to existing cca.
		/// <summary>
		/// Find where to append the new annotations. This can involve finding (and returning) a cca
		/// that is already the right type in the right row and column that we can just add to, or making a
		/// new CCA (possibly in a new row). Here is a summary of the options:
		/// 1. If the last non-marker CCA in the last row is in the right column, append the words to its AppliesTo.
		/// 2. If there's an empty spot in the last row at the specified column, and no non-marker CCAs
		///		in later columns, insert the words into a new cca inserted into the row at the appropriate place.
		/// 3. Otherwise, insert the new words into a new cca in a new row.
		/// Only used for appending words to the end of the chart.
		/// </summary>
		/// <param name="icol">desired column index</param>
		/// <param name="whereToInsertCca">index in Row's appliesTo, if new CCA</param>
		/// <param name="existingCcaToAppendTo"></param>
		/// <returns>enum result used in switch</returns>
		public FindWhereToAddResult FindWhereToAddWords(int icol,
			out int whereToInsert, out ICmIndirectAnnotation existingCca)
		{
			existingCca = null;
			whereToInsert = 0;
			ICmIndirectAnnotation lastRow = LastRow;
			if (lastRow == null)
				return FindWhereToAddResult.kMakeNewRow;
			if (lastRow.AppliesToRS.Count == 0)
			{
				// Probably the only way this happens is when inserting a moved text marker.
				// If the marker collides, we add a row before calling MoveToColumn
				return FindWhereToAddResult.kInsertCcaInSameRow;
			}
			List<int> allCols = new List<int>(AllMyColumns); // so we can use indexOf
			whereToInsert = lastRow.AppliesToRS.Count;
			int icca = lastRow.AppliesToRS.Count - 1;
			ICmAnnotation cca1 = lastRow.AppliesToRS[icca];
			ICmIndirectAnnotation cca = cca1 as CmIndirectAnnotation;
			// loop over markers (if any) at end of list till we find something that determines the outcome.
			// also skip CmBaseAnnotations (list refs)
			while (cca == null || cca.Comment.AnalysisDefaultWritingSystem.Length > 0
				|| cca.Comment.GetAlternative(WsLineNumber).Length > 0)
			{
				if (cca1.InstanceOfRAHvo == AllMyColumns[icol])
				{
					// marker in the same column...we'll have to start a new row. (case 3)
					whereToInsert = 0;
					return FindWhereToAddResult.kMakeNewRow;
				}
				if (allCols.IndexOf(cca1.InstanceOfRAHvo) < icol)
				{
					// It's a marker BEFORE the column we want (case 4). If we get here there's nothing
					// in the row except possibly other markers after our target column. Since we didn't take
					// the exit above, there can't be anything in the target column, so can insert in same row.
					whereToInsert = icca + 1; // insert into current row AFTER cca
					return FindWhereToAddResult.kInsertCcaInSameRow;
				}
				if (icca == 0)
				{
					// No non-markers, perhaps the first thing the user did was insert a missing item marker?
					whereToInsert = 0; // insert BEFORE the current marker, since it is in a later column.
					return FindWhereToAddResult.kInsertCcaInSameRow;
				}
				// Current cca is a marker in a column after the one we want: look earlier to see whether
				// there is a conflict or something we can append to.
				icca--;
				cca1 = lastRow.AppliesToRS[icca];
				cca = cca1 as CmIndirectAnnotation;
			}

			// Got a cca that is NOT a marker. If it's in the RIGHT colum, we just return it
			// (and whereToInsrtCca doesn't matter, because we won't make a new one). Case 2.
			if (cca.InstanceOfRAHvo == AllMyColumns[icol])
			{
				existingCca = cca;
				// whereToInsert doesn't matter
				return FindWhereToAddResult.kAppendToExisting;
			}

			// If the last non-marker is in a LATER column, we have to make a new row (case 1)
			if (allCols.IndexOf(cca.InstanceOfRAHvo) > icol)
			{
				whereToInsert = 0; // insert at start of new row.
				return FindWhereToAddResult.kMakeNewRow;
			}

			// cca is a non-marker in an EARLIER column; make a new cca AFTER it (case 4)
			whereToInsert = icca + 1;
			return FindWhereToAddResult.kInsertCcaInSameRow;
		}

		/// <summary>
		/// Find where to insert the ChOrph annotations. This can involve finding (and returning) a cca
		/// that is already the right type in the right row and column that we can just add to, or making a
		/// new CCA. Here is a summary of the options:
		/// 1. If there is a Wfic CCA at the current ChartLocation, return it and
		///    figure out where to insert the words in its AppliesTo (possibly append to end too).
		/// 2. If there is NOT a Wfic CCA in the current row at the specified column,
		///		insert the words into a new cca inserted into the row at the appropriate place.
		/// Only used for inserting ChOrphs into their 'rightful place' in the chart.
		/// </summary>
		/// <param name="curCell">desired ChOrph cell</param>
		/// <param name="iPara">ChOrph paragraph index</param>
		/// <param name="offset">ChOrph offset</param>
		/// <param name="whereToInsert">If we are creating a new CCA, this is the index in the row's appliesTo.
		/// If we are inserting into an existing CCA, this is the index in the CCA's appliesTo.</param>
		/// <param name="existingCca"></param>
		/// <returns></returns>
		public FindWhereToAddResult FindWhereToAddChOrph(ChartLocation curCell, int iChOrphPara, int beginChOrphOffset,
			out int whereToInsert, out ICmIndirectAnnotation existingCca)
		{
			// Enhance GordonM: This is an awfully long method. I need to break it down somehow.
			existingCca = null;
			whereToInsert = 0;
			int crowApplies = curCell.RowAnn.AppliesToRS.Count;
			if (crowApplies == 0)
			{
				return FindWhereToAddResult.kInsertCcaInSameRow; // whereToInsert == 0
			}
			List<ICmAnnotation> ccaCollection = CcasInCell(curCell);
			existingCca = FindFirstCcaWithWfics(ccaCollection);
			if (existingCca == null)
			{
				// Will need to insert a CCA. Figure out index into Row.AppliesTo.
				for (whereToInsert = 0; whereToInsert < crowApplies; whereToInsert++)
				{
					ICmAnnotation currCca1 = curCell.RowAnn.AppliesToRS[whereToInsert] as ICmAnnotation;
					ICmIndirectAnnotation currCca = currCca1 as ICmIndirectAnnotation;
					int icurrCol;
					if (currCca == null) // This is a marker
						icurrCol = IndexOfColumnForCca(currCca1.Hvo);
					else
						icurrCol = IndexOfColumnForCca(currCca.Hvo);
					if (icurrCol < curCell.ColIndex)
						continue;
					if (icurrCol > curCell.ColIndex)
						return FindWhereToAddResult.kInsertCcaInSameRow; // whereToInsert == 'the right spot' in seq.

					// currCca is in the correct column
					if (currCca == null) // This is a marker and should come after any Wfics in this cell
						return FindWhereToAddResult.kInsertCcaInSameRow;
					// if appliesTo is empty, replace Zero ref mrkr [I don't think we do it this way now.]
					if (currCca.AppliesToRS == null)
					{
						// This is a missing text marker, reuse this CCA
						Cache.SetMultiStringAlt(currCca.Hvo, kflidComment,
							Cache.DefaultAnalWs, null); // clear out comment '---' of CCA before re-using
						whereToInsert = 0;
						return FindWhereToAddResult.kInsertChOrphInCCA;
					}
					if (currCca.Comment.UserDefaultWritingSystem.Text == DiscourseStrings.ksMovedTextBefore)
						continue; // So far, we assume only preposed markers go before Wfics in a cell
					return FindWhereToAddResult.kInsertCcaInSameRow;
				}
				return FindWhereToAddResult.kInsertCcaInSameRow; // whereToInsert == crowApplies; append to end of seq.
			}

			// We've found at least one data CCA ("wfic-bearing")

			int icurrCca = ccaCollection.IndexOf(existingCca);
			int ifoundAt; // 'out' variable for FindNextCcaWithWfics() below
			ICmIndirectAnnotation nextCca = existingCca;
			while (true) // Loop through this and any other data CCAs in this cell until we find our spot
			{
				// Loop through all CBAs in CCA
				for (whereToInsert = 0; whereToInsert < nextCca.AppliesToRS.Count; whereToInsert++)
				{
					ICmBaseAnnotation currWfic = nextCca.AppliesToRS[whereToInsert] as ICmBaseAnnotation;
					// int iParaCurr = RawTextPane.GetParagraphIndexForAnnotation(Cache, existingCca.AppliesToRS[0].Hvo);
					int iParaCurr = GetParagraphIndexForWfic(currWfic.Hvo);
					if (iParaCurr > iChOrphPara) // We've gotten past our paragraph
					{
						if (nextCca.Hvo != existingCca.Hvo)
							return FindWhereToAddResult.kAppendToExisting; // Append to previous CCA in this cell
						return FindWhereToAddResult.kInsertChOrphInCCA; // In case of wfics from later paras in same CCA
					}
					// If the ChOrph belongs after the Wfic we are testing, keep going. (The iPara test is needed to
					// cover the pathological possibility that Wfics in the same CCA are in different paragraphs.)
					if ( iParaCurr < iChOrphPara || GetBeginOffset(currWfic.Hvo) < beginChOrphOffset)
					{
						existingCca = nextCca;
						continue;
					}
					// 'currWfic' is the one to insert before. If there's a choice append to previous CCA in cell
					// rather than inserting at start of current one. This is somewhat arbitrary and could be changed.
					// If there is more than one CCA, probably one is pre/postposed, but we don't know which one the
					// ChOrph belongs in.
					if (existingCca.Hvo != nextCca.Hvo)
						return FindWhereToAddResult.kAppendToExisting; // Append to previous CCA
					else
						return FindWhereToAddResult.kInsertChOrphInCCA;
				}
				// If we fall through here, we either need to append to the CCA we've been looking at
				// or there are multiple data CCAs in this cell and we need to look further.
				existingCca = nextCca;
				nextCca = FindNextCcaWithWfics(ccaCollection, icurrCca, out ifoundAt);
				if (nextCca == null)
					break;
				icurrCca = ifoundAt;
			}
			return FindWhereToAddResult.kAppendToExisting;
		}

		internal const int kMaxRibbonContext = 20;

		/// <summary>
		/// Insert another row below the argument row.
		/// Takes over any compDetails of the previous row.
		/// Line label is calculated based on the previous row's label.
		/// </summary>
		/// <param name="previousRow"></param>
		public void InsertRow(ICmIndirectAnnotation previousRow)
		{
			using (IUndoRedoTaskHelper helper = GetUndoHelper(DiscourseStrings.ksUndoInsertRow, DiscourseStrings.ksRedoInsertRow))
			{
				int index = IndexOfRow(previousRow.Hvo);
				// Inserting a new row will quite possibly remove special borders from the previous row.
				// But it shouldn't affect dependent/song/speech clause features! [LT-9587]
				// Pretend it got deleted and re-inserted, both now and on Undo/Redo.
				using (new ExtraPropChangedInserter(m_cache.MainCacheAccessor, m_chart.Hvo, kflidRows, index, 1, 1))
				{
					ICmIndirectAnnotation newRow = m_cache.LangProject.AnnotationsOC.Add(new CmIndirectAnnotation()) as ICmIndirectAnnotation;
					m_chart.RowsRS.InsertAt(newRow, index + 1);
					newRow.AnnotationTypeRA = CmAnnotationDefn.ConstituentChartRow(m_cache);
					SetupCompDetailsForInsertRow(previousRow, newRow);
					// It's easiest to just renumber starting with the row above the inserted one.
					// foneSentOnly = true, because we are only adding a letter to the current sentence.
					RenumberRows(index, true);
				}
			}
		}

		/// <summary>
		/// Setups the CompDetails features for InsertRow().
		/// Inserting a new row will quite possibly remove special borders from the previous row.
		/// But it shouldn't affect dependent/song/speech clause features! [LT-9587]
		/// </summary>
		/// <param name="previousRow"></param>
		/// <param name="newRow"></param>
		private void SetupCompDetailsForInsertRow(ICmIndirectAnnotation previousRow, ICmIndirectAnnotation newRow)
		{
			ISilDataAccess sda = Cache.MainCacheAccessor;
			bool fEndSent = GetFeature(sda, previousRow.Hvo, EndSentFeatureName);
			if (!fEndSent)
			{
				newRow.CompDetails = "";
				return;
			}
			// delete prevRow Sentence feature, add newRow Sentence feature
			SetFeature(sda, previousRow.Hvo, EndSentFeatureName, false);
			SetFeature(sda, newRow.Hvo, EndSentFeatureName, true);
			bool fEndPara = GetFeature(Cache.MainCacheAccessor, previousRow.Hvo, EndParaFeatureName);
			if (fEndPara)
			{
				// delete prevRow para feature, add newRow para feature
				SetFeature(sda, previousRow.Hvo, EndParaFeatureName, false);
				SetFeature(sda, newRow.Hvo, EndParaFeatureName, true);
			}
		}

		public const string DepClauseFeatureName = "dependent";
		public const string SpeechClauseFeatureName = "speech";
		public const string SongClauseFeatureName = "song";
		public const string StartDepClauseGroup = "firstDep";
		public const string EndDepClauseGroup = "endDep";
		public const string MovedTextFeatureName = "movedText"; // this is a CCA feature, the others are ROW features.

		/// <summary>
		/// Clears the chart from the given cell to the end of the chart.
		/// </summary>
		/// <param name="icol"></param>
		/// <param name="row"></param>
		public void ClearChartFromHereOn(ChartLocation cell)
		{
			using (GetUndoHelper(DiscourseStrings.ksUndoClearChart, DiscourseStrings.ksRedoClearChart))
			{
				int rowIndex = IndexOfRow(cell.HvoRow);
				ICmIndirectAnnotation row = cell.RowAnn;

				int ccaIndex;

				// If column index is zero we're deleting from the very start of the row.
				// This is done not mainly to avoid the call to FindIndexOfFirstCcaInOrAfterColumn,
				// but for robustness: it allows us to delete successfully even if we somehow have
				// a broken CCA that isn't marked as being in any column.
				if (cell.ColIndex == 0)
					ccaIndex = 0;
				else
					ccaIndex = FindIndexOfFirstCcaInOrAfterColumn(cell);
				Set<int> hvosToDelete = new Set<int>();
				// all the things (not deleted) that some CCA refers to.
				// (We only really care about the rows.)
				Set<int> ccaTargets = new Set<int>();
				int ihvoFirstRowToDelete = rowIndex + 1;

				// Delete the redundant stuff in the current row.
				for (int i = ccaIndex; i < row.AppliesToRS.Count; i++)
					hvosToDelete.Add(row.AppliesToRS.HvoArray[i]);
				int[] newVal = new int[0];
				Cache.ReplaceReferenceProperty(cell.HvoRow, kflidAppliesTo,
					ccaIndex, row.AppliesToRS.Count, ref newVal);

				// Delete the redundant rows.
				for (int i = ihvoFirstRowToDelete; i < m_chart.RowsRS.Count; i++)
				{
					hvosToDelete.AddRange(m_chart.RowsRS[i].AppliesToRS.HvoArray);
					hvosToDelete.Add(m_chart.RowsRS.HvoArray[i]);
				}
				Cache.ReplaceReferenceProperty(m_chart.Hvo, kflidRows, ihvoFirstRowToDelete,
					m_chart.RowsRS.Count, ref newVal);
				List<int> delFromRow = new List<int>();

				// The code above changed the rows of the chart and the ccas of the last surviving row, so this loops
				// only over surviving CCAs. Fix any that point at deleted objects. Also build the ccaTargets set.
				foreach (ICmIndirectAnnotation eachRow in m_chart.RowsRS)
				{
					delFromRow.Clear();
					foreach (ICmAnnotation cca1 in eachRow.AppliesToRS)
					{
						ICmIndirectAnnotation cca = cca1 as ICmIndirectAnnotation;
						if (cca == null)
							continue;
						foreach (int target in cca.AppliesToRS.HvoArray)
						{
							if (hvosToDelete.Contains(target))
								cca.AppliesToRS.Remove(target);
							else
								ccaTargets.Add(target);
						}
						if (cca.AppliesToRS.Count == 0)
						{
							hvosToDelete.Add(cca.Hvo);
							delFromRow.Add(cca.Hvo);
						}
						// Review JohnT: if cca survives but something was deleted,
						// and it is a clause reference, we may need to update its comment.
						// However, current plan is to make it automatic to display the
						// row labels of the actual rows it points to.
					}
					foreach (int hvo in delFromRow)
						eachRow.AppliesToRS.Remove(hvo);
				}
				// Deal with dependent clause stuff that refers back from deleted dep clause markers
				// to surviving clauses.
				foreach (ICmIndirectAnnotation eachRow in m_chart.RowsRS)
				{
					if (!ccaTargets.Contains(eachRow.Hvo))
					{
						// Not the target of any (dependent clause) annotation.
						// Make sure it doesn't have the property that makes it display as dependent.
						RemoveAllDepClauseMarkers(eachRow);
					}
				}
				// Delete the current row if it is now empty.
				// Note: I do NOT recommend trying to do this by adjusting ihvoFirstRowToDelete, as I once planned
				// to do. This will not catch a row that initially has something left, but it is a PostPosed
				// text marker that subsequently gets deleted.
				DeleteRowIfEmpty(row, hvosToDelete);
				FindMarkersClearTargets(hvosToDelete);
				CmObject.DeleteObjects(hvosToDelete, Cache, false);
				NoteActionChangesRibbonItems();
			}
		}

		/// <summary>
		/// Clears movedText feature from targets of any "to-be-deleted" CCAs. Also issue PropChangeds
		/// for the target rows. Unless the target is also on its way out.
		/// </summary>
		/// <param name="hvosToDelete"></param>
		private void FindMarkersClearTargets(Set<int> hvosToDelete)
		{
			int hvoCcaDefn = CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo;
			foreach (int hvo in hvosToDelete)
			{
				int hvoTarget;
				if (AnnTypeOfFirstAppliesTo(Cache, hvo, CmIndirectAnnotation.kclsidCmIndirectAnnotation,
					out hvoTarget) != hvoCcaDefn)
					continue;
				if (hvosToDelete.Contains(hvoTarget))
					continue; // Don't need to worry about this target as it's getting deleted too.
				// make sure this is a CCA and not a CCR (row)
				if (Cache.GetObjProperty(hvo, kflidAnnotationType) == hvoCcaDefn)
				{
					// Now we know hvo is a CCA that AppliesTo a CCA, make sure the target is not set as movedText
					SetFeature(Cache.MainCacheAccessor, hvoTarget, MovedTextFeatureName, false);
					ICmIndirectAnnotation ccr = GetMyRow(hvoTarget);
					if (ccr != null)
					{
						m_cache.PropChanged(m_chart.Hvo, kflidRows, IndexOfRow(ccr.Hvo), 1, 1);
					}
				}
			}
		}

		/// <summary>
		/// Get the row of a CCA.
		/// </summary>
		/// <param name="hvoCcaTarget"></param>
		/// <returns></returns>
		private ICmIndirectAnnotation GetMyRow(int hvoCcaTarget)
		{
			if (hvoCcaTarget != 0)
			{
				foreach (ICmIndirectAnnotation ccr in m_chart.RowsRS)
				{
					if (ccr.AppliesToRS.Contains(hvoCcaTarget))
					{
						return ccr;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// This creates a new row to append at the end of the chart's list of rows.
		/// </summary>
		/// <returns></returns>
		protected ICmIndirectAnnotation MakeNewRow()
		{
			using (GetUndoHelper(DiscourseStrings.ksUndoMakeNewRow, DiscourseStrings.ksRedoMakeNewRow))
			{
				string rowLabel = CreateNewRowLabel(); // this needs to be before the next 2 lines

				ICmIndirectAnnotation newRow = m_cache.LangProject.AnnotationsOC.Add(new CmIndirectAnnotation()) as ICmIndirectAnnotation;
				m_chart.RowsRS.Append(newRow);

				newRow.Comment.SetAlternative(rowLabel, WsLineNumber);
				newRow.AnnotationTypeRA = CmAnnotationDefn.ConstituentChartRow(m_cache);
				// Inserting a new row will quite possibly remove the end-of-document thick underline from
				// the previous line, so we need to force it to be redisplayed.
				// This is good, because we might also have changed the row number if the new row is
				// another clause of the previous line (e.g. 1 -> 1a).
				int chvoRow = m_chart.RowsRS.Count;
				if (chvoRow > 1)
					m_cache.PropChanged(m_chart.Hvo, kflidRows, chvoRow - 2, 1, 1);
				return newRow;
			}
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
				prevWasEOS = GetFeature(Cache.MainCacheAccessor, LastRow.Hvo, EndSentFeatureName);
			else
			{
				rowLabel = "1";
				return rowLabel;
			}
			DecipherRowLabel(prevRowLabel, out rowNumber, out clauseNumber);
			if (clauseNumber == 1 && !prevWasEOS)
				// If the previous one had no clause number, we should add an 'a' to it.
				// But only if the previous one wasn't the End of a Sentence.
				AddLetterToNumberOnlyLabel(IndexOfRow(LastRow.Hvo), rowNumber);
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
			m_chart.RowsRS[rowIndex].Comment.SetAlternative(Convert.ToString(rowNumber) + 'a', WsLineNumber);
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
				return m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			}
		}

		/// <summary>
		/// Pulls the row number and clause letter(a=1) from a string
		/// </summary>
		/// <param name="rowLabel1"></param>
		/// <param name="row"></param>
		/// <param name="clause"></param>
		private void DecipherRowLabel(string rowLabel1, out int row, out int clause)
		{
			string rowLabel = rowLabel1 == null ? "" : rowLabel1;
			int posFirstLetter = 0;

			for (int i = 1; i < rowLabel.Length; i++) // i=1 because never start with a letter
			{
				if (rowLabel[i] >= 'a' && rowLabel[i] <= 'z')
				{
					posFirstLetter = i;
					break;
				}
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
		private void RenumberRows(int irow, bool foneSentOnly)
		{
			bool fIsThisRowEOS;
			bool fIsPrevRowEOS = true;
			int csentence = 0;
			int cclause = 1;
			string rowLabel;
			if (irow > 0)
			{
				fIsPrevRowEOS = GetFeature(Cache.MainCacheAccessor,
					m_chart.RowsRS[irow - 1].Hvo, EndSentFeatureName);
				// Get label from previous row
				DecipherRowLabel(m_chart.RowsRS[irow - 1].Comment.GetAlternative(WsLineNumber).Text,
					out csentence, out cclause);
			}

			// Iterate through all rows starting with the modified one
			// and calculate row numbering to install in the Row label.
			bool foneSentFinished = false; // LT-8488 On foneSentOnly we need to do 2 sentences for odd case.
			while (irow < m_chart.RowsRS.Count)
			{
				// If this is the last row, assume EOS for numbering purposes,
				// otherwise look it up.
				if (m_chart.RowsRS.Count == irow + 1)
					fIsThisRowEOS = true;
				else
				{
					fIsThisRowEOS = GetFeature(Cache.MainCacheAccessor,
						m_chart.RowsRS[irow].Hvo, EndSentFeatureName);
				}
				// Calculate and deposit label (if changed)
				rowLabel = CalculateMyRowNums(ref csentence, ref cclause, fIsPrevRowEOS, fIsThisRowEOS);
				if (m_chart.RowsRS[irow].Comment.GetAlternative(WsLineNumber).Text != rowLabel)
				{
					m_chart.RowsRS[irow].Comment.SetAlternative(rowLabel, WsLineNumber);
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
		private string CalculateMyRowNums(ref int prevSentNum, ref int prevClauseNum,
			bool fSentBrkBefore, bool fSentBrkAfter)
		{
			string MyRowLabel;
			if (fSentBrkBefore)
			{
				prevSentNum++;
				prevClauseNum = 1;
			}
			else prevClauseNum++;

			// Make the string
			if (prevClauseNum > 26)
			{
				MyRowLabel = Convert.ToString(prevSentNum) + "a" + Convert.ToChar(Convert.ToInt32('a')
					+ prevClauseNum - 27);
			}
			else
			{
				MyRowLabel = Convert.ToString(prevSentNum) + Convert.ToChar(Convert.ToInt32('a')
					+ prevClauseNum - 1);
			}
			if (fSentBrkAfter && fSentBrkBefore)
			{
				// Strip 'a' off of string.
				MyRowLabel = MyRowLabel.Substring(0, MyRowLabel.Length - 1);
			}
			return MyRowLabel;
		}

		/// <summary>
		/// V1: move the selected annotations from the interlinear ribbon to the indicated column of the chart,
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
			int[] selectedAnnotations = m_ribbon.SelectedAnnotations;
			if (selectedAnnotations == null || selectedAnnotations.Length == 0)
				return DiscourseStrings.ksNoAnnotationsMsg;
			using (new UndoRedoTaskHelper(Cache, DiscourseStrings.ksUndoMakeMoved, DiscourseStrings.ksRedoMakeMoved))
			{
				MoveToColumn(icolActual);
				MakeMovedFrom(icolActual, icolMovedFrom);
				return null;
			}
		}

		internal ICmIndirectAnnotation LastRow
		{
			get
			{
				if (m_chart != null && m_chart.RowsRS.Count > 0)
					return m_chart.RowsRS[m_chart.RowsRS.Count - 1];
				return null;
			}
		}

		string LastRowNumber
		{
			get
			{
				if (m_chart != null && m_chart.RowsRS.Count > 0)
				{
					string result = LastRow.Comment.GetAlternative(WsLineNumber).Text;
					if (result != null)
						return result;
				}
				return "";
			}
		}

		/// <summary>
		/// Answer true if actualCell contains a cca and icolMovedFrom designates a column that contains
		/// a moved-text marker pointing at it (same row).
		/// </summary>
		/// <param name="actualCell"></param>
		/// <param name="icolMovedFrom"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		protected bool IsMarkedAsMovedFrom(ChartLocation actualCell, int icolMovedFrom)
		{
			foreach (ICmAnnotation cca in CcasInCell(new ChartLocation(icolMovedFrom, actualCell.RowAnn)))
				if (IsMarkerOfMovedFrom(cca, actualCell))
					return true;
			return false;
		}

		/// <summary>
		/// Answer true if cca is a CmIndirectAnnotation pointing to a CCA in the specified cell (same row).
		/// </summary>
		/// <param name="cca"></param>
		/// <param name="icolActual"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		internal bool IsMarkerOfMovedFrom(ICmAnnotation cca1, ChartLocation cellInSameRow)
		{
			ICmIndirectAnnotation cca = cca1 as ICmIndirectAnnotation;
			if (cca == null)
				return false;
			Set<int> ccaAppliesTo = new Set<int>(cca.AppliesToRS.HvoArray);
			Set<int> targets = new Set<int>(MergeCellContentsMethod.HvoArrayFromList(CcasInCell(cellInSameRow)));
			return ccaAppliesTo.Intersection(targets).Count != 0;
		}

		/// <summary>
		/// Remove an existing moved text marker from movedFromCell (that points at a cca in actualCell),
		/// </summary>
		/// <param name="actualCell"></param>
		/// <param name="movedFromCell"></param>
		protected void RemoveMovedFrom(ChartLocation actualCell, ChartLocation movedFromCell)
		{
			new MakeMovedTextMethod(this, actualCell, movedFromCell, new int[] { }).RemoveMovedFrom();
		}

		/// <summary>
		/// This is the most generic form.
		/// </summary>
		/// <returns></returns>
		internal bool IsPreposed(ChartLocation actualCell, ChartLocation movedFromCell)
		{
			if (actualCell.HvoRow == movedFromCell.HvoRow)
				return IsPreposed(actualCell.ColIndex, movedFromCell.ColIndex);
			else
				return IsPreposed(actualCell.RowAnn, movedFromCell.RowAnn);
		}

		/// <summary>
		/// Use this one if you have two columns in the same row to determine
		/// if the marker is Preposed or Postposed.
		/// </summary>
		/// <param name="icolActual"></param>
		/// <param name="icolMovedFrom"></param>
		/// <returns></returns>
		internal bool IsPreposed(int icolActual, int icolMovedFrom)
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
		internal bool IsPreposed(ICmIndirectAnnotation rowActual, ICmIndirectAnnotation rowMovedFrom)
		{
			return IndexOfRow(rowActual.Hvo) < IndexOfRow(rowMovedFrom.Hvo);
		}

		/// <summary>
		/// Return true if ccaMrkr is a MovedText marker and happens to mark Preposed text.
		/// Uses the most generic form of IsPreposed(). This is somewhat labor intensive.
		/// </summary>
		/// <param name="ccaMrkr"></param>
		/// <returns></returns>
		internal bool IsPreposedMarker(ICmAnnotation ccaMrkr)
		{
			// If it's an IndirectAnnotation and it points to a data CCA earlier in the chart, return true.
			ICmIndirectAnnotation ccaMarker = ccaMrkr as ICmIndirectAnnotation;
			// check that it's not a List marker or a 'missing' marker
			if (ccaMarker == null || ccaMarker.AppliesToRS.Count != 1)
				return false;
			// check it's not a clause marker (pointing at a CCR)
			int hvoMT = ccaMarker.AppliesToRS[0].Hvo;
			if (!IsWficGroup(Cache, hvoMT))
				return false;
			// Now we know it's a movedText marker, is it Preposed or Postposed?
			int hvoMarker = ccaMarker.Hvo;
			// Get column indicies for ccaMarker and ccaMT
			ChartLocation MTCell = new ChartLocation(IndexOfColumnForCca(hvoMT),GetMyRow(hvoMT));
			ChartLocation MarkerCell = new ChartLocation(IndexOfColumnForCca(hvoMarker), GetMyRow(hvoMarker));
			return IsPreposed(MTCell, MarkerCell);
		}

		/// <summary>
		/// Make a marker indicating that what is in icolActual of the current last row
		/// has been moved from the specified column. Assume there's a CCA in icolActual.
		/// </summary>
		/// <param name="icolActual"></param>
		/// <param name="icolMovedFrom"></param>
		private void MakeMovedFrom(int icolActual, int icolMovedFrom)
		{
			// Figure where to insert, and find the target.
			MakeMovedFrom(new ChartLocation(icolActual, LastRow), new ChartLocation(icolMovedFrom, LastRow));
		}

		/// <summary>
		/// Make a MovedText marker for moves (pre/postposed) involving two different rows.
		/// </summary>
		/// <param name="actual"></param>
		/// <param name="movedFrom"></param>
		protected void MakeMovedFrom(ChartLocation actual, ChartLocation movedFrom)
		{
			new MakeMovedTextMethod(this, actual, movedFrom, new int[] { }).MakeMovedFrom();
		}

		/// <summary>
		/// Make a MovedText marker for moves (pre/postposed) involving two different rows and only part
		/// of the source cell's text. Used by the 'Advanced' Pre/Postposed dialog. Most generic form.
		/// </summary>
		/// <param name="actual"></param>
		/// <param name="movedFrom"></param>
		/// <param name="hvoWficsToMove"></param>
		protected void MakeMovedFrom(ChartLocation actual, ChartLocation movedFrom, int[] hvoWficsToMove)
		{
			// what if some part of the source cell is already marked as moved?
			// Enhance GordonM: Someday may need to handle case where array of wfic hvos is non-contiguous,
			// but not yet.
			new MakeMovedTextMethod(this, actual, movedFrom, hvoWficsToMove).MakeMovedFrom();
		}

		/// <summary>
		/// Make a marker indicating that something is missing in the specified column.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="wasMarked"></param>
		internal void ToggleMissingMarker(ChartLocation cell, bool wasMarked)
		{
			using (IUndoRedoTaskHelper undoHelper = GetUndoHelper(DiscourseStrings.ksUndoMarkMissing,
				DiscourseStrings.ksRedoMarkMissing))
			{
				if (wasMarked)
				{
					List<ICmAnnotation> ccas = CcasInCell(cell);
					foreach (ICmAnnotation cca1 in ccas)
					{
						ICmIndirectAnnotation cca = cca1 as ICmIndirectAnnotation;
						if (cca == null)
							continue;
						if (cca.AppliesToRS.Count == 0) // This is the missing marker
						{
							RemoveAndDelete(cell.RowAnn, cca.Hvo);
							break;
						}
					}
				}
				else
				{
					int iccaInsertAt = FindIndexOfFirstCcaInOrAfterColumn(cell);
					// Enhance JohnT: may want to make this configurable.
					string marker = DiscourseStrings.ksMissingMarker;
					MakeCca(cell, new int[0], iccaInsertAt, marker);
				}
			}
		}

		/// <summary>
		/// Find the index in the row's AppliesTo of the first CCA in (or after)
		/// the specified column.
		/// </summary>
		/// <param name="targetCell"></param>
		/// <returns></returns>
		int FindIndexOfFirstCcaInOrAfterColumn(ChartLocation targetCell)
		{
			ChartLocation newTarget = new ChartLocation(targetCell.ColIndex - 1, targetCell.RowAnn);
			return FindIndexOfCcaInLaterColumn(newTarget);
		}

		/// <summary>
		/// Find the index of the first cca that is in a column with index > targetCell's column index.
		/// </summary>
		/// <param name="targetCell"></param>
		/// <returns></returns>
		protected internal int FindIndexOfCcaInLaterColumn(ChartLocation targetCell)
		{
			int iccaInsertAt = targetCell.RowAnn.AppliesToRS.Count; // insert at end unless we find something in a later column.
			int icca = 0;
			for (int icol = 0; icol < AllMyColumns.Length && icca < targetCell.RowAnn.AppliesToRS.Count; )
			{
				ICmAnnotation cca = targetCell.RowAnn.AppliesToRS[icca];
				int hvoCol = cca.InstanceOfRAHvo;
				if (hvoCol == AllMyColumns[icol])
				{
					// the current annotation is in column icol. If icol > icolMovedFrom, we want to insert
					// the new cca before this one.
					if (icol > targetCell.ColIndex)
					{
						iccaInsertAt = icca; // insert before this cca, the first one in a later column.
						break;
					}
					icca++;
				}
				else
				{
					// The current cca isn't in the current column; it must be in a later one.
					// Continue the main loop.
					icol++;
				}
			}
			return iccaInsertAt;
		}

		/// <summary>
		/// Return a list of all the ccas in the specified cell.
		/// (Doesn't test the ccas for "Twfic-ness".)
		/// </summary>
		/// <param name="cell"></param>
		/// <returns></returns>
		protected internal List<ICmAnnotation> CcasInCell(ChartLocation cell)
		{
			int dummy;
			return CcasInCell(cell, out dummy);
		}

		/// <summary>
		/// Return a list of all the ccas in the specified cell(and the index in the row where the first occurs).
		/// (Doesn't test the ccas for "Twfic-ness".)
		/// If no occurences, returns empty list, but index should still be accurate.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="index">index in row of first item in list</param>
		/// <returns></returns>
		protected internal List<ICmAnnotation> CcasInCell(ChartLocation cell, out int index)
		{
			int hvoCol = AllMyColumns[cell.ColIndex];
			List<ICmAnnotation> result = new List<ICmAnnotation>();
			index = 0;
			foreach (CmAnnotation cca in cell.RowAnn.AppliesToRS)
			{
				if (cca.InstanceOfRAHvo == hvoCol)
				{
					result.Add(cca);
					continue;
				}
				if (IndexOfColumn(cca.InstanceOfRAHvo) > cell.ColIndex)
					break;
				// Keep counting until we find one in the right column
				index++;
			}
			return result;
		}

		/// <summary>
		/// Return a list of all the Data CCAs from the list of all CCAs supplied.
		/// If no occurences, returns null.
		/// </summary>
		/// <param name="ccasInCell">A list of ICmIndirectAnnotations typically from a chart cell.</param>
		/// <returns></returns>
		internal List<ICmAnnotation> CollectDataCcas(List<ICmAnnotation> ccasInCell)
		{
			List<ICmAnnotation> result = new List<ICmAnnotation>();
			foreach (CmAnnotation cca in ccasInCell)
			{
				// Loop through the entire collection in case we allow interposed markers someday
				if (IsWficGroup(Cache, cca.Hvo))
					result.Add(cca);
			}
			return result;
		}

		internal List<ICmAnnotation> CcasInRow(ICmIndirectAnnotation row)
		{
			List<ICmAnnotation> result = new List<ICmAnnotation>();
			foreach (CmAnnotation cca in row.AppliesToRS)
					result.Add(cca);
			return result;
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
		public ICmIndirectAnnotation MakeDependentClauseMarker(ChartLocation cell, ICmIndirectAnnotation[] depClauses,
			string depType)
		{
			using (IUndoRedoTaskHelper undoHelper = GetUndoHelper(DiscourseStrings.ksUndoMakeDepClause,
				DiscourseStrings.ksRedoMakeDepClause))
			{
				// There's about to be something in the source cell.
				RemoveMissingMarker(cell);

				int ihvoMinDstRow = m_chart.RowsRS.Count;
				int ihvoMaxDstRow = 0;
				List<int> depClauseHvos = new List<int>(depClauses.Length);
				foreach (CmIndirectAnnotation rowDst in depClauses)
				{
					int ihvoDst = IndexOfRow(rowDst.Hvo);
					ihvoMinDstRow = Math.Min(ihvoDst, ihvoMinDstRow);
					ihvoMaxDstRow = Math.Max(ihvoDst, ihvoMaxDstRow);
					// clears any state it has from other, hopefully 'further out' markers
					// Enhance JohnT: possibly, we want to tag the marker itself so we have (partly redundant) information
					// about which clauses are dependent at which position. Then we could confidently restore, and perhaps
					// even figure out nesting automatically. For now (See LT-8100) we decided "most recent wins, though it
					// may leave an orphan marker".
					RemoveAllDepClauseMarkers(rowDst);
					SetFeature(Cache.MainCacheAccessor, rowDst.Hvo, depType, true);
					depClauseHvos.Add(rowDst.Hvo);
				}
				SetFeature(Cache.MainCacheAccessor, depClauses[0].Hvo, StartDepClauseGroup, true);
				SetFeature(Cache.MainCacheAccessor, depClauses[depClauses.Length - 1].Hvo, EndDepClauseGroup, true);
				int crow = ihvoMaxDstRow - ihvoMinDstRow + 1;
				using (new ExtraPropChangedInserter(m_cache.MainCacheAccessor,
					m_chart.Hvo, kflidRows, ihvoMinDstRow, crow, crow))
				{
					return MakeCca(cell, depClauseHvos.ToArray(),
						FindIndexOfCcaInLaterColumn(cell), null);
				}
			}
		}

		/// <summary>
		/// Find the (first) Cca in the specified column, or null if none.
		/// </summary>
		/// <param name="cell"></param>
		/// <returns></returns>
		private ICmAnnotation FindCcaInColumn(ChartLocation cell)
		{
			return FindCcaInColumn(cell, false);
		}

		/// <summary>
		/// Find a Cca in the specified column, or null if none.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="fWantWficGroup">If true, require result to be a Wfic group.</param>
		/// <returns></returns>
		internal ICmAnnotation FindCcaInColumn(ChartLocation cell, bool fWantWficGroup)
		{
			foreach (ICmAnnotation cca in cell.RowAnn.AppliesToRS)
			{
				// if it matches icol it's the one we want (as long as it's the right type, if we are checking that).
				if (cca.InstanceOfRAHvo == AllMyColumns[cell.ColIndex])
				{
					if (!fWantWficGroup || IsWficGroup(Cache, cca.Hvo))
						return cca;
				}
			}
			return null;
		}

		const int krowSearchLimit = 11; // A rather random limit to searching for dependent clause markers.

		///<summary>
		/// Answer true if hvo (a row of the chart) is a dependent clause.
		/// It's a dependent clause if some nearby row has a cca that refers to it.
		/// (We could use a back ref, but that's hard to test and hard to keep up-to-date.)
		/// </summary>
		/// <param name="hvoRow">A CCR (chart row) hvo.</param>
		public bool IsDepClause(int hvoRow)
		{
			int ihvoRow = IndexOfRow(hvoRow);
			int start = Math.Max(ihvoRow - krowSearchLimit, 0);
			int lim = Math.Min(ihvoRow + krowSearchLimit, m_chart.RowsRS.Count);
			for (int irow = start; irow < lim; irow++)
			{
				if (irow == ihvoRow)
					continue;
				foreach (CmAnnotation cca in m_chart.RowsRS[irow].AppliesToRS)
				{
					if (cca is ICmIndirectAnnotation)
					foreach (int hvoItem in (cca as ICmIndirectAnnotation).AppliesToRS.HvoArray)
						if (hvoItem == hvoRow)
							return true;
				}
			}
			return false;
		}

		internal int IndexOfRow(int hvo)
		{
			for (int i = 0; i < m_chart.RowsRS.Count; i++)
				if (m_chart.RowsRS.HvoArray[i] == hvo)
					return i;
			return -1;
		}

		internal int IndexOfColumn(int hvo)
		{
			for (int i = 0; i < AllMyColumns.Length; i++)
				if (AllMyColumns[i] == hvo)
					return i;
			return -1;
		}

		/// <summary>
		/// Find index in row.AppliesTo of CCA.
		/// </summary>
		/// <param name="hvoCca">Hvo of the CCA</param>
		/// <param name="row"></param>
		/// <returns></returns>
		internal int IndexOfCca(int hvoCca, ICmIndirectAnnotation row)
		{
			for (int i = 0; i < row.AppliesToRS.Count; i++)
				if (row.AppliesToRS.HvoArray[i] == hvoCca)
					return i;
			return -1;
		}

		internal int IndexOfColumnForCca(int hvoCca)
		{
			Debug.Assert(hvoCca > 0);
			int hvoCol = Cache.GetObjProperty(hvoCca, kflidInstanceOf);
			Debug.Assert(hvoCol > 0);
			return IndexOfColumn(hvoCol);
		}

		/// <summary>
		/// Make a new constituent chart annotation in the specified cell (of the chart), inserting it at
		/// position iccaInsertAt of the AppliesTo property of the cell's row. Make it applyTo the specified targets,
		/// and if marker is not null set its default analysis ws to that string.
		/// </summary>
		/// <param name="cell">The ChartLocation of the new cca.</param>
		/// <param name="targets">The new cca AppliesTo these annotations(hvos).</param>
		/// <param name="iccaInsertAt">The position in the row's AppliesTo of this new cca.</param>
		/// <param name="marker">string to display</param>
		/// <returns></returns>
		internal ICmIndirectAnnotation MakeCca(ChartLocation cell, int[] targets, int iccaInsertAt, string marker)
		{
			// We don't expect these strings to be user-visible
			using (GetUndoHelper("Undo Make Cca","Redo Make Cca"))
			{
				ICmIndirectAnnotation cca = Cache.LangProject.AnnotationsOC.Add(new CmIndirectAnnotation()) as ICmIndirectAnnotation;
				cca.InstanceOfRAHvo = AllMyColumns[cell.ColIndex];
				cca.AnnotationTypeRA = CmAnnotationDefn.ConstituentChartAnnotation(Cache);
				foreach (int item in targets)
					cca.AppliesToRS.Append(item);
				if (marker != null)
					cca.Comment.SetUserDefaultWritingSystem(marker);
				// It's best to change all its properties before we insert it into the row.
				// This avoids multiple display updates. Also, currently Redo loses changes
				// to properties of newly created objects, so if we set the CCA contents
				// after inserting the CCA, we actually don't see the recreated object.
				cell.RowAnn.AppliesToRS.InsertAt(cca, iccaInsertAt);
				return cca;
			}
		}

		/// <summary>
		/// Make a new constituent chart annotation that is a CmBaseAnnotation, pointing at the
		/// specified CmPossibility (hvoListItem), in the cell's column, inserting it at
		/// position iccaInsertAt of the AppliesTo property of the cell's row.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="hvoListItem"></param>
		/// <param name="iccaInsertAt"></param>
		/// <returns></returns>
		private ICmBaseAnnotation MakeListItemCca(ChartLocation cell, int hvoListItem, int iccaInsertAt)
		{
			ICmBaseAnnotation cca = Cache.LangProject.AnnotationsOC.Add(new CmBaseAnnotation()) as ICmBaseAnnotation;
			cca.InstanceOfRAHvo = AllMyColumns[cell.ColIndex];
			cca.AnnotationTypeRA = CmAnnotationDefn.ConstituentChartAnnotation(Cache);
			cca.BeginObjectRAHvo = hvoListItem;
			// It's best to change all its properties before we insert it into the row.
			// This avoids multiple display updates. Also, currently Redo loses changes
			// to properties of newly created objects, so if we set the CCA contents
			// after inserting the CCA, we actually don't see the recreated object.
			cell.RowAnn.AppliesToRS.InsertAt(cca, iccaInsertAt);
			return cca;
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
			return FindCcaInColumn(cell) == null;
		}

		public ContextMenuStrip MakeCellContextMenu(ChartLocation clickedCell)
		{
			int irow = IndexOfRow(clickedCell.HvoRow);
			Debug.Assert(irow >= 0); // should be one of our rows!
			ContextMenuStrip menu = new ContextMenuStrip();

			// Menu items allowing wfics to be marked as pre or postposed
			MakePreposedPostposedMenuItems(menu, clickedCell);

			// Menu items allowing dependent clause/speech clause to be made
			AddDependentClauseMenuItems(clickedCell, irow, menu);

			// Menu items for Moving cell contents.
			if (CellContainsWfics(clickedCell))
			{
				MakeMoveItems(clickedCell, menu, new EventHandler(itemMoveForward_Click),
					new EventHandler(itemMoveBack_Click), DiscourseStrings.ksMoveMenuItem);
				MakeMoveItems(clickedCell, menu, new EventHandler(itemMoveWordForward_Click),
					new EventHandler(itemMoveWordBack_Click), DiscourseStrings.ksMoveWordMenuItem);
			}

			menu.Items.Add(new ToolStripSeparator());

			// Menu items allowing the user to toggle whether the line ends a paragraph.
			OneValMenuItem itemREP = new OneValMenuItem(DiscourseStrings.ksRowEndsParaMenuItem, clickedCell.HvoRow);
			itemREP.CheckState = GetFeature(Cache.MainCacheAccessor, clickedCell.HvoRow, EndParaFeatureName) ?
				CheckState.Checked : CheckState.Unchecked;
			itemREP.Click += new EventHandler(itemREP_Click);
			menu.Items.Add(itemREP);

			// Menu items allowing the user to toggle whether the line ends a sentence.
			OneValMenuItem itemRES = new OneValMenuItem(DiscourseStrings.ksRowEndsSentMenuItem, clickedCell.RowAnn.Hvo);
			itemRES.CheckState = GetFeature(Cache.MainCacheAccessor, clickedCell.RowAnn.Hvo, EndSentFeatureName) ?
				CheckState.Checked : CheckState.Unchecked;
			itemRES.Click += new EventHandler(itemRES_Click);
			menu.Items.Add(itemRES);

			// Menu items allowing the cell to be visually merged with an empty cell to the left or right.
			ICmAnnotation cca = FindCcaInColumn(clickedCell);
			if (cca != null)
			{
				// non-empty, may have merge left/right capability
				if (clickedCell.ColIndex > 0 && IsCellEmpty(new ChartLocation(clickedCell.ColIndex - 1, clickedCell.RowAnn)))
				{
					RowColMenuItem itemMergeBefore = new RowColMenuItem(DiscourseStrings.ksMergeBeforeMenuItem,
						clickedCell);
					itemMergeBefore.Click += new EventHandler(itemMergeBefore_Click);
					if (GetFeature(Cache.MainCacheAccessor, cca.Hvo, mergeBeforeTag))
						itemMergeBefore.Checked = true;
					menu.Items.Add(itemMergeBefore);
				}
				if (clickedCell.ColIndex < AllMyColumns.Length - 1 && IsCellEmpty(new ChartLocation(clickedCell.ColIndex + 1, clickedCell.RowAnn)))
				{
					RowColMenuItem itemMergeAfter = new RowColMenuItem(DiscourseStrings.ksMergeAfterMenuItem,
						clickedCell);
					ICmAnnotation ccaLast = FindCcaInColumn(clickedCell, false);
					if (GetFeature(Cache.MainCacheAccessor, ccaLast.Hvo, mergeAfterTag))
						itemMergeAfter.Checked = true;
					menu.Items.Add(itemMergeAfter);
					itemMergeAfter.Click += new EventHandler(itemMergeAfter_Click);
				}
			}

			RowColMenuItem itemNewRow = new RowColMenuItem(DiscourseStrings.ksInsertRowMenuItem, clickedCell);
			menu.Items.Add(itemNewRow);
			itemNewRow.Click += new EventHandler(itemNewRow_Click);

			RowColMenuItem itemCFH = new RowColMenuItem(DiscourseStrings.ksClearFromHereOnMenuItem, clickedCell);
			menu.Items.Add(itemCFH);
			itemCFH.Click += new EventHandler(itemCFH_Click);

			menu.Items.Add(new ToolStripSeparator());

			// Menu items for inserting arbitrary markers from the ChartMarkers list.
			ICmPossibilityList ChartMarkerList = Cache.LangProject.DiscourseDataOA.ChartMarkersOA;
			GeneratePlMenuItems(menu, ChartMarkerList, new EventHandler(ToggleMarker_Item_Click), clickedCell);

			MissingMarkerState mms = MissingState(clickedCell);
			if (mms != MissingMarkerState.kmmsDoesNotApply)
			{
				RowColMenuItem itemMissingMarker = new RowColMenuItem(DiscourseStrings.ksMarkMissingItem, clickedCell);
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
			ICmAnnotation cca = FindCcaInColumn(cell);
			if (cca == null)
				return MissingMarkerState.kmmsUnchecked;
			if (!(cca is ICmIndirectAnnotation))
			{
				if (cca is ICmBaseAnnotation)
					return MissingMarkerState.kmmsUnchecked; // Possibility marker only (not 'content')
				return MissingMarkerState.kmmsDoesNotApply; // not empty or missing marker (might not be reachable)
			}
			if (IsMissingMarker(cca))
				return MissingMarkerState.kmmsChecked;
			return MissingMarkerState.kmmsDoesNotApply; // clause or moved text marker
		}

		/// <summary>
		/// Currently an annotation is identified as a missing marker (assuming it's in a chart row)
		/// by being an indirect one that doesn't point at anything.
		/// </summary>
		/// <param name="cca"></param>
		/// <returns></returns>
		private static bool IsMissingMarker(ICmAnnotation cca)
		{
			if (!(cca is ICmIndirectAnnotation))
				return false;
			return ((cca as ICmIndirectAnnotation).AppliesToRS.Count == 0);
		}

		/// <summary>
		/// If there's a missing marker in the specified cell get rid of it.
		/// </summary>
		/// <param name="cell"></param>
		internal void RemoveMissingMarker(ChartLocation cell)
		{
			ICmAnnotation cca = FindCcaInColumn(cell);
			if (IsMissingMarker(cca))
				RemoveAndDelete(cell.RowAnn, cca.Hvo);
		}

		private void AddDependentClauseMenuItems(ChartLocation srcCell, int irow, ContextMenuStrip menu)
		{
			// See whether there is any existing dep clause marker (of any subtype) in the cell.
			foreach (ICmAnnotation cca in CcasInCell(srcCell))
			{
				int dummy;
				if (IsClausePlaceholder(cca.Hvo, out dummy))
				{
					ToolStripMenuItem itemR = new RowColMenuItem(DiscourseStrings.ksRemoveDependentMarkerMenuItem,
						srcCell);
					itemR.Click += new EventHandler(itemR_Click);
					menu.Items.Add(itemR);
					return;
				}
			}
			ToolStripMenuItem itemMDC = MakeDepClauseItem(srcCell, irow, DiscourseStrings.ksMakeDepClauseMenuItem,
				DepClauseFeatureName);
			menu.Items.Add(itemMDC);

			ToolStripMenuItem itemMSC = MakeDepClauseItem(srcCell, irow, DiscourseStrings.ksMakeSpeechClauseMenuItem,
				SpeechClauseFeatureName);
			menu.Items.Add(itemMSC);

			ToolStripMenuItem itemMSoC = MakeDepClauseItem(srcCell, irow, DiscourseStrings.ksMakeSongClauseMenuItem,
				SongClauseFeatureName);
			menu.Items.Add(itemMSoC);
		}

		void itemR_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
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
			using (GetUndoHelper(DiscourseStrings.ksUndoRemoveClauseMarker, DiscourseStrings.ksRedoRemoveClauseMarker))
			{
				foreach (ICmAnnotation cca in CcasInCell(cell))
				{
					int hvoFirstTarget;
					if (IsClausePlaceholder(cca.Hvo, out hvoFirstTarget))
					{
						ICmIndirectAnnotation clauseMarker = cca as ICmIndirectAnnotation;
						int iFirstDepRow = IndexOfRow(clauseMarker.AppliesToRS[0].Hvo);
						int chvo = clauseMarker.AppliesToRS.Count;
						using (new ExtraPropChangedInserter(m_cache.MainCacheAccessor, m_chart.Hvo,
							kflidRows, iFirstDepRow, chvo, chvo))
						{
							cell.RowAnn.AppliesToRS.Remove(clauseMarker);
							foreach (ICmAnnotation depRow in clauseMarker.AppliesToRS)
							{
								RemoveAllDepClauseMarkers(depRow);
							}
							Set<int> hvosToDelete = new Set<int>();
							hvosToDelete.Add(clauseMarker.Hvo);
							CmObject.DeleteObjects(hvosToDelete, Cache, false);
							return;
						}
					}
				}
			}
		}

		private void RemoveAllDepClauseMarkers(ICmAnnotation depRow)
		{
			SetFeature(m_cache.MainCacheAccessor, depRow.Hvo, DepClauseFeatureName, false);
			SetFeature(m_cache.MainCacheAccessor, depRow.Hvo, SpeechClauseFeatureName, false);
			SetFeature(m_cache.MainCacheAccessor, depRow.Hvo, SongClauseFeatureName, false);
			SetFeature(m_cache.MainCacheAccessor, depRow.Hvo, StartDepClauseGroup, false);
			SetFeature(m_cache.MainCacheAccessor, depRow.Hvo, EndDepClauseGroup, false);
		}

		void itemCFH_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			int crowDel = m_chart.RowsRS.Count - IndexOfRow(item.SrcRow.Hvo);
			if (MessageBox.Show(String.Format(DiscourseStrings.ksDelRowWarning, crowDel),
				DiscourseStrings.ksWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
			{
				ClearChartFromHereOn(item.SrcCell);
			}
		}

		void itemNewRow_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			InsertRow(item.SrcRow);
		}

		private bool CellContainsWfics(ChartLocation cell)
		{
			return FindFirstCcaWithWfics(CcasInCell(cell)) != null;
		}

		private void MakeMoveItems(ChartLocation srcCell, ContextMenuStrip menu, EventHandler forward, EventHandler backward, string mainLabel)
		{
			// If there's nothing in the cell we can't move it; and a missing marker doesn't count.
			ICmAnnotation cca = FindCcaInColumn(srcCell);
			if (cca == null || IsMissingMarker(cca))
				return;

			ToolStripMenuItem itemMove = new ToolStripMenuItem(mainLabel);
			if (TryGetNextCell(srcCell))
			{
				RowColMenuItem itemMoveForward = new RowColMenuItem(DiscourseStrings.ksForwardMenuItem,
					srcCell);
				itemMoveForward.Click += forward;
				itemMove.DropDownItems.Add(itemMoveForward);
			}
			if (TryGetPreviousCell(srcCell))
			{
				RowColMenuItem itemMoveBack = new RowColMenuItem(DiscourseStrings.ksBackMenuItem,
					srcCell);
				itemMoveBack.Click += backward;
				itemMove.DropDownItems.Add(itemMoveBack);
			}
			menu.Items.Add(itemMove);
		}

		private void MakePreposedPostposedMenuItems(ContextMenuStrip menu, ChartLocation srcCell)
		{
			// Collect all data CCAs in the source cell
			List<ICmAnnotation> dataCcas = CollectDataCcas(CcasInCell(srcCell));
			if (dataCcas == null || dataCcas.Count == 0)
				return; // can't do this without some real data in the cell.

			ChartLocation markerCell; // Might eventually contain the location of the 'movedText' marker (MM=MovedMarker)
			bool fMMDifferentRow = false; // true if we find a marker in a different row pointing to this cell (either direction)
			bool fMMForward = false; // true if Preposed text has a marker in a different row
			bool fMMBackward = false; // true if Postposed text has a marker in a different row
			foreach (ICmAnnotation ccaWfic1 in dataCcas)
			{
				ICmIndirectAnnotation ccaWfic = ccaWfic1 as ICmIndirectAnnotation;

				// Check to see if ccaWfic is 'movedText' and if so, find its target
				if (GetFeature(Cache.MainCacheAccessor, ccaWfic.Hvo, MovedTextFeatureName))
				{
					// FindMovedMarkerRow can be expensive time-wise,
					// we only want to use it once, and only if we have to!
					markerCell = FindMovedMarkerCell(ccaWfic, srcCell);
					if (markerCell != null && srcCell.HvoRow != markerCell.HvoRow)
					{
						fMMDifferentRow = true;
						if (IndexOfRow(markerCell.HvoRow) > IndexOfRow(srcCell.HvoRow))
							fMMForward = true;
						else
							fMMBackward = true;
					}
				}
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
			bool fAnotherClausePossible = IsAnotherClausePossible(srcCell.RowAnn, text == DiscourseStrings.ksPreposeFromMenuItem);
			if ((icolStart >= icolLim) && !fMarkerPresent && !fAnotherClausePossible)
				return;
			ToolStripMenuItem itemMTSubmenu = new ToolStripMenuItem(text);
			menu.Items.Add(itemMTSubmenu);
			for (int i = icolStart; i < icolLim; i++)
			{
				TwoColumnMenuItem itemCol = new TwoColumnMenuItem(GetColumnLabel(i), srcCell.ColIndex, i, srcCell.RowAnn);
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

		private bool IsAnotherClausePossible(ICmIndirectAnnotation rowSrc, bool fPrepose)
		{
			List<ICmIndirectAnnotation> eligibleRows = CollectRowsInSentence(rowSrc, fPrepose);
			return (eligibleRows.Count > 1); // If there's more than the source row, then it's possible!
		}

		/// <summary>
		/// Find the Marker that points to the 'movedText' wfic group in ccaTarget (of targetCell).
		/// Return the marker's ChartLocation. Start with the target's row and move out.
		/// </summary>
		/// <param name="ccaTarget">The CCA that has a marker pointing to it somewhere in the chart.</param>
		/// <param name="targetCell">ccaTarget's ChartLocation</param>
		/// <returns></returns>
		private ChartLocation FindMovedMarkerCell(ICmIndirectAnnotation ccaTarget, ChartLocation targetCell)
		{
			int icolMarker = -1;
			// Look in my row for my marker first
			int irow = IndexOfRow(targetCell.HvoRow);
			if (IsMovedMarkerInRow(ccaTarget, irow, out icolMarker))
			{
				return new ChartLocation(icolMarker, targetCell.RowAnn); // Found it!
			}
			// Didn't find one in my own row.
			// Look in earlier & later rows ("simultaneously"?)
			int crow = m_chart.RowsRS.Count;
			int loopIncr = 1;
			int imax;
			int imin;
			while ((irow + loopIncr) < crow || (irow - loopIncr) > -1) // must hit both limits to quit
			{
				// Look for marker in 2 rows: imax and imin
				imax = irow + loopIncr;
				imin = irow - loopIncr;
				// Upper row = imin; test again
				if (imin > -1 && IsMovedMarkerInRow(ccaTarget, imin, out icolMarker))
					return new ChartLocation(icolMarker, m_chart.RowsRS[imin]); // Found it!
				// Lower row = imax; test again
				if (imax < crow && IsMovedMarkerInRow(ccaTarget, imax, out icolMarker))
					return new ChartLocation(icolMarker, m_chart.RowsRS[imax]); // Found it!

				// Go one row farther both directions
				loopIncr++;
			}
			Debug.Assert(false, "Moved Text marker not found.");
			return null; // Shouldn't ever happen...
		}

		/// <summary>
		/// Returns true if there is a 'movedText' marker in the current row that points to
		/// ccaTarget.
		/// </summary>
		/// <param name="ccaTarget"></param>
		/// <param name="irow">Index of the current row.</param>
		/// <param name="icolMarker">Index of the marker's column.</param>
		/// <returns></returns>
		private bool IsMovedMarkerInRow(ICmIndirectAnnotation ccaTarget, int irow, out int icolMarker)
		{
			icolMarker = -1;
			foreach (ICmAnnotation cca1 in m_chart.RowsRS[irow].AppliesToRS)
			{
				ICmIndirectAnnotation cca = cca1 as ICmIndirectAnnotation;
				if (cca == null || cca.AppliesToRS.Count == 0 || cca.AppliesToRS[0] == null || cca.Hvo == ccaTarget.Hvo)
					continue;
				if (cca.AppliesToRS[0].Hvo == ccaTarget.Hvo)
				{
					icolMarker = IndexOfColumn(cca.InstanceOfRAHvo);
					return true; // Found it!
				}
			}
			return false;
		}

		const int rowLimiter = 5;

		/// <summary>
		/// Collects a list of rows in the same Sentence as the supplied CCR
		/// going in the specified direction. "Same Sentence" is defined by the rows' EndOfSentence
		/// Feature and limited (at this point) to 5 rows forward or backward.
		/// Current row is now included.
		/// </summary>
		/// <param name="curRow"></param>
		/// <param name="fForward">true is forward, false is backward.</param>
		/// <returns></returns>
		private List<ICmIndirectAnnotation> CollectRowsInSentence(ICmIndirectAnnotation curRow, bool fForward)
		{
			// Better check first to see if we're going forward and curRow has EOS feature
			List<ICmIndirectAnnotation> result = new List<ICmIndirectAnnotation>();
			result.Add(curRow); // Include current row.
			if (fForward && GetFeature(Cache.MainCacheAccessor, curRow.Hvo, EndSentFeatureName))
				return result;
			int testIndex = IndexOfRow(curRow.Hvo);
			int indexMax = Math.Min(m_chart.RowsRS.HvoArray.Length - 1, testIndex + rowLimiter);
			int indexMin = Math.Max(0, testIndex - rowLimiter);
			do
			{
				ICmIndirectAnnotation tempCcr;
				// if fForward, are there rows after rowSrc?
				// if not, are there rows before rowSrc?
				if (fForward)
					testIndex++;
				else
					testIndex--;
				if (testIndex < indexMin || testIndex > indexMax)
					break; // We went too far!
				tempCcr = m_chart.RowsRS[testIndex];
				if (GetFeature(Cache.MainCacheAccessor, tempCcr.Hvo, EndSentFeatureName))
				{
					if (fForward)
						result.Add(tempCcr); // going forward, include the EOS row
					break;
				}
				result.Add(tempCcr);
			} while (true);
			if (!fForward)
				result.Reverse();
			return result;
		}

		/// <summary>
		/// Checks CCA(hvoCca) to see if it has an AppliesTo (we already know it is not pointing to Wfics)
		/// If the AppliesTo has one target it must be the target of this MovedText marker,
		/// so check to see if the target CCA has a movedText Feature, if not make it so.
		/// (Used by CleanupInvalidWfics() on chart load). This is only 'public' because I wanted to test it
		/// and the calling routine is 'private' too.
		/// </summary>
		/// <param name="hvoCca"></param>
		/// <returns>false, if the CCA has a non-data CCA as a target.</returns>
		public bool CheckForUnsetMovedTextOrInvalidMrkr(int hvoCca)
		{
			// We only get here if hvoCca is not a Wfic group.
			// So far that leaves us a few possibilities:
			// - "Real" Missing marker
			// - List marker
			// - MovedText marker
			// - Clause reference marker
			ICmIndirectAnnotation cca = CmAnnotation.CreateFromDBObject(Cache, hvoCca) as ICmIndirectAnnotation;
			if (cca == null || cca.AppliesToRS.Count != 1)
				return true; // Eliminates Missing and List marker cases

			int hvoTarget = m_cache.GetVectorItem(hvoCca, kflidAppliesTo, 0);
			if (Cache.GetObjProperty(hvoTarget, kflidAnnotationType) == CmAnnotationDefn.ConstituentChartRow(Cache).Hvo)
				return true; // Eliminates Clause ref case
			// Only remaining possibility is a MovedText Marker
			if (!IsWficGroup(Cache, hvoTarget))
			{
				// Found a problem! MovedText Markers should only point to data CCAs
				// If we return 'false', the calling routine will delete the source CCA.
				return false;
			}
			// Check for unset MovedText Feature and set it if not found.
			if (!GetFeature(m_cache.MainCacheAccessor, hvoTarget,
							MovedTextFeatureName))
				SetFeature(m_cache.MainCacheAccessor, hvoTarget,
						   MovedTextFeatureName, true);
			return true;
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
			if (m_chart.RowsRS.Count < 1)
				return;

			RowColMenuItem item = sender as RowColMenuItem;
			ChartLocation srcCell = item.SrcCell;
			int iSrc = IndexOfRow(srcCell.HvoRow);
			int iColSrc = srcCell.ColIndex; // in this case, the index of the clicked (marker target) column.

			if (item.Checked)
			{
				// Go find the marker that points to this cell and remove it.
				ChartLocation markerCell = FindMovedMarkerOtherRow(srcCell, fPrepose);
				RemoveMovedFrom(srcCell, markerCell);
				return;
			}

			// Collect rows and columns to display in dialog
			// First rows
			ICmIndirectAnnotation[] eligibleRows = CollectEligibleRows(srcCell, fPrepose).ToArray();
			if (eligibleRows.Length == 0) return; // Shouldn't happen!

			CChartSentenceElements paramObject = new CChartSentenceElements(srcCell, eligibleRows, AllMyColumns);

			// Enhance GordonM: We need to do something different if there are multiple CCAs in the cell?!
			// I did 'something different' alright, but I'm not done.
			// Maybe I need to make a temporary CCA that holds all the wfics in the cell for dialog ribbon display purposes.

			// Load all data CCAs in this cell into the parameter object's AffectedCcas.
			paramObject.AffectedCcas = CollectAllDataCcas(CcasInCell(srcCell));
			AdvancedMTDialog dlg = new AdvancedMTDialog(Cache, fPrepose, paramObject);

			// Display dialog
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				ICmIndirectAnnotation fromRow = dlg.SelectedRow.Row;
				int iColMovedFrom = IndexOfColumn(dlg.SelectedColumn.Column.Hvo);

				using (GetUndoHelper(DiscourseStrings.ksUndoMakeMoved, DiscourseStrings.ksRedoMakeMoved))
				{
					// LT-7668 If user chooses to make a movedText marker and one exists already for this cell
					// we need to remove the first one before adding this one.
					// Now we check all the CCAs affected by our new MovedText feature and remove any existing marker.
					foreach (int hvoCca in paramObject.AffectedCcas)
					{
						if (GetFeature(Cache.MainCacheAccessor, hvoCca, MovedTextFeatureName))
						{
							ChartLocation markerCell = FindMovedMarkerCell(
								CmIndirectAnnotation.CreateFromDBObject(Cache, hvoCca), srcCell);
							if (markerCell.IsValidLocation)
								RemoveMovedFrom(srcCell, markerCell);
						}
					}
					ChartLocation movedFrom = new ChartLocation(iColMovedFrom, fromRow);
					MakeMovedFrom(srcCell, movedFrom, dlg.SelectedWfics);
				}
			}
		}

		/// <summary>
		/// Returns a list of rows that should be eligible to choose from the Advanced... dialog row combo box.
		/// </summary>
		/// <param name="clickedCell"></param>
		/// <param name="fPrepose"></param>
		/// <returns></returns>
		protected List<ICmIndirectAnnotation> CollectEligibleRows(ChartLocation clickedCell, bool fPrepose)
		{
			// Collect all rows in the 'right' direction including the clicked one.
			List<ICmIndirectAnnotation> result = CollectRowsInSentence(clickedCell.RowAnn, fPrepose);
			if (result.Count < 0)
				return result;
			// If we are marking Postposed from the first column, we don't want the last row to be eligible.
			// If we are marking Preposed from the last column, we don't want the first row to be eligible.
			if (fPrepose)
			{
				int icolLast = IndexOfColumn(AllMyColumns[AllMyColumns.Length - 1]);
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
		private ChartLocation FindMovedMarkerOtherRow(ChartLocation srcCell, bool fPrepose)
		{
			// Want the wfic group CCA that is the target of a Moved Text Marker in another row.
			// Make sure if there is more than one data CCA that we have the one with the movedText feature!
			// Enhance GordonM: Need to extract a FindMTCca() method here.
			List<ICmAnnotation> ccasInTargetCell = CcasInCell(srcCell);
			int iccaInCell; // index within the cell of the current CCA

			// Start search at beginning of cell
			ICmIndirectAnnotation ccaTarget = FindNextCcaWithWfics(ccasInTargetCell, out iccaInCell);
			int hvoTarget = -1; // This variable is needed in both the 'while' and the 'for' loop.
			while (ccaTarget != null)
			{
				hvoTarget = ccaTarget.Hvo;
				if (GetFeature(Cache.MainCacheAccessor, hvoTarget, MovedTextFeatureName))
					break;
				int iFoundAt;
				ccaTarget = FindNextCcaWithWfics(ccasInTargetCell, iccaInCell, out iFoundAt);
				iccaInCell = iFoundAt;
			}
			if (ccaTarget == null)
				return null; // shouldn't happen!

			// Loop from this row either forward or backward depending on fPrepose looking for the MT marker
			int irow = IndexOfRow(srcCell.HvoRow);
			int rowIncr = fPrepose ? 1 : -1;
			for (irow += rowIncr; irow < m_chart.RowsRS.Count && irow > -1; irow += rowIncr)
			{
				foreach (ICmAnnotation cca1 in m_chart.RowsRS[irow].AppliesToRS)
				{
					ICmIndirectAnnotation cca = cca1 as ICmIndirectAnnotation;
					if (cca == null || cca.AppliesToRS[0] == null)
						continue;
					if (cca.AppliesToRS[0].Hvo == hvoTarget)
					{
						// Found it!
						return new ChartLocation(IndexOfColumnForCca(cca.Hvo), m_chart.RowsRS[irow]);
					}
				}
			}
			Debug.Assert(false, "We haven't found the marker that should be here.");
			return null;
		}

		void itemCol_Click(object sender, EventArgs e)
		// Mark Moved Text from one cell to another(within a row).
		{
			TwoColumnMenuItem item = sender as TwoColumnMenuItem;
			ChartLocation srcCell = new ChartLocation(item.Source, item.Row);
			ChartLocation dstCell = new ChartLocation(item.Destination, item.Row);
			if (item.Checked)
				RemoveMovedFrom(dstCell, srcCell);
			else
			{
				ICmIndirectAnnotation cca = FindFirstCcaWithWfics(CcasInCell(dstCell));

				using (GetUndoHelper(DiscourseStrings.ksUndoMakeMoved, DiscourseStrings.ksRedoMakeMoved))
				{
					// LT-7668 If user chooses to make a movedText marker and one exists already for this cell
					// we need to remove the first one before adding this one.
					if (GetFeature(Cache.MainCacheAccessor, cca.Hvo, MovedTextFeatureName))
					{
						ChartLocation markerCell = FindMovedMarkerCell(cca, dstCell);
						RemoveMovedFrom(dstCell, markerCell);
					}
					MakeMovedFrom(dstCell, srcCell);
				}
			}
		}

		void itemMoveForward_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			MoveCellForward(item.SrcCell);
		}
		void itemMoveBack_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			MoveCellBack(item.SrcCell);
		}
		void itemMoveWordForward_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			MoveWordForward(item.SrcCell);
		}
		void itemMoveWordBack_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			MoveWordBack(item.SrcCell);
		}

		void itemMergeAfter_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			ToggleMergedCellFlag(item.SrcCell, true);
		}

		void itemMergeBefore_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			ToggleMergedCellFlag(item.SrcCell, false);

		}

		private void GeneratePlMenuItems(ContextMenuStrip menu, ICmPossibilityList list,
			EventHandler clickHandler, ChartLocation cell)
		{
			List<ICmBaseAnnotation> markerRefs = SelectBaseAnnotations(CcasInCell(cell));
			foreach (ICmPossibility poss in list.PossibilitiesOS)
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
		/// <param name="rowSrc"></param>
		/// <param name="icol"></param>
		/// <param name="poss"></param>
		/// <returns></returns>
		private ToolStripMenuItem MakePlItem(EventHandler clickHandler, ChartLocation cell,
			ICmPossibility poss, List<ICmBaseAnnotation> markerRefs, string format)
		{
			RowColPossibilityMenuItem item = new RowColPossibilityMenuItem(cell, poss.Hvo);
			string label = CmPossibility.BestAnalysisName(Cache, poss.Hvo).Text;
			if (label == null)
				label = "";
			if (poss.SubPossibilitiesOS.Count == 0)
			{
				string abbr = Cache.LangProject.GetMagicStringAlt(LangProject.kwsFirstAnal, poss.Hvo,
					kflidAbbreviation).Text;
				if (abbr != label && !String.IsNullOrEmpty(abbr))
					label = label + " (" + abbr + ")";
				item.Click += clickHandler; // can only select leaves.
				if (ListRefersToMarker(markerRefs, poss.Hvo))
					item.Checked = true;
			}
			if (format != null)
				label = String.Format(format, label);
			item.Text = label;
			foreach (ICmPossibility poss2 in poss.SubPossibilitiesOS)
				item.DropDownItems.Add(MakePlItem(clickHandler, cell, poss2, markerRefs, null));
			return item;
		}

		private List<ICmBaseAnnotation> SelectBaseAnnotations(List<ICmAnnotation> input)
		{
			List<ICmBaseAnnotation> result = new List<ICmBaseAnnotation>(input.Count);
			foreach(ICmAnnotation cca in input)
				if (cca is ICmBaseAnnotation)
					result.Add(cca as ICmBaseAnnotation);
			return result;
		}

		private bool ListRefersToMarker(List<ICmBaseAnnotation> list, int hvoMarker)
		{
			foreach (ICmBaseAnnotation cba in list)
				if (cba.BeginObjectRAHvo == hvoMarker)
					return true;
			return false;
		}

		void ToggleMarker_Item_Click(object sender, EventArgs e)
		{
			RowColPossibilityMenuItem item = sender as RowColPossibilityMenuItem;

			AddOrRemoveMarker(item);
		}

		public void AddOrRemoveMarker(RowColPossibilityMenuItem item)
		{
			if (item.Checked)
				using (new UndoRedoTaskHelper(m_cache, DiscourseStrings.ksUndoRemoveMarker, DiscourseStrings.ksRedoRemoveMarker))
					RemoveListItemCca(item.SrcCell, item.m_hvoPoss);
			else
			{
				using (new UndoRedoTaskHelper(m_cache, DiscourseStrings.ksUndoAddMarker, DiscourseStrings.ksRedoAddMarker))
				{
					int iccaInsertAt = FindIndexOfCcaInLaterColumn(item.SrcCell);
					MakeListItemCca(item.SrcCell, item.m_hvoPoss, iccaInsertAt);
				}
			}
		}

		/// <summary>
		/// Delete a cca from a row; use in cases where this is the only thing to delete.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="cca"></param>
		private void RemoveAndDelete(ICmIndirectAnnotation row, int cca)
		{
			Set<int> hvosToDelete = new Set<int>();
			hvosToDelete.Add(cca);
			row.AppliesToRS.Remove(cca);
			CmObject.DeleteObjects(hvosToDelete, Cache, false);
		}

		private void RemoveListItemCca(ChartLocation srcCell, int hvoMarker)
		{
			foreach (ICmAnnotation cca in CcasInCell(srcCell))
				if (cca is ICmBaseAnnotation && (cca as ICmBaseAnnotation).BeginObjectRAHvo == hvoMarker)
				{
					RemoveAndDelete(srcCell.RowAnn, cca.Hvo);
					return;
				}
		}

		const int kdepClauseRowLimit = 5; // A limit to possible dependent clause menu rows.

		private ToolStripMenuItem MakeDepClauseItem(ChartLocation srcCell, int irowSrc,
			string mainLabel, string featureName)
		{
			ToolStripMenuItem itemMDC = new ToolStripMenuItem(mainLabel);
			if (irowSrc > 0)
			{
				// put in just one 'previous clause' item.
				DepClauseMenuItem item = new DepClauseMenuItem(DiscourseStrings.ksPreviousClauseMenuItem,
					srcCell, new ICmIndirectAnnotation[] { m_chart.RowsRS[irowSrc - 1] });
				item.Click += new EventHandler(itemDC_Click);
				item.DepType = featureName;
				itemMDC.DropDownItems.Add(item);
			}
			List<ICmIndirectAnnotation> depClauseRows = new List<ICmIndirectAnnotation>(kdepClauseRowLimit);
			for (int irow = irowSrc + 1; irow < Math.Min(irowSrc + kdepClauseRowLimit, m_chart.RowsRS.Count); irow++)
			{
				string label;
				switch (irow - irowSrc)
				{
					case 1: label = DiscourseStrings.ksNextClauseMenuItem; break;
					case 2: label = DiscourseStrings.ksNextTwoClausesMenuItem; break;
					default: label = String.Format(DiscourseStrings.ksNextNClausesMenuItem, (irow - irowSrc).ToString()); break;
				}
				depClauseRows.Add(m_chart.RowsRS[irow]);
				DepClauseMenuItem item = new DepClauseMenuItem(label, srcCell, depClauseRows.ToArray());
				item.Click += new EventHandler(itemDC_Click);
				itemMDC.DropDownItems.Add(item);
				item.DepType = featureName;
			}
			DepClauseMenuItem itemOther = new DepClauseMenuItem(DiscourseStrings.ksOtherMenuItem, srcCell, null);
			itemMDC.DropDownItems.Add(itemOther);
			itemOther.Click += new EventHandler(itemOther_Click);
			itemOther.DepType = featureName;

			return itemMDC;
		}

		// Generates a dialog with each row except the one clicked (within reason)
		void itemOther_Click(object sender, EventArgs e)
		{
			if (m_chart.RowsRS.Count == 1)
				return;
			DepClauseMenuItem item = sender as DepClauseMenuItem;
			SelectClausesDialog dlg = new SelectClausesDialog();
			List<RowMenuItem> items = new List<RowMenuItem>();
			int iSrc = IndexOfRow(item.HvoRow);
			int iSelect = -1;
			for (int irow = Math.Max(iSrc - 10, 0); irow < Math.Min(iSrc + 20, m_chart.RowsRS.Count); irow++)
			{
				if (irow == iSrc)
				{
					iSelect = items.Count;
					continue;
				}
				items.Add(new RowMenuItem(m_chart.RowsRS[irow]));
			}
			dlg.SetRows(items);
			if (iSelect >= items.Count)
				iSelect = items.Count - 1;
			dlg.SelectedRow = items[iSelect];
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				ICmIndirectAnnotation outer = dlg.SelectedRow.Row;
				int index = IndexOfRow(outer.Hvo);
				int start = iSrc + 1;
				int end = index;
				if (index < iSrc)
				{
					start = index;
					end = iSrc - 1;
				}
				List<ICmIndirectAnnotation> rows = new List<ICmIndirectAnnotation>();
				for (int i = start; i <= end; i++)
					rows.Add(m_chart.RowsRS[i]);
				MakeDependentClauseMarker(item.SrcCell, rows.ToArray(), item.DepType);
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
			OneValMenuItem item = sender as OneValMenuItem;
			bool fSentWasOn;
			int hvoRow = item.Source;
			int irow = IndexOfRow(hvoRow);

			if (GetFeature(Cache.MainCacheAccessor, hvoRow, EndParaFeatureName))
				SetFeature(Cache.MainCacheAccessor, hvoRow, EndParaFeatureName, false);
			else
			{
				// Save EOS state for determining if we need to renumber rows
				fSentWasOn = GetFeature(Cache.MainCacheAccessor, hvoRow, EndSentFeatureName);

				// Set both EOP and EOS
				SetFeature(Cache.MainCacheAccessor, hvoRow, EndParaFeatureName, true);
				SetFeature(Cache.MainCacheAccessor, hvoRow, EndSentFeatureName, true);

				// Turning on EOP only affects numbering if EOS was off before
				if (!fSentWasOn)
					RenumberRows(irow, false);
			}
			m_cache.PropChanged(m_chart.Hvo, kflidRows, irow, 1, 1);
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
			int irow = IndexOfRow(hvoRow);

			if (GetFeature(Cache.MainCacheAccessor, hvoRow, EndSentFeatureName))
			{
				// unchecking EOS, unchecks EOP too
				SetFeature(Cache.MainCacheAccessor, hvoRow, EndSentFeatureName, false);
				SetFeature(Cache.MainCacheAccessor, hvoRow, EndParaFeatureName, false);
			}
			else
				SetFeature(Cache.MainCacheAccessor, hvoRow, EndSentFeatureName, true);
			// Now we need to renumber our row labels, unless we're on the last row.
			if (!(irow == m_chart.RowsRS.Count - 1))
				RenumberRows(irow, false);
			m_cache.PropChanged(m_chart.Hvo, kflidRows, irow, 1, 1);
		}

		/// <summary>
		/// Invoked when the user clicks an item in add dependent/speech/song clause.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void itemDC_Click(object sender, EventArgs e)
		{
			DepClauseMenuItem item = sender as DepClauseMenuItem;
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
			using (IUndoRedoTaskHelper undoHelper = GetUndoHelper(DiscourseStrings.ksUndoMergeCells, DiscourseStrings.ksRedoMergeCells))
			{
				using (new ExtraPropChangedInserter(Cache.MainCacheAccessor,
					m_chart.Hvo, kflidRows, IndexOfRow(srcCell.HvoRow), 1, 1))
				{
					ICmAnnotation cca = FindCcaInColumn(srcCell, false);
					string id = following ? mergeAfterTag : mergeBeforeTag;
					bool newValue = !GetFeature(Cache.MainCacheAccessor, cca.Hvo, id);
					SetFeature(Cache.MainCacheAccessor, cca.Hvo, id, newValue);
					if (newValue)
					{
						// Make sure other direction is off
						ICmAnnotation cca2 = FindCcaInColumn(srcCell, false);
						string id2 = following ? mergeBeforeTag : mergeAfterTag;
						if (GetFeature(Cache.MainCacheAccessor, cca2.Hvo, id2))
							SetFeature(Cache.MainCacheAccessor, cca2.Hvo, id2, false);
					}
					return null;
				}
			}
		}

		public string GetColumnLabel(int icol)
		{
			return CmPossibility.BestAnalysisName(Cache, AllMyColumns[icol]).Text;
		}

		/// <summary>
		/// Creates the menu for a column's down arrow (context) button.
		/// </summary>
		/// <param name="icol">The icol.</param>
		/// <returns></returns>
		public ContextMenuStrip MakeContextMenu(int icol)
		{
			ContextMenuStrip menu = new ContextMenuStrip();

			OneValMenuItem itemNewClause = new OneValMenuItem(DiscourseStrings.ksMoveHereInNewClause, icol);
			itemNewClause.Click += new EventHandler(itemNewClause_Click);
			menu.Items.Add(itemNewClause);

			ToolStripMenuItem itemMT = new ToolStripMenuItem(DiscourseStrings.ksMovedFromMenuItem);
			for (int ihvo = 0; ihvo < AllMyColumns.Length; ihvo++)
			{
				if (ihvo == icol)
					continue;
				TwoColumnMenuItem item = new TwoColumnMenuItem(GetColumnLabel(ihvo), icol, ihvo);
				item.Click += new EventHandler(InsertMovedText_Click);
				itemMT.DropDownItems.Add(item);
			}
			menu.Items.Add(itemMT);
			return menu;
		}

		void itemMissingMarker_Click(object sender, EventArgs e)
		{
			RowColMenuItem item = sender as RowColMenuItem;
			ToggleMissingMarker(item.SrcCell, item.Checked);
		}

		void itemNewClause_Click(object sender, EventArgs e)
		{
			OneValMenuItem item = sender as OneValMenuItem;
			MoveToHereInNewClause(item.Source);
		}

		void InsertMovedText_Click(object sender, EventArgs e)
		{
			TwoColumnMenuItem item = sender as TwoColumnMenuItem;
			MakeMovedText(item.Destination, item.Source);

		}

		/// <summary>
		/// One of the four basic operations we use to implement moving things. This one moves ccas from one column
		/// to another. Caller is responsible to ensure this is legitimate (i.e., will not make text out
		/// of order). (However, with appropriate safeguards it is OK with a PropChanged on the row's AppliesTo property.
		/// </summary>
		/// <param name="ccasToMove"></param>
		/// <param name="hvoNewCol"></param>
		/// <param name="row"></param>
		public virtual void ChangeColumn(ICmAnnotation[] ccasToMove,
			int hvoNewCol, ICmIndirectAnnotation row)
		{
			// We don't expect these strings to be seen...calling routine should have its own undo action.
			using (IUndoRedoTaskHelper helper = GetUndoHelper("Undo change column", "Redo change column"))
			{
				Debug.Assert(row.AppliesToRS.Contains(ccasToMove[0]));
				int chvo = row.AppliesToRS.Count;
				using (new ExtraPropChangedInserter(m_cache.MainCacheAccessor, row.Hvo, kflidAppliesTo, 0,
						chvo, chvo))
					ChangeColumnInternal(ccasToMove, hvoNewCol);
			}
		}

		/// <summary>
		/// Implementation of ChangeColumn, but not responsible for PropChanged messages.
		/// Can also be used as part of a move from end of one row to oppposite end of another.
		/// protected virtual only for override in testing.
		/// </summary>
		/// <param name="ccasToMove"></param>
		/// <param name="hvoNewCol"></param>
		protected internal virtual void ChangeColumnInternal(ICmAnnotation[] ccasToMove, int hvoNewCol)
		{
			foreach (ICmAnnotation cca in ccasToMove)
				cca.InstanceOfRAHvo = hvoNewCol;
		}

		/// <summary>
		/// One of the basic move operations. Moves ccas from one row to another. Caller is responsible
		/// to ensure this is legitimate (i.e., will not make text out of order).
		/// </summary>
		/// <param name="ccasToMove"></param>
		/// <param name="rowSrc"></param>
		/// <param name="rowDst"></param>
		/// <param name="srcIndex">AppliesTo index</param>
		/// <param name="dstIndex">AppliesTo index</param>
		public virtual void ChangeRow(int[] ccasToMove, ICmIndirectAnnotation rowSrc, ICmIndirectAnnotation rowDst,
			int srcIndex, int dstIndex)
		{
			MoveIndirectAnnotationChildren(ccasToMove, rowSrc, rowDst, srcIndex, dstIndex);
		}

		/// <summary>
		/// One of the basic move operations. Moves children from AppliesTo of one indirect annotation to another. Caller is responsible
		/// to ensure this is legitimate (i.e., will not make text out of order).
		/// </summary>
		/// <param name="ccasToMove"></param>
		/// <param name="rowSrc"></param>
		/// <param name="rowDst"></param>
		/// <param name="srcIndex">AppliesTo index</param>
		/// <param name="dstIndex">AppliesTo index</param>
		private void MoveIndirectAnnotationChildren(int[] ccasToMove, ICmIndirectAnnotation ciaSrc, ICmIndirectAnnotation ciaDst,
			int srcIndex, int dstIndex)
		{
			// We don't expect these strings to be seen...calling routine should have its own undo action.
			using (IUndoRedoTaskHelper helper = GetUndoHelper("Undo move", "Redo move"))
			{
				Debug.Assert(ciaSrc.AppliesToRS[srcIndex].Hvo == ccasToMove[0]);
				// Delete them from the source...
				DeleteRefsFromAppliesTo(ccasToMove.Length, ciaSrc, srcIndex);
				// And add them to the destination.
				m_cache.ReplaceReferenceProperty(ciaDst.Hvo, kflidAppliesTo,
					dstIndex, dstIndex, ref ccasToMove);
			}
		}

		private void DeleteRefsFromAppliesTo(int count, ICmIndirectAnnotation rowSrc, int srcIndex)
		{
			int[] empty = new int[0];
			m_cache.ReplaceReferenceProperty(rowSrc.Hvo, kflidAppliesTo,
				srcIndex, srcIndex + count, ref empty);
		}


		/// <summary>
		/// Basic move operation, moves stuff from one CCA to another. Caller is responsible for validity.
		/// </summary>
		/// <param name="wficsToMove"></param>
		/// <param name="ccaSrc"></param>
		/// <param name="ccaDst"></param>
		/// <param name="srcIndex">AppliesTo index</param>
		/// <param name="dstIndex">AppliesTo index</param>
		public virtual void ChangeCca(int[] wficsToMove, ICmIndirectAnnotation ccaSrc,
			ICmIndirectAnnotation ccaDst, int srcIndex, int dstIndex)
		{
			MoveIndirectAnnotationChildren(wficsToMove, ccaSrc, ccaDst, srcIndex, dstIndex);
		}

		/// <summary>
		/// Delete a specified group of CCAs (from the row and also from the database).
		/// Mainly made a method for simplified testing of callers.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="ihvo">in AppliesTo</param>
		/// <param name="chvo"></param>
		public virtual void DeleteCcas(ICmIndirectAnnotation row, int ihvo, int chvo)
		{
			// We don't expect these strings to be seen...calling routine should have its own undo action.
			using (IUndoRedoTaskHelper helper = GetUndoHelper("Undo delete CCAs", "Redo delete CCAs"))
			{
				Set<int> ccasToDelete = new Set<int>(chvo);
				for (int i = ihvo; i < ihvo + chvo; i++)
				{
					ccasToDelete.Add(row.AppliesToRS[ihvo].Hvo);
				}
				DeleteRefsFromAppliesTo(chvo, row, ihvo); // make sure the memory copy gets cleaned up.
				CmObject.DeleteObjects(ccasToDelete, Cache, false);
			}
		}

		/// <summary>
		/// This is virtual so a subclass used as a test spy can avoid making a real one, but verify that it
		/// gets done.
		/// </summary>
		/// <param name="undoText"></param>
		/// <param name="redoText"></param>
		/// <returns></returns>
		protected internal virtual UndoRedoTaskHelper GetUndoHelper(string undoText, string redoText)
		{
			return new UndoRedoTaskHelper(m_cache, undoText, redoText);
		}

		/// <summary>
		/// Merge the contents of the source cell into the destination cell (at the start if forward is true,
		/// otherwise at the end).
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
		/// Given a list of ccas (typically in a single cell), find the last one (if any) that actually
		/// contains wfics. Since we are now going to have the possibility of more than one data CCA per cell
		/// we are trying a (faster?) different way of doing this.
		/// </summary>
		/// <param name="ccasInCell"></param>
		/// <returns></returns>
		protected internal ICmIndirectAnnotation FindLastCcaWithWfics(List<ICmAnnotation> ccasInCell)
		{
			List<ICmAnnotation> temp = ccasInCell.GetRange(0, ccasInCell.Count); // Make a shallow copy to avoid side-effects.
			temp.Reverse(); // returns 'void', can't embed in line above or below.
			return FindFirstCcaWithWfics(temp);
		}

		/// <summary>
		/// Given a list of ccas (typically in a single cell), find the first one (if any) that actually
		/// contains wfics.
		/// </summary>
		/// <param name="ccasInCell"></param>
		/// <returns></returns>
		protected internal ICmIndirectAnnotation FindFirstCcaWithWfics(List<ICmAnnotation> ccasInCell)
		{
			foreach (ICmAnnotation cca in ccasInCell)
			{
				if (IsWficGroup(Cache, cca.Hvo))
					return cca as ICmIndirectAnnotation;
			}
			return null;
		}

		/// <summary>
		/// Given a list of ccas (typically in a single cell), collect a list of all the ones
		/// that actually contains wfics.
		/// </summary>
		/// <param name="ccasInCell"></param>
		/// <returns></returns>
		protected internal List<int> CollectAllDataCcas(List<ICmAnnotation> ccasInCell)
		{
			List<int> result = new List<int>();
			foreach (ICmAnnotation cca in ccasInCell)
			{
				if (IsWficGroup(Cache, cca.Hvo))
					result.Add((cca as ICmIndirectAnnotation).Hvo);
			}
			return result;
		}

		/// <summary>
		/// Given a list of ccas (typically in a single cell) and the index of the one we're on,
		/// find the next one in the list that actually contains wfics.
		/// Returns null if no data CCA (Wfic-bearing) is found.
		/// </summary>
		/// <param name="ccasInCell"></param>
		/// <param name="iStartCca">The index of the current data CCA, a starting point.
		/// Index is relative to list, NOT to row annotation AppliesTo!</param>
		/// <param name="indexFoundAt">The index of the next data CCA, if found. Value will be -1, if not found.
		/// Index is relative to list, NOT to row annotation AppliesTo!</param>
		/// <returns></returns>
		protected internal ICmIndirectAnnotation FindNextCcaWithWfics(List<ICmAnnotation> ccasInCell, int iStartCca,
			out int indexFoundAt)
		{
			indexFoundAt = -1;
			for (int i = iStartCca + 1; i < ccasInCell.Count; i++)
			{
				if (IsWficGroup(Cache, ccasInCell[i].Hvo))
				{
					indexFoundAt = i;
					return ccasInCell[i] as ICmIndirectAnnotation;
				}
			}
			return null;
		}

		/// <summary>
		/// Given a list of CCAs (typically in a single cell), find the next one in the list that actually contains wfics.
		/// This overload starts at the beginning of the list of CCAs.
		/// The out var returns the index within the list (NOT within the row).
		/// Returns null if no data CCA (Wfic-bearing) is found.
		/// </summary>
		/// <param name="ccasInCell"></param>
		/// <param name="indexFoundAt"></param>
		/// <returns></returns>
		protected internal ICmIndirectAnnotation FindNextCcaWithWfics(List<ICmAnnotation> ccasInCell, out int indexFoundAt)
		{
			int iAtStart = -1;
			return FindNextCcaWithWfics(ccasInCell, iAtStart, out indexFoundAt);
		}

		/// <summary>
		/// Given a list of ccas (typically in a single cell) and the index of the one we're on,
		/// find the previous one in the list (if any) that actually contains wfics.
		/// </summary>
		/// <param name="ccasInCell"></param>
		/// <param name="icurrCca">routine updates icurrCca to index of previous data CCA</param>
		/// <returns></returns>
		protected internal ICmIndirectAnnotation FindPreviousCcaWithWfics(List<ICmAnnotation> ccasInCell, ref int icurrCca)
		{
			for (int i = icurrCca - 1; i > -1; i--)
			{
				if (IsWficGroup(Cache, ccasInCell[i].Hvo))
				{
					icurrCca = i;
					return ccasInCell[i] as ICmIndirectAnnotation;
				}
			}
			icurrCca = -1;
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
			using (GetUndoHelper(DiscourseStrings.ksUndoMoveCellBack, DiscourseStrings.ksRedoMoveCellBack))
			{
				ChartLocation prevCell;
				List<ICmAnnotation> ccasToMove = CcasInCell(srcCell);
				if (srcCell.ColIndex == 0)
				{
					// Move to start of previous row.
					ICmIndirectAnnotation prevRow = PreviousRow(srcCell.RowAnn);
					if (prevRow == null)
						return; // can't go back further.
					prevCell = new ChartLocation(AllMyColumns.Length - 1,prevRow);
				}
				else
				{
					// Normal case, merging/moving to cell on same row.
					prevCell = new ChartLocation(srcCell.ColIndex - 1, srcCell.RowAnn);
				}
				MergeCellContents(srcCell, prevCell, false);
			}
		}

		ICmIndirectAnnotation PreviousRow(ICmIndirectAnnotation row)
		{
			int index = IndexOfRow(row.Hvo);
			if (index == 0)
				return null;
			return Chart.RowsRS[index - 1];
		}

		ICmIndirectAnnotation NextRow(ICmIndirectAnnotation row)
		{
			int index = IndexOfRow(row.Hvo);
			if (index >= Chart.RowsRS.Count - 1)
				return null;
			return Chart.RowsRS[index + 1];
		}

		/// <summary>
		/// Move a cell's contents forward (typically right). If the next cell is empty, just change columns.
		/// If the next cell is occupied, merge the contents. Wraps around in last column. Does nothing in
		/// very last cell.
		/// </summary>
		/// <param name="srcCell"></param>
		public void MoveCellForward(ChartLocation srcCell)
		{
			using (GetUndoHelper(DiscourseStrings.ksUndoMoveCellForward, DiscourseStrings.ksRedoMoveCellForward))
			{
				ChartLocation dstCell;
				List<ICmAnnotation> ccasToMove = CcasInCell(srcCell);
				if (srcCell.ColIndex == AllMyColumns.Length - 1)
				{
					// Move to start of next row.
					ICmIndirectAnnotation nextRow = NextRow(srcCell.RowAnn);
					if (nextRow == null)
						return; // can't move further.
					dstCell = new ChartLocation(0, nextRow);
				}
				else
				{
					// Normal case, merging/moving to cell on same row.
					dstCell = new ChartLocation(srcCell.ColIndex + 1, srcCell.RowAnn);
				}
				MergeCellContents(srcCell, dstCell, true);
			}
		}

		internal void DeleteRowIfEmpty(ICmIndirectAnnotation row, Set<int> hvosToDelete)
		{
			if (row.AppliesToRS.Count == 0) // yep, it's empty
			{
				int irow = IndexOfRow(row.Hvo);

				m_chart.RowsRS.Remove(row);
				CheckForAndFixAffectedClauseMrkrs(row, hvosToDelete);
				int crow = m_chart.RowsRS.Count;
				hvosToDelete.Add(row.Hvo);
				irow = DecrementRowSafely(irow);
				RenumberRows(irow, false);
				Cache.PropChanged(m_chart.Hvo, kflidRows, irow, crow - irow, crow - irow + 1);
			}
		}

		/// <summary>
		/// Decrement a row index without going negative.
		/// </summary>
		/// <param name="irow"></param>
		/// <returns></returns>
		private static int DecrementRowSafely(int irow)
		{
			return Math.Max(0, irow - 1);
		}

		/// <summary>
		/// Scans entire chart looking for clause markers refering to this row (delRow).
		/// If a marker refers ONLY to this row, it gets deleted (added to hvosToDelete).
		/// Otherwise, it gets modified to eliminate this row from its reference.
		/// </summary>
		/// <param name="delRow">Row that is about to be deleted and has occasioned this search.</param>
		/// <param name="hvosToDelete"></param>
		internal void CheckForAndFixAffectedClauseMrkrs(ICmIndirectAnnotation delRow, Set<int> hvosToDelete)
		{
			foreach (ICmIndirectAnnotation ccr in Chart.RowsRS)
			{
				// We can use 'foreach' here because any clause marker to be deleted will only be
				// added to hvosToDelete.
				foreach (ICmAnnotation cca in ccr.AppliesToRS)
				{
					if (IsClauseMarkerAndRefersToMe(cca, delRow))
					{
						ICmIndirectAnnotation cca1 = cca as ICmIndirectAnnotation;
						if (cca1.AppliesToRS.Count == 1) // If there's only 1, it has to be 'delRow'.
						{
							ccr.AppliesToRS.Remove(cca1.Hvo);
							hvosToDelete.Add(cca1.Hvo);
							DeleteRowIfEmpty(ccr, hvosToDelete); // It's just possible!
							continue;
						}
						FixAffectedClauseMarker(cca1, delRow);
					}
				}
			}
		}

		/// <summary>
		/// Fixes features and Comments in dependent clauses in preparation for deleting a row.
		/// Caller guarantees that DepClMarker 'cca' points to more than one row, since Markers that only point
		/// to one row will be deleted elsewhere.
		/// </summary>
		/// <param name="cca"></param>
		/// <param name="delRow"></param>
		private void FixAffectedClauseMarker(ICmIndirectAnnotation cca, ICmIndirectAnnotation delRow)
		{
			// Enhance GordonM: This is another place that will need to change in the unlikely event that
			// dependent clauses can someday be non-contiguous.
			int[] hvoDepClauses = cca.AppliesToRS.HvoArray;
			int iArrayMax = hvoDepClauses.Length - 1;
			int iMinRow = IndexOfRow(hvoDepClauses[0]);
			int iMaxRow = IndexOfRow(hvoDepClauses[iArrayMax]);
			// Of the 2 following conditionals, only one should match, if any.
			// If delRow was the first reference in cca, move the firstDep feature to the next row in the list
			if (hvoDepClauses[0] == delRow.Hvo)
			{
				MoveDepFeature(hvoDepClauses[1], true);
				iMinRow++;
			}
			// If delRow was the last reference in cca, move the endDep feature to the previous row in the list
			if (hvoDepClauses[iArrayMax] == delRow.Hvo)
			{
				MoveDepFeature(hvoDepClauses[iArrayMax - 1], false);
				iMaxRow--;
			}
			// Remove 'delRow' from ClauseMarker's list of references
			cca.AppliesToRS.Remove(delRow);
		}

		private void MoveDepFeature(int hvoRow, bool fStart)
		{
			string StartOrEndGroup = fStart ? StartDepClauseGroup : EndDepClauseGroup;
			SetFeature(Cache.MainCacheAccessor, hvoRow, StartOrEndGroup, true);
		}

		/// <summary>
		/// Returns true if 'cca' is a clause placeholder that contains 'row' in its list of rows referenced.
		/// </summary>
		/// <param name="cca"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		private bool IsClauseMarkerAndRefersToMe(ICmAnnotation cca, ICmIndirectAnnotation row)
		{
			if (cca == null || row == null || !IsClausePlaceholder(cca.Hvo))
				return false;
			// Above test 'IsClausePlaceholder' verifies that its parameter is an Indirect Annotation.
			return (cca as ICmIndirectAnnotation).AppliesToRS.Contains(row);
		}

		/// <summary>
		/// Move the (first) word in a cell back (typically left). If the previous cell is empty, make a new
		/// CCA there; otherwise, move it into the CCA in the cell right. In the first column, moves to previous row.
		/// If there is only one word in the cell, merge everything into the destination.
		/// </summary>
		/// <param name="srcCell"></param>
		public void MoveWordBack(ChartLocation srcCell)
		{
			using (GetUndoHelper(DiscourseStrings.ksUndoMoveWord, DiscourseStrings.ksRedoMoveWord))
			{
				ChartLocation dstCell;
				List<ICmAnnotation> ccaList = CcasInCell(srcCell);
				int iccaInCell;
				// Start looking at the beginning of the cell
				ICmIndirectAnnotation ccaWfics = FindNextCcaWithWfics(ccaList, out iccaInCell);
				if (ccaWfics == null)
					return;
				if (!TryGetPreviousCell(srcCell, out dstCell))
					return;
				// If there's only one Wfic in the CCA and no other data CCAs, just merge the cell contents.
				// N.B. If the first data CCA is not the first CCA in the cell, we can't use "1" below!!!!
				if (ccaWfics.AppliesToRS.Count == 1 &&
					(ccaList.Count == iccaInCell + 1 || FindFirstCcaWithWfics(ccaList.GetRange(iccaInCell + 1, ccaList.Count - 1)) == null))
				{
					MergeCellContents(srcCell, dstCell, false);
					return;
				}

				// If the destination contains a missing marker get rid of it! Don't try to 'merge' with it.
				RemoveMissingMarker(dstCell);

				ICmIndirectAnnotation ccaTarget = FindLastCcaWithWfics(CcasInCell(dstCell));
				int[] wficToMove = new int[] { ccaWfics.AppliesToRS[0].Hvo };
				if (ccaTarget == null)
				{
					// Make a new CCA and move one wfic
					MakeCca(dstCell, wficToMove, FindIndexOfCcaInLaterColumn(dstCell), null);
					ccaWfics.AppliesToRS.RemoveAt(0);
				}
				else
				{
					MoveIndirectAnnotationChildren(wficToMove, ccaWfics, ccaTarget, 0, ccaTarget.AppliesToRS.Count);
				}
				// Enhance GordonM: If we eventually allow a marker to show that words within a cell are reversed in order,
				// that marker may need deleting here.
				DeleteCcaIfEmpty(srcCell.RowAnn, ccaWfics);
				m_lastMoveCell = dstCell;
			}
		}

		/// <summary>
		/// Move the (last) word in a cell forward (typically right). If the next cell is empty, make a new
		/// CCA there; otherwise, move it into the CCA in the cell right. In the last column, it moves to
		/// the next row. If there is only one word in the cell, merge everything into the destination.
		/// </summary>
		/// <param name="srcCell"></param>
		public void MoveWordForward(ChartLocation srcCell)
		{
			using (GetUndoHelper(DiscourseStrings.ksUndoMoveWord, DiscourseStrings.ksRedoMoveWord))
			{
				ChartLocation dstCell;
				List<ICmAnnotation> ccaList = CcasInCell(srcCell);
				int iccaInCell = ccaList.Count; // start at end of cell and work backwards
				ICmIndirectAnnotation ccaWfics = FindPreviousCcaWithWfics(ccaList, ref iccaInCell);
				if (ccaWfics == null)
					return;
				if(!TryGetNextCell(srcCell, out dstCell))
					return;
				// If there's only one Wfic in the CCA and no other data CCAs, just merge the cell contents.
				if (ccaWfics.AppliesToRS.Count == 1 &&
					(ccaList.Count == 1 || (iccaInCell > 0 && FindLastCcaWithWfics(ccaList.GetRange(0, iccaInCell - 1)) == null)))
				{
					MergeCellContents(srcCell, dstCell, true);
					return;
				}

				// If the destination contains a missing marker get rid of it! Don't try to 'merge' with it.
				RemoveMissingMarker(dstCell);

				List<ICmAnnotation> ccasInDestCell = CcasInCell(dstCell);
				ICmIndirectAnnotation ccaTarget = FindFirstCcaWithWfics(ccasInDestCell);
				int ihvoLast = ccaWfics.AppliesToRS.Count - 1;
				int[] wficToMove = new int[] { ccaWfics.AppliesToRS[ihvoLast].Hvo };
				if (ccaTarget == null)
				{
					int iinsertAt = FindIndexOfFirstCcaInOrAfterColumn(dstCell);
					// If there is a preposed marker in the same column at this index, increment index.
					if (ccasInDestCell.Count > 0 && IsPreposedMarker(ccasInDestCell[0]))
						iinsertAt++;
					// Make a new CCA and move one wfic
					MakeCca(dstCell, wficToMove, iinsertAt, null);
					ccaWfics.AppliesToRS.RemoveAt(ihvoLast);
				}
				else
				{
					MoveIndirectAnnotationChildren(wficToMove, ccaWfics, ccaTarget, ihvoLast, 0);
				}
				// Enhance GordonM: If we eventually allow a marker to show that words within a cell are reversed in order,
				// that marker may need deleting here.
				DeleteCcaIfEmpty(srcCell.RowAnn, ccaWfics);
				m_lastMoveCell = dstCell;
			}
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
			int icolDst = srcCell.ColIndex + 1;
			ICmIndirectAnnotation rowDst = srcCell.RowAnn;
			if (icolDst >= AllMyColumns.Length)
			{
				icolDst = 0;
				rowDst = NextRow(srcCell.RowAnn);
			}
			dstCell = new ChartLocation(icolDst, rowDst);
			return rowDst != null;
		}

		private bool TryGetPreviousCell(ChartLocation srcCell, out ChartLocation dstCell)
		{
			int icolDst = srcCell.ColIndex - 1;
			ICmIndirectAnnotation rowDst = srcCell.RowAnn;
			if (icolDst < 0)
			{
				icolDst = m_allMyColumns.Length - 1;
				rowDst = PreviousRow(srcCell.RowAnn);
			}
			dstCell = new ChartLocation(icolDst, rowDst);
			return rowDst != null;
		}

		private void DeleteCcaIfEmpty(ICmIndirectAnnotation row, ICmIndirectAnnotation ccaWfics)
		{
			if (ccaWfics.AppliesToRS.Count == 0)
			{
				// delete the old CCA, which is now empty
				row.AppliesToRS.Remove(ccaWfics);
				Cache.MainCacheAccessor.DeleteObj(ccaWfics.Hvo);
			}
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

		internal ListView MakeHeaderGroups()
		{
			ListView result = new ListView();

			return result;
		}

		internal void MakeMainHeaderCols(ListView view)
		{
			view.SuspendLayout();
			view.Columns.Clear();
			ColumnHeader ch = new ColumnHeader();
			ch.Text = ""; // otherwise default is 'column header'!
			view.Columns.Add(ch); // for row numbers column.
			foreach (int hvoCol in AllMyColumns)
			{
				ch = new ColumnHeader();
				ch.Text = CmPossibility.BestAnalysisName(Cache, hvoCol).Text.Normalize();	// ensure NFC -- See LT-8815.
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
		/// <param name="p"></param>
		/// <param name="positions"></param>
		/// <returns></returns>
		public int GetColumnFromPosition(int x, int[] positions)
		{
			for (int i = 1; i < positions.Length; i++)
			{
				if (positions[i] > x)
				{
					return i - 1;
				}
			}
			return positions.Length - 1;
		}


		/// <summary>
		/// Answer true if the HVO is a CmIndirectAnnotation which represents a placeholder
		/// for a dependent clause (or speech or song).
		/// This is determined by checking its class, then that it AppliesTo at least one
		/// CmIndirectAnnotation that is a ConstituentChartRow.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="hvoItem">the first clause</param>
		/// <returns></returns>
		internal bool IsClausePlaceholder(int hvo, out int hvoItem)
		{
			return AnnTypeOfFirstAppliesTo(Cache, hvo, CmIndirectAnnotation.kclsidCmIndirectAnnotation, out hvoItem)
				== CmAnnotationDefn.ConstituentChartRow(m_cache).Hvo;
		}

		internal bool IsClausePlaceholder(int hvo)
		{
			int dummy;
			return IsClausePlaceholder(hvo, out dummy);
		}

		/// <summary>
		/// Answer true if the HVO is a CmIndirectAnnotation which represents a group of Wfics.
		/// This is determined by checking its class, then that it AppliesTo at least one
		/// CmBaseAnnotation that is a (T)Wfic.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		internal static bool IsWficGroup(FdoCache cache, int hvo)
		{
			int hvoDummy;
			return AnnTypeOfFirstAppliesTo(cache, hvo, CmBaseAnnotation.kclsidCmBaseAnnotation, out hvoDummy)
				== CmAnnotationDefn.Twfic(cache).Hvo;
		}

		/// <summary>
		/// If the hvo is a CmIndirectAnnotation that AppliesTo at least one thing,
		/// and the first thing is a CmAnnotation of the expected subtype, return the annotation type of
		/// that first thing.
		/// Otherwise return zero.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="expectedClsid"></param>
		/// <param name="hvoItem">the AppliesToItem itself</param>
		/// <returns></returns>
		private static int AnnTypeOfFirstAppliesTo(FdoCache cache, int hvo, int expectedClsid, out int hvoItem)
		{
			hvoItem = 0;
			int clsid = cache.GetClassOfObject(hvo);
			if (clsid != CmIndirectAnnotation.kclsidCmIndirectAnnotation)
				return 0; // Can't be what we're looking for.
			if (cache.GetVectorSize(hvo, kflidAppliesTo) < 1)
				return 0; // no AppliesTo, can't return a type
			hvoItem = cache.GetVectorItem(hvo, kflidAppliesTo, 0);
			clsid = cache.GetClassOfObject(hvoItem);
			if (clsid != expectedClsid)
				return 0; // not the right kind of annotations
			return cache.GetObjProperty(hvoItem, kflidAnnotationType);
		}

		/// <summary>
		/// Gets bookmarkable annotation from the ribbon.
		/// </summary>
		/// <returns>hvo to bookmark or 0 to do nothing.</returns>
		internal int GetUnchartedAnnForBookmark()
		{
			if (m_chart == null || m_chart.RowsRS.Count == 0)
				return 0; // No chart! Don't want to change any bookmark that might already be set.

			// If we aren't done charting the text yet, we need to set the bookmark
			// to the first uncharted annotation in the ribbon
			int[] hvoArray = NextUnchartedInput(1);
			if (hvoArray.Length > 0)
				return hvoArray[0]; // return hvo of annotation to bookmark
			// Do nothing for now, since the chart has been fully charted.
			// Enhance GordonM: Can we figure out what part of the chart is selected and save that?
			return 0;
		}

		/// <summary>
		/// Answer true if hvoCca is a moved text item, by asking if it has the right feature.
		/// </summary>
		/// <param name="ccas"></param>
		/// <returns></returns>
		static internal bool IsMovedText(ISilDataAccess sda, int hvoCca)
		{
			return GetFeature(sda, hvoCca, MovedTextFeatureName);
		}

		/// <summary>
		/// Return a suitable style tag depending on whether hvo is the first or later
		/// moved text item in its row.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		internal string MovedTextTag(int hvoTarget)
		{
			if (HasPreviousMovedItemOnLine(m_chart, hvoTarget))
				return "movedText2";
			else
				return "movedText";
		}

		static internal bool HasPreviousMovedItemOnLine(IDsConstChart chart, int hvoTarget)
		{
			ISilDataAccess sda = chart.Cache.MainCacheAccessor;
			foreach (ICmIndirectAnnotation row in chart.RowsRS)
			{
				int cPrevMovedText = 0;
				foreach (int hvoCca in row.AppliesToRS.HvoArray)
				{
					if (hvoCca == hvoTarget)
						return cPrevMovedText == 0 ? false : true;
					if (IsMovedText(sda, hvoCca))
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
					else
					{
						if (irow == irowPrec && icol >= icolPrec)
							return true;
						else if (irow == irowFoll && icol <= icolFoll)
							return true;
					}
				}
			}
			return false;
		}
	}

	/// <summary>
	/// This is used for a slice to ask the data tree to display a context menu.
	/// </summary>
	public delegate void RowModifiedEventHandler (object sender, RowModifiedEventArgs e);

	public class RowModifiedEventArgs : EventArgs
	{
		ICmIndirectAnnotation m_row;
		public RowModifiedEventArgs(ICmIndirectAnnotation row)
		{
			m_row = row;
		}

		public ICmIndirectAnnotation Row
		{
			get { return m_row; }
		}
	}

	class DepClauseMenuItem : ToolStripMenuItem
	{
		ChartLocation m_srcCell;
		ICmIndirectAnnotation[] m_depClauses;
		string m_depType;

		public DepClauseMenuItem(string label, ChartLocation srcCell, ICmIndirectAnnotation[] depClauses)
			: base(label)
		{
			m_depClauses = depClauses;
			m_srcCell = srcCell;
		}

		public ICmIndirectAnnotation RowSource
		{
			get { return m_srcCell.RowAnn; }
		}

		public int HvoRow
		{
			get { return m_srcCell.HvoRow; }
		}

		public ICmIndirectAnnotation[] DepClauses
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

		public string DepType
		{
			get { return m_depType; }
			set { m_depType = value; }
		}
	}

	internal class RowColMenuItem : ToolStripMenuItem
	{
		ChartLocation m_srcCell;

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
		/// The row annotation.
		/// </summary>
		public ICmIndirectAnnotation SrcRow
		{
			get { return m_srcCell.RowAnn; }
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
		ICmIndirectAnnotation m_row;

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
		public TwoColumnMenuItem(string label, int colDst, int colSrc, ICmIndirectAnnotation row)
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
		public ICmIndirectAnnotation Row
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
		ConstituentChartLogic m_logic;
		ChartLocation m_srcCell;
		ChartLocation m_dstCell;
		bool m_forward;
		List<ICmAnnotation> m_ccasSrc;
		List<ICmAnnotation> m_ccasDest;
		List<ICmAnnotation> m_dataCcasSrc;
		List<ICmAnnotation> m_dataCcasDest;
		ICmIndirectAnnotation m_ccaWordsToMerge;
		ICmIndirectAnnotation m_ccaWordsToMergeWith;
		Set<int> m_hvosToDelete = new Set<int>();

		public MergeCellContentsMethod(ConstituentChartLogic logic, ChartLocation srcCell, ChartLocation dstCell, bool forward)
		{
			m_logic = logic;
			m_srcCell = srcCell;
			m_dstCell = dstCell;
			m_forward = forward;
		}

		FdoCache Cache
		{
			get { return m_logic.Cache; }
		}

		DsConstChart Chart
		{
			get { return m_logic.Chart; }
		}

		ICmIndirectAnnotation SrcRow
		{
			get { return m_srcCell.RowAnn; }
		}

		int HvoSrcRow
		{
			get { return m_srcCell.HvoRow; }
		}

		int SrcColIndex
		{
			get { return m_srcCell.ColIndex; }
		}

		ICmIndirectAnnotation DstRow
		{
			get { return m_dstCell.RowAnn; }
		}

		int HvoDstRow
		{
			get { return m_dstCell.HvoRow; }
		}

		int DstColIndex
		{
			get { return m_dstCell.ColIndex; }
		}

		internal static int[] HvoArrayFromList(List<ICmAnnotation> src)
		{
			int[] result = new int[src.Count];
			for (int i = 0; i < result.Length; i++)
				result[i] = src[i].Hvo;
			return result;
		}

		const int kflidAppliesTo = (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo;

		/// <summary>
		/// Remove the CCA from ccasToMerge (and its Row).
		/// </summary>
		/// <param name="cca"></param>
		/// <param name="fDeleteIt">If true, add it to hvosToDelete for eventual deletion.</param>
		/// <param name="indexDest">keeps track of Destination insertion point</param>
		void RemoveSourceCca(ICmAnnotation cca, bool fDeleteIt, ref int indexDest)
		{
			int hvoItem = cca.Hvo;
			if (fDeleteIt)
				m_hvosToDelete.Add(hvoItem);
			SrcRow.AppliesToRS.Remove(hvoItem);
			m_ccasSrc.Remove(cca);
			m_dataCcasSrc.Remove(cca);
			if ((HvoDstRow == HvoSrcRow) && m_forward)
				indexDest--; // To compensate for lost item(CCA) in row.AppliesTo
		}

		/// <summary>
		/// Remove the CCA from ccasToMergeWith (and its Row).
		/// </summary>
		/// <param name="cca"></param>
		/// <param name="fDeleteIt">If true, add it to hvosToDelete for eventual deletion.</param>
		/// <param name="indexDest">keeps track of Destination insertion point</param>
		void RemoveRedundantMTMarker(ICmAnnotation cca, bool fDeleteIt, ref int indexDest)
		{
			int hvoItem = cca.Hvo;
			if (fDeleteIt)
				m_hvosToDelete.Add(hvoItem);
			DstRow.AppliesToRS.Remove(hvoItem);
			m_ccasDest.Remove(cca);
			if ((HvoDstRow == HvoSrcRow) && m_forward)
				indexDest--; // To compensate for lost item(CCA) in row.AppliesTo; is a preposed marker
		}

		internal void Run()
		{
			using (IUndoRedoTaskHelper helper = m_logic.GetUndoHelper("undo merge cells", "redo merge cells"))
			{
				// Get markers and data CCAs from source and verify that source cell has some data.
				m_ccasSrc = m_logic.CcasInCell(m_srcCell);
				m_dataCcasSrc = m_logic.CollectDataCcas(m_ccasSrc);
				//Debug.Assert(dataCcasSrc.Count != 0, "Move Cell should not be an active option if no source dataCCA exists.");
				if (m_dataCcasSrc.Count == 0)
					return; // no words to move!!

				// If the destination contains a missing marker get rid of it! Don't try to 'merge' with it.
				m_logic.RemoveMissingMarker(m_dstCell);

				// Get markers and data CCAs from destination
				int indexDest; // This will keep track of where we are in rowDst.AppliesTo
				m_ccasDest = m_logic.CcasInCell(m_dstCell, out indexDest);
				// indexDest is now the index in the destination row's AppliesTo of the first CCA in the destination cell.
				m_dataCcasDest = m_logic.CollectDataCcas(m_ccasDest);

				// Here is where we need to check to see if the destination cell contains any movedText markers
				// for text in the source cell. If it does, the marker goes away, the movedText feature goes away,
				// and that MAY leave us ready to do a PreMerge in the source cell prior to merging the cells.
				if (CheckForRedundantMTMarkers(ref indexDest))
				{
					indexDest = TryPreMerge(indexDest);
				}

				// The above check for MTMarkers may cause this 'if' to be true, but we may still need to delete objects
				if (m_ccasDest.Count == 0)
				{
					// The destination is completely empty. Just move the dataCcasSrc.
					if (HvoSrcRow == HvoDstRow)
					{
						// This is where we worry about reordering SrcRow.AppliesTo if other annotations exist between
						// the dataCcasSrc and the destination (since non-data stuff doesn't move).
						if (m_dataCcasSrc.Count != m_ccasSrc.Count && m_forward)
						{
							MoveDataCcasToEndOfCell(indexDest); // in preparation to moving data to next cell
						}
						m_logic.ChangeColumn(m_dataCcasSrc.ToArray(), m_logic.AllMyColumns[DstColIndex],
								SrcRow);
					}
					else
					{
						if (SrcColIndex != DstColIndex)
							m_logic.ChangeColumnInternal(m_dataCcasSrc.ToArray(), m_logic.AllMyColumns[DstColIndex]);

						// Enhance GordonM: If we ever allow markers between dataCCAs, this [& ChangeRow()] will need to change.
						MoveCcasToDestRow(indexDest);
						DeleteRowIfEmpty(SrcRow);
					}
					if (m_hvosToDelete.Count > 0)
						CmObject.DeleteObjects(m_hvosToDelete, Cache, false);
					return;
				}

				if (m_logic.IsPreposedMarker(m_ccasDest[0]))
					indexDest++; // Insertion point should be after preposed marker

				// Set up possible coalescence of CCAs.
				PrepareForMerge();

				// At this point 'ccaWordsToMerge' and 'ccaWordsToMergeWith' are the only data CCAs that might actually
				// coalesce. Neither is guaranteed to be non-null (either one could be movedText, which is not mergeable).
				// If either is null, there will be no coalescence.
				// But there may well be other data CCAs in 'dataCcasSrc' that will need to move.

				if (m_ccaWordsToMerge != null)
				{
					if (m_ccaWordsToMergeWith != null)
					{
						// Merge the word groups and delete the empty one.
						m_logic.ChangeCca(m_ccaWordsToMerge.AppliesToRS.HvoArray, m_ccaWordsToMerge, m_ccaWordsToMergeWith, 0,
							(m_forward ? 0 : m_ccaWordsToMergeWith.AppliesToRS.Count));
						//int ihvoSrc = new List<int>(rowSrc.AppliesToRS.HvoArray).IndexOf(ccaWordsToMerge.Hvo);
						// Below is obsolete. ccaWordsToMerge would be null if the candidate was movedText.
						//CleanUpMovedTextMarkerFor(ccaWordsToMerge); // CCA is now empty, destination will NOT be movedText
						// mark CCA for delete and remove from source lists, keeping accurate destination index
						RemoveSourceCca(m_ccaWordsToMerge as ICmAnnotation, true, ref indexDest);
					}
					else
					{
						// Move the one(s) we have. This is accomplished by just leaving it in
						// the list (now dataCcasSrc).
						// Enhance JohnT: Possibly we may eventually have different rules about
						// where in the destination cell to put it?
					}

				}

				if (m_dataCcasSrc.Count > 0) // Maybe we merged the only one!
				{
					// Change column of any surviving items in dataCcasSrc.
					if (SrcColIndex != DstColIndex)
						m_logic.ChangeColumnInternal(m_dataCcasSrc.ToArray(), m_logic.AllMyColumns[DstColIndex]);

					//if (rowDst.Hvo != rowSrc.Hvo)
					//{ It actually works if the rows are the same too, otherwise we don't move the CCAs within
					// the row's AppliesTo (which we might need to if there are non-data (marker) CCAs
					indexDest = FindWhereToInsert(indexDest); // Needs an accurate 'dataCcasDest'
					MoveCcasToDestRow(indexDest);
					//}
				}

				// Review: what should we do about dependent clause markers pointing at the destination row?

				// If the source row is now empty, delete it.
				DeleteRowIfEmpty(SrcRow);
				CmObject.DeleteObjects(m_hvosToDelete, Cache, false);
			}
		}

		/// <summary>
		/// Moves the data ccas to the end of the source cell in preparation to move them to the next cell.
		/// Solves confusion in row.AppliesTo
		/// Situation: moving forward in same row, nothing in destination cell.
		/// </summary>
		private void MoveDataCcasToEndOfCell(int iDest)
		{
			Debug.Assert(iDest > 0,"Bad destination index.");
			foreach (ICmAnnotation cca in m_dataCcasSrc)
			{
				SrcRow.AppliesToRS.Remove(cca);
				SrcRow.AppliesToRS.InsertAt(cca, iDest - 1);
			}
		}

		private int TryPreMerge(int indexDest)
		{
			// Look through source data CCAs to see if any can be merged now that we've removed
			// a movedText feature prior to moving to a neighboring cell.
			bool flastWasNotMT = false;
			for (int iSrc = 0; iSrc < m_dataCcasSrc.Count; iSrc++)
			{
				ICmIndirectAnnotation currCca = m_dataCcasSrc[iSrc] as ICmIndirectAnnotation;
				if (ConstituentChartLogic.IsMovedText(Cache.MainCacheAccessor, currCca.Hvo))
				{
					flastWasNotMT = false;
					continue;
				}
				if (flastWasNotMT)
				{
					// Merge this CCA with the last one and delete this one
					ICmIndirectAnnotation destCca = m_dataCcasSrc[iSrc - 1] as ICmIndirectAnnotation;
					m_logic.ChangeCca(currCca.AppliesToRS.HvoArray, currCca, destCca, 0, destCca.AppliesToRS.Count);
					// mark CCA for delete and remove from source lists, keeping accurate destination index
					RemoveSourceCca(currCca as ICmAnnotation, true, ref indexDest);
					iSrc--;
				}
				flastWasNotMT = true;
			}
			return indexDest;
		}

		private bool CheckForRedundantMTMarkers(ref int indexDest)
		{
			bool found = false;
			for (int iDes = 0; iDes < m_ccasDest.Count; )
			{
				// Not foreach because we might delete from ccasDest
				ICmAnnotation cca = m_ccasDest[iDes];

				// The source wfic CCA is still in its old row and column, so we can detect this directly.
				if (IsMarkerForCell(cca as ICmIndirectAnnotation, m_srcCell))
				{
					// Turn off feature in source
					TurnOffMovedTextFeatureFromMarker(cca as ICmIndirectAnnotation, SrcRow);
					// Take out movedText marker, keep accurate destination index
					RemoveRedundantMTMarker(cca, true, ref indexDest);
					found = true;
					continue;
				}
				iDes++; // only if we do NOT clobber it.
			}
			return found;
		}

		/// <summary>
		/// Moves all CCAs in dataCcasSrc from rowSrc to rowDst.
		/// </summary>
		/// <param name="indexDest">Index in rowDst.AppliesTo where insertion should occur.</param>
		private void MoveCcasToDestRow(int indexDest)
		{
			foreach (ICmAnnotation cca in m_dataCcasSrc)
			{
				SrcRow.AppliesToRS.Remove(cca);
				if ((HvoSrcRow == HvoDstRow) && m_forward) // potential problem with indexDest in this case
					indexDest--; // compensate for lost item to be inserted later in row
				if (indexDest >= DstRow.AppliesToRS.Count)
					DstRow.AppliesToRS.Append(cca);
				else
					DstRow.AppliesToRS.InsertAt(cca, indexDest);
				indexDest++;
			}
		}

		private void PrepareForMerge()
		{
			// Destination cell has something in it, but it might not be a dataCCA!
			if (m_dataCcasDest.Count == 0)
				m_ccaWordsToMergeWith = null;
			else
			{
				if (m_forward)
					m_ccaWordsToMergeWith = m_dataCcasDest[0] as ICmIndirectAnnotation;
				else
					m_ccaWordsToMergeWith = m_dataCcasDest[m_dataCcasDest.Count - 1] as ICmIndirectAnnotation;
				if (ConstituentChartLogic.IsMovedText(Cache.MainCacheAccessor, m_ccaWordsToMergeWith.Hvo))
					m_ccaWordsToMergeWith = null; // Can't merge with movedText, must append/insert merging CCA instead.
			}

			if (m_forward)
				m_ccaWordsToMerge = m_dataCcasSrc[m_dataCcasSrc.Count - 1] as ICmIndirectAnnotation; // Has to be something here.
			else
				m_ccaWordsToMerge = m_dataCcasSrc[0] as ICmIndirectAnnotation;
			if (ConstituentChartLogic.IsMovedText(Cache.MainCacheAccessor, m_ccaWordsToMerge.Hvo))
				m_ccaWordsToMerge = null; // Can't merge with movedText, must append/insert merging CCA instead.
		}

		/// <summary>
		/// Turns off the movedText feature in the target of this movedText Marker
		/// </summary>
		/// <param name="markerMT"></param>
		/// <param name="row">Row of actual MovedText.</param>
		private void TurnOffMovedTextFeatureFromMarker(ICmIndirectAnnotation markerMT, ICmIndirectAnnotation row)
		{
			Debug.Assert(markerMT != null && markerMT.AppliesToRS.Count == 1, "Bad MovedText Marker");
			int hvoTarget = markerMT.AppliesToRS[0].Hvo;
			// Turn off movedText feature in cell
			int chvo = row.AppliesToRS.Count;
			using (new ExtraPropChangedInserter(Cache.MainCacheAccessor, row.Hvo,
				kflidAppliesTo, 0, chvo, chvo))
			{
				ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, hvoTarget,
					ConstituentChartLogic.MovedTextFeatureName, false);
			}
		}

		/// <summary>
		/// Return true if ciaSrc is a moved-text marker with its destination in the specified cell.
		/// </summary>
		/// <param name="ciaSrc"></param>
		/// <param name="rowDst"></param>
		/// <param name="icolDst"></param>
		/// <returns></returns>
		private bool IsMarkerForCell(ICmIndirectAnnotation ciaSrc, ChartLocation cell)
		{
			// typically it is null if the thing we're testing isn't the right kind of annotation.
			if (ciaSrc == null || ciaSrc.AppliesToRS.Count == 0)
				return false;
			ICmIndirectAnnotation firstTarget = ciaSrc.AppliesToRS[0] as ICmIndirectAnnotation;
			if (firstTarget == null || firstTarget.InstanceOfRAHvo != m_logic.AllMyColumns[cell.ColIndex])
				return false;
			return cell.RowAnn.AppliesToRS.Contains(firstTarget);
		}

		private void DeleteRowIfEmpty(ICmIndirectAnnotation row)
		{
			m_logic.DeleteRowIfEmpty(row, m_hvosToDelete);
		}

		/// <summary>
		/// If we find a moved text marker pointing at 'target', delete it.
		/// </summary>
		/// <param name="target"></param>
		private void CleanUpMovedTextMarkerFor(ICmIndirectAnnotation target)
		{
			ICmIndirectAnnotation row;
			ICmIndirectAnnotation marker = FindMovedTextMarkerFor(target, out row);
			if (marker == null)
				return;
			row.AppliesToRS.Remove(marker);
			DeleteRowIfEmpty(SrcRow);
			m_hvosToDelete.Add(marker.Hvo);
		}

		private ICmIndirectAnnotation FindMovedTextMarkerFor(ICmIndirectAnnotation target, out ICmIndirectAnnotation rowTarget)
		{
			// If we find a moved text marker pointing at 'target', return it and (through the 'out' var) its row.
			// Enhance JohnT: it MIGHT be faster (in long texts) to use a back ref. Or we might limit the
			// search to nearby rows...
			foreach (ICmIndirectAnnotation row in m_logic.Chart.RowsRS)
			{
				foreach (ICmAnnotation cca in row.AppliesToRS)
				{
					ICmIndirectAnnotation cia = cca as ICmIndirectAnnotation;
					if (cia != null && cia.AppliesToRS.Contains(target.Hvo))
					{
						rowTarget = row;
						return cia;
					}
				}
			}
			rowTarget = null;
			return null;
		}

		/// <summary>
		/// Returns index of where in destination row's AppliesTo we want to insert remaining source CCAs.
		/// Uses dataCcasDest list. And forward and rowDest and ccasDest and indexDest.
		/// </summary>
		/// <param name="indexDest">Enters as the index in row.AppliesTo of the beginning of the destination cell.</param>
		/// <returns></returns>
		private int FindWhereToInsert(int indexDest)
		{
			Debug.Assert(0 <= indexDest && indexDest <= DstRow.AppliesToRS.Count);
			if (m_ccasDest.Count == 0)
				return indexDest; // If indexDest == appliesTo.Count, we should take this branch.

			// Enhance GordonM: If we ever allow other markers before the first dataCCA or
			// we allow markers between dataCCAs, this will need to change.
			if (m_forward)
				return indexDest;
			else
				return indexDest + m_dataCcasDest.Count;
		}

		// Check whether the list contains a marker annotation for the specified destination.
		bool ContainsCcaWithSameMarker(List<ICmAnnotation> ccasToMergeWith, int hvoMarker)
		{
			for (int iDst = 0; iDst < ccasToMergeWith.Count; iDst++)
			{
				ICmBaseAnnotation cba = ccasToMergeWith[iDst] as ICmBaseAnnotation;
				if (cba != null && cba.BeginObjectRAHvo == hvoMarker)
				{
					return true;
				}
			}
			return false;
		}
	}

	#endregion // MergeCellContentsMethod

	#region MakeMovedTextMethod

	/// <summary>
	/// Actually this class is used both to Make and Remove MovedText Markers/Features.
	/// </summary>
	class MakeMovedTextMethod
	{
		ConstituentChartLogic m_logic;
		ChartLocation m_movedTextCell;
		ChartLocation m_markerCell;
		bool m_fPrepose;
		int[] m_hvosToMark;

		public MakeMovedTextMethod(ConstituentChartLogic logic, ChartLocation movedTextCell,
			ChartLocation markerCell, int[] hvoWficsToMark)
		{
			m_logic = logic;
			m_movedTextCell = movedTextCell;
			m_markerCell = markerCell;
			m_hvosToMark = hvoWficsToMark; // Empty array implies "mark entire cell(CCA)"
			m_fPrepose = m_logic.IsPreposed(movedTextCell, markerCell);
		}

		#region PropertiesAndConstants

		FdoCache Cache
		{
			get { return m_logic.Cache; }
		}

		ISilDataAccess SDAccess
		{
			get { return m_logic.Cache.MainCacheAccessor; }
		}

		DsConstChart Chart
		{
			get { return m_logic.Chart; }
		}

		ICmIndirectAnnotation MTRow
		{
			get { return m_movedTextCell.RowAnn; }
		}

		int HvoMTRow
		{
			get { return m_movedTextCell.HvoRow; }
		}

		ICmIndirectAnnotation MarkerRow
		{
			get { return m_markerCell.RowAnn; }
		}

		int MarkerColIndex
		{
			get { return m_markerCell.ColIndex; }
		}

		// Flid constants
		const int kflidRows = (int)DsConstChart.DsConstChartTags.kflidRows;

		#endregion

		#region Method Object methods

		/// <summary>
		/// One of two main entry points for this method object. The other is RemoveMovedFrom().
		/// Perhaps this should be deprecated? Does it properly handle multiple CCAs in a cell?
		/// </summary>
		/// <returns></returns>
		internal ICmIndirectAnnotation MakeMovedFrom()
		{
			// Making a MovedFrom marker creates a Feature on the Actual row that needs to be updated.
			// Removed ExtraPropChangedInserter that caused a crash on Undo in certain contexts. (LT-9442)
			// Not real sure why everything still works!
			using (GetMovedTextUndoHelper())
			{
				// Get rid of any empty marker in the cell where we will insert the marker.
				m_logic.RemoveMissingMarker(m_markerCell);

				// Enhance JohnT: reverse for RTL.
				// Enhance JohnT: may want to make actual strings configurable.
				string marker = m_fPrepose ? DiscourseStrings.ksMovedTextBefore : DiscourseStrings.ksMovedTextAfter;

				if (m_hvosToMark == null || m_hvosToMark.Length == 0)
					return MarkEntireCell(marker);
				return MarkPartialCell(marker);
			}
		}

		/// <summary>
		/// This handles the situation where we mark only the user-selected words as moved.
		/// m_hvosToMark member array contains the words to mark as moved
		///		(for now they should be contiguous)
		/// Deals with four cases:
		///		Case 1: the new CCA is at the beginning of the old, create a new one to hold the remainder
		///		Case 2: the new CCA is embedded within the old, create 2 new ones, first for the movedText
		///			and second for the remainder
		///		Case 3: the new CCA is at the end of the old, create a new one for the movedText
		///		Case 4: the new CCA IS the old CCA, just set the feature and marker
		///			(actually this last case should be handled by the dialog; it'll return no hvos)
		/// </summary>
		/// <param name="marker"></param>
		/// <returns></returns>
		private ICmIndirectAnnotation MarkPartialCell(string marker)
		{
			// find the first wfic (data) cca in this cell
			List<ICmAnnotation> cellContents = m_logic.CcasInCell(m_movedTextCell);
			ICmIndirectAnnotation ccaMovedText = m_logic.FindFirstCcaWithWfics(cellContents);
			Debug.Assert(ccaMovedText != null);

			// Find the CCA that contains our first "markable" hvo
			int iccaMT = 0;
			while (!ccaMovedText.AppliesToRS.Contains(m_hvosToMark[0]))
			{
				// Get next data CCA
				int ifoundAt;
				ccaMovedText = m_logic.FindNextCcaWithWfics(cellContents, iccaMT, out ifoundAt);
				// On failure of above Get return null
				if (ccaMovedText == null)
					return null; // Bail out because we didn't find a CCA pointing to our first hvo
				iccaMT = ifoundAt;
			}
			// Does this CCA contain all our hvos and are they "in order" (no skips or unorderliness)
			int ilastHvo;
			int ifirstHvo = CheckCcaContainsAllHvos(ccaMovedText, m_hvosToMark, out ilastHvo);
			if (ifirstHvo > -1)
			{
				int iccaAppliesTo = m_logic.IndexOfCca(ccaMovedText.Hvo, MTRow);
				// At this point we need to deal with our four cases:
				if (ifirstHvo == 0)
				{
					if (ilastHvo < ccaMovedText.AppliesToRS.Count - 1)
					{
						// Case 1: Add new CCA, put remainder of wfics in it
						PullOutRemainderWficsToNewCCA(ccaMovedText, iccaAppliesTo, ilastHvo + 1);
					}
					// Case 4: just drops through here, sets Feature and Marker
				}
				else
				{
					if (ilastHvo < ccaMovedText.AppliesToRS.Count - 1)
					{
						// Case 2: Take MT wfics and remainder wfics out of original, add new CCAs for MT and for remainder

						// For now, just take out remainder wfics and add a CCA for them.
						// The rest will be done when we drop through to Case 3
						PullOutRemainderWficsToNewCCA(ccaMovedText, iccaAppliesTo, ilastHvo + 1);
					}
					// Case 3: Take MT wfics out of original, add a new CCA for the MT
					ccaMovedText = PullOutRemainderWficsToNewCCA(ccaMovedText, iccaAppliesTo, ifirstHvo);
				}
				// Set the MT Feature
				ConstituentChartLogic.SetFeature(SDAccess, ccaMovedText.Hvo,
					ConstituentChartLogic.MovedTextFeatureName, true);
			}
			else return null; // Bail out because of a problem in our array of hvos (misordered or non-contiguous)

			// Now figure out where to insert the MTmarker
			int imarkerInsertAt = FindWhereToInsertMTMarker();

			// Insert the Marker
			return m_logic.MakeCca(m_markerCell, new int[] { ccaMovedText.Hvo }, imarkerInsertAt, marker);
		}

		/// <summary>
		/// Pulls trailing wfics out of a CCA and creates a new one for them. Then we return the new CCA.
		/// </summary>
		/// <param name="ccaMovedText">The original CCA from which we will remove the moved text wfics.</param>
		/// <param name="iccaMT">The index of ccaMovedText in its row's AppliesTo</param>
		/// <param name="ifirstHvo">The index of the first wfic to be pulled out of the original CCA.</param>
		/// <returns>The new CCA that will now contain the moved text wfics.</returns>
		private ICmIndirectAnnotation PullOutRemainderWficsToNewCCA(ICmIndirectAnnotation ccaMovedText, int iccaMT, int ifirstHvo)
		{
			// Pull out trailing wfics
			List<int> annHvoList = new List<int>();
			int cappliesTo = ccaMovedText.AppliesToRS.Count;
			for (int i = ifirstHvo; i < cappliesTo; i++)
				annHvoList.Add(ccaMovedText.AppliesToRS[i].Hvo);
			foreach (int hvo in annHvoList)
				ccaMovedText.AppliesToRS.Remove(hvo);
			return m_logic.MakeCca(m_movedTextCell, annHvoList.ToArray(), iccaMT + 1, null);
		}

		/// <summary>
		/// Checks our array of hvos to see that all are pointed to by this CCA and that they are in order.
		/// For now they must be contigous too. Returns the index of the first hvo within AppliesTo for this
		/// CCA or -1 if check fails. The out variable returns the index of the last hvo to mark within this CCA.
		/// </summary>
		/// <param name="ccaMovedText"></param>
		/// <param name="m_hvosToMark"></param>
		/// <param name="ilastHvo"></param>
		/// <returns>ifirstHvo</returns>
		private int CheckCcaContainsAllHvos(ICmIndirectAnnotation ccaMovedText, int[] m_hvosToMark, out int ilastHvo)
		{
			Debug.Assert(m_hvosToMark.Length > 0, "No HVOs to mark!");
			Debug.Assert(ccaMovedText != null && ccaMovedText.AppliesToRS != null
				&& ccaMovedText.AppliesToRS.Count > 0, "No wfics in this CCA!");
			int ifirstHvo = -1;
			ilastHvo = -1;
			int ihvosToMark = 0;
			int chvosToMark = m_hvosToMark.Length;
			for (int icca = 0; icca < ccaMovedText.AppliesToRS.Count; icca++)
			{
				if (ccaMovedText.AppliesToRS[icca].Hvo == m_hvosToMark[ihvosToMark])
				{
					// Found a live one!
					if (ihvosToMark == 0)
						ifirstHvo = icca; // found the first one!
					ilastHvo = icca;
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

		private ICmIndirectAnnotation MarkEntireCell(string marker)
		{
			// This handles the case where we mark the entire cell (CCA) as moved.
			// Review: What happens if there is more than one data CCA in the cell already?
			// At this point, only the first will get marked as moved.
			ICmAnnotation ccaMovedText = m_logic.FindCcaInColumn(m_movedTextCell, true); // find a wfic cca
			Debug.Assert(ccaMovedText != null);

			// Now figure out where to insert the MTmarker
			int iccaInsertAt = FindWhereToInsertMTMarker();

			ConstituentChartLogic.SetFeature(SDAccess, ccaMovedText.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			return m_logic.MakeCca(m_markerCell, new int[] { ccaMovedText.Hvo }, iccaInsertAt, marker);
		}

		/// <summary>
		/// Insert BEFORE anything already in column, if Preposed; otherwise, after.
		/// </summary>
		/// <returns></returns>
		private int FindWhereToInsertMTMarker()
		{
			ChartLocation markerCell = new ChartLocation((m_fPrepose ? MarkerColIndex - 1: MarkerColIndex), MarkerRow);
			return m_logic.FindIndexOfCcaInLaterColumn(markerCell);
		}

		internal void RemoveMovedFrom()
		{
			// Deleting a MovedFrom marker removes a Feature on the Actual row that needs to be updated.
			// Pretend it got deleted and re-inserted, both now and on Undo/Redo.
			using (GetMovedTextUndoHelper())
			{
				using (new ExtraPropChangedInserter(SDAccess, Chart.Hvo, kflidRows,
					m_logic.IndexOfRow(HvoMTRow), 1, 1))
				{
					FindMarkerAndDelete();
					// Handle cases where after removing there are multiple CCAs that can be merged.
					// If the removed movedFeature was part of the cell, there could easily be 3 CCAs to merge after removing.
					CollapseRedundantDataCcas();
				}
			}
		}

		private void FindMarkerAndDelete()
		{
			foreach (ICmAnnotation cca in m_logic.CcasInCell(m_markerCell))
			{
				if (m_logic.IsMarkerOfMovedFrom(cca, m_movedTextCell))
				{
					ICmIndirectAnnotation cca1 = cca as ICmIndirectAnnotation;
					// Remove Marker from Row
					MarkerRow.AppliesToRS.Remove(cca1);
					// Turn off MovedText feature in target cca
					ConstituentChartLogic.SetFeature(SDAccess, cca1.AppliesToRS[0].Hvo,
						ConstituentChartLogic.MovedTextFeatureName, false);
					// Delete the Marker
					cca1.DeleteUnderlyingObject();
				}
			}
		}

		private void CollapseRedundantDataCcas()
		{
			// Enhance GordonM: May need to do something different here if we can put markers between data CCAs.

			// Get ALL the CCAs
			List<ICmAnnotation> ccaList = m_logic.CcasInCell(m_movedTextCell);
			Set<int> hvosToDelete = new Set<int>();

			for (int icca = 0; icca < ccaList.Count - 1; icca++)
			{
				int hvoCurrCca = ccaList[icca].Hvo;
				if (ConstituentChartLogic.IsWficGroup(Cache, hvoCurrCca) &&
					!ConstituentChartLogic.IsMovedText(SDAccess, hvoCurrCca))
				{
					while (icca < ccaList.Count - 1) // Allows swallowing multiple CCAs if conditions are right.
					{
						int hvoNextCca = ccaList[icca + 1].Hvo;
						if (!ConstituentChartLogic.IsWficGroup(Cache, hvoNextCca))
							break;
						if (ConstituentChartLogic.IsMovedText(SDAccess, hvoNextCca))
						{
							icca++; // Skip current AND next, since next is MovedText
							continue;
						}
						// Conditions are right! Swallow the next CCA into the current one!
						SwallowRedundantCca(ccaList, icca, hvosToDelete);
					}
				}
			}

			// Delete all objects in hvosToDelete
			CmObject.DeleteObjects(hvosToDelete, Cache, false);
		}

		private void SwallowRedundantCca(List<ICmAnnotation> ccaList, int icca, Set<int> hvosToDelete)
		{
			// Move all wfic annotations from ccaList[icca+1] to end of ccaList[icca].
			ICmIndirectAnnotation ccaSrc = ccaList[icca+1] as ICmIndirectAnnotation;
			ICmIndirectAnnotation ccaDst = ccaList[icca] as ICmIndirectAnnotation;
			m_logic.ChangeCca(ccaSrc.AppliesToRS.HvoArray, ccaSrc, ccaDst, 0, ccaDst.AppliesToRS.Count);

			// Clean up
			hvosToDelete.Add(ccaList[icca + 1].Hvo); // set it to be deleted
			MTRow.AppliesToRS.Remove(ccaList[icca + 1]); // remove swallowed CCA from row
			ccaList.Remove(ccaList[icca + 1]); // remove swallowed CCA from list
		}

		private UndoRedoTaskHelper GetMovedTextUndoHelper()
		{
			string undoText = DiscourseStrings.ksUndoPostposeFrom;
			string redoText = DiscourseStrings.ksRedoPostposeFrom;
			if (m_fPrepose)
			{
				undoText = DiscourseStrings.ksUndoPreposeFrom;
				redoText = DiscourseStrings.ksRedoPreposeFrom;
			}
			return m_logic.GetUndoHelper(undoText, redoText);
		}

		#endregion
	}

	#endregion // MakeMovedTextMethod

	// used for user-defined markers
	public class RowColPossibilityMenuItem : ToolStripMenuItem
	{
		private ChartLocation m_srcCell;
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
	/// This interface defines the functionality that the logic needs from the ribbon. Test code implements
	/// mock ribbons.
	/// </summary>
	public interface IInterlinRibbon
	{
		int[] SelectedAnnotations { get; }
		void MakeInitialSelection();
		void SelectFirstAnnotation();
		int AnnotationListId { get; }
		int EndSelLimitIndex { get; set; }
		int SelLimAnn { get; set;}
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
		ConstituentChartLogic m_logic;
		bool m_fForRedo;

		public UpdateRibbonAction(ConstituentChartLogic logic, bool fForRedo)
		{
			m_logic = logic;
			m_fForRedo = fForRedo;
		}
		public void UpdateUnchartedAnnotations()
		{
			int[] unchartedAnnotations = m_logic.NextUnchartedInput(ConstituentChartLogic.kMaxRibbonContext);
			int[] oldAnnotations = m_logic.Cache.GetVectorProperty(m_logic.StTextHvo, m_logic.Ribbon.AnnotationListId, false);
			m_logic.Cache.VwCacheDaAccessor.CacheVecProp(m_logic.StTextHvo, m_logic.Ribbon.AnnotationListId, unchartedAnnotations,
				unchartedAnnotations.Length);
			// Do this BEFORE the PropChanged, in at least one case (clear chart from here on converts leading chorph
			// to normal), we need to clear the info about it being a chorph before we redraw.
			m_logic.RaiseRibbonChgEvent();
			// typically changed at start AND end; not worth trouble to figure out exactly and make multiple calls.
			m_logic.Cache.PropChanged(m_logic.StTextHvo, m_logic.Ribbon.AnnotationListId, 0,
				unchartedAnnotations.Length, oldAnnotations.Length);
			m_logic.Ribbon.SelectFirstAnnotation();
		}
		#region IUndoAction Members

		public void Commit()
		{
		}

		public bool IsDataChange()
		{
			return false; // no real data changes as a result of this.
		}

		public bool IsRedoable()
		{
			return true;
		}

		public bool Redo(bool fRefreshPending)
		{
			if (m_fForRedo)
				UpdateUnchartedAnnotations();
			return true;
		}

		public bool RequiresRefresh()
		{
			return false; // whole purpose of this is to avoid need for refresh
		}

		public bool SuppressNotification
		{
			set {  }
		}

		public bool Undo(bool fRefreshPending)
		{
			if (!m_fForRedo)
				UpdateUnchartedAnnotations();
			return true;
		}


		#endregion
	}

	internal class WficSorter : IComparer<ICmBaseAnnotation>
	{
		#region IComparer<ICmBaseAnnotation> Members

		public int Compare(ICmBaseAnnotation x, ICmBaseAnnotation y)
		{
			if (x.BeginObjectRAHvo == y.BeginObjectRAHvo)
			{
				return x.BeginOffset - y.BeginOffset;
			}
			return x.BeginObjectRA.OwnOrd - y.BeginObjectRA.OwnOrd;
		}

		#endregion
	}

	// Item used in row combo box.
	internal class RowMenuItem
	{
		ICmIndirectAnnotation m_row;
		internal RowMenuItem(ICmIndirectAnnotation row)
		{
			m_row = row;
		}

		// Return the CCR's row label (1a, 1b, etc.) as a string
		public override string ToString()
		{
			return m_row.Comment.GetAlternative(m_row.Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en")).Text;
		}

		public ICmIndirectAnnotation Row
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
