/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: RnImportDlg.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the File / Import Dialog classes.

		RnImportDest
		RnSfMarker
		RnShoeboxSettings
		RnImportShoeboxData
		RnImportCharMapping

		RnImportWizard : AfWizardDlg
		RnImportWizOverview : AfWizardPage
		RnImportWizSettings : AfWizardPage
		RnImportWizSfmDb : AfWizardPage
		RnImportWizRecMarker : AfWizardPage
		RnImportWizRecTypes : AfWizardPage
		RnImportWizFldMappings : AfWizardPage
		RnImportWizChrMappings : AfWizardPage
		RnImportWizFinal : AfWizardPage

		RnImportRecTypeDlg : AfDialog

		RnImportTopicsListOptionsDlg : AfDialog
		RnImportTextOptionsDlg : AfDialog
		RnImportDateOptionsDlg : AfDialog
		RnImportNoOptionsDlg : AfDialog

		RnImportFldMappingDlg : AfDialog
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RNIMPORTDLG_H_INCLUDED
#define RNIMPORTDLG_H_INCLUDED

//#import "mscorlib.tlb" // Not needed because of raw_interfaces_only (JohnT)
#pragma warning(disable:4336)
// Without this warning we get
// warning C4336: import cross-referenced type library
// 'C:\WINNT\Microsoft.NET\Framework\v1.0.3705\mscorlib.tlb' before importing 'SilEncConverters31.tlb'
// This seems to be a spurious warning.
#import "ECInterfaces.tlb" no_namespace raw_interfaces_only no_smart_pointers named_guids
// no_namespace allows us to use the SilEncConverters31 interfaces without figuring what namespace.
// raw_interfaces_only prevents #import from adding raw_ to all method names; it's also supposed
// to prevent the warning about mscorlib, but this doesn't work consistently;
// no_smart_pointers prevents it generating an ATL-style smart pointer class for each interface.
// We prefer to use our own smart pointer class, defined by DEFINE_COM_PTR below.
// named_guids causes it to generate things like IID_IEncConverters, CLSID_EncConverters.
DEFINE_COM_PTR(IEncConverters);
DEFINE_COM_PTR(IEncConverter);

// Possible types of import files.
enum AfImportFileType
{
	kiftXml,
	kiftShoebox,
};

// Possible ways to import data.
enum AfImportDestination
{
	kidstAppend,
	kidstReplace,
	kidstMerge,
};

//:> Forward declarations.
class RnImportFldMappingDlg;

/*----------------------------------------------------------------------------------------------
	This struct stores the data associated with a single import destination.

	@h3{Hungarian: ridst}
----------------------------------------------------------------------------------------------*/
struct RnImportDest
{
	RnImportDest()
	{
		m_clidRecord = 0;
		m_proptype = 0;
		m_flid = 0;
		m_clidDst = 0;
		m_hvoPssl = 0;
		m_wsDefault = 0;
	}

	StrApp m_strName;		// Name of the destination field.
	int m_clidRecord;		// Record type: kclidRnGeneric, kclidRnEvent, or kclidRnAnalysis
	int m_proptype;			// Data type (kcpt__ value).
	int m_flid;				// The field id.
	int m_clidDst;			// Class for Owning and Reference types.
	HVO m_hvoPssl;			// Database id of possibility list being used for references.
	int m_wsDefault;
};

/*----------------------------------------------------------------------------------------------
	This struct stores the data associated with a single Standard Format Marker.

	@h3{Hungarian: sfm}
----------------------------------------------------------------------------------------------*/
struct RnSfMarker
{
	enum RecordType
	{
		krtNone = 0,	// Field marker has no influence on record type.
		krtEvent,		// Field marker indicates an "event" record.
		krtAnalysis		// Field marker indicates an "analysis" record.
	};
	//:> Constructors.
	RnSfMarker();
	RnSfMarker(const char * pszMkr, const char * pszNam, const char * pszLng,
		const char * pszMkrOverThis, RecordType rt = krtNone, int nLevel = 0, int flid = 0);

	void Clear();

	//:> Data loaded from the TYP file.
	StrAnsiBufSmall m_stabsMkr;			// The field marker (without the leading \).
	StrAnsiBufSmall m_stabsNam;			// Short description of the field.
	StrAnsiBufSmall m_stabsLng;			// Language of the field data.
	StrAnsiBufSmall m_stabsMkrOverThis;	// Field marker of parent field, if any.

	//:> Other data describing this standard format marker.
	// What kind of record does this marker specify?  (krtNone means this marker does not
	// specify the record type in any way.)
	RecordType m_rt;
	// If record specifier, level of the record in the hierarchy (1 = root, 0 = not a record
	// specifier).
	int m_nLevel;
	int m_flidDst;				// Field identifier for destination in FieldWorks database.
	int m_wsDefault;			// Default writing system for the field.

	bool m_fIgnoreEmpty;
	/*------------------------------------------------------------------------------------------
		This struct stores the options data associated with a structured text destination.

		@h3{Hungarian: txo}
	------------------------------------------------------------------------------------------*/
	struct TextOptions
	{
		StrUni m_stuStyle;
		bool m_fStartParaNewLine;
		bool m_fStartParaBlankLine;
		bool m_fStartParaIndented;
		bool m_fStartParaShortLine;
		int m_cchShortLim;
		int m_ws;
	};
	TextOptions m_txo;

