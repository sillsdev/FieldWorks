/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwTableBox.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Contains the classes VwTableBox, VwTableRowBox, and VwTableCellBox, which implement the
	view of a table.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)
#include <limits.h>

using namespace std;

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	VwTableBox Methods
//:>********************************************************************************************

VwTableBox::VwTableBox(VwPropertyStore * pzvps, int ccolm, VwLength vlenWidth,
	int dzmpBorderWidth,
	VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule,
	int dzmpSpacing, int dzmpPadding, bool fSelectOneCol)
	:VwPileBox(pzvps)
{
		m_ccolm = ccolm;
		m_vlenRequestWidth = vlenWidth;
		m_dzmpBorderWidth = dzmpBorderWidth;
		m_vwalign = vwalign;
		m_frmpos = frmpos;
		m_vwrule = vwrule;
		m_dzmpCellSpacing = dzmpSpacing;
		m_dzmpCellPadding = dzmpPadding;
		m_fSelectOneCol = fSelectOneCol;

		if (ccolm > 0)
		{
			m_vcolspec.Resize(ccolm);
			int nColShare = 10000 / ccolm;   // default column's share of width,
											// in 100ths of percent
			for (int i = 0; i < ccolm; i++)
			{
				VwLength vlen;
				vlen.nVal = nColShare;
				vlen.unit = kunPercent100;
				m_vcolspec[i].SetWidthVLen(vlen);
			}
			//make colums request borders where appropriate for frame
			if (m_frmpos & kvfpLhs)
			{
				m_vcolspec[0].SetGroupLeft(true);
			}
			if (m_frmpos & kvfpRhs)
			{
				m_vcolspec.Top()->SetGroupRight(true);
			}
		}
}

// Does nothing, but I think we may need it so smart pointers can destroy things?
VwTableBox::~VwTableBox()
{
}

/*----------------------------------------------------------------------------------------------
	This is more-or-less the group box algorithm. The regular pile one does
	not work because of cells that span rows. We need to go direct to the cells,
	not find the closest row first, because a cell that spans rows is not entirely
	contained within its own row.
----------------------------------------------------------------------------------------------*/
VwBox * VwTableBox::FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
	Rect * prcSrc, Rect * prcDst)
{
#if 99-99
	{
		StrAnsi sta;
		int xMin = Left();
		int x = xd + rcSrc.left;
		int xMax = Right();
		int yMin = Top();
		int y = yd + rcSrc.top;
		int yMax = Bottom();
		char * pszWhere = (xMin <= x && x <= xMax && yMin <= y && y <= yMax) ? "INSIDE " : "OUTSIDE";
		sta.Format("VwTableBox::FindBoxClicked(}:       %s  %3d %s %3d %s %3d  && %3d %s %3d %s %3d%n",
			pszWhere, xMin, (xMin <= x ? "<=" : "> "), x, (x <= xMax ? "<=" : "< "), xMax,
			yMin, (yMin <= y ? "<=" : "> "), y, (y <= yMax ? "<=" : "< "), yMax);
		::OutputDebugStringA(sta.Chars());
	}
#endif
	if (!m_pboxFirst)
		return NULL;

	// Adjust rcSrc as usual for drawing embedded boxes.
	rcSrc.Offset(-Left(),-Top());

	VwBox * pboxClosest = NULL;
	int dsqMin = INT_MAX; // square of distance from best box so far to target point

	VwBox * pboxRow;  // for looping over rows
	VwTableRowBox * ptabrow;
	VwBox * pboxCell;

	for (pboxRow = FirstBox(); pboxRow; pboxRow = pboxRow->Next())
	{
		ptabrow = dynamic_cast<VwTableRowBox *>(pboxRow);
		Assert(ptabrow);
		Rect rcSrcRow(rcSrc);
		rcSrcRow.Offset(-ptabrow->Left(), -ptabrow->Top());
		for (pboxCell = ptabrow->FirstBox(); pboxCell; pboxCell = pboxCell->Next())
		{
			// Compute point in row source coords for comparison with cell boxes
			int xs = rcDst.MapXTo(xd, rcSrcRow);
			int ys = rcDst.MapYTo(yd, rcSrcRow);
			int dsq = pboxCell->DsqToPoint(xs,ys, rcSrc);
			if (dsq < dsqMin)
			{
				dsqMin = dsq;
				pboxClosest = pboxCell;
			}
		}
	}
	// To get coords relative to the box, we must adjust for the row vertical position,
	// and also for any indent set on the row
	rcSrc.Offset(-pboxClosest->Container()->Left(), -pboxClosest->Container()->Top());
	return pboxClosest->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);
}

/*----------------------------------------------------------------------------------------------
	This gets called as the different parts of a table are identified by the VwEnv as it
	is constructed. It handles rearranging the box lists so that we wind up with all the
	parts of the table identified and all the boxes properly linked up.
----------------------------------------------------------------------------------------------*/
void VwTableBox::ConstructionStage(VwConstructionStage constage)
{
	//switch to another construction stage. Make sure it is a later one,
	//unless in the body stage, which may be repeated.
	Assert(constage > m_constage || m_constage == kcsBody);

	//mark the first and last rows in the group
	VwTableRowBox * ptabrowFirst = dynamic_cast<VwTableRowBox *>(m_pboxFirst);
	VwTableRowBox * ptabrowLast = dynamic_cast<VwTableRowBox *>(m_pboxLast);
	if (ptabrowFirst)
	{
		ptabrowFirst->SetGroupTop(true);
		ptabrowLast->SetGroupBottom(true);
		// Compute column indexes for cells in this group of rows.
		// Doing it like this ensures that cells can't span rows from one table section
		// to another.
		ComputeColumnIndexes(ptabrowFirst,ptabrowLast);
	}

	//if we were constructing header or footer, save them.
	switch(m_constage)
	{
	case kcsHeader:
		m_ptabrowHeader = ptabrowFirst;
		m_ptabrowLastHeader = ptabrowLast;
		m_pboxFirst = m_pboxLast = NULL;
		break;
	case kcsFooter:
		m_ptabrowFooter = ptabrowFirst;
		m_ptabrowLastFooter = ptabrowLast;
		m_pboxFirst = m_pboxLast = NULL;
		break;
	default:
		break;
	}
	//if done constructing body, save pointers, and relink
	//so m_boxes points to the first thing in the whole table,
	//and all the parts follow in succession. This allows the
	//normal pile layout to work with minimal changes, if any.
	if (constage == kcsDone)
	{
		m_ptabrowBody = ptabrowFirst;
		m_ptabrowLastBody = ptabrowLast;
		if (m_ptabrowHeader)
		{
			m_pboxFirst = m_ptabrowHeader;
			// Link the end of the header to the Next non-empty component
			m_ptabrowLastHeader->
				SetNext(m_ptabrowBody ? m_ptabrowBody : m_ptabrowFooter);

		}
		if (m_ptabrowBody)
		{
			// Link the end of the body to the footer, if any
			m_ptabrowLastBody->SetNext(m_ptabrowFooter);
		}
		// The last box of the whole table is the last of the footer, if any;
		// otherwise, if there is a body it is (already) the last box of that;
		// if there is no body or footer, it is the last of the header.
		if (m_ptabrowFooter)
			m_pboxLast = m_ptabrowLastFooter;
		else if (!m_ptabrowBody)
			m_pboxLast = m_ptabrowLastHeader;
	}
	//set the new construction stage.
	m_constage = constage;
}

