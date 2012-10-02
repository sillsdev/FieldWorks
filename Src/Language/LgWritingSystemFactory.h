/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgWritingSystemFactory.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGWRITINGSYSTEMFACTORY_INCLUDED
#define LGWRITINGSYSTEMFACTORY_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: LgWritingSystemFactory
Description:
Hungarian: wsf
----------------------------------------------------------------------------------------------*/
class LgWritingSystemFactory : public ILgWritingSystemFactory
{
public:
	// Static methods

	// static method to get the factory associated with the registry.
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// static method to get the factory associated with the given database.
	static void Create(IOleDbEncap * pode, IStream * pfistLog,
		ILgWritingSystemFactory ** ppwsf);

	// static method to recreate a (read-only) free-standing factory from the data in the
	// IStorage object.
	static void Deserialize(IStorage * pstg, ILgWritingSystemFactory ** ppwsf);

	// static method to be called by shutdown notifier object.
	static void ShutdownIfActive();

	// static method to return singleton Unicode character properties object.
	static HRESULT GetUnicodeCharProps(ILgCharacterPropertyEngine ** pplcpe);

	// static method to serialize a vector of writing systems.
	static void SerializeVector(IStorage * pstg, ComVector<IWritingSystem> & vqws);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ILgWritingSystemFactory Methods
	STDMETHOD(get_Engine)(BSTR bstrIcuLocale, IWritingSystem ** ppwseng);
	STDMETHOD(get_EngineOrNull)(int ws, IWritingSystem ** ppwseng);
	STDMETHOD(AddEngine)(IWritingSystem * pwseng);
	STDMETHOD(RemoveEngine)(int ws);
	STDMETHOD(GetWsFromStr)(BSTR bstr, int * pws);
	STDMETHOD(GetStrFromWs)(int ws, BSTR * pbstr);
	STDMETHOD(get_NumberOfWs)(int * pcws);
	STDMETHOD(GetWritingSystems)(int * rgws, int cws);
	STDMETHOD(get_UnicodeCharProps)(ILgCharacterPropertyEngine ** pplcpe);
	STDMETHOD(get_DefaultCollater)(int ws, ILgCollatingEngine ** ppcoleng);
	STDMETHOD(get_CharPropEngine)(int ws, ILgCharacterPropertyEngine ** pplcpe);
	STDMETHOD(get_Renderer)(int ws, IVwGraphics * pvg, IRenderEngine ** ppre);
	STDMETHOD(get_RendererFromChrp)(LgCharRenderProps * pchrp, IRenderEngine ** ppre);
	STDMETHOD(Shutdown)();
	STDMETHOD(Clear)();
	STDMETHOD(SaveWritingSystems)();
	STDMETHOD(Serialize)(IStorage * pstg);
	STDMETHOD(get_UserWs)(int * pws);
	STDMETHOD(put_UserWs)(int ws);
	STDMETHOD(get_BypassInstall)(ComBool * pfBypass);
	STDMETHOD(put_BypassInstall)(ComBool fBypass);
	STDMETHOD(AddWritingSystem)(int ws, BSTR bstrIcuLocale);
	STDMETHOD(get_IsShutdown)(ComBool * pfIsShutdown);

	// Member variable access

	// Other public methods.
	void CreateLgCollation(Collation ** ppcoll, IWritingSystem * pws);
	void ChangingIcuLocale(int ws, const OLECHAR * pchOld, const OLECHAR * pchNew);
	bool DetermineRemoteUser();

	/*------------------------------------------------------------------------------------------
		The following two data structures hold the information loaded from the database for a
		single writing system before it is converted into actual COM objects.
	------------------------------------------------------------------------------------------*/
	// Hungarian: ci
	struct CollationInfo
	{
		// Default constructor.
		CollationInfo()
		{
			m_hvo = 0;
			m_nWinLCID = 0;
		}
		// Copy constructor.
		CollationInfo::CollationInfo(const CollationInfo & ci)
		{
			m_hvo = ci.m_hvo;
			m_nWinLCID = ci.m_nWinLCID;
			m_stuWinCollation = ci.m_stuWinCollation;
			m_stuIcuResourceName = ci.m_stuIcuResourceName;
			m_stuIcuResourceText = ci.m_stuIcuResourceText;
			m_stuIcuRules = ci.m_stuIcuRules;
			//	What we need is a const_iterator for the collection classes!
			const HashMap<int, StrUni> * pchm = &ci.m_hmwsstuName;
			HashMap<int, StrUni> * phm = const_cast<HashMap<int, StrUni> *>(pchm);
			HashMap<int, StrUni>::iterator it;
			for (it = phm->Begin(); it != phm->End(); ++it)
				m_hmwsstuName.Insert(it.GetKey(), it.GetValue());
		}

		int m_hvo;
		int m_nWinLCID;
		StrUni m_stuWinCollation;
		StrUni m_stuIcuResourceName;
		StrUni m_stuIcuResourceText;
		StrUni m_stuIcuRules;
		HashMap<int, StrUni> m_hmwsstuName;
	};

