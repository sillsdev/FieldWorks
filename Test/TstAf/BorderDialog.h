/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: BorderDialog.h
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Header file for the Paragraph Border Dialog class.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef BORDERDIALOG_H_INCLUDED
#define BORDERDIALOG_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Border Dialog. It is an abstract class.
	Derived classes are BorderDialogPara and BorderDialogTable.
	Hungarian: brd.
----------------------------------------------------------------------------------------------*/

// Global color set used by the application and within data.
// extern ColorTable g_ColorTable;

class FmtBorderDlg : public AfDialog
{
public:
	FmtBorderDlg();
	virtual ~FmtBorderDlg();

	void SetDialogValues(COLORREF crColor, int fBorders, int uWidth);
	void GetDialogValues(COLORREF * pcrColor, int * pfBorders, int * pnWidth);


protected:
	// Constants.
	enum {
	kfTop = 0x1,
	kfBottom = 0x2,
	kfLeft = 0x4,
	kfRight = 0x8,
	kfRows = 0x10,
	kfColumns = 0x20,
	kfBox = kfTop | kfBottom | kfLeft | kfRight,
	kfGrid = kfBox | kfRows | kfColumns,


	kcWidths = 9,	// Number of different widths of borders.
	knPenFactor = 128000,	// Width of pen for drawing borders in width box and preview diagram
							// expressed the number of diagram widths per thousandths of a point.
	kcListHeight = 24,	// Height of each selection in Width combo box.
	kchBitmaps = 10,	// Number of bitmaps which may be required.
	};

	enum {NONE, NONE_S, ALL, ALL_S, BOX, BOX_S, GRID, GRID_S, NONET, NONET_S}; // Bitmap indices.

	static const int krguWidths[kcWidths];
	static const int krguBitmapIds[kchBitmaps];
	static const char * krgpszWidths[kcWidths];	// Names of width choices as character strings.

	// Member variables.
	COLORREF	m_clrBorder;	// The color of the borders.
	int			m_fBorders;		// The borders currently selected.
	uint		m_nWidth;		// Index (within krguWidths[]) of width of borders.

	HANDLE			m_rghBitmaps[kchBitmaps];	// Bitmap handles.

	bool FDlgProc(uint wm, WPARAM wp, LPARAM lp);

	bool OnInitDlg(WPARAM wp, LPARAM lp);
	bool OnMeasureItem(int ctid, MEASUREITEMSTRUCT * pmis);
	bool OnDrawItem(int ctid, DRAWITEMSTRUCT * pdis);
	bool OnCommand(int ctid, int nc, HWND hctl);

	// Pure virtual functions.
	virtual void DrawDiagram(HDC hDC, HWND hwndItem, RECT rcItem) = 0;
	virtual void SetCheckBoxes() = 0;
	virtual void SetImages() = 0;

	// Helper functions.
	static void DrawLines(HDC hDC, int nWidth, int nSpacing, const RECT& rect);
};




/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Border Dialog for paragraphs.
	Hungarian: brdp.
----------------------------------------------------------------------------------------------*/

class FmtBorderParaDlg : public FmtBorderDlg
{
public:
	FmtBorderParaDlg(void);
	virtual ~FmtBorderParaDlg(void);

	void DrawDiagram(HDC hDC, HWND hwndItem, RECT rcItem);
	void SetCheckBoxes();
	void SetImages();

};

typedef AfSmartPtr<FmtBorderParaDlg> FmtBorderParaDlgPtr;

#endif  /*BORDERDIALOG_H_INCLUDED*/
