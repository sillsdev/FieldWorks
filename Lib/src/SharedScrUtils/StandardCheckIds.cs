// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StandardCheckIds.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This static class is just a holding-place for all the standard Check ID GUIDs.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	static public class StandardCheckIds
	{
		/// <summary>Check ID for Characters check</summary>
		public static readonly Guid kguidCharacters = new Guid("6558A579-B9C4-4EFD-8728-F994D0561293");
		/// <summary>Find Mixed Capitalization check</summary>
		public static readonly Guid kguidMixedCapitalization = new Guid("BABCB400-F274-4498-92C5-77E99C90F75D");
		/// <summary>Check ID for Chapter and Verse check</summary>
		public static readonly Guid kguidChapterVerse = new Guid("F17A054B-D21E-4298-A1A5-0D79C4AF6F0F");
		/// <summary>Check ID for Matching Punctuation Pairs check</summary>
		public static readonly Guid kguidMatchedPairs = new Guid("DDCCB400-F274-4498-92C5-77E99C90F75B");
		/// <summary>Check ID for Punctuation Patterns check</summary>
		public static readonly Guid kguidPunctuation = new Guid("DCC8D4D2-13B2-46E4-8FB3-29C166D189EA");
		/// <summary>Check ID for Repeated Words check</summary>
		public static readonly Guid kguidRepeatedWords = new Guid("72ABB400-F274-4498-92C5-77E99C90F75B");
		/// <summary>Check ID for Capitalization check</summary>
		public static readonly Guid kguidCapitalization = new Guid("BABCB400-F274-4498-92C5-77E99C90F75B");
		/// <summary>Check ID for Quotations check</summary>
		public static readonly Guid kguidQuotations = new Guid("DDCCB400-F274-4498-92C5-77E99C90F75C");
	}
}
