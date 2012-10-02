/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UndoAction.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	This file contains class declarations for:
	* VwUndoAction and its subclasses
	* VwUndoDa, a kind of data-access object that knows how to record changes
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UndoAction_INCLUDED
#define UndoAction_INCLUDED

class VwUndoAction;
class AfStylesheet;
typedef ComSmartPtr<AfStylesheet> AfStylesheetPtr;


/*----------------------------------------------------------------------------------------------
	VwUndoDa records each change to itself in an ActionHandler. UndoActions in the
	ActionHandler then know how to undo themselves. SQL is not used.

	Hungarian: uda
----------------------------------------------------------------------------------------------*/
class VwUndoDa : public VwCacheDa
{
	typedef VwCacheDa SuperClass;

	friend class VwUndoAction;
	friend class VwUndoDeleteAction;
	friend class VwUndoInsertAction;
	friend class VwUndoSetStringAction;
	friend class VwUndoMakeNewObjectAction;

public:
	VwUndoDa();
	~VwUndoDa();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	//	Methods of ISilDataAccess

	STDMETHOD(BeginUndoTask)(BSTR bstrUndo, BSTR bstrRedo);
	STDMETHOD(EndUndoTask)();
	STDMETHOD(ContinueUndoTask)();
	STDMETHOD(EndOuterUndoTask)();
	STDMETHOD(BreakUndoTask)(BSTR bstrUndo, BSTR bstrRedo);
	STDMETHOD(Rollback)();
	STDMETHOD(GetActionHandler)(IActionHandler ** ppacth);
	STDMETHOD(SetActionHandler)(IActionHandler * pacth);

	//	Methods overridden to create undo-actions:
	STDMETHOD(DeleteObj)(HVO hvoObj);
	STDMETHOD(DeleteObjOwner)(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo);
	STDMETHOD(InsertNew)(HVO hvoObj, PropTag tag, int ihvo, int chvo, IVwStylesheet * pss);
	STDMETHOD(MakeNewObject)(int clid, HVO hvoOwner, PropTag tag, int ord, HVO * phvoNew);
	STDMETHOD(MoveOwnSeq)(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart, int ihvoEnd,
		HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart);

	STDMETHOD(Replace)(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
		HVO * prghvo, int chvo);
	STDMETHOD(SetObjProp)(HVO hvo, PropTag tag, HVO hvoObj);

	STDMETHOD(SetBinary)(HVO hvo, PropTag tag, byte * prgb, int cb);
	STDMETHOD(SetGuid)(HVO hvo, PropTag tag, GUID uid);
	STDMETHOD(SetInt)(HVO hvo, PropTag tag, int n);
	STDMETHOD(SetInt64)(HVO hvo, PropTag tag, int64 lln);
	STDMETHOD(SetMultiStringAlt)(HVO hvo, PropTag tag, int ws, ITsString * ptss);
	STDMETHOD(SetString)(HVO hvo, PropTag tag, ITsString * ptss);
	STDMETHOD(SetTime)(HVO hvo, PropTag tag, int64 tim);
	STDMETHOD(SetUnicode)(HVO hvo, PropTag tag, OLECHAR * prgch, int cch);
	STDMETHOD(SetUnknown)(HVO hvoObj, PropTag tag, IUnknown * punk);

