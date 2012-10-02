/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfStylesheet.cpp
Responsibility: Larry Waswick
Last reviewed: never

Description:
	These classes provides a stylesheet (a collection of styles) which is managed by
	AfStylesDlg and passed to the initialization method of a VwRootBox to control
	the appearance of things.
	They inherit from IVwStyleSheet in order to communicate with the Views code.

	AfStylesheet is targetted at using ISilDataAccess to maintain a collection of StStyles
	owned in a particular property of a particular object.
	AfDbStylesheet adds knowledge of how to use the extra capabilities of CustViewDa
	to initially load the styles from the database.

	Correspondences:
	Main items: StStyle.
	Interesting properties:
		Name - the key for looking up the style in the style sheet.
		BasedOn - used to maintain derived styles.
		Next - the basis for implementing GetNextStyle.
		Type - tells us whether to allow paragraph-level info.
		Rules - binary data, actually a TsTextProps, contains the style rules.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Constructor/Destructor
//:>********************************************************************************************

// Generic constructor. Just set the reference count to 1.
AfStylesheet::AfStylesheet()
{
	m_cref = 1;
}

// Generic destructor.
AfStylesheet::~AfStylesheet()
{
}

// Init should be called by subclasses
void AfStylesheet::Init(ISilDataAccess * psda, HVO hvoStylesOwner, PropTag tagStylesList)
{
	AssertPtr(psda);
	m_qsda = psda;
	m_hvoStylesOwner = hvoStylesOwner;
	m_tagStylesList = tagStylesList;
}

static DummyFactory g_fact(_T("SIL.AppCore.AfStyleSheet"));


//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
// Get a pointer to the interface identified as iid.
STDMETHODIMP AfStylesheet::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwStylesheet)
		*ppv = static_cast<IVwStylesheet *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwStylesheet);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	IVwStylesheet methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Gets the name of the default paragraph style to use as the base for new styles (Usually
	"Normal")

	@param pbstrNormal Out Name of the style that is the "normal" style
	@return S_OK if successful, or E_FAIL if not.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::GetDefaultBasedOnStyleName(BSTR * pbstrNormal)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrNormal);

	// For the AfStyleSheet just return the style name "Normal"
	*pbstrNormal = ::SysAllocString(g_pszwStyleNormal);
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Gets the style name that is the default style to use for the given context

	@param nContext The context
	@param fCharStyle whether the style is a character style or not.
	@param pbstrStyleName Out Name of the style that is the default for the context
	@return S_OK if successful, or E_FAIL if not.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::GetDefaultStyleForContext(int nContext, ComBool fCharStyle,
													 BSTR * pbstrStyleName)
{
	BEGIN_COM_METHOD;
	return GetDefaultBasedOnStyleName(pbstrStyleName);
	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Store a style. In particular, store the pttp in the cache.

	@param bstrName The style name
	@param bstrUsage The usage information for the style
	@param hvoStyle  The style to be stored.
	@param hvoBasedOn What the style is based on.
	@param hvoNext The next Style.
	@param nType The Style type.
	@param fBuiltIn True if predefined style
	@param fModfied True if user has modified predefined style
	@param pttp TextProps, contains the formatting of the style
	@return S_OK, E_FAIL, or Throwable Error
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::PutStyle(BSTR bstrName, BSTR bstrUsage,
	HVO hvoStyle, HVO hvoBasedOn, HVO hvoNext, int nType, ComBool fBuiltIn, ComBool fModified,
	ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pttp);
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_INVALIDARG);
	m_qttpNormalFont.Clear();  // may have been changed, recompute if needed.

	SetupUndoStylesheet(true);	// to perform ComputeDerivedStyles when undoing

	int istyle;

	// Look for an existing style with id hvoStyle.
	for (istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		if (m_vhcStyles[istyle].hvo == hvoStyle)
		{
			// Store the new properties for the associated style in the database.
			CheckHr(m_qsda->SetUnicode(hvoStyle, kflidStStyle_Name,
				bstrName, BstrLen(bstrName)));
			CheckHr(m_qsda->SetObjProp(hvoStyle, kflidStStyle_BasedOn, hvoBasedOn));
			CheckHr(m_qsda->SetObjProp(hvoStyle, kflidStStyle_Next, hvoNext));
			CheckHr(m_qsda->SetInt(hvoStyle, kflidStStyle_Type, nType));
			CheckHr(m_qsda->SetInt(hvoStyle, kflidStStyle_IsBuiltIn, fBuiltIn));
			CheckHr(m_qsda->SetInt(hvoStyle, kflidStStyle_IsModified, fModified));
			// TODO EberhardB: Add usage
			m_qsda->SetUnknown(hvoStyle, kflidStStyle_Rules, pttp);

			// Recompute our hashmap, including effects on all derived styles.
			ComputeDerivedStyles();
			// StrUni stuName(sbstrName.Bstr(), sbstrName.Length());
			// m_hmstuttpStyles.Insert(stuName, pttp, true); // Overwrite.

			SetupUndoStylesheet(false);	// to perform ComputeDerivedStyles when redoing

			return S_OK;
		}
	}

	// Insert the new style into the vector of Styles.
	HvoClsid hc;
	hc.clsid = kclidStStyle;
	hc.hvo = hvoStyle;
	m_vhcStyles.Replace(m_vhcStyles.Size(), m_vhcStyles.Size(), &hc, 1);

	StrUni stuName(bstrName);
	SetupUndoInsertedStyle(hvoStyle, stuName);

	// Set its properties in the cache and db.
	CheckHr(m_qsda->SetUnicode(hvoStyle, kflidStStyle_Name,
		bstrName, BstrLen(bstrName)));
	CheckHr(m_qsda->SetObjProp(hvoStyle, kflidStStyle_BasedOn,
		hvoBasedOn));
	CheckHr(m_qsda->SetObjProp(hvoStyle, kflidStStyle_Next, hvoNext));
	CheckHr(m_qsda->SetInt(hvoStyle, kflidStStyle_Type, nType));
	CheckHr(m_qsda->SetInt(hvoStyle, kflidStStyle_IsBuiltIn, fBuiltIn));
	CheckHr(m_qsda->SetInt(hvoStyle, kflidStStyle_IsModified, fModified));
	// TODO EberhardB: Add usage
	m_qsda->SetUnknown(hvoStyle, kflidStStyle_Rules, pttp);

	// Recompute our hashmap, including effects on all derived styles.
	ComputeDerivedStyles();

	SetupUndoStylesheet(false);	// to perform ComputeDerivedStyles when redoing

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
} //:> End of AfStylesheet::PutStyle.

