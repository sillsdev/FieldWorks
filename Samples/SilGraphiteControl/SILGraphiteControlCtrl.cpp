// SILGraphiteControlCtrl.cpp : Implementation of the CSILGraphiteControlCtrl ActiveX Control class.
#include "stdafx.h"
#include "SILGraphiteControl.h"
#include "SILGraphiteControlCtrl.h" // Control header file.
#include "SILGraphiteControlPropPage.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

#define IDT_TIMER  10000
#define BOMB 300 //timer to draw the insertion point (in msec)

IMPLEMENT_DYNCREATE(CSILGraphiteControlCtrl, COleControl)

// Message map
BEGIN_MESSAGE_MAP(CSILGraphiteControlCtrl, COleControl)
  ON_OLEVERB(AFX_IDS_VERB_EDIT, OnEdit)
  ON_OLEVERB(AFX_IDS_VERB_PROPERTIES, OnProperties)

  // Windows Messages
  ON_WM_CREATE()
  ON_WM_PAINT()
  ON_WM_CHAR()
  ON_WM_LBUTTONDOWN()
  ON_WM_LBUTTONDBLCLK()
  ON_WM_RBUTTONDOWN()
  ON_WM_MOUSEMOVE()
  ON_WM_VSCROLL()
  ON_WM_WINDOWPOSCHANGING()
  ON_WM_SIZE()
  ON_WM_SHOWWINDOW()
  ON_WM_KILLFOCUS()
  ON_WM_SETFOCUS()
  ON_WM_ERASEBKGND()
  ON_WM_GETDLGCODE()
  ON_WM_DESTROY()
  ON_WM_TIMER()
  ON_WM_HSCROLL()
  ON_WM_KEYUP()

  // Custom MenuBar Messages (to be deleted in Release version)
  ON_COMMAND(IDR_MENUBAR_CUT,OnCut)
  ON_COMMAND(IDR_MENUBAR_COPY,OnCopy)
  ON_COMMAND(IDR_MENUBAR_PASTE,OnPaste)
  ON_COMMAND(IDR_MENUBAR_UNDO,Undo)
  ON_COMMAND(IDR_MENUBAR_REDO,Redo)

  ON_COMMAND(IDR_MENUBAR_OPEN, OnAcceleratorOpen)
  ON_COMMAND(IDR_MENUBAR_SAVE, OnAcceleratorSave)
  ON_COMMAND(IDR_MENUBAR_OPEN_HTML, OnOpenHtml)
  ON_COMMAND(IDR_MENUBAR_SAVE_AS_HTML, OnSaveAsHtml)

  ON_COMMAND(IDR_MENUBAR_CHOOSE_FONT, OnChooseFont)
  // TODO: put these into a submenu?
  ON_COMMAND(IDR_MENUBAR_50, OnFont50)
  ON_COMMAND(IDR_MENUBAR_75, OnFont75)
  ON_COMMAND(IDR_MENUBAR_100, OnFont100)
  ON_COMMAND(IDR_MENUBAR_150, OnFont150)
  ON_COMMAND(IDR_MENUBAR_200, OnFont200)

#ifdef _DEBUG
  ON_COMMAND(IDR_MENUBAR_INIT_SIL_DOULOS_PIGLATIN_DEMO,OnInitSILDoulosPiglatinDemo)
  ON_COMMAND(IDR_MENUBAR_INIT_SIMPLE_GRAPHITE_FONT,OnInitSimpleGraphiteFont)
  ON_COMMAND(IDR_MENUBAR_INIT_SIL_DOULOS_UNICODE_IPA,OnInitSILDoulosUnicodeIPA)
  ON_COMMAND(IDR_MENUBAR_INIT_ARIAL_UNICODE_MS,OnInitArialUnicodeMS)

  ON_COMMAND(IDR_MENUBAR_MULTILINE, OnMultiline)
#endif

END_MESSAGE_MAP()

// Dispatch map
BEGIN_DISPATCH_MAP(CSILGraphiteControlCtrl, COleControl)

//  DISP_PROPERTY_EX_ID(CSILGraphiteControlCtrl, "Text", dispidText, GetText, SetText, VT_BSTR)
//  DISP_PROPERTY_EX_ID(CSILGraphiteControlCtrl, "multiline", dispidmultiline, GetMultiline, SetMultiline, VT_BOOL)

  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "AboutBox", DISPID_ABOUTBOX, AboutBox, VT_EMPTY, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "SetText", dispidSetText, SetText, VT_EMPTY, VTS_BSTR)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetText", dispidGetText, GetText, VT_BSTR, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "SetSelectionFont", dispidSetSelectionFont, SetSelectionFont, VT_EMPTY, VTS_BSTR)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetSelectionFont", dispidGetSelectionFont, GetSelectionFont, VT_EMPTY, VTS_PBSTR)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "SetSelectionFontSize", dispidSetSelectionFontSize, SetSelectionFontSize, VT_EMPTY, VTS_I4) // was VT_R8 for double
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetSelectionFontSize", dispidGetSelectionFontSize, GetSelectionFontSize, VT_I4, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "SetDefaultFont", dispidSetDefaultFont, SetDefaultFont, VT_EMPTY, VTS_BSTR)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "SetDefaultFontSize", dispidSetDefaultFontSize, SetDefaultFontSize, VT_EMPTY, VTS_I4)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "Cut", dispidCut, Cut, VT_EMPTY, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "Copy", dispidCopy, Copy, VT_EMPTY, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "Paste", dispidPaste, Paste, VT_EMPTY, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "SetMultiline", dispidSetMultiline, SetMultiline, VT_EMPTY, VTS_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetMultiline", dispidGetMultiline, GetMultiline, VT_BOOL, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetAllowFormatDlg", dispidGetAllowFormatDlg, GetAllowFormatDlg, VT_BOOL, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "SetAllowFormatDlg", dispidSetAllowFormatDlg, SetAllowFormatDlg, VT_EMPTY, VTS_BOOL)
  //DISP_PROPERTY_EX_ID(CSILGraphiteControlCtrl, "AllowFormatDlg", dispidAllowFormatDialog, GetAllowFormatDialog, SetAllowFormatDialog, VT_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "SetVerticalScroll", dispidSetVerticalScroll, SetVerticalScroll, VT_EMPTY, VTS_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "ResizeWindow", dispidResizeWindow, ResizeWindow, VT_EMPTY, VTS_I4 VTS_I4)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetSizeX", dispidGetSizeX, GetSizeX, VT_I4, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetSizeY", dispidGetSizeY, GetSizeY, VT_I4, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "New", dispidNew, New, VT_EMPTY, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "Open", dispidOpen, Open, VT_I4, VTS_BSTR VTS_I4 VTS_I4)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "Save", dispidSave, Save, VT_I4, VTS_BSTR VTS_I4 VTS_I4)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "MoveRight", dispidMoveRight, MoveRight, VT_EMPTY, VTS_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "MoveLeft", dispidMoveLeft, MoveLeft, VT_EMPTY, VTS_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "Undo", dispidUndo, Undo, VT_EMPTY, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "Redo", dispidRedo, Redo, VT_EMPTY, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "TextColor", dispidTextColor, TextColor, VT_EMPTY, VTS_UI1 VTS_UI1 VTS_UI1)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "PutHtmlText", dispidPutHtmlText, PutHtmlText, VT_EMPTY, VTS_BSTR)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetHtmlText", dispidGetHtmlText, GetHtmlText, VT_EMPTY, VTS_PBSTR)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "MoveUp", dispidMoveUp, MoveUp, VT_EMPTY, VTS_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "MoveDown", dispidMoveDown, MoveDown, VT_EMPTY, VTS_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "HorizontalScroll", dispidHorizontalScroll, HorizontalScroll, VT_EMPTY, VTS_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "BeepIt", dispidBeepIt, BeepIt, VT_EMPTY, VTS_BOOL)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetFontName", dispidGetFontName, GetFontName, VT_BSTR, VTS_NONE)
  DISP_FUNCTION_ID(CSILGraphiteControlCtrl, "GetHtml", dispidGetHtml, GetHtml, VT_BSTR, VTS_NONE)
END_DISPATCH_MAP()

// Event map
BEGIN_EVENT_MAP(CSILGraphiteControlCtrl, COleControl)
  EVENT_STOCK_KEYDOWN()
  EVENT_STOCK_CLICK()
END_EVENT_MAP()

// Property pages

// TODO: Add more property pages as needed.  Remember to increase the count!
BEGIN_PROPPAGEIDS(CSILGraphiteControlCtrl, 1)
PROPPAGEID(CSILGraphiteControlPropPage::guid)
END_PROPPAGEIDS(CSILGraphiteControlCtrl)

// Initialize class factory and guid
IMPLEMENT_OLECREATE_EX(CSILGraphiteControlCtrl, "SILGRAPHITECONTR.SILGraphiteContrCtrl.1",0x62631ffe, 0x1185, 0x44cc, 0x8a, 0xd2, 0x60, 0x2a, 0x5, 0xd1, 0xd0, 0x71)

// Type library ID and version
IMPLEMENT_OLETYPELIB(CSILGraphiteControlCtrl, _tlid, _wVerMajor, _wVerMinor)

// Interface IDs
const IID BASED_CODE IID_DSILGraphiteControl = { 0xCFFFE4B0, 0xE6E, 0x4D86, { 0x82, 0xC5, 0x10, 0xFB, 0xD2, 0xCA, 0x6F, 0x16 } };
const IID BASED_CODE IID_DSILGraphiteControlEvents = { 0xF6C724C7, 0xCC62, 0x46DC, { 0xAB, 0x50, 0x1E, 0x88, 0x2, 0xAB, 0xD0, 0xD4 } };

// Control type information
static const DWORD BASED_CODE _dwSILGraphiteControlOleMisc =
OLEMISC_ACTIVATEWHENVISIBLE |
OLEMISC_SETCLIENTSITEFIRST |
OLEMISC_INSIDEOUT |
OLEMISC_CANTLINKINSIDE |
OLEMISC_RECOMPOSEONRESIZE;

IMPLEMENT_OLECTLTYPE(CSILGraphiteControlCtrl, IDS_SILGRAPHITECONTROL, _dwSILGraphiteControlOleMisc)

// CSILGraphiteControlCtrl::CSILGraphiteControlCtrlFactory::UpdateRegistry -
// Adds or removes system registry entries for CSILGraphiteControlCtrl
BOOL CSILGraphiteControlCtrl::CSILGraphiteControlCtrlFactory::UpdateRegistry(BOOL bRegister)
{
  // TODO: Verify that your control follows apartment-model threading rules.
  // Refer to MFC TechNote 64 for more information.
  // If your control does not conform to the apartment-model rules, then
  // you must modify the code below, changing the 6th parameter from
  // afxRegInsertable | afxRegApartmentThreading to afxRegInsertable.

  if (bRegister)
	return AfxOleRegisterControlClass(
	AfxGetInstanceHandle(),
	m_clsid,
	m_lpszProgID,
	IDS_SILGRAPHITECONTROL,
	IDB_SILGRAPHITECONTROL,
	afxRegInsertable | afxRegApartmentThreading,
	_dwSILGraphiteControlOleMisc,
	_tlid,
	_wVerMajor,
	_wVerMinor);
  else
	return AfxOleUnregisterClass(m_clsid, m_lpszProgID);
}

// Licensing strings
static const TCHAR BASED_CODE _szLicFileName[] = _T("SILGraphiteControl.lic");

static const WCHAR BASED_CODE _szLicString[] = L"Copyright (c) 2003 ";

// CSILGraphiteControlCtrl::CSILGraphiteControlCtrlFactory::VerifyUserLicense -
// Checks for existence of a user license
BOOL CSILGraphiteControlCtrl::CSILGraphiteControlCtrlFactory::VerifyUserLicense()
{
  return AfxVerifyLicFile(AfxGetInstanceHandle(), _szLicFileName,
	_szLicString);
}

// CSILGraphiteControlCtrl::CSILGraphiteControlCtrlFactory::GetLicenseKey -
// Returns a runtime licensing key
BOOL CSILGraphiteControlCtrl::CSILGraphiteControlCtrlFactory::GetLicenseKey(DWORD dwReserved, BSTR FAR* pbstrKey)
{
  if (pbstrKey == NULL)
	return FALSE;

  *pbstrKey = SysAllocString(_szLicString);
  return (*pbstrKey != NULL);
}

// CSILGraphiteControlCtrl::CSILGraphiteControlCtrl - Constructor
CSILGraphiteControlCtrl::CSILGraphiteControlCtrl()
{
#ifdef _DEBUG
  SetMemoryState(); //Call SetMemoryState, then GetMemoryState will report memory leaks between 2 calls.
  //_CrtSetBreakAlloc(68); //call to check specific block of memory
#endif

  InitializeIIDs(&IID_DSILGraphiteControl, &IID_DSILGraphiteControlEvents);
  m_currFileName = L"Untitled";//??


  Reinitialize(); //?? any number will do
}

// CSILGraphiteControlCtrl::~CSILGraphiteControlCtrl - Destructor
CSILGraphiteControlCtrl::~CSILGraphiteControlCtrl()
{
  COleDataSource::FlushClipboard();

  //m_pgrSegmentLst.RemoveAll();
  //m_pgrTextLst.RemoveAll();
  m_myString.Empty();
  m_myText.Empty();
  m_defaultFontName.Empty();
  m_currFontName.Empty();
  m_currFileName.Empty();
  m_undoStack.clear();
  m_redoStack.clear();
  SegListRemoveAll(m_segList);
  m_rangeList.RemoveAll();
  for (int i = 0; i < m_engineList.GetCount(); i++)
  {
	  NamedEngine eng = m_engineList.GetAt(m_engineList.FindIndex(i));
	  if (eng.pgreng)
	  {
		  eng.pgreng->DestroyContents();
		  delete eng.pgreng;
		  eng.pgreng = NULL;
	  }
  }
  m_engineList.RemoveAll();

#ifdef _DEBUG
  GetMemoryState();
#endif
}

DWORD CSILGraphiteControlCtrl::GetControlFlags()
{
  return COleControl::GetControlFlags() | noFlickerActivate /*| pointerInactive*/;
}

// CSILGraphiteControlCtrl::OnDraw - Drawing function. We use OnPaint() instead of OnDraw()
void CSILGraphiteControlCtrl::OnDraw(CDC* pdc, const CRect& rcBounds, const CRect& rcInvalid)
{
  //we use OnPaint()
  //m_myText.Empty();
  //m_myText = ::W2BSTR(m_myString);
}

// CSILGraphiteControlCtrl::DoPropExchange - Persistence support
void CSILGraphiteControlCtrl::DoPropExchange(CPropExchange* pPX)
{
  ExchangeVersion(pPX, MAKELONG(_wVerMinor, _wVerMajor));
  COleControl::DoPropExchange(pPX);
  // TODO: Call PX_ functions for each persistent custom property.
}

// CSILGraphiteControlCtrl::OnResetState - Reset control to default state
void CSILGraphiteControlCtrl::OnResetState()
{
  COleControl::OnResetState();  // Resets defaults found in DoPropExchange
  // TODO: Reset any other control state here.
}

// CSILGraphiteControlCtrl::AboutBox - Display an "About" box to the user
void CSILGraphiteControlCtrl::AboutBox()
{
  CDialog dlgAbout(IDD_ABOUTBOX_SILGRAPHITECONTROL);
  dlgAbout.DoModal();
}

// CSILGraphiteControlCtrl message handlers
int CSILGraphiteControlCtrl::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
  m_focusKilled = false;

  if (COleControl::OnCreate(lpCreateStruct) == -1)
	return -1;

  m_drawIt = true;

  KillTimer(m_timerEventID);
  m_timerEventID = SetTimer(IDT_TIMER,BOMB,NULL);

  m_rDst.top = m_rSrc.bottom;
  m_rDst.left = m_rSrc.right;
  m_rDst.right = -m_rSrc.left;
  m_rDst.bottom = -m_rSrc.top;

  if (m_multiline)
  {
	g_nCurrentHScroll = 0;// initialize to 0 to eliminate the HScroll
	g_nMaxHScroll = 0;
	g_nCurrentVScroll = 0;
	g_nMaxVScroll = 0;

	DWORD dwStyle = GetWindowLong(this->m_hWnd,GWL_STYLE);

	if (m_bVScrollBar == true)
	{
	  g_nCurrentVScroll = 0;
	  g_nMaxVScroll = 1000;
	  dwStyle |= WS_VSCROLL;
	} else {
	  dwStyle &= ~WS_VSCROLL;
	}

	if (m_bHScrollBar == true)
	{
	  g_nCurrentHScroll = 0;
	  g_nMaxHScroll = 1000;
	  dwStyle |= WS_HSCROLL;
	} else
	  dwStyle &= ~WS_HSCROLL;

	dwStyle |= WS_EX_RIGHTSCROLLBAR|WS_BORDER|WS_EX_WINDOWEDGE
			   |WS_TABSTOP|WS_EX_CLIENTEDGE|WS_VISIBLE;

	SetWindowLong(this->m_hWnd,GWL_STYLE, dwStyle);
	UpdateScrollBars(this->m_hWnd);
  } else {
	g_nCurrentVScroll = 0; // initialize to 0 to eliminate the VScroll
	g_nMaxVScroll = 0;
	g_nCurrentHScroll = 0;
	g_nMaxHScroll = 0;

	DWORD dwStyle = GetWindowLong(this->m_hWnd,GWL_STYLE);

	if(m_bHScrollBar == false)
	{
	  dwStyle &= ~WS_HSCROLL;
	} else {
	  g_nCurrentHScroll = 0;
	  g_nMaxHScroll = 1000;
	  dwStyle |= WS_HSCROLL;
	}

	dwStyle |= WS_BORDER|WS_EX_WINDOWEDGE|WS_VISIBLE;
	dwStyle &= ~WS_VSCROLL;
	SetWindowLong(this->m_hWnd,GWL_STYLE, dwStyle);
	UpdateScrollBars(this->m_hWnd);
  }
  SetModifiedFlag();
  return 0;
}

void CSILGraphiteControlCtrl::Reinitialize(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  //setup client window
  SetInitialSize(95,100); //x,y size of client window
  SetAppearance(1); //3D-Look
  SetMultiline(true);
  SetAllowFormatDlg(true);

  //text the program work on
  m_myText.Empty();
  m_myText = ::W2BSTR(m_myString);

  //main variables drawing text
  m_ichwIp = 0;
  m_ichwRp = 0;
  m_iRngI=0;
  m_rRngI=0;
  m_iSegStartPos=0;
  m_iSegStopPos=0;
  m_rSegStartPos=0;
  m_iSegI=0;
  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;
  m_bAssocPrev = false;
  m_bRng = false;

  //font properties
  m_defaultFontName = "Arial";
  m_currFontName = m_defaultFontName;
  m_defaultFontSize = 10;
  m_currFontSize = m_defaultFontSize;

  ClearNextCharProps();

  //Variables for blinking caret
  m_drawIt = true;
  m_keydown = false;
  m_timerEventID = SetTimer(IDT_TIMER,BOMB,NULL);

  //other variables
  m_pSource = NULL;   //for cut, copy paste
  m_noOfParas = 1;    //for setMultiline()

  //set up the m_rSrc and m_rDst
  m_rSrc.top = -2;
  m_rSrc.left = -6;
  m_rSrc.right = 0;
  m_rSrc.bottom = 0;

  m_rDst.top = m_rSrc.bottom;
  m_rDst.left = m_rSrc.right;
  m_rDst.right = -m_rSrc.left;
  m_rDst.bottom = -m_rSrc.top;

  m_rDstOri.top = m_rSrc.bottom;
  m_rDstOri.left = m_rSrc.right;
  m_rDstOri.right = -m_rSrc.left;
  m_rDstOri.bottom = -m_rSrc.top;

  //Set up the 1st range
  Range rngNew;
  InitNewRange(rngNew);
  rngNew.numChar = m_myString.GetLength();
  m_rangeList.AddHead(rngNew);
}


void CSILGraphiteControlCtrl::GrfxInit(HDC hdc, GrGraphics &grfx, LOGFONT lf, LgCharRenderProps lgchrp)
{
  grfx.Initialize(hdc);

  lf.lfHeight = -MulDiv(m_defaultFontSize, GetDeviceCaps(hdc, LOGPIXELSY), 72);

  HFONT hFont;
  hFont = CreateFontIndirect(&lf);

  grfx.SetFont(hFont);
  grfx.SetupGraphics(&lgchrp);
}

