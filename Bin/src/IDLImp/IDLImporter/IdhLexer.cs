// $ANTLR 2.7.7 (20060930): "idh.g" -> "IdhLexer.cs"$

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

namespace SIL.FieldWorks.Tools
{
	// Generate header specific to lexer CSharp file
	using System;
	using Stream                          = System.IO.Stream;
	using TextReader                      = System.IO.TextReader;
	using Hashtable                       = System.Collections.Hashtable;
	using Comparer                        = System.Collections.Comparer;

	using TokenStreamException            = antlr.TokenStreamException;
	using TokenStreamIOException          = antlr.TokenStreamIOException;
	using TokenStreamRecognitionException = antlr.TokenStreamRecognitionException;
	using CharStreamException             = antlr.CharStreamException;
	using CharStreamIOException           = antlr.CharStreamIOException;
	using ANTLRException                  = antlr.ANTLRException;
	using CharScanner                     = antlr.CharScanner;
	using InputBuffer                     = antlr.InputBuffer;
	using ByteBuffer                      = antlr.ByteBuffer;
	using CharBuffer                      = antlr.CharBuffer;
	using Token                           = antlr.Token;
	using IToken                          = antlr.IToken;
	using CommonToken                     = antlr.CommonToken;
	using SemanticException               = antlr.SemanticException;
	using RecognitionException            = antlr.RecognitionException;
	using NoViableAltForCharException     = antlr.NoViableAltForCharException;
	using MismatchedCharException         = antlr.MismatchedCharException;
	using TokenStream                     = antlr.TokenStream;
	using LexerSharedInputState           = antlr.LexerSharedInputState;
	using BitSet                          = antlr.collections.impl.BitSet;

	public 	class IdhLexer : antlr.CharScanner	, TokenStream
	 {
		public const int EOF = 1;
		public const int NULL_TREE_LOOKAHEAD = 3;
		public const int COMMENT = 4;
		public const int LITERAL_typedef = 5;
		public const int PREPROCESSOR = 6;
		public const int IDENTIFIER = 7;
		public const int LITERAL_DeclareInterface = 8;
		public const int LPAREN = 9;
		public const int COMMA = 10;
		public const int RPAREN = 11;
		public const int LBRACE = 12;
		public const int ATTRIBUTE = 13;
		public const int RBRACE = 14;
		public const int SEMICOLON = 15;
		public const int LITERAL_DeclareCoClass = 16;
		public const int LITERAL_DeclareDualInterface = 17;
		// "DeclareDualInterface2" = 18
		public const int LITERAL_interface = 19;
		public const int LITERAL_enum = 20;
		public const int LITERAL_struct = 21;
		public const int EQUAL = 22;
		public const int BAR = 23;
		public const int PLUS = 24;
		public const int LITERAL_const = 25;
		public const int STAR = 26;
		public const int LBRACKET = 27;
		public const int RBRACKET = 28;
		public const int MINUS = 29;
		public const int LITERAL_import = 30;
		public const int STRING_LITERAL = 31;
		public const int ATTRVAL = 32;
		public const int VARIABLE = 33;
		public const int EXPRESSION = 34;
		public const int ATTRLIST = 35;
		public const int ESC = 36;
		public const int OCTDIGIT = 37;
		public const int DIGIT = 38;
		public const int HEXDIGIT = 39;
		public const int LETTER = 40;
		public const int WS = 41;
		public const int IGNORE = 42;

		public IdhLexer(Stream ins) : this(new ByteBuffer(ins))
		{
		}

		public IdhLexer(TextReader r) : this(new CharBuffer(r))
		{
		}

		public IdhLexer(InputBuffer ib)		 : this(new LexerSharedInputState(ib))
		{
		}

