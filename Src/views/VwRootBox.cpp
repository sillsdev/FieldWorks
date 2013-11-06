/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwRootBox.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE



#undef ENABLE_TSF
#define ENABLE_TSF

#undef Tracing_KeybdSelection
//#define Tracing_KeybdSelection

#ifndef WIN32
const CLSID CLSID_ViewInputManager = {0x830BAF1F, 0x6F84, 0x46EF, {0xB6, 0x3E, 0x3C, 0x1B, 0xFD, 0xF9, 0xE8, 0x3E}};
#endif

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

VwRootBox::VwRootBox(VwPropertyStore * pzvps)
	:VwDivBox(pzvps)
{
	Init();
}

// Protected default constructor used for CreateCom
VwRootBox::VwRootBox()
{
	Init();
}

void VwRootBox::Init()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_fDirty = false;
	m_fNewSelection = false;
	m_fInDrag = false;
	m_hrSegmentError = S_OK;
	m_cMaxParasToScan = 4;
	// Usually set in Layout method, but some tests don't do this...
	// play safe also for any code called before Layout.
	m_ptDpiSrc.x = 96;
	m_ptDpiSrc.y = 96;

#ifdef ENABLE_TSF
#ifdef WIN32
	VwTextStorePtr qtxs;
	qtxs.Attach(NewObj VwTextStore(this));
	CheckHr(qtxs->QueryInterface(IID_IViewInputMgr, (void**)&m_qvim));
#else
	m_qvim.CreateInstance(CLSID_ViewInputManager);
#endif
#endif /*ENABLE_TSF*/
}


VwRootBox::~VwRootBox()
{
#ifdef ENABLE_TSF
	Assert(!m_qvim); // Make sure the Close method was called before it gets destroyed.
#endif /*ENABLE_TSF*/
	Assert(!m_qsda); // Make sure the Close method was called before it gets destroyed.
	// Any selections that may still be around (e.g., saved in the Find dialog), cannot be
	// used for anything any more so mark them invalid.
	for (int isel = 0; isel < m_vselInUse.Size(); isel++)
	{
		m_vselInUse[isel]->MarkInvalid();
	}
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwRootBox::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IVwRootBox *>(this));
	else if (riid == IID_IVwRootBox)
		*ppv = static_cast<IVwRootBox *>(this);
	else if (riid == IID_IVwNotifyChange)
		*ppv = static_cast<IVwNotifyChange *>(this);
	// trick to allow recovery of guaranteed internal pointer from interface pointer.
	else if (&riid == &CLSID_VwRootBox || &riid == &CLSID_VwInvertedRootBox)
		*ppv = static_cast<VwRootBox *>(this);
	else if (riid == IID_IServiceProvider)
		*ppv = static_cast<IServiceProvider *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IVwRootBox *>(this),
			IID_IVwRootBox, IID_IVwNotifyChange);
		return S_OK;
	}
	else
	{
		// Not worth it: DotNet asks for all kinds of interfaces we don't do.
		//StrAnsi stuError;
		//stuError.Format("Could not provide interface %g; compare %g", &riid, &IID_IServiceProvider);
		//Warn(stuError.Chars());
		return E_NOINTERFACE;
	}

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Views.VwRootBox"),
	&CLSID_VwRootBox,
	_T("SIL Root Box"),
	_T("Apartment"),
	&VwRootBox::CreateCom);


void VwRootBox::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwRootBox> qrootb;
	qrootb.Attach(NewObj VwRootBox());		// ref count initialy 1
	CheckHr(qrootb->QueryInterface(riid, ppv));
}

// Another generic factory (put here rather arbitrarily) to allow
// creating VwCacheDa objects using CoCreateInstance.
// We don't want this in the VwCacheDa.cpp file because that gets included in multiple projects.
static GenericFactory g_factCacheDa(
	_T("SIL.Views.VwCacheDa"),
	&CLSID_VwCacheDa,
	_T("SIL Data Cache"),
	_T("Apartment"),
	&VwCacheDa::CreateCom);

// And yet another one.
static GenericFactory g_factUndoDa(
	_T("SIL.Views.VwUndoDa"),
	&CLSID_VwUndoDa,
	_T("SIL Undoable Data Access"),
	_T("Apartment"),
	&VwUndoDa::CreateCom);

//:>********************************************************************************************
//:>	IVwNotifyChange methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Pass it on to the appropriate notifier(s), if any.
	NOTE: Changes made here should NOT sync in any way, because of the danger that the sync'd
	roots haven't seen the change yet and won't be in a consistent state.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::PropChanged(HVO hvo, PropTag tag, int ivMin, int cvIns,
	int cvDel)
{
	BEGIN_COM_METHOD;

	int ivMinDisp;
	if (m_qsda)
	{
		CheckHr(m_qsda->GetDisplayIndex(hvo, tag, ivMin, &ivMinDisp));
		// Most likely to happen when item has been deleted and nothing follows that will
		// be displayed at the old index.
		if (ivMinDisp < 0)
		{
			// Don't force a reconstruct on insert - new item just won't be displayed
			if (cvIns == 0)
				Reconstruct(true);
			return S_OK;
		}
	}
	else
		ivMinDisp = ivMin;

	m_fIsPropChangedInProgress = true;
	try
	{
		BuildNotifierMap();

		// Build a vector of all notifiers interested in that object. We need this because
		// calling PropChanged on one notifier could change the map.
		NotifierVec vpanote;

		ObjNoteMap::iterator itLim;
		ObjNoteMap::iterator it;
		// repeat this until we get all the way (typically the first try)
		if (m_mmhvoqnote.Retrieve(hvo, &it, &itLim))
		{
			for (; it != itLim; ++it)
			{
				vpanote.Push(*it);
			}
		}
		for (int i = 0; i < vpanote.Size(); i++)
		{
			// We need this check because doing the regenerate on one notifier might
			// delete another one in the list. If so, we don't want to try to regenerate
			// the obsolete one.
			if (vpanote[i]->KeyBox())
				CheckHr(vpanote[i]->PropChanged(hvo, tag, ivMinDisp, cvIns, cvDel));
			// REVIEW JohnT: should we try to do the rest even if one fails?
		}
	}
	catch(...)
	{
		m_fIsPropChangedInProgress = false;
		throw;
	}
	m_fIsPropChangedInProgress = false;

	END_COM_METHOD(g_fact, IID_IVwNotifyChange);
}

