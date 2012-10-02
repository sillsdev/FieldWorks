/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001, 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDbInfo.h
Responsibility: Steve McConnel
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		AfDbInfo : GenRefObj - This class contains information about a database connection.
		PossItemInfo - This class stores information on one possibility list item.
		PossListInfo : GenRefObj - This class stores information on one possibility list. It
			contains multiple PossItemInfo objects.
		AfLpInfo : GenRefObj - This class contains information about a language project.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDBINFO_H
#define AFDBINFO_H 1

#include "VwOleDbDa.h" // currently defines PossNameType, UserViewSpecVec, and FldSpec

// External forward declarations.
class AfTagOverlayTool;
class AfOverlayListBar;
class AfStylesheet;
class AfDbStylesheet;
class CustViewDa;
typedef ComSmartPtr<CustViewDa> CustViewDaPtr;
class UserViewSpec;
class FldSpec;
typedef GenSmartPtr<UserViewSpec> UserViewSpecPtr;
typedef Vector<UserViewSpecPtr> UserViewSpecVec; // Hungarian vuvs

// Internal forward declarations.
class AfLpInfo;
typedef GenSmartPtr<AfLpInfo> AfLpInfoPtr;
class PossListInfo;
typedef GenSmartPtr<PossListInfo> PossListInfoPtr;


const int kcchPossNameAbbrMax = 120;  // Maximum Chars for name and Abbr. of list items.
const OLECHAR kchHierDelim = ':';	// Delimiter for possibility hierarchy (e.g., noun:proper)


// This enum is used as bitmaps for AfDbInfo::FullRefresh().
typedef enum
{
	kfdbiSortSpecs = 1 << 0,
	kfdbiFilters = 1 << 1,
	kfdbiMetadata = 1 << 2,
	kfdbiUserViews = 1 << 3,
	kfdbiEncFactories = 1 << 4,
	kfdbiLpInfo = 1 << 5,
} DbiRefreshFlags;

/*----------------------------------------------------------------------------------------------
	This structure contains all the necessary information for filters.

	@h3{Hungarian: afi)
----------------------------------------------------------------------------------------------*/
struct AppFilterInfo
{
	StrUni m_stuName;		// Name of the filter.
	bool m_fSimple;			// Flag whether the filter is simple or complex.
	HVO m_hvo;				// Database id of the filter.
	StrUni m_stuColInfo;	// String representation of the filter.
	bool m_fShowPrompt;		// Flag whether to prompt the user each time filter is applied.
	StrUni m_stuPrompt;		// String to use for prompting the user.

	int m_clidRec;			// Basic class id this filter is targeted toward.
};

/*----------------------------------------------------------------------------------------------
	This structure contains all the necessary information for sort methods.

	@h3{Hungarian: asi)
----------------------------------------------------------------------------------------------*/
struct AppSortInfo
{
	StrUni m_stuName;			// Name of this sort method.
	bool m_fIncludeSubfields;	// Flag whether sorting/indexing includes subfields as well.
	HVO m_hvo;					// Database id of the sort method.
	StrUni m_stuPrimaryField;	// Path to the primary sort key field.
	int m_wsPrimary;			// Writing system of the primary sort key (may be null).
	int m_collPrimary;			// Collation choice of the primary sort key (may be null).
	bool m_fPrimaryReverse;		// Flag whether primary sort order is reversed.
	StrUni m_stuSecondaryField;	// Path to the secondary sort key field (may be null).
	int m_wsSecondary;			// Writing system of the secondary sort key (may be null).
	int m_collSecondary;		// Collation choice of the secondary sort key (may be null).
	bool m_fSecondaryReverse;	// Flag whether secondary sort order is reversed.
	StrUni m_stuTertiaryField;	// Path to the tertiary sort key field (may be null).
	int m_wsTertiary;			// Writing system of the tertiary sort key (may be null).
	int m_collTertiary;			// Collation choice of the tertiary sort key (may be null).
	bool m_fTertiaryReverse;	// Flag whether tertiary sort order is reversed.

	int m_clidRec;				// Basic class id this sort method is targeted toward.
	bool m_fMultiOutput;		// Flag that this sort method can produce multiple items in
								// the output list for each actual item.
};

/*----------------------------------------------------------------------------------------------
	This abstract base class contains information about a database connection.

	@h3{Hungarian: dbi)
----------------------------------------------------------------------------------------------*/
class AfDbInfo : public GenRefObj
{
public:
	AfDbInfo();
	virtual ~AfDbInfo();

