/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpXslt.cpp
Responsibility: Sharon Correll
Last reviewed: never.

	This file contains the XSLT hooks for WorldPad's File-Save As item.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

/*
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "Set_i.cpp"
*/

#undef THIS_FILE
DEFINE_THIS_FILE

#define READ_SIZE 16384

//:End Ignore


//:>********************************************************************************************
//:>	Generating the available transforms.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return a string containing the value of the given XML attribute,
	or NULL if that attribute is not defined for this XML element.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszName Name to search for in prgpszAtts.
----------------------------------------------------------------------------------------------*/
static void GetAttributeValue(const XML_Char ** prgpszAtts, const XML_Char * pszName,
	StrApp & strOut)
{
	strOut.Clear();
	if (!prgpszAtts)
		return;
	for (int i = 0; prgpszAtts[i]; i += 2)
	{
		if (strcmp(prgpszAtts[i], pszName) == 0)	// Assumes sizeof(XML_Char) == sizeof(char)
		{
			if (sizeof(achar) == sizeof(XML_Char))
			{
				strOut.Assign(prgpszAtts[i+1]);
				return;
			}
			else if (sizeof(achar) < sizeof(XML_Char))
			{
				Assert(sizeof(achar) == sizeof(char) && sizeof(XML_Char) == sizeof(wchar));
				// Convert from UTF-16 to UTF-8
				Vector<char> vch;
				int cch16 = wcslen((const wchar *)prgpszAtts[i+1]);
				int cch8 = CountUtf8FromUtf16((const wchar *)prgpszAtts[i+1], cch16);
				vch.Resize(cch8 + 1);
				int cch = ConvertUtf16ToUtf8(vch.Begin(), cch8, (const wchar *)prgpszAtts[i+1],
					cch16);
				Assert(cch == cch8);
				vch[cch] = 0;
				strOut.Assign(vch.Begin());
			}
			else if (sizeof(achar) > sizeof(XML_Char))
			{
				Assert(sizeof(achar) == sizeof(wchar) && sizeof(XML_Char) == sizeof(char));
				// Convert from UTF-8 to UTF-16
				Vector<wchar> vch;
				int cch16 = CountUtf16FromUtf8((const char *)prgpszAtts[i+1]);
				vch.Resize(cch16 + 1);
				int cch;
				cch = SetUtf16FromUtf8(vch.Begin(), cch16+1, (const char *)prgpszAtts[i+1]);
				Assert(cch == cch16);
				strOut.Assign(vch.Begin());
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_StartElementHandler typedef for the documentation
	such as it is.

	This particular method is used for the rather trivial job of looking for the export
	information in the XSL file to use in the UI (the File-Save As dialog).

	@param pvUser Pointer to generic user data (always an AfExportStyleSheet object in this
					case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
static void HandleStartTag(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi staElem(pszName);
	if (staElem == "silfw:file")
	{
		AfExportStyleSheet * pess = reinterpret_cast<AfExportStyleSheet *>(pvUser);
		AssertPtr(pess);
		GetAttributeValue(prgpszAtts, "title", pess->m_strTitle);
		GetAttributeValue(prgpszAtts, "outputext", pess->m_strOutputExt);
		GetAttributeValue(prgpszAtts, "description", pess->m_strDescription);
		StrApp str;
		GetAttributeValue(prgpszAtts, "chain", str);
		if (str.Length())
			pess->m_vstrChain.Push(str);
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements.

	This static method is passed to the expat XML parser as a callback function.

	@param pvUser Pointer to generic user data (always an AfExportStyleSheet object in this
					case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
static void HandleEndTag(void * pvUser, const XML_Char * pszName)
{
	// This is a trivial parse.  We don't need to do anything with the end tags.
}

/*----------------------------------------------------------------------------------------------
	Look for the .XSL files that define transformations from WorldPad XML into some other
	format.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::FindXslTransforms(Vector<AfExportStyleSheet> & vess)
{
	StrApp strDir;
	StrApp strFileSpec = "";

	strDir.Assign(DirectoryFinder::FwRootCodeDir().Chars());

	int cch = strDir.Length();
	if (cch)
	{
		if (strDir[cch - 1] != '\\')
			strDir += _T("\\");

		//	Store the name of the temporary WPX file to use.

		StrApp strDirTmp;
		strDirTmp.Format(L"%sTemp\\", strDir.Chars());
		_tmkdir(strDirTmp.Chars()); // create the directory if needed
		StrUni stuDirTmp(strDirTmp);
		m_stuXsltTmp.Format(L"%sWorldPadXslt.xml", stuDirTmp.Chars());

		//	Look for .xsl files in the (RootCodeDir)\WorldPad\XSLT directory.

		strDir += _T("WorldPad\\XSLT");
		strFileSpec.Format(_T("%s\\*.xsl"), strDir.Chars());

		_tfinddata_t fd;
		achar rgchFileSpec[200];
		memcpy(rgchFileSpec, strFileSpec.Chars(), isizeof(achar) * (strFileSpec.Length() + 1));
		intptr_t h = _tfindfirst(rgchFileSpec, &fd);
		bool fMore = (h != -1);
		while (fMore)
		{
			AfExportStyleSheet ess;
			ess.m_strFile.Format(_T("%s\\%s"), strDir.Chars(), fd.name);
			ess.m_strTitle.Clear();
			ess.m_strOutputExt.Clear();
			ess.m_strDescription.Clear();
			ess.m_vstrChain.Clear();

			//	Get the information out the silfw::file tag in the file for use in the
			//	File-Save As dialog UI.
			const XML_Char * pszEncoding = NULL;
			XML_Parser parser = XML_ParserCreate(pszEncoding);
			StrAnsi staFile(ess.m_strFile);
			if (!XML_SetBase(parser, staFile.Chars()))
			{
				XML_ParserFree(parser);
				fMore = (_tfindnext(h, &fd) == 0);
				continue;
			}
			try
			{
				XML_SetUserData(parser, &ess);
				XML_SetElementHandler(parser, HandleStartTag, HandleEndTag);
				IStreamPtr qstrm;
				FileStream::Create(ess.m_strFile.Chars(), kfstgmRead, &qstrm);
				for (;;)
				{
					ulong cbRead;
					void * pBuffer = XML_GetBuffer(parser, READ_SIZE);
					if (!pBuffer)
						break;
					CheckHr(qstrm->Read(pBuffer, READ_SIZE, &cbRead));
					if (cbRead == 0)
						break;
					if (!XML_ParseBuffer(parser, cbRead, cbRead == 0))
						break;
					if (ess.m_strTitle.Length() && ess.m_strOutputExt.Length())
					{
						vess.Push(ess);
						break;
					}
				}
			}
			catch (...)
			{
				// Ignore any errors in parsing.
			}
			XML_ParserFree(parser);

			//	Get the next file.
			fMore = (_tfindnext(h, &fd) == 0);
		}
		_findclose(h);
	}
}


//:>********************************************************************************************
//:>	Running the transform.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Load an XML file (either an intermediate output file or a "stylesheet") for use in applying
	an XSL transformation.

	@param pDOM Pointer to the COM object representing an XML document in DOM form.
	@param bstrFile Pathname of the XML file to load.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::LoadDOM(IXMLDOMDocument * pDOM, BSTR bstrFile)
{
	VARIANT vInput;
	VARIANT_BOOL fSuccess;
	V_VT(&vInput) = VT_BSTR;
	V_BSTR(&vInput) = bstrFile;
	CheckHr(pDOM->put_async(VARIANT_FALSE));
	CheckHr(pDOM->put_preserveWhiteSpace(VARIANT_TRUE));
	CheckHr(pDOM->load(vInput, &fSuccess));

	if (fSuccess == VARIANT_FALSE)
	{
		ComSmartPtr<IXMLDOMParseError> qParseError;
		CheckHr(pDOM->get_parseError(&qParseError));
		CheckHr(qParseError->get_reason(&m_sbstrXsltErr));
		CheckHr(qParseError->get_errorCode(&m_nErrorCode));
		CheckHr(qParseError->get_line(&m_nErrorLine));
		ThrowHr(E_FAIL);
	}

}

// The following definitions are cribbed from the latest version of <msxml2.h>.
// The #ifndefs should prevent any conflicts.
#ifndef __XSLTemplate40_FWD_DEFINED__
#define __XSLTemplate40_FWD_DEFINED__
typedef class XSLTemplate40 XSLTemplate40;
EXTERN_C const CLSID CLSID_XSLTemplate40;
class DECLSPEC_UUID("88d969c3-f192-11d4-a65f-0040963251e5") XSLTemplate40;
#endif  /* __XSLTemplate40_FWD_DEFINED__ */
#ifndef __DOMDocument40_FWD_DEFINED__
#define __DOMDocument40_FWD_DEFINED__
typedef class DOMDocument40 DOMDocument40;
EXTERN_C const CLSID CLSID_DOMDocument40;
class DECLSPEC_UUID("88d969c0-f192-11d4-a65f-0040963251e5") DOMDocument40;
#endif  /* __DOMDocument40_FWD_DEFINED__ */
#ifndef __FreeThreadedDOMDocument40_FWD_DEFINED__
#define __FreeThreadedDOMDocument40_FWD_DEFINED__
typedef class FreeThreadedDOMDocument40 FreeThreadedDOMDocument40;
EXTERN_C const CLSID CLSID_FreeThreadedDOMDocument40;
class DECLSPEC_UUID("88d969c1-f192-11d4-a65f-0040963251e5") FreeThreadedDOMDocument40;
#endif  /* __FreeThreadedDOMDocument40_FWD_DEFINED__ */

enum MsXslVersion {VERSION_NONE, VERSION_26, VERSION_30, VERSION_40};
struct MsXslInfo
{
	MsXslVersion m_version;
	const WCHAR * m_pwszVersion;
	const char * m_pszTemplateCLSID;
	const CLSID * m_clsidTemplate;
	const WCHAR * m_pwszTemplateProgID;
	const CLSID * m_clsidDocument;
	const WCHAR * m_pwszDocumentProgID;
	const CLSID * m_clsidFreeDocument;
	const WCHAR * m_pwszFreeDocumentProgID;
};

// These entries are in priority order.
static MsXslInfo g_rgMsXslInfo[] =
{
	{
		VERSION_40,
		L"4.0",
		"{88d969c3-f192-11d4-a65f-0040963251e5}",
		&__uuidof(XSLTemplate40),
		L"MSXML2.XSLTemplate.4.0",
		&__uuidof(DOMDocument40),
		L"MSXML2.DOMDocument.4.0",
		&__uuidof(FreeThreadedDOMDocument40),
		L"MSXML2.FreeThreadedDOMDocument.4.0",
	},
	{
		VERSION_30,
		L"3.0",
		"{f5078f36-c551-11d3-89b9-0000f81fe221}",
		&__uuidof(XSLTemplate30),
		L"MSXML2.XSLTemplate.3.0",
		&__uuidof(DOMDocument30),
		L"MSXML2.DOMDocument.3.0",
		&__uuidof(FreeThreadedDOMDocument30),
		L"MSXML2.FreeThreadedDOMDocument.3.0",
	},
	{
		VERSION_26,
		L"2.6",
		"{f5078f21-c551-11d3-89b9-0000f81fe221}",
		&__uuidof(XSLTemplate26),
		L"MSXML2.XSLTemplate.2.6",
		&__uuidof(DOMDocument26),
		L"MSXML2.DOMDocument.2.6",
		&__uuidof(FreeThreadedDOMDocument26),
		L"MSXML2.FreeThreadedDOMDocument.2.6",
	},
};
static const int g_cMsXslInfo = sizeof(g_rgMsXslInfo) / sizeof(MsXslInfo);

/*----------------------------------------------------------------------------------------------
	Run an XSL transformation.
----------------------------------------------------------------------------------------------*/
bool WpMainWnd::RunXslTransform(Vector<AfExportStyleSheet> & vess, int iess,
	StrApp strFileOut)
{
	StrApp strWp(kstidAppName);

	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	//	Output the temporary WPX file.
	//	There is a line in ProcessXSL() that is supposed to turn off validation in the XML
	//	parser, but it doesn't seem to work. We work around it by just not including
	//	any indication of the DTD in the XML file; hence the false below.
	DataAccess()->SaveXml(m_stuXsltTmp.Bstr(), this, false);

	// Get information about the version of MSXML in use
	HKEY hkeyOuter = NULL;
	HKEY hkeyInner = NULL;
	LONG lRegRet;
	::RegOpenKeyExA(HKEY_CLASSES_ROOT, "CLSID", NULL, KEY_READ, &hkeyOuter);
	int iXsl;
	bool fFound = false;
	for (iXsl = 0; iXsl < g_cMsXslInfo; ++iXsl)
	{
		// Lookup XSLTemplate version dependent CLSID in registry
		lRegRet = ::RegOpenKeyExA(hkeyOuter, g_rgMsXslInfo[iXsl].m_pszTemplateCLSID, NULL,
			KEY_READ, &hkeyInner);
		if (lRegRet == NO_ERROR)
		{
			fFound = true;
			break;
		}
	}
	if (!fFound)
	{
		// Error message?
		ThrowHr(E_FAIL);
	}

	try
	{
		StrUni stuInput(m_stuXsltTmp);
		StrUni stuStylesheet(vess[iess].m_strFile);
		StrUni stuOutput;
		if (vess[iess].m_vstrChain.Size())
		{
			stuOutput.Assign(m_stuXsltTmp.Chars());
			stuOutput.Append(L"1");
			ProcessXsl(stuInput, stuStylesheet, stuOutput, iXsl);
			int ista;
			for (ista = 0; ista < vess[iess].m_vstrChain.Size(); ++ista)
			{
				stuInput.Assign(stuOutput);
				stuStylesheet.Assign(vess[iess].m_vstrChain[ista].Chars());
				if (ista + 1 < vess[iess].m_vstrChain.Size())
					stuOutput.Format(L"%S%d", m_stuXsltTmp, ista + 2);
				else
					stuOutput.Assign(strFileOut.Chars());
				ProcessXsl(stuInput, stuStylesheet, stuOutput, iXsl);
			}
		}
		else
		{
			stuOutput.Assign(strFileOut.Chars());
			ProcessXsl(stuInput, stuStylesheet, stuOutput, iXsl);
		}
	}
	catch (Throwable & thr)
	{
		StrApp strMsg(thr.Message());
		if (strMsg.Length() == 0)
		{
			if (m_sbstrXsltErr.Length())
				strMsg.Assign(m_sbstrXsltErr.Chars());
			else
				strMsg.Load(kstidXsltFailed);
		}
		::MessageBox(m_hwnd, strMsg.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
		return false;
	}
	catch (...)
	{
		StrApp strMsg(kstidXsltFailed);
		::MessageBox(m_hwnd, strMsg.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
		return false;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Apply the XSL stylesheet (transformation) to the input file, producing the output file.  Use
	the indicated version of the Microsoft XML services DLL.

	@param stuInput Pathname of the input file.
	@param stuStylesheet Pathname of the XSL transformation stylesheet.
	@param stuOutput Pathname of the output file (which may be an intermediate file).
	@param iXsl Index into g_rgMsXslInfo of the newest available Microsoft XML services DLL.
----------------------------------------------------------------------------------------------*/
void WpMainWnd::ProcessXsl(StrUni & stuInput, StrUni & stuStylesheet, StrUni & stuOutput,
	int iXsl)
{
	// Load the source document.
	ComSmartPtr<IXMLDOMDocument> qDOMInput;
	CheckHr(::CoCreateInstance(*g_rgMsXslInfo[iXsl].m_clsidDocument, NULL, CLSCTX_SERVER,
		__uuidof(IXMLDOMDocument), (void **)&qDOMInput));
	LoadDOM(qDOMInput, stuInput.Bstr());
//	CheckHr(qDOMInput->put_validateOnParse(VARIANT_FALSE)); // this doesn't work for some reason

//	if (pstbr)
//		pstbr->StepProgressBar();

	// Load the stylesheet document.
	ComSmartPtr<IXMLDOMDocument> qDOMStylesheet;
	CheckHr(::CoCreateInstance(*g_rgMsXslInfo[iXsl].m_clsidFreeDocument, NULL, CLSCTX_SERVER,
		__uuidof(IXMLDOMDocument), (void **)&qDOMStylesheet));
	LoadDOM(qDOMStylesheet, stuStylesheet.Bstr());

//	if (pstbr)
//		pstbr->StepProgressBar();

	// Compile the stylesheet document.
	ComSmartPtr<IXSLTemplate> qTemplate;
	CheckHr(::CoCreateInstance(*g_rgMsXslInfo[iXsl].m_clsidTemplate, NULL, CLSCTX_SERVER,
		__uuidof(IXSLTemplate), (void **)&qTemplate));
	CheckHr(qTemplate->putref_stylesheet(qDOMStylesheet));

	// Execute the stylesheet
	ComSmartPtr<IXSLProcessor> qProcessor;
	CheckHr(qTemplate->createProcessor(&qProcessor));

	// Set processor's input to input IXMLDOMDocument.
	VARIANT vInput;
	V_VT(&vInput) = VT_UNKNOWN;
	V_UNKNOWN(&vInput) = (IUnknown *)qDOMInput;
	CheckHr(qProcessor->put_input(vInput));

	// Set processor's output to file output IStream.
	VARIANT vOut;
	IStreamPtr qstrm;
	FileStream::Create(stuOutput.Chars(), kfstgmWrite | kfstgmCreate, &qstrm);
	V_VT(&vOut) = VT_UNKNOWN;
	V_UNKNOWN(&vOut) = qstrm.Ptr();
	CheckHr(qProcessor->put_output(vOut));

//	if (pstbr)
//		pstbr->StepProgressBar();

	// Execute stylesheet
	VARIANT_BOOL fDone;
	CheckHr(qProcessor->transform(&fDone));

//	if (pstbr)
//		pstbr->StepProgressBar();
}
