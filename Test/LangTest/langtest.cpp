/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: langtest.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Tests for the language component of Fieldworks services.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)
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
static LangTest7 g_lt7Instance;
static LangTest6 g_lt6Instance;
static LangTest5 g_lt5Instance;
static LangTest4 g_lt4Instance;
static LangTest3 g_lt3Instance;
static LangTest2 g_lt2Instance;
static LangTest1 g_lt1Instance;

/***********************************************************************************************
	LangTest1 Methods
***********************************************************************************************/

LangTest1::LangTest1()
:TestBase(L"System Collater tests", 1)
{
}

LangTest1::~LangTest1()
{
}

//StrUni LangTest1::s_stuName(L"System Collater tests");

HRESULT LangTest1::Run()
{
/* IClassInitMonikerPtr, CLSID_ClassInitMoniker undeclared...
	HRESULT hr;
	// Make a collater for English. Initialize it the standard way with a moniker.
	int lid = MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US);
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);
	hr = qcim->InitNew(CLSID_LgSystemCollater, (const BYTE *)&lid, isizeof(lid));
	WarnHr(hr);

	IBindCtxPtr qbc;
	hr = ::CreateBindCtx(NULL, &qbc);
	WarnHr(hr);
	hr = qcim->BindToObject(qbc, NULL, IID_ILgCollatingEngine, (void **) &m_qcoleng);
	WarnHr(hr);

	StrUni stuValBlank = L"";
	StrUni stuValA = L"a";
	StrUni stuValB = L"b";
	StrUni stuValAUpper = L"A";
	StrUni stuValAnd = L"and";
	StrUni stuValAny = L"any";
	StrUni stuValAndUpper = L"And";

	// Try a sequence of test comparisions.
	TestCompare(stuValBlank, stuValBlank, 0);
	TestCompare(stuValBlank, stuValA, -1);
	TestCompare(stuValA, stuValBlank, 1);
	TestCompare(stuValA, stuValB, -1);
	TestCompare(stuValB, stuValA, 1);
	TestCompare(stuValA, stuValAUpper, -1);
	TestCompare(stuValB, stuValAUpper, 1);
	TestCompare(stuValA, stuValAnd, -1);
	TestCompare(stuValAny, stuValAnd, 1);
	TestCompare(stuValAny, stuValAndUpper, 1);
	TestCompare(stuValAnd, stuValAnd, 0);

	BSTR bstrKeyAnd;
	BSTR bstrKeyAndUpper;
	BSTR bstrKeyAny;
	m_qcoleng->get_SortKey(stuValAnd.Bstr(), fcoIgnoreCase, &bstrKeyAnd);
	m_qcoleng->get_SortKey(stuValAndUpper.Bstr(), fcoIgnoreCase, &bstrKeyAndUpper);
	m_qcoleng->get_SortKey(stuValAny.Bstr(), fcoIgnoreCase, &bstrKeyAny);

	if(wcscmp(bstrKeyAnd, bstrKeyAndUpper))
	{
		Failure("String compare failed");
		Warn("String compare failed");
	}
	if (wcscmp(bstrKeyAnd, bstrKeyAny) >= 0)
	{
		Failure("String compare failed");
		Warn("String compare failed");
	}
	m_qcoleng.Clear();
*/
	return S_OK;
}

void LangTest1::TestCompare(StrUni stuVal1, StrUni stuVal2, int nExpected)
{
	HRESULT hr;

	int nActual;
	hr = m_qcoleng->Compare(stuVal1.Bstr(), stuVal2.Bstr(), fcoDefault, &nActual);
	if (FAILED(hr))
	{
		Failure("String compare failed");
		Warn("String compare failed");
		WarnHr(hr);
		return;
	}

	if (nExpected == nActual)
		return; // 0 == 0 for equal
	if (nExpected < 0 && nActual < 0)
		return;
	if (nExpected > 0 && nActual > 0)
		return;
	Failure("String compare gave wrong result");
		Warn("String compare gave wrong result");
}


/***********************************************************************************************
	LangTest2 Methods
***********************************************************************************************/

LangTest2::LangTest2()
:TestBase(L"Character property tests", 2)
{
		m_stuBsl = "LgCharProp";
}

LangTest2::~LangTest2()
{
}

