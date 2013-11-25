/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2006-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestVwTableBox.h
Responsibility:
Last reviewed:

	Unit tests for the VwBox derived classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwTableBox_H_INCLUDED
#define TestVwTableBox_H_INCLUDED

#pragma once

#include "testViews.h"
#include "resource.h"

#include "VwTableBox.h"

namespace TestViews
{
	class DummyVwCellBox : public VwTableCellBox
	{
	public:
		DummyVwCellBox(int iColumn, int cColSpan = 1) : VwTableCellBox()
		{
			m_icolm = iColumn;
			m_ccolmSpan = cColSpan;
		}
	};

	class TestVwCellBox : public unitpp::suite
	{
	public:
		// Tests that NextBoxForSelection() returns the same column in the next row if we're
		// selecting in the same column only.
		void testNextBoxForSelection_OneTableSameColumn()
		{
			VwLength width;
			width.nVal = 20;
			width.unit = kunPercent100;

			VwTableBox * pTable = NewObj VwTableBox(NULL, 2, width, 0, kvaLeft, kvfpVoid,
				kvrlNone, 0, 0, true);
			VwTableRowBox * pRow1 = NewObj VwTableRowBox(NULL);
			VwTableRowBox * pRow2 = NewObj VwTableRowBox(NULL);
			pTable->_SetFirstBox(pRow1);
			pRow1->SetNext(pRow2);
			pTable->_SetLastBox(pRow2);
			pRow1->Container(pTable);
			pRow2->Container(pTable);

			DummyVwCellBox * pCell11 = NewObj DummyVwCellBox(0);
			DummyVwCellBox * pCell12 = NewObj DummyVwCellBox(1);
			DummyVwCellBox * pCell21 = NewObj DummyVwCellBox(0);
			DummyVwCellBox * pCell22 = NewObj DummyVwCellBox(1);
			pRow1->_SetFirstBox(pCell11);
			pRow1->_SetLastBox(pCell12);
			pCell11->SetNext(pCell12);
			pCell11->Container(pRow1);
			pCell12->Container(pRow1);

			pRow2->_SetFirstBox(pCell21);
			pRow2->_SetLastBox(pCell22);
			pCell21->SetNext(pCell22);
			pCell21->Container(pRow2);
			pCell22->Container(pRow2);

			VwBox* pStartSearch = NULL;
			VwBox* pNext = pCell11->NextBoxForSelection(&pStartSearch, true);

			unitpp::assert_eq("Got wrong box", pCell21, pNext);

			// Also deletes all children...
			delete pTable;
		}

		// Tests that NextBoxForSelection() returns the same column in the next row if we're
		// selecting in the same column only and if the box spans multiple columns.
		void testNextBoxForSelection_OneTableSameColumn_ColumnSpan()
		{
			VwLength width;
			width.nVal = 20;
			width.unit = kunPercent100;

			VwTableBox * pTable = NewObj VwTableBox(NULL, 3, width, 0, kvaLeft, kvfpVoid,
				kvrlNone, 0, 0, true);
			VwTableRowBox * pRow1 = NewObj VwTableRowBox(NULL);
			VwTableRowBox * pRow2 = NewObj VwTableRowBox(NULL);
			pTable->_SetFirstBox(pRow1);
			pRow1->SetNext(pRow2);
			pTable->_SetLastBox(pRow2);
			pRow1->Container(pTable);
			pRow2->Container(pTable);

			DummyVwCellBox * pCell11 = NewObj DummyVwCellBox(0);
			DummyVwCellBox * pCell12 = NewObj DummyVwCellBox(1);
			DummyVwCellBox * pCell13 = NewObj DummyVwCellBox(2);
			DummyVwCellBox * pCell21 = NewObj DummyVwCellBox(0);
			DummyVwCellBox * pCell22 = NewObj DummyVwCellBox(1, 2);
			pRow1->_SetFirstBox(pCell11);
			pRow1->_SetLastBox(pCell13);
			pCell11->SetNext(pCell12);
			pCell12->SetNext(pCell13);
			pCell11->Container(pRow1);
			pCell12->Container(pRow1);
			pCell13->Container(pRow1);

			pRow2->_SetFirstBox(pCell21);
			pRow2->_SetLastBox(pCell22);
			pCell21->SetNext(pCell22);
			pCell21->Container(pRow2);
			pCell22->Container(pRow2);

			VwBox* pStartSearch = NULL;
			VwBox* pNext = pCell13->NextBoxForSelection(&pStartSearch, true);

			unitpp::assert_eq("Got wrong box", pCell22, pNext);

			// Also deletes all children...
			delete pTable;
		}