	/*------------------------------------------------------------------------------------------
		This struct stores the options data associated with a topics list destination.

		@h3{Hungarian: clo}
	------------------------------------------------------------------------------------------*/
	struct TopicsListOptions
	{
		bool m_fHaveMulti;
		StrAnsi m_staDelimMulti;
		bool m_fHaveSub;
		StrAnsi m_staDelimSub;
		bool m_fHaveBetween;
		StrAnsi m_staMarkStart;
		StrAnsi m_staMarkEnd;
		bool m_fHaveBefore;
		StrAnsi m_staBefore;
		bool m_fIgnoreNewStuff;
		Vector<StrAnsi> m_vstaMatch;
		Vector<StrAnsi> m_vstaReplace;
		StrAnsi m_staEmptyDefault;
		PossNameType m_pnt;
		// Parsed versions of the strings above, split into possibly multiple delimiters.
		Vector<StrAnsi> m_vstaDelimMulti;
		Vector<StrAnsi> m_vstaDelimSub;
		Vector<StrAnsi> m_vstaMarkStart;
		Vector<StrAnsi> m_vstaMarkEnd;
		Vector<StrAnsi> m_vstaBefore;
	};
	TopicsListOptions m_clo;
	/*------------------------------------------------------------------------------------------
		This struct stores the options data associated with a date destination.

		@h3{Hungarian: dto}
	------------------------------------------------------------------------------------------*/
	struct DateOptions
	{
		Vector<StrAnsi> m_vstaFmt;
	};
	DateOptions m_dto;
	/*------------------------------------------------------------------------------------------
		This struct stores the options data associated with a multilingual destination.

		@h3{Hungarian: mlo}
	------------------------------------------------------------------------------------------*/
	struct MultiLingualOptions
	{
		int m_ws;
	};
	MultiLingualOptions m_mlo;
};

/*----------------------------------------------------------------------------------------------
	This struct stores the data loaded from a Shoebox database file.

	@h3{Hungarian: risd}
----------------------------------------------------------------------------------------------*/
struct RnImportShoeboxData
{
	Vector<char> m_vcb;						// The file loaded into memory.
	Vector<char *> m_vpszFields;			// Pointers to fields stored in m_vcb.
	Vector< Vector<char *> > m_vvpszLines;	// Map field codes onto list of field data.
	HashMapChars<int> m_hmcLines;
};


/*----------------------------------------------------------------------------------------------
	This struct stores the data for one character mapping.

	@h3{Hungarian: rcmp}
----------------------------------------------------------------------------------------------*/
struct RnImportCharMapping
{
	RnImportCharMapping();
	void Clear();

	StrAnsi m_staBegin;			// Marks the beginning of a character style/writing system.
	StrAnsi m_staEnd;			// Marks the end of a character style/writing system.
	enum CharMapType
	{
		kcmtNone = 0,
		kcmtDirectFormatting,
		kcmtCharStyle,
		kcmtOldWritingSystem
	};
	CharMapType m_cmt;
	StrApp m_strOldWritingSystem;	// Name of the old writing system [WritingSystem] (kcmtOldWritingSystem).
	int m_ws;					// Writing system code (kcmtOldWritingSystem).
	StrUni m_stuCharStyle;		// Name of the character style (kcmtCharStyle).
	int m_tptDirect;			// ktptItalic or ktptBold (kcmtDirectFormatting).
};


/*----------------------------------------------------------------------------------------------
	This class stores the Shoebox import settings.

	Hungarian: shbx
----------------------------------------------------------------------------------------------*/
class RnShoeboxSettings
{
public:
	RnShoeboxSettings();

	//:> Data access methods.
	void SetName(const achar * pszName)
	{
		m_strSettingsName = pszName;
	}
	const achar * Name()
	{
		return m_strSettingsName.Chars();
	}
	Vector<RnSfMarker> & FieldCodes()
	{
		return m_vsfm;
	}
	HashMapChars<int> & FieldCodesMap()
	{
		return m_hmcisfm;
	}
	void SetRecordIndex(int isfm)
	{
		m_isfmRecord = isfm;
	}
	int RecordIndex()
	{
		return m_isfmRecord;
	}
	Vector<RnImportCharMapping> & CharMappings()
	{
		return m_vrcmp;
	}
	HashMapChars<int> & CharMappingsMap()
	{
		return m_hmsaicmp;
	}

	// Retrieve the assigned database (.DB) file pathname.
	const achar * GetDbFile()
	{
		return m_strDbFile.Chars();
	}
	// Set the database (.DB) file pathname.
	void SetDbFile(const achar * pszDbFile)
	{
		m_strDbFile.Assign(pszDbFile);
	}

	// Retrieve the assigned project (.PRJ) file pathname.
	const achar * GetPrjFile()
	{
		return m_strPrjFile.Chars();
	}
	// Set the project (.PRJ) file pathname.
	void SetPrjFile(const achar * pszPrjFile)
	{
		m_strPrjFile.Assign(pszPrjFile);
	}

	void Clear();
	void SetDefaults();
	void Copy(RnShoeboxSettings * pshbx);
	bool Equals(RnShoeboxSettings * pshbx);
	void Serialize(FILE * fp, bool fSavePrjFile, ILgWritingSystemFactory * pwsf);
	char * Deserialize(char * pszSettings, ILgWritingSystemFactory * pwsf);

private:
	StrApp m_strSettingsName;		// Persistent name of these settings.
	Vector<RnSfMarker> m_vsfm;		// List of field markers in the database file.
	int m_isfmRecord;				// Index of record marker in the vector.
	HashMapChars<int> m_hmcisfm;	// Map field markers onto vector indices.
	Vector<RnImportCharMapping> m_vrcmp;	// List of character mappings.
	HashMapChars<int> m_hmsaicmp;			// Map character format codes onto vector indices.

	StrApp m_strDbFile;
	StrApp m_strPrjFile;
};


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Import wizard invoked for the File / Import
	command.

	Hungarian: riw
----------------------------------------------------------------------------------------------*/
class RnImportWizard : public AfWizardDlg
{
typedef AfWizardDlg SuperClass;
public:
	RnImportWizard();
	~RnImportWizard();

