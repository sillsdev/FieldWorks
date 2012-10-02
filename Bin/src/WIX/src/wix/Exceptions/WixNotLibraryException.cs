//-------------------------------------------------------------------------------------------------
// <copyright file="WixNotLibraryException.cs" company="Microsoft">
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
// Exception thrown when trying to create an library from a file that is not an library file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Exception thrown when trying to create an library from a file that is not an library file.
	/// </summary>
	public class WixNotLibraryException : WixException
	{
		private string detail;

		/// <summary>
		/// Creates a new exception.
		/// </summary>
		/// <param name="sourceLineNumbers">Path to file that failed.</param>
		/// <param name="detail">Extra information about error.</param>
		public WixNotLibraryException(SourceLineNumberCollection sourceLineNumbers, string detail) :
			base(sourceLineNumbers, WixExceptionType.NotLibrary)
		{
			this.detail = detail;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				if (null == this.detail)
				{
					return "Not an library file.";
				}
				else
				{
					return String.Format("Not an library file, detail: {0}.", this.detail);
				}
			}
		}
	}
}
