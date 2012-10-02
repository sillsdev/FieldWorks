#ifndef INC_GrpLexer_hpp_
#define INC_GrpLexer_hpp_

#include "antlr/config.hpp"
/*
 * ANTLR-generated file resulting from grammar c:\fw\src\graphite\grcompiler\grpparser.g
 *
 * Terence Parr, MageLang Institute
 * with John Lilley, Empathy Software
 * ANTLR Version 2.6.0; 1996-1999
 */

#include "antlr/CommonToken.hpp"
#include "antlr/InputBuffer.hpp"
#include "antlr/BitSet.hpp"
#include "antlr/CharScanner.hpp"

//	Header stuff here
void AddGlobalError(bool, int nID, std::string, int nLine);
class GrpTokenStreamFilter;

class GrpLexer : public CharScanner
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

private:
	void initLiterals();
public:
	bool getCaseSensitiveLiterals() const;
public:
	GrpLexer(std::istream& in);
	GrpLexer(InputBuffer& ib);
	GrpLexer(const LexerSharedInputState& state);
	RefToken nextToken();
	public: void mWS(bool _createToken);
	public: void mCOMMENT_SL(bool _createToken);
	public: void mCOMMENT_ML(bool _createToken);
	public: void mLIT_INT(bool _createToken);
	protected: void mDIGIT(bool _createToken);
	protected: void mXDIGIT(bool _createToken);
	public: void mLIT_UHEX(bool _createToken);
	public: void mLIT_CHAR(bool _createToken);
	protected: void mSQUOTE(bool _createToken);
	protected: void mESC(bool _createToken);
	public: void mLIT_STRING(bool _createToken);
	protected: void mDQUOTE(bool _createToken);
	protected: void mODIGIT(bool _createToken);
	public: void mOP_DOT(bool _createToken);
	public: void mOP_DOTDOT(bool _createToken);
	public: void mOP_COLON(bool _createToken);
	public: void mOP_SEMI(bool _createToken);
	public: void mOP_LBRACKET(bool _createToken);
	public: void mOP_RBRACKET(bool _createToken);
	public: void mOP_LPAREN(bool _createToken);
	public: void mOP_RPAREN(bool _createToken);
	public: void mOP_LBRACE(bool _createToken);
	public: void mOP_RBRACE(bool _createToken);
	public: void mOP_NOT(bool _createToken);
	public: void mOP_LT(bool _createToken);
	public: void mOP_LE(bool _createToken);
	public: void mOP_EQ(bool _createToken);
	public: void mOP_EQUALEQUAL(bool _createToken);
	public: void mOP_NE(bool _createToken);
	public: void mOP_GE(bool _createToken);
	public: void mOP_GT(bool _createToken);
	public: void mOP_PLUS(bool _createToken);
	public: void mOP_PLUSEQUAL(bool _createToken);
	public: void mOP_MINUS(bool _createToken);
	public: void mOP_MINUSEQUAL(bool _createToken);
	public: void mOP_MULT(bool _createToken);
	public: void mOP_MULTEQUAL(bool _createToken);
	public: void mOP_DIV(bool _createToken);
	public: void mOP_DIVEQUAL(bool _createToken);
	public: void mOP_COMMA(bool _createToken);
	public: void mOP_DOLLAR(bool _createToken);
	public: void mOP_LINEMARKER(bool _createToken);
	public: void mOP_HASH(bool _createToken);
	public: void mOP_AND(bool _createToken);
	public: void mOP_OR(bool _createToken);
	public: void mOP_BSLASH(bool _createToken);
	public: void mOP_UNDER(bool _createToken);
	public: void mOP_QUESTION(bool _createToken);
	public: void mOP_CARET(bool _createToken);
	public: void mIDENT(bool _createToken);
	public: void mAT_IDENT(bool _createToken);
private:

	static const unsigned long _tokenSet_0_data_[];
	static const BitSet _tokenSet_0;
	static const unsigned long _tokenSet_1_data_[];
	static const BitSet _tokenSet_1;
	static const unsigned long _tokenSet_2_data_[];
	static const BitSet _tokenSet_2;
	static const unsigned long _tokenSet_3_data_[];
	static const BitSet _tokenSet_3;
	static const unsigned long _tokenSet_4_data_[];
	static const BitSet _tokenSet_4;
	static const unsigned long _tokenSet_5_data_[];
	static const BitSet _tokenSet_5;
	static const unsigned long _tokenSet_6_data_[];
	static const BitSet _tokenSet_6;
};

#endif /*INC_GrpLexer_hpp_*/
