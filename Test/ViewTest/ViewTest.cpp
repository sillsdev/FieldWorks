/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ViewTest.cpp
Responsibility: Luke Ulrich
Last reviewed: Not yet.

Description:
	Tests the views aspect of Fieldworks services.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)
#include "..\\..\\src\\views\\main.h"
#include "limits.h"
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

// In reverse order so they get linked into the list in the natural order, 1 first.
static ViewTest1 g_lt1Instance;
static ViewTest2 g_lt2Instance;
static ViewTest3 g_lt3Instance;

/***********************************************************************************************
	ViewTest1 Methods
***********************************************************************************************/

ViewTest1::ViewTest1()
:TestBase(L"VwOverlay tests", 1)
{
}

ViewTest1::~ViewTest1()
{
}

//StrUni ViewTest1::s_stuName(L"System Collater tests");

HRESULT ViewTest1::Run()
{
	m_qxvoTest.CreateInstance(CLSID_VwOverlay);

	TestName();
	TestGuid();
	TestVwOverlayFlags();
	TestFontName();
	TestFontSize();
	TestMaxShowTags();
	TestCTags();
	TestSetTagInfo();
	TestGetDbTagInfo();
	TestGetDlgTagInfo();
	TestGetDispTagInfo();
	TestSort();
	TestMerge();

	return S_OK;
}

void ViewTest1::TestName()
{
	// Sending NULL pointers
	HRESULT	hr = m_qxvoTest->get_Name(NULL);
	if (!FAILED(hr))
		Failure("Call to function: get_Name(NULL) should have returned error on NULL pointer");

	// Comparing results of put_ and subsequent get_
	StrUni stuName1 = L"MyName";
	hr = m_qxvoTest->put_Name(stuName1.Bstr());
	TestHrFail(hr, "Function put_Name failed when passed(by value) a bstr");

	SmartBstr sbstrName1;
	hr = m_qxvoTest->get_Name(&sbstrName1);
	TestHrFail(hr, "Function get_Name failed when passed(by reference) a bstr");

	if (wcscmp(stuName1.Chars(), sbstrName1.Chars()))
		Failure("Name function returned wrong result");
}

void ViewTest1::TestGuid()
{
	OLECHAR rgchGuid[9] = L"B8A6E680";
	OLECHAR rgchGuid2[9];

	// Send NULL pointers
	HRESULT hr = m_qxvoTest->put_Guid(NULL);
	if (!FAILED(hr))
		Failure("Call to function put_Guid(NULL) should have returned an error");

	hr = m_qxvoTest->get_Guid(NULL);
	if (!FAILED(hr))
		Failure("Call to function get_Guid(NULL) should have returned an error");

	// Valid test
	CheckHr(m_qxvoTest->put_Guid(rgchGuid));
	CheckHr(m_qxvoTest->get_Guid(rgchGuid2));

	// Compare the two guids and report, result should be > or < 0
	if (wcscmp(rgchGuid, rgchGuid2))
		Failure("Guid function returned wrong result");
}

void ViewTest1::TestVwOverlayFlags()
{
	// Passing NULL to function
	HRESULT hr = m_qxvoTest->get_Flags(NULL);
	if (!FAILED(hr))
		Failure("Call to function: get_Flags(NULL) should have returned error on NULL pointer");

	// Verifying tag setting and retrieving
	VwOverlayFlags vof;
	hr = m_qxvoTest->get_Flags(&vof);
	TestHrFail(hr, "Function get_Flags failed when passed(by reference) a VwOverlayFlags "
		"object");
	if (vof != NULL)
		Failure("get_Flags call without initialization by put_ should have returned error");

	vof = kfofTagsUseAttribs;
	CheckHr(m_qxvoTest->get_Flags(&vof));
	if (vof != NULL)
		Failure("get_Flags call without initialization should have returned error");

	hr = m_qxvoTest->put_Flags(kfofTagsUseAttribs);
	TestHrFail(hr, "Function put_Flags failed when passed(by value) a VwOverlayFlags enum");
	CheckHr(m_qxvoTest->get_Flags(&vof));
	if (kfofTagsUseAttribs != vof)
		Failure("get_Flags did not return the same value as put_Flags - single tag");

		// Attempting multiple tags
	CheckHr(m_qxvoTest->put_Flags((VwOverlayFlags)(kfofLeadBracket | kfofTrailTag |
			kgrfofTagBelow)));
	CheckHr(m_qxvoTest->get_Flags(&vof));

	if ((VwOverlayFlags)(kfofLeadBracket | kfofTrailTag | kgrfofTagBelow) != vof)
		Failure("get_Flags did not return the same value as put_Flags - multiple tags");
}

void ViewTest1::TestFontName()
{
	// Trying to retrieve via get_ without a previous put_
	// Something to consider is if there can be a default font name in which case this
	// test would not apply
	SmartBstr sbstrName1;
	HRESULT hr = m_qxvoTest->get_FontName(&sbstrName1);
	TestHrFail(hr, "Function get_FontName failed when passed(by reference) a SmartBstr");
	if (sbstrName1 != NULL)
		Failure("get_FontName without initialization via put_ should have returned error");

	// Sending NULL pointers to functions
	// REVIEW LukeU This test is assuming that a font name of zero length is invalid
	hr = m_qxvoTest->put_FontName(NULL);
	if (!FAILED(hr))
		Failure("Call to function: put_FontName(NULL) should have returned error unless "
			"0 character font name is allowed");

	hr = m_qxvoTest->get_FontName(NULL);
	if (!FAILED(hr))
		Failure("Call to function: get_FontName(NULL) should have returned error on NULL "
			"pointer");

	// "put_ing" and "get_ing" and subsequent value comparison
	StrUni stuFontName = L"Garliz";
	hr = m_qxvoTest->put_FontName(stuFontName.Bstr());
	TestHrFail(hr, "Function put_FontName failed when passed(by value) a bstr");

	CheckHr(m_qxvoTest->get_FontName(&sbstrName1));
	if (wcscmp(stuFontName.Chars(), sbstrName1.Chars()))
		Failure("FontName returned wrong result");
}

