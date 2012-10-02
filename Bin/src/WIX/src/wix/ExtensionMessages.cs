//-------------------------------------------------------------------------------------------------
// <copyright file="ExtensionMessages.cs" company="Microsoft">
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
// Wrapper object for message handling.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Wrapper object for message handling.
	/// </summary>
	public class ExtensionMessages
	{
		private IExtensionMessageHandler messageHandler;

		/// <summary>
		/// Creates a new ExtensionMessages object.
		/// </summary>
		/// <param name="messageHandler">Message handler to pass the messages along to.</param>
		internal ExtensionMessages(IExtensionMessageHandler messageHandler)
		{
			this.messageHandler = messageHandler;
		}

		/// <summary>
		/// Sends an error message.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="errorLevel">Level of the error message.</param>
		/// <param name="errorMessage">Error message string.</param>
		public void OnError(SourceLineNumberCollection sourceLineNumbers, ErrorLevel errorLevel, string errorMessage)
		{
			this.messageHandler.OnExtensionError(sourceLineNumbers, errorLevel, errorMessage);
		}

		/// <summary>
		/// Sends a warning message.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="warningLevel">Level of the warning message.</param>
		/// <param name="warningMessage">Warning message string.</param>
		public void OnWarning(SourceLineNumberCollection sourceLineNumbers, WarningLevel warningLevel, string warningMessage)
		{
			this.messageHandler.OnExtensionWarning(sourceLineNumbers, warningLevel, warningMessage);
		}

		/// <summary>
		/// Sends a verbose message.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="verboseLevel">Level of the verbose message.</param>
		/// <param name="verboseMessage">Verbose message string.</param>
		public void OnVerbose(SourceLineNumberCollection sourceLineNumbers, VerboseLevel verboseLevel, string verboseMessage)
		{
			this.messageHandler.OnExtensionVerbose(sourceLineNumbers, verboseLevel, verboseMessage);
		}
	}
}