	virtual AfLpInfo * GetLpInfo(HVO hvoLp) = 0;
	virtual void LoadSortMethods()
	{
		// Does nothing unless overridden.
	}
	virtual void LoadFilters()
	{
		// Does nothing unless overridden.
	}
	virtual void SaveAllData()
	{
		// Don't save anything unless overridden.
	}
	virtual void CompleteBrowseRecordSpec(UserViewSpec * puvs)
	{
		// Applications that provide for browse views must override this method.
		Assert(puvs->m_vwt != kvwtBrowse);
	}

	/*------------------------------------------------------------------------------------------
		Return the log stream pointer (NULL unless overridden).
	------------------------------------------------------------------------------------------*/
	virtual HRESULT GetLogPointer(IStream ** ppfist)
	{
		if (ppfist)
			*ppfist = NULL;
		return S_OK;
	}

	/*------------------------------------------------------------------------------------------
		Return the name of the database server.
	------------------------------------------------------------------------------------------*/
	const OLECHAR * ServerName()
	{
		return m_stuSvrName.Chars();
	}
	/*------------------------------------------------------------------------------------------
		Return the name of the database.
	------------------------------------------------------------------------------------------*/
	const OLECHAR * DbName()
	{
		return m_stuDbName.Chars();
	}
	/*------------------------------------------------------------------------------------------
		Get the interface pointer for an IOleDbEncap (database access) object.

		@param ppode
	------------------------------------------------------------------------------------------*/
	void GetDbAccess(IOleDbEncap ** ppode)
	{
		AssertPtr(ppode);
		*ppode = m_qode;
		AddRefObj(*ppode);
	}
	/*------------------------------------------------------------------------------------------
		Set the interface pointer for an IOleDbEncap (database access) object.

		@param pode
	------------------------------------------------------------------------------------------*/
	void SetDbAccess(IOleDbEncap * pode)
	{
		AssertPtr(m_qmdc);
		m_qode = pode;
		CheckHr(m_qmdc->Init(m_qode));
	}
	/*------------------------------------------------------------------------------------------
		Get the interface pointer to the metadata cache (e.g., information on field definitions
		in database).

		@param ppmdc
	------------------------------------------------------------------------------------------*/
	void GetFwMetaDataCache(IFwMetaDataCache ** ppmdc)
	{
		AssertPtr(ppmdc);
		*ppmdc = m_qmdc;
		AddRefObj(*ppmdc);
	}

	//:>****************************************************************************************
	//:>	Methods for handling the filters cache.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return the number of filters defined in this database.
	------------------------------------------------------------------------------------------*/
	int GetFilterCount()
	{
		return m_vafi.Size();
	}
	/*------------------------------------------------------------------------------------------
		Return a reference to the information stored for the indicated filter.

		@param ifltr Index into the internal table of filters stored in the database.
	------------------------------------------------------------------------------------------*/
	AppFilterInfo & GetFilterInfo(int ifltr)
	{
		Assert((uint)ifltr < (uint)m_vafi.Size());
		return m_vafi[ifltr];
	}

	void AddFilter(const wchar * pszName, bool fSimple, HVO hvo, const wchar * pszColInfo,
		bool fShowPrompt, const wchar * pszPrompt, int clidRec);

	/*------------------------------------------------------------------------------------------
		Remove the indicated filter from the internal table.

		@param ifltr Index into the internal table of filters stored in the database.
	------------------------------------------------------------------------------------------*/
	void RemoveFilter(int ifltr)
	{
		Assert((uint)ifltr < (uint)m_vafi.Size());
		m_vafi.Delete(ifltr);
	}

	int ComputeFilterIndex(int isel, int clidRec);

	//:>****************************************************************************************
	//:>	Methods for handling the sort methods cache.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return the number of sort methods defined in this database.
	------------------------------------------------------------------------------------------*/
	int GetSortCount()
	{
		return m_vasi.Size();
	}
	/*------------------------------------------------------------------------------------------
		Return a reference to the information stored for the indicated sort method.

		@param isort Index into the internal table of sort methods stored in the database.
	------------------------------------------------------------------------------------------*/
	AppSortInfo & GetSortInfo(int isort)
	{
		Assert((uint)isort < (uint)m_vasi.Size());
		return m_vasi[isort];
	}
	/*------------------------------------------------------------------------------------------
		Add the sort method to the internal table.

		@param asi Reference to the sort method information.
	------------------------------------------------------------------------------------------*/
	void AddSort(AppSortInfo & asi)
	{
		m_vasi.Push(asi);
	}

	/*------------------------------------------------------------------------------------------
		Remove the indicated sort method from the internal table.

		@param isort Index into the internal table of sort methods stored in the database.
	------------------------------------------------------------------------------------------*/
	void RemoveSort(int isort)
	{
		Assert((uint)isort < (uint)m_vasi.Size());
		m_vasi.Delete(isort);
	}