/***********************************************************************************************
	Initialization and setup
	Clients must call Init, and exactly one of SetRootObjects, or SetRootVariant
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	This must be called along with either SetRootObjects or SetRootVariant
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::SetSite(IVwRootSite * pvrs)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvrs);

	m_qvrs = pvrs;

#ifdef ENABLE_TSF
	if (m_qvim)
		CheckHr(m_qvim->Init(this));
#endif /*ENABLE_TSF*/

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Pass in the repository that will be used to get spell-checkers.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::SetSpellingRepository(IGetSpellChecker * pgsp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pgsp);
	m_qgspCheckerRepository = pgsp;
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Use if the view contains one or more independent objects, which the corresponding
	view constructor sets up using Display. The corresponding fragment is used for each.
	You may also supply a style sheet (may be null) that will modify the standard
	defaults for everything in the view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::SetRootObjects(HVO * prghvo, IVwViewConstructor ** prgpvwvc,
	int * prgfrag, IVwStylesheet * pss, int chvo)
{
	BEGIN_COM_METHOD;

	ChkComArrayArg(prghvo, chvo);
	ChkComArrayArg(prgpvwvc, chvo);
	ChkComArrayArg(prgfrag, chvo);
	ChkComArgPtrN(pss);

	// ENHANCE: JohnT: also report error if someone has called SetRootVariant
	if (chvo < 0)
		ThrowInternalError(E_INVALIDARG, "Root box needs at least one object");

	// This assert is here as a warning. The Views code itself handles multiple roots
	// OK (AFAIK: JohnT), but it has not been tested for a while since we are currently
	// not using the capability, and various code (e.g., CollectorEnv, and therefore the
	// Find/Replace dialog, and some export mechanisms) do not support it. Note also that
	// there is currently no means of retrieving more than the first root object from
	// the view. SelectionHelper therefore also can't retrieve more than the index of
	// the root object.
	Assert(chvo <= 1);

	m_vhvo.Replace(0, m_vhvo.Size(), prghvo, chvo);
	m_vqvwvc.Replace(0, m_vqvwvc.Size(), prgpvwvc, chvo);
	m_vfrag.Replace(0, m_vfrag.Size(), prgfrag, chvo);
	m_chvoRoot = chvo;
	m_qss = pss;
	if (m_fConstructed)
		CheckHr(Reconstruct());

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Retrieve information set by SetRootObject(s)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::GetRootObject(HVO * phvo,
	IVwViewConstructor ** ppvwvc, int * pfrag, IVwStylesheet ** ppss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);
	ChkComOutPtr(ppvwvc);
	ChkComOutPtr(pfrag);
	ChkComOutPtr(ppss);
	*ppss = m_qss;
	AddRefObj(*ppss);
	if (m_vhvo.Size() == 0)
		return S_OK; // return everything else null or zero.
	*phvo = m_vhvo[0];
	*ppvwvc = m_vqvwvc[0];
	AddRefObj(*ppvwvc);
	*pfrag = m_vfrag[0];

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	Use if the view contains a single root objects, which the
	view constructor sets up using Display. The  fragment is passed to Display.
	You may also supply a style sheet (may be null) that will modify the standard
	defaults for everything in the view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::SetRootObject(HVO hvo, IVwViewConstructor * pvvc,
	int frag, IVwStylesheet * pss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvvc);
	ChkComArgPtrN(pss);
	return SetRootObjects(&hvo, &pvvc, &frag, pss, 1);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Use if view contains some basic item. The view constructor, if any, will be asked
	to display it using DisplayVariant; if no view constructor is supplied, a default
	view of the variant will be produced. Currently the system knows how to make
	default views of strings and ints only. A style rule may be supplied to control
	default appearance.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::SetRootVariant(VARIANT v, IVwStylesheet * pss,
	IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvvc);
	ChkComArgPtr(pss);
	// ENHANCE JohnT: implement.
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Set an Overlay specifier, which will cause certain appearance changes in text.
	Setting the overlay will cause a complete regeneration of the display,
	and may destroy the selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::putref_Overlay(IVwOverlay * pvo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvo);
	// Do NOT skip if pvo == m_qvo, because when an attribute of a tag in an overlay
	// changes, we need to refresh the window. In this case, pvo could be the same overlay as
	// the current overlay, but with an attribute changed.
	m_qvo = pvo;
	m_qvrs->OverlayChanged(this, m_qvo);
	if (m_fConstructed)
		LayoutFull();
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_Overlay(IVwOverlay ** ppvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvo);
	*ppvo = m_qvo;
	AddRefObj(*ppvo);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Use if view will display a single TsString. The property rule governs its
	appearance. This method always produces the standard default display of the
	string; the view constructor is not used in constructing it.
	If a view constructor and fragment are supplied, they will be used (with
	object cookie null and tag 0) to notify you of editing of the string,
	using UpdateProp; otherwise, the string will be non-editable.
	Calling this must not be combined with calls to the other initializers;
	however, it is permitted to call this method repeatedly. Each call completely
	replaces the contents of the root box. No invalidate messages will be sent.
	This can be used to make one root box display different strings in different
	places; note however that this makes subsequent paint operations somewhat
	slower, in that the root must be setup and laid out for each string each time.

	Note: not yet fully implemented; unusable.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::SetRootString(ITsString * ptss,
	IVwStylesheet * pss, IVwViewConstructor * pvwvc, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);
	ChkComArgPtrN(pss);
	ChkComArgPtrN(pvwvc);

	VwBox * pboxFirst = FirstBox();
	VwParagraphBox * pvpbox;
	// Come up with the default VwPropertyStore, but don't install it till we know all is
	// well.
	VwPropertyStorePtr qzvps;
	qzvps.Attach(NewObj VwPropertyStore()); // constructor does default state
	qzvps->putref_Stylesheet(pss);
	m_qss = pss;

	VwTxtSrcPtr qts;
	VpsTssVec vpst;

	int cch;
	CheckHr(ptss->get_Length(&cch));

	if (pboxFirst)
	{
		// If we have any contents, it should be a single paragraph box containing a single
		// primary string.
		pvpbox = dynamic_cast<VwParagraphBox *>(pboxFirst);
		if (!pvpbox)
			ThrowHr(WarnHr(E_UNEXPECTED));
		qts = pvpbox->Source();
		if (qts->CStrings() != 1)
			ThrowHr(WarnHr(E_UNEXPECTED));

		// ENHANCE JohnT: put new string in qts, find the old notifier and update it, update
		// screen, etc.--finish implementing.
		Assert(false);
	}
	else
	{
		pvpbox = NewObj VwParagraphBox(qzvps);
		// ENHANCE JohnT: put string in para, create a notifier, link box to container,...
		Assert(false);
	}
	m_qzvps = qzvps;
	return E_NOTIMPL;	// The partial implementation may be totally wrong as well.
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	Where the root is initialized with SetRootVariant, this allows the client to
	retrieve the current value, as edited by the end user. This allows a simple view-
	based editor to work without a HVO at all.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::GetRootVariant(VARIANT * pv)
{
	BEGIN_COM_METHOD;
	// ENHANCE: JohnT: implement. Make sure the variant is up-to-date. Typically, get a string
	// from the first (string) box of the first (paragraph) box of the root, and parse if
	// necessary. Then return the variant value.
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::putref_DataAccess(ISilDataAccess * psda)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(psda);

	if (m_qsda)
		CheckHr(m_qsda->RemoveNotification(this));

	CheckHr(psda->AddNotification(this));

	m_qsda = psda;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Extract the data access object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_DataAccess(ISilDataAccess ** ppsda)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsda);
	*ppsda = m_qsda;
	AddRefObj(*ppsda);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

//:>********************************************************************************************
//:>	Serialization
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Write the contents of the box to a stream; read it back and reconstruct the boxes. Links to
	the underlying objects are not recorded.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::Serialize(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Restore the state of a newly created box from info saved by Serialize().
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::Deserialize(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	Write the contents of this root box to the stream in WorldPad XML format.

	@param pstrm Pointer to an IStream object for output.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::WriteWpx(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);

	// Sorry, this will expand everything.  There's no getting around it!
	VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(FirstRealBox());
	Assert(pgbox);
	VwBox * pbox;
	for (pbox = pgbox->FirstRealBox(); pbox; pbox = pbox->NextRealBox())
		WriteWpxBoxes(pstrm, pbox);

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Write the contents of a box owned by a VwGroupBox to the stream in WorldPad XML format.

	@param pstrm Pointer to an IStream object for output.
	@param pbox Pointer to an inner box owned by a pile box.
----------------------------------------------------------------------------------------------*/
void VwRootBox::WriteWpxBoxes(IStream * pstrm, VwBox * pbox)
{
	AssertPtr(pstrm);
	AssertPtr(pbox);

	if (pbox->IsParagraphBox())
	{
		// This is the only kind of box we currently know how to write out in WorldPad XML
		// format.
		VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
		AssertPtr(pvpbox);
		pvpbox->WriteWpxText(pstrm);
		return;
	}
	// If this is a group box, recurse!
	VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(pbox);
	if (pgbox)
	{
		// Sorry, this will expand everything.  There's no getting around it!
		for (pbox = pgbox->FirstRealBox(); pbox; pbox = pbox->NextRealBox())
			WriteWpxBoxes(pstrm, pbox);
		return;
	}
#if 99-99
	FormatToStream(pstrm, "  <StTxtPara>%n");
	FormatToStream(pstrm, "    <StyleRules1002>%n");
	FormatToStream(pstrm, "      <Prop namedStyle=\"Normal\"/>%n");
	FormatToStream(pstrm, "    </StyleRules1002>%n");
	FormatToStream(pstrm, "    <Contents1003>%n");
	FormatToStream(pstrm, "      <Str><Run ws=\"ENG\" ows=\"0\" bold=\"on\" italic=\"on\"/>");
	FormatToStream(pstrm, "DEBUG: This box ");
	FormatToStream(pstrm, "<Run ws=\"ENG\" ows=\"0\"/>(%08x)", pbox);
	FormatToStream(pstrm, "<Run ws=\"ENG\" ows=\"0\" bold=\"on\" italic=\"on\"/>");
	FormatToStream(pstrm, " is ");
	if (pbox->IsLazyBox())
	{
		FormatToStream(pstrm, "Lazy!!");
	}
	else if (pbox->IsStringBox())
	{
		FormatToStream(pstrm, "a StringBox.");
	}
	else if (pbox->IsBoxFromTsString())
	{
		FormatToStream(pstrm, "from a TsString.");
	}
	else if (pbox->IsInnerPileBox())
	{
		FormatToStream(pstrm, "an InnerPileBox.");
	}
	else
	{
		FormatToStream(pstrm, "an unknown type??");
	}
	FormatToStream(pstrm, "</Str>%n");
	FormatToStream(pstrm, "    </Contents1003>%n");
	FormatToStream(pstrm, "  </StTxtPara>%n");
#endif
}


//:>********************************************************************************************
//:>	Selections
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the current selection as an object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_Selection(IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);
	*ppsel = m_qvwsel;
	AddRefObj(*ppsel);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Get rid of the current selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::DestroySelection()
{
	BEGIN_COM_METHOD;

	if (!m_qvwsel)
		return S_OK; // nothing to destroy
#ifdef ENABLE_TSF
	// If we don't do this the text service keeps trying to do things
	// and we get errors because our VwTextStore doesn't work right when there's no selection.
	if (m_qvim)
		CheckHr(m_qvim->TerminateAllCompositions());
#endif /*ENABLE_TSF*/
	m_qvwsel->Hide();
	m_qvwsel = NULL;
	NotifySelChange(ksctDeleted, false);

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Make sure the root box has been 'constructed' If not, do so. Returns true it was possible
	to construct (and, if requested, lay out) successfully. This can't be done if we still
	have a negative available width.
	If fDoLayout is true and the view has to be constructed, also lay it out.
----------------------------------------------------------------------------------------------*/
bool VwRootBox::EnsureConstructed(bool fDoLayout)
{
	if (m_fConstructed)
		return true;
	HoldLayoutGraphics hg(this);
	int dxsAvailWidth;
	CheckHr(m_qvrs->GetAvailWidth(this, &dxsAvailWidth));
	if (dxsAvailWidth <= 0)
		return false;
	// Recheck this...getting a Graphics object can cause the window handle to get created,
	// which can result in calling Construct...and calling it twice is a problem (e.g., LT-8224)
	if (m_fConstructed)
		return true;
	Construct(hg.m_qvg, dxsAvailWidth);
	if (fDoLayout)
		Layout(hg.m_qvg, dxsAvailWidth);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Make a selection in a completely general way. The arguments and behavior are documented
	at length in views.idh.
	Char indexes in this routine are logical.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MakeTextSelection(int ihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
	int tagTextProp, int cpropPrevious, int ichAnchor, int ichEnd, int ws, ComBool fAssocPrev,
	int ihvoEnd, ITsTextProps * pttpIns, ComBool fInstall, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pttpIns);
	ChkComArgPtrN(ppsel);
	if (!fInstall && !ppsel)
		ThrowInternalError(E_POINTER);
	ChkComArrayArg(prgvsli, cvsli);

	if (ppsel)
		*ppsel = NULL;
	// This could get called before Layout(). Make sure the boxes are present.
	EnsureConstructed();
	// Find the top-level notifier for the appropriate root object
	// This shouldn't need to expand lazy boxes, even if the top level display of an object
	// is a lazy box it should be the value of some property which should produce one notifier.
	VwNotifier * pnote = NotifierForSliArray(ihvoRoot, cvsli, prgvsli);
	// If we didn't find the target notifier, fail.
	if (!pnote)
		return E_FAIL;
	// Find prop index
	int iprop = pnote->TagOccurrence(tagTextProp, cpropPrevious);
	if (iprop < 0)
		return E_FAIL;
	VwBox * pboxTarget = pnote->Boxes()[iprop];
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pboxTarget);
	int itss = pnote->StringIndexes()[iprop];
	if (!pvpbox)
	{
		if (tagTextProp == ktagGapInAttrs)
		{
			// May be trying to select a literal that gets combined
			// with containing boxes.
			VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(pboxTarget);
			while (pgbox && !pvpbox)
			{
				pboxTarget = pgbox->FirstBox();
				pvpbox = dynamic_cast<VwParagraphBox *>(pboxTarget);
				pgbox = dynamic_cast<VwGroupBox *>(pboxTarget);
			}
			itss = 0; // try the first string in the paragraph.
		}
		if (!pvpbox)
			return E_FAIL; // can't make text selection
	}
	int ichMin = pvpbox->Source()->IchStartString(itss);
	int ichLim = pvpbox->Source()->IchStartString(itss + 1);
	// See LT-7695 for why we need to select what we have instead of just fail.
	if (ichAnchor > ichLim - ichMin)
		ichAnchor = ichLim - ichMin;
	if (ichAnchor < 0)
		return E_FAIL;
	ichAnchor += ichMin;  // Make it relative to the paragraph.
	VwParagraphBox * pvpboxEnd = NULL;
	if (ihvoEnd >= 0)
	{
		// Attempting to create a multi-para text selection...
		if (!pnote->Parent())
			return E_FAIL; // must be a level down to make multi-object/para selection
		VwNotifier * pnoteEnd = pnote->Parent()->FindChild(prgvsli[0].tag,
			prgvsli[0].cpropPrevious, ihvoEnd, 0);
		if (!pnoteEnd)
			return E_FAIL;
		// Currently we require that object to be represented by exactly one paragraph
		pvpboxEnd = dynamic_cast<VwParagraphBox *>(pnoteEnd->Boxes()[0]);
		if (!pvpboxEnd)
			return E_FAIL; // can't make multi-para text selection
		// And it has to be the whole paragraph
		if (pvpboxEnd->Source()->CStrings() != 1)
			return E_FAIL;
		ichMin = 0;
		ichLim = pvpboxEnd->Source()->Cch();
	}
	// Check the end point against the (original or modified) limits
	// See LT-7695 for why we need to select what we have instead of just fail.
	if (ichEnd > ichLim - ichMin)
		ichEnd = ichLim - ichMin;
	if (ichEnd < 0)
		return E_FAIL;
	ichEnd += ichMin; // Make relative to paragraph.
	VwTextSelectionPtr qtsel;
	qtsel.Attach(NewObj VwTextSelection(pvpbox, ichAnchor, ichEnd, fAssocPrev, pvpboxEnd));
	qtsel->SetInsertionProps(pttpIns);

	if (fInstall)
	{
		SetSelection(qtsel);
		ShowSelection();
	}
	if (ppsel)
		*ppsel = qtsel.Detach();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Make a range selection whose endpoints are the given insertion points.
	If pselAnchor is equivalent to pselEnd, an insertion point will result.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MakeRangeSelection(IVwSelection * pselAnchor, IVwSelection * pselEnd,
	ComBool fInstall, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pselAnchor);
	ChkComArgPtr(pselEnd);
	ChkComArgPtrN(ppsel);

	VwTextSelectionPtr qselAnchor = dynamic_cast<VwTextSelection *>(pselAnchor);
	Assert(qselAnchor);
	VwTextSelectionPtr qselEnd = dynamic_cast<VwTextSelection *>(pselEnd);
	Assert(qselEnd);

	if (qselAnchor->AnchorBox()->Root() != qselEnd->AnchorBox()->Root())
	{
		Assert(false);
		return E_INVALIDARG;
	}

	// Make a copy of the given anchor.
	IVwSelectionPtr qselNew;
	qselNew.Attach(NewObj VwTextSelection(qselAnchor->AnchorBox(),
		qselAnchor->AnchorOffset(), qselAnchor->EndOffset(),
		qselAnchor->AssocPrevious(), qselAnchor->EndBox()));

	// Extend the new selection to the given endpoint. If the so-called endpoint
	// is a range, use its endpoint.
	qselEnd->ContractToEnd();
	VwTextSelectionPtr qseltxtRet = dynamic_cast<VwTextSelection *>(qselNew.Ptr());
	Assert(qseltxtRet);
	qseltxtRet->ExtendEndTo(qselEnd);

	if (fInstall)
		CheckHr(qselNew->Install());
	if (ppsel)
		*ppsel = qselNew.Detach();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	This allows simple selections to be made without knowledge of the contents
	of the box, for example, tabbing into a field.
	If fInitial is true, it makes a selection at the first possible place;
	otherwise, the last possible place.
	If fEdit is true, the place selected must be somewhere that the user can
	do text editing.
	If fRange is true, then the IP resulting from the previous two steps is
	expanded to a range invovling all the text in the same property.
	Warning: only (true, true, false) case tested so far.
	Char indexes in this routine are logical.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MakeSimpleSel(ComBool fInitial, ComBool fEdit, ComBool fRange,
	ComBool fInstall, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ppsel);
	if (!fInstall && !ppsel)
		ThrowInternalError(E_POINTER);

	// This could get called before Layout(). Make sure the boxes are present.
	EnsureConstructed();
	return MakeSimpleSelAt(this, fInitial ? 0 : -1, fInitial, fEdit, fRange,
		fInstall, ppsel);

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

// determine the box to consider next. It will typically be pboxNext, unless
// fContinueToParents is false, in which case we return NULL unless pboxStart contains pboxNext.
// The idea is to use this to stop loops using NextInRootSeq and similar when we leave
// the children of the box we started with.
VwBox * NextCandidate(VwBox * pboxNext, VwBox * pboxStart, bool fContinueToParents)
{
	if (fContinueToParents)
		return pboxNext;
	// stop if pboxNext is not (directly or indirectly) a child of pboxStart.
	VwGroupBox * pgboxStart = dynamic_cast<VwGroupBox *>(pboxStart);
	if (pgboxStart && pgboxStart->Contains(pboxNext))
		return pboxNext;
	return NULL;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HRESULT VwRootBox::MakeSimpleSelAt(VwBox * pboxStart, int itssStart,
	ComBool fInitial, ComBool fEdit, ComBool fRange, ComBool fInstall, IVwSelection ** ppsel,
	bool fContinueToParents)
{
	// mostly dummy variables we will need to call EditableSubstringAt.
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
	VwNoteProps vnp; // Notifier Property attributes.
	VwEditPropVal vepv;
	int ichAnchor;
	int ichEnd;
	VwParagraphBox * pvpbox;
	bool fAssocPrev = false;
	// We set a 1/10 second limit here: if there isn't something editable
	// close enough to find that fast, we assume there isn't something editable.
	// Note that we can't make the time period TOO short, because the loop below
	// generates boxes, which can call Graphite, which is not terribly speedy. :-(
	DWORD ticks = GetTickCount();
	// Provide a local temp variable for use if the pointer passed in is NULL.  See DN-764 for
	// what can happen without this.
	IVwSelectionPtr qselT;
	if (ppsel == NULL)
		ppsel = &qselT;

	if (fInitial)
	{
		for (VwBox * pbox = pboxStart;
			pbox;
			pbox = NextCandidate(pbox->NextInRootSeq(), pboxStart, fContinueToParents))
		{
			// For now we can only make selections in paragraph boxes
			pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
			if (!pvpbox)
				continue;
			// Moreover, for now it must be a position in the paragraph where
			// there is a string.
			int ctss = pvpbox->Source()->CStrings();
			if (!ctss)
				continue; // unlikely, but make sure
			ITsStringPtr qtss;
			if (itssStart == -1)
				itssStart = 0;
			for (int itss = (pbox == pboxStart ? itssStart : 0); itss < ctss; itss++)
			{
				pvpbox->Source()->StringAtIndex(itss, &qtss);
				if (!qtss)
				{
					// see if we can make a selection INSIDE the child box.
					pbox = pvpbox->ChildAtStringIndex(itss);
					MakeSimpleSelAt(pbox, -1, fInitial, fEdit, fRange, fInstall, ppsel, false);
					if (*ppsel)
						return S_OK;
					continue; // if we can't make one in the child, try an earlier string in this.
				}
				ichAnchor = pvpbox->Source()->IchStartString(itss);
				vepv = pvpbox->EditableSubstringAt(ichAnchor, ichAnchor, false, &hvoEdit,
					&tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
					&qanote, &iprop, &vnp, &itssProp, &qtssPropFirst);
				if (fEdit && vepv != kvepvEditable)
					continue;
				// got a suitable position!
				if (fRange)
					ichEnd = ichLimEditProp;
				else
					ichEnd = ichAnchor;
				goto LGotit;
			}
			// We only want to do this after we've considered at least one paragraph,
			// because sometimes just expanding a paragraph can be kind of slow (eg, Graphite).
			if (GetTickCount() - ticks > 100)
				break;
		}
	}
	else
	{
		// Make selection near end
		fAssocPrev = true;
		for (VwBox * pbox = pboxStart;
			pbox;
			pbox = NextCandidate(pbox->NextInReverseRootSeq(), pboxStart, fContinueToParents))
		{
			// For now we can only make selections in paragraph boxes
			pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
			if (!pvpbox)
				continue;
			// Moreover, for now it must be a position in the paragraph where
			// there is a string.
			int ctss = pvpbox->Source()->CStrings();
			if (!ctss)
				continue;	// Unlikely, but make sure.
			// JohnT: this fails if the paragraph has never been laid out, for example, when
			// starting up LexText in document view. So I have removed it.
			// The questions is, why was it ever thought desirable? Is it some sort of attempt
			// to prevent selections in truncated text in paragraphs with limited numbers of lines?
			//if (!pvpbox->FirstBox())
			//	continue;	// Also unlikely, but this can happen in browse view!
			ITsStringPtr qtss;
			if (itssStart == -1)
				itssStart = ctss;
			int itss;
			if (pbox == pboxStart)
			{
				itss = itssStart + 1;
				if (itss > ctss)
					itss = ctss;
			}
			else
			{
				itss = ctss;
			}
			while (--itss >= 0)
			{
				pvpbox->Source()->StringAtIndex(itss, &qtss);
				if (!qtss)
				{
					// see if we can make a selection INSIDE the child box.
					pbox = pvpbox->ChildAtStringIndex(itss);
					MakeSimpleSelAt(pbox, -1, fInitial, fEdit, fRange, fInstall, ppsel, false);
					if (*ppsel)
						return S_OK;
					continue; // if we can't make one in the child, try an earlier string in this.
				}
				ichEnd = pvpbox->Source()->IchStartString(itss + 1);
				vepv = pvpbox->EditableSubstringAt(ichEnd, ichEnd, true, &hvoEdit,
					&tagEdit, &ichMinEditProp, &ichLimEditProp, &qvvcEdit, &fragEdit,
					&qanote, &iprop, &vnp, &itssProp, &qtssPropFirst);
				if (fEdit && vepv != kvepvEditable)
					continue;
				// got a suitable position!
				if (fRange)
					ichAnchor = ichMinEditProp;
				else
					ichAnchor = ichEnd;
				goto LGotit;
			}
			// We only want to do this after we've considered at least one paragraph,
			// because sometimes just expanding a paragraph can be kind of slow (eg, Graphite).
			if (GetTickCount() - ticks > 100)
				break;
		}
	}
	// If we get here we could not find a satisfactory selection.
	// This is perfectly reasonable--it can happen, for example, if the user selects a
	// combination of fields that contains nothing editable, and we asked for an
	// editable selection, or if the view is simply empty.
	return S_OK;
LGotit:
	VwTextSelectionPtr qtsel;
	qtsel.Attach(NewObj VwTextSelection(pvpbox, ichAnchor, ichEnd, fAssocPrev));
	int ichEndRen = pvpbox->Source()->LogToRen(ichEnd);
	if (!fInitial && !fRange && pvpbox->IsSelectionTruncated(ichEndRen))
	{
		// Insertion point at very end is bogus: move to last visible position.
		// Note (JT): this probably needs enhancing if we ever do truncated
		// interlinear texts. In that case, the very last possible IP might
		// be in a child paragraph rather than a child stringbox.
		VwStringBox * psboxLast = NULL; // find the last non-truncated string box
		for (VwBox * pbox = pvpbox->FirstBox(); pbox; pbox = pbox->Next())
		{
			if (pbox->Top() == knTruncated)
				break;
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox);
			if (!psbox)
				continue; // can't put IP there, but maybe later....
			psboxLast = psbox;
			// And go on to see if a later position will serve.
		}
		if (psboxLast)
		{
			// OK, want IP at end of this string box. Assume the end of line is a valid pos.
			int dichLim;
			CheckHr(psboxLast->Segment()->get_Lim(psboxLast->IchMin(), &dichLim));
			ichEnd = pvpbox->Source()->RenToLog(psboxLast->IchMin() + dichLim);
			qtsel.Attach(NewObj VwTextSelection(pvpbox, ichEnd, ichEnd, true));
		}
		// If we don't have a visible selection anywhere in the paragraph, we're in trouble.
		// Best bet seems to be to go with the selection we have already.
	}
	if (fInstall)
	{
		SetSelection(qtsel);
		ShowSelection();
	}
	if (ppsel)
		*ppsel = qtsel.Detach();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Given a the index of a root object, and
	an array of VwSelLevInfo objects that specifies a path from the root in the usual way,
	find the notifier for the object that the path specifies, or NULL if not found.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwRootBox::NotifierForSliArray(int ihvoRoot, int cvsli, VwSelLevInfo * prgvsli)
{
	EnsureConstructed();
	VwNotifier * pnote = VwNotifier::NextNotifierAt(0, this, -1);
	for (int inote = 0; pnote && pnote->ObjectIndex() != ihvoRoot; inote++)
	{
		pnote = pnote->NextNotifier();
	}
	for (int ilev = cvsli; pnote && --ilev >= 0; )
	{
		PropTag tag = prgvsli[ilev].tag;
		int cpropPrevious2 = prgvsli[ilev].cpropPrevious;
		int ihvo = prgvsli[ilev].ihvo;

		// We want to find a notifier directly descended from this
		pnote = pnote->FindChild(tag, cpropPrevious2, ihvo, prgvsli[ilev].ich);
	}
	return pnote;
}

/*----------------------------------------------------------------------------------------------
	Here, the path designates some object, and we suppose the display of the object is
	some sort of non-text leaf box, and make a VwPictureSelection out of that.
	If tag is specified (non-zero), the picture is a property of the object indicated
	by prgvsli.
	(Enhance: may generalize to select whole of object, making either picture or text
	selection as appropriate.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MakeSelInObj(int ihvoRoot, int cvsli, VwSelLevInfo * prgvsli, int tag,
	 ComBool fInstall, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgvsli, cvsli);
	ChkComArgPtrN(ppsel);
	if (!fInstall && !ppsel)
		ThrowInternalError(E_POINTER);
	// Find the notifier for the designated object
	// This shouldn't need to expand lazy boxes, even if the top level display of an object
	// is a lazy box it should be the value of some property which should produce one notifier.
	VwNotifier * pnote = NotifierForSliArray(ihvoRoot, cvsli, prgvsli);
	// If we didn't find the target notifier, fail.
	if (!pnote)
		return E_FAIL;
	int ibox = 0;
	VwBox * pboxStart;
	if (tag == 0)
	{
		pboxStart = pnote->FirstBox(&ibox);
	}
	else
	{
		pnote->GetBoxForProp(tag, ibox, &pboxStart);
		ibox--; // returns as index of next prop, want index of this one.
	}
	int itssStart = pnote->StringIndexes()[ibox];
	if (itssStart != -1)
	{
		// It's a box embedded in a paragraph.
		VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pboxStart);
		Assert(pvpbox);
		pboxStart = pvpbox->ChildAtStringIndex(itssStart);
	}
	VwLeafBox * plbox = dynamic_cast<VwLeafBox *>(pboxStart);
	if (!plbox)
		return E_FAIL;
	VwPictureSelectionPtr qvwpsel = NewObj VwPictureSelection(plbox, 1, 1, true);
	if (fInstall)
	{
		SetSelection(qvwpsel);
		ShowSelection();
	}
	if (ppsel)
		*ppsel = qvwpsel.Detach();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	This combines some of the features of the last two. ihvoRoot indicates a
	root object. prgvsli[cvsli-1] indicates a particular object in a particular
	property of that root. The next preceding prgvsli indicates an object that
	is part of that, and so forth. (If cvsli is 0, the object is the root
	object itself.)
	This path having designated some particular display of some particular object,
	the last three arguments then designate how to make a selection within that
	display, as in the previous method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MakeTextSelInObj(int ihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
	int cvsliEnd, VwSelLevInfo * prgvsliEnd, ComBool fInitial, ComBool fEdit, ComBool fRange,
	ComBool fWholeObj, ComBool fInstall, IVwSelection **ppsel)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgvsli, cvsli);
	if (prgvsliEnd)
		ChkComArrayArg(prgvsliEnd, cvsliEnd);
	ChkComArgPtrN(ppsel);
	if (!fInstall && !ppsel)
		ThrowInternalError(E_POINTER);
	if (cvsliEnd > 0 && cvsli != cvsliEnd )
		return E_INVALIDARG;
	if (cvsliEnd > 0 && !fWholeObj)
		return E_INVALIDARG;

	bool fEndBeforeAnchor = false;
	VwSelLevInfo * prgvsliFirst = prgvsli;
	VwSelLevInfo * prgvsliLast = NULL;
	if (cvsliEnd > 0)
	{
		prgvsliLast = prgvsliEnd;
		fEndBeforeAnchor = (prgvsli[0].ihvo > prgvsliEnd[0].ihvo);
		if (fEndBeforeAnchor)
		{
			prgvsliFirst = prgvsliEnd;
			prgvsliLast = prgvsli;
		}
	}

	// Find the top-level notifier for the appropriate root object
	// This shouldn't need to expand lazy boxes, even if the top level display of an object
	// is a lazy box it should be the value of some property which should produce one notifier.
	VwNotifier * pnote = NotifierForSliArray(ihvoRoot, cvsli, prgvsliFirst);
	// If we didn't find the target notifier, fail.
	if (!pnote)
		return S_OK;

	// Do the same for the endpoint, if any.
	VwNotifier * pnoteLast = pnote;
	if (prgvsliLast)
	{
		pnoteLast = NotifierForSliArray(ihvoRoot, cvsli, prgvsliLast);
		if (!pnoteLast)
			return S_OK;
	}

	VwBox * pboxStart;
	int itssStart;
	if (fWholeObj)
	{
		IVwSelectionPtr qselFirst;
		int ibox = 0;
		pboxStart = pnote->FirstBox(&ibox);
		itssStart = pnote->StringIndexes()[ibox];
		// Make insertion points at start and end of range, but don't install them
		CheckHr(MakeSimpleSelAt(pboxStart, itssStart, true,
			false, false, false, &qselFirst));
		if (!qselFirst)
			return S_OK;

		IVwSelectionPtr qselLast;
		pboxStart = pnoteLast->LastBox();
		itssStart = pnoteLast->LastStringIndex() - 1;
		if (itssStart < 0)
			itssStart = 0;
		CheckHr(MakeSimpleSelAt(pboxStart, itssStart, false,
			false, false, false, &qselLast));
		if (!qselLast)
			return S_OK;

		VwTextSelection * pselFirst = dynamic_cast<VwTextSelection *>(qselFirst.Ptr());
		VwTextSelection * pselLast = dynamic_cast<VwTextSelection *>(qselLast.Ptr());
		IVwSelectionPtr qselRet;
		VwTextSelection * pselRet;
		if (fEndBeforeAnchor)
		{
			qselRet = qselLast;
			pselRet = dynamic_cast<VwTextSelection *>(qselRet.Ptr());
			pselRet->ExtendEndTo(pselFirst);
		}
		else
		{
			qselRet = qselFirst;
			pselRet = dynamic_cast<VwTextSelection *>(qselRet.Ptr());
			pselRet->ExtendEndTo(pselLast);
			// pathologically, when trying to select a whole object, it may have no content...
			// for example, trying to select a whole object, where the whole object is an empty division.
			// We then find typically that the first selection we can make after the start of the object is
			// in the next object, while the last thing we can select before its end is at the end of the previous
			// object. This is not a valid selection of the target object. One way to detect it is that the
			// end-points of the selection are not in the expected order. Return a null selection in this case to indicate failure.
			ComBool fResultHasEndBeforeAnchor;
			CheckHr(pselRet->get_EndBeforeAnchor(&fResultHasEndBeforeAnchor));
			if (fResultHasEndBeforeAnchor)
				return S_OK;
		}
		if (fInstall)
		{
			SetSelection(pselRet);
			ShowSelection();
		}
		if (ppsel)
			*ppsel = qselRet.Detach();
		return S_OK;
	}
	if (fInitial)
	{
		int ibox = 0;
		pboxStart = pnote->FirstBox(&ibox);
		itssStart = pnote->StringIndexes()[ibox];
	}
	else
	{
		pboxStart = pnote->LastBox();
		itssStart = pnote->LastStringIndex() - 1;
		if (itssStart < 0)
			itssStart = 0;
	}

	return MakeSimpleSelAt(pboxStart, itssStart, fInitial, fEdit, fRange, fInstall, ppsel);

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


//:>********************************************************************************************
//:>	Passing window events to box
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle typed input by passing it to your selection if any. See the selection methods
	for details.

	Ignore typing happily if no selection. ENHANCE (JohnT): should we beep?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::OnTyping(IVwGraphics *pvg, BSTR bstrInput, VwShiftStatus ss, int * pwsPending)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvg);
	ChkComArgPtr(pwsPending);

	if (m_qvwsel)
	{
		// The following try doesn't work, because it messes things up in DN: assertion fires
		// when a character is typed in a choices field such as Researcher when in Data Entry
		// view of DN. Also possibly cause of RAID bug 4058.
		//// If anything goes wrong here and a message is displayed to the user or if the user manages
		//// to switch apps while the character processing is happening, the rootsite will lose focus,
		//// which could cause (in TE at least) a new PropChanged notification that actually sets the
		//// paragraph back (or thinks it's trying to) to its previous contents. This is bad. So,
		//// kill the rootbox's selection for the duration of this method and then reset it at the
		//// end.
		//VwSelectionPtr qvselTemp = m_qvwsel;
		//m_qvwsel = NULL;
		//try
		//{
		//	if (qvselTemp->OnTyping(pvg, bstrInput, BstrLen(bstrInput),
		//		cchBackspace, cchDelForward, chFirst,
		//		pencPending))
		//	{
		//		m_qvwsel = qvselTemp;
		//	}
		//}
		//catch(...)
		//{
		//	m_qvwsel = qvselTemp;
		//	throw;
		//}
		m_qvwsel->OnTyping(pvg, bstrInput, BstrLen(bstrInput), ss, pwsPending);
	}

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

STDMETHODIMP VwRootBox::DeleteRangeIfComplex(IVwGraphics *pvg, ComBool * pfWasComplex)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvg);
	ChkComOutPtr(pfWasComplex);

	if (m_qvwsel && m_qvwsel->IsComplexSelection())
	{
		IActionHandlerPtr qah;
		CheckHr(m_qsda->GetActionHandler(&qah));
		if (!qah)
			return S_OK; // We can't delete it if we're not processing changes
		int cactionsBefore;
		CheckHr(qah->get_UndoableActionCount(&cactionsBefore));
		int dummy = -1;
#if WIN32
		m_qvwsel->OnTyping(pvg, L"\x7F", 1, kfssNone, &dummy);
#else
		static OleStringLiteral str(L"\x7F");
		m_qvwsel->OnTyping(pvg, str, 1, kfssNone, &dummy);
#endif
		int cactionsAfter;
		CheckHr(qah->get_UndoableActionCount(&cactionsAfter));

		// Only claim to have deleted a complex range if we actually changed something.
		// This guards against, for example, trying to type over a non-editable range,
		// and then trying to merge two non-existent units of work.
		* pfWasComplex = cactionsAfter > cactionsBefore;
	}

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Pass a regular data character (including backspace and delete forward) to the view for
	processing. Note that a direct call to OnTyping is much preferred.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::OnChar(int chw)
{
	BEGIN_COM_METHOD;

	HoldScreenGraphics hg(this);
	IVwGraphics * pvg = hg.m_qvg;
	wchar chw2;
	chw2 = (wchar) chw;
	int encPending = -1;	// bogus
	m_qvwsel->OnTyping(pvg, &chw2, 1, kfssNone, &encPending);

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Pass what Windows considers a "system character" to the view for processing.
	ENHANCE: could we somehow make this and the next method less Windows specific?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::OnSysChar(int chw)
{
	return S_OK; // none implemented as yet, but ignore peacefully.
}

// Return true if the registry setting for suppressing clumping is true.
bool RegistrySuppressClumping()
{
#ifdef WIN32
	RegKey hkey;
	if(::RegOpenKeyEx(HKEY_CURRENT_USER, L"Software\\SIL\\FieldWorks", 0, KEY_READ, &hkey) != ERROR_SUCCESS)
		return false;
	OLECHAR rgch[MAX_PATH];
	DWORD cb = isizeof(rgch);
	DWORD dwT = 0;
	LONG nRet = ::RegQueryValueEx(hkey, L"ArrowByCharacter", NULL, &dwT, (BYTE *)rgch, &cb);
	if (nRet != ERROR_SUCCESS)
		return false;
	return wcscmp(L"True", rgch) == 0 || wcscmp(L"true", rgch) == 0 || wcscmp(L"TRUE", rgch) == 0;
#else
	// TODO-Linux
	// TODO Review do we need a unix equivialent.
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	Pass what Windows considers an "extended character" to the view for processing.
	ENHANCE: could we somehow make this and the previous method less Windows specific?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::OnExtendedKey(int chw, VwShiftStatus ss, int nFlags)
{
	BEGIN_COM_METHOD;

	if (!m_qvwsel)
		return S_OK;
	bool fLogArrows = nFlags && 0x1;
	bool fNeedShow = false;

	try
	{
		HoldScreenGraphics hg(this);
		IVwGraphics * pvg = hg.m_qvg;
		// Create a 'coordinate transformation' that has the right stretch factors
		// but no translation. This is adequate for moving around by key positions,
		// except that anything that expands lazy boxes should be careful to retain
		// only coordinates relative to some real box.
		Rect rcSrcRoot(0, 0, m_ptDpiSrc.x, m_ptDpiSrc.y);
		int dpiXDst, dpiYDst;
		CheckHr(pvg->get_XUnitsPerInch(&dpiXDst));
		CheckHr(pvg->get_YUnitsPerInch(&dpiYDst));
		Rect rcDstRoot(0, 0, dpiXDst, dpiYDst);
		int xdPos;				// set by VwSelection::UpArrow and VwSelection::DownArrow
		switch (chw)
		{
		case kecPageUpKey:
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				m_qvwsel->PageUpKey(pvg);
				break;
			case kfssShift:
				m_qvwsel->ShiftPageUpKey(pvg);
				break;
			case kfssControl:
				m_qvwsel->ControlPageUpKey(pvg);
				break;
			case kgrfssShiftControl:
				m_qvwsel->ControlShiftPageUpKey(pvg, rcSrcRoot, rcDstRoot);
				break;
			}
			break;
		case kecPageDownKey:
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				m_qvwsel->PageDownKey(pvg);
				break;
			case kfssShift:
				m_qvwsel->ShiftPageDownKey(pvg);
				break;
			case kfssControl:
				m_qvwsel->ControlPageDownKey(pvg, rcSrcRoot, rcDstRoot);
				break;
			case kgrfssShiftControl:
				m_qvwsel->ControlShiftPageDownKey(pvg, rcSrcRoot, rcDstRoot);
				break;
			}
			break;
			break;
		case kecEndKey:
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				m_qvwsel->EndKey(pvg, fLogArrows);
				break;
			case kfssShift:
				m_qvwsel->ShiftEndKey(pvg, fLogArrows);
				break;
			case kfssControl:
				m_qvwsel->ControlEndKey(pvg, fLogArrows);
				break;
			case kgrfssShiftControl:
				m_qvwsel->ControlShiftEndKey(pvg, fLogArrows);
				break;
			}
			break;
		case kecHomeKey:
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				m_qvwsel->HomeKey(pvg, fLogArrows);
				break;
			case kfssShift:
				m_qvwsel->ShiftHomeKey(pvg, fLogArrows);
				break;
			case kfssControl:
				m_qvwsel->ControlHomeKey(pvg, fLogArrows);
				break;
			case kgrfssShiftControl:
				m_qvwsel->ControlShiftHomeKey(pvg, fLogArrows);
				break;
			}
			break;
		case kecF7: // F7 means left-arrow-direction by one character, strictly.
			m_qvwsel->Hide();
			fNeedShow = true;
			switch(ss)
			{
			case kfssNone:
				if (!m_qvwsel->LeftArrow(pvg, fLogArrows, true))
				{
					// Need to move to the previous field.
					m_xdPos = -1;
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kfssShift:
				m_qvwsel->ShiftLeftArrow(pvg, fLogArrows, true);
				break;
			}
			break;
		case kecLeftArrowKey:
			m_qvwsel->Hide();
			fNeedShow = true;

			switch (ss)
			{
			case kfssNone:
				if (!m_qvwsel->LeftArrow(pvg, fLogArrows, RegistrySuppressClumping()))
				{
					// Need to move to the previous field.
					m_xdPos = -1;
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kfssShift:
				m_qvwsel->ShiftLeftArrow(pvg, fLogArrows, RegistrySuppressClumping());
				break;
			case kfssControl:
				if (!m_qvwsel->ControlLeftArrow(pvg, fLogArrows))
				{
					// Need to move to the previous field.
					m_xdPos = -1;
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kgrfssShiftControl:
				m_qvwsel->ControlShiftLeftArrow(pvg, fLogArrows);
				break;
			}
			break;
		case kecUpArrowKey:
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				if (!m_qvwsel->UpArrow(pvg, rcSrcRoot, rcDstRoot, &xdPos))
				{
					// Need to move to the previous field.
					m_xdPos = xdPos;
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kfssShift:
				m_qvwsel->ShiftUpArrow(pvg, rcSrcRoot, rcDstRoot);
				break;
			case kfssControl:
				if (!m_qvwsel->ControlUpArrow(pvg))
				{
					// Need to move to the previous field.
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kgrfssShiftControl:
				m_qvwsel->ControlShiftUpArrow(pvg, rcSrcRoot, rcDstRoot);
				break;
			}
			break;
		case kecF8: // F8 means right-arrow-direction by one character, strictly.
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				if (!m_qvwsel->RightArrow(pvg, fLogArrows, true))
				{
					// Need to move to the next field.
					m_xdPos = 0;
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kfssShift:
				m_qvwsel->ShiftRightArrow(pvg, fLogArrows, true);
				break;
			}
			break;
		case kecRightArrowKey:
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				if (!m_qvwsel->RightArrow(pvg, fLogArrows, RegistrySuppressClumping()))
				{
					// Need to move to the next field.
					m_xdPos = 0;
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kfssShift:
				m_qvwsel->ShiftRightArrow(pvg, fLogArrows, RegistrySuppressClumping());
				break;
			case kfssControl:
				if (!m_qvwsel->ControlRightArrow(pvg, fLogArrows))
				{
					// Need to move to the next field.
					m_xdPos = 0;
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kgrfssShiftControl:
				m_qvwsel->ControlShiftRightArrow(pvg, fLogArrows);
				break;
			}
			break;
		case kecDownArrowKey:
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				if (!m_qvwsel->DownArrow(pvg, rcSrcRoot, rcDstRoot, &xdPos))
				{
					// Need to move to the next field.
					m_xdPos = xdPos;
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kfssShift:
				m_qvwsel->ShiftDownArrow(pvg, rcSrcRoot, rcDstRoot);
				break;
			case kfssControl:
				if (!m_qvwsel->ControlDownArrow(pvg))
				{
					// Need to move to the next field.
					m_qvwsel->Show();
					return S_FALSE;
				}
				break;
			case kgrfssShiftControl:
				m_qvwsel->ControlShiftDownArrow(pvg, rcSrcRoot, rcDstRoot);
				break;
			}
			break;
		case kecTabKey:
			Assert(kecTabKey == static_cast<VwExtendedChars>(VK_TAB));
			m_qvwsel->Hide();
			fNeedShow = true;
			switch (ss)
			{
			case kfssNone:
				// Move to the next editable field in the record (if one exists).
				if (!m_qvwsel->TabKey())
				{
					m_qvwsel->Show();
					return S_FALSE;		// Signal the need to move to the next record.
				}
				break;
			case kfssShift:
				// Move to the previous editable field in the record (if one exists).
				if (!m_qvwsel->ShiftTabKey())
				{
					m_qvwsel->Show();
					return S_FALSE;		// Signal the need to move to the previous record.
				}
				break;
			case kfssControl:
				// Move to the last editable field in the record (if one exists).
				while (m_qvwsel->TabKey())
				{
					// Do nothing -- call TabKey() until it fails to move the cursor.
				}
				break;
			case kgrfssShiftControl:
				// Move to the first editable field in the record (if one exists).
				while (m_qvwsel->ShiftTabKey())
				{
					// Do nothing -- call ShiftTabKey() until it fails to move the cursor.
				}
				break;
			}
			break;

		case kecEnterKey:
			switch (ss)
			{
			case kfssShift:
				{
					m_qvwsel->Hide();
					fNeedShow = true;

					// Insert a hard line break
					wchar chwHardLineBreak = kchwHardLineBreak;
					int encPending = -1;	// bogus
					m_qvwsel->OnTyping(pvg, &chwHardLineBreak, 1, ss, &encPending);
					break;
				}
			case kfssNone:
			case kfssControl:
			case kgrfssShiftControl:
				return S_FALSE;
				break;
			}
			break;
		}
	}
	catch (...)
	{
		// Try to restore anyway...
		if (fNeedShow && m_qvwsel)
			m_qvwsel->Show();
		throw;
	}
	if (fNeedShow && m_qvwsel)
	{
		if (m_qvwsel && m_qvwsel.Ptr())
			m_qvwsel->Show();
	}

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Should be called every half second or so, at least while the selection is an IP.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::FlashInsertionPoint()
{
	BEGIN_COM_METHOD;

	if (m_vss == vssEnabled && m_qvwsel.Ptr() && m_qvwsel->IsInsertionPoint())
	{
		m_qvwsel->Invert();
	}

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Make a selection at the position that a selection would have been made by a
	${#MouseDown} using the same first four parameters, ignoring editability
	restrictions.
	@param fInstall If true, install the selection in the root box.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MakeSelAt(int xd, int yd, RECT rcSrc1, RECT rcDst1, ComBool fInstall,
	IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ppsel);
	if (!fInstall && !ppsel)
		ThrowInternalError(E_POINTER);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	Rect rcSrcBox;
	Rect rcDstBox;
	if (ppsel)
		*ppsel = NULL; // default

	HoldScreenGraphics hg(this);
	IVwGraphics * pvg = hg.m_qvg;
	// Find the most local box where the user clicked, and the transformation used to
	// draw it.
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);

	// Let that box make a selection.
	if (!pboxClick)
	{
		// Find the closest box at the same vertical position.
		int dx;
		for (dx = -16; (xd + dx) >= 0; dx -= 16)
		{
			pboxClick = FindBoxClicked(pvg, xd + dx, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);
			if (pboxClick)
				break;
		}
		if (!pboxClick)
		{
			for (dx = 16; (xd + dx) < 1600; dx += 16)
			{
				pboxClick = FindBoxClicked(pvg, xd + dx, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);
				if (pboxClick)
					break;
			}
		}
		if (!pboxClick)
		{
			return S_FALSE;
		}
	}
	else if (pboxClick->IsLazyBox())
	{
		// We don't want to expand a lazy box at this point -- let someone else do it!
		return S_FALSE;
	}
	VwSelectionPtr qvwsel;
	pboxClick->GetSelection(pvg, this, xd, yd, rcSrc1, rcDst1, rcSrcBox, rcDstBox,
		&qvwsel);
	if (!qvwsel)
		return S_FALSE;
	if (fInstall)
	{
		SetSelection(qvwsel);
		ShowSelection();
	}
	if (ppsel)
		*ppsel = qvwsel.Detach();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Make a selection in the layout box indicated by iLevel and iBox.  If fRange is true, select
	everything in that box.  Otherwise if fInitial is true, make the selection near the
	beginning of the box, else make the selection near the end.
	Note that lazy boxes are not expanded by this method.
	If this method cannot find a suitable box corresponding to the level requested and ppsel is
	non-null, it sets *ppsel to NULL (and does not install a new selection) and returns
	S_FALSE; otherwise, it will fail with E_INVALIDARG.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MakeSelInBox(IVwSelection * pselInit, ComBool fEndPoint, int iLevel,
	int iBox, ComBool fInitial, ComBool fRange, ComBool fInstall, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pselInit);
	ChkComArgPtrN(ppsel);
	if (!fInstall && !ppsel)
		ThrowInternalError(E_POINTER);

	// If the caller doesn't provide a selection, use our internal selection.
	if (pselInit == NULL)
		pselInit = m_qvwsel.Ptr();

	// Verify that the selection belongs to this root box.
	VwSelection * pvwsel = dynamic_cast<VwSelection *>(pselInit);
	AssertPtr(pvwsel);
	if (!pvwsel)
		ThrowHr(WarnHr(E_UNEXPECTED));
	if (pvwsel->RootBox() != this)
		ThrowHr(WarnHr(E_INVALIDARG));

	// Get the bottom box of the current selection to start processing from.
	VwBox * pboxBottom = NULL;
	VwTextSelection * pvwtsel = dynamic_cast<VwTextSelection *>(pselInit);
	if (pvwtsel != NULL)
	{
		if (fEndPoint)
			pboxBottom = pvwtsel->EndBox();
		if (pboxBottom == NULL)
			pboxBottom = pvwtsel->AnchorBox();
	}
	else
	{
		VwPictureSelection * pvwpsel = dynamic_cast<VwPictureSelection *>(pselInit);
		if (pvwpsel != NULL)
			pboxBottom = pvwpsel->LeafBox();
	}
	if (pboxBottom == NULL)
		ThrowHr(WarnHr(E_INVALIDARG));

	// Get the array of Boxes/Containers back to the root.
	Vector<VwBox *> vpbox;
	VwBox * pbox = pboxBottom;
	while (pbox != NULL)
	{
		vpbox.Push(pbox);
		pbox = pbox->Container();
	}
	if ((unsigned)iLevel > (unsigned)vpbox.Size())
		ThrowHr(WarnHr(E_INVALIDARG));

	// Now for the fun part: find the proper VwBox.
	VwBox * pboxStart = NULL;
	if (iLevel > 0)
	{
		// Get index into vpbox that relates properly to iLevel.
		int iDepth = (vpbox.Size() - 1) - iLevel;
		VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(vpbox[iDepth+1]);
		AssertPtr(pgbox);
		int iAtLevel = 0;
		for (pbox = pgbox->FirstBox(); pbox; pbox = pbox->NextOrLazy())
		{
			if (pbox->Container() == vpbox[iDepth+1])
			{
				if (iAtLevel == iBox)
				{
					pboxStart = pbox;
					break;
				}
				++iAtLevel;
			}
		}
	}
	else if (iBox == 0)
	{
		pboxStart = this;
	}
	if (pboxStart == NULL)
	{
		if (ppsel)
		{
			*ppsel = NULL;
			return S_FALSE;
		}
		else
		ThrowHr(WarnHr(E_INVALIDARG));
	}

	// Note that we want an editable selection if at all possible.
	return MakeSimpleSelAt(pboxStart, 0, fInitial, true, fRange, fInstall, ppsel);

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Test whether the click is in a text part of the display. A click beyond the end
	of a line, an embedded picture or divider box are not considered text. Nor is the
	space between lines. Basically, it is the area that would be inverted if the text
	were selected.
----------------------------------------------------------------------------------------------*/
HRESULT VwRootBox:: get_IsClickInText(int xd, int yd, RECT rcSrc1, RECT rcDst1,
	ComBool * pfInText)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfInText);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	Rect rcSrcBox;
	Rect rcDstBox;
	*pfInText = false; // default

	HoldGraphicsAtDst hg(this, Point(xd, yd));
	IVwGraphics * pvg = hg.m_qvg;
	// Find the most local box where the user clicked, and the transformation used to
	// draw it.
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);

	if (!pboxClick)
		return S_OK; // false, not in text

	if (!dynamic_cast<VwStringBox *>(pboxClick))
		return S_OK; // not a text box

	POINT pt;
	pt.x = xd;
	pt.y = yd;

	// Only succeeds if it's actually in the text box.

	if (pboxClick->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot).Contains(pt))
		*pfInText = true;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Test whether the click is on an object in a text part of the display.
----------------------------------------------------------------------------------------------*/
HRESULT VwRootBox:: get_IsClickInObject(int xd, int yd, RECT rcSrc1, RECT rcDst1,
	int * podt, ComBool * pfInObject)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfInObject);
	ChkComArgPtr(podt);

	*podt = 0; // Default.
	*pfInObject = false;	// Default.

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	Rect rcSrcBox;
	Rect rcDstBox;
	HoldGraphicsAtDst hg(this, Point(xd, yd));
	IVwGraphics * pvg = hg.m_qvg;
	// Find the most local box where the user clicked, and the transformation used to
	// draw it.
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);

	if (!pboxClick)
		return S_OK; // false, not in text

	VwStringBox * psbox = dynamic_cast<VwStringBox *>(pboxClick);
	if (!psbox)
		return S_OK; // not a text box

	POINT pt;
	pt.x = xd;
	pt.y = yd;

	// Only succeeds if it's actually in the text box.

	if (!pboxClick->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot).Contains(pt))
		return S_OK;	// not in the text box.

	VwSelectionPtr qvwsel;
	pboxClick->GetSelection(pvg, this, xd, yd, rcSrc1, rcDst1, rcSrcBox, rcDstBox, &qvwsel);
	if (!qvwsel)
		return S_OK;
	TtpVec vqttp;
	VwPropsVec vqvps;
	int cttp;
	CheckHr(qvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
	if (cttp == 0)
		return S_OK;
	vqttp.Resize(cttp);
	vqvps.Resize(cttp);
	CheckHr(qvwsel->GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
		(IVwPropertyStore **)vqvps.Begin(), &cttp));
	SmartBstr sbstr;
	CheckHr(vqttp[0]->GetStrPropValue(ktptObjData, &sbstr));
	if (sbstr.Length() > 0)
	{
		*pfInObject = true;
		*podt = (int)*sbstr.Chars();
	}

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	Test whether the click is on an opening or closing overlay tag (the part above or below
	the text that shows the abbreviation).
	This function returns the following:
		piGuid -> This is the index into pbstrGuids of which tag was clicked on.
					This will be -1 if the user clicked on the ...
		pbstrGuids -> This string stores 0 or more GUIDs that make up the overlay tags that
						are in the same group as the overlay tag the user clicked on.
		prcTag -> This is the rectangle in the window where the overlay tag is drawn.
		prcAllTags -> The rectangle containing the entire list of tags.
		pfOpeningTag -> True if the tag is above the line and false if below the line.
		pfInOverlayTag -> This will be set to true if the user clicked in an overlay tag.
