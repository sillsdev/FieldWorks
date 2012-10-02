/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: InterlinExample.cpp
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Interlinear text example application.
	In contrast with the "Interlinear" example, this one is not using real data from the
	database, but a simple sample data set. The purpose of the example is to illustrate many
	of the capabilities of the Views subsystem. The example is extensively discussed in
	fw\doc\guides\Views_user_guide.htm.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

// Create one global instance. It has to exist before WinMain is called.
IeApp g_app;

BEGIN_CMD_MAP(IeApp)
	ON_CID_ALL(kcidFileExit, &AfApp::CmdFileExit, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	IeApp methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
IeApp::IeApp()
{
	s_fws.SetRoot("Interlinear text example application"); //"Software\\SIL\\FieldWorks\\Hello World";
}


/*----------------------------------------------------------------------------------------------
	Initialize the application.
----------------------------------------------------------------------------------------------*/
void IeApp::Init(void)
{
	SuperClass::Init();

	AfWnd::RegisterClass("IeMainWnd", 0, 0, 0, COLOR_3DFACE, (int)kridInterlinExampleIcon);
	AfWnd::RegisterClass("IeClientWnd", kfwcsHorzRedraw | kfwcsVertRedraw, (int)IDC_ARROW, 0,
		COLOR_WINDOW);

	// Open initial window
	WndCreateStruct wcs;
	wcs.InitMain("IeMainWnd");
	IeMainWndPtr qwnd;
	qwnd.Create();

	qwnd->CreateHwnd(wcs);
	qwnd->Show(m_nShow);
}


/***********************************************************************************************
	IeMainWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Load settings specific to this window.
----------------------------------------------------------------------------------------------*/
void IeMainWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::LoadSettings(pszRoot, fRecursive);

	FwSettings * pfws;
	pfws = AfApp::GetSettings();

	// TODO: Use methods defined on pfws to load settings.

	// Get window position.
	LoadWindowPosition(pszRoot, "Position");

	::ShowWindow(m_hwnd, SW_SHOW);
	OnIdle();
	::UpdateWindow(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Save settings specific to this window.
----------------------------------------------------------------------------------------------*/
void IeMainWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	AssertPszN(pszRoot);

	SuperClass::SaveSettings(pszRoot, fRecursive);

	SaveWindowPosition(pszRoot, "Position");

	FwSettings * pfws;
	pfws = AfApp::GetSettings();

	// TODO: Use methods defined on pfws to save settings.
}


/*----------------------------------------------------------------------------------------------
	The hwnd has been attached.
----------------------------------------------------------------------------------------------*/
void IeMainWnd::PostAttach(void)
{
	StrAppBuf strbT; // Holds temp string

	// Set the default caption text.
	strbT.Load(kstidInterlinExample);
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)strbT.Chars());

	// This creates the main frame window and sets it as the current window. It also
	// creates the rebar and status bar.
	SuperClass::PostAttach();

	// Create the menu bar.
	AfMenuBarPtr qmnbr;
	qmnbr.Create();
	qmnbr->Initialize(m_hwnd, kridAppMenu, kridAppMenu, "Menu Bar");
	m_vqtlbr.Push(qmnbr.Ptr());
	CreateToolBar(qmnbr, true, true, 200);

	// Load window settings.
	LoadSettings(NULL, false);

	g_app.AddCmdHandler(this, 1);
	m_qstbr->RestoreStatusText();

	// Create the client window.
	const int kwidChild = 1000;
	WndCreateStruct wcs;
	wcs.InitChild("IeClientWnd", m_hwnd, kwidChild);
	wcs.dwExStyle |= WS_EX_CLIENTEDGE;
	m_iecw.Create();
	m_iecw->CreateHwnd(wcs);
	::ShowWindow(m_iecw->Hwnd(), SW_SHOW);
}


/*----------------------------------------------------------------------------------------------
	Resize the child window.
----------------------------------------------------------------------------------------------*/
bool IeMainWnd::OnSize(int wst, int dxp, int dyp)
{
	if (m_iecw)
	{
		Rect rc;
		GetClientRect(rc);
		::MoveWindow(m_iecw->Hwnd(), rc.left, rc.top, rc.Width(), rc.Height(), true);
	}
	return SuperClass::OnSize(wst, dxp, dyp);
}