	// Superclass methods, to avoid undo/redo mechanism:
	HRESULT SuperDeleteObj(HVO hvoObj)
	{
		return SuperClass::DeleteObj(hvoObj);
	}
	HRESULT SuperDeleteObjOwner(HVO hvoOwner, HVO hvoObj, PropTag tag, int ihvo)
	{
		return SuperClass::DeleteObjOwner(hvoOwner, hvoObj, tag, ihvo);
	}
	HRESULT SuperInsertNew(HVO hvoObj, PropTag tag, int ihvo, int chvo, IVwStylesheet * pss)
	{
		return SuperClass::InsertNew(hvoObj, tag, ihvo, chvo, pss);
	}
	HRESULT SuperMakeNewObject(int clid, HVO hvoOwner, PropTag tag, int ord, HVO * phvoNew)
	{
		return SuperClass::MakeNewObject(clid, hvoOwner, tag, ord, phvoNew);
	}
	HRESULT SuperMoveOwnSeq(HVO hvoSrcOwner, PropTag tagSrc, int ihvoStart, int ihvoEnd,
		HVO hvoDstOwner, PropTag tagDst, int ihvoDstStart)
	{
		return SuperClass::MoveOwnSeq(hvoSrcOwner, tagSrc, ihvoStart, ihvoEnd,
			hvoDstOwner, tagDst, ihvoDstStart);
	}
	HRESULT SuperReplace(HVO hvoObj, PropTag tag, int ihvoMin, int ihvoLim,
		HVO * prghvo, int chvo)
	{
		return SuperClass::Replace(hvoObj, tag, ihvoMin, ihvoLim, prghvo, chvo);
	}
	HRESULT SuperSetObjProp(HVO hvo, PropTag tag, HVO hvoObj)
	{
		return SuperClass::SetObjProp(hvo, tag, hvoObj);
	}
	HRESULT SuperSetBinary(HVO hvo, PropTag tag, byte * prgb, int cb)
	{
		return SuperClass::SetBinary(hvo, tag, prgb, cb);
	}
	HRESULT SuperSetGuid(HVO hvo, PropTag tag, GUID uid)
	{
		return SuperClass::SetGuid(hvo, tag, uid);
	}
	HRESULT SuperSetInt(HVO hvo, PropTag tag, int n)
	{
		return SuperClass::SetInt(hvo, tag, n);
	}
	HRESULT SuperSetInt64(HVO hvo, PropTag tag, int64 lln)
	{
		return SuperClass::SetInt64(hvo, tag, lln);
	}
	HRESULT SuperSetMultiStringAlt(HVO hvo, PropTag tag, int ws, ITsString * ptss)
	{
		return SuperClass::SetMultiStringAlt(hvo, tag, ws, ptss);
	}
	HRESULT SuperSetString(HVO hvo, PropTag tag, ITsString * ptss)
	{
		return SuperClass::SetString(hvo, tag, ptss);
	}
	HRESULT SuperSetTime(HVO hvo, PropTag tag, int64 tim)
	{
		return SuperClass::SetTime(hvo, tag, tim);
	}
	HRESULT SuperSetUnicode(HVO hvo, PropTag tag, OLECHAR * prgch, int cch)
	{
		return SuperClass::SetUnicode(hvo, tag, prgch, cch);
	}
	HRESULT SuperSetUnknown(HVO hvoObj, PropTag tag, IUnknown * punk)
	{
		return SuperClass::SetUnknown(hvoObj, tag, punk);
	}

protected:
	// member variables:
	IActionHandlerPtr m_qacth;

	// private methods:
	void RecordUndoAction(VwUndoAction * puact);
};
DEFINE_COM_PTR(VwUndoDa);


/*----------------------------------------------------------------------------------------------
	UndoAction is an abstract class. Each subclass knows how to undo a specific kind of
	change to an ISilDataAccess.
----------------------------------------------------------------------------------------------*/
class VwUndoAction : public IUndoAction
{
	friend class VwUndoDa;

public:
	VwUndoAction(VwUndoDa * puda, HVO hvoObj, PropTag tag);
	virtual ~VwUndoAction();

	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, Release)(void);

	STDMETHOD(Undo)(ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool * pfSuccess);
	STDMETHOD(Commit)();
	STDMETHOD(get_IsDataChange)(ComBool * pfRet);
	STDMETHOD(get_IsRedoable)(ComBool * pfRet);
	STDMETHOD(put_SuppressNotification)(ComBool fSuppress);

protected:
	int m_cref;	// Standard reference count variable.

	VwUndoDa * m_puda;		// data access object where changes are to be made; dumb pointer
							// to avoid circular pointer loops
	HVO m_hvoObj;			// object in which to make changes
	PropTag m_tag;			// property tag
	bool m_fStateUndone;	// undone or redone
};
DEFINE_COM_PTR(VwUndoAction);