/*----------------------------------------------------------------------------------------------
	Cache a (subtly modified) new set of text props without recording an undo action.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::CacheProps(int cch, OLECHAR * prgchName, HVO hvoStyle,
	ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchName, cch);
	ChkComArgPtr(pttp);
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_INVALIDARG);

	StrUni stuName(prgchName, cch);
	m_hmstuttpStyles.Insert(stuName, pttp, true);
	IVwCacheDaPtr qvcd;
	CheckHr(m_qsda->QueryInterface(IID_IVwCacheDa, (void **) &qvcd));
	CheckHr(qvcd->CacheUnknown(hvoStyle, kflidStStyle_Rules, pttp));

	// Recompute our hashmap, including effects on all derived styles.
	ComputeDerivedStyles();

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}


/*----------------------------------------------------------------------------------------------
	Retrieve the properties in ppttp for the style named prgchName.

	@param cch Length of the style name
	@param prgchName The style name
	@param ppttp TextProps to get the properties from.
	@return S_OK, E_FAIL, or Throwable Error
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::GetStyleRgch(int cch, OLECHAR * prgchName, ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchName, cch);
	ChkComOutPtr(ppttp);

	// Retrieve the pttp in the cache.
	StrUni stuKey(prgchName, cch);
	ITsTextPropsPtr qttp;
	if (m_hmstuttpStyles.Retrieve(stuKey, qttp))
	{
		*ppttp = qttp.Detach();
		return S_OK;
	}
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the next style that will be used if the user types a CR at the end of this paragraph.
	If the input is null, return "Normal".

	@param bstrName Name of the style for this paragraph.
	@param pbstrNext Out Name of the style for the next paragraph that is returned.
	@return S_OK if successful, or E_UNEXPECTED if not.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::GetNextStyle(BSTR bstrName, BSTR * pbstrNext)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComArgPtr(pbstrNext);
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_UNEXPECTED);

	if (!BstrLen(bstrName))
	{
		*pbstrNext = ::SysAllocString(g_pszwStyleNormal);
		return S_OK;
	}

	*pbstrNext = NULL;
	HVO hvoNext = 0;
	SmartBstr sbstrName;

	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		CheckHr(m_qsda->get_UnicodeProp(m_vhcStyles[istyle].hvo, kflidStStyle_Name,
			&sbstrName));
		if (sbstrName.Equals(bstrName, BstrLen(bstrName)))
		{
			CheckHr(m_qsda->get_ObjectProp(m_vhcStyles[istyle].hvo, kflidStStyle_Next,
				&hvoNext));
			CheckHr(m_qsda->get_UnicodeProp(hvoNext, kflidStStyle_Name, pbstrNext));
			return S_OK;
		}
	}
	// Copy/Cut/Paste does not transfer style definitions, although it transfers the style
	// properties in TsStrings just fine.  As a result, this error can happen without being (an
	// unknown) internal program bug.  Even after we fix Copy/Cut/Paste (if we ever do), old
	// data may still have undefined styles lurking inside.  -- SteveMc  :-(
	*pbstrNext = ::SysAllocString(bstrName);

	// calling MessageBox clears m_qsel->m_qtsbProp 4 levels up on the stack.  This is
	// unbelievably WIERD!!!  It does not do this if i comment out the MessageBox call, but
	// does it regardless of how may local StrUni variables i use here if i call MessageBox.
//	StrUni stuTitle(kstidMissingStyleTitle);
//	StrUni stuMsg;
//	StrUni stuFmt(kstidMissingStyleMsg);
//	stuMsg.Format(stuFmt.Chars(), bstrName, g_pszwStyleNormal);
//	::MessageBoxW(NULL, stuMsg.Chars(), stuTitle.Chars(), MB_OK | MB_ICONINFORMATION);
//	*pbstrNext = ::SysAllocString(g_pszwStyleNormal);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the basedOn style name (in pbstrBasedOn) for the style named bstrName.

	@param bstrName Name of the style.
	@param pbstrBasedOn Out Name of the BasedOn style..
	@return S_OK if successful, or E_UNEXPECTED if not.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::GetBasedOn(BSTR bstrName, BSTR * pbstrBasedOn)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComArgPtr(pbstrBasedOn);
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_INVALIDARG);

	*pbstrBasedOn = NULL;
	HVO hvoBasedOn = 0;
	SmartBstr sbstrName;

	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		CheckHr(m_qsda->get_UnicodeProp(m_vhcStyles[istyle].hvo, kflidStStyle_Name,
			&sbstrName));
		if (sbstrName.Equals(bstrName, BstrLen(bstrName)))
		{
			CheckHr(m_qsda->get_ObjectProp(m_vhcStyles[istyle].hvo, kflidStStyle_BasedOn,
				&hvoBasedOn));
			CheckHr(m_qsda->get_UnicodeProp(hvoBasedOn, kflidStStyle_Name, pbstrBasedOn));
			return S_OK;
		}
	}
	ThrowInternalError(E_INVALIDARG); // "bstrName" should be the name of a valid style.

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the type (in pnType) for the style named bstrName.

	@param bstrName Name of the style.
	@param pnType Out Style type of the given style.
	@return S_OK if successful, or E_UNEXPECTED if not.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::GetType(BSTR bstrName, int * pnType)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pnType);
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_INVALIDARG);

	*pnType = 0; // kstParagraph by default (See FmtGenDlg.h).
	SmartBstr sbstrName;

	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		CheckHr(m_qsda->get_UnicodeProp(m_vhcStyles[istyle].hvo, kflidStStyle_Name,
			&sbstrName));
		if (sbstrName.Equals(bstrName, BstrLen(bstrName)))
		{
			CheckHr(m_qsda->get_IntProp(m_vhcStyles[istyle].hvo, kflidStStyle_Type, pnType));
			return S_OK;
		}
	}
	ThrowInternalError(E_INVALIDARG); // "bstrName" should be the name of a valid style.

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the context (in pnContext) for the style named bstrName.

	@param bstrName Name of the style.
	@param pnContext Out Style context of the given style.
	@return S_OK if successful, or E_UNEXPECTED if not.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::GetContext(BSTR bstrName, int * pnContext)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pnContext);
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_INVALIDARG);

	*pnContext = 0; // General (0) by default.
	SmartBstr sbstrName;

	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		CheckHr(m_qsda->get_UnicodeProp(m_vhcStyles[istyle].hvo, kflidStStyle_Name,
			&sbstrName));
		if (sbstrName.Equals(bstrName, BstrLen(bstrName)))
		{
			CheckHr(m_qsda->get_IntProp(m_vhcStyles[istyle].hvo, kflidStStyle_Context, pnContext));
			return S_OK;
		}
	}
	ThrowInternalError(E_INVALIDARG); // "bstrName" should be the name of a valid style.

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Is the style named bstrName a predefined style?

	@param bstrName Name of the style.
	@param pfBuiltIn [Out] true if style is a predefined style
	@return S_OK if successful, or E_INVALIDARG if no style name bstrName exists.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::IsBuiltIn(BSTR bstrName, ComBool * pfBuiltIn)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pfBuiltIn);
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_UNEXPECTED);

	SmartBstr sbstrName;

	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		CheckHr(m_qsda->get_UnicodeProp(m_vhcStyles[istyle].hvo, kflidStStyle_Name,
			&sbstrName));
		if (sbstrName.Equals(bstrName, BstrLen(bstrName)))
		{
			int nBuiltIn;
			CheckHr(m_qsda->get_IntProp(m_vhcStyles[istyle].hvo, kflidStStyle_IsBuiltIn, &nBuiltIn));
			*pfBuiltIn = static_cast<bool>(nBuiltIn);
			return S_OK;
		}
	}
	ThrowInternalError(E_INVALIDARG); // "bstrName" should be the name of a valid style.

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Was the (predefined) style named bstrName changed by the user?

	@param bstrName Name of the style.
	@param pfModified [Out] true if style was modified by user
	@return S_OK if successful, or E_INVALIDARG if no style name bstrName exists.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::IsModified(BSTR bstrName, ComBool * pfModified)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pfModified);
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_UNEXPECTED);

	SmartBstr sbstrName;

	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		CheckHr(m_qsda->get_UnicodeProp(m_vhcStyles[istyle].hvo, kflidStStyle_Name,
			&sbstrName));
		if (sbstrName.Equals(bstrName, BstrLen(bstrName)))
		{
			int nModified;
			CheckHr(m_qsda->get_IntProp(m_vhcStyles[istyle].hvo, kflidStStyle_IsModified, &nModified));
			*pfModified = static_cast<bool>(nModified);
			return S_OK;
		}
	}
	ThrowInternalError(E_INVALIDARG); // "bstrName" should be the name of a valid style.

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

// ENHANCE EberhardB: Add method for usage

/*----------------------------------------------------------------------------------------------
	Return the associated Data Access object from m_qsda.

	@param ppsda Out The associated Data Access object
	@return S_OK
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::get_DataAccess(ISilDataAccess ** ppsda)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsda);

	*ppsda = m_qsda;
	AddRefObj(*ppsda);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Create a new style and return its HVO.

	@param phvoNewStyle Out HVO of the newly created style.
	@return S_OK if successful.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::MakeNewStyle(HVO * phvoNewStyle)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvoNewStyle);

	m_qttpNormalFont.Clear();  // may have been changed, recompute if needed.

	return GetNewStyleHVO(phvoNewStyle);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Delete the style with id hvoStyle. It is assumed that the caller has already changed any
	references to this style.

	@param hvoStyle HVO of the style to be deleted.
	@return S_OK if successful.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::Delete(HVO hvoStyle)
{
	BEGIN_COM_METHOD;
	AssertPtrN(m_qsda);
	if (!m_qsda)
		ThrowInternalError(E_INVALIDARG);
	m_qttpNormalFont.Clear();  // may have been changed, recompute if needed.

	SmartBstr sbstrName;
	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		if (hvoStyle == m_vhcStyles[istyle].hvo)
		{
			SetupUndoStylesheet(true); // to perform ComputeDerivedStyles when undoing

			CheckHr(m_qsda->DeleteObjOwner(m_hvoStylesOwner, hvoStyle,
				m_tagStylesList, -1));
				// if m_hvoStylesOwner is never initialized, apparently this is benign

			SetupUndoDeletedStyle(hvoStyle);

			// Also delete the style from our own member variables.
			m_vhcStyles.Delete(istyle);
			CheckHr(m_qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstrName));
			StrUni stuName(sbstrName.Chars(), sbstrName.Length());
			m_hmstuttpStyles.Delete(stuName);

			SetupUndoStylesheet(false);	// to perform ComputeDerivedStyles when redoing

			return S_OK;
		}
	}
	ThrowInternalError(E_INVALIDARG); // "bstrName" should be the name of a valid style.

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get number of styles in sheet.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::get_CStyles(int * pcttp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcttp);

	*pcttp = m_vhcStyles.Size();

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the HVO of the Nth style (in an arbitrary order).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::get_NthStyle(int ihvo, HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);
	if ((unsigned)ihvo >= (unsigned)m_vhcStyles.Size())
		ThrowInternalError(E_INVALIDARG);

	*phvo = m_vhcStyles[ihvo].hvo;

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the name of the Nth style (in an arbitrary order).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::get_NthStyleName(int ihvo, BSTR * pbstrSyleName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrSyleName);
	if ((unsigned)ihvo >= (unsigned)m_vhcStyles.Size())
		ThrowInternalError(E_INVALIDARG);

	HVO hvo = m_vhcStyles[ihvo].hvo;
	CheckHr(m_qsda->get_UnicodeProp(hvo, kflidStStyle_Name, pbstrSyleName));

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	A special style that contains only the parts of "Normal" that relate to the Font tab.
	This is automatically maintained as "Normal" is edited. If there is no "Normal" style,
	sets *ppttp to NULL. This is not currently considered an error.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::get_NormalFontStyle(ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppttp);

	if (!m_qttpNormalFont)
	{
		ITsTextPropsPtr qttpNormal;
		StrUni stuNormal(g_pszwStyleNormal);
		CheckHr(GetStyleRgch(stuNormal.Length(),
			const_cast<OLECHAR *>(stuNormal.Chars()), &qttpNormal));
		if (!qttpNormal)
			return S_OK;
		SmartBstr sbstrFontProps;
		CheckHr(qttpNormal->GetStrPropValue(ktptWsStyle, &sbstrFontProps));
		if (sbstrFontProps.Length())
		{
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			CheckHr(qtpb->SetStrPropValue(ktptWsStyle, sbstrFontProps));
			CheckHr(qtpb->GetTextProps(&m_qttpNormalFont));
		}
	}
	*ppttp = m_qttpNormalFont;
	AddRefObj(*ppttp);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Return true if the given style is one that is protected within the style sheet.
	This is a default implementation for AfStylesheet. Specialized style sheets may derive
	their own version.

	@param stuStyleName style to be checked.
	@param pfProtected pointer to the flag to be set.
	@return S_OK
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfStylesheet::get_IsStyleProtected(BSTR bstrName, ComBool * pfProtected)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfProtected);

	int i;

	for (i = 0; i < m_vhcStyles.Size(); i++)
	{
		int ntmp;
		HVO hvoStyle = m_vhcStyles[i].hvo;
		SmartBstr sbstr;

		CheckHr(m_qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstr));
		if (sbstr.Equals(bstrName))
		{
			CheckHr(m_qsda->get_IntProp(hvoStyle, kflidStStyle_IsBuiltIn, &ntmp));
			if (static_cast<bool>(ntmp))
			{
				*pfProtected = true;
				break;
			}
		}
	}

	END_COM_METHOD(g_fact, IID_IVwStylesheet)
}

//:>********************************************************************************************
//:>	Other Methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	AfDbStylesheet
	This class provides a stylesheet which is hooked into a CustViewDa database
	which stores the styles.

	Initialize the class and get the basic vector of style objects.

	@param plpi  The Language Project Info that holds the Data Access
	@param hvoStylesOwner  HVO of the owning Language Project or Scripture, etc.
	@param tagStylesList  the kflid of the owner's styles collection property
							(optional, defaults to kflidLangProject_Styles, which is
							appropriate if the owner is a language project)
----------------------------------------------------------------------------------------------*/
void AfDbStylesheet::Init(AfLpInfo * plpi, HVO hvoStylesOwner,
	PropTag tagStylesList /*kflidLangProject_Styles*/)
