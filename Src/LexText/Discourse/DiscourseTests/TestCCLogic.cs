using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;

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
		internal bool m_fRecordMergeCellContents = false; // set true to log MergeCellContents.
		internal bool m_fRecordBasicEdits = false; // set true for logging most low-level operations.

		internal TestCCLogic(FdoCache cache, DsConstChart chart, int hvoStText)
			: base(cache, chart, hvoStText)
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

		internal List<ICmIndirectAnnotation> CallCollectEligRows(ChartLocation cell, bool fPrepose)
		{
			return CollectEligibleRows(cell, fPrepose);
		}

		internal void CallMakeMovedFrom(int icolActual, int icolMovedFrom, CmIndirectAnnotation rowActual,
			CmIndirectAnnotation rowMovedFrom, int[] wficsToMove)
		{
			MakeMovedFrom(new ChartLocation(icolActual, rowActual), new ChartLocation(icolMovedFrom, rowMovedFrom), wficsToMove);
		}

		internal bool[] CallHighlightChOrphPossibles(int icolPrec, int irowPrec,
			int icolFoll, int irowFoll)
		{
			return HighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
		}

		internal int CallGetWficParaIndex(int hvoWfic)
		{
			return GetParagraphIndexForWfic(hvoWfic);
		}

		internal int CallGetBeginOffset(int hvoCba)
		{
			return GetBeginOffset(hvoCba);
		}

		internal bool CallIsChOrph(int hvoWfic)
		{
			return IsChOrph(hvoWfic);
		}

		internal int CallFindIndexOfCcaInLaterColumn(ChartLocation targetCell)
		{
			return FindIndexOfCcaInLaterColumn(targetCell);
		}

		internal ICmIndirectAnnotation CallFindFirstCcaWithWfics(List<ICmAnnotation> ccasInCell)
		{
			return FindFirstCcaWithWfics(ccasInCell);
		}
		internal void CallMakeMovedFrom(int icolActual, int icolMovedFrom, ICmIndirectAnnotation row)
		{
			MakeMovedFrom(new ChartLocation(icolActual, row), new ChartLocation(icolMovedFrom, row));
		}
		internal ICmIndirectAnnotation CallMakeNewRow()
		{
			return MakeNewRow();
		}
		internal bool CallIsMarkedAsMovedFrom(ChartLocation actualCell, int icolMovedFrom)
		{
			return IsMarkedAsMovedFrom(actualCell, icolMovedFrom);
		}
		internal void CallRemoveMovedFrom(int icolActual, int icolMovedFrom, ICmIndirectAnnotation row)
		{
			RemoveMovedFrom(new ChartLocation(icolActual, row), new ChartLocation(icolMovedFrom, row));
		}
		internal void CallRemoveMovedFromDiffRow(int icolActual, int icolMovedFrom, ICmIndirectAnnotation rowActual,
			ICmIndirectAnnotation rowMovedFrom)
		{
			RemoveMovedFrom(new ChartLocation(icolActual, rowActual), new ChartLocation(icolMovedFrom, rowMovedFrom));
		}
		internal void CallMergeCellContents(int icolSrc, ICmIndirectAnnotation rowSrc,
			int icolDst, ICmIndirectAnnotation rowDst, bool forward)
		{
			ChartLocation srcCell = new ChartLocation(icolSrc, rowSrc);
			ChartLocation dstCell = new ChartLocation(icolDst, rowDst);
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

		internal void VerifyChangeParentEvent(int[] wficsToMove, ICmIndirectAnnotation ccaSrc, ICmIndirectAnnotation ccaDst,
			int srcIndex, int dstIndex)
		{
			object[] event1 = VerifyEventExists("change parent", 5);
			Assert.AreEqual(wficsToMove, event1[1] as int[]);
			Assert.AreEqual(ccaSrc.Hvo, (event1[2] as ICmIndirectAnnotation).Hvo);
			Assert.AreEqual(ccaDst.Hvo, (event1[3] as ICmIndirectAnnotation).Hvo);
			Assert.AreEqual(srcIndex, (int)event1[4]);
			Assert.AreEqual(dstIndex, (int)event1[5]);
		}
		internal void VerifyChangeRowEvent(int[] ccasToMove, ICmIndirectAnnotation RowSrc, ICmIndirectAnnotation rowDst,
			int srcIndex, int dstIndex)
		{
			object[] event1 = VerifyEventExists("change row", 5);
			Assert.AreEqual(ccasToMove, event1[1] as int[]);
			Assert.AreEqual(RowSrc.Hvo, (event1[2] as ICmIndirectAnnotation).Hvo);
			Assert.AreEqual(rowDst.Hvo, (event1[3] as ICmIndirectAnnotation).Hvo);
			Assert.AreEqual(srcIndex, (int)event1[4]);
			Assert.AreEqual(dstIndex, (int)event1[5]);
		}

		internal void VerifyDeleteCcasEvent(ICmIndirectAnnotation row, int ihvo, int chvo)
		{
			object[] event1 = VerifyEventExists("delete ccas", 3);
			Assert.AreEqual(row.Hvo, (event1[1] as ICmIndirectAnnotation).Hvo);
			Assert.AreEqual(ihvo, (int)event1[2]);
			Assert.AreEqual(chvo, (int)event1[3]);
		}
		internal void VerifyChangeColumnEvent(ICmAnnotation[] ccasToMove, int hvoNewCol, ICmIndirectAnnotation row)
		{
			object[] event1 = VerifyEventExists("change column", 3);
			Assert.AreEqual(ccasToMove, event1[1] as ICmAnnotation[]);
			Assert.AreEqual(hvoNewCol, event1[2]);
			Assert.AreEqual(row.Hvo, (event1[3] as ICmIndirectAnnotation).Hvo);
		}

		internal void VerifyChangeColumnInternalEvent(ICmAnnotation[] ccasToMove, int hvoNewCol)
		{
			object[] event1 = VerifyEventExists("change column internal", 2);
			Assert.AreEqual(ccasToMove, event1[1] as ICmAnnotation[]);
			Assert.AreEqual(hvoNewCol, event1[2]);
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

		public ChartLocation MakeLocObj(int icol, ICmIndirectAnnotation row)
		{
			return new ChartLocation(icol, row);
		}

		public override void ChangeRow(int[] ccasToMove, ICmIndirectAnnotation rowSrc, ICmIndirectAnnotation rowDst,
			int srcIndex, int dstIndex)
		{
			if (m_fRecordBasicEdits)
				m_events.Add(new object[] { "change row", ccasToMove, rowSrc, rowDst, srcIndex, dstIndex });
			else
				base.ChangeRow(ccasToMove, rowSrc, rowDst, srcIndex, dstIndex);
		}

		public override void ChangeColumn(ICmAnnotation[] ccasToMove, int hvoNewCol, ICmIndirectAnnotation row)
		{
			if (m_fRecordBasicEdits)
				m_events.Add(new object[] { "change column", ccasToMove, hvoNewCol, row });
			else
				base.ChangeColumn(ccasToMove, hvoNewCol, row);
		}

		protected internal override void ChangeColumnInternal(ICmAnnotation[] ccasToMove, int hvoNewCol)
		{
			if (m_fRecordBasicEdits)
				m_events.Add(new object[] { "change column internal", ccasToMove, hvoNewCol });
			else
				base.ChangeColumnInternal(ccasToMove, hvoNewCol);
		}

		public override void DeleteCcas(ICmIndirectAnnotation row, int ihvo, int chvo)
		{
			if (m_fRecordBasicEdits)
				m_events.Add(new object[] { "delete ccas", row, ihvo, chvo });
			else
				base.DeleteCcas(row, ihvo, chvo);
		}

		public override void ChangeCca(int[] wficsToMove, ICmIndirectAnnotation ccaSrc, ICmIndirectAnnotation ccaDst,
			int srcIndex, int dstIndex)
		{
			if (m_fRecordBasicEdits)
				m_events.Add(new object[] { "change parent", wficsToMove, ccaSrc, ccaDst, srcIndex, dstIndex });
			else
				base.ChangeCca(wficsToMove, ccaSrc, ccaDst, srcIndex, dstIndex);
		}

		protected override void MergeCellContents(ChartLocation srcCell, ChartLocation dstCell,	bool forward)
		{
			if (m_fRecordMergeCellContents)
				m_events.Add(new object[] { "merge cell contents", srcCell, dstCell, forward });
			else
				base.MergeCellContents(srcCell, dstCell, forward);
		}
	}

	class UndoRedoHelperSpy : IUndoRedoTaskHelper
	{
		internal List<object[]> m_events;

		public UndoRedoHelperSpy(List<object[]> events, string undoText, string redoText)
		{
			m_events = events;
			m_events.Add(new object[] { "start undo sequence", undoText });
		}

		#region IUndoRedoTaskHelper Members

		public void AddAction(SIL.FieldWorks.Common.COMInterfaces.IUndoAction action)
		{
			m_events.Add(new object[] { "add Undo Action", action });
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			m_events.Add(new object[] { "end undo sequence" });
		}

		#endregion
	}
}
