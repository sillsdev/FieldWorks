/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: WpDa.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides an implementation of ISilDataAccess for WorldPad, where the underlying
	data is a simple XML representation of a document consisting of a sequence of styled
	paragraphs.
	The object representing the whole database is always an StText with HVO 1.

	This first draft just makes a new (but not empty) dummy document.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

WpDa::WpDa()
{
}

WpDa::~WpDa()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize the data from the given file, or create a new empty string.
----------------------------------------------------------------------------------------------*/
void WpDa::InitNew(StrAnsi staFileName)
{
	if (staFileName == "")
	{
		InitNewEmpty();
		return;
	}

	Vector<StrUni> vstu;
	ReadTextFromFile(staFileName, vstu);

	HVO * prghvoPara = NewObj HVO[vstu.Size()];
	for (int istu = 0; istu < vstu.Size(); istu++)
		prghvoPara[istu] = istu + 2;
	CacheVecProp(1, kflidStText_Paragraphs, prghvoPara, vstu.Size());
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int enc = 100; // replace by the right number when we figure out what it is

	for (istu = 0; istu < vstu.Size(); istu++)
	{
		StrUni stuPara = vstu[istu];
		ITsStringPtr qtss;
		CheckHr(qtsf->MakeStringRgch(stuPara.Chars(), stuPara.Length(), enc, &qtss));
		CacheStringProp(istu + 2, kflidStTxtPara_Contents, qtss);
	}

	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuNormal = L"Normal";
	CheckHr(qtpb->SetStrPropValue(kspNamedStyle,stuNormal.Bstr()));

	delete[] prghvoPara;
}

/*----------------------------------------------------------------------------------------------
	Initialize an empty document. It has a document object (always ID 1!) and one paragraph
	containing (implicitly) an empty string. Style is set to Normal
	Review SharonC(JohnT): what encoding should the empty string have??
----------------------------------------------------------------------------------------------*/
void WpDa::InitNewEmpty()
{
	HVO hvoPara = 2;
	CacheVecProp(1, kflidStText_Paragraphs, &hvoPara, 1);
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;
	int enc = 100;
	CheckHr(qtsf->MakeStringRgch(L"", 0, enc, &qtss));
	CacheStringProp(hvoPara, kflidStTxtPara_Contents, qtss);
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuNormal = L"Normal";
	CheckHr(qtpb->SetStrPropValue(kspNamedStyle, stuNormal.Bstr()));
}

/*----------------------------------------------------------------------------------------------
	Load the data from the given file into an empty window.
----------------------------------------------------------------------------------------------*/
void WpDa::LoadIntoEmpty(StrAnsi staFileName, WpChildWnd * pwcw)
{
	Assert(staFileName != "");
	int ctss;
	CheckHr(get_VecSize(1, kflidStText_Paragraphs, &ctss));
	Assert(ctss <= 1);

	Vector<StrUni> vstu;
	ReadTextFromFile(staFileName, vstu);

	HVO * prghvoPara = NewObj HVO[vstu.Size()];
	for (int istu = 0; istu < vstu.Size(); istu++)
		prghvoPara[istu] = istu + 2;
	CacheVecProp(1, kflidStText_Paragraphs, prghvoPara, vstu.Size());
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int enc = 100; // replace by the right number when we figure out what it is

	for (istu = 0; istu < vstu.Size(); istu++)
	{
		StrUni stuPara = vstu[istu];
		ITsStringPtr qtss;
		CheckHr(qtsf->MakeStringRgch(stuPara.Chars(), stuPara.Length(), enc, &qtss));
		CacheStringProp(istu + 2, kflidStTxtPara_Contents, qtss);
	}

	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	StrUni stuNormal = L"Normal";
	CheckHr(qtpb->SetStrPropValue(kspNamedStyle, stuNormal.Bstr()));

	delete[] prghvoPara;

	pwcw->ChangeNumberOfStrings(vstu.Size());
}

/*----------------------------------------------------------------------------------------------
	Return true if the text buffer is empty--that is, containing only one empty string.
----------------------------------------------------------------------------------------------*/
bool WpDa::IsEmpty()
{
	int ctss;
	CheckHr(get_VecSize(1, kflidStText_Paragraphs, &ctss));

	if (ctss == 0)
		return true;
	if (ctss > 1)
		return false;

	ITsStringPtr qtss;
	CheckHr(get_StringProp(2, kflidStTxtPara_Contents, &qtss));
	int cchw;
	CheckHr(qtss->get_Length(&cchw));
	return (cchw == 0);
}

