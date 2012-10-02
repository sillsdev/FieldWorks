//-------------------------------------------------------------------------------------------------
// <copyright file="WixMissingTableDefinitionException.cs" company="Microsoft">
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
// WiX missing table defintion exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX missing table defintion exception.
	/// </summary>
	public class WixMissingTableDefinitionException : WixException
	{
		private string tableName;

		/// <summary>
		/// Instantiate new WixMissingTableDefinitionException.
		/// </summary>
		/// <param name="tableName">The name of the table for which a defintion could not be found.</param>
		public WixMissingTableDefinitionException(string tableName) :
			base(null, WixExceptionType.MissingTableDefintion)
		{
			this.tableName = tableName;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return String.Format("Cannot find the table definitions for the '{0}' table.  This is likely due to a missing schema extension.  Please ensure all the necessary extensions are supplied on the command line with the -ext parameter.", this.tableName); }
		}
	}
}
