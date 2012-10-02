// AccessibleObjectFromPoint.cpp : Defines the entry point for the application.
//

#include "stdafx.h"
#include "AccessibleObjectFromPoint.h"
#include <gdiplus.h>

#include <stdio.h>
#include <vector>

void GetStringForRoleID(long role, char* roleName);
bool SetRegistryTestingForFLEX(bool testmode);
BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam);
void UpdateTimes();

struct PathInfo;

#define MAX_LOADSTRING 100

// Global Variables:
bool verbosePath = false;						// false: remove desktop and 'the window and dup window/client pairs
												// true: return the full path
												// Verbose is turned on by holding the shift key down when releasing the button
HINSTANCE hInst;								// current instance
TCHAR szTitle[MAX_LOADSTRING];					// The title bar text
TCHAR szWindowClass[MAX_LOADSTRING];			// the main window class name

// Forward declarations of functions included in this code module:
ATOM				MyRegisterClass(HINSTANCE hInstance);
BOOL				InitInstance(HINSTANCE, int);
LRESULT CALLBACK	WndProc(HWND, UINT, WPARAM, LPARAM);
LRESULT CALLBACK	About(HWND, UINT, WPARAM, LPARAM);
void				CopyDataToClipboard( TCHAR *data);
void				DrawScreenRectangle(RECT screenPos);
int					GetChildNumberWithNameAtPosition(IAccessible* parent, BSTR childName, RECT childPosition, HWND childHwnd);
bool				FindChildAccAtPosition(IAccessible* parent, RECT childPosition ,std::vector<PathInfo*> *objStack);
void BuildAccessibilityPath(std::vector<PathInfo*> *vecPathInfo);
IAccessible *GetIAccessibleFromIDispatch(IDispatch* pdisp, IAccessible** IAccOut);


int APIENTRY _tWinMain(HINSTANCE hInstance,
					 HINSTANCE hPrevInstance,
					 LPTSTR    lpCmdLine,
					 int       nCmdShow)
{
	// TODO: Place code here.
	MSG msg;
	HACCEL hAccelTable;

	::CoInitialize(NULL);

	// Initialize GDI+.
	Gdiplus::GdiplusStartupInput gdiplusStartupInput;
	ULONG_PTR gdiplusToken;
	Gdiplus::GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);

	// Initialize global strings
	LoadString(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
	LoadString(hInstance, IDC_ACCESSIBLEOBJECTFROMPOINT, szWindowClass, MAX_LOADSTRING);
	MyRegisterClass(hInstance);

	// Perform application initialization:
	if (!InitInstance (hInstance, nCmdShow))
	{
		return FALSE;
	}

	hAccelTable = LoadAccelerators(hInstance, (LPCTSTR)IDC_ACCESSIBLEOBJECTFROMPOINT);

	SetRegistryTestingForFLEX(true);

	// Main message loop:
	while (GetMessage(&msg, NULL, 0, 0))
	{
		if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg))
		{
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}
	}
	Gdiplus::GdiplusShutdown(gdiplusToken);
	SetRegistryTestingForFLEX(false);

	return (int) msg.wParam;
}


//
//  FUNCTION: MyRegisterClass()
//
//  PURPOSE: Registers the window class.
//
//  COMMENTS:
//
//    This function and its usage are only necessary if you want this code
//    to be compatible with Win32 systems prior to the 'RegisterClassEx'
//    function that was added to Windows 95. It is important to call this function
//    so that the application will get 'well formed' small icons associated
//    with it.
//
ATOM MyRegisterClass(HINSTANCE hInstance)
{
	WNDCLASSEX wcex;
	wcex.cbSize = sizeof(WNDCLASSEX);
	wcex.style			= CS_HREDRAW | CS_VREDRAW;
	wcex.lpfnWndProc	= (WNDPROC)WndProc;
	wcex.cbClsExtra		= 0;
	wcex.cbWndExtra		= 0;
	wcex.hInstance		= hInstance;
	wcex.hIcon			= LoadIcon(hInstance, (LPCTSTR)IDI_ACCESSIBLEOBJECTFROMPOINT);
	wcex.hCursor		= LoadCursor(NULL, IDC_ARROW);
	wcex.hbrBackground	= (HBRUSH)(COLOR_WINDOW+1);
	wcex.lpszMenuName	= (LPCTSTR)IDC_ACCESSIBLEOBJECTFROMPOINT;
	wcex.lpszClassName	= szWindowClass;
	wcex.hIconSm		= LoadIcon(wcex.hInstance, (LPCTSTR)IDI_SMALL);
	return RegisterClassEx(&wcex);
}

//
//   FUNCTION: InitInstance(HANDLE, int)
//
//   PURPOSE: Saves instance handle and creates main window
//
//   COMMENTS:
//
//        In this function, we save the instance handle in a global variable and
//        create and display the main program window.
//
BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
   HWND hWnd;

   hInst = hInstance; // Store instance handle in our global variable

   hWnd = CreateWindow(szWindowClass, szTitle, WS_BORDER | WS_CAPTION | WS_OVERLAPPED | WS_SYSMENU ,//WS_OVERLAPPEDWINDOW,
	  CW_USEDEFAULT, 0, 60, 90, NULL, NULL, hInstance, NULL);

   if (!hWnd)
   {
	  return FALSE;
   }

   ShowWindow(hWnd, nCmdShow);
   UpdateWindow(hWnd);

   return TRUE;
}

struct PathInfo
{
	static const int MaxValueSize = 25;		// static const for limiting the size of the value data

	TCHAR *name;			// the actual name of this object/control
	TCHAR *value;			// the value (if one exists)
	TCHAR *keyboard;		// the keyboard shortcut for this object
	int role;				// role
	int breadth;			// internal -
	int depth;				// internal -
	RECT location;			// location of the control
	int valueMaxSize;		// actual size of value - can be larger than MaxValueSize limit
	int valueLength;		// size of the value string
	int state;				// bit flags for the state information
	HWND hwnd;				// hwnd of this object