void ViewTest1::TestFontSize()
{
	// Trying to retrieve via get_ without a previous put_
	int ivar;
	HRESULT hr = m_qxvoTest->get_FontSize(&ivar);
	TestHrFail(hr, "Function get_FontSize failed when passed(by reference) an integer");
	if (ivar != NULL)
		Failure("get_FontSize without initialization via put_ should have returned error");

	// Sending NULL pointers
	hr = m_qxvoTest->put_FontSize(NULL);
	if (!FAILED(hr))
		Failure("Call to function put_FontSize(NULL) should have returned error unless "
			"a font size of 0 is allowed");

	hr = m_qxvoTest->get_FontSize(NULL);
	if (!FAILED(hr))
		Failure("Call to function get_FontSize(NULL) should have returned error on NULL "
			"pointer");

	// "put_ing" and "get_ing" font size
	hr = m_qxvoTest->put_FontSize(12);
	TestHrFail(hr, "Function put_FontSize failed when passed(by value) an integer");

	CheckHr(m_qxvoTest->get_FontSize(&ivar));
	if (ivar != 12)
		Failure("get_FontSize did not return same value as inputted via put_FontSize");

	// Sending erroneous values to put_ function e.g. -12, 53352
	hr = m_qxvoTest->put_FontSize(-12);
	if (!FAILED(hr))
		Failure("put_FontSize(-12) should have returned an error");
}

void ViewTest1::TestMaxShowTags()
{
	// Trying to retrieve via get_ without a previous put_
	int ivar;
	HRESULT hr = m_qxvoTest->get_MaxShowTags(&ivar);
	TestHrFail(hr, "Function get_MaxShowTags failed when passed(by reference) an integer");
	if (ivar != NULL)
		Failure("get_MaxShowTags without initialization via put_ should have returned error");

	// Sending NULL pointers
	hr = m_qxvoTest->get_MaxShowTags(NULL);
	if (!FAILED(hr))
		Failure("Call to function get_MaxShowTags(NULL) should have returned an error");

	// "put_ing" and "get_ing" for MaxShowTags
	hr = m_qxvoTest->put_MaxShowTags(10);
	TestHrFail(hr, "Function put_MaxShowTags failed when passed(by value) an integer");

	CheckHr(m_qxvoTest->get_MaxShowTags(&ivar));
	if (ivar != 10)
		Failure("get_MaxShowTags did not return same value as inputted via put_MaxShowTags");

	// Sending erroneous values to put_ function e.g. -112
	hr = m_qxvoTest->put_MaxShowTags(-112);
	if (!FAILED(hr))
		Failure("put_MaxShowTags(-112) should have returned an error");
}

void ViewTest1::TestCTags()
{
	// Sending NULL pointer to get_CTags only - there is no put_CTags function
	HRESULT hr = m_qxvoTest->get_CTags(NULL);
	if (!FAILED(hr))
		Failure("Call to function get_CTags(NULL) should have returned an error");

	// Calling function normally to test hr
	int ivar;
	hr = m_qxvoTest->get_CTags(&ivar);
	TestHrFail(hr, "Function get_CTags failed when passed(by reference) an integer");
}

