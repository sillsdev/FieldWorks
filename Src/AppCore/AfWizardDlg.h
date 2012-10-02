/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfWizardDlg.h
Responsibility: John Wimbish
Last reviewed: never

Description:
	Declaration of the generic Wizard dialog classes. These classes provide a framework for
	building a wizard. The framework handles Next, Back, Finish actions, and provides numerous
	hooks (via virtual methods available for subclassing) so that individual wizards can be
	tailored for specific needs.

	The framework is patterned after the MFC classes for providing wizards (although with
	a few changes that make sense in our Af framework.)

	Refer to the AfWizardDlg.cpp file for detailed documentation about each method which a
	subclass might choose to override.

	An example of a wizard may be found in the Explorer application, in the ExNewLP.h and
	ExNewLP.cpp files.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFWIZARDDLG_H_INCLUDED
#define AFWIZARDDLG_H_INCLUDED


/*----------------------------------------------------------------------------------------------
	This superclass provides the behavior for each page in a wizard. Each page should be
	defined as a subclass of this class.

	Hungarian: wizp.
----------------------------------------------------------------------------------------------*/
class AfWizardPage : public AfDialogView
{
typedef AfDialogView SuperClass;
friend class AfWizardDlg;

public:
	AfWizardPage(uint nIdTemplate);
	virtual ~AfWizardPage();

protected:
	// Methods that are typically subclassed
	virtual bool OnQueryCancel();           // Wanna cancel the Cancel btn action?
	virtual bool OnCancel();                // Cancel btn was pressed
	virtual bool OnSetActive();             // This page is becoming active
	virtual bool OnKillActive();            // Another page is becoming active
	virtual HWND OnWizardBack();            // The user has clicked on the Back btn
	virtual HWND OnWizardNext();            // The user has clicked on the Next btn
	virtual bool OnWizardFinish();          // The user clicked on the Finish btn

	// Implementational
	void SetWindowText(uint ridCtrl, uint ridResourceString);
	virtual bool OnHelp();					// Allow for individual page help.

	// Attributes
	AfWizardDlg * m_pwiz;                   // The wizard which owns this page
};

typedef GenSmartPtr<AfWizardPage> AfWizardPagePtr;


/*----------------------------------------------------------------------------------------------
	This superclass provides the behavior for the entire wizard dialog. A wizard is implemented
	by subclassing this class. In particular, you will want to override the OnInitDlg method;
	your override will want to call the AddPage method for each page you will add to the
	wizard.

	Hungarian: wiz.
----------------------------------------------------------------------------------------------*/
class AfWizardDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	AfWizardDlg();
	virtual ~AfWizardDlg();
	void AddPage(AfWizardPage * pwizp);       // Appends a new page onto the wizard
	int  GetPageCount();                      // The number of pages currently in the wizard
	int  GetActiveIndex();                    // The index of the current page
	int  GetPageIndex(AfWizardPage * pwizp);  // The index of the specified page
	bool SetActivePage(int iPageNo);          // Makes a new page active
	bool SetActivePage(AfWizardPage * pwizp); // Makes a new page active
	void SetFinishText(uint rid);             // Set the text for the "Finish" button
	AfWizardPage * GetPage(int iPageNo);      // Returns a pointer to the desired page
	AfWizardPage * GetActivePage();           // Returns a pointer to the current page
	bool EnableNextButton(bool fEnable = true);	// Enables/Disables the 'Next' button

protected:
	// Methods that are typically subclassed
	virtual bool OnWizardNext();              // The user clicked on the Next button
	virtual bool OnWizardBack();              // The user clicked on the Back button
	virtual bool OnWizardFinish();            // The user clicked on the Finish btn
	virtual bool OnCancel();                  // Cancel btn was pressed

	// Implementational (subclassing is less frequent)
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void OnNextButtonPressed();               // Handle the Next (& Finish) button action.
	void OnBackButtonPressed();               // Handle the Back button action.
	virtual bool OnHelp();                    // Allow for individual page help.

	// Attributes
	int m_cPages;                             // Number of pages currently in the dialog
	enum { kcdwizp = 16 };                    // Max number of pages the wizard can have
	AfWizardPagePtr m_rgwizp[kcdwizp];        // Array of pages in the wizard
	int m_iCurrentPage;                       // The page we're currently displaying
	uint m_ridFinishText;                     // The text for the "Finish" button.
};

typedef GenSmartPtr<AfWizardDlg> AfWizardDlgPtr;

#endif  // !AFWIZARDDLG_H_INCLUDED