	PathInfo::PathInfo(TCHAR* inName, int inRole)
	{
		size_t size = _tcslen(inName)+1;
		name = new TCHAR[size];
		_tcscpy_s(name, size, inName); // MDL
		role = inRole;
		breadth = 0;
		depth = 0;
		value = 0;
		valueMaxSize = 0;
		valueLength = 0;
		state = 0;
		keyboard = 0;
		hwnd = 0;
	}
	PathInfo::PathInfo(IAccessible *pAcc, VARIANT const *varChild)
	{
		BSTR bstrName = NULL;
		HRESULT hr = pAcc->get_accName(*varChild, &bstrName);

		// If a name was returned without error,
		// convert it from wide Unicode to multibyte Unicode (UTF8).
		if (bstrName && hr == S_OK)
		{
			int len = SysStringLen(bstrName);
			int buffSize = WideCharToMultiByte(CP_UTF8, 0, bstrName, len, NULL, 0, NULL, NULL);
			name = new TCHAR[buffSize+1];
			WideCharToMultiByte(CP_UTF8, 0, bstrName, len, name, buffSize+1, NULL, NULL);
			name[buffSize] = 0;	// make sure a zero byte is in the data stream
			SysFreeString(bstrName);

			////name = new TCHAR[SysStringLen(bstrName)+1];
			////WideCharToMultiByte(CP_UTF8/*CP_ACP*/, 0, bstrName,-1, name, 1024, NULL, NULL);
			////SysFreeString(bstrName);
		}
		else
		{
			TCHAR defName[] = "NAMELESS";
			size_t size = _tcslen(defName)+1;
			name = new TCHAR[size];
			_tcscpy_s(name, size, defName);
		}

		// set the role
		VARIANT varRetVal;
		VariantInit(&varRetVal);
		hr = pAcc->get_accRole(*varChild, &varRetVal);
		if (varRetVal.vt == VT_I4)
		{
			role = (int)varRetVal.lVal;
		}

		// set the location
		long xLeft, yTop, dxWidth, dyHeight;
		pAcc->accLocation(&xLeft, &yTop, &dxWidth, &dyHeight, *varChild);
		SetRect(&location, xLeft, yTop, xLeft+dxWidth, yTop+dyHeight);

		// see if it has a value
		BSTR bstrValue;
		hr = pAcc->get_accValue(*varChild, &bstrValue);
		if (hr == S_OK && bstrValue)
		{
			valueMaxSize = SysStringLen(bstrValue);
			if (valueMaxSize > MaxValueSize)
				valueLength = MaxValueSize;
			else
				valueLength = valueMaxSize;

			int buffSize = WideCharToMultiByte(CP_UTF8, 0, bstrValue, valueLength, value, 0, NULL, NULL);
			value = new TCHAR[buffSize+1];
			WideCharToMultiByte(CP_UTF8, 0, bstrValue, valueLength, value, buffSize+1, NULL, NULL);
			value[buffSize] = 0;	// make sure a zero byte is in the data stream
			SysFreeString(bstrValue);
		}

		// get the state value
		VariantInit(&varRetVal);
		hr = pAcc->get_accState(*varChild, &varRetVal);
		if (hr == S_OK)
			state = varRetVal.lVal;

		// see if it has a keyboard shortcut
		hr = pAcc->get_accKeyboardShortcut(*varChild, &bstrValue);
		if (hr == S_OK && bstrValue)
		{
			int buffSize = WideCharToMultiByte(CP_UTF8, 0, bstrValue, -1, keyboard, 0, NULL, NULL);
			keyboard = new TCHAR[buffSize+1];
			WideCharToMultiByte(CP_UTF8, 0, bstrValue, buffSize, keyboard, buffSize+1, NULL, NULL);
			keyboard[buffSize] = 0;	// make sure a zero byte is in the data stream
			SysFreeString(bstrValue);
		}
		else
			keyboard = 0;

		// get the hwnd
		hr = ::WindowFromAccessibleObject(pAcc, &hwnd);
		if (hr != S_OK)
			hwnd = 0;

	}

	PathInfo::~PathInfo()
	{
		if (name)
			delete [] name;
	}
};

// Find the main appliation HWND and the Accessible object for it, and then
// navigate down to the Accessible object that has the same location as the
// PathInfo parameter.
void FindPathInfoTopDown(PathInfo const *pi, POINT pt, std::vector<PathInfo*> *objStack)
{
	HWND hwndLast;	// last valid parent window
	HWND hwndStart = WindowFromPoint(pt);
	while (hwndStart != NULL)
	{
		hwndLast = hwndStart;
		hwndStart = ::GetParent(hwndLast);
	}

	IDispatch * pDisp;
	HRESULT hr = AccessibleObjectFromWindow(hwndLast, OBJID_WINDOW, IID_IDispatch, (void**) &pDisp);
	if (pDisp)
	{
		IAccessible* pAcc = NULL;
		GetIAccessibleFromIDispatch(pDisp, &pAcc);
		FindChildAccAtPosition(pAcc, pi->location, objStack);
	}
}

IAccessible *GetIAccessibleFromIDispatch(IDispatch* pdisp, IAccessible** IAccOut)
{
	*IAccOut = NULL;
	try
	{
		pdisp->QueryInterface(IID_IAccessible, (void**)IAccOut);
		pdisp->Release();
	}
	catch(...)
	{;	// some accessible objects will fail and even throw, so ignore those
	}
	return *IAccOut;
}

void IntersectRectX(RECT &intersection, RECT A, RECT B)
{
	int top, left, bottom, right;
	if (A.top < B.top)
		top = A.top;
	else
		top = B.top;

	if (A.left < B.left)
		left = B.left;
	else
		left = A.left;

	if (A.right < B.right)
		right = A.right;
	else
		right = B.right;

	if (A.bottom < B.bottom)
		bottom = A.bottom;
	else
		bottom = B.bottom;
	SetRect(&intersection, left, top, right, bottom);
}


