/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrpTokenStreamFilter.hpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implementions of methods for classes that are extensions to the ANTLR classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Grp.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Get the next token from the lexer. If it is a C-preprocessor line marker, do something
	special, otherwise pass the token on through to the parser.
----------------------------------------------------------------------------------------------*/
RefToken GrpTokenStreamFilter::nextToken()
{
	RefToken tok;

	if (m_tokPeek)
	{
		tok = m_tokPeek;
		m_tokPeek = RefToken(NULL);
	}
	else
		tok = m_lexer->nextToken();

	while (tok && tok->getType() == OP_LINEMARKER )
	{
		//	Handle the information from the line-and-file marker.
		RefToken tokLineNumber = m_lexer->nextToken();
		Assert(tokLineNumber && tokLineNumber->getType() == LIT_INT);
		RefToken tokFileName = m_lexer->nextToken();
		//Assert(tokFileName && tokFileName->getType() == LIT_STRING);

		int nLineInMarker = atoi(tokLineNumber->getText().c_str());
		int nLinePre = tokLineNumber->getLine();

		m_staPrevFile = m_staFile;
		m_nPrevLineOffset = m_nLineOffset;
		m_nLastLineMarker = nLinePre;

		if (tokFileName && tokFileName->getType() == LIT_STRING)
		{
			m_staFile = tokFileName->getText().c_str();
			tok = m_lexer->nextToken();
		}
		else
		{
			// m_staFile stays the same
			tok = tokFileName;
		}

		m_nLineOffset = nLineInMarker - nLinePre - 1;	// -1, because line marker gives
														// the number of the NEXT line
	}

	if (tok && tok->getType() == LITERAL_else)
	{
		//	"else" immediately followed by "if" on the same line is equivalent to "elseif",
		//	which does not need a separate "endif".
		m_tokPeek = m_lexer->nextToken();
		if (m_tokPeek && m_tokPeek->getType() == LITERAL_if &&
			m_tokPeek->getLine() == tok->getLine())
		{
			tok->setType(Zelseif);
			tok->setText("else if");
			m_tokPeek = RefToken(NULL);	// throw away if the if
		}
		//	otherwise, keep the peeked-at token for the next nextToken() call.
	}
	else if (tok && tok->getType() == AT_IDENT)
	{
		//	Break the token of the form "@abc" or "@:abc" into two tokens: OP_AT and Qalias.
		std::string s = tok->getText();
		Assert(s[0] == '@');
		if (s.length() > 1)
		{
			unsigned int ichMin = (s[1] == ':') ? 2 : 1;	// ignore colon
			if (s.length() > ichMin)
			{
				std::string sIdent = s.substr(ichMin, s.length());
				RefToken tokNext = m_lexer->publicMakeToken(Qalias);
				if (s[ichMin] >= '0' && s[ichMin] <= '9')
					tokNext->setType(LIT_INT);
				tokNext->setText(sIdent);
				tokNext->setLine(tok->getLine());
				m_tokPeek = tokNext;
			}
		}
		tok->setType(OP_AT);
		tok->setText("@");
	}

	//	Adjust the line and file information in the token.
	if (tok)
	{
		Token * bareToken = tok.get();
		GrpToken * wrToken = dynamic_cast<GrpToken *>(bareToken);
		Assert(wrToken);
		int nLinePre = tok->getLine();
		wrToken->SetOrigLineAndFile(nLinePre + m_nLineOffset, m_staFile);
	}

	return tok;

}


/*----------------------------------------------------------------------------------------------
	Initialize a tree node with the line-and-file information from the lexer token.
----------------------------------------------------------------------------------------------*/
void GrpASTNode::initialize(RefToken t)
{
	CommonASTNode::initialize(t);
	Token * bareToken = t.get();
	GrpToken * wrToken = dynamic_cast<GrpToken *>(bareToken);
	Assert(wrToken);
	m_lnf = wrToken->LineAndFile();
}


/*----------------------------------------------------------------------------------------------
	Intercept a lexer error and add the original line and file information.
----------------------------------------------------------------------------------------------*/
void GrpTokenStreamFilter::ReportLexerError(const ScannerException & ex)
{
	int nLinePre = ex.getLine();
	int nLineOrig = nLinePre + m_nLineOffset;
	AddGlobalError(true, 101, ex.getErrorMessage(), GrpLineAndFile(nLinePre, nLineOrig, m_staFile));
}

/*----------------------------------------------------------------------------------------------
	Intercept a parser error and add the original line and file information.
----------------------------------------------------------------------------------------------*/
void GrpTokenStreamFilter::ReportParserError(const ParserException & ex)
{
	int nLinePre = ex.getLine();
	if (nLinePre <= m_nLastLineMarker)
		//	Problematic token was before the last line marker in the pre-processed file.
		AddGlobalError(true, 102, ex.getErrorMessage(),
			GrpLineAndFile(nLinePre, nLinePre + m_nPrevLineOffset, m_staPrevFile));
	else
		AddGlobalError(true, 103, ex.getErrorMessage(),
			GrpLineAndFile(nLinePre, nLinePre + m_nLineOffset, m_staFile));
}