//REVIEW JohnT(BryanW):
// After settling that the CustViewDa provides the unique functionality of AfDbStylesheet,
// I'd prefer the first param to be simply a CustViewDa, but we are still dependent
// on the plpi later on in LoadStyles() !
// Is such a change possible, and would it be better to eliminate the LpInfo dependency?
// Of course, such a change will require a few lines to change in Notebook and List Editor.
{
	AssertPtr(plpi);
	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	SuperClass::Init(qcvd, hvoStylesOwner, tagStylesList);

	LoadStyles(qcvd, plpi);
}

/*----------------------------------------------------------------------------------------------
	Second step of Init, to get the basic vector of style objects.
----------------------------------------------------------------------------------------------*/
void AfDbStylesheet::LoadStyles(CustViewDa * pcvd, AfLpInfo * plpi)
{
	AssertPtr(pcvd);
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);

	// Create a RecordSpec for loading data into the cache.
	// Make sure it has the writing system factory it needs.
	RecordSpecPtr qrsp;
	qrsp.Attach(NewObj RecordSpec(kclidStStyle, 0));
	ILgWritingSystemFactoryPtr qwsf;
	pdbi->GetLgWritingSystemFactory(&qrsp->m_qwsf);
	AssertPtr(qrsp->m_qwsf);

	qrsp->AddField(true, kstidNoLabel, kflidStStyle_Name,
		kftUnicode, kwsAnal, 0, kFTVisAlways, kFTReqReq);
	qrsp->AddField(true, kstidNoLabel, kflidStStyle_BasedOn,
		kftRefAtomic, kwsAnal, 0, kFTVisAlways, kFTReqReq);
	qrsp->AddField(true, kstidNoLabel, kflidStStyle_Next,
		kftRefAtomic, kwsAnal, 0, kFTVisAlways, kFTReqReq);
	qrsp->AddField(true, kstidNoLabel, kflidStStyle_Type,
		kftEnum, kwsAnal, 0, kFTVisAlways, kFTReqReq);
	qrsp->AddField(true, kstidNoLabel, kflidStStyle_Rules,
		kftTtp, kwsAnal, 0, kFTVisAlways, kFTReqReq);
	qrsp->AddField(true, kstidNoLabel, kflidStStyle_IsBuiltIn,
		kftEnum, kwsAnal, 0, kFTVisAlways, kFTReqReq);
	qrsp->AddField(true, kstidNoLabel, kflidStStyle_IsModified,
		kftEnum, kwsAnal, 0, kFTVisAlways, kFTReqReq);
	// TODO EberhardB: Add usage
	// Finish it off.
	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);
	qrsp->SetMetaNames(qmdc);

	// Store the RecordSpec in a UserViewSpec.
	UserViewSpecPtr quvs;
	quvs.Attach(NewObj UserViewSpec);
	ClsLevel clev(kclidStStyle, 0);
	quvs->m_hmclevrsp.Insert(clev, qrsp, true);

	// Load the main vector of style objects, i.e., the ids of the StStyle objects.
	pcvd->SetTags(m_tagStylesList, 0);
	pcvd->LoadMainItems(m_hvoStylesOwner, m_vhcStyles);

	// Load the info about them into the hashmaps maintained by CacheViewDa, i.e., load the
	// field data of the StStyles.

	/* Sample query and results of the following LoadData:
	select itm.id,itm.Type,itm.Name,itm.BasedOn,itm1.Txt,itm.Next,itm2.Txt,itm.Type,itm.Rules,
	itm.IsBuiltIn, itm.IsModified
	from StStyle_ as itm
	left outer join CmPossibility_Name as itm1 on itm1.obj = itm.BasedOn and itm1.ws = 740664001
	left outer join CmPossibility_Name as itm2 on itm2.obj = itm.Next and itm2.ws = 740664001
	where itm.id in (1692,1693)

	id   Type Name       BasedOn Txt   Next  Txt   Type Rules      IsBuiltIn IsModified
	1692 0    TestStyle  NULL    NULL  NULL  NULL  0    0x00000000 1         0
	1693 1    TestStyle2 NULL    NULL  NULL  NULL  1    0x00000000 1         1
	*/

	pcvd->LoadData(m_vhcStyles, quvs, NULL, false);
	ComputeDerivedStyles();
	m_qttpNormalFont.Clear();  // may have been changed, recompute if needed.
}
//:> End of AfDbStylesheet

