/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: SqlUndoAction.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This file contains class definitions for the following class:
		SqlUndoAction

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#undef TRACE_SQLUNDOACTION
//#define TRACE_SQLUNDOACTION

//  Note: Including these 2 files will lead to code bloat.
// Note: All of these are now in ExplicitInstantiation.cpp
//#include "HashMap.h"
//#include "HashMap_i.cpp"
#include "Vector_i.cpp"

//:>********************************************************************************************
//:>	SqlUndoAction methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
SqlUndoAction::SqlUndoAction()
{
	m_cref = 1;
	m_sbstrVerifyRedo = NULL;
	ModuleEntry::ModuleAddRef();
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
SqlUndoAction::~SqlUndoAction()
{
	// It's vital to do them in this order; if the OleDbEncap objects get deleted first,
	// we get connections left open. Putting this here ensures the order is not dependent on
	// how the variables are declared or how the compiler behaves.
	m_vqodcRedo.Clear();
	m_vqodcUndo.Clear();
	m_vqode.Clear();
	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:>	SqlUndoAction - Generic factory stuff to allow creating an instance w/ CoCreateInstance.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
static GenericFactory g_factSqlUndoAct(
	_T("SIL.Views.SqlUndoAction"),
	&CLSID_SqlUndoAction,
	_T("SIL SQL Undo Action"),
	_T("Apartment"),
	&SqlUndoAction::CreateCom);


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void SqlUndoAction::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<SqlUndoAction> qzsqlua;
	qzsqlua.Attach(NewObj SqlUndoAction());		// ref count initially 1
	CheckHr(qzsqlua->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	SqlUndoAction - IUnknown Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ISqlUndoAction *>(this));
	else if (iid == IID_IUndoAction)
		*ppv = static_cast<IUndoAction *>(this);
	else if (iid == IID_ISqlUndoAction)
		*ppv = static_cast<ISqlUndoAction *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<ISqlUndoAction *>(this)), IID_ISqlUndoAction);
//		*ppv = NewObj CSupportErrorInfo(this, IID_IUndoAction);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) SqlUndoAction::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) SqlUndoAction::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Note that we have worked with the specified IOleDbEncap (which is passed as IUnknown
	because it is an argument to IUndoAction methods that are specified in FwKernel.idh
	which cannot know about IOleDbEncap without circularity.
----------------------------------------------------------------------------------------------*/
void SqlUndoAction::NoteConnection(IUnknown * punk)
{
	ChkComArgPtr(punk);

	IOleDbEncapPtr qode;
	CheckHr(punk->QueryInterface(IID_IOleDbEncap, (void **)&qode));

	// Check if we already have a connection to that database.
	int i;
	for (i = 0; i < m_vqode.Size(); i++)
	{
		if (qode.Ptr() == m_vqode[i].Ptr())
			break;
	}
	if (i >= m_vqode.Size())
	{
		// We haven't encountered a connection to this database before so add it.
		m_vqode.Push(qode);
	}
}