/*----------------------------------------------------------------------------------------------
	Undoes a DeleteObjOwner or DeleteObject operation.
----------------------------------------------------------------------------------------------*/
class VwUndoDeleteAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoDeleteAction(VwUndoDa * puda, HVO hvoOwner, PropTag tag, HVO hvoDeleted, int ihvo);
	~VwUndoDeleteAction()
	{
	}

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	HVO m_hvoDeleted;
	int m_ihvo;
};
DEFINE_COM_PTR(VwUndoDeleteAction);

/*----------------------------------------------------------------------------------------------
	Undoes a InsertNew operation.
----------------------------------------------------------------------------------------------*/
class VwUndoInsertAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoInsertAction(VwUndoDa * puda, HVO hvoOwner, PropTag tag,
		int ihvo, int chvo, IVwStylesheet * pss);
	~VwUndoInsertAction();

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	int m_ihvoMinIns;
	int m_chvo;
	IVwStylesheet * m_pss;
	HVO * m_prghvoNew;
};
DEFINE_COM_PTR(VwUndoInsertAction);

/*----------------------------------------------------------------------------------------------
	Undoes a MakeNewObject operation.
----------------------------------------------------------------------------------------------*/
class VwUndoMakeNewObjectAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoMakeNewObjectAction(VwUndoDa * puda, HVO hvoOwner, PropTag tag, int ihvo, HVO hvo,
		HVO hvoOld);
	~VwUndoMakeNewObjectAction();

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	HVO m_hvo;
	int m_ihvo;
	HVO m_hvoOld; // for atomics.
};
DEFINE_COM_PTR(VwUndoMakeNewObjectAction);

/*----------------------------------------------------------------------------------------------
	Undoes a Replace operation.
----------------------------------------------------------------------------------------------*/
class VwUndoReplaceAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoReplaceAction(VwUndoDa * puda, HVO hvoOwner, PropTag tag,
		int chvoIns, int chvoDel, int ihvoMin, HVO * prghvoIns, HVO * prghvoDel);
	~VwUndoReplaceAction();

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	int m_chvoIns;
	int m_chvoDel;
	int m_ihvoMin;
	HVO * m_prghvoIns;
	HVO * m_prghvoDel;
};
DEFINE_COM_PTR(VwUndoReplaceAction);

/*----------------------------------------------------------------------------------------------
	Undoes a SetBinary operation.
----------------------------------------------------------------------------------------------*/
class VwUndoSetBinaryAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoSetBinaryAction(VwUndoDa * puda, HVO hvo, PropTag tag,
		byte * prgb, int cb);
	~VwUndoSetBinaryAction()
	{
		if (m_prgbOther)
			delete[] m_prgbOther;
	}

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	byte * m_prgbOther;
	int m_cbOther;

	HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending);
};
DEFINE_COM_PTR(VwUndoSetBinaryAction);

/*----------------------------------------------------------------------------------------------
	Undoes a SetInt or SetObjProp operation.
----------------------------------------------------------------------------------------------*/
class VwUndoSetIntAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoSetIntAction(VwUndoDa * puda, HVO hvo, PropTag tag, int n, bool fObj);

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	int m_nOther;
	bool m_fObj;	// true if this is for a SetObjProp operation, false if for SetInt

	HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending);
};
DEFINE_COM_PTR(VwUndoSetIntAction);

/*----------------------------------------------------------------------------------------------
	Undoes a SetTime or SetInt64 operation.
----------------------------------------------------------------------------------------------*/
class VwUndoSetTimeAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoSetTimeAction(VwUndoDa * puda, HVO hvo, PropTag tag, int64 lln, bool fObj);

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	int64 m_llnOther;
	bool m_fTime;	// true if this is for a SetTime operation, false if for SetInt64

	HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending);
};
DEFINE_COM_PTR(VwUndoSetTimeAction);

/*----------------------------------------------------------------------------------------------
	Undoes a SetGuid operation.
----------------------------------------------------------------------------------------------*/
class VwUndoSetGuidAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoSetGuidAction(VwUndoDa * puda, HVO hvo, PropTag tag, GUID uid);

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	GUID m_uidOther;

	HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending);
};
DEFINE_COM_PTR(VwUndoSetGuidAction);