// recreate the drawnSegLst. must be called after OnChar(), OnEndRange()
// m_iFirstDirtySeg gives the index of the first segment we need to recompute.
// m_iLastDirtySeg gives the index of the last segment we need to recompute.
void CSILGraphiteControlCtrl::RecreateSegList(HDC hdc, int clientW)
{
Ltryagain: // Label used to redo the whole process if scroll bar visibility changes.
  // Variables used by FindBreakPoint()
  GrResult res;       // [return value] result from FindBreakPoint(): kresOK or kresFalse
  GrGraphics grfxTmp; // [in]           graphics device context which is used to draw and measure text
  int nextStart=0;    // [in] char index of beginning of next segment to build, relative to the entire string
  int endOfRange = 0;
  m_acDxWidth=0;      // [in]           accumulated width at a line
  int cchSeg;         // [out]          how many characters fit in the segment
  int dxWidth;        // [out]          width of resulting segment
  LgEndSegmentType est = kestMoreLines; // [out] why the segment was ended
  BYTE pbNextSegDat[256]; // [out]
  int pcbNextSegDat;      // [out]
  int dichContext;        // [out]
  OLECHAR errmsg[1];      // [out]
  int curHeight=0;
  int curAscent=0;
  int maxHeight=0; //Max seg height of the line
  int ascentOfTallestFont=0; // on line.
  int contentHeight = 0; // height of the contents of the window
  bool fVScrollVisibleBeforeLayout = IsVScrollVisible();

  //Other variables used in the loops.
  Range rngTmp;
  POSITION rngPos;
  DrawnSeg segNext;
  int startOfRange = 0;
  bool firstSegInPara = false;
  bool firstSegInRng = true;
  bool firstSegOnLine = true;
  int lbEaten = 0; // number of line-break characters taken into consideration
  int tempDxWidth = 0;
  LgEndSegmentType preEst=est;
  int rIStart = 0; // first range to process

  CList<DrawnSeg,DrawnSeg> segListOld;

  // Preparations before proceeds to the loops
  // Remove segments from m_iFirstDirtySeg to end
  int cseg = m_segList.GetCount();
  m_myText.Empty();
  m_myText = ::W2BSTR(m_myString);

  if ((m_iFirstDirtySeg > 0 || m_iLastDirtySeg > -1) && cseg > m_iFirstDirtySeg)
  {
	// not redoing the whole thing; need different initialization for several variables.
	// We will restart the loop at the beginning of the line of the segment BEFORE
	// the dirty one.
	// First, delete the segments we know for sure we will redo. Save any beyond the
	// the last dirty one.
	for (int i = 0; i < cseg - m_iFirstDirtySeg; i++)
	{
		bool bSave = (m_iLastDirtySeg != -1 && i < cseg - m_iLastDirtySeg - 1);
		RemoveOneSegment(bSave, segListOld);  // , pgrSegmentLstOld, pgrTextLstOld);
	}
	//assert(m_segList.GetCount() == m_iFirstDirtySeg);
	// last surviving segment is at the end of the line we will redo.
	if (!m_segList.IsEmpty())
	{
		DrawnSeg dsDeleted = m_segList.GetTail();
		RemoveOneSegment(false, segListOld);
		// Delete additional segments until the tail segment is on a different line.
		while ((!m_segList.IsEmpty()) && m_segList.GetTail().lineRsrcTop == dsDeleted.lineRsrcTop)
		{
			dsDeleted = m_segList.GetTail();
			RemoveOneSegment(false, segListOld);
		}
		if (!(m_segList.IsEmpty()))
		{
			rIStart = dsDeleted.rI;
			nextStart = dsDeleted.startPos;
			contentHeight = (dsDeleted.lineRsrcTop - m_rSrc.top)  * -1;
			segNext = m_segList.GetTail();
			maxHeight = segNext.lineHeight;
		}
	}
  }
  else
  {
	  SegListRemoveAll(m_segList);
  }

  bool bAdjustPrevLine = false; // true when the previous segment was newly generated

  for(int rI = 0; rI < m_rangeList.GetCount(); rI++)
  {
	rngPos = m_rangeList.FindIndex(rI);
	rngTmp = m_rangeList.GetAt(rngPos);
	if(rngTmp.numChar<0)
	  MessageBox(L"Numchar<0", L"", MB_OK);

	startOfRange = endOfRange;
	endOfRange = startOfRange + rngTmp.numChar;
	if (rI < rIStart)
		continue;

	preEst=est;                     // How previous range ended
	est = kestMoreLines;            // Initialise est to get into while loop
	firstSegInRng = ((endOfRange - nextStart) == rngTmp.numChar);

	if (rngTmp.firstInPara)
	{
	  m_acDxWidth=0;
	  firstSegInPara = true;
	  firstSegOnLine = true;
	} else
	  firstSegInPara = false;

	//if(m_greng.IsGraphiteFont(&grfxTmp)!=kresOk)//memory leaks
	//{
	//  MessageBox(L"Warning:m_greng.IsGraphiteFont(&grfxTmp)!=kresOk", L"Graphite Font Check", MB_OK);
	//}

	GrfxInit(hdc,grfxTmp,rngTmp.lf,rngTmp.lgchrp);
	while( est != kestNoMore && est != kestWsBreak )
	{
	  //MSG msg;
	  //PeekMessage(&msg,this->m_hWnd,0,0,PM_REMOVE);

	  if (!m_segList.IsEmpty())
		segNext = m_segList.GetTail();

	  // JT: segNext is entirely invalid first time through, but variables we care about can be set.
	  // Optimize JT: if not for this we could probably just have a psegNext, avoiding copying the object...
	  // except we're modifying it to make a start position for the segment...
	  segNext.startPos = nextStart; // SJC: ACCESSING INVALID OBJECT! (first time through)
	  //stopPos = endOfRange; // Maximum possible stopPos for the GrSegment

	  if (firstSegOnLine && segListOld.GetCount() > 0)
	  {
		  // See if an old segment matches where we are starting to create a new segment.
		  int cOldSeg = segListOld.GetCount();
		  DrawnSeg segOld = segListOld.GetHead();
		  while (cOldSeg > 0 && segOld.startPos + m_cchDirtyDiff < segNext.startPos)
		  {
			  SegListRemoveHead(segListOld);
			  cOldSeg--;
			  if (cOldSeg > 0)
				  segOld = segListOld.GetHead();
		  }
		  if (segOld.startPos + m_cchDirtyDiff == segNext.startPos
			  && segOld.rSrc.left == m_rSrc.left) // beginning of line
		  {
			  // Good match. Use the old segments from here on.

			  // First adjust the height of the previous line in the normal way.
			  if (bAdjustPrevLine)
			  {
				  AdjustHeightOfLine(m_segList.GetCount()-1, maxHeight, ascentOfTallestFont, hdc);
				  contentHeight += maxHeight;
			  }

			  // Adjust the vertical positions, range information, and character indices of
			  // the old segments and restore them to the main list.
			  int yDiff = segOld.lineRsrcTop - (m_rSrc.top - contentHeight);
			  int riDiff = rI - segOld.rI;
			  int rITmp = rI; // what is in rngTmp;
			  DrawnSeg segReplace;
			  int lineTop = 0; // segReplace.lineRsrcTop
			  for (int iseg = 0; iseg < cOldSeg; iseg++)
			  {
				  segReplace = segListOld.GetHead();

				  segReplace.lineRsrcTop -= yDiff;
				  segReplace.rSrc.top -= yDiff;
				  segReplace.rSrc.bottom -= yDiff;
				  segReplace.startPos += m_cchDirtyDiff;
				  segReplace.stopPos += m_cchDirtyDiff;
				  segReplace.rI += riDiff;
				  while (rITmp < segReplace.rI)
				  {	// get range for this segment
					  startOfRange += rngTmp.numChar;
					  rITmp++;
					  rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(rITmp));
				  }
				  segReplace.rngStartPos = startOfRange;

				  if (segReplace.lineRsrcTop != lineTop)
				  {
					  contentHeight += segReplace.lineHeight;
					  lineTop = segReplace.lineRsrcTop;
				  }

				  // Transfer from old list to new list.
				  m_segList.AddTail(segReplace);
				  segListOld.RemoveHead();
			  }
			  cOldSeg = 0;

			  // Or...
			  //contentHeight = m_rSrc.top - segReplace.rSrc.top + segReplace.lineHeight;

			  // We're done.
			  est = kestNoMore;
			  rI = m_rangeList.GetCount();
			  bAdjustPrevLine = false;
			  continue;
		  }
	  }

	  // TODO: rework so we don't make a new text source every time
	  GrTextSrc * pgrtext = new GrTextSrc(m_myText, rngTmp.lgchrp, L"", 0);

	  // eat up the square displayed by the linebreak character.
	  int strLen = m_myString.GetLength();
	  if (endOfRange > strLen+1)
		MessageBox(L"stopPos not valid", L"", MB_OK);
	  lbEaten = (endOfRange > startOfRange) ? numLnBrB4(endOfRange,m_myString) : 0;

	  GrEngine * pgreng;
	  CString fontName = rngTmp.lf.lfFaceName;
	  FindEngine(fontName, grfxTmp, &pgreng);
	  GrSegment * pgrseg;

	  //FindBreakPoint():Generates a GrSegment in a paragraph, where a line break might be necessary.
	  res = pgreng->FindBreakPoint(
		/*  1  */ &grfxTmp,        //[in] GrGraphics
		/*  2  */ pgrtext,         //[in] GrTextSrc
		/*  3  */ segNext.startPos, //[in] which character position (0-based) the segment shall starts.
		/*  4  */ endOfRange-lbEaten,      //[in] which charater position (0-based) the segment shall stop.
		/*  5  */ endOfRange-lbEaten,      //[in] when backtracking, where to start looking for a new break.
		/*  6  */ false,           //[in] need final break
		/*  7  */ firstSegOnLine,//[in] is this the first segment on the line?
		/*  8  */ m_multiline ? clientW-m_acDxWidth : 60000, //[in] width available // JT: MAXINT?
		/*  9  */ klbWordBreak,    //[in] prefered kind of line break
		/* 10  */ firstSegOnLine ? klbClipBreak : klbWordBreak,    //[in] worst-case line break
		/* 11  */ ktwshAll,        //[in] standard handling of trailing white space
		/* 12  */ false,           //[in] paragraph is left-to-right
		/* 13  */ &pgrseg,		   //[out] segment produced, or null if nothing fits
		/* 14  */ &cchSeg,         //[out] number of characters in segment produced.
		/* 15  */ &dxWidth,        //[out] trailing spaces not counted! (EndOfLine==true by default)
		/* 16  */ &est,            //[out] what caused the segment to end

		// info from prev seg // JT: need to fix this?
		/* 17  */ 0,   //cbPrev,       //[in]                       int - byte size of pbPrevSegDat buffer
		/* 18  */ NULL,//pbPrevSegDat, //[in]:size_is(cbPrev)     byte* - for initializing from previous segment

		// info for following segment, exception thrown if this missing
		/* 19  */ 256, //cbNextMax,    //[in]                       int - max size of pbNextSegDat buffer
		/* 20  */ pbNextSegDat,        //[out]:size_is(cbNextMax) byte* - for initializing next segment
		/* 21  */ &pcbNextSegDat,      //[out]                     int* - size of pbNextSegDat buffer

		/* 22  */ &dichContext,        //[out] index of first char of interest to following seg
		/*23,24*/ errmsg, 1);        //[out],[in] for returning an error message

		if (pgrseg == NULL)
		{
			// No segment created, presumably because there was no more room on the line.
			// Try again with the new segment starting a line
			// JT: if firstSegOnLine is already true, this produces an infinite loop!
			if (firstSegOnLine)
			{
				MessageBox(L"Failed to make segment on empty line! Paragraph truncated", L"Error",MB_OK);
				break;
			}
			m_acDxWidth = 0;
			firstSegOnLine = true;
			delete pgrtext; // not used; TODO: make this more efficient
			continue;
		}

		if ((est==kestNoMore || est==kestWsBreak) && lbEaten>0) //last GrSegment of a line with '\n'
		{
		  cchSeg +=lbEaten;  //"add" back RETURN character: logically RETURN charater is still there
		  lbEaten=0;
		}

		segNext.stopPos=nextStart+cchSeg; // real stopPos of segment.

		pgrseg->get_Height(segNext.startPos,&grfxTmp,&curHeight);
		pgrseg->get_Ascent(segNext.startPos,&grfxTmp,&curAscent);

		// Positioning of segments
		if (!m_segList.IsEmpty()) // if not 1st seg
		{
		  segNext.rSrc.left = m_rSrc.left;
		  segNext.rSrc.right = m_rSrc.right;

		  if (firstSegOnLine)
		  {
				if (bAdjustPrevLine)
				{
					// Now that we have finished the previous line, we can calculate the height
					// of that line, including any segments that have unusual ascents.
					AdjustHeightOfLine(m_segList.GetCount()-1, maxHeight, ascentOfTallestFont, hdc);
					// Add the height of the last line to the total height.
					contentHeight += maxHeight;
				}
				// Now position the new segment.
				// This approach assumes that the segment with the greatest height also has the largest
				// ascent. The approach will cause segments with unusually large descents to be chopped
				// off on the bottom. TODO: fix this.
				segNext.rSrc.top = m_rSrc.top - contentHeight;
				segNext.rSrc.bottom = m_rSrc.bottom - contentHeight;
				maxHeight=curHeight;
				ascentOfTallestFont=curAscent;

				firstSegInPara = false;
		  } else {
			// if not 1st seg of the line, add accumulated width.
			segNext.rSrc.left  -= m_acDxWidth;
			segNext.rSrc.right -= m_acDxWidth;
		  }
		} else {
		  // very first segment
		  segNext.rSrc=m_rSrc;
		}

		if (curHeight>maxHeight)
		{
		  maxHeight=curHeight;
		  pgrseg->get_Ascent(segNext.startPos,&grfxTmp,&ascentOfTallestFont);
		}

		// Put endline as false except the last seg of line to include the trailing whitespaces.
		if (est!=kestMoreLines)
		  pgrseg->put_EndLine(segNext.startPos,&grfxTmp,false);
		pgrseg->get_Width(segNext.startPos,&grfxTmp,&tempDxWidth);

		segNext.rngStartPos = startOfRange;
		segNext.dxWidth=tempDxWidth;
		segNext.rI=rI;

		segNext.pgrseg = pgrseg;
		segNext.pgrtext = pgrtext;
		m_segList.AddTail(segNext);

		bAdjustPrevLine = true;

		m_acDxWidth += tempDxWidth;
		firstSegOnLine = false;
		nextStart += cchSeg;
		preEst = est;

		if (preEst == kestMoreLines)
		{
			firstSegInRng = false;
			m_acDxWidth = 0;
			firstSegOnLine = true;
		}

	  } //end of while(est!=...) loop

  } // end for rI

  // Adjust the baselines of the final line, now that we have all the segments.
  if (bAdjustPrevLine)
  {
		AdjustHeightOfLine(m_segList.GetCount()-1, maxHeight, ascentOfTallestFont, hdc);
		contentHeight += maxHeight;
  }

  g_nMaxVScroll = contentHeight;
  m_si.nMax = contentHeight;
  CalcMaxHScroll();
  UpdateScrollBars(m_hWnd);

  SegListRemoveAll(segListOld);

  if (IsVScrollVisible() != fVScrollVisibleBeforeLayout)
  {
		// Only hope is to try again. Must recompute the client width we were passed!
		Rect clientRect;
		GetClientRect(&clientRect);
		clientW = clientRect.Width()-m_rDstOri.right;
		m_iFirstDirtySeg = 0;
		m_iLastDirtySeg = -1;
		m_cchDirtyDiff = 0;
		goto Ltryagain;
  }
  m_iFirstDirtySeg = -1;
  m_iLastDirtySeg = m_segList.GetCount();
  m_cchDirtyDiff = 0;
}

// Return true if a scroll bar is being displayed
bool CSILGraphiteControlCtrl::IsVScrollVisible()
{
	// This does NOT work because the scroll bar obtained is always null. Something to
	// do with a defect in GetScrollBarCtrl that causes it not to find scroll bars
	// created automatically.
	//pvscroll = GetScrollBarCtrl(SB_VERT);
	//bool fVScrollVisibleAfterLayout = (pvscroll != NULL && pvscroll->IsWindowVisible());

	if (!m_bVScrollBar) // we never want it
		return false;

	SCROLLINFO si;
	GetScrollInfo(SB_VERT, &si);
	return (si.nMax > si.nPage);
}

// Initialize a newly created range to use the defaults for the control.
void CSILGraphiteControlCtrl::InitNewRange(Range & rngNew)
{
	rngNew.numChar = 0;
	rngNew.firstInPara = true;
	memset(&(rngNew.lf), '\0', sizeof(LOGFONT));
	rngNew.lf.lfCharSet = DEFAULT_CHARSET;
	wcscpy(rngNew.lf.lfFaceName, m_currFontName);
	memset(&rngNew.lgchrp, '\0', sizeof(LgCharRenderProps));
	rngNew.lgchrp.clrBack = RGB(255, 255, 255); // default: white background
	rngNew.lgchrp.clrFore = RGB(0, 0, 0);
	rngNew.lgchrp.dympHeight = m_currFontSize * 1000;
	rngNew.sizePercent = -1;
}

// Now that we have calculated all the segments on a line, adjust the height of the
// line to account for the tallest segment.
// Assumes that no segments have yet been placed on the following line.
//    - segI: index of last segment on the line
//    - maxHeight, maxAscent: height/ascent of the tallest segment, already calculated
void CSILGraphiteControlCtrl::AdjustHeightOfLine(int segI, int maxHeight, int maxAscent, HDC hdc)
{
	Range rngLocal;
	int rngI = -1;
	int lnRsrcTop = INT_MAX;
	GrGraphics grfxTmp;

	while (segI>=0)
	{
		DrawnSeg segTmp = m_segList.GetAt(m_segList.FindIndex(segI));

		if (lnRsrcTop == INT_MAX)
			lnRsrcTop = segTmp.rSrc.top;
		else if (segTmp.rSrc.top != lnRsrcTop)
			break;

		if (rngI != segTmp.rI)
		{
			rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(segTmp.rI));
			rngI = segTmp.rI;
			GrfxInit(hdc, grfxTmp, rngLocal.lf, rngLocal.lgchrp);
		}
		int ascent;
		segTmp.pgrseg->get_Ascent(segTmp.startPos, &grfxTmp, &ascent);

		segTmp.rSrc.top    -=  maxAscent-ascent;
		segTmp.rSrc.bottom -=  maxAscent-ascent;
		segTmp.lineHeight   =  maxHeight;
		segTmp.lineRsrcTop  =  lnRsrcTop;

		m_segList.SetAt(m_segList.FindIndex(segI), segTmp);

		segI--;
	} //end while(segI>=0)
}

int CSILGraphiteControlCtrl::GetClientWidth()
{
	CRect clientRect;
	GetClientRect(&clientRect);
	return clientRect.Width() - m_rDstOri.right;
}

// Remove one segment from the end of the current list. But possibly save it
// in order to restore it later.
void CSILGraphiteControlCtrl::RemoveOneSegment(bool bSave,
	CList<DrawnSeg,DrawnSeg> & segListOld)
{
	if (bSave)
	{
		// Transfer to temporary list.
		DrawnSeg segToSave = m_segList.GetTail();
		segListOld.AddHead(segToSave);
		m_segList.RemoveTail();
	}
	else
		SegListRemoveTail(m_segList);
}

// The master segment list owns the GrSegment and GrTextSrc objects in the the DrawnSegs,
// so when segments are deleted from the list, these objects need to be deleted as well.
void CSILGraphiteControlCtrl::SegListRemoveHead(CList<DrawnSeg,DrawnSeg> & segList)
{
	DrawnSeg segToRemove = segList.GetHead();
	if (segToRemove.pgrseg)
		delete segToRemove.pgrseg;
	if (segToRemove.pgrtext)
		delete segToRemove.pgrtext;

	segList.RemoveHead();
}

void CSILGraphiteControlCtrl::SegListRemoveTail(CList<DrawnSeg,DrawnSeg> & segList)
{
	DrawnSeg segToRemove = segList.GetTail();
	if (segToRemove.pgrseg)
		delete segToRemove.pgrseg;
	if (segToRemove.pgrtext)
		delete segToRemove.pgrtext;

	segList.RemoveTail();
}

void CSILGraphiteControlCtrl::SegListRemoveAll(CList<DrawnSeg,DrawnSeg> & segList)
{
	while (!segList.IsEmpty())
		SegListRemoveTail(segList);
}

// Get a rectangle corresponding to an insertion point, either the actual one
// or what would exist at the end (as opposed to anchor) of the current range.
// Updates the segment list if necessary.
// This rectangle is relative to the ClientRect, not the overall content size.
CRect CSILGraphiteControlCtrl::GetIpOrRangeEnd()
{
	GrGraphics grfxTmp;
	DrawnSeg segTmp;
	Range rngTmp;
	int preRi = -1;
	CClientDC DC(this);
	CRect primary, secondary;
	bool bPrimaryHere, bSecHere;

	// If we have a range selection, associate with the character that is part
	// of the range.
	bool bAssocPrev = m_bAssocPrev;
	if (m_ichwIp != m_ichwRp)
		bAssocPrev = m_ichwIp > m_ichwRp;

	if (m_iFirstDirtySeg >= 0)
		RecreateSegList(DC.m_hDC, GetClientWidth());

	for (int segI=0; segI<=m_segList.GetCount()-1; segI++)
	{
		segTmp = m_segList.GetAt(m_segList.FindIndex(segI));
		if (preRi != segTmp.rI)
		{
			rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(segTmp.rI));
			GrfxInit(DC.m_hDC,grfxTmp,rngTmp.lf,rngTmp.lgchrp);//for Graphite functions
			preRi=segTmp.rI;
		}
		// TODO: fix this in Graphite; see comment in OnPaint
		if (m_ichwIp >= segTmp.startPos && m_ichwIp <= segTmp.stopPos)
		{
			segTmp.pgrseg->PositionsOfIP(segTmp.startPos,&grfxTmp,segTmp.rSrc,m_rDst,m_ichwIp,
				bAssocPrev,kdmNormal,
				&primary,&secondary,&bPrimaryHere,&bSecHere);
			if (bPrimaryHere)
				return primary;
		}
		// Note: in this loop do NOT try to terminate loop based on segment visibility.
		// We want to know where the IP is even if it is NOT visible
		// (e.g., when called from ScrollSelectionIntoView).
	}
	return Rect(0,0,0,0);
}

void CSILGraphiteControlCtrl::OnPaint()
{
  CPaintDC dc(this); // needs to be a paint DC to call BeginPaint and EndPaint. Don't use except in Paint!
  CRect rcPaint = dc.m_ps.rcPaint;
  CBitmap bmp;
  CDC DC; // for double-buffering

  CRect clientRect;
  int clientWidth;
  int clientHeight;

  Range rngTmp;
  GrGraphics grfxTmp;
  DrawnSeg segTmp;
  DrawnSeg nextSeg;
  bool caretInThisSeg = true;
  int dxWidth;
  int preRi=0;
  bool rpHere=false;

  //Variable used by PositionsOfIP()
  bool fPrimaryHere, fSecHere;
  RECT primary,secondary;

  // Create a bitmap just big enough to hold the changes, then we create a device context
  // to draw on, then we bring the two together so we have something to draw on.
  bmp.CreateCompatibleBitmap( &dc, rcPaint.Width(), rcPaint.Height() );
  DC.CreateCompatibleDC( &dc );
  CBitmap *pOld = DC.SelectObject( &bmp );

  // Set the origin of the device context so that our bitmap appears where the changes
  // need to be!
  DC.SetViewportOrg( -rcPaint.left, -rcPaint.top );

  GetClientRect(&clientRect);
  clientWidth = clientRect.Width()-m_rDstOri.right;
  clientHeight = clientRect.Height();

  FillRect(DC.m_hDC, &clientRect, (HBRUSH) GetStockObject(WHITE_BRUSH));//repaint background RECT

  if (m_iFirstDirtySeg >= 0)
	RecreateSegList(DC.m_hDC, clientWidth); // Onpaint() and recreateSegList() should use the same hdc

  if (m_ichwIp == m_ichwRp)
  {
	GetSegmentForCharIndex(m_ichwIp, m_bAssocPrev, true);
  }
  else
  {
	  bool bAssocPrevTmp = (m_ichwIp > m_ichwRp);
	  GetSegmentForCharIndex(m_ichwIp, bAssocPrevTmp, true);
	  bAssocPrevTmp = (m_ichwIp < m_ichwRp);
	  GetSegmentForCharIndex(m_ichwRp, bAssocPrevTmp, true);
  }

  // 1st Drawtext(), then DrawInsertionPoint() or DrawRange()
  for (int segI=0; segI<=m_segList.GetCount()-1; segI++)
  {
	segTmp = m_segList.GetAt(m_segList.FindIndex(segI));

	if (segI==0 || preRi!=segTmp.rI)
	{
	  rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(segTmp.rI));
	  GrfxInit(DC.m_hDC,grfxTmp,rngTmp.lf,rngTmp.lgchrp);//for Graphite functions
	}

	preRi=segTmp.rI;
	if (m_multiline) // optimise
	{
	  if ((-segTmp.lineRsrcTop + m_rDst.top) >= clientHeight + 2 * m_rDstOri.bottom)
		// segment below the bottom the screen: quit
		break;
	  if ((-segTmp.lineRsrcTop + m_rDst.top) + segTmp.lineHeight <= m_rDstOri.bottom)
		// segment above the top of the screen: skip and go on
		continue;
	}

	// Note: Lines above called only if text is in view.
	segTmp.pgrseg->DrawText(segTmp.startPos,&grfxTmp,segTmp.rSrc,m_rDst,&dxWidth);
  } // draw for loop.

  // Do a separate loop for drawing the selection. Otherwise, drawing a later segment can
  // can overwrite something that has already been drawn.
  for (int segI=0; segI<=m_segList.GetCount()-1; segI++)
  {
	segTmp = m_segList.GetAt(m_segList.FindIndex(segI));

	if((segI>0 && preRi!=segTmp.rI) || segI==0)
	{
	  rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(segTmp.rI));
	  GrfxInit(DC.m_hDC,grfxTmp,rngTmp.lf,rngTmp.lgchrp);//for Graphite functions
	}

	preRi=segTmp.rI;
	if (m_multiline) // optimise
	{
	  if ((-segTmp.lineRsrcTop + m_rDst.top) >= clientHeight + 2 * m_rDstOri.bottom)
		// segment below the bottom the screen: quit
		break;
	  if ((-segTmp.lineRsrcTop + m_rDst.top) + segTmp.lineHeight <= m_rDstOri.bottom)
		// segment above the top of the screen: skip and go on
		continue;
	}

	if (m_bRng) // have range selection
	  segTmp.pgrseg -> DrawRange(segTmp.startPos,&grfxTmp,segTmp.rSrc,m_rDst,m_ichwRp,m_ichwIp,
		(m_rDst.top - m_rDstOri.top) + (-segTmp.lineRsrcTop),                  //top
		(m_rDst.top - m_rDstOri.top) + (-segTmp.lineRsrcTop+segTmp.lineHeight),//bottom
		true);
	else
		// Todo SharonC: currently an empty Graphite segment always draws the IP, even if
		// way out of range. This should probably be fixed, rendering the following
		// test unnecessary. For now, only segments that might possibly hold the IP
		// are given a chance to draw. This might just conceivably cause a problem for a
		// very exotic font.
		if (m_ichwIp >= segTmp.startPos && m_ichwIp <= segTmp.stopPos)
		{
			// We can still get two IPs with the above test, as the CRLF is included in the
			// previous DrawnSeg. But it isn't included in the actual Graphite segment object,
			// so check that length also.
			int cchSeg;
			segTmp.pgrseg->get_Lim(segTmp.startPos, &cchSeg);
			if (m_ichwIp <= segTmp.startPos + cchSeg)
				segTmp.pgrseg->DrawInsertionPoint(segTmp.startPos,&grfxTmp,segTmp.rSrc,
					m_rDst,m_ichwIp,m_bAssocPrev,true,kdmNormal);
		}

  } //end selection for() loop

  VERIFY( dc.BitBlt( rcPaint.left, rcPaint.top, rcPaint.Width(), rcPaint.Height(), &DC, rcPaint.left, rcPaint.top, SRCCOPY ) );
  SelectObject( DC, pOld );
}


