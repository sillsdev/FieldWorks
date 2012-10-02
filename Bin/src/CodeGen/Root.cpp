#include "CmCG.h"
#include <iomanip>

extern bool g_fVerbose;
STR g_strSearchPath;
ModuleParser g_mop;

// TODO: Nuke.
int g_clidNext;
int g_flidNext;

/***********************************************************************************************
	Module parser.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Start an XML element.
----------------------------------------------------------------------------------------------*/
void ModuleParser::StartElement(STR strElem, const char ** prgpszAttr)
{
	const char ** ppsz;

	if (strElem == "CellarModule")
	{
		if (m_fParseClasses || m_vpcp.size() > 0)
			ParseError("Unexpected CellarModule Element");
		m_fParseClasses = true;

		for (ppsz = prgpszAttr; *ppsz; ppsz++)
		{
			AssertPsz(*ppsz);

			STR strAttr(*ppsz);
			STR strCur = (ppsz[1] ? *++ppsz : "");

			if (strAttr == "id")
			{
				if (!strCur.length())
				{
					ParseError(STR("Empty 'id' attribute"));
					continue;
				}

				if (m_strName.length())
				{
					ParseError(STR("Duplicate 'id' attribute: ") + strCur);
					continue;
				}

				m_strName = strCur;
				continue;
			}

			if (strAttr == "num")
			{
				if (!strCur.length())
				{
					ParseError(STR("Empty 'num' attribute"));
					continue;
				}

				if (m_mid >= 0)
				{
					ParseError(STR("Duplicate 'num' attribute: ") + strCur);
					continue;
				}

				m_mid = IntFromStr(strCur.c_str());
				if ((uint)m_mid >= (uint)kmidMax)
					ParseError(STR("Module number out of range: ") + strCur);
				continue;
			}

			if (strAttr == "ver")
			{
				if (!strCur.length())
				{
					ParseError(STR("Empty 'ver' attribute"));
					continue;
				}

				if (m_ver > 0)
				{
					ParseError(STR("Duplicate 'ver' attribute: ") + strCur);
					continue;
				}

				m_ver = IntFromStr(strCur.c_str());
				if (m_ver <= 0)
					ParseError(STR("Version number out of range: ") + strCur);
				continue;
			}

			if (strAttr == "verBack")
			{
				if (!strCur.length())
				{
					ParseError(STR("Empty 'verBack' attribute"));
					continue;
				}

				if (m_verBack > 0)
				{
					ParseError(STR("Duplicate 'verBack' attribute: ") + strCur);
					continue;
				}

				m_verBack = IntFromStr(strCur.c_str());
				if (m_verBack <= 0)
					ParseError(STR("Back version number out of range: ") + strCur);
				continue;
			}
		}

		if (!m_strName.length())
			ParseError(STR("Missing 'id' attribute"));
		if (m_mid < 0)
			ParseError(STR("Missing 'num' attribute"));
		if (m_ver <= 0)
			ParseError(STR("Missing 'ver' attribute"));
		if (m_verBack <= 0)
			m_verBack = m_ver;
		else if (m_verBack > m_ver)
			ParseError(STR("Back version number should be <= the version number."));
	}
	else if (strElem == "class")
	{
		if (m_pcp)
			ParseError(STR("Invalid nesting of <class> elements in XML file?!"));
		m_pcp = new ClassParser;
		AssertPtr(m_pcp);
		m_pcp->SetParser(m_pxmlp, m_strFile);		// Mostly needed for error messages.
		m_pcp->StartElement(strElem, prgpszAttr);
	}
	else if (m_pcp)
	{
		m_pcp->StartElement(strElem, prgpszAttr);
	}
}


/*----------------------------------------------------------------------------------------------
	End the XML element.
----------------------------------------------------------------------------------------------*/
void ModuleParser::EndElement(STR strElem)
{
	if (strElem == "CellarModule")
	{
		if (!m_fParseClasses)
			ParseError(STR("Unexpected end of CellarModule"));
		m_fParseClasses = false;
	}
	else if (strElem == "class")
	{
		if (!m_pcp)
		{
			ParseError(STR("Invalid nesting of </class> elements in XML file?!"));
		}
		else
		{
			m_pcp->EndElement(strElem);
			if (m_pcp->HasError())
			{
				delete m_pcp;
			}
			else if (m_pcp->Clid() <= 0)
			{
				if (g_fVerbose)
					std::cerr << "Skipping built-in class " << m_pcp->Name() <<
						" [" << m_pcp->Clid() << "]" << std::endl;
				delete m_pcp;
			}
			else
			{
				if (g_fVerbose)
					std::cerr << "Adding module class " << m_pcp->Name() <<
						" [" << m_pcp->Clid() << "]" << std::endl;
				m_vpcp.push_back(m_pcp);
			}
			m_pcp = NULL;
		}
	}
	else if (m_pcp)
	{
		m_pcp->EndElement(strElem);
	}
}