	void Initialize(AfMainWnd * pafw, ILgWritingSystemFactory * pwsf);
	void LoadStoredSettings();
	void LoadStyles();
	void LoadWritingSystems();
	void StoreSettings();
	void ImportShoeboxDatabase(RnLpInfo * plpi);

	// Retrieve the current set of import settings.
	RnShoeboxSettings * Settings()
	{
		return &m_shbx;
	}
	// Retrieve the source index of the current set of import settings.
	int GetSettingsIndex()
	{
		return m_ishbxFrom;
	}
	// Set the destination handling: append, replace, or merge.
	void SetDestination(int imdst)
	{
		m_imdst = imdst;
	}
	// Set the database (.DB) file pathname.
	void SetDbFile(const achar * pszDbFile)
	{
		m_shbx.SetDbFile(pszDbFile);
	}
	// Set the project (.PRJ) file pathname.
	void SetPrjFile(const achar * pszPrjFile)
	{
		m_shbx.SetPrjFile(pszPrjFile);
	}
	// Set the type (.TYP) file pathname.
	void SetTypFile(const achar * pszTypeFile)
	{
		m_strTypFile.Assign(pszTypeFile);
	}
	// Set the database type name (from first lines of .DB and .TYP files).
	void SetTypeName(const char * pszTypeName)
	{
		m_staTypeName.Assign(pszTypeName);
	}
	// Retrieve the assigned destination handling: append, replace, or merge.
	int GetDestination()
	{
		return m_imdst;
	}
	// Retrieve the assigned database (.DB) file pathname.
	const achar * GetDbFile()
	{
		return m_shbx.GetDbFile();
	}
	// Retrieve the assigned project (.PRJ) file pathname.
	const achar * GetPrjFile()
	{
		return m_shbx.GetPrjFile();
	}
	// Retrieve the assigned type (.TYP) file pathname.
	const achar * GetTypFile()
	{
		return m_strTypFile.Chars();
	}
	// Retrieve the assigned database type name (from first lines of .DB and .TYP files).
	const char * GetTypeName()
	{
		return m_staTypeName.Chars();
	}
	// Retrieve the number of sets of loaded settings.
	int StoredSettingsCount()
	{
		return m_vpshbxStored.Size();
	}
	// Retrieve the designated set of loaded settings.
	RnShoeboxSettings * StoredSettings(int i)
	{
		return (i < m_vpshbxStored.Size()) ?  m_vpshbxStored[i] : NULL;
	}
	// Set the indexes for the settings starting from, and saving to.
	void SetSettingsFromTo(int iselFrom, int iselTo)
	{
		m_ishbxFrom = iselFrom - 2;
		m_ishbxTo = iselTo -  2;
	}

	// Retrieve the list of possible destinations.
	Vector<RnImportDest> & Destinations()
	{
		return m_vridst;
	}
	// Retrieve the stored Shoebox database data.
	RnImportShoeboxData * StoredData()
	{
		return &m_risd;
	}
	// Retrieve the list of paragraph style names.
	Vector<StrUni> & ParaStyleNames()
	{
		return m_vstuParaStyles;
	}
	// Retrieve the default paragraph style name.
	StrUni & DefaultParaStyle()
	{
		return m_stuParaStyleDefault;
	}
	// Retrieve the list of character style names.
	Vector<StrUni> & CharStyleNames()
	{
		return m_vstuCharStyles;
	}
	// Retrieve the default character style name.
	StrUni & DefaultCharStyle()
	{
		return m_stuCharStyleDefault;
	}
	// Retrieve the stored frame window pointer.
	AfMainWnd * MainWindow()
	{
		return m_pafw;
	}

	// Retrieve the list of language encodings.
	Vector<WrtSysData> & WritingSystems()
	{
		return m_vwsd;
	}

	void DeriveDateFormat(RnSfMarker & sfm);

	ILgWritingSystemFactory * GetWritingSystemFactory()
	{
		return m_qwsf;
	}

	void LoadWrtSysData(IWritingSystem * pws, WrtSysData & wsd);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	// Store the information for the start of a character run in an imported string.
	struct RunStart
	{
		int m_ich;		// character index into string.
		int m_icmp;		// index into m_shbx.CharMappings() vector.
	};

	// Store the information for a possibility item used during processing.
	struct PossItemData
	{
		StrAnsi m_staName;		// Name (or abbreviation) loaded from field in Shoebox database.
		StrUni m_stuName;		// m_staName converted to Unicode.
		bool m_fHasHier;		// Flag whether the name contains a hierarchical delimiter.
		HVO m_hvoOwner;			// Owner of this item: originally the list itself.
		int m_cchMatch;			// Number of characters matched against owner hierarchically.
	};

	HRESULT MakeNewObject(IOleDbEncap * pode, IFwMetaDataCache * pmdc, int clid, HVO hvoOwner,
		int flid, int ord, HVO * phvoNew);
	HRESULT GetVecSize(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
		int * pchvo);
	HRESULT GetVecItem(IOleDbEncap * pode, HVO hvoOwner, int flid, int ord, HVO * phvoItem);
	HRESULT SetInt(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid, int nVal);
	HRESULT SetTime(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
		int64 nTime);
	HRESULT SetString(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
		ITsString * ptss);
	HRESULT SetUnicode(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
		OLECHAR * prgchText, int cchText);
	HRESULT SetMultiStringAlt(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
		int ws, ITsString * ptss);
	HRESULT SetUnknown(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
		IUnknown * punk);
	HRESULT SetObjProp(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
		HVO hvoObj);
	HRESULT AppendObjRefs(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
		HVO * prghvo, int chvoIns);