void CSILGraphiteControlCtrl::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
  if(!m_bRng && m_ichwIp!=m_ichwRp)
  {
	MessageBox(L"m_bRng=false but m_ichwIp!=m_ichwRp",L"Error",MB_OK);
	return;
  }

  m_undoStack.push(m_myText,m_ichwIp,m_ichwRp, m_rangeList);

  m_myString.SetString(CString(m_myText));

  int base = min(m_ichwRp,m_ichwIp);
  int limit = max(m_ichwRp,m_ichwIp);

  int baseRngI = min(m_iRngI,m_rRngI);
  int limitRngI = max(m_iRngI,m_rRngI);
  int baseRngStart = min(m_iRngStartPos,m_rRngStartPos);
  int limitRngStart = max(m_iRngStartPos,m_rRngStartPos);

  POSITION baseRngPos = m_rangeList.FindIndex(baseRngI);
  POSITION limitRngPos = m_rangeList.FindIndex(limitRngI);
  Range baseRng = m_rangeList.GetAt(baseRngPos); //Get the anchor range
  Range limitRng = m_rangeList.GetAt(limitRngPos);

  int baseSegI = min(m_rSegI,m_iSegI);
  int limitSegI = max(m_rSegI,m_iSegI);
  int baseSegStart = min(m_iSegStartPos,m_rSegStartPos);
  int limitSegStart = max(m_iSegStartPos,m_rSegStartPos);

  int baseRngEnd = baseRngStart + baseRng.numChar;

  POSITION limitSegPos = m_segList.FindIndex(limitSegI);
  DrawnSeg limitSeg = m_segList.GetAt(limitSegPos);

  CRect clientRect;
  GetClientRect(&clientRect);
  int clientWidth = clientRect.Width() - m_rDstOri.right;

  Range rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(m_iRngI));
  int numOfRng = m_rangeList.GetCount();

  switch(nChar)
  {
  case VK_RETURN: //add CR LF
	{
	  if (m_multiline)
	  {
		if (m_bRng)// have selection
		{
		  baseRng.numChar = base-baseRngStart+2; // number of character left in old range with base (1 part) + CRLF
		  m_rangeList.SetAt(baseRngPos,baseRng);

		  limitRng.numChar-=limit-limitRngStart; // number of character left in old range with limit (2nd part)
		  limitRng.firstInPara=true;
		  if(baseRngI==limitRngI )// selection within a range.
			m_rangeList.InsertAfter(limitRngPos,limitRng);
		  else
			m_rangeList.SetAt(limitRngPos,limitRng);

		  int removeAtRI=baseRngI+1;
		  for(int rI=removeAtRI; rI<=limitRngI-1 && baseRngI!=limitRngI; rI++)//delete rangesin between
		  {
			rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(removeAtRI));
			if(rngLocal.firstInPara)
			  m_noOfParas--;

			m_rangeList.RemoveAt(m_rangeList.FindIndex(removeAtRI));
		  }
		  m_iSegI = limitSegI;
		} else { // no selection, base=limit.
		  int posInRange = limit-limitRngStart; // Pos of m_ichwIp relative to start of range
		  int oldNumChar=limitRng.numChar;

		  if (m_ichwIp==limitRngStart) // m_ichwIp at the beginnning of current segment
		  {
			bool bFirstInPara = limitRng.firstInPara;
			limitRng.firstInPara = true;
			m_rangeList.SetAt(limitRngPos,limitRng);
			// Add a 2-char range in front, containing CRLF.
			Range nextRng = limitRng;
			nextRng.numChar=2;
			nextRng.firstInPara = bFirstInPara;
			m_rangeList.InsertBefore(limitRngPos, nextRng);
		  }
		  else if (m_ichwIp == limitRngStart + oldNumChar)
		  { // m_ichwIp at the end of cur seg
			limitRng.numChar+=2; // add CRLF to end of current range
			m_rangeList.SetAt(limitRngPos,limitRng);
			if (limitRngI==numOfRng-1){ // last range
			  limitRng.numChar=0;
			  limitRng.firstInPara = true;
			  m_rangeList.InsertAfter(limitRngPos, limitRng);
			} else {
			  Range nextRng = m_rangeList.GetAt(m_rangeList.FindIndex(limitRngI+1));
			  if (nextRng.firstInPara) // if next range is a new Para, add 0_char para above it
			  {
				limitRng.numChar=0;
				limitRng.firstInPara = true;
				m_rangeList.InsertAfter(limitRngPos,limitRng);
			  } else { // if next range not 1st in Para, just set the next range as new Para
				nextRng.firstInPara = true;
				m_rangeList.SetAt(m_rangeList.FindIndex(limitRngI+1),nextRng);
			  }
			}
		  }
		  else
		  { // m_ichwIp in the middle, confirm have to break current range
			limitRng.numChar=posInRange+2; // old range broken into 2 parts, this is 1st part, including enter-key
			m_rangeList.SetAt(limitRngPos,limitRng);

			limitRng.numChar=oldNumChar-posInRange; // 2nd part of the old range, set as new paragraph
			limitRng.firstInPara=true;
			m_rangeList.InsertAfter(limitRngPos,limitRng);
		  }
		  //m_iRngI++; // optional
		  //m_rRngI++; // optional
		  //m_iSegI++; // mandatory! m_iSegI has to be valid
		}
		m_noOfParas++;
		m_myString.Delete(base,limit-base);

		// Note: in Ms Windows, RETURN is "CRLF", however VK_RETURN==13(CR) & '\n'==10(LF)
		// Manually insert CRLF
		m_myString.Insert(base,L"\x000D\x000A");
		m_ichwIp = base+2;
		m_cchDirtyDiff = 2;
		m_bAssocPrev = false; // associate with following paragraph

		m_myText.Empty();
		m_myText = ::W2BSTR(m_myString);

	  }
	  // else ignore Return in a single-line control.

	  break;
	}
  case VK_BACK:
	{
	  if (m_ichwIp <= 0 && !m_bRng)
		break;

	  if (m_bRng)//have range selection
	  {
		if(baseRngPos != limitRngPos) //selection NOT within a single range.
		{
		  baseRng.numChar =  base-baseRngStart;
		  m_rangeList.SetAt(baseRngPos,baseRng);

		  limitRng.numChar -= limit-limitRngStart; // number of character left in old range with limit
		  if (limitRng.firstInPara)
		  {
			limitRng.firstInPara=false;
			m_noOfParas--;
		  }
		  m_rangeList.SetAt(limitRngPos,limitRng);

		  int removeAtRI = baseRngI+1;
		  for(int rI = removeAtRI ; rI<=limitRngI-1; rI++)//delete unnecessary ranges in betweens.
		  {
			rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(removeAtRI));
			if(rngLocal.firstInPara)
			  m_noOfParas--;
			m_rangeList.RemoveAt(m_rangeList.FindIndex(removeAtRI));
		  }
		} else {//selection within a single range, baseRng=limitRng
		  baseRng.numChar -=  limit-base;
		  m_rangeList.SetAt(baseRngPos,baseRng);
		}
		m_iSegI=baseSegI;
		m_myString.Delete(base,limit-base);
		m_cchDirtyDiff = base-limit; // negative number
		m_ichwIp = base;
	  } else {//no selection, base=limit

		int lnBrk = numLnBrB4(m_ichwIp,m_myString);
		if (lnBrk>=1) //character before m_ichwIp is linebreak
		{
		  Range preRng = m_rangeList.GetAt(m_rangeList.FindIndex(m_iRngI-1));//get previous range.

		  while (preRng.numChar<=0 && m_iRngI-1<=numOfRng-1) //0-character ranges after RETURN, but before current range.
		  {
			m_rangeList.RemoveAt(m_rangeList.FindIndex(m_iRngI-1));      //remove the range.
			preRng = m_rangeList.GetAt(m_rangeList.FindIndex(m_iRngI-1));
		  }
		  preRng.numChar-=lnBrk; // previous range: delete off its last charater

		  if (preRng.numChar<=0)//previous paragraph contains only RETURN key
		  {
			m_rangeList.RemoveAt(m_rangeList.FindIndex(m_iRngI-1));//remove the previous paragraph.
			m_iSegI--;
		  } else {
			m_rangeList.SetAt(m_rangeList.FindIndex(m_iRngI-1),preRng);

			Range currRng = m_rangeList.GetAt(m_rangeList.FindIndex(m_iRngI));
			currRng.firstInPara=false;
			m_rangeList.SetAt(m_rangeList.FindIndex(m_iRngI),currRng);
		  }

		  base-=lnBrk;
		  m_myString.Delete(base,lnBrk);
		  m_cchDirtyDiff = -lnBrk;
		  m_ichwIp = base;
		  m_ichwRp = base;
		  m_noOfParas--; // take note that m_noOfParas initialized to 1
		} else { //previous char not return-key
		  if (rngLocal.numChar==0)//0-character range
		  {
			if (numOfRng>1)
			{
			  m_rangeList.RemoveAt(m_rangeList.FindIndex(m_iRngI));
			  m_iSegI--;
			  m_iRngI--;
			}
			rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(m_iRngI));
			rngLocal.numChar--;
			m_rangeList.SetAt(m_rangeList.FindIndex(m_iRngI),rngLocal);
		  } else {
			rngLocal.numChar--;
			m_rangeList.SetAt(m_rangeList.FindIndex(m_iRngI),rngLocal);

			if (base-1 == baseSegStart)// delete to previous seg.
			  m_iSegI--;
		  }
		  base--;
		  m_myString.Delete(base,1);
		  m_cchDirtyDiff = -1;
		  m_ichwIp = base;
		  m_ichwRp = base;
		  int lb = this->numLnBrB4(m_ichwIp, m_myString);
		  m_bAssocPrev = (lb > 0 || m_ichwIp == m_myString.GetLength());
		}
	  }

	  m_myText.Empty();
	  m_myText = ::W2BSTR(m_myString);
	  CleanUpRanges();
	  break;
	}
  default:
	{
	  // typing

	  if(!(::GetKeyState(VK_CONTROL) & 0x8000))// if Ctrl key not held down
	  {
		if(m_bRng) // have range selection
		{
		  if(baseRngPos != limitRngPos) // selection NOT within a single range.
		  {
			baseRng.numChar =  base-baseRngStart+1; //+1 is the char just typed.
			m_rangeList.SetAt(baseRngPos,baseRng);

			limitRng.numChar -= limit-limitRngStart; // number of character left in old range with limit
			if (limitRng.firstInPara)
			{
			  limitRng.firstInPara=false;
			  m_noOfParas--;
			}
			m_rangeList.SetAt(limitRngPos,limitRng);

			int removeAtRI = baseRngI+1;
			for(int rI = removeAtRI ; rI<=limitRngI-1; rI++)//delete unnecessary ranges in betweens.
			{
			  rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(removeAtRI));
			  if(rngLocal.firstInPara)
				m_noOfParas--;
			  m_rangeList.RemoveAt(m_rangeList.FindIndex(removeAtRI));
			}
			m_iRngI=baseRngI;
		  } else { // selection within a single range, baseRng = limitRng
			  baseRng.numChar -= limit - base;
			  baseRng.numChar += 1;
			  m_rangeList.SetAt(baseRngPos,baseRng);
		  }
		} else { // no range selection
		  baseRng.numChar++;
		  m_rangeList.SetAt(baseRngPos,baseRng);
		}

		if (m_chrpNextChar.dympHeight != -1
			|| m_chrpNextChar.clrBack != 0xff000000
			|| m_chrpNextChar.clrFore != 0xff000000
			|| m_nextCharFont.GetLength() > 0)
		{
			SetRangeProperties(base, base + 1, baseRngI, baseRngI,
				m_iRngStartPos, m_iRngStartPos,
				m_chrpNextChar, m_nextCharFont, m_nextCharSizePercent);
		}

		if(m_ichwIp>0)
		{
		  m_myString.Delete(base,limit-base);
		  m_cchDirtyDiff = base - limit;
		}
		m_myString.Insert(base,nChar);
		m_cchDirtyDiff += 1;
		m_ichwIp = base+1;

		if(m_ichwIp == 0)
			m_bAssocPrev = false;
		else
			m_bAssocPrev = true;

		m_myText.Empty();
		m_myText = ::W2BSTR(m_myString);
	  }
	  CleanUpRanges();
	  break;
	} // end of default
  } // end of switch

  if(!(::GetKeyState(VK_CONTROL) & 0x8000))//reason: control key held down and type, no changes in text, selection continue to exists
  {
	m_ichwRp = m_ichwIp;
	m_bRng = false;
	m_iFirstDirtySeg = baseSegI;
	m_iLastDirtySeg = limitSegI;
	//m_cchDirtyDiff set above
  }
  m_iRngI=baseRngI;

  ClearNextCharProps();

  Invalidate();
  UpdateWindow();
  COleControl::OnChar(nChar, nRepCnt, nFlags);

  // After display has been updated:
  CalcMaxHScroll();
  AdjustAndScrollSelection();
}

// Adjust the caret positions to reflect the new selection, and scroll it into view.
// TODO: Enhance this method to be smart about range selections. Right now it just uses
// m_ichwIp, which is the adjusted end, which is most often what is wanted.
void CSILGraphiteControlCtrl::AdjustAndScrollSelection()
{
  if (m_bRng)
	  m_bAssocPrev = m_ichwIp > m_ichwRp;
  CRect caretRect = GetIpOrRangeEnd();
  ScrollSelectionIntoView(caretRect);
}


// Assumes m_caretPos has been set appropriately for the end of the range selection or the IP.
void CSILGraphiteControlCtrl::ScrollSelectionIntoView(CRect caretRect)
{
	CRect rcTmp;
	GetClientRect(&rcTmp);
	int clientWidth = rcTmp.Width();
	int clientHeight = rcTmp.Height();

	int newHPos = g_nCurrentHScroll;
	int newVPos = g_nCurrentVScroll;

	if (m_multiline)
	{
		int lineHeight = 15; // useful default

		int yTopOfCaret = caretRect.top + g_nCurrentVScroll; // relative to top of contents, not scrolled window
		int yBottomOfCaret = caretRect.bottom + g_nCurrentVScroll;
		// Scroll down if necessary.
		newVPos = max(newVPos, yBottomOfCaret + 2 - clientHeight);
		// Scroll up if necessary.
		newVPos = max(min(newVPos, yTopOfCaret - 2), 0);
	}

	int xAbs = caretRect.left + g_nCurrentHScroll;
	// Scroll right if necessary.
	newHPos = max(newHPos, xAbs + 4 - clientWidth);
	// Scroll left if necessary.
	newHPos = min(newHPos, xAbs - 4);
	// Never scroll before the beginning of the text.
	newHPos = max(newHPos, (m_rDstOri.left * -1));
	// Never scroll horizontally more than necessary to show all the text.
	newHPos = min(newHPos, g_nMaxHScroll);

	m_rDst.left = newHPos * -1;
	m_rDst.right = m_rDst.left + (m_rSrc.right - m_rSrc.left);
	m_rDst.top = newVPos * -1;
	m_rDst.bottom = m_rDst.top + (m_rSrc.bottom - m_rSrc.top);

	int scrollDeltaV = newVPos - g_nCurrentVScroll;
	int scrollDeltaH = newHPos - g_nCurrentHScroll;
	if (scrollDeltaV == 0 && scrollDeltaH == 0)
		return;

	ScrollWindowEx( -scrollDeltaH, -scrollDeltaV, NULL, NULL, NULL,
		&m_rc, SW_ERASE | SW_INVALIDATE );

	if (scrollDeltaV)
	{
		g_nCurrentVScroll = newVPos;
		// Reset the scroll bar.
		if (m_bVScrollBar)
		{
			//GetScrollInfo(SB_VERT, &m_si);
			m_si.cbSize = sizeof(m_si);
			m_si.fMask = SIF_POS;
			m_si.nPos = g_nCurrentVScroll;
			SetScrollInfo(SB_VERT, &m_si, TRUE);
		}
		Invalidate();
		UpdateWindow();
//		COleControl::OnVScroll(nSBCode, nPos, pScrollBar);
	}
	if (scrollDeltaH)
	{
		g_nCurrentHScroll = newHPos;
		// Reset the scroll bar.
		if (m_bHScrollBar)
		{
			//GetScrollInfo(SB_HORZ, &m_si);
			m_si.cbSize = sizeof(m_si);
			m_si.fMask = SIF_POS;
			m_si.nPos = g_nCurrentHScroll;
			SetScrollInfo(SB_HORZ, &m_si, TRUE);
		}
		Invalidate();
		UpdateWindow();
//		COleControl::OnHScroll(nSBCode, nPos, pScrollBar);
	}
}

// Figure out the maximum allowable horizontal scroll, ie, the position at the end of the line.
void CSILGraphiteControlCtrl::CalcMaxHScroll()
{
	// TODO: Implement this better for a multiline control: eg, figure out the last
	// segment on the line, iterating over all lines to get the maximum. For now,
	// don't bother with it.

	CRect rcTmp;
	GetClientRect(&rcTmp);
	int clientWidth = rcTmp.Width();
	if (m_multiline)
	{
		g_nMaxHScroll = 0;
	}
	else
	{
		// Never scroll past the width of the last segment.
		DrawnSeg segTmp = m_segList.GetAt(m_segList.FindIndex(m_segList.GetCount() - 1));
		int segRight = (segTmp.rSrc.left - segTmp.dxWidth + m_rDstOri.right) * -1;
		g_nMaxHScroll = (m_rDstOri.left - segRight - 10 + clientWidth) * -1;
		g_nMaxHScroll = max(g_nMaxHScroll, 0);
	}

/*
	// This is another possible implementation, but it doesn't account for the fact that
	// the logically last character may not be at the end of the line.
	Range rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(segTmp.rI));
	CClientDC DC(this);
	GrGraphics grfxTmp;
	GrfxInit(DC.m_hDC, grfxTmp, rngTmp.lf, rngTmp.lgchrp);
	RECT rectPrim, rectSec;
	bool fPrimHere, fSecHere;
	segTmp.pgrseg->PositionsOfIP(segTmp.startPos, &grfxTmp, segTmp.rSrc, m_rDst,
		segTmp.stopPos, true, kdmNormal, &rectPrim, &rectSec, &fPrimHere, &fSecHere);
	if (fPrimHere)
	{
		g_nMaxHScroll = rectPrim.left;
	}
	else if (fSecHere)
	{
		g_nMaxHScroll = rectSec.left;
	}
	// else can't figure it out; don't change anything
*/
}


//except for CRLF, a segment cannot ends with >1 line breaks
int CSILGraphiteControlCtrl::numLnBrB4(const int ip, const CString str)
{
  if (ip>0 && ip<=str.GetLength())
  {
	switch (str[ip-1])
	{
	case L'\x000A':
	  {
		if (ip>=2 && str[ip-2] == L'\x000D')
		  return 2;
		else
		  return 1;
		break;
	  }
	case L'\x000D':
	case L'\x0085':
	case L'\x2028':
	case L'\x2029':
	  {
		return 1;
		break;
	  }
	default:
	  return 0;
	}
  } else if (ip==str.GetLength()+1 ) {
	return numLnBrB4(ip-1,str);
  } else {
	return 0;
  }
}


int CSILGraphiteControlCtrl::GetSegmentForCharIndex(int ich, bool bAssocPrev, bool bUpdateSel)
{
	// If we're at a line-break (just after the CRLF), associate with the following
	// paragraph.
	int lb = numLnBrB4(ich, m_myString);
	if (lb > 0)
	{
		bAssocPrev = false;
		// If we're between the CR and the LF, adjust. (Should never happen, but just in case.)
		if (ich < m_myString.GetLength() && m_myString[ich-1] == 0xD && m_myString[ich] == 0xA)
		//if (ich < m_myString.GetLength() && numLnBrB4(ich + 1, m_myString))
		{
			ich++;
			if (ich == m_ichwIp && bUpdateSel)
				m_ichwIp = ich;
		}
	}
	else if (ich < m_myString.GetLength())
	{
		// If we're just before the CRLF, associate with the previous paragraph.
		lb = numLnBrB4(ich + 1, m_myString);
		if (lb > 0)
		{
			bAssocPrev = true;
		}
	}

	DrawnSeg segTmp;
	int cseg = m_segList.GetCount();
	for (int sIndex = 0; sIndex < cseg; sIndex++)
	{
		segTmp = m_segList.GetAt(m_segList.FindIndex(sIndex));
		if (segTmp.startPos <= ich && ich < segTmp.stopPos)
			break;
		if (segTmp.stopPos == ich && (sIndex == cseg - 1 || bAssocPrev))
			break;
	}

	if (sIndex >= cseg)
	{
		return -1;
	}

	if (bUpdateSel)
	{
		if (ich == m_ichwIp)
		{
			m_iSegI = sIndex;
			m_iRngI = segTmp.rI;
			m_iSegStartPos = segTmp.startPos;
			m_iSegStopPos = segTmp.stopPos;
			m_iRngStartPos = segTmp.rngStartPos;
			m_bAssocPrev = bAssocPrev; // in case we adjusted above
		}
		if (ich == m_ichwRp)
		{
			m_rSegI = sIndex;
			m_rRngI = segTmp.rI;
			m_rSegStartPos = segTmp.startPos;
			m_rRngStartPos = segTmp.rngStartPos;
		}
		m_bRng = (m_ichwRp != m_ichwIp);
	}

	return sIndex;
}

int CSILGraphiteControlCtrl::GetCurrentSegI(CPoint mousePt)
{
  CClientDC DC(this);
  GrGraphics grfxTmp;

  CRect clientRect;
  GetClientRect(&clientRect);
  int clientHeight = clientRect.Height();

  DrawnSeg segTmp;

  int segI_x=0;//segI to find current seg
  int segI_y=0;//segI to find which seg is first in current line

  int lastSegI= m_segList.GetCount()-1;

  DrawnSeg segLast = m_segList.GetTail();

  for(segI_y=0 ; segI_y<=lastSegI; segI_y++)// loop to find which line the mouse Point is in
  {
	segTmp = m_segList.GetAt(m_segList.FindIndex(segI_y));

	//mousePt above 1st line, then go to 1st line
	if( mousePt.y < (-m_rSrc.top + m_rDst.top) && mousePt.y > 0)
	{
	  if(mousePt.x < (-segTmp.rSrc.left + m_rDst.left))//Pt left of 1st seg in line
		return 0;

	  for(segI_x=0; segI_x<=lastSegI; segI_x++) //loop 1st line to find to corresponding seg to mousePt
	  {
		segTmp = m_segList.GetAt(m_segList.FindIndex(segI_x));

		if(mousePt.y < (-segTmp.lineRsrcTop + m_rDst.top) )// if reached next line
		{
		  if(segI_x>0)
			segI_x--;

		  return segI_x; // go back to 1st seg of current line.
		}

		if( mousePt.x >= -segTmp.rSrc.left + m_rDst.left && mousePt.x <= -segTmp.rSrc.left + m_rDst.left + segTmp.dxWidth)
		  return segI_x;
	  }
	  return --segI_x; //needed!: last seg in current line (if Pt is right of last seg in line)
	}

	//mousePt below last line, scan from lastSeg (backward scan)
	if( mousePt.y > (-segLast.lineRsrcTop + m_rDst.top) + segLast.lineHeight) // && mousePt.y <= clientHeight )
	{
	  if(mousePt.x > (-segLast.rSrc.left + m_rDst.left) + segLast.dxWidth)//Pt right of 1st seg in line
		return lastSegI;

	  int lastlnRsrcTop = segLast.lineRsrcTop;
	  for(segI_x=lastSegI ; segI_x>=0; segI_x--)
	  {
		segTmp = m_segList.GetAt(m_segList.FindIndex(segI_x));

		if(-segTmp.lineRsrcTop < -lastlnRsrcTop) // if reached previous line
		{
		  if(segI_x<lastSegI)
			segI_x++;

		  return segI_x; // go back to 1st seg of current line.
		}

		if( mousePt.x >= -segTmp.rSrc.left + m_rDst.left && mousePt.x <= -segTmp.rSrc.left + m_rDst.left + segTmp.dxWidth)
		  return segI_x;
	  }
	  return ++segI_x; //needed!: last seg in current line (if Pt is left of last last line)
	}

	//found the line
	if(mousePt.y >= (-segTmp.lineRsrcTop + m_rDst.top) && mousePt.y < (-segTmp.lineRsrcTop + m_rDst.top) + segTmp.lineHeight /* + (lnSpacing-1)*segTmp.lineHeight)/2 */)//15AprCSC
	{
	  if(mousePt.x < (-segTmp.rSrc.left + m_rDst.left))//Pt left of 1st seg in line
		return segI_y;

	  for(segI_x=segI_y ; segI_x<=lastSegI; segI_x++)
	  {
		segTmp = m_segList.GetAt(m_segList.FindIndex(segI_x));

		if(mousePt.y < (-segTmp.lineRsrcTop + m_rDst.top) )// if reached next line
		{
		  if(segI_x>0)
			segI_x--;

		  return segI_x; // go back to 1st seg of current line.
		}

		if( mousePt.x >= (-segTmp.rSrc.left + m_rDst.left) && mousePt.x <= -segTmp.rSrc.left + m_rDst.left + segTmp.dxWidth)
		{
		  return segI_x;
		}
	  }
	  return --segI_x; //needed!: last seg in current line (if Pt is right of last seg in line)
	}
  }
  return segI_x; //last seg in current line //TODO: is this needed?
}

