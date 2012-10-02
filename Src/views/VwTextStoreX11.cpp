/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2009 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwTextStoreX11.cpp
Responsibility:
Last reviewed: Not yet.

Description:
	Defines the class VwTextStore which is implemented in terms of IIMEKeyboardSwitcher
	This is only used on Linux.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"

#pragma hdrstop
// any other headers (not precompiled)

#ifndef WIN32
// GUID attachments
template<> const GUID __uuidof(VwTextStore)("52049bc0-9493-11dd-ad8b-0800200c9a66");
#endif //!WIN32

static const GUID KeyboardSwitcher_guid("4ED1E8bC-DAdE-11DE-B350-0019DBf4566E");

#define CLSID_KeyboardSwitcher KeyboardSwitcher_guid

using namespace std;

#undef THIS_FILE
DEFINE_THIS_FILE

#undef ENABLE_TSF
#define ENABLE_TSF

#undef TRACING_TSF
//#define TRACING_TSF

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

static DummyFactory g_factDummy(
	_T("SIL.Views.VwTextStore"));

// Global count of instances of VwTextStore objects.
static long g_ctxs = 0;
static StrUni s_stuParaBreak;
static int s_cchParaBreak;

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwTextStore::VwTextStore(VwRootBox * prootb)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	AssertPtr(prootb);

	m_focused = false;

	m_qrootb = prootb;
	m_qkbs.CreateInstance(CLSID_KeyboardSwitcher);
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwTextStore::~VwTextStore()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Standard COM function.

	@param riid - reference to the desired interface GUID.
	@param ppv - address that receives the interface pointer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (&riid == &CLSID_VwTextStore)
		*ppv = static_cast<VwTextStore *>(this);
	else
	{
		StrAnsi staError;
		staError.Format(
			"VwTextStore::QueryInterface could not provide interface %g; compare %g",
			&riid, &IID_IServiceProvider);
		// We might want this when doing further TSF testing, but otherwise
		// it causes unnecessary concerns to those watching warnings.
		Warn(staError.Chars());
		return E_NOINTERFACE;
	}

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	Other Methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Retrieve the start and end boxes of the selection. Return true if there is a text selection.
	Returns a value for both boxes, even if they are the same.