		public IdhLexer(LexerSharedInputState state) : base(state)
		{
			initialize();
		}
		private void initialize()
		{
			caseSensitiveLiterals = true;
			setCaseSensitive(true);
			literals = new Hashtable(100, (float) 0.4, null, Comparer.Default);
			literals.Add("const", 25);
			literals.Add("enum", 20);
			literals.Add("import", 30);
			literals.Add("DeclareInterface", 8);
			literals.Add("DeclareCoClass", 16);
			literals.Add("typedef", 5);
			literals.Add("DeclareDualInterface2", 18);
			literals.Add("interface", 19);
			literals.Add("DeclareDualInterface", 17);
			literals.Add("struct", 21);
		}

		override public IToken nextToken()			//throws TokenStreamException
		{
			IToken theRetToken = null;
tryAgain:
			for (;;)
			{
				IToken _token = null;
				int _ttype = Token.INVALID_TYPE;
				setCommitToPath(false);
				int _m;
				_m = mark();
				resetText();
				try     // for char stream error handling
				{
					try     // for lexical error handling
					{
						switch ( cached_LA1 )
						{
						case '/':
						{
							mCOMMENT(true);
							theRetToken = returnToken_;
							break;
						}
						case ',':
						{
							mCOMMA(true);
							theRetToken = returnToken_;
							break;
						}
						case '0':  case '1':  case '2':  case '3':
						case '4':  case '5':  case '6':  case '7':
						case '8':  case '9':  case 'A':  case 'B':
						case 'C':  case 'D':  case 'E':  case 'F':
						case 'G':  case 'H':  case 'I':  case 'J':
						case 'K':  case 'L':  case 'M':  case 'N':
						case 'O':  case 'P':  case 'Q':  case 'R':
						case 'S':  case 'T':  case 'U':  case 'V':
						case 'W':  case 'X':  case 'Y':  case 'Z':
						case '_':  case 'a':  case 'b':  case 'c':
						case 'd':  case 'e':  case 'f':  case 'g':
						case 'h':  case 'i':  case 'j':  case 'k':
						case 'l':  case 'm':  case 'n':  case 'o':
						case 'p':  case 'q':  case 'r':  case 's':
						case 't':  case 'u':  case 'v':  case 'w':
						case 'x':  case 'y':  case 'z':
						{
							mIDENTIFIER(true);
							theRetToken = returnToken_;
							break;
						}
						case '(':
						{
							mLPAREN(true);
							theRetToken = returnToken_;
							break;
						}
						case ')':
						{
							mRPAREN(true);
							theRetToken = returnToken_;
							break;
						}
						case '*':
						{
							mSTAR(true);
							theRetToken = returnToken_;
							break;
						}
						case '-':
						{
							mMINUS(true);
							theRetToken = returnToken_;
							break;
						}
						case '#':
						{
							mPREPROCESSOR(true);
							theRetToken = returnToken_;
							break;
						}
						case '"':
						{
							mSTRING_LITERAL(true);
							theRetToken = returnToken_;
							break;
						}
						case '{':
						{
							mLBRACE(true);
							theRetToken = returnToken_;
							break;
						}
						case '}':
						{
							mRBRACE(true);
							theRetToken = returnToken_;
							break;
						}
						case ']':
						{
							mRBRACKET(true);
							theRetToken = returnToken_;
							break;
						}
						case ';':
						{
							mSEMICOLON(true);
							theRetToken = returnToken_;
							break;
						}
						case '=':
						{
							mEQUAL(true);
							theRetToken = returnToken_;
							break;
						}
						case '+':
						{
							mPLUS(true);
							theRetToken = returnToken_;
							break;
						}
						case '|':
						{
							mBAR(true);
							theRetToken = returnToken_;
							break;
						}
						default:
							if ((cached_LA1=='[') && (tokenSet_0_.member(cached_LA2)))
							{
								mATTRIBUTE(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='[') && (true)) {
								mLBRACKET(true);
								theRetToken = returnToken_;
							}
						else
						{
							if (cached_LA1==EOF_CHAR) { uponEOF(); returnToken_ = makeToken(Token.EOF_TYPE); }
									else
					{
					commit();
					try {mIGNORE(false);}
					catch(RecognitionException e)
					{
						// catastrophic failure
						reportError(e);
						consume();
					}
					goto tryAgain;
				}
						}
						break; }
						commit();
						if ( null==returnToken_ ) goto tryAgain; // found SKIP token
						_ttype = returnToken_.Type;
						_ttype = testLiteralsTable(_ttype);
						returnToken_.Type = _ttype;
						return returnToken_;
					}
					catch (RecognitionException e) {
						if (!getCommitToPath())
						{
							rewind(_m);
							resetText();
							try {mIGNORE(false);}
							catch(RecognitionException ee) {
								// horrendous failure: error in filter rule
								reportError(ee);
								consume();
							}
						}
						else
							throw new TokenStreamRecognitionException(e);
					}
				}
				catch (CharStreamException cse) {
					if ( cse is CharStreamIOException ) {
						throw new TokenStreamIOException(((CharStreamIOException)cse).io);
					}
					else {
						throw new TokenStreamException(cse.Message);
					}
				}
			}
		}

