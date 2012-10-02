/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: HelpTopicProvider.h
Responsibility: TE Team
Last reviewed: never

Description:
	This class provides help file/topic information to dialogs.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DLGPROPS_INT_INCLUDED
#define DLGPROPS_INT_INCLUDED 1

DEFINE_COM_PTR(IHelpTopicProvider);

/*----------------------------------------------------------------------------------------------
	This class is used to provide help file/topic information to dialogs.
	Hungarian: htprov.
----------------------------------------------------------------------------------------------*/

class HelpTopicProvider : public IHelpTopicProvider
{
public:

	HelpTopicProvider(const achar * pszHelpFile);
	virtual ~HelpTopicProvider();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);


	// IHelpTopicProvider method
	STDMETHOD(GetHelpString)(BSTR bstrPropName, int iKey, BSTR *bstrPropValue);

protected:
	long m_cref;
	StrApp m_strHelpFile;
};


#endif // DLGPROPS_INT_INCLUDED
