/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: TestVwRoot.cpp
Responsibility: Luke Ulrich
Last reviewed: Not yet.

Description:

----------------------------------------------------------------------------------------------*/
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)


/***********************************************************************************************
	TestVwRoot Methods
***********************************************************************************************/
// Different actions are executed depending on the calling application this is handled by
// an #ifdef, #endif pragma statements
// Parameters: fDraw -> actually draw/output to screen/device context
//			   fBsl -> baseline to a log file?
TestVwRoot::TestVwRoot(bool fDraw, bool fBsl) : m_fDraw(fDraw), m_fBsl(fBsl)
{
	// The testharness is not being used by default...
	m_fth = false;

	// Use SilTestSite object to baseline - (SilTestSite.cpp, .h)
	// Smart pointer so do not have to destroy. Setup the baselining functionality even if the
	// the user does not want to baseline (m_fBsl = false). Control of baselining is handled
	// by the bool m_fBsl wherever a baselining statement occurs
	m_psts = NewObj SilTestSite();
	m_qst.Attach(m_psts);

#ifdef TESTHARNESS
	// Using the testharness
	m_fth = true;
	// Probably called from TestMan - create a "fake" rootbox
	Init();
#else
	SetBaselineFile(L"MainBase");
#endif
}

// Default constructor
TestVwRoot::~TestVwRoot()
{
}

// Returns the class SilTestSite ptr (why? for special baselining purposes other than
// what this class has specified. For example, to change the baseline file)
SilTestSite * TestVwRoot::GetSTSPtr()
{
	return m_psts;
}

// Not using the "fake" rootbox that is setup in the Init function call, therefore
// SetRootBox is a means of redirecting output to external programs by setting this
// and the VwGraphics ptr
void TestVwRoot::SetRootBox(IVwRootBoxPtr qrootb)
{
	// Assume user is providing a valid rootbox
	Assert(qrootb);
	m_qrootb = qrootb;
}

// See SetRootBox explanation above
void TestVwRoot::SetVwGraphicsPtr(VwGraphicsPtr qvg)
{
	// if using the test harness - leave. No need to set the graphics ptr
	if (m_fth)
		return;
	// Assume the user is not giving us an invalid pointer
	Assert(qvg);
	m_qvg = qvg;
}

// REVIEW LukeU Hmmm... do I want to move this and all the baselining core into MacroBase?
void TestVwRoot::SetBaselineFile(StrUni stuBslFile)
{
	// TestBase is used only for running via TestMan and its HRESULT checking abiilites
	// Baselining is incorporated using a hijacked portion of the TestHarness code located
	// in ..\\SilTestSite.cpp, .h These files hold nearly the same code that exists in
	// TestBase.cpp, .h

	// If not baselining return
	if (!m_fBsl)
		return;
	// The user provided a baseline name - insert path before name
	if (wcscmp(stuBslFile, L""))	{
		StrUni stuName = "c:\\fw\\teslog\\log\\";
		stuName.Append(stuBslFile.Chars());
		// This uses the hijacked portion of the TestHarness
		m_qst->SetBaselineFile(SmartBstr(stuBslFile.Chars()).Bstr());
	}
	else		// If no filename is given, generate a default baseline file
		m_qst->SetBaselineFile(SmartBstr(L"c:\\fw\\testlog\\log\\Interactive").Bstr());
}

// Initiate Macrobase output file
void TestVwRoot::SetOutfile(StrAnsi strName)
{
	// Call MacroBase inherited method - this opens strName file for writing
	SetMacroOut(strName);
}

