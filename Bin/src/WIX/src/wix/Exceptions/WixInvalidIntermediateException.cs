//-------------------------------------------------------------------------------------------------
// <copyright file="WixInvalidIntermediateException.cs" company="Microsoft">
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
// WiX invalid intermediate exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX invalid intermediate exception.
	/// </summary>
	public class WixInvalidIntermediateException : WixException
	{
		private string detail;

		/// <summary>
		/// Instantiate a new WixInvalidIntermediateException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="detail">Detail of the exception.</param>
		public WixInvalidIntermediateException(SourceLineNumberCollection sourceLineNumbers, string detail) :
			base(sourceLineNumbers, WixExceptionType.InvalidIntermediate)
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
					return "Invalid object file.";
				}
				else
				{
					return String.Format("Invalid object file, detail: {0}.", this.detail);
				}
			}
		}
	}
}