	public void mCOMMENT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = COMMENT;

		if ((cached_LA1=='/') && (cached_LA2=='*'))
		{
			match("/*");
			{    // ( ... )*
				for (;;)
				{
					// nongreedy exit test
					if ((cached_LA1=='*') && (cached_LA2=='/')) goto _loop94_breakloop;
					if (((cached_LA1 >= '\u0003' && cached_LA1 <= '\u00ff')) && ((cached_LA2 >= '\u0003' && cached_LA2 <= '\u00ff')))
					{
						matchNot(EOF/*_CHAR*/);
						if (0==inputState.guessing)
						{
							if (cached_LA1 == '\n') newline();
						}
					}
					else
					{
						goto _loop94_breakloop;
					}

				}
_loop94_breakloop:				;
			}    // ( ... )*
			match("*/");
		}
		else if ((cached_LA1=='/') && (cached_LA2=='/')) {
			{ // ( ... )+
				int _cnt99=0;
				for (;;)
				{
					if ((cached_LA1=='/'))
					{
						match("//");
						{    // ( ... )*
							for (;;)
							{
								if ((tokenSet_1_.member(cached_LA1)))
								{
									{
										match(tokenSet_1_);
									}
								}
								else
								{
									goto _loop98_breakloop;
								}

							}
_loop98_breakloop:							;
						}    // ( ... )*
						match('\n');
						if (0==inputState.guessing)
						{
							newline();
						}
					}
					else
					{
						if (_cnt99 >= 1) { goto _loop99_breakloop; } else { throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());; }
					}

					_cnt99++;
				}
_loop99_breakloop:				;
			}    // ( ... )+
		}
		else
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}

		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mATTRIBUTE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = ATTRIBUTE;

		match('[');
		{    // ( ... )*
			for (;;)
			{
				if ((tokenSet_2_.member(cached_LA1)))
				{
					mWS(false);
				}
				else
				{
					goto _loop102_breakloop;
				}

			}
_loop102_breakloop:			;
		}    // ( ... )*
		mATTRVAL(false);
		{    // ( ... )*
			for (;;)
			{
				if ((tokenSet_2_.member(cached_LA1)))
				{
					mWS(false);
				}
				else
				{
					goto _loop104_breakloop;
				}

			}
_loop104_breakloop:			;
		}    // ( ... )*
		{    // ( ... )*
			for (;;)
			{
				if ((cached_LA1==','))
				{
					mCOMMA(false);
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_2_.member(cached_LA1)))
							{
								mWS(false);
							}
							else
							{
								goto _loop107_breakloop;
							}

						}
_loop107_breakloop:						;
					}    // ( ... )*
					mATTRVAL(false);
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_2_.member(cached_LA1)))
							{
								mWS(false);
							}
							else
							{
								goto _loop109_breakloop;
							}

						}
_loop109_breakloop:						;
					}    // ( ... )*
				}
				else
				{
					goto _loop110_breakloop;
				}

			}