	void SetPossibility(IOleDbEncap * pode, IFwMetaDataCache * pmdc, char * pszFieldData,
		RnSfMarker & sfm, RnImportDest & ridst, HVO hvoEntry, Vector<HVO> & vhvopssNew,
		IEncConverter * pconAnal);

	void DeleteInvalidFilters(IOleDbEncap * pode);

	// Hungarian: lii
	struct ListItemInfo
	{
		HVO m_hvoItem;
		StrUni m_stuName;
		StrUni m_stuAbbr;
		int m_nHier;
	};
	// Hungarian: li
	struct ListInfo
	{
		ListInfo();
		ListInfo(const ListInfo & li);

		HVO m_hvo;
		short m_cMaxDepth;
		short m_nPreventChoiceAboveLevel;
		bool m_fIsSorted;
		bool m_fIsClosed;
		bool m_fAllowDuplicates;
		bool m_fPreventNodeChoices;
		bool m_fUseExtendedFields;
		short m_nDisplayOption;
		int m_nItemClsid;
		int m_ws;

		Vector<ListItemInfo> m_vlii;
		HashMap<HVO, int> m_hmhvoilii;
	};
	/*------------------------------------------------------------------------------------------
	 * This struct is used in loading the possibility list in order to build up the tree
	 * structure of a hierarchical list.
	 -----------------------------------------------------------------------------------------*/
	struct ListItemInfoPlus
	{
		ListItemInfo m_lii;
		int m_iFirstChild;
		int m_iNextSibling;
	};

	void GetHierName(ListInfo * pli, int ilii, StrUni & stuHier, PossNameType pnt);
	HVO InsertPss(IOleDbEncap * pode, IFwMetaDataCache * pmdc, ListInfo * pli, int ipss,
		const OLECHAR * pszAbbr, const OLECHAR * pszName, PossItemLocation pil);
	void StoreListItemVector(Vector<ListItemInfoPlus> & vliix, int ix,
		Vector<ListItemInfo> & vlii);

	ListInfo * LoadPossibilityList(IOleDbEncap * pode, HVO hvopssl);
	void ParsePossibilities(char * pszFieldData, RnSfMarker & sfm,
		Vector<PossItemData> & vpidValues);
	void SetListHvoAndConvertToUtf16(PossItemData & pid, HVO hvopssl, IEncConverter * pconAnal);
	int PossNameMatchesData(StrUni & stuName, Vector<PossItemData> & vpidData);
	void FixStringData(char * pszFieldData, RnSfMarker::TextOptions & txo,
		StrAnsi & stuFieldData);
	void MakeString(StrAnsi & stuIn, int ws, ITsStrFactory * ptsf, IEncConverters * pencc,
		HashMap<int, WrtSysData> & hmwswsd, ITsString ** pptss);
	void AppendToString(StrAnsi staIn, ITsIncStrBldr * ptisb, int ichMin, int cch, int ws,
		IEncConverters * pencc, HashMap<int, WrtSysData> & hmwswsd);
	void SetParaStyle(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvoPara,
		StrUni & stuStyle);
	int ActualWs(int wsSelector);

	RnShoeboxSettings m_shbx;				// The Shoebox import settings.
	Vector<RnShoeboxSettings *> m_vpshbxStored;
	int m_ishbxFrom;
	int m_ishbxTo;
	Vector<RnImportDest> m_vridst;			// List of possible import destinations.
	int m_imdst;							// select destination: append, replace, merge.
	AfMainWnd * m_pafw;					// Points to application's frame window.
	StrApp m_strTypFile;		// Name of the related Shoebox TYP file.
	StrAnsi m_staTypeName;		// Shoebox TYP name (loaded from DB file, selects TYP file).

	RnImportShoeboxData m_risd;		// The (mostly) raw data from the Shoebox database file.

	Vector<StrUni> m_vstuParaStyles;		// (Sorted) list of paragraph style names.
	StrUni m_stuParaStyleDefault;
	Vector<StrUni> m_vstuCharStyles;		// (Sorted) list of character style names.
	StrUni m_stuCharStyleDefault;
	// Map style names onto their database ids (char and para styles share the namespace).
	HashMapStrUni<HVO> m_hmsuhvoStyles;
	// Map style names onto their text properties.
	ComHashMapStrUni<ITsTextProps> m_hmsuqttpStyles;
	Vector<WrtSysData> m_vwsd;		// List of active writing systems.
	int m_wsVern;
	int m_wsAnal;
	Vector<ListInfo> m_vli;
	HashMap<HVO, int> m_hmhvoListili;
	ILgWritingSystemFactoryPtr m_qwsf;
	int m_wsUser;		// user interface writing system id.
};

typedef GenSmartPtr<RnImportWizard> RnImportWizardPtr;


/*----------------------------------------------------------------------------------------------
	Page One of the Wizard: display some overview information.

	Hungarian: riwov
----------------------------------------------------------------------------------------------*/
class RnImportWizOverview : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizOverview();
	~RnImportWizOverview()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
};

typedef GenSmartPtr<RnImportWizOverview> RnImportWizOverviewPtr;


/*----------------------------------------------------------------------------------------------
	Page Two of the Wizard: save / retrieve the wizard settings.

	Hungarian: riwse
----------------------------------------------------------------------------------------------*/
class RnImportWizSettings : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizSettings();
	~RnImportWizSettings()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual HWND OnWizardNext();			// The user has clicked on the Next btn

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;

	int m_iselFrom;
	int m_iselTo;
	bool m_fDirty;
};

typedef GenSmartPtr<RnImportWizSettings> RnImportWizSettingsPtr;


