// $ANTLR 2.7.7 (20060930): "idh.g" -> "IdhParser.cs"$

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

	public 	class IdhParser : antlr.LLkParser
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


		protected void initialize()
		{
			tokenNames = tokenNames_;
			initializeFactory();
		}


		protected IdhParser(TokenBuffer tokenBuf, int k) : base(tokenBuf, k)
		{
			initialize();
		}

		public IdhParser(TokenBuffer tokenBuf) : this(tokenBuf,2)
		{
		}

		protected IdhParser(TokenStream lexer, int k) : base(lexer,k)
		{
			initialize();
		}

		public IdhParser(TokenStream lexer) : this(lexer,2)
		{
		}

		public IdhParser(ParserSharedInputState state) : base(state,2)
		{
			initialize();
		}

	public IdhCommentProcessor.CommentInfo  idhfile() //throws RecognitionException, TokenStreamException
{
		IdhCommentProcessor.CommentInfo info;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST idhfile_AST = null;
		IToken  comment = null;
		AST comment_AST = null;
		IToken  id = null;
		AST id_AST = null;

				Dictionary<string, IdhCommentProcessor.CommentInfo> toplevel = new Dictionary<string, IdhCommentProcessor.CommentInfo>();
				info = new IdhCommentProcessor.CommentInfo(string.Empty, toplevel, 0);


		try {      // for error handling
			{ // ( ... )+
				int _cnt7=0;
				for (;;)
				{
					switch ( LA(1) )
					{
					case LITERAL_interface:
					{
						forwardDeclaration();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case LITERAL_DeclareCoClass:
					{
						coclassDeclaration();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
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
						if (0==inputState.guessing)
						{

											#if DEBUG_IDHGRAMMAR
											System.Diagnostics.Debug.WriteLine("***import");
											#endif

						}
						break;
					}
					case IDENTIFIER:
					{
						id = LT(1);
						id_AST = astFactory.create(id);
						astFactory.addASTChild(ref currentAST, id_AST);
						match(IDENTIFIER);
						if (0==inputState.guessing)
						{

											#if DEBUG_IDHGRAMMAR
											System.Diagnostics.Debug.WriteLine("***IDENTIFIER: " + id_AST.getText());
											#endif

						}
						break;
					}
					case LITERAL_const:
					{
						constDeclaration();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{

											#if DEBUG_IDHGRAMMAR
											System.Diagnostics.Debug.WriteLine("***const");
											#endif

						}
						break;
					}
					default:
						bool synPredMatched6 = false;
						if ((((LA(1) >= COMMENT && LA(1) <= PREPROCESSOR)) && (tokenSet_0_.member(LA(2)))))
						{
							int _m6 = mark();
							synPredMatched6 = true;
							inputState.guessing++;
							try {
								{
									{    // ( ... )*
										for (;;)
										{
											if ((LA(1)==COMMENT))
											{
												match(COMMENT);
											}
											else
											{
												goto _loop5_breakloop;
											}

										}
_loop5_breakloop:										;
									}    // ( ... )*
									match(LITERAL_typedef);
								}
							}
							catch (RecognitionException)
							{
								synPredMatched6 = false;
							}
							rewind(_m6);
							inputState.guessing--;
						}
						if ( synPredMatched6 )
						{
							typedef(toplevel);
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
						}
						else if ((LA(1)==COMMENT) && (tokenSet_1_.member(LA(2)))) {
							comment = LT(1);
							comment_AST = astFactory.create(comment);
							astFactory.addASTChild(ref currentAST, comment_AST);
							match(COMMENT);
							if (0==inputState.guessing)
							{

												info.Comment = comment.getText();
												info.LineNumber = comment.getLine();

							}
						}
						else if ((LA(1)==COMMENT) && (LA(2)==LITERAL_DeclareInterface)) {
							interfaceDeclaration(toplevel);
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
						}
						else if ((LA(1)==COMMENT) && (LA(2)==LITERAL_DeclareDualInterface||LA(2)==18)) {
							dualInterface(toplevel);
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
						}
						else if ((LA(1)==PREPROCESSOR) && (tokenSet_1_.member(LA(2)))) {
							AST tmp1_AST = null;
							tmp1_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp1_AST);
							match(PREPROCESSOR);
						}
					else
					{
						if (_cnt7 >= 1) { goto _loop7_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}
					break; }
					_cnt7++;
				}
_loop7_breakloop:				;
			}    // ( ... )+
			idhfile_AST = currentAST.root;
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
		returnAST = idhfile_AST;
		return info;
	}

	public void typedef(
		Dictionary<string, IdhCommentProcessor.CommentInfo> typedefs
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST typedef_AST = null;
		IToken  name = null;
		AST name_AST = null;
		IToken  name2 = null;
		AST name2_AST = null;
		IToken  structname = null;
		AST structname_AST = null;
		IToken  structname2 = null;
		AST structname2_AST = null;

				string mainComment, firstComment;
				Dictionary<string, IdhCommentProcessor.CommentInfo> children = new Dictionary<string, IdhCommentProcessor.CommentInfo>();


		try {      // for error handling
			mainComment=comment();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp2_AST = null;
			tmp2_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp2_AST);
			match(LITERAL_typedef);
			{
				switch ( LA(1) )
				{
				case ATTRIBUTE:
				case LITERAL_enum:
				{
					{
						switch ( LA(1) )
						{
						case ATTRIBUTE:
						{
							AST tmp3_AST = null;
							tmp3_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp3_AST);
							match(ATTRIBUTE);
							break;
						}
						case LITERAL_enum:
						{
							break;
						}
						default:
						{
							throw new NoViableAltException(LT(1), getFilename());
						}
						 }
					}
					AST tmp4_AST = null;
					tmp4_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp4_AST);
					match(LITERAL_enum);
					{
						switch ( LA(1) )
						{
						case IDENTIFIER:
						{
							name = LT(1);
							name_AST = astFactory.create(name);
							astFactory.addASTChild(ref currentAST, name_AST);
							match(IDENTIFIER);
							break;
						}
						case COMMENT:
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
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)==COMMENT))
							{
								AST tmp5_AST = null;
								tmp5_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp5_AST);
								match(COMMENT);
							}
							else
							{
								goto _loop37_breakloop;
							}

						}
_loop37_breakloop:						;
					}    // ( ... )*
					AST tmp6_AST = null;
					tmp6_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp6_AST);
					match(LBRACE);
					firstComment=comment();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					{ // ( ... )+
						int _cnt39=0;
						for (;;)
						{
							if ((LA(1)==IDENTIFIER))
							{
								enumMemberDeclaration(children, ref firstComment);
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(ref currentAST, returnAST);
								}
							}
							else
							{
								if (_cnt39 >= 1) { goto _loop39_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
							}

							_cnt39++;
						}
_loop39_breakloop:						;
					}    // ( ... )+
					AST tmp7_AST = null;
					tmp7_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp7_AST);
					match(RBRACE);
					{
						switch ( LA(1) )
						{
						case IDENTIFIER:
						{
							name2 = LT(1);
							name2_AST = astFactory.create(name2);
							astFactory.addASTChild(ref currentAST, name2_AST);
							match(IDENTIFIER);
							break;
						}
						case SEMICOLON:
						{
							break;
						}
						default:
						{
							throw new NoViableAltException(LT(1), getFilename());
						}
						 }
					}
					AST tmp8_AST = null;
					tmp8_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp8_AST);
					match(SEMICOLON);
					if (0==inputState.guessing)
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
					break;
				}
				case LITERAL_struct:
				{
					AST tmp9_AST = null;
					tmp9_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp9_AST);
					match(LITERAL_struct);
					{
						switch ( LA(1) )
						{
						case IDENTIFIER:
						{
							structname = LT(1);
							structname_AST = astFactory.create(structname);
							astFactory.addASTChild(ref currentAST, structname_AST);
							match(IDENTIFIER);
							break;
						}
						case COMMENT:
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
					{    // ( ... )*
						for (;;)
						{
							if ((LA(1)==COMMENT))
							{
								AST tmp10_AST = null;
								tmp10_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp10_AST);
								match(COMMENT);
							}
							else
							{
								goto _loop43_breakloop;
							}

						}
_loop43_breakloop:						;
					}    // ( ... )*
					AST tmp11_AST = null;
					tmp11_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp11_AST);
					match(LBRACE);
					firstComment=comment();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					{ // ( ... )+
						int _cnt45=0;
						for (;;)
						{
							if ((LA(1)==IDENTIFIER))
							{
								structMemberDeclaration(children, ref firstComment);
								if (0 == inputState.guessing)
								{
									astFactory.addASTChild(ref currentAST, returnAST);
								}
							}
							else
							{
								if (_cnt45 >= 1) { goto _loop45_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
							}

							_cnt45++;
						}
_loop45_breakloop:						;
					}    // ( ... )+
					AST tmp12_AST = null;
					tmp12_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp12_AST);
					match(RBRACE);
					{
						switch ( LA(1) )
						{
						case IDENTIFIER:
						{
							structname2 = LT(1);
							structname2_AST = astFactory.create(structname2);
							astFactory.addASTChild(ref currentAST, structname2_AST);
							match(IDENTIFIER);
							break;
						}
						case SEMICOLON:
						{
							break;
						}
						default:
						{
							throw new NoViableAltException(LT(1), getFilename());
						}
						 }
					}
					AST tmp13_AST = null;
					tmp13_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp13_AST);
					match(SEMICOLON);
					if (0==inputState.guessing)
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
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			typedef_AST = currentAST.root;
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
		returnAST = typedef_AST;
	}

	public void forwardDeclaration() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST forwardDeclaration_AST = null;

		try {      // for error handling
			AST tmp14_AST = null;
			tmp14_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp14_AST);
			match(LITERAL_interface);
			AST tmp15_AST = null;
			tmp15_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp15_AST);
			match(IDENTIFIER);
			AST tmp16_AST = null;
			tmp16_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp16_AST);
			match(SEMICOLON);
			forwardDeclaration_AST = currentAST.root;
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
		returnAST = forwardDeclaration_AST;
	}

	public void interfaceDeclaration(
		Dictionary<string, IdhCommentProcessor.CommentInfo> classes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST interfaceDeclaration_AST = null;
		IToken  comment = null;
		AST comment_AST = null;
		IToken  name = null;
		AST name_AST = null;
		Dictionary<string, IdhCommentProcessor.CommentInfo> methods = new Dictionary<string, IdhCommentProcessor.CommentInfo>();

		try {      // for error handling
			comment = LT(1);
			comment_AST = astFactory.create(comment);
			astFactory.addASTChild(ref currentAST, comment_AST);
			match(COMMENT);
			AST tmp17_AST = null;
			tmp17_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp17_AST);
			match(LITERAL_DeclareInterface);
			AST tmp18_AST = null;
			tmp18_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp18_AST);
			match(LPAREN);
			name = LT(1);
			name_AST = astFactory.create(name);
			astFactory.addASTChild(ref currentAST, name_AST);
			match(IDENTIFIER);
			AST tmp19_AST = null;
			tmp19_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp19_AST);
			match(COMMA);
			AST tmp20_AST = null;
			tmp20_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp20_AST);
			match(IDENTIFIER);
			AST tmp21_AST = null;
			tmp21_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp21_AST);
			match(COMMA);
			guid();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp22_AST = null;
			tmp22_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp22_AST);
			match(RPAREN);
			AST tmp23_AST = null;
			tmp23_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp23_AST);
			match(LBRACE);
			{ // ( ... )+
				int _cnt15=0;
				for (;;)
				{
					bool synPredMatched14 = false;
					if (((LA(1)==COMMENT||LA(1)==IDENTIFIER||LA(1)==ATTRIBUTE) && (LA(2)==COMMENT||LA(2)==IDENTIFIER||LA(2)==ATTRIBUTE)))
					{
						int _m14 = mark();
						synPredMatched14 = true;
						inputState.guessing++;
						try {
							{
								{    // ( ... )*
									for (;;)
									{
										if ((LA(1)==COMMENT))
										{
											match(COMMENT);
										}
										else
										{
											goto _loop12_breakloop;
										}

									}
_loop12_breakloop:									;
								}    // ( ... )*
								{
									switch ( LA(1) )
									{
									case ATTRIBUTE:
									{
										match(ATTRIBUTE);
										break;
									}
									case IDENTIFIER:
									{
										break;
									}
									default:
									{
										throw new NoViableAltException(LT(1), getFilename());
									}
									 }
								}
								match(IDENTIFIER);
							}
						}
						catch (RecognitionException)
						{
							synPredMatched14 = false;
						}
						rewind(_m14);
						inputState.guessing--;
					}
					if ( synPredMatched14 )
					{
						methodDeclaration(methods);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else if ((LA(1)==COMMENT) && (tokenSet_4_.member(LA(2)))) {
						AST tmp24_AST = null;
						tmp24_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp24_AST);
						match(COMMENT);
					}
					else if ((LA(1)==PREPROCESSOR)) {
						AST tmp25_AST = null;
						tmp25_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp25_AST);
						match(PREPROCESSOR);
					}
					else
					{
						if (_cnt15 >= 1) { goto _loop15_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt15++;
				}
_loop15_breakloop:				;
			}    // ( ... )+
			AST tmp26_AST = null;
			tmp26_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp26_AST);
			match(RBRACE);
			{
				switch ( LA(1) )
				{
				case SEMICOLON:
				{
					AST tmp27_AST = null;
					tmp27_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp27_AST);
					match(SEMICOLON);
					break;
				}
				case EOF:
				case COMMENT:
				case LITERAL_typedef:
				case PREPROCESSOR:
				case IDENTIFIER:
				case LITERAL_DeclareCoClass:
				case LITERAL_interface:
				case LITERAL_const:
				case LITERAL_import:
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

							classes[name.getText()] = new IdhCommentProcessor.CommentInfo(comment.getText(), methods, comment.getLine());
							#if DEBUG_IDHGRAMMAR
							System.Diagnostics.Debug.WriteLine("***DeclareInterface: " + name.getText());
							#endif

			}
			interfaceDeclaration_AST = currentAST.root;
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
		returnAST = interfaceDeclaration_AST;
	}

	public void dualInterface(
		Dictionary<string, IdhCommentProcessor.CommentInfo> classes
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST dualInterface_AST = null;
		IToken  comment = null;
		AST comment_AST = null;
		IToken  name = null;
		AST name_AST = null;
		Dictionary<string, IdhCommentProcessor.CommentInfo> methods = new Dictionary<string, IdhCommentProcessor.CommentInfo>();

		try {      // for error handling
			comment = LT(1);
			comment_AST = astFactory.create(comment);
			astFactory.addASTChild(ref currentAST, comment_AST);
			match(COMMENT);
			{
				switch ( LA(1) )
				{
				case LITERAL_DeclareDualInterface:
				{
					AST tmp28_AST = null;
					tmp28_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp28_AST);
					match(LITERAL_DeclareDualInterface);
					break;
				}
				case 18:
				{
					AST tmp29_AST = null;
					tmp29_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp29_AST);
					match(18);
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp30_AST = null;
			tmp30_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp30_AST);
			match(LPAREN);
			name = LT(1);
			name_AST = astFactory.create(name);
			astFactory.addASTChild(ref currentAST, name_AST);
			match(IDENTIFIER);
			AST tmp31_AST = null;
			tmp31_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp31_AST);
			match(COMMA);
			guid();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp32_AST = null;
			tmp32_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp32_AST);
			match(RPAREN);
			AST tmp33_AST = null;
			tmp33_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp33_AST);
			match(LBRACE);
			{ // ( ... )+
				int _cnt29=0;
				for (;;)
				{
					bool synPredMatched28 = false;
					if (((LA(1)==COMMENT||LA(1)==IDENTIFIER||LA(1)==ATTRIBUTE) && (LA(2)==COMMENT||LA(2)==IDENTIFIER||LA(2)==ATTRIBUTE)))
					{
						int _m28 = mark();
						synPredMatched28 = true;
						inputState.guessing++;
						try {
							{
								{    // ( ... )*
									for (;;)
									{
										if ((LA(1)==COMMENT))
										{
											match(COMMENT);
										}
										else
										{
											goto _loop26_breakloop;
										}

									}
_loop26_breakloop:									;
								}    // ( ... )*
								{
									switch ( LA(1) )
									{
									case ATTRIBUTE:
									{
										match(ATTRIBUTE);
										break;
									}
									case IDENTIFIER:
									{
										break;
									}
									default:
									{
										throw new NoViableAltException(LT(1), getFilename());
									}
									 }
								}
								match(IDENTIFIER);
							}
						}
						catch (RecognitionException)
						{
							synPredMatched28 = false;
						}
						rewind(_m28);
						inputState.guessing--;
					}
					if ( synPredMatched28 )
					{
						methodDeclaration(methods);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else if ((LA(1)==COMMENT) && (tokenSet_4_.member(LA(2)))) {
						AST tmp34_AST = null;
						tmp34_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp34_AST);
						match(COMMENT);
					}
					else if ((LA(1)==PREPROCESSOR)) {
						AST tmp35_AST = null;
						tmp35_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp35_AST);
						match(PREPROCESSOR);
					}
					else
					{
						if (_cnt29 >= 1) { goto _loop29_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt29++;
				}
_loop29_breakloop:				;
			}    // ( ... )+
			AST tmp36_AST = null;
			tmp36_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp36_AST);
			match(RBRACE);
			{
				switch ( LA(1) )
				{
				case SEMICOLON:
				{
					AST tmp37_AST = null;
					tmp37_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp37_AST);
					match(SEMICOLON);
					break;
				}
				case EOF:
				case COMMENT:
				case LITERAL_typedef:
				case PREPROCESSOR:
				case IDENTIFIER:
				case LITERAL_DeclareCoClass:
				case LITERAL_interface:
				case LITERAL_const:
				case LITERAL_import:
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

							classes[name.getText()] = new IdhCommentProcessor.CommentInfo(comment.getText(), methods, comment.getLine());
							#if DEBUG_IDHGRAMMAR
							System.Diagnostics.Debug.WriteLine("***DeclareDualInterface: " + name.getText());
							#endif

			}
			dualInterface_AST = currentAST.root;
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
		returnAST = dualInterface_AST;
	}

	public void coclassDeclaration() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST coclassDeclaration_AST = null;

		try {      // for error handling
			AST tmp38_AST = null;
			tmp38_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp38_AST);
			match(LITERAL_DeclareCoClass);
			AST tmp39_AST = null;
			tmp39_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp39_AST);
			match(LPAREN);
			AST tmp40_AST = null;
			tmp40_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp40_AST);
			match(IDENTIFIER);
			AST tmp41_AST = null;
			tmp41_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp41_AST);
			match(COMMA);
			guid();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp42_AST = null;
			tmp42_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp42_AST);
			match(RPAREN);
			AST tmp43_AST = null;
			tmp43_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp43_AST);
			match(LBRACE);
			{ // ( ... )+
				int _cnt19=0;
				for (;;)
				{
					if ((LA(1)==LITERAL_interface))
					{
						forwardDeclaration();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
					}
					else
					{
						if (_cnt19 >= 1) { goto _loop19_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt19++;
				}
_loop19_breakloop:				;
			}    // ( ... )+
			AST tmp44_AST = null;
			tmp44_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp44_AST);
			match(RBRACE);
			{
				switch ( LA(1) )
				{
				case SEMICOLON:
				{
					AST tmp45_AST = null;
					tmp45_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp45_AST);
					match(SEMICOLON);
					break;
				}
				case EOF:
				case COMMENT:
				case LITERAL_typedef:
				case PREPROCESSOR:
				case IDENTIFIER:
				case LITERAL_DeclareCoClass:
				case LITERAL_interface:
				case LITERAL_const:
				case LITERAL_import:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			coclassDeclaration_AST = currentAST.root;
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
		returnAST = coclassDeclaration_AST;
	}

	public void import() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST import_AST = null;

		try {      // for error handling
			AST tmp46_AST = null;
			tmp46_AST = astFactory.create(LT(1));
			astFactory.makeASTRoot(ref currentAST, tmp46_AST);
			match(LITERAL_import);
			AST tmp47_AST = null;
			tmp47_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp47_AST);
			match(STRING_LITERAL);
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMA))
					{
						AST tmp48_AST = null;
						tmp48_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp48_AST);
						match(COMMA);
						AST tmp49_AST = null;
						tmp49_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp49_AST);
						match(STRING_LITERAL);
					}
					else
					{
						goto _loop90_breakloop;
					}

				}
