/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: HelloWorld.h
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Hello World.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef HELLOWORLD_INCLUDED
#define HELLOWORLD_INCLUDED 1

class HwApp;
class HwMainWnd;
class HwClientWnd;
typedef GenSmartPtr<HwApp> HwAppPtr;
typedef GenSmartPtr<HwMainWnd> HwMainWndPtr;
typedef GenSmartPtr<HwClientWnd> HwClientWndPtr;

/*----------------------------------------------------------------------------------------------
	Our Hello World application class.
----------------------------------------------------------------------------------------------*/
class HwApp : public AfApp
{
	typedef AfApp SuperClass;

public:
	HwApp();
	virtual ~HwApp()
		{ } // Do nothing.

	int GetAppNameId()
	{
		return kstidAppName;
	}

protected:
	virtual void Init(void);

	CMD_MAP_DEC(HwApp);
};


/*----------------------------------------------------------------------------------------------
	Our main window frame class.
----------------------------------------------------------------------------------------------*/
class HwMainWnd : public AfMainWnd
{
	typedef AfMainWnd SuperClass;

public:
	virtual ~HwMainWnd()
		{ } // Do nothing.

	virtual void OnReleasePtr();

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);

	virtual void RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames,
		Vector<StrUni> & vstuNewNames, Vector<StrUni> & vstuDelNames)
	{
		// TODO (SharonC): Implement as desired.
	}

protected:
	virtual void PostAttach(void);

	/*------------------------------------------------------------------------------------------
		Message handlers.
	------------------------------------------------------------------------------------------*/
	virtual bool OnSize(int wst, int dxp, int dyp);

	HwClientWndPtr m_qhwcw;
};


/*----------------------------------------------------------------------------------------------
	Our Hello World client class.
	Hungarian: hwcw
----------------------------------------------------------------------------------------------*/
class HwClientWnd : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	virtual ~HwClientWnd()
		{ } // Do nothing.

protected:
	virtual bool OnPaint(HDC hdcDef);
};


#endif // HELLOWORLD_INCLUDED