//-------------------------------------------------------------------------------------------------
// <copyright file="WixMissingActionException.cs" company="Microsoft">
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
// WiX missing action exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX missing action exception.
	/// </summary>
	public class WixMissingActionException : WixException
	{
		private string actionName;
		private string actionParentName;

		/// <summary>
		/// Instantiate a new WixMissingActionException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information of the exception.</param>
		/// <param name="actionParentName">Name of the parent action which could not be found.</param>
		/// <param name="actionName">Name of the action with a missing parent.</param>
		public WixMissingActionException(SourceLineNumberCollection sourceLineNumbers, string actionParentName, string actionName) :
			base(sourceLineNumbers, WixExceptionType.MissingAction)
		{
			this.actionName = actionName;
			this.actionParentName = actionParentName;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				return String.Format("Parent action: {0} could not be found for Action: {1}", this.actionParentName, this.actionName);
			}
		}
	}
}
