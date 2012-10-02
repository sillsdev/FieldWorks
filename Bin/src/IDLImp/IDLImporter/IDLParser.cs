// $ANTLR 2.7.7 (20060930): "idl.g" -> "IDLParser.cs"$

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
	// Generate the header common to all output files.
	using System;

	using TokenBuffer              = antlr.TokenBuffer;
	using TokenStreamException     = antlr.TokenStreamException;
	using TokenStreamIOException   = antlr.TokenStreamIOException;
	using ANTLRException           = antlr.ANTLRException;
	using LLkParser = antlr.LLkParser;
	using Token                    = antlr.Token;
	using IToken                   = antlr.IToken;
	using TokenStream              = antlr.TokenStream;
	using RecognitionException     = antlr.RecognitionException;
	using NoViableAltException     = antlr.NoViableAltException;
	using MismatchedTokenException = antlr.MismatchedTokenException;
	using SemanticException        = antlr.SemanticException;
	using ParserSharedInputState   = antlr.ParserSharedInputState;
	using BitSet                   = antlr.collections.impl.BitSet;
	using AST                      = antlr.collections.AST;
	using ASTPair                  = antlr.ASTPair;
	using ASTFactory               = antlr.ASTFactory;
	using ASTArray                 = antlr.collections.impl.ASTArray;

/**
 *  This is a complete parser for the IDL language as defined
 *  by the CORBA 2.0 specification.  It will allow those who
 *  need an IDL parser to get up-and-running very quickly.
 *  Though IDL's syntax is very similar to C++, it is also
 *  much simpler, due in large part to the fact that it is
 *  a declarative-only language.
 *
 *  Some things that are not included are: Symbol table construction
 *  (it is not necessary for parsing, btw) and preprocessing (for
 *  IDL compiler #pragma directives). You can use just about any
 *  C or C++ preprocessor, but there is an interesting semantic
 *  issue if you are going to generate code: In C, #include is
 *  a literal include, in IDL, #include is more like Java's import:
 *  It adds definitions to the scope of the parse, but included
 *  definitions are not generated.
 *
 *  Jim Coker, jcoker@magelang.com
 */
	public 	class IDLParser : antlr.LLkParser
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


	private CodeNamespace m_Namespace = null;
	private IDLConversions m_Conv = null;

		protected void initialize()
		{
			tokenNames = tokenNames_;
			initializeFactory();
		}


		protected IDLParser(TokenBuffer tokenBuf, int k) : base(tokenBuf, k)
		{
			initialize();
		}

		public IDLParser(TokenBuffer tokenBuf) : this(tokenBuf,1)
		{
		}

		protected IDLParser(TokenStream lexer, int k) : base(lexer,k)
		{
			initialize();
		}

		public IDLParser(TokenStream lexer) : this(lexer,1)
		{
		}

		public IDLParser(ParserSharedInputState state) : base(state,1)
		{
			initialize();
		}

	public void specification(
		CodeNamespace cnamespace, IDLConversions conv
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST specification_AST = null;

				m_Namespace = cnamespace;
				m_Conv = conv;


		try {      // for error handling
			{ // ( ... )+
				int _cnt3=0;
				for (;;)
				{
					if ((tokenSet_0_.member(LA(1))))
					{
						definition();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						if (_cnt3 >= 1) { goto _loop3_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt3++;
				}
_loop3_breakloop:				;
			}    // ( ... )+
			AST tmp1_AST = null;
			tmp1_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp1_AST);
			match(Token.EOF_TYPE);
			specification_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{

						reportError(ex);
						return;

			}
			else
			{
				throw;
			}
		}
		returnAST = specification_AST;
	}

	public void definition() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST definition_AST = null;
		AST c_AST = null;
		AST e_AST = null;
		AST l_AST = null;
		AST m_AST = null;
		AST mi_AST = null;

				Hashtable attributes = new Hashtable();
				CodeTypeMember type = null;
				CodeTypeDeclaration decl = null;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case SEMI:
				case LITERAL_typedef:
				case LITERAL_native:
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				{
					type=type_dcl();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					match(SEMI);
					if (0==inputState.guessing)
					{

									#if DEBUG_IDLGRAMMAR
										System.Diagnostics.Debug.WriteLine(string.Format("\nType declaration found {0}\n\n", type != null ? type.Name : "<empty>"));
									#endif
										if (type != null && type is CodeTypeDeclaration)
										{
											m_Namespace.Types.Add((CodeTypeDeclaration)type);
											m_Namespace.UserData.Add(type.Name, type);
										}

					}
					break;
				}
				case LITERAL_const:
				{
					const_dcl();
					if (0 == inputState.guessing)
					{
						c_AST = (AST)returnAST;
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					match(SEMI);
					if (0==inputState.guessing)
					{

									#if DEBUG_IDLGRAMMAR
										System.Diagnostics.Debug.WriteLine(string.Format("\nConstant found {0}\n\n", c_AST != null ? c_AST.ToStringList() : "<null>"));
									#endif

					}
					break;
				}
				case LITERAL_exception:
				{
					except_dcl();
					if (0 == inputState.guessing)
					{
						e_AST = (AST)returnAST;
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					match(SEMI);
					if (0==inputState.guessing)
					{

									#if DEBUG_IDLGRAMMAR
										System.Diagnostics.Debug.WriteLine(string.Format("\nException declaration found {0}\n\n", e_AST != null ? e_AST.ToStringList() : "<null>"));
									#endif

					}
					break;
				}
				case LBRACKET:
				case LITERAL_library:
				case LITERAL_coclass:
				case LITERAL_interface:
				case LITERAL_dispinterface:
				{
					{
						switch ( LA(1) )
						{
						case LBRACKET:
						{
							AST tmp5_AST = null;
							tmp5_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp5_AST);
							match(LBRACKET);
							attribute_list(attributes);
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
							AST tmp6_AST = null;
							tmp6_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp6_AST);
							match(RBRACKET);
							break;
						}
						case LITERAL_library:
						case LITERAL_coclass:
						case LITERAL_interface:
						case LITERAL_dispinterface:
						{
							break;
						}
						default:
						{
							throw new NoViableAltException(LT(1), getFilename());
						}
						 }
					}
					{
						switch ( LA(1) )
						{
						case LITERAL_library:
						{
							library();
							if (0 == inputState.guessing)
							{
								l_AST = (AST)returnAST;
								astFactory.addASTChild(ref currentAST, returnAST);
							}
							if (0==inputState.guessing)
							{

													#if DEBUG_IDLGRAMMAR
													System.Diagnostics.Debug.WriteLine(string.Format("\nLibrary found {0}\n\n", l_AST != null ? l_AST.ToStringList() : "<null>"));
													#endif

							}
							break;
						}
						case LITERAL_interface:
						case LITERAL_dispinterface:
						{
							decl=interf();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
							if (0==inputState.guessing)
							{

													if (!(bool)decl.UserData["IsPartial"])
													{
														#if DEBUG_IDLGRAMMAR
														System.Diagnostics.Debug.WriteLine(string.Format("\nInterface declaration found {0}\n\n", decl.Name));
														#endif
														m_Conv.HandleInterface(decl, m_Namespace, attributes);
														m_Namespace.Types.Add(decl);
														m_Namespace.UserData.Add(decl.Name, decl);
													}

							}
							break;
						}
						case LITERAL_coclass:
						{
							decl=coclass();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
							if (0==inputState.guessing)
							{

													if (!(bool)decl.UserData["IsPartial"])
													{
														#if DEBUG_IDLGRAMMAR
														System.Diagnostics.Debug.WriteLine(string.Format("\nCoclass declaration found {0}\n\n", decl.Name));
														#endif
														m_Conv.HandleCoClassInterface(decl, m_Namespace, attributes);
														// Add coclass interface
														m_Namespace.Types.Add(decl);
														// Add coclass object
														m_Namespace.Types.Add(m_Conv.DeclareCoClassObject(decl, m_Namespace, attributes));
														// Add coclass creator object
														m_Namespace.Types.Add(m_Conv.DeclareCoClassCreator(decl, m_Namespace, attributes));
														attributes.Clear();
													}

							}
							break;
						}
						default:
						{
							throw new NoViableAltException(LT(1), getFilename());
						}
						 }
					}
					break;
				}
				case LITERAL_module:
				{
					module();
					if (0 == inputState.guessing)
					{
						m_AST = (AST)returnAST;
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					match(SEMI);
					if (0==inputState.guessing)
					{

									#if DEBUG_IDLGRAMMAR
										System.Diagnostics.Debug.WriteLine(string.Format("\nModule found {0}\n\n", m_AST != null ? m_AST.ToStringList() : "<null>"));
									#endif

					}
					break;
				}
				case LITERAL_import:
				{
					import();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					match(SEMI);
					break;
				}
				case LITERAL_importlib:
				{
					importlib();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					match(SEMI);
					break;
				}
				case LITERAL_cpp_quote:
				{
					cpp_quote();
					break;
				}
				case LITERAL_midl_pragma_warning:
				{
					midl_pragma_warning();
					if (0 == inputState.guessing)
					{
						mi_AST = (AST)returnAST;
					}
					if (0==inputState.guessing)
					{

									#if DEBUG_IDLGRAMMAR
										System.Diagnostics.Debug.WriteLine(string.Format("\nMIDL pragma found {0}\n\n", mi_AST != null ? mi_AST.ToStringList() : "<null>"));
									#endif

					}
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			definition_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_1_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = definition_AST;
	}

	public CodeTypeMember  type_dcl() //throws RecognitionException, TokenStreamException
{
		CodeTypeMember type;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST type_dcl_AST = null;

				type = null;
				string ignored;
				Hashtable attributes = new Hashtable();


		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_typedef:
			{
				AST tmp10_AST = null;
				tmp10_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp10_AST);
				match(LITERAL_typedef);
				{
					switch ( LA(1) )
					{
					case LBRACKET:
					{
						AST tmp11_AST = null;
						tmp11_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp11_AST);
						match(LBRACKET);
						type_attributes();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						AST tmp12_AST = null;
						tmp12_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp12_AST);
						match(RBRACKET);
						break;
					}
					case INT3264:
					case INT64:
					case SCOPEOP:
					case LITERAL_const:
					case LITERAL_signed:
					case LITERAL_unsigned:
					case LITERAL_octet:
					case LITERAL_any:
					case LITERAL_void:
					case LITERAL_byte:
					case LITERAL_wchar_t:
					case LITERAL_handle_t:
					case LITERAL_small:
					case LITERAL_short:
					case LITERAL_long:
					case LITERAL_int:
					case LITERAL_hyper:
					case LITERAL_char:
					case LITERAL_float:
					case LITERAL_double:
					case LITERAL_boolean:
					case LITERAL_struct:
					case LITERAL_union:
					case LITERAL_enum:
					case LITERAL_sequence:
					case LITERAL_string:
					case IDENT:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				type=type_declarator();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				type_dcl_AST = currentAST.root;
				break;
			}
			case LITERAL_struct:
			{
				type=struct_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				type_dcl_AST = currentAST.root;
				break;
			}
			case LITERAL_union:
			{
				union_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				type_dcl_AST = currentAST.root;
				break;
			}
			case LITERAL_enum:
			{
				type=enum_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				type_dcl_AST = currentAST.root;
				break;
			}
			case SEMI:
			{
				type_dcl_AST = currentAST.root;
				break;
			}
			case LITERAL_native:
			{
				AST tmp13_AST = null;
				tmp13_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp13_AST);
				match(LITERAL_native);
				ignored=declarator(attributes);
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				type_dcl_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = type_dcl_AST;
		return type;
	}

	public void const_dcl() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST const_dcl_AST = null;
		string ignored;

		try {      // for error handling
			AST tmp14_AST = null;
			tmp14_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp14_AST);
			match(LITERAL_const);
			const_type();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			identifier();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp15_AST = null;
			tmp15_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp15_AST);
			match(ASSIGN);
			ignored=const_exp();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			const_dcl_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = const_dcl_AST;
	}

	public void except_dcl() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST except_dcl_AST = null;
		CodeTypeMemberCollection ignored = new CodeTypeMemberCollection();

		try {      // for error handling
			AST tmp16_AST = null;
			tmp16_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp16_AST);
			match(LITERAL_exception);
			identifier();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp17_AST = null;
			tmp17_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp17_AST);
			match(LBRACE);
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_3_.member(LA(1))))
					{
						member(ignored);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop195_breakloop;
					}

				}
_loop195_breakloop:				;
			}    // ( ... )*
			AST tmp18_AST = null;
			tmp18_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp18_AST);
			match(RBRACE);
			except_dcl_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = except_dcl_AST;
	}

	public void attribute_list(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST attribute_list_AST = null;

		try {      // for error handling
			attribute(attributes);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp19_AST = null;
						tmp19_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp19_AST);
						match(COMMA);
						attribute(attributes);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop25_breakloop;
					}

				}
_loop25_breakloop:				;
			}    // ( ... )*
			attribute_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_4_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = attribute_list_AST;
	}

	public void library() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST library_AST = null;

		try {      // for error handling
			AST tmp20_AST = null;
			tmp20_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(ref currentAST, tmp20_AST);
			match(LITERAL_library);
			identifier();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp21_AST = null;
			tmp21_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp21_AST);
			match(LBRACE);
			{ // ( ... )+
				int _cnt17=0;
				for (;;)
				{
					if ((tokenSet_0_.member(LA(1))))
					{
						definition();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						if (_cnt17 >= 1) { goto _loop17_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt17++;
				}
_loop17_breakloop:				;
			}    // ( ... )+
			AST tmp22_AST = null;
			tmp22_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp22_AST);
			match(RBRACE);
			match(SEMI);
			library_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_1_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = library_AST;
	}

	public CodeTypeDeclaration  interf() //throws RecognitionException, TokenStreamException
{
		CodeTypeDeclaration type;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST interf_AST = null;
		AST name_AST = null;

				bool fForwardDeclaration = true;
				StringCollection inherits;
				type = new CodeTypeDeclaration();
				type.IsInterface = true;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LITERAL_interface:
				{
					AST tmp24_AST = null;
					tmp24_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(ref currentAST, tmp24_AST);
					match(LITERAL_interface);
					break;
				}
				case LITERAL_dispinterface:
				{
					AST tmp25_AST = null;
					tmp25_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(ref currentAST, tmp25_AST);
					match(LITERAL_dispinterface);
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			identifier();
			if (0 == inputState.guessing)
			{
				name_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			inherits=inheritance_spec();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{
				switch ( LA(1) )
				{
				case LBRACE:
				{
					match(LBRACE);
					{    // ( ... )*
						for (;;)
						{
							if ((tokenSet_5_.member(LA(1))))
							{
								fForwardDeclaration=interface_body(type);
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(ref currentAST, returnAST);
								}
							}
							else
							{
								goto _loop44_breakloop;
							}

						}
_loop44_breakloop:						;
					}    // ( ... )*
					match(RBRACE);
					break;
				}
				case EOF:
				case SEMI:
				case LBRACKET:
				case LITERAL_module:
				case RBRACE:
				case LITERAL_import:
				case LITERAL_library:
				case LITERAL_coclass:
				case LITERAL_importlib:
				case LITERAL_interface:
				case LITERAL_dispinterface:
				case LITERAL_const:
				case LITERAL_typedef:
				case LITERAL_native:
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				case LITERAL_exception:
				case LITERAL_cpp_quote:
				case LITERAL_midl_pragma_warning:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			if (0==inputState.guessing)
			{

							/// we don't treat a forward declaration as real declaration
							type.Name = name_AST.getText();
							type.UserData.Add("IsPartial", fForwardDeclaration);
							type.UserData.Add("inherits", inherits);

			}
			interf_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_1_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = interf_AST;
		return type;
	}

	public CodeTypeDeclaration  coclass() //throws RecognitionException, TokenStreamException
{
		CodeTypeDeclaration type;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST coclass_AST = null;
		AST name_AST = null;

				Hashtable attributes = new Hashtable();
				type = new CodeTypeDeclaration();
				type.IsInterface = true;
				type.UserData.Add("IsPartial", false); // The isPartial property is a .Net 2.0 feature


		try {      // for error handling
			AST tmp28_AST = null;
			tmp28_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(ref currentAST, tmp28_AST);
			match(LITERAL_coclass);
			identifier();
			if (0 == inputState.guessing)
			{
				name_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{
				type.Name = name_AST.getText();
			}
			AST tmp29_AST = null;
			tmp29_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp29_AST);
			match(LBRACE);
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==LBRACKET||LA(1)==LITERAL_interface||LA(1)==LITERAL_dispinterface))
					{
						{
							switch ( LA(1) )
							{
							case LBRACKET:
							{
								AST tmp30_AST = null;
								tmp30_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp30_AST);
								match(LBRACKET);
								attribute_list(attributes);
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(ref currentAST, returnAST);
								}
								AST tmp31_AST = null;
								tmp31_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp31_AST);
								match(RBRACKET);
								break;
							}
							case LITERAL_interface:
							case LITERAL_dispinterface:
							{
								break;
							}
							default:
							{
								throw new NoViableAltException(LT(1), getFilename());
							}
							 }
						}
						interf_declr(type.BaseTypes);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						{
							switch ( LA(1) )
							{
							case SEMI:
							{
								match(SEMI);
								break;
							}
							case LBRACKET:
							case RBRACE:
							case LITERAL_interface:
							case LITERAL_dispinterface:
							{
								break;
							}
							default:
							{
								throw new NoViableAltException(LT(1), getFilename());
							}
							 }
						}
					}
					else
					{
						goto _loop22_breakloop;
					}

				}
