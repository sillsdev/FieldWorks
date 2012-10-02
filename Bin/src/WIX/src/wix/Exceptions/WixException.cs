//-------------------------------------------------------------------------------------------------
// <copyright file="WixException.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Base class for all Wix exceptions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Specifies the type of the WiX exception.
	/// </summary>
	public enum WixExceptionType
	{
		/// <summary>Default WiX exception type.</summary>
		Default = 0,
		/// <summary>Cab creation WiX exception type.</summary>
		CabCreation,
		/// <summary>Duplicate primaryKey WiX exception type.</summary>
		DuplicatePrimaryKey,
		/// <summary>Duplicate symbol WiX exception type.</summary>
		DuplicateSymbol,
		/// <summary>File media information key not found WiX exception type.</summary>
		FileMediaInformationKeyNotFound,
		/// <summary>File not found WiX exception type.</summary>
		FileNotFound,
		/// <summary>Invalid assembly WiX exception type.</summary>
		InvalidAssembly,
		/// <summary>Invalid attribute WiX exception type.</summary>
		InvalidAttribute,
		/// <summary>Invalid element WiX exception type.</summary>
		InvalidElement,
		/// <summary>Invalid file name WiX exception type.</summary>
		InvalidFileName,
		/// <summary>Invalid Idt WiX exception type.</summary>
		InvalidIdt,
		/// <summary>Invalid intermediate WiX exception type.</summary>
		InvalidIntermediate,
		/// <summary>Invalid output WiX exception type.</summary>
		InvalidOutput,
		/// <summary>Invalid sequence WiX exception type.</summary>
		InvalidSequence,
		/// <summary>Invalid sequence type WiX exception type.</summary>
		InvalidSequenceType,
		/// <summary>Missing action WiX exception type.</summary>
		MissingAction,
		/// <summary>Mising entry section WiX exception type.</summary>
		MissingEntrySection,
		/// <summary>Missing feature WiX exception type.</summary>
		MissingFeature,
		/// <summary>Multiple entry sections WiX exception type.</summary>
		MultipleEntrySections,
		/// <summary>Multiple primary references WiX exception type.</summary>
		MultiplePrimaryReferences,
		/// <summary>Not intermediate WiX exception type.</summary>
		NotIntermediate,
		/// <summary>Not library WiX exception type.</summary>
		NotLibrary,
		/// <summary>Not resource WiX exception type.</summary>
		NotResource,
		/// <summary>Preprocessor WiX exception type.</summary>
		Preprocessor,
		/// <summary>Prohibited attribute WiX exception type.</summary>
		ProhibitedAttribute,
		/// <summary>Recursive action WiX exception type.</summary>
		RecursiveAction,
		/// <summary>Required attribute WiX exception type.</summary>
		RequiredAttribute,
		/// <summary>Schema validation WiX exception type.</summary>
		SchemaValidation,
		/// <summary>Unresolved reference WiX exception type.</summary>
		UnresolvedReference,
		/// <summary>Warning as error WiX exception type.</summary>
		WarningAsError,
		/// <summary>Missing directory exception type.</summary>
		MissingDirectory,
		/// <summary>Invalid xml exception type.</summary>
		InvalidXml,
		/// <summary>File in use exception type.</summary>
		FileInUse,
		/// <summary>Unknown merge module language exception type.</summary>
		UnknownMergeLanguage,
		/// <summary>Merge module open exception type.</summary>
		MergeModuleOpen,
		/// <summary>Object or library files were a different version on output than input.</summary>
		VersionMismatch,
		/// <summary>Invalid extension exception type.</summary>
		InvalidExtension,
		/// <summary>An example guid was found in the input file.</summary>
		ExampleGuid,
		/// <summary>Fatal error exception type.</summary>
		FatalError,
		/// <summary>Merge failure.</summary>
		MergeFailure,
		/// <summary>Invalid codepage.</summary>
		InvalidCodepage,
		/// <summary>Failed to extract cab.</summary>
		CabExtraction,
		/// <summary>Parse error.</summary>
		Parse,
		/// <summary>Extension's namespace conflicts with another extension's namespace.</summary>
		ExtensionNamespaceConflict,
		/// <summary>Extension's table definition conflicts with another table definition.</summary>
		ExtensionTableDefinitionConflict,
		/// <summary>Extension's type conflicts with another extension's type.</summary>
		ExtensionTypeConflict,
		/// <summary>Missing table defintion.</summary>
		MissingTableDefintion,
		/// <summary>Merge module missing parent feature.</summary>
		MergeModuleMissingFeature,
		// add all new exception numbers here
	}

	/// <summary>
	/// Base class for all WiX exceptions.
	/// </summary>
	public class WixException : ApplicationException
	{
		private SourceLineNumberCollection sourceLineNumbers;
		private WixExceptionType exceptionType;

		/// <summary>
		/// Instantiate a new WixException.
		/// </summary>
		protected WixException() :
			this(null, WixExceptionType.Default, null)
		{
		}

		/// <summary>
		/// Instantiate a new WixException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="exceptionType">Type of exception.</param>
		protected WixException(SourceLineNumberCollection sourceLineNumbers, WixExceptionType exceptionType) :
			this(sourceLineNumbers, exceptionType, null)
		{
		}

		/// <summary>
		/// Instantiate a new WixException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="exceptionType">Type of exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		protected WixException(SourceLineNumberCollection sourceLineNumbers, WixExceptionType exceptionType, Exception innerException) :
			base(null, innerException)
		{
			this.sourceLineNumbers = sourceLineNumbers;
			this.exceptionType = exceptionType;
		}

		/// <summary>
		/// Get the source line information of the exception.
		/// </summary>
		/// <value>Source line information of the exception.</value>
		public SourceLineNumberCollection SourceLineNumbers
		{
			get { return this.sourceLineNumbers; }
		}

		/// <summary>
		/// Get the type of the exception.
		/// </summary>
		/// <value>Type of the exception.</value>
		public WixExceptionType Type
		{
			get { return this.exceptionType; }
		}
	}
}
