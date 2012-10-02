/*
 * ANTLR-generated file resulting from grammar c:\fw\src\graphite\grcompiler\grpparser.g
 *
 * Terence Parr, MageLang Institute
 * with John Lilley, Empathy Software
 * ANTLR Version 2.6.0; 1996-1999
 */

#include "GrpParser.hpp"
#include "GrpParserTokenTypes.hpp"
#include "antlr/NoViableAltException.hpp"
#include "antlr/SemanticException.hpp"

//	Insert at the beginning of the GrpParser.cpp file
#pragma warning(disable:4101)
#include "Grp.h"

//	This function needs to go in the .cpp file, not the .hpp file, after the
//	WprASTNode class is defined.
void GrpParser::init(GrpTokenStreamFilter & tsf)
{
	m_ptsf = &tsf;
	setASTNodeFactory(&GrpASTNode::factory);
}

void GrpParser::reportError(const ParserException& ex)
{
	//	Pipe the error back through the token stream filter, so it can supply the
	//	line-and-file information.
	m_ptsf->ReportParserError(ex);
}


GrpParser::GrpParser(TokenBuffer& tokenBuf, int k)
: LLkParser(tokenBuf,k)
{
	setTokenNames(_tokenNames);
}

GrpParser::GrpParser(TokenBuffer& tokenBuf)
: LLkParser(tokenBuf,3)
{
	setTokenNames(_tokenNames);
}

GrpParser::GrpParser(TokenStream& lexer, int k)
: LLkParser(lexer,k)
{
	setTokenNames(_tokenNames);
}

GrpParser::GrpParser(TokenStream& lexer)
: LLkParser(lexer,3)
{
	setTokenNames(_tokenNames);
}

GrpParser::GrpParser(const ParserSharedInputState& state)
: LLkParser(state,3)
{
	setTokenNames(_tokenNames);
}

void GrpParser::renderDescription() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST renderDescription_AST = nullAST;
	RefAST D_AST = nullAST;

	try {      // for error handling
		declarationList();
		D_AST = returnAST;
		RefAST tmp1_AST = nullAST;
		tmp1_AST = astFactory.create(LT(1));
		match(Token::EOF_TYPE);
		renderDescription_AST = currentAST.root;
		renderDescription_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Ztop))->add(D_AST));
		currentAST.root = renderDescription_AST;
		currentAST.child = renderDescription_AST!=nullAST &&renderDescription_AST->getFirstChild()!=nullAST ?
			renderDescription_AST->getFirstChild() : renderDescription_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_0);
	}
	returnAST = renderDescription_AST;
}

void GrpParser::declarationList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST declarationList_AST = nullAST;

	try {      // for error handling
		{
		do {
			switch ( LA(1)) {
			case IDENT:
			case LITERAL_position:
			{
				globalDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_environment:
			case LITERAL_table:
			{
				topDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop4;
			}
			}
		} while (true);
		_loop4:;
		}
		declarationList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_0);
	}
	returnAST = declarationList_AST;
}

void GrpParser::globalDecl() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST globalDecl_AST = nullAST;

	try {      // for error handling
		identDot();
		astFactory.addASTChild(currentAST, returnAST);
		{
		switch ( LA(1)) {
		case OP_EQ:
		{
			RefAST tmp2_AST = nullAST;
			tmp2_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp2_AST);
			match(OP_EQ);
			break;
		}
		case OP_PLUSEQUAL:
		{
			RefAST tmp3_AST = nullAST;
			tmp3_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp3_AST);
			match(OP_PLUSEQUAL);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		if ((LA(1)==OP_LPAREN) && (_tokenSet_1.member(LA(2))) && (_tokenSet_2.member(LA(3)))) {
			RefAST tmp4_AST = nullAST;
			tmp4_AST = astFactory.create(LT(1));
			match(OP_LPAREN);
			exprList();
			astFactory.addASTChild(currentAST, returnAST);
			RefAST tmp5_AST = nullAST;
			tmp5_AST = astFactory.create(LT(1));
			match(OP_RPAREN);
		}
		else if ((_tokenSet_1.member(LA(1))) && (_tokenSet_3.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			expr();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp6_AST = nullAST;
			tmp6_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case Token::EOF_TYPE:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_position:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		globalDecl_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_5);
	}
	returnAST = globalDecl_AST;
}

void GrpParser::topDecl() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST topDecl_AST = nullAST;

	try {      // for error handling
		switch ( LA(1)) {
		case LITERAL_environment:
		{
			topEnvironDecl();
			astFactory.addASTChild(currentAST, returnAST);
			topDecl_AST = currentAST.root;
			break;
		}
		case LITERAL_table:
		{
			tableDecl();
			astFactory.addASTChild(currentAST, returnAST);
			topDecl_AST = currentAST.root;
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_4);
	}
	returnAST = topDecl_AST;
}

void GrpParser::topEnvironDecl() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST topEnvironDecl_AST = nullAST;

	try {      // for error handling
		RefAST tmp7_AST = nullAST;
		tmp7_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp7_AST);
		match(LITERAL_environment);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_position:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp8_AST = nullAST;
			tmp8_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_position:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case LITERAL_environment:
			case LITERAL_table:
			{
				topDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case IDENT:
			case LITERAL_position:
			{
				globalDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop14;
			}
			}
		} while (true);
		_loop14:;
		}
		RefAST tmp9_AST = nullAST;
		tmp9_AST = astFactory.create(LT(1));
		match(LITERAL_endenvironment);
		{
		if ((LA(1)==OP_SEMI) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			RefAST tmp10_AST = nullAST;
			tmp10_AST = astFactory.create(LT(1));
			match(OP_SEMI);
		}
		else if ((_tokenSet_4.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		topEnvironDecl_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_4);
	}
	returnAST = topEnvironDecl_AST;
}

void GrpParser::tableDecl() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableDecl_AST = nullAST;

	try {      // for error handling
		RefAST tmp11_AST = nullAST;
		tmp11_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp11_AST);
		match(LITERAL_table);
		{
		if ((LA(1)==OP_LPAREN) && (LA(2)==LITERAL_name)) {
			tableName();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_LPAREN) && (LA(2)==LITERAL_glyph)) {
			tableGlyph();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_LPAREN) && (LA(2)==LITERAL_feature)) {
			tableFeature();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_LPAREN) && (LA(2)==LITERAL_language)) {
			tableLanguage();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_LPAREN) && (LA(2)==LITERAL_substitution)) {
			tableSub();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_LPAREN) && (LA(2)==LITERAL_justification)) {
			tableJust();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_LPAREN) && (LA(2)==LITERAL_position||LA(2)==LITERAL_positioning)) {
			tablePos();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_LPAREN) && (LA(2)==LITERAL_linebreak)) {
			tableLineBreak();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_LPAREN) && (LA(2)==IDENT)) {
			tableOther();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		RefAST tmp12_AST = nullAST;
		tmp12_AST = astFactory.create(LT(1));
		match(LITERAL_endtable);
		{
		if ((LA(1)==OP_SEMI) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			RefAST tmp13_AST = nullAST;
			tmp13_AST = astFactory.create(LT(1));
			match(OP_SEMI);
		}
		else if ((_tokenSet_4.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		tableDecl_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_4);
	}
	returnAST = tableDecl_AST;
}

void GrpParser::identDot() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST identDot_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==IDENT||LA(1)==LITERAL_position) && (LA(2)==OP_DOT) && (LA(3)==IDENT||LA(3)==LITERAL_position)) {
			{
			switch ( LA(1)) {
			case IDENT:
			{
				RefAST tmp14_AST = nullAST;
				tmp14_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp14_AST);
				match(IDENT);
				break;
			}
			case LITERAL_position:
			{
				RefAST tmp15_AST = nullAST;
				tmp15_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp15_AST);
				match(LITERAL_position);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			RefAST tmp16_AST = nullAST;
			tmp16_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp16_AST);
			match(OP_DOT);
			identDot();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==IDENT||LA(1)==LITERAL_position) && (_tokenSet_6.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			{
			switch ( LA(1)) {
			case IDENT:
			{
				RefAST tmp17_AST = nullAST;
				tmp17_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp17_AST);
				match(IDENT);
				break;
			}
			case LITERAL_position:
			{
				RefAST tmp18_AST = nullAST;
				tmp18_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp18_AST);
				match(LITERAL_position);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		identDot_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_6);
	}
	returnAST = identDot_AST;
}

void GrpParser::exprList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST exprList_AST = nullAST;

	try {      // for error handling
		expr();
		astFactory.addASTChild(currentAST, returnAST);
		{
		do {
			if ((LA(1)==OP_COMMA)) {
				RefAST tmp19_AST = nullAST;
				tmp19_AST = astFactory.create(LT(1));
				match(OP_COMMA);
				expr();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop369;
			}

		} while (true);
		_loop369:;
		}
		exprList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_7);
	}
	returnAST = exprList_AST;
}

void GrpParser::expr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST expr_AST = nullAST;

	try {      // for error handling
		conditionalExpr();
		astFactory.addASTChild(currentAST, returnAST);
		expr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_8);
	}
	returnAST = expr_AST;
}

void GrpParser::directives() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST directives_AST = nullAST;
	RefAST D_AST = nullAST;

	try {      // for error handling
		directiveList();
		D_AST = returnAST;
		directives_AST = currentAST.root;
		directives_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zdirectives))->add(D_AST));
		currentAST.root = directives_AST;
		currentAST.child = directives_AST!=nullAST &&directives_AST->getFirstChild()!=nullAST ?
			directives_AST->getFirstChild() : directives_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_9);
	}
	returnAST = directives_AST;
}

void GrpParser::directiveList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST directiveList_AST = nullAST;

	try {      // for error handling
		RefAST tmp20_AST = nullAST;
		tmp20_AST = astFactory.create(LT(1));
		match(OP_LBRACE);
		{
		switch ( LA(1)) {
		case IDENT:
		{
			directive();
			astFactory.addASTChild(currentAST, returnAST);
			{
			do {
				if ((LA(1)==OP_SEMI) && (LA(2)==IDENT) && (LA(3)==OP_EQ)) {
					RefAST tmp21_AST = nullAST;
					tmp21_AST = astFactory.create(LT(1));
					match(OP_SEMI);
					directive();
					astFactory.addASTChild(currentAST, returnAST);
				}
				else {
					goto _loop20;
				}

			} while (true);
			_loop20:;
			}
			{
			switch ( LA(1)) {
			case OP_SEMI:
			{
				RefAST tmp22_AST = nullAST;
				tmp22_AST = astFactory.create(LT(1));
				match(OP_SEMI);
				break;
			}
			case OP_RBRACE:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			break;
		}
		case OP_RBRACE:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp23_AST = nullAST;
		tmp23_AST = astFactory.create(LT(1));
		match(OP_RBRACE);
		directiveList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_9);
	}
	returnAST = directiveList_AST;
}

void GrpParser::directive() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST directive_AST = nullAST;

	try {      // for error handling
		RefAST tmp24_AST = nullAST;
		tmp24_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp24_AST);
		match(IDENT);
		RefAST tmp25_AST = nullAST;
		tmp25_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp25_AST);
		match(OP_EQ);
		expr();
		astFactory.addASTChild(currentAST, returnAST);
		directive_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_10);
	}
	returnAST = directive_AST;
}

void GrpParser::tableName() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableName_AST = nullAST;

	try {      // for error handling
		RefAST tmp26_AST = nullAST;
		tmp26_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp27_AST = nullAST;
		tmp27_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp27_AST);
		match(LITERAL_name);
		RefAST tmp28_AST = nullAST;
		tmp28_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp29_AST = nullAST;
			tmp29_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case LITERAL_environment:
			{
				nameEnv();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case IDENT:
			case LIT_INT:
			{
				nameSpecList();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_table:
			{
				tableDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop30;
			}
			}
		} while (true);
		_loop30:;
		}
		tableName_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tableName_AST;
}

void GrpParser::tableGlyph() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableGlyph_AST = nullAST;

	try {      // for error handling
		RefAST tmp30_AST = nullAST;
		tmp30_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp31_AST = nullAST;
		tmp31_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp31_AST);
		match(LITERAL_glyph);
		RefAST tmp32_AST = nullAST;
		tmp32_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp33_AST = nullAST;
			tmp33_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case LITERAL_environment:
			{
				glyphEnv();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case IDENT:
			{
				glyphEntry();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_table:
			{
				tableDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop64;
			}
			}
		} while (true);
		_loop64:;
		}
		tableGlyph_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tableGlyph_AST;
}

void GrpParser::tableFeature() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableFeature_AST = nullAST;

	try {      // for error handling
		RefAST tmp34_AST = nullAST;
		tmp34_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp35_AST = nullAST;
		tmp35_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp35_AST);
		match(LITERAL_feature);
		RefAST tmp36_AST = nullAST;
		tmp36_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_name:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp37_AST = nullAST;
			tmp37_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_name:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case LITERAL_environment:
			{
				featureEnv();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case IDENT:
			case LITERAL_name:
			case LIT_INT:
			{
				featureSpecList();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_table:
			{
				tableDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop129;
			}
			}
		} while (true);
		_loop129:;
		}
		tableFeature_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tableFeature_AST;
}

void GrpParser::tableLanguage() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableLanguage_AST = nullAST;

	try {      // for error handling
		RefAST tmp38_AST = nullAST;
		tmp38_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp39_AST = nullAST;
		tmp39_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp39_AST);
		match(LITERAL_language);
		RefAST tmp40_AST = nullAST;
		tmp40_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp41_AST = nullAST;
			tmp41_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case LITERAL_environment:
			{
				languageEnv();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case IDENT:
			{
				languageSpecList();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_table:
			{
				tableDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop156;
			}
			}
		} while (true);
		_loop156:;
		}
		tableLanguage_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tableLanguage_AST;
}

void GrpParser::tableSub() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableSub_AST = nullAST;

	try {      // for error handling
		RefAST tmp42_AST = nullAST;
		tmp42_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp43_AST = nullAST;
		tmp43_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp43_AST);
		match(LITERAL_substitution);
		RefAST tmp44_AST = nullAST;
		tmp44_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp45_AST = nullAST;
			tmp45_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				subEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop187;
			}

		} while (true);
		_loop187:;
		}
		tableSub_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tableSub_AST;
}

void GrpParser::tableJust() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableJust_AST = nullAST;

	try {      // for error handling
		RefAST tmp46_AST = nullAST;
		tmp46_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp47_AST = nullAST;
		tmp47_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp47_AST);
		match(LITERAL_justification);
		RefAST tmp48_AST = nullAST;
		tmp48_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp49_AST = nullAST;
			tmp49_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				subEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop269;
			}

		} while (true);
		_loop269:;
		}
		tableJust_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tableJust_AST;
}

void GrpParser::tablePos() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tablePos_AST = nullAST;

	try {      // for error handling
		RefAST tmp50_AST = nullAST;
		tmp50_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		{
		switch ( LA(1)) {
		case LITERAL_position:
		{
			RefAST tmp51_AST = nullAST;
			tmp51_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp51_AST);
			match(LITERAL_position);
			break;
		}
		case LITERAL_positioning:
		{
			RefAST tmp52_AST = nullAST;
			tmp52_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp52_AST);
			match(LITERAL_positioning);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp53_AST = nullAST;
		tmp53_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp54_AST = nullAST;
			tmp54_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				posEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop275;
			}

		} while (true);
		_loop275:;
		}
		tablePos_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tablePos_AST;
}

void GrpParser::tableLineBreak() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableLineBreak_AST = nullAST;

	try {      // for error handling
		RefAST tmp55_AST = nullAST;
		tmp55_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp56_AST = nullAST;
		tmp56_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp56_AST);
		match(LITERAL_linebreak);
		RefAST tmp57_AST = nullAST;
		tmp57_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp58_AST = nullAST;
			tmp58_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				posEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop322;
			}

		} while (true);
		_loop322:;
		}
		tableLineBreak_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tableLineBreak_AST;
}

void GrpParser::tableOther() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST tableOther_AST = nullAST;

	try {      // for error handling
		RefAST tmp59_AST = nullAST;
		tmp59_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp60_AST = nullAST;
		tmp60_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp60_AST);
		match(IDENT);
		RefAST tmp61_AST = nullAST;
		tmp61_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		if ((LA(1)==OP_LBRACE) && (LA(2)==OP_RBRACE||LA(2)==IDENT) && ((LA(3) >= OP_EQ && LA(3) <= AT_IDENT))) {
			directives();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if (((LA(1) >= OP_EQ && LA(1) <= AT_IDENT)) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		{
		if ((LA(1)==OP_SEMI) && ((LA(2) >= OP_EQ && LA(2) <= AT_IDENT)) && (_tokenSet_4.member(LA(3)))) {
			RefAST tmp62_AST = nullAST;
			tmp62_AST = astFactory.create(LT(1));
			match(OP_SEMI);
		}
		else if (((LA(1) >= OP_EQ && LA(1) <= AT_IDENT)) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		{
		do {
			if ((_tokenSet_13.member(LA(1)))) {
				otherEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop344;
			}

		} while (true);
		_loop344:;
		}
		tableOther_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_11);
	}
	returnAST = tableOther_AST;
}

void GrpParser::nameEnv() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST nameEnv_AST = nullAST;

	try {      // for error handling
		RefAST tmp63_AST = nullAST;
		tmp63_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp63_AST);
		match(LITERAL_environment);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp64_AST = nullAST;
			tmp64_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case IDENT:
			case LIT_INT:
			{
				nameSpecList();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_environment:
			{
				nameEnv();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_table:
			{
				tableDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop35;
			}
			}
		} while (true);
		_loop35:;
		}
		RefAST tmp65_AST = nullAST;
		tmp65_AST = astFactory.create(LT(1));
		match(LITERAL_endenvironment);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp66_AST = nullAST;
			tmp66_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		nameEnv_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_14);
	}
	returnAST = nameEnv_AST;
}

