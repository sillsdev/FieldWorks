//-------------------------------------------------------------------------------------------------
// <copyright file="WixParseException.cs" company="Microsoft">
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
// Exception thrown when a parsing problem occurs.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Exception throw when a parsing problem occurs.
	/// </summary>
	public class WixParseException : WixException
	{
		private string detail;

		/// <summary>
		/// Creates a new exception.
		/// </summary>
		/// <param name="detail">More information about error.</param>
		public WixParseException(string detail) :
			base(null, WixExceptionType.Parse)
		{
			this.detail = detail;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return String.Concat("Error while parsing: ", this.detail); }
		}
	}
}