/*----------------------------------------------------------------------------------------------
	${ISqlUndoAction#AddRedoCommand}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::AddRedoCommand(IUnknown * pode, IUnknown * podc, BSTR bstrSqlRedo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pode);
	ChkComArgPtr(podc);
	ChkComBstrArgN(bstrSqlRedo);

	IOleDbCommandPtr qodc;
	CheckHr(podc->QueryInterface(IID_IOleDbCommand, (void **)&qodc));

	// Note we're working with this database.
	NoteConnection(pode);

	m_vqodcRedo.Push(qodc);
	SmartBstr sbstrSqlRedo;
	sbstrSqlRedo = bstrSqlRedo;
	m_vsbstrSqlRedo.Push(sbstrSqlRedo);

#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::AddRedoCommand(");
	sta.FormatAppend("\"%B\") - m_vqode[%d], m_vsbstrSavePoint[%d], m_vsbstrSqlRedo[%d]\n",
		bstrSqlRedo, m_vqode.Size(), m_vsbstrSavePoint.Size(), m_vsbstrSqlRedo.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	END_COM_METHOD(g_factSqlUndoAct, IID_ISqlUndoAction);
};


/*----------------------------------------------------------------------------------------------
	${ISqlUndoAction#AddUndoCommand}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::AddUndoCommand(IUnknown * pode, IUnknown * podc, BSTR bstrSqlUndo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pode);
	ChkComArgPtr(podc);
	ChkComBstrArgN(bstrSqlUndo);

	IOleDbCommandPtr qodc;
	CheckHr(podc->QueryInterface(IID_IOleDbCommand, (void **)&qodc));

	// Note we're working with this database.
	NoteConnection(pode);

	m_vqodcUndo.Push(qodc);
	SmartBstr sbstrSqlUndo;
	sbstrSqlUndo = bstrSqlUndo;
	m_vsbstrSqlUndo.Push(sbstrSqlUndo);

#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::AddUndoCommand(");
	sta.FormatAppend("\"%B\") - m_vqode[%d], m_vsbstrSavePoint[%d], m_vsbstrSqlUndo[%d]\n",
		bstrSqlUndo, m_vqode.Size(), m_vsbstrSavePoint.Size(), m_vsbstrSqlUndo.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	END_COM_METHOD(g_factSqlUndoAct, IID_ISqlUndoAction);
};

/*----------------------------------------------------------------------------------------------
	${ISqlUndoAction#AddRedoReloadInfo}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::AddRedoReloadInfo(IVwOleDbDa * podda, BSTR bstrSqlReloadData,
	IDbColSpec * pdcs, HVO hvoBase, int nrowMax, IAdvInd * padvi)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(podda);
	ChkComBstrArgN(bstrSqlReloadData);
	ChkComArgPtrN(pdcs);
	ChkComArgPtrN(padvi);

	Assert(!m_qodda || podda == m_qodda);
	if (m_qodda && podda != m_qodda)
		return E_UNEXPECTED;

	m_qodda = podda;
	SmartBstr sbstr = bstrSqlReloadData;
	m_vbstrSqlReloadDataRedo.Push(sbstr.Bstr());
	m_vqdcsRedo.Push(pdcs);
	m_vhvoBaseRedo.Push(hvoBase);
	m_vnrowMaxRedo.Push(nrowMax);
	m_vqadviRedo.Push(padvi);

#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::AddRedoReloadInfo(");
	sta.FormatAppend(
		"\"%B\") - m_vqode[%d], m_vsbstrSavePoint[%d], m_vbstrSqlReloadDataRedo[%d]\n",
		bstrSqlReloadData, m_vqode.Size(), m_vsbstrSavePoint.Size(),
		m_vbstrSqlReloadDataRedo.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	END_COM_METHOD(g_factSqlUndoAct, IID_ISqlUndoAction);
};


/*----------------------------------------------------------------------------------------------
	${ISqlUndoAction#AddUndoReloadInfo}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::AddUndoReloadInfo(IVwOleDbDa * podda, BSTR bstrSqlReloadData,
	IDbColSpec * pdcs, HVO hvoBase, int nrowMax, IAdvInd * padvi)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(podda);
	ChkComBstrArgN(bstrSqlReloadData);
	ChkComArgPtrN(pdcs);
	ChkComArgPtrN(padvi);

	Assert(!m_qodda || podda == m_qodda);
	if (m_qodda && podda != m_qodda)
		return E_UNEXPECTED;

	m_qodda = podda;
	SmartBstr sbstr = bstrSqlReloadData;
	m_vsbstrSqlReloadDataUndo.Push(sbstr);
	m_vqdcsUndo.Push(pdcs);
	m_vhvoBaseUndo.Push(hvoBase);
	m_vnrowMaxUndo.Push(nrowMax);
	m_vqadviUndo.Push(padvi);

#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::AddUndoReloadInfo(");
	sta.FormatAppend(
		"\"%B\") - m_vqode[%d], m_vsbstrSavePoint[%d], m_vbstrSqlReloadDataRedo[%d]\n",
		bstrSqlReloadData, m_vqode.Size(), m_vsbstrSavePoint.Size(),
		m_vbstrSqlReloadDataRedo.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	END_COM_METHOD(g_factSqlUndoAct, IID_ISqlUndoAction);
};

/*----------------------------------------------------------------------------------------------
	${ISqlUndoAction#VerifyUndoable}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::VerifyUndoable(/*IOleDbEncap*/IUnknown * pode,
	/*IOleDbCommand*/IUnknown * podc, BSTR bstrSql)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pode);
	ChkComArgPtr(podc);
	ChkComBstrArgN(bstrSql);

	CheckHr(podc->QueryInterface(IID_IOleDbCommand, (void **)&m_qodcVerifyUndo));

	// Note we're working with this database.
	NoteConnection(pode);

	m_sbstrVerifyUndo = bstrSql;