void GrpParser::nameSpecList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST nameSpecList_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==IDENT||LA(1)==LIT_INT) && (LA(2)==OP_LBRACE)) {
			nameSpecStruct();
			astFactory.addASTChild(currentAST, returnAST);
			{
			if ((LA(1)==IDENT||LA(1)==LIT_INT) && (_tokenSet_15.member(LA(2))) && (_tokenSet_16.member(LA(3)))) {
				nameSpecList();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else if ((_tokenSet_17.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
		}
		else if ((LA(1)==IDENT||LA(1)==LIT_INT) && (LA(2)==OP_EQ||LA(2)==OP_DOT||LA(2)==OP_PLUS_EQUAL)) {
			nameSpecFlat();
			astFactory.addASTChild(currentAST, returnAST);
			{
			if ((LA(1)==OP_SEMI) && (LA(2)==IDENT||LA(2)==LIT_INT) && (_tokenSet_15.member(LA(3)))) {
				RefAST tmp67_AST = nullAST;
				tmp67_AST = astFactory.create(LT(1));
				match(OP_SEMI);
				nameSpecList();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else if ((_tokenSet_17.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			{
			if ((LA(1)==OP_SEMI) && (_tokenSet_17.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
				RefAST tmp68_AST = nullAST;
				tmp68_AST = astFactory.create(LT(1));
				match(OP_SEMI);
			}
			else if ((_tokenSet_17.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		nameSpecList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_17);
	}
	returnAST = nameSpecList_AST;
}

void GrpParser::nameSpecStruct() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST nameSpecStruct_AST = nullAST;
	RefToken  N = nullToken;
	RefAST N_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LIT_INT:
		{
			N = LT(1);
			N_AST = astFactory.create(N);
			match(LIT_INT);
			break;
		}
		case IDENT:
		{
			I = LT(1);
			I_AST = astFactory.create(I);
			match(IDENT);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp69_AST = nullAST;
		tmp69_AST = astFactory.create(LT(1));
		match(OP_LBRACE);
		{
		switch ( LA(1)) {
		case IDENT:
		case LIT_INT:
		{
			nameSpecList();
			X_AST = returnAST;
			break;
		}
		case OP_RBRACE:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp70_AST = nullAST;
		tmp70_AST = astFactory.create(LT(1));
		match(OP_RBRACE);
		{
		if ((LA(1)==OP_SEMI) && (_tokenSet_17.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			RefAST tmp71_AST = nullAST;
			tmp71_AST = astFactory.create(LT(1));
			match(OP_SEMI);
		}
		else if ((_tokenSet_17.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		nameSpecStruct_AST = currentAST.root;
		nameSpecStruct_AST = astFactory.make( (new ASTArray(4))->add(astFactory.create(ZdotStruct))->add(N_AST)->add(I_AST)->add(X_AST));
		currentAST.root = nameSpecStruct_AST;
		currentAST.child = nameSpecStruct_AST!=nullAST &&nameSpecStruct_AST->getFirstChild()!=nullAST ?
			nameSpecStruct_AST->getFirstChild() : nameSpecStruct_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_17);
	}
	returnAST = nameSpecStruct_AST;
}

void GrpParser::nameSpecFlat() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST nameSpecFlat_AST = nullAST;
	RefToken  N = nullToken;
	RefAST N_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;
	RefToken  D = nullToken;
	RefAST D_AST = nullAST;
	RefAST X1_AST = nullAST;
	RefAST X2_AST = nullAST;
	RefToken  E1 = nullToken;
	RefAST E1_AST = nullAST;
	RefToken  E2 = nullToken;
	RefAST E2_AST = nullAST;
	RefAST Vi1_AST = nullAST;
	RefAST Vi2_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LIT_INT:
		{
			N = LT(1);
			N_AST = astFactory.create(N);
			match(LIT_INT);
			break;
		}
		case IDENT:
		{
			I = LT(1);
			I_AST = astFactory.create(I);
			match(IDENT);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_DOT:
		{
			D = LT(1);
			D_AST = astFactory.create(D);
			match(OP_DOT);
			{
			if ((LA(1)==IDENT||LA(1)==LIT_INT) && (LA(2)==OP_EQ||LA(2)==OP_DOT||LA(2)==OP_PLUS_EQUAL)) {
				nameSpecFlat();
				X1_AST = returnAST;
			}
			else if ((LA(1)==IDENT||LA(1)==LIT_INT) && (LA(2)==OP_LBRACE)) {
				nameSpecStruct();
				X2_AST = returnAST;
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			nameSpecFlat_AST = currentAST.root;
			nameSpecFlat_AST = astFactory.make( (new ASTArray(5))->add(D_AST)->add(N_AST)->add(I_AST)->add(X1_AST)->add(X2_AST));
			currentAST.root = nameSpecFlat_AST;
			currentAST.child = nameSpecFlat_AST!=nullAST &&nameSpecFlat_AST->getFirstChild()!=nullAST ?
				nameSpecFlat_AST->getFirstChild() : nameSpecFlat_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case OP_EQ:
		case OP_PLUS_EQUAL:
		{
			{
			switch ( LA(1)) {
			case OP_EQ:
			{
				E1 = LT(1);
				E1_AST = astFactory.create(E1);
				match(OP_EQ);
				break;
			}
			case OP_PLUS_EQUAL:
			{
				E2 = LT(1);
				E2_AST = astFactory.create(E2);
				match(OP_PLUS_EQUAL);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			{
			switch ( LA(1)) {
			case LIT_INT:
			case OP_PLUS:
			case OP_MINUS:
			case LITERAL_true:
			case LITERAL_false:
			{
				signedInt();
				Vi1_AST = returnAST;
				break;
			}
			case OP_LPAREN:
			case LIT_STRING:
			case LITERAL_string:
			{
				stringDefn();
				Vi2_AST = returnAST;
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			nameSpecFlat_AST = currentAST.root;
			nameSpecFlat_AST = astFactory.make( (new ASTArray(6))->add(E1_AST)->add(E2_AST)->add(N_AST)->add(I_AST)->add(Vi1_AST)->add(Vi2_AST));
			currentAST.root = nameSpecFlat_AST;
			currentAST.child = nameSpecFlat_AST!=nullAST &&nameSpecFlat_AST->getFirstChild()!=nullAST ?
				nameSpecFlat_AST->getFirstChild() : nameSpecFlat_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_17);
	}
	returnAST = nameSpecFlat_AST;
}

void GrpParser::signedInt() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST signedInt_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LITERAL_true:
		{
			RefAST tmp72_AST = nullAST;
			tmp72_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp72_AST);
			match(LITERAL_true);
			break;
		}
		case LITERAL_false:
		{
			RefAST tmp73_AST = nullAST;
			tmp73_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp73_AST);
			match(LITERAL_false);
			break;
		}
		case LIT_INT:
		case OP_PLUS:
		case OP_MINUS:
		{
			{
			switch ( LA(1)) {
			case OP_PLUS:
			{
				RefAST tmp74_AST = nullAST;
				tmp74_AST = astFactory.create(LT(1));
				match(OP_PLUS);
				break;
			}
			case OP_MINUS:
			{
				RefAST tmp75_AST = nullAST;
				tmp75_AST = astFactory.create(LT(1));
				astFactory.makeASTRoot(currentAST, tmp75_AST);
				match(OP_MINUS);
				break;
			}
			case LIT_INT:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			RefAST tmp76_AST = nullAST;
			tmp76_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp76_AST);
			match(LIT_INT);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		signedInt_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_18);
	}
	returnAST = signedInt_AST;
}

void GrpParser::stringDefn() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST stringDefn_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LIT_STRING:
		{
			RefAST tmp77_AST = nullAST;
			tmp77_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp77_AST);
			match(LIT_STRING);
			break;
		}
		case LITERAL_string:
		{
			stringFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		{
			{
			RefAST tmp78_AST = nullAST;
			tmp78_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp78_AST);
			match(OP_LPAREN);
			stringDefn();
			astFactory.addASTChild(currentAST, returnAST);
			{
			do {
				if ((_tokenSet_19.member(LA(1)))) {
					{
					switch ( LA(1)) {
					case OP_COMMA:
					{
						RefAST tmp79_AST = nullAST;
						tmp79_AST = astFactory.create(LT(1));
						astFactory.addASTChild(currentAST, tmp79_AST);
						match(OP_COMMA);
						break;
					}
					case OP_LPAREN:
					case LIT_STRING:
					case LITERAL_string:
					{
						break;
					}
					default:
					{
						throw NoViableAltException(LT(1));
					}
					}
					}
					stringDefn();
					astFactory.addASTChild(currentAST, returnAST);
				}
				else {
					goto _loop57;
				}

			} while (true);
			_loop57:;
			}
			RefAST tmp80_AST = nullAST;
			tmp80_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp80_AST);
			match(OP_RPAREN);
			}
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		stringDefn_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_20);
	}
	returnAST = stringDefn_AST;
}

void GrpParser::stringFunc() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST stringFunc_AST = nullAST;

	try {      // for error handling
		RefAST tmp81_AST = nullAST;
		tmp81_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp81_AST);
		match(LITERAL_string);
		RefAST tmp82_AST = nullAST;
		tmp82_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp83_AST = nullAST;
		tmp83_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp83_AST);
		match(LIT_STRING);
		{
		switch ( LA(1)) {
		case OP_COMMA:
		{
			RefAST tmp84_AST = nullAST;
			tmp84_AST = astFactory.create(LT(1));
			match(OP_COMMA);
			RefAST tmp85_AST = nullAST;
			tmp85_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp85_AST);
			match(LIT_INT);
			break;
		}
		case OP_RPAREN:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp86_AST = nullAST;
		tmp86_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		stringFunc_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_20);
	}
	returnAST = stringFunc_AST;
}

void GrpParser::glyphEnv() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST glyphEnv_AST = nullAST;

	try {      // for error handling
		RefAST tmp87_AST = nullAST;
		tmp87_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp87_AST);
		match(LITERAL_environment);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp88_AST = nullAST;
			tmp88_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case IDENT:
			{
				glyphEntry();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_environment:
			{
				glyphEnv();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_table:
			{
				tableDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop69;
			}
			}
		} while (true);
		_loop69:;
		}
		RefAST tmp89_AST = nullAST;
		tmp89_AST = astFactory.create(LT(1));
		match(LITERAL_endenvironment);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp90_AST = nullAST;
			tmp90_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		glyphEnv_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_21);
	}
	returnAST = glyphEnv_AST;
}

void GrpParser::glyphEntry() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST glyphEntry_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==IDENT) && (LA(2)==OP_EQ||LA(2)==OP_PLUSEQUAL)) {
			glyphContents();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==IDENT) && (LA(2)==OP_LBRACE||LA(2)==OP_DOT)) {
			glyphAttrs();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp91_AST = nullAST;
			tmp91_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		glyphEntry_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_21);
	}
	returnAST = glyphEntry_AST;
}

void GrpParser::glyphContents() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST glyphContents_AST = nullAST;

	try {      // for error handling
		RefAST tmp92_AST = nullAST;
		tmp92_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp92_AST);
		match(IDENT);
		{
		{
		switch ( LA(1)) {
		case OP_EQ:
		{
			RefAST tmp93_AST = nullAST;
			tmp93_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp93_AST);
			match(OP_EQ);
			break;
		}
		case OP_PLUSEQUAL:
		{
			RefAST tmp94_AST = nullAST;
			tmp94_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp94_AST);
			match(OP_PLUSEQUAL);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		glyphSpec();
		astFactory.addASTChild(currentAST, returnAST);
		}
		{
		if ((_tokenSet_22.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			attributes();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((_tokenSet_23.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		glyphContents_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_23);
	}
	returnAST = glyphContents_AST;
}

void GrpParser::glyphAttrs() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST glyphAttrs_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;
	RefAST A_AST = nullAST;
	RefAST X1_AST = nullAST;
	RefAST X2_AST = nullAST;
	RefAST X3_AST = nullAST;

	try {      // for error handling
		I = LT(1);
		I_AST = astFactory.create(I);
		match(IDENT);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			RefAST tmp95_AST = nullAST;
			tmp95_AST = astFactory.create(LT(1));
			match(OP_LBRACE);
			{
			switch ( LA(1)) {
			case IDENT:
			case LIT_INT:
			{
				attrItemList();
				X1_AST = returnAST;
				break;
			}
			case OP_RBRACE:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			RefAST tmp96_AST = nullAST;
			tmp96_AST = astFactory.create(LT(1));
			match(OP_RBRACE);
			A_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zattrs))->add(X1_AST));
			break;
		}
		case OP_DOT:
		{
			RefAST tmp97_AST = nullAST;
			tmp97_AST = astFactory.create(LT(1));
			match(OP_DOT);
			{
			if ((LA(1)==IDENT||LA(1)==LIT_INT) && (_tokenSet_24.member(LA(2)))) {
				attrItemFlat();
				X2_AST = returnAST;
			}
			else if ((LA(1)==IDENT||LA(1)==LIT_INT) && (LA(2)==OP_LBRACE)) {
				attrItemStruct();
				X3_AST = returnAST;
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			A_AST = astFactory.make( (new ASTArray(3))->add(astFactory.create(Zattrs))->add(X2_AST)->add(X3_AST));
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		glyphAttrs_AST = currentAST.root;
		glyphAttrs_AST = astFactory.make( (new ASTArray(3))->add(astFactory.create(OP_PLUSEQUAL))->add(I_AST)->add(A_AST));
		currentAST.root = glyphAttrs_AST;
		currentAST.child = glyphAttrs_AST!=nullAST &&glyphAttrs_AST->getFirstChild()!=nullAST ?
			glyphAttrs_AST->getFirstChild() : glyphAttrs_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_23);
	}
	returnAST = glyphAttrs_AST;
}

void GrpParser::glyphSpec() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST glyphSpec_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case IDENT:
		{
			RefAST tmp98_AST = nullAST;
			tmp98_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp98_AST);
			match(IDENT);
			break;
		}
		case LITERAL_codepoint:
		{
			codepointFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_glyphid:
		{
			glyphidFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_postscript:
		{
			postscriptFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_unicode:
		{
			unicodeFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LIT_UHEX:
		{
			unicodeCodepoint();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_pseudo:
		{
			pseudoFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		{
			{
			RefAST tmp99_AST = nullAST;
			tmp99_AST = astFactory.create(LT(1));
			match(OP_LPAREN);
			{
			switch ( LA(1)) {
			case OP_LPAREN:
			case IDENT:
			case LITERAL_pseudo:
			case LIT_UHEX:
			case LITERAL_codepoint:
			case LITERAL_glyphid:
			case LITERAL_postscript:
			case LITERAL_unicode:
			{
				glyphSpec();
				astFactory.addASTChild(currentAST, returnAST);
				{
				do {
					if ((_tokenSet_25.member(LA(1)))) {
						{
						switch ( LA(1)) {
						case OP_COMMA:
						{
							RefAST tmp100_AST = nullAST;
							tmp100_AST = astFactory.create(LT(1));
							match(OP_COMMA);
							break;
						}
						case OP_LPAREN:
						case IDENT:
						case LITERAL_pseudo:
						case LIT_UHEX:
						case LITERAL_codepoint:
						case LITERAL_glyphid:
						case LITERAL_postscript:
						case LITERAL_unicode:
						{
							break;
						}
						default:
						{
							throw NoViableAltException(LT(1));
						}
						}
						}
						glyphSpec();
						astFactory.addASTChild(currentAST, returnAST);
					}
					else {
						goto _loop88;
					}

				} while (true);
				_loop88:;
				}
				break;
			}
			case OP_RPAREN:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			RefAST tmp101_AST = nullAST;
			tmp101_AST = astFactory.create(LT(1));
			match(OP_RPAREN);
			}
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		glyphSpec_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_26);
	}
	returnAST = glyphSpec_AST;
}

void GrpParser::attributes() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST attributes_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			RefAST tmp102_AST = nullAST;
			tmp102_AST = astFactory.create(LT(1));
			match(OP_LBRACE);
			{
			switch ( LA(1)) {
			case IDENT:
			case LIT_INT:
			{
				attrItemList();
				X_AST = returnAST;
				break;
			}
			case OP_SEMI:
			case OP_RBRACE:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			{
			switch ( LA(1)) {
			case OP_SEMI:
			{
				RefAST tmp103_AST = nullAST;
				tmp103_AST = astFactory.create(LT(1));
				match(OP_SEMI);
				break;
			}
			case OP_RBRACE:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			RefAST tmp104_AST = nullAST;
			tmp104_AST = astFactory.create(LT(1));
			match(OP_RBRACE);
			attributes_AST = currentAST.root;
			attributes_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zattrs))->add(X_AST));
			currentAST.root = attributes_AST;
			currentAST.child = attributes_AST!=nullAST &&attributes_AST->getFirstChild()!=nullAST ?
				attributes_AST->getFirstChild() : attributes_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_GT:
		case OP_DIV:
		case OP_QUESTION:
		case OP_LBRACKET:
		case OP_RBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_27);
	}
	returnAST = attributes_AST;
}

void GrpParser::attrItemList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST attrItemList_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==IDENT||LA(1)==LIT_INT) && (LA(2)==OP_LBRACE)) {
			attrItemStruct();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==IDENT||LA(1)==LIT_INT) && (_tokenSet_24.member(LA(2)))) {
			attrItemFlat();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		{
		if ((LA(1)==OP_SEMI) && (LA(2)==IDENT||LA(2)==LIT_INT) && (_tokenSet_28.member(LA(3)))) {
			RefAST tmp105_AST = nullAST;
			tmp105_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			attrItemList();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else if ((LA(1)==OP_SEMI||LA(1)==OP_RBRACE) && (_tokenSet_29.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		attrItemList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_10);
	}
	returnAST = attrItemList_AST;
}

void GrpParser::attrItemFlat() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST attrItemFlat_AST = nullAST;
	RefToken  I1 = nullToken;
	RefAST I1_AST = nullAST;
	RefToken  I2 = nullToken;
	RefAST I2_AST = nullAST;
	RefToken  D = nullToken;
	RefAST D_AST = nullAST;
	RefAST X1_AST = nullAST;
	RefAST X2_AST = nullAST;
	RefAST E_AST = nullAST;
	RefAST V1_AST = nullAST;
	RefAST V2_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case IDENT:
		{
			I1 = LT(1);
			I1_AST = astFactory.create(I1);
			match(IDENT);
			break;
		}
		case LIT_INT:
		{
			I2 = LT(1);
			I2_AST = astFactory.create(I2);
			match(LIT_INT);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_DOT:
		{
			D = LT(1);
			D_AST = astFactory.create(D);
			match(OP_DOT);
			{
			if ((LA(1)==IDENT||LA(1)==LIT_INT) && (_tokenSet_24.member(LA(2)))) {
				attrItemFlat();
				X1_AST = returnAST;
			}
			else if ((LA(1)==IDENT||LA(1)==LIT_INT) && (LA(2)==OP_LBRACE)) {
				attrItemStruct();
				X2_AST = returnAST;
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			attrItemFlat_AST = currentAST.root;
			attrItemFlat_AST = astFactory.make( (new ASTArray(5))->add(D_AST)->add(I1_AST)->add(I2_AST)->add(X1_AST)->add(X2_AST));
			currentAST.root = attrItemFlat_AST;
			currentAST.child = attrItemFlat_AST!=nullAST &&attrItemFlat_AST->getFirstChild()!=nullAST ?
				attrItemFlat_AST->getFirstChild() : attrItemFlat_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case OP_EQ:
		case OP_PLUSEQUAL:
		case OP_MINUSEQUAL:
		case OP_DIVEQUAL:
		case OP_MULTEQUAL:
		{
			attrAssignOp();
			E_AST = returnAST;
			{
			if ((LA(1)==IDENT) && (LA(2)==OP_LPAREN) && (_tokenSet_30.member(LA(3)))) {
				function();
				V1_AST = returnAST;
			}
			else if ((_tokenSet_1.member(LA(1))) && (_tokenSet_31.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
				expr();
				V2_AST = returnAST;
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			attrItemFlat_AST = currentAST.root;
			attrItemFlat_AST = astFactory.make( (new ASTArray(5))->add(E_AST)->add(I1_AST)->add(I2_AST)->add(V1_AST)->add(V2_AST));
			currentAST.root = attrItemFlat_AST;
			currentAST.child = attrItemFlat_AST!=nullAST &&attrItemFlat_AST->getFirstChild()!=nullAST ?
				attrItemFlat_AST->getFirstChild() : attrItemFlat_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_32);
	}
	returnAST = attrItemFlat_AST;
}

void GrpParser::attrItemStruct() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST attrItemStruct_AST = nullAST;
	RefToken  I1 = nullToken;
	RefAST I1_AST = nullAST;
	RefToken  I2 = nullToken;
	RefAST I2_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case IDENT:
		{
			I1 = LT(1);
			I1_AST = astFactory.create(I1);
			match(IDENT);
			break;
		}
		case LIT_INT:
		{
			I2 = LT(1);
			I2_AST = astFactory.create(I2);
			match(LIT_INT);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp106_AST = nullAST;
		tmp106_AST = astFactory.create(LT(1));
		match(OP_LBRACE);
		{
		switch ( LA(1)) {
		case IDENT:
		case LIT_INT:
		{
			attrItemList();
			X_AST = returnAST;
			break;
		}
		case OP_SEMI:
		case OP_RBRACE:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp107_AST = nullAST;
			tmp107_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_RBRACE:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp108_AST = nullAST;
		tmp108_AST = astFactory.create(LT(1));
		match(OP_RBRACE);
		attrItemStruct_AST = currentAST.root;
		attrItemStruct_AST = astFactory.make( (new ASTArray(4))->add(astFactory.create(ZdotStruct))->add(I1_AST)->add(I2_AST)->add(X_AST));
		currentAST.root = attrItemStruct_AST;
		currentAST.child = attrItemStruct_AST!=nullAST &&attrItemStruct_AST->getFirstChild()!=nullAST ?
			attrItemStruct_AST->getFirstChild() : attrItemStruct_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_32);
	}
	returnAST = attrItemStruct_AST;
}

void GrpParser::codepointFunc() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST codepointFunc_AST = nullAST;
	RefToken  F = nullToken;
	RefAST F_AST = nullAST;
	RefAST X_AST = nullAST;
	RefAST C_AST = nullAST;
	RefToken  N = nullToken;
	RefAST N_AST = nullAST;

	try {      // for error handling
		F = LT(1);
		F_AST = astFactory.create(F);
		match(LITERAL_codepoint);
		RefAST tmp109_AST = nullAST;
		tmp109_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		codepointList();
		X_AST = returnAST;
		{
		switch ( LA(1)) {
		case OP_COMMA:
		{
			RefAST tmp110_AST = nullAST;
			tmp110_AST = astFactory.create(LT(1));
			match(OP_COMMA);
			N = LT(1);
			N_AST = astFactory.create(N);
			match(LIT_INT);
			C_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zcodepage))->add(N_AST));
			break;
		}
		case OP_RPAREN:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp111_AST = nullAST;
		tmp111_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		codepointFunc_AST = currentAST.root;
		codepointFunc_AST = astFactory.make( (new ASTArray(3))->add(F_AST)->add(C_AST)->add(X_AST));
		currentAST.root = codepointFunc_AST;
		currentAST.child = codepointFunc_AST!=nullAST &&codepointFunc_AST->getFirstChild()!=nullAST ?
			codepointFunc_AST->getFirstChild() : codepointFunc_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_33);
	}
	returnAST = codepointFunc_AST;
}

void GrpParser::glyphidFunc() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST glyphidFunc_AST = nullAST;

	try {      // for error handling
		RefAST tmp112_AST = nullAST;
		tmp112_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp112_AST);
		match(LITERAL_glyphid);
		RefAST tmp113_AST = nullAST;
		tmp113_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		intOrRange();
		astFactory.addASTChild(currentAST, returnAST);
		{
		do {
			if ((LA(1)==LIT_INT||LA(1)==OP_COMMA)) {
				{
				switch ( LA(1)) {
				case OP_COMMA:
				{
					RefAST tmp114_AST = nullAST;
					tmp114_AST = astFactory.create(LT(1));
					match(OP_COMMA);
					break;
				}
				case LIT_INT:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				intOrRange();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop107;
			}

		} while (true);
		_loop107:;
		}
		RefAST tmp115_AST = nullAST;
		tmp115_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		glyphidFunc_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_33);
	}
	returnAST = glyphidFunc_AST;
}

void GrpParser::postscriptFunc() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST postscriptFunc_AST = nullAST;

	try {      // for error handling
		RefAST tmp116_AST = nullAST;
		tmp116_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp116_AST);
		match(LITERAL_postscript);
		RefAST tmp117_AST = nullAST;
		tmp117_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp118_AST = nullAST;
		tmp118_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp118_AST);
		match(LIT_STRING);
		{
		do {
			if ((LA(1)==LIT_STRING||LA(1)==OP_COMMA)) {
				{
				switch ( LA(1)) {
				case OP_COMMA:
				{
					RefAST tmp119_AST = nullAST;
					tmp119_AST = astFactory.create(LT(1));
					match(OP_COMMA);
					break;
				}
				case LIT_STRING:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				RefAST tmp120_AST = nullAST;
				tmp120_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp120_AST);
				match(LIT_STRING);
			}
			else {
				goto _loop111;
			}

		} while (true);
		_loop111:;
		}
		RefAST tmp121_AST = nullAST;
		tmp121_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		postscriptFunc_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_33);
	}
	returnAST = postscriptFunc_AST;
}

void GrpParser::unicodeFunc() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST unicodeFunc_AST = nullAST;

	try {      // for error handling
		RefAST tmp122_AST = nullAST;
		tmp122_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp122_AST);
		match(LITERAL_unicode);
		RefAST tmp123_AST = nullAST;
		tmp123_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		intOrRange();
		astFactory.addASTChild(currentAST, returnAST);
		{
		do {
			if ((LA(1)==LIT_INT||LA(1)==OP_COMMA)) {
				{
				switch ( LA(1)) {
				case OP_COMMA:
				{
					RefAST tmp124_AST = nullAST;
					tmp124_AST = astFactory.create(LT(1));
					match(OP_COMMA);
					break;
				}
				case LIT_INT:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				intOrRange();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop115;
			}

		} while (true);
		_loop115:;
		}
		RefAST tmp125_AST = nullAST;
		tmp125_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		unicodeFunc_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_33);
	}
	returnAST = unicodeFunc_AST;
}

void GrpParser::unicodeCodepoint() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST unicodeCodepoint_AST = nullAST;
	RefAST U_AST = nullAST;

	try {      // for error handling
		unicodeIntOrRange();
		U_AST = returnAST;
		astFactory.addASTChild(currentAST, returnAST);
		unicodeCodepoint_AST = currentAST.root;
		unicodeCodepoint_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(ZuHex))->add(U_AST));
		currentAST.root = unicodeCodepoint_AST;
		currentAST.child = unicodeCodepoint_AST!=nullAST &&unicodeCodepoint_AST->getFirstChild()!=nullAST ?
			unicodeCodepoint_AST->getFirstChild() : unicodeCodepoint_AST;
		currentAST.advanceChildToEnd();
		unicodeCodepoint_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_33);
	}
	returnAST = unicodeCodepoint_AST;
}