/*----------------------------------------------------------------------------------------------
	Return an HVO for a newly created style, using the associated ISilDataAccess.
	Insert the new style into its owner property (e.g. the Styles property of LangProject).

	@param phvoNewStyle Out HVO of the style that is created.
	@return S_OK if successful.
----------------------------------------------------------------------------------------------*/
//REVIEW SharonC,JohnT(BryanW): This function moved to AfStylesheet from AfDbStylesheet
// per John's comment. It is a good default. It isn't yet clear whether the WorldPad
// stylesheet would still need to override it.

HRESULT AfStylesheet::GetNewStyleHVO(HVO * phvoNewStyle)
{
	return m_qsda->MakeNewObject(kclidStStyle, m_hvoStylesOwner, m_tagStylesList,
		-1, phvoNewStyle);
}

/*----------------------------------------------------------------------------------------------
	Compute our hashmap, m_hmstuttpStyles, including effects on all derived styles.
----------------------------------------------------------------------------------------------*/
void AfStylesheet::ComputeDerivedStyles()
{
	m_hmstuttpStyles.Clear();
	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		HVO hvoStyle = m_vhcStyles[istyle].hvo;
		StrUni stuName;
		SmartBstr sbstr;
		m_qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstr);
		stuName = sbstr.Chars();

		ITsTextPropsPtr qttpDerived;
		if (!m_hmstuttpStyles.Retrieve(stuName, qttpDerived))
		{
			// It might already be there because we had to compute it to figure out
			// a substyle.
			IUnknownPtr qunkTtp;
			CheckHr(m_qsda->get_UnknownProp(hvoStyle, kflidStStyle_Rules, &qunkTtp));
			ITsTextPropsPtr qttp;
			CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
			// To prevent infinite loops don't follow an inheritance chain more than this long.
			ComputeDerivedStyle(m_vhcStyles.Size(), istyle, stuName, qttp, &qttpDerived);
			m_hmstuttpStyles.Insert(stuName, qttpDerived);
		}
	}
	m_qttpNormalFont.Clear();  // may have been changed, recompute if needed.
}