/*----------------------------------------------------------------------------------------------
	As it finally goes away, make doubly sure all pointers get cleared. This helps break cycles.
----------------------------------------------------------------------------------------------*/
void IeMainWnd::OnReleasePtr()
{
	m_iecw.Clear();
	g_app.RemoveCmdHandler(this, 1);
	SuperClass::OnReleasePtr();
}


/***********************************************************************************************
	IeClientWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void IeClientWnd::MakeRoot(IVwGraphics * pvg, IVwRootBox ** pprootb)
{
	*pprootb = NULL;

	Init(); // basic initialization of data.

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	// SetSite takes an IVwRootSite, which this class implements.
	CheckHr(qrootb->SetSite(this));

	// Which fragment of the root object we're going to display. Needs to be a variable
	// so we can simulate an array using &.
	int frag = kfrSentence;

	// We need a pointer to the pointer, and we can't use &qhvvc because that clears the
	// pointer!!
	IVwViewConstructor * pvvc = m_qievc;

	CheckHr(qrootb->putref_DataAccess(m_qcda));

	CheckHr(qrootb->SetRootObjects(&m_hvoRoot, &pvvc, &frag, m_qiest, 1));
	*pprootb = qrootb.Detach();
}

void IeClientWnd::Init()
{
	// Create a data access object.
	m_qcda.Attach(NewObj VwCacheDa());

	// Create a style sheet and initialize it.
	m_qiest.Attach(NewObj IeStylesheet());
	m_qiest->Init(m_qcda);

	// Create a view constructor
	m_qievc.Attach(NewObj IeVc());
	m_qievc->Init();

	// Create string factory and encodings.
	ITsStringPtr qtss; // used to obtain various strings from builders
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int enc1 = StrUtil::ParseEnc("FRN"); // Won't really be French, I don't know it.
	int enc2 = StrUtil::ParseEnc("ENG");

	// Now make some test strings and objects.
	// First the long string that is the 'free translation'
	ITsIncStrBldrPtr qtisb;
	CheckHr(qtsf->GetIncBldr(&qtisb));
	CheckHr(qtisb->SetIntPropValues(ktptEncAndWs, 0, enc1));
	StrUni stuStyle1 = "style1";
	CheckHr(qtisb->SetStrPropValue(kspNamedStyle, stuStyle1.Bstr()));
	OLECHAR * pszData1 = L"This is quite a long piece of test data used to try wrapping. ";
	int cchData1 = wcslen(pszData1);
	CheckHr(qtisb->AppendRgch(pszData1, cchData1));

	StrUni stuStyle2 = "style2";
	CheckHr(qtisb->SetStrPropValue(kspNamedStyle, stuStyle2.Bstr()));
	OLECHAR * pszData2 = L"This is another sentence which should wrap right along with it. ";
	int cchData2 = wcslen(pszData2);
	CheckHr(qtisb->AppendRgch(pszData2, cchData2));

	StrUni stuStyle3 = "style3";
	CheckHr(qtisb->SetStrPropValue(kspNamedStyle, stuStyle3.Bstr()));
	OLECHAR * pszData3 = L"This third sentence should be on a red background";
	int cchData3 = wcslen(pszData3);
	CheckHr(qtisb->AppendRgch(pszData3, cchData3));

	CheckHr(qtisb->GetString(&qtss));

	// The root object, an annotated sentence, is arbitrarily object -1
	m_hvoRoot = -1;
	// The fancy string we just made is its freeform translation
	CheckHr(m_qcda->CacheStringProp(m_hvoRoot, ktagFreeform, qtss));

	// Make a sequence of annotated words and store them in the sentence
	struct WordAnn {OLECHAR * word; OLECHAR * ann;};
	WordAnn pairs[] =
	{
		{L"This", L"det"},
		{L"is", L"verb"},
		{L"a", L"det"},
		{L"very", L"adv"},
		{L"long", L"adj"},
		{L"sentence", L"noun"},
		{L"which", L"conj"},
		{L"if", L"conj"},
		{L"all", L"noun"},
		{L"goes", L"verb"},
		{L"well", L"adv"},
		{L"should", L"verb"},
		{L"provide", L"verb"},
		{L"a", L"det"},
		{L"pretty", L"adv"},
		{L"good", L"adj"},
		{L"demonstration", L"noun"},
	};

	HvoVec vhvoWords;
	for (int i = 0; i < 17; i++)
	{
		HVO hvoWord = i + 1; // don't use 0
		vhvoWords.Push(hvoWord);
		CheckHr(qtsf->MakeStringRgch(pairs[i].word, wcslen(pairs[i].word), enc2, &qtss));
		CheckHr(m_qcda->CacheStringProp(hvoWord, ktagBase, qtss));
		CheckHr(qtsf->MakeStringRgch(pairs[i].ann, wcslen(pairs[i].ann), enc1, &qtss));
		CheckHr(m_qcda->CacheStringProp(hvoWord, ktagAnn, qtss));
	}
	CheckHr(m_qcda->CacheVecProp(m_hvoRoot, ktagWords, vhvoWords.Begin(), vhvoWords.Size()));
}

void IeVc::Init()
{
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int enc1 = StrUtil::ParseEnc("ENG");

	// Make some literal strings for table headings
	OLECHAR rgchMainWord[] = L"Main word";
	qtsf->MakeStringRgch(rgchMainWord, wcslen(rgchMainWord), enc1, &m_qtssMainWord);
	OLECHAR rgchGramCat[] = L"Gram. cat.";
	qtsf->MakeStringRgch(rgchGramCat, wcslen(rgchGramCat), enc1, &m_qtssGramCat);
	OLECHAR rgchLetters[] = L"Letters";
	qtsf->MakeStringRgch(rgchLetters, wcslen(rgchLetters), enc1, &m_qtssLetters);
	OLECHAR rgchFree[] = L"Free Translation";
	qtsf->MakeStringRgch(rgchFree, wcslen(rgchFree), enc1, &m_qtssFree);
	OLECHAR rgchWord[] = L"Word";
	qtsf->MakeStringRgch(rgchWord, wcslen(rgchWord), enc1, &m_qtssWord);
	OLECHAR rgchCat[] = L"Cat";
	qtsf->MakeStringRgch(rgchCat, wcslen(rgchCat), enc1, &m_qtssCat);
	OLECHAR rgch30Percent[] = L"30%";
	qtsf->MakeStringRgch(rgch30Percent, wcslen(rgch30Percent), enc1, &m_qtss30Percent);
	OLECHAR rgch20Percent[] = L"20%";
	qtsf->MakeStringRgch(rgch20Percent, wcslen(rgch20Percent), enc1, &m_qtss20Percent);
	OLECHAR rgchPt6In[] = L"0.6 in";
	qtsf->MakeStringRgch(rgchPt6In, wcslen(rgchPt6In), enc1, &m_qtssPt6In);
	OLECHAR rgchRest[] = L"rest";
	qtsf->MakeStringRgch(rgchRest, wcslen(rgchRest), enc1, &m_qtssRest);
}


/*----------------------------------------------------------------------------------------------
	Create a new 'object' for the style.
	Return the new HVO.
----------------------------------------------------------------------------------------------*/
HRESULT IeStylesheet::GetNewStyleHVO(HVO * phvoNewStyle)
{
	AssertPtr(phvoNewStyle);
	*phvoNewStyle = m_hvoNextStyle++;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Initialize the stylesheet for a new window.
----------------------------------------------------------------------------------------------*/
void IeStylesheet::Init(ISilDataAccess * psda)
{
	m_qsda = psda;
	m_hvoNextStyle = khvoStyleMin;

	//	Create a paragraph style called "Normal". For now just use a minimal (ie, empty)
	//	set of text properties.

	HVO hvoNormal;
	CheckHr(GetNewStyleHVO(&hvoNormal));
	ITsTextPropsPtr qttp;
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->GetTextProps(&qttp));
	CheckHr(PutStyleRgch(6,	L"Normal", hvoNormal, 0, 0,	kstParagraph, qttp));

	// Now create some real styles: first for 'style1'
	// Make a named style for style1, a character style: 30 pt bold Serif
	StrUni stuSerif(L"Serif");
	CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuSerif.Bstr()));
	CheckHr(qtpb->SetIntPropValues(ktptBold, ktpvEnum, kttvOff));
	CheckHr(qtpb->SetIntPropValues(ktptFontSize, ktpvMilliPoint, 30000));
	CheckHr(qtpb->GetTextProps(&qttp));
	HVO hvoStyle1;
	CheckHr(GetNewStyleHVO(&hvoStyle1));
	StrUni stuStyle1(L"style1");
	CheckHr(PutStyleRgch(stuStyle1.Length(), const_cast<OLECHAR *>(stuStyle1.Chars()),
		hvoStyle1, hvoNormal, hvoStyle1, kstCharacter, qttp));

	// Now make style2: 20 pt Serif, italic, cyan on transparent.
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuSerif.Bstr()));
	CheckHr(qtpb->SetIntPropValues(ktptItalic, ktpvEnum, kttvOn));
	CheckHr(qtpb->SetIntPropValues(ktptFontSize, ktpvMilliPoint, 20000));
	CheckHr(qtpb->SetIntPropValues(ktptForeColor, ktpvDefault, RGB(0, 255, 255)));
	CheckHr(qtpb->SetIntPropValues(ktptBackColor, ktpvDefault, kclrTransparent));
	CheckHr(qtpb->GetTextProps(&qttp));
	HVO hvoStyle2;
	CheckHr(GetNewStyleHVO(&hvoStyle2));
	StrUni stuStyle2(L"style2");
	CheckHr(PutStyleRgch(stuStyle2.Length(), const_cast<OLECHAR *>(stuStyle2.Chars()),
		hvoStyle2, hvoNormal, hvoStyle2, kstCharacter, qttp));

	// and style 3: 30 pt Serif, cyan on red, bold.
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuSerif.Bstr()));
	CheckHr(qtpb->SetIntPropValues(ktptBold, ktpvEnum, kttvOn));
	CheckHr(qtpb->SetIntPropValues(ktptFontSize, ktpvMilliPoint, 30000));
	CheckHr(qtpb->SetIntPropValues(ktptForeColor, ktpvDefault, RGB(0, 255, 255)));
	CheckHr(qtpb->SetIntPropValues(ktptBackColor, ktpvDefault, RGB(255,0,0)));
	CheckHr(qtpb->GetTextProps(&qttp));
	HVO hvoStyle3;
	CheckHr(GetNewStyleHVO(&hvoStyle3));
	StrUni stuStyle3(L"style3");
	CheckHr(PutStyleRgch(stuStyle3.Length(), const_cast<OLECHAR *>(stuStyle3.Chars()),
		hvoStyle3, hvoNormal, hvoStyle3, kstCharacter, qttp));

	// baseline text: 30 pt Serif, dark blue
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuSerif.Bstr()));
	CheckHr(qtpb->SetIntPropValues(ktptFontSize, ktpvMilliPoint, 30000));
	CheckHr(qtpb->SetIntPropValues(ktptForeColor, ktpvDefault, RGB(0,0,100)));
	CheckHr(qtpb->GetTextProps(&qttp));
	HVO hvoBase;
	CheckHr(GetNewStyleHVO(&hvoBase));
	StrUni stuBase(L"baseline");
	CheckHr(PutStyleRgch(stuBase.Length(), const_cast<OLECHAR *>(stuBase.Chars()),
		hvoBase, hvoNormal, hvoBase, kstCharacter, qttp));

	// annotation text: 20 pt Monospace, dark red
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuMono(L"Monospace");
	CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuMono.Bstr()));
	CheckHr(qtpb->SetIntPropValues(ktptFontSize, ktpvMilliPoint, 20000));
	CheckHr(qtpb->SetIntPropValues(ktptForeColor, ktpvDefault, RGB(0,0,100)));
	CheckHr(qtpb->GetTextProps(&qttp));
	HVO hvoAnn;
	CheckHr(GetNewStyleHVO(&hvoAnn));
	StrUni stuAnn(L"ann");
	CheckHr(PutStyleRgch(stuAnn.Length(), const_cast<OLECHAR *>(stuAnn.Chars()),
		hvoAnn, hvoNormal, hvoAnn, kstCharacter, qttp));
}


