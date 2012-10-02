/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: DllClient.h
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Dll Client application.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef DLLCLIENT_INCLUDED
#define DLLCLIENT_INCLUDED 1

class DcApp;
class DcMainWnd;
class DcClientWnd;
typedef GenSmartPtr<DcApp> DcAppPtr;
typedef GenSmartPtr<DcMainWnd> DcMainWndPtr;
typedef GenSmartPtr<DcClientWnd> DcClientWndPtr;

/*----------------------------------------------------------------------------------------------
	Our Dll Client application application class.
----------------------------------------------------------------------------------------------*/
class DcApp : public AfApp
{
	typedef AfApp SuperClass;

public:
	DcApp();
	virtual ~DcApp()
		{ } // Do nothing.

	int GetAppNameId()
	{
		return kstidAppName;
	}

protected:
	virtual void Init(void);

	CMD_MAP_DEC(DcApp);
};


/*----------------------------------------------------------------------------------------------
	Our main window frame class.
----------------------------------------------------------------------------------------------*/
class DcMainWnd : public AfMainWnd
{
	typedef AfMainWnd SuperClass;

public:
	virtual ~DcMainWnd()
		{ } // Do nothing.

	virtual void OnReleasePtr();

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);

protected:
	virtual void PostAttach(void);

	/*------------------------------------------------------------------------------------------
		Message handlers.
	------------------------------------------------------------------------------------------*/
	virtual bool OnSize(int wst, int dxp, int dyp);

	DcClientWndPtr m_dccw;
};


/*----------------------------------------------------------------------------------------------
	Our Dll Client application client class.
	Hungarian: hwcw
----------------------------------------------------------------------------------------------*/
class DcClientWnd : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	virtual ~DcClientWnd()
		{ } // Do nothing.

protected:
	virtual bool OnPaint(HDC hdcDef);
};


#endif // DLLCLIENT_INCLUDED