// Recursive method to walk the Accessible object children looking for the one that is in
// the location of the parameter 'childPosition'.  Once found the method returns true and
// the unwinding of the stack populates the objStack parameter with the resulting Accessible
// objects PathInfo data.
bool FindChildAccAtPosition(IAccessible* parent, RECT childPosition, std::vector<PathInfo*> *objStack)
{
//	char buff[512];
	bool rval = false;
	HRESULT hr;
	long numChildren;
	int curMatchCount = 0;

	hr = parent->get_accChildCount(&numChildren);
	if (hr != S_OK || numChildren < 1)
		return false;

	// allocate storage for the children variants to be walked through
	VARIANT *pvarChildren = new VARIANT[numChildren];
	long cObtained;
	hr = AccessibleChildren(parent, 0L, numChildren, pvarChildren, &cObtained);
	if (hr != S_OK && hr != S_FALSE)
	{
		delete[] pvarChildren;
		return 0;	// couldn't get any children
	}

	VARIANT varChild;
	IAccessible *pAccChild;

	for (int childNum = 0; childNum < cObtained; childNum++)
	{
		VARIANT vChildren = pvarChildren[childNum];	// local variant for current child
		IDispatch * pdisp = NULL;
		if (vChildren.vt == VT_DISPATCH)	// if it's a dispatch type
		{
			if (GetIAccessibleFromIDispatch(vChildren.pdispVal, &pAccChild) == NULL)
				continue;
		}
		else if (vChildren.vt == VT_I4)
		{
			HRESULT hr = parent->get_accChild( vChildren, &pdisp );
			if(hr != S_OK || pdisp == NULL)
				continue;
			if (GetIAccessibleFromIDispatch(vChildren.pdispVal, &pAccChild) == NULL)
				continue;
		}
		else
		{
			// need to handle the VT_I4 case
			throw;
		}

		VariantClear(&varChild);
		varChild.vt = VT_I4;
		varChild.lVal = CHILDID_SELF;

		BSTR bstrName = NULL;
		hr = pAccChild->get_accName(varChild, &bstrName);
		if (hr != S_OK && hr != S_FALSE)
			continue;

		if (bstrName == NULL)	// replace with "NAMELESS"
			bstrName = SysAllocString(L"NAMELESS");

		::OutputDebugStringW(bstrName);
		OutputDebugString("\n");

		long xLeft, yTop, dxWidth, dyHeight;
		hr = pAccChild->accLocation(&xLeft, &yTop, &dxWidth, &dyHeight, varChild);
		if (hr != S_OK)
		{
			::SysFreeString(bstrName);
			continue;	// couldn't get location
		}

		RECT rectPoint;
		SetRect(&rectPoint, xLeft, yTop, xLeft+dxWidth, yTop+dyHeight);

		RECT intersection;
		IntersectRect(&intersection, &childPosition, &rectPoint);
		IntersectRectX(intersection, childPosition, rectPoint);

		if (intersection.top == intersection.bottom && intersection.bottom == 0 &&
			intersection.left == intersection.right && intersection.right == 0)	//IsRectEmpty(&intersection))
		{
			::SysFreeString(bstrName);
			continue; // this child doesn't contain the target location rectangle
		}

		if (EqualRect(&rectPoint, &childPosition))
		{
			OutputDebugString("Found accObject with same location: ");
			OutputDebugStringW(bstrName);
			::SysFreeString(bstrName);
			OutputDebugString(".  Now popping back up the stack...\n");
			return true;	// found it
		}
		else
		{
			if (FindChildAccAtPosition(pAccChild, childPosition, objStack))
			{
				OutputDebugStringW(bstrName);
				OutputDebugString("\n");
				PathInfo *pi = new PathInfo(pAccChild, &varChild);
				objStack->push_back(pi);
				int lastChildNum = GetChildNumberWithNameAtPosition(parent, bstrName, pi->location, pi->hwnd);
				pi->depth = lastChildNum;
//				CopyRect(&(pi->location), &rectObj);
				if (bstrName)
					SysFreeString(bstrName);
				return true;
			// otherwise keep looking through other children that can also contain this child location
			}
		}

		::SysFreeString(bstrName);
	}
	// Didn't find a match, just return false.  This is a case where we searched a parent with
	// overlap but no children had overlap.  Go to the next sibling of the parent, or next ancestor.
	return false;
}

void DoTheWork(POINT pt)
{
	// Setup variables for interface pointers and return value.
	IAccessible* pAcc = NULL;
	HRESULT      hr;
	VARIANT      varChild;

	std::vector<PathInfo*> vecPathInfo;

	// See if there is an accessible object under the cursor.
	VariantClear(&varChild);
	hr = AccessibleObjectFromPoint(pt, &pAcc, &varChild);
	if (hr == S_OK)
	{
		TCHAR num[64];
		//TCHAR childName[512];
		int count = 1;
		long xLeft, yTop, dxWidth, dyHeight;
		RECT rectChild, rectObj;
		pAcc->accLocation(&xLeft, &yTop, &dxWidth, &dyHeight, varChild);
		SetRect(&rectChild, xLeft, yTop, xLeft+dxWidth, yTop+dyHeight);
//		VARIANT vc;
//		VariantInit(&vc);

		while (true)
		{
			_itot_s(count, num, 64, 10); // MDL
			OutputDebugString(num);

			hr = pAcc->accLocation(&xLeft, &yTop, &dxWidth, &dyHeight, varChild);
			if (hr != S_OK)
			{
				int asdf = 234;
				asdf++;
			}
			SetRect(&rectObj, xLeft, yTop, xLeft+dxWidth, yTop+dyHeight);

			// Query on the object's name.
			BSTR bstrName = NULL;
			hr = pAcc->get_accName(varChild, &bstrName);
			if (bstrName == NULL)
				bstrName = SysAllocString(L"NAMELESS");

			TCHAR accName[1024];
			TCHAR defName[] = "NAMELESS";

			// If a name was returned without error,
			// convert it from Unicode to an ANSI/multibyte string.
			if (bstrName && hr == S_OK)
			{
//				LPTSTR g_szName;
//				char buff[32];
				WideCharToMultiByte(CP_UTF8, 0, bstrName,-1, accName, 1024, NULL, NULL);
//				SysFreeString(bstrName);
				OutputDebugString(" Name=<");
				OutputDebugString(accName);
				OutputDebugString(">");
			}
			else
			{
				OutputDebugString(" Name=<NAMELESS>");
				_tcscpy_s(accName, 1024, defName); // MDL
			}
			BSTR bstrRole = NULL;
			VARIANT varRetVal;
			PathInfo *pi;
			VariantInit(&varRetVal);
			hr = pAcc->get_accRole(varChild, &varRetVal);
			if (varRetVal.vt == VT_I4)
			{
				long role = (long)varRetVal.lVal;
				VariantClear(&varRetVal);

				_ltot_s(role, num, 64, 10); // MDL
				OutputDebugString(" role=");
				OutputDebugString(num);
				char buff[1024];
				GetStringForRoleID(role, buff);
				OutputDebugString(" [");
				OutputDebugString(buff);
				OutputDebugString("]");


				// save the info into the PathInfo object
				//////////////pi = new PathInfo(accName, role);
				pi = new PathInfo(pAcc, &varChild);
				vecPathInfo.push_back(pi);
				// check for depth and breadth values

			}
			else
				pi = NULL;

			// get the hwnd of that accessible object
			HWND curWindow;
			hr = ::WindowFromAccessibleObject(pAcc, &curWindow);
			if (hr != S_OK)
			{
				OutputDebugString("**** COULD NOT GET ACCESSIBLE HWND *****\n");
			}
			else
			{
				_ltot_s((long)curWindow, num, 64, 10);
				OutputDebugString(" hwnd=");
				OutputDebugString(num);
			}

			OutputDebugString("\n");

			IDispatch * pDisp;
			hr = pAcc->get_accParent(&pDisp);
			if (pDisp == NULL)
			{
				CopyRect(&(pi->location), &rectObj);
				if (_tcscmp(pi->name, "Desktop") != 0)	// didn't find path all the way out, try going top down
				{
					FindPathInfoTopDown(pi, pt, &vecPathInfo);
				}
				break;
			}

			if (pDisp)
			{
				GetIAccessibleFromIDispatch(pDisp, &pAcc);
			}
			varChild.vt = VT_I4;
			varChild.lVal = CHILDID_SELF;

			if (pi)
			{
				int lastChildNum = GetChildNumberWithNameAtPosition(pAcc, bstrName, rectObj, curWindow);
				pi->depth = lastChildNum;
				CopyRect(&(pi->location), &rectObj);
			}

			if (bstrName)
				SysFreeString(bstrName);

			count++;
		}

		BuildAccessibilityPath(&vecPathInfo);
	}
}

