// CAutoConfigDlg.cpp : implementation file
//

#include "stdafx.h"
#include "AutoConfigDlg.h"
#include "ECResource.h"
#include "QueryConverterNameDlg.h"

// CAutoConfigDlg dialog

IMPLEMENT_DYNAMIC(CAutoConfigDlg, CAutoConfigPropertyPage)

CAutoConfigDlg::CAutoConfigDlg
(
	IEncConverters* pECs,
	UINT            nID,
	const CString&  strFriendlyName,
	const CString&  strConverterIdentifier,
	ConvType        eConversionType,
	const CString&  strLhsEncodingId,
	const CString&  strRhsEncodingId,
	long            lProcessTypeFlags,
	BOOL            bIsInRepository
)
  : CAutoConfigPropertyPage(nID)
  , m_strFriendlyName(strFriendlyName)
  , m_strOriginalFriendlyName(strFriendlyName)
  , m_strConverterIdentifier(strConverterIdentifier)
  , m_strLhsEncodingId(strLhsEncodingId)
  , m_strRhsEncodingId(strRhsEncodingId)
  , m_eConversionType(eConversionType)
  , m_eOrigConvType(eConversionType)    // this'll remain what it was from the client (to go back if we change it)
  , m_lProcessTypeFlags(lProcessTypeFlags)
  , m_bIsInRepository(bIsInRepository)
  , m_pECs(pECs)
  , m_bQueryForConvType(false)
  , m_bQueryToUseTempConverter(true)
{
	// if the identifier is given, then it means we're editing.
	// (which means our button says *Update* rather than "Save in system repository"
	//  and we should ask during OnOK whether they want to update or not)
	m_bEditMode = !strConverterIdentifier.IsEmpty();

	// this parameter seems the most confusing and yet is a crucial part of EncConverters
	//  so if this is *given* to us, then just use those values rather than prompting the user
	//  for them (e.g. FW knows that the BulkEdits are Unicode_to_(from_)Unicode, so we don't
	//  have to bother the user with it.
	// If we're in 'edit' mode, it is often because the user has mis-configured these parameters
	//  so if edit mode, then query for the ConvType value.
	if( (m_eConversionType == ConvType_Unknown) || m_bEditMode )
	{
		m_bQueryForConvType = true;
		SetRbIntValuesFromConvType();
	}
}

BOOL CAutoConfigDlg::OnInitDialog()
{
	BOOL bRet = CAutoConfigPropertyPage::OnInitDialog();

	// if we're editing a converter and it's already in the repository...
	if( m_bEditMode && m_bIsInRepository )
	{
		// ... then change the "Save in System Repository" button to read "Update"
		SetDlgItemText(IDC_BTN_ADD_TO_REPOSITORY,_T("Update in System Repository"));
	}

	return bRet;
}

// some sub-classes (e.g. CC) want to reset the state of the spec identifier
//  and ConvType whenever the browse button is clicked so we don't mistakenly
//  configure the new table with the same details as the old one
void CAutoConfigDlg::ResetFields()
{
	m_eConversionType = m_eOrigConvType;
	m_strConverterIdentifier.Empty();
	m_lProcessTypeFlags = 0;
}

// turn on (or off) the visibility of the controls for determining the ConvType
void CAutoConfigDlg::ConvTypeVisibility(BOOL bVisible)
{
	SetVisibility(IDC_STATIC_FUNC_EXPECTS, bVisible);
	SetVisibility(IDC_STATIC_FUNC_RETURNS, bVisible);
	SetVisibility(IDC_RB_LHS_BYTES, bVisible);
	SetVisibility(IDC_RB_LHS_UNICODE, bVisible);
	SetVisibility(IDC_RB_RHS_BYTES, bVisible);
	SetVisibility(IDC_RB_RHS_UNICODE, bVisible);

	if( !bVisible )
		m_bQueryForConvType = false;
}

void CAutoConfigDlg::SetVisibility(UINT nID, BOOL bVisible)
{
	CWnd* pDlgItemWnd = GetDlgItem(nID);
	if( pDlgItemWnd != 0 )
		pDlgItemWnd->ShowWindow((bVisible) ? SW_SHOW : SW_HIDE);
}