void GrpParser::pseudoFunc() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST pseudoFunc_AST = nullAST;

	try {      // for error handling
		RefAST tmp126_AST = nullAST;
		tmp126_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp126_AST);
		match(LITERAL_pseudo);
		RefAST tmp127_AST = nullAST;
		tmp127_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		{
		switch ( LA(1)) {
		case LITERAL_codepoint:
		{
			codepointFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_glyphid:
		{
			glyphidFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_postscript:
		{
			postscriptFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_unicode:
		{
			unicodeFunc();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LIT_UHEX:
		{
			unicodeCodepoint();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case LIT_INT:
		case OP_COMMA:
		case LIT_UHEX:
		{
			{
			switch ( LA(1)) {
			case OP_COMMA:
			{
				RefAST tmp128_AST = nullAST;
				tmp128_AST = astFactory.create(LT(1));
				match(OP_COMMA);
				break;
			}
			case LIT_INT:
			case LIT_UHEX:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			{
			switch ( LA(1)) {
			case LIT_INT:
			{
				RefAST tmp129_AST = nullAST;
				tmp129_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp129_AST);
				match(LIT_INT);
				break;
			}
			case LIT_UHEX:
			{
				RefAST tmp130_AST = nullAST;
				tmp130_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp130_AST);
				match(LIT_UHEX);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			break;
		}
		case OP_RPAREN:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp131_AST = nullAST;
		tmp131_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		pseudoFunc_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_26);
	}
	returnAST = pseudoFunc_AST;
}

void GrpParser::codepointList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST codepointList_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_LPAREN:
		{
			{
			RefAST tmp132_AST = nullAST;
			tmp132_AST = astFactory.create(LT(1));
			match(OP_LPAREN);
			codepointItem();
			astFactory.addASTChild(currentAST, returnAST);
			{
			do {
				if ((_tokenSet_34.member(LA(1)))) {
					{
					switch ( LA(1)) {
					case OP_COMMA:
					{
						RefAST tmp133_AST = nullAST;
						tmp133_AST = astFactory.create(LT(1));
						match(OP_COMMA);
						break;
					}
					case LIT_INT:
					case LIT_STRING:
					case LIT_CHAR:
					{
						break;
					}
					default:
					{
						throw NoViableAltException(LT(1));
					}
					}
					}
					codepointItem();
					astFactory.addASTChild(currentAST, returnAST);
				}
				else {
					goto _loop101;
				}

			} while (true);
			_loop101:;
			}
			RefAST tmp134_AST = nullAST;
			tmp134_AST = astFactory.create(LT(1));
			match(OP_RPAREN);
			}
			break;
		}
		case LIT_INT:
		case LIT_STRING:
		case LIT_CHAR:
		{
			codepointItem();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		codepointList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_35);
	}
	returnAST = codepointList_AST;
}

void GrpParser::codepointItem() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST codepointItem_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LIT_STRING:
		{
			RefAST tmp135_AST = nullAST;
			tmp135_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp135_AST);
			match(LIT_STRING);
			break;
		}
		case LIT_INT:
		case LIT_CHAR:
		{
			charOrIntOrRange();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		codepointItem_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_36);
	}
	returnAST = codepointItem_AST;
}

void GrpParser::charOrIntOrRange() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST charOrIntOrRange_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LIT_CHAR:
		{
			RefAST tmp136_AST = nullAST;
			tmp136_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp136_AST);
			match(LIT_CHAR);
			break;
		}
		case LIT_INT:
		{
			RefAST tmp137_AST = nullAST;
			tmp137_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp137_AST);
			match(LIT_INT);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_DOTDOT:
		{
			RefAST tmp138_AST = nullAST;
			tmp138_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp138_AST);
			match(OP_DOTDOT);
			{
			switch ( LA(1)) {
			case LIT_CHAR:
			{
				RefAST tmp139_AST = nullAST;
				tmp139_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp139_AST);
				match(LIT_CHAR);
				break;
			}
			case LIT_INT:
			{
				RefAST tmp140_AST = nullAST;
				tmp140_AST = astFactory.create(LT(1));
				astFactory.addASTChild(currentAST, tmp140_AST);
				match(LIT_INT);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			break;
		}
		case OP_RPAREN:
		case LIT_INT:
		case LIT_STRING:
		case OP_COMMA:
		case LIT_CHAR:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		charOrIntOrRange_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_36);
	}
	returnAST = charOrIntOrRange_AST;
}

void GrpParser::intOrRange() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST intOrRange_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==LIT_INT) && (LA(2)==OP_DOTDOT)) {
			RefAST tmp141_AST = nullAST;
			tmp141_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp141_AST);
			match(LIT_INT);
			RefAST tmp142_AST = nullAST;
			tmp142_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp142_AST);
			match(OP_DOTDOT);
			RefAST tmp143_AST = nullAST;
			tmp143_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp143_AST);
			match(LIT_INT);
		}
		else if ((LA(1)==LIT_INT) && (LA(2)==OP_RPAREN||LA(2)==LIT_INT||LA(2)==OP_COMMA)) {
			RefAST tmp144_AST = nullAST;
			tmp144_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp144_AST);
			match(LIT_INT);
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		intOrRange_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_37);
	}
	returnAST = intOrRange_AST;
}

void GrpParser::unicodeIntOrRange() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST unicodeIntOrRange_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==LIT_UHEX) && (LA(2)==OP_DOTDOT)) {
			RefAST tmp145_AST = nullAST;
			tmp145_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp145_AST);
			match(LIT_UHEX);
			RefAST tmp146_AST = nullAST;
			tmp146_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp146_AST);
			match(OP_DOTDOT);
			RefAST tmp147_AST = nullAST;
			tmp147_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp147_AST);
			match(LIT_UHEX);
		}
		else if ((LA(1)==LIT_UHEX) && (_tokenSet_33.member(LA(2)))) {
			RefAST tmp148_AST = nullAST;
			tmp148_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp148_AST);
			match(LIT_UHEX);
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		unicodeIntOrRange_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_33);
	}
	returnAST = unicodeIntOrRange_AST;
}

void GrpParser::featureEnv() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST featureEnv_AST = nullAST;

	try {      // for error handling
		RefAST tmp149_AST = nullAST;
		tmp149_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp149_AST);
		match(LITERAL_environment);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_name:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp150_AST = nullAST;
			tmp150_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_name:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case IDENT:
			case LITERAL_name:
			case LIT_INT:
			{
				featureSpecList();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_environment:
			{
				featureEnv();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_table:
			{
				tableDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop134;
			}
			}
		} while (true);
		_loop134:;
		}
		RefAST tmp151_AST = nullAST;
		tmp151_AST = astFactory.create(LT(1));
		match(LITERAL_endenvironment);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp152_AST = nullAST;
			tmp152_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_name:
		case LIT_INT:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		featureEnv_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_38);
	}
	returnAST = featureEnv_AST;
}