void ViewTest1::TestSetTagInfo()
{
	// Trying to retrieve via get_ without a previous put_
	OLECHAR rgchGuid[9] = L"75F2376A";
	HVO hvo;
	SmartBstr sbstrAbbr;
	SmartBstr sbstrName;
	COLORREF clrFore;
	COLORREF clrBack;
	COLORREF clrUnder;
	int ivar;
	ComBool fHidden;

	HRESULT hr = m_qxvoTest->GetTagInfo(rgchGuid, &hvo, &sbstrAbbr, &sbstrName, &clrFore,
		&clrBack, &clrUnder, &ivar, &fHidden);
	if (!FAILED(hr))
		FailureFormat("Call to function GetTagInfo(invalid params...) should have failed. "
			"hr = %x", hr);

	// Sending NULL pointers for all params, only a few params, etc
	hr = m_qxvoTest->SetTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
	if (!FAILED(hr))
		Failure("Call to function SetTagInfo(NULL...) should have returned an error");

	hr = m_qxvoTest->SetTagInfo(NULL, 23, kosmAll, SmartBstr(L"AAAAA"), SmartBstr(L"ZZZZZ"),
		0x00000000, 0x00FF00FF, 0x0000FF00, 2, false);
	if (!FAILED(hr))
		Failure("Call to function SetTagInfo(NULL, valid params...) should have returned an "
			"error");

	hr = m_qxvoTest->GetTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
	if (!FAILED(hr))
		Failure("Call to function GetTagInfo(NULL...) should have returned an error");

	hr = m_qxvoTest->GetTagInfo(NULL, &hvo, &sbstrAbbr, &sbstrName, &clrFore, &clrBack,
		&clrUnder, &ivar, &fHidden);
	if (!FAILED(hr))
		Failure("Call to function GetTagInfo(NULL, valid params...) should have returned an "
			"error");

	hr = m_qxvoTest->GetTagInfo(rgchGuid, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
	if (!FAILED(hr))
		Failure("Call to function GetTagInfo(GUID, NULL...) should have returned an error");

	hr = m_qxvoTest->GetTagInfo(rgchGuid, NULL, NULL, &sbstrName, NULL, NULL, NULL, NULL,
		&fHidden);
	if (!FAILED(hr))
		Failure("Call to function GetTagInfo(GUID, some invalid params) should have returned an"
			" error");

	// Once set tag info is called, it stays recorded for the life of the test (if it does not
	// fail) The SmartBstr's added here are also used later in the test for the Sort function

	// Test to see if underline type has a range and that it is checked
	hr = m_qxvoTest->SetTagInfo(rgchGuid, 23, kosmAll, SmartBstr(L"AAAAA"), SmartBstr(L"ZZZZZ"),
		0x00000000, 0x00FF00FF, 0x0000FF00, -2342, false);
	if (!FAILED(hr))
		Failure("SetTagInfo with invalid underline type (-2342) should have returned an error");

	hr = m_qxvoTest->SetTagInfo(rgchGuid, 23, kosmAll, SmartBstr(L"AAAAA"), SmartBstr(L"ZZZZZ"),
		0x00000000, 0x00FF00FF, 0x0000FF00, 982347, false);
	if (!FAILED(hr))
		Failure("SetTagInfo with invalid underline type (982347) should have returned an "
			"error");

	// Comparing results of GetTagInfo after previous call to SetTagInfo
	// This is the first valid call to SetTagInfo. This will set one entry into the database at
	// element 0. Also subsequent calls to SetTagInfo with same GUID will replace data, not add
	hr = m_qxvoTest->SetTagInfo(rgchGuid, 23, kosmAll, SmartBstr(L"AAAAA"), SmartBstr(L"ZZZZZ"),
		0x00000000, 0x00FF00FF, 0x0000FF00, 2, false);
	if (FAILED(hr))
		FailureFormat("SetTagInfo function call failed. hr = %x", hr);

	CheckHr(m_qxvoTest->GetTagInfo(rgchGuid, &hvo, &sbstrAbbr, &sbstrName,
		&clrFore, &clrBack, &clrUnder, &ivar, &fHidden));
	if (hvo != 23 || wcscmp(sbstrAbbr.Chars(), L"AAAAA") ||
		wcscmp(sbstrName.Chars(), L"ZZZZZ") || clrFore != 0x00000000 ||
		clrBack != 0x00FF00FF || clrUnder != 0x0000FF00 || ivar != 2 ||	fHidden != false)
		Failure("Function GetTagInfo returned wrong result after previous SetTagInfo"
			" function call");

	// The above SetTagInfo should have set an entry (element 0) into the "database"
	// (what database?) Now try to retrieve via GetDbTagInfo as kind of follow up test
	CheckHr(m_qxvoTest->GetDbTagInfo(0, &hvo, &clrFore, &clrBack, &clrUnder, &ivar, &fHidden,
		rgchGuid));
	if (hvo != 23 || wcscmp(sbstrAbbr.Chars(), L"AAAAA") ||
		wcscmp(sbstrName.Chars(), L"ZZZZZ") || clrFore != 0x00000000 || clrBack != 0x00FF00FF ||
		clrUnder != 0x0000FF00 || ivar != 2 || fHidden != false)
		Failure("Function GetDbTagInfo returned wrong result after previous "
			"SetTagInfo function call");

	// Test with valid GUID but some invalid NULL params - again assuming that its an all
	// params are valid or else the function call should fail
	hr = m_qxvoTest->GetTagInfo(rgchGuid, NULL, NULL, &sbstrName, NULL, &clrBack, NULL, NULL,
		NULL);
	if (!FAILED(hr))
		Failure("GetTagInfo(rgchGuid, some invalid params) should have returned an error");
}

void ViewTest1::TestGetDbTagInfo()
{
	HVO hvo;
	COLORREF clrFore, clrBack, clrUnder;
	int unt;
	ComBool fHidden;
	OLECHAR rgchGuid[9];

	// Sending NULL pointers
	HRESULT hr = m_qxvoTest->GetDbTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
	if(!FAILED(hr))
		Failure("Call to function GetDbTagInfo(NULL...) should have returned an error");

	// Test some non-existent tag vector elements e.g. 15, -2
	hr = m_qxvoTest->GetDbTagInfo(15, &hvo, &clrFore, &clrBack, &clrUnder, &unt, &fHidden,
		rgchGuid);
	if (!FAILED(hr))
		Failure("Call to function GetDbTagInfo and referencing non-existent vector should have "
			"returned an error");

	hr = m_qxvoTest->GetDbTagInfo(-2, &hvo, &clrFore, &clrBack, &clrUnder, &unt, &fHidden,
		rgchGuid);
	if (!FAILED(hr))
		Failure("Call to function GetDbTagInfo and referencing non-existent vector should have "
			"returned an error");

	// Testing parameter interdependencies e.g. last param is NULL because it is a GUID
	hr = m_qxvoTest->GetDbTagInfo(0, &hvo, &clrFore, &clrBack, &clrUnder, &unt, &fHidden,
		NULL);
	if(!FAILED(hr))
		Failure("Call to function GetDbTagInfo with prgchGuid = NULL should have"
			" returned an error");
	// Valid test was performed in the setTagInfo function
}

void ViewTest1::TestGetDlgTagInfo()
{
	OLECHAR rgchGuid[9] = L"BF7056E0";
	SmartBstr sbstrAbbr;
	SmartBstr sbstrName;
	COLORREF clrFore;
	COLORREF clrBack;
	COLORREF clrUnder;
	int ivar;
	ComBool fHidden;

	// Sending NULL pointers
	HRESULT hr = m_qxvoTest->GetDlgTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
	if (SUCCEEDED(hr))
		Failure("Call to function GetDlgTagInfo(NULL...) should have returned an error");

	// Test for non-existent negative tag
	hr = m_qxvoTest->GetDlgTagInfo(-21, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
	if(!FAILED(hr))
		Failure("Function GetDlgTagInfo(-21, NULL...) accepted a non-existent tag setting");

	// Test referencing a non-existent positive tag
	hr = m_qxvoTest->GetDlgTagInfo(234, &fHidden, &clrFore, &clrBack, &clrUnder, &ivar,
		&sbstrAbbr, &sbstrName);
	if (!FAILED(hr))
		Failure("GetDlgTagInfo when passed a non-existent positive tag should have returned an "
			"error");

	CheckHr(m_qxvoTest->SetTagInfo(rgchGuid, 46, kosmAll, SmartBstr(L"BBBBB"), SmartBstr(L"YYYYY"),
		0x00000000, 0x00FF00FF, 0x0000FF00, 4, true));

	int inumtag;
	CheckHr(m_qxvoTest->get_CTags(&inumtag));

	// inumtag holds number of tags in overlay
	// (inumtag - 1) will reference the last tag, if no tags at all, will test for
	// non-existent tag
	CheckHr(m_qxvoTest->GetDlgTagInfo(inumtag-1, &fHidden, &clrFore, &clrBack, &clrUnder, &ivar,
		&sbstrAbbr, &sbstrName));
	if (fHidden != true || clrFore != 0x00000000 || clrBack != 0x00FF00FF
		|| clrUnder != 0x0000FF00 || ivar != 4 || wcscmp(sbstrAbbr.Chars(), L"BBBBB")
		|| wcscmp(sbstrName.Chars(), L"YYYYY"))
		Failure("Function GetDlgTagInfo returned wrong result after previous SetTagInfo "
			"function call");

	hr = m_qxvoTest->GetDlgTagInfo(inumtag-1, NULL, NULL, &clrBack, NULL, &ivar, NULL, NULL);
	if (SUCCEEDED(hr))
		Failure("GetDlgTagInfo(0, some invalid params) should have returned an error");
}

void ViewTest1::TestGetDispTagInfo()
{
	ComBool fHidden;
	COLORREF clrFore, clrBack, clrUnder;
	int unt;
	OLECHAR rgchGuid[9] = L"75F2376A";
	OLECHAR rgchAbbr[11];
	OLECHAR rgchName[20];
	int cchAbbr;
	int inumtag;
	int icchName;
	HRESULT hr;

	// Number of tags in overlay
	CheckHr(m_qxvoTest->get_CTags(&inumtag));

	// Send NULL pointers to all and various params
	hr = m_qxvoTest->GetDispTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
									NULL, NULL);
	if (!FAILED(hr))
		Failure("Function GetDispTagInfo(NULL...) should have returned an error");

	// Attempt to recognize NULL GUID
	hr = m_qxvoTest->GetDispTagInfo(NULL, &fHidden, NULL, NULL, &clrUnder, &unt, rgchAbbr,
		NULL, &cchAbbr, NULL, NULL, NULL);
	if (!FAILED(hr))
		Failure("Function GetDispTagInfo(NULL, some invalid params) should have returned"
		" an error");

	hr = m_qxvoTest->GetDispTagInfo(rgchGuid, &fHidden, &clrFore, &clrBack, &clrUnder,
		&unt, NULL, 3, &cchAbbr, NULL, 2, NULL);
	if (!FAILED(hr))
		FailureFormat("Given null ptr to OLECHAR* should have failed. hr = %x", hr);

	// Valid GetDispTagInfo test
	CheckHr(m_qxvoTest->SetTagInfo(rgchGuid, 92, kosmAll, SmartBstr(L"AAAAA"), SmartBstr(L"ZZZZZ"),
		0x00000000, 0x00FF00FF, 0x0000FF00, 2, false));

	// First run getdbtaginfo() to serve as comparison between this and getdisptaginfo
//	HVO hvo;
//	CheckHr(m_qxvoTest->GetDbTagInfo(0, &hvo, &clrFore, &clrBack, &clrUnder, &unt, &fHidden,
//		rgchGuid));

	hr = m_qxvoTest->GetDispTagInfo(rgchGuid, &fHidden, &clrFore, &clrBack, &clrUnder,
		&unt, rgchAbbr, 10, &cchAbbr, rgchName, 15, &icchName);
	if (FAILED(hr))
		FailureFormat("Call to function GetDispTagInfo failed. hr = %x", hr);

	// Only can compare 4 properties for correct return result
	if (fHidden != false || clrFore != 0x00000000 || clrBack != 0x00FF00FF
		|| clrUnder != 0x0000FF00)
		Failure("Function GetDispTagInfo returned wrong result");
}

void ViewTest1::TestSort()
{
	OLECHAR rgchGuid[9] = L"82FA6903";
	SmartBstr sbstrAbbr1, sbstrAbbr2, sbstrAbbr3;
	SmartBstr sbstrName1, sbstrName2, sbstrName3;
	COLORREF clrFore;
	COLORREF clrBack;
	COLORREF clrUnder;
	int unt;
	ComBool fHidden;
	HRESULT hr;

	// Send NULL pointer
	hr = m_qxvoTest->Sort(NULL);
	if (!FAILED(hr))
		Failure("Call to function Sort(NULL...) should have returned an error");

	// Normal call to sort
	hr = m_qxvoTest->Sort(true);
	TestHrFail(hr, "Call to Sort function failed");

	hr = m_qxvoTest->Sort(false);
	TestHrFail(hr, "Call to sort function failed");

	// Check to see if sort actually sorts the tags
	// First load in another tag with a different Name and Abbr, then compare results after
	// sorts and setting the ComBool prop: fByAbbr
	CheckHr(m_qxvoTest->SetTagInfo(rgchGuid, 3, kosmAll, SmartBstr(L"CCCCC"), SmartBstr(L"XXXXX"),
		0x00000000, 0x00000000, 0x00000000, 3, false));

	// Get the number of tags
	int inumtags;
	CheckHr(m_qxvoTest->get_CTags(&inumtags));

	// Sort by name
	CheckHr(m_qxvoTest->Sort(false));

	// Retrieve data
	// 0
	CheckHr(m_qxvoTest->GetDlgTagInfo(inumtags-3, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
		&sbstrAbbr1, &sbstrName1));
	// 1
	CheckHr(m_qxvoTest->GetDlgTagInfo(inumtags-2, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
		&sbstrAbbr2, &sbstrName2));
	// 2
	CheckHr(m_qxvoTest->GetDlgTagInfo(inumtags-1, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
		&sbstrAbbr3, &sbstrName3));

	// Validate sort
	if (wcscmp(sbstrName1.Chars(), L"XXXXX") || wcscmp(sbstrName2.Chars(), L"YYYYY") ||
		wcscmp(sbstrName3.Chars(), L"ZZZZZ"))
		Failure("Sort(false, ...) by Name did not work");

	// Sort by abbr now
	CheckHr(m_qxvoTest->Sort(true));

	// Retreive the data
	// 2
	CheckHr(m_qxvoTest->GetDlgTagInfo(inumtags-1, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
		&sbstrAbbr3, &sbstrName3));
	// 1
	CheckHr(m_qxvoTest->GetDlgTagInfo(inumtags-2, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
		&sbstrAbbr2, &sbstrName2));
	// 0
	CheckHr(m_qxvoTest->GetDlgTagInfo(inumtags-3, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
		&sbstrAbbr1, &sbstrName1));

	// Validate sort
	if (wcscmp(sbstrAbbr1.Chars(), L"AAAAA") || wcscmp(sbstrAbbr2.Chars(), L"BBBBB") ||
		wcscmp(sbstrAbbr3.Chars(), L"CCCCC"))
		Failure("Sort(true, ...) by Abbr did not work");
}

void ViewTest1::TestMerge()
{
	HRESULT hr;
	IVwOverlayPtr m_qxvoTest1;
	OLECHAR rgchGuid[9] = L"F121F260";
	OLECHAR rgchGuid2[9] = L"75F23760";
	int inumtags, inumtags2;

	// Send NULL pointer
	hr = m_qxvoTest->Merge(NULL, NULL);
	if (!FAILED(hr))
		Failure("Call to function Merge(NULL...) should have returned an error");

	hr = m_qxvoTest->Merge(m_qxvoTest, NULL);
	if (!FAILED(hr))
		Failure("Call to function Merge(m_qxvoTest, NULL) should have returned an error");

	m_qxvoTest1.CreateInstance(CLSID_VwOverlay);
	CheckHr(m_qxvoTest1->SetTagInfo(rgchGuid, 5, kosmAll, SmartBstr(L"DDDDD"), SmartBstr(L"WWWWW"),
		0x33003300, 0x00000000, 0x00330033, 4, true));

	//	Valid test
	hr = m_qxvoTest->Merge(m_qxvoTest, &m_qxvoTest1);
	TestHrFail(hr, "Merge function failed");

	// Check to see that merge actually occurred
	// Merged overlay should have one more tag than previous overlay
	hr = m_qxvoTest->get_CTags(&inumtags);
	WarnHr(hr);

	hr = m_qxvoTest1->get_CTags(&inumtags2);
	WarnHr(hr);
	if (inumtags != inumtags2)
		Failure("Merge failed");

	// Now attempt merge with similar GUID's yet different abbreviations
	CheckHr(m_qxvoTest1->SetTagInfo(rgchGuid2, 4, kosmAll, SmartBstr(L"EEEEE"), SmartBstr(L"VVVVV"),
		0x00000000, 0x00000000, 0x00000000, 4, true));

	hr = m_qxvoTest->Merge(m_qxvoTest, &m_qxvoTest1);
	if (FAILED(hr))
		FailureFormat("Merge with different abbreviations failed. hr = %x", hr);
}

/*---------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------*/
ViewTest2::ViewTest2()
:TestBase(L"VwGraphics Test", 2)
{
//	m_stuBsl = L"VwGraphics";
}

ViewTest2::~ViewTest2()
{
}

HRESULT ViewTest2::Run()
{
	// Make a special VwGraphics (#define BASELINE in compiling it turns on special features).
	// This VwGraphics will not draw (2nd arg false) but will record attempts at drawing into
	// the baseline.
	m_psts = NewObj SilTestSite();
	m_qst.Attach(m_psts);
	m_qvg.Attach(NewObj VwGraphics(m_psts, false, true));
	m_qst->SetBaselineFile(SmartBstr(L"c:\\fw\\testlog\\log\\VwGraphics").Bstr());

	// Todo LukeU(JohnT): make an off-screen bitmap HDC and initialize the VwGraphics to use it.
	// Here is your off-screen bitmap and memory surface
	HDC hdcScr, hdcMem;
	hdcScr = GetDC(NULL);
	hdcMem = CreateCompatibleDC(hdcScr);
	HBITMAP hBitmap = CreateCompatibleBitmap(hdcMem, 400, 400);
	SelectObject(hdcMem, hBitmap);
	ReleaseDC(NULL, hdcScr);

	m_qvg->Initialize(hdcMem);

	// Make a dummy root site for the Root box to talk back to
	m_qtrs.Attach(NewObj VwTestRootSite);
	m_qtrs->SetVgObject(m_qvg);
	Rect rcSrc(0, 0, 96, 96);
	Rect rcDst(0, 0, 96, 96);
	m_qtrs->SetSrcRoot(rcSrc);
	m_qtrs->SetDstRoot(rcDst);

	// Make our view constructor.
	m_qtvc.Attach(NewObj TestStVc);

	// Put some dummy data into a cache. HVO 1 identifies the text as a whole.
	// Arbitrarily objects 2, 3, and 4 are the three paragraphs of our test data.
	m_qda.Attach(NewObj VwCacheDa);

	HVO rghvoPara[3] = {2, 3, 4};
	m_qda->CacheVecProp(1, kflidStText_Paragraphs, rghvoPara, 3);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	ITsStringPtr qtss;
	int enc = 100;

	StrUni stuPara0 = L"This is the first paragraph";
	StrUni stuPara1 = L"Here is another paragraph, quite silly and trivial but it should "
						L"help test things";
	StrUni stuPara2 = L"I try to keep the text in these quite different so they can't be "
						L"confused";

	CheckHr(qtsf->MakeStringRgch(stuPara0.Chars(), stuPara0.Length(), enc, &qtss));
	m_qda->CacheStringProp(rghvoPara[0], kflidStTxtPara_Contents, qtss);

	CheckHr(qtsf->MakeStringRgch(stuPara1.Chars(), stuPara1.Length(), enc, &qtss));
	m_qda->CacheStringProp(rghvoPara[1], kflidStTxtPara_Contents, qtss);

	CheckHr(qtsf->MakeStringRgch(stuPara2.Chars(), stuPara2.Length(), enc, &qtss));
	m_qda->CacheStringProp(rghvoPara[2], kflidStTxtPara_Contents, qtss);

	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuNormal = L"Normal";
	CheckHr(qtpb->SetStrPropValue(kspNamedStyle, stuNormal.Bstr()));

	// Make the root box and initialize it.
	m_qrootb.CreateInstance(CLSID_VwRootBox);

	// OK, we have a root box set up. Now we can try some tests on it!
	TestInit();
	TestDataAccess();
	TestLayout(350);

	TestDrawRoot();
	TestOverlay();

	TestMakeSimpleSel();
	TestMakeTextSelection();

	TestKeys();
	TestMouse();

	Testget_Site();
	TestLoseFocus();
	TestListener();

	m_qrootb->Close();
	m_qrootb.Clear();
	m_qda.Clear();

	DeleteDC(hdcMem);
	DeleteObject(hBitmap);
	return S_OK;
}

void ViewTest2::TestInit()
{
	// SetSite takes an IVwRootSite.
	CheckHr(m_qrootb->SetSite(m_qtrs));
	// The root StText is always ID 1
	HVO  hvoDoc = 1;
	int frag = kfrText;
	// We need a pointer to the pointer, and we can't use &m_qvc because that clears the
	// pointer!!
	IVwViewConstructor * pvvc = m_qtvc;
	CheckHr(m_qrootb->putref_DataAccess(m_qda));
	CheckHr(m_qrootb->SetRootObjects(&hvoDoc, &pvvc, &frag, NULL, 1));
}

void ViewTest2::TestLayout(int width)
{
	m_psts->OutputFormat("  FUNCTION: Layout(m_qvg, %d) \n", width);
	CheckHr(m_qrootb->Layout(m_qvg, width));
}

void ViewTest2::TestDataAccess()
{
	VwCacheDaPtr qda, qda2;
	qda.Attach(NewObj VwCacheDa);
	qda2.Attach(NewObj VwCacheDa);

	HVO rghvoPara[1] = {2};
	qda->CacheVecProp(1, kflidStText_Paragraphs, rghvoPara, 1);

	ITsStrFactoryPtr qtsf2;
	qtsf2.CreateInstance(CLSID_TsStrFactory);

	ITsStringPtr qtss2;
	int enc = 100;

	StrUni stuPara0 = L"Testing DataAccess action";
	CheckHr(qtsf2->MakeStringRgch(stuPara0.Chars(), stuPara0.Length(), enc, &qtss2));
	qda->CacheStringProp(rghvoPara[0], kflidStTxtPara_Contents, qtss2);

	CheckHr(m_qrootb->putref_DataAccess(qda));
	CheckHr(m_qrootb->get_DataAccess((ISilDataAccess **)(&qda2)));

	if (qda != qda2)
		Failure("DataAccess returned wrong result");

	qda.Clear();
	qda2.Clear();

	CheckHr(m_qrootb->putref_DataAccess(m_qda));
}

void ViewTest2::TestOverlay()
{
	OLECHAR rgchGuid[] = L"F121F260";
	IVwOverlayPtr qxvoTest1, qxvoTest2;
	qxvoTest1.CreateInstance(CLSID_VwOverlay);
	qxvoTest2.CreateInstance(CLSID_VwOverlay);

	CheckHr(qxvoTest1->SetTagInfo(rgchGuid, 5, kosmAll, SmartBstr(L"DDDDD"), SmartBstr(L"WWWWW"),
		0x00003300, 0x00000000, 0x00330033, 4, true));

	m_psts->Output("  FUNCTION: put_Overlay(qxvoTest1)\n");
	CheckHr(m_qrootb->put_Overlay(qxvoTest1));

	m_psts->Output("  FUNCTION: get_Overlay(&qxvoTest2)\n");
	CheckHr(m_qrootb->get_Overlay(&qxvoTest2));
	if (qxvoTest1 != qxvoTest2)
		Failure("Overlay returned wrong result");
}

void ViewTest2::TestDrawRoot()
{
	// Attempt valid call. This is after Layout has been called
	m_psts->Output("  FUNCTION: DrawRoot(m_qvg, {0,0,96,96}, {0,0,192,192}) \n");
	Rect rcSrc(0,0,96,96);
	Rect rcDst(0,0,182,182);
	CheckHr(m_qrootb->DrawRoot(m_qvg, rcSrc, rcDst));
	TestHeight();
	TestWidth();
	TestLayout(200);

	m_psts->Output("\n  FUNCTION: DrawRoot(m_qvg, {0,0,96,96}, {0,0,96,96}) \n");
	CheckHr(m_qrootb->DrawRoot(m_qvg, rcSrc, rcSrc));
	TestHeight();
	TestWidth();
}

void ViewTest2::TestHeight()
{
	int ivar;
	CheckHr(m_qrootb->get_Height(&ivar));

	m_psts->OutputFormat("get_Height returned %d\n", ivar);
}

void ViewTest2::TestWidth()
{
	int ivar;
	CheckHr(m_qrootb->get_Width(&ivar));

	m_psts->OutputFormat("get_Width returned %d\n", ivar);
}


void ViewTest2::TestMakeSimpleSel()
{
	m_psts->Output("  FUNCTION: MakeSimpleSel(true, false, false) \n");
	CheckHr(m_qrootb->MakeSimpleSel(true, false, false));

	m_psts->Output("  FUNCTION: MakeSimpleSel(false, false, false) \n");
	CheckHr(m_qrootb->MakeSimpleSel(false, false, false));

	m_psts->Output("  FUNCTION: MakeSimpleSel(false, true, false) \n");
	CheckHr(m_qrootb->MakeSimpleSel(false, true, false));

	m_psts->Output("\n	Now Testing DestroySelection()\n");
	m_psts->Output("  FUNCTION: DestroySelection() \n");
	CheckHr(m_qrootb->DestroySelection());

	m_psts->Output("  FUNCTION: MakeSimpleSel(true, true, false) \n");
	CheckHr(m_qrootb->MakeSimpleSel(true, true, false));
}

void ViewTest2::TestMakeTextSelection()
{
	VwSelLevInfo vsli;
	vsli.ihvo=-1;
	vsli.cpropPrevious = 0;
	vsli.tag = kflidStText_Paragraphs;

	m_psts->Output("  FUNCTION: MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 5, "
					"25, 0, false, 2))\n");
	CheckHr(m_qrootb->MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 5, 25, 0,
					false, 2));

	m_psts->Output("\n	Now Testing DestroySelection()\n");
	CheckHr(m_qrootb->DestroySelection());

	m_psts->Output("  FUNCTION: MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 5, "
					"25, 0, false, 0))\n");
	CheckHr(m_qrootb->MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 5, 25, 0,
			false, 0));
	m_psts->Output("  FUNCTION: MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 5, "
					"25, 0, false, 0))\n");
	CheckHr(m_qrootb->MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 5, 25, 0,
			false, 0));

	m_psts->Output("MULTIPLE ENTRY");
	m_psts->Output("  FUNCTION: MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 25, "
					"75, 0, false, 1))\n");
	CheckHr(m_qrootb->MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 25, 75, 0,
			false, 1));
	m_psts->Output("  FUNCTION: MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 5, "
					"25, 0, false, -1))\n");
	CheckHr(m_qrootb->MakeTextSelection(0, 1, &vsli, kflidStTxtPara_Contents, 0, 5, 25, 0,
			false, -1));
}

