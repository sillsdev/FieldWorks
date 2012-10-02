/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeCliRef.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This is a data entry field editor for atomic reference fields.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_CLIREF_INCLUDED
#define AFDEFE_CLIREF_INCLUDED 1


/*----------------------------------------------------------------------------------------------
	This node is used to provide tree structures in data entry editors.
	Hungarian: decr.
----------------------------------------------------------------------------------------------*/

class AfDeFeCliRef : public AfDeFeEdBoxBut, public PossListNotify
{
public:
	typedef AfDeFeEdBoxBut SuperClass;

	AfDeFeCliRef();
	~AfDeFeCliRef();

	void SetContents(ITsString * ptss)
	{
		AssertPtrN(ptss);
		m_qtss = ptss;
	}

	void SetList(HVO pssl)
	{
		m_hvoPssl = pssl;
	}

	void SetItem(HVO pss)
	{
		m_pss = pss;
	}

	void SetHier(bool f)
	{
		m_fHier = f;
	}

	void InitContents(bool fHier, PossNameType pnt = kpntName);
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true,
		TptEditable tpte = ktptIsEditable);
	virtual bool IsDirty();
	virtual bool SaveEdit();
	virtual void ProcessChooser();
	virtual bool OnChange(AfDeFeEdBoxBut::DeEdit * pedit);
	virtual void UpdateField();
	virtual void EndEdit(bool fForce = false);
	virtual void ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc, int ipssDst);
	virtual void OnReleasePtr();
	virtual void ChooserApplied(PossChsrDlg * pplc);

protected:
	bool m_fRecurse; // Flag to stop recursion.
	HVO m_pss; // The id for the possibility item corresponding to m_qtss.
	HVO m_hvoPssl; // The id for the possibility list that owns this item (when it is a pss).
	bool m_fHier; // True if we are using hierarchical names.
	PossNameType m_pnt; // Determines whether we show name, abbr, or both for poss items.
	// This flag is used to skip notifications from PossListInfo when we are in the
	// middle of adding a new item. Without this, the update temporarily flashes the old back
	// while we are processing the new list.
	bool m_fSaving;
};

#endif // AFDEFE_CLIREF_INCLUDED
