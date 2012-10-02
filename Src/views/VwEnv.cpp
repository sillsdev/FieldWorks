/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwEnv.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	See header file.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

// Dummy factory for END_COM_METHOD macro.
static DummyFactory dfactEnv(_T("Sil.Views.VwEnv"));
//:>********************************************************************************************
//:>	Constructor/Destructor/Initializer
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwEnv::VwEnv()
{
	// COM object behavior
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

	// misc. variable initializations
	Assert(m_fObjectOpen == false);  // we expect an object to be opened before an attribute
	Assert(m_fTableRow == false);
	Assert(m_chvoProp == 0);
	Assert(m_cnsi == 0);
	Assert(m_pgboxCurr == NULL);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwEnv::~VwEnv()
{
	ModuleEntry::ModuleRelease();
}

VwPropertyStore * VwEnv::MakePropertyStore()
{
	return NewObj VwPropertyStore();
}

/*----------------------------------------------------------------------------------------------
	Initialize a VwEnv for creating a new view.
	Arguments:
		pvg - gives initial clip rect
----------------------------------------------------------------------------------------------*/
void VwEnv::Initialize(IVwGraphics * pvg, VwRootBox * pzrootb, IVwViewConstructor * pvc)
{
	m_qzvps.Attach(MakePropertyStore()); // constructor does default state
	IVwStylesheetPtr qss;
	qss = pzrootb->Stylesheet();
	m_qzvps->SetStyleSheet(qss);
	// set the initial root text props
	m_qzvps->InitRootTextProps(pvc);

	pzrootb->_SetPropStore(m_qzvps);
	m_qrootbox = pzrootb;
	m_qsda = pzrootb->GetDataAccess();
	// Set the writing system factory for the property store if we can.
	if (m_qsda)
	{
		ILgWritingSystemFactoryPtr qwsf;
		CheckHr(m_qsda->get_WritingSystemFactory(&qwsf));
		if (qwsf)
			CheckHr(m_qzvps->putref_WritingSystemFactory(qwsf));
	}

	// Save it as the 'initial state' to return to after closing a flow object in the root.
	m_vpzvpsInitialStyles.Push(m_qzvps);

	m_qvg = pvg;
	m_pgboxCurr = pzrootb;
	m_vpgboxOpen.Push(m_pgboxCurr);

	Assert(!m_fObjectOpen);
	Assert(!m_fTableRow);
	Assert(m_chvoProp == 0);
	Assert(m_cnsi == 0);
}

/*----------------------------------------------------------------------------------------------
	Initialize a VwEnv for creating a MoveablePileBox, a subview embedded in another view
	for an object embedded in a string.
----------------------------------------------------------------------------------------------*/
void VwEnv::InitEmbedded(IVwGraphics * pvg, VwMoveablePileBox * pmpbox)
{
	m_qzvps = pmpbox->Style();

	m_qrootbox = pmpbox->Root();
	m_qsda = m_qrootbox->GetDataAccess();

	// Save it as the 'initial state' to return to after closing a flow object in the root.
	m_vpzvpsInitialStyles.Push(m_qzvps);

	m_qvg = pvg;
	m_pgboxCurr = pmpbox;
	m_vpgboxOpen.Push(m_pgboxCurr);
}


/*----------------------------------------------------------------------------------------------
	Initialize a VwEnv for regenerating part of a display.
	Arguments:
		pvg - a graphics object set up for measuring in the appropriate DC
		pzrootb - a root box, typically a dummy one, into which we can
			add notifiers.
		pgboxContainer - the group box inside which the regenerated display is
			to go. Its current contents are saved by InitRegenerate, and
			restored by GetRegenerateInfo.
		pzvps - the property store that should be current for the call to the
			view constructor.
		hvo - the object one of whose property displays is being regenerated.
		pvbldrec - information about outer objects and properties the one regenerated is part of
		iprop - index of the regenerated property in the containing object.

----------------------------------------------------------------------------------------------*/
void VwEnv::InitRegenerate(IVwGraphics * pvg, VwRootBox * pzrootb,
	VwGroupBox * pgboxContainer, VwPropertyStore * pzvps, HVO hvo, BuildVec * pvbldrec,
	int iprop)
{
	m_qvg = pvg;
	m_qrootbox = pzrootb;
	m_qsda = pzrootb->GetDataAccess();

	// We are always regenerating some particular property.
	m_fObjectOpen = true;

	// We are going to put the generated boxes in here.
	// Make it temporarily empty so we can distinguish the new ones.
	// Save the old contents to restore when we get done.
	// Typically, this is the actual group box that the new ones will go into,
	// so it has any context information we might want from it, except preceding boxes.
	m_pgboxCurr = pgboxContainer;
	VwGroupBox * pgboxParent = m_pgboxCurr->Container();
	if (pgboxParent)
		m_vpgboxOpen.Push(pgboxParent);

	m_vpgboxOpen.Push(m_pgboxCurr);

	// (string factory and string builder are created as needed.)

	m_qzvps = pzvps;

	m_fTableRow = (dynamic_cast<VwTableRowBox *>(m_pgboxCurr) != NULL);

	// We should not close any flow object that is not opened during the regenerate.
	// But, if we open, then close, then open another, the initial state for the
	// other should be the reset style of the containing flow object, which
	// might not be either pzvps or the style of pgboxContainer. How can we find it?
	// Consider this example...

	// case ktagSense
	//	...
	//	pvwenv->//point size smaller
	//	pvwenv->OpenSpan();
	//	pvwenv->//bolder
	//	pvwenv->AddObjProp(ktagExample, this, kfragExample);
	//	pvwenv->CloseSpan();
	//	...
	//case ktagExample:
	//	AddStringProp(ktagVernacular, this);
	//	AddString(" ");
	//	AddStringProp(ktagFreeTrans, this);
	//	break;

	// pzvps will correctly be the smaller, bolder style and, so the vernacular will
	// come out right. But where can we get the "smaller" style of the span to use
	// for ktagFreeTrans? It is of course one of the parents of the bolder, smaller
	// style we saved--but which one? I think it has to be the closest parent which
	// is the reset style of the next parent above that...
	m_vpzvpsInitialStyles.Push(pzvps->InitialStyle());

	m_hvoCurr = hvo;
	// (an object is open, so we have not added any yet to any current prop.
	// m_chvoProp can stay 0).

	// We need to fill in object, tag, and item number info in fake NotifierStackItems,
	// in case the regenerate process uses context info.
	m_cnsi = pvbldrec->Size() - 1; // Build recs includes one for the current object
	for (int i = 0; i < m_cnsi; i++)
	{
		NotifierStackItem * pnsi = NewObj NotifierStackItem();
		pnsi->hvo = (*pvbldrec)[i].hvo;

		// The build rec [i] has info about where its object occurs in rec i-1's property.
		// The pnsi rec [i] has info about how many objects were opened in object i's prop up to
		// and including the object at level i+1 (or the current object). This means the count
		// we want for pnsi[i] comes from the [i+1] build rec. That is OK because there is one
		// more build rec than nsi's.
		pnsi->chvoProp = (*pvbldrec)[i + 1].ihvo + 1;

		NotifierRec * pnoterec = GetNotifierRec();

		// Context info looks at the last notifier rec only, to get tag info.
		// We don't need to rebuild anything else.
		pnoterec->tag = (*pvbldrec)[i].tag;
		pnsi->noterecvec.Push(pnoterec);
		m_nsivecStack.Push(pnsi);
	}

	// (m_noterecvecCurr can be empty. We won't do a closeObject on the currently
	// open one, so anything put there does not get used.) However, we need to set
	// these variables so the PropIndex of top-level object notifiers can be
	// properly set.
	m_ipropAdjust = iprop;
	m_insiAdjustLev = m_cnsi;

	// (m_noterecvecOpenProps can stay empty. We don't have any pending props that need to know
	// about new flow objects)

	// Do this last, after we are sure nothing is going to go wrong.
	m_pboxSaveContainer = m_pgboxCurr;
	m_pboxSaveLast = m_pgboxCurr->LastBox();
	m_pboxSaveFirst = m_pgboxCurr->RemoveAllBoxes();
}


//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwEnv::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwEnv)
		*ppv = static_cast<IVwEnv *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwEnv);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


