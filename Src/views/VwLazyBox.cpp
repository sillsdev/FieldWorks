/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwLazyBox.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Implementation for VwLazyBox
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

using namespace std;

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Replace (same as Vector.Replace())
----------------------------------------------------------------------------------------------*/
void VwLazyInfo::Replace(int iHvoMin, int iHvoLim, HVO * prghvoItems, int chvoItems)
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());
	m_vhvo.Replace(iHvoMin, iHvoLim, prghvoItems, chvoItems);

	long * name = (long *) new long[chvoItems];
	for(int i = 0; i < chvoItems; i++)
		name[i] = 1;

	m_vloEstimatedHeight.Replace(iHvoMin, iHvoLim, name, chvoItems);
	delete [] name;
}

/*----------------------------------------------------------------------------------------------
	Copys this VwLazyInfo items from ihvoMin to ihvoLim into the specified VwLazyInfo. Assumes
	that vwlzi is empty or that you want to insert into the beginning of the Vector.
----------------------------------------------------------------------------------------------*/
void VwLazyInfo::CopyRange(int ihvoMin, int ihvoLim, VwLazyInfo & vwlzi)
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());

	vwlzi.m_vhvo.Replace(0, 0, m_vhvo.Begin() + ihvoMin, ihvoLim - ihvoMin);
	vwlzi.m_vloEstimatedHeight.Replace(0, 0,
		m_vloEstimatedHeight.Begin() + ihvoMin, ihvoLim - ihvoMin);
}

/*----------------------------------------------------------------------------------------------
	Push (same as Vector.Push())
----------------------------------------------------------------------------------------------*/
void VwLazyInfo::Push(HVO hvo)
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());
	m_vhvo.Push(hvo);
	m_vloEstimatedHeight.Push(1);
}

/*----------------------------------------------------------------------------------------------
	EnsureSpace (same as Vector.EnsureSpace())
----------------------------------------------------------------------------------------------*/
void VwLazyInfo::EnsureSpace(int cItems)
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());
	m_vhvo.EnsureSpace(cItems);
	m_vloEstimatedHeight.EnsureSpace(cItems);
}

/*----------------------------------------------------------------------------------------------
	Gets the hvo of the first item
----------------------------------------------------------------------------------------------*/
HVO * VwLazyInfo::BeginHvo()
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());
	return m_vhvo.Begin();
}

/*----------------------------------------------------------------------------------------------
	Size (same as Vector.Size())
----------------------------------------------------------------------------------------------*/
int VwLazyInfo::Size()
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());
	return m_vhvo.Size();
}

/*----------------------------------------------------------------------------------------------
	Gets the hvo at the specified index
----------------------------------------------------------------------------------------------*/
HVO VwLazyInfo::GetHvo(int iHvo)
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());
	return m_vhvo[iHvo];
}

/*----------------------------------------------------------------------------------------------
	Gets the estimated height at the specified index
----------------------------------------------------------------------------------------------*/
long VwLazyInfo::GetEstimatedHeight(int iEstHeight)
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());
	return m_vloEstimatedHeight[iEstHeight];
}

/*----------------------------------------------------------------------------------------------
	Sets the estimated height for the specified index
----------------------------------------------------------------------------------------------*/
void VwLazyInfo::SetEstimatedHeight(int iEstHeight, long dypEstHeight)
{
	Assert(m_vhvo.Size() == m_vloEstimatedHeight.Size());
	m_vloEstimatedHeight[iEstHeight] = dypEstHeight;
}


//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructor/Destructor
//:>********************************************************************************************

VwLazyBox::VwLazyBox(VwPropertyStore * pzvps, HVO * prghvoItems, int chvoItems, int ihvoMin,
	IVwViewConstructor * pvc, int frag, HVO hvoContext)
:VwBox(pzvps)
{
	m_vwlziItems.Replace(0, 0, prghvoItems, chvoItems);
	m_ihvoMin = ihvoMin;
	m_qvc = pvc;
	m_frag = frag;
	m_hvoContext = hvoContext;
	m_fInLayout = false;
	// Lazy boxes should not be created in contexts where the first box will have different
	// properties from the others. ENHANCE: if we want to allow this, we have to be smart
	// about initializing the style of the VwEnv to m_qzvps->InitialStyle() to expand
	// items other than the first one that produces something.
	if(pzvps == NULL)
		ThrowHr(WarnHr(E_UNEXPECTED));

	Assert(pzvps == pzvps->InitialStyle());
}

// This constructor is used when breaking a chunk off an existing lazy box to create a new one.
VwLazyBox::VwLazyBox(VwLazyBox & lzbOrig, int ihvoMin, int chvoItems)
:VwBox(lzbOrig.m_qzvps)
{
	lzbOrig.m_vwlziItems.CopyRange(ihvoMin, ihvoMin + chvoItems, m_vwlziItems);
	m_ihvoMin = lzbOrig.m_ihvoMin + ihvoMin;
	m_qvc = lzbOrig.m_qvc;
	m_frag = lzbOrig.m_frag;
	m_hvoContext = lzbOrig.m_hvoContext;
	m_fInLayout = false;
}

// Protected default constructor used for deserialization
VwLazyBox::VwLazyBox()
{
}


VwLazyBox::~VwLazyBox()
{
	// It's an almost certain sign of a reentrant paint if this box gets deleted while it's
	// being laid out.
	Assert(!m_fInLayout);
}

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Verify that corresponding boxes in your containiner have matching 'top' values.
----------------------------------------------------------------------------------------------*/
#ifdef _DEBUG
void VerifyCorrespondences(VwPileBox * pzpbox)
{
	VwRootBox * prootb = pzpbox->Root();
	VwSynchronizer * psync = prootb->GetSynchronizer();
	if (!psync)
		return; // nothing to check.
	for (VwBox * pbox = pzpbox->FirstBox(); pbox; pbox = pbox->NextOrLazy())
	{
		VwNotifier * pnote = pzpbox->GetLowestNotifier(pbox);
		if (!pnote)
			continue; // can't sync
		if (!psync->VerifyCorrespondence(prootb, pnote->Object(), pbox))
			break;
	}
}

/*----------------------------------------------------------------------------------------------
	Write the content of this lazy box to the debug window.
----------------------------------------------------------------------------------------------*/
void VwLazyBox::TraceState(const char * pszFilename, int line, const char * pszVarName)
{
	StrAnsi sta;
	sta.FormatAppend("%s:%d:  %s=0x%x%n", pszFilename, line, pszVarName ? pszVarName : "this", this);
	sta.FormatAppend("    m_hvoContext=%d%n", m_hvoContext);
	sta.FormatAppend("    m_ihvoMin=%d%n", m_ihvoMin);
	sta.FormatAppend("    m_vwlziItems.m_vhvo.Size()=%d%n", m_vwlziItems.Size());
	for (int i = 0; i < m_vwlziItems.Size(); ++i)
		sta.FormatAppend("    [%2d]  %d%n", i, m_vwlziItems.GetHvo(i));
	sta.FormatAppend("%n");
	::OutputDebugStringA(sta.Chars());
}
#endif