void CSILGraphiteControlCtrl::OnLButtonDown(UINT nFlags, CPoint point)
{
  CClientDC DC(this);
  GrGraphics grfxTmp;
  Range rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(m_iRngI));
  GrfxInit(DC.m_hDC,grfxTmp,rngLocal.lf,rngLocal.lgchrp);

  SHORT shiftstate;
  shiftstate = GetKeyState(VK_SHIFT);
  bool bShift = (bool) (shiftstate & 0x8000);

  POINT pt;
  pt.x = point.x;
  pt.y = point.y;

  if (!bShift) // no shift key
  {
	m_bRng = false;
	m_ichwRp = m_ichwIp;
  }else{ // yes shift key
	if (!m_bRng)
	  m_ichwRp = m_ichwIp;
	m_bRng = true;
  }

  int oldIchwIp = m_ichwIp;
  DrawnSeg iSegOld = m_segList.GetAt(m_segList.FindIndex(m_iSegI));
  m_iSegI = GetCurrentSegI(point);
  DrawnSeg segTmp= m_segList.GetAt(m_segList.FindIndex(m_iSegI));

  if (segTmp.dxWidth==0)
	m_ichwIp=segTmp.startPos;
  else {
	segTmp.pgrseg->PointToChar(segTmp.startPos, &grfxTmp, segTmp.rSrc, m_rDst, pt, &m_ichwIp, &m_bAssocPrev);

	int lnBr=numLnBrB4(m_ichwIp,m_myString);
	if(m_ichwIp>0 && lnBr>=1 && segTmp.stopPos == m_ichwIp)
	  m_ichwIp-=lnBr;
  }
  if (!m_bRng)
	m_ichwRp = m_ichwIp;

  // JT/SC: don't waste time getting rid of empty segment?? Is there any reason we MUST get rid of it?
  //if(iSegOld.dxWidth==0 && m_ichwIp!=oldIchwIp)
  //  m_segDirty=true;

  ClearNextCharProps();

  Invalidate();
  UpdateWindow();
  COleControl::OnLButtonDown(nFlags, point);

  AdjustAndScrollSelection();
}

void CSILGraphiteControlCtrl::OnMouseMove(UINT nFlags, CPoint point)
{
  if (nFlags && MK_LBUTTON)  // Left mouse button down & mouse moving
  {
	ClearNextCharProps();

	CClientDC DC(this);
	GrGraphics grfxTmp;
	Range rngLocal = m_rangeList.GetAt(m_rangeList.FindIndex(m_iRngI));
	GrfxInit(DC.m_hDC,grfxTmp,rngLocal.lf,rngLocal.lgchrp);

	POINT pt;
	pt.x = point.x;
	pt.y = point.y;

	int mouseCurrentSeg = GetCurrentSegI(point);

	if (!m_bRng)
	  m_ichwRp = m_ichwIp;

	int oldIchwIp = m_ichwIp;
	DrawnSeg segTmp= m_segList.GetAt(m_segList.FindIndex(mouseCurrentSeg));

	segTmp.pgrseg->PointToChar(segTmp.startPos, &grfxTmp, segTmp.rSrc, m_rDst, pt,
		&m_ichwIp, &m_bAssocPrev);

	if (m_ichwRp != m_ichwIp && m_ichwIp!=oldIchwIp) // if m_ichwIp changed by PointToChar & m_ichwIp!=m_ichwRp, then refresh window
	{
	  m_bRng = true;
	  Invalidate();
	  UpdateWindow();
	}
  }
  if (!m_bRng)
	m_ichwRp = m_ichwIp;
  COleControl::OnMouseMove(nFlags, point);
}

void CSILGraphiteControlCtrl::OnLButtonDblClk(UINT nFlags, CPoint point)
{
  if(m_myString.IsEmpty() == false)
  {
	////int enterPos = m_myString.Find(L"[#x0000=#x10FFFF]-[\p{P}\p{Z}\p{C}]",m_ichwIp); //can't get this expression with [] to work with CString
	//CString rOfIp = m_myString.Right(m_myString.GetLength()-m_ichwIp);
	//int wSpc = m_ichwIp + rOfIp.FindOneOf(L" \r\n\t\v\f\'\"\\\?`~!@#$%^&*()-_=+|,./<>\x0085\x2028\x2029"); //find next white space

	//CString lOfIp = m_myString.Left(m_ichwIp);
	//lOfIp.Tokenize(
	//m_ichwIp = wSpc;

	ClearNextCharProps();

	int ip1=m_myString.GetLength()-m_ichwIp;
	int ip2=m_ichwIp;
	CString reverse = m_myString;
	reverse.MakeReverse();
	CString wordToken1 = reverse.Tokenize(L" \r\n\t\v\f\'\"\\\?`~!@#$%^&*()-_=+|,./<>\x0085\x2028\x2029",ip1);
	CString wordToken2 = m_myString.Tokenize(L" \r\n\t\v\f\'\"\\\?`~!@#$%^&*()-_=+|,./<>\x0085\x2028\x2029",ip2);
	m_ichwIp=ip2-1;
	m_ichwRp=m_myString.GetLength()-ip1+1;

	//if(m_ichwIp == -1)//can't find
	//  m_ichwIp = m_myString.GetLength();

	//for (int i = m_ichwIp ; i > 0 ; i--)
	//{
	//  if(m_myString[i-1] == ' ' || numLnBrB4(i,m_myString)>=1)
	//  {
	//    m_ichwRp = i;
	//    break;
	//  }
	//  else
	//    m_ichwRp = 0;
	//}
	m_bRng = true;
	Invalidate(FALSE);
  }
  COleControl::OnLButtonDblClk(nFlags, point);
}

void CSILGraphiteControlCtrl::OnRButtonDown(UINT nFlags, CPoint point)
{
  CMenu contextMenu;
  contextMenu.CreatePopupMenu();
  ClientToScreen(&point);
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_CUT,_T("Cut"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_COPY,_T("Copy"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_PASTE,_T("Paste"));

  contextMenu.AppendMenu(MF_SEPARATOR,0,_T(""));

  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_OPEN,_T("Open..."));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_SAVE,_T("Save As..."));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_OPEN_HTML,_T("Open HTML..."));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_SAVE_AS_HTML,_T("Save As HTML..."));

  contextMenu.AppendMenu(MF_SEPARATOR,0,_T(""));

  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_UNDO,_T("Undo"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_REDO,_T("Redo"));

  if (m_bAllowFormatDlg)
  {
	contextMenu.AppendMenu(MF_SEPARATOR,0,_T(""));
	contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_CHOOSE_FONT,_T("Format..."));
  }

  contextMenu.AppendMenu(MF_SEPARATOR,0,_T(""));

  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_50, _T("Font Size to 50%"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_75, _T("Font Size to 75%"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_100,_T("Font size to 100%"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_150,_T("Font Size to 150%"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_200,_T("Font Size to 200%"));

#ifdef _DEBUG
  contextMenu.AppendMenu(MF_SEPARATOR,0,_T(""));

  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_INIT_SIL_DOULOS_PIGLATIN_DEMO,_T("Use SIL Doulos Piglatin Demo"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_INIT_SIMPLE_GRAPHITE_FONT,_T("Use SimpleGraphiteFont"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_INIT_SIL_DOULOS_UNICODE_IPA,_T("Use SIL Doulos Unicode IPA"));
  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_INIT_ARIAL_UNICODE_MS,_T("Use Arial Unicode MS"));

  contextMenu.AppendMenu(MF_SEPARATOR,0,_T(""));

  contextMenu.AppendMenu(MF_ENABLED,IDR_MENUBAR_MULTILINE,_T("Multi-line <--> Single-line"));
#endif

  contextMenu.TrackPopupMenu(TPM_LEFTALIGN|TPM_RIGHTBUTTON,point.x,point.y,this);
  contextMenu.DestroyMenu();
  Invalidate();
  UpdateWindow();
  COleControl::OnRButtonDown(nFlags, point);
}

BSTR CSILGraphiteControlCtrl::GetText(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  return m_myText;
}

void CSILGraphiteControlCtrl::SetText(LPCTSTR newVal)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_undoStack.push(m_myText,m_ichwIp,m_ichwRp,m_rangeList);

  m_rangeList.RemoveAll();
  SegListRemoveAll(m_segList);

  // Initialize with one empty range, because this is what ProcessText_UpdateRngLst expects.
  Range rngTmp;
  InitNewRange(rngTmp);
  m_rangeList.AddHead(rngTmp);

  CString buffer = newVal;

  // note: ProcessText_UpdateRngLst updates m_rangeList also
  // must call ProcessText_UpdateRngLst b4 changing m_myString
  ProcessText_UpdateRngLst(buffer);

  m_myString = newVal;

  m_ichwIp = 0;
  m_ichwRp = 0;
  m_bAssocPrev = false;

  m_myText.Empty();
  m_myText = ::W2BSTR(m_myString);

  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;

  Invalidate();
  UpdateWindow();
  SetModifiedFlag();
}

void CSILGraphiteControlCtrl::SetDefaultFont(LPCTSTR fontName)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	CString oldDefaultName = m_defaultFontName;
	m_defaultFontName = fontName;

	if (m_myString.GetLength() == 0)
		SetSelectionFont(fontName);
}

void CSILGraphiteControlCtrl::SetDefaultFontSize(int fontSize)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	int oldFontSize = m_defaultFontSize;
	m_defaultFontSize = fontSize;

	if (m_myString.GetLength() == 0)
		SetSelectionFontSize(fontSize);
}

void CSILGraphiteControlCtrl::SetSelectionFont(LPCTSTR fontName)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_undoStack.push(m_myText, m_ichwIp, m_ichwRp, m_rangeList);

  if (!m_bRng)
  {
	  // Insertion point. Record the desired font for later.
	  ClearNextCharProps();
	  m_nextCharFont = fontName;
  }
  else
  {
	// Range selection.
	int base  = min(m_ichwIp,m_ichwRp); //start point of selection.
	int limit = max(m_ichwIp,m_ichwRp);//end point of selection

	int baseRngI  = min(m_iRngI,m_rRngI); //index of range contains base (base-range) in m_rangeList
	int limitRngI = max(m_iRngI,m_rRngI); //index of range contains limit (limit-range)in m_rangeList

	int baseRngStart = min(m_iRngStartPos,m_rRngStartPos);  // startPos of base-range
	int limitRngStart  = max(m_iRngStartPos,m_rRngStartPos); // startPos of limit-range

	LgCharRenderProps chrp;
	chrp.dympHeight = -1;
	chrp.clrBack = 0xff000000; // don't change
	chrp.clrFore = 0xff000000; // don't change
	CString newFont(fontName);
	SetRangeProperties(base, limit, baseRngI, limitRngI, baseRngStart, limitRngStart,
		chrp, newFont, -1);

	CleanUpRanges();

	m_iFirstDirtySeg = min(m_iSegI, m_rSegI);
	m_iLastDirtySeg = max(m_iSegI, m_rSegI);
	m_cchDirtyDiff = 0;

	Invalidate();
	UpdateWindow();
  }
}

void CSILGraphiteControlCtrl::GetSelectionFont(BSTR* fontName)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  POSITION curPos = m_rangeList.FindIndex(m_iRngI);
  POSITION rPos = m_rangeList.FindIndex(m_rRngI);

  Range rngLocal = m_rangeList.GetAt(curPos);
  m_currFontName = rngLocal.lf.lfFaceName;

  CComBSTR bstrString(m_currFontName);
  bstrString.CopyTo(fontName);
}

void CSILGraphiteControlCtrl::SetSelectionFontSize(int fontSize)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_undoStack.push(m_myText,m_ichwIp,m_ichwRp, m_rangeList);

  int posInRange = m_ichwIp-m_iRngStartPos; // Pos of m_ichwIp relative to startPos of range.

  if (!m_bRng)
  {
	  // Insertion point. Record the desired size for later.
	  ClearNextCharProps();
	  m_chrpNextChar.dympHeight = fontSize * 1000;
	  m_nextCharSizePercent = -1;
  } else { // range selection
	int base  = min(m_ichwIp,m_ichwRp); //start point of selection.
	int limit = max(m_ichwIp,m_ichwRp);//end point of selection

	int baseRngI  = min(m_iRngI,m_rRngI); //index of range contains base (base-range) in m_rangeList
	int limitRngI = max(m_iRngI,m_rRngI); //index of range contains limit (limit-range)in m_rangeList

	int baseRngStart = min(m_iRngStartPos,m_rRngStartPos); //startPos of base-range
	int limitRngStart  = max(m_iRngStartPos,m_rRngStartPos); //startPos of limit-range

	LgCharRenderProps chrp;
	chrp.dympHeight = fontSize * 1000;
	chrp.clrBack = 0xff000000; // don't change
	chrp.clrFore = 0xff000000; // don't change
	CString newFont;
	newFont.Empty();
	SetRangeProperties(base, limit, baseRngI, limitRngI, baseRngStart, limitRngStart,
		chrp, newFont, -1);

	CleanUpRanges();
	m_iFirstDirtySeg = min(m_iSegI, m_rSegI);
	m_iLastDirtySeg = max(m_iSegI, m_rSegI);
	m_cchDirtyDiff = 0;
	Invalidate();
	UpdateWindow();
  }
}

int CSILGraphiteControlCtrl::GetSelectionFontSize(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  POSITION curPos = m_rangeList.FindIndex(m_iRngI);
  POSITION rPos = m_rangeList.FindIndex(m_rRngI);

  Range rngLocal = m_rangeList.GetAt(curPos);
  m_currFontSize = rngLocal.lgchrp.dympHeight/1000;

  return m_currFontSize;
}

void CSILGraphiteControlCtrl::Cut()
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  if(m_ichwIp != m_ichwRp)
  {
	m_undoStack.push(m_myText,m_ichwIp,m_ichwRp,m_rangeList);

	CString toCopy;
	VARIANT var;
	VariantInit(&var);
	var.vt = VT_BSTR;

	int base = min(m_ichwRp,m_ichwIp);
	int limit = max(m_ichwRp,m_ichwIp);
	int numCharAffected = limit-base;

	int baseRngI = min(m_iRngI,m_rRngI);
	int limitRngI = max(m_iRngI,m_rRngI);
	int baseStart = min(m_iSegStartPos,m_rSegStartPos);
	int limitStart = max(m_iSegStartPos,m_rSegStartPos);
	POSITION baseRngPos = m_rangeList.FindIndex(baseRngI);
	POSITION limitRngPos = m_rangeList.FindIndex(limitRngI);

	Range baseRng = m_rangeList.GetAt(baseRngPos); //Get the anchor range
	Range limitRng = m_rangeList.GetAt(limitRngPos);

	POSITION iSegPos = m_segList.FindIndex(m_iSegI);
	DrawnSeg iSeg = m_segList.GetAt(iSegPos);

	if(baseRngPos == limitRngPos)
	{
	  int index = base;
	  while(index<limit)
	  {
		toCopy.AppendChar(m_myString[index++]);
	  }
	  m_myString.Delete(base,limit-base);
	  m_cchDirtyDiff = base - limit; // negative number
	  baseRng.numChar-= (limit-base);
	  m_rangeList.SetAt(baseRngPos,baseRng);
	  m_ichwIp = base;
	  m_ichwRp = base;
	}
	else{
	  int index = base;
	  while(index<limit)
	  {
		toCopy.AppendChar(m_myString[index++]);
	  }
	  m_myString.Delete(base,limit-base);
	  m_cchDirtyDiff = base - limit; // negative number
	  m_ichwIp = base;
	  m_ichwRp = base;

	  baseRng.numChar=base-baseStart; // number of character left in old range with base -1
	  m_rangeList.SetAt(baseRngPos,baseRng);

	  limitRng.numChar =limitRng.numChar - (limit-limitStart); // number of character left in old range with limit
	  limitRng.firstInPara=false;
	  m_rangeList.SetAt(limitRngPos,limitRng);

	  int baseRemove = baseRngI+1;
	  int limitRemove = limitRngI - 1;
	  for(int rI = baseRemove ; rI <= limitRemove ; rI++)//delete necessary ranges
	  {
		m_rangeList.RemoveAt(m_rangeList.FindIndex(rI));
	  }
	  m_iRngI=baseRngI;
	}

	m_myText.Empty();
	m_myText = W2BSTR(m_myString);

	CSharedFile sf(GMEM_MOVEABLE|GMEM_DDESHARE|GMEM_ZEROINIT);
	sf.Write(W2BSTR(toCopy), (toCopy.GetLength()*sizeof(TCHAR))); // You can write to the clipboard as you would to any CFile

	HGLOBAL hMem = sf.Detach();

	if (!hMem)
	{
	  MessageBox(_T("Error writing to the clipboard."), _T("Cut"));
	  return ;
	}

	m_pSource = new COleDataSource();

	m_pSource->CacheGlobalData(CF_UNICODETEXT, hMem);
	m_pSource->SetClipboard();

	var.bstrVal = W2BSTR(toCopy);
	m_iFirstDirtySeg = min(m_iSegI, m_rSegI);
	m_iLastDirtySeg = max(m_iSegI, m_rSegI);
	// m_cchDirtyDiff set above
	m_ichwRp = m_ichwIp;

	if(m_ichwIp == 0) // SJC: too simple; delete-forward should leave m_bAssocPrev = false
		m_bAssocPrev = false;
	else
		m_bAssocPrev = true;

	m_iRngI = baseRngI;
	m_bRng = (m_ichwIp != m_ichwRp);

	ClearNextCharProps(); // Should have no effect, but can't hurt

	Invalidate();
	UpdateWindow();

	// After display has been updated:
	CalcMaxHScroll();
	AdjustAndScrollSelection();
  }
  // else IP - do nothing
  return ;
}

void CSILGraphiteControlCtrl::Copy()
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  if(m_ichwIp != m_ichwRp)
  {
	CString myTempString(m_myText);
	CString toCopy;
	VARIANT var;
	VariantInit(&var);
	var.vt = VT_BSTR;

	if(m_ichwRp<m_ichwIp)
	{
	  for(int i = m_ichwRp; i< m_ichwIp;i++)
		toCopy.AppendChar(myTempString[i]);
	}
	else
	{
	  for(int j = m_ichwIp; j< m_ichwRp;j++)
		toCopy.AppendChar(myTempString[j]);
	}

	m_pSource = new COleDataSource();

	CSharedFile sf(GMEM_MOVEABLE|GMEM_DDESHARE|GMEM_ZEROINIT);

	sf.Write(W2BSTR(toCopy), (toCopy.GetLength()*sizeof(TCHAR))); // You can write to the clipboard as you would to any CFile

	HGLOBAL hMem = sf.Detach();
	if (!hMem) return ;

	m_pSource->CacheGlobalData(CF_UNICODETEXT, hMem);
	m_pSource->SetClipboard();

	var.bstrVal = W2BSTR(toCopy);
	Invalidate();
	UpdateWindow();
  }
  return ;
}

void CSILGraphiteControlCtrl::Paste()
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_undoStack.push(m_myText,m_ichwIp,m_ichwRp, m_rangeList);

  VARIANT var;
  VariantInit(&var);
  var.vt = VT_BSTR;

  COleDataObject obj;
  CString buffer;

  if (obj.AttachClipboard()) {
	if (obj.IsDataAvailable(CF_UNICODETEXT))
	{
	  HGLOBAL hmem = obj.GetGlobalData(CF_UNICODETEXT);
	  CMemFile sf((BYTE*) ::GlobalLock(hmem), ::GlobalSize(hmem));

	  LPCTSTR str = buffer.GetBufferSetLength(::GlobalSize(hmem));
	  sf.Read((void*) str, ::GlobalSize(hmem));
	  buffer.SetString(str);
	  ::GlobalUnlock(hmem);
	}
  }

  obj.Release();

  // note: ProcessText_UpdateRngLst updates m_rangeList also
  // must call ProcessText_UpdateRngLst b4 changing m_myString
  ProcessText_UpdateRngLst(buffer);

  int base  = min(m_ichwIp,m_ichwRp); // start point of selection.
  int limit = max(m_ichwIp,m_ichwRp); // end point of selection

  int baseSegI = min(m_iSegI, m_rSegI);

  int baseRngI = min(m_iRngI,m_rRngI);
  //int limitRngI = max(m_iRngI,m_rRngI);
  int baseRngStart = min(m_iRngStartPos,m_rRngStartPos);
  //int limitRngStart = max(m_iRngStartPos,m_rRngStartPos);

  int newLen = buffer.GetLength(); // length of new stuff
  if (m_chrpNextChar.dympHeight != -1
	  || m_chrpNextChar.clrBack != 0xFF000000
	  || m_chrpNextChar.clrFore != 0xFF000000
	  || m_nextCharFont.GetLength() > 0)
  {
	SetRangeProperties(base, base+newLen, baseRngI, baseRngI, baseRngStart, baseRngStart,
		m_chrpNextChar, m_nextCharFont, m_nextCharSizePercent);
  }

  if(m_bRng)
  {
	m_myString.Delete(base, limit-base); // delete the selection of text first
	m_cchDirtyDiff = base - limit; // negative number
  }
  m_myString.Insert(base, buffer);
  m_cchDirtyDiff += newLen;
  m_ichwIp = base + newLen;
  m_ichwRp = m_ichwIp;
  m_bRng = false;
  m_bAssocPrev = true;

  m_myText.Empty();
  m_myText = ::W2BSTR(m_myString);

  CleanUpRanges();

  m_iFirstDirtySeg = baseSegI;
  m_iLastDirtySeg = max(m_iSegI, m_rSegI);
  // m_cchDirtyDiff set above

  Invalidate();
  UpdateWindow();

  // Now that things have been redrawn, scroll the selection into view.
  CalcMaxHScroll();
  AdjustAndScrollSelection();

  return;
}

