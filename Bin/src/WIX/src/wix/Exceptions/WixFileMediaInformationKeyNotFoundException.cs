//-------------------------------------------------------------------------------------------------
// <copyright file="WixFileMediaInformationKeyNotFoundException.cs" company="Microsoft">
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
// Exception is thrown when a FileMediaInformation key cannot be found in an installlation msi.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Exception is thrown when a FileMediaInformation key cannot be found in an installlation msi.
	/// </summary>
	public class WixFileMediaInformationKeyNotFoundException : WixException
	{
		private string key;

		/// <summary>
		/// Instantiate a new WixFileMediaInformationKeyNotFoundException.
		/// </summary>
		/// <param name="key">The invalid FileMediaInformation key.</param>
		public WixFileMediaInformationKeyNotFoundException(string key) :
			base(null, WixExceptionType.FileMediaInformationKeyNotFound)
		{
			this.key = key;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				return String.Format("FileMediaInformation key '{0}' could not be found.", this.key);
			}
		}
	}
}
