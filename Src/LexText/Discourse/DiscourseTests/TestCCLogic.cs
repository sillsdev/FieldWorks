using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Class exists to make testable protected methods of the class
	/// It also overrides a number of database operations and just records that they happened.
	/// It functions as a test spy allowing us to do in-memory tests of operations that normally
	/// work on the database.
	/// </summary>
	class TestCCLogic : ConstituentChartLogic
	{
		internal List<object[]> m_events = new List<object[]>();
		internal bool m_fRecordMergeCellContents; // set true to log MergeCellContents.
		internal bool m_fRecordBasicEdits; // set true for logging most low-level operations.

		internal TestCCLogic(FdoCache cache, IDsConstChart chart, IStText stText)
			: base(cache, chart, stText.Hvo)
		{
		}
		/// <summary>
		/// Make one and set the other stuff later.
		/// </summary>
		/// <param name="cache"></param>
		internal TestCCLogic(FdoCache cache) : base(cache)
		{
		}

		#region protected methods we want to test

		internal List<IConstChartRow> CallCollectEligRows(ChartLocation cell, bool fPrepose)
		{
			return CollectEligibleRows(cell, fPrepose);
		}

		internal void CallMakeMovedFrom(int icolActual, int icolMovedFrom, IConstChartRow rowActual,
			IConstChartRow rowMovedFrom, AnalysisOccurrence begPoint, AnalysisOccurrence endPoint)
		{
			MakeMovedFrom(new ChartLocation(rowActual, icolActual), new ChartLocation(rowMovedFrom, icolMovedFrom),
				begPoint, endPoint);
		}

		internal bool[] CallHighlightChOrphPossibles(int icolPrec, int irowPrec,
			int icolFoll, int irowFoll)
		{
			return HighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
		}

		internal int CallGetParaIndexForOccurrence(AnalysisOccurrence point)
		{
			return GetParagraphIndexForOccurrence(point);
		}

		internal bool CallIsChOrph(AnalysisOccurrence point)
		{
			return IsChOrph(point);
		}

		internal int CallFindIndexOfCellPartInLaterColumn(ChartLocation targetCell)
		{
			return FindIndexOfCellPartInLaterColumn(targetCell);
		}

		internal IConstChartWordGroup CallFindWordGroup(List<IConstituentChartCellPart> cellPartsInCell)
		{
			return FindFirstWordGroup(cellPartsInCell);
		}

		internal void CallMakeMovedFrom(int icolActual, int icolMovedFrom, IConstChartRow row)
		{
			MakeMovedFrom(new ChartLocation(row, icolActual), new ChartLocation(row, icolMovedFrom));
		}

		/// <summary>
		/// This calls a method that assumes it is already inside of a valid UOW.
		/// </summary>
		/// <returns></returns>
		internal IConstChartRow CallMakeNewRow()
		{
			return MakeNewRow();
		}

		internal bool CallIsMarkedAsMovedFrom(ChartLocation actualCell, int icolMovedFrom)
		{
			return IsMarkedAsMovedFrom(actualCell, icolMovedFrom);
		}

		internal void CallRemoveMovedFrom(IConstChartRow row, int icolActual, int icolMovedFrom)
		{
			RemoveMovedFrom(new ChartLocation(row, icolActual), new ChartLocation(row, icolMovedFrom));
		}

		internal void CallRemoveMovedFromDiffRow(IConstChartRow rowActual, int icolActual, IConstChartRow rowMovedFrom, int icolMovedFrom)
		{
			RemoveMovedFrom(new ChartLocation(rowActual, icolActual), new ChartLocation(rowMovedFrom, icolMovedFrom));
		}

		internal void CallMergeCellContents(IConstChartRow rowSrc, int icolSrc, IConstChartRow rowDst, int icolDst, bool forward)
		{
			var srcCell = new ChartLocation(rowSrc, icolSrc);
			var dstCell = new ChartLocation(rowDst, icolDst);
			MergeCellContents(srcCell, dstCell, forward);
		}

		internal void CallRemoveDepClause(ChartLocation srcCell)
		{
			RemoveDepClause(srcCell);
		}

		#endregion protected methods we want to test

		#region event checkers
		/// <summary>
		/// Verify that there is an event with the specified name, and return it.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="cargs"></param>
		internal object[] VerifyEventExists(string name, int cargs)
		{
			foreach (object[] item in m_events)
			{
				if (item.Length > 0 && (item[0] is string) && name == (string)(item[0]))
				{
					Assert.AreEqual(cargs, item.Length - 1, name + " event should have " + cargs + " arguments");
					return item;
				}
			}
			Assert.Fail("expected event " + name);
			return null;
		}

		internal void VerifyMergeCellsEvent(ChartLocation srcCell, ChartLocation dstCell, bool forward)
		{
			//m_events.Add(new object[] { "merge cell contents", srcCell, dstCell, forward });
			object[] event1 = VerifyEventExists("merge cell contents", 3);
			Assert.IsTrue(srcCell.IsSameLocation(event1[1]));
			Assert.IsTrue(dstCell.IsSameLocation(event1[2]));
			Assert.AreEqual(forward, event1[3]);
		}

		internal void VerifyEventCount(int count)
		{
			Assert.AreEqual(count, m_events.Count, "Wrong number of events logged");
		}

		#endregion

		public ChartLocation MakeLocObj(int icol, IConstChartRow row)
		{
			return new ChartLocation(row, icol);
		}

		public override void ChangeColumn(IConstituentChartCellPart[] partsToMove, ICmPossibility newCol, IConstChartRow row)
		{
			if (m_fRecordBasicEdits)
				m_events.Add(new object[] { "change column", partsToMove, newCol, row });
			else
				base.ChangeColumn(partsToMove, newCol, row);
		}

		protected internal override void ChangeColumnInternal(IConstituentChartCellPart[] partsToMove, ICmPossibility newCol)
		{
			if (m_fRecordBasicEdits)
				m_events.Add(new object[] { "change column internal", partsToMove, newCol });
			else
				base.ChangeColumnInternal(partsToMove, newCol);
		}

		public override void DeleteCellParts(IConstChartRow row, int ihvo, int chvo)
		{
			if (m_fRecordBasicEdits)
				m_events.Add(new object[] { "delete cellParts", row, ihvo, chvo });
			else
				base.DeleteCellParts(row, ihvo, chvo);
		}

		protected override void MergeCellContents(ChartLocation srcCell, ChartLocation dstCell,	bool forward)
		{
			if (m_fRecordMergeCellContents)
				m_events.Add(new object[] { "merge cell contents", srcCell, dstCell, forward });
			else
				base.MergeCellContents(srcCell, dstCell, forward);
		}
	}

	// TODO: what to do here!??
	//class UndoRedoHelperSpy : IUndoRedoTaskHelper
	//{
	//    internal List<object[]> m_events;

	//    public UndoRedoHelperSpy(List<object[]> events, string undoText, string redoText)
	//    {
	//        m_events = events;
	//        m_events.Add(new object[] { "start undo sequence", undoText });
	//    }

	//    #region IUndoRedoTaskHelper Members

	//    public void AddAction(Common.COMInterfaces.IUndoAction action)
	//    {
	//        m_events.Add(new object[] { "add Undo Action", action });
	//    }

	//    #endregion

	//    #region IDisposable Members

	//    public void Dispose()
	//    {
	//        m_events.Add(new object[] { "end undo sequence" });
	//        GC.SuppressFinalize(this);
	//    }

	//    #endregion

	//}
}
