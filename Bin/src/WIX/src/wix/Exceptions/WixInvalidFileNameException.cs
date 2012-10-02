//-------------------------------------------------------------------------------------------------
// <copyright file="WixInvalidFileNameException.cs" company="Microsoft">
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
// Exception thrown when an invalid file name is encountered.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Exception thrown when an invalid file name is encountered.
	/// </summary>
	public class WixInvalidFileNameException : WixException
	{
		private string fileName;

		/// <summary>
		/// Creates an invalid file name exception
		/// </summary>
		/// <param name="sourceLineNumbers">Source line number the error occured at.</param>
		/// <param name="fileName">Name of the file that is invalid.</param>
		/// <param name="innerException">Original exception thrown.</param>
		public WixInvalidFileNameException(SourceLineNumberCollection sourceLineNumbers, string fileName, Exception innerException) :
			base(sourceLineNumbers, WixExceptionType.InvalidFileName, innerException)
		{
			this.fileName = fileName;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return String.Format("Invalid file name: {0}", this.fileName); }
		}
	}
}