//Process a stream of text and updates m_rangeList accordingly.
CString CSILGraphiteControlCtrl::ProcessText_UpdateRngLst(const CString inText)
{
  CString cpy = inText;      //make a copy of inText, cpy will be destroyed
  CString onePara, outText; //Cstrings used for storing a para, output text, line breaks

  int loopIndex=0; // while loop index, initialised to 0
  int posInRange = m_ichwIp-m_iRngStartPos; // Pos of m_ichwIp relative to startPos of range.

  int limit = max(m_ichwIp,m_ichwRp); //end point of selection
  int baseRngI  = min(m_iRngI,m_rRngI); //index of range contains base (base-range) in m_rangeList
  int limitRngI = max(m_iRngI,m_rRngI); //index of range contains limit (limit-range)in m_rangeList

  Range rngTmp;
  POSITION baseRngPos = m_rangeList.FindIndex(baseRngI); //POSITION of range which m_ichwIp is in
  Range baseRng = m_rangeList.GetAt(baseRngPos);
  Range rngTmp1=baseRng;                          //rngTmp1 first temp range for calculation
  Range rngTmp2=baseRng;                          //as second part of curr range if it broken into 2
  bool oldFirstInPara=baseRng.firstInPara;
  bool insertAfter=false;
  bool setAt=false;
  int lnBrksAfterLimit = numLnBrB4(limit+2,m_myString);

  // Preparing m_rangeList by deleting anything that needs to be and/or dividing existing
  // ranges into two.

  if (!m_bRng) // no selection, base=limit
  {
	if (posInRange<=0) // Ip at beginning of the range
	{
	  if (rngTmp1.numChar==0 && m_rangeList.GetCount()>1)
		m_rangeList.RemoveAt(baseRngPos); // remove 0-char range.
	  else {
		 //new text will be inserted in front of current range,
		rngTmp1.firstInPara=false; // no longer be first range in paragraph
		m_rangeList.SetAt(baseRngPos,rngTmp1);
	  }
	} else if(posInRange>0 && posInRange<rngTmp1.numChar){//if Ip in mid of range, split it
	  //Current range will be broken into 2
	  //set up the 1st part of current range
	  rngTmp1.numChar=posInRange;
	  m_rangeList.SetAt(baseRngPos,rngTmp1);

	  //set up 2nd part of current range, this will not be 1st in para
	  rngTmp2.numChar=baseRng.numChar-posInRange;
	  rngTmp2.firstInPara=false;
	  m_rangeList.InsertAfter(baseRngPos,rngTmp2);

	  baseRngI++;//set baseRngI to 2nd part
	  limitRngI++;
	  oldFirstInPara=false;
	  posInRange=0;
	} else {
	  insertAfter=true;
	  oldFirstInPara=false;
	}
  } else {//have selection

	int base  = min(m_ichwIp,m_ichwRp); //start point of selection.

	int baseRngStart   = min(m_iRngStartPos,m_rRngStartPos); //startPos of base-range
	int limitRngStart  = max(m_iRngStartPos,m_rRngStartPos); //startPos of limit-range

	int oldRngNumChar=-1;
	POSITION rIpos;
	for (int rI=baseRngI; rI<=limitRngI; rI++) // update ranges in selection
	{
	  rIpos = m_rangeList.FindIndex(rI);
	  rngTmp = m_rangeList.GetAt(rIpos);

	  oldRngNumChar=rngTmp.numChar;

	  if (rI==baseRngI) //currently is baseRng
	  {
		if (baseRngI==limitRngI)//selection within a single range
		{
		  if (limit-base+numLnBrB4(limit+2,m_myString)==rngTmp.numChar){// selection involve one whole range, but no other range
			//adjust baseRngPos before remove and BREAK
			if(rI==0 && m_rangeList.GetCount()==1)//if only have one range, cannot remove anything, set the range to 0-char.
			{
			  rngTmp1.numChar=0;
			  m_rangeList.SetAt(rIpos,rngTmp1);
			  setAt=true;
			}else//not only first range
			  m_rangeList.RemoveAt(rIpos); //remove the range that is totally in selection

		  } else {//selection within a range, only part of the range selected
			//First part of old range
			rngTmp1.numChar=base-baseRngStart; //base range: number of character left in the range
			m_rangeList.SetAt(rIpos,rngTmp1);

			//Second part of old range
			rngTmp2.numChar = oldRngNumChar - (limit-limitRngStart);
			rngTmp2.firstInPara=false;
			m_rangeList.InsertAfter(rIpos,rngTmp2);

			oldFirstInPara=false;
			limitRngI++;
			baseRngI++;
		  }
		  break;
		} else { //selection spans to other range
		  //First part of old range
		  rngTmp1.numChar = base-baseRngStart; //part in selection deleted
		  m_rangeList.SetAt(rIpos,rngTmp1); // "remainder" of old baseRng
		}
	  } else if (rI==limitRngI){ //currrently is limitRng
		rngTmp.numChar = oldRngNumChar - (limit-limitRngStart);
		rngTmp.firstInPara=false;     //If the old limitRng is firstInPara, this have to be false.
		m_rangeList.SetAt(rIpos,rngTmp);//2nd part of limitRng that is not in selection
	  } else { //other ranges which the whole range in selection
		m_rangeList.RemoveAt(rIpos);
		limitRngI--;
		rI--;
	  }
	}//end for()
  }

  // Now insert the new stuff.

  if (limitRngI>m_rangeList.GetCount()-1) // last range, set baseRng to tail
  {
	limitRngI=m_rangeList.GetCount()-1;
	insertAfter=true;
  }
  const POSITION limitRngPos = m_rangeList.FindIndex(limitRngI);
  const Range limitRng = m_rangeList.GetAt(limitRngPos);         //set rngTmp to current range
  int paraLen=0;
  int lnBrks=0;

  while (!cpy.IsEmpty()) // if still have remaining text to process
  {
	onePara =cpy.SpanExcluding(L"\x000D\x000A\x0085\x2028\x2029");//get the 1st para from cpy
	outText += onePara;

	paraLen=onePara.GetLength();
	if (m_multiline)
	{
		// Multiline control: take line break into consideration
		lnBrks = numLnBrB4(paraLen+2, cpy); // CRLF
		if (lnBrks == 0)
			lnBrks = numLnBrB4(paraLen+1, cpy); // LF, etc
		paraLen+=lnBrks;

		cpy.Delete(0,paraLen); // delete off one para from cpy, include any line breaks.
	}
	else
	{
		// Single-line control: truncate at the line break.
		cpy.Empty();
	}

	if (loopIndex==0) // first time in loop
	{
	  rngTmp=baseRng; // use the properties of the base range
	  rngTmp.numChar=paraLen;
	  rngTmp.firstInPara=oldFirstInPara;

	  if (setAt)
	  {
		m_rangeList.SetAt(limitRngPos,rngTmp);
	  } else {
		if (insertAfter)
		{
		  m_rangeList.InsertAfter(limitRngPos, rngTmp);
		  limitRngI++;
		} else
		  m_rangeList.InsertBefore(limitRngPos, rngTmp);
	  }
	} else { //if> 2nd para, rngTmp set as new para
	  rngTmp=limitRng;
	  rngTmp.firstInPara = true;
	  if (!cpy.IsEmpty())
		rngTmp.numChar = paraLen;
	  else
		rngTmp.numChar = paraLen+lnBrksAfterLimit;
	  m_rangeList.InsertAfter(m_rangeList.FindIndex(limitRngI), rngTmp);

	  limitRngI++;
	  m_noOfParas++;
	}//end if(loopIndex...)else...

	loopIndex++;

  }//end while()

  return outText;
}

// Delete any empty ranges, and merge ranges with identical properties.
void CSILGraphiteControlCtrl::CleanUpRanges()
{
	int crng = m_rangeList.GetCount();

	if (crng <= 1)
		return; // nothing to do

	POSITION rngPos;
	Range rngTmp;

	for (int rI = crng - 1; rI >= 0; rI--)
	{
		rngPos = m_rangeList.FindIndex(rI);
		rngTmp = m_rangeList.GetAt(rngPos);
		if (rngTmp.numChar == 0
			// But a final empty line is okay...
			&& !(rI == crng - 1 || rngTmp.firstInPara))
		{
			// Transfer the first-in-para flag to the following range.
			if (rngTmp.firstInPara && rI < crng - 1)
			{
				POSITION rngNextPos = m_rangeList.FindIndex(rI + 1);
				Range rngNext = m_rangeList.GetAt(rngNextPos);
				rngNext.firstInPara = rngTmp.firstInPara;
				m_rangeList.SetAt(rngNextPos, rngNext);
			}
			// Delete empty range.
			m_rangeList.RemoveAt(rngPos);
			crng--;
			// Re-examine the following range, if any, in case it might now merge
			// with the previous.
			rI = min(rI + 1, crng);
			if (rI < crng)
				rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(rI));
		}
		else if (rI > 0 && !rngTmp.firstInPara)
		{
			POSITION rngPrevPos = m_rangeList.FindIndex(rI-1);
			Range rngPrev = m_rangeList.GetAt(rngPrevPos);
			if (rngPrev.sizePercent == rngTmp.sizePercent &&
				rngPrev.lgchrp.clrBack == rngTmp.lgchrp.clrBack &&
				rngPrev.lgchrp.clrFore == rngTmp.lgchrp.clrFore &&
				rngPrev.lgchrp.dympHeight == rngTmp.lgchrp.dympHeight &&
				rngPrev.lgchrp.dympOffset == rngTmp.lgchrp.dympOffset &&
				rngPrev.lgchrp.fWsRtl == rngTmp.lgchrp.fWsRtl &&
				rngPrev.lgchrp.nDirDepth == rngTmp.lgchrp.nDirDepth &&
				rngPrev.lgchrp.ssv == rngTmp.lgchrp.ssv &&
				rngPrev.lgchrp.ttvBold == rngTmp.lgchrp.ttvBold &&
				rngPrev.lgchrp.ttvItalic == rngTmp.lgchrp.ttvItalic &&
				wcscmp(rngPrev.lf.lfFaceName, rngTmp.lf.lfFaceName) == 0)
				//wcscmp(rngPrev.lgchrp.szFaceName, rngTmp.lgchrp.szFaceName) == 0)
			{
				// Merge ranges with identical properties.
				rngPrev.numChar += rngTmp.numChar;
				m_rangeList.RemoveAt(rngPos);
				m_rangeList.SetAt(rngPrevPos, rngPrev);
				crng--;
			}
		}
	}
}

// Set the properties of a range of text, possibly the selection, or something just typed
// or pasted.
void CSILGraphiteControlCtrl::SetRangeProperties(int base, int limit,
	int baseRngI, int limitRngI, int baseRngStart, int limitRngStart,
	LgCharRenderProps & chrpNew, CString & fontNameNew, int sizePercent)
{
	////int posInRange = m_ichwIp-m_iRngStartPos; // Pos of m_ichwIp relative to startPos of range.

	POSITION curPos = m_rangeList.FindIndex(m_iRngI);
	Range rngLocal = m_rangeList.GetAt(curPos);
	Range rngTmp1 = rngLocal;
	Range rngTmp2 = rngTmp1;
	Range newRng  = rngTmp1;

	if (base == limit)
		return; // nothing to change

	int oldRngNumChar=-1;
	bool oldFirstInPara = false;
	POSITION rngPosRI;
	for(int rI=baseRngI; rI<=limitRngI; rI++) // update ranges in selection
	{
		rngPosRI = m_rangeList.FindIndex(rI);
		rngLocal = m_rangeList.GetAt(rngPosRI);

		oldRngNumChar=rngLocal.numChar;
		oldFirstInPara=rngLocal.firstInPara;

		if (rI==baseRngI) //currently is baseRng
		{
			if (baseRngI==limitRngI)
			{
				// Selection within a single range
				if (limit-base==rngLocal.numChar)// selection involves one whole range, but no other range.
				{
					CopyPropsToRange(rngLocal, chrpNew, fontNameNew, sizePercent);
					m_rangeList.SetAt(rngPosRI,rngLocal);
				} else {
					// stuff before the change
					rngLocal.numChar=base-baseRngStart; //base range: number of character left in the range
					m_rangeList.SetAt(rngPosRI,rngLocal);

					// stuff after the change
					rngLocal.numChar = oldRngNumChar - (limit-limitRngStart); //limit range(i): number of character left in the range
					rngLocal.firstInPara=false;
					m_rangeList.InsertAfter(rngPosRI,rngLocal); //note!!!: iRange InsertAfter() b4 newRange.

					// new range to be inserted in between base-range, limit-range.
					rngLocal.numChar=limit-base; // number of character is the difference between m_ichwIp and m_ichwRp

					CopyPropsToRange(rngLocal, chrpNew, fontNameNew, sizePercent);
					rngLocal.firstInPara=false;
					m_rangeList.InsertAfter(rngPosRI,rngLocal);

					limitRngI += 2; // we just inserted 2
					rI++;
					break;
				}
			} else { //selection spans to next range
				rngLocal.numChar = base-baseRngStart;
				m_rangeList.SetAt(rngPosRI,rngLocal); // "remainder" of old baseRng

				rngLocal.numChar = oldRngNumChar - (base-baseRngStart);
				CopyPropsToRange(rngLocal, chrpNew, fontNameNew, sizePercent);
				rngLocal.firstInPara=false;
				m_rangeList.InsertAfter(rngPosRI,rngLocal); // part of old baseRng in selection

				limitRngI++;
				rI++;
			}
		} else if (rI==limitRngI) { //currrently is limitRng
			rngLocal.numChar = oldRngNumChar - (limit-limitRngStart);
			if(oldFirstInPara)
				rngLocal.firstInPara=false;      //if the old limitRng is firstInPara, this have to be false.
			m_rangeList.SetAt(rngPosRI,rngLocal);//2nd part of limitRng not in selection

			rngLocal.numChar = limit-limitRngStart;
			CopyPropsToRange(rngLocal, chrpNew, fontNameNew, sizePercent);
			if (oldFirstInPara)
				rngLocal.firstInPara=true; //if the old limitRng is firstInPara, this have to be true.
			m_rangeList.InsertBefore(rngPosRI,rngLocal); //1st part of limitRng in selection

			limitRngI++;
			rI++;
		} else {
			// other ranges for which the whole range in selection
			CopyPropsToRange(rngLocal, chrpNew, fontNameNew, sizePercent);
			m_rangeList.SetAt(rngPosRI,rngLocal);
		}
	}

	CleanUpRanges();
}

void CSILGraphiteControlCtrl::CopyPropsToRange(Range & rng,
	LgCharRenderProps chrp, CString & fontName, int sizePercent)
{
	if (chrp.dympHeight != -1)
	{
		if (sizePercent == -1)
			rng.lgchrp.dympHeight = chrp.dympHeight;
		else
			rng.lgchrp.dympHeight = m_currFontSize * sizePercent * 10;
		rng.sizePercent = sizePercent;
	}
	if (chrp.clrBack != 0xff000000)
		rng.lgchrp.clrBack = chrp.clrBack;
	if (chrp.clrFore != 0xff000000)
		rng.lgchrp.clrFore = chrp.clrFore;

	if (fontName.GetLength() > 0)
		wcscpy(rng.lf.lfFaceName, fontName);
}

VARIANT_BOOL CSILGraphiteControlCtrl::GetMultiline(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  return m_multiline;
}

void CSILGraphiteControlCtrl::SetMultiline(VARIANT_BOOL newVal)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  if (newVal == TRUE)
  {
	m_multiline = true;
	m_bVScrollBar = true;
	m_bHScrollBar = false;

	//Resetting the m_rDst
	m_rDst.top = 0;
	m_rDst.left = 0;
	m_rDst.right = 6;
	m_rDst.bottom = 2;
  }
  else if (m_noOfParas == 1 && newVal == false)
  {
	m_rDst.top = 0;
	m_rDst.left = 0;
	m_rDst.right = 6;
	m_rDst.bottom = 2;
	m_multiline = false;
	m_bVScrollBar = false;
	m_bHScrollBar = false;
  }
  else if(m_noOfParas > 1 && newVal == false)
	MessageBox(L"No Of paragraphs > 1, cannot switch to single line",
	L"Error",MB_OK);

  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;
  RecreateControlWindow();
}

void CSILGraphiteControlCtrl::OnVScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar)
{
  //SCROLLINFO si;
  //RECT rc;
  //int nNewScrollPos, nScrollDelta;

  GetClientRect( &m_rc );
  int oldScrollPos = g_nCurrentVScroll;
  int newScrollPos = g_nCurrentVScroll;

  int scrollDelta;

  switch ( nSBCode )
  {
  case SB_LINEUP:
	scrollDelta = -5;
	break;
  case SB_LINEDOWN:
	scrollDelta = 5;
	break;
  case SB_PAGEUP:
	scrollDelta = -m_rc.bottom;
	break;
  case SB_PAGEDOWN:
	scrollDelta = m_rc.bottom;
	break;
  case SB_THUMBTRACK:
	scrollDelta = nPos - g_nCurrentVScroll;
	break;
  default:
	// If the current position does not change, do not scroll.
	return;
  }

  // Always show at least one line's worth.
  DrawnSeg segTmp = m_segList.GetAt(m_segList.FindIndex(m_segList.GetCount()-1));
  scrollDelta = min(scrollDelta, m_rDst.top - segTmp.rSrc.top);
  // Never scroll above the original top!
  if (m_rDst.top - scrollDelta > m_rDstOri.top)
	  scrollDelta = m_rDst.top - m_rDstOri.top;

  int scrollDeltaTmp = max(scrollDelta, m_rDst.top - m_rDstOri.top);

  newScrollPos = g_nCurrentVScroll + scrollDelta;

  // Reset the current scroll position.
  g_nCurrentVScroll = newScrollPos;

  m_rDst.top -= scrollDelta;
  m_rDst.bottom -= scrollDelta;

  // Scroll the window. (The system repaints most of the
  // client area when ScrollWindowEx is called; however, it is
  // necessary to call UpdateWindow in order to repaint the
  // rectangle of pixels that were invalidated.)

  m_si.cbSize = sizeof( m_si );
  m_si.fMask  = SIF_POS;
  m_si.nPos   = g_nCurrentVScroll;

  ScrollWindowEx( 0, -scrollDelta, NULL, NULL, NULL, &m_rc, SW_ERASE | SW_INVALIDATE );

  // Reset the scroll bar.
  if (m_bVScrollBar)
	SetScrollInfo( SB_VERT, &m_si, TRUE );

  Invalidate();
  UpdateWindow();

  COleControl::OnVScroll(nSBCode, nPos, pScrollBar);
}

void CSILGraphiteControlCtrl::SetVerticalScroll(VARIANT_BOOL scrollFlag)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  if (m_multiline == true)
	m_bVScrollBar = scrollFlag;
  else
	m_bVScrollBar = false;

  RecreateControlWindow();
}

void CSILGraphiteControlCtrl::OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar)
{
  //SCROLLINFO si;
  //RECT rc;
  //int nNewScrollPos, nScrollDelta;

  GetClientRect( &m_rc );
  int oldScrollPos = g_nCurrentHScroll;
  int newScrollPos = g_nCurrentHScroll;

  switch ( nSBCode )
  {
  case SB_LINELEFT:
	newScrollPos = g_nCurrentHScroll - 5;
	break;
  case SB_LINERIGHT:
	newScrollPos = g_nCurrentHScroll + 5;
	break;
	//case SB_PAGELEFT:
	//  m_nNewScrollPos = g_nCurrentHScroll - m_rc.right;
	//  break;
	//case SB_PAGERIGHT:
	//  m_nNewScrollPos = g_nCurrentHScroll + m_rc.right;
	//  break;
  case SB_THUMBTRACK:
	newScrollPos = nPos;
	break;
  default:
	// If the current position does not change, do not scroll.
	return;
  }
  // New position must be between 0 and the screen width.
  //m_nNewScrollPos = max( 0, m_nNewScrollPos );
  //m_nNewScrollPos = min( g_nMaxVScroll, m_nNewScrollPos );

  // Determine the amount scrolled (in pixels).
  int nScrollDelta = newScrollPos - g_nCurrentHScroll;

  // Reset the current scroll position.
  g_nCurrentHScroll = newScrollPos;

  // Scroll the window. (The system repaints most of the
  // client area when ScrollWindowEx is called; however, it is
  // necessary to call UpdateWindow in order to repaint the
  // rectangle of pixels that were invalidated.)

  m_rDst.left -= nScrollDelta;
  m_rDst.right -= nScrollDelta;

  m_si.cbSize = sizeof( m_si );
  m_si.fMask  = SIF_POS;
  m_si.nPos   = g_nCurrentHScroll;

  if (m_acDxWidth < -m_rDst.left)
  {
	m_rDst.left += nScrollDelta;
	m_rDst.right+= nScrollDelta;
	nScrollDelta = 0;
	g_nCurrentHScroll = oldScrollPos;
	m_si.nPos = oldScrollPos;
  }
  if (m_rDst.left > m_rDstOri.left)
  {
	m_rDst.left = m_rDstOri.left;
	m_rDst.right = m_rDstOri.right;
  }

  ScrollWindowEx( 0, -nScrollDelta, NULL, NULL, NULL, &m_rc, SW_ERASE | SW_INVALIDATE );

  // Reset the scroll bar.
  if (m_bHScrollBar)
	SetScrollInfo( SB_HORZ, &m_si, TRUE );
  Invalidate();
  UpdateWindow();
  COleControl::OnHScroll(nSBCode, nPos, pScrollBar);
}

void CSILGraphiteControlCtrl::HorizontalScroll(VARIANT_BOOL horz)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  if(m_multiline == true)
	m_bHScrollBar = horz;
  else
	m_bHScrollBar = false;

  RecreateControlWindow();
}

void CSILGraphiteControlCtrl::UpdateScrollBars( HWND hwnd )
{
  SCROLLINFO siTmp;
  RECT rcTmp;

  siTmp.cbSize = sizeof( siTmp );
  siTmp.fMask  = SIF_PAGE | SIF_POS | SIF_RANGE;
  siTmp.nMin   = 0;

  GetClientRect( &rcTmp );

  g_nCurrentHScroll = min( g_nCurrentHScroll, g_nMaxHScroll );
  siTmp.nMax   = g_nMaxHScroll;
  siTmp.nPage  = rcTmp.right;
  siTmp.nPos   = g_nCurrentHScroll;
  if (m_bHScrollBar)
	SetScrollInfo(SB_HORZ, &siTmp, TRUE );

  int oldVScroll = g_nCurrentVScroll;
  // We need +4 here to accommodate the amount of space that ScrollSelectionIntoView
  // tries to leave below the IP. Without it, after each keystroke this code moves
  // things up a bit and then ScrollSelectionIntoView moves it back down, producing
  // flicker.
  g_nCurrentVScroll = min( g_nCurrentVScroll, max(0, g_nMaxVScroll - rcTmp.bottom + 4));
  if (oldVScroll != g_nCurrentVScroll)
  {
	ScrollWindowEx( 0, oldVScroll - g_nCurrentVScroll, NULL, NULL, NULL,
		&m_rc, SW_ERASE | SW_INVALIDATE );
	m_rDst.top = g_nCurrentVScroll * -1;
	m_rDst.bottom = m_rDst.top + (m_rSrc.bottom - m_rSrc.top);
  }
  siTmp.nMax   = g_nMaxVScroll;
  siTmp.nPage  = rcTmp.bottom;
  siTmp.nPos   = g_nCurrentVScroll;
  if (m_bVScrollBar)
	SetScrollInfo( SB_VERT, &siTmp, TRUE );
}

void CSILGraphiteControlCtrl::OnWindowPosChanging(WINDOWPOS* lpwndpos)
{
  COleControl::OnWindowPosChanging(lpwndpos);
  UpdateScrollBars(this->m_hWnd);//this->m_hWnd);
}

void CSILGraphiteControlCtrl::ResizeWindow(LONG x, LONG y)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  this->SetControlSize(x,y);
}

LONG CSILGraphiteControlCtrl::GetSizeX(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  CRect clientRect;
  GetClientRect(clientRect);
  return (clientRect.Width()-20);
}

LONG CSILGraphiteControlCtrl::GetSizeY(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  CRect clientRect;
  GetClientRect(clientRect);
  return (clientRect.Height());
  return 0;
}

void CSILGraphiteControlCtrl::New()
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  m_undoStack.clear();
  m_redoStack.clear();
  m_rangeList.RemoveAll();

  m_myText.Empty();
  m_myText = W2BSTR(m_myString);
  m_currFileName = L"Untitled";

  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;

  Invalidate();
  UpdateWindow();
}

