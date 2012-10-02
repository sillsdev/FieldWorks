// SILGraphiteControlPropPage.cpp : Implementation of the CSILGraphiteControlPropPage property page class.

#include "stdafx.h"
#include "SILGraphiteControl.h"
#include "SILGraphiteControlPropPage.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


IMPLEMENT_DYNCREATE(CSILGraphiteControlPropPage, COlePropertyPage)



// Message map

BEGIN_MESSAGE_MAP(CSILGraphiteControlPropPage, COlePropertyPage)
END_MESSAGE_MAP()



// Initialize class factory and guid

IMPLEMENT_OLECREATE_EX(CSILGraphiteControlPropPage, "SILGRAPHITECON.SILGraphiteConPropPage.1",
	0x1a7accc2, 0x9988, 0x420a, 0xa4, 0xac, 0x74, 0xac, 0xaa, 0xcc, 0xd3, 0xa)



// CSILGraphiteControlPropPage::CSILGraphiteControlPropPageFactory::UpdateRegistry -
// Adds or removes system registry entries for CSILGraphiteControlPropPage

BOOL CSILGraphiteControlPropPage::CSILGraphiteControlPropPageFactory::UpdateRegistry(BOOL bRegister)
{
	if (bRegister)
		return AfxOleRegisterPropertyPageClass(AfxGetInstanceHandle(),
			m_clsid, IDS_SILGRAPHITECONTROL_PPG);
	else
		return AfxOleUnregisterClass(m_clsid, NULL);
}



// CSILGraphiteControlPropPage::CSILGraphiteControlPropPage - Constructor

CSILGraphiteControlPropPage::CSILGraphiteControlPropPage() :
	COlePropertyPage(IDD, IDS_SILGRAPHITECONTROL_PPG_CAPTION)
{
	SetHelpInfo(_T("Names to appear in the control"), _T("SILGraphiteControl.HLP"), 0);
}



// CSILGraphiteControlPropPage::DoDataExchange - Moves data between page and properties

void CSILGraphiteControlPropPage::DoDataExchange(CDataExchange* pDX)
{
	DDP_PostProcessing(pDX);
}



// CSILGraphiteControlPropPage message handlers
