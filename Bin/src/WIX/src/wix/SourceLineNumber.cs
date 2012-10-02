//-------------------------------------------------------------------------------------------------
// <copyright file="SourceLineNumber.cs" company="Microsoft">
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
// Hold information about a source line, and provide methods for getting
// and setting this information in xml.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Text;

	/// <summary>
	/// Represents file name and line number for source file
	/// </summary>
	public class SourceLineNumber
	{
		private bool hasLineNumber;
		private string fileName;
		private int lineNumber;

		/// <summary>
		/// Constructor for a source with no line information.
		/// </summary>
		/// <param name="fileName">File name of the source.</param>
		public SourceLineNumber(string fileName)
		{
			this.hasLineNumber = false;
			this.fileName = fileName;
		}

		/// <summary>
		/// Constructor for a source with line information.
		/// </summary>
		/// <param name="fileName">File name of the source.</param>
		/// <param name="lineNumber">Line number of the source.</param>
		public SourceLineNumber(string fileName, int lineNumber)
		{
			this.hasLineNumber = true;
			this.fileName = fileName;
			this.lineNumber = lineNumber;
		}

		/// <summary>
		/// Gets the file name of the source.
		/// </summary>
		/// <value>File name for the source.</value>
		public string FileName
		{
			get { return this.fileName; }
		}

		/// <summary>
		/// Gets flag for if the source has line number information.
		/// </summary>
		/// <value>Flag if source has line number information.</value>
		public bool HasLineNumber
		{
			get { return this.hasLineNumber; }
		}

		/// <summary>
		/// Gets and sets the line number of the source.
		/// </summary>
		/// <value>Line number of the source.</value>
		public int LineNumber
		{
			get { return this.lineNumber; }
			set
			{
				this.hasLineNumber = true;
				this.lineNumber = value;
			}
		}

		/// <summary>
		/// Gets the file name and line information.
		/// </summary>
		/// <value>File name and line information.</value>
		public string QualifiedFileName
		{
			get
			{
				if (this.hasLineNumber)
				{
					return String.Concat(this.fileName, "*", this.lineNumber);
				}
				else
				{
					return this.fileName;
				}
			}
		}

		/// <summary>
		/// Clone the object.
		/// </summary>
		/// <returns>Returns a new instance of the object with the same values.</returns>
		public SourceLineNumber Clone()
		{
			SourceLineNumber newSourceLineNumber = new SourceLineNumber(this.fileName);
			newSourceLineNumber.lineNumber = this.lineNumber;
			newSourceLineNumber.hasLineNumber = this.hasLineNumber;

			return newSourceLineNumber;
		}
	}
}
