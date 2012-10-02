/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgCollation.h
Responsibility: Steve McConnel
Last reviewed:

	Note that the interface is declared in Language.idh, but the conceptual model (and hence
	the database schema) is defined in Cellar.cm, since Language does not have a conceptual
	model directory of its own.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef __LgCollation_H
#define __LgCollation_H

/*----------------------------------------------------------------------------------------------
	An Collation represents one way of collating data in a particular writing system and
	writing system.

	Hungarian: coll
----------------------------------------------------------------------------------------------*/
class Collation : public ICollation
{
public:
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	static const wchar * GetCollationName(int lcid);
	static bool _IsValidCollation(StrUni & stuColl, IOleDbEncap * pode);

	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	//:> ICollation Methods
	STDMETHOD(get_Name)(int ws, BSTR * pbstr);
	STDMETHOD(put_Name)(int ws, BSTR bstr);
	STDMETHOD(get_NameWsCount)(int * pcws);
	STDMETHOD(get_NameWss)(int cws, int * prgenc);
	STDMETHOD(get_Hvo)(int * phvo);
	STDMETHOD(get_WinLCID)(int * pnCode);
	STDMETHOD(put_WinLCID)(int nCode);
	STDMETHOD(get_WinCollation)(BSTR * pbstr);
	STDMETHOD(put_WinCollation)(BSTR bstr);
	STDMETHOD(get_IcuResourceName)(BSTR * pbstr);
	STDMETHOD(put_IcuResourceName)(BSTR bstr);
	STDMETHOD(get_IcuResourceText)(BSTR * pbstr);
	STDMETHOD(put_IcuResourceText)(BSTR bstr);
	STDMETHOD(get_Dirty)(ComBool * pf);
	STDMETHOD(put_Dirty)(ComBool fDirty);
	STDMETHOD(WriteAsXml)(IStream * pstrm, int cchIndent);
	STDMETHOD(Serialize)(IStorage * pstg);
	STDMETHOD(Deserialize)(IStorage * pstg);
	STDMETHOD(get_IcuRules)(BSTR * pbstr);
	STDMETHOD(put_IcuRules)(BSTR bstr);
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** pwsf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(LoadIcuRules)(BSTR bstrBaseLocale);

	// public, but not interface.
	void SetHvo(int hvo);

protected:
	Collation();
	~Collation();

	long m_cref;
	long m_hvo;								// The database Id for the Collation object.
	bool m_fDirty;

	HashMap<int, StrUni> m_hmencstuName;	// Map encodings onto names of this collation.
	int m_lcid;								// Windows Locale ID assigned to this collation.
	StrUni m_stuWinCollation;				// Windows collation designator.
	StrUni m_stuIcuResourceName;			// ICU Resource name.
	StrUni m_stuIcuResourceText;			// ICU Resource text.
	StrUni m_stuIcuRules;					// ICU Rules.
	ILgWritingSystemFactoryPtr m_qwsf;

	// Vector of collations supported by the database.
	static Vector<StrUni> s_vstuColl;

	// The factory needs to create these directly.
	friend class LgWritingSystemFactory;
	// Unit tests also need to create these directly.
	friend class TestLanguage::TestLgWritingSystem;
};

DEFINE_COM_PTR(Collation);

// Local Variables:
// mode:C++
// End: (These 3 lines are useful to Steve McConnel.)

#endif //!__LgCollation_H