void CAutoConfigDlg::SetDlgItemText(int nID, LPCTSTR str)
{
	if( GetSafeHwnd() != NULL )
	{
		CWnd* pWnd = GetDlgItem(nID);
		if( pWnd != 0 )
			pWnd->SetWindowText(str);
	}
}

#define ConvTypeUnicode 0
#define ConvTypeLegacy  1

void CAutoConfigDlg::SetRbIntValuesFromConvType()
{
	switch(m_eConversionType)
	{
	case ConvType_Legacy_to_Unicode:
	case ConvType_Legacy_to_from_Unicode:
		{
			m_nLhsExpects = ConvTypeLegacy;
			m_nRhsReturns = ConvTypeUnicode;
			break;
		};
	case ConvType_Unicode_to_Legacy:
	case ConvType_Unicode_to_from_Legacy:
		{
			m_nLhsExpects = ConvTypeUnicode;
			m_nRhsReturns = ConvTypeLegacy;
			break;
		};
	case ConvType_Legacy_to_Legacy:
	case ConvType_Legacy_to_from_Legacy:
		{
			m_nLhsExpects = ConvTypeLegacy;
			m_nRhsReturns = ConvTypeLegacy;
			break;
		};
	case ConvType_Unicode_to_Unicode:
	case ConvType_Unicode_to_from_Unicode:
	default:
		{
			m_nLhsExpects = ConvTypeUnicode;
			m_nRhsReturns = ConvTypeUnicode;
			break;
		};
	};
}

void CAutoConfigDlg::SetConvTypeFromIntValues()
{
	bool bLhsUnicode = (m_nLhsExpects == 0);
	bool bRhsUnicode = (m_nRhsReturns == 0);
	if( bLhsUnicode )
	{
		if( bRhsUnicode )
		{
			m_eConversionType = ConvType_Unicode_to_Unicode;
		}
		else
		{
			m_eConversionType = ConvType_Unicode_to_Legacy;
		}
	}
	else
	{
		if( bRhsUnicode )
		{
			m_eConversionType = ConvType_Legacy_to_Unicode;
		}
		else
		{
			m_eConversionType = ConvType_Legacy_to_Legacy;
		}
	}
}

BOOL CAutoConfigDlg::UpdateData(BOOL bSaveAndValidate)
{
	// if this is being used in the IEncConverterConfig::DisplayTestPage method, then
	//  this dialog won't be present (but the class will exist so we can do things
	//  like InitializeEncConverter) and so we don't want to (and can't) do UpdateData
	//  because IsWnd will fail (and in any case, the data is already up-to-date from the caller).
	if( GetSafeHwnd() == NULL )
		return true;
	return CWnd::UpdateData(bSaveAndValidate);
}

BOOL CAutoConfigDlg::WorkAroundCompilerBug_OnApply()
{
	// check the configured data...
	if( UpdateData() )
	{
		// if it was okay, then go ahead and try to instantiate the converter (this'll display
		//  an error if the configuration is bad).
		PtrIEncConverter aEC = InitializeEncConverter();
		if( !!aEC )
		{
			// finally, if we're in 'edit mode' and the converter is already in the repository
			//  then re-add it to save changes to the repository (i.e. make the default behavior
			//  to save to repository in this case, rather than requiring the user to explicitly
			//  click the Update... button)
			if( m_bEditMode && m_bIsInRepository )
			{
				// I'm not calling "Remove" before calling AddConversionMap here
				//  (c.f. BnClickedBtnAddToRepositoryEx), because the only thing that might have
				//  changed without explicitly clicking 'Update...' is the ConvType, and the
				//  converter Identifier. However, the real benefit of using Remove is to purge
				//  possibly stranded FriendlyNames and Encoding IDs. But these can't be changed
				//  without clicking the Update button. But if the Update button is pressed, then
				//  Remove will have been called by that handler... (to make a short story long :-)
				AddConverterMapping();
			}

			// since it is rather non-obvious what a temporary converter is, let's make sure that's
			//  what the user really wanted before continuing (it would have been nice to do this
			//  only when the OK button is clicked, but for some bizaar reason, this is not possible
			//  with MFC (i.e. there is no way to distinguish between the user clicking Apply vs. OK!?)
			// But only do it once per instantiation (by using m_bQueryToUseTempConverter to trigger it)
			if( !m_bIsInRepository && m_bQueryToUseTempConverter )
			{
				m_bQueryToUseTempConverter = false; // stop the nagging
				int nRes = MessageBox(_T("You are creating a temporary converter that will only be available to this one calling program and only this one time.\n\nDo you want to make it permanent instead?"), MB_YESNOCANCEL);
				if( nRes == IDYES )
					BnClickedBtnAddToRepositoryEx();
			}

			return CPropertyPage::OnApply();
		}
	}

	return false;
}

