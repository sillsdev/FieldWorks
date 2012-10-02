/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003, 2006 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestFwXmlData.h
Responsibility:
Last reviewed:

	Unit tests for the FwXmlData class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTFWXMLDATA_H_INCLUDED
#define TESTFWXMLDATA_H_INCLUDED

#pragma once

#include "testFwCellar.h"
#define CMCG_SQL_DEFNS
#undef CMCG_SQL_ENUM
#include "LangProj.sqh"		// for kflidLangProject_AnthroList
#include <oledb.h>

namespace TestFwCellar
{
#define COMBINING_DIAERESIS L"\x0308" // cc 230
#define COMBINING_MACRON L"\x0304" // cc 230
#define COMBINING_OVERLINE L"\x0305" // not involved in any compositions with characters; cc 230
#define COMBINING_LEFT_HALF_RING_BELOW L"\x031C" // not involved in any compositions; cc 220.
#define COMBINING_BREVE L"\x0306" // cc 230
#define BREVE L"\x02D8" // compatibility decomposition to 0020 0306
#define MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"\xD834\xDDB9" // 1D1B9
#define MUSICAL_SYMBOL_COMBINING_STEM L"\xD834\xDD65" // 1D165

	// These GUID values are taken from MoreLexEntries.xml (and SemanticDomainList.xml)
	static const GUID kguidLexSenseCigaroid =	// 1ED831E1-21DB-46BC-9111-646801B85CE4
		{ 0x1ED831E1, 0x21DB, 0x46BC, { 0x91, 0x11, 0x64, 0x68, 0x01, 0xB8, 0x5C, 0xE4 } };
	static const GUID kguidLexSensePetroleum =	// B42DFCF6-701F-4ED9-BAF4-99631D7A9ACE
		{ 0xB42DFCF6, 0x701F, 0x4ED9, { 0xBA, 0xF4, 0x99, 0x63, 0x1D, 0x7A, 0x9A, 0xCE } };
	static const GUID kguidLexSenseForest =		// DCE5680A-ABB6-4A27-8FC1-95E65935C111
		{ 0xDCE5680A, 0xABB6, 0x4A27, { 0x8F, 0xC1, 0x95, 0xE6, 0x59, 0x35, 0xC1, 0x11 } };
	static const GUID kguidLexSenseEarth =		// DF90F946-33A0-491E-B8EE-8CE6B60C4243
		{ 0xDF90F946, 0x33A0, 0x491E, { 0xB8, 0xEE, 0x8C, 0xE6, 0xB6, 0x0C, 0x42, 0x43 } };

	static const GUID kguidSemDomPhysicaluniverse =	// 63403699-07C1-43F3-A47C-069D6E4316E5
		{ 0x63403699, 0x07C1, 0x43F3, { 0xA4, 0x7C, 0x06, 0x9D, 0x6E, 0x43, 0x16, 0xE5 } };
	static const GUID kguidSemDomSky =			 	// 999581C4-1611-4ACB-AE1B-5E6C1DFE6F0C
		{ 0x999581C4, 0x1611, 0x4ACB, { 0xAE, 0x1B, 0x5E, 0x6C, 0x1D, 0xFE, 0x6F, 0x0C } };
	static const GUID kguidSemDomSkyObjects =	 	// 61A83439-7ABD-433A-AA11-82B92B6D8CC4
		{ 0x61A83439, 0x7ABD, 0x433A, { 0xAA, 0x11, 0x82, 0xB9, 0x2B, 0x6D, 0x8C, 0xC4 } };
	static const GUID kguidSemDomSun =			 	// DC1A2C6F-1B32-4631-8823-36DACC8CB7BB
		{ 0xDC1A2C6F, 0x1B32, 0x4631, { 0x88, 0x23, 0x36, 0xDA, 0xCC, 0x8C, 0xB7, 0xBB } };
	static const GUID kguidSemDomMoon =			 	// 1BD42665-0610-4442-8D8D-7C666FEE3A6D
		{ 0x1BD42665, 0x0610, 0x4442, { 0x8D, 0x8D, 0x7C, 0x66, 0x6F, 0xEE, 0x3A, 0x6D } };
	static const GUID kguidSemDomStar =			 	// B044E890-CE30-455C-AEDE-7E9D5569396E
		{ 0xB044E890, 0xCE30, 0x455C, { 0xAE, 0xDE, 0x7E, 0x9D, 0x55, 0x69, 0x39, 0x6E } };
	static const GUID kguidSemDomPlanet =		 	// A0D073DF-D413-4DFD-9BA1-C3C68F126D90
		{ 0xA0D073DF, 0xD413, 0x4DFD, { 0x9B, 0xA1, 0xC3, 0xC6, 0x8F, 0x12, 0x6D, 0x90 } };
	static const GUID kguidSemDomUFO =			 	// 63781E49-4FAE-4FAD-A4BE-0F254CB8D7DC
		{ 0x63781E49, 0x4FAE, 0x4FAD, { 0xA4, 0xBE, 0x0F, 0x25, 0x4C, 0xB8, 0xD7, 0xDC } };
	static const GUID kguidSemDomAir =			 	// E836B01B-6C1A-4D41-B90A-EA5F349F88D4
		{ 0xE836B01B, 0x6C1A, 0x4D41, { 0xB9, 0x0A, 0xEA, 0x5F, 0x34, 0x9F, 0x88, 0xD4 } };
	static const GUID kguidSemDomWeather =		 	// B4AA4BBD-8ABF-4503-96E4-05C75EFD23D5
		{ 0xB4AA4BBD, 0x8ABF, 0x4503, { 0x96, 0xE4, 0x05, 0xC7, 0x5E, 0xFD, 0x23, 0xD5 } };
	static const GUID kguidSemDomWorld =		 	// B47D2604-8B23-41E9-9158-01526DD83894
		{ 0xB47D2604, 0x8B23, 0x41E9, { 0x91, 0x58, 0x01, 0x52, 0x6D, 0xD8, 0x38, 0x94 } };
	static const GUID kguidSemDomLand =			 	// CCE98603-FF8F-4213-945A-BD6746716139
		{ 0xCCE98603, 0xFF8F, 0x4213, { 0x94, 0x5A, 0xBD, 0x67, 0x46, 0x71, 0x61, 0x39 } };
	static const GUID kguidSemDomMountain =		 	// 0AC5E5F9-E7FE-4D37-A631-EAB1CEB1F8AE
		{ 0x0AC5E5F9, 0xE7FE, 0x4D37, { 0xA6, 0x31, 0xEA, 0xB1, 0xCE, 0xB1, 0xF8, 0xAE } };
	static const GUID kguidSemDomVolcano =		 	// D50F3921-FCEA-4AC9-B64A-25BF47DC3292
		{ 0xD50F3921, 0xFCEA, 0x4AC9, { 0xB6, 0x4A, 0x25, 0xBF, 0x47, 0xDC, 0x32, 0x92 } };
	static const GUID kguidSemDomWater =		 	// 60364974-A005-4567-82E9-7AAEFF894AB0
		{ 0x60364974, 0xA005, 0x4567, { 0x82, 0xE9, 0x7A, 0xAE, 0xFF, 0x89, 0x4A, 0xB0 } };
	static const GUID kguidSemDomBodiesOfWater = 	// 79EBB5CE-F0FD-4FB5-9F22-1FA4965A555B
		{ 0x79EBB5CE, 0xF0FD, 0x4FB5, { 0x9F, 0x22, 0x1F, 0xA4, 0x96, 0x5A, 0x55, 0x5B } };
	static const GUID kguidSemDomLiquids =		 	// BE065ABB-AE65-4CC8-940D-D0181BEEE04F
		{ 0xBE065ABB, 0xAE65, 0x4CC8, { 0x94, 0x0D, 0xD0, 0x18, 0x1B, 0xEE, 0xE0, 0x4F } };
	static const GUID kguidSemDomLivingThings =	 	// 8D47C9EC-80C4-4309-9848-C453DCD71182
		{ 0x8D47C9EC, 0x80C4, 0x4309, { 0x98, 0x48, 0xC4, 0x53, 0xDC, 0xD7, 0x11, 0x82 } };
	static const GUID kguidSemDomPlant =		 	// 025DA6F4-B1B6-423A-8C0F-B324F531A6F1
		{ 0x025DA6F4, 0xB1B6, 0x423A, { 0x8C, 0x0F, 0xB3, 0x24, 0xF5, 0x31, 0xA6, 0xF1 } };
	static const GUID kguidSemDomAnimal =		 	// 944CF5AF-469E-4B03-878F-A05D34B0D9F6
		{ 0x944CF5AF, 0x469E, 0x4B03, { 0x87, 0x8F, 0xA0, 0x5D, 0x34, 0xB0, 0xD9, 0xF6 } };
	static const GUID kguidSemDomNature =		 	// AA57936D-F8A9-4603-8C3D-27ABCCD13531
		{ 0xAA57936D, 0xF8A9, 0x4603, { 0x8C, 0x3D, 0x27, 0xAB, 0xCC, 0xD1, 0x35, 0x31 } };
	static const GUID kguidSemDomWilderness =	 	// 2BD48BD1-D8D5-4A5E-A883-D63D8E8652AA
		{ 0x2BD48BD1, 0xD8D5, 0x4A5E, { 0xA8, 0x83, 0xD6, 0x3D, 0x8E, 0x86, 0x52, 0xAA } };
	static const GUID kguidSemDomPerson =		 	// BA06DE9E-63E1-43E6-AE94-77BEA498379A
		{ 0xBA06DE9E, 0x63E1, 0x43E6, { 0xAE, 0x94, 0x77, 0xBE, 0xA4, 0x98, 0x37, 0x9A } };
	static const GUID kguidSemDomLanguageThought =	// F4491F9B-3C5E-42AB-AFC0-F22E19D0FFF5
		{ 0xF4491F9B, 0x3C5E, 0x42AB, { 0xAF, 0xC0, 0xF2, 0x2E, 0x19, 0xD0, 0xFF, 0xF5 } };
	// Semantic Domains added by RevisedSemDomList.xml.
	static const GUID kguidSemDomDeadThings =		// 06A89652-70E0-40AC-B929-ED42F011C9FC
		{ 0x06A89652, 0x70E0, 0x40AC, { 0xB9, 0x29, 0xED, 0x42, 0xF0, 0x11, 0xC9, 0xFC } };
	static const GUID kguidSemDomSpiritsOfThings =	// 1C512719-6ECB-48CB-980E-4FF20E8B5F9B
		{ 0x1C512719, 0x6ECB, 0x48CB, { 0x98, 0x0E, 0x4F, 0xF2, 0x0E, 0x8B, 0x5F, 0x9B } };
	static const GUID kguidSemDomBody =				// 1B0270A5-BABF-4151-99F5-279BA5A4B044
		{ 0x1B0270A5, 0xBABF, 0x4151, { 0x99, 0xF5, 0x27, 0x9B, 0xA5, 0xA4, 0xB0, 0x44 } };
	static const GUID kguidSemDomBodyFunctions =	// 7FE69C4C-2603-4949-AFCA-F39C010AD24E
		{ 0x7FE69C4C, 0x2603, 0x4949, { 0xAF, 0xCA, 0xF3, 0x9C, 0x01, 0x0A, 0xD2, 0x4E } };
	static const GUID kguidSemDomSensePerceive =	// 38BBB33A-90BF-4A2C-A0E5-4BDE7E134BD9
		{ 0x38BBB33A, 0x90BF, 0x4A2C, { 0xA0, 0xE5, 0x4B, 0xDE, 0x7E, 0x13, 0x4B, 0xD9 } };
	static const GUID kguidSemDomBodyCondition =	// F7706644-542F-4FCB-B8E1-E91D04C8032A
		{ 0xF7706644, 0x542F, 0x4FCB, { 0xB8, 0xE1, 0xE9, 0x1D, 0x04, 0xC8, 0x03, 0x2A } };
	static const GUID kguidSemDomHealthy =			// 32BEBE7E-BDCC-4E40-8F0A-894CD6B26F25
		{ 0x32BEBE7E, 0xBDCC, 0x4E40, { 0x8F, 0x0A, 0x89, 0x4C, 0xD6, 0xB2, 0x6F, 0x25 } };
	static const GUID kguidSemDomLife =				// 50DB27B5-89EB-4FFB-AF82-566F51C8EC0B
		{ 0x50DB27B5, 0x89EB, 0x4FFB, { 0xAF, 0x82, 0x56, 0x6F, 0x51, 0xC8, 0xEC, 0x0B } };
	static const GUID kguidThinking =				// 98F8B64D-B7BC-4339-A83F-1237365AFC61
		{ 0x98F8B64D, 0xB7BC, 0x4339, { 0xA8, 0x3F, 0x12, 0x37, 0x36, 0x5A, 0xFC, 0x61 } };