/*----------------------------------------------------------------------------------------------
	Page Three of the Wizard: choose the Shoebox database file.

	Hungarian: riwdb
----------------------------------------------------------------------------------------------*/
class RnImportWizSfmDb : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizSfmDb();
	~RnImportWizSfmDb()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnSetActive();				// This page is becoming active
	virtual HWND OnWizardBack();			// The user has clicked on the Back btn
	virtual HWND OnWizardNext();			// The user has clicked on the Next btn

	bool CheckForShoebox(const achar * pszFile, bool fInformUser);
	bool LoadTypFile(const achar * pszFile);
	void ScanDbForCodes();

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;

	int m_ishbx;
	StrApp m_strDbFile;
	StrApp m_strPrjFile;
	StrApp m_strCheckedDbFile;		// Facilitates checking DB files only once.
	bool m_fCheckedDbFileIsSFM;
};

typedef GenSmartPtr<RnImportWizSfmDb> RnImportWizSfmDbPtr;


/*----------------------------------------------------------------------------------------------
	Page Four of the Wizard: specify the record marker.

	Hungarian: riwrm
----------------------------------------------------------------------------------------------*/
class RnImportWizRecMarker : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizRecMarker();
	~RnImportWizRecMarker()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnSetActive();				// This page is becoming active

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
	Vector<int> m_visfm;	// Indexes of entries from priw->Settings()->FieldCodes().
};

typedef GenSmartPtr<RnImportWizRecMarker> RnImportWizRecMarkerPtr;


#undef USE_REC_TYPES
#ifdef USE_REC_TYPES
//:Ignore
/*----------------------------------------------------------------------------------------------
	Page Five of the Wizard: specify the record types and hierarchy.

	Hungarian: riwrt
----------------------------------------------------------------------------------------------*/
class RnImportWizRecTypes : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizRecTypes();
	~RnImportWizRecTypes()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

	int GetMaxLevel()
	{
		return m_nLevelMax;
	}
	Vector<RnSfMarker> & FieldCodes()
	{
		RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
		AssertPtr(priw);
		return priw->Settings()->FieldCodes();
	}
	StrApp & GetEventStr()
	{
		return m_strEvent;
	}
	StrApp & GetEventLevelFmt()
	{
		return m_strEventLevFmt;
	}
	StrApp & GetHypoStr()
	{
		return m_strHypo;
	}
	StrApp & GetHypoLevelFmt()
	{
		return m_strHypoLevFmt;
	}


protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnSetActive();				// This page is becoming active

	void ModifyRecType(int isel);
	void InitializeList();

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
	int m_nLevelMax;				// Maximum Depth in the hierarchy.
	Vector<RnSfMarker> m_vsfmHier;	// Std Fmt Markers involved in the hierarchy.
	StrApp m_strEvent;
	StrApp m_strEventLevFmt;
	StrApp m_strHypo;
	StrApp m_strHypoLevFmt;
};

typedef GenSmartPtr<RnImportWizRecTypes> RnImportWizRecTypesPtr;
//:End Ignore
#endif /*USE_REC_TYPES*/

/*----------------------------------------------------------------------------------------------
	Page Six of the Wizard: specify the content mapping, field to field.

	Hungarian: riwfm
----------------------------------------------------------------------------------------------*/
class RnImportWizFldMappings : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizFldMappings();
	~RnImportWizFldMappings()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

	// Return a pointer to the parent import wizard.
	RnImportWizard * GetImportWizard()
	{
		RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
		AssertPtr(priw);
		return priw;
	}
	// Retrieve the specified content (field) mapping.
	RnSfMarker & ContentMapping(int isfm)
	{
		RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
		AssertPtr(priw);
		return priw->Settings()->FieldCodes()[isfm];
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnSetActive();				// This page is becoming active
	virtual HWND OnWizardNext();			// The user has clicked on the Next btn

	void ModifyFieldMapping(int isel);
	void InitializeList();

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
	// Map from the FieldWorks database field identifier code (flid) to the corresponding
	// RnImportDest object.
	HashMap<int,int> m_hmflididst;
	Vector<int> m_visfm;	// Sorted indexes of entries from priw->Settings()->FieldCodes().
};

typedef GenSmartPtr<RnImportWizFldMappings> RnImportWizFldMappingsPtr;


/*----------------------------------------------------------------------------------------------
	Page Seven of the Wizard: specify the character meta-data mappings: old writing system
	(language writing system), direct formatting, character styles.

	Hungarian: riwcm
----------------------------------------------------------------------------------------------*/
class RnImportWizChrMappings : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizChrMappings();
	~RnImportWizChrMappings()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

	// Return a pointer to the parent import wizard.
	RnImportWizard * GetImportWizard()
	{
		RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
		AssertPtr(priw);
		return priw;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnSetActive();				// This page is becoming active

	void ModifyCharMapping(int isel);
	void InitializeList();

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;

	StrApp m_strBold;			// "Bold"
	StrApp m_strItalic;			// "Italic"
	StrApp m_strWrtSysFmt;		// "Writing System: %s"
	StrApp m_strChrStyleFmt;	// "Character Style: %s"
	StrApp m_strIgnored;		// "(Ignored, erased during import)"
};

typedef GenSmartPtr<RnImportWizChrMappings> RnImportWizChrMappingsPtr;


/*----------------------------------------------------------------------------------------------
	Page Eight of the Wizard: choose character code conversion mappings.

	Hungarian: riwcc
----------------------------------------------------------------------------------------------*/
class RnImportWizCodeConverters : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizCodeConverters();
	~RnImportWizCodeConverters()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

	// Return a pointer to the parent import wizard.
	RnImportWizard * GetImportWizard()
	{
		RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
		AssertPtr(priw);
		return priw;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnSetActive();				// This page is becoming active
	virtual HWND OnWizardBack();			// The user has clicked on the Back btn
	//virtual HWND OnWizardNext();			// The user has clicked on the Next btn

	void InitializeList();
	void ModifyCodeConverter(HWND hwndList, int isel, Vector<WrtSysData> & vwsdActive);

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
};

