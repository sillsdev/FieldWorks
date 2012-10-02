/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: langtest.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef LANGTEST_INCLUDED
#define LANGTEST_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: langtest
Description:
Hungarian:
----------------------------------------------------------------------------------------------*/
class LangTest1 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	LangTest1();
	virtual ~LangTest1();

	// Member variable access

	// Other public methods
	HRESULT Run();
	void TestCompare(StrUni stuKey1, StrUni stuKey2, int nExpected);

protected:
	// Member variables
	ILgCollatingEnginePtr m_qcoleng;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};

// character properties
class LangTest2 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	LangTest2();
	virtual ~LangTest2();

	// Member variable access

	// Other public methods
	HRESULT Run();
	void BaselineItoAArray(const int * prgnIn, int cn);
	void BaselineBtoAArray(const byte * prgnIn, int cn);
	void CheckBoolHr(HRESULT hr, bool fRes, bool fExpected, wchar * pszTestName);
	void TestUpper(StrUni stuFrom, StrUni stuExpected, HRESULT hrExpected = S_OK);
	void TestLower(StrUni stuFrom, StrUni stuExpected, HRESULT hrExpected = S_OK);
	void TestTitle(StrUni stuFrom, StrUni stuExpected, HRESULT hrExpected = S_OK);
	void TestUpperCh(int ch, int chExpected, HRESULT hrExpected = S_OK);
	void TestLowerCh(int ch, int chExpected, HRESULT hrExpected = S_OK);
	void TestTitleCh(int ch, int chExpected, HRESULT hrExpected = S_OK);
	void TestDecomp(int ch, OLECHAR * pszExpected);
	void TestFullDecomp(int ch, OLECHAR * pszExpected);
	void CheckIntHr(HRESULT hr, int nRes, int nExpected, wchar * pszTestName);
	void CheckName(int ch, OLECHAR * pszExpected);

protected:
	// Member variables
	ILgCharacterPropertyEnginePtr m_qlcpe;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};

class LangTest3 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	LangTest3();
	virtual ~LangTest3();

	// Member variable access

	// Other public methods
	HRESULT Run();
	void TestGetToken(OLECHAR *prgchInput, int cch, HRESULT hrExp,
		int ichMinExp, int ichLimExp, StrUni stuComment);
	void TestGetTokenStartEnd(BSTR bstrInput, int ichFirst, HRESULT hrExp,
		int ichMinExp, int ichLimExp, StrUni stuComment);

protected:
	// Member variables
	ILgTokenizerPtr m_qtoker;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};

class LangTest4 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	LangTest4();
	virtual ~LangTest4();

	// Member variable access

	// Other public methods
	HRESULT Run();

protected:
	// Member variables
	ILgNumericEnginePtr m_qnumeng;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};

class LangTest5 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	LangTest5();
	virtual ~LangTest5();

	// Member variable access

	// Other public methods
	HRESULT Run();
	void TestCompare(StrUni stuKey1, StrUni stuKey2, int nExpected);

protected:
	// Member variables
	ILgCollatingEnginePtr m_qcoleng;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};


class LangTest6 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	LangTest6();
	virtual ~LangTest6();

	// Member variable access

	// Other public methods
	HRESULT Run();

protected:
	// Member variables
	ILgInputMethodEditorPtr m_qime;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};


// Test LgFontManager.
class LangTest7 : public TestBase
{
public:
	// Static methods

	// Constructors/destructors/etc.
	LangTest7();
	virtual ~LangTest7();

	// Member variable access

	// Other public methods
	HRESULT Run();

protected:
	// Member variables

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};

#endif  //LANGTEST_INCLUDED