void BuildAccessibilityPath(std::vector<PathInfo*> *vecPathInfo)
{
		// now walk the vector of path info objects and build a path string for the test tool
		// the format is role:name/role:name..
		TCHAR objPath[4096] = "<click path=\"";
		bool justStarting = true;		// skip the first one "Desktop" window and client
		TCHAR lastWindow[1024] = {0};
		TCHAR num[64];

		while (vecPathInfo->size() > 0)
		{
			PathInfo *pi = vecPathInfo->back();
			vecPathInfo->pop_back();

			bool addToPath = true;

			// if the role is that of a window, save the name in "lastWindow"
			// if the role is that of client, see if the name is the same as the last window and skip if it is
			if (pi->role == ROLE_SYSTEM_WINDOW)
			{
				int len = 1024;
				if (_tcslen(pi->name) < 1024)
					len = (int)(_tcslen(pi->name)+1); //MDL

				_tcsncpy_s(lastWindow, 1024, pi->name, len); //MDL
				if (justStarting)
				{
					if (_tcscmp(lastWindow, "Desktop") == 0)
						addToPath = false;
					else if (_tcscmp(lastWindow, "The Window") == 0)
						addToPath = false;
				}
				else
					justStarting = false;
			}
			else if (pi->role == ROLE_SYSTEM_CLIENT)
			{
				if (_tcscmp(lastWindow, pi->name) == 0)
					addToPath = false;
			}

			if (verbosePath || addToPath)
			{
				_ltot_s(pi->role, num, 64, 10); //MDL
				_tcscat_s(objPath, 4096, num); //MDL
				_tcscat_s(objPath, 4096, ":"); //MDL
				// make sure the name isn't to big, if so truncate it
				int len = 128;
				if (_tcslen(pi->name) < 128)
					len = (int)(_tcslen(pi->name)+1); //MDL

				_tcsncat_s(objPath, 4096, pi->name, len); //MDL

				// If the depth is > 1, then it's needed for the script processing so put it out.
				// The number is the count of the like named child items,
				//   NOT a child ID and
				//   NOT a child index.
				// Again, it's the count of child items that have the same name.
				//
				if (pi->depth > 1)	// append the count of the like named children for this one
				{
					_itot_s(pi->depth, num, 64, 10); //MDL
					_tcscat_s(objPath, 4096, "["); //MDL
					_tcscat_s(objPath, 4096, num); //MDL
					_tcscat_s(objPath, 4096, "]"); //MDL
				}
			}

			if (vecPathInfo->size() > 0)
			{
				if (addToPath)					// dont add another one if we haven't put out any new data
					_tcscat_s(objPath, 4096, "/"); //MDL
			}
			else
			{
				_tcscat_s(objPath, 4096, "\"/>\n"); //MDL

				// put out the location for the last control
				_tcscat_s(objPath, 4096, "<!-- <location left=\""); //MDL
				_ltot_s(pi->location.left, num, 64, 10); //MDL
				_tcscat_s(objPath, 4096, num); //MDL
				_tcscat_s(objPath, 4096, "\" top=\""); //MDL
				_ltot_s(pi->location.top, num, 64, 10); //MDL
				_tcscat_s(objPath, 4096, num); //MDL
				_tcscat_s(objPath, 4096, "\" dx=\""); //MDL
				_ltot_s(pi->location.right-pi->location.left, num, 64, 10); //MDL
				_tcscat_s(objPath, 4096, num); //MDL
				_tcscat_s(objPath, 4096, "\" dy=\""); //MDL
				_ltot_s(pi->location.bottom-pi->location.top, num, 64, 10); //MDL
				_tcscat_s(objPath, 4096, num); //MDL
				_tcscat_s(objPath, 4096, "\"/> -->\n"); //MDL

				// put out the value for the last control (if it exists)
				if (pi->valueLength > 0)
				{
					_tcscat_s(objPath, 4096, "<!-- <value length=\""); //MDL
					_ltot_s(pi->valueLength, num, 64, 10); //MDL
					_tcscat_s(objPath, 4096, num); //MDL
					_tcscat_s(objPath, 4096, "\""); //MDL
					if (pi->valueMaxSize > pi->valueLength)
					{
						_tcscat_s(objPath, 4096, " origLength=\""); //MDL
						_ltot_s(pi->valueMaxSize, num, 64, 10); //MDL
						_tcscat_s(objPath, 4096, num); //MDL
						_tcscat_s(objPath, 4096, "\""); //MDL
					}
					_tcscat_s(objPath, 4096, ">"); //MDL
					_tcscat_s(objPath, 4096, pi->value); //MDL
					_tcscat_s(objPath, 4096, "</value> -->\n"); //MDL
				}
				//					else
				//						_tcscat_s(objPath, "<!-- <value length=\"0\"/> -->\n");


				// put out the just name element
				_tcscat_s(objPath, 4096, "<!-- <name>"); //MDL
				_tcscat_s(objPath, 4096, pi->name); //MDL
				_tcscat_s(objPath, 4096, "</name> -->\n"); //MDL

				// put out role information
				_tcscat_s(objPath, 4096, "<!-- <role id=\""); //MDL
				_ltot_s(pi->role, num, 64, 10); //MDL
				_tcscat_s(objPath, 4096, num); //MDL
				_tcscat_s(objPath, 4096, "\" text=\""); //MDL
				char buff[1024];
				GetStringForRoleID(pi->role, buff);
				_tcscat_s(objPath, 4096, buff); //MDL
				_tcscat_s(objPath, 4096, "\"/> -->\n"); //MDL

				// if there's a keyboard shortcut key show it
				if (pi->keyboard)
				{
					_tcscat_s(objPath, 4096, "<!-- <keyboard ShortcutKey=\""); //MDL
					_tcscat_s(objPath, 4096, pi->keyboard); //MDL
					_tcscat_s(objPath, 4096, "\"/> -->\n"); //MDL
				}

				// put out the state information for the accessible object
				if (pi->state != 0)
				{
					_tcscat_s(objPath, 4096, "<!-- <state>"); //MDL
					// Convert state flags to comma separated list.
					TCHAR stateString[512];
					LPTSTR lpszState = stateString;
					UINT cchState = 512;
					DWORD dwStateBit;
					UINT cCount= 32;
					UINT cChars = 0;
					for(dwStateBit = 1; cCount; cCount--, dwStateBit <<= 1)
					{
						if (pi->state & dwStateBit)
						{
							cChars += GetStateText(dwStateBit, lpszState +
								cChars, cchState - cChars);
							if(cchState > cChars)
								*(lpszState + cChars++) = ',';
							else
								break;
						}

					}
					if(cChars > 1)
						*(lpszState + cChars - 1) = '\0';

					_tcscat_s(objPath, 4096, stateString); //MDL
					_tcscat_s(objPath, 4096, "</state> -->"); //MDL

				}
			}
		}

		OutputDebugString(objPath);
		OutputDebugString("\n");

		CopyDataToClipboard(objPath);
///		hr = pAcc->get_accRole(varChild, &bstrRole);

}


