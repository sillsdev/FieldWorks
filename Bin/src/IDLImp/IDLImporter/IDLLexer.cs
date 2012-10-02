// $ANTLR 2.7.7 (20060930): "idl.g" -> "IDLLexer.cs"$

/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: idl.g
/// Responsibility: Eberhard Beilharz
/// Last reviewed:
///
/// <remarks>
/// Defines the (partial) IDL grammar and some actions. It needs to be compiled with the ANTL
/// tool
/// </remarks>
/// --------------------------------------------------------------------------------------------

//#define DEBUG_IDLGRAMMAR

using System.Diagnostics;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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

	public 	class IDLLexer : antlr.CharScanner	, TokenStream
	 {
		public const int EOF = 1;
		public const int NULL_TREE_LOOKAHEAD = 3;
		public const int V1_ENUM = 4;
		public const int INT3264 = 5;
		public const int INT64 = 6;
		public const int SEMI = 7;
		public const int LBRACKET = 8;
		public const int RBRACKET = 9;
		public const int LITERAL_module = 10;
		public const int LBRACE = 11;
		public const int RBRACE = 12;
		public const int LITERAL_import = 13;
		public const int COMMA = 14;
		public const int LITERAL_library = 15;
		public const int LITERAL_coclass = 16;
		public const int LITERAL_uuid = 17;
		public const int LITERAL_version = 18;
		public const int LPAREN = 19;
		public const int RPAREN = 20;
		public const int LITERAL_async_uuid = 21;
		public const int LITERAL_local = 22;
		public const int LITERAL_object = 23;
		public const int LITERAL_pointer_default = 24;
		public const int LITERAL_endpoint = 25;
		public const int LITERAL_odl = 26;
		public const int LITERAL_optimize = 27;
		public const int LITERAL_proxy = 28;
		public const int LITERAL_aggregatable = 29;
		public const int LITERAL_appobject = 30;
		public const int LITERAL_bindable = 31;
		public const int LITERAL_control = 32;
		public const int LITERAL_custom = 33;
		public const int LITERAL_default = 34;
		public const int LITERAL_defaultbind = 35;
		public const int LITERAL_defaultcollelem = 36;
		public const int LITERAL_defaultvtable = 37;
		public const int LITERAL_displaybind = 38;
		public const int LITERAL_dllname = 39;
		public const int LITERAL_dual = 40;
		public const int LITERAL_entry = 41;
		public const int LITERAL_helpcontext = 42;
		public const int LITERAL_helpfile = 43;
		public const int LITERAL_helpstring = 44;
		public const int LITERAL_helpstringdll = 45;
		public const int LITERAL_hidden = 46;
		public const int LITERAL_id = 47;
		public const int LITERAL_idempotent = 48;
		public const int LITERAL_immediatebind = 49;
		public const int LITERAL_lcid = 50;
		public const int LITERAL_licensed = 51;
		public const int LITERAL_message = 52;
		public const int LITERAL_nonbrowsable = 53;
		public const int LITERAL_noncreatable = 54;
		public const int LITERAL_nonextensible = 55;
		public const int LITERAL_oleautomation = 56;
		public const int LITERAL_restricted = 57;
		public const int LITERAL_importlib = 58;
		public const int LITERAL_interface = 59;
		public const int LITERAL_dispinterface = 60;
		public const int COLON = 61;
		public const int SCOPEOP = 62;
		public const int LITERAL_const = 63;
		public const int ASSIGN = 64;
		public const int STAR = 65;
		public const int OR = 66;
		public const int XOR = 67;
		public const int AND = 68;
		public const int LSHIFT = 69;
		public const int RSHIFT = 70;
		public const int PLUS = 71;
		public const int MINUS = 72;
		public const int DIV = 73;
		public const int MOD = 74;
		public const int TILDE = 75;
		public const int LITERAL_TRUE = 76;
		public const int LITERAL_true = 77;
		public const int LITERAL_FALSE = 78;
		public const int LITERAL_false = 79;
		public const int LITERAL_typedef = 80;
		public const int LITERAL_native = 81;
		public const int LITERAL_context_handle = 82;
		public const int LITERAL_handle = 83;
		public const int LITERAL_pipe = 84;
		public const int LITERAL_transmit_as = 85;
		public const int LITERAL_wire_marshal = 86;
		public const int LITERAL_represent_as = 87;
		public const int LITERAL_user_marshal = 88;
		public const int LITERAL_public = 89;
		public const int LITERAL_switch_type = 90;
		public const int LITERAL_signed = 91;
		public const int LITERAL_unsigned = 92;
		public const int LITERAL_octet = 93;
		public const int LITERAL_any = 94;
		public const int LITERAL_void = 95;
		public const int LITERAL_byte = 96;
		public const int LITERAL_wchar_t = 97;
		public const int LITERAL_handle_t = 98;
		public const int INT = 99;
		public const int HEX = 100;
		public const int LITERAL_ref = 101;
		public const int LITERAL_unique = 102;
		public const int LITERAL_ptr = 103;
		public const int LITERAL_small = 104;
		public const int LITERAL_short = 105;
		public const int LITERAL_long = 106;
		public const int LITERAL_int = 107;
		public const int LITERAL_hyper = 108;
		public const int LITERAL_char = 109;
		public const int LITERAL_float = 110;
		public const int LITERAL_double = 111;
		public const int LITERAL_boolean = 112;
		public const int LITERAL_struct = 113;
		public const int LITERAL_union = 114;
		public const int LITERAL_switch = 115;
		public const int LITERAL_case = 116;
		public const int LITERAL_enum = 117;
		public const int LITERAL_sequence = 118;
		public const int LT_ = 119;
		public const int GT = 120;
		public const int LITERAL_string = 121;
		public const int RANGE = 122;
		public const int LITERAL_readonly = 123;
		public const int LITERAL_attribute = 124;
		public const int LITERAL_exception = 125;
		public const int LITERAL_callback = 126;
		public const int LITERAL_broadcast = 127;
		public const int LITERAL_ignore = 128;
		public const int LITERAL_propget = 129;
		public const int LITERAL_propput = 130;
		public const int LITERAL_propputref = 131;
		public const int LITERAL_uidefault = 132;
		public const int LITERAL_usesgetlasterror = 133;
		public const int LITERAL_vararg = 134;
		public const int LITERAL_in = 135;
		public const int LITERAL_out = 136;
		public const int LITERAL_retval = 137;
		public const int LITERAL_defaultvalue = 138;
		public const int LITERAL_optional = 139;
		public const int LITERAL_requestedit = 140;
		public const int LITERAL_iid_is = 141;
		public const int LITERAL_range = 142;
		public const int LITERAL_size_is = 143;
		public const int LITERAL_max_is = 144;
		public const int LITERAL_length_is = 145;
		public const int LITERAL_first_is = 146;
		public const int LITERAL_last_is = 147;
		public const int LITERAL_switch_is = 148;
		public const int LITERAL_source = 149;
		public const int LITERAL_raises = 150;
		public const int LITERAL_context = 151;
		public const int LITERAL_SAFEARRAY = 152;
		public const int OCTAL = 153;
		public const int LITERAL_L = 154;
		public const int STRING_LITERAL = 155;
		public const int CHAR_LITERAL = 156;
		public const int FLOAT = 157;
		public const int IDENT = 158;
		public const int LITERAL_cpp_quote = 159;
		public const int LITERAL_midl_pragma_warning = 160;
		public const int QUESTION = 161;
		public const int DOT = 162;
		public const int NOT = 163;
		public const int QUOTE = 164;
		public const int WS_ = 165;
		public const int PREPROC_DIRECTIVE = 166;
		public const int SL_COMMENT = 167;
		public const int ML_COMMENT = 168;
		public const int ESC = 169;
		public const int VOCAB = 170;
		public const int DIGIT = 171;
		public const int OCTDIGIT = 172;
		public const int HEXDIGIT = 173;

		public IDLLexer(Stream ins) : this(new ByteBuffer(ins))
		{
		}

		public IDLLexer(TextReader r) : this(new CharBuffer(r))
		{
		}

		public IDLLexer(InputBuffer ib)		 : this(new LexerSharedInputState(ib))
		{
		}

		public IDLLexer(LexerSharedInputState state) : base(state)
		{
			initialize();
		}
		private void initialize()
		{
			caseSensitiveLiterals = true;
			setCaseSensitive(true);
			literals = new Hashtable(100, (float) 0.4, null, Comparer.Default);
			literals.Add("local", 22);
			literals.Add("size_is", 143);
			literals.Add("optional", 139);
			literals.Add("proxy", 28);
			literals.Add("last_is", 147);
			literals.Add("byte", 96);
			literals.Add("public", 89);
			literals.Add("represent_as", 87);
			literals.Add("case", 116);
			literals.Add("message", 52);
			literals.Add("short", 105);
			literals.Add("uidefault", 132);
			literals.Add("raises", 150);
			literals.Add("defaultbind", 35);
			literals.Add("object", 23);
			literals.Add("ignore", 128);
			literals.Add("readonly", 123);
			literals.Add("lcid", 50);
			literals.Add("propputref", 131);
			literals.Add("octet", 93);
			literals.Add("wire_marshal", 86);
			literals.Add("licensed", 51);
			literals.Add("module", 10);
			literals.Add("unsigned", 92);
			literals.Add("const", 63);
			literals.Add("float", 110);
			literals.Add("context_handle", 82);
			literals.Add("context", 151);
			literals.Add("length_is", 145);
			literals.Add("source", 149);
			literals.Add("retval", 137);
			literals.Add("defaultvalue", 138);
			literals.Add("ptr", 103);
			literals.Add("appobject", 30);
			literals.Add("first_is", 146);
			literals.Add("noncreatable", 54);
			literals.Add("control", 32);
			literals.Add("handle", 83);
			literals.Add("optimize", 27);
			literals.Add("importlib", 58);
			literals.Add("small", 104);
			literals.Add("ref", 101);
			literals.Add("handle_t", 98);
			literals.Add("cpp_quote", 159);
			literals.Add("custom", 33);
			literals.Add("range", 142);
			literals.Add("out", 136);
			literals.Add("callback", 126);
			literals.Add("library", 15);
			literals.Add("displaybind", 38);
			literals.Add("native", 81);
			literals.Add("iid_is", 141);
			literals.Add("hyper", 108);
			literals.Add("L", 154);
			literals.Add("entry", 41);
			literals.Add("FALSE", 78);
			literals.Add("usesgetlasterror", 133);
			literals.Add("oleautomation", 56);
			literals.Add("propput", 130);
			literals.Add("version", 18);
			literals.Add("typedef", 80);
			literals.Add("nonbrowsable", 53);
			literals.Add("interface", 59);
			literals.Add("sequence", 118);
			literals.Add("uuid", 17);
			literals.Add("switch_type", 90);
			literals.Add("pointer_default", 24);
			literals.Add("broadcast", 127);
			literals.Add("immediatebind", 49);
			literals.Add("coclass", 16);
			literals.Add("aggregatable", 29);
			literals.Add("midl_pragma_warning", 160);
			literals.Add("dispinterface", 60);
			literals.Add("any", 94);
			literals.Add("double", 111);
			literals.Add("SAFEARRAY", 152);
			literals.Add("nonextensible", 55);
			literals.Add("union", 114);
			literals.Add("__int3264", 5);
			literals.Add("enum", 117);
			literals.Add("pipe", 84);
			literals.Add("propget", 129);
			literals.Add("int", 107);
			literals.Add("exception", 125);
			literals.Add("switch_is", 148);
			literals.Add("boolean", 112);
			literals.Add("max_is", 144);
			literals.Add("requestedit", 140);
			literals.Add("char", 109);
			literals.Add("defaultvtable", 37);
			literals.Add("string", 121);
			literals.Add("default", 34);
			literals.Add("odl", 26);
			literals.Add("id", 47);
			literals.Add("dual", 40);
			literals.Add("helpstringdll", 45);
			literals.Add("false", 79);
			literals.Add("user_marshal", 88);
			literals.Add("restricted", 57);
			literals.Add("helpfile", 43);
			literals.Add("bindable", 31);
			literals.Add("dllname", 39);
			literals.Add("attribute", 124);
			literals.Add("v1_enum", 4);
			literals.Add("async_uuid", 21);
			literals.Add("struct", 113);
			literals.Add("__int64", 6);
			literals.Add("helpcontext", 42);
			literals.Add("signed", 91);
			literals.Add("import", 13);
			literals.Add("endpoint", 25);
			literals.Add("in", 135);
			literals.Add("TRUE", 76);
			literals.Add("void", 95);
			literals.Add("wchar_t", 97);
			literals.Add("transmit_as", 85);
			literals.Add("switch", 115);
			literals.Add("defaultcollelem", 36);
			literals.Add("helpstring", 44);
			literals.Add("true", 77);
			literals.Add("long", 106);
			literals.Add("hidden", 46);
			literals.Add("unique", 102);
			literals.Add("idempotent", 48);
			literals.Add("vararg", 134);
		}

		override public IToken nextToken()			//throws TokenStreamException
		{
			IToken theRetToken = null;
tryAgain:
			for (;;)
			{
				IToken _token = null;
				int _ttype = Token.INVALID_TYPE;
				resetText();
				try     // for char stream error handling
				{
					try     // for lexical error handling
					{
						switch ( cached_LA1 )
						{
						case ';':
						{
							mSEMI(true);
							theRetToken = returnToken_;
							break;
						}
						case '?':
						{
							mQUESTION(true);
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
						case '[':
						{
							mLBRACKET(true);
							theRetToken = returnToken_;
							break;
						}
						case ']':
						{
							mRBRACKET(true);
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
						case '|':
						{
							mOR(true);
							theRetToken = returnToken_;
							break;
						}
						case '^':
						{
							mXOR(true);
							theRetToken = returnToken_;
							break;
						}
						case '&':
						{
							mAND(true);
							theRetToken = returnToken_;
							break;
						}
						case ',':
						{
							mCOMMA(true);
							theRetToken = returnToken_;
							break;
						}
						case '=':
						{
							mASSIGN(true);
							theRetToken = returnToken_;
							break;
						}
						case '!':
						{
							mNOT(true);
							theRetToken = returnToken_;
							break;
						}
						case '+':
						{
							mPLUS(true);
							theRetToken = returnToken_;
							break;
						}
						case '-':
						{
							mMINUS(true);
							theRetToken = returnToken_;
							break;
						}
						case '~':
						{
							mTILDE(true);
							theRetToken = returnToken_;
							break;
						}
						case '*':
						{
							mSTAR(true);
							theRetToken = returnToken_;
							break;
						}
						case '%':
						{
							mMOD(true);
							theRetToken = returnToken_;
							break;
						}
						case '\t':  case '\n':  case '\r':  case ' ':
						{
							mWS_(true);
							theRetToken = returnToken_;
							break;
						}
						case '#':
						{
							mPREPROC_DIRECTIVE(true);
							theRetToken = returnToken_;
							break;
						}
						case '\'':
						{
							mCHAR_LITERAL(true);
							theRetToken = returnToken_;
							break;
						}
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
							mIDENT(true);
							theRetToken = returnToken_;
							break;
						}
						default:
							if ((cached_LA1=='.') && (cached_LA2=='.'))
							{
								mRANGE(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='<') && (cached_LA2=='<')) {
								mLSHIFT(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='>') && (cached_LA2=='>')) {
								mRSHIFT(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1==':') && (cached_LA2==':')) {
								mSCOPEOP(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='/') && (cached_LA2=='/')) {
								mSL_COMMENT(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='/') && (cached_LA2=='*')) {
								mML_COMMENT(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='"') && ((cached_LA2 >= '\u0000' && cached_LA2 <= '\u00ff'))) {
								mSTRING_LITERAL(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='0') && (cached_LA2=='X'||cached_LA2=='x')) {
								mHEX(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='.') && ((cached_LA2 >= '0' && cached_LA2 <= '9'))) {
								mFLOAT(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1==':') && (true)) {
								mCOLON(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='.') && (true)) {
								mDOT(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='<') && (true)) {
								mLT_(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='>') && (true)) {
								mGT(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='/') && (true)) {
								mDIV(true);
								theRetToken = returnToken_;
							}
							else if ((cached_LA1=='"') && (true)) {
								mQUOTE(true);
								theRetToken = returnToken_;
							}
							else if (((cached_LA1 >= '0' && cached_LA1 <= '9')) && (true)) {
								mINT(true);
								theRetToken = returnToken_;
							}
						else
						{
							if (cached_LA1==EOF_CHAR) { uponEOF(); returnToken_ = makeToken(Token.EOF_TYPE); }
				else {throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());}
						}
						break; }
						if ( null==returnToken_ ) goto tryAgain; // found SKIP token
						_ttype = returnToken_.Type;
						_ttype = testLiteralsTable(_ttype);
						returnToken_.Type = _ttype;
						return returnToken_;
					}
					catch (RecognitionException e) {
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

	public void mSEMI(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = SEMI;

		match(';');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mQUESTION(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = QUESTION;

		match('?');
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

	public void mOR(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = OR;

		match('|');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mXOR(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = XOR;

		match('^');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mAND(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = AND;

		match('&');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mCOLON(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = COLON;

		match(':');
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

	public void mDOT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = DOT;

		match('.');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mRANGE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = RANGE;

		match("..");
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mASSIGN(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = ASSIGN;

		match('=');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mNOT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = NOT;

		match('!');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mLT_(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = LT_;

		match('<');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mLSHIFT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = LSHIFT;

		match("<<");
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mGT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = GT;

		match('>');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mRSHIFT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = RSHIFT;

		match(">>");
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mDIV(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = DIV;

		match('/');
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

	public void mTILDE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = TILDE;

		match('~');
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

	public void mMOD(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = MOD;

		match('%');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mSCOPEOP(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = SCOPEOP;

		match("::");
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mQUOTE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = QUOTE;

		match('"');
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mWS_(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = WS_;

		{
			switch ( cached_LA1 )
			{
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
			case '\n':
			{
				match('\n');
				newline();
				break;
			}
			case '\r':
			{
				match('\r');
				break;
			}
			default:
			{
				throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
			}
			 }
		}
		_ttype = Token.SKIP;
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mPREPROC_DIRECTIVE(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = PREPROC_DIRECTIVE;

		match('#');
		{    // ( ... )*
			for (;;)
			{
				if ((tokenSet_0_.member(cached_LA1)))
				{
					matchNot('\n');
				}
				else
				{
					goto _loop283_breakloop;
				}

			}
_loop283_breakloop:			;
		}    // ( ... )*
		match('\n');
		newline(); _ttype = Token.SKIP;
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mSL_COMMENT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = SL_COMMENT;

		match("//");
		{    // ( ... )*
			for (;;)
			{
				if ((tokenSet_0_.member(cached_LA1)))
				{
					matchNot('\n');
				}
				else
				{
					goto _loop286_breakloop;
				}

			}
_loop286_breakloop:			;
		}    // ( ... )*
		match('\n');
		_ttype = Token.SKIP; newline();
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mML_COMMENT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = ML_COMMENT;

		match("/*");
		{    // ( ... )*
			for (;;)
			{
				switch ( cached_LA1 )
				{
				case '"':
				{
					mSTRING_LITERAL(false);
					break;
				}
				case '\'':
				{
					mCHAR_LITERAL(false);
					break;
				}
				case '\n':
				{
					match('\n');
					newline();
					break;
				}
				default:
					if ((cached_LA1=='*') && (tokenSet_1_.member(cached_LA2)))
					{
						match('*');
						matchNot('/');
					}
					else if ((tokenSet_2_.member(cached_LA1))) {
						matchNot('*');
					}
				else
				{
					goto _loop289_breakloop;
				}
				break; }
			}
_loop289_breakloop:			;
		}    // ( ... )*
		match("*/");
		_ttype = Token.SKIP;
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
				else if ((tokenSet_3_.member(cached_LA1))) {
					matchNot('"');
				}
				else
				{
					goto _loop294_breakloop;
				}

			}
_loop294_breakloop:			;
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

	public void mCHAR_LITERAL(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = CHAR_LITERAL;

		match('\'');
		{
			if ((cached_LA1=='\\'))
			{
				mESC(false);
			}
			else if ((tokenSet_4_.member(cached_LA1))) {
				matchNot('\'');
			}
			else
			{
				throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
			}

		}
		match('\'');
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
					if (((cached_LA1 >= '0' && cached_LA1 <= '7')) && ((cached_LA2 >= '\u0000' && cached_LA2 <= '\u00ff')) && (true) && (true))
					{
						mOCTDIGIT(false);
						{
							if (((cached_LA1 >= '0' && cached_LA1 <= '7')) && ((cached_LA2 >= '\u0000' && cached_LA2 <= '\u00ff')) && (true) && (true))
							{
								mOCTDIGIT(false);
							}
							else if (((cached_LA1 >= '\u0000' && cached_LA1 <= '\u00ff')) && (true) && (true) && (true)) {
							}
							else
							{
								throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
							}

						}
					}
					else if (((cached_LA1 >= '\u0000' && cached_LA1 <= '\u00ff')) && (true) && (true) && (true)) {
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
					if ((tokenSet_5_.member(cached_LA1)) && ((cached_LA2 >= '\u0000' && cached_LA2 <= '\u00ff')) && (true) && (true))
					{
						mHEXDIGIT(false);
					}
					else if (((cached_LA1 >= '\u0000' && cached_LA1 <= '\u00ff')) && (true) && (true) && (true)) {
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

		{
			switch ( cached_LA1 )
			{
			case '0':  case '1':  case '2':  case '3':
			case '4':  case '5':  case '6':  case '7':
			case '8':  case '9':
			{
				matchRange('0','9');
				break;
			}
			case 'a':  case 'b':  case 'c':  case 'd':
			case 'e':  case 'f':
			{
				matchRange('a','f');
				break;
			}
			case 'A':  case 'B':  case 'C':  case 'D':
			case 'E':  case 'F':
			{
				matchRange('A','F');
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

	protected void mVOCAB(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = VOCAB;

		matchRange('\x3','\xff');
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

	public void mHEX(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = HEX;

		{
			if ((cached_LA1=='0') && (cached_LA2=='x'))
			{
				match("0x");
			}
			else if ((cached_LA1=='0') && (cached_LA2=='X')) {
				match("0X");
			}
			else
			{
				throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
			}

		}
		{ // ( ... )+
			int _cnt309=0;
			for (;;)
			{
				if ((tokenSet_5_.member(cached_LA1)))
				{
					mHEXDIGIT(false);
				}
				else
				{
					if (_cnt309 >= 1) { goto _loop309_breakloop; } else { throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());; }
				}

				_cnt309++;
			}
_loop309_breakloop:			;
		}    // ( ... )+
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mINT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = INT;

		{ // ( ... )+
			int _cnt312=0;
			for (;;)
			{
				if (((cached_LA1 >= '0' && cached_LA1 <= '9')))
				{
					mDIGIT(false);
				}
				else
				{
					if (_cnt312 >= 1) { goto _loop312_breakloop; } else { throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());; }
				}

				_cnt312++;
			}
_loop312_breakloop:			;
		}    // ( ... )+
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mFLOAT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = FLOAT;

		match('.');
		{ // ( ... )+
			int _cnt315=0;
			for (;;)
			{
				if (((cached_LA1 >= '0' && cached_LA1 <= '9')))
				{
					mDIGIT(false);
				}
				else
				{
					if (_cnt315 >= 1) { goto _loop315_breakloop; } else { throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());; }
				}

				_cnt315++;
			}
_loop315_breakloop:			;
		}    // ( ... )+
		{
			if ((cached_LA1=='E'||cached_LA1=='e'))
			{
				{
					switch ( cached_LA1 )
					{
					case 'e':
					{
						match('e');
						break;
					}
					case 'E':
					{
						match('E');
						break;
					}
					default:
					{
						throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
					}
					 }
				}
				{
					switch ( cached_LA1 )
					{
					case '+':
					{
						match('+');
						break;
					}
					case '-':
					{
						match('-');
						break;
					}
					case '0':  case '1':  case '2':  case '3':
					case '4':  case '5':  case '6':  case '7':
					case '8':  case '9':
					{
						break;
					}
					default:
					{
						throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());
					}
					 }
				}
				{ // ( ... )+
					int _cnt320=0;
					for (;;)
					{
						if (((cached_LA1 >= '0' && cached_LA1 <= '9')))
						{
							mDIGIT(false);
						}
						else
						{
							if (_cnt320 >= 1) { goto _loop320_breakloop; } else { throw new NoViableAltForCharException(cached_LA1, getFilename(), getLine(), getColumn());; }
						}

						_cnt320++;
					}
_loop320_breakloop:					;
				}    // ( ... )+
			}
			else {
			}

		}
		if (_createToken && (null == _token) && (_ttype != Token.SKIP))
		{
			_token = makeToken(_ttype);
			_token.setText(text.ToString(_begin, text.Length-_begin));
		}
		returnToken_ = _token;
	}

	public void mIDENT(bool _createToken) //throws RecognitionException, CharStreamException, TokenStreamException
{
		int _ttype; IToken _token=null; int _begin=text.Length;
		_ttype = IDENT;

		{
			switch ( cached_LA1 )
			{
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
		}
		{    // ( ... )*
			for (;;)
			{
				switch ( cached_LA1 )
				{
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
				case '_':
				{
					match('_');
					break;
				}
				case '0':  case '1':  case '2':  case '3':
				case '4':  case '5':  case '6':  case '7':
				case '8':  case '9':
				{
					matchRange('0','9');
					break;
				}
				default:
				{
					goto _loop324_breakloop;
				}
				 }
			}
_loop324_breakloop:			;
		}    // ( ... )*
		_ttype = testLiteralsTable(_ttype);
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
		data[0]=-1025L;
		for (int i = 1; i<=3; i++) { data[i]=-1L; }
		for (int i = 4; i<=7; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = new long[8];
		data[0]=-140737488355329L;
		for (int i = 1; i<=3; i++) { data[i]=-1L; }
		for (int i = 4; i<=7; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	private static long[] mk_tokenSet_2_()
	{
		long[] data = new long[8];
		data[0]=-4964982195201L;
		for (int i = 1; i<=3; i++) { data[i]=-1L; }
		for (int i = 4; i<=7; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());
	private static long[] mk_tokenSet_3_()
	{
		long[] data = new long[8];
		data[0]=-17179869185L;
		data[1]=-268435457L;
		for (int i = 2; i<=3; i++) { data[i]=-1L; }
		for (int i = 4; i<=7; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_3_ = new BitSet(mk_tokenSet_3_());
	private static long[] mk_tokenSet_4_()
	{
		long[] data = new long[8];
		data[0]=-549755813889L;
		data[1]=-268435457L;
		for (int i = 2; i<=3; i++) { data[i]=-1L; }
		for (int i = 4; i<=7; i++) { data[i]=0L; }
		return data;
	}
	public static readonly BitSet tokenSet_4_ = new BitSet(mk_tokenSet_4_());
	private static long[] mk_tokenSet_5_()
	{
		long[] data = { 287948901175001088L, 541165879422L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_5_ = new BitSet(mk_tokenSet_5_());

}
}