_loop110_breakloop:			;
		}    // ( ... )*
		match(']');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mWS(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = WS;

		switch ( cached_LA1 )
		{
		case '\n':
		{
			match('\n');
			if (0==inputState.guessing)
			{
				newline();
			}
			break;
		}
		case '\r':
		{
			match('\r');
			match('\n');
			if (0==inputState.guessing)
			{
				newline();
			}
			break;
		}
		case ' ':
		{
			match(' ');
			break;
		}
		case '\t':
		{
			match('\t');
			if (0==inputState.guessing)
			{
				_ttype = Token.SKIP;
			}
			break;
		}
		default:
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}
		 }
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mATTRVAL(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = ATTRVAL;

		bool synPredMatched115 = false;
		if (((tokenSet_3_.member(cached_LA1)) && (tokenSet_4_.member(cached_LA2))))
		{
			int _m115 = mark();
			synPredMatched115 = true;
			inputState.guessing++;
			try {
				{
					mIDENTIFIER(false);
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_2_.member(cached_LA1)))
							{
								mWS(false);
							}
							else
							{
								goto _loop114_breakloop;
							}

						}
_loop114_breakloop:						;
					}    // ( ... )*
					mLPAREN(false);
				}
			}
			catch (RecognitionException)
			{
				synPredMatched115 = false;
			}
			rewind(_m115);
			inputState.guessing--;
		}
		if ( synPredMatched115 )
		{
			mIDENTIFIER(false);
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_2_.member(cached_LA1)))
					{
						mWS(false);
					}
					else
					{
						goto _loop117_breakloop;
					}

				}
_loop117_breakloop:				;
			}    // ( ... )*
			mLPAREN(false);
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_2_.member(cached_LA1)))
					{
						mWS(false);
					}
					else
					{
						goto _loop119_breakloop;
					}

				}
_loop119_breakloop:				;
			}    // ( ... )*
			{
				switch ( cached_LA1 )
				{
				case '*':  case '-':  case '0':  case '1':
				case '2':  case '3':  case '4':  case '5':
				case '6':  case '7':  case '8':  case '9':
				case 'A':  case 'B':  case 'C':  case 'D':
				case 'E':  case 'F':  case 'G':  case 'H':
				case 'I':  case 'J':  case 'K':  case 'L':
				case 'M':  case 'N':  case 'O':  case 'P':
				case 'Q':  case 'R':  case 'S':  case 'T':
				case 'U':  case 'V':  case 'W':  case 'X':
				case 'Y':  case 'Z':  case '_':  case 'a':
				case 'b':  case 'c':  case 'd':  case 'e':
				case 'f':  case 'g':  case 'h':  case 'i':
				case 'j':  case 'k':  case 'l':  case 'm':
				case 'n':  case 'o':  case 'p':  case 'q':
				case 'r':  case 's':  case 't':  case 'u':
				case 'v':  case 'w':  case 'x':  case 'y':
				case 'z':
				{
					mEXPRESSION(false);
					break;
				}
				case '"':
				{
					mATTRLIST(false);
					break;
				}
				default:
				{
					throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
				}
				 }
			}
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_2_.member(cached_LA1)))
					{
						mWS(false);
					}
					else
					{
						goto _loop122_breakloop;
					}

				}
_loop122_breakloop:				;
			}    // ( ... )*
			mRPAREN(false);
		}
		else if ((tokenSet_3_.member(cached_LA1)) && (tokenSet_5_.member(cached_LA2))) {
			mIDENTIFIER(false);
		}
		else
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}

		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mCOMMA(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = COMMA;

		match(',');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mIDENTIFIER(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = IDENTIFIER;

		bool synPredMatched171 = false;
		if (((cached_LA1=='c') && (cached_LA2=='p')))
		{
			int _m171 = mark();
			synPredMatched171 = true;
			inputState.guessing++;
			try {
				{
					match("cpp_quote");
					mLPAREN(false);
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_1_.member(cached_LA1)))
							{
								matchNot('\n');
							}
							else
							{
								goto _loop170_breakloop;
							}

						}
_loop170_breakloop:						;
					}    // ( ... )*
					match('\n');
				}
			}
			catch (RecognitionException)
			{
				synPredMatched171 = false;
			}
			rewind(_m171);
			inputState.guessing--;
		}
		if ( synPredMatched171 )
		{
			match("cpp_quote");
			mLPAREN(false);
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_1_.member(cached_LA1)))
					{
						matchNot('\n');
					}
					else
					{
						goto _loop173_breakloop;
					}

				}
