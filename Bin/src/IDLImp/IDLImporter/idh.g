header {
/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2007, SIL International. All Rights Reserved.
/// <copyright from='2007' to='2007' company='SIL International'>
///		Copyright (c) 2007, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: idh.g
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Defines the grammar for our IDH files.
/// </remarks>
/// --------------------------------------------------------------------------------------------

//#define DEBUG_IDHGRAMMAR

using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
}

options {
	language = "CSharp";
	namespace  =  "SIL.FieldWorks.Tools";
}

//-----------------------------------------------------------------------------------------------
// IDH Parser rules
//
// This parser has enough rules to parse our IDH files (at least I hope so), but it isn't a
// complete parser that covers all possibilities or that can check correct syntax.
// Its only purpose is to be possible to extract the comments belonging to the methods and
// parameters.
//-----------------------------------------------------------------------------------------------
class IdhParser extends Parser;
options {
	buildAST = true;	// uses CommonAST by default
	k = 2;
}

idhfile returns [IdhCommentProcessor.CommentInfo info]
	{
		Dictionary<string, IdhCommentProcessor.CommentInfo> toplevel = new Dictionary<string, IdhCommentProcessor.CommentInfo>();
		info = new IdhCommentProcessor.CommentInfo(string.Empty, toplevel, 0);
	}
	:	( ((COMMENT)* "typedef") => typedef[toplevel]
		| comment:COMMENT
			{
				info.Comment = comment.getText();
				info.LineNumber = comment.getLine();
			}
		| forwardDeclaration
		| interfaceDeclaration[toplevel]
		| dualInterface[toplevel]
		| coclassDeclaration
		| PREPROCESSOR
		| import
			{
				#if DEBUG_IDHGRAMMAR
				System.Diagnostics.Debug.WriteLine("***import");
				#endif
			}
		| id:IDENTIFIER
			{
				#if DEBUG_IDHGRAMMAR
				System.Diagnostics.Debug.WriteLine("***IDENTIFIER: " + #id.getText());
				#endif
			}
		| constDeclaration
			{
				#if DEBUG_IDHGRAMMAR
				System.Diagnostics.Debug.WriteLine("***const");
				#endif
			}
		)+
	;

interfaceDeclaration [Dictionary<string, IdhCommentProcessor.CommentInfo> classes]
	{ Dictionary<string, IdhCommentProcessor.CommentInfo> methods = new Dictionary<string, IdhCommentProcessor.CommentInfo>(); }
	:	comment:COMMENT "DeclareInterface" LPAREN name:IDENTIFIER COMMA IDENTIFIER COMMA guid RPAREN
		LBRACE (((COMMENT)* (ATTRIBUTE)? IDENTIFIER) => methodDeclaration[methods] | COMMENT | PREPROCESSOR)+ RBRACE (SEMICOLON)?
		{
			classes[name.getText()] = new IdhCommentProcessor.CommentInfo(comment.getText(), methods, comment.getLine());
			#if DEBUG_IDHGRAMMAR
			System.Diagnostics.Debug.WriteLine("***DeclareInterface: " + name.getText());
			#endif
		}
	;

coclassDeclaration
	:	"DeclareCoClass" LPAREN IDENTIFIER COMMA guid RPAREN
		LBRACE (forwardDeclaration)+ RBRACE (SEMICOLON)?
	;

dualInterface [Dictionary<string, IdhCommentProcessor.CommentInfo> classes]
	{ Dictionary<string, IdhCommentProcessor.CommentInfo> methods = new Dictionary<string, IdhCommentProcessor.CommentInfo>(); }
	:	comment:COMMENT ("DeclareDualInterface"|"DeclareDualInterface2") LPAREN name:IDENTIFIER COMMA guid RPAREN
		LBRACE (((COMMENT)* (ATTRIBUTE)? IDENTIFIER) => methodDeclaration[methods] | COMMENT | PREPROCESSOR)+ RBRACE (SEMICOLON)?
		{
			classes[name.getText()] = new IdhCommentProcessor.CommentInfo(comment.getText(), methods, comment.getLine());
			#if DEBUG_IDHGRAMMAR
			System.Diagnostics.Debug.WriteLine("***DeclareDualInterface: " + name.getText());
			#endif
		}
	;

forwardDeclaration
	:	"interface"	IDENTIFIER SEMICOLON
	;

typedef [Dictionary<string, IdhCommentProcessor.CommentInfo> typedefs]
	{
		string mainComment, firstComment;
		Dictionary<string, IdhCommentProcessor.CommentInfo> children = new Dictionary<string, IdhCommentProcessor.CommentInfo>();
	}
	:	mainComment=comment "typedef"
		((ATTRIBUTE)? "enum" (name:IDENTIFIER)? (COMMENT)*
			LBRACE firstComment=comment (enumMemberDeclaration[children, ref firstComment])+
			RBRACE (name2:IDENTIFIER)? SEMICOLON
			{
				string tmpName = string.Empty;
				int lineNo = 0;
				if (name != null)
				{
					tmpName = name.getText();
					lineNo = name.getLine();
				}
				if (name2 != null)
				{
					tmpName = name2.getText();
					lineNo = name2.getLine();
				}
				typedefs[tmpName] = new IdhCommentProcessor.CommentInfo(mainComment, children, lineNo);
				#if DEBUG_IDHGRAMMAR
				System.Diagnostics.Debug.WriteLine("***typedef enum: " + tmpName);
				#endif
			}
		|	"struct" (structname:IDENTIFIER)? (COMMENT)*
			LBRACE firstComment=comment (structMemberDeclaration[children, ref firstComment])+
			RBRACE (structname2:IDENTIFIER)? SEMICOLON
			{
				// NOTE: the ATTRIBUTE above is really LBRACKET number RBRACKET, but it's easier to
				// treat it the same as the attribute, and since we don't do anything with it I guess it's ok.
				// Makes at least things easier in the parser/lexer.

				string tmpName = string.Empty;
				int lineNo = 0;
				if (structname != null)
				{
					tmpName = structname.getText();
					lineNo = structname.getLine();
				}
				if (structname2 != null)
				{
					tmpName = structname2.getText();
					lineNo = structname2.getLine();
				}
				typedefs[tmpName] = new IdhCommentProcessor.CommentInfo(mainComment, children, lineNo);
				#if DEBUG_IDHGRAMMAR
				System.Diagnostics.Debug.WriteLine("***typedef struct: " + tmpName);
				#endif
			}
		)
	;

enumMemberDeclaration [Dictionary<string, IdhCommentProcessor.CommentInfo> members, ref string addComment]
	{
		StringBuilder bldr = new StringBuilder(addComment);
		addComment = string.Empty;
		bool fCheckInline = true;
	}
	:	name:IDENTIFIER (EQUAL enumVal)? (COMMA)? (lineComment:COMMENT
			{
				if (fCheckInline && name.getLine() == lineComment.getLine())
				{	// inline comment belongs to current member
					bldr.Append(#lineComment.getText());
				}
				else
				{	// comment belongs to following member
					if (fCheckInline)
					{	// append all comments we got so far and create a new CommentInfo
						members[#name.getText()] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, name.getLine());
						bldr = new StringBuilder();
						fCheckInline = false;
					}
					bldr.Append(#lineComment.getText());
				}
			}
			)*
		{
			if (fCheckInline)
			{	// append all comments left if there was none
				members[#name.getText()] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, name.getLine());
			}
			else
				addComment = bldr.ToString();
		}
	;

enumVal
	:	number (BAR IDENTIFIER | PLUS number)*
	|	IDENTIFIER LPAREN (~(RPAREN))* RPAREN
	;

structMemberDeclaration [Dictionary<string, IdhCommentProcessor.CommentInfo> members, ref string addComment]
	{
		StringBuilder bldr = new StringBuilder(addComment);
		addComment = string.Empty;
		IdhCommentProcessor.CommentInfo info = new IdhCommentProcessor.CommentInfo(string.Empty, null, 0);
		bool fCheckInline = true;
	}
	:	IDENTIFIER name:IDENTIFIER (ATTRIBUTE)? SEMICOLON (lineComment:COMMENT
			{
				if (fCheckInline && name.getLine() == lineComment.getLine())
				{	// inline comment belongs to current member
					bldr.Append(#lineComment.getText());
				}
				else
				{	// comment belongs to following member
					if (fCheckInline)
					{	// append all comments we got so far and create a new CommentInfo
						members[#name.getText()] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, 0);
						bldr = new StringBuilder();
						fCheckInline = false;
					}
					bldr.Append(#lineComment.getText());
				}
			}
			)*
		{
			if (fCheckInline)
			{	// append all comments left if there was none
				members[#name.getText()] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, name.getLine());
			}
			else
				addComment = bldr.ToString();
		}
	;

methodDeclaration [Dictionary<string, IdhCommentProcessor.CommentInfo> methods]
	{
		StringBuilder bldr = new StringBuilder();
		Dictionary<string, IdhCommentProcessor.CommentInfo> parameters = new Dictionary<string, IdhCommentProcessor.CommentInfo>();
		string lastParamName = null;
		int lastParamLine = 0;
	}
	:	(comment:COMMENT { bldr.Append(comment.getText()); })* (ATTRIBUTE)? IDENTIFIER name:IDENTIFIER
			LPAREN (COMMENT)? (lastParamName = parameterDeclaration[parameters] { lastParamLine = LT(1).getLine();})*
			RPAREN SEMICOLON ({LT(1).getLine() == lastParamLine}? paramComment:COMMENT )?
		{
			// This is tricky. We might get a comment after the semicolon - which really belongs to
			// the last parameter before the end of the method declaration. So we remember the
			// name of the last method so that we can assign the comment correctly.
			if (paramComment != null && lastParamName != null)
			{
				IdhCommentProcessor.CommentInfo lastParam = parameters[lastParamName];
				lastParam.Comment = paramComment.getText();
			}

			string key = name.getText();
			IdhCommentProcessor.CommentInfo info;
			if (methods.ContainsKey(key))
				info = methods[key];
			else
				info = new IdhCommentProcessor.CommentInfo();
			info.Comment = bldr.ToString();
			info.Children = parameters;
			info.LineNumber = name.getLine();
			methods[key] = info;

			foreach (IdhCommentProcessor.CommentInfo paramInfo in parameters.Values)
			{
				if (paramInfo.Attributes.ContainsKey("retval"))
				{
					info.Attributes.Add("retval", paramInfo.Attributes["retval"]);
					break;
				}
			}

			//#if DEBUG_IDHGRAMMAR
			//System.Diagnostics.Debug.WriteLine("		method declaration: " + key + ", next: " + LT(1).getText());
			//#endif
		}
	;

parameterDeclaration [Dictionary<string, IdhCommentProcessor.CommentInfo> parameters] returns [string paramName]
	{
		StringBuilder bldr = new StringBuilder();
		paramName = null;
	}
	:	(attribute:ATTRIBUTE)? ("const")? IDENTIFIER (STAR)* name:IDENTIFIER (LBRACKET (enumVal)? RBRACKET)* (COMMENT COMMA | COMMA)?
			(comment:COMMENT { bldr.Append(comment.getText()); })*
		{
			paramName = IDLConversions.ConvertParamName(name.getText());
			parameters[paramName] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, name.getLine());
			if (attribute != null && attribute.getText().Contains("retval"))
				parameters[paramName].Attributes.Add("retval", paramName);
		}
	;

number
	:	(MINUS)? IDENTIFIER
	;

guid
	:	IDENTIFIER MINUS IDENTIFIER MINUS IDENTIFIER MINUS IDENTIFIER MINUS IDENTIFIER
	;

constDeclaration
	:	"const" IDENTIFIER IDENTIFIER EQUAL enumVal SEMICOLON
	;

comment returns [string s]
	{
		s = string.Empty;
		StringBuilder bldr = new StringBuilder();
	}
	:	( text:COMMENT
			{ bldr.Append(text.getText()); }
		| PREPROCESSOR
		)*
		{
			s = bldr.ToString();
		}
	;

import
	: "import"^ STRING_LITERAL (COMMA STRING_LITERAL)* (SEMICOLON)?
	;

//-----------------------------------------------------------------------------------------------
//	IDH Lexical rules
//-----------------------------------------------------------------------------------------------
class IdhLexer extends Lexer;
options {
	k=2; // needed for comments
	charVocabulary='\3'..'\377';
	caseSensitiveLiterals=true;
	testLiterals=true;
	filter=IGNORE;
}

COMMENT
	:	"/*"  ( options {greedy=false;}:. {if (cached_LA1 == '\n') newline(); })* "*/"
	|	("//" (~('\n'))* '\n' { newline(); })+
	;

ATTRIBUTE
	:	'[' (WS)* ATTRVAL (WS)* (COMMA (WS)* ATTRVAL (WS)*)* ']'
	;

protected
ATTRVAL
	:	(IDENTIFIER (WS)* LPAREN) => IDENTIFIER (WS)* LPAREN (WS)* (EXPRESSION | ATTRLIST) (WS)* RPAREN
	|	IDENTIFIER
	;

protected
VARIABLE
	:	(STAR (WS)* )* IDENTIFIER
	;

protected
EXPRESSION
	:	(VARIABLE (WS)* ('-'|'+'|'*'|'/')) => VARIABLE (WS)* ('-'|'+'|'*'|'/') (WS)* VARIABLE
	|	VARIABLE
	|	MINUS (DIGIT)+
	;

protected
ATTRLIST
	:	'"' (~'"')* '"' (COMMA (WS)* '"' (~'"')* '"' )*
	;

PREPROCESSOR
	:	("#endif" ('\r'|'\n'|)) => "#endif"
	|	'#' IDENTIFIER WS ( options {greedy=false;}:(~'\n'))* '\n' { newline(); }
	;


STRING_LITERAL
	: '"'! (ESC|~'"')* '"'!
	;

protected
ESC
options {
  paraphrase = "an escape sequence";
}
	:	'\\'
		(	'n'
		|	't'
		|	'v'
		|	'b'
		|	'r'
		|	'f'
		|	'a'
		|	'\\'
		|	'?'
		|	'\''
		|	'"'
		|	('0' | '1' | '2' | '3')
			(
				/* Since a digit can occur in a string literal,
				 * which can follow an ESC reference, ANTLR
				 * does not know if you want to match the digit
				 * here (greedy) or in string literal.
				 * The same applies for the next two decisions
				 * with the warnWhenFollowAmbig option.
				 */
				options {
					warnWhenFollowAmbig = false;
				}
			:	OCTDIGIT
				(
					options {
						warnWhenFollowAmbig = false;
					}
				:	OCTDIGIT
				)?
			)?
		|   'x' HEXDIGIT
			(
				options {
					warnWhenFollowAmbig = false;
				}
			:	HEXDIGIT
			)?
		)
	;

protected
OCTDIGIT
options {
  paraphrase = "an octal digit";
}
	:	'0'..'7'
	;

// Serves also as number
IDENTIFIER
	:	("cpp_quote" LPAREN (~'\n')*  '\n') => "cpp_quote" LPAREN (~'\n')*  '\n' { newline(); }
	|	(LETTER | DIGIT)+
	;

protected
DIGIT
	:	'0'..'9'
	;

protected
HEXDIGIT
	:	DIGIT
	|	'A'..'F'
	|	'a'..'f'
	;

protected
LETTER
	: 'A'..'Z'
	| 'a'..'z'
	| '_'
	;

LPAREN
	:	'('
	;

RPAREN
	:	')'
	;

LBRACE
	:	'{'
	;

RBRACE
	:	'}'
	;

LBRACKET
	:	'['
	;

RBRACKET
	:	']'
	;

SEMICOLON
	:	';'
	;

COMMA
	:	','
	;

MINUS
	:	'-'
	;

EQUAL
	:	'='
	;

STAR
	:	'*'
	;

PLUS
	:	'+'
	;

BAR
	:	'|'
	;

protected
WS
	:	'\n' { newline(); }
	|	'\r' '\n' { newline(); }
	|	' '
	|	'\t'
	{$setType(Token.SKIP);}
	;

protected
IGNORE
	:	'\n' { newline(); }
	|	'\r'
	|	' '
	|	'\t'
	;