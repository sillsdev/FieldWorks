/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: ViewTest.h
Responsibility: Luke Ulrich
Last reviewed: Not yet.

Description: An assortment of classes that test the view code.
	ViewTest1 - tests direct calls to the VwOverlay interface
	ViewTest2 - calls various VwRootBox functions to test the view code in VwGraphics.cpp, .h
	ViewTest3 - executes macro test scripts written via TestViewer.exe

----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef VIEWTEST_INCLUDED
#define VIEWTEST_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: ViewTest1
Description:
Hungarian:
----------------------------------------------------------------------------------------------*/
class ViewTest1 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	ViewTest1();
	virtual ~ViewTest1();

	// Member variable access

	// Other public methods
	HRESULT Run();
	void TestName();
	void TestGuid();
	void TestVwOverlayFlags();
	void TestFontName();
	void TestFontSize();
	void TestMaxShowTags();
	void TestCTags();

	void TestGetDbTagInfo();
	void TestSetTagInfo();
	void TestGetDlgTagInfo();
	void TestGetDispTagInfo();

	void TestSort();
	void TestMerge();
protected:
	// Member variables
	IVwOverlayPtr m_qxvoTest;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};

/*----------------------------------------------------------------------------------------------
Class: ViewTest2
Description:
Hungarian:
----------------------------------------------------------------------------------------------*/

class ViewTest2 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	ViewTest2();
	virtual ~ViewTest2();

	// Member variable access

	// Other public methods
	void TestInit();
	void TestLayout(int width);

	void TestDataAccess();
	void TestOverlay();
	void TestDrawRoot();
	void TestHeight();
	void TestWidth();

	void TestMakeSimpleSel();
	void TestMakeTextSelection();
	void TestSelection();
	void TestOnTyping();
	void TestOnChar();

	void TestFlash();

	void TestKeys();
	void WrapOnTyping(char *pchinput, int _cchBackspace, int _cchDelForward, char *_chFirst,
		RECT _rcSrc, RECT _rcDst);
	void WrapOnChar(int _chw);
	void WrapOnSysChar(int _chw);
	void WrapOnExtendedKey(int _chw, VwShiftStatus _ss);

	void TestMouse();
	void WrapMouseDown(int _xd, int _yd, RECT _rcSrc, RECT _rcDst);
	void WrapMouseMoveDrag(int _xd, int _yd, RECT _rcSrc, RECT _rcDst);
	void WrapMouseDownExtended(int _xd, int _yd, RECT _rcSrc, RECT _rcDst);
	void WrapMouseUp(int _xd, int _yd, RECT _rcSrc, RECT _rcDst);

	void Testget_Site();
	void TestLoseFocus();
	void TestListener();

	HRESULT Run();
protected:
	// Member variables
	VwTestRootSitePtr m_qtrs;
	VwGraphicsPtr m_qvg;
	TestStVcPtr m_qtvc;
	VwCacheDaPtr m_qda;
	IVwRootBoxPtr m_qrootb;

	ISilTestSitePtr m_qst;
	SilTestSite *m_psts;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};

/*----------------------------------------------------------------------------------------------
Class: ViewTest3
Description:
Hungarian:
----------------------------------------------------------------------------------------------*/
class ViewTest3 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	ViewTest3();
	virtual ~ViewTest3();

	// Member variable access

	// Other public methods
	HRESULT Run();
protected:
	// Member variables
	// Object of the "viewclass" TestVwRoot (TestVwRoot.cpp, .h)
	TestVwRoot *m_tvr;
	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};
#endif  //VIEWTEST_INCLUDED