void GrpParser::featureSpecList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST featureSpecList_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==IDENT||LA(1)==LITERAL_name) && (LA(2)==OP_LBRACE)) {
			featureSpecStruct();
			astFactory.addASTChild(currentAST, returnAST);
			{
			if ((LA(1)==IDENT||LA(1)==LITERAL_name||LA(1)==LIT_INT) && (LA(2)==OP_EQ||LA(2)==OP_LBRACE||LA(2)==OP_DOT) && (_tokenSet_39.member(LA(3)))) {
				featureSpecList();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else if ((_tokenSet_40.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
		}
		else if ((LA(1)==IDENT||LA(1)==LITERAL_name||LA(1)==LIT_INT) && (LA(2)==OP_EQ||LA(2)==OP_DOT)) {
			featureSpecFlat();
			astFactory.addASTChild(currentAST, returnAST);
			{
			if ((LA(1)==OP_SEMI) && (LA(2)==IDENT||LA(2)==LITERAL_name||LA(2)==LIT_INT) && (LA(3)==OP_EQ||LA(3)==OP_LBRACE||LA(3)==OP_DOT)) {
				RefAST tmp153_AST = nullAST;
				tmp153_AST = astFactory.create(LT(1));
				match(OP_SEMI);
				featureSpecList();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else if ((_tokenSet_41.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			{
			switch ( LA(1)) {
			case OP_SEMI:
			{
				RefAST tmp154_AST = nullAST;
				tmp154_AST = astFactory.create(LT(1));
				match(OP_SEMI);
				break;
			}
			case LITERAL_environment:
			case LITERAL_endenvironment:
			case OP_RBRACE:
			case IDENT:
			case LITERAL_table:
			case LITERAL_endtable:
			case LITERAL_name:
			case LIT_INT:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		featureSpecList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_40);
	}
	returnAST = featureSpecList_AST;
}

void GrpParser::featureSpecStruct() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST featureSpecStruct_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;
	RefToken  In = nullToken;
	RefAST In_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case IDENT:
		{
			I = LT(1);
			I_AST = astFactory.create(I);
			match(IDENT);
			break;
		}
		case LITERAL_name:
		{
			In = LT(1);
			In_AST = astFactory.create(In);
			match(LITERAL_name);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp155_AST = nullAST;
		tmp155_AST = astFactory.create(LT(1));
		match(OP_LBRACE);
		{
		switch ( LA(1)) {
		case IDENT:
		case LITERAL_name:
		case LIT_INT:
		{
			featureSpecList();
			X_AST = returnAST;
			break;
		}
		case OP_RBRACE:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp156_AST = nullAST;
		tmp156_AST = astFactory.create(LT(1));
		match(OP_RBRACE);
		{
		if ((LA(1)==OP_SEMI) && (_tokenSet_41.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			RefAST tmp157_AST = nullAST;
			tmp157_AST = astFactory.create(LT(1));
			match(OP_SEMI);
		}
		else if ((_tokenSet_41.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		featureSpecStruct_AST = currentAST.root;
		featureSpecStruct_AST = astFactory.make( (new ASTArray(4))->add(astFactory.create(ZdotStruct))->add(I_AST)->add(In_AST)->add(X_AST));
		currentAST.root = featureSpecStruct_AST;
		currentAST.child = featureSpecStruct_AST!=nullAST &&featureSpecStruct_AST->getFirstChild()!=nullAST ?
			featureSpecStruct_AST->getFirstChild() : featureSpecStruct_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_41);
	}
	returnAST = featureSpecStruct_AST;
}

void GrpParser::featureSpecFlat() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST featureSpecFlat_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;
	RefToken  In = nullToken;
	RefAST In_AST = nullAST;
	RefToken  D = nullToken;
	RefAST D_AST = nullAST;
	RefAST X1_AST = nullAST;
	RefAST X2_AST = nullAST;
	RefToken  E1 = nullToken;
	RefAST E1_AST = nullAST;
	RefAST Vi1_AST = nullAST;
	RefAST Vi2_AST = nullAST;
	RefToken  Vi3 = nullToken;
	RefAST Vi3_AST = nullAST;
	RefToken  N = nullToken;
	RefAST N_AST = nullAST;
	RefToken  E2 = nullToken;
	RefAST E2_AST = nullAST;
	RefAST Vn1_AST = nullAST;
	RefAST Vn2_AST = nullAST;
	RefToken  Vn3 = nullToken;
	RefAST Vn3_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case IDENT:
		case LITERAL_name:
		{
			{
			switch ( LA(1)) {
			case IDENT:
			{
				I = LT(1);
				I_AST = astFactory.create(I);
				match(IDENT);
				break;
			}
			case LITERAL_name:
			{
				In = LT(1);
				In_AST = astFactory.create(In);
				match(LITERAL_name);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			{
			switch ( LA(1)) {
			case OP_DOT:
			{
				D = LT(1);
				D_AST = astFactory.create(D);
				match(OP_DOT);
				{
				if ((LA(1)==IDENT||LA(1)==LITERAL_name||LA(1)==LIT_INT) && (LA(2)==OP_EQ||LA(2)==OP_DOT)) {
					featureSpecFlat();
					X1_AST = returnAST;
				}
				else if ((LA(1)==IDENT||LA(1)==LITERAL_name) && (LA(2)==OP_LBRACE)) {
					featureSpecStruct();
					X2_AST = returnAST;
				}
				else {
					throw NoViableAltException(LT(1));
				}

				}
				featureSpecFlat_AST = currentAST.root;
				featureSpecFlat_AST = astFactory.make( (new ASTArray(5))->add(D_AST)->add(I_AST)->add(In_AST)->add(X1_AST)->add(X2_AST));
				currentAST.root = featureSpecFlat_AST;
				currentAST.child = featureSpecFlat_AST!=nullAST &&featureSpecFlat_AST->getFirstChild()!=nullAST ?
					featureSpecFlat_AST->getFirstChild() : featureSpecFlat_AST;
				currentAST.advanceChildToEnd();
				break;
			}
			case OP_EQ:
			{
				E1 = LT(1);
				E1_AST = astFactory.create(E1);
				match(OP_EQ);
				{
				switch ( LA(1)) {
				case LIT_INT:
				case OP_PLUS:
				case OP_MINUS:
				case LITERAL_true:
				case LITERAL_false:
				{
					signedInt();
					Vi1_AST = returnAST;
					break;
				}
				case OP_LPAREN:
				case LIT_STRING:
				case LITERAL_string:
				{
					stringDefn();
					Vi2_AST = returnAST;
					break;
				}
				case IDENT:
				{
					Vi3 = LT(1);
					Vi3_AST = astFactory.create(Vi3);
					match(IDENT);
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				featureSpecFlat_AST = currentAST.root;
				featureSpecFlat_AST = astFactory.make( (new ASTArray(6))->add(E1_AST)->add(I_AST)->add(In_AST)->add(Vi1_AST)->add(Vi2_AST)->add(Vi3_AST));
				currentAST.root = featureSpecFlat_AST;
				currentAST.child = featureSpecFlat_AST!=nullAST &&featureSpecFlat_AST->getFirstChild()!=nullAST ?
					featureSpecFlat_AST->getFirstChild() : featureSpecFlat_AST;
				currentAST.advanceChildToEnd();
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			break;
		}
		case LIT_INT:
		{
			N = LT(1);
			N_AST = astFactory.create(N);
			match(LIT_INT);
			E2 = LT(1);
			E2_AST = astFactory.create(E2);
			match(OP_EQ);
			{
			switch ( LA(1)) {
			case LIT_INT:
			case OP_PLUS:
			case OP_MINUS:
			case LITERAL_true:
			case LITERAL_false:
			{
				signedInt();
				Vn1_AST = returnAST;
				break;
			}
			case OP_LPAREN:
			case LIT_STRING:
			case LITERAL_string:
			{
				stringDefn();
				Vn2_AST = returnAST;
				break;
			}
			case IDENT:
			{
				Vn3 = LT(1);
				Vn3_AST = astFactory.create(Vn3);
				match(IDENT);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			featureSpecFlat_AST = currentAST.root;
			featureSpecFlat_AST = astFactory.make( (new ASTArray(5))->add(E2_AST)->add(N_AST)->add(Vn1_AST)->add(Vn2_AST)->add(Vn3_AST));
			currentAST.root = featureSpecFlat_AST;
			currentAST.child = featureSpecFlat_AST!=nullAST &&featureSpecFlat_AST->getFirstChild()!=nullAST ?
				featureSpecFlat_AST->getFirstChild() : featureSpecFlat_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_41);
	}
	returnAST = featureSpecFlat_AST;
}

void GrpParser::languageEnv() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST languageEnv_AST = nullAST;

	try {      // for error handling
		RefAST tmp158_AST = nullAST;
		tmp158_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp158_AST);
		match(LITERAL_environment);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp159_AST = nullAST;
			tmp159_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case IDENT:
			{
				languageSpecList();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_environment:
			{
				languageEnv();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			case LITERAL_table:
			{
				tableDecl();
				astFactory.addASTChild(currentAST, returnAST);
				break;
			}
			default:
			{
				goto _loop161;
			}
			}
		} while (true);
		_loop161:;
		}
		RefAST tmp160_AST = nullAST;
		tmp160_AST = astFactory.create(LT(1));
		match(LITERAL_endenvironment);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp161_AST = nullAST;
			tmp161_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		languageEnv_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_21);
	}
	returnAST = languageEnv_AST;
}

void GrpParser::languageSpecList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST languageSpecList_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==IDENT) && (LA(2)==OP_LBRACE||LA(2)==OP_DOT) && (_tokenSet_42.member(LA(3)))) {
			languageSpec();
			astFactory.addASTChild(currentAST, returnAST);
			{
			if ((LA(1)==IDENT) && (LA(2)==OP_LBRACE||LA(2)==OP_DOT) && (_tokenSet_42.member(LA(3)))) {
				languageSpecList();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else if ((_tokenSet_23.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
		}
		else if ((LA(1)==IDENT) && (LA(2)==OP_LBRACE||LA(2)==OP_DOT) && (_tokenSet_42.member(LA(3)))) {
			languageSpec();
			astFactory.addASTChild(currentAST, returnAST);
			{
			if ((LA(1)==OP_SEMI) && (LA(2)==IDENT) && (LA(3)==OP_LBRACE||LA(3)==OP_DOT)) {
				RefAST tmp162_AST = nullAST;
				tmp162_AST = astFactory.create(LT(1));
				match(OP_SEMI);
				languageSpecList();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else if ((_tokenSet_23.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			{
			if ((LA(1)==OP_SEMI) && (_tokenSet_23.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
				RefAST tmp163_AST = nullAST;
				tmp163_AST = astFactory.create(LT(1));
				match(OP_SEMI);
			}
			else if ((_tokenSet_23.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		languageSpecList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_23);
	}
	returnAST = languageSpecList_AST;
}

void GrpParser::languageSpec() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST languageSpec_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;
	RefAST X1_AST = nullAST;
	RefAST X2_AST = nullAST;

	try {      // for error handling
		I = LT(1);
		I_AST = astFactory.create(I);
		match(IDENT);
		{
		switch ( LA(1)) {
		case OP_DOT:
		{
			RefAST tmp164_AST = nullAST;
			tmp164_AST = astFactory.create(LT(1));
			match(OP_DOT);
			languageSpecItem();
			X1_AST = returnAST;
			languageSpec_AST = currentAST.root;
			languageSpec_AST = astFactory.make( (new ASTArray(3))->add(astFactory.create(ZdotStruct))->add(I_AST)->add(X1_AST));
			currentAST.root = languageSpec_AST;
			currentAST.child = languageSpec_AST!=nullAST &&languageSpec_AST->getFirstChild()!=nullAST ?
				languageSpec_AST->getFirstChild() : languageSpec_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case OP_LBRACE:
		{
			RefAST tmp165_AST = nullAST;
			tmp165_AST = astFactory.create(LT(1));
			match(OP_LBRACE);
			languageItemList();
			X2_AST = returnAST;
			RefAST tmp166_AST = nullAST;
			tmp166_AST = astFactory.create(LT(1));
			match(OP_RBRACE);
			{
			if ((LA(1)==OP_SEMI) && (_tokenSet_23.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
				RefAST tmp167_AST = nullAST;
				tmp167_AST = astFactory.create(LT(1));
				match(OP_SEMI);
			}
			else if ((_tokenSet_23.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			languageSpec_AST = currentAST.root;
			languageSpec_AST = astFactory.make( (new ASTArray(3))->add(astFactory.create(ZdotStruct))->add(I_AST)->add(X2_AST));
			currentAST.root = languageSpec_AST;
			currentAST.child = languageSpec_AST!=nullAST &&languageSpec_AST->getFirstChild()!=nullAST ?
				languageSpec_AST->getFirstChild() : languageSpec_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_23);
	}
	returnAST = languageSpec_AST;
}

void GrpParser::languageSpecItem() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST languageSpecItem_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;
	RefToken  E1 = nullToken;
	RefAST E1_AST = nullAST;
	RefAST Vi1_AST = nullAST;
	RefToken  Vi2 = nullToken;
	RefAST Vi2_AST = nullAST;
	RefToken  Ilang = nullToken;
	RefAST Ilang_AST = nullAST;
	RefToken  Ilangs = nullToken;
	RefAST Ilangs_AST = nullAST;
	RefToken  E2 = nullToken;
	RefAST E2_AST = nullAST;
	RefAST LL_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case IDENT:
		{
			I = LT(1);
			I_AST = astFactory.create(I);
			match(IDENT);
			E1 = LT(1);
			E1_AST = astFactory.create(E1);
			match(OP_EQ);
			{
			switch ( LA(1)) {
			case LIT_INT:
			case OP_PLUS:
			case OP_MINUS:
			case LITERAL_true:
			case LITERAL_false:
			{
				signedInt();
				Vi1_AST = returnAST;
				break;
			}
			case IDENT:
			{
				Vi2 = LT(1);
				Vi2_AST = astFactory.create(Vi2);
				match(IDENT);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			languageSpecItem_AST = currentAST.root;
			languageSpecItem_AST = astFactory.make( (new ASTArray(4))->add(E1_AST)->add(I_AST)->add(Vi1_AST)->add(Vi2_AST));
			currentAST.root = languageSpecItem_AST;
			currentAST.child = languageSpecItem_AST!=nullAST &&languageSpecItem_AST->getFirstChild()!=nullAST ?
				languageSpecItem_AST->getFirstChild() : languageSpecItem_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case LITERAL_language:
		case LITERAL_languages:
		{
			{
			switch ( LA(1)) {
			case LITERAL_language:
			{
				Ilang = LT(1);
				Ilang_AST = astFactory.create(Ilang);
				match(LITERAL_language);
				break;
			}
			case LITERAL_languages:
			{
				Ilangs = LT(1);
				Ilangs_AST = astFactory.create(Ilangs);
				match(LITERAL_languages);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			E2 = LT(1);
			E2_AST = astFactory.create(E2);
			match(OP_EQ);
			languageCodeList();
			LL_AST = returnAST;
			languageSpecItem_AST = currentAST.root;
			languageSpecItem_AST = astFactory.make( (new ASTArray(4))->add(E2_AST)->add(Ilang_AST)->add(Ilangs_AST)->add(LL_AST));
			currentAST.root = languageSpecItem_AST;
			currentAST.child = languageSpecItem_AST!=nullAST &&languageSpecItem_AST->getFirstChild()!=nullAST ?
				languageSpecItem_AST->getFirstChild() : languageSpecItem_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		if ((LA(1)==OP_SEMI) && (_tokenSet_43.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			RefAST tmp168_AST = nullAST;
			tmp168_AST = astFactory.create(LT(1));
			match(OP_SEMI);
		}
		else if ((_tokenSet_43.member(LA(1))) && (_tokenSet_4.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_43);
	}
	returnAST = languageSpecItem_AST;
}

void GrpParser::languageItemList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST languageItemList_AST = nullAST;

	try {      // for error handling
		{
		do {
			if ((LA(1)==IDENT||LA(1)==LITERAL_language||LA(1)==LITERAL_languages)) {
				languageSpecItem();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop173;
			}

		} while (true);
		_loop173:;
		}
		languageItemList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_44);
	}
	returnAST = languageItemList_AST;
}

void GrpParser::languageCodeList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST languageCodeList_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LIT_STRING:
		{
			RefAST tmp169_AST = nullAST;
			tmp169_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp169_AST);
			match(LIT_STRING);
			break;
		}
		case OP_LPAREN:
		{
			RefAST tmp170_AST = nullAST;
			tmp170_AST = astFactory.create(LT(1));
			match(OP_LPAREN);
			RefAST tmp171_AST = nullAST;
			tmp171_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp171_AST);
			match(LIT_STRING);
			{
			do {
				if ((LA(1)==OP_COMMA)) {
					RefAST tmp172_AST = nullAST;
					tmp172_AST = astFactory.create(LT(1));
					match(OP_COMMA);
					RefAST tmp173_AST = nullAST;
					tmp173_AST = astFactory.create(LT(1));
					astFactory.addASTChild(currentAST, tmp173_AST);
					match(LIT_STRING);
				}
				else {
					goto _loop182;
				}

			} while (true);
			_loop182:;
			}
			RefAST tmp174_AST = nullAST;
			tmp174_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp174_AST);
			match(OP_RPAREN);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		languageCodeList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_43);
	}
	returnAST = languageCodeList_AST;
}

void GrpParser::subEntry() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subEntry_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LITERAL_if:
		{
			subIf();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			subRule();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_pass:
		{
			subPass();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_environment:
		{
			subEnv();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_table:
		{
			tableDecl();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		subEntry_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = subEntry_AST;
}

void GrpParser::subIf() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subIf_AST = nullAST;
	RefToken  C1k = nullToken;
	RefAST C1k_AST = nullAST;
	RefAST E_AST = nullAST;
	RefAST C1_AST = nullAST;
	RefAST C1x_AST = nullAST;
	RefAST C2_AST = nullAST;
	RefAST C2x_AST = nullAST;
	RefAST C3_AST = nullAST;
	RefToken  C3k = nullToken;
	RefAST C3k_AST = nullAST;
	RefAST C3x_AST = nullAST;

	try {      // for error handling
		C1k = LT(1);
		C1k_AST = astFactory.create(C1k);
		match(LITERAL_if);
		RefAST tmp175_AST = nullAST;
		tmp175_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		expr();
		E_AST = returnAST;
		RefAST tmp176_AST = nullAST;
		tmp176_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		subEntryList();
		C1x_AST = returnAST;
		C1_AST = astFactory.make( (new ASTArray(3))->add(C1k_AST)->add(E_AST)->add(C1x_AST));
		}
		{
		if (((LA(1) >= LITERAL_else && LA(1) <= LITERAL_elseif)) && (_tokenSet_46.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			subElseIfList();
			C2x_AST = returnAST;
			C2_AST = C2x_AST;
		}
		else if ((LA(1)==LITERAL_else||LA(1)==LITERAL_endif) && (_tokenSet_46.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		{
		switch ( LA(1)) {
		case LITERAL_else:
		{
			C3k = LT(1);
			C3k_AST = astFactory.create(C3k);
			match(LITERAL_else);
			subEntryList();
			C3x_AST = returnAST;
			C3_AST = astFactory.make( (new ASTArray(2))->add(C3k_AST)->add(C3x_AST));
			break;
		}
		case LITERAL_endif:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp177_AST = nullAST;
		tmp177_AST = astFactory.create(LT(1));
		match(LITERAL_endif);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp178_AST = nullAST;
			tmp178_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case LITERAL_else:
		case LITERAL_endif:
		case Zelseif:
		case LITERAL_elseif:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		subIf_AST = currentAST.root;
		subIf_AST = astFactory.make( (new ASTArray(4))->add(astFactory.create(ZifStruct))->add(C1_AST)->add(C2_AST)->add(C3_AST));
		currentAST.root = subIf_AST;
		currentAST.child = subIf_AST!=nullAST &&subIf_AST->getFirstChild()!=nullAST ?
			subIf_AST->getFirstChild() : subIf_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = subIf_AST;
}

void GrpParser::subRule() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subRule_AST = nullAST;
	RefAST L_AST = nullAST;
	RefAST L1_AST = nullAST;
	RefAST R_AST = nullAST;
	RefAST R1_AST = nullAST;
	RefAST C_AST = nullAST;
	RefAST C1_AST = nullAST;

	try {      // for error handling
		{
		subLhs();
		L1_AST = returnAST;
		L_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zlhs))->add(L1_AST));
		}
		{
		switch ( LA(1)) {
		case OP_GT:
		{
			RefAST tmp179_AST = nullAST;
			tmp179_AST = astFactory.create(LT(1));
			match(OP_GT);
			{
			subRhs();
			R1_AST = returnAST;
			R_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zrhs))->add(R1_AST));
			}
			break;
		}
		case OP_SEMI:
		case OP_DIV:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_DIV:
		{
			RefAST tmp180_AST = nullAST;
			tmp180_AST = astFactory.create(LT(1));
			match(OP_DIV);
			{
			context();
			C1_AST = returnAST;
			C_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zcontext))->add(C1_AST));
			}
			break;
		}
		case OP_SEMI:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp181_AST = nullAST;
		tmp181_AST = astFactory.create(LT(1));
		match(OP_SEMI);
		subRule_AST = currentAST.root;
		subRule_AST = astFactory.make( (new ASTArray(4))->add(astFactory.create(Zrule))->add(L_AST)->add(R_AST)->add(C_AST));
		currentAST.root = subRule_AST;
		currentAST.child = subRule_AST!=nullAST &&subRule_AST->getFirstChild()!=nullAST ?
			subRule_AST->getFirstChild() : subRule_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = subRule_AST;
}

void GrpParser::subPass() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subPass_AST = nullAST;

	try {      // for error handling
		RefAST tmp182_AST = nullAST;
		tmp182_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp182_AST);
		match(LITERAL_pass);
		RefAST tmp183_AST = nullAST;
		tmp183_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp184_AST = nullAST;
		tmp184_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp184_AST);
		match(LIT_INT);
		RefAST tmp185_AST = nullAST;
		tmp185_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp186_AST = nullAST;
			tmp186_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				subEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop200;
			}

		} while (true);
		_loop200:;
		}
		RefAST tmp187_AST = nullAST;
		tmp187_AST = astFactory.create(LT(1));
		match(LITERAL_endpass);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp188_AST = nullAST;
			tmp188_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case LITERAL_else:
		case LITERAL_endif:
		case Zelseif:
		case LITERAL_elseif:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		subPass_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = subPass_AST;
}

void GrpParser::subEnv() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subEnv_AST = nullAST;

	try {      // for error handling
		RefAST tmp189_AST = nullAST;
		tmp189_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp189_AST);
		match(LITERAL_environment);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp190_AST = nullAST;
			tmp190_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				subEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop194;
			}

		} while (true);
		_loop194:;
		}
		RefAST tmp191_AST = nullAST;
		tmp191_AST = astFactory.create(LT(1));
		match(LITERAL_endenvironment);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp192_AST = nullAST;
			tmp192_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case LITERAL_else:
		case LITERAL_endif:
		case Zelseif:
		case LITERAL_elseif:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		subEnv_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = subEnv_AST;
}

void GrpParser::subEntryList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subEntryList_AST = nullAST;

	try {      // for error handling
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				subEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop214;
			}

		} while (true);
		_loop214:;
		}
		subEntryList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_47);
	}
	returnAST = subEntryList_AST;
}

void GrpParser::subElseIfList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subElseIfList_AST = nullAST;

	try {      // for error handling
		{
		do {
			if ((LA(1)==Zelseif||LA(1)==LITERAL_elseif)) {
				subElseIf();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop209;
			}

		} while (true);
		_loop209:;
		}
		subElseIfList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_48);
	}
	returnAST = subElseIfList_AST;
}

void GrpParser::subElseIf() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subElseIf_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case Zelseif:
		{
			RefAST tmp193_AST = nullAST;
			tmp193_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp193_AST);
			match(Zelseif);
			break;
		}
		case LITERAL_elseif:
		{
			RefAST tmp194_AST = nullAST;
			tmp194_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp194_AST);
			match(LITERAL_elseif);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp195_AST = nullAST;
		tmp195_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		expr();
		astFactory.addASTChild(currentAST, returnAST);
		RefAST tmp196_AST = nullAST;
		tmp196_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		subEntryList();
		astFactory.addASTChild(currentAST, returnAST);
		subElseIf_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_47);
	}
	returnAST = subElseIf_AST;
}

void GrpParser::subLhs() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subLhs_AST = nullAST;

	try {      // for error handling
		{
		int _cnt223=0;
		do {
			if ((_tokenSet_49.member(LA(1))) && (_tokenSet_50.member(LA(2))) && (_tokenSet_51.member(LA(3)))) {
				subLhsRange();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				if ( _cnt223>=1 ) { goto _loop223; } else {throw NoViableAltException(LT(1));}
			}

			_cnt223++;
		} while (true);
		_loop223:;
		}
		subLhs_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_52);
	}
	returnAST = subLhs_AST;
}

void GrpParser::subRhs() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subRhs_AST = nullAST;

	try {      // for error handling
		{
		int _cnt234=0;
		do {
			if ((_tokenSet_53.member(LA(1)))) {
				subRhsItem();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				if ( _cnt234>=1 ) { goto _loop234; } else {throw NoViableAltException(LT(1));}
			}

			_cnt234++;
		} while (true);
		_loop234:;
		}
		subRhs_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_54);
	}
	returnAST = subRhs_AST;
}

void GrpParser::context() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST context_AST = nullAST;

	try {      // for error handling
		{
		do {
			if ((_tokenSet_55.member(LA(1)))) {
				contextRange();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop325;
			}

		} while (true);
		_loop325:;
		}
		context_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_56);
	}
	returnAST = context_AST;
}

void GrpParser::subLhsRange() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subLhsRange_AST = nullAST;
	RefAST X1_AST = nullAST;
	RefAST X2_AST = nullAST;
	RefToken  Q = nullToken;
	RefAST Q_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_LBRACKET:
		{
			subLhsList();
			X1_AST = returnAST;
			subLhsRange_AST = currentAST.root;
			subLhsRange_AST = X1_AST;
			currentAST.root = subLhsRange_AST;
			currentAST.child = subLhsRange_AST!=nullAST &&subLhsRange_AST->getFirstChild()!=nullAST ?
				subLhsRange_AST->getFirstChild() : subLhsRange_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case OP_LPAREN:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			subLhsItem();
			X2_AST = returnAST;
			{
			if ((LA(1)==OP_QUESTION)) {
				Q = LT(1);
				Q_AST = astFactory.create(Q);
				match(OP_QUESTION);
				subLhsRange_AST = currentAST.root;
				subLhsRange_AST = astFactory.make( (new ASTArray(2))->add(Q_AST)->add(X2_AST));
				currentAST.root = subLhsRange_AST;
				currentAST.child = subLhsRange_AST!=nullAST &&subLhsRange_AST->getFirstChild()!=nullAST ?
					subLhsRange_AST->getFirstChild() : subLhsRange_AST;
				currentAST.advanceChildToEnd();
			}
			else if ((_tokenSet_52.member(LA(1))) && (_tokenSet_57.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
				subLhsRange_AST = currentAST.root;
				subLhsRange_AST = X2_AST;
				currentAST.root = subLhsRange_AST;
				currentAST.child = subLhsRange_AST!=nullAST &&subLhsRange_AST->getFirstChild()!=nullAST ?
					subLhsRange_AST->getFirstChild() : subLhsRange_AST;
				currentAST.advanceChildToEnd();
			}
			else if ((_tokenSet_52.member(LA(1))) && (_tokenSet_57.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_52);
	}
	returnAST = subLhsRange_AST;
}

void GrpParser::subLhsList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subLhsList_AST = nullAST;

	try {      // for error handling
		{
		RefAST tmp197_AST = nullAST;
		tmp197_AST = astFactory.create(LT(1));
		match(OP_LBRACKET);
		{
		int _cnt230=0;
		do {
			if ((_tokenSet_49.member(LA(1)))) {
				subLhs();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				if ( _cnt230>=1 ) { goto _loop230; } else {throw NoViableAltException(LT(1));}
			}

			_cnt230++;
		} while (true);
		_loop230:;
		}
		RefAST tmp198_AST = nullAST;
		tmp198_AST = astFactory.create(LT(1));
		match(OP_RBRACKET);
		}
		RefAST tmp199_AST = nullAST;
		tmp199_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp199_AST);
		match(OP_QUESTION);
		subLhsList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_52);
	}
	returnAST = subLhsList_AST;
}

void GrpParser::subLhsItem() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subLhsItem_AST = nullAST;

	try {      // for error handling
		subRhsItem();
		astFactory.addASTChild(currentAST, returnAST);
		subLhsItem_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_58);
	}
	returnAST = subLhsItem_AST;
}

void GrpParser::subRhsItem() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST subRhsItem_AST = nullAST;
	RefToken  C1g = nullToken;
	RefAST C1g_AST = nullAST;
	RefToken  C2at = nullToken;
	RefAST C2at_AST = nullAST;
	RefAST C2s_AST = nullAST;
	RefAST C2a_AST = nullAST;
	RefToken  C3at = nullToken;
	RefAST C3at_AST = nullAST;
	RefAST C3s_AST = nullAST;
	RefAST C4g1_AST = nullAST;
	RefToken  C4g2 = nullToken;
	RefAST C4g2_AST = nullAST;
	RefAST C4a1_AST = nullAST;
	RefAST C4s1_AST = nullAST;
	RefAST C4s2_AST = nullAST;
	RefAST C4a2_AST = nullAST;
	RefAST A_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_UNDER:
		{
			C1g = LT(1);
			C1g_AST = astFactory.create(C1g);
			match(OP_UNDER);
			break;
		}
		case OP_LPAREN:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_HASH:
		{
			{
			{
			switch ( LA(1)) {
			case OP_LPAREN:
			case IDENT:
			case LITERAL_pseudo:
			case LIT_UHEX:
			case LITERAL_codepoint:
			case LITERAL_glyphid:
			case LITERAL_postscript:
			case LITERAL_unicode:
			{
				glyphSpec();
				C4g1_AST = returnAST;
				break;
			}
			case OP_HASH:
			{
				C4g2 = LT(1);
				C4g2_AST = astFactory.create(C4g2);
				match(OP_HASH);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			{
			switch ( LA(1)) {
			case OP_COLON:
			{
				{
				RefAST tmp200_AST = nullAST;
				tmp200_AST = astFactory.create(LT(1));
				match(OP_COLON);
				associations();
				C4a1_AST = returnAST;
				{
				switch ( LA(1)) {
				case OP_DOLLAR:
				{
					RefAST tmp201_AST = nullAST;
					tmp201_AST = astFactory.create(LT(1));
					match(OP_DOLLAR);
					selector();
					C4s1_AST = returnAST;
					break;
				}
				case OP_EQ:
				case OP_LPAREN:
				case OP_SEMI:
				case OP_LBRACE:
				case IDENT:
				case LITERAL_pseudo:
				case LIT_UHEX:
				case LITERAL_codepoint:
				case LITERAL_glyphid:
				case LITERAL_postscript:
				case LITERAL_unicode:
				case OP_GT:
				case OP_DIV:
				case OP_QUESTION:
				case OP_LBRACKET:
				case OP_RBRACKET:
				case OP_UNDER:
				case OP_AT:
				case OP_HASH:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				}
				break;
			}
			case OP_DOLLAR:
			{
				{
				RefAST tmp202_AST = nullAST;
				tmp202_AST = astFactory.create(LT(1));
				match(OP_DOLLAR);
				selector();
				C4s2_AST = returnAST;
				{
				switch ( LA(1)) {
				case OP_COLON:
				{
					RefAST tmp203_AST = nullAST;
					tmp203_AST = astFactory.create(LT(1));
					match(OP_COLON);
					associations();
					C4a2_AST = returnAST;
					break;
				}
				case OP_EQ:
				case OP_LPAREN:
				case OP_SEMI:
				case OP_LBRACE:
				case IDENT:
				case LITERAL_pseudo:
				case LIT_UHEX:
				case LITERAL_codepoint:
				case LITERAL_glyphid:
				case LITERAL_postscript:
				case LITERAL_unicode:
				case OP_GT:
				case OP_DIV:
				case OP_QUESTION:
				case OP_LBRACKET:
				case OP_RBRACKET:
				case OP_UNDER:
				case OP_AT:
				case OP_HASH:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				}
				break;
			}
			case OP_EQ:
			case OP_LPAREN:
			case OP_SEMI:
			case OP_LBRACE:
			case IDENT:
			case LITERAL_pseudo:
			case LIT_UHEX:
			case LITERAL_codepoint:
			case LITERAL_glyphid:
			case LITERAL_postscript:
			case LITERAL_unicode:
			case OP_GT:
			case OP_DIV:
			case OP_QUESTION:
			case OP_LBRACKET:
			case OP_RBRACKET:
			case OP_UNDER:
			case OP_AT:
			case OP_HASH:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			}
			break;
		}
		default:
			if ((LA(1)==OP_AT) && (LA(2)==LIT_INT||LA(2)==Qalias) && (_tokenSet_59.member(LA(3)))) {
				{
				C2at = LT(1);
				C2at_AST = astFactory.create(C2at);
				match(OP_AT);
				selectorAfterAt();
				C2s_AST = returnAST;
				{
				switch ( LA(1)) {
				case OP_COLON:
				{
					RefAST tmp204_AST = nullAST;
					tmp204_AST = astFactory.create(LT(1));
					match(OP_COLON);
					associations();
					C2a_AST = returnAST;
					break;
				}
				case OP_EQ:
				case OP_LPAREN:
				case OP_SEMI:
				case OP_LBRACE:
				case IDENT:
				case LITERAL_pseudo:
				case LIT_UHEX:
				case LITERAL_codepoint:
				case LITERAL_glyphid:
				case LITERAL_postscript:
				case LITERAL_unicode:
				case OP_GT:
				case OP_DIV:
				case OP_QUESTION:
				case OP_LBRACKET:
				case OP_RBRACKET:
				case OP_UNDER:
				case OP_AT:
				case OP_HASH:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				}
			}
			else if ((LA(1)==OP_AT) && (_tokenSet_60.member(LA(2))) && (_tokenSet_61.member(LA(3)))) {
				{
				C3at = LT(1);
				C3at_AST = astFactory.create(C3at);
				match(OP_AT);
				{
				switch ( LA(1)) {
				case OP_COLON:
				{
					RefAST tmp205_AST = nullAST;
					tmp205_AST = astFactory.create(LT(1));
					match(OP_COLON);
					break;
				}
				case OP_EQ:
				case OP_LPAREN:
				case OP_SEMI:
				case OP_LBRACE:
				case IDENT:
				case LIT_INT:
				case LITERAL_pseudo:
				case LIT_UHEX:
				case LITERAL_codepoint:
				case LITERAL_glyphid:
				case LITERAL_postscript:
				case LITERAL_unicode:
				case OP_GT:
				case OP_DIV:
				case OP_QUESTION:
				case OP_LBRACKET:
				case OP_RBRACKET:
				case OP_UNDER:
				case OP_AT:
				case OP_HASH:
				case Qalias:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				{
				switch ( LA(1)) {
				case LIT_INT:
				case Qalias:
				{
					selectorAfterAt();
					C3s_AST = returnAST;
					break;
				}
				case OP_EQ:
				case OP_LPAREN:
				case OP_SEMI:
				case OP_LBRACE:
				case IDENT:
				case LITERAL_pseudo:
				case LIT_UHEX:
				case LITERAL_codepoint:
				case LITERAL_glyphid:
				case LITERAL_postscript:
				case LITERAL_unicode:
				case OP_GT:
				case OP_DIV:
				case OP_QUESTION:
				case OP_LBRACKET:
				case OP_RBRACKET:
				case OP_UNDER:
				case OP_AT:
				case OP_HASH:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				}
			}
		else {
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_EQ:
		{
			alias();
			A_AST = returnAST;
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case OP_LBRACE:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_GT:
		case OP_DIV:
		case OP_QUESTION:
		case OP_LBRACKET:
		case OP_RBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		if ((_tokenSet_62.member(LA(1))) && (_tokenSet_61.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			attributes();
			X_AST = returnAST;
		}
		else if ((_tokenSet_58.member(LA(1))) && (_tokenSet_57.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		subRhsItem_AST = currentAST.root;
		subRhsItem_AST = astFactory.make( (new ASTArray(15))->add(astFactory.create(ZruleItem))->add(C1g_AST)->add(C2at_AST)->add(C2s_AST)->add(C3at_AST)->add(C3s_AST)->add(C4g1_AST)->add(C4g2_AST)->add(C4s1_AST)->add(C4s2_AST)->add(A_AST)->add(C2a_AST)->add(C4a1_AST)->add(C4a2_AST)->add(X_AST));
		currentAST.root = subRhsItem_AST;
		currentAST.child = subRhsItem_AST!=nullAST &&subRhsItem_AST->getFirstChild()!=nullAST ?
			subRhsItem_AST->getFirstChild() : subRhsItem_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_58);
	}
	returnAST = subRhsItem_AST;
}

void GrpParser::selectorAfterAt() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST selectorAfterAt_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		slotIndicatorAfterAt();
		X_AST = returnAST;
		selectorAfterAt_AST = currentAST.root;
		selectorAfterAt_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zselector))->add(X_AST));
		currentAST.root = selectorAfterAt_AST;
		currentAST.child = selectorAfterAt_AST!=nullAST &&selectorAfterAt_AST->getFirstChild()!=nullAST ?
			selectorAfterAt_AST->getFirstChild() : selectorAfterAt_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_59);
	}
	returnAST = selectorAfterAt_AST;
}

void GrpParser::associations() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST associations_AST = nullAST;
	RefAST S1_AST = nullAST;
	RefAST S2_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case IDENT:
		case LIT_INT:
		case Qalias:
		{
			slotIndicator();
			S1_AST = returnAST;
			break;
		}
		case OP_LPAREN:
		{
			assocsList();
			S2_AST = returnAST;
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		associations_AST = currentAST.root;
		associations_AST = astFactory.make( (new ASTArray(3))->add(astFactory.create(Zassocs))->add(S1_AST)->add(S2_AST));
		currentAST.root = associations_AST;
		currentAST.child = associations_AST!=nullAST &&associations_AST->getFirstChild()!=nullAST ?
			associations_AST->getFirstChild() : associations_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_63);
	}
	returnAST = associations_AST;
}

void GrpParser::selector() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST selector_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		slotIndicator();
		X_AST = returnAST;
		selector_AST = currentAST.root;
		selector_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zselector))->add(X_AST));
		currentAST.root = selector_AST;
		currentAST.child = selector_AST!=nullAST &&selector_AST->getFirstChild()!=nullAST ?
			selector_AST->getFirstChild() : selector_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_59);
	}
	returnAST = selector_AST;
}

void GrpParser::alias() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST alias_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;

	try {      // for error handling
		RefAST tmp206_AST = nullAST;
		tmp206_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp206_AST);
		match(OP_EQ);
		I = LT(1);
		I_AST = astFactory.create(I);
		astFactory.addASTChild(currentAST, I_AST);
		match(IDENT);
		alias_AST = currentAST.root;
		alias_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zalias))->add(I_AST));
		currentAST.root = alias_AST;
		currentAST.child = alias_AST!=nullAST &&alias_AST->getFirstChild()!=nullAST ?
			alias_AST->getFirstChild() : alias_AST;
		currentAST.advanceChildToEnd();
		alias_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_64);
	}
	returnAST = alias_AST;
}

void GrpParser::slotIndicator() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST slotIndicator_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LIT_INT:
		{
			RefAST tmp207_AST = nullAST;
			tmp207_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp207_AST);
			match(LIT_INT);
			break;
		}
		case IDENT:
		{
			RefAST tmp208_AST = nullAST;
			tmp208_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp208_AST);
			match(IDENT);
			break;
		}
		case Qalias:
		{
			RefAST tmp209_AST = nullAST;
			tmp209_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp209_AST);
			match(Qalias);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		slotIndicator_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_65);
	}
	returnAST = slotIndicator_AST;
}