_loop90_breakloop:				;
			}    // ( ... )*
			{
				switch ( LA(1) )
				{
				case SEMICOLON:
				{
					AST tmp50_AST = null;
					tmp50_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp50_AST);
					match(SEMICOLON);
					break;
				}
				case EOF:
				case COMMENT:
				case LITERAL_typedef:
				case PREPROCESSOR:
				case IDENTIFIER:
				case LITERAL_DeclareCoClass:
				case LITERAL_interface:
				case LITERAL_const:
				case LITERAL_import:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			import_AST = currentAST.root;
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
		returnAST = import_AST;
	}

	public void constDeclaration() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST constDeclaration_AST = null;

		try {      // for error handling
			AST tmp51_AST = null;
			tmp51_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp51_AST);
			match(LITERAL_const);
			AST tmp52_AST = null;
			tmp52_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp52_AST);
			match(IDENTIFIER);
			AST tmp53_AST = null;
			tmp53_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp53_AST);
			match(IDENTIFIER);
			AST tmp54_AST = null;
			tmp54_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp54_AST);
			match(EQUAL);
			enumVal();
			if (0 == inputState.guessing)
			{
				astFactory.addASTChild(ref currentAST, returnAST);
			}
			AST tmp55_AST = null;
			tmp55_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp55_AST);
			match(SEMICOLON);
			constDeclaration_AST = currentAST.root;
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
		returnAST = constDeclaration_AST;
	}

	public void guid() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST guid_AST = null;

		try {      // for error handling
			AST tmp56_AST = null;
			tmp56_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp56_AST);
			match(IDENTIFIER);
			AST tmp57_AST = null;
			tmp57_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp57_AST);
			match(MINUS);
			AST tmp58_AST = null;
			tmp58_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp58_AST);
			match(IDENTIFIER);
			AST tmp59_AST = null;
			tmp59_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp59_AST);
			match(MINUS);
			AST tmp60_AST = null;
			tmp60_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp60_AST);
			match(IDENTIFIER);
			AST tmp61_AST = null;
			tmp61_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp61_AST);
			match(MINUS);
			AST tmp62_AST = null;
			tmp62_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp62_AST);
			match(IDENTIFIER);
			AST tmp63_AST = null;
			tmp63_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp63_AST);
			match(MINUS);
			AST tmp64_AST = null;
			tmp64_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp64_AST);
			match(IDENTIFIER);
			guid_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_5_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = guid_AST;
	}

	public void methodDeclaration(
		Dictionary<string, IdhCommentProcessor.CommentInfo> methods
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST methodDeclaration_AST = null;
		IToken  comment = null;
		AST comment_AST = null;
		IToken  name = null;
		AST name_AST = null;
		IToken  paramComment = null;
		AST paramComment_AST = null;

				StringBuilder bldr = new StringBuilder();
				Dictionary<string, IdhCommentProcessor.CommentInfo> parameters = new Dictionary<string, IdhCommentProcessor.CommentInfo>();
				string lastParamName = null;
				int lastParamLine = 0;


		try {      // for error handling
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMENT))
					{
						comment = LT(1);
						comment_AST = astFactory.create(comment);
						astFactory.addASTChild(ref currentAST, comment_AST);
						match(COMMENT);
						if (0==inputState.guessing)
						{
							bldr.Append(comment.getText());
						}
					}
					else
					{
						goto _loop64_breakloop;
					}

				}
