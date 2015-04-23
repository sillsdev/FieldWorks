/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrpParserDebug.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Additions to the auto-generated ANTLR stuff, for debugging.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Grp.h"
#include "GrPlatform.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

static char intString[20];

/*----------------------------------------------------------------------------------------------
	Print the syntax tree to the standard output.
----------------------------------------------------------------------------------------------*/
void AST::Trace(std::ostream & strmOut, const char * s, int level)
{
	for (int i = 0; i < level * 3; i++)
		strmOut << " ";

	if (s != 0)
		strmOut << s;

	GrpASTNode * wrNode = dynamic_cast<GrpASTNode *>(getNode());
	Assert(wrNode);

	strmOut << debugString() << "(" << getType() << ")";
	if (getText() != "")
		strmOut << ": '" << getText().c_str() << "'";
	int line = wrNode->LineAndFile().PreProcessedLine();
	if (line > 0)
		strmOut << "  [line #" << line << "]";
	strmOut << "\n";

	AST * pAST = getFirstChild();
	if (pAST)
		pAST->Trace(strmOut, 0, level + 1);
	pAST = getNextSibling();
	if (pAST)
		pAST->Trace(strmOut, 0, level);
}


/*----------------------------------------------------------------------------------------------
	Answer a string describing the tree's root node.
----------------------------------------------------------------------------------------------*/
const char * AST::debugString()
{
	if (this == NULL)
		return "NULL";

	GrpASTNode * wrNode = dynamic_cast<GrpASTNode *>(node);
	if (wrNode)
		return wrNode->debugString();
	else
		return "???";
}


/*----------------------------------------------------------------------------------------------
	Answer a string describing the node's token type.
----------------------------------------------------------------------------------------------*/
const char * GrpASTNode::debugString()
{
	if (this == NULL)
		return "NULL";

//	if (!ValidReadPtr(this))
//		return "corrupt";

	switch (getType())
	{
	case EOF_:					return "EOF";
	case NULL_TREE_LOOKAHEAD:	return "NULL_TREE_LOOKAHEAD";
	case OP_EQ:					return "OP_EQ";
	case OP_PLUSEQUAL:			return "OP_PLUSEQUAL";
	case OP_LPAREN:				return "OP_LPAREN";
	case OP_RPAREN:				return "OP_RPAREN";
	case OP_SEMI:				return "OP_SEMI";
	case LITERAL_environment:	return "LITERAL_environment";
	case LITERAL_endenvironment:	return "LITERAL_endenvironment";
	case OP_LBRACE:				return "OP_LBRACE";
	case IDENT:					return "IDENT";
	case LIT_INT:				return "LIT_INT";
	case OP_RBRACE:				return "OP_RBRACE";
	case LITERAL_table:			return "LITERAL_table";
	case LITERAL_endtable:		return "LITERAL_endtable";
	case LITERAL_name:			return "LITERAL_name";
	case OP_DOT:				return "OP_DOT";
	case LIT_STRING:			return "LIT_STRING";
//	case LIT_UNICHAR:			return "LIT_UNICHAR";
	case OP_COMMA:				return "OP_COMMA";
	case LITERAL_string:		return "LITERAL_string";
	case LITERAL_glyph:			return "LITERAL_glyph";
	case OP_LBRACKET:			return "OP_LBRACKET";
	case OP_RBRACKET:			return "OP_RBRACKET";
	case LITERAL_codepoint:		return "LITERAL_codepoint";
	case LIT_CHAR:				return "LIT_CHAR";
	case OP_DOTDOT:				return "OP_DOTDOT";
	case LITERAL_glyphid:		return "LITERAL_glyphid";
	case LITERAL_postscript:	return "LITERAL_postscript";
	case LITERAL_unicode:		return "LITERAL_unicode";
	case LITERAL_feature:		return "LITERAL_feature";
	case LITERAL_substitution :	return "LITERAL_substitution";
	case LITERAL_pass:			return "LITERAL_pass";
	case LITERAL_endpass:		return "LITERAL_endpass";
	case LITERAL_if:			return "LITERAL_if";
	case LITERAL_elseif:		return "LITERAL_elseif";
	case LITERAL_else:			return "LITERAL_else";
	case LITERAL_endif:			return "LITERAL_endif";
	case OP_GT:					return "OP_GT";
	case OP_DIV:				return "OP_DIV";
	case OP_UNDER:				return "OP_UNDER";
	case OP_QUESTION:			return "OP_QUESTION";
	case OP_AT:					return "OP_AT";
	case OP_COLON:				return "OP_COLON";
	case OP_DOLLAR:				return "OP_DOLLAR";
	case OP_CARET:				return "OP_CARET";
	case OP_HASH:				return "OP_HASH";
	case OP_EQUALEQUAL:			return "OP_EQUALEQUAL";
	case LITERAL_position:		return "LITERAL_position";
	case LITERAL_positioning:	return "LITERAL_positioning";
	case OP_MINUSEQUAL:			return "OP_MINUSEQUAL";
	case OP_DIVEQUAL:			return "OP_DIVEQUAL";
	case OP_MULTEQUAL:			return "OP_MULTEQUAL";
	case OP_PLUS:				return "OP_PLUS";
	case OP_MINUS:				return "OP_MINUS";
	case OP_MULT:				return "OP_MULT";
	case LITERAL_linebreak:		return "LITERAL_linebreak";
	case OP_NOT:				return "OP_NOT";
	case OP_AND:				return "OP_AND";
	case OP_OR:					return "OP_OR";
	case OP_LT:					return "OP_LT";
	case OP_LE:					return "OP_LE";
	case OP_GE:					return "OP_GE";
	case OP_NE:					return "OP_NE";
	case LITERAL_max:			return "LITERAL_max";
	case LITERAL_min:			return "LITERAL_min";
	case LITERAL_pseudo:		return "LITERAL_pseudo";
	case WS:					return "WS";
	case COMMENT_SL:			return "COMMENT_SL";
	case COMMENT_ML:			return "COMMENT_ML";
	case ESC:					return "ESC";
	case ODIGIT:				return "ODIGIT";
	case DIGIT:					return "DIGIT";
	case XDIGIT:				return "XDIGIT";
	case OP_BSLASH:				return "OP_BSLASH";
	case LITERAL_false:			return "LITERAL_false";
	case LITERAL_true:			return "LITERAL_true";
	case LITERAL_justification:	return "LITERAL_justification";
	case LITERAL_languages:		return "LITERAL_languages";
	case LITERAL_language:		return "LITERAL_language";

	case Zalias:				return "Zalias";
	case Zassocs:				return "Zassocs";
	case Zattrs:				return "Zattrs";
	case Zcluster:				return "Zcluster";
	case Zcodepage:				return "Zcodepage";
	case Zconstraint:			return "Zconstraint";
	case Zcontext:				return "Zcontext";
	case Zdirectives:			return "Zdirectives";
	case ZdotStruct:			return "ZdotStruct";
	case Zelseif:				return "Zelseif";
	case Zfeatures:				return "Zfeatures";
	case Zfunction:				return "Zfunction";
	case ZifStruct:				return "ZifStruct";
	case Zlhs:					return "Zlhs";
	case Zlookup:				return "Zlookup";
	case Zrhs:					return "Zrhs";
	case Zrule:					return "Zrule";
	case ZruleItem:				return "ZruleItem";
	case Zselector:				return "Zselector";
	case Ztop:					return "Ztop";

	default:
		itoa(getType(), intString, 10);
		return intString;
	}
}
