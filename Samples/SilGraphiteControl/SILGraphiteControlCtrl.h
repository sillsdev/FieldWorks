// SILGraphiteControlCtrl.h : Declaration of the CSILGraphiteControlCtrl ActiveX Control class.
// CSILGraphiteControlCtrl : See SILGraphiteControlCtrl.cpp for implementation.

#pragma once
//#include <afxtempl.h>  // For CList
//#include <afxadv.h>    // For CSharedFile
//#include "GrInclude.h" // Graphite Include files

// For Text Object Model Implementation
#include "TextDocument.h"
#include <atlbase.h>
#include "LinkedStack.h" // For Undo/Redo Stack
#define CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>
#include <atlcoll.h>

// For HTML input:
class FileOrBuffer
{
	friend class CSILGraphiteControlCtrl;

	void SetFile(CStdioFile * pfile)
	{
		m_pfile = pfile;
		m_pchBuffer = NULL;
		m_pch = NULL;
		m_cchBufLen = 0;
		m_chNext = 0;
	}
	void SetBuffer(char * pch, int cch)
	{
		m_pchBuffer = pch;
		m_pfile = NULL;
		m_pch = m_pchBuffer;
		m_cchBufLen = cch;
	}

	char NextChar()
	{
		if (m_pfile)
		{
			char chRet;
			if (m_chNext != 0)
			{
				chRet = m_chNext; // consume peeked character.
				m_chNext = 0;
			}
			else
				m_pfile->Read(&chRet, 1); // read a new char
			return chRet;
		}
		else
		{
			if (m_pch >= m_pchBuffer + m_cchBufLen)
				return 0;
			else
			{
				char chRet = *m_pch++;
				return chRet;
			}
		}
	}
	char Peek()
	{
		if (m_pfile)
		{
			if (m_chNext == 0)
				// Cache the next character.
				m_pfile->Read(&m_chNext, 1);
			return m_chNext;
		}
		else
		{
			if (m_pch >= m_pchBuffer + m_cchBufLen)
				return 0;
			else
				return *m_pch;
		}
	}

protected:
	CStdioFile * m_pfile;
	char * m_pchBuffer;
	int m_cchBufLen;

	char * m_pch;
	char m_chNext;
};

class CSILGraphiteControlCtrl : public COleControl
{
  DECLARE_DYNCREATE(CSILGraphiteControlCtrl)

public:
  CSILGraphiteControlCtrl();
  ~CSILGraphiteControlCtrl();
  virtual void OnDraw(CDC* pdc, const CRect& rcBounds, const CRect& rcInvalid);
  virtual void DoPropExchange(CPropExchange* pPX);
  virtual void OnResetState();
  virtual DWORD GetControlFlags();

// Implementation
protected:

  BEGIN_OLEFACTORY(CSILGraphiteControlCtrl) // Class factory and guid
	virtual BOOL VerifyUserLicense();
	virtual BOOL GetLicenseKey(DWORD, BSTR FAR*);
  END_OLEFACTORY(CSILGraphiteControlCtrl)

	DECLARE_OLETYPELIB(CSILGraphiteControlCtrl) // GetTypeInfo
  DECLARE_PROPPAGEIDS(CSILGraphiteControlCtrl)  // Property page IDs
  DECLARE_OLECTLTYPE(CSILGraphiteControlCtrl)   // Type name and misc status

// Message maps
  DECLARE_MESSAGE_MAP()

// Windows Message Handlers
  afx_msg int  OnCreate(LPCREATESTRUCT lpCreateStruct); //To Handle WM_CREATE
  afx_msg void OnPaint();  //To handle WM_PAINT
  afx_msg void OnChar(UINT nChar, UINT nRepCnt, UINT nFlags); //To handle WM_CHAR
  afx_msg void OnLButtonDown(UINT nFlags, CPoint point);      //To handle WM_LBUTTONDOWN
  afx_msg void OnLButtonDblClk(UINT nFlags, CPoint npoint);   //To handle WM_LBUTTONDBLCLICK
  afx_msg void OnRButtonDown(UINT nFlags, CPoint point);      //To handle WM_RBUTTONDOWN
  afx_msg void OnMouseMove(UINT nFlags, CPoint point);        //To handle WM_MOUSEMOVE
  afx_msg void OnVScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);  //To handle WM_VSCROLL
  afx_msg void OnWindowPosChanging(WINDOWPOS* lpwndpos); //To handle WM_WINDOWPOSCHANGING
  afx_msg BOOL OnEraseBkgnd(CDC* pDC); //Auto focus and double buffering purpose
  afx_msg UINT OnGetDlgCode();         // to get all keys when the control is active
  afx_msg void OnDestroy();            // called when the window is destroyed or recreated after modi to Windows style bits
  afx_msg void OnTimer(UINT nIDEvent); // for cursor blinking
  afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
  afx_msg void OnKeyUp(UINT nChar, UINT nRepCnt, UINT nFlags);
  afx_msg void OnShowWindow(BOOL bShow, UINT nStatus);
  afx_msg void OnKillFocus(CWnd* pNewWnd); // to tell the active status of the window
  afx_msg void OnSetFocus(CWnd* pOldWnd);

  // Custom MenuBar Message Handlers FOR CONTEXT MENU (to be deleted in Release version)

  afx_msg void OnCut();
  afx_msg void OnCopy();
  afx_msg void OnPaste();
  afx_msg void OnSaveAsHtml();
  afx_msg void OnOpenHtml();
  afx_msg void OnChooseFont();
  afx_msg void OnFont50(); // zooming function
  afx_msg void OnFont75();
  afx_msg void OnFont100();
  afx_msg void OnFont150();
  afx_msg void OnFont200();
  afx_msg void OnAcceleratorSave(); // save/retrieve file
  afx_msg void OnAcceleratorOpen();
