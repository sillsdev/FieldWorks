//-------------------------------------------------------------------------------------------------
// <copyright file="WixWarningAsErrorException.cs" company="Microsoft">
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
// WiX warning as error exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX warning as error exception.
	/// </summary>
	public class WixWarningAsErrorException : WixException
	{
		private string message;

		/// <summary>
		/// Instantiate a new WixWarningAsErrorException.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception, or an empty string("").</param>
		public WixWarningAsErrorException(string message) :
			base(null, WixExceptionType.WarningAsError, null)
		{
			this.message = message;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return this.message; }
		}
	}
}