_loop22_breakloop:				;
			}    // ( ... )*
			AST tmp33_AST = null;
			tmp33_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp33_AST);
			match(RBRACE);
			coclass_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_1_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = coclass_AST;
		return type;
	}

	public void module() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST module_AST = null;
		AST d_AST = null;

		try {      // for error handling
			AST tmp34_AST = null;
			tmp34_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(ref currentAST, tmp34_AST);
			match(LITERAL_module);
			identifier();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp35_AST = null;
			tmp35_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp35_AST);
			match(LBRACE);
			definition_list();
			if (0 == inputState.guessing)
			{
				d_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp36_AST = null;
			tmp36_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp36_AST);
			match(RBRACE);
			module_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = module_AST;
	}

	public void import() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST import_AST = null;

		try {      // for error handling
			AST tmp37_AST = null;
			tmp37_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(ref currentAST, tmp37_AST);
			match(LITERAL_import);
			string_literal();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp38_AST = null;
						tmp38_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp38_AST);
						match(COMMA);
						string_literal();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop11_breakloop;
					}

				}
_loop11_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{
				import_AST = (AST)currentAST.root;

							#if DEBUG_IDLGRAMMAR
								System.Diagnostics.Debug.WriteLine(import_AST.ToStringList());
							#endif

			}
			import_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = import_AST;
	}

	public void importlib() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST importlib_AST = null;
		AST str_AST = null;

		try {      // for error handling
			AST tmp39_AST = null;
			tmp39_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(ref currentAST, tmp39_AST);
			match(LITERAL_importlib);
			AST tmp40_AST = null;
			tmp40_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp40_AST);
			match(LPAREN);
			string_literal();
			if (0 == inputState.guessing)
			{
				str_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp41_AST = null;
			tmp41_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp41_AST);
			match(RPAREN);
			if (0==inputState.guessing)
			{

							#if DEBUG_IDLGRAMMAR
								System.Diagnostics.Debug.WriteLine("importlib " + str_AST.getText());
							#endif

			}
			importlib_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = importlib_AST;
	}

	public void cpp_quote() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST cpp_quote_AST = null;

		try {      // for error handling
			AST tmp42_AST = null;
			tmp42_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp42_AST);
			match(LITERAL_cpp_quote);
			AST tmp43_AST = null;
			tmp43_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp43_AST);
			match(LPAREN);
			string_literal();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp44_AST = null;
			tmp44_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp44_AST);
			match(RPAREN);
			cpp_quote_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_6_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = cpp_quote_AST;
	}

	public void midl_pragma_warning() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST midl_pragma_warning_AST = null;

		try {      // for error handling
			AST tmp45_AST = null;
			tmp45_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp45_AST);
			match(LITERAL_midl_pragma_warning);
			AST tmp46_AST = null;
			tmp46_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp46_AST);
			match(LPAREN);
			{    // ( ... )*
				for (;;)
				{
					if ((tokenSet_7_.member(LA(1))))
					{
						AST tmp47_AST = null;
						tmp47_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp47_AST);
						matchNot(RPAREN);
					}
					else
					{
						goto _loop249_breakloop;
					}

				}
