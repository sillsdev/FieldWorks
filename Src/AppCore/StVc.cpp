/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: StVc.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a view constructor for a standard view of a structured text
	ENHANCE JohnT: handle tables.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact(_T("SIL.AppCore.StVc")); // For END_COM_METHOD macros

StVc::StVc(int wsDefault)
{
	// If we don't initialize this, we will get black borders around paragraphs
	// unless the caller always remembers to set the bacground color.
	m_clrBkg = ::GetSysColor(COLOR_WINDOW);
	m_wsDefault = wsDefault;
	m_fRtl = false;		// We don't support RTL user interfaces.
}

StVc::StVc(LPCOLESTR pszSty, int wsDefault, COLORREF clrBkg)
{
	AssertPsz(pszSty);
	m_stuSty = pszSty;
	m_clrBkg = (clrBkg == -1 ? ::GetSysColor(COLOR_WINDOW) : clrBkg);
	m_wsDefault = wsDefault;
	m_fRtl = false;		// We don't support RTL user interfaces.
}

StVc::StVc(LPCOLESTR pszSty, int wsDefault, COLORREF clrBkg, ILgWritingSystemFactory * pwsf)
{
	AssertPsz(pszSty);
	AssertPtr(pwsf);
	m_stuSty = pszSty;
	m_clrBkg = (clrBkg == -1 ? ::GetSysColor(COLOR_WINDOW) : clrBkg);
	m_wsDefault = wsDefault; // Default for this field.
	IWritingSystemPtr qws;
	CheckHr(pwsf->get_EngineOrNull(wsDefault, &qws));
	if (qws)
	{
		ComBool fRTL;
		CheckHr(qws->get_RightToLeft(&fRTL));
		m_fRtl = bool(fRTL);
	}
	else
	{
		m_fRtl = false;
	}
}

StVc::~StVc()
{
}

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	Here a text is displayed by displaying its paragraphs;
	and a paragraph is displayed by invoking its style rule, making a paragraph,
	and displaying its contents.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP StVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	switch(frag)
	{
	case kfrText:
		{// BLOCK for var skip warnings
			// We need to show something, since the current view code can't handle a property
			// containing no boxes.
			// Review JohnT: should we show an empty string or something?
			// Should we prevent the occurrence of texts with no paragraphs?
			int cpara = 0;
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			if (hvo)
				CheckHr(qsda->get_VecSize(hvo, kflidStText_Paragraphs, &cpara));
			if (!cpara)
			{
				// Either we have no ST object at all, or it is empty of paragraphs. The
				// current view code can't handle either, so stick something in.
				// ENHANCE JohnT: come up with a real solution. This makes it look right,
				// but we should (a) be able to edit and have the first paragraph and
				// if necessary the text itself be created; and (b) if someone adds a real
				// paragraph and/or text in some other view, have them show up.
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				ITsStringPtr qtssMissing;
				int wsUser;
				ILgWritingSystemFactoryPtr qwsf;
				CheckHr(qsda->get_WritingSystemFactory(&qwsf));
				CheckHr(qwsf->get_UserWs(&wsUser));
				CheckHr(qtsf->MakeStringRgch(L"", 0, wsUser, &qtssMissing));
				CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault, m_clrBkg));
				CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, m_stuSty.Bstr()));
				CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
				// This sets the current default writing system from the relevant field spec.
				CheckHr(pvwenv->put_IntProperty(ktptBaseWs, ktpvDefault, m_wsDefault));
				// Set the the directionality based on m_wsDefault.
				CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, m_fRtl));
				CheckHr(pvwenv->AddString(qtssMissing));
				break;
			}
			if (m_fLazy)
				CheckHr(pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, kfrPara));
			else
				CheckHr(pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfrPara));
		}
		break;
	case kfrPara:
		{ // BLOCK
			// Set the current default paragraph writing system from the relevant field spec.
			// It will only be applied if the paragraph itself lacks an writing system.
			CheckHr(pvwenv->put_IntProperty(ktptBaseWs, ktpvDefault, m_wsDefault));
			// Set the the directionality based on m_wsDefault.
			CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, m_fRtl));
			// Invoke the paragraph's style rule if any.
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			ITsTextPropsPtr qttp;
			IUnknownPtr qunkTtp;
			CheckHr(qsda->get_UnknownProp(hvo, kflidStPara_StyleRules, &qunkTtp));
			if (qunkTtp)
				CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));

			// Decide what style to apply to the paragraph.
			// Rules:
			//	1. Apply the paragraph's own style, or "Normal" if it has none.
			//	2. If the creator of the view constructor specified a default style
			//		and background color, invoke those as overrides.
			if (!qttp)
			{
				// Client didn't spec, and nothing on the para, default to normal.
				qttp = NormalStyle();
			}
			CheckHr(pvwenv->put_Props(qttp));
			if (m_stuSty.Length())
			{
				CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, m_stuSty.Bstr()));
				CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault, m_clrBkg));
			}
			// Cause a regenerate when the style changes...this is mainly used for Undo.
			int flid = kflidStPara_StyleRules;
			CheckHr(pvwenv->NoteDependency(&hvo, &flid, 1));
			// And make the paragraph containing the paragraph contents.
			CheckHr(pvwenv->OpenTaggedPara());
			// Insert the label if it is the first paragraph.
			if (m_qtssLabel)
			{
				int lev;
				CheckHr(pvwenv->get_EmbeddingLevel(&lev));
				HVO hvoOuter;
				int ihvoItem;
				PropTag tagOuter;
				CheckHr(pvwenv->GetOuterObject(lev - 1, &hvoOuter, &tagOuter, &ihvoItem));
				if (ihvoItem == 0)
				{
					// The label is not editable.
					CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
					CheckHr(pvwenv->AddString(m_qtssLabel));

				}
			}

			// The body of the paragraph IS editable.
			// ENHANCE JohnT: If we need control over this we'll need to add a member variable.
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
			// NOTE:  If "NULL" is replaced with the this pointer in the following stmt,
			//        the return key does not work in RN, TLE, Worldpad (description fields).
			CheckHr(pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL));
			CheckHr(pvwenv->CloseParagraph());
		}
		break;
	}
	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}