/*----------------------------------------------------------------------------------------------
	Compute the actual meaning of a style given its definition (pttp) and the definitions of
	the other styles.  As a side effect, this computes the actual meaning of any style on which
	this is based that is not already computed.

	@param clevMax Size of the vector of styles (m_vhcStyles)
	@param istyle Index of the style to compute.
	@param stuName Style name (Not used here)
	@param pttp Text Properties of this style itself.
	@param ppttpDerived Out Derived text properties to be returned.
----------------------------------------------------------------------------------------------*/
void AfStylesheet::ComputeDerivedStyle(int clevMax, int istyle, StrUni stuName,
	ITsTextProps  * pttp, ITsTextProps ** ppttpDerived)
{
	HVO hvoBasedOn;
	CheckHr(m_qsda->get_ObjectProp(m_vhcStyles[istyle].hvo, kflidStStyle_BasedOn,
		&hvoBasedOn));
	if ((!hvoBasedOn) || hvoBasedOn == m_vhcStyles[istyle].hvo)
	{
		// Not based on anything, or based on itself; use as is.
		*ppttpDerived = pttp;
		pttp->AddRef();
		return;
	}

	// Find the style it is based on.
	int istyleBase;
	for (istyleBase = 0; istyleBase < m_vhcStyles.Size(); istyleBase++)
	{
		if (hvoBasedOn == m_vhcStyles[istyleBase].hvo)
			break;
	}
	if (istyleBase >= m_vhcStyles.Size())
	{
		// Missing base style: treat as based on nothing.
		*ppttpDerived = pttp;
		pttp->AddRef();
		return;
	}

	// Compute the effect of the base style. First check for infinite loop.
	if (clevMax < 0)
		ThrowInternalError(E_INVALIDARG,"Loop in style based-on");
	SmartBstr sbstrBaseName;
	StrUni stuBaseName;
	CheckHr(m_qsda->get_UnicodeProp(m_vhcStyles[istyleBase].hvo, kflidStStyle_Name,
		&sbstrBaseName));
	stuBaseName.Assign(sbstrBaseName.Chars(), sbstrBaseName.Length());
	ITsTextPropsPtr qttpBase;
	if (!m_hmstuttpStyles.Retrieve(stuBaseName, qttpBase))
	{
		// We haven't already computed it. Do so now.
		ITsTextPropsPtr qttpBaseDefn;
		IUnknownPtr qunkTtp;
		CheckHr(m_qsda->get_UnknownProp(m_vhcStyles[istyleBase].hvo, kflidStStyle_Rules, &qunkTtp));
		CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttpBaseDefn));
		ComputeDerivedStyle(clevMax - 1, istyleBase, stuBaseName, qttpBaseDefn,
			&qttpBase);
		m_hmstuttpStyles.Insert(stuBaseName, qttpBase);
	}

	// OK, we got the base style. Now compute the effect of our own defn.
	FwStyledText::ComputeInheritance(qttpBase, pttp, ppttpDerived);
} //:> End of AfStylesheet::ComputeDerivedStyle.

