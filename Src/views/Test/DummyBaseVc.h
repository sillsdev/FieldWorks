/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DummyBaseVc.h
Responsibility:
Last reviewed:

	Basic do-nothing view constructor; also a simple one for paragraph sequences.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef DUMMYBASEVC_H_INCLUDED
#define DUMMYBASEVC_H_INCLUDED

#pragma once

#include "testViews.h"
#include "guiddef.h"

namespace TestViews
{
	// this class is basically the same as VwBaseVc, except for some methods.
	// Duplicated here so that we don't need the overhead of AfLib
	class DummyBaseVc : public IVwViewConstructor
	{
	public:
		DummyBaseVc()
		{
			// COM object behavior
			m_cref = 1;
			ModuleEntry::ModuleAddRef();
		}

		virtual ~DummyBaseVc()
		{
			ModuleEntry::ModuleRelease();
		}
		STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
		{
			AssertPtr(ppv);
			if (!ppv)
				return WarnHr(E_POINTER);
			*ppv = NULL;

			if (riid == IID_IUnknown)
				*ppv = static_cast<IUnknown *>(this);
			else if (riid == IID_IVwViewConstructor)
				*ppv = static_cast<IVwViewConstructor *>(this);
			else
				return E_NOINTERFACE;

			AddRef();
			return NOERROR;
		}
		STDMETHOD_(ULONG, AddRef)(void)
		{
			return InterlockedIncrement(&m_cref);
		}
		STDMETHOD_(ULONG, Release)(void)
		{
			long cref = InterlockedDecrement(&m_cref);
			if (cref == 0) {
				m_cref = 1;
				delete this;
			}
			return cref;
		}
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			Assert(false);
			return E_NOTIMPL;
		}
		STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
		{
			Assert(false);
			return E_NOTIMPL;
		}
		STDMETHOD(DisplayVariant)(IVwEnv * pvwenv, int tag, VARIANT v, int frag,
			ITsString ** pptss)
		{
			Assert(false);
			return E_NOTIMPL;
		}

		STDMETHOD(DisplayPicture)(IVwEnv * pvwenv,  int hvo, int tag, int val, int frag,
			IPicture ** ppPict)
		{
			*ppPict = NULL;
			Assert(false);
			return E_NOTIMPL;
		}

		STDMETHOD(UpdateProp)(IVwSelection * pvwsel, HVO vwobj, int tag, int frag,
			ITsString * ptssVal, ITsString ** pptssRepVal)
		{
			Assert(false);
			return E_NOTIMPL;
		}
		STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
		{
			Assert(false);
			return E_NOTIMPL;
		}
		STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
			int tag, int frag, int ihvoMin)
		{
			Assert(false);
			return E_NOTIMPL;
		}
		STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss)
		{
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			SmartBstr bstr(bstrGuid);
			// {D10C12E2-BF77-4f1d-84EA-79BC983FAB0C}
			const GUID ZeroLengthOrc =
			{ 0xd10c12e2, 0xbf77, 0x4f1d, { 0x84, 0xea, 0x79, 0xbc, 0x98, 0x3f, 0xab, 0xc } };
			StrUniBufSmall stuGuid((wchar*)&ZeroLengthOrc, 8);

			if (bstr.Equals(stuGuid.Bstr()))
			{
				StrUni stuRep(L"");
				return qtsf->MakeString(stuRep.Bstr(), TestViews::g_wsEng, pptss);
			}
			StrUni stuRep(L"<obj>");
			return qtsf->MakeString(stuRep.Bstr(), TestViews::g_wsEng, pptss);
		}
		STDMETHOD(DoHotLinkAction)(BSTR bstrData, HVO hvoOwner, PropTag tag, ITsString * ptss,
			int ichObj)
		{
			Assert(false);
			return E_NOTIMPL;
		}
		STDMETHOD(GetIdFromGuid)(ISilDataAccess * psda, GUID * pguid, HVO * phvo)
		{
			Assert(false);
			return E_NOTIMPL;
		}
		STDMETHOD(DisplayEmbeddedObject)(IVwEnv * pvwenv, HVO hvo)
		{
			Assert(false);
			return E_NOTIMPL;
		}
		STDMETHOD(UpdateRootBoxTextProps)(ITsTextProps * pttp, ITsTextProps ** ppttp)
		{
			*ppttp = NULL;
			return S_OK;
		}

protected:
		long m_cref;
	};

#define kfragStText 1
#define kfragStTxtPara 2
#define kfragDiv 3
#define kfragAlignedTable 4
#define kfragOffsetTable 5
#define kfragStTxtPara2 6