#ifdef _DEBUG
  afx_msg void OnInitSILDoulosPiglatinDemo(); // use Doulos Piglatin
  afx_msg void OnInitSimpleGraphiteFont();
  afx_msg void OnInitSILDoulosUnicodeIPA();
  afx_msg void OnInitArialUnicodeMS();
  afx_msg void OnMultiline();
#endif

  afx_msg void AboutBox();
  afx_msg void OnSize(UINT nType, int cx, int cy);

public:
  // Dispatch maps
  DECLARE_DISPATCH_MAP()

  LONG GetSizeX(void); // get client width
  LONG GetSizeY(void); // get client height
  void SetDefaultFont(LPCTSTR fontName);
  void SetDefaultFontSize(int fontSize);
  void SetSelectionFontSize(int fontSize);
  int GetSelectionFontSize(void);
  void SetSelectionFont(LPCTSTR fontName);
  void GetSelectionFont(BSTR*);
  void New(void);
  void Open(BSTR pVar, LONG flags, LONG CodePage); // open file in different format
  void Save(BSTR pVar, LONG flags, LONG CodePage); // save file in different format
  void Copy();
  void Cut();
  void Paste();
  void MoveLeft(bool ShifState); //manual cursor movement using interface
  void MoveRight(bool ShifState);
  void MoveUp(bool ShiftState);
  void MoveDown(bool ShiftState);
  void Redo(/*LONG Count*/);
  void Undo(/*LONG Count*/);
  void ResizeWindow(LONG x, LONG y);       // resize the width and height of the window
  void SetMultiline(VARIANT_BOOL newVal); // set the multiline property
  VARIANT_BOOL GetMultiline(void);        // get the multiline property
  void TextColor(BYTE r, BYTE g, BYTE b); //set the color of selected text
  BSTR GetText(void);           //get the text in the current window
  void SetText(LPCTSTR newVal); //set text in the window
  void SetVerticalScroll(VARIANT_BOOL scrollFlag); // activate/deactivate vertical scroll bar
  void PutHtmlText(LPCTSTR text); // put file in HTML format
  void GetHtmlText(BSTR* text);   //get the HTML format of the current window text
  void HorizontalScroll(VARIANT_BOOL horz); // activate/deactivate horizontal scroll bar
  void BeepIt(VARIANT_BOOL beepy);          // turn on/off typing sound

// Event maps
  DECLARE_EVENT_MAP()

// Dispatch and event IDs
public:
  enum {
	dispidGetHtml              = 49L,  dispidGetFontName          = 48L,  dispidAllowFormatDlg    = 47L,
	dispidBeepIt               = 46L,  dispidHorizontalScroll     = 45L,  dispidMoveDown          = 44L,
	dispidMoveUp               = 43L,  dispidGetHtmlText          = 42L,  dispidPutHtmlText       = 41L,
	eventidKeyDown             =  1L,  dispidTextColor            = 40L,  dispidRedo              = 39L,
	dispidUndo                 = 38L,  dispidMoveLeft             = 29L,  dispidMoveRight         = 28L,
	dispidSave                 = 23L,  dispidOpen                 = 22L,  dispidNew               = 21L,
	dispidGetSizeY             = 20L,  dispidGetSizeX             = 19L,  dispidResizeWindow      = 18L,
	dispidGetSelectionFontSize = 17L,  dispidSetSelectionFontSize = 16L,  dispidSetVerticalScroll = 15L,
	dispidGetMultiline         = 14L,  dispidSetMultiline         = 13L,  dispidmultiline         = 12L,
	dispidSetText              = 10L,  dispidGetText              = 11L,  dispidPaste             =  9L,
	dispidCopy                 =  8L,  dispidCut                  =  7L,  dispidSetTextEnabled    =  6L,
	dispidGetSelectionFont     =  3L,  dispidSetSelectionFont     =  2L,  dispidText              =  1L,
	dispidSetDefaultFont       = 50L,  dispidSetDefaultFontSize   = 51L,  dispidGetAllowFormatDlg = 52L,
	dispidSetAllowFormatDlg    = 53L,
  };