/*----------------------------------------------------------------------------------------------
	Display an (atomic) object prop. Calls Display on the view constructor.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddObjProp(PropTag tag, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvvc);

	OpenProp(tag, pvvc, frag, kvnpObjProp);

	HVO hvoItem;
	CheckHr(m_qsda->get_ObjectProp(m_hvoCurr, tag, &hvoItem));
	if (hvoItem != 0)
	{
		// Currently CloseObject crashes (creating a notifier for a null object asserts)
		// if hvoItem is null. So do nothing in that case.
		// ENHANCE JohnT: We may want to improve what happens when hvoItem is null,
		// that is, the property we want to show is missing.
		// Do we want Cellar-1 functionality of displaying a string and causing an object to be
		// created and inserted if the string is edited? For now, if that is wanted I think
		// the client should do it using DisplayVariant.
		OpenObject(hvoItem);
		CheckHr(pvvc->Display(this, hvoItem, frag));
		CloseObject();
	}
	else
	{
		CheckHr(NoteDependency(&m_hvoCurr, &tag, 1));
	}
	CloseProp();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Display a vector using a view constructor (calls DisplayVec)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddObjVec(PropTag tag, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvvc);

	OpenProp(tag, pvvc, frag, kvnpObjVec);
	CheckHr(pvvc->DisplayVec(this, m_hvoCurr, tag, frag));
	CloseProp();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Display a vector using a view constructor on each item in turn (calls Display)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddObjVecItems(int tag, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvvc);

	OpenProp(tag, pvvc, frag, kvnpObjVecItems);

	HVO hvoItem;
	int cobj;
	CheckHr(m_qsda->get_VecSize(m_hvoCurr,tag, &cobj));

	for (int i = 0; i < cobj; i++)
	{
		CheckHr(m_qsda->get_VecItem(m_hvoCurr, tag, i, &hvoItem));
		OpenObject(hvoItem);
		CheckHr(pvvc->Display(this, hvoItem, frag));
		CloseObject();
	}

	CloseProp();
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Display a vector in reverse order using a view constructor on each item in turn (calls
	Display)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddReversedObjVecItems(int tag, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvvc);

	OpenProp(tag, pvvc, frag, kvnpObjVecItems);

	HVO hvoItem;
	int cobj;
	CheckHr(m_qsda->get_VecSize(m_hvoCurr,tag, &cobj));

	for (int i = cobj - 1; i >= 0; --i)
	{
		CheckHr(m_qsda->get_VecItem(m_hvoCurr, tag, i, &hvoItem));
		OpenObject(hvoItem);
		CheckHr(pvvc->Display(this, hvoItem, frag));
		CloseObject();
	}

	CloseProp();
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Display part of a vector using a view constructor on each item in turn (calls Display).
	This is used when regenerating to regenerate just part of a list.
----------------------------------------------------------------------------------------------*/
void VwEnv::AddObjVecItemRange(int tag, IVwViewConstructor * pvvc, int frag,
	int ihvoMin, int ihvoLim)
{
	// If it's not the first item in the sequence, instead of starting with any special
	// properties that may apply to that first item, start with the reset properties
	// that we use for subsequent items.
	if (ihvoMin != 0)
		m_qzvps = *(m_vpzvpsInitialStyles.Top());
	OpenProp(tag, pvvc, frag, kvnpObjVecItems, ihvoMin);

	HVO hvoItem;
	int cobj;
	CheckHr(m_qsda->get_VecSize(m_hvoCurr,tag, &cobj));
	Assert(cobj >= ihvoLim);

	for (int i = ihvoMin; i < ihvoLim; i++)
	{
		CheckHr(m_qsda->get_VecItem(m_hvoCurr, tag, i, &hvoItem));
		OpenObject(hvoItem);
		CheckHr(pvvc->Display(this, hvoItem, frag));
		CloseObject();
	}

	CloseProp();
}