void GrpParser::assocsList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST assocsList_AST = nullAST;

	try {      // for error handling
		{
		RefAST tmp210_AST = nullAST;
		tmp210_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		{
		switch ( LA(1)) {
		case IDENT:
		case LIT_INT:
		case Qalias:
		{
			slotIndicator();
			astFactory.addASTChild(currentAST, returnAST);
			{
			do {
				if ((_tokenSet_66.member(LA(1)))) {
					{
					switch ( LA(1)) {
					case OP_COMMA:
					{
						RefAST tmp211_AST = nullAST;
						tmp211_AST = astFactory.create(LT(1));
						match(OP_COMMA);
						break;
					}
					case IDENT:
					case LIT_INT:
					case Qalias:
					{
						break;
					}
					default:
					{
						throw NoViableAltException(LT(1));
					}
					}
					}
					slotIndicator();
					astFactory.addASTChild(currentAST, returnAST);
				}
				else {
					goto _loop259;
				}

			} while (true);
			_loop259:;
			}
			break;
		}
		case OP_RPAREN:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp212_AST = nullAST;
		tmp212_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		}
		assocsList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_63);
	}
	returnAST = assocsList_AST;
}

void GrpParser::slotIndicatorAfterAt() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST slotIndicatorAfterAt_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LIT_INT:
		{
			RefAST tmp213_AST = nullAST;
			tmp213_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp213_AST);
			match(LIT_INT);
			break;
		}
		case Qalias:
		{
			RefAST tmp214_AST = nullAST;
			tmp214_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp214_AST);
			match(Qalias);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		slotIndicatorAfterAt_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_59);
	}
	returnAST = slotIndicatorAfterAt_AST;
}

