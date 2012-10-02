/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CustViewDa.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides data access for customizeable views (Document and Data Entry;
	probably others later).
	The title and, for an event, the type, are always loaded.
	Other fields are loaded as specified by the Field specs in a custom document spec.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RN_CUST_VIEW_DA_INCLUDED
#define RN_CUST_VIEW_DA_INCLUDED 1


class CustViewDa;


class AfStatusBar;

/*----------------------------------------------------------------------------------------------
	The main customizeable document view constructor class.
	Hungarian: cvd.
----------------------------------------------------------------------------------------------*/
class CustViewDa : public VwOleDbDa
{
	typedef VwOleDbDa SuperClass;
public:

	CustViewDa();
	~CustViewDa();

	// Overridden interface methods--ISilDataAccess
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** ppencf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(get_WritingSystemsOfInterest)(int cwsMax, int * pws, int * pcws);

	// Other public methods
	void SetTags(int tagRootItems, int tagItemSort = 0)
	{
		m_tagRootItems = tagRootItems;
		m_tagItemSort = tagItemSort;
	}
	virtual void Init(AfLpInfo * plpi, IOleDbEncap * pode, IFwMetaDataCache * pmdc,
		ILgWritingSystemFactory * pwsf, IActionHandler * pacth)
	{
		AssertPtr(plpi);
		m_qlpi = plpi;
		CheckHr(SuperClass::Init(pode, pmdc, pwsf, pacth));
	}
	AfLpInfo * GetLpInfo()
	{
		return m_qlpi;
	}
	AfDbInfo * GetDbInfo()
	{
		AssertPtr(m_qlpi);
		return m_qlpi->GetDbInfo();
	}
	IOleDbEncap * GetOleDbEncap()
	{
		return m_qode;
	}
	void LoadMainItems(HVO hvoRoot, HvoClsidVec & vhcItems, AppSortInfo * pasi = NULL,
		SortKeyHvosVec * pvskhItems = NULL);

	/*------------------------------------------------------------------------------------------
		Return the vector of analysis encodings. (Override to get from LpInfo
	------------------------------------------------------------------------------------------*/
	virtual Vector<int> & AnalWss()
	{
		return m_qlpi->AnalWss();
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of analysis then vernacular writing systems.
	------------------------------------------------------------------------------------------*/
	virtual Vector<int> & AnalVernWss()
	{
		return m_qlpi->AnalVernWss();
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of vernacular then analysis writing systems.
	------------------------------------------------------------------------------------------*/
	virtual Vector<int> & VernAnalWss()
	{
		return m_qlpi->VernAnalWss();
	}
	/*------------------------------------------------------------------------------------------
		Return the first analysis writing system. (LoadWritingSytems makes sure there is one).
	------------------------------------------------------------------------------------------*/
	virtual int AnalWs()
	{
		return m_qlpi->AnalWs();
	}
	/*------------------------------------------------------------------------------------------
		Return the vector of vernacular encodings.
	------------------------------------------------------------------------------------------*/
	virtual Vector<int> & VernWss()
	{
		return m_qlpi->VernWss();
	}

protected:

	// Property of root object that contains the items (e.g., kflidRnResearchNbk_Records).
	int m_tagRootItems;
	// Property of an item that contains the sort column (or 0 to disable)
	// (e.g., kflidRnGenericRec_DateCreated).
	// When proper sorting gets implemented, this will probably not be needed. As it stands,
	// it is certainly too restrictive for lexical databases.
	int m_tagItemSort;

	GenSmartPtr<AfLpInfo> m_qlpi;

};
DEFINE_COM_PTR(CustViewDa);


#endif // RN_CUST_VIEW_DA_INCLUDED