----------------------------------------------------------------------------------------------*/
VwTextSelection * VwTextStore::GetStartAndEndBoxes(VwParagraphBox ** ppvpboxStart,
	VwParagraphBox ** ppvpboxEnd, bool * pfEndBeforeAnchor)
{
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Compute the length of the current text.
----------------------------------------------------------------------------------------------*/
int VwTextStore::TextLength()
{
		return 0;
}

/*----------------------------------------------------------------------------------------------
	The document changed. (Ideally we'd like to know where, but at least let the service know.)
----------------------------------------------------------------------------------------------*/
void VwTextStore::OnDocChange()
{

}


/*----------------------------------------------------------------------------------------------
	The selection changed.

	@param nHow Flag how the selection changed: ksctSamePara, ksctDiffPara, etc.
----------------------------------------------------------------------------------------------*/
void VwTextStore::OnSelChange(int nHow)
{
	if (!m_focused)
		return;

	GetCurrentWritingSystem();

	if (m_qws.Ptr() != NULL)
	{
		SmartBstr sbstrKeymanKbd;
		CheckHr(m_qws->get_Keyboard(&sbstrKeymanKbd));
		m_qkbs->put_IMEKeyboard(sbstrKeymanKbd);
	}
}

void VwTextStore::OnLayoutChange()
{

}

/*----------------------------------------------------------------------------------------------
	Set the Text Service focus to our root box.
----------------------------------------------------------------------------------------------*/
void VwTextStore::SetFocus()
{
	m_focused = true;

	GetCurrentWritingSystem();

	if (m_qws.Ptr() != NULL)
	{
		SmartBstr sbstrKeymanKbd;
		CheckHr(m_qws->get_Keyboard(&sbstrKeymanKbd));
		m_qkbs->put_IMEKeyboard(sbstrKeymanKbd);
	}
}

/*----------------------------------------------------------------------------------------------
	Create and initialize the document manager.
	This can be called more than once. It releases the old document manager if one
	already exists.
----------------------------------------------------------------------------------------------*/
void VwTextStore::Init()
{

}

/*----------------------------------------------------------------------------------------------
	Release the interfaces installed by the constructor or by Init.
----------------------------------------------------------------------------------------------*/
void VwTextStore::Close()
{
	if (m_qkbs)
	{
		m_qkbs->Close();
		m_qkbs.Clear();
	}

	m_qrootb.Clear();
	m_qws.Clear();
}

/*----------------------------------------------------------------------------------------------
	Convert the acp (TSF offset) value to an ich (Views code paragraph offset), and select the
	corresponding paragraph box that goes with it.
----------------------------------------------------------------------------------------------*/
int VwTextStore::ComputeBoxAndOffset(int acp, VwParagraphBox * pvpboxFirst,
	VwParagraphBox * pvpboxLast, VwParagraphBox ** ppvpboxOut)
{
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Create a new text selection based on the input document character offsets.
	If there's no current selection we can't do it; return a null pointer.
----------------------------------------------------------------------------------------------*/
void VwTextStore::CreateNewSelection(COMINT32 acpStart, COMINT32 acpEnd, bool fEndBeforeAnchor,
	VwTextSelection ** pptsel)
{

}

void VwTextStore::AddToKeepList(LazinessIncreaser *pli)
{
	if (m_pvpboxCurrent)
		pli->KeepSequence(m_pvpboxCurrent, m_pvpboxCurrent->NextOrLazy());
}

// The specified box is being deleted. If somehow we are stil pointing at it
// (this can happen, for one example, during a replace all where NoteDependencies
// cause large-scale regeneration), clear the pointers to a safe, neutral state.
void VwTextStore::ClearPointersTo(VwParagraphBox * pvpbox)
{
	if (m_pvpboxCurrent == pvpbox)
		m_pvpboxCurrent = NULL;
}

void VwTextStore::DoDisplayAttrs()
{

}

/*----------------------------------------------------------------------------------------------
	Terminate all compositions, and refresh the display attributes.
----------------------------------------------------------------------------------------------*/
void VwTextStore::TerminateAllCompositions(void)
{

}


/*----------------------------------------------------------------------------------------------
	Send appropriate mouse event notifications, if they have been requested.  A "Mouse Down"
	event terminates all open compositions if it is not handled by the sink (unless, of course,
	the sink does not exist).
----------------------------------------------------------------------------------------------*/
bool VwTextStore::MouseEvent(int xd, int yd, RECT rcSrc1, RECT rcDst1, VwMouseEvent me)
{
	return false;
}
/*----------------------------------------------------------------------------------------------
	Informs that the focus has been lost
----------------------------------------------------------------------------------------------*/
void VwTextStore::OnLoseFocus()
{
	m_focused = false;
}

void VwTextStore::GetCurrentWritingSystem()
{
	m_qws.Clear();
	VwTextSelection * psel = dynamic_cast<VwTextSelection *>(m_qrootb->Selection());
	if (psel && psel->IsValid())
	{
		VwParagraphBox * pvpboxStart = psel->AnchorBox();
		int ichStartRen;
		ComBool fEndBeforeAnchor;
		CheckHr(psel->get_EndBeforeAnchor(&fEndBeforeAnchor));
		if (psel->IsInsertionPoint() || !fEndBeforeAnchor)
			ichStartRen = pvpboxStart->Source()->LogToRen(psel->AnchorOffset());
		else
		{
			if (psel->EndBox()) // null if same as anchor
				pvpboxStart = psel->EndBox();
			ichStartRen = pvpboxStart->Source()->LogToRen(psel->EndOffset());
		}
		int ichMinDummy, ichLimDummy;
		LgCharRenderProps chrp;
		CheckHr(pvpboxStart->Source()->GetCharProps(ichStartRen, &chrp, &ichMinDummy, &ichLimDummy));
		if (!chrp.ws)
		{
			// If this wasn't an editable selection (might have been a rectangle or something),
			// we don't really care about the current writing system, so just ignore it.
			ComBool fEditable;
			CheckHr(psel->get_IsEditable(&fEditable));
			if (fEditable)
				ThrowHr(E_UNEXPECTED);
			return;
		}

		ISilDataAccessPtr qsdaT;
		CheckHr(m_qrootb->get_DataAccess(&qsdaT));
		ILgWritingSystemFactoryPtr qwsf;
		CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
		CheckHr(qwsf->get_EngineOrNull(chrp.ws, &m_qws));
	}
}


// Explicit instantiation
#include "Vector_i.cpp"
