/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpStylesheet.cpp
Responsibility: Sharon Correll
Last reviewed: never

Description:
	This class provides an implementation of IVwStylesheet for WorldPad, which rather than
	storing the styles in the database, instead writes them to an XML file.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Create a new object for the style, owned in the (fake) styles attribute of StText.
	Return the new HVO.
----------------------------------------------------------------------------------------------*/
HRESULT WpStylesheet::GetNewStyleHVO(HVO * phvoNewStyle)
{
	int cst;
	CheckHr(m_qsda->get_VecSize(khvoText, kflidStText_Styles, &cst));

	*phvoNewStyle = m_hvoNextStyle++;
	WpDaPtr wpda = dynamic_cast<WpDa *>(m_qsda.Ptr());
	HRESULT hr = wpda->CacheReplace(khvoText, kflidStText_Styles, cst, cst, phvoNewStyle, 1);
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Initialize the stylesheet for a new window.
----------------------------------------------------------------------------------------------*/
void WpStylesheet::Init(ISilDataAccess * psda)
{
	m_qsda = psda;
	m_hvoNextStyle = khvoStyleMin;

	AddNormalParaStyle();
}

/*----------------------------------------------------------------------------------------------
	Create a paragraph style called "Normal". For now just use a minimal (ie, empty)
	set of text properties.
----------------------------------------------------------------------------------------------*/
void WpStylesheet::AddNormalParaStyle()
{
	WpDaPtr wpda = dynamic_cast<WpDa *>(m_qsda.Ptr());
	Assert(wpda);

	HVO hvoNormal;
	CheckHr(GetNewStyleHVO(&hvoNormal));
	ITsTextPropsPtr qttp;
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->GetTextProps(&qttp));
	// Get a non-const version of the "Normal" string:
	// REVIEW: Should the PutStyle method be made to accept const strings?
	SmartBstr sbstrName(g_pszwStyleNormal);
	SmartBstr sbstrUsage(g_pszwStyleNormal);
	CheckHr(PutStyle(sbstrName, sbstrUsage, hvoNormal, 0, 0, kstParagraph, true,
		false, qttp));
	CheckHr(wpda->CacheObjProp(hvoNormal, kflidStStyle_Next, hvoNormal));
}

/*----------------------------------------------------------------------------------------------
	Set the list of style objects to the (fake) styles attribute of StText.
	Called from the XML import routine.
----------------------------------------------------------------------------------------------*/
HRESULT WpStylesheet::AddLoadedStyles(HVO * rghvoNew, int chvo, int hvoNextStyle)
{
	int cst;
	CheckHr(m_qsda->get_VecSize(khvoText, kflidStText_Styles, &cst));
	WpDaPtr wpda = dynamic_cast<WpDa *>(m_qsda.Ptr());
	// Note: we are clobbering any existing styles.
	HRESULT hr = wpda->CacheReplace(khvoText, kflidStText_Styles, 0, cst, rghvoNew, chvo);

	m_vhcStyles.Clear();
	bool fFoundNormal= false;
	for (int ihvo = 0; ihvo < chvo; ihvo++)
	{
		HvoClsid hc;
		hc.clsid = kclidStStyle;
		hc.hvo = rghvoNew[ihvo];
		m_vhcStyles.Push(hc);

		SmartBstr sbstrName;
		CheckHr(m_qsda->get_UnicodeProp(hc.hvo, kflidStStyle_Name, &sbstrName));
		StrUni stuName(sbstrName.Chars());
		if (stuName == g_pszwStyleNormal)
			fFoundNormal = true;
	}

	m_hvoNextStyle = max(m_hvoNextStyle, hvoNextStyle);

	if (!fFoundNormal)
		AddNormalParaStyle();

#if 0
	// Old code for deleting only duplicates.

	int cstNew;
	CheckHr(m_qsda->get_VecSize(khvoText, kflidStText_Styles, &cstNew));
	Assert(cstNew == cst + chvo);

	//	If there are any styles with duplicate names, delete the first
	//	(previously existing) one.

	Vector<int> vihvoDelete;
	Vector<StrUni> vstuStyleNames;
	for (ihvo = 0; ihvo < m_vhcStyles.Size(); ihvo++)
	{
		SmartBstr sbstrName;
		CheckHr(m_qsda->get_UnicodeProp(m_vhcStyles[ihvo].hvo, kflidStStyle_Name, &sbstrName));
		StrUni stu(sbstrName.Chars());
		vstuStyleNames.Push(stu);
	}
	Assert(vstuStyleNames.Size() == m_vhcStyles.Size());

	for (ihvo = 0; ihvo < vstuStyleNames.Size() - 1; ihvo++)
	{
		for (int ihvo2 = ihvo + 1; ihvo2 < vstuStyleNames.Size(); ihvo2++)
		{
			if (vstuStyleNames[ihvo] == vstuStyleNames[ihvo2])
			{
				vihvoDelete.Push(ihvo);
				break;
			}
		}
	}

	//	Delete the duplicates; do it backwards so the indices stay valid.
	for (int iihvo = vihvoDelete.Size(); --iihvo >= 0; )
	{
		HVO ihvoToDelete = vihvoDelete[iihvo];
		m_vhcStyles.Delete(ihvoToDelete);
		CheckHr(wpda->CacheReplace(khvoText, kflidStText_Styles, ihvoToDelete, ihvoToDelete+1,
			NULL, 0));
	}

	CheckHr(m_qsda->get_VecSize(khvoText, kflidStText_Styles, &cstNew));
	Assert(cstNew == cst + chvo - vihvoDelete.Size());

#endif

	return hr;
}