void ViewTest2::TestKeys()
{
	// First off take away any selection that exists
//	CheckHr(m_qrootb->DestroySelection());
	m_qtrs->SetAvailWidth(m_qrootb, 350);

	// Send some strings
	m_psts->Output("\n	Now Testing Key Functions\n");
	RECT rcSrc = {0, 0, 96, 96};
	WrapOnTyping("Peace!", 0, 0, "P", rcSrc, rcSrc);

	// Move to a different location and type
	WrapMouseDown(20, 32, rcSrc, rcSrc);
	WrapOnTyping("Joy!", 0, 0, "J", rcSrc, rcSrc);

	// Try the backspace and delete thingy
	WrapOnTyping("Death!", 4, 0, "D", rcSrc, rcSrc);
	WrapOnTyping("Anguish!", 0, 6, "A", rcSrc, rcSrc);

	// Now try OnChar
	WrapOnChar(74);		// ?74 == J?

	WrapOnSysChar(VK_DELETE);
	WrapOnSysChar(VK_BACK);

	// Now try the extended key action
	WrapOnExtendedKey(VK_DELETE, kfssControl);
	WrapOnExtendedKey(VK_TAB, kfssShift);
}

void ViewTest2::WrapOnTyping(char *pchinput, int _cchBackspace, int _cchDelForward,
							 char *_chFirst, RECT _rcSrc, RECT _rcDst)
{
	OLECHAR oleChar = _chFirst[0];
	SmartBstr _bstrInput = StrUni(pchinput).Bstr();

	m_psts->OutputFormat("  FUNCTION: OnTyping(m_qvg, \"%s\", %d, %d, '%s', {%d,%d,%d,%d}, "
		"{%d,%d,%d,%d})\n", pchinput, _cchBackspace, _cchDelForward, _chFirst, _rcSrc.left,
		_rcSrc.top, _rcSrc.right, _rcSrc.bottom, _rcDst.left, _rcDst.top, _rcDst.right,
		_rcDst.bottom);
	CheckHr(m_qrootb->OnTyping(m_qvg, _bstrInput, _cchBackspace, _cchDelForward, oleChar,
		_rcSrc, _rcDst));
}

