/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpStylesheet.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	This class provides an implementation of IVwStylesheet for WorldPad, which rather than
	storing the styles in the database, instead writes them to an XML file.
----------------------------------------------------------------------------------------------*/

#pragma once
#ifndef WPSTYLESHEET_INCLUDED
#define WPSTYLESHEET_INCLUDED 1

class WpStylesheet;
typedef GenSmartPtr<WpStylesheet> WpStylesheetPtr;

//:End Ignore

/*----------------------------------------------------------------------------------------------
	A stylesheet that is hooked into a WpDa database.
	Hungarian: wpsts.
----------------------------------------------------------------------------------------------*/
class WpStylesheet : public AfStylesheet
#if !WIN32
	, ISimpleStylesheet
#endif
{
public:
	void Init(ISilDataAccess * psda);

	HVO NextStyleHVO()
	{
		return m_hvoNextStyle;
	}

	void AddNormalParaStyle();
	HRESULT AddLoadedStyles(HVO * rghvoNew, int chvo, int hvoNextStyle);
	void FixStyleReferenceAttrs(int flid, HashMap<HVO, StrUni> & hmhvostu);
	void FinalizeStyles();
	void AddEmptyTextProps(HVO hvoStyle);

protected:
	HVO m_hvoNextStyle;

	virtual HRESULT GetNewStyleHVO(HVO * phvo);
};

#endif // !WPSTYLESHEET_INCLUDED
