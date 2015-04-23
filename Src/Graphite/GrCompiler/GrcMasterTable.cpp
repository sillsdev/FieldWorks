/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrcMasterTable.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Master tables that are used for the post-parser.
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
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Add an item to the list. The first field of the symbol is the class or feature name
	key for the main master list.
	Arguments:
		psym		- glyph attribute or feature setting symbol
		pexpValue	- value of the above item
		nPR			- current global PointRadius setting
		munitPR		- current units for PointRadius
		fOverride	- current global AttributeOverride setting
		lnf			- line where statement occurs
----------------------------------------------------------------------------------------------*/
void GrcMasterTable::AddItem(Symbol psym, GdlExpression * pexpValue,
	int nPR, int munitPR, bool fOverride, GrpLineAndFile const& lnf,
	StrAnsi staDescription)
{
	Symbol psymBase = psym->BaseDefnForNonGeneric();

	Assert(psymBase->FitsSymbolType(ksymtClass) || psymBase->FitsSymbolType(ksymtFeature));

	GrcMasterValueList * pmvl;

	if (!m_vlistmapEntries.Retrieve(psymBase, &pmvl) || !pmvl)
	{
		pmvl = new GrcMasterValueList();
		m_vlistmapEntries.Insert(psymBase, pmvl);
	}

	pmvl->AddItem(psym, pexpValue, nPR, munitPR, fOverride, lnf, staDescription);

//	GdlAssignment * pasgn;
///
//	if (!pmvl->m_valmapEntries.Retrieve(psym, &pasgn) || !pasgn)
//	{
//		pasgn = new GdlAssignment(pexpValue, nPR, munitPR, fOverride, lnf);
//		pmvl->m_valmapEntries.Insert(psym, pasgn);
//	}
//	else
//	{
//		StrAnsi staT = (psym->FitsSymbolType(ksymtGlyphAttr)) ?
//			StrAnsi("glyph attribute assignment") :
//			StrAnsi("feature setting");
//		g_errorList.AddWarning(2523, pexpValue,
//			"Duplicate ", staT);
//
//		if ((lnf.PreProcessedLine() > pasgn->PreProcessedLine() && fOverride) ||
//			(lnf.PreProcessedLine() < pasgn->PreProcessedLine() && pasgn->Override()))
//			pasgn->Set(pexpValue, nPR, munitPR, fOverride, lnf);
//	}
}


