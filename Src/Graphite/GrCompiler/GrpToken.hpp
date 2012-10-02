/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrpToken.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Customized lexer token.
-------------------------------------------------------------------------------*//*:End Ignore*/

#ifndef GRP_TOKEN
#define GRP_TOKEN

class GrpToken : public CommonToken
{
public:
	//	Additional instance variables:
	GrpLineAndFile m_lnf;

	//	And methods to handle it:
	GrpToken()
		: CommonToken()
	{
	}

	static RefToken factory()
	{
		return RefToken(new GrpToken);
	}

	virtual int getLine()
	{
		return m_lnf.PreProcessedLine();
	}

	virtual void setLine(int l)
	{
		CommonToken::setLine(l);
		m_lnf.SetPreProcessedLine(l);
	}

	GrpLineAndFile LineAndFile()
	{
		return m_lnf;
	}

	void SetOrigLineAndFile(int nOrig, StrAnsi sta)
	{
		m_lnf.SetOriginalLine(nOrig);
		m_lnf.SetFile(sta);
	}

//	void initialize(RefToken t)
//	{
//		CommonToken::initialize(t);
//		// add here
//	}
};


#endif // !GRP_TOKEN
