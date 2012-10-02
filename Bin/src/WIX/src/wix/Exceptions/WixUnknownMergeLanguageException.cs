//-------------------------------------------------------------------------------------------------
// <copyright file="WixUnknownMergeLanguageException.cs" company="Microsoft">
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
// WixException thrown when a merge module does not contain the specified lcid.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WixException thrown when a merge module does not contain the specified lcid.
	/// </summary>
	public class WixUnknownMergeLanguageException : WixException
	{
		private const WixExceptionType ExceptionType = WixExceptionType.FileInUse;
		private string mergeId;
		private string mergeModulePath;
		private int lcid;

		/// <summary>
		/// Instantiate a new WixUnknownMergeLanguageException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="mergeId">Name of the file which could not be found.</param>
		/// <param name="mergeModulePath">Path to the merge module.</param>
		/// <param name="lcid">User-specified lcid.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public WixUnknownMergeLanguageException(SourceLineNumberCollection sourceLineNumbers, string mergeId, string mergeModulePath, int lcid, Exception innerException) :
			base(sourceLineNumbers, ExceptionType, innerException)
		{
			this.mergeId = mergeId;
			this.mergeModulePath = mergeModulePath;
			this.lcid = lcid;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				return String.Format("Could not find language {0} in merge module '{1}' at '{2}'.", this.lcid, this.mergeId, this.mergeModulePath);
			}
		}
	}
}
