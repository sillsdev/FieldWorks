/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwTableBox.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Contains the classes VwTableBox, VwTableRowBox, and VwTableCellBox, which implement the
	view of a table.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWTABLEBOX_INCLUDED
#define VWTABLEBOX_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: VwColumnSpec
Description: Gives information about one column of a table.
Hungarian: colspec
----------------------------------------------------------------------------------------------*/
class VwColumnSpec
{
public:
	VwColumnSpec()
	{
		m_dxsWidth = 0;
		m_vlenWidth.nVal = 0;
		m_vlenWidth.unit = kunPoint1000;
		m_fGroupLeft = false;
		m_fGroupRight = false;
	}
	virtual ~VwColumnSpec();

	// reinstate when we do serialization
	//virtual void deserialize(std::istream& in);
	//virtual void serialize(std::ostream &out);

	bool GroupLeft()
	{
		return m_fGroupLeft;
	}
	void SetGroupLeft(bool f)
	{
		m_fGroupLeft = f;
	}

	bool GroupRight()
	{
		return m_fGroupRight;
	}
	void SetGroupRight(bool f)
	{
		m_fGroupRight = f;
	}

	VwLength WidthVLen()
	{
		return m_vlenWidth;
	}
	void SetWidthVLen(VwLength vlen)
	{
		m_vlenWidth = vlen;
	}

	int Width()
	{
		return m_dxsWidth;
	}
	void SetWidth(int dxs)
	{
		m_dxsWidth = dxs;
	}

	int Left()
	{
		return m_xsLeft;
	}
	void SetLeft(int xs)
	{
		m_xsLeft = xs;
	}

protected:
	int m_xsLeft;

	// m_vlenWidth is the real permanent width of the column,
	// but we cache its pixel value in m_dxsWidth for rapid use
	int m_dxsWidth;
	VwLength m_vlenWidth;

	bool m_fGroupLeft; //group boundary (or table edge) on left of this column
	bool m_fGroupRight; //group boundary (or table edge) on right of this column
};

enum VwConstructionStage
{
	kcsInit, kcsHeader, kcsFooter, kcsBody, kcsDone
}; // Hungarian constage

/*----------------------------------------------------------------------------------------------
Class: VwTableBox
Description:
Hungarian: table
----------------------------------------------------------------------------------------------*/
class VwTableBox : public VwPileBox
{
	friend class VwBox; //can use protected constructor
protected:
	VwTableBox()
		:VwPileBox()
	{
	}
public:
	// Static methods

	// Constructors/destructors/etc.
	VwTableBox(VwPropertyStore * pzvps, int cCols, VwLength vlWidth, int twBorder,
		VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule,
		int twSpacing, int twPadding, bool fSelectOneCol);
	virtual ~VwTableBox();
	virtual short SerializeKey()
	{
		return 12;
	}

	VwPropertyStore * RowPropertyStore();

	// Default border thickness is 1 if rules are to be drawn in the
	// requested direction, otherwise zero.
	// We don't use the defined constants for the first two because those include
	// the group bit so we would get them for groups as well.
	int xCellBorder()
	{
		return (m_vwrule & kvrlColsNoGroups) ? 1 : 0;
	}
	int yCellBorder()
	{
		return (m_vwrule & kvrlRowNoGroups) ? 1 : 0;
	}
	bool GroupBorder()
	{
		return m_vwrule & kvrlGroups;
	}

	VwColumnSpec * ColumnSpec(int icolm)
	{
		return &m_vcolspec[icolm];
	}

	int CellBorderWidth()
	{
		return m_dzmpBorderWidth;
	}
	int CellSpacing()
	{
		return m_dzmpCellSpacing;
	}
	int CellPadding()
	{
		return m_dzmpCellPadding;
	}

