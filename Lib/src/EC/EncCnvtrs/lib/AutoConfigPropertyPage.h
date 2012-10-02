#pragma once
#include <afxdlgs.h>

// base class for the PropertyPages in 'AutoConfigSheet'
// (it only offers a simplified MessageBox function)
class CAutoConfigPropertyPage : public CPropertyPage
{
	DECLARE_DYNAMIC(CAutoConfigPropertyPage)

public:
	CAutoConfigPropertyPage();   // don't care ctor (for no display--e.g. with the About dialog via the DisplayTestPage method
	CAutoConfigPropertyPage(UINT nID);

	// I think there's a compiler bug: I need to override the virtual function OnApply in the
	//  sub-class of this class, AutoConfigDlg. But for some reason, it isn't being called.
	//  I think the reason is that this class is defined in a static library which is used in
	//  an DLL that dynamically links with the MFC DLLs. [Possibly making matters worse, the DLL
	//  is such that it contains OLE/COM classes (i.e. so that it's not just a plane vanilla DLL,
	//  but a COM DLL with class factories and all).]
	// Anyway, I suspect that the MFC OnNotify handler can't call virtual functions that are more
	//  than once removed from the base class (CPropertyPage, in this case) in such a configuration.
	//  So I'm overriding OnApply in *this* class (the first sub-class of CPropertyPage) because it
	//  *is* being called in this case. I still need to give this class' sub-class, AutoConfigDlg,
	//  a chance to handle OnApply, so I'm creating a (pure) virtual function here which is called
	//  when OnApply is called. Now the sub-classes will get an opportunity to handle the OK and/or
	//  Apply buttons.
	// P.S. It wasn't enough to simply make a virtual function; that wasn't working either. It had
	//  to be a pure virtual function. This means that the three sub-classes of this class (one
	//  for the About tab, one for the Test tab, and one for the Setup tab) *all* have to implement
	//  this pure virtual function, even though only the last one really wants to do something
	//  with it.
	// Final note: the sub-class implementations should call the CPropertyPage::OnApply in their
	//  implementations (rather than the normal process of calling the immediate base class,
	//  CAutoConfigPropertyPage::OnApply, because otherwise, it'll be called recursively until
	//  the stack overflows...
	virtual BOOL WorkAroundCompilerBug_OnApply() = 0;
	virtual BOOL OnApply();

	// override this (non-virtual) function so we can keep track of whether it's clean or not.
	virtual void SetModified(BOOL bChanged = TRUE)
	{
		CPropertyPage::SetModified(bChanged);
		m_bChanged = bChanged;
	}

	virtual BOOL IsModified()
	{
		return m_bChanged;
	}

	int MessageBox(LPCTSTR lpszMessage, UINT nType = MB_OK );

protected:
	BOOL m_bChanged;
};