void ViewTest2::WrapOnChar(int _chw)
{
	m_psts->OutputFormat("  FUNCTION: OnChar(%d)\n", _chw);
	CheckHr(m_qrootb->OnChar(_chw));
}

void ViewTest2::WrapOnSysChar(int _chw)
{
	m_psts->OutputFormat("  FUNCTION: OnSysChar(%d)\n", _chw);
	CheckHr(m_qrootb->OnSysChar(_chw));
}

void ViewTest2::WrapOnExtendedKey(int _chw, VwShiftStatus _ss)
{
	StrAnsi staExt;
	switch (_ss)
	{
		case kfssNone:
			staExt.Append("None");
			break;
		case kgrfssShiftControl:
			staExt.Append("Shift + Control");
			break;
		case kfssShift:
			staExt.Append("Shift ");
			break;
		case kfssControl:
			staExt.Append("Control ");
			break;
		default:
			staExt.Append("Undefined state");
	}
	m_psts->OutputFormat("  FUNCTION: OnExtendedKey(%d, %s)\n", _chw, staExt.Chars());
	CheckHr(m_qrootb->OnExtendedKey(_chw, _ss));
}

void ViewTest2::TestSelection()
{
	IVwSelectionPtr qvsl;
	CheckHr(m_qrootb->get_Selection(&qvsl));
}

