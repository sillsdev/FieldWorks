/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: TstLanguage.cpp
Responsibility: Lars Huttar
Last reviewed: Not yet.
Description:
	This provides a test class/object that tests the Language model classes.
	It is bundled into a DLL for use by the test harness program.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE
/***********************************************************************************************
	Local classes
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	This class provides tests for the class LgWritingSystem.
	Hungarian: tws
----------------------------------------------------------------------------------------------*/
class TestWritingSystem : public TestBase
{
public:
	// constructor
	TestWritingSystem() : TestBase(L"WritingSystem tests", 1) { }

	// public methods
	HRESULT Run();

protected:
	// Other protected methods
	void TestSomeWritingSystems();

};

// This static instance is linked into the chain of DLL tests by the base class constructor
static TestWritingSystem g_twsInstance;

const int knDataBufSize = 256;
// Structure containing data for a Unicode character, as read from data file.
typedef struct CodePtData_st
{
	int chCodeValue;
	OLECHAR szwName[knDataBufSize];
	char szCategory[3];
	int nCombiningClass;
	OLECHAR szwDecomposition[64];
	int nNumericValue;
	int chUpper, chLower, chTitle;
} CodePtData;

/*----------------------------------------------------------------------------------------------
	This class provides tests for the class LgCollatingEngine
	Hungarian: tcoleng
----------------------------------------------------------------------------------------------*/
class TestCollatingEngine : public TestBase
{
public:
	// constructor
	TestCollatingEngine() : TestBase(L"CollatingEngine tests", 2) { }

	// public methods
	HRESULT Run();

	// Is this implemented elsewhere?
	int sgn(int a) { return ((a) ? ((a) < 0 ? -1 : 1) : (a)); }

protected:
	// Member variables
	ILgCollatingEnginePtr m_qcoleng;
	// Protected methods
	void TestCompare(StrUni stuVal1, StrUni stuVal2, int nExpected);

};

// This static instance is linked into the chain of DLL tests by the base class constructor
static TestCollatingEngine g_tcolengInstance;

/*----------------------------------------------------------------------------------------------
	This class provides tests for the class LgCharacterPropertyEngine.
	Hungarian: tpropeng
----------------------------------------------------------------------------------------------*/
class TestCharacterPropertyEngine : public TestBase
{
public:
	// constructor
	TestCharacterPropertyEngine() : TestBase(L"CharacterPropertyEngine tests", 3) { }

	// public methods
	HRESULT Run();
	void SpotCheckProperties();
	void TestCaseConversion();

protected:
	ILgCharacterPropertyEnginePtr m_qpropeng;

	void CheckCat(int ch, LgGeneralCharCategory ccExpected);
	void CheckHrBool(int ch, HRESULT hr, bool fExpected, bool fActual, char * szFname);
};

// This static instance is linked into the chain of DLL tests by the base class constructor
static TestCharacterPropertyEngine g_tpropengInstance;

/*----------------------------------------------------------------------------------------------
	This class provides an exhaustive test for the class LgCharacterPropertyEngine.
	Hungarian: tepropeng
----------------------------------------------------------------------------------------------*/
class ExhTestCPEngine : public TestBase
{
public:
	// constructor
	ExhTestCPEngine() : TestBase(L"CharacterPropertyEngine exhaustive test", 4) { }

	// public methods
	HRESULT Run();

protected:
	ILgCharacterPropertyEnginePtr m_qpropeng;

	void CheckCat(int ch, LgGeneralCharCategory ccExpected);
	void ExhTestProperties();
	void ParseRecord(FILE * pFileIn, CodePtData * pcpd);
	enum LgGeneralCharCategory ParseCC(char * szCC);
	void HangulName(OLECHAR * szwDecompIn, OLECHAR * szwNameOut);
	void HangulDecomposition(int ch, OLECHAR * szwDecompOut);
};

// This static instance is linked into the chain of DLL tests by the base class constructor
static ExhTestCPEngine g_tepropengInstance;

// Convenience macro: Make sure a failed call zeroes out any return ptrs.
#define TestFailZero(ptr, methname) \
	{ \
		if (FAILED(hr) && ptr) \
			FailureFormat("%s() failed but did not zero out return pointer.", methname); \
	}


/***********************************************************************************************
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Perform some tests on the LgWritingSystem class.
	Return S_OK if all tests succeed, or an appropriate COM error code.
----------------------------------------------------------------------------------------------*/
HRESULT TestWritingSystem::Run()
{
#ifdef OLD_SOURCE_TREE
	HRESULT hr;
	// Make a writing system. Initialize it the standard way with a moniker.
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);
#endif

#if 0
	// REVIEW LarsH  ###
	// LgWritingSystem is not fully implemented at this time, so
	// some of the following tests fail.  When implementation is finished,
	// these tests can be enabled, and more tests will need to be added.

	TestSomeWritingSystems();
#endif

	return S_OK;
}

void TestWritingSystem::TestSomeWritingSystems()
{
	HRESULT hr;
	ILgWritingSystemPtr qwse1;
	qwse1.CreateInstance(CLSID_LgWritingSystem);

	// Test properties of WritingSystem by getting them.
	// Is there a way to set them (indirectly) so we can test that
	//  the same value is returned by get?
	ComBool fRtL;

	{
		IgnoreWarnings iw;	 // These things should fail but not crash.

#       define TryWithNULL(mthd) \
		{ \
			hr = qwse1->mthd(NULL); \
			if (SUCCEEDED(hr)) \
				Failure(#mthd " should have returned failure given NULL ptr"); \
		}

		TryWithNULL(get_RenderEngine);
#ifdef MaybeGone
		TryWithNULL(get_Direction);
		TryWithNULL(get_LineBreakEngine);
		TryWithNULL(get_HyphenateEngine);
#endif
		TryWithNULL(get_CollatingEngine);
		TryWithNULL(get_CharPropEngine);
		TryWithNULL(get_Encoding);
#		undef TryWithNULL
	}

#ifdef MaybeGone
	hr = qwse1->get_Direction(&fRtL);
	WarnHr(hr);
#endif

	IRenderEnginePtr qreneng; // = new IRenderEngine()
	hr = qwse1->get_RenderEngine(&qreneng);
	TestFailZero(qreneng, "get_RenderEngine()");
	WarnHr(hr);
	WarnUnless(qreneng);

#ifdef MaybeGone
	ILgLineBreakEnginePtr qlbe;
	hr = qwse1->get_LineBreakEngine(&qlbe);
	TestFailZero(qlbe, "get_LineBreakEngine()");
	WarnHr(hr);
	WarnUnless(qlbe);

	ILgLineBreakEnginePtr qlbe1;
	hr = qwse1->get_HyphenateEngine(&qlbe1);
	TestFailZero(qlbe1, "get_HyphenateEngine()");
	WarnHr(hr);
	WarnUnless(qlbe1);
#endif // MaybeGone

	ILgCollatingEnginePtr qcoleng;
	hr = qwse1->get_CollatingEngine(&qcoleng);
	TestFailZero(qcoleng, "get_CollatingEngine()");
	WarnHr(hr);
	WarnUnless(qcoleng);

	ILgCharacterPropertyEnginePtr qpropeng;
	hr = qwse1->get_CharPropEngine(&qpropeng);
	TestFailZero(qpropeng, "get_CharPropEngine()");
	WarnHr(hr);
	WarnUnless(qpropeng);

	int enc;
	hr = qwse1->get_Encoding(&enc);
	WarnHr(hr);
	WarnUnless(enc); // REVIEW LarsH: can we test if enc is valid?

	return;
}


/*----------------------------------------------------------------------------------------------
	Perform some tests on the LgCollatingEngine class.
	Return S_OK if all tests succeed, or an appropriate COM error code.
----------------------------------------------------------------------------------------------*/
HRESULT TestCollatingEngine::Run()
{
	HRESULT hr;
	// WarnHr(E_FAIL);

#ifdef OLD_SOURCE_TREE

	// Make a collater for English. Initialize it the standard way with a moniker.
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);
	int lid = MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US);
	hr = qcim->InitNew(CLSID_LgSystemCollater, (const BYTE *)&lid, isizeof(lid));
	WarnHr(hr);

	IBindCtxPtr qbc;
	hr = ::CreateBindCtx(NULL, &qbc);
	WarnHr(hr);
	hr = qcim->BindToObject(qbc, NULL, IID_ILgCollatingEngine, (void **) &m_qcoleng);
	WarnHr(hr);
