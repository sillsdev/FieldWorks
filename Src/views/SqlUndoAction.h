/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: SqlUndoAction.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains class declarations for the following class:
		SqlUndoAction
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef SqlUndoAction_INCLUDED
#define SqlUndoAction_INCLUDED

/*----------------------------------------------------------------------------------------------
	Cross-Reference: ${ISqlUndoAction}, ${IUndoAction}

	@h3{Hungarian: sqlua}
----------------------------------------------------------------------------------------------*/
class SqlUndoAction : public ISqlUndoAction, public IUndoAction
{
public:
	//:> ENHANCE PaulP:  Probably need to make a deep copy method. (JohnT: why??)
	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(AddRedoCommand)(/*IOleDbEncap*/IUnknown * pode, /*IOleDbCommand*/IUnknown * podc,
		BSTR bstrSql);
	STDMETHOD(AddUndoCommand)(/*IOleDbEncap*/IUnknown * pode, /*IOleDbCommand*/IUnknown * podc,
		BSTR bstrSqlUndo);
	STDMETHOD(AddRedoReloadInfo)(IVwOleDbDa * podda, BSTR bstrSqlReloadData, IDbColSpec * pdcs,
		HVO hvoBase, int nrowMax, IAdvInd * padvi);
	STDMETHOD(AddUndoReloadInfo)(IVwOleDbDa * podda, BSTR bstrSqlReloadData, IDbColSpec * pdcs,
		HVO hvoBase, int nrowMax, IAdvInd * padvi);
	STDMETHOD(VerifyUndoable)(/*IOleDbEncap*/IUnknown * pode, /*IOleDbCommand*/IUnknown * podc,
		BSTR bstrSql);
	STDMETHOD(VerifyRedoable)(/*IOleDbEncap*/IUnknown * pode, /*IOleDbCommand*/IUnknown * podc,
		BSTR bstrSql);

	// ENHANCE PaulP: May want to make undo/redo/commit protected and make the ActionHandler a
	// friend class.
	STDMETHOD(Undo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Redo)(ComBool fRefreshPending, ComBool * pfSuccess);
	STDMETHOD(Commit)();
	STDMETHOD(IsDataChange)(ComBool * pfRet);
	STDMETHOD(IsRedoable)(ComBool * pfRet);
	STDMETHOD(RequiresRefresh)(ComBool * pfRet);
	STDMETHOD(put_SuppressNotification)(ComBool fSuppress);

protected:
	SqlUndoAction();
	~SqlUndoAction();

	void NoteConnection(IUnknown * punk);

	int m_cref;

	// Database connection; used for db transaction commit, for undo's.
	// Cleared after all IOleDbCommands are cleared, so things are destroyed in the right order.
	Vector<IOleDbEncapPtr> m_vqode;

	// The one and only cache
	IVwOleDbDaPtr m_qodda;

	// Cache used to reload data, after an undo
	Vector<SmartBstr> m_vsbstrSqlReloadDataUndo;
	Vector<IDbColSpecPtr> m_vqdcsUndo;
	Vector<HVO> m_vhvoBaseUndo;
	Vector<int> m_vnrowMaxUndo;
	Vector<IAdvIndPtr> m_vqadviUndo;

	// These are two matching-length vectors of the SQL needed to implement Redo.
	// Redo is accomplished by executing the each command in order passing the corresponding string.
	Vector<IOleDbCommandPtr> m_vqodcRedo;
	Vector<SmartBstr> m_vsbstrSqlRedo;

	// These are two matching-length vectors of the SQL needed to implement Undo.
	// Redo is accomplished by executing the each command in order passing the corresponding string.
	Vector<IOleDbCommandPtr> m_vqodcUndo;
	Vector<SmartBstr> m_vsbstrSqlUndo;

	// Cache used to reload data, after a redo
	Vector<SmartBstr> m_vbstrSqlReloadDataRedo;
	Vector<IDbColSpecPtr> m_vqdcsRedo;
	Vector<HVO> m_vhvoBaseRedo;
	Vector<int> m_vnrowMaxRedo;
	Vector<IAdvIndPtr> m_vqadviRedo;

	// Variables used to check we can legitimately Un/Redo.
	IOleDbCommandPtr m_qodcVerifyUndo; // command to execute following sql to verify Undoable
	SmartBstr m_sbstrVerifyUndo;
	IOleDbCommandPtr m_qodcVerifyRedo;
	SmartBstr m_sbstrVerifyRedo;
};
DEFINE_COM_PTR(SqlUndoAction);

#endif // SqlUndoAction_INCLUDED