/*----------------------------------------------------------------------------------------------
	Compute for each box which column it is in. This is normally based on how many boxes
	come before it in the same row, but a box on a previous row which spans multiple rows
	may interfere.
----------------------------------------------------------------------------------------------*/
void VwTableBox::ComputeColumnIndexes(VwTableRowBox * ptabrowFirst, VwTableRowBox * ptabrowLast)
{
	// First pass: set indexes on assumption of no interference from
	// cells on previous rows spanning multiple rows
	VwBox * pbox;
	VwTableRowBox * ptabrow;
	for (ptabrow = ptabrowFirst; ptabrow; ptabrow = dynamic_cast<VwTableRowBox *>(ptabrow->Next()))
	{
		int icolm = 0;
		for (pbox = ptabrow->FirstBox(); pbox; pbox = pbox->Next())
		{
			VwTableCellBox* ptabcell = dynamic_cast<VwTableCellBox *> (pbox);
			ptabcell->_ColPosition(icolm);
			icolm += ptabcell->ColSpan();
		}
		if (ptabrow == ptabrowLast)
			break; //normal exit from loop
	}
	Assert(ptabrow == ptabrowLast); //make sure we exited normally

	// Second pass: find cells that span rows, and adjust the fAffected rows.
	for (ptabrow = ptabrowFirst; ptabrow; ptabrow = dynamic_cast<VwTableRowBox*>(ptabrow->Next()))
	{
		for (pbox = ptabrow->FirstBox(); pbox; pbox = pbox->Next())
		{
			VwTableCellBox* ptabcell = dynamic_cast<VwTableCellBox*> (pbox);
			int ctabrowFix = ptabcell->RowSpan() - 1;
			for (VwTableRowBox* ptabrowFix = dynamic_cast<VwTableRowBox *>(ptabrow->Next());
				ctabrowFix > 0 && ptabrowFix;
				ctabrowFix--, ptabrowFix = dynamic_cast<VwTableRowBox*>(ptabrowFix->Next()))
			{
				// All cells in ptabrowFix whose icolm position is >= the cell above
				// that spans rows need to move over
				for (VwBox * pboxFix = ptabrowFix->FirstBox();
					pboxFix;
					pboxFix = pboxFix->Next())
				{
					VwTableCellBox* ptabcellFix = dynamic_cast<VwTableCellBox *> (pboxFix);
					if (ptabcellFix->ColPosition() >= ptabcell->ColPosition())
					{
						ptabcellFix->_ColPosition(ptabcellFix->ColPosition() +
							ptabcell->ColSpan());
					}
				}

			}
		}
		if (ptabrow == ptabrowLast) break; //normal exit from loop
	}
}

/*----------------------------------------------------------------------------------------------
	Border widths are overridden if otherwise zero and a frame is requested on that side.
----------------------------------------------------------------------------------------------*/
int VwTableBox::BorderTop()
{
	int dzmp = m_qzvps->BorderTop();
	//if already nonzero don't mess with it
	if (dzmp)
		return dzmp;
	return m_frmpos & kvfpAbove ? m_dzmpBorderWidth : 0;
}

int VwTableBox::BorderBottom()
{
	int dzmp = m_qzvps->BorderBottom();
	//if already nonzero don't mess with it
	if (dzmp)
		return dzmp;
	return m_frmpos & kvfpBelow ? m_dzmpBorderWidth : 0;
}

int VwTableBox::BorderLeading()
{
	int dzmp = m_qzvps->BorderLeading();
	//if already nonzero don't mess with it
	if (dzmp)
		return dzmp;
	return m_frmpos & kvfpLhs ? m_dzmpBorderWidth : 0;
}

int VwTableBox::BorderTrailing()
{
	int dzmp = m_qzvps->BorderTrailing();
	//if already nonzero don't mess with it
	if (dzmp)
		return dzmp;
	return m_frmpos & kvfpRhs ? m_dzmpBorderWidth : 0;
}

/*----------------------------------------------------------------------------------------------
	If all four frame sides are drawn and are the same thickness,
	draw a special shadow shape for the border.
----------------------------------------------------------------------------------------------*/
void VwTableBox::DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
	int dysTopBord = MulDivFixZero(BorderTop(), rcSrc.Height(), kdzmpInch);
	int dysBottomBord = MulDivFixZero(BorderBottom(), rcSrc.Height(), kdzmpInch);
	int dxsLeftBord = MulDivFixZero(BorderLeading(), rcSrc.Width(), kdzmpInch);
	int dxsRightBord = MulDivFixZero(BorderTrailing(), rcSrc.Width(), kdzmpInch);

	int dysBorderHeight = MulDivFixZero(m_dzmpBorderWidth, rcSrc.Height(), kdzmpInch);
	int dxsBorderWidth = MulDivFixZero(m_dzmpBorderWidth, rcSrc.Width(), kdzmpInch);

	//if we don't have a four-equal-sides Border, or no Border at all, just draw the usual thing
	if (!(dysTopBord == dysBorderHeight
		&& dysBottomBord == dysBorderHeight
		&& dxsLeftBord == dxsBorderWidth
		&& dxsRightBord == dxsBorderWidth
		&& m_dzmpBorderWidth > 0))
	{
		VwPileBox::DrawBorder(pvg, rcSrc, rcDst);
		return;
	}

	// Margin thicknesses
	int dxsMLeft = MulDiv(m_qzvps->MarginLeading(), rcSrc.Width(), kdzmpInch);
	int dysMTop = MulDiv(m_qzvps->MarginTop(), rcSrc.Height(), kdzmpInch);
	int dxsMRight = MulDiv(m_qzvps->MarginTrailing(), rcSrc.Width(), kdzmpInch);
	int dysMBottom = MulDiv(m_qzvps->MarginBottom(), rcSrc.Height(), kdzmpInch);

	// outside of border rectangle
	int xdLeftBord = rcSrc.MapXTo(dxsMLeft + m_xsLeft, rcDst);
	int ydTopBord = rcSrc.MapYTo(dysMTop + m_ysTop, rcDst);
	int xdRightBord = rcSrc.MapXTo(m_xsLeft + m_dxsWidth - dxsMRight, rcDst);
	int ydBottomBord = rcSrc.MapYTo(m_ysTop + m_dysHeight - dysMBottom, rcDst);

	// Thickness of border. Measure in dest coords, so that the same source
	// thickness always comes out the same drawing thickness.
	int dxdLeftBord = MulDiv(dxsLeftBord, rcDst.Width(), rcSrc.Width());
	int dydTopBord = MulDiv(dysTopBord, rcDst.Height(), rcSrc.Height());
	int dxdRightBord = MulDiv(dxsRightBord, rcDst.Width(), rcSrc.Width());
	int dydBottomBord = MulDiv(dysBottomBord, rcDst.Height(), rcSrc.Height());

	// Make sure dest border thickness is not zero unless source thickness is.
	if (dxsLeftBord && !dxdLeftBord)
		dxdLeftBord = 1;
	if (dysTopBord && !dydTopBord)
		dydTopBord = 1;
	if (dxsRightBord && !dxdRightBord)
		dxdRightBord = 1;
	if (dysBottomBord && !dydBottomBord)
		dydBottomBord = 1;

	// inside of border rectangle, outside of pad rectangle
	int xdLeftPad = xdLeftBord + dxdLeftBord;
	int ydTopPad = ydTopBord + dydTopBord;
	int xdRightPad = xdRightBord - dxdRightBord;
	int ydBottomPad = ydBottomBord - dydBottomBord;

	// draw background, as in normal draw
	if (m_qzvps->BackColor() != kclrTransparent)
	{
		CheckHr(pvg->put_BackColor(m_qzvps->BackColor()));
		CheckHr(pvg->DrawRectangle(xdLeftPad, ydTopPad, xdRightPad, ydBottomPad));
	}

	//draw Border lines.
	//each polygon covers its side with a diagonal at the corner.
	POINT rgptLeft[] =
	{
		{xdLeftBord, ydTopBord},
		{xdLeftPad, ydTopPad},
		{xdLeftPad,ydBottomPad},
		{xdLeftBord, ydBottomBord}
	};
	POINT rgptTop[] =
	{
		{xdLeftBord, ydTopBord},
		{xdRightBord, ydTopBord},
		{xdRightPad,ydTopPad},
		{xdLeftPad, ydTopPad}
	};
	POINT rgptRight[] =
	{
		{xdRightBord, ydTopBord},
		{xdRightPad, ydTopPad},
		{xdRightPad,ydBottomPad},
		{xdRightBord, ydBottomBord}
	};
	POINT rgptBottom[] =
	{
		{xdLeftBord, ydBottomBord},
		{xdRightBord, ydBottomBord},
		{xdRightPad,ydBottomPad},
		{xdLeftPad, ydBottomPad}
	};

	COLORREF rgbBaseColor = m_qzvps->BorderColor();
	int blue = GetBValue(rgbBaseColor);
	int green = GetGValue(rgbBaseColor);
	int red = GetRValue(rgbBaseColor);
	//draw 80% of base color on bottom right
	//we want to reduce darkness by 20%, that is, remove20%
	//of difference between value and 256

	//for example: if Border color is 3/4 red (red=192, blue=green=0),
	//we want the color which contains 20% intensity of blue and green,
	//and a little more red than 192: 20% of the 64 points by which
	//it is less than maximum intensity.
	int dBlue = 256-blue; //blue darkness, amount of blue less than white
	int dGreen = 256-green;
	int dRed = 256-red;
	COLORREF rgbRightBottomColor = RGB(256-dRed*80/100,
		256-dGreen*80/100, 256-dBlue*80/100);
	//draw 40% of base color on top left
	COLORREF rgbLeftTopColor = RGB(256-dRed*40/100,
		256-dGreen*40/100, 256-dBlue*40/100);

	pvg->put_ForeColor(rgbRightBottomColor);
	pvg->put_BackColor(rgbRightBottomColor);
	pvg->DrawPolygon(4, rgptRight);
	pvg->DrawPolygon(4, rgptBottom);
	pvg->put_ForeColor(rgbLeftTopColor);
	pvg->put_BackColor(rgbLeftTopColor);
	pvg->DrawPolygon(4, rgptTop);
	pvg->DrawPolygon(4, rgptLeft);
}


