//-------------------------------------------------------------------------------------------------
// <copyright file="WixExampleGuidException.cs" company="Microsoft">
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
// WiX example guid exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX example guid exception.
	/// </summary>
	public class WixExampleGuidException : WixException
	{
		/// <summary>
		/// Instantiate a new WixExampleGuidException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		public WixExampleGuidException(SourceLineNumberCollection sourceLineNumbers) :
			base(sourceLineNumbers, WixExceptionType.ExampleGuid)
		{
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return "A Guid needs to be generated and put in place of PUT-GUID-HERE in the source file."; }
		}
	}
}
