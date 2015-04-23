/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Grp.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Header file for the grammar classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GRAMMAR_H
#define GRAMMAR_H 1

////#include <cassert>
#ifdef GR_FW
namespace gr {
};
#include "Common.h"
#else
#include "GrCommon.h"
#endif

using namespace gr;

#include "UtilString.h"

#include "Antlr/AST.hpp"
#include "Antlr/CommonToken.hpp"
#include "Antlr/CommonASTNode.hpp"
#include "Antlr/TokenStream.hpp"
#include "Antlr/ScannerException.hpp"
#include "Antlr/ParserException.hpp"

#include "GrpLineAndFile.hpp"
#include "GrpParserTokenTypes.hpp"
#include "GrpToken.hpp"
#include "GrpASTNode.hpp"
#include "GrpTokenStreamFilter.hpp"
#include "GrpLexer.hpp"
#include "GrpParser.hpp"

void AddGlobalError(bool, int nID, std::string, int nLine);
void AddGlobalError(bool, int nID, std::string, GrpLineAndFile const&);


#endif // !GRAMMAR_H
