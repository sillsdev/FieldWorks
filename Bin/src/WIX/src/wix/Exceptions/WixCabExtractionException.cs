//-------------------------------------------------------------------------------------------------
// <copyright file="WixCabExtractionException.cs" company="Microsoft">
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
// WiX cab extraction exception.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WiX cab creation exception.
	/// </summary>
	public class WixCabExtractionException : WixException
	{
		private const WixExceptionType ExceptionType = WixExceptionType.CabExtraction;
		private string directoryName;

		/// <summary>
		/// Instantiate a new WixCabExtractionException.
		/// </summary>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public WixCabExtractionException(Exception innerException) :
			base(null, ExceptionType, innerException)
		{
			this.directoryName = null;
		}

		/// <summary>
		/// Instantiate a new WixCabExtractionException.
		/// </summary>
		/// <param name="directoryName">Name of the directory that the cabinet file could not be extracted to.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public WixCabExtractionException(string directoryName, Exception innerException) :
			base(null, ExceptionType, innerException)
		{
			this.directoryName = directoryName;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				if (null == this.directoryName)
				{
					return String.Format("Failed to extract cabinet file.");
				}
				else
				{
					return String.Format("Failed to extract cabinet file to '{0}'.", this.directoryName);
				}
			}
		}
	}
}
