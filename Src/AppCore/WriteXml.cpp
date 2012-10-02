/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WriteXml.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	code for utility functions used in XML export
	This file is designed to be included in the build (makefile) of various compilation units,
	much like a library but at the source level.  It might fit in the AppCore library except
	that several lower level DLLs use these functions.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "Main.h"
#pragma hdrstop

#ifndef WIN32
//#include <gtkmm/messagedialog.h>
//#include <gtk/gtkwidget.h>

#include <Hacks.h> // for sprintf_s
#endif

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Write the given string, which encodes a group of writing-system styles, to WorldPad
	XML format.

	@param pstrm Pointer to an IStream for output.
	@param bstrWsStyles
	@param cchIndent Number of spaces to indent the XML output.

	This code must be kept in sync with FwStyledText::DecodeFontPropsString()!
----------------------------------------------------------------------------------------------*/
void FwXml::WriteWsStyles(IStream * pstrm, ILgWritingSystemFactory * pwsf, BSTR bstrWsStyles,
	int cchIndent)
{
	AssertPtr(pstrm);
	AssertPtr(pwsf);
	Assert(cchIndent >= 0);

	Vector<char> vchIndent;
	vchIndent.Resize(cchIndent + 1);	// new elements are zero initialized.
	memset(vchIndent.Begin(), ' ', cchIndent);
	Vector<WsStyleInfo> vesi;
	Vector<int> vws;
	FwStyledText::DecodeFontPropsString(bstrWsStyles, vesi, vws);
	if (vesi.Size())
	{
		FormatToStream(pstrm, "%s<WsStyles9999>%n", vchIndent.Begin());
		for (int iv = 0; iv < vesi.Size(); ++iv)
		{
			SmartBstr sbstrWs;
			CheckHr(pwsf->GetStrFromWs(vesi[iv].m_ws, &sbstrWs));
			if (!sbstrWs.Length())
			{
				// We don't want WorldPad writing invalid XML!
				// ThrowInternalError(E_INVALIDARG, "Writing system invalid for <WsProp ws>");
#if 99
#if Win32
				::MessageBoxA(NULL, L"Writing system invalid for <WsProp ws>", L"DEBUG!", MB_OK);
#else
				Assert(!L"Writing system invalid for <WsProp ws>, DEBUG!");
				//Gtk::MessageDialog::MessageDialog(NULL, L"Writing system invalid for <WsProp ws>, DEBUG!",
				//	false, MESSAGE_ERROR, BUTTONS_OK, false);
#endif
#endif
				::OutputDebugStringA("Writing system invalid for <WsProp ws>");
				continue;
			}
			FormatToStream(pstrm, "%s%s<WsProp ws=\"",
				vchIndent.Begin(), cchIndent ? "  " : "");
			WriteXmlUnicode(pstrm, sbstrWs.Chars(), sbstrWs.Length());
			FormatToStream(pstrm, "\"");
			if (vesi[iv].m_stuFontFamily.Length())
				FwXml::WriteStrTextProp(pstrm, ktptFontFamily, vesi[iv].m_stuFontFamily.Bstr());
			if (vesi[iv].m_stuFontVar.Length())
				FwXml::WriteStrTextProp(pstrm, ktptFontVariations,
					vesi[iv].m_stuFontVar.Bstr());
			if (vesi[iv].m_mpSize != knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptFontSize, ktpvMilliPoint,
					vesi[iv].m_mpSize);
			if (vesi[iv].m_fBold != knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptBold, ktpvEnum, vesi[iv].m_fBold);
			if (vesi[iv].m_fItalic != knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptItalic, ktpvEnum, vesi[iv].m_fItalic);
			if (vesi[iv].m_ssv != knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptSuperscript, ktpvEnum, vesi[iv].m_ssv);
			if (vesi[iv].m_clrFore != (COLORREF)knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptForeColor, ktpvDefault,
					vesi[iv].m_clrFore);
			if (vesi[iv].m_clrBack != (COLORREF)knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptBackColor, ktpvDefault,
					vesi[iv].m_clrBack);
			if (vesi[iv].m_clrUnder != (COLORREF)knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptUnderColor, ktpvDefault,
					vesi[iv].m_clrUnder);
			if (vesi[iv].m_unt != knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptUnderline, ktpvEnum, vesi[iv].m_unt);
			if (vesi[iv].m_mpOffset != knNinch)
				FwXml::WriteIntTextProp(pstrm, pwsf, ktptOffset, ktpvMilliPoint,
					vesi[iv].m_mpOffset);
			FormatToStream(pstrm, " />%n");
		}
		FormatToStream(pstrm, "%s</WsStyles9999>%n", vchIndent.Begin());
	}
}

/*
 */

/*----------------------------------------------------------------------------------------------
	Interpret the color value as a C character string.  We need to be compatible in hexadecimal
	values with the XHTML standard.  The version 1.0 "strict" DTD has the following information:

	<!-- a color using sRGB: #RRGGBB as Hex values -->
	<!ENTITY % Color "CDATA">
	<!-- There are also 16 widely known color names with their sRGB values:
		White  = #FFFFFF		== white
		Black  = #000000		== black
		Red    = #FF0000		== red
		Lime   = #00FF00		== green
		Blue   = #0000FF		== blue
		Yellow = #FFFF00		== yellow
		Fuchsia= #FF00FF		== magenta
		Aqua   = #00FFFF		== cyan

		Green  = #008000
		Silver = #C0C0C0
		Gray   = #808080
		Maroon = #800000
		Purple = #800080
		Olive  = #808000
		Navy   = #000080
		Teal   = #008080
	-->

	@param nColor
----------------------------------------------------------------------------------------------*/
static const char * ColorName(int nColor)
{
	switch (nColor)
	{
	case kclrWhite:			return "white";
	case kclrBlack:			return "black";
	case kclrRed:			return "red";
	case kclrGreen:			return "green";
	case kclrBlue:			return "blue";
	case kclrYellow:		return "yellow";
	case kclrMagenta:		return "magenta";
	case kclrCyan:			return "cyan";
	case kclrTransparent:	return "transparent";
	}
	static char szColor[10];
#if 1
	int nBlue = (nColor >> 16) & 0xFF;
	int nGreen = (nColor >> 8) & 0xFF;
	int nRed = nColor & 0xFF;
	sprintf_s(szColor, sizeof(szColor), "%02x%02x%02x", nRed, nGreen, nBlue);
#else
	sprintf_s(szColor, sizeof(szColor), "%08x", nColor);
#endif
	return szColor;
}

