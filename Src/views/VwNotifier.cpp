/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwNotifier.cpp
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
//:>	Static methods
//:>********************************************************************************************
static DummyFactory g_fact(_T("SIL.Views.VwNotifier"));
static DummyFactory g_factM(_T("SIL.Views.VwMissingNotifier"));
static DummyFactory g_factL(_T("SIL.Views.VwPropListNotifier"));

//:>********************************************************************************************
//:>	VwAbstractNotifier methods
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructors etc.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwAbstractNotifier::VwAbstractNotifier(int ihvoProp)
{
	// COM object behavior
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

	// Other inst variable init
	Assert(m_pboxKey == NULL);
	m_ihvoProp = ihvoProp;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwAbstractNotifier::~VwAbstractNotifier()
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Clean up all external COM references
----------------------------------------------------------------------------------------------*/
void VwAbstractNotifier::Close(void)
{
}


//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwAbstractNotifier::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwNotifyChange)
		*ppv = static_cast<IVwNotifyChange *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwNotifyChange);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	IVwNotifyChange methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Handle receiving a notification that an attribute/property of the object which this
	notifier is monitoring has changed. Update the display.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwAbstractNotifier::PropChanged(HVO hvo, int tag, int ivMin, int cvIns,
	int cvDel)
{
	return S_OK; // default is do nothing (but most concrete classes should override)
}


/*----------------------------------------------------------------------------------------------
	Answer how the argument is related to yourself.
	If the argument is an ancestor or descendant, indicate which property index of the
	ancestor contains the descendant.
	That is, answer kvnrDescendent if the argument is a descendent of the recipient;
	kvnfAfter if the argument comes after the recipient; etc.
----------------------------------------------------------------------------------------------*/
VwNoteRel VwAbstractNotifier::HowRelated(VwAbstractNotifier * panoteArg, int * piprop)
{
	if (panoteArg == this)
	{
		return kvnrSame;
	}

	// The common ancestor of this and panoteUpdate, or null if they have none.
	// Only gets actually set if one is not an ancestor of the other.
	VwAbstractNotifier * panoteCommonAncestor;

	// The child of panoteCommonAncestor which is this or an ancestor of this;
	// or if panoteCommonAncestor is null, the top-level ancestor of this.
	VwAbstractNotifier * panoteChildForThis;

	// Similarly for panoteUpdate
	VwAbstractNotifier * panoteChildForArg;

	if (panoteArg->HasAncestor(this, &panoteChildForArg))
	{
		// panoteArg is a descendant. Update everything after (but not including) it.
		*piprop = panoteChildForArg->PropIndex();
		return kvnrDescendant;
	}

	if (this->HasAncestor(panoteArg, &panoteChildForThis))
	{
		*piprop = panoteChildForThis->PropIndex();
		return kvnrAncestor;
	}

	// OK, we need to find the actual common ancestor and set both ChildFor vars.
	// We will loop until panoteChildForThis's parent is an ancestor of panoteUpdate
	panoteCommonAncestor = NULL;
	for (panoteChildForThis = this;
		panoteChildForThis->Parent() && !panoteCommonAncestor;
		panoteChildForThis = panoteChildForThis->Parent())
	{
		if (panoteArg->HasAncestor(panoteChildForThis->Parent(), &panoteChildForArg))
		{
			panoteCommonAncestor = panoteChildForThis->Parent();
			break; // stop exactly when panoteChildForThis and panoteChildForArg have a common parent,
			// panoteCommonAncestor. We don't want to update panoteChildForThis to its parent before
			// terminating the loop with panoteCommonAncestor not null.
		}
	}

	// Turns out we don't actually care about panoteCommonAncestor; since one is not
	// descended from the other, we just need the relative ordering of the immediate
	// descendants. If both are top-level notifiers, they will be ordered
	// by their ObjectIndex.
	if (panoteChildForThis->PropIndex() < panoteChildForArg->PropIndex())
		return kvnrAfter;

	if (panoteChildForThis->PropIndex() > panoteChildForArg->PropIndex())
		return kvnrBefore;

	if (panoteChildForThis->ObjectIndex() < panoteChildForArg->ObjectIndex())
		return kvnrAfter;

	return kvnrBefore;
}


/*----------------------------------------------------------------------------------------------
	Answer whether this has panote as an ancestor. If so, set panoteChild to the ancestor of
	this which is an immediate child of panote (possibly this itself). If not, set panoteChild
	to the most remote ancestor of this.
----------------------------------------------------------------------------------------------*/
bool VwAbstractNotifier::HasAncestor(VwAbstractNotifier * panote,
	VwAbstractNotifier ** ppanoteChild)
{
	VwAbstractNotifier * panoteChild;
	VwAbstractNotifier * panoteParent;

	for (panoteChild = this, panoteParent = panoteChild->Parent();
		panoteParent && panoteParent != panote;
		panoteChild = panoteParent, panoteParent = panoteChild->Parent())
	{
	}

	*ppanoteChild = panoteChild;

	// i.e., true if it is non-null, since loop ended with it equal to panote
	return panoteParent;
}

/*----------------------------------------------------------------------------------------------
	Add yourself to the map from objects to notifiers. By default the key is the
	notifier's one main object, but VwPropListNotifier needs to override.
----------------------------------------------------------------------------------------------*/
void VwAbstractNotifier::AddToMap(ObjNoteMap & mmhvoqnote)
{
	HVO objKey = Object();
	mmhvoqnote.Insert(objKey, this);
}

#ifdef DEBUG
void VwAbstractNotifier::AssertValid()
{
	// see VwNotifier for implimentation :-)
}
#endif


