/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: InterlinExample.h
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Interlinear text example application.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef INTERLINEXAMPLE_INCLUDED
#define INTERLINEXAMPLE_INCLUDED 1

class IeApp;
class IeMainWnd;
class IeClientWnd;
typedef GenSmartPtr<IeApp> IeAppPtr;
typedef GenSmartPtr<IeMainWnd> IeMainWndPtr;
typedef GenSmartPtr<IeClientWnd> IeClientWndPtr;

/*----------------------------------------------------------------------------------------------
	Our Interlinear text example application application class.
----------------------------------------------------------------------------------------------*/
class IeApp : public AfApp
{
	typedef AfApp SuperClass;

public:
	IeApp();
	virtual ~IeApp()
		{ } // Do nothing.

	int GetAppNameId()
	{
		return kstidAppName;
	}

protected:
	virtual void Init(void);

	CMD_MAP_DEC(IeApp);
};


/*----------------------------------------------------------------------------------------------
	Our main window frame class.
----------------------------------------------------------------------------------------------*/
class IeMainWnd : public AfMainWnd
{
	typedef AfMainWnd SuperClass;

public:
	virtual ~IeMainWnd()
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

	IeClientWndPtr m_iecw;
};

#define khvoStyleMin 10000

/*----------------------------------------------------------------------------------------------
	A trivial stylesheet that is hooked into a VwCacheDa database.
	Hungarian: iest.
----------------------------------------------------------------------------------------------*/
class IeStylesheet : public AfStylesheet
{
public:
	void Init(ISilDataAccess * psda);

protected:
	HVO m_hvoNextStyle;

	virtual HRESULT GetNewStyleHVO(HVO * phvo);
};

DEFINE_COM_PTR(IeStylesheet);

enum TagsInterlin
{
	ktagFreeform = 100,
	ktagWords,
	ktagBase,
	ktagAnn,
};

enum FragmentsInterlin
{
	kfrSentence,
	kfrSentInterlin,
	kfrSentTable,
	kfrAnnWordBundle,
	kfrAnnWordRow,
	kfrStrLength,
	kfrSentFreeform,
};

/*----------------------------------------------------------------------------------------------
	Our Interlinear text view constructor.
	Hungarian: ievc
----------------------------------------------------------------------------------------------*/
class IeVc : public VwBaseVc
{
public:
	void Init();
	STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvoObj, int frag);
	STDMETHOD(DisplayVariant)(IVwEnv * pvwenv, VARIANT v, int frag,
		ITsString ** pptss);
	STDMETHOD(UpdateProp)(ISilDataAccess * psda, HVO hvoObj, int tag, int frag, ITsString * ptssVal,
		ITsString ** pptssRepVal);
protected:
	ITsStringPtr m_qtssMainWord;
	ITsStringPtr m_qtssGramCat;
	ITsStringPtr m_qtssLetters;
	ITsStringPtr m_qtssFree;
	ITsStringPtr m_qtssWord;
	ITsStringPtr m_qtssCat;
	ITsStringPtr m_qtss30Percent;
	ITsStringPtr m_qtss20Percent;
	ITsStringPtr m_qtssPt6In;
	ITsStringPtr m_qtssRest;

};

DEFINE_COM_PTR(IeVc);

/*----------------------------------------------------------------------------------------------
	Our Interlinear text example application client class.
	Hungarian: hwcw
----------------------------------------------------------------------------------------------*/
class IeClientWnd : public AfVwScrollWnd
{
	typedef AfWnd SuperClass;

public:
	virtual ~IeClientWnd()
		{ } // Do nothing.
	void Init();

protected:
	virtual void MakeRoot(IVwGraphics * pvg, IVwRootBox ** pprootb);

	IeStylesheetPtr m_qiest;
	VwCacheDaPtr m_qcda;
	HVO m_hvoRoot;
	IeVcPtr m_qievc;
};


#endif // INTERLINEXAMPLE_INCLUDED