	int ComputeSortIndex(int isel, int clidRec);

	//:>****************************************************************************************
	//:>	Methods for handling the user view cache.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return the vector of user-customizaeable view specifications.
	------------------------------------------------------------------------------------------*/
	UserViewSpecVec & GetUserViewSpecs()
	{
		return m_vuvs;
	}
	/*------------------------------------------------------------------------------------------
		Set the vector of user-customizaeable view specifications.

		@param pvuvs Pointer to a vector of user-customizaeable view specifications.

		@return True.
	------------------------------------------------------------------------------------------*/
	bool SetUserViewSpecs(UserViewSpecVec * pvuvs)
	{
		m_vuvs = *pvuvs;
		return true;
	}

	bool GetCopyUserViewSpecs(UserViewSpecVec * vuvs);
	FldSpec * FindFldSpec(int flid, int vwt = -1);
	virtual void Init(const OLECHAR * pszServer, const OLECHAR * pszDatabase, IStream * pfist);
	virtual void CleanUp();
	bool DeleteObject(HVO hvo);

	HVO GetIdFromGuid(GUID * puid);
	bool GetGuidFromId(HVO hvo, GUID & uid);

	/*------------------------------------------------------------------------------------------
		Return the language writing system factory for this database.
	------------------------------------------------------------------------------------------*/
	void GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
	{
		AssertPtr(ppwsf);
		*ppwsf = m_qwsf;
		AddRefObj(*ppwsf);
	}

	/*------------------------------------------------------------------------------------------
		Return the user interface writing system ws for this database.
	------------------------------------------------------------------------------------------*/
	int UserWs()
	{
		AssertPtr(m_qwsf);
		if (!m_wsUser)
			CheckHr(m_qwsf->get_UserWs(&m_wsUser));
		return m_wsUser;
	}

	Vector<AfLpInfoPtr> & GetLpiVec()
	{
		return m_vlpi;
	}
	virtual bool FullRefresh(int grfdbi);
	void CheckTransactionKludge();

	bool LoadUserViews(const CLSID * pclsid, int vwt = -1);

protected:
	StrUni m_stuSvrName;	// Currently connected server (DBPROP_INIT_DATASOURCE)
	StrUni m_stuDbName;		// Current database (DBPROP_INIT_CATALOG)

	//:> ENHANCE PaulP: Later, we may want to change this to a vector of IOleDbEncap
	//:> objects when the requirement to connect to multiple databases comes in.
	IOleDbEncapPtr m_qode;		// Current data access object.
	ComSmartPtr<IFwMetaDataCache> m_qmdc;	// Current meta data cache.
	Vector<AppFilterInfo> m_vafi;	// Vector of loaded (cached) filters.
	Vector<AppSortInfo> m_vasi;		// Vector of loaded (cached) sort methods.
	Vector<AfLpInfoPtr> m_vlpi;		// Vector of loaded (cached) language projects.
	UserViewSpecVec m_vuvs;			// Specs of user-customizeable views; see TlsOptDlg, etc.
	ILgWritingSystemFactoryPtr m_qwsf;
	int m_wsUser;					// local cached value.
};
typedef GenSmartPtr<AfDbInfo> AfDbInfoPtr;


// This enum is used as bitmaps for AfLpInfo::FullRefresh().
typedef enum
{
	kflpiStyles = 1 << 0,
	kflpiCache = 1 << 1,
	kflpiWritingSystems = 1 << 2,
	kflpiProjBasics = 1 << 3,
	kflpiPossLists = 1 << 4,
	kflpiOverlays = 1 << 5,
	kflpiExtLink = 1 << 6,
} LpiRefreshFlags;

// PossNameType enum moved to VwOleDbDa because used in FldSpec;

// This enum is used by PossListInfo::InsertPss and PossListInfo::MoveItem.
typedef enum
{
	kpilBefore = 0,		// Before, at the same level.
	kpilAfter = 1,		// After, at the same level.
	kpilUnder = 2,		// Following in a sublist.
	kpilTop = 3			// Following, but at the top level of the possibility list.
} PossItemLocation;



/*----------------------------------------------------------------------------------------------
	This class contains the information for one possibility list item.

	@h3{Hungarian: pii)
----------------------------------------------------------------------------------------------*/
class PossItemInfo
{
friend PossListInfo;

public:
	PossItemInfo();

	// Return the database id of this possibility list item.
	HVO GetPssId()
	{
		return m_hvoPss;
	}

