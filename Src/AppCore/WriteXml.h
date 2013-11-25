/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2002-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: WriteXml.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Utility functions used in XML import or export which may be useful elsewhere as well.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef WRITEXML_H_INCLUDED
#define WRITEXML_H_INCLUDED

namespace FwXml
{
	void WriteIntTextProp(IStream * pstrm, ILgWritingSystemFactory * pwsf, int tpt, int nVar,
		int nVal);
	void WriteStrTextProp(IStream * pstrm, int tpt, BSTR bstrVal);
	void WriteBulNumFontInfo(IStream * pstrm, BSTR bstrVal, int cchIndent = 0);
	void WriteWsStyles(IStream * pstrm, ILgWritingSystemFactory * pwsf, BSTR bstrCharStyles,
		int cchIndent = 0);

// TODO-Linux: FIXME: same logic different string widths? fix copy/paste
	int DecodeTextToggleVal(const OLECHAR * pszToggle, const OLECHAR ** ppsz);
	int DecodeSuperscriptVal(const OLECHAR * pszSuperscript, const OLECHAR ** ppsz);
	int DecodeTextColor(const OLECHAR * pszColor, const OLECHAR ** ppsz);
	int DecodeUnderlineType(const OLECHAR * pszUnderline, const OLECHAR ** ppsz);
	int DecodeTextAlign(const OLECHAR * pszAlign);
	int DecodeSpellingMode(const OLECHAR * pszSpellingMode, const OLECHAR ** ppsz);

	int DecodeTextToggleVal(const char * pszToggle, const char ** ppsz);
	int DecodeSuperscriptVal(const char * pszSuperscript, const char ** ppsz);
	int DecodeTextColor(const char * pszColor, const char ** ppsz);
	int DecodeUnderlineType(const char * pszUnderline, const char ** ppsz);
	int DecodeTextAlign(const char * pszAlign);
	int DecodeSpellingMode(const char * pszSpellingMode, const char ** ppsz);
};

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkall.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*WRITEXML_H_INCLUDED*/
