/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: TestVwRoot.h
Responsibility: Luke Ulrich
Last reviewed: Not yet.

Description: OBJECT => a modular class that can easily be integrated into both TestViewer.exe
			 and the Testharness as an efficient way of recording and executing interactive
			 test scripts

	Contains specific instructions and functions for properly executing and recording interactive
	test scripts. Most often this class will only be used by the TestHarness and TestViewer
	If the test harness is being used, the user should define TESTHARNESS in order to
	permit simulation of such things as a real rootbox (which is present with TestViewer), etc

----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef TESTVWROOT_INCLUDED
#define TESTVWROOT_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: TestVwRoot
Description:
Hungarian:
----------------------------------------------------------------------------------------------*/
class TestVwRoot : public MacroBase
{
private:
	// Using the test harness?
	bool m_fth;
#ifdef TESTHARNESS
	// Since were running under the TestHarness need special processing to
	// get valid pointers to certain objects (e.g. m_qtrs, m_qtvc, (see below))
	void Init();
#endif

public:
	// fDraw specifies whether to perform actual drawing routines, fBsl determines
	// whether we want to write to a baseline file or not
	TestVwRoot(bool fDraw, bool fBsl);
	~TestVwRoot();

	// Returns a SilTestSite (STS) pointer
	SilTestSite *GetSTSPtr();
	// Mainly useful to TestViewer.exe to flag which RootBox to write to
	void SetRootBox(IVwRootBoxPtr qrootb);
	// Allowing to set the graphics pointer
	void SetVwGraphicsPtr(VwGraphicsPtr qvg);
	void SetBaselineFile(StrUni stuBslFile);
	void SetOutfile(StrAnsi strName);
	// Function to execute a test script
	virtual void RunMacro(StrAnsi staMcrFile, StrUni stuBslFile);
	// Executes one line of code at a time
	virtual int RunLine();
	virtual void RunString(string str);
protected:
	// Member variables
	VwGraphicsPtr m_qvg;
	VwCacheDaPtr m_qda;
	IVwRootBoxPtr m_qrootb;
#ifdef TESTHARNESS
	VwTestRootSitePtr m_qtrs;
	TestStVcPtr m_qtvc;
#endif

	// Variables specific to baselining
	ISilTestSitePtr m_qst;
	SilTestSite *m_psts;

	// Draw? Baseline?
	bool m_fDraw, m_fBsl;

public:

	// Setup functions required for testharness to operate correctly using this test
	// Configure the rootbox appropiately
	void DoLayout(int width);
	void DoDrawRoot(RECT rcSrcRoot, RECT rcDstRoot);

	// Possibly incorporate these functions at a later time
//	void WrapMakeSimpleSel(ComBool fInitial, ComBool fEdit, ComBool fRange);
//	void TestMakeTextSelection();
//	void TestSelection();

	// The following "Do" functions override virtual methods declared in the AfVwRootSite
	// class (AfVwWnd.cpp, .h). This allows for special processing due for testing purposes
	void DoOnTyping(VwGraphicsPtr qvg, SmartBstr _bstr, int cchBackspace, int cchDelForward, OLECHAR oleChar,
								RECT rcSrc, RECT rcDst);
	// The DoOnChar function gets called through OnTyping
//	void DoOnChar(int _chw);
	void DoOnSysChar(int _chw);
	void DoOnExtendedKey(int _chw, VwShiftStatus _ss);

	void DoMouseDown(int _xd, int _yd, RECT _rcSrc, RECT _rcDst);
	void DoMouseMoveDrag(int _xd, int _yd, RECT _rcSrc, RECT _rcDst);
	void DoMouseDownExtended(int _xd, int _yd, RECT _rcSrc, RECT _rcDst);
	void DoMouseUp(int _xd, int _yd, RECT _rcSrc, RECT _rcDst);
};

#endif