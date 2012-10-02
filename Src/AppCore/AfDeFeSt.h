/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeSt.h
Responsibility: John Thomson
Last reviewed: never

Description:
	A superclass for field editors that contain a view: that is, the field editor creates a
	root box and displays it; when active, it creates an appropriate view window to handle
	editing.
	A base class for the view window is also implemented here: AfDeVw.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfDeFeSt_INCLUDED
#define AfDeFeSt_INCLUDED 1


/*----------------------------------------------------------------------------------------------
	A superclass for field editors based on a view.
	Hungarian: dfv.
----------------------------------------------------------------------------------------------*/
class AfDeFeSt : public AfDeFeVw
{
public:
	typedef AfDeFeVw SuperClass;

	AfDeFeSt(int ws = 0);
	virtual ~AfDeFeSt();
	void Init(CustViewDa * pcvd, HVO hvoText);

	void SetDa(CustViewDa * pcvd)
	{
		m_qcvd = pcvd;
	}

	void SetText(HVO hvoText)
	{
		m_hvoText = hvoText;
	}

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb);
	virtual bool IsEditable()
	{
		return true;
	}
	virtual bool IsDirty();
	// Saves changes and returns true.
	// If the data is illegal, this should return false without saving the changes.
	// In this case, it should also raise an error for the user so they know what to fix.
	virtual bool SaveEdit();

	// Saves changes, destroys the window, and sets m_hwnd to 0.
	// If error checking is needed, IsOkToClose() should be called before this.
	virtual void EndEdit(bool fForce = false);
	virtual void UpdateField();
	virtual FldReq HasRequiredData();

	virtual bool OnMouseMove(uint grfmk, int xp, int yp);
	virtual void SaveCursorInfo();
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur);

protected:
	StVcPtr m_qstvc; // std view constructor for structured texts.
	CustViewDaPtr m_qcvd;
	HVO m_hvoText; // The id of the structured text we are editing.
	// This is a special dummy data access object used if the object is missing.
	IVwCacheDaPtr m_qvcdMissing;
};


#endif // AfDeFeSt_INCLUDED