	// Return the ownership hierarchy level for this possibility list item relative to the list.
	int GetHierLevel()
	{
		return m_nHier;
	}

	// Return the writing system of this list item.
	int GetWs()
	{
		return m_ws;
	}

	int GetLevel(PossListInfo * ppli);
	COLORREF GetForeColor()
		{ return m_clrFore; }
	COLORREF GetBackColor()
		{ return m_clrBack; }
	COLORREF GetUnderColor()
		{ return m_clrUnder; }
	int GetUnderlineType()
		{ return m_unt; }

	void SetName(StrUni stu, PossNameType pnt = kpntNameAndAbbrev, int ws = 0);
	int GetName(StrUni & stu, PossNameType pnt = kpntNameAndAbbrev);
	int GetHierName(PossListInfo * ppli, StrUni & stu, PossNameType pnt = kpntNameAndAbbrev);

protected:
	HVO m_hvoPss; // Possibility ID.
	int m_nHier; // Contains the ownership hierarchy level.
	StrUni m_stu; // Contains the item abbreviation and name in the format "Ab - Name".
	COLORREF m_clrFore;
	COLORREF m_clrBack;
	COLORREF m_clrUnder;
	int m_unt; // FwUnderlineType
	int m_ws; // This is the actual writing system for this item.
};


// This enum is used by the PossListNotify class.
typedef enum
{
	kplnaInsert = 1,
	kplnaDelete,
	kplnaModify,
	kplnaMerged,
	kplnaDisplayOption,
	kplnaReload, // None of the other parameters are used with this.
} PossListNotifyAction;

/*----------------------------------------------------------------------------------------------
	This class is used to send a notification for any action that modifies a possibility list.

	@h3{Hungarian: pln)
----------------------------------------------------------------------------------------------*/
class PossListNotify
{
public:
	// Note: some instances will receive notifications from two different lists
	// (e.g., RnRoledPartic), so we need to include the list as well as the item.
	virtual void ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
		int ipssDst) = 0;
};


/*----------------------------------------------------------------------------------------------
	This class contains the information for a possibility list. It is reference counted, so it
	will get deleted when it goes out of scope. The dirty flag will be set when a change has
	been made to the list.

	@h3{Hungarian: pli)
----------------------------------------------------------------------------------------------*/
class PossListInfo : public GenRefObj
{
friend AfLpInfo;

public:
	PossListInfo();

	/*------------------------------------------------------------------------------------------
		Return the database id of this possibility list.
	------------------------------------------------------------------------------------------*/
	HVO GetPsslId()
	{
		return m_hvoPssl;
	}
	/*------------------------------------------------------------------------------------------
		Return the language writing system currently assigned to this possibility list.
	------------------------------------------------------------------------------------------*/
	int GetTitleWs()
	{
		return m_wsTitle;
	}
	/*------------------------------------------------------------------------------------------
		Return the Magic writing system currently assigned to this possibility list.
	------------------------------------------------------------------------------------------*/
	int GetWs()
	{
		return m_wsMagic;
	}
	/*------------------------------------------------------------------------------------------
		Return 1 for flat list ,otherwise returns the number of hierarchical levels.
	------------------------------------------------------------------------------------------*/
	int GetDepth()
	{
		return m_nDepth;
	}
	/*------------------------------------------------------------------------------------------
		Return true if the list is sorted.
	------------------------------------------------------------------------------------------*/
	bool GetIsSorted()
	{
		return m_fIsSorted;
	}
	/*------------------------------------------------------------------------------------------
		Return true if list is NOT Editable.
	------------------------------------------------------------------------------------------*/
	bool GetIsClosed()
	{
		return m_fIsClosed;
	}
	/*------------------------------------------------------------------------------------------
		Return true to allow duplicates in this possibility list.
	------------------------------------------------------------------------------------------*/
	bool GetAllowDup()
	{
		return m_fAllowDup;
	}
	/*------------------------------------------------------------------------------------------
		Return true if Extended fields used in this possibility list.
	------------------------------------------------------------------------------------------*/
	bool GetUseExtendedFlds()
	{
		return m_fUseExtendedFlds;
	}
	/*------------------------------------------------------------------------------------------
		Return the Class Id of this possibility list.
	------------------------------------------------------------------------------------------*/
	int GetItemClsid()
	{
		return m_nItemClsid;
	}
	/*------------------------------------------------------------------------------------------
		Return the name of this possibility list.
	------------------------------------------------------------------------------------------*/
	const OLECHAR * GetName()
	{
		return m_stuName.Chars();
	}
	/*------------------------------------------------------------------------------------------
		Return the abbreviation for this possibility list.
	------------------------------------------------------------------------------------------*/
	const OLECHAR * GetAbbr()
	{
		return m_stuAbbr.Chars();
	}
	/*------------------------------------------------------------------------------------------
		Return the description for this possibility list.
	------------------------------------------------------------------------------------------*/
	const OLECHAR * GetDescription()
	{
		return m_stuDesc.Chars();
	}
	/*------------------------------------------------------------------------------------------
		Return the name of the html help information "file".
	------------------------------------------------------------------------------------------*/
	const OLECHAR * GetHelpFile()
	{
		return m_stuHelp.Chars();
	}
	/*------------------------------------------------------------------------------------------
		Return the type of name information displayed for the possibility list items.
	------------------------------------------------------------------------------------------*/
	PossNameType GetDisplayOption()
	{
		return m_pnt;
	}