/*----------------------------------------------------------------------------------------------
	Create an undo action reflecting the need for a ComputeDerivedStyles if anything is
	undone. We surround a block of changes with two of these undo-actions: the first is
	run in undo mode, the last in redo mode. This (hopefully) keeps us from calling
	ComputeDerivedStyles unnecessarily often but makes sure it gets done after all the other
	changes have happened.
----------------------------------------------------------------------------------------------*/
void AfStylesheet::SetupUndoStylesheet(bool fFirst)
{
	IActionHandlerPtr qacth;
	CheckHr(m_qsda->GetActionHandler(&qacth));
	if (!qacth)
		return;

	VwUndoDaPtr quda = dynamic_cast<VwUndoDa *>(m_qsda.Ptr());
	if (!quda)
		return;

	VwUndoStylesheetActionPtr quact;
	quact.Attach(NewObj VwUndoStylesheetAction(quda, this, fFirst));

	CheckHr(qacth->AddAction(quact));
}

/*----------------------------------------------------------------------------------------------
	Create an undo action reflecting the creation of a new style.
----------------------------------------------------------------------------------------------*/
void AfStylesheet::SetupUndoInsertedStyle(HVO hvoStyle, StrUni stuName)
{
	IActionHandlerPtr qacth;
	CheckHr(m_qsda->GetActionHandler(&qacth));
	if (!qacth)
		return;

	VwUndoDaPtr quda = dynamic_cast<VwUndoDa *>(m_qsda.Ptr());
	if (!quda)
		return;

	VwUndoStyleActionPtr quact;
	quact.Attach(NewObj VwUndoStyleAction(quda, this, hvoStyle, stuName, false));

	CheckHr(qacth->AddAction(quact));
}