----------------------------------------------------------------------------------------------*/
HRESULT VwRootBox::get_IsClickInOverlayTag(int xd, int yd, RECT rcSrc1, RECT rcDst1,
	int * piGuid, BSTR * pbstrGuids, RECT * prcTag, RECT * prcAllTags, ComBool * pfOpeningTag,
	ComBool * pfInOverlayTag)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(piGuid);
	ChkComOutPtr(pbstrGuids);
	ChkComArgPtr(prcTag);
	ChkComArgPtr(prcAllTags);
	ChkComOutPtr(pfOpeningTag);
	ChkComOutPtr(pfInOverlayTag);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	Rect rcSrcBox;
	Rect rcDstBox;
	HoldGraphicsAtDst hg(this, Point(xd, yd));
	IVwGraphics * pvg = hg.m_qvg;
	// Find the most local box where the user clicked, and the transformation used to
	// draw it.
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);

	if (!pboxClick)
		return S_OK; // false, not in text

	VwParagraphBox * pvwpb = dynamic_cast<VwParagraphBox *>(pboxClick->Container());
	if (!pvwpb)
		return false;

	rcSrcBox.Offset(pvwpb->Left(), pvwpb->Top());
	*pfInOverlayTag = pvwpb->FindOverlayTagAt(pvg, rcSrcBox, rcDst, xd, yd, piGuid,
		pbstrGuids, prcTag, prcAllTags, pfOpeningTag);

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	This refers to the main button, pressed without the shift key
	ENHANCE: JohnT -define whether other modifiers are don't care or excluded.
	Currently we ignore other modifiers, so they are don't care. We may want that to change,
	however. The intent is that this method basically implements the behavior with NO modifiers;
	if we implement behavior for other modifiers, we will have to decide whether to enhance
	this or implement more methods.
	Arguments:
		xd, yd				in actual drawing coords
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MouseDown(int xd, int yd, RECT rcSrc1, RECT rcDst1)
{
	BEGIN_COM_METHOD;
	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	Rect rcSrcBox;
	Rect rcDstBox;
	m_fInDrag = false;
	m_fNewSelection = false;

	if (OnMouseEvent(xd, yd, rcSrc1, rcDst1, kmeDown))
		return S_OK;

	HoldScreenGraphics hg(this);
	IVwGraphics * pvg = hg.m_qvg;

#if 99-99
	{
	StrAnsi sta;
	sta.Format("VwRootBox::MouseDown(}:            %3d/%3d%n", xd, yd);
	::OutputDebugStringA(sta.Chars());
	}
#endif

	VwBox * pboxClick = FindClosestBox(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);

	// Let that box make a selection.
	if (pboxClick)
	{
		MakeSelResult msr = pboxClick->MakeSelection(pvg, this, xd, yd, rcSrc, rcDst, rcSrcBox, rcDstBox, true);
		if (msr == kmsrMadeSel)
		{
			ShowSelection();
			m_fNewSelection = true;
		}
		// Old code to try to make editable selection on mouse down. We now do this in mouse UP.
		//else if (msr == kmsrNoSel)
		//{
		//	/*
		//	If the user clicks over a column/row intersection which is invalid for the
		//	entry, place the cursor into the first preceding column that is editable or
		//	the first following editable column (should there not be any preceding
		//	editable columns in that row).
		//	*/
		//	VwSelectionPtr qvwsel;
		//	pboxClick->GetSelection(pvg, this, xd, yd, rcSrc1, rcDst1, rcSrcBox, rcDstBox,
		//			&qvwsel);
		//	if (qvwsel)
		//	{
		//		if (qvwsel->FindClosestEditableIP(pvg, rcSrc, rcDst))
		//		{
		//			SetSelection(qvwsel);
		//			ShowSelection();
		//			m_fNewSelection = true;
		//		}
		//		else
		//		{
		//			// This makes and installs a completely new selection...let qvsel die a natural death,
		//			// don't install it.
		//			msr = pboxClick->MakeSelection(pvg, this, xd, yd, rcSrc, rcDst, rcSrcBox, rcDstBox, true);
		//			m_fNewSelection = true;
		//		}
		//	}
		//}
	}
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Find which box was clicked, or the closest box if not an exact one.
	Arguments:
		xd, yd				in actual drawing coords
----------------------------------------------------------------------------------------------*/
VwBox * VwRootBox::FindClosestBox(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
	Rect * rcSrcBox, Rect * rcDstBox)
{
	// Find the most local box where the user clicked, and the transformation used to
	// draw it.
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, rcSrcBox, rcDstBox);

	// if a box was not found then find the closest one at the same vertical position
	if (!pboxClick)
	{
		VwBox * pbox = NULL;
		int dx;
		for (dx = -16; (xd + dx) >= 0; dx -= 16)
		{
			pbox = FindBoxClicked(pvg, xd + dx, yd, rcSrc, rcDst, rcSrcBox, rcDstBox);
			if (pbox)
				break;
		}
		if (!pbox)
		{
			for (dx = 16; (xd + dx) < 1600; dx += 16)
			{
				pbox = FindBoxClicked(pvg, xd + dx, yd, rcSrc, rcDst, rcSrcBox, rcDstBox);
				if (pbox)
					break;
			}
		}
		if (pbox)
		{
			pboxClick = pbox;
		}
	}
	return pboxClick;
}