	/*------------------------------------------------------------------------------------------
		Return the language project info pointer.
	------------------------------------------------------------------------------------------*/
	AfLpInfoPtr GetLpInfoPtr()
	{
		return m_qlpi;
	}

	void SetDisplayOption(PossNameType pnt);
	/*------------------------------------------------------------------------------------------
		Return the number of items in this possibility list.
	------------------------------------------------------------------------------------------*/
	int GetCount()
	{
		return m_vpii.Size();
	}

	/*------------------------------------------------------------------------------------------
		Return the indicated possibility list item.
		@param ipss Index into the internal vector of possibility list items.
	------------------------------------------------------------------------------------------*/
	PossItemInfo * GetPssFromIndex(int ipss)
	{
		if (m_vpii.Size() == 0)
			return NULL;
		Assert((uint)ipss < (uint)m_vpii.Size());
		return &m_vpii[ipss];
	}
	/*------------------------------------------------------------------------------------------
		Return the hierarchy level for the first item in the list (which is probably always 1).

		@param ipss Index into the internal vector of possibility list items.
	------------------------------------------------------------------------------------------*/
	int TopHierLevel()
	{
		if (m_vpii.Size() == 0)
			return 0;
		return m_vpii[0].GetHierLevel();
	}

	HVO GetOwnerIdFromId(HVO hvoPss);
	int GetIndexFromId(HVO hvoPss, PossListInfo ** pppli = NULL);
	PossItemInfo * FindPss(const OLECHAR * prgch, Locale & loc, PossNameType pnt,
		int * pipii = NULL, ComBool fExactMatch = false);
	PossItemInfo * FindPssHier(const OLECHAR * prgch, Locale & loc, PossNameType pnt,
		ComBool & fExactMatch);
	HVO GetIdFromHelpId(const OLECHAR * psz);
	bool LoadPossList(AfLpInfo * plpi, HVO hvoPssl, int wsMagic, int ipli);
	bool CreateNewOverlay(IVwOverlay ** ppvo);
	bool InsertPss(int ipss, const OLECHAR * pszAbbr, const OLECHAR * pszName,
		PossItemLocation pil, int * pipssNew);
	bool DeletePss(HVO hvoPss, bool fFixDbStrings = true);
	bool ValidPossName(int ipss, StrUni & stuAbbr, StrUni &  stuName);
	bool PossUniqueName(int ipss, StrUni &  stuName, PossNameType pnt, int & iMatch);
	bool IsFirstHvoAncestor(HVO hvoFirst, HVO hvoSecond);
	bool CorrectMoveWhenSorted(HVO hvoSource, HVO & hvoTarget, PossItemLocation & pil,
		bool fHaltAtSource);
	bool PutInSortedPosition(HVO hvoItem, bool fNoDownMoves);
	bool MoveItem(HVO hvoSource, HVO hvoTarget, PossItemLocation pil, bool fSort = true);
	void Sort();
	bool MergeItem(HVO hvoSrc, HVO hvoDst);
	bool FullRefresh();
	bool Synchronize(SyncInfo & sync);

	void AddNotify(PossListNotify * ppln);
	bool RemoveNotify(PossListNotify * ppln);
	void DoNotify(int nAction, HVO hvoSrc, HVO hvoDst, int ipssSrc, int ipssDst);

protected:
	int FindIndexFollowingOwner(const HVO hvoOwner, const Vector<PossItemInfo> & vpii);

