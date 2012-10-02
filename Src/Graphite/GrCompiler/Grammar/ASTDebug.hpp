/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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