/*----------------------------------------------------------------------------------------------
	pboxOldFirst..pboxOldLast are (possibly part of) a property, and are being replaced by
	pboxNewFirst..pboxNewLast. If either or both of them are important to this notifier,
	make appropriate updates. Note that it is possible that the two old pointers are the same--
	for example, when a property that used to contain one object (and hence paragraph) now
	contains two.
	In general, pboxOldLast should not be important unless it is your last box, in which
	case, replace that with pboxNewLast.
	If pboxOldFirst is the start of any of your	properties, replace with pboxNewFirst
	Also if it is your key box, fix things.
	Note that this base class method is not called by all subclasses. If the notifier
	has other boxes, deleting the old first box does not necessarily mean deleting the
	whole notifier.
----------------------------------------------------------------------------------------------*/
void VwAbstractNotifier::ReplaceBoxes(VwBox * pboxOldFirst, VwBox * pboxOldLast,
	VwBox * pboxNewFirst, VwBox * pboxNewLast, NotifierVec & vpanoteDel)
{
	if (pboxOldFirst != m_pboxKey)
		return;

	VwRootBox* prootb = m_pboxKey->Root();
	if (pboxNewFirst == NULL)
	{
		prootb->DeleteNotifier(this);
		vpanoteDel.Push(this);
	}
	else
		prootb->ChangeNotifierKey(this, pboxNewFirst);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void VwAbstractNotifier::SetObject(HVO hvo)
{
	m_hvo = hvo;
	Assert(hvo != 0);
}


/*----------------------------------------------------------------------------------------------
	The number of ancestors (including this)
----------------------------------------------------------------------------------------------*/
int VwAbstractNotifier::Level() const
{
	int chvoLevel = 0;

	for (VwAbstractNotifier * pnote = const_cast<VwAbstractNotifier *>(this); pnote;
			pnote = pnote->Parent())
		chvoLevel++;

	return chvoLevel;

}

//:>********************************************************************************************
//:>	VwNotifier methods
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructors etc.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Used instead of constructor which is private.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwNotifier::Create(int ihvoProp, int cprop)
{
	// allocate enough memory to hold the object itself,
	// plus its arrays of box pointers, attr tags, char indices, fragment ids, view constructor
	// pointers, style pointers and flags.
	// (minus the one pointer that is embedded in the Notifier itself).
	return NewObjExtra(
		cprop *
			(isizeof(VwBox *) * 3
			+ isizeof(PropTag)
			+ isizeof(int) * 2
			+ isizeof(VwNoteProps))
		- isizeof(VwBox *))
		VwNotifier(ihvoProp, cprop);
}


/*----------------------------------------------------------------------------------------------
	This construtor is declared private. Use Create() to initialize object.
----------------------------------------------------------------------------------------------*/
VwNotifier::VwNotifier(int ihvoProp, int cprop)
	:VwAbstractNotifier(ihvoProp)
{
	m_cprop = cprop;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwNotifier::~VwNotifier()
{
	// We have to do the Releases manually, because we are not using smart pointers,
	// and even if we were, in 'NewObjExtra' memory the compiler would not know about them.
	VwPropertyStore ** ppzvps = Styles();
	IVwViewConstructor ** ppvvc = Constructors();
	VwPropertyStore ** ppzvpsLim = Styles() + m_cprop;

	for (; ppzvps < ppzvpsLim; ppzvps++, ppvvc++)
	{
		VwPropertyStore * pzvps = *ppzvps;
		if (pzvps)
			((VwPropertyStore *)((long) pzvps & ~1))->Release();

		IVwViewConstructor * pvvc = *ppvvc;
		if (pvvc)
			((IVwViewConstructor *)((long) pvvc & ~1))->Release();
	}
}

/*----------------------------------------------------------------------------------------------
	Common bit of code executed to clean up both normal and error code.
	fError is true if already in error state. If so, ignore any new errors.
	(Note that qzvwenv is also non-zero only in an error state.)
----------------------------------------------------------------------------------------------*/
void Cleanup(VwRootBox * & prootb, VwSelectionState & vss, IVwGraphics * & pvg, VwSelection * & psel,
	bool & fSelShowing, VwEnvPtr & qzvwenv, bool fError)
{
	// Try to clean up and leave things the way they were if something goes wrong in the
	// regenerate process. Also cleans up if we succeeded, or if we need to try the next level
	// out notifier.
	// ENHANCE JohnT: is there some way we can simplify the logic of this method?!!
	// This also needs a try-catch block, as errors could occur
	try
	{
		// BEFORE we try to reactivate anything, clean up any problems and restore things to normal.
		if (qzvwenv)
		{
			// Put the box it is working on back in its standard state if possible; discard anything
			// new that has been made but not used.
			VwBox * pbox = NULL;
			NotifierVec vpanoteDummy;
			// Among other crucial things, restores the original contents of the container of
			// the stuff we were trying to regenerate.
			qzvwenv->GetRegenerateInfo(&pbox, vpanoteDummy, fError);
			// Clean up any notifiers made for the incomplete new stuff. Don't do this if we were
			// successful, we need the new notifiers. Do this before cleaning up the boxes,
			// they assert if deleted while notifiers point at them. Also, by asking the root box
			// for notifiers pointing at them, they transfer the new notifiers into the master hash
			// table, where we can't distinguish them.
			if (prootb)
			{
				NotifierVec vpanoteNew;
				prootb->ExtractNotifiers(&vpanoteNew);
				for (int inote = 0; inote < vpanoteNew.Size(); inote++)
					vpanoteNew[inote]->Close();
			}
			if (pbox)
			{
				// Do our best to clean up boxes created but not yet inserted into display.
				VwBox* pboxNext;

				for (; pbox; pbox = pboxNext)
				{
					pboxNext = pbox->NextOrLazy(); // before we delete it!
					pbox->DeleteContents(NULL, vpanoteDummy); // NULL = don't try to clean up notifiers
					delete pbox;
				}
			}
		}
		prootb->HandleActivate(vss); // Restore the activation state.
		if (pvg)
		{
			// Restore the selection if any
			psel = prootb->Selection();
			if (psel && fSelShowing)
				psel->Show();

			// Release the graphics object.
			prootb->ReleaseGraphics(pvg);
			pvg->Release();
			pvg = NULL;
		}
		prootb->Unlock();
	}
	catch(...)
	{
		prootb->Unlock();
		if (!fError)
			throw; // otherwise, ignore the new error.
	}
}

//:>********************************************************************************************
//:>	IVwNotifyChange methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Overrides the trival VwAbstractNotifier::PropChanged.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwNotifier::PropChanged(HVO hvo, int tag, int ivMin, int cvIns, int cvDel)
{
	BEGIN_COM_METHOD;

	IVwGraphics * pvg = NULL; // get if we need, release in Cleanup.
	VwRootBox * prootb = FirstBox()->Root();
	// These variables are for some data which is only needed if we have to set up a VwEnv, but
	// in case we do, it should be retained for use elsewhere in the method, particularly
	// UpOneLevel:.
	BuildVec vbldrec;
	bool fGotBuildVec = false; // true when we set up vbldrec and pnoteNextOuter
	VwEnvPtr qzvwenv;
	NotifierVec vpanoteNew;		// list of all new notifiers.
	// This is a flag that controls Cleanup: behavior if we go through UpOneLevel.
	bool fTryNextLevel = false;
	VwNotifier * pnoteNextOuter = NULL;
	VwSelection * psel;
	psel = prootb->Selection();
	bool fSelShowing = false;
	VwSelectionState vss = prootb->SelectionState();
	try
	{
		// Hide the selection. Drawing anomalies can happen if we update behind it.
		// Also disable, to stop anyone else turning it back on until we are done.
		prootb->Site()->get_LayoutGraphics(prootb, &pvg);
		if (psel)
		{
			fSelShowing = psel->Showing();
			if (fSelShowing)
				psel->Hide();
			if (vss == vssEnabled)
				prootb->HandleActivate(vssDisabled);
		}
		// OPTIMIZE JohnT: Can we do any better about cases where there has been no change?

		// variables that need this scope so compiler does not complain they are skipped cases and
		// gotos or so that Cleanup can use them too.
		ITsStrFactoryPtr qtsf;
		PropBoxList vpbrec;

		GetPropOccurrences(tag, ivMin, vpbrec);

		// If it is not a property being displayed in this notifier at all, we have nothing to do.
		// We get notified of all prop changes for our object, so it may well happen that the prop
		// that changed is one we have no interest in.
		if (0 == vpbrec.Size())
		{
			Cleanup(prootb, vss, pvg, psel, fSelShowing, qzvwenv, true);
			return S_OK;
		}

		prootb->ResetSpellCheck(); // need to check the changed material.
		prootb->Lock(); // no paints or spelling while we are making changes.

		//For each occurrence of the property in the notifier,
		for (int ipbrec = 0; ipbrec < vpbrec.Size(); ipbrec++)
		{
			// Regenerate that part of the display
			// The build rec includes char offsets and so forth that may be invalidated by earlier
			// replaces. So, make it afresh each time. We would put the variable inside the loop,
			// except that we do use it if we goto UpOneLevel.

			// For any kind of prop-changed regenerate we must have a data access object
			ISilDataAccessPtr qsda = prootb->GetDataAccess();
			if (!qsda)
				ThrowHr(WarnHr(E_FAIL));

			vbldrec.Clear();
			fGotBuildVec = false;

			// Get the info from the PropBoxRec...
			int iprop = vpbrec[ipbrec].iprop;
			VwBox * pboxFirst = vpbrec[ipbrec].pboxFirst;
			VwBox * pboxLast = vpbrec[ipbrec].pboxLast;
			if (!pboxFirst)
				goto UpOneLevel;

			Assert(!dynamic_cast<VwStringBox *>(pboxFirst)); // notifiers never point at string boxes

			int itssMin = vpbrec[ipbrec].itssMin;
			int itssLim = vpbrec[ipbrec].itssLim;

			// Get the info from self about this display of that property and how to rebuild it.
			// We already know the property tag
			IVwViewConstructor * pvvc = Constructors()[iprop];
			VwPropertyStore * pzvps = Styles()[iprop];
			int frag = Fragments()[iprop];
			VwNoteProps vnp = (VwNoteProps)((Flags()[iprop]) & kvnpPropTypeMask);

			// Figure out what will replace the current property contents.
			// Various ways of rebuilding will produce either a replacement string or
			// a replacement box sequence. One variable or the other gets filled in.
			VwBox * pboxSeqNew = NULL;
			ITsStringPtr qtssNew;

			// Flag that controls special cases: when we are not replacing a whole property,
			// or part of one, but doing a pure insert or a pure delete. This requires
			// special treatment at various points.
			UpdateType ut = kutNormal;

			// Probably redundant, but we want to be quite sure that any notifiers we get
			// back from ExtractNotifiers are new.
			prootb->BuildNotifierMap();

			if (pvvc && vnp != kvnpStringProp && vnp != kvnpStringAltMember && vnp != kvnpUnicodeProp)
			{
				// We have a view constructor and will send it some message to rebuild.
				// We need a VwEnv.
				// OPTIMIZE JohnT: could we make one for the first prop and reuse it?
				// Tentative answer: multiple occurrences of a property are rare enough to make the
				// optimization pointless, and also dangerous since it would rarely get tested.
				qzvwenv.Attach(prootb->MakeEnv());

				if (!fGotBuildVec)
				{
					// Get the info we need to initialize a VwEnv for regenerate.
					fGotBuildVec = true;
					GetBuildRecsFor(tag, vbldrec, &pnoteNextOuter);

					if (vbldrec.Size() == 0)
						goto UpOneLevel; // strange, but maybe we can still do something

					// Note that we are quite certain to have built its notifier map in the course
					// of GetBuildRecsFor(). We will use its (therefore empty) notifier vector to
					// accumulate any new notifiers.
					prootb = FirstBox()->Root();
				}

				VwGroupBox * pgboxContainer = pboxFirst->Container();
				Assert(pgboxContainer);

				qzvwenv->InitRegenerate(pvg, prootb,
					pgboxContainer, // temp. put new boxes here
					pzvps, hvo, &vbldrec, iprop);

				// If we are regenerating part of a paragraph, make a dummy paragraph to hold it.
				if (itssMin >= 0)
				{
					Assert(pboxFirst == pboxLast);
					VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pboxFirst);
					Assert(pvpbox);
					// Make a dummy paragraph to put things in. If the original is mapped the
					// replacement needs to be. Because it's a dummy, it doesn't have to be the
					// exact same type.
					if (pvpbox->Source()->IsMapped())
						CheckHr(qzvwenv->OpenMappedPara());
					else
						CheckHr(qzvwenv->OpenParagraph());
				}

				switch (vnp)
				{
				default:
					Warn("Found unexpected property type trying to regenerate a display");
					// If we opened a dummy paragraph, close it.
					if (itssMin >= 0)
						qzvwenv->CloseParagraph();
					goto UpOneLevel;
					//Assert(false);
					//ThrowHr(WarnHr(E_UNEXPECTED));
					break;
				case kvnpProp:
					{ // BLOCK, to avoid compiler complaints about skipping var init
						CheckHr(qzvwenv->AddProp(tag, pvvc, frag));
#if 0
						CheckHr(pvvc->DisplayVariant(qzvwenv, tag, frag, &qtssNew));
						CheckHr(qzvwenv->AddString(qtssNew));
						qtssNew.Clear(); // so code below doesn't think we did it two ways
#endif
					}
					break;
				case kvnpObjVec:
					// Display vector as a whole. Use DisplayVec.
					CheckHr(qzvwenv->AddObjVec(tag, pvvc, frag));
					//CheckHr(pvvc->DisplayVec(qzvwenv, hvo, tag, frag));
					break;
				case kvnpStringAltMember:
					// Display the string.
					// frag is actually an writing system, indicating which alternative to display.
					// The replacement string is just that alternative.
					CheckHr(qzvwenv->AddStringAltMember(tag, frag, pvvc));
					break;
				case kvnpUnicodeProp:
					// Display as a TsString in the indicated ws.
					// frag is actually an writing system, indicating which ws to display.
					CheckHr(qzvwenv->AddUnicodeProp(tag, frag, pvvc));
					break;
				case kvnpObjVecItems:
					{ // BLOCK
						// An object vector property.
						// If we can figure out the range of boxes that corresponds to the items being deleted,
						// just compute replacements for them. Otherwise, replace the whole property.
						ut = AdjustReplacedStuff(pboxFirst, pboxLast, itssMin, itssLim, iprop, ivMin,
							cvIns, cvDel, qsda, hvo, tag);
						if (ut == kutFail)
						{
							// Do a full, normal property replace.
							ut = kutNormal;
							CheckHr(qzvwenv->AddObjVecItems(tag, pvvc, frag));
						}
						else if (cvIns != 0)
						{
							// Compute just the items we are replacing.
							qzvwenv->AddObjVecItemRange(tag, pvvc, frag, ivMin, ivMin + cvIns);
						}
					}
					break;
				case kvnpLazyVecItems:
					ut = UpdateLazyProp(pboxFirst, pboxLast, iprop, ivMin, cvIns, cvDel,
						pboxSeqNew, qzvwenv->m_qzvps, pvvc, tag, frag);
					if (ut == kutFail)
					{
						// Do a full, normal property replace.
						ut = kutNormal;
						CheckHr(qzvwenv->AddLazyVecItems(tag, pvvc, frag));
					}
					break;
				case kvnpObjProp:
					{ // BLOCK
						// Object pointer.
						CheckHr(qzvwenv->AddObjProp(tag, pvvc, frag));
					}
					break;
				case kvnpIntPropPic:
					{
						VwIntegerPictureBox * pipbox = dynamic_cast<VwIntegerPictureBox *>(pboxFirst);
						Assert(pipbox);
						CheckHr(qzvwenv->AddIntPropPic(tag, pvvc, frag, pipbox->MinVal(), pipbox->MaxVal()));
					}
				}

				// If we opened a dummy paragraph, close it.
				if (itssMin >= 0)
					// close the dummy paragraph
					qzvwenv->CloseParagraph();

				// Get the boxes generated out out of qzvwenv. First clear the ptr, so that if
				// some problem occurs, cleanup code won't try to GetRegenerateInfo again.
				VwEnvPtr qzvwenvT;
				qzvwenvT.Attach(qzvwenv.Detach());
				NotifierVec vpanoteTop;
				VwBox * pboxDummy;
				// Call this anyway to clean things up, but if we already made the new box don't
				// overwrite it.
				qzvwenvT->GetRegenerateInfo((pboxSeqNew ? &pboxDummy : &pboxSeqNew), vpanoteTop);
				// Make the top-level notifiers have this as parent
				for (int inote = 0; inote < vpanoteTop.Size(); inote++)
				{
					vpanoteTop[inote]->SetParent(this);
				}
			} // have view constructor
			else
			{
				// We have no view constructor, so figure a default view.
				// Note: modifications to this switch, especially new options, may require
				// a similar change to the switch in VwSelection::OnTyping.
				switch(vnp)
				{
				default:
					// Not a type of property we have an automatic way of building.
					Assert(false);
					ThrowHr(WarnHr(E_UNEXPECTED));
					break;
				case kvnpStringProp:
					// The replacement string is just the current value of the property
					CheckHr(qsda->get_StringProp(hvo, tag, &qtssNew));
					break;
				case kvnpUnicodeProp:
					{
						// The replacement string is the value of the property in the ws indicated by
						// the fragment.
						SmartBstr sbstr;
						CheckHr(qsda->get_UnicodeProp(hvo, tag, &sbstr));
						qtsf.CreateInstance(CLSID_TsStrFactory);
						CheckHr(qtsf->MakeStringRgch(sbstr.Chars(), sbstr.Length(), frag, &qtssNew));
					}
					break;
				case kvnpIntProp:
					// Read the integer, use the standard default conversion to make a string
					int nVal;
					CheckHr(qsda->get_IntProp(hvo, tag, &nVal));
					VwEnv::IntToTsString(nVal, &qtsf, prootb->GetDataAccess(), &qtssNew);
					break;
				case kvnpTimeProp:
					int64 nTime;
					CheckHr(qsda->get_TimeProp(hvo, tag, (int64 *)(&nTime)));
					VwEnv::TimeToTsString(nTime, frag, prootb->GetDataAccess(), &qtssNew);
					break;

				case kvnpStringAltMember:
				case kvnpStringAltSeq:
				case kvnpStringAlt:
					{ // BLOCK
						if (frag)
						{
							// frag is actually an writing system, indicating which alternative to display.
							// The replacement string is just that alternative.
							CheckHr(qsda->get_MultiStringAlt(hvo, tag, frag, &qtssNew));
						}
						else
						{
							// ENHANCE JohnT: call something, probably a static method of VwEnv, to
							// generate the default view of all available alternatives.
							Assert(false);
							ThrowHr(WarnHr(E_NOTIMPL));
						}
					}
					break;
				} // switch (vpn)
			} // no view constructor

			// At this point we either have a box sequence or just a string. If just a string,
			// make it a box
			if (qtssNew)
			{
				Assert(!pboxSeqNew);

				VwParagraphBox * pvpboxOld = dynamic_cast<VwParagraphBox *>(pboxFirst);
				VwSourceType vst = pvpboxOld->Source()->SourceType();
				// This is a dummy box so it doesn't really matter what Style we give it.
				VwParagraphBox * pvpbox = NewObj VwParagraphBox(pzvps, vst);
				if (vst == kvstMapped || vst == kvstMappedTagged)
				{
					// the text source needs a pointer to the root box
					dynamic_cast<VwMappedTxtSrc *>(pvpbox->Source())->SetRoot(prootb);
				}
				pboxSeqNew = pvpbox;
				IVwViewConstructor * pvvc2 = pvvc;
				if ((!pvvc) && Parent())
				{
					// Use the view constructor one level up.
					VwBox * pboxFirstProp;
					int itssFirstProp;
					int tagTmp;
					int ipropTmp;
					Parent()->GetPropForSubNotifier(this, &pboxFirstProp,
						&itssFirstProp, &tagTmp, &ipropTmp);
					pvvc2 = Parent()->Constructors()[ipropTmp];
				}
				pvpbox->Source()->AddString(qtssNew, pzvps, pvvc2);
				//MakeEmbeddedBoxes(pvpbox, qtssNew, pvvc2);
			}
			else if (ut != kutDelItems && ut != kutDone && !pboxSeqNew)
			{
				// OPTIMIZE JohnT: Instead of going up a level, create a VwMissingNotifier.
				goto UpOneLevel;
			}

			// If we were replacing just part of a paragraph, these notifiers are going to be
			// using string offsets based on the new paragraph, which lacks the material in
			// the old one before the property regenerated. Adjust any notifier string offsets
			// which relate to the top level paragraph box.
			// JT 3/11/04: Nice idea, but ReplaceBoxes ALSO has code to handle it, so the offsets
			// were getting adjusted twice.
			//if (StringIndexes()[iprop] > 0)
			//{
			//	for (int inote = 0; inote < vpanoteNew.Size(); inote++)
			//	{
			//		vpanoteNew[inote]->FixOffsets(pboxSeqNew, StringIndexes()[iprop]);
			//	}
			//}

			// Figure out whether we can successfully do the replacement. If the new box sequence
			// is a different "shape" (roughly, a different number or kind of boxes) than we
			// expect, we can't. The special delete and insert items cases have already checked out.
			if (ut == kutNormal && !OkayToReplace(vpbrec[ipbrec], pboxSeqNew))
				goto UpOneLevel;

			// Actually do the replacement, update the display, etc.

			// Get the new notifiers, so nothing will add them to the main map until after
			// we build the structure of ones that might be affected by the replace boxes.
			vpanoteNew.Clear(); // Usually already empty, but if the property occurs more than once...
			prootb->ExtractNotifiers(&vpanoteNew);

			VwBox * pboxLastNew = pboxSeqNew ? pboxSeqNew->EndOfChain() : NULL;
			if (!pvg)
				prootb->Site()->get_LayoutGraphics(prootb, &pvg);

			int dyOld = prootb->FieldHeight();
			int dxOld = prootb->Width();
			if (ut == kutInsItems)
				InsertBoxes(iprop,	itssMin, itssLim, pboxFirst, pboxLast, pboxSeqNew, pboxLastNew,
					vpanoteNew, pvg);
			else if (ut == kutDone)
			{
				// Just need to re-lay-out the containing box.
				// Install and fix notifiers before doing layout.
				prootb->AddNotifiers(&vpanoteNew);
				FixupMap fixmap;
				InsertIntoFixMap(fixmap, NULL, NULL, ContainingBox());
				prootb->RelayoutRoot(pvg, &fixmap);
			}
			else
			{
				ReplaceBoxSeq(iprop, itssMin, itssLim, pboxFirst, pboxLast, pboxSeqNew, pboxLastNew,
					vpanoteNew, pvg);
			}
			if (dyOld != prootb->FieldHeight() || dxOld != prootb->Width())
				CheckHr(prootb->Site()->RootBoxSizeChanged(prootb));
		} // for loop over occurrences of property

		goto Cleanup;

UpOneLevel:
		// Any notifiers we made during the failed attempt are redundant.
		vpanoteNew.Clear(); // Usually already empty, play safe
		prootb->ExtractNotifiers(&vpanoteNew);
		prootb->DeleteNotifierVec(vpanoteNew);

		// We need the build recs--and replacement of an empty string could cause problems even if
		// we didn't need a VwEnv, so we might not have made them yet.
		if (!fGotBuildVec)
			GetBuildRecsFor(tag, vbldrec, &pnoteNextOuter);

		// See if there is another level to try!
		if (!pnoteNextOuter)
		{
			Cleanup(prootb, vss, pvg, psel, fSelShowing, qzvwenv, true);
			// Last resort is to rebuild everything.
			return KeyBox()->Root()->Reconstruct(); // Destroys *this! Do no more method calls!!
		}

		// Fall through into cleanup to fix all our variables, then do higher level.
		// PropChanged.
		fTryNextLevel = true;
	}
	catch (...)
	{
		Cleanup(prootb, vss, pvg, psel, fSelShowing, qzvwenv, true);

		throw;
	}
Cleanup:
	Cleanup(prootb, vss, pvg, psel, fSelShowing, qzvwenv, false);

	if (fTryNextLevel)
	{
		// regenerate next higher level. Pretend we have completely replaced the object that
		// needs redrawing.
		// ENHANCE JohnT: is this the best thing to tell the higher attribute about what changed?
		// Could it cause problems if NotifyChange on the higher object has complex (non-view)
		// side effects? Should we have a special ivMin value to signal a pseudo-change like
		// this? It is probably OK because it would be treated as replacing the object with
		// itself, which should be harmless.

		// However, it's possible (see e.g. LT-11566) that we are modifying an object that
		// has moved, as when merging two senses and keeping the second. If the index is invalid,
		// assume that we will be re-displaying the whole outer sequence as a result of its own
		// changes, and don't bother doing it as a way of showing this change.
		int hvoOuter = pnoteNextOuter->Object();
		int tagOuter = pnoteNextOuter->Tags()[m_ipropParent];
		// We can get a -1 here as in the FM Bangla project with its original settings files.
		if (tagOuter == -1)
			return S_OK;
		int index = ObjectIndex();
		int chvo;
		HRESULT hr;
		IgnoreHr(hr = prootb->GetDataAccess()->get_VecSize(hvoOuter, tagOuter, &chvo));
		if (hr != S_OK)
		{
			// Presumably not a sequence, may be atomic
			HVO hvoChild;
			IgnoreHr(hr = prootb->GetDataAccess()->get_ObjectProp(hvoOuter, tagOuter, &hvoChild));
			if (hr != S_OK)
			{
				// presumably a deleted object. If our parent object has been deleted (e.g., merging entries and
				// both moving and modifying a sense of the old entry), something else should update the properties
				// that used to own the old entry and the one that now owns the moved sense. We don't need to update
				// the display of the modified child property.
				return S_OK;
			}
			chvo = hvoChild == 0 ? 0 : 1;
		}
		if (index >= chvo)
			return S_OK;
		return pnoteNextOuter->PropChanged(hvoOuter, tagOuter, index, 1, 1);
	}

	END_COM_METHOD(g_fact, IID_IVwNotifyChange);
}

/*----------------------------------------------------------------------------------------------
	Given an initial pair of boxes that are property iprop, which is a lazy one, figure out
	how to make the update signified by replacing chvoDel items at ihvoMin with chvoIns new ones.

	- If the change or part of it can be done by adjusting the range of HVOs covered by existing
	lazy boxes, do so. If this leaves a lazy box empty, delete it.
	- If part of it can be done by inserting a new lazy box, do so.
	- If some of the deleted hvos are expanded, delete the associated boxes.

	If necessary adjust the ihvo values of subsequent notifiers and the ihvoMin values of
	subsequent lazy boxes.

	If doing a replacement, return kutNormal; adjust pboxFirst and pboxLast to indicate the
	boxes to be replaced, and set pboxNew to the stuff to insert.

	If doing a deletion, return kutDelItems; adjust pboxFirst and pboxLast, and set pboxNew
	to null.

	If doing an insertion, return kutInsItems; pboxLast should be the box to insert after,
	except that to insert at the start of the property, pboxLast should be null. Don't
	change pboxFirst in these cases.

	If no change to the box sequence is needed (everything was done by modifying lazy boxes),
	return kutDone. The container should in any case recompute its layout, as box sizes may
	have changed.
----------------------------------------------------------------------------------------------*/
VwNotifier::UpdateType VwNotifier::UpdateLazyProp(VwBox * & pboxFirst, VwBox * & pboxLast,
	int iprop, int ihvoMin1, int chvoIns, int chvoDel1, VwBox * & pboxNew,
	VwPropertyStore * pzvps, IVwViewConstructor * pvc, PropTag tag ,int frag)
{
	Assert(pboxFirst); // Don't use for initially empty prop.
	VwNotifier::UpdateType ut = kutNormal;
	pboxNew = NULL; // default

	// This is the number of items we still have to delete.
	int chvoDel = chvoDel1;
	// This is the first to delete (modified as we do deletions)
	int ihvoMin = ihvoMin1;

	// This is the amount to add to ObjectIndex() for any subsequent notifiers and to
	// MinObjIndex() for any subsequent lazy boxes.
	int dihvo = chvoIns - chvoDel; // amount to add to ObjectIndex()

	// Get the items to insert
	VwRootBox * prootb = FirstBox()->Root();
	ISilDataAccess * psda = prootb->GetDataAccess();
	if (ihvoMin == 0)
	{
		int chvo;
		CheckHr(psda->get_VecSize(m_hvo, tag, &chvo));
		if (chvo == chvoIns)
		{
			// We are processing a change that involves inserting everything there is. This is
			// sometimes used to "clean things up" when we are not too sure how many things there
			// used to be, and if we are inserting everything, we certainly want to replace
			// whatever we're showing now with everything; no optimization is possible.
			// So it is both safer and more efficient to just go ahead and replace the whole
			// property, which we can achieve by "failing".
			// If there are no items at all we can treat it as deleting everything, which is even
			// more efficient and may save us going up a level.
			return chvo == 0 ? kutDelItems : kutFail;
		}
	}
	HvoVec vhvoInsItems;
	for (int ihvo = ihvoMin; ihvo < ihvoMin + chvoIns; ihvo++)
	{
		int iDisplayIndex;
		CheckHr(psda->GetDisplayIndex(m_hvo, tag, ihvo, &iDisplayIndex));
		if (iDisplayIndex >= 0)
		{
			HVO hvoItem;
			CheckHr(psda->get_VecItem(m_hvo, tag, iDisplayIndex, &hvoItem));
			vhvoInsItems.Push(hvoItem);
		}
		else
			return kutFail;	// item being updated is no longer displayed, will need to rebuild display
	}

	NotifierMap * pmmboxqnote;
	prootb->GetNotifierMap(&pmmboxqnote);

	VwBox * pboxFirstRep = NULL; // first box we need to replace, if any.
	VwBox * pboxLastRep = NULL; // last box to replace, if any.

	VwBox * pboxPrev = NULL; // box to insert after, if any.
	// a lazy box we can insert into, if we find one. It's fairly critical that it be
	// left in the right state for the following code, if it is set at all.
	VwLazyBox * plzbIns = NULL;

	// This functions as a "limit" for this property. If we keep advancing through its boxes
	// and get here we have reached (past) the end of the boxes.
	VwBox * pboxLimProp = GetLimOfProp(iprop);

	for (VwBox * pbox = pboxFirst; pbox != pboxLimProp; pbox = pbox->NextOrLazy())
	{
		VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(pbox);
		if (plzb && plzb->Object() == Object()) // JT: is this enough to ensure it is the right flid?
		{
			int ihvoMinLazy = plzb->MinObjIndex();
			int chvoLazy = plzb->CItems();
			if (ihvoMinLazy >= ihvoMin + chvoDel)
			{
				// We've found a lazy box after the part we need to replace.
				// If it starts exactly at ihvoMin + chvoDel (right after the delted objects if any)
				// we can insert into it.
				if (!plzbIns && ihvoMinLazy == ihvoMin + chvoDel)
				{
					plzbIns = plzb;
					// It will start exactly at ihvoMin, after inserting the items.
					// Unlike any other lazy box, it is affected by chvoDel but not chvoIns
					plzbIns->m_ihvoMin = ihvoMin;
				}
				else
					plzb->m_ihvoMin += dihvo;
				if (!dihvo)
					break;
			}
			else if (ihvoMinLazy + chvoLazy <= ihvoMin)
			{
				// We've found a lazy box before the part we need to replace
				// If it's exactly on the border we can insert into it.
				if (ihvoMinLazy + chvoLazy == ihvoMin)
					plzbIns = plzb;
				// If we don't find a later one it will be the box before the range
				pboxPrev = pbox;
				continue;
			}
			// This is tricky. The first two conditions identify a situation where all the objects
			// that this lazy box represents are being deleted. In such a case, usually the lazy box
			// will be deleted entirely. But in certain circumstances, it is possible to reuse it
			// as the box that will represent the things inserted.
			// For that to work, three things must be true, determined by the three values ORed
			// (if any of them is true, we go ahead and delete the lazy box):
			// - if there's nothing to insert, there's nothing to reuse it for.
			// - if we already found a lazy box to insert the new items into, we don't need another.
			// - if we already started our list of things to delete, and there is going to be
			//	more stuff to delete after this, it's going to get deleted whether we like it or not,
			//	because it will be between pboxFirstRep and pboxLastRep.
			else if (ihvoMinLazy >= ihvoMin && ihvoMinLazy + chvoLazy <= ihvoMin + chvoDel &&
				((!chvoIns) || plzbIns  || (pboxFirstRep && ihvoMinLazy + chvoLazy < ihvoMin + chvoDel)))
			{
				// We're going to delete this entire lazy box
				if (!pboxFirstRep)
					pboxFirstRep = plzb;
				pboxLastRep = plzb;
				continue;
			}
			else
			{
				// We've found one that overlaps the range we're deleting and is not strictly
				// contained within it. We're going to decrease its
				// range so as to eliminate (some of) the deleted items.
				// We will also insert the new items into it, if we haven't already found
				// a suitable lazy box to insert into (e.g., because the delete range extended
				// from somewhere in a previous lazy box to this one).
				int ihvoMinRep = std::max(ihvoMin, ihvoMinLazy);
				int ihvoLimRep = std::min(ihvoMin + chvoDel, ihvoMinLazy + chvoLazy);
				if (!plzbIns)
					plzbIns = plzb;
				plzb->m_vwlziItems.Replace(ihvoMinRep - ihvoMinLazy, ihvoLimRep - ihvoMinLazy, NULL, 0);
				plzb->m_dysUniformHeightEstimate = 0; // must recalculate all heights, we inserted items.
				plzb->_Height(0); // force relayout when redoing container
				if (ihvoMin < ihvoMinLazy)
				{
					// We deleted stuff before this lazy box as well as possibly part of it...
					// its start index must now be ihvoMin, because anything after that before this
					// has been deleted. However, if it isn't plzbIns, we're going to insert into
					// some earlier lazy box, so it will also be increased by chvoIns
					plzb->m_ihvoMin = ihvoMin + (plzb == plzbIns ? 0 : chvoIns);
				}
				// PLEASE REVIEW: JohnT
				// The condition above possibly should be <= instead of < to work properly.
				// See TE-7276. (It took about 2 man-days to figure this out...)  However, not
				// having the courage of our convictions, and with JohnT on vacation, we added
				// the following minimal fix.
				else if (ihvoMin == ihvoMinLazy && plzb != plzbIns && chvoIns > 0)
				{
					plzb->m_ihvoMin = ihvoMin + chvoIns;
				}
			}
		}
		else // not a lazy box for this property
		{
			// We should be able to find a sub-notifier of this one that points at it.
			NotifierMap::iterator itboxnote;
			NotifierMap::iterator itboxnoteLim;
			pmmboxqnote->Retrieve(pbox, &itboxnote, &itboxnoteLim);
			VwNotifier * pnoteSub = NULL;
			// loop over the candidate notifiers till we find one of our own children.
			for (; itboxnote != itboxnoteLim; ++itboxnote)
			{
				pnoteSub = dynamic_cast<VwNotifier *>(itboxnote.GetValue().Ptr());
				if (pnoteSub && pnoteSub->Parent() == this)
					break;
			}
			Assert(pnoteSub); // any box in this property that isn't lazy should belong to a sub.
			int ihvoSub = pnoteSub->ObjectIndex();
			if (ihvoSub >= ihvoMin)
			{
				if (ihvoSub >= ihvoMin + chvoDel)
				{
					// We're past the boxes we want to replace
					if (!dihvo)
						break;
					pnoteSub->SetObjectIndex(pnoteSub->ObjectIndex() + dihvo);
				}
				else
				{
					// OK, this is one of the ones to replace. Make sure it's in the sequence.
					if (!pboxFirstRep)
						pboxFirstRep = pbox;
					pboxLastRep = pnoteSub->LastTopLevelBox();
				}
			}
			else
			{
				// It is before the target range, and thus defines pboxPrev if we don't find
				// something later.
				pboxPrev = pnoteSub->LastTopLevelBox();
				if (!pboxPrev)
					return kutFail;
			}
			// Allows us to skip intermediate boxes in this object.
			pbox = pnoteSub->LastTopLevelBox();
			if (!pbox)
				return kutFail;
		}
	}
	if (chvoIns)
	{
		if (plzbIns)
		{
			int ihvoMinLazy = plzbIns->MinObjIndex();
			// If we deleted some expanded hvos before it, we need to adjust its
			// starting point.
			if (ihvoMinLazy > ihvoMin)
			{
				plzbIns->m_ihvoMin = ihvoMin;
				ihvoMinLazy = ihvoMin;
			}
			plzbIns->m_vwlziItems.Replace(ihvoMin - ihvoMinLazy, ihvoMin - ihvoMinLazy,
				vhvoInsItems.Begin(), vhvoInsItems.Size());
			plzbIns->m_dysUniformHeightEstimate = 0; // must recalculate all heights, we inserted items.
			plzbIns->_Height(0); // force relayout when redoing container
			// Really an insert, but we've done it already without having to insert a box,
			// so it feels to the client like a delete or no-op.
			ut = pboxFirstRep ? kutDelItems : kutDone;
		}
		else
		{
			plzbIns = NewObj VwLazyBox(pzvps, vhvoInsItems.Begin(), vhvoInsItems.Size(), ihvoMin,
				pvc, frag, Object());
			plzbIns->Container(ContainingBox());
			pboxNew = plzbIns;
			if (!pboxFirstRep)
			{
				ut = kutInsItems;
				pboxLast = pboxPrev;
			}
		}
	}
	else
	{
		ut = pboxFirstRep ? kutDelItems : kutDone;
	}
	if (pboxFirstRep)
	{
		pboxFirst = pboxFirstRep;
		pboxLast = pboxLastRep;
	}

	// Finally we need to clean up any VwMissingNotifiers that are affected.
	NotifierMap::iterator itboxnote;
	NotifierMap::iterator itboxnoteLim;
	NotifierVec nvDelete;	// temp array for notifiers to be deleted
	VwBox * pboxContainer = ContainingBox();
	pmmboxqnote->Retrieve(pboxContainer, &itboxnote, &itboxnoteLim);
	// loop over the candidate notifiers, processing missing ones.
	for (; itboxnote != itboxnoteLim; ++itboxnote)
	{
		VwMissingNotifier * pmnote = dynamic_cast<VwMissingNotifier *>(itboxnote.GetValue().Ptr());
		if (!pmnote || pmnote->Parent() != this ||
			pmnote->PropIndex() != iprop || pmnote->ObjectIndex() < ihvoMin)
		{
			continue;
		}
		if (pmnote->ObjectIndex() < ihvoMin + chvoDel)
			nvDelete.Push(pmnote); // save notifier to be deleted - deleting now will corrupt iterator
		else
			pmnote->SetObjectIndex(pmnote->ObjectIndex() + dihvo);
	}
	// delete notifiers
	for (int i = 0; i < nvDelete.Size(); i++)
		prootb->DeleteNotifier(nvDelete[i]);

	return ut;
}

#ifdef DEBUG
void VwNotifier::AssertValid()
{
	VwBox ** ppbox = Boxes();

	for (int i = 0; i < m_cprop; ppbox++, i++)
	{
		for (VwBox * pbox = *ppbox; pbox; pbox = pbox->NextOrLazy())
			AssertPtr(pbox);
	}
}
#endif

/*----------------------------------------------------------------------------------------------
	Given an initial pair of boxes and range of strings (itssMin indicates the first string
	in pboxFirst, itssLim indicates the end of the range in pboxLast) that are property
	iprop, narrow the range to just some of the objects in that property, specifically, those
	from ihvoMin to ihvoMin + chvoDel, thus indicating the range of stuff to be deleted
	and possibly replaced with new material.

	If chvoDel is 0, nothing will be deleted. This is awkward if dealing with a box sequence,
	because we were counting on
	a list of boxes to delete to also indicate where we should insert. (No problem if
	dealing with a string sequence, we can just use indexes to tell where to insert.)
		- if not inserting at the start, set pboxLast to indicate the end of the
		previous object, and hence where to insert after. This works even if inserting at the
		end, so there is nothing to insert before.
		- if we are inserting at the start of the property, set pboxLast to null to indicate
		this. pboxFirst will be left pointing at the first box, the one to insert before.
		(We don't call this routine at all if there is nothing in the property to start with.)

	Also adjust the PropIndex of any subsequent notifiers.

	This is used when replacing items in an AddObjVecItems sequence, so there are no boxes or
	strings in the range that are not part of the property. Also, no closures that would expand
	to this property. So, we can use something a bit simpler than FindChild.

	Note that itssMin/Lim should be adjusted only if >= 0. -1 indicates that the property
	being edited is not a string sequence one, and the limits are irrelevant.

	Return karNormal if a successful adjustment was made and we can go ahead with a replace.
		In this case pboxFirst etc. are changed to indicate the reduced range of stuff to
		delete.
	Return karFail if we must replace the whole property.
		In this case pboxFirst etc. are unchanged.
	Return karIns if we are doing an insertion.
----------------------------------------------------------------------------------------------*/
VwNotifier::UpdateType VwNotifier::AdjustReplacedStuff(VwBox * & pboxFirst, VwBox * & pboxLast,
	int & itssMin,int & itssLim, int iprop, int ihvoMin, int chvoIns, int chvoDel,
	ISilDataAccess * psda, HVO hvo, int tag)
{
	if (ihvoMin == 0)
	{
		int chvo;
		CheckHr(psda->get_VecSize(hvo, tag, &chvo));
		if (chvo == chvoIns)
		{
			// We are processing a change that involves inserting everything there is. This is
			// sometimes used to "clean things up" when we are not too sure how many things there
			// used to be, and if we are inserting everything, we certainly want to replace
			// whatever we're showing now with everything; no optimization is possible.
			// So it is both safer and more efficient to just to ahead and replace the whole
			// property, which we can achieve by "failing".
			// If there are no items at all we can treat it as deleting everything, which is even
			// more efficient and may save us going up a level.
			return chvo == 0 ? kutDelItems : kutFail;
		}
	}
	// Any given property is either entirely within one paragraph box and has both itssMin
	// and itssLim, or else doesn't have string limits.
	Assert(itssMin * itssLim >= 0); // Can't have just one of them negative.
	Assert(itssMin < 0 || pboxFirst == pboxLast);

	UpdateType ut = kutNormal;
	if (!chvoDel)
		ut = kutInsItems; // pure insertion
	else if (!chvoIns)
		ut = kutDelItems; // pure deletion, OK for new box seq to be empty.

	VwNotifier * pnote = NextNotifierAt(Level() + 1, pboxFirst, itssMin);
	if (!pnote)
		return kutFail; // Property initially empty, do full normal update.
	if (pnote->Parent() != this)
		return kutFail; // Ran out of our objects, found something else, again => empty prop
	VwNotifier * pnoteLast = NULL; // Last object notifier before pnote
	for (; pnote && pnote->ObjectIndex() < ihvoMin; pnote = pnote->NextNotifier())
		pnoteLast = pnote;
	if (!pnote)
	{
		// Assume because we are inserting at the end of the property, if that is feasible?
		if (!chvoDel && (!pnoteLast || pnoteLast->ObjectIndex() == ihvoMin - 1))
		{
			if (itssMin >= 0)
			{
				itssMin = itssLim; // produces insert at end
				return kutInsItems; // nothing after these items to fix.
			}
			// It's a sequence of whole boxes we're inserting at the end of.
			// If we didn't find a previous notifier to insert after, it's probably an empty
			// property, and it won't hurt to do a full replace
			if (!pnoteLast)
				return kutFail;
			// Otherwise we want to insert right after pnoteLast.
			// We want to make pboxLast the last box of pnoteLast.
			VwBox * pboxLastT = pnoteLast->LastTopLevelBox();
			if (pboxLastT)
			{
				pboxLast = pboxLastT;
				return kutInsItems;
			}
			else
			{
				return kutFail;  // weird...try a complete replace, or up a level.
			}
		}
		else
			return kutFail; // ran out without finding it, maybe no box generated for this object?
	}
	if (pnote && pnote->ObjectIndex() != ihvoMin)
		return kutFail; // got past index without finding it, maybe same reason?

	// Found a notifier corresponding to the first object we are replacing, or the one
	// we are inserting before.
	VwNotifier * pnoteMin = pnote;

	if (chvoDel) // we must have a pnote
	{
		int ihvoLast = ihvoMin + chvoDel - 1;
		VwNotifier * pnoteLastInProp;
		if (ihvoLast == ihvoMin)
			pnoteLastInProp = pnoteMin;
		else
		{
			for (; pnote && pnote->ObjectIndex() < ihvoLast; pnote = pnote->NextNotifier())
				;
			if (!pnote)
				return kutFail; // ran out without finding it, maybe no box generated for this object?
			if (pnote->ObjectIndex() != ihvoLast)
				return kutFail; // got past index without finding it, maybe same reason?
			pnoteLastInProp = pnote;
		}
		if (itssLim > 0)
		{
			int ipropSub;
			if (pnoteMin->FirstBox(&ipropSub) != pboxFirst)
				return kutFail;
			int itssMinT = pnoteMin->StringIndexes()[ipropSub];
			if (itssMinT < 0)
				return kutFail;
			int itssLimT;
			// We're dealing with substrings. If the embedded notifier is not a range of substrings
			// for the same paragraph, give up the optimization.
			// OPTIMIZE JohnT: it might be possible to figure out what string index is being replaced
			// in the parent in this case...or some of them...if we really need to...but the big savings
			// of this optimization come at the super-paragraph level. Still, inserting words into
			// interlinear text might be a non-trivial gain.
			// In that case, we'd test using LastTopLevelBox to make sure it's part of the same
			// paragraph...but if LastBox() != pboxLast, then LastStringIndex() is irrelevant, it
			// applies to a different paragraph. We'd need a new method to find the last string index
			// IN THE RIGHT PARAGRAPH...and I'm not sure how to compute that. It might be right to
			// find the last property that has the right paragraph as its box and use that index.
			if (pnoteLastInProp->LastBox() != pboxLast)
			{
				// This may be grounds to fail, but it's possible that pnoteLastInProp ends at the end of
				// an embedded box. In that case, we can still replace just that part of the paragraph.
				// This is important for interlinear text. We can only consider this optimization
				// if replacing strings entirely in one box, and if pnoteLastInProp runs to the end
				// of its paragraph.
				if (pboxLast != pboxFirst || pnoteLastInProp->LastStringIndex() != -1)
					return kutFail;
				VwBox * pboxEndLast = pnoteLastInProp->LastBox();
				while (pboxEndLast != NULL)
				{
					VwGroupBox * pgboxCont = pboxEndLast->Container();
					if (pgboxCont == pboxLast)
					{
						// OK! pnoteLast ends at the end of a complete box in the containing paragraph.
						VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pboxLast);
						itssLimT = pvpbox->StringIndexOfChildBox(pboxEndLast) + 1; // +1 to make it a lim
						break;
					}
					if (pboxEndLast != pgboxCont->LastBox())
						return kutFail;
					pboxEndLast = pgboxCont;
				}
				if (pboxEndLast == NULL)
					return kutFail; // end is not a child of pboxLast at all.
			}
			else
			{
				// pnoteLastInProp points at the main paragraph, it better NOT have a non-string final index.
				itssLimT = pnoteLastInProp->LastStringIndex();
				if (itssLimT < 0)
					return kutFail;
			}
			itssLim = itssLimT;
			itssMin = itssMinT;
		}
		else
		{
			// Dealing with whole boxes. Leave itssMin/Lim -1.
			VwBox * pboxLastT = pnoteLastInProp->LastTopLevelBox();
			if (pboxLastT)
			{
				pboxFirst = pnoteMin->FirstBox(); // don't mofify if we're going to return kutFail.
				pboxLast = pboxLastT;
				// Falls through to figuring out dihvo and correcting subs notifiers.
			}
			else
			{
				return kutFail;  // weird...try a complete replace, or up a level.
			}
		}
	}
	else if (pnoteLast)
	{
		// Insert, and proved not to be at start because we found a previous item
		if (itssLim > 0)
		{
			// We're dealing with substrings. If the embedded notifier is not a range of substrings
			// for the same paragraph, give up the optimization.
			// OPTIMIZE JohnT: it might be possible to figure out what string index is being replaced
			// in the parent in this case...or some of them...if we really need to...but the big savings
			// of this optimization come at the super-paragraph level.
			if (pnoteLast->LastBox() != pboxLast)
				return kutFail;
			int itssLimT = pnoteLast->LastStringIndex();
			if (itssLimT < 0)
				return kutFail;
			itssLim = itssLimT;
		}
		else
		{
			// Dealing with whole boxes. Leave itssMin/Lim -1.
			pboxLast = pnoteLast->LastTopLevelBox();
			if (!pboxLast)
			{
				return kutFail;  // might let us recover.
			}
		}
	}
	else
	{
		// Insert at beginning
		pboxLast = NULL;
	}

	int dihvo = chvoIns - chvoDel; // amount to add to ObjectIndex()
	if (!dihvo)
		return ut;

	// Otherwise we have to fix any subsequent notifiers.
	// But, we can't fix one before we get the following one, or we get an Assert
	// in NextNotifier, because our own object index has changed and may be inconsistent.
	for (; pnote; )
	{
		VwNotifier * pnoteNext = pnote->NextNotifier();
		pnote->SetObjectIndex(pnote->ObjectIndex() + dihvo);
		pnote = pnoteNext;
	}

	// Also any missing-box notifiers. There are a few options for key boxes:
	// 1. Each object is adding boxes to a container. In this case, the missing notifier will
	// have pboxFirst->Container() as its key.
	// 2. Each object is adding strings to a paragraph. In this case, the missing notifier will
	// have pboxFirst as its key.
	// 3. (Conceivably) some objects add strings to a paragraph (say, just punctuation),
	// others add inner piles. In this case also the box that is open at the start and end of
	// each property is the container.
	// Simplest is just to check pboxFirst and its container.
	VwBox * pboxKey = pboxFirst;
	for (int i = 0; i < 2 && pboxKey; i++, pboxKey = pboxKey ->Container())
	{
		NotifierMap * pmmboxqnote;
		pboxFirst->Root()->GetNotifierMap(&pmmboxqnote);
		NotifierMap::iterator itboxnote;
		NotifierMap::iterator itboxnoteLim;
		pmmboxqnote->Retrieve(pboxKey, &itboxnote, &itboxnoteLim);
		// loop over the candidate notifiers, processing missing ones.
		NotifierVec deletedNotifiers; // build a vector of them because we must not delete during the iteration.
		for (; itboxnote != itboxnoteLim; ++itboxnote)
		{
			VwMissingNotifier * pmnote = dynamic_cast<VwMissingNotifier *>(itboxnote.GetValue().Ptr());
			if (!pmnote || pmnote->Parent() != this ||
				pmnote->PropIndex() != iprop || pmnote->ObjectIndex() < ihvoMin)
			{
				continue;
			}
			if (pmnote->ObjectIndex() < ihvoMin + chvoDel)
				deletedNotifiers.Push(pmnote);
			else
				pmnote->SetObjectIndex(pmnote->ObjectIndex() + dihvo);
		}
		// OK to delete them now the iterator is done.
		pboxFirst->Root()->DeleteNotifierVec(deletedNotifiers);
	}

	return ut;
}