typedef GenSmartPtr<RnImportWizCodeConverters> RnImportWizCodeConvertersPtr;

/*----------------------------------------------------------------------------------------------
	Page Nine of the Wizard: chose whether to overwrite, append or merge into the existing
	database.

	Hungarian: riwfi
----------------------------------------------------------------------------------------------*/
class RnImportWizSaveSet : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizSaveSet();
	~RnImportWizSaveSet()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

	// Return a pointer to the parent import wizard.
	RnImportWizard * GetImportWizard()
	{
		RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
		AssertPtr(priw);
		return priw;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnSetActive();				// This page is becoming active
	virtual HWND OnWizardBack();			// The user has clicked on the Back btn
	virtual HWND OnWizardNext();			// The user clicked on the Next btn

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
	bool m_fNoMerge;		// Flag that merging is not possible with this input data.
};

typedef GenSmartPtr<RnImportWizSaveSet> RnImportWizSaveSetPtr;

/*----------------------------------------------------------------------------------------------
	Page Ten of the Wizard: chose whether to import or quit.

	Hungarian: riwfi
----------------------------------------------------------------------------------------------*/
class RnImportWizFinal : public AfWizardPage
{
typedef AfWizardPage SuperClass;
public:
	RnImportWizFinal();
	~RnImportWizFinal()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
};

typedef GenSmartPtr<RnImportWizFinal> RnImportWizFinalPtr;

#ifdef USE_REC_TYPES
//:Ignore
/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Record Type Add or Modify

	@h3{Hungarian: rirtd}
----------------------------------------------------------------------------------------------*/
class RnImportRecTypeDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportRecTypeDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);

	void Initialize(RnImportWizRecTypes * priwrt, const achar * pszMkr,
		RnSfMarker::RecordType rt, int nLevel);

	const char * GetMarker()
	{
		return m_stabsMkr.Chars();
	}
	RnSfMarker::RecordType GetType()
	{
		return m_rt;
	}
	int GetLevel()
	{
		return m_nLevel;
	}

protected:
	void SaveDialogValues();

	RnImportWizRecTypes * m_priwrt;
	bool m_fError;
	StrAnsiBufSmall m_stabsMkr;
	RnSfMarker::RecordType m_rt;
	int m_nLevel;
};

typedef GenSmartPtr<RnImportRecTypeDlg> RnImportRecTypeDlgPtr;
//:End Ignore
#endif /*USE_REC_TYPES*/

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Content (Field) Mappings options for topics
	list type fields.

	@h3{Hungarian: riclod}
----------------------------------------------------------------------------------------------*/
class RnImportTopicsListOptionsDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportTopicsListOptionsDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void ModifySubstitution(int isel);
	void SaveDialogValues();

	// Initialize the data values.
	void Initialize(RnImportFldMappingDlg * prifmd, RnSfMarker::TopicsListOptions & clo,
		bool fIgnoreEmpty)
	{
		m_prifmd = prifmd;
		m_clo = clo;
		m_fIgnoreEmpty = fIgnoreEmpty;
	}
	// Retrieve the current option settings.
	RnSfMarker::TopicsListOptions & GetOptions()
	{
		return m_clo;
	}
	// Retrieve whether to ignore empty fields.
	bool IgnoreEmpty()
	{
		return m_fIgnoreEmpty;
	}

protected:
	RnImportFldMappingDlg * m_prifmd;
	RnSfMarker::TopicsListOptions m_clo;
	bool m_fIgnoreEmpty;
};

typedef GenSmartPtr<RnImportTopicsListOptionsDlg> RnImportTopicsListOptionsDlgPtr;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Content (Field) Mappings options for Text
	type fields.

	@h3{Hungarian: ritxod}
----------------------------------------------------------------------------------------------*/
class RnImportTextOptionsDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportTextOptionsDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void SaveDialogValues();

	// Initialize the data values.
	void Initialize(RnImportFldMappingDlg * prifmd, RnSfMarker & sfm, bool fIgnoreEmpty)
	{
		m_prifmd = prifmd;
		m_sfm = sfm;
//-		m_txo = sfm.txo;
		m_fIgnoreEmpty = fIgnoreEmpty;
	}
	// Retrieve the current option settings.
	RnSfMarker::TextOptions & GetOptions()
	{
		return m_sfm.m_txo;
	}
	// Retrieve whether to ignore empty fields.
	bool IgnoreEmpty()
	{
		return m_fIgnoreEmpty;
	}

protected:
	virtual bool CmdAddWrtSys(Cmd * pcmd);

	void InitStyleList();
	bool OnStylesClicked();
	bool OnNewParaEachLineEnbClicked();
	bool OnNewParaAfterBlankEnbClicked();
	bool OnNewParaIndentedEnbClicked();
	bool OnNewParaShortLineEnbClicked();
	bool OnDiscardEmptyEnbClicked();

	RnSfMarker m_sfm;
//-	RnSfMarker::TextOptions m_txo;
	RnImportFldMappingDlg * m_prifmd;
	bool m_fIgnoreEmpty;
	Vector<WrtSysData> m_vwsdAll;		// List of all writing systems.

	CMD_MAP_DEC(RnImportTextOptionsDlg);
};

typedef GenSmartPtr<RnImportTextOptionsDlg> RnImportTextOptionsDlgPtr;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Content (Field) Mappings options for Date
	type fields.

	@h3{Hungarian: ridtod}
