// $ANTLR 2.7.7 (20060930): "SurveyorTags.g" -> "SurveyorLexer.cs"$

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
/// File: SurveyorTags.g
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Defines the grammar for processing some Surveyor tags.
/// </remarks>
/// --------------------------------------------------------------------------------------------

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

	public 	class SurveyorLexer : antlr.CharScanner	, TokenStream
	 {
		public const int EOF = 1;
		public const int NULL_TREE_LOOKAHEAD = 3;
		public const int DOLLAR = 4;
		public const int LBRACE = 5;
		public const int IDENTIFIER = 6;
		public const int TABLE = 7;
		public const int RBRACE = 8;
		public const int ROW = 9;
		public const int CELL = 10;
		public const int HTTP = 11;
		public const int REFERENCE = 12;
		public const int POUND = 13;
		public const int DIGIT = 14;
		public const int LETTER = 15;
		public const int WS = 16;
		public const int IGNORE = 17;


		private StringBuilder m_bldr;

		public SurveyorLexer(StringBuilder bldr, TextReader r) : this(r)
		{
			m_bldr = bldr;
		}
		public SurveyorLexer(Stream ins) : this(new ByteBuffer(ins))
		{
		}

		public SurveyorLexer(TextReader r) : this(new CharBuffer(r))
		{
		}

		public SurveyorLexer(InputBuffer ib)		 : this(new LexerSharedInputState(ib))
		{
		}

		public SurveyorLexer(LexerSharedInputState state) : base(state)
		{
			initialize();
		}
		private void initialize()
		{
			caseSensitiveLiterals = true;
			setCaseSensitive(true);
			literals = new Hashtable(100, (float) 0.4, null, Comparer.Default);
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
						case '}':
						{
							mRBRACE(true);
							theRetToken = returnToken_;
							break;
						}
						case '{':
						{
							mLBRACE(true);
							theRetToken = returnToken_;
							break;
						}
						case '#':
						{
							mPOUND(true);
							theRetToken = returnToken_;
							break;
						}
						default:
							if ((cached_LA1=='@') && (cached_LA2=='t'))
							{
								mTABLE(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='@') && (cached_LA2=='r')) {
								mROW(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='@') && (cached_LA2=='c')) {
								mCELL(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='@') && (cached_LA2=='H')) {
								mHTTP(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='$') && (cached_LA2=='{')) {
								mREFERENCE(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='$') && (true)) {
								mDOLLAR(true);
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

	public void mTABLE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = TABLE;

		match("@table{");
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mROW(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = ROW;

		match("@row{");
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mCELL(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = CELL;

		int _saveIndex = 0;
		_saveIndex = text.Length;
		match("@cell{");
		text.Length = _saveIndex;
		{    // ( ... )*
			for (;;)
			{
				if ((tokenSet_0_.member(cached_LA1)))
				{
					matchNot('}');
				}
				else
				{
					goto _loop17_breakloop;
				}

			}
_loop17_breakloop:			;
		}    // ( ... )*
		_saveIndex = text.Length;
		mRBRACE(false);
		text.Length = _saveIndex;
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

	public void mHTTP(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = HTTP;

		int _saveIndex = 0;
		_saveIndex = text.Length;
		match("@HTTP{");
		text.Length = _saveIndex;
		{    // ( ... )*
			for (;;)
			{
				if ((tokenSet_0_.member(cached_LA1)))
				{
					matchNot('}');
				}
				else
				{
					goto _loop20_breakloop;
				}

			}
_loop20_breakloop:			;
		}    // ( ... )*
		_saveIndex = text.Length;
		mRBRACE(false);
		text.Length = _saveIndex;
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mREFERENCE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = REFERENCE;

		int _saveIndex = 0;
		_saveIndex = text.Length;
		mDOLLAR(false);
		text.Length = _saveIndex;
		_saveIndex = text.Length;
		mLBRACE(false);
		text.Length = _saveIndex;
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
				mIDENTIFIER(false);
				{
					switch ( cached_LA1 )
					{
					case '#':
					{
						mPOUND(false);
						mIDENTIFIER(false);
						break;
					}
					case '}':
					{
						break;
					}
					default:
					{
						throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
					}
					 }
				}
				break;
			}
			case '#':
			{
				_saveIndex = text.Length;
				mPOUND(false);
				text.Length = _saveIndex;
				mIDENTIFIER(false);
				break;
			}
			default:
			{
				throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
			}
			 }
		}
		_saveIndex = text.Length;
		mRBRACE(false);
		text.Length = _saveIndex;
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mDOLLAR(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = DOLLAR;

		match('$');
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

	protected void mIDENTIFIER(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = IDENTIFIER;

		mLETTER(false);
		{    // ( ... )*
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
					goto _loop30_breakloop;
				}
				 }
			}
_loop30_breakloop:			;
		}    // ( ... )*
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mPOUND(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = POUND;

		match('#');
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

	protected void mIGNORE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = IGNORE;
		char  c = '\0';

		bool synPredMatched36 = false;
		if (((cached_LA1=='\n') && (true)))
		{
			int _m36 = mark();
			synPredMatched36 = true;
			inputState.guessing++;
			try {
				{
					match('\n');
				}
			}
			catch (RecognitionException)
			{
				synPredMatched36 = false;
			}
			rewind(_m36);
			inputState.guessing--;
		}
		if ( synPredMatched36 )
		{
			match('\n');
			if (0==inputState.guessing)
			{
				newline(); m_bldr.AppendLine();
			}
		}
		else {
			bool synPredMatched38 = false;
			if (((cached_LA1=='\r') && (true)))
			{
				int _m38 = mark();
				synPredMatched38 = true;
				inputState.guessing++;
				try {
					{
						match('\r');
					}
				}
				catch (RecognitionException)
				{
					synPredMatched38 = false;
				}
				rewind(_m38);
				inputState.guessing--;
			}
			if ( synPredMatched38 )
			{
				match('\r');
			}
			else if (((cached_LA1 >= '\u0003' && cached_LA1 <= '\u00ff')) && (true)) {
				c = cached_LA1;
				matchNot(EOF/*_CHAR*/);
				if (0==inputState.guessing)
				{
					m_bldr.Append(c);
				}
			}
			else
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
			long[] data = new long[8];
			data[0]=-8L;
			data[1]=-2305843009213693953L;
			for (int i = 2; i<=3; i++) { data[i]=-1L; }
			for (int i = 4; i<=7; i++) { data[i]=0L; }
			return data;
		}
		public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());

	}
}
