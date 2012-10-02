//-------------------------------------------------------------------------------------------------
// <copyright file="WixVersionMismatchException.cs" company="Microsoft">
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
// Exception is thrown when object files are generated from one version, and read into another.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Exception is thrown when a version of WiX is specified for the intermediate, and then read in to a DLL with a different version.
	/// </summary>
	public class WixVersionMismatchException : WixException
	{
		private Version currentVersion;
		private Version objectVersion;
		private string fileType;
		private string path;

		/// <summary>
		/// Instantiate a new WixVersionMismatchException.
		/// </summary>
		/// <param name="currentVersion">The current WiX version.</param>
		/// <param name="objectVersion">The version in the object file.</param>
		/// <param name="fileType">The kind of file being read in.</param>
		/// <param name="path">The path of the file.</param>
		public WixVersionMismatchException(Version currentVersion, Version objectVersion, string fileType, string path) :
			base(null, WixExceptionType.VersionMismatch)
		{
			this.currentVersion = currentVersion;
			this.objectVersion = objectVersion;
			this.fileType = fileType;
			this.path = path;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				return String.Format("The file '{0}' with {1} file format version {2} is not compatible with the current {1} file format version {3}.", this.path, this.fileType, this.objectVersion.ToString(), this.currentVersion.ToString());
			}
		}
	}
}
