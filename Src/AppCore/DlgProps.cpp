/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: HelpTopicProvider.cpp
Responsibility: TE Team
Last reviewed:

	Dialog properties code.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact(_T("SIL.AppCore.HelpTopicProvider"));

HelpTopicProvider::HelpTopicProvider(const achar * pszHelpFile)
{
	m_cref = 1;
	m_strHelpFile = pszHelpFile;
}

HelpTopicProvider::~HelpTopicProvider()
{
}

/*----------------------------------------------------------------------------------------------
	IUnknown Methods
----------------------------------------------------------------------------------------------*/
STDMETHODIMP HelpTopicProvider::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IHelpTopicProvider)
		*ppv = static_cast<IHelpTopicProvider *>(this);
	else
		return WarnHr(E_NOINTERFACE);

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

STDMETHODIMP_(ULONG) HelpTopicProvider::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


STDMETHODIMP_(ULONG) HelpTopicProvider::Release(void)
{
	Assert(m_cref > 0);
	ulong cref = ::InterlockedDecrement(&m_cref);
	if (!cref)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}

/*----------------------------------------------------------------------------------------------
	Get a requested property value.
	@param bstrPropName Property name.
	@param iKey Key to be appended to property name.
	@param bstrPropValue value returned for given name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP HelpTopicProvider::GetHelpString(BSTR bstrPropName, int iKey, BSTR * bstrPropValue)
{
	BEGIN_COM_METHOD;
	if (!bstrPropValue)
		ThrowInternalError(E_POINTER);

	SmartBstr sbstrValue;

	SmartBstr sbstrProjPropHelpFile = L"UserHelpFile";

	SmartBstr sbstrWsPropGeneralTabTopic = L"khtpWsGeneral";
	SmartBstr sbstrWsPropFontsTabTopic = L"khtpWsFonts";
	SmartBstr sbstrWsPropKeyboardTabTopic = L"khtpWsKeyboard";
	SmartBstr sbstrWsPropConvertersTabTopic = L"khtpWsConverters";
	SmartBstr sbstrWsPropSortingTabTopic = L"khtpWsSorting";
	SmartBstr sbstrWsPropCharactersTabTopic = L"khtpWsPUACharacters";
	SmartBstr sbstrWsWizardStep1 = L"khtpWsWizardStep1";
	SmartBstr sbstrWsWizardStep2 = L"khtpWsWizardStep2";
	SmartBstr sbstrWsWizardStep3 = L"khtpWsWizardStep3";
	SmartBstr sbstrWsWizardStep4 = L"khtpWsWizardStep4";
	SmartBstr sbstrFwNewLangProjTopic = L"khtpFwNewLangProjHelpTopic";
	/* NOTE:
		Never use a dash '-', only use an underscore '_' because C# resource files
		do not allow dashes in the names
	*/
	SmartBstr sbstrProjPropGeneral = L"ProjectProperties_General";
	SmartBstr sbstrProjPropWrtSys = L"ProjectProperties_WritingSystem";
	SmartBstr sbstrProjPropExtLnk = L"ProjectProperties_ExternalLinks";
	SmartBstr sbstrAddPUAChar = L"khtpWsAddPUAChar";
	SmartBstr sbstrModifyPUAChar = L"khtpWsModifyPUAChar";
	SmartBstr sbstrNoProjFound = L"khtpNoProjectFound";
	SmartBstr sbstrDisconnectDb = L"khtpDisconnectDb";
	SmartBstr sbstrBackupTab = L"khtpBackupRestore_BackupTab";
	SmartBstr sbstrRestoreTab = L"khtpBackupRestore_RestoreTab";
	SmartBstr sbstrBackupPwd = L"khtpBackupPassword";
	SmartBstr sbstrBackupRem = L"khtpBackupReminder";
	SmartBstr sbstrBackupRems = L"khtpBackupReminders";
	SmartBstr sbstrBackupSched = L"khtpBackupSchedule";
	SmartBstr sbstrBackupSchedPwd = L"khtpBackupSchedulePassword";
	SmartBstr sbstrRestoreOpts = L"khtpRestoreOptions";
	SmartBstr sbstrRestorePwd = L"khtpRestoreEnterPassword";
	SmartBstr sbstrEncCnvPropTab = L"khtpECProperties";
	SmartBstr sbstrEncCnvTestTab = L"khtpECTest";
	SmartBstr sbstrEncCnvAdvTab = L"khtpECAdvanced";
	SmartBstr sbstrStylesDialogTab1 = L"kstidStylesDialogTab1";
	SmartBstr sbstrStylesDialogTab2 = L"kstidStylesDialogTab2";
	SmartBstr sbstrStylesDialogTab3 = L"kstidStylesDialogTab3";
	SmartBstr sbstrStylesDialogTab4 = L"kstidStylesDialogTab4";
	SmartBstr sbstrStylesDialogTab5 = L"kstidStylesDialogTab5";
	SmartBstr sbstrSelectLanguage = L"khtpWsSelectLanguage";
	// Valid characters dialog
	SmartBstr sbstrValidCharsTab1 = L"khtpValidCharsTabData";
	SmartBstr sbstrValidCharsTab2 = L"khtpValidCharsTabBasedOn";
	SmartBstr sbstrValidCharsTab3 = L"khtpValidCharsTabManual";
	// Punctuation dialog
	SmartBstr sbstrPunctuationTab1 = L"khtpPunctuationMatchingPairs";
	SmartBstr sbstrPunctuationTab2 = L"khtpPunctuationPatterns";
	SmartBstr sbstrPunctuationTab3 = L"khtpPunctuationQuotationMarks";

	if (sbstrProjPropHelpFile.Equals(bstrPropName))					// help file name
		sbstrValue.Assign(m_strHelpFile.Chars());
	else if (sbstrFwNewLangProjTopic.Equals(bstrPropName))			// New lang. proj. dialog
		sbstrValue.Assign(L"User_Interface/Menus/File/Create_a_new_FieldWorks_project.htm");
	else if (sbstrWsPropGeneralTabTopic.Equals(bstrPropName))			// writing system dialog
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/"
			L"Writing_System_Properties_General_tab.htm");

	else if (sbstrValidCharsTab1.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Valid_Characters/"
			L"Valid_Characters_From_Data_tab.htm");
	else if (sbstrValidCharsTab2.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Valid_Characters/"
			L"Valid_Characters_Based_On_tab.htm");
	else if (sbstrValidCharsTab3.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Valid_Characters/"
			L"Valid_Characters_Manual_Entry_tab.htm ");

	else if (sbstrPunctuationTab1.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Punctuation_Usage/"
			L"Punctuation_Usage_Matching_Pairs_tab.htm");
	else if (sbstrPunctuationTab2.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Punctuation_Usage/"
			L"Punctuation_Usage_Punctuation_Patterns_tab.htm ");
	else if (sbstrPunctuationTab3.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Punctuation_Usage/"
			L"Punctuation_Usage_Quotation_Marks_tab.htm");

	else if (sbstrWsPropFontsTabTopic.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/"
			L"Writing_System_Properties_Fonts_tab.htm");
	else if (sbstrWsPropKeyboardTabTopic.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/"
			L"Writing_System_Properties_Keyboard_tab.htm");
	else if (sbstrWsPropConvertersTabTopic.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/"
			L"Writing_System_Properties_Converters_tab.htm");
	else if (sbstrWsPropSortingTabTopic.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/"
			L"Writing_System_Properties_Sorting_tab.htm");
	else if (sbstrWsPropCharactersTabTopic.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/"
			L"Writing_System_Properties_Characters_tab.htm");
	else if (sbstrWsWizardStep1.Equals(bstrPropName))				// writing system wizard
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Defining_a_New_Writing_System/"
			L"Step_1_of_4_Language_ID.htm");
	else if (sbstrWsWizardStep2.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Defining_a_New_Writing_System/"
			L"Step_2_of_4_Writing_System.htm");
	else if (sbstrWsWizardStep3.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Defining_a_New_Writing_System/"
			L"Step_3_of_4_Appearance.htm");
	else if (sbstrWsWizardStep4.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Defining_a_New_Writing_System/"
			L"Step_4_of_4_Input.htm");
	else if (sbstrProjPropGeneral.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Project_Properties/"
			L"Project_Properties_General_tab.htm");
	else if (sbstrProjPropWrtSys.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Project_Properties/"
			L"Project_Properties_Writing_Systems_tab.htm");
	else if (sbstrProjPropExtLnk.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Project_Properties/"
			L"Project_Properties_External_Links_tab.htm");
	else if (sbstrAddPUAChar.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/"
			L"Add_PUA_Character.htm");
	else if (sbstrModifyPUAChar.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/"
			L"Private_Use_Area_character_properties.htm");
	else if (sbstrNoProjFound.Equals(bstrPropName))
		sbstrValue.Assign(L"Overview/Welcome_to_Fieldworks.htm");
	else if (sbstrDisconnectDb.Equals(bstrPropName))
		sbstrValue.Assign(
			L"Basic_Tasks/Collaborating_with_Others/Fieldworks_Shutdown_Progress.htm");
	else if (sbstrBackupTab.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/"
			L"Backup_and_Restore_Backup_tab.htm");
	else if (sbstrRestoreTab.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/"
			L"Backup_and_Restore_Restore_tab.htm");
	else if (sbstrBackupPwd.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/Backup_Password.htm");
	else if (sbstrBackupRem.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/Backup_Reminder.htm");
	else if (sbstrBackupRems.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/Backup_Reminders.htm");
	else if (sbstrBackupSched.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/Backup_Schedule.htm");
	else if (sbstrBackupSchedPwd.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/"
			L"Backup_Schedule_Password.htm");
	else if (sbstrRestoreOpts.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/Restore_Options.htm");
	else if (sbstrRestorePwd.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/File/Backup_and_Restore/Enter_Password.htm");
	else if (sbstrEncCnvPropTab.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Encoding_Converters/"
			L"Encoding_Converters_Properties_tab.htm");
	else if (sbstrEncCnvTestTab.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Encoding_Converters/"
			L"Encoding_Converters_Test_tab.htm");
	else if (sbstrEncCnvAdvTab.Equals(bstrPropName))
		sbstrValue.Assign(L"Advanced_Tasks/Writing_Systems/Encoding_Converters/"
			L"Encoding_Converters_Advanced_tab.htm");
	else if (sbstrStylesDialogTab1.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/Format/Style/Style_General_tab.htm");
	else if (sbstrStylesDialogTab2.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/Format/Style/Style_Font_tab.htm");
	else if (sbstrStylesDialogTab3.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/Format/Style/Style_Paragraph_tab.htm");
	else if (sbstrStylesDialogTab4.Equals(bstrPropName))
		sbstrValue.Assign(
			L"User_Interface/Menus/Format/Style/Style_Bullets_and_Numbering_tab.htm");
	else if (sbstrStylesDialogTab5.Equals(bstrPropName))
		sbstrValue.Assign(L"User_Interface/Menus/Format/Style/Style_Border_tab.htm");
	else if (sbstrSelectLanguage.Equals(bstrPropName))
		sbstrValue.Assign(
L"Advanced_Tasks/Writing_Systems/Modifying_a_Writing_System/Select_Language_dialog_box.htm");
	else
		sbstrValue.Assign(L"Unknown");

	// make the value string into a BSTR to return it
	*bstrPropValue = ::SysAllocString(sbstrValue.Bstr());

	END_COM_METHOD(g_fact, IID_IHelpTopicProvider)
}