void CAutoConfigDlg::DoDataExchange(CDataExchange* pDX)
{
	CAutoConfigPropertyPage::DoDataExchange(pDX);

	if( m_bQueryForConvType )
	{
		if( !pDX->m_bSaveAndValidate )
			ConvTypeVisibility(true);

		DDX_Control(pDX, IDC_RB_LHS_UNICODE, m_ctlLhsExpects);
		DDX_Control(pDX, IDC_RB_RHS_UNICODE, m_ctlRhsReturns);
		DDX_Radio(pDX,IDC_RB_LHS_UNICODE,m_nLhsExpects);
		DDX_Radio(pDX,IDC_RB_RHS_UNICODE,m_nRhsReturns);

		// even if we're not using the controls, we still have to calculate the ConvType when saving (which
		//  happens during OnKillActive)
		if( pDX->m_bSaveAndValidate )
			SetConvTypeFromIntValues();
	}
}

BEGIN_MESSAGE_MAP(CAutoConfigDlg, CAutoConfigPropertyPage)
	ON_BN_CLICKED(IDC_BTN_ADD_TO_REPOSITORY, OnBnClickedBtnAddToRepository)
	ON_BN_CLICKED(IDC_RB_LHS_UNICODE, OnBnClickedBytesUnicodeOptions)
	ON_BN_CLICKED(IDC_RB_RHS_UNICODE, OnBnClickedBytesUnicodeOptions)
	ON_BN_CLICKED(IDC_RB_LHS_BYTES, OnBnClickedBytesUnicodeOptions)
	ON_BN_CLICKED(IDC_RB_RHS_BYTES, OnBnClickedBytesUnicodeOptions)
END_MESSAGE_MAP()

CString GetDirNoSlash(const CString& strFSpec)
{
	if( strFSpec.IsEmpty() )
		return strFSpec;

	TCHAR	drive[_MAX_DRIVE];
	TCHAR	dir[_MAX_DIR];
	TCHAR	name[_MAX_FNAME];
	TCHAR	ext[_MAX_EXT];
	_tsplitpath_s( (LPCTSTR)strFSpec, drive, dir, name, ext );

	CString strDir = drive;
	strDir += dir;

	if( strDir.Right(1) == _T("\\") )
		strDir = strDir.Left(strDir.GetLength() - 1);
	return strDir;
}

CString GetDir(const CString& strFSpec)
{
	CString strDir = GetDirNoSlash(strFSpec);

	if( strDir.Right(1) != _T("\\") )
		strDir += _T("\\");

	return strDir;
}

// CAutoConfigDlg message handlers
BOOL CAutoConfigDlg::OnKillActive( )
{
	// we want to all the user to switch between the tabs at will, so just return true here.
	return true;
}

void CAutoConfigDlg::OnBnClickedBytesUnicodeOptions()
{
	SetModified();
}

// add a handler in the base class for the "Add to Repository" button
void CAutoConfigDlg::OnBnClickedBtnAddToRepository()
{
	// get the latest values
	if( UpdateData() )
		BnClickedBtnAddToRepositoryEx();
}

