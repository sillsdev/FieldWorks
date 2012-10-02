/*
 * ANTLR-generated file resulting from grammar c:\fw\src\graphite\grcompiler\grpparser.g
 *
 * Terence Parr, MageLang Institute
 * with John Lilley, Empathy Software
 * ANTLR Version 2.6.0; 1996-1999
 */

#include "GrpLexer.hpp"
#include "GrpParserTokenTypes.hpp"
#include "antlr/ScannerException.hpp"
#include "antlr/CharBuffer.hpp"



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


GrpLexer::GrpLexer(std::istream& in)
	: CharScanner(new CharBuffer(in))
{
	setCaseSensitive(true);
	initLiterals();
}

GrpLexer::GrpLexer(InputBuffer& ib)
	: CharScanner(ib)
{
	setCaseSensitive(true);
	initLiterals();
}

GrpLexer::GrpLexer(const LexerSharedInputState& state)
	: CharScanner(state)
{
	setCaseSensitive(true);
	initLiterals();
}

void GrpLexer::initLiterals()
{
	literals["positioning"] = 56;
	literals["min"] = 76;
	literals["name"] = 16;
	literals["endenvironment"] = 10;
	literals["endtable"] = 15;
	literals["false"] = 74;
	literals["true"] = 73;
	literals["glyph"] = 23;
	literals["codepoint"] = 26;
	literals["pass"] = 36;
	literals["table"] = 14;
	literals["substitution"] = 35;
	literals["string"] = 22;
	literals["environment"] = 9;
	literals["glyphid"] = 27;
	literals["justification"] = 54;
	literals["pseudo"] = 24;
	literals["position"] = 55;
	literals["endif"] = 40;
	literals["languages"] = 34;
	literals["elseif"] = 42;
	literals["feature"] = 32;
	literals["max"] = 75;
	literals["postscript"] = 28;
	literals["unicode"] = 29;
	literals["if"] = 38;
	literals["linebreak"] = 57;
	literals["else"] = 39;
	literals["endpass"] = 37;
	literals["language"] = 33;
}
bool GrpLexer::getCaseSensitiveLiterals() const
{
	return false;
}

