/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeString.h
Responsibility: Ken Zook
Last reviewed: never

Defines
	AfDeFeString: A field editor to display and edit TsStrings.
	StringVc: A view constructor used by this editor.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_STRINGS_INCLUDED
#define AFDEFE_STRINGS_INCLUDED 1

class StringVc;
typedef GenSmartPtr<StringVc> StringVcPtr;

// Enumeration to provide fragment view identifiers.
enum
{
	kfrAll, // Top level.
	kfrRow, // Displays one writing system row.
};

/*----------------------------------------------------------------------------------------------
	Provides a field editor to display a single string or a vector of strings. It can be used
	for String, Unicode, MultiStrings, or MultiUnicode. MultiStrings must provide at least one
	writing system in a vector as an argument to the constructor. In multi-string mode, each
	alternative is displayed in a separate paragraph starting with a superscript writing system
	abbreviation.When active, it creates a view window to handle the editing.

	@h3{Hungarian: dfs}
----------------------------------------------------------------------------------------------*/
class AfDeFeString : public AfDeFeVw
{
public:
	typedef AfDeFeVw SuperClass;

	AfDeFeString();
	virtual ~AfDeFeString();

	void Init(Vector<int> * pvws = NULL, TptEditable nEditable = ktptIsEditable);

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb);
	void GetCurString(ITsString ** pptss, int * pws);
	virtual FldReq HasRequiredData();

	// This field is always editable.
	// @return True indicating it is editable.
	virtual bool IsEditable()
	{
		return true;
	}
	virtual void SaveCursorInfo();
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur);

protected:
	StringVcPtr m_qsvc; // View constructor for the string.
	ILgWritingSystemFactoryPtr m_qwsf;
};


/*----------------------------------------------------------------------------------------------
	Constructs the view to display a single TsString or a vector of TsStrings.

	@h3{Hungarian: svc}
----------------------------------------------------------------------------------------------*/
class StringVc : public VwBaseVc
{
public:
	typedef VwBaseVc SuperClass;

	StringVc(int flid, Vector<int> * pvws, ITsTextProps * pttp, ILgWritingSystemFactory * pwsf,
		COLORREF clrBkg = -1);

	~StringVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);

	bool IsMulti()
	{
		return m_fMulti;
	}

	Vector<int> WritingSystems()
	{
		return m_vws;
	}

	void SetWritingSystem(int ws);

protected:
	bool m_fMulti;		// Flag indicating we are treating this as a vector of MultiStrings.
	Vector<int> m_vws; // A vector of writing system codes (>=1 for MultiStrings, =1 otherwise).
	ComVector<ITsString> m_vqtss; // A vector of abbreviations for each MultiString.
	int m_flid;			// The field id we are displaying.
	ITsTextPropsPtr m_qttp; // The text properties to use when displaying the string.
	COLORREF m_clrBkg;	// Color for the paragraph background.
	ILgWritingSystemFactoryPtr m_qwsf;
	int m_wsUser;		// user interface writing system id.
};

#endif // AFDEFE_STRINGS_INCLUDED