HRESULT CAutoConfigDlg::BnClickedBtnAddToRepositoryEx()
{
	// each sub-class tells us what to use as the default friendly name (e.g.
	//  ICU Transliterator will give the Transliterator ID, CC will use the
	//  filename, etc.)
	CString strDef;
	if( m_strFriendlyName.IsEmpty() )
		strDef = DefaultFriendlyName();
	else
		strDef = m_strFriendlyName;

	// Since we're going to query for certain pieces of information which in many cases
	//  the converters themselves can tell us (once they've been initialized), go ahead
	//  and initialize a dummy converter at this point (so that that initialization occurs)
	// (we don't really need the pointer, but the side effect of this call is that the
	//  encoding IDs, Conversion Type, and process types may be updated.)
	// However, some sub-classes (e.g. FallbackEncConverter) want to *do nothing* here, so
	//  make it a virtual function which can be overridden.
	if( !CallInitializeEncConverter() )
		return S_OK;    // not really okay, but the InitializeEncConverter already displayed the error msgs

	// put up a dialog to query the user for the name (at least--an "Advanced" button
	//  can also be used for the Encoding IDs and Process Type)
	CQueryConverterNameDlg dlg(m_pECs,this,strDef,m_strLhsEncodingId,m_strRhsEncodingId,m_lProcessTypeFlags);

	HRESULT hr = S_OK;
	if( dlg.DoModal() == IDOK )
	{
		// see if doing Remove would be beneficial (i.e. if, for example, the encoding IDs changed, then
		//  doing Remove will clean up the about to be stranded entries)
		BOOL bRemove =
			(
				(m_strFriendlyName != dlg.FriendlyName)
			||  (m_strLhsEncodingId != dlg.LhsEncodingID)
			||  (m_strRhsEncodingId != dlg.RhsEncodingID)
			);

		// update the values from those the dialog box queried
		m_strFriendlyName = dlg.FriendlyName;
		m_strLhsEncodingId = dlg.LhsEncodingID;
		m_strRhsEncodingId = dlg.RhsEncodingID;
		m_lProcessTypeFlags = dlg.ProcessType;

		// if it already exists, then remove it first (which cleans up possibly changed Encoding IDs which
		//  otherwise will be stranded).
		if( m_bEditMode && bRemove )
			m_pECs->Remove(CComVariant(strDef)); // don't care about return

		// finally, add it to the repository
		//  (again, some sub-classes do something different at this point, so call a virtual function)
		hr = AddConverterMapping();
	}

	return hr;
}

BOOL CAutoConfigDlg::CallInitializeEncConverter()
{
	// Since we're going to query for certain pieces of information which in many cases
	//  the converters themselves can tell us (once they've been initialized), go ahead
	//  and initialize a dummy converter at this point (so that that initialization occurs)
	// (we don't really need the pointer, but the side effect of this call is that the
	//  encoding IDs, Conversion Type, and process types may be updated.
	PtrIEncConverter aEC = InitializeEncConverter();
	return !!aEC;
}

HRESULT CAutoConfigDlg::AddConverterMapping()
{
	// remove any existing converter by the name we're about to give it (probably not necessary).
	ASSERT(!m_strFriendlyName.IsEmpty());
	CComVariant var = m_strFriendlyName;
	m_pECs->Remove(var);

	// if it was originally under a different name...
	if( !m_strOriginalFriendlyName.IsEmpty() )
	{
		// ... remove that too (this one probably *is* necessary)
		var = m_strOriginalFriendlyName;
		m_pECs->Remove(var);
	}

	// have the sub-classes do their thing to add it
	HRESULT hr = AddConverterMappingSub();

	// if it worked, then ...
	if( ProcessHResult(hr, m_pECs) )
	{
		// ... indicate that now this converter is in the repository
		m_bIsInRepository = true;

		// and save the name so we can clear it out if need be later
		m_strOriginalFriendlyName = m_strFriendlyName;
	}

	return hr;
}

HRESULT CAutoConfigDlg::AddConverterMappingSub()
{
	return m_pECs->AddConversionMap
					(
						m_strFriendlyName.AllocSysString(),
						m_strConverterIdentifier.AllocSysString(),
						m_eConversionType,
						ImplType().AllocSysString(),            // get from sub-class
						m_strLhsEncodingId.AllocSysString(),
						m_strRhsEncodingId.AllocSysString(),
						(ProcessTypeFlags)m_lProcessTypeFlags
					);
}

const CComBSTR strTempName = _T("Temporary Converter");

