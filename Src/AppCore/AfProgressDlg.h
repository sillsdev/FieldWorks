/*-----------------------------------------------------------------------*//*:Ignore in Surveyor

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfProgressDlg.h
Responsibility: Steve McConnel
Last reviewed: never

Description:

	AfProgressDlg : AfDialog
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFPROGRESSDLG_H_INCLUDED
#define AFPROGRESSDLG_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides a simple modeless dialog for displaying a message and a progress bar.
	This is useful for cases where the status bar of the current main window is either
	unavailable, or inappropriate, for displaying the progress information during a lengthy
	operation.

	Hungarian: prog.
----------------------------------------------------------------------------------------------*/
class AfProgressDlg : public AfDialog, public IAdvInd4, public IAdvInd3
{
typedef AfDialog SuperClass;
public:
	AfProgressDlg()
	{
		m_rid = kridProgressDlg;
		m_nLowLim = 0;
		m_nHighLim = 100;
		m_hcur = 0;
		m_hcurOld = 0;
		m_hcurOldProg = 0;
	}
	~AfProgressDlg()
	{
		DestroyHwnd();
	}

	//:> IAdvInd methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);
	STDMETHOD(Step)(int nStepAmt);

	//:> IAdvInd3 methods
	STDMETHOD(put_Title)(BSTR bstrTitle);
	STDMETHOD(put_Message)(BSTR bstrMessage);
	STDMETHOD(put_Position)(int nPos);
	STDMETHOD(put_StepSize)(int nStepInc);
	STDMETHOD(SetRange)(int nMin, int nMax);

	//:> IAdvInd4 methods
	STDMETHOD(get_Title)(BSTR * pbstrTitle);
	STDMETHOD(get_Message)(BSTR * pbstrMessage);
	STDMETHOD(get_Position)(int * pnPos);
	STDMETHOD(get_StepSize)(int * pnStepInc);
	STDMETHOD(GetRange)(int * pnMin, int * pnMax);

	//:> Other methods.
	void SetTitle(const achar * pszTitle);
	void SetMessage(const achar * pszMsg);
	void SetColors(COLORREF clrBar, COLORREF clrBk);	// PBM_SETBARCOLOR and PBM_SETBKCOLOR
	void StepIt(int nIncrement = 0);					// PBM_DELTAPOS or PBM_STEPIT
	void SetStep(int nStepInc);							// PBM_SETSTEP
	void SetPos(int nNewPos);							// PBM_SETPOS

	// Set the dialog to use a Cancel button, and store the address of a flag to set true
	// if the user clicks on the Cancel button.
	void SetCanceledFlag(bool * pfCancelled)
	{
		if (pfCancelled)
		{
			m_rid = kridProgressWithCancelDlg;
			m_pfCancelled = pfCancelled;
			*m_pfCancelled = false;
		}
	}
	void SetCursor(LPCTSTR cursor)
	{
		m_hcur = ::LoadCursor(NULL, cursor);
	}
	bool CheckCancel();
	virtual void DestroyHwnd();

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnCancel();

	int m_nLowLim;			// The lower limit of the progress bar values.
	int m_nHighLim;			// The upper limit of the progress bar values.
	int m_nStep;			// The default amount by which to increment the progress bar.
	int m_nCurrent;			// The current progress bar value.
	bool * m_pfCancelled;
	HWND m_hwndProgress;	// Handle to the progress bar.
	HCURSOR m_hcur;
	HCURSOR m_hcurOld;
	HCURSOR m_hcurOldProg;	// Probably overkill, but ...
};

typedef GenSmartPtr<AfProgressDlg> AfProgressDlgPtr;


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkaflib.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*AFPROGRESSDLG_H_INCLUDED*/
