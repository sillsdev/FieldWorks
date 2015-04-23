/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ASTDebug.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Additional defines for the AST class, to be included in the middle of the class
	definition.
-------------------------------------------------------------------------------*//*:End Ignore*/
public:
	void Trace(std::ostream & strmOut, const char * s, int level);
	const char * debugString();
//	int line();