// TODO: merge with OnAcceleratorOpen
void CSILGraphiteControlCtrl::Open(BSTR pVar, LONG flags, LONG CodePage)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_undoStack.clear();
  m_redoStack.clear();
  m_rangeList.RemoveAll();

  CString pathName(pVar);
  CStdioFile txtFile(pathName,CFile::modeRead|CFile::typeBinary/*CFile::typeText*/ );

  m_myString.Empty();
  CString myTempString("Dummy String");

  while(!myTempString.IsEmpty())
  {
	myTempString.Empty();
	txtFile.ReadString(myTempString);
	m_myString.Append(W2BSTR(myTempString));
  }

  m_myText.Empty();
  m_myText = ::W2BSTR(m_myString);
  txtFile.Close();
  m_currFileName = pathName;

  Range rngTmp;
  InitNewRange(rngTmp);
  rngTmp.numChar = m_myString.GetLength();
  m_rangeList.AddHead(rngTmp);

  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;

  Invalidate();
  UpdateWindow();
  return ;
}

void CSILGraphiteControlCtrl::Save(BSTR pVar, LONG flags, LONG CodePage)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  CString pathName(pVar);
  CStdioFile txtFile(pathName, CFile::modeCreate|
	CFile::modeWrite|CFile::typeText);
  CString myTempString(m_myText);
  txtFile.Write(m_myText,(myTempString.GetLength() * sizeof(TCHAR)));
  txtFile.Close();
  //txtFile.WriteString(myTempString);
  MessageBox(myTempString,L"Data Saved",MB_OK);
  m_currFileName = pathName;
  return ;
}


void CSILGraphiteControlCtrl::MoveRight(bool ShiftState)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  SetFocus();
  OnKeyDownEvent(VK_RIGHT,ShiftState? SHIFT_MASK:0);
  Invalidate();
  UpdateWindow();
  SetFocus();
  return ;
}

void CSILGraphiteControlCtrl::MoveLeft(bool ShiftState)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  SetFocus();
  OnKeyDownEvent(VK_LEFT,ShiftState? SHIFT_MASK:0);
  Invalidate();
  UpdateWindow();
  SetFocus();
  return ;
}

void CSILGraphiteControlCtrl::MoveUp(bool ShiftState)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  SetFocus();
  OnKeyDownEvent(VK_UP,ShiftState? SHIFT_MASK:0);
  Invalidate();
  UpdateWindow();
  SetFocus();
}

void CSILGraphiteControlCtrl::MoveDown(bool ShiftState)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  SetFocus();
  OnKeyDownEvent(VK_DOWN,ShiftState? SHIFT_MASK:0);
  Invalidate();
  UpdateWindow();
  SetFocus();
}

void CSILGraphiteControlCtrl::Undo()
{
  int index = 0;
  int Count = 1;
  while(index < Count && !m_undoStack.isEmpty())
  {
	m_redoStack.push(m_myText,m_ichwIp,m_ichwRp,m_rangeList);
	m_myText.Empty();
	m_undoStack.pop(&m_myText,&m_ichwIp,&m_ichwRp, &m_rangeList);
	index++;
  }

  m_myString = W2BSTR(m_myText);
  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;
  Invalidate();
  UpdateWindow();
}

void CSILGraphiteControlCtrl::Redo()
{
  int index = 0;
  int Count = 1;

  while(index < Count && !m_redoStack.isEmpty())
  {
	m_undoStack.push(m_myText,m_ichwIp,m_ichwRp, m_rangeList);
	m_myText.Empty();
	m_redoStack.pop(&m_myText,&m_ichwIp,&m_ichwRp, &m_rangeList);
	index++;
  }

  m_myString = W2BSTR(m_myText);

  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;
  Invalidate();
  UpdateWindow();
}

void CSILGraphiteControlCtrl::TextColor(BYTE r, BYTE g, BYTE b)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_undoStack.push(m_myText,m_ichwIp,m_ichwRp, m_rangeList);

  int posInRange = m_ichwIp-m_iRngStartPos; // Pos of m_ichwIp relative to startPos of range.

  POSITION curPos = m_rangeList.FindIndex(m_iRngI);
  Range rngLocal = m_rangeList.GetAt(curPos);
  Range rngTmp1 = rngLocal;
  Range rngTmp2 = rngTmp1;
  Range newRng  = rngTmp1;

  if(!m_bRng){
	if(posInRange==0)//beginning of the range: add 1 0-char range at begining old m_rangeList
	{
	  if(rngTmp1.numChar>0)//if current range >0 chracters
	  {
		newRng.numChar=0;//new range
		newRng.lgchrp.clrFore = RGB(r, g, b);
		m_rangeList.InsertBefore(curPos,newRng);

		rngTmp1.firstInPara=false; //old range: set to false
		m_rangeList.SetAt(curPos,rngTmp1);
	  }else{
		rngTmp1.lgchrp.clrFore = RGB(r, g, b);
		m_rangeList.SetAt(curPos,rngTmp1);
	  }
	}else if(posInRange>0 && posInRange<rngTmp1.numChar){//in the middle of the range
	  int oldNumChar=rngTmp1.numChar;

	  rngTmp1.numChar=posInRange; //old range broken into 2 parts, new range insert in between

	  newRng.numChar=0;//new range to be inserted in between
	  newRng.lgchrp.clrFore = RGB(r, g, b);
	  newRng.firstInPara=false;

	  rngTmp2.numChar=oldNumChar-posInRange;// 2nd part of the old range
	  rngTmp2.firstInPara=false;

	  //must be in this order: set rng1, insert rng2, then insert newrng.
	  m_rangeList.SetAt(curPos,rngTmp1);
	  m_rangeList.InsertAfter(curPos,rngTmp2);
	  m_rangeList.InsertAfter(curPos,newRng);
	  m_iRngI++;

	}else{ //at the end of the range, add a 0-char range after cur range.
	  newRng.numChar=0;
	  newRng.lgchrp.clrFore = RGB(r, g, b);
	  newRng.firstInPara=false;
	  m_rangeList.InsertAfter(curPos,newRng);
	  m_iRngI++;
	}
  }else{ //have selection
	int base = min(m_ichwIp,m_ichwRp); //start point of selection.
	int limit = max(m_ichwIp,m_ichwRp);//end point of selection

	int baseRngI  = min(m_iRngI,m_rRngI); //index of range contains base (base-range) in m_rangeList
	int limitRngI = max(m_iRngI,m_rRngI); //index of range contains limit (limit-range)in m_rangeList

	int baseRngStart = min(m_iRngStartPos,m_rRngStartPos); //startPos of base-range
	int limitRngStart  = max(m_iRngStartPos,m_rRngStartPos); //startPos of limit-range

	int oldRngNumChar=-1;
	bool oldFirstInPara = false;
	POSITION rngPosRI;
	for(int rI=baseRngI; rI<=limitRngI; rI++) // update ranges in selection
	{
	  rngPosRI = m_rangeList.FindIndex(rI);
	  rngLocal = m_rangeList.GetAt(rngPosRI);

	  oldRngNumChar=rngLocal.numChar;
	  oldFirstInPara=rngLocal.firstInPara;

	  if(rI==baseRngI) //currently is baseRng
	  {
		if(baseRngI==limitRngI)//selection within a single range
		{
		  if(limit-base==rngLocal.numChar)// selection involve one whole range, but no other range.
		  {
			rngLocal.lgchrp.clrFore = RGB(r, g, b);
			m_rangeList.SetAt(rngPosRI,rngLocal);
		  }else{
			rngLocal.numChar=base-baseRngStart; //base-range: number of character left in the range
			m_rangeList.SetAt(rngPosRI,rngLocal);

			rngLocal.numChar = oldRngNumChar - (limit-limitRngStart); //limit-range(i): number of character left in the range
			rngLocal.firstInPara=false;
			m_rangeList.InsertAfter(rngPosRI,rngLocal); //note!!!: iRange InsertAfter() b4 newRange.

			// new range to be inserted in between base-range, limit-range.
			rngLocal.numChar=limit-base; // number of character is the difference between m_ichwIp and m_ichwRp
			rngLocal.lgchrp.clrFore = RGB(r, g, b);
			rngLocal.firstInPara=false;
			m_rangeList.InsertAfter(rngPosRI,rngLocal);

			baseRngI++;
			break;
		  }
		}else{ //selection spans to other range
		  rngLocal.numChar = base-baseRngStart;
		  m_rangeList.SetAt(rngPosRI,rngLocal); // "remainder" of old baseRng

		  rngLocal.numChar = oldRngNumChar - (base-baseRngStart);
		  rngLocal.lgchrp.clrFore = RGB(r, g, b);
		  rngLocal.firstInPara=false;
		  m_rangeList.InsertAfter(rngPosRI,rngLocal); // part of old baseRng in selection

		  limitRngI++;
		  rI++;
		}
	  }else if(rI==limitRngI){ //currrently is limitRng
		rngLocal.numChar = oldRngNumChar - (limit-limitRngStart);
		if(oldFirstInPara)
		  rngLocal.firstInPara=false; //if the old limitRng is firstInPara, this have to be false.
		m_rangeList.SetAt(rngPosRI,rngLocal);//2nd part of limitRng not in selection

		rngLocal.numChar = limit-limitRngStart;
		rngLocal.lgchrp.clrFore = RGB(r, g, b);
		if(oldFirstInPara)
		  rngLocal.firstInPara=true; //if the old limitRng is firstInPara, this have to be true.
		m_rangeList.InsertBefore(rngPosRI,rngLocal); //1st part of limitRng in selection

		limitRngI++;
		rI++;
	  }else{ //other ranges which the whole range in selection
		rngLocal.lgchrp.clrFore = RGB(r, g, b);
		m_rangeList.SetAt(rngPosRI,rngLocal);
	  }
	}
  }

  CleanUpRanges();

  m_iFirstDirtySeg = 0; // when implemented figure first segment affected.
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;
  Invalidate();
  UpdateWindow();
}

//Recreate m_segList, after resizing window(witdh changed)
void CSILGraphiteControlCtrl::OnSize(UINT nType, int cx, int cy)
{
  COleControl::OnSize(nType, cx, cy);
  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;
}

void CSILGraphiteControlCtrl::OnShowWindow(BOOL bShow, UINT nStatus)
{
  COleControl::OnShowWindow(bShow, nStatus);
  SetFocus();
}

void CSILGraphiteControlCtrl::OnKillFocus(CWnd* pNewWnd)
{
  COleControl::OnKillFocus(pNewWnd);
  m_focusKilled = true;
  HideCaret();
  Invalidate(FALSE);
}

void CSILGraphiteControlCtrl::OnSetFocus(CWnd* pOldWnd)
{
  COleControl::OnSetFocus(pOldWnd);
  m_focusKilled = false;
  ShowCaret();
  Invalidate(FALSE);
}

// Set the contents of the edit control to the equivalent of the given HTML text.
// Treat the BSTR as zero-extended 8-bit data.
void CSILGraphiteControlCtrl::PutHtmlText(LPCTSTR text)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_undoStack.clear();
  m_redoStack.clear();
  m_rangeList.RemoveAll();

  wchar * pchw = (wchar *)text;
  int cch = wcslen(pchw);

  char * rgchs = new char[cch + 1];
  for (int ich = 0; ich < cch; ich++)
	  rgchs[ich] = (char)pchw[ich];
  rgchs[cch] = 0;

  FileOrBuffer fb;
  fb.SetBuffer(rgchs, cch);

  ParseHtml(fb);

  delete[] rgchs;

  CleanUpRanges();

  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;

  Invalidate();
  UpdateWindow();
}

// Return a block of text which is the HTML equivalent of the contents of the control.
// The returned BSTR contains 8-bit data zero-extended to fit in the BSTR which is
// 16-bit.
void CSILGraphiteControlCtrl::GetHtmlText(BSTR * text)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  char * pch = NULL;
  int cch = GenerateHtml(&pch);

  wchar * rgchw = new wchar[cch + 1];
  for (int ich = 0; ich < cch; ich++)
	  rgchw[ich] = (wchar)pch[ich];
  rgchw[cch] = 0;

  CComBSTR returnText = ::W2BSTR(rgchw);
  returnText.CopyTo(text);

  delete[] pch;
  delete[] rgchw;

  return;
}

void CSILGraphiteControlCtrl::OnKeyDownEvent(USHORT nChar, USHORT nShiftState)
{
  CClientDC DC(this);

  m_keydown=true;
  SHORT shiftstate = GetKeyState(VK_SHIFT);
  SHORT ctlstate = GetKeyState(VK_CONTROL);
  bool bCtl = (bool)(ctlstate & 0x8000);
  bool bShift = (bool) (shiftstate & 0x8000);
  bool bUpdateWnd=false;
  bool bDataChanged = false;

if (bShift)
{
	int x; x = 3;
}

  int oldIchwIp=m_ichwIp;
  DrawnSeg iSegOld = m_segList.GetAt(m_segList.FindIndex(m_iSegI));

  if(m_beep == true)
	Beep(50,10);

  Range rngTmp;

  switch(nChar)
  {
	bool bRight;
	LgIpValidResult IpValid;
	int oldIp;

  case 'C': case 'c':
	{
	  if (bCtl) // Ctrl-c
	  {
		Copy();
		bUpdateWnd = true;
	  }

	  break;
	}
  case 'X': case 'x':
	{
	  if (bCtl)
	  {
		Cut();
		bUpdateWnd = true;
	  }

	  break;
	}
  case 'V': case 'v':
	{
	  if (bCtl)
	  {
		Paste();
		bUpdateWnd = true;
	  }

	  break;
	}
  case 'Z': case 'z':
	{
	  if (bCtl)
	  {
		Undo();
		bUpdateWnd = true;
	  }

	  break;
	}
  case 'Y': case 'y':
	{
	  if(bCtl)
	  {
		Redo();
		bUpdateWnd = true;
	  }

	  break;
	}
  case VK_DELETE:
	{
	  m_undoStack.push(m_myText,m_ichwIp,m_ichwRp, m_rangeList);
	  m_myString.SetString(CString(m_myText));

	  int base = min(m_ichwRp,m_ichwIp);
	  int limit = max(m_ichwRp,m_ichwIp);
	  int numCharAffected = limit-base;

	  int baseRngI = min(m_iRngI,m_rRngI);
	  int limitRngI = max(m_iRngI,m_rRngI);
	  int baseRngStart = min(m_iRngStartPos,m_rRngStartPos);
	  int limitRngStart = max(m_iRngStartPos,m_rRngStartPos);

	  POSITION baseRngPos = m_rangeList.FindIndex(baseRngI);
	  POSITION limitRngPos = m_rangeList.FindIndex(limitRngI);
	  Range baseRng = m_rangeList.GetAt(baseRngPos); //Get the anchor range
	  Range limitRng = m_rangeList.GetAt(limitRngPos);

	  int baseSegI = min(m_rSegI,m_iSegI);
	  int limitSegI = max(m_rSegI,m_iSegI);
	  int baseSegStart = min(m_iSegStartPos,m_rSegStartPos);
	  int limitSegStart = max(m_iSegStartPos,m_rSegStartPos);

	  POSITION limitSegPos = m_segList.FindIndex(limitSegI);
	  DrawnSeg limitSeg = m_segList.GetAt(limitSegPos);
	  if (m_ichwIp > m_myString.GetLength()-1 && !m_bRng)
		break;

	  rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(m_iRngI));

	  if (m_bRng)//have selection, this part same as the backspace.
	  {
		if ( baseRngPos != limitRngPos) //selection NOT within a single range.
		{
		  baseRng.numChar =  base-baseRngStart;
		  m_rangeList.SetAt(baseRngPos,baseRng);

		  limitRng.numChar -= limit-limitRngStart; // number of character left in old range with limit
		  if (limitRng.firstInPara)
		  {
			limitRng.firstInPara=false;
			m_noOfParas--;
		  }
		  m_rangeList.SetAt(limitRngPos,limitRng);

		  int removeAtRI = baseRngI+1;
		  for (int rI = removeAtRI ; rI<=limitRngI-1; rI++)//delete unnecessary ranges in betweens.
		  {
			rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(removeAtRI));
			if(rngTmp.firstInPara)
			  m_noOfParas--;
			m_rangeList.RemoveAt(m_rangeList.FindIndex(removeAtRI));
		  }
		} else {//selection within a single range, baseRng=limitRng
		  limitRng.numChar -=  limit-base;
		  m_rangeList.SetAt(limitRngPos,limitRng);
		}
		m_iSegI=baseSegI;
		m_myString.Delete(base,limit-base);
		m_cchDirtyDiff = base - limit; // negative number
		m_ichwIp = base;
	  } else {//no selection, base=limit
		DrawnSeg lastSeg = m_segList.GetTail();
		int lastI = lastSeg.stopPos;
		if(m_ichwIp==lastI)
		  return;

		int posInRange = limit-limitRngStart;
		int lnBrksDel = 0;
		int ToBeDel = 0;

		if(m_ichwIp==lastI-1)
		  lnBrksDel = numLnBrB4(m_ichwIp+1,m_myString);
		else
		  lnBrksDel = numLnBrB4(m_ichwIp+2,m_myString);

		if(lnBrksDel==2)
		  ToBeDel=2;
		else
		  ToBeDel=1;

		if (posInRange==limitRng.numChar && limitRngI+1<m_rangeList.GetCount()) //delete into next range.
		{
		  POSITION nextRngPos = m_rangeList.FindIndex(limitRngI+1);
		  Range nextRng = m_rangeList.GetAt(nextRngPos);

		  if (lnBrksDel>=1)//deleted char is linebreak
		  {
			nextRng.firstInPara=false;
			m_noOfParas--;
		  }

		  nextRng.numChar-=ToBeDel;
		  if (nextRng.numChar==0)
		  {
			if(limitRngI<m_rangeList.GetCount()-2)
			{
			  POSITION nextnextPos = m_rangeList.FindIndex(limitRngI+2);
			  Range nextnext = m_rangeList.GetAt(nextnextPos);
			  nextnext.firstInPara=false;
			  m_rangeList.SetAt(nextnextPos,nextnext);
			}
			m_rangeList.RemoveAt(nextRngPos);
		  } else
			m_rangeList.SetAt(nextRngPos,nextRng);
		} else { //deleted char not from next range
		  limitRng.numChar-=ToBeDel;
		  if(lnBrksDel>=1)
		  {
			POSITION nextRngPos = m_rangeList.FindIndex(limitRngI+1);
			Range nextRng = m_rangeList.GetAt(nextRngPos);
			nextRng.firstInPara=false;
			m_rangeList.SetAt(nextRngPos,nextRng);
		  }

		  if(limitRng.numChar==0 && limitRngI < m_rangeList.GetCount()-1)
			m_rangeList.RemoveAt(limitRngPos);
		  else
			m_rangeList.SetAt(limitRngPos,limitRng);
		}
		m_myString.Delete(limit,ToBeDel);
		m_cchDirtyDiff = ToBeDel;
		m_ichwIp = limit;
	  }

	  m_myText.Empty();
	  m_myText = ::W2BSTR(m_myString);
	  m_iFirstDirtySeg = min(m_iSegI, m_rSegI);
	  m_iLastDirtySeg = max(m_iSegI, m_rSegI);
	  // m_cchDirtyDiff set above

	  // Reconstruct the display.
	  Invalidate();
	  UpdateWindow();

	  bUpdateWnd=true;
	  break;
	}
  case VK_LEFT:
  case VK_RIGHT: // left & right arrow keys
	{
	  bool bRngOld = m_bRng;
	  int lastSegI = m_segList.GetCount()-1;
	  int lnBrks=0;

	  bRight = (nChar == VK_RIGHT);

	  DrawnSeg segTmp;
	  int iSegISel = m_iSegI;
	  segTmp = m_segList.GetAt(m_segList.FindIndex(iSegISel));

	  rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(segTmp.rI));

	  GrGraphics grfxTmp;
	  GrfxInit(DC.m_hDC,grfxTmp,rngTmp.lf,rngTmp.lgchrp);

	  if (!bShift) // no shift key
	  {
		m_bRng = false;
		if (!bCtl) // no control key & no shift key
		{
		  if (bRngOld == false)
		  {
			  m_ichwIp = NextArrowKeyPosition(m_ichwIp, bRight, grfxTmp, &iSegISel);
			  if (m_iSegI != iSegISel)
			  {
				m_iSegI = iSegISel;
				segTmp = m_segList.GetAt(m_segList.FindIndex(m_iSegI));
			  }
		  }
		  else
		  {
			// contract the range down to an IP
			if (bRight)
				m_ichwIp = max(m_ichwIp, m_ichwRp);
			else
				m_ichwIp = min(m_ichwIp, m_ichwRp);
		  }
		  m_ichwRp = m_ichwIp;
		  m_bAssocPrev = bRight;
		  // Make sure that the association is with a character in the current segment.
		  if (m_bAssocPrev)
		  {
			  // Should not be at the start of a segment that is not contiguous with the
			  // previous one.
			  if (m_ichwIp == segTmp.startPos)
			  {
					// Can't associate with previous or won't be drawn by this seg!
					m_bAssocPrev = false;
			  }
		  }
		  else
		  {
			  int cch;
			  segTmp.pgrseg->get_Lim(segTmp.startPos, &cch);
			  if (m_ichwIp == segTmp.startPos + cch)
			  {
				  // Can't associate with next or won't be drawn by this seg!
				  m_bAssocPrev = true;
			  }
		  }
		}
		else // control key, no shift key
		{
		  oldIp = m_ichwIp;

		  segTmp.pgrseg->IsValidInsertionPoint(segTmp.startPos, &grfxTmp, m_ichwIp, &IpValid);
		  if (IpValid != kipvrOK)
			m_ichwIp = oldIp;
		  if (m_ichwIp == 0)
			m_bAssocPrev = false ;
		  else
			m_bAssocPrev = true;
		}
	  }
	  else // shift key
	  {
		if (!m_bRng)
		  m_ichwRp = m_ichwIp;
		m_bRng = true;

		if (!bCtl) // shift key, no control key
		{
			m_ichwIp = NextArrowKeyPosition(m_ichwIp, bRight, grfxTmp, &iSegISel);
			m_iSegI = iSegISel;
		}
		else // control key & shift key
		{
		  oldIp = m_ichwIp;

		  segTmp.pgrseg->IsValidInsertionPoint(segTmp.startPos, &grfxTmp, m_ichwIp, &IpValid);
		  if (IpValid != kipvrOK)
			m_ichwIp = oldIp;
		  if (m_ichwIp == 0 /*&& i==0*/)
			m_bAssocPrev = false;
		  else
			m_bAssocPrev = true;
		}
	  }

	  bUpdateWnd=true;
	  break;
	} // end for
  case VK_HOME:
	{
	  if (!bCtl) //if no control key
	  {
		int sI=m_iSegI;
		DrawnSeg segTmp = m_segList.GetAt(m_segList.FindIndex(m_iSegI));
		int lineTop = segTmp.lineRsrcTop;

		while (sI >= 0)
		{
		  segTmp = m_segList.GetAt(m_segList.FindIndex(sI));
		  if (segTmp.lineRsrcTop != lineTop)
		  {
			segTmp = m_segList.GetAt(m_segList.FindIndex(sI+1));
			break;
		  }
		  sI--;
		}
		m_ichwIp = segTmp.startPos;
	  } else { // Ctrl-Home
		m_ichwIp = 0;
	  }
	  m_bAssocPrev = false;

	  if (!bShift) // no shift key
		m_ichwRp = m_ichwIp;
	  else // Shift-Home or Shift-Ctrl-Home.
		m_bRng=true;

	  bUpdateWnd=true;
	  break;
	}
  case VK_END:
	{
	  if (!bCtl) // if Control-key not pressed down
	  {
		int sI=m_iSegI;
		DrawnSeg segTmp = m_segList.GetAt(m_segList.FindIndex(m_iSegI));
		int lineTop = segTmp.lineRsrcTop;

		while (sI <= m_segList.GetCount()-1)
		{
		  segTmp = m_segList.GetAt(m_segList.FindIndex(sI));
		  if (segTmp.lineRsrcTop != lineTop)
		  {
			segTmp = m_segList.GetAt(m_segList.FindIndex(sI-1));
			break;
		  }
		  sI++;
		}
		int lnBrks=numLnBrB4(segTmp.stopPos, m_myString);
		m_ichwIp =  segTmp.stopPos-lnBrks;
	  } else { // Ctrl-End
		m_ichwIp = m_myString.GetLength();
	  }
	  m_bAssocPrev = true;

	  if (!bShift) // no shift key
		m_ichwRp = m_ichwIp;
	  else // Shift-End or Shift-Ctrl-End
		  m_bRng = true;

	  bUpdateWnd=true;
	  break;
	}
	//TODO: with selection.
	//case A if have selection but no shift: base is used to determine next position
	//case B if have selection, have shift : m_ichwIp is used to determine the next position.
  case VK_UP:
  case VK_DOWN:
	{
	  GrGraphics grfxTmp;
	  DrawnSeg segTmp;
	  int segI = 0;
	  if (bShift)
		  segI = m_iSegI; // iseg
	  else
	  {
		  if (nChar == VK_UP)
		  {
			  segI = min(m_iSegI, m_rSegI);
			  m_ichwIp = min(m_ichwRp, m_ichwIp);
		  } else {
			  segI = max(m_iSegI, m_rSegI);
			  m_ichwIp = max(m_ichwRp, m_ichwIp);
		  }
	  }
	  segTmp = m_segList.GetAt(m_segList.FindIndex(segI)); // limit seg

	  POINT newCaretPos = GetIpOrRangeEnd().TopLeft();

	  if (nChar==VK_UP)
	  {
		if (-segTmp.lineRsrcTop <= m_rDstOri.bottom)//1st line
		  break;

		newCaretPos.y = (-segTmp.lineRsrcTop + m_rDst.top) - 3; // !!! caretPos can pose a problem for case A
	  } else {
		newCaretPos.y = (-segTmp.lineRsrcTop + m_rDst.top) + segTmp.lineHeight + 3;
	  }

	  m_iSegI = GetCurrentSegI(newCaretPos);
	  segTmp = m_segList.GetAt(m_segList.FindIndex(m_iSegI));

	  if (segTmp.dxWidth==0)
		m_ichwIp=segTmp.startPos;
	  else {
		rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(segTmp.rI));
		GrfxInit(DC.m_hDC,grfxTmp,rngTmp.lf,rngTmp.lgchrp);

		segTmp.pgrseg->PointToChar(segTmp.startPos, &grfxTmp, segTmp.rSrc, m_rDst,
			newCaretPos, &m_ichwIp, &m_bAssocPrev);
		int lnBrks = numLnBrB4(m_ichwIp,m_myString);
		if(m_ichwIp>0 && lnBrks>=1 && segTmp.stopPos == m_ichwIp)
		  m_ichwIp-=lnBrks;
	  }

	  if (!bShift)
	  {
		  m_ichwRp = m_ichwIp;
		  m_bRng = false;
	  } else
		  m_bRng = true;

	  if (m_ichwIp!=oldIchwIp) // optimise, updatewindow() only when changes in selection or ip
		bUpdateWnd=true;

	  break;
	}
  default:
	break;
  }

  m_bRng = (m_ichwIp != m_ichwRp);

  if (bUpdateWnd)
  {
	// Anything above which changes the data must update the display, before doing this:
	ClearNextCharProps();
	AdjustAndScrollSelection();
	Invalidate();
	UpdateWindow();
  }

  COleControl::OnKeyDownEvent(nChar, nShiftState);
}