/*----------------------------------------------------------------------------------------------
	Parse a class and save the results.
----------------------------------------------------------------------------------------------*/
bool ModuleParser::ExternalEntityRefHandler(const char * pszFile)
{
	ClassParser * pcp = new ClassParser;

	if (!pcp->Parse(STR(pszFile)))
		return false;

	if (g_fVerbose)
		std::cerr << "Adding module class " << pcp->Name() <<
			" [" << pcp->Clid() << "]" << std::endl;
	m_vpcp.push_back(pcp);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Generate the output files.
----------------------------------------------------------------------------------------------*/
int ModuleParser::GenerateCode(std::ostream & stmSql, std::ostream & stmSqh)
{
	ClassParser ** ppcp;
	int nRet;
	bool fFirst;

	// Generate the class definitions and constant definitions.
	fFirst = true;
	for (ppcp = m_vpcp.begin(); ppcp < m_vpcp.end(); ppcp++)
	{
		nRet = (*ppcp)->GenerateCode(fFirst, IDR_TMPL_SQL_CLASSES, stmSql);
		if (nRet)
			return nRet;
		nRet = (*ppcp)->GenerateCode(fFirst, IDR_TMPL_DEFNS, stmSqh);
		if (nRet)
			return nRet;
		fFirst = false;
	}

	// Generate the field definitions.
	fFirst = true;
	for (ppcp = m_vpcp.begin(); ppcp < m_vpcp.end(); ppcp++)
	{
		nRet = (*ppcp)->GenerateCode(fFirst, IDR_TMPL_SQL_FIELDS, stmSql);
		if (nRet)
			return nRet;
		fFirst = false;
	}

	return 0;
}


/***********************************************************************************************
	Class parser.
***********************************************************************************************/

void ClassParser::StartElement(STR strElem, const char **prgpszAttr)
{
	AssertPtr(prgpszAttr);

	const char ** ppsz;

	if (strElem == "class")
		m_cps = kcpsClass;
	else if (m_cps == kcpsNull)
	{
		ParseError("Expected Class Element");
		m_cps = kcpsError;
	}
	else if (m_cps == kcpsPostClass)
	{
		ParseError("Expected End of Text");
		m_cps = kcpsError;
	}
	else if (strElem == "props")
		m_cps = kcpsAttributes;


	if (strElem == "class")
	{
		g_flidNext = 0;

		bool fAbstractSeen = false;

		for (ppsz = prgpszAttr; *ppsz; ppsz++)
		{
			AssertPsz(*ppsz);

			STR strAttr(*ppsz);
			STR strCur = (ppsz[1] ? *++ppsz : "");

			if (strAttr == "id")
			{
				if (!strCur.length())
				{
					ParseError(STR("Empty 'id' attribute"));
					continue;
				}

				if (m_strName.length())
				{
					ParseError(STR("Duplicate 'id' attribute: ") + strCur);
					continue;
				}

				m_strName = strCur;
				continue;
			}

			if (strAttr == "num")
			{
				if (!strCur.length())
				{
					ParseError(STR("Empty 'num' attribute"));
					continue;
				}

				if (m_clid >= 0)
				{
					ParseError(STR("Duplicate 'num' attribute: ") + strCur);
					continue;
				}

				m_clid = IntFromStr(strCur.c_str());
				if ((uint)m_clid >= (uint)kclidMax)
					ParseError(STR("Class number out of range: ") + strCur);
				if (m_clid != 0)
					m_clid += g_mop.Mid() * kclidMax;
				continue;
			}

			if (strAttr == "base")
			{
				if (!strCur.length())
				{
					ParseError(STR("Empty 'base' attribute"));
					continue;
				}

				if (m_strBase.length())
				{
					ParseError(STR("Duplicate 'base' attribute: ") + strCur);
					continue;
				}

				m_strBase = strCur;
				continue;
			}

			if (strAttr == "abbr")
			{
				if (!strCur.length())
				{
					ParseError(STR("Empty 'abbr' attribute"));
					continue;
				}

				if (m_strAbbr.length())
				{
					ParseError(STR("Duplicate 'abbr' attribute: ") + strCur);
					continue;
				}

				m_strAbbr = strCur;
				for (int ich = 0; ich < strCur.length(); ich++)
				{
					if (strCur[ich] < 'a' || strCur[ich] > 'z')
					{
						ParseError(
							STR("Bad 'abbr' attribute - should be lowercase letters: ") +
								strCur);
						break;
					}
				}
				continue;
			}

			if (strAttr == "abstract")
			{
				if (fAbstractSeen)
				{
					ParseError(STR("Duplicate 'abstract' attribute: ") + strCur);
					continue;
				}

				fAbstractSeen = true;
				if (strCur == "true")
					m_fAbstract = true;
				else if (strCur != "false")
					ParseError(STR("Bad 'abstract' attribute: ") + strCur);
				continue;
			}
		}

		if (!m_strName.length())
			ParseError(STR("Missing 'id' attribute"));
#ifdef FUTURE // TODO: enable this.
		if (!m_strAbbr.length())
			ParseError(STR("Missing 'abbr' attribute"));
#endif // FUTURE
		if (!m_strBase.length())
			ParseError(STR("Missing 'base' attribute"));
		if (m_clid < 0)
		{
			// TODO: Fix this.
			m_clid = ++g_clidNext + g_mop.Mid() * kclidMax;;
			// ParseError(STR("Missing 'num' attribute"));
		}

		return;
	}

	int bt;

	if (strElem == "owning")
		bt = kbtOwnAtom;
	else if (strElem == "rel")
		bt = kbtRefAtom;
	else if (strElem == "basic")
		bt = kbtNil;
	else
		return;

	m_propCur.Init();
	m_propCur.m_bt = bt;

	STR strBits;
	STR strMin;
	STR strMax;
	STR strDef;
	STR strPrec;
	STR strScale;
	STR strBig;
	bool fCardSeen = false;
	bool fKindSeen = false;

	// Property of some type.
	for (ppsz = prgpszAttr; *ppsz; ppsz++)
	{
		STR strAttr(*ppsz);
		STR strCur = (ppsz[1] ? *++ppsz : "");

		if (strAttr == "id")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'id' attribute"));
				continue;
			}

			if (m_propCur.m_strName.length())
			{
				ParseError(STR("Duplicate 'id' attributes: ") + strCur);
				continue;
			}

			m_propCur.m_strName = strCur;
			continue;
		}

		if (strAttr == "num")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'num' attribute"));
				continue;
			}

			if (m_propCur.m_flid >= 0)
			{
				ParseError(STR("Duplicate 'num' attribute: ") + strCur);
				continue;
			}

			m_propCur.m_flid = IntFromStr(strCur.c_str());
			if ((uint)m_propCur.m_flid >= (uint)kflidMax)
				ParseError(STR("Field number out of range: ") + strCur);
			m_propCur.m_flid += m_clid * kflidMax;
			continue;
		}

		if (strAttr == "sig")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'sig' attribute"));
				continue;
			}

			if (m_propCur.m_strType.length())
			{
				ParseError(STR("Duplicate 'sig' attributes: ") + strCur);
				continue;
			}

			m_propCur.m_strType = strCur;
			continue;
		}

		if (strAttr == "card")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'card' attribute"));
				continue;
			}

			if (fCardSeen)
			{
				ParseError(STR("Duplicate 'card' attribute: ") + strCur);
				continue;
			}

			fCardSeen = true;
			if (!m_propCur.IsAtom())
			{
				ParseError(STR("Attribute 'card' is illegal for basic properties"));
				continue;
			}

			if (strCur == "col")
				m_propCur.m_bt += kbtMinCol - kbtMinAtom;
			else if (strCur == "seq")
				m_propCur.m_bt += kbtMinSeq - kbtMinAtom;
			else if (strCur != "atomic")
				ParseError(STR("Unknown 'card' type"));
			continue;
		}

		if (strAttr == "kind")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'kind' attribute"));
				continue;
			}

			if (fKindSeen)
			{
				ParseError(STR("Duplicate 'kind' attribute: ") + strCur);
				continue;
			}

			fKindSeen = true;
			if (!m_propCur.IsObject())
			{
				ParseError(STR("Attribute 'kind' is only legal for 'rel' properties"));
				continue;
			}

			if (strCur != "reference")
			{
				if (strCur != "backref")
					ParseError(STR("Bad 'kind' attribute"));
				m_propCur.m_bt = kbtNil;
				return;
			}
			continue;
		}

		if (strAttr == "prefix")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'prefix' attribute"));
				continue;
			}

			if (m_propCur.m_strPrefix.length())
			{
				ParseError(STR("Duplicate 'prefix' attribute"));
				continue;
			}

			m_propCur.m_strPrefix = strCur;
			for (int ich = 0; ich < strCur.length(); ich++)
			{
				if (strCur[ich] < 'a' || strCur[ich] > 'z')
				{
					ParseError(STR("Bad 'prefix' attribute - should be lowercase letters: ") +
						strCur);
					break;
				}
			}
			continue;
		}

		// Numeric attributes.
		if (strAttr == "bits")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'bits' attribute"));
				continue;
			}
			if (strBits.length())
				ParseError(STR("Duplicate 'bits' attribute: ") + strCur);
			else
				strBits = strCur;
			continue;
		}
		if (strAttr == "min")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'min' attribute"));
				continue;
			}
			if (strMin.length())
				ParseError(STR("Duplicate 'min' attribute: ") + strCur);
			else
				strMin = strCur;
			continue;
		}
		if (strAttr == "max")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'max' attribute"));
				continue;
			}
			if (strMax.length())
				ParseError(STR("Duplicate 'max' attribute: ") + strCur);
			else
				strMax = strCur;
			continue;
		}
		if (strAttr == "prec")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'prec' attribute"));
				continue;
			}
			if (strPrec.length())
				ParseError(STR("Duplicate 'prec' attribute: ") + strCur);
			else
				strPrec = strCur;
			continue;
		}
		if (strAttr == "scale")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'scale' attribute"));
				continue;
			}
			if (strScale.length())
				ParseError(STR("Duplicate 'scale' attribute: ") + strCur);
			else
				strScale = strCur;
			continue;
		}
		if (strAttr == "default")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'default' attribute"));
				continue;
			}
			if (strDef.length())
				ParseError(STR("Duplicate 'default' attribute: ") + strCur);
			else
				strDef = strCur;
			continue;
		}
		if (strAttr == "big")
		{
			if (!strCur.length())
			{
				ParseError(STR("Empty 'big' attribute"));
				continue;
			}
			if (strBig.length())
				ParseError(STR("Duplicate 'big' attribute: ") + strCur);
			else if (strCur != "true" && strCur != "false")
				ParseError(STR("Bad 'big' attribute: ") + strCur);
			else
				strBig = strCur;
			continue;
		}
	}

	if (!m_propCur.m_strName.length())
		ParseError(STR("Missing 'id' attribute"));
	if (!m_propCur.m_strType.length())
		ParseError(STR("Missing 'sig' attribute"));
	if (!m_propCur.m_strPrefix.length())
		m_propCur.m_strPrefix = "p";
	if (m_propCur.m_flid < 0)
	{
		// TODO: Fix this.
		m_propCur.m_flid = ++g_flidNext + m_clid * kflidMax;
		// ParseError(STR("Missing 'num' attribute"));
	}

	if (!m_propCur.IsObject())
	{
		// Basic property checks.
		if (m_propCur.m_strType == "Integer")
		{
			m_propCur.m_bt = kbtInteger;
			if (strBits != "")
			{
				int cbit = IntFromStr(strBits.c_str());
				if (cbit <= 0 || cbit > 32)
					ParseError(STR("Bad 'bits' attribute: ") + strBits);
				else if (cbit < 32)
				{
					m_propCur.m_nMin = 0;
					m_propCur.m_nMax = (1 << cbit) - 1;
					m_propCur.m_nDef = 0;
				}
				strBits = "";
			}
			if (strMin != "")
			{
				int nMin = IntFromStr(strMin.c_str());
				if (nMin < m_propCur.m_nMin)
					ParseError(STR("Bad 'min' attribute: ") + strMin);
				else
				{
					m_propCur.m_nMin = nMin;
					m_propCur.m_nDef = Max(0, m_propCur.m_nMin);
				}
				strMin = "";
			}
			if (strMax != "")
			{
				int nMax = IntFromStr(strMax.c_str());
				if (nMax > m_propCur.m_nMax || nMax <= m_propCur.m_nMin)
					ParseError(STR("Bad 'max' attribute: ") + strMax);
				else
				{
					m_propCur.m_nMax = nMax;
					if (m_propCur.m_nDef > nMax)
						m_propCur.m_nDef = nMax;
				}
				strMax = "";
			}
			if (strDef != "")
			{
				int nDef = IntFromStr(strDef.c_str());
				if (nDef < m_propCur.m_nMin || nDef > m_propCur.m_nMax)
					ParseError(STR("Bad 'default' attribute: ") + strDef);
				else
					m_propCur.m_nDef = nDef;
				strDef = "";
			}
		}
		else if (m_propCur.m_strType == "Numeric")
		{
			m_propCur.m_bt = kbtNumeric;
			m_propCur.m_nPrec = 19;
			m_propCur.m_nScale = 0;
			if (strPrec != "")
			{
				int nPrec = IntFromStr(strPrec.c_str());
				if (nPrec <= 0 || nPrec > 28)
					ParseError(STR("Bad 'prec' attribute: ") + strPrec);
				else
					m_propCur.m_nPrec = nPrec;
				strPrec = "";
			}
			if (strScale != "")
			{
				int nScale = IntFromStr(strScale.c_str());
				if (nScale <= 0 || nScale > m_propCur.m_nPrec)
					ParseError(STR("Bad 'scale' attribute: ") + strScale);
				else
					m_propCur.m_nScale = nScale;
				strScale = "";
			}
		}
		else if (m_propCur.m_strType == "Boolean")
			m_propCur.m_bt = kbtBoolean;
		else if (m_propCur.m_strType == "Time")
			m_propCur.m_bt = kbtTime;
		else if (m_propCur.m_strType == "GenDate")
			m_propCur.m_bt = kbtGenDate;
		else if (m_propCur.m_strType == "Binary")
		{
			m_propCur.m_bt = kbtBinary;
			if (strBig == "true")
				m_propCur.m_fBig = true;
			strBig = "";
		}
		else if (m_propCur.m_strType == "Float")
			m_propCur.m_bt = kbtFloat;
		else if (m_propCur.m_strType == "Int64")
		{
			// REVIEW ShonK: Should we support Int64? Should we use binary(8) instead of numeric?
			m_propCur.m_bt = kbtNumeric;
			m_propCur.m_nPrec = 19;
			m_propCur.m_nScale = 0;
			m_propCur.m_strType = "Numeric";
		}
		else if (m_propCur.m_strType == "String")
		{
			m_propCur.m_bt = kbtString;
			m_propCur.m_fFmt = true;
			if (strBig == "true")
			{
				m_propCur.m_fBig = true;
				m_propCur.m_strType = "BigString";
			}
			strBig = "";
		}
		else if (m_propCur.m_strType == "MultiString")
		{
			m_propCur.m_bt = kbtString;
			m_propCur.m_fFmt = true;
			m_propCur.m_fMulti = true;
			if (strBig == "true")
			{
				m_propCur.m_fBig = true;
				m_propCur.m_strType = "MultiBigString";
			}
			strBig = "";
		}
		else if (m_propCur.m_strType == "Unicode")
		{
			m_propCur.m_bt = kbtString;
			if (strBig == "true")
			{
				m_propCur.m_fBig = true;
				m_propCur.m_strType = "BigUnicode";
			}
			strBig = "";
		}
		else if (m_propCur.m_strType == "MultiUnicode")
		{
			m_propCur.m_bt = kbtString;
			m_propCur.m_fMulti = true;
			if (strBig == "true")
			{
				m_propCur.m_fBig = true;
				m_propCur.m_strType = "MultiBigUnicode";
			}
			strBig = "";
		}
		else if (m_propCur.m_strType == "Guid")
		{
			m_propCur.m_bt = kbtGuid;
		}
		else if (m_propCur.m_strType == "Image")
		{
			m_propCur.m_bt = kbtImage;
		}
		else
			ParseError(STR("Unknown basic type"));

		// If we use these we set them to empty above. If they are still non-empty here, it's
		// an error.
		if (strBits.length())
			ParseError(STR("Non-integer type should not have a 'bits' attribute: ") + strBits);
		if (strMin.length())
			ParseError(STR("Non-integer type should not have a 'min' attribute: ") + strMin);
		if (strMax.length())
			ParseError(STR("Non-integer type should not have a 'max' attribute: ") + strMax);
		if (strPrec.length())
			ParseError(STR("Non-numeric type should not have a 'prec' attribute: ") + strPrec);
		if (strScale.length())
			ParseError(STR("Non-numeric type should not have a 'scale' attribute: ") + strScale);
		if (strBig.length())
			ParseError(STR("Non-string type should not have a 'big' attribute: ") + strBig);
	}
}