#define ktagSection_Heading 692
#define ktagSection_Content 693
#define ktagBook_Footnotes 694
#define ktagFootnote_Paragraphs 695

	class DummyParaVc : public DummyBaseVc
	{
	private:
		int m_iPara;

	public:
		DummyParaVc()
		{
			m_dympParaTopMargin = 0;
			m_dympDivTopMargin = 0;
			m_dympParaBottomMargin = 0;
			m_dympDivBottomMargin = 0;
			m_dympLineSpace = 0;
			m_iPara = 0;
			m_dympParaBottomMarginInitial = 0;
			m_dympParaTopMarginInitial = 0;
		}
		int m_dympParaTopMargin;
		int m_dympDivTopMargin;
		int m_dympParaBottomMargin;
		int m_dympDivBottomMargin;
		int m_dympLineSpace;
		int m_dympParaTopMarginInitial;		// Top margin for first m_nInitialParas
		int m_dympParaBottomMarginInitial;	// Bottom margin for first m_nInitialParas
		int m_nInitialParas;	// Number of initial paragraphs that get different margins

		void SetMargins(IVwEnv* pvwenv, int dympTop, int dympBottom)
		{
			if (dympTop != 0)
				pvwenv->put_IntProperty(ktptMarginTop, ktpvMilliPoint, dympTop);
			if (dympBottom != 0)
				pvwenv->put_IntProperty(ktptMarginBottom, ktpvMilliPoint, dympBottom);
		}
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragDiv: // Some division bigger than an StText, display the StTexts.
				SetMargins(pvwenv, m_dympDivTopMargin, m_dympDivBottomMargin);
				if (m_dympLineSpace)
					pvwenv->put_IntProperty(ktptLineHeight, ktpvMilliPoint, m_dympLineSpace);
				pvwenv->OpenDiv();
				pvwenv->AddObjProp(ktagSection_Heading, this, kfragStText);
				pvwenv->CloseDiv();
				SetMargins(pvwenv, m_dympDivTopMargin, m_dympDivBottomMargin);
				pvwenv->OpenDiv();
				pvwenv->AddObjProp(ktagSection_Content, this, kfragStText);
				pvwenv->CloseDiv();
				break;
			case kfragStText: // An StText, display paragraphs not lazily.
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfragStTxtPara);
				break;
			case kfragStTxtPara2: // StTxtPara, display contents, pass this to AddStringProp
				// FALL THROUGH
			case kfragStTxtPara: // StTxtPara, display contents
				{
				if (m_iPara < m_nInitialParas)
					SetMargins(pvwenv, m_dympParaTopMarginInitial, m_dympParaBottomMarginInitial);
				else
					SetMargins(pvwenv, m_dympParaTopMargin, m_dympParaBottomMargin);
				m_iPara++;

				ISilDataAccessPtr qsda;
				pvwenv->get_DataAccess(&qsda);
				ITsTextPropsPtr qttp;
				HRESULT hr = qsda->get_UnknownProp(hvo, kflidStPara_StyleRules, (IUnknown**)&qttp);
				if (SUCCEEDED(hr))
					pvwenv->put_Props(qttp);

				pvwenv->OpenMappedPara();
				pvwenv->AddStringProp(kflidStTxtPara_Contents, (frag == kfragStTxtPara2 ? this : NULL));
				pvwenv->CloseParagraph();
				}
				break;
			case kfragAlignedTable: // table with two columns, showing the same StText in both
			case kfragOffsetTable: // same but second column is offset.
				{
				VwLength vlTable; // we use this to specify that the table takes 100% of the width.
				vlTable.nVal = 10000;
				vlTable.unit = kunPercent100;

				VwLength vlColumn;
				vlColumn.nVal = 5000; // each column is half the width
				vlColumn.unit = kunPercent100;
				pvwenv->OpenTable(2, // Two columns.
					vlTable, // Table uses 100% of available width.
					0, // Border thickness.
					kvaLeft, // Default alignment.
					kvfpVoid, // No border.
					kvrlNone,
					0, //No space between cells.
					0, //No padding inside cells.
					false); // multi-column select
				pvwenv->MakeColumns(2, vlColumn);
				pvwenv->OpenTableBody();
				pvwenv->OpenTableRow();

				// Display paragraph in the first cell
				pvwenv->OpenTableCell(1,1);

				pvwenv->AddObjProp(ktagSection_Heading, this, kfragStText);

				pvwenv->CloseTableCell();
				pvwenv->OpenTableCell(1,1);

				// This would bizarre in non-test code, since it doesn't immediately precede
				// opening the exact box it affects. Here it will offset the first paragraph,
				// whatever that turns out to be.
				if (frag == kfragOffsetTable)
					pvwenv->put_IntProperty(ktptMarginTop, ktpvMilliPoint, 3000);

				pvwenv->AddObjProp(ktagSection_Content, this, kfragStText);

				pvwenv->CloseTableCell();
				pvwenv->CloseTableRow();
				pvwenv->CloseTableBody();
				pvwenv->CloseTable();
				}
				break;
			}
			return S_OK;
		}
		STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
		{
			*pdyHeight = 15 + hvo * 2; // just give any arbitrary number
			return S_OK;
		}
		STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
			int tag, int frag, int ihvoMin)
		{
			return S_OK;
		}

		// For purposes of TestVwSelection::testEmbeddedObjects, we return a short, special string for any object.
		STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss)
		{
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			qtsf->MakeStringRgch(L"XX", 2, 1, pptss);
			return S_OK;
		}

	};

	// View constructor that adds all paragraphs of a StText to one Views code paragraph.
	// This is a simple way to simulate having multiple VwTxtSrc strings in one paragraph.
	class DummySquishedVc : public DummyParaVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragStText:
				pvwenv->OpenMappedPara();
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfragStTxtPara);
				pvwenv->CloseParagraph();
				break;
			case kfragStTxtPara:
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				break;
			}
			return S_OK;
		}
	};
}

#endif /*DUMMYBASEVC_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