void CSILGraphiteControlCtrl::ClearNextCharProps()
{
	m_chrpNextChar.dympHeight = -1;
	m_chrpNextChar.clrBack = 0xff000000; // invalid color
	m_chrpNextChar.clrFore = 0xff000000;
	m_nextCharFont.Empty();
	m_nextCharSizePercent = -1;
}

int CSILGraphiteControlCtrl::NextArrowKeyPosition(int ichw, bool bForward, GrGraphics & grfx,
	int * piSegISel)
{
	// Eventually this will be useful for visual arrow key movement, but for now
	// we're using logical movement.
	//segTmp.pgrseg->ArrowKeyPosition(segTmp.startPos, &grfxTmp,
	//	&m_ichwIp, &m_bAssocPrev, bRight, false, &bResult);

	LgIpValidResult ipValid;
	DrawnSeg segTmp = m_segList.GetAt(m_segList.FindIndex(*piSegISel));
	bool bOk;
	do {
		ichw = (bForward) ? ichw + 1 : ichw - 1;
		int lbCnt = numLnBrB4(segTmp.stopPos, m_myString);
		while (ichw < segTmp.startPos && *piSegISel > 0)
		{
			(*piSegISel)--;
			segTmp = m_segList.GetAt(m_segList.FindIndex(*piSegISel));
		}
		// We need to test bForward here since otherwise when we move back to the position
		// between cr and lf, this moves us forward again, and we make no progress.
		while (bForward && ichw > segTmp.stopPos - lbCnt && *piSegISel < m_segList.GetCount() - 1)
		{
			(*piSegISel)++;
			segTmp = m_segList.GetAt(m_segList.FindIndex(*piSegISel));
			ichw = max(segTmp.startPos, ichw);
		}
		segTmp.pgrseg->IsValidInsertionPoint(segTmp.startPos, &grfx, ichw,
			&ipValid);
		if (ipValid == kipvrUnknown)
		{
			if (ichw <= 0)
			{
				// Very beginning of the text is always valid.
				ichw = 0;
				ipValid = kipvrOK;
			}
			else if (ichw >= m_myString.GetLength())
			{
				// Very end of the text is always valid.
				ichw = m_myString.GetLength();
				ipValid = kipvrOK;
			}
			else if (ichw == segTmp.startPos || ichw == segTmp.stopPos - lbCnt)
			{
				// Segment boundary is always a valid position.
				ipValid = kipvrOK;
			}
		}
		bOk = (ipValid == kipvrOK);
	}
	while (!bOk);

	return ichw;
}

// TODO: do we need this method anymore?
void CSILGraphiteControlCtrl::OnKeyUp(UINT nChar, UINT nRepCnt, UINT nFlags)
{
  m_keydown = false;
  COleControl::OnKeyUp(nChar, nRepCnt, nFlags);
}

//To set the focus at the first time
BOOL CSILGraphiteControlCtrl::OnEraseBkgnd(CDC* pDC)
{
  //if(m_focusKilled == false)
  SetFocus();
  return TRUE;
}

UINT CSILGraphiteControlCtrl::OnGetDlgCode()
{
  return DLGC_WANTALLKEYS;
}

void CSILGraphiteControlCtrl::OnDestroy()
{
  COleControl::OnDestroy();

  SegListRemoveAll(m_segList);
  m_undoStack.clear();
  m_redoStack.clear();
}

void CSILGraphiteControlCtrl::OnTimer(UINT nIDEvent)
{
  if(!m_bRng && !m_focusKilled)
  {
	if(!m_drawIt)
	  m_drawIt = true;
	else
	  m_drawIt = false;
	Invalidate(FALSE);
  }

  COleControl::OnTimer(nIDEvent);
}

void CSILGraphiteControlCtrl::BeepIt(VARIANT_BOOL beepy)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_beep = beepy;
}

void CSILGraphiteControlCtrl::OnChooseFont()
{
  int base  = min(m_ichwIp,m_ichwRp); //start point of selection.
  int limit = max(m_ichwIp,m_ichwRp);//end point of selection

  int baseRngI  = min(m_iRngI,m_rRngI); //index of range contains base (base-range) in m_rangeList
  int limitRngI = max(m_iRngI,m_rRngI); //index of range contains limit (limit-range)in m_rangeList

  int baseRngStart = min(m_iRngStartPos,m_rRngStartPos);  // startPos of base-range
  int limitRngStart  = max(m_iRngStartPos,m_rRngStartPos); // startPos of limit-range

  //Range rngBase = m_rangeList.GetAt(m_rangeList.FindIndex(baseRngI));

  Range rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(baseRngI));
  CString fontNameOld = rngTmp.lf.lfFaceName;;
  int fontSizeOld = rngTmp.lgchrp.dympHeight;
  int sizePercentOld = rngTmp.sizePercent;
  for (int rI = baseRngI + 1; rI <= limitRngI; rI++)
  {
	  rngTmp = m_rangeList.GetAt(m_rangeList.FindIndex(rI));
	  if (wcscmp(rngTmp.lf.lfFaceName, fontNameOld.GetBuffer()) != 0)
		  fontNameOld.Empty();

	  if (fontSizeOld != rngTmp.lgchrp.dympHeight || sizePercentOld != rngTmp.sizePercent)
	  {
		  fontSizeOld = -1;
		  sizePercentOld = INT_MAX;
	  }
  }

  /*
  CHARFORMAT cfmt;
  memset(&cfmt, 0, isizeof(cfmt));
  cfmt.cbSize = isizeof(cfmt);
  DWORD flags = 0;
  if (true) // ...there is exactly one font
  {
	  //wcscpy(cf.lpLogFont->lfFaceName, rngBase.lf.lfFaceName);
	  cfmt.yHeight = fontSizeOld / 50; // twips
	  wcscpy(cfmt.szFaceName, fontNameOld);
	  cfmt.bCharSet = DEFAULT_CHARSET;
	  cfmt.dwMask |= CFM_CHARSET;
	  cfmt.dwMask |= CFM_FACE;
	  cfmt.dwMask |= CFM_SIZE;
	  //flags |= CF_INITTOLOGFONTSTRUCT;
  }
  */

  LOGFONT lfTmp;
  memset(&lfTmp, 0, isizeof(lfTmp));
  if (true)
  {
	  wcscpy(lfTmp.lfFaceName, fontNameOld);
	  if (fontSizeOld == -1 || sizePercentOld == INT_MAX)
		  // size invalid
		  lfTmp.lfHeight = 0;
	  else
		lfTmp.lfHeight = -MulDiv(fontSizeOld, 96, 72000);
	  lfTmp.lfCharSet = DEFAULT_CHARSET;
  }

  CFontDialog * fontDlg;
  fontDlg = new CFontDialog(&lfTmp, CF_SCREENFONTS, NULL, this); // use CF_EFFECTS to include color/underline/strikethrough
  //fontDlg = new CFontDialog(cfmt, CF_SCREENFONTS, NULL, this);
  //fontDlg->m_cf.Flags |= CF_NOSTYLESEL; // no, allow regular so the sample will show up

  if (fontDlg->DoModal() == IDOK)
  {
	int newSize = fontDlg->GetSize() / 10;
	CString newFont(fontDlg->GetFaceName());
	int newSizePercent = -1;
	if (sizePercentOld != -1 && sizePercentOld != INT_MAX)
		newSizePercent = sizePercentOld;

	bool fBold = fontDlg->IsBold();
	bool fItalic = fontDlg->IsItalic();
	if (fBold || fItalic)
		MessageBox(_T("Bold and italic styles are not yet supported."), _T("Font"));

	if (!m_bRng)
	{
		// Insertion point. Record the desired font and size for later.
		ClearNextCharProps();
		if (newSize > 0)
			m_chrpNextChar.dympHeight = newSize * 1000;
		m_nextCharSizePercent = newSizePercent;
		m_nextCharFont = newFont;
	}
	else
	{
		// Range selection.
		LgCharRenderProps chrp;
		chrp.dympHeight = newSize * 1000;
		chrp.clrBack = 0xff000000; // don't change
		chrp.clrFore = 0xff000000; // don't change
		SetRangeProperties(base, limit, baseRngI, limitRngI, baseRngStart, limitRngStart,
			chrp, newFont, newSizePercent);

		CleanUpRanges();
		m_iFirstDirtySeg = min(m_iSegI, m_rSegI);
		m_iLastDirtySeg = max(m_iSegI, m_rSegI);
		m_cchDirtyDiff = 0;
		Invalidate();
		UpdateWindow();
	}
  }
  delete fontDlg;
}
#ifdef _DEBUG
void CSILGraphiteControlCtrl::OnInitSILDoulosPiglatinDemo()
{
  MessageBox(L"Init SIL Doulos Piglatin Demo",L"",MB_OK);
  SetSelectionFont(L"SILDoulos PiglatinDemo");
  Invalidate();
  UpdateWindow();
}
void CSILGraphiteControlCtrl::OnInitSimpleGraphiteFont()
{
  MessageBox(L"Init Simple Graphite Font",L"",MB_OK);
  SetSelectionFont(L"SimpleGraphiteFont");
  Invalidate();
  UpdateWindow();
}
void CSILGraphiteControlCtrl::OnInitSILDoulosUnicodeIPA()
{
  MessageBox(L"Init SIL Doulos Unicode IPA",L"",MB_OK);
  SetSelectionFont(L"SILDoulosUnicodeIPA");
  Invalidate();
  UpdateWindow();
}
void CSILGraphiteControlCtrl::OnInitArialUnicodeMS()
{
  MessageBox(L"Init Arial Unicode MS",L"",MB_OK);
  SetSelectionFont(L"Arial Unicode MS");
  Invalidate();
  UpdateWindow();
}
#endif // _DEBUG
void CSILGraphiteControlCtrl::OnFont50()
{
  OnRelativeFontSize(50);
}
void CSILGraphiteControlCtrl::OnFont75()
{
  OnRelativeFontSize(75);
}
void CSILGraphiteControlCtrl::OnFont100()
{
  OnRelativeFontSize(100);
}
void CSILGraphiteControlCtrl::OnFont150()
{
  OnRelativeFontSize(150);
}
void CSILGraphiteControlCtrl::OnFont200()
{
  OnRelativeFontSize(200);
}

void CSILGraphiteControlCtrl::OnRelativeFontSize(int percent)
{
  m_undoStack.push(m_myText,m_ichwIp,m_ichwRp, m_rangeList);

  int posInRange = m_ichwIp-m_iRngStartPos; // Pos of m_ichwIp relative to startPos of range.

  if (!m_bRng)
  {
	  // Insertion point. Record the desired size for later.
	  ClearNextCharProps();
	  m_chrpNextChar.dympHeight = m_currFontSize * percent * 10;
	  m_nextCharSizePercent = percent;
  } else { //have selection
	int base  = min(m_ichwIp,m_ichwRp); //start point of selection.
	int limit = max(m_ichwIp,m_ichwRp);//end point of selection

	int baseRngI  = min(m_iRngI,m_rRngI); //index of range contains base (base-range) in m_rangeList
	int limitRngI = max(m_iRngI,m_rRngI); //index of range contains limit (limit-range)in m_rangeList

	int baseRngStart = min(m_iRngStartPos,m_rRngStartPos); //startPos of base-range
	int limitRngStart  = max(m_iRngStartPos,m_rRngStartPos); //startPos of limit-range

	LgCharRenderProps chrp;
	chrp.dympHeight = m_currFontSize * percent * 10;
	chrp.clrBack = 0xff000000; // don't change
	chrp.clrFore = 0xff000000; // don't change
	CString newFont;
	newFont.Empty();
	SetRangeProperties(base, limit, baseRngI, limitRngI, baseRngStart, limitRngStart,
		chrp, newFont, percent);

	CleanUpRanges();
	m_iFirstDirtySeg = min(m_iSegI, m_rSegI);
	m_iLastDirtySeg = max(m_iSegI, m_rSegI);
	m_cchDirtyDiff = 0;
	Invalidate();
	UpdateWindow();
  }
}

void CSILGraphiteControlCtrl::OnCut()
{
  Cut();
}

void CSILGraphiteControlCtrl::OnCopy()
{
  Copy();
}

void CSILGraphiteControlCtrl::OnPaste()
{
  Paste();
}

void CSILGraphiteControlCtrl::OnAcceleratorSave()
{
  // override to save a file
  CString szFilters = L"Text Files (*.txt)|*.txt|All Files (*.*)|*.*|";
  CFileDialog fileDlg (FALSE, L"txt", NULL,
	OFN_OVERWRITEPROMPT| OFN_HIDEREADONLY, szFilters, this);

  // Display the file dialog. When user clicks OK, fileDlg.DoModal()
  // returns IDOK.
  if( fileDlg.DoModal ()==IDOK )
  {
	CString pathName = fileDlg.GetPathName();
	CStdioFile txtFile(pathName, CFile::modeCreate|
	  CFile::modeWrite|CFile::typeText);
	CString myTempString(m_myText);
	txtFile.Write(m_myText,(myTempString.GetLength() * sizeof(TCHAR)));
	txtFile.Close();
	//txtFile.WriteString(myTempString);
	MessageBox(myTempString,L"Data Saved",MB_OK);
	m_currFileName = pathName;

  }
}

void CSILGraphiteControlCtrl::OnAcceleratorOpen()
{
  CString szFilters = L"Text Files (*.txt)|*.txt|All Files (*.*)|*.*||";
  CFileDialog fileDlg (TRUE, L"txt", NULL,
	OFN_OVERWRITEPROMPT, szFilters, this); // TRUE means Open File

  // Display the file dialog. When user clicks OK, fileDlg.DoModal()
  // returns IDOK.
  if( fileDlg.DoModal() == IDOK )
  {
	m_undoStack.clear();
	m_redoStack.clear();
	m_rangeList.RemoveAll();
	SegListRemoveAll(m_segList);
	m_myString.Empty();

	CString pathName = fileDlg.GetPathName();
	CStdioFile txtFile(pathName,CFile::modeRead|CFile::typeBinary); // typeText????

	CString myTempString("Dummy String");

	while(!myTempString.IsEmpty())
	{
	  myTempString.Empty();
	  txtFile.ReadString(myTempString);
	  m_myString.Append(W2BSTR(myTempString));
	}

	txtFile.Close();
	m_currFileName = pathName;

	m_myText.Empty();
	m_myText = ::W2BSTR(m_myString);

	Range rngNew;
	InitNewRange(rngNew);
	rngNew.numChar = m_myString.GetLength();
	m_rangeList.AddHead(rngNew);

  }

  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;
  Invalidate();
  UpdateWindow();
}

void CSILGraphiteControlCtrl::OnSaveAsHtml()
{
  // override to save a file
  CString szFilters = L"HTML Files (*.html)|*.html|HTM Files (*.htm)|*.htm|All Files (*.*)|*.*|";
  CFileDialog fileDlg(FALSE, L"html", NULL,
	OFN_OVERWRITEPROMPT | OFN_HIDEREADONLY, szFilters, this);

  // Display the file dialog. When user clicks OK, fileDlg.DoModal()
  // returns IDOK.
  if( fileDlg.DoModal() == IDOK )
  {
	CString pathName = fileDlg.GetPathName();
	CStdioFile txtFile(pathName, CFile::modeCreate | CFile::modeWrite | CFile::typeText);
	BSTR htmlBSTR = m_myText;
	//GetHtmlText(&htmlBSTR);
	//CString HTMLString(htmlBSTR);
	//txtFile.Write(htmlBSTR,(HTMLString.GetLength()*sizeof(TCHAR)));/*(myTempString.GetLength() * sizeof(TCHAR)));*/
	//txtFile.Close();

	char * pch = NULL;
	int cch = GenerateHtml(&pch);
	txtFile.Write(pch, cch);
	txtFile.Close();

#ifdef _DEBUG
	//MessageBox(HTMLString,L"Data Saved",MB_OK);
#endif

	delete[] pch; // buffer created by GenerateHtml
  }
}

void CSILGraphiteControlCtrl::OnOpenHtml()
{
  // override to save a file
  CString szFilters = L"HTML Files (*.html)|*.html|HTM Files (*.htm)|*.htm|All Files (*.*)|*.*|";
  CFileDialog fileDlg (TRUE, L"html", NULL,
	OFN_OVERWRITEPROMPT, szFilters, this);

  // Display the file dialog. When user clicks OK, fileDlg.DoModal()
  // returns IDOK.
  if( fileDlg.DoModal() == IDOK )
  {
	m_undoStack.clear();
	m_redoStack.clear();
	m_rangeList.RemoveAll();
	m_myString.Empty();

	CString pathName = fileDlg.GetPathName();
	CStdioFile txtFile(pathName, CFile::modeRead | CFile::typeBinary); // typeText??

	FileOrBuffer fb;
	fb.SetFile(&txtFile);
	ParseHtml(fb);

	txtFile.Close();

	m_currFileName = pathName;

	m_myText.Empty();
	m_myText = ::W2BSTR(m_myString);

	if (m_rangeList.IsEmpty())
	{
		Range rngNew;
		InitNewRange(rngNew);
		rngNew.numChar = m_myString.GetLength();
		m_rangeList.AddHead(rngNew);
	}
  }
  else
  {
	  CString strTmp = L"<?xml version=\"1.0\" encoding=\"UTF-8\"?>  <html><body><p><span>hello world</span></p></body></html>";
	  CComBSTR bstrTmp = ::W2BSTR(strTmp);
	  PutHtmlText(bstrTmp);
	  return;
  }


  CleanUpRanges();

  m_iFirstDirtySeg = 0;
  m_iLastDirtySeg = -1;
  m_cchDirtyDiff = 0;
  Invalidate();
  UpdateWindow();
}


#ifdef _DEBUG
void CSILGraphiteControlCtrl::OnMultiline()
{
  m_multiline == false? SetMultiline(true):SetMultiline(false);
  return;
}

//Call SetMemoryState, then GetMemoryState will report memory leaks between 2 calls.
void  CSILGraphiteControlCtrl::SetMemoryState()
{
  afxMemDF = allocMemDF | checkAlwaysMemDF;
  AfxEnableMemoryTracking(TRUE);
  msOld.Checkpoint();
}

void  CSILGraphiteControlCtrl::GetMemoryState()
{
  msNew.Checkpoint();
  if(diffMemState.Difference(msOld,msNew))
  {
	TRACE("Memory difference between 2 check points \n");
	diffMemState.DumpStatistics();
	diffMemState.DumpAllObjectsSince();
  }
}
#endif // _DEBUG


VARIANT_BOOL CSILGraphiteControlCtrl::GetAllowFormatDlg(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  return m_bAllowFormatDlg;
}

void CSILGraphiteControlCtrl::SetAllowFormatDlg(VARIANT_BOOL newVal)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());
  m_bAllowFormatDlg = newVal;
  SetModifiedFlag();
}

BSTR CSILGraphiteControlCtrl::GetFontName(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  CComBSTR strResult;
  strResult.Empty();
  GetSelectionFont(&strResult);

  return strResult;
}

BSTR CSILGraphiteControlCtrl::GetHtml(void)
{
  AFX_MANAGE_STATE(AfxGetStaticModuleState());

  CComBSTR strResult;
  strResult.Empty();
  GetHtmlText(&strResult);

  return strResult;
}

// Find the Graphite engine initialized for the given font. If none, create it
// and add it to the list.
void CSILGraphiteControlCtrl::FindEngine(CString & fontName, GrGraphics & grfx, GrEngine ** ppgreng)
{
	NamedEngine neng;
	for (int i = 0; i < m_engineList.GetCount(); i++)
	{
		neng = m_engineList.GetAt(m_engineList.FindIndex(i));
		if (neng.engineName == fontName)
		{
			*ppgreng = neng.pgreng;
			return;
		}
	}
	// Not there; add it.
	GrEngine * pgrengNew = new GrEngine;
	pgrengNew->InitRenderer(&grfx, NULL, 0);
	neng.engineName = fontName;
	neng.pgreng = pgrengNew;
	m_engineList.AddHead(neng);
	*ppgreng = pgrengNew;
}

