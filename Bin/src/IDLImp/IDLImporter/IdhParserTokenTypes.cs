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
	public class IdhParserTokenTypes
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

	}
}