/*----------------------------------------------------------------------------------------------
	Create an undo action reflection the deletion of a style.
----------------------------------------------------------------------------------------------*/
void AfStylesheet::SetupUndoDeletedStyle(HVO hvoStyle)
{
	IActionHandlerPtr qacth;
	CheckHr(m_qsda->GetActionHandler(&qacth));
	if (!qacth)
		return;

	VwUndoDaPtr quda = dynamic_cast<VwUndoDa *>(m_qsda.Ptr());
	if (!quda)
		return;

	SmartBstr sbstrName;
	CheckHr(m_qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstrName));
	StrUni stuName(sbstrName.Chars(), sbstrName.Length());
	VwUndoStyleActionPtr quact;
	quact.Attach(NewObj VwUndoStyleAction(quda, this, hvoStyle, stuName, true));

	CheckHr(qacth->AddAction(quact));
}

/*----------------------------------------------------------------------------------------------
	Undo the insertion of a style.
----------------------------------------------------------------------------------------------*/
HRESULT AfStylesheet::UndoInsertedStyle(HVO hvoStyle, StrUni stuName)
{
	for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
	{
		if (hvoStyle == m_vhcStyles[istyle].hvo)
		{
			m_vhcStyles.Delete(istyle);

//			CheckHr(m_qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstrName));
//			StrUni stuName(sbstrName.Chars(), sbstrName.Length());
			m_hmstuttpStyles.Delete(stuName);

			// Don't do this until things are thoroughly set up; the UndoStylesheetActions
			// should take care of it.
//			ComputeDerivedStyles();
			return S_OK;
		}
	}
	Assert(false);
	return E_UNEXPECTED;
}