		// Tests that NextBoxForSelection() returns a box in the next column if we're
		// NOT selecting in the same column only.
		void testNextBoxForSelection_OneTableMultiColumn()
		{
			VwLength width;
			width.nVal = 20;
			width.unit = kunPercent100;

			VwTableBox * pTable = NewObj VwTableBox(NULL, 2, width, 0, kvaLeft, kvfpVoid,
				kvrlNone, 0, 0, false);
			VwTableRowBox * pRow1 = NewObj VwTableRowBox(NULL);
			VwTableRowBox * pRow2 = NewObj VwTableRowBox(NULL);
			pTable->_SetFirstBox(pRow1);
			pRow1->SetNext(pRow2);
			pTable->_SetLastBox(pRow2);
			pRow1->Container(pTable);
			pRow2->Container(pTable);

			DummyVwCellBox * pCell11 = NewObj DummyVwCellBox(0);
			DummyVwCellBox * pCell12 = NewObj DummyVwCellBox(1);
			DummyVwCellBox * pCell21 = NewObj DummyVwCellBox(0);
			DummyVwCellBox * pCell22 = NewObj DummyVwCellBox(1);
			pRow1->_SetFirstBox(pCell11);
			pRow1->_SetLastBox(pCell12);
			pCell11->SetNext(pCell12);
			pCell11->Container(pRow1);
			pCell12->Container(pRow1);

			pRow2->_SetFirstBox(pCell21);
			pRow2->_SetLastBox(pCell22);
			pCell21->SetNext(pCell22);
			pCell21->Container(pRow2);
			pCell22->Container(pRow2);

			VwBox* pStartSearch = NULL;
			VwBox* pNext = pCell11->NextBoxForSelection(&pStartSearch, true);

			unitpp::assert_eq("Got wrong box", pCell12, pNext);

			// Also deletes all children...
			delete pTable;
		}

		// Tests that NextBoxForSelection() returns the same column in the next table if we're
		// selecting in the same column only.
		void testNextBoxForSelection_MultiTableSameColumn()
		{
			VwLength width;
			width.nVal = 20;
			width.unit = kunPercent100;

			VwRootBox * pRootb = NewObj VwRootBox(NULL);

			VwTableBox * pTable1 = NewObj VwTableBox(NULL, 2, width, 0, kvaLeft, kvfpVoid,
				kvrlNone, 0, 0, true);
			pTable1->Container(pRootb);

			VwTableRowBox * pRow1 = NewObj VwTableRowBox(NULL);
			pTable1->_SetFirstBox(pRow1);
			pTable1->_SetLastBox(pRow1);
			pRow1->Container(pTable1);

			VwTableBox * pTable2 = NewObj VwTableBox(NULL, 2, width, 0, kvaLeft, kvfpVoid,
				kvrlNone, 0, 0, true);
			pTable2->Container(pRootb);
			pTable1->SetNext(pTable2);

			VwTableRowBox * pRow2 = NewObj VwTableRowBox(NULL);
			pTable2->_SetFirstBox(pRow2);
			pTable2->_SetLastBox(pRow2);
			pRow2->Container(pTable2);

			pRootb->_SetFirstBox(pTable1);
			pRootb->_SetLastBox(pTable2);

			DummyVwCellBox * pCell11 = NewObj DummyVwCellBox(0);
			DummyVwCellBox * pCell12 = NewObj DummyVwCellBox(1);
			pRow1->_SetFirstBox(pCell11);
			pRow1->_SetLastBox(pCell12);
			pCell11->SetNext(pCell12);
			pCell11->Container(pRow1);
			pCell12->Container(pRow1);

			DummyVwCellBox * pCell21 = NewObj DummyVwCellBox(0);
			DummyVwCellBox * pCell22 = NewObj DummyVwCellBox(1);
			pRow2->_SetFirstBox(pCell21);
			pRow2->_SetLastBox(pCell22);
			pCell21->SetNext(pCell22);
			pCell21->Container(pRow2);
			pCell22->Container(pRow2);

			VwBox* pStartSearch = NULL;
			VwBox* pTable = pCell11->NextBoxForSelection(&pStartSearch, true);
			unitpp::assert_eq("Got wrong table box", pTable2, pTable);

			VwBox* pRow = pTable->NextBoxForSelection(&pStartSearch, true);
			unitpp::assert_eq("Got wrong row box", pRow2, pRow);

			VwBox* pCell = pRow->NextBoxForSelection(&pStartSearch, true);
			unitpp::assert_eq("Got wrong cell box", pCell21, pCell);

			// Also deletes all children...
			pRootb->Close();
			delete pRootb;
		}

	public:
		TestVwCellBox();
	};

}
#endif /*TestVwTableBox_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
