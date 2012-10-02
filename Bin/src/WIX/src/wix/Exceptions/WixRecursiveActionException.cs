//-------------------------------------------------------------------------------------------------
// <copyright file="WixRecursiveActionException.cs" company="Microsoft">
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
// Exception that occurs when user has loop in actions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Exception that occurs when user has loop in actions.
	/// </summary>
	public class WixRecursiveActionException : WixException
	{
		private string actionName;
		private string actionTable;

		/// <summary>
		/// Creates recursive action exception.
		/// </summary>
		/// <param name="sourceLineNumbers">Optional source file and line number error occured at.</param>
		/// <param name="actionName">Name of action in loop.</param>
		/// <param name="actionTable">Table for action.</param>
		public WixRecursiveActionException(SourceLineNumberCollection sourceLineNumbers, string actionName, string actionTable) :
			base(sourceLineNumbers, WixExceptionType.RecursiveAction)
		{
			this.actionName = actionName;
			this.actionTable = actionTable;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return String.Format("Action: {0} is recursively placed in the {1} table", this.actionName, this.actionTable); }
		}
	}
}
