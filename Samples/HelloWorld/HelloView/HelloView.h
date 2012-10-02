/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: HelloView.h
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Hello View.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef HELLOVIEW_INCLUDED
#define HELLOVIEW_INCLUDED 1

class HvApp;
class HvMainWnd;
class HvClientWnd;
typedef GenSmartPtr<HvApp> HvAppPtr;
typedef GenSmartPtr<HvMainWnd> HvMainWndPtr;
typedef GenSmartPtr<HvClientWnd> HvClientWndPtr;

/*----------------------------------------------------------------------------------------------
	Our Hello View application class.
----------------------------------------------------------------------------------------------*/
class HvApp : public AfApp
{
	typedef AfApp SuperClass;

public:
	HvApp();
	virtual ~HvApp()
		{ } // Do nothing.

	int GetAppNameId()
	{
		return kstidAppName;
	}

protected:
	virtual void Init(void);

	CMD_MAP_DEC(HvApp);
};


/*----------------------------------------------------------------------------------------------
	Our main window frame class.
----------------------------------------------------------------------------------------------*/
class HvMainWnd : public AfMainWnd
{
	typedef AfMainWnd SuperClass;

public:
	virtual ~HvMainWnd()
		{ } // Do nothing.

	virtual void OnReleasePtr();

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);

	virtual void RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames,
		Vector<StrUni> & vstuNewNames, Vector<StrUni> & vstuDeletedNames)
	{
	}
protected:
	virtual void PostAttach(void);

	/*------------------------------------------------------------------------------------------
		Message handlers.
	------------------------------------------------------------------------------------------*/
	virtual bool OnSize(int wst, int dxp, int dyp);

	HvClientWndPtr m_hvcw;
};

// an enumeration of "fragment" identifiers for the view constructor is usually a good idea,
// though in this case we only have one. Arbitrarily assign values to these constants so
// we can tell them apart in the debugger.
enum HvFragments
{
	kfrText = 10,
};

// "tags" are usually FLIDs from the database, but in this simple case we'll make another enum.
enum HvTags
{
	ktagProp = 20,
};

/*----------------------------------------------------------------------------------------------
	Our Hello View client class.
	Hungarian: hwcw
----------------------------------------------------------------------------------------------*/
class HvClientWnd : public AfVwWnd
{
	typedef AfWnd SuperClass;

public:
	virtual ~HvClientWnd()
		{ } // Do nothing.

protected:
	virtual void MakeRoot(IVwGraphics * pvg, ILgEncodingFactory * pencf, IVwRootBox ** pprootb);
};

/*----------------------------------------------------------------------------------------------
	Our Hello View view constructor.
	Hungarian: hvvc
----------------------------------------------------------------------------------------------*/
class HvVc : public VwBaseVc
{
	typedef VwBaseVc SuperClass;

public:
	STDMETHOD(Display)(IVwEnv* pvwenv, HVO vwobj, int frag);
};

DEFINE_COM_PTR(HvVc);
#endif // HELLOVIEW_INCLUDED
