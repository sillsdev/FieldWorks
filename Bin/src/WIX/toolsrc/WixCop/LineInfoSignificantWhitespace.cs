//-------------------------------------------------------------------------------------------------
// <copyright file="LineInfoSignificantWhitespace.cs" company="Microsoft">
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
// Wrapper for XmlSignificantWhitespace that implements IXmlLineInfo.
// Originally from www.gotdotnet.com/userfiles/XMLDom/extendDOM.zip
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
	using System;
	using System.Xml;

	/// <summary>
	/// Wrapper for XmlSignificantWhitespace that implements IXmlLineInfo.
	/// </summary>
	public class LineInfoSignificantWhitespace : XmlSignificantWhitespace, IXmlLineInfo
	{
		private int lineNumber = -1;
		private int linePosition = -1;

		/// <summary>
		/// Instantiate a new LineInfoSignificantWhitespace class.
		/// </summary>
		/// <param name="data">The string must contain only the following characters &#20; &#10; &#13; and &#9;</param>
		/// <param name="doc">The document that owns this node.</param>
		internal LineInfoSignificantWhitespace(string data, XmlDocument doc) : base(data, doc)
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