----------------------------------------------------------------------------------------------*/
class RnImportDateOptionsDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportDateOptionsDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void SaveDialogValues();

	// Initialize the data values.
	void Initialize(RnImportFldMappingDlg * prifmd, RnSfMarker & sfm,
		RnImportShoeboxData * prisd)
	{
		m_prifmd = prifmd;
		m_sfm = sfm;
		m_prisd = prisd;
	}
	// Retrieve the current option settings.
	RnSfMarker::DateOptions & GetOptions()
	{
		return m_sfm.m_dto;
	}
	// Retrieve whether to ignore empty fields.
	bool IgnoreEmpty()
	{
		return m_sfm.m_fIgnoreEmpty;
	}

protected:
	void InitFormatList();
	void ModifyDateFormat(int isel);

	RnImportFldMappingDlg * m_prifmd;
	RnSfMarker m_sfm;
	RnImportShoeboxData * m_prisd;
	LCID m_lcid;				// Analysis writing system's locale.
};

typedef GenSmartPtr<RnImportDateOptionsDlg> RnImportDateOptionsDlgPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Content (Field) Mappings options for
	multilingual string type fields.

	@h3{Hungarian: ridtod}
----------------------------------------------------------------------------------------------*/
class RnImportMultiLingualOptionsDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportMultiLingualOptionsDlg();
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void SaveDialogValues();

	// Initialize the data values.
	void Initialize(RnImportFldMappingDlg * prifmd, RnSfMarker & sfm,
		RnImportShoeboxData * prisd)
	{
		m_prifmd = prifmd;
		m_sfm = sfm;
		m_prisd = prisd;
	}
	// Retrieve the current option settings.
	RnSfMarker::MultiLingualOptions & GetOptions()
	{
		return m_sfm.m_mlo;
	}
	// Retrieve whether to ignore empty fields.
	bool IgnoreEmpty()
	{
		return m_sfm.m_fIgnoreEmpty;
	}

protected:
	virtual bool CmdAddWrtSys(Cmd * pcmd);

	RnImportFldMappingDlg * m_prifmd;
	RnSfMarker m_sfm;
	RnImportShoeboxData * m_prisd;
	Vector<WrtSysData> m_vwsdAll;		// List of all writing systems.

	CMD_MAP_DEC(RnImportMultiLingualOptionsDlg);
};

typedef GenSmartPtr<RnImportMultiLingualOptionsDlg> RnImportMultiLingualOptionsDlgPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Content (Field) Mappings options for fields
	that do not have any special options.

	@h3{Hungarian: rinod}
----------------------------------------------------------------------------------------------*/
class RnImportNoOptionsDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportNoOptionsDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void SaveDialogValues();

	// Initialize the data values.
	void Initialize(RnImportFldMappingDlg * prifmd, bool fIgnoreEmpty)
	{
		m_prifmd = prifmd;
		m_fIgnoreEmpty = fIgnoreEmpty;
	}
	void SetString(int stid);
	// Retrieve whether to ignore empty fields.
	bool IgnoreEmpty()
	{
		return m_fIgnoreEmpty;
	}

protected:
	HashMap<int, StrApp> m_hmstidstr;
	RnImportFldMappingDlg * m_prifmd;
	bool m_fIgnoreEmpty;
};

typedef GenSmartPtr<RnImportNoOptionsDlg> RnImportNoOptionsDlgPtr;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Content (Field) Mappings Add or Modify buttons.

	@h3{Hungarian: rifmd}
----------------------------------------------------------------------------------------------*/
class RnImportFldMappingDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportFldMappingDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void Initialize(RnImportWizFldMappings * priwfm, int isfm = 0, int idst = 0);
	void InitializeFldList();
	void InitializeOptions();

	// Retrieve a pointer to the parent wizard page object.
	RnImportWizFldMappings * WizardPage()
	{
		return m_priwfm;
	}
	// Retrieve the index into the array of Shoebox fields for this mapping.
	int GetSourceIndex()
	{
		return m_isfm;
	}
	// Retrieve the index into the array of FieldWorks Notebook fields for this mapping.
	int GetDestIndex()
	{
		return m_idst;
	}
	// Retrieve the options set for this mapping.
	RnSfMarker & GetDestOptions()
	{
		return m_sfm;
	}

protected:
	void SaveDialogValues();
	bool IsListFlat(RnImportWizard * priw, HVO hvoPssl);

	RnImportWizFldMappings * m_priwfm;
	int m_isfm;					// Index into master vector of RnSfMarker objects.
	int m_idst;					// Index into master vector of RnImportDest objects.
	RnSfMarker m_sfm;			// Used for values set by subdialogs.

	StrApp m_strDataLabelFmt;
	StrApp m_strDataSummaryFmt;
	StrApp m_strTopicsListLabel;
	StrApp m_strTextLabel;
	StrApp m_strStringLabel;
	StrApp m_strTimeLabel;
	StrApp m_strLinkLabel;
	StrApp m_strGenDateLabel;
	StrApp m_strIntegerLabel;
	StrApp m_strDiscardLabel;

	enum OptionsType
	{
		kotNone,
		kotTopicsList,
		kotText,
		kotDate,
		kotMultiLingual
	};
	OptionsType m_ot;
	RnImportNoOptionsDlgPtr m_qrinod;
	RnImportTopicsListOptionsDlgPtr	m_qriclod;
	RnImportTextOptionsDlgPtr m_qritxod;
	RnImportDateOptionsDlgPtr m_qridtod;
	RnImportMultiLingualOptionsDlgPtr m_qrimlod;

	Vector<int> m_vidst;	// Sorted indexes of entries from priw->Destinations().
};

typedef GenSmartPtr<RnImportFldMappingDlg> RnImportFldMappingDlgPtr;