void ClassParser::EndElement(STR strElem)
{
	if (m_propCur.m_bt != kbtNil &&
		(strElem == "owning" || strElem == "rel" || strElem == "basic"))
	{
		m_rgprop.push_back(m_propCur);
	}

	if (strElem == "class")
		m_cps = kcpsPostClass;
}


int ClassParser::GenerateCode(bool fFirst, int rid, std::ostream & stmOut)
{
	STR strError;
	HRSRC hrsrc;
	int cb;
	HGLOBAL hres;
	char *pchLim;
	char *pch = NULL;
	char *prgch = NULL;
	int nLine = 1;
	char *pchLine = NULL;
	int cactIfLevel = 0;
	char *pchLoop;
	char *pchLineLoop;
	int nLineLoop;
	int cactIfLoop;
	std::vector<Prop>::iterator ppropLoop = NULL;

	hrsrc = FindResource(NULL, MAKEINTRESOURCE(rid), "TEMPLATE");
	if (!hrsrc)
	{
		strError = "Can't open resource";
		goto LError;
	}

	cb = SizeofResource(NULL, hrsrc);
	if (cb == 0)
	{
		strError = "Resource is empty";
		goto LError;
	}

	hres = LoadResource(NULL, hrsrc);
	if (!hres)
	{
		strError = "Loading resource failed";
		goto LError;
	}

	prgch = (char *)LockResource(hres);
	if (!prgch)
	{
		strError = "Locking resource failed";
		goto LError;
	}
	pchLim = prgch + cb;
	pchLine = prgch;


	#define EOI(pch) ((pch) >= pchLim)
	#define MATCH(sz) (pchLim - pch >= sizeof(sz) - 1) && \
		(0 == memcmp(pch, sz, sizeof(sz) - 1)) && \
		((pch += sizeof(sz) - 1), true)

	for (pch = prgch; !EOI(pch); )
	{
		if ('$' != *pch)
		{
			if (0x0D != *pch)
			{
				stmOut << *pch++;
				continue;
			}
			stmOut << std::endl;
			if (++pch < pchLim && 0x0A == *pch)
				pch++;
			pchLine = pch;
			nLine++;
			continue;
		}

		pch++;

		if (*pch == 0x0D)
		{
			if (++pch < pchLim && 0x0A == *pch)
				pch++;
			pchLine = pch;
			nLine++;
			continue;
		}
		if (*pch == ' ')
		{
			pch++;
			continue;
		}
		if (*pch == '$')
		{
			stmOut << '$';
			pch++;
			continue;
		}

		if ('{' == *pch)
		{
			// Macros.
			pch++;
			if (MATCH("Class}"))
			{
				stmOut << m_strName;
				continue;
			}
			if (MATCH("Base}"))
			{
				stmOut << m_strBase;
				continue;
			}
			if (MATCH("Mid}"))
			{
				stmOut << g_mop.Mid();
				continue;
			}
			if (MATCH("Module}"))
			{
				stmOut << g_mop.Name();
				continue;
			}
			if (MATCH("ModVer}"))
			{
				stmOut << g_mop.Ver();
				continue;
			}
			if (MATCH("ModVerBack}"))
			{
				stmOut << g_mop.VerBack();
				continue;
			}
			if (MATCH("Clid}"))
			{
				stmOut << m_clid;
				continue;
			}
			if (MATCH("Abbr}"))
			{
				stmOut << m_strAbbr;
				continue;
			}
			if (MATCH("NewGuid}"))
			{
				GUID guid;
				::CoCreateGuid(&guid);
				::PrintGuid(stmOut, guid, "-", "");
				continue;
			}

			// Property Macros.
			if (!ppropLoop)
			{
				strError = "Invalid Macro";
				goto LError;
			}
			if (MATCH("PropName}"))
			{
				stmOut << ppropLoop->m_strName;
				continue;
			}
			if (MATCH("PropMin}"))
			{
				if (ppropLoop->m_bt == kbtInteger && ppropLoop->m_nMin != knIntMin)
					stmOut << ppropLoop->m_nMin;
				else
					stmOut << "null";
				continue;
			}
			if (MATCH("PropMax}"))
			{
				if (ppropLoop->m_bt == kbtInteger && ppropLoop->m_nMax != knIntMax)
					stmOut << ppropLoop->m_nMax;
				else
					stmOut << "null";
				continue;
			}
			if (MATCH("PropBig}"))
			{
				if (ppropLoop->m_bt == kbtBinary/* || ppropLoop->m_bt == kbtString*/)
				{
					if (ppropLoop->m_fBig)
						stmOut << "1";
					else
						stmOut << "0";
				}
				else
					stmOut << "null";
				continue;
			}
			if (MATCH("PropDstType}"))
			{
				stmOut << ppropLoop->m_strType;
				continue;
			}
			if (MATCH("PropType}"))
			{
				if (ppropLoop->IsObject())
				{
					if (ppropLoop->IsOwning())
						stmOut << "Owning";
					else
						stmOut << "Reference";

					if (ppropLoop->IsAtom())
						stmOut << "Atom";
					else if (ppropLoop->IsCollection())
						stmOut << "Collection";
					else
					{
						Assert(ppropLoop->IsSequence());
						stmOut << "Sequence";
					}
					continue;
				}
				stmOut << ppropLoop->m_strType;
				continue;
			}
			if (MATCH("PropPrefix}"))
			{
				stmOut << ppropLoop->m_strPrefix;
				continue;
			}
			if (MATCH("PropFlid}"))
			{
				stmOut << ppropLoop->m_flid;
				continue;
			}
			if (MATCH("PropSqlFldDfn}"))
			{
				switch (ppropLoop->m_bt)
				{
				case kbtInteger:
					{
						int nMin = ppropLoop->m_nMin;
						int nMax = ppropLoop->m_nMax;
						int nDef = ppropLoop->m_nDef;
						Assert(nMin <= nDef && nDef <= nMax);

						if (nMax <= 0x00FF && nMin >= 0)
						{
							stmOut << "	[" << ppropLoop->m_strName << "] tinyint not null default "
								<< nDef << "," << std::endl;
						}
						else if (nMax <= 0x7FFF && nMin >= 0xFFFF8000)
						{
							stmOut << "	[" << ppropLoop->m_strName << "] smallint not null default "
								<< nDef << "," << std::endl;
						}
						else
						{
							stmOut << "	[" << ppropLoop->m_strName << "] int not null default "
								<< nDef << "," << std::endl;
						}
						if (nMax < knIntMax || nMin > knIntMin)
						{
							stmOut << "	check ([" << ppropLoop->m_strName << "] <= " << nMax
								<< " and [" << ppropLoop->m_strName << "] >= " << nMin << ")," << std::endl;
						}
					}
					break;
				case kbtNumeric:
					stmOut << "	[" << ppropLoop->m_strName << "] decimal("
						<< ppropLoop->m_nPrec << ", " << ppropLoop->m_nScale
						<< ") not null default 0," << std::endl;
					break;
				case kbtFloat:
					stmOut << "	[" << ppropLoop->m_strName << "] float not null default 0," << std::endl;
					break;
				case kbtBoolean:
					stmOut << "	[" << ppropLoop->m_strName << "] bit not null default 0," << std::endl;
					break;
				case kbtTime:
					stmOut << "	[" << ppropLoop->m_strName << "] datetime null," << std::endl;
					break;
				case kbtString:
					if (ppropLoop->m_fMulti)
						break;
					stmOut << "	[" << ppropLoop->m_strName <<
						(ppropLoop->m_fBig ? "] ntext null," : "] nvarchar(4000) null,") << std::endl;
					if (!ppropLoop->m_fFmt)
						break;
					stmOut << "	[" << ppropLoop->m_strName <<
						(ppropLoop->m_fBig ? "_Fmt] image null," : "_Fmt] varbinary(8000) null,") << std::endl;
					break;
				case kbtGuid:
					stmOut << "	[" << ppropLoop->m_strName << "] uniqueidentifier null," << std::endl;
					break;
				case kbtImage:
					stmOut << "	[" << ppropLoop->m_strName << "] image null," << std::endl;
					break;
				case kbtGenDate:
					stmOut << "	[" << ppropLoop->m_strName << "] int not null default 0," << std::endl;
					break;
				case kbtBinary:
					stmOut << "	[" << ppropLoop->m_strName <<
						(ppropLoop->m_fBig ? "] image null," : "] varbinary(8000) null,") << std::endl;
					break;

				default:
					if (ppropLoop->m_bt == kbtRefAtom)
					{
						stmOut << "	[" << ppropLoop->m_strName << "] int null," << std::endl;
					}
					else
					{
						stmOut << "	-- Non-field type: " << ppropLoop->m_strType <<
							" (" << ppropLoop->m_strName << ")" << std::endl;
					}
					break;
				}
				continue;
			}

			strError = "Invalid Macro";
			goto LError;
		}

		if (MATCH("if("))
		{
LTest:
			bool fWant;
			bool fTest;

			fWant = true;

			if ('!' == *pch)
			{
				pch++;
				fWant = false;
			}

			if (MATCH("Class}"))
			{
				stmOut << m_strName;
				continue;
			}
			if (MATCH("Abstract)"))
				fTest = m_fAbstract;
			else if (MATCH("First)"))
				fTest = fFirst;
			else
			{
				if (!ppropLoop)
				{
					strError = "Invalid 'if' Condition";
					goto LError;
				}

				if (MATCH("PropObject)"))
					fTest = ppropLoop->IsObject();
				else if (MATCH("PropAtom)"))
					fTest = ppropLoop->IsAtom();
				else if (MATCH("PropCollection)"))
					fTest = ppropLoop->IsCollection();
				else if (MATCH("PropSequence)"))
					fTest = ppropLoop->IsSequence();
				else if (MATCH("PropRefGroup)"))
					fTest = ppropLoop->m_bt == kbtRefCol || ppropLoop->m_bt == kbtRefSeq;
				else if (MATCH("PropRefAtom)"))
					fTest = ppropLoop->m_bt == kbtRefAtom;
				else if (MATCH("PropMultiStr)"))
					fTest = ppropLoop->IsMultiString();
				else if (MATCH("PropBigStr)"))
					fTest = ppropLoop->IsBigString();
				else if (MATCH("PropFmtStr)"))
					fTest = ppropLoop->IsFmtString();
				else if (MATCH("PropEmbedded)"))
				{
					fTest = !(ppropLoop->IsOwning() || ppropLoop->IsCollection() ||
						ppropLoop->IsSequence() || ppropLoop->IsMultiString());
				}
				else if (MATCH("PropOwning)"))
					fTest = ppropLoop->IsOwning();
				else
				{
					strError = "Invalid 'if' Construct";
					goto LError;
				}
			}

			if (fTest == fWant)
			{
				cactIfLevel++;
				continue;
			}

			// Skip to $elif, $else or $endif.
			int cactNest = 0;
			for (;;)
			{
				if (EOI(pch))
				{
					strError = "Missing 'endif'";
					goto LError;
				}
				if (*pch != '$')
				{
					if (*pch++ == 0x0D)
					{
						if (pch < pchLim && 0x0A == *pch)
							pch++;
						pchLine = pch;
						nLine++;
					}
					continue;
				}
				pch++;
				if (MATCH("if("))
				{
					cactNest++;
					continue;
				}
				if (MATCH("endif"))
				{
					if (cactNest == 0)
					{
						// We have our endif.
						break;
					}
					cactNest--;
					continue;
				}
				if (cactNest == 0)
				{
					if (MATCH("else"))
					{
						// We have our else.
						cactIfLevel++;
						break;
					}
					if (MATCH("elif("))
						goto LTest;
				}
			}
			continue;
		}

		if (MATCH("else") || MATCH("elif("))
		{
			if (cactIfLevel <= 0)
			{
				strError = "Unexpected 'else' or 'elif'";
				goto LError;
			}
			cactIfLevel--;

			// Skip to the matching $endif.
			int cactNest = 0;
			for (;;)
			{
				if (EOI(pch))
				{
					strError = "Missing 'endif'";
					goto LError;
				}
				if (*pch != '$')
				{
					if (*pch++ == 0x0D)
					{
						if (pch < pchLim && 0x0A == *pch)
							pch++;
						pchLine = pch;
						nLine++;
					}
					continue;
				}
				pch++;
				if (MATCH("if("))
				{
					cactNest++;
					continue;
				}
				if (MATCH("endif"))
				{
					if (cactNest == 0)
					{
						// We have our endif.
						break;
					}
					cactNest--;
					continue;
				}
			}
			continue;
		}
		if (MATCH("endif"))
		{
			if (cactIfLevel <= 0)
			{
				strError = "Unexpected 'endif'";
				goto LError;
			}
			cactIfLevel--;
			continue;
		}

		if (MATCH("foreach"))
		{
			if (ppropLoop)
			{
				strError = "Cannot nest 'foreach' loops";
				goto LError;
			}
			if (m_rgprop.size() <= 0)
			{
				// No properties so skip to the endfor.
				for (;;)
				{
					if (EOI(pch))
					{
						strError = "Missing 'endfor'";
						goto LError;
					}
					if (*pch != '$')
					{
						if (*pch++ == 0x0D)
						{
							if (pch < pchLim && 0x0A == *pch)
								pch++;
							pchLine = pch;
							nLine++;
						}
						continue;
					}
					pch++;
					if (MATCH("endfor"))
						break;
				}
				continue;
			}
			ppropLoop = m_rgprop.begin();
			cactIfLoop = cactIfLevel;
			pchLoop = pch;
			pchLineLoop = pchLine;
			nLineLoop = nLine;
			continue;
		}
		if (MATCH("endfor"))
		{
			if (!ppropLoop)
			{
				strError = "Unexpected 'endfor'";
				goto LError;
			}
			if (cactIfLoop != cactIfLevel)
			{
				strError = "Unbalanced 'if'/'endif' in 'foreach' loop";
				goto LError;
			}
			ppropLoop++;
			if (ppropLoop == m_rgprop.end())
			{
				ppropLoop = NULL;
				continue;
			}
			pch = pchLoop;
			pchLine = pchLineLoop;
			nLine = nLineLoop;
			continue;
		}

		strError = "Unexpected '$'";
		goto LError;
	}

	if (cactIfLevel)
	{
		strError = "Missing endif";
		goto LError;
	}
	if (ppropLoop)
	{
		strError = "Missing 'endfor'";
		goto LError;
	}

	return 0;

LError:
	char *pszFile;

	switch (rid)
	{
	case IDR_TMPL_SQL_CLASSES:
		pszFile = "tmpl_cls.sql";
		break;
	case IDR_TMPL_SQL_FIELDS:
		pszFile = "tmpl_fld.sql";
		break;
	case IDR_TMPL_DEFNS:
		pszFile = "tmpl_dfn.sqh";
		break;
	default:
		pszFile = "Unknown Template File";
		break;
	}

	Error(pszFile, nLine, pch - pchLine + 1, strError);
	return 1;
}