/*----------------------------------------------------------------------------------------------
	Read the text to render from the file being opened.
----------------------------------------------------------------------------------------------*/
void WpDa::ReadTextFromFile(StrAnsi staFileName, Vector<StrUni> & vstu)
{
	OLECHAR pswzData[5000];
	memset(pswzData, 0, 5000);

	char pszData[10000];
	memset(pszData, 0, 10000);

	FILE * f = fopen(staFileName.Chars(), "rb");

//	IStreamPtr qstrm;
//	FileStream::Create(staFileName.Chars(),kfstgmRead,&qstrm);

	if (!f)
	{
		//	No such file
		ThrowHr(WarnHr(E_FAIL));
	}

	fread(pszData, 1, 10000, f);
	fclose(f);

	char * pch = pszData;
	OLECHAR * pchw;
	if (*pch == 0xFFFFFFFF && *(pch + 1) == 0xFFFFFFFE)
	{
		//	UTF16
		memcpy(pswzData, pszData + 2, 9998);
	}
	else if (*pch == 0xFFFFFFFE && *(pch + 1) == 0xFFFFFFFF)
	{
		//	UTF16 with the byte order reversed.
		pch += 2;
		pchw = pswzData;
		while (pch < pszData + 10000 && *pch != 0)
		{
			*pchw = (wchar)(*pch << 8 | *(pch + 1));
			pch += 2;
			pchw++;
		}
	}
	else
	{
		//	Assume plain 8-bit ANSI data.
		pchw = pswzData;
		while (pch < pszData + 5000 && *pch != 0)
		{
			*pchw = *pch;
			pch++;
			pchw++;
		}
	}

	//	Make a list of strings.

	pchw = pswzData;
	OLECHAR * pchwStrMin = pchw;
	while (pchw < pswzData + 5000 && *pchw != 0)
	{
		if (*pchw == 13 && *(pchw + 1) == 10)	// CRLF
		{
			//	Make a new string;
			StrUni stu(pchwStrMin, pchw - pchwStrMin);
			vstu.Push(stu);
			pchw += 2;	// skip CRLF
			pchwStrMin = pchw;
		}
		else
			pchw++;
	}
	StrUni stu(pchwStrMin, pchw - pchwStrMin);
	vstu.Push(stu);
}

/*----------------------------------------------------------------------------------------------
	Save the text to a file.
----------------------------------------------------------------------------------------------*/
bool WpDa::SaveToFile(StrAnsi staFileName)
{
	IStreamPtr qstrm;
	FileStream::Create(staFileName.Chars(), kfstgmWrite | kfstgmCreate, &qstrm);
//	FileStream::Create("c:\\output.txt", kfstgmWrite | kfstgmCreate, &qstrm);

	ULONG cbWritten;
	//	Write byte-order mark.
	byte b = 0xFF;
	CheckHr(qstrm->Write(&b, 1, &cbWritten));
	b = 0xFE;
	CheckHr(qstrm->Write(&b, 1, &cbWritten));

	int ctss;
	CheckHr(get_VecSize(1, kflidStText_Paragraphs, &ctss));

	for (int itss = 0; itss < ctss; itss++)
	{
		ITsStringPtr qtss;
		CheckHr(get_StringProp(itss + 2, kflidStTxtPara_Contents, &qtss));
		BSTR bstr = NULL;
		int cchw;
		CheckHr(qtss->get_Length(&cchw));
		qtss->GetChars(0, cchw, &bstr);

		//	Write the string to the file.
		if (cchw > 0)
		{
			Assert(bstr);
			CheckHr(qstrm->Write(bstr, cchw * isizeof(wchar), &cbWritten));
		}

		if (itss < ctss - 1)
		{
			//	CRLF--little-endian
			b = 13;
			CheckHr(qstrm->Write(&b, 1, &cbWritten));
			b = 0;
			CheckHr(qstrm->Write(&b, 1, &cbWritten));
			b = 10;
			CheckHr(qstrm->Write(&b, 1, &cbWritten));
			b = 0;
			CheckHr(qstrm->Write(&b, 1, &cbWritten));
		}

		ReleaseBstr(bstr);
	}

	return true;
}