HRESULT LangTest2::Run()
{
// IClassInitMonikerPtr, CLSID_ClassInitMoniker undeclared...
	HRESULT hr;
//	ILgCharacterPropertyEnginePtr qlcpe;
	m_qlcpe.CreateInstance(CLSID_LgCharacterPropertyEngine);

	// And now for the tests...
#if 0
	ComBool fRes;

	//-------------------------------------------
	// Basic character categories

	// Get a negative for each main category
	hr = m_qlcpe->get_IsLetter(';', &fRes);
	CheckBoolHr(hr, fRes, false, L"; is not a letter");
	hr = m_qlcpe->get_IsPunctuation('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not punct");
	hr = m_qlcpe->get_IsNumber('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not a number");
	hr = m_qlcpe->get_IsSeparator('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not a sep");
	hr = m_qlcpe->get_IsSymbol('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not a symbol");
	hr = m_qlcpe->get_IsMark('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not a mark");
	hr = m_qlcpe->get_IsOther('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not an 'Other'");

	// Get a positive result for each main category
	hr = m_qlcpe->get_IsLetter('A', &fRes);
	CheckBoolHr(hr, fRes, true, L"A is a letter");
	hr = m_qlcpe->get_IsPunctuation('.', &fRes);
	CheckBoolHr(hr, fRes, true, L". is punct");
	hr = m_qlcpe->get_IsNumber('5', &fRes);
	CheckBoolHr(hr, fRes, true, L"5 is a number");
	hr = m_qlcpe->get_IsSeparator(' ', &fRes);
	CheckBoolHr(hr, fRes, true, L"space is a sep");
	hr = m_qlcpe->get_IsSymbol('$', &fRes);
	CheckBoolHr(hr, fRes, true, L"$ is a symbol");
	hr = m_qlcpe->get_IsMark(0x300, &fRes);
	CheckBoolHr(hr, fRes, true, L"0300 is a mark");
	hr = m_qlcpe->get_IsOther(1, &fRes);
	CheckBoolHr(hr, fRes, true, L"0001 is an Other");

	// Get a negative for each letter sub category
	hr = m_qlcpe->get_IsUpper('a', &fRes);
	CheckBoolHr(hr, fRes, false, L"a is not upper case");
	hr = m_qlcpe->get_IsLower('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not lower case");
	hr = m_qlcpe->get_IsTitle('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not title case");
	hr = m_qlcpe->get_IsModifier('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not a modifier");
	hr = m_qlcpe->get_IsOtherLetter('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not an 'Other letter'");

	// And a positive...
	hr = m_qlcpe->get_IsUpper('A', &fRes);
	CheckBoolHr(hr, fRes, true, L"A is upper case");
	hr = m_qlcpe->get_IsLower('z', &fRes);
	CheckBoolHr(hr, fRes, true, L"z is lower case");
	hr = m_qlcpe->get_IsTitle(0x01C5, &fRes);
	CheckBoolHr(hr, fRes, true, L"Dz (01C5) is title case");
	hr = m_qlcpe->get_IsModifier(0x02B1, &fRes);
	CheckBoolHr(hr, fRes, true, L"02B1 is a modifier");
	hr = m_qlcpe->get_IsOtherLetter(0x01BB, &fRes);
	CheckBoolHr(hr, fRes, true, L"01BB is an 'Other letter'");

	// Also check that a non-letter works.
	hr = m_qlcpe->get_IsUpper(',', &fRes);
	CheckBoolHr(hr, fRes, false, L", is not upper case");

	// Now the punctuation subcategories
	hr = m_qlcpe->get_IsOpen(')', &fRes);
	CheckBoolHr(hr, fRes, false, L") is not opening punct");
	hr = m_qlcpe->get_IsClose('(', &fRes);
	CheckBoolHr(hr, fRes, false, L"( is not closing punct");
	hr = m_qlcpe->get_IsWordMedial('(', &fRes);
	CheckBoolHr(hr, fRes, false, L"( is not word medial");

	hr = m_qlcpe->get_IsWordMedial('A', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not word medial punct");

	hr = m_qlcpe->get_IsOpen('(', &fRes);
	CheckBoolHr(hr, fRes, true, L"( is opening punct");
	hr = m_qlcpe->get_IsClose(')', &fRes);
	CheckBoolHr(hr, fRes, true, L") is closing punct");
	hr = m_qlcpe->get_IsWordMedial('_', &fRes);
	CheckBoolHr(hr, fRes, true, L"_ is word medial");

	// And IsControl.
	hr = m_qlcpe->get_IsControl(')', &fRes);
	CheckBoolHr(hr, fRes, false, L") is not a control char");
	hr = m_qlcpe->get_IsControl(1, &fRes);
	CheckBoolHr(hr, fRes, true, L"0001 is a control char");

	// Try some on a missing page
	hr = m_qlcpe->get_IsOther(0x800, &fRes);
	CheckBoolHr(hr, fRes, true, L"0800 is an Other");

	// Try some fill-ins
	hr = m_qlcpe->get_IsOther(0x6ff, &fRes);
	CheckBoolHr(hr, fRes, true, L"06FF is an Other");
	hr = m_qlcpe->get_IsOther(0x7b1, &fRes);
	CheckBoolHr(hr, fRes, true, L"07B1 is an Other");
	hr = m_qlcpe->get_IsOther(0x7ff, &fRes);
	CheckBoolHr(hr, fRes, true, L"07FF is an Other");
	hr = m_qlcpe->get_IsOther(0x900, &fRes);
	CheckBoolHr(hr, fRes, true, L"0900 is an Other");

	// And a marginal case
	hr = m_qlcpe->get_IsNumber(0x6f9, &fRes);
	CheckBoolHr(hr, fRes, true, L"06F9 is a number");

	//--------------------------------------------------
	// Case conversions

	TestUpperCh('a', 'A');
	TestUpperCh('A', 'A');
	TestUpperCh(';', ';');

	// Try converting all three variations of the DZ digraph.
	TestUpperCh(0x01f1, 0x01f1);
	TestUpperCh(0x01f2, 0x01f1);
	TestUpperCh(0x01f3, 0x01f1);

	TestUpper(L"a", L"A");
	TestUpper(L"A", L"A");
	TestUpper(L";", L";");

	TestUpper(L"\x01f1", L"\x01f1");
	TestUpper(L"\x01f2", L"\x01f1");
	TestUpper(L"\x01f3", L"\x01f1");

	// From specialCasing:
	// 1F80; 1F80; 1F88; 1F08 0399; # GREEK SMALL LETTER ALPHA WITH PSILI AND YPOGEGRAMMENI
	TestUpper(L"\x1F80", L"\x1f08\x0399");

	// This test is disabled because, although specialCasing says the above is the
	// right result, the main database file says that 0x1F88 is upper case, and
	// therefore it does not even attempt a conversion.
	// TestUpper(L"\x1F88", L"\x1f080399");

	// Lower case conversions
	TestLowerCh('a', 'a');
	TestLowerCh('A', 'a');
	TestLowerCh('[', '['); // right after Z
	TestLowerCh('Z', 'z');
	TestLowerCh('@', '@'); // right before A

	// Try converting all three variations of the DZ digraph.
	TestLowerCh(0x01f1, 0x01f3);
	TestLowerCh(0x01f2, 0x01f3);
	TestLowerCh(0x01f3, 0x01f3);

	// Greek
	// For now, this test is expected to fail, because there is a multi-character
	// conversion and we're asking for a single-char conversion.
	// REVIEW JohnT(LarsH): If the data structures and/or
	// LgCharacterPropertyEngine::SpecialOtherCase() are enhanced, this test
	// case can some day succeed.
	// TestLowerCh(0x1f88, 0x1f80, E_NOTIMPL); // comment out for now (JohnL) as S_OK returned
	// TODO JohnL: fix this up some time

	// Test lowercase conversion with string
	TestLower(L"FiSh\n", L"fish\n");

	// Title case mostly duplicate upper
	TestTitleCh('a', 'A');
	TestTitleCh('A', 'A');
	TestTitleCh(';', ';');
	// Try converting all three variations of the DZ digraph.
	TestTitleCh(0x01f1, 0x01f2);
	TestTitleCh(0x01f2, 0x01f2);
	TestTitleCh(0x01f3, 0x01f2);

	OLECHAR szRes6[] = {0x0397, 0x0342, 0x0345, 0};
	TestTitle(L"\x1fC7", szRes6);

	// A couple more near the end of special casing.
	OLECHAR szRes7[] = {0x03A9, 0x0342, 0x0345, 0};
	TestTitle(L"\x1FF7", szRes7);

	OLECHAR szRes8[] = {0x03A9, 0x0342, 0x0399, 0};
	TestUpper(L"\x1FF7", szRes8);

	// Looking for trouble... invalid Unicode characters.  szRes7 is a dummy.

	TestLowerCh(0x110000, 0, E_INVALIDARG);	// character out of range
	TestUpper(L"\xd801", szRes7, E_INVALIDARG);	// malformed surrogate
	TestLower(L"\xdc00 ", szRes7, E_INVALIDARG);	// also malformed
	TestTitle(L"\xd802 ", szRes7, E_INVALIDARG); // also malformed
	TestUpper(L"\xd801\xdc01", L"\xd801\xdc01", S_OK); // A valid surrogate pair.
	// Not really sure about what the converted string should be, but this works
	// for now as we don't have special information about surrogates.

	// NULL strings should be treated as empty, no error.
	BSTR bstrEmpty = NULL;
	TestLower(bstrEmpty, bstrEmpty);

	//--------------------------------------------------
	// Miscellaneous

	// IsUserDefinedClass is trivial for the main engine; it has no override mechanism.
	hr = m_qlcpe->get_IsUserDefinedClass('A', 'B', &fRes);
	CheckBoolHr(hr, fRes, false, L"A is not in user defined class");

	// Sound-alike key is not currently implemented.

	// DigitValue
	int nRes;
	hr = m_qlcpe->get_NumericValue('1', &nRes);
	CheckIntHr(hr, nRes, 1, L"1 has right digit value");

	hr = m_qlcpe->get_NumericValue(0xbf0, &nRes);
	CheckIntHr(hr, nRes, 10, L"Tamil 10 has right digit value");

	hr = m_qlcpe->get_NumericValue(0xbf1, &nRes);
	CheckIntHr(hr, nRes, 100, L"Tamil 10 has right digit value");

	hr = m_qlcpe->get_NumericValue(0xbf2, &nRes);
	CheckIntHr(hr, nRes, 1000, L"Tamil 1000 has right digit value");

	hr = m_qlcpe->get_NumericValue(0x216b, &nRes);
	CheckIntHr(hr, nRes, 12, L"Roman 12 has right digit value");

	hr = m_qlcpe->get_NumericValue(0x216c, &nRes);
	CheckIntHr(hr, nRes, 50, L"Roman 50 has right digit value");
	hr = m_qlcpe->get_NumericValue(0x216e, &nRes);
	CheckIntHr(hr, nRes, 500, L"Roman 500 has right digit value");
	hr = m_qlcpe->get_NumericValue(0x2181, &nRes);
	CheckIntHr(hr, nRes, 5000, L"Roman 5000 has right digit value");
	hr = m_qlcpe->get_NumericValue(0x2182, &nRes);
	CheckIntHr(hr, nRes, 10000, L"Roman 10000 has right digit value");

	hr = m_qlcpe->get_NumericValue(0x9f9, &nRes);
	CheckIntHr(hr, nRes, 16, L"Bengali denominator sixteen has right digit value");

	// Combining class
	hr = m_qlcpe->get_CombiningClass('A', &nRes);
	CheckIntHr(hr, nRes, 0, L"A has 0 combining class");
	hr = m_qlcpe->get_CombiningClass(0x0338, &nRes);
	CheckIntHr(hr, nRes, 1, L"tilde has 1 combining class");

	//--------------------------------------------------
	// Decompositions
	TestDecomp('A', L""); // no decomposition
	TestDecomp(0xa0, L" "); // one character

	OLECHAR szResD1[] = {0x0020, 0x0308, 0};
	TestDecomp(0xa8, szResD1); // two chars

	OLECHAR szResD2[] = {0x0031, 0x2044, 0x0032, 0};
	TestDecomp(0xbd, szResD2); // three chars

	OLECHAR szResD3[] = {0x0028, 0x31, 0x0030, 0x0029, 0};
	TestDecomp(0x247d, szResD3); // four chars

	OLECHAR szResD4[] = {0x0635, 0x0644, 0x0649, 0x0020, 0x0627, 0x0644, 0x0644, 0x0647,
		0x0020, 0x0639, 0x0644, 0x064A, 0x0647, 0x0020, 0x0648, 0x0633, 0x0644, 0x0645, 0};
	TestDecomp(0xFDFA, szResD4); // lots of chars

	TestFullDecomp('A', L""); // no decomposition
	TestFullDecomp(0xa0, L" "); // one character

	OLECHAR szResD5[] = {0x0020, 0x0308, 0};
	TestFullDecomp(0xa8, szResD5); // two chars

	OLECHAR szResD6[] = {0x0028, 0x31, 0x0030, 0x0029, 0};
	TestFullDecomp(0x247d, szResD6); // four chars

	TestFullDecomp(0xFFAF,L"\x11b5"); //two level decomp into one char

	OLECHAR szResD7[] = {0x0044, 0x005a, 0x030C, 0};
	TestFullDecomp(0x01C4, szResD7);

	OLECHAR szResD8[] = {0x03c9, 0x0313, 0x0301, 0x0345, 0};
	TestFullDecomp(0x1Fa4, szResD8);
	//--------------------------------------------------
	// Unicode character names
	CheckName('A', L"LATIN CAPITAL LETTER A");
	CheckName(0xfffd, L"REPLACEMENT CHARACTER");
	CheckName(0xfc95, L"ARABIC LIGATURE YEH WITH ALEF MAKSURA FINAL FORM");
	CheckName(0x4e02, L"CJK UNIFIED IDEOGRAPH-4E02");

	// Test Combining Class and baseline results

	int ch;
	int nccvalues[16];
	int i=0;
	hr = Baseline("Combining class values for U+05AC to U+05BC:\r\n");
	for (ch = 0x05AC; ch < 0x05BC; ch++)
	{
		hr = m_qlcpe->get_CombiningClass(ch, &nccvalues[i]);
		WarnHr(hr);
		++i;
	}
	BaselineItoAArray(nccvalues, 16);
	hr = Baseline("\r\n");
	// Note: values expected are 230, 222, 228, 230,  10,  11,  12,  13,
	//                            14,  15,  16,  17,  18,  19,   0,  20

	// Test numeric values
	int nnvalues[13];
	i=0;
	hr = Baseline("Numeric values for U+1370 to U+137D:\r\n");
	for (ch = 0x1370; ch < 0x137D; ch++)
	{
		hr = m_qlcpe->get_NumericValue(ch, &nnvalues[i]);
		WarnHr(hr);
		++i;
	}
	BaselineItoAArray(nnvalues, 13);
	hr = Baseline("\r\n");
	// Note: values expected are   8,   9,  10,  20,  30,  40,  50,  60,
	//                            70,  80,  90, 100, 10000
#endif
	// Test line breaking properties
	int i;
	OLECHAR rgchwTest[16];
	byte rglbpTest[16];
	hr = Baseline("Line Breaking Properties for U+FFE0 to U+FFEF:\r\n");
	unsigned short chw = 0xFFE0;
	for (i = 0; i < 16; ++i)
	{
		rgchwTest[i] = chw++ ; // characters U+FFE0 to U+FFEF
	}
	hr = m_qlcpe->GetLineBreakProps(&rgchwTest[0], 16, &rglbpTest[0]);
	BaselineBtoAArray(rglbpTest, 16);
	hr = Baseline("\r\n");
	//Note: values expected are   20, 21, 13, 13, 13, 21, 21, 27,
	//                             1,  1,  1,  1,  1,  1,  1, 27

	// Test line breaking algorithm
	byte rglbsTest1[100];
	byte rglbpTest1[100];
	LPCWSTR pszwTest1 = L"Is it 1%  or-not?\n   (John !) \"a \"b\" abc\x300G  \rxy pq\f 1-2";
	int cch = wcslen(pszwTest1);
	hr = Baseline("\r\nTest string for line breaking properties and status:\r\n");
	hr = Baseline("Is it 1%  or-not?<LF>   (John !) \"a \"b\" abc<CM>  <CR>xy pq<FF> 1-2\r\n");
	hr = Baseline("  where <LF> is 0x0A, <CM> is 0x300, <CR> is 0x0D, <FF> is 0x0C\r\n\r\n");
	hr = Baseline("Line breaking properties are:\r\n");
	hr = m_qlcpe->GetLineBreakProps(pszwTest1, cch, rglbpTest1);
	int ccb;
	for (i = 0; i < cch; i = i + 24)
	{
		ccb = ((cch - i) < 24) ? cch - i : 24; // output lines of 24; last line may be shorter
		BaselineBtoAArray(rglbpTest1 + i, ccb);
	}
	hr = Baseline("\r\n");
/*	Note: values expected are:
   1   1  153   1   1  153  18  20  153  153   1   1  12   1   1   1  10  16  153  153  153  19   1   1
   1   1  153  10   7  153  22   1  153  22   1  22  153   1   1   1   8   1  153  153   9   1   1  153
   1   1   5  153  18  12  18
*/


	hr = Baseline("Line breaking stati are:\r\n");
	hr = m_qlcpe->GetLineBreakStatus(rglbpTest1, cch, rglbsTest1);
	for (i = 0; i < cch; i = i + 24)
	{
		ccb = ((cch - i) < 24) ? cch - i : 24; // output lines of 24; last line may be shorter
		BaselineBtoAArray(rglbsTest1 + i, ccb);
	}
	hr = Baseline("\r\n");
/*Note: values expected are:
   4   0   3   4   0   3   4   0   2   3   4   4   1   4   4   4   0   1   2   2   3   4   4   4
   4   0   6   4   0   3   4   0   3   4   4   0   3   4   4   0   4   0   2   2   1   4   0   3
   4   0   1   3   4   4   1
*/

	// Test Line Break Info algorithm.
	int ichBreak;
	hr = m_qlcpe->GetLineBreakInfo(pszwTest1, cch, 2, 25, rglbsTest1, &ichBreak);
	return S_OK;

} // HRESULT LangTest2::Run()


void LangTest2::BaselineItoAArray(const int * prgnIn, int cn)
{
	HRESULT hr;
	char prgchOut[100];
	int i;
	int ichPos = 0;

	if (cn > 0 && cn <= 16)
	{
		for (i = 0; i< cn; ++i)
			ichPos += sprintf(prgchOut + ichPos, "%6d", *prgnIn++);
	}
	ichPos += sprintf(prgchOut + ichPos, "\r\n");
	hr = Baseline(prgchOut);
	return;
}

void LangTest2::BaselineBtoAArray(const byte * prgnIn, int cn)
{
	HRESULT hr;
	char prgchOut[100];
	int i;
	int iOut;
	int ichPos = 0;

	if (cn > 0 && cn <= 24)
	{
		for (i = 0; i< cn; ++i)
		{
			iOut = *prgnIn++;
			ichPos += sprintf(prgchOut + ichPos, "%4d", iOut);
		}
	}
	ichPos += sprintf(prgchOut + ichPos, "\r\n");
	hr = Baseline(prgchOut);
	return;
}

void LangTest2::CheckBoolHr(HRESULT hr, bool fRes, bool fExpected, wchar * pszTestName)
{
	if (FAILED(hr))
	{
		StrUni stuProblem = "Bad HRESULT on test ";
		stuProblem += pszTestName;
		Failure(stuProblem.Chars());
		StrAnsi staProblem = stuProblem;
		Warn(staProblem.Chars());
	}

	if (fRes != fExpected)
	{
		StrUni stuProblem = "Wrong result on test ";
		stuProblem += pszTestName;
		Failure(stuProblem.Chars());
		StrAnsi staProblem = stuProblem;
		Warn(staProblem.Chars());
	}
}

void LangTest2::CheckIntHr(HRESULT hr, int nRes, int nExpected, wchar * pszTestName)
{
	if (FAILED(hr))
	{
		StrUni stuProblem = "Bad HRESULT on test ";
		stuProblem += pszTestName;
		Failure(stuProblem.Chars());
		StrAnsi staProblem = stuProblem;
		Warn(staProblem.Chars());
	}

	if (nRes != nExpected)
	{
		StrUni stuProblem = "Wrong result on test ";
		stuProblem += pszTestName;
		Failure(stuProblem.Chars());
		StrAnsi staProblem = stuProblem;
		Warn(staProblem.Chars());
	}
}


void LangTest2::TestUpperCh(int ch, int chExpected, HRESULT hrExpected)
{
	HRESULT hr;
	int chGot;
	StrUni stuProblem;

	if (FAILED(hrExpected))
		HideWarnings(true);
	hr = m_qlcpe->get_ToUpperCh(ch, &chGot);
	if (FAILED(hrExpected))
		HideWarnings(false);

	if (hr != hrExpected)
	{
		stuProblem.Format(L"Converting %c (0x%x) to uppercase got HR 0x%x.", ch, ch, hr);
		Failure(stuProblem);
		Warn("Bad HR on upper case ch conversion");
	}

	if (SUCCEEDED(hr) && chGot != chExpected)
	{
		stuProblem.Format(L"Converting 0x%x to uppercase gave 0x%x instead of 0x%x.",
			ch, chGot, chExpected);
		Failure(stuProblem);
		Warn("Bad upper case ch conversion");
	}
}

void LangTest2::TestLowerCh(int ch, int chExpected, HRESULT hrExpected)
{
	HRESULT hr;
	int chGot;
	StrUni stuProblem;

	if (FAILED(hrExpected))
		HideWarnings(true);
	hr = m_qlcpe->get_ToLowerCh(ch, &chGot);
	if (FAILED(hrExpected))
		HideWarnings(false);

	if (hr != hrExpected)
	{
		stuProblem.Format(L"Converting %c (0x%x) to lowercase got HR 0x%x.", ch, ch, hr);
		Failure(stuProblem);
		Warn("Bad HR on lower case ch conversion");
	}

	if (SUCCEEDED(hr) && chGot != chExpected)
	{
		stuProblem.Format(L"Converting 0x%x to lowercase gave 0x%x instead of 0x%x.",
			ch, chGot, chExpected);
		Failure(stuProblem);
		Warn("Bad lower case ch conversion");
	}
}

void LangTest2::TestTitleCh(int ch, int chExpected, HRESULT hrExpected)
{
	HRESULT hr;
	int chGot;
	StrUni stuProblem;

	if (FAILED(hrExpected))
		HideWarnings(true);
	hr = m_qlcpe->get_ToTitleCh(ch, &chGot);
	if (FAILED(hrExpected))
		HideWarnings(false);

	if (hr != hrExpected)
	{
		stuProblem.Format(L"Converting %c (0x%x) to title case got HR 0x%x.", ch, ch, hr);
		Failure(stuProblem);
		Warn("Bad HR on title case ch conversion");
	}

	if (SUCCEEDED(hr) && chGot != chExpected)
	{
		stuProblem.Format(L"Converting 0x%x to title case gave 0x%x instead of 0x%x.",
			ch, chGot, chExpected);
		Failure(stuProblem);
		Warn("Bad title case ch conversion");
	}
}

void LangTest2::TestUpper(StrUni stuFrom, StrUni stuExpected, HRESULT hrExpected)
{
	HRESULT hr;
	BSTR bstrGot;
	StrUni stuProblem;

	if (FAILED(hrExpected))
		HideWarnings(true);
	hr = m_qlcpe->ToUpper(stuFrom.Bstr(), &bstrGot);
	if (FAILED(hrExpected))
		HideWarnings(false);

	if (hr != hrExpected)
	{
		stuProblem.Format(L"Conversion of %s to upper case got HR 0x%x.", stuFrom.Bstr(), hr);
		Warn("Bad HR on upper case conversion");
		Failure(stuProblem);
		return;
	}

	if (SUCCEEDED(hr) && bstrGot && wcscmp(bstrGot, stuExpected))
	{
		stuProblem.Format(L"upper case conversion from %s gave %s instead of %s",
			stuFrom.Bstr(), bstrGot, stuExpected.Bstr());
		Warn("incorrect upper case conversion");
		Failure(stuProblem);
	}

	if (bstrGot)
		SysFreeString(bstrGot);
}

void LangTest2::TestLower(StrUni stuFrom, StrUni stuExpected, HRESULT hrExpected)
{
	HRESULT hr;
	BSTR bstrGot;
	StrUni stuProblem;

	if (FAILED(hrExpected))
		HideWarnings(true);
	hr = m_qlcpe->ToLower(stuFrom.Bstr(), &bstrGot);
	if (FAILED(hrExpected))
		HideWarnings(true);

	if (hr != hrExpected)
	{
		stuProblem.Format(L"Conversion of %s to lower case got HR 0x%x.", stuFrom.Bstr(), hr);
		Warn("Bad HR on lower case conversion");
		Failure(stuProblem);
		return;
	}

	if (SUCCEEDED(hr) && bstrGot && wcscmp(bstrGot, stuExpected))
	{
		stuProblem.Format(L"lower case conversion from %s gave %s instead of %s",
			stuFrom.Bstr(), bstrGot, stuExpected.Bstr());
		Warn("incorrect lower case conversion");
		Failure(stuProblem);
	}

	if (bstrGot)
		SysFreeString(bstrGot);
}

void LangTest2::TestTitle(StrUni stuFrom, StrUni stuExpected, HRESULT hrExpected)
{
	HRESULT hr;
	BSTR bstrGot;
	StrUni stuProblem;

	if (FAILED(hrExpected))
		HideWarnings(true);
	hr = m_qlcpe->ToTitle(stuFrom.Bstr(), &bstrGot);
	if (FAILED(hrExpected))
		HideWarnings(true);

	if (hr != hrExpected)
	{
		stuProblem.Format(L"Conversion of %s to title case got HR 0x%x.", stuFrom.Bstr(), hr);
		Warn("Bad HR on title case conversion");
		Failure(stuProblem);
		return;
	}

	if (SUCCEEDED(hr) && bstrGot && wcscmp(bstrGot, stuExpected))
	{
		stuProblem.Format(L"title case conversion from %s gave %s instead of %s",
			stuFrom.Bstr(), bstrGot, stuExpected.Bstr());
		Warn("incorrect title case conversion");
		Failure(stuProblem);
	}

	if (bstrGot)
		SysFreeString(bstrGot);
}

void LangTest2::TestDecomp(int ch, OLECHAR * pszExpected)
{
	// Use the BSTR version; it uses the Rgch one internally, so we test both
	BSTR bstr;
	HRESULT hr;

	if (FAILED(hr = m_qlcpe->get_Decomposition(ch, &bstr)))
	{
		// Review: do we need to make a string that says exactly what failed?
		Failure("Bad HR on decomposition");
		Warn("Bad HR on decomposition");
	}

	// Returning null for empty string is OK, but wcscomp won't handle it.
	if (bstr == 0 && *pszExpected == 0)
		return;

	if (wcscmp(bstr, pszExpected))
	{
		// Review: do we need to make a string that says exactly what failed?
		Failure("Bad decomposition conversion");
		Warn("Bad decomposition conversion");
	}
	SysFreeString(bstr);
}

void LangTest2::TestFullDecomp(int ch, OLECHAR * pszExpected)
{
	// Use the BSTR version; it uses the Rgch one internally, so we test both
	BSTR bstr;
	HRESULT hr;

	if (FAILED(hr = m_qlcpe->get_FullDecomp(ch, &bstr)))
	{
		// Review: do we need to make a string that says exactly what failed?
		Failure("Bad HR on decomposition");
		Warn("Bad HR on decomposition");
	}

	// Returning null for empty string is OK, but wcscomp won't handle it.
	if (bstr == 0 && *pszExpected == 0)
		return;

	if (wcscmp(bstr, pszExpected))
	{
		// Review: do we need to make a string that says exactly what failed?
		Failure("Bad decomposition conversion");
		Warn("Bad decomposition conversion");
	}
	SysFreeString(bstr);
}

void LangTest2::CheckName(int ch, OLECHAR * pszExpected)
{
	BSTR bstr;
	HRESULT hr;

	if (FAILED(hr = m_qlcpe->get_CharacterName(ch, &bstr)))
	{
		// Review: do we need to make a string that says exactly what failed?
		Failure("Bad HR on char name");
		Warn("Bad HR on char name");
	}

	// Returning null for empty string is OK, but wcscomp won't handle it.
	if (bstr == 0 && *pszExpected == 0)
		return;

	if (wcscmp(bstr, pszExpected))
	{
		// Review: do we need to make a string that says exactly what failed?
		Failure("Wrong character name");
		Warn("Wrong character name");
	}
	SysFreeString(bstr);
}

/***********************************************************************************************
	LangTest3 Methods
***********************************************************************************************/

LangTest3::LangTest3()
:TestBase(L"Char property word tokenizer test", 3)
{
}

LangTest3::~LangTest3()
{
}


HRESULT LangTest3::Run()
{
/* IClassInitMonikerPtr, CLSID_ClassInitMoniker undeclared...
	HRESULT hr;
	// Make a character property word tokenizer. Initialize it the standard way with a moniker.
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);
	hr = qcim->InitNew(CLSID_LgCPWordTokenizer, NULL, 0);
	WarnHr(hr);

	IBindCtxPtr qbc;
	hr = ::CreateBindCtx(NULL, &qbc);
	WarnHr(hr);
	hr = qcim->BindToObject(qbc, NULL, IID_ILgTokenizer, (void **)&m_qtoker);
	WarnHr(hr);

	// Do tests with m_qtoker
	OLECHAR rgchData1[] = L"This is some data";
	OLECHAR rgchData2[] = L"abcdefghijklmnop";
	OLECHAR rgchData3[] = L"1.3 apple cores";
	StrUni stuTest1 = "This is some more data";
	StrUni stuTest2 = "";

	//test to get first token with GetToken
	TestGetToken(L"This is some data", 10, S_OK, 0, 4,
		"Getting 'This' from 'This is some data' failed.");

	//test of GetToken on empty string
	TestGetToken(L"", 0, E_FAIL, -1, -1,
		"Getting word from empty string");

	//test of GetToken on end of string
	TestGetToken(rgchData1 + 12, 5, S_OK, 1, 5,
		"Getting word from end of string");

	//test of GetToken on middle of string
	TestGetToken(rgchData1 + 4, 3, S_OK, 1, 3,
		"Getting token from middle of string");

	//test of GetToken on string as long as cch
	TestGetToken(rgchData2, 4, S_OK, 0, 4,
		"Getting token that is as long as cch");

	// test of GetToken on one character string with reported negative length
	TestGetToken(L"a", -1, E_INVALIDARG, -1, -1,
		"Getting word from one-char string with reported negative length");

	// // This is not a good test, because there's probably no way the
	// // routine can detect that the length is incorrect.  Currently it
	// // succeeds, but is there any guarantee that it should?
	// TestGetToken(L"a", 10000, S_OK, 0, 1,
	// 	"Getting word from one-char string with reported length 10000");

	// // same test, but with only slightly overreported length
	// TestGetToken(L"a", 2, S_OK, 0, 1,
	//		"Getting word from one-char string with reported length 2");

	// test of GetToken with non-alpha characters
	TestGetToken(rgchData3, 3, E_FAIL, -1, -1,
		"Getting word from non-alpha string");

	// test of GetToken with non-alpha characters
	TestGetToken(rgchData3, 4, E_FAIL, -1, -1,
		"Getting word from non-alpha string");

	// test of GetToken with non-alpha characters
	TestGetToken(rgchData3, 5, S_OK, 4, 5,
		"Getting word from non-alpha-initial string");

	// test of GetToken with non-alpha characters
	TestGetToken(rgchData3, 7, S_OK, 4, 7,
		"Getting word from non-alpha-initial string");

	// test of GetToken with non-alpha characters
	TestGetToken(rgchData3, 9, S_OK, 4, 9,
		"Getting word from non-alpha-initial string");

	// test of GetToken with non-alpha characters
	TestGetToken(rgchData3, 10, S_OK, 4, 9,
		"Getting word from non-alpha-initial string");

	// test of GetToken with non-alpha characters
	TestGetToken(rgchData3 + 3, 10, S_OK, 1, 6,
		"Getting word from non-alpha-initial string");

	// test of GetToken with no alpha characters
	TestGetToken(L"&#$", 3, E_FAIL, -1, -1,
		"Getting token from string with no alpha characters");

	// test of GetToken with NULL pointer
	TestGetToken(NULL, 3, E_POINTER, -1, -1,
		"Passing NULL pointer to GetToken");

	// test of GetToken with NULL ptr for return values
	int ichRet;

	hr = m_qtoker->GetToken(rgchData1, 3, NULL, &ichRet);
	if (!FAILED(hr))
	{
		StrUni stuProblem = L"GetToken with NULL pointer for pichMin succeeded";
		Failure(stuProblem.Bstr());
	}

	hr = m_qtoker->GetToken(rgchData1, 3, &ichRet, NULL);
	if (!FAILED(hr))
	{
		StrUni stuProblem = L"GetToken with NULL pointer for pichLim succeeded";
		Failure(stuProblem.Bstr());
	}

	//test of get_TokenStart and get_TokenEnd on first token of string
	TestGetTokenStartEnd(stuTest1.Bstr(), 0, S_OK, 0, 4,
		"Getting first word starting from position 0");

	//test of get_TokenStart and get_TokenEnd on second token of string
	TestGetTokenStartEnd(stuTest1.Bstr(), 4, S_OK, 1, 3,
		"Getting second word starting from position 4");

	//test of get_TokenStart and get_TokenEnd on empty string
	TestGetTokenStartEnd(stuTest2.Bstr(), 0, E_FAIL, -1, -1,
		"Getting first token of empty string");

	// test of get_TokenStart and get_TokenEnd on null ptr
	// *** Causes a non-standard exception, which breaks the test.
	TestGetTokenStartEnd(NULL, 5, E_POINTER, -1, -1,
		"Passing NULL pointer to get_TokenStart/End");

	// test of get_TokenStart/End with NULL ptr for return argument
	hr = m_qtoker->get_TokenStart(stuTest1.Bstr(), 0, NULL);
	if (!FAILED(hr))
	{
		StrUni stuProblem = "get_TokenStart with NULL pointer succeeded";
		Failure(stuProblem.Bstr());
	}

	hr = m_qtoker->get_TokenEnd(stuTest1.Bstr(), 0, NULL);
	if (!FAILED(hr))
	{
		StrUni stuProblem = "get_TokenEnd with NULL pointer succeeded";
		Failure(stuProblem.Bstr());
	}

	// test of get_TokenStart/End on end of string
	TestGetTokenStartEnd(stuTest1.Bstr(), 17, S_OK, 1, 5, // S_OK
		"Getting last word in string");

	// test of get_TokenEnd with underreported length string
	// What is the expected return value?
	// Assume it should act as if the string is only as long as reported.
	hr = m_qtoker->get_TokenEnd(stuTest1.Bstr(), 2, &ichRet);
	if (FAILED(hr))
	{
		Failure("get_TokenEnd with underreported length string failed");
	}
	else if (ichRet != 2)
	{
		StrUni stuProblem;
		stuProblem.Format(L"get_TokenEnd with underreported length string: expected %d, got %d", 2, ichRet);
		Failure(stuProblem.Bstr());
	}
*/
	return S_OK;
}


// Automate tests of GetToken.
// If unexpected result, log detailed failure message.

void LangTest3::TestGetToken(OLECHAR *prgchInput, int cch, HRESULT hrExp,
	int ichMinExp, int ichLimExp, StrUni stuComment)
{
	HRESULT hr = E_FAIL;
	int ichMin = -1, ichLim = -1;
	BOOL bSuccess = FALSE;
	StrUni stuUnexp, stuLogMsg;

	hr = m_qtoker->GetToken(prgchInput, cch, &ichMin, &ichLim);

	if (hr != hrExp)
	{
		stuUnexp.Format(L"hr %x, got %x.", hrExp, hr);
	}
	else if (ichMin != ichMinExp)
	{
		stuUnexp.Format(L"min %d, got %d.", ichMinExp, ichMin);
	}
	else if (ichLim != ichLimExp)
	{
		stuUnexp.Format(L"lim %d, got %d.", ichLimExp, ichLim);
	}
	else
		bSuccess = TRUE;

	if (!bSuccess)
	{
		/* '=' : cannot convert from 'void' to 'long'
		hr = stuLogMsg.Format(L"GetToken: %s; expected %s",
				stuComment.Bstr(), stuUnexp.Bstr());
		*/
		WarnHr(hr);
		Failure(stuLogMsg.Bstr());
	}

	return;
}


void LangTest3::TestGetTokenStartEnd(BSTR bstrInput, int ichFirst, HRESULT hrExp,
	int ichMinExp, int ichLimExp, StrUni stuComment)
{
	HRESULT hr = E_FAIL;
	int ichMin = -1, ichLim = -1;
	BOOL bSuccess = FALSE;
	StrUni stuUnexp, stuLogMsg;

	hr = m_qtoker->get_TokenStart(bstrInput, ichFirst, &ichMin);

	if (hr != hrExp)
	{
		stuUnexp.Format(L"hr %x, got %x.", hrExp, hr);
	}
	else if (ichMin != ichMinExp)
	{
		stuUnexp.Format(L"min %d, got %d.", ichMinExp, ichMin);
	}
	else
		bSuccess = TRUE;

	if (!bSuccess)
	{
		/* '=' : cannot convert from 'void' to 'long'
		hr = stuLogMsg.Format(L"get_TokenStart: %s; expected %s",
				stuComment.Bstr(), stuUnexp.Bstr());
		*/
		WarnHr(hr);
		Failure(stuLogMsg.Bstr());
	}


	bSuccess = FALSE;

	hr = m_qtoker->get_TokenEnd(bstrInput, ichFirst, &ichLim);

	if (hr != hrExp)
	{
		stuUnexp.Format(L"hr %x, got %x.", hrExp, hr);
	}
	else if (ichLim != ichLimExp)
	{
		stuUnexp.Format(L"lim %d, got %d.", ichLimExp, ichLim);
	}
	else
		bSuccess = TRUE;

	if (!bSuccess)
	{
		/* '=' : cannot convert from 'void' to 'long'
		hr = stuLogMsg.Format(L"get_TokenEnd: %s; expected %s",
				stuComment.Bstr(), stuUnexp.Bstr());
		*/
		WarnHr(hr);
		Failure(stuLogMsg.Bstr());
	}

	return;
}

/***********************************************************************************************
	LangTest4 Methods
***********************************************************************************************/

LangTest4::LangTest4()
:TestBase(L"NumericEngine conversion test", 4)
{
}

LangTest4::~LangTest4()
{
}

HRESULT LangTest4::Run()
{
/* IClassInitMonikerPtr, CLSID_ClassInitMoniker undeclared...
	HRESULT hr;
	// Make a default numeric engine object. Initialize it the standard way with a moniker.
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);
	hr = qcim->InitNew(CLSID_LgNumericEngine, (const BYTE *)L"-.,E", 4 * isizeof(wchar));
	WarnHr(hr);

	IBindCtxPtr qbc;
	hr = ::CreateBindCtx(NULL, &qbc);
	WarnHr(hr);
	hr = qcim->BindToObject(qbc, NULL, IID_ILgNumericEngine, (void **)&m_qnumeng);
	WarnHr(hr);

	//do tests with m_qnumeng
	int nTest1 = 123456;
	int nTest2 = -654321;
	StrUni stuTest1 = "123456";
	StrUni stuTest2 = "12d34";
	StrUni stuTest3 = "133   ";
	StrUni stuTest4 = "2147483648";
	StrUni stuTest5 = "123.456";
	StrUni stuTest6 = "12.3d4";
	StrUni stuTest7 = "  12.34  ";
	StrUni stuTest8 = "12.34abcd ";
	int n;
	int ichUnused;
	double dbl;
	BSTR bstr;
	//testing get_IntToString
	//test with a positive integer
	hr = m_qnumeng->get_IntToString(nTest1, &bstr);
	if(wcscmp(bstr, L"123456") != 0)
	{
		Failure("Converting positive integer to string failed");
	}

	//test with a negative integer
	hr = m_qnumeng->get_IntToString(nTest2, &bstr);
	if(wcscmp(bstr,L"-654321") != 0)
	{
		Failure("Converting negative integer to string failed");
	}

	//test with zero
	hr = m_qnumeng->get_IntToString(0, &bstr);
	if(wcscmp(bstr,L"0") != 0)
	{
		Failure("Converting 0 to string failed");
	}

	//tests with non-zero, positive one digit number
	hr = m_qnumeng->get_IntToString(1, &bstr);
	if(wcscmp(bstr,L"1") != 0)
	{
		Failure("Converting one digit number to string failed");
	}

	//testing get_IntToPrettyString
	//testing with positive integer that has a multiple of 3 digits
	hr = m_qnumeng->get_IntToPrettyString(nTest1, &bstr);
	if(wcscmp(bstr,L"123,456") != 0)
	{
		Failure("Converting positive integer to pretty string failed");
	}

	//testing with negative integer that has a multiple of 3 digits
	hr = m_qnumeng->get_IntToPrettyString(nTest2, &bstr);
	if(wcscmp(bstr,L"-654,321") != 0)
	{
		Failure("Converting integer w/ mult of 3 digits to pretty string failed");
	}

	//testing with a positive integer that does not have a multiple of 3 digits
	hr = m_qnumeng->get_IntToPrettyString(1234, &bstr);
	if(wcscmp(bstr,L"1,234") != 0)
	{
		Failure("Converting a positive integer without a multiple of 3 digits to pretty string failed");
	}

	//testing with a negative integer that does not have a multiple of 3 digits
	hr = m_qnumeng->get_IntToPrettyString(-6543, &bstr);
	if(wcscmp(bstr,L"-6,543") != 0)
	{
		Failure("Converting a negative integer without a multiple of 3 digits to pretty string failed");
	}

	//testing StringToIntRgch
	//testing with a string representation of a positive integer
	hr = m_qnumeng->StringToIntRgch(L"123456", 7, &n, &ichUnused);
	if(n != 123456 || ichUnused != 6)
	{
		Failure("Converting a positive string to an integer failed");
	}

	//testing with a string representation of a negative integer
	hr = m_qnumeng->StringToIntRgch(L"-654321", 8, &n, &ichUnused);
	if(n != -654321 || ichUnused != 7)
	{
		Failure("Converting a negative string to an integer failed");
	}

	//testing a string with whitespace in the beginning
	hr = m_qnumeng->StringToIntRgch(L"  123456", 8, &n, &ichUnused);
	if(n != 123456 || ichUnused != 8)
	{
		Failure("Converting a positive string to an integer with leading whitespace failed");
	}

	//testing a string with whitespace at the end
	hr = m_qnumeng->StringToIntRgch(L"123456  ", 8, &n, &ichUnused);
	if(n != 123456 || ichUnused != 6)
	{
		Failure("Converting a positive string with trailing whitespaceto an integer failed");
	}

	//testing a positive string that will not fit in a 32 bit signed integer
	hr = m_qnumeng->StringToIntRgch(L"2147483648", 13, &n, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a positive string too large to be converted to an integer succeeded");
	}

	//testing the largest positive string that can fit in 32 bit signed integer
	hr = m_qnumeng->StringToIntRgch(L"2147483647", 13, &n, &ichUnused);
	if(FAILED(hr) || n != 2147483647)
	{
		Failure("Converting the largest signed integer to an integer failed");
	}

	//testing a negative string that will not fit in a 32 bit signed integer
	hr = m_qnumeng->StringToIntRgch(L"-2147483649", 13, &n, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a negative string too small to be converted to an integer succeeded");
	}

	//testing the most negative string that can fit in 32 bit signed integer
	hr = m_qnumeng->StringToIntRgch(L"-2147483648", 13, &n, &ichUnused);
	if(FAILED(hr) || n != INT_MIN)
	{
		Failure("Converting the most negative signed integer to an integer failed");
	}

	//testing a string with a space in the middle
	hr = m_qnumeng->StringToIntRgch(L"12 78", 5, &n, &ichUnused);
	if(ichUnused != 2 || n != 12)
	{
		Failure("Converting string with space character in middle failed");
	}

	//testing an empty string
	hr = m_qnumeng->StringToIntRgch(L"", 0, &n, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting an empty string succeeded");
	}

	//testing an all white string
	hr = m_qnumeng->StringToIntRgch(L"     ", 5, &n, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting an all whitespace string succeeded");
	}

	//testing a string with just a -
	hr = m_qnumeng->StringToIntRgch(L"- ", 2, &n, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting string with just a minus succeeded");
	}

	//testing an invalid string with 1 character
	hr = m_qnumeng->StringToIntRgch(L"A", 1, &n, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting string with just a character succeeded");
	}

	//testing get_StringToInt
	//testing with a string representation of a positive integer
	hr = m_qnumeng->get_StringToInt(stuTest1.Bstr(), &n);
	if(FAILED(hr) || n != 123456)
	{
		Failure("Converting a positive string to integer failed");
	}

	//testing with a string that contains a non-numeric character
	hr = m_qnumeng->get_StringToInt(stuTest2.Bstr(), &n);
	if(!FAILED(hr) || n != 12)
	{
		Failure("Converting a string with a non-numeric character was successful");
	}

	//testing a string with trailing whitespace
	hr = m_qnumeng->get_StringToInt(stuTest3.Bstr(), &n);
	if(FAILED(hr) || n != 133)
	{
		Failure("Converting a string with trailing whitespace to an integer failed");
	}
	//testing a string that can not be represented as a 32 bit signed integer
	hr = m_qnumeng->get_StringToInt(stuTest4.Bstr(), &n);
	if(!FAILED(hr))
	{
		Failure("Converting a string that would not fit in a 32 bit signed integer was successful");
	}

	//testing get_DblToString
	//testing converting zero to string
	hr = m_qnumeng->get_DblToString(0,1,&bstr);
	if(wcscmp(bstr, L"0.0") != 0)
	{
		Failure("Converting zero to a string failed.");
	}

	//testing a positive double to string
	hr = m_qnumeng->get_DblToString(12.34, 2, &bstr);
	if(wcscmp(bstr,L"12.34") != 0)
	{
		Failure("Converting a positive double to a string failed");
	}

	//testing a negative double to string
	hr = m_qnumeng->get_DblToString(-12.34, 2, &bstr);
	if(wcscmp(bstr,L"-12.34") != 0)
	{
		Failure("Converting a negative double to a string failed");
	}

	//testing a negative double with 0 before decimal
	hr = m_qnumeng->get_DblToString(-.34, 2, &bstr);
	if(wcscmp(bstr,L"-0.34") != 0)
	{
		Failure("Converting a negative double to a string failed");
	}

	//testing a double with zero before the decimal to a string
	hr = m_qnumeng->get_DblToString(.34, 2, &bstr);
	if(wcscmp(bstr,L"0.34") != 0)
	{
		Failure("Converting a double with no digits before decimal to a string failed");
	}

	//testing a double with no decimal digits
	hr = m_qnumeng->get_DblToString(12, 2, &bstr);
	if(wcscmp(bstr,L"12.00") != 0)
	{
		Failure("Converting a double with no decimal digits to a string failed");
	}

	//testing a double that will be padded with zeroes
	hr = m_qnumeng->get_DblToString(12.3, 3, &bstr);
	if(wcscmp(bstr,L"12.300") != 0)
	{
		Failure("Converting a double string padded with zeroes failed");
	}

	//testing a double that is greater than 10^15
	hr = m_qnumeng->get_DblToString(1234567812345678, 2, &bstr);
	if(wcscmp(bstr,L"1.23E15") != 0)
	{
		Failure("Converting a double that is greater than 10^15 to a string failed");
	}

	//testing get_DblToPrettyString
	//testing a positive double with multiple of 3 non decimal digits
	hr = m_qnumeng->get_DblToPrettyString(123456789.78, 2,  &bstr);
	if(wcscmp(bstr,L"123,456,789.78") != 0)
	{
		Failure("Converting double with multiple of three non-decimal digits to pretty string failed");
	}

	//testing a positive double that does not have a multiple of 3 non-decimal digits to string
	hr = m_qnumeng->get_DblToPrettyString(12345.12, 2, &bstr);
	if(wcscmp(bstr,L"12,345.12") != 0)
	{
		Failure("Converting double without a multiple of 3 non-decimal digits to pretty string failed");
	}
	//testing with a negative double that does have a multiple of 3 digits
	hr = m_qnumeng->get_DblToPrettyString(-123456789.67, 2, &bstr);
	if(wcscmp(bstr,L"-123,456,789.67") != 0)
	{
		Failure("Converting negative double to pretty string failed");
	}

	//testing get_DblToExpString
	//testing with a positive double
	hr = m_qnumeng->get_DblToExpString(12345.67, 3, &bstr);
	if(wcscmp(bstr,L"1.235E4") != 0)
	{
		Failure("Converting positive double to exponential string failed");
	}

	//testing with a positive double that has value less than 1
	hr = m_qnumeng->get_DblToExpString(.12345, 3, &bstr);
	if(wcscmp(bstr,L"1.235E-1") != 0)
	{
		Failure("Converting positive double less than 1 to exponential string failed");
	}

	//testing with a positive double with zeroes directly after the decimal
	hr = m_qnumeng->get_DblToExpString(.00012346, 3, &bstr);
	if(wcscmp(bstr,L"1.235E-4") != 0)
	{
		Failure("Converting double to negative exponential string failed");
	}

	//testing a negative double with more digits after the decimal asked for than exist
	hr = m_qnumeng->get_DblToExpString(-123.45, 5, &bstr);
	if(wcscmp(bstr,L"-1.23450E2") != 0)
	{
		Failure("Converting negative double to exponential string failed");
	}

	//testing zero
	hr = m_qnumeng->get_DblToExpString(0, 2, &bstr);
	if(wcscmp(bstr,L"0.00E0") != 0)
	{
		Failure("Converting 0 to exponential string failed");
	}

	//testing a double with maximum exponent
	hr = m_qnumeng->get_DblToExpString(1.234 * pow(10,308),3,&bstr);
	if(wcscmp(bstr,L"1.234E308") != 0)
	{
		Failure("Converting a double with the maximum exponent to a string failed");
	}

	//testing StringToDblRgch
	//testing a positive string conversion to a double
	hr = m_qnumeng->StringToDblRgch(L"12.3456", 7, &dbl, &ichUnused);
	if((fabs(dbl - 12.3456)/1.23456 >= pow(1,-14) ) || ichUnused != 7)
	{
		Failure("Converting a positive string to an double failed");
	}

	//testing a negative string conversion to a double
	hr = m_qnumeng->StringToDblRgch(L"-12.3456", 8, &dbl, &ichUnused);
	if((fabs(dbl - (-12.3456))/(-12.3456) >= pow(1,-14) ) || ichUnused != 8)
	{
		Failure("Converting a negative string to an double failed");
	}

	//testing an empty string conversion to a double
	hr = m_qnumeng->StringToDblRgch(L"", 0, &dbl, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting an empty string to a double succeeded");
	}

	//testing a string of whitespace to a double
	hr = m_qnumeng->StringToDblRgch(L"   ", 3, &dbl, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a whitespace string to a double succeeded");
	}

	//testing a string with no digits before the decimal point
	hr = m_qnumeng->StringToDblRgch(L".3456", 7, &dbl, &ichUnused);
	if((fabs(dbl - .3456)/.3456 >= pow(1,-14) ) || ichUnused != 5)
	{
		Failure("Converting a positive string with no non decimal digits to a double failed");
	}

	//testing a string with no decimal point
	hr = m_qnumeng->StringToDblRgch(L"1234", 5, &dbl, &ichUnused);
	if((fabs(dbl - 1234)/1234 >= pow(1,-14) ) || ichUnused != 4)
	{
		Failure("Converting a positive string to an double failed");
	}

	//testing the string zero converted to a double
	hr = m_qnumeng->StringToDblRgch(L"0", 1, &dbl, &ichUnused);
	if((fabs(dbl - 0.0) >= pow(1,-14) ) || ichUnused != 1)
	{
		Failure("Converting a positive string to an double failed");
	}

	//testing a positive string in scientific notation to a double
	hr = m_qnumeng->StringToDblRgch(L"1.2345E3", 9, &dbl, &ichUnused);
	if((fabs(dbl - 1234.5)/1234.5 >= pow(1,-14) ) || ichUnused != 8)
	{
		Failure("Converting a positive string in exponential notation to an double failed");
	}

	//testing a positive string in scientific notation with a negative exponent to a double
	hr = m_qnumeng->StringToDblRgch(L"1.2345E-2", 9, &dbl, &ichUnused);
	if((fabs(dbl - .012345)/.012345 >= pow(1,-14) ) || ichUnused != 9)
	{
		Failure("Converting a positive string in exponential notation to an double failed");
	}

	//testing a negative string in scientific notation with a negative exponent to a double
	hr = m_qnumeng->StringToDblRgch(L"-1.2345E-2", 10, &dbl, &ichUnused);
	if((fabs(dbl - (-.012345))/-.012345 >= pow(1,-14) ) || ichUnused != 10)
	{
		Failure("Converting a negative string with a negative exponent to an double failed");
	}

	//testing a negative string with leading and trailing whitespace
	hr = m_qnumeng->StringToDblRgch(L"  -123.456  ", 12, &dbl, &ichUnused);
	if((fabs(dbl - (-123.456))/-123.456 >= pow(1,-14) ) || ichUnused != 10)
	{
		Failure("Converting a positive string with leading whitespace to a double failed");
	}

	//testing a string with whitespace in the middle
	hr = m_qnumeng->StringToDblRgch(L" -123.4 56  ", 10, &dbl, &ichUnused);
	if((fabs(dbl - (-123.4))/123.4 >= pow(1,-14)) || ichUnused != 7)
	{
		Failure("Converting a positive string with space in the middle to a double failed");
	}

	//testing a string with a misplaced exponential symbol
	hr = m_qnumeng->StringToDblRgch(L" -E123.4a56  ", 10, &dbl, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a string with a misplaced exponent symbol to a double succeeded");
	}

	//testing just a decimal
	hr = m_qnumeng->StringToDblRgch(L".", 1, &dbl, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a decimal point to a double succeeded");
	}

	//testing a decimal point with characters following
	hr = m_qnumeng->StringToDblRgch(L".ABC", 4, &dbl, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a string with characters directly after decimal point succeeded");
	}

	//testing a string with E that is not followed by an exponent
	hr = m_qnumeng->StringToDblRgch(L"1.0E", 4, &dbl, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a string with E not followed by an exponent succeeded");
	}

	//testing a string with E followed by a character
	hr = m_qnumeng->StringToDblRgch(L"1.0EX", 5, &dbl, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a string with E followed by characters to a double succeeded");
	}

	//testing a string with E- followed by characters rather than an exponent
	hr = m_qnumeng->StringToDblRgch(L"1.0E-X", 6, &dbl, &ichUnused);
	if(!FAILED(hr))
	{
		Failure("Converting a scientific notation string without an exponent to a double succeeded");
	}

	//testing the string -0
	hr = m_qnumeng->StringToDblRgch(L"-0", 2, &dbl, &ichUnused);
	if(FAILED(hr) || fabs(dbl - 0) >= pow(1,-14) || ichUnused != 2)
	{
		Failure("Converting the string '-0' to a double failed");
	}

	//testing the string .0
	hr = m_qnumeng->StringToDblRgch(L".0", 2, &dbl, &ichUnused);
	if(FAILED(hr) || fabs(dbl - 0) >= pow(1,-14) || ichUnused != 2)
	{
		Failure("Converting the string '.0' to a double failed");
	}

	//testing the string 0.
	hr = m_qnumeng->StringToDblRgch(L"0.", 2, &dbl, &ichUnused);
	if(FAILED(hr) || fabs(dbl - 0) >= pow(1,-14) || ichUnused != 2)
	{
		Failure("Converting the string '0.' to a double failed");
	}

	//testing the string 10.A
	hr = m_qnumeng->StringToDblRgch(L"10.A", 4, &dbl, &ichUnused);
	if(FAILED(hr) || fabs(dbl - 10)/10 >= pow(1,-14) || ichUnused != 3)
	{
		Failure("Converting the string '10.A' to a double failed");
	}

	//testing the string "10. "
	hr = m_qnumeng->StringToDblRgch(L"10. ", 4, &dbl, &ichUnused);
	if(FAILED(hr) || fabs(dbl - 10)/10 >= pow(1,-14) || ichUnused != 3)
	{
		Failure("Converting the string '10.A' to a double failed");
	}

	//testing a string with a misplaced negative in exponent
	hr = m_qnumeng->StringToDblRgch(L"1.23E-2-3 ", 10, &dbl, &ichUnused);
	if((fabs(dbl - .0123)/.0123 >= pow(1,-14)) || ichUnused != 7)
	{
		Failure("Converting a string with an extra minus in exponent to double failed");
	}

	//testing with a misplaced negative in non-exponential string
	hr = m_qnumeng->StringToDblRgch(L"-123.4-56  ", 10, &dbl, &ichUnused);
	if((fabs(dbl - (-123.4))/-123.4 >= pow(1,-14)) || ichUnused != 6)
	{
		Failure("Converting a positive string with misplaced negative to a double failed");
	}

	//testing with a string of two scientific notation doubles
	hr = m_qnumeng->StringToDblRgch(L"-1.23 2.34E2  ", 14, &dbl, &ichUnused);
	if((fabs(dbl - (-1.23))/-1.23 >= pow(1,-14)) || ichUnused != 5)
	{
		Failure("Converting a positive string with non numerical characters to a double failed");
	}

	//testing get_StringToDbl
	//testing with a string representation of a positive double
	hr = m_qnumeng->get_StringToDbl(stuTest5.Bstr(), &dbl);
	if(FAILED(hr) || fabs(dbl - 123.456)/123.456 >= pow(1,-14))
	{
		Failure("Converting a positive string to double failed");
	}

	//testing with a string that contains a non-numeric character
	hr = m_qnumeng->get_StringToDbl(stuTest6.Bstr(), &dbl);
	if(!FAILED(hr))
	{
		Failure("Converting a string with a non-numeric character was successful");
	}

	//testing a string with leading and trailing whitespace
	hr = m_qnumeng->get_StringToDbl(stuTest7.Bstr(), &dbl);
	if(FAILED(hr) || fabs(dbl - 12.34)/12.34 >= pow(1,-14))
	{
		Failure("Converting a string with leading and trailing whitespace to a double failed");
	}
*/
	return S_OK;
} // HRESULT LangTest4::Run()


/***********************************************************************************************
	LangTest5 Methods
***********************************************************************************************/

LangTest5::LangTest5()
:TestBase(L"Unicode collater tests", 5)
{
}

LangTest5::~LangTest5()
{
}

HRESULT LangTest5::Run()
{
/* IClassInitMonikerPtr, CLSID_ClassInitMoniker undeclared...
	HRESULT hr;
	// Make a unicode collating engine. Initialize it the standard way with a moniker.
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);
	hr = qcim->InitNew(CLSID_LgUnicodeCollater, NULL, 0);
	WarnHr(hr);

	IBindCtxPtr qbc;
	hr = ::CreateBindCtx(NULL, &qbc);
	WarnHr(hr);
	hr = qcim->BindToObject(qbc, NULL, IID_ILgCollatingEngine, (void **)&m_qcoleng);
	WarnHr(hr);

	StrUni stuValBlank = L"";
	StrUni stuValA = L"a";
	StrUni stuValB = L"b";
	StrUni stuValAUpper = L"A";
	StrUni stuValAnd = L"and";
	StrUni stuValAny = L"any";
	StrUni stuValAndUpper = L"And";
	StrUni stuValMult = L"\xDF";
	StrUni stuValMult3 = L"\x0f00";
	StrUni stuValMult4 = L"\x02EB\x007c\x00DF";
	StrUni stuValDecomp = L"\x0043\x1FA4\x0043";
	StrUni stuValDecomp2 = L"\x00AF\x1FA4";
	StrUni stuValPredictable = L"\x03F5\x0023";
	StrUni stuValTest1 = L"\x3083\x3084\x30e3";
	StrUni stuValTest2 = L"\x3083\x3084\x3083\x3084";
	// Try a sequence of test comparisions.
	TestCompare(stuValBlank, stuValBlank, 0);
	TestCompare(stuValBlank, stuValA, -1);
	TestCompare(stuValA, stuValBlank, 1);
	TestCompare(stuValA, stuValB, -1);
	TestCompare(stuValB, stuValA, 1);
	TestCompare(stuValA, stuValAUpper, -1);
	TestCompare(stuValB, stuValAUpper, 1);
	TestCompare(stuValA, stuValAnd, -1);
	TestCompare(stuValAny, stuValAnd, 1);
	TestCompare(stuValAny, stuValAndUpper, 1);
	TestCompare(stuValAnd, stuValAnd, 0);
	TestCompare(stuValTest1, stuValTest2, -1);

	BSTR bstrKeyAnd;
	BSTR bstrKeyAndUpper;
	BSTR bstrKeyAny;
	BSTR bstrKeyAnd1;
	BSTR bstrKeyAndUpper1;

	m_qcoleng->get_SortKey(stuValAnd.Bstr(), fcoIgnoreCase, &bstrKeyAnd);
	m_qcoleng->get_SortKey(stuValAndUpper.Bstr(), fcoIgnoreCase, &bstrKeyAndUpper);
	m_qcoleng->get_SortKey(stuValAny.Bstr(), fcoIgnoreCase, &bstrKeyAny);
	m_qcoleng->get_SortKey(stuValAnd.Bstr(), fcoDefault, &bstrKeyAnd1);
	m_qcoleng->get_SortKey(stuValAndUpper.Bstr(), fcoDefault, &bstrKeyAndUpper1);

	if(wcscmp(bstrKeyAnd, bstrKeyAndUpper))
	{
		Failure("String compare failed");
		Warn("String compare failed");
	}
	if (wcscmp(bstrKeyAnd, bstrKeyAny) >= 0)
	{
		Failure("String compare failed");
		Warn("String compare failed");
	}
	if(wcscmp(bstrKeyAnd1, bstrKeyAndUpper1) >= 0)
	{
		Failure("String compare failed");
		Warn("String compare failed");
	}

	BSTR bstr;
	OLECHAR szResM1 [] = {0x85d, 0x85d, 0x1, 0xd520, 0x1, 0x0207, 0x01, 0xdf, 0};
	m_qcoleng->get_SortKey(stuValMult.Bstr(), fcoDontIgnoreVariant, &bstr);
	if(wcscmp(bstr, szResM1))
	{
		Failure("Getting sort key for a multiple failed");
		Warn("Getting sort key failed");
	}

	OLECHAR szResM2 [] = {0x0ea1, 0x0eb3, 0x0e5f, 0x0001, 0x2020, 0x2000, 0x0001, 0x0303, 0x0300,
						 0x0001, 0x0F00, 0};
	m_qcoleng->get_SortKey(stuValMult3.Bstr(), fcoDontIgnoreVariant, &bstr);
	if(wcscmp(bstr, szResM2))
	{
		Failure("Getting sort key for a multiple with an odd number of weight 2's and 3's failed");
		Warn("Getting sort key failed");
	}

	//testing a string with multiple characters
	OLECHAR szResM3 [] = {0x02eb, 0x0371, 0x085d, 0x085d, 0x0001, 0x2020, 0xd520, 0x0001, 0x0202, 0x0207, 0x0001, 0x02eb,
							0x007c, 0x00df, 0};
	m_qcoleng->get_SortKey(stuValMult4.Bstr(), fcoDontIgnoreVariant, &bstr);
	if(wcscmp(bstr, szResM3))
	{
		Failure("Getting sort key for a multiple with multiple characters failed");
		Warn("Getting sort key failed");
	}

	//testing a string with decomposable characters
	OLECHAR szResM4 [] = {0x06F7, 0x0951, 0x06F7, 0x0001, 0x2020, 0x2123, 0x6220, 0x0001,
						0x0E02, 0x0202, 0x020E, 0x0001, 0x0043, 0x1FA4, 0x0043, 0};
	m_qcoleng->get_SortKey(stuValDecomp.Bstr(), fcoDontIgnoreVariant, &bstr);
	if(wcscmp(bstr, szResM4))
	{
		Failure("Getting sort key for a string with a decomposable character failed");
		Warn("Getting sort key failed");
	}

	//testing a string with two decomposable chars
	OLECHAR szResM5 [] = {0x209, 0x951, 0x01, 0x2032, 0x2021, 0x2362, 0x01, 0x0202, 0x0202, 0x0202, 0x01, 0x00AF,
							0x1FA4,0};
	m_qcoleng->get_SortKey(stuValDecomp2.Bstr(), fcoDontIgnoreVariant, &bstr);
	if(wcscmp(bstr, szResM5))
	{
		Failure("Getting sort key for a string with two decomposable characters failed");
		Warn("Getting sort key failed");
	}

	//testing a string with characters that have predictable weights
	OLECHAR szResM6 [] = {0x03F5, 0x01, 0x2000, 0x01, 0x0200, 0x01, 0x03F5, 0x0023, 0};
	m_qcoleng->get_SortKey(stuValPredictable.Bstr(), fcoDefault, &bstr);
	if(wcscmp(bstr, szResM6))
	{
		Failure("Getting sort key for a predictable and non-predictable character failed");
		Warn("Getting sort key failed");
	}

	OLECHAR szResM7 [] = {0x1, 0x1, 0x01, 0xdf, 0};
	m_qcoleng->get_SortKey(stuValMult.Bstr(), fcoDefault, &bstr);
	if(wcscmp(bstr, szResM7))
	{
		Failure("Getting sort key for a multiple with variant set to true failed");
		Warn("Getting sort key failed");
	}

	//testing a string with two decomposable chars
	OLECHAR szResM8 [] = {0x951, 0x01,0x3220, 0x2123, 0x6200, 0x01, 0x0202, 0x0202, 0x0200, 0x01, 0xaf, 0x1fa4, 0};
	m_qcoleng->get_SortKey(stuValDecomp2.Bstr(), fcoDefault, &bstr);
	if(wcscmp(bstr, szResM8))
	{
		Failure("Getting sort key for two decomposable chars, one ignorable, failed.");
		Warn("Getting sort key failed");
	}

	m_qcoleng.Clear();
*/
	return S_OK;
}

void LangTest5::TestCompare(StrUni stuVal1, StrUni stuVal2, int nExpected)
{
	HRESULT hr;

	int nActual;
	hr = m_qcoleng->Compare(stuVal1.Bstr(), stuVal2.Bstr(), fcoDefault , &nActual);
	if (FAILED(hr))
	{
		Failure("String compare failed");
		Warn("String compare failed");
		WarnHr(hr);
		return;
	}

	if (nExpected == nActual)
		return; // 0 == 0 for equal
	if (nExpected < 0 && nActual < 0)
		return;
	if (nExpected > 0 && nActual > 0)
		return;
	Failure("String compare gave wrong result");
	Warn("String compare gave wrong result");
}

/***********************************************************************************************
	LangTest 6   Methods
***********************************************************************************************/

LangTest6::LangTest6()
:TestBase(L"IME tests", 6)
{
}

LangTest6::~LangTest6()
{
}

HRESULT LangTest6::Run()
{
/* IClassInitMonikerPtr, CLSID_ClassInitMoniker, ITsPropsBuilderPtr,
  ITsStringFactoryPtr, ITsStringBuilderPtr, CLSID_TsPropsBuilder,
  kttptFontFamily, CLSID_TsStringFactory undeclared...
	HRESULT hr;
	// Make a default IME object. Initialize it the standard way with a moniker.
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	WarnHr(hr);
	hr = qcim->InitNew(CLSID_LgInputMethodEditor, (const BYTE *)L"", 4 * isizeof(wchar));
	WarnHr(hr);

	IBindCtxPtr qbc;
	hr = ::CreateBindCtx(NULL, &qbc);
	WarnHr(hr);
	hr = qcim->BindToObject(qbc, NULL, IID_ILgInputMethodEditor, (void **)&m_qime);
	WarnHr(hr);

	m_qime->Setup();
	StrUni stuUnexp, stuLogMsg;
	ITsPropsBuilderPtr qtpb;
	ITsStringFactoryPtr qtsf;
	ITsStringBuilderPtr qtsb;
	ITsStringPtr qtss1;
	ITsTextPropsPtr qttp;

	int cactBackspace;
	int cactDelForward;
	int ichStart;
	int ichModMin;
	int ichModLim;
	int ichModIP;
	int cactBsRemaining;
	int cactDfRemaining;
	int enc = 0;
	SmartBstr sbstr1;
	SmartBstr sbstr2;
	int ichMin;
	int ichLim;
	const kenc1 = 3;  // first test encoding
	const kws = 81;	// sample writing system

	hr = qtpb.CreateInstance(CLSID_TsPropsBuilder);
	WarnHr(hr);
	hr = qtpb->PushEncoding(kenc1, kws);
	WarnHr(hr);
	StrUni stu = L"serif";
	hr = qtpb->SetStringPropValues(kttptFontFamily, 0, stu.Bstr());
	WarnHr(hr);

	// Setup initial stringBuilder ReplaceTsString
	hr = qtsf.CreateInstance(CLSID_TsStringFactory);
	WarnHr(hr);
	hr = qtsf->MakeString(NULL, enc, &qtss1);
	WarnHr(hr);
	hr = qtss1->GetBuilder(&qtsb);
	WarnHr(hr);

	// Make a TsTextProps object from the builder
	hr = qtpb->GetTextProps(&qttp);
	WarnHr(hr);

	// Replace the null string with sbstr1 and properties qttp
	ichMin=0;
	ichLim=0;
	sbstr1 = L"TUVWXY\xd800\xdd00Z\xd800\xdfffpqrs";
	hr = qtsb->Replace(ichMin, ichLim, sbstr1, qttp);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);

	//check LgInput Method Editor Backspace method.
	ichStart=10;
	cactBackspace=5;
	hr = m_qime->Backspace(ichStart, cactBackspace, qtsb, &ichModMin, &ichModLim, &ichModIP, &cactBsRemaining);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
	if(wcscmp(sbstr2,L"TUVWpqrs") != 0 || ichModMin != 4 || ichModLim != 4 || ichModIP != 4 || cactBsRemaining != 0)
	{
		Warn("LgInputMethodEditor Backspace method 1 error.");
	}

	ichStart=3;
	cactBackspace=7;
	hr = m_qime->Backspace(ichStart, cactBackspace, qtsb, &ichModMin, &ichModLim, &ichModIP, &cactBsRemaining);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
	if(wcscmp(sbstr2,L"Wpqrs") != 0 || ichModMin != 0 || ichModLim != 0 || ichModIP != 0 || cactBsRemaining != 4)
	{
		Warn("LgInputMethodEditor Backspace method 2 error.");
	}

	ichStart=0;
	cactBackspace=7;
	hr = m_qime->Backspace(ichStart, cactBackspace, qtsb, &ichModMin, &ichModLim, &ichModIP, &cactBsRemaining);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
	if(wcscmp(sbstr2,L"Wpqrs") != 0 || ichModMin != 0 || ichModLim != 0 || ichModIP != 0 || cactBsRemaining != 7)
	{
		Warn("LgInputMethodEditor Backspace method 3 error.");
	}

	ichStart=4;
	cactBackspace=2;
	hr = m_qime->Backspace(ichStart, cactBackspace, qtsb, &ichModMin, &ichModLim, &ichModIP, &cactBsRemaining);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
	if(wcscmp(sbstr2,L"Wps") != 0 || ichModMin != 2 || ichModLim != 2 || ichModIP != 2 || cactBsRemaining != 0)
	{
		Warn("LgInputMethodEditor Backspace method 4 error.");
	}

	// Replace the string with a known string
	sbstr1 = L"P\xd800\xdd00RS\xdbff\xdc00TUVWXYZ";
	hr = qtsb->get_Length(&ichLim);
	WarnHr(hr);
	hr = qtsb->Replace(0, ichLim, sbstr1, qttp);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);

	//check LgInput Method Editor DeleteForward method.
	ichStart=2;
	cactDelForward=4;
	hr = m_qime->DeleteForward(ichStart, cactDelForward, qtsb, &ichModMin, &ichModLim, &ichModIP, &cactDfRemaining);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
	if(wcscmp(sbstr2,L"PTUVWXYZ") != 0 || ichModMin != 1 || ichModLim != 1 || ichModIP != 1 || cactDfRemaining != 0)
	{
		Warn("LgInputMethodEditor Delete Forward method 1 error.");
	}

	ichStart=5;
	cactDelForward=10;
	hr = m_qime->DeleteForward(ichStart, cactDelForward, qtsb, &ichModMin, &ichModLim, &ichModIP, &cactDfRemaining);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
	if(wcscmp(sbstr2,L"PTUVW") != 0 || ichModMin != 5 || ichModLim != 5 || ichModIP != 5 || cactDfRemaining != 7)
	{
		Warn("LgInputMethodEditor Delete Forward method 2 error.");
	}

	ichStart=2;
	cactDelForward=2;
	hr = m_qime->DeleteForward(ichStart, cactDelForward, qtsb, &ichModMin, &ichModLim, &ichModIP, &cactDfRemaining);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
	if(wcscmp(sbstr2,L"PTW") != 0 || ichModMin != 2 || ichModLim != 2 || ichModIP != 2 || cactDfRemaining != 0)
	{
		Warn("LgInputMethodEditor Delete Forward method 3 error.");
	}

	// Replace the string with a known string
	sbstr1 = L"P\xd800\xdd00RS\xdbff\xdc00TUVWXYZ";
	hr = qtsb->get_Length(&ichLim);
	WarnHr(hr);
	hr = qtsb->Replace(0, ichLim, sbstr1, qttp);
	WarnHr(hr);
	hr = qtsb->GetString(&qtss1);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);

	//check IsValidInsertionPoint
	int ich = 2;
	BOOL fValid;
	hr = m_qime->IsValidInsertionPoint(ich, qtss1, &fValid);
	WarnHr(hr);
	if(fValid)
	{
		Warn("LgInputMethodEditor IsValidInsertionPoint method 1 error.");
	}

	ich = 4;
	hr = m_qime->IsValidInsertionPoint(ich, qtss1, &fValid);
	WarnHr(hr);
	if(!fValid)
	{
		Warn("LgInputMethodEditor IsValidInsertionPoint method 2 error.");
	}

	ich = 14;
	hr = m_qime->IsValidInsertionPoint(ich, qtss1, &fValid);
	WarnHr(hr);
	if(!fValid)
	{
		Warn("LgInputMethodEditor IsValidInsertionPoint method 3 error.");
	}
	ich = 20;
	hr = m_qime->IsValidInsertionPoint(ich, qtss1, &fValid);
	WarnHr(hr);
	if(fValid)
	{
		Warn("LgInputMethodEditor IsValidInsertionPoint method 4 error.");
	}

	//check InputMethodEditor Replace method

	sbstr1 = L"NEW";
	hr = qtpb->GetTextProps(&qttp);
	WarnHr(hr);
	ichMin = 10;
	ichLim = 12;
	hr = m_qime->Replace(sbstr1, qttp, qtsb , ichMin, ichLim, &ichModMin, &ichModLim, &ichModIP);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
	if(wcscmp(sbstr2,L"P\xd800\xdd00RS\xdbff\xdc00TUVNEWYZ") != 0 || ichModMin != 10|| ichModLim != 13 || ichModIP != 13)
	{
		Warn("LgInputMethodEditor Replace method 1 error.");
	}

	sbstr1 = L"-";
	ichMin = 2;
	ichLim = 6;
	hr = m_qime->Replace(sbstr1, qttp, qtsb , ichMin, ichLim, &ichModMin, &ichModLim, &ichModIP);
	WarnHr(hr);
	hr = qtsb->get_Text(&sbstr2);
	WarnHr(hr);
//	stuLogMsg.Format(L"After Replace 2,6: Min:%d Lim:%d IP:%d %s\r\n", ichModMin, ichModLim, ichModIP, sbstr2);
//	Log(stuLogMsg.Bstr());
	if(wcscmp(sbstr2,L"P-TUVNEWYZ") != 0 || ichModMin != 1|| ichModLim != 2 || ichModIP != 2)
	{
		Warn("LgInputMethodEditor Replace method 2 error.");
	}
*/
	return S_OK;
}


/***********************************************************************************************
	LangTest 7   Methods to test LgFontManager.
***********************************************************************************************/

LangTest7::LangTest7()
:TestBase(L"FontManager tests", 7)
{
}

LangTest7::~LangTest7()
{
}

HRESULT LangTest7::Run()
{
	HRESULT hr;
	ILgFontManagerPtr qfm;
	qfm.CreateInstance(CLSID_LgFontManager);

	SmartBstr sbstr = L"Times New Roman";
	OLECHAR rgchName[] = L"Times New Roman";
	BSTR bstrNames;
	ComBool fAvail = false;

	// STDMETHOD(AvailableFonts)(BSTR * pbstrNames);
	hr = qfm->AvailableFonts(&bstrNames);
	WarnHr(hr);
	if(!bstrNames)
	{
		Warn("LgFontManager::AvailableFonts does not return fonts.");
	}

	// STDMETHOD(IsFontAvailable)(BSTR bstrName, ComBool * pfAvail);
	hr = qfm->IsFontAvailable(sbstr, &fAvail);
	WarnHr(hr);
	if(!fAvail)
	{
		Warn("LgFontManager::IsFontAvailable cannot find Times New Roman.");
	}

	// STDMETHOD(IsFontAvailableRgch)(int cch, OLECHAR * prgchName, ComBool * pfAvail);
	hr = qfm->IsFontAvailableRgch(15, rgchName, &fAvail);
	WarnHr(hr);
	if(!fAvail)
	{
		Warn("LgFontManager::IsFontAvailableRgch cannot find font at 0.");
	}

	return S_OK;
}