void GrpParser::posEntry() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posEntry_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LITERAL_if:
		{
			posIf();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			posRule();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_pass:
		{
			posPass();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_environment:
		{
			posEnv();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LITERAL_table:
		{
			tableDecl();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		posEntry_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = posEntry_AST;
}

void GrpParser::posIf() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posIf_AST = nullAST;
	RefToken  C1k = nullToken;
	RefAST C1k_AST = nullAST;
	RefAST E_AST = nullAST;
	RefAST C1_AST = nullAST;
	RefAST C1x_AST = nullAST;
	RefAST C2_AST = nullAST;
	RefAST C2x_AST = nullAST;
	RefAST C3_AST = nullAST;
	RefToken  C3k = nullToken;
	RefAST C3k_AST = nullAST;
	RefAST C3x_AST = nullAST;

	try {      // for error handling
		C1k = LT(1);
		C1k_AST = astFactory.create(C1k);
		match(LITERAL_if);
		RefAST tmp215_AST = nullAST;
		tmp215_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		expr();
		E_AST = returnAST;
		RefAST tmp216_AST = nullAST;
		tmp216_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		posEntryList();
		C1x_AST = returnAST;
		C1_AST = astFactory.make( (new ASTArray(3))->add(C1k_AST)->add(E_AST)->add(C1x_AST));
		}
		{
		if (((LA(1) >= LITERAL_else && LA(1) <= LITERAL_elseif)) && (_tokenSet_46.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			posElseIfList();
			C2x_AST = returnAST;
			C2_AST = C2x_AST;
		}
		else if ((LA(1)==LITERAL_else||LA(1)==LITERAL_endif) && (_tokenSet_46.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		{
		switch ( LA(1)) {
		case LITERAL_else:
		{
			C3k = LT(1);
			C3k_AST = astFactory.create(C3k);
			match(LITERAL_else);
			posEntryList();
			C3x_AST = returnAST;
			C3_AST = astFactory.make( (new ASTArray(2))->add(C3k_AST)->add(C3x_AST));
			break;
		}
		case LITERAL_endif:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp217_AST = nullAST;
		tmp217_AST = astFactory.create(LT(1));
		match(LITERAL_endif);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp218_AST = nullAST;
			tmp218_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case LITERAL_else:
		case LITERAL_endif:
		case Zelseif:
		case LITERAL_elseif:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		posIf_AST = currentAST.root;
		posIf_AST = astFactory.make( (new ASTArray(4))->add(astFactory.create(ZifStruct))->add(C1_AST)->add(C2_AST)->add(C3_AST));
		currentAST.root = posIf_AST;
		currentAST.child = posIf_AST!=nullAST &&posIf_AST->getFirstChild()!=nullAST ?
			posIf_AST->getFirstChild() : posIf_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = posIf_AST;
}

void GrpParser::posRule() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posRule_AST = nullAST;
	RefAST R_AST = nullAST;
	RefAST R1_AST = nullAST;
	RefAST C_AST = nullAST;
	RefAST C1_AST = nullAST;

	try {      // for error handling
		{
		posRhs();
		R1_AST = returnAST;
		R_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zrhs))->add(R1_AST));
		}
		{
		switch ( LA(1)) {
		case OP_DIV:
		{
			RefAST tmp219_AST = nullAST;
			tmp219_AST = astFactory.create(LT(1));
			match(OP_DIV);
			{
			context();
			C1_AST = returnAST;
			C_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zcontext))->add(C1_AST));
			}
			break;
		}
		case OP_SEMI:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp220_AST = nullAST;
		tmp220_AST = astFactory.create(LT(1));
		match(OP_SEMI);
		posRule_AST = currentAST.root;
		posRule_AST = astFactory.make( (new ASTArray(3))->add(astFactory.create(Zrule))->add(R_AST)->add(C_AST));
		currentAST.root = posRule_AST;
		currentAST.child = posRule_AST!=nullAST &&posRule_AST->getFirstChild()!=nullAST ?
			posRule_AST->getFirstChild() : posRule_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = posRule_AST;
}

void GrpParser::posPass() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posPass_AST = nullAST;

	try {      // for error handling
		RefAST tmp221_AST = nullAST;
		tmp221_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp221_AST);
		match(LITERAL_pass);
		RefAST tmp222_AST = nullAST;
		tmp222_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		RefAST tmp223_AST = nullAST;
		tmp223_AST = astFactory.create(LT(1));
		astFactory.addASTChild(currentAST, tmp223_AST);
		match(LIT_INT);
		RefAST tmp224_AST = nullAST;
		tmp224_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp225_AST = nullAST;
			tmp225_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				posEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop288;
			}

		} while (true);
		_loop288:;
		}
		RefAST tmp226_AST = nullAST;
		tmp226_AST = astFactory.create(LT(1));
		match(LITERAL_endpass);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp227_AST = nullAST;
			tmp227_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case LITERAL_else:
		case LITERAL_endif:
		case Zelseif:
		case LITERAL_elseif:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		posPass_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = posPass_AST;
}

void GrpParser::posEnv() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posEnv_AST = nullAST;

	try {      // for error handling
		RefAST tmp228_AST = nullAST;
		tmp228_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp228_AST);
		match(LITERAL_environment);
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			directives();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp229_AST = nullAST;
			tmp229_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_if:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				posEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop282;
			}

		} while (true);
		_loop282:;
		}
		RefAST tmp230_AST = nullAST;
		tmp230_AST = astFactory.create(LT(1));
		match(LITERAL_endenvironment);
		{
		switch ( LA(1)) {
		case OP_SEMI:
		{
			RefAST tmp231_AST = nullAST;
			tmp231_AST = astFactory.create(LT(1));
			match(OP_SEMI);
			break;
		}
		case OP_LPAREN:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case LITERAL_pass:
		case LITERAL_endpass:
		case LITERAL_if:
		case LITERAL_else:
		case LITERAL_endif:
		case Zelseif:
		case LITERAL_elseif:
		case OP_LBRACKET:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		posEnv_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_45);
	}
	returnAST = posEnv_AST;
}

void GrpParser::posEntryList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posEntryList_AST = nullAST;

	try {      // for error handling
		{
		do {
			if ((_tokenSet_12.member(LA(1)))) {
				posEntry();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop302;
			}

		} while (true);
		_loop302:;
		}
		posEntryList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_47);
	}
	returnAST = posEntryList_AST;
}

void GrpParser::posElseIfList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posElseIfList_AST = nullAST;

	try {      // for error handling
		{
		do {
			if ((LA(1)==Zelseif||LA(1)==LITERAL_elseif)) {
				posElseIf();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop297;
			}

		} while (true);
		_loop297:;
		}
		posElseIfList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_48);
	}
	returnAST = posElseIfList_AST;
}

void GrpParser::posElseIf() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posElseIf_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case Zelseif:
		{
			RefAST tmp232_AST = nullAST;
			tmp232_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp232_AST);
			match(Zelseif);
			break;
		}
		case LITERAL_elseif:
		{
			RefAST tmp233_AST = nullAST;
			tmp233_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp233_AST);
			match(LITERAL_elseif);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp234_AST = nullAST;
		tmp234_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		expr();
		astFactory.addASTChild(currentAST, returnAST);
		RefAST tmp235_AST = nullAST;
		tmp235_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		posEntryList();
		astFactory.addASTChild(currentAST, returnAST);
		posElseIf_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_47);
	}
	returnAST = posElseIf_AST;
}

void GrpParser::posRhs() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posRhs_AST = nullAST;

	try {      // for error handling
		{
		int _cnt309=0;
		do {
			if ((_tokenSet_49.member(LA(1))) && (_tokenSet_67.member(LA(2))) && (_tokenSet_68.member(LA(3)))) {
				posRhsRange();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				if ( _cnt309>=1 ) { goto _loop309; } else {throw NoViableAltException(LT(1));}
			}

			_cnt309++;
		} while (true);
		_loop309:;
		}
		posRhs_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_69);
	}
	returnAST = posRhs_AST;
}

void GrpParser::posRhsRange() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posRhsRange_AST = nullAST;
	RefAST X1_AST = nullAST;
	RefAST X2_AST = nullAST;
	RefToken  Q = nullToken;
	RefAST Q_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_LBRACKET:
		{
			posRhsList();
			X1_AST = returnAST;
			posRhsRange_AST = currentAST.root;
			posRhsRange_AST = X1_AST;
			currentAST.root = posRhsRange_AST;
			currentAST.child = posRhsRange_AST!=nullAST &&posRhsRange_AST->getFirstChild()!=nullAST ?
				posRhsRange_AST->getFirstChild() : posRhsRange_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case OP_LPAREN:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_UNDER:
		case OP_AT:
		case OP_HASH:
		{
			posRhsItem();
			X2_AST = returnAST;
			{
			if ((LA(1)==OP_QUESTION)) {
				Q = LT(1);
				Q_AST = astFactory.create(Q);
				match(OP_QUESTION);
				posRhsRange_AST = currentAST.root;
				posRhsRange_AST = astFactory.make( (new ASTArray(2))->add(Q_AST)->add(X2_AST));
				currentAST.root = posRhsRange_AST;
				currentAST.child = posRhsRange_AST!=nullAST &&posRhsRange_AST->getFirstChild()!=nullAST ?
					posRhsRange_AST->getFirstChild() : posRhsRange_AST;
				currentAST.advanceChildToEnd();
			}
			else if ((_tokenSet_69.member(LA(1))) && (_tokenSet_70.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
				posRhsRange_AST = currentAST.root;
				posRhsRange_AST = X2_AST;
				currentAST.root = posRhsRange_AST;
				currentAST.child = posRhsRange_AST!=nullAST &&posRhsRange_AST->getFirstChild()!=nullAST ?
					posRhsRange_AST->getFirstChild() : posRhsRange_AST;
				currentAST.advanceChildToEnd();
			}
			else if ((_tokenSet_69.member(LA(1))) && (_tokenSet_70.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_69);
	}
	returnAST = posRhsRange_AST;
}

void GrpParser::posRhsList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posRhsList_AST = nullAST;

	try {      // for error handling
		{
		RefAST tmp236_AST = nullAST;
		tmp236_AST = astFactory.create(LT(1));
		match(OP_LBRACKET);
		{
		int _cnt316=0;
		do {
			if ((_tokenSet_49.member(LA(1)))) {
				posRhs();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				if ( _cnt316>=1 ) { goto _loop316; } else {throw NoViableAltException(LT(1));}
			}

			_cnt316++;
		} while (true);
		_loop316:;
		}
		RefAST tmp237_AST = nullAST;
		tmp237_AST = astFactory.create(LT(1));
		match(OP_RBRACKET);
		}
		RefAST tmp238_AST = nullAST;
		tmp238_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp238_AST);
		match(OP_QUESTION);
		posRhsList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_69);
	}
	returnAST = posRhsList_AST;
}

void GrpParser::posRhsItem() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST posRhsItem_AST = nullAST;

	try {      // for error handling
		subRhsItem();
		astFactory.addASTChild(currentAST, returnAST);
		posRhsItem_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_71);
	}
	returnAST = posRhsItem_AST;
}

void GrpParser::contextRange() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST contextRange_AST = nullAST;
	RefAST X1_AST = nullAST;
	RefToken  X2 = nullToken;
	RefAST X2_AST = nullAST;
	RefAST X3_AST = nullAST;
	RefToken  Q = nullToken;
	RefAST Q_AST = nullAST;

	try {      // for error handling
		{
		{
		switch ( LA(1)) {
		case OP_LBRACKET:
		{
			contextList();
			X1_AST = returnAST;
			contextRange_AST = currentAST.root;
			contextRange_AST = X1_AST;
			currentAST.root = contextRange_AST;
			currentAST.child = contextRange_AST!=nullAST &&contextRange_AST->getFirstChild()!=nullAST ?
				contextRange_AST->getFirstChild() : contextRange_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case OP_CARET:
		{
			X2 = LT(1);
			X2_AST = astFactory.create(X2);
			match(OP_CARET);
			contextRange_AST = currentAST.root;
			contextRange_AST = X2_AST;
			currentAST.root = contextRange_AST;
			currentAST.child = contextRange_AST!=nullAST &&contextRange_AST->getFirstChild()!=nullAST ?
				contextRange_AST->getFirstChild() : contextRange_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		case OP_LPAREN:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_UNDER:
		case OP_HASH:
		{
			contextItem();
			X3_AST = returnAST;
			{
			if ((LA(1)==OP_QUESTION)) {
				Q = LT(1);
				Q_AST = astFactory.create(Q);
				match(OP_QUESTION);
				contextRange_AST = currentAST.root;
				contextRange_AST = astFactory.make( (new ASTArray(2))->add(Q_AST)->add(X3_AST));
				currentAST.root = contextRange_AST;
				currentAST.child = contextRange_AST!=nullAST &&contextRange_AST->getFirstChild()!=nullAST ?
					contextRange_AST->getFirstChild() : contextRange_AST;
				currentAST.advanceChildToEnd();
			}
			else if ((_tokenSet_72.member(LA(1))) && (_tokenSet_73.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
				contextRange_AST = currentAST.root;
				contextRange_AST = X3_AST;
				currentAST.root = contextRange_AST;
				currentAST.child = contextRange_AST!=nullAST &&contextRange_AST->getFirstChild()!=nullAST ?
					contextRange_AST->getFirstChild() : contextRange_AST;
				currentAST.advanceChildToEnd();
			}
			else if ((_tokenSet_72.member(LA(1))) && (_tokenSet_73.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			}
			else {
				throw NoViableAltException(LT(1));
			}

			}
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_72);
	}
	returnAST = contextRange_AST;
}

void GrpParser::contextList() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST contextList_AST = nullAST;

	try {      // for error handling
		RefAST tmp239_AST = nullAST;
		tmp239_AST = astFactory.create(LT(1));
		match(OP_LBRACKET);
		{
		int _cnt332=0;
		do {
			if ((_tokenSet_55.member(LA(1)))) {
				contextRange();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				if ( _cnt332>=1 ) { goto _loop332; } else {throw NoViableAltException(LT(1));}
			}

			_cnt332++;
		} while (true);
		_loop332:;
		}
		RefAST tmp240_AST = nullAST;
		tmp240_AST = astFactory.create(LT(1));
		match(OP_RBRACKET);
		RefAST tmp241_AST = nullAST;
		tmp241_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp241_AST);
		match(OP_QUESTION);
		contextList_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_72);
	}
	returnAST = contextList_AST;
}

void GrpParser::contextItem() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST contextItem_AST = nullAST;
	RefToken  C1 = nullToken;
	RefAST C1_AST = nullAST;
	RefToken  C2 = nullToken;
	RefAST C2_AST = nullAST;
	RefAST C3_AST = nullAST;
	RefAST A_AST = nullAST;
	RefAST Y_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_HASH:
		{
			C1 = LT(1);
			C1_AST = astFactory.create(C1);
			match(OP_HASH);
			break;
		}
		case OP_UNDER:
		{
			C2 = LT(1);
			C2_AST = astFactory.create(C2);
			match(OP_UNDER);
			break;
		}
		case OP_LPAREN:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		{
			glyphSpec();
			C3_AST = returnAST;
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_EQ:
		{
			alias();
			A_AST = returnAST;
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case OP_LBRACE:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_QUESTION:
		case OP_LBRACKET:
		case OP_RBRACKET:
		case OP_UNDER:
		case OP_HASH:
		case OP_CARET:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		{
		switch ( LA(1)) {
		case OP_LBRACE:
		{
			constraint();
			Y_AST = returnAST;
			break;
		}
		case OP_LPAREN:
		case OP_SEMI:
		case IDENT:
		case LITERAL_pseudo:
		case LIT_UHEX:
		case LITERAL_codepoint:
		case LITERAL_glyphid:
		case LITERAL_postscript:
		case LITERAL_unicode:
		case OP_QUESTION:
		case OP_LBRACKET:
		case OP_RBRACKET:
		case OP_UNDER:
		case OP_HASH:
		case OP_CARET:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		contextItem_AST = currentAST.root;
		contextItem_AST = astFactory.make( (new ASTArray(6))->add(astFactory.create(ZruleItem))->add(C1_AST)->add(C2_AST)->add(C3_AST)->add(A_AST)->add(Y_AST));
		currentAST.root = contextItem_AST;
		currentAST.child = contextItem_AST!=nullAST &&contextItem_AST->getFirstChild()!=nullAST ?
			contextItem_AST->getFirstChild() : contextItem_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_74);
	}
	returnAST = contextItem_AST;
}

void GrpParser::constraint() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST constraint_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		RefAST tmp242_AST = nullAST;
		tmp242_AST = astFactory.create(LT(1));
		match(OP_LBRACE);
		{
		switch ( LA(1)) {
		case OP_LPAREN:
		case IDENT:
		case LIT_INT:
		case LIT_STRING:
		case OP_AT:
		case LITERAL_position:
		case OP_PLUS:
		case OP_MINUS:
		case OP_NOT:
		case LITERAL_true:
		case LITERAL_false:
		case LITERAL_max:
		case LITERAL_min:
		{
			expr();
			X_AST = returnAST;
			break;
		}
		case OP_RBRACE:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp243_AST = nullAST;
		tmp243_AST = astFactory.create(LT(1));
		match(OP_RBRACE);
		constraint_AST = currentAST.root;
		constraint_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zconstraint))->add(X_AST));
		currentAST.root = constraint_AST;
		currentAST.child = constraint_AST!=nullAST &&constraint_AST->getFirstChild()!=nullAST ?
			constraint_AST->getFirstChild() : constraint_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_74);
	}
	returnAST = constraint_AST;
}