/*----------------------------------------------------------------------------------------------
	This class supports the functionality of the Add button for the "Discard these words:" list
	on the Topics List options subdialog.

	@h3{Hungarian: ricldd}
----------------------------------------------------------------------------------------------*/
class RnImportTLFNewDiscardDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportTLFNewDiscardDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);

	// Retrieve the new discard word.
	const achar * GetData()
	{
		return m_strDiscard.Chars();
	}

	/*------------------------------------------------------------------------------------------
		Initialize the data for the controls embedded within the dialog box.

		@param pszWord The word to modify in the discard list.
	------------------------------------------------------------------------------------------*/
	void Initialize(const achar * pszWord)
	{
		m_strDiscard = pszWord;
	}

protected:
	void SaveDialogValues();

	StrApp m_strDiscard;
};

typedef GenSmartPtr<RnImportTLFNewDiscardDlg> RnImportTLFNewDiscardDlgPtr;


/*----------------------------------------------------------------------------------------------
	This class supports the functionality of the Add button for the "Substitution:" list on the
	Topics List options subdialog.

	@h3{Hungarian: riclsd}
----------------------------------------------------------------------------------------------*/
class RnImportTLFNewSubstitutionDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportTLFNewSubstitutionDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);

	// Retrieve the new match string.
	const achar * GetMatch()
	{
		return m_strMatch.Chars();
	}
	// Retrieve the new replacement string.
	const achar * GetReplacement()
	{
		return m_strReplace.Chars();
	}
	/*------------------------------------------------------------------------------------------
		Initialize the data for the controls embedded within the dialog box.

		@param pszMatch The string to match in the field data.
		@param pszReplace The string to replace it with in the field data.
	------------------------------------------------------------------------------------------*/
	void Initialize(const achar * pszMatch, const achar * pszReplace)
	{
		m_strMatch = pszMatch;
		m_strReplace = pszReplace;
	}

protected:
	void SaveDialogValues();

	StrApp m_strMatch;			// Match string.
	StrApp m_strReplace;		// Replacement string.
};

typedef GenSmartPtr<RnImportTLFNewSubstitutionDlg> RnImportTLFNewSubstitutionDlgPtr;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Character Mapping Add or Modify buttons.

	@h3{Hungarian: ricmd}
----------------------------------------------------------------------------------------------*/
class RnImportChrMappingDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportChrMappingDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void Initialize(RnImportWizChrMappings * priwcm, RnImportCharMapping * pricmp = NULL);

	// Retrieve a pointer to the parent wizard page object.
	RnImportWizChrMappings * WizardPage()
	{
		return m_priwcm;
	}

	// Retrieve the values set by the dialog.
	RnImportCharMapping & GetCharMapping()
	{
		return m_rcmp;
	}

protected:
	void SaveDialogValues();
	void InitStyleList(StrApp & strSel);
	void InitWrtSysList(StrApp & strSel);

	virtual bool CmdAddWrtSys(Cmd * pcmd);

	RnImportWizChrMappings * m_priwcm;
	RnImportCharMapping m_rcmp;
	Vector<WrtSysData> m_vwsdAll;		// List of all writing systems.

	CMD_MAP_DEC(RnImportChrMappingDlg);
};

typedef GenSmartPtr<RnImportChrMappingDlg> RnImportChrMappingDlgPtr;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Date Format List Add or Modify buttons.

	@h3{Hungarian: ridfd}
----------------------------------------------------------------------------------------------*/
class RnImportDateFormatDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnImportDateFormatDlg();

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void Initialize(LCID lcid, const achar * pszFormat = NULL);

	// Retrieve the values set by the dialog.
	const achar * GetDateFormat()
	{
		return m_strFormat.Chars();
	}

protected:
	bool SaveDialogValues();

	LCID m_lcid;
	StrApp m_strFormat;
};

typedef GenSmartPtr<RnImportDateFormatDlg> RnImportDateFormatDlgPtr;

/*----------------------------------------------------------------------------------------------
	Progress report dialog class. This will always be used as a modeless dialog.
	@h3{Hungarian: riprg}
----------------------------------------------------------------------------------------------*/
class RnImportProgressDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	enum
	{
		CHGENC_PRG_PERCENT = WM_APP + 1, // Message indicating new update for progress bar
	};

	// Constructor.
	RnImportProgressDlg();

	void SetActionString(StrApp & str);
	void UpdateStatus(int cDone, int cTotal, int nDeltaSeconds);
	void SetPercentComplete(int nPercent);

protected:
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	// The app framework calls this to initialize the dialog controls prior to displaying the
	// dialog.
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	StrApp m_strStatusFmt;
	StrApp m_strShortStatusFmt;
	StrApp m_strAction;
	StrApp m_strStatus;
	int m_nPercent;
};

/*----------------------------------------------------------------------------------------------
	List editing progress report dialog class. This is a modeless dialog.
	@h3{Hungarian: rielp}
----------------------------------------------------------------------------------------------*/
class RnImportEditListProgressDlg : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	RnImportEditListProgressDlg(int cTotal, const achar * pszFile, bool * fCancelled);
	~RnImportEditListProgressDlg();

	void UpdateStatus(const achar * pszList);
	void Close()
	{
		Assert(m_fModeless);
		if (m_hwnd)
		{
			::DestroyWindow(m_hwnd);
			m_hwnd = NULL;
		}
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnCancel();

	StrApp m_strActionFmt;		// Format string for action message.
	StrApp m_strStatusFmt;		// Format string for status message.
	StrApp m_strFile;			// Original import (standard format) file.
	int m_cDone;				// Number of lists edited thus far.
	int m_cTotal;				// Total number of lists to edit.
	bool * m_pfCancelled;		// Pointer to cancellation flag.
};

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mknb.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif  // !RNIMPORTDLG_H_INCLUDED