/*----------------------------------------------------------------------------------------------
	Modify the table column widths. cvlen must be <= the number of columns in the table.

	ENHANCE: For now, every column must be updated, so cvlen must equal the number of columns in
	the table. Also, the units of every column as well as the table as a whole will become
	kunPoint1000. This will need to be changed. How do you update the width of the table when
	changing columns that aren't kunPoint1000? And what should happen when the table unit is not
	kunPoint1000?
	Right now if the table is wider than the sum of its' columns before changing the width,
	the final table width is wider than the sum of its' columns by the same amount, if everything
	is kunPoint1000 to start with. If not, the table width ends up being the sum of the column
	widths.
----------------------------------------------------------------------------------------------*/
void VwTableBox::SetTableColWidths(VwLength * prgvlen, int cvlen)
{
	AssertArray(prgvlen, cvlen);

	// ENHANCEMENT 2048.
	if (cvlen != Columns())
		return;

	Assert((uint)cvlen <= (uint)Columns());

	Assert(cvlen == Columns());

	int dxpOld = m_vlenRequestWidth.nVal;

	int dxpColumnsOld = 0;
	int dxpTable = 0;
	for (int ivlen = 0; ivlen < cvlen; ivlen++)
	{
		Assert(prgvlen[ivlen].unit == kunPoint1000);
		dxpColumnsOld += m_vcolspec[ivlen].WidthVLen().nVal;
		m_vcolspec[ivlen].SetWidthVLen(prgvlen[ivlen]);
		dxpTable += prgvlen[ivlen].nVal;
	}
	if (m_vlenRequestWidth.unit == kunPoint1000)
	{
		m_vlenRequestWidth.nVal = dxpTable + max((dxpOld - dxpColumnsOld), 0);
	}
	else
	{
		m_vlenRequestWidth.unit = kunPoint1000;
		m_vlenRequestWidth.nVal = dxpTable;
	}
}


/*----------------------------------------------------------------------------------------------
	Table layout.
----------------------------------------------------------------------------------------------*/
void VwTableBox::DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	ComputeColumnWidths(pvg, dxAvailWidth);

	//should be declared as for-loop block vars, but MFC won't let you re-use names.
	VwBox* pboxRow; //before cast, for looping with Next()
	VwTableRowBox* ptabrow;
	VwBox* pboxCell; //before cast
	VwTableCellBox* ptabcell;
	int icolm;
	int ccolmSpan;

	// Figure cell border status BEFORE laying them out.
	ComputeCellBorders();

	// Lay out each individual cell; this determines their natural height
	for (pboxRow = FirstBox(); pboxRow; pboxRow = pboxRow->Next())
	{
		ptabrow = dynamic_cast<VwTableRowBox *>(pboxRow);
		for (pboxCell = ptabrow->FirstBox(); pboxCell; pboxCell = pboxCell->Next())
		{
			ptabcell = dynamic_cast<VwTableCellBox *>(pboxCell);
			icolm = ptabcell->ColPosition();
			ccolmSpan = ptabcell->ColSpan();
			// The available width for laying out the cell is the sum of
			// the widths of its columns. (Space between columns is part
			// of the cell's own margin/border/padding.)
			int twAvail = 0;
			for (int i = 0; i < ccolmSpan; i++, icolm++)
			{
				twAvail += m_vcolspec[icolm].Width();
			}
			ptabcell->DoLayout(pvg, twAvail, -1, fSyncTops);
		}
	}

	// Knowing the natural size and position of each cell, adjust so that all cells
	// in the same row line up.
	ComputeRowAndCellSizes();

	int dxsOrigWidth = m_dxsWidth;

	//now we fire the usual algorithm to position the rows.
	// Pass the actual table width, not the real available width, because it doesn't
	// affect the PileBox layout algorithm except that it sets the width of the rows to
	// the value passed, and if it is wrong, any table row borders will be drawn wrongly.
	VwPileBox::DoLayout(pvg, dxsOrigWidth, dxpAvailOnLine, fSyncTops);
	//but override its result about the width, since we have
	//not given the rows any meaningful width.
	m_dxsWidth = dxsOrigWidth;
	return;
}