/*----------------------------------------------------------------------------------------------
	Add an item to the list (ie, the name table list).
	Arguments:
		psym		- name symbol
		pexpValue	- value of the above item
		nPR			- current global PointRadius setting
		munitPR		- current units for PointRadius
		fOverride	- current global AttributeOverride setting
		lnf			- line where statement occurs
		staDescription - for error message
----------------------------------------------------------------------------------------------*/
void GrcMasterValueList::AddItem(Symbol psym, GdlExpression * pexpValue,
	int nPR, int munitPR, bool fOverride, GrpLineAndFile const& lnf,
	StrAnsi staDescription)
{
	GdlAssignment * pasgn;

	if (!m_valmapEntries.Retrieve(psym, &pasgn) || !pasgn)
	{
		pasgn = new GdlAssignment(pexpValue, nPR, munitPR, fOverride, lnf);
		m_valmapEntries.Insert(psym, pasgn);
	}
	else
	{
		g_errorList.AddWarning(2524, pexpValue,
			"Duplicate ", staDescription);

		if ((lnf.PreProcessedLine() > pasgn->PreProcessedLine() && fOverride) ||
			(lnf.PreProcessedLine() < pasgn->PreProcessedLine() && !pasgn->Override()))
		{
			pasgn->Set(pexpValue, nPR, munitPR, fOverride, lnf);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Return the value list associated with the given symbol, which should be a class or
	feature name.
----------------------------------------------------------------------------------------------*/
GrcMasterValueList * GrcMasterTable::ValueListFor(Symbol psym)
{
	Assert(psym->FitsSymbolType(ksymtClass) || psym->FitsSymbolType(ksymtFeature));

	GrcMasterValueList * pmvl;
	m_vlistmapEntries.Retrieve(psym, &pmvl);
	return pmvl;
}

/*----------------------------------------------------------------------------------------------
	Set up the data and settings for all the features from the
	master feature table.
----------------------------------------------------------------------------------------------*/
void GrcMasterTable::SetupFeatures()
{
	for (ValueListMap::iterator itvlistmap = m_vlistmapEntries.Begin();
		itvlistmap != m_vlistmapEntries.End();
		++itvlistmap)
	{
		Symbol psymFeatName = itvlistmap.GetKey();
		GrcMasterValueList * pmvl = itvlistmap.GetValue();

		Assert(psymFeatName->FitsSymbolType(ksymtFeature));

		GdlFeatureDefn * pfeat = dynamic_cast<GdlFeatureDefn*>(psymFeatName->Data());
		Assert(pfeat);

		pmvl->SetupFeatures(pfeat);
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize a GdlFeatureDefn with its settings from the
	master feature table.
----------------------------------------------------------------------------------------------*/
void GrcMasterValueList::SetupFeatures(GdlFeatureDefn * pfeat)
{
	StrAnsi fName = pfeat->Name();

	GdlExpression * pexpDefault = NULL;

	for (ValueMap::iterator itvalmap = m_valmapEntries.Begin();
		itvalmap != m_valmapEntries.End();
		++itvalmap)
	{
		Symbol psym = itvalmap.GetKey();
		Assert(psym->FitsSymbolType(ksymtFeatSetting));
		GdlAssignment * pasgnValue = itvalmap.GetValue();
		GdlExpression * pexp = pasgnValue->m_pexp;

		Assert(psym->FieldIs(0, fName));

		if (psym->FieldIs(1, "id"))
		{
			//	feature.id
			if (psym->FieldCount() == 2)
			{
				unsigned int nID;
				if (!pexp->ResolveToFeatureID(&nID))
					g_errorList.AddError(2147, pexp,
						"Feature id must be an integer or string of 4 characters or less");
				else if (nID == GdlFeatureDefn::kfidStdLang)
				{
					char rgch[20];
					itoa(nID, rgch, 10);
					g_errorList.AddError(2148, pexp,
						"Feature ID ", rgch, " is a reserved value");
					pfeat->SetID(nID);	// set it anyway, to avoid extra error message
				}
				else
					pfeat->SetID(nID);
			}
			else
				g_errorList.AddError(2149, pexp,
					"Invalid feature id statement");
		}

		else if (psym->FieldIs(1, "default"))
		{
			//	feature.default
			if (psym->FieldCount() == 2)
				pexpDefault = pexp;	// save for later
			else
				g_errorList.AddError(2150, pexp,
					"Invalid feature default statement");
		}

		else if (psym->FieldIs(1, "settings"))
		{
			//	feature.settings....
			GdlFeatureSetting * pfset = pfeat->FindOrAddSetting(psym->FieldAt(2),
				psym->LineAndFile());

			if (psym->FieldIs(3, "name"))
			{
				int nLangID;
				if (psym->FieldCount() != 5)
				{
					g_errorList.AddWarning(2525, pexp,
						"Invalid feature name statement");
					nLangID = LG_USENG;
				}
				else
				{
					StrAnsi strLang = psym->FieldAt(4);
					if (strLang[0] == '0' && strLang[1] == 'x')
						nLangID = strtoul(strLang.Chars() + 2, NULL, 16);	// 0x0409
					else if (strLang[0] == 'x')
						nLangID = strtoul(strLang.Chars() + 1, NULL, 16);	// x0409
					else
						nLangID = atoi(strLang);							// 1033
					if (nLangID == 0 && psym->FieldAt(4) != "0")
					{
						g_errorList.AddWarning(2526, pexp,
							"Invalid language ID: ",
							psym->FieldAt(4),
							"--should be an integer");
						nLangID = LG_USENG;
					}
				}
				if (!pexp->TypeCheck(kexptString))
					g_errorList.AddWarning(2527, pexp,
						"Feature setting name must be a string");
				else
					pfset->AddExtName((utf16)nLangID, pexp);
			}

			else if (psym->FieldIs(3, "value"))
			{
				int nValue;
				if (!pexp->ResolveToInteger(&nValue, false))
					g_errorList.AddError(2151, pexp,
						"Feature value must be an integer");
				else
				{
					if (!pexp->TypeCheck(kexptNumber, kexptZero, kexptOne))
						g_errorList.AddWarning(2528, pexp,
							"Feature setting value should not be a scaled number");
					pfset->SetValue(nValue);
				}
			}

			else
				g_errorList.AddError(2152, pexp,
					"Invalid feature settings field");
		}

		else if (psym->FieldIs(1, "name"))
		{
			//	feature.name.langID
			int nLangID;
			if (psym->FieldCount() != 3)
			{
				g_errorList.AddWarning(2529, pexp,
					"Invalid feature name statement");
				nLangID = LG_USENG;
			}
			else
			{
				StrAnsi strLang = psym->FieldAt(2);
				if (strLang[0] == '0' && strLang[1] == 'x')
					nLangID = strtoul(strLang.Chars() + 2, NULL, 16);	// 0x0409
				else if (strLang[0] == 'x')
					nLangID = strtoul(strLang.Chars() + 1, NULL, 16);	// x0409
				else
					nLangID = atoi(strLang);							// 1033
				if (nLangID == 0 && psym->FieldAt(2) != "0")
				{
					g_errorList.AddWarning(2530, pexp,
						"Invalid language ID: ",
						psym->FieldAt(2),
						"--should be an integer");
					nLangID = LG_USENG;
				}
			}
			if (!pexp->TypeCheck(kexptString))
				g_errorList.AddWarning(2531, pexp,
						"Feature name must be a string.");
			else
				pfeat->AddExtName((utf16)nLangID, pexp);
		}

		else
			g_errorList.AddError(2153, pexp, "Invalid feature statement");

	}

	// Now that all the settings are defined, handle the default.
	int nDefault;
	if (pexpDefault)
	{
		if (pexpDefault->ResolveToInteger(&nDefault, false))
		{
			if (!pexpDefault->TypeCheck(kexptNumber, kexptZero, kexptOne))
				g_errorList.AddWarning(2532, pexpDefault,
					"Feature setting value should not be a scaled number");
			if (!pfeat->FindSettingWithValue(nDefault))
			{
				if (pfeat->NumberOfSettings() == 0 && (nDefault == 0 || nDefault == 1))
				{
					// Boolean default--okay.
				}
				else
				{
					g_errorList.AddWarning(2533, pexpDefault,
						"Default feature setting is not among the defined values");
				}
			}
			pfeat->SetDefault(nDefault);
		}
		else
		{
			//	A kludge: the name of a feature setting was put in a slot-ref expression,
			//	even though that's not what it is (see the tree-walker).
			GdlSlotRefExpression * pexpsr =
				dynamic_cast<GdlSlotRefExpression *>(pexpDefault);
			if (!pexpsr || pexpsr->Alias() == "")
			{
				g_errorList.AddError(2154, pexpDefault,
					"Invalid default feature setting");
			}
			else
			{
				GdlFeatureSetting * pfsetDefault =
					pfeat->FindSetting(pexpsr->Alias());
				if (!pfsetDefault)
					g_errorList.AddError(2155, pexpDefault,
						"Default feature setting is undefined: ",
						pexpsr->Alias());
				else
					pfeat->SetDefault(pfsetDefault->Value());
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Set up the glyph attributes for all the classes from the master glyph
	attribute table.
----------------------------------------------------------------------------------------------*/
void GrcMasterTable::SetupGlyphAttrs()
{
	for (ValueListMap::iterator itvlistmap = m_vlistmapEntries.Begin();
		itvlistmap != m_vlistmapEntries.End();
		++itvlistmap)
	{
		Symbol psymClassName = itvlistmap.GetKey();
		GrcMasterValueList * pmvl = itvlistmap.GetValue();

		Assert(psymClassName->FitsSymbolType(ksymtClass));

		GdlGlyphClassDefn * pglfc = dynamic_cast<GdlGlyphClassDefn*>(psymClassName->Data());
		Assert(pglfc);

		pmvl->SetupGlyphAttrs(pglfc);
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize a single GlyphClassDefn with its settings from master glyph table.
----------------------------------------------------------------------------------------------*/
void GrcMasterValueList::SetupGlyphAttrs(GdlGlyphClassDefn * pglfc)
{
	Vector<Symbol> vpsymProcessed;

	for (ValueMap::iterator itvalmap = m_valmapEntries.Begin();
		itvalmap != m_valmapEntries.End();
		++itvalmap)
	{
		Symbol psym = itvalmap.GetKey();
		GdlAssignment * pasgnValue = itvalmap.GetValue();
		GdlExpression * pexp = pasgnValue->m_pexp;
//		int nPR = pasgnValue->m_nPR;
//		int munitPR = pasgnValue->m_munitPR;
//		int nStmtNo = pasgnValue->m_nStmtNo;
//		bool fOverride = pasgnValue->m_fOverride;

		Assert(psym->FieldIs(0, pglfc->Name()));

		if (psym->FieldIs(1, "directionality"))
		{
			if (psym->FieldCount() == 2)
			{
				pglfc->AddGlyphAttr(psym, pasgnValue);
				vpsymProcessed.Push(psym);
			}
			else
				g_errorList.AddError(2156, pexp,
					"Invalid use of directionality attribute");
		}
		else if (psym->FieldIs(1, "breakweight"))
		{
			if (psym->FieldCount() == 2)
			{
				pglfc->AddGlyphAttr(psym, pasgnValue);
				vpsymProcessed.Push(psym);
			}
			else
				g_errorList.AddError(2157, pexp,
					"Invalid use of breakweight attribute");
		}
		else if (psym->FieldIs(1, "component"))
		{
			if (psym->FieldCount() == 4 &&
				(psym->FieldIs(3, "left") ||
					psym->FieldIs(3, "bottom") ||
					psym->FieldIs(3, "right") ||
					psym->FieldIs(3, "top")))
			{
				pglfc->AddComponent(psym, pasgnValue);
				vpsymProcessed.Push(psym);
			}
			else
				g_errorList.AddError(2158, pexp, "Invalid use of component attribute");
		}
		else
		{
			if (psym->FitsSymbolType(ksymtGlyphMetric))
				//	We should never get here, but just in case.
				g_errorList.AddError(2159, pexp, "Cannot set the value of a glyph metric.");
			else
			{
				//	User-defined glyph attribute.
				pglfc->AddGlyphAttr(psym, pasgnValue);
				vpsymProcessed.Push(psym);
			}
		}
	}

	//	Now delete all the assignments that have been successfully processed from the map,
	//	since they are now "owned" by the class definition. (Otherwise we get double destruction.)

	for (int ipsym = 0; ipsym < vpsymProcessed.Size(); ipsym++)
	{
		m_valmapEntries.Delete(vpsymProcessed[ipsym]);
	}
}


/*----------------------------------------------------------------------------------------------
	Initialize a NameDefn with its values from master table.
----------------------------------------------------------------------------------------------*/
void GrcMasterValueList::SetupNameDefns(NameDefnMap & hmNameMap)
{
//	Vector<Symbol> vpsymProcessed;

	for (ValueMap::iterator itvalmap = m_valmapEntries.Begin();
		itvalmap != m_valmapEntries.End();
		++itvalmap)
	{
		Symbol psym = itvalmap.GetKey();
		GdlAssignment * pasgnValue = itvalmap.GetValue();
		GdlExpression * pexp = pasgnValue->m_pexp;
		//int nPR = pasgnValue->m_nPR;
		//int munitPR = pasgnValue->m_munitPR;
		bool fOverride = pasgnValue->m_fOverride;

		GdlStringExpression * pexpstr = dynamic_cast<GdlStringExpression *>(pexp);

		if (psym->FieldCount() != 2)
			g_errorList.AddError(2160, pexp, "Invalid name table entry.");

		else if (!pexpstr)
			g_errorList.AddError(2161, pexp,
				"Value of name table entry must be a string");

		else
		{
			StrAnsi staNameID = psym->FieldAt(0);
			StrAnsi staLangID = psym->FieldAt(1);

			int nNameID = atoi(staNameID.Chars());
			if (nNameID == 0 && staNameID != "0")
				g_errorList.AddWarning(2534, pexp,
					"Invalid name ID: ",
					staNameID,
					"--should be an integer");
			int nLangID = atoi(staLangID.Chars());
			if (nLangID == 0 && staLangID != "0")
				g_errorList.AddWarning(2535, pexp,
					"Invalid language ID: ",
					staLangID,
					"--should be an integer");

			GdlNameDefn * pndefn;
			if (!hmNameMap.Retrieve(nNameID, &pndefn))
			{
				pndefn = new GdlNameDefn();
				pndefn->SetLineAndFile(pasgnValue->LineAndFile());
				hmNameMap.Insert(nNameID, pndefn);
			}

			bool fFoundLang = false;
			for (int iextname = 0; iextname < pndefn->NameCount(); ++iextname)
			{
				if (pndefn->ExtName(iextname)->LanguageID() == nLangID)
				{
					fFoundLang = true;
					if (fOverride)
						pndefn->DeleteExtName(iextname);
					break;
				}
			}

			if (fFoundLang == false || fOverride)
			{
				pndefn->AddExtName((utf16)nLangID, pexp);
//				vpsymProcessed.Push(psym);
			}
		}
	}

	//	Now delete all the assignments that have been successfully processed from the map,
	//	since they are now "owned" by the name map. (Otherwise we get double destruction.)

//	for (Vector<Symbol>::iterator it = vpsymProcessed.Begin();
//		it != vpsymProcessed.End();
//		it++)
//	{
//		m_valmapEntries.Delete(*it);
//	}
}