	HVO m_hvoPssl;		// Possibility list ID.
	int m_wsTitle;		// Actual Writing system of list.
	int m_wsMagic;		// Magic Writing system of list.
	int m_nDepth;		// 1=flat list,otherwise number tells number of hierarchical levels
	bool m_fIsSorted;	// Is the list sorted.
	bool m_fIsClosed;	// true if list is NOT Editable .
	bool m_fAllowDup;	// true to allow duplicates.
	bool m_fUseExtendedFlds; // Extended fields
	int m_nItemClsid;	// Class Id of list.
	PossNameType m_pnt;	// Determines how possibility item names are displayed.
	StrUni m_stuAbbr;	// Abbreviation for the possibility list.
	StrUni m_stuName;	// Full name of the possibility list name.
	StrUni m_stuDesc;	// Description for the possibility list.
	StrUni m_stuHelp;	// Name of the html help information "file" for this possibility list.
	Vector<PossItemInfo> m_vpii;	// Vector of possibility list items.
	AfLpInfoPtr m_qlpi;		// Pointer to language project that contains this possibility list.
	Vector<PossListNotify *> m_vppln; // Vector of possibility list notifiers.
};


/*----------------------------------------------------------------------------------------------
	This structure contains all the necessary information for overlays.

	@h3{Hungarian: aoi)
----------------------------------------------------------------------------------------------*/
struct AppOverlayInfo
{
	HVO m_hvo;					// Database ID of the overlay.
	HVO m_hvoPssl;				// Database ID of the associated possibility list.
	StrUni m_stuName;			// Name of the overlay.
	IVwOverlayPtr m_qvo;		// Pointer to the overlay object.
	GenSmartPtr<AfTagOverlayTool> m_qtot;	// Pointer to the overlay tool window object.
};


/*----------------------------------------------------------------------------------------------
	This structure contains the hvo and ws of a possibility list item.

	@h3{Hungarian: aoi)
----------------------------------------------------------------------------------------------*/
typedef struct HvoWs
{
	HvoWs()
	{
		hvo = 0;
		wsPssl = 0;
	}

	HvoWs(HVO hvoIn, int wsPsslIn)
	{
		hvo = hvoIn;
		wsPssl = wsPsslIn;
	}
	HVO hvo; // HVO of a possibility list item
	int wsPssl; // The writing system of the list holding this item.
} HvoWs;


/*----------------------------------------------------------------------------------------------
	This pure virtual class contains information about a language project.

	@h3{Hungarian: lpi)
----------------------------------------------------------------------------------------------*/
class AfLpInfo : public GenRefObj
{
friend PossListInfo;

public:
	AfLpInfo();
	// Destructor.
	~AfLpInfo()
	{
	}

	virtual bool OpenProject() = 0;
	virtual bool LoadProjBasics() = 0;

	/*------------------------------------------------------------------------------------------
		Return the name of the language project.
	------------------------------------------------------------------------------------------*/
	const OLECHAR * PrjName()
	{
		return m_stuPrjName.Chars();
	}
	void SetPrjName(const OLECHAR * pszName);
	void GetCurrentProjectName(BSTR * pbstrProjName);

	/*------------------------------------------------------------------------------------------
		Return the database id of the language project.
	------------------------------------------------------------------------------------------*/
	HVO GetLpId()
	{
		return m_hvoLp;
	}

	/*------------------------------------------------------------------------------------------
		Return the name of the current top major project, or NULL if none available.
	------------------------------------------------------------------------------------------*/
	virtual const OLECHAR * ObjName()
	{
		return NULL;
	}
	/*------------------------------------------------------------------------------------------
		Return the database id of the current top major project, or 0 if none available.
	------------------------------------------------------------------------------------------*/
	virtual HVO ObjId()
	{
		return 0;
	}

	/*------------------------------------------------------------------------------------------
		Return the vector containing ids of standard project possibility lists.
	------------------------------------------------------------------------------------------*/
	Vector<HVO> & GetPsslIds()
	{
		return m_vhvoPsslIds;
	}
	/*------------------------------------------------------------------------------------------
		Adds a hvo of list to the vector containing ids of standard project possibility lists.
	------------------------------------------------------------------------------------------*/
	void AddPsslId(HVO hvo)
	{
		m_vhvoPsslIds.Push(hvo);
	}
	/*------------------------------------------------------------------------------------------
		Return a pointer to the database information associated with this language project.
	------------------------------------------------------------------------------------------*/
	AfDbInfo * GetDbInfo()
	{
		return m_qdbi.Ptr();
	}
	/*------------------------------------------------------------------------------------------
		Get the data access cache for this project.

		@param ppcvd Address of a pointer for returning the data access cache.
	------------------------------------------------------------------------------------------*/
	void GetDataAccess(CustViewDa ** ppcvd)
	{
		AssertPtr(ppcvd);
		*ppcvd = m_qcvd;
		AddRefObj(*ppcvd);
	}
	/*------------------------------------------------------------------------------------------
		Get an interface pointer to the action (undo/redo functionality) handler for this
		language project.

		@param ppacth Address of a pointer for returning the action handler interface.
	------------------------------------------------------------------------------------------*/
	void GetActionHandler(IActionHandler ** ppacth)
	{
		if (m_qacth)
		{
			AssertPtr(ppacth);
			*ppacth = m_qacth;
			AddRefObj(*ppacth);
		}
	};