/*----------------------------------------------------------------------------------------------
	Expand the lazy box to turn items from ihvoMin to ihvoLim into real boxes.
	NOTE that this may result in the destruction of the recipient lazy box, even (though rarely)
	if ihvoMin is not zero. Callers should not continue to use a pointer to the lazy box after
	calling this method.
	Note that even retaining a pointer to the previous or following box is not reliable, if
	those boxes are also lazy. In pathological cases involving borders and numbered paragraphs,
	expanding items in this lazy box could result in the expansion, even the destruction, of
	neighboring boxes. The safe thing is to retain a pointer to a neighboring non-lazy box.

	@param ihvoMin, Lim: range of object to expand, relative to the vector of objects this
		box represents. Note that these are NOT indexes into the original property; to get
		those, add m_ihvoMin.
	@param pfForcedScroll true if window was forced to scroll by change in root box size
----------------------------------------------------------------------------------------------*/
void VwLazyBox::ExpandItems(int ihvoMin, int ihvoLim, bool * pfForcedScroll)
{
#ifdef _DEBUG
	VwPileBox * pzpbox = dynamic_cast<VwPileBox *>(Container());
	VerifyCorrespondences(pzpbox);
#endif
	VwBox * pboxFirstLayout;
	VwBox * pboxLimLayout;

	int tag;
	int ipropBest;
	VwNotifier * pnoteBest = FindMyNotifier(ipropBest, tag);
	VwRootBox * prootb = Root();

	if (prootb->GetSynchronizer())
	{
		// We need to arrange for corresponding lazy items in the parallel roots to be
		// expanded.
		prootb->GetSynchronizer()->ExpandLazyItems(prootb, m_hvoContext, tag, ipropBest,
			ihvoMin + m_ihvoMin, ihvoLim + m_ihvoMin, pfForcedScroll, &pboxFirstLayout,
			&pboxLimLayout);
	}
	else
	{
		HoldGraphics hg(prootb);

		// Remember our old position. This will be helpful for making adjustments later
		Rect rcThisOld = GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
		Rect rcRootOld = prootb->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);

		// Get this BEFORE calling ExpandItems, which might destroy the lazy box.
		VwDivBox * pdboxContainer = dynamic_cast<VwDivBox *>(Container());
		ExpandItemsNoLayout(ihvoMin, ihvoLim, pnoteBest, ipropBest, tag, &pboxFirstLayout,
			&pboxLimLayout);
		AssertObjN(pboxFirstLayout);
		AssertObjN(pboxLimLayout);
		//***********************************************************************************
		// WARNING: no more virtual messages to this!! Don't use member variables!
		// *this may have been deleted.
		//***********************************************************************************
		prootb->AdjustBoxPositions(rcRootOld, pboxFirstLayout, pboxLimLayout, rcThisOld,
			pdboxContainer, pfForcedScroll, NULL, true);
	}
#ifdef _DEBUG
	VerifyCorrespondences(pzpbox);
#endif
}

/*----------------------------------------------------------------------------------------------
	Figure out which property of which notifier this lazy box represents.
----------------------------------------------------------------------------------------------*/
VwNotifier * VwLazyBox::FindMyNotifier(int & ipropBest, int & tag)
{
	VwRootBox * prootb = Root();
	ISilDataAccessPtr qsda = prootb->GetDataAccess();
	if (!qsda)
		ThrowHr(WarnHr(E_FAIL));
	VwDivBox * pdboxContainer = dynamic_cast<VwDivBox *>(Container());
	Assert(pdboxContainer);
	// Currently a box can't be lazy and also have a max lines.
	Assert(pdboxContainer->Style()->MaxLines() == INT_MAX);

	// Find the most local notifier that covers this lazy box.
	// Since lazy boxes live inside Divisions, we don't have to worry about string indexes.
	// First we get all the notifiers
	// Note: GetNotifiers goes up the chain of containers. For finding the most local one,
	// we could stop this process as soon as we find any. However, later in the process
	// when we consider which notifiers might be affected by box replacement, we need
	// the full list, so we may as well get it now.
	NotifierVec vpanote;
	pdboxContainer->GetNotifiers(this, vpanote);

	VwNotifier * pnoteBest = NULL;

	// Pick the one that is a real notifier and has a property that covers our key box. If
	// there is more than one eligible notifier, pick the one with the highest level.
	int inote;
	for (inote = 0; inote < vpanote.Size(); inote++)
	{
		VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[inote].Ptr());
		// No use in finding VwMissingNotifiers! I don't think we can, but make sure.
		if (!pnote)
			continue;
		// If we already found a better (or equal) candidate, don't search this one.
		if (pnoteBest && pnoteBest->Level() >= pnote->Level())
			continue;

		int cprop = pnote->CProps();
		for (int iprop = 0; iprop < cprop; iprop++)
		{
			// See if our box is part of this property. The property extends to the last box
			// of the notifier, unless there is a subsequent property that has a box, in
			// which case it extends to the box before that; or possibly, this property
			// has a distinct box chain which ends before the next prop, in which case
			// running out of Next() values terminates us.
			VwBox * pboxLim = pnote->GetLimOfProp(iprop);
			for (VwBox * pbox = pnote->Boxes()[iprop];
				pbox && pbox != pboxLim;
				pbox = pbox->NextOrLazy())
			{
				if (pbox == this)
				{
					tag = pnote->Tags()[iprop];
					if (tag == ktagGapInAttrs || tag == ktagNotAnAttr)
						continue; // can't regenerate using this

					// This notifier is eligible! We already know its level is better, so use it.
					pnoteBest = pnote;
					ipropBest = iprop;
					// Force double break: done with prop and notifier (but not triple...keep
					// trying other notifiers).
					iprop = cprop;
					break;
				}
			}
		}
	}
	AssertPtr(pnoteBest); // We should find one!

	tag = pnoteBest->Tags()[ipropBest];
	return pnoteBest;
}

/*----------------------------------------------------------------------------------------------
	Normally the first half of ExpandItems, but also used in special
	circumstances when expanding to get the paragraphs before a numbered one, or to consider
	whether a border should be drawn around a whole group of paragraphs.
	The first version does the FindMyNotifer, the second is passed the results.
	The first synchronizes with other root boxes if necessary; the second (which is intended
	for use from the routine that receives synchronization requests) does NOT.
	Note: we do NOT have to hide the selection, the whole idea is that this won't
	change anything visible...
	NOTE: For sync'd views, this method also does the layout and adjusting the boxes
	despite of this method name!
----------------------------------------------------------------------------------------------*/
VwBox * VwLazyBox::ExpandItemsNoLayout(int ihvoMin, int ihvoLim,
	VwBox ** ppboxFirstLayout, VwBox ** ppboxLimLayout)
{
	int tag;
	int ipropBest;
	VwNotifier * pnoteBest = FindMyNotifier(ipropBest, tag);
	VwRootBox * prootb = Root();
	if (prootb->GetSynchronizer())
	{
		// We need to arrange for corresponding lazy items in the parallel roots to be
		// expanded.
		bool fForcedScroll;
		return prootb->GetSynchronizer()->ExpandLazyItems(prootb, m_hvoContext, tag, ipropBest,
			ihvoMin + m_ihvoMin, ihvoLim + m_ihvoMin, &fForcedScroll, ppboxFirstLayout,
			ppboxLimLayout);
	}
	return ExpandItemsNoLayout(ihvoMin, ihvoLim, pnoteBest, ipropBest, tag, ppboxFirstLayout,
		ppboxLimLayout);
}

