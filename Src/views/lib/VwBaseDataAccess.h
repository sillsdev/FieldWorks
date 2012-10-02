/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CmDataAccess.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains class declarations for the following class:
		VwBaseDataAccess
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwBaseDataAcess_INCLUDED
#define VwBaseDataAcess_INCLUDED


/*----------------------------------------------------------------------------------------------
	A base class with most methods doing nothing or returning E_NOTIMPL.

	Subclassing this saves implementing interface methods that are not needed, and provides a
	standard implementation of QueryInterface.

	@h3{Hungarian: bda}
----------------------------------------------------------------------------------------------*/
class VwBaseDataAccess : public ISilDataAccess, public IStructuredTextDataAccess
{
public:
	//:>****************************************************************************************
	//:>    IUnknown Methods
	//:>****************************************************************************************
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}


	//:> ISilDataAccess methods

	//:>****************************************************************************************
	//:>	Methods used to retrieve object REFERENCE information.
	//:>****************************************************************************************
	STDMETHOD(get_ObjectProp)(HVO hvo, PropTag tag, HVO * phvo);
	STDMETHOD(get_VecSize)(HVO hvo, PropTag tag, int * pchvo);
	STDMETHOD(get_VecItem)(HVO hvo, PropTag tag, int index, HVO * phvo);


	//:>****************************************************************************************
	//:>	Methods used to retrieve object PROPERTY information (except references).
	//:>****************************************************************************************
	STDMETHOD(BinaryPropRgb)(HVO obj, PropTag tag, byte * prgch, int cbMax, int * pcb);
	STDMETHOD(get_GuidProp)(HVO hvo, PropTag tag, GUID * puid);
	STDMETHOD(get_IntProp)(HVO hvo, PropTag tag, int * pn);
	STDMETHOD(get_Int64Prop)(HVO hvo, PropTag tag, int64 * plln);
	STDMETHOD(get_BooleanProp)(HVO hvo, PropTag tag, ComBool * pn);
	STDMETHOD(get_MultiStringAlt)(HVO hvo, PropTag tag, int ws, ITsString ** pptss);
	STDMETHOD(get_MultiStringProp)(HVO hvo, PropTag tag, ITsMultiString ** pptms);
	STDMETHOD(get_Prop)(HVO hvo, PropTag tag, VARIANT * pvar);
	STDMETHOD(get_StringProp)(HVO hvo, PropTag tag, ITsString ** pptss);
	STDMETHOD(get_TimeProp)(HVO hvo, PropTag tag, int64 * ptim);
	STDMETHOD(get_UnicodeProp)(HVO obj, PropTag tag, BSTR * pbstr);
	STDMETHOD(put_UnicodeProp)(HVO obj, PropTag tag, BSTR bstr);
	STDMETHOD(UnicodePropRgch)(HVO obj, PropTag tag, OLECHAR * prgch, int cchMax, int * pcch);
	STDMETHOD(get_UnknownProp)(HVO hvo, PropTag tag, REFIID iid, void ** ppunk);


	//:>****************************************************************************************
	//:>	Methods to manage the undo/redo mechanism.
	//:>****************************************************************************************
	STDMETHOD(BeginUndoTask)(BSTR bstrUndo, BSTR bstrRedo);
	STDMETHOD(EndUndoTask)();
	STDMETHOD(ContinueUndoTask)();
	STDMETHOD(EndOuterUndoTask)();
	STDMETHOD(BreakUndoTask)(BSTR bstrUndo, BSTR bstrRedo);
	STDMETHOD(BeginNonUndoableTask)();
	STDMETHOD(EndNonUndoableTask)();
	STDMETHOD(Rollback)();
	STDMETHOD(GetActionHandler)(IActionHandler ** ppacth);
	STDMETHOD(SetActionHandler)(IActionHandler * pacth);


	//:>****************************************************************************************
	//:>	Methods used to create new objects or a combination of both creating and deleting
	//:>	objects (in the case of MoveOwnSeq).  These are the only methods that actually
	//:>	change the OWNERSHIP RELATIONSHIPS of objects.
	//:>****************************************************************************************
	STDMETHOD(InsertNew)(HVO hvoObj, PropTag tag, int ihvo, int chvo, IVwStylesheet * pss);
	STDMETHOD(MakeNewObject)(int clid, HVO hvoOwner, PropTag tag, int ord, HVO * phvoNew);
	STDMETHOD(MoveOwnSeq)(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart, int ihvoEnd,
		HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart);


	//:>****************************************************************************************
	//:>	SetObjProp changes the value of an atomic REFERENCES and Replace changes the values
	//:>	of collection/sequence references.
	//:>****************************************************************************************
	STDMETHOD(Replace)(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
		int * prghvo, int chvo);
	STDMETHOD(SetObjProp)(HVO hvo, PropTag tag, HVO hvoObj);


	//:>****************************************************************************************
	//:>	Methods used to change object PROPERTY information (except reference properties).
	//:>****************************************************************************************
	STDMETHOD(SetBinary)(HVO hvo, PropTag tag, byte * prgb, int cb);
	STDMETHOD(SetGuid)(HVO hvo, PropTag tag, GUID uid);
	STDMETHOD(SetInt)(HVO hvo, PropTag tag, int n);
	STDMETHOD(SetInt64)(HVO hvo, PropTag tag, int64 lln);
	STDMETHOD(SetBoolean)(HVO hvo, PropTag tag, ComBool n);
	STDMETHOD(SetMultiStringAlt)(HVO hvo, PropTag tag, int ws, ITsString * ptss);
	STDMETHOD(SetString)(HVO hvo, PropTag tag, ITsString * ptss);
	STDMETHOD(SetTime)(HVO hvo, PropTag tag, int64 tim);
	STDMETHOD(SetUnicode)(HVO hvo, PropTag tag, OLECHAR * prgch, int cch);
	STDMETHOD(SetUnknown)(HVO hvoObj, PropTag tag, IUnknown * punk);


	//:>****************************************************************************************
	//:>	Methods used for sending notifications to subscribers when a designated object
	//:>	property value (in the cache) has changed.
	//:>****************************************************************************************
	STDMETHOD(AddNotification)(IVwNotifyChange * pnchng);
	STDMETHOD(PropChanged)(IVwNotifyChange * pnchng, int pct, HVO hvo, int tag, int ivMin,
		int cvIns, int cvDel);
	STDMETHOD(RemoveNotification)(IVwNotifyChange * pnchng);


	//:>****************************************************************************************
	//:>	Methods to set and retrieve extra info for collection/sequence references.
	//:>****************************************************************************************
	STDMETHOD(InsertRelExtra)(HVO hvoSrc, PropTag tag, int ihvo, HVO hvoDst, BSTR bstrExtra);
	STDMETHOD(UpdateRelExtra)(HVO hvoSrc, PropTag tag, int ihvo, BSTR bstrExtra);
	STDMETHOD(GetRelExtra)(HVO hvoSrc, PropTag tag, int ihvo, BSTR * pbstrExtra);

	//:>****************************************************************************************
	//:>	Other methods
	//:>****************************************************************************************
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** ppencf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(get_WritingSystemsOfInterest)(int cwsMax, int * pws, int * pcws);

	//:>****************************************************************************************
	//:>	A method that indicates if the cache has changed since it was first loaded by means
	//:>	of Set* methods.  Basically what this means is that client code has called one
	//:>	of the property modification methods (eg. "Set" methods, MakeNewObject, DeleteObject*,
	//:>	MoveOwnSeq, or Replace methods).
	//:>****************************************************************************************
	STDMETHOD(IsDirty)(ComBool * pf);
	STDMETHOD(GetObjIndex)(HVO hvoOwn, int flid, HVO hvo, int * ihvo);
	STDMETHOD(GetOutlineNumber)(HVO hvo, int flid, ComBool fFinPer, BSTR * pbstr);

protected:
	VwBaseDataAccess();
	virtual ~VwBaseDataAccess();

	long m_cref;
};

DEFINE_COM_PTR(VwBaseDataAccess);
#endif // VwBaseDataAcess_INCLUDED
