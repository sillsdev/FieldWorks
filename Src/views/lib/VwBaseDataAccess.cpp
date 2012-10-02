/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwBaseDataAccess.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following class:
		VwBaseDataAccess
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact(_T("SIL.Views.lib.VwBaseDataAccess"));


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwBaseDataAccess::VwBaseDataAccess()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwBaseDataAccess::~VwBaseDataAccess()
{
	ModuleEntry::ModuleRelease();
}




//:>********************************************************************************************
//:>    IUnknown Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ISilDataAccess *>(this));
	else if (riid == IID_ISilDataAccess)
		*ppv = static_cast<ISilDataAccess *>(this);
	else if (riid == IID_IStructuredTextDataAccess)
		*ppv = static_cast<IStructuredTextDataAccess *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}




//:>********************************************************************************************
//:>	Methods used to retrieve object REFERENCE information.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ObjectProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_ObjectProp(HVO hvo, PropTag tag, HVO * phvo)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#VecItem}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_VecSize(HVO hvo, PropTag tag, int * pchvo)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#VecSize}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_VecItem(HVO hvo, PropTag tag, int index, HVO * phvo)
{
	Assert(false);
	return E_NOTIMPL;
}




//:>********************************************************************************************
//:>	Methods used to retrieve object PROPERTY information (except references).
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BinaryPropRgb}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::BinaryPropRgb(HVO obj, PropTag tag, byte * prgb, int cbMax,
	int * pcb)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GuidProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_GuidProp(HVO hvo, PropTag tag, GUID * puid)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#IntProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_IntProp(HVO hvo, PropTag tag, int * pn)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Int64Prop}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_Int64Prop(HVO hvo, PropTag tag, int64 * plln)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BooleanProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_BooleanProp(HVO hvo, PropTag tag, ComBool * pn)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#MultiStringAlt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_MultiStringAlt(HVO hvo, PropTag tag, int ws, ITsString ** pptss)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#MultiStringProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_MultiStringProp(HVO hvo, PropTag tag,
	ITsMultiString ** pptms)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Prop}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_Prop(HVO hvo, PropTag tag, VARIANT * pvar)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#StringProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_StringProp(HVO hvo, PropTag tag, ITsString ** pptss)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#TimeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_TimeProp(HVO hvo, PropTag tag, int64 * ptim)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#UnicodeProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_UnicodeProp(HVO obj, PropTag tag, BSTR * pbstr)
{
	Assert(false);
	return E_NOTIMPL;
}

STDMETHODIMP VwBaseDataAccess::put_UnicodeProp(HVO obj, PropTag tag, BSTR bstr)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#UnicodePropRgch}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::UnicodePropRgch(HVO obj, PropTag tag, OLECHAR * prgch, int cchMax,
	int * pcch)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Prop}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_UnknownProp(HVO hvo, PropTag tag, REFIID iid, void ** ppunk)
{
	Assert(false);
	return E_NOTIMPL;
}


//:>********************************************************************************************
//:>	Methods to manage the undo/redo mechanism.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BeginUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::BeginUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::EndUndoTask()
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#ContinueUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::ContinueUndoTask()
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndOuterUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::EndOuterUndoTask()
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BreakUndoTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::BreakUndoTask(BSTR bstrUndo, BSTR bstrRedo)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#BeginNonUndoableTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::BeginNonUndoableTask()
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#EndNonUndoableTask}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::EndNonUndoableTask()
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Rollback}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::Rollback()
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetActionHandler}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::GetActionHandler(IActionHandler ** ppacth)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppacth);

	*ppacth = NULL;
	return S_OK;
	END_COM_METHOD(g_fact, IID_ISilDataAccess);
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetActionHandler}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetActionHandler(IActionHandler * pacth)
{
	return S_OK;
}


//:>********************************************************************************************
//:>	Methods used to create new objects or a combination of both creating and deleting
//:>	objects (in the case of MoveOwnSeq).  These are the only methods that actually change
//:>	the OWNERSHIP RELATIONSHIPS of objects.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#InsertNew}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::InsertNew(HVO hvoObj, PropTag tag, int ihvo, int chvo,
	IVwStylesheet * pss)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#MakeNewObject}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::MakeNewObject(int clid, HVO hvoOwner, PropTag tag, int ord,
	HVO * phvoNew)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#MoveOwnSeq}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::MoveOwnSeq(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart,
	int ihvoEnd, HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart)
{
	Assert(false);
	return E_NOTIMPL;
}




