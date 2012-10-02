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
	public class SurveyorParserTokenTypes
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

	}
}
