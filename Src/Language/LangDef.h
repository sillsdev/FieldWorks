/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LangDef.h
Responsibility: Steve McConnel
Last reviewed:

	This file defines classes for reading a language definition XML file.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef __LangDef_H
#define __LangDef_H

/*----------------------------------------------------------------------------------------------
	This class holds the information stored in a FieldWorks language definition file.
	Hungarian: ld
----------------------------------------------------------------------------------------------*/
class LanguageDefinition : public GenRefObj
{
public:
	LanguageDefinition();
	~LanguageDefinition();

	// ILanguageDefinition methods.
	STDMETHOD(get_WritingSystem)(IWritingSystem ** ppws);
	STDMETHOD(put_WritingSystem)(IWritingSystem * pws);

	STDMETHOD(get_BaseLocale)(BSTR * pbstr);
	STDMETHOD(put_BaseLocale)(BSTR bstr);

	STDMETHOD(get_EthnoCode)(BSTR * pbstr);
	STDMETHOD(put_EthnoCode)(BSTR bstr);

	STDMETHOD(get_LocaleName)(BSTR * pbstr);
	STDMETHOD(put_LocaleName)(BSTR bstr);

	STDMETHOD(get_LocaleScript)(BSTR * pbstr);
	STDMETHOD(put_LocaleScript)(BSTR bstr);

	STDMETHOD(get_LocaleCountry)(BSTR * pbstr);
	STDMETHOD(put_LocaleCountry)(BSTR bstr);

	STDMETHOD(get_LocaleVariant)(BSTR * pbstr);
	STDMETHOD(put_LocaleVariant)(BSTR bstr);

	STDMETHOD(get_DisplayName)(BSTR * pbstr);

	STDMETHOD(get_CollationElements)(BSTR * pbstr);
	STDMETHOD(put_CollationElements)(BSTR bstr);

	STDMETHOD(get_LocaleResources)(BSTR * pbstr);
	STDMETHOD(put_LocaleResources)(BSTR bstr);

	STDMETHOD(get_PuaDefinitionCount)(int * pcPUA);
	STDMETHOD(GetPuaDefinition)(int i, int * pnCode, BSTR * pbstrData);
	STDMETHOD(UpdatePuaDefinition)(int i, int nCode, BSTR bstrData);
	STDMETHOD(AddPuaDefinition)(int nCode, BSTR bstrData);
	STDMETHOD(RemovePuaDefinition)(int i);

	STDMETHOD(get_FontCount)(int * pcFont);

	STDMETHOD(GetFont)(int i, BSTR * pbstrFilename);
	STDMETHOD(UpdateFont)(int i, BSTR bstrFilename);
	STDMETHOD(AddFont)(BSTR bstrFilename);
	STDMETHOD(RemoveFont)(int i);

	STDMETHOD(get_Keyboard)(BSTR * pbstr);
	STDMETHOD(put_Keyboard)(BSTR bstr);

	STDMETHOD(GetEncodingConverter)(BSTR * pbstrInstall, BSTR * pbstrFile);
	STDMETHOD(SetEncodingConverter)(BSTR bstrInstall, BSTR bstrFile);

	STDMETHOD(get_CollationCount)(int * pcColl);
	STDMETHOD(GetCollation)(int i, ICollation ** ppcoll);

	STDMETHOD(Serialize)();
	STDMETHOD(SaveWritingSystem)(BSTR bstrOldIcuLocale);

	/*------------------------------------------------------------------------------------------
		This data structure represents a PUA character definition.
		Hungarian: cd.
	------------------------------------------------------------------------------------------*/
	struct CharDef
	{
		int m_code;			// Written to XML as hexadecimal number.
		StrUni m_stuData;
	};

protected:
	IWritingSystemPtr m_qws;
	Vector<CharDef> m_vcdPua;
	Vector<StrUni> m_vstuFonts;
	StrUni m_stuEncConvInstall;
	StrUni m_stuEncConvFile;
	StrUni m_stuKeyboard;
	StrUni m_stuBaseLocale;
	StrUni m_stuCollationElements;
	StrUni m_stuEthnoCode;
	StrUni m_stuLocaleScript;
	StrUni m_stuLocaleCountry;
	StrUni m_stuLocaleName;
	StrUni m_stuLocaleResources;
	StrUni m_stuLocaleVariant;
	StrUni m_stuLocaleWinLCID;
	StrUni m_stuNewLocale;
	friend class LanguageDefinitionFactory;
};
typedef GenSmartPtr<LanguageDefinition> LanguageDefinitionPtr;


/*----------------------------------------------------------------------------------------------
	This class facilitates creating a LanguageDefinition object.
	Hungarian: ldf
----------------------------------------------------------------------------------------------*/
class LanguageDefinitionFactory : public GenRefObj
{
public:
	LanguageDefinitionFactory();
	~LanguageDefinitionFactory();

	// ILanguageDefinitionFactory methods.
	STDMETHOD(Initialize)(IWritingSystem * pws, LanguageDefinition ** ppld);
	STDMETHOD(InitializeFromXml)(ILgWritingSystemFactory * pwsf, BSTR bstrIcuLocale,
		LanguageDefinition ** ppld);
	STDMETHOD(get_LanguageDefinition)(LanguageDefinition ** ppld);
	STDMETHOD(put_LanguageDefinition)(LanguageDefinition * pld);

	//void Deserialize(BSTR bstrFile);
	//void get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf);
	//void put_WritingSystemFactory(ILgWritingSystemFactory * pwsf);

	void RemoveLanguage(BSTR bstrIcuLocale);

protected:
	LanguageDefinitionPtr m_qld;
	//ILgWritingSystemFactoryPtr m_qwsf;

	void RemoveWsReferences(const achar * pszFile, BSTR bstrIcuLocale);
};
typedef GenSmartPtr<LanguageDefinitionFactory> LanguageDefinitionFactoryPtr;


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*__LangDef_H*/