VwBox * VwLazyBox::ExpandItemsNoLayout(int ihvoMin, int ihvoLim, VwNotifier * pnoteBest,
	int ipropBest, int tag, VwBox ** ppboxFirstLayout, VwBox ** ppboxLimLayout)
{
	// this method should not be called re-entrantly while VwLazyBox::DoLayout is being called, because
	// it might expand the items while VwLazyBox::DoLayout is using them. This has happened before because
	// re-entrant message pumping in .NET (COM Interop and blocking methods can cause messages
	// to be pumped)
	Assert(!m_fInLayout);

	VwRootBox * prootb = Root();

	// During expanding lazy boxes operations like paint and PropChanged are dangerous just like PropChanged calls.
	bool fWasPropChangeInProgress = prootb->m_fIsPropChangedInProgress;
	prootb->m_fIsPropChangedInProgress = true;

	prootb->ResetSpellCheck(); // need to check the expanded material.
	HoldGraphics hg(prootb);
	BuildVec vbldrec;
	VwEnvPtr qzvwenv;
	qzvwenv.Attach(NewObj VwEnv());

	VwDivBox * pdboxContainer = dynamic_cast<VwDivBox *>(Container());
	NotifierVec vpanote;
	pdboxContainer->GetNotifiers(this, vpanote);

	VwNotifier * pnoteNextOuter;
	pnoteBest->GetBuildRecsFor(tag, vbldrec, &pnoteNextOuter);
	NotifierVec vpanoteTop;
	VwBox * pboxSeqNew;
	qzvwenv->InitRegenerate(hg.m_qvg, prootb,
		pdboxContainer, // temp. put new boxes here
		m_qzvps, m_hvoContext, &vbldrec, ipropBest);

	VwNoteProps vnp = pnoteBest->Flags()[ipropBest];
	Assert ((vnp & (~(int)kvnpEditable)) == kvnpLazyVecItems);
	try
	{
		// Once we've done the following successfully, we must be sure to call GetRegenerateInfo,
		// or we'll leave the container in a bad state.
		// Fake that we are building whatever property we were building when we made the
		// lazy box. Note how many objects in the property are before the first we will make.
		qzvwenv->OpenProp(tag, m_qvc, m_frag, vnp, ihvoMin + m_ihvoMin);

		// Make sure we have the data loaded that we need to do the display.
		CheckHr(m_qvc->LoadDataFor(qzvwenv, m_vwlziItems.BeginHvo() + ihvoMin, ihvoLim - ihvoMin,
			m_hvoContext, tag, m_frag, m_ihvoMin + ihvoMin));
		for (int ihvo = ihvoMin; ihvo < ihvoLim; ihvo++)
		{
			HVO hvoItem = m_vwlziItems.GetHvo(ihvo);
			qzvwenv->OpenObject(hvoItem);
			CheckHr(m_qvc->Display(qzvwenv, hvoItem, m_frag));
			qzvwenv->CloseObject();
		}

		qzvwenv->CloseProp();
	}
	catch (...)
	{
		// Put the box it is working on back in its standard state if possible; discard anything
		// new that has been made but not used.
		VwBox * pbox = NULL;
		NotifierVec vpanoteDummy;
		qzvwenv->GetRegenerateInfo(&pbox, vpanoteDummy, true);
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
		// Likewise clean up any notifiers made for them. Don't do this if we were
		// successful, we need the new notifiers.
		NotifierVec vpanoteNew;
		prootb->ExtractNotifiers(&vpanoteNew);
		for (int inote = 0; inote < vpanoteNew.Size(); inote++)
			vpanoteNew[inote]->Close();
		throw;
	}

	// Get the boxes generated out out of qzvwenv.
	qzvwenv->GetRegenerateInfo(&pboxSeqNew, vpanoteTop);
	// Make the top-level notifiers have the containing notifier as parent
	for (int inote2 = 0; inote2 < vpanoteTop.Size(); inote2++)
	{
		vpanoteTop[inote2]->SetParent(pnoteBest);
	}

	// Get the new notifiers, so nothing will add them to the main map until after
	// we build the structure of ones that might be affected by the replace boxes.
	NotifierVec vpanoteNew;		// list of all new notifiers.
	prootb->ExtractNotifiers(&vpanoteNew);

	// Figure out exactly what box replacements we are going to do. It depends somewhat
	// on what part of the lazy box is going away. The initialization here is appropriate
	// if the lazy is going away entirely (its whole range of hvo's got expanded).
	// We already have in vpanote the notifiers that might be affected if this box is
	// replaced.
	VwBox * pboxFirstNew = pboxSeqNew;
	VwBox * pboxLastNew = pboxFirstNew ? pboxSeqNew->EndOfChain() : NULL;
	VwLazyBox * plzbNew = NULL; // Set to new lazy box if we need one.
	VwBox * pboxFirstLayout = this;
	VwBox * pboxLimLayout = this->NextOrLazy();
	VwBox * pboxBeforeLayout = pdboxContainer->BoxBefore(this);
	VwBox * pboxRet = this; // box this method should return
	NotifierVec vpanoteDel; // dummy for ReplaceBoxes, no case should delete one.

	// Each branch of this IF should do the following, in an appropriate way:
	// 1. Decide whether to keep *this, discard it, or keep it and add another.
	// 2. Make the new lazy box and initialize it appropriately, if needed.
	// 3. Adjust the internal variables of *this, if kept, for the reduced range it represents.
	// 4. Fix the box sequence in pdboxContainer to hold the right sequence of original boxes,
	//    *this and *plzbNew if appropriate, and any new boxes we just generated.
	// 5. Update pboxFirstLayout and pboxLastLayout so that the represent the sequence of boxes
	//    that needs layout (which always includes *this if not deleted, and *plzboxNew if
	//    created).
	if (ihvoMin > 0)
	{
		// We will keep this lazy box in its present position, containing items up to ihvoMin.
		if (ihvoLim < m_vwlziItems.Size())
		{
			// We have something left at the end, too. We need a new lazy box. It takes in the hvo's
			// that are left over at the end, but is otherwise the same. It gets stuck at the end
			// of the chain of new boxes, to be inserted.
			plzbNew = NewObj VwLazyBox(*this, ihvoLim, m_vwlziItems.Size() - ihvoLim);
			if (pboxFirstNew)
				pboxLastNew->SetNext(plzbNew);
			else
				pboxFirstNew = plzbNew;
			pboxLastNew = plzbNew;
		}
		// Remove unneeded items from our vector...its start remains the same
		m_vwlziItems.Replace(ihvoMin, m_vwlziItems.Size(), NULL, 0);

		// recalculate the height of this lazy box.
		m_dysHeight = 0;
		for (int i = 0; i < m_vwlziItems.Size(); i++)
			m_dysHeight += m_vwlziItems.GetEstimatedHeight(i);

		// If there are any boxes to insert, do so.
		if (pboxFirstNew)
		{
			// First fix any notifiers that point at *this as their last box.
			// Ones that point at *this as their start are still OK.
			// (If there are no new boxes, then anything that pointed to this as its
			// last box can go on doing so, since it is still the last box in the prop
			// if it was before.)
			for (int i=0; i < vpanote.Size(); i++)
			{
				vpanote[i]->ReplaceBoxes(NULL, this, NULL,
					pboxLastNew, vpanoteDel);
			}
			pdboxContainer->RelinkBoxes(this, this->m_pboxNext, pboxFirstNew, pboxLastNew);
		}
	}
	else // ihvoMin is 0: no lazy box will remain before the new items.
	{
		if (ihvoLim < m_vwlziItems.Size())
		{
			// Expanding just at the beginning (ihvoMin == 0) but keeping something...
			// This lazy box survives but shrinks to the end.
			// (If no new boxes got generated, then nothing has changed for notifiers:
			// the lazy box remains as the first thing in any properties for which it
			// was previously the first.)
			m_vwlziItems.Replace(0, ihvoLim, NULL, 0);
			m_dysHeight = 0;
			for (int i = 0; i < m_vwlziItems.Size(); i++)
				m_dysHeight += m_vwlziItems.GetEstimatedHeight(i);

			m_ihvoMin += ihvoLim;

			if (pboxFirstNew)
			{
				pboxRet = pboxFirstNew;
				// First fix any notifiers that point at *this as their first or prop or key box.
				// Ones that have it as their last box are OK.
				for (int i=0; i < vpanote.Size(); i++)
				{
					vpanote[i]->ReplaceBoxes(this, NULL, pboxFirstNew,
						NULL, vpanoteDel);
				}
				pdboxContainer->RelinkBoxes(pboxBeforeLayout, this, pboxFirstNew, pboxLastNew);
				pboxFirstLayout = pboxFirstNew; // last can stay as this
			}
		}
		else
		{
			if (pboxFirstNew)
			{
				pboxRet = pboxFirstNew;
				// No remaining items before or after the expansion. *this can go away.
				// First fix any notifiers that point at *this in any way at all.
				// This is easy in this case because we have something to replace
				// this with.
				for (int i=0; i < vpanote.Size(); i++)
				{
					vpanote[i]->ReplaceBoxes(this, this, pboxFirstNew,
						pboxLastNew, vpanoteDel);
				}
			}
			else
			{
				// this vanished without producing anything, return the following box.
				pboxRet = NextOrLazy();
				// Now we have to fix notifiers that previously used *this, and we have nothing
				// to replace *this with.
				// That is a lot more difficult. For example,
				// - *this may have been the first thing in a property that is now empty.
				//		- detected because *this->Next() is null
				//		- detected because *this is the notifier's LastBox()
				//		- detected because *this->Next() is the first box of the next prop
				//  A further complication in this case is that a change in the notifier's
				//  key box may result.
				// - *this may have been the first thing in a property, and now this->Next()
				// starts the property.
				// - *this may have been the only box in the whole notifier, so it now
				// needs to change to a VwMissingNotifier.
				// Fortunately, only VwNotifiers (not any of the other types) point at
				// Lazy boxes. A further simplification is that we don't have to worry about
				// string indexes; a lazy box is never inside a paragraph.
				NotifierVec vpanoteDelLocal; // Ones to delete (because lazy box was only contents).
				int cnote = vpanote.Size();
				int inote; // used in two loops
				for (inote=0; inote < cnote; inote++)
				{
					VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[inote].Ptr());
					if (!pnote)
						continue; // other kinds can't be affected by our going away
					VwBox * pboxNext = NextOrLazy();
					// Look for a property that starts with this and fix it.
					// At the same time we keep track of the last non-null property
					int ipropLast = -1;
					int cprops = pnote->CProps();
					VwBox ** ppbox = pnote->Boxes();
					for (int iprop = 0; iprop < cprops; iprop++)
					{
						if (ppbox[iprop] == this)
						{
							if (pnote->LastBox() == this)
							{
								// There is nothing after this property.
								if (ipropLast < 0)
								{
									// Notifier had nothing in it but this lazy box.
									// Need to replace it with a VwMissingNotifier.
									VwMissingNotifierPtr qmnote;
									qmnote.Attach(VwMissingNotifier::Create(pnote->ObjectIndex(),
										cprops));
									PropTag * prgtagDst = qmnote->Tags();
									PropTag * prgtagSrc = pnote->Tags();
									::CopyItems(prgtagSrc, prgtagDst, cprops);
									qmnote->_KeyBox(pdboxContainer);
									qmnote->SetObject(pnote->Object());
									prootb->AddNotifier(qmnote);
									// The parent will be the notifier we are working on,
									// or its closest ancestor that will survive
									VwNotifier * pnoteParent = pnote->Parent();
									// Skip up through ones we have either already decided to delete
									// or will decide to delete. If we have already decided to delete
									// them their LastBox is already NULL.
									while (pnoteParent && pnoteParent->LastBox() == this &&
										pnoteParent->FirstBox() == this)
									{
										pnoteParent = pnoteParent->Parent();
									}
									qmnote->SetParent(pnoteParent);
									// The notifier will go away later.
									// We mustn't delete it now because it may show up in a child's
									// list of parents later in this loop.
									vpanoteDelLocal.Push(pnote);
									// Also remove it from the list of notifiers we will
									// try to fix key boxes on.
									vpanote.Delete(inote);
									inote--; cnote--;
									// Don't try to clean up this notifier any more, we will
									// delete it. Also, we use the old state of its first/last
									// box in handling any of its children we come across.
									break;
								}
								// There is something before the lazy box, so the notifier
								// will survive. Fix this property. It must become null because
								// there is nothing in the whole display of the object after
								// the lazy box (since it is the notifier's lastbox).
								ppbox[iprop] = NULL;
							}
							else
							{
								// OK, there is something after *this in the notifier.
								// We want to know whether there are any other boxes that belong
								// to this property. The boxes belonging to this property are
								// in the chain starting with *this and continuing to the end of
								// the chain, the last box of the notifier, or the box before the
								// start of the next non-null property, whichever is first.
								// We can simplify a little by figuring a box that is a "limit"
								// for the property, which is either the box (or null) following
								// lastbox, or the first box of the next property.
								VwBox * pboxLim = pnote->LastBox()->NextOrLazy();
								for (int iprop2 = iprop + 1; iprop2 < cprops; iprop2++)
								{
									if (ppbox[iprop2])
									{
										pboxLim = ppbox[iprop2];
										break;
									}
								}
								// If the next box after this is the limit box, there is nothing
								// else in the property except the disappearing lazy box, so set
								// the first box of the property to null. If there is something
								// following that is part of the same property, make it the start
								// of the property.
								ppbox[iprop] = (pboxNext == pboxLim) ? NULL : pboxNext;
							}
						} // found the lazy box
						if (ppbox[iprop])
						{
							// Found a non-null property that isn't the lazy box
							// (if it was, it has been fixed and is no longer).
							ipropLast = iprop;
						}
					}
					// At this point we have fixed all the start of prop info,
					// now (if we didn't have to delete the notifier) we need to check the
					// last box value.
					if (ipropLast >= 0 && this == pnote->LastBox())
					{
						// Have to determine a new last box. This loop is rather like
						// VwNotifier::_SetLastBox, except that when we find a previous
						// box and work forward from there, we have to stop before pboxNext,
						// rather than at the end of the chain. Also we can ignore string indexes.

						// We already found the last non-null property. The last box is it,
						// or the last in its chain, or the one in its chain before this.
						VwBox * pboxLast = pnote->Boxes()[ipropLast];
						while (pboxLast->NextOrLazy() && pboxLast->NextOrLazy() != this)
							pboxLast = pboxLast->NextOrLazy();
						pnote->SetLastBox(pboxLast);
					}
				}
				// Now we're done with the list of notifiers, we can delete any we decided to.
				prootb->DeleteNotifierVec(vpanoteDelLocal);
			}

			pboxFirstLayout = pboxFirstNew ? pboxFirstNew : pboxRet;
			VwBox * pboxPrev = pdboxContainer->BoxBefore(this);
			pdboxContainer->RelinkBoxes(pboxPrev, this->m_pboxNext, pboxFirstNew, pboxLastNew);
			// Now that we've fully fixed the notifier internals and relinked the boxes, we
			// can accurately determine what the key box of each notifier should be, and
			// change it if necessary.
			int cnote = vpanote.Size();
			for (int inote=0; inote < cnote; inote++)
			{
				VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[inote].Ptr());
				if (pnote && pnote->KeyBox() == this)
				{
					VwBox * pboxKey = pnote->FirstCoveringBox();
					prootb->ChangeNotifierKey(pnote, pboxKey);
				}
			}
			delete this;
		}
	}
	// And put any new notifiers back in...
	prootb->AddNotifiers(&vpanoteNew);

	*ppboxFirstLayout = pboxFirstLayout;
	*ppboxLimLayout = pboxLimLayout;
	Assert(vpanoteDel.Size() == 0);

	prootb->m_fIsPropChangedInProgress = fWasPropChangeInProgress;

	//prootb->AssertNotifiersValid(); // This is a error checking function related to TE-2962
	return pboxRet;
}