void GrpParser::otherEntry() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST otherEntry_AST = nullAST;
	RefAST X_AST = nullAST;

	try {      // for error handling
		{
		if ((_tokenSet_75.member(LA(1))) && ((LA(2) >= OP_EQ && LA(2) <= AT_IDENT)) && (_tokenSet_4.member(LA(3)))) {
			{
			RefAST tmp244_AST = nullAST;
			tmp244_AST = astFactory.create(LT(1));
			match(_tokenSet_75);
			}
		}
		else if ((LA(1)==LITERAL_environment||LA(1)==LITERAL_table) && (_tokenSet_76.member(LA(2))) && ((LA(3) >= OP_EQ && LA(3) <= AT_IDENT))) {
			topDecl();
			X_AST = returnAST;
			otherEntry_AST = currentAST.root;
			otherEntry_AST = X_AST;
			currentAST.root = otherEntry_AST;
			currentAST.child = otherEntry_AST!=nullAST &&otherEntry_AST->getFirstChild()!=nullAST ?
				otherEntry_AST->getFirstChild() : otherEntry_AST;
			currentAST.advanceChildToEnd();
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_9);
	}
	returnAST = otherEntry_AST;
}

void GrpParser::attrAssignOp() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST attrAssignOp_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_EQ:
		{
			RefAST tmp245_AST = nullAST;
			tmp245_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp245_AST);
			match(OP_EQ);
			break;
		}
		case OP_PLUSEQUAL:
		{
			RefAST tmp246_AST = nullAST;
			tmp246_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp246_AST);
			match(OP_PLUSEQUAL);
			break;
		}
		case OP_MINUSEQUAL:
		{
			RefAST tmp247_AST = nullAST;
			tmp247_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp247_AST);
			match(OP_MINUSEQUAL);
			break;
		}
		case OP_DIVEQUAL:
		{
			RefAST tmp248_AST = nullAST;
			tmp248_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp248_AST);
			match(OP_DIVEQUAL);
			break;
		}
		case OP_MULTEQUAL:
		{
			RefAST tmp249_AST = nullAST;
			tmp249_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp249_AST);
			match(OP_MULTEQUAL);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		attrAssignOp_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_1);
	}
	returnAST = attrAssignOp_AST;
}

void GrpParser::function() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST function_AST = nullAST;
	RefToken  I = nullToken;
	RefAST I_AST = nullAST;
	RefAST E_AST = nullAST;

	try {      // for error handling
		I = LT(1);
		I_AST = astFactory.create(I);
		match(IDENT);
		RefAST tmp250_AST = nullAST;
		tmp250_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		{
		switch ( LA(1)) {
		case OP_LPAREN:
		case IDENT:
		case LIT_INT:
		case LIT_STRING:
		case OP_AT:
		case LITERAL_position:
		case OP_PLUS:
		case OP_MINUS:
		case OP_NOT:
		case LITERAL_true:
		case LITERAL_false:
		case LITERAL_max:
		case LITERAL_min:
		{
			exprList();
			E_AST = returnAST;
			break;
		}
		case OP_RPAREN:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp251_AST = nullAST;
		tmp251_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		function_AST = currentAST.root;
		function_AST = astFactory.make( (new ASTArray(3))->add(astFactory.create(Zfunction))->add(I_AST)->add(E_AST));
		currentAST.root = function_AST;
		currentAST.child = function_AST!=nullAST &&function_AST->getFirstChild()!=nullAST ?
			function_AST->getFirstChild() : function_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_32);
	}
	returnAST = function_AST;
}

void GrpParser::conditionalExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST conditionalExpr_AST = nullAST;

	try {      // for error handling
		logicalOrExpr();
		astFactory.addASTChild(currentAST, returnAST);
		{
		switch ( LA(1)) {
		case OP_QUESTION:
		{
			RefAST tmp252_AST = nullAST;
			tmp252_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp252_AST);
			match(OP_QUESTION);
			expr();
			astFactory.addASTChild(currentAST, returnAST);
			RefAST tmp253_AST = nullAST;
			tmp253_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp253_AST);
			match(OP_COLON);
			expr();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case Token::EOF_TYPE:
		case OP_RPAREN:
		case OP_SEMI:
		case LITERAL_environment:
		case LITERAL_endenvironment:
		case OP_RBRACE:
		case IDENT:
		case LITERAL_table:
		case LITERAL_endtable:
		case OP_COMMA:
		case OP_COLON:
		case LITERAL_position:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		conditionalExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_8);
	}
	returnAST = conditionalExpr_AST;
}

void GrpParser::logicalOrExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST logicalOrExpr_AST = nullAST;

	try {      // for error handling
		logicalAndExpr();
		astFactory.addASTChild(currentAST, returnAST);
		{
		do {
			if ((LA(1)==OP_OR)) {
				RefAST tmp254_AST = nullAST;
				tmp254_AST = astFactory.create(LT(1));
				astFactory.makeASTRoot(currentAST, tmp254_AST);
				match(OP_OR);
				logicalAndExpr();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop374;
			}

		} while (true);
		_loop374:;
		}
		logicalOrExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_77);
	}
	returnAST = logicalOrExpr_AST;
}

void GrpParser::logicalAndExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST logicalAndExpr_AST = nullAST;

	try {      // for error handling
		comparativeExpr();
		astFactory.addASTChild(currentAST, returnAST);
		{
		do {
			if ((LA(1)==OP_AND)) {
				RefAST tmp255_AST = nullAST;
				tmp255_AST = astFactory.create(LT(1));
				astFactory.makeASTRoot(currentAST, tmp255_AST);
				match(OP_AND);
				comparativeExpr();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop377;
			}

		} while (true);
		_loop377:;
		}
		logicalAndExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_78);
	}
	returnAST = logicalAndExpr_AST;
}

void GrpParser::comparativeExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST comparativeExpr_AST = nullAST;

	try {      // for error handling
		additiveExpr();
		astFactory.addASTChild(currentAST, returnAST);
		{
		do {
			if ((_tokenSet_79.member(LA(1)))) {
				{
				switch ( LA(1)) {
				case OP_EQUALEQUAL:
				{
					RefAST tmp256_AST = nullAST;
					tmp256_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp256_AST);
					match(OP_EQUALEQUAL);
					break;
				}
				case OP_NE:
				{
					RefAST tmp257_AST = nullAST;
					tmp257_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp257_AST);
					match(OP_NE);
					break;
				}
				case OP_LT:
				{
					RefAST tmp258_AST = nullAST;
					tmp258_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp258_AST);
					match(OP_LT);
					break;
				}
				case OP_LE:
				{
					RefAST tmp259_AST = nullAST;
					tmp259_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp259_AST);
					match(OP_LE);
					break;
				}
				case OP_GT:
				{
					RefAST tmp260_AST = nullAST;
					tmp260_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp260_AST);
					match(OP_GT);
					break;
				}
				case OP_GE:
				{
					RefAST tmp261_AST = nullAST;
					tmp261_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp261_AST);
					match(OP_GE);
					break;
				}
				case OP_EQ:
				{
					RefAST tmp262_AST = nullAST;
					tmp262_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp262_AST);
					match(OP_EQ);
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				additiveExpr();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop381;
			}

		} while (true);
		_loop381:;
		}
		comparativeExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_80);
	}
	returnAST = comparativeExpr_AST;
}

void GrpParser::additiveExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST additiveExpr_AST = nullAST;

	try {      // for error handling
		multiplicativeExpr();
		astFactory.addASTChild(currentAST, returnAST);
		{
		do {
			if ((LA(1)==OP_PLUS||LA(1)==OP_MINUS)) {
				{
				switch ( LA(1)) {
				case OP_PLUS:
				{
					RefAST tmp263_AST = nullAST;
					tmp263_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp263_AST);
					match(OP_PLUS);
					break;
				}
				case OP_MINUS:
				{
					RefAST tmp264_AST = nullAST;
					tmp264_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp264_AST);
					match(OP_MINUS);
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				multiplicativeExpr();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop385;
			}

		} while (true);
		_loop385:;
		}
		additiveExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_81);
	}
	returnAST = additiveExpr_AST;
}

void GrpParser::multiplicativeExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST multiplicativeExpr_AST = nullAST;

	try {      // for error handling
		unaryExpr();
		astFactory.addASTChild(currentAST, returnAST);
		{
		do {
			if ((LA(1)==OP_DIV||LA(1)==OP_MULT)) {
				{
				switch ( LA(1)) {
				case OP_MULT:
				{
					RefAST tmp265_AST = nullAST;
					tmp265_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp265_AST);
					match(OP_MULT);
					break;
				}
				case OP_DIV:
				{
					RefAST tmp266_AST = nullAST;
					tmp266_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(currentAST, tmp266_AST);
					match(OP_DIV);
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				unaryExpr();
				astFactory.addASTChild(currentAST, returnAST);
			}
			else {
				goto _loop389;
			}

		} while (true);
		_loop389:;
		}
		multiplicativeExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_82);
	}
	returnAST = multiplicativeExpr_AST;
}

void GrpParser::unaryExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST unaryExpr_AST = nullAST;

	try {      // for error handling
		{
		if ((LA(1)==OP_MINUS||LA(1)==OP_NOT) && (_tokenSet_83.member(LA(2))) && (_tokenSet_84.member(LA(3)))) {
			{
			{
			switch ( LA(1)) {
			case OP_NOT:
			{
				RefAST tmp267_AST = nullAST;
				tmp267_AST = astFactory.create(LT(1));
				astFactory.makeASTRoot(currentAST, tmp267_AST);
				match(OP_NOT);
				break;
			}
			case OP_MINUS:
			{
				RefAST tmp268_AST = nullAST;
				tmp268_AST = astFactory.create(LT(1));
				astFactory.makeASTRoot(currentAST, tmp268_AST);
				match(OP_MINUS);
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			singleExpr();
			astFactory.addASTChild(currentAST, returnAST);
			}
		}
		else if ((_tokenSet_83.member(LA(1))) && (_tokenSet_84.member(LA(2))) && (_tokenSet_4.member(LA(3)))) {
			singleExpr();
			astFactory.addASTChild(currentAST, returnAST);
		}
		else {
			throw NoViableAltException(LT(1));
		}

		}
		unaryExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_85);
	}
	returnAST = unaryExpr_AST;
}

void GrpParser::singleExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST singleExpr_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_LPAREN:
		{
			RefAST tmp269_AST = nullAST;
			tmp269_AST = astFactory.create(LT(1));
			match(OP_LPAREN);
			expr();
			astFactory.addASTChild(currentAST, returnAST);
			RefAST tmp270_AST = nullAST;
			tmp270_AST = astFactory.create(LT(1));
			match(OP_RPAREN);
			break;
		}
		case LIT_STRING:
		{
			RefAST tmp271_AST = nullAST;
			tmp271_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp271_AST);
			match(LIT_STRING);
			break;
		}
		case LITERAL_max:
		case LITERAL_min:
		{
			arithFunction();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case IDENT:
		case OP_AT:
		case LITERAL_position:
		{
			lookupExpr();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case LIT_INT:
		case OP_PLUS:
		case OP_MINUS:
		case LITERAL_true:
		case LITERAL_false:
		{
			signedInt();
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		singleExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_85);
	}
	returnAST = singleExpr_AST;
}