/*----------------------------------------------------------------------------------------------
	Embed a display of another object, not one of your own properties.
	May also be used (e.g., by DisplayVec) to embed an object that is a member of a
	property.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddObj(HVO hvo, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvvc);

	bool fObjectWasOpen = m_fObjectOpen;

	if (m_fObjectOpen)
	{
		// Embedding an arbitrary object, not part of a property
		OpenProp(ktagNotAnAttr, pvvc, frag, kvnpNone);
	} // else, embedding what is assumed to be a member object of the current propery

	OpenObject(hvo);
	CheckHr(pvvc->Display(this, hvo, frag));
	CloseObject();

	if (fObjectWasOpen)
	{
		// Need to close the fake property we opened at the start of the method
		CloseProp();
	}

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Display a vector property using laziness.
	Nothing is added to the display immediately, but at some point the views code will call the
	${IVwViewConstructor#EstimateHeight} method to find out how high one or more items are.
	At that time or later, it may call your ${IVwViewConstructor#LoadData} method,
	followed by the ${IVwViewConstructor#Display} method,
	for one or more items in the property, as needed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddLazyVecItems(int tag, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvvc);
	// This test is probably only to let the null tests pass.
	if (!m_pgboxCurr)
		ReturnHr(E_UNEXPECTED);
	// We now allow divisions that aren't in other divisions (e.g., in a table cell), but
	// ALL containers must be divisions for lazines to work.
	for (VwGroupBox * pgbox = m_pgboxCurr; pgbox; pgbox = pgbox->Container())
		if (!dynamic_cast<VwDivBox *>(pgbox))
			ThrowHr(WarnHr(E_UNEXPECTED));

	VwLazyBox * plzb = 0;
	try
	{
		OpenProp(tag, pvvc, frag, kvnpLazyVecItems);
		int cobj;
		CheckHr(m_qsda->get_VecSize(m_hvoCurr,tag, &cobj));
		if (cobj) // otherwise don't make the lazy box at all.
		{
			plzb = NewObj VwLazyBox(m_qzvps, NULL, 0, 0, pvvc, frag, m_hvoCurr);
			plzb->m_vwlziItems.EnsureSpace(cobj);
			for (int i = 0; i < cobj; i++)
			{
				HVO hvoItem;
				CheckHr(m_qsda->get_VecItem(m_hvoCurr, tag, i, &hvoItem));
				plzb->m_vwlziItems.Push(hvoItem);
			}
			// Note that we don't open or close a flow object. A lazy box is not a flow object.
			AddBox(plzb); // Before close prop, it is the display of this property.
		}
		CloseProp();
	}
	catch(...)
	{
		if (plzb)
			delete plzb;
		throw;
	}

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Display a list of items using laziness. Typically, this is used to implement
	DisplayVec, either of the complete property contents, or a filtered subset.
	Nothing is added to the display immediately, but at some point the views code will call the
	${IVwViewConstructor#EstimateHeight} method to find out how high one or more items are.
	At that time or later, it may call your ${IVwViewConstructor#LoadData} method,
	followed by the ${IVwViewConstructor#Display} method,
	for one or more items in the list, as needed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddLazyItems(HVO * prghvo, int chvo, IVwViewConstructor * pvvc,
	int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvvc);

	VwLazyBox * plzb = 0;
	try
	{
		plzb = NewObj VwLazyBox(m_qzvps, prghvo, chvo, 0, pvvc, frag, m_hvoCurr);
		// Note that we don't open or close a flow object. A lazy box is not a flow object.
		AddBox(plzb); // Before close prop, it is the display of this property.
	}
	catch(...)
	{
		if (plzb)
			delete plzb;
		throw;
	}

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Generic basic property displays, managed by the VC.
	Display a property, with the client handling the process of formatting it as a string. (The
	view constructor will be asked to DisplayVariant, and the resulting	string inserted into the
	display.) If the user edits the data, then when focus leaves the display of the property,
	the system calls its IViewConstructor::UpdateProp method. That method is responsible to make
	an appropriate change to the underlying	data, or veto it with an appropriate error message.
	This may also be used for string alts. The Alternation object gets passed as an IUnknown
	variant.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddProp(int tag, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvvc);

#if WIN32
	SmartVariant v;
#else
	// TODO-Linux: Why don't we use the SmartVariant?
	VARIANT v;
#endif
	ITsStringPtr qtssNew;

	// If we don't already have a paragraph open, make one. AddString would do this for us,
	// but it works better for identifying properties as potentially editable strings
	// if the property is opened inside the paragraph.
	bool fHadPara = dynamic_cast<VwParagraphBox *>(m_pgboxCurr) != NULL;
	if (!fHadPara)
		OpenParagraph();
	OpenProp(tag, pvvc, frag, kvnpProp);
	CheckHr(pvvc->DisplayVariant(this, tag, frag, &qtssNew));
	// ENHANCE JohnT: do we need a new AddStringCore that does not assume it is a literal?
	AddString(qtssNew);
	CloseProp();
	// If a paragraph was not open before we added the string, close it
	if (!fHadPara)
		CloseParagraph();
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	This is similar, but used where it is necessary to follow a path of	attributes to get to the
	desired property. The first tag in prgtag indicates an atomic object property of the current
	open object, the next an atomic object property of the one obtained from the first property,
	and so on until the last indicates the property that is represented by the string. This last
	tag (and the corresponding object) is what gets	passed to UpdateProp(). If any of the attrs
	along the path is null, nothing gets added.	DisplayVariant is used to display the property
	(which must be basic).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddDerivedProp(int * prgtag, int ctag, IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvvc);
	//ChkComOutPtr(prgtag);
	// ENHANCE JohnT: implement. Just requires a loop opening objects and props, and reading the
	// prop, until we get to the end; then call DisplayVariant. No, a bit trickier, because for
	// intermediate props we don't have a valid constructor/frag pair in case we need to
	// regenerate. A new notifier type may be more efficient, anyway.
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Inform the view of special dependencies. The current flow object (and anything else
	that is part of the same object in the same higher level property) needs to be
	regenerated if any of the listed properties changes.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::NoteDependency(HVO * prghvo, PropTag * prgtag, int chvo)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prghvo, chvo);
	ChkComArrayArg(prgtag, chvo);
	Assert(m_pgboxCurr);

	VwPropListNotifierPtr qpln;
	qpln.Attach(VwPropListNotifier::Create(m_chvoProp - 1, chvo));
	qpln->SetObject(m_hvoCurr); // probably not meaningful, but all notifiers have it...
	m_vpanoteIncomplete.Push(qpln);
	// This is vital! Tells it which box to regenerate.
	qpln->_KeyBox(m_pgboxCurr);
	CopyItems(prghvo, qpln->Objects(), chvo);
	CopyItems(prgtag, qpln->Tags(), chvo);
	m_qrootbox->AddNotifier(qpln);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Inform the view of special dependencies. The current flow object (and anything else
	that is part of the same object in the same higher level property) needs to be
	regenerated if there is a change (from the time where this method is called)
	in whether it is true that ptssVal is equal to the specified property.
	It is a multilingual string property if ws is non-zero, otherwise a plain string.
	Note that this can be VERY much more efficient than using NoteDependency for such
	conditions, which will regenerate every time the property changes.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::NoteStringValDependency(HVO hvo, PropTag tag, int ws, ITsString * ptssVal)
{
	BEGIN_COM_METHOD;

	VwStringValueNotifierPtr qsvnote;
	qsvnote.Attach(NewObj VwStringValueNotifier(hvo, tag, ws, ptssVal, m_qsda));
	m_vpanoteIncomplete.Push(qsvnote);
	// This is vital! Tells it which box to regenerate.
	qsvnote->_KeyBox(m_pgboxCurr);
	m_qrootbox->AddNotifier(qsvnote);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Add a string which is generated by the client as a composite of multiple properties,
	possibly of multiple objects. The string will be generated by calling DisplayVariant
	(passing an IUnknown pointing to the object that is open when AddMultiProp is called). If
	the user edits the string, UpdateProp is called, with the object that is open when
	AddMultiProp is called, and ktagNil.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddMultiProp(HVO * prghvo, int * prgtag, int chvo,
	IVwViewConstructor * pvvc, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prghvo, chvo);
	ChkComArrayArg(prgtag, chvo);
	ChkComArgPtrN(pvvc);
	// ENHANCE: JohnT: implement. May require a new notifier type. May be implementable in terms of
	// StartDependency.
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Inform the view of special dependencies. The part of the view embedded between
	StartDependency and the matching EndDependency will be regenerated if any of the indicated
	properties changes (in addition to any dependencies	the view is already aware of).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::StartDependency(HVO * prghvo, int * prgtag, int chvo)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prghvo, chvo);
	ChkComArrayArg(prgtag, chvo);
	// ENHANCE: JohnT: implement. May require a new notifier type.
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	See comment for StartDependency
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::EndDependency()
{
	BEGIN_COM_METHOD;
	// ENHANCE: JohnT: implement.
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


//:>********************************************************************************************
//:>	Inserting basic object displays into the view.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Inserting basic object property displays into the view.
	The view looks up the value of the indicated property on the current open object and
	displays it. The property must be of the indicated type.

	Note that we have to duplicate a little of the work done in AddString to force the
	creation of a paragraph if needed, because we need the open attribute to be inside the
	auto-added paragraph.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddStringProp(PropTag tag, IVwViewConstructor * pvwvc)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvwvc);

	ITsStringPtr qtss;
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	if (!pvpbox)
		OpenParagraph();
	OpenProp(tag, pvwvc, 0, kvnpStringProp);
	CheckHr(m_qsda->get_StringProp(m_hvoCurr, tag, &qtss));
	CheckHr(AddString(qtss));
	CloseProp();
	if (!pvpbox)
		CloseParagraph();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

STDMETHODIMP VwEnv::AddUnicodeProp(int tag, int ws, IVwViewConstructor * pvwvc)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvwvc);

	ITsStringPtr qtss;
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	if (!pvpbox)
		OpenParagraph();
	OpenProp(tag, pvwvc, ws, kvnpUnicodeProp);
	SmartBstr sbstr;
	CheckHr(m_qsda->get_UnicodeProp(m_hvoCurr, tag, &sbstr));
	GetStringFactory(&m_qtsf);
	CheckHr(m_qtsf->MakeStringRgch(sbstr.Chars(), sbstr.Length(), ws, &qtss));
	AddString(qtss);
	CloseProp();
	if (!pvpbox)
		CloseParagraph();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Similarly for ints,
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddIntProp(int tag)
{
	BEGIN_COM_METHOD;

	int nVal;
	ITsStringPtr qtss;
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	if (!pvpbox)
		OpenParagraph();
	OpenProp(tag, NULL, 0, kvnpIntProp);
	CheckHr(m_qsda->get_IntProp(m_hvoCurr, tag, &nVal));
	IntToTsString(nVal, &m_qtsf, m_qsda, &qtss);
	AddString(qtss);
	CloseProp();
	if (!pvpbox)
		CloseParagraph();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	The view looks up the value of the indicated property in the current open
	object and passes it to the view constructor's DisplayPicture method.
	If nMax is greater than nMin, automatic cycling is performed: clicking on one
	of the pictures will not make a selection, but will merely increment the
	specified property (and rotate from nMax to nMin).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddIntPropPic(int tag, IVwViewConstructor * pvc, int frag, int nMin,
	int nMax)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvc);
	int nVal;
	CheckHr(m_qsda->get_IntProp(m_hvoCurr, tag, &nVal));
	IPicturePtr qpic;
	CheckHr(pvc->DisplayPicture(this, m_hvoCurr, tag, nVal, frag, &qpic));
	OpenProp(tag, pvc, frag, kvnpIntPropPic);
	VwIntegerPictureBox * pipbox = new VwIntegerPictureBox(m_qzvps, qpic, m_hvoCurr, tag,
		nMin, nMax);
	AddLeafBox(pipbox);
	CloseProp();
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	String alternations.
	Add a single, isolated alternative (of the indicated attr of the open object).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddStringAltMember(int tag, int ws, IVwViewConstructor * pvwvc)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvwvc);

	ITsStringPtr qtss;
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	if (!pvpbox)
		OpenParagraph();
	OpenProp(tag, pvwvc, ws, kvnpStringAltMember);
	CheckHr(m_qsda->get_MultiStringAlt(m_hvoCurr, tag,ws, &qtss));
	AddString(qtss);
	CloseProp();
	if (!pvpbox)
		CloseParagraph();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Add all the alternatives present, in a default format labelled by writing system tag markers.
	If the current flow object is a paragraph, the items are separated with a space. If the
	current flow object is a pile (Div or InnerPile), each alternative (with identifying tag)
	is a row.
	ENHANCE: JohnT: what controls the order of the encodings?  As found?	Alphabetical by enc?
		Numeric by enc?
	ENHANCE JohnT: this isn't really fully designed or implemented yet. Do so sometime.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddStringAlt(int tag)
{
	BEGIN_COM_METHOD;

	ITsMultiStringPtr qtms;
	int ctss;
	ITsStringPtr qtss;
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);

	OpenProp(tag, NULL, 0, kvnpStringAlt);
	CheckHr(m_qsda->get_MultiStringProp(m_hvoCurr, tag, &qtms));
	CheckHr(qtms->get_StringCount(&ctss));

	// ENHANCE JohnT: is this the right order? Should we sort by encoding?
	for (int i = 0; i < ctss; i++)
	{
		int ws;

		CheckHr(qtms->GetStringFromIndex(i, &ws, &qtss));

		// ENHANCE: JohnT: insert an writing system label
		// ENHANCE: JohnT: figure a way to keep track of what is data and what is value, and to be
		// able to know which alternative the user edited, if he does.
		// ENHANCE: JohnT: make a gap between the label and the string
		if (pvpbox && i < ctss - 1)
		{
			// ENHANCE JohnT: Make a gap between this alternative and the next
			// Should we insert an English space? Or set a property to make a gap after the
			// label box? Depends somewhat on whether we make the label its own box, or just a
			// substring with special properties...
		}

		// ENHANCE JohnT: do we need a new AddStringCore that does not assume it is a literal?
		AddString(qtss);
	}
	CloseProp();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Display the given list of encodings, with ws tags. An ws appears even if the corresponding
	alternative is absent.
	ENHANCE JohnT: implement fully, as noted below.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddStringAltSeq(int tag, int * prgenc, int cws)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgenc, cws);

	ITsStringPtr qtss;
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);

	for (int i = 0; i < cws; i++)
	{

		CheckHr(m_qsda->get_MultiStringAlt(m_hvoCurr, tag, prgenc[i], &qtss));

		// ENHANCE JohnT: insert an writing system label
		// ENHANCE JohnT: make a gap between the label and the string
		// ENHANCE JohnT: if current container is pile, make a para for each alternative.
		// Otherwise, put space between.
		if (!pvpbox)
			OpenParagraph();
		else if (i < cws - 1)
		{
			// ENHANCE JohnT: Make a gap between this alternative and the next
			// Should we insert an English space? Or set a property to make a
			// gap after the label box? Depends somewhat on whether we make the
			// label its own box, or just a substring with special properties...
		}

		OpenProp(tag, NULL, prgenc[i], kvnpStringAltSeq);
		AddString(qtss);
		CloseProp();
		if (!pvpbox)
			CloseParagraph();
	}
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Add literal text that is not a property and not editable.

	Note: currently also used for adding editable strings. Sometime we may need to make
	more distinction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddString(ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);

	// If we don't already have a paragraph open, make one.
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	bool fHadPara = pvpbox != NULL;
	if (!pvpbox)
	{
		OpenParagraph();
		pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	}
	// Do this before adding the string to the text source: the number of things in the
	// text source indicates the index where the property starts.
	int itssProp = pvpbox->Source()->Vpst().Size();
	for (int i = 0; i < m_noterecvecOpenProps.Size(); ++i)
	{
		(m_noterecvecOpenProps[i])->pbox = pvpbox;
		(m_noterecvecOpenProps[i])->itssStart = itssProp;
	}
	m_noterecvecOpenProps.Clear();
	// OPTIMIZE JohnT: we don't actually need the pvvc unless it is a mapped text source,
	// and then only in the unlikely case that the literal string contains an ORC.
	// Is it worth testing for it and skipping this if not?
	IVwViewConstructor * pvvc = NULL;
	if (m_noterecvecCurr.Size())
	{
		NotifierRec * pnoterecLast = *(m_noterecvecCurr.Top());
		pvvc = pnoterecLast->qvvc;
		if ((!pvvc) && m_nsivecStack.Size() >= 1)
		{
			// The next outer one, if any, must have one.
			NotifierStackItem * pnsi = *(m_nsivecStack.Top());
			NotifierRecVec & noterecvec = pnsi->noterecvec;
			if (noterecvec.Size())
			{
				pnoterecLast = *(noterecvec.Top());
				pvvc = pnoterecLast->qvvc;
			}
		}
	}
	pvpbox->Source()->AddString(ptss, m_qzvps, pvvc);
//	MakeEmbeddedBoxes(pvpbox, ptss, pvvc);

	// If a paragraph was not open before we added the string, close it
	if (!fHadPara)
		CloseParagraph();
	// Style of next string added will be the default style for the current containing
	// flow object, unless otherwise set.
	m_qzvps = *(m_vpzvpsInitialStyles.Top());

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

///*----------------------------------------------------------------------------------------------
//	Add any VwMoveablePileBoxes required for the string.
//----------------------------------------------------------------------------------------------*/
//void VwEnv::MakeEmbeddedBoxes(VwParagraphBox * pvpbox, ITsString * ptss,
//	IVwViewConstructor * pvvc)
//{
//	int crun;
//	CheckHr(ptss->get_RunCount(&crun))
//	for (int irun = 0; irun < crun; irun++)
//	{
//		TsRunInfo tri;
//		ITsPropsPtr qttp;
//		CheckHr(qtss->FetchRunInfo(irun, &tri, &qttp));
//
//		if (tri.ichLim - tri.ichMin == 1)
//		{
//			// One character in run...see if it is ORC
//			OLECHAR ch;
//			CheckHr(qtss->FetchChars(tri.ichMin, tri.ichLim, &ch));
//			if (ch == L'\xfffc')
//			{
//				SmartBstr sbstrObjData;
//				CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstrObjData));
//				// ...and the type we handle this way...
//				if (sbstrObjData.Length() == 9 &&
//					sbstrObjData.Chars()[0] == kodtGuidMoveableObjDisp)
//				{
//					GUID guid;
//					::memcpy(&guid, sbstrObjData.Chars() + 1, isizeof(guid));
//					int hvoEmbedded;
//					CheckHr(pvvc->GetIdFromGuid(guid, &hvoEmbedded));
//					OpenObject(hvoEmbedded);
//					VwMoveablePileBox * pmpbox = NewObj VwMoveablePictureBox(m_qzvps);
//					OpenFlowObject(pmpbox);
//					CheckHr(pvvc->DisplayEmbeddedObject(this, hvoEmbedded));
//					CloseFlowObject();
//					CloseObject();
//				}
//			}
//		}
//	}
//}