// Constants for UTF8 generation/decoding
enum
{
	kUtf8Min1 = 0x00,
	kUtf8Min2 = 0x80,
	kUtf8Min3 = 0x800,
	kUtf8Min4 = 0x10000,
	kUtf8Min5 = 0x200000,
	kUtf8Min6 = 0x4000000,

	kUtf8Mask1 = 0x7F,
	kUtf8Mask2 = 0x1F,
	kUtf8Mask3 = 0x0F,
	kUtf8Mask4 = 0x07,
	kUtf8Mask5 = 0x03,
	kUtf8Mask6 = 0x01,

	kUtf8Flag1 = 0x00,
	kUtf8Flag2 = 0xC0,
	kUtf8Flag3 = 0xE0,
	kUtf8Flag4 = 0xF0,
	kUtf8Flag5 = 0xF8,
	kUtf8Flag6 = 0xFC,

	kUcs2Max    =     0xFFFF,
	kUtf16Max   =   0x10FFFF,
	kUnicodeMax = 0x7FFFFFFF,
	kReplacementChar = 0xFFFD,

	kSurrogateShift = 10,
	kSurrogateBase  = 0x0010000,
	kSurrogateMask  = 0x3FF,
	kSurrogateHighFirst = 0xD800,
	kSurrogateHighLast  = 0xDBFF,
	kSurrogateLowFirst  = 0xDC00,
	kSurrogateLowLast   = 0xDFFF,

	kByteMask = 0x3F,
	kByteMark = 0x80,
	kByteShift = 6,
};


private:
  // Strictly speaking we should use a hashmap, but the list of fonts is not likely to
  // be long enough for a plain list to be a problem.
  CList<NamedEngine,NamedEngine> m_engineList;

  RECT m_rSrc; //needed for horizontal scrolling.
  RECT m_rDst;
  RECT m_rDstOri;  // original m_rDst (unscrolled) ?????

  int m_ichwIp; // insertion point index
  int m_ichwRp; // range point index, beginning of range (m_ichwIp is end of range)
  bool m_bAssocPrev;
  bool m_bRng;  // do we have active range selection

  CComBSTR m_myText;
  CString m_myString;

  int m_noOfParas;  // number of segments
  bool m_multiline; // to tell whether it's a multiline editor

  // Scrolling variables
  int g_nMaxHScroll, g_nMaxVScroll;         // max scroll value
  int g_nCurrentHScroll, g_nCurrentVScroll; // current scroll value
  SCROLLINFO m_si;
  RECT m_rc;
  bool m_bVScrollBar;   // whether to show the Vertical scroll bar
  bool m_bHScrollBar; // whether to show the Horizontal scroll bar
  int m_acDxWidth;

  CString m_defaultFontName;
  int m_defaultFontSize;
  CString m_currFontName;
  int m_currFontSize;

  CString m_currFileName;

  CList<Range,Range> m_rangeList;
  CList<DrawnSeg,DrawnSeg> m_segList;

  Stack m_undoStack;
  Stack m_redoStack;

  // Selection-mirroring variables:
  int m_iRngI; // Indexes in m_rangeList of range that has one end of selection.
  int m_rRngI;
  int m_iSegStartPos; // m_segList[m_iSegI].startPos
  int m_iSegStopPos; // m_segList[m_iSegI].stopPos
  int m_rSegStartPos;
  int m_iRngStartPos;
  int m_rRngStartPos;
  int m_iSegI; // index in m_segList of one end of the selection (logically last? end point?)
  int m_rSegI; // index in m_segList of other end of the selection (logically first? anchor?)

  // Properties for next char typed:
  LgCharRenderProps m_chrpNextChar;
  CString m_nextCharFont;
  int m_nextCharSizePercent;

  // The first segment in m_segList that needs to be recomputed by RecreateSegList.
  // -1 if all are OK.
  int m_iFirstDirtySeg;
  int m_iLastDirtySeg; // -1 if list is dirty till the end
  int m_cchDirtyDiff; // how many characters were inserted on last editing operation

  bool m_focusKilled;

  bool m_drawIt; // blinking
  bool m_keydown;

  bool m_bAllowFormatDlg;

  UINT m_timerEventID;
  bool m_beep;