/*----------------------------------------------------------------------------------------------
	Compute the widths of the columns. The idea is to first compute the width of the columns
	where it is given absolutely or as a percent of the available width. Then the remaining
	width, if any, is distributed to any columns that have relative widths.
----------------------------------------------------------------------------------------------*/
void VwTableBox::ComputeColumnWidths(IVwGraphics * pvg, int dxAvailWidth)
{
	switch(m_vlenRequestWidth.unit)
	{
	case kunPoint1000:
		{
			int dxpInch;
			CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
			m_dxsWidth = MulDiv(m_vlenRequestWidth.nVal, dxpInch, kdzmpInch);
			break;
		}
	case kunPercent100:
		// If the screen is 1600 points wide and we want 100% the produce is 1600*10000*20
		// which is 320,000,000: still OK for 32 bits.
		m_dxsWidth = m_vlenRequestWidth.nVal * dxAvailWidth / 10000;
		break;
	default:
		Assert(false); // relative does not make sense here.
	}
	int dxpInch;
	CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
	int dxpSurroundWidth = SurroundWidth(dxpInch);
	int dxInnerAvailWidth = m_dxsWidth - dxpSurroundWidth;
	int nSumRelative = 0; //sum of relative column widths
	int dxSumFixed = 0; //sum of fixed widths
	int ccolmRelative = 0; // number of columns using relative width
	VwColumnSpec * pcolspec;
	VwLength vlenColWidth;
	int dxColWidth;
	int i; //for use in two loops
	for (i = 0; i < m_ccolm; i++)
	{
		pcolspec = &m_vcolspec[i];
		vlenColWidth = pcolspec->WidthVLen();
		if (vlenColWidth.unit == kunRelative)
		{
			nSumRelative += vlenColWidth.nVal;
			ccolmRelative++;
		}
		else
		{
			if (i == m_ccolm && !ccolmRelative)
			{
				//last column, and there are no relative ones; make this
				//one use exactly the left over space.
				dxColWidth = dxInnerAvailWidth - dxSumFixed;
			}
			else if (vlenColWidth.unit == kunPoint1000)
			{
				dxColWidth = MulDiv(vlenColWidth.nVal, dxpInch, kdzmpInch);
			}
			else // kunPercent100
			{
				dxColWidth = vlenColWidth.nVal * dxInnerAvailWidth / 10000;
			}
			pcolspec->SetWidth(dxColWidth);
			dxSumFixed += dxColWidth;
		}
	}
	if (ccolmRelative)
	{
		//columns will be relative to remainder of available width
		int dxSumRelative = dxInnerAvailWidth - dxSumFixed;

		int dxSumInitialRelatives = 0; //width of all but last

		for (i = 0; i<m_ccolm; i++)
		{
			pcolspec = &m_vcolspec[i];
			vlenColWidth = pcolspec->WidthVLen();
			if (vlenColWidth.unit == kunRelative)
			{
				--ccolmRelative;
				if (ccolmRelative)
				{//not the last
					dxColWidth = dxSumRelative * vlenColWidth.nVal / nSumRelative;
					pcolspec->SetWidth(dxColWidth);
					dxSumInitialRelatives += dxColWidth;
				} else
				{
					//last relative column, give it exactly the remainder
					pcolspec->SetWidth(dxSumRelative - dxSumInitialRelatives);
					break;
				}
			}
		}
	}
	//Now set column left positions.
	int dxSoFar = 0;
	for (i = 0; i < m_ccolm; i++)
	{
		pcolspec = &m_vcolspec[i];
		dxColWidth = pcolspec->Width();
		pcolspec->SetLeft(dxSoFar);
		dxSoFar += dxColWidth;
	}
}

/*----------------------------------------------------------------------------------------------
	Table relayout. See  VwBox::Relayout for description of purpose and arguments.
----------------------------------------------------------------------------------------------*/
bool VwTableBox::Relayout(IVwGraphics * pvg, int dxAvailWidth,
	VwRootBox * prootb, FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi)
{
	if (m_dysHeight == 0)
	{ //new box, needs complete layout.
		this->DoLayout(pvg, dxAvailWidth);
		// But, may not really be new. If in the fixmap, the contract calls for us to
		// invalidate the old rectangle. (Not doing this causes a problem if the last table
		// row changes height without anything else changing.)
		Rect vrect;
		VwBox * pboxThis = this; // seems to be needed for calling Retrieve...
		if (pfixmap->Retrieve(pboxThis, &vrect))
		{
			Root()->InvalidateRect(&vrect);
		}
		return true;
	}
	Rect vrect;
	VwBox * pboxTable = this; // Retrieve requires non-const VwBox argument
	if (!pfixmap->Retrieve(pboxTable, &vrect))
		return false; //unmodified, needs nothing.
	Root()->InvalidateRect(&vrect); // called for by contract.

	//twAvailWidth has not changed from original layout, so column widths
	//have not changed either.

	VwBox * pboxRow; //before cast, for looping with Next()
	VwTableRowBox * ptabrow;
	VwBox * pboxCell; //before cast
	VwTableCellBox * ptabcell;
	int icolm;
	int ccolmSpan;

	Vector<int> vtwRowHeights;

	//number of rows starting from the present one that are fAffected by
	//containing a changed box.
	//Set to 1 when we find a row that is in pfixmap map.
	//Set to larger value when we find a box in pfixmap with crowSpan > 1.
	int ctabrowAffected = 0;

	// Figure cell border status BEFORE laying them out.
	ComputeCellBorders();

	//relayout each individual cell; this determines their natural height
	//in the process note current row heights.
	for (pboxRow = FirstBox(); pboxRow; pboxRow = pboxRow->Next())
	{
		ptabrow = dynamic_cast<VwTableRowBox*>(pboxRow);
		VwBox * pboxTempRow = ptabrow; // Retrieve non-const param
		if (pfixmap->Retrieve(pboxTempRow, &vrect))
		{
			// Make sure any new cells we added to the row have their correct column
			// index!
			// Review JohnT: in case of multi-row cells, we might need to do this
			// for the whole group of rows.
			this->ComputeColumnIndexes(ptabrow, ptabrow);
			//row contains something modified, all its boxes should
			//be re-laid out, since their sizes may have been increased
			//to match something that has changed
			ctabrowAffected = std::max(ctabrowAffected, 1);
			pboxTempRow->CheckBoxMap(pmmbi, prootb);
		}
		vtwRowHeights.Push(ptabrow->Height());
		for (pboxCell = ptabrow->FirstBox(); pboxCell; pboxCell = pboxCell->Next())
		{
			ptabcell = dynamic_cast<VwTableCellBox*>(pboxCell);
			icolm = ptabcell->ColPosition();
			ccolmSpan = ptabcell->ColSpan();
			int dxAvail = 0;
			for (int i=0; i < ccolmSpan; i++, icolm++)
			{
				dxAvail += m_vcolspec[icolm].Width();
			}
			//if the cell spans more than the number of rows we are
			//currently planning to recompute, and if it is a modified box,
			//increase ctabrowAffected.
			VwBox * pboxTempCell = ptabcell; // Retrieve param must be plain box
			if (ptabcell->RowSpan() > ctabrowAffected
				&& pfixmap->Retrieve(pboxTempCell, &vrect))
			{
				ctabrowAffected = ptabcell->RowSpan();
			}
			//if this row needs recomputing, redo layout of the cell unconditionally.
			if (ctabrowAffected)
				ptabcell->DoLayout(pvg, dxAvail);
			else
			{
				if (ptabcell->RowSpan() == 1)
					ptabcell->Relayout(pvg, dxAvail, prootb, pfixmap);
				else
				{
					//this row is not affected directly by the change, but
					//this cell may overlap a row that is affected. For
					//example, the height of this cell may have been
					//increased to match something in a later row which
					//has now shrunk
					//We need to do a full layout of the cell if any of the rows
					//it covers is affected.
					bool fAffected = false;
					VwTableRowBox* ptabrow2 = dynamic_cast<VwTableRowBox *>(ptabrow->Next());
					for (int i = ptabcell->RowSpan() - 1; i > 0; i--)
					{
						pboxTempRow = ptabrow2;
						if (pfixmap->Retrieve(pboxTempRow, &vrect))
						{
							fAffected = true;
							break;
						}

						ptabrow2 = dynamic_cast<VwTableRowBox *>(ptabrow2->Next());
					}
					if (fAffected)
					{
						ptabcell->DoLayout(pvg, dxAvail);
					}
					else
					{
						ptabcell->Relayout(pvg, dxAvail, prootb, pfixmap);
					}
				}
			}
		}
		// As we go on to the next row, the number of rows needing fixing is reduced by the
		// one we just did.
		ctabrowAffected = std::max(0, ctabrowAffected-1);
	}
	ComputeRowAndCellSizes();

	int dxsOrigWidth = m_dxsWidth;
	AdjustInnerBoxes(pvg, NULL, pmmbi);

	//but override its result about the width, since we have
	//not given the rows any meaningful width.
	m_dxsWidth = dxsOrigWidth;
	// And the whole table will be invalidated...this allows us to ignore the Relayout values
	// returned by children.
	return true;
}