_loop64_breakloop:				;
			}    // ( ... )*
			{
				switch ( LA(1) )
				{
				case ATTRIBUTE:
				{
					AST tmp65_AST = null;
					tmp65_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp65_AST);
					match(ATTRIBUTE);
					break;
				}
				case IDENTIFIER:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp66_AST = null;
			tmp66_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp66_AST);
			match(IDENTIFIER);
			name = LT(1);
			name_AST = astFactory.create(name);
			astFactory.addASTChild(ref currentAST, name_AST);
			match(IDENTIFIER);
			AST tmp67_AST = null;
			tmp67_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp67_AST);
			match(LPAREN);
			{
				switch ( LA(1) )
				{
				case COMMENT:
				{
					AST tmp68_AST = null;
					tmp68_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp68_AST);
					match(COMMENT);
					break;
				}
				case IDENTIFIER:
				case RPAREN:
				case ATTRIBUTE:
				case LITERAL_const:
				{
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
					if ((LA(1)==IDENTIFIER||LA(1)==ATTRIBUTE||LA(1)==LITERAL_const))
					{
						lastParamName=parameterDeclaration(parameters);
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{
							lastParamLine = LT(1).getLine();
						}
					}
					else
					{
						goto _loop68_breakloop;
					}

				}
_loop68_breakloop:				;
			}    // ( ... )*
			AST tmp69_AST = null;
			tmp69_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp69_AST);
			match(RPAREN);
			AST tmp70_AST = null;
			tmp70_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp70_AST);
			match(SEMICOLON);
			{
				if (((LA(1)==COMMENT) && (tokenSet_4_.member(LA(2))))&&(LT(1).getLine() == lastParamLine))
				{
					paramComment = LT(1);
					paramComment_AST = astFactory.create(paramComment);
					astFactory.addASTChild(ref currentAST, paramComment_AST);
					match(COMMENT);
				}
				else if ((tokenSet_4_.member(LA(1))) && (tokenSet_6_.member(LA(2)))) {
				}
				else
				{
					throw new NoViableAltException(LT(1), getFilename());
				}

			}
			if (0==inputState.guessing)
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
			methodDeclaration_AST = currentAST.root;
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
		returnAST = methodDeclaration_AST;
	}

	public string  comment() //throws RecognitionException, TokenStreamException
{
		string s;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST comment_AST = null;
		IToken  text = null;
		AST text_AST = null;

				s = string.Empty;
				StringBuilder bldr = new StringBuilder();


		try {      // for error handling
			{    // ( ... )*
				for (;;)
				{
					switch ( LA(1) )
					{
					case COMMENT:
					{
						text = LT(1);
						text_AST = astFactory.create(text);
						astFactory.addASTChild(ref currentAST, text_AST);
						match(COMMENT);
						if (0==inputState.guessing)
						{
							bldr.Append(text.getText());
						}
						break;
					}
					case PREPROCESSOR:
					{
						AST tmp71_AST = null;
						tmp71_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp71_AST);
						match(PREPROCESSOR);
						break;
					}
					default:
					{
						goto _loop87_breakloop;
					}
					 }
				}
_loop87_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{

							s = bldr.ToString();

			}
			comment_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_7_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = comment_AST;
		return s;
	}

	public void enumMemberDeclaration(
		Dictionary<string, IdhCommentProcessor.CommentInfo> members, ref string addComment
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST enumMemberDeclaration_AST = null;
		IToken  name = null;
		AST name_AST = null;
		IToken  lineComment = null;
		AST lineComment_AST = null;

				StringBuilder bldr = new StringBuilder(addComment);
				addComment = string.Empty;
				bool fCheckInline = true;


		try {      // for error handling
			name = LT(1);
			name_AST = astFactory.create(name);
			astFactory.addASTChild(ref currentAST, name_AST);
			match(IDENTIFIER);
			{
				switch ( LA(1) )
				{
				case EQUAL:
				{
					AST tmp72_AST = null;
					tmp72_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp72_AST);
					match(EQUAL);
					enumVal();
					if (0 == inputState.guessing)
					{
						astFactory.addASTChild(ref currentAST, returnAST);
					}
					break;
				}
				case COMMENT:
				case IDENTIFIER:
				case COMMA:
				case RBRACE:
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
				case COMMA:
				{
					AST tmp73_AST = null;
					tmp73_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp73_AST);
					match(COMMA);
					break;
				}
				case COMMENT:
				case IDENTIFIER:
				case RBRACE:
				{
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
					if ((LA(1)==COMMENT))
					{
						lineComment = LT(1);
						lineComment_AST = astFactory.create(lineComment);
						astFactory.addASTChild(ref currentAST, lineComment_AST);
						match(COMMENT);
						if (0==inputState.guessing)
						{

											if (fCheckInline && name.getLine() == lineComment.getLine())
											{	// inline comment belongs to current member
												bldr.Append(lineComment_AST.getText());
											}
											else
											{	// comment belongs to following member
												if (fCheckInline)
												{	// append all comments we got so far and create a new CommentInfo
													members[name_AST.getText()] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, name.getLine());
													bldr = new StringBuilder();
													fCheckInline = false;
												}
												bldr.Append(lineComment_AST.getText());
											}

						}
					}
					else
					{
						goto _loop51_breakloop;
					}

				}
_loop51_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{

							if (fCheckInline)
							{	// append all comments left if there was none
								members[name_AST.getText()] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, name.getLine());
							}
							else
								addComment = bldr.ToString();

			}
			enumMemberDeclaration_AST = currentAST.root;
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
		returnAST = enumMemberDeclaration_AST;
	}

	public void structMemberDeclaration(
		Dictionary<string, IdhCommentProcessor.CommentInfo> members, ref string addComment
	) //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST structMemberDeclaration_AST = null;
		IToken  name = null;
		AST name_AST = null;
		IToken  lineComment = null;
		AST lineComment_AST = null;

				StringBuilder bldr = new StringBuilder(addComment);
				addComment = string.Empty;
				IdhCommentProcessor.CommentInfo info = new IdhCommentProcessor.CommentInfo(string.Empty, null, 0);
				bool fCheckInline = true;


		try {      // for error handling
			AST tmp74_AST = null;
			tmp74_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp74_AST);
			match(IDENTIFIER);
			name = LT(1);
			name_AST = astFactory.create(name);
			astFactory.addASTChild(ref currentAST, name_AST);
			match(IDENTIFIER);
			{
				switch ( LA(1) )
				{
				case ATTRIBUTE:
				{
					AST tmp75_AST = null;
					tmp75_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp75_AST);
					match(ATTRIBUTE);
					break;
				}
				case SEMICOLON:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp76_AST = null;
			tmp76_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp76_AST);
			match(SEMICOLON);
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMENT))
					{
						lineComment = LT(1);
						lineComment_AST = astFactory.create(lineComment);
						astFactory.addASTChild(ref currentAST, lineComment_AST);
						match(COMMENT);
						if (0==inputState.guessing)
						{

											if (fCheckInline && name.getLine() == lineComment.getLine())
											{	// inline comment belongs to current member
												bldr.Append(lineComment_AST.getText());
											}
											else
											{	// comment belongs to following member
												if (fCheckInline)
												{	// append all comments we got so far and create a new CommentInfo
													members[name_AST.getText()] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, 0);
													bldr = new StringBuilder();
													fCheckInline = false;
												}
												bldr.Append(lineComment_AST.getText());
											}

						}
					}
					else
					{
						goto _loop61_breakloop;
					}

				}