#ifdef _DEBUG
  void SetMemoryState();
  void GetMemoryState();
  CMemoryState msOld;
  CMemoryState msNew;
  CMemoryState diffMemState;
#endif

  COleDataSource* m_pSource;

  bool m_bOpenHtmlFatal;
  bool m_bOpenHtmlNonFatal;

protected:
  void UpdateScrollBars(HWND hwnd);
  void GrfxInit(HDC hdc, GrGraphics &grfx, LOGFONT lf, LgCharRenderProps lgchrp);
  void Reinitialize(void);
  void AdjustHeightOfLine(int segI, int maxHeight, int maxAscent, HDC hdc);
  int GetSegmentForCharIndex(int ich, bool bAssocPrev, bool bUpdateSel = false);
  int GetCurrentSegI(CPoint mousePt);        // get the segment where a point is
  void RecreateSegList(HDC hdc, int clientW); // create a brand new list of segments
  void CSILGraphiteControlCtrl::RemoveOneSegment(bool bSave,
	CList<DrawnSeg,DrawnSeg> & segListOld);
  void SegListRemoveAll(CList<DrawnSeg,DrawnSeg> & segList);
  void SegListRemoveHead(CList<DrawnSeg,DrawnSeg> & segList);
  void SegListRemoveTail(CList<DrawnSeg,DrawnSeg> & segList);
  CString ProcessText_UpdateRngLst(const CString inText);
  void SetRangeProperties(int ichBase, int ichLimit,
	int baseRngI, int limitRngI, int baseRngStart, int limitRngStart,
	LgCharRenderProps & chrpNew, CString & fontNameNew, int sizePercent);
  void CopyPropsToRange(Range & rng,
	LgCharRenderProps chrp, CString & fontName, int sizePercent);
  void CleanUpRanges();
  void AdjustAndScrollSelection();
  void ScrollSelectionIntoView(CRect caretRect);
  void CalcMaxHScroll();
  bool IsVScrollVisible();
  void InitNewRange(Range & rng);
  void ClearNextCharProps();
  int numLnBrB4(const int ip, const CString str);
  VARIANT_BOOL GetAllowFormatDlg(void);
  void SetAllowFormatDlg(VARIANT_BOOL newVal);
  void OnRelativeFontSize(int percent);
  virtual void OnKeyDownEvent(USHORT nChar, USHORT nShiftState); // handle OnKeyDown event
  BSTR GetFontName(void); //for backward compatibility
  BSTR GetHtml(void);     //for backward compatibility
  int NextArrowKeyPosition(int ichw, bool bForward, GrGraphics & grfx, int * piSegISel);
  void FindEngine(CString & fontName, GrGraphics & grfx, GrEngine ** ppgreng);
  CRect GetIpOrRangeEnd();
  int GetClientWidth();
  int GenerateHtml(char ** ppch);
  int Utf16ToUtf8(wchar * prgchwIn, int cchInMax, char * prgchsOut, int cchOutMax, bool * pbErr);
  void OutputChars(char * prgchToPut, int cch, char ** pprgchsOut, char * pchLim, bool * pbErr);
  void OutputChars(char * prgchToPut, char ** pprgchsOut, char * pchLim, bool *pbErr);
  long DecodeUtf8(const char * rgchUtf8, int cchUtf8, int & cbOut);
  int Utf8ToUtf16(const char * rgchSrc, int cchSrc, wchar * rgchwDst, int cchwDst);
  void ParseHtml(FileOrBuffer & fb);
  void ParseHtmlTag(FileOrBuffer & fb);
  void ParseBodyTag(FileOrBuffer & fb);
  void ParsePTags(FileOrBuffer & fb, char * tagTerm);
  void ParseSpanTags(FileOrBuffer & fb, char * tagTerm);
  bool MatchChar(FileOrBuffer & fb, char ch);
  bool MatchString(FileOrBuffer & fb, char * str);
  bool MatchOpenTag(FileOrBuffer & fb, char * tag);
  bool MatchOpenTag(FileOrBuffer & fb, char * tag,
	  CList<CString,CString> * pattrNames, CList<CString, CString> * pattrValues,
	  bool * pbClose);
  bool MatchCloseTag(FileOrBuffer & fb, char * tag);
  bool MatchEOL(FileOrBuffer & fb);
  void SkipWs(FileOrBuffer & fb);
  void GetNextAttrName(FileOrBuffer & fb, char * pchAttr, int cchMax);
  void GetQuotedString(FileOrBuffer & fb, CString * pstr);
  void ApplyAttributes(CList<CString, CString> & attrNames,
	CList<CString, CString> & attrValues, Range & rngNew);

protected:
};