/*----------------------------------------------------------------------------------------------
	Call this BEFORE laying out the individual cells to figure out which borders of the table
	each cell is adjacent to.
----------------------------------------------------------------------------------------------*/
void VwTableBox::ComputeCellBorders()
{
	VwBox * pboxRow; //before cast, for looping with Next()
	VwTableRowBox * ptabrow;
	VwBox * pboxCell; //before cast
	VwTableCellBox * ptabcell;
	int crowSpan;

	Vector<VwTableCellBox *> vtabcellMultiRowBoxes;

	// Set the flags that tell each cell which borders it is adjacent to.
	// All cells are initialized to be adjacent to nothing.
	// Note that we must not use height or width of cells or call their
	// Layout methods until we have computed which borders they are adjacent to,
	// as that affects their height and width.
	ptabrow = dynamic_cast<VwTableRowBox *>(FirstBox());
	if (!ptabrow)
		return;
	for (pboxCell = ptabrow->FirstBox(); pboxCell; pboxCell = pboxCell->Next())
	{
		ptabcell = dynamic_cast<VwTableCellBox*>(pboxCell);
		ptabcell->m_grfcsEdges = (CellsSides)((int)ptabcell->m_grfcsEdges | (int) kfcsTop);
	}
	// This marks all the bottom cells, unless there is one that is not in
	// the last row because it spans multiple rows.
	ptabrow = dynamic_cast<VwTableRowBox *>(LastBox());
	for (pboxCell = ptabrow->FirstBox(); pboxCell; pboxCell = pboxCell->Next())
	{
		ptabcell = dynamic_cast<VwTableCellBox*>(pboxCell);
		ptabcell->m_grfcsEdges = (CellsSides)((int)ptabcell->m_grfcsEdges | (int) kfcsBottom);
	}
	// Mark left and right cells; get list of multi-row cells
	for (pboxRow = FirstBox(); pboxRow; pboxRow = pboxRow->Next())
	{
		ptabrow = dynamic_cast<VwTableRowBox *>(pboxRow);
		ptabcell = dynamic_cast<VwTableCellBox*>(ptabrow->FirstBox());
		ptabcell->m_grfcsEdges = (CellsSides)((int)ptabcell->m_grfcsEdges | (int) kfcsLeading);
		ptabcell = dynamic_cast<VwTableCellBox*>(ptabrow->LastBox());
		ptabcell->m_grfcsEdges = (CellsSides)((int)ptabcell->m_grfcsEdges | (int) kfcsTrailing);
		for (pboxCell = ptabrow->FirstBox(); pboxCell; pboxCell = pboxCell->Next())
		{
			ptabcell = dynamic_cast<VwTableCellBox*>(pboxCell);
			if (ptabcell->RowSpan() > 1)
			{
				crowSpan = ptabcell->RowSpan();
				ptabrow = dynamic_cast<VwTableRowBox *>(ptabcell->Container());
				Assert(ptabrow);
				int crowAvail = 1;  // rows available for this cell, may be less than its span
				VwTableRowBox * ptabrowT = ptabrow;
				while (crowAvail <= crowSpan - 1 && !ptabrowT->GroupBottom())
				{
					crowAvail++;
					ptabrowT = dynamic_cast<VwTableRowBox *>(ptabrowT->Next());
				}
				//If user asked for too many rows, now we can clean up.
				if (crowAvail < crowSpan)
				{
					ptabcell->_RowSpan(crowAvail);
					crowSpan = crowAvail;
				}
				// If the last row spanned is the last in the table, set bottom flag
				if (!ptabrowT->Next())
					ptabcell->m_grfcsEdges = (CellsSides)((int)ptabcell->m_grfcsEdges | (int) kfcsBottom);
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Assumes individual cells have been laid out or re-layed out.
	Compute row heights, then set sizes and positions of all cells.
----------------------------------------------------------------------------------------------*/
void VwTableBox::ComputeRowAndCellSizes()
{
	VwBox * pboxRow; //before cast, for looping with Next()
	VwTableRowBox * ptabrow;
	VwBox * pboxCell; //before cast
	VwTableCellBox * ptabcell;
	int i;
	int icolm;
	int ccolmSpan;
	int crowSpan;

	Vector<VwTableCellBox *> vtabcellMultiRowBoxes;

	//first cut at row heights is based on contained cells with RowSpan 1.
	for (pboxRow = FirstBox(); pboxRow; pboxRow = pboxRow->Next())
	{
		ptabrow = dynamic_cast<VwTableRowBox *>(pboxRow);
		int dyHeight = 0;
		for (pboxCell = ptabrow->FirstBox(); pboxCell; pboxCell = pboxCell->Next())
		{
			ptabcell = dynamic_cast<VwTableCellBox*>(pboxCell);
			if (ptabcell->RowSpan() > 1)
				vtabcellMultiRowBoxes.Push(ptabcell);
			else
				dyHeight = std::max(dyHeight, ptabcell->Height());
		}
		ptabrow->_Height(dyHeight); //may not be final height, depends on row spans
	}

	// Adjust row heights to allow for multi-row tables.
	for (i = 0; i < vtabcellMultiRowBoxes.Size(); i++)
	{
		ptabcell = vtabcellMultiRowBoxes[i];
		int dyFullHeight = ptabcell->Height();
		crowSpan = ptabcell->RowSpan();
		ptabrow = dynamic_cast<VwTableRowBox *>(ptabcell->Container());
		Assert(ptabrow);
		int crowAvail = 1;  // rows available for this cell, may be less than its span
		int dyAvailHeight = ptabrow->Height(); // space available across all those rows
		while (crowAvail <= crowSpan - 1)
		{
			crowAvail++;
			ptabrow = dynamic_cast<VwTableRowBox *>(ptabrow->Next());
			dyAvailHeight += ptabrow->Height();
		}

		//ENHANCE: it might be a little better to look for any other cells
		//on the same row with the same row span, and find the greatest
		//height of any of them. An even more involved strategy is to deal
		//with all row span 2 cells before working on span 3 ones...
		if (dyFullHeight > dyAvailHeight)
		{
			//need to distribute excess over the rows.
			//spec suggests a weighted average of two strategies:
			//  (a) divide equally between them;
			//  (b) divide in proportion to current heights
			int iwEqual = 50; //percentage weights
			int iwProportion = 100 - iwEqual;
			//can't do proportional if total current height is zero
			if (dyAvailHeight == 0)
			{
				iwEqual = 100; // Divide all excess equally between rows
				iwProportion = 0;
				dyAvailHeight = 1; // prevents 0 / 0.
			}
			ptabrow = dynamic_cast<VwTableRowBox *>(ptabcell->Container());
			int dyTotalFixedHeight = 0; //of all rows adjusted so far
			int dyHeightToAdd = dyFullHeight - dyAvailHeight; //amount to distribute
			// loop for all but the last row spanned
			for (int itabrow = 0; itabrow < crowSpan - 1; itabrow++)
			{
				//weighted average: fully proportional means adding a fraction
				//of the height we need to add proportional to the current height
				//of this row; equal means adding 1/crowSpan fraction of it.
				int dyNewHeight = ptabrow->Height() +
					dyHeightToAdd * ptabrow->Height() * iwProportion / dyAvailHeight / 100
					+ dyHeightToAdd * iwEqual /100 / crowSpan;
				ptabrow->_Height(dyNewHeight);
				dyTotalFixedHeight += dyNewHeight;
				ptabrow = dynamic_cast<VwTableRowBox * >(ptabrow->Next());
			}
			//last row gets all that is left. It should be >= the row's
			//original height due to rounding down in computing what to
			//add to earlier rows.
			Assert(dyFullHeight - dyTotalFixedHeight >= ptabrow->Height());
			ptabrow->_Height(dyFullHeight - dyTotalFixedHeight);
		}
	}

	//at this point all the rows have their correct height. Set cells sizes and
	//positions to fully occupy their available space.
	//Note that a row never lays itself out, and that its margin, pad, and border
	//properties are ignored. Not doing so would mess up alignments.
	//Therefore the first cell always has left = 0, relative to its row.
	for (pboxRow = FirstBox(); pboxRow; pboxRow = pboxRow->Next())
	{
		ptabrow = dynamic_cast<VwTableRowBox*>(pboxRow);
		for (pboxCell = ptabrow->FirstBox(); pboxCell; pboxCell = pboxCell->Next())
		{
			ptabcell = dynamic_cast<VwTableCellBox*>(pboxCell);
			icolm = ptabcell->ColPosition();
			ccolmSpan = ptabcell->ColSpan();
			crowSpan = ptabcell->RowSpan();
			int xsCellLeft = m_vcolspec[icolm].Left();
			//leave top 0, as set by constructor
			ptabcell->Left(xsCellLeft);
			int icolmLast = icolm + ccolmSpan - 1;
			ptabcell->_Width(m_vcolspec[icolmLast].Left() +
				m_vcolspec[icolmLast].Width() - xsCellLeft);
			//height is harder, have to add up rowspan rows
			VwBox * pboxRow2 = ptabrow;
			int dysSumHeight = 0;
			for (i = 0; i < crowSpan; i++, pboxRow2 = pboxRow2->Next())
			{
				VwTableRowBox * ptabrow2 = dynamic_cast<VwTableRowBox *>(pboxRow2);
				dysSumHeight += ptabrow2->Height();
			}
			ptabcell->_Height(dysSumHeight);
		}
	}
}

VwPropertyStore * VwTableBox::RowPropertyStore()
{
	if (!m_qzvpsRowDefault)
	{
		ITsTextPropsPtr qttpRowDefaultKey;
		ITsPropsBldrPtr qtpb;
		qtpb.CreateInstance(CLSID_TsPropsBldr);
		CheckHr(qtpb->SetIntPropValues(ktptCellBorderWidth, ktpvMilliPoint, CellBorderWidth()));
		CheckHr(qtpb->SetIntPropValues(ktptCellSpacing, ktpvMilliPoint, CellSpacing()));
		CheckHr(qtpb->SetIntPropValues(ktptCellPadding, ktpvMilliPoint, CellPadding()));
		CheckHr(qtpb->SetIntPropValues(ktptTableRule, ktpvEnum, m_vwrule));
		CheckHr(qtpb->SetIntPropValues(ktptSetRowDefaults, ktpvEnum, true));
		CheckHr(qtpb->GetTextProps(&qttpRowDefaultKey));
		CheckHr(m_qzvps->ComputedPropertiesForTtp(qttpRowDefaultKey, &m_qzvpsRowDefault));
	}
	return m_qzvpsRowDefault;
}


//:>********************************************************************************************
//:>	VwColumnSpec methods
//:>********************************************************************************************


VwColumnSpec::~VwColumnSpec()
{
}

//:>********************************************************************************************
//:>	VwTableRowBox methods
//:>********************************************************************************************

VwTableRowBox::VwTableRowBox(VwPropertyStore * pzvps)
	:VwGroupBox(pzvps)
{
	m_fGroupTop = false;
	m_fGroupBottom = false;
}

VwTableRowBox::~VwTableRowBox()
{
}

/*----------------------------------------------------------------------------------------------
	Table row layout. Don't mess with the height, since the table bypasses the rows and lays out
	the cells itself. However, a table row should always occupy the full available width,
	to make FindBoxContaining work right.
----------------------------------------------------------------------------------------------*/
void VwTableRowBox::DoLayout(IVwGraphics* pvg, int dxsAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	m_dxsWidth = dxsAvailWidth;
	return;
}

VwTableCellBox * ContainingCell(VwBox * pbox)
{
	for (VwGroupBox * pgbox = pbox->Container(); pgbox; pgbox = pgbox->Container())
	{
		VwTableCellBox * ptcbox = dynamic_cast<VwTableCellBox *>(pgbox);
		if (ptcbox != NULL)
			return ptcbox;
	}
	return NULL;
}

int comparePageLines(const void *arg1, const void *arg2)
{
	int bottom1 = ((PageLine *) arg1)->ypBottomOfLine;
	int bottom2 = ((PageLine *) arg2)->ypBottomOfLine;
	if (bottom1 < bottom2)
		return -1;
	if (bottom1 == bottom2)
	{
		// This is typically because they are in different columns of a table.
		// If so, we want the earlier column to come first: this helps keep things in
		// order so footnotes come out in the right order.
		VwTableCellBox * ptcBox1 = ContainingCell(((PageLine *) arg1)->pboxFirst);
		VwTableCellBox * ptcBox2 = ContainingCell(((PageLine *) arg2)->pboxFirst);
		if (ptcBox1 == NULL || ptcBox2 == NULL)
			return 0; // not both in table cells, can't tell which is first.
		if (ptcBox1->Container() != ptcBox2->Container())
			return 0; // can't tell optimal order.
		if (ptcBox1 == ptcBox2)
			return 0; // not sure this can happen; are we comparing the same cell?
		for (VwBox * pbox = ptcBox1; pbox; pbox = pbox->NextOrLazy())
			if (pbox == ptcBox2)
				return -1; // box 2 follows box 1, so consider box 1 smaller
		return 1; // box 1 must follow box 2.
	}
	return 1;
}

/*----------------------------------------------------------------------------------------------
	A table row scans its cells, extracting each of their rows.
----------------------------------------------------------------------------------------------*/
void VwTableRowBox::GetPageLines(IVwGraphics * pvg, PageLineVec & vln)
{
	int ilnMin = vln.Size();
	for (VwBox * pbox = FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		pbox->GetPageLines(pvg, vln);
	}
	int ilnLim = vln.Size();
	// Sort the lines from ilnMin to ilnLim by ypBottomOfLine
	qsort((void *)(vln.Begin() + ilnMin), (size_t)ilnLim - ilnMin, sizeof(PageLine), comparePageLines);
}

//:>********************************************************************************************
//:>	VwTableCellBox methods
//:>********************************************************************************************

VwTableCellBox::VwTableCellBox(VwPropertyStore * pzvps, bool fHead, int crow, int ccolm)
	:VwPileBox(pzvps)
{
	m_fHeader = fHead;
	m_crowSpan = crow;
	m_ccolmSpan = ccolm;
}

VwTableCellBox::~VwTableCellBox()
{
}

/*----------------------------------------------------------------------------------------------
	Cell border widths are overridden if otherwise zero and the border is a
	group boundary and we are requesting group boundaries in that direction.
	Also forced to zero if adjacent to a border of the whole table.
	Occasionally we might regret this, but otherwise, if we have rules between cells,
	we get an extra pixel of border on the margin,too.
----------------------------------------------------------------------------------------------*/
int VwTableCellBox::BorderTop()
{
	VwTableRowBox * ptabrow = dynamic_cast<VwTableRowBox *>(Container());
	Assert(ptabrow);
	VwTableBox * ptable = dynamic_cast<VwTableBox *> (ptabrow->Container());
	Assert(ptable);
	// If this cell is on the top of the table and the table has a top border,
	// we don't want one on the cell too.
	if (m_grfcsEdges & kfcsTop && ptable->BorderTop() != 0)
		return 0;
	int dzmpResult = m_qzvps->BorderTop();
	//if already nonzero don't mess with it
	if (dzmpResult)
		return dzmpResult;
	if (!ptabrow->GroupTop())
		return 0;
	return ptable->GroupBorder() ? 1 : 0;
}

int VwTableCellBox::BorderBottom()
{
	VwTableRowBox * ptabrow = dynamic_cast<VwTableRowBox *>(Container());
	Assert(ptabrow);
	VwTableBox * ptable = dynamic_cast<VwTableBox *> (ptabrow->Container());
	Assert(ptable);
	// If this cell is on the bottom of the table and the table has a bottom border,
	// we don't want one on the cell too.
	if (m_grfcsEdges & kfcsBottom && ptable->BorderBottom() != 0)
		return 0;
	int dzmpResult = m_qzvps->BorderBottom();
	//if already nonzero don't mess with it
	if (dzmpResult)
		return dzmpResult;
	if (!ptabrow->GroupBottom())
		return 0;
	return ptable->GroupBorder() ? 1 : 0;
}

int VwTableCellBox::BorderLeading()
{
	VwTableBox* ptable = dynamic_cast<VwTableBox *>(Container()->Container());
	Assert(ptable);
	if (m_grfcsEdges & kfcsLeading && ptable->BorderLeading() != 0)
		return 0;
	int dzmpResult = m_qzvps->BorderLeading();
	//if already nonzero don't mess with it
	if (dzmpResult)
		return dzmpResult;
	return (ptable->ColumnSpec(m_icolm)->GroupLeft() && ptable->GroupBorder()) ? 1 : 0;
}

int VwTableCellBox::BorderTrailing()
{
	VwTableBox* ptable = dynamic_cast<VwTableBox*>(Container()->Container());
	Assert(ptable);
	if (m_grfcsEdges & kfcsTrailing && ptable->BorderTrailing() != 0)
		return 0;
	int dzmpResult = m_qzvps->BorderTrailing();
	//if already nonzero don't mess with it
	if (dzmpResult)
		return dzmpResult;
	return (ptable->ColumnSpec(m_icolm + m_ccolmSpan - 1)->GroupRight() &&
		ptable->GroupBorder()) ? 1 : 0;
}

/*----------------------------------------------------------------------------------------------
	Cell margins are overridden to base the computation on whether the cell is adjacent to
	a border of the table.
----------------------------------------------------------------------------------------------*/
int VwTableCellBox::MarginTop()
{
	return m_qzvps->MarginTop(m_grfcsEdges);
}

int VwTableCellBox::MarginBottom()
{
	return m_qzvps->MarginBottom(m_grfcsEdges);
}

int VwTableCellBox::MarginLeading()
{
	return m_qzvps->MarginLeading(m_grfcsEdges);
}

int VwTableCellBox::MarginTrailing()
{
	return m_qzvps->MarginTrailing(m_grfcsEdges);
}


/*----------------------------------------------------------------------------------------------
	Differs from the normal draw in that borders are drawn through the margin
	area, if there is no border in the orthogonal direction.
	ENHANCE: Should this be the default? It might cure the gap between borders of adjacent
	paragraphs.
----------------------------------------------------------------------------------------------*/
void VwTableCellBox::DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
	if (BorderTop() || BorderBottom()
		|| BorderLeading() || BorderTrailing()
		|| m_qzvps->BackColor() != kclrTransparent)
	{
		// Margin thicknesses
		int dxsMLeft = MulDiv(m_qzvps->MarginLeading(), rcSrc.Width(), kdzmpInch);
		int dysMTop = MulDiv(m_qzvps->MarginTop(), rcSrc.Height(), kdzmpInch);
		int dxsMRight = MulDiv(m_qzvps->MarginTrailing(), rcSrc.Width(), kdzmpInch);
		int dysMBottom = MulDiv(m_qzvps->MarginBottom(), rcSrc.Height(), kdzmpInch);

		// outside of border rectangle
		int xdLeftBord = rcSrc.MapXTo(dxsMLeft + m_xsLeft, rcDst);
		int ydTopBord = rcSrc.MapYTo(dysMTop + m_ysTop, rcDst);
		int xdRightBord = rcSrc.MapXTo(m_xsLeft + m_dxsWidth - dxsMRight, rcDst);
		int ydBottomBord = rcSrc.MapYTo(m_ysTop + m_dysHeight - dysMBottom, rcDst);

		// Border thickness in twips.
		int dxsLeftBord = MulDivFixZero(this->BorderLeading(), rcSrc.Width(), kdzmpInch);
		int dysTopBord = MulDivFixZero(this->BorderTop(), rcSrc.Height(), kdzmpInch);
		int dxsRightBord = MulDivFixZero(this->BorderTrailing(), rcSrc.Width(), kdzmpInch);
		int dysBottomBord = MulDivFixZero(this->BorderBottom(), rcSrc.Height(), kdzmpInch);

		// Thickness of border. Measure in dest coords, so that the same source
		// thickness always comes out the same drawing thickness.
		int dxdLeftBord = MulDiv(dxsLeftBord, rcDst.Width(), rcSrc.Width());
		int dydTopBord = MulDiv(dysTopBord, rcDst.Height(), rcSrc.Height());
		int dxdRightBord = MulDiv(dxsRightBord, rcDst.Width(), rcSrc.Width());
		int dydBottomBord = MulDiv(dysBottomBord, rcDst.Height(), rcSrc.Height());

		// Make sure dest border thickness is not zero unless source thickness is.
		if (dxsLeftBord && !dxdLeftBord)
			dxdLeftBord = 1;
		if (dysTopBord && !dydTopBord)
			dydTopBord = 1;
		if (dxsRightBord && !dxdRightBord)
			dxdRightBord = 1;
		if (dysBottomBord && !dydBottomBord)
			dydBottomBord = 1;

		// inside of border rectangle, outside of pad rectangle
		int xdLeftPad = xdLeftBord + dxdLeftBord;
		int ydTopPad = ydTopBord + dydTopBord;
		int xdRightPad = xdRightBord - dxdRightBord;
		int ydBottomPad = ydBottomBord - dydBottomBord;

		//don't need to worry about extensions, table cells never are.

		// Draw background
		if (m_qzvps->BackColor() != kclrTransparent)
		{
			CheckHr(pvg->put_BackColor(m_qzvps->BackColor()));
			CheckHr(pvg->DrawRectangle(xdLeftPad, ydTopPad, xdRightPad, ydBottomPad));
		}

		// outside of the box as a whole
		int xdLeft = rcSrc.MapXTo(m_xsLeft, rcDst);
		int ydTop = rcSrc.MapYTo(m_ysTop, rcDst);
		int xdRight = rcSrc.MapXTo(m_xsLeft + m_dxsWidth, rcDst);
		int ydBottom = rcSrc.MapYTo(m_ysTop + m_dysHeight, rcDst);

		// Draw border lines. We initially set the background color because we draw the
		// borders using rectangles, and DrawRectangle uses the background color
		CheckHr(pvg->put_BackColor(m_qzvps->BorderColor()));
		// This one is typical. The top of the rectangle to draw at the left is
		// (a) the normal border top position, if there is a top border;
		// (b) the extreme top of the box, otherwise
		if (xdLeftPad != xdLeftBord)
			CheckHr(pvg->DrawRectangle(
				xdLeftBord,
				(dysTopBord ? ydTopBord : ydTop),
				xdLeftPad,
				(dysBottomBord ? ydBottomBord: ydBottom)));
		if (ydTopBord != ydTopPad)
			CheckHr(pvg->DrawRectangle(
				(dxsLeftBord ? xdLeftBord : xdLeft),
				ydTopBord,
				(dxsRightBord ? xdRightBord : xdRight),
				ydTopPad));
		if (xdRightPad != xdRightBord)
			CheckHr(pvg->DrawRectangle(
				xdRightPad,
				(dysTopBord ? ydTopBord : ydTop),
				xdRightBord,
				dysBottomBord ? ydBottomBord : ydBottom));
		if (ydBottomPad != ydBottomBord)
			CheckHr(pvg->DrawRectangle(
				(dxsLeftBord ? xdLeftBord : xdLeft),
				ydBottomPad,
				(dxsRightBord ? xdRightBord : xdRight),
				ydBottomBord));
	}
}

/*----------------------------------------------------------------------------------------------
	Tables sometimes have very restricted column widths, and often have something to the
	right of them. So, clip drawing to the actual cell.
----------------------------------------------------------------------------------------------*/
void VwTableCellBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTop,
		int dysHeight, bool fDisplayPartialLines)
{
#ifdef _DEBUG
#ifdef _DEBUG_SHOW_BOX
	DebugDrawBorder(pvg, rcSrc, rcDst);
#endif
#endif

	// The space we should draw in, in
	int dxsMLeft = MulDiv(MarginLeading(), rcSrc.Width(), kdzmpInch);
	int dxsLeftBord = MulDivFixZero(BorderLeading(), rcSrc.Width(), kdzmpInch);
	int dysMTop = MulDiv(m_qzvps->MarginTop(), rcSrc.Height(), kdzmpInch);
	int dysTopBord = MulDivFixZero(this->BorderTop(), rcSrc.Height(), kdzmpInch);
	int dxsMRight = MulDiv(m_qzvps->MarginTrailing(), rcSrc.Width(), kdzmpInch);
	int dysMBottom = MulDiv(m_qzvps->MarginBottom(), rcSrc.Height(), kdzmpInch);
	int dxsRightBord = MulDivFixZero(this->BorderTrailing(), rcSrc.Width(), kdzmpInch);
	int dysBottomBord = MulDivFixZero(this->BorderBottom(), rcSrc.Height(), kdzmpInch);
	Rect rcClip(Left() + dxsMLeft + dxsLeftBord, Top() + dysMTop + dysTopBord,
		Right() - dxsMRight - dxsRightBord - 1, VisibleBottom() - dysMBottom - dysBottomBord);
	rcClip.Map(rcSrc, rcDst);
	pvg->PushClipRect(rcClip);
	// Note that we are calling a superclass method, not on a child, so we do NOT adjust
	// the coordinate arguments.
	SuperClass::DrawForeground(pvg, rcSrc, rcDst, ysTop, dysHeight, fDisplayPartialLines);
	pvg->PopClipRect();
}

/*----------------------------------------------------------------------------------------------
	Find the first box contained in the next cell of the table, or NULL if not in a table cell,
	or in the last cell of the table.  (This obscure functionality is needed in some methods of
	VwSelection.)
----------------------------------------------------------------------------------------------*/
VwBox * VwTableCellBox::FirstBoxInNextTableCell()
{
	VwTableCellBox * pcell = dynamic_cast<VwTableCellBox *>(NextRealBox());
	while (pcell)
	{
		VwBox * pbox = pcell->FirstRealBox();
		if (pbox)
			return pbox;
		pcell = dynamic_cast<VwTableCellBox *>(pcell->NextRealBox());
	}
	VwTableRowBox * prow = dynamic_cast<VwTableRowBox *>(Container()->NextRealBox());
	while (prow)
	{
		pcell = dynamic_cast<VwTableCellBox *>(prow->FirstRealBox());
		while (pcell)
		{
			VwBox * pbox = pcell->FirstRealBox();
			if (pbox)
				return pbox;
			pcell = dynamic_cast<VwTableCellBox *>(pcell->NextRealBox());
		}
		prow = dynamic_cast<VwTableRowBox *>(prow->NextRealBox());
	}
	return NULL;
}

// A table cell's width doesn't depend on its content, so whatever width we
// already determined should be OK.
int VwTableCellBox::AvailWidthForChild(int dpiX, VwBox * pboxChild)
{
	return Width() - SurroundWidth(dpiX);
}

/*----------------------------------------------------------------------------------------------
	Get the column with index iColumn from table row pRow
----------------------------------------------------------------------------------------------*/
VwTableCellBox * GetColumn(VwTableRowBox * pRow, bool fReal, VwTableCellBox * pStartSearch)
{
	int iColumn;
	iColumn = pStartSearch->ColPosition();
	VwTableCellBox * pCol =
		dynamic_cast<VwTableCellBox *>(fReal ? pRow->FirstRealBox() : pRow->FirstBox());
	while (pCol)
	{
		VwTableCellBox * pNextCol =
			dynamic_cast<VwTableCellBox *>(fReal ? pCol->NextRealBox() : pCol->NextOrLazy());
		if (pCol->ColPosition() == iColumn || !pNextCol || pNextCol->ColPosition() > iColumn)
			return pCol;
		pCol = pNextCol;
	}

	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Consider all the boxes of the root as being ordered such that a box
	and all its descendents are before the next box at the same level, and also, a box
	is before its own descendents. Find the next box in this sequence, starting from this.

	Result may be a lazy box unless fReal is true.
	Result may be a child of the recipient if fIncludeChildren is true.
----------------------------------------------------------------------------------------------*/
VwBox * VwTableRowBox::NextBoxForSelection(VwBox ** ppStartSearch, bool fReal, bool fIncludeChildren)
{
	if (fIncludeChildren)
	{
		// try to go down
		VwBox * pboxNext = fReal ? FirstRealBox() : FirstBox();
		if (pboxNext)
		{
			VwTableCellBox * pCellStart = dynamic_cast<VwTableCellBox*>(*ppStartSearch);
			if (pCellStart)
				return GetColumn(this, fReal, pCellStart);
		}
	}

	return SuperClass::NextBoxForSelection(ppStartSearch, fReal, fIncludeChildren);
}

/*----------------------------------------------------------------------------------------------
	Consider all the boxes of the root as being ordered such that a box
	and all its descendents are before the next box at the same level, and also, a box
	is before its own descendents. Find the next box in this sequence, starting from this.

	Result may be a lazy box unless fReal is true.
	Result may be a child of the recipient if fIncludeChildren is true.
----------------------------------------------------------------------------------------------*/
VwBox * VwTableCellBox::NextBoxForSelection(VwBox ** ppStartSearch, bool fReal, bool fIncludeChildren)
{
	if (fIncludeChildren)
	{
		// try to go down
		VwBox * pboxNext = fReal ? FirstRealBox() : FirstBox();
		if (pboxNext)
			return pboxNext;
	}

	// Check the table to see if we should select in the same column only
	VwTableRowBox * pRow = dynamic_cast<VwTableRowBox*>(Container());
	VwTableBox * pTable = dynamic_cast<VwTableBox*>(pRow->Container());
	if (pTable->IsOneColumnSelect())
	{
		// Need to get same column in the next row
		VwTableRowBox * pNextRow =
			dynamic_cast<VwTableRowBox *>(fReal ? pRow->NextRealBox() : pRow->NextOrLazy());
		if (pNextRow)
		{
			VwTableCellBox * pCellStart = dynamic_cast<VwTableCellBox*>(*ppStartSearch);
			if (!pCellStart)
				pCellStart = this;

			return GetColumn(pNextRow, fReal, pCellStart);
		}

		// There wasn't another row in this table, so look in the next table. We don't
		// want to search children in this case, as we will find this same box again.
		*ppStartSearch = this;
		return pTable->NextBoxForSelection(ppStartSearch, fReal, false);
	}

	return SuperClass::NextBoxForSelection(ppStartSearch, fReal, fIncludeChildren);
}
