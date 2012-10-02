/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrpASTNode.hpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Customized tree node that contains line and file information.
-------------------------------------------------------------------------------*//*:End Ignore*/

#ifndef WRPASTNODE
#define WRPASTNODE

class GrpASTNode : public CommonASTNode
{
public:
	//	Additional instance variable:
	GrpLineAndFile m_lnf;

	//	And methods to handle it:
	GrpASTNode()
		: CommonASTNode()
	{
		// initialize instance variables
	}

	static ASTNode * factory()
	{
		return new GrpASTNode;
	}

	void initialize(RefToken t);

//	int getLine() { return m_lnf.Line(); }
//	void setLine(int n) { m_lnf.SetLine(n); }

	GrpLineAndFile LineAndFile()
	{
		return m_lnf;
	}

	//	Debugger:
	const char * debugString();
};


#endif // !WRPASTNODE