// NOTE: Caller is responsible for deleting the buffer that is allocated by this method.
int CSILGraphiteControlCtrl::GenerateHtml(char ** ppchsBuffer)
{
	int cchEstimate = m_rangeList.GetCount() * 200 + m_myString.GetLength() * 4;
	char * prgchsBuffer = new char[cchEstimate];
	char * pch = prgchsBuffer;
	char * pchLim = prgchsBuffer + cchEstimate;

	m_myText.Empty();
	m_myText = ::W2BSTR(m_myString);

	int cchTmp;
	char rgchTmp[20];

	bool bErr = false;

	OutputChars("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n", &pch, pchLim, &bErr);
	OutputChars("<html>\n", &pch, pchLim, &bErr);
	OutputChars("  <body>\n", &pch, pchLim, &bErr);
	OutputChars("    <p>\n", &pch, pchLim, &bErr);

	int ich = 0;
	for (int rI = 0; rI < m_rangeList.GetCount(); rI++)
	{
		Range rng = m_rangeList.GetAt(m_rangeList.FindIndex(rI));

		if (rng.firstInPara && rI > 0)
		{
			// Start a new paragraph
			OutputChars("    </p>\n    <p>\n", &pch, pchLim, &bErr);
		}
		// Output the style information for the range.

		OutputChars("      <span", &pch, pchLim, &bErr);

		if (wcslen(rng.lf.lfFaceName) > 0
			|| rng.lgchrp.dympHeight != m_defaultFontSize * 1000
			|| rng.sizePercent != -1)
		{
			OutputChars(" style=\"", &pch, pchLim, &bErr);
			int cprop = 0;
			if (wcslen(rng.lf.lfFaceName) > 0)
			{
				OutputChars("font-family: ", &pch, pchLim, &bErr);
				cchTmp = Utf16ToUtf8(rng.lf.lfFaceName, wcslen(rng.lf.lfFaceName),
					pch, cchEstimate - (pch - prgchsBuffer), &bErr);
				pch += cchTmp;
				cprop++;
			}
			if (rng.sizePercent != -1)
			{
				if (cprop > 0)
					OutputChars("; ", 2, &pch, pchLim, &bErr);
				OutputChars("font-size: ", &pch, pchLim, &bErr);
				itoa(rng.sizePercent, rgchTmp, 10);
				OutputChars(rgchTmp, &pch, pchLim, &bErr);
				OutputChars("%", 1, &pch, pchLim, &bErr);
				cprop++;
			}
			else if (rng.lgchrp.dympHeight != m_defaultFontSize * 1000)
			{
				if (cprop > 0)
					OutputChars("; ", 2, &pch, pchLim, &bErr);
				OutputChars("font-size: ", &pch, pchLim, &bErr);
				itoa(rng.lgchrp.dympHeight / 1000, rgchTmp, 10);
				OutputChars(rgchTmp, &pch, pchLim, &bErr);
				OutputChars("pt", 2, &pch, pchLim, &bErr);
				cprop++;
			}

			OutputChars("\"", &pch, pchLim, &bErr);
		}
		OutputChars(">", &pch, pchLim, &bErr); // closure of span tag

		// Output the text itself
		cchTmp = Utf16ToUtf8(m_myText + ich, rng.numChar,
			pch, cchEstimate - (pch - prgchsBuffer), &bErr);
		pch += cchTmp;
		ich += rng.numChar;

		OutputChars("</span>\n", &pch, pchLim, &bErr);

		if (bErr)
			break; // stop trying!
	}

	OutputChars("    </p>\n",  &pch, pchLim, &bErr);
	OutputChars("  </body>\n", &pch, pchLim, &bErr);
	OutputChars("</html>\n", &pch, pchLim, &bErr);

	// Caller is responsible to delete the buffer.

	*ppchsBuffer = prgchsBuffer;
	if (bErr)
		return 0;
	else
		return (pch - prgchsBuffer);
}

// Convert a UTF-16 string into UTF-8 suitable for an XML file
int CSILGraphiteControlCtrl::Utf16ToUtf8(wchar * prgchwIn, int cchInMax,
	char * prgchOut, int cchOutMax, bool * pbErr)
{
	int cchThis;
	char rgchTmp[8];
	char * prgchTmp;
	const wchar * prgchwInLim = prgchwIn + cchInMax;
	char * prgchOutLim = prgchOut + cchOutMax;

	byte bFirstMark = 0;

	int cchOut = 0;

	wchar * pchwLp;
	for (pchwLp = prgchwIn; pchwLp < prgchwInLim; )
	{
		ulong luChar = *pchwLp++;
		if (kSurrogateHighFirst <= luChar && luChar <= kSurrogateHighLast && prgchwIn < prgchwInLim)
		{
			ulong luChar2 = *prgchwIn;
			if (kSurrogateLowFirst <= luChar2 && luChar2 <= kSurrogateLowLast)
			{
				luChar -= kSurrogateHighFirst;
				luChar <<= kSurrogateShift;
				luChar += luChar2 - kSurrogateLowFirst;
				luChar += kSurrogateBase;
				++pchwLp;
			}
		}
		if (luChar > kUnicodeMax)
			luChar = kReplacementChar;

		prgchTmp = NULL;

		if (luChar < kUtf8Min2)
		{
			// XML equivalents for special characters.
			switch (luChar)
			{
			case '<':
				prgchTmp = "&lt;";
				cchThis = 4;
				break;
			case '>':
				prgchTmp = "&gt;";
				cchThis = 4;
				break;
			case '&':
				prgchTmp = "&amp;";
				cchThis = 5;
				break;
			default:
				bFirstMark = kUtf8Flag1;
				cchThis = 1;
				break;
			}
		}
		else if (luChar < kUtf8Min3)
		{
			bFirstMark = kUtf8Flag2;
			cchThis = 2;
		}
		else if (luChar < kUtf8Min4)
		{
			bFirstMark = kUtf8Flag3;
			cchThis = 3;
		}
		else if (luChar < kUtf8Min5)
		{
			bFirstMark = kUtf8Flag4;
			cchThis = 4;
		}
		else if (luChar < kUtf8Min6)
		{
			bFirstMark = kUtf8Flag5;
			cchThis = 5;
		}
		else
		{
			bFirstMark = kUtf8Flag6;
			cchThis = 6;
		}

		if (!prgchTmp)
		{
			prgchTmp = rgchTmp;

			char * pch = &rgchTmp[cchThis];

			switch (cchThis)
			{
			case 6:
				*--pch = (char) ((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 5:
				*--pch = (char) ((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 4:
				*--pch = (char) ((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 3:
				*--pch = (char) ((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 2:
				*--pch = (char) ((luChar & kByteMask) | kByteMark);
				luChar >>= kByteShift;
				// fall through
			case 1:
				*--pch = (char)(luChar | bFirstMark);
				break;
			default:
				// can't happen
				break;
			}
		}

		char * prgchThis = prgchOut + cchOut;
		OutputChars(prgchTmp, cchThis, &prgchThis, prgchOutLim, pbErr);

		cchOut += cchThis;

		if (*pbErr)
			return 0;
	}

	if (*pbErr)
		return 0;
	else
		return cchOut;
}

void CSILGraphiteControlCtrl::OutputChars(char * prgchToPut, int cch, char ** pprgchOut, char * pchLim,
	bool * pbErr)
{
	if (*pbErr == true)
		return; // already had an error; don't try again

	if (*pprgchOut + cch >= pchLim)
	{
		MessageBox(_T("Error in writing file--buffer overflow"), _T("Save As HTML"));
		*pbErr = true;
		return;
	}

	memcpy(*pprgchOut, prgchToPut, (isizeof(char) * cch));
	*pprgchOut += cch;
}

void CSILGraphiteControlCtrl::OutputChars(char * prgchToPut, char ** pprgchOut, char * pchLim,
	bool * pbErr)
{
	int cch = strlen(prgchToPut);
	OutputChars(prgchToPut, cch, pprgchOut, pchLim, pbErr);
}

// Decode 1-6 bytes in the character string from UTF-8 format to Unicode (UCS-4).
// As a side-effect, cbOut is set to the number of UTF-8 bytes consumed.

// rgchUtf8 - Pointer to a a character array containing UTF-8 data.
// cchUtf8 - Number of characters in the array.
// cbOut - Reference to an integer for holding the number of input (8-bit) characters
//				consumed to produce the single output Unicode character.

// return a single Unicode (UCS-4) character.  If an error occurs, return -1.
long CSILGraphiteControlCtrl::DecodeUtf8(const char * rgchUtf8, int cchUtf8, int & cbOut)
{
	// check for valid input
	AssertArray(rgchUtf8, cchUtf8);
	if ((cchUtf8 == 0) || (rgchUtf8[0] == '\0'))
	{
		cbOut = (cchUtf8) ? 1 : 0;
		return 0;
	}
	//
	// decode the first byte of the UTF-8 sequence
	//
	long lnUnicode;
	int cbExtra;
	int chsUtf8 = *rgchUtf8++ & 0xFF;
	if (chsUtf8 >= kUtf8Flag6)				// 0xFC
	{
		lnUnicode = chsUtf8 & kUtf8Mask6;
		cbExtra = 5;
	}
	else if (chsUtf8 >= kUtf8Flag5)			// 0xF8
	{
		lnUnicode = chsUtf8 & kUtf8Mask5;
		cbExtra = 4;
	}
	else if (chsUtf8 >= kUtf8Flag4)			// 0xF0
	{
		lnUnicode = chsUtf8 & kUtf8Mask4;
		cbExtra = 3;
	}
	else if (chsUtf8 >= kUtf8Flag3)			// 0xE0
	{
		lnUnicode = chsUtf8 & kUtf8Mask3;
		cbExtra = 2;
	}
	else if (chsUtf8 >= kUtf8Flag2)			// 0xC0
	{
		lnUnicode = chsUtf8 & kUtf8Mask2;
		cbExtra = 1;
	}
	else									// 0x00
	{
		lnUnicode = chsUtf8;
		cbExtra = 0;
	}
	if (cbExtra >= cchUtf8)
	{
		return -1;
	}

	switch (cbExtra)
	{
	case 5:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		// fall through
	case 4:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		// fall through
	case 3:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		// fall through
	case 2:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		// fall through
	case 1:
		lnUnicode <<= kByteShift;
		chsUtf8 = *rgchUtf8++ & 0xFF;
		if ((chsUtf8 & ~kByteMask) != 0x80)
			return -1;
		lnUnicode += chsUtf8 & kByteMask;
		break;
	case 0:
		// already handled
		break;
	default:
		Assert(false);
	}
	if ((ulong)lnUnicode > kUnicodeMax)
	{
		return -1;
	}
	cbOut = cbExtra + 1;
	return lnUnicode;
}

// Convert a range of UTF-8 characters to a range of UTF-16 characters.  The output buffer is
// not NUL terminated.
//
// rgchwDst - Pointer to an output array of wide (16-bit) characters.
// cchwDst - Maximum number of wide (16-bit) characters that can be stored in rgchwDst.
//				Due to surrogate pairs, this may be greater than the number of actual
//				Unicode characters that can be stored.
// rgchSrc - Pointer to an input array of (8-bit) characters.
// cchSrc - Number of characters in rgchSrc.
//
// return the number of 16-bit characters embedded in the UTF-8 string.  Due to surrogate
//				pairs, this may be greater than the number of actual Unicode characters.
int CSILGraphiteControlCtrl::Utf8ToUtf16(const char * rgchSrc, int cchSrc, wchar * rgchwDst, int cchwDst)
{
	AssertArray(rgchwDst, cchwDst);
	if (!cchwDst || !rgchSrc || !cchSrc)
		return 0;

	int cbUtf8;
	int cchw = 0;
	for (int ich = 0; ich < cchSrc; ich += cbUtf8)
	{
		long lnUnicode = DecodeUtf8(rgchSrc + ich, cchSrc - ich, cbUtf8);
		if (lnUnicode == -1)
		{
			return 0;
		}
		else if (lnUnicode > kUtf16Max)
		{
			// Valid UCS-4, but invalid UTF-16.
			lnUnicode = kReplacementChar;
		}
		else if (lnUnicode > kUcs2Max)
		{
			// Invalid UCS-2, but valid UTF-16:  convert to surrogate pairs.
			lnUnicode -= kSurrogateBase;
			if (cchw < cchwDst)
				rgchwDst[cchw++] = (wchar)((lnUnicode >> kSurrogateShift) +
											   kSurrogateHighFirst);
			lnUnicode = (lnUnicode & kSurrogateMask) + kSurrogateLowFirst;
		}
		if (cchw < cchwDst)
			rgchwDst[cchw++] = (wchar)lnUnicode;
	}
	return cchw;
}

void CSILGraphiteControlCtrl::ParseHtml(FileOrBuffer & fb)
{
//	chnext = 0;
	m_bOpenHtmlFatal = false;
	m_bOpenHtmlNonFatal = false;

	if (!MatchString(fb, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"))
		goto LFatalError;

	SkipWs(fb);

	ParseHtmlTag(fb);
	if (m_bOpenHtmlFatal)
		goto LFatalError;

	if (m_bOpenHtmlNonFatal)
		MessageBox(_T("Warning: the file was not completely loaded."), _T("Open HTML"));

	return;

LFatalError:
	MessageBox(_T("Fatal error in reading file."), _T("Open HTML"));
	return;
}

void CSILGraphiteControlCtrl::ParseHtmlTag(FileOrBuffer & fb)
{
	if (!MatchOpenTag(fb, "html"))
	{
		m_bOpenHtmlFatal = true;
		return;
	}

	SkipWs(fb);

	ParseBodyTag(fb);
	if (m_bOpenHtmlFatal)
		 return;

	SkipWs(fb);

	if (!MatchCloseTag(fb, "html"))
	{
		m_bOpenHtmlFatal = true;
		return;
	}
}

void CSILGraphiteControlCtrl::ParseBodyTag(FileOrBuffer & fb)
{
	if (!MatchOpenTag(fb, "body"))
	{
		m_bOpenHtmlFatal = true;
		return;
	}

	SkipWs(fb);

	ParsePTags(fb, "body"); // consume </body>
	if (m_bOpenHtmlFatal)
		 return;

	SkipWs(fb);
}

void CSILGraphiteControlCtrl::ParsePTags(FileOrBuffer & fb, char * tagTerm)
{
	bool bFirstPara = true;

	while (MatchOpenTag(fb, "p"))
	{
		if (!bFirstPara)
		{
			// Append CRLF to the end of the previous range.
			Range rngLast = m_rangeList.GetTail();
			rngLast.numChar += 2;
			m_rangeList.RemoveTail();
			m_rangeList.AddTail(rngLast);
			m_myString.Append(L"\x000D\x000A");
		}

		SkipWs(fb);

		ParseSpanTags(fb, "p"); // consume </p>
		if (m_bOpenHtmlFatal)
			 return;

		SkipWs(fb);

		bFirstPara = false;
	}

	// If we failed to find another <p> tag, then we should have the closing </body> tag.
	// We would have choked on the / character.
	if (!MatchChar(fb, '/'))
	{
		m_bOpenHtmlFatal = true;
		return;
	}
	if (!MatchString(fb, tagTerm))
	{
		m_bOpenHtmlFatal = true;
		return;
	}
	if (!MatchChar(fb, '>'))
	{
		m_bOpenHtmlFatal = true;
		return;
	}
}

void CSILGraphiteControlCtrl::ParseSpanTags(FileOrBuffer & fb, char * tagTerm)
{
	bool bFirstInPara = true;
	bool bClose = false;
	//char attrs[1000];

	CList<CString, CString> attrNames;
	CList<CString, CString> attrValues;

	while (MatchOpenTag(fb, "span", &attrNames, &attrValues, &bClose))
	{
		Range rngNew;
		InitNewRange(rngNew);

		ApplyAttributes(attrNames, attrValues, rngNew);

		if (!bClose)
		{
			int cbUtf8Len = 124;
			char * utf8 = new char[cbUtf8Len];
			char * pch = utf8;

			int chNext = fb.Peek();
			while (chNext != '<')
			{
				if (pch - utf8 >= cbUtf8Len)
				{	// lengthen the buffer
					int cchSoFar = pch - utf8;
					char * utf8Old = utf8;
					cbUtf8Len *= 2;
					utf8 = new char[cbUtf8Len];
					memcpy(utf8, utf8Old, cchSoFar * isizeof(char));
					delete[] utf8Old;
					pch = utf8 + cchSoFar;
				}
				*pch++ = chNext;
				fb.NextChar();
				chNext = fb.Peek();
			}

			int cch = pch - utf8;
			// UTF16 should never have more characters than UTF8.
			wchar * utf16 = new wchar[cch + 1];
			int cchUtf16 = Utf8ToUtf16(utf8, cch, utf16, cch);
			utf16[cchUtf16] = 0;

			m_myString.Append(utf16);
			rngNew.numChar = cch;

			delete[] utf8;
			delete[] utf16;
		}
		else
			rngNew.numChar = 0;

		rngNew.firstInPara = bFirstInPara;

		m_rangeList.AddTail(rngNew);

		if (!bClose)
		{
			if (!MatchCloseTag(fb, "span"))
			{
				m_bOpenHtmlFatal = true;
				return;
			}

			SkipWs(fb);
		}

		bFirstInPara = false;
	}

	// If we failed to find another <span> tag, then we should have the closing </p> tag.
	// We would have choked on the / character.
	if (!MatchChar(fb, '/'))
	{
		m_bOpenHtmlFatal = true;
		return;
	}
	if (!MatchString(fb, tagTerm))
	{
		m_bOpenHtmlFatal = true;
		return;
	}
	if (!MatchChar(fb, '>'))
	{
		m_bOpenHtmlFatal = true;
		return;
	}
}


// Return true if the character matches the next one in the input. Only consume a character if it
// matches.
bool CSILGraphiteControlCtrl::MatchChar(FileOrBuffer & fb, char ch)
{
	if (fb.Peek() == ch)
	{
		fb.NextChar();
		return true;
	}
	return false;
}

bool CSILGraphiteControlCtrl::MatchString(FileOrBuffer & fb, char * str)
{
	char *pchStr = str;
	while (*pchStr)
	{
		if (fb.Peek() != *pchStr)
			return false;
		pchStr++;
		fb.NextChar();
	}
	return true;
}

// Read a tag and return true if it matches the given string. The tag must be zero-terminated.
bool CSILGraphiteControlCtrl::MatchOpenTag(FileOrBuffer & fb , char * tag)
{
	bool bClose;
	bool bRet = MatchOpenTag(fb, tag, NULL, NULL, &bClose);
	return (bRet && !bClose);
}

bool CSILGraphiteControlCtrl::MatchOpenTag(FileOrBuffer & fb, char * tag,
	  CList<CString,CString> * pattrNames, CList<CString, CString> * pattrValues,
	  bool * pbClose)
{
	*pbClose = false;

	if (fb.Peek() != '<')
		return false;
	fb.NextChar();

	if (fb.Peek() == '/')
		return false; // close tag

	bool bRet = true;
	char * pchTag = tag;
	while (true)
	{
		if (fb.Peek() == ' ')
		{
			// Read attributes.
			CString nextAttr;
			CString nextValue;
			SkipWs(fb);
			char nextAttrChars[128];
			while (fb.Peek() != '>' && fb.Peek() != '/')
			{
				GetNextAttrName(fb, nextAttrChars, 128);
				SkipWs(fb);
				if (!MatchChar(fb, '='))
				{
					m_bOpenHtmlFatal = true;
					return false;
				}
				nextAttr = nextAttrChars; // dumb UTF-16 generation? good enough for now
				SkipWs(fb);
				GetQuotedString(fb, &nextValue);
				if (nextValue.GetLength() == 0)
					m_bOpenHtmlNonFatal = true;

				pattrNames->AddTail(nextAttr);
				pattrValues->AddTail(nextValue);

				SkipWs(fb); // gets next char into chnext
			}
		}
		if (fb.Peek() == '/')
		{
			*pbClose = true;
			fb.NextChar();
		}
		if (fb.Peek() == '>')
		{
			fb.NextChar();
			return (bRet && *pchTag == 0);
		}
		if (fb.Peek() != *pchTag)
			bRet = false;
		fb.NextChar();
		pchTag++;
	}

	return false;
}

bool CSILGraphiteControlCtrl::MatchCloseTag(FileOrBuffer & fb, char * tag)
{
	if (fb.Peek() != '<')
		return false;
	fb.NextChar();

	if (fb.Peek() != '/')
		return false;
	fb.NextChar();

	bool bRet = true;
	char * pchTag = tag;
	while (true)
	{
		if (fb.Peek() == '>')
		{
			fb.NextChar();
			return (bRet && *pchTag == 0);
		}
		if (fb.Peek() != *pchTag)
			bRet = false;
		pchTag++;
		fb.NextChar();
	}
	return false;
}

void CSILGraphiteControlCtrl::SkipWs(FileOrBuffer & fb)
{
	int chNext = fb.Peek();
	while (chNext == 0x0A || chNext == 0x0D || chNext == 32)
	{
		fb.NextChar();
		chNext = fb.Peek();
	}
}


bool CSILGraphiteControlCtrl::MatchEOL(FileOrBuffer & fb)
{
	int chNext = fb.Peek();
	if (chNext == 0x0D)
	{
		fb.NextChar();
		return true;
	}
	else if (chNext == 0x0A)
	{
		fb.NextChar();
		return true;
	}

	return false;
}

// TODO: implement the official XHTML syntax for attribute names.
void CSILGraphiteControlCtrl::GetNextAttrName(FileOrBuffer & fb, char * pchAttr, int cchMax)
{
	SkipWs(fb);

	char *pch = pchAttr;

	int chNext = fb.Peek();
	while ((chNext >= 'a' && chNext <= 'z')
		|| (chNext >= 'A' && chNext <= 'Z')
		|| chNext == '_' || chNext == '-'
		|| (pch - pchAttr > 0 && chNext >= '0' && chNext <= '9'))
	{
		if (pch - pchAttr < cchMax)
			*pch++ = chNext;
		fb.NextChar();
		chNext = fb.Peek();
	}
	*pch = 0; // terminate
}

void CSILGraphiteControlCtrl::GetQuotedString(FileOrBuffer & fb, CString * pstr)
{
	if (!MatchChar(fb, '"'))
		return;

	int cb = 256;
	char utf8[256];
	char * pch = utf8;

	int chNext = fb.Peek();
	while (chNext != '"')
	{
		if (pch - utf8 < 256)
			*pch++ = chNext;
		fb.NextChar();
		chNext = fb.Peek();
	}
	MatchChar(fb, '"');

	int cch = min(pch - utf8, 256);
	// UTF16 should never have more characters than UTF8.
	wchar utf16[257];
	int cchUtf16 = Utf8ToUtf16(utf8, cch, utf16, cb);
	utf16[cchUtf16] = 0;

	*pstr = utf16;
}

void CSILGraphiteControlCtrl::ApplyAttributes(CList<CString, CString> & attrNames,
	CList<CString, CString> & attrValues, Range & rngNew)
{
	while (!attrNames.IsEmpty())
	{
		CString attrName = attrNames.GetHead();
		CString attrValue = attrValues.GetHead();

		if (attrName == _T("style"))
		{
			// Interpret the style string.
			CComBSTR bstrValue = ::W2BSTR(attrValue);
			wchar * pch = bstrValue;
			wchar * pchStop = bstrValue + bstrValue.Length();

			while (pch < pchStop)
			{
				if (memcmp(_T("font-family:"), pch, (isizeof(wchar) * 12)) == 0)
				{
					pch += 12;
					while (*pch == ' ')
						pch++;
					wchar * pFont = rngNew.lf.lfFaceName;
					while (*pch != ';' && pch < pchStop)
						*pFont++ = *pch++;
					*pFont = 0;
					if (*pch = ';')
						pch++;
				}
				else if (memcmp(_T("font-size:"), pch, (isizeof(wchar) * 10)) == 0)
				{
					pch += 10;
					while (*pch == ' ')
						pch++;
					wchar num[20];
					wchar * pNum = num;
					while (*pch >= '0' && *pch <= '9')
						*pNum++ = *pch++;
					*pNum = 0;
					if (*pch == '%')
					{
						int percent = _ttoi(num);
						rngNew.lgchrp.dympHeight = m_currFontSize * percent * 10;
						rngNew.sizePercent = percent;
						pch++;
					}
					else if (*pch == 'p' && *(pch+1) == 't')
					{
						rngNew.lgchrp.dympHeight = _ttoi(num) * 1000;
						pch +=2;
					}
					else
					{
						// Some size that we can't handle.
						m_bOpenHtmlNonFatal = true;
						while (pch < pchStop && *pch != ';')
							pch++;
					}

					if (*pch = ';')
						pch++;
				}
				else
				{
					// Some formatting that we can't handle.
					m_bOpenHtmlNonFatal = true;
					while (pch < pchStop && *pch != ';')
						pch++;
				}

				while (*pch == ' ' && pch < pchStop)
					pch++;
			}
		}

		attrNames.RemoveHead();
		attrValues.RemoveHead();
	}
}