// The Init function is useful only for the TestHarness, because to perform these view code
// tests certain objects (e.g. device context) are required that normally only exist with an
// actual window. The TestHarness has no such window. Here the Init function provides a "fake"
// or substitute interface with which tests can be performed
#ifdef TESTHARNESS
void TestVwRoot::Init()
{
	// Make a special VwGraphics (#define BASELINE in compiling it turns on special features).
	// The first argument references a SilTestSite object. Passing this to the VwGraphics
	// allows baseling from code located in the Views.cpp, .h files. The Views source code
	// represents graphical engine. This VwGraphics can  choose to draw (2nd arg) and/or record
	// attempts at drawing, into
	// the baseline (3rd arg). Both of the second arguments are bools.
	m_qvg.Attach(NewObj VwGraphics(m_psts, m_fDraw, m_fBsl));

	// Make an off-screen bitmap HDC and initialize the VwGraphics to use it.
	HDC hdcScr, hdcMem;
	hdcScr = GetDC(NULL);
	hdcMem = CreateCompatibleDC(hdcScr);
	HBITMAP hBitmap = CreateCompatibleBitmap(hdcMem, 500, 500);	// Arbitrary height and width
	SelectObject(hdcMem, hBitmap);
	ReleaseDC(NULL, hdcScr);

	// Must be called before anything can be done with the VwGraphics object
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

/*	ITsStringPtr qtss;
	int enc = 100;

	StrUni stuPara0 = L"This is the first paragraph";
	StrUni stuPara1 = L"Here is another paragraph, quite silly and trivial but it should help test things";
	StrUni stuPara2 = L"I try to keep the text in these quite different so they can't be confused";

	CheckHr(qtsf->MakeStringRgch(stuPara0.Chars(), stuPara0.Length(), enc, &qtss));
	m_qda->CacheStringProp(rghvoPara[0], kflidStTxtPara_Contents, qtss);

	CheckHr(qtsf->MakeStringRgch(stuPara1.Chars(), stuPara1.Length(), enc, &qtss));
	m_qda->CacheStringProp(rghvoPara[1], kflidStTxtPara_Contents, qtss);

	CheckHr(qtsf->MakeStringRgch(stuPara2.Chars(), stuPara2.Length(), enc, &qtss));
	m_qda->CacheStringProp(rghvoPara[2], kflidStTxtPara_Contents, qtss);

	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuNormal = L"Normal";
	CheckHr(qtpb->SetStrPropValue(kspNamedStyle, stuNormal.Bstr()));*/

	// Make the root box and initialize it.
	m_qrootb.CreateInstance(CLSID_VwRootBox);
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
#endif

// Each view class object should have a RunMacro function to execute the entire test
// script at once. Located in such a function should be a switch statement to parse
// out each separate testable function and its specific parameters
// Parameters: staMcrFile -> MacroFile to execute
//			   stuBslFile -> Baseline file to use
void TestVwRoot::RunMacro(StrAnsi staMcrFile, StrUni stuBslFile)
{
	SetBaselineFile(stuBslFile);
	// Attempt to open the test script and alert user if could not open file
	if (!SetMacroIn(staMcrFile))
	{
		m_psts->OutputFormat("%s does not exist or cannot be opened!", staMcrFile);
		return;
	}

#ifdef TESTHARNESS
	// This has to be done so that tests will run properly under the testharness environ
	// For more information on what each thing here does consult the views.idh file
	Rect rcSrc(0, 0, 96, 96);
	DoLayout(350);
	DoDrawRoot(rcSrc, rcSrc);
	m_qtrs->SetAvailWidth(m_qrootb, 400);
#endif

	// Essentially loops through each function call of the test script. Each function
	// call gets a separate line in the test script. RunLine will return 0 when it is
	// done
	while(RunLine())
		;
	// Close the macro file
	CloseMacroIn();
}

// This is a more lower level function that executes a single line of the test
// script. The current function depends upon the caret position in the test script.
// Each call to Runlines pushes the caret one line down
int TestVwRoot::RunLine()
{
	// The GetTstFunc returns the
	// (hopefully) unique function ID located in the test script
	int ifunc = GetTstFunc();
	// Determine which function to execute
	switch(ifunc)	{
	// Each case statement represents a different function. To retrieve the various
	// parameters using inherited MacroBase functions.
	case 6:		// OnTyping function
		{
			StrUni uni = ReadUni();
			int ivar1 = ReadInt();
			int ivar2 = ReadInt();
			int ivar3 = ReadInt();
			RECT rc1 = ReadRect();
			RECT rc2 = ReadRect();
			DoOnTyping(m_qvg, uni.Bstr(), ivar1, ivar2, ivar3, rc1, rc2);
		}
		break;
	case 8:		// OnSysChar function
		DoOnSysChar(ReadInt());
		break;
	case 9:		// OnExtendedKey function
		{
			int key = ReadInt();
			DoOnExtendedKey(key, VwShiftStatus(ReadInt()));
		}
		break;
	case 11:	// MouseDown function
		{
			int ivar1 = ReadInt();
			int ivar2 = ReadInt();
			RECT rc1 = ReadRect();
			RECT rc2 = ReadRect();
			DoMouseDown(ivar1, ivar2, rc1, rc2);
		}
		break;
	case 12:	// MouseMoveDrag function
		{
			int ivar1 = ReadInt();
			int ivar2 = ReadInt();
			RECT rc1 = ReadRect();
			RECT rc2 = ReadRect();
			DoMouseMoveDrag(ivar1, ivar2, rc1, rc2);
		}
		break;
	case 13:	// MouseDownExtended function
		{
			int ivar1 = ReadInt();
			int ivar2 = ReadInt();
			RECT rc1 = ReadRect();
			RECT rc2 = ReadRect();
			DoMouseDownExtended(ivar1, ivar2, rc1, rc2);
		}
		break;
	case 14:	// MouseUp function
		{
			int ivar1 = ReadInt();
			int ivar2 = ReadInt();
			RECT rc1 = ReadRect();
			RECT rc2 = ReadRect();
			DoMouseUp(ivar1, ivar2, rc1, rc2);
		}
		break;
	}
	// ifunc == 0 when no more functions are available to call
	return ifunc;
}

// This is a more lower level function that executes a single string as
// though it were a test script line
void TestVwRoot::RunString(string str)
{
	// The GetTstFunc returns the
	// (hopefully) unique function ID located in the test script
	SetStringIn(str);
	// Determine which function to execute
	switch(StrGetTstFunc())	{
	// Each case statement represents a different function. To retrieve the various
	// parameters using inherited MacroBase functions.
	case 6:		// OnTyping function
		{
			StrUni uni = StrReadUni();
			int ivar1 = StrReadInt();
			int ivar2 = StrReadInt();
			int ivar3 = StrReadInt();
			RECT rc1 = StrReadRect();
			RECT rc2 = StrReadRect();
			DoOnTyping(m_qvg, uni.Bstr(), ivar1, ivar2, ivar3, rc1, rc2);
		}
		break;
	case 8:		// OnSysChar function
		DoOnSysChar(StrReadInt());
		break;
	case 9:		// OnExtendedKey function
		{
			int key = StrReadInt();
			DoOnExtendedKey(key, VwShiftStatus(StrReadInt()));
		}
		break;
	case 11:	// MouseDown function
		{
			int ivar1 = StrReadInt();
			int ivar2 = StrReadInt();
			RECT rc1 = StrReadRect();
			RECT rc2 = StrReadRect();
			DoMouseDown(ivar1, ivar2, rc1, rc2);
		}
		break;
	case 12:	// MouseMoveDrag function
		{
			int ivar1 = StrReadInt();
			int ivar2 = StrReadInt();
			RECT rc1 = StrReadRect();
			RECT rc2 = StrReadRect();
			DoMouseMoveDrag(ivar1, ivar2, rc1, rc2);
		}
		break;
	case 13:	// MouseDownExtended function
		{
			int ivar1 = StrReadInt();
			int ivar2 = StrReadInt();
			RECT rc1 = StrReadRect();
			RECT rc2 = StrReadRect();
			DoMouseDownExtended(ivar1, ivar2, rc1, rc2);
		}
		break;
	case 14:	// MouseUp function
		{
			int ivar1 = StrReadInt();
			int ivar2 = StrReadInt();
			RECT rc1 = StrReadRect();
			RECT rc2 = StrReadRect();
			DoMouseUp(ivar1, ivar2, rc1, rc2);
		}
		break;
	}
}


// These are the actual functions executed by the test script. In this view class, all
// test functions use the m_qrootb VwRootBox object, and some use the VwGraphics object
// (e.g. OnTyping)
// The name of each function is the m_qrootb object function name prefixed by "Do". For
// a description of the RootBox function calls look at the views.idh file
void TestVwRoot::DoLayout(int width)
{
	// This portion of code is for recording purposes. m_fth -> using the testharness?
	// OutfileIsOpen() -> is the output file open?
	if (!m_fth && OutfileIsOpen())	{
		// Write the test function name and unique ID to the test script
		AddTstFunc("Layout", 16);
		// Write any parameters
		WriteInt(width);
		// Signal end of test script function
		EndTstFunc();
	}

	// Baselining?
	if (m_fBsl && m_psts)
		m_psts->OutputFormat("  FUNCTION: Layout(m_qvg, %d) \n", width);
	CheckHr(m_qrootb->Layout(m_qvg, width));
}

void TestVwRoot::DoDrawRoot(RECT rcSrcRoot, RECT rcDstRoot)
{
	// This is after Layout has been called
	if (m_fBsl && m_psts)
		m_psts->OutputFormat("  FUNCTION: DrawRoot(m_qvg, {%d, %d, %d, %d}, {%d, %d, %d, %d})"
			"\n", rcSrcRoot.left, rcSrcRoot.top, rcSrcRoot.right, rcSrcRoot.bottom,
			rcDstRoot.left,	rcDstRoot.top, rcDstRoot.right, rcDstRoot.bottom);
	CheckHr(m_qrootb->DrawRoot(m_qvg, rcSrcRoot, rcDstRoot));
}

/*void TestVwRoot::WrapMakeSimpleSel(ComBool fInitial, ComBool fEdit, ComBool fRange)
{
	m_psts->OutputFormat("  FUNCTION: MakeSimpleSel(%d, %d, %d) \n", fInitial, fEdit, fRange);
	CheckHr(m_qrootb->MakeSimpleSel(fInitial, fEdit, fRange));
}

void TestVwRoot::TestMakeTextSelection()
{
	m_psts->Output("\n	Now Testing MakeTextSelection\n");

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
void TestVwRoot::TestSelection()
{
	IVwSelectionPtr qvsl;
	CheckHr(m_qrootb->get_Selection(&qvsl));
}

*/

void TestVwRoot::DoOnTyping(VwGraphicsPtr qvg, SmartBstr bstr, int cchBackspace,
							int cchDelForward, OLECHAR oleChar,	RECT rcSrc, RECT rcDst)
{
	CheckHr(m_qrootb->OnTyping(qvg, bstr, cchBackspace, cchDelForward, oleChar,
		rcSrc, rcDst));
	// Convert the bstr to ansistring for file output purposes. Here taking advantage of the
	// StrAnsi and StrUni character classes for the conversion
	StrAnsi staAns = StrUni(bstr.Chars()).Chars();
	// Probably a virtual key like VK_DELETE or backspace has been typed and there are no
	// characters in the bstr. In this case must assign a ALT-255 character so that when
	// reading the test script know when to terminate reading characters. The bstr gets written
	// as a series of ansi characters. This could include a space so incorporate a "stop"
	// character
	if (bstr.Length() == 0)
		bstr.Assign(L"_");
	if (m_fBsl && m_psts)
		m_psts->OutputFormat("  FUNCTION: OnTyping(m_qvg, \"%s\", %d, %d, '%d', {%d,%d,%d,%d}, "
			"{%d,%d,%d,%d})\n", staAns.Chars(), cchBackspace, cchDelForward, oleChar,
			rcSrc.left,	rcSrc.top, rcSrc.right, rcSrc.bottom, rcDst.left, rcDst.top,
			rcDst.right, rcDst.bottom);
}
/*
void TestVwRoot::DoOnChar(int _chw)
{
	m_psts->OutputFormat("  FUNCTION: OnChar(%d)\n", _chw);
	CheckHr(m_qrootb->OnChar(_chw));
}
*/
void TestVwRoot::DoOnSysChar(int _chw)
{
	if (m_fBsl && m_psts)
		m_psts->OutputFormat("  FUNCTION: OnSysChar(%d)\n", _chw);
	CheckHr(m_qrootb->OnSysChar(_chw));
}

void TestVwRoot::DoOnExtendedKey(int _chw, VwShiftStatus _ss)
{
	if (m_fBsl && m_psts)	{
		StrAnsi staExt;
		// Parse out the specific Shift and Control status for recording purposes
		switch (_ss)
		{
			case kfssNone:
				staExt.Append("None");
				break;
			case kgrfssShiftControl:
				staExt.Append("Shift + Control");
				break;
			case kfssShift:
				staExt.Append("Shift");
				break;
			case kfssControl:
				staExt.Append("Control");
				break;
			default:
				staExt.Append("Undefined state");
		}
		m_psts->OutputFormat("  FUNCTION: OnExtendedKey(%d, %s)\n", _chw, staExt.Chars());
	}
	CheckHr(m_qrootb->OnExtendedKey(_chw, _ss));
}

void TestVwRoot::DoMouseDown(int _xd, int _yd, RECT _rcSrc, RECT _rcDst)
{
	if (m_fBsl && m_psts)
		m_psts->OutputFormat("  FUNCTION: MouseDown(%d, %d, {%d,%d,%d,%d}, {%d,%d,%d,%d})\n",
			_xd, _yd, _rcSrc.left, _rcSrc.top, _rcSrc.right, _rcSrc.bottom, _rcDst.left,
			_rcDst.top, _rcDst.right, _rcDst.bottom);
	CheckHr(m_qrootb->MouseDown(_xd, _yd, _rcSrc, _rcDst));
}

void TestVwRoot::DoMouseMoveDrag(int _xd, int _yd, RECT _rcSrc, RECT _rcDst)
{
	if (m_fBsl && m_psts)
		m_psts->OutputFormat("  FUNCTION: MouseMoveDrag(%d, %d, {%d,%d,%d,%d}, {%d,%d,%d,%d})"
			"\n", _xd, _yd, _rcSrc.left, _rcSrc.top, _rcSrc.right, _rcSrc.bottom, _rcDst.left,
			_rcDst.top, _rcDst.right, _rcDst.bottom);
	CheckHr(m_qrootb->MouseMoveDrag(_xd, _yd, _rcSrc, _rcDst));
}

void TestVwRoot::DoMouseDownExtended(int _xd, int _yd, RECT _rcSrc, RECT _rcDst)
{
	if (m_fBsl && m_psts)
		m_psts->OutputFormat("  FUNCTION: MouseDownExtended(%d, %d, {%d,%d,%d,%d}, {%d,%d,%d,%d})"
			"\n", _xd, _yd,	_rcSrc.left, _rcSrc.top, _rcSrc.right, _rcSrc.bottom, _rcDst.left,
			_rcDst.top, _rcDst.right, _rcDst.bottom);
	CheckHr(m_qrootb->MouseDownExtended(_xd, _yd, _rcSrc, _rcDst));
}

void TestVwRoot::DoMouseUp(int _xd, int _yd, RECT _rcSrc, RECT _rcDst)
{
	if (m_fBsl && m_psts)
		m_psts->OutputFormat("  FUNCTION: MouseUp(%d, %d, {%d,%d,%d,%d}, {%d,%d,%d,%d})\n",
			_xd, _yd, _rcSrc.left, _rcSrc.top, _rcSrc.right, _rcSrc.bottom, _rcDst.left,
			_rcDst.top, _rcDst.right, _rcDst.bottom);
	CheckHr(m_qrootb->MouseUp(_xd, _yd, _rcSrc, _rcDst));
}