void CopyDataToClipboard( TCHAR *data)
{
	// Open the clipboard, and empty it.
	if (!OpenClipboard(NULL))
		return;
	EmptyClipboard();
	int len = (int)_tcsclen(/*(const TCHAR*)*/data);

	// If text is selected, copy it using the CF_TEXT format.
	// Allocate a global memory object for the text.

	HGLOBAL hglbCopy = GlobalAlloc(GMEM_MOVEABLE, (len+ 1) * sizeof(TCHAR));
	if (hglbCopy == NULL)
	{
		CloseClipboard();
		return;
	}

	// Lock the handle and copy the text to the buffer.
	LPTSTR lptstrCopy = (LPTSTR)GlobalLock(hglbCopy);
	memcpy(lptstrCopy, data, len * sizeof(TCHAR));
	lptstrCopy[len] = (TCHAR) 0;    // null character
	GlobalUnlock(hglbCopy);

	// Place the handle on the clipboard.

	SetClipboardData(CF_TEXT, hglbCopy);
	// Close the clipboard.

	CloseClipboard();

	::MessageBox(NULL,data,"Path Copied to clipboard", MB_OK);
}


void DrawScreenRectangle(RECT screenPos)
{
	HDC hdc = ::GetWindowDC(NULL);

	// Create a red brush.
	HBRUSH hbrush;//, hbrushOld;
	hbrush = CreateSolidBrush(RGB(255, 0, 0));

//	InvertRect(hdc, &screenPos);
	FrameRect(hdc, &screenPos, hbrush);
	DeleteObject(hbrush);
	ReleaseDC(NULL, hdc);
}


int GetChildNumberWithNameAtPosition(IAccessible* parent, BSTR childName, RECT childPosition, HWND childHwnd)
{
	// make sure there are more than 1 children
	long numChildren;
	HRESULT hr = parent->get_accChildCount(&numChildren);
	if (hr != S_OK || numChildren <= 1)
		return 0;

	// allocate storage for the children variants to be walked through
	VARIANT *pvarChildren = new VARIANT[numChildren];
	long cObtained;
	hr = AccessibleChildren(parent, 0L, numChildren, pvarChildren, &cObtained);
	if (hr != S_OK && hr != S_FALSE)
	{
		delete[] pvarChildren;
		return 0;	// couldn't get any children
	}

	int curMatchCount = 0;	// number with matching names
	int rVal = 0;			// default return value

	VARIANT varChild;
	IAccessible *pAccChild;

	for (int childNum = 0; childNum < cObtained; childNum++)
	{
		VARIANT vChildren = pvarChildren[childNum];	// local variant for current child
		IDispatch * pdisp = NULL;
		if (vChildren.vt == VT_DISPATCH)	// if it's a dispatch type
		{
			if (GetIAccessibleFromIDispatch(vChildren.pdispVal, &pAccChild) == NULL)
				continue;
		}
		else if (vChildren.vt == VT_I4)
		{
			HRESULT hr = parent->get_accChild( vChildren, &pdisp );
			if(hr != S_OK || pdisp == NULL)
				continue;
			if (GetIAccessibleFromIDispatch(vChildren.pdispVal, &pAccChild) == NULL)
				continue;
		}
		else
		{
			// need to handle the VT_I4 case
			throw;
		}

		VariantClear(&varChild);
		varChild.vt = VT_I4;
		varChild.lVal = CHILDID_SELF;

		BSTR bstrName = NULL;
		hr = pAccChild->get_accName(varChild, &bstrName);
		if (hr != S_OK && hr != S_FALSE)
			continue;
		if (bstrName == NULL)
			bstrName = SysAllocString(L"NAMELESS");
		if (wcscmp(childName, bstrName) != 0)
		{
			::SysFreeString(bstrName);
			continue;		// names are different
		}
		::SysFreeString(bstrName);
		curMatchCount++;	// bump the count of matches on name

		// get the hwnd
		HWND curWindow;
		hr = ::WindowFromAccessibleObject(pAccChild, &curWindow);
		if (hr != S_OK)
			curWindow = 0;

		long xLeft, yTop, dxWidth, dyHeight;
		RECT rectPoint;
		hr = pAccChild->accLocation(&xLeft, &yTop, &dxWidth, &dyHeight, varChild);
		if (hr != S_OK)
		{
			continue;	// couldn't get location
		}
		else
		{
			SetRect(&rectPoint, xLeft, yTop, xLeft+dxWidth, yTop+dyHeight);
			if (EqualRect(&rectPoint, &childPosition) && curWindow == childHwnd)
			{
				rVal = curMatchCount;
				break;
			}
		}
	}
	delete[] pvarChildren;
	return rVal;
}


