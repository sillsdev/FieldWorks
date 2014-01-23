/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestStVc.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a view constructor for a standard view of a structured text
	Todo JohnT: handle tables. Handle numbered and bulleted paragraphs, if any special
		action here is needed. Handle paragraph's explicit tag.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

TestStVc::TestStVc()
{
}

TestStVc::~TestStVc()
{
}

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	Here a text is displayed by displaying its paragraphs;
	and a paragraph is displayed by invoking its style rule, making a paragraph,
	and displaying its contents.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TestStVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	try
	{
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
					ITsStrFactoryPtr qtsf;
					qtsf.CreateInstance(CLSID_TsStrFactory);
					ITsStringPtr qtssMissing;
					CheckHr(qtsf->MakeStringRgch(L"<no paragraphs>", 15, 0, &qtssMissing));
					CheckHr(pvwenv->AddString(qtssMissing));
					break;
				}
				CheckHr(pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfrPara));
			}
			break;
		case kfrPara:
			{ // BLOCK
				// Invoke the paragraph's style rule if any
				ISilDataAccessPtr qsda;
				CheckHr(pvwenv->get_DataAccess(&qsda));
				ITsTextPropsPtr qttp;
				CheckHr(qsda->get_UnknownProp(hvo, kflidStPara_StyleRules, IID_ITsTextProps,
					(void **) &qttp));
				if (qttp)
					CheckHr(pvwenv->put_Props(qttp));
				// And make the paragraph containing the paragraph contents.
				CheckHr(pvwenv->OpenParagraph());
				CheckHr(pvwenv->AddStringProp(kflidStTxtPara_Contents));
				CheckHr(pvwenv->CloseParagraph());
			}
			break;
		}
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
	return S_OK;
}
