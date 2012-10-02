//-------------------------------------------------------------------------------------------------
// <copyright file="WixFileInUseException.cs" company="Microsoft">
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
// WixException thrown when a file is already in use and cannot be modified.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WixException thrown when a file is already in use and cannot be modified.
	/// </summary>
	public class WixFileInUseException : WixException
	{
		private const WixExceptionType ExceptionType = WixExceptionType.FileInUse;
		private string fileName;

		/// <summary>
		/// Instantiate a new WixFileInUseException.
		/// </summary>
		/// <param name="fileName">Name of the file which could not be found.</param>
		/// <param name="innerException">Exception that is the cause of this exception.</param>
		public WixFileInUseException(string fileName, Exception innerException) :
			base(null, ExceptionType)
		{
			this.fileName = fileName;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				return String.Format("The process can not access the file '{0}' because it is being used by another process.", this.fileName);
			}
		}
	}
}