/*----------------------------------------------------------------------------------------------
	Insert a picture along with the specified ws alternative of the caption. Mark it as if it
	came from the property tag, though we don't actually read data from there (the caller can
	pass any value that might be useful later to identify something about the picture the user
	clicked). If the tag argument is not useful, pass ktagNotAnAttr.
	It is planned that a positive dxmpHeight is a maximum height, negative is an exact
	height, and 0 means don't specify height (use natural height or determine from
	width and aspect ratio). Similarly for width.
	Currently only max height is implemented. Always pass zero for width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddPictureWithCaption(IPicture * ppict, PropTag tag,
	ITsTextProps * pttpCaption, HVO hvoCmFile, int ws, int dxmpWidth, int dympHeight,
	IVwViewConstructor * pvwvc)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ppict);

	CheckHr(AddPicture(ppict, tag, dxmpWidth, dympHeight));
	HVO rghvo[2] = {m_hvoCurr, hvoCmFile};
	int rgflid[2] = {kflidCmPicture_PictureFile, kflidCmFile_InternalPath};
	CheckHr(NoteDependency(rghvo, rgflid, 2));

	// Add the picture caption.
	// Paragraphs can only go inside some kind of pile
	Assert(dynamic_cast<VwPileBox *>(m_pgboxCurr));
	CheckHr(put_Props(pttpCaption));
	VwParagraphBox * pxpgbox = NewObj VwParagraphBox(m_qzvps, kvstNormal);
	OpenFlowObject(pxpgbox);

	CheckHr(AddStringAltMember(kflidCmPicture_Caption, ws, pvwvc));

	CloseFlowObject();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Insert a picture, optionally limiting the height and width. Mark it as if it came from
	the property tag, though we don't actually read data from there (the caller can pass
	any value that might be useful later to identify something about the picture the user
	clicked). If the tag argument is not useful, pass ktagNotAnAttr.
	It is planned that a positive dxmpHeight is a maximum height, negative is an exact
	height, and 0 means don't specify height (use natural height or determine from
	width and aspect ratio). Similarly for width.
	Currently only max height is implemented. Always pass zero for width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddPicture(IPicture * ppict, PropTag tag, int dxmpWidth, int dympHeight)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ppict);

	VwIndepPictureBox * pipbox = NULL;

	pipbox = NewObj VwIndepPictureBox(m_qzvps, ppict, dxmpWidth, dympHeight);
	if (tag != ktagNotAnAttr)
		OpenProp(tag, NULL, 0, kvnpNone);
	AddLeafBox(pipbox);
	if (tag != ktagNotAnAttr)
		CloseProp();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Setting a unicode character that indicates a boundary (e.g. for a paragraph or section).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::SetParagraphMark(VwBoundaryMark boundaryMark)
{
	BEGIN_COM_METHOD;

	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	Assert(pvpbox);

	if (pvpbox)
		pvpbox->SetParagraphMark(boundaryMark);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Convert an SilTime to a TsString. The flags are currently as for the flags argument of
	::GetDateFormat.
----------------------------------------------------------------------------------------------*/
void VwEnv::TimeToTsString(int64 nTime, DWORD flags, ISilDataAccess * psda, ITsString ** pptss)
{
	// Convert the date to a string.
	SilTime tim = nTime;
	StrUni stuBuf;
	if (nTime)
	{
#if WIN32
		SYSTEMTIME stim;
		stim.wYear = (unsigned short) tim.Year();
		stim.wMonth = (unsigned short) tim.Month();
		stim.wDayOfWeek = (unsigned short) tim.WeekDay();
		stim.wDay = (unsigned short) tim.Date();
		stim.wHour = (unsigned short) tim.Hour();
		stim.wMinute = (unsigned short) tim.Minute();
		stim.wSecond = (unsigned short) tim.Second();
		stim.wMilliseconds = (unsigned short)(tim.MilliSecond());

		// Then format it to a time based on the current user locale and supplied flags.
		achar rgchDate[50]; // Tuesday, August 15, 2000		mardi 15 août 2000
		//char rgchTime[50]; // 10:17:09 PM					22:20:08
		::GetDateFormat(LOCALE_USER_DEFAULT, flags, &stim, NULL, rgchDate, 50);
		//::GetTimeFormat(LOCALE_USER_DEFAULT, NULL, &stim, NULL, rgchTime, 50);
#else //WIN32
		char rgchDate[50];
		tm t;
		t.tm_year = tim.Year() - 1900; // tm_year : year since 1900
		t.tm_mon = tim.Month() -1; // tm_mon : month (0 - 11, 0 = January)
		t.tm_wday = tim.WeekDay();
		t.tm_mday = tim.Date();
		t.tm_hour = tim.Hour();
		t.tm_min = tim.Minute();
		t.tm_sec = tim.Second();
		// we throw away milliseconds
		tm *stim = &t;
		if(flags == DATE_LONGDATE)
			strftime(rgchDate, 50, "%a, %b %d %Y", stim);
		else if(flags == DATE_YEARMONTH)
			strftime(rgchDate, 50, "%Y/%B", stim);
		else // default to DATE_SHORTDATE
		{
			char buf[50];
			// previously this code used "%x" then tried to replace the year with %Y on the end
			// in an attempt to do locale dependent formating.
			// however the year could be first for some locales.
			strftime(rgchDate, 50, "%d/%m/%Y", stim);
		}
#endif //WIN32
		stuBuf = rgchDate;
	}
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeStringRgch(stuBuf.Chars(), stuBuf.Length(), GetUserWs(psda), pptss));
}

/*----------------------------------------------------------------------------------------------
	Add an SilTime property, formatted according to the supplied locale.
	The Locale and flags properties are currently interpreted as by
	::GetDateFormat, but we may decide to restrict the full range of these
	options to achieve easier portability. Passing LOCALE_USER_DEFAULT, DATE_SHORTDATE
	is safe.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddTimeProp(int tag, DWORD flags)
{
	BEGIN_COM_METHOD;
	int64 nTime;
	CheckHr(m_qsda->get_TimeProp(m_hvoCurr, tag, (int64 *)(&nTime)));

	ITsStringPtr qtss;
	TimeToTsString(nTime, flags, m_qsda, &qtss);
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	if (!pvpbox)
		OpenParagraph();
	OpenProp(tag, NULL, flags, kvnpTimeProp);
	CheckHr(AddString(qtss));
	CloseProp();
	if (!pvpbox)
		CloseParagraph();
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Add an arbitrary embedded window (which implements the required interface)
	Arguments:
		dysAscent - top of embedded box to baseline for text alignment
		fJustifyRight - if embedded box is last in para
			ENHANCE:: should we generalize this so that other kinds of box can be positioned in
			the same way?
		fAutoShow - if true view will ensure visibility before drawing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddWindow(IVwEmbeddedWindow * pew, int dysAscent, ComBool fJustifyRight,
	ComBool fAutoShow)
{
	BEGIN_COM_METHOD;
	//ChkComArgPtr(pew);
	// ENHANCE: implement when we have created VwWindowBox; enable code at end of para layout.
	Assert(false);
	ThrowInternalError(E_NOTIMPL);
#if 0
	VwWindowBox * pwindbox = NewObj VwWindowBox(m_qzvps, pew, dysAscent,
		fJustifyRight, fAutoShow);

	if (!pwindbox)
		return E_OUTOFMEMORY;

	return AddLeafBox(box);
#endif
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Add a vertical separator bar as used to separate items in Data Entry lists
	REVIEW JohnT(?): should this be generalized somehow? Renamed so it sounds less like an HTML
		horizontal separator?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddSeparatorBar()
{
	BEGIN_COM_METHOD;

	VwSeparatorBox * psepbox = NewObj VwSeparatorBox(m_qzvps);
	AddLeafBox(psepbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Insert a simple rectangular box with the specified color, height, and width.
	@param mpBaselineOffset positive to raise the box; 0 aligns bottom with baseline
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::AddSimpleRect(int rgb, int dmpWidth, int dmpHeight,
	int dmpBaselineOffset)
{
	BEGIN_COM_METHOD;

	VwBarBox * psepbox = NewObj VwBarBox(m_qzvps, rgb, dmpWidth, dmpHeight,
		dmpBaselineOffset);
	AddLeafBox(psepbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


//:>********************************************************************************************
//:>	Getting context info
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Get the current embedding level: the number of layers of object we are displaying. The
	number of outer objects is one less than this.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::get_EmbeddingLevel(int * pchvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pchvo);

	*pchvo = m_cnsi;

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Get info about outer objects, inside whose display the display of the current object is
	embedded. The outermost object is returned at level 0. Whatever property of that object the
	next object is embedded in is returned in *ptag. The index of the next-level object in that
	property (if it is a vector property) is returned in *pihvo; if not a vector property,
	that value is always zero. The Level argument may range from 0 to EmbeddingLevel() - 1.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::GetOuterObject(int ichvoLevel, HVO * phvo, int * ptag,
	int * pihvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);
	ChkComOutPtr(ptag);
	ChkComOutPtr(pihvo);

	if (ichvoLevel > m_cnsi - 1)
		ThrowInternalError(E_INVALIDARG, "Level exceeds current nesting");

	NotifierStackItem * pnsi = m_nsivecStack[ichvoLevel];
	*phvo = pnsi->hvo;

	// The property that the current object is part of is the last one being built at the
	// specified level.
	int cprop = pnsi->noterecvec.Size();

	NotifierRec * pnoterec = pnsi->noterecvec[cprop - 1];
	*ptag = pnoterec->tag;
	*pihvo = pnsi->chvoProp - 1;
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Get the data access object in use. This allows the view constructor to get at
	other properties of the object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::get_DataAccess(ISilDataAccess ** ppsda)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsda);

	*ppsda = m_qsda;
	AddRefObj(*ppsda);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	The current object, whether or not one of its properties is open
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CurrentObject(HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);

	*phvo = m_hvoCurr;

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Current open object, null if prop open
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::get_OpenObject(HVO * phvoRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvoRet);

	*phvoRet = (m_fObjectOpen ? m_hvoCurr : 0);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


//:>********************************************************************************************
//:>	Delimit layout flow objects
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Make a div box (overridden in VwInvertedEnv to make an inverted one)
----------------------------------------------------------------------------------------------*/
VwDivBox * VwEnv::MakeDivBox()
{
	return NewObj VwDivBox(m_qzvps);
}
/*----------------------------------------------------------------------------------------------
	Delimit a Division (group of related paragraphs)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenDiv()
{
	BEGIN_COM_METHOD;

	VwGroupBox * pgbox = NULL;

	// Divisions must not go inside paragraphs...use InnerPile
	Assert(!dynamic_cast<VwParagraphBox *>(m_pgboxCurr));
	pgbox = MakeDivBox();
	OpenFlowObject(pgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseDiv()
{
	BEGIN_COM_METHOD;
	Assert(!m_pgboxCurr || dynamic_cast<VwDivBox *>(m_pgboxCurr));

	CloseFlowObject();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Make a paragraph box (overridden in VwInvertedEnv to make an inverted paragraph
----------------------------------------------------------------------------------------------*/
VwParagraphBox * VwEnv::MakeParagraphBox(VwSourceType vst)
{
	return NewObj VwParagraphBox(m_qzvps, vst);
}

