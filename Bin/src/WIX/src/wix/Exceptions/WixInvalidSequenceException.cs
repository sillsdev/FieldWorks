//-------------------------------------------------------------------------------------------------
// <copyright file="WixInvalidSequenceException.cs" company="Microsoft">
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
// WiX invalid sequence exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX invalid sequence exception.
	/// </summary>
	public class WixInvalidSequenceException : WixException
	{
		private string actionId;

		/// <summary>
		/// Instantiate new WixInvalidSequenceException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="actionId">Id of the custom action with an invalid sequence.</param>
		public WixInvalidSequenceException(SourceLineNumberCollection sourceLineNumbers, string actionId) :
			base(sourceLineNumbers, WixExceptionType.InvalidSequence)
		{
			this.actionId = actionId;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				return String.Concat("Invalid sequence number for Action: ", this.actionId);
			}
		}
	}
}