/*----------------------------------------------------------------------------------------------
	Lays out the items that were expanded during an ExpandItems call.
----------------------------------------------------------------------------------------------*/
void VwLazyBox::LayoutExpandedItems(VwBox * pboxFirstLayout, VwBox * pboxLimLayout,
	VwDivBox * pdboxContainer, bool fSyncTops)
{
	Assert(pdboxContainer || (!pboxFirstLayout && !pboxLimLayout));
	if (!pdboxContainer)
		return;

	AssertPtr(pdboxContainer);

	VwRootBox * prootb = pdboxContainer->Root();
	int dxsAvailWidth;
	IVwRootSitePtr qvrs;
	CheckHr(prootb->get_Site(&qvrs));
	CheckHr(qvrs->GetAvailWidth(prootb, &dxsAvailWidth));

	HoldLayoutGraphics hg(prootb);
	int dxsSrcWidth = prootb->DpiSrc().y;
	// The available width for boxes embedded in a div is the original width available to
	// the container minus the margins etc. of all the containing divs, including the root.
	for (VwGroupBox * pgbox = pdboxContainer; pgbox; pgbox = pgbox->Container())
		dxsAvailWidth -= pgbox->SurroundWidth(dxsSrcWidth);

	// This fails in the case that pboxLimLayout is a lazy box, and in the course of
	// laying out the last real box in the sequence, we expand it. NextOrLazy then
	// moves on to the substiture box; pboxLimLayout no longer exists. Hence, the loop
	// doesn't terminate until pbox is null and an address error occurs.
	// I think the right solution is to first call DoLayout for all the lazy boxes
	// and make a vector of all the non-lazy ones; then call DoLayout for the non-lazy
	// ones. (We must not put lazy boxes into the vector because laying out the other
	// boxes might destroy the lazy ones. But we do need to call DoLayout for them, as
	// their estimated height may have changed. It is safe to do so because laying out
	// a lazy box never expands itself or any other lazy box.)
	Assert(!(pboxLimLayout != NULL && pboxFirstLayout == NULL)); // Can't have lim w/o first
	// lay out the lazyboxes
	Vector<VwBox *> vpboxes;
	for (VwBox * pbox = pboxFirstLayout; pbox != pboxLimLayout; pbox = pbox->NextOrLazy())
	{
		VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pbox);
		if (plzbox)
			pbox->DoLayout(hg.m_qvg, dxsAvailWidth);
		else
			vpboxes.Push(pbox);
	}
	// lay out the real boxes
	for (int ibox = 0; ibox < vpboxes.Size(); ibox++)
		vpboxes[ibox]->DoLayout(hg.m_qvg, dxsAvailWidth, -1, fSyncTops);
}