_loop249_breakloop:				;
			}    // ( ... )*
			AST tmp48_AST = null;
			tmp48_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp48_AST);
			match(RPAREN);
			midl_pragma_warning_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_1_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = midl_pragma_warning_AST;
	}

	public void identifier() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST identifier_AST = null;

		try {      // for error handling
			AST tmp49_AST = null;
			tmp49_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp49_AST);
			match(IDENT);
			identifier_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_8_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = identifier_AST;
	}

	public void definition_list() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST definition_list_AST = null;

		try {      // for error handling
			{ // ( ... )+
				int _cnt14=0;
				for (;;)
				{
					if ((tokenSet_0_.member(LA(1))))
					{
						definition();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						if (_cnt14 >= 1) { goto _loop14_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt14++;
				}
_loop14_breakloop:				;
			}    // ( ... )+
			definition_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_9_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = definition_list_AST;
	}

	public void string_literal() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST string_literal_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LITERAL_L:
				{
					AST tmp50_AST = null;
					tmp50_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp50_AST);
					match(LITERAL_L);
					break;
				}
				case STRING_LITERAL:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			{ // ( ... )+
				int _cnt240=0;
				for (;;)
				{
					if ((LA(1)==STRING_LITERAL))
					{
						AST tmp51_AST = null;
						tmp51_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp51_AST);
						match(STRING_LITERAL);
					}
					else
					{
						if (_cnt240 >= 1) { goto _loop240_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt240++;
				}
_loop240_breakloop:				;
			}    // ( ... )+
			string_literal_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_10_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = string_literal_AST;
	}

	public void interf_declr(
		CodeTypeReferenceCollection baseTypes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST interf_declr_AST = null;
		AST name_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LITERAL_interface:
				{
					AST tmp52_AST = null;
					tmp52_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(ref currentAST, tmp52_AST);
					match(LITERAL_interface);
					break;
				}
				case LITERAL_dispinterface:
				{
					AST tmp53_AST = null;
					tmp53_AST = astFactory.create(LT(1));
					astFactory.makeASTRoot(ref currentAST, tmp53_AST);
					match(LITERAL_dispinterface);
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			identifier();
			if (0 == inputState.guessing)
			{
				name_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{

						baseTypes.Add(name_AST.getText());

			}
			interf_declr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_11_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = interf_declr_AST;
	}

	public void attribute(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST attribute_AST = null;
		AST uuid_AST = null;
		AST name_AST = null;
		AST value_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_uuid:
			{
				AST tmp54_AST = null;
				tmp54_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp54_AST);
				match(LITERAL_uuid);
				uuid_literal();
				if (0 == inputState.guessing)
				{
					uuid_AST = (AST)returnAST;
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				if (0==inputState.guessing)
				{
					attributes.Add("Guid", new CodeAttributeArgument(new CodePrimitiveExpression(uuid_AST.ToStringList().Replace(" ", ""))));
				}
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_version:
			{
				AST tmp55_AST = null;
				tmp55_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp55_AST);
				match(LITERAL_version);
				AST tmp56_AST = null;
				tmp56_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp56_AST);
				match(LPAREN);
				{ // ( ... )+
					int _cnt28=0;
					for (;;)
					{
						if ((tokenSet_7_.member(LA(1))))
						{
							AST tmp57_AST = null;
							tmp57_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp57_AST);
							matchNot(RPAREN);
						}
						else
						{
							if (_cnt28 >= 1) { goto _loop28_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
						}

						_cnt28++;
					}
_loop28_breakloop:					;
				}    // ( ... )+
				AST tmp58_AST = null;
				tmp58_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp58_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_async_uuid:
			{
				AST tmp59_AST = null;
				tmp59_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp59_AST);
				match(LITERAL_async_uuid);
				uuid_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_local:
			{
				AST tmp60_AST = null;
				tmp60_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp60_AST);
				match(LITERAL_local);
				if (0==inputState.guessing)
				{
					attributes["local"] = true;
				}
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_object:
			{
				AST tmp61_AST = null;
				tmp61_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp61_AST);
				match(LITERAL_object);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_pointer_default:
			{
				AST tmp62_AST = null;
				tmp62_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp62_AST);
				match(LITERAL_pointer_default);
				AST tmp63_AST = null;
				tmp63_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp63_AST);
				match(LPAREN);
				ptr_attr();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp64_AST = null;
				tmp64_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp64_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_endpoint:
			{
				AST tmp65_AST = null;
				tmp65_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp65_AST);
				match(LITERAL_endpoint);
				AST tmp66_AST = null;
				tmp66_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp66_AST);
				match(LPAREN);
				string_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				{    // ( ... )*
					for (;;)
					{
						if ((LA(1)==COMMA))
						{
							AST tmp67_AST = null;
							tmp67_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp67_AST);
							match(COMMA);
							string_literal();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
						}
						else
						{
							goto _loop30_breakloop;
						}

					}
_loop30_breakloop:					;
				}    // ( ... )*
				AST tmp68_AST = null;
				tmp68_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp68_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_odl:
			{
				AST tmp69_AST = null;
				tmp69_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp69_AST);
				match(LITERAL_odl);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_optimize:
			{
				AST tmp70_AST = null;
				tmp70_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp70_AST);
				match(LITERAL_optimize);
				AST tmp71_AST = null;
				tmp71_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp71_AST);
				match(LPAREN);
				string_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp72_AST = null;
				tmp72_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp72_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_proxy:
			{
				AST tmp73_AST = null;
				tmp73_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp73_AST);
				match(LITERAL_proxy);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_aggregatable:
			{
				AST tmp74_AST = null;
				tmp74_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp74_AST);
				match(LITERAL_aggregatable);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_appobject:
			{
				AST tmp75_AST = null;
				tmp75_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp75_AST);
				match(LITERAL_appobject);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_bindable:
			{
				AST tmp76_AST = null;
				tmp76_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp76_AST);
				match(LITERAL_bindable);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_control:
			{
				AST tmp77_AST = null;
				tmp77_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp77_AST);
				match(LITERAL_control);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_custom:
			{
				AST tmp78_AST = null;
				tmp78_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp78_AST);
				match(LITERAL_custom);
				AST tmp79_AST = null;
				tmp79_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp79_AST);
				match(LPAREN);
				string_literal();
				if (0 == inputState.guessing)
				{
					name_AST = (AST)returnAST;
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp80_AST = null;
				tmp80_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp80_AST);
				match(COMMA);
				non_rparen();
				if (0 == inputState.guessing)
				{
					value_AST = (AST)returnAST;
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp81_AST = null;
				tmp81_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp81_AST);
				match(RPAREN);
				if (0==inputState.guessing)
				{

								attributes.Add("custom", new CodeAttributeArgument(name_AST.getText(),
									new CodePrimitiveExpression(value_AST.getText())));

				}
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_default:
			{
				AST tmp82_AST = null;
				tmp82_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp82_AST);
				match(LITERAL_default);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_defaultbind:
			{
				AST tmp83_AST = null;
				tmp83_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp83_AST);
				match(LITERAL_defaultbind);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_defaultcollelem:
			{
				AST tmp84_AST = null;
				tmp84_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp84_AST);
				match(LITERAL_defaultcollelem);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_defaultvtable:
			{
				AST tmp85_AST = null;
				tmp85_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp85_AST);
				match(LITERAL_defaultvtable);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_displaybind:
			{
				AST tmp86_AST = null;
				tmp86_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp86_AST);
				match(LITERAL_displaybind);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_dllname:
			{
				AST tmp87_AST = null;
				tmp87_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp87_AST);
				match(LITERAL_dllname);
				AST tmp88_AST = null;
				tmp88_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp88_AST);
				match(LPAREN);
				string_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp89_AST = null;
				tmp89_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp89_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_dual:
			{
				AST tmp90_AST = null;
				tmp90_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp90_AST);
				match(LITERAL_dual);
				if (0==inputState.guessing)
				{
					attributes.Add("dual", new CodeAttributeArgument());
				}
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_entry:
			{
				AST tmp91_AST = null;
				tmp91_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp91_AST);
				match(LITERAL_entry);
				AST tmp92_AST = null;
				tmp92_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp92_AST);
				match(LPAREN);
				{ // ( ... )+
					int _cnt32=0;
					for (;;)
					{
						if ((tokenSet_7_.member(LA(1))))
						{
							AST tmp93_AST = null;
							tmp93_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp93_AST);
							matchNot(RPAREN);
						}
						else
						{
							if (_cnt32 >= 1) { goto _loop32_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
						}

						_cnt32++;
					}
_loop32_breakloop:					;
				}    // ( ... )+
				AST tmp94_AST = null;
				tmp94_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp94_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_helpcontext:
			{
				AST tmp95_AST = null;
				tmp95_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp95_AST);
				match(LITERAL_helpcontext);
				AST tmp96_AST = null;
				tmp96_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp96_AST);
				match(LPAREN);
				integer_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp97_AST = null;
				tmp97_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp97_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_helpfile:
			{
				AST tmp98_AST = null;
				tmp98_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp98_AST);
				match(LITERAL_helpfile);
				AST tmp99_AST = null;
				tmp99_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp99_AST);
				match(LPAREN);
				string_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp100_AST = null;
				tmp100_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp100_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_helpstring:
			{
				AST tmp101_AST = null;
				tmp101_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp101_AST);
				match(LITERAL_helpstring);
				AST tmp102_AST = null;
				tmp102_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp102_AST);
				match(LPAREN);
				string_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp103_AST = null;
				tmp103_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp103_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_helpstringdll:
			{
				AST tmp104_AST = null;
				tmp104_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp104_AST);
				match(LITERAL_helpstringdll);
				AST tmp105_AST = null;
				tmp105_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp105_AST);
				match(LPAREN);
				string_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp106_AST = null;
				tmp106_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp106_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_hidden:
			{
				AST tmp107_AST = null;
				tmp107_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp107_AST);
				match(LITERAL_hidden);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_id:
			{
				AST tmp108_AST = null;
				tmp108_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp108_AST);
				match(LITERAL_id);
				AST tmp109_AST = null;
				tmp109_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp109_AST);
				match(LPAREN);
				{
					switch ( LA(1) )
					{
					case INT:
					case HEX:
					case OCTAL:
					{
						integer_literal();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case IDENT:
					{
						identifier();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				AST tmp110_AST = null;
				tmp110_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp110_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_idempotent:
			{
				AST tmp111_AST = null;
				tmp111_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp111_AST);
				match(LITERAL_idempotent);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_immediatebind:
			{
				AST tmp112_AST = null;
				tmp112_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp112_AST);
				match(LITERAL_immediatebind);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_lcid:
			{
				AST tmp113_AST = null;
				tmp113_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp113_AST);
				match(LITERAL_lcid);
				AST tmp114_AST = null;
				tmp114_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp114_AST);
				match(LPAREN);
				integer_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp115_AST = null;
				tmp115_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp115_AST);
				match(RPAREN);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_licensed:
			{
				AST tmp116_AST = null;
				tmp116_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp116_AST);
				match(LITERAL_licensed);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_message:
			{
				AST tmp117_AST = null;
				tmp117_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp117_AST);
				match(LITERAL_message);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_nonbrowsable:
			{
				AST tmp118_AST = null;
				tmp118_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp118_AST);
				match(LITERAL_nonbrowsable);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_noncreatable:
			{
				AST tmp119_AST = null;
				tmp119_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp119_AST);
				match(LITERAL_noncreatable);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_nonextensible:
			{
				AST tmp120_AST = null;
				tmp120_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp120_AST);
				match(LITERAL_nonextensible);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_oleautomation:
			{
				AST tmp121_AST = null;
				tmp121_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp121_AST);
				match(LITERAL_oleautomation);
				attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_restricted:
			{
				AST tmp122_AST = null;
				tmp122_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp122_AST);
				match(LITERAL_restricted);
				if (0==inputState.guessing)
				{
					attributes.Add("restricted", new CodeAttributeArgument());
				}
				attribute_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_12_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = attribute_AST;
	}

	public void uuid_literal() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST uuid_literal_AST = null;
		AST value_AST = null;

		try {      // for error handling
			match(LPAREN);
			non_rparen();
			if (0 == inputState.guessing)
			{
				value_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			match(RPAREN);
			uuid_literal_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_12_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = uuid_literal_AST;
	}

	public void ptr_attr() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST ptr_attr_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_ref:
			{
				AST tmp125_AST = null;
				tmp125_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp125_AST);
				match(LITERAL_ref);
				ptr_attr_AST = currentAST.root;
				break;
			}
			case LITERAL_unique:
			{
				AST tmp126_AST = null;
				tmp126_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp126_AST);
				match(LITERAL_unique);
				ptr_attr_AST = currentAST.root;
				break;
			}
			case LITERAL_ptr:
			{
				AST tmp127_AST = null;
				tmp127_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp127_AST);
				match(LITERAL_ptr);
				ptr_attr_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_13_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = ptr_attr_AST;
	}

	public void non_rparen() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST non_rparen_AST = null;

		try {      // for error handling
			{ // ( ... )+
				int _cnt36=0;
				for (;;)
				{
					if ((tokenSet_7_.member(LA(1))))
					{
						AST tmp128_AST = null;
						tmp128_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp128_AST);
						matchNot(RPAREN);
					}
					else
					{
						if (_cnt36 >= 1) { goto _loop36_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt36++;
				}
_loop36_breakloop:				;
			}    // ( ... )+
			non_rparen_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_14_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = non_rparen_AST;
	}

	public void integer_literal() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST integer_literal_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case INT:
			{
				AST tmp129_AST = null;
				tmp129_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp129_AST);
				match(INT);
				integer_literal_AST = currentAST.root;
				break;
			}
			case OCTAL:
			{
				AST tmp130_AST = null;
				tmp130_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp130_AST);
				match(OCTAL);
				integer_literal_AST = currentAST.root;
				break;
			}
			case HEX:
			{
				AST tmp131_AST = null;
				tmp131_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp131_AST);
				match(HEX);
				integer_literal_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_15_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = integer_literal_AST;
	}

	public void lib_definition() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST lib_definition_AST = null;

				Hashtable attributes = new Hashtable();
				CodeTypeMember ignored;


		try {      // for error handling
			switch ( LA(1) )
			{
			case LBRACKET:
			case LITERAL_interface:
			case LITERAL_dispinterface:
			{
				{
					switch ( LA(1) )
					{
					case LBRACKET:
					{
						AST tmp132_AST = null;
						tmp132_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp132_AST);
						match(LBRACKET);
						attribute_list(attributes);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						AST tmp133_AST = null;
						tmp133_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp133_AST);
						match(RBRACKET);
						break;
					}
					case LITERAL_interface:
					case LITERAL_dispinterface:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				ignored=interf();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				lib_definition_AST = currentAST.root;
				break;
			}
			case SEMI:
			case LITERAL_typedef:
			case LITERAL_native:
			case LITERAL_struct:
			case LITERAL_union:
			case LITERAL_enum:
			{
				ignored=type_dcl();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				match(SEMI);
				lib_definition_AST = currentAST.root;
				break;
			}
			case LITERAL_const:
			{
				const_dcl();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				match(SEMI);
				lib_definition_AST = currentAST.root;
				break;
			}
			case LITERAL_importlib:
			{
				importlib();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				match(SEMI);
				lib_definition_AST = currentAST.root;
				break;
			}
			case LITERAL_import:
			{
				import();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				match(SEMI);
				lib_definition_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_16_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = lib_definition_AST;
	}

	public StringCollection  inheritance_spec() //throws RecognitionException, TokenStreamException
{
		StringCollection coll;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST inheritance_spec_AST = null;
		coll = new StringCollection();

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case COLON:
				{
					AST tmp138_AST = null;
					tmp138_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp138_AST);
					match(COLON);
					scoped_name_list(coll);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case EOF:
				case SEMI:
				case LBRACKET:
				case LITERAL_module:
				case LBRACE:
				case RBRACE:
				case LITERAL_import:
				case LITERAL_library:
				case LITERAL_coclass:
				case LITERAL_importlib:
				case LITERAL_interface:
				case LITERAL_dispinterface:
				case LITERAL_const:
				case LITERAL_typedef:
				case LITERAL_native:
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				case LITERAL_exception:
				case LITERAL_cpp_quote:
				case LITERAL_midl_pragma_warning:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			inheritance_spec_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_17_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = inheritance_spec_AST;
		return coll;
	}

	public bool  interface_body(
		CodeTypeDeclaration type
	) //throws RecognitionException, TokenStreamException
{
		bool fForwardDeclaration;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST interface_body_AST = null;

				CodeTypeMember member = null;
				CodeTypeMember ignored;
				fForwardDeclaration = false;


		try {      // for error handling
			switch ( LA(1) )
			{
			case SEMI:
			case LITERAL_typedef:
			case LITERAL_native:
			case LITERAL_struct:
			case LITERAL_union:
			case LITERAL_enum:
			{
				ignored=type_dcl();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp139_AST = null;
				tmp139_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp139_AST);
				match(SEMI);
				interface_body_AST = currentAST.root;
				break;
			}
			case LITERAL_const:
			{
				const_dcl();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp140_AST = null;
				tmp140_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp140_AST);
				match(SEMI);
				interface_body_AST = currentAST.root;
				break;
			}
			case LITERAL_exception:
			{
				except_dcl();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp141_AST = null;
				tmp141_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp141_AST);
				match(SEMI);
				interface_body_AST = currentAST.root;
				break;
			}
			case LITERAL_readonly:
			case LITERAL_attribute:
			{
				attr_dcl();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp142_AST = null;
				tmp142_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp142_AST);
				match(SEMI);
				interface_body_AST = currentAST.root;
				break;
			}
			case LITERAL_cpp_quote:
			{
				cpp_quote();
				interface_body_AST = currentAST.root;
				break;
			}
			case INT3264:
			case INT64:
			case LBRACKET:
			case SCOPEOP:
			case LITERAL_signed:
			case LITERAL_unsigned:
			case LITERAL_octet:
			case LITERAL_any:
			case LITERAL_void:
			case LITERAL_byte:
			case LITERAL_wchar_t:
			case LITERAL_handle_t:
			case LITERAL_small:
			case LITERAL_short:
			case LITERAL_long:
			case LITERAL_int:
			case LITERAL_hyper:
			case LITERAL_char:
			case LITERAL_float:
			case LITERAL_double:
			case LITERAL_boolean:
			case LITERAL_string:
			case LITERAL_SAFEARRAY:
			case IDENT:
			{
				member=function_dcl(type.Members);
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp143_AST = null;
				tmp143_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp143_AST);
				match(SEMI);
				if (0==inputState.guessing)
				{

								if (member != null)
									type.Members.Add(member);

				}
				interface_body_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_18_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = interface_body_AST;
		return fForwardDeclaration;
	}

	public void attr_dcl() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST attr_dcl_AST = null;

				string ignored;
				Hashtable attributes = new Hashtable();


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LITERAL_readonly:
				{
					AST tmp144_AST = null;
					tmp144_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp144_AST);
					match(LITERAL_readonly);
					break;
				}
				case LITERAL_attribute:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp145_AST = null;
			tmp145_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp145_AST);
			match(LITERAL_attribute);
			param_type_spec();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			ignored=declarator_list(attributes);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			attr_dcl_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = attr_dcl_AST;
	}

	public CodeTypeMember  function_dcl(
		CodeTypeMemberCollection types
	) //throws RecognitionException, TokenStreamException
{
		CodeTypeMember memberRet;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST function_dcl_AST = null;
		AST rt_AST = null;
		AST name_AST = null;

				CodeParameterDeclarationExpressionCollection pars;
				CodeMemberMethod member = new CodeMemberMethod();
				Hashtable funcAttributes = new Hashtable();
				memberRet = member;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LBRACKET:
				{
					function_attribute_list(funcAttributes);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case INT3264:
				case INT64:
				case SCOPEOP:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_string:
				case LITERAL_SAFEARRAY:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			param_type_spec();
			if (0 == inputState.guessing)
			{
				rt_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{
				switch ( LA(1) )
				{
				case LITERAL_const:
				{
					AST tmp146_AST = null;
					tmp146_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp146_AST);
					match(LITERAL_const);
					break;
				}
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			identifier();
			if (0 == inputState.guessing)
			{
				name_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			pars=parameter_dcls();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{
				switch ( LA(1) )
				{
				case LITERAL_const:
				{
					AST tmp147_AST = null;
					tmp147_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp147_AST);
					match(LITERAL_const);
					break;
				}
				case SEMI:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			if (0==inputState.guessing)
			{

							member.Name = name_AST.getText();
							member.Parameters.AddRange(pars);

							if (rt_AST == null)
								memberRet = null;
							else
							{
								CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression();
								Hashtable attributes = new Hashtable();
								param.Type = m_Conv.ConvertParamType(rt_AST.ToString(), param, attributes);

								m_Conv.HandleSizeIs(member, funcAttributes);
								memberRet = m_Conv.HandleFunction_dcl(member, param.Type, types, funcAttributes);
							}

			}
			function_dcl_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = function_dcl_AST;
		return memberRet;
	}

	public void scoped_name_list(
		StringCollection coll
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST scoped_name_list_AST = null;
		AST name1_AST = null;
		AST name2_AST = null;

		try {      // for error handling
			scoped_name();
			if (0 == inputState.guessing)
			{
				name1_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{
				coll.Add(name1_AST.getText());
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp148_AST = null;
						tmp148_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp148_AST);
						match(COMMA);
						scoped_name();
						if (0 == inputState.guessing)
						{
							name2_AST = (AST)returnAST;
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{
							coll.Add(name2_AST.getText());
						}
					}
					else
					{
						goto _loop52_breakloop;
					}

				}
_loop52_breakloop:				;
			}    // ( ... )*
			scoped_name_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_19_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = scoped_name_list_AST;
	}

	public void scoped_name() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST scoped_name_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case SCOPEOP:
				{
					AST tmp149_AST = null;
					tmp149_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp149_AST);
					match(SCOPEOP);
					break;
				}
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			identifier();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==SCOPEOP))
					{
						AST tmp150_AST = null;
						tmp150_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp150_AST);
						match(SCOPEOP);
						identifier();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop56_breakloop;
					}

				}
_loop56_breakloop:				;
			}    // ( ... )*
			scoped_name_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_20_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = scoped_name_AST;
	}

	public void const_type() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST const_type_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case INT3264:
			case INT64:
			case LITERAL_signed:
			case LITERAL_unsigned:
			case LITERAL_octet:
			case LITERAL_any:
			case LITERAL_void:
			case LITERAL_byte:
			case LITERAL_wchar_t:
			case LITERAL_handle_t:
			case LITERAL_small:
			case LITERAL_short:
			case LITERAL_long:
			case LITERAL_int:
			case LITERAL_hyper:
			case LITERAL_char:
			case LITERAL_float:
			case LITERAL_double:
			case LITERAL_boolean:
			{
				base_type_spec();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				const_type_AST = currentAST.root;
				break;
			}
			case LITERAL_string:
			{
				string_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				const_type_AST = currentAST.root;
				break;
			}
			case SCOPEOP:
			case IDENT:
			{
				scoped_name();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				{    // ( ... )*
					for (;;)
					{
						if ((LA(1)==STAR))
						{
							AST tmp151_AST = null;
							tmp151_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp151_AST);
							match(STAR);
						}
						else
						{
							goto _loop60_breakloop;
						}

					}
_loop60_breakloop:					;
				}    // ( ... )*
				const_type_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_21_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = const_type_AST;
	}

	public string  const_exp() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST const_exp_AST = null;
		s = string.Empty;

		try {      // for error handling
			s=or_expr();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			const_exp_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_22_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = const_exp_AST;
		return s;
	}

	public void base_type_spec() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST base_type_spec_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case INT3264:
			case INT64:
			case LITERAL_signed:
			case LITERAL_unsigned:
			case LITERAL_octet:
			case LITERAL_any:
			case LITERAL_void:
			case LITERAL_byte:
			case LITERAL_wchar_t:
			case LITERAL_small:
			case LITERAL_short:
			case LITERAL_long:
			case LITERAL_int:
			case LITERAL_hyper:
			case LITERAL_char:
			case LITERAL_float:
			case LITERAL_double:
			case LITERAL_boolean:
			{
				{
					switch ( LA(1) )
					{
					case INT3264:
					case INT64:
					case LITERAL_signed:
					case LITERAL_unsigned:
					case LITERAL_small:
					case LITERAL_short:
					case LITERAL_long:
					case LITERAL_int:
					case LITERAL_hyper:
					case LITERAL_char:
					{
						{
							switch ( LA(1) )
							{
							case LITERAL_signed:
							{
								AST tmp152_AST = null;
								tmp152_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp152_AST);
								match(LITERAL_signed);
								break;
							}
							case LITERAL_unsigned:
							{
								AST tmp153_AST = null;
								tmp153_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp153_AST);
								match(LITERAL_unsigned);
								break;
							}
							case INT3264:
							case INT64:
							case LITERAL_small:
							case LITERAL_short:
							case LITERAL_long:
							case LITERAL_int:
							case LITERAL_hyper:
							case LITERAL_char:
							{
								break;
							}
							default:
							{
								throw new NoViableAltException(LT(1), getFilename());
							}
							 }
						}
						{
							switch ( LA(1) )
							{
							case INT3264:
							case INT64:
							case LITERAL_small:
							case LITERAL_short:
							case LITERAL_long:
							case LITERAL_int:
							case LITERAL_hyper:
							{
								integer_type();
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(ref currentAST, returnAST);
								}
								break;
							}
							case LITERAL_char:
							{
								char_type();
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(ref currentAST, returnAST);
								}
								break;
							}
							default:
							{
								throw new NoViableAltException(LT(1), getFilename());
							}
							 }
						}
						break;
					}
					case LITERAL_boolean:
					{
						boolean_type();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case LITERAL_float:
					case LITERAL_double:
					{
						floating_pt_type();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case LITERAL_octet:
					{
						AST tmp154_AST = null;
						tmp154_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp154_AST);
						match(LITERAL_octet);
						break;
					}
					case LITERAL_any:
					{
						AST tmp155_AST = null;
						tmp155_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp155_AST);
						match(LITERAL_any);
						break;
					}
					case LITERAL_void:
					{
						AST tmp156_AST = null;
						tmp156_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp156_AST);
						match(LITERAL_void);
						break;
					}
					case LITERAL_byte:
					{
						AST tmp157_AST = null;
						tmp157_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp157_AST);
						match(LITERAL_byte);
						break;
					}
					case LITERAL_wchar_t:
					{
						AST tmp158_AST = null;
						tmp158_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp158_AST);
						match(LITERAL_wchar_t);
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				{    // ( ... )*
					for (;;)
					{
						if ((LA(1)==STAR))
						{
							AST tmp159_AST = null;
							tmp159_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp159_AST);
							match(STAR);
						}
						else
						{
							goto _loop110_breakloop;
						}

					}