	// Other public methods
	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual bool Relayout(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine = -1, BoxIntMultiMap * pmmbi = NULL);
	virtual VwBox * FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst);
	void ConstructionStage(VwConstructionStage constage);
	void ComputeColumnIndexes(VwTableRowBox * ptabrowFirst, VwTableRowBox * ptabrowLast);
	virtual int BorderLeading();
	virtual int BorderTrailing();
	virtual int BorderBottom();
	virtual int BorderTop();
	virtual void DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	VwConstructionStage ConstructionStage()
	{
		return m_constage;
	}

	int ConstructCol()
	{
		return m_ccolmConstruct;
	}
	void SetConstructCol(int ccolm)
	{
		m_ccolmConstruct = ccolm;
	}

	int Columns()
	{
		return m_ccolm;
	}

	void SetTableColWidths(VwLength * prgvlen, int cvlen);
	virtual OLECHAR * Name()
	{
		return L"Table";
	}
	virtual int Role()
	{
		return ROLE_SYSTEM_TABLE;
	}
	bool IsOneColumnSelect()
	{
		return m_fSelectOneCol;
	}

protected:
	// Member variables
	// Special Hungarian: colm = column in table.
	VwConstructionStage m_constage;
	int m_ccolm;  //number of columns
	int m_ccolmConstruct; //number of columns so far specified using makeColumns
							//or makeColumnGroups; only relevant in stage kiInit.
	VwLength m_vlenRequestWidth; //by user, as opposed to inherited m_width actual width
	bool m_fSelectOneCol; // true to select only one column

	int m_dzmpBorderWidth;

	VwAlignment m_vwalign;
	VwFramePosition m_frmpos;
	VwRule m_vwrule;
	int m_dzmpCellSpacing;
	int m_dzmpCellPadding;

	typedef Vector<VwColumnSpec> ColSpecs; // Hungarian vcolspec
	ColSpecs m_vcolspec;

	// header, footer, and body: chains of VwTableRowBoxes
	// during construction, new rows are added to m_pboxFirst by the default
	// mechanism for group boxes, of which Table is a subclass.
	// When we switch construction stages, we save the current chain as
	// header or footer.
	// Once all is constructed, we link the rows in the order header, body, footer,
	// and make the body ones point to the body stuff in the middle.
	// m_pboxFirst is adjusted to point to the very first header box, so general
	// group box stuff works right.
	VwTableRowBox * m_ptabrowHeader;
	VwTableRowBox * m_ptabrowLastHeader;
	VwTableRowBox * m_ptabrowFooter;
	VwTableRowBox * m_ptabrowLastFooter;
	VwTableRowBox * m_ptabrowBody;
	VwTableRowBox * m_ptabrowLastBody;

	// To get the borders and spacing we want for cells by default, but still
	// allow individual cells to override, we make a special property set that
	// becomes the default for each row of the table. So we can reuse them,
	// they are stored in the parent property store keyed by a special Ttp. Here
	// we have a pointers to the resulting property store.
	VwPropertyStorePtr m_qzvpsRowDefault;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	void ComputeCellBorders();
	void ComputeRowAndCellSizes();
	void ComputeColumnWidths(IVwGraphics * pvg, int dxsAvailWidth);
};

class VwTableRowBox : public VwGroupBox
{
	typedef VwGroupBox SuperClass;
	friend class VwBox; //can use protected constructor
protected:
	VwTableRowBox() :VwGroupBox()
	{
	}
public:
	VwTableRowBox(VwPropertyStore * pzvps);
	virtual short SerializeKey()
	{
		return 14;
	}
protected:
	bool m_fGroupTop; //true if row is top in a group
	bool m_fGroupBottom; //true if row is bottom in a group
public:
	virtual void DoLayout(IVwGraphics* pvg, int dxsAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual ~VwTableRowBox();

	bool GroupTop()
	{
		return m_fGroupTop;
	}
	void SetGroupTop(bool f)
	{
		m_fGroupTop = f;
	}

	bool GroupBottom()
	{
		return m_fGroupBottom;
	}
	void SetGroupBottom(bool f)
	{
		m_fGroupBottom = f;
	}
	virtual OLECHAR * Name()
	{
		return L"Row";
	}
	virtual int Role()
	{
		return ROLE_SYSTEM_ROW;
	}
	virtual VwBox * NextBoxForSelection(VwBox ** ppStartSearch, bool fReal = true,
		bool fIncludeChildren = true); // override
	virtual void GetPageLines(IVwGraphics * pvg, PageLineVec & vln);
};

class VwTableCellBox : public VwPileBox
{
	typedef VwPileBox SuperClass;