/*----------------------------------------------------------------------------------------------
	This refers to the main button, with or without the shift key.
	Other modifiers are assumed off, but we haven't really decided what to do about them yet.
	Arguments:
		xd, yd				in actual drawing coords
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MouseDblClk(int xd, int yd, RECT rcSrc1, RECT rcDst1)
{
	BEGIN_COM_METHOD;

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	Rect rcSrcBox;
	Rect rcDstBox;
	m_fNewSelection = false;
	m_fInDrag = false;
	if (OnMouseEvent(xd, yd, rcSrc1, rcDst1, kmeDblClick))
		return S_OK;

	HoldScreenGraphics hg(this);
	IVwGraphics * pvg = hg.m_qvg;
	// Find the most local box where the user clicked, and the transformation used to
	// draw it.
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);
	if (!pboxClick) //
		return S_OK;
	// Get the insertion point that would be made by a single click there.
	VwSelectionPtr qsel;
	pboxClick->GetSelection(pvg, this, xd, yd, rcSrc, rcDst, rcSrcBox, rcDstBox, &qsel);
	if (!qsel)
		return S_OK;

	VwTextSelection * pselNew = dynamic_cast<VwTextSelection *>(qsel.Ptr());
	if (pselNew && !pselNew->IsEditable(pselNew->m_ichEnd, pselNew->m_pvpbox,
		pselNew->m_fAssocPrevious))
	{
		// If the new location is not editable, we can't set a selection there!
		return S_OK;
	}
	VwTextSelection * pselOld = dynamic_cast<VwTextSelection *>(m_qvwsel.Ptr());

	if (!pselOld || !pselNew)
	{
		// If they are not both text selections we don't know how to do a word selection.
		// So just make the new selection
		SetSelection(qsel);
		if (pselNew)
			pselNew->ExpandToWord(pselNew); // m_fNewSelection already true.
	}
	else
	{
		m_fNewSelection = pselOld->ExpandToWord(pselNew);
		return S_OK;
	}

	ShowSelection();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	Called as often as the mouse moves while the main button is held down
	(whether or not shift is pressed).
	Other modifiers are assumed off, but we haven't really decided what to do about them yet.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MouseMoveDrag(int xd, int yd, RECT rcSrc1, RECT rcDst1)
{
	BEGIN_COM_METHOD;
	if (OnMouseEvent(xd, yd, rcSrc1, rcDst1, kmeMoveDrag))
		return S_OK;

	if (m_fNewSelection)
	{
		m_fInDrag = true;
		return MouseDownExtended(xd, yd, rcSrc1, rcDst1);
	}
	else
	{
		return S_OK;
	}
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Main button pressed with shift
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MouseDownExtended(int xd, int yd, RECT rcSrc1, RECT rcDst1)
{
	BEGIN_COM_METHOD;

	Rect rcSrcRoot(rcSrc1);
	Rect rcDstRoot(rcDst1);
	Rect rcSrcBox;
	Rect rcDstBox;
	if (OnMouseEvent(xd, yd, rcSrc1, rcDst1, kmeExtend))
		return S_OK;

	HoldScreenGraphics hg(this);
	IVwGraphics * pvg = hg.m_qvg;
	// Find the most local box where the user clicked, and where he clicked relative to it.
	VwBox * pboxClick = FindBoxClicked(pvg, xd, yd, rcSrcRoot, rcDstRoot,
		&rcSrcBox, &rcDstBox);
	if (!pboxClick)
		return S_OK;

	if (m_qvwsel && m_qvwsel->IsValid())
	{
		m_qvwsel->ExtendTo(pvg, this, pboxClick, xd, yd, rcSrcRoot, rcDstRoot, rcSrcBox,
			rcDstBox);
	}
	else
	{
		pboxClick->MakeSelection(pvg, this, xd, yd, rcSrcRoot, rcDstRoot, rcSrcBox,
			rcDstBox);
		// Dragging can happen during some long-drawn-out processes; abort cleanly if no longer valid.
		if (!m_qvwsel || !m_qvwsel->IsValid())
			return S_OK;
		ShowSelection();
	}
	// Dragging can happen during some long-drawn-out processes; abort cleanly if no longer valid.
	if (!m_qvwsel || !m_qvwsel->IsValid())
		return S_OK;
	// Force display to update (this is because this is typically called during mouse
	// dragging, and if we don't force the update, the selection change won't show up
	// until the user releases the mouse.
	CheckHr(m_qvrs->DoUpdates(this));

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Main button released.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::MouseUp(int xd, int yd, RECT rcSrc1, RECT rcDst1)
{
	BEGIN_COM_METHOD;

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	Rect rcSrcBox;
	Rect rcDstBox;


	if (OnMouseEvent(xd, yd, rcSrc1, rcDst1, kmeUp))
		return S_OK;

	// Don't try to do a hotlink action if there is a range selection.
	ComBool rangeSel(FALSE);
	if (m_qvwsel)
		CheckHr(m_qvwsel->get_IsRange(&rangeSel));

	VwPictureSelection * pPictureSel = dynamic_cast<VwPictureSelection *>(m_qvwsel.Ptr());
	if (pPictureSel)
	{
		// See if this is a graphic hot-link.
		VwPictureBox * pvwPicBox = dynamic_cast<VwPictureBox *>(pPictureSel->m_plbox);
		// Try to do a hot link action
		if (pvwPicBox)
			pvwPicBox->DoHotLink(pPictureSel);
	}
	if (!rangeSel)
	{
		HoldScreenGraphics hg(this);
		IVwGraphics * pvg = hg.m_qvg;

		VwBox * pboxClick = FindClosestBox(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);
		VwStringBox * psb = dynamic_cast<VwStringBox *>(pboxClick);

		// Try to do a hot link action
		if (psb)
			psb->DoHotLink(pvg, this, xd, yd, rcSrc, rcDst, rcSrcBox, rcDstBox);
	}

	bool fWasNewSelection = m_fNewSelection;

	if (m_fNewSelection)
		m_fNewSelection = false;

	//make sure we have the correct location for the end of the drag
	if (m_fInDrag)
	{
		m_fInDrag = false;
		CheckHr(MouseDownExtended(xd, yd, rcSrc1, rcDst1));
	}

	// If the end result of a click is an IP that is not editable, try to move it to a nearby
	// location that is editable.
	if ((m_fNewSelection || fWasNewSelection) && m_qvwsel)
	{
		CheckHr(m_qvwsel->get_IsRange(&rangeSel));
		VwTextSelection * psel = dynamic_cast<VwTextSelection *>(m_qvwsel.Ptr());
		if (!rangeSel && psel && !psel->IsEditable(psel->m_ichEnd, psel->m_pvpbox, psel->m_fAssocPrevious))
		{
			// Try to move a non-editable IP produced by a click to a more promising place.
			// REVIEW (TimS/EberhardB): why do we have to get the selection again? We already
			// did it in MouseDown and we don't think it could have been changed without
			// being a range selection now (in which case we wouldn't be here).
			HoldScreenGraphics hg(this);
			IVwGraphics * pvg = hg.m_qvg;
			VwBox * pboxClick = FindClosestBox(pvg, xd, yd, rcSrc, rcDst, &rcSrcBox, &rcDstBox);
			VwSelectionPtr qvwsel;
			if (pboxClick)
			{
				pboxClick->GetSelection(pvg, this, xd, yd, rcSrc1, rcDst1, rcSrcBox, rcDstBox,
					&qvwsel);
			}
			else
			{
				// if we can't find a closest box (e.g. because we ended up in a div box
				// without any child boxes, we just use the selection we already have.
				qvwsel = m_qvwsel;
			}

			if (qvwsel)
			{
				if (qvwsel->FindClosestEditableIP(pvg, rcSrc, rcDst))
				{
					SetSelection(qvwsel);
					ShowSelection();
				}
			}
		}
	}

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Notify that the containing window is being activated. Allows control of insertion point
	and range selections independantly, according to vss.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::Activate(VwSelectionState vss)
{
	BEGIN_COM_METHOD;
	if ((uint)vss >= (uint)vssLim)
		ThrowHr(WarnHr(E_INVALIDARG));

	HandleActivate(vss, true);
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	Implement the guts of the interface method Activate.
----------------------------------------------------------------------------------------------*/
void VwRootBox::HandleActivate(VwSelectionState vss, bool fSetFocus)
{

	// This is critical to be called whenever Activate() is called, so that TSF has the
	// right focus.  Missing this call to SetFocus() caused LT-5345 and LT-7488!
#ifdef ENABLE_TSF
	if (vss == vssEnabled && fSetFocus && m_qvim)
		CheckHr(m_qvim->SetFocus());
#endif /*ENABLE_TSF*/
	// Do nothing (else) if we are already in the right state. (This could produce flicker.)
	if (m_vss == vss)
		return;
#ifdef ENABLE_TSF
	if (vss != vssEnabled && m_qvim)
		CheckHr(m_qvim->KillFocus());
#endif /*ENABLE_TSF*/

	m_vss = vss;

	if (!m_qvwsel)
		return; // Nothing to do if selection is not present.

	// Turn the cursor on/off.
	if (vss == vssEnabled)
		m_qvwsel->Show();
	else if (vss == vssDisabled)
		m_qvwsel->Hide();
	else // vssOutOfFocus
	{
		if (m_qvwsel->IsInsertionPoint())
			m_qvwsel->Hide();
		else
			m_qvwsel->Show();
	}
}

/*----------------------------------------------------------------------------------------------
	Gets the current selection state
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_SelectionState(VwSelectionState * pvss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr((int*)pvss);

	*pvss = m_vss;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

// Tells whether an IME composition is in progress (e.g., a chinese character is
// partly typed). Clients should try to avoid committing changes that may destroy
// and replace the selection while this is the case, since otherwise the composition
// will be interrupted (e.g., see LT-9929)
STDMETHODIMP VwRootBox::get_IsCompositionInProgress(ComBool * pfInProgress)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfInProgress);

#ifdef ENABLE_TSF
	CheckHr(m_qvim->get_IsCompositionActive(pfInProgress));
#else
	*pfInProgress = FALSE;
#endif

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

// Tells whether this root box is running a PropChanged. We should not attempt
// to paint while this is happening, because the changes caused by expanding
// lazy boxes may conflict with the work being done by the PropChanged.
STDMETHODIMP VwRootBox::get_IsPropChangedInProgress(ComBool * pfInProgress)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfInProgress);

	*pfInProgress = m_fIsPropChangedInProgress;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}
/*----------------------------------------------------------------------------------------------
	Discard all your notifiers. In case this happens during a sequence of PropChanged calls,
	mark them all as deleted.
----------------------------------------------------------------------------------------------*/
void VwRootBox::ClearNotifiers()
{
	for (int i = 0; i < m_vpanote.Size(); i++)
		m_vpanote[i]->_KeyBox(NULL);
	m_vpanote.Clear();
	ObjNoteMap::iterator itLim = m_mmhvoqnote.End();
	ObjNoteMap::iterator it = m_mmhvoqnote.Begin();
	for (; it != itLim; ++it)
	{
		(*it)->_KeyBox(NULL);
	}
	m_mmhvoqnote.Clear();
	m_mmboxqnote.Clear();
}

/*----------------------------------------------------------------------------------------------
	Clean out everything and rebuild the view from scratch. Selections are lost. This is a last
	resort if some property changed and we are not sure what the consequences should be.

	@param fCheckForSync True to do synchronization if it is needed; false to force the
						reconstruct only on this one VwRootBox.
----------------------------------------------------------------------------------------------*/
void VwRootBox::Reconstruct(bool fCheckForSync)
{
	if (m_qsync && fCheckForSync)
	{
		m_qsync->Reconstruct();
		return;
	}

	int dxAvailWidth;
	// Keep these intact.
	// m_qvrs.Clear();
	// m_qss.Clear();
	// m_vqvwvc.Clear();
	// m_vhvo.Clear();
	// m_vfrag.Clear();
	// m_qsda.Clear();

	CheckHr(DestroySelection());

	ClearNotifiers();
	NotifierVec vpanoteDelDummy; // required argument, but all gone already.

	DeleteContents(this, vpanoteDelDummy);

	CheckHr(m_qvrs->GetAvailWidth(this, &dxAvailWidth));
	HoldLayoutGraphics hg(this);
	// We need to invalidate both old and new rectangles, so remember the old one.
	Rect vwrect = GetInvalidateRect();
	// It is safest to check both Height() and FieldHeight(). Occasionally FieldHeight
	// may not detect a change, for example, when a style definition has changed before
	// this routine is called, and that is all that affects the height.
	int dyOld2 = Height();
	int dyOld;
	if (m_fConstructed)
		dyOld = FieldHeight();
	else
		dyOld = 0;

	int dxOld = Width();
	for (int i = 0; i < m_chvoRoot; i++)
	{
		ComBool fValid = true;
		if (m_vhvo[i] != 0) // allow zero, many VCs can cope with this as a special case for missing objects.
			CheckHr(m_qsda->get_IsValidObject(m_vhvo[i], &fValid));
		if (!fValid)
		{
			// Reconstructing, but object has been deleted!
			m_vhvo[i] = 0; // treat as null object, hope view constructor copes!
		}
	}
	Construct(hg.m_qvg, dxAvailWidth);

	Layout(hg.m_qvg, dxAvailWidth);
	if (dyOld != FieldHeight() || dxOld != Width() || dyOld2 != Height())
		CheckHr(m_qvrs->RootBoxSizeChanged(this));

	Invalidate(); // new
	InvalidateRect(&vwrect); //old
}

/*----------------------------------------------------------------------------------------------
	Clean out everything and rebuild the view from scratch. Selections are lost. This is a last
	resort if some property changed and we are not sure what the consequences should be.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::Reconstruct()
{
	BEGIN_COM_METHOD;

	// We always want to do syncronized reconstruction if we call this version of Reconstruct
	Reconstruct(true);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	When the stylesheet changes, redraw the boxes based on the style changes.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::OnStylesheetChange()
{
	BEGIN_COM_METHOD;

	if (!m_fConstructed || Style() == NULL)
		return S_OK;  // no Style() object exists to fix. (I think the second condition above is redundant, but play safe.)
	// Redraw the boxes based on stylesheet changes.

	Style()->InitRootTextProps(m_vqvwvc.Size() == 0 ? NULL : m_vqvwvc[0]);
	Style()->RecomputeEffects();
	LayoutFull();
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Check if there have been any non-fatal errors that occurred in the process of drawing.
	Return an error result if we found any, S_OK if none.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::DrawingErrors(IVwGraphics * pvg)
{
	BEGIN_COM_METHOD;

	ChkComArgPtrN(pvg);

	HRESULT hr;

	IgnoreHr(hr = Style()->DrawingErrors(pvg));
	if (FAILED(hr))
		return hr;

	// Reset error value after retrieving it so that we don't report it multiple times.
	hr = m_hrSegmentError;
	m_hrSegmentError = S_OK;

	if (FAILED(hr))
	{
		// Set the error info with the saved error message so that we can get to it from C#.
		// We don't have the original IID and help id and help file, so we just use defaults.
		StackDumper::RecordError(IID_IVwRootBox, m_stuSegmentError, L"", 0, L"");
	}
	return hr;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Get the style sheet
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_Stylesheet(IVwStylesheet ** ppvss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvss);

	*ppvss = m_qss;
	AddRefObj(*ppvss);

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Modify the column widths of every top-level table in the view. "Top-level" means we will
	go down into div boxes but not other types.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::SetTableColWidths(VwLength * prgvlen, int cvlen)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgvlen, cvlen);

	if (!m_fConstructed)
		return S_OK;

	for (VwBox * pbox = FirstBox(); pbox; )
	{
		VwTableBox * ptable = dynamic_cast<VwTableBox *>(pbox);
		if (ptable)
		{
			ptable->SetTableColWidths(prgvlen, cvlen);
		}
		VwDivBox * pdbox = dynamic_cast<VwDivBox *>(pbox);
		if (pdbox)
			pbox = pbox->NextInRootSeq(false); // recurse into it
		else
		{
			// Current box isn't a div. We don't look into it; typically we
			// just move to the next box in the div. If there are no more in
			// this div (NextOrLazy is null), we need to move up to the container...
			// which is a div we've finished...and on from there...or if there are
			// more layers of division, keep moving up until one has a next box.
			// When none does, we are done.
			while (pbox && !pbox->NextOrLazy())
				pbox = pbox->Container();
			if (pbox)
				pbox = pbox->NextOrLazy();
		}
	}
	LayoutFull();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

//:>********************************************************************************************
//:>	Window drawing.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Call this before drawing or printing. (Its purpose is to fully evaluate any parts of
	the display that have not been fully generated before trying to draw them.
	Calling it may be omitted if the view constructor does not use laziness.)
	It returns a boolean indicating whether the preparation process forced a change in scroll
	position that caused the window to be invalidated. If so, typically the drawing should be
	aborted (and will be redone due to the invalidate).
	@param rcSrcRoot/rcDstRoot See ${DrawRoot}.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::PrepareToDraw(IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
	VwPrepDrawResult * pxpdr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvg);

	if (pxpdr == NULL)
		return E_UNEXPECTED;

	*pxpdr = kxpdrNormal; // in case of exception thrown
	*pxpdr = VwDivBox::PrepareToDraw(pvg, rcSrc, rcDst);
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

void PushClipRect(Vector<Rect> & vrect, IVwGraphics * pvg)
{
	RECT clip;
	int left, top, right, bottom;
	CheckHr(pvg->GetClipRect(&left, &top, &right, &bottom));
	clip.left = left;
	clip.right = right;
	clip.top = top;
	clip.bottom = bottom;
	vrect.Push(clip);
}

/*----------------------------------------------------------------------------------------------
	Draw the contents of the box, at least that part of them which intersect the clip rect
	of the VwGraphics object. The root box will be drawn with its top left corner at (0,0)
	in the VwGraphics coordinate system. To put the box elsewhere in the window (whether for
	layout or scrolling purposes), adjust the VwGraphics object's origin.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::DrawRoot(IVwGraphics * pvg, RECT rcSrcRoot1, RECT rcDstRoot1,
	ComBool fDrawSel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	if (m_fLocked)
	{
		PushClipRect(m_vrectSkippedPaints, pvg);
		return S_OK;
	}

	// Because we typically show the selection by inverting, we need to turn that off before
	// redrawing the underlying, non-inverted text, otherwise we can confuse whether it is
	// currently inverted or not.
	// ENHANCE: JohnT: could this lead to an infinite loop if turning the selection off
	// involves repainting instead of inversion? (But we don't do it that way in any
	// current renderer.)
	Rect rcSrcRoot(rcSrcRoot1);
	Rect rcDstRoot(rcDstRoot1);
#if DIRECT_DRAW
	// Something roughly like this is needed if we are NOT using double buffering.
	// Clipping may affect part of the selection but not other parts, and the inversion
	// commonly used for selections does not respect clipping. The only way to get
	// the right effect if we are drawing directly on the screen is to turn the selection
	// off, then draw, then turn on again.
	// ENHANCE JohnT: if we ever use a combination of double-buffered and direct drawing,
	// we probably need a parameter indicating which is being used.
	if (m_qvwsel && m_vss == vssEnabled) // Need to show again unless already disabled.
		m_qvwsel->Hide();
#endif
	Draw(pvg, rcSrcRoot, rcDstRoot);
#if DIRECT_DRAW
	if (m_qvwsel && m_vss == vssEnabled)
		m_qvwsel->ReallyShow(pvg, rcSrcRoot, rcDstRoot);
#else
	if (m_qvwsel && fDrawSel)
		m_qvwsel->DrawIfShowing(pvg, rcSrcRoot, rcDstRoot, -1, INT_MAX);
#endif

	// If any kind of scrolling has occurred, try to increase laziness. This might be
	// because we scrolled, because we typed a lot, because we set a selection from a find
	// or from another window, or whatever.
	if (m_ydTopLastDraw != rcDstRoot.TopLeft().y)
	{
		m_ydTopLastDraw = rcDstRoot.TopLeft().y;
		MaximizeLaziness();
	}

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Draw the contents of the box, at least that part of them which intersect the clip rect
	of the VwGraphics object. The root box will be drawn with its top left corner at (0,0)
	in the VwGraphics coordinate system. To put the box elsewhere in the window (whether for
	layout or scrolling purposes), adjust the VwGraphics object's origin.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::DrawRoot2(IVwGraphics * pvg, RECT rcSrcRoot1, RECT rcDstRoot1,
	ComBool fDrawSel, int ysTop, int dysHeight)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	if (m_fLocked)
	{
		PushClipRect(m_vrectSkippedPaints, pvg);
		return S_OK;
	}

	Rect rcSrcRoot(rcSrcRoot1);
	Rect rcDstRoot(rcDstRoot1);

	Draw(pvg, rcSrcRoot, rcDstRoot, ysTop, dysHeight);
	if (m_qvwsel && fDrawSel)
		m_qvwsel->DrawIfShowing(pvg, rcSrcRoot, rcDstRoot, ysTop, dysHeight);

	// If any kind of scrolling has occurred, try to increase laziness. This might be
	// because we scrolled, because we typed a lot, because we set a selection from a find
	// or from another window, or whatever.
	if (m_ydTopLastDraw != rcDstRoot.TopLeft().y)
	{
		m_ydTopLastDraw = rcDstRoot.TopLeft().y;
		MaximizeLaziness();
	}

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Lay the box out in the available width. Must be called before Draw, Height, or Width
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::Layout(IVwGraphics * pvg, int dxAvailWidth)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	int dpiX, dpiY;
	CheckHr(pvg->get_XUnitsPerInch(&dpiX));
	CheckHr(pvg->get_YUnitsPerInch(&dpiY));
	m_ptDpiSrc.x = dpiX;
	m_ptDpiSrc.y = dpiY;

	//AssertNotifiersValid(); // This is a error checking function related to TE-2962
	if (!m_fConstructed)
		Construct(pvg, dxAvailWidth);
	VwDivBox::DoLayout(pvg, dxAvailWidth, -1, true);
#ifdef ENABLE_TSF
	if (m_qvim)
		CheckHr(m_qvim->OnLayoutChange());
#endif /*ENABLE_TSF*/

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	The height (in twips) needed to draw the box
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_Height(int * ptwHeight)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ptwHeight);

	if (!EnsureConstructed(true))
			return 16; // something arbitrary, can't produce a useful value.

	*ptwHeight = static_cast<VwDivBox *>(this)->FieldHeight();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	The width (in twips) needed to draw the box
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_Width(int * ptwWidth)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ptwWidth);

	if (!EnsureConstructed(true))
			return 100; // something arbitrary, can't produce a useful value.

	*ptwWidth = static_cast<VwDivBox *>(this)->Width();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