_loop110_breakloop:					;
				}    // ( ... )*
				base_type_spec_AST = currentAST.root;
				break;
			}
			case LITERAL_handle_t:
			{
				AST tmp160_AST = null;
				tmp160_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp160_AST);
				match(LITERAL_handle_t);
				base_type_spec_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_23_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = base_type_spec_AST;
	}

	public void string_type() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST string_type_AST = null;

		try {      // for error handling
			AST tmp161_AST = null;
			tmp161_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp161_AST);
			match(LITERAL_string);
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==STAR))
					{
						AST tmp162_AST = null;
						tmp162_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp162_AST);
						match(STAR);
					}
					else
					{
						goto _loop185_breakloop;
					}

				}
_loop185_breakloop:				;
			}    // ( ... )*
			string_type_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_24_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = string_type_AST;
	}

	public string  or_expr() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST or_expr_AST = null;
		IToken  op = null;
		AST op_AST = null;

				StringBuilder bldr = new StringBuilder();
				string expr = string.Empty;
				s = string.Empty;


		try {      // for error handling
			expr=xor_expr();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{
				bldr.Append(expr);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==OR))
					{
						op = LT(1);
						op_AST = astFactory.create(op);
						astFactory.addASTChild(ref currentAST, op_AST);
						match(OR);
						expr=xor_expr();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{

											bldr.Append(op_AST.getText());
											bldr.Append(expr);

						}
					}
					else
					{
						goto _loop64_breakloop;
					}

				}
_loop64_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{
				s = bldr.ToString();
			}
			or_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_22_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = or_expr_AST;
		return s;
	}

	public string  xor_expr() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST xor_expr_AST = null;
		IToken  op = null;
		AST op_AST = null;

				StringBuilder bldr = new StringBuilder();
				string expr = string.Empty;
				s = string.Empty;


		try {      // for error handling
			expr=and_expr();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{
				bldr.Append(expr);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==XOR))
					{
						op = LT(1);
						op_AST = astFactory.create(op);
						astFactory.addASTChild(ref currentAST, op_AST);
						match(XOR);
						expr=and_expr();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{

											bldr.Append(op_AST.getText());
											bldr.Append(expr);

						}
					}
					else
					{
						goto _loop67_breakloop;
					}

				}
_loop67_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{
				s = bldr.ToString();
			}
			xor_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_25_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = xor_expr_AST;
		return s;
	}

	public string  and_expr() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST and_expr_AST = null;
		IToken  op = null;
		AST op_AST = null;

				StringBuilder bldr = new StringBuilder();
				string expr = string.Empty;
				s = string.Empty;


		try {      // for error handling
			expr=shift_expr();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{
				bldr.Append(expr);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==AND))
					{
						op = LT(1);
						op_AST = astFactory.create(op);
						astFactory.addASTChild(ref currentAST, op_AST);
						match(AND);
						expr=shift_expr();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{

											bldr.Append(op_AST.getText());
											bldr.Append(expr);

						}
					}
					else
					{
						goto _loop70_breakloop;
					}

				}
_loop70_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{
				s = bldr.ToString();
			}
			and_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_26_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = and_expr_AST;
		return s;
	}

	public string  shift_expr() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST shift_expr_AST = null;
		AST op_AST = null;

				StringBuilder bldr = new StringBuilder();
				string expr = string.Empty;
				s = string.Empty;


		try {      // for error handling
			expr=add_expr();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{
				bldr.Append(expr);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==LSHIFT||LA(1)==RSHIFT))
					{
						shift_op();
						if (0 == inputState.guessing)
						{
							op_AST = (AST)returnAST;
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						expr=add_expr();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{

											bldr.Append(op_AST.getText());
											bldr.Append(expr);

						}
					}
					else
					{
						goto _loop73_breakloop;
					}

				}
_loop73_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{
				s = bldr.ToString();
			}
			shift_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_27_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = shift_expr_AST;
		return s;
	}

	public string  add_expr() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST add_expr_AST = null;
		AST op_AST = null;

				StringBuilder bldr = new StringBuilder();
				string expr = string.Empty;
				s = string.Empty;


		try {      // for error handling
			expr=mult_expr();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{
				bldr.Append(expr);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==PLUS||LA(1)==MINUS))
					{
						add_op();
						if (0 == inputState.guessing)
						{
							op_AST = (AST)returnAST;
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						expr=mult_expr();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{

											bldr.Append(op_AST.getText());
											bldr.Append(expr);

						}
					}
					else
					{
						goto _loop77_breakloop;
					}

				}
_loop77_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{
				s = bldr.ToString();
			}
			add_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_28_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = add_expr_AST;
		return s;
	}

	public void shift_op() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST shift_op_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LSHIFT:
			{
				AST tmp163_AST = null;
				tmp163_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp163_AST);
				match(LSHIFT);
				shift_op_AST = currentAST.root;
				break;
			}
			case RSHIFT:
			{
				AST tmp164_AST = null;
				tmp164_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp164_AST);
				match(RSHIFT);
				shift_op_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_29_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = shift_op_AST;
	}

	public string  mult_expr() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST mult_expr_AST = null;
		AST op_AST = null;

				StringBuilder bldr = new StringBuilder();
				string expr = string.Empty;
				s = string.Empty;


		try {      // for error handling
			expr=unary_expr();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{
				bldr.Append(expr);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==STAR||LA(1)==DIV||LA(1)==MOD))
					{
						mult_op();
						if (0 == inputState.guessing)
						{
							op_AST = (AST)returnAST;
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						expr=unary_expr();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{

											bldr.Append(op_AST.getText());
											bldr.Append(expr);

						}
					}
					else
					{
						goto _loop81_breakloop;
					}

				}
_loop81_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{
				s = bldr.ToString();
			}
			mult_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_30_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = mult_expr_AST;
		return s;
	}

	public void add_op() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST add_op_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case PLUS:
			{
				AST tmp165_AST = null;
				tmp165_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp165_AST);
				match(PLUS);
				add_op_AST = currentAST.root;
				break;
			}
			case MINUS:
			{
				AST tmp166_AST = null;
				tmp166_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp166_AST);
				match(MINUS);
				add_op_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_29_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = add_op_AST;
	}

	public string  unary_expr() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST unary_expr_AST = null;
		AST u_AST = null;

				string p = string.Empty;
				s = string.Empty;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case PLUS:
				case MINUS:
				case TILDE:
				{
					unary_operator();
					if (0 == inputState.guessing)
					{
						u_AST = (AST)returnAST;
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case LPAREN:
				case SCOPEOP:
				case LITERAL_TRUE:
				case LITERAL_true:
				case LITERAL_FALSE:
				case LITERAL_false:
				case INT:
				case HEX:
				case OCTAL:
				case LITERAL_L:
				case STRING_LITERAL:
				case CHAR_LITERAL:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			p=primary_expr();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{

							if (u_AST != null)
								s = u_AST.getText() + p;
							else
								s = p;

			}
			unary_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_10_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = unary_expr_AST;
		return s;
	}

	public void mult_op() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST mult_op_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case STAR:
			{
				AST tmp167_AST = null;
				tmp167_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp167_AST);
				match(STAR);
				mult_op_AST = currentAST.root;
				break;
			}
			case DIV:
			{
				AST tmp168_AST = null;
				tmp168_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp168_AST);
				match(DIV);
				mult_op_AST = currentAST.root;
				break;
			}
			case MOD:
			{
				AST tmp169_AST = null;
				tmp169_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp169_AST);
				match(MOD);
				mult_op_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_29_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = mult_op_AST;
	}

	public void unary_operator() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST unary_operator_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case MINUS:
			{
				AST tmp170_AST = null;
				tmp170_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp170_AST);
				match(MINUS);
				unary_operator_AST = currentAST.root;
				break;
			}
			case PLUS:
			{
				AST tmp171_AST = null;
				tmp171_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp171_AST);
				match(PLUS);
				unary_operator_AST = currentAST.root;
				break;
			}
			case TILDE:
			{
				AST tmp172_AST = null;
				tmp172_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp172_AST);
				match(TILDE);
				unary_operator_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_31_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = unary_operator_AST;
	}

	public string  primary_expr() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST primary_expr_AST = null;
		AST sn_AST = null;
		AST l_AST = null;

				string c;
				s = string.Empty;


		try {      // for error handling
			switch ( LA(1) )
			{
			case SCOPEOP:
			case IDENT:
			{
				scoped_name();
				if (0 == inputState.guessing)
				{
					sn_AST = (AST)returnAST;
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				if (0==inputState.guessing)
				{
					s = sn_AST.getText();
				}
				primary_expr_AST = currentAST.root;
				break;
			}
			case LITERAL_TRUE:
			case LITERAL_true:
			case LITERAL_FALSE:
			case LITERAL_false:
			case INT:
			case HEX:
			case OCTAL:
			case LITERAL_L:
			case STRING_LITERAL:
			case CHAR_LITERAL:
			{
				literal();
				if (0 == inputState.guessing)
				{
					l_AST = (AST)returnAST;
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				if (0==inputState.guessing)
				{
					s = l_AST.getText();
				}
				primary_expr_AST = currentAST.root;
				break;
			}
			case LPAREN:
			{
				AST tmp173_AST = null;
				tmp173_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp173_AST);
				match(LPAREN);
				c=const_exp();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp174_AST = null;
				tmp174_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp174_AST);
				match(RPAREN);
				if (0==inputState.guessing)
				{
					s = "(" + c + ")";
				}
				primary_expr_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_10_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = primary_expr_AST;
		return s;
	}

	public void literal() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST literal_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case INT:
			case HEX:
			case OCTAL:
			{
				floating_pt_or_integer_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				literal_AST = currentAST.root;
				break;
			}
			case LITERAL_TRUE:
			case LITERAL_true:
			case LITERAL_FALSE:
			case LITERAL_false:
			{
				boolean_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				literal_AST = currentAST.root;
				break;
			}
			default:
				bool synPredMatched89 = false;
				if (((LA(1)==LITERAL_L||LA(1)==STRING_LITERAL)))
				{
					int _m89 = mark();
					synPredMatched89 = true;
					inputState.guessing++;
					try {
						{
							string_literal();
						}
					}
					catch (RecognitionException)
					{
						synPredMatched89 = false;
					}
					rewind(_m89);
					inputState.guessing--;
				}
				if ( synPredMatched89 )
				{
					string_literal();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					literal_AST = currentAST.root;
				}
				else if ((LA(1)==LITERAL_L||LA(1)==CHAR_LITERAL)) {
					character_literal();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					literal_AST = currentAST.root;
				}
			else
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			break; }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_10_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = literal_AST;
	}

	public void character_literal() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST character_literal_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LITERAL_L:
				{
					AST tmp175_AST = null;
					tmp175_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp175_AST);
					match(LITERAL_L);
					break;
				}
				case CHAR_LITERAL:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp176_AST = null;
			tmp176_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp176_AST);
			match(CHAR_LITERAL);
			character_literal_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_10_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = character_literal_AST;
	}

	public void floating_pt_or_integer_literal() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST floating_pt_or_integer_literal_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case INT:
			{
				AST tmp177_AST = null;
				tmp177_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp177_AST);
				match(INT);
				{
					switch ( LA(1) )
					{
					case FLOAT:
					{
						AST tmp178_AST = null;
						tmp178_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp178_AST);
						match(FLOAT);
						break;
					}
					case SEMI:
					case RBRACKET:
					case RBRACE:
					case COMMA:
					case RPAREN:
					case COLON:
					case STAR:
					case OR:
					case XOR:
					case AND:
					case LSHIFT:
					case RSHIFT:
					case PLUS:
					case MINUS:
					case DIV:
					case MOD:
					case GT:
					case RANGE:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				floating_pt_or_integer_literal_AST = currentAST.root;
				break;
			}
			case OCTAL:
			{
				AST tmp179_AST = null;
				tmp179_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp179_AST);
				match(OCTAL);
				floating_pt_or_integer_literal_AST = currentAST.root;
				break;
			}
			case HEX:
			{
				AST tmp180_AST = null;
				tmp180_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp180_AST);
				match(HEX);
				floating_pt_or_integer_literal_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_10_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = floating_pt_or_integer_literal_AST;
	}

	public void boolean_literal() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST boolean_literal_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_TRUE:
			{
				AST tmp181_AST = null;
				tmp181_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp181_AST);
				match(LITERAL_TRUE);
				boolean_literal_AST = currentAST.root;
				break;
			}
			case LITERAL_true:
			{
				AST tmp182_AST = null;
				tmp182_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp182_AST);
				match(LITERAL_true);
				boolean_literal_AST = currentAST.root;
				break;
			}
			case LITERAL_FALSE:
			{
				AST tmp183_AST = null;
				tmp183_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp183_AST);
				match(LITERAL_FALSE);
				boolean_literal_AST = currentAST.root;
				break;
			}
			case LITERAL_false:
			{
				AST tmp184_AST = null;
				tmp184_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp184_AST);
				match(LITERAL_false);
				boolean_literal_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_10_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = boolean_literal_AST;
	}

	public void positive_int_const() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST positive_int_const_AST = null;
		string s;

		try {      // for error handling
			s=const_exp();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			positive_int_const_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_32_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = positive_int_const_AST;
	}

	public void type_attributes() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST type_attributes_AST = null;

		try {      // for error handling
			type_attribute();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp185_AST = null;
						tmp185_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp185_AST);
						match(COMMA);
						type_attribute();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop96_breakloop;
					}

				}