/*----------------------------------------------------------------------------------------------
	Fix up the 'next' or 'basedOn' attributes in the styles, now that we have a complete
	list.
	Attribute:
		flid		- attribute to set
		hmhvostu	- map containing the values ofthe attribute
----------------------------------------------------------------------------------------------*/
void WpStylesheet::FixStyleReferenceAttrs(int flid, HashMap<HVO, StrUni> & hmhvostu)
{
	WpDaPtr wpda = dynamic_cast<WpDa *>(m_qsda.Ptr());

	//	Generate a list of all the style names.
	Vector<StrUni> vstuNames;
	int cst;
	CheckHr(m_qsda->get_VecSize(khvoText, kflidStText_Styles, &cst));
	Assert(cst == m_vhcStyles.Size());
	for (int ist = 0; ist < cst; ist++)
	{
		SmartBstr sbstr;
		CheckHr(wpda->get_UnicodeProp(m_vhcStyles[ist].hvo, kflidStStyle_Name, &sbstr));
		vstuNames.Push(StrUni(sbstr.Chars()));
	}

	//	For each style, if it has a value in the map, find the corresponding style
	//	and set the attribute.
	for (int ist = 0; ist < cst; ist++)
	{
		HVO hvo = m_vhcStyles[ist].hvo;
		StrUni stuRef;
		if (hmhvostu.Retrieve(hvo, &stuRef))
		{
			for (int istTmp = 0; istTmp < cst; istTmp++)
			{
				if (vstuNames[istTmp] == stuRef)
				{
					CheckHr(wpda->CacheObjProp(hvo, flid, m_vhcStyles[istTmp].hvo));
					break;
				}
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	When we have finished loading and setting up all the styles:
----------------------------------------------------------------------------------------------*/
void WpStylesheet::FinalizeStyles()
{
	ComputeDerivedStyles();
}

/*----------------------------------------------------------------------------------------------
	Initialize a style with an empty text properties object.
	Called from the XML import routines.
----------------------------------------------------------------------------------------------*/
void WpStylesheet::AddEmptyTextProps(HVO hvoStyle)
{
	WpDaPtr wpda = dynamic_cast<WpDa *>(m_qsda.Ptr());

	ITsTextPropsPtr qttp;
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->GetTextProps(&qttp));
	CheckHr(wpda->CacheUnknown(hvoStyle, kflidStStyle_Rules, qttp));

}