/*----------------------------------------------------------------------------------------------
	Delimit a paragraph
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenParagraph()
{
	BEGIN_COM_METHOD;

	VwParagraphBox * pxpgbox = NULL;

	// Paragraphs can only go inside some kind of pile
	Assert(dynamic_cast<VwPileBox *>(m_pgboxCurr));
	pxpgbox = MakeParagraphBox();
	OpenFlowObject(pxpgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	A paragraph that supports display of tagging, if an overlay is installed in the
	root box.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenTaggedPara()
{
	BEGIN_COM_METHOD;

	VwParagraphBox * pxpgbox = NULL;

	// Paragraphs can only go inside some kind of pile
	Assert(dynamic_cast<VwPileBox *>(m_pgboxCurr));
	pxpgbox = MakeParagraphBox(kvstTagged);
	dynamic_cast<VwOverlayTxtSrc *>(pxpgbox->Source())->SetRoot(m_qrootbox);
	OpenFlowObject(pxpgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	A paragraph that also supports embedded objects whose names are obtained by
	calling the view constructor.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenMappedPara()
{
	BEGIN_COM_METHOD;

	VwParagraphBox * pxpgbox = NULL;

	// Paragraphs can only go inside some kind of pile
	Assert(dynamic_cast<VwPileBox *>(m_pgboxCurr));
	pxpgbox = NewObj VwParagraphBox(m_qzvps, kvstMapped);
	dynamic_cast<VwOverlayTxtSrc *>(pxpgbox->Source())->SetRoot(m_qrootbox);
	OpenFlowObject(pxpgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	A paragraph that also supports embedded objects whose names are obtained by
	calling the view constructor, and also tagging.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenMappedTaggedPara()
{
	BEGIN_COM_METHOD;

	VwParagraphBox * pxpgbox = NULL;

	// Paragraphs can only go inside some kind of pile
	Assert(dynamic_cast<VwPileBox *>(m_pgboxCurr));
	pxpgbox = MakeParagraphBox(kvstMappedTagged);
	dynamic_cast<VwOverlayTxtSrc *>(pxpgbox->Source())->SetRoot(m_qrootbox);
	OpenFlowObject(pxpgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	A paragraph that is intended to be a line in a concordance display.
	A key word is identified, typically the item being concorded on, and its position is
	indicated. Typically, the keyword is bold, and gets aligned with a specified position,
	dmpAlign. Alignment will be left, center, or right, according to the alignment of the
	paragraph as a whole.
	(Non-left alignment is not yet implemented.)
	@param ichMin/LimItem Indicate the position of the item being concorded. Depending
	on the flags, this item is typically made bold and aligned with dmpAlign
	@param cpoFlags indicates whether to bold the key word, and whether to align it.
	@param dmpAlign distance from left of paragraph to align keywords.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags,
								int dmpAlign)
{
	BEGIN_COM_METHOD;

	VwConcParaBox * pcpb = NULL;

	// Paragraphs can only go inside some kind of pile
	Assert(dynamic_cast<VwPileBox *>(m_pgboxCurr));
	pcpb = NewObj VwConcParaBox(m_qzvps, kvstConc);
	dynamic_cast<VwOverlayTxtSrc *>(pcpb->Source())->SetRoot(m_qrootbox);
	pcpb->Init(ichMinItem, ichLimItem, dmpAlign, cpoFlags);
	OpenFlowObject(pcpb);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	A paragraph that supports override text display.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenOverridePara(int cOverrideProperties,
									 DispPropOverride *prgOverrideProperties)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgOverrideProperties, cOverrideProperties);

	VwParagraphBox * pxpgbox = NULL;

	// Paragraphs can only go inside some kind of pile
	Assert(dynamic_cast<VwPileBox *>(m_pgboxCurr));
	pxpgbox = NewObj VwParagraphBox(m_qzvps, kvstOverride);
	PropOverrideVec pov;
	pov.InsertMulti(0, cOverrideProperties, prgOverrideProperties);
	VwOverrideTxtSrc * pcts = dynamic_cast<VwOverrideTxtSrc *>(pxpgbox->Source());
	pcts->SetOverrides(pov);
	dynamic_cast<VwOverlayTxtSrc *>(pcts->EmbeddedSrc())->SetRoot(m_qrootbox);
	OpenFlowObject(pxpgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseParagraph()
{
	BEGIN_COM_METHOD;

	VwParagraphBox * pxpgbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	Assert(!m_pgboxCurr || pxpgbox);
	if (m_pgboxCurr  && pxpgbox->Source()->CStrings() == 0)
	{
		if (m_emptyParagraphBehavior == 1)
		{
			// We want to be as invisible as we can manage.
			// We can't guarantee that we won't affect the overlap of the preceding paragraph's
			// bottom margin with the following one's top margin, but we can be zero height ourself.
			// We do want some content since various routines assume there is at least one thing.
			// A transparent block of zero size is about as inconspicuous as it gets.
			this->AddSimpleRect(kclrTransparent, 0, 0, 0);
			// The default styles we apply to the root should not set anything visible.
			pxpgbox->_SetPropStore(m_qrootbox->Style());
		}
		else
		{
			// Paragraph layout requires at least one string, so make a dummy literal.
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtss;
			CheckHr(qtsf->MakeStringRgch(NULL, 0, GetUserWs(m_qsda), &qtss));
			AddString(qtss);
		}
	}
	pxpgbox->Source()->AdjustOverrideOffsets(); // update the offsets

	CloseFlowObject();
	m_emptyParagraphBehavior = 0;

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Delimit pile of paras embedded in another para, for interlinear
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenInnerPile()
{
	BEGIN_COM_METHOD;

	VwGroupBox * pgbox = NULL;

	// Inner piles can only go inside paragraphs
	Assert(dynamic_cast<VwParagraphBox *>(m_pgboxCurr));
	pgbox = NewObj VwInnerPileBox(m_qzvps);
	OpenFlowObject(pgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseInnerPile()
{
	BEGIN_COM_METHOD;

	Assert(!m_pgboxCurr || dynamic_cast<VwInnerPileBox *>(m_pgboxCurr));
	CloseFlowObject();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Delimit span (group of sub-para objects sharing properties)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenSpan()
{
	BEGIN_COM_METHOD;

	// Spans only belong inside paragraphs.
	Assert(dynamic_cast<VwParagraphBox *>(m_pgboxCurr));

	// Currently we don't need any explicit object to represent a span
	OpenFlowObject(NULL);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseSpan()
{
	BEGIN_COM_METHOD;

	Assert(!m_pgboxCurr || dynamic_cast<VwParagraphBox *>(m_pgboxCurr));
	CloseFlowObject();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Delimit a table
	Arguments:
		vlWidth - of whole table, percent of available
		vlBorder - thickness, percent of ?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenTable(int ccolm, VwLength vlenWidth, int mpBorder,
	VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule,
	int mpSpacing, int mpPadding, ComBool fSelectOneCol)
{
	BEGIN_COM_METHOD;

	VwGroupBox * pgbox = NULL;

	// Tables can go pretty much anywhere, so no assert
	pgbox = NewObj VwTableBox(m_qzvps, ccolm, vlenWidth, mpBorder, vwalign, frmpos, vwrule,
		mpSpacing, mpPadding, fSelectOneCol);
	OpenFlowObject(pgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseTable()
{
	BEGIN_COM_METHOD;

	VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr);
	Assert(ptable);
	ptable->ConstructionStage(kcsDone);
	CloseFlowObject();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Delimit a row of a table.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenTableRow()
{
	BEGIN_COM_METHOD;

	VwGroupBox * pgbox = NULL;

	m_fTableRow = true;

	// Table rows can only go inside tables
	Assert(dynamic_cast<VwTableBox *>(m_pgboxCurr));
	pgbox = NewObj VwTableRowBox(m_qzvps);
	OpenFlowObject(pgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseTableRow()
{
	BEGIN_COM_METHOD;

	m_fTableRow = false;
	Assert(dynamic_cast<VwTableRowBox *>(m_pgboxCurr));
	CloseFlowObject();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Delimit cell of table.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenTableCell(int crowSpan, int ccolmSpan)
{
	BEGIN_COM_METHOD;

	VwGroupBox * pgbox = NULL;

	m_fTableRow = false; // only needed here and in constructor, since only cells go in rows

	// Table cells can only go inside table rows
	Assert(dynamic_cast<VwTableRowBox *>(m_pgboxCurr));
	pgbox = NewObj VwTableCellBox(m_qzvps, false, crowSpan, ccolmSpan);
	OpenFlowObject(pgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseTableCell()
{
	BEGIN_COM_METHOD;

	m_fTableRow = true;
	Assert(dynamic_cast<VwTableCellBox *>(m_pgboxCurr));
	CloseFlowObject();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Delimit cell to be shown as header (for row or column)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenTableHeaderCell(int crowSpan, int ccolmSpan)
{
	BEGIN_COM_METHOD;

	VwGroupBox * pgbox = NULL;

	m_fTableRow = false; // only needed here and in constructor, since only cells go in rows

	// Table cells can only go inside table rows
	Assert(dynamic_cast<VwTableRowBox *>(m_pgboxCurr));

	// the true makes it a header cell
	pgbox = NewObj VwTableCellBox(m_qzvps, true, crowSpan, ccolmSpan);
	OpenFlowObject(pgbox);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseTableHeaderCell()
{
	BEGIN_COM_METHOD;

	m_fTableRow = true;
	Assert(dynamic_cast<VwTableCellBox *>(m_pgboxCurr));
	CloseFlowObject();

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Specify the width assigned to each of a specified number of columns
	Arguments:
		vlWidth - percent of space between margins
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::MakeColumns(int ccolmSpan, VwLength vlenWidth)
{
	BEGIN_COM_METHOD;

	VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr);

	Assert(ptable);
	Assert(ptable->ConstructionStage() == kcsInit);
	int ccolmConstruct = ptable->ConstructCol();

	if (ccolmConstruct + ccolmSpan > ptable->Columns())
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	int icolmLim = ccolmConstruct + ccolmSpan;
	for (; ccolmConstruct < icolmLim; ccolmConstruct++)
	{
		ptable->ColumnSpec(ccolmConstruct)->SetWidthVLen(vlenWidth);
	}

	ptable->SetConstructCol(ccolmConstruct);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Specify the width assigned to each of a specified number of columns, and indicate that
	after the last of them is a column group boundary. (With certain styles, this will cause
	a rule to be drawn there.)
	Arguments:
		vlWidth - percent of space between margins
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::MakeColumnGroup(int ccolmSpan, VwLength vlenWidth)
{
	BEGIN_COM_METHOD;

	VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr);
	MakeColumns(ccolmSpan, vlenWidth);
	int icolmLim = ptable->ConstructCol();
	if (icolmLim > 1)
		ptable->ColumnSpec(icolmLim - 1)->SetGroupRight(true);
	if (icolmLim < ptable->Columns())
		ptable->ColumnSpec(icolmLim)->SetGroupLeft(true);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Delimit the main sections of the table. They should be added in the order header, footer,
	body.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenTableHeader()
{
	BEGIN_COM_METHOD;

	VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr);

	Assert(ptable);
	ptable->ConstructionStage(kcsHeader);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseTableHeader()
{
	BEGIN_COM_METHOD;

	Debug(VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr));
	AssertPtr(ptable);
	Assert(ptable->ConstructionStage() == kcsHeader);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenTableFooter()
{
	BEGIN_COM_METHOD;

	VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr);

	Assert(ptable);
	ptable->ConstructionStage(kcsFooter);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseTableFooter()
{
	BEGIN_COM_METHOD;

	Debug(VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr));

	AssertPtr(ptable);
	Assert(ptable->ConstructionStage() == kcsFooter);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Delimit the body of the table. A table may have several bodies, each forming a row group.
	With certain styles, groups produce a horizontal rule.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::OpenTableBody()
{
	BEGIN_COM_METHOD;

	VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr);

	Assert(ptable);
	ptable->ConstructionStage(kcsBody);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::CloseTableBody()
{
	BEGIN_COM_METHOD;

	Debug(VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_pgboxCurr));
	AssertPtr(ptable);
	Assert(ptable->ConstructionStage() == kcsBody);

	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Gives the height and width required to lay out the given string,
	using current display properties plus those produced by pttp (which
	may be null). In other words, this is the amount of space that would
	be occupied if one currently called Props(pttp); AddString(ptss);
	(assuming infinite available width).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::get_StringWidth(ITsString * ptss, ITsTextProps * pttp, int * pdxs,
	int * pdys)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);
	ChkComArgPtrN(pttp);
	ChkComOutPtr(pdxs);
	ChkComOutPtr(pdys);

	VwParagraphBox * pvpbox = NULL;
	VwParagraphBox * pvpboxCont = NULL;
	VwRootBoxPtr qrootb;
	try
	{
		VwPropertyStorePtr qzvps;
		VwPropertyStorePtr qzvps2;
		CheckHr(m_qzvps->ComputedPropertiesForTtp(pttp, &qzvps2));
		// Force it to align left; otherwise, it takes on the width of the available
		// space, and then the MulDiv may fail, besides which the answer would mean nothing.
		CheckHr(qzvps2->ComputedPropertiesForInt(ktptAlign, ktpvEnum, ktalLeft, &qzvps));
		pvpbox = NewObj VwParagraphBox(qzvps);
		// Make its container be a paragraph. Otherwise, it occupies all available width.
		pvpboxCont = NewObj VwParagraphBox(qzvps);
		pvpbox->Container(pvpboxCont);

		qrootb.Attach(NewObj VwRootBox(m_qzvps));
		qrootb->putref_DataAccess(m_qsda);
		pvpboxCont->Container(qrootb);

		pvpbox->Source()->Vpst().Push(VpsTssRec(qzvps, ptss));
		pvpbox->DoLayout(m_qvg, INT_MAX);
		int dxInch, dyInch;
		CheckHr(m_qvg->get_XUnitsPerInch(&dxInch));
		CheckHr(m_qvg->get_YUnitsPerInch(&dyInch));
		*pdxs = MulDiv(pvpbox->Width(), kdzmpInch, dxInch);
		*pdys = MulDiv(pvpbox->Height(), kdzmpInch, dyInch);
		delete pvpbox;
		delete pvpboxCont;
		qrootb->Close();
	}
	catch (...)
	{
		if (pvpbox)
			delete pvpbox;
		if (pvpboxCont)
			delete pvpboxCont;
		if (qrootb)
			qrootb->Close();
		throw;
	}
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

//:>********************************************************************************************
//:>	Begin and end of underlying objects and properties.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Handle starting the display of an object.
----------------------------------------------------------------------------------------------*/
void VwEnv::OpenObject(HVO hvo)
{
	if (m_fObjectOpen)
	{
		// If this assert fires, it is probably because you are not alternating between calls to
		// openObject and openAttribute. If Object A has an attribute Attr that owns Object B,
		// then A should have an openObject, inside of which there is an openAttribute inside of
		// which is the code to display B which does openObject again. If you want to display an
		// object or string that is not part of any attr of the current object, call
		// OpenAttr(ktagNotAnAttr)
		Assert(false);
		ThrowHr(WarnHr(E_UNEXPECTED));
	}

	m_fObjectOpen = true;
	m_chvoProp++; // Note one more object in current property

	// PRELIMINARY COMMENT: JeffG Save off the current object infomation
	NotifierStackItem * pnsi = NewObj NotifierStackItem();
	pnsi->hvo = m_hvoCurr;
	pnsi->chvoProp = m_chvoProp;
	pnsi->inoteIncomplete = m_vpanoteIncomplete.Size();
	// Save the information we are accumulating about the current object in the stack,
	// and start accumulating information about the new object we are opening.
	m_noterecvecCurr.CopyTo(pnsi->noterecvec);
	m_nsivecStack.Push(pnsi);
	m_noterecvecCurr.Clear(); //reuse, we copied it
	m_hvoCurr = hvo;
	++m_cnsi;

	// This waits for the first box of the object.
	NotifierRec * pnoterec = GetNotifierRec();
	m_noterecvecCurr.Push(pnoterec);
	m_noterecvecOpenProps.Push(pnoterec);
}


