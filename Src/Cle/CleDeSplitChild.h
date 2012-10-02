/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: CleDeSplitChild.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This provides the data entry List Editor functions in CleDeSplitChild, as well as
	an application specific subclass of AfDeFeString.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef CLE_DE_SPLIT_CHILD_WND_INCLUDED
#define CLE_DE_SPLIT_CHILD_WND_INCLUDED 1

class CleDeSplitChild;
typedef GenSmartPtr<CleDeSplitChild> CleDeSplitChildPtr;
class CleDeFeString;
typedef GenSmartPtr<CleDeFeString> CleDeFeStringPtr;

/*----------------------------------------------------------------------------------------------
	This class specializes a basic data entry window to handle CleGenericRecords
	@h3{Hungarian: cleadsc.}
----------------------------------------------------------------------------------------------*/
class CleDeSplitChild : public AfDeRecSplitChild
{
public:
	typedef AfDeRecSplitChild SuperClass;

	CleDeSplitChild();
	~CleDeSplitChild();

	virtual void SetTreeHeader(AfDeFeNode * pden);
	virtual void BeginEdit(AfDeFieldEditor * pdfe);

	STDMETHODIMP DragEnter(IDataObject * pdobj, DWORD grfKeyState, POINTL pt, DWORD * pdwEffect)
	{
		return S_OK; // Disable normal drags for now.
	}
	virtual bool Synchronize(SyncInfo & sync);
	virtual bool OpenPreviousEditor(int dxpCursor = 0, bool fTopCursor = true);
	virtual bool OpenNextEditor(int dxpCursor = 0);
	void SetNeedSync(bool fNeedSync)
	{
		m_fNeedSync = fNeedSync;
	}
	bool GetNeedSync()
	{
		return m_fNeedSync;
	}
	virtual bool CloseProj();
#if WantWWStuff
	bool CreateInflAffixItem(int clid);
#endif

protected:
	bool CmdDelete(Cmd * pcmd);
	void OnContextMenu(HWND hwnd, POINT pt);

	virtual void AddField(HVO hvoRoot, int clid, int nLev, FldSpec * pfsp,
		CustViewDa * pcvd, int & idfe, int nInd = 0, bool fAlwaysVisible = false);
	virtual void AddContextInsertItems(HMENU & hmenu);
	virtual HVO LaunchRefChooser(AfDeFeRefs * pdfr);
	virtual bool ConfirmDeletion(int flid, bool & fAtomic, bool & fTopLevelObj,
			int & kstid, bool & fHasRefs);
	virtual void LoadOtherData(IOleDbEncap * pode, CustViewDa * pcvd, HvoClsid & hcRoot);
	virtual bool IsSubitemFlid(int flid)
	{
		return false;
	}
	virtual bool InsertEntry(Cmd * pcmd);
	virtual bool OnLButtonDown(uint grfmk, int xp, int yp);
	virtual bool OnRButtonDown(uint grfmk, int xp, int yp);
	virtual void PromoteSetup(HVO hvo);

	//:>****************************************************************************************
	//:>	Message handlers.
	//:>****************************************************************************************

	//:>****************************************************************************************
	//:>	Command functions.
	//:>****************************************************************************************

	// member variables

	// This is used to signal that a user modified a name or abbreviation that requires updates
	// that may result in closing and reopening field editors. If that happens within the
	// EndEdit method, we get into serious trouble in places such as OpenNextEditor where we
	// immediately open another editor. So instead of doing the updating in EndEdit, it sets
	// this flag, and then we catch it at a more appropriate time and do the updates. It will
	// be cleared in when moving to the next field, in case the editor happened to be closed
	// in some other way.
	bool m_fNeedSync;

	CMD_MAP_DEC(CleDeSplitChild);
};

/*----------------------------------------------------------------------------------------------
	Provides a field editor to display a single string or a vector of strings. It can be used
	for String, Unicode, MultiStrings, or MultiUnicode. MultiStrings must provide at least one
	writing system in a vector as an argument to the constructor. In multi-string mode, each
	alternative is displayed in a separate paragraph starting with a superscript writing system
	abbreviation.When active, it creates a view window to handle the editing.

	@h3{Hungarian: cdfs}
----------------------------------------------------------------------------------------------*/
class CleDeFeString : public AfDeFeString
{
public:
	typedef AfDeFeString SuperClass;

	CleDeFeString()
	{
	}

	~CleDeFeString()
	{
	}

	void CleDeFeString::EndEdit(bool fForce = false);
	bool IsOkToClose(bool fWarn = true);
	bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true);

protected:
	// We need to find out if the primary string changed. Since changes are written directly
	// to the view cache and the database as they happen, and because the string in the
	// PossList cache may be a different writing system, the only way we can keep track of this is
	// to cache the original string when we get started.
	ITsStringPtr m_qtssOld;
};


#endif // CLE_DE_SPLIT_CHILD_WND_INCLUDED
