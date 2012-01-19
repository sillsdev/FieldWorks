/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwSelection.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	The various kinds of selection (currently only text) that can be made in views.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE
#define EDITABLE_SELECTIONS_ONLY 1

#ifndef WIN32
// GUID attachments
template<> const GUID __uuidof(VwTextSelection)("102AACD1-AE7E-11d3-9BAF-00400541F9E9");
template<> const GUID __uuidof(VwPictureSelection)("6AFD893B-6336-48a8-953A-3A6C2879F721");
#endif //!WIN32

using namespace std;

enum CharacterType { kSpace, kPunc, kAlpha };
static CharacterType GetCharacterType(ILgCharacterPropertyEngine * pcpe, OLECHAR chw);

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

IMPLEMENT_SIL_DISPATCH(IVwSelection, &IID_IVwSelection, &LIBID_Views)

static DummyFactory g_fact(
	_T("SIL.Views.VwTextSelection"));


//:>********************************************************************************************
//:>	VwSelection Constructors/Destructor
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwSelection::VwSelection()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_fShowing = false;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwSelection::~VwSelection()
{
	if (m_qrootb)
	{
		m_qrootb->UnregisterSelection(this);
		m_qrootb.Clear();
	}
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Standard COM function.

	@param riid - reference to the desired interface ID.
	@param ppv - address that receives the interface pointer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwSelection)
		*ppv = static_cast<IVwSelection *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwSelection);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

void GetCpeFromRootAndProps(VwRootBox * prootb, ITsTextProps * pttp, ILgCharacterPropertyEngine ** ppcpe)
{
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(prootb->GetDataAccess()->get_WritingSystemFactory(&qwsf));
	if (pttp)
	{
		int ws, tmp;
		CheckHr(pttp->GetIntPropValues(ktptWs, &tmp, &ws));
		CheckHr(qwsf->get_CharPropEngine(ws, ppcpe));
	}
	else
	{
		ILgCharacterPropertyEnginePtr qcpe;
		qcpe.CreateInstance(CLSID_LgIcuCharPropEngine);
		*ppcpe = qcpe.Detach();
	}
}
/*----------------------------------------------------------------------------------------------
	Override to allow CLSID_VwTextSelection trick so we can find out if an interface is our own
	implementation.

	@param riid - reference to the desired interface ID.
	@param ppv - address that receives the interface pointer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwSelection)
		*ppv = static_cast<IVwSelection *>(this);
	else if (&riid == &CLSID_VwTextSelection)	// ERRORJOHN
		*ppv = static_cast<VwTextSelection *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwSelection);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

/***********************************************************************************************
	IVwSelection methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Answer true if the selection is a range.

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_IsRange(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);
	*pfRet = !IsInsertionPoint();
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Update the properties of the selected text.
	An item is typically passed in for each TsTextProps previously returned by a
	call to GetSelectionProps. A null in the array passed here means do not
	change the properties for that range. A Ttp means change properties to that.

	@param cttp - number of text properties in the array.
	@param prgpttp - array of text properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::SetSelectionProps(int cttp, ITsTextProps ** prgpttp)
{
	return S_OK; // There's no way to get one of these objects at present, so anything works.
}

/*----------------------------------------------------------------------------------------------
	Get the paragraph-level properties of the selected paragraph(s).
	Note that, unlike character level properties, we can only read the actual state
	of the paragraphs (IVwPropertyStore), not the TsTextProps that produced them.
	In fact, it is only in StText or something similar that each paragraph has an
	associated TsTextProps. However, we can get the actual properties.

	@param cttpMax
	@param prgpvps
	@param pcttp
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::GetParaProps(int cttpMax, IVwPropertyStore ** prgpvps, int * pcttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgpvps, cttpMax);
	ChkComOutPtr(pcttp);
	// By default a selection has no text props, so leave *pcttp zero.
	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	Get a sequence of TsTextProps, one for each range in the current selection.
	If the current selection is an insertion point return one text props,
	the one that will currently be used for an inserted character.
	If the selection is not a text selection at all return 0 objects. This is the default here.

	@param cttpMax
	@param prgpttp
	@param prgpvps
	@param pcttp
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::GetSelectionProps(int cttpMax, ITsTextProps ** prgpttp,
	IVwPropertyStore ** prgpvps, int * pcttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgpttp, cttpMax);
	ChkComArrayArg(prgpvps, cttpMax);
	ChkComOutPtr(pcttp);
	// By default a selection has no text props, so leave 0.
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	These methods allow getting info about the current selection.
	A selection has an anchor and end point, which may be the same (insertion point) or
	different(range). Each consists of a position in a string. (We may later support other
	kinds of selection, in which case the interface will need extending.)
	A method allows you to obtain the strings and positions.
	You can also find out what properties of what objects those strings are.
	Moreover, it is possible that those objects are displayed as part of the
	display of some property of some higher level object. You can find out what
	properties of what object, all the way up to some object that is not part of
	any higher level object.
	The AssocPrev argument indicates whether the selection is considered to be
	before or after the indicated end point. Thus, for a range, it is false
	at the start of the range and true at the end. For an insertion point,
	it is the same for anchor and end point, and indicates whether the selection
	is primarily considered to come after the preceding character (true) or before
	the following one (false). This is important in deciding things like which
	character properties apply.

	@param fEndPoint - true for end point, false for anchor
	@param pptss - string containing selection
	@param pich - pointer to the offset into string
	@param pfAssocPrev - flag whether the selection is considered to be before or after the
					indicated end point
	@param phvoObj - object to which the string belongs (or 0 if none)
	@param ptag - tag of property to which the string belongs
	@param pws - identifies which alternative, if prop is an alternation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::TextSelInfo(ComBool fEndPoint, ITsString ** pptss, int * pich,
	ComBool * pfAssocPrev, HVO * phvoObj, PropTag * ptag, int * pws)
{
	// Note: I'm not worrying about doing a nice job of ones that are overridden.
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	The information returned by TextSelInfo indicates the level-0 object/prop
	information. CLevels indicates how many levels including that exist.

	@param fEndPoint - True for end point, false for anchor.
	@param pclev - Pointer to an integer for returning the number of levels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::CLevels(ComBool fEndPoint, int * pclev)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pclev);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Returns info about level ilev. Level 0 returns the same hvoObj and ptag as
	TextSelInfo. Level 1 is the object/prop that contains hvoObj[0].
	Level 2 is the object/prop that contains hvoObj[1]. And so forth.
	The ihvo returned for level n is the zero-based index of hvoObj[n-1] in prop
	tag[n] of hvoObj[n]. It is always 0 for level 0.
	The pcpropPrevious argument is sometimes useful in the rare cases where,
	within a display of a certain object, the same property is displayed more
	than once. For example, within the display of a book, we might display the
	sections once using a view that shows titles to produce a table of contents,
	then again to produce the main body of the book. cpropPrevious indicates
	how many previous occurrences of property tag there are in the display of hvoObj,
	before the one which contains the indicated end point.

	@param fEndPoint - true for end point, false for anchor.
	@param ilev - level for which information is desired.
	@param phvoObj
	@param ptag
	@param pihvo
	@param pcpropPrevious
	@param ppvps
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::PropInfo(ComBool fEndPoint, int ilev, HVO * phvoObj, PropTag * ptag,
	int * pihvo, int * pcpropPrevious, IVwPropertyStore ** ppvps)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return true if anchor is at the bottom/right (or left if RtoL) of the selection

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_EndBeforeAnchor(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfRet);
	// TODO: Someone should try to determine if any meaningful implementation can be devised.
	// If not, we might want to return E_NOTIMPL. For now, let's just take a chance on lying,
	// and guess that most of the time the end will be after the anchor.
	*pfRet = false;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Given the same first three arguments as used to draw the root, indicate where the
	selection is drawn. prdPrimary will be set to a rectangle in destination coords
	the bounds the selection as closely as possible; if there is a split cursor,
	prdSecondary gives the place where the secondary is drawn, and pfSplit is true.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrc
	@param rcDst
	@param prdPrimary
	@param prdSecondary
	@param pfSplit
	@param pfEndBeforeAnchor
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::Location(IVwGraphics * pvg, RECT rcSrc, RECT rcDst, RECT * prdPrimary,
	RECT * prdSecondary, ComBool * pfSplit, ComBool * pfEndBeforeAnchor)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point up one graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwSelection::PageUpKey(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	LoseSelection(true);
	DoPageUpDown(pvg, true/*is page up*/, false/*is selection*/);
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point down one graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwSelection::PageDownKey(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	LoseSelection(false);
	DoPageUpDown(pvg, false/*is page up*/, false/*is selection*/);
}


/*----------------------------------------------------------------------------------------------
	Extend the selection up one graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwSelection::ShiftPageUpKey(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	DoPageUpDown(pvg, true/*is page up*/, true/*is selection*/);
}

/*----------------------------------------------------------------------------------------------
	Extend the selection down one graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwSelection::ShiftPageDownKey(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	DoPageUpDown(pvg, false/*is page up*/, true/*is selection*/);
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the top of the graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwSelection::ControlPageUpKey(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	LoseSelection(true);
	DoPageUpDown(pvg, true/*is page up*/, false/*is selection*/);
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the bottom of the graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcDocumentCoord
	@param rcClientCoord
----------------------------------------------------------------------------------------------*/
void VwSelection::ControlPageDownKey(IVwGraphics * pvg, Rect rcDocumentCoord, Rect rcClientCoord)
{
	AssertPtr(pvg);
	LoseSelection(false);
	DoCtrlPageUpDown(pvg, rcDocumentCoord, rcClientCoord, false/*is page up*/, false/*is selection*/);
}

/*----------------------------------------------------------------------------------------------
	Extend the selection to the top of the graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcDocumentCoord
	@param rcClientCoord
----------------------------------------------------------------------------------------------*/
void VwSelection::ControlShiftPageUpKey(IVwGraphics * pvg, Rect rcDocumentCoord, Rect rcClientCoord)
{
	AssertPtr(pvg);
	DoCtrlPageUpDown(pvg, rcDocumentCoord, rcClientCoord, true/*is page up*/, true/*is selection*/);
}

/*----------------------------------------------------------------------------------------------
	Extend the selection to the bottom of the graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcDocumentCoord
	@param rcClientCoord
----------------------------------------------------------------------------------------------*/
void VwSelection::ControlShiftPageDownKey(IVwGraphics * pvg, Rect rcDocumentCoord, Rect rcClientCoord)
{
	AssertPtr(pvg);
	DoCtrlPageUpDown(pvg, rcDocumentCoord, rcClientCoord, false/*is page up*/, true/*is selection*/);
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point up (or down if not fIsPageUp) one graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fIsPageUp - true to handle page up, false to handle page down
	@param fIsExtendSelection - true when shift key was pressed (extend selection)
----------------------------------------------------------------------------------------------*/
void VwSelection::DoPageUpDown(IVwGraphics * pvg, bool fIsPageUp, bool fIsExtendedSelection)
{
	AssertPtr(pvg);

	Rect rcSrc, rcDest;
	IVwGraphics * pvgTmp;
	m_qrootb->Site()->GetGraphics(m_qrootb, &pvgTmp, &rcSrc, &rcDest);
	m_qrootb->Site()->ReleaseGraphics(m_qrootb, pvgTmp);

	// The amount to move is the visible page height
	// (The standard behavior of Word and other apps is to move an entire page and leave the IP
	// at the same position on the screen)
	COMINT32 lPageHeight = VisiblePageHeight(pvg, rcSrc, rcDest);

	// going up subtract, going down add
	if(fIsPageUp)
		lPageHeight *= -1;
	MoveIpVerticalByAmount(pvg, lPageHeight, false, fIsExtendedSelection);
}

/*----------------------------------------------------------------------------------------------
	Move the selection vertically by the specified number of pixels.
	If we have a range selection:
		If moving up, then move from the top of the selection.
		If moving down, then move from the bottom of the selection.

	@param lYAdjustment The amount to move the ip
----------------------------------------------------------------------------------------------*/
void VwSelection::MoveIpVerticalByAmount(IVwGraphics * pvg, int lYAdjustment, bool fPutSelAtEdge,
										 bool fIsExtendedSelection)
{
	AssertPtr(pvg);
	IVwGraphicsWin32 * pvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **) &pvg32));
	AssertPtr(pvg32);

	VwRootBox * pRootBox = RootBox();
	AssertPtr(pRootBox);
	if(!pRootBox)
		return;

	Rect rcSelection;
	ComBool fSplit, fEndBeforeAnchor;
	Rect rcSrc, rcDest;
	IVwGraphics * pvgTmp;
	m_qrootb->Site()->GetGraphics(m_qrootb, &pvgTmp, &rcSrc, &rcDest);

	// convert adjustment to units for the current view
	int viewUnitsPerInch;
	CheckHr(pvgTmp->get_YUnitsPerInch(&viewUnitsPerInch));
	int screenUnitsPerInch;
	CheckHr(pvg->get_YUnitsPerInch(&screenUnitsPerInch));
	lYAdjustment *=  viewUnitsPerInch / screenUnitsPerInch;

	m_qrootb->Site()->ReleaseGraphics(m_qrootb, pvgTmp);

	int rootbHeight;
	CheckHr(pRootBox->get_Height(&rootbHeight));

	ComBool fRange;
	CheckHr(get_IsRange(&fRange));
	Point newPos;
	int originalY = 0;
	if (!fRange)
	{
		Rect rcSecondary;
		CheckHr(Location(pvg, rcSrc, rcDest, &rcSelection, &rcSecondary, &fSplit, &fEndBeforeAnchor));
		originalY = rcSelection.top + (rcSelection.Height() /2);
		newPos.x = rcSelection.left + rcSelection.Width() / 2;
	}
	else
	{
		Rect rcSelTop, rcSelBottom, rcIgnored;
		IVwSelectionPtr qVwSelAnchor;
		IVwSelectionPtr qVwSelEnd;
		CheckHr(EndPoint(false, &qVwSelAnchor));
		CheckHr(qVwSelAnchor->Location(pvg, rcSrc, rcDest, &rcSelTop, &rcIgnored, &fSplit,
			&fEndBeforeAnchor));
		CheckHr(EndPoint(true, &qVwSelEnd));
		CheckHr(qVwSelEnd->Location(pvg, rcSrc, rcDest, &rcSelBottom, &rcIgnored, &fSplit,
			&fEndBeforeAnchor));

		if (fEndBeforeAnchor)
		{
			Rect tmp = rcSelTop;
			rcSelTop = rcSelBottom;
			rcSelBottom = tmp;
		}

		Rect bounds = pRootBox->GetBoundsRect(pvg, rcSrc, rcDest);
		rcSelection = Rect(bounds.left, rcSelTop.top, bounds.right, rcSelBottom.bottom);

		// If we have a range selection, then make the proper adjustments
		originalY = rcSelBottom.bottom - AdjustEndLocation(pvg, rcSrc, rcDest);

		if (fPutSelAtEdge)
			newPos.x = (lYAdjustment > 0 ? rcSelection.left : rcSelection.right);
		else
			newPos.x = rcSelBottom.left;
	}

	newPos.y = originalY + lYAdjustment;
	if(newPos.y <= rcDest.top || newPos.y > rcDest.top + rootbHeight)
	{
		newPos.y = max<LONG>(newPos.y, static_cast<int>(rcDest.top));
		newPos.y = min<LONG>(newPos.y, static_cast<int>(rcDest.top + rootbHeight));
		newPos.x = (fEndBeforeAnchor? rcSelection.left : rcSelection.right);
	}

	// OK, we need to move by dy (newPos.y - originalY). But, we may have to expand a
	// lazy box there in order to display a whole screen full.
	// Make the same PrepareToDraw call that the rendering code will make
	// so that our mouseDown event will be valid
	Rect clipRect;
	CheckHr(pvg->GetClipRect((int*)&clipRect.left, (int*)&clipRect.top, (int*)&clipRect.right,
		(int*)&clipRect.bottom));
	clipRect.Offset(0, lYAdjustment);
	CheckHr(pvg32->SetClipRect(&clipRect));

	VwPrepDrawResult pdr;

	CheckHr(pRootBox->PrepareToDraw(pvg, rcSrc, rcDest, &pdr));

	if(fIsExtendedSelection)
		CheckHr(pRootBox->MouseDownExtended(newPos.x, newPos.y, rcSrc, rcDest));
	else
		CheckHr(pRootBox->MouseDown(newPos.x, newPos.y, rcSrc, rcDest));

	pRootBox->NotifySelChange(ksctDiffPara);
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the top or bottom of the graphical page.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcDocumentCoord
	@param rcClientCoord
	@param fIsPageUp - true to handle page up, false to handle page down
	@param fIsExtendSelection - true when shift key was pressed (extend selection)
----------------------------------------------------------------------------------------------*/
void VwSelection::DoCtrlPageUpDown(IVwGraphics * pvg, Rect rcDocumentCoord, Rect rcClientCoord,
								   bool fIsPageUp, bool fIsExtendedSelection)
{
	// Ignore the ctrl for now: see following note
	return DoPageUpDown(pvg, fIsPageUp, fIsExtendedSelection);

	// The following code would work if the rcClientCoord, rcDocumentCoord transformation included
	//   the shift information (by how much has the client scrolled the document
	//   this information is not available to us though from a key press at this time.
	// We leave the following code in hopes that someday that information will be accessible and
	//   easily hooked up.
	AssertPtr(pvg);

	VwRootBox * pRootBox = RootBox();
	AssertPtr(pRootBox);
	if(!pRootBox){
		return;
	}

	IVwRootSite * pVwRootSite = pRootBox->Site();
	Assert(pVwRootSite);
	if(!pVwRootSite){
		return;
	}

	// we need the height of our window to know how much to scroll by
	HWND hwndRootSite;
	CheckHr(pVwRootSite->get_Hwnd(reinterpret_cast<DWORD *>(&hwndRootSite)));
	Assert(hwndRootSite);


	Rect rcRootSite;
#ifdef WIN32
	if(!GetClientRect(hwndRootSite, &rcRootSite)){
		Assert(false); // we better have a valid HWND at this point! That's the only reason GetClientRect should fail that we could think of.
		return;
	}
#else
	ComSmartPtr<IVwWindow> qhwndWrapper;
	qhwndWrapper.CreateInstance(CLSID_VwWindow);
	CheckHr(qhwndWrapper->put_Window(reinterpret_cast<DWORD *>(hwndRootSite)));
	CheckHr(qhwndWrapper->GetClientRectangle(&rcRootSite));
#endif

	// rcRootSite is in client coordinates. Map rcRootSite from client coordinates to document coordinates
//	rcRootSite.Map(rcClientCoord, rcDocumentCoord);
	int newX, newY;

	if(fIsPageUp){
		newX = rcRootSite.left;
		newY = rcRootSite.top;
	}
	else{
		newX = rcRootSite.right;
		newY = rcRootSite.bottom;
	}

	if(!fIsExtendedSelection) {
		CheckHr(pRootBox->MouseDown(newX, newY, rcDocumentCoord, rcClientCoord));
	}
	else {
		CheckHr(pRootBox->MouseDownExtended(newX, newY, rcDocumentCoord, rcClientCoord));
	}

	pRootBox->NotifySelChange(ksctDiffPara);
}

/*----------------------------------------------------------------------------------------------
	Returns the height of a page in document coordinates or 0 if unable to determine.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcDocumentCoord
	@param rcClientCoord
----------------------------------------------------------------------------------------------*/
COMINT32 VwSelection::VisiblePageHeight(IVwGraphics * pvg, Rect rcDocumentCoord, Rect rcClientCoord)
{
	AssertPtr(pvg);

	VwRootBox * pRootBox = RootBox();

	AssertPtr(pRootBox);
	if(!pRootBox){
		return 0;
	}

	IVwRootSite * pVwRootSite = pRootBox->Site();
	Assert(pVwRootSite);
	if(!pVwRootSite){
		return 0;
	}

	// we need the height of our window to know how much to scroll by
	HWND hwndRootSite;
	CheckHr(pVwRootSite->get_Hwnd(reinterpret_cast<DWORD *>(&hwndRootSite)));
	Assert(hwndRootSite);

	Rect rcRootSite;
#ifdef WIN32
	if(!GetClientRect(hwndRootSite, &rcRootSite)){
		Assert(false); // we better have a valid HWND at this point! That's the only reason GetClientRect should fail that we could think of.
		return 0;
	}
#else
	ComSmartPtr<IVwWindow> qhwndWrapper;
	qhwndWrapper.CreateInstance(CLSID_VwWindow);
	CheckHr(qhwndWrapper->put_Window(reinterpret_cast<DWORD *>(hwndRootSite)));
	CheckHr(qhwndWrapper->GetClientRectangle(&rcRootSite));
#endif

	// rcRootSite is in client coordinates. Map rcRootSite from client coordinates to document coordinates
	//rcRootSite.Map(rcClientCoord, rcDocumentCoord);

	return rcRootSite.Height();
}

/*----------------------------------------------------------------------------------------------
	If the selection is part of one or more paragraphs, return a rectangle that
	contains those paragraphs. Otherwise fail.

	@param prdLoc
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::GetParaLocation(RECT * prdLoc)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Replace what is selected with a TsString.
	If the string contains newlines, the properties associated with the Newline become
	the paragraph properties, if it is possible to make new paragraphs at the current
	selection. Otherwise, newlines are stripped out.

	@param ptss
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::ReplaceWithTsString(ITsString * ptss)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return what is selected as a TsString.

	@param pptss
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::GetSelectionString(ITsString ** pptss, BSTR bstr)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return first para of what is selected as a TsString.

	@param pptss
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::GetFirstParaString(ITsString ** pptss, BSTR bstr,
	ComBool * pfGotItAll)
{
	return E_NOTIMPL;
}
/*----------------------------------------------------------------------------------------------
	Move the selection to the indicated location if it is an insertion point.

	@param fTopLine
	@param xdPos
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::SetIPLocation(ComBool fTopLine, int xdPos)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return true if we can apply paragraph formatting on the current selection.

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_CanFormatPara(ComBool * pfRet)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return true if we can apply character formatting on the current selection. This is only
	possible for text selections, so by default answer false.

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_CanFormatChar(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfRet);
	*pfRet = false;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Return true if we can apply overlay tags on the current selection. This is only possible
	for text selections, so by default answer false.

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_CanFormatOverlay(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfRet);
	*pfRet = false;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Install *this as the active selection.


----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::Install()
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Answer true if this selection follows (comes after in the view) the argument.
	False if they are exactly the same position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_Follows(IVwSelection * psel, ComBool * pfFollows)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Like GetSelectionProps, except that the returned property stores contain the formatting
	for the selection MINUS the hard-formatting that is in the TsTextProps.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::GetHardAndSoftCharProps(
	int cttpMax, ITsTextProps ** prgpttpSel,
	IVwPropertyStore ** prgpvpsSoft, int * pcttp)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Like GetParaProps, except that the returned property stores contain the formatting
	for the selection MINUS the hard-formatting that is in the TsTextProps.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::GetHardAndSoftParaProps(int cttpMax, ITsTextProps ** prgpttpPara,
	ITsTextProps ** prgpttpHard, IVwPropertyStore ** prgpvpsSoft, int * pcttp)
{
	return E_NOTIMPL;
}


//:>********************************************************************************************
//:>	Other VwSelection methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Ensure turned on, unless disabled. This is typically used after moving the selection,
	or after changing the boxes following some edit. It is currently implemented by invalidating
	the area where the selection is visible. If you go back to really drawing it, make sure
	that selections that span pages in print layout views keep working.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwSelection::Show()
{
	if (m_fShowing)
		return;

	if (!IsEnabled())
		return;

	m_fShowing = true;
	InvalidateSel();
}
/*----------------------------------------------------------------------------------------------
	Really draw the selection, subject to current flags that control whether it is visible.
	Currently used only when NOT double-buffering (which is currently never true). Untested.
----------------------------------------------------------------------------------------------*/
void VwSelection::ReallyShow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	//StrAppBuf strb;
	//strb.Format("Show:%d m_fShowing:%d SelectionState:%d\n", this, m_fShowing,
	//	SelectionState());
	//OutputDebugString(strb.Chars());

	if (m_fShowing)
		return;

	if (!IsEnabled())
		return;

	m_fShowing = true;
	// we might have to pass in real values instead of -1, INT_MAX if this is really used
	Draw(pvg, true, rcSrcRoot, rcDstRoot, -1, INT_MAX);
}

/*----------------------------------------------------------------------------------------------
	Draw the selection if it is enabled. This is used with double buffering, when we have
	drawn the underlying stuff on the off-screen bitmap. In that bitmap, the selection is
	not showing, even if m_fShowing indicates that it is on the screen itself. Thus, we
	draw the selection exactly if it IS showing on the screen, so that what is in the bitmap
	winds up matching.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwSelection::DrawIfShowing(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot,
								int ysTop, int dysHeight, bool fDisplayPartialLines)
{
	if (!m_fShowing)
		return;

	if (!IsEnabled())
		return;

	Draw(pvg, true, rcSrcRoot, rcDstRoot, ysTop, dysHeight, fDisplayPartialLines);
}

/*----------------------------------------------------------------------------------------------
	Ensure turned off

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwSelection::Hide()
{
	if (!m_fShowing)
		return;
	m_fShowing = false;
	InvalidateSel();
}
/*----------------------------------------------------------------------------------------------
	Toggle on/off, unless disabled

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwSelection::Invert()
{
	if (!IsEnabled())
		return;
	m_fShowing = !m_fShowing;
	InvalidateSel();
}

/*----------------------------------------------------------------------------------------------
	Invalidate area(s) containing the selection, forcing them to be painted. This currently
	implements hide, show, and invert.
----------------------------------------------------------------------------------------------*/
void VwSelection::InvalidateSel()
{
	VwRootBox * prootb = RootBox();
	Point dpiSrc = prootb->DpiSrc();
	// Using this rectangle as both source and destination causes no transformation at all
	// from the root layout coordinates...which is exactly what we want to pass to invalidate.
	HoldScreenGraphics hg(prootb);
	int dpiX, dpiY;
	CheckHr(hg.m_qvg->get_XUnitsPerInch(&dpiX));
	CheckHr(hg.m_qvg->get_YUnitsPerInch(&dpiY));
	Rect rcSrcRoot(0, 0, dpiSrc.x, dpiSrc.y);
	Rect rcDstRoot(0, 0, dpiX, dpiY);
	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;
	// Get the rectangle(s) bounding the selection.
	CheckHr(Location(hg.m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit,
		&fEndBeforeAnchor));
	IVwRootSite * pvrs = prootb->Site();
	// Invalidate them.
	rdPrimary.Map(rcDstRoot, rcSrcRoot); // reverse transformation to src coords.
	// Fudge a little, since PositionsOfIP is not guaranteed to give an exact result.
	// Note: these fudge values cause clipping rectangle to be too large for lineheight
	// causing FWNX-456, and extra width not needed for linux either
#if WIN32
	rdPrimary.left -= 3;
	rdPrimary.right += 3;
	rdPrimary.top -= 3;
	rdPrimary.bottom += 3;
#endif
	CheckHr(pvrs->InvalidateRect(prootb, rdPrimary.left, rdPrimary.top, rdPrimary.Width(),
		rdPrimary.Height()));
	if (fSplit && !rdSecondary.IsEmpty())
	{
#if WIN32
		rdSecondary.left -= 3;
		rdSecondary.right += 3;
		rdSecondary.top -= 3;
		rdSecondary.bottom += 3;
#endif
		rdSecondary.Map(rcDstRoot, rcSrcRoot); // reverse transformation to src coords.
		CheckHr(pvrs->InvalidateRect(prootb, rdSecondary.left, rdSecondary.top, rdSecondary.Width(),
			rdSecondary.Height()));
	}

}
/*----------------------------------------------------------------------------------------------
	The specified paragraph box is being edited, specifically, the strings which are at itssMin
	to itssLim in the paragraph's text source are to be replaced with new strings from vpst.

	@param pvpbox
	@param itssMin
	@param itssLim
	@param vpst
----------------------------------------------------------------------------------------------*/
void VwSelection::FixTextEdit(VwParagraphBox * pvpbox, int itssMin,
	int itssLim, VpsTssVec & vpst)
{
	// The base class does not care, and does nothing.
}


/*----------------------------------------------------------------------------------------------
	Check whether the given insertion point is at an editable location.  This method must be
	overridden by subclasses.

	@param ichIP - Proposed insertion point for a new selection.
	@param pvpBoxIP - Points to the paragraph of the proposed insertion point.
	@param fAssocPrevIP - Flag whether the proposed insertion point associates with the
					preceding character in the paragraph.

	@return True if the proposed selection is editable, otherwise false.
----------------------------------------------------------------------------------------------*/
bool VwSelection::IsEditable(int ichIP, VwParagraphBox * pvpBoxIP, bool fAssocPrevIP)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Answer whether it is possible to edit at (the anchor of) the selection, specifically,
	whether typing would be able to insert a character at an IP at the anchor.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_IsEditable(ComBool * pfEditable)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfEditable); // this default implementation answers false.
	END_COM_METHOD(g_fact, IID_IVwSelection);
}
STDMETHODIMP VwTextSelection::get_IsEditable(ComBool * pfEditable)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfEditable);
	*pfEditable = IsEditable(m_ichAnchor, m_pvpbox);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Return true if selection is enabled, otherwise false. Being enabled depends on the
	selection state and wether it's a range selection or not (see ${VwSelectionState}).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_IsEnabled(ComBool * pfEnabled)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfEnabled);

	*pfEnabled = IsEnabled();

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Return true if selection is enabled, otherwise false. Being enabled depends on the
	selection state and wether it's a range selection or not (see ${VwSelectionState}).
----------------------------------------------------------------------------------------------*/
bool VwSelection::IsEnabled()
{
	VwSelectionState vss = SelectionState();
	if (vss == vssOutOfFocus)
	{
		// views.idh says for vssOutOfFocus: Insertion point is disabled, ranges are enabled.
		if (IsInsertionPoint())
			return false;
		return true;
	}
	return (vss == vssEnabled);
}

/*----------------------------------------------------------------------------------------------
	Try to find an editable insertion point on the same line, first looking before, and then if
	necessary, looking after.  This method must be overridden by subclasses.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot

	@return True if the the selection is adjusted to an editable location, otherwise false.
----------------------------------------------------------------------------------------------*/
bool VwSelection::FindClosestEditableIP(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return true if selection is still valid. Any editing or data changes that affect
	properties in the root box might change this; check if in doubt.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_IsValid(ComBool * pfValid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfValid);
	*pfValid = m_qrootb.Ptr() != NULL; // If we're invalid, our root box pointer gets cleared.
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Not implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_AssocPrev(ComBool * pfValue)
{
	BEGIN_COM_METHOD;
	ThrowHr(WarnHr(E_NOTIMPL));
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Not implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::put_AssocPrev(ComBool fValue)
{
	BEGIN_COM_METHOD;
	ThrowHr(WarnHr(E_NOTIMPL));
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Get the character offset of the anchor (if fEndPoint is false) or the end point
	(if fEndPoint is true) in whatever paragraph each occurs.
	Note that this is relative to the paragraph as a whole, not the particular
	string property.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_ParagraphOffset(ComBool fEndPoint, int * pich)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pich);
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Get the root box that this selection belongs to.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_RootBox(IVwRootBox ** pprootb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pprootb);
	*pprootb = RootBox();
	AddRefObj(*pprootb);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Return your root box, if possible.
----------------------------------------------------------------------------------------------*/
VwRootBox * VwSelection::RootBox()
{
	return m_qrootb;
}

//:>********************************************************************************************
//:>	VwTextSelection Constructors/Destructor
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwTextSelection::VwTextSelection()
{
	m_pvpbox = NULL;
	m_ichAnchor = 0;
	m_ichAnchor2 = -1;
	m_ichEnd = 0;
	m_fAssocPrevious = false;
	m_xdIP = -1;
}

/*----------------------------------------------------------------------------------------------
	Create a new text selection. The character offsets are relative to the particular
	paragraph box.

	@param pvpbox
	@param ichAnchor
	@param ichEnd
	@param fAssocPrevious - Flag whether the selection associates with the preceding character
					in the paragraph.
----------------------------------------------------------------------------------------------*/
VwTextSelection::VwTextSelection(VwParagraphBox * pvpbox, int ichAnchor, int ichEnd,
	bool fAssocPrevious)
{
	m_pvpbox = pvpbox;
	m_ichAnchor = ichAnchor;
	m_ichAnchor2 = -1;
	m_ichEnd = ichEnd;
	m_fAssocPrevious = fAssocPrevious;
	m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
	m_xdIP = -1;
	m_qrootb = m_pvpbox->Root();
	m_qrootb->RegisterSelection(this);
}

/*----------------------------------------------------------------------------------------------
	Make a new, possibly multi-paragraph selection. ichAnchor is relative to pvpbox.
	ichEnd is relative to pvpboxEnd if it is non-null, otherwise it is a single-para
	selection and ichEnd is relative to ichAnchor.

	@param pvpbox
	@param ichAnchor
	@param ichEnd
	@param fAssocPrevious - Flag whether the selection associates with the preceding character
					in the paragraph.
	@param pvpboxEnd
----------------------------------------------------------------------------------------------*/
VwTextSelection::VwTextSelection(VwParagraphBox * pvpbox, int ichAnchor, int ichEnd,
	bool fAssocPrevious, VwParagraphBox * pvpboxEnd)
{
	m_pvpbox = pvpbox;
	m_ichAnchor = ichAnchor;
	m_ichAnchor2 = -1;
	m_ichEnd = ichEnd;
	m_fAssocPrevious = fAssocPrevious;
	m_pvpboxEnd = pvpboxEnd;
	if (m_pvpboxEnd)
	{
		// Need to establish m_fEndBeforeAnchor. Start out assuming it is true.
		// First get containing boxes, if necessary, such that pboxAnchor and pboxEnd each is
		// or contains m_pvpboxAnchor and m_pvpboxEnd, and furthermore, they have the same
		// container.
		VwBox * pboxAnchor = m_pvpbox;
		VwBox * pboxEnd = m_pvpboxEnd;
		if (m_pvpbox->Container() != m_pvpboxEnd->Container())
		{
			VwGroupBox * pgboxCommonContainer = VwGroupBox::CommonContainer(m_pvpbox,
				m_pvpboxEnd);
			pgboxCommonContainer->Contains(m_pvpbox, &pboxAnchor);
			pgboxCommonContainer->Contains(m_pvpboxEnd, &pboxEnd);
			Assert(pboxAnchor->Container() == pboxEnd->Container());
		}
		m_fEndBeforeAnchor = true;
		for (VwBox * pbox = pboxAnchor; pbox; pbox = pbox->NextOrLazy())
		{
			if (pbox == pboxEnd)
			{
				// We found the end box after the anchor one
				m_fEndBeforeAnchor = false;
				break;
			}
		}
	}
	else
	{
		m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor; // works if in same para
	}
	m_xdIP = -1;
	m_qrootb = m_pvpbox->Root();
	m_qrootb->RegisterSelection(this);
}

/*----------------------------------------------------------------------------------------------
	Get tags that are used in the VwSelection.
----------------------------------------------------------------------------------------------*/
void GetTags(ISilDataAccess * psda, int *stTextTags_Paragraphs, int *stParaTags_StyleRules,
			 int *stTxtParaTags_Contents)
{
	// Get field ids
	Assert(psda);
	IStructuredTextDataAccess * pstda;
	CheckHr(psda->QueryInterface(IID_IStructuredTextDataAccess, (void **) &pstda));
	if (pstda)
	{
		pstda->get_TextParagraphsFlid(stTextTags_Paragraphs);
		pstda->get_ParaPropertiesFlid(stParaTags_StyleRules);
		pstda->get_ParaContentsFlid(stTxtParaTags_Contents);
	}
	else
		ThrowHr(WarnHr(E_NOTIMPL)); // IStructuredTextDataAccess not implemented
}

/*----------------------------------------------------------------------------------------------
	If any edits have been performed, commit them; then notify listeners and the rootbox's
	rootsite. In this base class, there is nothing to do for the commit, but this typically
	involves parsing integers or dates, not an actual persisted save.
	Return false if an error occurred; this usually has already been reported to the
	user, and should abort the user action that caused it.

	@param nHowChanged -- for notification purposes (needed by VecListeners)
	@param prootb -- the rootbox (can be null, in which case it will be computed)
----------------------------------------------------------------------------------------------*/
bool VwSelection::CommitAndNotify(VwSelChangeType nHowChanged, VwRootBox * prootb)
{
	prootb->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Obsolete; use Commit
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::CompleteEdits(VwChangeInfo * pci, ComBool * pfOk)
{
	BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::ExtendToStringBoundaries()
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	This is a common bit of code for several methods that retrieve information about selections.
	Given a pointer to the most specific notifier that covers the point of interest, it
	generates the array of VwSelLevInfo objects that indicate the hierarchy of containing
	objects.
----------------------------------------------------------------------------------------------*/
void VwSelection::BuildVsli(VwNotifier * pnoteInner, int cvsli,  VwSelLevInfo * prgvsli,
	int * pihvoRoot)
{
	int ivsli = 0; // count length of notifier chain.
	VwNotifier * pnote = pnoteInner->Parent();
	// prgvsli[ivsli] is the info about where pnoteInner comes in pnote.
	// Continue building it as long as we have a pnote
	for (; pnote && ivsli < cvsli; ivsli++)
	{
		prgvsli[ivsli].ihvo = pnoteInner->ObjectIndex();
		prgvsli[ivsli].hvo = pnoteInner->Object();
		prgvsli[ivsli].ws = 0;
		prgvsli[ivsli].ich = -1;
		VwBox * pboxFirstProp; // dummy
		int itssFirstProp; // dummy
		int iprop;
		int tag;
		pnote->GetPropForSubNotifier(pnoteInner, &pboxFirstProp, &itssFirstProp,
			&tag, &iprop);
		prgvsli[ivsli].tag = tag;
		prgvsli[ivsli].cpropPrevious = pnote->PropCount(tag, pboxFirstProp, itssFirstProp);
		if (pnoteInner->ObjectIndex() == -1)
		{
			// top-level notifier in an embedded object display. Find the mpbox and retrieve
			// the character information.
			for (VwBox * pbox = pnoteInner->FirstBox(); pbox; pbox = pbox->Container())
			{
				if (pbox->IsMoveablePile())
				{
					VwMoveablePileBox * pmpbox = dynamic_cast<VwMoveablePileBox *>(pbox);
					if (pmpbox->Notifier() == pnote) // pathologically they may nest??
					{
						prgvsli[ivsli].ws = pmpbox->Alternative();
						prgvsli[ivsli].ich = pmpbox->CharIndex();
						break;
					}
				}
			}
		}

		// Go on to next level. Current notifier becomes the inner one.
		pnoteInner = pnote;
		pnote = pnote->Parent();
	}
	if (pnote)
		ThrowHr(WarnHr(E_FAIL)); // not enough slots passed
	*pihvoRoot = pnoteInner->ObjectIndex();
}

VwEditPropVal CallEditableSubstring(VwParagraphBox * pvpbox, int ichMin, int ichLim, bool fAssocPrevious,
   HVO * phvoEdit, PropTag * ptagEdit, int * pichMinEditProp, int * pichLimEditProp,
   IVwViewConstructor ** ppvvcEdit, int * pfragEdit, VwAbstractNotifier ** ppanote, int * piprop,
   VwNoteProps * pvnp, int * pitssProp, ITsString ** pptssProp)

{
	VwEditPropVal vepv;
	// Do this loop at most twice: typically once is enough,
	// try again reversing fAssocPrev if we don't find a string in the desired direction,
	// then give up.
	for ( int iteration = 0; iteration < 2; iteration++)
	{
		vepv = pvpbox->EditableSubstringAt(ichMin, ichLim,
			fAssocPrevious, phvoEdit, ptagEdit, pichMinEditProp, pichLimEditProp, ppvvcEdit,
			pfragEdit, ppanote, piprop, pvnp, pitssProp, pptssProp);
		if (vepv == kvepvNone)
			return kvepvNone;
		if (*pptssProp)
			return vepv; // Normal case, found a string property at once.

		// Somehow, probably because of a less-than-ideally set fAssocPrev, we got
		// an adjacent non-string box property, perhaps an editable icon property.
		if (ichMin == *pichLimEditProp)
		{
			// Found a picture before the property we want.
			if (fAssocPrevious)
			{
				fAssocPrevious = false;
				continue;
			}
		}
		else if (ichLim == *pichMinEditProp) // must be a pic after the string we want
		{
			if (!fAssocPrevious)
			{
				fAssocPrevious = true;
				continue;
			}
		}
		// Enhance JohnT: a picture or pile at the start or end of a paragraph is an interesting
		// possibility--can we return anything more useful for that?
		// How about a picture on both sides?
		return kvepvNone; // Don't have anything useful.
	}
	return kvepvNone; // Don't have anything useful.
}

//:>********************************************************************************************
//:>	IVwSelection methods (VwTextSelection implementations)
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a sequence of TsTextProps, one for each range in the current selection.
	If the current selection is an insertion point return one text props,
	the one that will currently be used for an inserted character.
	If the selection is not a text selection at all return 0 objects.
	Give a ref count on the TsTextProps and VwPropertyStore objects.
	Char indexes are logical.

	Note that this method is only guaranteed to work correctly over the first field
	in the selection. If the selection spans multiple fields, all but the first may
	be ignored.

	@param cttpMax
	@param prgpttp
	@param prgpvps
	@param pcttp
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::GetSelectionProps(int cttpMax, ITsTextProps ** prgpttp,
	IVwPropertyStore ** prgpvps, int * pcttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgpttp, cttpMax);
	ChkComArrayArg(prgpvps, cttpMax);
	ChkComOutPtr(pcttp);
	Assert(m_qrootb);
	Assert(m_pvpbox);

	VwTxtSrc * pts = m_pvpbox->Source();
	if (m_ichAnchor == m_ichEnd && !m_pvpboxEnd)
	{
		// Insertion point.
		*pcttp = 1;
		if (cttpMax > 0)
		{
			// We need to get the actual ttp and prop store.  That requires us to figure out
			// which property would actually be edited if we started editing here.  Do that
			// first.
			HVO hvoEdit;
			int tagEdit;
			// While editing, the range of the whole TsString that corresponds to the
			// property being edited. Meaningless if m_qtsbProp is null.
			int ichMinEditProp;
			int ichLimEditProp;
			// The view constructor, if any, responsible for the edited property.
			IVwViewConstructorPtr qvvcEdit;
			int fragEdit; // The fragment identifier the VC needs for the edited property.
			VwAbstractNotifierPtr qanote; // The notifier for the property.
			int iprop; // the index of the property within that notifier.
			VwNoteProps vnp; // Notifier Property attributes.
			int itssProp; // index of edited string in list for this para box
			ITsStringPtr qtssProp;

			int ichString;
			VwPropertyStorePtr qzvps;
			// This uses notifier information to determine the substring we want to edit
			// and what property etc. it belongs to.
			VwEditPropVal vepv = CallEditableSubstring(m_pvpbox, m_ichAnchor, m_ichAnchor,
				m_fAssocPrevious, &hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp,
				&qvvcEdit, &fragEdit, &qanote, &iprop, &vnp, &itssProp, &qtssProp);
			if (vepv == kvepvNone)
			{
				// Nothing we can edit here, and it is an IP.
				// Return the actual props.
				int ichProps = m_ichAnchor;
				m_pvpbox->Source()->StringFromIch(ichProps, m_fAssocPrevious, &qtssProp, &ichMinEditProp,
					&ichLimEditProp, &qzvps, &itssProp);
				// we've already allowed for m_fAssocPrevious
				ichString = ichProps - ichMinEditProp;
			}
			else
			{
				// Get the start index of that string (it may not be the first in the property,
				// so we cannot reliably use ichMinEditProp).
				ichMinEditProp = m_pvpbox->Source()->IchStartString(itssProp);
				// Compute a character index relative to the selected (possibly) editable
				// string.
				ichString = m_ichAnchor - ichMinEditProp;
				// Base it off the previous character if there is one in that string and
				// we are associated with that character.
				if (m_ichAnchor > ichMinEditProp && m_fAssocPrevious)
					ichString--;
				// Get the view-supplied style associated with the string we want to edit
				pts->StyleAtIndex(itssProp, &qzvps);
			}

			if (!m_qttp)
			{
				if (!qtssProp)
				{
					// Something moderately bizarre has happened, possibly it is an out-of-date
					// selection. We can't get any useful information.
					*pcttp = 0;
					return S_OK;
				}
				CheckHr(qtssProp->get_PropertiesAt(ichString, &m_qttp));
			}
			// As in GetInsertionProps, if the writing system is 0, and this is an editable
			// property, retrieve the default writing system. This makes the results of
			// GetSelectionProps more consistent with what will actually be typed (though,
			// unlike GetInsertionProps, it does not force picture and hot link properties to be
			// off). This is important so that when someone clicks in an empty field the combo
			// boxes show accurately what writing system he will get if he types. The cost is
			// that we are not precisely returning the properties of the string itself...but
			// there are other interface methods the client can use to get the actual string if
			// that is needed.
			int ws, var;
			CheckHr(m_qttp->GetIntPropValues(ktptWs, &var, &ws));
			if (!ws)
			{
				VwNotifier * pnote = dynamic_cast<VwNotifier *>(qanote.Ptr());
				if (pnote && pnote->CProps() > iprop)
				{
					// We're editing a valid property and can retrieve this information
					VwPropertyStore * pvps = pnote->Styles()[m_iprop];
					// JohnT: I don't remember if this can ever not be there, but play safe.
					if (pvps)
						ws = pvps->DefaultWritingSystem();
					if (ws)
					{
						// Got one! use it.
						ITsPropsBldrPtr qtpb;
						CheckHr(m_qttp->GetBldr(&qtpb));
						CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws));
						CheckHr(qtpb->GetTextProps(&m_qttp));
					}
				}
			}
			*prgpttp = m_qttp;
			AddRefObj(*prgpttp);
			// Get the actual props
			VwPropertyStorePtr qzvpsA;
			CheckHr(qzvps->ComputedPropertiesForTtp(m_qttp, &qzvpsA));
			if (vepv != kvepvEditable && qzvpsA->EditableEnum() != ktptNotEditable)
			{
				// JohnT: DON'T do this! See LT-1189 for example. qzvpsA is shared by all
				// similar text, some of which may be editable; changing it makes
				// it ALL not editable.
				//qzvpsA->Unlock();
				//qzvpsA->put_IntProperty(ktptEditable, ktpvDefault, ktptNotEditable);
				//qzvpsA->Lock();

				// Instead we have to make a new property store. It will be saved for future use
				// in this situation, making future calls to ComputedPropertiesForInt efficient.
				VwPropertyStorePtr qzvpsB;
				qzvpsB.Attach(qzvpsA.Detach());
				CheckHr(qzvpsB->ComputedPropertiesForInt(ktptEditable, ktpvDefault, ktptNotEditable, &qzvpsA));
			}
			*prgpvps = qzvpsA.Detach();
		}
		return S_OK;
	}

	// Not an insertion point.
	// Loop over all boxes involved
	VwBox * pboxCurr = m_pvpbox;
	VwParagraphBox * pvpboxLast = m_pvpbox;
	VwParagraphBox * pvpboxCurr;
	int ichStart = m_ichAnchor;
	int ichEnd = 0;
	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
			pboxCurr = m_pvpboxEnd; // and leave Last pointing at end
		else
			pvpboxLast = m_pvpboxEnd; // and leave Curr pointing at start
	}
	if (m_fEndBeforeAnchor)
	{
		ichStart = m_ichEnd;
	}
	int cttp = 0;
	bool fStart = true;
	VwBox * pStartSearch = NULL;
	for (VwBox * pboxNew = pboxCurr; pboxNew; pboxNew = pboxNew->NextInRootSeq())
	{
		if (fStart || pboxCurr->NextBoxForSelection(&pStartSearch) == pboxNew)
		{
			fStart = false;
			pboxCurr = pboxNew;
			pvpboxCurr = dynamic_cast<VwParagraphBox *>(pboxCurr);
			if (!pvpboxCurr)
				continue;
			pts = pvpboxCurr->Source();
			if (pvpboxCurr == pvpboxLast)
				ichEnd = m_fEndBeforeAnchor ? m_ichAnchor : m_ichEnd;
			else
				ichEnd = pvpboxCurr->Source()->Cch();
			ITsStringPtr qtss;
			int ichMinTss;
			int ichLimTss;
			VwPropertyStorePtr qzvps;
			int itss;
			pts->StringFromIch(ichStart, false, &qtss, &ichMinTss, &ichLimTss,
				&qzvps, &itss);

			if (pts->Cch()== 0 && pboxCurr != pvpboxLast)
			{
				// The following loop skips the one string in each empty paragraph, so put
				// in this special case to deal with it. But to get the expected behavior,
				// we must not include the last box in the sequence if it is empty.
				Assert(qtss); // If there was an object the para would not be empty.
				if (cttpMax)
				{
					if (cttp < cttpMax)
					{
						ITsTextPropsPtr qttp;
						TsRunInfo tri;
						CheckHr(qtss->FetchRunInfoAt(0, &tri, &qttp));
						VwPropertyStorePtr qzvpsA;
						CheckHr(qzvps->ComputedPropertiesForTtp(qttp, &qzvpsA));
						prgpvps[cttp] = qzvpsA.Detach();
						prgpttp[cttp] = qttp.Detach();
					}
					else
					{
						return E_FAIL;
					}
				}
				cttp++;
			}

			for (int ich = ichStart; ich < ichEnd;)
			{
				// ich is relative to the paragraph, as is ichMinTss
				int ichNew;
				if (qtss)
				{
					ITsTextPropsPtr qttp;
					TsRunInfo tri;
					CheckHr(qtss->FetchRunInfoAt(ich - ichMinTss, &tri, &qttp));
					if (cttpMax)
					{
						if (cttp < cttpMax)
						{
							VwPropertyStorePtr qzvpsA;
							CheckHr(qzvps->ComputedPropertiesForTtp(qttp, &qzvpsA));
							prgpvps[cttp] = qzvpsA.Detach();
							prgpttp[cttp] = qttp.Detach();
						}
						else
						{
							return E_FAIL;
						}
					}
					cttp++;
					ichNew = ichMinTss + tri.ichLim;
				}
				else
				{
					// Otherwise, there aren't any ttps associated with this char position,
					// ignore it.  Just advance by the one character which is associated with
					// a non-tss.
					ichNew = ichMinTss + 1;
				}
				if (ichNew >= ichLimTss && ichEnd > ichLimTss)
				{
					Assert(pts->CStrings() > (itss+1));
					// We need the next string
					Assert(ichNew == ichLimTss);  // we should have used this string up
					pts->StringAtIndex(++itss, &qtss);
					ichMinTss = ichLimTss;
					int cch;
					if (qtss)
						CheckHr(qtss->get_Length(&cch));
					else
						cch = 1;
					ichLimTss += cch;
				}
				else
				{
					// Only if we didn't move to the next string: there might be some
					// empty strings in there...but processing a run should have
					// caused us to make some progress, otherwise.
					Assert(ichNew > ich);
				}
				ich = ichNew;
			}
		}
		// More boxes?
		if (pboxNew == pvpboxLast)
			break;
		ichStart = 0; // always start at beginning of subsequent boxes
	}
	*pcttp = cttp;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Get the paragraph-level properties of the selected paragraph(s).
	Note that, unlike character level properties, we can only read the actual state
	of the paragraphs (IVwPropertyStore), not the TsTextProps that produced them.
	In fact, it is only in StText or something similar that each paragraph has an
	associated TsTextProps. However, we can get the actual properties.
	The paragraph properties are gotten by running through the root sequence until we have
	gotten the properties for each paragraph.

	@param cttpMax
	@param prgpvps
	@param pcttp
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::GetParaProps(int cttpMax, IVwPropertyStore ** prgpvps,
	int * pcttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgpvps, cttpMax);
	ChkComOutPtr(pcttp);

	VwParagraphBox * pvpboxStart = m_pvpbox;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	if (m_pvpboxEnd && m_fEndBeforeAnchor)
	{
		pvpboxStart = pvpboxEnd;
		pvpboxEnd = m_pvpbox;
	}
	IVwPropertyStore ** ppvps = prgpvps;
	bool fStart = true;
	VwBox * pvpbox = NULL;
	VwBox * pStartSearch = NULL;
	for (VwBox * pboxNew = pvpboxStart; pboxNew; pboxNew = pboxNew->NextInRootSeq())
	{
		if (fStart || pvpbox->NextBoxForSelection(&pStartSearch) == pboxNew)
		{
			fStart = false;
			pvpbox = pboxNew;
			VwParagraphBox * pvpb = dynamic_cast<VwParagraphBox *>(pvpbox);
			if (pvpb)
			{
				if (cttpMax)
				{
					if (ppvps - prgpvps > cttpMax)
						ThrowHr(WarnHr(E_FAIL));
					*ppvps = pvpb->Style();
				}
				ppvps++;
			}
		}
		if (pboxNew == pvpboxEnd)
			break;
	}
	// If we succeeded we need to give ref counts on them all
	// Don't do this inside the loop above because of the danger of running out of buffer.
	if (cttpMax)
	{
		IVwPropertyStore ** ppvpsLim = ppvps;
		for (ppvps = prgpvps; ppvps < ppvpsLim; ppvps++)
			(*ppvps)->AddRef();
	}
	*pcttp = ppvps - prgpvps;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

// Answer true if it is OK to apply the specified properties to the specified field.
bool OkToApplyPropsToField(ISilDataAccess * psda, PropTag tagEdit)
{
	IFwMetaDataCachePtr qmdc;
	CheckHr(psda->get_MetaDataCache(&qmdc));
	int ftype;
	CheckHr(qmdc->GetFieldType(tagEdit, &ftype));
	switch (ftype)
	{
		// These three field types don't support storing style information, so don't make changes to them.
	case kcptMultiUnicode:
	case kcptMultiBigUnicode:
	case kcptUnicode:
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	This basically saves repeating a little chunk of code that is used two places in
	SetSelectionProps. It figures out what to do about the run of chars starting at ich
	in the string ptss, where ichMinTss indicates where this string starts in the paragraph,
	and pttp is the new props (or null) that have been requested to apply to this run.
	ichStart and ichEnd indicate the range of selected characters in this paragraph.
	It returns the lim of the run at ich.
	Note that all input/output character indexes are logical.

	@param ptss
	@param ich
	@param ichMinTss
	@param ichLimTss
	@param ichStart
	@param ichEnd
	@param pttp
	@param pvpboxCurr
	@param hmhtru
----------------------------------------------------------------------------------------------*/
int VwTextSelection::UpdatePropsMap(ITsString * ptss, int ich, int ichMinTss,
	int ichLimTss, int ichStart, int ichEnd, ITsTextProps * pttp,
	VwParagraphBox * pvpboxCurr, HvoTagUpdateMap & hmhtru)
{
	ITsStrBldrPtr qtsb; // for current string, if modified
	ITsTextPropsPtr qttp;
	TsRunInfo tri;
	CheckHr(ptss->FetchRunInfoAt(ich - ichMinTss, &tri, &qttp));
	if (pttp && pttp != qttp)
	{
		// This one got changed! See what property has this string.
		HVO hvoEdit;
		int tagEdit;
		// While editing, the range of the whole TsString that corresponds to
		// the property being edited.  Meaningless if m_qtsbProp is null.
		int ichMinEditProp;
		int ichLimEditProp;
		// The view constructor, if any, responsible for the edited property.
		IVwViewConstructorPtr qvvcEdit;
		// The fragment identifier which the VC needs for the edited property.
		int fragEdit;
		VwAbstractNotifierPtr qanote; // The notifier for the property.
		int iprop; // the index of the property within that notifier.
		VwNoteProps vnp; // Notifier Property attributes.
		int itssProp; // index of edited string in list for this string box
		ITsStringPtr qtssProp;

		// This uses notifier information to determine the editable property
		// following ich (adjusted to be box-relative).  If we don't get the
		// string and range we expect, it is probably because the string we
		// want is not editable, while the previous one is.
		if ((CallEditableSubstring(pvpboxCurr, ich, ich, false, &hvoEdit,
			&tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
			&qanote, &iprop, &vnp, &itssProp, &qtssProp) == kvepvEditable) &&
			qtssProp == ptss &&
			ichMinEditProp == ichMinTss &&
			ichLimEditProp == ichLimTss
			&& OkToApplyPropsToField(RootBox()->GetDataAccess(), tagEdit))
		{
			// We can update this string. Have we already?
			HvoTagRec hvt(hvoEdit, tagEdit);
			UpdateInfo upi;
			if (!hmhtru.Retrieve(hvt, &upi))
			{
				// If this is a property we are already actively editing, use
				// our existing builder.
				if (m_qtsbProp && hvoEdit == m_hvoEdit &&
					tagEdit == m_tagEdit &&
					iprop == m_iprop)
				{
					qtsb = m_qtsbProp;
				}
				else
				{
					// Make a builder based on current state of string.
					CheckHr(ptss->GetBldr(&qtsb));
				}

				// Insert it into the map with the necessary info
				upi.m_qvvcEdit = qvvcEdit;
				upi.m_fragEdit = fragEdit;
				upi.m_vnp = vnp;
				upi.m_qtsb = qtsb;
				hmhtru.Insert(hvt, upi);
			}
			else
			{
				qtsb = upi.m_qtsb;
#ifdef JohnT_10_5_01_CheckForDuplicates
// Just ignore all but the first occurrence. Maybe this should be an error, but
// the user probably doesn't want it to be, if he has tried to apply some formatting
// to a big selection.
				// Check the property is displayed simply; otherwise, fail.
				// We can't reliably deal with the implications of doing two
				// smart updates at once!
				if (upi.m_qvvcEdit || qvvcEdit)
					return E_FAIL;
				if (upi.m_fragEdit != fragEdit)
					return E_FAIL;	// Not yet prepared to handle multiple alts.
				if (upi.m_vnp != vnp)
					return E_FAIL;	// Not prepared to handle different modes of
									// display.
#endif
			}
			// Apply the change to the builder.
			CheckHr(qtsb->SetProperties(max(tri.ichMin,
				ichStart - ichMinEditProp),
				min(tri.ichLim, ichEnd - ichMinEditProp), pttp));
		}
	}
	return tri.ichLim;
}

/*----------------------------------------------------------------------------------------------
	Arrange that subsequent typing (as long as it occurs before the selection is moved)
	will create text with the specified properties. (This is no longer limited to an
	insertion point.)
	The default (non-text) selection does nothing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::SetTypingProps(ITsTextProps * pttp)
{
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Arrange that subsequent typing (as long as it occurs before the selection is moved)
	will create text with the specified properties. (This is no longer limited to an
	insertion point.) This method will *not* issue a SelectionChanged event.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::SetTypingProps(ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pttp);
	m_qttp = pttp;
//	m_pvpbox->Root()->NotifySelChange(ksctSamePara);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Update the properties of the selected text.
	An item is typically passed in for each TsTextProps previously returned by a
	call to GetSelectionProps. A null in the array passed here means do not
	change the properties for that range. A Ttp means change properties to that.
	All char indexes are logical.

	@param cttp
	@param prgpttp
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::SetSelectionProps(int cttp, ITsTextProps ** prgpttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgpttp, cttp);
	if (m_ichAnchor == m_ichEnd && !m_pvpboxEnd)
	{
		if (cttp != 1)
			return E_INVALIDARG;
//		StartEditing(); // OPTIMIZE JohnT: should we do this? May produce unnecessary commit.
		m_qttp = *prgpttp;
		m_pvpbox->Root()->NotifySelChange(ksctSamePara); // Needed to update style comboboxes.
		return S_OK;
	}
	else
	{
		m_qttp.Clear(); // Remembering the old props would make typing over the modified selection reset the properties.
	}
	// Not an insertion point: we are actually going to change something.
	// The change of formatting should be a separate undoable action, so if we already
	// have changes pending, commit them. This also allows us to apply a change of
	// formatting freely to multiple properties if we want.
	bool fOk;
	CommitAndContinue(&fOk);
	if (!fOk)
		// The user has already been warned, the SetSelectionProps can just do nothing.
		return S_OK;

	// OK, go ahead and do it. This loop mirrors very closely the one that obtained
	// the TsTextProps.
	// Note that, for each string, we make the changes to a builder. Thus, the original
	// TsString is not modified until we get done.
	// Also, we build up a set of modified properties. That way, we don't modify the
	// Text Source itself until we have figured all the changes.
	VwTxtSrc * pts;
	// Loop over all boxes involved
	VwBox * pboxCurr = m_pvpbox;
	VwParagraphBox * pvpboxLast = m_pvpbox;
	VwParagraphBox * pvpboxCurr;
	int ichStart = m_ichAnchor;
	int ichEnd;
	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
			pboxCurr = m_pvpboxEnd; // and leave Last pointing at end
		else
			pvpboxLast = m_pvpboxEnd; // and leave Curr pointing at start
	}
	if (m_fEndBeforeAnchor)
	{
		ichStart = m_ichEnd;
	}
	int ittp = 0;
	// This map keeps track of a string builder for each modified property.
	// This is so that if the same property occurs repeatedly in the range, we develop
	// the combined effect of the changes, and update the property only once.
	HvoTagUpdateMap hmhtru;
	bool fStart = true;
	VwBox * pStartSearch = NULL;
	for (VwBox * pboxNew = pboxCurr; pboxNew; pboxNew = pboxNew->NextInRootSeq())
	{
		if (fStart || pboxCurr->NextBoxForSelection(&pStartSearch) == pboxNew)
		{
			fStart = false;
			pboxCurr = pboxNew;
			pvpboxCurr = dynamic_cast<VwParagraphBox *>(pboxCurr);
			if (!pvpboxCurr)
				continue;
			pts = pvpboxCurr->Source();
			if (pvpboxCurr == pvpboxLast)
				ichEnd = m_fEndBeforeAnchor ? m_ichAnchor : m_ichEnd;
			else
				ichEnd = pts->Cch();

			// Handle chars from ichStart to ichEnd in current para
			ITsStringPtr qtss;
			int ichMinTss;
			int ichLimTss;
			VwPropertyStorePtr qzvps;
			int itss;
			pts->StringFromIch(ichStart, false, &qtss, &ichMinTss, &ichLimTss,
				&qzvps, &itss);
			if (pts->Cch() == 0 && pboxCurr != pvpboxLast)
			{
				// The following loop skips the one string in each empty paragraph, so put
				// in this special case to deal with it. But to get the expected behavior,
				// we must not include the last box in the sequence if it is empty.
				Assert(qtss); // If there was an object the para would not be empty.
				UpdatePropsMap(qtss, 0, 0, 0, 0, 0, prgpttp[ittp], pvpboxCurr, hmhtru);
				ittp++;
			}

			for (int ich = ichStart; ich < ichEnd;)
			{
				// ich is relative to the paragraph, like ichMinTss
				int ichNew; // ich for the next iteration.
				if (qtss)
				{
					Assert(ittp < cttp); // We should have a prop for each run that we are changing
					ichNew = ichMinTss + UpdatePropsMap(qtss, ich, ichMinTss, ichLimTss,
						ichStart, ichEnd,
						prgpttp[ittp], pvpboxCurr, hmhtru);
					ittp++;
				}
				else
				{
					// Otherwise, there aren't any ttps associated with this char position,
					// ignore it.  Just advance by the one character which is associated with
					// a non-tss.
					ichNew = ichMinTss + 1;
				}
				if (ichNew >= ichLimTss && ichEnd > ichLimTss)
				{
					// We need the next string
					Assert(ichNew == ichLimTss);  // we should have used this string up
					pts->StringAtIndex(++itss, &qtss);
					ichMinTss = ichLimTss;
					int cch;
					if (qtss)
						CheckHr(qtss->get_Length(&cch));
					else
						cch = 1;
					ichLimTss += cch;
				}
				else
				{
					// Only if we didn't move to the next string: there might be some
					// empty strings in there...but processing a run should have
					// caused us to make some progress, otherwise.
					Assert(ichNew > ich);
				}
				ich = ichNew;
			}
		}
		// More boxes?
		if (pboxNew == pvpboxLast)
			break;
		ichStart = 0; // always start at beginning of subsequent boxes
	}
	// At this point, hmhtru contains a record of all the modified properties.
	// Apply the changes.

	// It is just possible in complex cases that updating the property will cause
	// the selection to be discarded. Keep a ref count so *this won't go away.
	LockThis();

	// Deactivate the selection until all the changes are made.
	VwRootBox * prootb = m_pvpbox->Root();
	VwSelectionState vss = prootb->SelectionState();
	bool fSelShowing = Showing();
	if (fSelShowing)
		Hide();
	if (vss == vssEnabled)
		prootb->HandleActivate(vssDisabled);

	// The views code shouldn't have to create an undo task (especially in the new FDO)
	//BeginUndoTask(prootb->GetDataAccess(), kstidUndoFormatting);
	HvoTagUpdateMap::iterator it = hmhtru.Begin();
	for (; it != hmhtru.End(); ++it)
	{
		HvoTagRec htr = it.GetKey();
		UpdateInfo upi = it.GetValue();
		DoUpdateProp(prootb, htr.m_hvo, htr.m_tag, upi.m_vnp, upi.m_qvvcEdit,
			upi.m_fragEdit, upi.m_qtsb, &fOk);
		// REVIEW JohnT: should we break out of the loop if fOk is false?
	}

	//prootb->GetDataAccess()->EndUndoTask();

	{
		prootb->HandleActivate(vss); // Restore the activation state.
		// Restore the selection if any
		if (fSelShowing)
		{
			VwSelection * psel = prootb->Selection();
			if (psel)
				psel->Show();
		}
	}

	CommitAndNotify(ksctSamePara, prootb);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	These methods allow getting info about the current selection.
	Note: this code has not been tested in a wide variety of tasks. Paths not exercised by
	structured text documents (e.g., paths involving more than two levels, or multiple
	occurrences of a property) may not have been tested.
	A selection has an anchor and end point, which may be the same (insertion point) or
	different(range). Each consists of a position in a string. (We may later support other
	kinds of selection, in which case the interface will need extending.)
	A method allows you to obtain the strings and positions.
	You can also find out what properties of what objects those strings are.
	Moreover, it is possible that those objects are displayed as part of the
	display of some property of some higher level object. You can find out what
	properties of what object, all the way up to some object that is not part of
	any higher level object.
	The AssocPrev argument indicates whether the selection is considered to be
	before or after the indicated end point. Thus, for a range, it is false
	at the start of the range and true at the end. For an insertion point,
	it is the same for anchor and end point, and indicates whether the selection
	is primarily considered to come after the preceding character (true) or before
	the following one (false). This is important in deciding things like which
	character properties apply.

	@param fEndPoint - true for end point, false for anchor
	@param pptss - string containing selection
	@param pich - offset into string
	@param pfAssocPrev - flag whether the selection is considered to be before or after the
					indicated end point
	@param phvoObj - object to which the string belongs (or 0 if none)
	@param ptag - tag of property to which the string belongs
	@param pws - identifies which alternative, if prop is an alternation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::TextSelInfo(ComBool fEndPoint, ITsString ** pptss, int * pich,
	ComBool * pfAssocPrev, HVO * phvoObj, PropTag * ptag, int * pws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	ChkComOutPtr(pich);
	ChkComOutPtr(pfAssocPrev);
	ChkComOutPtr(phvoObj);
	ChkComOutPtr(ptag);
	ChkComOutPtr(pws);

	VwParagraphBox * pvpbox;
	int ichTarget;
	bool fAssocPrevious;
	GetEndInfo(fEndPoint, &pvpbox, ichTarget, fAssocPrevious);

	HVO hvoEdit;
	int tagEdit;
	// While editing, the range of the whole TsString that corresponds to the property
	// being edited. Meaningless if m_qtsbProp is null.
	int ichMinEditProp;
	int ichLimEditProp;
	// The view constructor, if any, responsible for the edited property.
	IVwViewConstructorPtr qvvcEdit;
	int fragEdit; // The fragment identifier which the VC needs for the edited property.
	VwAbstractNotifierPtr qanote; // The notifier for the property.
	int iprop; // the index of the property within that notifier.
	VwNoteProps vnp; // Notifier Property attributes.
	int itssProp; // index of edited string in list for this string box
	ITsStringPtr qtssProp;

	// This uses notifier information to determine the editable property following
	// ich (adjusted to be box-relative). If we don't get the string and range
	// we expect, it is probably because the string we want is not editable,
	// while the previous one is.
	VwEditPropVal vepv = CallEditableSubstring(pvpbox, ichTarget, ichTarget, fAssocPrevious,
		&hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
		&qanote, &iprop, &vnp, &itssProp, &qtssProp);
	if (vepv == kvepvNone)
		return S_OK;
	// Pass the info back to the caller
	*pptss = qtssProp.Detach();
	*pich = ichTarget - ichMinEditProp; // relative to the indicated string.
	*pfAssocPrev = fAssocPrevious;
	*phvoObj = hvoEdit;
	*ptag = tagEdit;
	if (vnp == kvnpStringAltMember || vnp == kvnpUnicodeProp)
		*pws = fragEdit; // otherwise leave 0.

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	The information returned by TextSelInfo indicates the level-0 object/prop
	information.  CLevels indicates how many levels including that exist.

	@param fEndPoint - True for end point, false for anchor.
	@param pclev - Pointer to an integer for returning the number of levels.

	@return S_OK if successful, S_FALSE if it is not editable (*pclev = 0 in that case), or an
					appropriate COM error code such as E_POINTER or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::CLevels(ComBool fEndPoint, int * pclev)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pclev);

	//For TE-5498
	if(!IsValid())
		return E_FAIL;

	VwParagraphBox * pvpbox;
	int ichTarget;
	bool fAssocPrevious;
	GetEndInfo(fEndPoint, &pvpbox, ichTarget, fAssocPrevious);

	HVO hvoEdit;
	int tagEdit;
	// While editing, the range of the whole TsString that corresponds to the property
	// being edited. Meaningless if m_qtsbProp is null.
	int ichMinEditProp;
	int ichLimEditProp;
	// The view constructor, if any, responsible for the edited property.
	IVwViewConstructorPtr qvvcEdit;
	int fragEdit; // The fragment identifier which the VC needs for the edited property.
	VwAbstractNotifierPtr qanote; // The notifier for the property.
	int iprop; // the index of the property within that notifier.
	VwNoteProps vnp; // Notifier Property attributes.
	int itssProp; // index of edited string in list for this string box
	ITsStringPtr qtssProp;

	// This uses notifier information to determine the editable property following
	// ich (adjusted to be box-relative). If we don't get the string and range
	// we expect, it is probably because the string we want is not editable,
	// while the previous one is.
	/*VwEditPropVal vepv = */CallEditableSubstring(pvpbox, ichTarget, ichTarget, fAssocPrevious,
		&hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
		&qanote, &iprop, &vnp, &itssProp, &qtssProp);
	//if (vepv == kvepvNone)
	//	return S_FALSE;
	int clev = 0; // count length of notifier chain.
	VwNotifier * pnoteInner = dynamic_cast<VwNotifier *>(qanote.Ptr());
	// If it's not part of any object we can't return any useful information about containing objects.
	if (!pnoteInner)
		return S_OK;
	while (qanote)
	{
		clev++;
		qanote = qanote->Parent();
	}

	*pclev = clev;

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Returns info about nth level. Level 0 returns the same hvoObj and ptag as
	TextSelInfo. Level 1 is the object/prop that contains hvoObj[0].
	Level 2 is the object/prop that contains hvoObj[1]. And so forth.
	The ihvo returned for level n is the zero-based index of hvoObj[n-1] in prop
	tag[n] of hvoObj[n]. It is always 0 for index 0
	The pcpropPrevious argument is sometimes useful in the rare cases where,
	within a display of a certain object, the same property is displayed more
	than once. For example, within the display of a book, we might display the
	sections once using a view that shows titles to produce a table of contents,
	then again to produce the main body of the book. cpropPrevious indicates
	how many previous occurrences of property tag there are in the display of hvoObj,
	before the one which contains the indicated end point.

	@param fEndPoint - true for end point, false for anchor
	@param ilev
	@param phvoObj
	@param ptag
	@param pihvo
	@param pcpropPrevious
	@param ppvps
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::PropInfo(ComBool fEndPoint, int ilev, HVO * phvoObj,
	PropTag * ptag, int * pihvo, int * pcpropPrevious, IVwPropertyStore ** ppvps)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvoObj);
	ChkComOutPtr(ptag);
	ChkComArgPtr(pihvo);
	ChkComOutPtr(pcpropPrevious);
	ChkComOutPtr(ppvps);

	*pihvo = -1;
	VwParagraphBox * pvpbox;
	int ichTarget;
	bool fAssocPrevious;
	GetEndInfo(fEndPoint, &pvpbox, ichTarget, fAssocPrevious);

	HVO hvoEdit;
	int tagEdit;
	// While editing, the range of the whole TsString that corresponds to the property
	// being edited. Meaningless if m_qtsbProp is null.
	int ichMinEditProp;
	int ichLimEditProp;
	// The view constructor, if any, responsible for the edited property.
	IVwViewConstructorPtr qvvcEdit;
	int fragEdit; // The fragment identifier which the VC needs for the edited property.
	VwAbstractNotifierPtr qanote; // The notifier for the property.
	int iprop; // the index of the property within that notifier.
	VwNoteProps vnp; // Notifier Property attributes.
	int itssProp; // index of edited string in list for this string box
	ITsStringPtr qtssProp;

	// This uses notifier information to determine the editable property following
	// ich (adjusted to be box-relative). If we don't get the string and range
	// we expect, it is probably because the string we want is not editable,
	// while the previous one is.
	VwEditPropVal vepv = CallEditableSubstring(pvpbox, ichTarget, ichTarget, fAssocPrevious,
		&hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
		&qanote, &iprop, &vnp, &itssProp, &qtssProp);
	if (vepv == kvepvNone)
		return S_OK;
	int clev = 0; // count length of notifier chain.
	VwNotifier * pnoteInner = 0;
	while (qanote && clev < ilev)
	{
		clev++;
		pnoteInner = dynamic_cast<VwNotifier *>(qanote.Ptr());
		Assert(pnoteInner); // should never get another type for a text selection.
		qanote = qanote->Parent();
	}
	if (!qanote)
		ThrowHr(WarnHr(E_INVALIDARG)); // don't have this much depth
	*phvoObj = qanote->Object();
	VwNotifier * pnote = dynamic_cast<VwNotifier *>(qanote.Ptr());
	Assert(pnote);
	if (pnoteInner)
	{
		// We are not the innermost notifier
		*pihvo = pnoteInner->ObjectIndex();
		VwBox * pboxFirstProp;
		int itssFirstProp;
		pnote->GetPropForSubNotifier(pnoteInner, &pboxFirstProp, &itssFirstProp, ptag,
			&iprop);
		*pcpropPrevious = pnote->PropCount(*ptag, pboxFirstProp, itssFirstProp);
	}
	else
	{
		// we are the innermost notifier, give the info about the string prop
		*ptag = tagEdit;
		// leave *pihvo 0
		*pcpropPrevious = pnote->PropCount(tagEdit, pvpbox, itssProp);
	}
	// AFTER the if, which in one branch modifies iprop
	*ppvps = pnote->Styles()[iprop];
	AddRefObj(*ppvps);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	This obtains all the information needed to re-create a text selection.
	The exact nature of the information is documented in the MakeTextSelection
	method of VwRootBox (see views.idh).

	@param pihvoRoot
	@param cvsli
	@param prgvsli
	@param ptagTextProp
	@param pcpropPrevious
	@param pichAnchor
	@param pichEnd
	@param pws
	@param pfAssocPrev - flag whether the selection is considered to be before or after the
					indicated end point
	@param pihvoEnd
	@param ppttpIns
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::AllTextSelInfo(int * pihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
	PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd, int * pws,
	ComBool * pfAssocPrev, int * pihvoEnd, ITsTextProps ** ppttpIns)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pihvoRoot);
	ChkComArrayArg(prgvsli, cvsli);
	ChkComOutPtr(ptagTextProp);
	ChkComOutPtr(pcpropPrevious);
	ChkComOutPtr(pichAnchor);
	ChkComOutPtr(pichEnd);
	ChkComOutPtr(pws);
	ChkComArgPtr(pfAssocPrev);
	ChkComArgPtr(pihvoEnd);
	ChkComArgPtrN(ppttpIns);

	return AllTextSelInfoAux(pihvoRoot, cvsli, prgvsli, ptagTextProp, pcpropPrevious,
		pichAnchor, pichEnd, pws, pfAssocPrev, pihvoEnd, ppttpIns, false);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Like AllTextSelInfo, except it obtains information for one endpoint of an arbitrary
	selection.

	@param pihvoRoot
	@param cvlsi
	@param prgvsli
	@param ptagTextProp
	@param pcpropPrevious
	@param pichAnchor
	@param pichEnd
	@param pws
	@param pfAssocPrev - flag whether the selection is considered to be before or after the
					indicated end point
	@param pihvoEnd
	@param ppttpIns
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::AllSelEndInfo(ComBool fEndPoint, int * pihvoRoot,
	int cvsli, VwSelLevInfo * prgvsli, PropTag * ptagTextProp, int * pcpropPrevious,
	int * pich, int * pws, ComBool * pfAssocPrev, ITsTextProps ** ppttpIns)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pihvoRoot);
	ChkComArrayArg(prgvsli, cvsli);
	ChkComOutPtr(ptagTextProp);
	ChkComOutPtr(pcpropPrevious);
	ChkComOutPtr(pich);
	ChkComOutPtr(pws);
	ChkComArgPtr(pfAssocPrev);
	ChkComArgPtrN(ppttpIns);

	return AllTextSelInfoAux(pihvoRoot, cvsli, prgvsli, ptagTextProp, pcpropPrevious,
		pich, NULL, pws, pfAssocPrev, NULL, ppttpIns, fEndPoint);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Guts of AllTextSelInfo and AllSelEndInfo.

	@param pichEnd	- NULL if we are only asking about one end, not both
	@param pihvoEnd	- NULL if we are only asking about one end, not both
	@param fEndPoint - we are only interested in the endpoint, not the anchor, and we return
						the endpoint information in the main arguments
----------------------------------------------------------------------------------------------*/
HRESULT VwSelection::AllTextSelInfoAux(int * pihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
	PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd, int * pws,
	ComBool * pfAssocPrev, int * pihvoEnd, ITsTextProps ** ppttpIns,
	ComBool fEndPoint)
{
	return E_NOTIMPL;
}


HRESULT VwTextSelection::AllTextSelInfoAux(int * pihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
	PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd, int * pws,
	ComBool * pfAssocPrev, int * pihvoEnd, ITsTextProps ** ppttpIns,
	ComBool fEndPoint)
{
	Assert((pichEnd && pihvoEnd) || (!pichEnd && !pihvoEnd));
	// If we asking JUST about the endpoint, use the anchor arguments to return the
	// information.
	Assert(!fEndPoint || (!pichEnd && !pihvoEnd));

	*pihvoRoot = -1;
	*pfAssocPrev = true; // most natural default
	if (pihvoEnd)
		*pihvoEnd = -1;
	VwParagraphBox * pvpbox = (fEndPoint && m_pvpboxEnd) ? m_pvpboxEnd : m_pvpbox;
	bool fAssocPrevious = m_fAssocPrevious;
	if (m_ichEnd != m_ichAnchor || m_pvpboxEnd)
		fAssocPrevious = m_fEndBeforeAnchor;
	if (fEndPoint && !IsInsertionPoint())
		fAssocPrevious = !fAssocPrevious;

	HVO hvoEdit;
	int tagEdit;
	// While editing, the range of the whole TsString that corresponds to the property
	// being edited. Meaningless if m_qtsbProp is null.
	int ichMinEditProp;
	int ichLimEditProp;
	// The view constructor, if any, responsible for the edited property.
	IVwViewConstructorPtr qvvcEdit;
	int fragEdit; // The fragment identifier which the VC needs for the edited property.
	VwAbstractNotifierPtr qanote; // The notifier for the property.
	int iprop; // the index of the property within that notifier.
	VwNoteProps vnp; // Notifier Property attributes.
	int itssProp; // index of edited string in list for this string box
	ITsStringPtr qtssProp;

	// This uses notifier information to determine the editable property following
	// ich (adjusted to be box-relative). If we don't get the string and range
	// we expect, it is probably because the string we want is not editable,
	// while the previous one is.
	int ichOfInterest = (fEndPoint) ? m_ichEnd : m_ichAnchor;
	VwEditPropVal vepv = CallEditableSubstring(pvpbox, ichOfInterest, ichOfInterest,
		fAssocPrevious, &hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit,
		&fragEdit, &qanote, &iprop, &vnp, &itssProp, &qtssProp);
	//if (vepv == kvepvNone)
	//	return S_OK;

	*pichAnchor = ichOfInterest - ichMinEditProp;
	if (pichEnd)
		*pichEnd = m_ichEnd - ichMinEditProp;
	bool fMultiObjSelection = m_ichEnd < ichMinEditProp || m_ichEnd > ichLimEditProp || m_pvpboxEnd;
	*pfAssocPrev = fAssocPrevious;
	*ptagTextProp = tagEdit;
	if (vnp == kvnpStringAltMember || vnp == kvnpUnicodeProp)
		*pws = fragEdit; // otherwise leave 0.
	VwNotifier * pnoteInner = dynamic_cast<VwNotifier *>(qanote.Ptr());
	// If it's not part of any object we can't return any useful information about containing objects.
	if (!pnoteInner)
		return S_OK;
	*pcpropPrevious = pnoteInner->PropCount(tagEdit, pvpbox, itssProp);
	BuildVsli(pnoteInner, cvsli,  prgvsli, pihvoRoot);

	// Now see if we have a multi-object selection. If so, and if we're getting both ends,
	// adjust *pichEnd, and if necessary set *pihvoEnd.
	if (fMultiObjSelection && !fEndPoint)
	{
		vepv = CallEditableSubstring(m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox, m_ichEnd, m_ichEnd, !fAssocPrevious,
			&hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
			&qanote, &iprop, &vnp, &itssProp, &qtssProp);
		if (pichEnd)
			*pichEnd = m_ichEnd - ichMinEditProp;
		// ENHANCE JohnT: No qanote means some sort of large selection in document view.
		// We won't be able to reinstate this selection successfully using these
		// properties. Should we allow such selections? What should happen?
		// For now, this test at least stops it crashing.
		if (pihvoEnd && qanote)
			*pihvoEnd = qanote->ObjectIndex();
	}

	if (ppttpIns)
	{
		if (IsInsertionPoint())
		{
			//	Get the text properties. These are really only useful for an insertion point,
			//	so we only bother getting one.
			IVwPropertyStorePtr qvps;
			int c;
			CheckHr(GetSelectionProps(1, ppttpIns, &qvps, &c));
		} else
		{
			int cttp;
			CheckHr(GetSelectionProps(0, NULL, NULL, &cttp));
			if (cttp == 0)
				return S_OK; // Can't provide any props (this should not happen).
			VwPropsVec vqvps;
			TtpVec vqttp;
			vqttp.Resize(cttp);
			vqvps.Resize(cttp);
			CheckHr(GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
				(IVwPropertyStore **)vqvps.Begin(), &cttp));
			bool fEndBeforeAnchor = m_pvpboxEnd ? m_fEndBeforeAnchor : m_ichEnd < m_ichAnchor;
			if (fEndPoint && fEndBeforeAnchor || (!fEndPoint) && (!fEndBeforeAnchor))
			{
				// want the first props
				*ppttpIns = vqttp[0];
			}
			else
			{
				// want the last
				*ppttpIns = vqttp[vqttp.Size()-1];
			}
			AddRefObj(*ppttpIns);
		}
	}

	return S_OK;
}

HRESULT VwPictureSelection::AllTextSelInfoAux(int * pihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
	PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd, int * pws,
	ComBool * pfAssocPrev, int * pihvoEnd, ITsTextProps ** ppttpIns,
	ComBool fEndPoint)
{
	Assert((pichEnd && pihvoEnd) || (!pichEnd && !pihvoEnd));
	// If we asking JUST about the endpoint, use the anchor arguments to return the
	// information.
	Assert(!fEndPoint || (!pichEnd && !pihvoEnd));

	*pfAssocPrev = true; // most natural default
	if (pihvoEnd)
		*pihvoEnd = -1;
	int iprop;
	VwNotifier * pnoteInner = GetNotifier(&iprop);
	*pichAnchor = -1;
	if (pichEnd)
		*pichEnd = -1;
	*pws = 0;
	if (ppttpIns)
		*ppttpIns = NULL;
	if (pnoteInner)
	{
		BuildVsli(pnoteInner, cvsli,  prgvsli, pihvoRoot);

		PropTag tag = pnoteInner->Tags()[iprop];
		*ptagTextProp = tag;

		VwNoteProps vnp = (VwNoteProps)(pnoteInner->Flags()[iprop] & kvnpPropTypeMask);
		if (vnp == kvnpStringAltMember || vnp == kvnpUnicodeProp)
			*pws = pnoteInner->Fragments()[iprop]; // otherwise leave 0.

		// Count previous occurrences of this tag; usually none.
		*pcpropPrevious = pnoteInner->PropCount(tag, pnoteInner->Boxes()[iprop]);
	}
	else
	{
		*pihvoRoot = -1;
		*ptagTextProp = 0;
		*pcpropPrevious = 0;
	}
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Return true if anchor is at the bottom/right (or left if RtoL) of the selection

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::get_EndBeforeAnchor(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);
	*pfRet = m_fEndBeforeAnchor;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Given the same first three arguments as used to draw the root, indicate where the
	selection is drawn. prdPrimary will be set to a rectangle in destination coords
	the bounds the selection as closely as possible; if there is a split cursor,
	prdSecondary gives the place where the secondary is drawn, and pfSplit is true.
	char indexes are logical.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot
	@param prdPrimary
	@param prdSecondary
	@param pfSplit
	@param pfEndBeforeAnchor
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::Location(IVwGraphics * pvg, RECT rcSrcRoot, RECT rcDstRoot,
	RECT * prdPrimary, RECT * prdSecondary, ComBool * pfSplit, ComBool * pfEndBeforeAnchor)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(prdPrimary);
	ChkComArgPtr(prdSecondary);
	ChkComArgPtr(pfSplit);
	ChkComOutPtr(pfEndBeforeAnchor);

	VwParagraphBox * pvpboxStart = m_pvpbox;
	VwParagraphBox * pvpboxLast = m_pvpbox;
	int ichMin = m_ichAnchor;
	int ichLim = m_ichEnd;
	*pfEndBeforeAnchor = m_fEndBeforeAnchor;
	if (m_fEndBeforeAnchor)
	{
		ichMin = m_ichEnd;
		ichLim = m_ichAnchor;
	}
	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
			pvpboxStart = m_pvpboxEnd; // and leave Last pointing at end
		else
			pvpboxLast = m_pvpboxEnd; // and leave Start pointing at anchor

		int ichLim2 = pvpboxStart->Source()->Cch();
		RECT rdSec1;
		pvpboxStart->LocOfSelection(pvg, ichMin, ichLim2, false, rcSrcRoot, rcDstRoot,
			prdPrimary,	&rdSec1, pfSplit, false, false);
		if (*pfSplit)
		{
			// need to factor in the other rectangle.
			prdPrimary->top = min(prdPrimary->top, rdSec1.top);
			prdPrimary->left = min (prdPrimary->left, rdSec1.left);
			prdPrimary->right = max(prdPrimary->right, rdSec1.right);
			*pfSplit = false; // never treat range as split.
			// ignore bottom, dealt with by last paragraph.
		}
		RECT rdPrim2;
		RECT rdSec2;
		ComBool fSplit2;
		pvpboxLast->LocOfSelection(pvg, 0, ichLim, true, rcSrcRoot, rcDstRoot,
			&rdPrim2, &rdSec2, &fSplit2, false, true);
		// JohnT: getting a split insertion point in a range is VERY unusual, but it can happen
		// with a font like Pig Latin where the very first letter is reordered,
		// if we make a range that stretches to just the start of a non-empty paragraph.
		//Assert(!*pfSplit);
		//Assert(!fSplit2);
		prdPrimary->bottom = rdPrim2.bottom;
		prdPrimary->left = min (prdPrimary->left, rdPrim2.left);
		prdPrimary->right = max(prdPrimary->right, rdPrim2.right);
		if (fSplit2)
		{
			// need to factor in the other rectangle.
			prdPrimary->bottom = max(prdPrimary->bottom, rdSec2.bottom);
			prdPrimary->left = min (prdPrimary->left, rdSec2.left);
			prdPrimary->right = max(prdPrimary->right, rdSec2.right);
		}
		// leave top, the first par's top should be higher.
		// If we have more than two paragraphs and they
		// are not immediately adjacent, widen the rectangle to the full width of the root.
		// (Even our container is not enough...for example, the start might be in a table
		// cell, but intermediate paragraphs may not be part of the same table.
		if (pvpboxStart->NextOrLazy() != pvpboxLast)
		{
			Rect bounds = pvpboxStart->Root()->GetBoundsRect(pvg, rcSrcRoot, rcDstRoot);
			prdPrimary->left = min(bounds.left, m_rcBounds.left);
			prdPrimary->right = max(bounds.right, m_rcBounds.right);
		}
	}
	else
	{
		pvpboxStart->LocOfSelection(pvg, ichMin, ichLim, m_fAssocPrevious, rcSrcRoot,
			rcDstRoot, prdPrimary, prdSecondary, pfSplit, ichMin == ichLim, true);
	}
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	If the selection is part of one or more paragraphs, return a rectangle that
	contains those paragraphs. Otherwise fail.

	@param prdLoc
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::GetParaLocation(RECT * prdLoc)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prdLoc);

	VwParagraphBox * pvpboxStart = m_pvpbox;
	VwParagraphBox * pvpboxLast = m_pvpbox;
	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
			pvpboxStart = m_pvpboxEnd; // and leave Last pointing at end
		else
			pvpboxLast = m_pvpboxEnd; // and leave Start pointing at anchor
	}
	Point pt(pvpboxStart->LeftToLeftOfDocument(), pvpboxStart->TopToTopOfDocument());
	HoldGraphicsAtSrc hg(pvpboxStart->Root(), pt);
	if (m_pvpboxEnd)
	{
		Rect rcStart = pvpboxStart->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
		Rect rcEnd = pvpboxLast->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
		rcStart.Sum(rcEnd);
		*prdLoc = rcStart;
	}
	else
	{
		*prdLoc = pvpboxStart->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
	}

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

//:>********************************************************************************************
//:>	Other VwTextSelection methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Draw selection. Coordinate transformation is for the root box.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fOn
	@param rcSrcRoot
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwTextSelection::Draw(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot,
						   int ysTop, int dysHeight, bool fDisplayPartialLines)
{
	VwBox * pboxCurr = m_pvpbox;
	VwParagraphBox * pvpboxLast = m_pvpbox;
	VwParagraphBox * pvpboxCurr;
	int ichStart = m_ichAnchor;
	int ichEnd;

	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
			pboxCurr = m_pvpboxEnd; // and leave Last pointing at end
		else
			pvpboxLast = m_pvpboxEnd; // and leave Curr pointing at start
	}
	if (m_fEndBeforeAnchor)
	{
		ichStart = m_ichEnd;
	}

	// We do NOT need to expand closures, because by definition it does not matter
	// whether we draw anything for them.
	bool fStart = true;
	VwBox * pStartSearch = NULL;
	for (VwBox * pboxNew = pboxCurr; pboxNew; pboxNew = pboxNew->NextInRootSeq(false))
	{
		if (fStart || pboxCurr->NextBoxForSelection(&pStartSearch, false) == pboxNew)
		{
			fStart = false;
			pboxCurr = pboxNew;
			pvpboxCurr = dynamic_cast<VwParagraphBox *>(pboxCurr);
			if (pvpboxCurr)
			{
				if (pvpboxCurr == pvpboxLast)
					ichEnd = m_fEndBeforeAnchor ? m_ichAnchor : m_ichEnd;
				else
					ichEnd = pvpboxCurr->Source()->Cch();

				int dysTopCurr = pvpboxCurr->TopToTopOfDocument();
				int dysBottomCurr = dysTopCurr + pvpboxCurr->Height();

				// TE-6025: we should also draw the selection for a paragraph that
				// starts above the range and ends below.
				if ((dysTopCurr < ysTop && dysBottomCurr >= ysTop) ||
					(dysTopCurr >= ysTop && dysTopCurr < ysTop + dysHeight))
				{
					m_rcBounds.Sum(pvpboxCurr->DrawSelection(pvg, ichStart, ichEnd,
						m_fAssocPrevious, fOn, rcSrcRoot, rcDstRoot, IsInsertionPoint(),
						pvpboxCurr == pvpboxLast || pvpboxCurr->Container()->IsPileBox(),
						ysTop, dysHeight, fDisplayPartialLines));
				}
			}
		}
#if 0 // JohnT 10/3/01 now we're using NextInRootSeq we get odd effects if we highlight
		// things above or below paragraph level. Even the following doesn't eliminate
		// enough. I so far haven't thought of anything else it would be good to hilite.
		// It isn't worth doing anything to lazy boxes because they're off screen, and
		// boxes directly below the paragraph level have already been dealt with by
		// their containing paragraph.
		else if (!pboxCurr->IsLazyBox() &&
			!dynamic_cast<VwParagraphBox *>(pboxCurr->Container()))
		{
			pboxCurr->HiliteAll(pvg, fOn, rcSrcRoot, rcDstRoot);
		}
#endif

		// More boxes?
		if (pboxNew == pvpboxLast)
			break;
		ichStart = 0; // always start at beginning of subsequent boxes
	}
}

/*----------------------------------------------------------------------------------------------
	GetClosestStringBox takes a pointer to any box and then checks to see if it is a string box
	  If it is, it returns that box cast as a string box. If it isn't, it checks the next box
	  and then previous box to see if either of these is a string box and continues doing so
	  until it either finds one that is a string box or finds no string boxes.

	@param pBox - pointer to a Box
	@param fFoundForward - optional out parameter to tell if the closest string box is located
		 after the current box defaults to null


----------------------------------------------------------------------------------------------*/
VwStringBox * VwTextSelection::GetClosestStringBox(VwBox * pBox, bool * fFoundAfter){
	VwBox * pNextBox = pBox;
	VwBox * pPrevBox = pBox;
	VwStringBox * pStringBox = NULL;
	if(fFoundAfter)
		*fFoundAfter = false;

	VwBox * pStartSearch = NULL;
	pStringBox = dynamic_cast<VwStringBox *>(pBox);
	if(!pStringBox)
	{
		while(pNextBox || pPrevBox)
		{
			if(pNextBox)
			{
				pNextBox = pNextBox->NextBoxForSelection(&pStartSearch);
				pStringBox = dynamic_cast<VwStringBox *>(pNextBox);
				if(pStringBox)
				{
					if(fFoundAfter)
						*fFoundAfter = true;
					break;
				}
			}
			if(pPrevBox)
			{
				pPrevBox = pPrevBox->NextInReverseRootSeq();
				pStringBox = dynamic_cast<VwStringBox *>(pPrevBox);
				if(pStringBox)
					break;
			}
		}
	}

	return pStringBox;
}

/*----------------------------------------------------------------------------------------------

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param prootb
	@param pboxClick
	@param xd
	@param yd
	@param rcSrcRoot
	@param rcDstRoot
	@param rcSrc
	@param rcDst
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ExtendTo(IVwGraphics * pvg, VwRootBox * prootb, VwBox * pboxClick,
	int xd, int yd, Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst)
{
	int ichEnd;
	bool fFoundStringBoxAfter;

	// If the user didn't click something we can identify, can't make any useful change.
	if (!pboxClick)
		return;

	VwStringBox * psboxClick = GetClosestStringBox(pboxClick,&fFoundStringBoxAfter);
	if (!psboxClick)
	{
		// ENHANCE JohnT: figure what offset clicked box corresponds to and change sel end
		// to just cover it.
		Warn("Click in non-text box ignored");
		return; // in the meantime do nothing.
	}

	if (psboxClick->Container() != m_pvpbox)
	{
		// We have already started editing, but without the right indexes and so forth for a
		// multi-para selection. This should never happen.
		Assert(!m_qtsbProp);
		if (m_qtsbProp)
			return;

		// Throw away a selection if one end is inside a picture (caption) and the other
		// end is outside.
		if (psboxClick->IsInMoveablePile() || m_pvpbox->IsInMoveablePile())
			return;

		// See if we can make a multi-paragraph selection: if we clicked a string
		// box in a paragraph with the same container as our own.
		//if (psboxClick) // && psboxClick->Container()->Container() == m_pvpbox->Container())
		{

			// We can do it! First hide our old state...
			Hide();

			// Figure which paragraph comes first.
			VwSelChangeType nHowChanged = ksctNoChange;
			VwParagraphBox * pvpboxEnd = dynamic_cast<VwParagraphBox *>(
				psboxClick->Container());
			if (pvpboxEnd != m_pvpboxEnd)
			{
				nHowChanged = ksctDiffPara;
				m_pvpboxEnd = pvpboxEnd;
			}
			Assert(m_pvpboxEnd);

			if(psboxClick == pboxClick) {
				bool fAssocPrevious; // dummy
				psboxClick->GetPointOffset(pvg, xd, yd, rcSrc, rcDst, &ichEnd, &fAssocPrevious);
			}
			else
			{
				if(fFoundStringBoxAfter)
				{
					ichEnd = pvpboxEnd->Source()->RenToLog(psboxClick->IchMin());
				}
				else
				{
					Assert(pvpboxEnd);
					ichEnd = pvpboxEnd->Source()->Cch();
				}
			}


			// Need to establish m_fEndBeforeAnchor. Start out assuming it is true.
			// First get containing boxes, if necessary, such that pboxAnchor and pboxEnd each
			// is or contains m_pvpboxAnchor and m_pvpboxEnd, and furthermore, they have the
			// same container.
			VwBox * pboxAnchor = m_pvpbox;
			VwBox * pboxEnd = m_pvpboxEnd;
			if (m_pvpbox->Container() != m_pvpboxEnd->Container())
			{
				VwGroupBox * pgboxCommonContainer = VwGroupBox::CommonContainer(m_pvpbox,
					m_pvpboxEnd);
				pgboxCommonContainer->Contains(m_pvpbox, &pboxAnchor);
				pgboxCommonContainer->Contains(m_pvpboxEnd, &pboxEnd);
				Assert(pboxAnchor->Container() == pboxEnd->Container());
			}
			m_fEndBeforeAnchor = true;
			if (pboxAnchor == pboxEnd)
			{
				VwGroupBox * pgboxAnchor = dynamic_cast<VwGroupBox *>(pboxAnchor);
				if (pgboxAnchor)
				{
					for (VwBox * pbox = pgboxAnchor->FirstBox(); pbox; )
					{
						VwStringBox * psBox = dynamic_cast<VwStringBox *>(pbox);
						if (psBox && psBox->Container() == m_pvpbox)
						{
							int  ichMin = psBox->IchMin();
							int dichSeg;
							CheckHr(psBox->Segment()->get_Lim(ichMin, &dichSeg));
							if (m_ichAnchor >= ichMin && m_ichAnchor < ichMin + dichSeg)
							{
								// found the anchor
								m_fEndBeforeAnchor = false;
								break;
							}
						}
						else if (psBox && psBox->Container() == m_pvpboxEnd)
						{
							int  ichMin = psBox->IchMin();
							int dichSeg;
							CheckHr(psBox->Segment()->get_Lim(ichMin, &dichSeg));
							if (ichEnd >= ichMin && ichEnd < ichMin + dichSeg)
							{
								// found the end (leave m_fEndBeforeAnchor true)
								break;
							}
						}
						else
						{
							VwGroupBox * pgboxChild = dynamic_cast<VwGroupBox *>(pbox);
							if (pgboxChild)
							{
								if (pgboxChild->Contains(m_pvpbox) && !pgboxChild->Contains(m_pvpboxEnd))
								{
									// found the anchor
									m_fEndBeforeAnchor = false;
									break;
								}
								else if (!pgboxChild->Contains(m_pvpbox) && pgboxChild->Contains(m_pvpboxEnd))
								{
									// found the end (leave m_fEndBeforeAnchor true)
									break;
								}
								else if (pgboxChild->Contains(m_pvpbox) && pgboxChild->Contains(m_pvpboxEnd))
								{
									// Child box contains both anchor and end
									pbox = pgboxChild->FirstBox();
									continue;
								}
							}
						}
						pbox = pbox->NextOrLazy();
					}
				}
			}
			else
			{
				for (VwBox * pbox = pboxAnchor->NextOrLazy(); pbox; pbox = pbox->NextOrLazy())
				{
					if (pbox == pboxEnd)
					{
						// We found the end box after the anchor one
						m_fEndBeforeAnchor = false;
						break;
					}
				}
			}
			if (nHowChanged < 0 && m_ichEnd != ichEnd)
				nHowChanged = ksctSamePara; // same paragraph
			Assert(ichEnd <= pvpboxEnd->Source()->Cch());
			m_ichEnd = ichEnd;
			if (nHowChanged > 0)
				m_pvpbox->Root()->NotifySelChange(nHowChanged);

			// It appears we don't need this code anymore. Having it here produces
			// phantom selections probably because rcSrc and rcDst are not the same
			// transforms used to draw the selection as a result of a Show(). (TE-5243)
			// we might need to draw the entire selection because it is possible that some
			// parts of the selection are drawn outside of the stringboxes (e.g. trailing
			// spaces. Therefore we can't use just Show().
			//if (m_rcBounds.IsEmpty())
			//{
			//	// Force the recalculation of the selection boundary so that we can
			//	// invalidate correctly.
			//	Draw(pvg, true, rcSrc, rcDst);
			//}
			Show();
		}

		// REVIEW: Why was this code here?
		//int clevAnchor, clevEnd;
		//CLevels(false, &clevAnchor);
		//CLevels(true, &clevEnd);
		//HVO hvo;
		//PropTag tag;
		//int ihvo, cpropPrev;
		//IVwPropertyStorePtr qvps;
		//for (int ilev = 0; ilev < clevAnchor; ilev++)
		//{
		//	CheckHr(this->PropInfo(false, ilev, &hvo, &tag, &ihvo, &cpropPrev, &qvps));
		//}
		//for (int ilev = 0; ilev < clevEnd; ilev++)
		//{
		//	CheckHr(this->PropInfo(true, ilev, &hvo, &tag, &ihvo, &cpropPrev, &qvps));
		//}

		return;
	}

	// clicked box belongs to our anchor paragraph.
	bool fInsideWord = false;
	psboxClick->GetExtendedClickOffset(pvg, xd, yd, rcSrc, rcDst,
		m_pvpbox, m_ichAnchor, &ichEnd);
	if (m_ichAnchor2 != -1)
	{
		// If the new end point is between the two anchor points, don't shrink the
		// selection below the anchor points.
		if ((m_ichAnchor <= ichEnd && ichEnd <= m_ichAnchor2) ||
			(m_ichAnchor2 <= ichEnd && ichEnd <= m_ichAnchor))
		{
			if (m_ichEnd == m_ichAnchor2)
				return;
			ichEnd = m_ichAnchor2;
			fInsideWord = true;
		}
	}

	if (ichEnd == m_ichEnd && !m_pvpboxEnd)
		return; // no change

	VwSelChangeType nHowChanged = m_pvpboxEnd ? ksctSamePara : ksctDiffPara; // change of para if used to be multiple
	Hide();
	// If a word has been selected, ensure new selection includes entire word.
	if (m_ichAnchor2 != -1)
	{
		if (fInsideWord)
		{
			if (m_ichAnchor2 < m_ichAnchor)
			{
				ichEnd = m_ichAnchor;
				m_ichAnchor = m_ichAnchor2;
				m_ichAnchor2 = ichEnd;
			}
		}
		else if ((ichEnd < m_ichAnchor && m_ichAnchor < m_ichAnchor2) ||
			(ichEnd > m_ichAnchor && m_ichAnchor > m_ichAnchor2))
		{
			int ichT = m_ichAnchor;
			m_ichAnchor = m_ichAnchor2;
			m_ichAnchor2 = ichT;
		}
	}
	m_ichEnd = ichEnd;
	m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
	m_pvpboxEnd = NULL; // force to one-paragraph selection
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	Show();
}

// Return true if this is a complex selection, in the sense of something that needs to be
// deleted as a separate UOW from inserting anything.
bool VwTextSelection::IsComplexSelection()
{
	if (m_pvpboxEnd)
		return true; // multi-paragraph ones are always considered complex.
	if (m_ichAnchor == m_ichEnd)
		return false; // insertion point is never complex.
	// Otherwise it's a single-paragraph range, this will tell whether it is complex.
	return IsProblemSelection() == kdptComplexRange;
}


/*----------------------------------------------------------------------------------------------
	Check if the current selection is a problem to delete.
	NOTE: This method only detects kdptComplexRange. There are other conditions that are
	detected elsewhere! So, even if this method returns kdptNone it still might be a problem
	to delete the selection that requires a call to OnProblemDeletion()
----------------------------------------------------------------------------------------------*/
VwDelProbType VwTextSelection::IsProblemSelection()
{
	// If we are already editing, things must be OK.
	if (m_qtsbProp)
		return kdptNone;

	VwParagraphBox * pvpboxFirst = m_pvpbox;
	VwParagraphBox * pvpboxLast = m_pvpboxEnd;
	int ichFirst = m_ichAnchor;
	int ichLast = m_ichEnd;
	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
		{
			pvpboxFirst = m_pvpboxEnd;
			pvpboxLast = m_pvpbox;
			ichFirst = m_ichEnd;
			ichLast = m_ichAnchor;
		}
	}
	else
	{
		pvpboxLast = pvpboxFirst;
		ichFirst = min(m_ichAnchor, m_ichEnd);
		ichLast = max(m_ichAnchor, m_ichEnd);
	}
	bool fIsRange = m_ichAnchor != m_ichEnd || m_pvpboxEnd != NULL;


	// This uses notifier information to determine the substring we want to edit
	// and what property etc. it belongs to.
	ITsStringPtr qtssPropLast;
	HVO hvoEditLast;
	int tagEditLast;
	int ichMinEditPropLast;
	int ichLimEditPropLast;
	IVwViewConstructorPtr qvvcEditLast;
	int fragEditLast;
	VwAbstractNotifierPtr qanoteLast;
	int ipropLast;
	int itssPropLast;
	ITsStringPtr qtssPropFirstLast;
	VwNoteProps vnpLast;
	// If it's a range, we definitely want to focus on the character before the end of the
	// range. If it's an IP, focus on whichever one our member variable tells us to.
	int fAssocPrevious = fIsRange ? true : m_fAssocPrevious;
	VwEditPropVal vepvLast = CallEditableSubstring(pvpboxLast, ichLast, ichLast, fAssocPrevious,
		&hvoEditLast, &tagEditLast, &ichMinEditPropLast, &ichLimEditPropLast, &qvvcEditLast,
		&fragEditLast, &qanoteLast, &ipropLast, &vnpLast, &itssPropLast, &qtssPropLast);

	if (!m_pvpboxEnd)
	{
		// Selection all in one paragraph...either it's all in one property, and we're OK,
		// or it's split across more than one property, and is too complex to edit.
		if (ichFirst >= ichMinEditPropLast && ichFirst <= ichLimEditPropLast)
		{
			// All one property...no problem (unless it's read-only).
			if (vepvLast == kvepvEditable)
				return kdptNone;
			return m_ichEnd == m_ichAnchor ? kdptReadOnly : kdptComplexRange;
		}
		else
			return kdptComplexRange;
	}
	else
	{
		// Multiple paragraphs means it's a 'too complex' problem UNLESS it's a structured text
		// (or something sufficiently like one). If it's multiple paragraphs and the last
		// property isn't editable, just consider it complex.
		if (vepvLast != kvepvEditable)
			return kdptComplexRange;

		// Now see if there is a higher-level property into which we can insert new
		// paragraph-level items. We only contemplate this if the string prop is the
		// whole contents of the paragraph, except that we extended this to allow
		// label information to be added (but not edited) at the start of the first paragraph.
		// Specifically we test that the property we are about to edit is the last in the
		// paragraph: its end is the total number of characters.
		if (itssPropLast >= pvpboxLast->Source()->CStrings())
			return kdptComplexRange;
		// If we are in a multi-para selection, this is the end-point in the last para,
		// so it must have only one property. In the first para, it is good enough that
		// we are in the last string, as just checked.
		if (ichMinEditPropLast > 0)
			return kdptComplexRange;
		// Also, it must be a simple string property.
		if (qvvcEditLast || vnpLast != kvnpStringProp)
			return kdptComplexRange;
		// This will become m_pnoteParent if we have a workable paragraph-level property
		// to edit.
		VwNotifier * pnoteParent;
		pnoteParent = qanoteLast->Parent();
		// We must have a containing property to do paragraph editing.
		if (!pnoteParent)
			return kdptComplexRange;
		int ipropPara = qanoteLast->PropIndex();

		int vnpParentProp = pnoteParent->Flags()[ipropPara];
		// It must be an editable sequence property.
		// We would generally like to check also that it is owning, but that is harder.
		// For now, the programmer must explicitly make any sequence refs where this might
		// otherwise be allowed uneditable.
		// ENHANCE JohnT: could we allow ObjVec also? Dangerous as the inserted para may not
		// show, or similar problems, depending on how the vector is displayed.
		if (vnpParentProp != (kvnpObjVecItems | kvnpEditable) &&
			vnpParentProp != (kvnpLazyVecItems | kvnpEditable))
		{
			return kdptComplexRange;
		}
		// And, specifically, it must be a structured text!
		// get field IDs provided by IStructuredTextDataAccess
		int notUsed_Contents;
		int notUsed_StyleRules;
		int stTextTags_Paragraphs;
		GetTags(m_pvpbox->Root()->GetDataAccess(), &stTextTags_Paragraphs, &notUsed_StyleRules, &notUsed_Contents);

		if (pnoteParent->Tags()[ipropPara] != stTextTags_Paragraphs)
			return kdptComplexRange;

		// Verify that there is a similarly editable string in the first para.
		ITsStringPtr qtssPropFirst;
		HVO hvoEditFirst;
		int tagEditFirst;
		int ichMinEditPropFirst;
		int ichLimEditPropFirst;
		IVwViewConstructorPtr qvvcEditFirst;
		int fragEditFirst;
		VwAbstractNotifierPtr qanoteFirst;
		int ipropFirst;
		int itssPropFirst;
		ITsStringPtr qtssPropFirstFirst;
		VwNoteProps vnpFirst;
		if (CallEditableSubstring(pvpboxFirst, ichFirst, ichFirst, false, &hvoEditFirst,
			&tagEditFirst, &ichMinEditPropFirst, &ichLimEditPropFirst, &qvvcEditFirst,
			&fragEditFirst, &qanoteFirst, &ipropFirst, &vnpFirst, &itssPropFirst,
			&qtssPropFirst) != kvepvEditable)
		{
			return kdptComplexRange;
		}
		// Must be the last string in that paragaph.
		if (ichLimEditPropFirst < pvpboxFirst->Source()->Cch())
			return kdptComplexRange;
		// Must be the same property. ENHANCE JohnT: could we relax this?
		if (tagEditLast != tagEditFirst)
			return kdptComplexRange;
		// Paragraph must belong to the same parent notifier and the same property.
		if (qanoteFirst->Parent() != pnoteParent ||
			qanoteFirst->PropIndex() != qanoteLast->PropIndex())
		{
			return kdptComplexRange;
		}
		return kdptNone;
	} // multiple paragraphs
}

/*----------------------------------------------------------------------------------------------
	Handle starting to edit: if we already have a string builder, do nothing, if not, set up
	the string builder variables, the object and property being edited, etc.
	If the selection spans multiple paragraphs in an StText, set up the additional variables
	we need to handle this.
	If the selection is read-only or otherwise too complex to handle, clear m_qtsbProp,
	as an error indication. Other editing variables may also be cleared.
	(Typically this case will have been dealt with by first calling
	DeleteRangeAndPrepareToInsert.)
	NOTE: hide the selection before calling this!
----------------------------------------------------------------------------------------------*/
void VwTextSelection::StartEditing()
{
	// If we are already editing, we are done!
	if (m_qtsbProp)
		return;

	m_pnoteParaOwner = NULL; // ensures not set unless this time OKs it.

	// Use fAssocPrevious to decide, if at a string boundary, whether we want the
	// previous or following string. If we have a range, we want the range direction
	// from the anchor. Otherwise, we want the value saved in a member variable
	bool fAssocPrevious = m_fAssocPrevious;
	if (m_ichEnd != m_ichAnchor || m_pvpboxEnd)
		fAssocPrevious = m_pvpboxEnd ? m_fEndBeforeAnchor : m_ichEnd < m_ichAnchor;

	Debug(VwRootBox * prootb = m_pvpbox->Root());
	Assert(prootb);

	ITsStringPtr qtssProp;

	// Look for string property at the anchor unless we are an multi-par sel,
	// in which case, look for it at whatever end point is in the last para.
	int ichAnchor = m_ichAnchor;
	VwParagraphBox * pvpbox = m_pvpbox;
	if (m_pvpboxEnd && !m_fEndBeforeAnchor)
	{
		ichAnchor = m_ichEnd;
		pvpbox = m_pvpboxEnd;
	}

	// This uses notifier information to determine the substring we want to edit
	// and what property etc. it belongs to.
	if (CallEditableSubstring(pvpbox, ichAnchor, ichAnchor, fAssocPrevious, &m_hvoEdit,
		&m_tagEdit, &m_ichMinEditProp, &m_ichLimEditProp, &m_qvvcEdit, &m_fragEdit,
		&m_qanote, &m_iprop, &m_vnp, &m_itssProp, &qtssProp) != kvepvEditable)
	{
		return;
	}

	// Now see if there is a higher-level property into which we can insert new
	// paragraph-level items. We only contemplate this if the string prop is the
	// whole contents of the paragraph, except that we extended this to allow
	// label information to be added (but not edited) at the start of the first paragraph.
	int cchProp;
	CheckHr(qtssProp->get_Length(&cchProp));
	if (m_ichLimEditProp < cchProp)
		goto LNoParaOps;
	// If we are in a multi-para selection, this is the end-point in the last para,
	// so it must have only one property. In the first para, it is good enough that
	// we are in the last string, as just checked.
	if (m_pvpboxEnd && m_ichMinEditProp > 0)
		goto LNoParaOps;
	// Also, it must be a simple string property.
	if (m_qvvcEdit || m_vnp != kvnpStringProp)
		goto LNoParaOps;
	// This will be m_pnotePara if we have a workable paragraph-level property
	// to edit. Otherwise we leave m_pnotePara null as an indication that we don't.
	VwNotifier * pnotePara;
	pnotePara = m_qanote->Parent();
	// We must have a containing property to do paragraph editing.
	if (!pnotePara)
		goto LNoParaOps;
	m_ipropPara = m_qanote->PropIndex();
	m_hvoParaOwner = pnotePara->Object();
	m_tagParaProp = pnotePara->Tags()[m_ipropPara];
	if (m_tagParaProp != kflidStText_Paragraphs)
		goto LNoParaOps; // can only do pargraph ops on StText.
	// By default these are both the same because we have only one paragraph
	m_ihvoFirstPara = m_ihvoLastPara = m_qanote->ObjectIndex();

	VwNoteProps vnp;
	vnp = pnotePara->Flags()[m_ipropPara];
	// It must be an editable sequence property.
	// We would generally like to check also that it is owning, but that is harder.
	// For now, the programmer must explicitly make any sequence refs where this might
	// otherwise be allowed uneditable.
	// ENHANCE JohnT: could we allow ObjVec also? Dangerous as the inserted para may not show,
	// or similar problems, depending on how the vector is displayed.
	if (vnp != (kvnpObjVecItems | kvnpEditable) && vnp != (kvnpLazyVecItems | kvnpEditable))
		goto LNoParaOps;

	if (m_pvpboxEnd)
	{
		// Multi-paragraph selection. Verify that there is a similarly editable string
		// in the first para.
		int ichFirst = m_ichAnchor;
		VwParagraphBox * pvpboxFirst = m_pvpbox;
		if (m_fEndBeforeAnchor)
		{
			ichFirst = m_ichEnd;
			pvpboxFirst = m_pvpboxEnd;
		}

		// Now get all the info about the property in the first paragraph.
		// These variables are analogous to the corresponding member ones.
		HVO hvoEdit;
		int tagEdit;
		int ichMinEditProp;
		int ichLimEditProp;
		IVwViewConstructorPtr qvvcEdit;
		int fragEdit;
		VwAbstractNotifierPtr qanote;
		int iprop;
		int itssProp;
		ITsStringPtr qtssPropFirst;

		if (CallEditableSubstring(pvpboxFirst, ichFirst, ichFirst, fAssocPrevious, &hvoEdit,
			&tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
			&qanote, &iprop, &vnp, &itssProp, &qtssPropFirst) != kvepvEditable)
		{
			return;
		}
		// Must be the last string in that paragaph.
		int cchPropFirst;
		CheckHr(qtssPropFirst->get_Length(&cchPropFirst));
		if (ichLimEditProp < cchPropFirst)
			return;
		// Must be the same property. ENHANCE JohnT: could we relax this?
		if (tagEdit != m_tagEdit)
			return;
		// Paragraph must belong to the same parent notifier and the same property.
		if (qanote->Parent() != pnotePara ||
			qanote->PropIndex() != m_qanote->PropIndex())
		{
			return;
		}
		// OK, we can do it. All that remains is to adjust the first para index
		m_ihvoFirstPara = qanote->ObjectIndex();

		m_pnoteParaOwner = pnotePara; // paragraph level editing is OK.
		// Make a string builder for the (first) paragraph property.
		CheckHr(qtssPropFirst->GetBldr(&m_qtsbProp));
		return;
	}

	m_pnoteParaOwner = pnotePara; // paragraph level editing is OK.

LNoParaOps:
	// Come here if we can't do create/destroy paragraph operations. Therefore we will fail
	// except for cases where we don't need to be able to.
	if (m_pvpboxEnd)
	{
		// Can't do multi-paragraph except for special case above.
		return;
	}
	// For copying and similar purposes, we allow selections larger than one property.
	// For editing we currently do not.
	if (m_ichEnd < m_ichMinEditProp)
		return;

	if (m_ichEnd > m_ichLimEditProp)
		return;

	Assert(m_ichAnchor >= m_ichMinEditProp && m_ichAnchor <= m_ichLimEditProp);

	// Now make a string builder for the property.
	CheckHr(qtssProp->GetBldr(&m_qtsbProp));

}

/*----------------------------------------------------------------------------------------------
	Shrinks the selection to the currently selected part of the string property which
	contains the anchor. This is the default way of handling typing or pasting over a selection
	too complex to automatically delete entirely.
	If the anchor is not editable but the end point is, shrink to that.
	Assumes that the selection needs shrinking...that is, it is a range that extends beyond
	the anchor property. Also assumes that the anchor property is editable.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShrinkSelection()
{
	// "Associate with previous" for this function if the selection extends backwards from
	// the anchor.
	bool fAssocPrevious = m_pvpboxEnd ? m_fEndBeforeAnchor : m_ichEnd < m_ichAnchor;
	ITsStringPtr qtssProp;
	VwEditPropVal vepv = CallEditableSubstring(m_pvpbox, m_ichAnchor, m_ichAnchor, fAssocPrevious,
		&m_hvoEdit, &m_tagEdit, &m_ichMinEditProp, &m_ichLimEditProp, &m_qvvcEdit,
		&m_fragEdit, &m_qanote, &m_iprop, &m_vnp, &m_itssProp,
		&qtssProp);
	if (vepv != kvepvEditable)
	{
		HVO hvoEdit;
		int tagEdit, ichMin, ichLim, fragEdit, iprop, itssProp;
		IVwViewConstructorPtr qvvcEdit;
		VwAbstractNotifierPtr qanote;
		VwNoteProps vnp;
		vepv = CallEditableSubstring(m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox, m_ichEnd, m_ichEnd, !fAssocPrevious,
				&hvoEdit, &tagEdit, &ichMin, &ichLim, &qvvcEdit,
				&fragEdit, &qanote, &iprop, &vnp, &itssProp,
				&qtssProp);
		if (vepv == kvepvEditable)
		{
			// Shrink to this instead.
			if (m_pvpboxEnd)
				m_pvpbox = m_pvpboxEnd;
			m_pvpboxEnd = NULL;
			m_hvoEdit = hvoEdit;
			m_tagEdit = tagEdit;
			m_ichMinEditProp = ichMin;
			m_ichLimEditProp = ichLim;
			m_qvvcEdit = qvvcEdit;
			m_fragEdit = fragEdit;
			m_qanote = qanote;
			m_iprop = iprop;
			m_vnp = vnp;
			m_itssProp = itssProp;
			m_ichAnchor = fAssocPrevious ? m_ichLimEditProp : m_ichMinEditProp;
			m_rcBounds.Clear();
			return;
		}
	}
	m_pvpboxEnd = NULL;
	m_ichEnd = fAssocPrevious ? m_ichMinEditProp : m_ichLimEditProp;
	m_qanote.Clear();
	m_rcBounds.Clear();
}

/*----------------------------------------------------------------------------------------------
	Get the properties that should currently be used to insert text at the specified position.
	The caller may pass in a candidate TsTextProps (e.g., from a range just deleted, or from
	m_qttp), or NULL to take the properties from the text at ichAnchor (typically m_ichAnchor).
	Note that the input character index is logical.

	NOTE that there is very similar code in GetSelectionProps, but I can't figure a clean
	way to separate the two, as the code here depends on StartEditing having been called.

	@param ich		Anchor position in para to return info about (presumed within current
						edit string if any).
	@param pttp		Candidate text props, or NULL for char at ichAnchor (-1 if m_fAssocPrevious
						is true)
	@param ppttp	Returns result.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::GetInsertionProps(int ichAnchor, ITsTextProps * pttp,
	ITsTextProps ** ppttp)
{
	AssertPtr(ppttp);
	AssertPtrN(pttp);
	Assert(!*ppttp);
	ITsTextPropsPtr qttp = pttp;
	if (!qttp)
	{
		int ichProps = ichAnchor;
		if (m_qtsbProp)
		{
			if (ichAnchor > m_ichMinEditProp && m_fAssocPrevious)
				ichProps--;
		}
		else
		{
			if (ichAnchor > 0 && m_fAssocPrevious)
				ichProps--;
		}

		OLECHAR ch;
		m_pvpbox->Source()->CharAndPropsAt(ichProps, &ch, &qttp);
		if (!qttp)
		{
			// We did the best we could to get the props, but we couldn't get any.
			// Just return a null props since we can't do any other processing.
			*ppttp = NULL;
			return;
		}
		if (ch == kchObject)
		{
			SmartBstr sbstr;
			CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
			if (sbstr.Length())
			{
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(ktptObjData, NULL));
				CheckHr(qtpb->GetTextProps(&qttp));
			}
		}
	}
	// If the properties include a picture, delete it: a picture must stand alone
	// in a run by itself.
	SmartBstr sbstrObjData;
	CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrObjData));
	if (sbstrObjData.Length())
	{
		const OLECHAR * pch = sbstrObjData.Chars();
		if (pch[0] == kodtPictEvenHot || pch[0] == kodtPictOddHot)
		{
			ITsPropsBldrPtr qtpb;
			CheckHr(qttp->GetBldr(&qtpb));
			CheckHr(qtpb->SetStrPropValue(ktptObjData, NULL));
			CheckHr(qtpb->GetTextProps(&qttp));
		}
	}
	// If the properties request the unknown writing system, see if the current property
	// store specifies a value for base writing system.
	int ws, var;
	CheckHr(qttp->GetIntPropValues(ktptWs, &var, &ws));
	if (!ws)
	{
		VwNotifier * pnote = dynamic_cast<VwNotifier *>(m_qanote.Ptr());
		if (pnote && pnote->CProps() > m_iprop)
		{
			// We're editing a valid property and can retrieve this information
			VwPropertyStore * pvps = pnote->Styles()[m_iprop];
			if (pvps) // JohnT: I don't remember if this can ever not be there, but play safe.
				ws = pvps->DefaultWritingSystem();
			if (ws)
			{
				// Got one! use it.
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws));
				CheckHr(qtpb->GetTextProps(&qttp));
			}
		}
	}
	*ppttp = qttp.Detach();
}

/*----------------------------------------------------------------------------------------------
	This class manages the implementation of the OnTyping method.
----------------------------------------------------------------------------------------------*/
class OnTypingMethod
{
public:
	IVwGraphics * m_pvg;
#ifdef DEBUG
	StrUni m_stuInput;
#endif
	StrUni m_stuNFD;			// Holds NFD normalized form of original input.
	const wchar * m_pchInput;
	int m_cchInput;
	int m_cchBackspace;
	int m_cchDelForward;
	wchar m_chFirst;
	int * m_pwsPending;
	VwRootBox * m_prootb;
	ISilDataAccessPtr m_qsda;
	VwTextSelectionPtr m_qsel;
	bool m_fSelectionHidden;
	int m_ichAnchor;
	int m_ichEnd;
	VwSelChangeType m_nHowChanged;
	VwShiftStatus m_ss;
	ILgCharacterPropertyEnginePtr m_qcpe; // default ICU CPE; use only where wordforming overrides are not important.

	OnTypingMethod(VwTextSelection * psel, IVwGraphics * pvg, const wchar * pchInput,
		int cchInput, VwShiftStatus ss, int * pwsPending)
	{
		m_qsel = psel;
		m_pvg = pvg;
		m_ss = ss;
		m_prootb = m_qsel->m_pvpbox->Root();
		Assert(m_prootb);
		if (cchInput > 1)
		{
			for (const wchar * pch = pchInput; pch < pchInput + cchInput; pch++)
				if (*pch == '\x8' || *pch == '\r' || *pch == '\x7f')
					ThrowHr(WarnHr(E_UNEXPECTED), L"OnTyping string may not include backspace, newline, or return unless it is the only character");
		}
		// Normalize the Unicode input character(s) to NFD.
#ifdef DEBUG
		m_stuInput.Assign(pchInput, cchInput);
#endif
		if (*pchInput == '\x8' || *pchInput == '\x7f')
			m_stuNFD.Assign(pchInput + 1, cchInput - 1); // don't include bs or del.
		else
			m_stuNFD.Assign(pchInput, cchInput);
		ComBool fCompositionInProgress;
		CheckHr(m_prootb->get_IsCompositionInProgress(&fCompositionInProgress));
		if (!fCompositionInProgress)
			StrUtil::NormalizeStrUni(m_stuNFD, UNORM_NFD);
		//if (cchInput != m_stuNFD.Length())
		//{
		//		StrUni stuMsg;
		//		stuMsg.Format(L"normalized %d chars to %d\n", cchInput,  m_stuNFD.Length());
		//			::OutputDebugStringW(stuMsg.Chars());
		//}

		m_pchInput = m_stuNFD.Chars();
		m_cchInput = m_stuNFD.Length();
		m_cchBackspace = (*pchInput == '\x8' ? 1 : 0);
		m_cchDelForward = (*pchInput == '\x7f' ? 1 : 0);
		m_chFirst = pchInput[0]; // don't use m_pchInput; has BS and DEL removed. Want original first.
		m_pwsPending = pwsPending;
		m_fSelectionHidden = false;
		m_qcpe.CreateInstance(CLSID_LgIcuCharPropEngine);
	}

	~OnTypingMethod()
	{
		if (m_fSelectionHidden)
			m_prootb->ShowSelectionAfterEdit();
	}

	// Call OnProblemDeleting when in backspace or delete loop.
	// If it returns true, the calling method should abort, doing no more work: either
	// there is nothing more to do, or some other selection has done it, or the
	// rootsite OnProblemDeletion call returned kdprAbort.
	bool CallOnProblemDeleting(VwDelProbType dpt, int & cbspOrDel)
	{
		// If we can't complete writing any current changes to the cache, stop.
		// (This also ensures we don't somehow later try to commit after moving the selection.)
		bool fOk;
		m_qsel->UnprotectedCommit(&fOk);
		if (!fOk)
			return true;
		VwDelProbResponse dpr;
		bool fWasWorking = m_qsel->m_csCommitState == kcsWorking;
		m_qsel->m_csCommitState = kcsNormal; // further commit allowed during OnProblemDeletion
		HRESULT hr;
		IgnoreHr(hr = m_prootb->Site()->OnProblemDeletion(m_qsel, dpt, &dpr));
		if (fWasWorking)
			m_qsel->m_csCommitState = kcsWorking;
		if (FAILED(hr))
		{
			if (hr == E_NOTIMPL || hr == E_FAIL)
			{
				// Default behavior is to give up on the kind of deleting we're doing, but
				// carry on with other edits that may be possible.
				cbspOrDel = 0;
				m_qsel->StartEditing();
				return false;
			}
			else {
				ThrowHr(hr);
			}
		}
		if (dpr == kdprAbort)
			return true;
		else if (dpr == kdprDone)
			cbspOrDel--;
		else if (dpr == kdprFail)
			cbspOrDel = 0; // give up on this kind of deletion.
		// If our selection is still current, let it go on with whatever remains to be done.
		if (m_prootb->Selection() == m_qsel.Ptr())
		{
			m_qsel->StartEditing();
			return false;
		}
		// Selection changed. Let it handle the rest, if any.
		if (m_cchBackspace || m_cchDelForward || m_cchInput)
		{
			if (cbspOrDel == 0)
			{
				// If we used up all the deletes of this type (or aborted it), any new call
				// to OnTyping needs to see a different 'first char typed'. If there are
				// still deletes to do (there can't be any backspaces left, because we do
				// them first), simulate a del; otherwise, use the first character.
				// (There must be one, because we don't come here if all counts are zero.)
				if (m_cchDelForward)
					m_chFirst = 0x7f;
				else
					m_chFirst = *m_pchInput;
			}
			m_prootb->Selection()->OnTyping(m_pvg, m_pchInput, m_cchInput, m_ss, m_pwsPending);
		}
		// If we get here, there is a different selection, but we've already handled all the
		// changes.
		return true;
	}

	bool OnlyControlCharacters()
	{
		const OLECHAR * pch = m_pchInput;
		for (; pch < m_pchInput + m_cchInput; pch++)
		{
			if (*pch >= 32 || *pch == '\r')
				break;
		}
		if (pch >= m_pchInput + m_cchInput && (!m_cchBackspace) && (!m_cchDelForward))
			return true;

		return false;
	}

	void HideSelection()
	{
		// Hide the current selection (so there is less flashing if we change it several times).
		// All exit paths from this method should call m_prootb->ShowSelectionAfterEdit();
		// to reverse the effects of this.
		m_qsel->Hide();
		m_prootb->HandleActivate(vssDisabled); // Prevent cursor flashing from turning on again.
		m_fSelectionHidden = true;
	}

	// The following code handles Control-Backspace and Control-Delete in the case where
	// there was not already a selection range.
	void HandleControlBackspaceDelete()
	{
		if (m_ss != kfssControl)
			return; // no Control-Backspace/Delete - nothing to do

		OLECHAR startCh;
		OLECHAR endCh;

		VwTxtSrc * pts = m_qsel->m_pvpbox->Source();
		AssertPtr(pts);
		int cchTemp = pts->Cch();
		OLECHAR *rgch = new OLECHAR[ cchTemp + 1];
		pts->FetchLog(0, pts->Cch(), rgch);
		ILgCharacterPropertyEnginePtr qcpeStart;
		ITsTextPropsPtr qttpStart;
		m_qsel->m_pvpbox->Source()->CharAndPropsAt(m_qsel->m_ichEnd, &startCh, &qttpStart);
		GetCpeFromRootAndProps(m_prootb, qttpStart, &qcpeStart);

		if (m_cchBackspace == 1)
		{
			// Find the next space or punct searching backwards (ie, left.. unless using
			//	right-to-left text, then it will go right; if vertical (or mirrored) text
			// you are in trouble.) If you are unsure, just trust us.
			int ichEnd = m_qsel->FindWordBoundary(m_qsel->m_ichEnd, m_qsel->m_pvpbox, m_pvg, false);

			if (ichEnd != m_qsel->m_ichEnd )
			{
				delete [] rgch;
				rgch = NULL;

				// Possibly delete one more space, if space is found both before and after the
				// range we are about to delete, or there is nothing after and one is found before.
				// Also if punct is found after.
				// We already got the character at m_qsel->m_ichEnd, which is the one after.
				if (ichEnd > 0 && (m_qsel->m_ichEnd >= cchTemp || GetCharacterType(qcpeStart, startCh) != kAlpha))
				{
					// Got a space after...and there is a character before...
					ILgCharacterPropertyEnginePtr qcpeEnd;
					ITsTextPropsPtr qttpEnd;
					m_qsel->m_pvpbox->Source()->CharAndPropsAt(ichEnd - 1, &endCh, &qttpEnd);
					GetCpeFromRootAndProps(m_prootb, qttpEnd, &qcpeEnd);
					if(GetCharacterType(qcpeEnd, endCh) == kSpace)
					{
						--ichEnd; // delete the preceding space
					}
				}

				if (!m_qsel->CheckCommit(ichEnd, m_qsel->m_pvpbox))
				{
					m_prootb->ShowSelectionAfterEdit();
					return;
				}
				m_qsel->m_ichEnd = ichEnd;
				m_qsel->m_fEndBeforeAnchor = true;
				// Go on and delete the newly established selection.
				m_cchInput = 0;
				m_cchBackspace = 1;
				m_cchDelForward = 0;
				m_chFirst = kscBackspace;
			}
		}
		else if (m_cchDelForward == 1)
		{
			// Find the next space or punct searching backwards (ie, left.. unless using
			// right-to-left text, then it will go right; if vertical (or mirrored) text
			// you are in trouble.) If you are unsure, just trust us.
			int ichEnd = m_qsel->FindWordBoundary(m_qsel->m_ichEnd, m_qsel->m_pvpbox, m_pvg, true);

			if (ichEnd != m_qsel->m_ichEnd )
			{
				delete [] rgch;
				rgch = NULL;

				if (ichEnd < m_qsel->m_pvpbox->Source()->Cch())
				{
					// Consider deleting one more space. Only if a space does in fact follow the range we intend to delete.
					ILgCharacterPropertyEnginePtr qcpeFollow;
					ITsTextPropsPtr qttpFollow;
					OLECHAR chFollow;
					m_qsel->m_pvpbox->Source()->CharAndPropsAt(ichEnd, &chFollow, &qttpFollow);
					GetCpeFromRootAndProps(m_prootb, qttpFollow, &qcpeFollow);
					if (GetCharacterType(qcpeFollow, chFollow) == kSpace)
					{
						// OK, conceivably we want to delete the space following the word.
						// But only if the bit we're deleting is at the start of the paragraph or preceded by space;
						// otherwise, we want to keep a space between the remaining part of the word and what follows.
						if( m_qsel->m_ichEnd == 0 )
							++ichEnd;
						else
						{
							ILgCharacterPropertyEnginePtr qcpeEnd;
							ITsTextPropsPtr qttpEnd;
							m_qsel->m_pvpbox->Source()->CharAndPropsAt(m_qsel->m_ichEnd - 1, &endCh, &qttpEnd);
							GetCpeFromRootAndProps(m_prootb, qttpEnd, &qcpeEnd);
							if( m_qsel->m_ichEnd > 0 && GetCharacterType(qcpeStart, startCh) == kAlpha &&
								GetCharacterType(qcpeEnd, endCh) == kSpace)
							{
								++ichEnd;
							}
						}
					}
				}

				if (!m_qsel->CheckCommit(ichEnd, m_qsel->m_pvpbox))
				{
					m_prootb->ShowSelectionAfterEdit();
					return;
				}
				m_qsel->m_ichEnd = ichEnd;
				m_qsel->m_fEndBeforeAnchor = true;
				// Go on and delete the newly established selection.
				m_cchInput = 0;
				m_cchBackspace = 0;
				m_cchDelForward = 1;
				m_chFirst = kscDelForward;
			}
		}
		if( rgch )
			delete [] rgch;
	}

	enum TypingAction
	{
		// Continue processing rest of typing
		kTypingContinue,
		// Return - everything's already processes, rest of input should be discarded
		kTypingReturn,
		// We need to commit, can't discard rest of input
		kTypingCommit
	};


	/************************************** HandleBackspace ()*********************************/
	TypingAction HandleBackspace(ITsTextPropsPtr & qttp)
	{
		int cchProp; // the number of characters currently in the string builder for the prop.
		CheckHr(m_qsel->m_qtsbProp->get_Length(&cchProp));

		while (m_cchBackspace)
		{
			// The first half of the code of this loop handles as many of the backspaces
			// as possible by deleting from the current string. The second half commits
			// changes to this string and moves to the previous one if there are left-over
			// backspaces.
			// We use a fairly complex approach based on valid insertion positions to
			// figure how many characters to delete for each backspace. At the end of
			// the first stage (deleting from the current string),
			// m_cchBackspace is decremented by the number of backspaces we've been able to
			// process in the current string; ichMin..ichLim is the actual range to delete.
			Assert(m_ichEnd == m_ichAnchor);
			// NOTE: This is even messier than you think because of the possible presence of
			// surrogate pairs.  AND even worse than that because of the possible presence
			// of diacritic marks (at least for the Uniscribe renderer).
			// We would like the new IP associated with the properties of the last
			// character backspaced over.

			// This is the limit relative to the current string of what we will delete.
			// It is the current IP position relative to the string.
			int ichLim = m_ichAnchor - m_qsel->m_ichMinEditProp;
			int ichMin = ichLim;
			// Loop while there are more backspaces to process and there is more to
			// delete in the current string, one iteration figures out how much text
			// is deleted by one backspace.
			while (ichMin > 0 && m_cchBackspace > 0)
			{
				m_cchBackspace--;
#if 0
// This behavior (deleting a complete base + diacritic combination) is not wanted.
				// Init of this loop ensures the backspace deletes at least one
				// character. We keep having it delete more until we find a valid
				// position.
				for (ichMin--; ichMin > 0; --ichMin)
				{
					// Let the relevant segment decide whether ichMin is a valid insertion
					// point position. One backspace deletes back to the previous valid IP
					// position. This ensures (if the renderer is implemented correctly)
					// that we won't delete just one half of a surrogate pair; typically
					// it also deletes a complete base+diacritic combination.
					LgIpValidResult ipvr = kipvrUnknown;
					int ichRen = m_qsel->m_pvpbox->Source()->LogToRen(ichMin + m_qsel->m_ichMinEditProp);
					for (VwBox * pbox = m_qsel->m_pvpbox->FirstBox();
						pbox && ipvr != kipvrOK;
						pbox = pbox->NextOrLazy())
					{
						VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
						if (psbox)
						{
							CheckHr(psbox->Segment()->
								IsValidInsertionPoint(psbox->IchMin(), m_pvg, ichRen, &ipvr));
						}
						else
						{
							// Other kinds of box can't rule on validity...leave it unknown.
						}
					}
					if (ipvr == kipvrOK)
						break;
				}
#else
				// We want to delete a single character (which however might be a surrogate pair).
				// This will usually (in the absence of funny keyboard tricks) delete one thing
				// the user typed, which is usually what is desired for backspace (cf del forward,
				// which deletes a character plus diacritic).
				ichMin--; // delete at least one.
				OLECHAR ch;
				CheckHr(m_qsel->m_qtsbProp->FetchChars(ichMin, ichMin + 1, &ch));
				// If it is a low (second of pair) surrogate, delete an extra character, presumed to be the
				// corresponding high surrogate.
				if (ichMin > 0 && IsLowSurrogate(ch))
					ichMin--;
#endif
			}
			if (ichMin < ichLim)
			{
				// There is something to delete in this string.
				CheckHr(m_qsel->m_qtsbProp->get_PropertiesAt(ichMin, &qttp));
				// If the last "character" backspaced over was a picture, remove the
				// picture property, keeping only the writing system.
				SmartBstr sbstrObjData;
				CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrObjData));
				if (sbstrObjData.Length())
				{
					const OLECHAR * pch = sbstrObjData.Chars();
					if (pch[0] == kodtPictEvenHot || pch[0] == kodtPictOddHot)
					{
						ITsPropsBldrPtr qtpb;
						CheckHr(qttp->GetBldr(&qtpb));
						CheckHr(qtpb->SetStrPropValue(ktptObjData, NULL));
						CheckHr(qtpb->GetTextProps(&qttp));
					}
				}
				CheckHr(m_qsel->m_qtsbProp->ReplaceRgch(ichMin, ichLim, NULL, 0, qttp));
			}
			int cchBackPhys = ichLim - ichMin;
			m_ichAnchor -= cchBackPhys;
			m_ichEnd -= cchBackPhys;
			cchProp -= cchBackPhys;
			if (m_cchBackspace)
			{
				// We've deleted all we can in the current edit property. Commit any changes to that property.
				int tagEdit = m_qsel->m_tagEdit; // in case Commit clears it.
				bool fOk;
				m_qsel->UnprotectedCommit(&fOk);
				if (!fOk)
					return kTypingCommit; // we needed to commit and can't discard rest of input.
				if (m_qsel->m_ichMinEditProp)
				{
					// There's more text in this paragraph, but in a different property.
					// Move to editing that, then delete there.
					// Force the selection to associate with the previous prop
					m_qsel->m_fAssocPrevious = true;
					m_qsel->StartEditing();
					if ((!m_qsel->m_qtsbProp) ||
						m_qsel->m_ichAnchor == m_qsel->m_ichMinEditProp)
					{
						m_qsel->m_fAssocPrevious = false;

						// We could not edit in the previous prop. Give up unless site
						// handles it.
						if (CallOnProblemDeleting(kdptBsReadOnly, m_cchBackspace))
							return kTypingReturn;
						else
							continue; // try again if any backspaces remain to deal with.
					}
					// All is well, continue loop to deal with remaining backspaces
				}
				else
				{
					// Extra backspaces at start of para. Try to do para-level delete.
					// If such editing is disabled or this is the first para, give up
					if ((!m_qsel->m_pnoteParaOwner) || (!m_qsel->m_ihvoFirstPara))
					{
						// Give up unless site handles it.
						if(CallOnProblemDeleting(kdptBsAtStartPara, m_cchBackspace))
							return kTypingReturn;
						else
							continue; // try again if any backspaces remain to deal with.
					}

					VwParagraphBox * pvpboxOri = dynamic_cast<VwParagraphBox*>(m_qsel->m_pvpbox);
					VwGroupBox * pboxCont = pvpboxOri->Container();
					VwParagraphBox * pvpboxPrev =
						dynamic_cast<VwParagraphBox*>(pboxCont->RealBoxBefore(pvpboxOri));
					if (!pvpboxPrev)
					{
						// If the previous box isn't a paragraph box, then it is either something
						// else (e.g. a picture), or we're the first paragraph in a table cell.
						// If it's a table cell, then there are multiple possibilities, what
						// a backspace at the beginning of a paragraph could mean: ignore,
						// merge with paragraph of previous cell in same row, or merge with paragraph
						// of previous cell in same column. We can't decide this here, so we
						// let the rootsite deal with that...
						if (dynamic_cast<VwTableCellBox *>(pboxCont))
						{
							if(CallOnProblemDeleting(kdptBsAtStartPara, m_cchBackspace))
								return kTypingReturn;
							else
								continue; // process any remaining backspaces.
						}
					}

					// This both finds the previous paragraph and expands it if we are being
					// lazy.
					VwNotifier * pnotePrev = m_qsel->m_pnoteParaOwner->FindChild(
						m_qsel->m_ipropPara, m_qsel->m_ihvoFirstPara - 1, 0);
					if (!pnotePrev)
						return kTypingContinue;
					VwParagraphBox * pboxPrev = pnotePrev->FirstBox()->GetOnlyContainedPara();
					if (!pboxPrev)
						return kTypingContinue;
					int cchPrev = pboxPrev->Source()->Cch();
					// Before we change anything that might destroy our properties, set up the post-UOW selection request.
					m_qsel->RequestSelectionAfterUow(m_qsel->m_ihvoFirstPara - 1, cchPrev, cchPrev != 0);
					if (cchPrev)
					{
						// Delete this paragraph (want to keep props of previous one, since there is text in it.)
						HVO hvoPrev = pnotePrev->Object();
						int cchThis = pvpboxOri->Source()->Cch();
						HVO hvoDel; // the paragraph we are going to delete.
						CheckHr(m_qsda->get_VecItem(m_qsel->m_hvoParaOwner,
							m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara, &hvoDel));
						// First, move any text in this paragraph into the previous one.
						CheckHr(m_qsda->MoveString(hvoDel, tagEdit, 0, 0, cchThis, hvoPrev, tagEdit, 0, cchPrev, false));
						// Then do the actual deletion.
						CheckHr(m_qsda->DeleteObjOwner(m_qsel->m_hvoParaOwner, hvoDel,
							m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara));
					}
					else
					{
						// Delete previous (empty) paragraph, and keep (props of) this one.
						HVO hvoDel;
						CheckHr(m_qsda->get_VecItem(m_qsel->m_hvoParaOwner,
							m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara - 1, &hvoDel));
						CheckHr(m_qsda->DeleteObjOwner(m_qsel->m_hvoParaOwner, hvoDel,
							m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara - 1));
					}
					// That really messed us up! The whole property which contained both
					// our paragraphs was replaced, which means both our paragraphs no
					// longer exist.  Moreover we may no longer be the current selection.
					// On top of that, we may have deleted the object which was our hvoEdit.
					// That's why we requested a new selection at the end of the UOW.
					// Old versions of this code tried to repair things, but we can't.
					// So, make SURE we're not in use any more, and exit.
					m_prootb->DestroySelection(); // no need to notify root site, will hear about the new sel
					return kTypingReturn; // We only handle one backspace at a time, there should be no more to do, but make sure.
				}
			}
		}
		return kTypingContinue;
	}

	//*******************************************************************************************
	// Calculate how many characters we have to delete.
	// This is even messier than you think because of the possible presence of
	// surrogate pairs.  AND even worse than that because of the possible presence of diacritic
	// marks.
	// We also have to check for pictures...
	//
	// @param qttp
	// @param cchProp - the number of characters currently in the string builder for the prop.
	// @param cchDelHere - the number of (logical) characters to delete
	// @returns The number of physical code points to delete
	//*******************************************************************************************
	int DetermineCharsToDelete(ITsTextPropsPtr & qttp, int cchProp, int & cchDelHere)
	{
		if (!cchDelHere)
			return 0;

		int cchDelPhys = cchDelHere;
		// We would like the new IP associated with the properties of the last
		// character deleted, if any.  Unless it's a picture...
		int ichMin = m_ichAnchor - m_qsel->m_ichMinEditProp;
		int ichLim = ichMin + cchDelHere;
		int ich;
		int cchHighSurr = 0;
		int cchMark = 0;
		SmartBstr sbstrT;
		CheckHr(m_qsel->m_qtsbProp->get_Text(&sbstrT));
		Assert(ichMin >= 0);
		Assert(ichLim <= sbstrT.Length());
		Assert(cchProp == sbstrT.Length());
		const OLECHAR * prgch = sbstrT.Chars();
		for (ich = ichMin; ich <= ichLim && ichLim <= cchProp; ++ich)
		{
			UChar32 uch32;
			if (IsHighSurrogate(prgch[ich]))
			{
				VERIFY(FromSurrogate(prgch[ich], prgch[ich+1], (uint *)&uch32));

				++cchHighSurr;
				if (ichLim < cchProp)
				{
					++ich;		// No need to go through loop for 2nd char of pair.
					++ichLim;
				}
				else
					--cchDelHere;
			}
			else
				uch32 = (unsigned)prgch[ich];

			LgGeneralCharCategory gcc;
			CheckHr(m_qcpe->get_GeneralCategory(uch32, &gcc));
			if (gcc >= kccMn && gcc <= kccMe && ich > ichMin)
			{
				++cchMark;
				if (ichLim < cchProp)
					++ichLim;
			}
		}
		if (cchHighSurr + cchMark)
			cchDelPhys = ichLim - ichMin;

		CheckHr(m_qsel->m_qtsbProp->get_PropertiesAt(ichLim - 1, &qttp));
		// If the last "character" deleted was a picture, remove the picture
		// property, keeping only the writing system.
		SmartBstr sbstrObjData;
		CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrObjData));
		if (sbstrObjData.Length())
		{
			const OLECHAR * pch = sbstrObjData.Chars();
			if (pch[0] == kodtPictEvenHot || pch[0] == kodtPictOddHot)
			{
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(ktptObjData, NULL));
				CheckHr(qtpb->GetTextProps(&qttp));
			}
		}
		CheckHr(m_qsel->m_qtsbProp->ReplaceRgch(ichMin, ichLim, NULL, 0, qttp));
		return cchDelPhys;
	}

	/************************************** HandleDelForward ()********************************/
	TypingAction HandleDelForward(ITsTextPropsPtr & qttp)
	{
		int cchProp; // the number of characters currently in the string builder for the prop.
		CheckHr(m_qsel->m_qtsbProp->get_Length(&cchProp));

		// NOTE: This depends only on the Unicode general category, not on the language.
		while (m_cchDelForward)
		{
			int cchDelHere = m_cchDelForward;
			// If we're trying to delete more than this property has, on this
			// iteration just destroy up to the end of the property.
			Assert(m_ichEnd == m_ichAnchor);
			if (cchDelHere > cchProp - (m_ichAnchor - m_qsel->m_ichMinEditProp))
				cchDelHere = cchProp - (m_ichAnchor - m_qsel->m_ichMinEditProp);

			int cchDelPhys = DetermineCharsToDelete(qttp, cchProp, cchDelHere);

			// Note that delete forward does not move the IP.
			m_cchDelForward -= cchDelHere;
			cchProp -= cchDelPhys;
			if (m_cchDelForward)
			{
				// We can't delete (any more) in current property, see what else we can do.
				// First save the current edit property.
				int tagEdit = m_qsel->m_tagEdit;
				int hvoEdit = m_qsel->m_hvoEdit;
				// commit any changes.
				bool fOk;
				m_qsel->UnprotectedCommit(&fOk);
				if (!fOk)
					return kTypingCommit; // we needed to commit and can't discard rest of input.
				// Is there another property in the same para?
				if (m_qsel->m_itssProp < m_qsel->m_pvpbox->Source()->CStrings() - 1)
				{
					// Now force the selection to associate with the next prop
					m_qsel->m_fAssocPrevious = false;
					m_qsel->StartEditing();
					if ((!m_qsel->m_qtsbProp) ||
						m_qsel->m_ichAnchor == m_qsel->m_ichLimEditProp)
					{
						// We could not edit in the next prop. Give up unless site handles
						// it.
						if(CallOnProblemDeleting(kdptDelReadOnly, m_cchDelForward))
							return kTypingReturn;
						else
							continue; // process any remaining dels.
					}
					// All is well, continue loop to deal with remaining deletes
				}
				else
				{
					// Extra deletes at end of para. Try to do para-level delete.
					// If such editing is disabled or this is the last para, give up.
					if (!m_qsel->m_pnoteParaOwner)
					{
						if(CallOnProblemDeleting(kdptDelAtEndPara, m_cchDelForward))
							return kTypingReturn;
						else
							continue; // process any remaining dels.
					}
					m_nHowChanged = ksctDiffPara;
					ITsStringPtr qtssT;
					int chvoPara;
					CheckHr(m_qsda->get_VecSize(m_qsel->m_hvoParaOwner,
						m_qsel->m_tagParaProp, &chvoPara));
					if (m_qsel->m_ihvoFirstPara >= chvoPara - 1)
					{
						// It's the last paragraph of the StText...this is a problem!
						if(CallOnProblemDeleting(kdptDelAtEndPara, m_cchDelForward))
							return kTypingReturn;
						else
							continue; // process any remaining dels.
					}
					VwParagraphBox * pvpboxOri = dynamic_cast<VwParagraphBox*>(m_qsel->m_pvpbox);
					VwParagraphBox * pvpboxNext =
						dynamic_cast<VwParagraphBox*>(pvpboxOri->NextRealBox());
					if (!pvpboxNext)
					{
						VwGroupBox * pboxCont = m_qsel->m_pvpbox->Container();

						// If the next box isn't a paragraph box, then it is either something
						// else (e.g. a picture), or we're the last paragraph in a table cell.
						// If it's a table cell, then there are multiple possibilities, what
						// a delete at the end of the last paragraph could mean: ignore,
						// merge with paragraph of next cell in same row, or merge with paragraph
						// of next cell in same column. We can't decide this here, so we
						// let the rootsite deal with that...
						if (dynamic_cast<VwTableCellBox *>(pboxCont))
						{
							if(CallOnProblemDeleting(kdptDelAtEndPara, m_cchDelForward))
								return kTypingReturn;
							else
								continue; // process any remaining dels.
						}

						// Otherwise we have to look go up in the hierarchy and look for a
						// paragraph box in one of it's child boxes.
						while (pboxCont && (!pvpboxNext || pvpboxNext == pvpboxOri))
						{
							if (pboxCont->NextRealBox())
								pvpboxNext = pboxCont->NextRealBox()->GetOnlyContainedPara();
							pboxCont = pboxCont->Container();
						}
					}
					if ((!pvpboxNext) || pvpboxNext->Source()->CStrings() != 1)
						return kTypingContinue; // Not sure how this could happen, but play safe.
					// ENHANCE JohnT: should we verify next para belongs to different
					// object?
					// Need to if we allow one object in seq to generate multiple paras...
					// But for now we only do multi-para editing on StText, which doesn't
					// have the problem...
					pvpboxNext->Source()->StringAtIndex(0, &qtssT);
					int cch = m_qsel->m_pvpbox->Source()->Cch();
					m_qsel->RequestSelectionAfterUow(m_qsel->m_ihvoFirstPara, cch, false);
					if (cch)
					{
						// Delete next paragraph (want to keep props of this one)
						HVO hvoDel;
						CheckHr(m_qsda->get_VecItem(m_qsel->m_hvoParaOwner,
							m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara + 1, &hvoDel));
						// First move the text, if any.
						int cchNext;
						CheckHr(qtssT->get_Length(&cchNext));
						if (cchNext)
							CheckHr(m_qsda->MoveString(hvoDel, tagEdit, 0, 0, cchNext, hvoEdit, tagEdit, 0, cch, false));

						CheckHr(m_qsda->DeleteObjOwner(m_qsel->m_hvoParaOwner, hvoDel,
							m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara + 1));
					}
					else
					{
						// Delete this (empty) paragraph, keep props of next one.
						HVO hvoDel;
						CheckHr(m_qsda->get_VecItem(m_qsel->m_hvoParaOwner,
							m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara, &hvoDel));
						CheckHr(m_qsda->DeleteObjOwner(m_qsel->m_hvoParaOwner, hvoDel,
							m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara));
					}
					// We've changed the paragraph sequence. Rather than trying to clean up (earlier versions which
					// did this can be found in version control), just ensure we stop here. Currently we only handle
					// one delete per call.
					m_prootb->DestroySelection();
					return kTypingReturn;
				}
			}
			// make sure it is current, it is important for Del
			CheckHr(m_qsel->m_qtsbProp->get_Length(&cchProp));
		}

		return kTypingContinue;
	}

	/************************************** HandleOtherInput ()*************************************/
	TypingAction HandleOtherInput(ITsTextPropsPtr & qttp, int cchPropOrig)
	{
		int cchProp; // the number of characters currently in the string builder for the prop.
		CheckHr(m_qsel->m_qtsbProp->get_Length(&cchProp));
#if WANTPORT // IVwOleDbDa has been removed, so what should be done here?
		// Check to see if the record has been edited by someone else.
		// TODO 1724 (PaulP):  This check needs to go in here somewhere, but I'm not sure
		// where and also what to do if the user says "No" and the method returns.
		IVwOleDbDaPtr qodde;
		HRESULT hrDbCache = E_FAIL;
		//ISilDataAccess * psdaTemp = m_qsel->m_pvpbox->Root()->GetDataAccess();
		hrDbCache = m_qsda->QueryInterface(IID_IVwOleDbDa, (void **) &qodde);
		if ((hrDbCache == S_OK) && m_qsel->m_hvoParaOwner)
		{
			HRESULT hrTemp = E_FAIL;
			// This seems kludgy, but CheckTimeStamp may pop up a dialog which causes
			// us to lose focus. Part of LoseFocus clears m_qtsbProp. So if the user
			// responds Yes to the dialog, m_qtsbProp is cleared and we get a crash
			// below when we use it. So prior to calling CheckTimeStamp we'll save a
			// pointer to m_qtsbProp and restore it afterwards if it ended up being
			// cleared.
			ITsStrBldrPtr qtsbT = m_qsel->m_qtsbProp;
			if ((hrTemp = qodde->CheckTimeStamp(m_qsel->m_hvoParaOwner)) != S_OK)
			{
				return kTypingReturn;
			}
			if (!m_qsel->m_qtsbProp)
				m_qsel->m_qtsbProp = qtsbT;
		}
#endif // WIN32

		// Text was typed.
		ITsTextPropsPtr qttpT = qttp;
		m_qsel->GetInsertionProps(m_ichAnchor, qttpT, &qttp);
		// Ensure that an empty set of properties changes to using the default writing
		// system.  Note: ideally some such logic as this should be part of
		// GetInsertionProps, but other callers are unable to supply a meaningful
		// m_pwsPending.
		if (qttp)
		{
			int ctip;
			int ctsp;
			CheckHr(qttp->get_IntPropCount(&ctip));
			CheckHr(qttp->get_StrPropCount(&ctsp));
			if (ctip + ctsp == 0)
				qttp.Clear();
		}
		// If we don't have a text props, or if the user is overriding the writing system,
		// make a new one.
		if (!qttp || (*m_pwsPending != -1))
		{
			int ws;
			// Enhance KenZ(SteveMc): figure out how to get the appropriate default writing
			// system.
			// Note JohnT: is there any way we can not have a qttp by this point? Every
			// string should know some writing system and ows...
			if (*m_pwsPending == -1)
			{
				VwRootBox * prootb = m_qsel->m_pvpbox->Root();
				ISilDataAccessPtr qsda;
				CheckHr(prootb->get_DataAccess(&qsda));
				ILgWritingSystemFactoryPtr qwsf;
				CheckHr(qsda->get_WritingSystemFactory(&qwsf));
				CheckHr(qwsf->get_UserWs(&ws));
			}
			else
				ws = *m_pwsPending;
			ITsPropsBldrPtr qtpb;
			if (qttp)
				qttp->GetBldr(&qtpb);
			else
				qtpb.CreateInstance(CLSID_TsPropsBldr);
			CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws));
			CheckHr(qtpb->GetTextProps(&qttp));
		}

		// Do the actual insertion.
		// get field IDs provided by IStructuredTextDataAccess
		int notUsed_Contents;
		int stParaTags_StyleRules;
		int notUsed_Paragraphs;
		GetTags(m_qsel->RootBox()->GetDataAccess(), &notUsed_Paragraphs, &stParaTags_StyleRules, &notUsed_Contents);
		while (m_cchInput)
		{
			const OLECHAR * pch;
			// Insert all the ordinary characters.  We ignore control characters. Return is
			// handled below.
			// ENHANCE (version 2 or later): when paragraph layout handles tab chars as
			// data, allow them to be inserted as ordinary data.
			for (pch = m_pchInput; pch < m_pchInput + m_cchInput && *pch >= 32; pch++)
				;
			int cchIns = pch - m_pchInput;
			if (cchIns)
			{
				CheckHr(m_qsel->m_qtsbProp->ReplaceRgch(
					m_ichAnchor - m_qsel->m_ichMinEditProp,
					m_ichAnchor - m_qsel->m_ichMinEditProp, m_pchInput, cchIns, qttp));
				m_ichAnchor += cchIns;
				m_ichEnd += cchIns;
				m_cchInput -= cchIns;
				m_pchInput += cchIns;
				cchProp += cchIns;
			}
			for (; pch < m_pchInput + m_cchInput && *pch == '\r'; pch++)
				;
			cchIns = pch - m_pchInput;
			// If we got cr's and if we have a property where we can edit at para level,
			// insert paragraphs. Otherwise just ignore them.
			if (cchIns)
			{
				// We want to insert cchIns returns, currently the count is always one.
				if (!m_qsel->m_pnoteParaOwner)
					return kTypingReturn; // don't have any way to do it, give up.
				// Insert this number of returns, i.e., new paragraphs. We don't actually allow more than one currently.
				// First commit our changes, after saving a couple of values we may want.
				int tagEdit = m_qsel->m_tagEdit;
				int hvoEdit = m_qsel->m_hvoEdit;
				ITsStringPtr qtssEmpty; // an empty string in the same writing system as the current paragraph.
				ITsStringPtr qtssCurrent; // old contents of current paragraph.
				CheckHr(m_qsel->m_qtsbProp->GetString(&qtssCurrent));
				ITsStrBldrPtr qtsbT;
				CheckHr(qtssCurrent->GetBldr(&qtsbT));
				CheckHr(qtsbT->ReplaceTsString(0, cchProp, NULL));
				CheckHr(qtsbT->GetString(&qtssEmpty));

				bool fOk;
				m_qsel->UnprotectedCommit(&fOk);
				if (!fOk)
					return kTypingReturn; // something went wrong, can't do any more.

				m_nHowChanged = ksctDiffPara;
				int ihvoIns = m_qsel->m_ihvoFirstPara + 1; //index of first inserted paragraph.
				// Whatever happens, we should end up at the start of a paragraph at index one more than current.
				m_qsel->RequestSelectionAfterUow(m_qsel->m_ihvoFirstPara + 1, 0, false);
				//ISilDataAccess * psda = m_qsel->m_pvpbox->Root()->GetDataAccess();
				if (m_ichAnchor - m_qsel->m_ichMinEditProp < cchProp)
				{
					// There's more text in this property...which we will want to move to the new paragraph.
					if (m_ichAnchor == 0)
					{
						// If we're at the very start of a paragraph, insert before it, without
						// modifying the one we're in. The following code implements the relevant
						// cases of what InsertNew does. (It can't be used, because we want to
						// copy the styles of the following, not preceding, paragraph.)
						ITsTextPropsPtr qttpLocal; // Style info to copy to all inserted paras.
						int ihvo = m_qsel->m_ihvoFirstPara;
						PropTag tag = m_qsel->m_tagParaProp;
						HVO hvoObj = m_qsel->m_hvoParaOwner;
						VwRootBox * prootb = m_qsel->m_pvpbox->Root();
						ISilDataAccessPtr qsda;
						CheckHr(prootb->get_DataAccess(&qsda));
						if (tag == kflidStText_Paragraphs)
						{
							HVO hvoBase;
							CheckHr(qsda->get_VecItem(hvoObj, tag, ihvo, &hvoBase));
							IUnknownPtr qunkTtp;
							CheckHr(qsda->get_UnknownProp(hvoBase, stParaTags_StyleRules, &qunkTtp));
							if (qunkTtp)
								CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttpLocal));
						}

						// Create and initialize the new objects.
						for (int i2 = 0; i2 < cchIns; i2++)
						{
							HVO hvoNew;
							CheckHr(qsda->MakeNewObject(kclidStTxtPara, hvoObj, tag,
								ihvo + i2, &hvoNew));
							if (tag == kflidStText_Paragraphs)
								CheckHr(qsda->SetUnknown(hvoNew, stParaTags_StyleRules, qttpLocal));
							CheckHr(m_qsda->SetString(hvoNew, m_qsel->m_tagEdit, qtssEmpty));
						}
						ihvoIns--;
					}
					else
					{
						// Split the current paragraph.
						int ichStartDel = m_ichAnchor - m_qsel->m_ichMinEditProp;

						// There is text after the IP, new lines take same style
						CheckHr(m_qsda->InsertNew(m_qsel->m_hvoParaOwner, m_qsel->m_tagParaProp,
							m_qsel->m_ihvoFirstPara, cchIns, NULL));

						// Move the tail end of this paragraph to the new one.
						HVO hvoNew;
						CheckHr(m_qsda->get_VecItem(m_qsel->m_hvoParaOwner, m_qsel->m_tagParaProp, m_qsel->m_ihvoFirstPara + 1, &hvoNew));
						CheckHr(m_qsda->MoveString(hvoEdit, tagEdit, 0, ichStartDel, cchProp, hvoNew, tagEdit, 0, 0, true));
					}
				}
				else
				{
					// We're at the very end of the current paragraph; insert a new one, with smart style, without any text movement.
					CheckHr(m_qsda->InsertNew(m_qsel->m_hvoParaOwner, m_qsel->m_tagParaProp,
						m_qsel->m_ihvoFirstPara, cchIns,
						m_prootb->Stylesheet()));
				}
				// Now that we've typed real characters, any pending writing system will be
				// associated with them, so we can delete this information:
				*m_pwsPending = -1;
				// We've requested a new selection at end of UOW, so get rid of *this and make sure we don't try to do any more.
				m_prootb->DestroySelection();
				return kTypingReturn;
			}
			// If we get here we found some characters that are NOT text or returns. Skip them.
			// For now control chars are simply discarded, if they get this far.  (Many
			// clients, such as the data entry window, intercept some of them and do
			// something with them.)
			for (; pch < m_pchInput + m_cchInput && *pch < 32 && *pch != '\r'; pch++)
				;
			cchIns = pch - m_pchInput;
			m_cchInput -= cchIns;
			m_pchInput += cchIns;
		}

		return kTypingContinue;
	}

	/************************************** UpdateDisplay ()*************************************/
	void UpdateDisplay(ITsTextPropsPtr qttp)
	{
		ITsTextPropsPtr qttpBefore;
		ITsTextPropsPtr qttpAfter;
		IVwRootSitePtr qvrs;
		ITsStringPtr qtssNewProp;
		VwParagraphBox * pvpboxT;
		VwPropertyStorePtr qzvps;

		// At this point the string builder has been updated with what the user typed.
		// Now we have to update the display.
		// Get the new string that results from the user's typing.
		CheckHr(m_qsel->m_qtsbProp->GetString(&qtssNewProp));

		int cchPropNew;
		CheckHr(qtssNewProp->get_Length(&cchPropNew));

		// Make the replacement in the string box and update the display.
		// OPTIMIZE JohnT: look for ways to optimize this.
		// For example: actually put the string builder into the VwTxtSrc; but then,
		// do we have to do all our invalidating of old stuff before we update it?
		// Or: make the string box replace smarter, so it does not invalidate all lines
		// of the paragraph, only those that change.
		m_qsel->m_pvpbox->Source()->StyleAtIndex(m_qsel->m_itssProp, &qzvps);

		// Unfortunately this method requires a paragraph box argument. Make one.
		pvpboxT = m_qsel->MakeDummyPara(qtssNewProp, qzvps);
		{
			HoldLayoutGraphics hg(m_prootb);
			m_qsel->m_pvpbox->ReplaceStrings(hg.m_qvg, m_qsel->m_itssProp, m_qsel->m_itssProp + 1,
				pvpboxT);
		}
		delete pvpboxT;

		// update the member variables
		m_qsel->m_ichLimEditProp = m_qsel->m_ichMinEditProp + cchPropNew;
		m_qsel->m_ichAnchor = m_qsel->m_ichEnd = m_ichAnchor;
		m_qsel->m_ichAnchor2 = -1;
		m_qsel->m_qttp = qttp;

		// Figure out whether the insertion point that is left after we are done will
		// associate with the preceding or following character. Default is the
		// preceding one--users generally expect that newly typed characters will
		// have the properties of the character before. This also works if we are
		// going to insert something, since the thing inserted will have the exact
		// right properties, and the IP will follow it. There are two exceptions. First,
		// we try hard to keep the IP logically in the same string, so any typed text
		// will actually replace the range. Second, we try to make the IP associate with
		// a character that has the same properties as the first character of the range
		// deleted.
		m_qsel->m_fAssocPrevious = true;
		if (!m_cchInput && m_ichAnchor <= m_qsel->m_ichLimEditProp)
		{
			// assoc following is plausible since not inserting and there is a following
			// char in the same property.
			if (m_ichAnchor == m_qsel->m_ichMinEditProp)
			{
				// must associate following to keep in same string
				m_qsel->m_fAssocPrevious = false;
			}
			else
			{
				CheckHr(qtssNewProp->get_PropertiesAt(
					m_ichAnchor - 1 - m_qsel->m_ichMinEditProp, &qttpBefore));

				if (qttpBefore != qttp)
				{
					// properties before don't match--are properties after better?
					CheckHr(qtssNewProp->get_PropertiesAt(
						m_ichAnchor - m_qsel->m_ichMinEditProp, &qttpAfter));

					if (qttpAfter == qttp)
					{
						m_qsel->m_fAssocPrevious = false;
					}
				}
			}
		}

		// With look-ahead we can afford to do this, and otherwise
		// the display can get well behind, since keyboard events take priority
		// over paint ones.
		HRESULT hr;
		IgnoreHr(hr = m_prootb->get_Site(&qvrs));
		if (SUCCEEDED(hr))
		{
			// Ignore anything that goes wrong with either of these calls--they are
			// just an optimization, and very unlikely to fail anyway.
			IgnoreHr(qvrs->DoUpdates(m_prootb));
		}
	}

	/************************************** CommitChangesAndUpdateSelection () ****************/
	void CommitChangesAndUpdateSelection()
	{
		int ichAnchorRen = m_qsel->m_pvpbox->Source()->LogToRen(m_qsel->m_ichAnchor);
		if (m_qsel->m_pvpbox->IsSelectionTruncated(ichAnchorRen))
		{
			bool fOk;
			m_qsel->UnprotectedCommit(&fOk);
			// Ignore any problems; we can't recover cleanly in this situation, and there
			// should be no need; we don't have cases of parsing multi-paragraph texts.
			m_prootb->DestroySelection();
#ifdef WIN32
			::MessageBeep(MB_OK); // ENHANCE JohnT (Mac portability).
#else
			// TODO-Linux: implement MessageBeep method to remove this ifndef
			printf("Warning: MessageBeep not implemented\n");
			fflush(stdout);
#endif
		}
		else
		{
			CommitState csOld = m_qsel->m_csCommitState;
			m_qsel->m_csCommitState = kcsNormal;
			m_qsel->CommitAndNotify(m_nHowChanged, m_prootb);
			// Optimize JohnT: if they did a commit, we might want to move from kcsCommitRequest
			// to kcsWorking. But there's no current way to know this happened.
			m_qsel->m_csCommitState = csOld;
		}
		m_prootb->SetDirty(true);
	}

	/************************************** InvalidIntegerInput () *****************************/
	// Check for characters that are invalid in a integer property
	bool InvalidIntegerInput()
	{
		// ENHANCE JohnT: if we allow typing other than English numbers we will need to improve
		// this.
		for (const OLECHAR * pchCheck = m_pchInput + m_cchInput; --pchCheck >= m_pchInput;)
		{
			// This is the same range as in AfDeFeInt::ValidKeyUp, except we don't have
			// to allow backspace or tab because they are otherwise handled.
			int ch = *pchCheck;
			if (!(((ch > 0x2F) && (ch < 0x3A)) || ch == 0x2D))
				return true; // ENHANCE JohnT: do we want to beep or something?
		}

		return false;
	}

	/************************************** Run () *************************************/
	void Run()
	{
		// If all characters are control ones, do nothing...don't even delete a range.
		if (OnlyControlCharacters())
			return;

		// If we're trying to insert something, a complex selection should already have been
		// deleted. It's tempting to throw, but it might be a selection we tried to delete
		// and could not. In that case we will fail again, and correctly do nothing
		// for a non-editable selection.
		if (m_cchInput > 0 && m_qsel->IsComplexSelection())
			return;

		m_qsda = m_prootb->GetDataAccess();
		if (!m_qsda)
		{
			ThrowHr(WarnHr(E_FAIL));
			return;
		}

		HideSelection();

		if (m_qsel->IsInsertionPoint())
		{
			HandleControlBackspaceDelete();
		}
		// default: only position changed; set to ksctDiffPara if para changes
		m_nHowChanged = ksctSamePara;
		bool fWasInsertionPoint = m_qsel->IsInsertionPoint();
		VwParagraphBox * pvpboxOld = m_qsel->m_pvpbox;
		VwParagraphBox * pvpboxEndOld = m_qsel->m_pvpboxEnd;
		if (!m_qsel->DeleteRangeAndPrepareToInsert())
			return;

		if (m_qsel->m_pvpbox != pvpboxOld || m_qsel->m_pvpboxEnd != pvpboxEndOld)
			m_nHowChanged = ksctDiffPara;
		if (!fWasInsertionPoint)
		{
			// If first character was backspace or delete, we used it up deleting the range.
			if (m_chFirst == kscBackspace)
			{
				if (m_cchBackspace > 0)
					m_cchBackspace--;
				else if (m_cchBackspace < 0)
					m_cchBackspace = 0; // no multiple ctrl-backspace
			}
			else if (m_chFirst == kscDelForward)
			{
				if (m_cchDelForward > 0)
					m_cchDelForward--;
				else if (m_cchDelForward < 0)
					m_cchDelForward = 0; // no multiple ctrl-delete
			}
		}
		// If selection has changed, let new selection do the rest of the job...
		if (m_qsel.Ptr() != m_prootb->Selection())
		{
			// I (JohnT) don't think this will ever fire, because currently the selection can only change
			// at this point as a result of processing backspaces, and we never combine them with further characters.
			// I'm leaving it in in case we relax that rule at some point.
			if (m_prootb->Selection() && m_cchInput != 0)
				m_prootb->Selection()->OnTyping(m_pvg, m_pchInput, m_cchInput, m_ss, m_pwsPending);
			return;
		}
		ITsTextPropsPtr qttp = m_qsel->m_qttp; // Used for inserted text.

		// If the first thing typed was backspace or delete, force using the property in the
		// right direction.
		if (m_chFirst == kscBackspace)
			m_qsel->m_fAssocPrevious = true;
		else if (m_chFirst == kscDelForward)
			m_qsel->m_fAssocPrevious = false;

		if (!m_qsel->m_qtsbProp)
		{
			CommitChangesAndUpdateSelection();
			return; // nothing editable
		}

		// If DeleteRangeAndPrepareToInsert succeeded, we should now have an IP.
		Assert(m_qsel->IsInsertionPoint());

		// If it is an integer property, reject any edit which includes invalid characters.
		if (m_cchInput && (m_qsel->m_vnp & kvnpPropTypeMask) == kvnpIntProp)
		{
			if (InvalidIntegerInput())
			{
				CommitChangesAndUpdateSelection();
				return;
			}
		}

		int cchPropOrig; // the original number of characters in the string builder
		CheckHr(m_qsel->m_qtsbProp->get_Length(&cchPropOrig));

		// Don't modify the real variables until we know the update succeeded.
		m_ichAnchor = m_qsel->m_ichAnchor;
		m_ichEnd = m_qsel->m_ichEnd;

		// At this point we have deleted any old range, and m_ichAnchor
		// and m_ichEnd indicate the same position. From this position we want to
		// delete m_cchBackspace preceding characters and/or m_cchDelForward following ones,
		// and then insert the typed text if any.
		if (m_cchBackspace > 0)
		{
			switch (HandleBackspace(qttp))
			{
			case kTypingReturn:
				return;
			case kTypingCommit:
				CommitChangesAndUpdateSelection();
				return;
			default:
				break;
			}
		}

		// At this point we have deleted any old range, and m_ichAnchor and m_ichEnd indicate
		// the same position. From this position we want to delete m_cchDelForward following
		// characters, and then insert the typed text if any.
		if (m_cchDelForward > 0)
		{
			switch (HandleDelForward(qttp))
			{
			case kTypingReturn:
				return;
			case kTypingCommit:
				CommitChangesAndUpdateSelection();
				return;
			default:
				break;
			}
		}
		else if (m_cchDelForward == -1)
		{
			// Delete backward one word.
			Assert(!m_qsel->m_pvpboxEnd);
			Assert(m_qsel->m_ichEnd == m_qsel->m_ichAnchor);
			Assert(!m_cchInput);
			Assert(!m_cchBackspace);
			// We handle this case later.
		}

		// Done all the delete type stuff, if the result is an empty string clear link info.
		m_qsel->m_qttp = qttp; // allow this to be updated by the following code.
		m_qsel->CleanPropertiesForTyping();
		qttp = m_qsel->m_qttp;

		if (m_cchInput)
		{
			if (HandleOtherInput(qttp, cchPropOrig) == kTypingReturn)
			{
				// We may be left in an unstable state.  (See FWR-3085.)
				if (m_qsel->m_qtsbProp)
					CommitChangesAndUpdateSelection();
				return;
			}
		}
		else
		{
			if (!qttp)
			{
				// The only way this can happen is if we neither deleted nor inserted anything,
				// for example, hit backspace at the very start of a field.
				// Since we didn't change anything we can skip straight to being done.
				CommitChangesAndUpdateSelection();
				return;
			}
			if (*m_pwsPending != -1)
			{
				ITsPropsBldrPtr qtpb;
				qtpb.CreateInstance(CLSID_TsPropsBldr);
				CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, *m_pwsPending));
				CheckHr(qtpb->GetTextProps(&qttp));
			}
		}

		UpdateDisplay(qttp);
		CommitChangesAndUpdateSelection();
	}
};

/*----------------------------------------------------------------------------------------------
	Handle typed input consisting of cchInput characters starting at pchInput
	not including any special characters; in addition, the user typed
	cchBackspace backspaces before inserting the data, and cchDelForward
	forward deletes. The very first character the user typed was chFirst.
	Typical response is to delete any range that is selected, plus cchBackspace
	chars logically before the selection, plus cchDelForward after it, then insert
	the chars at pchInput. If chFirst is a backspace or delete and the selection
	was a range, the appropriate count is decremented.
	The caller should call this method as often as possible consistent with
	accumulating all available type-ahead input before calling.
	The caller should complete all IME processing before calling.

	If cchBackspace == -1, then either delete the current selection if there is one, or create
	a selection that extends to the next preceding beginning of a word and delete that newly
	created selection.  (This handles Control-Backspace.)
	If cchDelForward == -1, then either delete the current selection if there is one, or create
	a selection that extends to the next following beginning of a word and delete that newly
	created selection.  (This handles Control-Delete.)

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param pchInput
	@param cchInput
	@param cchBackspace
	@param cchDelForward
	@param chFirst
	@param rcSrcRoot
	@param rcDstRoot
	@param pwsPending Used to pass in an writing system to use, overriding the writing system of
		the text, for newly typed characters. This is used to allow the user to select a
		keyboard and have it apply to the next typing, even if there was not an IP at the time
		he made the selection (e.g., the selection was then a range).  Once used, this routine
		sets it back to -1 so it won't get applied again.
	Note: client is responsible to start a suitable unit of work.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::OnTyping(IVwGraphics * pvg, const wchar * pchInput, int cchInput, VwShiftStatus ss, int * pwsPending)
{
	StartProtectMethod();
	try
	{
		// Some changes may result in deleting the selection. Don't let the deletion actually
		// happen until we are done.
		LockThis();
		OnTypingMethod otm(this, pvg, pchInput, cchInput, ss, pwsPending);
		otm.Run();
	}
	catch(Throwable& thr)
	{
		EndProtectMethod();
		// do this so that we keep the original message (and stack trace)
		CheckHrCore(HandleThrowable(thr, IID_IVwSelection, &g_fact));
	}
	catch(...)
	{
		EndProtectMethod();
		throw;
	}
	EndProtectMethod();
}

/*----------------------------------------------------------------------------------------------
	This is called after we have updated the paragraph-level property to fix up the
	variables that relate to the string level property.
	We assume that the caller has ensured that the string property for the relevant object
	contains the right value, as does the display, and q_tsbProp. Also, the paragraph-level
	variables, especially m_pnoteParaOwner, m_ipropPara, and m_ihvoFirstPara, are correct.
	Now, we need to find the notifier (m_qanote) and paragraph box (m_pvpbox) that
	correspond to that position in the containing notifier.
	Also ensures that:
		- this is the root box's selection
		- we don't have a multi-paragraph selection
		- m_ihvoLastPara is the same as m_ihvoFirstPara.

	@param ichEditNew The new ich position in the paragraph. Usually this is the value that will
					become m_ichMinEditProp. However, it's not possible in all cases to
					set m_ichMinEditProp prior to calling this method, so we pass it in.
					This value is used to determine if we found the correct paragraph box.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::FixStringVars(int ichEditNew)
{
	if (!m_pnoteParaOwner)
		return; // Not much we can do

	// This finds the sub-notifier, expanding lazy boxes if necessary.
	m_qanote = m_pnoteParaOwner->FindChild(m_ipropPara, m_ihvoFirstPara, 0);
	m_pvpbox = NULL;
	m_rcBounds.Clear();
	// in case we don't find anything at all we use the first paragraph box we find - this
	// prevents breaking things in case this fix for TE-5604 doesn't work in all cases.
	VwParagraphBox * pvpBoxFallback = NULL;
	// Lim for our search is the next box after the last box that isn't inside the notifier.
	VwBox * pboxLim = m_qanote->LastBox()->NextInRootSeq(true, NULL, false);
	for (VwBox* pbox = m_qanote->FirstBox();
		pbox && !m_pvpbox && pbox != pboxLim;
		pbox = pbox->NextInRootSeq())
	{
		m_pvpbox = dynamic_cast<VwParagraphBox *>(pbox);

		if (!m_pvpbox)
			continue;

		if (!pvpBoxFallback)
			pvpBoxFallback = m_pvpbox;

		if (ichEditNew <= m_pvpbox->Source()->Cch())
		{
			// Now see if the paragraph box we found is the right one. In cases where we have
			// a back translation and a vernacular paragraph inside of the same table row we
			// might get the wrong one in a right-to-left project (TE-5604).
			HVO hvoEdit;
			int tagEdit, ichMinDummy, ichLimDummy;
			IVwViewConstructorPtr qvvcDummy;
			int fragDummy, ipropDummy, itssPropDummy;
			VwAbstractNotifierPtr qanoteDummy;
			VwNoteProps vnpDummy;
			ITsStringPtr qtssPropDummy;

			// ENHANCE JohnT: if we ever allow multiple properties in paragraphs in situations
			// where paragraph-sequence editing can occur, we need to pass a correct ichLim here
			// to be sure we test the right part of the paragraph.
			m_pvpbox->EditableSubstringAt(ichEditNew, ichEditNew, m_fAssocPrevious,
				&hvoEdit, &tagEdit, &ichMinDummy, &ichLimDummy, &qvvcDummy, &fragDummy,
				&qanoteDummy, &ipropDummy, &vnpDummy, &itssPropDummy, &qtssPropDummy);
			if (hvoEdit == m_hvoEdit && tagEdit == m_tagEdit)
			{
				// The paragraph we found is the right one!
				break;
			}
		}
		// The paragraph we found isn't the right one, so we try again
		m_pvpbox = NULL;
	}
	if (!m_pvpbox)
		m_pvpbox = pvpBoxFallback;
	Assert(m_pvpbox);
	m_pvpboxEnd = NULL; // no longer a multi-para selection
	m_rcBounds.Clear();
	// Reinstate us as selection. Since we are not enabled, no drawing should occur.
	VwRootBox* prootb = m_pvpbox->Root();
	prootb->SetSelection(this, false);
	m_ihvoLastPara = m_ihvoFirstPara;
	if (!m_qrootb)
	{
		// We've become valid again!
		m_qrootb = prootb;
		m_qrootb->RegisterSelection(this);
	}
}

/*----------------------------------------------------------------------------------------------
	If any edits have been performed, commit them; then notify listeners and the rootbox's
	rootsite. In this base class, there is nothing to do for the commit, but this typically
	involves parsing integers or dates, not an actual persisted save.
	Return false if an error occurred; this usually has already been reported to the
	user, and should abort the user action that caused it.

	@param nHowChanged -- for notification purposes (needed by VecListeners)
	@param prootb -- the rootbox (can be null, in which case it will be computed)

	If any edits have been performed, commit them. This typically involves parsing
	integers or dates, and closing an Undo record, not an actual save to the database.
	Return false if an error occurred; this usually has already been reported to the
	user, and should abort the user action that caused it.
	Note: this will result in an attempt to update the occurrence we have been editing,
	but the code that detects string similarities should prevent it from doing much work.

----------------------------------------------------------------------------------------------*/
bool VwTextSelection::CommitAndNotify(VwSelChangeType nHowChanged, VwRootBox * prootb)
{
	if (!prootb)
		prootb = m_pvpbox->Root();

	switch (m_csCommitState)
	{
	case kcsWorking:
		// we'll try the commit again at the end of the protected operation.
		m_csCommitState = kcsCommitRequest;
		return true;
	case kcsCommitRequest:
		// We already have one pending, ignore new request.
		return true;
	case kcsInCommit:
		// We're doing a commit, a recursive call is strange; ignore it.
		return true;
	case kcsNormal:
		break; // normal default, go ahead and commit.
	}

	try
	{
		bool fOk;
		UnprotectedCommit(&fOk);
	}
	catch(...)
	{
		m_csCommitState = kcsNormal;
		throw;
	}
	m_csCommitState = kcsNormal;
	return VwSelection::CommitAndNotify(nHowChanged, prootb);
}

/*----------------------------------------------------------------------------------------------
	This is like Commit, but does not do the checking that no operation is in progress
	which we would normally do calling from outside the DLL.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::UnprotectedCommit(bool * pfOk)
{
	bool fWasWorking = m_csCommitState == kcsWorking || m_csCommitState == kcsCommitRequest;
	m_csCommitState = kcsNormal;
	LockThis();

	CommitAndContinue(pfOk);
	if (*pfOk)
		m_qtsbProp.Clear();
	// We can't be still committing or still needing to commit, but we might be still working.
	if (!fWasWorking)
		m_csCommitState = kcsWorking;
}

/*----------------------------------------------------------------------------------------------
	This method is obsolete. Use Commit() instead. Once upon a time, this method differed by
	allowing PropChanged notifications to be delayed, but now they are always delayed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::CompleteEdits(VwChangeInfo * pci, ComBool * pfOk)
{
	BEGIN_COM_METHOD;

	ThrowHr(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	Extend the selection to include the entire range of the selected string or strings.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::ExtendToStringBoundaries()
{
	BEGIN_COM_METHOD;

	// Deactivate the selection until all the changes are made.
	VwRootBox * prootb = m_pvpbox->Root();
	VwSelectionState vss = prootb->SelectionState();
	bool fSelShowing = Showing();
	if (fSelShowing)
		Hide();
	if (vss == vssEnabled)
		prootb->HandleActivate(vssDisabled);

	int ichMinAnchor, ichLimAnchor;
	int ichMinEnd, ichLimEnd;
	if (m_qtsbProp)
	{
		// If editing is in progress, we already know the bounds of the property (and should ensure
		// we don't expand to any other!)
		ichMinAnchor = ichMinEnd = m_ichMinEditProp;
		ichLimAnchor = ichLimEnd = m_ichLimEditProp;
	}
	else
	{
		ITsStringPtr qtss;
		VwPropertyStorePtr qzvps;
		int itssAnchor;
		bool fAssocPrev = this->IsInsertionPoint() ? m_fAssocPrevious : m_fEndBeforeAnchor;
		m_pvpbox->Source()->StringFromIch(m_ichAnchor, fAssocPrev, &qtss, &ichMinAnchor, &ichLimAnchor, &qzvps, &itssAnchor);

		VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
		int itssEnd;
		fAssocPrev = this->IsInsertionPoint() ? m_fAssocPrevious : !m_fEndBeforeAnchor;
		pvpboxEnd->Source()->StringFromIch(m_ichEnd, fAssocPrev, &qtss, &ichMinEnd, &ichLimEnd, &qzvps, &itssEnd);
	}

	m_ichAnchor = ichMinAnchor;
	m_ichEnd = ichLimEnd;

	if (this->IsInsertionPoint())
	{
		// Todo JohnT: Use EditableSubString here so that even if there are empty strings,
		// we're sure to select all of the same property that the user will actually edit.
		// Recalculate m_qttp from the new beginning character (m_ichAnchor).
		int cprop;
		IVwPropertyStorePtr qvps;
		GetSelectionProps(1, &m_qttp, &qvps, &cprop);
		// Don't worry about cprop == 0: the method got confused and thought it failed.
	}
	else
		m_qttp = NULL;

	prootb->HandleActivate(vss); // Restore the activation state.
	// Restore the selection.
	if (fSelShowing)
		Show();

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	A version of Commit for internal use, which does not clear m_qtsbProp

	@param pfOk
----------------------------------------------------------------------------------------------*/
void VwTextSelection::CommitAndContinue(bool * pfOk, VwChangeInfo * pci)
{
	// If there are no pending changes there is nothing to do.

	if (!m_qtsbProp)
	{
#ifdef ENABLE_TSF
		if (m_pvpbox->Root()->TextStore()->IsDoingRecommit())
		{
			// We need to do a real update!!
			StartEditing();
		}
		else
#elif defined(MANAGED_KEYBOARDING)
		ComBool fDoingRecommit;
		CheckHr(m_pvpbox->Root()->InputManager()->get_IsEndingComposition(&fDoingRecommit));
		if (fDoingRecommit)
		{
			// We need to do a real update!!
			StartEditing();
		}
		else
#endif
		{
			*pfOk = true;
			return;
		}
	}

	CommitState nextCommitState = kcsNormal; // state to set at end

	switch (m_csCommitState)
	{
	case kcsWorking:
		// This may be called from inside the protected method; stay working.
		nextCommitState = kcsWorking;
		break;
	case kcsCommitRequest:
		// May be called from inside the protected method; since we've committed,
		// we're back to 'working', unless called from EndProtectMethod, which
		// will clean up after us.
		nextCommitState = kcsWorking;
	case kcsInCommit:
		// True recursion needs to be prevented.
		*pfOk = false;
		return;
	case kcsNormal:
		break; // normal default, go ahead and commit.
	}

	m_csCommitState = kcsInCommit;

	try
	{
		// Update the property.
		DoUpdateProp(m_pvpbox->Root(), m_hvoEdit, m_tagEdit, m_vnp, m_qvvcEdit, m_fragEdit,
			m_qtsbProp, pfOk);
		// Force future edits to start editing again. Without this, when Commit is used from
		// outside the subsystem (e.g., when switching views), we can go on saving data
		// in m_qtsbProp which becomes out of date.
	}
	catch(...)
	{
		m_pvpbox->Root()->DestroySelection(); // so we don't keep trying to commit it
		*pfOk = false;
		m_csCommitState = nextCommitState;
		throw; // Don't ignore the exception
	}

	m_csCommitState = nextCommitState;
}

// Call this at the start of a method that should not be interrupted by Commit
void VwTextSelection::StartProtectMethod()
{
	m_csCommitState = kcsWorking;
}

// Call this at the end of a method that should not be interrupted by Commit.
void VwTextSelection::EndProtectMethod()
{
	if (m_csCommitState == kcsCommitRequest)
	{
		m_csCommitState = kcsNormal; // BEFORE commit, allows Commit to proceed.
		CommitAndNotify(ksctUnknown);
	}
	m_csCommitState = kcsNormal; // return to normal state, commits enabled
}

void VwTextSelection::MakeSubString(ITsString * ptss, int ichMin, int ichLim, ITsString ** pptssSub)
{
	ITsStrBldrPtr qtsb;
	int cch;
	CheckHr(ptss->GetBldr(&qtsb));
	CheckHr(ptss->get_Length(&cch));
	if (ichLim < cch)
		CheckHr(qtsb->Replace(ichLim, cch, NULL, NULL));
	if (ichMin)
		CheckHr(qtsb->Replace(0, ichMin, NULL, NULL));
	// If we are getting a substring of 0 characters (one run, zero length), then make
	// sure it has a writing system set (fixes TE-4547)
	if (ichMin == ichLim)
	{
		ITsTextPropsPtr qttp;
		int var, val;
		CheckHr(qtsb->get_Properties(0, &qttp));
		CheckHr(qttp->GetIntPropValues(ktptWs, &var, &val));
		if (var == -1)
		{
			// We don't have a WS!
			int wsNew = 0;
			int iRun;
			CheckHr(ptss->get_RunAt(ichMin, &iRun));
			if (iRun > 0)
			{
				// Try to get the WS from the previous run
				CheckHr(ptss->get_Properties(iRun - 1, &qttp));
				CheckHr(qttp->GetIntPropValues(ktptWs, &var, &wsNew));
			}

			if (wsNew <= 0)
			{
				// Still don't have a writing system, so use the WS of the last run that the
				// selection is located in.
				int cRun;
				CheckHr(m_qtsbProp->get_RunCount(&cRun));
				Assert(cRun > 0);
				CheckHr(m_qtsbProp->get_Properties(cRun - 1, &qttp));
				CheckHr(qttp->GetIntPropValues(ktptWs, &var, &wsNew));
			}

			Assert(wsNew > 0);
			// update the builder with the new writing system
			CheckHr(qtsb->get_Properties(0, &qttp));
			ITsPropsBldrPtr qtpb;
			ITsTextPropsPtr qttpNew;
			CheckHr(qttp->GetBldr(&qtpb));
			CheckHr(qtpb->SetIntPropValues(ktptWs, 0, wsNew));
			// During this case, the TsString seems to contain a paragraph style set as a
			// character style. We need to get rid of that.
			// REVIEW (TimS): Is there something else we could do? Could end out removing
			// a legitimate style?
			CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, NULL));
			CheckHr(qtpb->GetTextProps(&qttpNew));
			CheckHr(qtsb->SetProperties(0, 0, qttpNew));
		}
	}
	qtsb->GetString(pptssSub);
}

// Determine whether there is something we want to treat as paragraph break at position ich in *pchw.
// If a match is found, and pichNext is not null, set *pichNext to the index of the next character
// following the paragraph break.
// Currently, crlf counts as a single paragraph break;
// cr followed by anything but lf counts as a single paragraph break (e.g., text copied from Mac);
// lf counts as a single paragraph break (unless skipped by processing preceding cr; e.g., text copied from Linux
// or programs like MS Publisher that use lf for shift-Enter).
// Don't consider characters at position pchw +cch or beyond.
bool CheckForParaBreak(const wchar * pchw, int ich, int cch, int * pichNext)
{
	if (ich >= cch)
		return false;
	if (pchw[ich] == '\r')
	{
		if (ich + 1 >= cch || pchw[ich + 1] != '\n')
		{
			// isolated cr
			*pichNext = ich + 1;
			return true;
		}
		else
		{
			// crlf
			*pichNext = ich + 2;
			return true;
		}
	}
	else if (pchw[ich] == '\n')
	{
		// (presumably) isolated lf
			*pichNext = ich + 1;
			return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Replace what is selected with a TsString.
	If the string contains newlines, the properties associated with the Newline become
	the paragraph properties, if it is possible to make new paragraphs at the current
	selection. Otherwise, newlines are stripped out.

	If the properties of paragraphs being inserted are different from the parargraph inserted
	into, call the root site's InsertDiffParas() method and respond according to what it
	returns.

	@param ptss
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::ReplaceWithTsString(ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);

	VwRootBox * prootb = m_pvpbox->Root();

	Assert(prootb);
	Hide();
	prootb->HandleActivate(vssDisabled); // Prevent cursor flashing from turning on again.
	// some changes may result in deleting the selection. Don't let the deletion actually
	// happen until we are done.
	LockThis();

	// We can prevent some odd cursor movements later in some situations if we normalize
	// up front.
	ITsStringPtr qtssNorm;
	CheckHr(ptss->get_NormalizedForm(knmNFD, &qtssNorm));
	ptss = qtssNorm;

	// default: only position changed; set to 2 if para changes
	VwSelChangeType nHowChanged = ksctSamePara;
	try
	{
		VwParagraphBox * pvpboxT;
		VwPropertyStorePtr qzvps;
		ITsStrBldrPtr qtsbFix;
		ITsStringPtr qtssNewProp;
		ITsStringPtr qtss;
		ComVector<ITsTextProps> vpttpPara;
		int cTabs;
		int cNewlines;
		SmartBstr sbstr;
		bool fWasRootSelection = (this == prootb->Selection());

		bool fHaveParas = false;
		int ich = -1;
		int ichAnchor;

		VwParagraphBox * pvpboxOld = m_pvpbox;
		VwParagraphBox * pvpboxEndOld = m_pvpboxEnd;
		bool fSuccess = DeleteRangeAndPrepareToInsert();
		if (!fSuccess)
		{
			prootb->ShowSelectionAfterEdit();
			return S_OK;
		}

		// DeleteRangeAndPrepareToInsert can DestroySelection when deleting
		// Complex selections. UOW needs to be committed before selection
		// is valid again.
		// It seems in general (see FWR-1732) Pasting over
		// Paragraph boundaries isn't allowed. This code block deals with
		// the cases where it is allowed.
		// Ideally before calling ReplaceWithTsString, the caller should have already deleted
		// the Selection if it was complex. (as the caller can then join the two UOW)
		if (fWasRootSelection && prootb->Selection() == NULL)
		{   ISilDataAccessPtr qsda;
			prootb->get_DataAccess(&qsda);
			IActionHandlerPtr qah;
			CheckHr(qsda->GetActionHandler(&qah));

			SmartBstr sbstrUndo;
			qah->GetUndoText(&sbstrUndo);
			SmartBstr sbstrRedo;
			qah->GetRedoText(&sbstrRedo);
			qah->BreakUndoTask(sbstrUndo, sbstrRedo);
			if (prootb->Selection() == NULL)
				return S_OK;
			return prootb->Selection()->ReplaceWithTsString(ptss);

			// ENHANCE FWNX-616 TomH: should call MergeLastTwoUnitsOfWork but that is part of IActionHandlerExtenstions
			// which isn't currenly avaliable to C++.
		}

		// If that resulted in a different selection object, let it take over.
		if (fWasRootSelection && prootb->Selection() != this)
			return prootb->Selection()->ReplaceWithTsString(ptss);
		// If this selection has become unusable, all we can do is give up.
		if (!m_qrootb)
			ThrowHr(WarnHr(E_FAIL));

		if (m_pvpbox != pvpboxOld || m_pvpboxEnd != pvpboxEndOld)
			nHowChanged = ksctDiffPara;
		if (!m_qtsbProp)
			goto LDone;				// Nothing editable.

		Assert(IsInsertionPoint());
		// Don't modify the real variables until we know the update succeeded.
		ichAnchor = m_ichAnchor;
		// At this point we have deleted any old range, and ichAnchor and ichEnd indicate the
		// same position.

		int cch;
		const wchar * pchw;
		// Count the number of tabs and newlines in the replacement text.
		CheckHr(ptss->get_Length(&cch));
		CheckHr(ptss->get_Text(&sbstr));
		pchw = sbstr.Chars();
		cTabs = 0;
		cNewlines = 0; // Count of newlines OTHER THAN paragraph breaks in the input.

		// Get field IDs provided by IStructuredTextDataAccess
		ISilDataAccess * psda = m_pvpbox->Root()->GetDataAccess();
		int notUsed_Contents;
		int stParaTags_StyleRules;
		int stTextTags_Paragraphs;
		GetTags(psda, &stTextTags_Paragraphs, &stParaTags_StyleRules, &notUsed_Contents);

		if (m_pnoteParaOwner)
		{
			fHaveParas = true;
		}
		else if (m_qvvcEdit)
		{
			// Handle replacing a view constructor artifact with pasting multiple paragraphs.
			// TODO (TE-5833): Something needs to be done to initialize underlying data structures. Setting
			// fHaveParas to true is nice, but results in the view constructor static string
			// never being replaced!
			VwNotifier * pnotePara = m_qanote->Parent();
			// We must have a containing property to do paragraph editing.
			if (pnotePara)
			{
				int ipropPara = m_qanote->PropIndex();
				if (pnotePara->Tags()[ipropPara] == stTextTags_Paragraphs)
					fHaveParas = true;		// paragraph level editing is OK.
				// except that it isn't -- nothing gets pasted in!!
				fHaveParas = false;
			}
		}
		// Search the input text for newlines which do or do not have properties different
		// from the preceding character. If the properties are different, we assume this
		// text was copied from another StText and the newline properties are paragraph ones.
		// Otherwise we assume the text came from some other program and the newlines are
		// stripped out. This code does not cleanly handle strings in which both occur.
		for (ich = 0; ich < cch;)
		{
			int ichNext;
			if (CheckForParaBreak(pchw, ich, cch, &ichNext))
			{
				if (fHaveParas)			// (m_pnoteParaOwner)
				{
					// We have an owning property into which we can insert paragraphs.
					ITsTextPropsPtr qttpText;
					ITsTextPropsPtr qttpPara;
					CheckHr(ptss->get_PropertiesAt(ich, &qttpPara));

					if (ich)
						CheckHr(ptss->get_PropertiesAt(ich-1, &qttpText));
					else if (ichNext < cch) // Handle the case of a newline being the very first character.
						CheckHr(ptss->get_PropertiesAt(ichNext, &qttpText));
					else // This is just a newline by itself.
						qttpPara.Clear();

					if (SameObject(qttpText, qttpPara))
						qttpPara.Clear();		// Not a special paragraph property after all.
					vpttpPara.Push(qttpPara);
					Assert(cNewlines == 0);
				}
				else
				{
					++cNewlines;
					Assert(vpttpPara.Size() == 0);
				}
				ich = ichNext;
				continue;
			}
			else if (pchw[ich] == '\t')
			{
				++cTabs;
			}
			++ich;
		}
		// Remove any TAB and characters from the string. If there are non-paragraph-props
		// newlines remove them too.
		if (cTabs || cNewlines)
		{
			StrUni stuSpace(L" ");
			StrUni stuEmpty;
			ITsStrBldrPtr qtsb;
			CheckHr(ptss->GetBldr(&qtsb));
			int ichNext;
			for (ich = cch; ich-- > 0; )
			{
				if (pchw[ich] == '\t')
					qtsb->Replace(ich, ich+1, NULL, NULL);
				else if (cNewlines && CheckForParaBreak(pchw, ich, cch, &ichNext))
				{
					// Working backwards...CheckForParaBreak may have found trailing newline in crlf.
					if (ich > 0 && pchw[ich] == '\n' && pchw[ich - 1] == '\r')
						ich--;
					// Most newlines get changed to spaces, but a trailing newline being pasted
					// where we can't have newline can just be ignored.
					SmartBstr sbstrReplacement((ichNext == cch) ? stuEmpty : stuSpace);
					ITsTextPropsPtr qttp = m_qttp;
					if (ich > 0)
						CheckHr(qtsb->get_PropertiesAt(ich - 1, &qttp));
					else if (ichNext < cch)
						CheckHr(qtsb->get_PropertiesAt(ichNext, &qttp));
					CheckHr(qtsb->Replace(ich, ichNext, sbstrReplacement, qttp));
					cch -= (ichNext - ich - sbstrReplacement.Length());
				}
			}
			CheckHr(qtsb->GetString(&qtss));
			CheckHr(qtss->get_Length(&cch));
			CheckHr(qtss->get_Text(&sbstr));
			pchw = sbstr.Chars();
		}
		else
		{
			qtss = ptss;
		}
		// Now look for runs that are embedded object data. If any are found, attempt to replace
		// them with an ORC with appropriate properties; or delete if unsuccessful.
		int crun;
		CheckHr(qtss->get_RunCount(&crun));
		// each iteration, cchOffset is increased by the amount by which qtsbFix is shorter than qtss.
		// It thus serves as an offset from the position in the source string to the position in the string builder.
		int cchOffset = 0;
		for (int irun = 0; irun < crun; irun++)
		{
			ITsTextPropsPtr qttp;
			TsRunInfo tri;
			CheckHr(qtss->FetchRunInfo(irun, &tri, &qttp));
			SmartBstr sbstrObjData;
			CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrObjData));
			if (sbstrObjData.Length() == 0)
				continue;
			if (sbstrObjData.Chars()[0] != kodtEmbeddedObjectData)
				continue;
			// If we found some of this kind of data, we will either make an ORC or delete it.
			// Either way we need a builder, if we didn't make one already.
			if (!qtsbFix)
				CheckHr(qtss->GetBldr(&qtsbFix));
			GUID guid;
			StrUni stuText(sbstrObjData.Chars() + 1);
			int odt;
			CheckHr(prootb->Site()->get_MakeObjFromText(stuText.Bstr(), this, &odt, &guid));
			if (guid == GUID_NULL)
			{
				// No corresponding object could be created...delete the run
				CheckHr(qtsbFix->Replace(tri.ichMin - cchOffset, tri.ichLim - cchOffset, NULL, NULL));
				cchOffset += tri.ichLim - tri.ichMin;
			}
			else
			{
				// Replace the run with an ORC for the guid.
				StrUni stuNewData;
				OLECHAR * pchData;
				stuNewData.SetSize(9, &pchData);
				pchData[0] = (OLECHAR)odt;
				memcpy(pchData + 1, &guid, 16);
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(ktptObjData, stuNewData.Bstr()));
				CheckHr(qtpb->GetTextProps(&qttp));
				OLECHAR chOrc = L'\xfffc';
				CheckHr(qtsbFix->ReplaceRgch(tri.ichMin - cchOffset, tri.ichLim - cchOffset, &chOrc, 1, qttp));
				cchOffset += tri.ichLim - tri.ichMin - 1;
			}
		}
		if (qtsbFix)
		{
			CheckHr(qtsbFix->GetString(&qtss));
		}
		// We possibly changed qtss so we need to get the characters again (fixes TE-3483)
		CheckHr(qtss->get_Text(&sbstr));
		CheckHr(qtss->get_Length(&cch));
		pchw = sbstr.Chars();
		// Both branches of the If set this.
		int ichAnchorNew;
		if (vpttpPara.Size())
		{
			// the insertion point's position will be after the last line being inserted,
			// in a paragraph that is the number we plan to insert further on.
			int ichMinLastLine;
			for (ichMinLastLine = sbstr.Length();
				ichMinLastLine > 0 && pchw[ichMinLastLine - 1] != L'\r' && pchw[ichMinLastLine - 1] != L'\n';
				ichMinLastLine--)
			{
			}

			// Store these variables for use when requesting a selection later
			int cchNewLastLine = sbstr.Length() - ichMinLastLine;
			int ihvoParaNewSel = m_ihvoFirstPara + vpttpPara.Size();

			int tagEdit = m_tagEdit;
			HVO hvoEdit = m_hvoEdit;
			// We have multiple paragraphs to create. The properties of each are in vpttpPara.
			int cchProp;
			int ichMin;
			int ichLim;
			bool fNewline = false;
			ichAnchorNew = ichAnchor;
			int iPara;
			//// Save any prior changes to the current paragraph.
			//ComBool f;
			//Commit(&f); // Enhance JohnT: anything sensible we can do if this fails??
			// See if we need to call InsertDiffParas.
			// This is true if any of vpttpPara is different from the properties of the current para.
			// First get those properties.
			HVO hvoOriginalPara;
			CheckHr(psda->get_VecItem(m_hvoParaOwner, stTextTags_Paragraphs,
				m_ihvoFirstPara, &hvoOriginalPara));
			ITsTextPropsPtr qttpOrig;
			IUnknownPtr qunkTtp;
			CheckHr(psda->get_UnknownProp(hvoOriginalPara, stParaTags_StyleRules, &qunkTtp));
			if (qunkTtp)
				CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **)&qttpOrig));
			// We have to call if any item in vpttpPara is different.
			bool fNeedInsertDiffParas = false;
			for (int i = 0; i < vpttpPara.Size(); i++)
			{
				if (vpttpPara[i] != qttpOrig.Ptr())
				{
					fNeedInsertDiffParas = true;
					break;
				}
			}
			if (fNeedInsertDiffParas)
			{
				ComVector<ITsString> vqtssParas;
				for (iPara = 0, ichMin = 0, ichLim = 0; ichLim < cch;)
				{
					int ichNext;
					if (CheckForParaBreak(pchw, ichLim, cch, &ichNext))
					{
						ITsStringPtr qtssT;
						MakeSubString(qtss, ichMin, ichLim, &qtssT);
						vqtssParas.Push(qtssT);
						ichMin = ichLim = ichNext; // move past the line break
					}
					else
					{
						ichLim++;
					}
				}
				ITsStringPtr qtssTrail; // Stuff left over after last newline. May be empty.
				MakeSubString(qtss, ichMin, ichLim, &qtssTrail);
				VwInsertDiffParaResponse idpr;
				Assert(vpttpPara.Size() == vqtssParas.Size());
				bool fOk;
				UnprotectedCommit(&fOk);
// See FWNX-447 - mono doesn't currently support arrays of COM objects native -> managed.
#ifndef WIN32
				if (vpttpPara.Size() == 1)
				{
					CheckHr(prootb->Site()->OnInsertDiffPara(prootb, qttpOrig,
						*(vpttpPara.Begin()),
						*(vqtssParas.Begin()),
						qtssTrail,
						&idpr));
				}
				else
				{
					fprintf(stderr, "Warning using unsupported mono feature. see FWNX-447\n");
					CheckHr(prootb->Site()->OnInsertDiffParas(prootb, qttpOrig, vpttpPara.Size(),
						(ITsTextProps **)(vpttpPara.Begin()),
						(ITsString **)(vqtssParas.Begin()), qtssTrail, &idpr));
				}

#else
				CheckHr(prootb->Site()->OnInsertDiffParas(prootb, qttpOrig, vpttpPara.Size(),
					(ITsTextProps **)(vpttpPara.Begin()),
					(ITsString **)(vqtssParas.Begin()), qtssTrail, &idpr));
#endif
				switch(idpr)
				{
				case kidprDefault:
					break; // Default action, go on with normal insert.
				case kidprDone:
					prootb->ShowSelectionAfterEdit();
					return S_OK;
					break;
				case kidprFail:
					prootb->ShowSelectionAfterEdit();
					return E_FAIL; // should cause containing code to roll back any deletion.
				}
			}

			// Now that we've given the Rootsite a chance to handle the complex insertion and
			// it decided to go with a normal insertion (or there was no complex insertion at
			// all), we can safely request the new selection.
			RequestSelectionAfterUow(ihvoParaNewSel, cchNewLastLine, cchNewLastLine != 0);

			// The behavior inserting multiple lines into the middle of a line is a bit non-obvious.
			// First, it inserts into the current paragraph the text of the first line being pasted (anything before the first newline).
			// Next, it inserts a new paragraph following the current one, and moves the tail end of the original paragraph there.
			// Each of the other lines being inserted, except the last, now goes in as a new paragraph BEFORE the first one
			// inserted, and gets the appropriate bit of the pasted text.
			// Finally, anything after the last newline in the pasted text gets inserted into the paragraph we made first,
			// before the tail end of the original paragraph.
			for (iPara = 0, ichMin = 0; ichMin < cch; ichMin = ichLim)
			{
				fNewline = false;
				// Find the next newline (may be at the beginning of the search).
				int ichNext; // lim of newline
				for (ichLim = ichMin; ichLim < cch; ++ichLim)
				{
					if (CheckForParaBreak(pchw, ichLim, cch, &ichNext))
					{
						fNewline = true;
						break;
					}
				}

				if (ichAnchorNew == 0 && m_ichMinEditProp == 0 && m_itssProp == 0 && fNewline)
				{
					// The insertion position is at the beginning of the paragraph and we found a newline.
					// The text from ichMin to ichLim, even if empty, represents a complete paragraph to be
					// inserted before the current one (and we expect that the IP will then be effectively
					// at the start of the same paragraph we inserted before).
					ITsStringPtr qtssT;
					MakeSubString(qtss, ichMin, ichLim, &qtssT);
					//int cchT = ichLim - ichMin; // calculate before we change ichLim!
					ichLim = ichNext;

					HVO hvoNewPara;
					// Unlike InsertNew, this puts the new paragraph at m_ihvoFirstPara,
					// and not after it.
					psda->MakeNewObject(kclidStTxtPara, m_hvoParaOwner, m_tagParaProp,
						m_ihvoFirstPara, &hvoNewPara);
					CheckHr(psda->SetString(hvoNewPara, tagEdit, qtssT));
					// Set the paragraph styles (if any) of the paragraph object.
					Assert(iPara < vpttpPara.Size());
					ITsTextPropsPtr qttpT = vpttpPara[iPara];
					if (qttpT)
					{
						ITsPropsBldrPtr qtpbT;
						CheckHr(qttpT->GetBldr(&qtpbT));
						// WS required when building TsStrings, but don't want it on paragraph
						CheckHr(qtpbT->SetIntPropValues(ktptWs, -1, -1));
						CheckHr(qtpbT->GetTextProps(&qttpT));
						CheckHr(psda->SetUnknown(hvoNewPara, stParaTags_StyleRules, qttpT));
					}
					else
						CheckHr(psda->SetUnknown(hvoNewPara, stParaTags_StyleRules, qttpOrig));
					++iPara;
					++m_ihvoFirstPara;
					continue;
				}
				else if (ichMin < ichLim)
				{
					// Not at the start of a paragraph; first deal with inserting the text from ichMin to ichLim
					// into the current string property. The next iteration will handle the newline, if any,
					// since it will have no prior text, so will take the next branch.
					// Didn't find newline immediately; line is non-empty.
					ITsStringPtr qtssInsert;
					MakeSubString(qtss, ichMin, ichLim, &qtssInsert);
					ITsStringPtr qtssCurrent;
					CheckHr(psda->get_StringProp(hvoEdit, tagEdit, &qtssCurrent));
					ITsStrBldrPtr qtsbProp;
					CheckHr(qtssCurrent->GetBldr(&qtsbProp));
					CheckHr(qtsbProp->ReplaceTsString(ichAnchorNew - m_ichMinEditProp,
						ichAnchorNew - m_ichMinEditProp, qtssInsert));
					ITsStringPtr qtssNewVal;
					CheckHr(qtsbProp->GetString(&qtssNewVal));
					CheckHr(psda->SetString(hvoEdit, tagEdit, qtssNewVal));
					ichAnchorNew += ichLim - ichMin;
				}
				else
				{
					// We get here (after possibly taking the previous branch on the previous iteration) when we want to
					// insert a newline, not at the start of the current paragraph, and there is no (more) text to insert
					// before it.
					Assert(fNewline);
					CheckForParaBreak(pchw, ichLim, cch, &ichLim);

					if (fHaveParas)			// (m_pnoteParaOwner)
					{
						// Insert a paragraph.
						nHowChanged = ksctDiffPara;
						ITsStringPtr qtssCurrent;
						CheckHr(psda->get_StringProp(hvoEdit, tagEdit, &qtssCurrent));
						CheckHr(qtssCurrent->get_Length(&cchProp));

						if (ichAnchorNew - m_ichMinEditProp < cchProp)
						{
							// The insertion position is before the end of the paragraph...
							// There is text after the IP, new lines take same style.
							// Insert the new paragraph and move the tail end of the current one to it.
							CheckHr(psda->InsertNew(m_hvoParaOwner, m_tagParaProp,
								m_ihvoFirstPara, 1, NULL));

							// Move the tail end of the current paragraph to the new one.
							HVO hvoNew;
							CheckHr(psda->get_VecItem(m_hvoParaOwner, m_tagParaProp,
								m_ihvoFirstPara + 1, &hvoNew));
							CheckHr(psda->MoveString(hvoEdit, tagEdit, 0, ichAnchorNew - m_ichMinEditProp, cchProp,
								hvoNew, tagEdit, 0, 0, true));
						}
						else
						{
							// We're inserting the newline at the very end of this paragraph.
							// Just add another one.
							CheckHr(psda->InsertNew(m_hvoParaOwner, m_tagParaProp,
								m_ihvoFirstPara, 1, NULL));
						}

						// Set the paragraph styles (if any) of the *previous* paragraph object.
						Assert(iPara < vpttpPara.Size());
						ITsTextPropsPtr qttpT;
						qttpT = vpttpPara[iPara];
						++iPara;
						if (qttpT)
						{
							HVO hvoPara;
							CheckHr(psda->get_VecItem(m_hvoParaOwner, stTextTags_Paragraphs,
								m_ihvoFirstPara, &hvoPara));
							ITsPropsBldrPtr qtpbT;
							CheckHr(qttpT->GetBldr(&qtpbT));
							// WS required when building TsStrings, but don't want it on paragraph
							CheckHr(qtpbT->SetIntPropValues(ktptWs, -1, -1));
							CheckHr(qtpbT->GetTextProps(&qttpT));
							CheckHr(psda->SetUnknown(hvoPara, stParaTags_StyleRules, qttpT));
						}
						// We've inserted a newline, the variables we use in the loop should now represent
						// an IP at the start of that paragraph.
						m_ihvoFirstPara += 1;
						// Get the object ID of the new para object.
						CheckHr(psda->get_VecItem(m_hvoParaOwner, m_tagParaProp,
							m_ihvoFirstPara, &hvoEdit));
						ichAnchorNew = 0;
						m_itssProp = 0; // selection is in first string in new para.
						m_ichMinEditProp = 0; // selection prop now certainly starts at zero.
					}
					// else (can't insert para)...just skip newline (but we should have eliminated them earlier)
				}
			}
			// We've arranged a request to replace this selection entirely, we don't need to try to clean it up.
			// Nor should we try to display this selection in its final inconsistent state.
			CheckHr(prootb->DestroySelection());
			// Even though there isn't one to show, we need to be back in the state where it will show when
			// we create a new one.
			prootb->ShowSelectionAfterEdit();
			return S_OK;
		}
		else
		{
			// vpttpPara is empty..we're just adding to the current paragraph.
			CheckHr(m_qtsbProp->ReplaceTsString(ichAnchor - m_ichMinEditProp,
				ichAnchor - m_ichMinEditProp, qtss));
			ichAnchorNew = ichAnchor + cch;
			CheckHr(m_qtsbProp->GetString(&qtssNewProp));
		}
		// Make the replacement in the string box.
		// For example: actually put the string builder into the VwTxtSrc; but then, do we
		// have to do all our invalidating of old stuff before we update it?
		// Or: make the string box replace smarter, so it does not invalidate all lines of
		// the paragraph, only those that change.
		m_pvpbox->Source()->StyleAtIndex(m_itssProp, &qzvps);
		// Unfortunately this method requires a paragraph box argument.  Make one.
		pvpboxT = MakeDummyPara(qtssNewProp, qzvps);
		{ // BLOCK, for HoldLayoutGraphics.
			HoldLayoutGraphics hg(prootb);
			m_pvpbox->ReplaceStrings(hg.m_qvg, m_itssProp, m_itssProp + 1, pvpboxT);
		}
		delete pvpboxT;

		m_ichAnchor = ichAnchorNew;
		m_ichAnchor2 = -1;
		m_ichEnd = ichAnchorNew;
		// Let selection have props of inserted text (unless at start of paragraph...then will be invisible
		// if associated previous).
		m_fAssocPrevious = m_ichAnchor != 0;
		int cchPropNew;
		CheckHr(qtssNewProp->get_Length(&cchPropNew));
		m_ichLimEditProp = m_ichMinEditProp + cchPropNew;
		// Let the text props for any subsequent typing be taken from the last character
		// inserted.
		m_qttp = NULL;
		prootb->SetDirty(true);
	}
	catch (...)
	{
		prootb->HandleActivate(vssEnabled);
		throw;
	}
LDone:
	CommitAndNotify(nHowChanged, prootb);
	prootb->HandleActivate(vssEnabled);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Make a dummy paragraph box into which we can initially insert the new string, so that
	the Replace routine can use that as an argument. Make sure it has the right sort of
	text source, and find a view constructor if appropriate to expand any mapped object
	characters.

	@param ptss
	@param pzvps
----------------------------------------------------------------------------------------------*/
VwParagraphBox * VwTextSelection::MakeDummyPara(ITsString * ptss, VwPropertyStore * pzvps)
{
	bool fMapped = m_pvpbox->Source()->IsMapped();
	VwParagraphBox * pvpboxT = NewObj VwParagraphBox(m_pvpbox->Style(),
		m_pvpbox->Source()->SourceType());

	IVwViewConstructorPtr qvvcEdit;
	if (fMapped)
	{
		// We need a view constructor to process any embedded object characters in the
		// inserted text (or already in the original string).
		qvvcEdit  =  m_qvvcEdit;
		if (!qvvcEdit)
		{
			// The common case, the property was added normally using AddStringProp.
			// Find the higher level view constructor
			VwBox * pboxFirstProp;
			int itssFirstProp;
			int tag;
			int iprop;
			Assert(m_qanote->Parent());
			VwNotifier * pnote = dynamic_cast<VwNotifier *>(m_qanote.Ptr());
			if (pnote)
			{
				m_qanote->Parent()->GetPropForSubNotifier(pnote, &pboxFirstProp,
					&itssFirstProp, &tag, &iprop);
				qvvcEdit = m_qanote->Parent()->Constructors()[iprop];
			} // otherwise pass null, and do no conversions.
		}
		// A mapped text source needs a pointer to the root box to be in a valid state.
		VwRootBox * prootb = m_pvpbox->Root();
		Assert(prootb);
		dynamic_cast<VwMappedTxtSrc *>(pvpboxT->Source())->SetRoot(prootb);
	}
	pvpboxT->Source()->AddString(ptss, pzvps, qvvcEdit);
	return pvpboxT;
}

/*----------------------------------------------------------------------------------------------
	This implements the guts of GetSelectionString and GetFirstParaString. If fWholeSelection
	is true, we always process the whole selection and return true; otherwise, we process just
	the first paragraph, and return true if we truncated.

	Note that the logic here is very similar to VwStringBox::GetDependentObjects.
	We were not able to figure a clean way to factor it out, however.
----------------------------------------------------------------------------------------------*/
class GetSelectionStringMethod
{
private:
	BSTR m_bstrNonText;
	bool m_fWholeSelection;
	ITsStrBldrPtr m_qtsb;
	VwTextSelection * m_pvwsel;
	ISilDataAccess * m_psda;
	StrUniBufSmall m_stubsNewline;
	int m_ichSel;
	int m_ichStart;
	int m_ichEnd;
	int m_ichLim;
	VwParagraphBox * m_pvpboxLast;
	bool m_fLastParaInterlinear;

public:
	GetSelectionStringMethod(VwTextSelection * pvwsel, BSTR bstrNonText, bool fWholeSelection)
	{
		m_pvwsel = pvwsel;
		m_bstrNonText = bstrNonText;
		m_fWholeSelection = fWholeSelection;
		m_stubsNewline.Format(L"%n");
		m_fLastParaInterlinear = false;
	}

	// Find the highest-level containing paragraph box of pbox. This may be pbox itself, in which case,
	// charIndex is not modified. Or, it may be a containing box, in which case, charIndex is changed
	// to the (logical) index of the ORC in that paragraph that represents pbox or one of its containers.
	// It's theoretically possible for it to return null, if pbox is not itself a paragraph, but
	// currently it always is.
	VwParagraphBox * TopParagraph(VwBox * pbox, int & charIndex)
	{
		VwParagraphBox * pvpboxResult = NULL;
		VwBox * pboxChildResult = NULL;
		VwBox * pboxContainer;
		VwBox * pboxChild = pbox;
		for(; pboxChild != NULL; pboxChild = pboxContainer)
		{
			pboxContainer = pboxChild->Container();
			VwParagraphBox * pvpboxContainer = dynamic_cast<VwParagraphBox *>(pboxContainer);
			if (pvpboxContainer)
			{
				pvpboxResult = pvpboxContainer;
				pboxChildResult = pboxChild;
			}
		}
		if (pvpboxResult == NULL)
			return dynamic_cast<VwParagraphBox *>(pbox);
		int itssChildStringIndex = pvpboxResult->StringIndexOfChildBox(pboxChildResult);
		charIndex = pvpboxResult->Source()->IchStartString(itssChildStringIndex);
		return pvpboxResult;
	}

	// pboxCurr, m_pvpboxLast, m_ichStart, and m_ichLim are what they would normally be for a non-interlinear
	// selection. If either end-point is in an embedded paragraph in interinear text, adjust it.
	// The idea is to change any end-point that is embedded somewhere in an inner pile so that
	// the box is the highest-level containing paragraph and the ORC representing the embedded text
	// is included in the range to copy.
	VwBox * AdjustStartAndEndForInterlinear(VwBox * pboxCurr)
	{
		// Don't adjust if in the same paragraph; OK to copy part of one row out of something interlinear.
		if (pboxCurr == m_pvpboxLast)
			return pboxCurr;

		VwParagraphBox * pvpboxCurrTop = TopParagraph(pboxCurr, m_ichStart);
		VwParagraphBox * pvpboxLast = TopParagraph(m_pvpboxLast, m_ichLim);
		if(pvpboxLast != m_pvpboxLast)
		{
			// We did find a containing paragraph; m_ichLim has been set to the char index of the ORC,
			// but to make it a 'lim' it needs to be one greater.
			m_ichLim++;
			m_pvpboxLast = pvpboxLast;
		}
		return pvpboxCurrTop;
	}

	ComBool Run(ITsString ** pptss)
	{
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		CheckHr(qtsf->GetBldr(&m_qtsb));

		if (m_pvwsel->IsInsertionPoint())
		{
			// We'll make an empty string with the properties we would currently apply to
			// inserted text, that is, without attached pictues and so forth, but with the
			// correct writing system, style, and similar.
			ITsTextPropsPtr qttp;
			m_pvwsel->GetInsertionProps(m_pvwsel->m_ichAnchor, m_pvwsel->m_qttp, &qttp);
			if (qttp)
				CheckHr(m_qtsb->SetProperties(0, 0, qttp));
			else
			{
				// We didn't get any properties, which is usually really bad because we should
				// at least have a writing system. But it's okay if this wasn't an editable
				// selection (might have been a rectangle or something), so just skip trying to
				// set the properties. (Caller must be able to handle the possiblity of a string
				// with no props).
				ComBool fEditable;
				CheckHr(m_pvwsel->get_IsEditable(&fEditable));
				if (fEditable)
					ThrowHr(E_UNEXPECTED);
			}
		}
		else
		{
			m_psda = m_pvwsel->m_pvpbox->Root()->GetDataAccess();
			// Piece together the string builder from the selection, which is not an IP.
			// Loop over all boxes involved. Using NextRealBox allows us to loop through tables
			// and even down into interlinear texts, though each paragraph will wind up on
			// a separate line in the result.
			VwBox * pboxCurr = m_pvwsel->m_pvpbox;
			m_pvpboxLast = m_pvwsel->m_pvpbox;
			VwParagraphBox * pvpboxCurr;
			m_ichStart = m_pvwsel->m_ichAnchor;
			m_ichLim = m_pvwsel->m_ichEnd;
			if (m_pvwsel->m_pvpboxEnd)
			{
				if (m_pvwsel->m_fEndBeforeAnchor)
					pboxCurr = m_pvwsel->m_pvpboxEnd; // and leave Last pointing at end
				else
					m_pvpboxLast = m_pvwsel->m_pvpboxEnd; // and leave Curr pointing at start
			}
			if (m_pvwsel->m_fEndBeforeAnchor)
			{
				m_ichStart = m_pvwsel->m_ichEnd;
				m_ichLim = m_pvwsel->m_ichAnchor;
			}
			pboxCurr = AdjustStartAndEndForInterlinear(pboxCurr);
			int ichStartOrig = m_ichStart;
			if (m_pvpboxLast != pboxCurr && !m_fWholeSelection)
			{
				// Change the limit to the end of the first paragraph.
				m_pvpboxLast = dynamic_cast<VwParagraphBox *>(pboxCurr);
				Assert(m_pvpboxLast);
				m_ichLim = m_pvpboxLast->Source()->Cch();
			}
			m_ichSel = 0;
			VwBox * pboxLim = m_pvpboxLast->NextInClipSeq();
			// Keeps track of the previous paragraph we processed (if any) so we can check whether
			// it is part of a table cell on the same line.
			VwParagraphBox * pvpboxPrev = NULL;
			bool fStart = true;
			VwBox * pStartSearch = NULL;
			for (VwBox * pboxNew = pboxCurr; pboxNew != pboxLim; pboxNew = pboxNew->NextInClipSeq())
			{
				// NextBoxForSelection typically returns the same box as NextInClipSeq (provided we don't
				// look 'down' into the current box if it is a paragraph). The current exception is if
				// we're selecting only in one column. If the current box is a table cell, we'll skip any boxes
				// in other columns. (At least I (JohnT) think that's the purpose of this test.)
				if (!fStart && pboxCurr->NextBoxForSelection(&pStartSearch, true, !pboxCurr->IsParagraphBox()) != pboxNew)
					continue;

				fStart = false;
				pboxCurr = pboxNew;
				pvpboxCurr = dynamic_cast<VwParagraphBox *>(pboxCurr);
				if (!pvpboxCurr)
					continue;								// (not a paragraph box.)
				ProcessParagraph(pvpboxCurr, pvpboxPrev);
			}
			// We want to add another newline at the end of the string if:
			// -- the selection extends to the end of the current paragraph
			// -- and either the whole paragraph is selected, or the selection
			//		spanned multiple paragraphs. ichStartOrig is zero if either of these
			//		things occurred.
			// -- We always want it if the last paragraph was interlinear.
			ITsTextPropsPtr qttp;
			if (m_fLastParaInterlinear || (pvpboxCurr && m_ichEnd == pvpboxCurr->Source()->Cch() && ichStartOrig == 0))
			{
				GetParaLevelProps(pvpboxCurr, &qttp);
				CheckHr(m_qtsb->Replace(m_ichSel, m_ichSel, m_stubsNewline.Bstr(), qttp));
			}
		}

		CheckHr(m_qtsb->GetString(pptss));
		return m_fWholeSelection;
	}

protected:
	void GetParaLevelProps(VwParagraphBox * pvpbox, ITsTextProps ** ppttp)
	{
		// Except for the first paragraph, add a newline character or tab to
		// separate the previous paragaph from this. Add paragraph level
		// properties attached if available.
		// First we call EditableSubstringAt to get info about the string at the end of
		// the paragraph.
		HVO hvoEdit;
		int tagEdit;
		// While editing, the range of the whole TsString that corresponds to the property
		// being edited. Meaningless if m_qtsbProp is null.
		int ichMinEditProp;
		int ichLimEditProp;
		// The view constructor, if any, responsible for the edited property.
		IVwViewConstructorPtr qvvcEdit;
		int fragEdit; // The fragment identifier which the VC needs for the edited property.
		VwAbstractNotifierPtr qanote; // The notifier for the property.
		int iprop; // the index of the property within that notifier.
		VwNoteProps vnp; // Notifier Property attributes.
		int itssProp; // index of edited string in list for this string box
		ITsStringPtr qtssProp;
		int ichTarget = pvpbox->Source()->Cch();
		VwEditPropVal vepv = CallEditableSubstring(pvpbox, ichTarget, ichTarget, true,
			&hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
			&qanote, &iprop, &vnp, &itssProp, &qtssProp);

		// Get field IDs provided by IStructuredTextDataAccess
		int stTxtParaTags_Contents;
		int stParaTags_StyleRules;
		int notUsed_Paragraphs;
		GetTags(pvpbox->Root()->GetDataAccess(), &notUsed_Paragraphs, &stParaTags_StyleRules, &stTxtParaTags_Contents);

		if (vepv == kvepvEditable && tagEdit == stTxtParaTags_Contents)
		{
			// We probably have paragraph level props.
			IUnknownPtr qunkTtp;
			CheckHr(m_psda->get_UnknownProp(hvoEdit, stParaTags_StyleRules, &qunkTtp));
			if (qunkTtp)
				CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **)ppttp));
			// If we don't have paragraph props, just give the newline the same props as
			// the last characters we placed.
		}
	}


	/*----------------------------------------------------------------------------------------------
		Process an entire paragraph. Adds to string builder whatever should go into the clipboard
		from pvpboxCurr(which is not changed). Before that, inserts whatever separator should
		go between the previous box and this one. pvpboxPrev is updated to current.
	----------------------------------------------------------------------------------------------*/
	void ProcessParagraph(VwParagraphBox *& pvpboxCurr, VwParagraphBox *& pvpboxPrev)
	{
		ITsTextPropsPtr qttp;

		if (pvpboxPrev)
		{
			GetParaLevelProps(pvpboxPrev, &qttp);
			// Decide whether to insert tab or newline.
			StrUniBufSmall stubsSep = m_stubsNewline;
			VwTableCellBox * ptcboxPrev =
				dynamic_cast<VwTableCellBox *>(pvpboxPrev->Container());
			if (ptcboxPrev)
			{
				VwTableCellBox * ptcboxCurr =
					dynamic_cast<VwTableCellBox *>(pvpboxCurr->Container());
				if (ptcboxCurr && ptcboxPrev->Container() == ptcboxCurr->Container())
				{
					// Two cells in the same row: separate with tab.
					stubsSep = L"\t";
				}
			}
			CheckHr(m_qtsb->Replace(m_ichSel, m_ichSel, stubsSep.Bstr(), qttp));
			m_ichSel += stubsSep.Length();
		}

		VwTxtSrc * pts = pvpboxCurr->Source();
		if (pvpboxCurr == m_pvpboxLast)
			m_ichEnd = m_ichLim;
		else
			m_ichEnd = pvpboxCurr->Source()->Cch();
		m_fLastParaInterlinear = ProcessParagraphAsInterlinear(pvpboxCurr);
		if (!m_fLastParaInterlinear)
		{
		ITsStringPtr qtss;
		int ich; // Start of uncopied text in pararaph (logical)
		int ichMinTss; // Start of current string in paragraph (logical)
		int ichLimTss; // End of current string in paragraph (logical)
		int ichNew = 0; // Each loop iteration computes the new ich and stores it here.
		int itss; // indexes strings in paragraph.
		VwPropertyStorePtr qzvps;
		SmartBstr sbstr;
		pts->StringFromIch(m_ichStart, false, &qtss, &ichMinTss, &ichLimTss, &qzvps, &itss);
		for (ich = m_ichStart; ich < m_ichEnd; ich = ichNew)
			ProcessRun(pts, qtss, ich, ichMinTss, ichLimTss, ichNew, itss);
		}

		m_ichStart = 0; // always start at beginning of subsequent boxes
		pvpboxPrev = pvpboxCurr;
	}

	/*----------------------------------------------------------------------------------------------
		Process one run.

		@param pts			Text source
		@param qtss			The string
		@param ich			Start of uncopied text in pararaph (logical)
		@param ichMinTss	Start of current string in paragraph (logical)
		@param ichLimTss	End of current string in paragraph (logical)
		@param ichNew		Each loop iteration computes the new ich and stores it here.
		@param itss			indexes strings in paragraph.
	----------------------------------------------------------------------------------------------*/
	void ProcessRun(VwTxtSrc * pts, ITsStringPtr& qtss, int ich, int& ichMinTss, int& ichLimTss,
		int& ichNew, int& itss)
	{
		ITsTextPropsPtr qttp;
		int cchRun; // Number of characters in run added to output string.

		// ich is relative to the paragraph, as is ichMinTss and m_ichEnd
		if (qtss)
		{
			TsRunInfo tri;
			CheckHr(qtss->FetchRunInfoAt(ich - ichMinTss, &tri, &qttp));
			if (GetTextRepresentationForObject(tri, qtss, qttp))
			{
				// Get the rendered text, not the actual data.
				int ichMinRen = pts->LogToRen(ich);
				int ichLimRen = pts->LogToRen(min(tri.ichLim + ichMinTss, m_ichEnd));
				Vector<OLECHAR> vch;
				vch.Resize(ichLimRen - ichMinRen);
				pts->Fetch(ichMinRen, ichLimRen, vch.Begin());
				cchRun = ichLimRen - ichMinRen;

				CheckHr(m_qtsb->ReplaceRgch(m_ichSel, m_ichSel, vch.Begin(), cchRun, qttp));
				m_ichSel += cchRun;
			}
			ichNew = ichMinTss + tri.ichLim;
		}
		else
		{
			// Otherwise, there aren't any tss's associated with this char position,
			// so insert the specified string.
			cchRun = BstrLen(m_bstrNonText);
			CheckHr(m_qtsb->Replace(m_ichSel, m_ichSel, m_bstrNonText, qttp));
			m_ichSel += cchRun;
			// Also advance by the one character which is associated with a non-tss.
			ichNew = ichMinTss + 1;
		}
		if (ichNew >= ichLimTss && m_ichEnd > ichLimTss)
		{
			// We need the next string
			Assert(ichNew == ichLimTss);  // we should have used this string up
			if (++itss < pts->CStrings())
				pts->StringAtIndex(itss, &qtss);
			else
				return;
			ichMinTss = ichLimTss;
			int cch;
			if (qtss)
				CheckHr(qtss->get_Length(&cch));
			else
				cch = 1;
			ichLimTss += cch;
		}
		else
		{
			// Only if we didn't move to the next string: there might be some
			// empty strings in there...but processing a run should have
			// caused us to make some progress, otherwise.
			Assert(ichNew > ich);
		}
	}

	// Add to m_qtsb appropriate text for the current paragraph (or the part of it between
	// m_ichStart and m_ichLim) if it is interlinear (and return true); if it has no
	// interlinear text (no direct children in the range are inner piles) return false.
	bool ProcessParagraphAsInterlinear(VwParagraphBox * pvpboxCurr)
	{
		VwTxtSrc * pts = pvpboxCurr->Source();
		Vector<VwInnerPileBox *> vpipboxes;
		Vector<int> vcCols; // number of columns each inner pile needs.
		ITsStringPtr qtss;
		int ichMinTss; // start of first string
		int ichLimTss;
		int itssMin; // first strings in selection.
		int itssLim; // last string we want (part of).
		VwPropertyStorePtr qzvps; // dummy
		pts->StringFromIch(m_ichStart, false, &qtss, &ichMinTss, &ichLimTss, &qzvps, &itssMin);
		if (m_ichLim == pts->Cch())
		{
			itssLim = pts->CStrings();
		}
		else
		{
			pts->StringFromIch(m_ichEnd, false, &qtss, &ichMinTss, &ichLimTss, &qzvps, &itssLim);
			// If the character limit isn't at a string boundary, we need to include the
			// string that contains it in our sequence.
			if (ichMinTss < m_ichEnd)
				itssLim++;
		}

		// This next block of code counts the rows and columns needed. It's very similar to the paragraph box implementation
		// of CountColumnsAndLines, but has different limits and builds the vectors as well.
		int cLineMax = 1;
		int cColTotal = 0;
		VwInnerPileBox * pipboxPrev = NULL;
		for (int itss = itssMin; itss < itssLim; itss++)
		{
			VwInnerPileBox * pipbox = dynamic_cast<VwInnerPileBox *>(pvpboxCurr->ChildAtStringIndex(itss));
			if (pipbox)
			{
				vpipboxes.Push(pipbox);
				int cCol, cLine;
				pipbox->CountColumnsAndLines(&cCol, &cLine);
				vcCols.Push(cCol);
				if (cLine > cLineMax)
					cLineMax = cLine;
			}
			else if (pipboxPrev!= NULL || cColTotal == 0)
				cColTotal++;
			pipboxPrev = pipbox;
		}
		if (vpipboxes.Size() == 0)
			return false; // normal paragraph.

		// OK, we have interlinear...cLineMax lines of it.
		for (int iline = 0; iline < cLineMax; iline++)
		{
			bool fPrevBoxWasInnerPile = false;
			for (int itss = itssMin; itss < itssLim; itss++)
			{
				if (fPrevBoxWasInnerPile)
					AppendTabs(1);
				fPrevBoxWasInnerPile = false;
				pts->StringAtIndex(itss, &qtss);
				if (qtss)
				{
					// The behavior in this block is not (much?) tested...currently interlinear displays do
					// not mix strings and inner piles in a single paragraph.
					if (iline != 0)
					{
						continue; // simple strings happen only on the first line.
					}
					// Output the (rest of) this string as usual.
					int ich; // Start of uncopied text in pararaph (logical)
					int ichMinThis; // Start of current string in paragraph (logical)
					int ichLimThis; // End of current string in paragraph (logical)
					int ichNew = 0; // Each loop iteration computes the new ich and stores it here.
					int itssT; // indexes strings in paragraph.
					VwPropertyStorePtr qzvpsT;
					SmartBstr sbstr;
					pts->StringFromIch(m_ichStart, false, &qtss, &ichMinThis, &ichLimThis, &qzvpsT, &itssT);
					for (ich = max(m_ichStart, ichMinThis); ich < min(m_ichEnd, ichLimThis); ich = ichNew)
						ProcessRun(pts, qtss, ich, ichMinThis, ichLimThis, ichNew, itss);
				}
				else
				{
					VwBox * pbox = pvpboxCurr->ChildAtStringIndex(itss);
					VwInnerPileBox * pipbox = dynamic_cast<VwInnerPileBox *>(pbox);
					if (pipbox)
					{
						AppendInnerPileLine(pipbox, iline);
						fPrevBoxWasInnerPile = true;
					}
					else
					{
						// embedded non-inner-pile box; insert the specified replacement.
						AppendNonStringBoxText();
					}
				}
			}
			// If this is the last paragraph, we will output a closing newline because
			// the last paragraph is interlinear. If there is another paragraph, we will output
			// one as we start processing it.
			if (iline < cLineMax - 1)
				AppendNewline();
		}
		return true; // We output in interlinear mode.
	}

	void AppendTabs(int count)
	{
		for(int i = 0; i < count; i++)
		{
			StrUniBufSmall stubsSep(L"\t");
			CheckHr(m_qtsb->Replace(m_ichSel, m_ichSel, stubsSep.Bstr(), NULL));
			m_ichSel += stubsSep.Length();
		}
	}
	void AppendNewline()
	{
		CheckHr(m_qtsb->Replace(m_ichSel, m_ichSel, m_stubsNewline.Bstr(), NULL));
		m_ichSel += m_stubsNewline.Length();
	}

	void AppendNonStringBoxText()
	{
		// It's rather arbitrary what WS we use for inserting the string that represents a non-text
		// box, but it's most likely something recognizable in the UI language. (Usually it is just a semi-colon.)
		// We need to give some WS because string builder complains about runs with no WS, and this MIGHT
		// get inserted as the very first thing, when there is no adjacent WS to copy. (FWR-3589.)
		ILgWritingSystemFactoryPtr qwsf;
		CheckHr(m_psda->get_WritingSystemFactory(&qwsf));
		int wsUser;
		CheckHr(qwsf->get_UserWs(&wsUser));
		ITsTextPropsPtr qttpTmp;
		ITsPropsBldrPtr qtpb;
		qtpb.CreateInstance(CLSID_TsPropsBldr);
		CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, wsUser));
		CheckHr(qtpb->GetTextProps(&qttpTmp));

		CheckHr(m_qtsb->Replace(m_ichSel, m_ichSel, m_bstrNonText, qttpTmp));
		m_ichSel += BstrLen(m_bstrNonText);
	}

	// Add the appropriate material for the specified line of the specified inner pile.
	// Must include the appropriate number of tabs, even if line is missing.
	// Appropriate number is one fewer than the number of columns the pile as a whole has.
	int AppendInnerPileLine(VwInnerPileBox * pipbox, int ilineTarget)
	{
		int cCol, cLine;
		pipbox->CountColumnsAndLines(&cCol, &cLine);
		int ctabOutput = 0; // we must include exactly cCol - 1 tabs.
		int iline = 0;
		for (VwBox * pbox = pipbox->FirstBox(); pbox; pbox = pbox->NextOrLazy())
		{
			int cColItem, cLineItem;
			pbox->CountColumnsAndLines(&cColItem, &cLineItem);
			// This box represents lines indexed from iline to (but not including) iline + cLineItem.
			if (iline + cLineItem >ilineTarget)
			{
				if (pbox->IsParagraphBox())
					ctabOutput = AppendLineFromParagraph(dynamic_cast<VwParagraphBox *>(pbox), ilineTarget - iline);
				else
				{
					AppendNonStringBoxText();
				}
				break;
			}
			iline += cLineItem;
		}
		AppendTabs(cCol - 1 - ctabOutput);
		return cCol - 1;
	}

	// Add the appropriate material for the specified line of the specified paragraph.
	int AppendLineFromParagraph(VwParagraphBox * pvpbox, int ilineTarget)
	{
		VwTxtSrc * pts = pvpbox->Source();
		int ctab = 0;
		bool fLastBoxWasInnerPile = false;
		for (int itss = 0; itss < pts->CStrings(); itss++)
		{
			if (fLastBoxWasInnerPile)
			{
				AppendTabs(1);
				ctab++;
			}
			fLastBoxWasInnerPile = false; // default for next time
			ITsStringPtr qtss;
			pts->StringAtIndex(itss, &qtss);
			if (qtss)
			{
				// normal text...only on line 0.
				if (ilineTarget == 0)
				{
					CheckHr(m_qtsb->ReplaceTsString(m_ichSel, m_ichSel, qtss));
					int cch;
					CheckHr(qtss->get_Length(&cch));
					m_ichSel += cch;
				}
			}
			else
			{
				VwBox * pbox = pvpbox->ChildAtStringIndex(itss);
				// JohnT says that the following check (for pbox not being NULL) should not be
				// necessary, but that it is safe.  It fixes the immediate crash encountered
				// following the steps given in LT-10456.  However, there may be a deeper
				// problem that should be investigated in greater depth.
				if (pbox && pbox->IsInnerPileBox())
				{
					ctab += AppendInnerPileLine(dynamic_cast<VwInnerPileBox *>(pbox), ilineTarget);
					fLastBoxWasInnerPile = true;
				}
				else
				{
					AppendNonStringBoxText();
				}
			}
		}
		return ctab;
	}

	/*----------------------------------------------------------------------------------------------
		See if we need to get a text representation of an object.

		This is so if we have a one-character run with contents the object replacement character
		and its properties includes ktptObjData and it is of type kodtOwnNameGuidHot or
		kodtGuidMoveableObjDisp.

		@param tri			The run info
		@param qtss			The string
		@param qttp			The text props
		@returns			true if this run should be included in the string representation,
							otherwise false.
	----------------------------------------------------------------------------------------------*/
	bool GetTextRepresentationForObject(TsRunInfo & tri, ITsStringPtr & qtss, ITsTextPropsPtr & qttp)
	{
		if (tri.ichLim - tri.ichMin == 1)
		{
			// One character in run...see if it is ORC
			OLECHAR ch;
			CheckHr(qtss->FetchChars(tri.ichMin, tri.ichLim, &ch));
			if (ch == L'\xfffc')
			{
				SmartBstr sbstrObjData;
				CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrObjData));

				if (sbstrObjData.Length() == 9 &&
					(sbstrObjData.Chars()[0] == kodtOwnNameGuidHot ||
					sbstrObjData.Chars()[0] == kodtGuidMoveableObjDisp ||
					sbstrObjData.Chars()[0] == kodtNameGuidHot))
				{
					// OK, it ought to be replaced. See if the site can make us a textual representation
					// of it.
					GUID guid;
					memcpy(&guid, sbstrObjData.Chars() + 1, 16);
					SmartBstr sbstrText;
					CheckHr(m_pvwsel->m_pvpbox->Root()->Site()->get_TextRepOfObj(&guid, &sbstrText));
					if (sbstrText.Length() > 0)
					{
						// It did!. The output string will have the textual representation of the object
						// with a different type.
						ITsPropsBldrPtr qtpb;
						CheckHr(qttp->GetBldr(&qtpb));
						OLECHAR chType = kodtEmbeddedObjectData;
						StrUni stuNewObjData(&chType, 1);
						stuNewObjData += sbstrText.Chars();
						CheckHr(qtpb->SetStrPropValue(ktptObjData, stuNewObjData.Bstr()));
						CheckHr(qtpb->GetTextProps(&qttp));
					}
					else
					{
						// don't include this run since there is no text representation for the ORC.
						return false;
					}
				}
			}
		}
		return true;
	}
};

/*----------------------------------------------------------------------------------------------
	Return the current selection as a TsString.
	All char indexes are logical.

	@param pptss
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::GetSelectionString(ITsString ** pptss, BSTR bstrNonText)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	Assert(m_qrootb);

	GetSelectionStringMethod method(this, bstrNonText, true);
	method.Run(pptss);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Return the current selection as a TsString.
	All char indexes are logical.

	@param pptss
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::GetFirstParaString(ITsString ** pptss, BSTR bstrNonText,
	ComBool * pfGotItAll)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	ChkComOutPtr(pfGotItAll);

	GetSelectionStringMethod method(this, bstrNonText, pfGotItAll != NULL);
	*pfGotItAll = method.Run(pptss);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Move the selection to the indicated location if it is an insertion point.
	Typically used, I (JohnT) think, to position IP when moving from one field to
	another using arrow keys.

	@param fTopLine true to move to position in top line of view, false for bottom.
	@param xdPos position to align to. 0 means start of line, negative means end.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::SetIPLocation(ComBool fTopLine, int xdPos)
{
	BEGIN_COM_METHOD;

	if (!IsInsertionPoint())
		return S_FALSE;
	VwRootBox * prootb = m_pvpbox->Root();
	HoldScreenGraphics hg(prootb);
	// Create a 'coordinate transformation' that has the right stretch factors
	// but no translation. This is adequate for moving around by key positions,
	// except that anything that expands lazy boxes should be careful to retain
	// only coordinates relative to some real box.
	Rect rcSrcRoot(0, 0, prootb->DpiSrc().x, prootb->DpiSrc().y);

	Hide();
	int xdT;
	// Move to either the top line or the bottom line.
	if (fTopLine)
	{
		while (UpArrow(hg.m_qvg, rcSrcRoot, rcSrcRoot, &xdT))
			;	// Do nothing loop.
	}
	else
	{
		while (DownArrow(hg.m_qvg, rcSrcRoot, rcSrcRoot, &xdT))
			;	// Do nothing loop.
	}
	// Move to the desired horizontal location on that line.
	m_xdIP = -1;
	if (!xdPos)
	{
		HomeKey(hg.m_qvg, 0);
	}
	else if (xdPos < 0)
	{
		EndKey(hg.m_qvg, 0);
	}
	else
	{
		HomeKey(hg.m_qvg, 0);
		// Get as close as possible to the desired horizontal offset.
		int ichNew = m_ichEnd;
		int xdSec;
		int xdNew = PositionOfIP(ichNew, m_pvpbox, true, hg.m_qvg, rcSrcRoot, rcSrcRoot, &xdSec);
		// Scan forward for the closest matching insertion point.
		int ichT;
		for (;;)
		{
			ichT = ForwardOneCharInLine(ichNew, m_pvpbox, m_fAssocPrevious, hg.m_qvg);
			if (ichT == -1 || ichT == ichNew)
			{
				break;
			}
			xdT = PositionOfIP(ichT, m_pvpbox, true, hg.m_qvg, rcSrcRoot, rcSrcRoot, &xdSec);
			if (xdT >= xdPos)
			{
				if (xdPos - xdNew > xdT - xdPos)
					ichNew = ichT;
				break;
			}
			ichNew = ichT;
			xdNew = xdT;
		}
		m_ichEnd = ichNew;
		m_ichAnchor = ichNew;
		m_ichAnchor2 = -1;
		m_fEndBeforeAnchor = false;
		m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, hg.m_qvg);
	}
	Show();
	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	Return true if we can apply paragraph formatting on the current selection.

	Currently, this returns true if the selection is entirely within one StText. We will
	probably want to generalize this eventually, especially since this is currently, I think,
	the only code within the View subsystem itself that knows about the special status of
	StText.

	Currently it checks that the paragraph sequence is editable, as a (rather rough) way of
	checking that it isn't some sort of read-only display.

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::get_CanFormatPara(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);
	*pfRet = false; // default

	HVO hvoEdit;
	int tagEdit;
	// While editing, the range of the whole TsString that corresponds to the property
	// being edited. Meaningless if m_qtsbProp is null.
	int ichMinEditProp;
	int ichLimEditProp;
	// The view constructor, if any, responsible for the edited property.
	IVwViewConstructorPtr qvvcEdit;
	int fragEdit; // The fragment identifier which the VC needs for the edited property.
	VwAbstractNotifierPtr qanote; // The notifier for the property.
	int iprop; // the index of the property within that notifier.
	VwNoteProps vnp; // Notifier Property attributes.
	int itssProp; // index of edited string in list for this string box
	ITsStringPtr qtssProp;

	// This uses notifier information to determine the editable property following
	// ich (adjusted to be box-relative). If we don't get the string and range
	// we expect, it is probably because the string we want is not editable,
	// while the previous one is.
	VwEditPropVal vepv = CallEditableSubstring(m_pvpbox, m_ichAnchor, m_ichAnchor,
		m_fAssocPrevious, &hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit,
		&fragEdit, &qanote, &iprop, &vnp, &itssProp, &qtssProp);
	if (vepv != kvepvEditable)
		return S_OK;
	// For now, we have to be in a structured text to do this.
	VwNotifier * pnote = qanote->Parent();
	if (!pnote || pnote->Tags()[qanote->PropIndex()] != kflidStText_Paragraphs)
		return S_OK;
	vnp = pnote->Flags()[qanote->PropIndex()];
	// It must be an editable sequence property.
	if (vnp != (kvnpObjVecItems | kvnpEditable) && vnp != (kvnpLazyVecItems | kvnpEditable))
		return S_OK;
	// If the selection is entirely in one paragraph of an StText we can do it.
	if (!m_pvpboxEnd)
	{
		*pfRet = true;
		return S_OK;
	}
	// Otherwise need to check end-point is in the same StText.
	VwAbstractNotifierPtr qanoteEnd;
	vepv = CallEditableSubstring(m_pvpboxEnd, m_ichEnd, m_ichEnd, m_fAssocPrevious,
		&hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
		&qanoteEnd, &iprop, &vnp, &itssProp, &qtssProp);
	if (vepv != kvepvEditable)
		return S_OK;
	VwNotifier * pnoteEnd = qanoteEnd->Parent();
	// Has to be looking at the same sequence of paragraphs in the same occurrence of the same
	// StText.
	if (pnoteEnd != pnote || qanoteEnd->PropIndex() != qanote->PropIndex())
		return S_OK;
	*pfRet = true; // OK, we can do it.

	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	Return true if we can apply character formatting on the current selection.

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::get_CanFormatChar(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	TtpVec vqttp;
	VwPropsVec vqvps;
	int cttp;
	CheckHr(GetSelectionProps(0, NULL, NULL, &cttp));
	if (!cttp)
	{
		// If we can't get any props, we can't format anything.
		return S_OK;
	}
	vqttp.Resize(cttp);
	vqvps.Resize(cttp);
	CheckHr(GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
		(IVwPropertyStore **)vqvps.Begin(), &cttp));

	int nvar;
	int nval;
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		HRESULT hr = S_FALSE;
		ITsTextProps * pttp = vqttp[ittp];
		if (pttp)
			CheckHr(hr = pttp->GetIntPropValues(ktptEditable, &nvar, &nval));
		if (hr == S_FALSE)
			CheckHr(vqvps[ittp]->get_IntProperty(ktptEditable, &nval));
		if ((nval == ktptNotEditable) || (nval == ktptSemiEditable))
		{
			// If it's not fully editable, we can't format a selection.
			return S_OK;
		}
	}
	*pfRet = true;

	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	Return true if we can apply overlay tags on the current selection.

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::get_CanFormatOverlay(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfRet);

	// We can't apply overlay tags if we can't apply character formatting.
	CheckHr(get_CanFormatChar(pfRet));
	if (!*pfRet)
		return S_OK;

	// Make sure all the paragraph boxes in the selection are the right type to accept overlays.
	VwParagraphBox * pvpboxLast = m_pvpbox;
	VwParagraphBox * pvpboxFirst = m_pvpbox;

	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
			pvpboxFirst = m_pvpboxEnd;
		else
			pvpboxLast = m_pvpboxEnd;
	}
	VwParagraphBox * pvpbox = pvpboxFirst;
	while (pvpbox)
	{
		if (dynamic_cast<VwOverlayTxtSrc *>(pvpbox->Source()) == NULL)
		{
			*pfRet = false;
			return S_OK;
		}
		if (pvpboxLast == pvpbox)
			return S_OK;
		pvpbox = dynamic_cast<VwParagraphBox *>(pvpbox->NextRealBox());
	}

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Get a selection equivalent to this but grown to a whole word.
	Currently does nothing if selection is not an IP. May enhance to
	grow to nearest boundary. Will not grow beyond confines of one
	string property. Selections that don't implement just answer null.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::GrowToWord(IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwTextSelection::GrowToWord(IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);
	if (!IsInsertionPoint())
		return S_OK;
	int ichMin;
	int ichLim;
	FindWordBoundaries(ichMin, ichLim);
	*ppsel = NewObj VwTextSelection(m_pvpbox, ichMin, ichLim, true);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Get a selection equivalent to the anchor or end point of the current selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::EndPoint(ComBool fEndPoint, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwTextSelection::EndPoint(ComBool fEndPoint, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);
	VwParagraphBox * pvpbox = m_pvpbox;
	if (fEndPoint && m_pvpboxEnd)
		pvpbox = m_pvpboxEnd;
	int ich = fEndPoint ? m_ichEnd : m_ichAnchor;
	bool fAssocPrev = m_fAssocPrevious;
	if (!IsInsertionPoint())
	{
		// Rather arbitrarily answer the direction towards the old selection.
		ComBool fEndBeforeAnchor;
		CheckHr(get_EndBeforeAnchor(&fEndBeforeAnchor));
		fAssocPrev = fEndPoint ? (!fEndBeforeAnchor) : (bool)fEndBeforeAnchor;
	}

	*ppsel = NewObj VwTextSelection(pvpbox, ich, ich, fAssocPrev);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwPictureSelection::EndPoint(ComBool fEndPoint, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);

	int ich = fEndPoint ? m_ichEndLog : m_ichAnchorLog;
	*ppsel = NewObj VwPictureSelection(m_plbox, ich, ich, m_fAssocPrevious);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	Lock a TsString for the duration of your existence, then unlock it.
	It is assumed that the client retains a reference count on the string.

	ENHANCE JohnT: If necessary, this could be made to hide the fact that LockText can't be
	used for a marshalled string. In that case we would add member variables for a BSTR,
	and if LockText fails, use get_Text and release the BSTR at the end.

	Hungarian: slk;
----------------------------------------------------------------------------------------------*/
class StringLocker
{
public:
	StringLocker(ITsString * ptss)
	{
		m_ptss = ptss;
		CheckHr(ptss->LockText(&m_pch, &m_cch));
	}
	~StringLocker()
	{
		m_ptss->UnlockText(m_pch);
	}
	ITsString * m_ptss;
	const OLECHAR * m_pch;
	int m_cch;
};

/*----------------------------------------------------------------------------------------------
	Compare the contents of two strings. Return the range (in the second) that is different.
	Both return values will be -1 if the source strings are the same.
	As a special case, if both strings are empty, and they differ only by different properties,
	the routine sets both return values to zero.
----------------------------------------------------------------------------------------------*/
void CompareStrings(ITsString * ptss1, ITsString * ptss2,
	int * pichwMinDiff, int * pichwLimDiff)
{
	int ichwRunMin1 = 0;
	int ichwRunMin2 = 0;
	int ichwRunLim1, ichwRunLim2 = 0;

	StringLocker slk1(ptss1);
	StringLocker slk2(ptss2);
	int cchwLen1 = slk1.m_cch;
	int cchwLen2 = slk2.m_cch;

	const OLECHAR * prgchw1; // used as pointers to start of runs
	const OLECHAR * prgchw2;

	*pichwMinDiff = -1; // not set
	*pichwLimDiff = -1;
	ichwRunMin1 = 0;
	ichwRunMin2 = 0;
	TsRunInfo tri1, tri2;
	ITsTextPropsPtr qttp1, qttp2;
	while (ichwRunMin1 < cchwLen1 && ichwRunMin2 < cchwLen2)
	{
		CheckHr(ptss1->FetchRunInfoAt(ichwRunMin1, &tri1, &qttp1));
		ichwRunMin1 = tri1.ichMin;
		ichwRunLim1 = tri1.ichLim;
		CheckHr(ptss2->FetchRunInfoAt(ichwRunMin2, &tri2, &qttp2));
		ichwRunMin2 = tri2.ichMin;
		ichwRunLim2 = tri2.ichLim;

		int cchwRun1 = ichwRunLim1 - ichwRunMin1;
		int cchwRun2 = ichwRunLim2 - ichwRunMin2;

		if (qttp1.Ptr() != qttp2.Ptr())
		{
			*pichwMinDiff = ichwRunMin2;
			break;
		}
		prgchw1 = slk1.m_pch + ichwRunMin1;
		prgchw2 = slk2.m_pch + ichwRunMin2;
		int ichw;
		for (ichw = 0; ichw < min(cchwRun1, cchwRun2); ichw++)
		{
			if (prgchw1[ichw] != prgchw2[ichw])
			{
				*pichwMinDiff = ichwRunMin2 + ichw;
				break;
			}
		}
		if (*pichwMinDiff != -1)
			break;
		if (cchwRun1 != cchwRun2)
		{
			*pichwMinDiff = ichwRunMin2 + ichw;
			break;
		}

		ichwRunMin1 = ichwRunLim1;
		ichwRunMin2 = ichwRunLim2;
	}

	if (*pichwMinDiff == -1)
	{
		if (cchwLen1 < cchwLen2)
		{
			*pichwMinDiff = cchwLen1;
			*pichwLimDiff = cchwLen2;
		}
		else if (cchwLen1 > cchwLen2)
		{
			*pichwMinDiff = cchwLen2;
			*pichwLimDiff = cchwLen2;
		}
		// else no differences...unless both strings empty with different props
		if (cchwLen1 == 0 && cchwLen2 == 0)
		{
			CheckHr(ptss1->FetchRunInfoAt(0, &tri1, &qttp1));
			CheckHr(ptss2->FetchRunInfoAt(0, &tri2, &qttp2));
			if (qttp1.Ptr() != qttp2.Ptr())
			{
				*pichwMinDiff = 0;
				*pichwLimDiff = 0;
			}
		}
		// else no diffs at all...leave both values -1.
		return;
	}

	if (*pichwMinDiff >= cchwLen1 && cchwLen1 <= cchwLen2)
	{
		*pichwLimDiff = cchwLen2;
		return;
	}

	ichwRunMin1 = cchwLen1 - 1;
	ichwRunMin2 = cchwLen2 - 1;
	// Loop invariant: the strings are the same from ichwRunMin2 + 1 to the end of ptss2
	// as from ichwRunMin1 + 1 to the end of ptss1.
	while (ichwRunMin1 >= 0 || ichwRunMin2 >= 0)
	{
		if (ichwRunMin2 >= 0)
		{
			// This handles a run inserted at the very start. We have no more characters
			// to compare in ptss1, so the limit in string 2 is the ones we already
			// know are the same.
			if (ichwRunMin1 < 0)
			{
				*pichwLimDiff = ichwRunMin2 + 1;
				break;
			}

			CheckHr(ptss1->FetchRunInfoAt(ichwRunMin1, &tri1, &qttp1));
			ichwRunMin1 = tri1.ichMin;
			ichwRunLim1 = tri1.ichLim;
		}
		else
		{
			*pichwLimDiff = 0;
			break;
		}
		if (ichwRunMin1 >= 0)
		{
			CheckHr(ptss2->FetchRunInfoAt(ichwRunMin2, &tri2, &qttp2));
			ichwRunMin2 = tri2.ichMin;
			ichwRunLim2 = tri2.ichLim;
		}
		else
		{
			*pichwLimDiff = ichwRunLim2;
			break;
		}

		int cchwRun1 = ichwRunLim1 - ichwRunMin1;
		int cchwRun2 = ichwRunLim2 - ichwRunMin2;

		if (qttp1.Ptr() != qttp2.Ptr())
		{
			*pichwLimDiff = ichwRunLim2;
			break;
		}

		prgchw1 = slk1.m_pch + ichwRunMin1;
		prgchw2 = slk2.m_pch + ichwRunMin2;
		int ichw1, ichw2;
		for (ichw1 = cchwRun1, ichw2 = cchwRun2; ichw1 > 0 && ichw2 > 0; )
		{
			ichw1--;
			ichw2--;
			if (prgchw1[ichw1] != prgchw2[ichw2])
			{
				*pichwLimDiff = ichwRunMin2 + ichw2 + 1;
				break;
			}
		}
		if (*pichwLimDiff != -1)
			break;
		if (cchwRun1 != cchwRun2)
		{
			*pichwLimDiff = ichwRunMin2 + ichw2;
			break;
		}

		ichwRunMin1 = ichwRunMin1 - 1;
		ichwRunMin2 = ichwRunMin2 - 1;
	}

	Assert(*pichwLimDiff != -1);

	if (cchwLen1 < cchwLen2 && *pichwLimDiff - *pichwMinDiff < cchwLen2 - cchwLen1)
		//	The length of stuff changed must be at least a big as the difference in size.
		*pichwLimDiff = *pichwMinDiff + (cchwLen2 - cchwLen1);
	else if (*pichwLimDiff < *pichwMinDiff)
		// The beginning offset can't be greater than the end offset.
		*pichwMinDiff = *pichwLimDiff;
}

/*----------------------------------------------------------------------------------------------
	Update a property.

	@param prootb
	@param hvo
	@param tag
	@param vnp
	@param pvvcEdit
	@param fragEdit
	@param ptsb
	@param pfOk
	@param pci if not null (default), record change info here instead of sending PropChanged.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::DoUpdateProp(VwRootBox * prootb, HVO hvo, PropTag tag, VwNoteProps vnp,
	IVwViewConstructor * pvvcEdit, int fragEdit, ITsStrBldr * ptsb, bool * pfOk)
{
	*pfOk = false; // only set true if we make it all the way (though failures throw exceptions)

#ifdef ENABLE_TSF
	VwTextStore * ptxs = prootb->TextStore();
#elif defined(MANAGED_KEYBOARDING)
	IViewInputMgr * pvim = prootb->InputManager();
#endif

	// Update the property.
	ITsStringPtr qtssNewSub;
	CheckHr(ptsb->GetString(&qtssNewSub));
	// At this point, the new string may not be normalized. But saving it to the database WILL
	// cause it to get normalized. If we just let it happen, we're in trouble, because that may
	// well change the length and hence, the proper position of the insertion point.
	// To see the effect, remove the next few lines, and type 132 while holding down alt.
	// This generates a-umlaut. Do this somewhere not at the end of a line, and then type space.
	// See it change to a - space - box.
	// The solution is to normalize it here, in a way that lets us figure the effect on the IP.
	// (If we aren't an IP, skip this...that's very unlikely.)
	int ichNormalizedAnchor = m_ichAnchor; // We'll adjust these if we need to change the orig.
	int ichNormalizedEnd = m_ichEnd;
	int ichNormalizedLim = m_ichLimEditProp;
	if (this->IsInsertionPoint())
	{
		int ichOffset = m_ichAnchor - m_ichMinEditProp;
		int * poffset = &ichOffset;
#ifdef ENABLE_TSF
		if (ptxs->IsCompositionActive())
		{
			ptxs->NoteCommitDuringComposition();
		}
		else
#elif defined(MANAGED_KEYBOARDING)
		ComBool fProcessed;
		CheckHr(pvim->OnUpdateProp(&fProcessed));
		if (!fProcessed)
#endif
		{
			// normal case...normalize
			ITsStringPtr qtssT;
			qtssT.Attach(qtssNewSub.Detach());
			CheckHr(qtssT->NfdAndFixOffsets(&qtssNewSub, &poffset, 1));
		}
		ichNormalizedAnchor = ichNormalizedEnd = ichOffset + m_ichMinEditProp;
		int cch;
		CheckHr(qtssNewSub->get_Length(&cch));
		ichNormalizedLim = m_ichMinEditProp + cch;
	}

	ISilDataAccessPtr qsda = prootb->GetDataAccess();
	if (!qsda)
		ThrowHr(WarnHr(E_FAIL));

	if (pvvcEdit &&
		vnp != kvnpStringProp && vnp != kvnpStringAltMember && vnp != kvnpUnicodeProp)
	{
		// The view constructor will handle the update. It is responsible to begin/end
		// a transaction if anything changed, and update timestamp on objects.
		ITsStringPtr qtssFixedVal;
		CheckHr(pvvcEdit->UpdateProp(this, hvo, tag, fragEdit,
			qtssNewSub, &qtssFixedVal));	// ERRORJOHN
		// There are behaviors needed when UpdateProp returns a modified string.
		// If we get a different string, update the text box as we would
		// normally do with qtssNewVal when not changing focus.
		ComBool fEq;
		CheckHr(qtssNewSub->Equals(qtssFixedVal, &fEq));
		m_ichAnchor = ichNormalizedAnchor;
		m_ichEnd = ichNormalizedEnd;
		m_ichLimEditProp = ichNormalizedLim;
		if (!fEq)
		{
			qtssNewSub = qtssFixedVal;
			// And, make sure the anchor etc is in range.
			int cch;
			CheckHr(qtssFixedVal->get_Length(&cch));
			m_ichLimEditProp = m_ichMinEditProp + cch;
			m_ichAnchor = min(m_ichAnchor, m_ichLimEditProp);
			m_ichEnd = min(m_ichEnd, m_ichLimEditProp);
			// If there's been a substitution, we want to continue typing in the properties
			// at the end of the string.
			CheckHr(qtssNewSub->get_PropertiesAt(cch, &m_qttp));
		}
	}
	else
	{
		m_ichAnchor = ichNormalizedAnchor;
		m_ichEnd = ichNormalizedEnd;
		m_ichLimEditProp = ichNormalizedLim;
		// If we are not dealing with a view constructor, it must be a standard prop type.
		// This code should stay in sync with that in VwNotifier::PropChanged.

#if WANTPORT // IVwOleDbDa has been removed, so what should be done here?
		// Check to see if the record has been edited by someone else.
		// TODO 1724 (PaulP):  This check needs to go in here somewhere, but I'm not sure
		// where and also what to do if the user says "No" and the method returns.
		IVwOleDbDaPtr qodde;
		HRESULT hrDbCache = E_FAIL;
		hrDbCache = qsda->QueryInterface(IID_IVwOleDbDa, (void **) &qodde);
		if (hrDbCache == S_OK)
		{
			HRESULT hrTemp = E_FAIL;
			if ((hrTemp = qodde->CheckTimeStamp(hvo)) != S_OK)
			{
				qsda->PropChanged(prootb, kpctNotifyMeThenAll, hvo, tag, 0, 0, 0);
				*pfOk = false;
				return;
			}
		}
#endif

		switch(vnp)
		{
		default:
			// Not a type of property we have an automatic way of building.
			Assert(false);
			ThrowInternalError(E_UNEXPECTED);
			break;
		case kvnpStringProp:
			{ // BLOCK
				ITsStringPtr qtssOld;
				CheckHr(qsda->get_StringProp(hvo, tag, &qtssOld));
				int ichMinDiff, ichLimDiff; // in qtssNewSub
				CompareStrings(qtssOld, qtssNewSub, &ichMinDiff, &ichLimDiff);
#ifdef MANAGED_KEYBOARDING
				ComBool fDoingRecommit;
				CheckHr(pvim->get_IsEndingComposition(&fDoingRecommit));
#endif

				if (ichMinDiff >= 0
#ifdef ENABLE_TSF
					 || ptxs->IsDoingRecommit() // there's some difference
#elif defined(MANAGED_KEYBOARDING)
					 || fDoingRecommit // there's some difference
#endif
					)
				{
					// Make the new string the value of the property
					CheckHr(qsda->SetString(hvo, tag, qtssNewSub));
				}
			}
			break;
		case kvnpUnicodeProp:
			{ // BLOCK
				SmartBstr sbstrOld;
				CheckHr(qsda->get_UnicodeProp(hvo, tag, &sbstrOld));
				SmartBstr sbstrNew;
				CheckHr(qtssNewSub->get_Text(&sbstrNew));
#ifdef WIN32
				if (wcscmp(sbstrOld.Chars(), sbstrNew.Chars()) != 0)
#else
				if (u_strcmp(sbstrOld.Chars(), sbstrNew.Chars()) != 0)
#endif
				{
					// There's a difference.
					CheckHr(qsda->put_UnicodeProp(hvo, tag, sbstrNew));
				}
			}
			break;
		case kvnpIntProp:
			// Parse the string into an integer, update the prop.
			// ENHANCE: JohnT: use an encoding-specific parser, if we one day use
			// an encoding-specific display method.
			int nVal;
			const wchar * pch;
			int cch;

			CheckHr(qtssNewSub->LockText(&pch, &cch));

			wchar buf[50];
			if (cch >= 50)
				cch = 49;

			wcsncpy(buf, pch, cch);

			buf[49] = 0; // in case there really were 50 + chars
#ifdef WIN32
			nVal = _wtoi(buf);
#else
			{
				UErrorCode status = U_ZERO_ERROR;
				// value should be 0 if we encounter an error
				Formattable result(0);
				NumberFormat* nf = NumberFormat::createInstance(status);
				nf->parse(buf, result, status);
				nVal = result.getLong();
			}
#endif
			CheckHr(qtssNewSub->UnlockText(pch));
			int nValOld;
			CheckHr(qsda->get_IntProp(hvo, tag, &nValOld));
			if (nValOld != nVal)
			{
				CheckHr(qsda->SetInt(hvo, tag, nVal));
			}
			break;
		case kvnpStringAltMember:
		case kvnpStringAlt:
		case kvnpStringAltSeq:

			if (fragEdit)
			{
				// frag is actually an writing system, indicating which alternative to replace.
				// The replacement string is just that alternative.
				ITsStringPtr qtssOld;
				CheckHr(qsda->get_MultiStringAlt(hvo, tag, fragEdit, &qtssOld));
				ComBool fEqual;
				CheckHr(qtssOld->Equals(qtssNewSub, &fEqual));
#ifdef MANAGED_KEYBOARDING
				ComBool fDoingRecommit;
				CheckHr(pvim->get_IsEndingComposition(&fDoingRecommit));
#endif

				if (!fEqual
#ifdef ENABLE_TSF
					 || ptxs->IsDoingRecommit()
#elif defined(MANAGED_KEYBOARDING)
					 || fDoingRecommit
#endif
					)
				{
						CheckHr(qsda->SetMultiStringAlt(hvo, tag, fragEdit, qtssNewSub));
					}
			} else
			{
				// ENHANCE: JohnT: figure how to handle this. We may need some more notifier
				// structure.
				// Get the full alternation
				ITsMultiStringPtr qtms;
				CheckHr(qsda->get_MultiStringProp(hvo, tag, &qtms));
				Assert(false);
				ThrowHr(WarnHr(E_NOTIMPL));
			}
			break;
		} // switch (cpt)
	}

	*pfOk = true;
}

/*----------------------------------------------------------------------------------------------
	Focus loss: return false to indicate that focus loss is vetoed. The caller should then
	SetFocus back to this window. If this call is happening because a new selection is being
	made in the containing root box, pass the new selection in pvwselNew; otherwise pass null.

	@param pvwselNew
	@param pfOk
----------------------------------------------------------------------------------------------*/
void VwTextSelection::LoseFocus(IVwSelection * pvwselNew, ComBool * pfOk)
{
	AssertPtr(pfOk);
	AssertPtrN(pvwselNew);

	//StrAppBuf strb;
	//strb.Format("Lose:%d\n", this);
	//OutputDebugString(strb.Chars());

	*pfOk = true;

	Assert(!m_qtsbProp); // Any pending edit should have already been committed.

	//// See if the new selection is another text selection. If not, we need to validate the
	//// changes with a call to Commit().
	//VwTextSelectionPtr qtsel;
	//HRESULT hr = S_OK;
	//if (pvwselNew)
	//	IgnoreHr(hr = pvwselNew->QueryInterface(CLSID_VwTextSelection, (void **) &qtsel));
	//if (!pvwselNew || FAILED(hr))
	//{
	//	Commit(pfOk);
	//	return;
	//}

	//// We know that the new selection is a VwTextSelection. Is it in the same text box? If not
	//// we need to validate.
	//if (m_pvpbox != qtsel->m_pvpbox)
	//{
	//	Commit(pfOk);
	//	return;
	//}

	//// If the new one is a multi-para selection we'd better commit, as it can't be entirely in
	//// this prop.
	//if (qtsel->m_pvpboxEnd)
	//{
	//	Commit(pfOk);
	//	return;
	//}

	//int ichAnchorNew = qtsel->m_ichAnchor;
	//if (ichAnchorNew < m_ichMinEditProp || ichAnchorNew > m_ichLimEditProp)
	//{
	//	Commit(pfOk);
	//	return;
	//}

	//int ichEndNew = qtsel->m_ichEnd;
	//if (ichEndNew < m_ichMinEditProp || ichEndNew > m_ichLimEditProp)
	//{
	//	Commit(pfOk);
	//	return;
	//}

	//if (ichAnchorNew == ichEndNew)
	//{
	//	// It's an IP: its fAssocPrev affects whether it is in the same property.
	//	if ((qtsel->m_fAssocPrevious && ichAnchorNew == m_ichMinEditProp) ||
	//		(!qtsel->m_fAssocPrevious && ichAnchorNew == m_ichLimEditProp))
	//	{
	//		Commit(pfOk);
	//		return;
	//	}
	//}

	//// OK, the new selection can edit the same property. Copy over all the editing-related vars
	//// and then clear all our own variables (just to facilitate freeing things). This would be
	//// same thing as calling StartEditing();
	//qtsel->m_qtsbProp = m_qtsbProp;
	//m_qtsbProp.Clear();

	//// Copy the Prop min and lim.
	//qtsel->m_ichMinEditProp = m_ichMinEditProp;
	//qtsel->m_ichLimEditProp = m_ichLimEditProp;

	//// Don't copy the fAssocPrevious status. It is part of the real information in the new sel.
	//// Copying can cause real problems, for example if the new sel is at the start of the line,
	//// so m_fAssocPrevious must be false, but in the old selection it was true.
	////qtsel->m_fAssocPrevious = m_fAssocPrevious;

	//// Copy other member variables set in the call to EditableSubstringAt.
	//qtsel->m_hvoEdit = m_hvoEdit;
	//qtsel->m_tagEdit = m_tagEdit;
	//qtsel->m_qvvcEdit = m_qvvcEdit;
	//qtsel->m_fragEdit = m_fragEdit;
	//qtsel->m_qanote = m_qanote;
	//m_qanote.Clear();
	//qtsel->m_iprop = m_iprop;
	//qtsel->m_itssProp = m_itssProp;
	//qtsel->m_vnp = m_vnp;

	//qtsel->m_hvoParaOwner = m_hvoParaOwner;
	//qtsel->m_tagParaProp = m_tagParaProp;
	//qtsel->m_pnoteParaOwner = m_pnoteParaOwner;
	//qtsel->m_ipropPara = m_ipropPara;
	//qtsel->m_ihvoFirstPara = m_ihvoFirstPara;
	//qtsel->m_ihvoLastPara = m_ihvoLastPara;

	//// Allow the new selection to take over responsibility for committing changes
}

/*----------------------------------------------------------------------------------------------
	Return true if this selection is messed up by deleting the specified box.

	@param pbox
	@param pboxReplacement
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::RuinedByDeleting(VwBox * pbox, VwBox * pboxReplacement)
{
	if (pbox == m_pvpbox)
		return true;
	if (m_pvpboxEnd && m_pvpboxEnd == pbox)
		return true;
	return false;
}

bool VwPictureSelection::RuinedByDeleting(VwBox * pbox, VwBox * pboxReplacement)
{
	return pbox == m_plbox;
}

void VwTextSelection::GetFirstAndLast(VwParagraphBox ** ppvpboxFirst,
	VwParagraphBox ** ppvpboxLast, int * pichFirst, int * pichLast)
{
	*ppvpboxFirst = *ppvpboxLast = m_pvpbox;
	*pichFirst = m_ichAnchor;
	*pichLast = m_ichEnd;
	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
		{
			*ppvpboxFirst = m_pvpboxEnd;
			*pichFirst = m_ichEnd;
			*pichLast = m_ichAnchor;
		}
		else
		{
			*ppvpboxLast = m_pvpboxEnd;
		}
	}
	else if (m_ichAnchor > m_ichEnd)
	{
		*pichFirst = m_ichEnd;
		*pichLast = m_ichAnchor;
	}
}

// Submit to the root site a request that, at the end of the UOW, a selection should be
// made that is like the current one, except in the indexed paragraph at the specified ich.
void VwTextSelection::RequestSelectionAfterUow(int ihvoPara, int ich, bool fAssocPrev)
{
	Vector<VwSelLevInfo> vsli;
	int clev;
	CheckHr(CLevels(false, &clev));
	vsli.Resize(clev - 1);
	int ihvoRoot, tagTextProp, cpropPrevious, ichAnchor, ichEnd, ws, ihvoEnd;
	ComBool fAssocPrevDummy;
	ITsTextPropsPtr qttp;
	CheckHr(AllTextSelInfo(&ihvoRoot, clev - 1, vsli.Begin(), &tagTextProp, &cpropPrevious, &ichAnchor, &ichEnd,
		&ws, &fAssocPrevDummy, &ihvoEnd, &qttp));
	// Change the most local SelLevInfo to indicate the appropriate paragraph.
	vsli[0].ihvo = ihvoPara;
	CheckHr(m_qrootb->Site()->RequestSelectionAtEndOfUow(m_qrootb, ihvoRoot, clev - 1, vsli.Begin(),
		tagTextProp, 0, ich, ws, fAssocPrevDummy, qttp));
}

/*----------------------------------------------------------------------------------------------
	Try to set things up so that the selection is an insertion point where we can actually
	insert. If successful, all member variables are set up for editing, UNLESS we deleted
	a complex range, in which case we will have committed and deleted it, but will NOT
	be ready for more edits.

	Return true if successful; if it returns false, edit should be aborted.

	Assumes the selection is hidden and the caller will show it again, whether or not
	we succeed.

	Save the properties that should be
	applied to typed text in m_qttp (where they will be applied to the text typed).

	@param ppttp
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::DeleteRangeAndPrepareToInsert()
{
	VwRootBox * prootb = m_pvpbox->Root();
	VwParagraphBox * pvpboxStart;
	VwParagraphBox * pvpboxEnd;
	// initially the start of the selection relative to the para, gets adjusted to be relative to its last string,
	// if a multi-para selection.
	int ichMin;
	int ichLim;
	GetFirstAndLast(&pvpboxStart, &pvpboxEnd, &ichMin, &ichLim);
	if (!IsInsertionPoint())
	{
		// Save the properties of the first deleted character, unless we delete the
		// whole of the first paragraph, in which case policy is to keep the
		// properties of the first character of the last paragraph.
		if (!m_qttp)
		{
			ITsStringPtr qtss;
			if (ichMin == 0)
			{
				pvpboxEnd->Source()->StringAtIndex(0, &qtss);
				if (qtss)
					CheckHr(qtss->get_PropertiesAt(0, &m_qttp));
			}
			else
			{
				int ichMinTss;
				int ichLimTss;
				VwPropertyStorePtr qzvps;
				int itss;
				pvpboxStart->Source()->StringFromIch(ichMin, false, &qtss, &ichMinTss,
					&ichLimTss, &qzvps, &itss);
				if (qtss)
					CheckHr(qtss->get_PropertiesAt(ichMin - ichMinTss, &m_qttp));
			}
		}
	}
	VwDelProbType dpt = IsProblemSelection();
	if (dpt != kdptNone)
	{
		// If OnProblemDeletion is not implemented, we want the default behavior.
		// Any initial value other than kdprAbort will do, but Fail is most like 'not implemented'.
		VwDelProbResponse dpr = kdprFail;
		bool fWasWorking = m_csCommitState == kcsWorking;
		m_csCommitState = kcsNormal; // further commit allowed during OnProblemDeletion
		HRESULT hr;
		IgnoreHr(hr = prootb->Site()->OnProblemDeletion(this, dpt, &dpr));
		if (fWasWorking)
			m_csCommitState = kcsWorking;
		if (FAILED(hr) && hr != E_NOTIMPL && hr != E_FAIL)
			ThrowHr(hr);

		if (dpr == kdprAbort)
		{
			return false;
		}
		if (this != prootb->Selection())
		{
			// If the current selection has changed, we will either abort (if there is no longer
			// a selection), or retry the operation using the new selection, which will do all
			// needed checks.
			return prootb->Selection() != NULL;
		}
		dpt = IsProblemSelection();
		switch(dpt)
		{
		case kdptNone:
			break;
		case kdptReadOnly:
			return false;
		case kdptComplexRange:
			// It's STILL a complex range!
			ShrinkSelection(); // Cut it down to size.
			if (IsProblemSelection() != kdptNone)
			{
				return false; // probably read only
			}
			break;
		default:
			Assert(false);
			break;
		}
	}
	// At this point the selection is either already an IP, or something we know how
	// to delete.
	StartEditing();
	if (IsInsertionPoint())
		return true;
	if (m_pvpboxEnd) // multi-paragraph selection
	{
		// Do NOT Commit here; with a multi-para selection we cannot have any pending edits,
		// and the variables are not in the right state. But we don't want to look as if we're
		// ready to edit (though on this branch, this selection should never be reused).
		m_qtsbProp.Clear();
		// We have a multi-paragraph delete. Our policy is to keep the (properties of) the
		// first paragraph, unless the whole of that paragrah is selected, in which case we
		// keep the (properties of) the last.

		// StartEditing will allow a multi-paragraph selection only if the whole of the last
		// paragraph is a single string and the selection begins in the last string of the
		// first paragraph. So, our first step is to replace what is selected in the first
		// paragraph's final string with what isn't selected in the last.  Then we delete
		// the following paragraphs.
		ITsStringPtr qtssStart;
		// "LastStart" variables refer to the last substring of the start paragraph.
		int itssLastStart = pvpboxStart->Source()->CStrings() - 1;
		int ichMinLastStart = pvpboxStart->Source()->IchStartString(itssLastStart);
		pvpboxStart->Source()->StringAtIndex(itssLastStart, &qtssStart);
		ichMin -= ichMinLastStart; // ichMin is relative to the string now.
		int cchStart;
		CheckHr(qtssStart->get_Length(&cchStart));

		ITsStringPtr qtssEnd;		// the part of the end para that survives.
		pvpboxEnd->Source()->StringAtIndex(0, &qtssEnd);
		int cchEnd;
		CheckHr(qtssEnd->get_Length(&cchEnd));

		// Now delete the superfluous paragraphs.
		ISilDataAccessPtr qsda = m_pvpbox->Root()->GetDataAccess();
		if (!qsda)
			ThrowHr(WarnHr(E_FAIL));

		// We will mess up the paragraphs we are contained in, request a new selection after UOW completes.
		RequestSelectionAfterUow(m_ihvoFirstPara, ichMin, false);

		int ihvoFirstDel = m_ihvoFirstPara + 1;
		int ihvoLastDel = m_ihvoLastPara;

		if (ichMin == 0 && ichMinLastStart == 0)
		{
			// Keep the last paragraph, delete the first one entirely, along with any intermediate ones.
			// There is nothing to keep from it.
			ihvoFirstDel--;
			ihvoLastDel--;
			// Delete whatever is selected in the last paragraph, which will be from the start to ichLim.
			if (ichLim > 0)
			{
				HVO hvoLast;
				CheckHr(qsda->get_VecItem(m_hvoParaOwner, m_tagParaProp, m_ihvoLastPara, &hvoLast));
				ITsStrBldrPtr qtsb;
				CheckHr(qtssEnd->GetBldr(&qtsb));
				CheckHr(qtsb->ReplaceRgch(0, ichLim, NULL, 0, NULL));
				ITsStringPtr qtssNewLast;
				CheckHr(qtsb->GetString(&qtssNewLast));
				CheckHr(qsda->SetString(hvoLast, m_tagEdit, qtssNewLast));
			}
		}
		else
		{
			// Keep the first paragraph, delete the last one; copy the surviving part
			// of the last one into the first one.
			HVO hvoDest;
			CheckHr(qsda->get_VecItem(m_hvoParaOwner, m_tagParaProp,
				m_ihvoFirstPara, &hvoDest));
			ITsStrBldrPtr qtpbNewFirst;
			CheckHr(qtssStart->GetBldr(&qtpbNewFirst));
			if (ichMin < cchStart)
			{
				CheckHr(qtpbNewFirst->ReplaceTsString(ichMin, cchStart, NULL));
				// The IP is now after the ichMin characters from the original first para.

				ITsStringPtr qtssNewFirst;
				CheckHr(qtpbNewFirst->GetString(&qtssNewFirst));
				CheckHr(qsda->SetString(hvoDest, m_tagEdit, qtssNewFirst));
			}

			// We are moving some text, so we need to notify the SDA.
			HVO hvoSource;
			CheckHr(qsda->get_VecItem(m_hvoParaOwner, m_tagParaProp,
				ihvoLastDel, &hvoSource));

			CheckHr(qsda->MoveString(hvoSource, m_tagEdit, 0, ichLim, cchEnd,
				hvoDest, m_tagEdit, 0, ichMin, false));
		}

		// Destroy the actual paragraph objects.
		// OPTIMIZE JohnT: create a way to delete several in one go.This
		for (int ihvo = ihvoLastDel; ihvoFirstDel <= ihvo; ihvo--)
		{
			HVO hvoDel;
			CheckHr(qsda->get_VecItem(m_hvoParaOwner, m_tagParaProp,
				ihvo, &hvoDel));
			CheckHr(qsda->DeleteObjOwner(m_hvoParaOwner, hvoDel, m_tagParaProp, ihvo));
		}
	}
	else if (m_ichAnchor != m_ichEnd)
	{
		// Delete a range within a single paragraph.
		ichMin = min(m_ichAnchor, m_ichEnd);
		ichLim = max(m_ichAnchor, m_ichEnd);

		//ITsTextPropsPtr qttp; // new props, irrelevant unless string is empty.
		//if (ichMin == m_ichMinEditProp && ichLim > m_ichMinEditProp && ichLim == m_ichLimEditProp)
		//{
		//	// Deleting the whole string, make sure an empty string doesn't keep external link properties.
		//	CheckHr(m_qtsbProp->get_PropertiesAt(0, &qttp));
		//	ITsPropsBldrPtr qtpb;
		//	CheckHr(qttp->GetBldr(&qtpb));
		//	SmartBstr sbstrEmpty(L"");
		//	CheckHr(qtpb->SetStrPropValue(ktptObjData, sbstrEmpty));
		//	SmartBstr sbstrStyle;
		//	CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
		//	if (wcscmp(sbstrStyle.Chars(), L"External Link") == 0 || // Still used in DN?
		//		wcscmp(sbstrStyle.Chars(), L"Hyperlink") == 0) // Now used in TE & FLEx
		//	{
		//		// Also remove the external link style
		//		CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, sbstrEmpty));
		//	}
		//	CheckHr(qtpb->GetTextProps(&qttp));
		//	m_qttp = qttp; // prevent immediate subsequent typing from restoring the hot link
		//}
		CheckHr(m_qtsbProp->ReplaceRgch(ichMin - m_ichMinEditProp,
			ichLim - m_ichMinEditProp, NULL, 0, m_qttp));
		m_ichAnchor = m_ichEnd = ichMin;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Clear any style properties that should not be applied to newly typed text
----------------------------------------------------------------------------------------------*/
void VwTextSelection::CleanPropertiesForTyping()
{
	if (!m_qttp)
		return;

	// Clean out the object data if the data is not for kodtExternalPathName
	SmartBstr sbstrObjData;
	CheckHr(m_qttp->GetStrPropValue(ktptObjData, &sbstrObjData));
	if (sbstrObjData.Length())
	{
		const OLECHAR * pch = sbstrObjData.Chars();
		if (pch[0] != kodtExternalPathName)
		{
			// Get a props builder from m_qttp
			ITsPropsBldrPtr qtpb;
			CheckHr(m_qttp->GetBldr(&qtpb));
			// Clear the object data
			SmartBstr sbstrEmpty(L"");
			CheckHr(qtpb->SetStrPropValue(ktptObjData, sbstrEmpty));
			// set the props back into m_qttp
			CheckHr(qtpb->GetTextProps(&m_qttp));
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point left one character, or collapse the selection to its left edge.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical	- true if we want to move the arrow to operate logically, false if moving
					physically (visually)
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::LeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping)
{
	if (fLogical)
	{
		if (m_pvpbox->RightToLeft())
			return ForwardArrow(pvg, fSuppressClumping);
		else
			return BackwardArrow(pvg, fSuppressClumping);
	}
	else
	{
		return PhysicalArrow(pvg, false, fSuppressClumping);
	}
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point right one character, or collapse the selection to its right edge.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical	- true if we want to move the arrow to operate logically,
					false if moving physically (visually)
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::RightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping)
{
	if (fLogical)
	{
		if (m_pvpbox->RightToLeft())
			return BackwardArrow(pvg, fSuppressClumping);
		else
			return ForwardArrow(pvg, fSuppressClumping);
	}
	else
	{
		return PhysicalArrow(pvg, true, fSuppressClumping);
	}
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to a physically adjacent valid (editable) position.

	Note: this behavior can be pretty subtle. If you mess with it, please switch Data Notebook
	to use physical movement (Make the ComplexKeyBehavior method in AfVwWnd.h return 0.) and
	check it out thoroughly.

	Here are some things to look for:

	1. It should move cleanly past the fish picture in TestLangProj record 1. It should not
	skip past the space on either side of the fish: it should be possible to get to the position
	immediately adjacent to the fish from either direction.

	2. It should move cleanly past the grey bars used to separate anthropology categories. It
	should not jump from one category to one character into the next one.

	3. It should move over hot links (e.g., to other records in the supporting evidence field)
	in one step.

	4. It should move from one paragraph to the next, and one line to the next in the same
	paragraph, without jumping from the end of one line to one character into the next line.

	5. All the above should also work with RTL text.

	6. Arrange a mixed-direction paragraph so that some upstream text has a line-break in the
	middle, and make sure you can move smoothly through both lines.

	7. Arrange a mixed-direction RTL paragraph so there is upstream text at the end (left)
	of the first line. Use right arrow to move into the paragraph from the previous paragraph.
	The IP should end up at the extreme left of the line, not somewhere in the middle.

	8. Make sure you can move through the pig latin and stacked diacritic text in TLP record 2.

	9. Make sure you can move through the surrogate-pair data in TLP record 2, paying special
	attention to line and font boundary movements.

	Known problems (as of 2-20-04):

	1. In TestLangProj record 2 there is English "We hope it will work" embedded in a Divehi
	paragraph. If you arrow to the right to the end of it, the IP appears to stick: it stays
	before the following period for two right-key presses. Apparently Uniscribe considers
	the period to be RTL (since the whole paragraph is), and so the position after it is
	physically the same as the one after the k in "work".

	2. In TestLangProj record 2, in the paragraph right under the brown Gothic text, typically
	near the left of the second line, there is an upstream 4-digit numver. The IP moves through
	it in logical order rather than visual.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fRight - true if moving to the right, false if moving to the left
	@param fSuppressClumping - true to move by single characters, not clumps.
		Not fully implemented for physical movement.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::PhysicalArrow(IVwGraphics * pvg, bool fRight, bool fSuppressClumping)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return false;

	m_xdIP = -1;
	if (m_pvpboxEnd && m_pvpbox != m_pvpboxEnd)
	{
		//	Range selection spans several paragraphs. Use the directionality of the anchor
		//	paragraph to interpret physical direction.
		if (m_pvpbox->RightToLeft() == fRight)
			return BackwardArrow(pvg, fSuppressClumping);
		else
			return ForwardArrow(pvg, fSuppressClumping);
	}

	VwStringBox * psbox = GetStringBox(m_ichAnchor, m_pvpbox, m_fAssocPrevious);
	// Account for possibility that IP is not in a visible part of any box.
	// This can happen in concordance when headword column is made very narrow.
	// REVIEW JohnT (TomB): Should we handle this in a more elegant way?
	if (!psbox)
		return false;
	ILgSegmentPtr qseg = psbox->Segment();

	if (m_ichAnchor != m_ichEnd)
	{
		//	Range selection within a single paragraph.
		VwStringBox * psboxEnd = GetStringBox(m_ichEnd, m_pvpbox, m_fAssocPrevious);
		ComBool fRtl; // directionality for interpreting physical direction
		if (psbox == psboxEnd)
			CheckHr(qseg->get_RightToLeft(psbox->IchMin(), &fRtl));
		else
			fRtl = m_pvpbox->RightToLeft();
		if (fRtl == fRight)
			return BackwardArrow(pvg, fSuppressClumping);
		else
			return ForwardArrow(pvg, fSuppressClumping);
	}

	//	Insertion point

	// New anchor in rendered character units.
	int ichNewAnchor = m_pvpbox->Source()->LogToRen(m_ichAnchor);
	ComBool fResult;
	ComBool fAssocPrev = m_fAssocPrevious;
	VwParagraphBox * pvpbox = m_pvpbox;

	// Set this before any place that we can loop. This makes sure that we keep going
	// in the direction we originally decide, whatever obstacles of non-editable
	// paragraphs and the like we encounter along the way. A value of 1 means we are
	// moving forward through the document, when that is an issue.
	int nDir = (pvpbox->RightToLeft() == fRight) ? -1 : 1;

LNextEditable:

	//	Ask segment to figure it out, if possible.
	int ichMinPrevBox;
	CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichNewAnchor, &fAssocPrev,
		fRight, false, &fResult));
	if (fResult)
	{
		// Try again if this isn't a different logical character position at all
		// (for example, in the middle of a hot link).
		int ichNewLogAnchor = pvpbox->Source()->RenToLog(ichNewAnchor);
		if (ichNewLogAnchor == m_ichAnchor)
			goto LNextEditable;
		// Also move further if we can't edit at this position.
		if (!IsEditable(ichNewLogAnchor, pvpbox, fAssocPrev))
			goto LNextEditable;
		m_ichAnchor = ichNewLogAnchor;
		m_ichEnd = ichNewLogAnchor;
		m_ichAnchor2 = -1;
		m_pvpbox = pvpbox;
		m_fAssocPrevious = (bool)fAssocPrev;
		m_qttp = NULL;
		m_rcBounds.Clear();
		m_pvpbox->Root()->NotifySelChange(ksctSamePara);
		return true;
	}
	// We could move no further in the current segment/string box. If the next one
	// is adjacent, we will move one character (or so) into it, using a special
	// parameter to ArrowKeyPosition. If it is on a different line, or if some special
	// non-text box intervenes, we just want to be at the start or end of the segment,
	// depending on the direction of the segment and paragraph.
	ichMinPrevBox = psbox->IchMin();
	bool fFoundNonStringBox = false;

	//	Find an adjacent box in the same paragraph.
	BoxVec vboxInPara;
	pvpbox->GetBoxesInPhysicalOrder(vboxInPara);
	VwBox * pboxTmp = dynamic_cast<VwBox *>(psbox);
	for (;;)
	{
		pboxTmp = pvpbox->GetAdjPhysicalBox(pboxTmp, vboxInPara, nDir);
		if (!pboxTmp)
			break;
		VwStringBox * psboxTmp = dynamic_cast<VwStringBox *>(pboxTmp);
		if (!psboxTmp)
		{
			fFoundNonStringBox = true;
			continue;
		}
		bool fPutOnBoundary = psbox->Top() + psbox->Ascent() != psboxTmp->Top() + psboxTmp->Ascent()
				|| fFoundNonStringBox;
		psbox = psboxTmp;
		qseg = psbox->Segment();
		int dichSeg;
		CheckHr(qseg->get_Lim(psbox->IchMin(), &dichSeg));
		ichNewAnchor = psbox->IchMin(); // default if empty.
		if (dichSeg > 0)
		{
			// Move into the segment from the appropriate direction. If this fails, try another
			// segment.
			CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichNewAnchor, &fAssocPrev,
				fRight, true, &fResult));
			if (!fResult)
				continue;
			// If this box is on a different baseline, or the last box was a non-string,
			// move back to put IP at the segment boundary.
			if (fPutOnBoundary)
			{
				CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichNewAnchor, &fAssocPrev,
					!fRight, false, &fResult));
				if (!fResult)
					continue;
			}
		}
		// If it's not editable we just continue our main loop.
		int ichNewLogAnchor = pvpbox->Source()->RenToLog(ichNewAnchor);
		if (!IsEditable(ichNewLogAnchor, pvpbox, fAssocPrev))
			goto LNextEditable;
		// got something we can use! Do it.
		m_ichAnchor = ichNewLogAnchor;
		m_ichEnd = m_ichAnchor;
		m_ichAnchor2 = -1;
		m_pvpbox = pvpbox;
		m_fAssocPrevious = (bool)fAssocPrev;
		m_qttp = NULL;
		m_rcBounds.Clear();
		m_pvpbox->Root()->NotifySelChange(ksctSamePara);
		return true;
	}

	// If we drop out of the loop above, there's no postion in the right direction in
	// the current paragraph.
	//	Find a position in an adjacent paragraph. First get a selection in the para, then
	//	Use the segment routines to get one at the appropriate edge.
	VwParagraphBox * pvpboxNew = pvpbox;

	int dichSegLim;
	CheckHr(qseg->get_Lim(psbox->IchMin(), &dichSegLim));
	ichNewAnchor = (pvpbox->RightToLeft() == fRight) ?
		psbox->IchMin() :
		psbox->IchMin() + dichSegLim;
	while (pvpboxNew == pvpbox)
	{
		int ichTmp = (pvpbox->RightToLeft() == fRight) ?
			BackOneChar(ichNewAnchor, pvpbox, fAssocPrev, pvg, &pvpboxNew, fSuppressClumping) :
			ForwardOneChar(ichNewAnchor, pvpbox, fAssocPrev, pvg, &pvpboxNew, fSuppressClumping);
		if (pvpboxNew == pvpbox && ichTmp == ichNewAnchor)
			return false; // No adjustment possible in desired direction.
		ichNewAnchor = ichTmp;
	}
	// OK, a selection in pvpboxNew is possible. Now try to make the best
	// initial selection there.
	pvpbox = pvpboxNew;
	VwBox * pboxStart;
	BoxVec vboxOneLine;
	VwBox * pboxNextLine = pvpbox->GetALine(pvpbox->FirstBox(), vboxOneLine); // first line
	if (nDir == 1)
	{
		// moving forward, want the left or right box on the first line.
		pboxStart = fRight ? vboxOneLine[0] : *vboxOneLine.Top();
	}
	else
	{
		// moving back, want the left or right box on the last line.
		while (pboxNextLine)
			pboxNextLine = pvpbox->GetALine(pboxNextLine, vboxOneLine);
		pboxStart = fRight ? vboxOneLine[0] : *vboxOneLine.Top();
	}
	vboxInPara.Clear();
	pvpbox->GetBoxesInPhysicalOrder(vboxInPara);
	while ((psbox = dynamic_cast<VwStringBox *>(pboxStart)) == 0 && pboxStart != NULL)
		pboxStart = pvpboxNew->GetAdjPhysicalBox(pboxStart, vboxInPara, nDir);
	if (psbox == NULL)
		return false; // should not happen, Back/ForwardOneChar found a useable string box.

	qseg = psbox->Segment();

	// Now we want to move into this segment from the appropriate direction. This is a bit
	// of a trick...we move one character into it, then move one character in the opposite
	// direction to get back to the boundary. Is this better or worse than the strategy
	// used above of using fRight and the segment direction to decide whether to start
	// at the min or lim of the segment?
	// Check for an empty segment since it won't be possible to move within it. Note that
	// ArrowKeyPosition returns false if the segment is empty, even if we are moving into it.
	CheckHr(qseg->get_Lim(psbox->IchMin(), &dichSegLim));
	ichNewAnchor = psbox->IchMin();
	if (dichSegLim > 0)
	{
		CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichNewAnchor, &fAssocPrev,
			fRight, true, &fResult));
		if (!fResult)
			return false;	// Give up.

		// Move back out to the edge of the segment.
		CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichNewAnchor, &fAssocPrev,
			!fRight, false, &fResult));
	}
	int ichNewLogAnchor = pvpbox->Source()->RenToLog(ichNewAnchor);
	if (!IsEditable(ichNewLogAnchor, pvpbox, fAssocPrev))
		goto LNextEditable;

	m_ichAnchor = ichNewLogAnchor;
	m_ichEnd = ichNewLogAnchor;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_fAssocPrevious = (bool)fAssocPrev;
	m_qttp = NULL;
	m_rcBounds.Clear();
	m_pvpbox->Root()->NotifySelChange(ksctDiffPara);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the next valid position in a logically backward direction.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::BackwardArrow(IVwGraphics * pvg, bool fSuppressClumping)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return false;

	VwSelChangeType nHowChanged = ksctSamePara; // default same para

	bool fHadRangeSelected = false;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
		fHadRangeSelected = true;
	}

	if (m_ichAnchor != m_ichEnd)
	{
		fHadRangeSelected = true;
	}

	LoseSelection(true);
	m_fAssocPrevious = CalcAssocPrevForBackArrow(m_ichEnd, m_pvpbox, pvg);

	// only move if we did not have a range selected
	if(!fHadRangeSelected)
	{
		bool fWasEditable = IsEditable(m_ichEnd, m_pvpbox, m_fAssocPrevious);
		VwParagraphBox * pvpboxNew;
		int ichNew = BackOneChar(m_ichEnd, m_pvpbox, m_fAssocPrevious, pvg, &pvpboxNew, fSuppressClumping);
		bool fAssocPrev = CalcAssocPrevForBackArrow(ichNew, pvpboxNew, pvg);
		int ich = -1;
		VwParagraphBox * pvpbox;
		int cParaLim = 0;
		CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
		int cPara = 0;
		// If we weren't in editable text to start with, then we assume that any selection (even a
		// non-editable one) is fine
		if (fWasEditable)
		{
			while (!IsEditable(ichNew, pvpboxNew))
			{
				m_fAssocPrevious = true;
				// Keep moving backward until we find an editable insertion point.
				ich = BackOneChar(ichNew, pvpboxNew, fAssocPrev, pvg, &pvpbox, fSuppressClumping);
				if (ich == ichNew && pvpbox == pvpboxNew)
				{
					// Must have hit the beginning of this field.
					m_fAssocPrevious = IsEndOfLine(m_ichEnd, m_pvpbox, pvg);
					m_fEndBeforeAnchor = false;
					m_qttp = NULL;
					return false;
				}
				if (pvpboxNew != pvpbox)
				{
					if (++cPara > cParaLim)
					{
						// There's no point in looking forever -- the whole text may be read-only!
						m_fAssocPrevious = IsEndOfLine(m_ichEnd, m_pvpbox, pvg);
						m_fEndBeforeAnchor = false;
						m_qttp = NULL;
						return false;
					}
					pvpboxNew = pvpbox;

					// (TimS) Okay... this is ugly, but we need to set m_fAssocPrevious based on whether or not
					// the new paragraph is empty. Unfortunately, we can't easily determine that because
					// a views-code paragraph can contain any number of strings. Ideally, we would just have
					// to look at the editable status of either side of the IP, but because of the way
					// IsEditable was coded, it essentially ignores the fAssocPrev flag which makes it really
					// difficult to tell if either side of an IP is not editable. To get around that we
					// just check the number of characters in the string containing the IP. This also isn't
					// entirely correct since fAssocPrev, here, may be wrong giving us the wrong string at a
					// string boundary. However, it fixes the test that was breaking because of this...
					ITsStringPtr qtss;
					VwPropertyStorePtr qvps;
					int ichLim, ichMin, itss, length;
					pvpboxNew->Source()->StringFromIch(ich, fAssocPrev, &qtss, &ichMin, &ichLim, &qvps, &itss);
					CheckHr(qtss->get_Length(&length));
					m_fAssocPrevious = (length > 0);
				}
				ichNew = ich;
				fAssocPrev = CalcAssocPrevForBackArrow(ichNew, pvpboxNew, pvg);
			}
		}
		if (ichNew == m_ichEnd && pvpboxNew == m_pvpbox)
		{
			// must have already been at the beginning of this field.
			m_fAssocPrevious = CalcAssocPrevForBackArrow(m_ichEnd, m_pvpbox, pvg);
			m_fEndBeforeAnchor = false;
			m_qttp = NULL;
			return false;
		}
		if (!CheckCommit(ichNew, pvpboxNew))
			return true;
		if (pvpboxNew != m_pvpbox)
			nHowChanged = ksctDiffPara; // different para
		m_ichEnd = ichNew;
		m_ichAnchor = ichNew;
		m_pvpbox = pvpboxNew;
		m_rcBounds.Clear();
	}
	m_ichAnchor2 = -1;
	m_fEndBeforeAnchor = false;
	m_qttp = NULL;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Determine whether an insertion point at the given character position should be associated
	with the previous or following character, in the case where a back-arrow key has been
	pressed.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::CalcAssocPrevForBackArrow(int ich, VwParagraphBox *pvpbox,
	IVwGraphics *pvg)
{
	if (IsBeginningOfLine(ich, pvpbox, pvg))
		// It's possibly also at the end of the previous line, but we want to draw the IP
		// at the beginning of the line whenever possible, not after the space above.
		return false;
	else if (IsEndOfLine(ich, pvpbox, pvg) && IsEditable(ich, pvpbox, true))
		return true;  // probably at the end of the paragraph
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the next valid position in a logically forward direction.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ForwardArrow(IVwGraphics * pvg, bool fSuppressClumping)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return false;

	VwSelChangeType nHowChanged = ksctSamePara;

	bool fHadRangeSelected = false;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
		fHadRangeSelected = true;
	}

	if (m_ichAnchor != m_ichEnd)
	{
		fHadRangeSelected = true;
	}

	LoseSelection(false);
	m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
		IsEditable(m_ichEnd, m_pvpbox, true);

	// only move if we had a range selected
	if(!fHadRangeSelected)
	{
		bool fWasEditable = IsEditable(m_ichEnd, m_pvpbox, m_fAssocPrevious);
		VwParagraphBox * pvpboxNew;
		int ichNew = ForwardOneChar(m_ichEnd, m_pvpbox, m_fAssocPrevious, pvg, &pvpboxNew, fSuppressClumping);
		bool fAssocPrev = (ichNew != 0);
		int ich = -1;
		VwParagraphBox * pvpbox;
		int cParaLim = 0;
		CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
		int cPara = 0;
		// If we weren't in editable text to start with, then we assume that any selection (even a
		// non-editable one) is fine
		if (fWasEditable)
		{
			// Do this loop while both sides of the IP are read-only.
			while (!IsEditable(ichNew, pvpboxNew))
			{
				m_fAssocPrevious = false;
				// Move forward until we find an editable insertion point.
				ich = ForwardOneChar(ichNew, pvpboxNew, fAssocPrev, pvg, &pvpbox, fSuppressClumping);
				if (ich == ichNew && pvpbox == pvpboxNew)
				{
					// Must have hit the end of this field.
					m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
						IsEditable(m_ichEnd, m_pvpbox, true);
					m_fEndBeforeAnchor = false;
					m_qttp = NULL;
					return false;
				}
				if (pvpboxNew != pvpbox)
				{
					if (++cPara > cParaLim)
					{
						// There's no point in looking forever -- the whole text may be read-only!
						m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
							IsEditable(m_ichEnd, m_pvpbox, true);
						m_fEndBeforeAnchor = false;
						m_qttp = NULL;
						return false;
					}
					pvpboxNew = pvpbox;
				}
				ichNew = ich;
				fAssocPrev = (ichNew != 0);
			}
		}
		if (ichNew == m_ichEnd && pvpboxNew == m_pvpbox)
		{
			// must have already been at the end of this field.
			m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
				IsEditable(m_ichEnd, m_pvpbox, true);

			m_fEndBeforeAnchor = false;
			m_qttp = NULL;
			return false;
		}
		if (pvpboxNew != m_pvpbox)
			m_fAssocPrevious = false;
		if (!CheckCommit(ichNew, pvpboxNew))
			return true;
		if (pvpboxNew != m_pvpbox)
			nHowChanged = ksctDiffPara;
		m_ichEnd = ichNew;
		m_ichAnchor = ichNew;
		m_pvpbox = pvpboxNew;
		m_rcBounds.Clear();
	}
	m_ichAnchor2 = -1;
	m_fEndBeforeAnchor = false;
	m_qttp = NULL;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the point one line above its current position.
	Return true if the up motion was handled, or false if we cannot move up any further in this
	field.  If returning false, set *pxdPos to the desired x (horizontal) position.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
	@param pxdPos
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::UpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos)
{
	bool fSelChanged = false;
	AssertPtr(pvg);
	if (!IsEnabled())
	{
		*pxdPos = 0;
		return false;
	}
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
		fSelChanged = true;
	}
	if (m_ichAnchor != m_ichEnd){
		fSelChanged = true;
	}

	LoseSelection(true);

	bool fWasEditable = IsEditable(m_ichEnd, m_pvpbox, m_fAssocPrevious);
	VwParagraphBox * pvpboxNew;
	int ichHome;
	int ichNew = UpOneLine(m_ichEnd, m_pvpbox, m_fAssocPrevious, pvg, rcSrcRoot, rcDstRoot,
		&pvpboxNew, &ichHome);
	bool fAssocPrev = ichNew != ichHome;
	int ich = -1;
	int cParaLim = 0;
	CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
	int cPara = 0;

	// If we weren't in editable text to start with, then we assume that any selection (even a
	// non-editable one) is fine
	if (fWasEditable)
	{
		while (!IsEditable(ichNew, pvpboxNew, fAssocPrev))
		{
			// If moving the IP up a line landed us in a read-only box, then move the IP forward
			// until finding an editable field. If, while moving forward, we hit the end of the
			// line without finding an editable field, then move up to the next line and repeat
			// the process.
			if (!FindEditablePlaceOnLine(&ichNew, &pvpboxNew, &fAssocPrev, pvg))
			{
				VwParagraphBox * pvpbox;

				// Move upward until we find an editable insertion point.
				ich = UpOneLine(ichNew, pvpboxNew, fAssocPrev, pvg, rcSrcRoot, rcDstRoot, &pvpbox,
					&ichHome);
				if (ich == ichNew && pvpbox == pvpboxNew)
				{
					// Must have hit the top line of the root view.
					ichNew = m_ichEnd;
					pvpboxNew = m_pvpbox;
					break;
				}
				if (pvpboxNew != pvpbox)
				{
					if (++cPara > cParaLim)
					{
						// There's no point in looking forever -- the whole text may be read-only!
						ichNew = m_ichEnd;
						pvpboxNew = m_pvpbox;
						break;
					}
					pvpboxNew = pvpbox;
				}
				ichNew = ich;
				fAssocPrev = ichNew != ichHome;
			}
		}
	}
	if (ichNew == m_ichEnd && pvpboxNew == m_pvpbox)
	{
		*pxdPos = m_xdIP;			// Must already be on the top line of the root view.
		if (fSelChanged)
			m_pvpbox->Root()->NotifySelChange(nHowChanged);
		return false;
	}
	if (!CheckCommit(ichNew, pvpboxNew))
	{
		// If commit won't allow the selection to move away, we mustn't.
		// The best way to help ensure that is to return a value that indicates
		// we have already handled the movement request. We've already done all that
		// legitimately can be at this point.
		return true;
	}
	if (pvpboxNew != m_pvpbox)
		nHowChanged = ksctDiffPara;
	m_fAssocPrevious = fAssocPrev;
	m_fEndBeforeAnchor = false;
	m_ichEnd = ichNew;
	m_ichAnchor = ichNew;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_rcBounds.Clear();
	m_qttp = NULL;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the point one line below its current position.
	Return true if the down motion was handled, or false if we cannot move down any further in
	this field.  If returning false, set *pxdPos to the desired x (horizontal) position.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
	@param pxdPos
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::DownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos)
{
	bool fSelChanged = false;
	AssertPtr(pvg);
	if (!IsEnabled())
	{
		*pxdPos = 0;
		return false;
	}
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		fSelChanged = true;
		nHowChanged = ksctDiffPara;
	}
	if (m_ichAnchor != m_ichEnd)
	{
		fSelChanged = true;
	}
	LoseSelection(false);

	bool fWasEditable = IsEditable(m_ichEnd, m_pvpbox, m_fAssocPrevious);
	VwParagraphBox * pvpboxNew;
	int ichHome;
	int ichNew = DownOneLine(m_ichEnd, m_pvpbox, m_fAssocPrevious, pvg, rcSrcRoot, rcDstRoot,
		&pvpboxNew, &ichHome);
	bool fAssocPrev = (ichNew != ichHome);
	int ich = -1;
	int cParaLim = 0;
	CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
	int cPara = 0;

	// If we weren't in editable text to start with, then we assume that any selection (even a
	// non-editable one) is fine
	if (fWasEditable)
	{
		while (!IsEditable(ichNew, pvpboxNew, fAssocPrev))
		{
			// If moving the IP down a line landed us in a read-only box, then move the IP forward
			// until finding an editable field.  If, while moving forward, we hit the end of the
			// line without finding an editable field, then move up to the next line and repeat
			// the process.
			if (!FindEditablePlaceOnLine(&ichNew, &pvpboxNew, &fAssocPrev, pvg))
			{
				VwParagraphBox * pvpbox;

				// Move downward until we find an editable insertion point.
				ich = DownOneLine(ichNew, pvpboxNew, fAssocPrev, pvg, rcSrcRoot, rcDstRoot,
					&pvpbox, &ichHome);
				if (ich == ichNew && pvpbox == pvpboxNew)
				{
					// Must have hit the bottom line of the root view.
					ichNew = m_ichEnd;
					pvpboxNew = m_pvpbox;
					break;
				}
				if (pvpboxNew != pvpbox)
				{
					if (++cPara > cParaLim)
					{
						// There's no point in looking forever -- the whole text may be read-only!
						ichNew = m_ichEnd;
						pvpboxNew = m_pvpbox;
						break;
					}
					pvpboxNew = pvpbox;
				}
				ichNew = ich;
				fAssocPrev = (ichNew != ichHome);
			}
		}
	}
	if (ichNew == m_ichEnd && pvpboxNew == m_pvpbox)
	{
		*pxdPos = m_xdIP;			// Must already be on the bottom line of the root view.
		if (fSelChanged)
			m_pvpbox->Root()->NotifySelChange(nHowChanged);
		return false;
	}
	if (!CheckCommit(ichNew, pvpboxNew))
	{
		// REVIEW SteveMc: What do we want to do here?
		return true;
	}
	if (pvpboxNew != m_pvpbox)
		nHowChanged = ksctDiffPara;
	m_fAssocPrevious = fAssocPrev;
	m_fEndBeforeAnchor = false;
	m_ichEnd = ichNew;
	m_ichAnchor = ichNew;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_qttp = NULL;
	m_rcBounds.Clear();
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the end of the line.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::EndKey(IVwGraphics * pvg, bool fLogical)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;

	//	Change a range selection to an IP at the end of the range
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
		m_fAssocPrevious = true;
	}
	if (m_ichAnchor != m_ichEnd)
	{
		m_fAssocPrevious = true;
	}

	LoseSelection(false);

	if (m_ichAnchor == 0)
		m_fAssocPrevious = false;

	if (fLogical)
	{
		int ichEnd = EndOfLine(m_ichEnd, m_pvpbox, m_fAssocPrevious, pvg);
		ComBool fAssocPrev = (ichEnd == 0) ? false : true;
		VwParagraphBox * pvpbox;
		int ichEndOrig = ichEnd;
		ComBool fAssocPrevOrig = fAssocPrev;
		while (!IsEditable(ichEnd, m_pvpbox))
		{
			// Move backward until we find an editable insertion point.
			int ich = BackOneChar(ichEnd, m_pvpbox, (bool)fAssocPrev, pvg, &pvpbox);
			if (pvpbox != m_pvpbox)
			{
				ichEnd = ichEndOrig;
				fAssocPrev = !fAssocPrevOrig;
				break;
			}
			if (ich == ichEnd)
			{
				ichEnd = ichEndOrig;
				fAssocPrev = !fAssocPrevOrig;
				break;		// What should we do here?
			}
			ichEnd = ich;
			fAssocPrev = ichEnd ? true : false;
		}
		if (!CheckCommit(ichEnd, m_pvpbox))
			return;
		m_ichEnd = ichEnd;
		m_ichAnchor = m_ichEnd;
		m_ichAnchor2 = -1;
		if (IsEditable(m_ichEnd, m_pvpbox, fAssocPrev))
			m_fAssocPrevious = fAssocPrev;
		else
			m_fAssocPrevious = !fAssocPrev;
		m_fEndBeforeAnchor = false;
		m_qttp = NULL;
	}
	else
	{
		PhysicalHomeOrEnd(pvg, true);
	}

	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the beginning of the line.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::HomeKey(IVwGraphics * pvg, bool fLogical)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;

	//	Change a range selection to an IP at beginning of the range.
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
	}
	LoseSelection(true);

	if (fLogical)
	{
		int ichHome = BeginningOfLine(m_ichEnd, m_pvpbox, m_fAssocPrevious, pvg);
		if (ichHome == -1)
		{
			m_ichAnchor2 = -1;
			return;
		}
		ComBool fAssocPrev = false;
		int ich = -1;
		VwParagraphBox * pvpbox = m_pvpbox;
		int ichHomeOrig = ichHome;
		bool fReadOnlyPara = false;
		while (!IsEditable(ichHome, m_pvpbox))
		{
			// Move forward until we find an editable insertion point.
			ich = ForwardOneChar(ichHome, m_pvpbox, fAssocPrev, pvg, &pvpbox);
			if (pvpbox != m_pvpbox)
			{
				ichHome = ichHomeOrig;
				fReadOnlyPara = true;
				break;
			}
			if (ich == ichHome)
			{
				ichHome = ichHomeOrig;
				fReadOnlyPara = true;
				break;			// What should we do here?
			}
			ichHome = ich;
			fAssocPrev = !IsBeginningOfLine(ichHome, pvpbox, pvg);
		}
		if (!CheckCommit(ichHome, m_pvpbox))
			return;
		m_ichEnd = ichHome;
		m_ichAnchor = m_ichEnd;
		m_ichAnchor2 = -1;
		if (fReadOnlyPara)
			m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg);
		else
			m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
				IsEditable(m_ichEnd, m_pvpbox, true);
		m_fEndBeforeAnchor = false;
		m_qttp = NULL;
	}
	else
	{
		PhysicalHomeOrEnd(pvg, false);
	}
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}


/*----------------------------------------------------------------------------------------------
	If we are not a range, just leave the insertion point where it is.
	If we are a range, lose the selection, leaving us with an Insertion Point at one end
	position.
	Whether we move the anchor or end point is controlled by fCollapseToStart. If it is true,
	we collapse to whichever end comes first; if false, to whichever comes last.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::LoseSelection(bool fCollapseToStart)
{
	bool fMoveAnchor = (m_fEndBeforeAnchor && fCollapseToStart) ||
		(!m_fEndBeforeAnchor && !fCollapseToStart);
	int ichAnchor = m_ichAnchor;
	int ichEnd = m_ichEnd;
	if (fMoveAnchor)
	{
		m_ichAnchor = m_ichEnd;
		if(m_pvpboxEnd)
		{
			m_pvpbox = m_pvpboxEnd;
			m_rcBounds.Clear();
		}
	}
	else
	{
		m_ichEnd = m_ichAnchor;
	}

	// We need to update the assocPrev property (TE-5564)
	if (ichAnchor != m_ichAnchor || ichEnd != m_ichEnd)
		m_fAssocPrevious = (m_ichAnchor != 0 || m_ichEnd != 0);
	m_pvpboxEnd = NULL;
	m_rcBounds.Clear();
	m_xdIP = -1;
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the physical beginning or end of the current selection's line.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fEnd
----------------------------------------------------------------------------------------------*/
void VwTextSelection::PhysicalHomeOrEnd(IVwGraphics * pvg, bool fEnd)
{
	Assert(IsInsertionPoint());	// adjustment has already been done
	m_xdIP = -1;

	//	Find the line the current box is on.
	VwStringBox * psbox = GetStringBox(m_ichAnchor, m_pvpbox, m_fAssocPrevious);

	int iboxHomeOrEnd = -1;
	int nDir = 0;
	VwBox * pboxFirstOnLine = m_pvpbox->FirstBox();
	VwBox * pboxNextLine;
	BoxVec vboxOneLine;
	int ibox;
	for (; pboxFirstOnLine; pboxFirstOnLine = pboxNextLine)
	{
		vboxOneLine.Clear();
		pboxNextLine = m_pvpbox->GetALine(pboxFirstOnLine, vboxOneLine);
		for (ibox = 0; ibox < vboxOneLine.Size(); ibox++)
		{
			if (vboxOneLine[ibox] == psbox)
				break;
		}
		if (ibox >= vboxOneLine.Size())
			continue;
		//	Boxes in vboxOneLine are ordered physically from left to right.
		if (m_pvpbox->RightToLeft() == fEnd)
		{
			iboxHomeOrEnd = 0;
			nDir = 1;
		}
		else
		{
			iboxHomeOrEnd = vboxOneLine.Size() - 1;
			nDir = -1;
		}
		break;
	}
	if (iboxHomeOrEnd == -1)
		return;

	while (!dynamic_cast<VwStringBox *>(vboxOneLine[iboxHomeOrEnd]))
	{
		iboxHomeOrEnd += nDir;
		if (iboxHomeOrEnd < 0 || iboxHomeOrEnd > vboxOneLine.Size())
			return;
	}
	psbox = dynamic_cast<VwStringBox *>(vboxOneLine[iboxHomeOrEnd]);
	ILgSegmentPtr qseg = psbox->Segment();
	int dichLim;
	CheckHr(qseg->get_Lim(psbox->IchMin(), &dichLim));
	ComBool fSegRtl;
	CheckHr(qseg->get_RightToLeft(psbox->IchMin(), &fSegRtl));
	ComBool fRight = (m_pvpbox->RightToLeft() != fEnd);
	ComBool fResult;
	int ichNewSel = (fSegRtl == fRight) ?
		psbox->IchMin() :
		psbox->IchMin() + dichLim;
	ComBool fAssocPrev = (ichNewSel != psbox->IchMin());
	int ichTmp = ichNewSel;
	ComBool fTmp = fAssocPrev;
	CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichTmp, &fTmp,
			fRight, false, &fResult));
	while (fResult)
	{
		ichNewSel = ichTmp;
		fAssocPrev = fTmp;
		CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichTmp, &fTmp,
			fRight, false, &fResult));
	}
	fAssocPrev = (ichNewSel == psbox->IchMin()) ? (ComBool)false : fAssocPrev;

	int ichNewLog = m_pvpbox->Source()->RenToLog(ichNewSel);
	if (!IsEditable(ichNewLog, m_pvpbox, (bool)fAssocPrev))
	{
		(fRight) ? RightArrow(pvg, true) : LeftArrow(pvg, true);
	}

	if (!CheckCommit(ichNewLog, m_pvpbox))
		return;
	m_ichEnd = ichNewLog;
	m_ichAnchor = m_ichEnd;
	m_ichAnchor2 = -1;
	m_fAssocPrevious = (bool)fAssocPrev;
	m_fEndBeforeAnchor = false;
	m_qttp = NULL;
}

/*----------------------------------------------------------------------------------------------
	Extend the selection to the left.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical - true if we want to move the selection logically, false if moving
					physically (visually)
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftLeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping)
{
	if (fLogical)
	{
		if (m_pvpbox->RightToLeft())
			ShiftForwardArrow(pvg, fSuppressClumping);
		else
			ShiftBackwardArrow(pvg, fSuppressClumping);
	}
	else
	{
		ShiftPhysicalArrow(pvg, false, fSuppressClumping);
	}
}

/*----------------------------------------------------------------------------------------------
	Extend the selection to the right.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical - true if we want to move the selection logically, false if moving
					physically (visually)
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftRightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping)
{
	if (fLogical)
	{
		if (m_pvpbox->RightToLeft())
			ShiftBackwardArrow(pvg, fSuppressClumping);
		else
			ShiftForwardArrow(pvg, fSuppressClumping);
	}
	else
	{
		ShiftPhysicalArrow(pvg, true, fSuppressClumping);
	}
}

/*----------------------------------------------------------------------------------------------
	Extend the selection based on the physical arrangment of the characters.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fRight - true if moving to the right, false if moving to the left
	@param fSuppressClumping - true to move by single characters, not clumps.
		Not fully implemented for physical movement.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftPhysicalArrow(IVwGraphics * pvg, bool fRight, bool fSuppressClumping)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;

	m_xdIP = -1;
	VwStringBox * psboxAnchor = GetStringBox(m_ichAnchor, m_pvpbox, m_fEndBeforeAnchor);
	ILgSegmentPtr qsegAnchor = psboxAnchor->Segment();
	ComBool fParaRtl;
	fParaRtl = m_pvpbox->RightToLeft();

	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwStringBox * psboxOldEnd = GetStringBox(m_ichEnd, pvpboxEnd, !m_fEndBeforeAnchor);
	ILgSegmentPtr qsegOldEnd = psboxOldEnd->Segment();
	ComBool fOldEndRtl;
	CheckHr(qsegOldEnd->get_RightToLeft(psboxOldEnd->IchMin(), &fOldEndRtl));
	int cchEndPara;
	CheckHr(pvpboxEnd->Source()->get_Length(&cchEndPara));

	VwStringBox * psboxNewEnd;
	ILgSegmentPtr qsegNewEnd;
	ComBool fAssocPrevNeeded;
	ComBool fNewEndRtl;
	int ichNewEnd = m_ichEnd;

	ComBool fResult;
	ComBool fAssocPrev = (IsInsertionPoint()) ? m_fAssocPrevious : !m_fEndBeforeAnchor;

	//	Adjust end-point within the same segment, if possible.
	bool fRightTmp = fRight;
	fAssocPrevNeeded = (IsInsertionPoint()) ? (ComBool)(fRight != fOldEndRtl) : fAssocPrev;
	CheckHr(qsegOldEnd->ExtendSelectionPosition(psboxOldEnd->IchMin(), pvg,
		&ichNewEnd, ichNewEnd != 0, fAssocPrevNeeded, m_ichAnchor,
		fRightTmp, false, &fResult));
	if (fResult && pvpboxEnd != m_pvpbox &&
		((ichNewEnd == 0 && !m_fEndBeforeAnchor) ||
			(ichNewEnd == cchEndPara && m_fEndBeforeAnchor)))
	{
		//	Nothing in this paragraph selected anymore.
		fResult = false;
	}
	if (fResult)
	{
		m_ichEnd = ichNewEnd;
		if (!m_pvpboxEnd)
			m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
		// else anchor and end are far away, so m_fEndBeforeAnchor can't have changed
		m_pvpbox->Root()->NotifySelChange(ksctSamePara);
		return;
	}

	//	Adjust end-point to an adjacent box in the same paragraph.
	BoxVec vboxInEndPara;
	pvpboxEnd->GetBoxesInPhysicalOrder(vboxInEndPara);
	psboxNewEnd = psboxOldEnd;
	qsegNewEnd = psboxNewEnd->Segment();
	VwBox * pboxTmp = psboxNewEnd;
	int nDir = (fParaRtl == fRight) ? -1 : 1;
	for (;;)
	{
		pboxTmp = m_pvpbox->GetAdjPhysicalBox(pboxTmp, vboxInEndPara, nDir);
		if (!pboxTmp)
			break;
		VwStringBox * psboxTmp = dynamic_cast<VwStringBox *>(pboxTmp);
		if (!psboxTmp)
			continue;
		VwStringBox * psboxNewEndTmp = psboxTmp;
		ILgSegmentPtr qsegNewEndTmp = psboxNewEndTmp->Segment();
		CheckHr(qsegNewEndTmp->get_RightToLeft(psboxNewEndTmp->IchMin(), &fNewEndRtl));
		if (psboxNewEndTmp == psboxAnchor)
			fAssocPrevNeeded = (m_fEndBeforeAnchor) ?
				(fParaRtl != fNewEndRtl) :
				(fParaRtl == fNewEndRtl);
		else if (!m_pvpboxEnd && psboxOldEnd == psboxAnchor)
			fAssocPrevNeeded = (fParaRtl != fRight);
		else
			fAssocPrevNeeded = !m_fEndBeforeAnchor;
		CheckHr(qsegNewEndTmp->ExtendSelectionPosition(psboxNewEndTmp->IchMin(), pvg,
			&ichNewEnd, true, fAssocPrevNeeded, -1,
			fRight, true, &fResult));
		if (!fResult)
			CheckHr(qsegNewEndTmp->ExtendSelectionPosition(psboxNewEndTmp->IchMin(), pvg,
				&ichNewEnd, true, !fAssocPrevNeeded, -1,
				fRight, true, &fResult));
		if (fResult)
		{
			m_ichEnd = ichNewEnd;
			if (!m_pvpboxEnd)
				m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
			// else anchor and end are far away, so m_fEndBeforeAnchor can't have changed
			m_pvpbox->Root()->NotifySelChange(ksctSamePara);
			return;
		}
	}

	//	Find a position in an adjacent paragraph. First get a selection in the box, then
	//	use the segment routines to get one at the appropriate edge.
	VwParagraphBox * pvpboxNewEnd = pvpboxEnd;
	int dichSegLim;
	CheckHr(qsegNewEnd->get_Lim(psboxNewEnd->IchMin(), &dichSegLim));
	ichNewEnd = (m_pvpbox->RightToLeft() == fRight) ?
		psboxNewEnd->IchMin() :				// backward
		psboxNewEnd->IchMin() + dichSegLim;	// forward
	while (pvpboxNewEnd == pvpboxEnd)
	{
		int ichTmp = (pvpboxEnd->RightToLeft() == fRight) ?
			BackOneChar(ichNewEnd, pvpboxEnd, fAssocPrev, pvg, &pvpboxNewEnd) :
			ForwardOneChar(ichNewEnd, pvpboxEnd, fAssocPrev, pvg, &pvpboxNewEnd);
		if (pvpboxNewEnd == pvpboxEnd && ichTmp == ichNewEnd)
			return; // no adjustment possible in desired direction
		ichNewEnd = ichTmp;
	}
	//if (pvpboxNewEnd->Container() != m_pvpbox->Container())
	//{
	//	// REVIEW JohnT(SharonC): should we beep here?
	//	return;
	//}
	psboxNewEnd = GetStringBox(ichNewEnd, pvpboxNewEnd, !m_fEndBeforeAnchor);
	qsegNewEnd = psboxNewEnd->Segment();
	CheckHr(qsegNewEnd->get_RightToLeft(psboxNewEnd->IchMin(), &fNewEndRtl));
	if (psboxNewEnd == psboxAnchor)
		fAssocPrevNeeded = (m_fEndBeforeAnchor) ?
			(fParaRtl != fNewEndRtl) :
			(fParaRtl == fNewEndRtl);
	else if (!m_pvpboxEnd && psboxOldEnd == psboxAnchor)
		fAssocPrevNeeded = (fParaRtl != fRight);
	else
		fAssocPrevNeeded = !m_fEndBeforeAnchor;

	int cch;
	CheckHr(pvpboxNewEnd->Source()->get_Length(&cch));
	if (cch)
	{
		CheckHr(qsegNewEnd->ExtendSelectionPosition(psboxNewEnd->IchMin(), pvg,
			&ichNewEnd, true, fAssocPrevNeeded, -1,
			fRight, true, &fResult));
		if (!fResult)
			return;	// give up
	}

	m_ichEnd = ichNewEnd;
	if (!m_pvpboxEnd && pvpboxNewEnd)
		// Just moved into a new paragraph; might have switched directions.
		m_fEndBeforeAnchor = (pvpboxEnd->RightToLeft() == fRight);
	m_pvpboxEnd = (pvpboxNewEnd == m_pvpbox) ? NULL : pvpboxNewEnd;
	m_rcBounds.Clear();
	if (!m_pvpboxEnd)
		m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
	// else it can stay the same
	m_pvpbox->Root()->NotifySelChange(ksctDiffPara);
}

/*----------------------------------------------------------------------------------------------
	Extend the selection left by one character.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftBackwardArrow(IVwGraphics * pvg, bool fSuppressClumping)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;

	m_xdIP = -1;
	VwSelChangeType nHowChanged = ksctSamePara;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwParagraphBox * pvpboxNew;
	int ichEnd = BackOneChar(m_ichEnd, pvpboxEnd, m_fAssocPrevious, pvg, &pvpboxNew, fSuppressClumping);
	//if (pvpboxNew->Container() != m_pvpbox->Container())
	//{
	//	// REVIEW JohnT(SteveMc): should we beep here? allow this more complicated selection?
	//	return;
	//}
	if (!SetEnd(pvpboxNew, pvpboxEnd, ichEnd, true, nHowChanged))
		return;

	m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg);
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Extend the selection right by one character.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftForwardArrow(IVwGraphics * pvg, bool fSuppressClumping)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;

	m_xdIP = -1;
	VwSelChangeType nHowChanged = ksctSamePara;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwParagraphBox * pvpboxNew;
	int ichEnd = ForwardOneChar(m_ichEnd, pvpboxEnd, m_fAssocPrevious, pvg, &pvpboxNew, fSuppressClumping);
	//if (pvpboxNew->Container() != m_pvpbox->Container())
	//{
	//	// REVIEW JohnT(SteveMc): should we beep here? allow this more complicated selection?
	//	return;
	//}
	if (!SetEnd(pvpboxNew, pvpboxEnd, ichEnd, false, nHowChanged))
		return;

	m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg);
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Extend the selection to the point one line above the cursor, moving the cursor as well.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;
	VwSelChangeType nHowChanged = ksctSamePara;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwParagraphBox * pvpboxNew;
	int ichHome;
	int ichEnd = UpOneLine(m_ichEnd, pvpboxEnd, m_fAssocPrevious, pvg, rcSrcRoot, rcDstRoot,
		&pvpboxNew, &ichHome);
	//if (pvpboxNew->Container() != m_pvpbox->Container())
	//{
	//	// REVIEW JohnT(SteveMc): should we beep here? allow this more complicated selection?
	//	return;
	//}
	if (!SetEnd(pvpboxNew, pvpboxEnd, ichEnd, true, nHowChanged))
		return;

	m_fAssocPrevious = m_ichEnd != ichHome;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Extend the selection to the point one line below the cursor, moving the cursor as well.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;
	VwSelChangeType nHowChanged = ksctSamePara;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwParagraphBox * pvpboxNew;
	int ichHome;
	int ichEnd = DownOneLine(m_ichEnd, pvpboxEnd, m_fAssocPrevious, pvg, rcSrcRoot, rcDstRoot,
		&pvpboxNew, &ichHome);
	//if (pvpboxNew->Container() != m_pvpbox->Container())
	//{
	//	// REVIEW JohnT(SteveMc): should we beep here? allow this more complicated selection?
	//	return;
	//}
	if (!SetEnd(pvpboxNew, pvpboxEnd, ichEnd, false, nHowChanged))
		return;
	m_fAssocPrevious = m_ichEnd != ichHome;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

// Sets the end of the selection and m_fEndBeforeAnchor flag.
bool VwTextSelection::SetEnd(VwParagraphBox * pvpboxNew, VwParagraphBox * pvpboxEnd, int ichEnd,
	bool fEndBeforeAnchor, VwSelChangeType & nHowChanged)
{
	if (!CheckCommit(ichEnd, pvpboxNew))
		return false;
	if (pvpboxNew != pvpboxEnd)
	{
		// Throw away a selection if one end is inside a picture (caption) and the other
		// end is outside.
		if (pvpboxEnd->IsInMoveablePile() || pvpboxNew->IsInMoveablePile())
			return false;

		nHowChanged = ksctDiffPara;
		m_ichEnd = ichEnd;
		if (m_pvpboxEnd)
		{
			if (pvpboxNew == m_pvpbox)
			{
				m_pvpboxEnd = NULL;
				m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
			}
			else
			{
				m_pvpboxEnd = pvpboxNew;
			}
		}
		else
		{
			m_pvpboxEnd = pvpboxNew;
			m_fEndBeforeAnchor = fEndBeforeAnchor;
		}
		m_rcBounds.Clear();
	}
	else if (ichEnd != m_ichEnd)
	{
		m_ichEnd = ichEnd;
		if (!m_pvpboxEnd)
			m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Extend selection to the end of the line.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftEndKey(IVwGraphics * pvg, bool fLogical)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;
	m_xdIP = -1;
	if (fLogical)
	{
		VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
		int ichEnd = EndOfLine(m_ichEnd, pvpboxEnd, m_fAssocPrevious, pvg);
		if (!CheckCommit(ichEnd, pvpboxEnd))
			return;
		m_ichEnd = ichEnd;
		if (!m_pvpboxEnd)
			m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
		m_fAssocPrevious = m_ichEnd ? true : false;
	}
	else
	{
		ShiftPhysicalHomeOrEnd(pvg, true);
	}
	m_pvpbox->Root()->NotifySelChange(ksctDiffPara);
}

/*----------------------------------------------------------------------------------------------
	Extend selection to the beginning of the line.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftHomeKey(IVwGraphics * pvg, bool fLogical)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;
	m_xdIP = -1;
	if (fLogical)
	{
		VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
		int ichEnd = BeginningOfLine(m_ichEnd, pvpboxEnd, m_fAssocPrevious, pvg);
		if (ichEnd != -1)
		{
			if (!CheckCommit(ichEnd, pvpboxEnd))
				return;
			m_ichEnd = ichEnd;
			if (!m_pvpboxEnd)
				m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
			m_fAssocPrevious = false;
		}
	}
	else
	{
		ShiftPhysicalHomeOrEnd(pvg, false);
	}
	m_pvpbox->Root()->NotifySelChange(ksctDiffPara);
}

/*----------------------------------------------------------------------------------------------
	Move the insertion point to the physical beginning or end of the current selection's line.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fEnd
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ShiftPhysicalHomeOrEnd(IVwGraphics * pvg, bool fEnd)
{
	m_xdIP = -1;

	//	Find the line the current end-point box is on.
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwStringBox * psbox = GetStringBox(m_ichEnd, pvpboxEnd, m_fAssocPrevious);
	int iboxHomeOrEnd = -1;
	int nDir = 0;
	VwBox * pboxFirstOnLine = pvpboxEnd->FirstBox();
	VwBox * pboxNextLine;
	BoxVec vboxOneLine;
	int ibox;
	for (; pboxFirstOnLine; pboxFirstOnLine = pboxNextLine)
	{
		vboxOneLine.Clear();
		pboxNextLine = pvpboxEnd->GetALine(pboxFirstOnLine, vboxOneLine);
		for (ibox = 0; ibox < vboxOneLine.Size(); ibox++)
		{
			if (vboxOneLine[ibox] == psbox)
				break;
		}
		if (ibox >= vboxOneLine.Size())
			continue;
		//	Boxes in vboxOneLine are ordered physically from left to right.
		if (m_pvpbox->RightToLeft() == fEnd)
		{
			iboxHomeOrEnd = 0;
			nDir = 1;
		}
		else
		{
			iboxHomeOrEnd = vboxOneLine.Size() - 1;
			nDir = -1;
		}
		break;
	}
	if (iboxHomeOrEnd == -1)
		return;

	while (!dynamic_cast<VwStringBox *>(vboxOneLine[iboxHomeOrEnd]))
	{
		iboxHomeOrEnd += nDir;
		if (iboxHomeOrEnd < 0 || iboxHomeOrEnd > vboxOneLine.Size())
			return;
	}
	psbox = dynamic_cast<VwStringBox *>(vboxOneLine[iboxHomeOrEnd]);
	ILgSegmentPtr qseg = psbox->Segment();
	int dichLim;
	CheckHr(qseg->get_Lim(psbox->IchMin(), &dichLim));
	ComBool fSegRtl;
	CheckHr(qseg->get_RightToLeft(psbox->IchMin(), &fSegRtl));
	ComBool fRight = (m_pvpbox->RightToLeft() != fEnd);
	ComBool fResult;
	int ichNewSel = (fSegRtl == fRight) ?
		psbox->IchMin() :
		psbox->IchMin() + dichLim;
	ComBool fAssocPrev = (ichNewSel != psbox->IchMin());
	int ichTmp = ichNewSel;
	ComBool fTmp = fAssocPrev;
	CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichTmp, &fTmp,
			fRight, false, &fResult));
	while (fResult)
	{
		ichNewSel = ichTmp;
		fAssocPrev = fTmp;
		CheckHr(qseg->ArrowKeyPosition(psbox->IchMin(), pvg, &ichTmp, &fTmp,
			fRight, false, &fResult));
	}
	fAssocPrev = (ichNewSel == psbox->IchMin()) ? (ComBool)false : fAssocPrev;

	int ichNewLog = m_pvpbox->Source()->RenToLog(ichNewSel);
	if (!CheckCommit(ichNewLog, m_pvpbox))
		return;
	m_ichEnd = ichNewLog;
	if (!m_pvpboxEnd)
		m_fEndBeforeAnchor = (m_ichEnd < m_ichAnchor);
}

/*----------------------------------------------------------------------------------------------
	Move one word to the left.
	ENHANCE SharonC: Implement visual behavior if we decide we need it.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ControlLeftArrow(IVwGraphics * pvg, bool fLogical)
{
//	if (fLogical)
//	{
		if (m_pvpbox->RightToLeft())
			return ControlForwardArrow(pvg);
		else
			return ControlBackwardArrow(pvg);
//	}
//	else
//		return ControlPhysicalArrow(pvg, false);
}

/*----------------------------------------------------------------------------------------------
	Move one word to the right.
	TODO SharonC: Implement visual behavior if we decide we need it.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ControlRightArrow(IVwGraphics * pvg, bool fLogical)
{
//	if (fLogical)
//	{
		if (m_pvpbox->RightToLeft())
			return ControlBackwardArrow(pvg);
		else
			return ControlForwardArrow(pvg);
//	}
//	else
//		return ControlPhysicalArrow(pvg, true);
}


/*----------------------------------------------------------------------------------------------
	Move one word to the left or right in a physical direction.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fRight
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ControlPhysicalArrow(IVwGraphics * pvg, bool fRight)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Move one word backward.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ControlBackwardArrow(IVwGraphics * pvg)
{
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
	}
	LoseSelection(true);
	VwParagraphBox * pvpboxNew;
	int ichEnd = BackOneWord(m_ichEnd, m_pvpbox, pvg, &pvpboxNew);
	bool fAssocPrev = !IsBeginningOfLine(ichEnd, pvpboxNew, pvg);
	int ich = -1;
	VwParagraphBox * pvpbox;
	int ichEndOrig = ichEnd;
	VwParagraphBox * pvpboxNewOrig = pvpboxNew;
	int cParaLim = 0;
	CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
	int cPara = 0;
	while (!IsEditable(ichEnd, pvpboxNew) && cParaLim > 0)
	{
		// Keep moving backward until we find an editable insertion point.
		ich = BackOneWord(ichEnd, pvpboxNew, pvg, &pvpbox);
		if (ich == ichEnd && pvpbox == pvpboxNew)
		{
			// Must have hit the beginning of this field.
			m_fAssocPrevious = IsEndOfLine(m_ichEnd, m_pvpbox, pvg);
			m_fEndBeforeAnchor = false;
			m_qttp = NULL;
			return false;
		}
		if (pvpboxNew != pvpbox)
		{
			if (++cPara > cParaLim)
			{
				// There's no point in looking forever -- the whole text may be read-only!
				ichEnd = ichEndOrig;
				pvpboxNew = pvpboxNewOrig;
				break;
			}
			pvpboxNew = pvpbox;
		}
		ichEnd = ich;
		fAssocPrev = !IsBeginningOfLine(ichEnd, pvpboxNew, pvg);
	}
	if (ichEnd == m_ichEnd && pvpboxNew == m_pvpbox)
	{
		// must have already been at the beginning of this field.
		m_fAssocPrevious = IsEndOfLine(m_ichEnd, m_pvpbox, pvg);
		m_fEndBeforeAnchor = false;
		m_qttp = NULL;
		return false;
	}
	if (!CheckCommit(ichEnd, pvpboxNew))
	{
		// If commit won't allow the selection to move away, we mustn't.
		// The best way to help ensure that is to return a value that indicates
		// we have already handled the movement request. We've already done all that
		// legitimately can be at this point.
		return true;
	}
	if (pvpboxNew != m_pvpbox)
		nHowChanged = ksctDiffPara;
	m_ichEnd = ichEnd;
	m_ichAnchor = ichEnd;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_rcBounds.Clear();
	m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
		IsEditable(m_ichEnd, m_pvpbox, true);
	m_fEndBeforeAnchor = false;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Move one word right.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ControlForwardArrow(IVwGraphics * pvg)
{
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
	}
	LoseSelection(false);
	VwParagraphBox * pvpboxNew;
	int ichEnd = ForwardOneWord(m_ichEnd, m_pvpbox, pvg, &pvpboxNew);
	bool fAssocPrev = !IsBeginningOfLine(ichEnd, pvpboxNew, pvg);
	int ich = -1;
	VwParagraphBox * pvpbox;
	int ichEndOrig = ichEnd;
	VwParagraphBox * pvpboxNewOrig = pvpboxNew;
	int cParaLim = 0;
	CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
	int cPara = 0;
	while (!IsEditable(ichEnd, pvpboxNew) && cParaLim > 0)
	{
		// Move forward until we find an editable insertion point.
		ich = ForwardOneWord(ichEnd, pvpboxNew, pvg, &pvpbox);
		if (ich == ichEnd && pvpbox == pvpboxNew)
		{
			// Must have hit the end of this field.
			m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
				IsEditable(m_ichEnd, m_pvpbox, true);
			m_fEndBeforeAnchor = false;
			m_qttp = NULL;
			return false;
		}
		if (pvpboxNew != pvpbox)
		{
			if (++cPara > cParaLim)
			{
				// There's no point in looking forever -- the whole text may be read-only!
				ichEnd = ichEndOrig;
				pvpboxNew = pvpboxNewOrig;
				break;
			}
			pvpboxNew = pvpbox;
		}
		ichEnd = ich;
		fAssocPrev = !IsBeginningOfLine(ichEnd, pvpboxNew, pvg);
	}
	if (ichEnd == m_ichEnd && pvpboxNew == m_pvpbox)
	{
		// must have already been at the end of this field.
		m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
			IsEditable(m_ichEnd, m_pvpbox, true);
		m_fEndBeforeAnchor = false;
		m_qttp = NULL;
		return false;
	}
	if (!CheckCommit(ichEnd, pvpboxNew))
	{
		// If commit won't allow the selection to move away, we mustn't.
		// The best way to help ensure that is to return a value that indicates
		// we have already handled the movement request. We've already done all that
		// legitimately can be at this point.
		return true;
	}
	if (pvpboxNew != m_pvpbox)
		nHowChanged = ksctDiffPara;
	m_ichEnd = ichEnd;
	m_ichAnchor = ichEnd;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_rcBounds.Clear();
	m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
		IsEditable(m_ichEnd, m_pvpbox, true);
	m_fEndBeforeAnchor = false;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	If at the beginning of the paragraph, move to the beginning of the previous paragraph.
	Otherwise, move to the beginning of this paragraph.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.

	@return True if the up motion was handled, or false if we cannot move up any further in this
					field.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ControlUpArrow(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return false;
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
	}
	LoseSelection(true);
	// If we're at the beginning of the paragraph, move to the beginning of the previous
	// paragraph.
	int ich = -1;
	// Find the first editable insertion point in this paragraph.
	int ichFirst = 0;
	ComBool fAssocPrev = false;
	VwParagraphBox * pvpboxNew = m_pvpbox;
	VwParagraphBox * pvpbox;
	while (!IsEditable(ichFirst, m_pvpbox, fAssocPrev))
	{
		ich = ForwardOneChar(ichFirst, m_pvpbox, fAssocPrev, pvg, &pvpboxNew);
		if (pvpboxNew != m_pvpbox)
		{
			pvpboxNew = m_pvpbox;
			break;
		}
		if (ich == ichFirst)
			break;
		ichFirst = ich;
		fAssocPrev = !IsBeginningOfLine(ichFirst, m_pvpbox, pvg);
	}
	int cParaLim = 0;
	CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
	int cPara = 0;
	VwParagraphBox * pvpboxNewOrig = NULL;
	if (ichFirst == m_ichEnd)
	{
		// We're already at the first editable point in the current paragraph.
		// Move to the beginning of the previous paragraph.
		pvpbox = m_pvpbox;
LBackup:
		VwBox * pbox;
		pvpboxNew = NULL;
		for (pbox = pvpbox->NextInReverseRootSeq(); pbox; pbox = pbox->NextInReverseRootSeq())
		{
			if (pbox->IsParagraphBox())
			{
				pvpboxNew = dynamic_cast<VwParagraphBox *>(pbox);
				if (pvpboxNew != pvpbox)
					break;
			}
		}
		if (!pvpboxNew)
			return false;			// Must be at beginning of first paragraph already.
	}
	if (pvpboxNewOrig == NULL)
		pvpboxNewOrig = pvpboxNew;
	// Find the first editable insertion point in the previous paragraph.
	ichFirst = 0;
	fAssocPrev = false;
	while (!IsEditable(ichFirst, pvpboxNew, fAssocPrev) && cParaLim > 0)
	{
		ich = ForwardOneChar(ichFirst, pvpboxNew, fAssocPrev, pvg, &pvpbox);
		if (pvpbox != pvpboxNew)
		{
			if (++cPara > cParaLim)
			{
				// There's no point in looking forever -- the whole text may be read-only!
				ichFirst = 0;
				pvpboxNew = pvpboxNewOrig;
				break;
			}
			pvpbox = pvpboxNew;
			goto LBackup;		// Back up another paragraph.
		}
		if (ich == ichFirst)
			goto LBackup;
		ichFirst = ich;
		fAssocPrev = !IsBeginningOfLine(ichFirst, pvpboxNew, pvg);
	}
	if (!CheckCommit(ichFirst, pvpboxNew))
	{
		// If commit won't allow the selection to move away, we mustn't.
		// The best way to help ensure that is to return a value that indicates
		// we have already handled the movement request. We've already done all that
		// legitimately can be at this point.
		return true;
	}
	if (pvpboxNew != m_pvpbox)
		nHowChanged = ksctDiffPara;
	m_fAssocPrevious = !IsBeginningOfLine(ichFirst, pvpboxNew, pvg);
	m_fEndBeforeAnchor = false;
	m_ichEnd = ichFirst;
	m_ichAnchor = ichFirst;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_rcBounds.Clear();
	m_qttp = NULL;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Move to the beginning of the next paragraph.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.

	@return True if the down motion was handled, or false if we cannot move down any further in
					this field.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ControlDownArrow(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return false;
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
	}
	LoseSelection(false);

	// Move to the beginning of the next paragraph.
	VwParagraphBox * pvpboxNewOrig = NULL;
	int cParaLim = 0;
	CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
	int cPara = 0;
	VwBox * pbox;
	VwParagraphBox * pvpboxNew;
	VwParagraphBox * pvpbox = m_pvpbox;
LForward:
	pvpboxNew = NULL;
	VwBox * pStartSearch = NULL;
	for (pbox = pvpbox->NextBoxForSelection(&pStartSearch); pbox;
		pbox = pbox->NextBoxForSelection(&pStartSearch))
	{
		if (pbox->IsParagraphBox())
		{
			pvpboxNew = dynamic_cast<VwParagraphBox *>(pbox);
			break;
		}
	}
	if (!pvpboxNew)
		return false;			// Must be at end of last paragraph.

	if (pvpboxNewOrig == NULL)
		pvpboxNewOrig = pvpboxNew;

	// Find the first editable insertion point in the next paragraph.
	int ich = -1;
	int ichFirst = 0;
	ComBool fAssocPrev = false;
	while (!IsEditable(ichFirst, pvpboxNew, fAssocPrev) && cParaLim > 0)
	{
		ich = ForwardOneChar(ichFirst, pvpboxNew, fAssocPrev, pvg, &pvpbox);
		if (pvpboxNew != pvpbox)
		{
			if (++cPara > cParaLim)
			{
				// There's no point in looking forever -- the whole text may be read-only!
				ichFirst = 0;
				pvpboxNew = pvpboxNewOrig;
				break;
			}
			pvpbox = pvpboxNew;
			goto LForward;
		}
		if (ich == ichFirst)
			goto LForward;
		ichFirst = ich;
		fAssocPrev = !IsBeginningOfLine(ichFirst, pvpboxNew, pvg);
	}
	if (!CheckCommit(ichFirst, pvpboxNew))
	{
		// If commit won't allow the selection to move away, we mustn't.
		// The best way to help ensure that is to return a value that indicates
		// we have already handled the movement request. We've already done all that
		// legitimately can be done at this point.
		return true;
	}
	if (pvpboxNew != m_pvpbox)
		nHowChanged = ksctDiffPara;
	m_fAssocPrevious = !IsBeginningOfLine(ichFirst, pvpboxNew, pvg);
	m_fEndBeforeAnchor = false;
	m_ichEnd = ichFirst;
	m_ichAnchor = ichFirst;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_rcBounds.Clear();
	m_qttp = NULL;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Move to the end of the view.
	TODO Sharon: implement visual behavior if we decide we need it

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlEndKey(IVwGraphics * pvg, bool fLogical)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;

	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
	}
	LoseSelection(false);

	VwParagraphBox * pvpboxNew;
	int ichEnd = EndOfView(pvg, &pvpboxNew);
	bool fAssocPrev = ichEnd ? true : false;
	int ich = -1;
	VwParagraphBox * pvpbox;
	// arbitrarily, won't look more than 4 paras for editable text.
	// (this prevents near-infinite loops for large views that aren't editable at all)
	int cParaLim = 0;
	CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
	while (!IsEditable(ichEnd, pvpboxNew, fAssocPrev) && cParaLim > 0)
	{
		fAssocPrev = ichEnd ? true : false;
		// Move backward until we find an editable insertion point.
		ich = BackOneChar(ichEnd, pvpboxNew, fAssocPrev, pvg, &pvpbox);
		if (ich == ichEnd && pvpbox == pvpboxNew)
			break;		// What should we do here?
		if (pvpbox != pvpboxNew)
		{
			if (cParaLim-- <= 0)
			{
				ichEnd = EndOfView(pvg, &pvpboxNew);
				break; // use the very end.
			}
			pvpboxNew = pvpbox;
		}
		ichEnd = ich;
	}
	if (!CheckCommit(ichEnd, pvpboxNew))
		return;
	if (pvpboxNew != m_pvpbox)
		nHowChanged = ksctDiffPara;
	m_ichEnd = ichEnd;
	m_ichAnchor = ichEnd;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_rcBounds.Clear();
	m_fAssocPrevious = m_ichEnd ? true : false;
	m_fEndBeforeAnchor = false;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Move to the beginning of the view.
	TODO Sharon: implement visual behavior if we decide we need it

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlHomeKey(IVwGraphics * pvg, bool fLogical)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;

	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
	}
	LoseSelection(true);
	VwParagraphBox * pvpboxNew;
	int ichEnd = BeginningOfView(pvg, &pvpboxNew);
	bool fAssocPrev = ichEnd ? true : false;
	int ich = -1;
	VwParagraphBox * pvpbox = pvpboxNew;
	int ichEndOrig = ichEnd;
	VwParagraphBox * pvpboxNewOrig = pvpboxNew;
	bool fAssocPrevOrig = fAssocPrev;
	int cParaLim = 0;
	CheckHr(m_qrootb->get_MaxParasToScan(&cParaLim));
	int cPara = 0;
	while (!IsEditable(ichEnd, pvpboxNew) && cParaLim > 0)
	{
		// Move forward until we find an editable insertion point.
		ich = ForwardOneChar(ichEnd, pvpboxNew, fAssocPrev, pvg, &pvpbox);
		if (ich == ichEnd && pvpbox == pvpboxNew)
			break;		// What should we do here?
		if (pvpboxNew != pvpbox)
		{
			if (++cPara > cParaLim)
			{
				// There's no point in looking forever -- the whole text may be read-only!
				ichEnd = ichEndOrig;
				pvpboxNew = pvpboxNewOrig;
				fAssocPrev = fAssocPrevOrig;
				break;
			}
			pvpboxNew = pvpbox;
		}
		ichEnd = ich;
		fAssocPrev = !IsBeginningOfLine(ichEnd, pvpboxNew, pvg);
	}
	if (!CheckCommit(ichEnd, pvpboxNew))
		return;
	if (pvpboxNew != m_pvpbox)
		nHowChanged = ksctDiffPara;
	m_ichEnd = ichEnd;
	m_ichAnchor = ichEnd;
	m_ichAnchor2 = -1;
	m_pvpbox = pvpboxNew;
	m_rcBounds.Clear();
	m_fAssocPrevious = !IsBeginningOfLine(ichEnd, pvpbox, pvg) &&
			IsEditable(m_ichEnd, pvpbox, true);
	m_fEndBeforeAnchor = false;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Extend selection one word left.
	TODO Sharon: implement visual behavior if we decide we need it

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlShiftLeftArrow(IVwGraphics * pvg, bool fLogical)
{
//	if (fLogical)
//	{
		if (m_pvpbox->RightToLeft())
			ControlShiftForwardArrow(pvg);
		else
			ControlShiftBackwardArrow(pvg);
//	}
//	else
//		ControlShiftPhysicalArrow(pvg, false);
}

/*----------------------------------------------------------------------------------------------
	Extend selection one word right.
	TODO Sharon: implement visual behavior if we decide we need it

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlShiftRightArrow(IVwGraphics * pvg, bool fLogical)
{
//	if (fLogical)
//	{
		if (m_pvpbox->RightToLeft())
			ControlShiftBackwardArrow(pvg);
		else
			ControlShiftForwardArrow(pvg);
//	}
//	else
//		ControlShiftPhysicalArrow(pvg, true);
}

/*----------------------------------------------------------------------------------------------
	Extend selection one word backward.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlShiftBackwardArrow(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;
	m_xdIP = -1;
	VwSelChangeType nHowChanged = ksctSamePara;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwParagraphBox * pvpboxNew;
	int ichEnd = BackOneWord(m_ichEnd, pvpboxEnd, pvg, &pvpboxNew);
	//if (pvpboxNew->Container() != m_pvpbox->Container())
	//{
	//	// REVIEW JohnT(SteveMc): should we beep here? allow this more complicated selection?
	//	return;
	//}
	if (!SetEnd(pvpboxNew, pvpboxEnd, ichEnd, true, nHowChanged))
		return;
	m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg);
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Extend selection one word forward.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlShiftForwardArrow(IVwGraphics * pvg)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;
	m_xdIP = -1;
	VwSelChangeType nHowChanged = ksctSamePara;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwParagraphBox * pvpboxNew;
	int ichEnd = ForwardOneWord(m_ichEnd, pvpboxEnd, pvg, &pvpboxNew);
	//if (pvpboxNew->Container() != m_pvpbox->Container())
	//{
	//	// REVIEW JohnT(SteveMc): should we beep here? allow this more complicated selection?
	//	return;
	//}
	if (!SetEnd(pvpboxNew, pvpboxEnd, ichEnd, false, nHowChanged))
		return;
	m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg);
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Extend selection to the beginning of the current paragraph, or to the beginning of the
	previous paragraph if it already ends at the beginning of the current paragraph.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
}

/*----------------------------------------------------------------------------------------------
	Extend selection to the beginning of the next paragraph.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
}

/*----------------------------------------------------------------------------------------------
	Extend selection to end of view.
	TODO Sharon: implement visual behavior if we decide we need it

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlShiftEndKey(IVwGraphics * pvg, bool fLogical)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;
	m_xdIP = -1;
	VwSelChangeType nHowChanged = ksctSamePara;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwParagraphBox * pvpboxNew;
	int ichEnd = EndOfView(pvg, &pvpboxNew);
#if 0
	// JohnT: there seems to be no reason to disallow this...the larger selection can be made
	// by shift-clicking or dragging, and this allows select-all to work properly.
	if (pvpboxNew->Container() != m_pvpbox->Container())
	{
		// REVIEW JohnT(SteveMc): should we beep here? allow this more complicated selection?
		return;
	}
#endif
	if (!SetEnd(pvpboxNew, pvpboxEnd, ichEnd, false, nHowChanged))
		return;
	m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg);
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Extend selection to beginning of view.
	TODO Sharon: implement visual behavior if we decide we need it

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fLogical
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ControlShiftHomeKey(IVwGraphics * pvg, bool fLogical)
{
	AssertPtr(pvg);
	if (!IsEnabled())
		return;
	m_xdIP = -1;
	VwSelChangeType nHowChanged = ksctSamePara;
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwParagraphBox * pvpboxNew;
	int ichEnd = BeginningOfView(pvg, &pvpboxNew);
#if 0
	// This is excluded on the same basis as the corresponding code in ControlShiftEndKey()
	// because it was preventing the <ctrl> + <shift> + <home> key combination from selecting
	// any text.
	if (pvpboxNew->Container() != m_pvpbox->Container())
	{
		// REVIEW JohnT(SteveMc): should we beep here? allow this more complicated selection?
		return;
	}
#endif
	if (!SetEnd(pvpboxNew, pvpboxEnd, ichEnd, true, nHowChanged))
		return;
	m_fAssocPrevious = false;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
}

/*----------------------------------------------------------------------------------------------
	Move the selection to the beginning of the next field.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::TabKey()
{
	HoldScreenGraphics hg(m_pvpbox->Root());
	if (!IsEnabled())
		return false;

	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
	}
	LoseSelection(false);

	// Review JohnT(SteveMc): This is way too slow, and VwSelection may be too low into the
	// guts to handle the concept of "next field" properly anyway.  It also depends on some
	// heuristics: changing between fields either involves an intervening uneditable section,
	// or a change in the tag returned by EditableSubstringAt, or a change in the hvo returned
	// by EditableSubstringAt, and an uneditable section always
	// introduces a new field.
	HVO hvoOrig;
	PropTag tagOrig;
	int ichMinEditProp;
	int ichLimEditProp;
	IVwViewConstructorPtr qvvcEdit;
	int fragEdit;
	VwAbstractNotifierPtr qanote;
	int iprop;
	int itssProp;
	ITsStringPtr qtssPropFirst;
	VwNoteProps vnp;
	VwEditPropVal vepv;
	vepv = CallEditableSubstring(m_pvpbox, m_ichEnd, m_ichEnd, m_fAssocPrevious, &hvoOrig, &tagOrig,
		&ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit, &qanote, &iprop, &vnp,
		&itssProp, &qtssPropFirst);
	// if we haven't found an editable string to tab to, don't continue.
	if (vepv != kvepvEditable)
		return false;

	PropTag tag;
	bool fNewAttr = false;
	VwParagraphBox * pvpboxIP;
	int ichIP;
	bool fAssocPrev = !IsBeginningOfLine(m_ichEnd, m_pvpbox, hg.m_qvg);
	int ich = m_ichEnd;
	VwParagraphBox * pvpbox = m_pvpbox;
	for (;;)
	{
		ichIP = ForwardOneChar(ich, pvpbox, fAssocPrev, hg.m_qvg, &pvpboxIP);
		fAssocPrev = !IsBeginningOfLine(ichIP, pvpboxIP, hg.m_qvg);
		int hvoCurrent;
		vepv = CallEditableSubstring(pvpboxIP, ichIP, ichIP, fAssocPrev, &hvoCurrent,
			&tag, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit, &qanote,
			&iprop, &vnp, &itssProp, &qtssPropFirst);
		if (tag != tagOrig || hvoCurrent != hvoOrig || vepv != kvepvEditable)
			fNewAttr = true;
		if (fNewAttr && vepv == kvepvEditable)
			break;
		if (ichIP == ich && pvpboxIP == pvpbox)
			return false;
		ich = ichIP;
		pvpbox = pvpboxIP;
	}
	if (!CheckCommit(ichIP, pvpboxIP))
		return false;
	m_ichEnd = ichIP;
	m_ichAnchor = m_ichEnd;
	m_ichAnchor2 = -1;
	m_fAssocPrevious = !IsBeginningOfLine(ichIP, pvpboxIP, hg.m_qvg);
	m_fEndBeforeAnchor = false;
	m_pvpbox = pvpboxIP;
	m_rcBounds.Clear();
	m_qttp = NULL;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Move the selection to the beginning of the previous field.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ShiftTabKey()
{
	HoldScreenGraphics hg(m_pvpbox->Root());
	if (!IsEnabled())
		return false;
	m_xdIP = -1;
	VwSelChangeType nHowChanged = ksctSamePara;
	if (m_pvpboxEnd)
	{
		nHowChanged = ksctDiffPara;
		if (m_fEndBeforeAnchor)
		{
			m_ichAnchor = m_ichEnd;
			m_pvpbox = m_pvpboxEnd;
			m_rcBounds.Clear();
		}
		else
		{
			m_ichEnd = m_ichAnchor;
		}
		m_pvpboxEnd = NULL;
		m_rcBounds.Clear();
	}
	else if (m_ichAnchor > m_ichEnd)
	{
		m_ichAnchor = m_ichEnd;
	}
	else if (m_ichAnchor < m_ichEnd)
	{
		m_ichEnd = m_ichAnchor;
	}

	// Review JohnT(SteveMc): This is way too slow, and VwSelection may be too deep into the
	// guts to handle the concept of "previous field" properly anyway.  It also depends on some
	// heuristics: changing between fields either involves an intervening uneditable section,
	// or a change in the tag returned by EditableSubstringAt, and an uneditable section always
	// introduces a new field.
	HVO hvo;
	PropTag tagOrig;
	int ichMinEditProp;
	int ichLimEditProp;
	IVwViewConstructorPtr qvvcEdit;
	int fragEdit;
	VwAbstractNotifierPtr qanote;
	int iprop;
	int itssProp;
	ITsStringPtr qtssPropFirst;
	VwNoteProps vnp;
	VwEditPropVal vepv;
	vepv = CallEditableSubstring(m_pvpbox, m_ichEnd, m_ichEnd, m_fAssocPrevious, &hvo, &tagOrig,
		&ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit, &qanote, &iprop, &vnp,
		&itssProp, &qtssPropFirst);
	// if we haven't found an editable string to tab to, don't continue.
	if (vepv != kvepvEditable)
		return false;

	PropTag tag = tagOrig;
	bool fNewAttr = false;
	VwParagraphBox * pvpboxIP;
	int ichIP;
	bool fAssocPrev = !IsBeginningOfLine(m_ichEnd, m_pvpbox, hg.m_qvg);
	int ich = m_ichEnd;
	VwParagraphBox * pvpbox = m_pvpbox;
	// Move to the end of the previous editable field.
	for (;;)
	{
		ichIP = BackOneChar(ich, pvpbox, fAssocPrev, hg.m_qvg, &pvpboxIP);
		if (ichIP == ich && pvpboxIP == pvpbox)
			return false;
		ich = ichIP;
		pvpbox = pvpboxIP;
		fAssocPrev = !IsBeginningOfLine(ichIP, pvpboxIP, hg.m_qvg);
		vepv = CallEditableSubstring(pvpboxIP, ichIP, ichIP, fAssocPrev, &hvo,
			&tag, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit, &qanote,
			&iprop, &vnp, &itssProp, &qtssPropFirst);
		if (tag != tagOrig || vepv != kvepvEditable)
			fNewAttr = true;
		if (fNewAttr && vepv == kvepvEditable)
			break;
	}
	// Move to the beginning of the previous editable field.
	tagOrig = tag;
	for (;;)
	{
		ichIP = BackOneChar(ich, pvpbox, fAssocPrev, hg.m_qvg, &pvpboxIP);
		if (ichIP == ich && pvpboxIP == pvpbox)
			break;
		fAssocPrev = !IsBeginningOfLine(ichIP, pvpboxIP, hg.m_qvg);
		vepv = CallEditableSubstring(pvpboxIP, ichIP, ichIP, fAssocPrev, &hvo,
			&tag, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit, &qanote,
			&iprop, &vnp, &itssProp, &qtssPropFirst);
		if (tag != tagOrig || vepv != kvepvEditable)
		{
			// Overshoot by one character into the 2nd previous field.
			ichIP = ich;
			pvpboxIP = pvpbox;
			break;
		}
		ich = ichIP;
		pvpbox = pvpboxIP;
	}
	if (!CheckCommit(ichIP, pvpboxIP))
		return false;
	m_ichEnd = ichIP;
	m_ichAnchor = m_ichEnd;
	m_ichAnchor2 = -1;
	m_fAssocPrevious = !IsBeginningOfLine(ichIP, pvpboxIP, hg.m_qvg);
	m_fEndBeforeAnchor = false;
	m_pvpbox = pvpboxIP;
	m_rcBounds.Clear();
	m_qttp = NULL;
	m_pvpbox->Root()->NotifySelChange(nHowChanged);
	return true;
}


/*----------------------------------------------------------------------------------------------
	This method is called when pich represents the character location of the IP within a line
	of boxes. If the IP is in a read-only box, then this method will search the other boxes on
	the same line for the closest editable box. The search begins by going forward. If no
	editable boxes are found when the end of the line is reached, then a search is made going
	backward. If no editable boxes are found on the line, this function returns false. When
	entering this method the current pich and box are proposed values the caller would like
	to use. So if that box is determined to be editable, then no further searching needs to
	take place.

	@param pich - pointer to the proposed character location of the IP.
	@param pvpbox - pointer to the paragraph box where the IP is to be located
	@param pfAssocPrev - Indicates whether the IP should be associated with the previous
	character or not (this is an in and out param).
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::FindEditablePlaceOnLine(int * pich, VwParagraphBox ** ppvpbox,
	bool * pfAssocPrev, IVwGraphics * pvg)
{
	int ichNew = 0;
	int ichCurr = *pich;

//	VwParagraphBox * pvpboxNew = *ppvpbox;
	VwParagraphBox * pvpboxCurr = *ppvpbox;

	bool fAssocPrev = *pfAssocPrev;
	bool fForward = true;

	if (IsEditable(ichCurr, pvpboxCurr, fAssocPrev))
		return true;

	if (IsEditable(ichCurr, pvpboxCurr, !fAssocPrev))
	{
		*pfAssocPrev = !fAssocPrev;
		return true;
	}

	while (!IsEditable(ichCurr, pvpboxCurr, !fForward))
	{
		// Move forward or backward one character to see if we find an editable box.
		// Since we already know that our current place is not editable no matter which way
		// we set fAsscoPrev, we always associate in the direction we're searching.
		if (fForward)
			ichNew = ForwardOneCharInLine(ichCurr, pvpboxCurr, false, pvg);
		else
			ichNew = BackOneCharInLine(ichCurr, pvpboxCurr, true, pvg);

		// Check if we hit the beginning or end of the line.
		if (ichNew == -1)
		{
			// If we've hit the beginning of the line then we didn't find an editable
			// box and we're done.
			if (!fForward)
			{
				return false;
			}
			else
			{
				// We've hit the end of the line without finding an editable box, so search
				// backward from the original location.
				fForward = false;
				ichCurr = *pich;
				pvpboxCurr = *ppvpbox;
			}
		}
		else
		{
			ichCurr = ichNew;
		}
	}

	*pich = ichNew;
//	*ppvpbox = pvpboxNew;
	*pfAssocPrev = ichNew ? !fForward : false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the next valid insertion point following the one given.
	Note that all input/output character indexes are logical.

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param ppvpbox
	@param fSuppressClumping - ?
	@param fLimitToCurrentPara - true if forward is limited to current paragraph only, false
					to also forward to next paragraph if necessary (normal behavior)
----------------------------------------------------------------------------------------------*/
int VwTextSelection::ForwardOneChar(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
	IVwGraphics * pvg, VwParagraphBox ** ppvpbox, bool fSuppressClumping,
	bool fLimitToCurrentPara)
{
	AssertPtr(pvg);
	AssertPtr(ppvpbox);
	AssertPtr(pvpboxIP);

	VwBox * pbox;
	VwBox * pboxNext;
	VwParagraphBox * pvpboxNew = pvpboxIP;
	VwStringBox * psbox;
	ILgSegmentPtr qlseg;
	LgIpValidResult ipvr;
	int ich = -1;
	// The actual minimum is larger than this if ichLogIP points to a surrogate pair or a
	// composite character.
	AssertPtr(pvpboxIP->Source());
	int ichNewMin = pvpboxIP->Source()->LogToRen(ichLogIP + 1);
	int ichBoxMin;
	int ichSegLim;
	VwBox * pStartSearch = NULL;
	// For purposes of moving forward, we always want to associate with the following
	// character. (If this is false, it becomes possible to move 'forward' from the
	// start of a line to the start of an embedded picture caption just above it.)
	pbox = GetStringBox(ichLogIP, pvpboxIP, false);
	if (!pbox && !fLimitToCurrentPara)
	{
		// We must have an empty paragraph, so move to the beginning of the next paragraph.
		for (pbox = pvpboxIP->NextBoxForSelection(&pStartSearch); pbox;
			pbox = pbox->NextBoxForSelection(&pStartSearch))
		{
			if (pbox->IsParagraphBox())
				break;
		}
	}
	for (; pbox; pbox = pboxNext)
	{
		pboxNext = pbox->NextBoxForSelection(&pStartSearch);
		if (pbox->IsStringBox())
		{
			psbox = dynamic_cast<VwStringBox *>(pbox);
			AssertPtr(psbox);
			if (pvpboxNew != pbox->Container())
			{
				// We've moved to a different paragraph. Note that we are not necessarily at the
				// start of it; we may have moved out of an embedded box into a line of the
				// containing paragraph.
				pvpboxNew = dynamic_cast<VwParagraphBox *>(pbox->Container());
				ichNewMin = psbox->IchMin(); // any spot in the new box is OK.
			}
			ichBoxMin = psbox->IchMin();
			qlseg = psbox->Segment();
			CheckHr(qlseg->get_Lim(0, &ichSegLim));
			if (!ichSegLim && ichNewMin == ichBoxMin)
			{
				// If the segment appears to be empty, but we're at the right spot, assume it's
				// a valid insertion point.
				*ppvpbox = pvpboxNew;
				return pvpboxNew->Source()->RenToLog(ichBoxMin);
			}
			else
			{
				// Try for the first valid insertion point within the segment following where
				// we started (ichLogIP).
				for (ich = ichNewMin - ichBoxMin; ich < ichSegLim; ++ich)
				{
					if (fSuppressClumping)
					{
						// The only issue is to avoid being in the middle of a surrogate pair.
						OLECHAR ch;
						CheckHr(pvpboxNew->Source()->Fetch(ich, ich + 1, &ch));
						// If it is a low (second of pair) surrogate, we can't stop here.
						if (IsLowSurrogate(ch))
							continue;
					}
					else
					{
						CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichBoxMin + ich,
							&ipvr));
						if (ipvr != kipvrOK)
							continue;
					}
					// If no problem, use this position.
					*ppvpbox = pvpboxNew;
					return pvpboxNew->Source()->RenToLog(ichBoxMin + ich);
				}
			}
			int ichLim = ichBoxMin + ichSegLim;
			if (ichLim > ichNewMin)
			{
				// No valid insertion points within the segment, try the end of segment.
				ichNewMin = ichLim;
			}
			if (ichNewMin == ichLim)
			{
				if (!pboxNext || !pboxNext->IsStringBox())
				{
					//	Moving to the trailing edge of the paragraph or text range.
					*ppvpbox = pvpboxNew;
					return pvpboxNew->Source()->RenToLog(ichNewMin);
				}
			}
		}
	}
	// We must already be at the Lim of the last paragraph.
	*ppvpbox = pvpboxIP;
	return ichLogIP;
}

/*----------------------------------------------------------------------------------------------
	Get the type of character: space, alphanumeric, or punctuation.

	@param pcpe
	@param chw
----------------------------------------------------------------------------------------------*/
static CharacterType GetCharacterType(ILgCharacterPropertyEngine * pcpe, OLECHAR chw)
{
	ComBool fIsSeparator;
	CheckHr(pcpe->get_IsSeparator(chw, &fIsSeparator));
	if (fIsSeparator)
		return kSpace;

	ComBool fIsLetter;
	CheckHr(pcpe->get_IsWordForming(chw, &fIsLetter));
	if (fIsLetter)
		return kAlpha;

	ComBool fIsNumber;
	CheckHr(pcpe->get_IsNumber(chw, &fIsNumber));
	return fIsNumber ? kAlpha : kPunc;
}

/*----------------------------------------------------------------------------------------------
	Get the valid insertion point at the closest beginning of a word following the given point,
	if it exists.
	Note that all input/output character indexes are logical.

	@param ichLogIP
	@param pvpboxIP
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param ppvpbox
----------------------------------------------------------------------------------------------*/
int VwTextSelection::ForwardOneWord(int ichLogIP, VwParagraphBox * pvpboxIP, IVwGraphics * pvg,
	VwParagraphBox ** ppvpbox)
{
	AssertPtr(pvpboxIP);
	AssertPtr(pvg);
	AssertPtr(ppvpbox);

	VwBox * pbox;
	VwParagraphBox * pvpbox;
	VwTxtSrc * pts;
	int cch;
	int ichMin = ichLogIP;
	int ichLim;
	int ich = -1;
	OLECHAR rgch[kcchTemp];
	enum { kInitial, kSkipPunc, kSkipAlpha, kWantNonSpace, kFinal } state = kInitial;
	CharacterType chtype;
	VwBox * pStartSearch = NULL;

	// Scan through the given paragraph box, possibly moving to the next one.
	for (pbox = pvpboxIP; pbox; pbox = pbox->NextBoxForSelection(&pStartSearch))
	{
		if (!pbox->IsParagraphBox())
			continue;
		pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
		pts = pvpbox->Source();
		AssertPtr(pts);
		if (pvpbox != pvpboxIP)
		{
			ichMin = 0;
			state = kWantNonSpace;
		}

		// get the initial props so we can know when it changes
		ITsTextProps * pttpInitial = NULL;
		OLECHAR ch;
		pts->CharAndPropsAt(ichMin, &ch, &pttpInitial);
		// This CPE will do for almost all purposes, at least until we enhance the algorithm to
		// allow a 'word' to contain characters with different properties. When we encounter
		// differnt properties, we immediately set state kFinal. After that, we can only toggle
		// between states kFinal and kWantNonSpace, both of which only test characters for being
		// spaces. Currently, which WS a CPE belongs to makes no difference to its answers
		// to whether a character is space.
		// The one exception is looking ahead for another alpha on the other side of a single quote.
		ILgCharacterPropertyEnginePtr qcpe;
		GetCpeFromRootAndProps(m_qrootb, pttpInitial, &qcpe);

		for (cch = pts->Cch(); ichMin < cch; ichMin = ichLim)
		{
			ichLim = ichMin + kcchTemp;
			if (ichLim > cch)
				ichLim = cch;
			pts->FetchLog(ichMin, ichLim, rgch);
			for (ich = ichMin; ich < ichLim; ++ich)
			{
				// get the props at this point to check for a run change
				ITsTextProps * pttpCurrent = NULL;
				pts->CharAndPropsAt(ich, &ch, &pttpCurrent);
				bool fPropsChanged = false;
				if (pttpInitial != pttpCurrent)
				{
					state = kFinal;
					fPropsChanged = true; // at run boundary
				}
				chtype = GetCharacterType(qcpe, rgch[ich - ichMin]);
				switch (state)
				{
				case kInitial:
					switch (chtype)
					{
					case kSpace:
								// Skip over any number of leading separator characters.
						break;
					case kPunc:
						state = kSkipPunc;
						break;
					case kAlpha:
						state = kSkipAlpha;
						break;
					}
					break;
				case kSkipPunc:
					switch (chtype)
					{
					case kSpace:
						state = kWantNonSpace;
						break;
					case kPunc:
								// Skip over any number of punctuation characters.
						break;
					case kAlpha:
						state = kFinal;
						break;
					}
					break;
				case kSkipAlpha:
					switch (chtype)
					{
					case kSpace:
						state = kWantNonSpace;
						break;
					case kPunc:
						if (rgch[ich - ichMin] == '\'')
						{
								// Treat an ASCII single quote (punctuation character) as an
								// apostrophe (alphanumeric character) if it is surrounded by
								// two alphanumeric characters.
							int ich2 = ich + 1;
							if (ich2 < ichLim)
							{
								ITsTextProps * pttpFollow = NULL;
								OLECHAR chFollow;
								pts->CharAndPropsAt(ich2, &chFollow, &pttpFollow);
								// To properly test the following charcter, technically we must try it with
								// its own CPE.
								ILgCharacterPropertyEnginePtr qcpeFollow;
								GetCpeFromRootAndProps(m_qrootb, pttpFollow, &qcpeFollow);
								chtype = GetCharacterType(qcpeFollow, chFollow);
								if (chtype == kAlpha)
									break;
							}
						}
						state = kFinal;
						break;
					case kAlpha:
								// Skip over any number of nonseparator, nonpunctuation
								// characters.
						break;
					}
					break;
				case kWantNonSpace:
					switch (chtype)
					{
					case kSpace:
								// Skip over any number of trailing separator characters.
						break;
					case kPunc:
					case kAlpha:
						state = kFinal;
						break;
					}
					break;
				case kFinal:
					if (chtype == kSpace)
						state = kWantNonSpace;
					break;
				}
				if (state == kFinal)
				{
					bool fAssocPrev = !IsBeginningOfLine(ich,pvpbox, pvg) &&
						IsEditable(ich, pvpbox, true);
					VwStringBox * psbox = GetStringBox(ich, pvpbox, fAssocPrev);
					if (!psbox)
						continue;
					ILgSegmentPtr qlseg = psbox->Segment();
					int ichBoxMin = psbox->IchMin();
					int ichSegLim;
					CheckHr(qlseg->get_Lim(0, &ichSegLim));
					Assert(ich >= ichBoxMin || ich <= ichBoxMin + ichSegLim);
					LgIpValidResult ipvr;
					CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ich,
						&ipvr));
					// if the properties have changed, then don't change the state from final
					// here: we want to stop where the run properties change.
					if (ipvr != kipvrOK && !fPropsChanged)
					{
						state = kWantNonSpace;
						continue;
					}
					else if (fPropsChanged && fAssocPrev)
					{
						psbox = GetStringBox(ich, pvpbox, false);
						if (!psbox)
							continue;
						qlseg = psbox->Segment();
						ichBoxMin = psbox->IchMin();
						CheckHr(qlseg->get_Lim(0, &ichSegLim));
						Assert(ich >= ichBoxMin || ich <= ichBoxMin + ichSegLim);
						CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ich,
							&ipvr));
					}
					*ppvpbox = pvpbox;
					m_qttp = NULL;
					return ich;
				}
			}
			if ((pvpbox == pvpboxIP && ich != ichLogIP) || ich > 0)
			{
				// We must have arrived at the lim of the paragraph.
				bool fAssocPrev = !IsBeginningOfLine(ich,pvpbox, pvg) &&
					IsEditable(ich, pvpbox, true);
				VwStringBox * psbox = GetStringBox(ich, pvpbox, fAssocPrev);
				if (!psbox)
					continue;
				ILgSegmentPtr qlseg = psbox->Segment();
				int ichBoxMin = psbox->IchMin();
				int ichSegLim;
				CheckHr(qlseg->get_Lim(0, &ichSegLim));
				Assert(ich >= ichBoxMin || ich <= ichBoxMin + ichSegLim);
				LgIpValidResult ipvr;
				CheckHr(qlseg->IsValidInsertionPoint(ich, pvg, ichBoxMin + ichSegLim,
					&ipvr));
				if (ipvr != kipvrOK)
				{
					state = kWantNonSpace;
					continue;
				}
				*ppvpbox = pvpbox;
				m_qttp = NULL;
				return ich;
			}
		}
	}
	// We must have started at the lim of the last paragraph.
	*ppvpbox = pvpboxIP;
	m_qttp = NULL;
	return ichLogIP;
}

/*----------------------------------------------------------------------------------------------
	Get the last valid insertion point on the same line in this paragraph as ichIP.
	Return ichLogIP if no valid insertion points can be found on the line.
	Note that all input/output character indexes are logical.
	TODO SteveMc: Handle kipvrUnknown properly

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
int VwTextSelection::EndOfLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
	IVwGraphics * pvg)
{
	int ichIP = pvpboxIP->Source()->LogToRen(ichLogIP);
	AssertPtr(pvg);
	AssertPtr(pvpboxIP);

	VwBox * pbox;
	VwStringBox * psbox;
	bool fBaselineSet = false;
	int nBaselineIP = 0;
	int nBaseline;
	int ich = -1;
	int ichNew = ichIP;
	ILgSegmentPtr qlseg;
	LgIpValidResult ipvr;
	int ichSegLim;
	VwBox * pStartSearch = NULL;
	for (pbox = GetStringBox(ichLogIP, pvpboxIP, fAssocPrev);
		pbox;
		pbox = pbox->NextBoxForSelection(&pStartSearch))
	{
		if (pbox->IsStringBox())
		{
			if (!fBaselineSet)
			{
				fBaselineSet = true;
				nBaselineIP = pbox->Top() + pbox->Ascent();
			}
			nBaseline = pbox->Top() + pbox->Ascent();
			if (nBaseline == nBaselineIP)
			{
				psbox = dynamic_cast<VwStringBox *>(pbox);
				qlseg = psbox->Segment();
				int ichBoxMin = psbox->IchMin();
				CheckHr(qlseg->get_Lim(0, &ichSegLim));
				for (ich = ichSegLim; ich >= 0; --ich)
				{
					CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichBoxMin+ich,
						&ipvr));
					if (ipvr == kipvrOK ||
						ipvr == kipvrUnknown)	// for now, assume okay
					{
						ichNew = psbox->IchMin() + ich;
						break;
					}
				}
			}
			else
			{
				break;
			}
		}
		else if (pbox->IsParagraphBox())
		{
			break;
		}
	}
	return pvpboxIP->Source()->RenToLog(ichNew);
}

/*----------------------------------------------------------------------------------------------
	Get the valid insertion point closest to one line visually below the given one, if it
	exists.
	Note that all input/output character indexes are logical.

	@param ichLogIP - initial insertion point character index in the paragraph
	@param pvpboxIP - points to the paragraph box of the initial insertion point
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
	@param ppvpbox - address of pointer to the new insertion point's paragraph box
	@param pichLogHome - address of an integer to receive the character index of the beginning
					of the line for the new insertion point
----------------------------------------------------------------------------------------------*/
int VwTextSelection::DownOneLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
	IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, VwParagraphBox ** ppvpbox,
	int * pichLogHome)
{
	AssertPtr(pvpboxIP);
	AssertPtr(pvg);
	AssertPtr(ppvpbox);
	AssertPtr(pichLogHome);

	IVwGraphicsPtr qvg = pvg;
	VwRootBox * prootb = pvpboxIP->Root();
	Assert(prootb);
	RECT rdIP;
	RECT rdIPSec;
	ComBool fSplitIP;
	pvpboxIP->LocOfSelection(qvg, ichLogIP, ichLogIP, fAssocPrev, rcSrcRoot, rcDstRoot,
		&rdIP, &rdIPSec, &fSplitIP, true);
	if (m_xdIP == -1)
		m_xdIP = (rdIP.left + rdIP.right) / 2;
	int yd = (rdIP.top + rdIP.bottom) / 2;
	if (yd < rdIP.top)
		yd = rdIP.top;
	Rect rcSrc;
	Rect rcDst;
	// Note that the cursor can sometimes stick so far down that (m_xdIP,yd) will find the box
	// below the one we want.
	VwBox * pboxOrig = prootb->FindBoxClicked(qvg, m_xdIP, (rdIP.top + rdIP.bottom) / 2,
		rcSrcRoot, rcDstRoot, &rcSrc, &rcDst);
	if (!pboxOrig)
	{
		// If we don't know where we're starting, how can we go anywhere?
		*ppvpbox = pvpboxIP;
		int ichHome = BeginningOfLine(ichLogIP, pvpboxIP, fAssocPrev, qvg);
		*pichLogHome = ichHome;
		return ichLogIP;
	}
	int twHeight;
	CheckHr(prootb->get_Height(&twHeight));
	int yBaselineOrig = CalculateBaseline(pboxOrig);
	if (yBaselineOrig < yd)
		yBaselineOrig = yd;
LNextLine:
	VwBox * pbox = NULL;
	int yBaselineNew = yBaselineOrig;		// Definitely not > yBaselineOrig.
	do
	{
		yd += 3;
		if (yd > twHeight)
			break;
		pbox = prootb->FindBoxClicked(qvg, m_xdIP, yd, rcSrcRoot, rcDstRoot, &rcSrc, &rcDst);
		while (pbox && pbox->IsLazyBox())
		{
			// Before we expand lazy boxes, we need to do something about yd
			// so that if the box we were looking at changes its position, they are still
			// relative to it.
			int ys = rcDstRoot.MapYTo(yd, rcSrcRoot);
			int dys = ys - yBaselineOrig; // This is constant whatever expand does.
			// Need to expand the lazy box...specifically expand its first item.
			pbox->Expand();

			// Baseline of original start box can be recomputed at new position.
			yBaselineOrig = CalculateBaseline(pboxOrig);
			// yd can then be recovered
			ys = dys + yBaselineOrig;
			yd = rcSrcRoot.MapYTo(ys, rcDstRoot);
			// See what we clicked on now we expanded it. This might still be lazy,
			// which is why this is a loop!
			pbox = prootb->FindBoxClicked(qvg, m_xdIP, yd, rcSrcRoot, rcDstRoot, &rcSrc,
				&rcDst);
		}
		if (pbox)
		{
			yBaselineNew = CalculateBaseline(pbox);
		}
	} while (!pbox || pbox == pboxOrig || yBaselineNew <= yBaselineOrig || !pbox->IsStringBox());
	if (pbox && yBaselineNew > yBaselineOrig)
	{
		VwSelectionPtr qvwsel;
		pbox->GetSelection(qvg, prootb, m_xdIP, yd, rcSrcRoot, rcDstRoot, rcSrc, rcDst,
			&qvwsel);
		AssertPtr(qvwsel.Ptr());
		VwTextSelection * pvwsel = dynamic_cast<VwTextSelection *>(qvwsel.Ptr());
		// ENHANCE JohnT: figure what to do here if we ever have other types.
		if (pvwsel)
		{
			int ichNew = pvwsel->m_ichEnd;
			int ichHome = pvwsel->BeginningOfLine(pvwsel->m_ichEnd, pvwsel->m_pvpbox,
				pvwsel->m_fAssocPrevious, qvg);
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
			if (psbox && ichNew < psbox->IchMin())
			{
				// Eg, string box that starts in the middle of an internal link--skip this line.
				// Review SteveMc (SharonC): is the test above adequate?
				pboxOrig = pbox;
				yBaselineOrig = yBaselineNew;
				goto LNextLine;
			}
			VwParagraphBox * pvpboxNew = pvwsel->m_pvpbox;
			// Check how close the insertion point is to where we want it.
			RECT rdPrimary;
			RECT rdSecondary;
			ComBool fSplit;
			pvpboxNew->LocOfSelection(qvg, ichNew, ichNew, ichNew != ichHome,
				rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit, true);
			int xdIPNew = (rdPrimary.left + rdPrimary.right) / 2;
			if (abs(m_xdIP - xdIPNew) > 20)
			{
				// Move down some more to see if we can get a better insertion point.
				VwBox * pbox2 = NULL;
				int yd2;
				int yBaselineNew2 = yBaselineNew - 1;	// Definitely not == yBaselineNew.
				int ydEnd2 = yd + pbox->Height() + 5;
				if (ydEnd2 > twHeight)
					ydEnd2 = twHeight;
				for (yd2 = yd + 3; yd2 <= ydEnd2; yd2 += 3)
				{
					pbox2 = prootb->FindBoxClicked(qvg, m_xdIP, yd2, rcSrcRoot, rcDstRoot,
						&rcSrc, &rcDst);
					if (pbox2 && pbox2 != pbox)
					{
						if (pbox2->Container() != pbox->Container() ||
							pbox2->Baseline() != pbox->Baseline())
						{
							pbox2 = NULL;
						}
						else
						{
							yBaselineNew2 = CalculateBaseline(pbox2);
						}
						break;
					}
				}
				if (pbox2 && pbox2 != pbox && yBaselineNew2 == yBaselineNew)
				{
					pbox2->GetSelection(qvg, prootb, m_xdIP, yd2, rcSrcRoot, rcDstRoot,
						rcSrc, rcDst, &qvwsel);
					AssertPtr(qvwsel.Ptr());
					VwTextSelection * pvwsel2 = dynamic_cast<VwTextSelection *>(qvwsel.Ptr());
					// ENHANCE JohnT: figure what to do if we have other types.
					if (pvwsel2)
					{
						int ichNew2 = pvwsel2->m_ichEnd;
						int ichHome2 = pvwsel2->BeginningOfLine(pvwsel2->m_ichEnd,
							pvwsel2->m_pvpbox, false, qvg);
						VwParagraphBox * pvpboxNew2 = pvwsel2->m_pvpbox;
						if (pvpboxNew2 == pvpboxNew)
						{
							// Check how close the insertion point is to where we want it.
							pvpboxNew2->LocOfSelection(qvg, ichNew2, ichNew2,
								ichNew2 != ichHome2, rcSrcRoot, rcDstRoot,
								&rdPrimary, &rdSecondary, &fSplit, true);
							int xdIPNew2 = (rdPrimary.left + rdPrimary.right) / 2;
							if (abs(m_xdIP - xdIPNew2) < abs(m_xdIP - xdIPNew))
							{
								// Do we need to check for being a valid insertion point?
								*ppvpbox = pvpboxNew;
								*pichLogHome = ichHome2;
								return ichNew2;
							}
						}
					}
				}
			}
			// Do we need to check for being a valid insertion point?
			*ppvpbox = pvpboxNew;
			*pichLogHome = ichHome;
			return ichNew;
		}
		else
		{
			*pichLogHome = 0;
			if (pbox->IsParagraphBox())
			{
				*ppvpbox = dynamic_cast<VwParagraphBox *>(pbox);
				return 0;
			}
		}
	}

	// Can't find anywhere to go!?
	*ppvpbox = pvpboxIP;
	*pichLogHome = BeginningOfLine(ichLogIP, pvpboxIP, fAssocPrev, qvg);
	return ichLogIP;
}

/*----------------------------------------------------------------------------------------------
	Get the final insertion point in the entire view (logical chars).
	This returns a logical character position.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param ppvpbox
----------------------------------------------------------------------------------------------*/
int VwTextSelection::EndOfView(IVwGraphics * pvg, VwParagraphBox ** ppvpbox)
{
	AssertPtr(pvg);
	AssertPtr(ppvpbox);

	VwParagraphBox * pvpboxNew = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwBox * pbox;
	VwStringBox * psbox;
	int ichSegLim;
	int ich = -1;
	ILgSegmentPtr qlseg;
	LgIpValidResult ipvr;
	for (pbox = m_pvpbox->Root(); pbox; pbox = pbox->NextInReverseRootSeq())
	{
		if (pbox->IsParagraphBox())
		{
			pvpboxNew = dynamic_cast<VwParagraphBox *>(pbox);
		}
		else if (pbox->IsStringBox())
		{
			// Find the last valid insertion point in this string box (if any exist).
			psbox = dynamic_cast<VwStringBox *>(pbox);
			int ichBoxMin = psbox->IchMin();
			qlseg = psbox->Segment();
			CheckHr(qlseg->get_Lim(0, &ichSegLim));
			for (ich = ichSegLim; ich >= 0; --ich)
			{
				CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichBoxMin+ich, &ipvr));
				if (ipvr == kipvrOK ||
					ipvr == kipvrUnknown) // since this is the last box in the sequence
				{
					*ppvpbox = pvpboxNew;
					return pvpboxNew->Source()->RenToLog(psbox->IchMin() + ich);
				}
			}
		}
	}
	// We should never reach here, but just in case, and to keep the compiler happy:
	*ppvpbox = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	return m_ichEnd;
}

// Given an insertion point at ichlogIP in the specified paragraph, determine the position
// that should be interpreted as 'one word forward' or 'one word back' (depending on fForward)
// ...currently used for CTRL-backspace and CTRL-Delete, so we're looking for the amount of text
// to delete for these commands.
// Specifically,
// If we're at the paragraph boundary in the relevant direction answer ichLogIP.
// Otherwise move at least one character in the specified direction.
// Don't worry about valid insertion points (unless we reach the bounds of the whole string),
// since the user probably wants to delete something, and it's unlikely something that looks
// like a word boundary isn't a valid IP position, anyway.
// White space may be skipped until we see an alphabetic character; after that, stop just before
// the first white space.
// Also stop just before the first punctuation character, unless that means not moving at all,
// in which case we skip a single punctuation character.
// Enhance JohnT: this doesn't adequately handle surrogate pairs that are non-word-forming.
int VwTextSelection::FindWordBoundary(int ichLogIP, VwParagraphBox * pvpboxIP,
	IVwGraphics * pvg, bool fForward)
{
	AssertPtr(pvpboxIP);
	AssertPtr(pvg);
	Assert(ichLogIP >= 0);

	ILgCharacterPropertyEnginePtr qcpe;

	VwTxtSrc * pts = pvpboxIP->Source();
	AssertPtr(pts);
	int cch = pts->Cch(); // logical chars in source of paragraph.
	int ich = ichLogIP;
	CharacterType chtype;
	CharacterType chPrevType = kSpace;

	OLECHAR *rgch = new OLECHAR[cch + 1]; // logical characters in entire paragraph text source.
	OLECHAR ch;
	pts->FetchLog(0, cch, rgch);
	bool fFoundAlpha = false;
	int irun;
	int ichMin, ichLim, itss;
	ITsStringPtr qtss;
	ITsTextPropsPtr qttp;
	VwPropertyStorePtr qvwps;
	int wsFirstChar;

	if (ichLogIP == 0 && !fForward)
		return 0; // can't go back from start of para.
	if (ichLogIP == cch && fForward)
		return ichLogIP; // can't go forward from end of para
	int ichFirstCharInDirection = fForward ? ich : ich - 1;

	// Determine the string and range of characters that we can consider.
	pts->StringFromIch(ichFirstCharInDirection, false, &qtss, &ichMin, &ichLim, &qvwps, &itss);
	if (qtss)
	{
		int iFirstRun;
		CheckHr(qtss->get_RunAt(ichFirstCharInDirection - ichMin, &iFirstRun));
		CheckHr(qtss->get_Properties(iFirstRun, &qttp));
		int tmp;
		CheckHr(qttp->GetIntPropValues(ktptWs, &tmp, &wsFirstChar));
	}
	else
	{
		// The very first character is an ORC; consider it a complete word to delete.
		Assert(rgch[ich] == 0xFFFC);
		delete [] rgch;
		return fForward ? ich + 1 : ich - 1;
	}

	while( fForward ? ich < ichLim : ich > ichMin)
	{
		int ichNextCharToConsider = fForward ? ich : ich - 1;
		ch = rgch[ichNextCharToConsider];
		CheckHr(qtss->get_RunAt(ichNextCharToConsider - ichMin, &irun));
		ITsTextPropsPtr qttpCurr;
		CheckHr(qtss->get_Properties(irun, &qttpCurr));
		int tmp, ws;
		CheckHr(qttpCurr->GetIntPropValues(ktptWs, &tmp, &ws));
		GetCpeFromRootAndProps(m_qrootb, qttpCurr, &qcpe);
		chtype = GetCharacterType(qcpe, ch);

		if (chtype == kAlpha)
			fFoundAlpha = true;
		if (ich != ichLogIP)
		{
			// we've moved...did we find a boundary?
			if( (chtype == kSpace && chPrevType != kSpace) ||	// we are a space, and previously found non-space
				(chPrevType == kPunc) ||	// each punct counts as a word, so if we've passed one we stop.
				(chtype == kPunc && fFoundAlpha) || // stop before punct if we've found some word text.
				ws != wsFirstChar) // any ws change counts as a word boundary.
			{
				delete [] rgch;
				return ich;
			}
		}

		chPrevType = chtype;
		ich = fForward ? ich + 1 : ich - 1;
	}
	// We reached the limits of the string; delete to the string boundary.
	delete [] rgch;
	return ich;
}

/*----------------------------------------------------------------------------------------------
	Get the previous valid insertion point just prior to the one given, if one exists.
	Note that all input/output character indexes are logical.

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param ppvpbox
----------------------------------------------------------------------------------------------*/
int VwTextSelection::BackOneChar(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
	IVwGraphics * pvg, VwParagraphBox ** ppvpbox, bool fSuppressClumping)
{
	int ichIP = pvpboxIP->Source()->LogToRen(ichLogIP);
	AssertPtr(pvpboxIP);
	AssertPtr(pvg);
	AssertPtr(ppvpbox);

	VwBox * pbox;
	VwParagraphBox * pvpboxNew = pvpboxIP;
	VwStringBox * psbox;
	ILgSegmentPtr qlseg;
	LgIpValidResult ipvr;
	int ich = -1;
	int ichBoxMin;
	int ichSegLim;
	bool fOrigBox = true;
	bool fSamePara = false;
	int ichMinPrevBox = 0; // if fSamePara is true, holds ichBoxMin of previous string box.
	pbox = GetStringBox(ichLogIP, pvpboxIP, fAssocPrev);
	if (!pbox)
	{
		// We must have an empty paragraph, so move to the end of the preceding paragraph.
		fOrigBox = false;
		for (pbox = pvpboxIP->NextInReverseRootSeq(); pbox; pbox = pbox->NextInReverseRootSeq())
		{
			if (pbox->IsParagraphBox())
				break;
		}
	}
	for (; pbox; pbox = pbox->NextInReverseRootSeq())
	{
		if (pbox->IsStringBox())
		{
			psbox = dynamic_cast<VwStringBox *>(pbox);
			if (pvpboxNew != pbox->Container())
			{
				// We've moved to a different paragraph. Note that we are not necessarily at the
				// end of it; we may have moved out of an embedded box into a line of the
				// containing paragraph.
				pvpboxNew = dynamic_cast<VwParagraphBox *>(pbox->Container());
				fSamePara = false;
			}
			ichBoxMin = psbox->IchMin();
			qlseg = psbox->Segment();
			CheckHr(qlseg->get_Lim(0, &ichSegLim));
			if (fOrigBox)
				ichSegLim = ichIP - ichBoxMin - 1;
			else if (fSamePara)
				// we tried character positions down to ichMinPrevBox in another box
				// of this para, so start at least one char before that. Also, there
				// may be intervening non-text boxes, so don't try positions in them.
				// However, if there are intervening non-text boxes, it is OK to move
				// to the last position of this string box.
				ichSegLim = min(ichSegLim, ichMinPrevBox - ichBoxMin - 1);
			for (ich = ichSegLim; ich >= 0; --ich)
			{
				if (fSuppressClumping)
				{
					// The only issue is to avoid being in the middle of a surrogate pair.
					OLECHAR ch;
					CheckHr(pvpboxNew->Source()->Fetch(ich, ich + 1, &ch));
					// If it is a low (second of pair) surrogate, we can't stop here.
					if (IsLowSurrogate(ch))
						continue;
				}
				else
				{
					CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichBoxMin+ich, &ipvr));
					// kipvrUnknown is acceptable in this case because we already checked the
					// next segment.
					if (ipvr == kipvrBad)
						continue;
				}
				*ppvpbox = pvpboxNew;
				return pvpboxNew->Source()->RenToLog(ichBoxMin + ich);
			}
			fSamePara = true;
			ichMinPrevBox = ichBoxMin; // for next time.
		}
		fOrigBox = false;
	}
	// We must already be at the Min of the first paragraph.
	*ppvpbox = pvpboxIP;
	return pvpboxIP->Source()->RenToLog(ichIP);
}

/*----------------------------------------------------------------------------------------------
	Get the valid insertion point at the closest beginning of a word before the given point,
	if it exists.
	Note that all input/output character indexes are logical.

	@param ichLogIP
	@param pvpboxIP
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param ppvpbox
----------------------------------------------------------------------------------------------*/
int VwTextSelection::BackOneWord(int ichLogIP, VwParagraphBox * pvpboxIP, IVwGraphics * pvg,
	VwParagraphBox ** ppvpbox)
{
	AssertPtr(pvpboxIP);
	AssertPtr(pvg);
	AssertPtr(ppvpbox);
	VwBox * pbox;
	VwParagraphBox * pvpbox;
	VwTxtSrc * pts;
	int cch;
	int ichMin;
	int ichLim = ichLogIP;
	int ich = -1;
	OLECHAR rgch[kcchTemp+1];
	ComBool fIsPunctuation;
	ComBool fIsSeparator;
	enum { kInitial, kSkipPunc, kSkipAlpha, kSkipSpace, kFinal } state = kInitial;
	CharacterType chtype;

	// Scan through the given paragraph box, possibly moving to the preceding one.
	for (pbox = pvpboxIP; pbox; pbox = pbox->NextInReverseRootSeq())
	{
		if (!pbox->IsParagraphBox())
			continue;
		pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
		pts = pvpbox->Source();
		AssertPtr(pts);
		if (pvpbox != pvpboxIP)
		{
			ichLim = pts->Cch();
			state = kSkipSpace;
		}
		cch = pts->Cch();
		if (ichLim > cch)
			ichLim = cch;
		ichMin = ichLim - kcchTemp;
		if (ichMin < 0)
			ichMin = 0;
		OLECHAR ch;

		// this will hold the initial props so we can know when it changes. Don't get the
		// initial props until we have moved back the first character.
		ITsTextProps * pttpInitial = NULL;

		ILgCharacterPropertyEnginePtr qcpe;
		while (ichMin < ichLim)
		{
			pts->FetchLog(ichMin, ichLim, rgch);
			// Scan backward through the characters in this piece of the segment.
			for (ich = ichLim; --ich >= ichMin; )
			{
				int ichIP = ich + 1;	// loop moves one past desired insert position
				ch = rgch[ich - ichMin];

				// If the props have changed, then this is a new run, don't go any further
				OLECHAR chDummy;
				ITsTextProps * pttpCurrent = NULL;
				bool fPropsChanged = false;
				if (pttpInitial == NULL)
				{
					pts->CharAndPropsAt(ich, &chDummy, &pttpInitial);
					// Enhance JohnT: We can get the WS and CPE just once because currently this code stops
					// at any change of properties. To be more consistent with spelling and double-click
					// code, it should ignore changes in properties other than writing system and editability
					// (and spell-checkability).
					GetCpeFromRootAndProps(m_qrootb, pttpInitial, &qcpe);
				}
				else
				{
					pts->CharAndPropsAt(ich, &chDummy, &pttpCurrent);
					if (pttpCurrent != pttpInitial)
					{
						state = kFinal;
						fPropsChanged = true; // character is at run boundary
					}
				}
				chtype = GetCharacterType(qcpe, ch);

				switch (state)
				{
				case kInitial:
					switch (chtype)
					{
					case kSpace:
						// Skip over any number of initial separator characters.
						break;
					case kPunc:
						state = kSkipPunc;
						break;
					case kAlpha:
						state = kSkipAlpha;
						break;
					}
					break;
				case kSkipPunc:
					switch (chtype)
					{
					case kSpace:
						state = kFinal;
						break;
					case kPunc:
						// Skip over any number of punctuation characters.
						break;
					case kAlpha:
						state = kFinal;
						break;
					}
					break;
				case kSkipAlpha:
					switch (chtype)
					{
					case kSpace:
						state = kFinal;
						break;
					case kPunc:
						if (rgch[ich - ichMin] == '\'')
						{
								// Treat an ASCII single quote (punctuation character) as an
								// apostrophe (alphanumeric character) if it is surrounded by
								// two alphanumeric characters.
							int ich2 = ich - 1;
							if (ich2 >= ichMin)
							{
								ITsTextProps * pttpPrev = NULL;
								OLECHAR chPrev;
								pts->CharAndPropsAt(ich2, &chPrev, &pttpPrev);
								// To properly test the preceding charcter, technically we must try it with
								// its own CPE.
								ILgCharacterPropertyEnginePtr qcpePrev;
								GetCpeFromRootAndProps(m_qrootb, pttpPrev, &qcpePrev);
								chtype = GetCharacterType(qcpePrev, chPrev);
								if (chtype == kAlpha)
									break;
							}
						}
						state = kFinal;
						break;
					case kAlpha:
						// Skip over any number of alpha characters until beginning of paragraph.
						if (ich == 0)
						{
							state = kFinal;
							ichIP = ich;	// Leave insert position at beginning of paragraph
						}
						break;
					}
					break;
				case kSkipSpace:
					if (chtype != kSpace)
						state = kFinal;
					break;
				case kFinal:
					break;
				}
				if (state == kFinal)
				{
					// We always overshoot by one going backward: hence ich + 1 in this block.
					bool fAssocPrev = !IsBeginningOfLine(ichIP, pvpbox, pvg) &&
						IsEditable(ichIP, pvpbox, true);
					VwStringBox * psbox = GetStringBox(ichIP, pvpbox, fAssocPrev);
					if (!psbox)
						continue;
					ILgSegmentPtr qlseg = psbox->Segment();
					int ichBoxMin = psbox->IchMin();
					int ichSegLim;
					CheckHr(qlseg->get_Lim(0, &ichSegLim));
					Assert(ichIP >= ichBoxMin || ichIP <= ichBoxMin + ichSegLim);
					LgIpValidResult ipvr;
					CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichIP, &ipvr));
					// if the properties have changed, then don't change the state from final
					// here: we want to stop where the run properties change.
					if (ipvr != kipvrOK && !fPropsChanged)
					{
						state = kInitial;		// ?????????
						continue;
					}
					*ppvpbox = pvpbox;
					m_qttp = NULL;
					return ichIP;
				}
			}
			ichLim = ichMin;
			ichMin -= kcchTemp;
			if (ichMin < 0)
				ichMin = 0;
		}
		if (ichLogIP || pvpbox != pvpboxIP)
		{
			if (ich < 0)
				ich = 0;
			// We must have arrived at the min of the paragraph.
			bool fAssocPrev = !IsBeginningOfLine(ich, pvpbox, pvg) &&
				IsEditable(ich, pvpbox, true);
			VwStringBox * psbox = GetStringBox(ich, pvpbox, fAssocPrev);
			if (!psbox)
			{
				state = kSkipSpace;	// ?????????
				continue;
			}
			ILgSegmentPtr qlseg = psbox->Segment();
			int ichBoxMin = psbox->IchMin();
			int ichSegLim;
			CheckHr(qlseg->get_Lim(0, &ichSegLim));
			Assert(ich >= ichBoxMin || ich <= ichBoxMin + ichSegLim);
			LgIpValidResult ipvr;
			CheckHr(qlseg->IsValidInsertionPoint(ich, pvg, ichBoxMin + ichSegLim, &ipvr));
			if (ipvr != kipvrOK)
			{
				state = kSkipSpace;	// ?????????
				continue;
			}
			*ppvpbox = pvpbox;
			m_qttp = NULL;
			return 0;
		}
	}
	// We must have started at the min of the first paragraph.
	*ppvpbox = pvpboxIP;
	m_qttp = NULL;
	return ichLogIP;
}

/*----------------------------------------------------------------------------------------------
	Get the first valid insertion point on the same line in this paragraph as ichLogIP.
	Return -1 if either the string box containing the given insertion point cannot be found, or
	0 if no valid insertion points can be found on the line.
	Note that all input/output character indexes are logical.

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
int VwTextSelection::BeginningOfLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
	IVwGraphics * pvg)
{
	AssertPtr(pvg);
	AssertPtr(pvpboxIP);

	VwBox * pbox;
	VwStringBox * psbox;
	bool fBaselineSet = false;
	int nBaselineIP = 0;
	int nBaseline;
	int ich = -1;
	int ichNew = -1;
	ILgSegmentPtr qlseg;
	LgIpValidResult ipvr;
	int ichSegLim;
	for (pbox = GetStringBox(ichLogIP, pvpboxIP, fAssocPrev);
		pbox;
		pbox = pbox->NextInReverseRootSeq())
	{
		if (pbox->IsStringBox())
		{
			if (!fBaselineSet)
			{
				fBaselineSet = true;
				nBaselineIP = pbox->Top() + pbox->Ascent();
			}
			nBaseline = pbox->Top() + pbox->Ascent();
			if (nBaseline == nBaselineIP)
			{
				psbox = dynamic_cast<VwStringBox *>(pbox);
				int ichBoxMin = psbox->IchMin();
				qlseg = psbox->Segment();
				CheckHr(qlseg->get_Lim(0, &ichSegLim));
				for (ich = 0; ich <= ichSegLim; ++ich)
				{
					CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichBoxMin+ich,
						&ipvr));
					if (ipvr == kipvrOK)
					{
						ichNew = psbox->IchMin() + ich;
						break;
					}
				}
			}
			else
			{
				return pvpboxIP->Source()->RenToLog(ichNew);
			}
		}
		else if (pbox->IsParagraphBox())
		{
			return pvpboxIP->Source()->RenToLog(ichNew);
		}
	}
	return pvpboxIP->Source()->RenToLog(ichNew);
}

/*----------------------------------------------------------------------------------------------
	Return the height of half a line.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
int VwTextSelection::AdjustEndLocation(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	IVwGraphicsPtr qvg = pvg;
	RECT rdIP;
	RECT rdIPSec;
	ComBool fSplitIP;
	pvpboxEnd->LocOfSelection(qvg, m_ichEnd, m_ichEnd, m_fAssocPrevious, rcSrcRoot, rcDstRoot,
		&rdIP, &rdIPSec, &fSplitIP, true);

	// Move down a little from the top center of the IP to be sure we're inside the start
	// (typically string) box. This is a position at screen resolution relative to the whole
	// document. It will be invalidated by expanding lazy boxes.
	int yd = (rdIP.top + rdIP.bottom) / 2;
	if (yd > rdIP.bottom)
		yd = rdIP.bottom;

	return yd - rdIP.top;
}

/*----------------------------------------------------------------------------------------------
	Get the valid insertion point closest to one line visually above the given one, if it
	exists.
	Note that all input/output character indexes are logical.

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Not a real transformation...origins both zero.
	@param rcDstRoot
	@param ppvpbox
	@param pichLogHome
----------------------------------------------------------------------------------------------*/
int VwTextSelection::UpOneLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
	IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, VwParagraphBox ** ppvpbox,
	int * pichLogHome)
{
	AssertPtr(pvpboxIP);
	AssertPtr(pvg);
	AssertPtr(ppvpbox);
	AssertPtr(pichLogHome);

	IVwGraphicsPtr qvg = pvg;
	VwRootBox * prootb = pvpboxIP->Root();
	Assert(prootb);
	RECT rdIP;
	RECT rdIPSec;
	ComBool fSplitIP;
	pvpboxIP->LocOfSelection(qvg, ichLogIP, ichLogIP, fAssocPrev, rcSrcRoot, rcDstRoot,
		&rdIP, &rdIPSec, &fSplitIP, true);
	if (m_xdIP == -1)
		m_xdIP = (rdIP.left + rdIP.right) / 2;
	// Move down a little from the top center of the IP to be sure we're inside the start
	// (typically string) box. This is a position at screen resolution relative to the whole
	// document. It will be invalidated by expanding lazy boxes.
	int yd = (rdIP.top + rdIP.bottom) / 2;
	if (yd > rdIP.bottom)
		yd = rdIP.bottom;
	Rect rcSrc;
	Rect rcDst;
	// Find the (typically string) box that contains the IP, within the original paragraph.
	// This should find a real box, since the paragraph doesn't contain lazy boxes.
	VwBox * pboxOrig = prootb->FindBoxClicked(qvg, m_xdIP, yd, rcSrcRoot, rcDstRoot,
		&rcSrc, &rcDst);
	if (!pboxOrig)
	{
		// If we don't know where we're starting, how can we go anywhere?
		// This should never happen, there's always something inside a paragraph.
		*ppvpbox = pvpboxIP;
		int ichHome = BeginningOfLine(ichLogIP, pvpboxIP, fAssocPrev, qvg);
		*pichLogHome = ichHome;
		return ichLogIP;
	}
	// This computes the baseline of the lowest-level box clicked in, relative to
	// the whole document in layout coordinates. Note that expanding lazy boxes
	// will render this value meaningless unless corrected.
	int yBaselineOrig = CalculateBaseline(pboxOrig);
	VwBox * pbox;
	int yBaselineNew = yBaselineOrig;		// Definitely not < yBaselineOrig.
	do
	{
		yd -= 3;
		if (yd < rcDst.top)
		{
			pbox = NULL;
			break;
		}
		pbox = prootb->FindBoxClicked(qvg, m_xdIP, yd, rcSrcRoot, rcDstRoot, &rcSrc, &rcDst);

		// Make sure we find a box and that it is not lazy
		while (pbox && pbox->IsLazyBox())
		{
			// Before we expand lazy boxes, we need to do something about yd
			// so that if the box we were looking at changes its position, they are still
			// relative to it.
			int ys = rcDstRoot.MapYTo(yd, rcSrcRoot);
			int dys = ys - yBaselineOrig; // This is constant whatever expand does.
			// Need to expand the lazy box...specifically expand its last item.
			VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pbox);
			int ihvoLim = plzbox->CItems();
			plzbox->ExpandItems(ihvoLim - 1, ihvoLim);

			// Baseline of original start box can be recomputed at new position.
			yBaselineOrig = CalculateBaseline(pboxOrig);
			// yd can then be recovered
			ys = dys + yBaselineOrig;
			yd = rcSrcRoot.MapYTo(ys, rcDstRoot);
			// See what we clicked on now we expanded it. This might still be lazy,
			// which is why this is a loop!
			pbox = prootb->FindBoxClicked(qvg, m_xdIP, yd, rcSrcRoot, rcDstRoot, &rcSrc,
				&rcDst);
		}
		if (pbox)
		{
			// Check to make sure we made it into the box.  This keeps the GetSelection code
			// from thinking we are at the bottom of the box and need to make a selection at
			// the end of the text (TE-2275)
			int ydBottom = rcSrc.MapYTo(pbox->Bottom(), rcDst);
			if (ydBottom + 3 < yd)
			{
				yd = ydBottom;
				continue;
			}
			// We found a non-lazy box to click on. Figure its absolute baseline to make sure it
			// is really above the one we started from.
			yBaselineNew = CalculateBaseline(pbox);
		}
		// Keep going until we find a box that is different from the one we started with and
		// that the box is not a string box and is above the box we started from.
	} while (!pbox || pbox == pboxOrig || yBaselineNew >= yBaselineOrig || !pbox->IsStringBox());

	if (pbox && yBaselineNew < yBaselineOrig)
	{
		VwSelectionPtr qvwsel;
		pbox->GetSelection(qvg, prootb, m_xdIP, yd, rcSrcRoot, rcDstRoot, rcSrc, rcDst,
			&qvwsel);
		AssertPtr(qvwsel.Ptr());
		VwTextSelection * pvwsel = dynamic_cast<VwTextSelection *>(qvwsel.Ptr());
		// ENHANCE JohnT: deal with other types when we have them.
		if (pvwsel)
		{
			int ichNew = pvwsel->m_ichEnd;
			int ichHome = pvwsel->BeginningOfLine(pvwsel->m_ichEnd, pvwsel->m_pvpbox,
				pvwsel->m_fAssocPrevious, qvg);
			VwParagraphBox * pvpboxNew = pvwsel->m_pvpbox;
			// Check how close the insertion point is to where we want it.
			RECT rdPrimary;
			RECT rdSecondary;
			ComBool fSplit;
			pvpboxNew->LocOfSelection(qvg, ichNew, ichNew, ichNew != ichHome,
				rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit, true);
			int xdIPNew = (rdPrimary.left + rdPrimary.right) / 2;
			if (abs(m_xdIP - xdIPNew) > 20)
			{
				// Move up some more to see if we can get a better insertion point.
				VwBox * pbox2 = NULL;
				int yd2;
				int yBaselineNew2 = yBaselineNew - 1;	// Definitely not == yBaselineNew.
				int ydTop2 = yd - 30;
				if (ydTop2 < 0)
					ydTop2 = 0;
				for (yd2 = yd - 3; yd2 >= ydTop2; yd2 -= 3)
				{
					pbox2 = prootb->FindBoxClicked(qvg, m_xdIP, yd2, rcSrcRoot, rcDstRoot,
						&rcSrc, &rcDst);
					if (pbox2 && pbox2 != pbox)
					{
						if (pbox2->Container() != pbox->Container() ||
							pbox2->Baseline() != pbox->Baseline())
						{
							pbox2 = NULL;
						}
						else
						{
							yBaselineNew2 = CalculateBaseline(pbox2);
						}
						break;
					}
				}
				if (pbox2 && pbox2 != pbox && yBaselineNew2 == yBaselineNew)
				{
					pbox2->GetSelection(qvg, prootb, m_xdIP, yd2, rcSrcRoot, rcDstRoot,
						rcSrc, rcDst, &qvwsel);
					AssertPtr(qvwsel.Ptr());
					VwTextSelection * pvwsel2 = dynamic_cast<VwTextSelection *>(qvwsel.Ptr());
					if (pvwsel2)
					{
						int ichNew2 = pvwsel2->m_ichEnd;
						int ichHome2 = pvwsel2->BeginningOfLine(pvwsel2->m_ichEnd,
							pvwsel2->m_pvpbox, false, qvg);
						VwParagraphBox * pvpboxNew2 = pvwsel2->m_pvpbox;
						if (pvpboxNew2 == pvpboxNew)
						{
							// Check how close the insertion point is to where we want it.
							pvpboxNew2->LocOfSelection(qvg, ichNew2, ichNew2,
								ichNew2 != ichHome2, rcSrcRoot, rcDstRoot,
								&rdPrimary, &rdSecondary, &fSplit, true);
							int xdIPNew2 = (rdPrimary.left + rdPrimary.right) / 2;
							if (abs(m_xdIP - xdIPNew2) < abs(m_xdIP - xdIPNew))
							{
								// Do we need to check for being a valid insertion point?
								*ppvpbox = pvpboxNew;
								*pichLogHome = ichHome2;
								return ichNew2;
							}
						}
					}
				}
			}
			// Do we need to check for being a valid insertion point?
			*ppvpbox = pvpboxNew;
			*pichLogHome = ichHome;
			return ichNew;
		}
		else
		{
			// ENHANCE JohnT: deal with other types one day?
		}
	}

	// Can't find anywhere to go!?
	*ppvpbox = pvpboxIP;
	*pichLogHome = BeginningOfLine(ichLogIP, pvpboxIP, fAssocPrev, qvg);
	return ichLogIP;
}

/*----------------------------------------------------------------------------------------------
	Get the first insertion point in the entire view.
	This returns a logical character position.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param ppvpbox
----------------------------------------------------------------------------------------------*/
int VwTextSelection::BeginningOfView(IVwGraphics * pvg, VwParagraphBox ** ppvpbox)
{
	AssertPtr(pvg);
	AssertPtr(ppvpbox);

	VwParagraphBox * pvpboxNew = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	VwBox * pbox;
	VwStringBox * psbox;
	int ichSegLim;
	int ich = -1;
	ILgSegmentPtr qlseg;
	LgIpValidResult ipvr;
	for (pbox = m_pvpbox->Root(); pbox; pbox = pbox->NextInRootSeq())
	{
		if (pbox->IsParagraphBox())
		{
			pvpboxNew = dynamic_cast<VwParagraphBox *>(pbox);
		}
		else if (pbox->IsStringBox())
		{
			// Find the first valid insertion point in this string box (if any exist).
			psbox = dynamic_cast<VwStringBox *>(pbox);
			int ichBoxMin = psbox->IchMin();
			qlseg = psbox->Segment();
			CheckHr(qlseg->get_Lim(0, &ichSegLim));
			for (ich = 0; ich <= ichSegLim; ++ich)
			{
				CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichBoxMin+ich, &ipvr));
				if (ipvr == kipvrOK)
				{
					*ppvpbox = pvpboxNew;
					return pvpboxNew->Source()->RenToLog(psbox->IchMin() + ich);
				}
			}
		}
	}
	// We should never reach here, but just in case, and to keep the compiler happy:
	*ppvpbox = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	return (*ppvpbox)->Source()->RenToLog(m_ichEnd);
}

/*----------------------------------------------------------------------------------------------
	Get the next valid insertion point on this line just following the one given.
	Return -1 if no valid following insertion points can be found.
	Note that all input/output character indexes are logical.

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
int VwTextSelection::ForwardOneCharInLine(int ichLogIP, VwParagraphBox * pvpboxIP,
	bool fAssocPrev, IVwGraphics * pvg)
{
	AssertPtr(pvg);
	AssertPtr(pvpboxIP);

	VwBox * pbox;
	VwStringBox * psbox;
	ILgSegmentPtr qlseg;
	LgIpValidResult ipvr;
	bool fBaselineSet = false;
	int nBaselineIP = 0;
	int nBaseline;
	int ichBoxMin;
	int ichBoxLim;
	int ichSegLim;
	int ichNew = ichLogIP + 1;
	int ich = -1;
	VwBox * pStartSearch = NULL;
	// Find the string box that contains the given insertion point, and move forward from
	// there.
	for (pbox = GetStringBox(ichLogIP, pvpboxIP, fAssocPrev);
		pbox;
		pbox = pbox->NextBoxForSelection(&pStartSearch))
	{
		if (pbox->IsStringBox())
		{
			if (!fBaselineSet)
			{
				fBaselineSet = true;
				nBaselineIP = pbox->Top() + pbox->Ascent();
			}
			nBaseline = pbox->Top() + pbox->Ascent();
			if (nBaseline == nBaselineIP)
			{
				psbox = dynamic_cast<VwStringBox *>(pbox);
				ichBoxMin = psbox->IchMin();
				qlseg = psbox->Segment();
				CheckHr(qlseg->get_Lim(0, &ichSegLim));
				ichBoxLim = ichBoxMin + ichSegLim;
				if (ichNew < ichBoxMin)
					ichNew = ichBoxMin;
				if (ichNew <= ichBoxLim)
				{
					for (ich = ichNew - ichBoxMin; ich <= ichSegLim; ++ich)
					{
						CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichBoxMin+ich,
							&ipvr));
						if (ipvr == kipvrOK)
							return ichBoxMin + ich;
					}
				}
			}
			else
			{
				break;
			}
		}
		else if (pbox->IsParagraphBox())
		{
			break;
		}
	}
	// We're already at the last valid insertion point on this line.
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Get the previous valid insertion point on this line just prior to the one given.
	Return -1 if no valid prior insertion points can be found.
	Note that all input/output character indexes are logical.

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
----------------------------------------------------------------------------------------------*/
int VwTextSelection::BackOneCharInLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
	IVwGraphics * pvg)
{
	int ichIP = pvpboxIP->Source()->LogToRen(ichLogIP);
	AssertPtr(pvg);
	AssertPtr(pvpboxIP);

	VwBox * pbox;
	VwStringBox * psbox;
	ILgSegmentPtr qlseg;
	LgIpValidResult ipvr;
	bool fBaselineSet = false;
	int nBaselineIP = 0;
	int nBaseline;
	int ichBoxMin;
	int ichBoxLim;
	int ichSegLim;
	int ichNew = ichIP - 1;
	int ich = -1;
	// Find the string box that contains the given insertion point, and move backward from
	// there.
	for (pbox = GetStringBox(ichLogIP, pvpboxIP, false);
		pbox;
		pbox = pbox->NextInReverseRootSeq())
	{
		if (pbox->IsStringBox())
		{
			if (!fBaselineSet)
			{
				fBaselineSet = true;
				nBaselineIP = pbox->Top() + pbox->Ascent();
			}
			nBaseline = pbox->Top() + pbox->Ascent();
			if (nBaseline == nBaselineIP)
			{
				psbox = dynamic_cast<VwStringBox *>(pbox);
				ichBoxMin = psbox->IchMin();
				qlseg = psbox->Segment();
				CheckHr(qlseg->get_Lim(0, &ichSegLim));
				ichBoxLim = ichBoxMin + ichSegLim;
				if (ichBoxMin <= ichNew && ichNew <= ichBoxLim)
				{
					for (ich = ichNew - ichBoxMin; ich >= 0; --ich)
					{
						CheckHr(qlseg->IsValidInsertionPoint(ichBoxMin, pvg, ichBoxMin+ich,
							&ipvr));
						if (ipvr == kipvrOK)
							return pvpboxIP->Source()->RenToLog(ichBoxMin + ich);
					}
					ichNew = ichBoxMin;		// This should be the lim of the preceding box.
				}
			}
			else
			{
				break;
			}
		}
		else if (pbox->IsParagraphBox())
		{
			break;
		}
	}
	// We're already at the first valid insertion point on this line.
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Get the X position(s) of the current insertion point.
	Return 0 if an error occurs.
	Note that the input character index is logical.

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot Dummy transformation as used for Up/DownArrow.
	@param rcDstRoot
	@param pxdSec
----------------------------------------------------------------------------------------------*/
int VwTextSelection::PositionOfIP(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
	IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdSec)
{
	AssertPtr(pvpboxIP);
	AssertPtr(pvg);
	AssertPtr(pxdSec);

	RECT rectPrimary;
	RECT rectSecondary;
	ComBool fPrimaryHere = 0;
	ComBool fSecHere = 0;
	ILgSegmentPtr qlseg;
	VwStringBox * psbox;

	psbox = GetStringBox(ichLogIP, pvpboxIP, fAssocPrev);
	if (!psbox)
	{
		*pxdSec = 0;
		return 0;
	}
	qlseg = psbox->Segment();
	CheckHr(qlseg->PositionsOfIP(psbox->IchMin(), pvg, rcSrcRoot, rcDstRoot,
		pvpboxIP->Source()->LogToRen(ichLogIP), fAssocPrev,
		kdmNormal, &rectPrimary, &rectSecondary, &fPrimaryHere, &fSecHere));
	int xdPrime = 0;
	int xdSec = 0;
	int xdBase = 0;
	VwBox * pbox;
	for (pbox = psbox; pbox; pbox = pbox->Container())
	{
		xdBase += pbox->Left();
	}
	if (fPrimaryHere)
		xdPrime = xdBase + (rectPrimary.left + rectPrimary.right) / 2;
	if (fSecHere)
		xdSec = xdBase + (rectSecondary.left + rectSecondary.right) / 2;
	*pxdSec = xdSec;
	return xdPrime;
}

/*----------------------------------------------------------------------------------------------
	Commit if editing and moving outside the edit range. Return false if commit fails;
	otherwise return true.
	Note that the input character index is logical.

	@param ichLogIP
	@param pvpboxIP
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::CheckCommit(int ichLogIP, VwParagraphBox * pvpboxIP)
{
	AssertPtr(pvpboxIP);
	VwParagraphBox * pvpboxEnd = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	// REVIEW JohnT(TomB): Should the range checking here take into consideration
	// m_fAssocPrevious when the new logical insertion point is right on the min/lim
	// boundary?
	if (m_qtsbProp &&
		(pvpboxIP != pvpboxEnd || ichLogIP < m_ichMinEditProp || ichLogIP > m_ichLimEditProp))
	{
		bool fOK;
		CommitAndContinue(&fOK);
		if (!fOK)
			return false;
		m_qtsbProp.Clear(); // force a new start editing, as we are in a different para.
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the VwBox pointer to the VwStringBox that contains the given insertion point.
	Return NULL if the string box cannot be found.  (This can happen for an empty line /
	paragraph.)
	If the problem is only fAssocPrevious, return the box on the other side of the IP.
	Note that the input character index is logical.

	@param ichLogIP
	@param pvpboxIP
	@param fAssocPrev - Flag whether the insertion point associates with the preceding character
					in the paragraph.
----------------------------------------------------------------------------------------------*/
VwStringBox * VwTextSelection::GetStringBox(int ichLogIP, VwParagraphBox * pvpboxIP,
	bool fAssocPrev)
{
	int ichIP = pvpboxIP->Source()->LogToRen(ichLogIP);
	VwBox * pbox;
	VwStringBox * psbox = NULL;
	int ichMin = -1;
	int ichLim = -1;
	ILgSegmentPtr qlseg;
	int ichSegLim;
	for (pbox = pvpboxIP->FirstBox(); pbox; pbox = pbox->Next())
	{
		if (pbox->IsStringBox())
		{
			psbox = dynamic_cast<VwStringBox *>(pbox);
			ichMin = psbox->IchMin();
			// If we are in a stringbox that is before the IP, or if it contains the IP,
			// we get the segment, and if it is the right one we return this string box.
			if (ichMin <= ichIP)
			{
				qlseg = psbox->Segment();
				CheckHr(qlseg->get_Lim(0, &ichSegLim));
				ichLim = ichMin + ichSegLim;
				// If the IP is before the end of this segment, it is unconditionally the one.
				if (ichIP < ichLim)
					return psbox;
				else if (ichIP == ichLim)
				{
					// If it is right at the end of the segment, and associated with the previous
					// character, that is the character at the end of this string box.
					if (fAssocPrev)
						return psbox;
					// JT: not sure why we do this, AFAIK, in every case where this is true
					// either there is no next string box or its ichMin is greater than the end of
					// this box, so we would end up returning this one anyway. Saved it from
					// some earlier code, in case it is important for a reason I've forgotten.
					// Typically this occurs with empty paragraphs (no next box) or hard line
					// breaks (next box starts 2 characters later) or between two non-string boxes
					// (next box is not a string).
					if (ichSegLim == 0)
						return psbox;
					// Otherwise it turns on the next box, not counting moveable piles
					VwBox * pboxNext = pbox->NextOrLazy();
					while (pboxNext && pboxNext->IsMoveablePile())
						pboxNext = pboxNext->NextOrLazy();
					// If that box is a string box that starts exactly where this one left
					// off, then since fAssocPrev is true we should return it.
					if (pboxNext && pboxNext->IsStringBox())
					{
						VwStringBox * psboxNext = dynamic_cast<VwStringBox *>(pboxNext);
						if (psboxNext->IchMin() == ichIP)
							return psboxNext;
					}
					// If there is no following string box with a starting position equal to ichIP,
					// return the one the IP is at the end of, despite fAssocPrev being false.
					return psbox;
				}
			}
			else
			{
				// We missed the box with the IP position. No use to search further.
				break;
			}
		}
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Note that the input/output character index is logical.

	@param ichLog
	@param pvpbox
	@param itssMin
	@param itssLim
	@param vpst
----------------------------------------------------------------------------------------------*/
void VwTextSelection::AdjustForRep(int & ichLog, VwParagraphBox * pvpbox, int itssMin,
	int itssLim, VpsTssVec & vpst)
{
	int ichMin = pvpbox->Source()->IchStartString(itssMin);
	if (ichLog <= ichMin)
		return;
	// These variables accumulate the length of corresponding strings in the old and
	// new sequences.
	int cchOldTotal = 0;
	int cchNewTotal = 0;
	int itssLimLoop = max(itssLim, itssMin + vpst.Size());
	for (int itss = itssMin; itss < itssLimLoop; itss++)
	{
		int cchOld = 0; // default if no more old strings
		if (itss < itssLim)
		{
			ITsStringPtr qtss;
			pvpbox->Source()->StringAtIndex(itss, &qtss);
			if (qtss)
				CheckHr(qtss->get_Length(&cchOld));
			else
				cchOld = 1; // embedded box counts 1
		}
		int cchNew = 0;
		if (itss - itssMin < vpst.Size())
		{
			ITsString * ptss = vpst[itss - itssMin].qtms;
			if (ptss)
				CheckHr(ptss->get_Length(&cchNew));
			else
				cchNew = 1; // embedded box counts 1
		}
		if (ichLog < ichMin + cchOldTotal + cchOld)
		{
			// It used to be in the current string. Adjust it by the change in length
			// of previous strings.
			ichLog += cchNewTotal - cchOldTotal;
			// And, if it is now too big to fit in the length of the new string, fix
			int ichMax = ichMin + cchNewTotal + cchNew;
			if (ichLog > ichMax)
				ichLog = ichMax;
			return;
		}
		cchOldTotal += cchOld;
		cchNewTotal += cchNew;
	}
	// If we get out here, we have processed all the old and new strings, and the old
	// position was beyond any of them. Adjust by the total change in length.
	ichLog += cchNewTotal - cchOldTotal;
	// But if it's now out of range, fix it.  See FWR-2479.
	int ichLim = ichMin + cchNewTotal;
	if (ichLog > ichLim)
		ichLog = ichLim;
}

/*----------------------------------------------------------------------------------------------
	The specified paragraph box is being edited, specifically, the strings which are at itssMin
	to itssLim in the paragraph's text source are to be replaced with new strings from vpst.

	The basic idea is to keep an end-point in the same string as it was, as nearly as possible
	at the same position. We can do this precisely if it is in a string that is not being
	replaced. If it is in a string that is being replaced, we make sure it stays in the
	corresponding new string.

	@param pvpbox
	@param itssMin
	@param itssLim
	@param vpst
----------------------------------------------------------------------------------------------*/
void VwTextSelection::FixTextEdit(VwParagraphBox * pvpbox, int itssMin,
	int itssLim, VpsTssVec & vpst)
{
	AssertPtr(pvpbox);
	// First a quick check to save time if it isn't our box.
	if (pvpbox != m_pvpbox && pvpbox != m_pvpboxEnd)
		return;

	// Adjust the end point if the change is in the end-point paragraph, or there
	// is no end-point paragraph and the change is in the main paragraph.
	if (pvpbox == m_pvpboxEnd || (pvpbox == m_pvpbox && !m_pvpboxEnd))
		AdjustForRep(m_ichEnd, pvpbox, itssMin, itssLim, vpst);
	if (pvpbox == m_pvpbox)
	{
		AdjustForRep(m_ichAnchor, pvpbox, itssMin, itssLim, vpst);
	}
}

/*----------------------------------------------------------------------------------------------
	A double-click has occurred, and the second click would naturally produce the selection
	pselNew (if it had been a single). If *this is an IP, and pselNew is close to it, make a
	word selection. Otherwise, replace yourself with pselNew.

	@param pselNew
	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::ExpandToWord(VwTextSelection * pselNew)
{
	// If this somehow gets called on an invalid selection, just quietly ignore it.
	if (!m_qrootb)
		return S_OK;
	if (m_ichAnchor2 != -1 && pselNew->m_pvpbox == m_pvpbox &&
		pselNew->m_ichEnd == pselNew->m_ichAnchor &&
		((m_ichAnchor <= pselNew->m_ichEnd && pselNew->m_ichEnd <= m_ichAnchor2) ||
		 (m_ichAnchor2 <= pselNew->m_ichEnd && pselNew->m_ichEnd <= m_ichAnchor)))
	{
		// We have a (shift-)double-click inside the old word selection.
		Hide();
		if (m_ichAnchor < m_ichAnchor2)
		{
			m_ichEnd = m_ichAnchor2;
		}
		else
		{
			m_ichEnd = m_ichAnchor;
			m_ichAnchor = m_ichAnchor2;
			m_ichAnchor2 = m_ichEnd;
		}
		m_qttp = NULL;
		Show();
		// Make sure we tell the rootbox that we changed
		m_pvpbox->Root()->NotifySelChange(ksctSamePara);
		return true;
	}
	if (m_pvpboxEnd || m_ichAnchor != m_ichEnd || pselNew->m_pvpbox != m_pvpbox ||
		abs(pselNew->m_ichAnchor - m_ichAnchor) > 2)
	{
		m_pvpbox->Root()->SetSelection(pselNew);
		return false;
	}
	int ichMinWord;
	int ichLimWord;
	FindWordBoundaries(ichMinWord, ichLimWord);
	if ((m_ichAnchor2 == -1) ||
		((ichMinWord != m_ichAnchor || ichLimWord != m_ichEnd) &&
		 (ichMinWord != m_ichEnd || ichLimWord != m_ichAnchor)))
	{
		Hide();
		m_ichAnchor = ichMinWord;
		m_ichEnd = ichLimWord;
		m_ichAnchor2 = ichLimWord;			// Mark the end of the word as an anchor also.
		m_qttp = NULL;
		Show();
		// Make sure we tell the rootbox that we changed
		m_pvpbox->Root()->NotifySelChange(ksctSamePara);
	}
	return true;
}

// Answer true if the specified text prop objects have a different value (or variant) for the specified integer property.
bool PropsDiffer(ITsTextProps * pttp1, ITsTextProps * pttp2, int ttp)
{
	// If either props is null (e.g., no style, or undefined style), treat the property as having its default value.
	int val1 = 0;
	int val2 = 0;
	int var1 = -1;
	int var2 = -1;
	if (pttp1)
	{
		CheckHr(pttp1->GetIntPropValues(ttp, &var1, &val1));
		if (var1 == -1)
			val1 = 0;  // treat undefined as default.
	}
	if (pttp2)
	{
		CheckHr(pttp2->GetIntPropValues(ttp, &var2, &val2));
		if (var2 == -1)
			val2 = 0;  // treat undefined as default.
	}
	if (val1 != val2)
		return true;
	// different variants is significant only if we have two real TsTextProps and both specify the property.
	if (var1 != -1 && var2 != -1 && var1 != var2)
		return true;
	return false;
}

// Answer true if the text props differ in a way which should prevent the associated characters from
// being considered part of the same word.
bool PropsIndicateWordBreak(ITsTextProps * pttp1, ITsTextProps * pttp2,IVwStylesheet * psty)
{
	if (pttp1 == pttp2)
		return false; // most common case, no change.
	// A writing system difference is considered only if both ttps exist. Typically the only time one of
	// them doesn't is when one run of characters has a style and the other doesn't. The ttps from stylesheets
	// don't have writing systems.
	if (pttp1 && pttp2 && PropsDiffer(pttp1, pttp2, ktptWs))
		return true;
	if (PropsDiffer(pttp1, pttp2, ktptSpellCheck))
		return true;
	if (!psty)
		return false; // no stylesheet, style can't cause differences.
	SmartBstr sbstrStyle1, sbstrStyle2;
	CheckHr(pttp1->GetStrPropValue(ktptNamedStyle, &sbstrStyle1));
	CheckHr(pttp2->GetStrPropValue(ktptNamedStyle, &sbstrStyle2));
	if (sbstrStyle1.Length() == 0 && sbstrStyle2.Length() == 0)
		return false; // neither has a style, no diff resulting from style.
	if (sbstrStyle1.Length() > 0 && sbstrStyle2.Length() > 0 && wcscmp(sbstrStyle1.Chars(), sbstrStyle2.Chars()) == 0)
		return false; // same style, can't result in any difference.
	// Different styles are NOT automatically different. Look up what they mean and see if there's a significant difference.
	ITsTextPropsPtr qttp1, qttp2;
	if (sbstrStyle1.Length())
		CheckHr(psty->GetStyleRgch(sbstrStyle1.Length(), sbstrStyle1.Bstr(), &qttp1));
	if (sbstrStyle2.Length())
		CheckHr(psty->GetStyleRgch(sbstrStyle2.Length(), sbstrStyle2.Bstr(), &qttp2));
	return PropsIndicateWordBreak(qttp1, qttp2, NULL);
	// What on earth should we do if one style means something and the other doesn't??
	// Simplest just to treat as no significant difference, I think.
	return false;
}

void VwTextSelection::FindWordBoundaries(int & ichMinWord, int & ichLimWord)
{
	VwTxtSrc * psrc = m_pvpbox->Source();
	// Determine the range of characters we will consider: the text property that contains the anchor.
	int ichMin;
	int ichLim;
	ITsStringPtr qtssProp;
	VwPropertyStorePtr qzvps;
	int itss;
	// REVIEW JohnT (TomB & TimS): We're hard-coding "false" here for the fAssocPrev parameter
	// to get the same behavior as before (when we didn't have this param), but we're not sure
	// that's right. Perhaps we should pass m_fAssocPrev (at least if this is an insertion point).
	psrc->StringFromIch(m_ichAnchor, false, &qtssProp, &ichMin, &ichLim, &qzvps, &itss);

	if (m_qtsbProp)
	{
		// editing--don't select a wider range than the property
		ichMin = m_ichMinEditProp;
		ichLim = m_ichLimEditProp;
	}

	// NOTE: This depends only on the Unicode general category, not on the language.
	ITsTextPropsPtr qttpStart;
	ITsTextPropsPtr qttpCurrent;
	OLECHAR ch;
	ichLimWord = m_ichAnchor;

	// Get the properties at the start of the selection so we can know if they change
	int ichProps = (ichLimWord == ichLim) ? ichLimWord - 1 : ichLimWord;
	if (ichProps >= 0)
		psrc->CharAndPropsAt(ichProps, &ch, &qttpStart);
	IVwStylesheet * psty = m_qrootb->Stylesheet();

	// Advance ichLimWord to the limit of available characters, or to one that is not
	// word-forming or a digit or a property change is found
	while (ichLimWord < ichLim)
	{
		psrc->CharAndPropsAt(ichLimWord, &ch, &qttpCurrent);
		if (PropsIndicateWordBreak(qttpCurrent, qttpStart, psty))
			break;
		ILgCharacterPropertyEnginePtr qcpe;
		GetCpeFromRootAndProps(m_qrootb, qttpCurrent, &qcpe);
		ComBool isDigit;
		CheckHr(qcpe->get_IsNumber(ch, &isDigit));
		ComBool isWordForming;
		CheckHr(qcpe->get_IsWordForming(ch, &isWordForming));
		if (!isWordForming && !isDigit)
			break;
		ichLimWord++;
	}

	// Similarly decrease ichMinWord
	ichMinWord = m_ichAnchor;
	while (ichMinWord > ichMin)
	{
		psrc->CharAndPropsAt(ichMinWord - 1, &ch, &qttpCurrent);
		if (PropsIndicateWordBreak(qttpCurrent, qttpStart, psty))
			break;
		ILgCharacterPropertyEnginePtr qcpe;
		GetCpeFromRootAndProps(m_qrootb, qttpCurrent, &qcpe);
		ComBool isDigit;
		qcpe->get_IsNumber(ch, &isDigit);
		ComBool isWordForming;
		qcpe->get_IsWordForming(ch, &isWordForming);
		if (!isWordForming && !isDigit)
			break;
		ichMinWord--;
	}
}

/*----------------------------------------------------------------------------------------------
	Get the needed state variables for retrieving level info for a given endpoint of a
	selection.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::GetEndInfo(bool fEndPoint, VwParagraphBox ** ppvpbox, int & ichTarget,
		bool & fAssocPrevious)
{
	Assert(IsValid()); // if this selection isn't valid, then it's very likely that m_pvpbox isn't valid either

	*ppvpbox = m_pvpbox;
	ichTarget = m_ichAnchor;
	fAssocPrevious = m_fAssocPrevious;
	if (m_ichEnd != m_ichAnchor || m_pvpboxEnd)
		fAssocPrevious = m_fEndBeforeAnchor;

	if (fEndPoint)
	{
		if (m_pvpboxEnd)
			*ppvpbox = m_pvpboxEnd;
		ichTarget = m_ichEnd;
		// If it is a range toggle fAssocPrevious for the other end.
		if (m_ichAnchor != m_ichEnd || m_pvpboxEnd)
			fAssocPrevious = !fAssocPrevious;
	}
}

/*----------------------------------------------------------------------------------------------
	Return the selection state of the rootbox.
	If the selection has been made invalid treat it as disabled.
----------------------------------------------------------------------------------------------*/
VwSelectionState VwTextSelection::SelectionState()
{
	if (!m_qrootb)
		return vssDisabled;
	return m_qrootb->SelectionState();
}


/*----------------------------------------------------------------------------------------------
	Check whether the proposed insertion point lies within editable text.

	@param ichIP Proposed insertion point for a new selection.
	@param pvpBoxIP Points to the paragraph of the proposed insertion point.

	@return True if the proposed IP is editable on either of its sides.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::IsEditable(int ichIP, VwParagraphBox * pvpboxIP)
{
	return (IsEditable(ichIP, pvpboxIP, true) || IsEditable(ichIP, pvpboxIP, false));
}


/*----------------------------------------------------------------------------------------------
	Check whether the proposed insertion point lies within editable text.

	@param ichIP Proposed insertion point for a new selection.
	@param pvpBoxIP Points to the paragraph of the proposed insertion point.
	@param fAssocPrevIP - Flag whether the proposed insertion point associates with the
					preceding character in the paragraph.

	@return True if the proposed selection is editable, otherwise false.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::IsEditable(int ichIP, VwParagraphBox * pvpboxIP, bool fAssocPrevIP)
{
	// REVIEW JohnT (TimS): This version of IsEditable seems to ignore fAssocPrevIP. This
	// essentially makes the fAssocPrevIP flag useless. There is an overload of IsEditable
	// that doesn't take this parameter and indeed calls this method with both true
	// and false (See above method). Is this behaviour what we really wanted for this method?

	HVO hvoEdit;
	int tagEdit;
	int ichMinEditProp;
	int ichLimEditProp;
	IVwViewConstructorPtr qvvcEdit;
	int fragEdit;
	VwAbstractNotifierPtr qanote;
	int iprop;
	VwNoteProps vnp;
	int itssProp;
	ITsStringPtr qtssProp;
	// This uses notifier information to determine the editable property following ichIP.
	VwEditPropVal vepv = CallEditableSubstring(pvpboxIP, ichIP, ichIP, fAssocPrevIP,
		&hvoEdit, &tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
		&qanote, &iprop, &vnp, &itssProp, &qtssProp);
	return vepv == kvepvEditable;
}

// Answer true if rcMoved is on the same line as rcDefault, the natural (but not editable)
// place for the IP. This means requiring that the Moved rectangle overlaps the default rect.
bool OnSameLine(Rect rcDefault, Rect rcMoved)
{
	return ((rcMoved.top <= rcDefault.top && rcDefault.top < rcMoved.bottom) ||
			(rcDefault.top <= rcMoved.top && rcMoved.top < rcDefault.bottom));
}

// Answer true if rcMoved is 'close enough' to being on the same line as rcDefault,
// the natural (but not editable) place for the IP. Currently this means requiring
// them to have overlap or be within about 40 pixels.
bool CloseEnough(Rect rcDefault, Rect rcMoved)
{
	return OnSameLine(rcDefault, rcMoved) ||
		(rcMoved.top >= rcDefault.top - 40 && rcMoved.bottom <= rcDefault.bottom + 40);
}


/*----------------------------------------------------------------------------------------------
	Try to find an editable insertion point on the same line or one very close, first looking
	before, and then if necessary, looking after.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot

	@return True if the the selection is adjusted to an editable location, otherwise false.
----------------------------------------------------------------------------------------------*/
bool VwTextSelection::FindClosestEditableIP(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	Assert(m_ichEnd == m_ichAnchor);
	Assert(!m_pvpboxEnd);

	// Look for the closest preceding editable insertion point.
	bool fBackEditable = true;
	int ichBack = m_ichEnd;
	VwParagraphBox * pvpboxBack = m_pvpbox;
	int ich = -1;
	VwParagraphBox * pvpboxNew;
	bool fAssocPrev = !IsBeginningOfLine(ichBack, pvpboxBack, pvg);
	// This is the bounds of the paragraph the IP would be in by default, if we weren't
	// trying to make it editable.
	Rect rcDefault = m_pvpbox->GetOuterBoundsRect(pvg, rcSrcRoot, rcDstRoot);
	// There's a bug in BackOneChar() that causes an infinite loop cycling between
	// paragraph boxes.  I'm putting in a simple cycle check that solves the problem
	// in the one place I've seen it without too much additional overhead.  But we
	// really should find and fix the real bug.
	int cLoop = 0;
	Set<VwParagraphBox *> setpvpFound;
	DWORD start = GetTickCount();
	while (!IsEditable(ichBack, pvpboxBack))
	{
		ich = BackOneChar(ichBack, pvpboxBack, fAssocPrev, pvg, &pvpboxNew);
		if (pvpboxNew != pvpboxBack)
		{
			// Apparently in this read-only context BackOneChar sometimes goes one char too far,
			// if we switch paragraphs. Correct this.
			ich = pvpboxNew->Source()->Cch();
			// Also, if we're in a different paragraph, it's time to limit the range of the
			// search. For one thing, we don't want to scroll an arbitrarily large distance
			// when the user clicks. For another, in a wholly read-only document, we don't
			// want to defeat laziness and expand everything looking for something editable.
			if (!CloseEnough(rcDefault, pvpboxNew->GetOuterBoundsRect(pvg, rcSrcRoot, rcDstRoot)))
			{
				// We moved too far. Can't find an editable position backwards.
				fBackEditable = false;
				break;
			}
			if (cLoop > 100)
			{
				if (setpvpFound.IsMember(pvpboxNew))
				{
					// We're stuck in a cycle!??
					fBackEditable = false;
					break;
				}
				setpvpFound.Insert(pvpboxNew);
				setpvpFound.Insert(pvpboxBack);
			}
		}
		else if (ich == ichBack && pvpboxBack == pvpboxNew)
		{
			// We stopped moving...presumably reached the
			// start of the document. Can't find an editable position backwards.
			fBackEditable = false;
			break;
		}
		if (GetTickCount() - start > 50)
		{
			// It's just too unresponsive to spend more than 1/10 second searching for this;
			// allow half this for the back search.
			fBackEditable = false;
			break;
		}
		ichBack = ich;
		pvpboxBack = pvpboxNew;
		fAssocPrev = !IsBeginningOfLine(ichBack, pvpboxBack, pvg);
		++cLoop;
	}
	Rect rcBack;
	if (fBackEditable)
	{
		// Check for being in the same horizontal row. If so, we go there
		// without checking forwards.
		rcBack = pvpboxBack->GetOuterBoundsRect(pvg, rcSrcRoot, rcDstRoot);
		if (OnSameLine(rcDefault, rcBack))
		{
			m_ichEnd = ichBack;
			m_ichAnchor = ichBack;
			m_pvpbox = pvpboxBack;
			m_rcBounds.Clear();
			m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
				IsEditable(m_ichEnd, m_pvpbox, true);
			m_fEndBeforeAnchor = false;
			return true;
		}
	}
	// Look for the closest following editable insertion point.
	bool fForwardEditable = true;
	int ichForward = m_ichEnd;
	VwParagraphBox * pvpboxForward = m_pvpbox;
	fAssocPrev = !IsBeginningOfLine(ichForward, pvpboxForward, pvg);
	while (!IsEditable(ichForward, pvpboxForward))
	{
		ich = ForwardOneChar(ichForward, pvpboxForward, fAssocPrev, pvg, &pvpboxNew);
		if ((pvpboxNew != pvpboxForward &&
			!CloseEnough(rcDefault, pvpboxNew->GetOuterBoundsRect(pvg, rcSrcRoot, rcDstRoot)))
			|| (ich == ichForward && pvpboxForward == pvpboxNew)
			|| GetTickCount() - start > 100)
		{
			// End of doc, or moved too far, or spent too long searching.
			fForwardEditable = false;
			break;
		}
		ichForward = ich;
		pvpboxForward = pvpboxNew;
		fAssocPrev = !IsBeginningOfLine(ichForward, pvpboxForward, pvg);
	}
	Rect rcForward;
	if (fForwardEditable)
	{
		// Check for being in the same horizontal row. If so, we go there.
		rcForward = pvpboxForward->GetOuterBoundsRect(pvg, rcSrcRoot, rcDstRoot);
		if (OnSameLine(rcDefault, rcForward))
		{
			m_ichEnd = ichForward;
			m_ichAnchor = ichForward;
			m_pvpbox = pvpboxForward;
			m_rcBounds.Clear();
			m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
				IsEditable(m_ichEnd, m_pvpbox, true);
			m_fEndBeforeAnchor = false;
			return true;
		}
	}
	// We couldn't find something on the same row. If we found something only backwards
	// or only forwards, go with it. If both are possible go forwards.
	if (fForwardEditable)
	{
		// If we get this far forwards is preferable if it is possible at all.
		m_ichEnd = ichForward;
		m_ichAnchor = ichForward;
		m_pvpbox = pvpboxForward;
		m_rcBounds.Clear();
		m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
				IsEditable(m_ichEnd, m_pvpbox, true);
		m_fEndBeforeAnchor = false;
		return true;
	}
	if (fBackEditable)
	{
		// Backwards is possible (and, if both are possible, preferable).
		m_ichEnd = ichBack;
		m_ichAnchor = ichBack;
		m_pvpbox = pvpboxBack;
		m_rcBounds.Clear();
		m_fAssocPrevious = !IsBeginningOfLine(m_ichEnd, m_pvpbox, pvg) &&
			IsEditable(m_ichEnd, m_pvpbox, true);
		m_fEndBeforeAnchor = false;
		return true;
	}
	// No reasonably close position is editable.
	return false;
}

/*----------------------------------------------------------------------------------------------
	Change the end point of this selection to be the (anchor of) the argument selection.
	Both must be currently useable selections on the same root.  Note psel may be an IP
	bordering on this selection, thus causing this selection to remain the same, or psel may be
	inside this selection, causing this selection to shrink rather than expand.

	@param psel - pointer to another text selection
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ExtendEndTo(VwTextSelection * psel)
{
	Assert(psel->m_pvpbox->Root() == m_pvpbox->Root());

	m_ichEnd = psel->m_ichAnchor;

	m_rcBounds.Clear();
	if (psel->m_pvpbox == m_pvpbox)
	{
		m_fEndBeforeAnchor = m_ichEnd < m_ichAnchor;
		m_pvpboxEnd = NULL;
	}
	else
	{
		m_pvpboxEnd = psel->m_pvpbox;
		// Start out assuming m_fEndBeforeAnchor is true.  First get containing boxes, if
		// necessary, such that pboxAnchor and pboxEnd each is or contains m_pvpboxAnchor and
		// m_pvpboxEnd, and furthermore, they have the same container.  Then look for pboxEnd
		// starting at pboxAnchor.  If we find it, then m_fEndBeforeAnchor is false.
		VwBox * pboxAnchor = m_pvpbox;
		VwBox * pboxEnd = m_pvpboxEnd;
		if (m_pvpbox->Container() != m_pvpboxEnd->Container())
		{
			VwGroupBox * pgboxCommonContainer = VwGroupBox::CommonContainer(m_pvpbox,
				m_pvpboxEnd);
			pgboxCommonContainer->Contains(m_pvpbox, &pboxAnchor);
			pgboxCommonContainer->Contains(m_pvpboxEnd, &pboxEnd);
			Assert(pboxAnchor->Container() == pboxEnd->Container());
		}
		m_fEndBeforeAnchor = true;
		for (VwBox * pbox = pboxAnchor; pbox; pbox = pbox->NextOrLazy())
		{
			if (pbox == pboxEnd)
			{
				// We found the end box after the anchor one
				m_fEndBeforeAnchor = false;
				break;
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	If this is a range selection, change the anchor of this selection to be at its endpoint.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::ContractToEnd()
{
	if (IsInsertionPoint())
		return;

	if (m_pvpboxEnd && m_pvpboxEnd != m_pvpbox)
	{
		m_pvpbox = m_pvpboxEnd;
		m_pvpboxEnd = NULL;
		m_rcBounds.Clear();
	}
	m_ichAnchor = m_ichEnd;
	m_fEndBeforeAnchor = false;
}

/*----------------------------------------------------------------------------------------------
	Install *this as the active selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::Install()
{
	BEGIN_COM_METHOD;
	VwRootBox * prootb = m_pvpbox->Root();
	prootb->SetSelection(this);
	prootb->ShowSelection();
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Answer true if this selection follows (comes after in the view) the argument.
	False if they are exactly the same position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::get_Follows(IVwSelection * psel, ComBool * pfFollows)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(psel);
	ChkComOutPtr(pfFollows);
	VwTextSelectionPtr qsel;
	// ENHANCE JohnT: should we do something nicer if it isn't our implementation?
	CheckHr(psel->QueryInterface(CLSID_VwTextSelection, (void **)(&qsel)));
	VwParagraphBox * pvpboxEndOther;
	int ichEndOther;
	qsel->GetLimit(true, &pvpboxEndOther, &ichEndOther);
	VwParagraphBox * pvpboxStartThis;
	int ichStartThis;
	GetLimit(false, &pvpboxStartThis, &ichStartThis);
	if (pvpboxEndOther == pvpboxStartThis)
	{
		*pfFollows = ichEndOther < ichStartThis;
		return S_OK;
	}
	// Otherwise, we follow the other box if our start box comes after its in root sequence.
	// We don't have to expand lazy boxes because the other selection's paragraph must already
	// be expanded.
	VwBox * pbox = pvpboxEndOther;
	for (; pbox; pbox = pbox->NextInRootSeq(false))
	{
		if (pbox == pvpboxStartThis)
		{
			*pfFollows = true;
			return S_OK;
		}
	}
	// Otherwise leave *pfFollows false, from ChkComOutPtr.

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Get one of the boundaries of the selection as a box, offset pair. If fEnd is true, it is
	the end of the selection, otherwise the start. Note that this is the start and end, not
	anchor and end.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::GetLimit(bool fEnd, VwParagraphBox ** ppvpbox, int * pich)
{
	*pich = m_ichAnchor;
	*ppvpbox = m_pvpbox;
	if (m_pvpboxEnd)
	{
		if (fEnd && !m_fEndBeforeAnchor || !fEnd && m_fEndBeforeAnchor)
		{
			*pich = m_ichEnd;
			*ppvpbox = m_pvpboxEnd;
		}
	}
	else if (fEnd && m_ichEnd > m_ichAnchor || !fEnd && m_ichAnchor > m_ichEnd)
		*pich = m_ichEnd;
}

/*----------------------------------------------------------------------------------------------
	Start a task for undoing, with the label indicated by the string resource ID.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::BeginUndoTask(ISilDataAccess * psda, int stid)
{
	StrUni stuUndo, stuRedo;
	StrUtil::MakeUndoRedoLabels(stid, &stuUndo, &stuRedo);
	CheckHr(psda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
}

/*----------------------------------------------------------------------------------------------
	Get the character offset of the anchor (if fEndPoint is false) or the end point
	(if fEndPoint is true) in whatever paragraph each occurs.
	Note that this is relative to the paragraph as a whole, not the particular
	string property.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::get_ParagraphOffset(ComBool fEndPoint, int * pich)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pich);
	if (fEndPoint)
		*pich = m_ichAnchor;
	else
		*pich = m_ichEnd;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Like GetSelectionProps, except that the return property stores contain the formatting
	for the selection MINUS the hard-formatting that is in the TsTextProps.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::GetHardAndSoftCharProps(int cttpMax, ITsTextProps ** prgpttpSel,
	IVwPropertyStore ** prgpvpsSoft, int * pcttp)
{
	ChkComArrayArg(prgpttpSel, cttpMax);
	ChkComArrayArg(prgpvpsSoft, cttpMax);
	ChkComArgPtr(pcttp);

	TtpVec vqttpSel;
	VwPropsVec vqvpsSel;
	CheckHr(GetSelectionProps(0, NULL, NULL, pcttp));
	if (!*pcttp)
		// No text selected.
		return S_FALSE;
	if (cttpMax == 0)
		return S_OK;

	int cttp = *pcttp;
	vqttpSel.Resize(cttp);
	vqvpsSel.Resize(cttp);
	CheckHr(GetSelectionProps(cttp, (ITsTextProps **)vqttpSel.Begin(),
		(IVwPropertyStore **)vqvpsSel.Begin(), pcttp));

	// Generate the properties that correspond to the view constructor and the styles
	// without any hard-formatting.
	ComVector<IVwPropertyStore> vqvpsParent;
	vqvpsParent.Resize(cttp);
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		prgpttpSel[ittp] = vqttpSel[ittp];
		CheckHr(vqvpsSel[ittp]->get_ParentStore(&vqvpsParent[ittp]));
	}

	VwTxtSrc * pts = m_pvpbox->Source();

	if (m_ichAnchor == m_ichEnd && !m_pvpboxEnd)
	{
		// Insertion point.
		Assert(cttp == 1);

		ITsStringPtr qtss;
		int ichMinTss;
		int ichLimTss;
		VwPropertyStorePtr qzvps;
		int itss;
		pts->StringFromIch(m_ichAnchor, m_fAssocPrevious, &qtss, &ichMinTss, &ichLimTss, &qzvps, &itss);

		ITsTextPropsPtr qttp;
		TsRunInfo tri;
		CheckHr(qtss->FetchRunInfoAt(m_ichAnchor - ichMinTss, &tri, &qttp));
		GetHardAndSoftPropsOneRun(qttp, vqvpsParent[0], &prgpvpsSoft[0]);

		return S_OK;
	}

	// Not an insertion point.

	// Loop over all boxes involved
	VwBox * pboxCurr = m_pvpbox;
	VwParagraphBox * pvpboxLast = m_pvpbox;
	VwParagraphBox * pvpboxCurr;
	int ichStart = m_ichAnchor;
	int ichEnd;
	if (m_pvpboxEnd)
	{
		if (m_fEndBeforeAnchor)
			pboxCurr = m_pvpboxEnd; // and leave Last pointing at end
		else
			pvpboxLast = m_pvpboxEnd; // and leave Curr pointing at start
	}
	if (m_fEndBeforeAnchor)
	{
		ichStart = m_ichEnd;
	}

	cttp = 0;
	for (; pboxCurr; pboxCurr = pboxCurr->NextRealBox())
	{
		pvpboxCurr = dynamic_cast<VwParagraphBox *>(pboxCurr);
		if (!pvpboxCurr)
			continue;
		pts = pvpboxCurr->Source();
		if (pvpboxCurr == pvpboxLast)
			ichEnd = m_fEndBeforeAnchor ? m_ichAnchor : m_ichEnd;
		else
			ichEnd = pvpboxCurr->Source()->Cch();
		ITsStringPtr qtss;
		int ichMinTss;
		int ichLimTss;
		VwPropertyStorePtr qzvps;
		int itss;
		pts->StringFromIch(ichStart, false, &qtss, &ichMinTss, &ichLimTss,
			&qzvps, &itss);

		if (pts->Cch()== 0 && pboxCurr != pvpboxLast)
		{
			// The following loop skips the one string in each empty paragraph, so put
			// in this special case to deal with it. But to get the expected behavior,
			// we must not include the last box in the sequence if it is empty.
			Assert(qtss); // If there was an object the para would not be empty.

			ITsTextPropsPtr qttp;
			TsRunInfo tri;
			CheckHr(qtss->FetchRunInfoAt(0, &tri, &qttp));
			GetHardAndSoftPropsOneRun(qttp, vqvpsParent[cttp], &prgpvpsSoft[cttp]);
			cttp++;
		}

		for (int ich = ichStart; ich < ichEnd;)
		{
			// ich is relative to the paragraph, as is ichMinTss
			int ichNew;
			if (qtss)
			{
				ITsTextPropsPtr qttp;
				TsRunInfo tri;
				CheckHr(qtss->FetchRunInfoAt(ich - ichMinTss, &tri, &qttp));
				GetHardAndSoftPropsOneRun(qttp, vqvpsParent[cttp], &prgpvpsSoft[cttp]);
				cttp++;
				ichNew = ichMinTss + tri.ichLim;
			}
			else
			{
				// Otherwise, there aren't any ttps associated with this char position,
				// ignore it.  Just advance by the one character which is associated with
				// a non-tss.
				ichNew = ichMinTss + 1;
			}
			if (ichNew >= ichLimTss && ichEnd > ichLimTss)
			{
				// We need the next string
				Assert(ichNew == ichLimTss);  // we should have used this string up
				pts->StringAtIndex(++itss, &qtss);
				ichMinTss = ichLimTss;
				int cch;
				if (qtss)
					CheckHr(qtss->get_Length(&cch));
				else
					cch = 1;
				ichLimTss += cch;
			}
			else
			{
				// Only if we didn't move to the next string: there might be some
				// empty strings in there...but processing a run should have
				// caused us to make some progress, otherwise.
				Assert(ichNew > ich);
			}
			ich = ichNew;

			Assert(cttp <= *pcttp);
		}
		// More boxes?
		if (pboxCurr == pvpboxLast)
			break;
		ichStart = 0; // always start at beginning of subsequent boxes
	}

	Assert(cttp == *pcttp);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	An auxiliary method for GetStylePropsFromParaProps that generates the new property
	store.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::GetHardAndSoftPropsOneRun(ITsTextProps * pttp,
	IVwPropertyStore * pvpsParent, IVwPropertyStore ** ppvpsRet)
{
	// First apply any character properties for this writing system that are already
	// in the property store.
	ITsTextPropsPtr qttpTmp;
	int ws, nVar;
	CheckHr(pttp->GetIntPropValues(ktptWs, &nVar, &ws));
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetIntPropValues(ktptWs, nVar, ws));
	CheckHr(qtpb->GetTextProps(&qttpTmp));
	IVwPropertyStorePtr qvpsTmp;
	CheckHr(pvpsParent->get_DerivedPropertiesForTtp(qttpTmp, &qvpsTmp));

	// Then apply the named style.
	SmartBstr sbstrNamedStyle;
	HRESULT hr;
	CheckHr(hr = pttp->GetStrPropValue(ktptNamedStyle, &sbstrNamedStyle));
	if (hr == S_OK)
	{
		// (Seems okay to just slap the character style on top of the ws/ows in the
		// same builder.)
		ITsTextPropsPtr qttpCharStyle;
		CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, sbstrNamedStyle));
		CheckHr(qtpb->GetTextProps(&qttpCharStyle));

		CheckHr(qvpsTmp->get_DerivedPropertiesForTtp(qttpCharStyle, ppvpsRet));
	}
	else {
		if (hr == S_FALSE) {
			*ppvpsRet = qvpsTmp.Detach();
		}
	}
}

/*----------------------------------------------------------------------------------------------
	An auxiliary method for GetStylePropsFromParaProps that generates the new property
	store.

	@param prgpttpPara - contains the paragraph style name and para hard-formatting for
			each paragraph
	@param prgpttpHard - returns the hard formatting
	@param prgpvpsSoft - returns the view constructor and style formatting
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::GetHardAndSoftParaProps(int cttpMax, ITsTextProps ** prgpttpPara,
	ITsTextProps ** prgpttpHard, IVwPropertyStore ** prgpvpsSoft, int * pcttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgpttpPara, cttpMax);
	ChkComArrayArg(prgpttpHard, cttpMax);
	ChkComArrayArg(prgpvpsSoft, cttpMax);
	ChkComArgPtr(pcttp);

	VwPropsVec vqvpsSel;
	CheckHr(GetParaProps(0, NULL, pcttp));
	if (!*pcttp)
		// No text selected.
		return S_FALSE;
	if (cttpMax == 0)
		return S_OK;

	int cttp = *pcttp;
	vqvpsSel.Resize(cttp);
	CheckHr(GetParaProps(cttp, (IVwPropertyStore **)vqvpsSel.Begin(), pcttp));

	// Generate the properties that correspond to the view constructor and the styles
	// without any hard-formatting.
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		// Make a copy of the paragraph properties minus the style name, which leaves
		// just the hard-formatting.
		ITsPropsBldrPtr qtpbHard;
		if (prgpttpPara[ittp])
			CheckHr(prgpttpPara[ittp]->GetBldr(&qtpbHard));
		else
			qtpbHard.CreateInstance(CLSID_TsPropsBldr);
		SmartBstr sbstrEmpty;
		CheckHr(qtpbHard->SetStrPropValue(ktptNamedStyle, sbstrEmpty));
		CheckHr(qtpbHard->GetTextProps(prgpttpHard + ittp));

		byte rgb[1000];  // TODO: delete
		int cb;
		prgpttpHard[ittp]->SerializeRgb(rgb, 1000, &cb);

		// Apply the named style to the parent, which gives the soft formatting.
		ITsPropsBldrPtr qtpbStyle;
		qtpbStyle.CreateInstance(CLSID_TsPropsBldr);
		if (prgpttpPara[ittp])
		{
			SmartBstr sbstrStyleName;
			CheckHr(prgpttpPara[ittp]->GetStrPropValue(ktptNamedStyle, &sbstrStyleName));
			CheckHr(qtpbStyle->SetStrPropValue(ktptNamedStyle, sbstrStyleName));
			ITsTextPropsPtr qttpStyle;
			CheckHr(qtpbStyle->GetTextProps(&qttpStyle));

			IVwPropertyStorePtr qvpsParent;
			CheckHr(vqvpsSel[ittp]->get_ParentStore(&qvpsParent));
			CheckHr(qvpsParent->get_DerivedPropertiesForTtp(qttpStyle, prgpvpsSoft + ittp));
		}
		else
			CheckHr(vqvpsSel[ittp]->get_ParentStore(prgpvpsSoft + ittp));
	}

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Note any boxes that should not be deleted while this selection exists.
	Usually this is just the single paragraph box, more rarely the sequence from the
	first to the last. Very rarely the first and last don't have the same container,
	and we have to do something more complex.
----------------------------------------------------------------------------------------------*/
void VwTextSelection::AddToKeepList(LazinessIncreaser *pli)
{
	if (!m_qrootb)
		return;
	if (!m_pvpboxEnd)
	{
		pli->KeepSequence(m_pvpbox, m_pvpbox->NextOrLazy());
		return;
	}
	// Get the first and last box (in the proper order).
	VwParagraphBox * pvpboxFirst = m_pvpbox;
	VwParagraphBox * pvpboxLast = m_pvpboxEnd;
	if (m_fEndBeforeAnchor)
	{
		pvpboxFirst = m_pvpboxEnd;
		pvpboxLast = m_pvpbox;
	}
	// Get containing boxes, if necessary, such that pboxFirst and pboxLast each is
	// or contains pvpboxFirst and pvpboxLast, and furthermore, they have the
	// same container.
	VwGroupBox * pgboxCommonContainer = VwGroupBox::CommonContainer(pvpboxFirst, pvpboxLast);
	VwBox * pboxFirst;
	pgboxCommonContainer->Contains(pvpboxFirst, &pboxFirst);
	VwBox * pboxLast;
	pgboxCommonContainer->Contains(pvpboxLast, &pboxLast);
	Assert(pboxFirst->Container() == pboxLast->Container());

	// Make those boxes keepers.
	// Enhance JohnT: note that it is possible that there are boxes in pboxFirst before
	// pvpboxFirst, which it would be OK to offline; but I don't think it is worth the
	// greatly increased code complexity to try to take advantage of this.
	pli->KeepSequence(pboxFirst, pboxLast->NextOrLazy());
}

//:>********************************************************************************************
//:>	VwPictureSelection methods
//:>
//:>	REVIEW DavidO: Make sure the picture selection class works for multiple picture
//:>	selections.
//:>
//:>********************************************************************************************

VwPictureSelection::VwPictureSelection()
{
	m_plbox = NULL;
	m_ichAnchorLog = 0;
	m_ichEndLog = 0;
	m_fAssocPrevious = false;
	m_xdIP = -1;
}

/*----------------------------------------------------------------------------------------------
	Create a new picture selection. The picture offsets are relative to the particular
	picture box.

	@param ppicbox
	@param ipicAnchor
	@param ipicEnd
	@param fAssocPrevious - Flag whether the selection associates with the preceding picture
					in the paragraph.
----------------------------------------------------------------------------------------------*/
VwPictureSelection::VwPictureSelection(VwLeafBox * ppicbox, int ipicAnchor, int ipicEnd,
										bool fAssocPrevious)
{
	m_plbox = ppicbox;
	m_ichAnchorLog = ipicAnchor;
	m_ichEndLog = ipicEnd;
	m_fEndBeforeAnchor = m_ichEndLog < m_ichAnchorLog;
	m_fAssocPrevious = fAssocPrevious;
	m_xdIP = -1;
	m_qrootb = m_plbox->Root();
	m_qrootb->RegisterSelection(this);
}

/*----------------------------------------------------------------------------------------------
	Override to allow CLSID_VwPictureSelection trick so we can find out if an interface is our
	own implementation.

	@param riid - reference to the desired interface ID.
	@param ppv - address that receives the interface pointer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwSelection)
		*ppv = static_cast<IVwSelection *>(this);
	else if (&riid == &CLSID_VwPictureSelection)	// ERRORJOHN
		*ppv = static_cast<VwPictureSelection *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwSelection);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}
/*----------------------------------------------------------------------------------------------
	Indicates the maximum level number that may be passed to PropInfo (*pclev - 1)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::CLevels(ComBool fEndPoint, int * pclev)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pclev);

	VwNotifier * pnote = GetNotifier();
	if (pnote)
		*pclev = pnote->Level();

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	If the picture is embedded in a paragraph box, return the corresponding character position
	in rendered characters, relative to entire paragraph.
----------------------------------------------------------------------------------------------*/
int VwPictureSelection::GetCharPosition()
{
	return m_plbox->GetCharPosition();
}

/*----------------------------------------------------------------------------------------------
	Return the most specific notifier that contains the picture box. This is the highest-level
	notifier of all those that relate to this box, unless its container is a paragraph. In
	that case we have to do something more complex.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwPictureSelection::GetNotifier(int * piprop)
{
	NotifierVec vpanote;
	m_plbox->Container()->GetNotifiers(m_plbox, vpanote);

	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_plbox->Container());

	if (pvpbox)
	{
		// If the container is a paragraph, typically the picture box is either the expansion
		// of a character in one of the strings of the paragraph, or was independently inserted
		// into the paragraph. In either case, it is considered as occupying one character
		// position in the paragraph, and the notifier points at the paragraph box, with a
		// string index indicating which string (or embedded non-string box) it is.
		VwNotifier * pnoteBest = NULL;
		int tagBest = 0;
		int ipropBest = 0; // index where we found picture in the best notifier.
		for (int i = 0; i < vpanote.Size(); i++)
		{
			HVO hvo;
			int tag;
			int ichMinProp;
			int ichLimProp;
			IVwViewConstructorPtr qvvc;
			int frag;
			int iprop; // index of property in newly tried notifier that has substring
			VwNoteProps vnp;
			int itssProp;
			ITsStringPtr qtssProp;
			VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[i].Ptr());
			if (!pnote)
				continue;  // For this purpose only real notifiers are interesting.
			VwEditPropVal vepv = pnote->EditableSubstringAt(pvpbox, m_ichAnchorLog, m_ichEndLog, true,
				&hvo, &tag, &ichMinProp, &ichLimProp, &qvvc, &frag, &iprop, &vnp,
				&itssProp, &qtssProp);
			// If the notifier doesn't cover this property at all skip it.
			if (vepv == kvepvNone)
				continue;
			// Since the picture is a non-empty range, any successful match is probably what we want...
			// unless we get one that is at a deeper level and also covers it.
			if (pnoteBest)
			{
				// Keep pnoteBest if it is at a higher level, or the same level and is a real property.
				if (pnoteBest->Level() > pnote->Level())
					continue;
				if (pnoteBest->Level() == pnote->Level() && tagBest != ktagNotAnAttr && tagBest != ktagGapInAttrs)
					continue;
			}
			ipropBest = iprop;
			pnoteBest = pnote;
			tagBest = tag;
		}
		if (pnoteBest)
		{
			if (piprop)
				*piprop = ipropBest;
			return pnoteBest;
		}
		// If we didn't find a match, it's still possible that the whole containing paragraph is part of
		// some higher-level property, which will therefore be the innermost.
	}

	VwNotifier * pnoteResult = NULL;
	int clevMax = -1;
	for (int inote = 0; inote < vpanote.Size(); inote++)
	{
		VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[inote].Ptr());
		if (pnote && pnote->Level() > clevMax)
		{
			pnoteResult = pnote;
			clevMax = pnote->Level();
		}
	}
	if (piprop)
		*piprop = pnoteResult->PropInfoFromBox(m_plbox);
	return pnoteResult;
}


/*----------------------------------------------------------------------------------------------
	Returns info about nth level. Level 0 returns the hvoObj and tag of the property that
	contains the display of the picture.
	Level 1 is the object/prop that contains hvoObj[0].
	Level 2 is the object/prop that contains hvoObj[1]. And so forth.
	The ihvo returned for level n is the zero-based index of hvoObj[n-1] in prop
	tag[n] of hvoObj[n]. It is always 0 for index 0
	The pcpropPrevious argument is sometimes useful in the rare cases where,
	within a display of a certain object, the same property is displayed more
	than once. For example, within the display of a book, we might display the
	sections once using a view that shows titles to produce a table of contents,
	then again to produce the main body of the book. cpropPrevious indicates
	how many previous occurrences of property tag there are in the display of hvoObj,
	before the one which contains the current picture.

	@param fEndPoint - true for end point, false for anchor. Ignored here for picture.
	@param ilev
	@param phvoObj
	@param ptag
	@param pihvo
	@param pcpropPrevious
	@param ppvps
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::PropInfo(ComBool fEndPoint, int ilev, HVO * phvoObj,
	PropTag * ptag, int * pihvo, int * pcpropPrevious, IVwPropertyStore ** ppvps)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvoObj);
	ChkComOutPtr(ptag);
	ChkComOutPtr(pihvo);
	ChkComOutPtr(pcpropPrevious);
	ChkComOutPtr(ppvps);

	int iprop;
	VwAbstractNotifier * panote = GetNotifier(&iprop);
	int clev = 0; // count length of notifier chain.
	VwNotifier * pnoteInner = 0;
	while (panote && clev < ilev)
	{
		clev++;
		pnoteInner = dynamic_cast<VwNotifier *>(panote);
		Assert(pnoteInner); // should never get another type for a picture.
		panote = panote->Parent();
	}
	if (!panote)
		ThrowHr(WarnHr(E_INVALIDARG)); // don't have this much depth
	*phvoObj = panote->Object();
	VwNotifier * pnote = dynamic_cast<VwNotifier *>(panote);
	Assert(pnote);
	if (pnoteInner)
	{
		// We are not the innermost notifier
		*pihvo = pnoteInner->ObjectIndex();
		VwBox * pboxFirstProp;
		int itssFirstProp;
		// The value we really want here is *ptag.
		pnote->GetPropForSubNotifier(pnoteInner, &pboxFirstProp, &itssFirstProp, ptag,
			&iprop);
		*pcpropPrevious = pnote->PropCount(*ptag, pboxFirstProp, itssFirstProp);
	}
	else
	{
		// we are the innermost notifier, give the info about the picture prop.
		// Note that *pihvo is deliberately left 0, as set by the argument check macro.
		PropTag tag = pnote->Tags()[iprop];
		*ptag = tag;

		// Count previous occurrences of this tag; usually none.
		*pcpropPrevious = pnote->PropCount(tag, pnote->Boxes()[iprop]);
	}
	if (ppvps)
	{
		*ppvps = pnote->Styles()[iprop];
		AddRefObj(*ppvps);
	}

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwPictureSelection::get_CanFormatPara(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);
	*pfRet = false;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwPictureSelection::get_CanFormatChar(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);
	*pfRet = false;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Focus loss: return false to indicate that focus loss is vetoed. The caller should then
	SetFocus back to this window. If this call is happening because a new selection is being
	made in the containing root box, pass the new selection in pvwselNew; otherwise pass null.

	@param pvwselNew
	@param pfOk
----------------------------------------------------------------------------------------------*/
void VwPictureSelection::LoseFocus(IVwSelection * pvwselNew, ComBool * pfOk)
{
	AssertPtr(pfOk);
	AssertPtrN(pvwselNew);

	*pfOk = true;
}

/*----------------------------------------------------------------------------------------------
	Draw selection. Coordinate transformation is for the root box.
	ENHANCE DavidO: Figure out what's desired when a picture is selected -- like
	inverting -- and implement it.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param fOn
	@param rcSrcRoot
	@param rcDstRoot
----------------------------------------------------------------------------------------------*/
void VwPictureSelection::Draw(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot,
							  int ysTop, int dysHeight, bool fDisplayPartialLines)
{
	// Enhance DavidO: We'll probably need to invert the picture (or something similar) when
	// this is fully implemented... by someone else.
	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;

	CheckHr(Location(pvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit,
		&fEndBeforeAnchor));

	pvg->InvertRect(rdPrimary.left, rdPrimary.top, rdPrimary.right, rdPrimary.bottom);
}

/*----------------------------------------------------------------------------------------------
	Return true if anchor is at the bottom/right (or left if RtoL) of the selection

	@param pfRet - pointer to return value through.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::get_EndBeforeAnchor(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);
	*pfRet = m_fEndBeforeAnchor;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Get the character offset of the anchor (if fEndPoint is false) or the end point
	(if fEndPoint is true) in the underlying paragraph where this picture is anchored.
	Note that this is relative to the paragraph as a whole, not the particular
	string property. Note also that some picture boxes are not anchored in any paragraph,
	in which case this returns -1.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::get_ParagraphOffset(ComBool fEndPoint, int * pich)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pich);
	if (fEndPoint)
		*pich = m_ichEndLog;
	else
		*pich = m_ichAnchorLog;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Given the same first three arguments as used to draw the root, indicate where the
	selection is drawn. prdPrimary will be set to a rectangle in destination coords
	the bounds the selection as closely as possible; if there is a split cursor,
	prdSecondary gives the place where the secondary is drawn, and pfSplit is true.

	Picture selections actually aren't drawn at all, but the location information is sometimes
	used for things like restoring scroll positions, so it's useful to return something.
	If a picture selection ever is drawn, we'll probably invert the whole picture box, so
	let's pretend we do that and return the picture box's rectangle.

	@param pvg - pointer to the IVwGraphics object for actually drawing or measuring things.
	@param rcSrcRoot
	@param rcDstRoot
	@param prdPrimary
	@param prdSecondary
	@param pfSplit
	@param pfEndBeforeAnchor
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::Location(IVwGraphics * pvg, RECT rcSrcRoot, RECT rcDstRoot,
	RECT * prdPrimary, RECT * prdSecondary, ComBool * pfSplit, ComBool * pfEndBeforeAnchor)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(prdPrimary);
	ChkComArgPtr(prdSecondary);
	ChkComOutPtr(pfSplit);
	ChkComOutPtr(pfEndBeforeAnchor);

	// Get the offset for the picture. (usually the picture is centered and the location is
	// returned left justified if this isn't done)
	int xOffset = 0;
	VwPictureBox * ppbpic = dynamic_cast<VwPictureBox *>(m_plbox);
	if(ppbpic)
		xOffset = ppbpic->AdjustedLeft();


	Rect rcSrc, rcDst;
	m_plbox->CoordTransFromRoot(pvg, rcSrcRoot, rcDstRoot, &rcSrc, &rcDst);
	prdPrimary->top = rcSrc.MapYTo(m_plbox->Top() + m_plbox->GapTop(rcSrc.Height()), rcDst);
	prdPrimary->left = rcSrc.MapXTo(m_plbox->Left() + xOffset +
			m_plbox->GapLeft(rcSrc.Width()),
		rcDst);
	prdPrimary->bottom = rcSrc.MapYTo(m_plbox->Top() +
			m_plbox->GapTop(rcSrc.Height()) + m_plbox->Height(),
		rcDst);
	prdPrimary->right = rcSrc.MapXTo(m_plbox->Left() + xOffset +
			m_plbox->GapLeft(rcSrc.Width()) + m_plbox->Width(),
		rcDst);
	*pfSplit = false; // and hence can ignore prdSecondary
	*pfEndBeforeAnchor = false; // meaningless here, but return something.
	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	This method exists to deal with trying to get text out of a picture box, which is
	meaningless, but is needed when the user right-clicks on one of our picture boxes.
	@param fEndPoint - true for end point, false for anchor
	@param pptss - string containing selection
	@param pich - pointer to the offset into string
	@param pfAssocPrev - flag whether the selection is considered to be before or after the
					indicated end point
	@param phvoObj - object to which the string belongs (or 0 if none)
	@param ptag - tag of property to which the string belongs
	@param pws - identifies which alternative, if prop is an alternation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::TextSelInfo(ComBool fEndPoint, ITsString ** pptss, int * pich,
	ComBool * pfAssocPrev, HVO * phvoObj, PropTag * ptag, int * pws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	ChkComOutPtr(pich);
	ChkComOutPtr(pfAssocPrev);
	ChkComOutPtr(phvoObj);
	ChkComOutPtr(ptag);
	ChkComOutPtr(pws);

	// Set values to zero:
	*pptss = NULL;
	*pich = 0;
	*pfAssocPrev = 0;
	*phvoObj = 0;
	*ptag = 0;
	*pws = 0;

	return S_FALSE;

	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	Return the current selection as a TsString.
	This method exists to deal with trying to get text out of a picture box, which is
	meaningless, but is needed when the user left-clicks on one of our picture boxes.
	@param pptss
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::GetSelectionString(ITsString ** pptss, BSTR bstrNonText)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);

	// We can't return anything, so why try?  Even an empty TsString is impossible due to our
	// inability to even get a writing system id.
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_IVwSelection);
}


/*----------------------------------------------------------------------------------------------
	Return the current selection as a TsString.
	This returns an empty string if requested from a picture selection.
	@param pptss
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::GetFirstParaString(ITsString ** pptss, BSTR bstrNonText,
	ComBool * pfGotItAll)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	ChkComOutPtr(pfGotItAll);

	// We can't return anything, so why try?  Even an empty TsString is impossible due to our
	// inability to even get a writing system id.
	*pfGotItAll = TRUE;
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Note any boxes that should not be deleted while this selection exists.
	In this case, just the picture box.
----------------------------------------------------------------------------------------------*/
void VwPictureSelection::AddToKeepList(LazinessIncreaser *pli)
{
	pli->KeepSequence(m_plbox, m_plbox->NextOrLazy());
}

bool VwPictureSelection::LeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping)
{
	MoveIpVerticalByAmount(pvg, -2, true, false);
	return true;
}

bool VwPictureSelection::RightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping)
{
	MoveIpVerticalByAmount(pvg, 2, true, false);
	return true;
}

bool VwPictureSelection::UpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos)
{
	return LeftArrow(pvg, true);
}

bool VwPictureSelection::DownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos)
{
	return RightArrow(pvg, true);
}

void VwPictureSelection::EndKey(IVwGraphics * pvg, bool fLogical)
{
	RightArrow(pvg, fLogical);
}

void VwPictureSelection::HomeKey(IVwGraphics * pvg, bool fLogical)
{
	LeftArrow(pvg, fLogical);
}

void VwPictureSelection::ShiftLeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping)
{
	LeftArrow(pvg, fLogical);
}

void VwPictureSelection::ShiftRightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping)
{
	RightArrow(pvg, fLogical);
}

void VwPictureSelection::ShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	LeftArrow(pvg, true);
}

void VwPictureSelection::ShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	RightArrow(pvg, true);
}

void VwPictureSelection::ShiftEndKey(IVwGraphics * pvg, bool fLogical)
{
	RightArrow(pvg, fLogical);
}

void VwPictureSelection::ShiftHomeKey(IVwGraphics * pvg, bool fLogical)
{
	LeftArrow(pvg, fLogical);
}

bool VwPictureSelection::ControlLeftArrow(IVwGraphics * pvg, bool fLogical)
{
	return LeftArrow(pvg, fLogical);
}

bool VwPictureSelection::ControlRightArrow(IVwGraphics * pvg, bool fLogical)
{
	return RightArrow(pvg, fLogical);
}

bool VwPictureSelection::ControlUpArrow(IVwGraphics * pvg)
{
	return LeftArrow(pvg, true);
}

bool VwPictureSelection::ControlDownArrow(IVwGraphics * pvg)
{
	return RightArrow(pvg, true);
}

void VwPictureSelection::ControlEndKey(IVwGraphics * pvg, bool fLogical)
{
	RightArrow(pvg, fLogical);
}

void VwPictureSelection::ControlHomeKey(IVwGraphics * pvg, bool fLogical)
{
	LeftArrow(pvg, fLogical);
}

void VwPictureSelection::ControlShiftLeftArrow(IVwGraphics * pvg, bool fLogical)
{
	LeftArrow(pvg, fLogical);
}

void VwPictureSelection::ControlShiftRightArrow(IVwGraphics * pvg, bool fLogical)
{
	RightArrow(pvg, fLogical);
}

void VwPictureSelection::ControlShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	LeftArrow(pvg, true);
}

void VwPictureSelection::ControlShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
{
	RightArrow(pvg, true);
}

void VwPictureSelection::ControlShiftEndKey(IVwGraphics * pvg, bool fLogical)
{
	RightArrow(pvg, fLogical);
}

void VwPictureSelection::ControlShiftHomeKey(IVwGraphics * pvg, bool fLogical)
{
	LeftArrow(pvg, fLogical);
}

/*----------------------------------------------------------------------------------------------
	If the selection is part of one or more paragraphs, return a rectangle that
	contains those paragraphs. For pictures, just answer the location of the picture.

	@param prdLoc
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPictureSelection::GetParaLocation(RECT * prdLoc)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prdLoc);
	HoldGraphics hg(m_plbox->Root());
	*prdLoc = m_plbox->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Get the kind of selection it is.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_SelType(VwSelType * pstType)
{
	return E_NOTIMPL;
}

STDMETHODIMP VwPictureSelection::get_SelType(VwSelType * pstType)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstType);
	*pstType = kstPicture;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwTextSelection::get_SelType(VwSelType * pstType)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstType);
	*pstType = kstText;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Return true if selection is associated with the previous character position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::get_AssocPrev(ComBool * pfValue)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfValue);
	*pfValue = m_fAssocPrevious;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Sets whether the selection is associated with the previous character position. This also
	clears any cached selection props.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextSelection::put_AssocPrev(ComBool fValue)
{
	BEGIN_COM_METHOD;
	m_fAssocPrevious = fValue;
	m_qttp = NULL;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Utility function for getting the depth of nested layout boxes.
----------------------------------------------------------------------------------------------*/
static int CalculateBoxDepth(VwBox * pboxBottom)
{
	AssertPtr(pboxBottom);

	int cDepth = 0;
	VwBox * pbox = pboxBottom;
	while (pbox != NULL)
	{
		++cDepth;
		pbox = pbox->Container();
	}
	return cDepth;
}

/*----------------------------------------------------------------------------------------------
	Get the depth of nested layout boxes at either the anchor or end point of this selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_BoxDepth(ComBool fEndPoint, int * pcDepth)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwTextSelection::get_BoxDepth(ComBool fEndPoint, int * pcDepth)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcDepth);

	VwBox * pbox;
	if (fEndPoint)
		pbox = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	else
		pbox = m_pvpbox;
	*pcDepth = CalculateBoxDepth(pbox);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwPictureSelection::get_BoxDepth(ComBool fEndPoint, int * pcDepth)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcDepth);

	*pcDepth = CalculateBoxDepth(m_plbox);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Utility function for getting the index of the box at the given level.
----------------------------------------------------------------------------------------------*/
static int CalculateBoxIndex(int iLevel, VwBox * pboxBottom)
{
	AssertPtr(pboxBottom);

	Vector<VwBox *> vpbox;
	VwBox * pbox = pboxBottom;
	while (pbox != NULL)
	{
		vpbox.Push(pbox);
		pbox = pbox->Container();
	}
	if ((unsigned)iLevel > (unsigned)vpbox.Size())
		ThrowHr(WarnHr(E_INVALIDARG));
	if (iLevel == 0)
		return 0;		// The only possible value.
	int iAtLevel = 0;
	int iMax = vpbox.Size() - 1;
	int iDepth = iMax - iLevel;		// convert iLevel into index of vpbox[].
	VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(vpbox[iDepth+1]);
	AssertPtr(pgbox);
	for (pbox = pgbox->FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		if (pbox == vpbox[iDepth])
			break;
		if (pbox->Container() == vpbox[iDepth+1])
			++iAtLevel;
	}
	return iAtLevel;
}

/*----------------------------------------------------------------------------------------------
	Get the index of the box at the given level which contains this selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_BoxIndex(ComBool fEndPoint, int iLevel, int * piAtLevel)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwTextSelection::get_BoxIndex(ComBool fEndPoint, int iLevel, int * piAtLevel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(piAtLevel);

	VwBox * pbox;
	if (fEndPoint)
		pbox = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	else
		pbox = m_pvpbox;
	*piAtLevel = CalculateBoxIndex(iLevel, pbox);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwPictureSelection::get_BoxIndex(ComBool fEndPoint, int iLevel, int * piAtLevel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(piAtLevel);

	*piAtLevel = CalculateBoxIndex(iLevel, m_plbox);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Utility function for getting the number of boxes at the given level of the layout.
----------------------------------------------------------------------------------------------*/
static int CountBoxesAtLevel(int iLevel, VwBox * pboxBottom)
{
	AssertPtr(pboxBottom);

	Vector<VwBox *> vpbox;
	VwBox * pbox = pboxBottom;
	while (pbox != NULL)
	{
		vpbox.Push(pbox);
		pbox = pbox->Container();
	}
	if ((unsigned)iLevel > (unsigned)vpbox.Size())
		ThrowHr(WarnHr(E_INVALIDARG));
	if (iLevel == 0)
		return 1;		// The only possible value.
	int cBoxes = 0;
	int iMax = vpbox.Size() - 1;
	int iDepth = iMax - iLevel;		// convert iLevel into index of vpbox[].
	VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(vpbox[iDepth+1]);
	AssertPtr(pgbox);
	for (pbox = pgbox->FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		if (pbox->Container() == vpbox[iDepth+1])
			++cBoxes;
	}
	return cBoxes;
}

/*----------------------------------------------------------------------------------------------
	Get the number of boxes at the given level of the layout, which must be above this
	selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_BoxCount(ComBool fEndPoint, int iLevel, int * pcAtLevel)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwTextSelection::get_BoxCount(ComBool fEndPoint, int iLevel, int * pcAtLevel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcAtLevel);

	VwBox * pbox;
	if (fEndPoint)
		pbox = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	else
		pbox = m_pvpbox;
	*pcAtLevel = CountBoxesAtLevel(iLevel, pbox);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwPictureSelection::get_BoxCount(ComBool fEndPoint, int iLevel, int * pcAtLevel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcAtLevel);

	*pcAtLevel = CountBoxesAtLevel(iLevel, m_plbox);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

/*----------------------------------------------------------------------------------------------
	Utility function for getting the type of box at the given level which contains this
	selection.

	VwBox
		VwLazyBox : VwBox
		VwGroupBox : VwBox
			VwPileBox : VwGroupBox
				VwDivBox : VwPileBox
					VwRootBox : VwDivBox
				VwInnerPileBox : VwPileBox
					VwMoveablePileBox : VwInnerPileBox
				VwTableBox : VwPileBox
				VwTableCellBox : VwPileBox
			VwTableRowBox : VwGroupBox
			VwParagraphBox : VwGroupBox
				VwConcParaBox : VwParagraphBox
		VwLeafBox : VwBox
			VwAnchorBox : VwLeafBox
			VwSeparatorBox : VwLeafBox
			VwBarBox : VwLeafBox
			VwPictureBox : VwLeafBox
				VwIndepPictureBox : VwPictureBox
					VwIntegerPictureBox : VwIndepPictureBox
			VwStringBox : VwLeafBox
				VwDropCapStringBox : VwStringBox
----------------------------------------------------------------------------------------------*/
static VwBoxType GetBoxType(int iLevel, VwBox * pboxBottom)
{
	AssertPtr(pboxBottom);

	Vector<VwBox *> vpbox;
	VwBox * pbox = pboxBottom;
	while (pbox != NULL)
	{
		vpbox.Push(pbox);
		pbox = pbox->Container();
	}
	if ((unsigned)iLevel > (unsigned)vpbox.Size())
		ThrowHr(WarnHr(E_INVALIDARG));
	int iMax = vpbox.Size() - 1;
	int iDepth = iMax - iLevel;		// convert iLevel into index of vpbox[].
	pbox = vpbox[iDepth];

	if (pbox->IsLazyBox())
		return kvbtLazy;
	VwGroupBox * pgroupb = dynamic_cast<VwGroupBox *>(pbox);
	if (pgroupb != NULL)
	{
		if (pbox->IsParagraphBox())
		{
			VwConcParaBox * pcpbox = dynamic_cast<VwConcParaBox *>(pbox);
			if (pcpbox != NULL)
				return kvbtConcPara;
			return kvbtParagraph;
		}
		if (pbox->IsPileBox())
		{
			if (pbox->IsMoveablePile())
				return kvbtMoveablePile;
			if (pbox->IsInnerPileBox())
				return kvbtInnerPile;
			VwRootBox * prootb = dynamic_cast<VwRootBox *>(pbox);
			if (prootb != NULL)
				return kvbtRoot;
			VwDivBox * pdivb = dynamic_cast<VwDivBox *>(pbox);
			if (pdivb != NULL)
				return kvbtDiv;
			VwTableBox * ptableb = dynamic_cast<VwTableBox *>(pbox);
			if (ptableb != NULL)
				return kvbtTable;
			VwTableCellBox * pcellb = dynamic_cast<VwTableCellBox *>(pbox);
			if (pcellb != NULL)
				return kvbtTableCell;
			return kvbtPile;
		}
		VwTableRowBox * prowb = dynamic_cast<VwTableRowBox *>(pbox);
		if (prowb != NULL)
			return kvbtTableRow;
		return kvbtGroup;
	}
	VwLeafBox * pleafb = dynamic_cast<VwLeafBox *>(pbox);
	if (pleafb != NULL)
	{
		if (pbox->IsDropCapBox())
			return kvbtDropCapString;
		if (pbox->IsStringBox())
			return kvbtString;
		if (pbox->IsAnchor())
			return kvbtAnchor;
		VwSeparatorBox * psepb = dynamic_cast<VwSeparatorBox *>(pbox);
		if (psepb != NULL)
			return kvbtSeparator;
		VwBarBox * pbarb = dynamic_cast<VwBarBox *>(pbox);
		if (pbarb != NULL)
			return kvbtBar;
		VwIntegerPictureBox * pintpicb = dynamic_cast<VwIntegerPictureBox *>(pbox);
		if (pintpicb != NULL)
			return kvbtIntegerPicture;
		VwIndepPictureBox * pindpicb = dynamic_cast<VwIndepPictureBox *>(pbox);
		if (pindpicb != NULL)
			return kvbtIndepPicture;
		VwPictureBox * ppicb = dynamic_cast<VwPictureBox *>(pbox);
		if (ppicb != NULL)
			return kvbtPicture;
		return kvbtLeaf;
	}
	return kvbtUnknown;
}

/*----------------------------------------------------------------------------------------------
	Get the type of box at the given level which contains this selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSelection::get_BoxType(ComBool fEndPoint, int iLevel, VwBoxType * pvbt)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwTextSelection::get_BoxType(ComBool fEndPoint, int iLevel, VwBoxType * pvbt)
{
	BEGIN_COM_METHOD;

	VwBox * pbox;
	if (fEndPoint)
		pbox = m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox;
	else
		pbox = m_pvpbox;
	*pvbt = GetBoxType(iLevel, pbox);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

STDMETHODIMP VwPictureSelection::get_BoxType(ComBool fEndPoint, int iLevel, VwBoxType * pvbt)
{
	BEGIN_COM_METHOD;

	*pvbt = GetBoxType(iLevel, m_plbox);

	END_COM_METHOD(g_fact, IID_IVwSelection);
}

// The guts of AdjustForStringReplacement, deals with one end of the selection.
bool VwTextSelection::AdjustForStringReplacement1(VwTxtSrc * psrcModify, int itssMin, int itssLim,
	VwTxtSrc * psrcRep, int & ich, VwParagraphBox * pvpbox)
{
	if (pvpbox->Source() != psrcModify)
		return false; // change not relevant.
	int ichMinRep = psrcModify->IchStartString(itssMin);
	if (ich <= ichMinRep)
		return false;
	int ichLimRep = psrcModify->IchStartString(itssLim);
	int cchNew = psrcRep->Cch();
	int dcch = cchNew - (ichLimRep - ichMinRep); // amount string length changed.
	if (dcch == 0)
		return false;
	if (ich > ichLimRep)
	{
		// Char index is beyond the range changed, adjust it by how much the replaced
		// strings differ from the others.
		ich += dcch;
		return true;
	}
	// Otherwise, it's within the range replaced. We will keep it within the new range,
	// but otherwise not change it.
	if (ich > ichMinRep + cchNew)
	{
		ich = ichMinRep + cchNew;
		return true;
	}
	return false;
}
// The indicated text source is being modified, replacing the specified range of strings
// with those from srcRep. If this is the text source for either end of THIS selection,
// adjust it as well as possible. We don't try to be especially smart if we're IN the
// modified string, just make sure we're in range. If we're AFTER it, reduce char offsets
// appropriately.
bool VwTextSelection::AdjustForStringReplacement(VwTxtSrc * psrcModify, int itssMin, int itssLim,
	VwTxtSrc * psrcRep)
{
	bool fDidChange = false;
	if (m_qtsbProp)
	{
		int ichMinChange = psrcModify->IchStartString(itssMin);
		if (ichMinChange == m_ichMinEditProp)
		{
			// We're adjusting the property we're currently editing.
			// Typically DoUpdateProp has already adjusted the variables.
			// To be quite sure, we make the limit of the edit prop the length of the first
			// replacement string, and ensure that anchor and end are in range.
			Assert(m_pvpboxEnd == NULL); // active editing should be in a single paragraph.
			m_ichLimEditProp = m_ichMinEditProp + psrcRep->IchStartString(1);
			if (m_ichAnchor > m_ichLimEditProp)
			{
				m_ichAnchor = m_ichLimEditProp;
				fDidChange = true;
			}
			if (m_ichEnd > m_ichLimEditProp)
			{
				m_ichEnd = m_ichLimEditProp;
				fDidChange = true;
			}
			return fDidChange;
		}
	}
	fDidChange |= AdjustForStringReplacement1(psrcModify, itssMin, itssLim, psrcRep, m_ichAnchor, m_pvpbox);
	fDidChange |= AdjustForStringReplacement1(psrcModify, itssMin, itssLim, psrcRep, m_ichEnd,
		(m_pvpboxEnd ? m_pvpboxEnd : m_pvpbox));
	return fDidChange;
}

// Default does nothing.
bool VwSelection::AdjustForStringReplacement(VwTxtSrc * psrcModify, int itssMin, int itssLim,
	VwTxtSrc * psrcRep)
{
	return false;
}
//:>********************************************************************************************
//:>	Explicit instantation of collection classes used only in this file.
//:>********************************************************************************************
#include "HashMap_i.cpp"
#include "Vector_i.cpp"
#include "Set_i.cpp"
template class HashMap<HvoTagRec, UpdateInfo>;	// Hungarian: hmhtru
template class Vector<OLECHAR>;
