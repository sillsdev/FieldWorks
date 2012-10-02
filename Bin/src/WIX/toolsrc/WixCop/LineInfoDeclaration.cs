//-------------------------------------------------------------------------------------------------
// <copyright file="LineInfoDeclaration.cs" company="Microsoft">
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
// Wrapper for XmlDeclaration that implements IXmlLineInfo.
// Originally from www.gotdotnet.com/userfiles/XMLDom/extendDOM.zip
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
	using System;
	using System.Xml;

	/// <summary>
	/// Wrapper for XmlDeclaration that implements IXmlLineInfo.
	/// </summary>
	public class LineInfoDeclaration : XmlDeclaration, IXmlLineInfo
	{
		private int lineNumber = -1;
		private int linePosition = -1;

		/// <summary>
		/// Instantiate a new LineInfoWhitespace class.
		/// </summary>
		/// <param name="version">The XML version of the document.</param>
		/// <param name="encoding">The encoding level of the XML document.</param>
		/// <param name="standalone">The value of the standalone attribute.</param>
		/// <param name="doc">The document that owns this node.</param>
		internal LineInfoDeclaration(string version, string encoding, string standalone, XmlDocument doc) : base(version, encoding, standalone, doc)
		{
		}

		/// <summary>
		/// Gets the line number.
		/// </summary>
		/// <value>The line number.</value>
		public int LineNumber
		{
			get { return this.lineNumber; }
		}

		/// <summary>
		/// Gets the line position.
		/// </summary>
		/// <value>The line position.</value>
		public int LinePosition
		{
			get { return this.linePosition; }
		}

		/// <summary>
		/// Set the line information for this node.
		/// </summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="linePosition">The line position.</param>
		public void SetLineInfo(int lineNumber, int linePosition)
		{
			this.lineNumber = lineNumber;
			this.linePosition = linePosition;
		}

		/// <summary>
		/// Determines if this node has line information.
		/// </summary>
		/// <returns>true.</returns>
		public bool HasLineInfo()
		{
			return true;
		}
	}
}