ITsTextProps * StVc::NormalStyle()
{
	if (!m_qttpNormal)
	{
		ITsPropsBldrPtr qtpb;
		qtpb.CreateInstance(CLSID_TsPropsBldr);
		CheckHr(qtpb->SetStrPropValue(kspNamedStyle, SmartBstr(g_pszwStyleNormal)));
		CheckHr(qtpb->GetTextProps(&m_qttpNormal));
	}
	return m_qttpNormal;
}

/*----------------------------------------------------------------------------------------------
	This routine is used to estimate the height of an item. The item will be one of
	those you have added to the environment using AddLazyItems. Note that the calling code
	does NOT ensure that data for displaying the item in question has been loaded.
	The first three arguments are as for Display, that is, you are being asked to estimate
	how much vertical space is needed to display this item in the available width.

	Note that the number of items expanded and laid out is the window height divided by the
	estimated height of an item. Therefore a low estimate leads to laying out too much;
	a high estimate merely leads to multiple expansions of the lazy box (much less expensive).
	Therefore we err on the high side and guess 10 12-point lines per paragraph.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP StVc::EstimateHeight(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdyHeight);
	*pdyHeight = 120;
	END_COM_METHOD(g_fact, IID_ISilDataAccess)
}

/*----------------------------------------------------------------------------------------------
	Static method to create a new structured text. It creates an StText object owned by
	hvoOwner in property tag, then creates an StTxtPara owned by the new StText. It sets the
	contents of the paragraph to be an empty string in the specified writing system, and its style
	to be "Normal".
	ENHANCE JohnT: probably we will identify styles by something other than name.
----------------------------------------------------------------------------------------------*/
HVO StVc::MakeEmptyStText(ISilDataAccess * psda, HVO hvoOwner, PropTag tag, int ws)
{
	HVO hvoRet;
	CheckHr(psda->MakeNewObject(kclidStText, hvoOwner, tag,
		-2, &hvoRet));
	HVO hvoPara;
	CheckHr(psda->MakeNewObject(kclidStTxtPara, hvoRet, kflidStText_Paragraphs,
		0, &hvoPara));

	// Set the style of the paragraph to Normal
	ITsTextPropsPtr qttpNormal;
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetStrPropValue(kspNamedStyle, SmartBstr(g_pszwStyleNormal)));
	CheckHr(qtpb->GetTextProps(&qttpNormal));
	CheckHr(psda->SetUnknown(hvoPara, kflidStPara_StyleRules, qttpNormal));

	// Set its contents to an empty string in the right writing system.
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtssContents;
	CheckHr(qtsf->MakeStringRgch(L"", 0, ws, &qtssContents));
	CheckHr(psda->SetString(hvoPara, kflidStTxtPara_Contents, qtssContents));
	return hvoRet;
}