STDMETHODIMP IeVc::Display(IVwEnv* pvwenv, HVO hvoObj, int frag)
{
	try
	{
		switch(frag)
		{
		case kfrSentence:
			// the whole thing (this is the top level)
			CheckHr(Display(pvwenv, hvoObj, kfrSentInterlin));
			CheckHr(Display(pvwenv, hvoObj, kfrSentTable));
			break;
		case kfrSentInterlin:
			// Display paragaph of annotated word bundles followed by the freeform.
			CheckHr(pvwenv->OpenParagraph());
			CheckHr(pvwenv->AddObjVecItems(ktagWords, this, kfrAnnWordBundle));
			CheckHr(pvwenv->CloseParagraph());
			CheckHr(pvwenv->AddStringProp(ktagFreeform));
			break;
		case kfrAnnWordBundle:
			{ // Block
				// Display an annotated word as an inner pile of word, annotation.
				// put 10 points between adjacent piles
				CheckHr(pvwenv->put_IntProperty(kspMarginRight, ktpvMilliPoint, 10000));
				CheckHr(pvwenv->OpenInnerPile());

				StrUni stuBase(L"baseline");
				CheckHr(pvwenv->put_StringProperty(kspNamedStyle, stuBase.Bstr()));
				CheckHr(pvwenv->AddStringProp(ktagBase));

				StrUni stuAnn(L"ann");
				CheckHr(pvwenv->put_StringProperty(kspNamedStyle, stuAnn.Bstr()));
				CheckHr(pvwenv->AddStringProp(ktagAnn));

				CheckHr(pvwenv->CloseInnerPile());
			}
			break;
		case kfrSentTable:
			// Display table where each row is word, annotation, letter count for each.

			// Initialize the table and its column dimensions
			VwLength vlen;
			vlen.nVal = 10000; // 100 %
			vlen.unit = kunPercent100;
			CheckHr(pvwenv->OpenTable(5, // columns
				&vlen, // 100% of available width
				2000, // 2 point border
				kvaLeft,  // align cell contents left (only supported option currently)
				kvfpBox,  // frame table on all sides
				kvrlAll,   // rule between cells on all sides
				2000,		// spacing between cells 2 pt
				3000));		// padding within them 3 pt

			//first two columns take 30% and 20% of the space
			vlen.nVal = 3000;
			CheckHr(pvwenv->MakeColumnGroup(1,vlen));
			vlen.nVal = 2000;
			CheckHr(pvwenv->MakeColumnGroup(1,vlen));

			//next two columns are fixed, 0.6 inch wide
			vlen.nVal = 72*1000*6/10;
			vlen.unit = kunPoint1000;
			CheckHr(pvwenv->MakeColumnGroup(2,vlen));
			//use rest of space
			vlen.unit = kunRelative; // value does not matter for just one relative column
			CheckHr(pvwenv->MakeColumns(1,vlen));

			// Add the fixed table header info.
			CheckHr(pvwenv->OpenTableHeader());
				CheckHr(pvwenv->OpenTableRow());
					// Heading cell two rows high saying "Main word"
					CheckHr(pvwenv->OpenTableCell(2,1));
						CheckHr(pvwenv->AddString(m_qtssMainWord));
					CheckHr(pvwenv->CloseTableCell());

					// Heading cell two rows high saying "gram. cat."
					CheckHr(pvwenv->OpenTableCell(2,1));
						CheckHr(pvwenv->AddString(m_qtssGramCat));
					CheckHr(pvwenv->CloseTableCell());

					// Heading cell two columns wide saying "letters"
					CheckHr(pvwenv->OpenTableCell(1,2));
						CheckHr(pvwenv->AddString(m_qtssLetters));
					CheckHr(pvwenv->CloseTableCell());

					// Heading cell 2 rows high for last column saying "free translation"
					CheckHr(pvwenv->OpenTableCell(2,1));
						CheckHr(pvwenv->AddString(m_qtssFree));
					CheckHr(pvwenv->CloseTableCell());

				CheckHr(pvwenv->CloseTableRow());

				// Another row of heading, has just two cells, which should wind up
				// under the "letters" heading.
				CheckHr(pvwenv->OpenTableRow());
					// heading for "word" letters
					CheckHr(pvwenv->OpenTableCell(1,1));
						CheckHr(pvwenv->AddString(m_qtssWord));
					CheckHr(pvwenv->CloseTableCell());
					// heading for "cat" letters
					CheckHr(pvwenv->OpenTableCell(1,1));
						CheckHr(pvwenv->AddString(m_qtssCat));
					CheckHr(pvwenv->CloseTableCell());
				CheckHr(pvwenv->CloseTableRow());

				// A further row of headings indicates the policies being used for column widths.
				CheckHr(pvwenv->OpenTableRow());
					CheckHr(pvwenv->OpenTableCell(1,1));
						CheckHr(pvwenv->AddString(m_qtss30Percent));
					CheckHr(pvwenv->CloseTableCell());

					CheckHr(pvwenv->OpenTableCell(1,1));
						CheckHr(pvwenv->AddString(m_qtss20Percent));
					CheckHr(pvwenv->CloseTableCell());

					CheckHr(pvwenv->OpenTableCell(1,1));
						CheckHr(pvwenv->AddString(m_qtssPt6In));
					CheckHr(pvwenv->CloseTableCell());

					CheckHr(pvwenv->OpenTableCell(1,1));
						CheckHr(pvwenv->AddString(m_qtssPt6In));
					CheckHr(pvwenv->CloseTableCell());

					CheckHr(pvwenv->OpenTableCell(1,1));
						CheckHr(pvwenv->AddString(m_qtssRest));
					CheckHr(pvwenv->CloseTableCell());
				CheckHr(pvwenv->CloseTableRow());
			CheckHr(pvwenv->CloseTableHeader());

			// Main part of table.
			CheckHr(pvwenv->OpenTableBody());

			CheckHr(pvwenv->AddObjVecItems(ktagWords, this, kfrAnnWordRow));

			CheckHr(pvwenv->CloseTableBody());

			CheckHr(pvwenv->CloseTable());
			break;
		case kfrAnnWordRow:
			{ // BLOCK (so we can have smart pointers defined in it.)
				// Display annotated word as row in table.
				// If first word in attr, display fifth cell with freeform ann.
				CheckHr(pvwenv->OpenTableRow());

				CheckHr(pvwenv->OpenTableCell(1,1));
				CheckHr(pvwenv->AddStringProp(ktagBase));
				CheckHr(pvwenv->CloseTableCell());

				CheckHr(pvwenv->OpenTableCell(1,1));
				CheckHr(pvwenv->AddStringProp(ktagAnn));
				CheckHr(pvwenv->CloseTableCell());

				CheckHr(pvwenv->OpenTableCell(1,1));
				// AddProp needs this even though AddStringProp doesn't..at least to make it editable.
				CheckHr(pvwenv->OpenParagraph());
				CheckHr(pvwenv->AddProp(ktagBase, this, kfrStrLength));
				CheckHr(pvwenv->CloseParagraph());
				CheckHr(pvwenv->CloseTableCell());

				CheckHr(pvwenv->OpenTableCell(1,1));
				CheckHr(pvwenv->OpenParagraph());
				CheckHr(pvwenv->AddProp(ktagAnn, this, kfrStrLength));
				CheckHr(pvwenv->CloseParagraph());
				CheckHr(pvwenv->CloseTableCell());

				// See if this is the first word.
				int lev;
				CheckHr(pvwenv->get_EmbeddingLevel(&lev));
				HVO hvoOuter;
				int ihvoItem;
				PropTag tagOuter;
				CheckHr(pvwenv->GetOuterObject(lev - 1, &hvoOuter, &tagOuter, &ihvoItem));
				if (ihvoItem == 0)
				{
					// Add a fifth cell containing the freeform annotation.
					// It is the full height of the table.
					ISilDataAccessPtr qsda;
					CheckHr(pvwenv->get_DataAccess(&qsda));
					int cannword;
					CheckHr(qsda->get_VecSize(hvoOuter, tagOuter, &cannword));
					CheckHr(pvwenv->OpenTableCell(cannword,1));
					CheckHr(pvwenv->AddObj(hvoOuter, this, kfrSentFreeform));
					CheckHr(pvwenv->CloseTableCell());
				}
				CheckHr(pvwenv->CloseTableRow());
			}
			break;
		case kfrSentFreeform:
			// Display just the freeform attribute.
			// Assert that hvoObj is an AnnSent
			CheckHr(pvwenv->AddStringProp(ktagFreeform));
			break;
		}
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
	return S_OK;
}


STDMETHODIMP IeVc::DisplayVariant(IVwEnv * pvwenv, VARIANT v, int frag,
	ITsString ** pptss)
{
	try
	{
		switch(frag)
		{
		case kfrStrLength:
			{ // BLOCK, for smart pointers
				// Display the length of the string in the variant
				SmartVariant svar(v);
				ITsStringPtr qtss;
				CheckHr(svar.GetObject(IID_ITsString, (void **) &qtss));
				int cch;
				CheckHr(qtss->get_Length(&cch));
				StrUni stuNumber;
				stuNumber.Format(L"%d", cch);
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				CheckHr(qtsf->MakeString(stuNumber.Bstr(), StrUtil::ParseEnc("ENG"), pptss));
			}
			break;
		default:
			Assert(false);
			return WarnHr(E_UNEXPECTED);
		}
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	return S_OK;
}

STDMETHODIMP IeVc::UpdateProp(ISilDataAccess * psda, HVO hvoObj, int tag, int frag, ITsString * ptssVal,
	ITsString ** pptssRepVal)
{
	try
	{
		pptssRepVal = NULL;

		switch(frag)
		{
		case kfrStrLength:
			{// BLOCK, to avoid skipping initialization warnings
				// It was a display of the string length.
				// Truncate the string or extend with asterisks.
				const wchar * pch;
				int cch;
				CheckHr(ptssVal->LockText(&pch, &cch));
				int cchAttr = _wtoi(pch);
				CheckHr(ptssVal->UnlockText(pch));

				ITsStringPtr qtssAttr;
				CheckHr(psda->get_StringProp(hvoObj, tag, &qtssAttr));
				int cchCur;
				CheckHr(qtssAttr->get_Length(&cchCur));
				// Don't allow outrageous length
				int cchAttrFix;
				cchAttrFix = min(cchAttr, 200);
				cchAttrFix = max(cchAttrFix, 0);
				if (cchCur != cchAttrFix)
				{
					ITsStrBldrPtr qtsb;
					CheckHr(qtssAttr->GetBldr(&qtsb));
					if (cchCur < cchAttr)
					{
						// Add asterisks
						int cchExtra = cchAttrFix - cchCur;
						wchar rgbuf[201];
						for (wchar * pchBuf = rgbuf; pchBuf < rgbuf + cchExtra; pchBuf++)
							*pchBuf = '*';
						ITsTextPropsPtr qttp;
						CheckHr(qtssAttr->get_PropertiesAt(cchCur, &qttp));
						CheckHr(qtsb->ReplaceRgch(cchCur, cchCur, rgbuf, cchExtra, qttp));
					}
					else
					{
						// truncate string
						CheckHr(qtsb->ReplaceRgch(cchAttrFix, cchCur, NULL, 0, NULL));
					}
					ITsStringPtr qtssNew;
					CheckHr(qtsb->GetString(&qtssNew));
					StrUni stuUndoText(L"Edit length");
					CheckHr(psda->SetString(hvoObj, tag, qtssNew, stuUndoText.Bstr()));
				}
				else if (cchAttr != cchAttrFix)
				{
					// If we updated the attr, the length will get updated automatically.
					// If we didn't, and it was out of range, need to fix.
					wchar_t buf[20];
					swprintf(buf, L"%d", cchAttrFix);
					ITsStrFactoryPtr qtsf;
					qtsf.CreateInstance(CLSID_TsStrFactory);
					CheckHr(qtsf->MakeStringRgch(buf, wcslen(buf), StrUtil::ParseEnc("ENG"), pptssRepVal));
				}
			}
			break;
		default:
			Assert(false);
			ThrowHr( WarnHr(E_UNEXPECTED));
		}
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	return S_OK;
}