/*----------------------------------------------------------------------------------------------
	Notify that we are ending the current object.
----------------------------------------------------------------------------------------------*/
void VwEnv::CloseObject()
{
	if (!m_fObjectOpen)
	{
		Assert(false);
		ThrowHr(WarnHr(E_UNEXPECTED));
	}

	m_fObjectOpen = false;

	// Make a notifier for the object we have just finished building
	int cprop = m_noterecvecCurr.Size();

	//	If we are waiting for a box to start a non-prop range and didn't get it, forget it.
	if (cprop > 0 && m_noterecvecCurr[cprop - 1]->tag == ktagGapInAttrs &&
			m_noterecvecCurr[cprop - 1]->pbox == NULL)
	{
		FreeNotifierRec(m_noterecvecCurr[cprop - 1]);
		m_noterecvecCurr.Pop();
		Assert(m_noterecvecOpenProps.Size() >= 1);
		m_noterecvecOpenProps.Pop();
		cprop--;
	}

	VwAbstractNotifierPtr qanote;

	// This is part of popping the stack (below), but we need to fix this value
	// before we create the notifier, because its object index is the index of its
	// object within some outer property.
	m_chvoProp = m_nsivecStack[m_nsivecStack.Size() - 1]->chvoProp;

	if (cprop && m_hvoCurr)
	{
		// There were some properties, so we will need to do automatic update if one
		// changes. So we want a notifier. First, check whether any of the properties
		// actually produced some data; if not, we will use a special notifier.
		bool fFoundOneBox = false;
		int i;
		for (i = 0; i < cprop; i++)
		{
			if (m_noterecvecCurr[i]->pbox)
				fFoundOneBox = true;
		}
		if (fFoundOneBox)
		{
			VwNotifierPtr qnote;
			//	Make a VwNotifier out of the data we've collected.
			// m_chvoProp is a count of objects created so far, so the index
			// of the last one is one less.
			qnote.Attach(VwNotifier::Create(m_chvoProp - 1, cprop));

			// Assign it as the parent of an appropriate list of notifiers
			int i;

			// Making a real notifier, any embedded ones we have made now get this as
			// their parent.
			int inoteFirstIncompleteThis = m_nsivecStack[m_nsivecStack.Size() - 1]->inoteIncomplete;
			while (m_vpanoteIncomplete.Size() > inoteFirstIncompleteThis)
			{
				VwAbstractNotifierPtr qanote;
				m_vpanoteIncomplete.Pop(&qanote);
				qanote->SetParent(qnote);
			}

			VwBox ** rgpbox = qnote->Boxes();
			PropTag * rgtag = qnote->Tags();
			int * rgitss = qnote->StringIndexes();
			int * rgfrag = qnote->Fragments();
			IVwViewConstructor ** prgpvvc = qnote->Constructors();
			VwPropertyStore ** prgpzvps = qnote->Styles();
			VwNoteProps * rgvnp = qnote->Flags();

			for (i = 0; i < cprop; i++)
			{
				rgpbox[i] = m_noterecvecCurr[i]->pbox;
				rgtag[i] = m_noterecvecCurr[i]->tag;
				rgitss[i] = m_noterecvecCurr[i]->itssStart;
				rgfrag[i] = m_noterecvecCurr[i]->frag;
				rgvnp[i] = m_noterecvecCurr[i]->vnp;
				VwPropertyStore * pzvps = m_noterecvecCurr[i]->pzvps;
				prgpzvps[i] = pzvps;
				AddRefObj (pzvps);

				IVwViewConstructor * pvvc = m_noterecvecCurr[i]->qvvc;
				prgpvvc[i] = pvvc;
				AddRefObj (pvvc);
			}
			qnote->_SetLastBox();
			qanote.Attach(qnote.Detach());
		}
		else
		{
			// No boxes (every property of this object is empty): make a VwMissingNotifier.
			// Since it can't regenerate on its own, all it records is the list of
			// interesting tags. (If any other property changes, the display does not care.)
			VwMissingNotifierPtr qmnote;
			qmnote.Attach(VwMissingNotifier::Create(m_chvoProp - 1, cprop));
			PropTag * rgtag = qmnote->Tags();
			for (int i = 0; i < cprop; i++)
			{
				rgtag[i] = m_noterecvecCurr[i]->tag;
			}
			qanote.Attach(qmnote.Detach());
			qanote->_KeyBox(m_pgboxCurr);
		}

		qanote->SetObject(m_hvoCurr);

		// Have the notifier remember the very last box and string index for this object.
		m_qrootbox->AddNotifier(qanote);
	}

	// Pop the stack, disposing of notifier items we are done with (we have already closed any
	// items in openAttributes pointing	at these records).
	m_hvoCurr = m_nsivecStack[m_nsivecStack.Size() - 1]->hvo;
	for (int i = 0; i < m_noterecvecCurr.Size(); i++)
	{
		FreeNotifierRec(m_noterecvecCurr[i]);
	}

	m_nsivecStack[m_nsivecStack.Size() - 1]->noterecvec.CopyTo(m_noterecvecCurr);

	delete m_nsivecStack[m_nsivecStack.Size() - 1];
	m_nsivecStack.Pop();
	m_cnsi--;

	if (qanote)
	{
		// The notifier we just made is one that has to b a child of the next level up when it
		// is made.
		m_vpanoteIncomplete.Push(qanote);

		// And its property index is the number of currently open properties: it is part of the
		// last one. An adjustment is needed for the top-level objects in a regenerate; see
		// the comments above the declaration of m_ipropAdjust for details.
		qanote->SetPropIndex(m_noterecvecCurr.Size() - 1 +
			(m_insiAdjustLev == m_cnsi ? m_ipropAdjust : 0));
	}
}


