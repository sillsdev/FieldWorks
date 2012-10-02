//-------------------------------------------------------------------------------------------------
// <copyright file="WixFatalErrorException.cs" company="Microsoft">
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
// Exception for when fatal errors occur.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX fatal error exception.
	/// </summary>
	public class WixFatalErrorException : WixException
	{
		private MessageEventArgs messageEventArgs;

		/// <summary>
		/// Instantiate a new WixFatalErrorException.
		/// </summary>
		/// <param name="messageEventArgs">Message event args.</param>
		public WixFatalErrorException(MessageEventArgs messageEventArgs) :
			base(null, WixExceptionType.FatalError, null)
		{
			this.messageEventArgs = messageEventArgs;
		}
	}
}