/*----------------------------------------------------------------------------------------------
	Undo the deletion of a style.
----------------------------------------------------------------------------------------------*/
HRESULT AfStylesheet::UndoDeletedStyle(HVO hvoStyle)
{
	HvoClsid hc;
	hc.clsid = kclidStStyle;
	hc.hvo = hvoStyle;
	m_vhcStyles.Replace(m_vhcStyles.Size(), m_vhcStyles.Size(), &hc, 1);

	// Don't do this until things are thoroughly set up; the UndoStylesheetActions should
	// take care of it.
//	ComputeDerivedStyles();

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Return the line height (ascender + descender) for the given style

	@param bstrName Name of the style.
	@param ws Writing system for the language
	@param hdc Handle to a DC
	@return height of the line.
----------------------------------------------------------------------------------------------*/
int AfStylesheet::GetLineHeight(BSTR bstrName, int ws, HDC hdc)
{
	AssertBstr(bstrName);

	IVwPropertyStorePtr qvps;
	qvps.CreateInstance(CLSID_VwPropertyStore);
	CheckHr(qvps->putref_Stylesheet(this));
	ITsTextPropsPtr qttpWsStyle;
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws));
	CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, bstrName));
	CheckHr(qtpb->GetTextProps(&qttpWsStyle));
	LgCharRenderProps chrp;
	CheckHr(qvps->get_ChrpFor(qttpWsStyle, &chrp));
	IVwGraphicsWin32Ptr qvg;
	qvg.CreateInstance(CLSID_VwGraphicsWin32);
	qvg->Initialize(hdc); // puts the DC in the right state
	qvg->SetupGraphics(&chrp);
	int nFontAscent, nFontDescent;
	qvg->get_FontAscent(&nFontAscent);
	qvg->get_FontDescent(&nFontDescent);
	return nFontAscent + nFontDescent;
}
