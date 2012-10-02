/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2009 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwTextStoreX11.h
Responsibility:
Last reviewed: Not yet.

Description:
	Defines the class VwTextStore which is implemented in terms of IIMEKeyboardSwitcher
	This is only used on Linux.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwTextStore_INCLUDED
#define VwTextStore_INCLUDED

class __declspec(uuid("52049bc0-9493-11dd-ad8b-0800200c9a66")) VwTextStore;
#define CLSID_VwTextStore __uuidof(VwTextStore)

DEFINE_COM_PTR(IIMEKeyboardSwitcher);
/*----------------------------------------------------------------------------------------------
	Class: VwTextStore
	Description: implement the ITextStoreACP interface as a wrapper around a VwRootBox.
	Hungarian: txs
----------------------------------------------------------------------------------------------*/
class VwTextStore : public IUnknown
{
public:
	VwTextStore(VwRootBox * prootb);
	~VwTextStore();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID, LPVOID*);
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
public:
	void OnDocChange();
	void OnSelChange(int nHow);
	void OnLayoutChange();
	void SetFocus();
	void Init();
	void Close();
	void AddToKeepList(LazinessIncreaser *pli);
	bool MouseEvent(int xd, int yd, RECT rcSrc1, RECT rcDst1, VwMouseEvent me);

protected:
	/////
	// Existing Member variables from VwTextStore.h
	long m_cref;
	ILgWritingSystemPtr m_qws;
	VwRootBoxPtr m_qrootb;
	/////

	// C# COM object that switches keyboards.
	IIMEKeyboardSwitcherPtr m_qkbs;

	// Stores if this TextStore has focus.
	bool m_focused;

	// Set this to false to suppress all notifications, typically while we do something
	// through the interface that would produce a notification if done any other way.
	bool m_fLocked;
	DWORD m_dwLockType;
	bool m_fPendingLockUpgrade;
	bool m_fLayoutChanged;	// Set when TSF changes layout but can't notify until lock released.

	// This is set true if a property is updated without normalization (and therefore not
	// in the database) in order to avoid messing up compositions.
	bool m_fCommitDuringComposition;
	bool m_fDoingRecommit;

	/////
	// Existing Methods  from VwTextStore.h

	void GetCurrentWritingSystem();
	////

public:
	VwParagraphBox * m_pvpboxCurrent;

	// Internal methods.

	bool _LockDocument(DWORD dwLockFlags)
	{
		if (m_fLocked)
			return false;
		m_fLocked = true;
		m_dwLockType = dwLockFlags;
		return true;
	}

	void _UnlockDocument()
	{
		m_fLocked = false;
		m_dwLockType = 0;

		// if there is a pending lock upgrade, grant it
		if (m_fPendingLockUpgrade)
		{
			m_fPendingLockUpgrade = false;
		}

		// if any layout changes occurred during the lock, notify the manager
		if (m_fLayoutChanged)
		{
			m_fLayoutChanged = false;
		}
	}

	VwTextSelection * GetStartAndEndBoxes(VwParagraphBox ** ppvpboxStart,
		VwParagraphBox ** ppvpboxEnd, bool * pfEndBeforeAnchor = NULL);
	int TextLength();
	int ComputeBoxAndOffset(int acp, VwParagraphBox * pvpboxFirst, VwParagraphBox * pvpboxLast,
		VwParagraphBox ** ppvpboxOut);
	void CreateNewSelection(COMINT32 acpStart, COMINT32 acpEnd, bool fEndBeforeAnchor,
		VwTextSelection ** pptsel);
	void ClearPointersTo(VwParagraphBox * pvpbox);
	void DoDisplayAttrs();
	void TerminateAllCompositions(void);
	// Conditionally terminate all compositions and return false.  Used mostly in MouseEvent().
	bool EndAllCompositions(bool fStop)
	{
		if (fStop)
			TerminateAllCompositions();
		return false;
	}
	void OnLoseFocus();
	DWORD SuspendAdvise(IUnknown ** ppunk);
	bool IsCompositionActive()
	{
		// TODO-Linux: implement
		return false;
		// return m_compositions.Size() > 0;
	}

	void NoteCommitDuringComposition()
	{
		m_fCommitDuringComposition = true;
	}

	bool IsDoingRecommit()
	{
		return m_fDoingRecommit;
	}

	bool IsCompositionInProgress() { return false; }
};

DEFINE_COM_PTR(VwTextStore);

#endif  //VwTextStore_INCLUDED
