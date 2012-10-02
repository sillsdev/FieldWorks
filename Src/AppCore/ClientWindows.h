/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ClientWindows.h
Responsibility: Ken Zook
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		AfClientRecWnd : AfSplitterClientWnd - This class supports record-based data
				in a language project as well as the functionality supported by RecMainWnd.
			AfClientRecDeWnd : AfClientRecWnd
			AfClientRecVwWnd : AfClientRecWnd
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef ClientWindows_H
#define ClientWindows_H 1

class AfClientRecWnd;
class AfClientRecDeWnd;
class AfClientRecVwWnd;
class AfDeSplitChild;

typedef GenSmartPtr<AfClientRecWnd> AfClientRecWndPtr;
typedef GenSmartPtr<AfClientRecDeWnd> AfClientRecDeWndPtr;
typedef GenSmartPtr<AfClientRecVwWnd> AfClientRecVwWndPtr;

/*----------------------------------------------------------------------------------------------
	Client window for apps with records.

	@h3{Hungarian: afcrw}
----------------------------------------------------------------------------------------------*/
class AfClientRecWnd : public AfSplitterClientWnd
{
public:
	typedef AfSplitterClientWnd SuperClass;

	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew);
	virtual void PrepareToHide();
	virtual void PrepareToShow();
	virtual bool IsOkToChange(bool fChkReq = false);
	virtual void ReloadRootObjects();
	// Fill in clsid and nLevel based on the current selection. (Defaults to doing nothing.)
	virtual bool CloseProj();
	virtual bool Synchronize(SyncInfo & sync);
	virtual HRESULT DispCurRec(BYTE bmk, int drec)
	{
		return S_OK;
	}
	virtual void GetCurClsLevel(int * pclsid, int * pnLevel)
	{
		CurrentPane()->GetCurClsLevel(pclsid, pnLevel);
	}
	virtual void SetPrimarySortKeyFlid(int flid)
	{
		if (m_qsplf && m_qsplf->CurrentPane())
			CurrentPane()->SetPrimarySortKeyFlid(flid);
	}

protected:
	// This keeps track of the hvo of the current record. Its main use is to determine
	// whether the current hvo has changed when something like a view or filter changes.
	HVO m_hvoCurrent;
};

/*----------------------------------------------------------------------------------------------
	Data entry client window for apps with records.

	@h3{Hungarian: crde}
----------------------------------------------------------------------------------------------*/
class AfClientRecDeWnd : public AfClientRecWnd
{
public:
	typedef AfClientRecWnd SuperClass;

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void OnTreeWidthChanged(int dxpTreeWidth, AfDeSplitChild * padsc);
	virtual void GetVwSpInfo(WndSettings * pwndSet);
	virtual HRESULT DispCurRec(BYTE bmk, int drec);
	virtual bool FullRefresh();
};

/*----------------------------------------------------------------------------------------------
	Browse and Document client window for apps with records.

	@h3{Hungarian: crvw}
----------------------------------------------------------------------------------------------*/
class AfClientRecVwWnd : public AfClientRecWnd
{
public:
	typedef AfClientRecWnd SuperClass;

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true)
		{/* Do nothing, yet. */}
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true)
		{/* Do nothing, yet. */}
	virtual HRESULT DispCurRec(BYTE bmk, int drec);
	virtual HRESULT DispRec(HVO hvoItem);
	virtual bool ObjectIsSelected(HVO hvo);
	void SelectWholeObjects(IVwRootBox * prootb);
	virtual bool FullRefresh();
	virtual bool PreSynchronize(SyncInfo & sync);
	virtual void ReloadRootObjects();
	bool GetHelpStrFromPt(Point pt, ITsString ** pptss);

protected:
	virtual void ShowCurrentRec();
	void EnsureRootBoxes();
};

#endif // !ClientWindows_H