#ifdef DEBUG
/*----------------------------------------------------------------------------------------------
	This is a error checking function related to TE-2962
-----------------------------------------------------------------------------------------------*/
void VwRootBox::AssertNotifiersValid()
{
	ObjNoteMap::iterator itLim = m_mmhvoqnote.End();
	ObjNoteMap::iterator it = m_mmhvoqnote.Begin();
	for (; it != itLim; ++it)
	{
		(*it)->AssertValid();
	}
}
#endif

/*----------------------------------------------------------------------------------------------
	Convert a TsString that may contain special header and footer escapes into a literal
	string.
	ENHANCE JohnT: enhance to handle #pages, title, time
----------------------------------------------------------------------------------------------*/
void VwRootBox::ProcessHeaderSpecials(ITsString * ptss, ITsString ** pptssRet, int nPageNo,
	int nPageTotal)
{
	if (!ptss)
	{
		return;
	}
	AssertPtr(ptss);
	AssertPtr(pptssRet);
	Assert(!*pptssRet);

	const OLECHAR * pchString;
	int cch;
	CheckHr(ptss->LockText(&pchString, &cch));
	if (!cch)
	{
		// Don't make a string at all if empty.
		ptss->UnlockText(pchString);
		return;
	}
	ITsStringPtr qtss = ptss;
	try
	{
		bool fMatch = false;
		ITsStrBldrPtr qtsb;
		do
		{
			fMatch = false; // reset each iteration. We only repeat if it is true.
#ifndef WIN32
			static OleStringLiteral page(L"&[page]");
			const OLECHAR * pchSpecial = u_strstr(pchString, page);
#else
			const OLECHAR * pchSpecial = wcsstr(pchString, L"&[page]");
#endif
			OLECHAR buf[200];
			int cchSpecial = 0;
			if (pchSpecial)
			{
				fMatch = true;
				_itow_s(nPageNo,buf,isizeof(buf)/isizeof(OLECHAR),10);	// ENHANCE JohnT: locale-dependent conversion
				cchSpecial = 7;
			}
			else
			{
				static OleStringLiteral date(L"&[date]");
				pchSpecial = u_strstr(pchString, date);
				if (pchSpecial)
				{
					fMatch = true;
					// ENHANCE (Mac portability): get the date string some portable way...
#if WIN32
					TCHAR buf2[200];
					SYSTEMTIME stim;
					::GetSystemTime(&stim);
					::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, buf2, 200);
#else //WIN32
					time_t t;
					tm *stim;
					stim = gmtime(&t);
					char buf2[50];
					char buf3[50];
					strftime(buf2, 50, "%x", stim);
					strftime(buf3, 50, "%Y", stim);
					strncat(buf2, buf2, 6);
					strcat(buf2, buf3);
#endif //WIN32
					StrUni stuConv = buf2;
					wcscpy_s(buf, 200, stuConv.Chars());
					cchSpecial = 7; // delete 7 chars for &[date]
				}
				else
				{
					static OleStringLiteral time(L"&[time]");
					pchSpecial = u_strstr(pchString, time);
					if (pchSpecial)
					{
						fMatch = true;
						// ENHANCE (Mac portability): get the time string some portable way...
#if WIN32
						TCHAR buf2[200];
						SYSTEMTIME stim;
						::GetLocalTime(&stim);
						::GetTimeFormat(LOCALE_USER_DEFAULT, 0, &stim, NULL, buf2, 200);
#else //WIN32
						time_t t;
						tm *stim;
						stim = gmtime(&t);
						char buf2[100];
						strftime(buf2, 12, "%X %p", stim);
#endif //WIN32
						StrUni stuConv = buf2;
						wcscpy_s(buf, 200, stuConv.Chars());
						cchSpecial = 7; // delete 7 chars for &[time]
					}
					else
					{
						static OleStringLiteral pages(L"&[pages]");
						pchSpecial = u_strstr(pchString, pages);
						if (pchSpecial)
						{
							fMatch = true;
							_itow_s(nPageTotal,buf,isizeof(buf)/isizeof(OLECHAR),10);	// ENHANCE JohnT: locale-dependent conversion

							cchSpecial = 7;
							cchSpecial = 8; // delete 7 chars for &[pages]
						}
					}
				}
			}
			if (fMatch)
			{
				if (!qtsb)
					CheckHr(ptss->GetBldr(&qtsb));
				int cchBuf = u_strlen(buf);
				ITsTextPropsPtr qttp;
				TsRunInfo tri;
				int ichSpecial = pchSpecial - pchString;
				CheckHr(qtss->FetchRunInfoAt(pchSpecial - pchString, &tri, &qttp));
				CheckHr(qtsb->ReplaceRgch(ichSpecial, ichSpecial + cchSpecial, buf, cchBuf, qttp));
				qtss->UnlockText(pchString);
				pchString = NULL;
				qtsb->GetString(&qtss);
				qtss->LockText(&pchString, &cch);
			}
		} while (fMatch);
	}
	catch (...)
	{
		if (pchString)
			CheckHr(qtss->UnlockText(pchString));
		throw;
	}
	if (pchString)
		CheckHr(qtss->UnlockText(pchString));
	*pptssRet = qtss.Detach();
}


/*----------------------------------------------------------------------------------------------
	Print a single header string. ipos is 0-5 where 0 = left, 1 = middle, 2 = right, +3 for
	bottom.
----------------------------------------------------------------------------------------------*/
void VwRootBox::PrintHeader(VwPrintInfo * pvpi, ISilDataAccess * psda,
	ITsString * ptss, int ipos, int dxsAvailWidth)
{
	if (!ptss)
		return;
	VwParagraphBox * pvpbox = NULL;
	VwParagraphBox * pvpboxOuter = NULL;
	// It's important to use a smart pointer here, even though we will positively be done with
	// this object at the end of this method, because we set its SilDataAccess, and that involves
	// telling the SDA to notify the root box of changes. The SDA may be a managed code object,
	// which means an RCW gets created. The root box cannot safely go away until the RCW gets
	// garbage collection
	VwRootBoxPtr qrootb;
	try
	{
		pvpbox = NewObj VwParagraphBox(m_qzvps); // to contain the text
		pvpboxOuter = NewObj VwParagraphBox(m_qzvps);
		qrootb.Attach(NewObj VwRootBox(m_qzvps));
		qrootb->putref_DataAccess(psda);
		// Put the inner paragraph inside an outer one. This causes its width to be the actual width
		// of the string, not the available width.
		pvpbox->Container(pvpboxOuter);
		pvpboxOuter->Container(qrootb);
		pvpbox->Source()->AddString(ptss, m_qzvps, NULL);
		pvpbox->DoLayout(pvpi->m_pvg, dxsAvailWidth);
		Rect rcSrc, rcDst;

		int dxdLeftMargin, dxdRightMargin, dydHeaderMargin, dydTopMargin, dydBottomMargin, dydFooterMargin;
		CheckHr(pvpi->m_pvpc->GetMargins(&dxdLeftMargin, &dxdRightMargin, &dydHeaderMargin, &dydTopMargin,
			&dydBottomMargin, &dydFooterMargin));

		// Non-view code is responsible to adjust the margins for the unprintable
		// part of the page. Therefore assume margins we have are relative to pvpi->m_rcPage.
		// ENHANCE JohnT: if we support zoom factor, convert here from source to dest.
		int dxdWidth = pvpbox->Width();
		int dydHeight = pvpbox->Height();
		rcSrc.left = rcSrc.top = 0; // Plan to do all positioning using the Dst rect.
		if (ipos >= 3)
		{
			// bottom of page
			rcDst.top = pvpi->m_rcPage.bottom - dydHeight - dydFooterMargin;
		}
		else
		{
			// top of page
			rcDst.top = pvpi->m_rcPage.top + dydHeaderMargin;
		}
		if (ipos % 3 == 0)
		{
			// left
			rcDst.left = pvpi->m_rcPage.left + dxdLeftMargin;
		}
		else if (ipos % 3 == 1)
		{
			// center
			rcDst.left = pvpi->m_rcPage.left + dxdLeftMargin +
				(pvpi->m_rcPage.Width() - dxdLeftMargin - dxdRightMargin) / 2 -
				dxdWidth / 2;
		}
		else
		{
			// right
			rcDst.left = pvpi->m_rcPage.right - dxdRightMargin - dxdWidth;
		}
		// Set resolution fields in src and dst rectangles.
		rcSrc.right = rcSrc.left + pvpi->m_dxpInch;
		rcDst.right = rcDst.left + pvpi->m_dxpInch;
		rcSrc.bottom = rcSrc.top + pvpi->m_dypInch;
		rcDst.bottom = rcDst.top + pvpi->m_dypInch;
		// Do the actual drawing
		pvpbox->Draw(pvpi->m_pvg, rcSrc, rcDst);
	}
	catch(...)
	{
		if (qrootb)
			qrootb->Close();
		if (pvpboxOuter)
			delete pvpboxOuter;
		if (pvpbox)
		{
			NotifierVec vpanoteDelDummy; // required argument, but all gone already.
			pvpbox->DeleteContents(NULL, vpanoteDelDummy);
			delete pvpbox;
		}
		throw;
	}
	NotifierVec vpanoteDelDummy; // required argument, but all gone already.
	pvpbox->DeleteContents(NULL, vpanoteDelDummy);

	qrootb->Close();
	delete pvpbox;
	delete pvpboxOuter;
}

/*----------------------------------------------------------------------------------------------
	Print all the headers that the print context says are wanted on page nPageNo.
----------------------------------------------------------------------------------------------*/
void VwRootBox::PrintHeaders(IVwPrintContext * pvpc, ISilDataAccess * psda, VwPrintInfo * pvpi,
	bool fFirst)
{
	int nPageNo = pvpi->m_nPageNo;
	int nPageTotal = pvpi->m_nPageTotal;
	int dxsAvailWidth = pvpi->m_rcDoc.Width();
	// Loop over the six possible header positions.
	// 0, 1, 2 = left, center, right;
	// +3 for bottom.
	ITsStringPtr rgqtss[6];
	int ipos;
	for (ipos = 0; ipos < 6; ipos++)
	{
		// Really a VwHeaderPositions, but can't do |= on enumerations.
		int grfvhp = (ipos >= 3) ? kvhpBottom : kvhpTop;
		if (fFirst) grfvhp |= kvhpFirst;
		bool fOdd = nPageNo % 2;
		if (fOdd)
			grfvhp |= kvhpOdd;
		else
			grfvhp |= kvhpEven;
		if (ipos == 0 || ipos == 3)
		{
			grfvhp |= kvhpLeft;
			if (fOdd)
				// Odd pages are conventionally right-hand pages, so this is the inside.
				grfvhp |= kvhpInside;
			else
				grfvhp |= kvhpOutside;
		}
		else if (ipos == 1 || ipos == 4)
		{
			grfvhp |= kvhpCenter;
		}
		else
		{
			grfvhp |= kvhpRight;
			if (fOdd)
				grfvhp |= kvhpOutside;
			else
				grfvhp |= kvhpInside;
		}
		ITsStringPtr qtss;
		pvpc->get_HeaderString((VwHeaderPositions)grfvhp, nPageNo, &qtss);
		ProcessHeaderSpecials(qtss, &rgqtss[ipos], nPageNo, nPageTotal);
	}

	// Now we have all the strings we can figure available widths.
	int cposTop = 0;
	for (ipos = 0; ipos < 3; ipos++)
	{
		if (rgqtss[ipos])
			cposTop++;
	}
	int cposBottom = 0;
	for (ipos = 3; ipos < 6; ipos++)
	{
		if (rgqtss[ipos])
			cposBottom++;
	}

	if (cposTop)
	{
		// Some headings to print at the top. Divide the available space in 1, 2, or 3,
		// depending on how many pieces we have to print...except that if we have Center
		// plus one other, we have to treat it as three pieces.
		if (cposTop == 2 && rgqtss[1])
			cposTop = 3;
		int dxpTopWidth = dxsAvailWidth / cposTop;
		for (int i = 0; i < 3; i++)
		{
			PrintHeader(pvpi, psda, rgqtss[i], i, dxpTopWidth);
		}
	}

	if (cposBottom)
	{
		// Some headings to print at the bottom. Divide the available space in 1, 2, or 3,
		// depending on how many pieces we have to print...except that if we have Center
		// plus one other, we have to treat it as three pieces.
		if (cposBottom == 2 && rgqtss[4])
			cposBottom = 3;
		int dxpBottomWidth = dxsAvailWidth / cposBottom;
		for (int i = 3; i < 6; i++)
		{
			PrintHeader(pvpi, psda, rgqtss[i], i, dxpBottomWidth);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Get total number of pages for a printed view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::InitializePrinting(IVwPrintContext * pvpc)
{
	BEGIN_COM_METHOD;

	VwPrintInfo vpi;
	CreatePrintInfo(pvpc, vpi);

	int dxsAvailWidth = vpi.m_rcDoc.Width();
	if (dxsAvailWidth <= 0)
	{
		// Put out a temporary error message if it fails. Eventually we should do better.
		StrUni stu;
		stu.Format(L"No room to print: left = %d, right = %d, top = %d, bottom = %d "
			L"width = %d", vpi.m_rcDoc.left, vpi.m_rcDoc.right, vpi.m_rcDoc.top,
			vpi.m_rcDoc.bottom, dxsAvailWidth);
#if WIN32
		::MessageBox(NULL, stu.Chars(), L"Initializing Printing", MB_OK);
#else
		// TODO-Linux: I don't think this is even used on Linux.
		printf("Warning: Initializing Printing: No room to print.\n");
		fflush(stdout);
#endif
		ThrowHr(WarnHr(E_FAIL));
	}

	// To make this algorithm work reliably we need to expand all lazy boxes.
	// This is needed, for example, to ensure that when we ask whether the root box fits
	// entirely on a page, we get an accurate answer. Since the first pass measures all
	// the pages to determine how many there are, we are going to have to expand
	// everything anyhow.
	// Optimization: possibly we could avoid this if we don't need the total page count?
	// Possibly we could do it progressively somehow if short of memory?
	Construct(vpi.m_pvg, dxsAvailWidth); // Get the initial boxes (possibly lazy)
	ExpandFully(); // Get rid of lazy ones
	CheckHr(Layout(vpi.m_pvg, dxsAvailWidth));

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Get total number of pages for a printed view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::GetTotalPrintPages(IVwPrintContext * pvpc, int *pcPageTotal)
{
	BEGIN_COM_METHOD;

	ChkComOutPtr(pcPageTotal);

	VwPrintInfo vpi;
	CreatePrintInfo(pvpc, vpi);

	Rect rcSrc;
	Rect rcDst;
	GetResInfo(vpi, rcSrc, rcDst);

	// Figure total number of pages.
	// OPTIMIZE JohnT: eventually we may want to skip this if we can determine that
	// no header or footer uses it. This is not important until layout is lazy.
	int ysStartPage = ChooseSecondIfInverted(0, Bottom());
	int ysEndDoc = ChooseSecondIfInverted(Bottom(), 0);
	int ysEnd = 0;
	int nPageTotal = 0;

	for (; IsVerticallyAfter(ysEndDoc, ysStartPage); nPageTotal++, ysStartPage = ysEnd)
	{
		rcSrc.top = ysStartPage;
		rcSrc.bottom = ysStartPage + vpi.m_dypInch;
		FindBreak(&vpi, rcSrc, rcDst, ysStartPage, &ysEnd);
		Assert(IsVerticallyAfter(ysEnd, ysStartPage)); // We need to make some progress!
	}

	*pcPageTotal = nPageTotal;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Initialize a print info object.
----------------------------------------------------------------------------------------------*/
void VwRootBox::CreatePrintInfo(IVwPrintContext * pvpc, VwPrintInfo & vpi)
{
	IVwGraphicsPtr qvg;
	CheckHr(pvpc->get_Graphics(&qvg));
	if (!qvg)
		ThrowHr(WarnHr(E_FAIL));

	int dxpLeft, dxpRight, dypHeader, dypTop, dypBottom, dypFooter;

	CheckHr(pvpc->GetMargins(&dxpLeft, &dxpRight, &dypHeader, &dypTop,
		&dypBottom, &dypFooter));

	vpi.m_pvg = qvg;
	int left, top, right, bottom;

	CheckHr(qvg->GetClipRect(&left, &top, &right, &bottom));
	vpi.m_pvpc = pvpc;
	vpi.m_rcPage.left = left;
	vpi.m_rcPage.right = right;
	vpi.m_rcPage.top = top;
	vpi.m_rcPage.bottom = bottom;
#if !WIN32
	qvg->put_XUnitsPerInch(72);
	qvg->put_YUnitsPerInch(72);
#endif
	CheckHr(qvg->get_XUnitsPerInch(&vpi.m_dxpInch));
	CheckHr(qvg->get_YUnitsPerInch(&vpi.m_dypInch));
	vpi.m_rcDoc = vpi.m_rcPage;
	vpi.m_rcDoc.left += dxpLeft;
	vpi.m_rcDoc.right -= dxpRight;
	vpi.m_rcDoc.top += dypTop;
	vpi.m_rcDoc.bottom -= dypBottom;
}

/*----------------------------------------------------------------------------------------------
	Set resolution information in the supplied rectangles.
----------------------------------------------------------------------------------------------*/
void VwRootBox::GetResInfo(VwPrintInfo & vpi, Rect & rcSrc, Rect & rcDst)
{
	rcSrc.left = rcSrc.top = 0;
	rcDst.left = vpi.m_rcDoc.left;
	rcDst.top = ChooseSecondIfInverted(vpi.m_rcDoc.top, vpi.m_rcDoc.bottom);

	// Set resolution fields in src and dst rectangles.
	rcSrc.right = rcSrc.left + vpi.m_dxpInch;
	rcDst.right = rcDst.left + vpi.m_dxpInch;
	rcSrc.bottom = rcSrc.top + vpi.m_dypInch;
	rcDst.bottom = rcDst.top + vpi.m_dypInch;
}

/*----------------------------------------------------------------------------------------------
	Print a single page, given the specified page number. (Note: InitializePrinting must be
	called first.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::PrintSinglePage(IVwPrintContext * pvpc, int nPageNo)
{
	BEGIN_COM_METHOD;

	// TODO: Arrange for the total number of pages to get set in the vpi. (This isn't necessary
	// yet until applications that call this method support printing headers/footers.)
	VwPrintInfo vpi;
	CreatePrintInfo(pvpc, vpi);

	Rect rcSrc;
	Rect rcDst;
	GetResInfo(vpi, rcSrc, rcDst);

	int nPageFirst;
	CheckHr(pvpc->get_FirstPageNumber(&nPageFirst));

	// Find the top of the page we want to print.
	int ysStartPage = ChooseSecondIfInverted(0, Bottom());
	int ysEndDoc = ChooseSecondIfInverted(Bottom(), 0);
	int ysEnd = 0;
	int iPage = nPageFirst;

	for (; IsVerticallyAfter(ysEndDoc, ysStartPage) && iPage != nPageNo; iPage++, ysStartPage = ysEnd)
	{
		rcSrc.top = ysStartPage;
		rcSrc.bottom = ysStartPage + vpi.m_dypInch;
		FindBreak(&vpi, rcSrc, rcDst, ysStartPage, &ysEnd);
		Assert(IsVerticallyAfter(ysEnd, ysStartPage)); // We need to make some progress!
	}

	// Figure the end of this page so we know what to print.
	rcSrc.top = ysStartPage;
	rcSrc.bottom = ysStartPage + vpi.m_dypInch;
	FindBreak(&vpi, rcSrc, rcDst, ysStartPage, &ysEnd);

	PrintHeaders(pvpc, m_qsda, &vpi, vpi.m_nPageNo == nPageFirst);
	vpi.m_pvg->PushClipRect(vpi.m_rcDoc);
	PrintPage(&vpi, rcSrc, rcDst, ysStartPage, ysEnd);
	vpi.m_pvg->PopClipRect();

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Find the best (last) place to put a page break in yourself after ysStart.
	Make a "nice" break if possible, otherwise, fall back progressively to disregarding
	KeepWithNext and KeepTogether, and ultimately even keeping boxes undivided.
	Otherwise arguments are as for FindNiceBreak.
----------------------------------------------------------------------------------------------*/
void VwRootBox::FindBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
	int ysStart, int * pysEnd)
{
	// If we fit entirely, no need to seek a break
	int ydEnd = rcSrc.MapYTo(TrailingPrintEdge(this, rcSrc.Height()), rcDst);
	if (!IsVerticallySameOrAfter(ydEnd, TrailingEdge(pvpi->m_rcDoc))) //ydEnd <= pvpi->m_rcDoc.bottom)
	{
		*pysEnd = ChooseSecondIfInverted(VisibleBottom(), Top());
		return;
	}

	if (FindNiceBreak(pvpi, rcSrc, rcDst, ysStart, pysEnd, false))
		return;

	// Failing that, try again with no "keep" rules.
	if (FindNiceBreak(pvpi, rcSrc, rcDst, ysStart, pysEnd, true))
		return;

	// Otherwise, just print a pageful.
	*pysEnd = rcDst.MapYTo(TrailingEdge(pvpi->m_rcDoc), rcSrc);
}

//:>********************************************************************************************
//:>	Store and retrieve containing window.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Retrieve the root container.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_Site(IVwRootSite ** ppvrs)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvrs);

	AddRefObj(m_qvrs.Ptr());
	*ppvrs = m_qvrs;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Store the root container.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::put_Site(IVwRootSite * pvrs)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvrs);

	m_qvrs = pvrs;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

//:>********************************************************************************************
//:>	Handle focus
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Focus loss: return false to indicate that focus loss is vetoed. The caller should then
	SetFocus back to this window.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::LoseFocus(ComBool * pfOk)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfOk);

	*pfOk = true;

	// If we don't have a selection, losing focus is fine!
	if (!m_qvwsel)
		return S_OK;

	// Otherwise let the selection decide.
	m_qvwsel->LoseFocus(NULL, pfOk);
	m_fDirty = false;

	// Note: should we veto on error or not? Most likely something is badly wrong,
	// perhaps even a bug, so it seems best not to lock the user into this field.
	// Hence we go ahead with normal return (*pfOk == true) even if we catch an error.

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


//:>********************************************************************************************
//:>	Miscellaneous methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	The root box is closing. It should release all referernce counts, especially the
	reference to its site, to facilitate breaking cycles.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::Close()
{
	BEGIN_COM_METHOD;

	m_qvrs.Clear();
	ClearNotifiers();
	m_qvwsel.Clear();
	m_qss.Clear();
	m_vqvwvc.Clear();
	m_vhvo.Clear();
	m_vfrag.Clear();
	if (m_qsda)
	{
		CheckHr(m_qsda->RemoveNotification(this));
		m_qsda.Clear();
	}
	m_qsync.Clear();

#ifdef ENABLE_TSF
	// m_qvim gets created in the c'tor, so one could think of destroying it in the
	// d'tor. However, because the view manager holds a smart pointer to us (VwRootBox) this
	// would result in the RootBox not being deleted because the reference count is one off.
	if (m_qvim)		// In case this method is called twice (which can happen).
	{
		CheckHr(m_qvim->Close());	// Clear out any internal smart pointers.
		m_qvim.Clear();
	}
#endif /*ENABLE_TSF*/
	// All our selections become invalid (and therefore no longer hold a reference count to
	// this).
	for (int i = 0; i < m_vselInUse.Size(); i++)
		m_vselInUse[i]->MarkInvalid();
	m_vselInUse.Clear();

	ClearNotifiers();
	NotifierVec vpanoteDelDummy; // required argument, but all gone already.
	DeleteContents(this, vpanoteDelDummy);

	m_fConstructed = false;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Notify the caller whether or not the selection has changed at some point.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::IsDirty(ComBool * pfDirty)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfDirty);

	*pfDirty = m_fDirty;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Get the horizontal position for moving to the next (or previous) field.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_XdPos(int * pxdPos)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pxdPos);

	*pxdPos = m_xdPos;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Get the maximum number of paragraphs to scan while looking for an editable insertion point.
	Make sure that procedures that use this limit do not move the cursor from editable to
	non-editable field.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::get_MaxParasToScan(int * pcParas)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcParas);

	*pcParas = m_cMaxParasToScan;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Set the maximum number of paragraphs to scan while looking for an editable insertion point.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::put_MaxParasToScan(int cParas)
{
	BEGIN_COM_METHOD;

	m_cMaxParasToScan = cParas;

	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Return true if everything (except contents
	of lazy boxes that have not been expanded) has been checked. Same result as DoSpellCheckStep(),
	but does not do any checking; should be VERY fast, enough to have no significant impact
	when called on every paint, for example.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::IsSpellCheckComplete(ComBool * pfComplete)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfComplete);
	*pfComplete = m_fCompletedSpellCheck;
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Do a step of spell-checking the view. Return true if everything (except contents
	of lazy boxes that have not been expanded) has been checked. One call should be short
	enough to be performed during idle time without significant impact.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::DoSpellCheckStep(ComBool * pfComplete)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfComplete);
	if (m_fCompletedSpellCheck)
	{
		*pfComplete = true;
		return S_OK;
	}
	if (m_fLocked)
	{
		return S_OK;
	}
	VwBox * pboxTarget;
	if (m_pvpboxNextSpellCheck == NULL)
	{
		pboxTarget = FirstBox();
	}
	else
	{
		m_pvpboxNextSpellCheck->SpellCheck();
		pboxTarget = m_pvpboxNextSpellCheck->NextInRootSeq(false, NULL, true);
	}
	while (pboxTarget != NULL && dynamic_cast<VwParagraphBox *>(pboxTarget) == NULL)
		pboxTarget = pboxTarget->NextInRootSeq(false, NULL, true);
	m_pvpboxNextSpellCheck = dynamic_cast<VwParagraphBox *>(pboxTarget);
	if (m_pvpboxNextSpellCheck == NULL)
		m_fCompletedSpellCheck = true;
	else
		m_pvpboxNextSpellCheck->AssertValid();
	*pfComplete = m_fCompletedSpellCheck;


	END_COM_METHOD(g_fact, IID_IVwRootBox);
}