/*----------------------------------------------------------------------------------------------
	Compute a box size based on an estimate of the size of an item.

	(Note: it would be interesting to see if the program's behavior is noticeably improved
	by averaging the estimates for several items.)
----------------------------------------------------------------------------------------------*/
void VwLazyBox::DoLayout(IVwGraphics* pvg, int dxsAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
	m_fInLayout = true;
	if (m_vwlziItems.Size())
	{
		if (m_dxsWidth == dxsAvailWidth && m_dysUniformHeightEstimate != 0)
		{
			m_dysHeight = m_dysUniformHeightEstimate * m_vwlziItems.Size();
		}
		else
		{
			int dypInch;
			pvg->get_YUnitsPerInch(&dypInch);
			int itemHeight = 0;
			m_dysHeight = 0;
			m_dysUniformHeightEstimate = 0; // set on first iteration, cleared again if not uniform
			for (int i = 0; i < m_vwlziItems.Size(); i++)
			{
				CheckHr(m_qvc->EstimateHeight(m_vwlziItems.GetHvo(i), m_frag, dxsAvailWidth, &itemHeight));
				itemHeight = MulDiv((itemHeight > 0 ? itemHeight : 1), dypInch, 72); // points to pixels.
				m_vwlziItems.SetEstimatedHeight(i, itemHeight);
				m_dysHeight += itemHeight;
				if (this == Container()->LastBox())
					Container()->_Height(this->VisibleBottom() + Container()->GapBottom(Root()->DpiSrc().y));
				if (i == 0)
					m_dysUniformHeightEstimate = itemHeight;
				else if (m_dysUniformHeightEstimate != itemHeight)
					m_dysUniformHeightEstimate = 0; // disable, not all the same
			}
		}
	}
	m_fInLayout = false;
	m_dxsWidth = dxsAvailWidth;
}

/*----------------------------------------------------------------------------------------------
	Gets the items in this lazybox to expand to cover the location from ydTopClip to
	ydBottomClip. This probably doesn't work well.
----------------------------------------------------------------------------------------------*/
void VwLazyBox::GetItemsToExpand(int ydTopClip, int ydBottomClip, Rect rcSrc, Rect rcDst,
	int * pihvoMin, int * pihvoLim)
{
	// We want to pick an ihvoMin that will make SURE that anything that remains of the
	// lazy box is above the top of the clip rectangle. The bottom of the box will be
	// the sum of its current top, ihvoMin times dysItem, and its internal vertical
	// margins. Then that gets converted. First compute a dys that is the distance
	// between the top of the lazy box and the top of the clip box. We subtract a few
	// pixels in case of rounding errors. We wind up with an underestimate of how
	// much of the lazy box is invisible above the top of the clipping rectangle.
	int ydTopBox = rcSrc.MapYTo(Top(), rcDst);
	Assert(rcDst.Height());
	int dysTopClip = max(MulDiv(ydTopClip - ydTopBox, rcSrc.Height(), rcDst.Height()) - 3, 0);
	int dysBottomClip = max(MulDiv(ydBottomClip - ydTopBox, rcSrc.Height(), rcDst.Height()), dysTopClip);

	*pihvoMin = 0;
	*pihvoLim = 1;
	int dysItemTop = 0;
	for (int i = 0; i < m_vwlziItems.Size(); i++)
	{
		int dysItemBottom = dysItemTop + m_vwlziItems.GetEstimatedHeight(i);
		if (dysItemTop <= dysTopClip)
			*pihvoMin = i;
		*pihvoLim = i + 1;
		dysItemTop = dysItemBottom;
		if (dysItemBottom > dysBottomClip)
			break;
	}

	*pihvoMin = min(m_vwlziItems.Size() - 1, *pihvoMin); // we can't return more than we have
	*pihvoLim = min(m_vwlziItems.Size(), *pihvoLim);
	// Must expand at least one item or we could get into an infinite loop.
	Assert(*pihvoLim > *pihvoMin);
}

/*----------------------------------------------------------------------------------------------
	Recompute the (estimated) size.

	It might seem that we would never need to invalidate. However, consider the following
	sequence, which occurs when pasting multiple paragraphs at end of doc:
	1. Paste code inserts first new paragraph.
	2. New paragraph is represented as lazy box, the last one in the doc.
	3. Screen is invalidated for a lazy box that is one para high.
	4. Paste inserts second new paragraph.
	5. Lazy box's size changes in Relayout.
	6. But, we don't invalidate a larger area of the screen!
----------------------------------------------------------------------------------------------*/
bool VwLazyBox::Relayout(IVwGraphics * pvg, int dxsAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi)
{
#ifdef JT_7_19_01_OkNotToInvalidate
	// If height of this is 0, we are a new box that has never been laid out,
	// and need to do a full layout.
	if (m_dysHeight != 0)
	{
		// If we are in the map, something happened to us and we need to be
		// re-laid out.
		Rect vrect;
		VwBox * pboxThis = this; // seems to be needed for calling Retrieve
		if (!pfixmap->Retrieve(pboxThis, &vrect))
			return;
	}
	// If we haven't been laid out or need a new layout, do it.
	this->DoLayout(pvg,dxsAvailWidth);
#else
	return SuperClass::Relayout(pvg, dxsAvailWidth, prootb, pfixmap, dxpAvailOnLine,
		pmmbi);
#endif
}