_loop96_breakloop:				;
			}    // ( ... )*
			type_attributes_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_4_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = type_attributes_AST;
	}

	public CodeTypeMember  type_declarator() //throws RecognitionException, TokenStreamException
{
		CodeTypeMember type;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST type_declarator_AST = null;

				type = null;
				string name;
				Hashtable attributes = new Hashtable();


		try {      // for error handling
			type=type_spec();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			name=declarator_list(attributes);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			if (0==inputState.guessing)
			{

						#if DEBUG_IDLGRAMMAR
							System.Diagnostics.Debug.WriteLine(string.Format("typespec: {0}; {1}", type != null ? type.Name : "<empty>", name));
						#endif
							if (type.Name == string.Empty)
								type.Name = name;

			}
			type_declarator_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = type_declarator_AST;
		return type;
	}

	public CodeTypeDeclaration  struct_type() //throws RecognitionException, TokenStreamException
{
		CodeTypeDeclaration type;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST struct_type_AST = null;
		AST name_AST = null;

				type = new CodeTypeDeclaration();
				type.IsStruct = true;
				// Add attribute: [StructLayout(LayoutKind.Sequential, Pack=4)]
				type.CustomAttributes.Add(new CodeAttributeDeclaration("StructLayout",
					new CodeAttributeArgument(new CodeVariableReferenceExpression("LayoutKind.Sequential")),
					new CodeAttributeArgument("Pack", new CodePrimitiveExpression(4))));


		try {      // for error handling
			AST tmp186_AST = null;
			tmp186_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp186_AST);
			match(LITERAL_struct);
			{
				switch ( LA(1) )
				{
				case IDENT:
				{
					identifier();
					if (0 == inputState.guessing)
					{
						name_AST = (AST)returnAST;
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case LBRACE:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp187_AST = null;
			tmp187_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp187_AST);
			match(LBRACE);
			{ // ( ... )+
				int _cnt138=0;
				for (;;)
				{
					if ((tokenSet_3_.member(LA(1))))
					{
						member(type.Members);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						if (_cnt138 >= 1) { goto _loop138_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt138++;
				}
_loop138_breakloop:				;
			}    // ( ... )+
			AST tmp188_AST = null;
			tmp188_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp188_AST);
			match(RBRACE);
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==STAR))
					{
						AST tmp189_AST = null;
						tmp189_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp189_AST);
						match(STAR);
					}
					else
					{
						goto _loop140_breakloop;
					}

				}
_loop140_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{

							if (name_AST != null)
							{
								#if DEBUG_IDLGRAMMAR
								System.Console.WriteLine(string.Format("struct {0}", name_AST.getText()));
								#endif
								type.Name = name_AST.getText();
							}

			}
			struct_type_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_33_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = struct_type_AST;
		return type;
	}

	public void union_type() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST union_type_AST = null;
		AST name_AST = null;

		try {      // for error handling
			AST tmp190_AST = null;
			tmp190_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp190_AST);
			match(LITERAL_union);
			identifier();
			if (0 == inputState.guessing)
			{
				name_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{
				switch ( LA(1) )
				{
				case LITERAL_switch:
				{
					AST tmp191_AST = null;
					tmp191_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp191_AST);
					match(LITERAL_switch);
					AST tmp192_AST = null;
					tmp192_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp192_AST);
					match(LPAREN);
					switch_type_spec();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					identifier();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					AST tmp193_AST = null;
					tmp193_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp193_AST);
					match(RPAREN);
					{
						switch ( LA(1) )
						{
						case IDENT:
						{
							identifier();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
							break;
						}
						case LBRACE:
						{
							break;
						}
						default:
						{
							throw new NoViableAltException(LT(1), getFilename());
						}
						 }
					}
					AST tmp194_AST = null;
					tmp194_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp194_AST);
					match(LBRACE);
					switch_body();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					AST tmp195_AST = null;
					tmp195_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp195_AST);
					match(RBRACE);
					break;
				}
				case LBRACE:
				{
					AST tmp196_AST = null;
					tmp196_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp196_AST);
					match(LBRACE);
					n_e_case_list();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					AST tmp197_AST = null;
					tmp197_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp197_AST);
					match(RBRACE);
					break;
				}
				case SEMI:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			if (0==inputState.guessing)
			{

							if (name_AST != null)
							{
								#if DEBUG_IDLGRAMMAR
								System.Console.WriteLine(string.Format("union {0}", name_AST.getText()));
								#endif
								CodeTypeDeclaration type = new CodeTypeDeclaration();
								type.IsStruct = true; // IsUnion does not exist
								type.Name = name_AST.getText();
								m_Namespace.UserData[type.Name] = type;
							}

			}
			union_type_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_33_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = union_type_AST;
	}

	public CodeTypeDeclaration  enum_type() //throws RecognitionException, TokenStreamException
{
		CodeTypeDeclaration type;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST enum_type_AST = null;
		AST name_AST = null;

				type = new CodeTypeDeclaration();
				type.IsEnum = true;
				string name = string.Empty;


		try {      // for error handling
			AST tmp198_AST = null;
			tmp198_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp198_AST);
			match(LITERAL_enum);
			{
				switch ( LA(1) )
				{
				case IDENT:
				{
					identifier();
					if (0 == inputState.guessing)
					{
						name_AST = (AST)returnAST;
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					if (0==inputState.guessing)
					{
						if (name_AST != null) name = name_AST.getText();
					}
					break;
				}
				case LBRACE:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp199_AST = null;
			tmp199_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp199_AST);
			match(LBRACE);
			enumerator_list(name, type.Members);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp200_AST = null;
			tmp200_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp200_AST);
			match(RBRACE);
			if (0==inputState.guessing)
			{

							if (name_AST != null)
							{
								#if DEBUG_IDLGRAMMAR
								System.Console.WriteLine(string.Format("enum {0}", name_AST.getText()));
								#endif
								type.Name = name_AST.getText();
							}

			}
			enum_type_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_34_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = enum_type_AST;
		return type;
	}

	public string  declarator(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST declarator_AST = null;
		AST i_AST = null;

				s = string.Empty;
				string f = string.Empty;
				List<string> arraySize = new List<string>();


		try {      // for error handling
			identifier();
			if (0 == inputState.guessing)
			{
				i_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==LBRACKET))
					{
						f=fixed_array_size();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{
							arraySize.Add(f);
						}
					}
					else
					{
						goto _loop134_breakloop;
					}

				}
_loop134_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{

						#if DEBUG_IDLGRAMMAR
							System.Diagnostics.Debug.WriteLine("declarator: " + i_AST.getText());
						#endif
							s = i_AST.getText();

							if (arraySize.Count > 0)
								attributes.Add("IsArray", arraySize);

			}
			declarator_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_35_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = declarator_AST;
		return s;
	}

	public void type_attribute() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST type_attribute_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_context_handle:
			{
				AST tmp201_AST = null;
				tmp201_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp201_AST);
				match(LITERAL_context_handle);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_handle:
			{
				AST tmp202_AST = null;
				tmp202_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp202_AST);
				match(LITERAL_handle);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_pipe:
			{
				AST tmp203_AST = null;
				tmp203_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp203_AST);
				match(LITERAL_pipe);
				type_attribute_AST = currentAST.root;
				break;
			}
			case V1_ENUM:
			{
				AST tmp204_AST = null;
				tmp204_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp204_AST);
				match(V1_ENUM);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_transmit_as:
			{
				AST tmp205_AST = null;
				tmp205_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp205_AST);
				match(LITERAL_transmit_as);
				AST tmp206_AST = null;
				tmp206_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp206_AST);
				match(LPAREN);
				simple_type_spec();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp207_AST = null;
				tmp207_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp207_AST);
				match(RPAREN);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_wire_marshal:
			{
				AST tmp208_AST = null;
				tmp208_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp208_AST);
				match(LITERAL_wire_marshal);
				AST tmp209_AST = null;
				tmp209_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp209_AST);
				match(LPAREN);
				simple_type_spec();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp210_AST = null;
				tmp210_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp210_AST);
				match(RPAREN);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_represent_as:
			{
				AST tmp211_AST = null;
				tmp211_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp211_AST);
				match(LITERAL_represent_as);
				AST tmp212_AST = null;
				tmp212_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp212_AST);
				match(LPAREN);
				simple_type_spec();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp213_AST = null;
				tmp213_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp213_AST);
				match(RPAREN);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_user_marshal:
			{
				AST tmp214_AST = null;
				tmp214_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp214_AST);
				match(LITERAL_user_marshal);
				AST tmp215_AST = null;
				tmp215_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp215_AST);
				match(LPAREN);
				simple_type_spec();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp216_AST = null;
				tmp216_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp216_AST);
				match(RPAREN);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_public:
			{
				AST tmp217_AST = null;
				tmp217_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp217_AST);
				match(LITERAL_public);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_string:
			{
				string_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_switch_type:
			{
				AST tmp218_AST = null;
				tmp218_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp218_AST);
				match(LITERAL_switch_type);
				AST tmp219_AST = null;
				tmp219_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp219_AST);
				match(LPAREN);
				switch_type_spec();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp220_AST = null;
				tmp220_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp220_AST);
				match(RPAREN);
				type_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_ref:
			case LITERAL_unique:
			case LITERAL_ptr:
			{
				ptr_attr();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				type_attribute_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_12_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = type_attribute_AST;
	}

	public void simple_type_spec() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST simple_type_spec_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case INT3264:
			case INT64:
			case LITERAL_signed:
			case LITERAL_unsigned:
			case LITERAL_octet:
			case LITERAL_any:
			case LITERAL_void:
			case LITERAL_byte:
			case LITERAL_wchar_t:
			case LITERAL_handle_t:
			case LITERAL_small:
			case LITERAL_short:
			case LITERAL_long:
			case LITERAL_int:
			case LITERAL_hyper:
			case LITERAL_char:
			case LITERAL_float:
			case LITERAL_double:
			case LITERAL_boolean:
			{
				base_type_spec();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				simple_type_spec_AST = currentAST.root;
				break;
			}
			case LITERAL_sequence:
			case LITERAL_string:
			{
				template_type_spec();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				simple_type_spec_AST = currentAST.root;
				break;
			}
			case SCOPEOP:
			case IDENT:
			{
				scoped_name();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				{    // ( ... )*
					for (;;)
					{
						if ((LA(1)==STAR))
						{
							AST tmp221_AST = null;
							tmp221_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp221_AST);
							match(STAR);
						}
						else
						{
							goto _loop104_breakloop;
						}

					}
_loop104_breakloop:					;
				}    // ( ... )*
				simple_type_spec_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_36_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = simple_type_spec_AST;
	}

	public void switch_type_spec() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST switch_type_spec_AST = null;
		CodeTypeDeclaration type;

		try {      // for error handling
			switch ( LA(1) )
			{
			case INT3264:
			case INT64:
			case LITERAL_signed:
			case LITERAL_unsigned:
			case LITERAL_small:
			case LITERAL_short:
			case LITERAL_long:
			case LITERAL_int:
			case LITERAL_hyper:
			case LITERAL_char:
			{
				{
					switch ( LA(1) )
					{
					case LITERAL_signed:
					{
						AST tmp222_AST = null;
						tmp222_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp222_AST);
						match(LITERAL_signed);
						break;
					}
					case LITERAL_unsigned:
					{
						AST tmp223_AST = null;
						tmp223_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp223_AST);
						match(LITERAL_unsigned);
						break;
					}
					case INT3264:
					case INT64:
					case LITERAL_small:
					case LITERAL_short:
					case LITERAL_long:
					case LITERAL_int:
					case LITERAL_hyper:
					case LITERAL_char:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				{
					switch ( LA(1) )
					{
					case INT3264:
					case INT64:
					case LITERAL_small:
					case LITERAL_short:
					case LITERAL_long:
					case LITERAL_int:
					case LITERAL_hyper:
					{
						integer_type();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case LITERAL_char:
					{
						char_type();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				switch_type_spec_AST = currentAST.root;
				break;
			}
			case LITERAL_boolean:
			{
				boolean_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				switch_type_spec_AST = currentAST.root;
				break;
			}
			case LITERAL_enum:
			{
				type=enum_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				switch_type_spec_AST = currentAST.root;
				break;
			}
			case SCOPEOP:
			case IDENT:
			{
				scoped_name();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				switch_type_spec_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_37_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = switch_type_spec_AST;
	}

	public CodeTypeMember  type_spec() //throws RecognitionException, TokenStreamException
{
		CodeTypeMember type;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST type_spec_AST = null;
		AST s_AST = null;
		type = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LITERAL_const:
				{
					AST tmp224_AST = null;
					tmp224_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp224_AST);
					match(LITERAL_const);
					break;
				}
				case INT3264:
				case INT64:
				case SCOPEOP:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				case LITERAL_sequence:
				case LITERAL_string:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			{
				switch ( LA(1) )
				{
				case INT3264:
				case INT64:
				case SCOPEOP:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_sequence:
				case LITERAL_string:
				case IDENT:
				{
					simple_type_spec();
					if (0 == inputState.guessing)
					{
						s_AST = (AST)returnAST;
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					if (0==inputState.guessing)
					{

										type = new CodeMemberField();
										((CodeMemberField)type).Type = m_Conv.ConvertParamType(s_AST.getText(), null, new Hashtable());
										type.Attributes = (type.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;

					}
					break;
				}
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				{
					type=constr_type_spec();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			type_spec_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_33_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = type_spec_AST;
		return type;
	}

	public string  declarator_list(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST declarator_list_AST = null;

				string ignored;
				s = string.Empty;


		try {      // for error handling
			s=declarator(attributes);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp225_AST = null;
						tmp225_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp225_AST);
						match(COMMA);
						{    // ( ... )*
							for (;;)
							{
								if ((LA(1)==STAR))
								{
									AST tmp226_AST = null;
									tmp226_AST = astFactory.create(LT(1));
									astFactory.addASTChild(ref currentAST, tmp226_AST);
									match(STAR);
								}
								else
								{
									goto _loop130_breakloop;
								}

							}
_loop130_breakloop:							;
						}    // ( ... )*
						ignored=declarator(attributes);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop131_breakloop;
					}

				}
_loop131_breakloop:				;
			}    // ( ... )*
			declarator_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = declarator_list_AST;
		return s;
	}

	public CodeTypeDeclaration  constr_type_spec() //throws RecognitionException, TokenStreamException
{
		CodeTypeDeclaration type;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST constr_type_spec_AST = null;
		type = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_struct:
			{
				type=struct_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				constr_type_spec_AST = currentAST.root;
				break;
			}
			case LITERAL_union:
			{
				union_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				constr_type_spec_AST = currentAST.root;
				break;
			}
			case LITERAL_enum:
			{
				type=enum_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				constr_type_spec_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_33_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = constr_type_spec_AST;
		return type;
	}

	public void template_type_spec() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST template_type_spec_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_sequence:
			{
				sequence_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				template_type_spec_AST = currentAST.root;
				break;
			}
			case LITERAL_string:
			{
				string_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				template_type_spec_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_36_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = template_type_spec_AST;
	}

	public void integer_type() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST integer_type_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LITERAL_small:
				{
					AST tmp227_AST = null;
					tmp227_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp227_AST);
					match(LITERAL_small);
					break;
				}
				case LITERAL_short:
				{
					AST tmp228_AST = null;
					tmp228_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp228_AST);
					match(LITERAL_short);
					break;
				}
				case LITERAL_long:
				{
					AST tmp229_AST = null;
					tmp229_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp229_AST);
					match(LITERAL_long);
					break;
				}
				case LITERAL_int:
				{
					AST tmp230_AST = null;
					tmp230_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp230_AST);
					match(LITERAL_int);
					break;
				}
				case LITERAL_hyper:
				{
					AST tmp231_AST = null;
					tmp231_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp231_AST);
					match(LITERAL_hyper);
					break;
				}
				case INT3264:
				{
					AST tmp232_AST = null;
					tmp232_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp232_AST);
					match(INT3264);
					break;
				}
				case INT64:
				{
					AST tmp233_AST = null;
					tmp233_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp233_AST);
					match(INT64);
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			{
				switch ( LA(1) )
				{
				case LITERAL_int:
				{
					AST tmp234_AST = null;
					tmp234_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp234_AST);
					match(LITERAL_int);
					break;
				}
				case SEMI:
				case COMMA:
				case RPAREN:
				case LITERAL_const:
				case STAR:
				case GT:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			integer_type_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_38_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = integer_type_AST;
	}

	public void char_type() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST char_type_AST = null;

		try {      // for error handling
			AST tmp235_AST = null;
			tmp235_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp235_AST);
			match(LITERAL_char);
			char_type_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_38_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = char_type_AST;
	}

	public void boolean_type() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST boolean_type_AST = null;

		try {      // for error handling
			AST tmp236_AST = null;
			tmp236_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp236_AST);
			match(LITERAL_boolean);
			boolean_type_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_38_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = boolean_type_AST;
	}

	public void floating_pt_type() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST floating_pt_type_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_float:
			{
				AST tmp237_AST = null;
				tmp237_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp237_AST);
				match(LITERAL_float);
				floating_pt_type_AST = currentAST.root;
				break;
			}
			case LITERAL_double:
			{
				AST tmp238_AST = null;
				tmp238_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp238_AST);
				match(LITERAL_double);
				floating_pt_type_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_38_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = floating_pt_type_AST;
	}

	public void attr_vars() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST attr_vars_AST = null;

		try {      // for error handling
			attr_var();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp239_AST = null;
						tmp239_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp239_AST);
						match(COMMA);
						attr_var();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop113_breakloop;
					}

				}
