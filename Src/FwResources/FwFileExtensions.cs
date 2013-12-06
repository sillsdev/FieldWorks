// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwFileExtensions.cs
// --------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.Resources
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Static class to hold a few constant FW file extensions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FwFileExtensions
	{
		/// <summary>Default extension for Scripture XML (Open XML for Editing Scripture) files (with the period).</summary>
		public const string ksOpenXmlForEditingScripture = ".oxes";
		/// <summary>Default extension for Scripture annotations XML (Open XML for Exchanging Scripture Annotations) files (with the period).</summary>
		public const string ksOpenXmlForExchangingScrAnnotations = ".oxesa";
		/// <summary>Default extension for key terms XML (Open XML for Exchanging Key Terms) files (with the period).</summary>
		public const string ksOpenXmlForExchangingKeyTerms = ".oxekt";
		/// <summary>Default extension for Lexicon Interchange FormaT files (with the period).</summary>
		public const string ksLexiconInterchangeFormat = ".lift";
		/// <summary>Default extension for FlexText format interlinear texts.</summary>
		public const string ksFLexText = ".flextext";


		/*
		 * The following extensions are also defined in FdoFileExtensions.
		 * They are defined here as a temporary stopgap.
		 * The idea is that eventually, these will be removed
		 * from here and all references will use FdoFileExtensions.
		 *
		 * If a change is made here, it should be made in FdoFileExtensions as well.
		 */
		/// <summary>Default extension for FieldWorks XML data files (with the period)</summary>
		public const string ksFwDataXmlFileExtension = ".fwdata";
		/// <summary>Default extension for FieldWorks DB4o data files (with the period)</summary>
		public const string ksFwDataDb4oFileExtension = ".fwdb";
		/// <summary>Default extension for FieldWorks backup files (with the period).</summary>
		public const string ksFwBackupFileExtension = ".fwbackup";
		/// <summary>Default extension for FieldWorks 6.0 and earlier backup files (with the period).</summary>
		public const string ksFw60BackupFileExtension = ".zip";
		/// <summary>Default extension for FieldWorks TEMPORARY fallback data files (with the period).</summary>
		public const string ksFwDataFallbackFileExtension = ".bak";
	}
}
