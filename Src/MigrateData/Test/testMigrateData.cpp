/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testMigrateData.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the MigrateData DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testMigrateData.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
		::OleInitialize(NULL);
		TestMigrateData::g_fVerbose = verbose;
	}
	void GlobalTeardown()
	{
		::OleUninitialize();
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
	}
}

namespace TestMigrateData
{
	// These are the languages (ICU locales) that result from the test database migration.
	// The final abbreviation is the new equivalent for 'mig'
	const char * g_rgpszLanguages[] = { "en", "fr", "es", "pt", "emig" };
	const int g_cLanguages = isizeof(g_rgpszLanguages) / isizeof(const char *);
	bool g_fVerbose = false;
}

// The following minimal implementations provide all that is needed for the AfProgressDlg
// used by the DLL.  Nothing displays anywhere, but we arent' testing progress reporting itself.

void AfProgressDlg::DestroyHwnd()
{
}

STDMETHODIMP AfProgressDlg::QueryInterface(REFIID iid, void ** ppv)
{
	return S_OK;
}

STDMETHODIMP_(ULONG) AfProgressDlg::AddRef(void)
{
	return 0;
}

STDMETHODIMP_(ULONG) AfProgressDlg::Release(void)
{
	return 0;
}

STDMETHODIMP AfProgressDlg::Step(int nStepAmt)
{
	if (TestMigrateData::g_fVerbose)
		putchar('.');
	return S_OK;
}

STDMETHODIMP AfProgressDlg::put_Title(BSTR bstrTitle)
{
	if (TestMigrateData::g_fVerbose)
	{
		StrAnsi staTitle(bstrTitle);
		printf("\n%s\n", staTitle.Chars());
	}
	return S_OK;
}

STDMETHODIMP AfProgressDlg::put_Message(BSTR bstrMessage)
{
	if (TestMigrateData::g_fVerbose)
	{
		StrAnsi staMessage(bstrMessage);
		printf("\n%s\n", staMessage.Chars());
	}
	return S_OK;
}

STDMETHODIMP AfProgressDlg::put_Position(int nPos)
{
	return S_OK;
}

STDMETHODIMP AfProgressDlg::put_StepSize(int nStepInc)
{
	return S_OK;
}

STDMETHODIMP AfProgressDlg::SetRange(int nMin, int nMax)
{
	return S_OK;
}

STDMETHODIMP AfProgressDlg::get_Title(BSTR * pbstrTitle)
{
	*pbstrTitle = NULL;
	return S_OK;
}

STDMETHODIMP AfProgressDlg::get_Message(BSTR * pbstrMessage)
{
	*pbstrMessage = NULL;
	return S_OK;
}

STDMETHODIMP AfProgressDlg::get_Position(int * pnPos)
{
	*pnPos = 0;
	return S_OK;
}

STDMETHODIMP AfProgressDlg::get_StepSize(int * pnStepInc)
{
	*pnStepInc = 1;
	return S_OK;
}

STDMETHODIMP AfProgressDlg::GetRange(int * pnMin, int * pnMax)
{
	*pnMin = 0;
	*pnMax = 1;
	return S_OK;
}

void AfProgressDlg::SetTitle(const achar * pszTitle)
{
	if (TestMigrateData::g_fVerbose)
	{
		StrAnsi staTitle(pszTitle);
		printf("\n%s\n", staTitle.Chars());
	}
}

void AfProgressDlg::SetMessage(const achar * pszMsg)
{
	if (TestMigrateData::g_fVerbose)
	{
		StrAnsi staMsg(pszMsg);
		printf("\n%s\n", staMsg.Chars());
	}
}

void AfProgressDlg::SetColors(COLORREF clrBar, COLORREF clrBk)
{
}

void AfProgressDlg::StepIt(int nIncrement)
{
	if (TestMigrateData::g_fVerbose)
		putchar('.');
}

void AfProgressDlg::SetStep(int nStepInc)
{
}

void AfProgressDlg::SetPos(int nNewPos)
{
}

bool AfProgressDlg::CheckCancel()
{
	return false;
}

void AfDialog::DoModeless(struct HWND__ *,int,void *)
{
}

bool AfDialog::OnActivate(bool,long)
{
	return false;
}

bool AfDialog::OnHelp(void)
{
	return false;
}

bool AfProgressDlg::OnCancel(void)
{
	return false;
}

bool AfDialog::OnApply(bool)
{
	return false;
}

bool AfProgressDlg::OnInitDlg(struct HWND__ *,long)
{
	return false;
}

bool AfDialog::Synchronize(struct SyncInfo &)
{
	return false;
}

void AfWnd::OnStylesheetChange(void)
{
}

bool AfDialog::OnHelpInfo(struct tagHELPINFO *)
{
	return false;
}

bool AfWnd::OnDrawChildItem(struct tagDRAWITEMSTRUCT *)
{
	return false;
}

bool AfWnd::OnMeasureChildItem(struct tagMEASUREITEMSTRUCT *)
{
	return false;
}

bool AfDialog::OnNotifyChild(int,struct tagNMHDR *,long &)
{
	return false;
}

bool AfWnd::OnInitMenuPopup(struct HMENU__ *,int,bool)
{
	return false;
}

bool AfDialog::OnCommand(int,int,struct HWND__ *)
{
	return false;
}

void AfWnd::DetachHwnd(struct HWND__ *)
{
}

void AfWnd::SubclassHwnd(struct HWND__ *)
{
}

long AfWnd::DefWndProc(unsigned int,unsigned int,long)
{
	return 0;
}

void AfWnd::AttachHwnd(struct HWND__ *)
{
}

bool AfDialog::FWndProc(unsigned int,unsigned int,long,long &)
{
	return false;
}

void AfWnd::RefreshAll(bool)
{
}

void AfWnd::SaveSettings(const achar *,bool)
{
}

void AfWnd::LoadSettings(const achar *,bool)
{
}

void AfWnd::Show(int)
{
}

void AfWnd::CreateAndSubclassHwnd(class WndCreateStruct &)
{
}

void AfWnd::CreateHwnd(class WndCreateStruct &)
{
}

bool CmdHandler::FindCmdMapEntry(int,unsigned int,struct CmdHandler::CmdMapEntry * *)
{
	return false;
}

bool CmdHandler::FSetCmdState(class CmdState &,bool &)
{
	return false;
}

bool CmdHandler::FDoCmd(class Cmd *)
{
	return false;
}

AfDialog::~AfDialog(void)
{
}

AfDialog::AfDialog(void)
{
}

struct CmdHandler::CmdMap CmdHandler::s_cmdm;

void AfDialog::HandleDlgMessages(struct HWND__ *,bool)
{
}

bool AfDialog::OnCancel(void)
{
	return false;
}

bool AfDialog::OnInitDlg(struct HWND__ *,long)
{
	return false;
}

void AfWnd::DestroyHwnd(void)
{
}

AfWnd::~AfWnd(void)
{
}

AfWnd::AfWnd(void)
{
}

bool AfWnd::OnNotifyChild(int,struct tagNMHDR *,long &)
{
	return false;
}

bool AfWnd::OnCommand(int,int,struct HWND__ *)
{
	return false;
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkmig-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