bool XmlParser::Parse(STR strIn)
{
	const int cchBuf = 1024;
	char rgch[cchBuf];
	bool fFinal;
	std::ifstream stmIn;

	char * pchFile;
	DWORD dw = SearchPath(g_strSearchPath.c_str(), strIn.c_str(), ".xml",
		sizeof(rgch) - 1, rgch, &pchFile);

	if (!dw)
	{
		Error(strIn.c_str(), 0, 0, STR("File not found."));
		return false;
	}

	m_strFile = rgch;

	stmIn.open(m_strFile.c_str());
	if (stmIn.bad())
	{
		std::cerr << "Error opening : " << m_strFile.c_str() << std::endl;
		return false;
	}

	m_pxmlp = XML_ParserCreate(NULL);
	if (!m_pxmlp)
	{
		std::cerr << "Creating XML Parser failed." << std::endl << std::endl;
		return false;
	}

	XML_SetUserData(m_pxmlp, this);
	SetStubs();

	do
	{
		stmIn.read(rgch, sizeof(rgch));
		int cch = stmIn.gcount();
		fFinal = cch < sizeof(rgch);
		if (!XML_Parse(m_pxmlp, rgch, cch, fFinal))
		{
			std::cerr
				<< m_strFile.c_str()
				<< "("
				<< XML_GetCurrentLineNumber(m_pxmlp)
				<< ") : error : "
				<< XML_ErrorString(XML_GetErrorCode(m_pxmlp))
				<< std::endl;
			return false;
		}
	} while (!fFinal);

	XML_ParserFree(m_pxmlp);
	m_pxmlp = NULL;

	return !m_fError;
}