#endif // def OLD_SOURCE_TREE

	// ## ILgFontManagerPtr qfm;
	// ## qfm.CreateInstance(CLSID_LgFontManager);
	try
	{
		m_qcoleng.CreateInstance(CLSID_LgSystemCollater);
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}


	// Create sort keys for some strings and use them.
	StrUni stuValBlank = L"";
	StrUni stuValSpace = L" a";
	StrUni stuValA = L"a";
	StrUni stuValB = L"b";
	StrUni stuValAUpper = L"A";
	StrUni stuValAnd = L"and";
	StrUni stuValAny = L"any";
	StrUni stuValAndUpper = L"And";
	StrUni stuValPunct = L";(* #([/";

	// REVIEW LarsH
#define FIXED_BUG_173 1
#ifdef FIXED_BUG_173
	{
		IgnoreWarnings iw;

		hr = m_qcoleng->Compare(stuValB.Bstr(), stuValA.Bstr(), fcoDefault, NULL);
		if (SUCCEEDED(hr))
			Failure("Compare() with NULL return ptr should have failed");
	}
#endif

	int nRes = -1;

	// NULL sbstrs are valid, but the CollatingOption isn't.
	// REVIEW LarsH:
	// Are invalid CollatingOptions are supposed to be ignored, or answered with an error?
	{
		IgnoreWarnings iw;
		hr = m_qcoleng->Compare(NULL, stuValA.Bstr(), (enum LgCollatingOptions)37, &nRes);
		if (!FAILED(hr))
			Failure("get_SortKey with invalid option should have failed.");
		//		WarnHr(hr);
		//		if (nRes != -1)
		//			Failure("Compare() with invalid CollatingOption gave wrong answer");
	}

	// Try a sequence of test comparisions.
	TestCompare(stuValBlank, stuValBlank, 0);
	TestCompare(stuValBlank, stuValSpace, -1);
	TestCompare(stuValBlank, stuValA, -1);
	TestCompare(stuValBlank, stuValAUpper, -1);
	TestCompare(stuValSpace, stuValA, -1);
	TestCompare(stuValA, stuValBlank, 1);
	TestCompare(stuValA, stuValB, -1);
	TestCompare(stuValB, stuValA, 1);
	TestCompare(stuValA, stuValAUpper, -1);
	TestCompare(stuValB, stuValAUpper, 1);
	TestCompare(stuValA, stuValAnd, -1);
	TestCompare(stuValAny, stuValAnd, 1);
	TestCompare(stuValAny, stuValAndUpper, 1);
	TestCompare(stuValAnd, stuValAnd, 0);
	TestCompare(stuValBlank, stuValPunct, -1);
	TestCompare(stuValPunct, stuValA, -1);
	TestCompare(stuValBlank, (wchar *)NULL, 0);
	TestCompare((wchar *)NULL, stuValA, -1);
	TestCompare(stuValA, (wchar *)NULL, 1);

	// REVIEW JohnT(LarsH): interface needs to define what happens to characters that
	//  are not part of the character set for that language.  E.g. behavior is defined
	//  by the such-and-such procedure for converting string to 8-bit etc.

	// Invalid surrogate sequences.
	StrUni stuValInvalid1 = L"\xdc00";
	StrUni stuValInvalid2 = L"\xdb80";
	StrUni stuValInvalid3 = L"\xdc00\xdc00";
	StrUni stuValSurr = L"\xdb80\xdc00"; // valid surrogate pair, but invalid code point

	// At this point I don't know what the result of the
	// comparison should be, or whether it should return a failure code, but at least
	// we can make sure it doesn't crash.
	int nResult;
	hr = m_qcoleng->Compare(stuValInvalid1.Bstr(), stuValInvalid2.Bstr(), fcoDefault, &nResult);
	hr = m_qcoleng->Compare(stuValInvalid3.Bstr(), stuValSurr.Bstr(), fcoDefault, &nResult);
	hr = m_qcoleng->Compare(stuValSurr.Bstr(), stuValAnd.Bstr(), fcoDefault, &nResult);

	// test case-insensitive comparison
	// TODO LarsH: ought to test both locale-dependent and locale-independent
	// case folding.  See UTR#21 and CaseFolding.txt.  But to do this we need
	// to be able to set the locale.  So far we have no such concept IMO.

	SmartBstr sbstrKeyAnd = L"foo";
	SmartBstr sbstrKeyAndUpper = L"bar";
	SmartBstr sbstrKeyAny = L"baz";

	hr = m_qcoleng->get_SortKey(stuValAnd.Bstr(), fcoIgnoreCase, &sbstrKeyAnd);
	TestFailZero(sbstrKeyAnd, "get_SortKey");
	TestHrFail(hr, "getting SortKey on string");
	hr = m_qcoleng->get_SortKey(stuValAndUpper.Bstr(), fcoIgnoreCase, &sbstrKeyAndUpper);
	TestFailZero(sbstrKeyAndUpper, "get_SortKey");
	TestHrFail(hr, "getting SortKey on string");
	hr = m_qcoleng->get_SortKey(stuValAny.Bstr(), fcoIgnoreCase, &sbstrKeyAny);
	TestFailZero(sbstrKeyAny, "get_SortKey");
	TestHrFail(hr, "getting SortKey on string");

	if(wcscmp(sbstrKeyAnd, sbstrKeyAndUpper))
		Failure("String compare failed");
	if (wcscmp(sbstrKeyAnd, sbstrKeyAny) >= 0)
		Failure("String compare failed");

	// REVIEW LarsH: we need a test specific to DontIgnoreVariant, whatever that means
	hr = m_qcoleng->get_SortKey(stuValAnd.Bstr(), fcoDontIgnoreVariant, &sbstrKeyAnd);
	TestFailZero(sbstrKeyAnd, "get_SortKey");
	TestHrFail(hr, "getting SortKey on string");
	hr = m_qcoleng->get_SortKey(stuValAndUpper.Bstr(), fcoDontIgnoreVariant, &sbstrKeyAndUpper);
	TestFailZero(sbstrKeyAndUpper, "get_SortKey");
	TestHrFail(hr, "getting SortKey on string");
	hr = m_qcoleng->get_SortKey(stuValAny.Bstr(), fcoDontIgnoreVariant, &sbstrKeyAny);
	TestFailZero(sbstrKeyAny, "get_SortKey");
	TestHrFail(hr, "getting SortKey on string");

	if(wcscmp(sbstrKeyAnd, sbstrKeyAndUpper) >= 0)
		Failure("String compare failed");
	if (wcscmp(sbstrKeyAnd, sbstrKeyAny) >= 0)
		Failure("String compare failed");

	// Try to break get_SortKey()
