/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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