_loop173_breakloop:				;
			}    // ( ... )*
			match('\n');
			if (0==inputState.guessing)
			{
				newline();
			}
		}
		else if ((tokenSet_3_.member(cached_LA1)) && (true)) {
			{ // ( ... )+
				int _cnt175=0;
				for (;;)
				{
					switch ( cached_LA1 )
					{
					case 'A':  case 'B':  case 'C':  case 'D':
					case 'E':  case 'F':  case 'G':  case 'H':
					case 'I':  case 'J':  case 'K':  case 'L':
					case 'M':  case 'N':  case 'O':  case 'P':
					case 'Q':  case 'R':  case 'S':  case 'T':
					case 'U':  case 'V':  case 'W':  case 'X':
					case 'Y':  case 'Z':  case '_':  case 'a':
					case 'b':  case 'c':  case 'd':  case 'e':
					case 'f':  case 'g':  case 'h':  case 'i':
					case 'j':  case 'k':  case 'l':  case 'm':
					case 'n':  case 'o':  case 'p':  case 'q':
					case 'r':  case 's':  case 't':  case 'u':
					case 'v':  case 'w':  case 'x':  case 'y':
					case 'z':
					{
						mLETTER(false);
						break;
					}
					case '0':  case '1':  case '2':  case '3':
					case '4':  case '5':  case '6':  case '7':
					case '8':  case '9':
					{
						mDIGIT(false);
						break;
					}
					default:
					{
						if (_cnt175 >= 1) { goto _loop175_breakloop; } else { throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());; }
					}
					break; }
					_cnt175++;
				}
_loop175_breakloop:				;
			}    // ( ... )+
		}
		else
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}

		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mLPAREN(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = LPAREN;

		match('(');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mEXPRESSION(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = EXPRESSION;

		bool synPredMatched133 = false;
		if (((tokenSet_6_.member(cached_LA1)) && (tokenSet_7_.member(cached_LA2))))
		{
			int _m133 = mark();
			synPredMatched133 = true;
			inputState.guessing++;
			try {
				{
					mVARIABLE(false);
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_2_.member(cached_LA1)))
							{
								mWS(false);
							}
							else
							{
								goto _loop131_breakloop;
							}

						}
_loop131_breakloop:						;
					}    // ( ... )*
					{
						switch ( cached_LA1 )
						{
						case '-':
						{
							match('-');
							break;
						}
						case '+':
						{
							match('+');
							break;
						}
						case '*':
						{
							match('*');
							break;
						}
						case '/':
						{
							match('/');
							break;
						}
						default:
						{
							throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
						}
						 }
					}
				}
			}
			catch (RecognitionException)
			{
				synPredMatched133 = false;
			}
			rewind(_m133);
			inputState.guessing--;
		}
		if ( synPredMatched133 )
		{
			mVARIABLE(false);
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_2_.member(cached_LA1)))
					{
						mWS(false);
					}
					else
					{
						goto _loop135_breakloop;
					}

				}
_loop135_breakloop:				;
			}    // ( ... )*
			{
				switch ( cached_LA1 )
				{
				case '-':
				{
					match('-');
					break;
				}
				case '+':
				{
					match('+');
					break;
				}
				case '*':
				{
					match('*');
					break;
				}
				case '/':
				{
					match('/');
					break;
				}
				default:
				{
					throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
				}
				 }
			}
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_2_.member(cached_LA1)))
					{
						mWS(false);
					}
					else
					{
						goto _loop138_breakloop;
					}

				}
