/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: HelloGraphite.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	This file contains the base classes for Hello Graphite.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef HELLOGR_INCLUDED
#define HELLOGR_INCLUDED 1

class HgApp;
class HgMainWnd;
class HgClientWnd;
typedef GenSmartPtr<HgApp> HgAppPtr;
typedef GenSmartPtr<HgMainWnd> HgMainWndPtr;
typedef GenSmartPtr<HgClientWnd> HgClientWndPtr;

/*----------------------------------------------------------------------------------------------
	Our Hello Graphite application class.
	Hungarian: hgapp
----------------------------------------------------------------------------------------------*/
class HgApp : public AfApp
{
	typedef AfApp SuperClass;

public:
	HgApp();
	virtual ~HgApp()
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
	Hungarian: hgw
----------------------------------------------------------------------------------------------*/
class HgMainWnd : public AfMainWnd
{
	typedef AfMainWnd SuperClass;

public:
	virtual ~HgMainWnd()
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

	HgClientWndPtr m_hgcw;
};

// an enumeration of "fragment" identifiers for the view constructor is usually a good idea,
// though in this case we only have one. Arbitrarily assign values to these constants so
// we can tell them apart in the debugger.
enum HgFragments
{
	kfrText = 10,
};

// "tags" are usually FLIDs from the database, but in this simple case we'll make another enum.
enum HgTags
{
	ktagProp = 20,
};

/*----------------------------------------------------------------------------------------------
	Our Hello Graphite client class.
	Hungarian: hgcw
----------------------------------------------------------------------------------------------*/
class HgClientWnd : public AfVwWnd
{
	typedef AfWnd SuperClass;

public:
	virtual ~HgClientWnd()
		{ } // Do nothing.

protected:
	// Member variables:
	int m_nSize;

	// Protected methods:
	virtual void MakeRoot(IVwGraphics * pvg, IVwRootBox ** pprootb);
	void ReadInitFile(ITsString ** pptss);
};

/*----------------------------------------------------------------------------------------------
	Our Hello Graphite view constructor.
	Hungarian: hgvc
----------------------------------------------------------------------------------------------*/
class HgVc : public VwBaseVc
{
	typedef VwBaseVc SuperClass;

public:
	STDMETHOD(Display)(IVwEnv* pvwenv, HVO vwobj, int frag);

	void SetFontSize(int mp)
	{
		m_mpSize = mp;
	}
	int FontSize()
	{
		return m_mpSize;
	}

protected:
	int m_mpSize;
};

DEFINE_COM_PTR(HgVc);
#endif // HELLOGR_INCLUDED