	Locale GetLocale(int ws);	//Get ICU Locale for a writing system.

	//:>****************************************************************************************
	//:>	Methods for handling the possibility lists cache.
	//:>****************************************************************************************

	bool LoadCustomLists();
	bool LoadPossList(HVO hvoPssl, int wsMagic, PossListInfo ** pppli, bool fRefresh = false);
	bool LoadPossListForItem(HVO hvoPss, int wsMagic, PossListInfo ** pppli);
	bool GetPossListAndItem(HVO hvoPss, int wsMagic, PossItemInfo ** pppii,
		PossListInfo ** pppli = NULL);
	bool GetPossListAndItem(HVO hvoPss, int wsMagic, int * pipss, PossListInfo ** pppli);
	bool GetPossList(HVO hvoPss, int wsMagic, PossListInfo ** pppli);

	//:>****************************************************************************************
	//:>	Methods for handling the overlays cache.
	//:>****************************************************************************************

	/*------------------------------------------------------------------------------------------
		Return the number of overlays in the overlay cache.
	------------------------------------------------------------------------------------------*/
	int GetOverlayCount()
	{
		return m_vaoi.Size();
	}

	bool LoadOverlays();
	bool GetOverlay(int ivo, IVwOverlay ** ppvo);
	int GetOverlayIndex(AfTagOverlayTool * ptot);

	/*------------------------------------------------------------------------------------------
		Return a reference to the information cached for the indicated overlay.

		@param ivo Index into the internal table of loaded overlay.
	------------------------------------------------------------------------------------------*/
	AppOverlayInfo & GetOverlayInfo(int ivo)
	{
		Assert((uint)ivo < (uint)m_vaoi.Size());
		return m_vaoi[ivo];
	}
	/*------------------------------------------------------------------------------------------
		Add an overlay defined by the function parameters to the overlay cache.

		@param hvo Database ID of the overlay itself.
		@param hvoPssl Database ID of the possibility list associated with the overlay.
		@param stuName Name of the overlay.
	------------------------------------------------------------------------------------------*/
	void AddOverlay(HVO hvo, HVO hvoPssl, StrUni & stuName)
	{
		AppOverlayInfo aoi;
		aoi.m_hvo = hvo;
		aoi.m_hvoPssl = hvoPssl;
		aoi.m_stuName = stuName;
		m_vaoi.Push(aoi);
	}

	void RemoveOverlay(int ivo);
	void ShowOverlay(int ivo, AfOverlayListBar * polb, HWND hwndOwner, bool fShow = true,
		RECT * prc = NULL);