/*----------------------------------------------------------------------------------------------
	Undoes a SetString or SetMultiStringAlt operation.
----------------------------------------------------------------------------------------------*/
class VwUndoSetStringAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoSetStringAction(VwUndoDa * puda, HVO hvo, PropTag tag, int ws, ITsString * ptss);

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	ITsStringPtr m_qtssOther;
	int m_ws;

	HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending);
};
DEFINE_COM_PTR(VwUndoSetStringAction);

/*----------------------------------------------------------------------------------------------
	Undoes a SetUnicode operation.
----------------------------------------------------------------------------------------------*/
class VwUndoSetUnicodeAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoSetUnicodeAction(VwUndoDa * puda, HVO hvo, PropTag tag,
		OLECHAR * prgch, int cch);
	~VwUndoSetUnicodeAction()
	{
		if (m_prgchOther)
			delete[] m_prgchOther;
	}

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	OLECHAR * m_prgchOther;
	int m_cchOther;

	HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending);
};
DEFINE_COM_PTR(VwUndoSetUnicodeAction);

/*----------------------------------------------------------------------------------------------
	Undoes a SetUnknown operation.
----------------------------------------------------------------------------------------------*/
class VwUndoSetUnknownAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoSetUnknownAction(VwUndoDa * puda, HVO hvo, PropTag tag, IUnknown * punk);
	~VwUndoSetUnknownAction()
	{
	}

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	IUnknownPtr m_qunkOther;

	HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess, ComBool fRefreshPending);
};
DEFINE_COM_PTR(VwUndoSetUnknownAction);

// The main Views DLL doesn't need these classes, and can't build them, because they depends on some
// includes from AppCore.
#ifndef VIEWSDLL

/*----------------------------------------------------------------------------------------------
	Undoes an operation to the stylesheet--making a change that requires ComputeDerivedStyles.
----------------------------------------------------------------------------------------------*/
class VwUndoStylesheetAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoStylesheetAction(VwUndoDa * puda, AfStylesheet * pasts, bool fForUndo);
	~VwUndoStylesheetAction()
	{
	}

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);

protected:
	AfStylesheet * m_pasts; // dumb pointer to avoid circular loops

	// We surround the all the changes to a style with two of these actions, which cause
	// ComputeDerivedStyles to be run.. The first action is "undone" only when we are undoing,
	// so it happens after all the changes have been undone. The terminating action is "undone,"
	// or rather, redone, only when we are redoing, so it happens after all the changes have
	// been redone. The flag below tells us which action we've got.
	bool m_fForUndo;

	// JohnT, 8/28/06: This really positively needs to be virtual! VwUndoStyleAction overrides!
	virtual HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess);
};
DEFINE_COM_PTR(VwUndoStylesheetAction);


/*----------------------------------------------------------------------------------------------
	Undoes an operation to the stylesheet--inserting or deleting a style.
----------------------------------------------------------------------------------------------*/
class VwUndoStyleAction : public VwUndoStylesheetAction
{
	typedef VwUndoStylesheetAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoStyleAction(VwUndoDa * puda, AfStylesheet * pasts, HVO hvoStyle, StrUni stu,
		bool fDeleted);
	~VwUndoStyleAction()
	{
	}

protected:
	HVO m_hvoStyle;
	StrUni m_stuName;
	bool m_fDeleted;	// if false, it was inserted

	virtual HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess);
};
DEFINE_COM_PTR(VwUndoStyleAction);

/*----------------------------------------------------------------------------------------------
	Handle replacing a selection after undoing or redoing.
----------------------------------------------------------------------------------------------*/
class VwUndoSelectionAction : public VwUndoAction
{
	typedef VwUndoAction SuperClass;

	friend class VwUndoDa;

public:
	VwUndoSelectionAction(VwUndoDa * puda, AfVwRootSite * pavrs, IVwSelection* psel,
		bool fForUndo);
	~VwUndoSelectionAction()
	{
	}

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(get_IsDataChange)(ComBool * pfRet);

protected:
//	AfVwRootSite * m_pavrs;
	IVwRootBox * m_prootb;	// root box to make active
	AfVwSelInfo m_avsi;
	bool m_fForUndo;

	HRESULT UndoRedo(bool fUndo, ComBool * pfSuccess);
};
DEFINE_COM_PTR(VwUndoSelectionAction);
#endif

#endif // UndoAction_INCLUDED
