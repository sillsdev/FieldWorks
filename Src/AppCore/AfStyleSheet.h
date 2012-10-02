/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfStylesheet.h
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
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef AF_STYLESHEET_INCLUDED
#define AF_STYLESHEET_INCLUDED 1
/*:End Ignore*/

class AfStylesheet;
typedef ComSmartPtr<AfStylesheet> AfStylesheetPtr;

class AfDbStylesheet;
typedef ComSmartPtr<AfDbStylesheet> AfDbStylesheetPtr;

/*----------------------------------------------------------------------------------------------
	AfStylesheet
	This class provides a stylesheet, maintaining a vector of styles. It also maintains
	a parallel copy of the style information in object/property form in an ISilDataAccess.
	It uses a pointer to the ISilDataAccess, the hvo of the owning object, and the tag
	specifying the owner's property which holds the collection of StStyle objects.
	Correspondences:	@code{
	Main items: StStyle.
	Interesting properties:
		Name - the key for looking up the style in the style sheet.
		BasedOn - used to maintain derived styles.
		Next - the basis for implementing GetNextStyle.
		Type - tells us whether to allow paragraph-level info.
		IsBuiltIn - tells us if style is a predefined style
		IsModified - tells us if user has modified the predefined style
		Rules - binary data, actually a TsTextProps, contains the style rules.
	}

	To use AfStyleSheet, derive your own stylesheet class from this base class, or use
	AfDbStylesheet below.
	A subclass initialization method should call this class's Init().
	(Actually, if Init() is made public, AfStylesheet could be instantiated as is, if desired.)
//REVIEW JohnT(BryanW): After you have reviewed everything else, should Init() be public now
//and AfStylesheet not restriced to being a base class?
	#h3{Hungarian: asts}
----------------------------------------------------------------------------------------------*/
class AfStylesheet : public IVwStylesheet
{
public:
	// Generic constructor.
	AfStylesheet();
	// Generic destructor.
	~AfStylesheet();

	//:>****************************************************************************************
	//:>	IUnknown methods.
	//:>****************************************************************************************
	// Get a pointer to the interface identified as iid.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	// Add a reference to the AfStylesheet.
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	// Release a reference to the AfStylesheet.
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	//:>****************************************************************************************
	//:>	IVwStylesheet methods.
	//:>****************************************************************************************
	// Gets the name of the default paragraph style to use as the base for new styles (Usually
	// "Normal")
	STDMETHOD(GetDefaultBasedOnStyleName)(BSTR * pbstrNormal);
	// Gets the style name that is the default style to use for the given context
	STDMETHOD(GetDefaultStyleForContext)(int nContext, ComBool fCharStyle, BSTR * pbstrStyleName);
	// Store a style; in particular, store the pttp in the cache.
	STDMETHOD(PutStyle)(BSTR bstrName, BSTR bstrUsage, HVO hvoStyle, HVO hvoBasedOn,
		HVO hvoNext, int nType, ComBool fBuiltIn, ComBool fModified, ITsTextProps * pttp);
	// Retrieve the properties in ppttp for the style named prgchName.
	STDMETHOD(GetStyleRgch)(int cch, OLECHAR * prgchName, ITsTextProps ** ppttp);
	// Get the next style that will be used if the user types a CR at the end of this paragraph.
	// If the input is null, return "Normal".
	STDMETHOD(GetNextStyle)(BSTR bstrName, BSTR * pbstrNext);
	// Get the basedOn style name (in pbstrBasedOn) for the style named bstrName.
	STDMETHOD(GetBasedOn)(BSTR bstrName, BSTR * pbstrBasedOn);
	// Get the type (in pnType) for the style named bstrName.
	STDMETHOD(GetType)(BSTR bstrName, int * pnType);
	// Get the context (in pnContext) for the style named bstrName.
	STDMETHOD(GetContext)(BSTR bstrName, int * pnContext);
	// Is the style a predefined style?
	STDMETHOD(IsBuiltIn)(BSTR bstrName, ComBool * pfBuiltIn);
	// Was the (predefined) style changed by the user?
	STDMETHOD(IsModified)(BSTR bstrName, ComBool * pfModified);
	// ENHANCE EberhardB: Add method for usage