	/*------------------------------------------------------------------------------------------
		Return the vector of analysis encodings.
	------------------------------------------------------------------------------------------*/
	Vector<int> & AnalWss()
	{
		return m_vwsAnal;
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of analysis then vernacular writing systems.
	------------------------------------------------------------------------------------------*/
	Vector<int> & AnalVernWss()
	{
		return m_vwsAnalVern;
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of vernacular then analysis writing systems.
	------------------------------------------------------------------------------------------*/
	Vector<int> & VernAnalWss()
	{
		return m_vwsVernAnal;
	}
	/*------------------------------------------------------------------------------------------
		Return the first analysis writing system, if there is one, otherwise return the UI ws.
	------------------------------------------------------------------------------------------*/
	int AnalWs()
	{
		AssertPtr(m_qdbi);
		return m_vwsAnal.Size() ? m_vwsAnal[0] : m_qdbi->UserWs();
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of vernacular encodings.
	------------------------------------------------------------------------------------------*/
	Vector<int> & VernWss()
	{
		return m_vwsVern;
	}
	/*------------------------------------------------------------------------------------------
		Return the first vernacular writing system, if there is one, otherwise return English.
	------------------------------------------------------------------------------------------*/
	int VernWs()
	{
		AssertPtr(m_qdbi);
		return m_vwsVern.Size() ? m_vwsVern[0] : m_qdbi->UserWs();
	}
	/*------------------------------------------------------------------------------------------
		Return possible project vernacular encodings.
	------------------------------------------------------------------------------------------*/
	Vector<int> & AllVernWss()
	{
		return m_vwsAllVern;
	}
	/*------------------------------------------------------------------------------------------
		Return possible project analysis encodings.
	------------------------------------------------------------------------------------------*/
	Vector<int> & AllAnalWss()
	{
		return m_vwsAllAnal;
	}

	/*------------------------------------------------------------------------------------------
		Return a normalized magic writing system. This is needed for entering keys in m_hmPssWs.
		Since kwsAnal/kwsAnals and kwsVern/kwsVerns give identical results, we force to plural.
	------------------------------------------------------------------------------------------*/
	int NormalizeWs(int wsMagic)
	{
		switch (wsMagic)
		{
		case kwsAnal:
			return kwsAnals;
		case kwsVern:
			return kwsVerns;
		default:
			return wsMagic;
		}
	}

	int ActualWs(int ws);
	void ProjectWritingSystems(Vector<int> & vws);
	bool LoadWritingSystems();

	AfStylesheet * GetAfStylesheet();
	virtual void Init(AfDbInfo * pdbi, HVO hvoLp);
	virtual void CleanUp();
	void ClearOverlays();
	// Load Stylesheet. This needs to be overwritten in subclasses that use it.
	virtual void LoadStyles(CustViewDa * pcvd, AfLpInfo * plpi) {}
	int GetPsslWsFromDb(HVO hvoPssl);

	/*------------------------------------------------------------------------------------------
		External Link methods.
	------------------------------------------------------------------------------------------*/
	const OLECHAR * GetExtLinkRoot(bool fRefresh = false);
	bool MapExternalLink(StrAppBuf & strbFile);
	bool UnmapExternalLink(StrAppBuf & strbFile);

	/*------------------------------------------------------------------------------------------
		Synchronization methods.
	------------------------------------------------------------------------------------------*/
	virtual bool FullRefresh(int grflpi);
	virtual bool Synchronize(SyncInfo & sync);
	virtual bool StoreAndSync(SyncInfo & sync)
	{
		// Do nothing unless overridden.
		return false;
	}
	void StoreSync(SyncInfo & sync);
	GUID GetSyncGuid()
	{
		return m_guidSync;
	}
	void SetSyncGuid(GUID guid)
	{
		m_guidSync = guid;
	}
	int GetLastSync()
	{
		return m_nLastSync;
	}
	void SetLastSync(int nId)
	{
		m_nLastSync = nId;
	}

	Vector<PossListInfoPtr> & GetPossLists()
	{
		return m_vqpli; // Vector of loaded (cached) possibility lists.
	}

	// hvoPss is the id of the item, wsPssl is the writing system of the list.
	bool IsPssLoaded(HVO hvoPss, int wsPssl)
	{
		int nT;
		HvoWs hvows(hvoPss, NormalizeWs(wsPssl));
		return m_hmPssWs.Retrieve(hvows, &nT);
	}

protected:
	StrUni m_stuPrjName; // Name of the current language project.
	// StyleSheet containing styles (Paragraph, Char, etc.).
	ComSmartPtr<AfDbStylesheet> m_qdsts;
	Vector<int> m_vwsAnal; // Current analysis writing system.
	Vector<int> m_vwsVern; // Current vernacular writing systems.
	Vector<int> m_vwsVernAnal; // Current vernacular then analysis writing systems.
	Vector<int> m_vwsAnalVern; // Current analysis then vernacular writing systems.
	Vector<int> m_vwsAllAnal; // Possible analysis writing systems for project.
	Vector<int> m_vwsAllVern; // Possible vernacular writing systems for project.
	Vector<AppOverlayInfo> m_vaoi; // Vector of loaded (cached) overlays.
	// The key of this hashmap is a possibility. All possibilities from all cached lists
	// are inserted into the hashmap.
	// The HIWORD of each item is the index of the possibility list in our cache.
	// The LOWORD of each item is the index of the possibility within the list.
	HashMap<HvoWs, int> m_hmPssWs;
	Vector<PossListInfoPtr> m_vqpli; // Vector of loaded (cached) possibility lists.
	Vector<HVO> m_vhvoPsslIds; // Ids of possibility lists for current project.
	HVO m_hvoLp; // Id of the language project itself.
	AfDbInfoPtr m_qdbi; // Points to the database containing this language project.
	CustViewDaPtr m_qcvd; // Holds the data access cache for this project.
	IActionHandlerPtr m_qacth; // Points to the undo/redo action handler
	StrUni m_stuExtLinkRoot;  // External Link root
	GUID m_guidSync; // Unique ID used in Synch$ for changes made by this application.
	int m_nLastSync; // The last ID from Synch$ when we synchronized data with the database.

private:
	// Instead of using this to clear a list, you should use FullRefresh();
	bool ReleasePossList(HVO hvoPssl, int wsMagic);

};

#endif /* AFDBINFO_H */