/*----------------------------------------------------------------------------------------------
	Handle starting the display of a property.
	Arguments:
		tag - of the property we are displaying
		pvvc - view constructor to use to rebuild it (possibly with low bit set)
		frag - argument to pass to pvvc, or other info if pvvc is null.
		fVariant - true if prop displayed using DisplayVariant
		chvoPrev - number of objects to pretend we have already displayed in this property.
			This is used when expanding part of a property in a lazy box, or when regenerating
			just some objects.
----------------------------------------------------------------------------------------------*/
void VwEnv::OpenProp(int tag, IVwViewConstructor * pvvc, int frag, VwNoteProps vnp,
	int chvoPrev)
{
	if (!m_fObjectOpen)
	{
		Assert(false);
		ThrowHr(WarnHr(E_UNEXPECTED));
	}

	m_fObjectOpen = false;

	// Record view controller's idea of editability. Note that the nature of the property
	// may override this. Time type is as yet never editable. Adding an appropriate
	// case to VwNotifier::DoUpdateProp would fix this.
	if (m_qzvps->Editable() && vnp != kvnpTimeProp)
		vnp  = (VwNoteProps) (vnp | kvnpEditable);

	// Keep track of the object index within the prop.
	m_chvoProp = chvoPrev; // Usually have not opened any yet, but we may pretend we have.

	NotifierRec * pnoterec = NULL;
	if (m_noterecvecCurr.Size() > 0)
	{
		NotifierRec * pnoterecLast = *(m_noterecvecCurr.Top());

		if (pnoterecLast->tag == ktagGapInAttrs && pnoterecLast->pbox == NULL)
		{
			// We were waiting for a non-property box, but	didn't find one; there is nothing
			// between one prop and the next. So, we don't need a notifier entry between the two
			// prop ones. Instead of freeing a record and making a new one,	just use the pending
			// one for the current prop.
			pnoterec = pnoterecLast;

			// Since we don't have a box in it, we ought to be looking to put one there.
			Assert(*(m_noterecvecOpenProps.Top()) == pnoterecLast);
		}
	}

	if (!pnoterec)
	{
		pnoterec = GetNotifierRec();
		//	Add it to the list of box/tag pairs being built for the current object
		// ENHANCE JohnT: there should be some sort of smart pointer applicable to this...
		try
		{
			m_noterecvecCurr.Push(pnoterec);
		}
		catch (...)
		{
			FreeNotifierRec(pnoterec);
			throw;
		}

		// Put a pointer to the record in the list of things that need to be set if we get a
		// box.
		m_noterecvecOpenProps.Push(pnoterec);
	}

	pnoterec->tag = tag;
	pnoterec->frag = frag;
	pnoterec->qvvc = pvvc;
	pnoterec->vnp = vnp;
	pnoterec->pzvps = m_qzvps;
	// If we add a string box next, we will change this to the appropriate index. If we add
	// no box, or a non-string box, it remains -1.
	pnoterec->itssStart = -1;
}


/*----------------------------------------------------------------------------------------------
	Handle ending the current property.
----------------------------------------------------------------------------------------------*/
void VwEnv::CloseProp()
{
	if (m_fObjectOpen)
	{
		Assert(false);
		ThrowHr(WarnHr(E_UNEXPECTED));
	}

	m_fObjectOpen = true;

	// If there was any kind of box added while we were processing this property, then the
	// NotifierRec we added to m_noterecvecOpenProps to 'catch' that box will already have been
	// removed, along with all others for props above this level. Anything for a lower level
	// prop should have been removed before or when it was closed. Therefore, if there is still
	// anything in m_noterecvecOpenProps, the last thing there must be our own pending notifier.
	// This means there was nothing generated for this property, and it is now closed, so there
	// never can be. We don't want a later box (from some higher level prop) to get associated
	// with this one, so remove the notifier from m_noterecvecOpenProps. Note that it is still
	// present in m_noterecvecCurr and will form a record (with box NULL) in the notifier,
	// unless possibly its tag is ktagGapInAttrs.
	if (m_noterecvecOpenProps.Size() > 0)
		m_noterecvecOpenProps.Pop();

	// If we add another box before opening another property, we want to make an entry in the
	// notifier to indicate it does not belong to any property. This record gets discarded (see
	// OpenProp) if we open another prop before we make a new box.
	NotifierRec *pnoterec = GetNotifierRec();

	try
	{
		m_noterecvecCurr.Push(pnoterec);
	}
	catch (...)
	{
		FreeNotifierRec(pnoterec);
		throw;
	}

	// Now if this fails, pnoterec gets freed as part of cleaning up m_noterecvecCurr
	m_noterecvecOpenProps.Push(pnoterec);
}


//:>********************************************************************************************
//:>	Miscellaneous public methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Make the default display of an integer as a TsString. May be given a string factory; if not,
	creates one and returns a pointer as a side effect.
----------------------------------------------------------------------------------------------*/
void VwEnv::IntToTsString(int nVal, ITsStrFactory ** pptsf, ISilDataAccess * psda, ITsString ** pptss)
{
	OLECHAR buf[20];

	// the last value of itow_s is base not string length.
	_itow_s(nVal, buf, 20, 10);		// ENHANCE: encoding-dependent conversion

	GetStringFactory(pptsf);
	CheckHr((*pptsf)->MakeStringRgch(buf, u_strlen(buf), GetUserWs(psda), pptss));
}


