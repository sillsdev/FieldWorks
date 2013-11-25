/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UniscribeEngine.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UNISCRIBEENGINE_INCLUDED
#define UNISCRIBEENGINE_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: DisableMultiscribe
Description: This is used to temporarily and safely disable multiscribe if it is loaded.
Hungarian:
----------------------------------------------------------------------------------------------*/
class DisableMultiscribe
{
public:
	DisableMultiscribe() : m_run(true)
	{
#if WIN32
		if (!s_multiscribeHandleRetrieved)
		{
			HMODULE handle = GetModuleHandle(L"multiscribe.dll");
			if (handle != NULL)
				s_setMultiscribeEnabled = (SetMultiscribeEnabledFunc) GetProcAddress(handle, "SetMultiscribeEnabled");
			s_multiscribeHandleRetrieved = true;
		}

		if (s_setMultiscribeEnabled != NULL)
			s_setMultiscribeEnabled(false);
#endif
	}

	bool Run()
	{
		return m_run;
	}

	void Stop()
	{
		m_run = false;
	}

	~DisableMultiscribe()
	{
#if WIN32
		if (s_setMultiscribeEnabled != NULL)
			s_setMultiscribeEnabled(true);
#endif
	}

protected:
	typedef void (*SetMultiscribeEnabledFunc)(bool enabled);

	static SetMultiscribeEnabledFunc s_setMultiscribeEnabled;
	static bool s_multiscribeHandleRetrieved;

	SetMultiscribeEnabledFunc m_setMultiscribeEnabled;
	bool m_run;
};

#define DISABLE_MULTISCRIBE for (DisableMultiscribe disableMultiscribe; disableMultiscribe.Run(); disableMultiscribe.Stop())

/*----------------------------------------------------------------------------------------------
Class: UniscribeEngine
Description:
Hungarian: rre
----------------------------------------------------------------------------------------------*/
class UniscribeEngine : public IRenderEngine
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IRenderEngine methods
	STDMETHOD(InitRenderer)(IVwGraphics * pvg, BSTR bstrData);
	STDMETHOD(FontIsValid)();
	STDMETHOD(get_SegDatMaxLength)(int * cb);
	STDMETHOD(FindBreakPoint)(IVwGraphics * pvg, IVwTextSource * pts, IVwJustifier * pvjus,
		int ichMin, int ichLim, int ichLimBacktrack,
		ComBool fNeedFinalBreak, ComBool fStartLine, int dxMaxWidth,
		LgLineBreak lbPref, LgLineBreak lbMax, LgTrailingWsHandling twsh, ComBool fParaRtoL,
		ILgSegment ** ppsegRet, int * pdichLimSeg, int * pdxWidth, LgEndSegmentType * pest,
		ILgSegment * psegPrev);
	STDMETHOD(get_ScriptDirection)(int * pgrfsdc);
	STDMETHOD(get_ClassId)(GUID * pguid);
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** ppwsf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);

	// Other public methods

protected:
	// Member variables
	long m_cref;

	// Writing system factory used by this rendering engine.
	ILgWritingSystemFactoryPtr m_qwsf;

	// Static methods

	// Constructors/destructors/etc.
	UniscribeEngine();
	virtual ~UniscribeEngine();

	// Other protected methods
//	void AdjustEndForWidth(IVwGraphics * pvg);
	void FindLineBreak(const byte * prglbs, const int ichMin, const int ichLim,
		const LgLineBreak lbrkRequired, const bool fBackFromEnd, int & ichBreak, int & ichDim);
	bool RemoveTrailingWhiteSpace(int ichMinRun, int * pichLimSeg, UniscribeRunInfo & uri);
	void RemoveNonWhiteSpace(int ichMinRun, int * pichLimSeg, UniscribeRunInfo & uri);
	int * CalculateStretchValues(int cglyph, const Vector<int>& vcst);
};

#endif  //UNISCRIBEENGINE_INCLUDED
