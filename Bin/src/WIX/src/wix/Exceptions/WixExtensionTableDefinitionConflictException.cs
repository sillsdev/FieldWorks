//-------------------------------------------------------------------------------------------------
// <copyright file="WixExtensionTableDefinitionConflictException.cs" company="Microsoft">
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
// WixException thrown when an extension is invalid.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WixException thrown when an extension's table definition collides with another table definition.
	/// </summary>
	public class WixExtensionTableDefinitionConflictException : WixException
	{
		private const WixExceptionType ExceptionType = WixExceptionType.ExtensionTableDefinitionConflict;
		private SchemaExtension extension;
		private TableDefinition tableDefinition;

		/// <summary>
		/// Instantiate a new WixExtensionTableDefinitionConflictException.
		/// </summary>
		/// <param name="extension">Extension with conflict.</param>
		/// <param name="tableDefinition">Table definition conflicting</param>
		public WixExtensionTableDefinitionConflictException(SchemaExtension extension, TableDefinition tableDefinition) :
			base(null, ExceptionType)
		{
			this.extension = extension;
			this.tableDefinition = tableDefinition;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				return String.Format("The specified extension '{0}' contains table definition '{1}' that conflicts with an already loaded table definition.  Either remove one of the conflicting extension or rename one of the table definitions to avoid the conflict.", this.extension.ToString(), this.tableDefinition.Name);
			}
		}
	}
}
