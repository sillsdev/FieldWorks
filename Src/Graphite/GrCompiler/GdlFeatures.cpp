/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GdlFeatures.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implement the classes to handle the features mechanism.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Methods: Post-parser
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Return the feature setting with the given name; create it if necessary.
----------------------------------------------------------------------------------------------*/
GdlFeatureSetting * GdlFeatureDefn::FindOrAddSetting(StrAnsi sta, GrpLineAndFile & lnf)
{
	GdlFeatureSetting * pfset = FindSetting(sta);
	if (pfset)
		return pfset;

	pfset = new GdlFeatureSetting();
	pfset->SetName(sta);
	pfset->SetLineAndFile(lnf);
	m_vpfset.Push(pfset);
	return pfset;
}

/*----------------------------------------------------------------------------------------------
	Return the feature setting with the given name, or NULL if none.
----------------------------------------------------------------------------------------------*/
GdlFeatureSetting * GdlFeatureDefn::FindSetting(StrAnsi sta)
{
	for (int i = 0; i < m_vpfset.Size(); ++i)
	{
		if (m_vpfset[i]->m_staName == sta)
			return m_vpfset[i];
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Return the feature setting with the given value, or NULL if none.
----------------------------------------------------------------------------------------------*/
GdlFeatureSetting * GdlFeatureDefn::FindSettingWithValue(int n)
{
	for (int i = 0; i < m_vpfset.Size(); i++)
	{
		if (m_vpfset[i]->m_nValue == n)
			return m_vpfset[i];
	}
	return NULL;
}


/***********************************************************************************************
	Methods: Pre-compiler
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Check for various error conditions.
----------------------------------------------------------------------------------------------*/
bool GdlFeatureDefn::ErrorCheck()
{
	if (m_fStdLang)
	{
		Assert(m_vpfset.Size() == 0);
		m_fIDSet = true;
		return true;
	}

	//	Feature with no ID: fatal error
	if (m_fIDSet == false)
	{
		g_errorList.AddError(3158, this,
			"No id specified for feature ",
			m_staName);
		m_fFatalError = true;
		return false;
	}

	//	Duplicate IDs in feature settings: fatal error
	Set<int> setnIDs;
	for (int ifset = 0; ifset < m_vpfset.Size(); ++ifset)
	{
		int nValue = m_vpfset[ifset]->m_nValue;
		if (setnIDs.IsMember(nValue))
		{
			g_errorList.AddError(3159, m_vpfset[ifset],
				"Duplicate feature setting values in ",
				m_staName);
			m_fFatalError = true;
			return false;
		}
		setnIDs.Insert(nValue);
	}

	//	Feature with only one setting: warning
	if (m_vpfset.Size() == 1)
		g_errorList.AddWarning(3525, this,
			"Only one setting given for feature ",
			m_staName);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Determine if this style is the standard style; if so, set the flag.
----------------------------------------------------------------------------------------------*/
void GdlFeatureDefn::SetStdStyleFlag()
{
	if (m_nID == kfidStdStyles)
		m_fStdStyles = true;
}

/*----------------------------------------------------------------------------------------------
	If there are no settings for this feature, fill in with the default boolean settings.
----------------------------------------------------------------------------------------------*/
void GdlFeatureDefn::FillInBoolean(GrcSymbolTable * psymtbl)
{
	if (m_fStdLang)
		return;

	bool fBoolean = false;

	if (m_vpfset.Size() == 0)
	{
		GdlFeatureSetting * pfsetFalse = new GdlFeatureSetting();
		pfsetFalse->CopyLineAndFile(*this);
		pfsetFalse->m_nValue = 0;
		pfsetFalse->m_fHasValue = true;
		pfsetFalse->m_staName = StrAnsi("false");
		pfsetFalse->m_vextname.Push(GdlExtName(StrUni("False"), LG_USENG));
		m_vpfset.Push(pfsetFalse);

		GdlFeatureSetting * pfsetTrue = new GdlFeatureSetting();
		pfsetTrue->CopyLineAndFile(*this);
		pfsetTrue->m_nValue = 1;
		pfsetTrue->m_fHasValue = true;
		pfsetTrue->m_staName = StrAnsi("true");
		pfsetTrue->m_vextname.Push(GdlExtName(StrUni("True"), LG_USENG));
		m_vpfset.Push(pfsetTrue);

		if (!m_fDefaultSet)
			m_nDefault = 0;
		m_fDefaultSet = true;

		fBoolean = true;

	}
	else if (m_vpfset.Size() == 2)
	{
		fBoolean = ((m_vpfset[0]->m_nValue == 0 && m_vpfset[1]->m_nValue == 1)
			|| (m_vpfset[0]->m_nValue == 1 && m_vpfset[1]->m_nValue == 0));
	}

	if (fBoolean)
	{
		// Mark the expression type as boolean, not number.
		Symbol psymFeat = psymtbl->FindSymbol(m_staName);
		Assert(psymFeat->ExpType() == kexptNumber);
		psymFeat->SetExpType(kexptBoolean);
	}
}

/*----------------------------------------------------------------------------------------------
	More error checks that should be done after creating the boolean settings.
----------------------------------------------------------------------------------------------*/
void GdlFeatureDefn::ErrorCheckContd()
{
	if (m_fStdLang)
	{
		return;
	}

	Set<int> setnValues;
	for (int ifset = 0; ifset < m_vpfset.Size(); ++ifset)
	{
		//	Feature setting with no value set: warning
		if (!m_vpfset[ifset]->m_fHasValue)
		{
			g_errorList.AddWarning(3526, this,
				"Feature setting with no value specified; value will be zero: ",
				m_vpfset[ifset]->m_staName);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Calculate the default setting, if it has not been stated explicitly. The default will
	be the minimum value.
----------------------------------------------------------------------------------------------*/
void GdlFeatureDefn::CalculateDefault()
{
	if (m_fStdLang)
	{
		m_nDefault = 0;
		m_fDefaultSet = true;
		return;
	}

	if (m_fDefaultSet)
	{
		for (int ifset = 0; ifset < m_vpfset.Size(); ++ifset)
		{
			if (m_vpfset[ifset]->m_nValue == m_nDefault)
			{
				m_pfsetDefault = m_vpfset[ifset];
				return;
			}
		}
		//	Default setting not found; make one (with no name).
		GdlFeatureSetting * pfset = new GdlFeatureSetting();
		pfset->SetValue(m_nDefault);
		m_vpfset.Push(pfset);
		m_pfsetDefault = pfset;
		m_fDefaultSet = true;
		return;
	}

	if (m_vpfset.Size() == 0)
		return;

	m_nDefault = m_vpfset[0]->m_nValue;
	m_pfsetDefault = m_vpfset[0];
	for (int ifset = 1; ifset < m_vpfset.Size(); ++ifset)
	{
		if (m_vpfset[ifset]->m_nValue < m_nDefault)
		{
			m_nDefault = m_vpfset[ifset]->m_nValue;
			m_pfsetDefault = m_vpfset[ifset];
		}
	}
	m_fDefaultSet = true;
}

/*----------------------------------------------------------------------------------------------
	Assign name table ids to the feature itself and all its settings. Each of these gets a
	unique id which starts with nFirst and is incremented.
	Return the id that should be used next.
----------------------------------------------------------------------------------------------*/
utf16 GdlFeatureDefn::SetNameTblIds(utf16 wFirst)
{
	utf16 wNameTblId = wFirst;
	m_wNameTblId = wNameTblId++;

	int cnFeatSet = m_vpfset.Size();
	for (int i = 0; i < cnFeatSet; i++)
	{
		m_vpfset[i]->SetNameTblId(wNameTblId++);
	}

	return wNameTblId;
}

StrUni GdlExtName::s_stuNoName("NoName"); // static
/*----------------------------------------------------------------------------------------------
	Push onto the argument vectors information for the feature itself and all its settings.
	The three arrays must remain parallel. Retrieve all the external names and their
	corresponding language ids and the name table ids (assigned in SetNameTblIds().
----------------------------------------------------------------------------------------------*/
bool GdlFeatureDefn::NameTblInfo(Vector<StrUni> * pvstuExtNames, Vector<utf16> * pvwLangIds,
		Vector<utf16> * pvwNameTblIds)
{
	// store data for the feature itself
	int cnExtNames = m_vextname.Size();
	if (cnExtNames <= 0)
	{
		pvstuExtNames->Push(GdlExtName::s_stuNoName);
		pvwLangIds->Push(LG_USENG);
		pvwNameTblIds->Push(m_wNameTblId);
	}
	else
	{
		for (int i = 0; i < cnExtNames; i++)
		{
			pvstuExtNames->Push(m_vextname[i].Name());
			pvwLangIds->Push(m_vextname[i].LanguageID());
			pvwNameTblIds->Push(m_wNameTblId);
		}
	}

	// store data for all settings
	int cnSettings = m_vpfset.Size();
	for (int i = 0; i < cnSettings; i++)
	{
		GdlFeatureSetting * pFeatSet = m_vpfset[i];
		cnExtNames = pFeatSet->m_vextname.Size();
		if (cnExtNames <= 0)
		{
			pvstuExtNames->Push(GdlExtName::s_stuNoName);
			pvwLangIds->Push(LG_USENG);
			pvwNameTblIds->Push(pFeatSet->NameTblId());
		}
		else
		{
			for (int j = 0; j < cnExtNames; j++)
			{
				pvstuExtNames->Push(pFeatSet->m_vextname[j].Name());
				pvwLangIds->Push(pFeatSet->m_vextname[j].LanguageID());
				pvwNameTblIds->Push(pFeatSet->NameTblId());
			}
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Record something about the feature in the debug database.
----------------------------------------------------------------------------------------------*/
void GdlFeatureDefn::RecordDebugInfo()
{
}

/*----------------------------------------------------------------------------------------------
	Add a default feature setting to the language definition.
----------------------------------------------------------------------------------------------*/
void GdlLanguageDefn::AddFeatureValue(GdlFeatureDefn * pfeat, GdlFeatureSetting * pfset,
	int nFset, GrpLineAndFile & lnf)
{
	for (int ifeat = 0; ifeat < m_vpfeat.Size(); ifeat++)
	{
		if (m_vpfeat[ifeat] == pfeat)
		{
			if (m_vnFset[ifeat] == nFset)
				return;
			else
			{
				g_errorList.AddError(3160, NULL, "Duplicate language feature setting", lnf);
				return;
			}
		}
	}
	m_vpfeat.Push(pfeat);
	m_vpfset.Push(pfset);
	m_vnFset.Push(nFset);
	Assert(m_vpfeat.Size() == m_vpfset.Size());
	Assert(m_vpfeat.Size() == m_vnFset.Size());
}