_loop138_breakloop:				;
			}    // ( ... )*
			mVARIABLE(false);
		}
		else if ((tokenSet_6_.member(cached_LA1)) && (tokenSet_8_.member(cached_LA2))) {
			mVARIABLE(false);
		}
		else if ((cached_LA1=='-')) {
			mMINUS(false);
			{ // ( ... )+
				int _cnt140=0;
				for (;;)
				{
					if (((cached_LA1 >= '0' && cached_LA1 <= '9')))
					{
						mDIGIT(false);
					}
					else
					{
						if (_cnt140 >= 1) { goto _loop140_breakloop; } else { throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());; }
					}

					_cnt140++;
				}
_loop140_breakloop:				;
			}    // ( ... )+
		}
		else
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}

		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mATTRLIST(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = ATTRLIST;

		match('"');
		{    // ( ... )*
			for (;;)
			{
				if ((tokenSet_9_.member(cached_LA1)))
				{
					matchNot('"');
				}
				else
				{
					goto _loop143_breakloop;
				}

			}
_loop143_breakloop:			;
		}    // ( ... )*
		match('"');
		{    // ( ... )*
			for (;;)
			{
				if ((cached_LA1==','))
				{
					mCOMMA(false);
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_2_.member(cached_LA1)))
							{
								mWS(false);
							}
							else
							{
								goto _loop146_breakloop;
							}

						}
_loop146_breakloop:						;
					}    // ( ... )*
					match('"');
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_9_.member(cached_LA1)))
							{
								matchNot('"');
							}
							else
							{
								goto _loop148_breakloop;
							}

						}
_loop148_breakloop:						;
					}    // ( ... )*
					match('"');
				}
				else
				{
					goto _loop149_breakloop;
				}

			}
_loop149_breakloop:			;
		}    // ( ... )*
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mRPAREN(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = RPAREN;

		match(')');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mVARIABLE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = VARIABLE;

		{    // ( ... )*
			for (;;)
			{
				if ((cached_LA1=='*'))
				{
					mSTAR(false);
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_2_.member(cached_LA1)))
							{
								mWS(false);
							}
							else
							{
								goto _loop126_breakloop;
							}

						}
_loop126_breakloop:						;
					}    // ( ... )*
				}
				else
				{
					goto _loop127_breakloop;
				}

			}
