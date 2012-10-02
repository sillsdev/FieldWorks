/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: Root.h
Responsibility: Shon Katzenberger
Last reviewed:

	ClassParser, etc.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef Root_H
#define Root_H 1

enum PropType
{
	kbtNil,

	// Basic property types.
	kbtMinBasic,

	kbtInteger = kbtMinBasic,
	kbtNumeric,
	kbtFloat,
	kbtBoolean,
	kbtTime,
	kbtString,
	kbtGuid,
	kbtImage,
	kbtGenDate,
	kbtBinary,

	kbtLimBasic,

	// Object referencing property types.
	kbtMinObject = kbtLimBasic,

	// Atomic.
	kbtMinAtom = kbtMinObject,
	kbtOwnAtom = kbtMinAtom,
	kbtRefAtom,
	kbtLimAtom,

	// Collection.
	kbtMinCol = kbtLimAtom,
	kbtOwnCol = kbtMinCol,
	kbtRefCol,
	kbtLimCol,

	// Sequence.
	kbtMinSeq = kbtLimCol,
	kbtOwnSeq = kbtMinSeq,
	kbtRefSeq,
	kbtLimSeq,

	kbtLimObject = kbtLimSeq
};

enum
{
	knIntMin = 0x80000000,
	knIntMax = 0x7FFFFFFF
};

struct Prop
{
	int m_bt;
	bool m_fMulti;
	bool m_fBig;
	bool m_fFmt;

	int m_flid;
	STR m_strName;
	STR m_strType;
	STR m_strPrefix;
	int m_nMin;
	int m_nMax;
	int m_nDef;
	int m_nPrec;  // Precision for numeric type.
	int m_nScale; // Scale for numeric type.

	void Init(void)
	{
		m_bt = kbtNil;
		m_fMulti = false;
		m_fBig = false;
		m_fFmt = false;

		m_flid = -1;
		m_strName = "";
		m_strType = "";
		m_strPrefix = "";
		m_nMin = knIntMin;
		m_nMax = knIntMax;
		m_nDef = 0;
		m_nPrec = 19;
		m_nScale = 0;
	}

	bool IsObject(void)
	{
		return m_bt >= kbtMinObject && m_bt < kbtLimObject;
	}
	bool IsAtom(void)
	{
		return m_bt >= kbtMinAtom && m_bt < kbtLimAtom;
	}
	bool IsCollection(void)
	{
		return m_bt >= kbtMinCol && m_bt < kbtLimCol;
	}
	bool IsSequence(void)
	{
		return m_bt >= kbtMinSeq && m_bt < kbtLimSeq;
	}
	bool IsOwning(void)
	{
		return !((m_bt - kbtMinObject) & 1) &&
			m_bt >= kbtMinObject && m_bt < kbtLimObject;
	}
	bool IsMultiString(void)
	{
		Assert(m_bt == kbtString || !m_fMulti);
		return m_fMulti;
	}
	bool IsFmtString(void)
	{
		Assert(m_bt == kbtString || !m_fFmt);
		return m_fFmt;
	}
	bool IsBigString(void)
	{
		Assert(m_bt == kbtString || !m_fBig);
		return m_fBig;
	}
};


class XmlParser
{
public:
	XmlParser(void)
	{
		m_fError = false;
	}

	bool Parse(STR strFileIn);
	virtual void SetStubs(void);
	bool HasError()
	{
		return m_fError;
	}

protected:
	bool m_fError;
	STR m_strFile;
	XML_Parser m_pxmlp;

	static void __cdecl StartElementStub(void *pvUser, const char *pszName,
		const char **prgpszAttr)
	{
		AssertPsz(pszName);
		XmlParser *pxp = reinterpret_cast<XmlParser *>(pvUser);
		AssertPtr(pxp);
		pxp->StartElement(STR(pszName), prgpszAttr);
	}
	static void __cdecl EndElementStub(void *pvUser, const char *pszName)
	{
		AssertPsz(pszName);
		XmlParser *pxp = reinterpret_cast<XmlParser *>(pvUser);
		AssertPtr(pxp);
		pxp->EndElement(STR(pszName));
	}

	void ParseError(STR str);
	void Error(STR strFile, int nLine, int nCol, STR str);
	int IntFromStr(const char * psz);

	virtual void StartElement(STR strElem, const char **prgpszAttr) = 0;
	virtual void EndElement(STR strElem) = 0;
};


// Attribute flags.
enum
{
	kfatNil         = 0x0000,
	kfatCmObject    = 0x0001,
	kfatOwning      = 0x0002,
	kfatVector      = 0x0004,
	kfatOrdered     = 0x0008,
};


class ClassParser : public XmlParser
{
public:
	ClassParser(void)
	{
		m_cps = kcpsNull;
		m_clid = -1;
		m_fAbstract = false;
	}

	int GenerateCode(bool fFirst, int rid, std::ostream & stmOut);
	int Clid()
	{
		return m_clid;
	}
	STR & Name()
	{
		return m_strName;
	}

protected:
	// ClassParserState enum.
	enum
	{
		kcpsNull,
		kcpsError,
		kcpsClass,
		kcpsAttributes,
		kcpsPostClass,
	};

	int m_cps;
	int m_clid;
	STR m_strName;
	STR m_strBase;
	STR m_strAbbr;
	bool m_fAbstract;
	std::vector<Prop> m_rgprop;

	// The current property being built.
	Prop m_propCur;

	virtual void StartElement(STR strElem, const char **prgpszAttr);
	virtual void EndElement(STR strElem);

	// The following method and friend declaration allow ClassParser::StartElement() and
	// ClassParser::EndElement() to be called from ModuleParser::StartElement() and
	// ModuleParser::EndElement().
	void SetParser(XML_Parser parser, STR strFile)
	{
		m_pxmlp = parser;
		m_strFile = strFile;
	}
	friend class ModuleParser;
};


class ModuleParser : public XmlParser
{
public:
	ModuleParser(void)
	{
		m_fParseClasses = false;
		m_mid = -1;
		m_ver = 0;
		m_verBack = 0;
		m_pcp = NULL;
	}

	int GenerateCode(std::ostream & stmSql, std::ostream & stmSqh);
	virtual void SetStubs(void)
	{
		XmlParser::SetStubs();
		XML_SetExternalEntityRefHandler(m_pxmlp, &ExternalEntityRefHandlerStub);
	}

	int Mid(void)
	{
		return m_mid;
	}
	int Ver(void)
	{
		return m_ver;
	}
	int VerBack(void)
	{
		return m_verBack;
	}
	STR & Name(void)
	{
		return m_strName;
	}

protected:
	bool m_fParseClasses;
	int m_mid;
	int m_ver;
	int m_verBack;
	STR m_strName;
	std::vector<ClassParser *> m_vpcp;

	// Allow embedded ClassParser when XML input is not split into one file per <class>.
	ClassParser * m_pcp;

	static int __cdecl ExternalEntityRefHandlerStub(XML_Parser pxmlp, const char * pszCtxt,
		const char * pszBase, const char * pszSys, const char * pszPub)
	{
		void * pvUser = XML_GetUserData(pxmlp);
		ModuleParser *pmop = reinterpret_cast<ModuleParser *>(pvUser);
		AssertPtr(pmop);
		return pmop->ExternalEntityRefHandler(pszSys);
	}

	virtual void StartElement(STR strElem, const char **prgpszAttr);
	virtual void EndElement(STR strElem);
	virtual bool ExternalEntityRefHandler(const char * pszFile);
};

#endif // !Root_H