/*----------------------------------------------------------------------------------------------
	Designed to be called by VwBox::NextRealBox(), this virtual method answers false on all box
	classes except lazy box. On LazyBox, it expands the first thing inside itself and answers
	true. Note that this may destroy the recipient box, and may possibly produce no results
	in the process; it's possible that a box that was previously followed by a lazy box is now
	the last box in its parent. Note also that the expansion may still be a lazy box (one level
	down the object hierarchy).
----------------------------------------------------------------------------------------------*/
bool VwLazyBox::Expand()
{
	ExpandItems(0, 1);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Designed to be called by VwDivBox::ExpandFully(), this method expands everything in the
	lazy box (and therefore destroys it! Be careful about loops using the lazy box as the
	current position in the list!)
----------------------------------------------------------------------------------------------*/
void VwLazyBox::ExpandFully()
{
	ExpandItems(0, m_vwlziItems.Size());
}

/*----------------------------------------------------------------------------------------------
	Answer the box the user clicked in (or nearest to). Lazy boxes need to expand at least once
	to get a real box to return.
	WARNING: This can destroy this box!
	(xd,yd) is the point clicked in the coordinates used to draw the box.
	rcSrc and rcDst are the coordinate transformation used to draw this box, and
	the result rectangles are the coordinate transformation used to draw the chosen box.
----------------------------------------------------------------------------------------------*/
VwBox * VwLazyBox::FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
	Rect * prcSrc, Rect * prcDst)
{
	VwGroupBox * pboxContainer = Container();
	Assert(pboxContainer);
	VwBox * pboxBefore = pboxContainer->BoxBefore(this);

	// This can destroy *this!
	ExpandItems(0, 1);

	if (pboxBefore && pboxBefore->NextOrLazy())
		return pboxBefore->NextOrLazy()->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);
	else if (!pboxBefore && pboxContainer->FirstBox())
		return pboxContainer->FirstBox()->FindBoxClicked(pvg, xd, yd, rcSrc, rcDst, prcSrc, prcDst);

	return NULL;
}

/*----------------------------------------------------------------------------------------------
	A Lazy box must NOT be drawn, at least if it intersects the clip rectangle.

	For now DrawBorder does nothing at all; DrawForeground generates some warning info
	in a debug build.
----------------------------------------------------------------------------------------------*/
void VwLazyBox::DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
}

/*----------------------------------------------------------------------------------------------
	A Lazy box must NOT be drawn, at least if it intersects the clip rectangle.

	For a release build I'm going to let it go ahead and do nothing; seems least harmful.
	The other option would be to trigger an "internal error" and get a report about it...
----------------------------------------------------------------------------------------------*/
void VwLazyBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
#ifdef _DEBUG
	int ydTop = rcSrc.MapYTo(m_ysTop, rcDst);
	int ydBottom = rcSrc.MapYTo(m_ysTop + m_dysHeight, rcDst);
	int xdLeftClip, ydTopClip, xdRightClip, ydBottomClip;
	CheckHr(pvg->GetClipRect(&xdLeftClip, &ydTopClip, &xdRightClip, &ydBottomClip));

	// Assert created problems with some TE DraftView tests, so it was replaced by a displayed warning below.
	//Assert(ydBottom <= ydTopClip || ydTop >= ydBottomClip);
	// Displays a warning on the console when the lazy box is overlapping a portion of the clip rect.
	if(ydBottom > ydTopClip && ydTop < ydBottomClip)
	{
		StrAnsi stuError;
		stuError.Format("Drawing lazy box: top (%d) bottom (%d)  overlaps clip rectangle (%d) (%d)",
			ydTop, ydBottom, ydTopClip, ydBottomClip);
		Warn(stuError.Chars());

		// Draws a red border around the lazy box
		pvg->put_BackColor(kclrRed);
		pvg->DrawRectangle(0, ydTop, m_dxsWidth, ydBottom);
	}
#endif
}
//:>********************************************************************************************
//:>	LazinessIncreaser methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
LazinessIncreaser::LazinessIncreaser(VwRootBox * prootb)
{
	m_prootb = prootb;
	m_prs = prootb->Site();
}

LazinessIncreaser::~LazinessIncreaser()
{
}

// Answer true if the argument box cares about the properties of its neighbors.
// Arguably this should be a virtual method on VwBox.
bool BoxCaresAboutNeighborProps(VwBox * pbox)
{
	VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
	if (pvpbox == NULL)
		return false;
	VwPropertyStore * pzvps = pvpbox->Style();
	return pzvps->BorderLeading() || pzvps->BorderTrailing() ||
		pzvps->BorderTop() || pzvps->BorderBottom() ||
		pzvps->BackColor() != kclrTransparent ||
		pzvps->BulNumScheme() != kvbnNone;
	// REVIEW JohnT (EberhardB/TimS): why do we care about BorderLeading and BorderTrailing
	// and about BackColor?
}

/*----------------------------------------------------------------------------------------------
	Answer true if it is OK to convert the given box into a lazy box.
	This mainly means it is not visible, according to the root site's definition. (The root
	site may actually treat a larger range around what is truly visible as 'visible' to
	improve performance.)
	Also, don't convert something that is adjacent to a paragraph with borders or line
	numbering, since it might be re-expanded producing an infinite loop.
----------------------------------------------------------------------------------------------*/
bool LazinessIncreaser::OkToConvert(VwBox * pbox)
{
	if (m_boxsetKeep.IsMember(pbox))
		return false;
	HoldGraphics hg(m_prootb);
	Rect rdBounds = pbox->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
	ComBool fOk;
	CheckHr(m_prs->IsOkToMakeLazy(m_prootb, rdBounds.TopLeft().y,
		rdBounds.BottomRight().y, &fOk));
	if (!fOk)
	{
		m_boxsetKeep.Insert(pbox);
		return false;
	}

	if (BoxCaresAboutNeighborProps(pbox->NextOrLazy()))
		return false;
	if (pbox->Container() && BoxCaresAboutNeighborProps(pbox->Container()->BoxBefore(pbox)))
		return false;

	// Todo JohnT: need to verify that pbox isn't something we're forbidden to offline.
	return true;
}

/*----------------------------------------------------------------------------------------------
	The box sequence indicated by the arguments is to be kept (that is, they are not
	OkToConvert). Also, none of their children or parents may be converted.
----------------------------------------------------------------------------------------------*/
void LazinessIncreaser::KeepSequence(VwBox * pboxMinKeep, VwBox * pboxLimKeep)
{
	for (VwBox * pbox = pboxMinKeep; pbox != pboxLimKeep; pbox = pbox->NextOrLazy())
	{
		Assert(pbox); // fails if pboxLimKeep is not in the linked list starting at pbox.
		m_boxsetKeep.Insert(pbox);
		for (VwBox * pbox1 = pbox->Container(); pbox1 != NULL; pbox1 = pbox1->Container())
			m_boxsetKeep.Insert(pbox1);
		pbox->AddAllChildrenToSet(m_boxsetKeep);
	}
}

/*----------------------------------------------------------------------------------------------
	The given box is part of the display of property iprop of notifier pnote, which is a lazy
	object sequence property.

	It is either a lazy box representing some of the objects in that property,
	or it is the first box of the (expanded) display of one of the objects in the property.

	In the former case, answer true if pbox is OkToConvert, and set *ppboxNext to the next box.

	In the latter case, answer true if all the boxes in the display of that object are
	OkToConvert, and set *ppboxNext to the box following the display of the object.

	If the object is OkToConvert,
		-update the range  of objects indicated in m_ihvoMin..m_ihvoLim.
			- if ihvoMin is -1, update it to the ihvo of the first object
			- always update ihvoLim.
		- update the range of boxes indicated in m_pboxFirst..m_pboxLim
			- if ihvoMin is -1, update m_pboxFirst to the first box of the object
			- always update m_pboxLim to the last box of the object.


	(Successive use of this function ensures that pbox is always pointing to the FIRST box
	of a given child object, or to a top-level lazy box.)
----------------------------------------------------------------------------------------------*/
bool LazinessIncreaser::OkToConvertObject(VwBox * pbox, VwNotifier * pnote, int iprop,
	VwBox ** ppboxNext)
{
	VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pbox);
	// If it's a lazy box for the object of our notifier, and part of the right property,
	// it just has to be OkToConvert.
	if (plzbox && plzbox->Object() == pnote->Object()
		&& plzbox->m_frag == pnote->Fragments()[iprop]
		&& plzbox->m_qvc.Ptr() == pnote->Constructors()[iprop])
	{
		*ppboxNext = pbox->NextOrLazy(); // with no object notifier, never skip anything more.
		if (OkToConvert(pbox))
		{
			if (m_ihvoMin == -1)
			{
				m_ihvoMin = plzbox->MinObjIndex();
				m_pboxFirst = plzbox;
			}
			m_ihvoLim = plzbox->LimObjIndex();
			m_pboxLast = plzbox;
			return true;
		}
		return false;
	}
	// Otherwise we have to find the right child notifier to figure which boxes are part
	// of the display of the same child object.
	VwNotifier * pnoteChild = m_prootb->NotifierWithKeyAndParent(pbox, pnote);
	Assert(pnoteChild); // box inside object seq property MUST have a notifier showing what object.
	*ppboxNext = pnoteChild->LastCoveringBox()->NextOrLazy();
	for (VwBox * pboxT = pbox; pboxT != *ppboxNext; pboxT = pboxT->NextOrLazy())
	{
		if (!OkToConvert(pboxT))
			return false;
	}
	if (m_ihvoMin == -1)
	{
		m_ihvoMin = pnoteChild->ObjectIndex();
		m_pboxFirst = pbox;
	}
	m_ihvoLim = pnoteChild->ObjectIndex() + 1;
	m_pboxLast = pnoteChild->LastCoveringBox();
	return true;
}

