/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrpParser.g
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Provides ANTLR definitions for the Graphite GDL parser generation.
	ANTLR compiles this file and produces the following files as output:
		GrpLexer.hpp
		GrpLexer.cpp
		GrpParser.hpp
		GrpParser.cpp
		GrpParserTokenTypes.hpp

	Notes on ANTLR syntax (what I remember from 6 years ago):
		(A|B) = A or B
		* = zero or more
		? = optional item
		^ = make this item the root of the (default) returned tree construct
		! = in LHS: don't return the default tree construct; return the explicit one if any
			in RHS: syntactic marker only--don't include in output tree
		: = associate a label with an item, which can be used to explicitly build the
				return tree construct
		{#label = #(...) } = builds an explicit tree construct
----------------------------------------------------------------------------------------------*/

/*----------------------------------------------------------------------------------------------
	Housekeeping
----------------------------------------------------------------------------------------------*/
header
{
//	Header stuff here
void AddGlobalError(bool, int nID, std::string, int nLine);
class GrpTokenStreamFilter;
}

options
{
	language="Cpp";						// We're Generating C++ Code
}


/*----------------------------------------------------------------------------------------------
	The Graphite Grammar: GrpParser
----------------------------------------------------------------------------------------------*/

{
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

}

class GrpParser extends Parser;

options	{
	k = 3;									// Lookahead 3 tokens
	buildAST = true;						// uses CommonAST by default
///	ASTLabelType = "GrpASTNode";
}

{
//	Customized code:
public:
	//	Record the token stream filter, which supplies the line-and-file information
	//	to error messages.
	GrpTokenStreamFilter * m_ptsf;
	void init(GrpTokenStreamFilter & tsf);

	void reportError(const ParserException& ex);

	void reportError(const std::string& s)
	{
		AddGlobalError(true, 104, s.c_str(), 0);
	}
	void reportWarning(const std::string& s)
	{
		AddGlobalError(false, 504, s.c_str(), 0);
	}
}

renderDescription! :	D:declarationList EOF
							{ #renderDescription = #([Ztop], D); }
;

declarationList	:	( globalDecl | topDecl )*;

//	Top level declaration, outside of the scope of any table:
topDecl			:	topEnvironDecl | tableDecl;


//
//	Global Declarations
//

globalDecl		:	identDot
					( OP_EQ^ | OP_PLUSEQUAL^ )
					( OP_LPAREN! exprList OP_RPAREN!
					| expr
					)
					(OP_SEMI!)?
;

//
//	Environment Declaration
//

//	Environment outside of the scope of any table:
topEnvironDecl	:	"environment"^
					(directives)?
					(OP_SEMI!)?
					(topDecl | globalDecl)*
					"endenvironment"!
					(OP_SEMI!)?
;

directives!		:	D:directiveList
						{ #directives = #([Zdirectives], D); }
;

directiveList	:	OP_LBRACE!
					(	directive
						(OP_SEMI! directive)*
						(OP_SEMI!)?
					)?
					OP_RBRACE!
//					(OP_SEMI!)?
;

directive		:	IDENT OP_EQ^ expr;


//
//	Table Declaration
//

tableDecl		:	"table"^
					(	tableName | tableGlyph | tableFeature | tableLanguage
					|	tableSub | tableJust | tablePos | tableLineBreak
					|	tableOther
					)
					"endtable"!
					(OP_SEMI!)?
;


//
//	Name Table
//

tableName		:	OP_LPAREN! "name" OP_RPAREN! (directives)? (OP_SEMI!)?
					( nameEnv | nameSpecList | tableDecl )*;

nameEnv			:	"environment"^
					(directives)?
					(OP_SEMI!)?
					( nameSpecList | nameEnv | tableDecl )*
					"endenvironment"!
					(OP_SEMI!)?
;

nameSpecList		:	(	nameSpecStruct (nameSpecList)?
						|	nameSpecFlat (OP_SEMI! nameSpecList)? (OP_SEMI!)?
						)
;

//	Note that using IDENT in the rules below, or an integer as the value, will be an error.
nameSpecStruct!		:	( N:LIT_INT | I:IDENT )
						OP_LBRACE! (X:nameSpecList)? OP_RBRACE! (OP_SEMI!)?
							{ #nameSpecStruct = #([ZdotStruct], N, I, X); }
;

nameSpecFlat!		:	(	N:LIT_INT | I:IDENT )
						(	D:OP_DOT! ( X1:nameSpecFlat | X2:nameSpecStruct )
								{ #nameSpecFlat = #(D, N, I, X1, X2); }

						|	( E1:OP_EQ | E2:OP_PLUS_EQUAL )
							(Vi1:signedInt | Vi2:stringDefn )
								{ #nameSpecFlat = #(E1, E2, N, I, Vi1, Vi2); }
						)
;


stringDefn		:	( LIT_STRING
//					| LIT_UNICHAR
					| stringFunc
					| ( OP_LPAREN stringDefn ((OP_COMMA)? stringDefn)* OP_RPAREN )
					)
;

stringFunc		:	"string"^ OP_LPAREN! LIT_STRING (OP_COMMA! LIT_INT)? OP_RPAREN!;


//
//	Glyph Table
//

tableGlyph		:	OP_LPAREN! "glyph" OP_RPAREN! (directives)? (OP_SEMI!)?
					( glyphEnv | glyphEntry | tableDecl)*
;

glyphEnv		:	"environment"^
					(directives)?
					(OP_SEMI!)?
					(glyphEntry | glyphEnv | tableDecl)*
					"endenvironment"!
					(OP_SEMI!)?
;

glyphEntry		:	( glyphContents | glyphAttrs ) (OP_SEMI!)?;


glyphContents	:	IDENT
					( (OP_EQ^ | OP_PLUSEQUAL^) glyphSpec )
					(attributes)?
;

glyphAttrs!		:	I:IDENT
					A:(	OP_LBRACE! (X1:attrItemList)? OP_RBRACE!
							{ #A = #([Zattrs], X1); }

					|	OP_DOT! ( X2:attrItemFlat | X3:attrItemStruct )
							{ #A = #([Zattrs], X2, X3); }
					)
					{ #glyphAttrs = #([OP_PLUSEQUAL], I, A); }
;


glyphSpec		:	(	IDENT
					|	codepointFunc
					|	glyphidFunc
					|	postscriptFunc
					|	unicodeFunc
					|	unicodeCodepoint
					|	pseudoFunc
					|	( OP_LPAREN! (glyphSpec ((OP_COMMA!)? glyphSpec)*)? OP_RPAREN! )
					)
;

pseudoFunc		:	"pseudo"^ OP_LPAREN!
					(codepointFunc | glyphidFunc | postscriptFunc | unicodeFunc | unicodeCodepoint)
					((OP_COMMA!)? (LIT_INT | LIT_UHEX))?
					OP_RPAREN!
;

codepointFunc!	:	F:"codepoint" OP_LPAREN! X:codepointList
					C:(! OP_COMMA! N:LIT_INT { #C = #([Zcodepage], N); } )?
					OP_RPAREN!
						{ #codepointFunc = #(F, C, X); }
;

codepointList	:	(	(	OP_LPAREN!
							codepointItem
							( (OP_COMMA!)? codepointItem )*
							OP_RPAREN!
						)
					|	codepointItem
					)
;

codepointItem	:	( LIT_STRING | charOrIntOrRange);

glyphidFunc		:	"glyphid"^ OP_LPAREN! intOrRange
					((OP_COMMA!)? intOrRange)*
					OP_RPAREN!
;

postscriptFunc	:	"postscript"^ OP_LPAREN! LIT_STRING ((OP_COMMA!)? LIT_STRING)* OP_RPAREN!;

unicodeFunc		:	"unicode"^ OP_LPAREN! intOrRange
					((OP_COMMA!)? intOrRange)*
					OP_RPAREN!
;

unicodeCodepoint	:	U:unicodeIntOrRange
							{ #unicodeCodepoint = #([ZuHex], U); }
;

intOrRange		:	(	LIT_INT OP_DOTDOT^ LIT_INT
					|	LIT_INT
					)
;

charOrIntOrRange	:	( LIT_CHAR | LIT_INT )
						( OP_DOTDOT^ ( LIT_CHAR | LIT_INT ))?
;

unicodeIntOrRange	:	(	LIT_UHEX OP_DOTDOT^ LIT_UHEX
						|	LIT_UHEX
						)
;

//
//	Features
//

tableFeature	:	OP_LPAREN! "feature" OP_RPAREN! (directives)? (OP_SEMI!)?
					( featureEnv | featureSpecList | tableDecl )*;

featureEnv		:	"environment"^
					(directives)?
					(OP_SEMI!)?
					( featureSpecList | featureEnv | tableDecl )*
					"endenvironment"!
					(OP_SEMI!)?
;

featureSpecList		:	(	featureSpecStruct (featureSpecList)?
						|	featureSpecFlat (OP_SEMI! featureSpecList)? (OP_SEMI!)?
						)
;

// "name is treated specially below because it is already a defined keyword.
// "value" is also a special keyword but we treat it as an identifier.
featureSpecStruct!	:	( I:IDENT | In:"name" )
						OP_LBRACE! (X:featureSpecList)? OP_RBRACE! (OP_SEMI!)?
							{ #featureSpecStruct = #([ZdotStruct], I, In, X); }
;

featureSpecFlat!	:	(	( I:IDENT | In:"name" )
							(	D:OP_DOT! ( X1:featureSpecFlat | X2:featureSpecStruct )
									{ #featureSpecFlat = #(D, I, In, X1, X2); }

							|	E1:OP_EQ
								(Vi1:signedInt | Vi2:stringDefn | Vi3:IDENT)
									{ #featureSpecFlat = #(E1, I, In, Vi1, Vi2, Vi3); }
							)
						|	N:LIT_INT // language ID
							E2:OP_EQ
								(Vn1:signedInt | Vn2:stringDefn | Vn3:IDENT)
									{ #featureSpecFlat = #(E2, N, Vn1, Vn2, Vn3); }
						)
;

//
// Language Table
//

tableLanguage	:	OP_LPAREN! "language" OP_RPAREN! (directives)? (OP_SEMI!)?
					( languageEnv | languageSpecList | tableDecl )*;

languageEnv		:	"environment"^
					(directives)?
					(OP_SEMI!)?
					( languageSpecList | languageEnv | tableDecl )*
					"endenvironment"!
					(OP_SEMI!)?
;

languageSpecList	:	(	languageSpec (languageSpecList)?
						|	languageSpec (OP_SEMI! languageSpecList)? (OP_SEMI!)?
						)
;

languageSpec!		:	I:IDENT
						(	OP_DOT X1:languageSpecItem
								{ #languageSpec = #([ZdotStruct], I, X1); }
						|	OP_LBRACE X2:languageItemList OP_RBRACE! (OP_SEMI!)?
								{ #languageSpec = #([ZdotStruct], I, X2); }
						)
;

languageItemList	:	(languageSpecItem)*
;

languageSpecItem!	:	(	I:IDENT
							E1:OP_EQ
							(Vi1:signedInt | Vi2:IDENT)
								{ #languageSpecItem = #(E1, I, Vi1, Vi2); }
						|	( Ilang:"language" | Ilangs:"languages" )
							E2:OP_EQ
							LL:languageCodeList
								{ #languageSpecItem = #(E2, Ilang, Ilangs, LL); }
						)
						(OP_SEMI!)?
;

languageCodeList	:	(	LIT_STRING
						|	OP_LPAREN! LIT_STRING (OP_COMMA! LIT_STRING)* OP_RPAREN
						)
;



//
//	Substitution Table
//

tableSub		:	OP_LPAREN! "substitution" OP_RPAREN! (directives)? (OP_SEMI!)?
					(subEntry)*;

subEntry		:	(subIf | subRule | subPass | subEnv | tableDecl);

subEnv			:	"environment"^
					(directives)?
					(OP_SEMI!)?
					(subEntry)*
					"endenvironment"!
					(OP_SEMI!)?
;

subPass			:	"pass"^ OP_LPAREN! LIT_INT OP_RPAREN! (directives)? (OP_SEMI!)?
					(subEntry)*
					"endpass"! (OP_SEMI!)?
;

subIf!			:	C1k:"if" OP_LPAREN! E:expr OP_RPAREN!
					C1:(! C1x:subEntryList { #C1 = #(C1k, E, C1x); } )
					C2:(! C2x:subElseIfList { #C2 = #C2x; })?
					C3:(! C3k:"else" C3x:subEntryList { #C3 = #(C3k, C3x); } )?
					"endif"!
					(OP_SEMI!)?
						{ #subIf = #([ZifStruct], C1, C2, C3); }
;

subElseIfList	:	(subElseIf)*;

subElseIf		:	( Zelseif^ | "elseif"^ )
					OP_LPAREN! expr OP_RPAREN! subEntryList
;

subEntryList	:	(subEntry)*;

//	Note that if there is no left-hand-side, the right-hand-side will be treated here like
//	the left-hand-side, and the tree-walker will straighten it out later.
subRule!		:	L:(! L1:subLhs { #L = #([Zlhs], L1); } )
					(	OP_GT!
						R:(! R1:subRhs { #R = #([Zrhs], R1); } )
					)?
					( OP_DIV! C:(! C1:context { #C = #([Zcontext], C1); } ) )?
					OP_SEMI!
						{ #subRule = #([Zrule], L, R, C); }
;


subLhs			:	(subLhsRange)+;

subLhsRange!	:	(	X1:subLhsList { #subLhsRange = #X1; }
					|	X2:subLhsItem
						(	Q:OP_QUESTION { #subLhsRange = #(Q, X2); }
						|	{ #subLhsRange = #X2; }
						)?
					)
;

subLhsList		:	(OP_LBRACKET! (subLhs)+ OP_RBRACKET!) OP_QUESTION^;

//	Allow anything the right-hand-side will allow, and let the tree-walker check for it later.
subLhsItem			:	subRhsItem;

////  Old version:
////subLhsItem!		:	(C1:OP_UNDER | C2:glyphSpec | C3:OP_HASH) (A:alias)?
////						{ #subLhsItem = #([ZruleItem], C1, C2, C3, A); }
////;



//	REVIEW: should we allow optional items in the right hand side, and make the
//	tree-walker check for them?
subRhs			:	(subRhsItem)+;

//	Note: we recognize the # even though it is illegal; the compiler will check for it
//	and give a better error message.
subRhsItem!		:	(	C1g:OP_UNDER
					|	( C2at:OP_AT C2s:selectorAfterAt (OP_COLON! C2a:associations)? )
					|	( C3at:OP_AT (OP_COLON!)? (C3s:selectorAfterAt)? )
					|	( (C4g1:glyphSpec | C4g2:OP_HASH)
							(	( OP_COLON! C4a1: associations (OP_DOLLAR! C4s1:selector)? )
							|	( OP_DOLLAR! C4s2: selector (OP_COLON! C4a2:associations)? )
							)?
						)
					)
					(A:alias)?
					(X:attributes)?
						{ #subRhsItem = #([ZruleItem],
							C1g, C2at, C2s,
							C3at, C3s,
							C4g1, C4g2, C4s1, C4s2,
							A,
							C2a, C4a1, C4a2,
							X); }
;


selector!		:	X:slotIndicator
						{ #selector = #([Zselector], X); }
;


associations!	:	( S1:slotIndicator | S2: assocsList )
						{ #associations = #([Zassocs], S1, S2); }
;

assocsList		:	(OP_LPAREN! (slotIndicator ((OP_COMMA!)? slotIndicator)*)? OP_RPAREN!);


slotIndicator	:	( LIT_INT | IDENT | Qalias );


//	Immediately after an @, use a special version that does not accept IDENT, only Qalias,
//	which is what will be generated (by the token stream filter) when there is no
//	white space between the @ and the identifier. This is to distinguish between
//	'@abc' and '@ abc'.

selectorAfterAt!	:	X:slotIndicatorAfterAt
							{ #selectorAfterAt = #([Zselector], X); }
;

slotIndicatorAfterAt : ( LIT_INT | Qalias );


//
//	Justification Table
//

tableJust		:	OP_LPAREN! "justification" OP_RPAREN! (directives)? (OP_SEMI!)?
					(subEntry)*;


//
//	Positioning Table
//

tablePos		:	OP_LPAREN! ( "position" | "positioning" ) OP_RPAREN!
					(directives)? (OP_SEMI!)? (posEntry)*
;

posEntry		:	(posIf | posRule | posPass | posEnv | tableDecl);

posEnv			:	"environment"^
					(directives)?
					(OP_SEMI!)?
					(posEntry)*
					"endenvironment"!
					(OP_SEMI!)?
;
posPass			:	"pass"^ OP_LPAREN! LIT_INT OP_RPAREN! (directives)? (OP_SEMI!)?
					(posEntry)*
					"endpass"! (OP_SEMI!)?
;

posIf!			:	C1k:"if" OP_LPAREN! E:expr OP_RPAREN!
					C1:(! C1x:posEntryList { #C1 = #(C1k, E, C1x); } )
					C2:(! C2x:posElseIfList { #C2 = #C2x; })?
					C3:(! C3k:"else" C3x:posEntryList { #C3 = #(C3k, C3x); } )?
					"endif"!
					(OP_SEMI!)?
						{ #posIf = #([ZifStruct], C1, C2, C3); }
;

posElseIfList	:	(posElseIf)*;

posElseIf		:	( Zelseif^ | "elseif"^ )
					OP_LPAREN! expr OP_RPAREN! posEntryList
;

posEntryList	:	(posEntry)*;

//	REVIEW: should we make the syntax here allow for a LHS, and check for it later?
posRule!		:	R:(! R1:posRhs { #R = #([Zrhs], R1); } )
					( OP_DIV! C:(! C1:context { #C = #([Zcontext], C1); } ) )?
					OP_SEMI!
						{ #posRule = #([Zrule], R, C); }
;

posRhs			:	(posRhsRange)+;

posRhsRange!	:	(	X1:posRhsList { #posRhsRange = #X1; }
					|	X2:posRhsItem
						(	Q:OP_QUESTION { #posRhsRange = #(Q, X2); }
						|	{ #posRhsRange = #X2; }
						)?
					)
;

posRhsList		:	(OP_LBRACKET! (posRhs)+ OP_RBRACKET!) OP_QUESTION^;

//	For now, recognize anything that can go in a sustitution rule; the compiler
//	will check and give error messages.
posRhsItem		:	subRhsItem;


//
//	Line-break Table
//

tableLineBreak	:	OP_LPAREN! "linebreak" OP_RPAREN! (directives)? (OP_SEMI!)? (posEntry)*;


//
//	General table stuff
//

context			:	(contextRange)*;

contextRange!	:	(	(	X1:contextList { #contextRange = #X1; }
						|	X2:OP_CARET { #contextRange = #X2; }
						|	X3:contextItem
							(	Q:OP_QUESTION { #contextRange = #(Q, X3); }
							|	{ #contextRange = #X3; }
							)?
						)
					)
;

contextList		:	OP_LBRACKET! (contextRange)+ OP_RBRACKET! OP_QUESTION^;

contextItem!	:	(	C1:OP_HASH
					|	C2:OP_UNDER
					|	C3:glyphSpec
					)
					(A:alias)?
					(Y:constraint)?
						{ #contextItem = #([ZruleItem], C1, C2, C3, A, Y); }
;


constraint!	:	OP_LBRACE! (X:expr)? OP_RBRACE!
					{ #constraint = #([Zconstraint], X); }
;


alias	:	OP_EQ I:IDENT
				{ #alias = #([Zalias], I); }
;


//
//	Other Tables
//

tableOther		:	OP_LPAREN! IDENT OP_RPAREN! (directives)? (OP_SEMI!)? (otherEntry)*;

otherEntry!		:	(	~("endtable" | "table")
					|	X:topDecl { #otherEntry = #X; }
					);


//
//	Attributes
//

attributes!		:	( OP_LBRACE! (X:attrItemList)? (OP_SEMI!)? OP_RBRACE!
						{ #attributes = #([Zattrs], X); }
					)?
;

attrItemList	:	( attrItemStruct | attrItemFlat )
					(OP_SEMI! attrItemList)?
;

attrItemStruct!	:	(I1:IDENT | I2:LIT_INT) OP_LBRACE! (X:attrItemList)? (OP_SEMI!)? OP_RBRACE!
						{ #attrItemStruct = #([ZdotStruct], I1, I2, X); }
;

//attrItemFlatTop! :	(	S:attrSel OP_DOT X3:attrItemFlatTop
//							{ #attrItemFlatTop = #([OP_DOT], S, X3); }
//					|	X:attrItemFlat
//							{ #attrItemFlatTop = X; }
//					)
//;

attrItemFlat!	:	(I1:IDENT | I2:LIT_INT)
					(	D:OP_DOT! ( X1:attrItemFlat | X2:attrItemStruct )
							{ #attrItemFlat = #(D, I1, I2, X1, X2); }

					|	E:attrAssignOp
						(V1:function | V2:expr)
							{ #attrItemFlat = #(E, I1, I2, V1, V2); }
					)
;

attrAssignOp	:	(	OP_EQ
					|	OP_PLUSEQUAL
					|	OP_MINUSEQUAL
					|	OP_DIVEQUAL
					|	OP_MULTEQUAL
					)
;


//
//	Expressions
//

expr				:	conditionalExpr;

exprList			:	expr (OP_COMMA! expr)*;

conditionalExpr		:	logicalOrExpr
						(OP_QUESTION^ expr OP_COLON expr)?
;

logicalOrExpr		:	logicalAndExpr
						(OP_OR^ logicalAndExpr)*
;

logicalAndExpr		:	comparativeExpr
						(OP_AND^ comparativeExpr)*
;

comparativeExpr		:	additiveExpr
						(	( OP_EQUALEQUAL^ | OP_NE^ | OP_LT^ | OP_LE^ | OP_GT^ | OP_GE^
							| OP_EQ^  // error
							)
							additiveExpr
						)*
;

additiveExpr		:	multiplicativeExpr
						((OP_PLUS^ | OP_MINUS^) multiplicativeExpr)*
;

multiplicativeExpr	:	unaryExpr
						((OP_MULT^ | OP_DIV^) unaryExpr)*
;

unaryExpr			:	(	(( OP_NOT^ | OP_MINUS^) singleExpr)
						|	singleExpr
						)
;

singleExpr			:	(	OP_LPAREN! expr OP_RPAREN!
						|	LIT_STRING
						|	arithFunction
						|	lookupExpr
						|	signedInt
						)
;

lookupExpr!			:	(	S:selectorExpr
							(	OP_DOT I1:identDot (C1:clusterExpr)?
									{ #lookupExpr = #([Zlookup], S, I1, C1); }
							|	{ #lookupExpr = #S; }
							)
						|	I2:identDot (C2:clusterExpr)?
								{ #lookupExpr = #([Zlookup], I2, C2); }
						)
;

selectorExpr		:	OP_AT^ ( LIT_INT | Qalias );

clusterExpr!		:	OP_DOT C:LIT_INT
							{ #clusterExpr = #([Zcluster], C); }
;

signedInt			:	("true" | "false" | (OP_PLUS! | OP_MINUS^)? LIT_INT);

identDot			:	(	( IDENT | "position" ) OP_DOT^ identDot
						|	( IDENT | "position" )
						)
;


//
//	Functions
//

arithFunction	:	("max"^ | "min"^ ) OP_LPAREN! (E1:exprList)? OP_RPAREN!;


function!		:	I:IDENT OP_LPAREN! (E:exprList)? OP_RPAREN!
						{ #function = #([Zfunction], I, E); }
;



/*----------------------------------------------------------------------------------------------
	The Graphite Scanner: GrpLexer
----------------------------------------------------------------------------------------------*/

{

//	Insert at the beginning of the GrpLexer.cpp file:
#pragma warning(disable:4101)
#include "Grp.h"

//	This function needs to go in the .cpp file, not the .hpp file, after the
//	GrpToken class is defined.
void GrpLexer::init(GrpTokenStreamFilter & tsf)
{
	m_ptsf = &tsf;
	setTokenObjectFactory(&GrpToken::factory);
}

void GrpLexer::reportError(const ScannerException& ex)
{
	//	Pipe the error through the token stream filter, to handle the
	//	line-and-file adjustments.
	m_ptsf->ReportLexerError(ex);
}

}

class GrpLexer extends Lexer;

options	{
	charVocabulary = '\0'..'\377';	// Set of recognizable characters
//	tokenVocabulary = Grp;				//	Name of generated token set
	testLiterals = false;				// Don't automatically test literals
	caseSensitiveLiterals = false;
	k = 6;								// Look ahead (6 characters)
}

tokens {
	"codepoint";
	"else";
	"elseif";
	"endenvironment";
	"endif";
	"endpass";
	"endtable";
	"environment";
	"false";
	"feature";
	"glyph";
	"glyphid";
	"if";
	"justification";
	"linebreak";
	"max";
	"min";
	"name";
	"pass";
	"position";
	"positioning";
	"postscript";
	"pseudo";
	"substitution";
	"string";
	"table";
	"true";
	"unicode";

	Zalias;
	Zassocs;
	Zattrs;
	Zcluster;
	Zcodepage;
	Zconstraint;
	Zcontext;
	Zdirectives;
	ZdotStruct;
	Zelseif;	// generated by token-stream filter
	Zfeatures;
	Zfunction;
	ZifStruct;
	Zlhs;
	Zlookup;
	Zrhs;
	Zrule;
	ZruleItem;
	Zselector;
	Ztop;
	ZuHex;

	//	Generated by the token-stream filter from AT_IDENT:
	Qalias;
	OP_AT;
}

{
//	Customized code:
public:
	//	Record the token stream filter, which supplies the line-and-file information
	//	to error messages.
	GrpTokenStreamFilter * m_ptsf;
	void init(GrpTokenStreamFilter & tsf);

	void reportError(const ScannerException& ex);

	void reportError(const std::string& s)
	{
		AddGlobalError(true, 105, s.c_str(), 0);
	}
	void reportWarning(const std::string& s)
	{
		AddGlobalError(false, 505, s.c_str(), 0);
	}
	RefToken publicMakeToken(int t)
	{
		return makeToken(t);
	}

}



//
//	GrpLexer Rules
//

//
//	Whitespace
//

WS					:	(	' '
						|	'\t'
						|	'\f'
						|	(	"\r\n"
							|	'\r'
							|	'\n'
							) { newline(); }
//						|	','
						) { _ttype = Token::SKIP; }
;

//
// Comments (Single (rest of) line and Multiple line)
//

COMMENT_SL		:	"//"	(~('\n'|'\r'))*
						{ _ttype = Token::SKIP; }
;

COMMENT_ML		:	"/*"
						(	{ LA(2)!='/' }? '*'
						|	'\n' { newline(); }
						|	~('*'|'\n')
						)*
						"*/"
						{ _ttype = Token::SKIP; }
;

//
//	Literals
//


LIT_INT			:	( (DIGIT)+ | ( "0x" (XDIGIT)+ ) ) ('m' | 'M')?;

LIT_UHEX			:	"U+" (XDIGIT)+;

//LIT_UNICHAR		:	"0u" XDIGIT XDIGIT XDIGIT XDIGIT;

//	For some reason we have to expand the SQUOTE and DQUOTE rules into the negations of the
//	following rules, or we get "rule cannot be inverted" errors.
LIT_CHAR		:	SQUOTE! ( ESC | ~( '\'' | '\221' | '\222' ) ) SQUOTE!;

LIT_STRING		:	DQUOTE! ( ESC | ~( '"' | '\223' | '\224' ) )* DQUOTE!;

protected
ESC				:	'\\'
					(	'n'
					|	'r'
					|	't'
					|	'b'
					|	'f'
					|	'"'
					|	'\''
					|	'\\'
//					|	('0'..'3') ( ODIGIT (ODIGIT)? )?
//					|	('4'..'7') (ODIGIT)?
//					|	'u' XDIGIT XDIGIT XDIGIT XDIGIT
					)
;

protected
ODIGIT			:	'0'..'7';

protected
DIGIT			:	'0'..'9';

protected
XDIGIT			:	'0' .. '9' | 'a' .. 'f' | 'A' .. 'F';

protected
SQUOTE			:	( '\'' | '\221' | '\222' );

protected
DQUOTE			:	( '"' | '\223' | '\224' );

//
//	Operators
//

OP_DOT			:	'.';
OP_DOTDOT		:	"..";
OP_COLON		:	':';
OP_SEMI			:	';';
OP_LBRACKET		:	'[';
OP_RBRACKET		:	']';
OP_LPAREN		:	'(';
OP_RPAREN		:	')';
OP_LBRACE		:	'{';
OP_RBRACE		:	'}';
OP_NOT			:	'!';
OP_LT			:	'<';
OP_LE			:	"<=";
OP_EQ			:	'=';
OP_EQUALEQUAL	:	"==";
OP_NE			:	"!=";
OP_GE			:	">=";
OP_GT			:	'>';
OP_PLUS			:	'+';
OP_PLUSEQUAL	:	"+=";
OP_MINUS		:	'-';
OP_MINUSEQUAL	:	"-=";
OP_MULT			:	'*';
OP_MULTEQUAL	:	"*=";
OP_DIV			:	'/';
OP_DIVEQUAL		:	"/=";
OP_COMMA		:	',';
//OP_AT			:	'@';	// see AT_IDENT below
OP_DOLLAR		:	'$';
OP_LINEMARKER	:	"#line";
OP_HASH			:	'#';
OP_AND			:	"&&";
OP_OR			:	"||";
OP_BSLASH		:	'\\';
OP_UNDER		:	'_';
OP_QUESTION		:	'?';
OP_CARET		:	'^';

//
//	Identifiers
//

IDENT
	options	{	testLiterals = true;	}
	:
	( 'a'..'z' | 'A'..'Z' ) ( '_' | 'a'..'z' | 'A'..'Z' | '0'..'9' )*
;

//	Because white space IS significant after an @; ie, "@ X" is not the same as "@X"
//	(in the first case X is a class name; in the second it is an alias). So we treat them
//	as one token here, and the token stream filter breaks them into 2 tokens: OP_AT
//	followed by Qalias or LIT_INT.
AT_IDENT : ('@' | "@:" )
			( ( 'a'..'z' | 'A'..'Z' ) ( '_' | 'a'..'z' | 'A'..'Z' | '0'..'9' )*
			| ( '0'..'9' )*
			)
;