_loop61_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{

							if (fCheckInline)
							{	// append all comments left if there was none
								members[name_AST.getText()] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, name.getLine());
							}
							else
								addComment = bldr.ToString();

			}
			structMemberDeclaration_AST = currentAST.root;
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
		returnAST = structMemberDeclaration_AST;
	}

	public void enumVal() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST enumVal_AST = null;

		try {      // for error handling
			if ((LA(1)==IDENTIFIER||LA(1)==MINUS) && (tokenSet_9_.member(LA(2))))
			{
				number();
				if (0 == inputState.guessing)
				{
					astFactory.addASTChild(ref currentAST, returnAST);
				}
				{    // ( ... )*
					for (;;)
					{
						switch ( LA(1) )
						{
						case BAR:
						{
							AST tmp77_AST = null;
							tmp77_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp77_AST);
							match(BAR);
							AST tmp78_AST = null;
							tmp78_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp78_AST);
							match(IDENTIFIER);
							break;
						}
						case PLUS:
						{
							AST tmp79_AST = null;
							tmp79_AST = astFactory.create(LT(1));
							astFactory.addASTChild(ref currentAST, tmp79_AST);
							match(PLUS);
							number();
							if (0 == inputState.guessing)
							{
								astFactory.addASTChild(ref currentAST, returnAST);
							}
							break;
						}
						default:
						{
							goto _loop54_breakloop;
						}
						 }
					}
_loop54_breakloop:					;
				}    // ( ... )*
				enumVal_AST = currentAST.root;
			}
			else if ((LA(1)==IDENTIFIER) && (LA(2)==LPAREN)) {
				AST tmp80_AST = null;
				tmp80_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp80_AST);
				match(IDENTIFIER);
				AST tmp81_AST = null;
				tmp81_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp81_AST);
				match(LPAREN);
				{    // ( ... )*
					for (;;)
					{
						if ((tokenSet_10_.member(LA(1))))
						{
							{
								AST tmp82_AST = null;
								tmp82_AST = astFactory.create(LT(1));
								astFactory.addASTChild(ref currentAST, tmp82_AST);
								match(tokenSet_10_);
							}
						}
						else
						{
							goto _loop57_breakloop;
						}

					}
_loop57_breakloop:					;
				}    // ( ... )*
				AST tmp83_AST = null;
				tmp83_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp83_AST);
				match(RPAREN);
				enumVal_AST = currentAST.root;
			}
			else
			{
				throw new NoViableAltException(LT(1), getFilename());
			}

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
		returnAST = enumVal_AST;
	}

	public void number() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST number_AST = null;

		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case MINUS:
				{
					AST tmp84_AST = null;
					tmp84_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp84_AST);
					match(MINUS);
					break;
				}
				case IDENTIFIER:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp85_AST = null;
			tmp85_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp85_AST);
			match(IDENTIFIER);
			number_AST = currentAST.root;
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
		returnAST = number_AST;
	}

	public string  parameterDeclaration(
		Dictionary<string, IdhCommentProcessor.CommentInfo> parameters
	) //throws RecognitionException, TokenStreamException
{
		string paramName;

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST parameterDeclaration_AST = null;
		IToken  attribute = null;
		AST attribute_AST = null;
		IToken  name = null;
		AST name_AST = null;
		IToken  comment = null;
		AST comment_AST = null;

				StringBuilder bldr = new StringBuilder();
				paramName = null;


		try {      // for error handling
			{
				switch ( LA(1) )
				{
				case ATTRIBUTE:
				{
					attribute = LT(1);
					attribute_AST = astFactory.create(attribute);
					astFactory.addASTChild(ref currentAST, attribute_AST);
					match(ATTRIBUTE);
					break;
				}
				case IDENTIFIER:
				case LITERAL_const:
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
					AST tmp86_AST = null;
					tmp86_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp86_AST);
					match(LITERAL_const);
					break;
				}
				case IDENTIFIER:
				{
					break;
				}
				default:
				{
					throw new NoViableAltException(LT(1), getFilename());
				}
				 }
			}
			AST tmp87_AST = null;
			tmp87_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp87_AST);
			match(IDENTIFIER);
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==STAR))
					{
						AST tmp88_AST = null;
						tmp88_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp88_AST);
						match(STAR);
					}
					else
					{
						goto _loop74_breakloop;
					}

				}