#define FIXED_BUG_431  1
#ifdef FIXED_BUG_431
	{
		IgnoreWarnings iw;
		hr = m_qcoleng->get_SortKey(stuValAny.Bstr(), (enum LgCollatingOptions)(-2),
			&sbstrKeyAny);
		if (!FAILED(hr) || sbstrKeyAny)
			Failure("get_SortKey with invalid option should have failed and returned NULL.");

		hr = m_qcoleng->get_SortKey(stuValAny.Bstr(), fcoDefault, NULL);
		if (!FAILED(hr))
			Failure("get_SortKey with NULL return ptr should have failed.");
	}
#endif


	m_qcoleng.Clear(); // really? what for? do I need this elsewhere?
	// Why . instead of -> ?

	return S_OK;
}

void TestCollatingEngine::TestCompare(StrUni stuVal1, StrUni stuVal2, int nExpected)
{
	HRESULT hr;
	int nActual1, nActual2;
	SmartBstr sbstrKey1, sbstrKey2;
	SmartBstr sbstrVal1 = (stuVal1 ? stuVal1.Bstr() : NULL);
	SmartBstr sbstrVal2 = (stuVal2 ? stuVal2.Bstr() : NULL);

	// First comparison method
	hr = m_qcoleng->Compare(sbstrVal1, sbstrVal2, fcoDefault, &nActual1);
	TestHrFail(hr, "String compare");

	// Make sure we get the same result with the other method:
	// by getting the keys themselves and comparing them.
	hr = m_qcoleng->get_SortKey(sbstrVal1, fcoDefault, &sbstrKey1);
	TestFailZero(sbstrKey1, "get_SortKey");
	TestHrFail(hr, "getting SortKey on string 1");
	hr = m_qcoleng->get_SortKey(sbstrVal2, fcoDefault, &sbstrKey2);
	TestFailZero(sbstrKey2, "get_SortKey");
	TestHrFail(hr, "getting SortKey on string 2");

	nActual2 = wcscmp(sbstrKey1, sbstrKey2);

	if (sgn(nActual1) != sgn(nActual2))
		Failure("String comparison yielded inconsistent results");

	if (sgn(nActual1) != sgn(nExpected))
	{
		FailureFormat(L"Comparison of %s to %s gave %d instead of %d.",
			(sbstrVal1 ? sbstrVal1 : L"NULL"), (sbstrVal2 ? sbstrVal2 : L"NULL"),
			nActual1, nExpected);
	}

	return;
}

/*----------------------------------------------------------------------------------------------
	Perform some tests on the LgCharacterPropertyEngine class.
	Return S_OK if all tests succeed; do not return otherwise.
	REVIEW LarsH: will this really not return after *any* failure?
----------------------------------------------------------------------------------------------*/
HRESULT TestCharacterPropertyEngine::Run()
{
#ifdef OLD_SOURCE_TREE
	HRESULT hr;

	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);

	hr = qcim->InitNew(CLSID_LgCharacterPropertyEngine, NULL, 0);
	WarnHr(hr);
