//-------------------------------------------------------------------------------------------------
// <copyright file="IMessageHandler.cs" company="Microsoft">
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
// Interface for handling messages (error/warning/verbose).
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Interface for handling messages (error/warning/verbose).
	/// </summary>
	internal interface IMessageHandler
	{
		/// <summary>
		/// Sends a message with the given arguments.
		/// </summary>
		/// <param name="mea">Message arguments.</param>
		void OnMessage(MessageEventArgs mea);
	}
}