RefToken GrpLexer::nextToken()
{
	RefToken _rettoken;
	for (;;) {
		RefToken _rettoken;
		int _ttype = Token::INVALID_TYPE;
		resetText();
		try {   // for error handling
			switch ( LA(1)) {
			case static_cast<unsigned char>('\t'):
			case static_cast<unsigned char>('\n'):
			case static_cast<unsigned char>('\14'):
			case static_cast<unsigned char>('\r'):
			case static_cast<unsigned char>(' '):
			{
				mWS(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('0'):
			case static_cast<unsigned char>('1'):
			case static_cast<unsigned char>('2'):
			case static_cast<unsigned char>('3'):
			case static_cast<unsigned char>('4'):
			case static_cast<unsigned char>('5'):
			case static_cast<unsigned char>('6'):
			case static_cast<unsigned char>('7'):
			case static_cast<unsigned char>('8'):
			case static_cast<unsigned char>('9'):
			{
				mLIT_INT(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('\''):
			case static_cast<unsigned char>('\221'):
			case static_cast<unsigned char>('\222'):
			{
				mLIT_CHAR(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('"'):
			case static_cast<unsigned char>('\223'):
			case static_cast<unsigned char>('\224'):
			{
				mLIT_STRING(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>(':'):
			{
				mOP_COLON(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>(';'):
			{
				mOP_SEMI(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('['):
			{
				mOP_LBRACKET(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>(']'):
			{
				mOP_RBRACKET(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('('):
			{
				mOP_LPAREN(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>(')'):
			{
				mOP_RPAREN(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('{'):
			{
				mOP_LBRACE(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('}'):
			{
				mOP_RBRACE(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>(','):
			{
				mOP_COMMA(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('$'):
			{
				mOP_DOLLAR(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('&'):
			{
				mOP_AND(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('|'):
			{
				mOP_OR(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('\\'):
			{
				mOP_BSLASH(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('_'):
			{
				mOP_UNDER(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('?'):
			{
				mOP_QUESTION(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('^'):
			{
				mOP_CARET(true);
				_rettoken=_returnToken;
				break;
			}
			case static_cast<unsigned char>('@'):
			{
				mAT_IDENT(true);
				_rettoken=_returnToken;
				break;
			}
			default:
				if ((LA(1)==static_cast<unsigned char>('/')) && (LA(2)==static_cast<unsigned char>('/'))) {
					mCOMMENT_SL(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('/')) && (LA(2)==static_cast<unsigned char>('*'))) {
					mCOMMENT_ML(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('U')) && (LA(2)==static_cast<unsigned char>('+'))) {
					mLIT_UHEX(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('.')) && (LA(2)==static_cast<unsigned char>('.'))) {
					mOP_DOTDOT(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('<')) && (LA(2)==static_cast<unsigned char>('='))) {
					mOP_LE(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('=')) && (LA(2)==static_cast<unsigned char>('='))) {
					mOP_EQUALEQUAL(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('!')) && (LA(2)==static_cast<unsigned char>('='))) {
					mOP_NE(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('>')) && (LA(2)==static_cast<unsigned char>('='))) {
					mOP_GE(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('+')) && (LA(2)==static_cast<unsigned char>('='))) {
					mOP_PLUSEQUAL(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('-')) && (LA(2)==static_cast<unsigned char>('='))) {
					mOP_MINUSEQUAL(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('*')) && (LA(2)==static_cast<unsigned char>('='))) {
					mOP_MULTEQUAL(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('/')) && (LA(2)==static_cast<unsigned char>('='))) {
					mOP_DIVEQUAL(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('#')) && (LA(2)==static_cast<unsigned char>('l'))) {
					mOP_LINEMARKER(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('.'))) {
					mOP_DOT(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('!'))) {
					mOP_NOT(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('<'))) {
					mOP_LT(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('='))) {
					mOP_EQ(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('>'))) {
					mOP_GT(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('+'))) {
					mOP_PLUS(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('-'))) {
					mOP_MINUS(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('*'))) {
					mOP_MULT(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('/'))) {
					mOP_DIV(true);
					_rettoken=_returnToken;
				}
				else if ((LA(1)==static_cast<unsigned char>('#'))) {
					mOP_HASH(true);
					_rettoken=_returnToken;
				}
				else if ((_tokenSet_0.member(LA(1)))) {
					mIDENT(true);
					_rettoken=_returnToken;
				}
			else {
				if (LA(1)==EOF_CHAR) {_returnToken = makeToken(Token::EOF_TYPE);}
				else {throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());}
			}
			}
			if ( !_returnToken ) goto tryAgain; // found SKIP token
			_ttype = _returnToken->getType();
			_returnToken->setType(_ttype);
			return _returnToken;
		}
		catch (ScannerException& e) {
			reportError(e);
			consume();
		}
tryAgain:;
	}
}

void GrpLexer::mWS(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = WS;
	int _saveIndex;

	{
	switch ( LA(1)) {
	case static_cast<unsigned char>(' '):
	{
		match(static_cast<unsigned char>(' '));
		break;
	}
	case static_cast<unsigned char>('\t'):
	{
		match(static_cast<unsigned char>('\t'));
		break;
	}
	case static_cast<unsigned char>('\14'):
	{
		match(static_cast<unsigned char>('\14'));
		break;
	}
	case static_cast<unsigned char>('\n'):
	case static_cast<unsigned char>('\r'):
	{
		{
		if ((LA(1)==static_cast<unsigned char>('\r')) && (LA(2)==static_cast<unsigned char>('\n'))) {
			match("\r\n");
		}
		else if ((LA(1)==static_cast<unsigned char>('\r'))) {
			match(static_cast<unsigned char>('\r'));
		}
		else if ((LA(1)==static_cast<unsigned char>('\n'))) {
			match(static_cast<unsigned char>('\n'));
		}
		else {
			throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
		}

		}
		newline();
		break;
	}
	default:
	{
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}
	}
	}
	_ttype = Token::SKIP;
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mCOMMENT_SL(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = COMMENT_SL;
	int _saveIndex;

	match("//");
	{
	do {
		if ((_tokenSet_1.member(LA(1)))) {
			{
			match(_tokenSet_1);
			}
		}
		else {
			goto _loop422;
		}

	} while (true);
	_loop422:;
	}
	_ttype = Token::SKIP;
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mCOMMENT_ML(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = COMMENT_ML;
	int _saveIndex;

	match("/*");
	{
	do {
		switch ( LA(1)) {
		case static_cast<unsigned char>('\n'):
		{
			match(static_cast<unsigned char>('\n'));
			newline();
			break;
		}
		case static_cast<unsigned char>('\0'):
		case static_cast<unsigned char>('\1'):
		case static_cast<unsigned char>('\2'):
		case static_cast<unsigned char>('\3'):
		case static_cast<unsigned char>('\4'):
		case static_cast<unsigned char>('\5'):
		case static_cast<unsigned char>('\6'):
		case static_cast<unsigned char>('\7'):
		case static_cast<unsigned char>('\10'):
		case static_cast<unsigned char>('\t'):
		case static_cast<unsigned char>('\13'):
		case static_cast<unsigned char>('\14'):
		case static_cast<unsigned char>('\r'):
		case static_cast<unsigned char>('\16'):
		case static_cast<unsigned char>('\17'):
		case static_cast<unsigned char>('\20'):
		case static_cast<unsigned char>('\21'):
		case static_cast<unsigned char>('\22'):
		case static_cast<unsigned char>('\23'):
		case static_cast<unsigned char>('\24'):
		case static_cast<unsigned char>('\25'):
		case static_cast<unsigned char>('\26'):
		case static_cast<unsigned char>('\27'):
		case static_cast<unsigned char>('\30'):
		case static_cast<unsigned char>('\31'):
		case static_cast<unsigned char>('\32'):
		case static_cast<unsigned char>('\33'):
		case static_cast<unsigned char>('\34'):
		case static_cast<unsigned char>('\35'):
		case static_cast<unsigned char>('\36'):
		case static_cast<unsigned char>('\37'):
		case static_cast<unsigned char>(' '):
		case static_cast<unsigned char>('!'):
		case static_cast<unsigned char>('"'):
		case static_cast<unsigned char>('#'):
		case static_cast<unsigned char>('$'):
		case static_cast<unsigned char>('%'):
		case static_cast<unsigned char>('&'):
		case static_cast<unsigned char>('\''):
		case static_cast<unsigned char>('('):
		case static_cast<unsigned char>(')'):
		case static_cast<unsigned char>('+'):
		case static_cast<unsigned char>(','):
		case static_cast<unsigned char>('-'):
		case static_cast<unsigned char>('.'):
		case static_cast<unsigned char>('/'):
		case static_cast<unsigned char>('0'):
		case static_cast<unsigned char>('1'):
		case static_cast<unsigned char>('2'):
		case static_cast<unsigned char>('3'):
		case static_cast<unsigned char>('4'):
		case static_cast<unsigned char>('5'):
		case static_cast<unsigned char>('6'):
		case static_cast<unsigned char>('7'):
		case static_cast<unsigned char>('8'):
		case static_cast<unsigned char>('9'):
		case static_cast<unsigned char>(':'):
		case static_cast<unsigned char>(';'):
		case static_cast<unsigned char>('<'):
		case static_cast<unsigned char>('='):
		case static_cast<unsigned char>('>'):
		case static_cast<unsigned char>('?'):
		case static_cast<unsigned char>('@'):
		case static_cast<unsigned char>('A'):
		case static_cast<unsigned char>('B'):
		case static_cast<unsigned char>('C'):
		case static_cast<unsigned char>('D'):
		case static_cast<unsigned char>('E'):
		case static_cast<unsigned char>('F'):
		case static_cast<unsigned char>('G'):
		case static_cast<unsigned char>('H'):
		case static_cast<unsigned char>('I'):
		case static_cast<unsigned char>('J'):
		case static_cast<unsigned char>('K'):
		case static_cast<unsigned char>('L'):
		case static_cast<unsigned char>('M'):
		case static_cast<unsigned char>('N'):
		case static_cast<unsigned char>('O'):
		case static_cast<unsigned char>('P'):
		case static_cast<unsigned char>('Q'):
		case static_cast<unsigned char>('R'):
		case static_cast<unsigned char>('S'):
		case static_cast<unsigned char>('T'):
		case static_cast<unsigned char>('U'):
		case static_cast<unsigned char>('V'):
		case static_cast<unsigned char>('W'):
		case static_cast<unsigned char>('X'):
		case static_cast<unsigned char>('Y'):
		case static_cast<unsigned char>('Z'):
		case static_cast<unsigned char>('['):
		case static_cast<unsigned char>('\\'):
		case static_cast<unsigned char>(']'):
		case static_cast<unsigned char>('^'):
		case static_cast<unsigned char>('_'):
		case static_cast<unsigned char>('`'):
		case static_cast<unsigned char>('a'):
		case static_cast<unsigned char>('b'):
		case static_cast<unsigned char>('c'):
		case static_cast<unsigned char>('d'):
		case static_cast<unsigned char>('e'):
		case static_cast<unsigned char>('f'):
		case static_cast<unsigned char>('g'):
		case static_cast<unsigned char>('h'):
		case static_cast<unsigned char>('i'):
		case static_cast<unsigned char>('j'):
		case static_cast<unsigned char>('k'):
		case static_cast<unsigned char>('l'):
		case static_cast<unsigned char>('m'):
		case static_cast<unsigned char>('n'):
		case static_cast<unsigned char>('o'):
		case static_cast<unsigned char>('p'):
		case static_cast<unsigned char>('q'):
		case static_cast<unsigned char>('r'):
		case static_cast<unsigned char>('s'):
		case static_cast<unsigned char>('t'):
		case static_cast<unsigned char>('u'):
		case static_cast<unsigned char>('v'):
		case static_cast<unsigned char>('w'):
		case static_cast<unsigned char>('x'):
		case static_cast<unsigned char>('y'):
		case static_cast<unsigned char>('z'):
		case static_cast<unsigned char>('{'):
		case static_cast<unsigned char>('|'):
		case static_cast<unsigned char>('}'):
		case static_cast<unsigned char>('~'):
		case static_cast<unsigned char>('\177'):
		case static_cast<unsigned char>('\200'):
		case static_cast<unsigned char>('\201'):
		case static_cast<unsigned char>('\202'):
		case static_cast<unsigned char>('\203'):
		case static_cast<unsigned char>('\204'):
		case static_cast<unsigned char>('\205'):
		case static_cast<unsigned char>('\206'):
		case static_cast<unsigned char>('\207'):
		case static_cast<unsigned char>('\210'):
		case static_cast<unsigned char>('\211'):
		case static_cast<unsigned char>('\212'):
		case static_cast<unsigned char>('\213'):
		case static_cast<unsigned char>('\214'):
		case static_cast<unsigned char>('\215'):
		case static_cast<unsigned char>('\216'):
		case static_cast<unsigned char>('\217'):
		case static_cast<unsigned char>('\220'):
		case static_cast<unsigned char>('\221'):
		case static_cast<unsigned char>('\222'):
		case static_cast<unsigned char>('\223'):
		case static_cast<unsigned char>('\224'):
		case static_cast<unsigned char>('\225'):
		case static_cast<unsigned char>('\226'):
		case static_cast<unsigned char>('\227'):
		case static_cast<unsigned char>('\230'):
		case static_cast<unsigned char>('\231'):
		case static_cast<unsigned char>('\232'):
		case static_cast<unsigned char>('\233'):
		case static_cast<unsigned char>('\234'):
		case static_cast<unsigned char>('\235'):
		case static_cast<unsigned char>('\236'):
		case static_cast<unsigned char>('\237'):
		case static_cast<unsigned char>('\240'):
		case static_cast<unsigned char>('\241'):
		case static_cast<unsigned char>('\242'):
		case static_cast<unsigned char>('\243'):
		case static_cast<unsigned char>('\244'):
		case static_cast<unsigned char>('\245'):
		case static_cast<unsigned char>('\246'):
		case static_cast<unsigned char>('\247'):
		case static_cast<unsigned char>('\250'):
		case static_cast<unsigned char>('\251'):
		case static_cast<unsigned char>('\252'):
		case static_cast<unsigned char>('\253'):
		case static_cast<unsigned char>('\254'):
		case static_cast<unsigned char>('\255'):
		case static_cast<unsigned char>('\256'):
		case static_cast<unsigned char>('\257'):
		case static_cast<unsigned char>('\260'):
		case static_cast<unsigned char>('\261'):
		case static_cast<unsigned char>('\262'):
		case static_cast<unsigned char>('\263'):
		case static_cast<unsigned char>('\264'):
		case static_cast<unsigned char>('\265'):
		case static_cast<unsigned char>('\266'):
		case static_cast<unsigned char>('\267'):
		case static_cast<unsigned char>('\270'):
		case static_cast<unsigned char>('\271'):
		case static_cast<unsigned char>('\272'):
		case static_cast<unsigned char>('\273'):
		case static_cast<unsigned char>('\274'):
		case static_cast<unsigned char>('\275'):
		case static_cast<unsigned char>('\276'):
		case static_cast<unsigned char>('\277'):
		case static_cast<unsigned char>('\300'):
		case static_cast<unsigned char>('\301'):
		case static_cast<unsigned char>('\302'):
		case static_cast<unsigned char>('\303'):
		case static_cast<unsigned char>('\304'):
		case static_cast<unsigned char>('\305'):
		case static_cast<unsigned char>('\306'):
		case static_cast<unsigned char>('\307'):
		case static_cast<unsigned char>('\310'):
		case static_cast<unsigned char>('\311'):
		case static_cast<unsigned char>('\312'):
		case static_cast<unsigned char>('\313'):
		case static_cast<unsigned char>('\314'):
		case static_cast<unsigned char>('\315'):
		case static_cast<unsigned char>('\316'):
		case static_cast<unsigned char>('\317'):
		case static_cast<unsigned char>('\320'):
		case static_cast<unsigned char>('\321'):
		case static_cast<unsigned char>('\322'):
		case static_cast<unsigned char>('\323'):
		case static_cast<unsigned char>('\324'):
		case static_cast<unsigned char>('\325'):
		case static_cast<unsigned char>('\326'):
		case static_cast<unsigned char>('\327'):
		case static_cast<unsigned char>('\330'):
		case static_cast<unsigned char>('\331'):
		case static_cast<unsigned char>('\332'):
		case static_cast<unsigned char>('\333'):
		case static_cast<unsigned char>('\334'):
		case static_cast<unsigned char>('\335'):
		case static_cast<unsigned char>('\336'):
		case static_cast<unsigned char>('\337'):
		case static_cast<unsigned char>('\340'):
		case static_cast<unsigned char>('\341'):
		case static_cast<unsigned char>('\342'):
		case static_cast<unsigned char>('\343'):
		case static_cast<unsigned char>('\344'):
		case static_cast<unsigned char>('\345'):
		case static_cast<unsigned char>('\346'):
		case static_cast<unsigned char>('\347'):
		case static_cast<unsigned char>('\350'):
		case static_cast<unsigned char>('\351'):
		case static_cast<unsigned char>('\352'):
		case static_cast<unsigned char>('\353'):
		case static_cast<unsigned char>('\354'):
		case static_cast<unsigned char>('\355'):
		case static_cast<unsigned char>('\356'):
		case static_cast<unsigned char>('\357'):
		case static_cast<unsigned char>('\360'):
		case static_cast<unsigned char>('\361'):
		case static_cast<unsigned char>('\362'):
		case static_cast<unsigned char>('\363'):
		case static_cast<unsigned char>('\364'):
		case static_cast<unsigned char>('\365'):
		case static_cast<unsigned char>('\366'):
		case static_cast<unsigned char>('\367'):
		case static_cast<unsigned char>('\370'):
		case static_cast<unsigned char>('\371'):
		case static_cast<unsigned char>('\372'):
		case static_cast<unsigned char>('\373'):
		case static_cast<unsigned char>('\374'):
		case static_cast<unsigned char>('\375'):
		case static_cast<unsigned char>('\376'):
		case static_cast<unsigned char>('\377'):
		{
			{
			match(_tokenSet_2);
			}
			break;
		}
		default:
			if (((LA(1)==static_cast<unsigned char>('*')) && ((LA(2) >= static_cast<unsigned char>('\0') && LA(2) <= static_cast<unsigned char>('\377'))) && ((LA(3) >= static_cast<unsigned char>('\0') && LA(3) <= static_cast<unsigned char>('\377'))))&&( LA(2)!='/' )) {
				match(static_cast<unsigned char>('*'));
			}
		else {
			goto _loop426;
		}
		}
	} while (true);
	_loop426:;
	}
	match("*/");
	_ttype = Token::SKIP;
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mLIT_INT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = LIT_INT;
	int _saveIndex;

	{
	if ((LA(1)==static_cast<unsigned char>('0')) && (LA(2)==static_cast<unsigned char>('x'))) {
		{
		match("0x");
		{
		int _cnt433=0;
		do {
			if ((_tokenSet_3.member(LA(1)))) {
				mXDIGIT(false);
			}
			else {
				if ( _cnt433>=1 ) { goto _loop433; } else {throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());}
			}

			_cnt433++;
		} while (true);
		_loop433:;
		}
		}
	}
	else if (((LA(1) >= static_cast<unsigned char>('0') && LA(1) <= static_cast<unsigned char>('9')))) {
		{
		int _cnt430=0;
		do {
			if (((LA(1) >= static_cast<unsigned char>('0') && LA(1) <= static_cast<unsigned char>('9')))) {
				mDIGIT(false);
			}
			else {
				if ( _cnt430>=1 ) { goto _loop430; } else {throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());}
			}

			_cnt430++;
		} while (true);
		_loop430:;
		}
	}
	else {
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}

	}
	{
	switch ( LA(1)) {
	case static_cast<unsigned char>('m'):
	{
		match(static_cast<unsigned char>('m'));
		break;
	}
	case static_cast<unsigned char>('M'):
	{
		match(static_cast<unsigned char>('M'));
		break;
	}
	default:
		{
		}
	}
	}
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mDIGIT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = DIGIT;
	int _saveIndex;

	matchRange(static_cast<unsigned char>('0'),static_cast<unsigned char>('9'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mXDIGIT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = XDIGIT;
	int _saveIndex;

	switch ( LA(1)) {
	case static_cast<unsigned char>('0'):
	case static_cast<unsigned char>('1'):
	case static_cast<unsigned char>('2'):
	case static_cast<unsigned char>('3'):
	case static_cast<unsigned char>('4'):
	case static_cast<unsigned char>('5'):
	case static_cast<unsigned char>('6'):
	case static_cast<unsigned char>('7'):
	case static_cast<unsigned char>('8'):
	case static_cast<unsigned char>('9'):
	{
		matchRange(static_cast<unsigned char>('0'),static_cast<unsigned char>('9'));
		break;
	}
	case static_cast<unsigned char>('a'):
	case static_cast<unsigned char>('b'):
	case static_cast<unsigned char>('c'):
	case static_cast<unsigned char>('d'):
	case static_cast<unsigned char>('e'):
	case static_cast<unsigned char>('f'):
	{
		matchRange(static_cast<unsigned char>('a'),static_cast<unsigned char>('f'));
		break;
	}
	case static_cast<unsigned char>('A'):
	case static_cast<unsigned char>('B'):
	case static_cast<unsigned char>('C'):
	case static_cast<unsigned char>('D'):
	case static_cast<unsigned char>('E'):
	case static_cast<unsigned char>('F'):
	{
		matchRange(static_cast<unsigned char>('A'),static_cast<unsigned char>('F'));
		break;
	}
	default:
	{
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}
	}
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mLIT_UHEX(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = LIT_UHEX;
	int _saveIndex;

	match("U+");
	{
	int _cnt437=0;
	do {
		if ((_tokenSet_3.member(LA(1)))) {
			mXDIGIT(false);
		}
		else {
			if ( _cnt437>=1 ) { goto _loop437; } else {throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());}
		}

		_cnt437++;
	} while (true);
	_loop437:;
	}
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mLIT_CHAR(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = LIT_CHAR;
	int _saveIndex;

	_saveIndex=text.length();
	mSQUOTE(false);
	text.erase(_saveIndex);
	{
	if ((LA(1)==static_cast<unsigned char>('\\')) && (_tokenSet_4.member(LA(2))) && (LA(3)==static_cast<unsigned char>('\'')||LA(3)==static_cast<unsigned char>('\221')||LA(3)==static_cast<unsigned char>('\222'))) {
		mESC(false);
	}
	else if ((_tokenSet_5.member(LA(1))) && (LA(2)==static_cast<unsigned char>('\'')||LA(2)==static_cast<unsigned char>('\221')||LA(2)==static_cast<unsigned char>('\222'))) {
		{
		match(_tokenSet_5);
		}
	}
	else {
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}

	}
	_saveIndex=text.length();
	mSQUOTE(false);
	text.erase(_saveIndex);
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mSQUOTE(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = SQUOTE;
	int _saveIndex;

	{
	switch ( LA(1)) {
	case static_cast<unsigned char>('\''):
	{
		match(static_cast<unsigned char>('\''));
		break;
	}
	case static_cast<unsigned char>('\221'):
	{
		match(static_cast<unsigned char>('\221'));
		break;
	}
	case static_cast<unsigned char>('\222'):
	{
		match(static_cast<unsigned char>('\222'));
		break;
	}
	default:
	{
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}
	}
	}
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mESC(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = ESC;
	int _saveIndex;

	match(static_cast<unsigned char>('\\'));
	{
	switch ( LA(1)) {
	case static_cast<unsigned char>('n'):
	{
		match(static_cast<unsigned char>('n'));
		break;
	}
	case static_cast<unsigned char>('r'):
	{
		match(static_cast<unsigned char>('r'));
		break;
	}
	case static_cast<unsigned char>('t'):
	{
		match(static_cast<unsigned char>('t'));
		break;
	}
	case static_cast<unsigned char>('b'):
	{
		match(static_cast<unsigned char>('b'));
		break;
	}
	case static_cast<unsigned char>('f'):
	{
		match(static_cast<unsigned char>('f'));
		break;
	}
	case static_cast<unsigned char>('"'):
	{
		match(static_cast<unsigned char>('"'));
		break;
	}
	case static_cast<unsigned char>('\''):
	{
		match(static_cast<unsigned char>('\''));
		break;
	}
	case static_cast<unsigned char>('\\'):
	{
		match(static_cast<unsigned char>('\\'));
		break;
	}
	default:
	{
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}
	}
	}
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mLIT_STRING(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = LIT_STRING;
	int _saveIndex;

	_saveIndex=text.length();
	mDQUOTE(false);
	text.erase(_saveIndex);
	{
	do {
		if ((LA(1)==static_cast<unsigned char>('\\')) && (_tokenSet_4.member(LA(2))) && ((LA(3) >= static_cast<unsigned char>('\0') && LA(3) <= static_cast<unsigned char>('\377')))) {
			mESC(false);
		}
		else if ((_tokenSet_6.member(LA(1))) && ((LA(2) >= static_cast<unsigned char>('\0') && LA(2) <= static_cast<unsigned char>('\377')))) {
			{
			match(_tokenSet_6);
			}
		}
		else {
			goto _loop444;
		}

	} while (true);
	_loop444:;
	}
	_saveIndex=text.length();
	mDQUOTE(false);
	text.erase(_saveIndex);
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mDQUOTE(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = DQUOTE;
	int _saveIndex;

	{
	switch ( LA(1)) {
	case static_cast<unsigned char>('"'):
	{
		match(static_cast<unsigned char>('"'));
		break;
	}
	case static_cast<unsigned char>('\223'):
	{
		match(static_cast<unsigned char>('\223'));
		break;
	}
	case static_cast<unsigned char>('\224'):
	{
		match(static_cast<unsigned char>('\224'));
		break;
	}
	default:
	{
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}
	}
	}
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mODIGIT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = ODIGIT;
	int _saveIndex;

	matchRange(static_cast<unsigned char>('0'),static_cast<unsigned char>('7'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_DOT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_DOT;
	int _saveIndex;

	match(static_cast<unsigned char>('.'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_DOTDOT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_DOTDOT;
	int _saveIndex;

	match("..");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_COLON(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_COLON;
	int _saveIndex;

	match(static_cast<unsigned char>(':'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_SEMI(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_SEMI;
	int _saveIndex;

	match(static_cast<unsigned char>(';'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_LBRACKET(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_LBRACKET;
	int _saveIndex;

	match(static_cast<unsigned char>('['));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_RBRACKET(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_RBRACKET;
	int _saveIndex;

	match(static_cast<unsigned char>(']'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_LPAREN(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_LPAREN;
	int _saveIndex;

	match(static_cast<unsigned char>('('));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_RPAREN(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_RPAREN;
	int _saveIndex;

	match(static_cast<unsigned char>(')'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_LBRACE(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_LBRACE;
	int _saveIndex;

	match(static_cast<unsigned char>('{'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_RBRACE(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_RBRACE;
	int _saveIndex;

	match(static_cast<unsigned char>('}'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_NOT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_NOT;
	int _saveIndex;

	match(static_cast<unsigned char>('!'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_LT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_LT;
	int _saveIndex;

	match(static_cast<unsigned char>('<'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_LE(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_LE;
	int _saveIndex;

	match("<=");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_EQ(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_EQ;
	int _saveIndex;

	match(static_cast<unsigned char>('='));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_EQUALEQUAL(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_EQUALEQUAL;
	int _saveIndex;

	match("==");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_NE(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_NE;
	int _saveIndex;

	match("!=");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_GE(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_GE;
	int _saveIndex;

	match(">=");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_GT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_GT;
	int _saveIndex;

	match(static_cast<unsigned char>('>'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_PLUS(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_PLUS;
	int _saveIndex;

	match(static_cast<unsigned char>('+'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_PLUSEQUAL(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_PLUSEQUAL;
	int _saveIndex;

	match("+=");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_MINUS(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_MINUS;
	int _saveIndex;

	match(static_cast<unsigned char>('-'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_MINUSEQUAL(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_MINUSEQUAL;
	int _saveIndex;

	match("-=");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_MULT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_MULT;
	int _saveIndex;

	match(static_cast<unsigned char>('*'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_MULTEQUAL(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_MULTEQUAL;
	int _saveIndex;

	match("*=");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_DIV(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_DIV;
	int _saveIndex;

	match(static_cast<unsigned char>('/'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_DIVEQUAL(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_DIVEQUAL;
	int _saveIndex;

	match("/=");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_COMMA(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_COMMA;
	int _saveIndex;

	match(static_cast<unsigned char>(','));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_DOLLAR(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_DOLLAR;
	int _saveIndex;

	match(static_cast<unsigned char>('$'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_LINEMARKER(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_LINEMARKER;
	int _saveIndex;

	match("#line");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_HASH(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_HASH;
	int _saveIndex;

	match(static_cast<unsigned char>('#'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_AND(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_AND;
	int _saveIndex;

	match("&&");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_OR(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_OR;
	int _saveIndex;

	match("||");
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_BSLASH(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_BSLASH;
	int _saveIndex;

	match(static_cast<unsigned char>('\\'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_UNDER(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_UNDER;
	int _saveIndex;

	match(static_cast<unsigned char>('_'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_QUESTION(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_QUESTION;
	int _saveIndex;

	match(static_cast<unsigned char>('?'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mOP_CARET(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = OP_CARET;
	int _saveIndex;

	match(static_cast<unsigned char>('^'));
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mIDENT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = IDENT;
	int _saveIndex;

	{
	switch ( LA(1)) {
	case static_cast<unsigned char>('a'):
	case static_cast<unsigned char>('b'):
	case static_cast<unsigned char>('c'):
	case static_cast<unsigned char>('d'):
	case static_cast<unsigned char>('e'):
	case static_cast<unsigned char>('f'):
	case static_cast<unsigned char>('g'):
	case static_cast<unsigned char>('h'):
	case static_cast<unsigned char>('i'):
	case static_cast<unsigned char>('j'):
	case static_cast<unsigned char>('k'):
	case static_cast<unsigned char>('l'):
	case static_cast<unsigned char>('m'):
	case static_cast<unsigned char>('n'):
	case static_cast<unsigned char>('o'):
	case static_cast<unsigned char>('p'):
	case static_cast<unsigned char>('q'):
	case static_cast<unsigned char>('r'):
	case static_cast<unsigned char>('s'):
	case static_cast<unsigned char>('t'):
	case static_cast<unsigned char>('u'):
	case static_cast<unsigned char>('v'):
	case static_cast<unsigned char>('w'):
	case static_cast<unsigned char>('x'):
	case static_cast<unsigned char>('y'):
	case static_cast<unsigned char>('z'):
	{
		matchRange(static_cast<unsigned char>('a'),static_cast<unsigned char>('z'));
		break;
	}
	case static_cast<unsigned char>('A'):
	case static_cast<unsigned char>('B'):
	case static_cast<unsigned char>('C'):
	case static_cast<unsigned char>('D'):
	case static_cast<unsigned char>('E'):
	case static_cast<unsigned char>('F'):
	case static_cast<unsigned char>('G'):
	case static_cast<unsigned char>('H'):
	case static_cast<unsigned char>('I'):
	case static_cast<unsigned char>('J'):
	case static_cast<unsigned char>('K'):
	case static_cast<unsigned char>('L'):
	case static_cast<unsigned char>('M'):
	case static_cast<unsigned char>('N'):
	case static_cast<unsigned char>('O'):
	case static_cast<unsigned char>('P'):
	case static_cast<unsigned char>('Q'):
	case static_cast<unsigned char>('R'):
	case static_cast<unsigned char>('S'):
	case static_cast<unsigned char>('T'):
	case static_cast<unsigned char>('U'):
	case static_cast<unsigned char>('V'):
	case static_cast<unsigned char>('W'):
	case static_cast<unsigned char>('X'):
	case static_cast<unsigned char>('Y'):
	case static_cast<unsigned char>('Z'):
	{
		matchRange(static_cast<unsigned char>('A'),static_cast<unsigned char>('Z'));
		break;
	}
	default:
	{
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}
	}
	}
	{
	do {
		switch ( LA(1)) {
		case static_cast<unsigned char>('_'):
		{
			match(static_cast<unsigned char>('_'));
			break;
		}
		case static_cast<unsigned char>('a'):
		case static_cast<unsigned char>('b'):
		case static_cast<unsigned char>('c'):
		case static_cast<unsigned char>('d'):
		case static_cast<unsigned char>('e'):
		case static_cast<unsigned char>('f'):
		case static_cast<unsigned char>('g'):
		case static_cast<unsigned char>('h'):
		case static_cast<unsigned char>('i'):
		case static_cast<unsigned char>('j'):
		case static_cast<unsigned char>('k'):
		case static_cast<unsigned char>('l'):
		case static_cast<unsigned char>('m'):
		case static_cast<unsigned char>('n'):
		case static_cast<unsigned char>('o'):
		case static_cast<unsigned char>('p'):
		case static_cast<unsigned char>('q'):
		case static_cast<unsigned char>('r'):
		case static_cast<unsigned char>('s'):
		case static_cast<unsigned char>('t'):
		case static_cast<unsigned char>('u'):
		case static_cast<unsigned char>('v'):
		case static_cast<unsigned char>('w'):
		case static_cast<unsigned char>('x'):
		case static_cast<unsigned char>('y'):
		case static_cast<unsigned char>('z'):
		{
			matchRange(static_cast<unsigned char>('a'),static_cast<unsigned char>('z'));
			break;
		}
		case static_cast<unsigned char>('A'):
		case static_cast<unsigned char>('B'):
		case static_cast<unsigned char>('C'):
		case static_cast<unsigned char>('D'):
		case static_cast<unsigned char>('E'):
		case static_cast<unsigned char>('F'):
		case static_cast<unsigned char>('G'):
		case static_cast<unsigned char>('H'):
		case static_cast<unsigned char>('I'):
		case static_cast<unsigned char>('J'):
		case static_cast<unsigned char>('K'):
		case static_cast<unsigned char>('L'):
		case static_cast<unsigned char>('M'):
		case static_cast<unsigned char>('N'):
		case static_cast<unsigned char>('O'):
		case static_cast<unsigned char>('P'):
		case static_cast<unsigned char>('Q'):
		case static_cast<unsigned char>('R'):
		case static_cast<unsigned char>('S'):
		case static_cast<unsigned char>('T'):
		case static_cast<unsigned char>('U'):
		case static_cast<unsigned char>('V'):
		case static_cast<unsigned char>('W'):
		case static_cast<unsigned char>('X'):
		case static_cast<unsigned char>('Y'):
		case static_cast<unsigned char>('Z'):
		{
			matchRange(static_cast<unsigned char>('A'),static_cast<unsigned char>('Z'));
			break;
		}
		case static_cast<unsigned char>('0'):
		case static_cast<unsigned char>('1'):
		case static_cast<unsigned char>('2'):
		case static_cast<unsigned char>('3'):
		case static_cast<unsigned char>('4'):
		case static_cast<unsigned char>('5'):
		case static_cast<unsigned char>('6'):
		case static_cast<unsigned char>('7'):
		case static_cast<unsigned char>('8'):
		case static_cast<unsigned char>('9'):
		{
			matchRange(static_cast<unsigned char>('0'),static_cast<unsigned char>('9'));
			break;
		}
		default:
		{
			goto _loop493;
		}
		}
	} while (true);
	_loop493:;
	}
	_ttype = testLiteralsTable(_ttype);
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}

void GrpLexer::mAT_IDENT(bool _createToken) {
	int _ttype; RefToken _token; int _begin=text.length();
	_ttype = AT_IDENT;
	int _saveIndex;

	{
	if ((LA(1)==static_cast<unsigned char>('@')) && (LA(2)==static_cast<unsigned char>(':'))) {
		match("@:");
	}
	else if ((LA(1)==static_cast<unsigned char>('@'))) {
		match(static_cast<unsigned char>('@'));
	}
	else {
		throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
	}

	}
	{
	if ((_tokenSet_0.member(LA(1)))) {
		{
		switch ( LA(1)) {
		case static_cast<unsigned char>('a'):
		case static_cast<unsigned char>('b'):
		case static_cast<unsigned char>('c'):
		case static_cast<unsigned char>('d'):
		case static_cast<unsigned char>('e'):
		case static_cast<unsigned char>('f'):
		case static_cast<unsigned char>('g'):
		case static_cast<unsigned char>('h'):
		case static_cast<unsigned char>('i'):
		case static_cast<unsigned char>('j'):
		case static_cast<unsigned char>('k'):
		case static_cast<unsigned char>('l'):
		case static_cast<unsigned char>('m'):
		case static_cast<unsigned char>('n'):
		case static_cast<unsigned char>('o'):
		case static_cast<unsigned char>('p'):
		case static_cast<unsigned char>('q'):
		case static_cast<unsigned char>('r'):
		case static_cast<unsigned char>('s'):
		case static_cast<unsigned char>('t'):
		case static_cast<unsigned char>('u'):
		case static_cast<unsigned char>('v'):
		case static_cast<unsigned char>('w'):
		case static_cast<unsigned char>('x'):
		case static_cast<unsigned char>('y'):
		case static_cast<unsigned char>('z'):
		{
			matchRange(static_cast<unsigned char>('a'),static_cast<unsigned char>('z'));
			break;
		}
		case static_cast<unsigned char>('A'):
		case static_cast<unsigned char>('B'):
		case static_cast<unsigned char>('C'):
		case static_cast<unsigned char>('D'):
		case static_cast<unsigned char>('E'):
		case static_cast<unsigned char>('F'):
		case static_cast<unsigned char>('G'):
		case static_cast<unsigned char>('H'):
		case static_cast<unsigned char>('I'):
		case static_cast<unsigned char>('J'):
		case static_cast<unsigned char>('K'):
		case static_cast<unsigned char>('L'):
		case static_cast<unsigned char>('M'):
		case static_cast<unsigned char>('N'):
		case static_cast<unsigned char>('O'):
		case static_cast<unsigned char>('P'):
		case static_cast<unsigned char>('Q'):
		case static_cast<unsigned char>('R'):
		case static_cast<unsigned char>('S'):
		case static_cast<unsigned char>('T'):
		case static_cast<unsigned char>('U'):
		case static_cast<unsigned char>('V'):
		case static_cast<unsigned char>('W'):
		case static_cast<unsigned char>('X'):
		case static_cast<unsigned char>('Y'):
		case static_cast<unsigned char>('Z'):
		{
			matchRange(static_cast<unsigned char>('A'),static_cast<unsigned char>('Z'));
			break;
		}
		default:
		{
			throw ScannerException(std::string("no viable alt for char: ")+charName(LA(1)),getLine());
		}
		}
		}
		{
		do {
			switch ( LA(1)) {
			case static_cast<unsigned char>('_'):
			{
				match(static_cast<unsigned char>('_'));
				break;
			}
			case static_cast<unsigned char>('a'):
			case static_cast<unsigned char>('b'):
			case static_cast<unsigned char>('c'):
			case static_cast<unsigned char>('d'):
			case static_cast<unsigned char>('e'):
			case static_cast<unsigned char>('f'):
			case static_cast<unsigned char>('g'):
			case static_cast<unsigned char>('h'):
			case static_cast<unsigned char>('i'):
			case static_cast<unsigned char>('j'):
			case static_cast<unsigned char>('k'):
			case static_cast<unsigned char>('l'):
			case static_cast<unsigned char>('m'):
			case static_cast<unsigned char>('n'):
			case static_cast<unsigned char>('o'):
			case static_cast<unsigned char>('p'):
			case static_cast<unsigned char>('q'):
			case static_cast<unsigned char>('r'):
			case static_cast<unsigned char>('s'):
			case static_cast<unsigned char>('t'):
			case static_cast<unsigned char>('u'):
			case static_cast<unsigned char>('v'):
			case static_cast<unsigned char>('w'):
			case static_cast<unsigned char>('x'):
			case static_cast<unsigned char>('y'):
			case static_cast<unsigned char>('z'):
			{
				matchRange(static_cast<unsigned char>('a'),static_cast<unsigned char>('z'));
				break;
			}
			case static_cast<unsigned char>('A'):
			case static_cast<unsigned char>('B'):
			case static_cast<unsigned char>('C'):
			case static_cast<unsigned char>('D'):
			case static_cast<unsigned char>('E'):
			case static_cast<unsigned char>('F'):
			case static_cast<unsigned char>('G'):
			case static_cast<unsigned char>('H'):
			case static_cast<unsigned char>('I'):
			case static_cast<unsigned char>('J'):
			case static_cast<unsigned char>('K'):
			case static_cast<unsigned char>('L'):
			case static_cast<unsigned char>('M'):
			case static_cast<unsigned char>('N'):
			case static_cast<unsigned char>('O'):
			case static_cast<unsigned char>('P'):
			case static_cast<unsigned char>('Q'):
			case static_cast<unsigned char>('R'):
			case static_cast<unsigned char>('S'):
			case static_cast<unsigned char>('T'):
			case static_cast<unsigned char>('U'):
			case static_cast<unsigned char>('V'):
			case static_cast<unsigned char>('W'):
			case static_cast<unsigned char>('X'):
			case static_cast<unsigned char>('Y'):
			case static_cast<unsigned char>('Z'):
			{
				matchRange(static_cast<unsigned char>('A'),static_cast<unsigned char>('Z'));
				break;
			}
			case static_cast<unsigned char>('0'):
			case static_cast<unsigned char>('1'):
			case static_cast<unsigned char>('2'):
			case static_cast<unsigned char>('3'):
			case static_cast<unsigned char>('4'):
			case static_cast<unsigned char>('5'):
			case static_cast<unsigned char>('6'):
			case static_cast<unsigned char>('7'):
			case static_cast<unsigned char>('8'):
			case static_cast<unsigned char>('9'):
			{
				matchRange(static_cast<unsigned char>('0'),static_cast<unsigned char>('9'));
				break;
			}
			default:
			{
				goto _loop499;
			}
			}
		} while (true);
		_loop499:;
		}
	}
	else {
		{
		do {
			if (((LA(1) >= static_cast<unsigned char>('0') && LA(1) <= static_cast<unsigned char>('9')))) {
				matchRange(static_cast<unsigned char>('0'),static_cast<unsigned char>('9'));
			}
			else {
				goto _loop501;
			}

		} while (true);
		_loop501:;
		}
	}

	}
	if ( _createToken && _token==nullToken && _ttype!=Token::SKIP ) {
	   _token = makeToken(_ttype);
	   _token->setText(text.substr(_begin, text.length()-_begin));
	}
	_returnToken = _token;
}


const unsigned long GrpLexer::_tokenSet_0_data_[] = { 0UL, 0UL, 134217726UL, 134217726UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpLexer::_tokenSet_0(_tokenSet_0_data_,10);
const unsigned long GrpLexer::_tokenSet_1_data_[] = { 4294958079UL, 4294967295UL, 4294967295UL, 4294967295UL, 4294967295UL, 4294967295UL, 4294967295UL, 4294967295UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpLexer::_tokenSet_1(_tokenSet_1_data_,16);
const unsigned long GrpLexer::_tokenSet_2_data_[] = { 4294966271UL, 4294966271UL, 4294967295UL, 4294967295UL, 4294967295UL, 4294967295UL, 4294967295UL, 4294967295UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpLexer::_tokenSet_2(_tokenSet_2_data_,16);
const unsigned long GrpLexer::_tokenSet_3_data_[] = { 0UL, 67043328UL, 126UL, 126UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpLexer::_tokenSet_3(_tokenSet_3_data_,10);
const unsigned long GrpLexer::_tokenSet_4_data_[] = { 0UL, 132UL, 268435456UL, 1327172UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpLexer::_tokenSet_4(_tokenSet_4_data_,10);
const unsigned long GrpLexer::_tokenSet_5_data_[] = { 4294967295UL, 4294967167UL, 4294967295UL, 4294967295UL, 4294574079UL, 4294967295UL, 4294967295UL, 4294967295UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpLexer::_tokenSet_5(_tokenSet_5_data_,16);
const unsigned long GrpLexer::_tokenSet_6_data_[] = { 4294967295UL, 4294967291UL, 4294967295UL, 4294967295UL, 4293394431UL, 4294967295UL, 4294967295UL, 4294967295UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL, 0UL };
const BitSet GrpLexer::_tokenSet_6(_tokenSet_6_data_,16);