#if 0


// An alternative algorithm. Assumes a new member variable, VwBox * pboxNext. This moves
// sequentially over all the boxes that could possibly be converted. The main loop
// initializes it to m_prootb->FirstBox();



/*----------------------------------------------------------------------------------------------
	Look at pboxNext, considering it as a possible target for converting.

	* If it's a lazy box, then it represents a sequence of objects from some property,
	which could possibly be combined with others.
		- it could still also be part of a higher level property, which we would
		prefer if the higher-level one is convertible.

	* If it's the first box of some object, and if that object is part of a lazy property,
	it could be combined with others.
		- in this case, it may be the start of more than one level of object.
		We prefer the highest-level one that has at least one object fully convertible.

	This routine identifies the best way, if any, to convert pbox to something lazy,
	assuming that we haven't currently found anything to convert that is allowed.

	If successful, it advances pboxNext past the convertible object that the old pboxNext
	was part of, and sets m_ihvoMin, m_ihvoLim, m_pboxFirst, m_pboxLast, m_qnote, and m_iprop
	to indicate what we can (so far) convert. The caller must then look to see whether
	more objects from this property can be converted, and if not, check for the case where
	the only thing we can convert is a single lazy box, so converting it isn't really progress.

	If unsucessful, it keeps trying? Or returns after advancing pboxNext one box?
----------------------------------------------------------------------------------------------*/
bool LazinessIncreaser::FindStartingPoint()
{
	VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pboxNext);
	// If it's a lazy box, it just has to be OkToConvert.
	if (plzbox && OkToConvert(pbox))
	{
		m_ihvoMin = plzbox->MinObjIndex();
		m_pboxFirst = plzbox;
		m_ihvoLim = plzbox->LimObjIndex();
		m_pboxLast = plzbox;
		// Fix: find the notifier and figure which prop.
	}

	NotifierMap * pmmboxqnote;
	GetNotifierMap(&pmmboxqnote);

	NotifierMap::iterator itnote;
	NotifierMap::iterator itnoteLim;
	pmmboxqnote->Retrieve(pboxNext, &itnote, &itnoteLim);
	// loop over the candidate notifiers.
	for (; itnote != itnoteLim; ++itnote)
	{
		VwNotifier * pnoteCandidate = dynamic_cast<VwNotifier *>(itnote.GetValue().Ptr());
		if (!pnoteCandidate)
			continue;
		VwNotifier pnoteParent = pnoteCandidate->Parent();
		if (!pnoteParent)
			continue;
		if ((pnoteParent->Flags()[pnoteCandidate->PropIndex()] & kvnpPropTypeMask)
			!= kvnpLazyVecItems
		{
			continue; // parent property isn't lazy
		}
		// fix: verify all boxes are OkToConvert.
		// fix: set the variables indicating what we found
	}
	// fix: consider m_qnote and its parents, till we find one where pboxNext isn't it's keybox.
	// (or run out of parents). If one of these candidates belongs to a lazy property, consider
	// whether we can convert one complete object from that property; if so, prefer it.

	// fix: update pboxNext.
}

bool LazinessIncreaser::FindSomethingToConvert()
{
	while (!FindStartingPoint())
		;
	// fix: something like this to see whether we can convert more of the property
	// we identified as a starting point.
			for (VwBox * pbox = pboxFirst; pbox != pboxLimProp; pbox = pboxNext)
			{
				// If this one isn't OK and we previously found something that is OK,
				// We are probably done searching and have found something to convert.
				if (!OkToConvertObject(pbox, m_qnote, m_iprop, &pboxNext) && m_ihvoMin >= 0)
				{
					// Probably, we've found what we want: boxes from m_pboxFirst
					// up to and including m_pboxLast can be converted into
					// a lazy box. But there's one exception: if our 'sequence'
					// is only one box and it's already a lazy box, there's no
					// progress we can make.
					if (m_pboxFirst == m_pboxLast && dynamic_cast<VwLazyBox *>(m_pboxFirst))
					{
						m_ihvoMin = -1; // still haven't found anything we can convert.
						continue; // back to FindStartingPoint
					}
					// Otherwise we found what we want,  stop.
					return true;
				}
			}
			// If we drop out of the loop, the property is either entirely OkToConvert,
			// or none of it is.
			if (m_pboxFirst == m_pboxLast && dynamic_cast<VwLazyBox *>(m_pboxFirst))
			{
				continue; // with FindStartingPoint.
			}

}
#endif