/*----------------------------------------------------------------------------------------------
	Turn the current selection back on, as required at the end of an editing session.
----------------------------------------------------------------------------------------------*/
void VwRootBox::ShowSelectionAfterEdit()
{
	HandleActivate(vssEnabled);
	if (m_qvwsel)
		m_qvwsel->Show();
}


/*----------------------------------------------------------------------------------------------
	Set the synchronizer that will coordinate this root box being synchronized
	with others.
----------------------------------------------------------------------------------------------*/
void VwRootBox::SetSynchronizer(VwSynchronizer * psync)
{
	m_qsync = psync;
}

/*----------------------------------------------------------------------------------------------
	Get the synchronizer that will coordinate this root box being synchronized
	with others.
----------------------------------------------------------------------------------------------*/
VwSynchronizer * VwRootBox::GetSynchronizer()
{
	return m_qsync;
}

/*----------------------------------------------------------------------------------------------
	Get the box (if there is exactly one) that represents the whole of the display
	of hvoObj.
	Enhance JohnT: we could make this method more general by obtaining a sequence of arbitrary
	boxes, but we don't have a need for that yet.
----------------------------------------------------------------------------------------------*/
VwBox * VwRootBox::GetBoxDisplaying(HVO hvoObj)
{
	BuildNotifierMap();
	// See if we have exactly one notifier for this object.
	ObjNoteMap::iterator itLim;
	ObjNoteMap::iterator it;
	if (m_mmhvoqnote.Retrieve(hvoObj, &it, &itLim))
	{
		// Although we only expect one 'real' notifier per hvo in synchronized views, we could
		// well have some others, such as ones resulting from NoteDependency. Look for the
		// first real one.
		for ( ; it != itLim; ++it)
		{
			VwAbstractNotifier * pvanote = *it;
			// Synchronized views should be constructed so that each boxed object occurs only
			// once.
			VwNotifier * pnote = dynamic_cast<VwNotifier *>(pvanote);
			if (!pnote)
				continue;
			VwBox * pboxFirst = pnote->FirstBox();
			// For now we only try to synchronize single boxes. Higher level properties
			// (such as the sections of a book) could be a sequence; just ignore them.
			if (pboxFirst != pnote->LastBox())
			{
				VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(pboxFirst);
				if (pgbox == NULL || !pgbox->Contains(pnote->LastBox()))
					break;
			}
			// OK, this is the box we need to synchronize.
			return pboxFirst;
		}
	}
	// JohnT: the following would be a useful check about half the time. The problem is,
	// when we expand something lazy, the FIRST thing we do is to expand the corresponding
	// lazy stuff in all the related views. When THOSE views try to update their layout,
	// they fail to find the new stuff, which we haven't yet created in the starting view.
	// So those failures are normal.
	// It may be that we could arrange that the layout done as part of a sync expand does
	// not try to sync...if so, we would save some computation AND make the following
	// check useful.
	// It may also be worth re-enabling this warning in some problem-solving situations,
	// but ignore it if the call stack includes VwSynchronizer::ExpandLazyItems.
	//// JohnT: this is a bit of a kludge, specific to TE. We expect to fail to find a
	//// single box for books, sections, and higher-level objects; but not for StTxtParas.
	//// So generate a warning.
	//HVO clsid;
	//CheckHr(m_qsda->get_IntProp(hvoObj, kflidCmObject_Class, &clsid));
	//if (clsid == kclidStTxtPara)
	//{
	//	StrAnsi sta;
	//	sta.Format("could not find matching sync box for object %d\n", hvoObj);
	//	Warn(sta.Chars());
	//}
	return NULL; // this object not (yet) displayed in this view.
}

/*----------------------------------------------------------------------------------------------
	Set the actual distance from the top of the previous box (or top of containing pile, if
	no previous box) to the top of the one that displays hvoObj
	(as a result of synchronization).
----------------------------------------------------------------------------------------------*/
void VwRootBox::SetActualTopToTop(HVO hvoObj, int dypActualTopToTop)
{
	VwBox * pbox = GetBoxDisplaying(hvoObj);
	if (!pbox)
		return;
	return pbox->SetActualTopToTop(dypActualTopToTop);
}

/*----------------------------------------------------------------------------------------------
	Set the actual distance from the top of the box displaying hvoObj box to the top of the
	next one (as a result of synchronization).
----------------------------------------------------------------------------------------------*/
void VwRootBox::SetActualTopToTopAfter(HVO hvoObj, int dypActualTopToTop)
{
	VwBox * pbox = GetBoxDisplaying(hvoObj);
	if (!pbox)
		return;
	VwPileBox * pboxp = dynamic_cast<VwPileBox *>(pbox->Container());
	if (!pboxp)
		return;
	pboxp->SetActualTopToTopAfter(pbox, dypActualTopToTop);
}

/*----------------------------------------------------------------------------------------------
	Get the natural distance from the top of the previous box (or top of containing pile, if
	no previous box) to the top of the one that displays hvoObj
	(in the absence of synchronization).
----------------------------------------------------------------------------------------------*/
int VwRootBox::NaturalTopToTop(HVO hvoObj)
{
	VwBox * pbox = GetBoxDisplaying(hvoObj);
	if (!pbox)
		return 0;
	return pbox->NaturalTopToTop();
}

/*----------------------------------------------------------------------------------------------
	Get the natural distance from the top of the box displaying hvoObj to the top of the next one
	(in the absence of synchronization), or where the next one would go if there were
	one.
----------------------------------------------------------------------------------------------*/
int VwRootBox::NaturalTopToTopAfter(HVO hvoObj)
{
	VwBox * pbox = GetBoxDisplaying(hvoObj);
	if (!pbox)
		return 0;
	return pbox->NaturalTopToTopAfter();
}

/*----------------------------------------------------------------------------------------------
	Signal whether or not the contents of the selection have changed.
----------------------------------------------------------------------------------------------*/
void VwRootBox::SetDirty(bool fDirty)
{
	m_fDirty = fDirty;
}


/*----------------------------------------------------------------------------------------------
	Notify listeners that the selection changed. -1=notchanged; 1= same para; 2=different para
----------------------------------------------------------------------------------------------*/
void VwRootBox::NotifySelChange(VwSelChangeType nHow, bool fUpdateRootSite)
{
	if (m_qvwsel && fUpdateRootSite && m_qvwsel->IsValid())
		CheckHr(m_qvrs->SelectionChanged(this, m_qvwsel));

#ifdef ENABLE_TSF
	if (m_qvim)
	{
		// I'm not sure if it is really necessary to store m_pvpboxLastSelectedAnchor or if we
		// could get it dynamically when we need it. But I'm trying to reimplement
		// VwTextStore::OnSelChange as close as possible.
		if (nHow != ksctSamePara)
		{
			VwTextSelection * psel = dynamic_cast<VwTextSelection *>(Selection());
			m_pvpboxLastSelectedAnchor = psel ? psel->AnchorBox() : NULL;
		}

		CheckHr(m_qvim->OnSelectionChange(nHow));
	}
#endif /*ENABLE_TSF*/
}

/*----------------------------------------------------------------------------------------------
	This calls Layout with the correct parameters. It also notifies the root site of
	any size changes in case it needs to update anything.
----------------------------------------------------------------------------------------------*/
void VwRootBox::LayoutFull()
{
	AssertPtr(m_qvrs);

	// May affect layout; recompute.
	int dxAvailWidth;
	CheckHr(m_qvrs->GetAvailWidth(this, &dxAvailWidth));
	HoldLayoutGraphics hg(this);
	// It is safest to check both Height() and FieldHeight(). Occasionally FieldHeight
	// may not detect a change, for example, when a style definition has changed before
	// this routine is called, and that is all that affects the height.
	int dyOld2 = Height();
	int dyOld = FieldHeight();
	int dxOld = Width();

	// Invalidate before and after because the box may get larger or smaller
	// (Or perhaps both, in different directions) and we want all the old and new areas
	// erased.
	Invalidate();
	Layout(hg.m_qvg, dxAvailWidth);
	Invalidate();
	if (dyOld != FieldHeight() || dxOld != Width() || dyOld2 != Height())
		CheckHr(m_qvrs->RootBoxSizeChanged(this));
#ifdef ENABLE_TSF
	if (m_qvim)
		CheckHr(m_qvim->OnLayoutChange());
#endif /*ENABLE_TSF*/
}


/*----------------------------------------------------------------------------------------------
	Invalidate the specified rectangle.
----------------------------------------------------------------------------------------------*/
void VwRootBox::InvalidateRect (Rect * pvwrect)
{
	if (!m_qvrs)
	{
		ThrowHr(E_UNEXPECTED);
	}
	Assert(m_qvrs.Ptr());
	// if an error occurs while doing this ignore it.
	m_qvrs->InvalidateRect(this, pvwrect->left, pvwrect->top,
		pvwrect->Width(), pvwrect->Height());
}

/*----------------------------------------------------------------------------------------------
	Box is about to be deleted; if this affects your selection destroy the selection
	or repair it, if a replacement is known. Also clean up any other active selections.
	Also a convenient place to detect deletion of the current spell-check box.
----------------------------------------------------------------------------------------------*/
void VwRootBox::FixSelections(VwBox * pbox, VwBox* pboxReplacement)
{
	if (m_qvwsel && m_qvwsel->RuinedByDeleting(pbox, pboxReplacement))
	{
		DestroySelection();
	}
	// Reverse loop so we can delete safely.
	for (int isel = m_vselInUse.Size(); --isel >= 0; )
	{
		if (m_vselInUse[isel]->RuinedByDeleting(pbox, pboxReplacement))
		{
			m_vselInUse[isel]->MarkInvalid();
			m_vselInUse.Delete(isel);
		}
	}
	if (pbox == m_pvpboxNextSpellCheck)
	{
		ResetSpellCheck();
	}
}

/*----------------------------------------------------------------------------------------------
	Strings are being replaced in text source, fix selections as well as we can. At least keep
	them valid.
	Return true if the current selection was modified.
----------------------------------------------------------------------------------------------*/
bool VwRootBox::FixSelectionsForStringReplacement(VwTxtSrc * psrcModify, int itssMin, int itssLim,
		VwTxtSrc * psrcRep)
{
	bool fCurrentSelectionChanged = false;
	for (int isel = m_vselInUse.Size(); --isel >= 0; )
	{
		if (m_vselInUse[isel]->AdjustForStringReplacement(psrcModify, itssMin, itssLim, psrcRep)
			&& m_vselInUse[isel] == m_qvwsel)
		{
			// If it's the current selection and actually got changed, we want to send a notification.
			fCurrentSelectionChanged = true;
		}
	}
	return fCurrentSelectionChanged;
}

/*----------------------------------------------------------------------------------------------
	Strings are being replaced in text source, fix selections as well as we can. At least keep
	them valid.
----------------------------------------------------------------------------------------------*/
void VwRootBox::ResetSpellCheck()
{
	m_pvpboxNextSpellCheck = NULL;
	m_fCompletedSpellCheck = false;
}
/*----------------------------------------------------------------------------------------------
	Spell checking needs to start over (possibly dictionaries have been changed)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::RestartSpellChecking()
{
	BEGIN_COM_METHOD;
	ResetSpellCheck();
	END_COM_METHOD(g_fact, IID_IVwRootBox);
}

struct NamePair
{
	std::string extendedName;
	std::string targetName;
};

/*----------------------------------------------------------------------------------------------
	Get a dictionary, assigning a ref count on it. If no suitable dictionary
	exists, return null.
----------------------------------------------------------------------------------------------*/
void VwRootBox::GetDictionary(const OLECHAR * pszId, ICheckWord ** ppcw)
{
	*ppcw = NULL;
	if (!m_qgspCheckerRepository)
		return;
	static const OleStringLiteral literalNone = L"<None>";
	if (wcscmp(pszId, literalNone) == 0)
		return;
	CheckHr(m_qgspCheckerRepository->GetChecker(const_cast<OLECHAR *>(pszId), ppcw));
}


/*----------------------------------------------------------------------------------------------
	Compute a list of notifiers (to delete, usually) whose key is pbox and whose level is
	greater or equal to chvoLevel (default (-1): all notifiers).
	Add the specified notifiers to vpanote.
	We accumulate the list like this because when a complex structure of boxes is being deleted,
	it can be difficult to determine an order in which to delete the notifiers so as to ensure
	that we never delete a parent notifier before one of its children. So, we compute an
	overall list before deleting any.
	However, we do need to delete them from the map right away. Otherwise, later parts of the
	process may
----------------------------------------------------------------------------------------------*/
void VwRootBox::DeleteNotifiersFor(VwBox * pbox, int chvoLevel, NotifierVec & vpanote)
{
	BuildNotifierMap(); //make sure we can look up

	//start at the first item if any with the right key
	NotifierMap::iterator itLim;
	NotifierMap::iterator it;
	int inoteNew = vpanote.Size();
	if (m_mmboxqnote.Retrieve(pbox, &it, &itLim))
	{
		// Make a list of ones to delete after we get done iterating
		// (Deleting invalidates iterators.)
		// Don't close them until done, either...could mess up level calculations.
		for (; it != itLim; ++it)
		{
			// *it points to a notifier from the set
			// if its level is appropriate delete it
			if ((*it).Ptr()->Level() >= chvoLevel)
			{
				vpanote.Push((*it).Ptr()); // note for deletion.
			}
		}
	}
	// We want these notifiers dead except for their parent pointer, which is used in
	// calculating levels of child notifiers. In particular it should no longer be
	// possible to find them by HVO and try to do PropChanged notifications on them.
	// Removing them from the box map allows the validation in the VwBox destructor.
	for (int inote = inoteNew; inote < vpanote.Size(); inote++)
	{
		VwAbstractNotifier * panote = vpanote[inote];
		HVO hvo = panote->Object(); // get before closing
		VwBox * pboxKey = panote->KeyBox(); // to avoid const errors in next line
		m_mmboxqnote.Delete(pboxKey, panote);
		m_mmhvoqnote.Delete(hvo, panote);
		panote->_KeyBox(NULL); // indicates it is dead, even if still in some lists.
	}
}

void VwRootBox::DeleteNotifierVec(NotifierVec & vpanote)
{
	BuildNotifierMap(); // for paranoia, should be done any time we currently call this.
	for (int i = 0; i < vpanote.Size(); i++)
	{
		VwAbstractNotifier * panote = vpanote[i];
		HVO hvo = panote->Object(); // get before closing
		panote->Close(); // force delete the actual notifier
		VwBox * pboxKey = panote->KeyBox(); // to avoid const errors in next line
		m_mmboxqnote.Delete(pboxKey, panote);
		m_mmhvoqnote.Delete(hvo, panote);
		panote->_KeyBox(NULL); // indicates it is dead, even if still in some lists.
	}
}

void VwRootBox::DeleteNotifier(VwAbstractNotifier * panote)
{
	BuildNotifierMap(); // for paranoia, should be done any time we currently call this.
	HVO hvo = panote->Object(); // key is not declared const
	m_mmhvoqnote.Delete(hvo, panote);
	// Do this last, it may get actually deleted as we remove it from the ComMM.
	VwBox * pboxKey = panote->KeyBox();
	panote->_KeyBox(NULL); // indicates it is dead, even if still in some lists.
	m_mmboxqnote.Delete(pboxKey, panote);
}

void VwRootBox::AddNotifier(VwAbstractNotifier * panote)
{
	m_vpanote.Push(panote);
}

//add the specified notifiers to your list
//ENHANCE JohnT: could probably be private, a friend of VwNotifier
void VwRootBox::AddNotifiers(NotifierVec * pvpanote)
{
	m_vpanote.EnsureSpace(pvpanote->Size());
	for (int i = 0; i< pvpanote->Size(); i++)
		m_vpanote.Push((*pvpanote)[i]);
}

// Shortcut for getting the container's graphics object.
void VwRootBox::GetGraphics(IVwGraphics ** ppvg, Rect * prcSrcRoot, Rect * prcDstRoot)
{
	if (!m_qvrs)
		ThrowHr(WarnHr(E_UNEXPECTED));
	CheckHr(m_qvrs->GetGraphics(this, ppvg, prcSrcRoot, prcDstRoot));
}

void VwRootBox::ReleaseGraphics(IVwGraphics * pvg)
{
	if (!m_qvrs)
		ThrowHr(WarnHr(E_UNEXPECTED));
	CheckHr(m_qvrs->ReleaseGraphics(this, pvg));
}

// This is the core of Relayout that VwLayoutStream needs to override.
bool VwRootBox::RelayoutCore(IVwGraphics * pvg, int dxpAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi,
		BoxSet * pboxsetDeleted)
{
	bool result = SuperClass::Relayout(pvg, dxpAvailWidth, prootb, pfixmap,
		dxpAvailOnLine, pmmbi);
#ifdef ENABLE_TSF
	if (m_qvim)
		CheckHr(m_qvim->OnLayoutChange());
#endif /*ENABLE_TSF*/
	return result;
}