/*----------------------------------------------------------------------------------------------
	Interpret the FwTextToggleVal enum value as a C character string.

	@param ttv
----------------------------------------------------------------------------------------------*/
static const char * ToggleValueName(byte ttv)
{
	static char szName[16];
	switch (ttv)
	{
	case kttvOff:		return "off";
	case kttvForceOn:		return "on";
	case kttvInvert:	return "invert";
	default:
		sprintf_s(szName, sizeof(szName), "%u", ttv);		// REVIEW SteveMc: what if it's not a valid value?
		return szName;
	}
}

/*----------------------------------------------------------------------------------------------
	Interpret the FwTextPropVar enum value as a C character string.

	@param tpv
----------------------------------------------------------------------------------------------*/
static const char * PropVarName(int tpv)
{
	static char szName[16];
	switch (tpv)
	{
	case ktpvMilliPoint:	return "mpt";
	case ktpvRelative:		return "rel";
	default:
		sprintf_s(szName, sizeof(szName), "%u", tpv);		// REVIEW SteveMc: what if it's not a valid value?
		return szName;
	}
}

/*----------------------------------------------------------------------------------------------
	Interpret the FwSuperscriptVal enum value as a C character string.

	@param ssv
----------------------------------------------------------------------------------------------*/
static const char * SuperscriptValName(byte ssv)
{
	static char szName[16];
	switch (ssv)
	{
	case kssvOff:		return "off";
	case kssvSuper:		return "super";
	case kssvSub:		return "sub";
	default:
		sprintf_s(szName, sizeof(szName), "%u", ssv);		// REVIEW SteveMc: what if it's not a valid value?
		return szName;
	}
}

/*----------------------------------------------------------------------------------------------
	Interpret the FwUnderlineType enum value as a C character string.

	@param unt
----------------------------------------------------------------------------------------------*/
static const char * UnderlineTypeName(byte unt)
{
	static char szName[16];
	switch (unt)
	{
	case kuntNone:		return "none";
	case kuntDotted:	return "dotted";
	case kuntDashed:	return "dashed";
	case kuntStrikethrough:	return "strikethrough";
	case kuntSingle:	return "single";
	case kuntDouble:	return "double";
	case kuntSquiggle:	return "squiggle";
	default:
		sprintf_s(szName, sizeof(szName), "%u", unt);		// REVIEW SteveMc: what if it's not a valid value?
		return szName;
	}
}

/*----------------------------------------------------------------------------------------------
	Interpret the SpellingMode enum value as a C character string.
	Keep consistent with DecodeSpellingMode.

	@param unt
----------------------------------------------------------------------------------------------*/
static const char * SpellingModeName(byte sm)
{
	static char szName[16];
	switch (sm)
	{
	case ksmNormalCheck:	return "normal";
	case ksmDoNotCheck:		return "doNotCheck";
	case ksmForceCheck:		return "forceCheck";
	default:
		sprintf_s(szName, sizeof(szName), "%u", sm);		// REVIEW SteveMc: what if it's not a valid value?
		return szName;
	}
}

/*----------------------------------------------------------------------------------------------
	Interpret the FwTextAlign enum value as a C character string.

	@param tal
----------------------------------------------------------------------------------------------*/
static const char * AlignmentTypeName(byte tal)
{
	static char szName[16];
	switch (tal)
	{
	case ktalLeading:		return "leading";
	case ktalLeft:			return "left";
	case ktalCenter:		return "center";
	case ktalRight:			return "right";
	case ktalTrailing:		return "trailing";
	case ktalJustify:		return "justify";
	default:
		sprintf_s(szName, sizeof(szName), "%u", tal);		// REVIEW SteveMc: what if it's not a valid value?
		return szName;
	}
}

/*----------------------------------------------------------------------------------------------
	Interpret the TptEditable enum value as a C character string.

	@param tpt
----------------------------------------------------------------------------------------------*/
static const char * EditableName(int ted)
{
	static char szName[16];
	switch (ted)
	{
	case ktptNotEditable:	return "not";
	case ktptIsEditable:	return "is";
	case ktptSemiEditable:	return "semi";
	default:
		sprintf_s(szName, sizeof(szName), "%u", ted);		// REVIEW SteveMc: what if it's not a valid value?
		return szName;
	}
}