	friend class VwBox; //can use protected constructor
	friend class VwTableBox; // can manipulate arbitrarily.
protected:
	VwTableCellBox() :VwPileBox()
	{
	}

public:
	virtual short SerializeKey()
	{
		return 13;
	}
protected:
	int m_crowSpan; //number of rows cell covers
	int m_ccolmSpan; //number of columns
	int m_icolm; //column in which it starts
	bool m_fHeader; //true for header cells
	bool m_fSelectOneCol; // true if selecting same column only
	CellsSides m_grfcsEdges; // has bit for each table border cell is adjacent to.

	// Don't try to break table cells across pages.
	virtual bool FindNiceBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
		int ysStart, int * pysEnd, bool fDisregardKeeps)
	{return false;}

public:
	VwTableCellBox(VwPropertyStore * pzvps, bool fHead, int rows, int cols);
	virtual ~VwTableCellBox();

	int ColPosition()
	{
		return m_icolm;
	}
	void _ColPosition(int icolm)
	{
		m_icolm = icolm;
	}

	int ColSpan()
	{
		return m_ccolmSpan;
	}

	int RowSpan()
	{
		return m_crowSpan;
	}
	// Generally set by constructor, but corrected in layout if not enough rows in group
	void _RowSpan(int crow)
	{
		m_crowSpan = crow;
	}


	virtual void DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTop,
		int dysHeight, bool fDisplayPartialLines = false);

	virtual int BorderLeading();
	virtual int BorderTrailing();
	virtual int BorderBottom();
	virtual int BorderTop();
	virtual int MarginLeading();
	virtual int MarginTrailing();
	virtual int MarginBottom();
	virtual int MarginTop();

	virtual VwBox * FirstBoxInNextTableCell();
	virtual OLECHAR * Name()
	{
		return L"Cell";
	}
	virtual int Role()
	{
		return ROLE_SYSTEM_CELL;
	}
	virtual int AvailWidthForChild(int dpiX, VwBox * pboxChild);
	virtual VwBox * NextBoxForSelection(VwBox ** ppStartSearch, bool fReal = true,
		bool fIncludeChildren = true); // override
};

#endif  //VWTABLEBOX_INCLUDED


#if 0 // stuff from old version not yet converted


class VwTableBox : public VwPileBox
{
public:
	virtual void deserialize(std::istream& in, Vw::StyleVector& styles);
	virtual void serialize(std::ostream &out, Vw::BoxVector& starts, Vw::BoxVector& ends, Vw::StyleMap& map);


	VwConstructionStage ConstructionStage() {return m_constage;}

	void constructCol(int v) {m_iConstructCol = v;}

	//default border thickness is 1 if rules are to be drawn in the
	//requested direction, otherwise zero.
	//we don't use the defined constants because those include
	//the group bit so we would get them for groups as well.
	int xCellBorder() {return m_eRule&4 ? 1 : 0;}
	int yCellBorder() {return m_eRule&2 ? 1 : 0;}
	bool groupBorder() {return m_eRule & kiGroups;}


protected:
	bool serializeBoxPointer(std::ostream &out,VwBox* m_pLastBody, VwBox* start, VwBox* end);
	void deserializeBoxPointer(std::istream &in, VwTableRowBox*& m_pHeader);
};

class VwTableRowBox : public VwGroupBox
{
public:
	virtual void deserialize(std::istream& in, Vw::StyleVector& styles);
	virtual void serialize(std::ostream &out, Vw::BoxVector& starts, Vw::BoxVector& ends, Vw::StyleMap& map);
}

class VwTableCellBox : public VwPileBox
{
public:
	virtual void deserialize(std::istream& in, Vw::StyleVector& styles);
	virtual void serialize(std::ostream &out, Vw::BoxVector& starts, Vw::BoxVector& ends, Vw::StyleMap& map);
	void ColSpan(int ccolm)
	{
		m_ccolmSpan = ccolm;
	}
};
#endif