_loop74_breakloop:				;
			}    // ( ... )*
			name = LT(1);
			name_AST = astFactory.create(name);
			astFactory.addASTChild(ref currentAST, name_AST);
			match(IDENTIFIER);
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==LBRACKET))
					{
						AST tmp89_AST = null;
						tmp89_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp89_AST);
						match(LBRACKET);
						{
							switch ( LA(1) )
							{
							case IDENTIFIER:
							case MINUS:
							{
								enumVal();
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
						AST tmp90_AST = null;
						tmp90_AST = astFactory.create(LT(1));
						astFactory.addASTChild(ref currentAST, tmp90_AST);
						match(RBRACKET);
					}
					else
					{
						goto _loop77_breakloop;
					}

				}
_loop77_breakloop:				;
			}    // ( ... )*
			{
				if ((LA(1)==COMMENT) && (LA(2)==COMMA))
				{
					AST tmp91_AST = null;
					tmp91_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp91_AST);
					match(COMMENT);
					AST tmp92_AST = null;
					tmp92_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp92_AST);
					match(COMMA);
				}
				else if ((LA(1)==COMMA)) {
					AST tmp93_AST = null;
					tmp93_AST = astFactory.create(LT(1));
					astFactory.addASTChild(ref currentAST, tmp93_AST);
					match(COMMA);
				}
				else if ((tokenSet_12_.member(LA(1))) && (tokenSet_13_.member(LA(2)))) {
				}
				else
				{
					throw new NoViableAltException(LT(1), getFilename());
				}

			}
			{    // ( ... )*
				for (;;)
				{
					if ((LA(1)==COMMENT))
					{
						comment = LT(1);
						comment_AST = astFactory.create(comment);
						astFactory.addASTChild(ref currentAST, comment_AST);
						match(COMMENT);
						if (0==inputState.guessing)
						{
							bldr.Append(comment.getText());
						}
					}
					else
					{
						goto _loop80_breakloop;
					}

				}
_loop80_breakloop:				;
			}    // ( ... )*
			if (0==inputState.guessing)
			{

							paramName = IDLConversions.ConvertParamName(name.getText());
							parameters[paramName] = new IdhCommentProcessor.CommentInfo(bldr.ToString(), null, name.getLine());
							if (attribute != null && attribute.getText().Contains("retval"))
								parameters[paramName].Attributes.Add("retval", paramName);

			}
			parameterDeclaration_AST = currentAST.root;
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
		returnAST = parameterDeclaration_AST;
		return paramName;
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
		factory.setMaxNodeType(42);
	}

	public static readonly string[] tokenNames_ = new string[] {
		@"""<0>""",
		@"""EOF""",
		@"""<2>""",
		@"""NULL_TREE_LOOKAHEAD""",
		@"""COMMENT""",
		@"""typedef""",
		@"""PREPROCESSOR""",
		@"""IDENTIFIER""",
		@"""DeclareInterface""",
		@"""LPAREN""",
		@"""COMMA""",
		@"""RPAREN""",
		@"""LBRACE""",
		@"""ATTRIBUTE""",
		@"""RBRACE""",
		@"""SEMICOLON""",
		@"""DeclareCoClass""",
		@"""DeclareDualInterface""",
		@"""DeclareDualInterface2""",
		@"""interface""",
		@"""enum""",
		@"""struct""",
		@"""EQUAL""",
		@"""BAR""",
		@"""PLUS""",
		@"""const""",
		@"""STAR""",
		@"""LBRACKET""",
		@"""RBRACKET""",
		@"""MINUS""",
		@"""import""",
		@"""STRING_LITERAL""",
		@"""ATTRVAL""",
		@"""VARIABLE""",
		@"""EXPRESSION""",
		@"""ATTRLIST""",
		@"""an escape sequence""",
		@"""an octal digit""",
		@"""DIGIT""",
		@"""HEXDIGIT""",
		@"""LETTER""",
		@"""WS""",
		@"""IGNORE"""
	};

	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 3154032L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = { 1107886322L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	private static long[] mk_tokenSet_2_()
	{
		long[] data = { 2L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());
	private static long[] mk_tokenSet_3_()
	{
		long[] data = { 1107902706L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_3_ = new BitSet(mk_tokenSet_3_());
	private static long[] mk_tokenSet_4_()
	{
		long[] data = { 24784L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_4_ = new BitSet(mk_tokenSet_4_());
	private static long[] mk_tokenSet_5_()
	{
		long[] data = { 2048L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_5_ = new BitSet(mk_tokenSet_5_());
	private static long[] mk_tokenSet_6_()
	{
		long[] data = { 1107943666L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_6_ = new BitSet(mk_tokenSet_6_());
	private static long[] mk_tokenSet_7_()
	{
		long[] data = { 160L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_7_ = new BitSet(mk_tokenSet_7_());
	private static long[] mk_tokenSet_8_()
	{
		long[] data = { 16512L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_8_ = new BitSet(mk_tokenSet_8_());
	private static long[] mk_tokenSet_9_()
	{
		long[] data = { 293651600L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_9_ = new BitSet(mk_tokenSet_9_());
	private static long[] mk_tokenSet_10_()
	{
		long[] data = { 8796093020144L, 0L, 0L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_10_ = new BitSet(mk_tokenSet_10_());
	private static long[] mk_tokenSet_11_()
	{
		long[] data = { 268485776L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_11_ = new BitSet(mk_tokenSet_11_());
	private static long[] mk_tokenSet_12_()
	{
		long[] data = { 33564816L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_12_ = new BitSet(mk_tokenSet_12_());
	private static long[] mk_tokenSet_13_()
	{
		long[] data = { 100706448L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_13_ = new BitSet(mk_tokenSet_13_());
	private static long[] mk_tokenSet_14_()
	{
		long[] data = { 33564800L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_14_ = new BitSet(mk_tokenSet_14_());

}
}