/*----------------------------------------------------------------------------------------------
	Write an integer-valued text property in XML format.

	@param pstrm Pointer to an IStream for output.
	@param tpt
	@param nVar
	@param nVal
----------------------------------------------------------------------------------------------*/
void FwXml::WriteIntTextProp(IStream * pstrm, ILgWritingSystemFactory * pwsf, int tpt, int nVar,
	int nVal)
{
	AssertPtr(pstrm);
	AssertPtr(pwsf);

	switch (tpt)
	{
	case ktptBackColor:
		FormatToStream(pstrm, " backcolor=\"%s\"", ColorName(nVal));
		break;
	case ktptBold:
		FormatToStream(pstrm, " bold=\"%s\"", ToggleValueName((byte)nVal));
		break;
	case ktptWs:
		if (nVal == 0)
		{
			Assert(nVar == 0);
		}
		else
		{
			SmartBstr sbstrWs;
			CheckHr(pwsf->GetStrFromWs(nVal, &sbstrWs));
			if (!sbstrWs.Length())
				ThrowInternalError(E_INVALIDARG, "Writing system invalid for <Run ws>");
			FormatToStream(pstrm, " ws=\"", nVal);
			WriteXmlUnicode(pstrm, sbstrWs.Chars(), sbstrWs.Length());
			FormatToStream(pstrm, "\"", nVal);
		}
		break;
	case ktptBaseWs:
		if (nVal == 0)
		{
			Assert(nVar == 0);
		}
		else
		{
			SmartBstr sbstrWs;
			CheckHr(pwsf->GetStrFromWs(nVal, &sbstrWs));
			if (!sbstrWs.Length())
				ThrowInternalError(E_INVALIDARG, "Writing system invalid for <Run wsBase>");
			FormatToStream(pstrm, " wsBase=\"", nVal);
			WriteXmlUnicode(pstrm, sbstrWs.Chars(), sbstrWs.Length());
			FormatToStream(pstrm, "\"", nVal);
		}
		break;
	case ktptFontSize:
		FormatToStream(pstrm, " fontsize=\"%d\"", nVal);
		if (nVar != ktpvDefault)
			FormatToStream(pstrm, " fontsizeUnit=\"%s\"", PropVarName(nVar));
		break;
	case ktptForeColor:
		FormatToStream(pstrm, " forecolor=\"%s\"", ColorName(nVal));
		break;
	case ktptItalic:
		FormatToStream(pstrm, " italic=\"%s\"", ToggleValueName((byte)nVal));
		break;
	case ktptOffset:
		FormatToStream(pstrm, " offset=\"%d\"", nVal);
		FormatToStream(pstrm, " offsetUnit=\"%s\"",	PropVarName(nVar));
		break;
	case ktptSuperscript:
		FormatToStream(pstrm, " superscript=\"%s\"", SuperscriptValName((byte)nVal));
		break;
	case ktptUnderColor:
		FormatToStream(pstrm, " undercolor=\"%s\"", ColorName(nVal));
		break;
	case ktptUnderline:
		FormatToStream(pstrm, " underline=\"%s\"", UnderlineTypeName((byte)nVal));
		break;
	case ktptSpellCheck:
		FormatToStream(pstrm, " spellcheck=\"%s\"", SpellingModeName((byte)nVal));
		break;

	//	Paragraph-level properties:

	case ktptAlign:
		FormatToStream(pstrm, " align=\"%s\"", AlignmentTypeName((byte)nVal));
		break;
	case ktptFirstIndent:
		FormatToStream(pstrm, " firstIndent=\"%d\"", nVal);
		break;
	case ktptLeadingIndent:
		FormatToStream(pstrm, " leadingIndent=\"%d\"", nVal);
		break;
	case ktptTrailingIndent:
		FormatToStream(pstrm, " trailingIndent=\"%d\"", nVal);
		break;
	case ktptSpaceBefore:
		FormatToStream(pstrm, " spaceBefore=\"%d\"", nVal);
		break;
	case ktptSpaceAfter:
		FormatToStream(pstrm, " spaceAfter=\"%d\"", nVal);
		break;
	case ktptTabDef:
		FormatToStream(pstrm, " tabDef=\"%d\"", nVal);
		break;
	case ktptLineHeight:
		FormatToStream(pstrm, " lineHeight=\"%d\"", abs(nVal));
		FormatToStream(pstrm, " lineHeightUnit=\"%s\"",	PropVarName(nVar));
		Assert(nVal >= 0 || nVar == ktpvMilliPoint);
		if (nVar == ktpvMilliPoint)
		{
			// negative means "exact" internally.  See FWC-20.
			FormatToStream(pstrm, " lineHeightType=\"%s\"", nVal < 0 ? "exact" : "atLeast");
		}
		break;
	case ktptParaColor:
		FormatToStream(pstrm, " paracolor=\"%s\"", ColorName(nVal));
		break;

	//	Properties from the views subsystem:

	case ktptRightToLeft:
		FormatToStream(pstrm, " rightToLeft=\"%d\"", nVal);
		break;
	case ktptPadLeading:
		FormatToStream(pstrm, " padLeading=\"%d\"", nVal);
		break;
	case ktptPadTrailing:
		FormatToStream(pstrm, " padTrailing=\"%d\"", nVal);
		break;
	// Not the other margins: they are duplicated by FirstIndent etc.
	case ktptMarginTop:
		FormatToStream(pstrm, " MarginTop=\"%d\"", nVal);
		break;
	case ktptPadTop:
		FormatToStream(pstrm, " padTop=\"%d\"", nVal);
		break;
	case ktptPadBottom:
		FormatToStream(pstrm, " padBottom=\"%d\"", nVal);
		break;

	case ktptBorderTop:
		FormatToStream(pstrm, " borderTop=\"%d\"", nVal);
		break;
	case ktptBorderBottom:
		FormatToStream(pstrm, " borderBottom=\"%d\"", nVal);
		break;
	case ktptBorderLeading:
		FormatToStream(pstrm, " borderLeading=\"%d\"", nVal);
		break;
	case ktptBorderTrailing:
		FormatToStream(pstrm, " borderTrailing=\"%d\"", nVal);
		break;
	case ktptBorderColor:
		FormatToStream(pstrm, " borderColor=\"%s\"", ColorName(nVal));
		break;

	case ktptBulNumScheme:
		FormatToStream(pstrm, " bulNumScheme=\"%d\"", nVal);
		break;
	case ktptBulNumStartAt:
		FormatToStream(pstrm, " bulNumStartAt=\"%d\"", nVal == INT_MIN ? 0 : nVal);
		break;

	case ktptDirectionDepth:
		FormatToStream(pstrm, " directionDepth=\"%d\"", nVal);
		break;
	case ktptKeepWithNext:
		FormatToStream(pstrm, " keepWithNext=\"%d\"", nVal);
		break;
	case ktptKeepTogether:
		FormatToStream(pstrm, " keepTogether=\"%d\"", nVal);
		break;
	case ktptHyphenate:
		FormatToStream(pstrm, " hyphenate=\"%d\"", nVal);
		break;
	case ktptWidowOrphanControl:
		FormatToStream(pstrm, " widowOrphan=\"%d\"", nVal);
		break;
	case ktptMaxLines:
		FormatToStream(pstrm, " maxLines=\"%d\"", nVal);
		break;
	case ktptCellBorderWidth:
		FormatToStream(pstrm, " cellBorderWidth=\"%d\"", nVal);
		break;
	case ktptCellSpacing:
		FormatToStream(pstrm, " cellSpacing=\"%d\"", nVal);
		break;
	case ktptCellPadding:
		FormatToStream(pstrm, " cellPadding=\"%d\"", nVal);
		break;
	case ktptEditable:
		FormatToStream(pstrm, " editable=\"%s\"", EditableName(nVal));
		break;
	case ktptSetRowDefaults:
		FormatToStream(pstrm, " setRowDefaults=\"%d\"", nVal);
		break;
	case ktptRelLineHeight:
		FormatToStream(pstrm, " relLineHeight=\"%d\"", nVal);
		break;
	case ktptTableRule:
		// Ignore this view-only property.
		break;
	default:
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Write a string-valued text property in XML format.

	@param pstrm Pointer to an IStream for output.
	@param tpt
	@param bstrVal
----------------------------------------------------------------------------------------------*/
void FwXml::WriteStrTextProp(IStream * pstrm, int tpt, BSTR bstrVal)
{
	AssertPtr(pstrm);

	int cch = ::SysStringLen(bstrVal);
	switch (tpt)
	{
	case ktptCharStyle:
		FormatToStream(pstrm, " charStyle=\"");
		WriteXmlUnicode(pstrm, bstrVal, cch);
		FormatToStream(pstrm, "\"");
		break;
	case ktptFontFamily:
		FormatToStream(pstrm, " fontFamily=\"");
		WriteXmlUnicode(pstrm, bstrVal, cch);
		FormatToStream(pstrm, "\"");
		break;

	case ktptObjData:
		if (cch >= 1)
		{
			switch (bstrVal[0])
			{
			case kodtPictEvenHot:
			case kodtPictOddHot:
				// type="chars" is the default value for this attribute.
				// The caller is responsible for writing out the picture data.  (This is an
				// antique kludge that isn't really used in practice, but some of our test data
				// still exercises it.)
				FormatToStream(pstrm, " type=\"picture\"");
				break;

			case kodtNameGuidHot:
				Assert((cch - 1) * isizeof(OLECHAR) == isizeof(GUID));
				{
					const GUID * pguid = reinterpret_cast<const GUID *>(bstrVal + 1);
					FormatToStream(pstrm, " link=\"%g\"", pguid);
				}
				break;

			case kodtExternalPathName:
				FormatToStream(pstrm, " externalLink=\"");
				WriteXmlUnicode(pstrm, bstrVal + 1, cch - 1);
				FormatToStream(pstrm, "\"");
				break;

			case kodtOwnNameGuidHot:
				Assert((cch - 1) * isizeof(OLECHAR) == isizeof(GUID));
				{
					const GUID * pguid = reinterpret_cast<const GUID *>(bstrVal + 1);
					FormatToStream(pstrm, " ownlink=\"%g\"", pguid);
				}
				break;

			case kodtEmbeddedObjectData:
				// This is only used for copying to the clipboard
				// We assume that the buffer contains valid XML and that the receiving code
				// knows what to do with it!
				FormatToStream(pstrm, " embedded=\"");
				WriteXmlUnicode(pstrm, bstrVal + 1, cch - 1);
				FormatToStream(pstrm, "\"");
				break;

			case kodtContextString:
				// This is a generated context-sensitive string.  The next 8 characters give a
				// GUID, which is from to a known set of GUIDs that have special meaning to a
				// view contructor.
				Assert((cch - 1) * isizeof(OLECHAR) == isizeof(GUID));
				{
					const GUID * pguid = reinterpret_cast<const GUID *>(bstrVal + 1);
					FormatToStream(pstrm, " contextString=\"%g\"", pguid);
				}
				break;

			case kodtGuidMoveableObjDisp:
				// This results in a call-back to the VC, with a new VwEnv, to create any
				// display it wants of the object specified by the Guid (see
				// IVwViewConstructor.DisplayEmbeddedObject).  The display will typically
				// occur immediately following the paragraph line that contains the ORC,
				// which functions as an anchor, but may be moved down past following text
				// to improve page breaking.
				Assert((cch - 1) * isizeof(OLECHAR) == isizeof(GUID));
				{
					const GUID * pguid = reinterpret_cast<const GUID *>(bstrVal + 1);
					FormatToStream(pstrm, " moveableObj=\"%g\"", pguid);
				}
				break;

			default:
				// This forces an assert when a new enum value is added and used.
				Assert(bstrVal[0] == kodtExternalPathName);
				break;
			}
		}
		break;

	//	Properties from the views subsystem:

	case ktptNamedStyle:
		FormatToStream(pstrm, " namedStyle=\"");
		WriteXmlUnicode(pstrm, bstrVal, cch);
		FormatToStream(pstrm, "\"");
		break;
	case ktptBulNumTxtBef:
		FormatToStream(pstrm, " bulNumTxtBef=\"");
		WriteXmlUnicode(pstrm, bstrVal, cch);
		FormatToStream(pstrm, "\"");
		break;
	case ktptBulNumTxtAft:
		FormatToStream(pstrm, " bulNumTxtAft=\"");
		WriteXmlUnicode(pstrm, bstrVal, cch);
		FormatToStream(pstrm, "\"");
		break;
	case ktptBulNumFontInfo:
		// BulNumFontInfo is written separately.
		break;
	case ktptWsStyle:
		//	WsStyles are written separately
		break;
	case ktptFontVariations:		// string, giving variation names specific to font.
		FormatToStream(pstrm, " fontVariations=\"");
		WriteXmlUnicode(pstrm, bstrVal, cch);
		FormatToStream(pstrm, "\"");
		break;
	case ktptParaStyle:
		FormatToStream(pstrm, " paraStyle=\"");
		WriteXmlUnicode(pstrm, bstrVal, cch);
		FormatToStream(pstrm, "\"");
		break;
	case ktptTabList:
		FormatToStream(pstrm, " tabList=\"");
		WriteXmlUnicode(pstrm, bstrVal, cch);
		FormatToStream(pstrm, "\"");
		break;
	case ktptTags:
		// prop holds sequence of 8-char items, each the memcpy-equivalent of a GUID
		{
			const GUID * prgguid = reinterpret_cast<const GUID *>(bstrVal);
			int cguid = (cch * isizeof(OLECHAR)) / isizeof(GUID);
			FormatToStream(pstrm, " tags=\"");
			for (int iguid = 0; iguid < cguid; ++iguid)
			{
				if (iguid)
					FormatToStream(pstrm, " ");
				FormatToStream(pstrm, "%g", prgguid + iguid);
			}
			FormatToStream(pstrm, "\"");
		}
		break;
/*
 ktptFieldName:	// Fake string valued text property used for exporting.
*/
	default:
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Write the given FontInfo string text property value in readable XML format.

	@param pstrm Pointer to an IStream for output.
	@param bstrVal
----------------------------------------------------------------------------------------------*/
void FwXml::WriteBulNumFontInfo(IStream * pstrm, BSTR bstrVal, int cchIndent)
{
	AssertPtr(pstrm);
	Assert(cchIndent >= 0);
	int cchProps = ::SysStringLen(bstrVal);
	if (!cchProps)
		return;

	Vector<char> vchIndent;
	vchIndent.Resize(cchIndent + 1);
	memset(vchIndent.Begin(), ' ', cchIndent);

	const OLECHAR * pchProps = bstrVal;
	const OLECHAR * pchPropsLim = pchProps + cchProps;

	FormatToStream(pstrm, "%s<BulNumFontInfo", vchIndent.Begin());
	StrAnsi staItalic;
	StrAnsi staBold;
	StrAnsi staSuperscript;
	StrAnsi staUnderline;
	StrAnsi staFontsize;
	StrAnsi staOffset;
	StrAnsi staForecolor;
	StrAnsi staBackcolor;
	StrAnsi staUndercolor;
	StrAnsi staXXX;
	int tpt;
	while (pchProps < pchPropsLim)
	{
		tpt = *pchProps++;
		if (tpt == ktptFontFamily)
			break;

		int nVal = *pchProps + ((*(pchProps + 1)) << 16);
		pchProps += 2;
		switch (tpt)
		{
		case ktptItalic:
			staItalic.Format(" italic=\"%s\"", ToggleValueName((byte)nVal));
			break;
		case ktptBold:
			staBold.Format(" bold=\"%s\"", ToggleValueName((byte)nVal));
			break;
		case ktptSuperscript:
			staSuperscript.Format(" superscript=\"%s\"", SuperscriptValName((byte)nVal));
			break;
		case ktptUnderline:
			staUnderline.Format(" underline=\"%s\"", UnderlineTypeName((byte)nVal));
			break;
		case ktptFontSize:
			staFontsize.Format(" fontsize=\"%dmpt\"", nVal);
			break;
		case ktptOffset:
			staOffset.Format(" offset=\"%dmpt\"", nVal);
			break;
		case ktptForeColor:
			staForecolor.Format(" forecolor=\"%s\"", ColorName(nVal));
			break;
		case ktptBackColor:
			staBackcolor.Format(" backcolor=\"%s\"", ColorName(nVal));
			break;
		case ktptUnderColor:
			staUndercolor.Format(" undercolor=\"%s\"", ColorName(nVal));
			break;
		default:
			staXXX.FormatAppend(" prop_%u=\"%d\"", tpt, nVal);
			break;
		}
	}
	UCOMINT32 cb;
	// Write the integer valued properties in alphabetical order.
	if (staBackcolor.Length())
		pstrm->Write(staBackcolor.Chars(), staBackcolor.Length(), &cb);
	if (staBold.Length())
		pstrm->Write(staBold.Chars(), staBold.Length(), &cb);
	if (staFontsize.Length())
		pstrm->Write(staFontsize.Chars(), staFontsize.Length(), &cb);
	if (staForecolor.Length())
		pstrm->Write(staForecolor.Chars(), staForecolor.Length(), &cb);
	if (staItalic.Length())
		pstrm->Write(staItalic.Chars(), staItalic.Length(), &cb);
	if (staOffset.Length())
		pstrm->Write(staOffset.Chars(), staOffset.Length(), &cb);
	if (staSuperscript.Length())
		pstrm->Write(staSuperscript.Chars(), staSuperscript.Length(), &cb);
	if (staUndercolor.Length())
		pstrm->Write(staUndercolor.Chars(), staUndercolor.Length(), &cb);
	if (staUnderline.Length())
		pstrm->Write(staUnderline.Chars(), staUnderline.Length(), &cb);
	if (staXXX.Length())
		pstrm->Write(staXXX.Chars(), staXXX.Length(), &cb);
	// Write the string valued property (if it exists).
	if (tpt == ktptFontFamily && pchProps < pchPropsLim)
	{
		FormatToStream(pstrm, " fontFamily=\"");
		WriteXmlUnicode(pstrm, pchProps, pchPropsLim - pchProps);
		FormatToStream(pstrm, "\"");
	}
	FormatToStream(pstrm, "/>%n");
}


/*----------------------------------------------------------------------------------------------
	Decode the toggle enum value represented by the input string, which must either be a
	standard term, or a decimal number.

	@param pszToggle String containing a toggle enum value, either a name or a decimal value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The toggle value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeTextToggleVal(const char * pszToggle, const char ** ppsz)
{
	AssertPtr(pszToggle);
	AssertPtrN(ppsz);

	if (!_strnicmp(pszToggle, "off", 3))
	{
		if (ppsz)
			*ppsz = pszToggle + 3;
		return kttvOff;
	}
	else if (!_strnicmp(pszToggle, "on", 2))
	{
		if (ppsz)
			*ppsz = pszToggle + 2;
		return kttvForceOn;
	}
	else if (!_strnicmp(pszToggle, "invert", 6))
	{
		if (ppsz)
			*ppsz = pszToggle + 6;
		return kttvInvert;
	}
	else
	{
		return (int)strtol(pszToggle, const_cast<char **>(ppsz), 10);
	}
}

/*----------------------------------------------------------------------------------------------
	Decode the toggle enum value represented by the input string, which must either be a
	standard term, or a decimal number.

	@param pszToggle String containing a toggle enum value, either a name or a decimal value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The toggle value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeTextToggleVal(const OLECHAR * pszToggle, const OLECHAR ** ppsz)
{
	AssertPtr(pszToggle);
	AssertPtrN(ppsz);

	static const OleStringLiteral off(L"off");
	static const OleStringLiteral on(L"on");
	static const OleStringLiteral invert(L"invert");

	if (!_wcsnicmp(pszToggle, off, 3))
	{
		if (ppsz)
			*ppsz = pszToggle + 3;
		return kttvOff;
	}
	else if (!_wcsnicmp(pszToggle, on, 2))
	{
		if (ppsz)
			*ppsz = pszToggle + 2;
		return kttvForceOn;
	}
	else if (!_wcsnicmp(pszToggle, invert, 6))
	{
		if (ppsz)
			*ppsz = pszToggle + 6;
		return kttvInvert;
	}
	else
	{
		return (int)Utf16StrToL(pszToggle, ppsz, 10);
	}
}

// TODO-Linux: FIXME: same logic different string widths? fix copy/paste
/*----------------------------------------------------------------------------------------------
	Decode the color value represented by the input string, which must either be a standard
	English color name, or a 6/8-digit hexadecimal number.

	@param pszColor String containing a color value, either a name or a hexadecimal (RGB?)
					value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The color value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeTextColor(const OLECHAR * pszColor, const OLECHAR ** ppsz)
{
	AssertPtr(pszColor);
	AssertPtrN(ppsz);

	static const OleStringLiteral white(L"white");
	static const OleStringLiteral black(L"black");
	static const OleStringLiteral red(L"red");
	static const OleStringLiteral green(L"green");
	static const OleStringLiteral blue(L"blue");
	static const OleStringLiteral yellow(L"yellow");
	static const OleStringLiteral magenta(L"magenta");
	static const OleStringLiteral cyan(L"cyan");
	static const OleStringLiteral transparent(L"transparent");

	if (!_wcsnicmp(pszColor, white, 5))
	{
		if (ppsz)
			*ppsz = pszColor + 5;
		return kclrWhite;
	}
	else if (!_wcsnicmp(pszColor, black, 5))
	{
		if (ppsz)
			*ppsz = pszColor + 5;
		return kclrBlack;
	}
	else if (!_wcsnicmp(pszColor, red, 3))
	{
		if (ppsz)
			*ppsz = pszColor + 3;
		return kclrRed;
	}
	else if (!_wcsnicmp(pszColor, green, 5))
	{
		if (ppsz)
			*ppsz = pszColor + 5;
		return kclrGreen;
	}
	else if (!_wcsnicmp(pszColor, blue, 4))
	{
		if (ppsz)
			*ppsz = pszColor + 4;
		return kclrBlue;
	}
	else if (!_wcsnicmp(pszColor, yellow, 6))
	{
		if (ppsz)
			*ppsz = pszColor + 6;
		return kclrYellow;
	}
	else if (!_wcsnicmp(pszColor, magenta, 7))
	{
		if (ppsz)
			*ppsz = pszColor + 7;
		return kclrMagenta;
	}
	else if (!_wcsnicmp(pszColor, cyan, 4))
	{
		if (ppsz)
			*ppsz = pszColor + 4;
		return kclrCyan;
	}
	else if (!_wcsnicmp(pszColor, transparent, 11))
	{
		if (ppsz)
			*ppsz = pszColor + 11;
		return kclrTransparent;
	}
	else
	{
#if 1
		StrAnsi digits("0123456789ABCDEFabcdef");
		StrAnsi color(pszColor);
		if (strspn(color, digits) == 6)
		{
			// Interpret the hexadecimal value as "RRGGBB" (compatible with XHTML).
			unsigned nBigEndian = Utf16StrToL(pszColor, ppsz, 16);
			unsigned nBlue = (nBigEndian & 0xFF) << 16;
			unsigned nGreen = (nBigEndian & 0xFF00);
			unsigned nRed = (nBigEndian & 0xFF0000) >> 16;
			return nBlue | nGreen | nRed;
		}
		else
		{
			// Maintain compatibility with our old hexadecimal format, which was zero-padded to
			// 8 digits.
			Assert(strspn(color, digits) == 8);
			Assert(pszColor[0] == '0' && pszColor[1] == '0');
			return Utf16StrToL(pszColor, ppsz, 16);
		}
#else
		return Utf16StrToL(pszColor, ppsz, 16);
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Decode the color value represented by the input string, which must either be a standard
	English color name, or a 6/8-digit hexadecimal number.

	@param pszColor String containing a color value, either a name or a hexadecimal (RGB?)
					value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The color value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeTextColor(const char * pszColor, const char ** ppsz)
{
	AssertPtr(pszColor);
	AssertPtrN(ppsz);

	if (!_strnicmp(pszColor, "white", 5))
	{
		if (ppsz)
			*ppsz = pszColor + 5;
		return kclrWhite;
	}
	else if (!_strnicmp(pszColor, "black", 5))
	{
		if (ppsz)
			*ppsz = pszColor + 5;
		return kclrBlack;
	}
	else if (!_strnicmp(pszColor, "red", 3))
	{
		if (ppsz)
			*ppsz = pszColor + 3;
		return kclrRed;
	}
	else if (!_strnicmp(pszColor, "green", 5))
	{
		if (ppsz)
			*ppsz = pszColor + 5;
		return kclrGreen;
	}
	else if (!_strnicmp(pszColor, "blue", 4))
	{
		if (ppsz)
			*ppsz = pszColor + 4;
		return kclrBlue;
	}
	else if (!_strnicmp(pszColor, "yellow", 6))
	{
		if (ppsz)
			*ppsz = pszColor + 6;
		return kclrYellow;
	}
	else if (!_strnicmp(pszColor, "magenta", 7))
	{
		if (ppsz)
			*ppsz = pszColor + 7;
		return kclrMagenta;
	}
	else if (!_strnicmp(pszColor, "cyan", 4))
	{
		if (ppsz)
			*ppsz = pszColor + 4;
		return kclrCyan;
	}
	else if (!_strnicmp(pszColor, "transparent", 11))
	{
		if (ppsz)
			*ppsz = pszColor + 11;
		return kclrTransparent;
	}
	else
	{
#if 1
		if (strspn(pszColor, "0123456789ABCDEFabcdef") == 6)
		{
			// Interpret the hexadecimal value as "RRGGBB" (compatible with XHTML).
			unsigned nBigEndian = strtoul(pszColor, const_cast<char **>(ppsz), 16);
			unsigned nBlue = (nBigEndian & 0xFF) << 16;
			unsigned nGreen = (nBigEndian & 0xFF00);
			unsigned nRed = (nBigEndian & 0xFF0000) >> 16;
			return nBlue | nGreen | nRed;
		}
		else
		{
			// Maintain compatibility with our old hexadecimal format, which was zero-padded to
			// 8 digits.
			Assert(strspn(pszColor, "0123456789ABCDEFabcdef") == 8);
			Assert(pszColor[0] == '0' && pszColor[1] == '0');
			return strtoul(pszColor, const_cast<char **>(ppsz), 16);
		}
#else
		return strtoul(pszColor, const_cast<char **>(ppsz), 16);
#endif
	}
}

// TODO-Linux: FIXME: same logic different string widths? fix copy/paste
/*----------------------------------------------------------------------------------------------
	Decode the superscript value represented by the input string, which must either be a
	standard English superscript type name, or a decimal number.

	@param pszSuperscript String containing a superscript type, either a name or a decimal
							value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The superscript type value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeSuperscriptVal(const OLECHAR * pszSuperscript, const OLECHAR ** ppsz)
{
	AssertPtr(pszSuperscript);
	AssertPtrN(ppsz);

	static const OleStringLiteral off(L"off");
	static const OleStringLiteral super(L"super");
	static const OleStringLiteral sub(L"sub");

	if (!_wcsnicmp(pszSuperscript, off, 3))
	{
		if (ppsz)
			*ppsz = pszSuperscript + 3;
		return kssvOff;
	}
	else if (!_wcsnicmp(pszSuperscript, super, 5))
	{
		if (ppsz)
			*ppsz = pszSuperscript + 5;
		return kssvSuper;
	}
	else if (!_wcsnicmp(pszSuperscript, sub, 3))
	{
		if (ppsz)
			*ppsz = pszSuperscript + 3;
		return kssvSub;
	}
	else
	{
		return (int)Utf16StrToL(pszSuperscript, ppsz, 10);
	}
}
/*----------------------------------------------------------------------------------------------
	Decode the superscript value represented by the input string, which must either be a
	standard English superscript type name, or a decimal number.

	@param pszSuperscript String containing a superscript type, either a name or a decimal
							value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The superscript type value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeSuperscriptVal(const char * pszSuperscript, const char ** ppsz)
{
	AssertPtr(pszSuperscript);
	AssertPtrN(ppsz);

	if (!_strnicmp(pszSuperscript, "off", 3))
	{
		if (ppsz)
			*ppsz = pszSuperscript + 3;
		return kssvOff;
	}
	else if (!_strnicmp(pszSuperscript, "super", 5))
	{
		if (ppsz)
			*ppsz = pszSuperscript + 5;
		return kssvSuper;
	}
	else if (!_strnicmp(pszSuperscript, "sub", 3))
	{
		if (ppsz)
			*ppsz = pszSuperscript + 3;
		return kssvSub;
	}
	else
	{
		return (int)strtol(pszSuperscript, const_cast<char **>(ppsz), 10);
	}
}

// TODO-Linux: FIXME: same logic different string widths? fix copy/paste
/*----------------------------------------------------------------------------------------------
	Decode the underline value represented by the input string, which must either be a standard
	English underline type name, or a decimal number.

	@param pszUnderline String containing an underline type, either a name or a decimal value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The underline type value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeUnderlineType(const OLECHAR * pszUnderline, const OLECHAR ** ppsz)
{
	AssertPtr(pszUnderline);
	AssertPtrN(ppsz);

	static const OleStringLiteral none(L"none");
	static const OleStringLiteral single(L"single");
	static const OleStringLiteral _double(L"double");
	static const OleStringLiteral dotted(L"dotted");
	static const OleStringLiteral dashed(L"dashed");
	static const OleStringLiteral strikethrough(L"strikethrough");
	static const OleStringLiteral squiggle(L"squiggle");

	if (!_wcsnicmp(pszUnderline, none, 4))
	{
		if (ppsz)
			*ppsz = pszUnderline + 4;
		return kuntNone;
	}
	else if (!_wcsnicmp(pszUnderline, single, 6))
	{
		if (ppsz)
			*ppsz = pszUnderline + 6;
		return kuntSingle;
	}
	else if (!_wcsnicmp(pszUnderline, _double, 6))
	{
		if (ppsz)
			*ppsz = pszUnderline + 6;
		return kuntDouble;
	}
	else if (!_wcsnicmp(pszUnderline, dotted, 6))
	{
		if (ppsz)
			*ppsz = pszUnderline + 6;
		return kuntDotted;
	}
	else if (!_wcsnicmp(pszUnderline, dashed, 6))
	{
		if (ppsz)
			*ppsz = pszUnderline + 6;
		return kuntDashed;
	}
	else if (!_wcsnicmp(pszUnderline, strikethrough, 13))
	{
		if (ppsz)
			*ppsz = pszUnderline + 13;
		return kuntStrikethrough;
	}
	else if (!_wcsnicmp(pszUnderline, squiggle, 8))
	{
		if (ppsz)
			*ppsz = pszUnderline + 8;
		return kuntSquiggle;
	}
	else
	{
		return (int)Utf16StrToL(pszUnderline, ppsz, 10);
	}
}

/*----------------------------------------------------------------------------------------------
	Decode the underline value represented by the input string, which must either be a standard
	English underline type name, or a decimal number.

	@param pszUnderline String containing an underline type, either a name or a decimal value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The underline type value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeUnderlineType(const char * pszUnderline, const char ** ppsz)
{
	AssertPtr(pszUnderline);
	AssertPtrN(ppsz);

	if (!_strnicmp(pszUnderline, "none", 4))
	{
		if (ppsz)
			*ppsz = pszUnderline + 4;
		return kuntNone;
	}
	else if (!_strnicmp(pszUnderline, "single", 6))
	{
		if (ppsz)
			*ppsz = pszUnderline + 6;
		return kuntSingle;
	}
	else if (!_strnicmp(pszUnderline, "double", 6))
	{
		if (ppsz)
			*ppsz = pszUnderline + 6;
		return kuntDouble;
	}
	else if (!_strnicmp(pszUnderline, "dotted", 6))
	{
		if (ppsz)
			*ppsz = pszUnderline + 6;
		return kuntDotted;
	}
	else if (!_strnicmp(pszUnderline, "dashed", 6))
	{
		if (ppsz)
			*ppsz = pszUnderline + 6;
		return kuntDashed;
	}
	else if (!_strnicmp(pszUnderline, "strikethrough", 13))
	{
		if (ppsz)
			*ppsz = pszUnderline + 13;
		return kuntStrikethrough;
	}
	else if (!_strnicmp(pszUnderline, "squiggle", 8))
	{
		if (ppsz)
			*ppsz = pszUnderline + 8;
		return kuntSquiggle;
	}
	else
	{
		return (int)strtol(pszUnderline, const_cast<char **>(ppsz), 10);
	}
}

// TODO-Linux: FIXME: same logic different string widths? fix copy/paste
/*----------------------------------------------------------------------------------------------
	Decode the underline value represented by the input string, which must either be a standard
	English underline type name, or a decimal number.

	@param pszUnderline String containing an underline type, either a name or a decimal value.
	@param ppsz Address of pointer to the end of the comparison or computation.

	@return The underline type value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeSpellingMode(const OLECHAR * pszSpellMode, const OLECHAR ** ppsz)
{
	AssertPtr(pszSpellMode);
	AssertPtrN(ppsz);

	static const OleStringLiteral normal(L"normal");
	static const OleStringLiteral doNotCheck(L"doNotCheck");
	static const OleStringLiteral forceCheck(L"forceCheck");

	if (!_wcsnicmp(pszSpellMode, normal, 6))
	{
		if (ppsz)
			*ppsz = pszSpellMode + 6;
		return ksmNormalCheck;
	}
	else if (!_wcsnicmp(pszSpellMode, doNotCheck, 10))
	{
		if (ppsz)
			*ppsz = pszSpellMode + 10;
		return ksmDoNotCheck;
	}
	else if (!_wcsnicmp(pszSpellMode, forceCheck, 10))
	{
		if (ppsz)
			*ppsz = pszSpellMode + 10;
		return ksmForceCheck;
	}
	else
	{
		// JohnT: copied from DecodeUnderline, but shouldn't ever happen, unless reading a newer file.
		return (int)Utf16StrToL(pszSpellMode, ppsz, 10);
	}
}
int FwXml::DecodeSpellingMode(const char * pszSpellMode, const char ** ppsz)
{
	AssertPtr(pszSpellMode);
	AssertPtrN(ppsz);

	if (!_strnicmp(pszSpellMode, "normal", 6))
	{
		if (ppsz)
			*ppsz = pszSpellMode + 6;
		return ksmNormalCheck;
	}
	else if (!_strnicmp(pszSpellMode, "doNotCheck", 10))
	{
		if (ppsz)
			*ppsz = pszSpellMode + 10;
		return ksmDoNotCheck;
	}
	else if (!_strnicmp(pszSpellMode, "forceCheck", 10))
	{
		if (ppsz)
			*ppsz = pszSpellMode + 10;
		return ksmForceCheck;
	}
	else
	{
		// JohnT: copied from DecodeUnderline, but shouldn't ever happen, unless reading a newer file.
		return (int)strtol(pszSpellMode, const_cast<char **>(ppsz), 10);
	}
}

// TODO-Linux: FIXME: same logic different string widths? fix copy/paste
/*----------------------------------------------------------------------------------------------
	Decode the text alignment value represented by the input string, which must either be a
	standard English alignment type name, or a decimal number.

	@param pszAlign String containing an alignment type, either a name or a decimal value.

	@return The alignment type value represented by the input string.
----------------------------------------------------------------------------------------------*/
int FwXml::DecodeTextAlign(const OLECHAR * pszAlign)
{
	AssertPtr(pszAlign);

	static const OleStringLiteral leading(L"leading");
	static const OleStringLiteral left(L"left");
	static const OleStringLiteral center(L"center");
	static const OleStringLiteral right(L"right");
	static const OleStringLiteral trailing(L"trailing");
	static const OleStringLiteral justify(L"justify");


	if (!_wcsicmp(pszAlign, leading))
		return ktalLeading;
	else if (!_wcsicmp(pszAlign, left))
		return ktalLeft;
	else if (!_wcsicmp(pszAlign, center))
		return ktalCenter;
	else if (!_wcsicmp(pszAlign, right))
		return ktalRight;
	else if (!_wcsicmp(pszAlign, trailing))
		return ktalTrailing;
	else if (!_wcsicmp(pszAlign, justify))
		return ktalJustify;

	const OLECHAR * psz;
	int nVal = (int)Utf16StrToL(pszAlign, &psz, 10);
	if (*psz)
		nVal = ktalMin - 1;		// Signal an error!
	return nVal;
}
int FwXml::DecodeTextAlign(const char * pszAlign)
{
	AssertPtr(pszAlign);

	if (!_stricmp(pszAlign, "leading"))
		return ktalLeading;
	else if (!_stricmp(pszAlign, "left"))
		return ktalLeft;
	else if (!_stricmp(pszAlign, "center"))
		return ktalCenter;
	else if (!_stricmp(pszAlign, "right"))
		return ktalRight;
	else if (!_stricmp(pszAlign, "trailing"))
		return ktalTrailing;
	else if (!_stricmp(pszAlign, "justify"))
		return ktalJustify;

	char * psz;
	int nVal = (int)strtol(pszAlign, &psz, 10);
	if (*psz)
		nVal = ktalMin - 1;		// Signal an error!
	return nVal;
}

#include "Vector_i.cpp"	// Needed for Release build.
template class Vector<char>;	// Needed for Release build.

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkall.bat"
// End: (These 4 lines are useful to Steve McConnel.)