void ViewTest2::TestOnChar()
{
	m_psts->Output("  FUNCTION: OnChar(L\"D\") \n");
	CheckHr(m_qrootb->OnChar(69));
}

void ViewTest2::TestFlash()
{
	m_psts->Output("  FUNCTION: FlashInsertionPoint()\n");
	CheckHr(m_qrootb->FlashInsertionPoint());
}

void ViewTest2::TestMouse()
{
	RECT rcSrc = {0,0,96,96};
	WrapMouseDown(0, 0, rcSrc, rcSrc);
	WrapMouseDown(40, 40, rcSrc, rcSrc);
	WrapMouseDown(0, 40, rcSrc, rcSrc);
	WrapMouseDown(40, 0, rcSrc, rcSrc);

	WrapMouseMoveDrag(40, 40, rcSrc, rcSrc);
	WrapMouseUp(40, 40, rcSrc, rcSrc);

	WrapMouseDown(0, 0, rcSrc, rcSrc);
	WrapMouseDownExtended(40, 40, rcSrc, rcSrc);
	WrapMouseUp(40, 40, rcSrc, rcSrc);

	// Be weird for a little bit.
	for (int i=0; i < 5; i++)
		TestFlash();
}

void ViewTest2::WrapMouseDown(int _xd, int _yd, RECT _rcSrc, RECT _rcDst)
{
	m_psts->OutputFormat("  FUNCTION: MouseDown(%d, %d, {%d,%d,%d,%d}, "
		"{%d,%d,%d,%d})\n", _xd, _yd, _rcSrc.left, _rcSrc.top, _rcSrc.right, _rcSrc.bottom,
		_rcDst.left, _rcDst.top, _rcDst.right, _rcDst.bottom);
	CheckHr(m_qrootb->MouseDown(_xd, _yd, _rcSrc, _rcDst));
}

