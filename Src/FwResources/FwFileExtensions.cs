// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
		/// <summary>Default extension for FieldWorks XML data files (with the period)</summary>
		public const string ksFwDataXmlFileExtension = ".fwdata";
		/// <summary>Default extension for FieldWorks DB4o data files (with the period)</summary>
		public const string ksFwDataDb4oFileExtension = ".fwdb";
		/// <summary>Default extension for FieldWorks backup files (with the period).</summary>
		public const string ksFwBackupFileExtension = ".fwbackup";
		/// <summary>Default extension for FieldWorks 6.0 and earlier backup files (with the period).</summary>
		public const string ksFw60BackupFileExtension = ".zip";
		/// <summary>Default extension for Scripture XML (Open XML for Editing Scripture) files (with the period).</summary>
		public const string ksOpenXmlForEditingScripture = ".oxes";
		/// <summary>Default extension for Scripture annotations XML (Open XML for Exchanging Scripture Annotations) files (with the period).</summary>
		public const string ksOpenXmlForExchangingScrAnnotations = ".oxesa";
		/// <summary>Default extension for key terms XML (Open XML for Exchanging Key Terms) files (with the period).</summary>
		public const string ksOpenXmlForExchangingKeyTerms = ".oxekt";
		/// <summary>Default extension for Lexicon Interchange FormaT files (with the period).</summary>
		public const string ksLexiconInterchangeFormat = ".lift";
		/// <summary>Default extension for FieldWorks TEMPORARY fallback data files (with the period).</summary>
		public const string ksFwDataFallbackFileExtension = ".bak";
		/// <summary>Default extension for FlexText format interlinear texts.</summary>
		public const string ksFLexText = ".flextext";
	}
}