/*----------------------------------------------------------------------------------------------
// Make sure *pptsf is set to a valid string factory. If not, create one.
// Enhance JohnT: Most callers effectively pass &m_qtsf, which makes a new one each time...
// Maybe that doesn't matter much as it is a singleton.
----------------------------------------------------------------------------------------------*/
void VwEnv::GetStringFactory(ITsStrFactory ** pptsf)
{
	if (!*pptsf)
		CheckHr(CoCreateInstance(CLSID_TsStrFactory, NULL, CLSCTX_ALL,
			IID_ITsStrFactory, (void **) pptsf));
}

/*----------------------------------------------------------------------------------------------
	At end of making an embedded display, set the parent of the top-level notifiers.
	Also sets their ObjectIndex to -1, indicating they don't represent an object within
	an object property.
----------------------------------------------------------------------------------------------*/
void VwEnv::SetParentOfTopNotifiers(VwNotifier * pnote)
{
	for (int i = 0; i < m_vpanoteIncomplete.Size(); i++)
	{
		m_vpanoteIncomplete[i]->SetParent(pnote);
		m_vpanoteIncomplete[i]->SetObjectIndex(-1);
	}
}


/*----------------------------------------------------------------------------------------------
	At end of regenerate extract the boxes that have been built and the top-level notifiers
	that need parents and do any cleanup that is needed.
----------------------------------------------------------------------------------------------*/
void VwEnv::GetRegenerateInfo(VwBox ** ppbox, NotifierVec & vpanoteIncomplete,
	bool fForErrorRecovery)
{
	if (!fForErrorRecovery)
	{
		Assert(m_vpgboxOpen.Size()> 0); // parent box should be in it still.
		// We should have popped any groups we started from the stack.
		Assert(*(m_vpgboxOpen.Top()) == m_pboxSaveContainer);
	}
	// Don't do this! Sometime we push the parent of the starting box as well.
	//Assert(m_vpgboxOpen.Size() == 1); // Should have closed all boxes we opened.
	*ppbox = m_pboxSaveContainer->RemoveAllBoxes();
	vpanoteIncomplete.Clear();
	int i;
	for (i = 0; i < m_vpanoteIncomplete.Size(); i++)
		vpanoteIncomplete.Push(m_vpanoteIncomplete[i]);
	m_pboxSaveContainer->RestoreBoxes(m_pboxSaveFirst, m_pboxSaveLast);
	// Clean up the fake nsi stack we made in InitRegen
	while (m_nsivecStack.Size())
	{
		NotifierStackItem * pnsi = *(m_nsivecStack.Top());
		m_nsivecStack.Pop();
		NotifierRecVec & noterecvec = pnsi->noterecvec;
		for (i = 0; i < noterecvec.Size(); i++)
			FreeNotifierRec(noterecvec[i]);
		delete pnsi;
	}
	for (i = 0; i < m_noterecvecCurr.Size(); i++)
		FreeNotifierRec(m_noterecvecCurr[i]);
}


/*----------------------------------------------------------------------------------------------
	At end of (re)building the entire display, clean up anything that needs it.
----------------------------------------------------------------------------------------------*/
void VwEnv::Cleanup()
{
	// At present there is nothing to do...
}


/***********************************************************************************************
	Style variations, to be applied to the next flow object opened.
	Ignored if a close operation precedes the next open.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Set an integer property value. The IDL file defines the possible varition and property
	combinations.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::put_IntProperty(int sp, int pv, int nValue)
{
	BEGIN_COM_METHOD;
	// DO NOT simplify this to
	//		return m_qzvps->ComputedPropertiesForInt(sp, pv, nValue, &m_qzvps)
	// Getting the reference to m_qzvps zeros out the pointer, and possibly deletes the object,
	// and then we can't use it to call the method.
	VwPropertyStorePtr qzvps = m_qzvps;

	return qzvps->ComputedPropertiesForInt(sp, pv, nValue, &m_qzvps);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}


/*----------------------------------------------------------------------------------------------
	Similar method for string-valued properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::put_StringProperty(int sp, BSTR bstrValue)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrValue);
	VwPropertyStorePtr qzvps = m_qzvps;
	return qzvps->ComputedPropertiesForString(sp, bstrValue, &m_qzvps);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	Similar method for to set a whole group of properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::put_Props(ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pttp);
	VwPropertyStorePtr qzvps = m_qzvps;
	return qzvps->ComputedPropertiesForTtp(pttp, &m_qzvps);
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

/*----------------------------------------------------------------------------------------------
	 Called while a paragraph is open, this controls how the view will behave if no
	 content is added to the paragraph before it is closed.
	 Currently the argument must be 1; the only reason to have the argument at
	 all is in the interests of forward compatibility if we think of more behaviors.
	 The default behavior is that the paragraph behaves as if it contained a read-only
	 empty string.
	 The behavior when this method is called (with argument 1) is to make the
	 empty paragraph as nearly as possible invisible.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwEnv::EmptyParagraphBehavior(int behavior)
{
	BEGIN_COM_METHOD;
	Assert(behavior == 1);
	m_emptyParagraphBehavior = behavior;
	END_COM_METHOD(dfactEnv, IID_IVwEnv);
}

//:>********************************************************************************************
//:>	General flow object handling
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Open a flow object.
	Arguments:
		pgbox - the box representing the flow object. May be null if the current flow object is
			a span
----------------------------------------------------------------------------------------------*/
void VwEnv::OpenFlowObject(VwGroupBox * pgbox)
{
	// Note: arguably we should we try to delete pgbox if we can't complete this work
	// successfully.
	// But even partial success at AddBox may leave something pointing at it. Seems safer to
	// risk a small memory leak in the very unlikely event of failure during this method.
	if (pgbox)
	{
		AddBox(pgbox);
		m_pgboxCurr = pgbox;
	}
	m_vpgboxOpen.Push(m_pgboxCurr);
	ResetStyles();
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void VwEnv::CloseFlowObject()
{
	Assert(m_vpgboxOpen.Size()>1);
	m_vpgboxOpen.Pop();  // remove the one currently being built

	// Get the one that we will now resume building
	VwGroupBox * pgboxTop = m_vpgboxOpen[m_vpgboxOpen.Size() - 1];
	if (pgboxTop != m_pgboxCurr)
	{
		// It could be the same: opening a span does not start a new actual box. But if it is
		// different, we are done with the old current group box
		m_pgboxCurr = pgboxTop;
	}

	m_vpzvpsInitialStyles.Pop();

	// Having removed the initial style for things inside the flow object we are closing, we now
	// want to reset current styles to the initial style for the containing flow object, which
	// (after the pop) is now on the top of the stack.
	m_qzvps = *(m_vpzvpsInitialStyles.Top());

	// Any open properties when we opened the flow object would have been satisfied and
	// cleared. So, any still opened have been started within this flow object, but not closed.
	// This is an error, except that if we opened a prop, made something, and closed it, then we
	// are waiting for anything to be added that is not in a prop. In that case just discard it.
	if (m_noterecvecOpenProps.Size() > 0)
	{
		Assert(m_noterecvecOpenProps.Size() == 1);
		Assert(m_noterecvecOpenProps[0]->tag == ktagGapInAttrs);
		// Note that we do NOT delete the noterec, nor remove it from open props; we may yet
		// make more boxes that are part of this object but not part of a property.
	}
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void VwEnv::ResetStyles()
{
	if (m_fTableRow)
	{
		// On top of the stack is the row, one down is the table.
		Assert(m_vpgboxOpen.Size() >= 2);
		VwTableBox * ptable = dynamic_cast<VwTableBox *>(m_vpgboxOpen[m_vpgboxOpen.Size() - 2]);
		Assert(ptable);
		m_qzvps = ptable->RowPropertyStore();
	}
	else
	{
		// get a new property store by invoking standard reset of uninherited properties on the
		// current one
		VwPropertyStorePtr qzvps = m_qzvps;
		CheckHr(qzvps->ComputedPropertiesForEmbedding(&m_qzvps));
	}

	// push on stack, so we can come back to this style at the end of any embedded box
	m_vpzvpsInitialStyles.Push(m_qzvps);
}


//:>********************************************************************************************
//:>	Adding boxes and managing strings
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Add a (non-string) box to the currently open box, and if there are current notifiers
	which don't yet have a starting box, fix them too.
----------------------------------------------------------------------------------------------*/
void VwEnv::AddBox(VwBox * pbox)
{
	// For most kinds of group box we make the notifier point at the sub-box and give a
	// string index of -1.
	VwBox * pboxNote = pbox;
	int itssNote = -1;
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(m_pgboxCurr);
	if (pvpbox)
	{
		// We make the notifier point at the paragraph box and the string index be
		// the number of strings (or boxes) already added.
		// Be sure to calculate this before adding the box to the paragraph.
		pboxNote = pvpbox;
		itssNote = pvpbox->Source()->Vpst().Size();
	}
	for (int i = 0; i < m_noterecvecOpenProps.Size(); ++i)
	{
		(m_noterecvecOpenProps[i])->pbox = pboxNote;
		(m_noterecvecOpenProps[i])->itssStart = itssNote;
	}
	m_noterecvecOpenProps.Clear();
	// Put the new box in the containing group box. Do this after getting itssNote.
	m_pgboxCurr->Add(pbox);
}

/*----------------------------------------------------------------------------------------------
	Add a leaf box to the current flow object. Since we will not be opening it as a flow
	object, we need to reset the styles, so that styles that were set for this one box
	will not accidentally apply to the following box.
----------------------------------------------------------------------------------------------*/
void VwEnv::AddLeafBox(VwBox * pbox)
{
	AddBox(pbox);
	m_qzvps = *(m_vpzvpsInitialStyles.Top());
}

/*----------------------------------------------------------------------------------------------
	Get the default user interface writing system integer code.
----------------------------------------------------------------------------------------------*/
int VwEnv::GetUserWs(ISilDataAccess * psda)
{
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(psda->get_WritingSystemFactory(&qwsf));
	AssertPtr(qwsf);
	int ws;
	CheckHr(qwsf->get_UserWs(&ws));
	return ws;
}