_loop113_breakloop:				;
			}    // ( ... )*
			attr_vars_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_14_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = attr_vars_AST;
	}

	public void attr_var() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST attr_var_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case STAR:
				case IDENT:
				{
					{
						{
							switch ( LA(1) )
							{
							case STAR:
							{
								AST tmp240_AST = null;
								tmp240_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp240_AST);
								match(STAR);
								break;
							}
							case IDENT:
							{
								break;
							}
							default:
							{
								throw new NoViableAltException(LT(1), getFilename());
							}
							 }
						}
						identifier();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					break;
				}
				case INT:
				{
					AST tmp241_AST = null;
					tmp241_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp241_AST);
					match(INT);
					break;
				}
				case HEX:
				{
					AST tmp242_AST = null;
					tmp242_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp242_AST);
					match(HEX);
					break;
				}
				case COMMA:
				case RPAREN:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			attr_var_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_15_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = attr_var_AST;
	}

	public void sequence_type() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST sequence_type_AST = null;

		try {      // for error handling
			AST tmp243_AST = null;
			tmp243_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp243_AST);
			match(LITERAL_sequence);
			AST tmp244_AST = null;
			tmp244_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp244_AST);
			match(LT_);
			simple_type_spec();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			opt_pos_int();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp245_AST = null;
			tmp245_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp245_AST);
			match(GT);
			sequence_type_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_36_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = sequence_type_AST;
	}

	public string  fixed_array_size() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST fixed_array_size_AST = null;
		AST bounds_AST = null;
		s = string.Empty;

		try {      // for error handling
			AST tmp246_AST = null;
			tmp246_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp246_AST);
			match(LBRACKET);
			{
				switch ( LA(1) )
				{
				case LPAREN:
				case SCOPEOP:
				case STAR:
				case PLUS:
				case MINUS:
				case TILDE:
				case LITERAL_TRUE:
				case LITERAL_true:
				case LITERAL_FALSE:
				case LITERAL_false:
				case INT:
				case HEX:
				case OCTAL:
				case LITERAL_L:
				case STRING_LITERAL:
				case CHAR_LITERAL:
				case IDENT:
				{
					array_bounds();
					if (0 == inputState.guessing)
					{
						bounds_AST = (AST)returnAST;
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case RBRACKET:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp247_AST = null;
			tmp247_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp247_AST);
			match(RBRACKET);
			if (0==inputState.guessing)
			{

							if (bounds_AST != null)
								s = bounds_AST.getText();

			}
			fixed_array_size_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_39_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = fixed_array_size_AST;
		return s;
	}

	public void member(
		CodeTypeMemberCollection members
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST member_AST = null;

				Hashtable attributes = new Hashtable();
				CodeTypeMember type;
				string name;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LBRACKET:
				{
					field_attribute_list(attributes);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case INT3264:
				case INT64:
				case SCOPEOP:
				case LITERAL_const:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				case LITERAL_sequence:
				case LITERAL_string:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			type=type_spec();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			name=declarator_list(attributes);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp248_AST = null;
			tmp248_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp248_AST);
			match(SEMI);
			if (0==inputState.guessing)
			{

							if (type != null && name != string.Empty)
							{
								if (attributes["IsArray"] != null)
								{
									List<string> arraySizes = (List<string>)attributes["IsArray"];
									if (arraySizes.Count > 1)
									{
										Console.WriteLine(string.Format("Can't handle multi dimensional arrays: {0}",
											name));
									}

									if (arraySizes.Count == 1)
									{
										// Add attribute: [MarshalAs(UnmanagedType.ByValArray, SizeConst=x)]
										int val;
										if (int.TryParse(arraySizes[0], out val))
										{
											if (type is CodeMemberField)
												((CodeMemberField)type).Type.ArrayRank = 1;
											else
												Console.WriteLine(string.Format("Unhandled type: {0}", type.GetType()));

											type.CustomAttributes.Add(new CodeAttributeDeclaration("MarshalAs",
												new CodeAttributeArgument(
													new CodeSnippetExpression("UnmanagedType.ByValArray")),
												new CodeAttributeArgument("SizeConst",
													new CodePrimitiveExpression(val))));
										}
										else
										{
											Console.WriteLine(string.Format("Can't handle array dimension spec: '{0}' for {1}",
												arraySizes[0], name));
										}
									}
									attributes.Remove("IsArray");
								}

								type.Name = name;
								members.Add(type);
							}

			}
			member_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_40_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = member_AST;
	}

	public void field_attribute_list(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST field_attribute_list_AST = null;

		try {      // for error handling
			AST tmp249_AST = null;
			tmp249_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp249_AST);
			match(LBRACKET);
			field_attribute(attributes);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp250_AST = null;
						tmp250_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp250_AST);
						match(COMMA);
						field_attribute(attributes);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop222_breakloop;
					}

				}
_loop222_breakloop:				;
			}    // ( ... )*
			AST tmp251_AST = null;
			tmp251_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp251_AST);
			match(RBRACKET);
			field_attribute_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_41_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = field_attribute_list_AST;
	}

	public void switch_body() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST switch_body_AST = null;

		try {      // for error handling
			case_stmt_list();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			switch_body_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_9_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = switch_body_AST;
	}

	public void n_e_case_list() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST n_e_case_list_AST = null;

		try {      // for error handling
			{ // ( ... )+
				int _cnt152=0;
				for (;;)
				{
					if ((LA(1)==LBRACKET))
					{
						n_e_case_stmt();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						if (_cnt152 >= 1) { goto _loop152_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt152++;
				}
_loop152_breakloop:				;
			}    // ( ... )+
			n_e_case_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_9_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = n_e_case_list_AST;
	}

	public void case_stmt_list() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST case_stmt_list_AST = null;

		try {      // for error handling
			{ // ( ... )+
				int _cnt159=0;
				for (;;)
				{
					if ((LA(1)==LITERAL_default||LA(1)==LITERAL_case))
					{
						case_stmt();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						if (_cnt159 >= 1) { goto _loop159_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt159++;
				}
_loop159_breakloop:				;
			}    // ( ... )+
			case_stmt_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_9_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = case_stmt_list_AST;
	}

	public void n_e_case_stmt() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST n_e_case_stmt_AST = null;

		try {      // for error handling
			n_e_case_label();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{
				switch ( LA(1) )
				{
				case INT3264:
				case INT64:
				case LBRACKET:
				case SCOPEOP:
				case LITERAL_const:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				case LITERAL_sequence:
				case LITERAL_string:
				case IDENT:
				{
					unnamed_element_spec();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case SEMI:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp252_AST = null;
			tmp252_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp252_AST);
			match(SEMI);
			n_e_case_stmt_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_42_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = n_e_case_stmt_AST;
	}

	public void n_e_case_label() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST n_e_case_label_AST = null;
		string ignored;

		try {      // for error handling
			AST tmp253_AST = null;
			tmp253_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp253_AST);
			match(LBRACKET);
			{
				switch ( LA(1) )
				{
				case LITERAL_case:
				{
					AST tmp254_AST = null;
					tmp254_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp254_AST);
					match(LITERAL_case);
					AST tmp255_AST = null;
					tmp255_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp255_AST);
					match(LPAREN);
					ignored=const_exp();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					AST tmp256_AST = null;
					tmp256_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp256_AST);
					match(RPAREN);
					break;
				}
				case LITERAL_default:
				{
					AST tmp257_AST = null;
					tmp257_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp257_AST);
					match(LITERAL_default);
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp258_AST = null;
			tmp258_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp258_AST);
			match(RBRACKET);
			n_e_case_label_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_43_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = n_e_case_label_AST;
	}

	public void unnamed_element_spec() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST unnamed_element_spec_AST = null;

				Hashtable attributes = new Hashtable();
				CodeTypeMember type;
				string ignored;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LBRACKET:
				{
					field_attribute_list(attributes);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case INT3264:
				case INT64:
				case SCOPEOP:
				case LITERAL_const:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				case LITERAL_sequence:
				case LITERAL_string:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			type=type_spec();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{
				switch ( LA(1) )
				{
				case IDENT:
				{
					ignored=declarator(attributes);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case SEMI:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			unnamed_element_spec_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_2_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = unnamed_element_spec_AST;
	}

	public void case_stmt() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST case_stmt_AST = null;

		try {      // for error handling
			case_label_list();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			unnamed_element_spec();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp259_AST = null;
			tmp259_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp259_AST);
			match(SEMI);
			case_stmt_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_44_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = case_stmt_AST;
	}

	public void case_label_list() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST case_label_list_AST = null;

		try {      // for error handling
			{ // ( ... )+
				int _cnt163=0;
				for (;;)
				{
					if ((LA(1)==LITERAL_default||LA(1)==LITERAL_case))
					{
						case_label();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						if (_cnt163 >= 1) { goto _loop163_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt163++;
				}
_loop163_breakloop:				;
			}    // ( ... )+
			case_label_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_3_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = case_label_list_AST;
	}

	public void case_label() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST case_label_AST = null;
		string ignored;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_case:
			{
				AST tmp260_AST = null;
				tmp260_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp260_AST);
				match(LITERAL_case);
				ignored=const_exp();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp261_AST = null;
				tmp261_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp261_AST);
				match(COLON);
				case_label_AST = currentAST.root;
				break;
			}
			case LITERAL_default:
			{
				AST tmp262_AST = null;
				tmp262_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp262_AST);
				match(LITERAL_default);
				AST tmp263_AST = null;
				tmp263_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp263_AST);
				match(COLON);
				case_label_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_45_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = case_label_AST;
	}

	public void element_spec() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST element_spec_AST = null;

				Hashtable attributes = new Hashtable();
				CodeTypeMember type;
				string ignored;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LBRACKET:
				{
					field_attribute_list(attributes);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case INT3264:
				case INT64:
				case SCOPEOP:
				case LITERAL_const:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_struct:
				case LITERAL_union:
				case LITERAL_enum:
				case LITERAL_sequence:
				case LITERAL_string:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			type=type_spec();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			ignored=declarator(attributes);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			element_spec_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_16_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = element_spec_AST;
	}

	public void enumerator_list(
		string enumName, CodeTypeMemberCollection members
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST enumerator_list_AST = null;

				string s1 = string.Empty;
				string s2 = string.Empty;
				string expr1 = string.Empty;
				string expr2 = string.Empty;


		try {      // for error handling
			s1=enumerator(ref expr1);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp264_AST = null;
						tmp264_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp264_AST);
						match(COMMA);
						s2=enumerator(ref expr2);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{

												members.Add(IDLConversions.CreateEnumMember(enumName, s1, expr1));
												s1 = s2;
												expr1 = expr2;

						}
					}
					else
					{
						goto _loop174_breakloop;
					}

				}
_loop174_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{

							if (s1 != string.Empty)
								members.Add(IDLConversions.CreateEnumMember(enumName, s1, expr1));

			}
			enumerator_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_9_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = enumerator_list_AST;
	}

	public string  enumerator(
		ref string e
	) //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST enumerator_AST = null;
		AST name_AST = null;

				s = string.Empty;
				e = string.Empty;


		try {      // for error handling
			switch ( LA(1) )
			{
			case IDENT:
			{
				identifier();
				if (0 == inputState.guessing)
				{
					name_AST = (AST)returnAST;
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				{
					switch ( LA(1) )
					{
					case ASSIGN:
					{
						AST tmp265_AST = null;
						tmp265_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp265_AST);
						match(ASSIGN);
						e=const_exp();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case RBRACE:
					case COMMA:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				if (0==inputState.guessing)
				{

								s = name_AST.getText();

				}
				enumerator_AST = currentAST.root;
				break;
			}
			case RBRACE:
			case COMMA:
			{
				enumerator_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_46_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = enumerator_AST;
		return s;
	}

	public void enumerator_value() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST enumerator_value_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case MINUS:
				{
					AST tmp266_AST = null;
					tmp266_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp266_AST);
					match(MINUS);
					break;
				}
				case INT:
				case HEX:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			{
				switch ( LA(1) )
				{
				case INT:
				{
					AST tmp267_AST = null;
					tmp267_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp267_AST);
					match(INT);
					break;
				}
				case HEX:
				{
					AST tmp268_AST = null;
					tmp268_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp268_AST);
					match(HEX);
					break;
				}
				case IDENT:
				{
					identifier();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			enumerator_value_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_16_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = enumerator_value_AST;
	}

	public void opt_pos_int() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST opt_pos_int_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case COMMA:
				{
					AST tmp269_AST = null;
					tmp269_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp269_AST);
					match(COMMA);
					positive_int_const();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case GT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			opt_pos_int_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_47_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = opt_pos_int_AST;
	}

	public void array_bounds() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST array_bounds_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LPAREN:
			case SCOPEOP:
			case PLUS:
			case MINUS:
			case TILDE:
			case LITERAL_TRUE:
			case LITERAL_true:
			case LITERAL_FALSE:
			case LITERAL_false:
			case INT:
			case HEX:
			case OCTAL:
			case LITERAL_L:
			case STRING_LITERAL:
			case CHAR_LITERAL:
			case IDENT:
			{
				array_bound();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				{
					switch ( LA(1) )
					{
					case RANGE:
					{
						AST tmp270_AST = null;
						tmp270_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp270_AST);
						match(RANGE);
						array_bound();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case RBRACKET:
					{
						break;
					}
					default:
					{
						throw new NoViableAltException(LT(1), getFilename());
					}
					 }
				}
				array_bounds_AST = currentAST.root;
				break;
			}
			case STAR:
			{
				AST tmp271_AST = null;
				tmp271_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp271_AST);
				match(STAR);
				array_bounds_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_4_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = array_bounds_AST;
	}

	public void array_bound() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST array_bound_AST = null;

		try {      // for error handling
			positive_int_const();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			array_bound_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_48_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = array_bound_AST;
	}

	public void param_type_spec() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST param_type_spec_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case INT3264:
				case INT64:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				{
					base_type_spec();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case LITERAL_string:
				{
					string_type();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case SCOPEOP:
				case IDENT:
				{
					scoped_name();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)==STAR))
							{
								AST tmp272_AST = null;
								tmp272_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp272_AST);
								match(STAR);
							}
							else
							{
								goto _loop235_breakloop;
							}

						}
_loop235_breakloop:						;
					}    // ( ... )*
					break;
				}
				case LITERAL_SAFEARRAY:
				{
					AST tmp273_AST = null;
					tmp273_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp273_AST);
					match(LITERAL_SAFEARRAY);
					AST tmp274_AST = null;
					tmp274_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp274_AST);
					match(LPAREN);
					base_type_spec();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					AST tmp275_AST = null;
					tmp275_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp275_AST);
					match(RPAREN);
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			param_type_spec_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_49_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = param_type_spec_AST;
	}

	public void function_attribute_list(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST function_attribute_list_AST = null;

		try {      // for error handling
			AST tmp276_AST = null;
			tmp276_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp276_AST);
			match(LBRACKET);
			{
				switch ( LA(1) )
				{
				case LITERAL_uuid:
				case LITERAL_version:
				case LITERAL_async_uuid:
				case LITERAL_local:
				case LITERAL_object:
				case LITERAL_pointer_default:
				case LITERAL_endpoint:
				case LITERAL_odl:
				case LITERAL_optimize:
				case LITERAL_proxy:
				case LITERAL_aggregatable:
				case LITERAL_appobject:
				case LITERAL_bindable:
				case LITERAL_control:
				case LITERAL_custom:
				case LITERAL_default:
				case LITERAL_defaultbind:
				case LITERAL_defaultcollelem:
				case LITERAL_defaultvtable:
				case LITERAL_displaybind:
				case LITERAL_dllname:
				case LITERAL_dual:
				case LITERAL_entry:
				case LITERAL_helpcontext:
				case LITERAL_helpfile:
				case LITERAL_helpstring:
				case LITERAL_helpstringdll:
				case LITERAL_hidden:
				case LITERAL_id:
				case LITERAL_idempotent:
				case LITERAL_immediatebind:
				case LITERAL_lcid:
				case LITERAL_licensed:
				case LITERAL_message:
				case LITERAL_nonbrowsable:
				case LITERAL_noncreatable:
				case LITERAL_nonextensible:
				case LITERAL_oleautomation:
				case LITERAL_restricted:
				case LITERAL_context_handle:
				case LITERAL_ref:
				case LITERAL_unique:
				case LITERAL_ptr:
				case LITERAL_string:
				case LITERAL_callback:
				case LITERAL_broadcast:
				case LITERAL_ignore:
				case LITERAL_propget:
				case LITERAL_propput:
				case LITERAL_propputref:
				case LITERAL_uidefault:
				case LITERAL_usesgetlasterror:
				case LITERAL_vararg:
				{
					function_attribute(attributes);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)==COMMA))
							{
								AST tmp277_AST = null;
								tmp277_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp277_AST);
								match(COMMA);
								function_attribute(attributes);
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(ref currentAST, returnAST);
								}
							}
							else
							{
								goto _loop203_breakloop;
							}

						}