//
// get the accessible object under the point
// get the rect of that accessible object
// draw a rectangle around that accessible object
//
void GetAccPointForCursorPosition(POINT &pt, HWND &hwndLastPoint, RECT &rectLastPoint)
{
	bool newRect = false;
	IAccessible* pAcc = NULL;
	HRESULT      hr;
	VARIANT      varChild;

	// See if there is an accessible object under the cursor.
	VariantClear(&varChild);
	hr = AccessibleObjectFromPoint(pt, &pAcc, &varChild);
	if (hr != S_OK)
	{
		OutputDebugString("**** COULD NOT GET ACCESSIBLE OBJECT FROM POINT *****\n");
		return;	// NO CHANGE
	}

	if (varChild.lVal != CHILDID_SELF)
	{
		char buff[128];
		sprintf_s(buff, 128, "Child ID = %d\n", varChild.lVal); // MDL
		OutputDebugString(buff);
	}
	// Use the variant from the above call so that it will get the CHILDID_SELF
	// or the child id that was returned.  This is what we want and get it by default
	// when we use the same variant.
	// Get the rect of that accessible object.
	long xLeft, yTop, dxWidth, dyHeight;
	RECT rectPoint;
	hr = pAcc->accLocation(&xLeft, &yTop, &dxWidth, &dyHeight, varChild);
	if (hr != S_OK)
	{
		OutputDebugString("**** COULD NOT GET ACCESSIBLE LOCATION *****\n");
		return;	// NO CHANGE
	}
	else
	{
		SetRect(&rectPoint, xLeft, yTop, xLeft+dxWidth, yTop+dyHeight);
		if (!EqualRect(&rectLastPoint, &rectPoint))
		{
			char buff[128];
			sprintf_s(buff, 128, "left=%d, top=%d, width=%d, height=%d\n", xLeft, yTop, dxWidth, dyHeight); // MDL
			OutputDebugString(buff);
			rectLastPoint = rectPoint;
			newRect = true;	// new rectangle to draw
		}
	}

	if (newRect)
	{
		// allow the previous rectangle to be erased
		if (hwndLastPoint != NULL)
			::InvalidateRect(hwndLastPoint, NULL, false);
		hwndLastPoint = WindowFromPoint(pt);
//		hwndLastPoint = GetParent(hwndLastPoint);	//GetAncestor(hwndLastPoint, GA_PARENT);
//		if (hwndLastPoint != NULL)
//		{
//			::InvalidateRect(hwndLastPoint, NULL, true);
//			::PostMessage(hwndLastPoint, WM_NCPAINT, 1, 0);
//		}

		// get the hwnd of that accessible object
		HWND curWindow;
		hr = ::WindowFromAccessibleObject(pAcc, &curWindow);
		if (hr != S_OK)
		{
			OutputDebugString("**** COULD NOT GET ACCESSIBLE HWND *****\n");
			return;	// NO CHANGE
		}

	//	hwndLastPoint = curWindow;

		DrawScreenRectangle(rectPoint);
	}
}


bool gCapturing = false;
//
//  FUNCTION: WndProc(HWND, unsigned, WORD, LONG)
//
//  PURPOSE:  Processes messages for the main window.
//
//  WM_COMMAND	- process the application menu
//  WM_PAINT	- Paint the main window
//  WM_DESTROY	- post a quit message and return
//
//
HWND hwndLastPoint = NULL;
RECT rectLastPoint;

HCURSOR hDefaultCursor;
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	int wmId, wmEvent;
	PAINTSTRUCT ps;
	HDC hdc;
	POINT pt;
	HWND hwndPoint = NULL;

////	EnumWindows(EnumWindowsProc, NULL);
////	UpdateTimes();

	switch (message)
	{
	case WM_LBUTTONDOWN:
		OutputDebugString("WM_LBUTTONDOWN\n");
		SetCapture(hWnd);
		gCapturing  = true;
		HCURSOR target;
		target = ::LoadCursor(hInst, MAKEINTRESOURCE(IDC_BEYE));	// IDC_BULLSEYE));
//		target = ::LoadCursor(hInst, "IDC_BULLSEYE");
		hDefaultCursor = ::SetCursor(target);
		break;
	case WM_LBUTTONUP:
		OutputDebugString("WM_LBUTTONUP\n");
		verbosePath = false;
		short shiftKeyState;
		shiftKeyState = GetAsyncKeyState(VK_SHIFT);
		if (shiftKeyState & 0x8000)
			verbosePath = true;
		ReleaseCapture();
		gCapturing = false;
		pt.x = LOWORD(lParam);
		pt.y = HIWORD(lParam);
		ClientToScreen(hWnd, &pt);
		GetCursorPos(&pt);
		::InvalidateRect(NULL, NULL, true);		// tell it to redraw itself
		DoTheWork(pt);
		::SetCursor(hDefaultCursor);
		break;
	case WM_NCLBUTTONUP:
		OutputDebugString("WM_NCLBUTTONUP\n");
		ReleaseCapture();
		gCapturing = false;
		pt.x = LOWORD(lParam);
		pt.y = HIWORD(lParam);
		GetCursorPos(&pt);
		::InvalidateRect(NULL, NULL, true);		// tell it to redraw itself
		DoTheWork(pt);
		break;
	case WM_MOUSEMOVE:
		if (!gCapturing )
			break;
		//pt.x = LOWORD(lParam);
		//pt.y = HIWORD(lParam);
		//hwndPoint = ::WindowFromPoint(pt);
		GetCursorPos(&pt);
		hwndPoint = ::WindowFromPoint(pt);
		GetAccPointForCursorPosition(pt, hwndLastPoint, rectLastPoint);

		//char buff[64];
		//sprintf(buff,"wm x=%I32d y=%I32d\n", LOWORD(lParam), HIWORD(lParam));
		//OutputDebugString(buff);
		if (hwndPoint)
		{
			//char buff[16];
			//sprintf(buff,"0x%04X\n", hwndPoint);
			//OutputDebugString(buff);
#if 0
			POINT clPt = pt;
			HWND parent = hwndPoint;
			::ScreenToClient(parent, &clPt);
			HWND child = ::RealChildWindowFromPoint(hwndPoint, pt);
			while (child && parent != child)
			{
				parent = child;
				clPt = pt;
				child = ::RealChildWindowFromPoint(hwndPoint, pt);
			}

			// see if we've changed windows
			if (parent && parent != hwndLastPoint)	// yes we have
			{
				if (hwndLastPoint)
				{
					::InvalidateRect(hwndLastPoint, NULL, true);		// tell it to redraw itself
					::PostMessage(hwndLastPoint, WM_NCPAINT, 1, 0);
//					::InvalidateRect(NULL, NULL, true);		// tell it to redraw itself
				}
				hwndLastPoint = parent;
			}
#endif
#if 0
////		HDC hdcScreen = ::GetDC(NULL);	// screen dc
////		if (hdcScreen != NULL)
			HDC hdcParent = ::GetWindowDC(parent);
			if (hdcParent!= NULL)
			{
////			Gdiplus::Graphics gr(hdcScreen);
				Gdiplus::Graphics gr(hdcParent);
				Gdiplus::Pen redPen(Gdiplus::Color(255, 255,0,0),2);
				RECT wRect;
				GetWindowRect(parent, &wRect);
				Gdiplus::Rect rect(0,0,abs(wRect.right-wRect.left), abs(wRect.bottom-wRect.top));
////				Gdiplus::Rect rect(wRect.left,wRect.top,abs(wRect.right-wRect.left), abs(wRect.bottom-wRect.top));
////				rect.Inflate(3,3);
				rect.Inflate(-1,-1);
				gr.DrawRectangle(&redPen, rect);
//				gr.DrawLine(&redPen, pt1, pt2);

				::ReleaseDC(parent, hdcParent);
////				::ReleaseDC(NULL, hdcScreen);
//				OutputDebugString("**After drawing line***\n");
			}
			//get to actual child window
////			::FlashWindow(parent, false);
#endif
			//////FLASHWINFO fwi;
			//////fwi.cbSize = sizeof(fwi);
			//////fwi.hwnd = parent;
			//////fwi.dwFlags = FLASHW_CAPTION || FLASHW_TIMERNOFG;
			//////fwi.dwTimeout = 3000;
			//////fwi.uCount = 5;
			//////::FlashWindowEx(&fwi);
		}
		break;
	case WM_COMMAND:
		wmId    = LOWORD(wParam);
		wmEvent = HIWORD(wParam);
		// Parse the menu selections:
		switch (wmId)
		{
		case IDM_ABOUT:
			DialogBox(hInst, (LPCTSTR)IDD_ABOUTBOX, hWnd, (DLGPROC)About);
			break;
		case IDM_EXIT:
			DestroyWindow(hWnd);
			break;
		default:
			return DefWindowProc(hWnd, message, wParam, lParam);
		}
		break;
	case WM_PAINT:
		{
		hdc = BeginPaint(hWnd, &ps);

		::DrawIcon(hdc, 0, 0, ::LoadIcon(hInst, MAKEINTRESOURCE(IDI_SMALL)));
		TCHAR text[ ] = "This is test text";

		//::TextOut(hdc, 5, 5, text, 17);
		//::TextOut(hdc, 5, 25, text, 17);

		EndPaint(hWnd, &ps);
		break;
		}
	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	default:
		return DefWindowProc(hWnd, message, wParam, lParam);
	}
	return 0;
}

