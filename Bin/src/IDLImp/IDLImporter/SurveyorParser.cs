// $ANTLR 2.7.7 (20060930): "SurveyorTags.g" -> "SurveyorParser.cs"$

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

	public 	class SurveyorParser : antlr.LLkParser
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

		public SurveyorParser(StringBuilder bldr, TokenStream lexer) : this(lexer,2)
		{
			m_bldr = bldr;
		}

		protected void initialize()
		{
			tokenNames = tokenNames_;
			initializeFactory();
		}


		protected SurveyorParser(TokenBuffer tokenBuf, int k) : base(tokenBuf, k)
		{
			initialize();
		}

		public SurveyorParser(TokenBuffer tokenBuf) : this(tokenBuf,2)
		{
		}

		protected SurveyorParser(TokenStream lexer, int k) : base(lexer,k)
		{
			initialize();
		}

		public SurveyorParser(TokenStream lexer) : this(lexer,2)
		{
		}

		public SurveyorParser(ParserSharedInputState state) : base(state,2)
		{
			initialize();
		}

	public void surveyorTags() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST surveyorTags_AST = null;
		AST o_AST = null;

		try {      // for error handling
			{    // ( ... )*
				for (;;)
				{
					switch ( LA(1) )
					{
					case TABLE:
					{
						table();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case REFERENCE:
					{
						reference();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					case DOLLAR:
					case IDENTIFIER:
					{
						other();
						if (0 == inputState.guessing)
						{
							o_AST = (AST)returnAST;
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{
							m_bldr.Append(o_AST.getText());
						}
						break;
					}
					case HTTP:
					{
						http();
						if (0 == inputState.guessing)
						{
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						break;
					}
					default:
					{
						goto _loop5_breakloop;
					}
					 }
				}
_loop5_breakloop:				;
			}    // ( ... )*
			surveyorTags_AST = currentAST.root;
		}
		catch (RecognitionException ex)
		{
			if (0 == inputState.guessing)
			{
				reportError(ex);
				recover(ex,tokenSet_0_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = surveyorTags_AST;
	}

	public void table() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST table_AST = null;
		AST r_AST = null;

		try {      // for error handling
			AST tmp1_AST = null;
			tmp1_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp1_AST);
			match(TABLE);
			if (0==inputState.guessing)
			{
				m_bldr.Append("<list type=\"table\">");
			}
			{ // ( ... )+
				int _cnt9=0;
				for (;;)
				{
					if ((LA(1)==ROW))
					{
						row();
						if (0 == inputState.guessing)
						{
							r_AST = (AST)returnAST;
							astFactory.addASTChild(ref currentAST, returnAST);
						}
						if (0==inputState.guessing)
						{
							m_bldr.Append(r_AST.getText());
						}
					}
					else
					{
						if (_cnt9 >= 1) { goto _loop9_breakloop; } else { throw new NoViableAltException(LT(1), getFilename());; }
					}

					_cnt9++;
				}
_loop9_breakloop:				;
			}    // ( ... )+
			AST tmp2_AST = null;
			tmp2_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp2_AST);
			match(RBRACE);
			if (0==inputState.guessing)
			{
				m_bldr.Append("</list>");
			}
			table_AST = currentAST.root;
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
		returnAST = table_AST;
	}

	public void reference() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST reference_AST = null;
		IToken  r = null;
		AST r_AST = null;

		try {      // for error handling
			r = LT(1);
			r_AST = astFactory.create(r);
			astFactory.addASTChild(ref currentAST, r_AST);
			match(REFERENCE);
			if (0==inputState.guessing)
			{
				m_bldr.Append(string.Format("<c>{0}</c>", r_AST.getText().Replace("#", ".")));
			}
			reference_AST = currentAST.root;
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
		returnAST = reference_AST;
	}

	public void other() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST other_AST = null;

		try {      // for error handling
			switch ( LA(1) )
			{
			case IDENTIFIER:
			{
				AST tmp3_AST = null;
				tmp3_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp3_AST);
				match(IDENTIFIER);
				other_AST = currentAST.root;
				break;
			}
			case DOLLAR:
			{
				AST tmp4_AST = null;
				tmp4_AST = astFactory.create(LT(1));
				astFactory.addASTChild(ref currentAST, tmp4_AST);
				match(DOLLAR);
				other_AST = currentAST.root;
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
				recover(ex,tokenSet_1_);
			}
			else
			{
				throw ex;
			}
		}
		returnAST = other_AST;
	}

	public void http() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST http_AST = null;
		IToken  h = null;
		AST h_AST = null;

		try {      // for error handling
			h = LT(1);
			h_AST = astFactory.create(h);
			astFactory.addASTChild(ref currentAST, h_AST);
			match(HTTP);
			if (0==inputState.guessing)
			{
				m_bldr.Append(string.Format("<see href=\"{0}\">{0}</see>", h_AST.getText()));
			}
			http_AST = currentAST.root;
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
		returnAST = http_AST;
	}

	public void row() //throws RecognitionException, TokenStreamException
{

		returnAST = null;
		ASTPair currentAST = new ASTPair();
		AST row_AST = null;
		IToken  f = null;
		AST f_AST = null;
		IToken  s = null;
		AST s_AST = null;

		try {      // for error handling
			AST tmp5_AST = null;
			tmp5_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp5_AST);
			match(ROW);
			f = LT(1);
			f_AST = astFactory.create(f);
			astFactory.addASTChild(ref currentAST, f_AST);
			match(CELL);
			s = LT(1);
			s_AST = astFactory.create(s);
			astFactory.addASTChild(ref currentAST, s_AST);
			match(CELL);
			AST tmp6_AST = null;
			tmp6_AST = astFactory.create(LT(1));
			astFactory.addASTChild(ref currentAST, tmp6_AST);
			match(RBRACE);
			if (0==inputState.guessing)
			{

							currentAST.root.setText(string.Format("<item><term>{0}</term>{2} <description>{2} {1}{2} </description>{2} </item>",
								f_AST.getText(), s_AST.getText(), Environment.NewLine));

			}
			row_AST = currentAST.root;
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
		returnAST = row_AST;
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
		factory.setMaxNodeType(17);
	}

	public static readonly string[] tokenNames_ = new string[] {
		@"""<0>""",
		@"""EOF""",
		@"""<2>""",
		@"""NULL_TREE_LOOKAHEAD""",
		@"""DOLLAR""",
		@"""LBRACE""",
		@"""IDENTIFIER""",
		@"""TABLE""",
		@"""RBRACE""",
		@"""ROW""",
		@"""CELL""",
		@"""HTTP""",
		@"""REFERENCE""",
		@"""POUND""",
		@"""DIGIT""",
		@"""LETTER""",
		@"""WS""",
		@"""IGNORE"""
	};

	private static long[] mk_tokenSet_0_()
	{
		long[] data = { 2L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());
	private static long[] mk_tokenSet_1_()
	{
		long[] data = { 6354L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());
	private static long[] mk_tokenSet_2_()
	{
		long[] data = { 768L, 0L};
		return data;
	}
	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());

}
}