void ViewTest2::WrapMouseMoveDrag(int _xd, int _yd, RECT _rcSrc, RECT _rcDst)
{
	m_psts->OutputFormat("  FUNCTION: MouseMoveDrag(%d, %d, {%d,%d,%d,%d}, "
		"{%d,%d,%d,%d})\n", _xd, _yd, _rcSrc.left, _rcSrc.top, _rcSrc.right,
		_rcSrc.bottom, _rcDst.left, _rcDst.top, _rcDst.right, _rcDst.bottom);
	CheckHr(m_qrootb->MouseMoveDrag(_xd, _yd, _rcSrc, _rcDst));
}

void ViewTest2::WrapMouseDownExtended(int _xd, int _yd, RECT _rcSrc, RECT _rcDst)
{
	m_psts->OutputFormat("  FUNCTION: MouseDownExtended(%d, %d, {%d,%d,%d,%d}, "
		"{%d,%d,%d,%d})\n", _xd, _yd, _rcSrc.left, _rcSrc.top, _rcSrc.right,
		_rcSrc.bottom, _rcDst.left, _rcDst.top, _rcDst.right, _rcDst.bottom);
	CheckHr(m_qrootb->MouseDownExtended(_xd, _yd, _rcSrc, _rcDst));
}

void ViewTest2::WrapMouseUp(int _xd, int _yd, RECT _rcSrc, RECT _rcDst)
{
	m_psts->OutputFormat("  FUNCTION: MouseUp(%d, %d, {%d,%d,%d,%d}, {%d,%d,%d,%d})\n", _xd,
		_yd, _rcSrc.left, _rcSrc.top, _rcSrc.right, _rcSrc.bottom, _rcDst.left, _rcDst.top,
		_rcDst.right, _rcDst.bottom);
	CheckHr(m_qrootb->MouseUp(_xd, _yd, _rcSrc, _rcDst));
}