//:>********************************************************************************************
//:>	SetObjProp changes the value of an atomic REFERENCES and Replace changes the values of
//:>	collection/sequence references.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#Replace}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::Replace(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
	int * prghvo, int chvo)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetObjProp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetObjProp(HVO hvo, PropTag tag, HVO hvoObj)
{
	Assert(false);
	return E_NOTIMPL;
}




//:>********************************************************************************************
//:>	Methods used to change object PROPERTY information (except reference properties).
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetBinary}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetBinary(HVO hvo, PropTag tag, byte * prgb, int cb)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetGuid}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetGuid(HVO hvo, PropTag tag, GUID uid)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetInt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetInt(HVO hvo, PropTag tag, int n)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetInt64}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetInt64(HVO hvo, PropTag tag, int64 lln)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetBoolean}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetBoolean(HVO hvo, PropTag tag, ComBool n)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetMultiStringAlt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetMultiStringAlt(HVO hvo, PropTag tag, int ws,
	ITsString * ptss)
{
	Assert(false); return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetString}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetString(HVO hvo, PropTag tag, ITsString * ptss)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetTime}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetTime(HVO hvo, PropTag tag, int64 tim)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetUnicode}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetUnicode(HVO hvo, PropTag tag, OLECHAR * prgch, int cch)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#SetUnknown}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::SetUnknown(HVO hvoObj, PropTag tag, IUnknown * punk)
{
	Assert(false);
	return E_NOTIMPL;
}


//:>********************************************************************************************
//:>	Methods used for sending notifications to subscribers when a designated object property
//:>	value (in the cache) has changed.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#AddNotification}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::AddNotification(IVwNotifyChange * pnchng)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#PropChanged}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::PropChanged(IVwNotifyChange * pnchng, int pct, HVO hvo, int tag,
	int ivMin, int cvIns, int cvDel)
{
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#RemoveNotification}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::RemoveNotification(IVwNotifyChange * pnchng)
{
	Assert(false);
	return E_NOTIMPL;
}

//:>****************************************************************************************
//:>	Methods to set and retrieve extra info for collection/sequence references.
//:>****************************************************************************************
STDMETHODIMP VwBaseDataAccess::InsertRelExtra(HVO hvoSrc, PropTag tag, int ihvo, HVO hvoDst,
	BSTR bstrExtra)
{
	Assert(false);
	return E_NOTIMPL;
}

STDMETHODIMP VwBaseDataAccess::UpdateRelExtra(HVO hvoSrc, PropTag tag, int ihvo, BSTR bstrExtra)
{
	Assert(false);
	return E_NOTIMPL;
}

STDMETHODIMP VwBaseDataAccess::GetRelExtra(HVO hvoSrc, PropTag tag, int ihvo, BSTR * pbstrExtra)
{
	Assert(false);
	return E_NOTIMPL;
}

//:>********************************************************************************************
//:>	Other methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return the writing system factory for this database (or the registry, as the case may be).

	@param ppwsf Address of the pointer for returning the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory for this database (or the registry, as the case may be).

	@param pwsf Pointer to the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#get_WritingSystemsOfInterest}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::get_WritingSystemsOfInterest(int cwsMax, int * pws, int * pcws)
{
	Assert(false);
	return E_NOTIMPL;
}

//:>********************************************************************************************
//:>	A method that indicates if the cache has changed since it was first loaded by means of
//:>	Set* methods.  Basically what this means is that client code has called one of the
//:>	property modification methods (eg. "Set" methods, MakeNewObject, DeleteObject*,
//:>	MoveOwnSeq, or Replace methods).
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#IsDirty}
---------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::IsDirty(ComBool * pf)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetObjIndex}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::GetObjIndex(HVO hvoOwn, int flid, HVO hvo, int * pihvo)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	${ISilDataAccess#GetOutlineNumber}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseDataAccess::GetOutlineNumber(HVO hvo, int flid, ComBool fFinPer, BSTR * pbstr)
{
	Assert(false);
	return E_NOTIMPL;
}
