/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpDa.h
Responsibility: Sharon Correll (first draft by John Thomson)
Last reviewed: never

Description:
	This class provides an implementation of ISilDataAccess for WorldPad, where the underlying
	data is a simple XML representation of a document consisting of a sequence of styled
	paragraphs.
	The object representing the whole database is always an StText with HVO 1.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef WPDA_INCLUDED
#define WPDA_INCLUDED 1

#if !WIN32
#include "../views/Main.h"
#include "ISimpleChildWnd.h"
#include "winnt.h"
#include "COMBase.h"
#endif // !WIN32

class WpApp;
class WpDa;
class WpMainWnd;
#ifndef WIN32
class ISimpleMainWnd;
#endif
class WpChildWnd;
typedef GenSmartPtr<WpDa> WpDaPtr;
typedef GenSmartPtr<WpMainWnd> WpMainWndPtr;
typedef GenSmartPtr<WpChildWnd> WpChildWndPtr;

//:End Ignore


enum {
	khvoText = 1,
	khvoParaMin = 10,
	khvoStyleMin = 100000,
};

// File import/export types:
enum {
	kfietUnknown = 1,
	kfietXml,
	kfietTemplate,
	kfietUtf16,
	kfietUtf8,
	kfietAnsi,
	//kfietHtml,

	kfietLim,

	kfietPlain = kfietUtf16,
};

enum {	// hungarian fls
	kflsOkay,		// error-free load
	kflsPartial,	// partial load, with errors
	kflsAborted		// nothing loaded
};

// For storing styles from stylesheet as owned objects of the main text;
// using a number under 10000 is supposed to keep this from conflicting.
const int kflidStText_Styles = 100;

/*----------------------------------------------------------------------------------------------
	The main customizeable document view constructor class.
	Hungarian: dw.
----------------------------------------------------------------------------------------------*/
class WpDa : public VwUndoDa
#if !WIN32
//, COMBase::Unknown<IWpDa> // TODO-Linux: use COMBase probably.
, IWpDa
#endif //!WIN32
{
	typedef VwUndoDa SuperClass;

public:

	WpDa();
	~WpDa();

	void OnReleasePtr();

#if !WIN32
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return SuperClass::AddRef();
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = SuperClass::Release();
		return cref;
	}
#endif // !WIN32

	//	ISilDataAccess methods, overridden:

	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** ppwsf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(get_WritingSystemsOfInterest)(int cwsMax, int * pws, int * pcws);

	//	Other methods:

	void InitNew(StrAnsi staFileName, int * pft, ISimpleMainWnd * pwpwnd, ISimpleChildWnd * pwcwnd);
	void InitNewEmpty();
	void LoadIntoEmpty(StrAnsi staFileName, int * pft,
		ISimpleMainWnd * pwpwnd, ISimpleChildWnd * pwcw, ITsTextProps * pttp,
		int * pfls);
	bool IsEmpty();
	bool SaveToFile(StrAnsi staFileName, int fiet, ISimpleMainWnd * pwpwnd);

	STDMETHOD(LoadXml)(BSTR bstrFile, ISimpleMainWnd * pwpwnd, ISimpleMainWnd * pwpwndLauncher, int * pfls);
	STDMETHOD(SaveXml)(BSTR bstrFile, ISimpleMainWnd * pwpwnd, ComBool fDtd = true);
	void SaveXmlToStream(IStream * pstrm, ISimpleMainWnd * pwpwnd, bool fDtd);

	int DocRightToLeft();

	void GenerateXmlForEncoding(int iws, StrAnsi * psta);
	bool ParseXmlForWritingSystem(StrUni & stuInput, StrApp * pstrFile = NULL, bool fReportMissingWs = true);
	bool ParseXmlForWritingSystem(StrAnsi & staInput, StrApp * pstrFile = NULL, bool fReportMissingWs = true);
	bool ParseLanguageDefinitionFile(StrApp & strFile, bool fReportMissingWs);

	typedef enum
	{
		katRead = KEY_READ,
		katWrite = KEY_WRITE,
		katBoth = katRead | katWrite,
	} AccessType;

	void InitWithWsInRegistry(HWND hwndMain);
	void InitWithPersistedWs(HWND hwndMain);
	void PersistAllWs(HWND hwndMain);
	bool DeleteWsFromRegistry(int ws);
	static bool GetAllWsFromRegistry(Vector<StrApp> & vstr);
	static bool GetWsDescription(const achar * pszFontKey, StrApp & str);
	static bool OpenRegWsKey(int at, HKEY * phkey);
	static bool DeleteRegWsKey();

	void RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames, Vector<StrUni> & vstuNewNames,
		Vector<StrUni> & vstuDelNames);

	bool AnyStringWithWs(int ws);

	bool Undo();
	bool CanUndo();
	bool Redo();
	bool CanRedo();

	void ClearUndo()
	{
		if (m_qacth)
			m_qacth->Commit();
	}

	bool IsDirty();
	virtual void InformNowDirty();
	void ClearChanges();

	void SetUpWsFactory();

	int FileLoadStatus()
	{
		return m_fls;
	}

protected:
	//	instance variables:
	ISimpleMainWnd * m_pwpwnd;	// use a dumb pointer to avoid circular loops
	int m_fls;

	void GetTextFromFile(StrAnsi staFileName, int * pft, int * pcbFileLen,
		OLECHAR ** ppswzData, int * pcchw, int * pfls);
	bool WorldPadXmlFormat(char * pszData, int pcbFileLen, bool * fUtf8);
	void ReadStringsFromBuffer(OLECHAR * pswzData, int cchw, Vector<StrUni> & vstu);

	void WriteXmlLanguages(IStream * pstrm, Set<int> & setws);
	void WriteXmlEncoding(IStream * pstrm, ILgWritingSystemFactory * pwsf, int ws);
	void GetUsedEncodings(Set<int> & setws);
#ifdef WIN32
	void WriteXmlStyles(IStream * pstrm, AfStylesheet * pasts, Set<int> & setws);
#else
	void WriteXmlStyles(IStream * pstrm, ISimpleStylesheet * pasts, Set<int> & setws);
#endif
	void WriteXmlBody(IStream * pstrm);
	void WriteXmlPageSetup(IStream * pstrm, ISimpleMainWnd * pwpwnd);

	void CleanupWritingSystems(Vector<void *> & vpvNewWs);
//	void DeleteEmptyLogFile(FILE *, StrAnsiBufPath, int);

	bool Delete(Vector<StrUni> & vstuDelNames, StrUni stu);
	bool Rename(Vector<StrUni> & vstuOldNames, Vector<StrUni> & vstuNewNames,
		StrUni stuOld, StrUni & stuNew);
};


static const char * ColorName(int nColor);
static const char * ToggleValueName(byte ttv);
static const char * PropVarName(int tpv);
static const char * SuperscriptValName(byte ssv);
static const char * UnderlineTypeName(byte unt);
static const char * AlignmentTypeName(byte tal);

#endif // WPDA_INCLUDED