void ViewTest2::Testget_Site()
{
	IVwRootSitePtr qvrs;
	HRESULT hr = m_qrootb->get_Site(&qvrs);
	WarnHr(hr);

	if (qvrs != m_qtrs)
		Failure("get_Site returned wrong result");
}

void ViewTest2::TestLoseFocus()
{
	ComBool ffoc;
	m_psts->Output("  FUNCTION: LoseFocus(&ffoc)\n");
	CheckHr(m_qrootb->LoseFocus(&ffoc));
}

void ViewTest2::TestListener()
{
	IEventListenerPtr qel;
	m_psts->Output("  FUNCTION: AddSelChngListener(qel)\n");
	CheckHr(m_qrootb->AddSelChngListener(qel));

	m_psts->Output("\n	Now Testing DelSelChngListener\n");
	m_psts->Output("  FUNCTION: DelSelChngListener(qel)\n");
	CheckHr(m_qrootb->AddSelChngListener(qel));
}

/*---------------------------------------------------------------------------------------------
		ViewTest3 class implementation
---------------------------------------------------------------------------------------------*/
ViewTest3::ViewTest3() : TestBase(L"Interactive Test 1", 3)
{
}

ViewTest3::~ViewTest3()
{
}

HRESULT ViewTest3::Run()
{
	// Call the constructor specifying whether to baseline and/or draw
	m_tvr = NewObj TestVwRoot(true, true);
	// The first param to runmacro is the test file to execute, the second specifies the
	// baseline file to baseline to if desired. Leaving it blank chooses a default name
	// of: THInteractive (Test Harness Interactive)
	m_tvr->RunMacro("monpm.tst", "");

	// The class obj is not a smart pointer and therefore must delete
	delete m_tvr;
	return S_OK;
}