IEncConverter*  CAutoConfigDlg::InitializeEncConverter()
{
	CWaitCursor x;  // Perl takes a *long* time!

	// for most converters, the identifier is necessary (must override if not)
	if( m_strConverterIdentifier.IsEmpty() )
		return 0;

	// for most, simply create one from scratch, and initialize it via the converter identifier.
	// (but see CmpdAutoConfigDlg.cpp for a specialization for the case when the converter
	//  is already known to be in the repository)
	PtrIEncConverter aEC;
	HRESULT hr = aEC.CoCreateInstance(ProgramID()); // from sub-class

	if( ProcessHResult(hr, aEC) && !!aEC )
	{
		// pass the flag that says we're "adding" the converter (i.e. to the repository). This will
		//  force a "Load" (which may "correct" a bad eConvType type and update the encoding ids as well.
		CComBSTR strConverterIdentifier = m_strConverterIdentifier;
		CComBSTR strLhsEncID = m_strLhsEncodingId;
		CComBSTR strRhsEncID = m_strRhsEncodingId;
		hr = aEC->Initialize(
						strTempName,
						strConverterIdentifier,
						&strLhsEncID,
						&strRhsEncID,
						&m_eConversionType,
						&m_lProcessTypeFlags,
						0,
						0,
						VARIANT_TRUE
					);

		if( !ProcessHResult(hr, aEC) )
			return 0;

		// in case they were updated
		m_strLhsEncodingId = strLhsEncID;
		m_strRhsEncodingId = strRhsEncID;
	}

	return aEC.Detach();    // return by detaching it
}

IEncConverter*  CAutoConfigDlg::InsureApplyAndInitializeEncConverter()
{
	// in case it wasn't done, update the variables now (i.e. do "OnApply")
	if( IsModified() && !OnApply() )
		return NULL;

	return InitializeEncConverter();
}

// add to the base class the ability to support a "RecentlyUsed" folder for converters like
//  Regular expressions and ICU custom transliterators. Those sub-classes will put the
//  list of recently used (good) converter ids into the registry for rememory

// keys from EncCnvtrs/EncConverters.cs, but too hard to get (in CPP) from managed assemblies
const CString CNVTRS_ROOT = _T("SOFTWARE\\SIL\\SilEncConverters40");

CString CAutoConfigDlg::GetRegKey()
{
	CString strRegKey;
	strRegKey.Format(CNVTRS_ROOT + _T("\\ConvertersSupported\\%s\\RecentlyUsed"), ImplType());
	return strRegKey;
}

// called by the base class to remember a good converter identifier.
void CAutoConfigDlg::AddToRecentlyUsed(const CString& strRecentlyUsed)
{
	CRegKey keyRecentConverterIDs;
	CString strRegKey = GetRegKey();
	if( keyRecentConverterIDs.Create(HKEY_CURRENT_USER, strRegKey) == ERROR_SUCCESS )
	{
		keyRecentConverterIDs.SetStringValue(strRecentlyUsed, _T(""));
	}
}

void CAutoConfigDlg::RemFromRecentlyUsed(const CString& strRecentlyUsed)
{
	CRegKey keyRecentConverterIDs;
	CString strRegKey = GetRegKey();
	if( keyRecentConverterIDs.Open(HKEY_CURRENT_USER, strRegKey) == ERROR_SUCCESS )
	{
		keyRecentConverterIDs.DeleteValue(strRecentlyUsed);
	}
}

// the sub-class can call this method to fill out a combo box with the values stored in
//  the recently used list.
void CAutoConfigDlg::EnumRecentlyUsed(CComboBox& cbRecentlyUsed)
{
	cbRecentlyUsed.Clear();
	CRegKey keyRecentConverterIDs;
	CString strRegKey = GetRegKey();
	if( keyRecentConverterIDs.Open(HKEY_CURRENT_USER, strRegKey) == ERROR_SUCCESS )
	{
		DWORD dwIndex = 0;
		BOOL bStop = false;
		do
		{
			DWORD dwValueType = 0, cbName = _MAX_PATH;
			TCHAR lpName[_MAX_PATH];    lpName[0] = 0;
			LONG lVal = RegEnumValue(keyRecentConverterIDs,dwIndex++,lpName,&cbName,0,&dwValueType,0,0);
			if( (lVal == ERROR_SUCCESS) || (lVal == ERROR_MORE_DATA) )
			{
				// skip the default value
				if( _tcslen(lpName) > 0 )
				{
					TRACE(_T("Found: (%s)"), lpName);
					if( cbRecentlyUsed.FindStringExact(0,lpName) < 0 )
						cbRecentlyUsed.AddString(lpName);
				}
			}
			else
				bStop = true;
		} while( !bStop );

		// select the first one so there's something in it.
		cbRecentlyUsed.SetCurSel(0);
	}
}

const CString& strEmpty = _T("");    // default string parameter
