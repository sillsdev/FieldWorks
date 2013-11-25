/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwBaseVc.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This provides a base class for implementing IVwViewConstructor.
	It implements IUnknown, and default (return E_NOTIMPL) implementations of all the methods.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


static DummyFactory g_fact(_T("SIL.FwViews.VwBaseVc"));


VwBaseVc::VwBaseVc()
{
	// COM object behavior
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

VwBaseVc::~VwBaseVc()
{
	ModuleEntry::ModuleRelease();
}

STDMETHODIMP VwBaseVc::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwViewConstructor)
		*ppv = static_cast<IVwViewConstructor *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them. Most
	subclasses should override.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::Display(IVwEnv* pvwenv, HVO hvo, int frag)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	This is used for displaying vectors in complex ways. Often not used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::DisplayVec(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	This is used to display integers by showing icons.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::DisplayPicture(IVwEnv * pvwenv,  int hvo, int tag, int val, int frag,
	IPicture ** ppPict)
{
	*ppPict = NULL;
	Assert(false);
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This pair of methods work together to allow (usually basic) properties to be displayed
	as strings in custom ways. Often not used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::DisplayVariant(IVwEnv * pvwenv, int tag, int frag, ITsString ** pptss)
{
	Assert(false);
	return E_NOTIMPL;
}

STDMETHODIMP VwBaseVc::UpdateProp(IVwSelection * pvwsel, HVO hvo, int tag, int frag,
	ITsString * ptssVal, ITsString ** pptssRepVal)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	This routine is used to estimate the height of an item. The item will be one of
	those you have added to the environment using AddLazyItems. Note that the calling code
	does NOT ensure that data for displaying the item in question has been loaded.
	The first three arguments are as for Display, that is, you are being asked to estimate
	how much vertical space is needed to display this item in the available width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::EstimateHeight(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Load data needed to display the specified objects using the specified fragment.
	This is called before attempting to Display an item that has been listed for lazy display
	using AddLazyItems. It may be used to load the necessary data into the DataAccess object.
	If you are not using AddLazyItems this method may be left unimplemented.
	If you pre-load all the data, it should trivially succeed (i.e., without doing anything).
	This is the default behavior.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::LoadDataFor(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
	int tag, int frag, int ihvoMin)
{
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the string that should be displayed in place of an object character associated
	with the specified GUID.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::GetStrForGuid(BSTR bstrGuid, ITsString ** pptss)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Perform whatever action is appropriate when the use clicks on a hot link
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::DoHotLinkAction(BSTR bstrData, ISilDataAccess * psda)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);

	if (BstrLen(bstrData) > 0 && bstrData[0] == kodtExternalPathName)
	{
		StrAppBuf strbFile(bstrData + 1);
		if (::UrlIs(strbFile.Chars(), URLIS_URL))
		{
			// If it's a URL launch whatever it means.
			::ShellExecute(NULL, L"open", strbFile.Chars(), NULL, NULL, SW_SHOWNORMAL);
			return S_OK;
		}
		if (AfApp::Papp())
		{
			AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
			if (pafw && pafw->GetLpInfo())
				pafw->GetLpInfo()->MapExternalLink(strbFile);

			AfApp::LaunchHL(NULL, _T("open"), strbFile.Chars(), NULL, NULL, SW_SHOW);
		}
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

STDMETHODIMP VwBaseVc::GetIdFromGuid(ISilDataAccess * psda, GUID * pguid, HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(psda);
	ChkComArgPtr(pguid);

	IVwOleDbDaPtr qodd;
	CheckHr(psda->QueryInterface(IID_IVwOleDbDa, (void **)&qodd));
	if (!qodd)
		return S_OK; // Can't do anything; ignore.
	CheckHr(qodd->GetIdFromGuid(pguid, phvo));
	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

/*----------------------------------------------------------------------------------------------
	Insert material as appropriate to display the specified object.
	This method has not yet been tested...maybe not even compiled?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::DisplayEmbeddedObject(IVwEnv * pvwenv, HVO hvo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	// See if it is a CmPicture.
	ISilDataAccessPtr qsda;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	int clsid;
	CheckHr(qsda->get_IntProp(hvo, kflidCmObject_Class, &clsid));
	if (clsid != kclidCmPicture)
		return S_OK; // don't know how to deal with it.
	StrUni stuRootDir = DirectoryFinder::FwRootDataDir();
	HVO hvoFile;
	CheckHr(qsda->get_ObjectProp(hvo, kflidCmPicture_PictureFile, &hvoFile));
	if (hvoFile == 0)
		return S_OK;
	SmartBstr sbstrFileName;
	CheckHr(qsda->get_UnicodeProp(hvoFile, kflidCmFile_InternalPath, &sbstrFileName));
	if (sbstrFileName.Length() == 0)
		return S_OK;
	StrUni stuPath;
	stuPath.Format(L"%s,%s,%s", stuRootDir.Chars(), L"\\", sbstrFileName.Chars());
	IPicturePtr qpic;
	try
	{
		IStreamPtr qstrm;
		FileStream::Create(stuPath, STGM_READ, &qstrm);
		STATSTG stg;
		CheckHr(qstrm->Stat(&stg, STATFLAG_NONAME));
		LONG cbdata = (LONG)stg.cbSize.QuadPart;
		CheckHr(::OleLoadPicture(qstrm, cbdata, FALSE, IID_IPicture, (LPVOID *)&qpic));
		CheckHr(pvwenv->AddPicture(qpic, ktagNotAnAttr, 0, 0));
	}
	catch (...)
	{
		return S_OK; // if anything goes wrong (e.g., file not found), just give up for now.
		// Todo: insert a 'file XXX not found string.
	}
	// Todo: also add the caption.

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

/*----------------------------------------------------------------------------------------------
	Do not make any changes to the text props.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwBaseVc::UpdateRootBoxTextProps(ITsTextProps * pttp, ITsTextProps ** ppttp)
{
	*ppttp = NULL;
	return S_OK;
}
