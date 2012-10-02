/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: PreCompiler.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods to implement the pre-compiler, which does error checking and adjustments.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Features
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Do the pre-compilation tasks for the feature definitions. Return false if
	compilation cannot continue due to an unrecoverable error.
----------------------------------------------------------------------------------------------*/
bool GrcManager::PreCompileFeatures(GrcFont * pfont)
{
	return m_prndr->PreCompileFeatures(this, pfont, &m_fxdFeatVersion);
}

/*--------------------------------------------------------------------------------------------*/

bool GdlRenderer::PreCompileFeatures(GrcManager * pcman, GrcFont * pfont, int * pfxdFeatVersion)
{
	*pfxdFeatVersion = 0x00010000;

	int nInternalID = 0;

	Set<unsigned int> setID;

	for (int ipfeat = 0; ipfeat < m_vpfeat.Size(); ipfeat++)
	{
		GdlFeatureDefn * pfeat = m_vpfeat[ipfeat];
		unsigned int nID = pfeat->ID();
		if (setID.IsMember(nID))
		{
			char rgch[20];
			if (nID > 0x00FFFFFF)
			{
				char rgchID[5];
				memcpy(rgch, &nID, 4);
				rgchID[0] = rgch[3]; rgchID[1] = rgch[2]; rgchID[2] = rgch[1]; rgchID[3] = rgch[0];
				rgchID[4] = 0;
				StrAnsi staTmp = "'";
				staTmp.Append(rgchID);
				staTmp.Append("'");
				memcpy(rgch, staTmp.Chars(), staTmp.Length() + 1);
			}
			else
				itoa(nID, rgch, 10);
			g_errorList.AddError(3152, pfeat, "Duplicate feature ID: ", rgch);
		}
		else
			setID.Insert(nID);

		if (pfeat->ErrorCheck())
		{
			pfeat->SetStdStyleFlag();
			pfeat->FillInBoolean(pcman->SymbolTable());
			pfeat->ErrorCheckContd();
			pfeat->CalculateDefault();
			pfeat->AssignInternalID(nInternalID);
			pfeat->RecordDebugInfo();
		}

		if (nID > 0x0000FFFF)
			*pfxdFeatVersion = 0x00020000;

		nInternalID++;
	}

	if (m_vpfeat.Size() > kMaxFeatures)
	{
		char rgchMax[20];
		itoa(kMaxFeatures, rgchMax, 10);
		char rgchCount[20];
		itoa(m_vpfeat.Size(), rgchCount, 10);
		g_errorList.AddError(3153, NULL,
			"Number of features (",
			rgchCount,
			") exceeds maximum of ",
			rgchMax);
	}

	return true;
}

/***********************************************************************************************
	Languages
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Do the pre-compilation tasks for the language-to-feature mappings. Return false if
	compilation cannot continue due to an unrecoverable error.
----------------------------------------------------------------------------------------------*/

bool GrcManager::PreCompileLanguages(GrcFont * pfont)
{
	for (int ilcls = 0; ilcls < m_vplcls.Size(); ilcls++)
		m_vplcls[ilcls]->PreCompile(this);

	m_prndr->CheckLanguageFeatureSize();

	return true;
}

/*--------------------------------------------------------------------------------------------*/

bool GdlLangClass::PreCompile(GrcManager * pcman)
{
	// Each item in the vectors corresponds to a feature assignment.
	for (int ifasgn = 0; ifasgn < m_vstaFeat.Size(); ifasgn++)
	{
		Symbol psymFeat = pcman->SymbolTable()->FindSymbol(m_vstaFeat[ifasgn]);
		if (!psymFeat)
		{
			g_errorList.AddError(3154, NULL, "Undefined feature: ", m_vstaFeat[ifasgn], m_vlnf[ifasgn]);
			continue;
		}

		GdlFeatureDefn * pfeat = psymFeat->FeatureDefnData();
		Assert(pfeat);
		StrAnsi staValue = m_vstaVal[ifasgn];
		GdlExpression * pexpVal = m_vpexpVal[ifasgn];
		int nVal;
		GdlFeatureSetting * pfset;
		if (pexpVal)
		{
			if (!pexpVal->ResolveToInteger(&nVal, false))
			{
				g_errorList.AddError(3155, pexpVal,
					"Feature value cannot be evaluated", m_vlnf[ifasgn]);
				continue;
			}
			else
			{
				pfset = pfeat->FindSettingWithValue(nVal);
				if (!pfset)
				{
					char rgchValue[20];
					itoa(nVal, rgchValue, 10);
					g_errorList.AddWarning(3523, NULL,
						"Feature ", pfeat->Name(), " has no defined setting corresponding to value ",
						rgchValue,
						m_vlnf[ifasgn]);
				}
			}
		}
		else
		{
			// Feature setting identifier
			pfset = pfeat->FindSetting(staValue);
			if (!pfset)
			{
				g_errorList.AddError(3156, NULL, "Undefined feature setting: ", staValue, m_vlnf[ifasgn]);
				continue;
			}
			nVal = pfset->Value();
		}

		// Store the feature values in the language items.
		for (int ilang = 0; ilang < m_vplang.Size(); ilang++)
			m_vplang[ilang]->AddFeatureValue(pfeat, pfset, nVal, m_vlnf[ifasgn]);

		if (m_vplang.Size() == 0 && ifasgn == 0)
		{
			g_errorList.AddWarning(3524, NULL, "No languages specified for language group '", m_staLabel,
				"'; settings will have no effect",
				m_vlnf[0]);
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Determine if there are enough languages and features to overflow the Sill table.
	Actually what would happen is that the offsets would overflow the 16 bits allotted for them.
----------------------------------------------------------------------------------------------*/
void GdlRenderer::CheckLanguageFeatureSize()
{
	// 12 = table info, + 8 bytes per language
	int cbSillSize = 12 + (m_vplang.Size() * 8);
	cbSillSize += 8; // bogus entry

	for (int ilang = 0; ilang < m_vplang.Size(); ilang++)
		cbSillSize += m_vplang[ilang]->NumberOfSettings() * 4; // 4 bytes per feature setting

	if (cbSillSize >= 0x0000FFFF)
	{
		g_errorList.AddError(3157, NULL,
			"Too many language-feature assignments to fit in Sill table");
	}
}