LRESULT CALLBACK About(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
	switch (message)
	{
	case WM_INITDIALOG:
		return TRUE;

	case WM_COMMAND:
		if (LOWORD(wParam) == IDOK || LOWORD(wParam) == IDCANCEL)
		{
			EndDialog(hDlg, LOWORD(wParam));
			return TRUE;
		}
		break;
	}
	return FALSE;
}

void GetStringForRoleID(long role, char* roleName)
{
	switch (role)
	{
	case ROLE_SYSTEM_TITLEBAR	:
		strcpy_s(roleName, 9, "TITLEBAR"); //MDL
		break;
	case ROLE_SYSTEM_MENUBAR	:
		strcpy_s(roleName, 8, "MENUBAR"); //MDL
		break;
	case ROLE_SYSTEM_SCROLLBAR	:
		strcpy_s(roleName, 10, "SCROLLBAR"); //MDL
		break;
	case ROLE_SYSTEM_GRIP	:
		strcpy_s(roleName, 5, "GRIP"); //MDL
		break;
	case ROLE_SYSTEM_SOUND	:
		strcpy_s(roleName, 6, "SOUND"); //MDL
		break;
	case ROLE_SYSTEM_CURSOR	:
		strcpy_s(roleName, 7, "CURSOR"); //MDL
		break;
	case ROLE_SYSTEM_CARET	:
		strcpy_s(roleName, 6, "CARET"); //MDL
		break;
	case ROLE_SYSTEM_ALERT	:
		strcpy_s(roleName, 6, "ALERT"); //MDL
		break;
	case ROLE_SYSTEM_WINDOW	:
		strcpy_s(roleName, 7, "WINDOW"); //MDL
		break;
	case ROLE_SYSTEM_CLIENT	:
		strcpy_s(roleName, 7, "CLIENT"); //MDL
		break;
	case ROLE_SYSTEM_MENUPOPUP	:
		strcpy_s(roleName, 10, "MENUPOPUP"); //MDL
		break;
	case ROLE_SYSTEM_MENUITEM	:
		strcpy_s(roleName, 9, "MENUITEM"); //MDL
		break;
	case ROLE_SYSTEM_TOOLTIP	:
		strcpy_s(roleName, 8, "TOOLTIP"); //MDL
		break;
	case ROLE_SYSTEM_APPLICATION:
		strcpy_s(roleName, 12, "APPLICATION"); //MDL
		break;
	case ROLE_SYSTEM_DOCUMENT	:
		strcpy_s(roleName, 9, "DOCUMENT"); //MDL
		break;
	case ROLE_SYSTEM_PANE	:
		strcpy_s(roleName, 5, "PANE"); //MDL
		break;
	case ROLE_SYSTEM_CHART	:
		strcpy_s(roleName, 6, "CHART"); //MDL
		break;
	case ROLE_SYSTEM_DIALOG	:
		strcpy_s(roleName, 7, "DIALOG"); //MDL
		break;
	case ROLE_SYSTEM_BORDER	:
		strcpy_s(roleName, 7, "BORDER"); //MDL
		break;
	case ROLE_SYSTEM_GROUPING	:
		strcpy_s(roleName, 9, "GROUPING"); //MDL
		break;
	case ROLE_SYSTEM_SEPARATOR	:
		strcpy_s(roleName, 10, "SEPARATOR"); //MDL
		break;
	case ROLE_SYSTEM_TOOLBAR	:
		strcpy_s(roleName, 8, "TOOLBAR"); //MDL
		break;
	case ROLE_SYSTEM_STATUSBAR	:
		strcpy_s(roleName, 10, "STATUSBAR"); //MDL
		break;
	case ROLE_SYSTEM_TABLE	:
		strcpy_s(roleName, 6, "TABLE"); //MDL
		break;
	case ROLE_SYSTEM_COLUMNHEADER	:
		strcpy_s(roleName, 13, "COLUMNHEADER"); //MDL
		break;
	case ROLE_SYSTEM_ROWHEADER	:
		strcpy_s(roleName, 10, "ROWHEADER"); //MDL
		break;
	case ROLE_SYSTEM_COLUMN	:
		strcpy_s(roleName, 7, "COLUMN"); //MDL
		break;
	case ROLE_SYSTEM_ROW	:
		strcpy_s(roleName, 4, "ROW"); //MDL
		break;
	case ROLE_SYSTEM_CELL	:
		strcpy_s(roleName, 5, "CELL"); //MDL
		break;
	case ROLE_SYSTEM_LINK	:
		strcpy_s(roleName, 5, "LINK"); //MDL
		break;
	case ROLE_SYSTEM_HELPBALLOON	:
		strcpy_s(roleName, 12, "HELPBALLOON"); //MDL
		break;
	case ROLE_SYSTEM_CHARACTER	:
		strcpy_s(roleName, 10, "CHARACTER"); //MDL
		break;
	case ROLE_SYSTEM_LIST	:
		strcpy_s(roleName, 5, "LIST"); //MDL
		break;
	case ROLE_SYSTEM_LISTITEM	:
		strcpy_s(roleName, 9, "LISTITEM"); //MDL
		break;
	case ROLE_SYSTEM_OUTLINE	:
		strcpy_s(roleName, 8, "OUTLINE"); //MDL
		break;
	case ROLE_SYSTEM_OUTLINEITEM	:
		strcpy_s(roleName, 12, "OUTLINEITEM"); //MDL
		break;
	case ROLE_SYSTEM_PAGETAB	:
		strcpy_s(roleName, 8, "PAGETAB"); //MDL
		break;
	case ROLE_SYSTEM_PROPERTYPAGE	:
		strcpy_s(roleName, 13, "PROPERTYPAGE"); //MDL
		break;
	case ROLE_SYSTEM_INDICATOR	:
		strcpy_s(roleName, 10, "INDICATOR"); //MDL
		break;
	case ROLE_SYSTEM_GRAPHIC	:
		strcpy_s(roleName, 8, "GRAPHIC"); //MDL
		break;
	case ROLE_SYSTEM_STATICTEXT	:
		strcpy_s(roleName, 11, "STATICTEXT"); //MDL
		break;
	case ROLE_SYSTEM_TEXT	:
		strcpy_s(roleName, 5, "TEXT"); //MDL
		break;
	case ROLE_SYSTEM_PUSHBUTTON	:
		strcpy_s(roleName, 11, "PUSHBUTTON"); //MDL
		break;
	case ROLE_SYSTEM_CHECKBUTTON	:
		strcpy_s(roleName, 12, "CHECKBUTTON"); //MDL
		break;
	case ROLE_SYSTEM_RADIOBUTTON	:
		strcpy_s(roleName, 12, "RADIOBUTTON"); //MDL
		break;
	case ROLE_SYSTEM_COMBOBOX	:
		strcpy_s(roleName, 9, "COMBOBOX"); //MDL
		break;
	case ROLE_SYSTEM_DROPLIST	:
		strcpy_s(roleName, 9, "DROPLIST"); //MDL
		break;
	case ROLE_SYSTEM_PROGRESSBAR	:
		strcpy_s(roleName, 12, "PROGRESSBAR"); //MDL
		break;
	case ROLE_SYSTEM_DIAL	:
		strcpy_s(roleName, 5, "DIAL"); //MDL
		break;
	case ROLE_SYSTEM_HOTKEYFIELD	:
		strcpy_s(roleName, 12, "HOTKEYFIELD"); //MDL
		break;
	case ROLE_SYSTEM_SLIDER	:
		strcpy_s(roleName, 7, "SLIDER"); //MDL
		break;
	case ROLE_SYSTEM_SPINBUTTON	:
		strcpy_s(roleName, 11, "SPINBUTTON"); //MDL
		break;
	case ROLE_SYSTEM_DIAGRAM	:
		strcpy_s(roleName, 8, "DIAGRAM"); //MDL
		break;
	case ROLE_SYSTEM_ANIMATION	:
		strcpy_s(roleName, 10, "ANIMATION"); //MDL
		break;
	case ROLE_SYSTEM_EQUATION	:
		strcpy_s(roleName, 9, "EQUATION"); //MDL
		break;
	case ROLE_SYSTEM_BUTTONDROPDOWN	:
		strcpy_s(roleName, 15, "BUTTONDROPDOWN"); //MDL
		break;
	case ROLE_SYSTEM_BUTTONMENU	:
		strcpy_s(roleName, 11, "BUTTONMENU"); //MDL
		break;
	case ROLE_SYSTEM_BUTTONDROPDOWNGRID	:
		strcpy_s(roleName, 19, "BUTTONDROPDOWNGRID"); //MDL
		break;
	case ROLE_SYSTEM_WHITESPACE	:
		strcpy_s(roleName, 11, "WHITESPACE"); //MDL
		break;
	case ROLE_SYSTEM_PAGETABLIST	:
		strcpy_s(roleName, 12, "PAGETABLIST"); //MDL
		break;
	case ROLE_SYSTEM_CLOCK	:
		strcpy_s(roleName, 6, "CLOCK"); //MDL
		break;
	case ROLE_SYSTEM_SPLITBUTTON	:
		strcpy_s(roleName, 12, "SPLITBUTTON"); //MDL
		break;
	case ROLE_SYSTEM_IPADDRESS	:
		strcpy_s(roleName, 10, "IPADDRESS"); //MDL
		break;
	case ROLE_SYSTEM_OUTLINEBUTTON	:
		strcpy_s(roleName, 14, "OUTLINEBUTTON"); //MDL
		break;
	default:
		strcpy_s(roleName, 6, "EMPTY"); //MDL
		break;

	}
}


bool SetRegistryTestingForFLEX(bool testmode)
{
	WCHAR wideData[] = { 0x00ef };
	char charData[16];
	int numBytes = ::WideCharToMultiByte(CP_UTF8, 0, wideData, 1, charData, 16, NULL, NULL);
	charData[numBytes] = 0;
	HKEY hKeyLex;	// key to lex location
	if (RegOpenKey(HKEY_CURRENT_USER, "SOFTWARE\\SIL\\Language Explorer", &hKeyLex) != ERROR_SUCCESS)
		return false;

	TCHAR strTrue[] = "true";
	TCHAR strFalse[] = "false";
	TCHAR *strValue = strFalse;
	if (testmode)
		strValue = strTrue;

	RegSetValueEx(hKeyLex, "AccessibilityTestingMode", 0, REG_SZ, (const BYTE *)strValue, (DWORD)_tcslen(strValue));
	RegCloseKey(hKeyLex);

	return true;
}