void GrpParser::arithFunction() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST arithFunction_AST = nullAST;
	RefAST E1_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case LITERAL_max:
		{
			RefAST tmp272_AST = nullAST;
			tmp272_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp272_AST);
			match(LITERAL_max);
			break;
		}
		case LITERAL_min:
		{
			RefAST tmp273_AST = nullAST;
			tmp273_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(currentAST, tmp273_AST);
			match(LITERAL_min);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp274_AST = nullAST;
		tmp274_AST = astFactory.create(LT(1));
		match(OP_LPAREN);
		{
		switch ( LA(1)) {
		case OP_LPAREN:
		case IDENT:
		case LIT_INT:
		case LIT_STRING:
		case OP_AT:
		case LITERAL_position:
		case OP_PLUS:
		case OP_MINUS:
		case OP_NOT:
		case LITERAL_true:
		case LITERAL_false:
		case LITERAL_max:
		case LITERAL_min:
		{
			exprList();
			E1_AST = returnAST;
			astFactory.addASTChild(currentAST, returnAST);
			break;
		}
		case OP_RPAREN:
		{
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		RefAST tmp275_AST = nullAST;
		tmp275_AST = astFactory.create(LT(1));
		match(OP_RPAREN);
		arithFunction_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_85);
	}
	returnAST = arithFunction_AST;
}

void GrpParser::lookupExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST lookupExpr_AST = nullAST;
	RefAST S_AST = nullAST;
	RefAST I1_AST = nullAST;
	RefAST C1_AST = nullAST;
	RefAST I2_AST = nullAST;
	RefAST C2_AST = nullAST;

	try {      // for error handling
		{
		switch ( LA(1)) {
		case OP_AT:
		{
			selectorExpr();
			S_AST = returnAST;
			{
			switch ( LA(1)) {
			case OP_DOT:
			{
				RefAST tmp276_AST = nullAST;
				tmp276_AST = astFactory.create(LT(1));
				match(OP_DOT);
				identDot();
				I1_AST = returnAST;
				{
				switch ( LA(1)) {
				case OP_DOT:
				{
					clusterExpr();
					C1_AST = returnAST;
					break;
				}
				case Token::EOF_TYPE:
				case OP_EQ:
				case OP_RPAREN:
				case OP_SEMI:
				case LITERAL_environment:
				case LITERAL_endenvironment:
				case OP_RBRACE:
				case IDENT:
				case LITERAL_table:
				case LITERAL_endtable:
				case OP_COMMA:
				case OP_GT:
				case OP_DIV:
				case OP_QUESTION:
				case OP_COLON:
				case LITERAL_position:
				case OP_OR:
				case OP_AND:
				case OP_EQUALEQUAL:
				case OP_NE:
				case OP_LT:
				case OP_LE:
				case OP_GE:
				case OP_PLUS:
				case OP_MINUS:
				case OP_MULT:
				{
					break;
				}
				default:
				{
					throw NoViableAltException(LT(1));
				}
				}
				}
				lookupExpr_AST = currentAST.root;
				lookupExpr_AST = astFactory.make( (new ASTArray(4))->add(astFactory.create(Zlookup))->add(S_AST)->add(I1_AST)->add(C1_AST));
				currentAST.root = lookupExpr_AST;
				currentAST.child = lookupExpr_AST!=nullAST &&lookupExpr_AST->getFirstChild()!=nullAST ?
					lookupExpr_AST->getFirstChild() : lookupExpr_AST;
				currentAST.advanceChildToEnd();
				break;
			}
			case Token::EOF_TYPE:
			case OP_EQ:
			case OP_RPAREN:
			case OP_SEMI:
			case LITERAL_environment:
			case LITERAL_endenvironment:
			case OP_RBRACE:
			case IDENT:
			case LITERAL_table:
			case LITERAL_endtable:
			case OP_COMMA:
			case OP_GT:
			case OP_DIV:
			case OP_QUESTION:
			case OP_COLON:
			case LITERAL_position:
			case OP_OR:
			case OP_AND:
			case OP_EQUALEQUAL:
			case OP_NE:
			case OP_LT:
			case OP_LE:
			case OP_GE:
			case OP_PLUS:
			case OP_MINUS:
			case OP_MULT:
			{
				lookupExpr_AST = currentAST.root;
				lookupExpr_AST = S_AST;
				currentAST.root = lookupExpr_AST;
				currentAST.child = lookupExpr_AST!=nullAST &&lookupExpr_AST->getFirstChild()!=nullAST ?
					lookupExpr_AST->getFirstChild() : lookupExpr_AST;
				currentAST.advanceChildToEnd();
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			break;
		}
		case IDENT:
		case LITERAL_position:
		{
			identDot();
			I2_AST = returnAST;
			{
			switch ( LA(1)) {
			case OP_DOT:
			{
				clusterExpr();
				C2_AST = returnAST;
				break;
			}
			case Token::EOF_TYPE:
			case OP_EQ:
			case OP_RPAREN:
			case OP_SEMI:
			case LITERAL_environment:
			case LITERAL_endenvironment:
			case OP_RBRACE:
			case IDENT:
			case LITERAL_table:
			case LITERAL_endtable:
			case OP_COMMA:
			case OP_GT:
			case OP_DIV:
			case OP_QUESTION:
			case OP_COLON:
			case LITERAL_position:
			case OP_OR:
			case OP_AND:
			case OP_EQUALEQUAL:
			case OP_NE:
			case OP_LT:
			case OP_LE:
			case OP_GE:
			case OP_PLUS:
			case OP_MINUS:
			case OP_MULT:
			{
				break;
			}
			default:
			{
				throw NoViableAltException(LT(1));
			}
			}
			}
			lookupExpr_AST = currentAST.root;
			lookupExpr_AST = astFactory.make( (new ASTArray(3))->add(astFactory.create(Zlookup))->add(I2_AST)->add(C2_AST));
			currentAST.root = lookupExpr_AST;
			currentAST.child = lookupExpr_AST!=nullAST &&lookupExpr_AST->getFirstChild()!=nullAST ?
				lookupExpr_AST->getFirstChild() : lookupExpr_AST;
			currentAST.advanceChildToEnd();
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_85);
	}
	returnAST = lookupExpr_AST;
}

void GrpParser::selectorExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST selectorExpr_AST = nullAST;

	try {      // for error handling
		RefAST tmp277_AST = nullAST;
		tmp277_AST = astFactory.create(LT(1));
		astFactory.makeASTRoot(currentAST, tmp277_AST);
		match(OP_AT);
		{
		switch ( LA(1)) {
		case LIT_INT:
		{
			RefAST tmp278_AST = nullAST;
			tmp278_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp278_AST);
			match(LIT_INT);
			break;
		}
		case Qalias:
		{
			RefAST tmp279_AST = nullAST;
			tmp279_AST = astFactory.create(LT(1));
			astFactory.addASTChild(currentAST, tmp279_AST);
			match(Qalias);
			break;
		}
		default:
		{
			throw NoViableAltException(LT(1));
		}
		}
		}
		selectorExpr_AST = currentAST.root;
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_86);
	}
	returnAST = selectorExpr_AST;
}

void GrpParser::clusterExpr() {

	returnAST = nullAST;
	ASTPair currentAST;
	RefAST clusterExpr_AST = nullAST;
	RefToken  C = nullToken;
	RefAST C_AST = nullAST;

	try {      // for error handling
		RefAST tmp280_AST = nullAST;
		tmp280_AST = astFactory.create(LT(1));
		match(OP_DOT);
		C = LT(1);
		C_AST = astFactory.create(C);
		match(LIT_INT);
		clusterExpr_AST = currentAST.root;
		clusterExpr_AST = astFactory.make( (new ASTArray(2))->add(astFactory.create(Zcluster))->add(C_AST));
		currentAST.root = clusterExpr_AST;
		currentAST.child = clusterExpr_AST!=nullAST &&clusterExpr_AST->getFirstChild()!=nullAST ?
			clusterExpr_AST->getFirstChild() : clusterExpr_AST;
		currentAST.advanceChildToEnd();
	}
	catch (ParserException& ex) {
		reportError(ex);
		consume();
		consumeUntil(_tokenSet_85);
	}
	returnAST = clusterExpr_AST;
}

const char* GrpParser::_tokenNames[] = {
	"<0>",
	"EOF",
	"<2>",
	"NULL_TREE_LOOKAHEAD",
	"OP_EQ",
	"OP_PLUSEQUAL",
	"OP_LPAREN",
	"OP_RPAREN",
	"OP_SEMI",
	"\"environment\"",
	"\"endenvironment\"",
	"OP_LBRACE",
	"OP_RBRACE",
	"IDENT",
	"\"table\"",
	"\"endtable\"",
	"\"name\"",
	"LIT_INT",
	"OP_DOT",
	"OP_PLUS_EQUAL",
	"LIT_STRING",
	"OP_COMMA",
	"\"string\"",
	"\"glyph\"",
	"\"pseudo\"",
	"LIT_UHEX",
	"\"codepoint\"",
	"\"glyphid\"",
	"\"postscript\"",
	"\"unicode\"",
	"OP_DOTDOT",
	"LIT_CHAR",
	"\"feature\"",
	"\"language\"",
	"\"languages\"",
	"\"substitution\"",
	"\"pass\"",
	"\"endpass\"",
	"\"if\"",
	"\"else\"",
	"\"endif\"",
	"Zelseif",
	"\"elseif\"",
	"OP_GT",
	"OP_DIV",
	"OP_QUESTION",
	"OP_LBRACKET",
	"OP_RBRACKET",
	"OP_UNDER",
	"OP_AT",
	"OP_COLON",
	"OP_HASH",
	"OP_DOLLAR",
	"Qalias",
	"\"justification\"",
	"\"position\"",
	"\"positioning\"",
	"\"linebreak\"",
	"OP_CARET",
	"OP_MINUSEQUAL",
	"OP_DIVEQUAL",
	"OP_MULTEQUAL",
	"OP_OR",
	"OP_AND",
	"OP_EQUALEQUAL",
	"OP_NE",
	"OP_LT",
	"OP_LE",
	"OP_GE",
	"OP_PLUS",
	"OP_MINUS",
	"OP_MULT",
	"OP_NOT",
	"\"true\"",
	"\"false\"",
	"\"max\"",
	"\"min\"",
	"Zalias",
	"Zassocs",
	"Zattrs",
	"Zcluster",
	"Zcodepage",
	"Zconstraint",
	"Zcontext",
	"Zdirectives",
	"ZdotStruct",
	"Zfeatures",
	"Zfunction",
	"ZifStruct",
	"Zlhs",
	"Zlookup",
	"Zrhs",
	"Zrule",
	"ZruleItem",
	"Zselector",
	"Ztop",
	"ZuHex",
	"WS",
	"COMMENT_SL",
	"COMMENT_ML",
	"ESC",
	"ODIGIT",
	"DIGIT",
	"XDIGIT",
	"SQUOTE",
	"DQUOTE",
	"OP_LINEMARKER",
	"OP_BSLASH",
	"AT_IDENT",
	0
};

const unsigned long GrpParser::_tokenSet_0_data_[] = { 2UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_0(_tokenSet_0_data_,4);
const unsigned long GrpParser::_tokenSet_1_data_[] = { 1187904UL, 8519680UL, 8032UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_1(_tokenSet_1_data_,8);
const unsigned long GrpParser::_tokenSet_2_data_[] = { 3547344UL, 3231856640UL, 8191UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_2(_tokenSet_2_data_,8);
const unsigned long GrpParser::_tokenSet_3_data_[] = { 1468242UL, 3231856640UL, 8191UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_3(_tokenSet_3_data_,8);
const unsigned long GrpParser::_tokenSet_4_data_[] = { 4294967282UL, 4294967295UL, 4294967295UL, 8191UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_4(_tokenSet_4_data_,8);
const unsigned long GrpParser::_tokenSet_5_data_[] = { 26114UL, 8388608UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_5(_tokenSet_5_data_,4);
const unsigned long GrpParser::_tokenSet_6_data_[] = { 2422706UL, 3229890560UL, 255UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_6(_tokenSet_6_data_,8);
const unsigned long GrpParser::_tokenSet_7_data_[] = { 128UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_7(_tokenSet_7_data_,4);
const unsigned long GrpParser::_tokenSet_8_data_[] = { 2160514UL, 8650752UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_8(_tokenSet_8_data_,4);
const unsigned long GrpParser::_tokenSet_9_data_[] = { 4294967280UL, 4294967295UL, 4294967295UL, 8191UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_9(_tokenSet_9_data_,8);
const unsigned long GrpParser::_tokenSet_10_data_[] = { 4352UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_10(_tokenSet_10_data_,4);
const unsigned long GrpParser::_tokenSet_11_data_[] = { 32768UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_11(_tokenSet_11_data_,4);
const unsigned long GrpParser::_tokenSet_12_data_[] = { 1056989760UL, 737360UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_12(_tokenSet_12_data_,4);
const unsigned long GrpParser::_tokenSet_13_data_[] = { 4294934512UL, 4294967295UL, 4294967295UL, 8191UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_13(_tokenSet_13_data_,8);
const unsigned long GrpParser::_tokenSet_14_data_[] = { 189952UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_14(_tokenSet_14_data_,4);
const unsigned long GrpParser::_tokenSet_15_data_[] = { 788496UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_15(_tokenSet_15_data_,4);
const unsigned long GrpParser::_tokenSet_16_data_[] = { 5386304UL, 0UL, 1632UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_16(_tokenSet_16_data_,8);
const unsigned long GrpParser::_tokenSet_17_data_[] = { 194304UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_17(_tokenSet_17_data_,4);
const unsigned long GrpParser::_tokenSet_18_data_[] = { 2357138UL, 3229890566UL, 255UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_18(_tokenSet_18_data_,8);
const unsigned long GrpParser::_tokenSet_19_data_[] = { 7340096UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_19(_tokenSet_19_data_,4);
const unsigned long GrpParser::_tokenSet_20_data_[] = { 7600064UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_20(_tokenSet_20_data_,4);
const unsigned long GrpParser::_tokenSet_21_data_[] = { 58880UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_21(_tokenSet_21_data_,4);
const unsigned long GrpParser::_tokenSet_22_data_[] = { 61184UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_22(_tokenSet_22_data_,4);
const unsigned long GrpParser::_tokenSet_23_data_[] = { 59136UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_23(_tokenSet_23_data_,4);
const unsigned long GrpParser::_tokenSet_24_data_[] = { 262192UL, 939524096UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_24(_tokenSet_24_data_,4);
const unsigned long GrpParser::_tokenSet_25_data_[] = { 1059070016UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_25(_tokenSet_25_data_,4);
const unsigned long GrpParser::_tokenSet_26_data_[] = { 1059123152UL, 69203968UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_26(_tokenSet_26_data_,4);
const unsigned long GrpParser::_tokenSet_27_data_[] = { 1057023808UL, 784384UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_27(_tokenSet_27_data_,4);
const unsigned long GrpParser::_tokenSet_28_data_[] = { 264240UL, 939524096UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_28(_tokenSet_28_data_,4);
const unsigned long GrpParser::_tokenSet_29_data_[] = { 1057027904UL, 784384UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_29(_tokenSet_29_data_,4);
const unsigned long GrpParser::_tokenSet_30_data_[] = { 1188032UL, 8519680UL, 8032UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_30(_tokenSet_30_data_,8);
const unsigned long GrpParser::_tokenSet_31_data_[] = { 1505104UL, 3231856640UL, 8191UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_31(_tokenSet_31_data_,8);
const unsigned long GrpParser::_tokenSet_32_data_[] = { 63232UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_32(_tokenSet_32_data_,4);
const unsigned long GrpParser::_tokenSet_33_data_[] = { 1059254224UL, 69203968UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_33(_tokenSet_33_data_,4);
const unsigned long GrpParser::_tokenSet_34_data_[] = { 2150760448UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_34(_tokenSet_34_data_,4);
const unsigned long GrpParser::_tokenSet_35_data_[] = { 2097280UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_35(_tokenSet_35_data_,4);
const unsigned long GrpParser::_tokenSet_36_data_[] = { 2150760576UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_36(_tokenSet_36_data_,4);
const unsigned long GrpParser::_tokenSet_37_data_[] = { 2228352UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_37(_tokenSet_37_data_,4);
const unsigned long GrpParser::_tokenSet_38_data_[] = { 255488UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_38(_tokenSet_38_data_,4);
const unsigned long GrpParser::_tokenSet_39_data_[] = { 5451840UL, 0UL, 1632UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_39(_tokenSet_39_data_,8);
const unsigned long GrpParser::_tokenSet_40_data_[] = { 259584UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_40(_tokenSet_40_data_,4);
const unsigned long GrpParser::_tokenSet_41_data_[] = { 259840UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_41(_tokenSet_41_data_,4);
const unsigned long GrpParser::_tokenSet_42_data_[] = { 12288UL, 6UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_42(_tokenSet_42_data_,4);
const unsigned long GrpParser::_tokenSet_43_data_[] = { 63232UL, 6UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_43(_tokenSet_43_data_,4);
const unsigned long GrpParser::_tokenSet_44_data_[] = { 4096UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_44(_tokenSet_44_data_,4);
const unsigned long GrpParser::_tokenSet_45_data_[] = { 1057023552UL, 739312UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_45(_tokenSet_45_data_,4);
const unsigned long GrpParser::_tokenSet_46_data_[] = { 1057023808UL, 739312UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_46(_tokenSet_46_data_,4);
const unsigned long GrpParser::_tokenSet_47_data_[] = { 0UL, 1920UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_47(_tokenSet_47_data_,4);
const unsigned long GrpParser::_tokenSet_48_data_[] = { 0UL, 384UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_48(_tokenSet_48_data_,4);
const unsigned long GrpParser::_tokenSet_49_data_[] = { 1056972864UL, 737280UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_49(_tokenSet_49_data_,4);
const unsigned long GrpParser::_tokenSet_50_data_[] = { 2130848208UL, 4192256UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_50(_tokenSet_50_data_,4);
const unsigned long GrpParser::_tokenSet_51_data_[] = { 4281532368UL, 71303152UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_51(_tokenSet_51_data_,4);
const unsigned long GrpParser::_tokenSet_52_data_[] = { 1056973120UL, 776192UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_52(_tokenSet_52_data_,4);
const unsigned long GrpParser::_tokenSet_53_data_[] = { 1056972864UL, 720896UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_53(_tokenSet_53_data_,4);
const unsigned long GrpParser::_tokenSet_54_data_[] = { 256UL, 4096UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_54(_tokenSet_54_data_,4);
const unsigned long GrpParser::_tokenSet_55_data_[] = { 1056972864UL, 67715072UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_55(_tokenSet_55_data_,4);
const unsigned long GrpParser::_tokenSet_56_data_[] = { 256UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_56(_tokenSet_56_data_,4);
const unsigned long GrpParser::_tokenSet_57_data_[] = { 2130898896UL, 71303152UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_57(_tokenSet_57_data_,4);
const unsigned long GrpParser::_tokenSet_58_data_[] = { 1056973120UL, 784384UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_58(_tokenSet_58_data_,4);
const unsigned long GrpParser::_tokenSet_59_data_[] = { 1056975184UL, 1046528UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_59(_tokenSet_59_data_,4);
const unsigned long GrpParser::_tokenSet_60_data_[] = { 1057106256UL, 3143680UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_60(_tokenSet_60_data_,4);
const unsigned long GrpParser::_tokenSet_61_data_[] = { 2130902992UL, 71303152UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_61(_tokenSet_61_data_,4);
const unsigned long GrpParser::_tokenSet_62_data_[] = { 1056975168UL, 784384UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_62(_tokenSet_62_data_,4);
const unsigned long GrpParser::_tokenSet_63_data_[] = { 1056975184UL, 1832960UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_63(_tokenSet_63_data_,4);
const unsigned long GrpParser::_tokenSet_64_data_[] = { 1056975168UL, 67893248UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_64(_tokenSet_64_data_,4);
const unsigned long GrpParser::_tokenSet_65_data_[] = { 1059203536UL, 4192256UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_65(_tokenSet_65_data_,4);
const unsigned long GrpParser::_tokenSet_66_data_[] = { 2236416UL, 2097152UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_66(_tokenSet_66_data_,4);
const unsigned long GrpParser::_tokenSet_67_data_[] = { 2130848208UL, 4190208UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_67(_tokenSet_67_data_,4);
const unsigned long GrpParser::_tokenSet_68_data_[] = { 4281532368UL, 71301104UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_68(_tokenSet_68_data_,4);
const unsigned long GrpParser::_tokenSet_69_data_[] = { 1056973120UL, 774144UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_69(_tokenSet_69_data_,4);
const unsigned long GrpParser::_tokenSet_70_data_[] = { 2130898896UL, 71301104UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_70(_tokenSet_70_data_,4);
const unsigned long GrpParser::_tokenSet_71_data_[] = { 1056973120UL, 782336UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_71(_tokenSet_71_data_,4);
const unsigned long GrpParser::_tokenSet_72_data_[] = { 1056973120UL, 67747840UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_72(_tokenSet_72_data_,4);
const unsigned long GrpParser::_tokenSet_73_data_[] = { 2130767824UL, 67889136UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_73(_tokenSet_73_data_,4);
const unsigned long GrpParser::_tokenSet_74_data_[] = { 1056973120UL, 67756032UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_74(_tokenSet_74_data_,4);
const unsigned long GrpParser::_tokenSet_75_data_[] = { 4294918128UL, 4294967295UL, 4294967295UL, 8191UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_75(_tokenSet_75_data_,8);
const unsigned long GrpParser::_tokenSet_76_data_[] = { 28480UL, 8388608UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_76(_tokenSet_76_data_,4);
const unsigned long GrpParser::_tokenSet_77_data_[] = { 2160514UL, 8658944UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_77(_tokenSet_77_data_,4);
const unsigned long GrpParser::_tokenSet_78_data_[] = { 2160514UL, 1082400768UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_78(_tokenSet_78_data_,4);
const unsigned long GrpParser::_tokenSet_79_data_[] = { 16UL, 2048UL, 31UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_79(_tokenSet_79_data_,8);
const unsigned long GrpParser::_tokenSet_80_data_[] = { 2160514UL, 3229884416UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_80(_tokenSet_80_data_,4);
const unsigned long GrpParser::_tokenSet_81_data_[] = { 2160530UL, 3229886464UL, 31UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_81(_tokenSet_81_data_,8);
const unsigned long GrpParser::_tokenSet_82_data_[] = { 2160530UL, 3229886464UL, 127UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_82(_tokenSet_82_data_,8);
const unsigned long GrpParser::_tokenSet_83_data_[] = { 1187904UL, 8519680UL, 7776UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_83(_tokenSet_83_data_,8);
const unsigned long GrpParser::_tokenSet_84_data_[] = { 3602386UL, 3232118784UL, 8191UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_84(_tokenSet_84_data_,8);
const unsigned long GrpParser::_tokenSet_85_data_[] = { 2160530UL, 3229890560UL, 255UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_85(_tokenSet_85_data_,8);
const unsigned long GrpParser::_tokenSet_86_data_[] = { 2422674UL, 3229890560UL, 255UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpParser::_tokenSet_86(_tokenSet_86_data_,8);
