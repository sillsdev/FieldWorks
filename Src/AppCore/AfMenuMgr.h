/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfMenuMgr.h
Responsibility: Shon Katzenberger
Last reviewed:

	Menu manager. Designed to be embedded in the frame window object.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfMenuMgr_H
#define AfMenuMgr_H 1


/*----------------------------------------------------------------------------------------------
	Utility classes used in implementing the menu manager.
----------------------------------------------------------------------------------------------*/
namespace AfMenuMgrUtils
{
	const int kcchMaxMenu = 256;

	/*------------------------------------------------------------------------------------------
		Friendly version of MENUITEMINFO.
		Hungarian: mii.
	------------------------------------------------------------------------------------------*/
	class MenuItemInfo : public MENUITEMINFO
	{
	public:
		achar m_sz[kcchMaxMenu + 1];

		MenuItemInfo(void)
		{
			::ClearItems(static_cast<MENUITEMINFO *>(this), 1);
			// For backward compatibility, we cannot use isizeof(MENUITEMINFO) here.
			// Windows NT 4 fails when calling GetMenuItemInfo when cbSize is 48,
			// which is the size of the MENUITEMINFO structure when WINVER >= 0x500.
			// To see the declaration of MENUITEMINFO, look in Winuser.h.
			cbSize = 44;//isizeof(MENUITEMINFO);
			dwTypeData = m_sz;
			cch = kcchMaxMenu;
		}
	};


	/*------------------------------------------------------------------------------------------
		Information for a command in an accelerator table. The string is for putting
		accelerator information on the menus. The strings are filled in the first time they
		are needed.
		Hungarian: aki.
	------------------------------------------------------------------------------------------*/
	class AccelKeyInfo : public ACCEL
	{
	public:
		// String name for the key combination. This is empty until it is needed.
		StrApp m_strKey;

		void GetName(StrApp & str);
	};


	/*------------------------------------------------------------------------------------------
		Accelerator table information.
		Hungarian: ati.
	------------------------------------------------------------------------------------------*/
	class AccelTableInfo
	{
	public:
		// Handle to the accelerator table.
		HACCEL m_hact;
		// Accelerator priority level. Accelerator tables are sorted by this.
		int m_apl;
		// Window to which the command should be directed, if found in this table.
		// If null, the accelerator table is disabled.
		HWND m_hwnd;
		// ID associated with the table by AddAccelTable.
		int m_atid;

		// Vector that maps command id to keyboard shortcut and string.
		// This is sorted by cid with at most one entry per cid.
		Vector<AccelKeyInfo> m_vaki;

		bool FFindAccelKeyInfo(int cid, int * piaki);
	};


	/*------------------------------------------------------------------------------------------
		Maps a command id to its index into the image list. Also stores the icon equivalent
		of the image in the image list. The icon is only extracted from the image list if it
		is needed. The extraction is done by GetIcon. The icon is only used to draw embossed
		buttons on disabled menu items.
	------------------------------------------------------------------------------------------*/
	class CidToImag
	{
	public:
		CidToImag(void)
		{
			m_hicon = NULL;
		}
		~CidToImag(void)
		{
			if (m_hicon)
			{
				::DestroyIcon(m_hicon);
				m_hicon = NULL;
			}
		}

		HICON GetIcon(HIMAGELIST himl);

		int m_cid;
		int m_imag;
		HICON m_hicon;
	};


	/*------------------------------------------------------------------------------------------
		One of these for each owner-draw menu item. These are stored in the dwItemData field
		of the MenuItemInfo.
		Hungarian: mid.
	------------------------------------------------------------------------------------------*/
	class MenuItemData
	{
	public:
		// Identifies owner-draw data as ours.
		enum { knMagicMenuItemData = 'FwMd' };

		int m_nMagic; // Magic number identifying this as a MenuItemData.
		StrApp m_strTxt; // Item text.
		StrApp m_strAccel; // Accelerator text.
		uint m_uType; // Original item type flags.
		HBITMAP m_hbmpChecked;
		HBITMAP m_hbmpUnchecked;
		bool m_fSubMenu;

		MenuItemData(void)
		{
			m_nMagic = knMagicMenuItemData;
			m_hbmpChecked = NULL;
			m_hbmpUnchecked = NULL;
		}

		static MenuItemData * GetData(ulong luData)
		{
			if (!luData)
				return NULL;

			try
			{
				MenuItemData * pmid = (MenuItemData *)luData;
				return pmid->m_nMagic == knMagicMenuItemData ? pmid : NULL;
			}
			catch (...)
			{
				return NULL;
			}
		}
	};
}