void XmlParser::SetStubs(void)
{
	XML_SetElementHandler(m_pxmlp, StartElementStub, EndElementStub);
}


void XmlParser::ParseError(STR str)
{
	int nLine = 1;
	int nCol = 1;

	if (m_pxmlp)
	{
		nLine = XML_GetCurrentLineNumber(m_pxmlp);
		nCol = XML_GetCurrentColumnNumber(m_pxmlp);
	}
	Error(m_strFile, nLine, nCol, str);
}


void XmlParser::Error(STR strFile, int nLine, int nCol, STR str)
{
	if (strFile.size())
	{
		std::cerr << strFile.c_str();
		std::cerr << "(" << nLine << ":" << nCol << ") : ";
	}
	std::cerr << "error : ";
	std::cerr << str.c_str();
	std::cerr << std::endl;

	m_fError = true;
}


int XmlParser::IntFromStr(const char * psz)
{
	bool fNeg;
	int nT;

	fNeg = false;
	if (psz[0] == '-')
	{
		fNeg = true;
		psz++;
	}

	if (!psz[0])
		ParseError(STR("Expected Number"));

	nT = 0;
	while (psz[0])
	{
		if (psz[0] < '0' || psz[0] > '9')
			ParseError(STR("Digit expected: ") + psz);
		if (nT * 10 / 10 != nT)
			ParseError(STR("Numeric overflow: ") + psz);
		nT *= 10;
		nT += psz[0] - '0';
		if (nT < 0)
		{
			if (nT == knIntMin && !psz[0] && fNeg)
				break;
			ParseError(STR("Numeric overflow: ") + psz);
		}
		psz++;
	}

	if (fNeg)
		nT = -nT;

	return nT;
}