	class TestFwXmlData : public unitpp::suite
	{
		IFwXmlDataPtr m_qfwxd;
		StrUni m_stuServer;
		StrUni m_stuDbName;
		StrApp m_strMdfFile;
		StrApp m_strLdfFile;
		StrApp m_strRootDir;		// eg, "C:\\FW\\DistFiles"
		StrApp m_strSrcDir;			// eg, "C:\\FW\\Src\\Cellar\\Test"
		StrApp m_strXmlInit;
		StrApp m_strXmlImport;
		StrApp m_strXmlImportMulti;
		StrApp m_strXmlImport2;
		StrApp m_strXmlImport3;
		StrApp m_strXmlImport4;
		StrApp m_strXmlUpdateList;

		/*--------------------------------------------------------------------------------------
			This struct is used for validating links loaded into the database.  It serves for
			both reference and ownership links (From == owner, To == owned).
		--------------------------------------------------------------------------------------*/
		struct LinkInfo
		{
			GUID m_guidFrom;
			GUID m_guidTo;
			bool m_fFound;

			bool IsMatch(const GUID & guidFrom, const GUID & guidTo)
			{
				if (guidFrom == m_guidFrom && guidTo == m_guidTo)
				{
					m_fFound = true;
					return true;
				}
				else
				{
					return false;
				}
			}
		};

		/*--------------------------------------------------------------------------------------
			Test calling methods with null arguments.
		--------------------------------------------------------------------------------------*/
		void Dont_testNullArgs()
		{
			unitpp::assert_true("Non-null m_qfwxd after setup", m_qfwxd.Ptr() != 0);
			HRESULT hr;
#ifndef _DEBUG
			hr = m_qfwxd->QueryInterface(IID_NULL, NULL);
			unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, hr);
#endif
			hr = m_qfwxd->Open(NULL, NULL);
			unitpp::assert_eq("Open(NULL, NULL) HRESULT", E_INVALIDARG, hr);
			hr = m_qfwxd->LoadXml(NULL, NULL);
			unitpp::assert_eq("LoadXml(NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qfwxd->SaveXml(NULL, NULL, NULL);
			unitpp::assert_eq("SaveXml(NULL, NULL, NULL) HRESULT", E_INVALIDARG, hr);

			IFwXmlData2Ptr qfwxd2;
			hr = m_qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2);
			unitpp::assert_eq("QueryInterface(IID_IFwXmlData2, &qfwxd2) HRESULT", S_OK, hr);
			hr = qfwxd2->ImportXmlObject(NULL, 0, 0, NULL);
			unitpp::assert_eq("ImportXmlObject(NULL, 0, 0, NULL) HRESULT", E_POINTER, hr);
		}

		/*--------------------------------------------------------------------------------------
			Test the LoadXml method.
		--------------------------------------------------------------------------------------*/
		void testLoadXml()
		{
			HRESULT hr;
			unitpp::assert_true("Non-null m_qfwxd after setup", m_qfwxd.Ptr() != 0);
			unitpp::assert_true("blank language project database created",
				m_strLdfFile.Length());

			// Attach the FxXmlData object to the database created in SuiteSetup().
			hr = m_qfwxd->Open(m_stuServer.Bstr(), m_stuDbName.Bstr());
			unitpp::assert_eq("m_qfwxd->Open() HRESULT", S_OK, hr);

			// STDMETHODIMP FwXmlData::LoadXml(BSTR bstrFile, IAdvInd * padvi)
			StrUni stuFile(m_strXmlInit);
			hr = m_qfwxd->LoadXml(stuFile.Bstr(), NULL);
			unitpp::assert_eq("m_qfwxd->LoadXml() HRESULT", S_OK, hr);

			// Detach the FxXmlData object from the database created in SuiteSetup().
			hr = m_qfwxd->Close();
			unitpp::assert_eq("m_qfwxd->Close() HRESULT", S_OK, hr);

			StrUni stuUnicodeNFD =
				L"abcA" COMBINING_DIAERESIS COMBINING_MACRON L"A" COMBINING_DIAERESIS
				COMBINING_MACRON L"C" COMBINING_LEFT_HALF_RING_BELOW COMBINING_OVERLINE
				L"XYZ" BREVE L"GAP " COMBINING_BREVE L"QED"
				MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM;

			StrUni stuQuery;
			IOleDbEncapPtr qode; // Declare before qodc.
			IOleDbCommandPtr qodc;
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(m_stuServer.Bstr(), m_stuDbName.Bstr(), NULL,
				koltReturnError, 1000);
			unitpp::assert_eq("testLoadXml qode->Init hr", S_OK, hr);
			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("testLoadXml qode->CreateCommand hr", S_OK, hr);

			int ws = GetWs(qodc, L"en");

			// Check the simple TsString value.
			stuQuery.Assign(L"select Title,Title_Fmt From RnGenericRec"
				L" where Id = (select Id from CmObject where"
				L" Guid$ = '7FC3C53F-4834-4B64-9C1B-A97C7428B672');");
			checkTsString(qodc, stuQuery.Bstr(), "NFD String is NFD", "NFD String value",
				stuUnicodeNFD);

			// Check the MultiLingual String value.
			stuQuery.Format(L"select Txt,Fmt from MultiStr$"
				L" where Flid = %d AND Ws = %d AND"
				L" Obj in (select Id from CmObject where"
				L" Guid$ = '572F9C44-CB40-4644-8627-03B1AB7AF6AA')",
				kflidLgWritingSystem_Description, ws);
			checkTsString(qodc, stuQuery.Bstr(), "NFD MultiString is NFD",
				"NFD MultiString value", stuUnicodeNFD);

			// Check the "Big" String value.
			stuQuery.Assign(L"select Contents, Contents_Fmt from StTxtPara"
				L" where id = (select id from CmObject where"
				L" Guid$ = 'A5C8FE52-E896-4584-B34C-E3F62FE814DC');");
			checkTsString(qodc, stuQuery.Bstr(), "NFD BigString is NFD", "NFD BigString value",
				stuUnicodeNFD);

			// Check the MultiLingual "Big" String value.
			stuQuery.Format(L"select Txt,Fmt from MultiBigStr$"
				L" where Flid = %d AND Ws = %d AND"
				L" Obj = (select Id from CmObject where"
				L" Guid$ = '14D5143D-A59E-40F1-95AA-4A68FEA09A9C')",
				kflidCmPossibility_Description, ws);
			checkTsString(qodc, stuQuery.Bstr(), "NFD MultiBigString is NFD",
				"NFD MultiBigString value", stuUnicodeNFD);

			// Check the simple Unicode value.
			stuQuery.Assign(L"select Name from StStyle_"
				L" where Guid$ = '040D18A3-8024-4483-AC4A-0FBBA1EEAFFF'");
			checkUnicode(qodc, stuQuery.Bstr(), "simple NFD Unicode value", stuUnicodeNFD);

			// Check the MultiLingual Unicode value.
			stuQuery.Format(L"SELECT Txt"
				L" FROM CmPossibility_Name mt"
				L" JOIN CmObject o ON o.[Id] = mt.Obj AND"
					L" o.Guid$ = '14D5143D-A59E-40F1-95AA-4A68FEA09A9C'"
				L" WHERE mt.Ws = %d;", ws);

			checkUnicode(qodc, stuQuery.Bstr(), "NFD MultiUnicode value", stuUnicodeNFD);

			// Check the "Big" Unicode value.
			stuQuery.Assign(L"select IcuResourceText from LgCollation where"
				L" Id = (select Id from CmObject where"
				L" Guid$ = '897FA32D-8299-489F-966E-13661F3AF8F6')");
			checkUnicode(qodc, stuQuery.Bstr(), "big NFD Unicode value", stuUnicodeNFD);

			// Check the MultiLingual "Big" Unicode value -- oh, there aren't any...

			// Check the custom string field value.
			stuQuery.Assign(L"select r.custom,r.custom_Fmt"
				L" from RnGenericRec r"
				L" join CmObject co on co.Id = r.Id"
				L" where co.Guid$ = '7FC3C53F-4834-4B64-9C1B-A97C7428B672'");
			StrUni stuT(L"This is a test!");
			checkTsString(qodc, stuQuery.Bstr(), "Custom string value",
				"NFD Custom String value", stuT);

			// Check the Custom reference sequence values.
			stuQuery.Assign(L"select co.Guid$"
				L" from RnGenericRec_custom5 cust"
				L" join CmObject co on co.Id = cust.Dst"
				L" order by cust.Ord");
			static const GUID rgGuids[] = {
				// 6C7BC4D8-48DF-4E65-AAB3-2C2A63C3CAE3
				{ 0x6C7BC4D8, 0x48DF, 0x4E65, { 0xAA,0xB3, 0x2C,0x2A,0x63,0xC3,0xCA,0xE3 } },
				// 21494972-A3C3-4488-B496-FF4D5D9D3340
				{ 0x21494972, 0xA3C3, 0x4488, { 0xB4,0x96, 0xFF,0x4D,0x5D,0x9D,0x33,0x40 } }
				};
			checkObjectSeq(qodc, stuQuery.Bstr(), "Custom Reference sequence", rgGuids, 2);
		}

		/*--------------------------------------------------------------------------------------
			Test the ImportXmlObject() method.  This will fail unless testLoadXml() is run
			first.
		--------------------------------------------------------------------------------------*/
		void testImportXmlObject()
		{
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			int hvoList;
			int hvoListOwner;
			StrUni stuQuery;
			IOleDbEncapPtr qode; // Declare before qodc.
			IOleDbCommandPtr qodc;
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(m_stuServer.Bstr(), m_stuDbName.Bstr(), NULL,
				koltReturnError, 1000);
			unitpp::assert_eq("testImportXmlObject qode->Init hr", S_OK, hr);

			// Get the list owner for the AnthroList (ie, LangProject)
			hvoList = GetIdAndOwner(qode, kflidLangProject_AnthroList,
				"AnthroList (ImportXmlObject)", &hvoListOwner);

			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM CmAnthroItem", "before import", 0);

			IFwXmlData2Ptr qfwxd2;
			hr = m_qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2);
			unitpp::assert_eq("QueryInterface(IID_IFwXmlData2, &qfwxd2) HRESULT [2]", S_OK, hr);
			hr = qfwxd2->Open(m_stuServer.Bstr(), m_stuDbName.Bstr());
			unitpp::assert_eq("qfwxd2->Open() HRESULT", S_OK, hr);

			// *********************************************************************************
			StrUni stuXml(m_strXmlImport);
			hr = qfwxd2->ImportXmlObject(stuXml.Bstr(), hvoListOwner,
				kflidLangProject_AnthroList, NULL);
			unitpp::assert_eq("ImportXmlObject() HRESULT", S_OK, hr);
			hr = qfwxd2->Close();
			unitpp::assert_eq("qfwxd2->Close() HRESULT", S_OK, hr);
			// *********************************************************************************

			// Now to check whether the CmAnthroItem objects are really there...
			// NOTE THE MAGIC NUMBER 34 IN THE FOLLOWING LINE.  CHANGE AnthroList.xml, AND THIS
			// NUMBER PROBABLY CHANGES!
			const int kchobj = 34;
			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM CmAnthroItem", "after import", kchobj);

			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("testImportXmlObject qode->CreateCommand hr [2]", S_OK, hr);

