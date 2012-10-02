/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeUni.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This class provides for nested field editors.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_UNI_INCLUDED
#define AFDEFE_UNI_INCLUDED 1

//class DeuEdit;

/*----------------------------------------------------------------------------------------------
	This node is used to provide tree structures in data entry editors.
	Hungarian: deu.
----------------------------------------------------------------------------------------------*/

class AfDeFeUni : public AfDeFieldEditor
{
public:
	typedef AfDeFieldEditor SuperClass;

	AfDeFeUni();
	~AfDeFeUni();

	virtual void Init();
	virtual bool IsTextSelected();
	virtual void DeleteSelectedText();
	void Draw(HDC hdc, const Rect & rcpClip);
	int SetHeightAt(int dxpWidth);

	// This field can be made editable
	virtual bool IsEditable()
	{
		return true;
	}

	virtual bool IsDirty();
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true)
	{
		return BeginEdit(hwnd, rc, dxpCursor, fTopCursor, ktptSemiEditable);
	}
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true,
		TptEditable tpte = ktptIsEditable);
	virtual bool SaveEdit();
	virtual void EndEdit(bool fForce = false);
	virtual void MoveWnd(const Rect & rcClip);
	virtual void UpdateField();
	virtual bool ValidKeyUp(UINT wp)
	{
		return true;
	}
	virtual FldReq HasRequiredData();
	virtual IActionHandler * BeginTempEdit();
	virtual IActionHandler * EndTempEdit();
	virtual void SaveCursorInfo();
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur);

	class DeuEdit : public TssEdit
	{
	public:
	friend AfDeFeUni;
		typedef TssEdit SuperClass;
	protected:
		virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
		void PostAttach(void);
		int DxpCursorOffset();

		AfDeFeUni * m_pdeu;
		ITsStringPtr m_qtssOld;
	};

protected:
	ITsStringPtr m_qtss; // The text to display/edit for this field.
};

typedef GenSmartPtr<AfDeFeUni::DeuEdit> DeuEditPtr;



#endif // AFDEFE_UNI_INCLUDED