_loop203_breakloop:						;
					}    // ( ... )*
					break;
				}
				case RBRACKET:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp278_AST = null;
			tmp278_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp278_AST);
			match(RBRACKET);
			function_attribute_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_50_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = function_attribute_list_AST;
	}

	public CodeParameterDeclarationExpressionCollection  parameter_dcls() //throws RecognitionException, TokenStreamException
{
		CodeParameterDeclarationExpressionCollection paramColl;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST parameter_dcls_AST = null;

				paramColl = new CodeParameterDeclarationExpressionCollection();
				CodeParameterDeclarationExpression param;


		try {      // for error handling
			match(LPAREN);
			{
				switch ( LA(1) )
				{
				case INT3264:
				case INT64:
				case LBRACKET:
				case SCOPEOP:
				case LITERAL_const:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_string:
				case LITERAL_SAFEARRAY:
				case IDENT:
				{
					param=param_dcl();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					if (0==inputState.guessing)
					{
						paramColl.Add(param);
					}
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)==COMMA))
							{
								match(COMMA);
								param=param_dcl();
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(ref currentAST, returnAST);
								}
								if (0==inputState.guessing)
								{
									paramColl.Add(param);
								}
							}
							else
							{
								goto _loop208_breakloop;
							}

						}
_loop208_breakloop:						;
					}    // ( ... )*
					break;
				}
				case RPAREN:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			match(RPAREN);
			parameter_dcls_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_51_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = parameter_dcls_AST;
		return paramColl;
	}

	public void function_attribute(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST function_attribute_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_callback:
			{
				AST tmp282_AST = null;
				tmp282_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp282_AST);
				match(LITERAL_callback);
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_broadcast:
			{
				AST tmp283_AST = null;
				tmp283_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp283_AST);
				match(LITERAL_broadcast);
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_ref:
			case LITERAL_unique:
			case LITERAL_ptr:
			{
				ptr_attr();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_string:
			{
				string_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_ignore:
			{
				AST tmp284_AST = null;
				tmp284_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp284_AST);
				match(LITERAL_ignore);
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_context_handle:
			{
				AST tmp285_AST = null;
				tmp285_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp285_AST);
				match(LITERAL_context_handle);
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_propget:
			{
				AST tmp286_AST = null;
				tmp286_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp286_AST);
				match(LITERAL_propget);
				if (0==inputState.guessing)
				{

								attributes.Add("propget", new CodeAttributeArgument());

				}
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_propput:
			{
				AST tmp287_AST = null;
				tmp287_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp287_AST);
				match(LITERAL_propput);
				if (0==inputState.guessing)
				{

								attributes.Add("propput", new CodeAttributeArgument());

				}
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_propputref:
			{
				AST tmp288_AST = null;
				tmp288_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp288_AST);
				match(LITERAL_propputref);
				if (0==inputState.guessing)
				{

								attributes.Add("propputref", new CodeAttributeArgument());

				}
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_uidefault:
			{
				AST tmp289_AST = null;
				tmp289_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp289_AST);
				match(LITERAL_uidefault);
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_usesgetlasterror:
			{
				AST tmp290_AST = null;
				tmp290_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp290_AST);
				match(LITERAL_usesgetlasterror);
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_vararg:
			{
				AST tmp291_AST = null;
				tmp291_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp291_AST);
				match(LITERAL_vararg);
				function_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_uuid:
			case LITERAL_version:
			case LITERAL_async_uuid:
			case LITERAL_local:
			case LITERAL_object:
			case LITERAL_pointer_default:
			case LITERAL_endpoint:
			case LITERAL_odl:
			case LITERAL_optimize:
			case LITERAL_proxy:
			case LITERAL_aggregatable:
			case LITERAL_appobject:
			case LITERAL_bindable:
			case LITERAL_control:
			case LITERAL_custom:
			case LITERAL_default:
			case LITERAL_defaultbind:
			case LITERAL_defaultcollelem:
			case LITERAL_defaultvtable:
			case LITERAL_displaybind:
			case LITERAL_dllname:
			case LITERAL_dual:
			case LITERAL_entry:
			case LITERAL_helpcontext:
			case LITERAL_helpfile:
			case LITERAL_helpstring:
			case LITERAL_helpstringdll:
			case LITERAL_hidden:
			case LITERAL_id:
			case LITERAL_idempotent:
			case LITERAL_immediatebind:
			case LITERAL_lcid:
			case LITERAL_licensed:
			case LITERAL_message:
			case LITERAL_nonbrowsable:
			case LITERAL_noncreatable:
			case LITERAL_nonextensible:
			case LITERAL_oleautomation:
			case LITERAL_restricted:
			{
				attribute(attributes);
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				function_attribute_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_12_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = function_attribute_AST;
	}

	public CodeParameterDeclarationExpression  param_dcl() //throws RecognitionException, TokenStreamException
{
		CodeParameterDeclarationExpression param;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST param_dcl_AST = null;
		AST strType_AST = null;

				param = new CodeParameterDeclarationExpression();
				Hashtable attributes = new Hashtable();
				string name = string.Empty;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case LBRACKET:
				{
					AST tmp292_AST = null;
					tmp292_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp292_AST);
					match(LBRACKET);
					param_attributes(attributes);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					AST tmp293_AST = null;
					tmp293_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp293_AST);
					match(RBRACKET);
					break;
				}
				case INT3264:
				case INT64:
				case SCOPEOP:
				case LITERAL_const:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_string:
				case LITERAL_SAFEARRAY:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			{
				switch ( LA(1) )
				{
				case LITERAL_const:
				{
					AST tmp294_AST = null;
					tmp294_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp294_AST);
					match(LITERAL_const);
					break;
				}
				case INT3264:
				case INT64:
				case SCOPEOP:
				case LITERAL_signed:
				case LITERAL_unsigned:
				case LITERAL_octet:
				case LITERAL_any:
				case LITERAL_void:
				case LITERAL_byte:
				case LITERAL_wchar_t:
				case LITERAL_handle_t:
				case LITERAL_small:
				case LITERAL_short:
				case LITERAL_long:
				case LITERAL_int:
				case LITERAL_hyper:
				case LITERAL_char:
				case LITERAL_float:
				case LITERAL_double:
				case LITERAL_boolean:
				case LITERAL_string:
				case LITERAL_SAFEARRAY:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			param_type_spec();
			if (0 == inputState.guessing)
			{
				strType_AST = (AST)returnAST;
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{
				switch ( LA(1) )
				{
				case LITERAL_const:
				{
					AST tmp295_AST = null;
					tmp295_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp295_AST);
					match(LITERAL_const);
					break;
				}
				case COMMA:
				case RPAREN:
				case IDENT:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			{
				switch ( LA(1) )
				{
				case IDENT:
				{
					name=declarator(attributes);
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case COMMA:
				case RPAREN:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			if (0==inputState.guessing)
			{

							string str = null;
							if (strType_AST != null && name != string.Empty)
							{
								str = strType_AST.ToStringList();
								param.Name = IDLConversions.ConvertParamName(name);
								param.Type = m_Conv.ConvertParamType(str, param, attributes);
							}

			}
			param_dcl_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_15_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = param_dcl_AST;
		return param;
	}

	public void param_attributes(
		IDictionary param
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST param_attributes_AST = null;

		try {      // for error handling
			param_attribute(param);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp296_AST = null;
						tmp296_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp296_AST);
						match(COMMA);
						param_attribute(param);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop216_breakloop;
					}

				}
_loop216_breakloop:				;
			}    // ( ... )*
			param_attributes_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_4_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = param_attributes_AST;
	}

	public void param_attribute(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST param_attribute_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_in:
			{
				AST tmp297_AST = null;
				tmp297_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp297_AST);
				match(LITERAL_in);
				if (0==inputState.guessing)
				{
					attributes["in"] = true;
				}
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_out:
			{
				AST tmp298_AST = null;
				tmp298_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp298_AST);
				match(LITERAL_out);
				if (0==inputState.guessing)
				{
					attributes["out"] = true;
				}
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_retval:
			{
				AST tmp299_AST = null;
				tmp299_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp299_AST);
				match(LITERAL_retval);
				if (0==inputState.guessing)
				{
					attributes["retval"] = true;
				}
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_defaultvalue:
			{
				AST tmp300_AST = null;
				tmp300_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp300_AST);
				match(LITERAL_defaultvalue);
				AST tmp301_AST = null;
				tmp301_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp301_AST);
				match(LPAREN);
				{ // ( ... )+
					int _cnt219=0;
					for (;;)
					{
						if ((tokenSet_7_.member(LA(1))))
						{
							AST tmp302_AST = null;
							tmp302_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp302_AST);
							matchNot(RPAREN);
						}
						else
						{
							if (_cnt219 >= 1) { goto _loop219_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
						}

						_cnt219++;
					}
_loop219_breakloop:					;
				}    // ( ... )+
				AST tmp303_AST = null;
				tmp303_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp303_AST);
				match(RPAREN);
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_optional:
			{
				AST tmp304_AST = null;
				tmp304_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp304_AST);
				match(LITERAL_optional);
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_readonly:
			{
				AST tmp305_AST = null;
				tmp305_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp305_AST);
				match(LITERAL_readonly);
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_requestedit:
			{
				AST tmp306_AST = null;
				tmp306_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp306_AST);
				match(LITERAL_requestedit);
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_iid_is:
			{
				AST tmp307_AST = null;
				tmp307_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp307_AST);
				match(LITERAL_iid_is);
				AST tmp308_AST = null;
				tmp308_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp308_AST);
				match(LPAREN);
				attr_vars();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp309_AST = null;
				tmp309_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp309_AST);
				match(RPAREN);
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_range:
			{
				AST tmp310_AST = null;
				tmp310_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp310_AST);
				match(LITERAL_range);
				AST tmp311_AST = null;
				tmp311_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp311_AST);
				match(LPAREN);
				integer_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp312_AST = null;
				tmp312_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp312_AST);
				match(COMMA);
				integer_literal();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp313_AST = null;
				tmp313_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp313_AST);
				match(RPAREN);
				param_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_uuid:
			case LITERAL_version:
			case LITERAL_async_uuid:
			case LITERAL_local:
			case LITERAL_object:
			case LITERAL_pointer_default:
			case LITERAL_endpoint:
			case LITERAL_odl:
			case LITERAL_optimize:
			case LITERAL_proxy:
			case LITERAL_aggregatable:
			case LITERAL_appobject:
			case LITERAL_bindable:
			case LITERAL_control:
			case LITERAL_custom:
			case LITERAL_default:
			case LITERAL_defaultbind:
			case LITERAL_defaultcollelem:
			case LITERAL_defaultvtable:
			case LITERAL_displaybind:
			case LITERAL_dllname:
			case LITERAL_dual:
			case LITERAL_entry:
			case LITERAL_helpcontext:
			case LITERAL_helpfile:
			case LITERAL_helpstring:
			case LITERAL_helpstringdll:
			case LITERAL_hidden:
			case LITERAL_id:
			case LITERAL_idempotent:
			case LITERAL_immediatebind:
			case LITERAL_lcid:
			case LITERAL_licensed:
			case LITERAL_message:
			case LITERAL_nonbrowsable:
			case LITERAL_noncreatable:
			case LITERAL_nonextensible:
			case LITERAL_oleautomation:
			case LITERAL_restricted:
			case LITERAL_ref:
			case LITERAL_unique:
			case LITERAL_ptr:
			case LITERAL_string:
			case LITERAL_ignore:
			case LITERAL_size_is:
			case LITERAL_max_is:
			case LITERAL_length_is:
			case LITERAL_first_is:
			case LITERAL_last_is:
			case LITERAL_switch_is:
			case LITERAL_source:
			{
				field_attribute(attributes);
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				param_attribute_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_12_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = param_attribute_AST;
	}

	public void field_attribute(
		IDictionary attributes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST field_attribute_AST = null;
		string val;

		try {      // for error handling
			switch ( LA(1) )
			{
			case LITERAL_ignore:
			{
				AST tmp314_AST = null;
				tmp314_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp314_AST);
				match(LITERAL_ignore);
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_size_is:
			{
				AST tmp315_AST = null;
				tmp315_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp315_AST);
				match(LITERAL_size_is);
				AST tmp316_AST = null;
				tmp316_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp316_AST);
				match(LPAREN);
				{    // ( ... )*
					for (;;)
					{
						if ((LA(1)==STAR))
						{
							AST tmp317_AST = null;
							tmp317_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp317_AST);
							match(STAR);
						}
						else
						{
							goto _loop225_breakloop;
						}

					}
_loop225_breakloop:					;
				}    // ( ... )*
				val=const_exp();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp318_AST = null;
				tmp318_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp318_AST);
				match(RPAREN);
				if (0==inputState.guessing)
				{
					attributes.Add("size_is", val);
				}
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_max_is:
			{
				AST tmp319_AST = null;
				tmp319_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp319_AST);
				match(LITERAL_max_is);
				AST tmp320_AST = null;
				tmp320_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp320_AST);
				match(LPAREN);
				attr_vars();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp321_AST = null;
				tmp321_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp321_AST);
				match(RPAREN);
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_length_is:
			{
				AST tmp322_AST = null;
				tmp322_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp322_AST);
				match(LITERAL_length_is);
				AST tmp323_AST = null;
				tmp323_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp323_AST);
				match(LPAREN);
				attr_vars();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp324_AST = null;
				tmp324_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp324_AST);
				match(RPAREN);
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_first_is:
			{
				AST tmp325_AST = null;
				tmp325_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp325_AST);
				match(LITERAL_first_is);
				AST tmp326_AST = null;
				tmp326_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp326_AST);
				match(LPAREN);
				attr_vars();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp327_AST = null;
				tmp327_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp327_AST);
				match(RPAREN);
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_last_is:
			{
				AST tmp328_AST = null;
				tmp328_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp328_AST);
				match(LITERAL_last_is);
				AST tmp329_AST = null;
				tmp329_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp329_AST);
				match(LPAREN);
				attr_vars();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp330_AST = null;
				tmp330_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp330_AST);
				match(RPAREN);
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_switch_is:
			{
				AST tmp331_AST = null;
				tmp331_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp331_AST);
				match(LITERAL_switch_is);
				AST tmp332_AST = null;
				tmp332_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp332_AST);
				match(LPAREN);
				attr_var();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				AST tmp333_AST = null;
				tmp333_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp333_AST);
				match(RPAREN);
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_source:
			{
				AST tmp334_AST = null;
				tmp334_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp334_AST);
				match(LITERAL_source);
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_string:
			{
				string_type();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				if (0==inputState.guessing)
				{
					attributes.Add("string", new CodeAttributeArgument());
				}
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_ref:
			case LITERAL_unique:
			case LITERAL_ptr:
			{
				ptr_attr();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				field_attribute_AST = currentAST.root;
				break;
			}
			case LITERAL_uuid:
			case LITERAL_version:
			case LITERAL_async_uuid:
			case LITERAL_local:
			case LITERAL_object:
			case LITERAL_pointer_default:
			case LITERAL_endpoint:
			case LITERAL_odl:
			case LITERAL_optimize:
			case LITERAL_proxy:
			case LITERAL_aggregatable:
			case LITERAL_appobject:
			case LITERAL_bindable:
			case LITERAL_control:
			case LITERAL_custom:
			case LITERAL_default:
			case LITERAL_defaultbind:
			case LITERAL_defaultcollelem:
			case LITERAL_defaultvtable:
			case LITERAL_displaybind:
			case LITERAL_dllname:
			case LITERAL_dual:
			case LITERAL_entry:
			case LITERAL_helpcontext:
			case LITERAL_helpfile:
			case LITERAL_helpstring:
			case LITERAL_helpstringdll:
			case LITERAL_hidden:
			case LITERAL_id:
			case LITERAL_idempotent:
			case LITERAL_immediatebind:
			case LITERAL_lcid:
			case LITERAL_licensed:
			case LITERAL_message:
			case LITERAL_nonbrowsable:
			case LITERAL_noncreatable:
			case LITERAL_nonextensible:
			case LITERAL_oleautomation:
			case LITERAL_restricted:
			{
				attribute(attributes);
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				field_attribute_AST = currentAST.root;
				break;
			}
			default:
			{
				throw new NoViableAltException(LT(1), getFilename());
			}
			 }
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_12_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = field_attribute_AST;
	}

	public void raises_expr() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST raises_expr_AST = null;
		StringCollection ignored = new StringCollection();

		try {      // for error handling
			AST tmp335_AST = null;
			tmp335_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp335_AST);
			match(LITERAL_raises);
			AST tmp336_AST = null;
			tmp336_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp336_AST);
			match(LPAREN);
			scoped_name_list(ignored);
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp337_AST = null;
			tmp337_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp337_AST);
			match(RPAREN);
			raises_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_16_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = raises_expr_AST;
	}

	public void context_expr() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST context_expr_AST = null;

		try {      // for error handling
			AST tmp338_AST = null;
			tmp338_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp338_AST);
			match(LITERAL_context);
			AST tmp339_AST = null;
			tmp339_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp339_AST);
			match(LPAREN);
			string_literal_list();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp340_AST = null;
			tmp340_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp340_AST);
			match(RPAREN);
			context_expr_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_16_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = context_expr_AST;
	}

	public void string_literal_list() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST string_literal_list_AST = null;

		try {      // for error handling
			string_literal();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						match(COMMA);
						string_literal();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						goto _loop230_breakloop;
					}

				}