void VwRootBox::RelayoutRoot(IVwGraphics * pvg, FixupMap * pfixmap, int dxpAvailOnLine,
	BoxSet * pboxsetDeleted)
{
	if (!m_qvrs) {
		Assert (false);
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	int dxAvailWidth;
	CheckHr(m_qvrs->GetAvailWidth(this, &dxAvailWidth));
	// It is safest to check both Height() and FieldHeight(). Occasionally FieldHeight
	// may not detect a change, for example, when a style definition has changed before
	// this routine is called, and that is all that affects the height.
	int dyOld2 = Height();
	int dyOld = FieldHeight();
	int dxOld = Width();
	RelayoutCore(pvg, dxAvailWidth, this, pfixmap, -1, NULL, pboxsetDeleted);
	if (dyOld != FieldHeight() || dxOld != Width() || dyOld2 != Height())
		CheckHr(m_qvrs->RootBoxSizeChanged(this));
}

int VwRootBox::AvailWidthForChild(int dpiX, VwBox * pboxChild)
{
	int dxAvailWidth;
	m_qvrs->GetAvailWidth(this, &dxAvailWidth);
	return dxAvailWidth - SurroundWidth(dpiX);
}


void VwRootBox::ShowSelection()
{
	if (!m_qvwsel)
		return;
	m_qvwsel->Show();
}

/*----------------------------------------------------------------------------------------------
	Construct the right sort of VwEnv. Typical usage is
	VwEnvPtr qvwenv;
	qvwenv->Attach(MakeEnv());
----------------------------------------------------------------------------------------------*/
VwEnv * VwRootBox::MakeEnv()
{
	return NewObj(VwEnv);
}

/*----------------------------------------------------------------------------------------------
	Construct your embedded boxes, assuming Init and one of the SetRoot methods has been called.
	ENHANCE: JohnT: figure what it should do for SetRootVariant.
----------------------------------------------------------------------------------------------*/
void VwRootBox::Construct(IVwGraphics * pvg, int dxAvailWidth)
{
	AssertPtr(pvg);
	VwEnvPtr qvwenv;
	qvwenv.Attach(MakeEnv());
	qvwenv->Initialize(pvg, this, m_vqvwvc.Size() == 0 ? NULL : m_vqvwvc[0]);
	for (int i = 0; i < m_chvoRoot; i++)
	{
		qvwenv->OpenObject(m_vhvo[i]);
		CheckHr(m_vqvwvc[i]->Display(qvwenv, m_vhvo[i], m_vfrag[i]));
		qvwenv->CloseObject();
	}
	// ENHANCE: JohnT: there are probably some things we could usefully verify here
	// about everything being closed, etc...
	qvwenv->Cleanup();
	m_fConstructed = true;
	ResetSpellCheck(); // in case it somehow got called while we had no contents.
}

/*----------------------------------------------------------------------------------------------
	Set your selection. Will remove the old one, but will not show the new one; call
	ShowSelection if appropriate. Does nothing if the argument is already the selection.
----------------------------------------------------------------------------------------------*/
void VwRootBox::SetSelection(VwSelection * pvwsel,	bool fUpdateRootSite)
{
	if (pvwsel == m_qvwsel)
		return;
	VwSelChangeType nHowChanged = ksctUnknown;
	if (!pvwsel)
		nHowChanged = ksctDeleted;
	if (m_qvwsel)
	{
		ComBool fOk;

		m_qvwsel->LoseFocus(pvwsel, &fOk);
		if (!fOk)
		{
			// Don't disturb current selection
			return;
		}
		//- DestroySelection(); This sends a spurious selection destroyed notification.
		// Hide the old selection.
		m_qvwsel->Hide();

		// See if we can determine whether it is the same or a different paragraph.
		VwTextSelection * pselOld = dynamic_cast<VwTextSelection *>(m_qvwsel.Ptr());
		VwTextSelection * pselNew = dynamic_cast<VwTextSelection *>(pvwsel);
		if (pselOld && pselNew)
		{
			if (pselOld->AnchorBox() != pselNew->AnchorBox() || pselOld->EndBox() != pselNew->EndBox())
				nHowChanged = ksctDiffPara;
			else
				nHowChanged = ksctSamePara;
		}
	}

	m_qvwsel = pvwsel;
	NotifySelChange(nHowChanged, fUpdateRootSite); // presume para changed for completely new sel.
	return;
}

//:>********************************************************************************************
//:>	Notifier-related methods
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	A notifier's key box is being changed. If it is in the old map, remove it and re-insert
	with the new key.
----------------------------------------------------------------------------------------------*/
void VwRootBox::ChangeNotifierKey(VwAbstractNotifier * panote, VwBox * pboxNewKey)
{
	VwBox * pboxOldKey = panote->KeyBox();
	panote->_KeyBox(pboxNewKey); //remember how it is registered
	if (m_mmboxqnote.Delete(pboxOldKey, panote))
		m_mmboxqnote.Insert(pboxNewKey, panote);
}

/*----------------------------------------------------------------------------------------------
	If there are newly added notifiers not yet in the map, insert them.
	There are several reasons not to insert at once. For one thing, we may never need the map,
	if the user never edits. Also, in the regenerate process, we can use this to keep new
	notifiers (for the new display fragment) separated from old ones in the map, which require
	a different kind of updating for the change.
----------------------------------------------------------------------------------------------*/
void VwRootBox::BuildNotifierMap()
{
	// If no notifiers need adding we are done.
	if (m_vpanote.Size() == 0)
		return;

	for (int i = 0; i < m_vpanote.Size(); i++)
	{
		VwAbstractNotifier * panote = m_vpanote[i];
		VwBox * pboxKey = panote->FirstCoveringBox();
		panote->_KeyBox(pboxKey); //remember how it is registered
		m_mmboxqnote.Insert(pboxKey, panote);
		panote->AddToMap(m_mmhvoqnote);
		// Following no good for PropListNotifier
		//HVO hvoKey = panote->Object();
		//m_mmhvoqnote.Insert(hvoKey, panote);
	}
	m_vpanote.Clear();
}

/*----------------------------------------------------------------------------------------------
	Get a pointer to the notifier map. The client must not retain this pointer beyond the
	lifetime of the VwRootBox. The map is guaranteed accurate at the time this is called;
	however, subsequent additions to the box's notifiers will not automatically appear
	unless BuildNotifierMap is called.
----------------------------------------------------------------------------------------------*/
void VwRootBox::GetNotifierMap(NotifierMap ** ppmmboxqnote, ObjNoteMap ** ppmmhvoqnote)
{
	BuildNotifierMap();
	*ppmmboxqnote = &m_mmboxqnote;
	if (ppmmhvoqnote)
		*ppmmhvoqnote = &m_mmhvoqnote;
}

/*----------------------------------------------------------------------------------------------
	Extract all your new notifiers, ones that have not yet been added to the map. The argument
	vector should be empty.
----------------------------------------------------------------------------------------------*/
void VwRootBox::ExtractNotifiers(NotifierVec * pvpanote)
{
	Assert(pvpanote->Size() == 0);
	// OPTIMIZE JohnT: if we had a vector swap operation we could use it here.
	for (int i = 0; i < m_vpanote.Size(); i++)
		pvpanote->Push(m_vpanote[i]);
	m_vpanote.Clear();
}

void VwRootBox::RegisterSelection(VwSelection * psel)
{
	m_vselInUse.Push(psel);
}

void VwRootBox::UnregisterSelection(VwSelection * psel)
{
	int csel = m_vselInUse.Size();
	for (int isel = 0; isel < csel; isel++)
	{
		if (m_vselInUse[isel] == psel)
		{
			m_vselInUse.Delete(isel);
			return;
		}
	}
	Assert(false);
}

/*----------------------------------------------------------------------------------------------
	Find the notifier (there could be more than one, but this is used in synchronized view
	situations where there shouldn't be) that displays property tag of object hvoContext as
	its iprop'th property.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwRootBox::NotifierForObjPropIndex(HVO hvoContext, int tag,
	int iprop)
{
	BuildNotifierMap();
	ObjNoteMap::iterator itLim;
	ObjNoteMap::iterator it;
	if (!m_mmhvoqnote.Retrieve(hvoContext, &it, &itLim))
		return NULL; // Can't do it, we don't have any display of that object. (Should we Assert?)
	VwNotifier * pnote = NULL;
	for (; it != itLim; ++it)
	{
		VwAbstractNotifier * pvanote = *it;
		pnote = dynamic_cast<VwNotifier *>(pvanote);
		if (!pnote)
			continue;
		if (pnote->CProps() < iprop)
			continue; // Some other display of the object? (Should we warn or Assert?)
		if (pnote->Tags()[iprop] != tag)
			continue; // Also very unexpected...
		break; // got the one we want.
	}
	return pnote;
}

/*----------------------------------------------------------------------------------------------
	Expand the lazy box which is displaying property tag of object hvoContext as the iprop'th
	property, but don't lay out here (that's done in the Synchronizer).
----------------------------------------------------------------------------------------------*/
VwBox * VwRootBox::ExpandItemsNoLayout(HVO hvoContext, int tag, int iprop, int ihvoMin, int ihvoLim,
	Rect * prcLazyBoxOld, VwBox ** ppboxFirstLayout, VwBox ** ppboxLimLayout,
	VwDivBox ** ppdboxContainer)
{
	AssertPtr(ppboxFirstLayout);
	AssertPtr(ppboxLimLayout);
	AssertPtr(ppdboxContainer);
	AssertPtr(prcLazyBoxOld);

	*ppboxFirstLayout = NULL;
	*ppboxLimLayout = NULL;
	*ppdboxContainer = NULL;

	EnsureConstructed();

	VwNotifier * pnote = NotifierForObjPropIndex(hvoContext, tag, iprop);
	if (!pnote)
	{
		StrAnsi sta;
		sta.Format("Failed to find notifier to expand sync'd object %d\n", hvoContext);
		Warn(sta.Chars());
		return NULL; // Or Assert?
	}
	VwBox * pbox = pnote->Boxes()[iprop];
	// OK, we found the notifier that displays the same property of the same object
	// in the same position. Now we search the contents of that property for a lazy box
	// that is representing the range of objects from that property that we want
	// to expand (or a larger range that contains the one we want).
	for (; pbox; pbox = pbox->NextOrLazy())
	{
		VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pbox);
		if (!plzbox)
			continue;
		if (plzbox->Object() != hvoContext)
			continue; // Expanding an item could lead to another level of laziness
		if (plzbox->LimObjIndex() >= ihvoLim && plzbox->MinObjIndex() <= ihvoMin)
		{
			// Remember our old position. This will be helpful for making adjustments later
			HoldGraphics hg(this);
			*prcLazyBoxOld = plzbox->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
			// Get this BEFORE calling ExpandItems, which might destroy the lazy box.
			*ppdboxContainer = dynamic_cast<VwDivBox *>(plzbox->Container());
			// Note that this version of the routine does NOT attempt to synchronize
			// other roots. Note also that the ihvo's passed to it are relative to its MinObjIndex().
			// Note that calling this may DESTROY plzbox; don't use it in any way after this call.
			// NOTE that this function might insert things into the hash table, invalidating the
			// iterator. Beware of this if enhancing somehow to handle multiple occurrences of the
			// object. It's OK when we've already terminated the iterator loop as above.
			VwBox * pRet = plzbox->ExpandItemsNoLayout(ihvoMin - plzbox->MinObjIndex(),
				ihvoLim - plzbox->MinObjIndex(),
				pnote, iprop, tag, ppboxFirstLayout, ppboxLimLayout);
			return pRet; // STOP the loop (especially to ensure we don't call NextOrLazy on destroyed box.)
		}
		// Enhance JohnT: possibly it would be worth expanding a lazy box that covers
		// only part of the range. However, as we are keeping all the roots synchronized,
		// this should never occur.
	}
	StrAnsi sta;
	sta.Format("Failed to find lazy box to expand range %d to %d of sync'd object %d\n",
		ihvoMin, ihvoLim, hvoContext);
	Warn(sta.Chars());
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Method that is normally the second half of ExpandItems, but also used in special
	circumstances when expanding to get the paragraphs before a numbered one.
	It calls DoLayout for each box from pboxFirstLayout to the one before pboxLimLayout,
	computes how any size change affects containing boxes, and if necessary notifies the
	root site to adjust the scroll position. The presumption is that the listed boxes
	have been produced by expanding a closure.

	@param rcRootOld: The rectangle occupied by the root box before expansion.
	@param pboxFirstLayout: Usually, the box where layout started. It is possible for
	this to be null, if the items expanded produced no boxes and the lazy box was destroyed
	and it had no following boxes
	@param rcThisOld The rectangle occupied by the Lazy box being expanded, before the
	expansion.
	@param pdboxContainer: the container of the lazy box before expansion. Usually this is
	the container of pboxFirstLayout (unless pboxFirstLayout is null, in which case
	containing group boxes, including the root, still need to be fixed).
	@param psync: Normally pass the root box's synchronizer. When expanding or
	contracting lazy boxes in a derived view, pass null, so the adjustment is made in the
	dependent view as if there were no synchronization. Later, the view doing the primary
	expand or contract will do a synchronized layout, and fix things.
----------------------------------------------------------------------------------------------*/
void VwRootBox::AdjustBoxPositions(Rect rcRootOld, VwBox * pboxFirstLayout, VwBox * pboxLimLayout,
	Rect rcThisOld, VwDivBox * pdboxContainer, bool * pfForcedScroll, VwSynchronizer * psync,
	bool fDoLayoutForExpandedItems)
{
	Assert(pdboxContainer || (!pboxFirstLayout && !pboxLimLayout));
	if (!pdboxContainer)
		return;

	// This method is used for layout operations during expanding lazy boxes. Operations like paint and PropChanged
	// are dangerous during it, just like PropChanged calls.
	bool fWasPropChangeInProgress = m_fIsPropChangedInProgress;
	m_fIsPropChangedInProgress = true;

	AssertPtr(pdboxContainer);
	if (pfForcedScroll)
		*pfForcedScroll = false; // default

	// We deliberately don't do any excess invalidating. A lazy box never occupies a visible part of
	// the screen. However, the new boxes need to get laid out, and the size of the root may change
	// in a way that matters.
	if (Height() != 0)
	{
		// The root has been laid out, we need to lay out the new boxes and consider the possibility
		// that the root's size has changed, also that the scroll position needs to change if
		// the closure is before it.
		int dxsAvailWidth;
		IVwRootSitePtr qvrs;
		CheckHr(get_Site(&qvrs));
		CheckHr(qvrs->GetAvailWidth(this, &dxsAvailWidth));
		int dysSrcHeight; // the height of the Src rectangle in our coord transformation.
		int dxsSrcWidth;
		{	// BLOCK, to control scope of HoldLayoutGraphics. Note that we only use its width,
			// otherwise we would possibly need to obtain it more often.
			HoldLayoutGraphics hg(this);
			dysSrcHeight = DpiSrc().x;
			dxsSrcWidth = DpiSrc().y;
		}

		// Sync'd views did the layout already
		if (fDoLayoutForExpandedItems)
			VwLazyBox::LayoutExpandedItems(pboxFirstLayout, pboxLimLayout, pdboxContainer, true);

		int dxpSurroundWidth = pdboxContainer->SurroundWidth(dxsSrcWidth);
		int dxpInnerWidth = pdboxContainer->Width() - dxpSurroundWidth;

		int xpPos = pdboxContainer->GapLeft(dxsSrcWidth); // left of all boxes goes here

		VwDivBox * pdboxOuter = pdboxContainer;
		VwBox * pboxCurr = pdboxOuter->FirstBox();

		// Do NOT try to optimize by redoing the layout only from the box before pboxFirstLayout.
		// It is possible that pboxFirstLayout was a lazy box, and the laying out one of the
		// subsequent boxes has destroyed it, so trying to do anything with it is dangerous.

		// This keeps track of the previous box we have positioned.
		VwBox * pboxBeforeLayout = NULL;

		// This loop fixes the positions of boxes subsequent to the change, in all containers.
		// The first iteration fixes everything in the lowest level container, because of
		// the problem mentioned above.
		// Subsequent iterations move to higher level containers.
		// The code at the end also needs to update pboxBeforeLayout.
		for (;;)
		{
			int ypPos; // Becomes position of top of each box successively.
			if (pboxBeforeLayout)
				ypPos = pdboxOuter->SyncedComputeTopOfBoxAfter(pboxBeforeLayout, dysSrcHeight,
					this, psync);
			else
				ypPos = pdboxOuter->SyncedFirstBoxTopY(dysSrcHeight, this, psync);

			// This loop should do very much the same as the one in VwPileBox::DoLayout,
			// except it doesn't have to worry about max lines.
			// Note that we deliberately go on past pboxLimLayout; subsequent boxes may have
			// their positions changed also.
			VwBox * pboxSave = pboxCurr;
			for (; pboxCurr; pboxCurr=pboxCurr->NextOrLazy())
				dxpInnerWidth = std::max(dxpInnerWidth, pboxCurr->Width());

			for (pboxCurr = pboxSave; pboxCurr; pboxCurr = pboxCurr->NextOrLazy())
			{
				// Todo JohnT: to support laziness with divisions not aligned left,
				// we need to compare Left() to the position that AdjustLeft() computes.
				if (pboxCurr->Top() != ypPos || pboxCurr->Left() != xpPos)
				{
					//it moved! need to invalidate
					// OPTIMIZE JohnT: there are probably cases where, even though it moved,
					// the move is exactly cancelled out by the change in scroll position
					// produced by the expansion, and we don't need the invalidate calls.
					// Note, however, that there are cases where we do, for example, adding
					// paragraphs in the middle of a numbered list in a lazy structured text
					// (e.g., WorldPad), where Relayout gets messed up because this code gets
					// ahead of it, while expanding to figure the numbers of earlier paragraphs
					// that get expanded.
					pboxCurr->Invalidate();
					pboxCurr->Top(ypPos);
					pdboxContainer->AdjustLeft(pboxCurr, xpPos, dxpInnerWidth);
					pboxCurr->Invalidate();
				}
				ypPos = pdboxOuter->SyncedComputeTopOfBoxAfter(pboxCurr, dysSrcHeight,
					this, psync);
			}
			// Getting the previous height here doesn't work because calling DoLayout
			// might have it changed already.
			//if (pdboxOuter == this)
			//{
			//	// All the effects of expanding other closures have now been taken into
			//	// account, so it is safe to compute the old root box size to figure from it
			//	// how much difference this expansion made. Only after that should we
			//	// update its size directly.
			//	HoldGraphics hg(this);
			//	rcRootOld = this->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
			//}

			if (psync)
				psync->AdjustSyncedBoxHeights(pdboxOuter, dysSrcHeight);
			else
				pdboxOuter->_Height(ypPos + pdboxOuter->GapBottom(dysSrcHeight));
			pdboxOuter->_Width(dxpInnerWidth + dxpSurroundWidth);

			// Now fix any containers, as necessary
			pboxCurr = pdboxOuter;
			pdboxOuter = dynamic_cast<VwDivBox *>(pdboxOuter->Container());
			if (!pdboxOuter)
				break;
			pboxBeforeLayout = pdboxOuter->BoxBefore(pboxCurr);
		}

		{ // BLOCK for HoldGraphics
			HoldGraphics hg(this);
			Rect rcRootNew = GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
			int dydSize = rcRootNew.Height() - rcRootOld.Height();
			int dxdSize = rcRootNew.Width() - rcRootOld.Width();
			if (dydSize > 0)
			{
				// Root box grew...the growth area is not necessarily in the paint area. Invalidate it.
				Rect rcInvalidate = rcRootNew;
				rcInvalidate.top = rcInvalidate.bottom - dydSize;
				InvalidateRect(&rcInvalidate);
			}
			// Only need to adjust the scroll range if the height or width changed and if
			// this root box is the driving one in the synch group (or if we're not synched at all).
			if ((dydSize || dxdSize) && (!GetSynchronizer() || psync))
			{
				ComBool fForcedScroll;
				CheckHr(qvrs->AdjustScrollRange(this, dxdSize, rcThisOld.left - hg.m_rcDstRoot.left,
					dydSize, rcThisOld.top - hg.m_rcDstRoot.top,
					&fForcedScroll));
				if (pfForcedScroll)
					*pfForcedScroll = fForcedScroll ? true : false;
			}
		}
	}
	m_fIsPropChangedInProgress = fWasPropChangeInProgress;
}
/*----------------------------------------------------------------------------------------------
	Another view with which this is synchronized has contracted items from ihvoMin to ihvoLim
	to a lazy box representing property tag of object hvoContext as the iprop'th
	property. If possible make the corresponding contraction here.
	Note that this should only be called if this box has no selections.
----------------------------------------------------------------------------------------------*/
void VwRootBox::ContractLazyItems(HVO hvoContext, int tag,
	int iprop, int ihvoMin, int ihvoLim)
{
	Assert(m_vselInUse.Size() == 0);
	if (m_vselInUse.Size() > 0)
		return;
	VwNotifier * pnote = NotifierForObjPropIndex(hvoContext, tag, iprop);
	if (!pnote)
		return; // Or Assert?
	LazinessIncreaser li(this);
	li.MakeLazy(pnote, iprop, ihvoMin, ihvoLim);
}

/*----------------------------------------------------------------------------------------------
	Make as much stuff lazy as possible.
	Don't convert any box that is currently forbidden by the root site, based on its location.
	Don't convert any box in the sequence from pboxMinKeep to pboxLimKeep (typically the ones
	we just expanded!). These arguments may both be null to not use this restriction.
	Don't convert any boxes currently involved in any selection in use.
	Don't convert any box that is a container or child (directly or indirectly) of boxes
	already forbidden.

	WARNING: any other box might possibly be DELETED by calling this routine! Leaving a
	dangling pointer if you aren't careful! Use with care.
----------------------------------------------------------------------------------------------*/
void VwRootBox::MaximizeLaziness(VwBox * pboxMinKeep, VwBox * pboxLimKeep)
{
	// If we are synchronized, only the root box that has the selection may increase laziness.
	// Otherwise, we'd have to figure which conversions would impact selections in other
	// synchronized views!
	if (GetSynchronizer() && GetSynchronizer()->AnotherRootHasSelection(this))
		return;
	LazinessIncreaser li(this);
	li.KeepSequence(pboxMinKeep, pboxLimKeep);
	for (int i = 0; i < m_vselInUse.Size(); i++)
		m_vselInUse[i]->AddToKeepList(&li);
#ifdef ENABLE_TSF
	if (m_pvpboxLastSelectedAnchor)
		li.KeepSequence(m_pvpboxLastSelectedAnchor, m_pvpboxLastSelectedAnchor->NextOrLazy());
#endif /*ENABLE_TSF*/
	li.ConvertAsMuchAsPossible();
}

/*----------------------------------------------------------------------------------------------
	Get a notifier that has the specified box as its key and the specified notifier as its
	parent. (Return NULL if pbox is NULL.)
----------------------------------------------------------------------------------------------*/
VwNotifier * VwRootBox::NotifierWithKeyAndParent(VwBox * pbox, VwNotifier * pnoteParent)
{
	if (!pbox)
		return NULL;
	NotifierMap * pmmboxqnote;
	GetNotifierMap(&pmmboxqnote);

	NotifierMap::iterator itnote;
	NotifierMap::iterator itnoteLim;
	pmmboxqnote->Retrieve(pbox, &itnote, &itnoteLim);
	// loop over the candidate notifiers.
	for (; itnote != itnoteLim; ++itnote)
	{
		VwNotifier * pnoteCandidate = dynamic_cast<VwNotifier *>(itnote.GetValue().Ptr());
		if (!pnoteCandidate)
			continue;
		if (pnoteCandidate->Parent() == pnoteParent)
			return pnoteCandidate;
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Unlock the root box and if we have skipped any paints redo them by invalidating now.
----------------------------------------------------------------------------------------------*/
void VwRootBox::Unlock()
{
	m_fLocked = false;
	Rect invalid;
	while (m_vrectSkippedPaints.Pop(&invalid))
		InvalidateRect(&invalid);
}

#ifdef WIN32 // In Linux we use a managed implementation
//:>********************************************************************************************
//:>	VwDrawRootBuffered
//:>********************************************************************************************
// Protected default constructor used for CreateCom
VwDrawRootBuffered::VwDrawRootBuffered()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

VwDrawRootBuffered::~VwDrawRootBuffered()
{
	if (m_hdcMem)
	{
		HBITMAP hbmp = (HBITMAP)::GetCurrentObject(m_hdcMem, OBJ_BITMAP);
		BOOL fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
		Assert(fSuccess);
		fSuccess = AfGdi::DeleteDC(m_hdcMem);
		Assert(fSuccess);
		m_hdcMem = 0;
	}
	ModuleEntry::ModuleRelease();
}

STDMETHODIMP VwDrawRootBuffered::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwDrawRootBuffered)
		*ppv = static_cast<IVwDrawRootBuffered *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IVwDrawRootBuffered *>(this),
			IID_IVwDrawRootBuffered, IID_IVwNotifyChange);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factVDRB(
	_T("SIL.Views.VwDrawRootBuffered"),
	&CLSID_VwDrawRootBuffered,
	_T("SIL root drawing wrapper"),
	_T("Apartment"),
	&VwDrawRootBuffered::CreateCom);

void VwDrawRootBuffered::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwDrawRootBuffered> qvdrb;
	qvdrb.Attach(NewObj VwDrawRootBuffered());		// ref count initialy 1
	CheckHr(qvdrb->QueryInterface(riid, ppv));
}