#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::VerifyUndoable(");
	sta.FormatAppend("\"%B\") - m_vqode[%d]\n",
		bstrSql, m_vqode.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	END_COM_METHOD(g_factSqlUndoAct, IID_ISqlUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${ISqlUndoAction#VerifyRedoable}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::VerifyRedoable(/*IOleDbEncap*/IUnknown * pode,
	/*IOleDbCommand*/IUnknown * podc, BSTR bstrSql)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pode);
	ChkComArgPtr(podc);
	ChkComBstrArgN(bstrSql);

	CheckHr(podc->QueryInterface(IID_IOleDbCommand, (void **)&m_qodcVerifyRedo));

	// Note we're working with this database.
	NoteConnection(pode);

	m_sbstrVerifyRedo = bstrSql;

#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::VerifyRedoable(");
	sta.FormatAppend("\"%B\") - m_vqode[%d]\n",
		bstrSql, m_vqode.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	END_COM_METHOD(g_factSqlUndoAct, IID_ISqlUndoAction);
}

// Returns true if (a) podc is null (no test to run passes);
// or if having podc execute bstrQuery produces a rowset in which there is at least
// one row and the first row starts with a non-zero integer.
bool VerifySqlTest(IOleDbCommand * podc, BSTR bstrQuery)
{
	if (!podc)
		return true;
	ULONG nVerifyResult = 0;
	HRESULT hr;
	IgnoreHr(hr = podc->ExecCommand(bstrQuery, knSqlStmtSelectWithOneRowset));
	if (FAILED(hr))
		return false;
	CheckHr(podc->GetRowset(0));
	ComBool fMoreRows;
	CheckHr(podc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		ULONG cbSpaceTaken;
		ComBool fIsNull;
		CheckHr(podc->GetColValue(1, (BYTE*)&nVerifyResult,
			isizeof(ULONG), &cbSpaceTaken, &fIsNull, 0));
	}
	CheckHr(podc->ReleaseExceptParams());
	return (nVerifyResult != 0);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#Undo}

	Executes the SQL that was specified to actually bring about the Undoing of the change.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::Undo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);

	HRESULT hr = E_FAIL;
	HRESULT hrFinal = S_OK;

	// Run the SQL to verify that Undo is possible.
	if (!VerifySqlTest(m_qodcVerifyUndo, m_sbstrVerifyUndo))
		return S_OK; // *pfSuccess is false.

	// Execute the Undo command(s)
	int i;
	for (i = 0; i < m_vqodcUndo.Size(); i++)
	{
		IgnoreHr(hr = m_vqodcUndo[i]->ExecCommand(m_vsbstrSqlUndo[i], knSqlStmtStoredProcedure));
		if (FAILED(hr))
		{
			// REVIEW 1897 (PaulP): It is really bad if this fails, so it is not clear what
			// should be done.  I think probably dump the whole undo/redo stack.
			hrFinal = hr;
		}
		CheckHr(m_vqodcUndo[i]->ReleaseExceptParams());
	}


	// Reload the respective caches.
	for (int i = m_vsbstrSqlReloadDataUndo.Size() - 1; i >= 0; i--)
	{
		HVO hvo = m_vhvoBaseRedo[i];
		IgnoreHr(hr = m_qodda->Load(m_vsbstrSqlReloadDataUndo[i], m_vqdcsUndo[i],
			hvo, m_vnrowMaxUndo[i], m_vqadviUndo[i], !fRefreshPending));

		// If for some reason the reload fails, simply continue on.
		if (FAILED(hr))
		{
			hrFinal = hr;
		}

		// The following lines fix TE-3022.
		// The Undo updated the timestamp on the object and on the owner.
		// We need to make sure the cache copy is kept in sync.
		// If we are dealing with a paragraph from an StText, the database updates both
		// the StText and its owner, so we need to update both timestamps in this case.
		m_qodda->CacheCurrTimeStamp(hvo);
		HVO hvoParent;
		m_qodda->get_ObjOwner(hvo, &hvoParent);
		if (hvoParent)
		{
			m_qodda->CacheCurrTimeStamp(hvoParent);

			int flidOwn;
			m_qodda->get_ObjOwnFlid(hvo, &flidOwn);
			if (flidOwn == kflidStText_Paragraphs) {
				HVO hvoGrandparent;
				m_qodda->get_ObjOwner(hvoParent, &hvoGrandparent);
				if(hvoGrandparent){
					m_qodda->CacheCurrTimeStamp(hvoGrandparent);
				}
			}
		}
	}

#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::Undo()");
	sta.FormatAppend(" - m_vqode[%d], m_vsbstrSavePoint[%d], m_vbstrSqlReloadDataRedo[%d]\n",
		m_vqode.Size(), m_vsbstrSavePoint.Size(), m_vbstrSqlReloadDataRedo.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	// Review: Should we return hrFinal instead of hr?
	//Assert(SUCCEEDED(hrFinal));
	*pfSuccess = true;
	return hr;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#Redo}

	Executes all the redo SQL commands that were set for the SqlUndoAction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::Redo(ComBool fRefreshPending, ComBool * pfSuccess)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfSuccess);

	HRESULT hr = E_FAIL;
	HRESULT hrFinal = S_OK;

	// Run the SQL to verify that Redo is possible.
	if (!VerifySqlTest(m_qodcVerifyRedo, m_sbstrVerifyRedo))
		return S_OK; // *pfSuccess is false.

	int i;
	// Execute the Redo command(s)
	for (i = 0; i < m_vqodcRedo.Size(); i++)
	{
		IgnoreHr(hr = m_vqodcRedo[i]->ExecCommand(m_vsbstrSqlRedo[i], knSqlStmtStoredProcedure));
		if (FAILED(hr))
		{
			// REVIEW 1897 (PaulP): It is really bad if this fails, so it is not clear what
			// should be done.  I think probably dump the whole undo/redo stack.
			hrFinal = hr;
		}
		CheckHr(m_vqodcRedo[i]->ReleaseExceptParams());
	}


	// Reload the respective caches.
	for (i = m_vbstrSqlReloadDataRedo.Size() - 1; i >= 0; i--)
	{
		HVO hvo = m_vhvoBaseRedo[i];
		IgnoreHr(hr = m_qodda->Load(m_vbstrSqlReloadDataRedo[i], m_vqdcsRedo[i],
			hvo, m_vnrowMaxRedo[i], m_vqadviRedo[i], !fRefreshPending));

		// If for some reason the reload fails, simply continue on.
		if (FAILED(hr))
		{
			hrFinal = hr;
		}

		// The following lines fix TE-4033 and TE-4017.
		// The Redo updated the timestamp on the object and on the owner.
		// We need to make sure the cache copy is kept in sync.
		// If we are dealing with a paragraph from an StText, the database updates both
		// the StText and its owner, so we need to update both timestamps in this case.
		m_qodda->CacheCurrTimeStamp(hvo);
		HVO hvoParent;
		m_qodda->get_ObjOwner(hvo, &hvoParent);
		if (hvoParent)
		{
			m_qodda->CacheCurrTimeStamp(hvoParent);

			int flidOwn;
			m_qodda->get_ObjOwnFlid(hvo, &flidOwn);
			if (flidOwn == kflidStText_Paragraphs) {
				HVO hvoGrandparent;
				m_qodda->get_ObjOwner(hvoParent, &hvoGrandparent);
				if(hvoGrandparent){
					m_qodda->CacheCurrTimeStamp(hvoGrandparent);
				}
			}
		}
	}

#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::Redo()");
	sta.FormatAppend(
		" - m_vqode[%d], m_vsbstrSavePoint[%d] (\"%S\"), m_vbstrSqlReloadDataRedo[%d]\n",
		m_vqode.Size(), m_vsbstrSavePoint.Size(), m_vsbstrSavePoint.Top()->Chars(),
		m_vbstrSqlReloadDataRedo.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	*pfSuccess = true;
	return hr;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#Commit}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::Commit()
{
	BEGIN_COM_METHOD;

	HRESULT hr = E_FAIL;
	HRESULT hrFinal = S_OK;
	ComBool fTransOpen;

	// Commit all open transactions on database connections that are used for this action.
	for (int i = 0; i < m_vqode.Size(); i++)
	{
		m_vqode[i]->IsTransactionOpen(&fTransOpen);
		if (fTransOpen)
		{
			IgnoreHr(hr = m_vqode[i]->CommitTrans());
			if (FAILED(hr))
			{
				// REVIEW 1897 (PaulP): If one commit fails, maybe dump the whole undo/redo
				// stack.
				Assert(0);
				hrFinal = hr;
			}
		}
	}
#ifdef TRACE_SQLUNDOACTION
	StrAnsi sta("SqlUndoAction::Commit()");
	sta.FormatAppend(" - m_vqode[%d], m_vsbstrSavePoint[%d], m_vbstrSqlReloadDataRedo[%d]\n",
		m_vqode.Size(), m_vsbstrSavePoint.Size(), m_vbstrSqlReloadDataRedo.Size());
	::OutputDebugStringA(sta.Chars());
#endif
	return hrFinal;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#IsDataChange}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::IsDataChange(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	*pfRet = true;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#IsRedoable}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::IsRedoable(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);
	if (m_vsbstrSqlRedo.Size())
		*pfRet = true;
	else
		*pfRet = false;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#RequiresRefresh}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::RequiresRefresh(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);

	*pfRet = false;

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}

/*----------------------------------------------------------------------------------------------
	${IUndoAction#SuppressNotification}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SqlUndoAction::put_SuppressNotification(ComBool fSuppress)
{
	BEGIN_COM_METHOD;

	if (!m_qodda) // Can happen if we never give a SQL statement to reload the cache with
		return S_OK;

	IVwCacheDaPtr qcda;
	HRESULT hr = m_qodda->QueryInterface(IID_IVwCacheDa, (void **)&qcda);
	if (SUCCEEDED(hr))
	{
		if (fSuppress)
			CheckHr(qcda->SuppressPropChanges());
		else
			CheckHr(qcda->ResumePropChanges());
	}

	END_COM_METHOD(g_factSqlUndoAct, IID_IUndoAction);
}