	// Hungarian: wsi
	struct WritingSystemInfo
	{
		int m_hvo;
		int m_nLocale;
		int m_fRightToLeft;
		StrUni m_stuDefSerif;
		StrUni m_stuDefSS;
		StrUni m_stuDefBodyFont;
		StrUni m_stuDefMono;
		StrUni m_stuFontVar;
		StrUni m_stuSansFontVar;
		StrUni m_stuBodyFontFeatures; // Features = Var. TODO FWM-123: data migration to change variation to features to be consistent with the UI.
		StrUni m_stuIcuLocale;
		StrUni m_stuSpellCheckDictionary;
		StrUni m_stuLegacyMapping;
		StrUni m_stuKeymanKeyboard;
		StrUni m_stuValidChars;
		StrUni m_stuMatchedPairs;
		StrUni m_stuPunctuationPatterns;
		StrUni m_stuCapitalizationInfo;
		StrUni m_stuQuotationMarks;
		SYSTEMTIME m_stModified;

		HashMap<int, StrUni> m_hmwsstuAbbr;
		HashMap<int, StrUni> m_hmwsstuName;
		ComHashMap<int, ITsString> m_hmwsqtssDesc;
		Vector<CollationInfo> m_vci;
	};

protected:
	LgWritingSystemFactory(void)
	{
		m_cref = 1;
		ModuleEntry::ModuleAddRef();
	}
	~LgWritingSystemFactory()
	{
		ModuleEntry::ModuleRelease();
	}

	static ComVector<ILgWritingSystemFactory> g_vqwsf;

	// Member variables

	// We assume the all ws Ids and all ws ICULocales are unique.
	// ws ICULocale, looked up by ws identifiers.
	HashMap<int, StrUni> m_hmwsLocale;
	// ws identifiers, looked up by ws ICULocale.
	HashMapStrUni<int> m_hmwsId;

	long m_cref;

	// A map relating writing system identifiers to loaded writing system instances.
	typedef ComHashMap<int, IWritingSystem> MapIntEncoding; // Hungarian hmnwsobj
	MapIntEncoding m_hmnwsobj;

	// The set of defined writing systems, either in memory or in the database.
	Set<int> m_setws;

	// The names of the database server machine and database for reading/writing persistent
	// language writing system information.
	StrUni m_stuServer;
	StrUni m_stuDatabase;
	IOleDbEncapPtr m_qode;
	// For WSF's created for a particular database, this keeps track of how many
	// times Create has been used to get this WSF for different ODEs and not yet
	// shutdown. There should be one shutdown for each Create. Only the last is
	// effective.
	int m_nOdeShareCount;
	IStreamPtr m_qfistLog;	// Pointer to logging stream.

	// The writing system for the user interface, as obtained from the resource file.
	int m_wsUser;

	// This flag allows a program (particularly during testing) to bypass the normal
	// operation of updating the language definition xml file and calling InstallLanguage
	// to update the ICU files.
	bool m_fBypassInstall;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods

	HRESULT GetEngine(int ws, IWritingSystem ** ppwseng);
	HRESULT CreateEngine(BSTR bstrIcuLocale, IWritingSystem ** ppwseng);

	int GetDefaultWsCode();

	HRESULT UseDatabase(IOleDbEncap * pode, IStream * pfistLog);
	int GetNewHvoWs();
	bool LoadWsFromFile(WritingSystem * pzws);
	void RemoveWsRunsForDescriptions(IWritingSystem * pws, int wsDel);
	bool IsLocaleInstalled(const wchar * pszIcuLocale);
	bool IsCustomLocale(const wchar * pszIcuLocale);
	bool RefreshMapsIfNeeded();
	void LoadMapsFromDatabase(Set<int> & setws,
		HashMap<int, StrUni> & hmwsLocale, HashMapStrUni<int> & hmwsId);

	// Let the unit tests create factories directly.
	friend class TestLanguage::TestLgWritingSystem;
	friend class TestLanguage::TestLgWritingSystemFactory;
	friend void TestLanguage::CreateTestWritingSystemFactory(ILgWritingSystemFactory ** ppwsf);
};

DEFINE_COM_PTR(LgWritingSystemFactory);

/*----------------------------------------------------------------------------------------------
Class: LgWritingSystemFactoryBuilder
Description:
Hungarian: wsfb
----------------------------------------------------------------------------------------------*/
class LgWritingSystemFactoryBuilder : public ILgWritingSystemFactoryBuilder
{
public:
	// static method to get a factory builder.
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ILgWritingSystemFactoryBuilder Methods
	STDMETHOD(GetWritingSystemFactory)(IOleDbEncap * pode, IStream * pfistLog,
		ILgWritingSystemFactory ** ppwsf);
	STDMETHOD(GetWritingSystemFactoryNew)(BSTR bstrServer, BSTR bstrDatabase,
		IStream * pfistLog, ILgWritingSystemFactory ** ppwsf);
	STDMETHOD(Deserialize)(IStorage * pstg, ILgWritingSystemFactory ** ppwsf);
	STDMETHOD(ShutdownAllFactories)();

protected:
	LgWritingSystemFactoryBuilder(void)
	{
		m_cref = 1;
	}

	// Member variables

	long m_cref;

	// Let the unit tests create factorie builders directly.
	friend class TestLanguage::TestLgWritingSystemFactory;
};

DEFINE_COM_PTR(LgWritingSystemFactoryBuilder);

#endif  //LGWRITINGSYSTEMFACTORY_INCLUDED
