//-------------------------------------------------------------------------------------------------
// <copyright file="WixFileNotFoundException.cs" company="Microsoft">
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
// WixException thrown when a file cannot be found.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// WixException thrown when a file cannot be found.
	/// </summary>
	public class WixFileNotFoundException : WixException
	{
		private const WixExceptionType ExceptionType = WixExceptionType.FileNotFound;
		private string fileName;
		private string fileType = null;
		private string extendedErrorInformation = null;

		/// <summary>
		/// Instantiate a new WixFileNotFoundException.
		/// </summary>
		/// <param name="fileName">Name of the file which could not be found.</param>
		/// <param name="fileType">Description of the type of file that could not be found.</param>
		public WixFileNotFoundException(string fileName, string fileType) :
			this(fileName, fileType, null)
		{
			this.fileName = fileName;
			this.fileType = fileType;
		}

		/// <summary>
		/// Instantiate a new WixFileNotFoundException.
		/// </summary>
		/// <param name="fileName">Name of the file which could not be found.</param>
		/// <param name="fileType">Description of the type of file that could not be found.</param>
		/// <param name="extendedErrorInformation">Extended error information.</param>
		public WixFileNotFoundException(string fileName, string fileType, string extendedErrorInformation) :
			base(null, ExceptionType)
		{
			this.fileName = fileName;
			this.fileType = fileType;
			this.extendedErrorInformation = extendedErrorInformation;
		}

		/// <summary>
		/// Instantiate a new WixFileNotFoundException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line information from which the file came.</param>
		/// <param name="fileName">Name of the file which could not be found.</param>
		/// <param name="innerException">Exception that is the cause of this exception.</param>
		public WixFileNotFoundException(SourceLineNumberCollection sourceLineNumbers, string fileName, Exception innerException) :
			base(sourceLineNumbers, ExceptionType, innerException)
		{
			this.fileName = fileName;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get
			{
				if (null != this.fileType)
				{
					if (null != this.extendedErrorInformation)
					{
						return String.Format("File of type '{0}' with name '{1}' could not be found.  {2}", this.fileType, this.fileName, this.extendedErrorInformation);
					}
					else
					{
						return String.Format("File of type '{0}' with name '{1}' could not be found.", this.fileType, this.fileName);
					}
				}
				else
				{
					return String.Format("The system cannot find the file specified: {0}", this.fileName);
				}
			}
		}
	}
}
