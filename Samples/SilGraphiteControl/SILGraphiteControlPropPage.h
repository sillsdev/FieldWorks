#pragma once

// SILGraphiteControlPropPage.h : Declaration of the CSILGraphiteControlPropPage property page class.


// CSILGraphiteControlPropPage : See SILGraphiteControlPropPage.cpp.cpp for implementation.

class CSILGraphiteControlPropPage : public COlePropertyPage
{
	DECLARE_DYNCREATE(CSILGraphiteControlPropPage)
	DECLARE_OLECREATE_EX(CSILGraphiteControlPropPage)

// Constructor
public:
	CSILGraphiteControlPropPage();

// Dialog Data
	enum { IDD = IDD_PROPPAGE_SILGRAPHITECONTROL };

// Implementation
protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Message maps
protected:
	DECLARE_MESSAGE_MAP()
};