/*----------------------------------------------------------------------------------------------
	Get the list of occurrences of the specified property within self, with the associated box
	and char range info.
----------------------------------------------------------------------------------------------*/
void VwNotifier::GetPropOccurrences(int tag, int ws, PropBoxList& vpbrec)
{
	int * ptag = Tags();
	int * pfrag = Fragments();
	VwNoteProps * pvnp = Flags();

	for (int i = 0; i < m_cprop; i++)
	{
		int frag = *pfrag++;
		VwNoteProps vnp = (VwNoteProps) (*pvnp++ & kvnpPropTypeMask);
		bool isMultiStr = vnp == kvnpStringAltMember || vnp == kvnpStringAltSeq || vnp == kvnpStringAlt;

		if (*ptag++ == tag && (!isMultiStr || frag == ws))
		{
			// found occurrence of target attr
			PropBoxRec pbrec;

			LimitsOfPropAt(i, &pbrec.pboxFirst, &pbrec.pboxLast, &pbrec.itssMin, &pbrec.itssLim);
			pbrec.iprop = i;
			vpbrec.Push(pbrec);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Answer an indication of what needs to be replaced for property iprop.
	If it is a property contained in a paragraph, set *ppboxFirst and *ppboxLast to the
	paragraph, and *pitssMin and *pitssLim to the range of strings to be replaced.
	If it is not contained in a paragraph, set *ppboxFirst and *ppboxLast to indicate the
	first and last boxes in the linked list to be replaced, and *pitssMin and *pitssLim to -1.
----------------------------------------------------------------------------------------------*/
void VwNotifier::LimitsOfPropAt(int iprop, VwBox ** ppboxFirst, VwBox ** ppboxLast,
	int * pitssMin, int * pitssLim)
{
	AssertPtr(ppboxFirst);
	AssertPtr(ppboxLast);
	AssertPtr(pitssMin);
	AssertPtr(pitssLim);

	// The starting values are given directly in the notifier record for the indicated slot.
	VwBox * pboxFirst = Boxes()[iprop];

	*ppboxFirst = pboxFirst;
	*pitssMin = StringIndexes()[iprop];
	if (!pboxFirst)
	{
		// There are no boxes associated with this occurrence of the property at all, so we
		// can't find last ones either.
		*ppboxLast = NULL;
		Assert(*pitssMin == -1); // no para box, should not have index
		*pitssLim = -1;
		return;
	}

	// Find the index and first box of the next property, if any, that has a box.
	int ipropNext = iprop + 1;
	VwBox * pboxLim = NULL;

	while (ipropNext < m_cprop && (pboxLim = Boxes()[ipropNext]) == NULL)
		++ipropNext;

	if (pboxLim)
	{
		// There is a subsequent property with non-null box.
		if (*pitssMin < 0)
		{
			// Property not contained in a paragraph.
			// We want either the box before pboxLim, or the very last box, in the chain starting at
			// pboxFirst
			VwBox * pboxLast = pboxFirst;
			for (VwBox * pbox = pboxFirst; pbox && pbox != pboxLim; pbox=pbox->NextOrLazy())
			{
				// We have a box (non-null) in the chain, and we haven't yet come to pboxLim.
				// The last time this executes sets up the value we really want.
				pboxLast = pbox;
			}
			*ppboxLast = pboxLast;
			*pitssLim = -1;
		}
		else
		{
			// Property in a paragraph. If the next property is in the same paragraph,
			// use it as a limit.
			*ppboxLast = pboxFirst;
			if (pboxLim == pboxFirst)
			{
				// Next prop in the same box, lim is where it starts.
				*pitssLim = StringIndexes()[ipropNext];
			}
			else
			{
				// Next prop is a different box, we want all the rest of this one
				VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pboxFirst);
				Assert(pvpbox);
				*pitssLim = pvpbox->Source()->Vpst().Size();
			}
		}
	}
	else
	{
		// No subsequent non-empty property.
		if (*pitssMin < 0)
		{
			// non-paragraph property
			*ppboxLast = m_pboxLast;
			*pitssLim = -1;
		}
		else
		{
			*ppboxLast = pboxFirst;
			*pitssLim = m_itssLimSubString;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Build the vector that VwEnv needs to reconstruct its view of a specified prop.
		- for each notifier above self (smaller level), make an entry. Its object field
		is the object of that notifier.

		- to get the tag and cprop fields, we must figure out which of the
		props of that object the next level (down) notifier is part of.

		- if the next level notifier's key box is in the box chain for its parent
		notifier, we just note which property that box is part of (and count
		how many times previously it occurs in the upper notifier)

		- otherwise, we have to look for a container of the lower level notifier's box in the
		higher one's list:

		- build a set of containers of the first box of the lower notifier, including
		that box itself

		- check each box in the current notifier until we find one in the set

		- then go back and count how many times this prop has occurred.

		- the objindex field is filled in from the notifier itself.

		- the last item in the vector has the object and objIndex from self, the
		prop tag from the prop that is changing, and a count indicating how many times that prop
		occurs.

	Set *ppnoteNextOuter to the next outer notifier to which we should send a notifyChange
	if regenerating at the current level does not yield something sufficiently similar for a
	reliable replacement.
----------------------------------------------------------------------------------------------*/
void VwNotifier::GetBuildRecsFor(int tag, BuildVec &vbldrec, VwNotifier ** ppnoteNextOuter)
{
	*ppnoteNextOuter = Parent();

	int ipropStart = 0; // start search for prop from beginning
	VwBox * pboxFirst; // first box of first occurrence of prop

	// If the target prop does not occur there is nothing to do.
	if (!GetBoxForProp(tag, ipropStart, &pboxFirst))
	{
		Assert(false); // Very bad. We probably added a wrong tag in the view constructor.
		return;
	}

	// ipropStart is updated to one after the place we found it.

	//	Get all ancestor notifiers, including this
	NotifierVec vpanote;
	for (VwNotifier * pnote = this; pnote; pnote = pnote->Parent())
		vpanote.Push(pnote);

	BuildRec bldrec;

	// Loop through all the higher levels (omit this, which may not have another notifier nested
	// inside it).
	for (int i = vpanote.Size() - 1; i >= 1; i--)
	{
		//	Notifiers outside of something we are rebuilding should be the basic type
		VwNotifier * pnoteOuter = dynamic_cast<VwNotifier *>(vpanote[i].Ptr());
		Assert(pnoteOuter);
		VwNotifier * pnoteInner = dynamic_cast<VwNotifier *>(vpanote[i - 1].Ptr());
		Assert(pnoteInner);

		//	Make a record allowing the rebuild to select the relevant sub-component.
		bldrec.hvo = pnoteOuter->Object();
		bldrec.ihvo = pnoteOuter->ObjectIndex();
		int tagNotifier;  // which prop of pnoteOuter contains the next level notifier
		VwBox * pboxFirstProp; // first box of that prop
		int itssFirstProp; // first string of the prop, or -1 if not in para
		int iprop;

		pnoteOuter->GetPropForSubNotifier(pnoteInner, &pboxFirstProp, &itssFirstProp,
			&tagNotifier, &iprop);
		bldrec.tag = tagNotifier;
		bldrec.cprop = pnoteOuter->PropCount(tagNotifier, pboxFirstProp, itssFirstProp);
		vbldrec.Push(bldrec);
	}

	//	Build the last record; its prop count is the total number of occurrences.
	bldrec.hvo = this->Object();
	bldrec.ihvo = this->ObjectIndex();
	bldrec.tag = tag;
	bldrec.cprop = this->PropCount(tag);
	vbldrec.Push(bldrec);
}


/*----------------------------------------------------------------------------------------------
	Get the (first) box, if any, associated with a particular property tag, at or following a
	particular slot in the notifier.
	Set result to NULL and return false if prop not found.
	Return true if prop found (even if it has no boxes).
	May be used repeatedly to find multiple occurrences of the property.
	Arguments:
		ipropStartInOut indicates where in the list of props for this object to start the
		search, and is adjusted to indicate where to start the next search (the prop following
		the one that matches the tag), in case the same property is used twice in the
		display of one object.
----------------------------------------------------------------------------------------------*/
bool VwNotifier::GetBoxForProp(int tag, int& ipropStartInOut, VwBox ** ppboxRet)
{
	int * ptag = Tags() + ipropStartInOut;
	int i = ipropStartInOut;

	for (; i < m_cprop; i++, ptag++)
	{
		if (*ptag == tag)
		{
			ipropStartInOut = i+1;
			*ppboxRet = Boxes()[i];
			return true;
		}
	}
	*ppboxRet = NULL;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Get the iprop index of the ipropTagth occurrence of tag; or -1 if not that often.
----------------------------------------------------------------------------------------------*/
int VwNotifier::TagOccurrence(int tag, int ipropTag)
{
	int iprop = 0;

	int ctag = 0;

	for (int * ptag = Tags(); iprop < m_cprop; iprop++, ptag++)
	{
		if (*ptag == tag)
		{
			if (ctag >= ipropTag)
				return iprop;
			ctag++;
		}
	}
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Given that pnote is a notifier embedded inside this, which means that it is for an object
	which is in one of the properties of this, and so entirely belongs to some one property
	of this, and also that it is a notifier which actually has some box associated with it,
	so that box should be (or be contained in) one of the recipient's boxes,
	figure which prop of this it is part of; also return the first box and string index
	of the recipient for that property.

	This is quite tricky. The first box of the embedded notifier might be in any of the
	recipient's property box chains, or embedded in any of those boxes. Also, each chain may
	terminate at a null or at the next property boundary or just at m_pboxLast.

	The strategy is to build a set of the first box of the subnotifier and all its containers.
	Then, we scan the boxes of this notifier, property by property, till we find one that is in
	the set.
----------------------------------------------------------------------------------------------*/
void VwNotifier::GetPropForSubNotifier(VwNotifier * pnote, VwBox ** ppboxFirstProp,
	int * pitssFirstProp, int * ptag, int * piprop)
{
	bool fGotSomething = false;

	//Build the set of containers of the sub-notifier's first box.
	BoxSet boxsetTargets;
	int iprop = 0;
	VwBox * pboxSubFirst = pnote->FirstBox(&iprop);

	// Make a set of the box and all its containers up to but not including any containing
	// VwMoveablePileBox. If we find an mpbox, remember it.
	for (VwBox * pbox=pboxSubFirst; pbox; pbox = pbox->Container())
	{
		if (pbox->IsMoveablePile())
		{
			VwMoveablePileBox * pmpbox = dynamic_cast<VwMoveablePileBox *>(pbox);
			// If this is the parent of the mpbox, pnote is a top-level
			// notifier in the mpbox, and it knows which property.
			if (pmpbox->Notifier() == this)
			{
				int ipropTmp = pmpbox->ParentPropIndex();
				*piprop = ipropTmp;
				*ppboxFirstProp = Boxes()[ipropTmp];
				*pitssFirstProp = StringIndexes()[ipropTmp];
				*ptag = Tags()[ipropTmp];
				return;
			}
		}
		boxsetTargets.Insert(pbox);
	}

	// The first string index for the sub-notifier.
	int itssMinSub = pnote->StringIndexes()[iprop];

	VwBox ** ppbox = Boxes();  // gets incremented through the properties

	// counts the properties
	for (int i = 0; i < m_cprop; i++)
	{
		VwBox * pboxFirstInProp = *ppbox;
		ppbox++; // now ready for next prop, also to set up pboxLim

		// Find the box that is the lim of this prop, if any.
		// It is either the first box of the next prop with a non-null box, or the box after the
		// last box of the whole notifier.
		// Exception: it is possible that the box we obtain by the above is the same as
		// pboxFirstInProp, since two attributes can share the same string box. In that case, we
		// DO want to consider the current box, so we take the following box as the limit. May
		// also be null, if nothing follows this prop in the same container.
		VwBox * pboxLim = NULL;

		for (VwBox ** ppbox2 = ppbox; !pboxLim && ppbox2 < Boxes() + m_cprop; ppbox2++)
			pboxLim = *ppbox2;

		if (!pboxLim) // nothing after this prop has boxes at all
			pboxLim = m_pboxLast->NextOrLazy();

		if (pboxLim == pboxFirstInProp) // two attrs sharing string box
			pboxLim = pboxLim->NextOrLazy();

		for (VwBox * pbox = pboxFirstInProp; pbox && pbox != pboxLim; pbox=pbox->NextOrLazy())
		{
			if (boxsetTargets.IsMember(pbox))
			{
				// We got it! The target is in the current prop...unless it is a para box and
				// we are not up to the property with the right range of string indices.
				if (dynamic_cast<VwParagraphBox *>(pbox) // several props might be pointing at it
					&& i < m_cprop - 1		// it isn't the last prop
					&& pbox == *ppbox			// and the next prop uses the same string box
					&& itssMinSub >= StringIndexes()[i + 1]) // and the index we want is in or
														// after that attr
				{
					// Break out of the loop searching the boxes of this prop, we have
					// concluded that we want a later prop.
					break;
				}

				if (ppboxFirstProp)
					*ppboxFirstProp = pboxFirstInProp;
				if (pitssFirstProp)
					*pitssFirstProp = StringIndexes()[i];
				if (piprop)
					*piprop = i;

				*ptag = Tags()[i];
				// We have found a property of this which has as one of its boxes a container of
				// the first box of the target notifier. Is it possible that any other property
				// of this also does? We have already handled one such case, where multiple
				// properties point at the same (para) box. Apart from para boxes, it is not
				// possible for two properties of the same notifier to point at the same box,
				// because when we start a new property we start looking for a new first box,
				// and if we don't get one before closing the property, we record the prop as
				// having no box. It is also generally not possible that one property points at
				// a box, and a later property of the same notifier points at one of its
				//  descendents. The higher level box would have had to be opened after opening
				// the first property, and not closed before opening the second; but that would
				// be an error, because properties and flow objects are required to nest
				// cleanly. There is just one exception: while not inside a property at all we
				// can open a box to group several properties together, and not close it until
				// after adding all those properties. So if we have found a non-property which
				// contains our target box, there may be an embedded real property which also
				// contains it, and we would prefer to find that. On the other hand, it is also
				// just possible that the embedded object was displayed not as part of any
				// property, and the non-property we found here is what we really want. To
				// handle both cases, we make the prop we just found a tentative answer, to use
				// if we don't find a better one, but keep looking. Otherwise, we have a
				// definite answer and can stop.
				if (*ptag == ktagGapInAttrs)
				{
					fGotSomething = true; // Use this result unless we find a better
					break; // Done with this prop, try the next.
				}

				return;
			}
		}
	}

	// We should find something--at worst, it is part of a non-attribute only.
	Assert(fGotSomething);
	if (!fGotSomething)
		ThrowHr(WarnHr(E_UNEXPECTED));

	return;
}

/*----------------------------------------------------------------------------------------------
	Answer the index of one of your properties that contains pbox (or one of its ancestors).
----------------------------------------------------------------------------------------------*/
int VwNotifier::PropInfoFromBox(VwBox * pbox)
{
	// Enhance JohnT: this could usefully be a method of VwNotifer...
	VwBox ** ppboxes = Boxes();
	int cprops = CProps();
	// Loop over the argument box and all its ancestors.
	for (VwBox * pboxTarget = pbox;
		pboxTarget;
		pboxTarget = pboxTarget->Container())
	{
		// Loop over the properties.
		for (int iprop = 0; iprop < cprops; iprop++)
		{
			// This loop searches the box chain starting at *ppbox for pboxTarget.
			VwBox * pboxT;
			VwBox * pboxLim = GetLimOfProp(iprop);
			for (pboxT = ppboxes[iprop]; pboxT && pboxT != pboxTarget && pboxT != pboxLim;
					pboxT = pboxT->NextOrLazy())
				;
			if (pboxT == pboxTarget)
			{
				return iprop;
			}
		}
	}
	Assert(false); // Picture should be part of its notifier somewhere.
	return 0; // Have to return something.
}

/*----------------------------------------------------------------------------------------------
	Answer a count of the number of occurrences of the specified property tag in this, up to but
	not including the limit	box, if it is one of your start-of-attribute boxes, also up to but
	not including itssLim within pboxLim, if pboxLim is a paragraph box.
	(pass NULL, the default, to count all occurrences.	note that NULL will count all occurrences, even
	if one or more occurrences of this prop generated no boxes and so point to NULL)
----------------------------------------------------------------------------------------------*/
int VwNotifier::PropCount(int tag, VwBox * pboxLim, int itssLim)
{
	VwBox ** ppbox = Boxes();
	int * ptag = Tags();
	int cprop = 0;
	int cpropNotAnAttr = -1;

	for (int i = 0; i < m_cprop; ppbox++, i++, ptag++)
	{
		if (pboxLim && *ppbox == pboxLim)
		{
			if (itssLim < 0 || StringIndexes()[i] >= itssLim)
				break;
		}
		if (tag == ktagGapInAttrs)
		{
			// For the gap case, things are more complex. The limit...typically the
			// box containing the selection...might be following or inside the
			// one ppbox points to, and this would still be the place to stop...
			// unless we find a more specific place later. (for example, an early
			// property might point at a whole table, which contains a cell which
			// contains the limit box; a later one might point at the cell.)
			int itssLimProp;
			VwBox * pboxLimOfProp = GetLimOfProp(i, &itssLimProp);

			for (VwBox * pbox = *ppbox;
				pbox && pbox != pboxLimOfProp;
				pbox = pbox->NextOrLazy())
			{
				if (pbox == pboxLim)
				{
					if (itssLim < 0 || StringIndexes()[i] >= itssLim)
					{
						return cprop; // won't find better than this!
					}
					// Also possible we did something like AddString(), which had to make a
					// paragraph (StringIndexes()[i] == -1), and then added the string
					// (itssLim == 0 if the selection is in it, but there is no record of
					// this in the list of props, and itssLimProp is -1). In general, if
					// itssLimProp is -1, an itss anywhere in this paragraph is a hit.
					// Or, we might have done OpenPara() (makes property record with
					// StringIndexes()[i] == -1), two or more AddString()s (which don't make
					// records), then an AddStringProp (makes a record pointing at this box
					// with string index 2, say). A selection in either of the first two
					// strings, but not the real property one, should match. In this case,
					// only literals before itssLimProp (2) are part of this literal.
					if (StringIndexes()[i] < 0 && (itssLimProp == -1 || itssLim < itssLimProp))
						return cprop;
				}
				if (dynamic_cast<VwGroupBox *>(pbox)
					&& dynamic_cast<VwGroupBox *>(pbox)->Contains(pboxLim))
				{
					cpropNotAnAttr = cprop;
					break; // from inner loop, keep looking for better prop
				}
			}
		}

		if (*ptag == tag)
			cprop++;
	}
	if (cpropNotAnAttr != -1)
		return cpropNotAnAttr;

	return cprop;
}

/*----------------------------------------------------------------------------------------------
	Insert pboxFirstNew through pboxLastNew (which may be the same box) into your property
	iprop.

	if pboxLastOld1 is null, insert before pboxFirstOld1
	otherwise insert after pboxLastOld1 (and ignore pboxFirstOld1).

	If the itss values are not -1, they indicate a string index within the corresponding box.
	itssMin in pboxFirstOld,
	itssLim in pboxLastOld.
	All the strings in pboxFirstNew etc are to be included.

	At least for now, a property that begins inside a particular paragraph must end there.
	Thus, if itss values are >= 0, there should be only one replacement box, and only one
	old box, and we are doing a substring insert.
	Otherwise, we're linking in new boxes.

	Before redoing layout, fix notifiers and re-install vpaNoteNew in the root.
----------------------------------------------------------------------------------------------*/
void VwNotifier::InsertBoxes(int iprop, int itssMin,  int itssLim, VwBox * pboxFirstOld,
	VwBox * pboxLastOld, VwBox * pboxFirstNew, VwBox * pboxLastNew, NotifierVec & vpanoteNew,
	IVwGraphics * pvg)
{
	FixupMap fixmap; // must be declared before any gotos
	NotifierVec vpanoteOuterFirst; // likewise
	NotifierVec vpanoteOuterLast; // likewise
	VwGroupBox * pgboxContainer = pboxFirstOld->Container();
	VwRootBox * prootb = pgboxContainer->Root();
	NotifierVec vpanoteDel; // dummy, inserting will not cause deleting notifiers but ReplaceBoxes needs arg.
	if (itssMin == -1)
	{
		Assert(itssLim == -1);

		VwBox * pboxPrev;
		VwBox * pboxNext;

		if (pboxLastOld)
		{
			// insert after it
			// If any outer notifier has pboxLastOld as its last box, we must be inserting
			// at the end of the property, and should update them to point at the last new box.
			// Since *this will be in the list, this will extend the limit of this property,
			// if we are making a change at the end of it.
			// We must not, however, modify the notifier for the previous item, whose level
			// is one greater than ours and which certainly ends at pboxLastOld.
			pgboxContainer->GetNotifiers(pboxLastOld, vpanoteOuterLast);
			for (int i=0; i < vpanoteOuterLast.Size(); i++)
			{
				if (vpanoteOuterLast[i]->Level() <= this->Level())
					vpanoteOuterLast[i]->ReplaceBoxes(NULL, pboxLastOld, NULL, pboxLastNew, vpanoteDel);
			}
			pboxPrev = pboxLastOld;
			pboxNext = pboxPrev->NextOrLazy();
		}
		else
		{
			// insert before pboxFirstOld, at start of property.
			// Any outer notifiers that point at pboxFirstOld as the start of a property
			// need to be updated to the inserted pboxFirstNew. The tail end of this property
			// is being kept, so we don't need to do anything about last boxes.
			pgboxContainer->GetNotifiers(pboxFirstOld, vpanoteOuterFirst);
			for (int i=0; i < vpanoteOuterFirst.Size(); i++)
			{
				if (vpanoteOuterFirst[i]->Level() <= this->Level())
					vpanoteOuterFirst[i]->ReplaceBoxes(pboxFirstOld, NULL, pboxFirstNew, NULL, vpanoteDel);
			}
			pboxNext = pboxFirstOld;
			pboxPrev = pgboxContainer->BoxBefore(pboxFirstOld); // could be null if first box
		}
		// Relink the box chain.
		pgboxContainer->RelinkBoxes(pboxPrev,
			pboxPrev ? pboxPrev->NextOrLazy() : pgboxContainer->FirstBox(),
			pboxFirstNew, pboxLastNew);

		InsertIntoFixMap(fixmap, pboxNext, pboxPrev, pgboxContainer);

		// Install notifiers before doing layout.
		prootb->AddNotifiers(&vpanoteNew);

		prootb->RelayoutRoot(pvg, &fixmap);
	}
	else
	{
		// Doing string level inserts into pboxFirstOld
		Assert(!pboxLastOld || pboxLastOld == pboxFirstOld);
		VwParagraphBox * pvpboxFirstOld = dynamic_cast<VwParagraphBox *>(pboxFirstOld);
		Assert(pvpboxFirstOld);
		VwParagraphBox * pvpboxFirstNew = dynamic_cast<VwParagraphBox *>(pboxFirstNew);
		Assert(pvpboxFirstNew);

		int itssInsert;
		if (pboxLastOld)
		{
			// insert after itssLim
			itssInsert = itssLim;
		}
		else
		{
			// insert before itssMin
			itssInsert = itssMin;
		}
		pgboxContainer->GetNotifiers(pboxFirstOld, vpanoteOuterFirst);
		// Install and fix notifiers before doing layout (but after getting old linked notifiers).
		prootb->AddNotifiers(&vpanoteNew);

		// Fix any notifiers that point at the old box and have string indexes
		// after what we inserted. Also delete any that are for properties
		// embedded in the one we are regenerating.
		int i;
		int ditss = pvpboxFirstNew->Source()->CStrings();
		for (i = 0; i < vpanoteOuterFirst.Size(); i++)
		{
			vpanoteOuterFirst[i]->AdjustForStringRep(pvpboxFirstOld, itssInsert, itssInsert, ditss,
				Level() + 1);
		}

		// The new box will not be used, so we will get rid of it. Any new notifiers that were
		// pointing at it need to point to pboxFirstOld instead. Also fix string indexes
		// in the new notifiers to account for an extra itssInsert strings occurring before
		// them in the full paragraph.
		for (i = 0; i < vpanoteNew.Size(); i++)
		{
			vpanoteNew[i]->AdjustForStringRep(pvpboxFirstNew, 0, 0, itssInsert, INT_MAX);
			vpanoteNew[i]->ReplaceBoxes(pboxFirstNew, pboxFirstNew, pboxFirstOld, pboxFirstOld, vpanoteDel);
		}
		Assert(vpanoteDel.Size() == 0);

		// Since we aren't deleting anything we can ignore the returned list of sub-boxes to delete.
		pvpboxFirstOld->ReplaceStrings(pvg, itssInsert, itssInsert,
			pvpboxFirstNew);

		delete pboxFirstNew;
	}
}

/*----------------------------------------------------------------------------------------------
	Common code in InsertBoxes and ReplaceBoxes. Figures out which boxes may change location,
	given that pboxNext is the box after the change, pboxPrev is the one before (or null if
	nothing is before), and pgboxContainer is the one whose box sequence changed.
----------------------------------------------------------------------------------------------*/
void VwNotifier::InsertIntoFixMap(FixupMap & fixmap, VwBox * pboxNext, VwBox * pboxPrev,
	VwGroupBox * pgboxContainer)
{
	// Exactly what we need to do about preceding and following boxes depends on the exact
	// kind of container.
	pgboxContainer->AddPrevAndFollowingToFixMap(fixmap, pboxNext,pboxPrev);

	for (VwGroupBox * pgbox = pgboxContainer; pgbox; pgbox = pgbox->Container())
	{
		Rect vwrect = pgbox->GetInvalidateRect();
		VwBox * pboxKey = pgbox;
		fixmap.Insert(pboxKey, vwrect);
	}
}

/*----------------------------------------------------------------------------------------------
	Remove from vpaNote any notifiers which occur in vpaNote2
----------------------------------------------------------------------------------------------*/
void RemoveVec2FromVec1(NotifierVec & vpaNote, NotifierVec & vpaNote2)
{
	for (int i = vpaNote.Size(); --i >= 0;)
	{
		for (int j = vpaNote2.Size(); --j >= 0; )
		{
			if (vpaNote[i] == vpaNote2[j])
			{
				vpaNote.Delete(i);
				break;
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Replace the specified boxes (or parts of them), which are your property at iprop, with the
	given replacements.
	Delete the old boxes and any notifiers which apply only to them.
	Correct any notifiers which point at pboxFirstOld or pboxLastOld, if those get replaced.
	This includes notifiers in vpanoteNew, the ones produced in the course of the regenerate.
	If changing a string length, fix any affected notifiers. Ones in vpanoteNew are
	not affected.
	Update the display.

	Arguments:
		The property being repalced runs from pboxFirstOld1 to pboxLastOld1,
		except that if either of those is a para box, possibly only the strings
		from itssMin up to itssLim are to be replaced.
		Note that itssMin is relative to the strings assigned to pboxFirstOld1,
		itssLim is relative to the strings assigned to pboxLastOld1.
		The new boxes contain only strings from the specified property.

	Algorithm:
		Case 1: no substring replacement; itssMin and itssLim both -1. Replace old boxes
			and everything between with the new boxes.
		Case 2: pboxFirstOld1 and pboxLastOld1 are the same para box, and so are pboxFirstNew1
			and pboxLastNew1, and itssMin and itssLim both >= 0. Replace the indicated range
			of strings with the contents of the new box, and discard it.
			(Fix notifiers as necessary if different numbers of strings are in the replacement.)
		Case 3: (no longer handled) pboxFirstOld1 or pboxLastOld1 or both are para boxes with corresponding string
			limit >= 0, but not the same.
			pboxFirstNew1 and pboxLastNew1 are not the same, and are para boxes if the
			corresponding old box was a para box. Boundary para boxes handle replacing old
			strings with new, while non-para boxes (possibly including one of the boundary
			boxes) are replaced with any non-para boxes in the new sequence.
			Note: I don't think case 3 can actually happen yet.

		Other possibilities cause a higher-level regenerate and should not be passed to this
		method; calling OkayToReplace should prevent that happening.

	If fNeedLayout is true, the default, the system recomputes the layout of all affected
	boxes. If it is false, the caller is responsible for any necessary layout adjustment.
	Currently, fNeedLayout false is only supported if itssMin and itssLim are -1. That is,
	we're replacing whole boxes, not string sequences inside paragraphs.

	After retrieving old notifiers, but before doing relayout, reinstall new notifiers.
----------------------------------------------------------------------------------------------*/
void VwNotifier::ReplaceBoxSeq(int iprop, int itssMin,  int itssLim, VwBox * pboxFirstOld1,
	VwBox * pboxLastOld1, VwBox * pboxFirstNew1, VwBox * pboxLastNew1, NotifierVec & vpanoteNew,
	IVwGraphics * pvg, bool fNeedLayout)
{
	Assert((itssMin == -1 && itssLim == -1) || fNeedLayout);
	FixupMap fixmap; // must be declared before any gotos
	NotifierVec vpanoteOuterFirst; // likewise
	NotifierVec vpanoteOuterLast; // likewise
	VwGroupBox * pgboxContainer = pboxFirstOld1->Container();
	VwRootBox * prootb = pgboxContainer->Root();
	// Accumulate a list of notifiers we need to delete.
	NotifierVec vpanoteDel;

	VwBox* pboxFirstOld = pboxFirstOld1;
	VwBox* pboxLastOld   = pboxLastOld1;
	VwBox* pboxFirstNew = pboxFirstNew1;
	VwBox* pboxLastNew   = pboxLastNew1;

	VwParagraphBox * pvpboxFirstOld = dynamic_cast<VwParagraphBox *>(pboxFirstOld);
	VwParagraphBox * pvpboxFirstNew = dynamic_cast<VwParagraphBox *>(pboxFirstNew);
	VwParagraphBox * pvpboxLastOld = dynamic_cast<VwParagraphBox *>(pboxLastOld);
	VwParagraphBox * pvpboxLastNew = dynamic_cast<VwParagraphBox *>(pboxLastNew);

	if (itssMin < 0)
	{
		// we're not doing a substring replacement, even if the boxes happened to be paragaph ones;
		// pretend they are not.
		pvpboxFirstOld = pvpboxFirstNew = NULL;
	}
	else
	{
		// If doing substring replacement we must have paragraphs!
		Assert(pvpboxFirstOld);
		Assert(pvpboxFirstNew);
	}
	if (itssLim < 0)
	{
		pvpboxLastOld = pvpboxLastNew = NULL;
	}
	else
	{
		// If doing substring replacement we must have paragraphs!
		Assert(pvpboxLastOld);
		Assert(pvpboxLastNew);
	}

	BoxSet boxsetDeleted;

	if (pboxFirstNew)
	{
		if (pvpboxFirstOld && pboxLastOld == pboxFirstOld
				&& pvpboxFirstNew && pboxLastNew == pboxFirstNew)
		{
			//	Case 2: Replacement involves a single para; let the para box handle it.
			pgboxContainer->GetNotifiers(pboxFirstOld, vpanoteOuterFirst);
			// Install and fix notifiers before doing layout (but after getting old linked notifiers).
			prootb->AddNotifiers(&vpanoteNew);
			// Fix any notifiers that point at the old box and have string indexes
			// after what we replaced. Also delete any that are for properties
			// embedded in the one we are regenerating.
			int i;
			int ditss = pvpboxFirstNew->Source()->CStrings() - (itssLim - itssMin);
			for (i = 0; i < vpanoteOuterFirst.Size(); i++)
			{
				vpanoteOuterFirst[i]->AdjustForStringRep(pvpboxFirstOld, itssMin, itssLim, ditss,
					Level() + 1);
			}

			// The new box will not be used, so get rid of it. Any new notifiers that were
			// pointing at it need to point to pboxFirstOld instead. Also fix string indexes
			// in the new notifiers to account for an extra itssMin strings occurring before
			// them in the full paragraph.
			for (i = 0; i < vpanoteNew.Size(); i++)
			{
				vpanoteNew[i]->AdjustForStringRep(pvpboxFirstNew, 0, 0, itssMin, INT_MAX);
				vpanoteNew[i]->ReplaceBoxes(pboxFirstNew, pboxFirstNew, pboxFirstOld, pboxFirstOld, vpanoteDel);
			}
			pvpboxFirstOld->ReplaceStrings(pvg, itssMin, itssLim,
				pvpboxFirstNew, this, &vpanoteDel, &boxsetDeleted);

			delete pboxFirstNew;
			prootb->DeleteNotifierVec(vpanoteDel);
			return;
		}
	}

	// ENHANCE JohnT: Note: some of the remaining cases have not been fully tested,
	// especially anything that involves replacing a range of strings that spans
	// a box boundary.

	// In all other cases, the container should not be a paragraph
	Assert(!dynamic_cast<VwParagraphBox *>(pgboxContainer));

	// Now pboxFirstOld and
	// pboxLastOld indicate the sequence of boxes to replace completely with the sequence from
	// pboxFirstNew through pboxLastNew.
	// We should not be doing this sort of replace inside a paragraph; that should have been
	// handled above.

	VwBox * pboxPrev;
	pboxPrev = pgboxContainer->BoxBefore(pboxFirstOld);

	VwBox * pboxNext;
	pboxNext = pboxLastOld->NextOrLazy(); // place to relink

	// It is possible that we replaced part of the first and last boxes, and there is nothing
	// in between. In this case, pboxFirstOld has been updated to the following box,
	// and pboxLastOld has been updated to the preceding one, and they are no longer in order.
	// We must detect that or we will be messed up by finding them out of order.
	// Old boxes consisted of just two partly-replaced para boxes with nothing in between.
	// If this happens, then we wind up with pboxLastOld being exactly the box before
	// pboxFirstOld.
	// No old boxes are going away.
	bool fNoOld = pboxLastOld->NextOrLazy() == pboxFirstOld;

	// Invalidate the old boxes (to force redraw even if the new boxes don't cover the same
	// exact part of the screen).
	VwBox * pbox;
	if (!fNoOld)
	{
		if (fNeedLayout)
		{
			for (pbox = pboxFirstOld; ; pbox = pbox->NextOrLazy())
			{
				Assert(pbox);
				pbox->Invalidate();
				if (pbox == pboxLastOld)
					break;
			}
		}
		// Before we start fixing notifiers, get rid altogether of any embedded ones
		// for the first box we are going to delete. Otherwise they would get "fixed"
		// to point at the new box, and would never get deleted.
		pboxFirstOld->Root()->DeleteNotifiersFor(pboxFirstOld, Level() + 1, vpanoteDel);
	}



	// Fix containing notifiers. Get BOTH lists before we make ANY changes, because
	// GetNotifiers depends on the list structures in all sorts of subtle ways.
	if (pboxFirstOld == pboxFirstOld1)
		pgboxContainer->GetNotifiers(pboxFirstOld, vpanoteOuterFirst);
	if (pboxLastOld == pboxLastOld1 && pboxLastOld != pboxFirstOld)
		pgboxContainer->GetNotifiers(pboxLastOld, vpanoteOuterLast);

	// Must NOT fix any of the notifiers we're going to delete, because some of them
	// could get spuriously turned into missing notifiers.
	RemoveVec2FromVec1(vpanoteOuterFirst, vpanoteDel);
	RemoveVec2FromVec1(vpanoteOuterLast, vpanoteDel);

	// Box to replace pboxFirstOld in any notifier which regarded it as the start of a prop.
	VwBox * pboxFirstRep = pboxFirstNew;
	// Box to replace pboxLastOld in any notifier which regarded it as the end of a prop.
	VwBox * pboxLastRep = pboxLastNew;

	if (pboxFirstOld == pboxFirstOld1)
	{
		// We may have actually replaced the first box used to display the property. Any surviving
		// notifier which makes use of that box should be changed to point to the replacement, if any.
		// Note that we don't have any string indexes to fix related to pboxFirstOld, unless it
		// is also pboxLastOld, because we replaced all of the last property it held. If it is
		// pboxLastOld, the following code will fix things.
		for (int i=0; i < vpanoteOuterFirst.Size(); i++)
		{
			vpanoteOuterFirst[i]->ReplaceBoxes(pboxFirstOld, pboxLastOld, pboxFirstRep,
				pboxLastRep, vpanoteDel);
		}
	}

	// A lot of these may be the same notifiers, but since the boxes have already been
	// replaced, calling again will do no harm...even for notifiers that have been replaced
	// with missing ones (they won't actually be deleted until our reference count is released).
	if (pboxLastOld == pboxLastOld1 && pboxLastOld != pboxFirstOld)
	{
		for (int i=0; i < vpanoteOuterLast.Size(); i++)
		{
			vpanoteOuterLast[i]->ReplaceBoxes(pboxFirstOld, pboxLastOld, pboxFirstRep,
				pboxLastRep, vpanoteDel);
		}
	}

	// Relink the box chain.
	pgboxContainer->RelinkBoxes(pboxPrev, pboxNext, pboxFirstNew, pboxLastNew);

	// Delete the old boxes and any notifiers that apply only to them.
	if (!fNoOld)
		DeleteBoxes(pboxFirstOld, pboxLastOld, Level()+1, pboxFirstNew, vpanoteDel, &boxsetDeleted);

	// Invalidate any containing or subsequent boxes that are affected.
	if (fNeedLayout)
		InsertIntoFixMap(fixmap, pboxNext, pboxPrev, pgboxContainer);

	prootb->DeleteNotifierVec(vpanoteDel);
	// Install and fix notifiers before doing layout (but after getting old linked notifiers).
	prootb->AddNotifiers(&vpanoteNew);
	if (fNeedLayout)
		prootb->RelayoutRoot(pvg, &fixmap, -1, &boxsetDeleted);
}

/*----------------------------------------------------------------------------------------------
	Strings from itssMin to itssLim in pvpbox are being replaced with new strings. This is a
	notifier which to some extent covers pvpbox. It might be entirely embedded in pvpbox, or
	it may be that just some of its properties relate to pvpbox. It might even be that it is
	embedded within the property that is being regenerated, in which case it needs to be
	deleted! It's also possible we're doing a pure insert, with itssMin == itssLim.
	Specifically,
		1. If this notifier begins at one of the strings in [itssMin+1, itssLim) of pvpbox,
		it is embedded in the property being replaced; check that its level is at least levMin,
		and delete it.
		2. If it begins at itssMin of pvpbox and itssLim > itssMin and its level is >= levMin,
		delete it.
		3. Assuming it will survive, if any of its properties relates to pvpbox with itss >=
		itssLim, add ditss to the itss of that property.
		4. Assert that no property relates to pvpbox with itss in [itssMin+1, itssLim). This
		would correspond to an outer notifier with a property starting in the middle of some
		lower level notifier's property.
----------------------------------------------------------------------------------------------*/
void VwAbstractNotifier::AdjustForStringRep(VwParagraphBox * pvpbox, int itssMin, int itssLim,
	int ditss, int levMin)
{
}
void VwNotifier::AdjustForStringRep(VwParagraphBox * pvpbox, int itssMin, int itssLim,
	int ditss, int levMin)
{
	if (m_pboxLast == pvpbox)
	{
		if (m_itssLimSubString >= itssLim)
		{
			if (m_itssLimSubString > itssLim || Level() < levMin)
			{
				// If this notifier ends strictly after the end of the change, then its end
				// has certainly been affected (first case of the if).
				// Otherwise, it ends exactly AT the limit of what we're replacing.
				// If it's an outer notifier, then what we're replacing is inside it,
				// and its end is affected.
				// Otherwise, either it's embedded in what's being deleted, and will
				// be deleted itself; or we're doing a pure insert, and it is right
				// BEFORE the thing we're inserting, and we do NOT want to modify it.
				// So if the two are equal, whether to adjust depends on whether it's a
				// notifier with a greater-or-equal level (embedded in the one whose
				// property is changing) or a lesser level (the one that's changing or
				// one of its parents).
				m_itssLimSubString += ditss;
			}
		}
		else
			// If it is a higher level notifier, it should end before the start
			// of the property being modified.
			Assert(Level() >= levMin  || m_itssLimSubString <= itssMin);
	}
	VwBox ** ppvpbox = Boxes();
	int * pitss = StringIndexes();
	VwBox ** ppvpboxLim = ppvpbox + m_cprop;
	for (; ppvpbox < ppvpboxLim; ppvpbox++, pitss++)
	{
		if (*ppvpbox != pvpbox)
			continue;
		int itss = *pitss;
		if (itss < itssMin)
			continue;
		// Note that this next case produces the correct behavior (and suppresses
		// undesired special cases below) for the property at itssMin/Lim in the case of
		// pure insertion.
		if (itss >= itssLim)
		{
			*pitss = itss + ditss;
			continue;
		}
		if (itss > itssMin)
		{
			Assert(Level() >= levMin);
			pvpbox->Root()->DeleteNotifier(this);
			return; // no point in more fixing, and dangerous since *this may be deleted.
		}
		Assert(itss == itssMin); // we covered all other cases..
		if (Level() >= levMin)
		{
			pvpbox->Root()->DeleteNotifier(this);
			return; // no point in more fixing, and dangerous since this may be deleted.
		}
	}
}

/*----------------------------------------------------------------------------------------------
	This is a method object, the implemtation of the EditableSubstringAt method.
----------------------------------------------------------------------------------------------*/
class EditableSubstringAtMethod
{
public:
	VwNotifier * pnote;
	VwParagraphBox *pvpbox;
	int ichMin;
	int ichLim;
	bool fAssocBefore;
	HVO * phvo;
	int * ptag;
	int * pichMin;
	int *pichLim;
	IVwViewConstructor ** ppvvc;
	int * pfrag;
	int *piprop;
	VwNoteProps * pvnp;
	int * pitssProp;
	ITsString ** pptssProp;
	bool fFoundBox;
	VwEditPropVal vepvBest;

	EditableSubstringAtMethod(VwNotifier * pnote1, VwParagraphBox *pvpbox1,
		int ichMin1, int ichLim1, bool fAssocBefore1,
		HVO * phvo1, int * ptag1, int * pichMin1, int *pichLim1, IVwViewConstructor ** ppvvc1,
		int * pfrag1, int *piprop1, VwNoteProps * pvnp1, int * pitssProp1, ITsString ** pptssProp1
		)
	{
		pnote = pnote1;
		pvpbox = pvpbox1;
		ichMin = ichMin1;
		ichLim = ichLim1;
		fAssocBefore = fAssocBefore1;
		phvo = phvo1;
		ptag = ptag1;
		pichMin = pichMin1;
		pichLim = pichLim1;
		ppvvc = ppvvc1;
		pfrag = pfrag1;
		piprop = piprop1;
		pvnp = pvnp1;
		pitssProp = pitssProp1;
		pptssProp = pptssProp1;
		fFoundBox = false;
		vepvBest = kvepvNone;
	}

	VwEditPropVal Run()
	{
		for (int iprop = 0; iprop < pnote->CProps(); iprop++)
		{
			if (pnote->Boxes()[iprop] != pvpbox)
			{
				// If we've seen the box we're looking for and have now found a different one,
				// it's no use looking further.
				if (fFoundBox)
					break;
			} else // It IS the box we're looking for.
			{
				VwTxtSrc * pts = pvpbox->Source();

				int * pitss = pnote->StringIndexes() + iprop;
				int itss = *pitss;

				// We may find that this paragraph occurs as a box in a pile, as well as finding
				// the entries we want for the strings embedded in the paragraph.
				// For example, OpenObject..OpenParagraph..OpenProp..
				// produces an initial entry pointing at the paragraph and indicating that
				// the paragraph as a whole is the start of the display of the object.
				// Since we are searching for strings, we aren't interested in such entries.
				// On the other hand, OpenObject..OpenParagraph..AddString produces a literal,
				// and we DO want to find the ktagGapInAttrs that points at the paragraph
				// like that for that string.
				if (itss < 0)
				{
					CheckForAltMatch(iprop);
					continue;
				}

				fFoundBox = true;

				// Figure how many characters between start of para and start of property iprop
				int ichMinProp = pts->IchStartString(itss);

				// if the range we want starts before this property, it is no good, and furthermore,
				// any subsequent property is going to have a still larger ichMinProp and hence
				// fail. So drop out of the loop.
				if (ichMinProp > ichMin)
				{
					// There's an exception, though: a label (string that isn't part of the
					// property) at the start of the paragraph, from a call sequence like
					// OpenObject...OpenParagraph...AddString...OpenProp...
					// In this case the whole paragraph (with itss of = -1) is the first property,
					// and there is no separate entry for the label, so the first thing we find is
					// some string that is beyond the label. Only consider this as an answer if
					// we haven't already found a more workable one.
					if (vepvBest == kvepvNone && iprop > 0 && pnote->Boxes()[iprop - 1] == pvpbox
						&& pnote->StringIndexes()[iprop - 1] == -1 && pnote->Tags()[iprop - 1] == ktagGapInAttrs)
					{
						vepvBest = kvepvReadOnly;
						*ptag = ktagGapInAttrs;
						pichMin = 0;	// dummy prop starts at beginning of paragraph
						*pichLim = ichMinProp; // start of the next one
						*ppvvc = NULL;
						*pfrag = pnote->Fragments()[iprop - 1];
						// Strip the editable bit. This dummy prop is certainly not editable.
						*pvnp = (VwNoteProps)((pnote->Flags()[iprop - 1]) & ~kvnpEditable);
						*piprop = iprop - 1;
						VwPropertyStorePtr qzvps;
						// ichMinProp and ichLimProp are dummies here, we return a string that
						// contains ichMin.
						int ichMinDummy, ichLimDummy;
						pts->StringFromIch(ichMin, false, pptssProp, &ichMinDummy, &ichLimDummy, &qzvps,
							pitssProp);
					}
					break;
				}

				// Now figure the limit of the current property character position.
				int itssLim;
				VwBox * pboxLim = pnote->GetLimOfProp(iprop, &itssLim);
				if (pboxLim != pvpbox)
				{
					// REVIEW JohnT: Why does putting in this code cause multiple tests to
					// fail (MoreRootSiteTests.AdjustScrollRangeTestXX, and many others) (see
					// also TE-4169)
					// We can get here if we are looking for an editable substring while laying
					// out the paragraph box.
					//if (!pvpbox->FirstBox())
					//	continue;
					// We found a limit somwhere inside some embedded box.
					// The resulting itssLim is relative to some string in that box.
					// We want one relative to our own paragraph.
					VwBox * pboxChild; // The child of our own box which contains pboxLim.
					pvpbox->Contains(pboxLim, &pboxChild);
					// Count how many previous boxes are not from TsStrings.
					int cChildBoxesPrev = 0;
					for (VwBox * pboxT = pvpbox->FirstBox(); pboxT && pboxT != pboxChild; pboxT = pboxT->NextOrLazy())
					{
						if (!pboxT->IsBoxFromTsString())
							cChildBoxesPrev++;
					}
					// Now find the index of the corresponding TsString.
					for (int i = 0; i < pvpbox->Source()->CStrings(); i++)
					{
						ITsStringPtr qtssT;
						pvpbox->Source()->StringAtIndex(i, &qtssT);
						if (!qtssT)
						{
							if (cChildBoxesPrev == 0)
							{
								itssLim = i + 1;
								break;
							}
							cChildBoxesPrev--;
						}
					}
				}

				int ichLimProp = pts->IchStartString(itssLim);

				// If the end of the range we want is after the end of prop iprop, this property is no good,
				// but we can still try the next one, which may have a larger ichLimProp.
				if (ichLim > ichLimProp)
					continue;

				// OK, prop iprop is a possibility. Is it editable in the sense required?
				IVwViewConstructor * pvvc =  pnote->Constructors()[iprop];

				// OK, we have a property that may be some use. See if it is editable.

				// We can edit if (1) the view constructor said so, and (2) if it is a
				// "normal" property (i.e., no explicit view constructor) or it is a special
				// property that requires a view constructor either for displaying, updating or
				// both. Complex properties such as objects and sequences are never editable.
				// kvnpNone (used currently for picture boxes) also lacks a VC but does not
				// allow text editing!
				VwNoteProps vnp = pnote->Flags()[iprop];
				bool fEditable = (vnp & kvnpEditable) &&
					(vnp & ~kvnpEditable) != kvnpNone &&
					(!pvvc ||
					((vnp & ~kvnpEditable) == kvnpProp ||
					((vnp & ~kvnpEditable) == kvnpStringProp) ||
					((vnp & ~kvnpEditable) == kvnpStringAltMember) ||
					((vnp & ~kvnpEditable) == kvnpUnicodeProp)));
				// REVIEW JohnT(TomB):May need to extend this to account for additional VwNoteProps.
				// ENHANCE JohnT(TomB):May need to extend this to account for additional VwNoteProps.

				// If it is not a real property of the object, we also can't edit with it.
				int tag = pnote->Tags()[iprop];
				if ((tag == ktagNotAnAttr || tag == ktagGapInAttrs))
					fEditable = false;

				// If we already have an editable property and this is not editable, ignore it.
				if (vepvBest == kvepvEditable && !fEditable)
					continue;

				// If we already have a non-editable string property, and this one is also non-editable,
				// and we want the first, ignore the new one.
				if (vepvBest == kvepvReadOnly && fAssocBefore && !fEditable)
					continue;

				// OK, we got one that is better than anything we had.
				if (vepvBest == kvepvNone)
				{
					// The first property that is any good at all.
					// This only needs to be done once, it is the same for every prop.
					*phvo = pnote->Object();
				}
				else
				{
					// If we already had one, we need to remove the addref on the view
					// constructor and string we previously put in the result slots.
					ReleaseObj(*ppvvc);
					ReleaseObj(*pptssProp);
				}
				vepvBest = fEditable ? kvepvEditable : kvepvReadOnly;

				*ptag = tag;
				*pichMin = ichMinProp;
				*pichLim = ichLimProp;
				*ppvvc = pvvc;
				AddRefObj(pvvc);
				*pfrag = pnote->Fragments()[iprop];
				// strip the editable bit. We already used it to determine the value of vepv
				*pvnp = (VwNoteProps)((pnote->Flags()[iprop]) & ~kvnpEditable);
				*piprop = iprop;
				if (itss == itssLim - 1 || ichMin == ichMinProp)
				{
					// Only one string in the property, assume it's the one; or, ichMin is at
					// the very start of the property, answer the first string.
					*pitssProp = itss;
					pts->StringAtIndex(itss, pptssProp);
				}
				else if (ichMin == ichLimProp)
				{
					// Be sure to answer the last string in the property
					*pitssProp = itssLim - 1;
					pts->StringAtIndex(itssLim - 1, pptssProp);
				}
				else
				{
					VwPropertyStorePtr qzvps;
					// ichMinProp and ichLimProp are dummies here, we return a string that actually
					// contains ichMin.
					pts->StringFromIch(ichMin, false, pptssProp, &ichMinProp, &ichLimProp, &qzvps, pitssProp);
					// It ought to be a string within the property.
					Assert(itss <= *pitssProp && *pitssProp < itssLim);
				}

				// If we are looking for the first eligible property and have an editable one,
				// stop at once.
				if (fAssocBefore && vepvBest == kvepvEditable)
					break;

				// Otherwise, keep going and see if we find more
			} // box is target box
			if (!fFoundBox)
				CheckForAltMatch(iprop);
		}
		return vepvBest;
	}

	// pbox is part of the chain of boxes for property iprop.
	// We want to know whether pvpbox, the target, is contained in pbox,
	// and if so, whether it is part of property iprop.
	bool BoxContainsTarget(VwBox * pbox, int iprop)
	{
		// If it's not a group box, it can't contain anything.
		VwGroupBox * pgbox = dynamic_cast<VwGroupBox *>(pbox);
		if (!pgbox)
			return false;
		// If it doesn't contain the target it's not interesting.
		if (!pgbox->Contains(pvpbox))
			return false;

		// If it's not a paragraph box, and is in the chain for the property,
		// the target being part of it is enough to show the target is part
		// of the property.
		VwParagraphBox * pvpboxCont = dynamic_cast<VwParagraphBox *>(pbox);
		if (!pvpboxCont)
			return true;
		// Likewise if the property is not just part of a paragraph.
		int itss = pnote->StringIndexes()[iprop];
		if (itss < 0)
			return true;
		// If it's not the top-level box for the property, then it's nested inside
		// that box, so we succeed, on the same basis as a non-paragraph box.
		// This is important to check, because if this condition fails, itss is
		// inapplicable to pvpboxCont, and using it may Assert.
		if (pvpboxCont != pnote->Boxes()[iprop])
			return true;

		// But, if the property is part of paragraph pvpboxCont, we need to
		// test whether pvpbox is part of the particular inner pile that is
		// 'string' itss.
		// First, check that there really is some embedded box at this index.
		// If not (maybe we're in a moveable pile?) we're not handling this case yet.
		VwTxtSrc * pts = pvpboxCont->Source();
		ITsStringPtr qtss;
		pts->StringAtIndex(itss, &qtss);
		if (qtss)
			return false;
		// First, count how many previous non-string items the paragraph has
		int cNonStrings = 0;
		for (int itssT = 0; itssT < itss; itssT++)
		{
			pts->StringAtIndex(itss, &qtss);
			if (!qtss)
				cNonStrings++;
		}
		// Now look for embedded boxes that are not generated from strings.
		// Find the one that corresponds to property itss.
		for (VwBox * pboxT = pvpboxCont->FirstBox(); pboxT; pboxT=pboxT->NextOrLazy())
		{
			if (pboxT->IsBoxFromTsString())
				continue;
			if (cNonStrings)
			{
				cNonStrings--;
				continue;
			}
			// OK, it's the box we want.
			VwGroupBox * pgbox2 = dynamic_cast<VwGroupBox *>(pboxT);
			if (!pgbox2)
				return false;
			// To be in the property, the original paragraph must be contained in this
			// child of the paragraph. (Paragraphs are never directly embedded in paragraphs,
			// so it isn't possible that pboxT == pvpbox.)
			return pgbox2->Contains(pvpbox);
		}
		return false; // Should never happen...some sort of default.
	}

	// This method checks property iprop to see if it contains the specified box
	// with itss = -1. This can produce the 'special case' indicated in the
	// header comment.
	void CheckForAltMatch(int iprop)
	{
		if (vepvBest > kvepvNonStringProp)
			return; // Not interested if we've already found a better sort of match.
		// Consider a sequence like OpenParagraph...OpenObject...OpenInnerPile...AddString
		// (or AddPicture). The inner pile is the box of the ktagGapInAttrs, yet (because it
		// is also part of a paragraph) it doesn't have itss == -1. So we need to check
		// for a box containing our paragraph even if itss >= 0.
		//if (pnote->StringIndexes()[iprop] >= 0)
		//	return; // this property is a string-within-box one, this case does not apply.
		int itss = pnote->StringIndexes()[iprop];
		if (pnote->Tags()[iprop] != ktagGapInAttrs)
			return; // not a gap, some lower-level notifier will cover it if not a string prop.

		int itssLimProp;
		VwBox * pboxLim = pnote->GetLimOfProp(iprop, &itssLimProp);

		for (VwBox * pbox = pnote->Boxes()[iprop];
			pbox && (pbox != pboxLim || (pbox == pboxLim && itss >= 0 && itss < itssLimProp));
			pbox = pbox->NextOrLazy())
		{
			// If pbox IS the para we want, and itss >= 0, then this property covers only part
			// of our paragraph, and this isn't a match. If itss < 0, then the property covers
			// the whole paragraph, or at least the strings at the start of it,
			// and is the sort of match we're looking for here.
			// If pbox CONTAINS the para we want, then we have this sort of match, even if
			// pbox is part of some higher-level paragraph (in interlinear text) and itss >= 0.
			if ((pbox == pvpbox && itss < 0)
				|| BoxContainsTarget(pbox, iprop))
			{
				// Note: it is tempting to set fFoundBox true here. Don't do it!
				// See comments where this method is called first.
				// fFoundBox = true;

				// If it is found in two places, the second is sure to be best.
				// The only way this happens is when the first thing in an object is
				// a group box that contains everything else; so the later container
				// will be more specific.
				if (vepvBest == kvepvNone)
				{
					// The first property that is any good at all.
					// This only needs to be done once, it is the same for every prop.
					*phvo = pnote->Object();
				}
				else
				{
					// If we already had one, we need to remove the addref on the view
					// constructor and string we previously put in the result slots.
					ReleaseObj(*ppvvc);
					ReleaseObj(*pptssProp);
				}
				vepvBest = kvepvNonStringProp;
				*ptag = pnote->Tags()[iprop];
				VwPropertyStorePtr qzvps;
				pvpbox->Source()->StringFromIch(ichMin, false, pptssProp, pichMin, pichLim, &qzvps, pitssProp);
				*ppvvc = pnote->Constructors()[iprop];
				AddRefObj(*ppvvc);
				*pfrag = pnote->Fragments()[iprop];
				// strip the editable bit. We already used it to determine the value of vepv
				*pvnp = (VwNoteProps)((pnote->Flags()[iprop]) & ~kvnpEditable);
				*piprop = iprop;
			}
		}
	}

};

/*----------------------------------------------------------------------------------------------
	Try to find one of your properties that (1) points at pvpbox; (2) covers the positions
	ichMin to ichLim, or a character adjacent to that position if they are the same;
	and (3) is editable, in the sense that it is a direct basic property, or else the output
	of a DisplayVariant. Return the information required for updating the data server when the
	user edits the string: an object and tag, and possibly (if from DisplayVariant) a view
	constructor and fragment. Also indicate which of your properties it is.
	Return an indication of whether any property covers the range, and if so, whether editing
	is allowed.
	In the event that more than one property covers an insertion point (because one or more
	strings are empty), answer the first editable one if fAssocBefore is true, otherwise the
	last editable one--or if none is editable, the first if fAssocBefore is true, otherwise
	the last.
	If a range is passed and it is not entirely within one property, answer kvepvNone.
	Note that ich, *pichMin, and *pichLim are relative to the start of the para box.
	Caller gains a ref count on tssProp and pvvc.
	Return an indication of whether we got a property, and if so, whether editing it is valid.
	Note that the editable bit is stripped out of VwNoteProps, as it has already been used.
	char indexes in this routine and its interface are in logical chars.

	A special case occurs if the range passed is within some literal string. Consider a
	sequence like OpenObject...AddString...AddString...
	Each AddString creates a paragraph within the ktagGapInAttrs property; only the first
	of these paragraphs occurs in the notifier at all, and both of them have itss = -1;
	yet we'd like to be able to find them, though they are not editable, so we can identify
	that notifier as the most local one that refers to the label and retrieve info about
	containing notifiers. In such a case, we return this notifier's hvo, ktagGapInAttrs,
	pichMin and lim give the range within the paragraph of the TsString that contains
	the range, vc, frag, iprop, vnp as usual, itss is the index of the string that has
	the range, and tss is the string itself.
	(In this case, since editing is certainly not possible and it isn't important that the
	range is within a single string, we base pichMin, pichLim, pitssProp, and pptssProp
	on ichMin, and don't check whether ichLim is also in that range.)
----------------------------------------------------------------------------------------------*/
VwEditPropVal VwNotifier::EditableSubstringAt(VwParagraphBox *pvpbox, int ichMin, int ichLim,
	bool fAssocBefore,
	HVO * phvo, int * ptag, int * pichMin, int *pichLim, IVwViewConstructor ** ppvvc,
	int * pfrag, int *piprop, VwNoteProps * pvnp, int * pitssProp, ITsString ** pptssProp)
{
	EditableSubstringAtMethod esam(this, pvpbox, ichMin, ichLim, fAssocBefore,
		phvo, ptag, pichMin, pichLim, ppvvc,
		pfrag, piprop, pvnp, pitssProp, pptssProp);
	return esam.Run();
}


/*----------------------------------------------------------------------------------------------
	Delete the boxes between the given arguments (inclusive).
	Also delete associated notifiers:
		- for all embedded boxes and all but the first of this list, just delete any notifiers
		whose firstCoveringBox is that box
		- for the first box, delete any notifier whose level is deeper than this.
		(We can keep this...)
		- delete the old boxes (and embedded ones)

	Also inform the root box of the box deletions so it can check whether any selections are
	affected.

	To allow recursive use, we pass the level up to which to delete	for the first box as an
	argument; the default is -1, indicating this->m_level+1	replacement is the box, if any, that
	replaces the first box deleted. This is useful in case the selection is in a text box being
	replaced.
	Build a set of the deleted boxes. (Note that, as they have actually been deleted, only
	the pointers themselves should be used.)
----------------------------------------------------------------------------------------------*/
void VwNotifier::DeleteBoxes(VwBox * pboxFirst, VwBox * pboxLast, int chvoStopLevel1,
	VwBox * pboxReplaceFirst, NotifierVec & vpanoteDel, BoxSet * pboxsetDeleted)
{
	VwRootBox * prootb = pboxFirst->Root();
	VwBox * pbox;

	//	Real stop level is the one passed unless -1, in which case,
	//	use one more than my level.
	int chvoStopLevel = (chvoStopLevel1 == -1) ? Level()+1 : chvoStopLevel1;
	VwBox * pboxNext;

	for (pbox = pboxFirst; pbox; pbox = pboxNext)
	{
		pbox->DeleteContents(prootb, vpanoteDel, pboxsetDeleted);
		// Delete all notifiers unless the first box, in which case, delete those at levels
		// higher than chvoStopLevel.
		prootb->DeleteNotifiersFor(pbox, (pbox == pboxFirst ? chvoStopLevel : -1), vpanoteDel);
		prootb->FixSelections(pbox, (pbox == pboxFirst ? pboxReplaceFirst : NULL));

		//	Get this before deleting!
		pboxNext = pbox->NextOrLazy();
		BOOL fSame = (pbox == pboxLast);
		pboxsetDeleted->Insert(pbox);
		delete pbox;
		if (fSame)
			break;
	}
}


/*----------------------------------------------------------------------------------------------
	Determine if the replacement of the stuff in the prop-box record with the given newBox can
	be done in a reasonably straightforward way. If not, caller will regenerate next higher
	level of object. Specifically, it will be possible to do the replacement if we have
	corresponding para-boxes (or lack thereof) at the start- and end-points, and the change
	does not result in making a para box contain zero strings.
----------------------------------------------------------------------------------------------*/
bool VwNotifier::OkayToReplace(PropBoxRec& pbrec, VwBox* pboxNewFirst)
{
	// If the prop is going to disappear altogether, there could well be special cases
	// involved. Do a higher level regenerate.
	if (pboxNewFirst == 0)
		return false;

	VwBox * pboxOldFirst = pbrec.pboxFirst;
	VwBox * pboxOldLast = pbrec.pboxLast;
	VwBox * pboxNewLast = pboxNewFirst->EndOfChain();


	// If we're dealing entirely with whole boxes all is well.
	if (pbrec.itssMin < 0 && pbrec.itssLim < 0)
		return true;

	// If we're dealing with any strings-within-paragraphs, must be dealing with strings
	// at both ends and in just one paragraph.
	return pbrec.itssMin >= 0 && pbrec.itssLim >= 0
		&& dynamic_cast<VwParagraphBox *>(pboxNewFirst) &&
		pboxOldFirst == pboxOldLast && pboxNewFirst == pboxNewLast;
}


/*----------------------------------------------------------------------------------------------
	pboxOldFirst..pboxOldLast are (possibly part of) a property, and are being replaced by
	pboxNewFirst..pboxNewLast. If either or both of them are important to this notifier,
	make appropriate updates. Note that it is possible that the two old pointers are the same--
	for example, when a property that used to contain one object (and hence paragraph) now
	contains two (or none).
	In general, pboxOldLast should not be important unless it is your last box, in which
	case, replace that with pboxNewLast.
	If pboxOldFirst is the start of any of your	properties, replace with pboxNewFirst
	Also if it is your key box, fix things.

	It is also possible this is a new notifier pointing at a new paragraph box which is being
	discarded, after inserting its strings into another paragraph box.

	It is possible that pboxNewFirst and pboxNewLast are both null, that is, nothing was
	generated to replace the deleted stuff. In that case, this notifier might have to go
	away and be replaced by a VwMissingNotifier.

	It is possible that both first boxes, or both last boxes, are null, if only the other
	end of a property is affected.

	If pboxOldLast is non-null, and pboxNewLast is null, code must search for a new last box
	and possibly new string index limit for the recipint notifier, as appropriate for the
	last non-null remaining property. Therefore in such cases pboxOldFirst may not be null.
----------------------------------------------------------------------------------------------*/
void VwNotifier::ReplaceBoxes(VwBox * pboxOldFirst, VwBox * pboxOldLast,
	VwBox * pboxNewFirst, VwBox * pboxNewLast, NotifierVec & vpanoteDel)
{
	VwBox ** ppbox = Boxes();
	VwBox * pboxFirstThis = FirstBox();  // save before we possibly change it.
	for (int i = 0; i < m_cprop; i++, ppbox++)
	{
		if (*ppbox == pboxOldFirst)
		{
			if (pboxNewFirst)
			{
				*ppbox = pboxNewFirst;
			}
			else
			{
				// We're deleting boxes from pboxOldFirst to pboxOldLast.
				// It might work to make the first box of the property
				// pboxOldLast->NextBox...unless that box is in another property,
				// or even beyond the end of this.
				if (pboxOldLast == m_pboxLast || !pboxOldLast->NextOrLazy())
				{
					// This property is being deleted entirely.
					*ppbox = NULL;
				}
				else
				{
					VwBox * pboxCandidate = pboxOldLast->NextOrLazy();
					// It still might be being deleted entirely...
					// if so, the box that starts the next non-empty property
					// must be pboxOldLast->NextOrLazy.
					for (int iprop = i + 1; iprop < m_cprop; iprop++)
					{
						if (Boxes()[iprop] == pboxCandidate)
						{
							*ppbox = NULL;
							break;
						}
					}
					if (*ppbox)
					{
						// didn't find it...it was a continuation of this property
						// (and is now the start of it).
						*ppbox = pboxCandidate;
					}
				}
			}
		}
	}

	if (m_pboxLast == pboxOldLast)
	{
		if (pboxNewLast)
			m_pboxLast = pboxNewLast;
		else
		{
			if (pboxOldFirst == pboxFirstThis)
			{
				VwRootBox * prootb = pboxOldFirst->Root();
				// Everything we had is being deleted! Convert to VwMissingNotifier.
				VwMissingNotifierPtr qmnote;
				qmnote.Attach(VwMissingNotifier::Create(ObjectIndex(),
					m_cprop));
				PropTag * prgtagDst = qmnote->Tags();
				PropTag * prgtagSrc = Tags();
				::CopyItems(prgtagSrc, prgtagDst, m_cprop);
				qmnote->_KeyBox(pboxOldFirst->Container());
				qmnote->SetObject(Object());
				qmnote->SetParent(Parent()); // replacing this, so it should have the same parent.
				prootb->AddNotifier(qmnote);
				LockThis(); // Don't let it really go away until method exits.
				prootb->DeleteNotifier(this);
				vpanoteDel.Push(this); // when it's safe it should really get deleted.
				return;
			}
			// Otherwise we need to recompute LastBox.
			// NB do NOT use _SetLastBox; it depends on there being nothing in the box chain
			// after this object, but when regenerating there might be.
			Assert(pboxOldFirst != NULL); // couldn't properly terminate loop below.
			for (int iprop = m_cprop; --iprop >= 0;)
			{
				VwBox * pboxProp = Boxes()[iprop];
				if (pboxProp)
				{
					// OK, last box will be in this property.
					// If the last non-empty property is a sub-paragraph one, we need to adjust
					// the string limit to the end of the paragraph.
					if (StringIndexes()[iprop] >= 0)
					{
						VwParagraphBox * pvpboxProp = dynamic_cast<VwParagraphBox *>(pboxProp);
						Assert(pvpboxProp); // must be paragraph if using positive string indexes.
						m_itssLimSubString = pvpboxProp->Source()->CStrings();
					}
					else
					{
						// The last property is a 'whole box' property.
						m_itssLimSubString = -1; // must not have string index if it's a whole box property.
						// either it will be the last box in the chain, or the one before
						// the first box we're deleting, or itself if the first box we're
						// deleting is a child box.
						// There are three possible scenarios that we have to handle:
						// 1. The previous property is inside its own box: openobj...opendiv...
						//	  openprop...closeprop...closediv...openprop[now empty]...
						//	  In this case the last box is now the last thing in that previous
						//	  property, detected by pboxProp->NextOrLazy() being null.
						// 2. The previous property is a sequence of boxes in the same container
						//	  as the one now empty: openobj...opendiv...openprop...closeprop...
						//    openprop[now empty]... This is caught by
						//    pboxProp->NextOrLazy() == pboxOldFirst.
						// 3. The previous property is a dummy one created for a box added to
						//    contain the empty property, so the empty property is inside the box
						//    for the previous property: openobj...openprop...closeprop...opendiv...
						//    openprop...closeprop. This is caught by
						//    pboxProp->Contains(pboxOldFirst)
						while (pboxProp->NextOrLazy() && pboxProp->NextOrLazy() != pboxOldFirst &&
							(dynamic_cast<VwGroupBox *>(pboxProp) == NULL ||
							!dynamic_cast<VwGroupBox *>(pboxProp)->Contains(pboxOldFirst)))
						{
							pboxProp = pboxProp->NextOrLazy();
						}
					}
					m_pboxLast = pboxProp;
					break; // found a non-empty property, don't search further.
				}
			}
		}
	}

	// Do this last because it affects the key box, which we might need if changing to missing.
	if (pboxOldFirst != m_pboxKey)
		return;

	VwRootBox* prootb = m_pboxKey->Root();
	if (pboxNewFirst == NULL)
		pboxNewFirst = FirstBox();
	if (pboxNewFirst == NULL)
		prootb->DeleteNotifier(this);
	else
		prootb->ChangeNotifierKey(this, pboxNewFirst);
}


//:>********************************************************************************************
//:>	Misc. VwNotifier methods
//:>********************************************************************************************



/*----------------------------------------------------------------------------------------------
	The answer is first box, if it or its chain of following boxes includes	lastBox or one of
	lastBox's containers. Otherwise, it is the container of firstBox, if that satisfies the same
	constraint.

	Note that in a simple case, firstCoveringBox may not be a group box.

	For example: consider this structure
		div
			paragraph
				inner pile = first box
			para 2
				string attr (one of the attrs of this
			table
				table row
					table cell
						para
							string attr = last box
	The div is the lowest level box that contains everything this notifier
	points at, so the covering boxes are a list under the div.
	the first of them is the initial paragraph.

	Note that under current constraints for how things nest, structures as messy as the above
	can't actually happen. The first box is always at the top level, because any box opened
	inside the object must be closed before it, and it must be closed before any box that
	was opened outside it. However it is very possible for the last box to be nested inside
	firstbox or one of its followers (though there must be a chain of last boxes up to the
	follower).
----------------------------------------------------------------------------------------------*/
VwBox * VwNotifier::FirstCoveringBox()
{
	VwBox * pboxFirst = FirstBox();
	// Try to find a common container for firstBox and lastBox. Iit might actually be firstBox,
	// if it is in fact a group and lastBox is embedded in it.
	VwGroupBox * pgboxCommonContainer = ContainingBox();

	// Contains will not find what we want if it actually is the first box.
	if (pgboxCommonContainer == pboxFirst)
		return pboxFirst;

	// We want the box that is a direct sub-box of commonContainer, and contains (or is) the
	// firstBox
	VwBox * pboxContainer;
	pgboxCommonContainer->Contains(pboxFirst, &pboxContainer);

	return pboxContainer;
}


/*----------------------------------------------------------------------------------------------
	Similarly answer the last covering box.
	In the example above it would be the table.
	It is the container of lastBox() that has the same container as	firstCoveringBox()
	Result may be lazy box!
----------------------------------------------------------------------------------------------*/
VwBox* VwNotifier::LastCoveringBox()
{
	VwBox * pboxFirst = FirstBox();
	VwBox * pboxLast = LastBox();

	// try to find a common container for firstBox and lastBox it might actually be pboxLast,
	// if it is in fact a group.
	VwGroupBox * pgboxCommonContainer = ContainingBox();

	// Contains will not find what we want if it actually is the first box
	if (pgboxCommonContainer == pboxFirst)
		return pboxFirst;

	// We want the box that is a direct sub-box of commonContainer, and contains (or is) the
	// lastBox
	VwBox * pboxContainer;
	pgboxCommonContainer->Contains(pboxLast, &pboxContainer);

	return pboxContainer;
}

/*----------------------------------------------------------------------------------------------
	With current nesting constraints this answers the same as LastCoveringBox. However, it is
	required to be a box with the same parent as FirstBox(), and containing nothing after
	LastBox(). That is, FirstBox()...LastTopLevelBox() are in the same linked list, and
	represent the entire contents of this notifier.
----------------------------------------------------------------------------------------------*/
VwBox * VwNotifier::LastTopLevelBox()
{
	VwBox * pboxFirst = FirstBox();
	VwGroupBox * pgboxCont = pboxFirst->Container();
	VwBox * pboxRet = LastBox();
	while (pboxRet->Container() != pgboxCont)
	{
		pboxRet = pboxRet->Container();
		if (!pboxRet)
		{
			Assert(false);
			return NULL;
		}
	}
	return pboxRet;
}

/*----------------------------------------------------------------------------------------------
	Answer the group box that contains both your first and last boxes
----------------------------------------------------------------------------------------------*/
VwGroupBox* VwNotifier::ContainingBox()
{
	VwBox* pboxFirst = FirstBox();
	VwBox* pboxLast = LastBox();

	// Try to find a common container for firstBox and lastBox. It might actually be firstBox,
	// if it is in fact a group.
	VwGroupBox * pgboxCommonContainer = dynamic_cast<VwGroupBox *>(pboxFirst);

	if (!pgboxCommonContainer)
		pgboxCommonContainer = pboxFirst->Container();

	while (pgboxCommonContainer && !pgboxCommonContainer->Contains(pboxLast))
		pgboxCommonContainer = pgboxCommonContainer->Container();

	// since the root box always contains all related boxes, it is a bad bug not to find a
	// common container!
	Assert(pgboxCommonContainer != 0);

	return pgboxCommonContainer;
}


/*----------------------------------------------------------------------------------------------
	Answer a pointer to the first box covered by this notifier.
	If index is not null set it to the index of the box.
----------------------------------------------------------------------------------------------*/
VwBox * VwNotifier::FirstBox(int * ibox)
{
	//pointer to start of array of them
	VwBox ** ppbox = Boxes();

	//find the first one that is not null.
	int i = 0;
	while (*ppbox == 0)
	{
		++ppbox;
		++i;
	}

	if (ibox)
		*ibox = i;

	return *ppbox;
}

/*----------------------------------------------------------------------------------------------
	Set the m_pboxLast variable. Must be called exactly as part of the CloseObject() call to
	VwEnv. Establishes the proper values in m_pboxLast and m_itssLimSubString.

	These variables are used to indicate the end of the last property recorded in the notifier.
	m_pboxLast is needed if the last property is not contained in a paragraph; it is set
	to the current last box in the chain pointed to by the last non-empty property. (It is
	needed because further boxes may later be added to the chain.)

	m_itssLimSubString is needed if the last property IS embedded in a paragraph. It is set to
	the number of strings and/or substrings currently in the paragraph. It is needed because
	displays of subsequent objects might add to the paragraph.

	ENHANCE JohnT: set this up as private and make VwEnv::CloseObject a friend so only	that can
	call it.

	JohnT 1/8/02: now also called from ReplaceBoxes(), to re-establish the end conditions
	when clearing out the last property. Removed the commented-out assert as may no longer
	be true. If the deleted property was a subsequent box, as it must have been for this
	case to occur, the last property must end at the end of the paragraph box.
----------------------------------------------------------------------------------------------*/
void VwNotifier::_SetLastBox()
{
	VwBox ** ppbox = Boxes();

	for (int i = m_cprop - 1; i >= 0; i--)
	{
		if (ppbox[i])
		{
			if (StringIndexes()[i] >= 0)
			{
				VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(ppbox[i]);
				Assert(pvpbox);
				// property that is a sequence of strings within a para:
				// Note the paragraph and string index. There should not yet
				// be any subsequent boxes.
				//Assert(!ppbox[i]->NextOrLazy());
				m_itssLimSubString = pvpbox->Source()->CStrings();
				m_pboxLast = pvpbox;
			}
			else
			{
				m_pboxLast = ppbox[i]->EndOfChain();
				m_itssLimSubString = -1;
			}
			return;
		}
	}
	Assert(false);
}

/*----------------------------------------------------------------------------------------------
	Add ditss to any string index which relates to pbox
----------------------------------------------------------------------------------------------*/
void VwNotifier::FixOffsets(VwBox * pbox, int ditss)
{
	VwBox ** ppbox = Boxes();
	int * pitss = StringIndexes();
	for (int i = 0; i < m_cprop; ++i, ++ppbox, ++pitss)
	{
		if (*ppbox == pbox)
			*pitss += ditss;
	}
	if (m_pboxLast == pbox)
		m_itssLimSubString += ditss;
}

/*----------------------------------------------------------------------------------------------
	Find the first notifier whose level is at least lev and whose key box is pboxStart,
	or a box inside pboxStart, or a box following pboxStart, or a box following an ancestor
	of pboxStart...Consider all the boxes of the root as being ordered such that a box
	and all its descendents are before the next box at the same level, and also, a box
	is before its own descendents. We want the first box that is, or is after, pboxStart,
	and has a notifier whose level is at least lev and which, if itssStart >= 0, starts
	with at least that string index; and then, of the notifiers that qualify, we want the
	lowest-level one.
	If more than one notifier qualifies, we want the one with the smallest initial string index.

	Note that this will NOT expand lazy boxes. See the strategy used in FindChild if you need
	to find a lower level notifier that may not have been created yet.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwNotifier::NextNotifierAt(int lev, VwBox * pboxStart, int itssStart)
{
	AssertPtr(pboxStart);

	VwBox * pbox = pboxStart;
	VwRootBox * prootb = pbox->Root();
	int itss = itssStart;
	// This prevents us from looking inside a box if we are looking for something
	// after one of the strings of that box. After we've moved past the first box, itss will
	// be -1, and we can move down again (typically after moving up).
	bool fIncludeChildren = false; // initial value not used, value used at end of I1, set during it.
	for (; pbox; pbox = pbox->NextInRootSeq(false, NULL, fIncludeChildren))
	{
		fIncludeChildren = itss < 0; // Will become true during second iteration, if not already.
		NotifierMap * pmmboxqnote;
		prootb->GetNotifierMap(&pmmboxqnote);
		NotifierMap::iterator itboxnote;
		NotifierMap::iterator itboxnoteLim;
		pmmboxqnote->Retrieve(pbox, &itboxnote, &itboxnoteLim);
		VwNotifier * pnoteBest = NULL;
		// loop over the candidate notifiers.
		for (; itboxnote != itboxnoteLim; ++itboxnote)
		{
			VwAbstractNotifier * panote = itboxnote.GetValue();

			VwNotifier * pnote = dynamic_cast<VwNotifier *>(panote);
			if (!pnote)
				continue; // not interested in other notifier types
			if (pnote->Level() < lev)
				continue;
			if (itss >= 0)
			{
				// It has to start with our box and index
				if (pnote->Boxes()[0] != pbox)
					continue;
				if (pnote->StringIndexes()[0] < itss)
					continue;
			}
			if (pnoteBest && (pnoteBest->Level() < pnote->Level()
				|| pnoteBest->StringIndexes()[0] < pnote->StringIndexes()[0]))
				continue;
			pnoteBest = pnote;
		}
		if (pnoteBest)
			return pnoteBest;

		// In the next box, any string index will do
		itss = -1;
	}
	return NULL; // didn't find a qualifiying notifier at all.
}

/*----------------------------------------------------------------------------------------------
	Find the notifier for the next object in the same sequence, if any.
	Note that this will NOT expand lazy boxes. See the strategy used in FindChild if you need
	to find a lower level notifier that may not have been created yet.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwNotifier::NextNotifier()
{
	VwBox * pbox = m_pboxLast;
	int itss = m_itssLimSubString;
	if (itss >= 0 && dynamic_cast<VwParagraphBox *>(pbox)->Source()->CStrings() > itss)
	{
		// Start looking with the next string of the same box, which is the limit of the
		// current notifier, so nothing to do.
		// itss++; // Old code, I think a bug...
	}
	else
	{
		itss = -1; // important in case starting point was last tss in para.
		// This notifier doesn't end part-way through a box, but with a complete box (or with the
		// last string in a possibly embedded paragraph box).
		// Typically, we want to start our search for the following notifier with the
		// box that follows this notifier's last box.
		// But there may not be one. Then it gets complicated :-).
		// If m_pboxLast doesn't have a following box, it may be that this is the
		// notifier for the last object in the sequence. But it may also be that
		// this notifier ends with some box embedded inside the box that represents
		// the object. That suggests starting with the box following m_pboxLast's
		// container...or it's container, if it doesn't have a following box, and so forth.
		// But there's one MORE complication. If one of the containers is contained by a
		// paragraph, we don't want to start looking at the following box in the paragraph...
		// we want to start looking at the paragraph itself, starting with the string
		// index following the one for the container we're in.
		for (; pbox; pbox = pbox->Container())
		{
			VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pbox->Container());
			if (pvpbox)
			{
				// Figure the string index for pbox within pvpbox.
				Assert(pbox->IsInnerPileBox()); // It's in a paragraph and contains things.
				// Count the preceding boxes that aren't derived from TsStrings.
				int cNonStringPrev = 0;
				for (VwBox * pboxT = pvpbox->FirstBox(); pboxT && pboxT != pbox; pboxT = pboxT->NextOrLazy())
				{
					if (!pboxT->IsBoxFromTsString())
						cNonStringPrev++;
				}
				// Work through the Source() until you find the index corresponding to pbox.
				VwTxtSrc * psrc = pvpbox->Source();
				int ctss = psrc->CStrings();
				int iNonString = 0;
				int itssT = 0;
				for (; itssT < ctss; itssT++)
				{
					ITsStringPtr qtss;
					psrc->StringAtIndex(itssT, &qtss);
					if (!qtss)
					{
						// This index is for a non-string box.
						if (iNonString == cNonStringPrev)
							break;
						else
							iNonString++;
					}
				}
				if (itssT + 1 < ctss)
				{
					// There are more strings in the paragraph...we can start the search with
					// this paragraph and the string index we figured.
					pbox = pvpbox;
					itss = itssT + 1;
					break;
				}
			}
			else
			{
				// Container not a paragraph...want the following box if any.
				if (pbox->NextOrLazy())
				{
					pbox = pbox->NextOrLazy();
					break;
				}
			}
		}
	}
	int level = Level();
	VwNotifier * pnoteParent = Parent();
	for (; pbox; )
	{
		VwNotifier * pnote = NextNotifierAt(level, pbox, itss);
		// It is just possible to get a more deeply nested notifier, or one
		// that somehow belongs to a separator, not one of the objects we want.
		for (; pnote && pnote->Parent() != pnoteParent; pnote = pnote->Parent())
		{
			// If, however, we found a notifier that is not embedded in our parent at all,
			// we have run out and must give up.
			if (!pnote)
				return NULL;
		}
		if (!pnote)
			return NULL;
		if (pnote->PropIndex() == this->PropIndex())
		{
			Assert(pnote->ObjectIndex() > this->ObjectIndex());
			return pnote; // got it
		}
		// Otherwise, it may be a notifier for some box embedded inside this...if so,
		// keep looking.
		pbox = pnote->LastBox()->NextBoxAfter();
		itss = -1;
	}
	return NULL; // ran out of boxes
}

/*----------------------------------------------------------------------------------------------
	Get a box that functions as a "limit" for property iprop of this object, in the sense that
	for the last box in the property pboxLast->NextOrLazy() == pboxLim. pboxLim may be null,
	if nothing (in the same containing box) follows the last box in this property.

	If pitss is non-null, also return the string index that is the limit for this property,
	or -1 if it is not a string-level property.

	Note that in a loop over the boxes in the property, it is necessary to test for reaching
	the end of the chain as well as for reaching pboxLim. Sometimes a containing box ends
	between one property and the next, and the chain of boxes for a property therefore does
	not link up with the following property.
----------------------------------------------------------------------------------------------*/
VwBox * VwNotifier::GetLimOfProp(int ipropTarget, int * pitssLim)
{
	// If there is a subsequent property
	for (int iprop = ipropTarget + 1; iprop < CProps(); iprop++)
	{
		VwBox * pboxResult = Boxes()[iprop];
		if (pboxResult)
		{
			if (pitssLim)
			{
				VwBox * pboxTarget = Boxes()[ipropTarget];
				if (pboxTarget == pboxResult)
					*pitssLim = StringIndexes()[iprop];
				// If the property isn't a string-within-paragraph one, return -1.
				else if (StringIndexes()[ipropTarget] == -1)
					*pitssLim = -1;
				else // It is a string-within-paragraph one, the target box better be para.
				{
					Assert(dynamic_cast<VwParagraphBox *>(pboxTarget));
					*pitssLim = dynamic_cast<VwParagraphBox *>(pboxTarget)->Source()->CStrings();
				}
			}
			return pboxResult;
		}
	}
	// No subsequent property, use special instance variables.
	if (pitssLim)
		*pitssLim = m_itssLimSubString;
	if (m_itssLimSubString >= 0)
		return LastBox(); // limit is in the paragraph box of the previous property.
	return LastBox()->NextOrLazy();
}

/*----------------------------------------------------------------------------------------------
	Find the notifier for the object at index ihvoTarget in occurrence ipropTag of property tag.
	Note that this method won't find VwMissingNotifiers, it will only work if there is a real
	one.

	However, it will expand a lazy box that is hiding the display of the target object.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwNotifier::FindChild(PropTag tag, int ipropTag, int ihvoTarget, int ich)
{
	int cpropTag = 0;  // Count occurrences of property tag
	int iprop = -1;
	for (PropTag * ptag = Tags(); ptag < Tags() + m_cprop; ptag++)
	{
		if (*ptag == tag)
		{
			if (cpropTag == ipropTag)
			{
				iprop = ptag - Tags();
				break;
			}
			cpropTag++;
		}
	}
	if (iprop < 0)
		return NULL; // failure.
	return FindChild(iprop, ihvoTarget, ich);
}
/*----------------------------------------------------------------------------------------------
	Find the notifier for the object at index ihvoTarget in property iprop.
	It will expand a lazy box that is hiding the display of the target object.
	If ihvoTarget is -1, it will use ich to identify an ORC and find the root notifier
	for the embedded object display for that character (if possible). Otherwise, ich is
	ignored.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwNotifier::FindChild(int iprop, int ihvoTarget, int ich)
{
	VwBox * pbox = Boxes()[iprop];
	if (!pbox)
		return NULL; // empty prop, can't find object notifier, no objects
	// Scan the list of boxes. If this property was displayed lazily, we may find a lazy box
	// that represents it. If so we can make the necessary expansion.
	VwBox * pboxLim = GetLimOfProp(iprop);
	for (VwBox * pboxProp = pbox;
		pboxProp && pboxProp != pboxLim;
		pboxProp = pboxProp->NextOrLazy())
	{
		VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(pboxProp);
		if (!plzb)
			continue;
		// If it's a lazy box that relates to the object of this notifier, it must be for the
		// right property because it's a top-level box in the sequence for this property,
		// so if it also covers the right object index, expand it.
		if (plzb->Object() != this->Object())
			continue;
		int ihvoMin = plzb->MinObjIndex();
		if (ihvoMin > ihvoTarget)
			break; // we aren't going to find later useful fragments of laziness
		if (ihvoMin + plzb->CItems() <= ihvoTarget)
			continue; // target not in this lazy box
		plzb->ExpandItems(ihvoTarget - ihvoMin, ihvoTarget - ihvoMin + 1, NULL);
		break; // expanded the thing we want.
	}
	// Do this again, an Expand above may have replace your first box, in which case pbox is
	// an invalid pointer to a deleted box.
	pbox = Boxes()[iprop];

	if (ihvoTarget == -1)
	{
		// special case for embedded object display.
		VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
		if (!pvpbox)
			return NULL;
		for (VwBox * pboxChild = pvpbox->FirstBox(); pboxChild; pboxChild = pboxChild->NextOrLazy())
		{
			VwMoveablePileBox * pmpbox = dynamic_cast<VwMoveablePileBox *>(pboxChild);
			if (!pmpbox || pmpbox->CharIndex() != ich)
				continue;
			// Now, we want a notifier for stuff INSIDE the pmpbox.
			VwNotifier * pnoteSub = NextNotifierAt(Level() + 1, pmpbox->FirstBox(), -1);
			if (!pnoteSub)
				return NULL; // No objects, or something.
			if (pnoteSub->Parent() != this)
				return NULL; // Ran out of our objects, found something else
			// This is not infallible, there could be more than one root object in the
			// embedded stuff. Then we'll need some more info to select one of them.
			// It's also remotely possible that the MP box we found has NO embedded
			// notifiers, and we have now found some other child of the outer notifier.
			// These are very pathological cases. For now, assume we got the one we want.
			return pnoteSub;
		}
		return NULL; // looking for an embedded object, not found.
	}

	VwNotifier * pnote = NextNotifierAt(Level() + 1, pbox, StringIndexes()[iprop]);
	if (!pnote)
		return NULL; // No objects, or something.
	if (pnote->Parent() != this)
		return NULL; // Ran out of our objects, found something else
	for (; pnote && pnote->ObjectIndex() < ihvoTarget; pnote = pnote->NextNotifier())
		;
	if (!pnote)
		return NULL; // ran out without finding it, maybe no box generated for this object?
	if (pnote->ObjectIndex() != ihvoTarget)
		return NULL; // got past index without finding it, maybe same reason?
	return pnote;
}

/*----------------------------------------------------------------------------------------------
	The basic idea of this method is to locate a particular object (ihvoTarget) within a
	particular property of this notifier (iprop), without expanding lazy boxes. The property
	is assumed to be a lazy one, meaning that we don't need to worry about string indexes: each
	object is represented by one or more complete boxes, or nothing at all.

	If the object is expanded and represented by boxes, return a VwNotifier.
		- if there is a lazy box in the property immediately before the first box of the
		target notifier return it in plzbPrev.
		- if there is a non-lazy box in the property immediately before the first box of
		the target notifier, return the VwNotifier that covers it in ppnotePrev.
	If the object is embedded in a lazy box, set *pplzb to the lazy box and return NULL.
		- if there is a lazy box in the property immediately before *pplzb return it in
		plzbPrev.
		- if there is a non-lazy box in the property immediately before *pplzb, return the
		VwNotifier that covers it in ppnotePrev.
	If the object expanded to nothing, return the appropriate VwMissingNotifier.
		- if there is a lazy box in the property immediately before where the target object's
		representation belongs return it in plzbPrev.
		- if there is a non-lazy box in the property immediately before where the target
		object's representation belongs, return the VwNotifier that covers it in ppnotePrev.
	If there is no lazy box or notifier for this ihvo, return NULL and set **pplzb to NULL.
	Assume ihvoTarget is after the end of the property (probably we are inserting at the end).
		- if there is a lazy box in the property immediately before the end return it in
		plzbPrev.
		- if there is a non-lazy box in the property immediately before the end, return the
		VwNotifier that covers it in ppnotePrev.

	*plzbPrev and *pnotePrev are set to null when they don't apply. If both are null it means
	there is nothing in the property before the target hvo.

	Note that, because of the way lazy properties are generated, we don't have to worry about
	boxes in the sequence that are not part of a sub-object. If we have the previous notifier,
	its last box is right before the one we want.

	Note: this seems like a potentially useful method so I'm leaving the code here, but it
	isn't currently used and so has not yet been tested.
----------------------------------------------------------------------------------------------*/
VwAbstractNotifier * VwNotifier::FindChildOrLazy(int iprop, int ihvoTarget, VwLazyBox ** pplzb,
	VwNotifier ** ppnotePrev, VwLazyBox ** pplzbPrev)
{
	*ppnotePrev = NULL;
	*pplzbPrev = NULL;
	*pplzb = NULL;
	VwBox * pbox = Boxes()[iprop];
	VwLazyBox * plzbTarget = NULL; // set if we find a lazy box that covers the range
	VwLazyBox * plzbPrev = NULL; // set to last lazy box before ihvoTarget, if any
	VwNotifier * pnoteTarget = NULL; // set if we find a matching VwNotifier.
	VwNotifier * pnotePrev = NULL; // set to last VwNotifier we find with smaller ihvo
	if (pbox)
	{
		// Scan the list of boxes for relevant lazy boxes.
		VwBox * pboxLim = GetLimOfProp(iprop);
		for (VwBox * pboxProp = pbox;
			pboxProp && pboxProp != pboxLim;
			pboxProp = pboxProp->NextOrLazy())
		{
			VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(pboxProp);
			if (!plzb)
				continue;
			// If it's a lazy box that relates to the object of this notifier, it must be for the
			// right property because it's a top-level box in the sequence for this property,
			// so if it also covers the right object index, expand it.
			if (plzb->Object() != this->Object())
				continue;
			int ihvoMin = plzb->MinObjIndex();
			if (ihvoMin > ihvoTarget)
				break; // we aren't going to find later useful lazy boxes
			if (ihvoMin + plzb->CItems() <= ihvoTarget)
			{
				// This is the closest previous lazy box, unless we find a closer one later.
				plzbPrev = plzb;
				continue; // target not in this lazy box
			}
			plzbTarget = plzb;
			break; // found the thing we want.
		}

		// Scan sub-notifiers for relevant ones.
		VwNotifier * pnote = NextNotifierAt(Level() + 1, pbox, StringIndexes()[iprop]);
		if (pnote && pnote->Parent() == this)
		{
			// we got a relevant child notifier. While object index is less than the one we
			// want, note it in pnotePrev
			for (; pnote && pnote->ObjectIndex() < ihvoTarget; pnote = pnote->NextNotifier())
				pnotePrev = pnote;
			// If we got one with a matching index, note it...otherwise we may have run out
			// or found one with a higher index, indicating no exact match exists.
			if (pnote && pnote->ObjectIndex() == ihvoTarget)
				pnoteTarget = pnote;
		}
	}
	// Decide which of these two is closest, if we found both
	if (plzbPrev && pnotePrev)
	{
		if (plzbPrev->MinObjIndex() + plzbPrev->CItems() < pnotePrev->ObjectIndex())
			plzbPrev = NULL;
		else
			pnotePrev = NULL;
	}
	*ppnotePrev = pnotePrev;
	*pplzbPrev = plzbPrev;

	if (!plzbTarget && !pnoteTarget)
	{
		// Look for a relevant missing notifier. It will be attached to the containing box.
		VwGroupBox * pgboxContainer = ContainingBox();
		NotifierMap * pmmboxqnote;
		pgboxContainer->Root()->GetNotifierMap(&pmmboxqnote);
		NotifierMap::iterator itboxnote;
		NotifierMap::iterator itboxnoteLim;
		VwBox * pboxContainer = pgboxContainer;
		pmmboxqnote->Retrieve(pboxContainer, &itboxnote, &itboxnoteLim);
		// loop over the candidate notifiers, processing missing ones.
		for (; itboxnote != itboxnoteLim; ++itboxnote)
		{
			VwMissingNotifier * pmnote =
				dynamic_cast<VwMissingNotifier *>(itboxnote.GetValue().Ptr());
			if (pmnote && pmnote->Parent() == this &&
				pmnote->PropIndex() == iprop && pmnote->ObjectIndex() == ihvoTarget)
			{
				return pmnote;
			}
		}
		// Assume ihvoTarget is beyond the end of the property (typically one beyond).
		// Since pzbTarget and pnoteTarget are both null, the following code does what we want.
	}
	*pplzb = plzbTarget;
	return pnoteTarget;
}

//:>********************************************************************************************
//:>	VwRegenNotifier methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	This construtor is declared protected. Use Create() to initialize object.
----------------------------------------------------------------------------------------------*/
VwRegenNotifier::VwRegenNotifier(int ihvoProp, int cprop)
	:VwAbstractNotifier(ihvoProp)
{
	m_cprop = cprop;
}

/*----------------------------------------------------------------------------------------------
	pboxOldFirst..pboxOldLast are (possibly part of) a property, and are being replaced by
	pboxNewFirst..pboxNewLast. If either or both of them are important to this notifier,
	make appropriate updates. Note that it is possible that the two old pointers are the same--
	for example, when a property that used to contain one object (and hence paragraph) now
	contains two.
	In general, pboxOldLast should not be important unless it is your last box, in which
	case, replace that with pboxNewLast.
	If pboxOldFirst is the start of any of your	properties, replace with pboxNewFirst
	Also if it is your key box, fix things.
----------------------------------------------------------------------------------------------*/
void VwRegenNotifier::ReplaceBoxes(VwBox * pboxOldFirst, VwBox * pboxOldLast,
	VwBox * pboxNewFirst, VwBox * pboxNewLast, NotifierVec & vpanoteDel)
{
	if (pboxNewFirst != NULL)
	{
		// Note: it is tempting here to arrange to delete *this, if its key box is pboxOldFirst.
		// Please do NOT do this. See notes on FWR-1201. It is unnecessary when pboxOldFirst
		// represents an object that is going away, because we separately identify notifiers
		// to delete. It is potentially harmful when inserting material before pboxOldFirst, though
		// this routine should not be called in that case. It is definitely harmful when
		// inserting strings into a paragraph, because then pboxOldFirst is the temporary box
		// in which we assembled the new strings, and any of their regen notifiers need to move
		// with the new material into the real paragraph box.
		SuperClass::ReplaceBoxes(pboxOldFirst, pboxOldLast, pboxNewFirst, pboxNewLast, vpanoteDel);
		return;
	}
	if (pboxOldFirst == NULL)
		return; // no action needed, and calling Container() has bad results!

	// If we don't have a replacement box, hook it to the next higher container.
	SuperClass::ReplaceBoxes(pboxOldFirst, pboxOldLast, pboxOldFirst->Container(),
		pboxOldFirst->Container(), vpanoteDel);
}

// Given some box (from which we can get a root box and hence a data access),
// test whether the given HVO is a real, valid object (by attempting to retrieve its class).
bool IsRealObject(VwBox * pboxKey, HVO hvo)
{
	VwRootBox * prootb = pboxKey->Root();
	ISilDataAccess * psda = prootb->GetDataAccess();
	ComBool fValid;
	CheckHr(psda->get_IsValidObject(hvo, &fValid));
	return fValid;
}

void VwRegenNotifier::Regenerate()
{

	// Get all the notifiers which have anything to do with our box.
	NotifierVec vpanote;
	VwGroupBox * pgboxCont = KeyBox()->Container();
	// If we don't have a container, we are right at the root, and will regenerate
	// everything. Happens automatically because with no notifiers the loops below
	// don't find anything.
	if (pgboxCont)
		pgboxCont->GetNotifiers(KeyBox(), vpanote);

	// We are looking for two notifiers. One contains the property we are going to regenerate,
	// The other is one of its children and indicates which object in that property is
	// represented by the box we are replacing.
	// For example, suppose we are showing LexEntries, and inside each a sequence of LexSenses.
	// For each LexSense we show a complex header paragraph, then a sequence of example
	// sentence paragraphs. We choose not to display anything at all for example sentences
	// where the translation is empty. Thus, if an example has an empty translation, we get
	// no paragraph box at all for that Sentence object.

	// Now, there are several ways we might arrange the box structure. If all the paragraphs
	// are direct children of the root, the VwMissingNotifier points at the root itself,
	// no suitable Notifier can be found to regenerate part of it, and we will regenerate
	// the entire root contents. Hopefully this is not done often!

	// Suppose instead that we make a Div for each LexSense. Now, the VwMissingNotifier is
	// pointing at the DivBox (to which we would have added its paragraph, if there had
	// been one). We want to regenerate the whole LexSense. Thus, we need to find one notifier
	// that has the Senses property of the LexEntry, and one of its children that indicates
	// which sense.

	// Now suppose we go one better. Make a Div for just the list of example sentences.
	// Then, we can regenerate by just redoing the Examples property of the LexSense.
	// The whole contents of that property will be the DivBox that is our target. We can find
	// the notifier for the LexSense, but this time, there is no notifier that corresponds
	// to a particular item.

	// The problem is to distinguish these cases. One easy one is that if no notifier knows
	// anything about our box, we have to regenerate from the root down.

	// If we find a notifier with some interest in our box, the next issue is whether there
	// is any property of that notifier which, when regenerated, will entirely replace that box.
	// Properties that just represent a string within our box are not interesting. We want a
	// property that points to our box (or one of its predecessors), and covers the whole of
	// that box (i.e., it does not somehow stop at the end of a string within our box).

	// It's possible there is more than one such property. For example, if we have a Div for the
	// example sentences, but not for the LexSenses, then the Div box is part of both the
	// Examples property of the LexSense and the Senses property of the entry. We want the most
	// deeply embedded one that qualifies, to minimize the work of regenerating.

	// Once we have the right property, the next goal is to find what target covers the box
	// we are interested in. We figure this by scanning the list of notifiers interested
	// in our box, one level down from the one that will regenerate, and finding the min and
	// max object indexes.

	VwNotifier * pnoteBest = NULL;
	int ipropBest = 0;

	BoxSet boxsetTargets;

	KeyBox()->Containers(&boxsetTargets);
	VwBox * pboxKey = KeyBox();
	boxsetTargets.Insert(pboxKey);

	// Pick the one that is a real notifier and has a property that covers our key box
	// as a complete box (not just a range of strings within the box). If there is more
	// than one eligible notifier, pick the one with the highest level.
	int inote;
	for (inote = 0; inote < vpanote.Size(); inote++)
	{
		VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[inote].Ptr());
		// No use in finding other VwMissingNotifiers! That would be an infinite loop.
		if (!pnote)
			continue;
		if (!IsRealObject(pboxKey, pnote->Object()))
			continue;

		int cprop = pnote->CProps();
		for (int iprop = 0; iprop < cprop; iprop++)
		{
			// If this is a string-within-para property we aren't interested
			if (pnote->StringIndexes()[iprop] >= 0)
				continue;
			PropTag tag = pnote->Tags()[iprop];
			if (tag == ktagGapInAttrs || tag == ktagNotAnAttr)
				continue; // can't regenerate using this
			VwNoteProps vnp = (VwNoteProps)((pnote->Flags()[iprop]) & kvnpPropTypeMask);
			// These are the only ones we can sensibly handle like this.
			if (vnp != kvnpObjProp && vnp != kvnpObjVec && vnp != kvnpObjVecItems
				&& vnp != kvnpLazyVecItems)
				continue;
			// See if our box is part of this property. The property extends to the last box
			// of the notifier, unless there is a subsequent property that has a box, in
			// which case it extends to the box before that; or possibly, this property
			// has a distinct box chain which ends before the next prop, in which case
			// running out of NextBox values terminates us.
			VwBox * pboxLim = pnote->LastBox()->NextOrLazy();
			int itssLim = pnote->LastStringIndex();
			for (int iprop2 = iprop + 1; iprop2 < cprop; iprop2++)
			{
				if (pnote->Boxes()[iprop2])
				{
					pboxLim = pnote->Boxes()[iprop2];
					itssLim = pnote->StringIndexes()[iprop2];
					break;
				}
			}
			// If it's just part of a box, it's no good
			if (itssLim >= 0)
				continue;
			// Now we have a regeneratable property that is a box sequence (rather than a TsString
			// sequence). See if our key box (or one of its ancestors) is in it.
			for (VwBox * pbox = pnote->Boxes()[iprop];
				pbox && pbox != pboxLim;
				pbox = pbox->NextOrLazy())
			{
				if (boxsetTargets.IsMember(pbox))
				{
					// Got a notifier which can do it!
					if ((!pnoteBest) || pnoteBest->Level() < pnote->Level())
					{
						pnoteBest = pnote;
						ipropBest = iprop;
						iprop = cprop; // force double break: done with prop and notifier
						break;
					}
				}
			}
		}
	}
	if (!pnoteBest)
	{
		// Last resort is to rebuild everything. Make sure we only rebuild the VwRootBox
		// we care about and not rebuild any that may be sync'd with it.
		KeyBox()->Root()->Reconstruct(false); // Destroys *this! Do no more method calls!!
		return;
	}

	// OK, we are going to pretend that prop ipropBest of notifier pnoteBest changed.
	// See if we can limit a range of items that is affected.
	ISilDataAccess * psda = KeyBox()->Root()->GetDataAccess();

	int ihvoMin = 0; // Correct if it is an atomic prop.
	int ihvoLim = 1;
	VwNoteProps vnp = (VwNoteProps)((pnoteBest->Flags()[ipropBest]) & kvnpPropTypeMask);
	if (vnp != kvnpObjProp)
	{
		// Since we only consider three property types eligible, it must be an object vec.
		// Therefore it is relevant to try to figure out the changed index.
		// If we can find embedded notifiers for some of the objects, and they apply to our box,
		// use them to figure the range.
		ihvoMin = INT_MAX;
		ihvoLim = 0;

		for (inote = 0; inote < vpanote.Size(); inote++)
		{
			VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[inote].Ptr());
			if (!pnote)
				continue;
			if (pnote->Parent() != pnoteBest)
				continue;
			int ihvo = pnote->ObjectIndex();
			ihvoMin = std::min(ihvoMin, ihvo);
			ihvoLim = std::max(ihvoLim, ihvo + 1);
		}
		if (ihvoMin == INT_MAX)
		{
			// We did not find any. Perhaps there are no lower level notifiers, or they
			// point at embedded boxes. Regenerate the whole property.
			ihvoMin = 0;
			CheckHr(psda->get_VecSize(pnoteBest->Object(), pnoteBest->Tags()[ipropBest], &ihvoLim));
		}
	}
	HVO hvoParent = pnoteBest->Object();
	PropTag tagParent = pnoteBest->Tags()[ipropBest];
	// Check whether the rebuild has a chance of working. If the property is no longer valid,
	// probably we are doing a refresh. If it no longer has the required number of items, probably
	// a higher-level update is also in progress, and a change at that level will fix things.
	// Initially the code checked if the property is still in the cache, but that doesn't check
	// for virtual properties. All this was done to fix LT-2987 (and the modifications to fix
	// TE-4005).
	ComBool fIsValid;
	CheckHr(psda->get_IsValidObject(hvoParent, &fIsValid));
	if (!fIsValid)
		return;
	if (vnp != kvnpObjProp)
	{
		// vector prop...check length for extra safety.
		int chvo;
		CheckHr(psda->get_VecSize(hvoParent, tagParent, &chvo));
		if (ihvoLim > chvo)
			return;
	}
	// Finally we are ready to do our fake property change. Pretend we deleted all the items
	// related to the box we want to regenerate, and inserted them again.
	CheckHr(pnoteBest->PropChanged(hvoParent, tagParent, ihvoMin,
		ihvoLim - ihvoMin, ihvoLim - ihvoMin));
	return;
}
//:>********************************************************************************************
//:>	VwMissingNotifier methods
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructors etc.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Used instead of constructor which is private.
----------------------------------------------------------------------------------------------*/
VwMissingNotifier * VwMissingNotifier::Create(int ihvoProp, int cprop)
{
	// allocate enough memory to hold the object itself,
	// plus its arrays of box pointers, attr tags, char indices, fragment ids, view constructor
	// pointers, style pointers and flags.
	// (minus the one pointer that is embedded in the Notifier itself).
	return NewObjExtra((cprop - 1) * isizeof(PropTag))
		VwMissingNotifier(ihvoProp, cprop);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwMissingNotifier::~VwMissingNotifier()
{
	// Nothing special to do.
}


//:>********************************************************************************************
//:>	IVwNotifyChange methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Overrides the trival VwAbstractNotifier::PropChanged. Figures the lowest level real notifer
	that covers its box, and triggers an appropriate regenerate.
	OPTIMIZE JohnT: could we have it record a position within a paragraph as well as a box,
	and thus reduce the amount we have to regenerate? But things within a paragraph rarely
	produce nothing at all, they usually just make an empty string...

	NOTE: Changes made here should NOT sync in any way, because of the danger that the sync'd
	roots haven't seen the change yet and won't be in a consistent state.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMissingNotifier::PropChanged(HVO hvoChange, int tag, int ivMin, int cvIns,
	int cvDel)
{
	BEGIN_COM_METHOD;
	// We don't have anything to do if it isn't our object or one of the tags we care about.
	if (hvoChange != m_hvo)
		return S_OK;
	PropTag * ptagLim = Tags() + m_cprop;
	bool fFound = false;
	for (PropTag * ptag = Tags(); ptag < ptagLim; ptag++)
	{
		if (*ptag == tag)
		{
			fFound = true;
			break;
		}
	}
	if (!fFound)
		return S_OK;

	Regenerate();

	END_COM_METHOD(g_factM, IID_IVwNotifyChange);
}

//:>********************************************************************************************
//:>	VwPropListNotifier methods
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructors etc.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Used instead of constructor which is private.
----------------------------------------------------------------------------------------------*/
VwPropListNotifier * VwPropListNotifier::Create(int ihvoProp, int cprop)
{
	// allocate enough memory to hold the object itself,
	// plus its arrays of box pointers, attr tags, char indices, fragment ids, view constructor
	// pointers, style pointers and flags.
	// (minus the one pointer that is embedded in the Notifier itself).
	return NewObjExtra((cprop - 1) * isizeof(PropTag) + cprop * isizeof(HVO))
		VwPropListNotifier(ihvoProp, cprop);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwPropListNotifier::~VwPropListNotifier()
{
	// Nothing special to do.
}


//:>********************************************************************************************
//:>	IVwNotifyChange methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Overrides the trival VwAbstractNotifier::PropChanged. Figures the lowest level real notifer
	that covers its box, and triggers an appropriate regenerate.
	OPTIMIZE JohnT: could we have it record a position within a paragraph as well as a box,
	and thus reduce the amount we have to regenerate? But things within a paragraph rarely
	produce nothing at all, they usually just make an empty string...
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropListNotifier::PropChanged(HVO hvoChange, int tag, int ivMin, int cvIns,
	int cvDel)
{
	BEGIN_COM_METHOD;

	// We don't have anything to do if it isn't our object or one of the tags we care about.
	PropTag * ptagLim = Tags() + m_cprop;
	HVO * phvo = Objects();
	PropTag * ptag = Tags();
	for (; ptag < ptagLim; ptag++, phvo++)
	{
		if (*ptag == tag && *phvo == hvoChange)
		{
			break;
		}
	}
	if (ptag >= ptagLim)
		return S_OK; // no match

	Regenerate();

	END_COM_METHOD(g_factL, IID_IVwNotifyChange);
}

/*----------------------------------------------------------------------------------------------
	Add yourself to the map from objects to notifiers. By default the key is the
	notifier's one main object, but VwPropListNotifier needs to override.
----------------------------------------------------------------------------------------------*/
void VwPropListNotifier::AddToMap(ObjNoteMap & mmhvoqnote)
{
	HVO * phvoLim = Objects() + m_cprop;
	for (HVO * phvo = Objects(); phvo < phvoLim; phvo++)
	{
		mmhvoqnote.Insert(*phvo, this);
	}
}

/*----------------------------------------------------------------------------------------------
	StringValueNotifier does not use either m_cprop or m_ihvoProp from its baseclass.
----------------------------------------------------------------------------------------------*/
VwStringValueNotifier::VwStringValueNotifier(HVO hvo, int tag, int ws, ITsString * ptssVal,
	ISilDataAccess * psda)
: VwRegenNotifier(0,0)
{
	m_hvo = hvo;
	m_tag = tag;
	m_ws = ws;
	m_qtssVal = ptssVal;
	m_fWasInitiallyEqual = EvalString(psda);
}


VwStringValueNotifier::~VwStringValueNotifier()
{
}

STDMETHODIMP VwStringValueNotifier::PropChanged(HVO hvoChange, int tag, int ivMin, int cvIns, int cvDel)
{
	BEGIN_COM_METHOD;
	if (hvoChange != m_hvo || tag != m_tag)
		return S_OK;
	ISilDataAccess * psda = KeyBox()->Root()->GetDataAccess();
	if (EvalString(psda) != m_fWasInitiallyEqual)
		Regenerate();

	END_COM_METHOD(g_factL, IID_IVwNotifyChange);
}

bool VwStringValueNotifier::EvalString(ISilDataAccess * psda)
{
	ITsStringPtr qtssCurrent;
	if (m_ws == 0)
		CheckHr(psda->get_StringProp(m_hvo, m_tag, &qtssCurrent));
	else
		CheckHr(psda->get_MultiStringAlt(m_hvo, m_tag, m_ws, &qtssCurrent));
	if (m_qtssVal)
	{
		ComBool fResult;
		CheckHr(m_qtssVal->Equals(qtssCurrent, &fResult));
		return fResult;
	}
	else if (!qtssCurrent)
		return true;
	else
	{
		int cch;
		CheckHr(qtssCurrent->get_Length(&cch));
		return cch == 0;
	}
}
