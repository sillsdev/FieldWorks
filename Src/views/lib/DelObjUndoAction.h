/*------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DelObjUndoAction.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains class declarations for the following class:
		DelObjUndoAction
-----------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DelObjUndoAction_INCLUDED
#define DelObjUndoAction_INCLUDED

/*--------------------------------------------------------------------------------------------
	Cross-Reference: ${IUndoAction}

	@h3{Hungarian: sqlua}
--------------------------------------------------------------------------------------------*/
class DelObjUndoAction : public IUndoAction, IInitUndoDeleteObject
{
public:
	// Used to store the basic CmObject info for the deleted objects.
	struct CmObjectInfo
	{
		HVO m_hvoObj;
		GUID m_guid;
		int m_clsid;
		HVO m_hvoOwner;
		int m_flidOwner;
		int m_ordOwner;
	};
	typedef Vector<CmObjectInfo> CmiVec;

	struct DelObjInfo
	{
		//int m_cpt; // type of property
		HVO m_hvoObj; // object having the property
		int m_flid; // identifier of specific property.
	};

	// Used for all string types (13-19), Guid (6), Image (7), Binary (9)
	struct StringDelObjInfo : DelObjInfo
	{
		int m_ws; // writing system (if multistring prop).
		StrUni m_stuText; // Typically text of a string.
		Vector<byte> m_vb; // Often used for format; also VarBinary, Image, etc.
	};
	typedef Vector<StringDelObjInfo> SdoiVec;

	// Special one for Time (5)
	struct TimeDelObjInfo : DelObjInfo
	{
		DBTIMESTAMP m_time;
	};
	typedef Vector<TimeDelObjInfo> TdoiVec;

	// Properties whose value fits in a 32-bit integer: Boolean (1), Integer (2), GenDate (8),
	// Atomic Reference (24)
	struct IntDelObjInfo : DelObjInfo
	{
		int m_val;
	};
	typedef Vector<IntDelObjInfo> IdoiVec;

	// Reference collection or sequence (26, 28)
	struct SeqDelObjInfo : DelObjInfo
	{
		Vector<HVO> m_vhvoValue; // in order if sequence.
	};
	typedef Vector<SeqDelObjInfo> SqdoiVec;

	struct IncomingRefInfo
	{
		HVO m_hvoSrc;
		HVO m_hvoDst;
		int m_flid;
		int m_ord;
	};
	typedef Vector<IncomingRefInfo> IriVec;

	DelObjUndoAction();
	~DelObjUndoAction();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Commit)();
	STDMETHOD(IsDataChange)(ComBool *pfRet);
	STDMETHOD(IsRedoable)(ComBool * pfRet);
	STDMETHOD(RequiresRefresh)(ComBool * pfRet);
	STDMETHOD(put_SuppressNotification)(ComBool fSuppress);
	STDMETHOD(GatherUndoInfo)(BSTR bstrIds, IOleDbEncap * pode,
		IFwMetaDataCache * pmdc, IVwCacheDa * pcda);

	bool HasIncomingReferences()
	{
		return m_viriIncomingRefInfo.Size();
	}

protected:
	int m_cref;

	// For each struct defined above, we have a vector to store occurrences for
	// different objects and flids.
	CmiVec m_vcmiCoreInfo;
	SdoiVec m_vsdoiStringInfo;
	TdoiVec m_vtdoiTimeInfo;
	IdoiVec m_vidoiIntInfo;
	SqdoiVec m_vsqdoiSeqInfo;
	IriVec m_viriIncomingRefInfo;
	HvoVec m_vhvoBefore; // objects to insert the roots before.

	// Database connection; used for db transaction commit, for undo's.
	// Cleared after all IOleDbCommands are cleared, so things are destroyed in the right
	// order.
	IOleDbEncapPtr m_qode;
	IFwMetaDataCachePtr m_qmdc;
	IVwCacheDaPtr m_qcda;
	void GetNames(int flid, int & cpt, SmartBstr & sbstrFieldName, SmartBstr & sbstrClassName);
	void UpdateCache();
	bool IsTopLevelObjInOwningSeq();
	bool IsTopLevelObjInType(int cptTarget);
};
DEFINE_COM_PTR(DelObjUndoAction);

#endif // DelObjUndoAction_INCLUDED
