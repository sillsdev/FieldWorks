// Listbox.cpp : Defines the entry point for the application.
//

#include "main.h"

/***********************************************************************************************
	Declare and define the test widgets dialog class.
***********************************************************************************************/
class TstWidgetsDlg : public AfDialog
{
public:
	TstWidgetsDlg()
	{
		m_rid = IDD_SDKWIDGETS_DIALOG;
	}
	void SetValues(int iselNormal, int iselWidget)
	{
		m_iselNormal = iselNormal;
		m_iselWidget = iselWidget;
	}
	void GetValues(int & iselNormal, int & iselWidget)
	{
		iselNormal = m_iselNormal;
		iselWidget = m_iselWidget;
	}
	void DoDataExchange(AfDataExchange * padx)
	{
		AssertPtr(padx);

		AfDialog::DoDataExchange(padx);

		DDX_LBIndex(padx, IDC_LISTSTD, m_iselNormal);
		DDX_LBIndex(padx, IDC_LISTFW, m_iselWidget);
	}

protected:
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnCommand(int cid, int nc, HWND hctl);

	int m_iselNormal;
	int m_iselWidget;
};

typedef GenSmartPtr<TstWidgetsDlg> TstWidgetsDlgPtr;


bool TstWidgetsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	const int kcFruit = 15;
	const achar * krgszFruit[kcFruit] =
	{
		_T("apple"), _T("banana"), _T("cherry"), _T("durian"), _T("cantelope"), _T("fig"),
		_T("grape"), _T("blackberry"), _T("blueberry"), _T("prune"), _T("pear"), _T("peach"),
		_T("mango"), _T("nectarine"), _T("orange")
	};
	int iFruit;
	HWND hwnd;
	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	int encEng = 0;

	/*------------------------------------------------------------------------------------------
		Initialize the Combobox controls.
	------------------------------------------------------------------------------------------*/

	HBITMAP hbmp = LoadBitmap(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(IDB_PLANETS));
	HIMAGELIST himl = ImageList_Create(32, 32, ILC_COLORDDB | ILC_MASK, 0, 0);
	ImageList_AddMasked(himl, hbmp, RGB(4, 254, 252));
	int cImages = ImageList_GetImageCount(himl) - 1;

	HWND hwndCombo = GetDlgItem(m_hwnd, IDC_COMBOSTD);
	SendMessage(hwndCombo, CBEM_SETIMAGELIST, 0, (LPARAM)himl);
	COMBOBOXEXITEM cbi;
	cbi.mask = CBEIF_TEXT | CBEIF_INDENT | CBEIF_IMAGE| CBEIF_SELECTEDIMAGE;

	for (iFruit = 0; iFruit < kcFruit; iFruit++)
	{
		cbi.iItem = iFruit;
		cbi.iIndent = cbi.iImage = cbi.iSelectedImage = iFruit % cImages;
		cbi.pszText = const_cast<achar *>(krgszFruit[iFruit]);
		SendMessage(hwndCombo, CBEM_INSERTITEM, 0, (LPARAM)&cbi);
	}
	SendMessage(hwndCombo, CB_SETCURSEL, (WPARAM)5, 0);

	TssComboExPtr qtcb;
	qtcb.Create();
	qtcb->SubclassCombo(m_hwnd, IDC_COMBOFW, 0, true);

	hwnd = qtcb->Hwnd();
	hwndCombo = GetDlgItem(m_hwnd, IDC_COMBOFW);
	Assert(hwnd == hwndCombo);
	SendMessage(hwndCombo, CBEM_SETIMAGELIST, 0, (LPARAM)himl);

	qtsf.CreateInstance(CLSID_TsStrFactory);
	FW_COMBOBOXEXITEM fcbi;
	fcbi.mask = CBEIF_TEXT | CBEIF_INDENT | CBEIF_IMAGE| CBEIF_SELECTEDIMAGE;
	for (iFruit = 0; iFruit < kcFruit; iFruit++)
	{
		StrUni stu = krgszFruit[iFruit];
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), encEng, &fcbi.qtss);
		fcbi.iItem = iFruit;
		fcbi.iIndent = fcbi.iImage = fcbi.iSelectedImage = iFruit % cImages;
		SendMessage(hwndCombo, FW_CBEM_INSERTITEM, 0, (LPARAM)&fcbi);
	}
	SendMessage(hwndCombo, CB_SETCURSEL, (WPARAM)5, 0);

	/*------------------------------------------------------------------------------------------
		Initialize the listbox controls.
	------------------------------------------------------------------------------------------*/
	HWND hwndList = GetDlgItem(m_hwnd, IDC_LISTSTD);
	for (iFruit = 0; iFruit < kcFruit; iFruit++)
		SendMessage(hwndList, LB_ADDSTRING, 0, (LPARAM)krgszFruit[iFruit]);

	TssListBoxPtr qtlb;
	qtlb.Create();
	qtlb->SubclassListBox(GetDlgItem(m_hwnd, IDC_LISTFW));

	hwnd = qtlb->Hwnd();
	hwndList = GetDlgItem(m_hwnd, IDC_LISTFW);
	Assert(hwnd == hwndList);

	encEng = 0;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	for (iFruit = 0; iFruit < kcFruit; iFruit++)
	{
		StrUni stu = krgszFruit[iFruit];
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), encEng, &qtss);
		SendMessage(hwndList, FW_LB_ADDSTRING, 0, (LPARAM)qtss.Ptr());
	}

	/*------------------------------------------------------------------------------------------
		Initialize the editbox controls.
	------------------------------------------------------------------------------------------*/
	HWND hwndEdit = GetDlgItem(m_hwnd, IDC_EDITSTD);
	SendMessage(hwndEdit, WM_SETTEXT , 0,
		(LPARAM)"apple, banana, cherry, durian, cantelope, fig, grape, blackberry, blueberry, "
		"prune, pear, peach, mango, nectarine, orange");
	// REVIEW: We probably need the writing system factory for the correct database. This is a
	// hack to get it to compile, and it works, at least to some degree.
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the registry-based factory.
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	TssEditPtr qte;
	qte.Create();
	qte->SubclassEdit(m_hwnd, IDC_EDITFW, qwsf, wsUser, 0);
	hwnd = qte->Hwnd();
	hwndEdit = GetDlgItem(m_hwnd, IDC_EDITFW);
	Assert(hwnd == hwndEdit);
	qtsf->MakeStringRgch(L"apple, banana, cherry, durian, cantelope, fig, grape, blackberry, "
		L"blueberry, orange", 83, encEng, &qtss);
	SendMessage(hwndEdit, FW_EM_SETTEXT, 0, (LPARAM)qtss.Ptr());

	/*------------------------------------------------------------------------------------------
		Initialize the Treeview controls.
	------------------------------------------------------------------------------------------*/
	HWND hwndTV = GetDlgItem(m_hwnd, IDC_TVSTD);
	TVITEM tvi;
	TVINSERTSTRUCT tvins;

	static HTREEITEM hPrev = (HTREEITEM) TVI_FIRST;
	static HTREEITEM hLevelOneItem = NULL;
	static HTREEITEM hLevelTwoItem = NULL;

	tvi.mask = TVIF_TEXT | TVIF_IMAGE
		| TVIF_SELECTEDIMAGE | TVIF_PARAM;

	tvi.pszText = _T("Fruit");
	tvins.item = tvi;
	tvins.hInsertAfter = hPrev;
	tvins.hParent = TVI_ROOT;
	tvins.item.lParam = 19;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		(LPARAM) (LPTVINSERTSTRUCT) &tvins);
	hLevelOneItem = hPrev;

	tvins.item.pszText = _T("Good Fruit");
	tvins.hInsertAfter = hPrev;
	tvins.hParent = hLevelOneItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		 (LPARAM) (LPTVINSERTSTRUCT) &tvins);
	hLevelTwoItem = hPrev;

	tvins.item.pszText = _T("Apples");
	tvins.hInsertAfter = hPrev;
	tvins.hParent = hLevelTwoItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		 (LPARAM) (LPTVINSERTSTRUCT) &tvins);

	tvins.item.pszText = _T("Oranges");
	tvins.hInsertAfter = hPrev;
	tvins.hParent = hLevelTwoItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		 (LPARAM) (LPTVINSERTSTRUCT) &tvins);

	tvins.item.pszText = _T("Bad Fruit");
	tvins.hInsertAfter = hPrev;
	tvins.hParent = hLevelOneItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		 (LPARAM) (LPTVINSERTSTRUCT) &tvins);
	hLevelTwoItem = hPrev;

	tvins.item.pszText = _T("Figs");
	tvins.hInsertAfter = hPrev;
	tvins.hParent = hLevelTwoItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		 (LPARAM) (LPTVINSERTSTRUCT) &tvins);

	TssTreeViewPtr qtv;
	qtv.Create();
	qtv->SubclassTreeView(GetDlgItem(m_hwnd, IDC_TVFW));
	hwnd = qtv->Hwnd();
	hwndTV = GetDlgItem(m_hwnd, IDC_TVFW);
	Assert(hwnd == hwndTV);

	FW_TVITEMEX fwtvix;
	FW_TVINSERTSTRUCT fwtvins;
	hPrev = (HTREEITEM) TVI_FIRST;
	hLevelOneItem = NULL;
	hLevelTwoItem = NULL;

	fwtvix.mask = TVIF_TEXT | TVIF_IMAGE | TVIF_SELECTEDIMAGE | TVIF_PARAM;

	qtsf->MakeStringRgch(L"Fruit", 5, encEng, &qtss);
	fwtvix.qtss = qtss;
	fwtvins.itemex = fwtvix;
	fwtvins.hInsertAfter = TVI_FIRST;
	fwtvins.hParent = TVI_ROOT;
	fwtvins.itemex.lParam = 19;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		(LPARAM) (LPFW_TVINSERTSTRUCT) &fwtvins);
	hLevelOneItem = hPrev;

	qtsf->MakeStringRgch(L"Good", 5, encEng, &qtss);
	fwtvix.qtss = qtss;
	fwtvins.itemex = fwtvix;
	fwtvins.hInsertAfter = TVI_LAST;
	fwtvins.hParent = hLevelOneItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		(LPARAM) (LPFW_TVINSERTSTRUCT) &fwtvins);
	hLevelTwoItem = hPrev;

	qtsf->MakeStringRgch(L"apple", 5, encEng, &qtss);
	fwtvix.qtss = qtss;
	fwtvins.itemex = fwtvix;
	fwtvins.hInsertAfter = TVI_LAST;
	fwtvins.hParent = hLevelTwoItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		(LPARAM) (LPFW_TVINSERTSTRUCT) &fwtvins);

	qtsf->MakeStringRgch(L"Orange", 5, encEng, &qtss);
	fwtvix.qtss = qtss;
	fwtvins.itemex = fwtvix;
	fwtvins.hInsertAfter = TVI_FIRST;
	fwtvins.hParent = hLevelTwoItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		(LPARAM) (LPFW_TVINSERTSTRUCT) &fwtvins);

	qtsf->MakeStringRgch(L"Bad", 5, encEng, &qtss);
	fwtvix.qtss = qtss;
	fwtvins.itemex = fwtvix;
	fwtvins.hInsertAfter = TVI_FIRST;
	fwtvins.hParent = hLevelOneItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		(LPARAM) (LPFW_TVINSERTSTRUCT) &fwtvins);
	hLevelTwoItem = hPrev;

	qtsf->MakeStringRgch(L"Fig", 5, encEng, &qtss);
	fwtvix.qtss = qtss;
	fwtvins.itemex = fwtvix;
	fwtvins.hInsertAfter = TVI_FIRST;
	fwtvins.hParent = hLevelTwoItem;
	hPrev = (HTREEITEM) SendMessage(hwndTV, TVM_INSERTITEM, 0,
		(LPARAM) (LPFW_TVINSERTSTRUCT) &fwtvins);




	/*------------------------------------------------------------------------------------------
		Initialize the listview controls.
	------------------------------------------------------------------------------------------*/
	hwndList = GetDlgItem(m_hwnd, IDC_LVSTD);
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	lvc.pszText = _T("Fruit");
	lvc.cx = 60;
	ListView_InsertColumn(hwndList, 0, &lvc);
	lvc.pszText = _T("Test");
	lvc.cx = 40;
	ListView_InsertColumn(hwndList, 1, &lvc);
	LVITEM lvi = { LVIF_TEXT };
	for (iFruit = 0; iFruit < kcFruit; iFruit++)
	{
		lvi.iItem = iFruit;
		lvi.pszText = const_cast<achar *>(krgszFruit[iFruit]);
		ListView_InsertItem(hwndList, &lvi);
	}

	TssListViewPtr qtlv;
	qtlv.Create();
	qtlv->SubclassListView(::GetDlgItem(m_hwnd, IDC_LVFW), wsUser);

	hwnd = qtlv->Hwnd();
	hwndList = GetDlgItem(m_hwnd, IDC_LVFW);
	Assert(hwnd == hwndList);
	FW_LVCOLUMN flvc;
	flvc.mask = LVCF_TEXT | LVCF_WIDTH;
	qtsf->MakeStringRgch(L"Fruit", 5, encEng, &flvc.qtss);
	flvc.cx = 60;
	Fw_ListView_InsertColumn(hwndList, 0, &flvc);
	qtsf->MakeStringRgch(L"Test", 4, encEng, &flvc.qtss);
	flvc.cx = 40;
	Fw_ListView_InsertColumn(hwndList, 1, &flvc);

	FW_LVITEM flvi(LVIF_TEXT);
	for (iFruit = 0; iFruit < kcFruit; iFruit++)
	{
		flvi.iItem = iFruit;
		StrUni stu = krgszFruit[iFruit];
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), encEng, &flvi.qtss);
		Fw_ListView_InsertItem(hwndList, &flvi);
	}

	return true;
}

