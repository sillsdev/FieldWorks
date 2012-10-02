//-------------------------------------------------------------------------------------------------
// <copyright file="WarningEventArgs.cs" company="Microsoft">
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
// Event arguments for warning messages.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Event arguments for warning messages.
	/// </summary>
	public class WarningEventArgs : EventArgs
	{
		private WarningLevel level;
		private string message;

		/// <summary>
		/// WarningEventArgs Constructor.
		/// </summary>
		/// <param name="level">Level of the warning message.</param>
		/// <param name="message">Warning message content.</param>
		public WarningEventArgs(WarningLevel level, string message)
		{
			this.level = level;
			this.message = message;
		}

		/// <summary>
		/// Getter for the warning level.
		/// </summary>
		/// <value>The level of this message.</value>
		public WarningLevel Level
		{
			get { return this.level; }
		}

		/// <summary>
		/// Getter for the message content.
		/// </summary>
		/// <value>The message content.</value>
		public string Message
		{
			get { return this.message; }
		}
	}
}