			stuQuery.Format(L"SELECT [Id], [Owner$] FROM CmObject WHERE Class$ = %d",
				kclidCmAnthroItem);
			Vector<int> vhobj;
			Vector<int> vhobjOwner;
			Set<int> sethobjOwner;
			sethobjOwner.Insert(hvoList);
			int hobj;
			int hobjOwner;
			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("testImportXmlObject qode->CreateCommand hr [3]", S_OK, hr);
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("Select [Id], [Owner$] for CmAnthroItems hr", S_OK, hr);
			hr = qodc->GetRowset(0);
			unitpp::assert_eq("Select [Id], [Owner$] for CmAnthroItems GetRowset hr", S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			unitpp::assert_eq("Select [Id], [Owner$] for CmAnthroItems NextRow hr", S_OK, hr);
			StrAnsi sta;
			while (fMoreRows)
			{
				hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hobj), isizeof(hobj),
					&cbSpaceTaken, &fIsNull, 0);
				sta.Format("Select [Id], [Owner$] for CmAnthroItems GetColValue(1)[%d] hr",
					vhobj.Size());
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
				hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hobjOwner),
					isizeof(hobjOwner), &cbSpaceTaken, &fIsNull, 0);
				sta.Format("Select [Id], [Owner$] for CmAnthroItems GetColValue(2)[%d] hr",
					vhobj.Size());
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
				hr = qodc->NextRow(&fMoreRows);
				sta.Format("Select [Id], [Owner$] for CmAnthroItems NextRow[%d] hr",
					vhobj.Size());
				unitpp::assert_eq(sta.Chars(), S_OK, hr);

				vhobj.Push(hobj);
				vhobjOwner.Push(hobjOwner);
				sethobjOwner.Insert(hobjOwner);
			}
			unitpp::assert_eq("CmAnthroItem count", kchobj, vhobj.Size());

			stuQuery.Format(L"SELECT Depth, DisplayOption, ItemClsid, WsSelector"
				L" FROM CmPossibilityList WHERE [Id] = %d", hvoList);
			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("testImportXmlObject qode->CreateCommand hr [4]", S_OK, hr);
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("Select Depth, DisplayOption, ItemClsid, WsSelector hr",
				S_OK, hr);
			hr = qodc->GetRowset(0);
			unitpp::assert_eq("Depth, DisplayOption, ItemClsid, WsSelector - GetRowset hr",
				S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			unitpp::assert_eq("Depth, DisplayOption, ItemClsid, WsSelector - NextRow hr",
				S_OK, hr);
			unitpp::assert_true("Depth, DisplayOption, ItemClsid, WsSelector - fMoreRows",
				(bool)fMoreRows);
			int nT = 0;
			hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nT), isizeof(nT),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("GetColValue for Depth hr", S_OK, hr);
			unitpp::assert_eq("Anthro List Depth", 127, nT);
			hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&nT), isizeof(nT),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("GetColValue for DisplayOption hr", S_OK, hr);
			unitpp::assert_eq("Anthro List DisplayOption", 1, nT);
			hr = qodc->GetColValue(3, reinterpret_cast<BYTE *>(&nT), isizeof(nT),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("GetColValue for ItemClsid hr", S_OK, hr);
			unitpp::assert_eq("Anthro List ItemClsid", 26, nT);
			hr = qodc->GetColValue(4, reinterpret_cast<BYTE *>(&nT), isizeof(nT),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("GetColValue for WsSelector hr", S_OK, hr);
			unitpp::assert_eq("Anthro List WsSelector", -3, nT);

			int ihobj;
			for (ihobj = 0; ihobj < kchobj; ++ihobj)
			{
				sta.Format("CmAnthroItem ownership [%d]", ihobj);
				unitpp::assert_true(sta.Chars(), sethobjOwner.IsMember(vhobjOwner[ihobj]));
			}

			stuQuery.Assign(L"SELECT Txt"
				L" FROM CmPossibility_Name mt"
				L" JOIN CmObject o ON o.[Id] = mt.Obj AND"
					L" Guid$ = 'DA2D0D29-97AE-49CD-A752-9F748D64DF63'"
				L" JOIN LgWritingSystem ws ON ws.[Id] = mt.ws AND ws.IcuLocale = 'en'");
			StrUni stuName(L"Living objects or beings");
			checkUnicode(qodc, stuQuery.Bstr(), "first CmAnthroItem (en) name", stuName);

			stuQuery.Assign(L"SELECT Txt"
				L" FROM CmPossibility_Name mt"
				L" JOIN CmObject o ON o.[Id] = mt.Obj AND"
					L" Guid$ = 'DB0818BC-DF04-412A-A941-60A887C7E6DB'"
				L" JOIN LgWritingSystem ws ON ws.[Id] = mt.ws AND ws.IcuLocale = 'utp'");
			stuName.Assign(L"Plantes qui ne sont pas d'arbres");
			checkUnicode(qodc, stuQuery.Bstr(), "another CmAnthroItem (utp) name", stuName);

			stuQuery.Assign(L"SELECT Txt"
				L" FROM CmPossibility_Abbreviation mt"
				L" JOIN CmObject o ON o.[Id] = mt.Obj AND"
					L" Guid$ = 'DB0818BC-DF04-412A-A941-60A887C7E6DB'"
				L" JOIN LgWritingSystem ws ON ws.[Id] = mt.ws AND ws.IcuLocale = 'en'");
			stuName.Assign(L"96");
			checkUnicode(qodc, stuQuery.Bstr(), "same CmAnthroItem (en) abbr", stuName);

			/* The magic values below are from AnthroList.xml, of course. */
			stuQuery.Assign(L"select DateCreated, DateModified, ForeColor, BackColor,"
				L" UnderColor, UnderStyle"
				L" from CmPossibility where [Id] = (select [Id] from CmObject where Guid$"
				L" = '9356F81E-ECFB-4864-BB25-5E10BD8346E0')");
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("select DateCreated, ... hr", S_OK, hr);
			hr = qodc->GetRowset(0);
			unitpp::assert_eq("select DateCreated, ... GetRowset hr", S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			unitpp::assert_eq("select DateCreated, ... NextRow hr", S_OK, hr);
			unitpp::assert_true("select DateCreated, ... fMoreRows", fMoreRows);
			DBTIMESTAMP dbts;
			hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&dbts), isizeof(dbts),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("select DateCreated, ... GetColValue 1 hr", S_OK, hr);
			unitpp::assert_eq("CmAnthroItem DateCreated year", 2001, dbts.year);
			unitpp::assert_eq("CmAnthroItem DateCreated month", 10, dbts.month);
			unitpp::assert_eq("CmAnthroItem DateCreated day", 6, dbts.day);
			unitpp::assert_eq("CmAnthroItem DateCreated hour", 22, dbts.hour);
			unitpp::assert_eq("CmAnthroItem DateCreated minute", 50, dbts.minute);
			unitpp::assert_eq("CmAnthroItem DateCreated second", 9, dbts.second);
			unitpp::assert_eq("CmAnthroItem DateCreated fraction",
				(ULONG)347000000, dbts.fraction);
			hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&dbts), isizeof(dbts),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("select ..., DateModified, ... GetColValue 2 hr", S_OK, hr);
			unitpp::assert_eq("CmAnthroItem DateModified year", 2001, dbts.year);
			unitpp::assert_eq("CmAnthroItem DateModified month", 10, dbts.month);
			unitpp::assert_eq("CmAnthroItem DateModified day", 6, dbts.day);
			unitpp::assert_eq("CmAnthroItem DateModified hour", 22, dbts.hour);
			unitpp::assert_eq("CmAnthroItem DateModified minute", 50, dbts.minute);
			unitpp::assert_eq("CmAnthroItem DateModified second", 9, dbts.second);
			unitpp::assert_eq("CmAnthroItem DateModified fraction",
				(ULONG)347000000, dbts.fraction);
			nT = 0;
			hr = qodc->GetColValue(3, reinterpret_cast<BYTE *>(&nT), isizeof(nT),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("select ..., ForeColor, ... GetColValue 3 hr", S_OK, hr);
			unitpp::assert_eq("CmAnthroItem ForeColor", 16777215, nT);
			hr = qodc->GetColValue(4, reinterpret_cast<BYTE *>(&nT), isizeof(nT),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("select ..., BackColor, ... GetColValue 4 hr", S_OK, hr);
			unitpp::assert_eq("CmAnthroItem BackColor", 65535, nT);
			hr = qodc->GetColValue(5, reinterpret_cast<BYTE *>(&nT), isizeof(nT),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("select ..., UnderColor, ... GetColValue 5 hr", S_OK, hr);
			unitpp::assert_eq("CmAnthroItem UnderColor", 65535, nT);
			nT = 0;		// 8-bit value returned, so clear upper bits.
			hr = qodc->GetColValue(6, reinterpret_cast<BYTE *>(&nT), isizeof(nT),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("select ..., UnderStyle, ... GetColValue 6 hr", S_OK, hr);
			unitpp::assert_eq("CmAnthroItem UnderStyle", 4, nT);
		}

		/*--------------------------------------------------------------------------------------
			Test the ImportMultipleXmlFields() method.  This will fail unless testLoadXml() is
			run first.
		--------------------------------------------------------------------------------------*/
		void testImportMultipleXmlFields()
		{
			HRESULT hr;
			int hvoLexDb;
			int hvoLangProj;
			StrUni stuQuery;
			IOleDbEncapPtr qode; // Declare before qodc.
			IOleDbCommandPtr qodc;
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(m_stuServer.Bstr(), m_stuDbName.Bstr(), NULL,
				koltReturnError, 1000);
			unitpp::assert_eq("testImportMultipleXmlFields qode->Init hr", S_OK, hr);

			// Get the database ids for the lexical database and its owner (language project)
			hvoLexDb = GetIdAndOwner(qode, kflidLangProject_LexDb,
				"LexDb (ImportMultipleXmlFields)", &hvoLangProj);

			static const wchar * rgszCountCmds[] =
				{
					L"SELECT COUNT(*) FROM LangProject_Texts",
					L"SELECT COUNT(*) FROM LexDb_Entries",
					L"SELECT COUNT(*) FROM LexSense_ReversalEntries",
					L"SELECT COUNT(*) FROM LexDb_ReversalIndexes",
					L"SELECT COUNT(*) FROM LangProject_Annotations",
					L"SELECT COUNT(*) FROM ReversalIndex_PartsOfSpeech rp "
						L"JOIN CmPossibilityList cpl on cpl.Id=rp.Dst "
						L"JOIN CmPossibilityList_Possibilities cpp on cpp.Src=cpl.Id "
						L"JOIN PartOfSpeech ps on ps.Id = cpp.Dst "
						L"JOIN CmPossibility_Name cpn on cpn.Obj=ps.Id "
						L"JOIN CmPossibility_Abbreviation cpa on cpa.Obj=ps.Id",
					L"SELECT COUNT(*) FROM LgWritingSystem",
				};

			VerifySelectedInt(qode, rgszCountCmds[0], "before", 0);
			VerifySelectedInt(qode, rgszCountCmds[1], "before", 0);
			VerifySelectedInt(qode, rgszCountCmds[2], "before", 0);
			VerifySelectedInt(qode, rgszCountCmds[3], "before", 1);
			VerifySelectedInt(qode, rgszCountCmds[4], "before", 0);
			VerifySelectedInt(qode, rgszCountCmds[5], "before", 0);
			VerifySelectedInt(qode, rgszCountCmds[6], "before", 2);

			StrUni stuFolderName;
			StrUni stuInternalPath;
			LoadFolderAndFilePaths(qode, stuFolderName, stuInternalPath);
			unitpp::assert_eq("no folder before ImportMultipleXmlFields()",
				0, stuFolderName.Length());
			unitpp::assert_eq("no internal path before ImportMultipleXmlFields()",
				0, stuInternalPath.Length());

			IFwXmlData2Ptr qfwxd2;
			hr = m_qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2);
			unitpp::assert_eq("QueryInterface(IID_IFwXmlData2, &qfwxd2) HRESULT [3]", S_OK, hr);
			hr = qfwxd2->Open(m_stuServer.Bstr(), m_stuDbName.Bstr());
			unitpp::assert_eq("qfwxd2->Open() HRESULT [2]", S_OK, hr);

			// *********************************************************************************
			FixInternalPaths(m_strXmlImportMulti);
			StrUni stuXml(m_strXmlImportMulti);
			hr = qfwxd2->ImportMultipleXmlFields(stuXml.Bstr(), hvoLangProj, NULL);
			unitpp::assert_eq("ImportMultipleXmlFields() HRESULT", S_OK, hr);
			hr = qfwxd2->Close();
			unitpp::assert_eq("qfwxd2->Close() HRESULT [2]", S_OK, hr);
			// *********************************************************************************

			LoadFolderAndFilePaths(qode, stuFolderName, stuInternalPath);
			unitpp::assert_eq("folder name after ImportMultipleXmlFields()",
				stuFolderName, L"Local Pictures");
			unitpp::assert_true("InternalPath exists after ImportMultipleXmlFields",
				stuInternalPath.Length() > 0);

			VerifySelectedInt(qode, rgszCountCmds[0], "after", 2);
			VerifySelectedInt(qode, rgszCountCmds[1], "after", 12);
			VerifySelectedInt(qode, rgszCountCmds[2], "after", 6);
			VerifySelectedInt(qode, rgszCountCmds[3], "after", 1);	// shouldn't change!
			VerifySelectedInt(qode, rgszCountCmds[4], "after", 41);
			VerifySelectedInt(qode, rgszCountCmds[5], "after", 2);
			VerifySelectedInt(qode, rgszCountCmds[6], "after", 3);

			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("testImportMultipleXmlFields qode->CreateCommand hr [2]",
				S_OK, hr);
			stuQuery.Assign(L"SELECT n.Txt "
				L"FROM ReversalIndex ri "
				L"JOIN LgWritingSystem ws on ws.Id = ri.WritingSystem AND ws.ICULocale = 'en' "
				L"JOIN ReversalIndex_Entries ri2 on ri2.Src = ri.Id "
				L"JOIN ReversalIndexEntry rie on rie.Id = ri2.Dst "
				L"JOIN ReversalIndexEntry_ReversalForm rf on rf.Obj = rie.Id AND rf.Txt = N'enjoy' "
				L"JOIN CmPossibility_Name n on n.Obj = rie.PartOfSpeech AND n.Ws = ws.Id");
			StrUni stuVerb(L"Verb");
			checkUnicode(qodc, stuQuery.Bstr(), "PartOfSpeech Name for ReversalEntry 'enjoy'",
				stuVerb);
			qodc.Clear();
			qode.Clear();
		}


		/*--------------------------------------------------------------------------------------
			Test the ImportXmlObject() method a second time.  This will fail unless
			testLoadXml() is run first.
		--------------------------------------------------------------------------------------*/
		void testImportXmlObject2()
		{
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			int hvoList;
			int hvoListOwner;
			int hvoListOwner2;
			StrUni stuQuery;
			IOleDbEncapPtr qode; // Declare before qodc.
			IOleDbCommandPtr qodc;
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(m_stuServer.Bstr(), m_stuDbName.Bstr(), NULL,
				koltReturnError, 1000);
			unitpp::assert_eq("testImportXmlObject2 qode->Init hr", S_OK, hr);
			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("testImportXmlObject2 qode->CreateCommand hr", S_OK, hr);

			// Verify that the Semantic Domain list doesn't even exist yet.
			stuQuery.Format(L"SELECT [Id],Owner$ from CmObject where OwnFlid$ = %d",
				kflidLangProject_SemanticDomainList);
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("Select [Id],Owner$ for SemanticDomainList ExecCommand hr",
				S_OK, hr);
			hr = qodc->GetRowset(0);
			unitpp::assert_eq("Select [Id],Owner$ for SemanticDomainList GetRowset hr",
				S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			unitpp::assert_eq("Select [Id],Owner$ for SemanticDomainList NextRow hr", S_OK, hr);
			unitpp::assert_true("Select [Id],Owner$ for SemanticDomainList !fMoreRows",
				!fMoreRows);

			// no items exist either
			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM CmSemanticDomain", "before import", 0);

			stuQuery.Assign("SELECT TOP 1 [Id] FROM LangProject");
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("SELECT TOP 1 [Id] FROM LangProject ExecCommand hr",
				S_OK, hr);
			hr = qodc->GetRowset(0);
			unitpp::assert_eq("SELECT TOP 1 [Id] FROM LangProject GetRowset hr",
				S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			unitpp::assert_eq("SELECT TOP 1 [Id] FROM LangProject NextRow hr", S_OK, hr);
			unitpp::assert_true("SELECT TOP 1 [Id] FROM LangProject fMoreRows", fMoreRows);
			hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoListOwner),
				isizeof(hvoListOwner), &cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("SELECT TOP 1 [Id] FROM LangProject GetColValue 1 hr",
				S_OK, hr);
			unitpp::assert_true(
				"SELECT TOP 1 [Id] FROM LangProject GetColValue 1: not null", !fIsNull);
			unitpp::assert_true(
				"SELECT TOP 1 [Id] FROM LangProject GetColValue 1: not zero", hvoListOwner);
			qodc.Clear();

			IFwXmlData2Ptr qfwxd2;
			hr = m_qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2);
			unitpp::assert_eq("QueryInterface(IID_IFwXmlData2, &qfwxd2) HRESULT [4]", S_OK, hr);
			hr = qfwxd2->Open(m_stuServer.Bstr(), m_stuDbName.Bstr());
			unitpp::assert_eq("qfwxd2->Open() HRESULT [3]", S_OK, hr);

			// *********************************************************************************
			StrUni stuXml(m_strXmlImport2);
			hr = qfwxd2->ImportXmlObject(stuXml.Bstr(), hvoListOwner,
				kflidLangProject_SemanticDomainList, NULL);
			unitpp::assert_eq("ImportXmlObject() HRESULT [2]", S_OK, hr);
			hr = qfwxd2->Close();
			unitpp::assert_eq("qfwxd2->Close() HRESULT [3]", S_OK, hr);
			// *********************************************************************************

			// Verify that the list now exists.
			hvoList = GetIdAndOwner(qode, kflidLangProject_SemanticDomainList,
				"SemanticDomainList (ImportXmlObject2)", &hvoListOwner2);
			unitpp::assert_eq("Owner of SemanticDomainList is LangProject",
				hvoListOwner, hvoListOwner2);

			// Now to check whether the CmSemanticDomain objects are really there...
			// NOTE THE MAGIC NUMBER 24 IN THE FOLLOWING LINE.  CHANGE SemanticDomainList.xml,
			// AND THIS NUMBER PROBABLY CHANGES!
			const int kcSemDom = 24;
			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM CmSemanticDomain", "after import",
				kcSemDom);
			// And changing SemanticDomainList.xml will probably also change this array!
			LinkInfo rgliOwning[kcSemDom] = {
				{ GUID_NULL,					kguidSemDomPhysicaluniverse, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomSky, false },
				{ kguidSemDomSky,				kguidSemDomSkyObjects, false },
				{ kguidSemDomSkyObjects,		kguidSemDomSun, false },
				{ kguidSemDomSkyObjects,		kguidSemDomMoon, false },
				{ kguidSemDomSkyObjects,		kguidSemDomStar, false },
				{ kguidSemDomSkyObjects,		kguidSemDomPlanet, false },
				{ kguidSemDomSkyObjects,		kguidSemDomUFO, false },
				{ kguidSemDomSky,				kguidSemDomAir, false },
				{ kguidSemDomSky,				kguidSemDomWeather, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomWorld, false },
				{ kguidSemDomWorld,				kguidSemDomLand, false },
				{ kguidSemDomLand,				kguidSemDomMountain, false },
				{ kguidSemDomLand,				kguidSemDomVolcano, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomWater, false },
				{ kguidSemDomWater,				kguidSemDomBodiesOfWater, false },
				{ kguidSemDomWater,				kguidSemDomLiquids, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomLivingThings, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomPlant, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomAnimal, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomNature, false },
				{ kguidSemDomNature,			kguidSemDomWilderness, false },
				{ GUID_NULL,					kguidSemDomPerson, false },
				{ GUID_NULL,					kguidSemDomLanguageThought, false },
			};
			GUID guidSemDomList;
			GetGuidForHvo(qode, hvoList, &guidSemDomList, "ImportXmlObject2");
			for (int i = 0 ; i < kcSemDom; ++i)
			{
				if (rgliOwning[i].m_guidFrom == GUID_NULL)
					rgliOwning[i].m_guidFrom = guidSemDomList;
			}
			VerifySemanticDomainOwnerShip(qode, rgliOwning, kcSemDom, "ImportXmlObject2");

			stuQuery.Format(L"SELECT COUNT(*) FROM CmObject co "
				L"JOIN CmObject co2 on co2.Id = co.Owner$ "
				L"WHERE co.Class$=%<0>u AND co2.Guid$='%<1>g'",
				kclidCmDomainQ, &kguidSemDomSun);
			// CHANGING SemanticDomainList.xml WILL PROBABLY ALSO CHANGE THIS MAGIC NUMBER!
			VerifySelectedInt(qode, stuQuery.Chars(), "after import", 15);
		}

		/*--------------------------------------------------------------------------------------
			Test the ImportXmlObject() method a third time.  This will fail unless
			testLoadXml() and testImportMultipleXmlFields() is run first.
		--------------------------------------------------------------------------------------*/
		void testImportXmlObject3()
		{
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			int hvoLexDb;
			int hvoLangProj;
			StrUni stuQuery;
			IOleDbEncapPtr qode; // Declare before qodc.
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(m_stuServer.Bstr(), m_stuDbName.Bstr(), NULL,
				koltReturnError, 1000);
			unitpp::assert_eq("testImportXmlObject3 qode->Init hr", S_OK, hr);

			// Verify that the lexical database already exists.
			hvoLexDb = GetIdAndOwner(qode, kflidLangProject_LexDb,
				"LexDb (ImportXmlObject3)", &hvoLangProj);

			// Now check for the original number of LexEntry objects.
			// NOTE THE MAGIC NUMBER 12 IN THE FOLLOWING LINE.  CHANGE LLImportTest.xml, AND
			// THIS NUMBER PROBABLY CHANGES!
			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM LexEntry", "before ImportXmlObject3", 12);

			IFwXmlData2Ptr qfwxd2;
			hr = m_qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2);
			unitpp::assert_eq("QueryInterface(IID_IFwXmlData2, &qfwxd2) HRESULT [5]", S_OK, hr);
			hr = qfwxd2->Open(m_stuServer.Bstr(), m_stuDbName.Bstr());
			unitpp::assert_eq("qfwxd2->Open() HRESULT [4]", S_OK, hr);

			// *********************************************************************************
			StrUni stuXml(m_strXmlImport3);
			hr = qfwxd2->ImportXmlObject(stuXml.Bstr(), hvoLexDb,
				kflidLexDb_Entries, NULL);
			unitpp::assert_eq("ImportXmlObject() HRESULT [3]", S_OK, hr);
			hr = qfwxd2->Close();
			unitpp::assert_eq("qfwxd2->Close() HRESULT [4]", S_OK, hr);
			// *********************************************************************************

			// Now check whether the added LexEntry objects are really there...
			// NOTE THE MAGIC NUMBER 16 IN THE FOLLOWING LINE.  CHANGE EITHER LLImportTest.xml
			// OR MoreLexEntries.xml, AND THIS NUMBER PROBABLY CHANGES!
			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM LexEntry", "after ImportXmlObject3", 16);

			// Check the SemanticDomain links.
			// NOTE THE MAGIC NUMBER 7 IN THE FOLLOWING LINE.  CHANGE MoreLexEntries.xml, AND
			// THIS NUMBER PROBABLY CHANGES! (not to mention the array content)
			const int kclinks = 7;
			LinkInfo rgli[kclinks] = {
				{ kguidLexSenseCigaroid, kguidSemDomSkyObjects, false },
				{ kguidLexSenseCigaroid, kguidSemDomUFO, false },
				{ kguidLexSensePetroleum, kguidSemDomLiquids, false },
				{ kguidLexSenseForest, kguidSemDomNature, false },
				{ kguidLexSenseForest, kguidSemDomWilderness, false },
				{ kguidLexSenseEarth, kguidSemDomPlanet, false },
				{ kguidLexSenseEarth, kguidSemDomWorld, false },
			};
			VerifyLexSenseSemanticDomains(qode, rgli, kclinks, "ImportMultipleXmlFields");
		}

		/*--------------------------------------------------------------------------------------
			Test the UpdateListFromXml() method.  This will fail unless testImportXmlObject3()
			(and its antecedents) are run first.
		--------------------------------------------------------------------------------------*/
		void testUpdateListFromXml()
		{
			HRESULT hr;
			int hvoSemDomList;
			int hvoLangProj;
			IOleDbEncapPtr qode; // Declare before qodc.
			IOleDbCommandPtr qodc;
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(m_stuServer.Bstr(), m_stuDbName.Bstr(), NULL,
				koltReturnError, 1000);
			unitpp::assert_eq("testUpdateListFromXml qode->Init hr", S_OK, hr);
			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("testUpdateListFromXml qode->CreateCommand hr", S_OK, hr);
			int wsEn = GetWs(qodc, L"en");
			int wsFr = GetWs(qodc, L"fr");
			qodc.Clear();

			// Get the id and owner for the Semantic Domain List
			hvoSemDomList = GetIdAndOwner(qode, kflidLangProject_SemanticDomainList,
				"SemanticDomainList (UpdateListFromXml)", &hvoLangProj);

			// Now to check the original number of CmSemanticDomain objects.
			// NOTE THE MAGIC NUMBER 24 IN THE FOLLOWING LINE.  CHANGE SemanticDomainList.xml,
			// AND THIS NUMBER PROBABLY CHANGES!
			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM CmSemanticDomain", "before update", 24);

			// Check the original values of one particular abbreviation.
			StrUni stuQuery;
			stuQuery.Format(L"SELECT a.Txt "
				L"FROM CmPossibility_Abbreviation a "
				L"JOIN CmObject c on c.Id=a.Obj "
				L"WHERE c.Guid$='%<0>g' AND a.Ws=%<1>u", &kguidSemDomWorld, wsEn);
			VerifyMultiStringValue(qode, stuQuery.Bstr(), L"1.2",
				"English abbr for World before update = \"1.2\"");
			stuQuery.Format(L"SELECT a.Txt "
				L"FROM CmPossibility_Abbreviation a "
				L"JOIN CmObject c on c.Id=a.Obj "
				L"WHERE c.Guid$='%<0>g' AND a.Ws=%<1>u", &kguidSemDomWorld, wsFr);
			VerifyMultiStringValue(qode, stuQuery.Bstr(), NULL,
				"French abbr for World before update is null");

			IFwXmlData2Ptr qfwxd2;
			hr = m_qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2);
			unitpp::assert_eq("QueryInterface(IID_IFwXmlData2, &qfwxd2) HRESULT [6]", S_OK, hr);
			hr = qfwxd2->Open(m_stuServer.Bstr(), m_stuDbName.Bstr());
			unitpp::assert_eq("qfwxd2->Open() HRESULT [5]", S_OK, hr);

			// *********************************************************************************
			StrUni stuXml(m_strXmlUpdateList);
			hr = qfwxd2->UpdateListFromXml(stuXml.Bstr(), hvoLangProj,
				kflidLangProject_SemanticDomainList, NULL);
			unitpp::assert_eq("UpdateListFromXml() HRESULT", S_OK, hr);
			hr = qfwxd2->Close();
			unitpp::assert_eq("qfwxd2->Close() HRESULT [5]", S_OK, hr);
			// *********************************************************************************

			// Check the updated number of CmSemanticDomain objects.  (This includes three
			// "custom" CmSemanticDomains that were loaded from SemanticDomainList.xml.)
			// NOTE THE MAGIC NUMBER 30 IN THE FOLLOWING LINE.  CHANGE SemanticDomainList.xml OR
			// RevisedSemDomList.xml, AND THIS NUMBER PROBABLY CHANGES!
			const int kcSemDom = 31;
			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM CmSemanticDomain", "after update",
				kcSemDom);

			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM CmSemanticDomain", "after update",
				kcSemDom);
			// And changing SemanticDomainList.xml or RevSemDomList.xml will probably also
			// change this array!
			LinkInfo rgliOwning[kcSemDom] = {
				{ GUID_NULL,					kguidSemDomPhysicaluniverse, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomSky, false },
				{ kguidSemDomSky,				kguidSemDomSun, false },
				{ kguidSemDomSky,				kguidSemDomMoon, false },
				{ kguidSemDomSky,				kguidSemDomStar, false },
				{ kguidSemDomSky,				kguidSemDomPlanet, false },
				{ kguidSemDomSky,				kguidSemDomUFO, false },
				{ kguidSemDomSky,				kguidSemDomAir, false },
				{ kguidSemDomSky,				kguidSemDomWeather, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomWorld, false },
				{ kguidSemDomWorld,				kguidSemDomLand, false },
				{ kguidSemDomLand,				kguidSemDomMountain, false },
				{ kguidSemDomLand,				kguidSemDomVolcano, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomWater, false },
				{ kguidSemDomWater,				kguidSemDomBodiesOfWater, false },
				{ kguidSemDomWater,				kguidSemDomLiquids, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomLivingThings, false },
				{ kguidSemDomLivingThings,		kguidSemDomDeadThings, false },
				{ kguidSemDomLivingThings,		kguidSemDomSpiritsOfThings, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomPlant, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomAnimal, false },
				{ kguidSemDomPhysicaluniverse,	kguidSemDomWilderness, false },
				{ GUID_NULL,					kguidSemDomPerson, false },
				{ kguidSemDomPerson,			kguidSemDomBody, false },
				{ kguidSemDomPerson,			kguidSemDomBodyFunctions, false },
				{ kguidSemDomPerson,			kguidSemDomSensePerceive, false },
				{ kguidSemDomPerson,			kguidSemDomBodyCondition, false },
				{ kguidSemDomPerson,			kguidSemDomHealthy, false },
				{ kguidSemDomPerson,			kguidSemDomLife, false },
				{ GUID_NULL,					kguidThinking, false },
				{ kguidThinking,				kguidSemDomLanguageThought, false },

			};
			GUID guidSemDomList;
			GetGuidForHvo(qode, hvoSemDomList, &guidSemDomList, "ImportXmlObject2");
			for (int i = 0 ; i < kcSemDom; ++i)
			{
				if (rgliOwning[i].m_guidFrom == GUID_NULL)
					rgliOwning[i].m_guidFrom = guidSemDomList;
			}
			VerifySemanticDomainOwnerShip(qode, rgliOwning, kcSemDom, "ImportXmlObject2");

			// Check the SemanticDomain links.
			// NOTE THE MAGIC NUMBER 6 IN THE FOLLOWING LINE.  CHANGE MoreLexEntries.xml OR
			// RevisedSemDomList.xml, AND THIS NUMBER PROBABLY CHANGES! (not to mention the
			// array content)
			const int kclinks = 6;
			LinkInfo rgli[kclinks] = {
				{ kguidLexSenseCigaroid,  kguidSemDomSky,        false },
				{ kguidLexSenseCigaroid,  kguidSemDomUFO,        false },
				{ kguidLexSensePetroleum, kguidSemDomLiquids,    false },
				{ kguidLexSenseForest,    kguidSemDomWilderness, false },
				{ kguidLexSenseEarth,     kguidSemDomPlanet,     false },
				{ kguidLexSenseEarth,     kguidSemDomWorld,      false },
			};
			VerifyLexSenseSemanticDomains(qode, rgli, kclinks, "UpdateListFromXml");

			// Check that an existing item has additional owned objects after updating.
			stuQuery.Format(L"SELECT COUNT(*) FROM CmObject co "
				L"JOIN CmObject co2 on co2.Id = co.Owner$ "
				L"WHERE co.Class$=%<0>u AND co2.Guid$='%<1>g'",
				kclidCmDomainQ, &kguidSemDomSun);
			// CHANGING RevisedSemDomList.xml WILL PROBABLY ALSO CHANGE THIS MAGIC NUMBER!
			VerifySelectedInt(qode, stuQuery.Chars(), "after update", 17);

			// Check that objects owned by custom items are still there.
			stuQuery.Format(L"SELECT COUNT(*) FROM CmObject co "
				L"JOIN CmObject co2 on co2.Id = co.Owner$ "
				L"WHERE co2.Guid$='%<0>g'",
				&kguidSemDomLiquids);
			// CHANGING SemanticDomainList.xml WILL PROBABLY ALSO CHANGE THIS MAGIC NUMBER!
			VerifySelectedInt(qode, stuQuery.Chars(), "after update", 1);

			// Check that some things change and some stay the same.
			stuQuery.Format(L"SELECT a.Txt "
				L"FROM CmPossibility_Abbreviation a "
				L"JOIN CmObject c on c.Id=a.Obj "
				L"WHERE c.Guid$='%<0>g' AND a.Ws=%<1>u", &kguidSemDomWorld, wsEn);
			VerifyMultiStringValue(qode, stuQuery.Bstr(), L"1.2",
				"English abbr for World after update = \"1.2\"");
			stuQuery.Format(L"SELECT a.Txt "
				L"FROM CmPossibility_Abbreviation a "
				L"JOIN CmObject c on c.Id=a.Obj "
				L"WHERE c.Guid$='%<0>g' AND a.Ws=%<1>u", &kguidSemDomWorld, wsFr);
			VerifyMultiStringValue(qode, stuQuery.Bstr(), L"1.2",
				"French abbr for World after update = \"1.2\"");
		}

		/*--------------------------------------------------------------------------------------
			Test the ImportXmlObject() method a fourth time.  This will fail unless
			testLoadXml() is run first.
		--------------------------------------------------------------------------------------*/
		void testImportXmlObject4()
		{
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			int hvoLexDb;
			int hvoLangProj;
			StrUni stuQuery;
			IOleDbEncapPtr qode; // Declare before qodc.
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(m_stuServer.Bstr(), m_stuDbName.Bstr(), NULL,
				koltReturnError, 1000);
			unitpp::assert_eq("testImportXmlObject4 qode->Init hr", S_OK, hr);

			// Verify that the lexical database already exists.
			hvoLexDb = GetIdAndOwner(qode, kflidLangProject_LexDb,
				"LexDb (ImportXmlObject4)", &hvoLangProj);

			int hvoMin = ReadOneInt(qode, L"SELECT MAX(Id) FROM CmObject",
				"ImportXmlObject4 read min hvo");

			IFwXmlData2Ptr qfwxd2;
			hr = m_qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2);
			unitpp::assert_eq("QueryInterface(IID_IFwXmlData2, &qfwxd2) HRESULT [5]", S_OK, hr);
			hr = qfwxd2->Open(m_stuServer.Bstr(), m_stuDbName.Bstr());
			unitpp::assert_eq("qfwxd2->Open() HRESULT [4]", S_OK, hr);

			// *********************************************************************************
			StrUni stuXml(m_strXmlImport4);
			hr = qfwxd2->ImportXmlObject(stuXml.Bstr(), hvoLexDb,
				kflidLexDb_Entries, NULL);
			unitpp::assert_eq("ImportXmlObject() HRESULT [3]", S_OK, hr);
			hr = qfwxd2->Close();
			unitpp::assert_eq("qfwxd2->Close() HRESULT [4]", S_OK, hr);
			// *********************************************************************************

			// Check the number of entries created by this import operation.
			stuQuery.Format(L"SELECT COUNT(*) FROM LexEntry WHERE Id > %d", hvoMin);
			// NOTE THE MAGIC NUMBER 8 IN THE FOLLOWING LINE.  CHANGE SFImportTest.xml AND THIS
			// NUMBER PROBABLY CHANGES!
			VerifySelectedInt(qode, stuQuery.Chars(), "ImportXmlObject4 new entry count", 8);

			// Check the number of senses created by this import operation.
			stuQuery.Format(L"SELECT COUNT(*) FROM LexEntry_Senses WHERE Src > %d", hvoMin);
			// NOTE THE MAGIC NUMBER 2 IN THE FOLLOWING LINE.  CHANGE SFImportTest.xml AND THIS
			// NUMBER PROBABLY CHANGES!
			VerifySelectedInt(qode, stuQuery.Chars(), "ImportXmlObject4 new sense count", 2);

			// Check that we do not have any orphaned MSAs.
			VerifySelectedInt(qode, L"select count(*) from MoMorphSynAnalysis_ msa "
				L"left outer join LexSense sen on sen.MorphoSyntaxAnalysis = msa.id "
				L"join LexEntry le on le.id = msa.owner$ "
				L"where sen.MorphoSyntaxAnalysis is null and msa.OwnFlid$ = 5002009",
				"ImportXmlObject4 missing orphaned MSA count", 0);
			VerifySelectedInt(qode, L"SELECT COUNT(*) FROM LexSense"
				L" WHERE MorphoSyntaxAnalysis IS NULL",
				"ImportXmlObject4 missing sense MSA count", 0);

			// Check that the proper type of MSA is created for stem and affix type morphs.
			stuQuery.Format(L"SELECT COUNT(*) FROM LexEntry le%n"
				L"JOIN LexEntry_LexemeForm lelf on lelf.Src=le.Id%n"
				L"JOIN MoForm mf on mf.Id=lelf.Dst%n"
				L"JOIN MoMorphType_ mmt on mmt.Id=mf.MorphType%n"
				L"JOIN LexEntry_MorphoSyntaxAnalyses lemsa on lemsa.Src=le.Id%n"
				L"JOIN MoStemMsa msa on msa.Id=lemsa.Dst%n"
				L"WHERE le.Id > %d AND mmt.Guid$='D7F713E8-E8CF-11D3-9764-00C04F186933'",
				hvoMin);
			// NOTE THE MAGIC NUMBER 1 IN THE FOLLOWING LINE.  CHANGE SFImportTest.xml AND THIS
			// NUMBER PROBABLY CHANGES!
			VerifySelectedInt(qode, stuQuery.Chars(), "ImportXmlObject4 stem MSA count", 1);

			stuQuery.Format(L"SELECT COUNT(*) FROM LexEntry le%n"
				L"JOIN LexEntry_LexemeForm lelf on lelf.Src=le.Id%n"
				L"JOIN MoForm mf on mf.Id=lelf.Dst%n"
				L"JOIN MoMorphType_ mmt on mmt.Id=mf.MorphType%n"
				L"JOIN LexEntry_MorphoSyntaxAnalyses lemsa on lemsa.Src=le.Id%n"
				L"JOIN MoUnclassifiedAffixMsa msa on msa.Id=lemsa.Dst%n"
				L"WHERE le.Id > %d AND mmt.Guid$<>'D7F713E8-E8CF-11D3-9764-00C04F186933'",
				hvoMin);
			// NOTE THE MAGIC NUMBER 1 IN THE FOLLOWING LINE.  CHANGE SFImportTest.xml AND THIS
			// NUMBER PROBABLY CHANGES!
			VerifySelectedInt(qode, stuQuery.Chars(), "ImportXmlObject4 affix MSA count", 1);
		}

		/*--------------------------------------------------------------------------------------
			Return the Guid$ value for the given database id.
		--------------------------------------------------------------------------------------*/
		void GetGuidForHvo(IOleDbEncap * pode, int hvo, GUID * pguid, const char * pszTag)
		{
			ComBool fIsNull;
			ComBool fMoreRows;
			StrUni stuQuery;
			IOleDbCommandPtr qodc;
			stuQuery.Format(L"SELECT Guid$ FROM CmObject WHERE [Id]=%u", hvo);
			ULONG cbSpaceTaken;
			HRESULT hr = pode->CreateCommand(&qodc);
			StrAnsi sta;
			sta.Format("GetGuidForHvo(%u) pode->CreateCommand hr [%s]", hvo, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			sta.Format("GetGuidForHvo(%u) ExecCommand hr [%s]", hvo, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->GetRowset(0);
			sta.Format("GetGuidForHvo(%u) GetRowSet hr [%s]", hvo, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			sta.Format("GetGuidForHvo(%u) NextRow hr [%s]", hvo, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			sta.Format("GetGuidForHvo(%u) fMoreRows [%s]", hvo, pszTag);
			unitpp::assert_true(sta.Chars(), fMoreRows);
			hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(pguid), sizeof(GUID),
				&cbSpaceTaken, &fIsNull, 0);
			sta.Format("GetGuidForHvo(%u) GetColValue 1 hr [%s]", hvo, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			sta.Format("GetGuidForHvo(%u) GetColValue 1: not null [%s]", hvo, pszTag);
			unitpp::assert_true(sta.Chars(), !fIsNull);
			qodc.Clear();
		}

		/*--------------------------------------------------------------------------------------
			Return the writing system database id for the given ICU Locale.
		--------------------------------------------------------------------------------------*/
		int GetWs(IOleDbCommand * podc, const wchar * pszIcuLocale)
		{
			StrUni stuQuery;
			stuQuery.Format(L"SELECT [Id] FROM LgWritingSystem WHERE ICULocale=N'%s'",
				pszIcuLocale);
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			int ws;
			HRESULT hr;
			hr = podc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("GetWs ExecCommand hr", S_OK, hr);
			hr = podc->GetRowset(0);
			unitpp::assert_eq("GetWs GetRowset hr", S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			unitpp::assert_eq("GetWs NextRow hr", S_OK, hr);
			unitpp::assert_true("GetWs fMoreRows", fMoreRows);
			hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(&ws), isizeof(ws),
				&cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("GetWs GetColValue 1 hr", S_OK, hr);
			return ws;
		}

		/*--------------------------------------------------------------------------------------
			Check a TsString in the database for validity.
		--------------------------------------------------------------------------------------*/
		void checkTsString(IOleDbCommand * podc, BSTR bstrQuery, const char * pszTestMsg1,
			const char * pszTestMsg2, StrUni & stuUnicodeNFD)
		{
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			wchar rgchText[8200];
			BYTE rgbFormat[8200];
			StrUni stuT;
			ITsStringPtr qtss;

			HRESULT hr;
			hr = podc->ExecCommand(bstrQuery, knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("checkTsString ExecCommand hr", S_OK, hr);
			hr = podc->GetRowset(0);
			unitpp::assert_eq("checkTsString GetRowset hr", S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			unitpp::assert_eq("checkTsString NextRow hr", S_OK, hr);
			unitpp::assert_true("checkTsString fMoreRows", fMoreRows);
			hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(rgchText),
				isizeof(rgchText), &cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("checkTsString GetColValue 1 hr", S_OK, hr);
			int cchText = (int)cbSpaceTaken / isizeof(wchar);
			hr = podc->GetColValue(2, reinterpret_cast<BYTE *>(rgbFormat),
				isizeof(rgbFormat), &cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("checkTsString GetColValue 2 hr", S_OK, hr);
			int cbFormat = (int)cbSpaceTaken;
			stuT.Assign(rgchText, cchText);
			// Create a TsString.
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			hr = qtsf->DeserializeStringRgch(rgchText, &cchText, rgbFormat, &cbFormat,
				&qtss);
			unitpp::assert_eq("checkTsString DeserializeStringRgch hr", S_OK, hr);
			ComBool fT;
			hr = qtss->get_IsNormalizedForm(knmNFD, &fT);
			unitpp::assert_eq("checkTsString get_IsNormalizedForm hr", S_OK, hr);
			unitpp::assert_true(pszTestMsg1,  bool(fT));
			unitpp::assert_true(pszTestMsg2, stuUnicodeNFD == stuT);
		}

		/*--------------------------------------------------------------------------------------
			Check a Unicode value in the database for validity.
		--------------------------------------------------------------------------------------*/
		void checkUnicode(IOleDbCommand * podc, BSTR bstrQuery, const char * pszTestMsg,
			StrUni & stuUnicodeNFD)
		{
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			wchar rgchText[8200];
			StrUni stuT;

			HRESULT hr;
			hr = podc->ExecCommand(bstrQuery, knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("checkUnicode ExecCommand hr", S_OK, hr);
			hr = podc->GetRowset(0);
			unitpp::assert_eq("checkUnicode GetRowset hr", S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			unitpp::assert_eq("checkUnicode NextRow hr", S_OK, hr);
			unitpp::assert_true("checkUnicode fMoreRows", fMoreRows);
			hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(rgchText),
				isizeof(rgchText), &cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("checkUnicode GetColValue 1 hr", S_OK, hr);
			int cchText = (int)cbSpaceTaken / isizeof(wchar);
			stuT.Assign(rgchText, cchText);
			unitpp::assert_true(pszTestMsg, stuUnicodeNFD == stuT);
		}

		/*--------------------------------------------------------------------------------------
			Check an ordered seqence of object references in the database for validity.
		--------------------------------------------------------------------------------------*/
		void checkObjectSeq(IOleDbCommand * podc, BSTR bstrQuery, const char * pszTestMsg,
			const GUID rgGuids[], int cGuids)
		{
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			GUID guid;

			HRESULT hr;
			hr = podc->ExecCommand(bstrQuery, knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("checkObjectSeq ExecCommand hr", S_OK, hr);
			hr = podc->GetRowset(0);
			unitpp::assert_eq("checkObjectSeq GetRowset hr", S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			unitpp::assert_eq("checkObjectSeq NextRow hr", S_OK, hr);
			unitpp::assert_true("checkObjectSeq fMoreRows", fMoreRows);
			hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(&guid),
				isizeof(guid), &cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("checkObjectSeq GetColValue 1 hr", S_OK, hr);
		}

		/*--------------------------------------------------------------------------------------
			Check that the expected number is returned by the SQL query.
		--------------------------------------------------------------------------------------*/
		void VerifySelectedInt(IOleDbEncap * pode, const wchar * pszSql, const char * pszTag,
			const int cValWanted)
		{
			StrAnsi sta;
			int cVal = SelectIntValue(pode, pszSql, pszTag, "VerifySelectedInt");
			sta.Format("VerifySelectedInt(\"%S\") GetColValue 1: equals %d [%s]",
				pszSql, cValWanted, pszTag);
			unitpp::assert_eq(sta.Chars(), cValWanted, cVal);
		}


		/*--------------------------------------------------------------------------------------
			Execute some SQL that returns one integer value.
		--------------------------------------------------------------------------------------*/
		int SelectIntValue(IOleDbEncap * pode, const wchar * pszSql, const char * pszTag,
			const char * pszFunc)
		{
			IOleDbCommandPtr qodc;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			HRESULT hr = pode->CreateCommand(&qodc);
			StrAnsi sta;
			sta.Format("%s(\"%S\") pode->CreateCommand hr [%s]", pszFunc, pszSql, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			StrUni stuQuery(pszSql);
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			sta.Format("%s(\"%S\") ExecCommand hr [%s]", pszFunc, pszSql, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->GetRowset(0);
			sta.Format("%s(\"%S\") GetRowSet hr [%s]", pszFunc, pszSql, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			sta.Format("%s(\"%S\") NextRow hr [%s]", pszFunc, pszSql, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			sta.Format("%s(\"%S\") fMoreRows [%s]", pszFunc, pszSql, pszTag);
			unitpp::assert_true(sta.Chars(), fMoreRows);
			int cVal;
			hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cVal), isizeof(cVal),
				&cbSpaceTaken, &fIsNull, 0);
			sta.Format("%s(\"%S\") GetColValue 1 hr [%s]", pszFunc, pszSql, pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			sta.Format("%s(\"%S\") GetColValue 1: not null [%s]", pszFunc, pszSql, pszTag);
			unitpp::assert_true(sta.Chars(), !fIsNull);
			qodc.Clear();
			return cVal;
		}


		/*--------------------------------------------------------------------------------------
			Read one integer value from the database using the given SQL code.
		--------------------------------------------------------------------------------------*/
		int ReadOneInt(IOleDbEncap * pode, const wchar * pszSql, const char * pszTag)
		{
			return SelectIntValue(pode, pszSql, pszTag, "ReadOneInt");
		}

		/*--------------------------------------------------------------------------------------
			Check that the expected string is returned by the SQL query.
		--------------------------------------------------------------------------------------*/
		void VerifyMultiStringValue(IOleDbEncap * pode, BSTR bstrQuery,
			const wchar * pszValue, const char * pszAssertMsg)
		{
			IOleDbCommandPtr qodc;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			HRESULT hr = pode->CreateCommand(&qodc);
			StrAnsi sta;
			sta.Format("%s: pode->CreateCommand hr", pszAssertMsg);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->ExecCommand(bstrQuery, knSqlStmtSelectWithOneRowset);
			sta.Format("%s: ExecCommand hr", pszAssertMsg);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->GetRowset(0);
			sta.Format("%s: GetRowSet hr", pszAssertMsg);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			sta.Format("%s: NextRow hr", pszAssertMsg);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			if (pszValue == NULL)
			{
				unitpp::assert_true(pszAssertMsg, !fMoreRows);
			}
			else
			{
				sta.Format("%s: fMoreRows", pszAssertMsg);
				unitpp::assert_true(sta.Chars(), fMoreRows);
				wchar rgch[4000];
				hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgch), isizeof(rgch),
					&cbSpaceTaken, &fIsNull, 2);
				sta.Format("%s: GetColValue 1 hr", pszAssertMsg);
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
				sta.Format("%s: GetColValue 1: not null", pszAssertMsg);
				unitpp::assert_true(sta.Chars(), !fIsNull);
				StrUni stuWanted(pszValue);
				StrUni stuValue;
				if (cbSpaceTaken == 0)
					stuValue.Clear();
				else
					stuValue.Assign(rgch);
				unitpp::assert_eq(pszAssertMsg, stuWanted, stuValue);
			}
			qodc.Clear();
		}

		/*--------------------------------------------------------------------------------------
			Set the root directory for where things are located, including template files and
			temporary files.
		--------------------------------------------------------------------------------------*/
		void SetRootDir()
		{
			if (m_strRootDir.Length())
				return;		// Set this only once!

			// Get the path to the template files.
			HKEY hk;
			if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, _T("Software\\SIL\\FieldWorks"), 0,
					KEY_QUERY_VALUE, &hk) == ERROR_SUCCESS)
			{
				achar rgch[MAX_PATH];
				DWORD cb = isizeof(rgch);
				DWORD dwT;
				if (::RegQueryValueEx(hk, _T("RootCodeDir"), NULL, &dwT, (BYTE *)rgch, &cb)
					== ERROR_SUCCESS)
				{
					Assert(dwT == REG_SZ);
					m_strRootDir.Assign(rgch);
					// Trim any trailing \\ character.
					int ich = m_strRootDir.FindCh('\\', m_strRootDir.Length() - 1);
					if (ich >= 0)
						m_strRootDir.Replace(ich, m_strRootDir.Length(), _T(""));
				}
				RegCloseKey(hk);
			}
			if (!m_strRootDir.Length())
				m_strRootDir.Assign("C:\\FW\\DistFiles");
			int ich = m_strRootDir.FindStrCI(_T("\\distfiles"));
			Assert(ich >= 0);
			m_strSrcDir.Assign(m_strRootDir.Chars(), ich);
			m_strSrcDir.Append(_T("\\Src\\Cellar\\Test"));
		}

		/*--------------------------------------------------------------------------------------
			Set the MDF and LDF filenames for the given database.
		--------------------------------------------------------------------------------------*/
		void SetFilenames(const achar * pszProjName, StrApp & strMdfFile, StrApp & strLdfFile)
		{
			strMdfFile.Format(_T("%s\\Data\\%s.mdf"), m_strRootDir.Chars(), pszProjName);
			strLdfFile.Format(_T("%s\\Data\\%s_Log.ldf"), m_strRootDir.Chars(), pszProjName);
		}

		/*--------------------------------------------------------------------------------------
			Create an empty test database with the given name.
		--------------------------------------------------------------------------------------*/
		void CreateEmptyDatabase(const achar * pszProjName, const achar * pszMdfFile,
			const achar * pszLdfFile)
		{
			// Data directory = <RootDir>/Data
			StrApp strDataDir(m_strRootDir);
			strDataDir.Append(_T("\\Data"));
			DWORD dwChk = ::GetFileAttributes(strDataDir);
			if (!(dwChk & FILE_ATTRIBUTE_DIRECTORY) || dwChk == INVALID_FILE_ATTRIBUTES)
			{
				::CreateDirectory(strDataDir, NULL);
			}
			// Template directory = either <RootDir>\Templates or
			//                             <RootDir>\..\Output\Templates
			StrApp strTemplateDir;
			strTemplateDir.Format(_T("%s\\Templates"), m_strRootDir.Chars());
			StrApp strTemplate;
			strTemplate.Format(_T("%s\\BlankLangProj.mdf"), strTemplateDir.Chars());
			StrApp str;
			BOOL fOk = ::CopyFile(strTemplate.Chars(), pszMdfFile, TRUE);
			if (!fOk)
			{
				DWORD dwError = ::GetLastError();
				if (dwError == ERROR_FILE_NOT_FOUND)
				{
					strTemplateDir.Assign(m_strRootDir.Chars(), m_strRootDir.Length());
					int ich = strTemplateDir.ReverseFindCh('\\');
					Assert(ich >= 0);
					strTemplateDir.Replace(ich, strTemplateDir.Length(),
						_T("\\Output\\Templates"));
					strTemplate.Format(_T("%s\\BlankLangProj.mdf"), strTemplateDir.Chars());
					fOk = ::CopyFile(strTemplate.Chars(), pszMdfFile, TRUE);
				}
				if (!fOk)
				{
					dwError = ::GetLastError();
					throw dwError;
				}
			}
			// We've copied the .mdf file - now copy the .ldf file.
			strTemplate.Format(_T("%s\\BlankLangProj_Log.ldf"), strTemplateDir.Chars());
			fOk = ::CopyFile(strTemplate.Chars(), pszLdfFile, TRUE);
			if (!fOk)
			{
				DWORD dwError = ::GetLastError();
				throw dwError;
			}

			// We've copied the files, now attach the unit test database.
			IOleDbEncapPtr qode; // Declare before qodc.
			IOleDbCommandPtr qodc;
			StrUni stuDatabase(L"master");
			qode.CreateInstance(CLSID_OleDbEncap);
			HRESULT hr = qode->Init(m_stuServer.Bstr(), stuDatabase.Bstr(), NULL,
				koltReturnError, 1000);
			unitpp::assert_eq("CreateEmptyDatabase(): qode->Init(\"master\") hr", S_OK, hr);
			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("CreateEmptyDatabase(): qode->CreateCommand(&qodc) hr", S_OK, hr);
			StrUni stuProjName(pszProjName);
			StrUni stuMdfFile(pszMdfFile);
			StrUni stuLdfFile(pszLdfFile);
			StrUni stuQuery;
			stuQuery.Format(L"EXEC sp_attach_db '%s', '%s', '%s'",
				stuProjName.Chars(), stuMdfFile.Chars(), stuLdfFile.Chars());
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults);
			unitpp::assert_eq(
				"CreateEmptyDatabase(): qode->ExecCommand(\"EXEC sp_attach_db ...\") hr",
				S_OK, hr);
			qodc.Clear();
			qode.Clear();
		}


		/*--------------------------------------------------------------------------------------
			Load the LangProject_Pictures folder and file names from the database.
		--------------------------------------------------------------------------------------*/
		void LoadFolderAndFilePaths(IOleDbEncap * pode, StrUni & stuFolderName,
			StrUni & stuInternalPath)
		{
			StrUni stuQuery;
			stuQuery.Format(L"SELECT n.Txt, f.InternalPath %n"
				L"FROM LangProject_Pictures pic %n"
				L"JOIN CmFolder_Name n on n.Obj = pic.Dst %n"
				L"JOIN CmFolder_Files fol on fol.Src = pic.Dst %n"
				L"JOIN CmFile f on f.Id = fol.Dst");
			IOleDbCommandPtr qodc;
			HRESULT hr = pode->CreateCommand(&qodc);
			unitpp::assert_eq("LoadFolderAndFilePaths(): pode->CreateCommand(&qodc) hr",
				S_OK, hr);
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("LoadFolderAndFilePaths(): qodc->ExecCommand(\"SELECT ...\") hr",
				S_OK, hr);
			hr = qodc->GetRowset(0);
			unitpp::assert_eq("LoadFolderAndFilePaths(): qodc->GetRowset(0) hr", S_OK, hr);
			ComBool fMoreRows;
			hr = qodc->NextRow(&fMoreRows);
			unitpp::assert_eq("LoadFolderAndFilePaths(): qodc->NextRow(&fMoreRows) hr",
				S_OK, hr);
			if (fMoreRows)
			{
				wchar rgch[4001];
				ULONG cbSpaceTaken;
				ComBool fIsNull;
				int cch;

				hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgch), isizeof(rgch),
					&cbSpaceTaken, &fIsNull, 2);
				unitpp::assert_eq("LoadFolderAndFilePaths(): GetColValue 1 hr", S_OK, hr);
				cch = (int)cbSpaceTaken / isizeof(wchar);
				if (fIsNull || cch == 0)
					stuFolderName.Clear();
				else
					stuFolderName.Assign(rgch);

				hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgch), isizeof(rgch),
					&cbSpaceTaken, &fIsNull, 2);
				unitpp::assert_eq("LoadFolderAndFilePaths(): GetColValue 2 hr", S_OK, hr);
				cch = (int)cbSpaceTaken / isizeof(wchar);
				if (fIsNull || cch == 0)
					stuInternalPath.Clear();
				else
					stuInternalPath.Assign(rgch);

				hr = qodc->NextRow(&fMoreRows);
				unitpp::assert_eq("LoadFolderAndFilePaths(): qodc->NextRow(&fMoreRows) [2] hr",
					S_OK, hr);
				unitpp::assert_true("LoadFolderAndFilePaths(): !fMoreRows", !fMoreRows);
			}
			else
			{
				stuFolderName.Clear();
				stuInternalPath.Clear();
			}
			qodc.Clear();
		}

		/*--------------------------------------------------------------------------------------
			Verify that the given links between LexSenses and CmSemanticDomains are correct.
		--------------------------------------------------------------------------------------*/
		void VerifyLexSenseSemanticDomains(IOleDbEncap * pode, LinkInfo rgli[], int kclinks,
			const char * pszTag)
		{
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			int cLinks = 0;
			GUID guidLexSense;
			GUID guidSemDom;
			StrAnsi sta;
			StrUni stuQuery;
			stuQuery.Format(L"SELECT ls.Guid$, sd.Guid$%n"
				L"FROM LexSense_SemanticDomains lssd%n"
				L"JOIN CmObject ls ON ls.Id = lssd.Src%n"
				L"JOIN CmObject sd ON sd.Id = lssd.Dst");
			IOleDbCommandPtr qodc;
			hr = pode->CreateCommand(&qodc);
			sta.Format("VerifyLexSenseSemanticDomains qode->CreateCommand hr [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			sta.Format("VerifyLexSenseSemanticDomains SELECT ... hr [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->GetRowset(0);
			sta.Format("VerifyLexSenseSemanticDomains GetRowset hr [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			sta.Format("VerifyLexSenseSemanticDomains NextRow hr [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			sta.Format("VerifyLexSenseSemanticDomains fMoreRows [%s]", pszTag);
			unitpp::assert_true(sta.Chars(), fMoreRows);

			while (fMoreRows)
			{
				++cLinks;
				hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&guidLexSense),
					sizeof(GUID), &cbSpaceTaken, &fIsNull, 0);
				sta.Format("VerifyLexSenseSemanticDomains GetColValue 1 hr [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
				sta.Format("VerifyLexSenseSemanticDomains GetColValue 1 cbSpace [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), sizeof(GUID), cbSpaceTaken);
				hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&guidSemDom), sizeof(GUID),
					&cbSpaceTaken, &fIsNull, 0);
				sta.Format("VerifyLexSenseSemanticDomains GetColValue 2 hr [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
				sta.Format("VerifyLexSenseSemanticDomains GetColValue 2 cbSpace [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), sizeof(GUID), cbSpaceTaken);
				sta.Format("VerifyLexSenseSemanticDomains valid values [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_true(sta.Chars(),
					VerifyLinkGuids(guidLexSense, guidSemDom, rgli, kclinks));
				hr = qodc->NextRow(&fMoreRows);
				sta.Format("VerifyLexSenseSemanticDomains NextRow hr [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
			}

			// Check the number of SemanticDomain links.
			sta.Format("VerifyLexSenseSemanticDomains - the count of links [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), kclinks, cLinks);
			for (int i = 0; i < kclinks; ++i)
			{
				sta.Format("VerifyLexSenseSemanticDomains link[%d] found [%s]", i, pszTag);
				unitpp::assert_true(sta.Chars(), rgli[i].m_fFound);
			}
			qodc.Clear();
		}

		/*--------------------------------------------------------------------------------------
			Verify that the CmSemanticDomain objects have the proper owners.
		--------------------------------------------------------------------------------------*/
		void VerifySemanticDomainOwnerShip(IOleDbEncap * pode, LinkInfo rgli[], int kclinks,
			const char * pszTag)
		{
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			int cLinks = 0;
			GUID guidSemDom;
			GUID guidOwner;
			StrAnsi sta;
			StrUni stuQuery;
			stuQuery.Format(L"SELECT sd.Guid$, owner.Guid$%n"
				L"FROM CmObject sd%n"
				L"JOIN CmObject owner on owner.Id = sd.Owner$%n"
				L"WHERE sd.Class$=%u", kclidCmSemanticDomain);
			IOleDbCommandPtr qodc;
			hr = pode->CreateCommand(&qodc);
			sta.Format("VerifySemanticDomainOwnerShip qode->CreateCommand hr [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			sta.Format("VerifySemanticDomainOwnerShip SELECT ... hr [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->GetRowset(0);
			sta.Format("VerifySemanticDomainOwnerShip GetRowset hr [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			sta.Format("VerifySemanticDomainOwnerShip NextRow hr [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			sta.Format("VerifySemanticDomainOwnerShip fMoreRows [%s]", pszTag);
			unitpp::assert_true(sta.Chars(), fMoreRows);

			while (fMoreRows)
			{
				++cLinks;
				hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&guidSemDom),
					sizeof(GUID), &cbSpaceTaken, &fIsNull, 0);
				sta.Format("VerifySemanticDomainOwnerShip GetColValue 1 hr [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
				sta.Format("VerifySemanticDomainOwnerShip GetColValue 1 cbSpace [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), sizeof(GUID), cbSpaceTaken);
				hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&guidOwner), sizeof(GUID),
					&cbSpaceTaken, &fIsNull, 0);
				sta.Format("VerifySemanticDomainOwnerShip GetColValue 2 hr [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
				sta.Format("VerifySemanticDomainOwnerShip GetColValue 2 cbSpace [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), sizeof(GUID), cbSpaceTaken);
				sta.Format("VerifySemanticDomainOwnerShip valid values [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_true(sta.Chars(),
					VerifyLinkGuids(guidOwner, guidSemDom, rgli, kclinks));
				hr = qodc->NextRow(&fMoreRows);
				sta.Format("VerifySemanticDomainOwnerShip NextRow hr [%d/%s]",
					cLinks, pszTag);
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
			}

			// Check the number of SemanticDomain ownership links.
			sta.Format("VerifySemanticDomainOwnerShip - the count of links [%s]", pszTag);
			unitpp::assert_eq(sta.Chars(), kclinks, cLinks);
			for (int i = 0; i < kclinks; ++i)
			{
				sta.Format("VerifySemanticDomainOwnerShip link[%d] found [%s]", i, pszTag);
				unitpp::assert_true(sta.Chars(), rgli[i].m_fFound);
			}
			qodc.Clear();
		}

		/*--------------------------------------------------------------------------------------
			Verify that the given link between two objects is valid.
		--------------------------------------------------------------------------------------*/
		bool VerifyLinkGuids(const GUID & guidFrom, const GUID & guidTo,
			LinkInfo rgli[], int cli)
		{
			for (int i = 0; i < cli; ++i)
			{
				if (rgli[i].IsMatch(guidFrom, guidTo))
					return true;
			}
			return false;
		}

		/*--------------------------------------------------------------------------------------
			Get the database id and owner's database id for the given flid of a list or other
			major object like the lexical database.
		--------------------------------------------------------------------------------------*/
		int GetIdAndOwner(IOleDbEncap * pode, int flid, const char * pszLabel,
			int * phvoOwner)
		{
			int hvoList;
			int hvoListOwner;
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			StrUni stuQuery;
			StrAnsi staMsg;
			stuQuery.Format(L"SELECT [Id],Owner$ from CmObject where OwnFlid$ = %d", flid);

			IOleDbCommandPtr qodc;
			hr = pode->CreateCommand(&qodc);
			unitpp::assert_eq("testUpdateListFromXml qode->CreateCommand hr", S_OK, hr);

			hr = qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset);
			staMsg.Format("Select [Id],Owner$ for %s ExecCommand hr", pszLabel);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);

			hr = qodc->GetRowset(0);
			staMsg.Format("Select [Id],Owner$ for %s GetRowset hr", pszLabel);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);

			hr = qodc->NextRow(&fMoreRows);
			staMsg.Format("Select [Id],Owner$ for %s NextRow hr", pszLabel);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("Select [Id],Owner$ for %s fMoreRows", pszLabel);
			unitpp::assert_true(staMsg.Chars(), fMoreRows);

			hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoList), isizeof(hvoList),
				&cbSpaceTaken, &fIsNull, 0);
			staMsg.Format("Select [Id],Owner$ for %s GetColValue 1 hr", pszLabel);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("Select [Id],Owner$ for %s GetColValue 1: not null", pszLabel);
			unitpp::assert_true(staMsg.Chars(), !fIsNull);
			staMsg.Format("Select [Id],Owner$ for %s GetColValue 1: not zero", pszLabel);
			unitpp::assert_true(staMsg.Chars(), hvoList);

			hr = qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hvoListOwner),
				isizeof(hvoListOwner), &cbSpaceTaken, &fIsNull, 0);
			staMsg.Format("Select [Id],Owner$ for %s GetColValue 2 hr", pszLabel);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("Select [Id],Owner$ for %s GetColValue 2: not null", pszLabel);
			unitpp::assert_true(staMsg.Chars(), !fIsNull);
			staMsg.Format("Select [Id],Owner$ for %s GetColValue 2: not zero", pszLabel);
			unitpp::assert_true(staMsg.Chars(), hvoListOwner);

			qodc.Clear();	// not needed, but safe.

			*phvoOwner = hvoListOwner;
			return hvoList;
		}


		/*--------------------------------------------------------------------------------------
			Scan the file for any file pathnames containing "C:\\FW\\", and fix them to replace
			"C:\\FW" with the proper value for the local machine.  If any fixes are made (to a
			copy of the file), adjust the input filename to refer to the fixed file.
		--------------------------------------------------------------------------------------*/
		void FixInternalPaths(StrApp & strXml)
		{
			int ich = m_strRootDir.ReverseFindCh('\\');
			StrAnsi staBase(m_strRootDir.Chars(), ich + 1);
			if (staBase == "C:\\FW\\")
				return;
			StrAnsi staXml(strXml);
			FILE * fp;
			fopen_s(&fp, staXml.Chars(), "r");
			ich = staXml.ReverseFindCh('.');
			StrAnsi staXml2(staXml.Chars(), ich);
			staXml2.Append("1.xml");
			char rgch[1024];
			FILE * fpOut;
			fopen_s(&fpOut, staXml2.Chars(), "w");
			while (fgets(rgch, sizeof(rgch), fp) != NULL)
			{
				char * psz = strstr(rgch, "C:\\FW\\");
				if (psz != NULL)
				{
					*psz = '\0';
					fputs(rgch, fpOut);
					fputs(staBase.Chars(), fpOut);
					fputs(psz + 6, fpOut);
				}
				else
				{
					fputs(rgch, fpOut);
				}
			}
			fclose(fp);
			fclose(fpOut);
			strXml.Assign(staXml2);
		}


		/*--------------------------------------------------------------------------------------
			Delete the test database with the given name.
		--------------------------------------------------------------------------------------*/
		void DropTestDatabase(const achar * pszProjName, const achar * pszMdfFile,
			const achar * pszLdfFile)
		{
			try
			{
				// Delete the UnitTestProj database.
				StrUni stuDatabase(L"master");
				IOleDbEncapPtr qode; // Declare before qodc.
				IOleDbCommandPtr qodc;
				qode.CreateInstance(CLSID_OleDbEncap);
				CheckHr(qode->Init(m_stuServer.Bstr(), stuDatabase.Bstr(), NULL,
					koltReturnError, 1000));
				CheckHr(qode->CreateCommand(&qodc));

				StrUni stuQuery;
				StrUni stuProjName(pszProjName);
				stuQuery.Format(L"DROP DATABASE [%s]", stuProjName.Chars());
				CheckHr(qode->CreateCommand(&qodc));
				CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
				qodc.Clear();
				qode.Clear();
			}
			catch (...)
			{
			}
			// Just in case, try to delete the relevant files.
			if (pszMdfFile && *pszMdfFile)
				::DeleteFile(pszMdfFile);
			if (pszLdfFile && *pszLdfFile)
				::DeleteFile(pszLdfFile);
		}

	public:
		TestFwXmlData();

		virtual void Setup()
		{
			FwXmlData::CreateCom(NULL, IID_IFwXmlData, (void **)&m_qfwxd);
		}
		virtual void Teardown()
		{
			m_qfwxd.Clear();
		}

		virtual void SuiteSetup()
		{
			// Copy the template database files, renaming them in the process, and then
			// "attach" the new database.
			// Get the local server name.
			achar psz[MAX_COMPUTERNAME_LENGTH + 1];
			ulong cch = isizeof(psz);
			::GetComputerName(psz, &cch);
			StrUni stuMachine(psz);
			m_stuServer.Format(L"%s\\SILFW", stuMachine.Chars());

			SetRootDir();

			// Make sure the unit test database doesn't exist, then create it.
			const achar * pszDbName = _T("UnitTestProj");
			m_stuDbName = pszDbName;
			SetFilenames(pszDbName, m_strMdfFile, m_strLdfFile);
			DropTestDatabase(pszDbName, m_strMdfFile.Chars(), m_strLdfFile.Chars());
			CreateEmptyDatabase(pszDbName, m_strMdfFile.Chars(), m_strLdfFile.Chars());

			// Create the input XML filenames.
			m_strXmlInit.Format(_T("%s\\UnitTestProj.xml"), m_strSrcDir.Chars());
			m_strXmlImport.Format(_T("%s\\AnthroList.xml"), m_strSrcDir.Chars());
			m_strXmlImportMulti.Format(_T("%s\\LLImportTest.xml"), m_strSrcDir.Chars());
			m_strXmlImport2.Format(_T("%s\\SemanticDomainList.xml"), m_strSrcDir.Chars());
			m_strXmlImport3.Format(_T("%s\\MoreLexEntries.xml"), m_strSrcDir.Chars());
			m_strXmlImport4.Format(_T("%s\\SFImportTest.xml"), m_strSrcDir.Chars());
			m_strXmlUpdateList.Format(_T("%s\\RevisedSemDomList.xml"), m_strSrcDir.Chars());
		}

		virtual void SuiteTeardown()
		{
			// Shut down any LgWritingSystemFactory that may have been created by one of the
			// Import tests.  (See LT-3583 for details.)
			ILgWritingSystemFactoryBuilderPtr qwsfb;
			qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
			qwsfb->ShutdownAllFactories();
			qwsfb.Clear();

			StrApp strDbName(m_stuDbName);
			DropTestDatabase(strDbName.Chars(), m_strMdfFile.Chars(), m_strLdfFile.Chars());
		}
	};
}

#endif /*TESTFWXMLDATA_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkcel-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
