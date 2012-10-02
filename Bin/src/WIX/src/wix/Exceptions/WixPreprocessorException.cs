//-------------------------------------------------------------------------------------------------
// <copyright file="WixPreprocessorException.cs" company="Microsoft">
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
// Exceptions for a preprocessor.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.IO;
	using System.Xml.Schema;

	/// <summary>
	/// Wrapper for XmlSchemaExceptions that parses the error text of the message
	/// to throw a more meaningful WixException.
	/// </summary>
	public class WixPreprocessorException : WixException
	{
		private string message;

		/// <summary>
		/// Instantiate a new WixPreprocessorException.
		/// </summary>
		/// <param name="message">Message to display to the user.</param>
		public WixPreprocessorException(string message) :
			this(null, message)
		{
		}

		/// <summary>
		/// Instantiate a new WixPreprocessorException.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line number trace to where the exception occured.</param>
		/// <param name="message">Message to display to the user.</param>
		public WixPreprocessorException(SourceLineNumberCollection sourceLineNumbers, string message) :
			base(sourceLineNumbers, WixExceptionType.Preprocessor)
		{
			this.message = message;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>The error message that explains the reason for the exception, or an empty string("").</value>
		public override string Message
		{
			get { return this.message; }
		}
	}
}