#endif // def OLD_SOURCE_TREE

	try
	{
		m_qpropeng.CreateInstance(CLSID_LgCharacterPropertyEngine);
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

#ifdef OLD_SOURCE_TREE
	IBindCtxPtr qbc;
	hr = ::CreateBindCtx(NULL, &qbc);
	WarnHr(hr);
	hr = qcim->BindToObject(qbc, NULL, IID_ILgCharacterPropertyEngine, (void **)&m_qpropeng);
	WarnHr(hr);
#endif // def OLD_SOURCE_TREE

	SpotCheckProperties();

	// REVIEW LarsH: set up a condition so that this test is only run when exhaustive,
	// time-consuming tests are called for.  Maybe make it a separate test from
	// the spot check.  Yes.  And its name should include a hint, like "brute force".
	// However, ExhTestProperties() will assume that SpotCheck()
	// is also being run, and will not cover all the same ground.


	// TestCaseConversion(); TODO LarsH

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Perform some tests on the LgCharacterPropertyEngine class.
	Return S_OK if all tests succeed; do not return otherwise.
	REVIEW LarsH: will this really not return after *any* failure?
----------------------------------------------------------------------------------------------*/
HRESULT ExhTestCPEngine::Run()
{
#ifdef OLD_SOURCE_TREE
	HRESULT hr;

	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);

	hr = qcim->InitNew(CLSID_LgCharacterPropertyEngine, NULL, 0);
	WarnHr(hr);

	IBindCtxPtr qbc;
	hr = ::CreateBindCtx(NULL, &qbc);
	WarnHr(hr);
	hr = qcim->BindToObject(qbc, NULL, IID_ILgCharacterPropertyEngine, (void **)&m_qpropeng);
	WarnHr(hr);
#endif // def OLD_SOURCE_TREE

	try
	{
		m_qpropeng.CreateInstance(CLSID_LgCharacterPropertyEngine);
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	ExhTestProperties();

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Perform some tests on the LgCharacterPropertyEngine class.
	Check error conditions and try a variety of scenarios for each property method.
	Return if all tests succeed; do not return otherwise.
	REVIEW LarsH: will this really not return after *any* failure?
----------------------------------------------------------------------------------------------*/

void TestCharacterPropertyEngine::SpotCheckProperties()
{
	HRESULT hr;

	{
		IgnoreWarnings iw;
		ComBool fRet;
		enum LgGeneralCharCategory ccRet;

		// TryToBreakGet(GeneralCategory); but different because return type is not bool
		hr = m_qpropeng->get_GeneralCategory('A', NULL);
		if (SUCCEEDED(hr))
			Failure("get_GeneralCategory with NULL return ptr should have failed");
		hr = m_qpropeng->get_GeneralCategory(0x111111, &ccRet);
		if (SUCCEEDED(hr))
			Failure("get_GeneralCategory with invalid char code should have failed");

#       define TryToBreakGet(prop) \
		{	\
			hr = m_qpropeng->get_##prop('A', NULL); \
			if (SUCCEEDED(hr)) \
				Failure("get_" #prop " with NULL return ptr should have failed"); \
			hr = m_qpropeng->get_##prop(0x111111, &fRet); \
			if (SUCCEEDED(hr)) \
				Failure("get_" #prop " with invalid char code should have failed"); \
		}

		TryToBreakGet(IsLetter);
		TryToBreakGet(IsPunctuation);
		TryToBreakGet(IsNumber);
		TryToBreakGet(IsSeparator);
		TryToBreakGet(IsSymbol);
		TryToBreakGet(IsMark);
		TryToBreakGet(IsOther);
		TryToBreakGet(IsUpper);
		TryToBreakGet(IsTitle);
		TryToBreakGet(IsLower);
		TryToBreakGet(IsModifier);
		TryToBreakGet(IsOtherLetter);
		TryToBreakGet(IsOpen);
		TryToBreakGet(IsClose);
		TryToBreakGet(IsWordMedial);
		TryToBreakGet(IsControl);

#		undef TryToBreakGet
	}

	// Check to make sure various characters return the right GeneralCategory.
	// See Language.idh or UnicodeData.html for definitions of categories.
	CheckCat('A', kccLu);
	CheckCat('z', kccLl);
	CheckCat(0x01c5, kccLt); // LATIN CAPITAL LETTER D WITH SMALL LETTER Z WITH CARON
	CheckCat(0x02b0, kccLm); // MODIFIER LETTER SMALL H
	CheckCat(0x01bb, kccLo); // LATIN LETTER TWO WITH STROKE

	CheckCat(0x0300, kccMn);
	CheckCat(0x0903, kccMc);
	CheckCat(0x06dd, kccMe);

	CheckCat('1', kccNd);
	CheckCat(0x2160, kccNl);
	CheckCat(0x00b2, kccNo);

	CheckCat(' ', kccZs);
	CheckCat(0x2028, kccZl);
	CheckCat(0x2029, kccZp);

	CheckCat(0x007f, kccCc);
	CheckCat(0x200c, kccCf);
	CheckCat(0xd800, kccCs);
	CheckCat(0xe000, kccCo);
	CheckCat(0xffc1, kccCn); // currently unassigned

	CheckCat('_', kccPc);
	CheckCat('-', kccPd);
	CheckCat('(', kccPs);
	CheckCat(')', kccPe);
	CheckCat(0xAB, kccPi); // '<<'
	CheckCat(0x2019, kccPf); // RIGHT SINGLE QUOTATION MARK
	CheckCat('%', kccPo);

	CheckCat('<', kccSm);
	CheckCat(0xA2, kccSc); // CENT SIGN
	CheckCat(0x5E, kccSk); // CIRCUMFLEX ACCENT
	CheckCat(0xA9, kccSo); // COPYRIGHT SIGN


	// Spot check boolean query methods for various characters.
	ComBool fRes;

#   define CheckPropHrBool(prop, ch, fExpected) \
	{ \
		hr = m_qpropeng->get_##prop(ch, &fRes); \
		CheckHrBool(ch, hr, fExpected, fRes, #prop); \
	}


	CheckPropHrBool(IsLetter, 'y', true);
	CheckPropHrBool(IsLetter, 0xAA, true); // FEMININE ORDINAL INDICATOR
	CheckPropHrBool(IsLetter, 0xBF, false); // INVERTED QUESTION MARK
	// subcategories of Letter
	CheckPropHrBool(IsUpper, 'y', false);
	CheckPropHrBool(IsUpper, 'Y', true);
	CheckPropHrBool(IsUpper, '#', false); // non-letter
	CheckPropHrBool(IsLower, 'a', true);
	CheckPropHrBool(IsLower, 0x0115, true); // LATIN SMALL LETTER E WITH BREVE
	CheckPropHrBool(IsLower, 0x0114, false); // LATIN CAPITAL LETTER E WITH BREVE
	CheckPropHrBool(IsLower, '8', false); // non-letter
	CheckPropHrBool(IsTitle, 'A', false);
	CheckPropHrBool(IsTitle, 0x1F8E, true); // GREEK CAPITAL LETTER ALPHA WITH...
	CheckPropHrBool(IsTitle, ' ', false);
	CheckPropHrBool(IsModifier, 0x3005, true); // IDEOGRAPHIC ITERATION MARK
	CheckPropHrBool(IsModifier, 'Q', false);
	CheckPropHrBool(IsModifier, '$', false);
	CheckPropHrBool(IsOtherLetter, 0x00E2, false); // LATIN SMALL LETTER A WITH CIRCUMFLEX
	CheckPropHrBool(IsOtherLetter, 0xF900, true); // CJK COMPATIBILITY IDEOGRAPH-F900
	CheckPropHrBool(IsOtherLetter, '{', false);

	CheckPropHrBool(IsPunctuation, '$', false);
	CheckPropHrBool(IsPunctuation, ';', true);
	CheckPropHrBool(IsPunctuation, 0x037e, true); // GREEK QUESTION MARK
	// subcategories of Punctuation
	CheckPropHrBool(IsOpen, '$', false);
	CheckPropHrBool(IsOpen, ']', false);
	CheckPropHrBool(IsOpen, 'a', false);
	CheckPropHrBool(IsOpen, '(', true);
	CheckPropHrBool(IsOpen, 0xFD3E, true); // ORNATE LEFT PARENTHESIS
	CheckPropHrBool(IsClose, '(', false);
	CheckPropHrBool(IsClose, '+', false);
	CheckPropHrBool(IsClose, 'G', false);
	CheckPropHrBool(IsClose, ']', true);
	CheckPropHrBool(IsClose, 0x0F3B, true); // TIBETAN MARK GUG RTAGS GYAS
	CheckPropHrBool(IsWordMedial, '*', false);
	CheckPropHrBool(IsWordMedial, 'b', false);
	CheckPropHrBool(IsWordMedial, ')', false);
	CheckPropHrBool(IsWordMedial, '_', true);
	CheckPropHrBool(IsWordMedial, 0x203F, true); // UNDERTIE

	CheckPropHrBool(IsNumber, 'I', false);
	CheckPropHrBool(IsNumber, '9', true);
	CheckPropHrBool(IsNumber, 0x2160, true); // ROMAN NUMERAL ONE
	CheckPropHrBool(IsNumber, 0x09F4, true); // BENGALI CURRENCY NUMERATOR ONE

	CheckPropHrBool(IsSeparator, '-', false);
	CheckPropHrBool(IsSeparator, ' ', true);
	CheckPropHrBool(IsSeparator, 0x2003, true); // EM SPACE

	CheckPropHrBool(IsSymbol, '?', false);
	CheckPropHrBool(IsSymbol, '$', true);
	CheckPropHrBool(IsSymbol, 0xA6, true);  // BROKEN BAR

	CheckPropHrBool(IsMark, 0x02ff, false); // undefined
	CheckPropHrBool(IsMark, 0x0302, true);  // COMBINING CIRCUMFLEX ACCENT
	CheckPropHrBool(IsMark, 0x0488, true); // COMBINING CYRILLIC HUNDRED THOUSANDS SIGN

	CheckPropHrBool(IsOther, '{', false);
	CheckPropHrBool(IsOther, 0x80, true);
	CheckPropHrBool(IsOther, 0xD800, true); // <Non Private Use High Surrogate, First>
	CheckPropHrBool(IsOther, 0xD800, false); // test of test
	CheckPropHrBool(IsControl, '*', false);
	CheckPropHrBool(IsControl, '\0', true);
	CheckPropHrBool(IsControl, 0x9F, true);
	CheckPropHrBool(IsControl, 0x070F, false); // SYRIAC ABBREVIATION MARK


	// TODO LarsH: still need to test other property methods, such as
	// decomposition.  How do we test IsUserDefinedClass?
}


/*----------------------------------------------------------------------------------------------
	Test that get_GeneralCategory returns the expected category for the given character.
	If not, fail and do not return.
----------------------------------------------------------------------------------------------*/
void TestCharacterPropertyEngine::CheckCat(int ch, LgGeneralCharCategory ccExpected)
{
	HRESULT hr;
	LgGeneralCharCategory ccActual;

	hr = m_qpropeng->get_GeneralCategory(ch, &ccActual);
	TestHrFail(hr, "Couldn't get general category");
	if (ccActual != ccExpected)
		FailureFormat("Wrong GeneralCategory for character U+%x: got %d instead of %d",
			ch, ccActual, ccExpected);
}

/*----------------------------------------------------------------------------------------------
	Test that the given TestMethod returns the expected boolean value for the given character.
	If not, fail and do not return.
----------------------------------------------------------------------------------------------*/
void TestCharacterPropertyEngine::CheckHrBool(int ch, HRESULT hr, bool fExpected, bool fActual,
	char * szFname)
{
	TestHrFailFormat(hr, "get_%s(U+%x) failed", szFname, ch);
	if (fActual != fExpected)
		FailureFormat("get_%s(U+%x) gave wrong result", szFname, ch);
}

/*----------------------------------------------------------------------------------------------
	Exhaustively test the property methods of the LgCharacterPropertyEngine class
	by parsing the UnicodeData.txt file and checking that the get_* methods give
	results consistent with this data.  Do not check error conditions like NULL ptrs.
	Return if all tests succeed; do not return otherwise.
	REVIEW LarsH: will this really not return after *any* failure?
----------------------------------------------------------------------------------------------*/

void ExhTestCPEngine::ExhTestProperties()
{
	HRESULT hr;

	// REVIEW LarsH: how do we pass in the location of the UnicodeData.txt file as
	// a parameter?  Env var?
	StrAnsi staDataFile = L"d:\\lars\\Unicode\\UnicodeData.txt";
	FILE *pFileIn = fopen(staDataFile, "r");
	if (!pFileIn)
		FailureFormat("Unable to open Unicode data file %s", staDataFile);

	CodePtData cpdCurrent, cpdNext;
	cpdNext.chCodeValue = -1;	// uninitialized

	// TODO LarsH: We should test through 0x10FFFF, but currently there are
	// some problems with higher characters which have been postponed,
	// so in order to avoid a humungous error log we'll stick with the first page for now.
	// Raid #31
#define JUST_FIRST_PAGE
#ifdef JUST_FIRST_PAGE
	for (int ch = 0; ch <= 0xFFFF; ch++)
#else
	for (int ch = 0; ch <= 0x10FFFF; ch++)
#endif
	{
		if (! (ch % 0x1000))
			LogFormat("%x:\n", ch);

		// Read in the next record unless we've already got this data
		if (cpdNext.chCodeValue < ch)
			ParseRecord(pFileIn, &cpdNext);
		// This is NOT an "else"; it may happen in addition to the above.
		if (cpdNext.chCodeValue == ch)
		{
			// got the right record; copy it over
			cpdCurrent.chCodeValue = ch;
			strcpy(cpdCurrent.szCategory, cpdNext.szCategory);
			wcscpy(cpdCurrent.szwName, cpdNext.szwName);
			cpdCurrent.nNumericValue = cpdNext.nNumericValue;
			cpdCurrent.nCombiningClass = cpdNext.nCombiningClass;
			wcscpy(cpdCurrent.szwDecomposition, cpdNext.szwDecomposition);
			cpdCurrent.chUpper = cpdNext.chUpper;
			cpdCurrent.chLower = cpdNext.chLower;
			cpdCurrent.chTitle = cpdNext.chTitle;
		}

		// Treat special ranges specially:
		if (ch >= 0x3400 && ch <= 0x4db5 || ch >= 0x4e00 && ch <= 0x9fa5)
		{
			// CJK Ideographs (Extension A and other)
			// Most data is same as previous, so don't need to load;
			// but must set names algorithmically.
			swprintf(cpdCurrent.szwName, L"CJK UNIFIED IDEOGRAPH-%X", ch);
		}
		else if (ch >= 0xac00 && ch <= 0xd7a3)
		{
			// Hangul Syllables
			// Data for 0xAC00 is loaded, and applies to all others, except
			//  name and decomp.
			HangulDecomposition(ch, cpdCurrent.szwDecomposition);
			HangulName(cpdCurrent.szwDecomposition, cpdCurrent.szwName);
		}
		else if (ch >= 0xd800 &&
			  // ch <= 0xdb7f || ch >= 0xdb80 && ch <= 0xdbff ||
			  // ch >= 0xdc00 && ch <= 0xdfff || ch >= 0xe000
				 ch <= 0xf8ff)
		{
			// Consecutive ranges: Non-Private Use High Surrogates,
			// Private Use High Surrogates, Low Surrogates, Private Use Area.
			// All of these are nameless (UnicodeData File Format v3.0.0)
			// TODO LarsH: change all this before closing Raid #24.
			cpdCurrent.szwName[0] = '\0';
		}
		else if (ch == 0xfffe || ch == 0xffff ||  // explicitly "not Unicode chars"
			cpdNext.chCodeValue > ch || (cpdNext.chCodeValue < ch && feof(pFileIn)))
		{
			// Not assigned in the data file.
			cpdCurrent.chCodeValue = ch;
			cpdCurrent.szwName[0] = '\0';
			strcpy(cpdCurrent.szCategory, "Cn");
			cpdCurrent.nCombiningClass = 0;
			// Must distinguish between lack of a NumericValue and a zero NumericValue.
			cpdCurrent.nNumericValue = -1;
			cpdCurrent.szwDecomposition[0] = '\0';
			// Zero is not an anycase equivalent of any character.
			cpdCurrent.chLower = cpdCurrent.chUpper = cpdCurrent.chTitle = ch;
		}

		// ** If looking for a character with a specific combination of properties,
		// ** you can test it here.  This would be better done in SQL.
		//	if (cpdCurrent.szCategory[0] != 'L' && cpdCurrent.chUpper > 0)
		//		LogFormat("*** U+%x fulfills conditions\n");
		//	continue; // skip all the usual tests if desired

		// Make sure the property interface methods get the same data.
		LgGeneralCharCategory cc = ParseCC(cpdCurrent.szCategory);
		CheckCat(ch, cc);

		ComBool fLet, fPun, fNum, fSep, fSym, fMar, fOth, fAll;
#define CheckBoolProp(prop, var, index, letter) \
		{ \
			hr = m_qpropeng->get_##prop(ch, &var); \
			WarnHr(hr); \
			if (var != (cpdCurrent.szCategory[index] == letter)) \
				FailureFormat("get_" #prop "(U+%x) gave wrong answer", ch); \
		}

		CheckBoolProp(IsLetter,      fLet, 0, 'L');
		CheckBoolProp(IsPunctuation, fPun, 0, 'P');
		CheckBoolProp(IsNumber,      fNum, 0, 'N');
		CheckBoolProp(IsSeparator,   fSep, 0, 'Z');
		CheckBoolProp(IsSymbol,      fSym, 0, 'S');
		CheckBoolProp(IsMark,        fMar, 0, 'M');
		CheckBoolProp(IsOther,       fOth, 0, 'C');

		// REVIEW LarsH: is int(ComBool) guaranteed to be 1 if true?  I think so.
		int nCat;
		if ((nCat = fLet + fPun + fNum + fSep + fSym + fMar + fOth) != 1)
			FailureFormat("Codepoint U+%x is in %d categories instead of one.",
				ch, nCat);

		if (fLet)
		{
			ComBool fUpp, fLow, fTit, fMod, fOth;
			CheckBoolProp(IsUpper,    fUpp, 1, 'u');
			CheckBoolProp(IsLower,    fLow, 1, 'l');
			CheckBoolProp(IsTitle,    fTit, 1, 't');
			CheckBoolProp(IsModifier, fMod, 1, 'm');
			CheckBoolProp(IsOtherLetter,    fOth, 1, 'o');
			if ((nCat = fUpp + fLow + fTit + fMod + fOth) != 1)
				FailureFormat("Letter U+%x is in %d subcategories instead of one.",
					ch, nCat);
		}
		else
		{
			// All should be false.
			CheckBoolProp(IsUpper,    fAll, 0, 'L');
			CheckBoolProp(IsLower,    fAll, 0, 'L');
			CheckBoolProp(IsTitle,    fAll, 0, 'L');
			CheckBoolProp(IsModifier, fAll, 0, 'L');
			CheckBoolProp(IsOtherLetter, fAll, 0, 'L');
		}

		if (fPun)
		{
			ComBool fOpe, fClo, fMed;
			CheckBoolProp(IsOpen,       fOpe, 1, 's');
			CheckBoolProp(IsClose,      fOpe, 1, 'e');
			CheckBoolProp(IsWordMedial, fOpe, 1, 'c');
			// TODO LarsH: update this if definition in Language.idh changes, e.g.
			// if the above methods come to be true for Pd, Pi, Pf
		}
		else
		{
			CheckBoolProp(IsOpen,       fAll, 0, 'P');
			CheckBoolProp(IsClose,      fAll, 0, 'P');
			CheckBoolProp(IsWordMedial, fAll, 0, 'P');
		}

		// IsControl should return true if category is Cc, false otherwise:
		CheckBoolProp(IsControl, fAll, 1, (cpdCurrent.szCategory[0] == 'C' ? 'c' : 0));

#undef CheckBoolProp

#define IsLetter() (cpdCurrent.szCategory[0] == 'L')
#define IsUpper() (cpdCurrent.szCategory[1] == 'u')
#define IsLower() (cpdCurrent.szCategory[1] == 'l')
#define IsTitle() (cpdCurrent.szCategory[1] == 't')
		// We must check IsLetter() etc. because the docs say, if it's not Lu/Ll/Tt, ToUpper() etc.
		// don't do case conversion, even if the Unicode database gives a conversion.

		int chUpper = -1, chLower = -1, chTitle = -1;

		// TODO LarsH: some of these should check different locales
		// if such data exists in SpecialCasing.txt

		hr = m_qpropeng->get_ToUpperCh(ch, &chUpper);
		if (FAILED(hr))
			FailureFormat("get_ToUpperCh(0x%x) gave result %s", ch, AsciiHresult(hr));
		int chUExp = (IsLetter() && (IsLower() || IsTitle()) && cpdCurrent.chUpper) ?
			cpdCurrent.chUpper : ch;
		if (chUpper != chUExp)
		{
			FailureFormat("get_ToUpperCh(0x%x {%s}) returned 0x%x instead of 0x%x",
				ch, cpdCurrent.szCategory, chUpper, chUExp);
		}

		hr = m_qpropeng->get_ToLowerCh(ch, &chLower);
		if (FAILED(hr))
			FailureFormat("get_ToLowerCh(0x%x) gave result %s", ch, AsciiHresult(hr));
		int chLExp = (IsLetter() && (IsUpper() || IsTitle()) && cpdCurrent.chLower) ?
			cpdCurrent.chLower : ch;
		if (chLower != chLExp)
		{
			FailureFormat("get_ToLowerCh(0x%x {%s}) returned 0x%x instead of 0x%x",
				ch, cpdCurrent.szCategory, chLower, chLExp);
		}
		hr = m_qpropeng->get_ToTitleCh(ch, &chTitle);
		if (FAILED(hr))
			FailureFormat("get_ToTitleCh(0x%x) gave result %s", ch, AsciiHresult(hr));
#define FIXED_BUG_198 // just for now
#ifdef FIXED_BUG_198
		int chTExp = (IsLetter() && (IsUpper() || IsLower())) ?
			(cpdCurrent.chTitle ? cpdCurrent.chTitle : cpdCurrent.chUpper ? cpdCurrent.chUpper : ch) :
			ch;
		// ToTitleCh() returns the uppercase if there is no titlecase value defined

		if (chTitle != chTExp)
		{
			FailureFormat("get_ToTitleCh(0x%x {%s}) returned 0x%x instead of 0x%x",
				ch, cpdCurrent.szCategory, chTitle, chTExp);
		}
#endif // FIXED_BUG_198

		// TODO LarsH: test get_ToLower, etc. (BSTR functions)
		// These should take into account any special casing (SpecialCasing.txt), by testing
		// different locales (esp. Turkish) and contexts (FINAL/NON_FINAL, MODERN/NON_MODERN).
		wchar pwz[2];
		pwz[0] = (wchar)ch;
		pwz[1] = '\0';
		SmartBstr sbstrFrom(pwz), sbstrTo = L"fish";

		// This one properly gives a warning if ch is half of a surrogate pair.
		if (ch >= 0xd800 && ch <= 0xdfff) // half a surrogate pair
		{
			IgnoreWarnings iw;
			hr = m_qpropeng->ToLower(sbstrFrom, &sbstrTo);
			TestFailZero(sbstrTo, "ToLower");
			if (SUCCEEDED(hr))
				FailureFormat("ToLower with 0x%x (half a surrogate pair) should have failed", ch);
		}
		else
		{
			hr = m_qpropeng->ToLower(sbstrFrom, &sbstrTo);
			TestFailZero(sbstrTo, "ToLower");
			WarnHr(hr);
			// TODO LarsH: check that the return value from get_ToLower is correct
			// TODO LarsH: use smart bstrs where possible
		}


		// TODO LarsH: test get_ToLowerRgch, etc. (Rgch functions)

		// TODO LarsH: test IsUserDefinedClass().  Right now there is no way to define
		// classes, so this will always return false.
		ComBool fRet;
		hr = m_qpropeng->get_IsUserDefinedClass(ch, 'A', &fRet);
		WarnHr(hr);
		if (fRet)
			FailureFormat("Character U+%x is a member of class 'A'??", ch);

		// TODO LarsH: test SoundAlikeKey

		// REVIEW LarsH: is this bstr causing a memory leak on every iteration of ch?
		SmartBstr sbstr = L"foo";

		// TODO LarsH: Remove this kludge once get_CharacterName(Hangul) is working.
		if (ch >= 0xac00 && ch <= 0xd7a3)
			goto skipHangul;

		hr = m_qpropeng->get_CharacterName(ch, &sbstr);
		TestFailZero(sbstr, "get_CharacterName");
		if (FAILED(hr))
			FailureFormat("get_CharacterName(U+%x) failed", ch);
		else if (
#ifndef FIXED_BUG_24
			!(ch >= 0xd800 && ch <= 0xf8ff) &&
#endif
				wcscmp(cpdCurrent.szwName, sbstr))
		{
			FailureFormat(L"get_CharacterName(U+%x) returned \"%s\" instead of \"%s\".",
				ch, sbstr, cpdCurrent.szwName);
		}
		// if (sbstr)
		//  	SysFreeString(sbstr);

#ifdef FIXED_BUG_125
		sbstr = L"foo";
		hr = m_qpropeng->get_Decomposition(ch, &sbstr);
		TestFailZero(sbstr, "get_Decomposition");
		if (FAILED(hr))
			FailureFormat("get_Decomposition(U+%x) failed", ch);
		// REVIEW LarsH: Does wcscmp understand NULL sbstrs?  Apparently...
		else if (sbstr ? wcscmp(cpdCurrent.szwDecomposition, sbstr) :
				cpdCurrent.szwDecomposition[0])
		{
			FailureFormat("Got wrong decomposition for U+%x", ch);
		}
		// if (sbstr)
		// 	SysFreeString(bstr);
#endif // FIXED_BUG_125

		sbstr = NULL;

		// TODO LarsH: test DecompositionRgch
		// That will probably require a separate pass through the whole UnicodeData
		// file to build up a hash table or sthg...



skipHangul:

		int nNumericValue;
		{
			IgnoreWarnings iw;
			hr = m_qpropeng->get_NumericValue(ch, &nNumericValue);
		}
		if (fNum && (cc == kccNd || cc == kccNl || cc == kccNo) && cpdCurrent.nNumericValue > -1)
		{
			if (FAILED(hr))
				FailureFormat("get_NumericValue(U+%x) failed with hr=%s", ch, AsciiHresult(hr));
			else if (nNumericValue != cpdCurrent.nNumericValue)
			{
				FailureFormat("get_NumericValue(U+%x) returned %d instead of %d",
					ch, nNumericValue, cpdCurrent.nNumericValue);
			}
		}
		else if (hr != E_UNEXPECTED)
		{
			FailureFormat("get_NumericValue(U+%x {%s}) gave result %s instead of E_UNEXPECTED",
				ch, cpdCurrent.szCategory, AsciiHresult(hr));
		}

#define FIXED_BUG_18 1  // Seems to be fixed now.
#ifdef FIXED_BUG_18
		int nCombClass;
		hr = m_qpropeng->get_CombiningClass(ch, &nCombClass);
		WarnHr(hr);
		if (nCombClass != cpdCurrent.nCombiningClass)
		{
			FailureFormat("get_CombiningClass(U+%x) gave %d instead of %d",
				ch, nCombClass, cpdCurrent.nCombiningClass);
		}
#endif
		// TODO LarsH: test everything else


		// TODO LarsH: test get_Comment

		// TODO LarsH: test GetLineBreakProps if not obsolete
		// TODO LarsH: test GetLineBreakStatus if not obsolete

	}
}

/*----------------------------------------------------------------------------------------------
	Test that get_GeneralCategory returns the expected category for the given character.
	If not, fail and do not return.  (Same as TestCharacterPropertyEngine::CheckCat())
----------------------------------------------------------------------------------------------*/
void ExhTestCPEngine::CheckCat(int ch, LgGeneralCharCategory ccExpected)
{
	HRESULT hr;
	LgGeneralCharCategory ccActual;

	hr = m_qpropeng->get_GeneralCategory(ch, &ccActual);
	TestHrFail(hr, "Couldn't get general category");
	if (ccActual != ccExpected)
		FailureFormat("Wrong GeneralCategory for character U+%x: got %d instead of %d",
			ch, ccActual, ccExpected);
}


/*----------------------------------------------------------------------------------------------
   Attempt to read a Unicode record from input file.
   If successful, populate record structure.  Otherwise, just return.
----------------------------------------------------------------------------------------------*/

void ExhTestCPEngine::ParseRecord(FILE * pFileIn, CodePtData * pcpd)
{
	if (feof(pFileIn))
		return;

	// REVIEW LarsH: do we know an official limit on line length for this file?
	char szLineBuffer[knDataBufSize];
	char * pch;
	int n;

	pcpd->chCodeValue = -1;
	pcpd->szwName[0] = '\0';
	pcpd->szCategory[0] = '\0';
	pcpd->nCombiningClass = 0;
	pcpd->szwDecomposition[0] = '\0';
	pcpd->nNumericValue = -1;
	// Default for case mapping: no change
	pcpd->chLower = pcpd->chUpper = pcpd->chTitle = 0;

	// No data to read?
	if (!fgets(szLineBuffer, knDataBufSize, pFileIn) || szLineBuffer[0] == '\n')
		return;

#define NextField { pch = strchr(pch, ';') + 1; }

	// Possible input: "0031;DIGIT ONE;Nd;0;EN;;1;1;1;N;;;;;"
	pch = szLineBuffer;
	//  0. Code Value field
	sscanf(pch, "%4x", &pcpd->chCodeValue);
	NextField;
	//  1. Character Name field
	for (n = 0; *pch != ';'; )
		pcpd->szwName[n++] = (OLECHAR)*pch++;
	pcpd->szwName[n] = '\0';
	pch++;
	//  2. General Category field
	strncpy(pcpd->szCategory, pch, 2);
	pcpd->szCategory[2] = '\0';
	NextField;
	//  3. Canonical Combining Class field
	pcpd->nCombiningClass = atoi(pch);
	NextField;
	//  4. Bidi cat field
	NextField;

	//  5. Character decomposition mapping field
	if (*pch != ';')
	{
		// we need to parse the decomposition.
		OLECHAR * q = pcpd->szwDecomposition;

		// skip initial tag, if any
		if (*pch == '<')
		{
			pch = strchr(pch, '>');
			if (!pch)
				goto malformed;
			else pch++;
		}

		do
		{
			while (isspace(*pch))
				pch++;
			if (*pch == ';')
				break;
			if (sscanf(pch, "%4x", &n) < 1)
			{
malformed:
				FailureFormat("Char U+%x: Malformed decomposition \"%s\"",
					pcpd->chCodeValue, pch);
			}
			*q++ = (OLECHAR)n;
			pch += strspn(pch, "0123456789abcdefABCDEF");

		} while (*pch != ';');
		*q = '\0';
	}

	pch++;
	//  6. Decimal Digit Value field
	NextField;
	//  7. Digit value field
	NextField;
	//  8. Numeric value field
	if (*pch != ';')
	{
		// Here we need to treat a fraction as an invalid value, because we can't handle it.
		char * q = strchr(pch, '/');
		if (!q || q > strchr(pch, ';'))
			pcpd->nNumericValue = atoi(pch);
		// else leave it at -1.
	}
	NextField;
	//  9. Mirrored field
	NextField;
	// 10. Unicode 1.0 name field
	NextField;
	// 11. 10646 comment field
	NextField;


	// 12. Uppercase mapping field
	if (*pch != ';' && sscanf(pch, "%4x", &pcpd->chUpper) != 1)
		FailureFormat("Bad format of uppercase mapping field [%s]", pch);
	NextField;
	// 13. Lowercase mapping field
	if (*pch != ';' && sscanf(pch, "%4x", &pcpd->chLower) != 1)
		FailureFormat("Bad format of lowercase mapping field [%s]", pch);
	NextField;
	// 14. Titlecase mapping field
	if (*pch && *pch != '\n' && *pch != ';' && sscanf(pch, "%4x", &pcpd->chTitle) != 1)
		FailureFormat("Bad format of titlecase mapping field [%s]", pch);

	return;
}

/*----------------------------------------------------------------------------------------------
   Convert a two-character string such as "Lu" into a constant, like kccLu.
   Issue failure message on error.
----------------------------------------------------------------------------------------------*/

enum LgGeneralCharCategory ExhTestCPEngine::ParseCC(char * szCC)
{
	switch (szCC[0])
	{
	case 'L':
		switch (szCC[1])
		{
		case 'u': return kccLu;
		case 'l': return kccLl;
		case 't': return kccLt;
		case 'm': return kccLm;
		case 'o': return kccLo;
		}
		break;
	case 'M':
		switch (szCC[1])
		{
		case 'n': return kccMn;
		case 'c': return kccMc;
		case 'e': return kccMe;
		}
		break;
	case 'N':
		switch (szCC[1])
		{
		case 'd': return kccNd;
		case 'l': return kccNl;
		case 'o': return kccNo;
		}
		break;
	case 'Z':
		switch (szCC[1])
		{
		case 's': return kccZs;
		case 'l': return kccZl;
		case 'p': return kccZp;
		}
		break;
	case 'C':
		switch (szCC[1])
		{
		case 'c': return kccCc;
		case 'f': return kccCf;
		case 's': return kccCs;
		case 'o': return kccCo;
		case 'n': return kccCn;
		}
		break;
	case 'P':
		switch (szCC[1])
		{
		case 'c': return kccPc;
		case 'd': return kccPd;
		case 's': return kccPs;
		case 'e': return kccPe;
		case 'i': return kccPi;
		case 'f': return kccPf;
		case 'o': return kccPo;
		}
		break;
	case 'S':
		switch (szCC[1])
		{
		case 'm': return kccSm;
		case 'c': return kccSc;
		case 'k': return kccSk;
		case 'o': return kccSo;
		}
		break;
	}

	FailureFormat("Malformed CharCategory %s", szCC);
	// The above doesn't return, but we may need the following to satisfy the compiler.
	return kccCc;
}

// Constants for Hangul decomposition and name computation
const int knSBase = 0xAC00, knLBase = 0x1100, knVBase = 0x1161, knTBase = 0x11A7;
const int knLCount = 19, knVCount = 21, knTCount = 28;
const int knNCount = knVCount * knTCount, knSCount = knLCount * knNCount;

/*----------------------------------------------------------------------------------------------
	Compute the decomposition string for a Hangul character; put result in szwDecompOut,
	a buffer whose size is assumed to be at least 4 wide chars.
	Fail if ch is not in Hangul range.
----------------------------------------------------------------------------------------------*/

void ExhTestCPEngine::HangulDecomposition(int ch, OLECHAR * szwDecompOut)
{
	// see Unicode Technical Report 15

	int inS = ch - knSBase;
	if (inS < 0 || inS >= knSCount)
		FailureFormat("Called HangulDecomposition() on non-Hangul character U+%x", ch);

	OLECHAR *p = szwDecompOut;
	int nL = knLBase + inS / knNCount;
	int nV = knVBase + (inS % knNCount) / knTCount;
	int nT = knTBase + inS % knTCount;
	*p++ = (OLECHAR)nL;
	*p++ = (OLECHAR)nV;
	if (nT != knTBase)
		*p++ = (OLECHAR)nT;
	*p = '\0';
}


/*----------------------------------------------------------------------------------------------
	Compute the name for a Hangul character whose decomposition is in szwDecompIn;
	put result in szwNameOut, a buffer whose size is assumed to be at least 25 wide chars.
----------------------------------------------------------------------------------------------*/

void ExhTestCPEngine::HangulName(OLECHAR * szwDecompIn, OLECHAR * szwNameOut)
{
	// Unicode Technical Report #15
	static char rgszJamoLTable[19][3] = {
		"G", "GG", "N", "D", "DD", "R", "M", "B", "BB",
		"S", "SS", "", "J", "JJ", "C", "K", "T", "P", "H"
	};
	static char rgszJamoVTable[21][4] = {
		"A", "AE", "YA", "YAE", "EO", "E", "YEO", "YE", "O",
		"WA", "WAE", "OE", "YO", "U", "WEO", "WE", "WI",
		"YU", "EU", "YI", "I"
	};
	static char rgszJamoTTable[28][3] = {
		"", "G", "GG", "GS", "N", "NJ", "NH", "D", "L", "LG", "LM",
		"LB", "LS", "LT", "LP", "LH", "M", "B", "BS",
		"S", "SS", "NG", "J", "C", "K", "T", "P", "H"
	};

	swprintf(szwNameOut, L"HANGUL SYLLABLE %s%s%s",
		rgszJamoLTable[szwDecompIn[0] - knLBase],
		rgszJamoVTable[szwDecompIn[1] - knVBase],
		rgszJamoTTable[szwDecompIn[2] ? szwDecompIn[2] - knTBase : 0]);
}