///*----------------------------------------------------------------------------------------------
//	This must ONLY be used for RootSite or other classes where GetGraphics returns the
//	one-and-only coordinate transformation.
//----------------------------------------------------------------------------------------------*/
//STDMETHODIMP VwDrawRootBuffered:: DrawTheRoot(IVwRootBox * prootb, HDC hdc, RECT rcpDraw,
//	COLORREF bkclr, ComBool fDrawSel, IVwRootSite * pvrs)
//{
//	BEGIN_COM_METHOD;
//
//	HBITMAP hbmpOld;
//	IVwGraphicsPtr qvg;
//	qvg.CreateInstance(CLSID_VwGraphicsWin32);
//	IVwGraphicsWin32Ptr qvg32;
//	Rect rcp(rcpDraw);
//	CheckHr(qvg->QueryInterface(IID_IVwGraphicsWin32, (void **) &qvg32));
//	BOOL fSuccess;
//	if (m_hdcMem)
//	{
//		BOOL fSuccess = AfGdi::DeleteDC(m_hdcMem);
//		Assert(fSuccess);
//	}
//	m_hdcMem = AfGdi::CreateCompatibleDC(hdc);
//	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rcp.Width(), rcp.Height());
//	Assert(hbmp);
//	hbmpOld = AfGdi::SelectObjectBitmap(m_hdcMem, hbmp);
//	Assert(hbmpOld && hbmpOld != HGDI_ERROR);
//	AfGfx::FillSolidRect(m_hdcMem, Rect(0, 0, rcp.Width(), rcp.Height()), bkclr);
//	CheckHr(qvg32->Initialize(m_hdcMem));
//	VwPrepDrawResult xpdr = kxpdrAdjust;
//	IVwGraphicsPtr qvgDummy; // Required for GetGraphics calls to get transform rects
//
//	try
//	{
//		Rect rcDst, rcSrc;
//
//		while (xpdr == kxpdrAdjust)
//		{
//			CheckHr(pvrs->GetGraphics(prootb, &qvgDummy, &rcSrc, &rcDst));
//			rcDst.Offset(-rcp.left, -rcp.top);
//
//			// Make sure our local graphics object (i.e. qvg32) contains the same dpi values
//			// as the graphics object just returned to us via the GetGraphics call.
//			int dpi;
//			qvgDummy->get_XUnitsPerInch(&dpi);
//			qvg32->put_XUnitsPerInch(dpi);
//			qvgDummy->get_YUnitsPerInch(&dpi);
//			qvg32->put_YUnitsPerInch(dpi);
//
//			CheckHr(prootb->PrepareToDraw(qvg32, rcSrc, rcDst, &xpdr));
//			CheckHr(pvrs->ReleaseGraphics(prootb, qvgDummy));
//			qvgDummy.Clear();
//		}
//		// kxpdrInvalidate true means that expanding lazy boxes at the position we planned
//		// to draw caused a nasty change in the scroll position, typically because
//		// we were near the bottom, and expanding the lazy stuff at the bottom
//		// did not yield a screen-ful of information. The entire window has
//		// been invalidated, which will cause a new Paint, so do nothing
//		// here. Otherwise, we can go ahead and draw.
//		if (xpdr != kxpdrInvalidate)
//		{
//			// Note that we need to get these again at this point,
//			// because PrepareToDraw may have made changes that alter the transformation
//			// rectangles.
//			CheckHr(pvrs->GetGraphics(prootb, &qvgDummy, &rcSrc, &rcDst));
//			rcDst.Offset(-rcp.left, -rcp.top);
//			Assert(rcSrc.Width());
//			Assert(rcSrc.Height());
//			Assert(rcDst.Width());
//			Assert(rcDst.Height());
//
//			// Make sure our local graphics object (i.e. qvg32) contains the same dpi values
//			// as the graphics object just returned to us via the GetGraphics call.
//			int dpi;
//			qvgDummy->get_XUnitsPerInch(&dpi);
//			qvg32->put_XUnitsPerInch(dpi);
//			qvgDummy->get_YUnitsPerInch(&dpi);
//			qvg32->put_YUnitsPerInch(dpi);
//
//			CheckHr(prootb->DrawRoot(qvg, rcSrc, rcDst, fDrawSel));
//			CheckHr(pvrs->ReleaseGraphics(prootb, qvgDummy));
//			qvgDummy.Clear();
//		}
//	}
//	catch (...)
//	{
//		if (qvgDummy)
//			CheckHr(pvrs->ReleaseGraphics(prootb, qvgDummy));
//		CheckHr(qvg->ReleaseDC());
//		throw;
//	}
//	CheckHr(qvg->ReleaseDC());
//	if (xpdr != kxpdrInvalidate)
//	{
//		// We drew something...now blast it onto the screen.
//		::BitBlt(hdc, rcp.left, rcp.top, rcp.Width(), rcp.Height(), m_hdcMem, 0, 0, SRCCOPY);
//	}
//
//	// Clean up bitmap.
//	HBITMAP hbmpDebug;
//	hbmpDebug = AfGdi::SelectObjectBitmap(m_hdcMem, hbmpOld, AfGdi::OLD);
//	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
//	Assert(fSuccess);
//
//	END_COM_METHOD(g_factVDRB, IID_IVwRootBox);
//}

/*----------------------------------------------------------------------------------------------
	This must ONLY be used for RootSite or other classes where GetGraphics returns the
	one-and-only coordinate transformation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwDrawRootBuffered::DrawTheRoot(IVwRootBox * prootb, HDC hdc, RECT rcpDraw,
	COLORREF bkclr, ComBool fDrawSel, IVwRootSite * pvrs)
{
	BEGIN_COM_METHOD;

	// Don't draw if we're in the middle of expanding lazy items because drawing could result in
	// expanding more lazy items (which would be a recursive expand. That is bad because the
	// pointers the outer expansion is using might no longer be valid).
	if (((VwRootBox*)prootb)->GetSynchronizer() &&
		((VwRootBox*)prootb)->GetSynchronizer()->IsExpandingLazyItems())
	{
		return S_OK;
	}

	IVwGraphicsPtr qvg;
	qvg.CreateInstance(CLSID_VwGraphicsWin32);
	IVwGraphicsWin32Ptr qvg32;
	Rect rcp(rcpDraw);
	CheckHr(qvg->QueryInterface(IID_IVwGraphicsWin32, (void **) &qvg32));
	BOOL fSuccess;
	if (m_hdcMem)
	{
		HBITMAP hbmp = (HBITMAP)::GetCurrentObject(m_hdcMem, OBJ_BITMAP);
		BOOL fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
		Assert(fSuccess);
		fSuccess = AfGdi::DeleteDC(m_hdcMem);
		Assert(fSuccess);
	}
	m_hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rcp.Width(), rcp.Height());
	Assert(hbmp);
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(m_hdcMem, hbmp);
	Assert(hbmpOld && hbmpOld != HGDI_ERROR);
	fSuccess = AfGdi::DeleteObjectBitmap(hbmpOld);
	Assert(fSuccess);
	if (bkclr == kclrTransparent)
		// if the background color is transparent, copy the current screen area in to the
		// bitmap buffer as our background
		::BitBlt(m_hdcMem, 0, 0, rcp.Width(), rcp.Height(), hdc, rcp.left, rcp.top, SRCCOPY);
	else
		AfGfx::FillSolidRect(m_hdcMem, Rect(0, 0, rcp.Width(), rcp.Height()), bkclr);
	CheckHr(qvg32->Initialize(m_hdcMem));
	VwPrepDrawResult xpdr = kxpdrAdjust;
	IVwGraphicsPtr qvgDummy; // Required for GetGraphics calls to get transform rects

	try
	{
		Rect rcDst, rcSrc;

		while (xpdr == kxpdrAdjust)
		{
			CheckHr(pvrs->GetGraphics(prootb, &qvgDummy, &rcSrc, &rcDst));
			rcDst.Offset(-rcp.left, -rcp.top);

			// Make sure our local graphics object (i.e. qvg32) contains the same dpi values
			// as the graphics object just returned to us via the GetGraphics call.
			int dpi;
			qvgDummy->get_XUnitsPerInch(&dpi);
			qvg32->put_XUnitsPerInch(dpi);
			qvgDummy->get_YUnitsPerInch(&dpi);
			qvg32->put_YUnitsPerInch(dpi);

			CheckHr(prootb->PrepareToDraw(qvg32, rcSrc, rcDst, &xpdr));
			CheckHr(pvrs->ReleaseGraphics(prootb, qvgDummy));
			qvgDummy.Clear();
		}
		// kxpdrInvalidate true means that expanding lazy boxes at the position we planned
		// to draw caused a nasty change in the scroll position, typically because
		// we were near the bottom, and expanding the lazy stuff at the bottom
		// did not yield a screen-ful of information. The entire window has
		// been invalidated, which will cause a new Paint, so do nothing
		// here. Otherwise, we can go ahead and draw.
		if (xpdr != kxpdrInvalidate)
		{
			// Note that we need to get these again at this point,
			// because PrepareToDraw may have made changes that alter the transformation
			// rectangles.
			CheckHr(pvrs->GetGraphics(prootb, &qvgDummy, &rcSrc, &rcDst));
			rcDst.Offset(-rcp.left, -rcp.top);
			Assert(rcSrc.Width());
			Assert(rcSrc.Height());
			Assert(rcDst.Width());
			Assert(rcDst.Height());

			// Make sure our local graphics object (i.e. qvg32) contains the same dpi values
			// as the graphics object just returned to us via the GetGraphics call.
			int dpi;
			qvgDummy->get_XUnitsPerInch(&dpi);
			qvg32->put_XUnitsPerInch(dpi);
			qvgDummy->get_YUnitsPerInch(&dpi);
			qvg32->put_YUnitsPerInch(dpi);

			CheckHr(prootb->DrawRoot(qvg, rcSrc, rcDst, fDrawSel));
			CheckHr(pvrs->ReleaseGraphics(prootb, qvgDummy));
			qvgDummy.Clear();
		}
	}
	catch (...)
	{
		if (qvgDummy)
			CheckHr(pvrs->ReleaseGraphics(prootb, qvgDummy));
		CheckHr(qvg->ReleaseDC());
		throw;
	}
	CheckHr(qvg->ReleaseDC());
	if (xpdr != kxpdrInvalidate)
	{
		// We drew something...now blast it onto the screen.
		::BitBlt(hdc, rcp.left, rcp.top, rcp.Width(), rcp.Height(), m_hdcMem, 0, 0, SRCCOPY);
	}

	END_COM_METHOD(g_factVDRB, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	Special drawing routine for rotated views. See Views.idh for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwDrawRootBuffered:: DrawTheRootRotated(IVwRootBox * prootb, HDC hdc, RECT rcpDraw,
	COLORREF bkclr, ComBool fDrawSel, IVwRootSite * pvrs, int nHow)
{
	BEGIN_COM_METHOD;
	if (nHow != 1)
		ThrowHr(WarnHr(E_INVALIDARG));

	IVwGraphicsPtr qvg;
	qvg.CreateInstance(CLSID_VwGraphicsWin32);
	IVwGraphicsWin32Ptr qvg32;
	Rect rcp;
	rcp.left = rcpDraw.top;
	rcp.top = rcpDraw.left;
	rcp.bottom = rcpDraw.right;
	rcp.right = rcpDraw.bottom;
	CheckHr(qvg->QueryInterface(IID_IVwGraphicsWin32, (void **) &qvg32));
	BOOL fSuccess;
	if (m_hdcMem)
	{
		HBITMAP hbmp = (HBITMAP)::GetCurrentObject(m_hdcMem, OBJ_BITMAP);
		BOOL fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
		Assert(fSuccess);
		fSuccess = AfGdi::DeleteDC(m_hdcMem);
		Assert(fSuccess);
	}
	m_hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rcp.Width(), rcp.Height());
	Assert(hbmp);
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(m_hdcMem, hbmp);
	Assert(hbmpOld && hbmpOld != HGDI_ERROR);
	fSuccess = AfGdi::DeleteObjectBitmap(hbmpOld);
	Assert(fSuccess);
	if (bkclr == kclrTransparent)
		// if the background color is transparent, copy the current screen area in to the
		// bitmap buffer as our background
		// REVIEW: do we need to rotate the screen area?
		::BitBlt(m_hdcMem, 0, 0, rcp.Width(), rcp.Height(), hdc, rcp.left, rcp.top, SRCCOPY);
	else
		AfGfx::FillSolidRect(m_hdcMem, Rect(0, 0, rcp.Width(), rcp.Height()), bkclr);
	CheckHr(qvg32->Initialize(m_hdcMem));
	IVwGraphicsPtr qvgDummy; // Required for GetGraphics calls to get transform rects

	try
	{
		Rect rcDst, rcSrc;

			CheckHr(pvrs->GetGraphics(prootb, &qvgDummy, &rcSrc, &rcDst));
			rcDst.Offset(-rcp.left, -rcp.top); // Review JohnT: curently always zero; should they be reversed?
			Assert(rcSrc.Width());
			Assert(rcSrc.Height());
			Assert(rcDst.Width());
			Assert(rcDst.Height());

			// Make sure our local graphics object (i.e. qvg32) contains the same dpi values
			// as the graphics object just returned to us via the GetGraphics call.
			int dpi;
			qvgDummy->get_XUnitsPerInch(&dpi);
			qvg32->put_XUnitsPerInch(dpi);
			qvgDummy->get_YUnitsPerInch(&dpi);
			qvg32->put_YUnitsPerInch(dpi);

			CheckHr(prootb->DrawRoot(qvg, rcSrc, rcDst, fDrawSel));
			CheckHr(pvrs->ReleaseGraphics(prootb, qvgDummy));
			qvgDummy.Clear();
	}
	catch (...)
	{
		if (qvgDummy)
			CheckHr(pvrs->ReleaseGraphics(prootb, qvgDummy));
		CheckHr(qvg->ReleaseDC());
		throw;
	}
	CheckHr(qvg->ReleaseDC());
	POINT rgptTransform[3];
	rgptTransform[0].x = rcpDraw.right; // upper left of actual drawing maps to top right of rotated drawing
	rgptTransform[0].y = rcpDraw.top;
	rgptTransform[1].x = rcpDraw.right;
	rgptTransform[1].y = rcpDraw.bottom; // upper right of actual drawing maps to bottom right of rotated drawing.
	rgptTransform[2].x = rcpDraw.left;
	rgptTransform[2].y = rcpDraw.top; // bottom left of actual drawing maps to top left of rotated drawing.
		// We drew something...now blast it onto the screen.
	::PlgBlt(hdc, rgptTransform, m_hdcMem, 0, 0, rcp.Width(), rcp.Height(), 0, 0, 0);

	END_COM_METHOD(g_factVDRB, IID_IVwRootBox);
}
/*----------------------------------------------------------------------------------------------
	This must ONLY be used for RootSite or other classes where GetGraphics returns the
	one-and-only coordinate transformation.
	This must ONLY be used following a successful call to DrawTheRoot, where expansion
	did not require a complete invalidation of the rootsite's client area. It is intended for
	redrawing the exact same content as the previous call (for situations when data may be in
	flux but it is expedient to keep the view looking nice even though it isn't "live" data.
	This method merely re-blits the previously computed bitmap to the device context.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwDrawRootBuffered:: ReDrawLastDraw(HDC hdc, RECT rcpDraw)
{
	BEGIN_COM_METHOD;
	Assert(hdc);
	Assert(m_hdcMem);
	Rect rcp(rcpDraw);
	::BitBlt(hdc, rcp.left, rcp.top, rcp.Width(), rcp.Height(), m_hdcMem, rcp.left, rcp.top,
		SRCCOPY);
	END_COM_METHOD(g_factVDRB, IID_IVwRootBox);
}

/*----------------------------------------------------------------------------------------------
	This is a drawing routine for print layout views. It handles double buffering
	but assumes that lazy boxes in the drawing area have already been dealt with.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwDrawRootBuffered::DrawTheRootAt(IVwRootBox * prootb, HDC hdc, RECT rcpDraw,
	COLORREF bkclr, ComBool fDrawSel, IVwGraphics * pvg, RECT rcSrc, RECT rcDst, int ysTop,
	int dysHeight)
{
	BEGIN_COM_METHOD;

	HBITMAP hbmpOld;
	IVwGraphicsWin32Ptr qvg32;
	qvg32.CreateInstance(CLSID_VwGraphicsWin32);
	Rect rcp(rcpDraw);
	Rect rcFill(0, 0, rcp.Width(), rcp.Height());
	//CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **) &qvg32));
	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rcp.Width(), rcp.Height());
	Assert(hbmp);
	hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	Assert(hbmpOld && hbmpOld != HGDI_ERROR);
	if (bkclr == kclrTransparent)
		// if the background color is transparent, copy the current screen area in to the
		// bitmap buffer as our background
		::BitBlt(hdcMem, 0, 0, rcp.Width(), rcp.Height(), hdc, rcp.left, rcp.top, SRCCOPY);
	else
		AfGfx::FillSolidRect(hdcMem, rcFill, bkclr);
	CheckHr(qvg32->Initialize(hdcMem));
	CheckHr(qvg32->put_XUnitsPerInch(rcDst.right - rcDst.left));
	CheckHr(qvg32->put_YUnitsPerInch(rcDst.bottom - rcDst.top));

	try
	{
		CheckHr(prootb->DrawRoot2(qvg32, rcSrc, rcDst, fDrawSel, ysTop, dysHeight));
	}
	catch (...)
	{
		CheckHr(qvg32->ReleaseDC());
		throw;
	}
	CheckHr(qvg32->ReleaseDC());

	::BitBlt(hdc, rcp.left, rcp.top, rcp.Width(), rcp.Height(), hdcMem, 0, 0, SRCCOPY);

	// Clean up memory DC.
	HBITMAP hbmpDebug;
	hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
	BOOL fSuccess;
	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
	Assert(fSuccess);

	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	END_COM_METHOD(g_factVDRB, IID_IVwRootBox);
}
#endif


/*----------------------------------------------------------------------------------------------
	Most boxes can't yet save an accessible name. Rootbox overrides.
----------------------------------------------------------------------------------------------*/
void VwRootBox::SetAccessibleName(BSTR bstrName)
{
	m_stuAccessibleName = bstrName;
}

//:>********************************************************************************************
//:>	IServiceProvider methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	This method allows root box to provide access to other objects, based on what interface
	is wanted. The only one so far implemented is an implemenation of IAccessible.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRootBox::QueryService(REFGUID guidService, REFIID riid, void ** ppv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppv);
#if WIN32 // IAccessible not implemented
	if (riid == IID_IAccessible)
	{
		IDispatchPtr qacc;
		VwAccessRoot::GetAccessFor(this, &qacc);
		*ppv = qacc.Detach();
	}
	else
#endif
	{
		//::MessageBox(NULL, L"RootBox QueryService failed", L"Trace", MB_OK);
		return E_NOINTERFACE;
	}
	END_COM_METHOD(g_fact, IID_IServiceProvider);
}

STDMETHODIMP VwRootBox::get_Synchronizer(IVwSynchronizer ** ppsync)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsync);
	*ppsync = GetSynchronizer();
	if (*ppsync)
		(*ppsync)->AddRef();

	END_COM_METHOD(g_fact, IID_IVwSynchronizer);
}

inline bool VwRootBox::OnMouseEvent(int xd, int yd, RECT rcSrc, RECT rcDst, VwMouseEvent me)
{
#ifdef ENABLE_TSF
	if (m_qvim)
	{
		ComBool fHandled;
		CheckHr(m_qvim->OnMouseEvent(xd, yd, rcSrc, rcDst, me, &fHandled));
		return fHandled;
	}
#endif /* ENABLE_TSF */
	return false;
}