bool TstWidgetsDlg::OnCommand(int cid, int nc, HWND hctl)
{
	switch (nc)
	{
	case BN_CLICKED:
		switch (cid)
		{
		case IDC_BTNCOMBOSTD:
			{
				HWND hwndCombo = GetDlgItem(m_hwnd, IDC_COMBOSTD);
				int cch = SendMessage(hwndCombo, WM_GETTEXTLENGTH, 0, 0) + 1;
				achar * prgch = NewObj achar[cch];
				SendMessage(hwndCombo, WM_GETTEXT, (WPARAM)cch, (LPARAM)prgch);
				MessageBox(m_hwnd, prgch, NULL, MB_OK);
				delete prgch;
				return true;
			}

		case IDC_BTNCOMBOFW:
			{
				HWND hwndCombo = GetDlgItem(m_hwnd, IDC_COMBOFW);
				const OLECHAR * prgwch;
				int cch;
				ITsStringPtr qtss;
				StrAppBufBig strbb; // Assume 1024 is adequate space.
				if (SendMessage(hwndCombo, FW_CB_GETTEXT, 0, (LPARAM)&qtss) == LB_ERR)
					return false;
				if (FAILED(qtss->LockText(&prgwch, &cch)))
					return false;
				strbb.Append(prgwch);
				qtss->UnlockText(prgwch);
				MessageBox(m_hwnd, strbb.Chars(), NULL, MB_OK);
				return true;
			}

		case IDC_BTNLISTSTD:
			{
				HWND hwndList = GetDlgItem(m_hwnd, IDC_LISTSTD);
				int citems = SendMessage(hwndList, LB_GETSELCOUNT, 0, 0);
				int * prgitems = new int[citems];
				if (prgitems)
				{
					SendMessage(hwndList, LB_GETSELITEMS, citems, (LPARAM)prgitems);
					achar szMessage[2000] = {0};
					achar szItem[100];
					_stprintf_s(szMessage, _T("%d item%s: "),
						citems, citems == 1 ? _T("") : _T("s"));
					for (int i = 0; i < citems; i++)
					{
						SendMessage(hwndList, LB_GETTEXT, prgitems[i], (LPARAM)szItem);
						_tcscat_s(szMessage, szItem);
						_tcscat_s(szMessage, _T(" "));
					}
					MessageBox(m_hwnd, szMessage, NULL, MB_OK);
				}
				return true;
			}

		case IDC_BTNLISTFW:
			{
				HWND hwndList = GetDlgItem(m_hwnd, IDC_LISTFW);
				int citems = SendMessage(hwndList, LB_GETSELCOUNT, 0, 0);
				int * prgitems = new int[citems];
				if (prgitems)
				{
					const OLECHAR * prgwch;
					int cch;
					ITsStringPtr qtss;
					StrAppBufBig strbb; // Assume 1024 is adequate space.
					SendMessage(hwndList, LB_GETSELITEMS, citems, (LPARAM)prgitems);
					strbb.Format(_T("%d item%s: "), citems, citems == 1 ? _T("") : _T("s"));
					for (int i = 0; i < citems; i++)
					{
						if (SendMessage(hwndList, FW_LB_GETTEXT, prgitems[i], (LPARAM)&qtss) == LB_ERR)
							return false;
						if (FAILED(qtss->LockText(&prgwch, &cch)))
							return false;
						strbb.Append(prgwch);
						strbb.Append(" ");
						qtss->UnlockText(prgwch);
					}
					MessageBox(m_hwnd, strbb.Chars(), NULL, MB_OK);
				}
				return true;
			}

		case IDC_BTNEDITSTD:
			{
				HWND hwndEdit = GetDlgItem(m_hwnd, IDC_EDITSTD);
				int iStart = 0;
				int iEnd = 0;
				int selrg = SendMessage(hwndEdit, EM_GETSEL, 0, iEnd);
				iStart = LOWORD(selrg);
				iEnd = HIWORD(selrg);
				int cch = SendMessage(hwndEdit, WM_GETTEXTLENGTH, 0, 0) + 1;
				achar * prgch = NewObj achar[cch];
				selrg = SendMessage(hwndEdit, WM_GETTEXT, (WPARAM)cch, (LPARAM)prgch);
				memmove(prgch, prgch + iStart, (iEnd - iStart) * sizeof(achar));
				prgch[iEnd - iStart] = 0;
				MessageBox(m_hwnd, prgch, NULL, MB_OK);
				delete prgch;
				return true;
			}

		case IDC_BTNEDITFW:
			{

/*
// TODO: fix this so it reads tss string
				HWND hwndEdit = GetDlgItem(m_hwnd, IDC_EDITFW);
				int iStart = 0;
				int iEnd = 0;
				int selrg = SendMessage(hwndEdit, EM_GETSEL, 0, iEnd);
				iStart = LOWORD(selrg);
				iEnd = HIWORD(selrg);

				const OLECHAR * prgch;
				int cch;
				ITsStringPtr qtss;
				StrAnsiBufBig stabb; // Assume 1024 is adequate space.
				if (SendMessage(hwndEdit, FW_WM_GETTEXT, 0, (LPARAM)&qtss) == LB_ERR)
					return false;
				if (FAILED(qtss->LockText(&prgch, &cch)))
					return false;
				stabb.Append(prgch);
				qtss->UnlockText(prgch);
				OLECHAR * prgwch;
				prgwch = prgch;
				memmove(prgch, prgch + iStart, iEnd - iStart);
				prgch[iEnd - iStart] = 0;
				MessageBox(m_hwnd, prgch, NULL, MB_OK);
				delete prgch;
				return true;
*/
				HWND hwndEdit = GetDlgItem(m_hwnd, IDC_EDITFW);
				int iStart = 0;
				int iEnd = 0;
				int selrg = SendMessage(hwndEdit, EM_GETSEL, 0, iEnd);
				iStart = LOWORD(selrg);
				iEnd = HIWORD(selrg);
				int cch = SendMessage(hwndEdit, WM_GETTEXTLENGTH, 0, 0) + 1;
				achar * prgch = NewObj achar[cch];
				selrg = SendMessage(hwndEdit, WM_GETTEXT, (WPARAM)cch, (LPARAM)prgch);
				memmove(prgch, prgch + iStart, (iEnd - iStart) * isizeof(achar));
				prgch[iEnd - iStart] = 0;
				MessageBox(m_hwnd, prgch, NULL, MB_OK);
				delete prgch;
				return true;
			}

		case IDC_BTNLVSTD:
			{
				HWND hwndList = GetDlgItem(m_hwnd, IDC_LVSTD);
				StrApp str;
				int citems = ListView_GetSelectedCount(hwndList);
				str.Format(_T("%d item%s: "), citems, citems == 1 ? _T("") : _T("s"));
				int iItem = ListView_GetNextItem(hwndList, -1, LVNI_SELECTED);
				achar rgchBuf[1024];
				while (iItem != -1)
				{
					ListView_GetItemText(hwndList, iItem, 0, rgchBuf, isizeof(rgchBuf));
					str.Append(rgchBuf);
					str.Append(_T(" "));
					iItem = ListView_GetNextItem(hwndList, iItem, LVNI_SELECTED);
				}
				MessageBox(m_hwnd, str.Chars(), NULL, MB_OK);
				return true;
			}

		case IDC_BTNLVFW:
			{
				HWND hwndList = GetDlgItem(m_hwnd, IDC_LVFW);
				StrApp str;
				int citems = ListView_GetSelectedCount(hwndList);
				str.Format(_T("%d item%s: "), citems, citems == 1 ? _T("") : _T("s"));
				int iItem = ListView_GetNextItem(hwndList, -1, LVNI_SELECTED);
				achar rgchBuf[1024];
				while (iItem != -1)
				{
					ListView_GetItemText(hwndList, iItem, 0, rgchBuf, isizeof(rgchBuf));
					str.Append(rgchBuf);
					str.Append(_T(" "));
					iItem = ListView_GetNextItem(hwndList, iItem, LVNI_SELECTED);
				}
				MessageBox(m_hwnd, str.Chars(), NULL, MB_OK);
			}

		case IDC_BTNTVSTD:
			{
				HWND hwndTV = GetDlgItem(m_hwnd, IDC_TVSTD);
				TVITEM tvi;
				tvi.hItem =(HTREEITEM) SendMessage(hwndTV, TVM_GETNEXTITEM, (WPARAM)TVGN_CARET, (LPARAM)NULL);
				tvi.mask = TVIF_TEXT | TVIF_IMAGE | TVIF_SELECTEDIMAGE | TVIF_PARAM;
				int cch = 1000;
				achar * prgch = NewObj achar[cch];
				tvi.pszText = prgch;
				tvi.cchTextMax = cch;
				int selitem;
				selitem = SendMessage(hwndTV, TVM_GETITEM, 0, (LPARAM)&tvi);
				MessageBox(m_hwnd, tvi.pszText, NULL, MB_OK);
				return true;
			}
		case IDC_BTNTVFW:
			{
				const OLECHAR * prgwch;
				int cch;
				StrAppBufBig strbb; // Assume 1024 is adequate space.
				ITsStringPtr qtss;
				HWND hwndTV = GetDlgItem(m_hwnd, IDC_TVFW);
				FW_TVITEMEX fwtvix;
				fwtvix.hItem =(HTREEITEM) SendMessage(hwndTV, TVM_GETNEXTITEM, (WPARAM)TVGN_CARET, (LPARAM)NULL);
				fwtvix.mask = TVIF_TEXT | TVIF_IMAGE | TVIF_SELECTEDIMAGE | TVIF_PARAM;
				int selitem;
				selitem = SendMessage(hwndTV, TVM_GETITEM, 0, (LPARAM)&fwtvix);
				qtss = fwtvix.qtss;
				if (FAILED(qtss->LockText(&prgwch, &cch)))
					return false;
				qtss->UnlockText(prgwch);
				strbb = prgwch;
				MessageBox(m_hwnd, strbb, NULL, MB_OK);
				return true;
			}
		}
		break;

	case LBN_SELCHANGE:
		return true;
	case LBN_SELCANCEL:
		return true;
	case LBN_DBLCLK:
		return true;
	default:
		break;
	}
	return AfDialog::OnCommand(cid, nc, hctl);
}

/*----------------------------------------------------------------------------------------------
	Our test application class.
----------------------------------------------------------------------------------------------*/
class TstWidgetsAf : public AfApp
{
public:
protected:
	virtual void Init(void)
	{
		CoInitialize(NULL);

		TstWidgetsDlgPtr qtwd;
		qtwd.Create();

		INITCOMMONCONTROLSEX icce = { isizeof(icce), ICC_USEREX_CLASSES };
		InitCommonControlsEx(&icce);

		int iselNormal = 1;
		int iselWidget = 4;

		qtwd->SetValues(iselNormal, iselWidget);
		qtwd->DoModal(GetDesktopWindow());
		qtwd->GetValues(iselNormal, iselWidget);

		CoUninitialize();
		Quit();
	}
};

static TstWidgetsAf g_app;