class AfMainWnd;

/*----------------------------------------------------------------------------------------------
	Menu manager. Handles icons and accelerator names.
	Hungarian: mum
----------------------------------------------------------------------------------------------*/
class AfMenuMgr
{
public:
	AfMenuMgr(AfMainWnd * pafw);
	~AfMenuMgr(void);

	void LoadToolBar(int rid);
	void LoadToolBars(const int * prgrid, int crid);

	void SetMenuStates(CmdHandler * pcmh, HMENU hmenu, int ihmenu, bool fSysMenu);

	/*------------------------------------------------------------------------------------------
		Accelerator table management.
	------------------------------------------------------------------------------------------*/
	int AddAccelTable(HACCEL hact, int apl, HWND hwnd);
	int LoadAccelTable(int ridAccel, int apl, HWND hwnd);
	void RemoveAccelTable (int atid);
	void SetAccelHwnd(int atid, HWND hwnd);
	bool FTransAccel(MSG * pmsg);

	/*------------------------------------------------------------------------------------------
		Call backs from WndProcs.
	------------------------------------------------------------------------------------------*/
	bool OnMeasureItem(MEASUREITEMSTRUCT * pmis);
	bool OnDrawItem(DRAWITEMSTRUCT * pdis);
	void OnInitMenuPopup(HMENU hmenu, int imnu);
	void OnMenuClose(void);
	long OnMenuChar(achar ch, HMENU hmenu);

	void Refresh(void);
	void ResumeRefresh(void)
	{
		m_fIgnoreRefresh = false;
	}

	void GetLastExpMenuInfo(HMENU * phmenu, int * pidDummy);
	void SaveActiveMenu(HMENU hmenu);

	bool UseIdenticalIcon(int cidOriginal, int cidNew);

	HFONT GetMenuFont(void);

#ifdef DEBUG
	bool AssertValid(void)
	{
		AssertPtr(this);
		return true;
	}
#endif // DEBUG

	void ExpandMenuItems(HMENU hmenu, int imnu);

	typedef enum
	{
		kmaExpandItem,
		kmaGetStatusText,
		kmaDoCommand,
	} MenuAction;

	void SetMenuHandler(int cid);

	bool FFindAccelKeyName(int cid, StrApp & str);
	int GetImagFromCid(int cid);
	HIMAGELIST GetImageList()
	{
		return m_himl;
	}

protected:
	bool m_fIgnoreRefresh;

	// Dimensions of the bitmaps (must all be the same).
	int m_dxsBmp;
	int m_dysBmp;
	int m_dxsBtn;
	int m_dysBtn;

	// Menu font. Used by OnMeasureItem. Created by GetMenuFont.
	HFONT m_hfontMenu;

	// Images for the buttons.
	HIMAGELIST m_himl;

	// System check mark bitmap.
	HBITMAP m_hbmpCheck;

	// ToolBar IDs loaded.
	Vector<int> m_vridTlbr;

	// Next id for accel table.
	int m_atidNext;

	// Accelerator table information. This is sorted by atid.
	Vector<AfMenuMgrUtils::AccelTableInfo> m_vati;

	// Maps from command id to image index and icon.
	// This is sorted by command id.
	Vector<AfMenuMgrUtils::CidToImag> m_vcti;

	bool FFindCidToImag(int cid, int * picti);

	// These menus have been converted to owner draw.
	// Sorted by HMENU.
	Vector<HMENU> m_vhmenu;

	AfMainWnd * m_pafwFrame;

	typedef struct
	{
		// Note: only one expanded section is allowed per menu.
		HMENU m_hmenu; // Handle to the menu containing the expanded items.
		uint m_idDummy; // ID of the original dummy item that gets expanded.
		int m_imni; // Index of the first expanded item. (Also the index of the dummy item.)
		int m_cmniAdded; // Number of expanded items that replaced the dummy item.
	} ExpandedMenu;
	// These menus have been expanded.
	Vector<ExpandedMenu> m_vemMenus;
	// This is updated whenever a new menu item is activated, thus it always has information
	// on the last active menu.
	ExpandedMenu m_emLastExp;

	bool FFindHmenu(HMENU hmenu, int * pihmenu);

	void LoadImages(int rid);
	void ConvertMenu(HMENU hmenu, int imnu, bool fShowBtns);
	void DrawMenuText(HDC hdc, Rect & rc, const AfMenuMgrUtils::MenuItemData * pmid,
		COLORREF clr);
	void DrawCheckmark(HDC hdc, const Rect & rc, COLORREF clr, HBITMAP hbmp);
};

#endif // !AfMenuMgr_H