/*----------------------------------------------------------------------------------------------
	Try to find something that can be made lazy. Return true if successful. Record what can
	be converted in m_qnote, m_iprop, m_pboxFirst, m_pboxLast.
----------------------------------------------------------------------------------------------*/
bool LazinessIncreaser::FindSomethingToConvert()
{
	NotifierMap * pmmboxqnote;
	m_prootb->GetNotifierMap(&pmmboxqnote);
	NotifierMap::iterator itboxnote = pmmboxqnote->Begin();
	NotifierMap::iterator itboxnoteLim = pmmboxqnote->End();
	// loop over the candidate notifiers, processing missing ones.
	for (; itboxnote != itboxnoteLim; ++itboxnote)
	{
		VwNotifier * pnote = dynamic_cast<VwNotifier *>(itboxnote.GetValue().Ptr());
		if (!pnote)
			continue; // only interested in 'real' notifiers for offlining.
		int cprops = pnote->CProps();
		VwNoteProps * prgvnp = pnote->Flags();
		for (int iprop = 0; iprop < cprops; iprop++)
		{
			// If it isn't a property created with AddLazyVecItems, we aren't interested.
			if ((prgvnp[iprop] & kvnpPropTypeMask) != kvnpLazyVecItems)
				continue;
			VwBox * pboxFirst = pnote->Boxes()[iprop];
			// If displaying that property didn't produce anything, no point in making lazy.
			if (!pboxFirst)
				continue;
			// Get the end of the list of boxes that represents this property
			// (or null, if the property extends to the end of the linked list).
			VwBox * pboxLimProp = pnote->GetLimOfProp(iprop);
			// Find a range of these boxes that is OK to convert.
			m_ihvoMin = -1; // This indicates we haven't yet found a useful range.
			// Loop until we find a box we can convert; then keep going until we find
			// one we can't. The range between can all be offlined.
			VwBox * pboxNext;
			for (VwBox * pbox = pboxFirst; pbox != pboxLimProp; pbox = pboxNext)
			{
				// If this one isn't OK and we previously found something that is OK,
				// We are probably done searching and have found something to convert.
				if (!OkToConvertObject(pbox, pnote, iprop, &pboxNext) && m_ihvoMin >= 0)
				{
					// Probably, we've found what we want: boxes from m_pboxFirst
					// up to and including m_pboxLast can be converted into
					// a lazy box. But there's one exception: if our 'sequence'
					// is only one box and it's already a lazy box, there's no
					// progress we can make.
					if (m_pboxFirst == m_pboxLast && dynamic_cast<VwLazyBox *>(m_pboxFirst))
					{
						m_ihvoMin = -1; // still haven't found anything we can convert.
						continue;
					}
					// Otherwise we found what we want, record it and stop.
					m_qnote = pnote;
					m_iprop = iprop;
					return true;
				}
			}
			// If we didn't find anything convertible in the lazy property, go on to the
			// next prop (or notifier).
			if (m_ihvoMin == -1)
				continue;
			// If we drop out of the loop, the property is either entirely OkToConvert,
			// or none of it is.
			if (m_pboxFirst == m_pboxLast && dynamic_cast<VwLazyBox *>(m_pboxFirst))
			{
				continue; // mid loop trying other properties
			}
			// Otherwise we found what we want (the whole prop is OK), record it and stop.
			m_qnote = pnote;
			m_iprop = iprop;
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Convert everything that is OK to convert.
----------------------------------------------------------------------------------------------*/
void LazinessIncreaser::ConvertAsMuchAsPossible()
{
	while (FindSomethingToConvert())
		ConvertIt();
}

/*----------------------------------------------------------------------------------------------
	Convert the box/object sequence found by FindSomethingToConvert into a lazy box.
----------------------------------------------------------------------------------------------*/
void LazinessIncreaser::ConvertIt(bool fSynchronizing)
{
	// Build the vector of object IDs that the lazy box represents.
	HvoVec vhvoItems;
	ISilDataAccess * psda = m_prootb->GetDataAccess();
	HVO hvoContext = m_qnote->Object(); // the object the lazy box represents part of.
	PropTag tag = m_qnote->Tags()[m_iprop];
	for (int ihvo = m_ihvoMin; ihvo < m_ihvoLim; ++ihvo)
	{
		HVO hvoItem;
		CheckHr(psda->get_VecItem(hvoContext, tag, ihvo, &hvoItem));
		vhvoItems.Push(hvoItem);
	}

	// The properties we want to use as a starting point for expanding things in the
	// lazy box are those of the containing box, with non-inheritable properties reset.
	VwPropertyStorePtr qzvps;
	VwDivBox * pdboxContainer = dynamic_cast<VwDivBox *>(m_pboxFirst->Container());
	CheckHr(pdboxContainer->Style()->ComputedPropertiesForEmbedding(&qzvps));

	// Now we have enough information to actually make the lazy box.
	VwLazyBox * plzbox = NewObj VwLazyBox(qzvps, vhvoItems.Begin(), vhvoItems.Size(),
		m_ihvoMin, m_qnote->Constructors()[m_iprop], m_qnote->Fragments()[m_iprop],
		hvoContext);
	plzbox->Container(pdboxContainer);

	HoldGraphics hg(m_prootb);
	// Save the absolute position of the first box we replace. This is used later
	// to determine whether AdjustBoxPositions needs to adjust the scroll position(s).
	Rect rdOld = m_pboxFirst->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
	Rect rcRootOld = m_prootb->GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
	// Replace the boxes we are making lazy with the lazy box. This routine handles
	// relinking the box linked lists, deleting the old boxes, deleting notifiers
	// as necessary, and fixing any pointers from surviving notifiers that need to
	// switch from the deleted boxes to the new ones.
	// With the final false argument, it does NOT change layout.
	NotifierVec vpanoteNew; // lazy box has no new notifiers, but this argument is required.
	m_qnote->ReplaceBoxSeq(m_iprop,
		-1,  -1, // Note replacing at the substrings-within-paragraphs level
		m_pboxFirst, m_pboxLast, // What we're replacing
		plzbox, plzbox, // replacement is this single box
		vpanoteNew,
		hg.m_qvg, false);
	// We need to make the corresponding change of laziness in the other sync'd
	// root sites FIRST, so that OUR AdjustBoxPositions will get correct sizes
	// when it asks the other sites for the sizes of corresponding things,
	// so we correctly computer the eventual size of everyting, and send proper
	// AdjustScrollRange notifications.
	if (fSynchronizing && m_prootb->GetSynchronizer())
	{
		// We need to arrange for corresponding lazy items in the parallel roots to be
		// expanded.
		m_prootb->GetSynchronizer()->ContractLazyItems(m_prootb, m_qnote->Object(),
			m_qnote->Tags()[m_iprop], m_iprop,
			m_ihvoMin, m_ihvoLim);
	}
	bool fForcedScroll;
	VwSynchronizer * psync = fSynchronizing ? m_prootb->GetSynchronizer() : NULL;
	m_prootb->AdjustBoxPositions(rcRootOld, plzbox, plzbox->NextOrLazy(), rdOld,
		pdboxContainer, &fForcedScroll, psync, true);
	Assert(!fForcedScroll);
}

/*----------------------------------------------------------------------------------------------
	Convert the specified part of the specified property to a lazy box.
----------------------------------------------------------------------------------------------*/
void LazinessIncreaser::MakeLazy(VwNotifier * pnote, int iprop, int ihvoMin, int ihvoLim)
{
	m_qnote = pnote;
	m_iprop = iprop;
	m_ihvoMin = ihvoMin;
	m_ihvoLim = ihvoLim;
	VwBox *pbox = pnote->Boxes()[iprop];
	if (!pbox)
		return;
	int ihvo;
	for (ihvo = 0; ihvo < ihvoMin; ihvo++)
	{
		VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pbox);
		if (plzbox && plzbox->Object() == pnote->Object()
			&& plzbox->m_frag == pnote->Fragments()[iprop]
			&& plzbox->m_qvc.Ptr() == pnote->Constructors()[iprop])
		{
			pbox = pbox->NextOrLazy();
			ihvo += plzbox->CItems() - 1; // advance this many objects (loop advance code adds one more).
			continue;
		}
		VwNotifier * pnoteChild = m_prootb->NotifierWithKeyAndParent(pbox, pnote);
		if (!pnoteChild)
			break;
		// Pathologically, there may be no notifier for ihvoMin-1. In that case,
		// the notifier for the next box might be one of the ones we want.
		// If so, stop the loop.
		if (pnoteChild->ObjectIndex() >= ihvoMin)
			break;
		pbox = pnoteChild->LastCoveringBox()->NextOrLazy();
	}
	m_pboxFirst = pbox; // Found the first box of the stuff we want to convert.
	m_pboxLast = NULL;
	for ( ; ihvo < ihvoLim; ihvo++)
	{
		VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pbox);
		if (plzbox && plzbox->Object() == pnote->Object()
			&& plzbox->m_frag == pnote->Fragments()[iprop]
			&& plzbox->m_qvc.Ptr() == pnote->Constructors()[iprop])
		{
			m_pboxLast = pbox;
			pbox = pbox->NextOrLazy();
			ihvo += plzbox->CItems() - 1; // advance this many objects (loop advance code adds one more).
			continue;
		}
		VwNotifier * pnoteChild = m_prootb->NotifierWithKeyAndParent(pbox, pnote);
		if (!pnoteChild)
			break;
		// Pathologically, there may be no notifier for ihvoMin-1. In that case,
		// the notifier for the next box might be one of the ones we want.
		// If so, stop the loop.
		if (pnoteChild->ObjectIndex() >= ihvoLim)
			break;
		// This keeps getting overwritten until we get to what is really the last box
		// of the last non-empty object display in our range.
		m_pboxLast = pnoteChild->LastCoveringBox();
		pbox = m_pboxLast->NextOrLazy();
	}
	if (m_pboxLast == NULL)
		return; // no actual boxes in the range given.

	ConvertIt(false);
}