_loop230_breakloop:				;
			}    // ( ... )*
			string_literal_list_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_14_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = string_literal_list_AST;
	}

	private void initializeFactory()
	{
		if (astFactory == null)
		{
			astFactory = new ASTFactory();
		}
		initializeASTFactory( astFactory );
	}
	static public void initializeASTFactory( ASTFactory factory )
	{
		factory.setMaxNodeType(173);
	}

	public static readonly string[] tokenNames_ = new string[] {
		@"""<0>""",
		@"""EOF""",
		@"""<2>""",
		@"""NULL_TREE_LOOKAHEAD""",
		@"""v1_enum""",
		@"""__int3264""",
		@"""__int64""",
		@""";""",
		@"""[""",
		@"""]""",
		@"""module""",
		@"""{""",
		@"""}""",
		@"""import""",
		@""",""",
		@"""library""",
		@"""coclass""",
		@"""uuid""",
		@"""version""",
		@"""(""",
		@""")""",
		@"""async_uuid""",
		@"""local""",
		@"""object""",
		@"""pointer_default""",
		@"""endpoint""",
		@"""odl""",
		@"""optimize""",
		@"""proxy""",
		@"""aggregatable""",
		@"""appobject""",
		@"""bindable""",
		@"""control""",
		@"""custom""",
		@"""default""",
		@"""defaultbind""",
		@"""defaultcollelem""",
		@"""defaultvtable""",
		@"""displaybind""",
		@"""dllname""",
		@"""dual""",
		@"""entry""",
		@"""helpcontext""",
		@"""helpfile""",
		@"""helpstring""",
		@"""helpstringdll""",
		@"""hidden""",
		@"""id""",
		@"""idempotent""",
		@"""immediatebind""",
		@"""lcid""",
		@"""licensed""",
		@"""message""",
		@"""nonbrowsable""",
		@"""noncreatable""",
		@"""nonextensible""",
		@"""oleautomation""",
		@"""restricted""",
		@"""importlib""",
		@"""interface""",
		@"""dispinterface""",
		@""":""",
		@"""::""",
		@"""const""",
		@"""=""",
		@"""*""",
		@"""|""",
		@"""^""",
		@"""&""",
		@"""<<""",
		@""">>""",
		@"""+""",
		@"""-""",
		@"""/""",
		@"""%""",
		@"""~""",
		@"""TRUE""",
		@"""true""",
		@"""FALSE""",
		@"""false""",
		@"""typedef""",
		@"""native""",
		@"""context_handle""",
		@"""handle""",
		@"""pipe""",
		@"""transmit_as""",
		@"""wire_marshal""",
		@"""represent_as""",
		@"""user_marshal""",
		@"""public""",
		@"""switch_type""",
		@"""signed""",
		@"""unsigned""",
		@"""octet""",
		@"""any""",
		@"""void""",
		@"""byte""",
		@"""wchar_t""",
		@"""handle_t""",
		@"""an integer value""",
		@"""a hexadecimal value value""",
		@"""ref""",
		@"""unique""",
		@"""ptr""",
		@"""small""",
		@"""short""",
		@"""long""",
		@"""int""",
		@"""hyper""",
		@"""char""",
		@"""float""",
		@"""double""",
		@"""boolean""",
		@"""struct""",
		@"""union""",
		@"""switch""",
		@"""case""",
		@"""enum""",
		@"""sequence""",
		@"""<""",
		@""">""",
		@"""string""",
		@"""..""",
		@"""readonly""",
		@"""attribute""",
		@"""exception""",
		@"""callback""",
		@"""broadcast""",
		@"""ignore""",
		@"""propget""",
		@"""propput""",
		@"""propputref""",
		@"""uidefault""",
		@"""usesgetlasterror""",
		@"""vararg""",
		@"""in""",
		@"""out""",
		@"""retval""",
		@"""defaultvalue""",
		@"""optional""",
		@"""requestedit""",
		@"""iid_is""",
		@"""range""",
		@"""size_is""",
		@"""max_is""",
		@"""length_is""",
		@"""first_is""",
		@"""last_is""",
		@"""switch_is""",
		@"""source""",
		@"""raises""",
		@"""context""",
		@"""SAFEARRAY""",
		@"""OCTAL""",
		@"""L""",
		@"""a string literal""",
		@"""a character literal""",
		@"""an floating point value""",
		@"""an identifer""",
		@"""cpp_quote""",
		@"""midl_pragma_warning""",
		@"""?""",
		@""".""",
		@"""!""",
		@"""double quotes""",
		@"""white space""",
		@"""a preprocessor directive""",
		@"""a comment""",
		@"""a comment""",
		@"""an escape sequence""",
		@"""an escaped character value""",
		@"""a digit""",
		@"""an octal digit""",
		@"""a hexadecimal digit"""
	};

	private static long[] mk_tokenSet_0_()
	{
		long[] data = { -7205759403792685696L, 2316539058328895488L, 6442450944L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = { -7205759403792681598L, 2316539058328895488L, 6442450944L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	private static long[] mk_tokenSet_2_()
	{
		long[] data = { 128L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());
	private static long[] mk_tokenSet_3_()
	{
		long[] data = { -4611686018427387552L, 173387520367656960L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_3_ = new BitSet(mk_tokenSet_3_());
	private static long[] mk_tokenSet_4_()
	{
		long[] data = { 512L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_4_ = new BitSet(mk_tokenSet_4_());
	private static long[] mk_tokenSet_5_()
	{
		long[] data = { -4611686018427387424L, 4190598387982336000L, 3238002688L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_5_ = new BitSet(mk_tokenSet_5_());
	private static long[] mk_tokenSet_6_()
	{
		long[] data = { -2594073385365293598L, 4190598387982336000L, 7532969984L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_6_ = new BitSet(mk_tokenSet_6_());
	private static long[] mk_tokenSet_7_()
	{
		long[] data = { -1048592L, -1L, 70368744177663L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_7_ = new BitSet(mk_tokenSet_7_());
	private static long[] mk_tokenSet_8_()
	{
		long[] data = { -288230376150007934L, 2679078828332222463L, 7516192768L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_8_ = new BitSet(mk_tokenSet_8_());
	private static long[] mk_tokenSet_9_()
	{
		long[] data = { 4096L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_9_ = new BitSet(mk_tokenSet_9_());
	private static long[] mk_tokenSet_10_()
	{
		long[] data = { 2305843009214763648L, 360287970189641726L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_10_ = new BitSet(mk_tokenSet_10_());
	private static long[] mk_tokenSet_11_()
	{
		long[] data = { 1729382256910274944L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_11_ = new BitSet(mk_tokenSet_11_());
	private static long[] mk_tokenSet_12_()
	{
		long[] data = { 16896L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_12_ = new BitSet(mk_tokenSet_12_());
	private static long[] mk_tokenSet_13_()
	{
		long[] data = { 1065472L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_13_ = new BitSet(mk_tokenSet_13_());
	private static long[] mk_tokenSet_14_()
	{
		long[] data = { 1048576L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_14_ = new BitSet(mk_tokenSet_14_());
	private static long[] mk_tokenSet_15_()
	{
		long[] data = { 1064960L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_15_ = new BitSet(mk_tokenSet_15_());
	private static long[] mk_tokenSet_16_()
	{
		long[] data = { 2L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_16_ = new BitSet(mk_tokenSet_16_());
	private static long[] mk_tokenSet_17_()
	{
		long[] data = { -7205759403792679550L, 2316539058328895488L, 6442450944L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_17_ = new BitSet(mk_tokenSet_17_());
	private static long[] mk_tokenSet_18_()
	{
		long[] data = { -4611686018427383328L, 4190598387982336000L, 3238002688L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_18_ = new BitSet(mk_tokenSet_18_());
	private static long[] mk_tokenSet_19_()
	{
		long[] data = { -7205759403791630974L, 2316539058328895488L, 6442450944L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_19_ = new BitSet(mk_tokenSet_19_());
	private static long[] mk_tokenSet_20_()
	{
		long[] data = { -4899916394577920126L, 2676827028518537214L, 7516192768L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_20_ = new BitSet(mk_tokenSet_20_());
	private static long[] mk_tokenSet_21_()
	{
		long[] data = { 0L, 0L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_21_ = new BitSet(mk_tokenSet_21_());
	private static long[] mk_tokenSet_22_()
	{
		long[] data = { 2305843009214763648L, 360287970189639680L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_22_ = new BitSet(mk_tokenSet_22_());
	private static long[] mk_tokenSet_23_()
	{
		long[] data = { -9223372036853710720L, 72057594037927936L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_23_ = new BitSet(mk_tokenSet_23_());
	private static long[] mk_tokenSet_24_()
	{
		long[] data = { -9223372036853710208L, 72057594037927936L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_24_ = new BitSet(mk_tokenSet_24_());
	private static long[] mk_tokenSet_25_()
	{
		long[] data = { 2305843009214763648L, 360287970189639684L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_25_ = new BitSet(mk_tokenSet_25_());
	private static long[] mk_tokenSet_26_()
	{
		long[] data = { 2305843009214763648L, 360287970189639692L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_26_ = new BitSet(mk_tokenSet_26_());
	private static long[] mk_tokenSet_27_()
	{
		long[] data = { 2305843009214763648L, 360287970189639708L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_27_ = new BitSet(mk_tokenSet_27_());
	private static long[] mk_tokenSet_28_()
	{
		long[] data = { 2305843009214763648L, 360287970189639804L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_28_ = new BitSet(mk_tokenSet_28_());
	private static long[] mk_tokenSet_29_()
	{
		long[] data = { 4611686018427912192L, 103079278976L, 1577058304L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_29_ = new BitSet(mk_tokenSet_29_());
	private static long[] mk_tokenSet_30_()
	{
		long[] data = { 2305843009214763648L, 360287970189640188L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_30_ = new BitSet(mk_tokenSet_30_());
	private static long[] mk_tokenSet_31_()
	{
		long[] data = { 4611686018427912192L, 103079276544L, 1577058304L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_31_ = new BitSet(mk_tokenSet_31_());
	private static long[] mk_tokenSet_32_()
	{
		long[] data = { 512L, 360287970189639680L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_32_ = new BitSet(mk_tokenSet_32_());
	private static long[] mk_tokenSet_33_()
	{
		long[] data = { 128L, 0L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_33_ = new BitSet(mk_tokenSet_33_());
	private static long[] mk_tokenSet_34_()
	{
		long[] data = { 1048704L, 0L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_34_ = new BitSet(mk_tokenSet_34_());
	private static long[] mk_tokenSet_35_()
	{
		long[] data = { 1065090L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_35_ = new BitSet(mk_tokenSet_35_());
	private static long[] mk_tokenSet_36_()
	{
		long[] data = { 1065088L, 72057594037927936L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_36_ = new BitSet(mk_tokenSet_36_());
	private static long[] mk_tokenSet_37_()
	{
		long[] data = { 1048576L, 0L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_37_ = new BitSet(mk_tokenSet_37_());
	private static long[] mk_tokenSet_38_()
	{
		long[] data = { -9223372036853710720L, 72057594037927938L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_38_ = new BitSet(mk_tokenSet_38_());
	private static long[] mk_tokenSet_39_()
	{
		long[] data = { 1065346L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_39_ = new BitSet(mk_tokenSet_39_());
	private static long[] mk_tokenSet_40_()
	{
		long[] data = { -4611686018427383456L, 173387520367656960L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_40_ = new BitSet(mk_tokenSet_40_());
	private static long[] mk_tokenSet_41_()
	{
		long[] data = { -4611686018427387808L, 173387520367656960L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_41_ = new BitSet(mk_tokenSet_41_());
	private static long[] mk_tokenSet_42_()
	{
		long[] data = { 4352L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_42_ = new BitSet(mk_tokenSet_42_());
	private static long[] mk_tokenSet_43_()
	{
		long[] data = { -4611686018427387424L, 173387520367656960L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_43_ = new BitSet(mk_tokenSet_43_());
	private static long[] mk_tokenSet_44_()
	{
		long[] data = { 17179873280L, 4503599627370496L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_44_ = new BitSet(mk_tokenSet_44_());
	private static long[] mk_tokenSet_45_()
	{
		long[] data = { -4611686001247518368L, 177891119995027456L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_45_ = new BitSet(mk_tokenSet_45_());
	private static long[] mk_tokenSet_46_()
	{
		long[] data = { 20480L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_46_ = new BitSet(mk_tokenSet_46_());
	private static long[] mk_tokenSet_47_()
	{
		long[] data = { 0L, 72057594037927936L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_47_ = new BitSet(mk_tokenSet_47_());
	private static long[] mk_tokenSet_48_()
	{
		long[] data = { 512L, 288230376151711744L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_48_ = new BitSet(mk_tokenSet_48_());
	private static long[] mk_tokenSet_49_()
	{
		long[] data = { -9223372036853710848L, 0L, 1073741824L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_49_ = new BitSet(mk_tokenSet_49_());
	private static long[] mk_tokenSet_50_()
	{
		long[] data = { 4611686018427388000L, 144677072743170048L, 1090519040L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_50_ = new BitSet(mk_tokenSet_50_());
	private static long[] mk_tokenSet_51_()
	{
		long[] data = { -9223372036854775680L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_51_ = new BitSet(mk_tokenSet_51_());

}
}