_loop127_breakloop:			;
		}    // ( ... )*
		mIDENTIFIER(false);
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mSTAR(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = STAR;

		match('*');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mMINUS(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = MINUS;

		match('-');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mDIGIT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = DIGIT;

		matchRange('0','9');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mPREPROCESSOR(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = PREPROCESSOR;

		bool synPredMatched153 = false;
		if (((cached_LA1=='#') && (cached_LA2=='e')))
		{
			int _m153 = mark();
			synPredMatched153 = true;
			inputState.guessing++;
			try {
				{
					match("#endif");
					{
						switch ( cached_LA1 )
						{
						case '\r':
						{
							match('\r');
							break;
						}
						case '\n':
						{
							match('\n');
							break;
						}
						default:
							{
							}
						break; }
					}
				}
			}
			catch (RecognitionException)
			{
				synPredMatched153 = false;
			}
			rewind(_m153);
			inputState.guessing--;
		}
		if ( synPredMatched153 )
		{
			match("#endif");
		}
		else if ((cached_LA1=='#') && (tokenSet_3_.member(cached_LA2))) {
			match('#');
			mIDENTIFIER(false);
			mWS(false);
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_1_.member(cached_LA1)))
					{
						{
							matchNot('\n');
						}
					}
					else
					{
						goto _loop156_breakloop;
					}

				}
_loop156_breakloop:				;
			}    // ( ... )*
			match('\n');
			if (0==inputState.guessing)
			{
				newline();
			}
		}
		else
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}

		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mSTRING_LITERAL(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = STRING_LITERAL;

		int _saveIndex = 0;
		_saveIndex = text.Length;
		match('"');
		text.Length = _saveIndex;
		{    // ( ... )*
			for (;;)
			{
				if ((cached_LA1=='\\'))
				{
					mESC(false);
				}
				else if ((tokenSet_10_.member(cached_LA1))) {
					matchNot('"');
				}
				else
				{
					goto _loop159_breakloop;
				}

			}
_loop159_breakloop:			;
		}    // ( ... )*
		_saveIndex = text.Length;
		match('"');
		text.Length = _saveIndex;
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mESC(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = ESC;

		match('\\');
		{
			switch ( cached_LA1 )
			{
			case 'n':
			{
				match('n');
				break;
			}
			case 't':
			{
				match('t');
				break;
			}
			case 'v':
			{
				match('v');
				break;
			}
			case 'b':
			{
				match('b');
				break;
			}
			case 'r':
			{
				match('r');
				break;
			}
			case 'f':
			{
				match('f');
				break;
			}
			case 'a':
			{
				match('a');
				break;
			}
			case '\\':
			{
				match('\\');
				break;
			}
			case '?':
			{
				match('?');
				break;
			}
			case '\'':
			{
				match('\'');
				break;
			}
			case '"':
			{
				match('"');
				break;
			}
			case '0':  case '1':  case '2':  case '3':
			{
				{
					switch ( cached_LA1 )
					{
					case '0':
					{
						match('0');
						break;
					}
					case '1':
					{
						match('1');
						break;
					}
					case '2':
					{
						match('2');
						break;
					}
					case '3':
					{
						match('3');
						break;
					}
					default:
					{
						throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
					}
					 }
				}
				{
					if (((cached_LA1 >= '0' && cached_LA1 <= '7')) && ((cached_LA2 >= '\u0003' && cached_LA2 <= '\u00ff')))
					{
						mOCTDIGIT(false);
						{
							if (((cached_LA1 >= '0' && cached_LA1 <= '7')) && ((cached_LA2 >= '\u0003' && cached_LA2 <= '\u00ff')))
							{
								mOCTDIGIT(false);
							}
							else if (((cached_LA1 >= '\u0003' && cached_LA1 <= '\u00ff')) && (true)) {
							}
							else
							{
								throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
							}

						}
					}
					else if (((cached_LA1 >= '\u0003' && cached_LA1 <= '\u00ff')) && (true)) {
					}
					else
					{
						throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
					}

				}
				break;
			}
			case 'x':
			{
				match('x');
				mHEXDIGIT(false);
				{
					if ((tokenSet_11_.member(cached_LA1)) && ((cached_LA2 >= '\u0003' && cached_LA2 <= '\u00ff')))
					{
						mHEXDIGIT(false);
					}
					else if (((cached_LA1 >= '\u0003' && cached_LA1 <= '\u00ff')) && (true)) {
					}
					else
					{
						throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
					}

				}
				break;
			}
			default:
			{
				throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
			}
			 }
		}
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mOCTDIGIT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = OCTDIGIT;

		matchRange('0','7');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mHEXDIGIT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = HEXDIGIT;

		switch ( cached_LA1 )
		{
		case '0':  case '1':  case '2':  case '3':
		case '4':  case '5':  case '6':  case '7':
		case '8':  case '9':
		{
			mDIGIT(false);
			break;
		}
		case 'A':  case 'B':  case 'C':  case 'D':
		case 'E':  case 'F':
		{
			matchRange('A','F');
			break;
		}
		case 'a':  case 'b':  case 'c':  case 'd':
		case 'e':  case 'f':
		{
			matchRange('a','f');
			break;
		}
		default:
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}
		 }
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mLETTER(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = LETTER;

		switch ( cached_LA1 )
		{
		case 'A':  case 'B':  case 'C':  case 'D':
		case 'E':  case 'F':  case 'G':  case 'H':
		case 'I':  case 'J':  case 'K':  case 'L':
		case 'M':  case 'N':  case 'O':  case 'P':
		case 'Q':  case 'R':  case 'S':  case 'T':
		case 'U':  case 'V':  case 'W':  case 'X':
		case 'Y':  case 'Z':
		{
			matchRange('A','Z');
			break;
		}
		case 'a':  case 'b':  case 'c':  case 'd':
		case 'e':  case 'f':  case 'g':  case 'h':
		case 'i':  case 'j':  case 'k':  case 'l':
		case 'm':  case 'n':  case 'o':  case 'p':
		case 'q':  case 'r':  case 's':  case 't':
		case 'u':  case 'v':  case 'w':  case 'x':
		case 'y':  case 'z':
		{
			matchRange('a','z');
			break;
		}
		case '_':
		{
			match('_');
			break;
		}
		default:
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}
		 }
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mLBRACE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = LBRACE;

		match('{');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mRBRACE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = RBRACE;

		match('}');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mLBRACKET(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = LBRACKET;

		match('[');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mRBRACKET(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = RBRACKET;

		match(']');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mSEMICOLON(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = SEMICOLON;

		match(';');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mEQUAL(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = EQUAL;

		match('=');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mPLUS(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = PLUS;

		match('+');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mBAR(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = BAR;

		match('|');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	protected void mIGNORE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = IGNORE;

		switch ( cached_LA1 )
		{
		case '\n':
		{
			match('\n');
			if (0==inputState.guessing)
			{
				newline();
			}
			break;
		}
		case '\r':
		{
			match('\r');
			break;
		}
		case ' ':
		{
			match(' ');
			break;
		}
		case '\t':
		{
			match('\t');
			break;
		}
		default:
		{
			throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
		}
		 }
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}


	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 287948905469978112L, 576460745995190270L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = new long[8];
		data[0]=-1032L;
		for (int i = 1; i<=3; i++) { data[i]=-1L; }
		for (int i = 4; i<=7; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	private static long[] mk_tokenSet_2_()
	{
		long[] data = { 4294977024L, 0L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());
	private static long[] mk_tokenSet_3_()
	{
		long[] data = { 287948901175001088L, 576460745995190270L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_3_ = new BitSet(mk_tokenSet_3_());
	private static long[] mk_tokenSet_4_()
	{
		long[] data = { 287950004981605888L, 576460745995190270L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_4_ = new BitSet(mk_tokenSet_4_());
	private static long[] mk_tokenSet_5_()
	{
		long[] data = { 287966497656022528L, 576460746532061182L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_5_ = new BitSet(mk_tokenSet_5_());
	private static long[] mk_tokenSet_6_()
	{
		long[] data = { 287953299221512192L, 576460745995190270L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_6_ = new BitSet(mk_tokenSet_6_());
	private static long[] mk_tokenSet_7_()
	{
		long[] data = { 288138021469955584L, 576460745995190270L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_7_ = new BitSet(mk_tokenSet_7_());
	private static long[] mk_tokenSet_8_()
	{
		long[] data = { 287955502539744768L, 576460745995190270L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_8_ = new BitSet(mk_tokenSet_8_());
	private static long[] mk_tokenSet_9_()
	{
		long[] data = new long[8];
		data[0]=-17179869192L;
		for (int i = 1; i<=3; i++) { data[i]=-1L; }
		for (int i = 4; i<=7; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_9_ = new BitSet(mk_tokenSet_9_());
	private static long[] mk_tokenSet_10_()
	{
		long[] data = new long[8];
		data[0]=-17179869192L;
		data[1]=-268435457L;
		for (int i = 2; i<=3; i++) { data[i]=-1L; }
		for (int i = 4; i<=7; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_10_ = new BitSet(mk_tokenSet_10_());
	private static long[] mk_tokenSet_11_()
	{
		long[] data = { 287948901175001088L, 541165879422L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_11_ = new BitSet(mk_tokenSet_11_());

}
}
