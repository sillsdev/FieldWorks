/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TextProps1.cpp
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

Description:
	Implement text property byte strings.  This uses DataReader and DataWriter objects.

	Note that these functions can't go into the generic library because they use the various
	ktpt and kscp constants from TextServ.idh.  This file should be added individually to the
	projects that use it.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


/*----------------------------------------------------------------------------------------------
	Convert the "tpt" value to the corresponding "scp" value.  These must be kept in sync with
	the FwTextPropType and FwTextScalarProp enums in Kernel/TextServ.idh.
----------------------------------------------------------------------------------------------*/
int TextProps::ConvertTptToScp(int tpt)
{
	switch (tpt)
	{
	case ktptWs:				return kscpWs;
	case ktptItalic:			return kscpItalic;
	case ktptBold:				return kscpBold;
	case ktptSuperscript:		return kscpSuperscript;
	case ktptUnderline:			return kscpUnderline;
	case ktptRightToLeft:		return tpt << 2;		// Boolean: lg size = 0.
	case ktptFontSize:			return kscpFontSize;
	case ktptOffset:			return kscpOffset;
	case ktptForeColor:			return kscpForeColor;
	case ktptBackColor:			return kscpBackColor;
	case ktptUnderColor:		return kscpUnderColor;
	case ktptBaseWs:			return kscpBaseWs;
	case ktptAlign:				return kscpAlign;
	case ktptFirstIndent:		return kscpFirstIndent;
	case ktptLeadingIndent:		return kscpLeadingIndent;
	case ktptTrailingIndent:	return kscpTrailingIndent;
	case ktptSpaceBefore:		return kscpSpaceBefore;
	case ktptSpaceAfter:		return kscpSpaceAfter;
	case ktptTabDef:			return kscpTabDef;
	case ktptLineHeight:		return kscpLineHeight;
	case ktptParaColor:			return kscpParaColor;
	case ktptKeepWithNext:		return kscpKeepWithNext;
	case ktptKeepTogether:		return kscpKeepTogether;
	case ktptWidowOrphanControl:return kscpWidowOrphanControl;

	case ktptMarkItem:			return kscpMarkItem;	// This shouldn't appear, but ...
	default:					return (tpt << 2) | 3;	// Assume the worst for data size.
	}
}

/*----------------------------------------------------------------------------------------------
	Read a single integer valued text property.
----------------------------------------------------------------------------------------------*/
void TextProps::ReadTextIntProp(DataReader * pdrdr, TextIntProp * ptxip)
{
	AssertPtr(pdrdr);
	AssertPtr(ptxip);

	ptxip->m_scp = ReadTextPropCode(pdrdr);
	ptxip->m_tpt = ptxip->m_scp >> 2;
	ptxip->m_nVal = 0;
	ptxip->m_nVar = 0;
	switch (CbScpData(ptxip->m_scp))
	{
	case 1:
		pdrdr->ReadBuf(&ptxip->m_nVal, 1);
		switch(ptxip->m_tpt)
		{
			// Set the proper default variation.
		case ktptItalic:		// These are definitely enumerations.
		case ktptBold:
		case ktptSuperscript:
		case ktptUnderline:
		case ktptRightToLeft:
		case ktptKeepWithNext:
		case ktptKeepTogether:
		case ktptWidowOrphanControl:
		default:				// Other one-byte items are likely to be, also.
			ptxip->m_nVar = ktpvEnum;
			break;
		}
		break;
	case 2:
		pdrdr->ReadBuf(&ptxip->m_nVal, 2);
		break;
	case 4:
		pdrdr->ReadBuf(&ptxip->m_nVal, 4);
		switch (ptxip->m_tpt)
		{
		case ktptFontSize:
		case ktptOffset:
		case ktptFirstIndent:
		case ktptLeadingIndent:
		case ktptTrailingIndent:
		case ktptSpaceBefore:
		case ktptSpaceAfter:
		case ktptTabDef:
		case ktptLineHeight:
			ptxip->m_nVar = ptxip->m_nVal & 0xF;
			// TODO SteveMc: do we always want to preserve (extended) sign bits?
			// Does this do that?
			ptxip->m_nVal >>= 4;
			break;
		default:
			break;
		}
		break;
	case 8:
		pdrdr->ReadBuf(&ptxip->m_nVal, 4);
		pdrdr->ReadBuf(&ptxip->m_nVar, 4);
		break;
	default:
		Assert(false);		// THIS SHOULD NEVER HAPPEN!
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Write a single integer valued text property.
----------------------------------------------------------------------------------------------*/
void TextProps::WriteTextIntProp(DataWriter * pdw, TextIntProp * ptxip)
{
	AssertPtr(pdw);
	AssertPtr(ptxip);

	WriteTextPropCode(pdw, ptxip->m_scp);
	int nT;
	short sT;
	byte bT;

	switch (CbScpData(ptxip->m_scp))
	{
	case 1:
		Assert(sizeof(byte) == 1);
		bT = (byte)ptxip->m_nVal;
		pdw->WriteBuf(&bT, 1);
		break;
	case 2:
		Assert(sizeof(short) == 2);
		sT = (short)ptxip->m_nVal;
		pdw->WriteBuf(&sT, 2);
		break;
	case 4:
		Assert(sizeof(int) == 4);
		switch (ptxip->m_scp)
		{
		case kscpFontSize:
		case kscpOffset:
		case kscpFirstIndent:
		case kscpLeadingIndent:
		case kscpTrailingIndent:
		case kscpSpaceBefore:
		case kscpSpaceAfter:
		case kscpTabDef:
		case kscpLineHeight:
			nT = (ptxip->m_nVal << 4) | (ptxip->m_nVar & 0xF);
			break;
		default:
			nT = ptxip->m_nVal;
			break;
		}
		pdw->WriteBuf(&nT, 4);
		break;
	case 8:
		pdw->WriteBuf(&ptxip->m_nVal, 4);
		pdw->WriteBuf(&ptxip->m_nVar, 4);
		break;
	default:
		Assert(false);		// THIS SHOULD NEVER HAPPEN!
		break;
	}
}