	// Return the associated Data Access object, kept in member variable m_qsda.
	STDMETHOD(get_DataAccess)(ISilDataAccess ** ppsda);
	// Create a new style and return its HVO.
	STDMETHOD(MakeNewStyle)(HVO * phvoNewStyle);
	// Delete the style with id hvoStyle. It is assumed that the caller has already changed
	// any references to this style.
	STDMETHOD(Delete)(HVO hvoStyle);
	STDMETHOD(get_CStyles)(int * pcttp);
	STDMETHOD(get_NthStyle)(int ihvo, HVO * phvo);
	STDMETHOD(get_NthStyleName)(int ihvo, BSTR * pbstrStyleName);
	STDMETHOD(get_NormalFontStyle)(ITsTextProps ** ppttp);
	STDMETHOD(get_IsStyleProtected)(BSTR bstrName, ComBool * pfProtected);
	STDMETHOD(CacheProps)(int cch, OLECHAR * prgchName, HVO hvoStyle, ITsTextProps * pttp);

	// Return a reference to the vector of styles, m_vhcStyles.
	HvoClsidVec & GetStyles()
	{
		return m_vhcStyles;
	}

// REVIEW JohnT(BryanW): Any need to resurrect GetStylesOwner() ?
	/* apparently this is no longer needed
	// Return the HVO of the style owner, stored in m_hvoStylesOwner.
	HVO GetStylesOwner()
	{
		return m_hvoStylesOwner;
	} */

	HRESULT UndoInsertedStyle(HVO hvoStyle, StrUni stuName);
	HRESULT UndoDeletedStyle(HVO hvoStyle);

	// Compute our hashmap, m_hmstuttpStyles, including effects on all derived styles.
	// This is public so it can be called by the undo actions.
	void ComputeDerivedStyles();

	// Return the line height (ascender + descender) for the given style
	int GetLineHeight(BSTR bstrName, int ws, HDC hdc);

protected:
	long m_cref; // Reference count.

	HvoClsidVec m_vhcStyles; // Vector of HvoClsid objects; one for each style.
	typedef ComHashMapStrUni<ITsTextProps> StyleMap; // Map from style name to the TsTextProps
									// object that represents the style. Hungarian hmstuttp.
	StyleMap m_hmstuttpStyles; // Hash map of StrUni (Style name) to its TsTextProps.

	ISilDataAccessPtr m_qsda; // Ptr to SilDataAccess; used to get and set style properties.

	// The HVO of the object that owns the styles in the iSilDataAccess
	HVO m_hvoStylesOwner;

	// The field ID of the owner(m_hvoStylesOwner)'s collection of StStyles.
	PropTag m_tagStylesList;

	// The style returned by "get_NormalFontStyle"
	ITsTextPropsPtr m_qttpNormalFont;

	// this Init() should be called by subclasses
	void Init(ISilDataAccess * psda, HVO hvoStylesOwner, PropTag tagStylesList);

	// Return an HVO for a newly created style, using the associated database. Insert the new
	// style into its owner property.
	virtual HRESULT GetNewStyleHVO(HVO * phvo);

	// Compute the actual meaning of a style given its definition (pttp) and the definitions
	// of the other styles.
	void ComputeDerivedStyle(int clevMax, int istyle, StrUni stuName,
		ITsTextProps * pttp, ITsTextProps ** ppttpDerived);

	void SetupUndoStylesheet(bool fFirst);
	void SetupUndoInsertedStyle(HVO hvoStyle, StrUni stuName);
	void SetupUndoDeletedStyle(HVO hvoStyle);

}; //:> End of AfStylesheet.


/*----------------------------------------------------------------------------------------------
	This class provides a stylesheet which is hooked into a CustViewDa database
	which stores the styles.

	@h3{Hungarian: dsts}
----------------------------------------------------------------------------------------------*/
class AfDbStylesheet : public AfStylesheet
{
	typedef AfStylesheet SuperClass;

public:
	// Initialize the class and load the owner's collection of styles from the db
	void Init(AfLpInfo * plpi,
			HVO hvoStylesOwner,
			PropTag tagStylesList = kflidLangProject_Styles);

	// Second step of Init, to get the basic vector of style objects.
	// Also used to reload styles during FullRefresh.
	virtual void LoadStyles(CustViewDa * pcvd, AfLpInfo * plpi);

protected:

};

#endif // AF_STYLESHEET_INCLUDED
